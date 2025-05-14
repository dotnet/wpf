// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement.Tests;

public class UnsignedPublishLicenseTests
{
    [Fact]
    public void Ctor_Default()
    {
        var license = new UnsignedPublishLicense();
        Assert.NotEqual(Guid.Empty, license.ContentId);
        Assert.Empty(license.Grants);
        Assert.Same(license.Grants, license.Grants);
        Assert.Empty(license.LocalizedNameDescriptionDictionary);
        Assert.Same(license.LocalizedNameDescriptionDictionary, license.LocalizedNameDescriptionDictionary);
        Assert.Null(license.Owner);
        Assert.Null(license.ReferralInfoName);
        Assert.Null(license.ReferralInfoUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("unsignedPublishLicense")]
    [InlineData("<xml></xml>")]
    public void Ctor_InvalidPublishLicenseTemplate_ThrowsRightsManagementException(string unsignedPublishLicense)
    {
        Assert.Throws<RightsManagementException>(() => new UnsignedPublishLicense(unsignedPublishLicense));
    }

    [Fact]
    public void Ctor_NullPublishLicenseTemplate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("publishLicenseTemplate", () => new UnsignedPublishLicense(null));
    }

    public static IEnumerable<object[]> ContentId_TestData()
    {
        yield return new object[] { Guid.Empty };
        yield return new object[] { Guid.NewGuid() };
    }

    [Theory]
    [MemberData(nameof(ContentId_TestData))]
    public void ContentId_Set_GetReturnsExpected(Guid value)
    {
        var license = new UnsignedPublishLicense
        {
            ContentId = value
        };
        Assert.Equal(value, license.ContentId);

        // Set same.
        license.ContentId = value;
        Assert.Equal(value, license.ContentId);
    }

    public static IEnumerable<object?[]> Owner_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows) };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Passport) };
        yield return new object?[] { new ContentUser("name", AuthenticationType.WindowsPassport) };
        yield return new object?[] { new ContentUser("anyone", AuthenticationType.Internal) };
        yield return new object?[] { new ContentUser("owner", AuthenticationType.Internal) };
    }

    [Theory]
    [MemberData(nameof(Owner_TestData))]
    public void Owner_Set_GetReturnsExpected(ContentUser? value)
    {
        var license = new UnsignedPublishLicense
        {
            Owner = value
        };
        Assert.Equal(value, license.Owner);

        // Set same.
        license.Owner = value;
        Assert.Equal(value, license.Owner);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("referralInfoName")]
    public void ReferralInfoName_Set_GetReturnsExpected(string? value)
    {
        var license = new UnsignedPublishLicense
        {
            ReferralInfoName = value
        };
        Assert.Equal(value, license.ReferralInfoName);

        // Set same.
        license.ReferralInfoName = value;
        Assert.Equal(value, license.ReferralInfoName);
    }

    public static IEnumerable<object?[]> ReferralInfoUri_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new Uri("https://google.com") };
    }

    [Theory]
    [MemberData(nameof(ReferralInfoUri_TestData))]
    public void ReferralInfoUri_Set_GetReturnsExpected(Uri? value)
    {
        var license = new UnsignedPublishLicense
        {
            ReferralInfoUri = value
        };
        Assert.Equal(value, license.ReferralInfoUri);

        // Set same.
        license.ReferralInfoUri = value;
        Assert.Equal(value, license.ReferralInfoUri);
    }

    [Fact]
    public void Sign_NullSecureEnvironment_ThrowsArgumentNullException()
    {
        var license = new UnsignedPublishLicense();
        UseLicense? useLicense = null;
        Assert.Throws<ArgumentNullException>("secureEnvironment", () => license.Sign(null!, out useLicense));
        Assert.Null(useLicense);
    }

    [Fact]
    public void ToString_Default_ThrowsRightsManagementException()
    {
        var license = new UnsignedPublishLicense();
        Assert.Throws<RightsManagementException>(() => license.ToString());
    }
}