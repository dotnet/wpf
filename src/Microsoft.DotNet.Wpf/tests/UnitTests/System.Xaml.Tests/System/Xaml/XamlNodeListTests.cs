// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Xaml.Tests;

public class XamlNodeListTests
{
    [Fact]
    public void Ctor_XamlSchemaContext()
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context);
        Assert.Equal(0, list.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Ctor_XamlSchemaContext_Int(int size)
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context, size);
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void Ctor_NullSchemaContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlNodeList(null));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlNodeList(null, 64));
    }

    [Fact]
    public void Ctor_NegativeSize_ThrowsArgumentOutOfRangeException()
    {
        var context = new XamlSchemaContext();
        Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new XamlNodeList(context, -1));
    }

    [Fact]
    public void GetReader_WriterClosed_ReturnsExpected()
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context);
        list.Writer.Close();

        XamlReader reader = list.GetReader();
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(context, reader.SchemaContext);

        IXamlIndexingReader indexingReader = Assert.IsAssignableFrom<IXamlIndexingReader>(reader);
        Assert.Equal(0, indexingReader.Count);
        Assert.Equal(-1, indexingReader.CurrentIndex);
    }

    [Fact]
    public void GetReader_ReadPastEof_ReturnsExpected()
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context);
        list.Writer.WriteGetObject();
        list.Writer.WriteEndObject();
        list.Writer.Close();

        XamlReader reader = list.GetReader();
        IXamlIndexingReader indexingReader = Assert.IsAssignableFrom<IXamlIndexingReader>(reader);

        Assert.True(reader.Read());
        Assert.Equal(XamlNodeType.GetObject, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(0, indexingReader.CurrentIndex);

        Assert.True(reader.Read());
        Assert.Equal(XamlNodeType.EndObject, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(1, indexingReader.CurrentIndex);

        Assert.False(reader.Read());
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.True(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(2, indexingReader.CurrentIndex);
    }

    [Fact]
    public void GetReader_ReadWhenDisposed_ThrowsObjectDisposedException()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        list.Writer.Close();

        XamlReader reader = list.GetReader();
        reader.Close();
        
        Assert.Throws<ObjectDisposedException>(() => reader.Read());
    }

    [Fact]
    public void GetReader_SetCurrentIndex_SeeksToValue()
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context);
        list.Writer.WriteGetObject();
        list.Writer.WriteEndObject();
        list.Writer.Close();

        XamlReader reader = list.GetReader();
        IXamlIndexingReader indexingReader = Assert.IsAssignableFrom<IXamlIndexingReader>(reader);
        indexingReader.CurrentIndex = 1;
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(1, indexingReader.CurrentIndex);
        Assert.Equal(XamlNodeType.EndObject, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(1, indexingReader.CurrentIndex);
    }

    [Fact]
    public void GetReader_SetCurrentIndexEnd_SeeksToEof()
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context);
        list.Writer.WriteGetObject();
        list.Writer.WriteEndObject();
        list.Writer.Close();

        XamlReader reader = list.GetReader();
        IXamlIndexingReader indexingReader = Assert.IsAssignableFrom<IXamlIndexingReader>(reader);
        indexingReader.CurrentIndex = 2;
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(2, indexingReader.CurrentIndex);
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.True(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(2, indexingReader.CurrentIndex);
    }

    [Fact]
    public void GetReader_SetCurrentIndexMinusOne_SeeksToStart()
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context);
        list.Writer.WriteGetObject();
        list.Writer.WriteEndObject();
        list.Writer.Close();

        XamlReader reader = list.GetReader();
        IXamlIndexingReader indexingReader = Assert.IsAssignableFrom<IXamlIndexingReader>(reader);
        indexingReader.CurrentIndex = -1;
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(-1, indexingReader.CurrentIndex);
        Assert.Equal(XamlNodeType.None, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
        Assert.Equal(2, indexingReader.Count);
        Assert.Equal(-1, indexingReader.CurrentIndex);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(3)]
    public void GetReader_SetInvalidIndex_ThrowsIndexOutOfRangeException(int value)
    {
        var context = new XamlSchemaContext();
        var list = new XamlNodeList(context);
        list.Writer.WriteGetObject();
        list.Writer.WriteEndObject();
        list.Writer.Close();

        XamlReader reader = list.GetReader();
        IXamlIndexingReader indexingReader = Assert.IsAssignableFrom<IXamlIndexingReader>(reader);
        Assert.Throws<IndexOutOfRangeException>(() => indexingReader.CurrentIndex = value);
    }

    [Fact]
    public void GetReader_WriterNotClosed_ThrowsXamlException()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        Assert.Throws<XamlException>(() => list.GetReader());
    }

    [Fact]
    public void Clear_Invoke_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        list.Writer.WriteGetObject();
        list.Writer.Close();
        Assert.Equal(1, list.Count);
        Assert.NotNull(list.GetReader());

        list.Clear();
        Assert.Equal(0, list.Count);
        Assert.Throws<XamlException>(() => list.GetReader());
    }

    [Fact]
    public void Writer_Get_ReturnsExpected()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        Assert.NotNull(writer);
        Assert.Same(writer, list.Writer);
    }

    [Fact]
    public void Writer_WriteGetObject_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.WriteGetObject();
        Assert.Equal(1, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        Assert.True(reader.Read());
        Assert.Equal(1, list.Count);
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
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteGetObject());
    }

    [Fact]
    public void Writer_WriteStartObject_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.WriteStartObject(XamlLanguage.Object);
        Assert.Equal(1, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        Assert.True(reader.Read());
        Assert.Equal(1, list.Count);
        Assert.Equal(XamlNodeType.StartObject, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Equal(XamlLanguage.Object, reader.Type);
        Assert.Null(reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteStartObjectWhenDisposed_ThrowsObjectDisposedException()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteStartObject(null));
    }

    [Fact]
    public void Writer_WriteEndObject_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.WriteEndObject();
        Assert.Equal(1, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        Assert.True(reader.Read());
        Assert.Equal(1, list.Count);
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
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteEndObject());
    }

    [Fact]
    public void Writer_WriteStartMember_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.WriteStartMember(XamlLanguage.Key);
        Assert.Equal(1, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        Assert.True(reader.Read());
        Assert.Equal(1, list.Count);
        Assert.Equal(XamlNodeType.StartMember, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Null(reader.Value);
        Assert.Equal(XamlLanguage.Key, reader.Member);
    }

    [Fact]
    public void Writer_WriteStartMemberWhenDisposed_ThrowsObjectDisposedException()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteStartMember(null));
    }

    [Fact]
    public void Writer_WriteEndMember_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.WriteEndMember();
        Assert.Equal(1, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        Assert.True(reader.Read());
        Assert.Equal(1, list.Count);
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
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteEndMember());
    }

    [Fact]
    public void Writer_WriteValue_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.WriteValue(1);
        Assert.Equal(1, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        Assert.True(reader.Read());
        Assert.Equal(1, list.Count);
        Assert.Equal(XamlNodeType.Value, reader.NodeType);
        Assert.False(reader.IsEof);
        Assert.Null(reader.Namespace);
        Assert.Null(reader.Type);
        Assert.Equal(1, reader.Value);
        Assert.Null(reader.Member);
    }

    [Fact]
    public void Writer_WriteValueWhenDisposed_ThrowsObjectDisposedException()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteValue(null));
    }

    [Fact]
    public void Writer_WriteNamespace_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        var namespaceDeclaration = new NamespaceDeclaration("namespace", "prefix");
        writer.WriteNamespace(namespaceDeclaration);
        Assert.Equal(1, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        Assert.True(reader.Read());
        Assert.Equal(1, list.Count);
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
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteNamespace(null));
    }

    [Fact]
    public void Writer_SetLineInfoBeforeWrite_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        var namespaceDeclaration = new NamespaceDeclaration("namespace", "prefix");
        consumer.SetLineInfo(1, 2);
        writer.WriteNamespace(namespaceDeclaration);
        Assert.Equal(2, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        IXamlLineInfo lineInfo = Assert.IsAssignableFrom<IXamlLineInfo>(reader);
        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(2, list.Count);
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
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        var namespaceDeclaration = new NamespaceDeclaration("namespace", "prefix");
        writer.WriteNamespace(namespaceDeclaration);
        consumer.SetLineInfo(1, 2);
        Assert.Equal(2, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        IXamlLineInfo lineInfo = Assert.IsAssignableFrom<IXamlLineInfo>(reader);
        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(2, list.Count);
        Assert.Equal(XamlNodeType.NamespaceDeclaration, reader.NodeType);

        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);
    }

    [Fact]
    public void Writer_SetLineInfoMultipleTimes_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        consumer.SetLineInfo(1, 2);
        consumer.SetLineInfo(2, 3);
        Assert.Equal(2, list.Count);

        writer.Close();
        XamlReader reader = list.GetReader();
        IXamlLineInfo lineInfo = Assert.IsAssignableFrom<IXamlLineInfo>(reader);
        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);

        Assert.False(reader.Read());
        Assert.Equal(2, list.Count);
        Assert.Equal(XamlNodeType.None, reader.NodeType);

        Assert.True(lineInfo.HasLineInfo);
        Assert.Equal(0, lineInfo.LineNumber);
        Assert.Equal(0, lineInfo.LinePosition);
    }

    [Fact]
    public void Writer_SetLineInfoWhenDisposed_ThrowsObjectDisposedException()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        Assert.Throws<ObjectDisposedException>(() => consumer.SetLineInfo(1, 2));
    }

    [Fact]
    public void Writer_GetShouldProvideLineInfo_ReturnTrue()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(list.Writer);
        Assert.True(consumer.ShouldProvideLineInfo);
    }

    [Fact]
    public void Writer_GetShouldProvideLineInfoWhenDisposed_ThrowsObjectDisposedException()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldProvideLineInfo);
    }

    [Fact]
    public void Writer_CloseMultipleTimes_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        writer.Close();
        writer.Close();
    }

    [Fact]
    public void Writer_DisposeDisposingFalse_Success()
    {
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
        MethodInfo dispose = writer.GetType().GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic)!;
        dispose.Invoke(writer, new object[] { false });

        IXamlLineInfoConsumer consumer = Assert.IsAssignableFrom<IXamlLineInfoConsumer>(writer);
        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldProvideLineInfo);
    }

    [Fact]
    public void Writer_DisposeCallAddDelegate_ThrowsXamlException()
    {
        // Dead code in WriterDelegate.
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
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
        var list = new XamlNodeList(new XamlSchemaContext());
        XamlWriter writer = list.Writer;
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

    [Fact]
    public void Add_WriterClosed_ThrowsXamlException()
    {
        // Use reflection to simulate potential thread race condition.
        var list = new XamlNodeList(new XamlSchemaContext());
        list.Writer.Close();

        MethodInfo addMethod = typeof(XamlNodeList).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool threwXamlException = true;
        try
        {
            addMethod.Invoke(list, new object?[] { null, null });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is XamlException)
        {
            threwXamlException = true;
        }
        Assert.True(threwXamlException);
    }

    [Fact]
    public void AddLineInfo_WriterClosed_ThrowsXamlException()
    {
        // Use reflection to simulate potential thread race condition.
        var list = new XamlNodeList(new XamlSchemaContext());
        list.Writer.Close();

        MethodInfo addLineInfoMethod = typeof(XamlNodeList).GetMethod("AddLineInfo", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool threwXamlException = true;
        try
        {
            addLineInfoMethod.Invoke(list, new object[] { 1, 2 });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is XamlException)
        {
            threwXamlException = true;
        }
        Assert.True(threwXamlException);
    }

    [Fact]
    public void Index_WriterNotClosed_ThrowsXamlException()
    {
        // Use reflection to simulate potential thread race condition.
        var list = new XamlNodeList(new XamlSchemaContext());

        MethodInfo indexMethod = typeof(XamlNodeList).GetMethod("Index", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool threwXamlException = true;
        try
        {
            indexMethod.Invoke(list, new object[] { 1 });
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
