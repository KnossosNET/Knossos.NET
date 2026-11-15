using Knossos.NET.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using VP.NET;

namespace Knossos.NET.Classes
{
    internal static class InstalledModFile
    {
        internal static string Resolve(string root, string? relativePath)
        {
            var relative = relativePath?.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(relative) || !KnUtils.IsSubPath(root, relative))
                throw new InvalidDataException("Invalid file path in the mod manifest.");
            return Path.GetFullPath(Path.Combine(root, relative));
        }

        internal static string CompressedPath(string path) =>
            path.EndsWith(".vp", StringComparison.OrdinalIgnoreCase) ? path + "c" : path + ".lz41";

        internal static string Find(string expected) =>
            File.Exists(expected) ? expected : CompressedPath(expected);

        internal static bool NeedsCompressionChange(Mod mod, bool compress, CancellationToken token)
        {
            if (mod.modSettings.isCompressed != compress) return true;
            if (compress) return false;

            // An interrupted conversion can leave compressed files without setting the flag.
            bool ContainsCompressedFiles(string folder, bool recursive, bool includeLooseFiles)
            {
                if (!Directory.Exists(folder)) return false;
                foreach (var file in Directory.EnumerateFiles(folder, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = recursive,
                    AttributesToSkip = FileAttributes.ReparsePoint
                }))
                {
                    token.ThrowIfCancellationRequested();
                    var extension = Path.GetExtension(file);
                    if (extension.Equals(".vpc", StringComparison.OrdinalIgnoreCase) ||
                        includeLooseFiles && extension.Equals(".lz41", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }

            return mod.devMode
                ? ContainsCompressedFiles(mod.fullPath, true, true)
                : ContainsCompressedFiles(mod.fullPath, false, false) ||
                    ContainsCompressedFiles(Path.Combine(mod.fullPath, "data"), true, true);
        }

        internal static void PublishRepair(string stagingFolder, string modFolder, CancellationToken token)
        {
            foreach (var file in Directory.EnumerateFiles(stagingFolder, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                token.ThrowIfCancellationRequested();
                var destination = Resolve(modFolder, Path.GetRelativePath(stagingFolder, file));
                var parent = Path.GetDirectoryName(destination)!;
                for (var folder = new DirectoryInfo(parent); folder != null &&
                    KnUtils.IsSubPath(modFolder, Path.GetRelativePath(modFolder, folder.FullName)); folder = folder.Parent)
                {
                    if (folder.Exists && (folder.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Cannot repair files through a linked directory.");
                }
                Directory.CreateDirectory(parent);
                // Replacing the directory entry leaves any other hardlinks to the old file intact.
                File.Move(file, destination, overwrite: true);
                File.Delete(CompressedPath(destination));
            }
        }

        internal static async Task<string> GetHashAsync(string actual, string expected, CancellationToken token)
        {
            using var sha = SHA256.Create();
            if (actual == expected)
            {
                using var input = File.OpenRead(actual);
                return Convert.ToHexString(await sha.ComputeHashAsync(input, token));
            }

            using var hash = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write);
            if (expected.EndsWith(".vp", StringComparison.OrdinalIgnoreCase))
            {
                await VPContainer.WriteUncompressedAsync(actual, hash, token);
            }
            else
            {
                using var input = File.OpenRead(actual);
                // An explicit header requires the decoder to reject an invalid or truncated file.
                VPCompression.DecompressStream(input, hash, CompressionHeader.LZ41, cancellationToken: token);
            }

            hash.FlushFinalBlock();
            return Convert.ToHexString(sha.Hash!);
        }
    }
}
