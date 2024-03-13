// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using PresentationCore.Tests.TestUtilities;

namespace PresentationCore.Tests.BinaryFormat;

public class PointFTests
{
    [Fact]
    public void PointF_Parse()
    {
        BinaryFormattedObject format = new PointF().SerializeAndParse();

        BinaryLibrary binaryLibrary = (BinaryLibrary)format[1];
        binaryLibrary.LibraryId.Should().Be(2);
        binaryLibrary.LibraryName.Should().Be("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

        ClassWithMembersAndTypes classInfo = (ClassWithMembersAndTypes)format[2];
        classInfo.ObjectId.Should().Be(1);
        classInfo.Name.Should().Be("System.Drawing.PointF");
        classInfo.MemberNames.Should().BeEquivalentTo(new string[] { "x", "y" });
        classInfo.MemberValues.Should().BeEquivalentTo(new object[] { 0.0f, 0.0f });
        classInfo.MemberTypeInfo.Should().BeEquivalentTo(new[]
        {
            (BinaryType.Primitive, PrimitiveType.Single),
            (BinaryType.Primitive, PrimitiveType.Single)
        });
    }
}