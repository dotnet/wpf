// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

namespace System.Windows.Media.Imaging
{
    #region BitmapMetadataBlob

    /// <summary>
    /// BitmapMetadataBlob class
    /// </summary>
    public class BitmapMetadataBlob
    {
        /// <summary>
        ///
        /// </summary>
        public BitmapMetadataBlob(byte[] blob)
        {
            _blob = blob;
        }

        /// <summary>
        ///
        /// </summary>
        public byte[] GetBlobValue()
        {
            return (byte[]) _blob.Clone();
        }

        /// <summary>
        ///
        /// </summary>
        internal byte[] InternalGetBlobValue()
        {
            return  _blob;
        }

        private byte[] _blob;
    }

    #endregion
}
