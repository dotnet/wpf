// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The source of the input language of the thread.
//
//

using System.Security;
using System.Collections;
using System.Globalization;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Diagnostics;

using MS.Win32;
using MS.Utility;

namespace System.Windows.Input
{
    //------------------------------------------------------
    //
    //  InputLanguageSource class
    //
    //------------------------------------------------------
 
    /// <summary>
    /// This is an internal. The source for input languages.
    /// </summary>
    internal sealed class InputLanguageSource : IInputLanguageSource, IDisposable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        /// <summary>
        ///    This is an internal. The source for input languages.
        /// </summary>
        internal InputLanguageSource(InputLanguageManager inputlanguagemanager)
        {
            _inputlanguagemanager = inputlanguagemanager;

            // initialize the current input language.
            _langid = (short)NativeMethods.IntPtrToInt32(SafeNativeMethods.GetKeyboardLayout(0));

            // store the dispatcher thread id. This will be used to call GetKeyboardLayout() from
            // other thread.
            _dispatcherThreadId = SafeNativeMethods.GetCurrentThreadId();

            // Register source
            _inputlanguagemanager.RegisterInputLanguageSource(this);
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        ///    Dispose method.
        /// </summary>
        public void Dispose()
        {
             if (_ipp != null)
                 Uninitialize();
        }

        /// <summary>
        ///    IIInputLanguageSource.Initialize()
        ///    This creates ITfInputProcessorProfile object and advice sink.
        /// </summary>
        public void Initialize()
        {
            EnsureInputProcessorProfile();
        }

        /// <summary>
        ///    IIInputLanguageSource.Uninitialize()
        ///    This releases ITfInputProcessorProfile object and unadvice sink.
        /// </summary>
        public void Uninitialize()
        {
            if (_ipp != null)
            {
                _ipp.Uninitialize();
                _ipp = null;
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        /// <summary>
        ///    returns the current input language of this win32 thread.
        /// </summary>
        public CultureInfo CurrentInputLanguage
        {
            get
            {
                return new CultureInfo(_CurrentInputLanguage);
            }
            set
            {
                _CurrentInputLanguage = (short)value.LCID;
            }
        }

        /// <summary>
        ///    returns the list of the available input languages of this win32 thread.
        /// </summary>
        public IEnumerable InputLanguageList
        {
             get
             {
                EnsureInputProcessorProfile();

                if (_ipp == null)
                {
                    ArrayList al = new ArrayList();
                    al.Add(CurrentInputLanguage);
                    return al;
                }
                return _ipp.InputLanguageList;
             }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        /// <summary>
        ///    The input language change call back from the sink.
        /// </summary>
        internal bool OnLanguageChange(short langid)
        {
            if (_langid != langid)
            {
                // Call InputLanguageManager if its current source is this.
                if (InputLanguageManager.Current.Source == this)
                {
                    return InputLanguageManager.Current.ReportInputLanguageChanging(new CultureInfo(langid), new CultureInfo(_langid));
                }
            }
                
            return true;
        }

        /// <summary>
        ///    The input language changed call back from the sink.
        /// </summary>
        internal void OnLanguageChanged()
        {
            short langid = _CurrentInputLanguage;
            if (_langid != langid)
            {
                short prevlangid = _langid;
                _langid = langid;

                // Call InputLanguageManager if its current source is this.
                if (InputLanguageManager.Current.Source == this)
                {
                    InputLanguageManager.Current.ReportInputLanguageChanged(new CultureInfo(langid), new CultureInfo(prevlangid));
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Method
        //
        //------------------------------------------------------

        /// <summary>
        ///    This creates ITfInputProcessorProfile object and advice sink.
        /// </summary>
        private void EnsureInputProcessorProfile()
        {
            // _ipp has been initialzied. Don't do this again.
            if (_ipp != null)
                return;

            // We don't need to initialize _ipp if there is onlyone keyboard layout.
            // Only one input language is available.
            if (SafeNativeMethods.GetKeyboardLayoutList(0, null) <= 1)
                return;

            Debug.Assert(_ipp == null, "_EnsureInputProcesoorProfile has been called.");

            InputLanguageProfileNotifySink lpns;
            lpns = new InputLanguageProfileNotifySink(this);
            _ipp= new InputProcessorProfiles();

            if (!_ipp.Initialize(lpns))
            {
                _ipp = null;
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
                
        /// <summary>
        ///    The current input language in LANGID of this win32 thread.
        /// </summary>
        private short _CurrentInputLanguage
        {
            get
            {
                // Return input language of the dispatcher thread.
                return (short)NativeMethods.IntPtrToInt32(SafeNativeMethods.GetKeyboardLayout(_dispatcherThreadId));
            }
            set
            {
                EnsureInputProcessorProfile();

                if (_ipp != null)
                {
                    _ipp.CurrentInputLanguage = value;
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        // the current input language in LANGID.
        private short _langid;

        // The dispatcher thread id.
        private int _dispatcherThreadId;

        // the connected input language manager.
        InputLanguageManager _inputlanguagemanager;

        // the reference to ITfInputProcessorProfile.
        InputProcessorProfiles _ipp;

        #endregion Private Fields
    }
}

