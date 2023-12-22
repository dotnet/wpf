// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization;
using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;

namespace PresentationCore.Tests.BinaryFormat;

public class BinaryFormattedTypes
{
    [Theory]
    [MemberData(nameof(BinaryFormattedTypes_TestData))]
    public void Types_UseBinaryFormatter(Type type)
    {
        bool iSerializable = type.IsAssignableTo(typeof(ISerializable));
#pragma warning disable SYSLIB0050 // Type or member is obsolete
        bool serializable = type.IsSerializable;
#pragma warning restore SYSLIB0050

        Assert.True(iSerializable || serializable, "Type should either implement ISerializable or be marked as [Serializable]");

        var converter = TypeDescriptor.GetConverter(type);
        Assert.False(
            converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string)),
            "Type should not be convertable back and forth to string.");
        Assert.False(
            converter.CanConvertFrom(typeof(byte[])) && converter.CanConvertTo(typeof(byte[])),
            "Type should not be convertable back and forth to byte[].");
    }

    public static TheoryData<Type> BinaryFormattedTypes_TestData => new()
    {
        typeof(Hashtable),
        typeof(ArrayList),
        typeof(PointF),
        typeof(RectangleF),
        typeof(List<string>),
    };

    [Theory]
    [MemberData(nameof(NotBinaryFormattedTypes_TestData))]
    public void Types_DoNotUseBinaryFormatter(Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);
        Assert.True(
            (converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string)))
            || (converter.CanConvertFrom(typeof(byte[])) && converter.CanConvertTo(typeof(byte[]))),
            "Type should be convertable back and forth to string or byte[].");
    }

    public static TheoryData<Type> NotBinaryFormattedTypes_TestData => new()
    {
        typeof(System.Drawing.Point),
        typeof(System.Drawing.Size),
        typeof(SizeF),
        typeof(Rectangle),
        typeof(Color)
    };
}