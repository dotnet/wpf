// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

using Microsoft.Internal.OSVersionHelper.NativeConstants;

namespace Microsoft.Internal.OSVersionHelper
{
    /// <summary>
    /// Native methods used to support <see cref="VersionHelper"/> methods.
    /// </summary>
    internal static class NativeMethods
    {
        #region Private DllImports

        /// <summary>
        /// The RtlVerifyVersionInfo routine compares a specified set of operating system version 
        /// requirements to the corresponding attributes of the currently running version of the operating system.
        /// </summary>
        /// <param name="lpVersionInfo">
        /// A structure that specifies the operating system requirements to compare 
        /// to the corresponding attributes of the currently running version of the operating system
        /// </param>
        /// <param name="dwTypeMask">
        /// Specifies which members of <paramref name="lpVersionInfo"/> to compare with the 
        /// corresponding attributes of the currently running version of the operating system. 
        /// <paramref name="dwTypeMask"/> is set to a logical OR of one or more of <see cref="NativeConstants.TypeBitMasks"/> values.
        /// </param>
        /// <param name="dwlConditionMask">
        /// Specifies how to compare each <paramref name="lpVersionInfo"/> member. 
        /// To set the value of <paramref name="dwlConditionMask"/>, a caller should use <see cref="VER_SET_CONDITION(ref ulong, uint, byte)"/>
        /// </param>
        /// <returns> An <see cref="NtStatus"/> value representing success, revision mismatch (failure) or invalid parameter (failure) </returns>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern uint RtlVerifyVersionInfo([In] NativeTypes.OSVERSIONINFOEX lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask);

        /// <summary>
        /// Sets the bits of a 64-bit value to indicate the comparison operator to use for a specified operating system version attribute. 
        /// This function is used to build the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>  and 
        /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions.
        /// </summary>
        /// <param name="dwlConditionMask">
        /// A value to be passed as the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> and 
        /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions. The function stores the comparison information in the bits of this variable.
        /// </param>
        /// <param name="dwTypeBitMask">A mask that indicates the member of <see cref="NativeTypes.OSVERSIONINFOEX"/> whose comparision operator is being set.
        /// This value corresponds to one of the bits specified in the dwTypeMask parameter of <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>. 
        /// This parameter can have one of the values from <see cref="NativeConstants.TypeBitMasks"/></param>
        /// <param name="dwConditionMask"></param>
        /// <returns>The function returns the condition mask value.</returns>
        /// <remarks> 
        /// Before the first call to this function, initialize <paramref name="dwlConditionMask"/> variable to zero. 
        /// For subsequent calls, pass in the variable used in the previous call.
        /// </remarks>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern ulong VerSetConditionMask(ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask);

        #endregion // Private DllImports

        /// <summary>
        /// Compares a set of operating system version requirements to the corresponding values for the currently running version of the system.
        /// </summary>
        /// <param name="lpVersionInfo">A <see cref="NativeTypes.OSVERSIONINFOEX"/> instance containing the operating 
        /// system version requirements to compare. The <paramref name="dwTypeMask"/> parameter indicates the 
        /// members of this structure that contain information to compare. </param>
        /// <param name="dwTypeMask">A mask that indicates the members of the <see cref="NativeTypes.OSVERSIONINFOEX"/> instance to 
        /// be tested. This parameter can be one of the values from <see cref="NativeConstants.TypeBitMasks"/> </param>
        /// <param name="dwlConditionMask">The type of comparision to be used for each <paramref name="lpVersionInfo"/> member 
        /// being compared. To build this value, call the <see cref="VER_SET_CONDITION(ref ulong, uint, byte)"/> function once for 
        /// each <see cref="NativeTypes.OSVERSIONINFOEX"/> member being compared.</param>
        /// <returns>True if the currently running operating system satisfies the specified requirements, False otherwise.</returns>
        [SecuritySafeCritical]
        internal static bool VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask)
        {
            var result = RtlVerifyVersionInfo(lpVersionInfo, dwTypeMask, dwlConditionMask);

            if (result == NtStatus.STATUS_INVALID_PARAMETER)
            {
                throw new ArgumentException(string.Empty, string.Empty);
            }

            System.Diagnostics.Debug.Assert(
                (result == NtStatus.STATUS_SUCCESS) ||
                (result == NtStatus.STATUS_REVISION_MISMATCH));

            return (result == NtStatus.STATUS_SUCCESS);
        }

        /// <summary>
        /// Sets the bits of a 64-bit value to indicate the comparison operator to use for a specified operating system version attribute. 
        /// This function is used to build the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>  and 
        /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions.
        /// </summary>
        /// <param name="dwlConditionMask">
        /// A value to be passed as the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> and 
        /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions. The function stores the comparison information in the bits of this variable.
        /// </param>
        /// <param name="dwTypeBitMask">A mask that indicates the member of <see cref="NativeTypes.OSVERSIONINFOEX"/> whose comparision operator is being set.
        /// This value corresponds to one of the bits specified in the dwTypeMask parameter of <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>. 
        /// This parameter can have one of the values from <see cref="NativeConstants.TypeBitMasks"/></param>
        /// <param name="dwConditionMask"></param>
        /// <remarks> 
        /// Before the first call to this function, initialize <paramref name="dwlConditionMask"/> variable to zero. 
        /// For subsequent calls, pass in the variable used in the previous call.
        /// </remarks>
        [SecuritySafeCritical]
        internal static void VER_SET_CONDITION(ref ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask)
        {
            dwlConditionMask = VerSetConditionMask(dwlConditionMask, dwTypeBitMask, dwConditionMask);
        }
    }
}
