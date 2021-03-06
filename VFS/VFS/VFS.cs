﻿// ------------------------------------------------------------------------
// VFS.cs written by Code A Software (http://www.code-a-software.net)
// Created on:      07.01.2017
// Last update on:  09.07.2017
// ------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VFS.Interfaces;
using VFS.Storage;

namespace VFS
{
    /// <summary>
    /// Represents the base of a virtual file system
    /// </summary>
    public abstract class VFS
    {
        /// <summary>
        /// The unique directory index, e.g. if the name of to directories are the same (but different path)
        /// </summary>
        public static int DirIndex = 0;

        /// <summary>
        /// Root Directory - Name: ""
        /// </summary>
        public abstract IDirectory RootDirectory
        {
            get;
        }

        /// <summary>
        /// A file which doesn't relay to somenthing, just to use some methods which aren't static anymore (Since IFile and IDirectory-Interfaces)
        /// </summary>
        public abstract IFile NULLFILE
        {
            get;
        }

        /// <summary>
        /// The access to the real filesystem (for making VFS .Net-Core compatible)
        /// </summary>
        protected IStorage storage = null;

        /// <summary>
        /// VFS Handle for progress
        /// This is necessary when using vfs instances in other vfs instance - e.g. CryptoVFS
        /// </summary>
        public VFS Handle { get; set; }

        /// <summary>
        /// The path where the VFS-file is stored
        /// </summary>
        protected IFilePath saveFile = null;

        /// <summary>
        /// The stopwatch to measure the elapsed time
        /// </summary>
        public Stopwatch VStopWatch = new Stopwatch();

        public VFS(IStorage storageImplementation)
        {
            this.storage = storageImplementation;
        }

        /// <summary>
        /// Formatting the path to the right format
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            if (path[0] == '\\')
            {
                string nPath = string.Empty;
                for (int i = 1; i <= path.Length - 1; i++)
                    nPath += path[i];

                return nPath;
            }
            else
                return path;
        }


        /// <summary>
        /// Creates a new VFS
        /// </summary>
        /// <param name="directory">All files and folders in this directory are used</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> Create(IDirectoryPath directory);

        /// <summary>
        /// Creates a new VFS
        /// </summary>
        /// <param name="files">Files which will be processed</param>
        /// <param name="directories">Directories which will be processed</param>
        public abstract Task<Result<bool>> Create(IFilePath[] files, IDirectoryPath[] directories);

        /// <summary>
        /// Loads a VHP-File into the RAM (just header-content)
        /// </summary>
        /// <param name="filePath">The path where the vhp-file is stored</param>
        public abstract Task<Result<bool>> Read(IFilePath file);

        /// <summary>
        /// Returns true if a file is already existing
        /// </summary>
        /// <param name="path">Path of the virtual file</param>
        /// <param name="startNode">The directory where the path is beginning</param>
        /// <returns></returns>
        public virtual bool FileExists(string path, IDirectory startNode)
        {
            if (!path.Contains(@"\"))
                return NULLFILE.ByPath(startNode.GetFiles(), path) != null;
            else
            {
                IDirectory currentNode = startNode;
                string[] segments = path.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i <= segments.Length - 2; i++)
                {
                    string currentSegment = segments[i];

                    if (currentNode.Contains(currentSegment))
                        currentNode = currentNode.GetSubDirectories()[currentNode.IndexOf(currentSegment)];
                    else
                        return false;
                }
                return NULLFILE.ByPath(currentNode.GetFiles(), currentNode.ToFullPath() + @"\" + segments[segments.Length - 1]) != null;
            }
        }

        /// <summary>
        /// Removes a virtual file
        /// </summary>
        /// <param name="path">Path of the virtual file</param>
        /// <param name="startNode">The directory where the path is beginning</param>
        /// <returns></returns>
        public virtual async Task<Result<bool>> RemoveFile(string path, IDirectory startNode)
        {
            Func<Result<bool>> work = new Func<Result<bool>>(delegate {
                // Search suitable Directory instance and check if the file is there and then delete it.
                if (!path.Contains(@"\"))
                {
                    string nPath = startNode.ToFullPath() + @"\" + path;
                    if (NULLFILE.Contains(startNode.GetFiles(), nPath))
                    {
                        IFile currentFile = NULLFILE.ByPath(startNode.GetFiles(), nPath);
                        if (currentFile != null)
                        {
                            startNode.GetFiles().Remove(currentFile);
                            return new Result<bool>(true, true, null);
                        }
                        else
                            return new Result<bool>(false, false, null);
                    }
                    else
                        return new Result<bool>(false, false, null);
                }

                string[] segments = path.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                IDirectory currentNode = startNode;

                for (int i = 0; i <= segments.Length - 2; i++)
                {
                    if (currentNode.Contains(segments[i]))
                        currentNode = currentNode.GetSubDirectories()[currentNode.IndexOf(segments[i])];
                    else
                        return new Result<bool>(false, false, null);
                }

                if (NULLFILE.Contains(currentNode.GetFiles(), path))
                {
                    IFile currentFile = NULLFILE.ByPath(currentNode.GetFiles(), path);
                    if (currentFile != null)
                    {
                        currentNode.GetFiles().Remove(currentFile);
                        return new Result<bool>(true, true, null);
                    }
                    else
                        return new Result<bool>(false, false, null);
                }
                else
                    return new Result<bool>(false, false, null);
            });

            return await Task.Run(() => work.Invoke());
        }

        /// <summary>
        /// Extract all files and directories to the given path
        /// </summary>
        /// <param name="filePath">Path where the content will be extracted</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> Extract(IDirectoryPath targetDir);

        /// <summary>
        /// Extracts the given files to the given path
        /// </summary>
        /// <param name="files">The files which should be extracted</param>
        /// <param name="directoryPath">Path where the files will be extracted</param>
        public abstract Task<Result<bool>> ExtractFiles(string[] files, IDirectoryPath directoryPath);

        /// <summary>
        /// Extracts the given files to the given path
        /// </summary>
        /// <param name="files">The files which should be extracted</param>
        /// <param name="directoryPath">Path where the files will be extracted</param>
        public abstract Task<Result<bool>> ExtractFiles(IFile[] files, IDirectoryPath directoryPath);

        /// <summary>
        /// Extracts the given directory to the given path
        /// </summary>
        /// <param name="currentDir">The virutal directory which should be extracted</param>
        /// <param name="toPath">Path where the virtual directory will be extracted</param>
        public abstract Task<Result<bool>> ExtractDirectory(IDirectory currentDir, IDirectoryPath directoryPath);

        /// <summary>
        /// Extract the directory to a given path
        /// </summary>
        /// <param name="path">The virutal path where the directory is stored</param>
        /// <param name="filePath">Path where the directory will be extracted</param>
        public abstract Task<Result<bool>> ExtractDirectory(string path, IDirectoryPath directoryPath);

        /// <summary>
        /// Returns the content of a virtual file as a string (Max: 1 GB)
        /// </summary>
        /// <param name="path">Path of the virtual file</param>
        /// <param name="startNode">The directory where the path is beginning</param>
        /// <returns></returns>
        public abstract Task<Result<string>> ReadAllText(string path, IDirectory startNode);

        /// <summary>
        /// Returns the bytes of a virutal files (reading from originial file) (Max: 1 GB)
        /// </summary>
        /// <param name="path">Path of the virtual file</param>
        /// <param name="startNode">The directory where the path is beginning</param>
        /// <param name="different">Just to differniate between these methods (not used in this method)</param>
        /// <returns></returns>
        public abstract Task<Result<byte[]>> ReadAllBytes(string path, IDirectory startNode, bool different = false);

        /// <summary>
        /// Writes bytes into a file in the workspace directory (while saving the file will be saved too)
        /// </summary>
        /// <param name="data">The bytes to write</param>
        /// <param name="name">The name of the file</param>
        /// <param name="dir">The directory where the file is stored in</param>
        /// <param name="overrideExisting">Determines if the file will be replace if the file is already exisiting</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> WriteAllBytes(byte[] data, string name, IDirectory dir, bool overrideExisting = false);

        /// <summary>
        /// Writes bytes into a file in the workspace directory (while saving the file will be saved too)
        /// </summary>
        /// <param name="data">The bytes to write</param>
        /// <param name="path">The path where the file is stored in</param>
        /// <param name="overrideExisting">Determines if the file will be replace if the file is already exisiting</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> WriteAllBytes(byte[] data, string path, bool overrideExisting = false);

        /// <summary>
        /// Writes bytes into a file in the workspace directory (while saving the file will be saved too)
        /// </summary>
        /// <param name="content">The string which should be written into the file</param>
        /// <param name="name">The name of the file</param>
        /// <param name="dir">The directory where the file is stored in</param>
        /// <param name="overrideExisting">Determines if the file will be replace if the file is already exisiting</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> WriteAllText(string content, string name, IDirectory dir, bool overrideExisting = false);

        /// <summary>
        /// Writes a stream (file) into a file in the workspace directory (while saving the file will be saved too)
        /// </summary>
        /// <param name="name">The name of the file</param>
        /// <param name="dir">The directory where the file is stored in</param>
        /// <param name="stream">The stream which will be written to a file stream</param>
        /// <param name="overrideExisting">Determines if the file will be replace if the file is already exisiting</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> WriteStream(string name, IDirectory dir, IStream stream, bool overrideExisting = false);

        /// <summary>
        /// Automatically called when SaveAfterChange is true.
        /// This method is for saving changes.
        /// </summary>
        /// <returns></returns>
        public abstract Task<Result<bool>> Save();

        /// <summary>
        /// Iterates all directories and searches for a special string
        /// </summary>
        /// <param name="searchString">The search value you want to find</param>
        /// <param name="startNode">The dir where you want to start the search</param>
        /// <param name="recurse">If false, just startNode is used not the sub-directories</param>
        /// <returns></returns>
        public virtual SearchResult Search(string searchString, IDirectory startNode, bool recurse)
        {
            SearchResult currentResult = new SearchResult();
            List<IDirectory> lstDirs = new List<IDirectory>();
            List<IFile> lstFiles = new List<IFile>();

            if (!recurse)
            {
                foreach (IFile currentFile in startNode.GetFiles())
                {
                    if (currentFile.GetName().ToLower().Contains(searchString.ToLower()))
                        lstFiles.Add(currentFile);
                }

                currentResult.Directories = lstDirs.ToArray();
                currentResult.Files = lstFiles.ToArray();
                return currentResult;
            }

            Action<IDirectory> passDirs = null;

            passDirs = new Action<IDirectory>((IDirectory dir) => {

                foreach (IDirectory currentDir in dir.GetSubDirectories())
                {
                    if (currentDir.GetName().ToLower().Contains(searchString.ToLower()))
                        lstDirs.Add(currentDir);

                    foreach (IFile currentFile in currentDir.GetFiles())
                    {
                       if (currentFile.GetName().ToLower().Contains(searchString.ToLower()))
                            lstFiles.Add(currentFile);
                    }

                    passDirs(currentDir);
                }

            });
            passDirs(startNode);

            foreach (IFile currentFile in startNode.GetFiles())
            {
                if (currentFile.GetName().ToLower().Contains(searchString.ToLower()))
                    lstFiles.Add(currentFile);
            }

            currentResult.Directories = lstDirs.ToArray();
            currentResult.Files = lstFiles.ToArray();
            return currentResult;
        }
    }
}
