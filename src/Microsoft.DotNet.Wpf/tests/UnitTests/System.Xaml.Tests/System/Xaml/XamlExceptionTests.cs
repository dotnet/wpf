// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;

namespace System.Xaml.Tests;

public class XamlExceptionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var exception = new XamlException();
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Equal(0, exception.LineNumber);
        Assert.Equal(0, exception.LinePosition);
    }

    [Fact]
    public void Ctor_String()
    {
        var exception = new XamlException("message");
        Assert.Equal("message", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Equal(0, exception.LineNumber);
        Assert.Equal(0, exception.LinePosition);
    }

    public static IEnumerable<object?[]> Ctor_String_Exception_Int_Int_TestData()
    {
        yield return new object?[] { "message", null, 0, 0 };
        yield return new object?[] { "message", null, 0, 1 };
        yield return new object?[] { "message", null, 1, 0 };
        yield return new object?[] { "message", new DivideByZeroException(), -1, -2 };
        yield return new object?[] { "message", new XamlException("message", null, 1, 2), 1, 2 };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_Exception_Int_Int_TestData))]
    public void Ctor_String_Exception_Int_Int(string message, Exception innerException, int lineNumber, int linePosition)
    {
        var exception = new XamlException(message, innerException, lineNumber,linePosition);
        Assert.Contains(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(lineNumber, exception.LineNumber);
        Assert.Equal(linePosition, exception.LinePosition);
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
        var exception = new XamlException(message, innerException);
        Assert.Contains(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(expectedLineNumber, exception.LineNumber);
        Assert.Equal(expectedLinePosition, exception.LinePosition);
    }

#pragma warning disable SYSLIB0011, SYSLIB0051 // Type or member is obsolete
    [Fact]
    public void Ctor_NullSerializationInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("info", () => new SubXamlException(null!, new StreamingContext()));
    }

    [Fact]
    public void GetObjectData_NullInfo_ThrowsArgumentNullException()
    {
        var exception = new XamlException();
        Assert.Throws<ArgumentNullException>("info", () => exception.GetObjectData(null, new StreamingContext()));
    }
#pragma warning restore SYSLIB0011

    private class SubXamlException : XamlException
    {
        public SubXamlException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
