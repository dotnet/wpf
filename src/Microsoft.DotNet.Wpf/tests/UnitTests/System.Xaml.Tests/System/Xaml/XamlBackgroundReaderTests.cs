// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Tests;

public class XamlBackgroundReaderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ctor_XamlReader(bool hasLineInfo)
    {
        var wrappedReader = new SubXamlReaderWithLineInfo(XamlNodeType.GetObject)
        {
            IsEofResult = true,
            NamespaceResult = new NamespaceDeclaration("ns", "prefix"),
            TypeResult = new XamlType(typeof(int), new XamlSchemaContext()),
            ValueResult = new object(),
            MemberResult = new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false),
            SchemaContextResult = new XamlSchemaContext(),
            HasLineInfoResult = hasLineInfo,
            LineNumberResult = 1,
            LinePositionResult = 2
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(wrappedReader.SchemaContext, reader.SchemaContext);
        Assert.Equal(hasLineInfo, reader.HasLineInfo);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal(0, reader.LinePosition);
    }

    [Fact]
    public void Ctor_NullWrappedReader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("wrappedReader", () => new XamlBackgroundReader(null));
    }

    [Fact]
    public void Ctor_WrappedReaderNotIXamlLineInfo_ThrowsInvalidCastException()
    {
        Assert.Throws<InvalidCastException>(() => new XamlBackgroundReader(new SubXamlReader()));
    }

    [Fact]
    public void Ctor_NullReaderSchemaContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlBackgroundReader(new SubXamlReaderWithLineInfo()));
    }

    [Fact]
    public void Read_Started_Success()
    {
        var wrappedReader = new SubXamlReaderWithLineInfo(XamlNodeType.Value, XamlNodeType.StartMember)
        {
            SchemaContextResult = new XamlSchemaContext()
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.StartThread();

        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.False(reader.HasLineInfo);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal(0, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(XamlNodeType.StartMember, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.False(reader.HasLineInfo);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal(0, reader.LinePosition);

        Assert.False(reader.Read());
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.True(reader.IsEof);
        Assert.False(reader.HasLineInfo);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal(0, reader.LinePosition);

        Assert.False(reader.Read());
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.True(reader.IsEof);
        Assert.False(reader.HasLineInfo);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal(0, reader.LinePosition);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(0, 2)]
    [InlineData(1, 0)]
    public void Read_HasLineInfo_Success(int lineNumber, int linePosition)
    {
        var wrappedReader = new SubXamlReaderWithLineInfo(XamlNodeType.Value, XamlNodeType.StartMember)
        {
            SchemaContextResult = new XamlSchemaContext(),
            HasLineInfoResult = true,
            LineNumberResult = lineNumber,
            LinePositionResult = linePosition
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.StartThread();
        
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.True(reader.HasLineInfo);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal(0, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(XamlNodeType.StartMember, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.True(reader.HasLineInfo);
        Assert.Equal(lineNumber, reader.LineNumber);
        Assert.Equal(lineNumber == 0 ? 0 : linePosition, reader.LinePosition);

        Assert.False(reader.Read());
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.True(reader.IsEof);
        Assert.True(reader.HasLineInfo);
        Assert.Equal(lineNumber, reader.LineNumber);
        Assert.Equal(lineNumber == 0 ? 0 : linePosition, reader.LinePosition);

        Assert.False(reader.Read());
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.True(reader.IsEof);
        Assert.True(reader.HasLineInfo);
        Assert.Equal(lineNumber, reader.LineNumber);
        Assert.Equal(lineNumber == 0 ? 0 : linePosition, reader.LinePosition);
    }

    [Fact]
    public void Read_BufferFull_Success()
    {
        var wrappedReader = new SubXamlReaderWithLineInfo(Enumerable.Repeat(XamlNodeType.StartMember, 65).ToArray())
        {
            SchemaContextResult = new XamlSchemaContext()
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.StartThread(null);
        
        for (int i = 0; i < wrappedReader.NodeTypes.Length - 1; i++)
        {
            Assert.True(reader.Read());
            Assert.Equal(XamlNodeType.StartMember, reader.NodeType);
        }

        Assert.False(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void Read_ThrowsException_RethrowsException()
    {
        var wrappedReader = new ThrowingXamlReader(XamlNodeType.StartMember, XamlNodeType.StartMember)
        {
            SchemaContextResult = new XamlSchemaContext()
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.StartThread(null);

        Assert.Throws<DivideByZeroException>(() => reader.Read());
    }

    private class ThrowingXamlReader : SubXamlReaderWithLineInfo
    {
        public ThrowingXamlReader(params XamlNodeType[] nodeTypes) : base(nodeTypes)
        {
        }

        public override bool Read() => throw new DivideByZeroException();
    }

    [Fact]
    public void Read_Disposes_ThrowsObjectDisposedException()
    {
        var wrappedReader = new DisposingXamlReader(XamlNodeType.StartMember, XamlNodeType.StartMember)
        {
            SchemaContextResult = new XamlSchemaContext()
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        wrappedReader.Inner = reader;
        reader.StartThread(null);
        
    }

    private class DisposingXamlReader : SubXamlReaderWithLineInfo
    {
        public DisposingXamlReader(params XamlNodeType[] nodeTypes) : base(nodeTypes)
        {
        }

        private bool ReadShouldClose { get; set; }
        public XamlBackgroundReader? Inner { get; set; }

        private bool CalledHasLineInfo { get; set; }
        public override bool HasLineInfo
        {
            get
            {
                if (CalledHasLineInfo)
                {
                    ReadShouldClose = true;
                }
                CalledHasLineInfo = true;
                return base.HasLineInfo;
            }
        }

        public override bool Read()
        {
            bool result = base.Read();
            if (ReadShouldClose)
            {
                Inner!.Close();
            }
            return result;
        }
    }

    [Fact]
    public void Next_Disposed_ThrowsXamlException()
    {
        // Use reflection to simulate potential thread race condition.
        var wrappedReader = new ThrowingXamlReader(XamlNodeType.StartMember, XamlNodeType.StartMember)
        {
            SchemaContextResult = new XamlSchemaContext()
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.Close();

        MethodInfo nextMethod = typeof(XamlBackgroundReader).GetMethod("Next", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool threwObjectDisposedException = true;
        try
        {
            nextMethod.Invoke(reader, Array.Empty<object>());
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            threwObjectDisposedException = true;
        }
        Assert.True(threwObjectDisposedException);
    }

    [Fact]
    public void AddLineInfo_Disposed_Nop()
    {
        // Use reflection to simulate potential thread race condition.
        var wrappedReader = new ThrowingXamlReader(XamlNodeType.StartMember, XamlNodeType.StartMember)
        {
            SchemaContextResult = new XamlSchemaContext()
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.Close();

        MethodInfo addLineInfoMethod = typeof(XamlBackgroundReader).GetMethod("AddLineInfo", BindingFlags.Instance | BindingFlags.NonPublic)!;
        addLineInfoMethod.Invoke(reader, new object[] { 1, 2 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Read_Disposed_ThrowsObjectDisposedException(bool hasLineInfo)
    {
        var wrappedReader = new SubXamlReaderWithLineInfo(XamlNodeType.GetObject)
        {
            SchemaContextResult = new XamlSchemaContext(),
            HasLineInfoResult = hasLineInfo
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.Close();
        Assert.Throws<ObjectDisposedException>(() => reader.Read());
    }

    [Fact]
    public void StartThread_AlreadyStarted_ThrowsInvalidOperationException()
    {
        var wrappedReader = new SubXamlReaderWithLineInfo(XamlNodeType.GetObject, XamlNodeType.EndMember)
        {
            SchemaContextResult = new XamlSchemaContext()
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.StartThread();

        Assert.Throws<InvalidOperationException>(() => reader.StartThread());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Close_MultipleTimes_ThrowsObjectDisposedExcpeption(bool hasLineInfo)
    {
        var wrappedReader = new SubXamlReaderWithLineInfo(XamlNodeType.GetObject)
        {
            SchemaContextResult = new XamlSchemaContext(),
            HasLineInfoResult = hasLineInfo
        };
        var reader = new XamlBackgroundReader(wrappedReader);
        reader.Close();
        Assert.Throws<ObjectDisposedException>(() => reader.Close());
    }
}
