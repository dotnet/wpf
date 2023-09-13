#if NETFRAMEWORK
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using Xunit;

namespace System.Xaml.Permissions.Tests;

#pragma warning disable SYSLIB0003

public class XamlLoadPermissionTests
{
    [Theory]
    [InlineData(PermissionState.None)]
    [InlineData(PermissionState.Unrestricted)]
    [InlineData(PermissionState.None - 1)]
    [InlineData(PermissionState.Unrestricted + 1)]
    public void Ctor_PermissionState(PermissionState state)
    {
        var permission = new XamlLoadPermission(state);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Equal(state == PermissionState.Unrestricted, permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#endif
    }

    [Fact]
    public void Ctor_XamlAccessLevel()
    {
        XamlAccessLevel allowedAccess = XamlAccessLevel.PrivateAccessTo(typeof(int));
        var permission = new XamlLoadPermission(allowedAccess);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.False(permission.IsUnrestricted());
        Assert.Equal(new XamlAccessLevel[] { allowedAccess }, permission.AllowedAccess.ToArray());
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#endif
    }

    [Fact]
    public void Ctor_NullAllowedAccess_ThrowsArgumentNullException()
    {
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        var permission = new XamlLoadPermission((XamlAccessLevel)null!);
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Throws<ArgumentNullException>("allowedAccess", () => new XamlLoadPermission((XamlAccessLevel)null!));
#endif
    }

    public static IEnumerable<object[]> Ctor_XamlAccessLevels_TestData()
    {
        yield return new object[] { new XamlAccessLevel[0] };
        yield return new object[] { new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) } };
        yield return new object[] { new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(int)) } };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlAccessLevels_TestData))]
    public void Ctor_XamlAccessLevels(IEnumerable<XamlAccessLevel> allowedAccess)
    {
        var permission = new XamlLoadPermission(allowedAccess);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.False(permission.IsUnrestricted());
        // Seems to include each entry twice.
        AssertEqualXamlAccessLevels(allowedAccess.Concat(allowedAccess).ToArray(), permission.AllowedAccess.ToArray());
#endif
    }

    [Fact]
    public void Ctor_NullAllowedAccesess_ThrowsArgumentNullException()
    {
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        var permission = new XamlLoadPermission((IEnumerable<XamlAccessLevel>)null!);
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Throws<ArgumentNullException>("allowedAccess", () => new XamlLoadPermission((IEnumerable<XamlAccessLevel>)null!));
#endif
    }

    [Fact]
    public void Ctor_NullValueInAllowedAccesess_ThrowsArgumentException()
    {
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        var permission = new XamlLoadPermission(new XamlAccessLevel?[] { null });
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Throws<ArgumentException>("allowedAccess", () => new XamlLoadPermission(new XamlAccessLevel?[] { null }));
#endif
    }

    [Fact]
    public void Copy_Unrestricted_ReturnsExpected()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        XamlLoadPermission copy = Assert.IsType<XamlLoadPermission>(permission.Copy());
        Assert.True(copy.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
    }

    [Fact]
    public void Copy_Restricted_ReturnsExpected()
    {
        var permission = new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) });
        XamlLoadPermission copy = Assert.IsType<XamlLoadPermission>(permission.Copy());
        Assert.Equal(permission.IsUnrestricted(), copy.IsUnrestricted());
        AssertEqualXamlAccessLevels(permission.AllowedAccess.ToArray(), copy.AllowedAccess.ToArray());
    }

    [Fact]
    public void ToXml_Unrestricted_ReturnsExpected()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        SecurityElement element = permission.ToXml();
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.Equal(default, element);
#else
        Assert.Equal("IPermission", element.Tag);
        Assert.Equal(3, element.Attributes.Count);
        Assert.Equal(typeof(XamlLoadPermission).AssemblyQualifiedName, element.Attribute("class"));
        Assert.Equal("1", element.Attribute("version"));
        Assert.Equal("True", element.Attribute("Unrestricted"));
        Assert.Null(element.Children);
#endif
    }

    [Fact]
    public void ToXml_Restricted_ReturnsExpected()
    {
        var permission = new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) });
        SecurityElement element = permission.ToXml();
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.Equal(default, element);
#else
        Assert.Equal("IPermission", element.Tag);
        Assert.Equal(2, element.Attributes!.Count);
        Assert.Equal(typeof(XamlLoadPermission).AssemblyQualifiedName, element.Attribute("class"));
        Assert.Equal("1", element.Attribute("version"));
        Assert.Equal(4, element.Children!.Count);
        
        SecurityElement firstChild = Assert.IsType<SecurityElement>(element.Children[0]);
        Assert.Equal("XamlAccessLevel", firstChild.Tag);
        Assert.Equal(2, firstChild.Attributes!.Count);
        Assert.Equal(typeof(int).Assembly.FullName, firstChild.Attribute("AssemblyName"));
        Assert.Equal(typeof(int).FullName, firstChild.Attribute("TypeName"));

        SecurityElement secondChild = Assert.IsType<SecurityElement>(element.Children[1]);
        Assert.Equal("XamlAccessLevel", secondChild.Tag);
        Assert.Single(secondChild.Attributes!);
        Assert.Equal(typeof(int).Assembly.FullName, secondChild.Attribute("AssemblyName"));
#endif
    }

    public static IEnumerable<object?[]> IsSubsetOf_TestData()
    {
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), null, false    };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), null, true };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.Unrestricted), true };
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.None), false };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermission)) }),
            false
        };
    }

    [Theory]
    [MemberData(nameof(IsSubsetOf_TestData))]
    public void IsSubsetOf_Invoke_ReturnsExpected(XamlLoadPermission permission, IPermission target, bool expected)
    {
        bool result = permission.IsSubsetOf(target);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        var _ = expected;
        Assert.True(result);
#else
        Assert.Equal(expected, result);
#endif
    }

    [Fact]
    public void IsSubsetOf_NotXamlLoadPermission_ThrowsArgumentException()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.True(permission.IsSubsetOf(new CustomPermission()));
#else
        Assert.Throws<ArgumentException>(() => permission.IsSubsetOf(new CustomPermission()));
#endif        
    }

    public static IEnumerable<object[]> Includes_TestData()
    {
        yield return new object[] { new XamlLoadPermission(PermissionState.Unrestricted), XamlAccessLevel.PrivateAccessTo(typeof(int)), true };
        yield return new object[] { new XamlLoadPermission(PermissionState.None), XamlAccessLevel.PrivateAccessTo(typeof(int)), false };
        
        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly),
            true
        };

        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly),
            false
        };

        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            XamlAccessLevel.PrivateAccessTo(typeof(int)),
            false
        };

        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)),
            false
        };

        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            XamlAccessLevel.PrivateAccessTo(typeof(int)),
            true
        };

        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            XamlAccessLevel.PrivateAccessTo(typeof(string)),
            false
        };

        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)),
            false
        };

        yield return new object[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly),
            true
        };
    }

    [Theory]
    [MemberData(nameof(Includes_TestData))]
    public void Includes_Invoke_ReturnsExpected(XamlLoadPermission permission, XamlAccessLevel requestedAccess, bool expected)
    {
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        var _ = expected;
        Assert.True(permission.Includes(requestedAccess));
#else
        Assert.Equal(expected, permission.Includes(requestedAccess));
#endif
    }

    [Fact]
    public void Includes_NullRequestedAccess_ThrowsArgumentNullException()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.True(permission.Includes(null));
#else
        Assert.Throws<ArgumentNullException>("requestedAccess", () => permission.Includes(null));
#endif
    }

    public static IEnumerable<object?[]> Intersect_TestData()
    {
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), null, null };
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.None) };
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.Unrestricted) };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.None) };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.None) };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[0]),
            new XamlLoadPermission(new XamlAccessLevel[0])
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) })
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[0])
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) })
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermission)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) })
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) })
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) })
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[0])
        };
    }

    [Theory]
    [MemberData(nameof(Intersect_TestData))]
    public void Intersect_Invoke_ReturnsExpected(XamlLoadPermission permission, XamlLoadPermission target, XamlLoadPermission expected)
    {
        XamlLoadPermission result = Assert.IsType<XamlLoadPermission>(permission.Intersect(target));
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.NotEqual(expected, result);
        Assert.True(result.IsUnrestricted());
        Assert.Empty(result.AllowedAccess);
        Assert.Same(result.AllowedAccess, result.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(result.AllowedAccess);
#else
        Assert.Equal(expected, result);
#endif
    }

    [Fact]
    public void Intersect_NotXamlLoadPermission_ThrowsArgumentException()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        XamlLoadPermission result = Assert.IsType<XamlLoadPermission>(permission.Intersect(new CustomPermission()));
        Assert.True(result.IsUnrestricted());
        Assert.Empty(result.AllowedAccess);
        Assert.Same(result.AllowedAccess, result.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(result.AllowedAccess);
#else
        Assert.Throws<ArgumentException>(() => permission.Intersect(new CustomPermission()));
#endif
    }

    public static IEnumerable<object?[]> Union_TestData()
    {
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), null, new XamlLoadPermission(PermissionState.Unrestricted) };
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.Unrestricted) };
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.Unrestricted) };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.Unrestricted), new XamlLoadPermission(PermissionState.Unrestricted) };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.None) };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[0]),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) })
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly) }),
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermission)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermission)) })
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)), XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
        };
        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(XamlLoadPermissionTests)) })
        };
    }

    [Theory]
    [MemberData(nameof(Union_TestData))]
    public void Union_Invoke_ReturnsExpected(XamlLoadPermission permission, XamlLoadPermission target, XamlLoadPermission expected)
    {
        XamlLoadPermission result = Assert.IsType<XamlLoadPermission>(permission.Union(target));
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.NotEqual(expected, result);
        Assert.True(result.IsUnrestricted());
        Assert.Empty(result.AllowedAccess);
        Assert.Same(result.AllowedAccess, result.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(result.AllowedAccess);
#else
        Assert.Equal(expected, result);
#endif
    }

    [Fact]
    public void Union_NotXamlLoadPermission_ThrowsArgumentException()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        XamlLoadPermission result = Assert.IsType<XamlLoadPermission>(permission.Union(new CustomPermission()));
        Assert.True(result.IsUnrestricted());
        Assert.Empty(result.AllowedAccess);
        Assert.Same(result.AllowedAccess, result.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(result.AllowedAccess);
#else
        Assert.Throws<ArgumentException>(() => permission.Union(new CustomPermission()));
#endif
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        yield return new object?[] { permission, permission, true };
        yield return new object?[] { permission, new XamlLoadPermission(PermissionState.Unrestricted), true };
        yield return new object?[] { permission, new XamlLoadPermission(PermissionState.None), false };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.None), true };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new XamlLoadPermission(PermissionState.Unrestricted), false };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)), XamlAccessLevel.PrivateAccessTo(typeof(string)) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            true
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.AssemblyAccessTo(typeof(XamlLoadPermissionTests).Assembly) }),
            false
        };

        yield return new object?[]
        {
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly) }),
            new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.PrivateAccessTo(typeof(int)) }),
            false
        };

        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), new CustomPermission(), false };
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), new object(), false };
        yield return new object?[] { new XamlLoadPermission(PermissionState.Unrestricted), null, false };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new CustomPermission(), false };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), new object(), false };
        yield return new object?[] { new XamlLoadPermission(PermissionState.None), null, true };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(XamlLoadPermission permission, object obj, bool expected)
    {
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        var _ = expected;
        Assert.Equal(ReferenceEquals(permission, obj), permission.Equals(obj));
#else
        Assert.Equal(expected, permission.Equals(obj));
#endif
    }
    
    [Fact]
    public void GetHashCode_Invoke_ReturnsExpected()
    {
        var permission = new XamlLoadPermission(PermissionState.Unrestricted);
        Assert.Equal(permission.GetHashCode(), permission.GetHashCode());
    }

    public static IEnumerable<object[]> FromXml_TestData()
    {
        var child1 = new SecurityElement("XamlAccessLevel");
        child1.AddAttribute("AssemblyName", typeof(int).Assembly.FullName!);

        var child2 = new SecurityElement("XamlAccessLevel");
        child2.AddAttribute("AssemblyName", typeof(int).Assembly.FullName!);
        child2.AddAttribute("TypeName", "  " + typeof(int).FullName + "  ");

        var noVersionUnrestricted = new SecurityElement("IPermission");
        noVersionUnrestricted.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        noVersionUnrestricted.AddAttribute("Unrestricted", "True");
        yield return new object[] { noVersionUnrestricted, new XamlLoadPermission(PermissionState.Unrestricted) };

        var versionUnrestricted = new SecurityElement("IPermission");
        versionUnrestricted.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        versionUnrestricted.AddAttribute("version", "1");
        versionUnrestricted.AddAttribute("Unrestricted", "True");
        yield return new object[] { versionUnrestricted, new XamlLoadPermission(PermissionState.Unrestricted) };

        var childrenUnrestricted = new SecurityElement("IPermission");
        childrenUnrestricted.AddAttribute("class", typeof(XamlLoadPermission).FullName + "  ");
        childrenUnrestricted.AddAttribute("Unrestricted", "true");
        childrenUnrestricted.AddChild(child1);
        childrenUnrestricted.AddChild(child2);
        yield return new object[] { noVersionUnrestricted, new XamlLoadPermission(PermissionState.Unrestricted) };

        var noVersionRestricted = new SecurityElement("IPermission");
        noVersionRestricted.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        noVersionRestricted.AddAttribute("version", "1");
        noVersionRestricted.AddAttribute("Unrestricted", "False");
        noVersionRestricted.AddChild(child1);
        noVersionRestricted.AddChild(child2);
        yield return new object[] { noVersionRestricted, new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.PrivateAccessTo(typeof(int)) }) };

        var versionRestricted = new SecurityElement("IPermission");
        versionRestricted.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        versionRestricted.AddAttribute("version", "1");
        versionRestricted.AddAttribute("Unrestricted", "False");
        versionRestricted.AddChild(child1);
        versionRestricted.AddChild(child2);
        yield return new object[] { versionRestricted, new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.PrivateAccessTo(typeof(int)) }) };

        var noChildren = new SecurityElement("IPermission");
        noChildren.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        noChildren.AddAttribute("version", "1");
        yield return new object[] { noChildren, new XamlLoadPermission(PermissionState.None) };

        var children = new SecurityElement("IPermission");
        children.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        children.AddAttribute("version", "1");
        children.AddChild(child1);
        children.AddChild(child2);
        yield return new object[] { children, new XamlLoadPermission(new XamlAccessLevel[] { XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly), XamlAccessLevel.PrivateAccessTo(typeof(int)) }) };
    }

    [Theory]
    [MemberData(nameof(FromXml_TestData))]
    public void FromXml_Invoke_ReturnsExpected(SecurityElement elem, XamlLoadPermission expected)
    {
        var permission = new XamlLoadPermission(PermissionState.None);
        permission.FromXml(elem);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        Assert.NotEqual(expected, permission);
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Equal(expected, permission);
#endif
    }

    [Fact]
    public void FromXml_NullElem_ThrowsArgumentNullException()
    {
        var permission = new XamlLoadPermission(PermissionState.None);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        permission.FromXml(null);
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Throws<ArgumentNullException>("elem", () => permission.FromXml(null));
#endif
    }

    public static IEnumerable<object[]> FromXml_InvalidElem_TestData()
    {
        foreach (string invalidTag in new string[] { "", "tag", "ipermission" })
        {
            yield return new object[] { new SecurityElement(invalidTag) };
        }

        yield return new object[] { new SecurityElement("IPermission") };

        foreach (string invalidClass in new string[] { "", typeof(XamlLoadPermission).FullName!.ToLower() })
        {
            var invalidClassElem = new SecurityElement("IPermission");
            invalidClassElem.AddAttribute("class", invalidClass);
            yield return new object[] { invalidClassElem };
        }

        foreach (string invalidVersion in new string[] { "", "12", "2" })
        {
            var invalidVersionElem = new SecurityElement("IPermission");
            invalidVersionElem.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
            invalidVersionElem.AddAttribute("version", invalidVersion);
            yield return new object[] { invalidVersionElem };
        }

        foreach (string invalidChildTag in new string[] { "", "tag", "xamlaccesslevel" })
        {
            var invalidChildTagElem = new SecurityElement("IPermission");
            invalidChildTagElem.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
            invalidChildTagElem.AddAttribute("version", "1");

            var child = new SecurityElement(invalidChildTag);
            invalidChildTagElem.AddChild(child);

            yield return new object[] { invalidChildTagElem };
        }

        foreach (string? invalidChildAssemblyName in new string?[] { null, "Name" })
        {
            var invalidChildAssemblyNameElem = new SecurityElement("IPermission");
            invalidChildAssemblyNameElem.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
            invalidChildAssemblyNameElem.AddAttribute("version", "1");

            var child = new SecurityElement("XamlAccessLevel");
            if (invalidChildAssemblyName != null)
            {
                child.AddAttribute("AssemblyName", invalidChildAssemblyName);
            }
            invalidChildAssemblyNameElem.AddChild(child);

            yield return new object[] { invalidChildAssemblyNameElem };
        }
    }

    [Theory]
    [MemberData(nameof(FromXml_InvalidElem_TestData))]
    public void FromXml_InvalidElem_ThrowsArgumentException(SecurityElement elem)
    {
        var permission = new XamlLoadPermission(PermissionState.None);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        permission.FromXml(elem);
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Throws<ArgumentException>("elem", () => permission.FromXml(elem));
#endif
    }

    [Theory]
    [InlineData("")]
    [InlineData("\0Name")]
    public void FromXml_InvalidChildAssemblyName_ThrowsArgumentException(string name)
    {
        var elem = new SecurityElement("IPermission");
        elem.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        elem.AddAttribute("version", "1");

        var child = new SecurityElement("XamlAccessLevel");
        child.AddAttribute("AssemblyName", name);
        elem.AddChild(child);

        var permission = new XamlLoadPermission(PermissionState.None);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        permission.FromXml(elem);
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Throws<ArgumentException>(() => permission.FromXml(elem));
#endif
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void FromXml_InvalidUnrestricted_ThrowsFormatException(string value)
    {
        var elem = new SecurityElement("IPermission");
        elem.AddAttribute("class", typeof(XamlLoadPermission).FullName!);
        elem.AddAttribute("Unrestricted", value);

        var permission = new XamlLoadPermission(PermissionState.None);
        // XamlLoadPermission no longer has any functionality.
#if !NETFRAMEWORK
        permission.FromXml(elem);
        Assert.True(permission.IsUnrestricted());
        Assert.Empty(permission.AllowedAccess);
        Assert.Same(permission.AllowedAccess, permission.AllowedAccess);
        Assert.IsType<ReadOnlyCollection<XamlAccessLevel>>(permission.AllowedAccess);
#else
        Assert.Throws<FormatException>(() => permission.FromXml(elem));
#endif
    }

    private static void AssertEqualXamlAccessLevels(XamlAccessLevel[] expected, XamlAccessLevel[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].AssemblyAccessToAssemblyName.FullName, actual[i].AssemblyAccessToAssemblyName.FullName);
            Assert.Equal(expected[i].PrivateAccessToTypeName, actual[i].PrivateAccessToTypeName);
        }
    }

    private class CustomPermission : IPermission
    {
        public IPermission Copy() => throw new NotImplementedException();

        public void Demand() => throw new NotImplementedException();

        public void FromXml(SecurityElement e) => throw new NotImplementedException();

        public IPermission Intersect(IPermission? target) => throw new NotImplementedException();

        public bool IsSubsetOf(IPermission? target) => throw new NotImplementedException();

        public SecurityElement ToXml() => throw new NotImplementedException();

        public IPermission? Union(IPermission? target) => throw new NotImplementedException();
    }
}

#pragma warning restore 0618
#endif
