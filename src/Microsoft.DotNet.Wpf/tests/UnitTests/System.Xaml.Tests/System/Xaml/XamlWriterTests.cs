// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Tests;

public class XamlWriterTests
{
    [Theory]
    [InlineData(XamlNodeType.None, null)]
    [InlineData(XamlNodeType.GetObject, nameof(XamlWriter.WriteGetObject))]
    [InlineData(XamlNodeType.EndObject, nameof(XamlWriter.WriteEndObject))]
    [InlineData(XamlNodeType.EndMember, nameof(XamlWriter.WriteEndMember))]
    public void WriteNode_Parameterless_Success(XamlNodeType nodeType, string? expectedCalledMethodName)
    {
        var writer = new SubXamlWriter();
        var reader = new SubXamlReader(nodeType);
        writer.WriteNode(reader);
        Assert.Equal(expectedCalledMethodName, writer.CalledMethodName);
        Assert.Empty(writer.CalledMethodArgs);
    }

    [Fact]
    public void WriteNode_StartObject_Success()
    {
        var writer = new SubXamlWriter();
        var reader = new SubXamlReader(XamlNodeType.StartObject)
        {
            TypeResult = new XamlType(typeof(int), new XamlSchemaContext())
        };
        writer.WriteNode(reader);
        Assert.Equal(nameof(XamlWriter.WriteStartObject), writer.CalledMethodName);
        Assert.Equal(reader.Type, Assert.Single(writer.CalledMethodArgs));
    }

    [Fact]
    public void WriteNode_StartMember_Success()
    {
        var writer = new SubXamlWriter();
        var reader = new SubXamlReader(XamlNodeType.StartMember)
        {
            MemberResult = new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
        };
        writer.WriteNode(reader);
        Assert.Equal(nameof(XamlWriter.WriteStartMember), writer.CalledMethodName);
        Assert.Equal(reader.Member, Assert.Single(writer.CalledMethodArgs));
    }

    [Fact]
    public void WriteNode_Value_Success()
    {
        var writer = new SubXamlWriter();
        var reader = new SubXamlReader(XamlNodeType.Value)
        {
            ValueResult = 1
        };
        writer.WriteNode(reader);
        Assert.Equal(nameof(XamlWriter.WriteValue), writer.CalledMethodName);
        Assert.Equal(reader.Value, Assert.Single(writer.CalledMethodArgs));
    }

    [Fact]
    public void WriteNode_NamespaceDeclaration_Success()
    {
        var writer = new SubXamlWriter();
        var reader = new SubXamlReader(XamlNodeType.NamespaceDeclaration)
        {
            NamespaceResult = new NamespaceDeclaration("ns", "prefix")
        };
        writer.WriteNode(reader);
        Assert.Equal(nameof(XamlWriter.WriteNamespace), writer.CalledMethodName);
        Assert.Equal(reader.Namespace, Assert.Single(writer.CalledMethodArgs));
    }

    [Fact]
    public void WriteNode_NullReader_ThrowsArgumentNullException()
    {
        var writer = new SubXamlWriter();
        Assert.Throws<ArgumentNullException>("reader", () => writer.WriteNode(null));
    }

    [Theory]
    [InlineData(XamlNodeType.NamespaceDeclaration + 1)]
    public void WriteNode_InvalidReaderNodeType_ThrowsNotImplementedException(XamlNodeType nodeType)
    {
        var writer = new SubXamlWriter();
        var reader = new SubXamlReader(nodeType);
        Assert.Throws<NotImplementedException>(() => writer.WriteNode(reader));
    }

    [Fact]
    public void Dispose_Invoke_SetsIsDisposed()
    {
        var writer = new SubXamlWriter();
        ((IDisposable)writer).Dispose();
        Assert.True(writer.IsDisposedEntry);

        // Can still write.
        var reader = new SubXamlReader(XamlNodeType.None);
        writer.WriteNode(reader);
    }

    [Fact]
    public void Close_Invoke_SetsIsDisposed()
    {
        var writer = new SubXamlWriter();
        writer.Close();
        Assert.True(writer.IsDisposedEntry);

        // Can still write.
        var reader = new SubXamlReader(XamlNodeType.None);
        writer.WriteNode(reader);
    }

    private class SubXamlWriter : XamlWriter
    {
        public string? CalledMethodName { get; set; }
        public List<object> CalledMethodArgs { get; } = new List<object>();
        
        public override void WriteGetObject()
        {
            CalledMethodName = nameof(WriteGetObject);
        }

        public override void WriteStartObject(XamlType type)
        {
            CalledMethodName = nameof(WriteStartObject);
            CalledMethodArgs.Add(type);
        }

        public override void WriteEndObject()
        {
            CalledMethodName = nameof(WriteEndObject);
        }

        public override void WriteStartMember(XamlMember xamlMember)
        {
            CalledMethodName = nameof(WriteStartMember);
            CalledMethodArgs.Add(xamlMember);
        }

        public override void WriteEndMember()
        {
            CalledMethodName = nameof(WriteEndMember);
        }

        public override void WriteValue(object value)
        {
            CalledMethodName = nameof(WriteValue);
            CalledMethodArgs.Add(value);
        }

        public override void WriteNamespace(NamespaceDeclaration namespaceDeclaration)
        {
            CalledMethodName = nameof(WriteNamespace);
            CalledMethodArgs.Add(namespaceDeclaration);
        }

        public override XamlSchemaContext? SchemaContext { get; }

        public bool IsDisposedEntry => IsDisposed;
    }
}
