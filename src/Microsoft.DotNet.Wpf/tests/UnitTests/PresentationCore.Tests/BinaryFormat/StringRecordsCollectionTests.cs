// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PresentationCore.Tests.TestUtilities;

namespace PresentationCore.Tests.BinaryFormat;

public class StringRecordsCollectionTests
{
    [Fact]
    public void BasicFunctionality()
    {
        StringRecordsCollection collection = new(currentId: 1);
        IRecord record = collection.GetStringRecord("Foo");
        collection.CurrentId.Should().Be(2);
        record.Should().BeOfType<BinaryObjectString>();
        ((BinaryObjectString)record).ObjectId.Should().Be(1);

        record = collection.GetStringRecord("Foo");
        collection.CurrentId.Should().Be(2);
        record.Should().BeOfType<MemberReference>();
        ((MemberReference)record).IdRef.Should().Be(1);

        record = collection.GetStringRecord("Bar");
        collection.CurrentId.Should().Be(3);
        record.Should().BeOfType<BinaryObjectString>();
        ((BinaryObjectString)record).ObjectId.Should().Be(2);
    }
}