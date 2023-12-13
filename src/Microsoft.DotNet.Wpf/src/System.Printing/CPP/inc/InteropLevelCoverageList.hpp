// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPLEVELCOVERAGELIST_HPP__
#define __INTEROPLEVELCOVERAGELIST_HPP__
/*++
        
    Abstract:

        InfoLevelCoverageList - this is the container object that holds the thunk
        objects. The list is used to group the thunk objects and then enumerated
        to call the thunking on each object. The thunk objects are expected to be 
        of the same type. For instance, a InfoLevelCoverageList generated for 
        PrinterThunkingProfile will only hold objects of type Win32PrinterThunk.

        IThunkingProfile - interface exposed by thunking profile objects(see DriverThunkingProfile
        and PrinterThunkingProfile). The thunking profile objects hold the attribute to level mapping 
        for each type of printing object that thunks to unmanaged code.

--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{

    private ref class  InfoLevelCoverageList
    {
        public:

        InfoLevelCoverageList(
            void
            );

        void 
        Release(
            void
            );

        void
        Add(
            InfoLevelThunk^    profile
            );

        InfoLevelThunk^
        GetInfoLevelThunk(
            UInt64             mask
            );

        IEnumerator^
        GetEnumerator(
            void
            );

        property
        UInt32
        Count
        {
            void set(UInt32 value);
            UInt32 get();
        }

        private:

        ArrayList^      coverageList;
        bool            isDisposed;
        UInt32          objectCount;
    };

    private interface class IThunkingProfile
    {
        public:

        virtual
        InfoLevelCoverageList^
        GetCoverageList(
            InfoLevelMask
            );
    };
}
}
}
}
#endif
