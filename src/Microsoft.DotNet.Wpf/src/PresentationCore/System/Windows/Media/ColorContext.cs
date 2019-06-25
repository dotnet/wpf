// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

using System;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Resources;
using System.Security;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.IO.Packaging;
using System.Windows.Navigation;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethodsMilCoreApi = MS.Win32.PresentationCore.UnsafeNativeMethods;
using IWICCC = MS.Win32.PresentationCore.UnsafeNativeMethods.IWICColorContext;

namespace System.Windows.Media
{
    /// <summary>
    /// Color Context
    /// </summary>
    public class ColorContext
    {
        #region Constructors

        /// <summary>
        /// Create a ColorContext from an unmanaged color context
        /// </summary>
        private ColorContext(SafeMILHandle colorContextHandle)
        {
            _colorContextHandle = colorContextHandle;

            //
            // For 3.* backwards compat, we aren't going to HRESULT.Check() anywhere because
            // that could introduce new exceptions. If anything fails, _colorContextHelper
            // will be invalid and we'll emulate the old failure behavior later in
            // OpenProfileStream()
            //
            
            IWICCC.WICColorContextType type;
            if (HRESULT.Failed(IWICCC.GetType(_colorContextHandle, out type)))
            {
                return;
            }

            switch (type)
            {
                case IWICCC.WICColorContextType.WICColorContextProfile:
                    uint cbProfileActual;
                    int hr = IWICCC.GetProfileBytes(_colorContextHandle, 0, null, out cbProfileActual);
                    if (HRESULT.Succeeded(hr) && cbProfileActual != 0)
                    {
                        byte[] profileData = new byte[cbProfileActual];
                        if (HRESULT.Failed(IWICCC.GetProfileBytes(
                            _colorContextHandle, cbProfileActual, profileData, out cbProfileActual))
                            )
                        {
                            return;
                        }
                        
                        FromRawBytes(profileData, (int)cbProfileActual, /* dontThrowException = */ true);
                    }

                    break;

                case IWICCC.WICColorContextType.WICColorContextExifColorSpace:
                    uint colorSpace;
                    if (HRESULT.Failed(IWICCC.GetExifColorSpace(_colorContextHandle, out colorSpace)))
                    {
                        return;
                    }

                    //
                    // From MSDN:
                    //     "1" is sRGB. We will use our built-in sRGB profile.
                    //     "2" is Adobe RGB. WIC says we should never see this because they are nonstandard and instead a 
                    //     real profile will be returned.
                    //     "3-65534" is unused.
                    //
                    // From the Exif spec:
                    //     B. Tag Relating to Color Space
                    //     ColorSpace
                    //
                    //     The color space information tag (ColorSpace) is always recorded as the color space specifier.
                    //     Normally sRGB (=1) is used to define the color space based on the PC monitor conditions and environment. If a
                    //     color space other than sRGB is used, Uncalibrated (=FFFF.H) is set. Image data recorded as Uncalibrated can be
                    //     treated as sRGB when it is converted to Flashpix. On sRGB see Annex E.
                    //     Tag = 40961 (A001.H)
                    //     Type = SHORT
                    //     Count = 1
                    //     1 = sRGB
                    //     FFFF.H = Uncalibrated
                    //
                    // So for 65535 we will return sRGB since it is acceptible rather than having an invalid ColorContext. The Exif 
                    // CC should always be the second one so the real one is given priority. Alternatively, we could ignore the
                    // uncalibrated CC but that would be a breaking change with 3.* (returning 1 instead of 2).
                    //
                    // If anything other than 1 or 65535 happens, _colorContextHelper will remain invalid and we will emulate
                    // the old crash behavior in OpenProfileStream().
                    //
                    
                    if (colorSpace == 1 || colorSpace == 65535)
                    {
                        ResourceManager resourceManager = new ResourceManager(
                            _colorProfileResources, Assembly.GetAssembly(typeof(ColorContext))
                            );
                        byte[] sRGBProfile = (byte[])resourceManager.GetObject(_sRGBProfileName);
                        // The existing ColorContext has already been initialized as Exif so we can't initialize it again
                        // and instead must create a new one.
                        using (FactoryMaker factoryMaker = new FactoryMaker())
                        {
                            _colorContextHandle.Dispose();
                            _colorContextHandle = null;
                            
                            if (HRESULT.Failed(UnsafeNativeMethodsMilCoreApi.WICCodec.CreateColorContext(
                                factoryMaker.ImagingFactoryPtr, out _colorContextHandle))
                                )
                            {
                                return;
                            }

                            if (HRESULT.Failed(IWICCC.InitializeFromMemory(
                                _colorContextHandle, sRGBProfile, (uint)sRGBProfile.Length))
                                )
                            {
                                return;
                            }
                        }

                        // Finally, fill in _colorContextHelper
                        FromRawBytes(sRGBProfile, sRGBProfile.Length, /* dontThrowException = */ true);
                    }
                    else if (Invariant.Strict)
                    {
                        Invariant.Assert(false, String.Format(CultureInfo.InvariantCulture, "IWICColorContext::GetExifColorSpace returned {0}.", colorSpace));
                    }

                    break;

                default:
                    if (Invariant.Strict)
                    {
                        Invariant.Assert(false, "IWICColorContext::GetType() returned WICColorContextUninitialized.");
                    }

                    break;
            }


            // SECURITY NOTE: This constructor does not set a Uri because the profile comes from raw file
            //                data. Thus, we don't set _isProfileUriNotFromUser to true because we
            //                don't want get_ProfileUri to demand permission to return null.
            Debug.Assert(_profileUri.Value == null);
        }

        /// <summary>
        /// Creates a new ColorContext object from a .icm or .icc color profile specified by profileUri.
        /// </summary>
        /// <param name="profileUri">Specifies the URI of a color profile used by the newly created ColorContext.</param>
        public ColorContext(Uri profileUri)
        {
            Initialize(profileUri, /* isStandardProfileUriNotFromUser = */ false);
        }

        /// <summary>
        /// Given a pixel format, this function will return the closest standard color space (sRGB, scRGB, etc)
        /// </summary>
        public ColorContext(PixelFormat pixelFormat)
        {
            switch (pixelFormat.Format)
            {
                case PixelFormatEnum.Default:
                case PixelFormatEnum.Indexed1:
                case PixelFormatEnum.Indexed2:
                case PixelFormatEnum.Indexed4:
                case PixelFormatEnum.Indexed8:
                case PixelFormatEnum.Bgr555:
                case PixelFormatEnum.Bgr565:
                case PixelFormatEnum.Bgr24:
                case PixelFormatEnum.Rgb24:
                case PixelFormatEnum.Bgr32:
                case PixelFormatEnum.Bgra32:
                case PixelFormatEnum.Pbgra32:
                default:
                    Initialize(GetStandardColorSpaceProfile(), /* isStandardProfileUriNotFromUser = */ true);
                    break;

                case PixelFormatEnum.Rgba64:
                case PixelFormatEnum.Prgba64:
                case PixelFormatEnum.Rgba128Float:
                case PixelFormatEnum.Prgba128Float:
                case PixelFormatEnum.BlackWhite:
                case PixelFormatEnum.Gray2:
                case PixelFormatEnum.Gray4:
                case PixelFormatEnum.Gray8:
                case PixelFormatEnum.Gray32Float:
                case PixelFormatEnum.Cmyk32:
                    throw new NotSupportedException(); // standard scRGB profile does not exist yet
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a memory stream to the color profile bits
        /// </summary>
        public Stream OpenProfileStream()
        {
            //
            // 3.* backwards compat for a "bad" ColorContext. Now the helper is a
            // struct so when it's invalid we'll pretend it's a reference type. This
            // should only happen if the color profile is corrupt (see early exits in
            // ColorContext(SafeMILHandle) and FromRawBytes()).
            //
            if (_colorContextHelper.IsInvalid)
            {
                throw new NullReferenceException();
            }
            
            uint profileSize = 0;
            _colorContextHelper.GetColorProfileFromHandle(null, ref profileSize);
            
            byte[] profile = new byte[profileSize];
            _colorContextHelper.GetColorProfileFromHandle(profile, ref profileSize);

            return new MemoryStream(profile);
        }

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// ProfileUri
        /// </summary>
        public Uri ProfileUri
        {
            get
            {
                Uri uri = _profileUri.Value;

                //
                // If the user didn't give us the uri value, then the uri has
                // to be a file path because we got it from GetStandardColorSpaceProfile
                //
                if (_isProfileUriNotFromUser.Value)
                {
                    Invariant.Assert(uri.IsFile);
                }

                return uri;
            }
        }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// ProfileHandle
        /// </summary>
        internal SafeProfileHandle ProfileHandle
        {
            get
            {
                return _colorContextHelper.ProfileHandle;
            }
        }

        /// <summary>
        /// ColorContextHandleHandle
        /// </summary>
        internal SafeMILHandle ColorContextHandle
        {
            get
            {
                return _colorContextHandle;
            }
        }


        /// <summary>
        /// NumChannels
        /// </summary>
        internal int NumChannels
        {
            get
            {
                if (_colorContextHelper.IsInvalid) // sRGB or scRGB
                    return 3;

                return _numChannels;
            }
        }

        /// <summary>
        /// ColorType
        /// </summary>
        internal UInt32 ColorType
        {
            get
            {
                return (UInt32)_colorTypeFromChannels[NumChannels];
            }
        }

        /// <summary>
        /// ColorSpaceFamily
        /// </summary>
        internal StandardColorSpace ColorSpaceFamily
        {
            get
            {
                if (_colorContextHelper.IsInvalid) // sRGB or scRGB
                {
                    return StandardColorSpace.Srgb;
                }
                else
                {
                    return _colorSpaceFamily;
                }
            }
        }

        /// <summary>
        /// Returns false if the ColorContext hasn't been properly initialized due to a bad color profile.
        /// If this is false, use of this ColorContext will lead to exceptions.
        /// </summary>
        internal bool IsValid
        {
            get
            {
                return !_colorContextHelper.IsInvalid;
            }
        }

        internal delegate int GetColorContextsDelegate(ref uint numContexts, IntPtr[] colorContextPtrs);

        /// <summary>
        /// Helper method that will retrieve ColorContexts from an unmanaged object (e.g. BitmapDecoder or BitmapFrameDecode)
        /// </summary>
        internal static IList<ColorContext> GetColorContextsHelper(GetColorContextsDelegate getColorContexts)
        {
            uint numContexts = 0;
            List<ColorContext> colorContextsList = null;

            int hr = getColorContexts(ref numContexts, null);
            if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
            {
                HRESULT.Check(hr);
            }
            
            if (numContexts > 0)
            {
                // GetColorContexts does not create new IWICColorContexts. Instead, it initializes existing
                // ones so we must create them beforehand.
                SafeMILHandle[] colorContextHandles = new SafeMILHandle[numContexts];
                
                using (FactoryMaker factoryMaker = new FactoryMaker())
                {
                    for (uint i = 0; i < numContexts; ++i)
                    {
                        HRESULT.Check(UnsafeNativeMethodsMilCoreApi.WICCodec.CreateColorContext(factoryMaker.ImagingFactoryPtr, out colorContextHandles[i]));
                    }
                }

                // The Marshal is unable to handle SafeMILHandle[] so we will convert it to an IntPtr[] ourselves.
                {
                    IntPtr[] colorContextPtrs = new IntPtr[numContexts];
                    
                    for (uint i = 0; i < numContexts; ++i)
                    {
                        colorContextPtrs[i] = colorContextHandles[i].DangerousGetHandle();
                    }

                    HRESULT.Check(getColorContexts(ref numContexts, colorContextPtrs));
                }

                colorContextsList = new List<ColorContext>((int)numContexts);
                for (uint i = 0; i < numContexts; ++i)
                {
                    colorContextsList.Add(new ColorContext(colorContextHandles[i]));
                }
            }

            return colorContextsList;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Equality Methods/Properties
        //
        //------------------------------------------------------

        #region Equality methods and Properties

        /// <summary>
        /// Equals method
        /// </summary>
        override public bool Equals(object obj)
        {
            ColorContext context = obj as ColorContext;

            return (context == this);
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        override public int GetHashCode()
        {
            // phDateTime_2 contains the minute and second that the profile was created. Obviously this 
            // is not a great hash, but the compiler forces us to implement this due to us implementing
            // operator==. Plus, we don't see hashing ColorContexts as an important scenario. This
            // is good enough.
            return (int)_profileHeader.phDateTime_2;
        }

        /// <summary>
        /// Operator==
        /// </summary>
        public static bool operator==(ColorContext context1, ColorContext context2)
        {
            object obj1 = context1;
            object obj2 = context2;

            if (obj1 == null && obj2 == null)
            {
                return true;
            }
            else if (obj1 != null && obj2 != null)
            {
                #pragma warning disable 6506
                return (
                    (context1._profileHeader.phSize == context2._profileHeader.phSize) &&
                    (context1._profileHeader.phCMMType == context2._profileHeader.phCMMType) &&
                    (context1._profileHeader.phVersion == context2._profileHeader.phVersion) &&
                    (context1._profileHeader.phClass == context2._profileHeader.phClass) &&
                    (context1._profileHeader.phDataColorSpace == context2._profileHeader.phDataColorSpace) &&
                    (context1._profileHeader.phConnectionSpace == context2._profileHeader.phConnectionSpace) &&
                    (context1._profileHeader.phDateTime_0 == context2._profileHeader.phDateTime_0) &&
                    (context1._profileHeader.phDateTime_1 == context2._profileHeader.phDateTime_1) &&
                    (context1._profileHeader.phDateTime_2 == context2._profileHeader.phDateTime_2) &&
                    (context1._profileHeader.phSignature == context2._profileHeader.phSignature) &&
                    (context1._profileHeader.phPlatform == context2._profileHeader.phPlatform) &&
                    (context1._profileHeader.phProfileFlags == context2._profileHeader.phProfileFlags) &&
                    (context1._profileHeader.phManufacturer == context2._profileHeader.phManufacturer) &&
                    (context1._profileHeader.phModel == context2._profileHeader.phModel) &&
                    (context1._profileHeader.phAttributes_0 == context2._profileHeader.phAttributes_0) &&
                    (context1._profileHeader.phAttributes_1 == context2._profileHeader.phAttributes_1) &&
                    (context1._profileHeader.phRenderingIntent == context2._profileHeader.phRenderingIntent) &&
                    (context1._profileHeader.phIlluminant_0 == context2._profileHeader.phIlluminant_0) &&
                    (context1._profileHeader.phIlluminant_1 == context2._profileHeader.phIlluminant_1) &&
                    (context1._profileHeader.phIlluminant_2 == context2._profileHeader.phIlluminant_2) &&
                    (context1._profileHeader.phCreator == context2._profileHeader.phCreator)
                    );
                #pragma warning restore 6506
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Operator!=
        /// </summary>
        public static bool operator!=(ColorContext context1, ColorContext context2)
        {
            return !(context1 == context2);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads color profile given by profileUri
        /// </summary>
        private void Initialize(Uri profileUri, bool isStandardProfileUriNotFromUser)
        {
            bool tryProfileFromResource = false;

            if (profileUri == null)
            {
                throw new ArgumentNullException("profileUri");
            }

            if (!profileUri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.Get(SRID.UriNotAbsolute), "profileUri");
            }

            _profileUri = new SecurityCriticalData<Uri>(profileUri);
            _isProfileUriNotFromUser = new SecurityCriticalDataForSet<bool>(isStandardProfileUriNotFromUser);

            Stream profileStream = null;

            try
            {
                profileStream = WpfWebRequestHelper.CreateRequestAndGetResponseStream(profileUri);
            }
            catch (WebException)
            {
                //
                // If we couldn't load the system's default color profile (e.g. in partial trust), load a color profile from
                // a resource so the image shows up at least. If the user specified a color profile and we weren't 
                // able to load it, we'll fail to avoid letting the user use this resource fallback as a way to discover 
                // files on disk.
                //
                if (isStandardProfileUriNotFromUser)
                {
                    tryProfileFromResource = true;
                }
            }

            if (profileStream == null)
            {
                if (tryProfileFromResource)
                {
                    ResourceManager resourceManager = new ResourceManager(_colorProfileResources, Assembly.GetAssembly(typeof(ColorContext)));
                    byte[] sRGBProfile = (byte[])resourceManager.GetObject(_sRGBProfileName);

                    profileStream = new MemoryStream(sRGBProfile);
                }
                else
                {
                    //
                    // SECURITY WARNING: This exception includes the profile URI which may contain sensitive information. However, as of right now,
                    // this is safe because it can only happen when the URI is given to us by the user.
                    //
                    Invariant.Assert(!isStandardProfileUriNotFromUser);
                    throw new FileNotFoundException(SR.Get(SRID.FileNotFoundExceptionWithFileName, profileUri.AbsolutePath), profileUri.AbsolutePath);
                }
            }

            FromStream(profileStream, profileUri.AbsolutePath);
        }

        /// <summary>
        /// Obtains the system color profile path
        /// </summary>
        private static Uri GetStandardColorSpaceProfile()
        {
            const int SIZE = NativeMethods.MAX_PATH;
            
            uint dwProfileID = (uint)NativeMethods.ColorSpace.SPACE_sRGB;
            uint bufferSize = SIZE;
            StringBuilder buffer = new StringBuilder(SIZE);

            HRESULT.Check(UnsafeNativeMethodsMilCoreApi.Mscms.GetStandardColorSpaceProfile(IntPtr.Zero, dwProfileID, buffer, out bufferSize));

            Uri profilePath;
            string profilePathString = buffer.ToString();

            if (!Uri.TryCreate(profilePathString, UriKind.Absolute, out profilePath))
            {
                //
                // GetStandardColorSpaceProfile() returns whatever was given to SetStandardColorSpaceProfile().
                // If it were set to a relative path by the user, we should throw an exception to avoid any possible
                // security issues. However, the Vista control panel uses the same API and sometimes likes to set
                // relative paths. Since we can't tell the difference and we want people to be able to change
                // their color profile from the control panel, we'll tack on the system directory.
                //

                // bufferSize was modified by GetStandardColorSpaceProfile so set it again
                bufferSize = SIZE;

                HRESULT.Check(UnsafeNativeMethodsMilCoreApi.Mscms.GetColorDirectory(IntPtr.Zero, buffer, out bufferSize));

                profilePath = new Uri(Path.Combine(buffer.ToString(), profilePathString));
            }

            return profilePath;
        }

        private void FromStream(Stream stm, string filename)
        {
            Debug.Assert(stm != null);

            int bufferSize = _bufferSizeIncrement;

            if (stm.CanSeek)
            {
                bufferSize = (int)stm.Length + 1; // If this stream is seekable (most cases), we will only have one buffer alloc and read below
                                                // otherwise, we will incrementally grow the buffer and read until end of profile.
                                                // profiles are typcially small, so usually one allocation will suffice
            }

            byte[] rawBytes = new byte[bufferSize];
            int numBytesRead = 0;
            while (bufferSize < _maximumColorContextLength)
            {
                numBytesRead += stm.Read(rawBytes, numBytesRead, bufferSize - numBytesRead);

                if (numBytesRead < bufferSize)
                {
                    FromRawBytes(rawBytes, numBytesRead, /* dontThrowException = */ false);

                    using (FactoryMaker factoryMaker = new FactoryMaker())
                    {
                        HRESULT.Check(UnsafeNativeMethodsMilCoreApi.WICCodec.CreateColorContext(factoryMaker.ImagingFactoryPtr, out _colorContextHandle));
                        HRESULT.Check(IWICCC.InitializeFromMemory(_colorContextHandle, rawBytes, (uint)numBytesRead));
                    }

                    return;
                }
                else
                {
                    bufferSize += _bufferSizeIncrement;
                    byte[] newRawBytes = new byte[bufferSize];
                    rawBytes.CopyTo(newRawBytes, 0);
                    rawBytes = newRawBytes;
                }
            }

            throw new ArgumentException(SR.Get(SRID.ColorContext_FileTooLarge), filename);
        }

        /// Note: often the data buffer is larger than the actual data in it.
        ///
        /// dontThrowException is for preserving the 3.* behavior of ColorContext(SafeMILHandle)
        ///
        private void FromRawBytes(byte[] data, int dataLength, bool dontThrowException) 
        {
            Invariant.Assert(dataLength <= data.Length);
            Invariant.Assert(dataLength >= 0);
            
            UnsafeNativeMethods.PROFILEHEADER header;
            UnsafeNativeMethods.PROFILE profile;

            unsafe
            {
                fixed (void *dataPtr = data)
                {
                    profile.dwType = NativeMethods.ProfileType.PROFILE_MEMBUFFER;
                    profile.pProfileData = dataPtr;
                    profile.cbDataSize = (uint)dataLength;

                    _colorContextHelper.OpenColorProfile(ref profile);

                    if (_colorContextHelper.IsInvalid)
                    {
                        if (dontThrowException)
                        {
                            return;
                        }
                        else
                        {                
                            HRESULT.Check(Marshal.GetHRForLastWin32Error());
                        }
                    }
                }
            }

            if (!_colorContextHelper.GetColorProfileHeader(out header))
            {
                if (dontThrowException)
                {
                    return;
                }
                else
                {                
                    HRESULT.Check(Marshal.GetHRForLastWin32Error());
                }
            }
            
            // Copy the important stuff from the header into our smaller cache
            _profileHeader.phSize            = header.phSize;
            _profileHeader.phCMMType         = header.phCMMType;
            _profileHeader.phVersion         = header.phVersion;
            _profileHeader.phClass           = header.phClass;
            _profileHeader.phDataColorSpace  = header.phDataColorSpace;
            _profileHeader.phConnectionSpace = header.phConnectionSpace;
            _profileHeader.phDateTime_0      = header.phDateTime_0;
            _profileHeader.phDateTime_1      = header.phDateTime_1;
            _profileHeader.phDateTime_2      = header.phDateTime_2;
            _profileHeader.phSignature       = header.phSignature;
            _profileHeader.phPlatform        = header.phPlatform;
            _profileHeader.phProfileFlags    = header.phProfileFlags;
            _profileHeader.phManufacturer    = header.phManufacturer;
            _profileHeader.phModel           = header.phModel;
            _profileHeader.phAttributes_0    = header.phAttributes_0;
            _profileHeader.phAttributes_1    = header.phAttributes_1;
            _profileHeader.phRenderingIntent = header.phRenderingIntent;
            _profileHeader.phIlluminant_0    = header.phIlluminant_0;
            _profileHeader.phIlluminant_1    = header.phIlluminant_1;
            _profileHeader.phIlluminant_2    = header.phIlluminant_2;
            _profileHeader.phCreator         = header.phCreator;

            switch (_profileHeader.phDataColorSpace)
            {
                case NativeMethods.ColorSpace.SPACE_XYZ:
                case NativeMethods.ColorSpace.SPACE_Lab:
                case NativeMethods.ColorSpace.SPACE_Luv:
                case NativeMethods.ColorSpace.SPACE_YCbCr:
                case NativeMethods.ColorSpace.SPACE_Yxy:
                case NativeMethods.ColorSpace.SPACE_HSV:
                case NativeMethods.ColorSpace.SPACE_HLS:
                case NativeMethods.ColorSpace.SPACE_CMY:
                    _numChannels = 3;
                    _colorSpaceFamily = StandardColorSpace.Unknown;
                    break;
                case NativeMethods.ColorSpace.SPACE_RGB:
                    _colorSpaceFamily = StandardColorSpace.Rgb;
                    _numChannels = 3;
                    break;
                case NativeMethods.ColorSpace.SPACE_GRAY:
                    _colorSpaceFamily = StandardColorSpace.Gray;
                    _numChannels = 1;
                    break;
                case NativeMethods.ColorSpace.SPACE_CMYK:
                    _colorSpaceFamily = StandardColorSpace.Cmyk;
                    _numChannels = 4;
                    break;
                case NativeMethods.ColorSpace.SPACE_2_CHANNEL:
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    _numChannels = 2;
                    break;
                case NativeMethods.ColorSpace.SPACE_3_CHANNEL:
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    _numChannels = 3;
                    break;
                case NativeMethods.ColorSpace.SPACE_4_CHANNEL:
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    _numChannels = 4;
                    break;
                case NativeMethods.ColorSpace.SPACE_5_CHANNEL:
                    _numChannels = 5;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_6_CHANNEL:
                    _numChannels = 6;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_7_CHANNEL:
                    _numChannels = 7;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_8_CHANNEL:
                    _numChannels = 8;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_9_CHANNEL:
                    _numChannels = 9;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_A_CHANNEL:
                    _numChannels = 10;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_B_CHANNEL:
                    _numChannels = 11;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_C_CHANNEL:
                    _numChannels = 12;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_D_CHANNEL:
                    _numChannels = 13;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_E_CHANNEL:
                    _numChannels = 14;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case NativeMethods.ColorSpace.SPACE_F_CHANNEL:
                    _numChannels = 15;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                default:
                    _numChannels = 0;
                    _colorSpaceFamily = StandardColorSpace.Unknown;
                    break;
            }
        }

        #endregion

        #region Private Fields

        private ColorContextHelper _colorContextHelper;

        private StandardColorSpace _colorSpaceFamily;

        private int _numChannels;

        private SecurityCriticalData<Uri> _profileUri;
        
        private SecurityCriticalDataForSet<bool> _isProfileUriNotFromUser;

        private AbbreviatedPROFILEHEADER _profileHeader;

        private SafeMILHandle _colorContextHandle;

        private const int _bufferSizeIncrement = 1024 * 1024;  // 1 Mb

        private const int _maximumColorContextLength = _bufferSizeIncrement * 32; // 32 Mb

        private readonly static NativeMethods.COLORTYPE[] _colorTypeFromChannels =
            new NativeMethods.COLORTYPE[9] {
                NativeMethods.COLORTYPE.COLOR_UNDEFINED,
                NativeMethods.COLORTYPE.COLOR_UNDEFINED,
                NativeMethods.COLORTYPE.COLOR_UNDEFINED,
                NativeMethods.COLORTYPE.COLOR_3_CHANNEL,
                NativeMethods.COLORTYPE.COLOR_CMYK,
                NativeMethods.COLORTYPE.COLOR_5_CHANNEL,
                NativeMethods.COLORTYPE.COLOR_6_CHANNEL,
                NativeMethods.COLORTYPE.COLOR_7_CHANNEL,
                NativeMethods.COLORTYPE.COLOR_8_CHANNEL
                };

        private readonly static string _colorProfileResources = "ColorProfiles";

        private readonly static string _sRGBProfileName = "sRGB_icm";

        [StructLayout(LayoutKind.Sequential)]
        private struct AbbreviatedPROFILEHEADER
        {
            public uint phSize;                  // profile size in bytes
            public uint phCMMType;               // CMM for this profile
            public uint phVersion;               // profile format version number
            public uint phClass;                 // type of profile
            public NativeMethods.ColorSpace phDataColorSpace;  // color space of data
            public uint phConnectionSpace;       // PCS
            public uint phDateTime_0;            // date profile was created
            public uint phDateTime_1;            // date profile was created
            public uint phDateTime_2;            // date profile was created
            public uint phSignature;             // magic number ("Reserved for internal use.")
            public uint phPlatform;              // primary platform
            public uint phProfileFlags;          // various bit settings
            public uint phManufacturer;          // device manufacturer
            public uint phModel;                 // device model number
            public uint phAttributes_0;          // device attributes
            public uint phAttributes_1;          // device attributes
            public uint phRenderingIntent;       // rendering intent
            public uint phIlluminant_0;          // profile illuminant
            public uint phIlluminant_1;          // profile illuminant
            public uint phIlluminant_2;          // profile illuminant
            public uint phCreator;               // profile creator
            // Not including the reserved bits because we don't want to unnecessarily
            // increase the size of ColorContext
            //    public byte phReserved[44];   
        };

        internal enum StandardColorSpace : int
        {
            Unknown = 0,
            Srgb = 1,
            ScRgb = 2,
            Rgb = 3,
            Cmyk = 4,
            Gray = 6,
            Multichannel = 7
        }

        #endregion
    }
}
