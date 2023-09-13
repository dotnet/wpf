#if NETFRAMEWORK
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Xaml.Permissions.Tests;

public class XamlAccessLevelTests
{
    [Fact]
    public void AssemblyAccessTo_Assembly_ReturnsExpected()
    {
        XamlAccessLevel access = XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly);
        Assert.Equal(typeof(int).Assembly.FullName, access.AssemblyAccessToAssemblyName.FullName);
        Assert.Null(access.PrivateAccessToTypeName);
    }

    [Fact]
    public void AssemblyAccessTo_NullAssembly_ThrowsArgumentNullException()
    {
        // TODO: should throw ANE.
        //Assert.Throws<ArgumentNullException>("assembly", () => XamlAccessLevel.AssemblyAccessTo((Assembly)null!));
        Assert.Throws<NullReferenceException>(() => XamlAccessLevel.AssemblyAccessTo((Assembly)null!));
    }

    public static IEnumerable<object[]> AssemblyAccessTo_TestData()
    {
        yield return new object[] { new AssemblyName(typeof(int).Assembly.FullName!) };
        yield return new object[]
        {
            new AssemblyName
            {
                Name = "name",
                Version = new Version(1, 2),
                CultureInfo = CultureInfo.InvariantCulture,
            }
        };      
    }

    [Theory]
    [MemberData(nameof(AssemblyAccessTo_TestData))]
    public void AssemblyAccessTo_AssemblyName_ReturnsExpected(AssemblyName assemblyName)
    {
        XamlAccessLevel access = XamlAccessLevel.AssemblyAccessTo(assemblyName);
        Assert.NotSame(assemblyName, access.AssemblyAccessToAssemblyName);
        Assert.Equal(assemblyName.FullName, access.AssemblyAccessToAssemblyName.FullName);
        Assert.Null(access.PrivateAccessToTypeName);
    }

    [Fact]
    public void AssemblyAccessTo_NullAssemblyName_ThrowsArgumentNullException()
    {
        // TODO: should throw ANE
        //Assert.Throws<ArgumentNullException>("assemblyName", () => XamlAccessLevel.AssemblyAccessTo((AssemblyName)null!));
        Assert.Throws<NullReferenceException>(() => XamlAccessLevel.AssemblyAccessTo((AssemblyName)null!));
    }

    public static IEnumerable<object[]> InvalidAssemblyName_TestData()
    {
        static AssemblyName SetPublicKeyToken(AssemblyName name)
        {
            name.SetPublicKeyToken(new byte[] { 1, 2, 3 });
            return name;
        }

        yield return new object[] { new AssemblyName() };
        yield return new object[]
        {
            SetPublicKeyToken(new AssemblyName
            {
                Name = null,
                Version = new Version(1, 2),
                CultureInfo = CultureInfo.InvariantCulture,
            })
        };
        yield return new object[]
        {
            SetPublicKeyToken(new AssemblyName
            {
                Name = "name",
                Version = null,
                CultureInfo = CultureInfo.InvariantCulture,
            })
        };
        yield return new object[]
        {
            SetPublicKeyToken(new AssemblyName
            {
                Name = "name",
                Version = new Version(1, 2),
                CultureInfo = null,
            })
        };
    }

    [Theory]
    [MemberData(nameof(InvalidAssemblyName_TestData))]
    public void AssemblyAccessTo_InvalidAssemblyName_Success(AssemblyName assemblyName)
    {
        XamlAccessLevel access = XamlAccessLevel.AssemblyAccessTo(assemblyName);
        Assert.ThrowsAny<Exception>(() => access.AssemblyAccessToAssemblyName);
        Assert.Null(access.PrivateAccessToTypeName);
    }

    [Fact]
    public void PrivateAccessTo_Type_ReturnsExpected()
    {
        XamlAccessLevel access = XamlAccessLevel.PrivateAccessTo(typeof(int));
        Assert.Equal(typeof(int).Assembly.FullName, access.AssemblyAccessToAssemblyName.FullName);
        Assert.Equal(typeof(int).FullName, access.PrivateAccessToTypeName);
    }

    [Fact]
    public void PrivateAccessTo_NullType_ThrowsArgumentNullException()
    {
        // TODO: should throw ANE.
        //Assert.Throws<ArgumentNullException>("type", () => XamlAccessLevel.PrivateAccessTo((Type)null!));
        Assert.Throws<NullReferenceException>(() => XamlAccessLevel.PrivateAccessTo((Type)null!));
    }

    [Fact]
    public void PrivateAccessTo_String_ReturnsExpected()
    {
        XamlAccessLevel access = XamlAccessLevel.PrivateAccessTo(typeof(int).AssemblyQualifiedName!);
        Assert.Equal(typeof(int).Assembly.FullName, access.AssemblyAccessToAssemblyName.FullName);
        Assert.Equal(typeof(int).FullName, access.PrivateAccessToTypeName);
    }

    [Fact]
    public void PrivateAccessTo_NullAssemblyQualifiedTypeName_ThrowsArgumentNullException()
    {
        // TODO: should throw ANE.
        //Assert.Throws<ArgumentNullException>("assemblyQualifiedTypeName", () => XamlAccessLevel.PrivateAccessTo((string)null!));
        Assert.Throws<NullReferenceException>(() => XamlAccessLevel.PrivateAccessTo((string)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("assemblyQualifiedTypeName")]
    public void PrivateAccessTo_InvalidAssemblyQualfiiedTypeName_ThrowsArgumentOutOfRangeException(string assemblyQualifiedName)
    {
        // TODO: should throw ArgumentException
        //Assert.Throws<ArgumentException>("assemblyQualifiedTypeName", () => XamlAccessLevel.PrivateAccessTo(assemblyQualifiedName));
        Assert.Throws<ArgumentOutOfRangeException>("start", () => XamlAccessLevel.PrivateAccessTo(assemblyQualifiedName));
    }
}
#endif
