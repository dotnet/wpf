// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PresentationCore.Tests.TestUtilities;

namespace PresentationCore.Tests.BinaryFormat;

public class NullTests
{
    [Fact]
    public void ObjectNullMultiple256_ThrowsOverflowOnWrite()
    {
        // We read a byte on the way in so there is nothing to check.

        NullRecord.ObjectNullMultiple256 objectNull = new(1000);

        using BinaryWriter writer = new(new MemoryStream());
        Action action = () => objectNull.Write(writer);
        action.Should().Throw<OverflowException>();
    }

    [Fact]
    public void ObjectNullMultiple256_WritesCorrectly()
    {
        NullRecord.ObjectNullMultiple256 objectNull = new(0xCA);

        byte[] buffer = new byte[2];
        using BinaryWriter writer = new(new MemoryStream(buffer));
        objectNull.Write(writer);
        buffer.Should().BeEquivalentTo(new byte[] { (byte)NullRecord.ObjectNullMultiple256.RecordType, 0xCA });
    }
}