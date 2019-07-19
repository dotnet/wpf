// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.WindowsBase;
using System.IO;
using System.IO.Packaging;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Extensions to provide wrappers for functionality that no longer exists in System.IO.Packaging.PackagePart
    /// </summary>
    [FriendAccessAllowed]
    internal static class PackagePartExtensions
    {
        /// <summary>
        /// Gets a seekable stream from the PackagePart.
        /// <see cref="GetSeekableStream(PackagePart, FileMode, FileAccess)"/> for details.
        /// </summary>
        /// <param name="packPart"></param>
        /// <returns>A seekable stream representing the data in the PackagePart.</returns>
        internal static Stream GetSeekableStream(this PackagePart packPart)
        {
            return GetSeekableStream(packPart, FileMode.OpenOrCreate, packPart.Package.FileOpenAccess);
        }

        /// <summary>
        /// Gets a seekable stream from the PackagePart.
        /// <see cref="GetSeekableStream(PackagePart, FileMode, FileAccess)"/> for details.
        /// </summary>
        /// <param name="packPart"></param>
        /// <param name="mode">The FileMode to open the PackagePart</param>
        /// <returns>A seekable stream representing the data in the PackagePart.</returns>
        internal static Stream GetSeekableStream(this PackagePart packPart, FileMode mode)
        {
            return GetSeekableStream(packPart, mode, packPart.Package.FileOpenAccess);
        }

        /// <summary>
        /// Gets a seekable stream from the PackagePart.
        /// </summary>
        /// <remarks>
        /// In .NET Core 3.0, System.IO.Packaging was removed, in part, from WPF.  WPF now uses the implementation
        /// contained in System.IO.Packaging.dll.  This implementation has distinct differences from the .NET Framework
        /// WPF implementation.  One such difference is that the <see cref="DeflateStream"/> returned by <see cref="PackagePart.GetStream"/> calls
        /// when the <see cref="PackagePart"/> is opened read-only is not a seekable stream.  This breaks several assumptions in WPF
        /// and causes crashes when various parts of the code-base call into <see cref="Stream.Seek"/> or <see cref="Stream.Position"/>.
        /// 
        /// To fix this, we read the entire <see cref="DeflateStream"/> into a <see cref="MemoryStream"/>, allowing callers to fully seek the stream.
        /// This is, generally, what would be the case in .NET Framework.
        /// 
        /// Note that if the stream returned is seekable (the <see cref="PackagePart"/> was opened write or read-write) then we just pass the resulting
        /// stream back as we're already guaranteed it meets our needs.
        /// </remarks>
        /// <param name="packPart"></param>
        /// <param name="mode">The FileMode to open the PackagePart</param>
        /// <param name="access">The FileAccess used to open the PackagePart</param>
        /// <returns>A seekable stream representing the data in the PackagePart.</returns>
        internal static Stream GetSeekableStream(this PackagePart packPart, FileMode mode, FileAccess access)
        {
            var packStream = packPart.GetStream(mode, access);

            // If the stream returned is seekable it meets all requirements and can be used directly.
            if (packStream.CanSeek)
            {
                return packStream;
            }

            // Non-seekable streams need to be copied out into memory so they are seekable.
            using (packStream)
            {
                var seekableStream = new MemoryStream((int)packStream.Length);

                packStream.CopyTo(seekableStream);

                // Reset the stream to the beginning.  If this is not done, attempts to read the stream 
                // from the current position will fail.  E.G. XAML/XML parsing.
                seekableStream.Position = 0;

                return seekableStream;
            }
        }
    }
}
