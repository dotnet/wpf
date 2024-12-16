// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Media;
using Windows.Win32.System.Diagnostics.Debug;

namespace Windows.Win32.Foundation;

internal static class HResultExtensions
{
    /// <summary>
    ///  Extended version of <see cref="HRESULT.ThrowOnFailure"/> that special cases WIC and MIL errors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowOnFailureExtended(this HRESULT result)
    {
        if (!result.Failed)
        {
            return;
        }

        // Separate method to facilitate inlining.
        result.ThrowOnFailureExtendedPrivate();
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOnFailureExtendedPrivate(this HRESULT result) => throw result.GetExtendedException();

    /// <summary>
    ///  Helper that gets gets exceptions with special casing for WIC and MIL errors. Callers should check
    ///  <see cref="HRESULT.Failed"/> before calling this method.
    /// </summary>
    internal static Exception GetExtendedException(this HRESULT result)
    {
        Debug.Assert(result.Failed);

        if (result.IsNtStatus() && result.ToNtStatus() == NTSTATUS.STATUS_NO_MEMORY)
        {
            // We historically threw this.
#pragma warning disable CA2201 // Do not raise reserved exception types
            return new OutOfMemoryException();
#pragma warning restore CA2201
        }

        // Pass -1 to ignore current IErrorInfo.
        Exception exception = Marshal.GetExceptionForHR(result, -1)
            ?? new InvalidOperationException() { HResult = result.Value };

        FACILITY_CODE facility = result.Facility;

        if (facility == FACILITY_CODE.FACILITY_WIN32)
        {
            WIN32_ERROR error = (WIN32_ERROR)result.Code;

            return error switch
            {
                // This is what WINCODEC_ERR_VALUEOVERFLOW is defined to.
                WIN32_ERROR.ERROR_ARITHMETIC_OVERFLOW => new OverflowException(SR.Image_Overflow, exception),
                // Previously checked directly as 0x80070057.
                WIN32_ERROR.ERROR_INVALID_PARAMETER => new ArgumentException(SR.Media_InvalidArgument, exception),
                // Previously checked directly as 0x800707db.
                WIN32_ERROR.ERROR_INVALID_PROFILE => new FileFormatException(null, SR.Image_InvalidColorContext, exception),
                _ => exception,
            };
        }
        else if (facility == FACILITY_CODE.FACILITY_MEDIASERVER)
        {
            if (result == MediaPlayerErrors.NS_E_WMP_LOGON_FAILURE)
            {
                return new SecurityException(SR.Media_LogonFailure, exception);
            }
            else if (result == MediaPlayerErrors.NS_E_WMP_CANNOT_FIND_FILE)
            {
                return new FileNotFoundException(SR.Media_FileNotFound, exception);
            }
            else if (result == MediaPlayerErrors.NS_E_WMP_UNSUPPORTED_FORMAT
                || result == MediaPlayerErrors.NS_E_WMP_DSHOW_UNSUPPORTED_FORMAT)
            {
                return new FileFormatException(SR.Media_FileFormatNotSupported, exception);
            }
            else if (result == MediaPlayerErrors.NS_E_WMP_INVALID_ASX)
            {
                return new FileFormatException(SR.Media_PlaylistFormatNotSupported, exception);
            }
        }
        else if (facility == FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM)
        {
            if (result == MilErrors.WGXERR_DISPLAYSTATEINVALID)
            {
                return new InvalidOperationException(SR.Image_DisplayStateInvalid, exception);
            }
            else if (result == MilErrors.WGXERR_NONINVERTIBLEMATRIX)
            {
                return new ArithmeticException(SR.Image_SingularMatrix, exception);
            }
            else if (result == MilErrors.WGXERR_AV_INVALIDWMPVERSION)
            {
                return new InvalidWmpVersionException(SR.Media_InvalidWmpVersion, exception);
            }
            else if (result == MilErrors.WGXERR_AV_INSUFFICIENTVIDEORESOURCES)
            {
                return new NotSupportedException(SR.Media_InsufficientVideoResources, exception);
            }
            else if (result == MilErrors.WGXERR_AV_VIDEOACCELERATIONNOTAVAILABLE)
            {
                return new NotSupportedException(SR.Media_HardwareVideoAccelerationNotAvailable, exception);
            }
            else if (result == MilErrors.WGXERR_AV_MEDIAPLAYERCLOSED)
            {
                return new NotSupportedException(SR.Media_PlayerIsClosed, exception);
            }
            else if (result == MilErrors.WGXERR_BADNUMBER)
            {
                return new ArgumentException(SR.Geometry_BadNumber, exception);
            }
            else if (result == MilErrors.WGXERR_D3DI_INVALIDSURFACEUSAGE)
            {
                return new ArgumentException(SR.D3DImage_InvalidUsage, exception);
            }
            else if (result == MilErrors.WGXERR_D3DI_INVALIDSURFACESIZE)
            {
                return new ArgumentException(SR.D3DImage_SurfaceTooBig, exception);
            }
            else if (result == MilErrors.WGXERR_D3DI_INVALIDSURFACEPOOL)
            {
                return new ArgumentException(SR.D3DImage_InvalidPool, exception);
            }
            else if (result == MilErrors.WGXERR_D3DI_INVALIDSURFACEDEVICE)
            {
                return new ArgumentException(SR.D3DImage_InvalidDevice, exception);
            }
            else if (result == MilErrors.WGXERR_D3DI_INVALIDANTIALIASINGSETTINGS)
            {
                return new ArgumentException(SR.D3DImage_AARequires9Ex, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_WRONGSTATE)
            {
                return new InvalidOperationException(SR.Image_WrongState, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_VALUEOUTOFRANGE)
            {
                return new OverflowException(SR.Image_Overflow, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_UNKNOWNIMAGEFORMAT)
            {
                return new FileFormatException(null, SR.Image_UnknownFormat, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_UNSUPPORTEDVERSION)
            {
                return new FileLoadException(SR.MilErr_UnsupportedVersion, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_NOTINITIALIZED)
            {
                return new InvalidOperationException(SR.WIC_NotInitialized, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND)
            {
                return new ArgumentException(SR.Image_PropertyNotFound, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_PROPERTYNOTSUPPORTED)
            {
                return new NotSupportedException(SR.Image_PropertyNotSupported, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_PROPERTYSIZE)
            {
                return new ArgumentException(SR.Image_PropertySize, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_CODECPRESENT)
            {
                return new InvalidOperationException(SR.Image_CodecPresent, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_CODECNOTHUMBNAIL)
            {
                return new NotSupportedException(SR.Image_NoThumbnail, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_PALETTEUNAVAILABLE)
            {
                return new InvalidOperationException(SR.Image_NoPalette, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_CODECTOOMANYSCANLINES)
            {
                return new ArgumentException(SR.Image_TooManyScanlines, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_INTERNALERROR)
            {
                return new InvalidOperationException(SR.Image_InternalError, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_SOURCERECTDOESNOTMATCHDIMENSIONS)
            {
                return new ArgumentException(SR.Image_BadDimensions, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_COMPONENTINITIALIZEFAILURE
                || result == HRESULT.WINCODEC_ERR_COMPONENTNOTFOUND)
            {
                return new NotSupportedException(SR.Image_ComponentNotFound, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_UNEXPECTEDSIZE
                || result == HRESULT.WINCODEC_ERR_BADIMAGE)
            {
                return new FileFormatException(null, SR.Image_DecoderError, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_BADHEADER)
            {
                return new FileFormatException(null, SR.Image_HeaderError, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_FRAMEMISSING)
            {
                return new ArgumentException(SR.Image_FrameMissing, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_BADMETADATAHEADER)
            {
                return new ArgumentException(SR.Image_BadMetadataHeader, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_BADSTREAMDATA)
            {
                return new ArgumentException(SR.Image_BadStreamData, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_STREAMWRITE)
            {
                return new InvalidOperationException(SR.Image_StreamWrite, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT)
            {
                return new NotSupportedException(SR.Image_UnsupportedPixelFormat, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION)
            {
                return new NotSupportedException(SR.Image_UnsupportedOperation, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_IMAGESIZEOUTOFRANGE)
            {
                return new ArgumentException(SR.Image_SizeOutOfRange, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_STREAMREAD)
            {
                return new IOException(SR.Image_StreamRead, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_INVALIDQUERYREQUEST)
            {
                return new IOException(SR.Image_InvalidQueryRequest, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_UNEXPECTEDMETADATATYPE)
            {
                return new FileFormatException(null, SR.Image_UnexpectedMetadataType, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_REQUESTONLYVALIDATMETADATAROOT)
            {
                return new FileFormatException(null, SR.Image_RequestOnlyValidAtMetadataRoot, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_INVALIDQUERYCHARACTER)
            {
                return new IOException(SR.Image_InvalidQueryCharacter, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_DUPLICATEMETADATAPRESENT)
            {
                return new FileFormatException(null, SR.Image_DuplicateMetadataPresent, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_PROPERTYUNEXPECTEDTYPE)
            {
                return new FileFormatException(null, SR.Image_PropertyUnexpectedType, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_TOOMUCHMETADATA)
            {
                return new FileFormatException(null, SR.Image_TooMuchMetadata, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_STREAMNOTAVAILABLE)
            {
                return new NotSupportedException(SR.Image_StreamNotAvailable, exception);
            }
            else if (result == HRESULT.WINCODEC_ERR_INSUFFICIENTBUFFER)
            {
                return new ArgumentException(SR.Image_InsufficientBuffer, exception);
            }
        }

        return exception;
    }

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable format

    // Waiting on WinForms update to get these in HRESULT.
    internal static class MediaPlayerErrors
    {
        internal static readonly HRESULT NS_E_WMP_LOGON_FAILURE = new(unchecked((int)0xC00D1196L));
        internal static readonly HRESULT NS_E_WMP_CANNOT_FIND_FILE = new(unchecked((int)0xC00D1197L));
        internal static readonly HRESULT NS_E_WMP_UNSUPPORTED_FORMAT = new(unchecked((int)0xC00D1199L));
        internal static readonly HRESULT NS_E_WMP_DSHOW_UNSUPPORTED_FORMAT = new(unchecked((int)0xC00D119AL));
        internal static readonly HRESULT NS_E_WMP_INVALID_ASX = new(unchecked((int)0xC00D119DL));
        internal static readonly HRESULT NS_E_WMP_URLDOWNLOADFAILED = new(unchecked((int)0xC00D0FEAL));
    }
}
