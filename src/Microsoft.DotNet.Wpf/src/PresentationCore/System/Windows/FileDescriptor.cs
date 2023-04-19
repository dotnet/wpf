// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace System.Windows
{
    public class FileGroup : IReadOnlyList<KeyValuePair<FileDescriptor, Stream>>
    {
        private List<FileDescriptor> _fileDescriptors;
        private List<Stream> _streams;
        private IDataObjectWithIndex _dataObject;

        public FileGroup()
        {
            _fileDescriptors = new List<FileDescriptor>();
            _streams = new List<Stream>();
        }
        internal FileGroup(IDataObjectWithIndex dataObject, IEnumerable<FileDescriptor> fileDescriptors)
        {
            if (dataObject == null)
            {
                throw new ArgumentNullException(nameof(dataObject));
            }

            _dataObject = dataObject;
            _fileDescriptors = new List<FileDescriptor>(fileDescriptors);
        }

        public int Count
        {
            get { return _fileDescriptors.Count; }
        }
        public bool IsReadOnly
        {
            get { return _streams == null; }
        }

        public IReadOnlyList<FileDescriptor> FileDescriptors
        {
            get { return _fileDescriptors.AsReadOnly(); }
        }

        public void Add(string filename, byte[] data)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Add(new FileDescriptor(filename), new MemoryStream(data));
        }
        public void Add(string filename, Stream stream)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            Add(new FileDescriptor(filename), stream);
        }
        public void Add(FileDescriptor descriptor, Stream stream)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            // stream can be null for directories

            if (IsReadOnly)
            {
                throw new InvalidOperationException(); // TODO: SR
            }

            _fileDescriptors.Add(descriptor);
            _streams.Add(stream);
        }

        public Stream GetFileContents(FileDescriptor descriptor)
        {
            int index = _fileDescriptors.IndexOf(descriptor);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }

            return GetFileContents(index);
        }
        public Stream GetFileContents(int index)
        {
            if (index < 0 || index >= _fileDescriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (_dataObject != null)
            {
                if (_fileDescriptors[index].IsDirectory)
                {
                    return null;
                }

                object data = null;
                try
                {
                    data = _dataObject.GetData(DataFormats.FileContents, false, index);
                }
                catch (NotImplementedException) { } // e.g. folder contents

                if (data is Stream stream)
                {
                    return stream;
                }
                else if (data is byte[] buffer)
                {
                    return new MemoryStream(buffer);
                }
                else
                {
                    return null; // if we throw here, the enumerator will throw unrecoverably
                }
            }
            else
            {
                return _streams[index];
            }
        }

        public IEnumerator<KeyValuePair<FileDescriptor, Stream>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return KeyValuePair.Create(_fileDescriptors[i], GetFileContents(i));
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        KeyValuePair<FileDescriptor, Stream> IReadOnlyList<KeyValuePair<FileDescriptor, Stream>>.this[int index]
        {
            get { return KeyValuePair.Create(_fileDescriptors[index], GetFileContents(index)); }
        }
    }

    public class FileDescriptor
    {
        public Guid? Clsid { get; set; }
        public Int32Rect? Icon { get; set; }
        public FileAttributes? FileAttributes { get; set; }
        public DateTime? CreationTime { get; set; }
        public DateTime? LastAccessTime { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public long? FileSize { get; set; }
        public string FileName { get; }

        public bool IsDirectory
        {
            get { return (FileAttributes.GetValueOrDefault() & IO.FileAttributes.Directory) != 0; }
        }

        public FileDescriptor(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            FileName = filename;
        }

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
