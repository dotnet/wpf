﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    // Internal Class that wraps the IntPtr to the Node
    internal sealed class SafeConditionMemoryHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        // (Also used by UiaCoreApi to create invalid handles.)
        internal SafeConditionMemoryHandle()
            : base(IntPtr.Zero, true)
        {
        }

        // No need to provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.
        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeCoTaskMem(handle);
            return true;
        }

        // uiaCondition is one of the Uia condition structs - eg UiaCoreApi.UiaAndOrCondition
        internal static SafeConditionMemoryHandle AllocateConditionHandle<T>(T uiaCondition)
            where T : struct
        {
            // Allocate SafeHandle first to avoid failure later.
            SafeConditionMemoryHandle sh = new SafeConditionMemoryHandle();
            int size = Marshal.SizeOf(uiaCondition);

            try { }
            finally
            {
                IntPtr mem = Marshal.AllocCoTaskMem(size);
                sh.SetHandle(mem);
            }
            Marshal.StructureToPtr(uiaCondition, sh.handle, false);
            return sh;
        }


        // used by And/Or conditions to allocate an array of pointers to other conditions
        internal static SafeConditionMemoryHandle AllocateConditionArrayHandle(Condition[] conditions)
        {
            // Allocate SafeHandle first to avoid failure later.
            SafeConditionMemoryHandle sh = new SafeConditionMemoryHandle();

            try { }
            finally
            {
                IntPtr mem = Marshal.AllocCoTaskMem(conditions.Length * IntPtr.Size);
                sh.SetHandle(mem);
            }

            unsafe
            {
                IntPtr* pdata = (IntPtr*)sh.handle;
                for (int i = 0; i < conditions.Length; i++)
                {
                    *pdata++ = conditions[i]._safeHandle.handle;
                }
            }
            return sh;
        }

        // Can't pass null into an API that takes a SafeHandle - so using this instead...
        internal static SafeConditionMemoryHandle NullHandle = new SafeConditionMemoryHandle();
    }





    /// <summary>
    /// Base type for conditions used by LogicalElementSearcher.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal abstract class Condition
#else
    public abstract class Condition
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // Internal ctor to prevent others from deriving from this class
        internal Condition()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public readonly fields & constants
        //
        //------------------------------------------------------
 
        #region Public readonly fields & constants

        /// <summary>Condition object that always evaluates to true</summary>
        public static readonly Condition TrueCondition = new BoolCondition(true);
        /// <summary>Condition object that always evaluates to false</summary>
        public static readonly Condition FalseCondition = new BoolCondition(false);

        #endregion Public readonly fields & constants

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal void SetMarshalData<T>(T uiaCondition)
            where T : struct
        {
            // Takes one of the interop UiaCondition classes (from UiaCoreApi.cs), and allocs
            // a SafeHandle with associated unmanaged memory - can then pass that to the UIA APIs.
            _safeHandle = SafeConditionMemoryHandle.AllocateConditionHandle(uiaCondition);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal SafeConditionMemoryHandle _safeHandle;

        #endregion Internal Fields


        //------------------------------------------------------
        //
        //  Nested Classes
        //
        //------------------------------------------------------
        private class BoolCondition: Condition
        {
            internal BoolCondition(bool b)
            {
                SetMarshalData(new UiaCoreApi.UiaCondition(b ? UiaCoreApi.ConditionType.True : UiaCoreApi.ConditionType.False));
            }
        }
    }
}
