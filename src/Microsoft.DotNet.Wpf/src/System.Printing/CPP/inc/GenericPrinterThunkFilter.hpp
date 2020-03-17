// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __GENERICPRINTERTHUNKFILTER_HPP__
#define __GENERICPRINTERTHUNKFILTER_HPP__
/*++
                                                                              
    Abstract:

        PrinterThunkingProfile - This object holds the knowledge about how a PrintQueue object
        thunks into unmanaged code. It does the mapping between the attributes 
        and Win32 levels for different types of operations, it does the level reconciliation 
        and based on a coverage mask, it creates the coverage list.
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{
namespace PrintQueueThunk
{
    private ref class PrinterThunkingProfile sealed : public IThunkingProfile
    {
        static
        PrinterThunkingProfile(
            void
            )
        {
            getAttributeMap  = gcnew Hashtable();
            setAttributeMap  = gcnew Hashtable();
            enumAttributeMap = gcnew Hashtable();

            RegisterAttributeMap();
        }

        public:

        static
        Hashtable^
        GetStaticAttributeMapForGetOperations(
            void
            );

        static
        Hashtable^
        GetStaticAttributeMapForSetOperations(
            void
            );

        static
        Hashtable^
        GetStaticAttributeMapForEnumOperations(
            void
            );

        virtual InfoLevelCoverageList^
        GetCoverageList(
            InfoLevelMask       coverageMask
            );

        static
        UInt64
        ReconcileMask(
            UInt64              coverageMask
            );

        private:

        static
        void
        RegisterAttributeMap(
            void
            );

        static
        Hashtable^      getAttributeMap;

        static
        Hashtable^      setAttributeMap;

        static
        Hashtable^      enumAttributeMap;

        static
        array<String^>^ attributeNames =
        {
            "HostingPrintServerName",
            "Name",
            "ShareName",
            "QueueDriverName",
            "QueuePortName",
            "Attributes",
            "Comment",
            "Location",
            "SecurityDescriptor",
            "QueuePrintProcessorName",
            "PrintProcessorDatatype",
            "PrintProcessorParameters",
            "SeparatorFile",
            "Priority",
            "DefaultPriority",
            "StartTimeOfDay",
            "UntilTimeOfDay",
            "AveragePagesPerMinute",
            "Flags",
            "NumberOfJobs",
            "UserDevMode",
            "DefaultDevMode",
            "Status",
            "Action",
            "ObjectGUID",
            "Description",
            "IsXpsEnabled"
        };

        static
        array<InfoAttributeData^>^ attributeLevelCoverageForGetOperations =
        {
            //S"ServerName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrinterName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"ShareName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DriverName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PortName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Attributes",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Comment",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo | InfoLevelMask::LevelOne), false),
            //S"Location",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"SD",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo | InfoLevelMask::LevelThree), false),
            //S"PrintProcessor"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessorDataType"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessorParameters"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"SeparatorFile"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Priority"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DefaultPriority"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"StartTime"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"UntilTime"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"AveragePpm"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Flags",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne), true),
            //S"Jobs"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"UserDevMode"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DefaultDevMode"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelEight), true),
            //S"Status"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Action"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelSeven), true),
            //S"ObjectGUID"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelSeven), true),
            //S"Description"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne), true),
            //S"IsXpsEnabled"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::NoLevel), true)
        };

        static
        array<InfoAttributeData^>^ attributeLevelCoverageForEnumOperations =
        {
            //S"ServerName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo | InfoLevelMask::LevelFour ), false),
            //S"PrinterName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo | InfoLevelMask::LevelFour), false),
            //S"ShareName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DriverName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PortName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Attributes",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo | InfoLevelMask::LevelFour), false),
            //S"Comment",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),            
            //S"Location",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),                         
            //S"SD",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessor"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessorDataType"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessorParameters"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"SeparatorFile"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Priority"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DefaultPriority"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"StartTime"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"UntilTime"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"AveragePpm"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Flags",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne), true),            
            //S"Jobs"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"UserDevMode"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DefaultDevMode"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Status"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Action"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::NoLevel), true),
            //S"ObjectGUID"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::NoLevel), true),
             //S"Description"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne), true),
            //S"IsXpsEnabled"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::NoLevel), true)
        };

        static
        array<InfoAttributeData^>^ attributeLevelCoverageForSetOperations =
        {
            //S"ServerName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrinterName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"ShareName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DriverName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PortName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Attributes",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Comment",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),            
            //S"Location",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),                         
            //S"SD",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo | InfoLevelMask::LevelThree), false),
            //S"PrintProcessor"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessorDataType"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessorParameters"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"SeparatorFile"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Priority"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"DefaultPriority"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"StartTime"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"UntilTime"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"AveragePpm"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Flags",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne), true),            
            //S"Jobs"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"UserDevMode"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelNine), true),
            //S"DefaultDevMode"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelEight), true),
            //S"Status"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Action"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelSeven), true),
            //S"ObjectGUID"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelSeven), true),
             //S"Description"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne), true),
            //S"IsXpsEnabled"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::NoLevel), true)
        };

        static
        array<InfoLevelMask>^  levelMaskTable = 
        {
            InfoLevelMask::NoLevel,
            InfoLevelMask::LevelOne,
            InfoLevelMask::LevelTwo,
            InfoLevelMask::LevelThree,
            InfoLevelMask::LevelFour,
            InfoLevelMask::LevelFive,
            InfoLevelMask::LevelSix,
            InfoLevelMask::LevelSeven,
            InfoLevelMask::LevelEight,
            InfoLevelMask::LevelNine
        };
        
    };
}
}
}
}
}
#endif

