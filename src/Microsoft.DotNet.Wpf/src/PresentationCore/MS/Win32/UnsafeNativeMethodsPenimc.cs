// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define OLD_ISF

using System;
using System.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Interop;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;

namespace MS.Win32.Penimc
{
    internal static class UnsafeNativeMethods
    {

        // The flags in this region are all in support of COM hardening to add resilience
        // to (OSGVSO:10779198).
        // They are special arguments to COM calls that allow us to re-purpose them for
        // functions relating to this hardening.
        #region PenIMC Operations Flags

        /// <summary>
        /// Instruct IPimcManager3.GetTablet to release the external lock on itself.
        /// </summary>
        private const UInt32 ReleaseManagerExt = 0xFFFFDEAD;

        /// <summary>
        /// Instruct IPimcTablet3.GetCursorButtonCount to release the external lock on itself.
        /// </summary>
        private const int ReleaseTabletExt = -1;

        /// <summary>
        /// Instruct IPimcTablet3.GetCursorButtonCount to return the GIT key for the WISP Tablet.
        /// </summary>
        private const int GetWispTabletKey = -2;

        /// <summary>
        /// Instruct IPimcTablet3.GetCursorButtonCount to return the GIT key for the WISP Tablet Manager.
        /// </summary>
        private const int GetWispManagerKey = -3;

        /// <summary>
        /// Instruct IPimcTablet3.GetCursorButtonCount to acquire the external lock on itself.
        /// </summary>
        private const int LockTabletExt = -4;

        /// <summary>
        /// Instruct IPimcContext3.GetPacketPropertyInfo to return the GIT key for the WISP Tablet Context.
        /// </summary>
        private const int GetWispContextKey = -1;

        #endregion

        #region Stylus Input Thread Manager

        /// <summary>
        /// The GIT key to use when managing the WISP Tablet Manager objects
        /// </summary>
        [ThreadStatic]
        private static UInt32? _wispManagerKey;

        /// <summary>
        /// Whether or not the WISP Tablet Manager server object has been locked in the MTA.
        /// </summary>
        [ThreadStatic]
        private static bool _wispManagerLocked = false;

        [ThreadStatic]
        private static IPimcManager3 _pimcManagerThreadStatic;

        /// <summary>
        /// The cookie for the PenIMC activation context.
        /// </summary>
        [ThreadStatic]
        private static IntPtr _pimcActCtxCookie = IntPtr.Zero;

        #endregion

        #region PenIMC

        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern IntPtr RegisterDllForSxSCOM();

        #endregion

        /// <summary>
        /// Make sure we load penimc.dll from WPF's installed location to avoid two instances of it.
        ///
        /// Add an activation context to the thread's stack to ensure the registration-free COM objects
        /// are available.
        /// </summary>
        /// <remarks>
        /// PenIMC COM objects are only directly used (functions called on their interfaces) from inside the
        /// PenThread.  As such, the PenThreads need to create the activation context.  The various Dispatcher
        /// threads need not do so as they merely pass the RCWs around in manged objects and will only use
        /// them via operations queued on their associated PenThread.
        /// </remarks>
        internal static void EnsurePenImcClassesActivated()
        {
            if (_pimcActCtxCookie == IntPtr.Zero)
            {
                // Register PenIMC for SxS COM for the lifetime of the thread.
                //
                // RegisterDllForSxSCOM returns a non-zero ActivationContextCookie if SxS registration
                // succeeds, or IntPtr.Zero if SxS registration fails.
                if ((_pimcActCtxCookie = RegisterDllForSxSCOM()) == IntPtr.Zero)
                {
                    throw new InvalidOperationException(SR.Get(SRID.PenImcSxSRegistrationFailed, ExternDll.Penimc));
                }
            }
        }

        /// <summary>
        /// Deactivates the activation context for PenIMC objects.
        /// </summary>
        internal static void DeactivatePenImcClasses()
        {
            if (_pimcActCtxCookie != IntPtr.Zero)
            {
                if(DeactivateActCtx(0, _pimcActCtxCookie))
                {
                    _pimcActCtxCookie = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Returns IPimcManager3 interface.  Creates this object the first time per thread.
        /// </summary>
        internal static IPimcManager3 PimcManager
        {
            get
            {
                if (_pimcManagerThreadStatic == null)
                {
                    _pimcManagerThreadStatic = CreatePimcManager();
                }
                return _pimcManagerThreadStatic;
            }
        }

        /// <summary>
        /// Creates a new instance of PimcManager.
        /// </summary>
        private static IPimcManager3 CreatePimcManager()
        {
            // Instantiating PimcManager using "new PimcManager()" results
            // in calling CoCreateInstanceForApp from an immersive process
            // (like designer). Such a call would fail because PimcManager is not
            // in white list for that call. Hence we call CoCreateInstance directly.
            // Note: Normally WPF is not supported for immersive processes
            // but designer is an exception.
            Guid clsid = Guid.Parse(PimcConstants.PimcManager3CLSID);
            Guid iid = Guid.Parse(PimcConstants.IPimcManager3IID);
            object pimcManagerObj = CoCreateInstance(ref clsid,
                                                     null,
                                                     0x1, /*CLSCTX_INPROC_SERVER*/
                                                     ref iid);
            return ((IPimcManager3)pimcManagerObj);
        }

        #region COM Locking/Unlocking Functions

        #region General

        /// <summary>
        /// Calls WISP GIT lock functions on Win8+.
        /// On Win7 these will always fail since WISP objects are always proxies (WISP is out of proc).
        /// </summary>
        /// <param name="gitKey">The GIT key for the object to lock.</param>
        internal static void CheckedLockWispObjectFromGit(UInt32 gitKey)
        {
            if (OSVersionHelper.IsOsWindows8OrGreater)
            {
                if (!LockWispObjectFromGit(gitKey))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Calls WISP GIT unlock functions on Win8+.
        /// On Win7 these will always fail since WISP objects are always proxies (WISP is out of proc).
        /// </summary>
        /// <param name="gitKey">The GIT key for the object to unlock.</param>
        internal static void CheckedUnlockWispObjectFromGit(UInt32 gitKey)
        {
            if (OSVersionHelper.IsOsWindows8OrGreater)
            {
                if (!UnlockWispObjectFromGit(gitKey))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion

        #region Manager

        /// <summary>
        ///
        /// Calls into GetTablet with a special flag that indicates we should release
        /// the lock obtained previously by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The manager to release the lock for.</param>'
        private static void ReleaseManagerExternalLockImpl(IPimcManager3 manager)
        {
            IPimcTablet3 unused = null;
            manager.GetTablet(ReleaseManagerExt, out unused);
        }

        /// <summary>
        /// Calls into GetTablet with a special flag that indicates we should release
        /// the lock obtained previously by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The manager to release the lock for.</param>'
        internal static void ReleaseManagerExternalLock()
        {
            if (_pimcManagerThreadStatic != null)
            {
                ReleaseManagerExternalLockImpl(_pimcManagerThreadStatic);
            }
        }

        /// <summary>
        /// Queries and sets the GIT key for the WISP Tablet Manager
        /// </summary>
        /// <param name="tablet">The tablet to call through</param>
        internal static void SetWispManagerKey(IPimcTablet3 tablet)
        {
            UInt32 latestKey = QueryWispKeyFromTablet(GetWispManagerKey, tablet);

            // Assert here to ensure that every call through to this specific manager has the same
            // key.  This should be guaranteed since these calls are always done on the thread the tablet
            // is created on and all tablets created on a particular thread should be through the same
            // manager.
            Invariant.Assert(!_wispManagerKey.HasValue || _wispManagerKey.Value == latestKey);

            _wispManagerKey = latestKey;
        }

        /// <summary>
        /// Calls down into PenIMC in order to lock the WISP Tablet Manager.
        /// </summary>
        internal static void LockWispManager()
        {
            if (!_wispManagerLocked && _wispManagerKey.HasValue)
            {
                CheckedLockWispObjectFromGit(_wispManagerKey.Value);
                _wispManagerLocked = true;
            }
        }

        /// <summary>
        /// Calls down into PenIMC in order to unlock the WISP Tablet Manager.
        /// </summary>
        internal static void UnlockWispManager()
        {
            if (_wispManagerLocked && _wispManagerKey.HasValue)
            {
                CheckedUnlockWispObjectFromGit(_wispManagerKey.Value);
                _wispManagerLocked = false;
            }
        }

        #endregion

        #region Tablet

        /// <summary>
        /// Calls into GetCursorButtonCount with a special flag that indicates we should acquire
        /// the external lock by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The tablet to acquire the lock for.</param>'
        internal static void AcquireTabletExternalLock(IPimcTablet3 tablet)
        {
            int unused = 0;

            // Call through with special param to release the external lock on the tablet.
            tablet.GetCursorButtonCount(LockTabletExt, out unused);
        }

        /// <summary>
        /// Calls into GetCursorButtonCount with a special flag that indicates we should release
        /// the lock obtained previously by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The tablet to release the lock for.</param>'
        internal static void ReleaseTabletExternalLock(IPimcTablet3 tablet)
        {
            int unused = 0;

            // Call through with special param to release the external lock on the tablet.
            tablet.GetCursorButtonCount(ReleaseTabletExt, out unused);
        }

        /// <summary>
        /// Queries the GIT key from the PenIMC Tablet
        /// </summary>
        /// <param name="keyType">The kind of key to instruct the tablet to return</param>
        /// <param name="tablet">The tablet to call through</param>
        /// <returns>The GIT key for the requested operation</returns>
        private static UInt32 QueryWispKeyFromTablet(int keyType, IPimcTablet3 tablet)
        {
            int key = 0;

            tablet.GetCursorButtonCount(keyType, out key);

            if(key == 0)
            {
                throw new InvalidOperationException();
            }

            return (UInt32)key;
        }

        /// <summary>
        /// Queries the GIT key for the WISP Tablet
        /// </summary>
        /// <param name="tablet">The tablet to call through</param>
        /// <returns>The GIT key for the WISP Tablet</returns>
        internal static UInt32 QueryWispTabletKey(IPimcTablet3 tablet)
        {
            return QueryWispKeyFromTablet(GetWispTabletKey, tablet);
        }

        #endregion

        #region Context

        /// <summary>
        /// Queries the GIT key for the WISP Tablet Context
        /// </summary>
        /// <param name="context">The context to query through</param>
        /// <returns>The GIT key for the WISP Tablet Context</returns>
        internal static UInt32 QueryWispContextKey(IPimcContext3 context)
        {
            int key = 0;
            Guid unused = Guid.Empty;
            int unused2 = 0;
            int unused3 = 0;
            float unused4 = 0;

            context.GetPacketPropertyInfo(GetWispContextKey, out unused, out key, out unused2, out unused3, out unused4);

            if (key == 0)
            {
                throw new InvalidOperationException();
            }

            return (UInt32)key;
        }

        #endregion

        #endregion

#if OLD_ISF
        /// <summary>
        /// Managed wrapper for IsfCompressPropertyData
        /// </summary>
        /// <param name="pbInput">Input byte array</param>
        /// <param name="cbInput">number of bytes in byte array</param>
        /// <param name="pnAlgoByte">
        /// In: Preferred algorithm Id
        /// Out: Best algorithm with parameters
        /// </param>
        /// <param name="pcbOutput">
        /// In: output buffer size (of pbOutput)
        /// Out: Actual number of bytes needed for compressed data
        /// </param>
        /// <param name="pbOutput">Buffer to hold the output</param>
        /// <returns>Status</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfCompressPropertyData(
                [In] byte [] pbInput,
                uint cbInput,
                ref byte pnAlgoByte,
                ref uint pcbOutput,
                [In, Out] byte [] pbOutput
            );

        /// <summary>
        /// Managed wrapper for IsfDecompressPropertyData
        /// </summary>
        /// <param name="pbCompressed">Input buffer to be decompressed</param>
        /// <param name="cbCompressed">Number of bytes in the input buffer to be decompressed</param>
        /// <param name="pcbOutput">
        /// In: Output buffer capacity
        /// Out: Actual number of bytes required to hold uncompressed bytes
        /// </param>
        /// <param name="pbOutput">Output buffer</param>
        /// <param name="pnAlgoByte">Algorithm id and parameters</param>
        /// <returns>Status</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfDecompressPropertyData(
                [In] byte [] pbCompressed,
                uint cbCompressed,
                ref uint pcbOutput,
                [In, Out] byte [] pbOutput,
                ref byte pnAlgoByte
            );

        /// <summary>
        /// Managed wrapper for IsfCompressPacketData
        /// </summary>
        /// <param name="hCompress">Handle to the compression engine (null is ok)</param>
        /// <param name="pbInput">Input buffer</param>
        /// <param name="cInCount">Number of bytes in the input buffer</param>
        /// <param name="pnAlgoByte">
        /// In: Preferred compression algorithm byte
        /// Out: Actual compression algorithm byte
        /// </param>
        /// <param name="pcbOutput">
        /// In: Output buffer capacity
        /// Out: Actual number of bytes required to hold compressed bytes
        /// </param>
        /// <param name="pbOutput">Output buffer</param>
        /// <returns>Status</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfCompressPacketData(
                CompressorSafeHandle hCompress,
                [In] int [] pbInput,
                [In] uint cInCount,
                ref byte pnAlgoByte,
                ref uint pcbOutput,
                [In, Out] byte [] pbOutput
            );

        /// <summary>
        /// Managed wrapper for IsfDecompressPacketData
        /// </summary>
        /// <param name="hCompress">Handle to the compression engine (null is ok)</param>
        /// <param name="pbCompressed">Input buffer of compressed bytes</param>
        /// <param name="pcbCompressed">
        /// In: Size of the input buffer
        /// Out: Actual number of compressed bytes decompressed.
        /// </param>
        /// <param name="cInCount">Count of int's in the compressed buffer</param>
        /// <param name="pbOutput">Output buffer to receive the decompressed int's</param>
        /// <param name="pnAlgoData">Algorithm bytes</param>
        /// <returns>Status</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfDecompressPacketData(
                CompressorSafeHandle hCompress,
                [In] byte [] pbCompressed,
                ref uint pcbCompressed,
                uint cInCount,
                [In, Out] int [] pbOutput,
                ref byte pnAlgoData
            );

        /// <summary>
        /// Managed wrapper for IsfLoadCompressor
        /// </summary>
        /// <param name="pbInput">Input buffer where compressor is saved</param>
        /// <param name="pcbInput">
        /// In: Size of the input buffer
        /// Out: Number of bytes in the input buffer decompressed to construct compressor
        /// </param>
        /// <returns>Handle to the compression engine loaded</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern CompressorSafeHandle IsfLoadCompressor(
                [In] byte [] pbInput,
                ref uint pcbInput
            );

        /// <summary>
        /// Managed wrapper for IsfReleaseCompressor
        /// </summary>
        /// <param name="hCompress">Handle to the Compression Engine to be released</param>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern void IsfReleaseCompressor(
                IntPtr hCompress
            );
#endif

        /// <summary>
        /// Managed wrapper for GetPenEvent
        /// </summary>
        /// <param name="commHandle">Win32 event handle to wait on for new stylus input.</param>
        /// <param name="handleReset">Win32 event the signals a reset.</param>
        /// <param name="evt">Stylus event that was triggered.</param>
        /// <param name="stylusPointerId">Stylus Device ID that triggered input.</param>
        /// <param name="cPackets">Count of packets returned.</param>
        /// <param name="cbPacket">Byte count of packet data returned.</param>
        /// <param name="pPackets">Array of ints containing the packet data.</param>
        /// <returns>true if succeeded, false if failed or shutting down.</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPenEvent(
            IntPtr      commHandle,
            IntPtr      handleReset,
            out int     evt,
            out int     stylusPointerId,
            out int     cPackets,
            out int     cbPacket,
            out IntPtr  pPackets);

        /// <summary>
        /// Managed wrapper for GetPenEventMultiple
        /// </summary>
        /// <param name="cCommHandles">Count of elements in commHandles.</param>
        /// <param name="commHandles">Array of Win32 event handles to wait on for new stylus input.</param>
        /// <param name="handleReset">Win32 event the signals a reset.</param>
        /// <param name="iHandle">Index to the handle that triggered return.</param>
        /// <param name="evt">Stylus event that was triggered.</param>
        /// <param name="stylusPointerId">Stylus Device ID that triggered input.</param>
        /// <param name="cPackets">Count of packets returned.</param>
        /// <param name="cbPacket">Byte count of packet data returned.</param>
        /// <param name="pPackets">Array of ints containing the packet data.</param>
        /// <returns>true if succeeded, false if failed or shutting down.</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPenEventMultiple(
            int         cCommHandles,
            IntPtr[]    commHandles,
            IntPtr      handleReset,
            out int     iHandle,
            out int     evt,
            out int     stylusPointerId,
            out int     cPackets,
            out int     cbPacket,
            out IntPtr  pPackets);

        /// <summary>
        /// Managed wrapper for GetLastSystemEventData
        /// </summary>
        /// <param name="commHandle">Specifies PimcContext object handle to get event data on.</param>
        /// <param name="evt">ID of system event that was triggered.</param>
        /// <param name="modifier">keyboar modifier (unused).</param>
        /// <param name="key">Keyboard key (unused).</param>
        /// <param name="x">X position in device units of gesture.</param>
        /// <param name="y">Y position in device units of gesture.</param>
        /// <param name="cursorMode">Mode of the cursor.</param>
        /// <param name="buttonState">State of stylus buttons (flick returns custom data in this).</param>
        /// <returns>true if succeeded, false if failed.</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetLastSystemEventData(
            IntPtr      commHandle,
            out int     evt,
            out int     modifier,
            out int     key,
            out int     x,
            out int     y,
            out int     cursorMode,
            out int     buttonState);

        /// <summary>
        /// Managed wrapper for CreateResetEvent
        /// </summary>
        /// <param name="handle">Win32 event handle created.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateResetEvent(out IntPtr handle);

        /// <summary>
        /// Managed wrapper for DestroyResetEvent
        /// </summary>
        /// <param name="handle">Win32 event handle to destroy.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyResetEvent(IntPtr handle);

        /// <summary>
        /// Managed wrapper for RaiseResetEvent
        /// </summary>
        /// <param name="handle">Win32 event handle to set.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RaiseResetEvent(IntPtr handle);

        /// <summary>
        /// Managed wrapper for LockObjectExtFromGit
        /// </summary>
        /// <param name="gitKey">The key used to refer to this object in the GIT.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        [DllImport(ExternDll.Penimc, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LockWispObjectFromGit(UInt32 gitKey);

        /// <summary>
        /// Managed wrapper for UnlockObjectExtFromGit
        /// </summary>
        /// <param name="gitKey">The key used to refer to this object in the GIT.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        [DllImport(ExternDll.Penimc, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnlockWispObjectFromGit(UInt32 gitKey);

        /// <summary>
        /// Managed wrapper for CoCreateInstance
        /// </summary>
        /// <param name="clsid">CLSID of the COM class to be instantiated</param>
        /// <param name="punkOuter">Aggregate object</param>
        /// <param name="context">Context in which the newly created object will run</param>
        /// <param name="iid">Identifier of the Interface</param>
        /// <returns>Returns the COM object created by CoCreateInstance</returns>
        [return: MarshalAs(UnmanagedType.Interface)]
        [DllImport(ExternDll.Ole32, ExactSpelling = true, PreserveSig = false)]
        private static extern object CoCreateInstance(
            [In]
            ref Guid clsid,
            [MarshalAs(UnmanagedType.Interface)]
            object punkOuter,
            int context,
            [In]
            ref Guid iid);

        /// <summary>
        /// Deactivates the specified Activation Context.
        /// </summary>
        /// <remarks>
        /// See: https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-deactivateactctx
        /// </remarks>
        /// <param name="flags">Flags that indicate how the deactivation is to occur.</param>
        /// <param name="activationCtxCookie">The ULONG_PTR that was passed into the call to ActivateActCtx.
        /// This value is used as a cookie to identify a specific activated activation context.</param>
        /// <returns>True on success, false otherwise.</returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.Kernel32, ExactSpelling = true)]
        private static extern bool DeactivateActCtx(int flags, IntPtr activationCtxCookie);
    }

#if OLD_ISF
    internal class CompressorSafeHandle: SafeHandle
    {
        private CompressorSafeHandle()
            : this(true)
        {
        }

        private CompressorSafeHandle(bool ownHandle)
            : base(IntPtr.Zero, ownHandle)
        {
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.
        public override bool IsInvalid
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return IsClosed || handle == IntPtr.Zero;
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        override protected bool ReleaseHandle()
        {
            //
            // return code from this is void.
            // internally it just calls delete on
            // the compressor pointer
            //
            UnsafeNativeMethods.IsfReleaseCompressor(handle);
            handle = IntPtr.Zero;
            return true;
        }

        public static CompressorSafeHandle Null
        {
            get
            {
              return new CompressorSafeHandle(false);
            }
        }
    }
#endif
}
