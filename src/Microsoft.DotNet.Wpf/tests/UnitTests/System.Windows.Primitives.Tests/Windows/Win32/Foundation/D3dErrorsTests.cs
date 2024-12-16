// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Windows.Win32.Foundation;

public class D3dErrorsTests
{
    [Theory]
    [MemberData(nameof(D3dErrorTestData))]
    public void D3dErrors_ValidateDefines(int result, int expected)
    {
        result.Should().Be(expected);
    }

    public static TheoryData<int, int> D3dErrorTestData => new()
    {
        // Check a few to see that we've copied the pattern correctly
        { (int)D3dErrors.D3DERR_DEVICEHUNG, unchecked((int)0x88760874) },
        { (int)D3dErrors.D3DERR_OUTOFVIDEOMEMORY, unchecked((int)0x8876017C) }
    };
}
