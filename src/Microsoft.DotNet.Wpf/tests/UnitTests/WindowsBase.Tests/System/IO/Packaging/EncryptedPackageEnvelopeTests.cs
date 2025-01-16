// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Tests;

namespace System.IO.Packaging.Tests;

public class EncryptedPackageEnvelopeTests
{
    [Fact]
    public void Open_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("envelopeStream", () => EncryptedPackageEnvelope.Open((Stream)null!));
    }

    [Fact]
    public void Open_WriteOnlyStream_ThrowsArgumentException()
    {
        using var stream = new WriteOnlyStream();
        Assert.Throws<ArgumentException>(() => EncryptedPackageEnvelope.Open(stream));
    }

    [Fact]
    public void Open_InvalidStream_ThrowsIOException()
    {
        using var stream = new MemoryStream();
        Assert.Throws<IOException>(() => EncryptedPackageEnvelope.Open(stream));
    }
    
    [Fact]
    public void Open_NullEnvelopeFileName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("envelopeFileName", () => EncryptedPackageEnvelope.Open((string)null!));
        Assert.Throws<ArgumentNullException>("envelopeFileName", () => EncryptedPackageEnvelope.Open(null!, FileAccess.Read));
        Assert.Throws<ArgumentNullException>("envelopeFileName", () => EncryptedPackageEnvelope.Open(null!, FileAccess.Read, FileShare.Read));
    }
    
    [Fact]
    public void Open_DocumentDoesNotContainPackage_ThrowsFileFormatException()
    {
        string path = Helpers.GetResourcePath("test-ole-file.doc");
        using var stream = File.OpenRead(path);
        Assert.Throws<FileFormatException>(() => EncryptedPackageEnvelope.Open(stream));
    }
    
    [Fact]
    public void Open_DocumentDoesNotContainRightsManagementProtectedStream_ThrowsFileFormatException()
    {
        string path = Helpers.GetResourcePath("Invalid_1.xps");
        using var stream = File.OpenRead(path);
        Assert.Throws<FileFormatException>(() => EncryptedPackageEnvelope.Open(stream));
    }

    private class ReadOnlyStream : MemoryStream
    {
        public override bool CanWrite => false;
    }

    private class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
