// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Windows
{
    /// <summary>
    /// Describes the properties of a file for data transfer. 
    /// </summary>
    public class FileDescriptor
    {
        /// <summary>
        /// Gets or sets the file type identifier.
        /// </summary>
        public Guid? Clsid { get; set; }

        /// <summary>
        /// Gets or sets the screen coordinates of the file object.
        /// </summary>
        public Int32Rect? Icon { get; set; }

        /// <summary>
        /// Gets or sets the file attribute flags.
        /// </summary>
        public FileAttributes? FileAttributes { get; set; }

        /// <summary>
        /// Gets or sets the time of file creation.
        /// </summary>
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the time that the file was last accessed.
        /// </summary>
        public DateTime? LastAccessTime { get; set; }

        /// <summary>
        /// Gets or sets the time of the last write operation.
        /// </summary>
        public DateTime? LastWriteTime { get; set; }

        /// <summary>
        /// Gets or sets the file sizes, in bytes.
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets whether the file represents a directory.
        /// </summary>
        public bool IsDirectory
        {
            get { return (FileAttributes.GetValueOrDefault() & IO.FileAttributes.Directory) != 0; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FileDescriptor"/> with given file name.
        /// </summary>
        /// <param name="filename">A relative file name.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filename"/> is <see langword="null"/>.</exception>
        public FileDescriptor(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            FileName = filename;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FileDescriptor"/> representing a physical file or directory.
        /// </summary>
        /// <param name="path">A path to the file or directory.</param>
        /// <returns>an instance of <see cref="FileDescriptor"/> representing given file or directory.</returns>
        public static FileDescriptor FromFile(string path)
        {
            FileSystemInfo info;

            FileAttributes attributes = File.GetAttributes(path); // throws as necessary
            if ((attributes & IO.FileAttributes.Directory) != 0)
            {
                info = new DirectoryInfo(path);
            }
            else
            {
                info = new FileInfo(path);
            }

            return FromFile(info);
        }

        private static FileDescriptor FromFile(FileSystemInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            FileDescriptor descriptor = new FileDescriptor(info.Name)
            {
                FileAttributes = info.Attributes,
                CreationTime = info.CreationTime,
                LastAccessTime = info.LastAccessTime,
                LastWriteTime = info.LastWriteTime,
            };

            if (info is FileInfo fileInfo)
            {
                descriptor.FileSize = fileInfo.Length;
            }

            return descriptor;
        }
    }
}
