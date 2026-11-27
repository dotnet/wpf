// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Windows.Navigation.Tests;

/// <summary>
/// Tests for <see cref="BaseUriHelper.CreateAssemblyName(string, string, string)"/>, the assembly
/// identity a pack URI names.
/// </summary>
public class BaseUriHelperTests
{
    [Fact]
    public void CreateAssemblyName_UriWithoutVersionOrToken_NamesAnyVersion()
    {
        AssemblyName name = BaseUriHelper.CreateAssemblyName("Lib", string.Empty, string.Empty);

        Assert.Equal("Lib", name.Name);
        Assert.Null(name.Version);
        Assert.Null(name.GetPublicKeyToken());
        Assert.Equal(string.Empty, name.CultureName);
    }

    [Fact]
    public void CreateAssemblyName_UriWithVersionAndToken_KeepsBoth()
    {
        AssemblyName name = BaseUriHelper.CreateAssemblyName("Lib", "2.0.0.0", "31bf3856ad364e35");

        Assert.Equal("Lib", name.Name);
        Assert.Equal(new Version(2, 0, 0, 0), name.Version);
        Assert.Equal(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 }, name.GetPublicKeyToken());
        Assert.Equal(string.Empty, name.CultureName);
    }
}
