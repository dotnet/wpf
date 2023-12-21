// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using PresentationCore.Tests.TestUtilities;

namespace PresentationCore.Tests.BinaryFormat;

public class BinaryFormattedObjectTests
{
    [Fact]
    public void ReadHeader()
    {
        BinaryFormattedObject format = "Hello World.".SerializeAndParse();
        SerializationHeader header = (SerializationHeader)format[0];
        header.MajorVersion.Should().Be(1);
        header.MinorVersion.Should().Be(0);
        header.RootId.Should().Be(1);
        header.HeaderId.Should().Be(-1);
    }

    [Theory]
    [InlineData("Hello there.")]
    [InlineData("")]
    [InlineData("Embedded\0 Null.")]
    public void ReadBinaryObjectString(string testString)
    {
        BinaryFormattedObject format = testString.SerializeAndParse();
        BinaryObjectString stringRecord = (BinaryObjectString)format[1];
        stringRecord.ObjectId.Should().Be(1);
        stringRecord.Value.Should().Be(testString);
    }

    [Fact]
    public void ReadEmptyHashTable()
    {
        BinaryFormattedObject format = new Hashtable().SerializeAndParse();

        SystemClassWithMembersAndTypes systemClass = (SystemClassWithMembersAndTypes)format[1];
        systemClass.ObjectId.Should().Be(1);
        systemClass.Name.Should().Be("System.Collections.Hashtable");
        systemClass.MemberNames.Should().BeEquivalentTo(new[]
        {
            "LoadFactor",
            "Version",
            "Comparer",
            "HashCodeProvider",
            "HashSize",
            "Keys",
            "Values"
        });

        systemClass.MemberTypeInfo.Should().BeEquivalentTo(new (BinaryType Type, object? Info)[]
        {
            (BinaryType.Primitive, PrimitiveType.Single),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.SystemClass, "System.Collections.IComparer"),
            (BinaryType.SystemClass, "System.Collections.IHashCodeProvider"),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.ObjectArray, null),
            (BinaryType.ObjectArray, null)
        });

        systemClass.MemberValues.Should().BeEquivalentTo(new object?[]
        {
            0.72f,
            0,
            ObjectNull.Instance,
            ObjectNull.Instance,
            3,
            new MemberReference(2),
            new MemberReference(3)
        });

        ArraySingleObject array = (ArraySingleObject)format[2];
        array.ArrayInfo.ObjectId.Should().Be(2);
        array.ArrayInfo.Length.Should().Be(0);

        array = (ArraySingleObject)format[3];
        array.ArrayInfo.ObjectId.Should().Be(3);
        array.ArrayInfo.Length.Should().Be(0);
    }

    [Fact]
    public void ReadHashTableWithStringPair()
    {
        BinaryFormattedObject format = new Hashtable()
        {
            { "This", "That" }
        }.SerializeAndParse();

        SystemClassWithMembersAndTypes systemClass = (SystemClassWithMembersAndTypes)format[1];

        systemClass.MemberTypeInfo.Should().BeEquivalentTo(new (BinaryType Type, object? Info)[]
        {
            (BinaryType.Primitive, PrimitiveType.Single),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.SystemClass, "System.Collections.IComparer"),
            (BinaryType.SystemClass, "System.Collections.IHashCodeProvider"),
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.ObjectArray, null),
            (BinaryType.ObjectArray, null)
        });

        systemClass.MemberValues.Should().BeEquivalentTo(new object?[]
        {
            0.72f,
            1,
            ObjectNull.Instance,
            ObjectNull.Instance,
            3,
            new MemberReference(2),
            new MemberReference(3)
        });

        ArraySingleObject array = (ArraySingleObject)format[2];
        array.ArrayInfo.ObjectId.Should().Be(2);
        array.ArrayInfo.Length.Should().Be(1);
        BinaryObjectString value = (BinaryObjectString)array.ArrayObjects[0];
        value.ObjectId.Should().Be(4);
        value.Value.Should().Be("This");

        array = (ArraySingleObject)format[3];
        array.ArrayInfo.ObjectId.Should().Be(3);
        array.ArrayInfo.Length.Should().Be(1);
        value = (BinaryObjectString)array.ArrayObjects[0];
        value.ObjectId.Should().Be(5);
        value.Value.Should().Be("That");
    }

    [Fact]
    public void ReadHashTableWithRepeatedStrings()
    {
        BinaryFormattedObject format = new Hashtable()
        {
            { "This", "That" },
            { "TheOther", "This" },
            { "That", "This" }
        }.SerializeAndParse();

        // The collections themselves get ids first before the strings do.
        // Everything in the second array is a string reference.
        ArraySingleObject array = (ArraySingleObject)format[3];
        array.ObjectId.Should().Be(3);
        array[0].Should().BeOfType<MemberReference>();
        array[1].Should().BeOfType<MemberReference>();
        array[2].Should().BeOfType<MemberReference>();
    }

    [Fact]
    public void ReadHashTableWithNullValues()
    {
        BinaryFormattedObject format = new Hashtable()
        {
            { "Yowza", null },
            { "Youza", null },
            { "Meeza", null }
        }.SerializeAndParse();

        SystemClassWithMembersAndTypes systemClass = (SystemClassWithMembersAndTypes)format[1];

        systemClass.MemberValues.Should().BeEquivalentTo(new object?[]
        {
            0.72f,
            4,
            ObjectNull.Instance,
            ObjectNull.Instance,
            7,
            new MemberReference(2),
            new MemberReference(3)
        });

        ArrayRecord array = (ArrayRecord)format[(MemberReference)systemClass.MemberValues[5]];

        array.ArrayInfo.ObjectId.Should().Be(2);
        array.ArrayInfo.Length.Should().Be(3);
        BinaryObjectString value = (BinaryObjectString)array.ArrayObjects[0];
        value.ObjectId.Should().Be(4);
        value.Value.Should().BeOneOf("Yowza", "Youza", "Meeza");

        array = (ArrayRecord)format[(MemberReference)systemClass["Values"]];
        array.ArrayInfo.ObjectId.Should().Be(3);
        array.ArrayInfo.Length.Should().Be(3);
        array.ArrayObjects[0].Should().BeOfType<ObjectNull>();
    }

    [Fact]
    public void ReadObject()
    {
        BinaryFormattedObject format = new object().SerializeAndParse();
        format[1].Should().BeOfType<SystemClassWithMembersAndTypes>();
    }

    [Fact]
    public void ReadStruct()
    {
        ValueTuple<int> tuple = new(355);
        BinaryFormattedObject format = tuple.SerializeAndParse();
        format[1].Should().BeOfType<SystemClassWithMembersAndTypes>();
    }

    [Fact]
    public void ReadSimpleSerializableObject()
    {
        BinaryFormattedObject format = new SimpleSerializableObject().SerializeAndParse();

        BinaryLibrary library = (BinaryLibrary)format[1];
        library.LibraryName.Should().Be(typeof(BinaryFormattedObjectTests).Assembly.FullName);
        library.LibraryId.Should().Be(2);

        ClassWithMembersAndTypes @class = (ClassWithMembersAndTypes)format[2];
        @class.ObjectId.Should().Be(1);
        @class.Name.Should().Be(typeof(SimpleSerializableObject).FullName);
        @class.MemberNames.Should().BeEmpty();
        @class.LibraryId.Should().Be(2);
        @class.MemberTypeInfo.Should().BeEmpty();

        format[3].Should().BeOfType<MessageEnd>();
    }

    [Fact]
    public void ReadNestedSerializableObject()
    {
        BinaryFormattedObject format = new NestedSerializableObject().SerializeAndParse();

        BinaryLibrary library = (BinaryLibrary)format[1];
        library.LibraryName.Should().Be(typeof(BinaryFormattedObjectTests).Assembly.FullName);
        library.LibraryId.Should().Be(2);

        ClassWithMembersAndTypes @class = (ClassWithMembersAndTypes)format[2];
        @class.ObjectId.Should().Be(1);
        @class.Name.Should().Be(typeof(NestedSerializableObject).FullName);
        @class.MemberNames.Should().BeEquivalentTo(new[] { "_object", "_meaning" });
        @class.LibraryId.Should().Be(2);
        @class.MemberTypeInfo.Should().BeEquivalentTo(new (BinaryType Type, object? Info)[]
        {
            (BinaryType.Class, new ClassTypeInfo(typeof(SimpleSerializableObject).FullName!, 2)),
            (BinaryType.Primitive, PrimitiveType.Int32)
        });
        @class.MemberValues.Should().BeEquivalentTo(new object?[]
        {
            new MemberReference(3),
            42
        });

        @class = (ClassWithMembersAndTypes)format[3];
        @class.ObjectId.Should().Be(3);
        @class.Name.Should().Be(typeof(SimpleSerializableObject).FullName);
        @class.MemberNames.Should().BeEmpty();
        @class.LibraryId.Should().Be(2);
        @class.MemberTypeInfo.Should().BeEmpty();

        format[4].Should().BeOfType<MessageEnd>();
    }

    [Fact]
    public void ReadTwoIntObject()
    {
        BinaryFormattedObject format = new TwoIntSerializableObject().SerializeAndParse();

        BinaryLibrary library = (BinaryLibrary)format[1];
        library.LibraryName.Should().Be(typeof(BinaryFormattedObjectTests).Assembly.FullName);
        library.LibraryId.Should().Be(2);

        ClassWithMembersAndTypes @class = (ClassWithMembersAndTypes)format[2];
        @class.ObjectId.Should().Be(1);
        @class.Name.Should().Be(typeof(TwoIntSerializableObject).FullName);
        @class.MemberNames.Should().BeEquivalentTo(new[] { "_value", "_meaning" });
        @class.LibraryId.Should().Be(2);
        @class.MemberTypeInfo.Should().BeEquivalentTo(new (BinaryType Type, object? Info)[]
        {
            (BinaryType.Primitive, PrimitiveType.Int32),
            (BinaryType.Primitive, PrimitiveType.Int32)
        });

        @class.MemberValues.Should().BeEquivalentTo(new object?[]
        {
            1970,
            42
        });

        format[3].Should().BeOfType<MessageEnd>();
    }

    [Fact]
    public void ReadRepeatedNestedObject()
    {
        BinaryFormattedObject format = new RepeatedNestedSerializableObject().SerializeAndParse();
        ClassWithMembersAndTypes firstClass = (ClassWithMembersAndTypes)format[3];
        ClassWithId classWithId = (ClassWithId)format[4];
        classWithId.MetadataId.Should().Be(firstClass.ObjectId);
        classWithId.MemberValues.Should().BeEquivalentTo(new object[] { 1970, 42 });
    }

    [Fact]
    public void ReadPrimitiveArray()
    {
        BinaryFormattedObject format = new int[] { 10, 9, 8, 7 }.SerializeAndParse();

        ArraySinglePrimitive array = (ArraySinglePrimitive)format[1];
        array.ArrayInfo.Length.Should().Be(4);
        array.PrimitiveType.Should().Be(PrimitiveType.Int32);
        array.ArrayObjects.Should().BeEquivalentTo(new object[] { 10, 9, 8, 7 });
    }

    [Fact]
    public void ReadStringArray()
    {
        BinaryFormattedObject format = new string[] { "Monday", "Tuesday", "Wednesday" }.SerializeAndParse();
        ArraySingleString array = (ArraySingleString)format[1];
        array.ArrayInfo.ObjectId.Should().Be(1);
        array.ArrayInfo.Length.Should().Be(3);
        BinaryObjectString value = (BinaryObjectString)array.ArrayObjects[0];
    }

    [Fact]
    public void ReadStringArrayWithNulls()
    {
        BinaryFormattedObject format = new string?[] { "Monday", null, "Wednesday", null, null, null }.SerializeAndParse();
        ArraySingleString array = (ArraySingleString)format[1];
        array.ArrayInfo.ObjectId.Should().Be(1);
        array.ArrayInfo.Length.Should().Be(6);
        array.ArrayObjects.Should().BeEquivalentTo(new object?[]
        {
            new BinaryObjectString(2, "Monday"),
            ObjectNull.Instance,
            new BinaryObjectString(3, "Wednesday"),
            ObjectNull.Instance,
            ObjectNull.Instance,
            ObjectNull.Instance
        });
        BinaryObjectString value = (BinaryObjectString)array.ArrayObjects[0];
    }

    [Fact]
    public void ReadDuplicatedStringArray()
    {
        BinaryFormattedObject format = new string[] { "Monday", "Tuesday", "Monday" }.SerializeAndParse();
        ArraySingleString array = (ArraySingleString)format[1];
        array.ArrayInfo.ObjectId.Should().Be(1);
        array.ArrayInfo.Length.Should().Be(3);
        BinaryObjectString value = (BinaryObjectString)array.ArrayObjects[0];
        MemberReference reference = (MemberReference)array.ArrayObjects[2];
        reference.IdRef.Should().Be(value.ObjectId);
    }

    [Fact]
    public void ReadObjectWithNullableObjects()
    {
        BinaryFormattedObject format = new ObjectWithNullableObjects().SerializeAndParse();
        ClassWithMembersAndTypes classRecord = (ClassWithMembersAndTypes)format[2];
        BinaryLibrary library = (BinaryLibrary)format[classRecord.LibraryId];
    }

    [Fact]
    public void ReadNestedObjectWithNullableObjects()
    {
        BinaryFormattedObject format = new NestedObjectWithNullableObjects().SerializeAndParse();
        ClassWithMembersAndTypes classRecord = (ClassWithMembersAndTypes)format[2];
        BinaryLibrary library = (BinaryLibrary)format[classRecord.LibraryId];
    }

    [Serializable]
    private class SimpleSerializableObject
    {
    }

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0414  // Field is assigned but its value is never used
#pragma warning disable CS0649  // Field is never assigned to, and will always have its default value null
#pragma warning disable CA1823 // Avoid unused private fields
    [Serializable]
    private class ObjectWithNullableObjects
    {
        public object? First;
        public object? Second;
        public object? Third;
    }

    [Serializable]
    private class NestedObjectWithNullableObjects
    {
        public ObjectWithNullableObjects? First;
        public ObjectWithNullableObjects? Second;
        public ObjectWithNullableObjects? Third = new();
    }

    [Serializable]
    private class NestedSerializableObject
    {
        private readonly SimpleSerializableObject _object = new();
        private readonly int _meaning = 42;
    }

    [Serializable]
    private class TwoIntSerializableObject
    {
        private readonly int _value = 1970;
        private readonly int _meaning = 42;
    }

    [Serializable]
    private class RepeatedNestedSerializableObject
    {
        private readonly TwoIntSerializableObject _first = new();
        private readonly TwoIntSerializableObject _second = new();
    }
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0414  // Field is assigned but its value is never used
#pragma warning restore CS0649  // Field is never assigned to, and will always have its default value null
#pragma warning restore CA1823 // Avoid unused private fields
}