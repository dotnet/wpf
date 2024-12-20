// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;

namespace System.Xaml.Tests;

public class XamlDuplicateMemberExceptionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var exception = new XamlDuplicateMemberException();
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Equal(0, exception.LineNumber);
        Assert.Equal(0, exception.LinePosition);
        Assert.Null(exception.DuplicateMember);
        Assert.Null(exception.ParentType);
    }

    [Fact]
    public void Ctor_String()
    {
        var exception = new XamlDuplicateMemberException("message");
        Assert.Equal("message", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Equal(0, exception.LineNumber);
        Assert.Equal(0, exception.LinePosition);
        Assert.Null(exception.DuplicateMember);
        Assert.Null(exception.ParentType);
    }

    public static IEnumerable<object?[]> Ctor_XamlMember_XamlType_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var member = new XamlMember("name", type, false);

        yield return new object?[] { member, type };
        yield return new object?[] { member, null };
        yield return new object?[] { null, type };
        yield return new object?[] { null, null };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlMember_XamlType_TestData))]
    public void Ctor_XamlMember_XamlType(XamlMember member, XamlType type)
    {
        var exception = new XamlDuplicateMemberException(member, type);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Equal(0, exception.LineNumber);
        Assert.Equal(0, exception.LinePosition);
        Assert.Equal(member, exception.DuplicateMember);
        Assert.Equal(type, exception.ParentType);
    }

    public static IEnumerable<object?[]> Ctor_String_Exception_TestData()
    {
        yield return new object?[] { "message", null, 0, 0 };
        yield return new object?[] { "message", new DivideByZeroException(), 0, 0 };
        yield return new object?[] { "message", new XamlException("message", null, 1, 2), 1, 2 };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_Exception_TestData))]
    public void Ctor_String_Exception(string message, Exception innerException, int expectedLineNumber, int expectedLinePosition)
    {
        var exception = new XamlDuplicateMemberException(message, innerException);
        Assert.Contains(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(expectedLineNumber, exception.LineNumber);
        Assert.Equal(expectedLinePosition, exception.LinePosition);
        Assert.Null(exception.DuplicateMember);
        Assert.Null(exception.ParentType);
    }

#pragma warning disable SYSLIB0011, SYSLIB0051 // Type or member is obsolete
    [Fact]
    public void Ctor_NullSerializationInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("info", () => new SubXamlDuplicateMemberException(null!, new StreamingContext()));
    }

    [Fact]
    public void GetObjectData_NullInfo_ThrowsArgumentNullException()
    {
        var exception = new XamlDuplicateMemberException();
        Assert.Throws<ArgumentNullException>("info", () => exception.GetObjectData(null, new StreamingContext()));
    }
#pragma warning restore SYSLIB0011, SYSLIB0051 // Type or member is obsolete

    private class SubXamlDuplicateMemberException : XamlObjectWriterException
    {
        public SubXamlDuplicateMemberException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
