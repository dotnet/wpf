// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement.Tests;

public class SecureEnvironmentTests
{
#if false
    [Fact]
    public void Create()
    {
        const string Manifest =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>

<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
    <assemblyIdentity version=""1.0.0.0"" name=""YourAppName.app""/>
    <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
        <security>
            <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
                <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
            </requestedPrivileges>
        </security>
    </trustInfo>
</assembly>";
        SecureEnvironment environment = SecureEnvironment.Create(Manifest, AuthenticationType.Passport, UserActivationMode.Permanent);
    }
#endif

    [Fact]
    public void Create_NullApplicationManifest_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("applicationManifest", () => SecureEnvironment.Create(null!, AuthenticationType.Windows, UserActivationMode.Permanent));
        Assert.Throws<ArgumentNullException>("applicationManifest", () => SecureEnvironment.Create(null!, new ContentUser("name", AuthenticationType.Windows)));
    }

    // [Theory]
    // [InlineData("")]
    // [InlineData("  ")]
    // public void Create_EmptyApplicationManifest_ThrowsRightsManagementException(string applicationManifest)
    // {
    //     Assert.Throws<DllNotFoundException>(() => SecureEnvironment.Create(applicationManifest, AuthenticationType.Windows, UserActivationMode.Permanent));
    //     Assert.Throws<RightsManagementException>(() => SecureEnvironment.Create(applicationManifest, new ContentUser("name", AuthenticationType.Windows)));
    // }

    // [Theory]
    // [InlineData("applicationManifest")]
    // public void Create_InvokeInvalidApplicationManifest_ThrowsRightsManagementException(string applicationManifest)
    // {
    //     Assert.Throws<DllNotFoundException>(() => SecureEnvironment.Create(applicationManifest, AuthenticationType.Windows, UserActivationMode.Permanent));
    //     Assert.Throws<RightsManagementException>(() => SecureEnvironment.Create(applicationManifest, new ContentUser("name", AuthenticationType.Windows)));
    // }

    [Theory]
    [InlineData(AuthenticationType.Internal)]
    [InlineData(AuthenticationType.WindowsPassport)]
    [InlineData(AuthenticationType.Windows - 1)]
    [InlineData(AuthenticationType.Internal + 1)]
    public void Create_InvalidAuthenticationType_ThrowsArgumentOutOfRangeException(AuthenticationType authentication)
    {
        Assert.Throws<ArgumentOutOfRangeException>("authentication", () => SecureEnvironment.Create("manifest", authentication, UserActivationMode.Permanent));
    }

    [Theory]
    [InlineData(UserActivationMode.Permanent - 1)]
    [InlineData(UserActivationMode.Temporary + 1)]
    public void Create_InvalidUserActivationMode_ThrowsArgumentOutOfRangeException(UserActivationMode userActivationMode)
    {
        Assert.Throws<ArgumentOutOfRangeException>("userActivationMode", () => SecureEnvironment.Create("manifest", AuthenticationType.Windows, userActivationMode));
    }

    [Fact]
    public void Create_NullUser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("user", () => SecureEnvironment.Create("manifest", null!));
    }

    [Fact]
    public void Create_UserNotActivatedWindows_ThrowsRightsManagementException()
    {
        var user = new ContentUser("name", AuthenticationType.Windows);
        Assert.Throws<RightsManagementException>(() => SecureEnvironment.Create("<manifest></manifest>", user));
    }

    [Fact]
    public void Create_UserNotActivatedPassport_ThrowsRightsManagementException()
    {
        var user = new ContentUser("name", AuthenticationType.Passport);
        Assert.Throws<RightsManagementException>(() => SecureEnvironment.Create("<manifest></manifest>", user));
    }

    [Fact]
    public void Create_WindowsPassportUser_ThrowsArgumentOutOfRangeException()
    {
        var user = new ContentUser("name", AuthenticationType.WindowsPassport);
        Assert.Throws<ArgumentOutOfRangeException>("user", () => SecureEnvironment.Create("manifest", user));
    }

    [Theory]
    [InlineData("anyone")]
    [InlineData("owner")]
    public void Create_InternalUser_ThrowsArgumentOutOfRangeException(string name)
    {
        var user = new ContentUser(name, AuthenticationType.Internal);
        Assert.Throws<ArgumentOutOfRangeException>("user", () => SecureEnvironment.Create("manifest", user));
    }

    [Fact]
    public void Create_NotActivatedWindowsUser_ThrowsRightsManagementException()
    {
        var user = new ContentUser("name", AuthenticationType.Windows);
        Assert.Throws<RightsManagementException>(() => SecureEnvironment.Create("manifest", user));
    }

    [Fact]
    public void Create_NotActivatedPassportUser_ThrowsRightsManagementException()
    {
        var user = new ContentUser("name", AuthenticationType.Passport);
        Assert.Throws<RightsManagementException>(() => SecureEnvironment.Create("manifest", user));
    }

    [Fact]
    public void GetActivatedUsers_Invoke_ReturnsExpected()
    {
        var users = SecureEnvironment.GetActivatedUsers();
        Assert.NotNull(users);
    }

    [Fact]
    public void IsUserActivated_WindowsUser_ReturnsFalse()
    {
        var user = new ContentUser("name", AuthenticationType.Windows);
        Assert.False(SecureEnvironment.IsUserActivated(user));
    }
    
    [Fact]
    public void IsUserActivated_PassportUser_ReturnsFalse()
    {
        var user = new ContentUser("name", AuthenticationType.Passport);
        Assert.False(SecureEnvironment.IsUserActivated(user));
    }

    [Fact]
    public void IsUserActivated_NullUser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("user", () => SecureEnvironment.IsUserActivated(null!));
    }

    [Fact]
    public void IsUserActivated_WindowsPassportUser_ThrowsArgumentOutOfRangeException()
    {
        var user = new ContentUser("name", AuthenticationType.WindowsPassport);
        Assert.Throws<ArgumentOutOfRangeException>("user", () => SecureEnvironment.IsUserActivated(user));
    }

    [Theory]
    [InlineData("anyone")]
    [InlineData("owner")]
    public void IsUserActivated_InternalUser_ThrowsArgumentOutOfRangeException(string name)
    {
        var user = new ContentUser(name, AuthenticationType.Internal);
        Assert.Throws<ArgumentOutOfRangeException>("user", () => SecureEnvironment.IsUserActivated(user));
    }

    [Fact]
    public void RemoveActivatedUser_WindowsUser_ReturnsFalse()
    {
        var user = new ContentUser("name", AuthenticationType.Windows);
        SecureEnvironment.RemoveActivatedUser(user);
        Assert.False(SecureEnvironment.IsUserActivated(user));
    }
    
    [Fact]
    public void RemoveActivatedUser_PassportUser_ReturnsFalse()
    {
        var user = new ContentUser("name", AuthenticationType.Passport);
        SecureEnvironment.RemoveActivatedUser(user);
        Assert.False(SecureEnvironment.IsUserActivated(user));
    }

    [Fact]
    public void RemoveActivatedUser_NullUser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("user", () => SecureEnvironment.RemoveActivatedUser(null!));
    }

    [Fact]
    public void RemoveActivatedUser_WindowsPassportUser_ThrowsArgumentOutOfRangeException()
    {
        var user = new ContentUser("name", AuthenticationType.WindowsPassport);
        Assert.Throws<ArgumentOutOfRangeException>("user", () => SecureEnvironment.RemoveActivatedUser(user));
    }

    [Theory]
    [InlineData("anyone")]
    [InlineData("owner")]
    public void RemoveActivatedUser_InternalUser_ThrowsArgumentOutOfRangeException(string name)
    {
        var user = new ContentUser(name, AuthenticationType.Internal);
        Assert.Throws<ArgumentOutOfRangeException>("user", () => SecureEnvironment.RemoveActivatedUser(user));
    }
}