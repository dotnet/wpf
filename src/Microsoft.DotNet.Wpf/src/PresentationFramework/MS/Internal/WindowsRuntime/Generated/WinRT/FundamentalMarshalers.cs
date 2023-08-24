// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

namespace ABI.System
{
    internal struct Boolean
    {
        byte value;
        public static bool CreateMarshaler(bool value) => value;
        public static Boolean GetAbi(bool value) => new Boolean() { value = (byte)(value ? 1 : 0) };
        public static bool FromAbi(Boolean abi) => abi.value != 0;
        public static unsafe void CopyAbi(bool value, IntPtr dest) => *(byte*)dest.ToPointer() = GetAbi(value).value;
        public static Boolean FromManaged(bool value) => GetAbi(value);
        public static unsafe void CopyManaged(bool arg, IntPtr dest) => *(byte*)dest.ToPointer() = FromManaged(arg).value;
        public static void DisposeMarshaler(bool m) { }
        public static void DisposeAbi(byte abi) { }
    }

    internal struct Char
    {
        ushort value;
        public static char CreateMarshaler(char value) => value;
        public static Char GetAbi(char value) => new Char() { value = (ushort)value };
        public static char FromAbi(Char abi) => (char)abi.value;
        public static unsafe void CopyAbi(char value, IntPtr dest) => *(ushort*)dest.ToPointer() = GetAbi(value).value;
        public static Char FromManaged(char value) => GetAbi(value);
        public static unsafe void CopyManaged(char arg, IntPtr dest) => *(ushort*)dest.ToPointer() = FromManaged(arg).value;
        public static void DisposeMarshaler(char m) { }
        public static void DisposeAbi(Char abi) { }
    }
}

