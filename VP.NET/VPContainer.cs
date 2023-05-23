using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

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
            var file = new FileStream(vpFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
            var header = ReadHeader(file);
            var index = ReadIndex(file, header);
            ParseIndex(index, file);
            file.Close();
            file.Dispose();
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
                var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
                var header = ReadHeader(file);
                var index = ReadIndex(file, header);
                ParseIndex(index, file);
                file.Close();
                file.Dispose();
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

            var vp = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);

            if(!vp.CanWrite)
            {
                throw new Exception("Unable to open file for writting : " + tempPath);
            }

            /* Write Files */

            vp.Seek(headerSize, SeekOrigin.Begin);

            foreach (var file in vpFiles)
            {
                if (cancelSource != null && cancelSource.IsCancellationRequested)
                {
                    break;
                }
                await SaveRecursive(file, vp, bufferSize, index, progressCallback, cancelSource);
            }

            if (cancelSource != null && cancelSource.IsCancellationRequested)
            {
                vp.Dispose();
                return;
            }

            /* Write Index */

            header.indexOffset = (int)vp.Position;
            header.numberEntries = index.Count();

            foreach (var entry in index)
            {
                var offBytes = BitConverter.GetBytes(entry.offset);
                await vp.WriteAsync(offBytes, 0, 4);

                var sizeBytes = BitConverter.GetBytes(entry.size);
                await vp.WriteAsync(sizeBytes, 0, 4);

                if (entry.nameBytes == null || Encoding.ASCII.GetString(entry.nameBytes).Split('\0')[0] != entry.name)
                {
                    var name = entry.name;
                    if (name.Length > 31)
                    {
                        name = entry.name.Substring(0, 31);
                    }
                    name = name.PadRight(32, '\0');
                    var nameBytes = Encoding.ASCII.GetBytes(name);
                    await vp.WriteAsync(nameBytes, 0, 32);
                }
                else
                {
                    await vp.WriteAsync(entry.nameBytes, 0, 32);
                }

                var timeBytes = BitConverter.GetBytes(entry.timestamp);
                await vp.WriteAsync(timeBytes, 0, 4);
            }

            /* Write Header */

            vp.Seek(0, SeekOrigin.Begin);

            var headerBytes = Encoding.ASCII.GetBytes(header.header);
            await vp.WriteAsync(headerBytes, 0, 4);

            var versionBytes = BitConverter.GetBytes(header.version);
            await vp.WriteAsync(versionBytes, 0, 4);

            var offsetBytes = BitConverter.GetBytes(header.indexOffset);
            await vp.WriteAsync(offsetBytes, 0, 4);

            var numberEntries = BitConverter.GetBytes(header.numberEntries);
            await vp.WriteAsync(numberEntries, 0, 4);

            /* Finish */

            vp.Close();
            vp.Dispose();
            File.Delete(destPath);
            File.Move(tempPath,destPath);
            vpFilePath = destPath;

            /* Remove deleted files from the tree */

            DeleteRecursive(vpFiles);
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

                            var sourceVP = new FileStream(vpFilePath,FileMode.Open,FileAccess.Read,FileShare.Read,bufferSize);
                            
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
                                    MemoryStream compressedOutput = new MemoryStream();
                                    int newSize = await VPCompression.CompressStream(sourceVP, compressedOutput, file.info.size);
                                    if (newSize >= file.info.size)
                                    {
                                        sourceVP.Position = inputStartingPosition;
                                        int leftToCopy = file.info.size;
                                        file.info.offset = (int)vp.Position;

                                        while (leftToCopy > 0)
                                        {
                                            byte[] buffer = new byte[bufferSize];
                                            int bytesToRead = leftToCopy > bufferSize ? bufferSize : leftToCopy;
                                            leftToCopy -= await sourceVP.ReadAsync(buffer, 0, bytesToRead);
                                            await vp.WriteAsync(buffer, 0, bytesToRead);
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
                                    compressedOutput.Dispose();
                                }
                                else
                                {
                                    //Compressed file in VP to Uncompressed
                                    file.info.offset = (int)vp.Position;
                                    file.compressionInfo.header = null;
                                    file.compressionInfo.uncompressedFileSize = null;
                                    file.info.size = await VPCompression.DecompressStream(sourceVP, vp, CompressionHeader.LZ41, file.info.size);
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
                                    leftToCopy -= await sourceVP.ReadAsync(buffer, 0, bytesToRead);
                                    await vp.WriteAsync(buffer, 0, bytesToRead);
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

                            var source = new FileStream(file.fullpath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize);

                            if (!source.CanRead)
                            {
                                throw new Exception("Unable to open file " + file.fullpath + " to add it to the vp.");
                            }

                            file.info.offset = (int)vp.Position;
                            file.info.size = (int)source.Length;

                            if (file.CheckHaveToCompress())
                            {
                                MemoryStream compressedOutput = new MemoryStream();
                                int newSize = await VPCompression.CompressStream(source, compressedOutput);
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
                                compressedOutput.Dispose();
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
                    var nf = new VPFile(VPFileType.File, this, null, file.Name, 0, 0, VPTime.GetCurrentTime(), file.FullName);
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

        private VPHeader ReadHeader(Stream file)
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
                if(header.version >= 3)
                {
                    compression = true;
                }
                else
                {
                    compression = false;
                }
                header.indexOffset = br.ReadInt32();
                header.numberEntries = br.ReadInt32();
            }catch (Exception ex)
            {
                throw new Exception("An error has ocurred while reading the VP file header: " + ex.Message);
            }
            return header;
        }

        private List<VPIndexEntry> ReadIndex(Stream file, VPHeader vpHeader)
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