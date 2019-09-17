// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;


#if PBTCOMPILER
namespace MS.Internal.Markup
#else
using System.Windows;

namespace System.Windows.Markup
#endif
{
    // Helper functions for baml records

    static internal class BamlRecordHelper
    {
#if !PBTCOMPILER
        // 
        // This method checks to see if the baml record type is one of the records used
        // to build up the map table, e.g. type information.
        //
        static internal bool IsMapTableRecordType( BamlRecordType bamlRecordType )
        {
            switch( bamlRecordType )
            {
                case BamlRecordType.PIMapping:
                case BamlRecordType.AssemblyInfo:
                case BamlRecordType.TypeInfo:
                case BamlRecordType.TypeSerializerInfo:
                case BamlRecordType.AttributeInfo:
                case BamlRecordType.StringInfo:
                    return true;

                default:
                    return false;
            }
        }

        internal static bool IsDebugBamlRecordType(BamlRecordType recordType)
        {
            if (   recordType == BamlRecordType.LineNumberAndPosition
                || recordType == BamlRecordType.LinePosition )
            {
                return true;
            }
            return false;
        }

        // Does the given Baml Record have a Debug Baml Record in its Next link.
        internal static bool HasDebugExtensionRecord(bool isDebugBamlStream, BamlRecord bamlRecord)
        {
            if (isDebugBamlStream && (bamlRecord.Next != null))
            {
                if (IsDebugBamlRecordType(bamlRecord.Next.RecordType))
                {
                    return true;
                }
            }
            return false;
        }
#endif

        internal static bool DoesRecordTypeHaveDebugExtension(BamlRecordType recordType)
        {
            switch(recordType)
            {
                case BamlRecordType.ElementStart:
                case BamlRecordType.ElementEnd:
                case BamlRecordType.Property:
                case BamlRecordType.PropertyComplexStart:
                case BamlRecordType.PropertyArrayStart:
                case BamlRecordType.PropertyIListStart:
                case BamlRecordType.PropertyIDictionaryStart:
                case BamlRecordType.XmlnsProperty:
                case BamlRecordType.PIMapping:
                case BamlRecordType.PropertyTypeReference:
                case BamlRecordType.PropertyWithExtension:
                case BamlRecordType.PropertyWithConverter:
                case BamlRecordType.KeyElementStart:
                case BamlRecordType.ConnectionId:
                case BamlRecordType.ContentProperty:
                case BamlRecordType.StaticResourceStart:
                case BamlRecordType.PresentationOptionsAttribute:
                    return true;

                case BamlRecordType.DocumentStart:
                case BamlRecordType.DocumentEnd:                // End record
                case BamlRecordType.PropertyCustom:             // The "custom" size of this is a problem
                case BamlRecordType.PropertyComplexEnd:         // End record
                case BamlRecordType.PropertyArrayEnd:           // End record
                case BamlRecordType.PropertyIListEnd:           // End record
                case BamlRecordType.PropertyIDictionaryEnd:     // End record
                case BamlRecordType.LiteralContent:             // Not needed
                case BamlRecordType.Text:                       // Not needed
                case BamlRecordType.TextWithConverter:          // Not common enough
                case BamlRecordType.RoutedEvent:                // Not common enough
                case BamlRecordType.ClrEvent:                   // Not common enough
                case BamlRecordType.XmlAttribute:               // Not common enough
                case BamlRecordType.ProcessingInstruction:      // Not common enough
                case BamlRecordType.Comment:                    // Not common enough
                case BamlRecordType.DefTag:                     // Not common enough
                case BamlRecordType.DefAttribute:               // Not common enough
                case BamlRecordType.EndAttributes:              // Not common enough
                case BamlRecordType.AssemblyInfo:               // Info records (in general) don't advance file position
                case BamlRecordType.TypeInfo:                   // Info records (in general) don't advance file position
                case BamlRecordType.TypeSerializerInfo:         // Not common enough
                case BamlRecordType.AttributeInfo:              // Info records (in general) don't advance file position
                case BamlRecordType.StringInfo:                 // Info records (in general) don't advance file position
                case BamlRecordType.PropertyStringReference:    // Not common enough
                case BamlRecordType.DeferableContentStart:      // This would complicate Deferable Content Size
                case BamlRecordType.ConstructorParametersStart: // Not Needed
                case BamlRecordType.ConstructorParametersEnd:   // End record
                case BamlRecordType.ConstructorParameterType:   // Not Needed
                case BamlRecordType.NamedElementStart:          // Not common enough
                case BamlRecordType.TextWithId:                 // Not Needed
                case BamlRecordType.LineNumberAndPosition:      // This would become recursive
                case BamlRecordType.LinePosition:               // This would become recursive
                case BamlRecordType.DefAttributeKeyString:
                case BamlRecordType.DefAttributeKeyType:
                case BamlRecordType.KeyElementEnd:
                case BamlRecordType.StaticResourceEnd:
                case BamlRecordType.StaticResourceId:
                case BamlRecordType.OptimizedStaticResource:
                case BamlRecordType.PropertyWithStaticResourceId:
                    return false;

                default:
                    Debug.Assert(false, "Unhandled case in DoesRecordTypeHaveDebugExtension");
                    return false;
            }
        }
    }
}

