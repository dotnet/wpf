// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Utility
{
    using MS.Utility;
    using MS.Win32;
    using System;

    /// <content>
    /// Contains inner class <see cref="DpiUtil"/>
    /// </content>
    internal partial class DpiAwarenessContextHandle
    {
        /// <summary>
        /// Contains High-DPI specific utility methods related
        /// </summary>
        private static class DpiUtil
        {
            /// <summary>
            /// True if user32.dll!AreDpiAwarenessContextsEqual is supported on
            /// the current platform, otherwise False
            /// </summary>
            private static bool IsAreDpiAwarenessContextsEqualMethodSupported { get; set; } = true;

            /// <summary>
            /// Determines whether two DPI_AWARENESS_CONTEXT values are identical.
            /// </summary>
            /// <param name="dpiContextA">The first value to compare</param>
            /// <param name="dpiContextB">The second value to compare</param>
            /// <returns>Returns true if the value are equal, otherwise false</returns>
            /// <remarks>
            /// <see cref="SafeNativeMethods.AreDpiAwarenessContextsEqual"/> is supported on Windows 10 v1607
            /// and later. On Platforms prior to that, we will fall-back to direct comparison of these
            /// pseudo-handles.
            /// </remarks>
            internal static bool AreDpiAwarenessContextsEqual(IntPtr dpiContextA, IntPtr dpiContextB)
            {
                if (IsAreDpiAwarenessContextsEqualMethodSupported)
                {
                    try
                    {
                        return SafeNativeMethods.AreDpiAwarenessContextsEqual(dpiContextA, dpiContextB);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsAreDpiAwarenessContextsEqualMethodSupported = false;
                    }
                }

                return AreDpiAwarenessContextsTriviallyEqual(dpiContextA, dpiContextB);
            }

            /// <summary>
            /// Determines whether two DPI_AWARENESS_CONTEXT handles are trivially equal
            /// </summary>
            /// <param name="dpiContextA">The first value to compare</param>
            /// <param name="dpiContextB">The second value to compare</param>
            /// <returns>Returns true if the values are equal, otherwise false</returns>
            private static bool AreDpiAwarenessContextsTriviallyEqual(IntPtr dpiContextA, IntPtr dpiContextB)
            {
                return dpiContextA == dpiContextB;
            }
        }
    }
}