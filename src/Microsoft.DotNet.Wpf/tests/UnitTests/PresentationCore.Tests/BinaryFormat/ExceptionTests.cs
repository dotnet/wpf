// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;

namespace PresentationCore.Tests.BinaryFormat;

public class ExceptionTests
{
    [Fact]
    public void NotSupportedException_Parse()
    {
        BinaryFormattedObject format = new NotSupportedException().SerializeAndParse();
        format.RecordCount.Should().Be(3);
        var systemClass = (SystemClassWithMembersAndTypes)format[1];
        systemClass.Name.Should().Be(typeof(NotSupportedException).FullName);
        systemClass.MemberNames.Should().BeEquivalentTo(new string[]
        {
            "ClassName",
            "Message",
            "Data",
            "InnerException",
            "HelpURL",
            "StackTraceString",
            "RemoteStackTraceString",
            "RemoteStackIndex",
            "ExceptionMethod",
            "HResult",
            "Source",
            "WatsonBuckets"
        });

        systemClass.MemberTypeInfo.Should().BeEquivalentTo(new (BinaryType, object?)[]
        {
            (BinaryType.String, null),
            (BinaryType.String, null),
            (BinaryType.SystemClass, typeof(IDictionary).FullName),
            (BinaryType.SystemClass, typeof(Exception).FullName),
            (BinaryType.String, null),
            (BinaryType.String, null),
            (BinaryType.String, null),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.String, null),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.String, null),
            (BinaryType.PrimitiveArray, PrimitiveType.Byte)
        });

        systemClass.MemberValues.Should().BeEquivalentTo(new object[]
        {
            new BinaryObjectString(2, "System.NotSupportedException"),
            new BinaryObjectString(3, "Specified method is not supported."),
            ObjectNull.Instance,
            ObjectNull.Instance,
            ObjectNull.Instance,
            ObjectNull.Instance,
            ObjectNull.Instance,
            0,
            ObjectNull.Instance,
            unchecked((int)0x80131515),
            ObjectNull.Instance,
            ObjectNull.Instance
        });
    }
}