// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Baml2006
{
    internal enum Baml2006RecordType : byte
    {
        Unknown = 0,
        DocumentStart,              // 1
        DocumentEnd,                // 2
        ElementStart,               // 3
        ElementEnd,                 // 4
        Property,                   // 5
        PropertyCustom,             // 6
        PropertyComplexStart,       // 7
        PropertyComplexEnd,         // 8
        PropertyArrayStart,         // 9
        PropertyArrayEnd,           // 10
        PropertyIListStart,         // 11
        PropertyIListEnd,           // 12
        PropertyIDictionaryStart,   // 13
        PropertyIDictionaryEnd,     // 14
        LiteralContent,             // 15
        Text,                       // 16
        TextWithConverter,          // 17
        RoutedEvent,                // 18       Untested because never seen this record in actual BAML
        ClrEvent,                   // 19       NOT IMPLEMENTED in Avalon
        XmlnsProperty,              // 20
        XmlAttribute,               // 21       NOT IMPLEMENTED in Avalon
        ProcessingInstruction,      // 22       NOT IMPLEMENTED in Avalon
        Comment,                    // 23       NOT IMPLEMENTED in Avalon
        DefTag,                     // 24       NOT IMPLEMENTED in Avalon
        DefAttribute,               // 25
        EndAttributes,              // 26       NOT IMPLEMENTED in Avalon
        PIMapping,                  // 27
        AssemblyInfo,               // 28
        TypeInfo,                   // 29
        TypeSerializerInfo,         // 30       Untested because never seen this record in actual BAML
        AttributeInfo,              // 31
        StringInfo,                 // 32
        PropertyStringReference,    // 33       Untested because never seen this record in actual BAML
        PropertyTypeReference,      // 34
        PropertyWithExtension,      // 35
        PropertyWithConverter,      // 36
        DeferableContentStart,      // 37
        DefAttributeKeyString,      // 38
        DefAttributeKeyType,        // 39
        KeyElementStart,            // 40
        KeyElementEnd,              // 41
        ConstructorParametersStart, // 42
        ConstructorParametersEnd,   // 43
        ConstructorParameterType,   // 44
        ConnectionId,               // 45
        ContentProperty,            // 46
        NamedElementStart,          // 47
        StaticResourceStart,        // 48
        StaticResourceEnd,          // 49
        StaticResourceId,           // 50
        TextWithId,                 // 51
        PresentationOptionsAttribute,// 52
        LineNumberAndPosition,      // 53
        LinePosition,               // 54
        OptimizedStaticResource,     // 55,
        PropertyWithStaticResourceId,// 56,
        LastRecordType
    }
}
