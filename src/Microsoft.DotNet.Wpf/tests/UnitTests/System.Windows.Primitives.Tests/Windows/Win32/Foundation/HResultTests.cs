// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Windows.Win32.System.Diagnostics.Debug;

namespace Windows.Win32.Foundation;

public class HResultTests
{
    [Theory]
    [MemberData(nameof(HResultTestData))]
    public void HResult_GetExceptionUnwrapWin32(int result, Type expectedType)
    {
        Exception? e = ((HRESULT)result).GetExceptionUnwrapWin32();
        e.Should().BeOfType(expectedType);
    }

    public static TheoryData<int, Type> HResultTestData => new()
    {
        { HRESULT.FromWin32(WIN32_ERROR.ERROR_ACCESS_DENIED), typeof(UnauthorizedAccessException) },
        { HRESULT.COR_E_ARGUMENT, typeof(ArgumentException) },
        { HRESULT.FromWin32(WIN32_ERROR.ERROR_INVALID_HANDLE), typeof(Win32Exception) }
    };

    [Theory]
    [MemberData(nameof(WicFacilityTestData))]
    public void WicFacilityCode(int result, int expectedFacility)
    {
        FACILITY_CODE code = ((HRESULT)result).Facility;
        code.Should().Be((FACILITY_CODE)expectedFacility);
    }

    public static TheoryData<int, int> WicFacilityTestData => new()
    {
        {
            (int)HRESULT.WINCODEC_ERR_WRONGSTATE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_VALUEOUTOFRANGE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        // #define INTSAFE_E_ARITHMETIC_OVERFLOW   ((HRESULT)0x80070216L)  // 0x216 = 534 = ERROR_ARITHMETIC_OVERFLOW
        //{ (int)HRESULT.WINCODEC_ERR_VALUEOVERFLOW, (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM },
        {
            (int)HRESULT.WINCODEC_ERR_UNKNOWNIMAGEFORMAT,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_UNSUPPORTEDVERSION,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_NOTINITIALIZED,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_PROPERTYNOTFOUND,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_PROPERTYNOTSUPPORTED,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_PROPERTYSIZE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_CODECPRESENT,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_CODECNOTHUMBNAIL,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_PALETTEUNAVAILABLE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_CODECTOOMANYSCANLINES,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_INTERNALERROR,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_SOURCERECTDOESNOTMATCHDIMENSIONS,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_COMPONENTINITIALIZEFAILURE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_COMPONENTNOTFOUND,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_UNEXPECTEDSIZE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_BADIMAGE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_BADHEADER,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_FRAMEMISSING,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_BADMETADATAHEADER,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_BADSTREAMDATA,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_STREAMWRITE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_IMAGESIZEOUTOFRANGE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_STREAMREAD,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_INVALIDQUERYREQUEST,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_UNEXPECTEDMETADATATYPE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_REQUESTONLYVALIDATMETADATAROOT,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_INVALIDQUERYCHARACTER,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_DUPLICATEMETADATAPRESENT,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_PROPERTYUNEXPECTEDTYPE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_TOOMUCHMETADATA,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_STREAMNOTAVAILABLE,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
        {
            (int)HRESULT.WINCODEC_ERR_INSUFFICIENTBUFFER,
            (int)FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
        },
    };
    
}
