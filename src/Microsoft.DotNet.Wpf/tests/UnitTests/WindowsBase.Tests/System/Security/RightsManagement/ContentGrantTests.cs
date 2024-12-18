// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement.Tests;
 
public class ContentGrantTests
{
    public static IEnumerable<object[]> Ctor_ContentUser_ContentRight_TestData()
    {
        yield return new object[] { ContentUser.AnyoneUser, ContentRight.View };
        yield return new object[] { ContentUser.OwnerUser, ContentRight.Edit };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Print };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Extract };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.ObjectModel };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Owner };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.ViewRightsData };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Forward };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Reply };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.ReplyAll };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Sign };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.DocumentEdit };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Export };
    }

    [Theory]
    [MemberData(nameof(Ctor_ContentUser_ContentRight_TestData))]
    public void Ctor_ContentUser_ContentRight(ContentUser user, ContentRight right)
    {
        var grant = new ContentGrant(user, right);
        Assert.Equal(user, grant.User);
        Assert.Equal(right, grant.Right);
        Assert.Equal(DateTime.MinValue, grant.ValidFrom);
        Assert.Equal(DateTime.MaxValue, grant.ValidUntil);
    }
    public static IEnumerable<object[]> Ctor_ContentUser_ContentRight_DateTime_DateTime_TestData()
    {
        yield return new object[] { ContentUser.AnyoneUser, ContentRight.View, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { ContentUser.OwnerUser, ContentRight.Edit, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Print, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Extract, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.ObjectModel, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Owner, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.ViewRightsData, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Forward, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Reply, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.ReplyAll, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Sign, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.DocumentEdit, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Export, DateTime.MinValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Export, DateTime.MinValue, DateTime.MinValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Export, DateTime.MaxValue, DateTime.MaxValue };
        yield return new object[] { new ContentUser("name", AuthenticationType.Windows), ContentRight.Export, new DateTime(2023, 01, 01), new DateTime(2023, 01, 02) };
    }

    [Theory]
    [MemberData(nameof(Ctor_ContentUser_ContentRight_DateTime_DateTime_TestData))]
    public void Ctor_ContentUser_ContentRight_DateTime_DateTime(ContentUser user, ContentRight right, DateTime validFrom, DateTime validUntil)
    {
        var grant = new ContentGrant(user, right, validFrom, validUntil);
        Assert.Equal(user, grant.User);
        Assert.Equal(right, grant.Right);
        Assert.Equal(validFrom, grant.ValidFrom);
        Assert.Equal(validUntil, grant.ValidUntil);
    }

    [Fact]
    public void Ctor_NullUser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("user", () => new ContentGrant(null, ContentRight.View));
        Assert.Throws<ArgumentNullException>("user", () => new ContentGrant(null, ContentRight.View, DateTime.MinValue, DateTime.MaxValue));
    }

    [Theory]
    [InlineData(ContentRight.View - 1)]
    [InlineData(ContentRight.Export + 1)]
    public void Ctor_ContentUser_ContentRight_InvalidRight(ContentRight right)
    {
        var user = new ContentUser("name", AuthenticationType.Windows);
        Assert.Throws<ArgumentOutOfRangeException>("right", () => new ContentGrant(user, right));
    }

    [Fact]
    public void Ctor_InvalidValidFrom_ThrowsArgumentOutOfRangeException()
    {
        var user = new ContentUser("name", AuthenticationType.Windows);
        Assert.Throws<ArgumentOutOfRangeException>("validFrom", () => new ContentGrant(user, ContentRight.View, new DateTime(2023, 01, 01), new DateTime(2023, 01, 01).AddTicks(-1)));
    }
}