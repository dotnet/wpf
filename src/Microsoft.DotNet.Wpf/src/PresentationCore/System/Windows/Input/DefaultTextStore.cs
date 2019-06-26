// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: the default text store that allows default TSF enabling.
//
//

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32;

namespace System.Windows.Input
{
    // This class has the default text store implementation.
    // DefaultTextStore is a TextStore to be shared by any element of the Dispatcher.
    // When the keyboard focus is on the element, Cicero input goes into this by default.
    // This DefaultTextStore will be used unless an Element (such as TextBox) set
    // the focus on the document manager for its own TextStore.
    internal class DefaultTextStore :  UnsafeNativeMethods.ITfContextOwner,
                                       UnsafeNativeMethods.ITfContextOwnerCompositionSink,
                                       UnsafeNativeMethods.ITfTransitoryExtensionSink
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a DefaultTextStore instance.
        internal DefaultTextStore(Dispatcher dispatcher)
        {
            // Save the target Dispatcher. 
            _dispatcher = dispatcher;

            _editCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            _transitoryExtensionSinkCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods - ITfContextOwner
        //
        //------------------------------------------------------

        #region ITfContextOwner


        //
        //  ITfContextOwner implementation for Cicero's default text store.
        //  
        // These methods may need to return real values.
        public void GetACPFromPoint(ref UnsafeNativeMethods.POINT point, UnsafeNativeMethods.GetPositionFromPointFlags flags, out int position)
        {
            position = 0;
        }

        public void GetTextExt(int start, int end, out UnsafeNativeMethods.RECT rect, out bool clipped)
        {
            rect = new UnsafeNativeMethods.RECT();
            clipped = false;
        }

        public void GetScreenExt(out UnsafeNativeMethods.RECT rect)
        {
            rect = new UnsafeNativeMethods.RECT();
        }

        public void GetStatus(out UnsafeNativeMethods.TS_STATUS status)
        {
            status = new UnsafeNativeMethods.TS_STATUS();
        }

        public void GetWnd(out IntPtr hwnd)
        {
            hwnd = IntPtr.Zero;
        }

        public void GetValue(ref Guid guidAttribute, out object varValue)
        {
            varValue = null;
        }

        #endregion ITfContextOwner


        //------------------------------------------------------
        //
        //  Public Methods - ITfContextOwnerCompositionSink
        //
        //------------------------------------------------------

        #region ITfContextOwnerCompositionSink

        public void OnStartComposition(UnsafeNativeMethods.ITfCompositionView view, out bool ok)
        {
            // Return true in ok to start the composition.
            ok = true;
        }

        public void OnUpdateComposition(UnsafeNativeMethods.ITfCompositionView view, UnsafeNativeMethods.ITfRange rangeNew)
        {
        }

        public void OnEndComposition(UnsafeNativeMethods.ITfCompositionView view)
        {
        }

        #endregion ITfContextOwnerCompositionSink

        //------------------------------------------------------
        //
        //  Public Methods - ITfTransitoryExtensionSink
        //
        //------------------------------------------------------

        #region ITfTransitoryExtensionSink

        // Transitory Document has been updated.
        // This is the notification of the changes of the result string and the composition string.
        public void OnTransitoryExtensionUpdated(UnsafeNativeMethods.ITfContext context, int ecReadOnly, UnsafeNativeMethods.ITfRange rangeResult, UnsafeNativeMethods.ITfRange rangeComposition, out bool fDeleteResultRange)
        {

            fDeleteResultRange = true;

            TextCompositionManager compmgr = InputManager.Current.PrimaryKeyboardDevice.TextCompositionManager;

            if (rangeResult != null)
            {
                string result = StringFromITfRange(rangeResult, ecReadOnly);
                if (result.Length > 0)
                {
                    if (_composition == null)
                    {
                        // We don't have the composition now and we got the result string.
                        // The result text is result and automatic termination is true.
                        _composition = new DefaultTextStoreTextComposition(InputManager.Current, Keyboard.FocusedElement, result, TextCompositionAutoComplete.On);
                        TextCompositionManager.StartComposition(_composition);

                        // relese composition.
                        _composition = null;
                        }
                    else
                    {
                        // Finalize the composition.
                        _composition.SetCompositionText("");
                        _composition.SetText(result);

                        // We don't call _composition.Complete() here. We just want to generate
                        // TextInput events.
                        TextCompositionManager.CompleteComposition(_composition);

                        // relese composition.
                        _composition = null;
                    }
                }
            }

            if (rangeComposition != null)
            {
                string comp = StringFromITfRange(rangeComposition, ecReadOnly);
                if (comp.Length > 0)
                {
                if (_composition == null)
                    {
                        // Start the new composition.
                        _composition = new DefaultTextStoreTextComposition(InputManager.Current, Keyboard.FocusedElement, "", TextCompositionAutoComplete.Off);
                        _composition.SetCompositionText(comp);
                        TextCompositionManager.StartComposition(_composition);
                    }
                    else
                    {
                        // Update the current composition.
                        _composition.SetCompositionText(comp);
                        _composition.SetText("");
                        TextCompositionManager.UpdateComposition(_composition);
                    }
                }
            }
        }

        #endregion ITfTransitoryExtensionSink

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // Return the text services host associated with the current Dispatcher.
        internal static DefaultTextStore Current
        {
            get
            {
                // DefaultTextStore is per Dispatcher and the cached referrence is stored in InputMethod class.
                DefaultTextStore defaulttextstore = InputMethod.Current.DefaultTextStore;

                if(defaulttextstore == null)
                {
                    defaulttextstore = new DefaultTextStore(Dispatcher.CurrentDispatcher);
                    InputMethod.Current.DefaultTextStore = defaulttextstore;

                    defaulttextstore.Register();
                }
                return defaulttextstore;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        // Pointer to ITfDocumentMgr interface.
        internal UnsafeNativeMethods.ITfDocumentMgr DocumentManager
        { 
            get { return _doc.Value;}
            
            set { _doc = new SecurityCriticalData<UnsafeNativeMethods.ITfDocumentMgr>(value); }
        }

        // EditCookie for ITfContext.
        internal int EditCookie
        { 
            // get { return _editCookie; }
            set { _editCookie = value; }
        }

        internal int TransitoryExtensionSinkCookie
        { 
            get { return _transitoryExtensionSinkCookie; }
            set { _transitoryExtensionSinkCookie = value; }
        }

        //
        // Get Transitory's DocumentMgr from GUID_COMPARTMENT_TRANSITORYEXTENSION_DOCUMENTMANAGER.
        //
        internal UnsafeNativeMethods.ITfDocumentMgr TransitoryDocumentManager
        { 
            get
            {

                UnsafeNativeMethods.ITfDocumentMgr doc;
                UnsafeNativeMethods.ITfCompartmentMgr compartmentMgr;
                UnsafeNativeMethods.ITfCompartment compartment;

                // get compartment manager of the parent doc.
                compartmentMgr = (UnsafeNativeMethods.ITfCompartmentMgr)DocumentManager;

                // get compartment.
                Guid guid = UnsafeNativeMethods.GUID_COMPARTMENT_TRANSITORYEXTENSION_DOCUMENTMANAGER;
                compartmentMgr.GetCompartment(ref guid, out compartment);

                // get value of the compartment.
                object obj;
                compartment.GetValue(out obj);
                doc = obj as UnsafeNativeMethods.ITfDocumentMgr;

                Marshal.ReleaseComObject(compartment);
                return doc;
            }
        }


        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // get the text from ITfRange.
        private string StringFromITfRange(UnsafeNativeMethods.ITfRange range, int ecReadOnly)
        {
            // Transitory Document uses ther TextStore, which is ACP base.
            UnsafeNativeMethods.ITfRangeACP rangeacp = (UnsafeNativeMethods.ITfRangeACP)range;
            int start;
            int count;
            int countRet;
            rangeacp.GetExtent(out start, out count);
            char[] text = new char[count];
            rangeacp.GetText(ecReadOnly, 0, text, count, out countRet);
            return new string(text);
        }

        // This function calls TextServicesContext to create TSF document and start transitory extension.
        private void Register()
        {
            // Create TSF document and advise the sink to it.
            TextServicesContext.DispatcherCurrent.RegisterTextStore(this);
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // Dispatcher for this text store
        private readonly Dispatcher _dispatcher;

        // The current active composition.
        private TextComposition _composition;

        // The TSF document object.  This is a native resource.
        private SecurityCriticalData<UnsafeNativeMethods.ITfDocumentMgr> _doc;

        // The edit cookie TSF returns from CreateContext.
        private int _editCookie;

        // The transitory extension sink cookie.
        private int _transitoryExtensionSinkCookie;
    }
}
