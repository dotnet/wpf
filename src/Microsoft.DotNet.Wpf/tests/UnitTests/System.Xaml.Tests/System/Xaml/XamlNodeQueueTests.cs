// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Xaml.Tests;

public class XamlNodeQueueTests
{
    [Fact]
    public void Ctor_XamlSchemaContext()
    {
        var context = new XamlSchemaContext();
        var queue = new XamlNodeQueue(context);
        Assert.True(queue.IsEmpty);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void Ctor_NullSchemaContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlNodeQueue(null));
    }

    [Fact]
    public void Reader_GetProperties_ReturnsExpected()
    {
        var context = new XamlSchemaContext();
        var queue = new XamlNodeQueue(context);
        XamlReader reader = queue.Reader;
        Assert.NotNull(reader);
        Assert.Same(reader, queue.Reader);

        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(context, reader.SchemaContext);
    }

    [Fact]
    public void Reader_NotIXamlLineInfoReader_PropertiesReturnExpected()
    {
        var context = new XamlSchemaContext();
        var queue = new XamlNodeQueue(context);
        IXamlLineInfo reader = Assert.IsAssignableFrom<IXamlLineInfo>(queue.Reader);
        Assert.False(reader.HasLineInfo);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal(0, reader.LinePosition);
    }

    [Fact]
    public void Reader_ReadWhenEmpty_SetsEof()
    {
        var context = new XamlSchemaContext();
        var queue = new XamlNodeQueue(context);
        XamlReader reader = queue.Reader;
        Assert.False(reader.Read());
        Assert.Equal(0, queue.Count);
        
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.True(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Reader_ReadWhenDisposed_ThrowsObjectDisposedException()
    {
        var context = new XamlSchemaContext();
        var queue = new XamlNodeQueue(context);
        XamlReader reader = queue.Reader;
        reader.Close();

        Assert.Throws<ObjectDisposedException>(() => reader.Read());
    }

    [Fact]
    public void Writer_Get_ReturnsExpected()
    {
        var context = new XamlSchemaContext();
        var queue = new XamlNodeQueue(context);
        XamlWriter writer = queue.Writer;
        Assert.NotNull(writer);
        Assert.Same(writer, queue.Writer);
    }

    [Fact]
    public void Writer_WriteGetObject_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.WriteGetObject();
        Assert.Equal(1, queue.Count);

        XamlReader reader = queue.Reader;
        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.GetObject, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteGetObjectWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteGetObject());
    }

    public static IEnumerable<object?[]> Writer_WriteStartObject_TestData()
    {
        yield return new object?[] { XamlLanguage.Object };
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(Writer_WriteStartObject_TestData))]
    public void Writer_WriteStartObject_Success(XamlType type)
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.WriteStartObject(type);
        Assert.Equal(1, queue.Count);

        XamlReader reader = queue.Reader;
        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.StartObject, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Equal(type, reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteStartObjectWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteStartObject(null));
    }

    [Fact]
    public void Writer_WriteEndObject_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.WriteEndObject();
        Assert.Equal(1, queue.Count);

        XamlReader reader = queue.Reader;
        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.EndObject, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteEndObjectWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteEndObject());
    }

    public static IEnumerable<object?[]> Writer_WriteStartMember_TestData()
    {
        yield return new object?[] { XamlLanguage.Key };
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(Writer_WriteStartMember_TestData))]
    public void Writer_WriteStartMember_Success(XamlMember member)
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.WriteStartMember(member);
        Assert.Equal(1, queue.Count);

        XamlReader reader = queue.Reader;
        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.StartMember, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Equal(member, reader.Member);
    }

    [Fact]
    public void Writer_WriteStartMemberWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteStartMember(null));
    }

    [Fact]
    public void Writer_WriteEndMember_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.WriteEndMember();
        Assert.Equal(1, queue.Count);

        XamlReader reader = queue.Reader;
        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.EndMember, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteEndMemberWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteEndMember());
    }

    public static IEnumerable<object?[]> Writer_WriteValue_TestData()
    {
        yield return new object?[] { 1 };
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(Writer_WriteValue_TestData))]
    public void Writer_WriteValue_Success(object value)
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.WriteValue(value);
        Assert.Equal(1, queue.Count);

        XamlReader reader = queue.Reader;
        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.Value, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Equal(value, reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteValueWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteValue(null));
    }

    public static IEnumerable<object?[]> Writer_WriteNamespace_TestData()
    {
        yield return new object?[] { new NamespaceDeclaration("namespace", "prefix") };
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(Writer_WriteNamespace_TestData))]
    public void Writer_WriteNamespace_Success(NamespaceDeclaration namespaceDeclaration)
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.WriteNamespace(namespaceDeclaration);
        Assert.Equal(1, queue.Count);

        XamlReader reader = queue.Reader;
        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.NamespaceDeclaration, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Equal(namespaceDeclaration, reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteNamespaceWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteNamespace(null));
    }

    [Fact]
    public void Writer_SetLineInfoBeforeWrite_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        var namespaceDeclaration = new NamespaceDeclaration("namespace", "prefix");
        consumer.SetLineInfo(1, 2);
        writer.WriteNamespace(namespaceDeclaration);
        Assert.Equal(2, queue.Count);

        XamlReader reader = queue.Reader;
        IXamlLineInfo lineInfo = Assert.IsAssignableFrom<IXamlLineInfo>(reader);
        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.NamespaceDeclaration, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Equal(namespaceDeclaration, reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);

        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(1, lineInfo.LineNumber);
        Assert.Equal(2, lineInfo.LinePosition);
    }

    [Fact]
    public void Writer_SetLineInfoAfterWrite_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        var namespaceDeclaration = new NamespaceDeclaration("namespace", "prefix");
        writer.WriteNamespace(namespaceDeclaration);
        consumer.SetLineInfo(1, 2);
        Assert.Equal(2, queue.Count);

        XamlReader reader = queue.Reader;
        IXamlLineInfo lineInfo = Assert.IsAssignableFrom<IXamlLineInfo>(reader);
        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(1, queue.Count);
        Assert.Equal(XamlNodeType.NamespaceDeclaration, reader.NodeType);

        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);
    }

    [Fact]
    public void Writer_SetLineInfoMultipleTimes_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        consumer.SetLineInfo(1, 2);
        consumer.SetLineInfo(2, 3);
        Assert.Equal(2, queue.Count);

        XamlReader reader = queue.Reader;
        IXamlLineInfo lineInfo = Assert.IsAssignableFrom<IXamlLineInfo>(reader);
        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        Assert.False(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.None, reader.NodeType);

        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(2, lineInfo.LineNumber);
        Assert.Equal(3, lineInfo.LinePosition);
    }

    [Fact]
    public void Writer_SetLineInfoMultipleTimesAfterGettingReader_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        XamlReader reader = queue.Reader;
        IXamlLineInfo lineInfo = Assert.IsAssignableFrom<IXamlLineInfo>(reader);
        Assert.False(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        consumer.SetLineInfo(1, 2);
        consumer.SetLineInfo(2, 3);
        Assert.Equal(2, queue.Count);

        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        Assert.False(reader.Read());
        Assert.Equal(0, queue.Count);
        Assert.Equal(XamlNodeType.None, reader.NodeType);

        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(2, lineInfo.LineNumber);
        Assert.Equal(3, lineInfo.LinePosition);
    }

    [Fact]
    public void Writer_SetLineInfoWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        Assert.Throws<ObjectDisposedException>(() => consumer.SetLineInfo(1, 2));
    }

    [Fact]
    public void Writer_GetShouldProvideLineInfo_ReturnTrue()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(queue.Writer);
        Assert.True(consumer.ShouldProvideLineInfo);
    }

    [Fact]
    public void Writer_GetShouldProvideLineInfoWhenDisposed_ThrowsObjectDisposedException()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldProvideLineInfo);
    }

    [Fact]
    public void Writer_CloseMultipleTimes_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();
        writer.Close();

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldProvideLineInfo);
    }

    [Fact]
    public void Writer_DisposeDisposingFalse_Success()
    {
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        MethodInfo dispose = writer.GetType().GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic)!;
        dispose.Invoke(writer, new object[] { false });

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldProvideLineInfo);
    }

    [Fact]
    public void Writer_DisposeCallAddDelegate_ThrowsXamlException()
    {
        // Dead code in WriterDelegate.
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        FieldInfo addDelegateField = writer.GetType().GetField("_addDelegate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Delegate addDelegate = Assert.IsAssignableFrom<Delegate>(addDelegateField.GetValue(writer));

        bool threwXamlException = true;
        try
        {
            addDelegate.DynamicInvoke(new object?[] { null, null });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is XamlException)
        {
            threwXamlException = true;
        }
        Assert.True(threwXamlException);
    }

    [Fact]
    public void Writer_DisposeCallAddLineInfoDelegate_ThrowsXamlException()
    {
        // Dead code in WriterDelegate.
        var queue = new XamlNodeQueue(new XamlSchemaContext());
        XamlWriter writer = queue.Writer;
        writer.Close();

        FieldInfo addLineInfoDelegateField = writer.GetType().GetField("_addLineInfoDelegate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Delegate addLineInfoDelegate = Assert.IsAssignableFrom<Delegate>(addLineInfoDelegateField.GetValue(writer));

        bool threwXamlException = true;
        try
        {
            addLineInfoDelegate.DynamicInvoke(new object?[] { null, null });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is XamlException)
        {
            threwXamlException = true;
        }
        Assert.True(threwXamlException);
    }

    private delegate void XamlNodeAddDelegate(XamlNodeType nodeType, object data);
    private delegate void XamlLineInfoAddDelegate(int lineNumber, int linePosition);
}
