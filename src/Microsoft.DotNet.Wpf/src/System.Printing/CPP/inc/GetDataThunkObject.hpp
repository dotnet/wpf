// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __GETDATATHUNKOBJECT_HPP__
#define __GETDATATHUNKOBJECT_HPP__
/*++
        
    Abstract:

        This file contains the declaration for GetDataThunkObject object.
        This object populates the PrintSystemObject with data by calling Win32 APIs. 
        The Win32 APIs to be called are determined based on the propertiesFilter
        parameter. 
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    private ref class GetDataThunkObject
    {
        public:

        GetDataThunkObject(
            Type^       printingType
            );

        ~GetDataThunkObject(
            void
            );
        
        //
        // Populates an AttributeValue collection for a given type by 
        // calling the Win32 Get method.
        //
        bool
        PopulatePrintSystemObject(
            PrinterThunkHandler^                   printingHandler,
            PrintSystemObject^                     printObject,
            array<String^>^                        propertiesFilter
            );

        
        property
        Object^
        Cookie
        {
            void set(Object^ internalCookie);
            Object^ get();
        }

        private:

        GetDataThunkObject(
            void
            );

        AttributeNameToInfoLevelMapping::InfoLevelCoverageList^
        BuildCoverageListAndGetData(
            PrinterThunkHandler^                                        printingHandler,
            AttributeNameToInfoLevelMapping::InfoLevelMask              mask       
            );

        bool
        PopulateAttributesFromCoverageList(
            PrintSystemObject^                                          printObject,
            array<String^>^                                             propertiesFilter,
            AttributeNameToInfoLevelMapping::InfoLevelCoverageList^     coverageList
            );
               

        Type^       printingType;    
        bool        isDisposed;  
        Object^     cookie;
    };
}
}
}
#endif
