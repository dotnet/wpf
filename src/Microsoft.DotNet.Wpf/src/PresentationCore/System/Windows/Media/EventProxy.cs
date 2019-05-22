// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

using System.Windows.Media;
using System;
using MS.Internal;
using MS.Win32;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using MS.Internal.PresentationCore;

namespace System.Windows.Media
{
    #region EventProxyDescriptor
    [StructLayout(LayoutKind.Sequential)]
    internal struct EventProxyDescriptor
    {
        internal delegate void Dispose(
            ref EventProxyDescriptor pEPD
            );

        internal delegate int RaiseEvent(
            ref EventProxyDescriptor pEPD,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buffer,
            uint cb
            );

        internal Dispose pfnDispose;
        internal RaiseEvent pfnRaiseEvent;

        ///<SecurityNote>
        ///     Critical: calls GCHandle.get_Target which LinkDemands
        ///     TreatAsSafe: can't pass in an arbitrary class, only EventProxyDescriptor.  Also, it's OK to dispose this.
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void StaticDispose(ref EventProxyDescriptor pEPD)
        {
            Debug.Assert(((IntPtr)pEPD.m_handle) != IntPtr.Zero, "If this asserts fires: Why is it firing? It might be legal in future.");
            EventProxyWrapper epw = (EventProxyWrapper)(pEPD.m_handle.Target);
            ((System.Runtime.InteropServices.GCHandle)(pEPD.m_handle)).Free();
        }

        internal System.Runtime.InteropServices.GCHandle m_handle;
    }
    #endregion

    #region EventProxyStaticPtrs
    /// <summary>
    /// We need to keep the delegates alive.
    /// </summary>
    internal static class EventProxyStaticPtrs
    {
        static EventProxyStaticPtrs()
        {
            EventProxyStaticPtrs.pfnDispose = new EventProxyDescriptor.Dispose(EventProxyDescriptor.StaticDispose);
            EventProxyStaticPtrs.pfnRaiseEvent = new EventProxyDescriptor.RaiseEvent(EventProxyWrapper.RaiseEvent);
}

        internal static EventProxyDescriptor.Dispose pfnDispose;
        internal static EventProxyDescriptor.RaiseEvent pfnRaiseEvent;
    }
    #endregion

    #region EventProxyWrapper
    /// <summary>
    /// Event proxy wrapper will relay events from unmanaged code to managed code
    /// </summary>
    internal class EventProxyWrapper
    {
        private WeakReference target;

        #region Constructor

        private EventProxyWrapper(IInvokable invokable)
        {
            target = new WeakReference(invokable);
        }

        #endregion

        #region Verify

        private void Verify()
        {
            if (target == null)
            {
                throw new System.ObjectDisposedException("EventProxyWrapper");
            }
        }

        #endregion

        #region Public methods

        ///<SecurityNote>
        ///     Critical: calls Marshal.GetHRForException which LinkDemands
        ///     TreatAsSafe: ok to return an hresult in partial trust
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe] 
        public int RaiseEvent(byte[] buffer, uint cb)
        {
#pragma warning disable 6500
            try
            {
                Verify();
                IInvokable invokable = (IInvokable)target.Target;
                if (invokable != null)
                {
                    invokable.RaiseEvent(buffer, (int)cb);
                }
                else
                {
                    // return E_HANDLE to notify that object is no longer alive

                    return NativeMethods.E_HANDLE;
                }
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
#pragma warning restore 6500

            return NativeMethods.S_OK;
        }

        #endregion

        #region Delegate Implemetations
        /// <SecurityNote>
        ///     Critical: This code calls into handle to get Target which has a link demand
        ///     TreatAsSafe: EventProxyWrapper is safe to expose
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal static EventProxyWrapper FromEPD(ref EventProxyDescriptor epd)
        {
            Debug.Assert(((IntPtr)epd.m_handle) != IntPtr.Zero, "Stream is disposed.");
            System.Runtime.InteropServices.GCHandle handle = (System.Runtime.InteropServices.GCHandle)(epd.m_handle);
            return (EventProxyWrapper)(handle.Target);
        }

        internal static int RaiseEvent(ref EventProxyDescriptor pEPD, byte[] buffer, uint cb)
        {
            EventProxyWrapper target = EventProxyWrapper.FromEPD(ref pEPD);
            if (target != null)
            {
                return target.RaiseEvent(buffer, cb);
            }
            else
            {
                return NativeMethods.E_HANDLE;
            }
        }

        #endregion

        #region Static Create Method(s)

        /// <SecurityNote>
        ///     Critical: This code hooks up event sinks for unmanaged events
        ///               It also calls into a native method via MILCreateEventProxy
        /// </SecurityNote>
        [SecurityCritical]
        internal static SafeMILHandle CreateEventProxyWrapper(IInvokable invokable)
        {
            if (invokable == null)
            {
                throw new System.ArgumentNullException("invokable");
            }

            SafeMILHandle eventProxy = null;

            EventProxyWrapper epw = new EventProxyWrapper(invokable);
            EventProxyDescriptor epd = new EventProxyDescriptor();

            epd.pfnDispose = EventProxyStaticPtrs.pfnDispose;
            epd.pfnRaiseEvent = EventProxyStaticPtrs.pfnRaiseEvent;

            epd.m_handle = System.Runtime.InteropServices.GCHandle.Alloc(epw, System.Runtime.InteropServices.GCHandleType.Normal);

            HRESULT.Check(MILCreateEventProxy(ref epd, out eventProxy));

            return eventProxy;
        }

        #endregion

        /// <SecurityNote>
        ///     Critical: Elevates to unmanaged code permission
        /// </SecurityNote>
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        [DllImport(DllImport.MilCore)]
        private extern static int /* HRESULT */ MILCreateEventProxy(ref EventProxyDescriptor pEPD, out SafeMILHandle ppEventProxy);
    }
    #endregion
}
