// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Xunit;

namespace System.Windows.Markup.Tests;

public class XDataTests
{
    [Fact]
    public void Ctor_Default()
    {
        var data = new XData();
        Assert.Null(data.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("text")]
    public void Text_Set_GetReturnsExpected(string value)
    {
        var data = new XData { Text = value };
        Assert.Equal(value, data.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<Text />")]
    public void XmlReader_GetWithText_ReturnsExpected(string text)
    {
        var data = new XData { Text = text };
        XmlReader reader = Assert.IsAssignableFrom<XmlReader>(data.XmlReader);
        Assert.Same(data.XmlReader, reader);
    }

    [Fact]
    public void XmlReader_GetWhenNullText_ThrowsArgumentNullException()
    {
        var data = new XData();
        Assert.Throws<ArgumentNullException>("s", () => data.XmlReader);
    }

    [Fact]
    public void XmlReader_SetReader_GetReturnsExpected()
    {
        XmlReader reader = XmlReader.Create(new StringReader("<Text />"));
        var data = new XData
        {
            Text = "text",
            XmlReader = reader
        };
        Assert.Null(data.Text);
        Assert.Same(reader, data.XmlReader);
    }

    [Fact]
    public void XmlReader_SetObject_GetReturnsExpected()
    {
        var data = new XData
        {
            Text = "text",
            XmlReader = new object()
        };
        Assert.Null(data.Text);
        Assert.Throws<ArgumentNullException>("s", () => data.XmlReader);
    }
}
