// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPINFOLLEVELPROFILE_HPP__
#define __INTEROPINFOLLEVELPROFILE_HPP__
/*++
        
    Abstract:

        InfoLevelMask - for each level in the printing Win32 APIs there 
        is a level associated with in the enumeration. The enumeration 
        is marked with the Flags attribute. The idea is that a certain 
        managed attribute can be covered by multiple levels. 
        For instance: the attribute "Attributes" can be retrieved by calling
        LevelTwo or LevelOne.

        InfoAttributeData - is a value struct that hols information about each attribute,
        naming what levels cover the attribute and whether there is only only level that covers the attribute.
        The later information could be determined from the coverage mask, but for simplicity, it is done this way.

        InfoLevelThunk - This is the abstract base class for the object that it's being created for each level 
        that is being thunked to unmanaged code.
        This is the base class for Win32PrinterThunk and Win32DriverThunk.
        For a given attribute collection, we determine the Win32 calls that need to be done. 
        For each call, depending on the type of the LAPI object, we create a thunk object that has 
        the knowledge to make the Win32 call. The thunk objects are placed in InfoLevelCoverageList.
        The type mapping is done by the TypeToLevelMap utility class.        
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{
    [Flags]
    private enum class InfoLevelMask
    {
        NoLevel                         = 0x00000000,
        LevelOne                        = 0x00000001,
        LevelTwo                        = 0x00000002,
        LevelThree                      = 0x00000004,   
        LevelFour                       = 0x00000008,   
        LevelFive                       = 0x00000010,   
        LevelSix                        = 0x00000020,   
        LevelSeven                      = 0x00000040,   
        LevelEight                      = 0x00000080,   
        LevelNine                       = 0x00000100
    };

    private ref struct InfoAttributeData
    {
        InfoAttributeData(InfoLevelMask inMask, bool b) { mask = inMask; isSingleLevelCovered = b;}
        
        InfoLevelMask  mask;
        bool           isSingleLevelCovered;
    };


    private ref class InfoLevelThunk abstract
    {
        public:

        property
        UInt32
        Level
        {
            UInt32 get();
        }

        property
        InfoLevelMask
        LevelMask
        {
            InfoLevelMask get();
        }

        property
        IPrinterInfo^
        PrintInfoData
        {
            IPrinterInfo^ get();
            void set(IPrinterInfo^  printerInfo);    
        }

        property
        bool
        Succeeded
        {
            public:
                bool get();

            protected:
                void set(bool value);
        }

        void
        Release();

        virtual
        void
        CallWin32ApiToGetPrintInfoData(
            PrinterThunkHandler^   printThunkHandler,
            Object^                cookie
            ) = 0;

        virtual
        void
        BeginCallWin32ApiToSetPrintInfoData(
            PrinterThunkHandler^   printThunkHandler
            ) = 0;

        virtual
        void
        EndCallWin32ApiToSetPrintInfoData(
            PrinterThunkHandler^   printThunkHandler
            ) = 0;

        virtual
        Object^
        GetValueFromInfoData(
            String^                                 valueName
            );

        virtual
        Object^
        GetValueFromInfoData(
            String^                                 valueName,
            UInt32                                  index
            );

        virtual
        bool
        SetValueFromAttributeValue(
            String^                                 valueName,
            Object^                                 value
            );
        
        
        protected:

        InfoLevelThunk(
            UInt32                                  infoLevel,
            InfoLevelMask                           infoLevelMask
            );

        InfoLevelThunk(
            void
            );

        private:

        UInt32                          level;
        InfoLevelMask                   levelMask;
        IPrinterInfo^                   printInfoData;
        bool                            succeeded;
        bool                            isDisposed;
        
    };
}
}
}
}
#endif
