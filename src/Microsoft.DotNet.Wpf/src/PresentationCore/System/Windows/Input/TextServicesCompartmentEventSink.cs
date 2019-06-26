// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Manages Text Services Compartment.
//
//

using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

using System.Diagnostics;
using System.Collections;
using MS.Utility;
using MS.Win32;

namespace System.Windows.Input 
{
    //------------------------------------------------------
    //
    //  TextServicesCompartmentManager class
    //
    //------------------------------------------------------
 
    /// <summary>
    /// This is a class to have a real implement of ITfCompartmentEventSink.
    /// </summary>
    internal class TextServicesCompartmentEventSink : UnsafeNativeMethods.ITfCompartmentEventSink
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal TextServicesCompartmentEventSink(InputMethod inputmethod)
        {
            _inputmethod = inputmethod;
        }

        //------------------------------------------------------
        //
        //  Public Method
        //
        //------------------------------------------------------

        /// <summary>
        ///  This is OnChange method of ITfCompartmentEventSink internface.
        /// </summary> 
        public void OnChange(ref Guid rguid)
        {
            _inputmethod.OnChange(ref rguid);
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        private InputMethod _inputmethod;

        #endregion Private Fields
    }
}

