// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Creates ITfInputProcessorProfiles instances.
//
//

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security;
using MS.Win32;
using MS.Internal;
using System.Diagnostics;
using System.Globalization;
using System.Collections;

namespace System.Windows.Input
{
    //------------------------------------------------------
    //
    //  InputProcessorProfiles class
    //
    //------------------------------------------------------

    /// <summary>
    /// The InputProcessorProfiles class is always associated with 
    /// hwndInputLanguage class.
    /// </summary>
    internal class InputProcessorProfiles
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// InputProcessorProfiles Constructor;
        /// </summary>
        /// Critical - as this sets the value for _ipp.
        /// Safe - as this just initializes it to null.
        internal InputProcessorProfiles()
        {
            // _ipp is a ValueType, hence no need for new.
            _ipp.Value = null;
            _cookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods
 
        /// <summary>
        /// Initialize an interface and notify sink.
        /// </summary>
        internal bool Initialize(object o)
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Initialize called on MTA thread!");

            Debug.Assert(_ipp.Value == null, "Initialize called twice");

            _ipp.Value = InputProcessorProfilesLoader.Load();

            if (_ipp.Value == null)
            {
                return false;
            }

            AdviseNotifySink(o);
            return true;
        }

        /// <summary>
        /// Initialize an interface and notify sink.
        /// </summary>
        internal void Uninitialize()
        {
            Debug.Assert(_ipp.Value != null, "Uninitialize called without initializing");

            UnadviseNotifySink();            
            Marshal.ReleaseComObject(_ipp.Value);
            _ipp.Value = null;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Get the current input language of the current thread.
        /// </summary>
        internal short CurrentInputLanguage
        {
            set
            {
                if (_ipp.Value != null)
                {
                    if (_ipp.Value.ChangeCurrentLanguage(value) != 0)
                    {
                        //
                        // Under WinXP or W2K3, ITfInputProcessorProfiles::ChangeCurrentLanguage() fails
                        // if there is no thread manager in the current thread. This is fixed on 
                        // Windows Vista+
                        IntPtr[] hklList = null;

                        int count = (int)SafeNativeMethods.GetKeyboardLayoutList(0, null);
                        if (count > 1) 
                        {
                            hklList = new IntPtr[count];

                            count = SafeNativeMethods.GetKeyboardLayoutList(count, hklList);

                            int i;
                            for (i = 0; (i < hklList.Length) && (i < count); i++)
                            {
                                if (value == (short)hklList[i])
                                {
                                    SafeNativeMethods.ActivateKeyboardLayout(new HandleRef(this,hklList[i]), 0);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the list of the input languages that are available in the
        /// current thread.
        /// </summary>
        internal ArrayList InputLanguageList
        {
             get
             {
                 int nCount;
                 IntPtr langids;

                 // ITfInputProcessorProfiles::GetLanguageList returns the pointer that was allocated by
                 // CoTaskMemAlloc().
                 _ipp.Value.GetLanguageList(out langids, out nCount);

                 ArrayList arrayLang = new ArrayList();

                 int sizeOfShort = Marshal.SizeOf(typeof(short));

                 for (int i = 0; i < nCount; i++)
                 {
                     // Unmarshal each langid from short array.
                     short langid = (short)Marshal.PtrToStructure((IntPtr)((Int64)langids + sizeOfShort * i), typeof(short));
                     arrayLang.Add(new CultureInfo(langid));
                 }

                 // Call CoTaskMemFree().
                 Marshal.FreeCoTaskMem(langids);

                 return arrayLang;
             }
        }


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        
        /// <summary>
        /// This advices the input language notify sink to
        /// ITfInputProcessorProfile.
        /// </summary>
        private void AdviseNotifySink(object o)
        {
            Debug.Assert(_cookie == UnsafeNativeMethods.TF_INVALID_COOKIE, "Cookie is already set.");

            UnsafeNativeMethods.ITfSource source = _ipp.Value as UnsafeNativeMethods.ITfSource;

            // workaround because I can't pass a ref to a readonly constant
            Guid guid = UnsafeNativeMethods.IID_ITfLanguageProfileNotifySink;

            source.AdviseSink(ref guid, o, out _cookie);
        }

        /// <summary>
        /// This unadvises the sink.
        /// </summary>
        private void UnadviseNotifySink()
        {
            Debug.Assert(_cookie != UnsafeNativeMethods.TF_INVALID_COOKIE, "Cookie is not set.");

            UnsafeNativeMethods.ITfSource source = _ipp.Value as UnsafeNativeMethods.ITfSource;

            source.UnadviseSink(_cookie);

            _cookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        // The reference to ITfInputProcessorProfile.
        private SecurityCriticalDataForSet<UnsafeNativeMethods.ITfInputProcessorProfiles> _ipp;

        // The cookie for the advised sink.
        private int _cookie;
    }
}
