// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Creates ITfInputProcessorProfiles instances.
//

using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;
using MS.Win32;

namespace System.Windows.Input
{
    /// <summary>
    /// The <see cref="InputProcessorProfiles"/> class is always associated with hwndInputLanguage class.
    /// </summary>
    internal sealed class InputProcessorProfiles
    {
        /// <summary>
        /// InputProcessorProfiles Constructor;
        /// </summary>
        internal InputProcessorProfiles()
        {
            // _ipp is a ValueType, hence no need for new.
            _ipp = null;
            _cookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
        }

        /// <summary>
        /// Initialize an interface and notify sink.
        /// </summary>
        internal bool Initialize(object o)
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Initialize called on MTA thread!");

            Debug.Assert(_ipp == null, "Initialize called twice");

            _ipp = InputProcessorProfilesLoader.Load();

            if (_ipp == null)
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
            Debug.Assert(_ipp != null, "Uninitialize called without initializing");

            UnadviseNotifySink();
            Marshal.ReleaseComObject(_ipp);
            _ipp = null;
        }

        /// <summary>
        /// Get the current input language of the current thread.
        /// </summary>
        internal short CurrentInputLanguage
        {
            set
            {
                if (_ipp != null)
                {
                    if (_ipp.ChangeCurrentLanguage(value) != 0)
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
                                    SafeNativeMethods.ActivateKeyboardLayout(new HandleRef(this, hklList[i]), 0);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the list of the input languages that are available in the current thread.
        /// </summary>
        internal unsafe CultureInfo[] InputLanguageList
        {
            get
            {
                // ITfInputProcessorProfiles::GetLanguageList returns the pointer that was allocated by CoTaskMemAlloc().
                _ipp.GetLanguageList(out nint ptrLanguageIDs, out int nCount);

                ReadOnlySpan<short> languageIDs = new((void*)ptrLanguageIDs, nCount);
                CultureInfo[] langArray = new CultureInfo[nCount];

                // Create CultureInfo from each ID and store it
                for (int i = 0; i < langArray.Length; i++)
                    langArray[i] = new CultureInfo(languageIDs[i]);

                // Call CoTaskMemFree().
                Marshal.FreeCoTaskMem(ptrLanguageIDs);

                return langArray;
            }
        }

        /// <summary>
        /// This advices the input language notify sink to
        /// ITfInputProcessorProfile.
        /// </summary>
        private void AdviseNotifySink(object o)
        {
            Debug.Assert(_cookie == UnsafeNativeMethods.TF_INVALID_COOKIE, "Cookie is already set.");

            UnsafeNativeMethods.ITfSource source = _ipp as UnsafeNativeMethods.ITfSource;

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

            UnsafeNativeMethods.ITfSource source = _ipp as UnsafeNativeMethods.ITfSource;

            source.UnadviseSink(_cookie);

            _cookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
        }

        /// <summary>
        /// The reference to <see cref="UnsafeNativeMethods.ITfInputProcessorProfiles"/>.
        /// </summary>
        private UnsafeNativeMethods.ITfInputProcessorProfiles _ipp;

        /// <summary>
        /// The cookie for the advised sink.
        /// </summary>
        private int _cookie;
    }
}
