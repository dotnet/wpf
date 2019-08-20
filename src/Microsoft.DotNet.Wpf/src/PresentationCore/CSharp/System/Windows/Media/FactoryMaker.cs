// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Diagnostics;
using MS.Internal;
using MS.Win32;
using System.Windows.Media.Composition;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    /// <summary>
    /// This is a internal class which is used by non context affinity objects
    /// to get access to a MIL factory object.
    /// </summary>
    internal class FactoryMaker: IDisposable
    {
        private bool _disposed = false;
        internal FactoryMaker()
        {
            lock (s_factoryMakerLock)
            {
                // If we haven't have a factory, create one

                if (s_pFactory == IntPtr.Zero)
                {
                    // Create the Core MIL factory.
                    // Note: the call below might throw exception. The caller
                    // should catch it. We won't add ref counter here if this
                    // happens.

                    HRESULT.Check(UnsafeNativeMethods.MILFactory2.CreateFactory(out s_pFactory, MS.Internal.Composition.Version.MilSdkVersion));
                }

                s_cInstance++;
                _fValidObject = true;
            }
        }

        ~FactoryMaker()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose of any resources
        /// </summary>
        public void Dispose()
        {
                Dispose(true);
        }

        protected virtual void Dispose(bool fDisposing)
        {
                if (!_disposed)
                {
                    if (_fValidObject == true)
                    {
                        lock (s_factoryMakerLock)
                        {
                            s_cInstance--;

                            // Make sure we don't dispose twice
                            _fValidObject = false;

                            // If there is no FactoryMaker object out there, release
                            // factory object

                            if (s_cInstance == 0)
                            {
                                UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref s_pFactory);

                                if (s_pImagingFactory != IntPtr.Zero)
                                {
                                    UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref s_pImagingFactory);
                                }

                                s_pFactory = IntPtr.Zero;
                                s_pImagingFactory = IntPtr.Zero;
                            }
                        }
                    }

                                
                // Set the sentinel.
                _disposed = true;
   
                // Suppress finalization of this disposed instance.
                if (fDisposing)
                {
                    GC.SuppressFinalize(this);
                }
                }
        }

        internal IntPtr FactoryPtr
        {
            get
            {
                Debug.Assert(s_pFactory != IntPtr.Zero);
                return s_pFactory;
            }
        }

        internal IntPtr ImagingFactoryPtr
        {
            get
            {
                if (s_pImagingFactory == IntPtr.Zero)
                {
                    lock (s_factoryMakerLock)
                    {
                        HRESULT.Check(UnsafeNativeMethods.WICCodec.CreateImagingFactory(UnsafeNativeMethods.WICCodec.WINCODEC_SDK_VERSION, out s_pImagingFactory));
                    }
                }
                Debug.Assert(s_pImagingFactory != IntPtr.Zero);
                return s_pImagingFactory;
            }
        }

        private static IntPtr s_pFactory;
        private static IntPtr s_pImagingFactory;

        /// <summary>
        /// Keeps track of how many instance of current object have been passed out
        /// </summary>
        private static int s_cInstance = 0;

        /// <summary>
        /// "FactoryMaker" is free threaded. This lock is used to synchronize
        /// access to the FactoryMaker.
        /// </summary>
        private static object s_factoryMakerLock = new object();
        private bool _fValidObject;
    }
}

