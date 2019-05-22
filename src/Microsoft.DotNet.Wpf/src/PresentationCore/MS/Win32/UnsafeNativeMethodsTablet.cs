// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Windows.Ink;
using System.Diagnostics;
using MS.Win32;

namespace MS.Win32.Recognizer
{
    //
    // Tablet unsafe native methods structs and pinvokes used by PresentationCore
    //
    internal static class UnsafeNativeMethods
    {
        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int CreateRecognizer([In] ref Guid clsid, [Out] out RecognizerSafeHandle hRec);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int DestroyRecognizer([In] IntPtr hRec);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int CreateContext([In] RecognizerSafeHandle hRec, [Out] out ContextSafeHandle hRecContext);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int DestroyContext([In] IntPtr hRecContext);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int AddStroke([In] ContextSafeHandle hRecContext, [In] ref PACKET_DESCRIPTION packetDesc, [In] uint cbPackets, [In] IntPtr pByte, [In, MarshalAs(UnmanagedType.LPStruct)] NativeMethods.XFORM xForm);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int SetEnabledUnicodeRanges([In] ContextSafeHandle hRecContext, [In] uint cRangs, [In] CHARACTER_RANGE[] charRanges);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int EndInkInput([In] ContextSafeHandle hRecContext);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int Process([In] ContextSafeHandle hRecContext, [Out] out bool partialProcessing);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int GetAlternateList([In] ContextSafeHandle hRecContext, [In, Out] ref RECO_RANGE recoRange, [In, Out] ref uint cAlts, [In, Out] IntPtr[] recAtls, [In] ALT_BREAKS breaks);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int DestroyAlternate([In] IntPtr hRecAtls);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        internal static extern int GetString([In] IntPtr hRecAtls, [Out] out RECO_RANGE recoRange, [In, Out]ref uint size, [In, Out] StringBuilder recoString);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int GetConfidenceLevel([In] IntPtr hRecAtls, [Out] out RECO_RANGE recoRange, [Out] out RecognitionConfidence confidenceLevel);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int ResetContext([In] ContextSafeHandle hRecContext);

        /// <SecurityNote>
        ///     Critical: as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Mshwgst, CallingConvention = CallingConvention.Winapi)]
        internal static extern int GetLatticePtr([In] ContextSafeHandle hRecContext, [In] ref IntPtr pRecoLattice);
    }


    
    /// <summary>
    /// RecognizerSafeHandle
    /// </summary>
    /// <SecurityNote>
    ///      Critical: Marked critical to prevent inadvertant spread to transparent code.
    /// </SecurityNote>
    [SecurityCritical]
    internal class RecognizerSafeHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        /// <SecurityNote>
        ///     Critical: This code calls into a base class which is protected by link demand and by inheritance demand
        /// </SecurityNote>
        [SecurityCritical]
        private RecognizerSafeHandle()
            : this(true)
        {
        }

        /// <SecurityNote>
        ///     Critical: This code calls into a base class which is protected by link demand and by inheritance demand
        /// </SecurityNote>
        [SecurityCritical]
        private RecognizerSafeHandle(bool ownHandle)
            : base(IntPtr.Zero, ownHandle)
        {
        }


        // Do not provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.
        /// <SecurityNote>
        ///     Critical: This code calls into a base class which is protected by link demand an by inheritance demand.
        ///               Also the code accessed the SecurityCritical member - handle.
        /// </SecurityNote>
        public override bool IsInvalid
        {
            [SecurityCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return IsClosed || handle == IntPtr.Zero;
            }
        }

        /// <SecurityNote>
        ///     Critical: Calls a SecurityCritical method 
        ///                 NativeRecognizer.UnsafeNativeMethods.DestroyRecognizer
        /// </SecurityNote>
        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        override protected bool ReleaseHandle()
        {
            Debug.Assert(handle != IntPtr.Zero);
            return (MS.Internal.HRESULT.Succeeded(MS.Win32.Recognizer.UnsafeNativeMethods.DestroyRecognizer(handle)));
        }
    }

    /// <summary>
    /// ContextSafeHandle
    /// </summary>
    /// <SecurityNote>
    ///     Critical: Marked critical to prevent inadvertant spread to transparent code.
    /// </SecurityNote>
    [SecurityCritical]
    internal class ContextSafeHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles

        /// <SecurityNote>
        ///     Critical:This code calls into a base class which is protected by link demand an by inheritance demand
        /// </SecurityNote>
        [SecurityCritical]
        private ContextSafeHandle()
            : this(true)
        {
        }

        /// <SecurityNote>
        ///     Critical:This code calls into a base class which is protected by link demand an by inheritance demand
        /// </SecurityNote>
        [SecurityCritical]
        private ContextSafeHandle(bool ownHandle)
            : base(IntPtr.Zero, ownHandle)
        {
        }


        // Do not provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.
        /// <SecurityNote>
        ///     Critical: This code calls into a base class which is protected by link demand an by inheritance demand.
        ///               Also the code accessed the SecurityCritical member - handle.
        /// </SecurityNote>
        public override bool IsInvalid
        {
            [SecurityCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return IsClosed || handle == IntPtr.Zero;
            }
        }


        /// <SecurityNote>
        ///     Critical: Calls a SecurityCritical method 
        ///                 NativeRecognizer.UnsafeNativeMethods.DestroyContext
        /// </SecurityNote>
        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        override protected bool ReleaseHandle()
        {
            //Note: It is not an error to have already called DestroyRecognizer
            //which makes _recognizerHandle.IsInvalid == true before calling
            //DestroyContext.  I have removed this assert, but left it commented for
            //context.
            //Debug.Assert(_recognizerHandle != null && !_recognizerHandle.IsInvalid);
            Debug.Assert(handle != IntPtr.Zero);
            int hr = MS.Win32.Recognizer.UnsafeNativeMethods.DestroyContext(handle);

            // Now, dereference the attached recognizer.
            _recognizerHandle = null;

            return MS.Internal.HRESULT.Succeeded(hr);
        }


        /// <SecurityNote>
        ///     Critical: Accesses the SecurityCritical member _recognizerHandle
        /// </SecurityNote>
        [SecurityCritical]
        internal void AddReferenceOnRecognizer(RecognizerSafeHandle handle)
        {
            System.Diagnostics.Debug.Assert(_recognizerHandle == null);
            _recognizerHandle = handle;
        }

        /// <SecurityNote>
        ///     Critical: This is a handle to our recognizer, we consider it plutonium
        /// </SecurityNote>
        [SecurityCritical]
        private RecognizerSafeHandle _recognizerHandle;
    }
    
    // The structure has been copied from public\internal\drivers\inc\tpcshrd.h
    //typedef struct _PROPERTY_METRICS
    //    {
    //    LONG nLogicalMin;
    //    LONG nLogicalMax;
    //    PROPERTY_UNITS Units;
    //    FLOAT fResolution;
    //    }     PROPERTY_METRICS;
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPERTY_METRICS
    {
        public int nLogicalMin;
        public int nLogicalMax;
        public int Units;
        public float fResolution;
    }

    // The structure has been copied from public\internal\drivers\inc\tpcshrd.h
    //typedef struct _PACKET_PROPERTY
    //    {
    //    GUID guid;
    //    PROPERTY_METRICS PropertyMetrics;
    //    }     PACKET_PROPERTY;
    [StructLayout(LayoutKind.Sequential)]
    internal struct PACKET_PROPERTY
    {
        public Guid guid;
        public PROPERTY_METRICS PropertyMetrics;
    }

    // The structure has been copied from public\internal\drivers\inc\tpcshrd.h
    //typedef struct _PACKET_DESCRIPTION
    //    {
    //    ULONG cbPacketSize;
    //    ULONG cPacketProperties;
    //    PACKET_PROPERTY *pPacketProperties;
    //    ULONG cButtons;
    //    GUID *pguidButtons;
    //    }     PACKET_DESCRIPTION;
    [StructLayout(LayoutKind.Sequential)]
    internal struct PACKET_DESCRIPTION
    {
        public uint cbPacketSize;
        public uint cPacketProperties;
        public IntPtr pPacketProperties;
        public uint cButtons;
        public IntPtr pguidButtons;
    };

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //typedef struct tagCHARACTER_RANGE
    //    {
    //    WCHAR wcLow;
    //    USHORT cChars;
    //    } CHARACTER_RANGE
    [StructLayout(LayoutKind.Sequential)]
    internal struct CHARACTER_RANGE
    {
        public ushort wcLow;
        public ushort cChars;
    }

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //typedef struct tagRECO_RANGE
    //    {
    //    ULONG iwcBegin;
    //    ULONG cCount;
    //    }     RECO_RANGE;
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECO_RANGE
    {
        public uint iwcBegin;
        public uint cCount;
    }

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //enum enumALT_BREAKS
    //    { ALT_BREAKS_SAME = 0,
    //    ALT_BREAKS_UNIQUE = 1,
    //    ALT_BREAKS_FULL   = 2
    //    }     ALT_BREAKS;
    internal enum ALT_BREAKS
    {
        ALT_BREAKS_SAME = 0,
        ALT_BREAKS_UNIQUE   = 1,
        ALT_BREAKS_FULL = 2
    }

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //enum enumRECO_TYPE
    //    { RECO_TYPE_WSTRING   = 0,
    //    RECO_TYPE_WCHAR   = 1
    //    }     RECO_TYPE;
    // internal enum RECO_TYPE : ushort
    //{
    //    RECO_TYPE_WSTRING = 0,
    //    RECO_TYPE_WCHAR = 1
    //}

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //typedef struct tagRECO_LATTICE_PROPERTY
    //    {
    //    GUID guidProperty;
    //    USHORT cbPropertyValue;
    //    BYTE *pPropertyValue;
    //    }     RECO_LATTICE_PROPERTY;
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECO_LATTICE_PROPERTY
    {
        public Guid     guidProperty;
        public ushort   cbPropertyValue;
        public IntPtr   pPropertyValue;
    }

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //typedef struct tagRECO_LATTICE_PROPERTIES
    //    {
    //    ULONG cProperties;
    //    RECO_LATTICE_PROPERTY **apProps;
    //    }     RECO_LATTICE_PROPERTIES;
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECO_LATTICE_PROPERTIES
    {
        public uint     cProperties;
        public IntPtr   apProps;
    }

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //typedef int RECO_SCORE;
    //
    //typedef struct tagRECO_LATTICE_ELEMENT
    //    {
    //    RECO_SCORE score;
    //    WORD type;
    //    BYTE *pData;
    //    ULONG ulNextColumn;
    //    ULONG ulStrokeNumber;
    //    RECO_LATTICE_PROPERTIES epProp;
    //    }     RECO_LATTICE_ELEMENT;
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECO_LATTICE_ELEMENT
    {
        public int      score;
        public ushort   type;
        public IntPtr   pData;
        public uint     ulNextColumn;
        public uint     ulStrokeNumber;
        public RECO_LATTICE_PROPERTIES epProp;
    }

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //typedef struct tagRECO_LATTICE_COLUMN
    //    {
    //    ULONG key;
    //    RECO_LATTICE_PROPERTIES cpProp;
    //    ULONG cStrokes;
    //    ULONG *pStrokes;
    //    ULONG cLatticeElements;
    //    RECO_LATTICE_ELEMENT *pLatticeElements;
    //    }     RECO_LATTICE_COLUMN;
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECO_LATTICE_COLUMN
    {
        public uint     key;
        public RECO_LATTICE_PROPERTIES cpProp;
        public uint     cStrokes;
        public IntPtr   pStrokes;
        public uint     cLatticeElements;
        public IntPtr   pLatticeElements;
    }

    // The structure has been copied from public\internal\drivers\inc\rectypes.h
    //typedef struct tagRECO_LATTICE
    //    {
    //    ULONG ulColumnCount;
    //    RECO_LATTICE_COLUMN *pLatticeColumns;
    //    ULONG ulPropertyCount;
    //    GUID *pGuidProperties;
    //    ULONG ulBestResultColumnCount;
    //    ULONG *pulBestResultColumns;
    //    ULONG *pulBestResultIndexes;
    //    }     RECO_LATTICE;
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECO_LATTICE
    {
        public uint     ulColumnCount;
        public IntPtr   pLatticeColumns;
        public uint     ulPropertyCount;
        public IntPtr   pGuidProperties;
        public uint     ulBestResultColumnCount;
        public IntPtr   pulBestResultColumns;
        public IntPtr   pulBestResultIndexes;
    }
}
