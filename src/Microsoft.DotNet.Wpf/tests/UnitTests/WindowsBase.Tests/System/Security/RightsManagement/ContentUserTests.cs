// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement.Tests;

public class ContentUserTests
{
    [Theory]
    [InlineData("name", AuthenticationType.Windows)]
    [InlineData("NaMe", AuthenticationType.Passport)]
    [InlineData("name", AuthenticationType.WindowsPassport)]
    [InlineData("  name  ", AuthenticationType.Windows)]
    [InlineData("anyone", AuthenticationType.Windows)]
    [InlineData("anyone", AuthenticationType.Passport)]
    [InlineData("anyone", AuthenticationType.WindowsPassport)]
    [InlineData("anyone", AuthenticationType.Internal)]
    [InlineData("ANYONE", AuthenticationType.Internal)]
    [InlineData("owner", AuthenticationType.Internal)]
    [InlineData("OWNER", AuthenticationType.Internal)]
    public void Ctor_String_AuthenticationType(string name, AuthenticationType authenticationType)
    {
        var user = new ContentUser(name, authenticationType);
        Assert.Equal(name, user.Name);
        Assert.Equal(authenticationType, user.AuthenticationType);
    }

    [Fact]
    public void Ctor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("name", () => new ContentUser(null!, AuthenticationType.Windows));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Ctor_InvalidName_ThrowsArgumentOutOfRangeException(string name)
    {
        Assert.Throws<ArgumentOutOfRangeException>("name", () => new ContentUser(name, AuthenticationType.Windows));
    }

    [Theory]
    [InlineData(AuthenticationType.Windows - 1)]
    [InlineData(AuthenticationType.Internal + 1)]
    public void Ctor_InvalidAuthenticationType_ThrowsArgumentOutOfRangeException(AuthenticationType authenticationType)
    {
        Assert.Throws<ArgumentOutOfRangeException>("authenticationType", () => new ContentUser("name", authenticationType));
    }

    [Theory]
    [InlineData("name")]
    [InlineData(" anyone ")]
    [InlineData(" owner ")]
    public void Ctor_InvalidInternalName_ThrowsArgumentOutOfRangeException(string name)
    {
        Assert.Throws<ArgumentOutOfRangeException>("name", () => new ContentUser(name, AuthenticationType.Internal));
    }

    [Fact]
    public void AnyoneUser_Get_ReturnsExpected()
    {
        ContentUser user = ContentUser.AnyoneUser;
        Assert.Same(user, ContentUser.AnyoneUser);
        Assert.Equal("Anyone", user.Name);
        Assert.Equal(AuthenticationType.Internal, user.AuthenticationType);
    }

    [Fact]
    public void OwnerUser_Get_ReturnsExpected()
    {
        ContentUser user = ContentUser.OwnerUser;
        Assert.Same(user, ContentUser.OwnerUser);
        Assert.Equal("Owner", user.Name);
        Assert.Equal(AuthenticationType.Internal, user.AuthenticationType);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new ContentUser("name", AuthenticationType.Windows), true };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new SubContentUser("name", AuthenticationType.Windows), false };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new ContentUser("NAME", AuthenticationType.Windows), true };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new ContentUser("nAME", AuthenticationType.Windows), true };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new ContentUser(" name ", AuthenticationType.Windows), false };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new ContentUser("name2", AuthenticationType.Windows), false };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new ContentUser("name", AuthenticationType.WindowsPassport), false };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new object(), false };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), null, false };
        
        yield return new object?[] { new SubContentUser("name", AuthenticationType.Windows), new SubContentUser("name", AuthenticationType.Windows), true };
        yield return new object?[] { new SubContentUser("name", AuthenticationType.Windows), new ContentUser("name", AuthenticationType.Windows), false };
        yield return new object?[] { new ContentUser("name", AuthenticationType.Windows), new SubContentUser("name", AuthenticationType.Windows), false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(ContentUser user, object obj, bool expected)
    {
        Assert.Equal(expected, user.Equals(obj));
        if (user?.GetType() == obj?.GetType())
        {
            Assert.Equal(expected, user!.GetHashCode().Equals(obj!.GetHashCode()));
        }
    }

    [Fact]
    public void IsAuthenticated_InvokeWindows_ReturnsFalse()
    {
        var user = new ContentUser("name", AuthenticationType.Windows);
        Assert.False(user.IsAuthenticated());
    }

    [Fact]
    public void IsAuthenticated_InvokePassport_ReturnsFalse()
    {
        var user = new ContentUser("name", AuthenticationType.Passport);
        Assert.False(user.IsAuthenticated());
    }

    [Fact]
    public void IsAuthenticated_InvokeWindowsPassport_ReturnsFalse()
    {
        var user = new ContentUser("name", AuthenticationType.WindowsPassport);
        Assert.False(user.IsAuthenticated());
    }

    [Theory]
    [InlineData("anyone")]
    [InlineData("owner")]
    public void IsAuthenticated_InvokeInternal_ReturnsFalse(string name)
    {
        var user = new ContentUser(name, AuthenticationType.Internal);
        Assert.False(user.IsAuthenticated());
    }

    private class SubContentUser : ContentUser
    {
        public SubContentUser(string name, AuthenticationType authenticationType) : base(name, authenticationType)
        {
        }
    }
}