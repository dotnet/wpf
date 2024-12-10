// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;
using static Windows.Win32.Foundation.HResultExtensions;

namespace Windows.Win32.Foundation;

public class HResultTests
{
    [Theory]
    [MemberData(nameof(HResultTestData))]
    public void HResult_GetExtendedException(int result, Type expectedType)
    {
        Exception? e = ((HRESULT)result).GetExtendedException();
        e.Should().BeOfType(expectedType);
    }

    public static TheoryData<int, Type> HResultTestData => new()
    {
        { HRESULT.FromWin32(WIN32_ERROR.ERROR_ARITHMETIC_OVERFLOW), typeof(OverflowException) },
        { HRESULT.FromWin32(WIN32_ERROR.ERROR_INVALID_PARAMETER), typeof(ArgumentException) },
        { MilErrors.WGXERR_AV_INVALIDWMPVERSION, typeof(InvalidWmpVersionException) },
        { MediaPlayerErrors.NS_E_WMP_CANNOT_FIND_FILE, typeof(FileNotFoundException) },
        { HRESULT.WINCODEC_ERR_UNSUPPORTEDVERSION, typeof(FileLoadException) }
    };
}
