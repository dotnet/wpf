// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __GENERICTYPEMAPPINGS_HPP__
#define __GENERICTYPEMAPPINGS_HPP__
/*++
    Abstract:

        TypeToLevelMap - utility class that does the type mapping between the LAPI and 
        thunk objects, for each type of operation ( get, set, enum ). 
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{
    private ref class TypeToLevelMap sealed
    {

        static
        TypeToLevelMap(
            void
            )
        {
            perTypeAttributesMapForGetOperations  = gcnew Hashtable();
            perTypeAttributesMapForSetOperations  = gcnew Hashtable();
            perTypeAttributesMapForEnumOperations = gcnew Hashtable();
            perTypeReconcileMap                   = gcnew Hashtable();

            BuildAttributesMapForGetOperations();
            BuildAttributesMapForEnumOperations();
            BuildAttributesMapForSetOperations();
            BuildReconcileMask();
        }

        public: 

        enum class OperationType
        {
            Get         = 1,
            Set         = 2,
            Enumeration = 3
        };

        delegate
        Hashtable^
        GetStaticAttributeMap(
            void
            );

        delegate
        UInt64
        ReconcileMask(
            UInt64          mask
            );

        static
        void
        BuildAttributesMapForGetOperations(
            void
            );

        static
        void
        BuildAttributesMapForSetOperations(
            void
            );

        static
        void
        BuildAttributesMapForEnumOperations(
            void
            );

        static
        void
        BuildReconcileMask(
            void
            );

        static
        GetStaticAttributeMap^
        GetStaticAttributesMapPerTypeForGetOperations(
            Type^    printingType
            );

        static
        GetStaticAttributeMap^
        GetStaticAttributesMapPerTypeForEnumOperations(
            Type^    printingType
            );

        static
        GetStaticAttributeMap^
        GetStaticAttributesMapPerTypeForSetOperations(
            Type^    printingType
            );

        static
        ReconcileMask^
        GetStaticReconcileMaskPerType(
            Type^    printingType
            );

        static
        IThunkingProfile^
        GetThunkProfileForPrintType(
            Type^     printingType
            );

        static
        InfoLevelMask
        GetCoverageMaskForPropertiesFilter(
            Type^               printingType,   
            OperationType       operationType,
            array<String^>^     propertiesFilter
            );

        static
        Hashtable^
        GetAttributeMapPerType(
            Type^               printingType,
            OperationType       operationType
            );

        static
        UInt64
        InvokeReconcileMaskPerType(
            Type^               printingType,
            InfoLevelMask       mask   
            );


        private:

        TypeToLevelMap(
            void
            )
        {
        }

        static
        Hashtable^   perTypeAttributesMapForGetOperations;

        static
        Hashtable^   perTypeAttributesMapForSetOperations;

        static
        Hashtable^   perTypeAttributesMapForEnumOperations;
        
        static
        Hashtable^   perTypeReconcileMap;
    };
}
}
}
}
#endif
