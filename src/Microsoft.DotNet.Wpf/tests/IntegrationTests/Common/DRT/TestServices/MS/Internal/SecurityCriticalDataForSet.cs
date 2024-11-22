// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
// Description:
//              This is a helper class to facilate the storage of data that's Critical for set.
//              The data itself is not information disclosure but the value controls a critical
//              operation.
//
//              For example a filepath variable might control what part of the file system the
//              code gets access to.
//
// History:
//  01/30/05 : prasadt Created. 
//---------------------------------------------------------------------------
using System ; 
using System.Security ;

#if WINDOWS_BASE
    using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
    using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
    using MS.Internal.PresentationFramework;
#elif PRESENTATIONUI
    using MS.Internal.PresentationUI;
#elif UIAUTOMATIONTYPES
    using MS.Internal.UIAutomationTypes;
#elif DRT
    using MS.Internal.Drt;
#elif SYSTEM_XAML
    using MS.Internal.WindowsBase;
#else
#error Attempt to use FriendAccessAllowedAttribute from an unknown assembly.
using MS.Internal.YourAssemblyName;
#endif

#if SYSTEM_XAML
namespace MS.Internal.Xaml
#else
namespace MS.Internal
#endif
{
    [Serializable]
    public struct SecurityCriticalDataForSet<T>
    {
        internal SecurityCriticalDataForSet(T value)
        { 
            _value = value; 
        }

        internal T Value 
        {
        #if DEBUG
            [System.Diagnostics.DebuggerStepThrough]
        #endif
            get
            {
                return _value;
            }

        #if DEBUG
            [System.Diagnostics.DebuggerStepThrough]
        #endif
            set
            {
                _value = value;
            }
        }

        private T _value;
    }
}

