// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Microsoft.Test.Diagnostics
{
    /// <summary>
    /// Allows a tree of processes to be managed by the OS and killed together if needed
    /// </summary>
    internal static class ProcessGroup
    {

        #region Private Data

        private const int ERROR_ACCESS_DENIED = 5;

        #endregion
        
        #region Public Members

        public static long CreateProcessGroup(int processId)
        {
            Process process = Process.GetProcessById(processId);

            if (process.HasExited)
                throw new InvalidOperationException("Process has exited");

            IntPtr hGroupObject = IntPtr.Zero;

            try
            {
                //Group name must be unique
                string groupName = Guid.NewGuid().ToString();
                hGroupObject = CreateJobObject(IntPtr.Zero, groupName);

                if (hGroupObject == IntPtr.Zero)
                    throw new Win32Exception();

                //Allow processes that need to break away from the job from doing so. For these cases, we use
                //a snapshot of the processes on the system to catch the remaining processes and kill them.
                SetProcessGroupLimits(hGroupObject, JOB_OBJECT_LIMIT_BREAKAWAY_OK);

                if (!AssignProcessToJobObject(hGroupObject, process.Handle))
                    throw new Win32Exception();
            }
            catch
            {
                //Ensure we don't hold on to a resource if an exception is thrown
                if (hGroupObject != IntPtr.Zero)
                {
                    TerminateJobObject(hGroupObject, 1);
                    CloseHandle(hGroupObject);
                }
                throw;
            }

            return (long) hGroupObject;
        }

        public static void KillProcessGroup(IntPtr hGroupObject)
        {           
            int exitCode = 1;   //Assume failure since processes should have exited properly            

            try
            {
                if (!TerminateJobObject(hGroupObject, exitCode))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                if (hGroupObject != IntPtr.Zero)
                {
                    CloseHandle(hGroupObject);
                }
            }
        }

        /// <summary>
        /// Adds a process to an existing group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="processId"></param>
        public static void AddProcessToProcessGroup(IntPtr hGroupObject, int processId)
        {
            Process process;
            try
            {
                process = Process.GetProcessById(processId);
            }
            catch (ArgumentException)
            {
                //If the process has exited do nothing
                return;
            }

            //If the process has exited do nothing
            if (process.HasExited)
                return;

            if (!AssignProcessToJobObject(hGroupObject, process.Handle))
            {
                Win32Exception exception = new Win32Exception();

                //If the process is already part of the ProcessGroup we will get access denied
                //In this case continue, otherwise, throw the exception
                if (exception.NativeErrorCode != ERROR_ACCESS_DENIED)
                    throw exception;
            }
        }

        #endregion

        #region Private Members

        private static void SetProcessGroupLimits(IntPtr hGroupObject, uint limitFlags)
        {
            IntPtr hLimitInfo = IntPtr.Zero;

            JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();

            try
            {
                //HACK: For some reason, the SetInformationJobObject API returns a failure if you use BASIC_LIMIT structure
                //instead of the EXTENDED limit structure, even if we only care about the BASIC one.

                int returnLength = 0;
                int limitInfoSize = Marshal.SizeOf(limitInfo);
                hLimitInfo = Marshal.AllocHGlobal(limitInfoSize);

                //Retrieve the current limit info
                if (!QueryInformationJobObject(hGroupObject, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, hLimitInfo, limitInfoSize, ref returnLength))
                    throw new Win32Exception();

                //Marshall the data between managed and unmanaged memory
                Marshal.PtrToStructure(hLimitInfo, limitInfo);
                limitInfo.BasicLimitInformation.LimitFlags |= limitFlags;
                Marshal.StructureToPtr(limitInfo, hLimitInfo, false);

                if (!SetInformationJobObject(hGroupObject, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, hLimitInfo, limitInfoSize))
                    throw new Win32Exception();
            }
            finally
            {
                if (hLimitInfo != IntPtr.Zero)
                    Marshal.FreeHGlobal(hLimitInfo);
            }
        }

        #endregion

        #region Unmanaged Interop

        private const int SYNCHRONIZE = 0x00100000;
        private const uint JOB_OBJECT_ASSIGN_PROCESS = 0x0001;
        private const uint JOB_OBJECT_SET_ATTRIBUTES = 0x0002;
        private const uint JOB_OBJECT_QUERY = 0x0004;
        private const uint JOB_OBJECT_TERMINATE = 0x0008;
        private const uint JOB_OBJECT_SET_SECURITY_ATTRIBUTES = 0x0010;
        private const uint JOB_OBJECT_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1F;
        private const uint JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x00000800;
        private const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateJobObject(/*[In] ref ProcessHelper.SECURITY_ATTRIBUTES*/ IntPtr jobAttributes, string lpName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static IntPtr OpenJobObject(UInt32 dwDesiredAccess, bool bInheritHandles, string groupName);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS JobObjectInfoClass, IntPtr lpJobObjectInfo, int cbJobObjectInfoLength);        

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool QueryInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS JobObjectInfoClass, IntPtr lpJobObjectInfo, int cbJobObjectInfoLength, ref int lpReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateJobObject(IntPtr hJob, int exitCode);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">Handle to an open object</param>
        /// <returns>Nonzero indicates success. Zero indicates failure. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);


        private enum JOBOBJECTINFOCLASS
        {
            JobObjectBasicAccountingInformation = 1,
            JobObjectBasicLimitInformation,
            JobObjectBasicProcessIdList,
            JobObjectBasicUIRestrictions,
            JobObjectSecurityLimitInformation,
            JobObjectEndOfJobTimeInformation,
            JobObjectAssociateCompletionPortInformation,
            JobObjectBasicAndIoAccountingInformation,
            JobObjectExtendedLimitInformation,
            JobObjectJobSetInformation,
            MaxJobObjectInfoClass,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            internal UInt64 ReadOperationCount;
            internal UInt64 WriteOperationCount;
            internal UInt64 OtherOperationCount;
            internal UInt64 ReadTransferCount;
            internal UInt64 WriteTransferCount;
            internal UInt64 OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
            {
            }
            internal JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            internal IO_COUNTERS IoInfo;
            internal IntPtr ProcessMemoryLimit;    //Using IntPtr here for x64 support
            internal IntPtr JobMemoryLimit;
            internal IntPtr PeakProcessMemoryUsed;
            internal IntPtr PeakJobMemoryUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION()
            {
            }
            internal UInt64 PerProcessUserTimeLimit;
            internal UInt64 PerJobUserTimeLimit;
            internal UInt32 LimitFlags;
            internal IntPtr MinimumWorkingSetSize; //This is a SIZE_T type, which is platform specific
            internal IntPtr MaximumWorkingSetSize; //This is a SIZE_T type, which is platform specific
            internal UInt32 ActiveProcessLimit; 
            internal IntPtr Affinity;
            internal UInt32 PriorityClass;
            internal UInt32 SchedulingClass;
        } 

        #endregion
    }
}
