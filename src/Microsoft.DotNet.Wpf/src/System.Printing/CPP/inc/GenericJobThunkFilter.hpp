// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __GENERICJOBTHUNKFILTER_HPP__
#define __GENERICJOBTHUNKFILTER_HPP__
/*++
    Abstract:

        JobThunkingProfile - This object holds the knowledge about how a Job object
        thunks into unmanaged code. It does the mapping between the attributes 
        and Win32 levels, it does the level reconciliation and based on a 
        coverage mask, it creates the coverage list.
--*/

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{
namespace JobThunk
{

    private ref class JobThunkingProfile sealed : public IThunkingProfile
    {
        static
        JobThunkingProfile(
            void
            )
        {
            attributeMap = gcnew Hashtable();
            RegisterAttributeMap();
        }

        public:

        virtual InfoLevelCoverageList^
        GetCoverageList(
            InfoLevelMask    coverageMask
            );

        static
        Hashtable^
        GetStaticAttributeMap(
            void
            );

        static
        UInt64
        ReconcileMask(
            UInt64           coverageMask
            );
        
        private:
        
        static
        void
        RegisterAttributeMap(
            void
            );

        static
        Hashtable^      attributeMap;

        static
        array<InfoLevelMask>^   levelMaskTable = 
        {
            InfoLevelMask::NoLevel,
            InfoLevelMask::LevelOne,
            InfoLevelMask::LevelTwo,
            InfoLevelMask::LevelThree
        };

        static
        array<String^>^ attributeNames =
        {
            "JobIdentifier", //jobIdentifier
            "Name",
            "JobType",
            "JobContainerName",
            "NextJobId",
            "PrintQueue", //HostingPrintQueue
            "QueueDriverName",
            "PrintServer", //HostingPrintServer
            "Submitter", //UserName
            "NotifyName",
            "Document",
            "PrintProcessor",
            "PrintProcessorDatatype",
            "PrintProcessorParameters",
            "StatusDescription",
            "Status", //JobStatus
            "DevMode",
            "JobPriority", //Priority
            "PositionInQueue", //Position
            "NumberOfPages", //TotalPages
            "NumberOfPagesPrinted", //PagesPrinted
            "TimeJobSubmitted", //SubmittedTime
            "StartTimeOfDay",
            "UntilTimeOfDay",
            "JobSize", //Size
            "TimeSinceStartedPrinting", //TimeSinceSubmitted
            "SecurityDescriptor"
        };

        static
        array<InfoAttributeData^>^ attributeLevelCoverage =
        {
            //S"JobId",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo | InfoLevelMask::LevelThree), false),
            //S"Name",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"JobType",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::NoLevel), true),
            //S"JobContainerName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::NoLevel), true),
            //S"NextJobId",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelThree), true),
            //S"PrintQueue",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne| InfoLevelMask::LevelTwo), false),
            //S"QueueDriverName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintServer",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"Submitter",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"NotifyName",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Document",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"PrintProcessor",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"PrintProcessorDatatype"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"PrintProcessorParameters"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"StatusDescription"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"Status"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
             //S"DevMode"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"JobPriority"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"Position"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"TotalPages"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"PagesPrinted"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"SubmittedTime"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelOne | InfoLevelMask::LevelTwo), false),
            //S"StartTime",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
             //S"UntilTime",
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"Size"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"TimeSinceSubmitted"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true),
            //S"SecurityDescriptor"
            gcnew InfoAttributeData(static_cast<InfoLevelMask>(InfoLevelMask::LevelTwo), true)
        };


    };
}
}
}
}
}
#endif
