using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Knossos.NET.Classes;

namespace Knossos.NET.Models;

public sealed record VulkanDriverRepository(string Name, string GitHubPath, string? Compatibility)
{
    public string Label => string.IsNullOrWhiteSpace(Compatibility) ? Name : $"{Name} ({Compatibility})";
}

public sealed record VulkanDriverReleaseZip(string ReleaseName, string ZipName, string DownloadUrl, string? Digest);

internal static class VulkanDriverCatalog
{
    private const string RepositoryListUrl =
        "https://raw.githubusercontent.com/KnossosNET/KNet-General-Resources-Repo/refs/heads/main/vulkan-repos.json";

    public static async Task<IReadOnlyList<VulkanDriverRepository>> GetRepositoriesAsync()
    {
        using var client = KnUtils.GetHttpClient();
        var json = await client.GetStringAsync(RepositoryListUrl).ConfigureAwait(false);
        var entries = JsonSerializer.Deserialize<RepositoryEntry[]>(json);
        if (entries == null)
            throw new InvalidDataException("The Vulkan driver repository list is invalid.");

        var repositories = new List<VulkanDriverRepository>();
        foreach (var entry in entries)
        {
            if (!TryGetGitHubUrl(entry.repo, out var uri))
                continue;
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length != 3 || segments[2] != "releases")
                continue;
            var path = segments[0] + "/" + segments[1];
            repositories.Add(new VulkanDriverRepository(entry.name ?? path, path, entry.compatibility));
        }
        return repositories.DistinctBy(repository => repository.GitHubPath, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static async Task<IReadOnlyList<VulkanDriverReleaseZip>> GetReleaseZipsAsync(VulkanDriverRepository repository)
    {
        const int pageSize = 100;
        var releases = new List<GitHubRelease>();
        using var client = KnUtils.GetHttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Knossos.NET");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        for (int page = 1; ; page++)
        {
            var url = $"https://api.github.com/repos/{repository.GitHubPath}/releases?per_page={pageSize}&page={page}";
            using var response = await client.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var batch = JsonSerializer.Deserialize<GitHubRelease[]>(json)
                ?? throw new InvalidDataException("GitHub returned an invalid release list.");
            releases.AddRange(batch);
            if (batch.Length < pageSize)
                break;
        }

        var zips = new List<VulkanDriverReleaseZip>();
        foreach (var release in releases.Where(release => !release.draft)
                     .OrderByDescending(release => ParseDate(release.published_at ?? release.created_at)))
        {
            foreach (var asset in release.assets ?? Array.Empty<GitHubReleaseAsset>())
            {
                if (asset.name is not { } name || !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                    !TryGetGitHubUrl(asset.browser_download_url, out var uri))
                    continue;

                zips.Add(new VulkanDriverReleaseZip(release.name ?? release.tag_name ?? name, name,
                    uri.AbsoluteUri, asset.digest));
            }
        }
        return zips;
    }

    public static async Task DownloadAsync(VulkanDriverReleaseZip zip, VulkanDriverStore store)
    {
        using var client = KnUtils.GetHttpClient();
        using var response = await client.GetAsync(zip.DownloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await store.InstallAsync(stream, zip.ZipName, zip.DownloadUrl, zip.Digest).ConfigureAwait(false);
    }

    private static DateTimeOffset ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, out var date) ? date : DateTimeOffset.MinValue;

    private static bool TryGetGitHubUrl(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) &&
            parsed.Scheme == Uri.UriSchemeHttps && parsed.Host == "github.com")
        {
            uri = parsed;
            return true;
        }
        uri = null!;
        return false;
    }

    private sealed class RepositoryEntry
    {
        public string? repo { get; set; }
        public string? name { get; set; }
        public string? compatibility { get; set; }
    }
}
