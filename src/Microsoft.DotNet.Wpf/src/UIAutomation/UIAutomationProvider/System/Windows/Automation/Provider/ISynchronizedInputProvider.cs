// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: SynchronizedInput control pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{

   
    /// <summary>
    /// Interface implemented by peers which support synchronized input
    /// </summary>
    [ComVisible(true)]
    [Guid("29db1a06-02ce-4cf7-9b42-565d4fab20ee")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ISynchronizedInputProvider
#else
    public interface ISynchronizedInputProvider
#endif
    {
        /// <summary>
        /// When called, framework checks for further input of the specified type
        /// When matching input is found, WPF checks the route of the incoming event,
        /// if the element which StartListening was originally called on is not in the route, 
        /// WPF will discard the input and fire InputDiscarded Automation event otherwise,
        /// If the input event reaches the element which which StartListening was originally 
        /// called on then WPF fires InputReachedTarget automation event else InputReachedOtherElement
        ///  automation event will be raised.
        /// 
        ///  This is a one-shot API; once the input is received, the framework
        /// stops checking input and continues as normal once one of the
        /// above events is fired.
        /// </summary>
        void  StartListening(SynchronizedInputType inputType);
        /// <summary>
        /// If the framework is currently listening for input, it will revert
        /// to normal operation. 
        /// </summary>
        void Cancel();

    }
}
