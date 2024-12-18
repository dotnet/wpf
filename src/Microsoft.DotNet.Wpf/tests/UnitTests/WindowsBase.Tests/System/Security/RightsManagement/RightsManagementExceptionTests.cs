// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Security.RightsManagement.Tests;

public class RightsManagementExceptionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var exception = new RightsManagementException();
        Assert.Equal(RightsManagementFailureCode.Success, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Null(exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Theory]
    [InlineData(RightsManagementFailureCode.Success)]
    [InlineData(RightsManagementFailureCode.ServerError)]
    [InlineData(RightsManagementFailureCode.Success - 1)]
    public void Ctor_RightsManagementFailureCode(RightsManagementFailureCode failureCode)
    {
        var exception = new RightsManagementException(failureCode);
        Assert.Equal(failureCode, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Null(exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Theory]
    [InlineData("")]
    [InlineData("message")]
    public void Ctor_String(string message)
    {
        var exception = new RightsManagementException(message);
        Assert.Equal(RightsManagementFailureCode.Success, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Null(exception.InnerException);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Fact]
    public void Ctor_String_Null()
    {
        var exception = new RightsManagementException(null!);
        Assert.Equal(RightsManagementFailureCode.Success, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Null(exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    public static IEnumerable<object?[]> Ctor_RightsManagementFailureCode_Exception_TestData()
    {
        yield return new object?[] { RightsManagementFailureCode.Success, null };
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { RightsManagementFailureCode.ServerError, new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { RightsManagementFailureCode.Success - 1, new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
    }

    [Theory]
    [MemberData(nameof(Ctor_RightsManagementFailureCode_Exception_TestData))]
    public void Ctor_RightsManagementFailureCode_Exception(RightsManagementFailureCode failureCode, Exception innerException)
    {
        var exception = new RightsManagementException(failureCode, innerException);
        Assert.Equal(failureCode, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Same(innerException, exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Theory]
    [InlineData(RightsManagementFailureCode.Success, "")]
    [InlineData(RightsManagementFailureCode.ServerError, "message")]
    [InlineData(RightsManagementFailureCode.Success - 1, "message")]
    public void Ctor_RightsManagementFailureCode_String(RightsManagementFailureCode failureCode, string message)
    {
        var exception = new RightsManagementException(failureCode, message);
        Assert.Equal(failureCode, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Null(exception.InnerException);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Theory]
    [InlineData(RightsManagementFailureCode.Success)]
    [InlineData(RightsManagementFailureCode.ServerError)]
    [InlineData(RightsManagementFailureCode.Success - 1)]
    public void Ctor_RightsManagementFailureCode_String_Null(RightsManagementFailureCode failureCode)
    {
        var exception = new RightsManagementException(failureCode, (string)null!);
        Assert.Equal(failureCode, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Null(exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    public static IEnumerable<object?[]> Ctor_String_Exception_TestData()
    {
        yield return new object?[] { "", null };
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { "message", new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
    }

    [Theory]
    [MemberData(nameof(Ctor_String_Exception_TestData))]
    public void Ctor_String_Exception(string message, Exception innerException)
    {
        var exception = new RightsManagementException(message, innerException);
        Assert.Equal(RightsManagementFailureCode.Success, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Same(innerException, exception.InnerException);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    public static IEnumerable<object?[]> Ctor_String_Exception_Null_TestData()
    {
        yield return new object?[] { null };
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
    }

    [Theory]
    [MemberData(nameof(Ctor_String_Exception_Null_TestData))]
    public void Ctor_String_Exception_Null(Exception innerException)
    {
        var exception = new RightsManagementException(null, innerException);
        Assert.Equal(RightsManagementFailureCode.Success, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Same(innerException, exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    public static IEnumerable<object?[]> Ctor_RightsManagementFailureCode_String_Exception_TestData()
    {
        yield return new object?[] { RightsManagementFailureCode.Success, "", null };
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { RightsManagementFailureCode.ServerError, "message", new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { RightsManagementFailureCode.Success - 1, "message", new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
    }

    [Theory]
    [MemberData(nameof(Ctor_RightsManagementFailureCode_String_Exception_TestData))]
    public void Ctor_RightsManagementFailureCode_String_Exception(RightsManagementFailureCode failureCode, string message, Exception innerException)
    {
        var exception = new RightsManagementException(failureCode, message, innerException);
        Assert.Equal(failureCode, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Same(innerException, exception.InnerException);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    public static IEnumerable<object?[]> Ctor_RightsManagementFailureCode_String_Exception_Null_TestData()
    {
        yield return new object?[] { RightsManagementFailureCode.Success, null };
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { RightsManagementFailureCode.ServerError, new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning disable CA2201 // Do not raise reserved exception types
        yield return new object?[] { RightsManagementFailureCode.Success - 1, new Exception() };
#pragma warning restore CA2201 // Do not raise reserved exception types
    }

    [Theory]
    [MemberData(nameof(Ctor_RightsManagementFailureCode_String_Exception_Null_TestData))]
    public void Ctor_RightsManagementFailureCode_String_Exception_Null(RightsManagementFailureCode failureCode, Exception innerException)
    {
        var exception = new RightsManagementException(failureCode, null, innerException);
        Assert.Equal(failureCode, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.Same(innerException, exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

#pragma warning disable SYSLIB0050, SYSLIB0051
    [Fact]
    public void Ctor_SerializationInfo_StreamingContext()
    {
        var info = new SerializationInfo(typeof(RightsManagementException), new FormatterConverter());
        info.AddValue("FailureCode", RightsManagementFailureCode.ServerError);
        info.AddValue("Message", "message");
        info.AddValue("InnerException", new DivideByZeroException());
        info.AddValue("HelpURL", "HelpURL");
        info.AddValue("StackTraceString", "StackTraceString");
        info.AddValue("RemoteStackTraceString", "RemoteStackTraceString");
        info.AddValue("HResult", -2146233088);
        info.AddValue("Source", null);

        var exception = new SubRightsManagementException(info, new StreamingContext());
        Assert.Equal(RightsManagementFailureCode.ServerError, exception.FailureCode);
        Assert.Equal(-2146233088, exception.HResult);
        Assert.NotNull(exception.InnerException);
        Assert.Equal("message", exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Fact]
    public void Ctor_NullInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("info", () => new SubRightsManagementException((SerializationInfo)null!, default));
    }

    [Fact]
    public void GetObjectData_Invoke_Success()
    {
        var exception = new RightsManagementException();
        var info = new SerializationInfo(typeof(RightsManagementException), new FormatterConverter());
        var context = new StreamingContext();

        exception.GetObjectData(info, context);
        Assert.Equal(0, info.GetValue("FailureCode", typeof(int)));
        Assert.Equal(-2146233088, info.GetInt32("HResult"));
        Assert.Null(exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Fact]
    public void GetObjectData_InvokeCustomFailureCode_Success()
    {
        var exception = new RightsManagementException(RightsManagementFailureCode.ManifestPolicyViolation);
        var info = new SerializationInfo(typeof(RightsManagementException), new FormatterConverter());
        var context = new StreamingContext();

        exception.GetObjectData(info, context);
        Assert.Equal(-2147183860, info.GetValue("FailureCode", typeof(int)));
        Assert.Equal(-2146233088, info.GetInt32("HResult"));
        Assert.Null(exception.InnerException);
        Assert.NotEmpty(exception.Message);
        Assert.Null(exception.Source);
        Assert.Null(exception.TargetSite);
    }

    [Fact]
    public void GetObjectData_NullInfo_ThrowsArgumentNullException()
    {
        var exception = new RightsManagementException();
        Assert.Throws<ArgumentNullException>("info", () => exception.GetObjectData(null!, default));
    }
#pragma warning restore SYSLIB0050, SYSLIB0051

    private class SubRightsManagementException : RightsManagementException
    {
        public SubRightsManagementException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
