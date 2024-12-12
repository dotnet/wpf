// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Win32.PresentationCore;
using System.Text;
using Windows.Win32.Foundation;

namespace System.Windows.Media.Imaging
{
    #region BitmapCodecInfo

    /// <summary>
    /// Codec info for a given Encoder/Decoder
    /// </summary>
    public abstract class BitmapCodecInfo
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        protected BitmapCodecInfo()
        {
        }

        /// <summary>
        /// Internal Constructor
        /// </summary>
        internal BitmapCodecInfo(SafeMILHandle codecInfoHandle)
        {
            Debug.Assert(codecInfoHandle != null);
            _isBuiltIn = true;
            _codecInfoHandle = codecInfoHandle;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Container format
        /// </summary>
        public virtual Guid ContainerFormat
        {
            get
            {
                EnsureBuiltIn();

                UnsafeNativeMethods.WICBitmapCodecInfo.GetContainerFormat(
                    _codecInfoHandle,
                    out Guid containerFormat).ThrowOnFailureExtended();

                return containerFormat;
            }
        }

        /// <summary>
        /// Author
        /// </summary>
        public virtual string Author
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder author = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICComponentInfo.GetAuthor(
                    _codecInfoHandle,
                    0,
                    author,
                    out uint length).ThrowOnFailureExtended();

                // Get the string back
                if (length > 0)
                {
                    author = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICComponentInfo.GetAuthor(
                        _codecInfoHandle,
                        length,
                        author,
                        out _).ThrowOnFailureExtended();
                }

                return author?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        ///  Version
        /// </summary>
        public virtual Version Version
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder version = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICComponentInfo.GetVersion(
                    _codecInfoHandle,
                    0,
                    version,
                    out uint length).ThrowOnFailureExtended();

                if (length > 0)
                {
                    // Get the string back
                    version = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICComponentInfo.GetVersion(
                        _codecInfoHandle,
                        length,
                        version,
                        out _).ThrowOnFailureExtended();
                }

                return version is not null ? new Version(version.ToString()) : new Version();
            }
        }

        /// <summary>
        ///  Spec Version
        /// </summary>
        public virtual Version SpecificationVersion
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder specVersion = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICComponentInfo.GetSpecVersion(
                    _codecInfoHandle,
                    0,
                    specVersion,
                    out uint length).ThrowOnFailureExtended();

                // Get the string back
                if (length > 0)
                {
                    specVersion = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICComponentInfo.GetSpecVersion(
                        _codecInfoHandle,
                        length,
                        specVersion,
                        out _).ThrowOnFailureExtended();
                }

                return specVersion is not null ? new Version(specVersion.ToString()) : new Version();
            }
        }

        /// <summary>
        /// Friendly Name
        /// </summary>
        public virtual string FriendlyName
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder friendlyName = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICComponentInfo.GetFriendlyName(
                    _codecInfoHandle,
                    0,
                    friendlyName,
                    out uint length).ThrowOnFailureExtended();

                Debug.Assert(length >= 0);

                // Get the string back
                if (length > 0)
                {
                    friendlyName = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICComponentInfo.GetFriendlyName(
                        _codecInfoHandle,
                        length,
                        friendlyName,
                        out length
                        ).ThrowOnFailureExtended();
                }

                return friendlyName?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Device Manufacturer
        /// </summary>
        public virtual string DeviceManufacturer
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder deviceManufacturer = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICBitmapCodecInfo.GetDeviceManufacturer(
                    _codecInfoHandle,
                    0,
                    deviceManufacturer,
                    out uint length).ThrowOnFailureExtended();

                // Get the string back
                if (length > 0)
                {
                    deviceManufacturer = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICBitmapCodecInfo.GetDeviceManufacturer(
                        _codecInfoHandle,
                        length,
                        deviceManufacturer,
                        out length).ThrowOnFailureExtended();
                }

                return deviceManufacturer?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Device Models
        /// </summary>
        public virtual string DeviceModels
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder deviceModels = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICBitmapCodecInfo.GetDeviceModels(
                    _codecInfoHandle,
                    0,
                    deviceModels,
                    out uint length).ThrowOnFailureExtended();

                // Get the string back
                if (length > 0)
                {
                    deviceModels = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICBitmapCodecInfo.GetDeviceModels(
                        _codecInfoHandle,
                        length,
                        deviceModels,
                        out _).ThrowOnFailureExtended();
                }

                return deviceModels?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Mime types
        /// </summary>
        public virtual string MimeTypes
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder mimeTypes = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICBitmapCodecInfo.GetMimeTypes(
                    _codecInfoHandle,
                    0,
                    mimeTypes,
                    out uint length).ThrowOnFailureExtended();

                Debug.Assert(length >= 0);

                // Get the string back
                if (length > 0)
                {
                    mimeTypes = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICBitmapCodecInfo.GetMimeTypes(
                        _codecInfoHandle,
                        length,
                        mimeTypes,
                        out _).ThrowOnFailureExtended();
                }

                return mimeTypes?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// File extensions
        /// </summary>
        public virtual string FileExtensions
        {
            get
            {
                EnsureBuiltIn();

                StringBuilder fileExtensions = null;

                // Find the length of the string needed
                UnsafeNativeMethods.WICBitmapCodecInfo.GetFileExtensions(
                    _codecInfoHandle,
                    0,
                    fileExtensions,
                    out uint length).ThrowOnFailureExtended();

                // Get the string back
                if (length > 0)
                {
                    fileExtensions = new StringBuilder((int)length);

                    UnsafeNativeMethods.WICBitmapCodecInfo.GetFileExtensions(
                        _codecInfoHandle,
                        length,
                        fileExtensions,
                        out _).ThrowOnFailureExtended();
                }

                return fileExtensions?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Does Support Animation
        /// </summary>
        public virtual bool SupportsAnimation
        {
            get
            {
                EnsureBuiltIn();

                UnsafeNativeMethods.WICBitmapCodecInfo.DoesSupportAnimation(
                    _codecInfoHandle,
                    out bool supportsAnimation).ThrowOnFailureExtended();

                return supportsAnimation;
            }
        }

        /// <summary>
        /// Does Support Lossless
        /// </summary>
        public virtual bool SupportsLossless
        {
            get
            {
                EnsureBuiltIn();

                UnsafeNativeMethods.WICBitmapCodecInfo.DoesSupportLossless(
                    _codecInfoHandle,
                    out bool supportsLossless).ThrowOnFailureExtended();

                return supportsLossless;
            }
        }

        /// <summary>
        /// Does Support Multiple Frames
        /// </summary>
        public virtual bool SupportsMultipleFrames
        {
            get
            {
                EnsureBuiltIn();

                UnsafeNativeMethods.WICBitmapCodecInfo.DoesSupportMultiframe(
                    _codecInfoHandle,
                    out bool supportsMultiFrame).ThrowOnFailureExtended();

                return supportsMultiFrame;
            }
        }

        #endregion

        #region Private Methods

        private void EnsureBuiltIn()
        {
            if (!_isBuiltIn)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Data Members

        /// is this a built in codec info?
        private bool _isBuiltIn;

        /// Codec info handle
        SafeMILHandle _codecInfoHandle;

        #endregion
    }

    #endregion
}
