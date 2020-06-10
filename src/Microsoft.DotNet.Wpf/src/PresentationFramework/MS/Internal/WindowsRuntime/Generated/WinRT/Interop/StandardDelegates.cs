// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace WinRT.Interop
{
    // standard accessors/mutators
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsBoolean(IntPtr thisPtr, out byte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsBoolean(IntPtr thisPtr, byte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsChar(IntPtr thisPtr, out ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsChar(IntPtr thisPtr, ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsSByte(IntPtr thisPtr, out sbyte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsSByte(IntPtr thisPtr, sbyte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsByte(IntPtr thisPtr, out byte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsByte(IntPtr thisPtr, byte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsInt16(IntPtr thisPtr, out short value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsInt16(IntPtr thisPtr, short value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsUInt16(IntPtr thisPtr, out ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsUInt16(IntPtr thisPtr, ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsInt32(IntPtr thisPtr, out int value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsInt32(IntPtr thisPtr, int value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsUInt32(IntPtr thisPtr, out uint value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsUInt32(IntPtr thisPtr, uint value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsInt64(IntPtr thisPtr, out long value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsInt64(IntPtr thisPtr, long value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsUInt64(IntPtr thisPtr, out ulong value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsUInt64(IntPtr thisPtr, ulong value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsFloat(IntPtr thisPtr, out float value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsFloat(IntPtr thisPtr, float value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsDouble(IntPtr thisPtr, out double value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsDouble(IntPtr thisPtr, double value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsObject(IntPtr thisPtr, out IntPtr value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsObject(IntPtr thisPtr, IntPtr value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsGuid(IntPtr thisPtr, out Guid value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsGuid(IntPtr thisPtr, Guid value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _get_PropertyAsString(IntPtr thisPtr, out IntPtr value);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal delegate int _put_PropertyAsString(IntPtr thisPtr, IntPtr value);
}
