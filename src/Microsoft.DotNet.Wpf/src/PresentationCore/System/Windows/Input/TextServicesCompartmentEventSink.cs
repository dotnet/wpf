// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
// Description: Manages Text Services Compartment.
//
//

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

