// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;
using System.Collections;

using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    /// <summary>
    /// Represents a group of files for data transfer.
    /// </summary>
    public class FileGroup : IReadOnlyList<KeyValuePair<FileDescriptor, Stream>>
    {
        private List<FileDescriptor> _fileDescriptors;
        private List<Stream> _streams;
        private IDataObjectWithIndex _dataObject;

        /// <summary>
        /// Initializes a new, modifiable instance of <see cref="FileGroup"/> class.
        /// </summary>
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

        /// <summary>
        /// Gets the number of files in the file group.
        /// </summary>
        public int Count
        {
            get { return _fileDescriptors.Count; }
        }

        /// <summary>
        /// Gets whether the file group is read only.
        /// </summary>
        /// <remarks>
        /// Received file groups cannot be modified.
        /// </remarks>
        public bool IsReadOnly
        {
            get { return _streams == null; }
        }

        /// <summary>
        /// Gets a collection of file descriptors in the file group.
        /// </summary>
        public IReadOnlyList<FileDescriptor> FileDescriptors
        {
            get { return _fileDescriptors.AsReadOnly(); }
        }

        /// <summary>
        /// Adds a file to the file group.
        /// </summary>
        /// <param name="filename">A relative file name.</param>
        /// <param name="data">File contents.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filename"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The file group is read only.</exception>
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

        /// <summary>
        /// Adds a file to the file group.
        /// </summary>
        /// <param name="filename">A relative file name.</param>
        /// <param name="stream">File contents.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filename"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The file group is read only.</exception>
        public void Add(string filename, Stream stream)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Add(new FileDescriptor(filename), stream);
        }

        /// <summary>
        /// Adds a file or folder to the file group.
        /// </summary>
        /// <param name="descriptor">A file descriptor identifying the file contents.</param>
        /// <param name="stream">File contents. This should be <see langword="null"/> for folders.</param>
        /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The file group is read only.</exception>
        public void Add(FileDescriptor descriptor, Stream stream)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            // stream can be null for directories

            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.FileGroup_ReadOnly);
            }

            _fileDescriptors.Add(descriptor);
            _streams.Add(stream);
        }

        /// <summary>
        /// Gets a file content associated with a file in the file group.
        /// </summary>
        /// <param name="descriptor">A file descriptor identifying the file contents to receive. The file descriptor must come from this file group.</param>
        /// <returns>a <see cref="Stream"/> with the requested file content, or, <see langword="null"/> if the file does not have content.</returns>
        /// <exception cref="KeyNotFoundException">The <paramref name="descriptor"/> is not found in the file group.</exception>
        public Stream GetFileContents(FileDescriptor descriptor)
        {
            int index = _fileDescriptors.IndexOf(descriptor);
            if (index < 0)
            {
                throw new KeyNotFoundException(SR.FileGroup_DescriptorNotFound);
            }

            return GetFileContents(index);
        }

        /// <summary>
        /// Gets a file content associated with a file in the file group.
        /// </summary>
        /// <param name="index">Index of the file content to receive.</param>
        /// <returns>a <see cref="Stream"/> with the requested file content, or, <see langword="null"/> if the file does not have content.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</exception>
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

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="FileGroup"/>.
        /// </summary>
        /// <returns>an enumerator for the <see cref="FileGroup"/>.</returns>
        /// <remarks>Enumerating the file group causes the file contents to be retrieved.</remarks>
        public IEnumerator<KeyValuePair<FileDescriptor, Stream>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return KeyValuePair.Create(_fileDescriptors[i], GetFileContents(i));
            }
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets a file descriptor and contents at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the file to get.</param>
        /// <returns>a pair of file descriptor and its content if present.</returns>
        KeyValuePair<FileDescriptor, Stream> IReadOnlyList<KeyValuePair<FileDescriptor, Stream>>.this[int index]
        {
            get { return KeyValuePair.Create(_fileDescriptors[index], GetFileContents(index)); }
        }
    }
}
