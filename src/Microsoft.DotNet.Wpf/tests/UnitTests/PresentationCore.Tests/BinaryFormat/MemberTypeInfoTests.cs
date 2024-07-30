// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PresentationCore.Tests.TestUtilities;

namespace PresentationCore.Tests.BinaryFormat;

public class MemberTypeInfoTests
{
    private static readonly byte[] s_hashtableMemberInfo = new byte[]
    {
        0x00, 0x00, 0x03, 0x03, 0x00, 0x05, 0x05, 0x0b, 0x08, 0x1c, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6d,
        0x2e, 0x43, 0x6f, 0x6c, 0x6c, 0x65, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x73, 0x2e, 0x49, 0x43, 0x6f,
        0x6d, 0x70, 0x61, 0x72, 0x65, 0x72, 0x24, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6d, 0x2e, 0x43, 0x6f,
        0x6c, 0x6c, 0x65, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x73, 0x2e, 0x49, 0x48, 0x61, 0x73, 0x68, 0x43,
        0x6f, 0x64, 0x65, 0x50, 0x72, 0x6f, 0x76, 0x69, 0x64, 0x65, 0x72, 0x08
    };

    [Fact]
    public void MemberTypeInfo_ReadHashtable()
    {
        using BinaryReader reader = new(new MemoryStream(s_hashtableMemberInfo));
        MemberTypeInfo info = MemberTypeInfo.Parse(reader, 7);

        info.Should().BeEquivalentTo(new (BinaryType Type, object? Info)[]
        {
            (BinaryType.Primitive, PrimitiveType.Single),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.SystemClass, "System.Collections.IComparer"),
            (BinaryType.SystemClass, "System.Collections.IHashCodeProvider"),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.ObjectArray, null),
            (BinaryType.ObjectArray, null)
        });
    }

    [Fact]
    public void MemberTypeInfo_HashtableRoundTrip()
    {
        using BinaryReader reader = new(new MemoryStream(s_hashtableMemberInfo));
        MemberTypeInfo info = MemberTypeInfo.Parse(reader, 7);

        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        info.Write(writer);
        stream.Position = 0;

        using BinaryReader reader2 = new(stream);
        info = MemberTypeInfo.Parse(reader2, 7);
        info.Should().BeEquivalentTo(new (BinaryType Type, object? Info)[]
        {
            (BinaryType.Primitive, PrimitiveType.Single),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.SystemClass, "System.Collections.IComparer"),
            (BinaryType.SystemClass, "System.Collections.IHashCodeProvider"),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.ObjectArray, null),
            (BinaryType.ObjectArray, null)
        });
    }

    [Fact]
    public void MemberTypeInfo_ReadHashtable_TooShort()
    {
        MemoryStream stream = new(s_hashtableMemberInfo);
        stream.SetLength(stream.Length - 1);
        using BinaryReader reader = new(stream);
        Action action = () => MemberTypeInfo.Parse(reader, 7);
        action.Should().Throw<EndOfStreamException>();
    }
}