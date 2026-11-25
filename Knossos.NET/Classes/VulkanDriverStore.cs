using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Knossos.NET.Classes;

internal sealed record InstalledVulkanDriver(Guid Id, string Name, string ZipName, string LibraryName, string? DownloadUrl);

internal sealed class VulkanDriverStore
{
    private const int MaxEntries = 256;
    private const int MaxMetadataBytes = 64 * 1024;
    private const long MaxPackageBytes = 256L * 1024 * 1024;
    private const string MetadataFile = "meta.json";
    private const string InstallFile = "install.json";
    private static readonly Regex LibraryNamePattern = new(@"\A[A-Za-z0-9_.+-]+\.so\z");

    private readonly string root;
    private readonly int androidApi;
    private string ActiveFile => Path.Combine(root, "active");

    public VulkanDriverStore(string root, int androidApi)
    {
        this.root = root;
        this.androidApi = androidApi;
    }

    public string GetDirectory(Guid id) => Path.Combine(root, id.ToString("D"));

    public IReadOnlyList<InstalledVulkanDriver> GetInstalled()
    {
        if (!Directory.Exists(root))
            return Array.Empty<InstalledVulkanDriver>();

        return Directory.EnumerateDirectories(root)
            .Select(Path.GetFileName)
            .Select(name => Guid.TryParseExact(name, "D", out var id) ? Find(id) : null)
            .OfType<InstalledVulkanDriver>()
            .OrderBy(driver => driver.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(driver => driver.ZipName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public InstalledVulkanDriver? GetActive()
    {
        return File.Exists(ActiveFile) && Guid.TryParseExact(File.ReadAllText(ActiveFile), "D", out var id)
            ? Find(id)
            : null;
    }

    public void SetActive(Guid? id)
    {
        if (id == null)
        {
            if (File.Exists(ActiveFile))
                File.Delete(ActiveFile);
            return;
        }
        if (Find(id.Value) == null)
            throw new IOException("This Vulkan driver is no longer installed.");

        var temporary = Path.Combine(root, "." + Guid.NewGuid());
        try
        {
            File.WriteAllText(temporary, id.Value.ToString("D"));
            File.Move(temporary, ActiveFile, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    public void Delete(Guid id)
    {
        var removed = Path.Combine(root, "." + Guid.NewGuid());
        // Removing the visible directory also makes an active selection fall back to the system driver.
        Directory.Move(GetDirectory(id), removed);
        DeleteTemporaryDirectory(removed);
    }

    public async Task InstallAsync(Stream source, string zipName, string? downloadUrl = null, string? digest = null)
    {
        var id = Guid.NewGuid();
        var staging = Path.Combine(root, "." + id);
        Directory.CreateDirectory(staging);
        try
        {
            var archive = Path.Combine(staging, "package.zip");
            using (var output = File.Create(archive))
                await CopyBoundedAsync(source, output, MaxPackageBytes).ConfigureAwait(false);

            using (var input = File.OpenRead(archive))
            {
                if (digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true)
                {
                    using var sha256 = SHA256.Create();
                    var hash = await sha256.ComputeHashAsync(input).ConfigureAwait(false);
                    if (!Convert.ToHexString(hash).Equals(digest[7..], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The downloaded ZIP did not match its SHA-256 checksum.");
                    input.Position = 0;
                }
                await ExtractAsync(input, staging).ConfigureAwait(false);
            }
            File.Delete(archive);

            var driver = ReadPackage(id, staging, zipName, downloadUrl);
            File.WriteAllText(Path.Combine(staging, InstallFile), JsonSerializer.Serialize(driver));
            Directory.Move(staging, GetDirectory(id));
        }
        finally
        {
            DeleteTemporaryDirectory(staging);
        }
    }

    private InstalledVulkanDriver? Find(Guid id)
    {
        var directory = GetDirectory(id);
        try
        {
            var installPath = Path.Combine(directory, InstallFile);
            if (!File.Exists(installPath))
                return null;
            var driver = JsonSerializer.Deserialize<InstalledVulkanDriver>(File.ReadAllText(installPath));
            return driver?.Id == id && driver.LibraryName is { } libraryName && LibraryNamePattern.IsMatch(libraryName) &&
                File.Exists(Path.Combine(directory, libraryName)) ? driver : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Log.Add(Log.LogSeverity.Warning, nameof(VulkanDriverStore), ex);
            return null;
        }
    }

    private static async Task ExtractAsync(Stream source, string directory)
    {
        using var zip = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count > MaxEntries)
            throw new InvalidDataException("The driver ZIP has too many entries.");

        long totalBytes = 0;
        foreach (var entry in zip.Entries)
        {
            if (entry.Length > MaxPackageBytes - totalBytes)
                throw new InvalidDataException("The driver ZIP exceeds the import size limit.");
            totalBytes += entry.Length;

            // ADPKG uses a flat ZIP. Other entries are never extracted.
            var metadata = entry.FullName == MetadataFile;
            if (!metadata && !LibraryNamePattern.IsMatch(entry.FullName))
                continue;
            var limit = metadata ? Math.Min(entry.Length, MaxMetadataBytes) : entry.Length;
            if (entry.Length > limit)
                throw new InvalidDataException("The driver metadata is too large.");

            using var input = entry.Open();
            using var output = new FileStream(Path.Combine(directory, entry.FullName), FileMode.CreateNew, FileAccess.Write);
            if (await CopyBoundedAsync(input, output, limit).ConfigureAwait(false) != entry.Length)
                throw new InvalidDataException("The driver ZIP contains an incomplete file.");
        }
    }

    private InstalledVulkanDriver ReadPackage(Guid id, string directory, string zipName, string? downloadUrl)
    {
        var metadata = Path.Combine(directory, MetadataFile);
        if (!File.Exists(metadata))
            throw new InvalidDataException("Expected meta.json at the root of the driver ZIP.");

        using var document = JsonDocument.Parse(File.ReadAllText(metadata));
        var json = document.RootElement;
        if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty("libraryName", out var library) ||
            library.ValueKind != JsonValueKind.String || library.GetString() is not { } libraryName ||
            !LibraryNamePattern.IsMatch(libraryName))
            throw new InvalidDataException("Invalid libraryName in meta.json.");

        if (json.TryGetProperty("minApi", out var api) && api.ValueKind == JsonValueKind.Number &&
            api.TryGetInt32(out int minApi) && minApi > androidApi)
            throw new InvalidDataException($"This driver requires Android API {minApi} or later.");

        var libraryPath = Path.Combine(directory, libraryName);
        if (!File.Exists(libraryPath))
            throw new InvalidDataException($"The driver ZIP is missing {libraryName}.");
        using var reader = new BinaryReader(File.OpenRead(libraryPath));
        var header = reader.ReadBytes(20);
        if (header.Length != 20 || header[0] != 0x7f || header[1] != 'E' || header[2] != 'L' || header[3] != 'F' ||
            header[4] != 2 || header[5] != 1 || header[16] != 3 || header[17] != 0 || header[18] != 183 || header[19] != 0)
            throw new InvalidDataException("The main driver library must be an ARM64 shared object.");

        var displayName = json.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString() : null;
        return new InstalledVulkanDriver(id, string.IsNullOrWhiteSpace(displayName) ? libraryName : displayName,
            zipName, libraryName, downloadUrl);
    }

    private static async Task<long> CopyBoundedAsync(Stream source, Stream destination, long limit)
    {
        var buffer = new byte[64 * 1024];
        long total = 0;
        int bytes;
        while ((bytes = await source.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) != 0)
        {
            total += bytes;
            if (total > limit)
                throw new InvalidDataException("The driver ZIP exceeds the import size limit.");
            await destination.WriteAsync(buffer.AsMemory(0, bytes)).ConfigureAwait(false);
        }
        return total;
    }

    private static void DeleteTemporaryDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Add(Log.LogSeverity.Warning, nameof(VulkanDriverStore), ex);
        }
    }
}
