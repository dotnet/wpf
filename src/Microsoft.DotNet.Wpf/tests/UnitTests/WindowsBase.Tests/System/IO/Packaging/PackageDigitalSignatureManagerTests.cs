// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.IO.Packaging.Tests;

public class PackageDigitalSignatureManagerTests
{
    // TODO:
    // Signatures - signed.
    // SignatureOrigin - signed.
    // Countersign - signed.
    // GetSignature - signed.
    // RemoveAllSignatures - signed.
    // RemoveSignature - signed.
    // Sign
    // VerifySignatures - signed.

    [Fact]
    public void Ctor_Package()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Equal(CertificateEmbeddingOption.InCertificatePart, manager.CertificateOption);
        Assert.Equal("http://www.w3.org/2001/04/xmlenc#sha256", manager.HashAlgorithm);
        Assert.False(manager.IsSigned);
        Assert.Equal(IntPtr.Zero, manager.ParentWindow);
        Assert.Equal(new Uri("/package/services/digital-signature/origin.psdsor", UriKind.Relative), manager.SignatureOrigin);
        Assert.Empty(manager.Signatures);
        Assert.Same(manager.Signatures, manager.Signatures);
        Assert.Equal("YYYY-MM-DDThh:mm:ss.sTZD", manager.TimeFormat);
        Assert.NotEmpty(manager.TransformMapping);
        Assert.Same(manager.TransformMapping, manager.TransformMapping);
        Assert.Equal(2, manager.TransformMapping.Count);
        Assert.Equal("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", manager.TransformMapping["application/vnd.openxmlformats-package.relationships+xml"]);
        Assert.Equal("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", manager.TransformMapping["application/vnd.openxmlformats-package.digital-signature-xmlsignature+xml"]);
    }

    [Fact]
    public void Ctor_NullPackage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("package", () => new PackageDigitalSignatureManager(null));
    }

    [Fact]
    public void DefaultHashAlgorithm_Get_ReturnsExpected()
    {
        Assert.Equal("http://www.w3.org/2001/04/xmlenc#sha256", PackageDigitalSignatureManager.DefaultHashAlgorithm);
    }

    [Fact]
    public void SignatureOriginRelationshipType_Get_ReturnsExpected()
    {
        Assert.Equal("http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin", PackageDigitalSignatureManager.SignatureOriginRelationshipType);
    }

    [Fact]
    public void VerifyCertificate_NullCertificate_ThrowsArgumentNullException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentNullException>("certificate", () => PackageDigitalSignatureManager.VerifyCertificate(null));
    }

    [Fact]
    public void VerifyCertificate_InvokeDefaultCertificate_ThrowsArgumentException()
    {
#pragma warning disable SYSLIB0026
        var c = new X509Certificate();
#pragma warning restore 0618
        Assert.Throws<ArgumentException>("handle", () => PackageDigitalSignatureManager.VerifyCertificate(c));
    }

    [Theory]
    [InlineData(CertificateEmbeddingOption.InCertificatePart)]
    [InlineData(CertificateEmbeddingOption.InSignaturePart)]
    [InlineData(CertificateEmbeddingOption.NotEmbedded)]
    public void CertificateOption_Set_GetReturnsExpected(CertificateEmbeddingOption value)
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package)
        {
            // Set.
            CertificateOption = value
        };
        Assert.Equal(value, manager.CertificateOption);

        // Set same.
        manager.CertificateOption = value;
        Assert.Equal(value, manager.CertificateOption);
    }

    [Theory]
    [InlineData(CertificateEmbeddingOption.InCertificatePart - 1)]
    [InlineData(CertificateEmbeddingOption.NotEmbedded + 1)]
    public void CertificateOption_SetInvalid_ThrowsArgumentOutOfRangeException(CertificateEmbeddingOption value)
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentOutOfRangeException>("value", () => manager.CertificateOption = value);
    }

    [Theory]
    [InlineData("http://www.w3.org/2001/04/xmlenc#sha256")]
    [InlineData("value")]
    [InlineData("  ")]
    public void HashAlgorithm_Set_GetReturnsExpected(string value)
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package)
        {
            // Set.
            HashAlgorithm = value
        };
        Assert.Equal(value, manager.HashAlgorithm);

        // Set same.
        manager.HashAlgorithm = value;
        Assert.Equal(value, manager.HashAlgorithm);
    }

    [Fact]
    public void HashAlgortithm_SetNull_ThrowsArgumentNullException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentNullException>("value", () => manager.HashAlgorithm = null);
    }

    [Fact]
    public void HashAlgorithm_SetEmpty_ThrowsArgumentException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentException>("value", () => manager.HashAlgorithm = string.Empty);
    }
    
    public static IEnumerable<object[]> ParentWindow_Set_TestData()
    {
        yield return new object[] { (IntPtr)(-1) };
        yield return new object[] { IntPtr.Zero };
        yield return new object[] { (IntPtr)1 };
    }

    [Theory]
    [MemberData(nameof(ParentWindow_Set_TestData))]
    public void ParentWindow_Set_GetReturnsExpected(IntPtr value)
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package)
        {
            // Set.
            ParentWindow = value
        };
        Assert.Equal(value, manager.ParentWindow);

        // Set same.
        manager.ParentWindow = value;
        Assert.Equal(value, manager.ParentWindow);
    }

    [Theory]
    [InlineData("YYYY-MM-DDThh:mm:ss.sTZD")]
    [InlineData("YYYY-MM-DDThh:mm:ssTZD")]
    [InlineData("YYYY-MM-DDThh:mmTZD")]
    [InlineData("YYYY-MM-DD")]
    [InlineData("YYYY-MM")]                 
    [InlineData("YYYY")]
    public void TimeFormat_Set_GetReturnsExpected(string value)
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package)
        {
            // Set.
            TimeFormat = value
        };
        Assert.Equal(value, manager.TimeFormat);

        // Set same.
        manager.TimeFormat = value;
        Assert.Equal(value, manager.TimeFormat);
    }

    [Fact]
    public void TimeFormat_SetNull_ThrowsArgumentNullException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentNullException>("value", () => manager.TimeFormat = null);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("value")]
    [InlineData("yyyy-MM-ddTHH:mm:ss.fzzz")]
    [InlineData("yyyy-MM-ddTHH:mm:sszzz")] 
    [InlineData("yyyy-MM-ddTHH:mmzzz")]   
    [InlineData("yyyy-MM-dd")]
    [InlineData("yyyy-MM")]
    [InlineData("yyyy")] 
    [InlineData("yyyy-MM-ddTHH:mm:ss.fZ")]
    [InlineData("yyyy-MM-ddTHH:mm:ssZ")]
    [InlineData("yyyy-MM-ddTHH:mmZ")]
    [InlineData(" YYYY ")]
    public void TimeFormat_SetInvalid_ThrowsFormatException(string value)
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<FormatException>(() => manager.TimeFormat = value);
    }

    [Fact]
    public void Countersign_NotSigned_ThrowsInvalidOperationException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<InvalidOperationException>(() => manager.Countersign());

#pragma warning disable SYSLIB0026
        var c = new X509Certificate();
#pragma warning restore 0618
        Assert.Throws<InvalidOperationException>(() => manager.Countersign(c));
        Assert.Throws<InvalidOperationException>(() => manager.Countersign(c, Array.Empty<Uri>()));
    }

    [Fact]
    public void Countersign_NullCertificate_ThrowsArgumentNullException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentNullException>("certificate", () => manager.Countersign(null));
        Assert.Throws<ArgumentNullException>("certificate", () => manager.Countersign(null, Array.Empty<Uri>()));
    }

    [Fact]
    public void Countersign_NullSignatures_ThrowsArgumentNullException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        
#pragma warning disable SYSLIB0026
        var c = new X509Certificate();
#pragma warning restore 0618
        Assert.Throws<ArgumentNullException>("signatures", () => manager.Countersign(c, null));
    }

    [Fact]
    public void GetSignature_InvokeNotSigned_ReturnsNull()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Null(manager.GetSignature(new Uri("http://microsoft.com")));
    }

    [Fact]
    public void GetSignature_NullSignatureUri_ThrowsArgumentNullException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentNullException>("signatureUri", () => manager.GetSignature(null));
    }

    [Fact]
    public void RemoveAllSignatures_InvokeNotSigned_Nop()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        int callCount = 0;
        var parts = new List<Uri>();
        package.DeletePartCoreAction = (uri) =>
        {
            parts.Add(uri);
            callCount++;
        };

        // Remove.
        manager.RemoveAllSignatures();
        Assert.Equal(2, callCount);
        Assert.Equal(new Uri[] { new Uri("/package/services/digital-signature/origin.psdsor", UriKind.Relative), new Uri("/package/services/digital-signature/_rels/origin.psdsor.rels", UriKind.Relative) }, parts);
        Assert.Empty(manager.Signatures);
        Assert.Same(manager.Signatures, manager.Signatures);

        // Remove again.
        manager.RemoveAllSignatures();
        Assert.Equal(4, callCount);
        Assert.Equal(new Uri[] { new Uri("/package/services/digital-signature/origin.psdsor", UriKind.Relative), new Uri("/package/services/digital-signature/_rels/origin.psdsor.rels", UriKind.Relative), new Uri("/package/services/digital-signature/origin.psdsor", UriKind.Relative), new Uri("/package/services/digital-signature/_rels/origin.psdsor.rels", UriKind.Relative) }, parts);
        Assert.Empty(manager.Signatures);
        Assert.Same(manager.Signatures, manager.Signatures);
    }

    [Fact]
    public void RemoveSignature_InvokeNotSigned_Nop()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        int callCount = 0;
        package.DeletePartCoreAction = (uri) => callCount++;

        // Remove.
        manager.RemoveSignature(new Uri("https://microsoft.com"));
        Assert.Equal(0, callCount);
        Assert.Empty(manager.Signatures);
        Assert.Same(manager.Signatures, manager.Signatures);

        // Remove again.
        manager.RemoveSignature(new Uri("https://microsoft.com"));
        Assert.Equal(0, callCount);
        Assert.Empty(manager.Signatures);
        Assert.Same(manager.Signatures, manager.Signatures);
    }

    [Fact]
    public void RemoveSignature_NullSignatureUri_ThrowsArgumentNullException()
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Throws<ArgumentNullException>("signatureUri", () => manager.RemoveSignature(null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VerifySignatures_InvokeNotSigned_ReturnsNotSigned(bool exitOnFailure)
    {
        var package = new CustomPackage(FileAccess.ReadWrite);
        var manager = new PackageDigitalSignatureManager(package);
        Assert.Equal(VerifyResult.NotSigned, manager.VerifySignatures(exitOnFailure));
    }

    private class CustomPackage : Package
    {
        public CustomPackage(FileAccess openFileAccess) : base(openFileAccess)
        {
        }

        protected override PackagePart CreatePartCore(Uri partUri, string contentType, CompressionOption compressionOption)
        {
            throw new NotImplementedException();
        }

        public Action<Uri>? DeletePartCoreAction { get; set; }

        protected override void DeletePartCore(Uri partUri)
        {
            if (DeletePartCoreAction is null)
            {
                throw new NotImplementedException();
            }

            DeletePartCoreAction(partUri);
        }

        protected override void FlushCore()
        {
            throw new NotImplementedException();
        }

        protected override PackagePart? GetPartCore(Uri partUri)
        {
            throw new NotImplementedException();
        }

        protected override PackagePart[] GetPartsCore()
        {
            throw new NotImplementedException();
        }
    }
}
