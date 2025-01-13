// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement.Tests;

public class UseLicenseTests
{
    [Fact]
    public void Ctor_NullUseLicense_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("useLicense", () => new UseLicense(null!));
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("name")]
    [InlineData("<xml></xml>")]
    public void Ctor_InvalidUseLicense_ThrowsRightsManagementException(string useLicense)
    {
        Assert.Throws<RightsManagementException>(() => new UseLicense(useLicense));
    }
}