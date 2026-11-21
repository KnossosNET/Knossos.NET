using System.Text;

namespace VP.NET
{
    public struct VPHeader
    {
        public string header; //VPVP
        public int version; //2 = Compression off, 3 = Compression on
        public int indexOffset; //16 for empty file
        public int numberEntries;
    }

    public struct VPIndexEntry
    {
        public string name; //32 with null terminator
        public byte[]? nameBytes; //keep the original 32 bytes
        public int timestamp; //unix
        public int size;
        public int offset; //from start of the file
    }

    public class VPContainer
    {
        public List<VPFile> vpFiles = new List<VPFile>();
        public string? vpFilePath;
        public int numberFiles = 0;
        public int numberFolders = 0;
        public bool compression = false;

        /// <summary>
        /// Creates a new empty VP file
        /// </summary>
        public VPContainer()
        {
            var data = new VPFile(VPFileType.Directory, this, null, "data");
            data.files!.Add(new VPFile(VPFileType.BackDir, this, data));
            vpFiles.Add(data);
        }

        /// <summary>
        /// Reads a VP file from a path
        /// </summary>
        /// <param name="vpFilePath"></param>
        public VPContainer(string vpFilePath)
        {
            if (!File.Exists(vpFilePath))
                throw new IOException("File "+vpFilePath+" does not exist!");
            this.vpFilePath = vpFilePath;
            using var file = new FileStream(vpFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
            var header = ReadHeader(file);
            compression = header.version >= 3;
            var index = ReadIndex(file, header);
            ParseIndex(index, file);
        }

        /// <summary>
        /// Loads a VP file and cleans any previous data
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task LoadVP(string path)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(path))
                    throw new IOException("File " + path + " does not exist!");
                vpFiles.Clear();
                numberFiles = 0;
                numberFolders = 0;
                vpFilePath = path;
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
                var header = ReadHeader(file);
                compression = header.version >= 3;
                var index = ReadIndex(file, header);
                ParseIndex(index, file);
            }).ContinueWith((task) =>
            {
                if (task.IsFaulted && task.Exception != null)
                    throw task.Exception;
            });
        }

        /// <summary>
        /// Extracts the entire VP to a folder
        /// Any compressed file will be auto-decompressed
        /// </summary>
        /// <param name="destFolderPath"></param>
        /// <returns></returns>
        public async Task ExtractVpAsync(string destFolderPath, Action<string,int,int>? progressCallback = null)
        {
            if(progressCallback != null)
                progressCallback("", 0, numberFiles);
            foreach(var file in vpFiles)
            {
                await file.ExtractRecursiveAsync(destFolderPath, progressCallback);
            }
        }

        /// <summary>
        /// Saves the current VP structure in the same file name and path
        /// Any new files added will be read and saved into the vp here
        /// Any file marked for deletion will be removed
        /// </summary>
        /// <returns></returns>
        public async Task SaveAsync(Action<string, int>? progressCallback = null, CancellationTokenSource? cancelSource = null)
        { 
            if(vpFilePath == null || vpFilePath.Trim() == string.Empty)
            {
                throw new Exception("VP file path is unset, use SaveAS().");
            }
            await SaveAsAsync(vpFilePath,progressCallback, cancelSource);
        }

        /// <summary>
        /// Saves the current VP structure as a new file
        /// Any new files added will be read and saved into the vp here
        /// Any file marked for deletion will be removed
        /// </summary>
        /// <param name="destPath"></param> 
        /// <returns></returns>
        public async Task SaveAsAsync(string destPath, Action<string, int>? progressCallback = null, CancellationTokenSource? cancelSource = null)
        {
            var tempPath = destPath + ".tmp";
            int bufferSize = 8192;
            int headerSize = 16;
            VPHeader header;
            header.header = "VPVP";
            header.version = compression ? 3 : 2;

            if (progressCallback != null)
            {
                progressCallback("", numberFiles);
            } 

            var index = new List<VPIndexEntry>();

            try
            {
                using (var vp = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
                {
                    vp.Position = headerSize;
                    foreach (var file in vpFiles)
                    {
                        cancelSource?.Token.ThrowIfCancellationRequested();
                        await SaveRecursive(file, vp, bufferSize, index, progressCallback, cancelSource);
                    }

                    var token = cancelSource?.Token ?? CancellationToken.None;
                    token.ThrowIfCancellationRequested();
                    header.indexOffset = checked((int)vp.Position);
                    header.numberEntries = index.Count;
                    await WriteIndexAsync(vp, index, token);
                    vp.Position = 0;
                    await WriteHeaderAsync(vp, header, token);
                }
                cancelSource?.Token.ThrowIfCancellationRequested();
                File.Move(tempPath, destPath, overwrite: true);
                vpFilePath = destPath;
            }
            finally
            {
                File.Delete(tempPath);
            }

            /* Remove deleted files from the tree */

            DeleteRecursive(vpFiles);
        }

        /// <summary>Writes the reconstructed VP in order, so the destination can be a hashing stream.</summary>
        public static async Task WriteUncompressedAsync(string sourcePath, Stream destination, CancellationToken token = default)
        {
            using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var header = ReadHeader(source);
            if (header.version < 3 || header.indexOffset < 16 || header.numberEntries < 0 ||
                source.Length - header.indexOffset != (long)header.numberEntries * 44)
                throw new InvalidDataException("Invalid VPC header or index.");

            var entries = ReadIndex(source, header);
            var index = new List<VPIndexEntry>(entries.Count);
            var compressed = new bool[entries.Count];
            using var reader = new BinaryReader(source, Encoding.ASCII, leaveOpen: true);
            int position = 16;
            for (int i = 0; i < entries.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var entry = entries[i];
                if (entry.size < 0 || entry.size > 0 && (entry.offset < 16 || (long)entry.offset + entry.size > header.indexOffset))
                    throw new InvalidDataException("A VPC entry falls outside its data region.");
                if (entry.size > 0)
                {
                    source.Position = entry.offset;
                    compressed[i] = entry.size >= 4 && reader.ReadUInt32() == 0x31345A4C; // LZ41
                    if (compressed[i])
                    {
                        if (entry.size < 16) throw new InvalidDataException("Incomplete LZ41 entry.");
                        source.Position = (long)entry.offset + entry.size - 8;
                        entry.size = reader.ReadInt32();
                        if (entry.size <= 0) throw new InvalidDataException("Invalid uncompressed entry size.");
                    }
                    entry.offset = position;
                    position = checked(position + entry.size);
                }
                else if (entry.offset != 0) entry.offset = position;
                index.Add(entry);
            }

            header.version = 2;
            header.indexOffset = position;
            await WriteHeaderAsync(destination, header, token);
            var buffer = new byte[81920];
            for (int i = 0; i < entries.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var entry = entries[i];
                if (entry.size == 0) continue;
                source.Position = entry.offset;
                if (compressed[i])
                {
                    if (VPCompression.DecompressStream(source, destination, CompressionHeader.LZ41, entry.size, token) != index[i].size)
                        throw new InvalidDataException("Unexpected decompressed entry size.");
                    continue;
                }

                int remaining = entry.size;
                while (remaining > 0)
                {
                    int bytesRead = await source.ReadAsync(buffer.AsMemory(0, Math.Min(remaining, buffer.Length)), token);
                    if (bytesRead == 0) throw new EndOfStreamException();
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    remaining -= bytesRead;
                }
            }
            await WriteIndexAsync(destination, index, token);
        }

        private static async Task WriteHeaderAsync(Stream output, VPHeader header, CancellationToken token)
        {
            await output.WriteAsync(Encoding.ASCII.GetBytes(header.header), token);
            await output.WriteAsync(BitConverter.GetBytes(header.version), token);
            await output.WriteAsync(BitConverter.GetBytes(header.indexOffset), token);
            await output.WriteAsync(BitConverter.GetBytes(header.numberEntries), token);
        }

        private static async Task WriteIndexAsync(Stream output, List<VPIndexEntry> index, CancellationToken token)
        {
            foreach (var entry in index)
            {
                await output.WriteAsync(BitConverter.GetBytes(entry.offset), token);
                await output.WriteAsync(BitConverter.GetBytes(entry.size), token);
                var nameBytes = entry.nameBytes;
                if (nameBytes == null || Encoding.ASCII.GetString(nameBytes).Split('\0')[0] != entry.name)
                {
                    var name = entry.name.Length > 31 ? entry.name[..31] : entry.name;
                    nameBytes = Encoding.ASCII.GetBytes(name.PadRight(32, '\0'));
                }
                await output.WriteAsync(nameBytes, token);
                await output.WriteAsync(BitConverter.GetBytes(entry.timestamp), token);
            }
        }

        private void DeleteRecursive(List<VPFile> files)
        {
            foreach (var file in files.ToList())
            {
                if(file.delete)
                {
                    files.Remove(file);
                }
                else
                {
                    if(file.type == VPFileType.Directory)
                    {
                        if (file.files != null)
                        {
                            DeleteRecursive(file.files);
                        }
                    }
                }
            }
        }

        private async Task SaveRecursive(VPFile file, Stream vp, int bufferSize, List<VPIndexEntry> index, Action<string, int>? progressCallback = null, CancellationTokenSource? cancelSource = null)
        {
            if (!file.delete)
            {
                switch (file.type)
                {
                    case VPFileType.Directory:
                        file.info.offset = file.info.offset != 0 ? (int)vp.Position : 0 ;
                        index.Add(file.info);
                        foreach (var subfile in file.files!)
                        {
                            if (cancelSource != null && cancelSource.IsCancellationRequested)
                            {
                                return;
                            }
                            await SaveRecursive(subfile, vp, bufferSize, index, progressCallback, cancelSource);
                        }
                        break;
                    case VPFileType.BackDir:
                        file.info.offset = file.info.offset != 0 ? (int)vp.Position : 0;
                        index.Add(file.info);
                        break;
                    case VPFileType.File:
                        if (progressCallback != null)
                        {
                            progressCallback(file.info.name, numberFiles);
                        }
                        if (cancelSource != null && cancelSource.IsCancellationRequested)
                        {
                            return;
                        }
                        if (file.fullpath == null)
                        {
                            if (!File.Exists(vpFilePath))
                            {
                                throw new Exception("Unable to open vp file in path : " + vpFilePath);
                            }

                            using var sourceVP = new FileStream(vpFilePath,FileMode.Open,FileAccess.Read,FileShare.Read,bufferSize);
                            
                            if (!sourceVP.CanRead)
                            {
                                throw new Exception("Unable to read vp file : " + vpFilePath);
                            }

                            sourceVP.Seek(file.info.offset, SeekOrigin.Begin);

                            if (file.CheckHaveToCompress() || file.CheckHaveToDecompress())
                            {
                                //Uncompressed file in VP to Compressed
                                if (file.CheckHaveToCompress())
                                {
                                    long inputStartingPosition = sourceVP.Position;
                                    using var compressedOutput = new MemoryStream();
                                    int newSize = VPCompression.CompressStream(sourceVP, compressedOutput, file.info.size, cancelSource?.Token ?? CancellationToken.None);
                                    if (newSize >= file.info.size)
                                    {
                                        sourceVP.Position = inputStartingPosition;
                                        int leftToCopy = file.info.size;
                                        file.info.offset = (int)vp.Position;

                                        while (leftToCopy > 0)
                                        {
                                            byte[] buffer = new byte[bufferSize];
                                            int bytesToRead = leftToCopy > bufferSize ? bufferSize : leftToCopy;
                                            var bytesRead = await sourceVP.ReadAsync(buffer, 0, bytesToRead);
                                            if(bytesRead != bytesToRead)
                                            {
                                                throw new IOException("Unable to read from file");
                                            }
                                            leftToCopy -= bytesRead;
                                            await vp.WriteAsync(buffer, 0, bytesRead);
                                        }
                                    }
                                    else
                                    {
                                        file.info.offset = (int)vp.Position;
                                        file.compressionInfo.header = CompressionHeader.LZ41;
                                        file.compressionInfo.uncompressedFileSize = file.info.size;
                                        file.info.size = newSize;
                                        compressedOutput.Seek(0, SeekOrigin.Begin);
                                        await compressedOutput.CopyToAsync(vp);
                                    }
                                }
                                else
                                {
                                    //Compressed file in VP to Uncompressed
                                    file.info.offset = (int)vp.Position;
                                    file.compressionInfo.header = null;
                                    file.compressionInfo.uncompressedFileSize = null;
                                    file.info.size = VPCompression.DecompressStream(sourceVP, vp, CompressionHeader.LZ41, file.info.size, cancelSource?.Token ?? CancellationToken.None);
                                }
                            }
                            else
                            {
                                int leftToCopy = file.info.size;
                                file.info.offset = (int)vp.Position;

                                while (leftToCopy > 0)
                                {
                                    byte[] buffer = new byte[bufferSize];
                                    int bytesToRead = leftToCopy > bufferSize ? bufferSize : leftToCopy;
                                    var bytesRead = await sourceVP.ReadAsync(buffer, 0, bytesToRead);
                                    if (bytesRead != bytesToRead)
                                    {
                                        throw new IOException("Unable to read from file");
                                    }
                                    leftToCopy -= bytesRead;
                                    await vp.WriteAsync(buffer, 0, bytesRead);
                                }
                            }
                            sourceVP.Dispose();
                            index.Add(file.info);
                        }
                        else
                        {
                            if (!File.Exists(file.fullpath))
                            {
                                throw new Exception("Unable to open file " + file.fullpath + " to add it to the vp.");
                            }

                            using var source = new FileStream(file.fullpath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize);

                            if (!source.CanRead)
                            {
                                throw new Exception("Unable to open file " + file.fullpath + " to add it to the vp.");
                            }

                            file.info.offset = (int)vp.Position;
                            file.info.size = (int)source.Length;

                            if (file.CheckHaveToCompress())
                            {
                                using var compressedOutput = new MemoryStream();
                                int newSize = VPCompression.CompressStream(source, compressedOutput, cancellationToken: cancelSource?.Token ?? CancellationToken.None);
                                if (newSize >= source.Length)
                                {
                                    source.Seek(0, SeekOrigin.Begin);
                                    await source.CopyToAsync(vp);
                                    file.info.size = (int)source.Position;
                                    file.compressionInfo.header = null;
                                    file.compressionInfo.uncompressedFileSize = null;
                                }
                                else
                                {
                                    compressedOutput.Seek(0, SeekOrigin.Begin);
                                    await compressedOutput.CopyToAsync(vp);
                                    file.info.size = newSize;
                                    file.compressionInfo.header = CompressionHeader.LZ41;
                                    file.compressionInfo.uncompressedFileSize = (int)source.Position;
                                }
                            }
                            else
                            {
                                await source.CopyToAsync(vp);
                                file.info.size = (int)source.Position;
                            }

                            source.Dispose();

                            index.Add(file.info);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Adds the contents of a folder to the root of the vp
        /// The data folder should be inside of this path
        /// Ex: folderPath\\data\\*.*
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>-
        public void AddFolderToRoot(string folderPath)
        {
            var di = new DirectoryInfo(folderPath);
            var folders = di.GetDirectories();
            var files = di.GetFiles();
            if (folders != null)
            {
                foreach (DirectoryInfo dir in folders)
                {
                    var exists = vpFiles.FirstOrDefault(f => f.info!.name == dir.Name && f.type == VPFileType.Directory);
                    if (exists != null)
                    {
                        exists.AddDirectoryRecursive(dir.FullName);
                    }
                    else
                    {
                        var folder = new VPFile(VPFileType.Directory, this, null, dir.Name);
                        folder.files!.Add(new VPFile(VPFileType.BackDir, this, folder));
                        VPFile.InsertInOrder(vpFiles, folder);
                        folder.AddDirectoryRecursive(dir.FullName);
                    }
                }
            }
            if (files != null)
            {
                foreach (var file in files)
                {
                    var exists = vpFiles.FirstOrDefault(x => x.info!.name == file.Name && x.type == VPFileType.File);
                    if (exists != null)
                    {
                        exists.Delete();
                    }
                    var nf = new VPFile(VPFileType.File, this, null, file.Name, 0, 0, VPTime.GetTimestampFromFile(file.FullName), file.FullName);
                    VPFile.InsertInOrder(vpFiles, nf);
                }
            }
        }

        /// <summary>
        /// Enables VP Compression
        /// If compression is enabled all new files added to the vp will be compressed if possible.
        /// Old files on the vp are unaffected.
        /// </summary>
        public void EnableCompression()
        {
            compression = true;
        }

        /// <summary>
        /// Disables VP Compression
        /// If compression is disabled no compression will be done to new files added to the vp.
        /// Old files on the vp are unaffected.
        /// </summary>
        public void DisableCompression()
        {
            compression = false;
        }

        private static VPHeader ReadHeader(Stream file)
        {
            VPHeader header;
            try
            {
                file.Seek(0, SeekOrigin.Begin);
                BinaryReader br = new BinaryReader(file);
                header.header = Encoding.ASCII.GetString(br.ReadBytes(4));
                if(header.header != "VPVP")
                {
                    throw new Exception("This is not a valid VP file: Header mismatch!");
                }
                header.version = br.ReadInt32();
                header.indexOffset = br.ReadInt32();
                header.numberEntries = br.ReadInt32();
            }catch (Exception ex)
            {
                throw new Exception("An error has ocurred while reading the VP file header: " + ex.Message);
            }
            return header;
        }

        private static List<VPIndexEntry> ReadIndex(Stream file, VPHeader vpHeader)
        {
            var vpIndex = new List<VPIndexEntry>();
            if (vpHeader.indexOffset < 16)
            {
                throw new Exception("An error has ocurred while reading the VP file index: The index offset had a value below 16.");
            }
            try
            {
                file.Seek(vpHeader.indexOffset, SeekOrigin.Begin);
                BinaryReader br = new BinaryReader(file);

                while (file.Position < file.Length)
                {
                    var entry = new VPIndexEntry();
                    entry.offset = br.ReadInt32();
                    entry.size = br.ReadInt32();
                    entry.nameBytes = br.ReadBytes(32);
                    entry.name = Encoding.ASCII.GetString(entry.nameBytes).Split('\0')[0];
                    entry.timestamp = br.ReadInt32();
                    vpIndex.Add(entry);
                }
            }catch (Exception ex) 
            { 
                throw new Exception("An error has ocurred while reading the VP file index: " + ex.Message); 
            }
            return vpIndex;
        }

        private void ParseIndex(List<VPIndexEntry> vpIndex, Stream vpStream)
        {
            VPFile? currentNode = null;
            foreach(var entry in vpIndex)
            {
                //Entering new directory
                if (entry.size == 0 && entry.name != "..")
                {
                    var vpfile = new VPFile(VPFileType.Directory, this, currentNode);
                    vpfile.info = entry;
                    if(currentNode == null)
                    {
                        vpFiles.Add(vpfile);
                    }
                    else
                    {
                        currentNode.files!.Add(vpfile);
                    }
                    currentNode = vpfile;
                }
                //Adding a file to directory
                if(entry.size > 0)
                {
                    var vpfile = new VPFile(VPFileType.File, this, currentNode);
                    vpfile.info = entry;

                    //Check if the file is compressed
                    vpStream.Seek(entry.offset,SeekOrigin.Begin);
                    BinaryReader br = new BinaryReader(vpStream);
                    var header = Encoding.ASCII.GetString(br.ReadBytes(4));
                    if (header == "LZ41")
                    {
                        vpfile.compressionInfo.header = CompressionHeader.LZ41;
                        vpStream.Seek(entry.size-12, SeekOrigin.Current);
                        vpfile.compressionInfo.uncompressedFileSize = br.ReadInt32();
                    }

                    if (currentNode == null)
                    {
                        vpFiles.Add(vpfile);
                    }
                    else
                    {
                        currentNode.files!.Add(vpfile);
                    }
                }
                //End directory
                if(entry.size == 0 && entry.name == "..")
                {
                    var vpfile = new VPFile(VPFileType.BackDir, this, currentNode);
                    vpfile.info = entry;
                    if (currentNode == null)
                    {
                        vpFiles.Add(vpfile);
                    }
                    else
                    {
                        currentNode.files!.Add(vpfile);
                        currentNode = currentNode.parent;
                    }
                }
            }
        }
    }
}