// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;

#pragma warning disable CS0649

namespace WinRT.Interop
{
    // IActivationFactory
    [Guid("00000035-0000-0000-C000-000000000046")]
    internal struct IActivationFactoryVftbl
    {
        public unsafe delegate int _ActivateInstance(IntPtr pThis, out IntPtr instance);

        public IInspectable.Vftbl IInspectableVftbl;
        public _ActivateInstance ActivateInstance;
    }

    // IDelegate
    internal struct IDelegateVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public IntPtr Invoke;
    }
}
