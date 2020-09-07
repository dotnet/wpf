// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __ENUMDATATHUNKOBJECT_HPP__
#define __ENUMDATATHUNKOBJECT_HPP__
/*++
    Abstract:

        This file contains the declaration for EnumDataThunkObject object.
        This object enumerates the objects of a given type by calling Win32 APIs. 
        The Win32 APIs to be called are determined based on the propertiesFilter
        parameter. The objects are created and only the properties in the propertiesFilter
        are populated with data. The objects are added to the printObjectsCollection. 
--*/

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    private ref class EnumDataThunkObject
    {
        public:

        EnumDataThunkObject(
            Type^       printingType
            );

        ~EnumDataThunkObject(
            void
            );

        void
        GetPrintSystemValuesPerPrintQueues(
            PrintServer^                        printServer,
            array<EnumeratedPrintQueueTypes>^   flags,
            System::
            Collections::
            Generic::Queue<PrintQueue^>^        printObjectsCollection,
            array<String^>^                     propertyFilter
            );

        void
        GetPrintSystemValuesPerPrintJobs(
            PrintQueue^                   printQueue,
            System::
            Collections::
            Generic::
            Queue<PrintSystemJobInfo^>^   printObjectsCollection,
            array<String^>^               propertyFilter,
            UInt32                        firstJobIndex,
            UInt32                        numberOfJobs
            );

        private:

        EnumDataThunkObject(
            void
            );

        UInt32
        TweakTheFlags(
            UInt32  attributeFlags
            );

        AttributeNameToInfoLevelMapping::
        InfoLevelCoverageList^
        BuildCoverageListAndEnumerateData(
            String^                                             serverName,
            UInt32                                              flags,            
            AttributeNameToInfoLevelMapping::InfoLevelMask      mask          
            );

        AttributeNameToInfoLevelMapping::
        InfoLevelCoverageList^
        BuildJobCoverageListAndEnumerateData(
            PrinterThunkHandler^                                    printingHandler,  
            AttributeNameToInfoLevelMapping::InfoLevelMask          mask,
            UInt32                                                  firstJobIndex,
            UInt32                                                  numberOfJobs
            );

        void
        MapEnumeratePrinterQueuesFlags(
            array<EnumeratedPrintQueueTypes>^                       enumerateFlags
            );

        Type^                                printingType;    
        bool                                 isDisposed;  
        UInt32                               win32EnumerationFlags;
        UInt32                               win32PrinterAttributeFlags;
    };
}
}
}
#endif
