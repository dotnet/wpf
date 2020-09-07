// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO;
using System.Security;
using System.Windows.Media.Imaging;
using System.Windows.Media.Composition;
using System.Text;
using MS.Internal.PresentationCore;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

//
// This class wraps a PROPVARIANT type for interop with the unmanaged metadata APIs.  Only
// the capabilities used by this API are supported (so for example, there is no SAFEARRAY, IDispatch,
// general VT_UNKNOWN support, etc.)
//
// The types are mapped to C# types as follows:
//
// byte <=> VT_UI1
// sbyte <=> VT_I1
// char <=> VT_LPSTR (size 1)
// ushort <=> VT_UI2
// short <=> VT_I2
// String <=> VT_LPWSTR
// uint <=> VT_UI4
// int <=> VT_I4
// UInt64 <=> VT_UI8
// Int64 <=> VT_I8
// float <=> VT_R4
// double <=> VT_R8
// Guid <=> VT_CLSID
// bool <=> VT_BOOL
// BitmapMetadata <=> VT_UNKNOWN (IWICMetadataQueryReader)
// BitmapMetadataBlob <=> VT_BLOB
//
// For array types:
//
// byte[] <=> VT_UI1|VT_VECTOR
// sbyte[] <=> VT_I1|VT_VECTOR
// char[] <=> VT_LPSTR (size is length of array - treated as ASCII string) - read back is String, use ToCharArray().
// char[][] <=> VT_LPSTR|VT_VECTOR (array of ASCII strings)
// ushort[] <=> VT_UI2|VT_VECTOR
// short[] <=> VT_I2|VT_VECTOR
// String[] <=> VT_LPWSTR|VT_VECTOR
// uint[] <=> VT_UI4|VT_VECTOR
// int[] <=> VT_I4|VT_VECTOR
// UInt64[] <=> VT_UI8|VT_VECTOR
// Int64[] <=> VT_I8|VT_VECTOR
// float[] <=> VT_R4|VT_VECTOR
// double[] <=> VT_R8|VT_VECTOR
// Guid[] <=> VT_CLSID|VT_VECTOR
// bool[] <=> VT_BOOL|VT_VECTOR
//

namespace System.Windows.Media.Imaging
{
    #region PropVariant

    [StructLayout(LayoutKind.Sequential, Pack=0)]
    internal struct PROPARRAY
    {
        internal UInt32 cElems;
        internal IntPtr pElems;
    }

    [StructLayout(LayoutKind.Explicit, Pack=1)]
    internal struct PROPVARIANT
    {
       [FieldOffset(0)] internal ushort varType;
       [FieldOffset(2)] internal ushort wReserved1;
       [FieldOffset(4)] internal ushort wReserved2;
       [FieldOffset(6)] internal ushort wReserved3;

       [FieldOffset(8)] internal byte bVal;
       [FieldOffset(8)] internal sbyte cVal;
       [FieldOffset(8)] internal ushort uiVal;
       [FieldOffset(8)] internal short iVal;
       [FieldOffset(8)] internal UInt32 uintVal;
       [FieldOffset(8)] internal Int32 intVal;
       [FieldOffset(8)] internal UInt64 ulVal;
       [FieldOffset(8)] internal Int64 lVal;
       [FieldOffset(8)] internal float fltVal;
       [FieldOffset(8)] internal double dblVal;
       [FieldOffset(8)] internal short boolVal;
       [FieldOffset(8)] internal IntPtr pclsidVal; //this is for GUID ID pointer
       [FieldOffset(8)] internal IntPtr pszVal; //this is for ansi string pointer
       [FieldOffset(8)] internal IntPtr pwszVal; //this is for Unicode string pointer
       [FieldOffset(8)] internal IntPtr punkVal; //this is for punkVal (interface pointer)
       [FieldOffset(8)] internal PROPARRAY ca;
       [FieldOffset(8)] internal System.Runtime.InteropServices.ComTypes.FILETIME filetime;


        /// <summary>
        /// CopyBytes - Poor man's mem copy.  Copies cbData from pbFrom to pbTo.
        /// </summary>
        /// <param name="pbTo"> byte* pointing to the "to" array.</param>
        /// <param name="cbTo"> int - count of bytes of receiving buffer.</param>
        /// <param name="pbFrom"> byte* pointing to the "from" array.</param>
        /// <param name="cbFrom"> int - count of bytes to copy from buffer.</param>
        private static unsafe void CopyBytes(
            byte* pbTo,
            int cbTo,
            byte* pbFrom,
            int cbFrom
        )
        {
            if (cbFrom>cbTo)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_InsufficientBufferSize));
            }

            byte* pCurFrom = (byte*)pbFrom;
            byte* pCurTo = (byte*)pbTo;

            for (int i = 0; i < cbFrom; i++)
            {
                pCurTo[i] = pCurFrom[i];
            }
        }

        internal void InitVector(Array array, Type type, VarEnum varEnum)
        {
            Init(array,  type, varEnum | VarEnum.VT_VECTOR);
        }

        internal void Init(Array array, Type type, VarEnum vt)
        {
            varType = (ushort) vt;
            ca.cElems = 0;
            ca.pElems = IntPtr.Zero;

            int length = array.Length;

            if (length > 0)
            {
                long size = Marshal.SizeOf(type) * length;

                IntPtr destPtr =IntPtr.Zero;
                GCHandle handle = new GCHandle();

                try
                {
                    destPtr = Marshal.AllocCoTaskMem((int) size);
                    handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    unsafe
                    {
                        CopyBytes((byte *) destPtr, (int)size, (byte *)handle.AddrOfPinnedObject(), (int)size);
                    }

                    ca.cElems = (uint)length;
                    ca.pElems = destPtr;

                    destPtr = IntPtr.Zero;
                }
                finally
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }

                    if (destPtr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(destPtr);
                    }
                }
            }
        }

        internal void Init(String[] value, bool fAscii)
        {
            varType = (ushort) (fAscii ? VarEnum.VT_LPSTR : VarEnum.VT_LPWSTR);
            varType |= (ushort) VarEnum.VT_VECTOR;
            ca.cElems = 0;
            ca.pElems = IntPtr.Zero;

            int length = value.Length;

            if (length > 0)
            {
                IntPtr destPtr = IntPtr.Zero;
                int sizeIntPtr = 0;
                unsafe
                {
                    sizeIntPtr = sizeof(IntPtr);
                }
                long size = sizeIntPtr * length;
                int index = 0;

                try
                {
                    IntPtr pString = IntPtr.Zero;

                    destPtr = Marshal.AllocCoTaskMem((int)size);

                    for (index=0; index<length; index++)
                    {
                        if (fAscii)
                        {
                            pString = Marshal.StringToCoTaskMemAnsi(value[index]);
                        }
                        else
                        {
                            pString = Marshal.StringToCoTaskMemUni(value[index]);
                        }
                        Marshal.WriteIntPtr(destPtr, (int)index*sizeIntPtr, pString);
                    }

                    ca.cElems = (uint) length;
                    ca.pElems = destPtr;
                    destPtr = IntPtr.Zero;
                }
                finally
                {
                    if (destPtr != IntPtr.Zero)
                    {
                        for (int i=0; i<index; i++)
                        {
                            IntPtr pString = Marshal.ReadIntPtr(destPtr, i*sizeIntPtr);
                            Marshal.FreeCoTaskMem(pString);
                        }

                        Marshal.FreeCoTaskMem(destPtr);
                    }
                }
            }
        }

        internal void Init(object value)
        {
            if (value == null)
            {
                varType = (ushort)VarEnum.VT_EMPTY;
            }
            else if (value is Array)
            {
                Type type = value.GetType();

                if (type == typeof(sbyte[]))
                {
                    InitVector(value as Array, typeof(sbyte), VarEnum.VT_I1);
                }
                else if (type == typeof(byte[]))
                {
                    InitVector(value as Array, typeof(byte), VarEnum.VT_UI1);
                }
                else if (value is char[])
                {
                    varType = (ushort) VarEnum.VT_LPSTR;
                    pszVal = Marshal.StringToCoTaskMemAnsi(new String(value as char[]));
                }
                else if (value is char[][])
                {
                    char[][] charArray = value as char[][];

                    String[] strArray = new String[charArray.GetLength(0)];

                    for (int i=0; i<charArray.Length; i++)
                    {
                        strArray[i] = new String(charArray[i] as char[]);
                    }

                    Init (strArray, true);
                }
                else if (type == typeof(short[]))
                {
                    InitVector(value as Array, typeof(short), VarEnum.VT_I2);
                }
                else if (type == typeof(ushort[]))
                {
                    InitVector(value as Array, typeof(ushort), VarEnum.VT_UI2);
                }
                else if (type == typeof(int[]))
                {
                    InitVector(value as Array, typeof(int), VarEnum.VT_I4);
                }
                else if (type == typeof(uint[]))
                {
                    InitVector(value as Array, typeof(uint), VarEnum.VT_UI4);
                }
                else if (type == typeof(Int64[]))
                {
                    InitVector(value as Array, typeof(Int64), VarEnum.VT_I8);
                }
                else if (type == typeof(UInt64[]))
                {
                    InitVector(value as Array, typeof(UInt64), VarEnum.VT_UI8);
                }
                else if (value is float[])
                {
                    InitVector(value as Array, typeof(float), VarEnum.VT_R4);
                }
                else if (value is double[])
                {
                    InitVector(value as Array, typeof(double), VarEnum.VT_R8);
                }
                else if (value is Guid[])
                {
                    InitVector(value as Array, typeof(Guid), VarEnum.VT_CLSID);
                }
                else if (value is String[])
                {
                    Init (value as String[], false);
                }
                else if (value is bool[])
                {
                    bool[] boolArray =value as bool[];
                    short[] array = new short[boolArray.Length];

                    for (int i=0; i<boolArray.Length; i++)
                    {
                        array[i] = (short) (boolArray[i] ? -1 : 0);
                    }

                    InitVector(array, typeof(short), VarEnum.VT_BOOL);
                }
                else
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Image_PropertyNotSupported));
                }
            }
            else
            {
                Type type = value.GetType();

                if (value is String)
                {
                    varType = (ushort) VarEnum.VT_LPWSTR;
                    pwszVal = Marshal.StringToCoTaskMemUni(value as String);
                }
                else if (type == typeof(sbyte))
                {
                    varType = (ushort) VarEnum.VT_I1;
                    cVal = (sbyte) value;
                }
                else if (type == typeof(byte))
                {
                    varType = (ushort) VarEnum.VT_UI1;
                    bVal = (byte) value;
                }
                else if (type == typeof(System.Runtime.InteropServices.ComTypes.FILETIME))
                {
                    varType = (ushort)VarEnum.VT_FILETIME;
                    filetime = (System.Runtime.InteropServices.ComTypes.FILETIME)value;
                }
                else if (value is char)
                {
                    varType = (ushort)VarEnum.VT_LPSTR;
                    pszVal = Marshal.StringToCoTaskMemAnsi(new String(new char[] { (char)value }));
                }
                else if (type == typeof(short))
                {
                    varType = (ushort)VarEnum.VT_I2;
                    iVal = (short)value;
                }
                else if (type == typeof(ushort))
                {
                    varType = (ushort)VarEnum.VT_UI2;
                    uiVal = (ushort)value;
                }
                else if (type == typeof(int))
                {
                    varType = (ushort)VarEnum.VT_I4;
                    intVal = (int)value;
                }
                else if (type == typeof(uint))
                {
                    varType = (ushort)VarEnum.VT_UI4;
                    uintVal = (uint)value;
                }
                else if (type == typeof(Int64))
                {
                    varType = (ushort)VarEnum.VT_I8;
                    lVal = (Int64)value;
                }
                else if (type == typeof(UInt64))
                {
                    varType = (ushort)VarEnum.VT_UI8;
                    ulVal = (UInt64)value;
                }
                else if (value is float)
                {
                    varType = (ushort)VarEnum.VT_R4;
                    fltVal = (float)value;
                }
                else if (value is double)
                {
                    varType = (ushort)VarEnum.VT_R8;
                    dblVal = (double)value;
                }
                else if (value is Guid)
                {
                    byte[] guid = ((Guid)value).ToByteArray();
                    varType = (ushort)VarEnum.VT_CLSID;
                    pclsidVal = Marshal.AllocCoTaskMem(guid.Length);
                    Marshal.Copy(guid, 0, pclsidVal, guid.Length);
                }
                else if (value is bool)
                {
                    varType = (ushort)VarEnum.VT_BOOL;
                    boolVal = (short)(((bool)value) ? -1 : 0);
                }
                else if (value is BitmapMetadataBlob)
                {
                    Init((value as BitmapMetadataBlob).InternalGetBlobValue(), typeof(byte), VarEnum.VT_BLOB);
                }
                else if (value is BitmapMetadata)
                {
                    IntPtr punkTemp = IntPtr.Zero;
                    BitmapMetadata metadata = value as BitmapMetadata;

                    SafeMILHandle metadataHandle = metadata.InternalMetadataHandle;

                    if (metadataHandle == null || metadataHandle.IsInvalid)
                    {
                        throw new NotImplementedException();
                    }

                    Guid wicMetadataQueryReader = MILGuidData.IID_IWICMetadataQueryReader;
                    HRESULT.Check(UnsafeNativeMethods.MILUnknown.QueryInterface(
                        metadataHandle,
                        ref wicMetadataQueryReader,
                        out punkTemp));

                    varType = (ushort)VarEnum.VT_UNKNOWN;
                    punkVal = punkTemp;
                }
                else
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Image_PropertyNotSupported));
                }
            }
       }

        internal void Clear()
       {
            VarEnum vt = (VarEnum) varType;

            if ((vt & VarEnum.VT_VECTOR) != 0 || vt == VarEnum.VT_BLOB)
            {
                if (ca.pElems != IntPtr.Zero)
                {
                    vt = vt & ~VarEnum.VT_VECTOR;

                    if (vt == VarEnum.VT_UNKNOWN)
                    {
                        IntPtr punkPtr = ca.pElems;
                        int sizeIntPtr = 0;
                        unsafe
                        {
                            sizeIntPtr = sizeof(IntPtr);
                        }

                        for (uint i=0; i<ca.cElems; i++)
                        {
                            #pragma warning suppress 6031 // Return value ignored on purpose.
                            UnsafeNativeMethods.MILUnknown.Release(Marshal.ReadIntPtr(punkPtr, (int) (i*sizeIntPtr)));
                        }
                    }
                    else if (vt == VarEnum.VT_LPWSTR || vt == VarEnum.VT_LPSTR)
                    {
                        IntPtr strPtr = ca.pElems;
                        int sizeIntPtr = 0;
                        unsafe
                        {
                            sizeIntPtr = sizeof(IntPtr);
                        }

                        for (uint i=0; i<ca.cElems; i++)
                        {
                            Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(strPtr, (int) (i*sizeIntPtr)));
                        }
                    }

                    Marshal.FreeCoTaskMem(ca.pElems);
                }
            }
            else if (vt == VarEnum.VT_LPWSTR ||
                vt == VarEnum.VT_LPSTR ||
                vt == VarEnum.VT_CLSID)
            {
                Marshal.FreeCoTaskMem(pwszVal);
            }
            else if (vt == VarEnum.VT_UNKNOWN)
            {
                #pragma warning suppress 6031 // Return value ignored on purpose.
                UnsafeNativeMethods.MILUnknown.Release(punkVal);
            }

            vt = VarEnum.VT_EMPTY;
       }

        internal object ToObject(object syncObject)
        {
            VarEnum vt = (VarEnum) varType;

            if ((vt & VarEnum.VT_VECTOR) != 0)
            {
                switch (vt & (~VarEnum.VT_VECTOR))
                {
                    case VarEnum.VT_EMPTY:
                        return null;

                    case VarEnum.VT_I1:
                    {
                        sbyte[] array = new sbyte[ca.cElems];
                        for (int i=0; i<ca.cElems; i++)
                            array[i] = (sbyte) Marshal.ReadByte(ca.pElems, i);
                        return array;
                    }

                    case VarEnum.VT_UI1:
                    {
                        byte[] array = new byte[ca.cElems];
                        Marshal.Copy(ca.pElems, array, 0, (int)ca.cElems);
                        return array;
                    }

                    case VarEnum.VT_I2:
                    {
                        short[] array = new short[ca.cElems];
                        Marshal.Copy(ca.pElems, array, 0, (int)ca.cElems);
                        return array;
                    }

                    case VarEnum.VT_UI2:
                    {
                        ushort[] array = new ushort[ca.cElems];
                        for (int i=0; i<ca.cElems; i++)
                            array[i] = (ushort) Marshal.ReadInt16(ca.pElems, i*sizeof(ushort));
                        return array;
                    }

                    case VarEnum.VT_I4:
                    {
                        int[] array = new int[ca.cElems];
                        Marshal.Copy(ca.pElems, array, 0, (int)ca.cElems);
                        return array;
                    }

                    case VarEnum.VT_UI4:
                    {
                        uint[] array = new uint[ca.cElems];
                        for (int i=0; i<ca.cElems; i++)
                            array[i] = (uint) Marshal.ReadInt32(ca.pElems, i*sizeof(uint));
                        return array;
                    }

                    case VarEnum.VT_I8:
                    {
                        Int64[] array = new Int64[ca.cElems];
                        Marshal.Copy(ca.pElems, array, 0, (int)ca.cElems);
                        return array;
                    }

                    case VarEnum.VT_UI8:
                    {
                        UInt64[] array = new UInt64[ca.cElems];
                        for (int i=0; i<ca.cElems; i++)
                            array[i] = (UInt64) Marshal.ReadInt64(ca.pElems, i*sizeof(UInt64));
                        return array;
                    }

                    case VarEnum.VT_R4:
                    {
                        float[] array = new float[ca.cElems];
                        Marshal.Copy(ca.pElems, array, 0, (int)ca.cElems);
                        return array;
                    }

                    case VarEnum.VT_R8:
                    {
                        double[] array = new double[ca.cElems];
                        Marshal.Copy(ca.pElems, array, 0, (int)ca.cElems);
                        return array;
                    }

                    case VarEnum.VT_BOOL:
                    {
                        bool[] array = new bool[ca.cElems];
                        for (int i=0; i<ca.cElems; i++)
                            array[i] = (bool) (Marshal.ReadInt16(ca.pElems, i*sizeof(ushort)) != 0);
                        return array;
                    }

                    case VarEnum.VT_CLSID:
                    {
                        Guid[] array = new Guid[ca.cElems];
                        for (int i=0; i<ca.cElems; i++)
                        {
                            byte[] guid = new byte[16];
                            Marshal.Copy(ca.pElems, guid, i*16, 16);
                            array[i] = new Guid(guid);
                        }
                        return array;
                    }

                    case VarEnum.VT_LPSTR:
                    {
                        String[] array = new String[ca.cElems];
                        int sizeIntPtr = 0;
                        unsafe
                        {
                            sizeIntPtr = sizeof(IntPtr);
                        }

                        for (int i=0; i<ca.cElems; i++)
                        {
                            IntPtr ptr = Marshal.ReadIntPtr(ca.pElems, i*sizeIntPtr);
                            array[i] = Marshal.PtrToStringAnsi(ptr);
                        }
                        return array;
                    }

                    case VarEnum.VT_LPWSTR:
                    {
                        String[] array = new String[ca.cElems];
                        int sizeIntPtr = 0;
                        unsafe
                        {
                            sizeIntPtr = sizeof(IntPtr);
                        }

                        for (int i=0; i<ca.cElems; i++)
                        {
                            IntPtr ptr = Marshal.ReadIntPtr(ca.pElems, i*sizeIntPtr);
                            array[i] = Marshal.PtrToStringUni(ptr);
                        }
                        return array;
                    }

                    case VarEnum.VT_UNKNOWN:
                    default:
                        break;
                }
            }
            else
            {
                switch (vt)
                {
                    case VarEnum.VT_EMPTY:
                        return null;

                    case VarEnum.VT_I1:
                        return cVal;

                    case VarEnum.VT_UI1:
                        return bVal;

                    case VarEnum.VT_I2:
                        return iVal;

                    case VarEnum.VT_UI2:
                        return uiVal;

                    case VarEnum.VT_I4:
                        return intVal;

                    case VarEnum.VT_UI4:
                        return uintVal;

                    case VarEnum.VT_I8:
                        return lVal;

                    case VarEnum.VT_UI8:
                        return ulVal;

                    case VarEnum.VT_R4:
                        return fltVal;

                    case VarEnum.VT_R8:
                        return dblVal;

                    case VarEnum.VT_FILETIME:
                        return filetime;

                    case VarEnum.VT_BOOL:
                        return (bool) (boolVal != 0);

                    case VarEnum.VT_CLSID:
                        byte[] guid = new byte[16];
                        Marshal.Copy(pclsidVal, guid, 0, 16);
                        return new Guid(guid);

                    case VarEnum.VT_LPSTR:
                        return Marshal.PtrToStringAnsi(pszVal);

                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(pwszVal);

                    case VarEnum.VT_BLOB:
                    {
                        byte[] blob = new byte[ca.cElems];
                        Marshal.Copy(ca.pElems, blob, 0, (int)ca.cElems);
                        return new BitmapMetadataBlob(blob);
                    }

                    case VarEnum.VT_UNKNOWN:
                    {
                        IntPtr queryHandle = IntPtr.Zero;
                        Guid guidIWICQueryWriter = MILGuidData.IID_IWICMetadataQueryWriter;
                        Guid guidIWICQueryReader = MILGuidData.IID_IWICMetadataQueryReader;

                        try
                        {
                            int hr = UnsafeNativeMethods.MILUnknown.QueryInterface(punkVal, ref guidIWICQueryWriter, out queryHandle);

                            if (hr == HRESULT.S_OK)
                            {
                                // It's a IWICMetadataQueryWriter interface - read and write
                                SafeMILHandle metadataHandle = new SafeMILHandle(queryHandle);

                                // To avoid releasing the queryHandle in finally.
                                queryHandle = IntPtr.Zero;

                                return new BitmapMetadata(metadataHandle, false, false, syncObject);
                            }
                            else
                            {
                                hr = UnsafeNativeMethods.MILUnknown.QueryInterface(punkVal, ref guidIWICQueryReader, out queryHandle);

                                if (hr == HRESULT.S_OK)
                                {
                                    // It's a IWICMetadataQueryReader interface - read only
                                    SafeMILHandle metadataHandle = new SafeMILHandle(queryHandle);

                                    // To avoid releasing the queryHandle in finally.
                                    queryHandle = IntPtr.Zero;

                                    return new BitmapMetadata(metadataHandle, true, false, syncObject);
                                }

                                HRESULT.Check(hr);
                            }
                        }
                        finally
                        {
                            if (queryHandle != IntPtr.Zero)
                            {
                                UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref queryHandle);
                            }
                        }
                        break;
                    }

                    default:
                        break;
                }
            }

            throw new System.NotSupportedException(SR.Get(SRID.Image_PropertyNotSupported));
       }

        internal bool RequiresSyncObject
        {
            get
            {
                return (varType == (ushort) VarEnum.VT_UNKNOWN);
            }
        }
    }

    #endregion
}
