// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Tests;

public class XamlReaderTests
{
    [Theory]
    [InlineData(XamlNodeType.None)]
    [InlineData(XamlNodeType.GetObject)]
    [InlineData(XamlNodeType.EndObject)]
    [InlineData(XamlNodeType.EndMember)]
    [InlineData(XamlNodeType.Value)]
    [InlineData(XamlNodeType.NamespaceDeclaration)]
    [InlineData(XamlNodeType.NamespaceDeclaration + 1)]
    public void Skip_NotSkippable_Reads(XamlNodeType nodeType)
    {
        var reader = new SubXamlReader(nodeType);
        reader.Skip();
        Assert.Equal(1, reader.ReadCount);
    }

    public static IEnumerable<object[]> Skip_Skippable_TestData()
    {
        yield return new object[] { new XamlNodeType[] { XamlNodeType.StartObject, XamlNodeType.StartMember, XamlNodeType.EndMember, XamlNodeType.Value, XamlNodeType.None, XamlNodeType.EndObject, XamlNodeType.Value }, 6 };
        yield return new object[] { new XamlNodeType[] { XamlNodeType.StartObject, XamlNodeType.StartMember, XamlNodeType.EndMember, XamlNodeType.Value, XamlNodeType.StartObject, XamlNodeType.None, XamlNodeType.EndObject, XamlNodeType.EndObject, XamlNodeType.Value }, 8 };
        yield return new object[] { new XamlNodeType[] { XamlNodeType.StartMember, XamlNodeType.StartObject, XamlNodeType.EndObject, XamlNodeType.Value, XamlNodeType.None, XamlNodeType.EndMember, XamlNodeType.Value }, 6 };
        yield return new object[] { new XamlNodeType[] { XamlNodeType.StartMember, XamlNodeType.StartObject, XamlNodeType.EndObject, XamlNodeType.Value, XamlNodeType.StartMember, XamlNodeType.None, XamlNodeType.EndMember, XamlNodeType.EndMember, XamlNodeType.Value }, 8 };
    }

    [Theory]
    [MemberData(nameof(Skip_Skippable_TestData))]
    public void Skip_Skippable_Reads(XamlNodeType[] nodeTypes, int expectedReadCount)
    {
        var reader = new SubXamlReader(nodeTypes);
        reader.Skip();
        Assert.Equal(expectedReadCount, reader.ReadCount);
    }

#if DEBUG // This is a nop in release builds.
    [Theory]
    [InlineData(XamlNodeType.StartObject)]
    [InlineData(XamlNodeType.StartMember)]
    public void Skip_InvalidStartType_ThrowsXamlInternalException(XamlNodeType nodeType)
    {
        var reader = new ChangingXamlReader(nodeType);
        Assert.Throws<XamlInternalException>(() => reader.Skip());
    }
#endif

    [Fact]
    public void ReadSubtree_GetProperties_ReturnsExpected()
    {
        var reader = new SubXamlReader(XamlNodeType.Value)
        {
            IsEofResult = false,
            NamespaceResult = new NamespaceDeclaration("ns", "prefix"),
            TypeResult = new XamlType(typeof(int), new XamlSchemaContext()),
            ValueResult = new object(),
            MemberResult = new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false),
            SchemaContextResult = new XamlSchemaContext()
        };

        XamlReader subReader = reader.ReadSubtree();
        Assert.Equal(XamlNodeType.None, subReader.NodeType);
        Assert.True(subReader.IsEof);
        Assert.Null(subReader.Namespace);
        Assert.Null(subReader.Type);
        Assert.Null(subReader.Value);
        Assert.Null(subReader.Member);
        Assert.Equal(reader.SchemaContext, subReader.SchemaContext);

        subReader.Read();
        Assert.Equal(reader.NodeType, subReader.NodeType);
        Assert.Equal(reader.IsEof, subReader.IsEof);
        Assert.Equal(reader.Namespace, subReader.Namespace);
        Assert.Equal(reader.Type, subReader.Type);
        Assert.Equal(reader.Value, subReader.Value);
        Assert.Equal(reader.Member, subReader.Member);
        Assert.Equal(reader.SchemaContext, subReader.SchemaContext);
    }

    [Fact]
    public void ReadSubtree_NotIXamlLineInfoReader_PropertiesReturnExpected()
    {
        var reader = new SubXamlReader(XamlNodeType.Value);
        IXamlLineInfo subReader = Assert.IsAssignableFrom<IXamlLineInfo>(reader.ReadSubtree());
        Assert.False(subReader.HasLineInfo);
        Assert.Equal(0, subReader.LineNumber);
        Assert.Equal(0, subReader.LinePosition);
    }

    [Fact]
    public void ReadSubtree_IXamlLineInfoReader_PropertiesReturnExpected()
    {
        var reader = new SubXamlReaderWithLineInfo(XamlNodeType.Value)
        {
            HasLineInfoResult = true,
            LineNumberResult = 1,
            LinePositionResult = 2
        };
        IXamlLineInfo subReader = Assert.IsAssignableFrom<IXamlLineInfo>(reader.ReadSubtree());
        Assert.True(subReader.HasLineInfo);
        Assert.Equal(1, subReader.LineNumber);
        Assert.Equal(2, subReader.LinePosition);
    }

    [Fact]
    public void ReadSubtree_ReadEof_ReturnsFalse()
    {
        var reader = new SubXamlReader(XamlNodeType.Value)
        {
            IsEofResult = true
        };

        XamlReader subReader = reader.ReadSubtree();
        Assert.True(subReader.Read());
        Assert.False(subReader.Read());
    }

    [Theory]
    [MemberData(nameof(Skip_Skippable_TestData))]
    public void ReadSubtree_ReadStartMember_Success(XamlNodeType[] nodeTypes, int expectedReadCount)
    {
        var reader = new SubXamlReader(nodeTypes);
        XamlReader subReader = reader.ReadSubtree();
        for (int i = 0; i < expectedReadCount; i++)
        {
            Assert.True(subReader.Read());
        }
        Assert.False(subReader.Read());
    }

    [Fact]
    public void ReadSubtree_ReadWhenDisposed_ThrowsObjectDisposedException()
    {
        var reader = new SubXamlReader(XamlNodeType.None);
        XamlReader subReader = reader.ReadSubtree();
        subReader.Close();
        Assert.Throws<ObjectDisposedException>(() => subReader.Read());
    }

    [Fact]
    public void Dispose_Invoke_SetsIsDisposed()
    {
        var reader = new SubXamlReader(XamlNodeType.None);
        ((IDisposable)reader).Dispose();
        Assert.True(reader.IsDisposedEntry);

        // Can still read.
        reader.Read();
    }

    [Fact]
    public void Close_Invoke_SetsIsDisposed()
    {
        var reader = new SubXamlReader(XamlNodeType.None);
        reader.Close();
        Assert.True(reader.IsDisposedEntry);

        // Can still read.
        reader.Read();
    }

    private class ChangingXamlReader : SubXamlReader
    {
        public ChangingXamlReader(params XamlNodeType[] nodeTypes) : base(nodeTypes) { }

        public override XamlNodeType NodeType
        {
            get
            {
                XamlNodeType current = NodeTypes[CurrentIndex];
                NodeTypes[CurrentIndex] = (XamlNodeType)(current + 1);
                return current;
            }
        }
    }
}
