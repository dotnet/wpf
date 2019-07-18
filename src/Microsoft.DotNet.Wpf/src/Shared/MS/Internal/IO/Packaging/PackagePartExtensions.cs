// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.WindowsBase;
using System.IO;
using System.IO.Packaging;

namespace MS.Internal.IO.Packaging
{
    [FriendAccessAllowed]
    internal static class PackagePartExtensions
    {
        internal static Stream GetSeekableStream(this PackagePart packPart)
        {
            return GetSeekableStream(packPart, FileMode.OpenOrCreate, packPart.Package.FileOpenAccess);
        }

        internal static Stream GetSeekableStream(this PackagePart packPart, FileMode mode)
        {
            return GetSeekableStream(packPart, mode, packPart.Package.FileOpenAccess);
        }

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

                return seekableStream;
            }
        }
    }
}
