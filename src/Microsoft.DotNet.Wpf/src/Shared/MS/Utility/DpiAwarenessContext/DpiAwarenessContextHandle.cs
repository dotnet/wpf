// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Utility
{
    using MS.Win32;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// A <see cref="SafeHandle"/> type representing DPI_AWARENESS_CONTEXT values
    /// </summary>
    /// <remarks>
    /// A <see cref="SafeHandle"/> for a pseudo-handle would normally be an overkill. In this instance,
    /// it is not quite so. DPI_AWARENESS_CONTEXT handles require extra work to compare, require special
    /// work to extract the DPI information from, and need to be converted into an integral form (for e.g.,
    /// an enumeration) before it can be transmitted effectively to the renderer. All of this work requires
    /// some sort of encapsulation and abstraction. It is easier to do this if the native pseudo-handles are
    /// converted into an appropriate class instance from the start.
    /// </remarks>
    internal partial class DpiAwarenessContextHandle : SafeHandle, IEquatable<IntPtr>, IEquatable<DpiAwarenessContextHandle>, IEquatable<DpiAwarenessContextValue>
    {
        static DpiAwarenessContextHandle()
        {
            WellKnownContextValues = new Dictionary<DpiAwarenessContextValue, IntPtr>
            {
                { DpiAwarenessContextValue.Unaware, new IntPtr((int)DpiAwarenessContextValue.Unaware) },
                { DpiAwarenessContextValue.SystemAware, new IntPtr((int)DpiAwarenessContextValue.SystemAware) },
                { DpiAwarenessContextValue.PerMonitorAware, new IntPtr((int)DpiAwarenessContextValue.PerMonitorAware) },
                { DpiAwarenessContextValue.PerMonitorAwareVersion2, new IntPtr((int)DpiAwarenessContextValue.PerMonitorAwareVersion2) },
            };

            DPI_AWARENESS_CONTEXT_UNAWARE =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.Unaware]);

            DPI_AWARENESS_CONTEXT_SYSTEM_AWARE =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.SystemAware]);

            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.PerMonitorAware]);

            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.PerMonitorAwareVersion2]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiAwarenessContextHandle"/> class.
        /// </summary>
        internal DpiAwarenessContextHandle()
            : base(IntPtr.Zero, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiAwarenessContextHandle"/> class.
        /// </summary>
        /// <param name="dpiAwarenessContextValue">Enumeration value equivalent to DPI_AWARENESS_CONTEXT handle value</param>
        internal DpiAwarenessContextHandle(DpiAwarenessContextValue dpiAwarenessContextValue)
            : base(WellKnownContextValues[dpiAwarenessContextValue], false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiAwarenessContextHandle"/> class.
        /// </summary>
        /// <param name="dpiContext">Handle to DPI Awareness context</param>
        internal DpiAwarenessContextHandle(IntPtr dpiContext)
            : base(dpiContext, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiAwarenessContextHandle"/> class.
        /// </summary>
        /// <param name="invalidHandleValue">The value of an invalid handle</param>
        /// <param name="ownsHandle">Set to true to reliably let SafeHandle release the handle
        /// during the finalization phase; otherwise, set to false (not recommended).</param>
        /// <remarks>
        ///     Always set ownsHandle = false. This ensures that the handle will
        ///     never be attempted to be released, which is just what we need since
        ///     this is a pseudo-handle.
        /// </remarks>
        protected DpiAwarenessContextHandle(IntPtr invalidHandleValue, bool ownsHandle)
            : base(invalidHandleValue, false)
        {
        }

        /// <inheritdoc/>
        public override bool IsInvalid
        {
            get
            {
                // This is a pseudo-handle. Always
                // returning true will ensure that
                // critical-finalization will be avoided
                return true;
            }
        }

        /// <summary>
        /// Gets DPI_AWARENESS_CONTEST_UNAWARE
        /// </summary>
        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_UNAWARE { get; }

        /// <summary>
        /// Gets DPI_AWARENESS_CONTEXT_SYSTEM_AWARE
        /// </summary>
        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_SYSTEM_AWARE { get; }

        /// <summary>
        /// Gets DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE
        /// </summary>
        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE { get; }

        /// <summary>
        /// Gets DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
        /// </summary>
        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 { get; }

        /// <summary>
        /// Gets map of well-known DPI awareness context handles
        /// </summary>
        private static Dictionary<DpiAwarenessContextValue, IntPtr> WellKnownContextValues { get; }

        /// <summary>
        /// Conversion to DpiAwarenessContextValue
        /// </summary>
        /// <param name="dpiAwarenessContextHandle">Handle being converted</param>
        public static explicit operator DpiAwarenessContextValue(DpiAwarenessContextHandle dpiAwarenessContextHandle)
        {
            foreach (DpiAwarenessContextValue dpiContextValue in Enum.GetValues(typeof(DpiAwarenessContextValue)))
            {
                if (dpiContextValue != DpiAwarenessContextValue.Invalid)
                {
                    if (dpiAwarenessContextHandle.Equals(dpiContextValue))
                    {
                        return dpiContextValue;
                    }
                }
            }

            return DpiAwarenessContextValue.Invalid;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="dpiContextHandle">DPI context being compared against</param>
        /// <returns>True if equivalent to the other DPI context, otherwise False</returns>
        public bool Equals(DpiAwarenessContextHandle dpiContextHandle)
        {
            return SafeNativeMethods.AreDpiAwarenessContextsEqual(this.DangerousGetHandle(), dpiContextHandle.DangerousGetHandle());
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="dpiContext">DPI Context being compared against</param>
        /// <returns>True if equivalent to the other DPI context, otherwise False</returns>
        public bool Equals(IntPtr dpiContext)
        {
            return DpiUtil.AreDpiAwarenessContextsEqual(this.DangerousGetHandle(), dpiContext);
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="dpiContextEnumValue">DPI context enumeration value being compared against</param>
        /// <returns>True if equivalent to the DPI context enum value, otherwise False</returns>
        public bool Equals(DpiAwarenessContextValue dpiContextEnumValue)
        {
            return this.Equals(WellKnownContextValues[dpiContextEnumValue]);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is IntPtr)
            {
                return this.Equals((IntPtr)obj);
            }
            else if (obj is DpiAwarenessContextHandle)
            {
                return this.Equals((DpiAwarenessContextHandle)obj);
            }
            else if (obj is DpiAwarenessContextValue)
            {
                return this.Equals((DpiAwarenessContextValue)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ((DpiAwarenessContextValue)this).GetHashCode();
        }

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            // Nothing to release - just return true
            // This will never get called
            // because the handle is marked as invalid
            // as soon as it is created
            return true;
        }
    }
}
