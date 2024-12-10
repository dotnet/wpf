// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Windows.Win32.Foundation;

public class MilErrorsTests
{
    [Theory]
    [MemberData(nameof(MilErrorTestData))]
    public void MilErrors_ValidateDefines(int result, int expected)
    {
        result.Should().Be(expected);
    }

    public static TheoryData<int, int> MilErrorTestData => new()
    {
        // Check a few to see that we've copied the pattern correctly
        { (int)MilErrors.WGXHR_CLIPPEDTOEMPTY, 0x8980001 },
        { (int)MilErrors.WGXERR_OBJECTBUSY, unchecked((int)0x88980001) },
        { (int)MilErrors.WGXERR_UCE_UNSUPPORTEDTRANSPORTVERSION, unchecked((int)0x88980415) },
        { (int)MilErrors.WGXERR_AV_UNKNOWNHARDWAREERROR, unchecked((int)0x8898050E) },
    };
}
