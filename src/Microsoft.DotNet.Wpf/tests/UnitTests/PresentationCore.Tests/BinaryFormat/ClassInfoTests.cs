// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;
namespace PresentationCore.Tests.BinaryFormat;

public class ClassInfoTests
{
    private static readonly byte[] s_hashtableClassInfo = new byte[]
    {
        0x01, 0x00, 0x00, 0x00, 0x1c, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6d, 0x2e, 0x43, 0x6f, 0x6c, 0x6c,
        0x65, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x73, 0x2e, 0x48, 0x61, 0x73, 0x68, 0x74, 0x61, 0x62, 0x6c,
        0x65, 0x07, 0x00, 0x00, 0x00, 0x0a, 0x4c, 0x6f, 0x61, 0x64, 0x46, 0x61, 0x63, 0x74, 0x6f, 0x72,
        0x07, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6f, 0x6e, 0x08, 0x43, 0x6f, 0x6d, 0x70, 0x61, 0x72, 0x65,
        0x72, 0x10, 0x48, 0x61, 0x73, 0x68, 0x43, 0x6f, 0x64, 0x65, 0x50, 0x72, 0x6f, 0x76, 0x69, 0x64,
        0x65, 0x72, 0x08, 0x48, 0x61, 0x73, 0x68, 0x53, 0x69, 0x7a, 0x65, 0x04, 0x4b, 0x65, 0x79, 0x73,
        0x06, 0x56, 0x61, 0x6c, 0x75, 0x65, 0x73
    };

    [Fact]
    public void ClassInfo_ReadHashtable()
    {
        using BinaryReader reader = new(new MemoryStream(s_hashtableClassInfo));
        ClassInfo info = ClassInfo.Parse(reader, out Count memberCount);

        memberCount.Should().Be(7);
        info.ObjectId.Should().Be(1);
        info.Name.Should().Be("System.Collections.Hashtable");
        info.MemberNames.Should().BeEquivalentTo(new[]
        {
            "LoadFactor",
            "Version",
            "Comparer",
            "HashCodeProvider",
            "HashSize",
            "Keys",
            "Values"
        });
    }

    [Fact]
    public void ClassInfo_Hashtable_RoundTrip()
    {
        using BinaryReader reader = new(new MemoryStream(s_hashtableClassInfo));
        ClassInfo info = ClassInfo.Parse(reader, out Count memberCount);

        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        info.Write(writer);
        stream.Position = 0;

        using BinaryReader reader2 = new(stream);
        info = ClassInfo.Parse(reader2, out memberCount);

        memberCount.Should().Be(7);
        info.ObjectId.Should().Be(1);
        info.Name.Should().Be("System.Collections.Hashtable");
        info.MemberNames.Should().BeEquivalentTo(new[]
        {
            "LoadFactor",
            "Version",
            "Comparer",
            "HashCodeProvider",
            "HashSize",
            "Keys",
            "Values"
        });
    }

    [Fact]
    public void MemberTypeInfo_ReadHashtable_TooShort()
    {
        MemoryStream stream = new(s_hashtableClassInfo);
        stream.SetLength(stream.Length - 1);
        using BinaryReader reader = new(stream);
        Action action = () => ClassInfo.Parse(reader, out _);
        action.Should().Throw<EndOfStreamException>();
    }
}