// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Xunit;

namespace System.Xaml.Tests;

public class XamlServicesTests
{
    [Fact]
    public void Parse_NullXaml_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xaml", () => XamlServices.Parse(null));
    }

    [Fact]
    public void Load_NullFileName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("fileName", () => XamlServices.Load((string)null!));
    }

    [Fact]
    public void Load_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => XamlServices.Load((Stream)null!));
    }

    [Fact]
    public void Load_NullTextReader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("textReader", () => XamlServices.Load((TextReader)null!));
    }

    [Fact]
    public void Load_NullXmlReader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xmlReader", () => XamlServices.Load((XmlReader)null!));
    }

    [Fact]
    public void Load_NullXamlReader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xamlReader", () => XamlServices.Load((XamlReader)null!));
    }

    [Fact]
    public void Transform_NullXamlReader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xamlReader", () => XamlServices.Transform((XamlReader)null!, new XamlObjectWriter(new XamlSchemaContext())));
        Assert.Throws<ArgumentNullException>("xamlReader", () => XamlServices.Transform((XamlReader)null!, new XamlObjectWriter(new XamlSchemaContext()), false));
    }

    [Fact]
    public void Transform_NullXamlWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xamlWriter", () => XamlServices.Transform(new XamlObjectReader(1), null));
        Assert.Throws<ArgumentNullException>("xamlWriter", () => XamlServices.Transform(new XamlObjectReader(1), null, false));
    }
    
    [Fact]
    public void Save_NullFileName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("fileName", () => XamlServices.Save((string)null!, 1));
    }
    
    [Fact]
    public void Save_EmptyFileName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("fileName", () => XamlServices.Save("", 1));
    }
    
    [Fact]
    public void Save_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => XamlServices.Save((Stream)null!, 1));
    }
    
    [Fact]
    public void Save_NullTextWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("writer", () => XamlServices.Save((TextWriter)null!, 1));
    }
    
    [Fact]
    public void Save_NullXmlWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("writer", () => XamlServices.Save((XmlWriter)null!, 1));
    }

    [Fact]
    public void Save_NullXamlWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("writer", () => XamlServices.Save((XamlWriter)null!, 1));
    }
}
