// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//      Helper routines for doing cross-process SendMessage to Common Controls

using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    static class XSendMessage
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Retrieves a string when the reference to a string is embedded as a field
        // within a structure. The lParam to the message is a reference to the struct
        //
        // The parameters are: a pointer the struct and its size.
        //                     a pointer to the pointer to the string to retrieve.
        //                     the max size for the string.
        // Param "hwnd" the Window Handle
        // Param "uMsg" the Windows Message
        // Param "wParam" the Windows wParam
        // Param "lParam" a pointer to a struct allocated on the stack
        // Param "cbSize" the size of the structure
        // Param "pszText" the address of a pointer to a string located with the structure referenced by the lParam
        // Param "maxLength" the size of the string
        internal static string GetTextWithinStructure(IntPtr hwnd, int uMsg, IntPtr wParam, IntPtr lParam, int cbSize, IntPtr pszText, int maxLength)
        {
            return GetTextWithinStructureRemoteBitness(
                    hwnd, uMsg, wParam, lParam, cbSize, pszText, maxLength, ProcessorTypes.ProcessorUndef, false);
        }

        internal static unsafe string GetTextWithinStructure(IntPtr hwnd, int uMsg, IntPtr wParam, IntPtr lParam, int cbSize, IntPtr pszText, int maxLength, bool ignoreSendResult)
        {
            return GetTextWithinStructureRemoteBitness(
                    hwnd, uMsg, wParam, lParam, cbSize, pszText, maxLength, ProcessorTypes.ProcessorUndef, ignoreSendResult);
        }

        // Retrieves a string when the reference to a string is embedded as a field
        // within a structure. The lParam to the message is a reference to the struct
        //
        // The parameters are: a pointer the struct and its size.
        //                     a pointer to the pointer to the string to retrieve.
        //                     the max size for the string.
        // Param "hwnd" the Window Handle
        // Param "uMsg" the Windows Message
        // Param "wParam" the Windows wParam
        // Param "lParam" a pointer to a struct allocated on the stack
        // Param "cbSize" the size of the structure
        // Param "pszText" the address of a pointer to a string located with the structure referenced by the lParam
        // Param "maxLength" the size of the string
        // Param "remoteBitness" the bitness of the pointer contained within pszText
        internal static unsafe string GetTextWithinStructureRemoteBitness(
            IntPtr hwnd, int uMsg, IntPtr wParam, IntPtr lParam, int cbSize,
            IntPtr pszText, int maxLength, ProcessorTypes remoteBitness, bool ignoreSendResult)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                using (RemoteMemoryBlock rmem = new RemoteMemoryBlock(cbSize + (maxLength + 1) * sizeof(char), hProcess))
                {
                    // Allocate the space for the object as well as the string
                    // Ensure proper allocation
                    if (rmem.IsInvalid)
                    {
                        return "";
                    }

                    // Force the string to be zero terminated

                    IntPtr remoteTextArea = new IntPtr((byte*)rmem.Address.ToPointer() + cbSize);
                    if (remoteBitness == ProcessorTypes.Processor32Bit)
                    {
                        // When the structure will be sent to a 32-bit process,
                        // pszText points to an int, not an IntPtr.
                        // remoteTextArea should be a 32-bit address.
                        System.Diagnostics.Debug.Assert(remoteTextArea.ToInt32() == remoteTextArea.ToInt64());
                        *(int*)((byte*)pszText.ToPointer()) = rmem.Address.ToInt32() + cbSize;
                    }
                    else
                    {
                        *(IntPtr*)((byte*)pszText.ToPointer()) = remoteTextArea;
                    }

                    // Copy the struct to the remote process...
                    rmem.WriteTo(lParam, new IntPtr(cbSize));

                    // Send the message...
                    IntPtr result = Misc.ProxySendMessage(hwnd, uMsg, wParam, rmem.Address);

                    // Nothing, early exit
                    if (!ignoreSendResult && result == IntPtr.Zero)
                    {
                        return "";
                    }

                    // Allocate the buffer for the string
                    char[] achRes = new char[maxLength + 1];

                    // Force the result string not to go past maxLength
                    achRes[maxLength] = '\0';
                    fixed (char* pchRes = achRes)
                    {
                        // Read the string from the common
                        rmem.ReadFrom(new IntPtr((byte*)rmem.Address.ToPointer() + cbSize), new IntPtr(pchRes), new IntPtr(maxLength * sizeof(char)));

                        // Construct the returned string with an explicit length to avoid
                        // a string with an over-allocated buffer, as would occur
                        // if we simply use new string(achRes).
                        int length = 0;
                        for (; achRes[length] != 0 && length < maxLength; length++)
                        {
                        }
                        return new string(achRes, 0, length);
                    }
                }
            }
        }

        internal static void GetProcessTypes(IntPtr hwnd, out ProcessorTypes localBitness, out ProcessorTypes remoteBitness)
        {
            if (IsWOW64Process(IntPtr.Zero))
            {
                // Local process is running in emulation mode on a 64-bit machine

                // Since the local proces is running in emulation mode it is a 32-bit process
                localBitness = ProcessorTypes.Processor32Bit;

                // If the remote process is not running under WOW64 it must be a native 64-bit process.
                remoteBitness = !IsWOW64Process(hwnd) ? ProcessorTypes.Processor64Bit : ProcessorTypes.Processor32Bit;
            }
            else
            {
                // Local process is running in native mode

                // Check the machine bitness.
                if (Marshal.SizeOf(hwnd) == sizeof(int))
                {
                    // The machine bitness is 32-bit.

                    // Both processes are native 32-bit.
                    localBitness = ProcessorTypes.Processor32Bit;
                    remoteBitness = ProcessorTypes.Processor32Bit;
                }
                else
                {
                    // The machine bitness is 64-bit.

                    // The local process is a native 64-bit process.
                    localBitness = ProcessorTypes.Processor64Bit;

                    // If the remote process is not running under WOW64 it must be a native 64-bit process.
                    remoteBitness = !IsWOW64Process(hwnd) ? ProcessorTypes.Processor64Bit : ProcessorTypes.Processor32Bit;
                }
            }
        }

        // Helper override to handle common scenario of having a string lParam
        // Handles bi-directional string marshaling
        internal static bool XSend (IntPtr hwnd, int uMsg, IntPtr wParam, ref string str, int maxLength)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                using (RemoteMemoryBlock rmem = new RemoteMemoryBlock(maxLength * sizeof(char), hProcess))
                {
                    if (rmem.IsInvalid)
                    {
                        return false;
                    }

                    // Send the message...
                    if (Misc.ProxySendMessage(hwnd, uMsg, wParam, rmem.Address) == IntPtr.Zero)
                    {
                        return false;
                    }

                    // Read the string from the remote buffer
                    return rmem.ReadString(out str, maxLength);
                }
            }
        }

        // Overload for the default object copy scenario, assume that the result must be != to zero
        internal static bool XSend (IntPtr hwnd, int uMsg, IntPtr wParam, IntPtr ptrStructure, int cbSize)
        {
            return XSend (hwnd, uMsg, wParam, ptrStructure, cbSize, ErrorValue.Zero);
        }

        // Main method.  It simply copies an unmamaged buffer to the remote process, sends the message, and then
        // copies the remote buffer back to the local unmanaged buffer.
        internal static bool XSend (IntPtr hwnd, int uMsg, IntPtr wParam, IntPtr ptrStructure, int cbSize, ErrorValue errorCode)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                using (RemoteMemoryBlock rmem = new RemoteMemoryBlock(cbSize, hProcess))
                {
                    // Ensure proper allocation
                    if (rmem.IsInvalid)
                    {
                        return false;
                    }

                    // Copy the struct to the remote process...
                    rmem.WriteTo(ptrStructure, new IntPtr(cbSize));

                    // Send the message...
                    IntPtr res = Misc.ProxySendMessage(hwnd, uMsg, wParam, rmem.Address);

                    // check the result
                    if ((errorCode != ErrorValue.NoCheck) && ((errorCode == ErrorValue.Zero && res == IntPtr.Zero) || (errorCode == ErrorValue.NotZero && res != IntPtr.Zero)))
                    {
                        return false;
                    }

                    // Copy returned struct back to local process...
                    rmem.ReadFrom(ptrStructure, new IntPtr(cbSize));
                }
            }

            return true;
        }

        // Overload for the default object copy scenario, assume that the result must be != to zero this assumes the structure is
        // the wParam LVM_GETNEXTITEMINDEX uses this.
        internal static bool XSend (IntPtr hwnd, int uMsg, IntPtr ptrStructure, int lParam, int cbSize)
        {
            return XSend (hwnd, uMsg, ptrStructure, lParam, cbSize, ErrorValue.Zero);
        }

        // Main method.  It simply copies an unmamaged buffer to the remote process, sends the message, and then
        // copies the remote buffer back to the local unmanaged buffer.
        internal static bool XSend (IntPtr hwnd, int uMsg, IntPtr ptrStructure, int lParam, int cbSize, ErrorValue errorCode)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                using (RemoteMemoryBlock rmem = new RemoteMemoryBlock(cbSize, hProcess))
                {
                    // Ensure proper allocation
                    if (rmem.IsInvalid)
                    {
                        return false;
                    }

                    // Copy the struct to the remote process...
                    rmem.WriteTo(ptrStructure, new IntPtr(cbSize));

                    // Send the message...
                    IntPtr res = Misc.ProxySendMessage(hwnd, uMsg, rmem.Address, new IntPtr(lParam));

                    // check the result
                    if ((errorCode != ErrorValue.NoCheck) && ((errorCode == ErrorValue.Zero && res == IntPtr.Zero) || (errorCode == ErrorValue.NotZero && res != IntPtr.Zero)))
                    {
                        return false;
                    }

                    // Copy returned struct back to local process...
                    rmem.ReadFrom(ptrStructure, new IntPtr(cbSize));
                }
            }

            return true;
        }

        // Overload for the default object copy scenario, assume that the result must be != to zero this assumes the structure is
        // the wParam LVM_GETNEXTITEMINDEX uses this.
        internal static bool XSend (IntPtr hwnd, int uMsg, IntPtr ptrStructure1, IntPtr ptrStructure2, int cbSize1, int cbSize2)
        {
            return XSend (hwnd, uMsg, ptrStructure1, ptrStructure2, cbSize1, cbSize2, ErrorValue.Zero);
        }

        // Main method.  It simply copies an unmamaged buffer to the remote process, sends the message, and then
        // copies the remote buffer back to the local unmanaged buffer.
        internal static bool XSend (IntPtr hwnd, int uMsg, IntPtr ptrStructure1, IntPtr ptrStructure2, int cbSize1, int cbSize2, ErrorValue errorCode)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                using (RemoteMemoryBlock rmem1 = new RemoteMemoryBlock(cbSize1, hProcess))
                {
                    // Ensure proper allocation
                    if (rmem1.IsInvalid)
                    {
                        return false;
                    }

                    using (RemoteMemoryBlock rmem2 = new RemoteMemoryBlock(cbSize2, hProcess))
                    {
                        // Ensure proper allocation
                        if (rmem2.IsInvalid)
                        {
                            return false;
                        }

                        // Copy the struct to the remote process...
                        rmem1.WriteTo(ptrStructure1, new IntPtr(cbSize1));
                        rmem2.WriteTo(ptrStructure2, new IntPtr(cbSize2));

                        // Send the message...
                        IntPtr res = Misc.ProxySendMessage(hwnd, uMsg, rmem1.Address, rmem2.Address);

                        // check the result
                        if ((errorCode != ErrorValue.NoCheck) && ((errorCode == ErrorValue.Zero && res == IntPtr.Zero) || (errorCode == ErrorValue.NotZero && res != IntPtr.Zero)))
                        {
                            return false;
                        }

                        // Copy returned struct back to local process...
                        rmem1.ReadFrom(ptrStructure1, new IntPtr(cbSize1));
                        rmem2.ReadFrom(ptrStructure2, new IntPtr(cbSize2));
                    }
                }
            }

            return true;
        }

        // Main method.  It simply copies an unmamaged buffer to the remote process, sends the message, and then
        // copies the remote buffer back to the local unmanaged buffer.
        internal static int XSendGetIndex(IntPtr hwnd, int uMsg, IntPtr wParam, IntPtr ptrStructure, int cbSize)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                using (RemoteMemoryBlock rmem = new RemoteMemoryBlock(cbSize, hProcess))
                {
                    if (rmem.IsInvalid)
                    {
                        throw new OutOfMemoryException();
                    }

                    // Copy the struct to the remote process...
                    rmem.WriteTo(ptrStructure, new IntPtr(cbSize));

                    // Send the message...
                    int res = Misc.ProxySendMessageInt(hwnd, uMsg, wParam, rmem.Address);

                    // Copy returned struct back to local process...
                    rmem.ReadFrom(ptrStructure, new IntPtr(cbSize));

                    return res;
                }
            }
        }


        //------------------------------------------------------
        //
        //  ListView Control Methods that support cross process / cross bitness
        //
        //------------------------------------------------------

        #region ListView Control Methods

        // This overload method is used to get ListView Item text.
        internal static unsafe string GetItemText(IntPtr hwnd, NativeMethods.LVITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETITEMW, IntPtr.Zero, new IntPtr(&item), Marshal.SizeOf(item.GetType()), new IntPtr(&item.pszText), item.cchTextMax);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVITEM_32 item32 = new LVITEM_32(item);

                return GetTextWithinStructureRemoteBitness(hwnd, NativeMethods.LVM_GETITEMW, IntPtr.Zero,
                    new IntPtr(&item32), Marshal.SizeOf(item32.GetType()), new IntPtr(&item32.pszText),
                    item32.cchTextMax, remoteBitness, false);

            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVITEM_64 item64 = new LVITEM_64(item);

                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETITEMW, IntPtr.Zero, new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), new IntPtr(&item64.pszText), item64.cchTextMax);
            }
            return "";
        }

        // This overload method is used to set ListView Item data.
        internal static unsafe bool SetItem(IntPtr hwnd, int index, NativeMethods.LVITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                return XSend(hwnd, NativeMethods.LVM_SETITEMSTATE, new IntPtr(index), new IntPtr(&item), Marshal.SizeOf(item.GetType()));
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVITEM_32 item32 = new LVITEM_32(item);

                return XSend(hwnd, NativeMethods.LVM_SETITEMSTATE, new IntPtr(index), new IntPtr(&item32), Marshal.SizeOf(item32.GetType()));
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVITEM_64 item64 = new LVITEM_64(item);

                return XSend(hwnd, NativeMethods.LVM_SETITEMSTATE, new IntPtr(index), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()));
            }
            return false;
        }

        // This overload method is used to get ListView Item data with LVITEM_V6.  LVITEM_V6 is used to get ListView Group ID.
        internal static unsafe bool GetItem(IntPtr hwnd, ref NativeMethods.LVITEM_V6 item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                fixed (NativeMethods.LVITEM_V6 *pItem = &item)
                {
                    return XSend(hwnd, NativeMethods.LVM_GETITEMW, IntPtr.Zero, new IntPtr(pItem), Marshal.SizeOf(item.GetType()), XSendMessage.ErrorValue.NoCheck);
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVITEM_V6_32 item32 = new LVITEM_V6_32(item);

                bool result = XSend(hwnd, NativeMethods.LVM_GETITEMW, IntPtr.Zero, new IntPtr(&item32), Marshal.SizeOf(item32.GetType()), XSendMessage.ErrorValue.NoCheck);

                if (result)
                {
                    item = (NativeMethods.LVITEM_V6)item32;
                }
                return result;
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVITEM_V6_64 item64 = new LVITEM_V6_64(item);

                bool result = XSend(hwnd, NativeMethods.LVM_GETITEMW, IntPtr.Zero, new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), XSendMessage.ErrorValue.NoCheck);

                if (result)
                {
                    item = (NativeMethods.LVITEM_V6)item64;
                }
                return result;
            }
            return false;
        }

        // This overload method is used to set ListView group data.
        internal static unsafe bool SetGroupInfo(IntPtr hwnd, NativeMethods.LVGROUP group)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                int result = XSendGetIndex(hwnd, NativeMethods.LVM_SETGROUPINFO,
                                new IntPtr(group.iGroupID), new IntPtr(&group), Marshal.SizeOf(group.GetType()));
                return (result == group.iGroupID);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVGROUP_32 group32 = new LVGROUP_32(group);
                int result = XSendGetIndex(hwnd, NativeMethods.LVM_SETGROUPINFO,
                                new IntPtr(group.iGroupID), new IntPtr(&group32), Marshal.SizeOf(group32.GetType()));
                return (result == group.iGroupID);
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVGROUP_64 group64 = new LVGROUP_64(group);
                int result = XSendGetIndex(hwnd, NativeMethods.LVM_SETGROUPINFO,
                                new IntPtr(group.iGroupID), new IntPtr(&group64), Marshal.SizeOf(group64.GetType()));
                return (result == group.iGroupID);
            }
            return false;
        }

        // This overload method is used to get ListView group data.
        internal static unsafe bool GetGroupInfo(IntPtr hwnd, ref NativeMethods.LVGROUP group)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                int result = 0;
                fixed (NativeMethods.LVGROUP* pGroup = &group)
                {
                    result = XSendGetIndex(hwnd, NativeMethods.LVM_GETGROUPINFO,
                                    new IntPtr(group.iGroupID), new IntPtr(pGroup), Marshal.SizeOf(group.GetType()));
                }
                if (result == group.iGroupID)
                {
                    return true;
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVGROUP_32 group32 = new LVGROUP_32(group);
                int result = XSendGetIndex(hwnd, NativeMethods.LVM_GETGROUPINFO,
                                new IntPtr(group.iGroupID), new IntPtr(&group32), Marshal.SizeOf(group32.GetType()));
                if (result == group32.iGroupID)
                {
                    group = (NativeMethods.LVGROUP)group32;
                    return true;
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVGROUP_64 group64 = new LVGROUP_64(group);
                int result = XSendGetIndex(hwnd, NativeMethods.LVM_GETGROUPINFO,
                                new IntPtr(group.iGroupID), new IntPtr(&group64), Marshal.SizeOf(group64.GetType()));
                if (result == group64.iGroupID)
                {
                    group = (NativeMethods.LVGROUP)group64;
                    return true;
                }
            }
            return false;
        }

        // This overload method is used to get ListView group data.
        internal static unsafe bool GetGroupInfo(IntPtr hwnd, ref NativeMethods.LVGROUP_V6  group)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                int result = 0;
                fixed (NativeMethods.LVGROUP_V6* pGroup = &group)
                {
                    result = XSendGetIndex(hwnd, NativeMethods.LVM_GETGROUPINFO,
                                    new IntPtr(group.iGroupID), new IntPtr(pGroup), Marshal.SizeOf(group.GetType()));
                }
                if (result == group.iGroupID)
                {
                    return true;
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVGROUP_V6_32 group32 = new LVGROUP_V6_32(group);
                int result = XSendGetIndex(hwnd, NativeMethods.LVM_GETGROUPINFO,
                                new IntPtr(group.iGroupID), new IntPtr(&group32), Marshal.SizeOf(group32.GetType()));
                if (result == group32.iGroupID)
                {
                    group = (NativeMethods.LVGROUP_V6)group32;
                    return true;
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVGROUP_V6_64 group64 = new LVGROUP_V6_64(group);
                int result = XSendGetIndex(hwnd, NativeMethods.LVM_GETGROUPINFO,
                                new IntPtr(group.iGroupID), new IntPtr(&group64), Marshal.SizeOf(group64.GetType()));
                if (result == group64.iGroupID)
                {
                    group = (NativeMethods.LVGROUP_V6)group64;
                    return true;
                }
            }
            return false;
        }

        // This overload method is used to get ListView Group Item text.
        internal static unsafe string GetItemText(IntPtr hwnd, NativeMethods.LVGROUP item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                // obtain group string (header only)
                if (Environment.OSVersion.Version.Major == 5)
                {
                    return ListView_V6_GetGroupTextOnWinXp(hwnd, item);
                }

                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(item.iGroupID), new IntPtr(&item), Marshal.SizeOf(item.GetType()), new IntPtr(&item.pszHeader), item.cchHeader);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVGROUP_32 item32 = new LVGROUP_32(item);

                // obtain group string (header only)
                if (Environment.OSVersion.Version.Major == 5)
                {
                    return ListView_V6_GetGroupTextOnWinXp(hwnd, item32);
                }

                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(item32.iGroupID), new IntPtr(&item32), Marshal.SizeOf(item32.GetType()), new IntPtr(&item32.pszHeader), item32.cchHeader);
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVGROUP_64 item64 = new LVGROUP_64(item);

                // obtain group string (header only)
                if (Environment.OSVersion.Version.Major == 5)
                {
                    return ListView_V6_GetGroupTextOnWinXp(hwnd, item64);
                }

                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(item64.iGroupID), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), new IntPtr(&item64.pszHeader), item64.cchHeader);
            }
            return "";
        }

        // This overload method is used to get ListView Group Item text.
        internal static unsafe string GetItemText(IntPtr hwnd, NativeMethods.LVGROUP_V6 item, int mask)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            item.mask = mask;
            IntPtr textAddress = IntPtr.Zero;
            int size = 0;

            // these are the only two fields we ask for right now
            if (((mask & NativeMethods.LVGF_HEADER)  == 0) && ((mask & NativeMethods.LVGF_SUBSET) == 0))
                return "";


            if (localBitness == remoteBitness)
            {
                switch (mask)
                {
                    case NativeMethods.LVGF_HEADER:
                        textAddress = new IntPtr(&item.pszHeader);
                        size = item.cchHeader;
                        break;

                    case NativeMethods.LVGF_SUBSET:
                        textAddress = new IntPtr(&item.pszSubsetTitle);
                        size = item.cchSubsetTitle;
                        break;
                }
                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(item.iGroupID), new IntPtr(&item), Marshal.SizeOf(item.GetType()), textAddress, size, true);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                LVGROUP_V6_32 item32 = new LVGROUP_V6_32(item);

                switch (mask)
                {
                    case NativeMethods.LVGF_HEADER:
                        textAddress = new IntPtr(&item32.pszHeader);
                        size = item32.cchHeader;
                        break;

                    case NativeMethods.LVGF_SUBSET:
                        textAddress = new IntPtr(&item32.pszSubsetTitle);
                        size = item32.cchSubsetTitle;
                        break;
                }
                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(item32.iGroupID), new IntPtr(&item32), Marshal.SizeOf(item32.GetType()), textAddress, size, true);
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                LVGROUP_V6_64 item64 = new LVGROUP_V6_64(item);

                switch (mask)
                {
                    case NativeMethods.LVGF_HEADER:
                        textAddress = new IntPtr(&item64.pszHeader);
                        size = item64.cchHeader;
                        break;

                    case NativeMethods.LVGF_SUBSET:
                        textAddress = new IntPtr(&item64.pszSubsetTitle);
                        size = item64.cchSubsetTitle;
                        break;
                }
                return GetTextWithinStructure(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(item64.iGroupID), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), textAddress, size, true);
            }
            return "";
        }

        #endregion

        //------------------------------------------------------
        //
        //  Tab Control Methods that support cross process / cross bitness
        //
        //------------------------------------------------------

        #region Tab Contrl Methods

        // This overload method is used to get Tab Item data.
        internal static unsafe bool GetItem(IntPtr hwnd, int index, ref NativeMethods.TCITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                fixed (NativeMethods.TCITEM* pItem = &item)
                {
                    return XSend(hwnd, NativeMethods.TCM_GETITEMW, new IntPtr(index), new IntPtr(pItem), Marshal.SizeOf(item.GetType()));
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                TCITEM_32 item32 = new TCITEM_32(item);

                bool result = XSend(hwnd, NativeMethods.TCM_GETITEMW, new IntPtr(index), new IntPtr(&item32), Marshal.SizeOf(item32.GetType()));

                if (result)
                {
                    item = (NativeMethods.TCITEM)item32;
                }

                return result;
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                TCITEM_64 item64 = new TCITEM_64(item);

                bool result = XSend(hwnd, NativeMethods.TCM_GETITEMW, new IntPtr(index), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()));

                if (result)
                {
                    item = (NativeMethods.TCITEM)item64;
                }

                return result;
            }
            return false;
        }

        // This overload method is used to get Tab Item text.
        internal static unsafe string GetItemText(IntPtr hwnd, int index, NativeMethods.TCITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                return GetTextWithinStructure(hwnd, NativeMethods.TCM_GETITEMW, new IntPtr(index), new IntPtr(&item), Marshal.SizeOf(item.GetType()), new IntPtr(&item.pszText), item.cchTextMax);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                TCITEM_32 item32 = new TCITEM_32(item);

                return GetTextWithinStructureRemoteBitness(hwnd, NativeMethods.TCM_GETITEMW, new IntPtr(index),
                    new IntPtr(&item32), Marshal.SizeOf(item32.GetType()), new IntPtr(&item32.pszText),
                    item32.cchTextMax, remoteBitness, false);

            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                TCITEM_64 item64 = new TCITEM_64(item);

                return GetTextWithinStructure(hwnd, NativeMethods.TCM_GETITEMW, new IntPtr(index), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), new IntPtr(&item64.pszText), item64.cchTextMax);
            }
            return "";
        }

        #endregion

        //------------------------------------------------------
        //
        //  SysHeader Control Methods that support cross process / cross bitness
        //
        //------------------------------------------------------

        #region SysHeader Control Methods

        // This overload method is used to get SysHeader Item data.
        internal static unsafe bool GetItem(IntPtr hwnd, int index, ref NativeMethods.HDITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                fixed (NativeMethods.HDITEM* pItem = &item)
                {
                    return XSend(hwnd, NativeMethods.HDM_GETITEMW, new IntPtr(index), new IntPtr(pItem), Marshal.SizeOf(item.GetType()));
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                HDITEM_32 item32 = new HDITEM_32(item);

                bool result = XSend(hwnd, NativeMethods.HDM_GETITEMW, new IntPtr(index), new IntPtr(&item32), Marshal.SizeOf(item32.GetType()));

                if (result)
                {
                    item = (NativeMethods.HDITEM)item32;
                }

                return result;
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                HDITEM_64 item64 = new HDITEM_64(item);

                bool result = XSend(hwnd, NativeMethods.HDM_GETITEMW, new IntPtr(index), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()));

                if (result)
                {
                    item = (NativeMethods.HDITEM)item64;
                }

                return result;
            }
            return false;
        }

        // This overload method is used to get SysHeader Item text.
        internal static unsafe string GetItemText(IntPtr hwnd, int index, NativeMethods.HDITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                return GetTextWithinStructure(hwnd, NativeMethods.HDM_GETITEMW, new IntPtr(index), new IntPtr(&item), Marshal.SizeOf(item.GetType()), new IntPtr(&item.pszText), item.cchTextMax);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                HDITEM_32 item32 = new HDITEM_32(item);

                return GetTextWithinStructureRemoteBitness(
                          hwnd, NativeMethods.HDM_GETITEMW, new IntPtr(index), new IntPtr(&item32),
                          Marshal.SizeOf(item32.GetType()), new IntPtr(&item32.pszText), item32.cchTextMax,
                          remoteBitness, false);
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                HDITEM_64 item64 = new HDITEM_64(item);

                return GetTextWithinStructure(hwnd, NativeMethods.HDM_GETITEMW, new IntPtr(index), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), new IntPtr(&item64.pszText), item64.cchTextMax);
            }
            return "";
        }

        #endregion

        //------------------------------------------------------
        //
        //  TreeView Control Methods that support cross process / cross bitness
        //
        //------------------------------------------------------

        #region TreeView Control Methods

        // This overload method is used to get TreeView Item data.
        internal static unsafe bool GetItem(IntPtr hwnd, ref NativeMethods.TVITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                fixed (NativeMethods.TVITEM* pItem = &item)
                {
                    return XSend(hwnd, NativeMethods.TVM_GETITEMW, IntPtr.Zero, new IntPtr(pItem), Marshal.SizeOf(item.GetType()));
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                TVITEM_32 item32 = new TVITEM_32(item);

                bool result = XSend(hwnd, NativeMethods.TVM_GETITEMW, IntPtr.Zero, new IntPtr(&item32), Marshal.SizeOf(item32.GetType()));

                if (result)
                {
                    item = (NativeMethods.TVITEM)item32;
                }

                return result;
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                TVITEM_64 item64 = new TVITEM_64(item);

                bool result = XSend(hwnd, NativeMethods.TVM_GETITEMW, IntPtr.Zero, new IntPtr(&item64), Marshal.SizeOf(item64.GetType()));

                if (result)
                {
                    item = (NativeMethods.TVITEM)item64;
                }

                return result;
            }
            return false;
        }

        // This overload method is used to set TreeView Item data.
        internal static unsafe bool SetItem(IntPtr hwnd, NativeMethods.TVITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                return XSend(hwnd, NativeMethods.TVM_SETITEMW, IntPtr.Zero, new IntPtr(&item), Marshal.SizeOf(item.GetType()));
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                TVITEM_32 item32 = new TVITEM_32(item);

                return XSend(hwnd, NativeMethods.TVM_SETITEMW, IntPtr.Zero, new IntPtr(&item32), Marshal.SizeOf(item32.GetType()));
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                TVITEM_64 item64 = new TVITEM_64(item);

                return XSend(hwnd, NativeMethods.TVM_SETITEMW, IntPtr.Zero, new IntPtr(&item64), Marshal.SizeOf(item64.GetType()));
            }
            return false;
        }

        internal static unsafe IntPtr HitTestTreeView(IntPtr hwnd, int x, int y)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);
            IntPtr hitTestItem = IntPtr.Zero;

            // Convert the coordinates for the point of interest from
            // screen coordinates to window-relative coordinates.
            NativeMethods.Win32Point clientPoint = new NativeMethods.Win32Point(x, y);
            if (Misc.MapWindowPoints(IntPtr.Zero, hwnd, ref clientPoint, 1))
            {
                if (localBitness == remoteBitness)
                {
                    NativeMethods.TVHITTESTINFO hitTestInfo =
                        new NativeMethods.TVHITTESTINFO(clientPoint.x, clientPoint.y, 0);
                    if (XSend(hwnd, NativeMethods.TVM_HITTEST, IntPtr.Zero, new IntPtr(&hitTestInfo),
                        Marshal.SizeOf(hitTestInfo.GetType()), XSendMessage.ErrorValue.Zero))
                    {
                        hitTestItem = hitTestInfo.hItem;
                    }
                }
                else if (remoteBitness == ProcessorTypes.Processor32Bit)
                {
                    TVHITTESTINFO_32 hitTestInfo32 = new TVHITTESTINFO_32(clientPoint.x, clientPoint.y, 0);
                    if (XSend(hwnd, NativeMethods.TVM_HITTEST, IntPtr.Zero, new IntPtr(&hitTestInfo32),
                        Marshal.SizeOf(hitTestInfo32.GetType()), XSendMessage.ErrorValue.Zero))
                    {
                        hitTestItem = new IntPtr(hitTestInfo32.hItem);
                    }
                }
                else if (remoteBitness == ProcessorTypes.Processor64Bit)
                {
                    TVHITTESTINFO_64 hitTestInfo64 = new TVHITTESTINFO_64(clientPoint.x, clientPoint.y, 0);
                    if (XSend(hwnd, NativeMethods.TVM_HITTEST, IntPtr.Zero, new IntPtr(&hitTestInfo64),
                        Marshal.SizeOf(hitTestInfo64.GetType()), XSendMessage.ErrorValue.Zero))
                    {
                        hitTestItem = new IntPtr(hitTestInfo64.hItem);
                    }
                }
            }

            return hitTestItem;
        }

        // This overload method is used to get TreeView Item Text.
        internal static unsafe string GetItemText(IntPtr hwnd, NativeMethods.TVITEM item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                return GetTextWithinStructure(hwnd, NativeMethods.TVM_GETITEMW, IntPtr.Zero, new IntPtr(&item), Marshal.SizeOf(item.GetType()), new IntPtr(&item.pszText), item.cchTextMax);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                TVITEM_32 item32 = new TVITEM_32(item);

                return GetTextWithinStructureRemoteBitness(
                            hwnd, NativeMethods.TVM_GETITEMW, IntPtr.Zero, new IntPtr(&item32),
                            Marshal.SizeOf(item32.GetType()), new IntPtr(&item32.pszText), item32.cchTextMax,
                            remoteBitness, false);
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                TVITEM_64 item64 = new TVITEM_64(item);

                return GetTextWithinStructure(hwnd, NativeMethods.TVM_GETITEMW, IntPtr.Zero, new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), new IntPtr(&item64.pszText), item64.cchTextMax);
            }
            return "";
        }

        #endregion

        //------------------------------------------------------
        //
        //  ToolBar Control Methods that support cross process / cross bitness
        //
        //------------------------------------------------------

        #region ToolBar Control Methods

        // This overload method is used to get Toolbar Button Item data.
        internal static unsafe bool GetItem(IntPtr hwnd, int index, ref NativeMethods.TBBUTTON item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                fixed (NativeMethods.TBBUTTON* pItem = &item)
                {
                    return XSend(hwnd, NativeMethods.TB_GETBUTTON, new IntPtr(index), new IntPtr(pItem), Marshal.SizeOf(item.GetType()), ErrorValue.Zero);
                }
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                TBBUTTON_32 item32 = new TBBUTTON_32(item);

                bool result = XSend(hwnd, NativeMethods.TB_GETBUTTON, new IntPtr(index), new IntPtr(&item32), Marshal.SizeOf(item32.GetType()), ErrorValue.Zero);

                if (result)
                {
                    item = (NativeMethods.TBBUTTON)item32;
                }

                return result;
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                TBBUTTON_64 item64 = new TBBUTTON_64(item);

                bool result = XSend(hwnd, NativeMethods.TB_GETBUTTON, new IntPtr(index), new IntPtr(&item64), Marshal.SizeOf(item64.GetType()), ErrorValue.Zero);

                if (result)
                {
                    item = (NativeMethods.TBBUTTON)item64;
                }

                return result;
            }
            return false;
        }

        #endregion

        //------------------------------------------------------
        //
        //  ToolTip Control Methods that support cross process / cross bitness
        //
        //------------------------------------------------------

        #region ToolTip Control Methods

        internal static unsafe string GetItemText(IntPtr hwnd, NativeMethods.TOOLINFO item)
        {
            ProcessorTypes localBitness;
            ProcessorTypes remoteBitness;
            GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            // On Vista, TTM_GETTEXT accepts the max length of the text buffer in wParam
            // in order to prevent text buffer overflow.
            // The value of wParam will be ignored on XP.
            const int maxTextLength = 128;
            IntPtr wParam = new IntPtr(maxTextLength);

            if (localBitness == remoteBitness)
            {
                return GetTextWithinStructure(
                    hwnd, NativeMethods.TTM_GETTEXT, wParam, new IntPtr(&item),
                    Marshal.SizeOf(item.GetType()), new IntPtr(&item.pszText), maxTextLength, true);
            }
            else if (remoteBitness == ProcessorTypes.Processor32Bit)
            {
                TOOLINFO_32 item32 = new TOOLINFO_32(item);

                return GetTextWithinStructureRemoteBitness(
                    hwnd, NativeMethods.TTM_GETTEXT, wParam, new IntPtr(&item32),
                    Marshal.SizeOf(item32.GetType()), new IntPtr(&item32.pszText), maxTextLength,
                    ProcessorTypes.Processor32Bit, true);
            }
            else if (remoteBitness == ProcessorTypes.Processor64Bit)
            {
                TOOLINFO_64 item64 = new TOOLINFO_64(item);

                return GetTextWithinStructure(
                    hwnd, NativeMethods.TTM_GETTEXT, wParam, new IntPtr(&item64),
                    Marshal.SizeOf(item64.GetType()), new IntPtr(&item64.pszText), maxTextLength, true);
            }

            return "";
        }

        #endregion

        //------------------------------------------------------
        //
        //  Generic Control Methods that support cross process / cross bitness
        //
        //------------------------------------------------------

        #region Generic Control Methods

        // A generic GetItemRect that sends a message to a control then reads memory out of the controls process.
        internal static Rect GetItemRect(IntPtr hwnd, int msg, int index)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                NativeMethods.Win32Rect rectW32 = NativeMethods.Win32Rect.Empty;
                int cMem = Marshal.SizeOf(rectW32.GetType());

                using (RemoteMemoryBlock remoteMem = new RemoteMemoryBlock(cMem, hProcess))
                {
                    // Check if RemoteMmeoryBlock.Allocate returns null.
                    if (remoteMem.IsInvalid)
                    {
                        return Rect.Empty;
                    }

                    unsafe
                    {
                        IntPtr localRectStart = new IntPtr(&rectW32.left);

                        remoteMem.WriteTo(localRectStart, new IntPtr(cMem));

                        if (Misc.ProxySendMessageInt(hwnd, msg, new IntPtr(index), remoteMem.Address) != 0)
                        {
                            remoteMem.ReadFrom(localRectStart, new IntPtr(cMem));
                            Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rectW32, 2);

                            // Errors in RTL handling are sometimes caused by a failure to clearly define
                            // class responsibility for normalizing the rectangles.  If a class provides a
                            // normalized rectangle that is subsequently normalized by a caller, Win32Rect
                            // will convert the boundaries to EmptyRect.  This is caused by the test in
                            // Win32Rect.IsEmpty, which returns true if (left >= right || top >= bottom).
                            // For various reasons some controls provide normalized rectangles and some
                            // controls do not, so the calling class needs to selectively normalize based
                            // on known behaviors, version information, or other criteria.
                            // CommonXSendMessage is an intermediary, consumed by multiple classes,
                            // and normalizing here introduces some unwarranted complexity into the
                            // responsibility contract.
                            return rectW32.ToRect(false);
                        }
                    }
                }

                return Rect.Empty;
            }

            //ProcessorTypes localBitness;
            //ProcessorTypes remoteBitness;
            //GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            //if (localBitness == remoteBitness)
            //{
            //    return Rect.Empty;
            //}
            //else if (remoteBitness == ProcessorTypes.Processor32Bit)
            //{
                // Convert from 64-bit to 32-bit
            //    return Rect.Empty;
            //}
            //else if (remoteBitness == ProcessorTypes.Processor64Bit)
            //{
                // Convert from 32-bit to 64-bit
            //    return Rect.Empty;
            //}
            //return Rect.Empty;
        }

        // A generic GetItemText that sends a message to a control then reads memory out of the controls process.
        internal static string GetItemText(IntPtr hwnd, int msg, int index, int textLen)
        {
            if (textLen <= 0)
            {
                return "";
            }

            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    // assume that the hwnd was bad
                    throw new ElementNotAvailableException();
                }

                using (RemoteMemoryBlock rmem = new RemoteMemoryBlock((textLen + 1) * sizeof(char), hProcess))
                {
                    // Allocate memory for UNICODE string.
                    if (rmem.IsInvalid)
                    {
                        return "";
                    }

                    if (Misc.ProxySendMessage(hwnd, msg, new IntPtr(index), rmem.Address) != IntPtr.Zero)
                    {
                        string itemText;

                        // Read the string from the remote buffer
                        if (rmem.ReadString(out itemText, (textLen + 1)))
                        {
                            return itemText;
                        }
                    }
                }
            }

            return "";

            //ProcessorTypes localBitness;
            //ProcessorTypes remoteBitness;
            //GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            //if (localBitness == remoteBitness)
            //{
            //    return "";
            //}
            //else if (remoteBitness == ProcessorTypes.Processor32Bit)
            //{
            // Convert from 64-bit to 32-bit
            //    return "";
            //}
            //else if (remoteBitness == ProcessorTypes.Processor64Bit)
            //{
            // Convert from 32-bit to 64-bit
            //    return "";
            //}
            //return "";
        }

        #endregion

        #endregion

        // ------------------------------------------------------
        //
        // Internal Fields
        //
        // ------------------------------------------------------

        #region Internal Fields

        // Values to check against the return call to SendMessage
        // e.g. SendMessage () == ErrorValue.Zero => failed
        internal enum ErrorValue
        {
            Zero,
            NotZero,
            NoCheck
        }

        internal enum ProcessorTypes
        {
            ProcessorUndef,
            Processor32Bit,
            Processor64Bit
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // This method retrieves the group text on WinXP OS
        private static string ListView_V6_GetGroupTextOnWinXp(IntPtr hwnd, NativeMethods.LVGROUP group)
        {
            // Due to the ListView Group bug on WinXP we need to have a special implementation
            // for retrieving a text of the group for WinXP
            // On WinXP the code to give back the header text looks something like that:
            //            if (plvgrp->mask & LVGF_HEADER)
            //            {
            //                plvgrp->pszHeader = pgrp->pszHeader;
            //            }
            // Instead of something along the lines of StringCchCopy(plvgrp->pszHeader, plvgrp->cchHeader, pgrp->pszHeader);
            // Hence after the call to CommCtrl.Common_GetSetText() we will get back an internal buffer pointer
            // and not the text itself (ref string str will be ""). It makes no sense to call CommCtrl.Common_GetSetText()
            // Use XSendMessage to get the internal buffer pointer and than "manually" read the string

            // get internal buffer pointer (group.pszHeader)
            // NOTE: do no check XSendMessage.XSend since, LVM_GETGROUPINFO returns id of the group which can be 0, hence
            // can be treated as failure
            // We will check group.pszHeader to IntPtr.Zero after the call
            unsafe
            {
                XSend(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(group.iGroupID), new IntPtr(&group), group.cbSize, ErrorValue.NoCheck);
            }
            if (group.pszHeader != IntPtr.Zero)
            {
                // Read the string manually...
                // allocate memory from the unmanaged memory
                using (SafeCoTaskMem copyTo = new SafeCoTaskMem(NativeMethods.MAX_PATH))
                {
                    if (!copyTo.IsInvalid)
                    {
                        using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
                        {
                            if (!hProcess.IsInvalid)
                            {
                                IntPtr count;
                                if (Misc.ReadProcessMemory(hProcess, group.pszHeader, copyTo, new IntPtr(NativeMethods.MAX_PATH), out count))
                                {
                                    return copyTo.GetStringAuto();
                                }
                            }
                        }
                    }
                }
            }

            return "";
        }

        private static string ListView_V6_GetGroupTextOnWinXp(IntPtr hwnd, LVGROUP_32 group)
        {
            // Due to the ListView Group bug on WinXP we need to have a special implementation
            // for retrieving a text of the group for WinXP
            // On WinXP the code to give back the header text looks something like that:
            //            if (plvgrp->mask & LVGF_HEADER)
            //            {
            //                plvgrp->pszHeader = pgrp->pszHeader;
            //            }
            // Instead of something along the lines of StringCchCopy(plvgrp->pszHeader, plvgrp->cchHeader, pgrp->pszHeader);
            // Hence after the call to CommCtrl.Common_GetSetText() we will get back an internal buffer pointer
            // and not the text itself (ref string str will be ""). It makes no sense to call CommCtrl.Common_GetSetText()
            // Use XSendMessage to get the internal buffer pointer and than "manually" read the string

            // get internal buffer pointer (group.pszHeader)
            // NOTE: do no check XSendMessage.XSend since, LVM_GETGROUPINFO returns id of the group which can be 0, hence
            // can be treated as failure
            // We will check group.pszHeader to IntPtr.Zero after the call
            unsafe
            {
                XSend(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(group.iGroupID), new IntPtr(&group), group.cbSize, ErrorValue.NoCheck);
            }
            if (group.pszHeader != 0)
            {
                // Read the string manually...
                // allocate memory from the unmanaged memory
                using (SafeCoTaskMem copyTo = new SafeCoTaskMem(NativeMethods.MAX_PATH))
                {
                    if (!copyTo.IsInvalid)
                    {
                        using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
                        {
                            if (!hProcess.IsInvalid)
                            {
                                IntPtr count;
                                if (Misc.ReadProcessMemory(hProcess, new IntPtr(group.pszHeader), copyTo, new IntPtr(NativeMethods.MAX_PATH), out count))
                                {
                                    return copyTo.GetStringAuto();
                                }
                            }
                        }
                    }
                }
            }

            return "";
        }

        private static string ListView_V6_GetGroupTextOnWinXp(IntPtr hwnd, LVGROUP_64 group)
        {
         // Due to the ListView Group bug on WinXP we need to have a special implementation
            // for retrieving a text of the group for WinXP
            // On WinXP the code to give back the header text looks something like that:
            //            if (plvgrp->mask & LVGF_HEADER)
            //            {
            //                plvgrp->pszHeader = pgrp->pszHeader;
            //            }
            // Instead of something along the lines of StringCchCopy(plvgrp->pszHeader, plvgrp->cchHeader, pgrp->pszHeader);
            // Hence after the call to CommCtrl.Common_GetSetText() we will get back an internal buffer pointer
            // and not the text itself (ref string str will be ""). It makes no sense to call CommCtrl.Common_GetSetText()
            // Use XSendMessage to get the internal buffer pointer and than "manually" read the string
            
            // get internal buffer pointer (group.pszHeader)
            // NOTE: do no check XSendMessage.XSend since, LVM_GETGROUPINFO returns id of the group which can be 0, hence
            // can be treated as failure
            // We will check group.pszHeader to IntPtr.Zero after the call
            unsafe
            {
                XSend(hwnd, NativeMethods.LVM_GETGROUPINFO, new IntPtr(group.iGroupID), new IntPtr(&group), group.cbSize, ErrorValue.NoCheck);
            }
            if (group.pszHeader != 0)
            {
                // Read the string manually...
                // allocate memory from the unmanaged memory
                using (SafeCoTaskMem copyTo = new SafeCoTaskMem(NativeMethods.MAX_PATH))
                {
                    if (!copyTo.IsInvalid)
                    {
                        using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
                        {
                            if (!hProcess.IsInvalid)
                            {
                                IntPtr count;
                                if (Misc.ReadProcessMemory(hProcess, new IntPtr(group.pszHeader), copyTo, new IntPtr(NativeMethods.MAX_PATH), out count))
                                {
                                    return copyTo.GetStringAuto();
                                }
                            }
                        }
                    }
                }
            }

            return "";
        }

        // This method will determine if the process is running in 32-bit emulation mode on a 64-bit machine.
        private static bool IsWOW64Process(IntPtr hwnd)
        {
            using (SafeProcessHandle hProcess = new SafeProcessHandle(hwnd))
            {
                if (hProcess.IsInvalid)
                {
                    throw new Win32Exception();
                }

                // Windows XP(major version 5 and minor version 1) and above
                if (Environment.OSVersion.Version.Major > 5 || (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor == 1))
                {
                    try
                    {
                        // IsWow64Process() implemented in Windows XP
                        bool isWOW64Process;

                        if (!Misc.IsWow64Process(hProcess, out isWOW64Process))
                        {
                            // Function failed. Assume not running under WOW64.
                            return false;
                        }

                        return isWOW64Process;
                    }
                    catch (Win32Exception)
                    {
                        // Function failed. Assume not running under WOW64.
                        return false;
                    }
                }
                // Windows 2000 (major version 5)
                else if (Environment.OSVersion.Version.Major == 5)
                {
                    // NtQueryInformationProcess is available for use in Windows 2000 and Windows XP.
                    // It may be altered or unavailable in subsequent versions. Applications should use the alternate functions
                    ulong infoWOW64 = 0;
                    int status = UnsafeNativeMethods.NtQueryInformationProcess(hProcess, UnsafeNativeMethods.ProcessWow64Information, ref infoWOW64, Marshal.SizeOf(typeof(ulong)), null);
                    if (NT_ERROR(status))
                    {
                        // Query failed. Assume not running under WOW64.
                        return false;
                    }
                    return infoWOW64 != 0;
                }
                // Windows 95, Windows 98, Windows Me, or Windows NT (major version 4)
                else
                {
                    // WOW64 was not available in these versions of Windows.
                    return false;
                }
            }
        }

        //
        // Generic test for error on any status value.
        //
        private static bool NT_ERROR(int status)
        {
            return (ulong)(status) >> 30 == 3;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TCITEM_32
        {
            internal int mask;
            internal int dwState;
            internal int dwStateMask;
            internal int pszText;
            internal int cchTextMax;
            internal int iImage;
            internal int lParam;

            // This constructor should only be called with TCITEM is a 64 bit structure
            internal TCITEM_32(NativeMethods.TCITEM item)
            {
                mask = item.mask;
                dwState = item.dwState;
                dwStateMask = item.dwStateMask;
                pszText = 0;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                lParam = unchecked((int)item.lParam);
            }

            // This operator should only be called when TCITEM is a 64 bit structure
            static public explicit operator NativeMethods.TCITEM(TCITEM_32 item)
            {
                NativeMethods.TCITEM nativeItem = new NativeMethods.TCITEM();

                nativeItem.mask = item.mask;
                nativeItem.dwState = item.dwState;
                nativeItem.dwStateMask = item.dwStateMask;
                nativeItem.pszText = new IntPtr(item.pszText);
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.lParam = new IntPtr(item.lParam);

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TCITEM_64
        {
            internal int mask;
            internal int dwState;
            internal int dwStateMask;
            internal int for_alignment;
            internal long pszText;
            internal int cchTextMax;
            internal int iImage;
            internal long lParam;

            // This constructor should only be called with TCITEM is a 32 bit structure
            internal TCITEM_64(NativeMethods.TCITEM item)
            {
                mask = item.mask;
                dwState = item.dwState;
                dwStateMask = item.dwStateMask;
                for_alignment = 0;
                pszText = (long)item.pszText;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                lParam = (long)item.lParam;
            }

            // This operator should only be called when TCITEM is a 32 bit structure
            static public explicit operator NativeMethods.TCITEM(TCITEM_64 item)
            {
                NativeMethods.TCITEM nativeItem = new NativeMethods.TCITEM();

                nativeItem.mask = item.mask;
                nativeItem.dwState = item.dwState;
                nativeItem.dwStateMask = item.dwStateMask;
                nativeItem.pszText = IntPtr.Zero;
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.lParam = new IntPtr(unchecked((int)item.lParam));

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct HDITEM_32
        {
            internal uint mask;
            internal int cxy;
            internal int pszText;
            internal int hbm;
            internal int cchTextMax;
            internal int fmt;
            internal int lParam;
            internal int iImage;
            internal int iOrder;
            internal uint type;
            internal int pvFilter;

            // This constructor should only be called with HDITEM is a 64 bit structure
            internal HDITEM_32(NativeMethods.HDITEM item)
            {
                mask = item.mask;
                cxy = item.cxy;
                pszText = 0;
                hbm = 0;
                cchTextMax = item.cchTextMax;
                fmt = item.fmt;
                lParam = unchecked((int)item.lParam);
                iImage = item.iImage;
                iOrder = item.iOrder;
                type = item.type;
                pvFilter = 0;
            }

            // This operator should only be called when HDITEM is a 64 bit structure
            static public explicit operator NativeMethods.HDITEM(HDITEM_32 item)
            {
                NativeMethods.HDITEM nativeItem = new NativeMethods.HDITEM();

                nativeItem.mask = item.mask;
                nativeItem.cxy = item.cxy;
                nativeItem.pszText = new IntPtr(item.pszText);
                nativeItem.hbm = new IntPtr(item.hbm);
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.fmt = item.fmt;
                nativeItem.lParam = new IntPtr(item.lParam);
                nativeItem.iImage = item.iImage;
                nativeItem.iOrder = item.iOrder;
                nativeItem.type = item.type;
                nativeItem.pvFilter = new IntPtr(item.pvFilter);

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct HDITEM_64
        {
            internal uint mask;
            internal int cxy;
            internal long pszText;
            internal long hbm;
            internal int cchTextMax;
            internal int fmt;
            internal long lParam;
            internal int iImage;
            internal int iOrder;
            internal uint type;
            internal long pvFilter;

            // This constructor should only be called with HDITEM is a 32 bit structure
            internal HDITEM_64(NativeMethods.HDITEM item)
            {
                mask = item.mask;
                cxy = item.cxy;
                pszText = (long)item.pszText;
                hbm = (long)item.hbm;
                cchTextMax = item.cchTextMax;
                fmt = item.fmt;
                lParam = (long)item.lParam;
                iImage = item.iImage;
                iOrder = item.iOrder;
                type = item.type;
                pvFilter = (long)item.pvFilter;
            }

            // This operator should only be called when HDITEM is a 32 bit structure
            static public explicit operator NativeMethods.HDITEM(HDITEM_64 item)
            {
                NativeMethods.HDITEM nativeItem = new NativeMethods.HDITEM();

                nativeItem.mask = item.mask;
                nativeItem.cxy = item.cxy;
                nativeItem.pszText = IntPtr.Zero;
                nativeItem.hbm = IntPtr.Zero;
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.fmt = item.fmt;
                nativeItem.lParam = new IntPtr(unchecked((int)item.lParam));
                nativeItem.iImage = item.iImage;
                nativeItem.iOrder = item.iOrder;
                nativeItem.type = item.type;
                nativeItem.pvFilter = IntPtr.Zero;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVITEM_32
        {
            internal int mask;
            internal int iItem;
            internal int iSubItem;
            internal int state;
            internal int stateMask;
            internal int pszText;
            internal int cchTextMax;
            internal int iImage;
            internal int lParam;
            internal int iIndent;

            // This constructor should only be called with LVITEM is a 64 bit structure
            internal LVITEM_32(NativeMethods.LVITEM item)
            {
                mask = item.mask;
                iItem = item.iItem;
                iSubItem = item.iSubItem;
                state = item.state;
                stateMask = item.stateMask;
                pszText = 0;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                lParam = unchecked((int)item.lParam);
                iIndent = item.iIndent;
            }

            // This operator should only be called when LVITEM is a 64 bit structure
            static public explicit operator NativeMethods.LVITEM(LVITEM_32 item)
            {
                NativeMethods.LVITEM nativeItem = new NativeMethods.LVITEM();

                nativeItem.mask = item.mask;
                nativeItem.iItem = item.iItem;
                nativeItem.iSubItem = item.iSubItem;
                nativeItem.state = item.state;
                nativeItem.stateMask = item.stateMask;
                nativeItem.pszText = new IntPtr(item.pszText);
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.lParam = new IntPtr(item.lParam);
                nativeItem.iIndent = item.iIndent;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVITEM_64
        {
            internal int mask;
            internal int iItem;
            internal int iSubItem;
            internal int state;
            internal int stateMask;
            internal int for_alignment;
            internal long pszText;
            internal int cchTextMax;
            internal int iImage;
            internal long lParam;
            internal int iIndent;

            // This constructor should only be called with LVITEM is a 32 bit structure
            internal LVITEM_64(NativeMethods.LVITEM item)
            {
                mask = item.mask;
                iItem = item.iItem;
                iSubItem = item.iSubItem;
                state = item.state;
                stateMask = item.stateMask;
                for_alignment = 0;
                pszText = (long)item.pszText;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                lParam = (long)item.lParam;
                iIndent = item.iIndent;
            }

            // This operator should only be called when LVITEM is a 32 bit structure
            static public explicit operator NativeMethods.LVITEM(LVITEM_64 item)
            {
                NativeMethods.LVITEM nativeItem = new NativeMethods.LVITEM();

                nativeItem.mask = item.mask;
                nativeItem.iItem = item.iItem;
                nativeItem.iSubItem = item.iSubItem;
                nativeItem.state = item.state;
                nativeItem.stateMask = item.stateMask;
                nativeItem.pszText = IntPtr.Zero;
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.lParam = new IntPtr(unchecked((int)item.lParam));
                nativeItem.iIndent = item.iIndent;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVITEM_V6_32
        {
            internal uint mask;
            internal int iItem;
            internal int iSubItem;
            internal int state;
            internal int stateMask;
            internal int pszText;
            internal int cchTextMax;
            internal int iImage;
            internal int lParam;
            internal int iIndent;
            internal int iGroupID;
            internal int cColumns;
            internal int puColumns;

            // This constructor should only be called with LVITEM_V6 is a 64 bit structure
            internal LVITEM_V6_32(NativeMethods.LVITEM_V6 item)
            {
                mask = item.mask;
                iItem = item.iItem;
                iSubItem = item.iSubItem;
                state = item.state;
                stateMask = item.stateMask;
                pszText = 0;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                lParam = unchecked((int)item.lParam);
                iIndent = item.iIndent;
                iGroupID = item.iGroupID;
                cColumns = item.cColumns;
                puColumns = 0;
            }

            // This operator should only be called when LVITEM_V6 is a 64 bit structure
            static public explicit operator NativeMethods.LVITEM_V6(LVITEM_V6_32 item)
            {
                NativeMethods.LVITEM_V6 nativeItem = new NativeMethods.LVITEM_V6();

                nativeItem.mask = item.mask;
                nativeItem.iItem = item.iItem;
                nativeItem.iSubItem = item.iSubItem;
                nativeItem.state = item.state;
                nativeItem.stateMask = item.stateMask;
                nativeItem.pszText = new IntPtr(item.pszText);
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.lParam = new IntPtr(item.lParam);
                nativeItem.iIndent = item.iIndent;
                nativeItem.iGroupID = item.iGroupID;
                nativeItem.cColumns = item.cColumns;
                nativeItem.puColumns = new IntPtr(item.puColumns);

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVITEM_V6_64
        {
            internal uint mask;
            internal int iItem;
            internal int iSubItem;
            internal int state;
            internal int stateMask;
            internal int for_alignment;
            internal long pszText;
            internal int cchTextMax;
            internal int iImage;
            internal long lParam;
            internal int iIndent;
            internal int iGroupID;
            internal int cColumns;
            internal int for_alignment_2;
            internal long puColumns;

            // This constructor should only be called with LVITEM_V6 is a 32 bit structure
            internal LVITEM_V6_64(NativeMethods.LVITEM_V6 item)
            {
                mask = item.mask;
                iItem = item.iItem;
                iSubItem = item.iSubItem;
                state = item.state;
                stateMask = item.stateMask;
                for_alignment = 0;
                pszText = (long)item.pszText;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                lParam = (long)item.lParam;
                iIndent = item.iIndent;
                iGroupID = item.iGroupID;
                cColumns = item.cColumns;
                for_alignment_2 = 0;
                puColumns = (long)item.puColumns;
            }

            // This operator should only be called when LVITEM_V6 is a 32 bit structure
            static public explicit operator NativeMethods.LVITEM_V6(LVITEM_V6_64 item)
            {
                NativeMethods.LVITEM_V6 nativeItem = new NativeMethods.LVITEM_V6();

                nativeItem.mask = item.mask;
                nativeItem.iItem = item.iItem;
                nativeItem.iSubItem = item.iSubItem;
                nativeItem.state = item.state;
                nativeItem.stateMask = item.stateMask;
                nativeItem.pszText = IntPtr.Zero;
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.lParam = new IntPtr(unchecked((int)item.lParam));
                nativeItem.iIndent = item.iIndent;
                nativeItem.iGroupID = item.iGroupID;
                nativeItem.cColumns = item.cColumns;
                nativeItem.puColumns = IntPtr.Zero;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVGROUP_32
        {
            internal int cbSize;
            internal int mask;
            internal int pszHeader;
            internal int cchHeader;
            internal int pszFooter;
            internal int cchFooter;
            internal int iGroupID;
            internal int stateMask;
            internal int state;
            internal int align;

            // This constructor should only be called with LVGROUP is a 64 bit structure
            internal LVGROUP_32(NativeMethods.LVGROUP item)
            {
                cbSize = Marshal.SizeOf(typeof(LVGROUP_32));
                mask = item.mask;
                pszHeader = 0;
                cchHeader = item.cchHeader;
                pszFooter = 0;
                cchFooter = item.cchFooter;
                iGroupID = item.iGroupID;
                stateMask = item.stateMask;
                state = item.state;
                align = item.align;
            }

            // This operator should only be called when LVGROUP is a 64 bit structure
            static public explicit operator NativeMethods.LVGROUP(LVGROUP_32 item)
            {
                NativeMethods.LVGROUP nativeItem = new NativeMethods.LVGROUP();

                nativeItem.cbSize = Marshal.SizeOf(typeof(NativeMethods.LVGROUP));
                nativeItem.mask = item.mask;
                nativeItem.pszHeader = new IntPtr(item.pszHeader);
                nativeItem.cchHeader = item.cchHeader;
                nativeItem.pszFooter = new IntPtr(item.pszFooter);
                nativeItem.cchFooter = item.cchFooter;
                nativeItem.iGroupID = item.iGroupID;
                nativeItem.stateMask = item.stateMask;
                nativeItem.state = item.state;
                nativeItem.align = item.align;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVGROUP_V6_32
        {
            internal int cbSize;
            internal int mask;
            internal int pszHeader;
            internal int cchHeader;
            internal int pszFooter;
            internal int cchFooter;
            internal int iGroupID;
            internal int stateMask;
            internal int state;
            internal int align;

            // new stuff for v6
            internal IntPtr pszSubtitle;
            internal int cchSubtitle;
            internal IntPtr pszTask;
            internal int cchTask;
            internal IntPtr pszDescriptionTop;
            internal int cchDescriptionTop;
            internal IntPtr pszDescriptionBottom;
            internal int cchDescriptionBottom;
            internal int iTitleImage;
            internal int iExtendedImage;
            internal int iFirstItem;         // Read only
            internal int cItems;             // Read only
            internal IntPtr pszSubsetTitle;     // NULL if group is not subset
            internal int cchSubsetTitle;

            // This constructor should only be called with LVGROUP is a 64 bit structure
            internal LVGROUP_V6_32(NativeMethods.LVGROUP_V6 item)
            {
                cbSize = Marshal.SizeOf(typeof(LVGROUP_V6_32));
                mask = item.mask;
                pszHeader = 0;
                cchHeader = item.cchHeader;
                pszFooter = 0;
                cchFooter = item.cchFooter;
                iGroupID = item.iGroupID;
                stateMask = item.stateMask;
                state = item.state;
                align = item.align;

                // new stuff for v6
                pszSubtitle = item.pszSubtitle;
                cchSubtitle = item.cchSubtitle;
                pszTask = item.pszTask;
                cchTask = item.cchTask;
                pszDescriptionTop = item.pszDescriptionTop;
                cchDescriptionTop = item.cchDescriptionTop;
                pszDescriptionBottom = item.pszDescriptionBottom;
                cchDescriptionBottom = item.cchDescriptionBottom;
                iTitleImage = item.iTitleImage;
                iExtendedImage = item.iExtendedImage;
                iFirstItem = item.iFirstItem;         // Read only
                cItems = item.cItems;             // Read only
                pszSubsetTitle = item.pszSubsetTitle; // NULL if group is not subset
                cchSubsetTitle = item.cchSubsetTitle;
            }

            // This operator should only be called when LVGROUP is a 64 bit structure
            static public explicit operator NativeMethods.LVGROUP_V6(LVGROUP_V6_32 item)
            {
                NativeMethods.LVGROUP_V6 nativeItem = new NativeMethods.LVGROUP_V6();

                nativeItem.cbSize = Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6));
                nativeItem.mask = item.mask;
                nativeItem.pszHeader = new IntPtr(item.pszHeader);
                nativeItem.cchHeader = item.cchHeader;
                nativeItem.pszFooter = new IntPtr(item.pszFooter);
                nativeItem.cchFooter = item.cchFooter;
                nativeItem.iGroupID = item.iGroupID;
                nativeItem.stateMask = item.stateMask;
                nativeItem.state = item.state;
                nativeItem.align = item.align;

                // new stuff for v6
                nativeItem.pszSubtitle = item.pszSubtitle;
                nativeItem.cchSubtitle = item.cchSubtitle;
                nativeItem.pszTask = item.pszTask;
                nativeItem.cchTask = item.cchTask;
                nativeItem.pszDescriptionTop = item.pszDescriptionTop;
                nativeItem.cchDescriptionTop = item.cchDescriptionTop;
                nativeItem.pszDescriptionBottom = item.pszDescriptionBottom;
                nativeItem.cchDescriptionBottom = item.cchDescriptionBottom;
                nativeItem.iTitleImage = item.iTitleImage;
                nativeItem.iExtendedImage = item.iExtendedImage;
                nativeItem.iFirstItem = item.iFirstItem;         // Read only
                nativeItem.cItems = item.cItems;             // Read only
                nativeItem.pszSubsetTitle = item.pszSubsetTitle; // NULL if group is not subset
                nativeItem.cchSubsetTitle = item.cchSubsetTitle;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVGROUP_64
        {
            internal int cbSize;
            internal int mask;
            internal long pszHeader;
            internal int cchHeader;
            internal int for_alignment;
            internal long pszFooter;
            internal int cchFooter;
            internal int iGroupID;
            internal int stateMask;
            internal int state;
            internal int align;

            // This constructor should only be called with LVGROUP is a 32 bit structure
            internal LVGROUP_64(NativeMethods.LVGROUP item)
            {
                cbSize = Marshal.SizeOf(typeof(LVGROUP_64));
                mask = item.mask;
                pszHeader = (long)item.pszHeader;
                cchHeader = item.cchHeader;
                for_alignment = 0;
                pszFooter = (long)item.pszFooter;
                cchFooter = item.cchFooter;
                iGroupID = item.iGroupID;
                stateMask = item.stateMask;
                state = item.state;
                align = item.align;
            }

            // This operator should only be called when LVGROUP is a 32 bit structure
            static public explicit operator NativeMethods.LVGROUP(LVGROUP_64 item)
            {
                NativeMethods.LVGROUP nativeItem = new NativeMethods.LVGROUP();

                nativeItem.cbSize = Marshal.SizeOf(typeof(NativeMethods.LVGROUP));
                nativeItem.mask = item.mask;
                nativeItem.pszHeader = IntPtr.Zero;
                nativeItem.cchHeader = item.cchHeader;
                nativeItem.pszFooter = IntPtr.Zero;
                nativeItem.cchFooter = item.cchFooter;
                nativeItem.iGroupID = item.iGroupID;
                nativeItem.stateMask = item.stateMask;
                nativeItem.state = item.state;
                nativeItem.align = item.align;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVGROUP_V6_64
        {
            internal int cbSize;
            internal int mask;
            internal long pszHeader;
            internal int cchHeader;
            internal int for_alignment;
            internal long pszFooter;
            internal int cchFooter;
            internal int iGroupID;
            internal int stateMask;
            internal int state;
            internal int align;

            // new stuff for v6
            internal long pszSubtitle;
            internal int cchSubtitle;
            internal long pszTask;
            internal int cchTask;
            internal long pszDescriptionTop;
            internal int cchDescriptionTop;
            internal long pszDescriptionBottom;
            internal int cchDescriptionBottom;
            internal int iTitleImage;
            internal int iExtendedImage;
            internal int iFirstItem;         // Read only
            internal int cItems;             // Read only
            internal long pszSubsetTitle;     // NULL if group is not subset
            internal int cchSubsetTitle;

            // This constructor should only be called with LVGROUP is a 32 bit structure
            internal LVGROUP_V6_64(NativeMethods.LVGROUP_V6 item)
            {
                cbSize = Marshal.SizeOf(typeof(LVGROUP_V6_64));
                mask = item.mask;
                pszHeader = (long)item.pszHeader;
                cchHeader = item.cchHeader;
                for_alignment = 0;
                pszFooter = (long)item.pszFooter;
                cchFooter = item.cchFooter;
                iGroupID = item.iGroupID;
                stateMask = item.stateMask;
                state = item.state;
                align = item.align;

                // new stuff for v6
                pszSubtitle = (long)item.pszSubtitle;
                cchSubtitle = item.cchSubtitle;
                pszTask = (long)item.pszTask;
                cchTask = item.cchTask;
                pszDescriptionTop = (long)item.pszDescriptionTop;
                cchDescriptionTop = item.cchDescriptionTop;
                pszDescriptionBottom = (long)item.pszDescriptionBottom;
                cchDescriptionBottom = item.cchDescriptionBottom;
                iTitleImage = item.iTitleImage;
                iExtendedImage = item.iExtendedImage;
                iFirstItem = item.iFirstItem;         // Read only
                cItems = item.cItems;             // Read only
                pszSubsetTitle = (long)item.pszSubsetTitle; // NULL if group is not subset
                cchSubsetTitle = item.cchSubsetTitle;
            }

            // This operator should only be called when LVGROUP is a 32 bit structure
            static public explicit operator NativeMethods.LVGROUP_V6(LVGROUP_V6_64 item)
            {
                NativeMethods.LVGROUP_V6 nativeItem = new NativeMethods.LVGROUP_V6();

                nativeItem.cbSize = Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6));
                nativeItem.mask = item.mask;
                nativeItem.pszHeader = IntPtr.Zero;
                nativeItem.cchHeader = item.cchHeader;
                nativeItem.pszFooter = IntPtr.Zero;
                nativeItem.cchFooter = item.cchFooter;
                nativeItem.iGroupID = item.iGroupID;
                nativeItem.stateMask = item.stateMask;
                nativeItem.state = item.state;
                nativeItem.align = item.align;

                // new stuff for v6
                nativeItem.pszSubtitle = IntPtr.Zero;
                nativeItem.cchSubtitle = item.cchSubtitle;
                nativeItem.pszTask = IntPtr.Zero;
                nativeItem.cchTask = item.cchTask;
                nativeItem.pszDescriptionTop = IntPtr.Zero;
                nativeItem.cchDescriptionTop = item.cchDescriptionTop;
                nativeItem.pszDescriptionBottom = IntPtr.Zero;
                nativeItem.cchDescriptionBottom = item.cchDescriptionBottom;
                nativeItem.iTitleImage = item.iTitleImage;
                nativeItem.iExtendedImage = item.iExtendedImage;
                nativeItem.iFirstItem = item.iFirstItem;         // Read only
                nativeItem.cItems = item.cItems;             // Read only
                nativeItem.pszSubsetTitle = IntPtr.Zero; // NULL if group is not subset
                nativeItem.cchSubsetTitle = item.cchSubsetTitle;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        private struct TBBUTTON_32
        {
            internal int iBitmap;
            internal int idCommand;
            internal byte fsState;
            internal byte fsStyle;
            internal byte bReserved0;
            internal byte bReserved1;
            internal int dwData;
            internal int iString;

            // This constructor should only be called with TBBUTTON is a 64 bit structure
            internal TBBUTTON_32(NativeMethods.TBBUTTON item)
            {
                iBitmap = item.iBitmap;
                idCommand = item.idCommand;
                fsState = item.fsState;
                fsStyle = item.fsStyle;
                bReserved0 = item.bReserved0;
                bReserved1 = item.bReserved1;
                dwData = item.dwData;
                iString = 0;
            }

            // This operator should only be called when TBBUTTON is a 64 bit structure
            static public explicit operator NativeMethods.TBBUTTON(TBBUTTON_32 item)
            {
                NativeMethods.TBBUTTON nativeItem = new NativeMethods.TBBUTTON();

                nativeItem.iBitmap = item.iBitmap;
                nativeItem.idCommand = item.idCommand;
                nativeItem.fsState = item.fsState;
                nativeItem.fsStyle = item.fsStyle;
                nativeItem.bReserved0 = item.bReserved0;
                nativeItem.bReserved1 = item.bReserved1;
                nativeItem.dwData = item.dwData;
                nativeItem.iString = new IntPtr(item.iString);

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        private struct TBBUTTON_64
        {
            internal int iBitmap;
            internal int idCommand;
            internal byte fsState;
            internal byte fsStyle;
            internal byte bReserved0;
            internal byte bReserved1;
            internal int dwData;
            internal int for_alignment;
            internal long iString;

            // This constructor should only be called with TBBUTTON is a 32 bit structure
            internal TBBUTTON_64(NativeMethods.TBBUTTON item)
            {
                iBitmap = item.iBitmap;
                idCommand = item.idCommand;
                fsState = item.fsState;
                fsStyle = item.fsStyle;
                bReserved0 = item.bReserved0;
                bReserved1 = item.bReserved1;
                dwData = item.dwData;
                for_alignment = 0;
                iString = (long)item.iString;
            }

            // This operator should only be called when TBBUTTON is a 32 bit structure
            static public explicit operator NativeMethods.TBBUTTON(TBBUTTON_64 item)
            {
                NativeMethods.TBBUTTON nativeItem = new NativeMethods.TBBUTTON();

                nativeItem.iBitmap = item.iBitmap;
                nativeItem.idCommand = item.idCommand;
                nativeItem.fsState = item.fsState;
                nativeItem.fsStyle = item.fsStyle;
                nativeItem.bReserved0 = item.bReserved0;
                nativeItem.bReserved1 = item.bReserved1;
                nativeItem.dwData = item.dwData;
                nativeItem.iString = IntPtr.Zero;

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TVITEM_32
        {
            internal uint mask;
            internal int hItem;
            internal uint state;
            internal uint stateMask;
            internal int pszText;
            internal int cchTextMax;
            internal int iImage;
            internal int iSelectedImage;
            internal int cChildren;
            internal int lParam;

            // This constructor should only be called with TVITEM is a 64 bit structure
            // but refers to an item in a 32-bit TreeView.
            internal TVITEM_32(NativeMethods.TVITEM item)
            {
                mask = item.mask;

                // Since the high 32-bits of item.hItem are zero,
                // we can force them into the 32-bit hItem in a
                // TVITEM_32.
                hItem = item.hItem.ToInt32();

                state = item.state;
                stateMask = item.stateMask;
                pszText = 0;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                iSelectedImage = item.iSelectedImage;
                cChildren = item.cChildren;
                lParam = unchecked((int)item.lParam);
            }

            // This operator should only be called when TVITEM is a 64 bit structure
            static public explicit operator NativeMethods.TVITEM(TVITEM_32 item)
            {
                NativeMethods.TVITEM nativeItem = new NativeMethods.TVITEM();

                nativeItem.mask = item.mask;
                nativeItem.hItem = new IntPtr(item.hItem);
                nativeItem.state = item.state;
                nativeItem.stateMask = item.stateMask;
                nativeItem.pszText = new IntPtr(item.pszText);
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.iSelectedImage = item.iSelectedImage;
                nativeItem.cChildren = item.cChildren;
                nativeItem.lParam = new IntPtr(item.lParam);

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TVITEM_64
        {
            internal uint mask;
            internal int for_alignment;
            internal long hItem;
            internal uint state;
            internal uint stateMask;
            internal long pszText;
            internal int cchTextMax;
            internal int iImage;
            internal int iSelectedImage;
            internal int cChildren;
            internal long lParam;

            // This constructor should only be called with TVITEM is a 32 bit structure
            internal TVITEM_64(NativeMethods.TVITEM item)
            {
                mask = item.mask;
                for_alignment = 0;
                hItem = (long)item.hItem;
                state = item.state;
                stateMask = item.stateMask;
                pszText = (long)item.pszText;
                cchTextMax = item.cchTextMax;
                iImage = item.iImage;
                iSelectedImage = item.iSelectedImage;
                cChildren = item.cChildren;
                lParam = (long)item.lParam;
            }

            // This operator should only be called when TVITEM is a 32 bit structure
            static public explicit operator NativeMethods.TVITEM(TVITEM_64 item)
            {
                NativeMethods.TVITEM nativeItem = new NativeMethods.TVITEM();

                nativeItem.mask = item.mask;
                nativeItem.hItem = IntPtr.Zero;
                nativeItem.state = item.state;
                nativeItem.stateMask = item.stateMask;
                nativeItem.pszText = IntPtr.Zero;
                nativeItem.cchTextMax = item.cchTextMax;
                nativeItem.iImage = item.iImage;
                nativeItem.iSelectedImage = item.iSelectedImage;
                nativeItem.cChildren = item.cChildren;
                nativeItem.lParam = new IntPtr(unchecked((int)item.lParam));

                return nativeItem;
            }
        }

        [StructLayout (LayoutKind.Sequential)]
        internal struct TVHITTESTINFO_32
        {
            internal NativeMethods.Win32Point pt;
            internal uint flags;
            internal int hItem;

            internal TVHITTESTINFO_32 (int x, int y, uint flags)
            {
                pt.x = x;
                pt.y = y;
                this.flags = flags;
                hItem = 0;
            }

            // This operator should only be called when TVHITTESTINFO is a 64 bit structure
            static public explicit operator NativeMethods.TVHITTESTINFO(TVHITTESTINFO_32 hitTestInfo)
            {
                NativeMethods.TVHITTESTINFO nativeHitTestInfo = new NativeMethods.TVHITTESTINFO();
                nativeHitTestInfo.pt = hitTestInfo.pt;
                nativeHitTestInfo.flags = hitTestInfo.flags;
                nativeHitTestInfo.hItem = new IntPtr(hitTestInfo.hItem);

                return nativeHitTestInfo;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TVHITTESTINFO_64
        {
            internal NativeMethods.Win32Point pt;
            internal uint flags;
            internal long hItem;

            internal TVHITTESTINFO_64 (int x, int y, uint flags)
            {
                pt.x = x;
                pt.y = y;
                this.flags = flags;
                hItem = 0;
            }

            // This operator should only be called when hitTestInfo is a 32 bit structure
            static public explicit operator NativeMethods.TVHITTESTINFO(TVHITTESTINFO_64 hitTestInfo64)
            {
                NativeMethods.TVHITTESTINFO nativeHitTestInfo = new NativeMethods.TVHITTESTINFO();
                nativeHitTestInfo.pt = hitTestInfo64.pt;
                nativeHitTestInfo.flags = hitTestInfo64.flags;
                nativeHitTestInfo.hItem = new IntPtr(hitTestInfo64.hItem);
                return nativeHitTestInfo;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOOLINFO_32
        {
            internal int cbSize;
            internal int uFlags;
            internal int hwnd;
            internal int uId;
            internal NativeMethods.Win32Rect rect;
            internal int hinst;
            internal int pszText;
            internal int lParam;

            // This constructor should only be called with TOOLINFO is a 64 bit structure
            internal TOOLINFO_32(NativeMethods.TOOLINFO item)
            {
                cbSize = Marshal.SizeOf(typeof(TOOLINFO_32));
                uFlags = item.uFlags;
                hwnd = item.hwnd.ToInt32();
                uId = item.uId;
                rect = item.rect;
                hinst = unchecked((int)item.hinst);
                pszText = 0;
                lParam = unchecked((int)item.lParam);
            }

            // This operator should only be called when TOOLINFO is a 64 bit structure
            static public explicit operator NativeMethods.TOOLINFO(TOOLINFO_32 item)
            {
                NativeMethods.TOOLINFO nativeItem = new NativeMethods.TOOLINFO();

                nativeItem.cbSize = Marshal.SizeOf(typeof(NativeMethods.TOOLINFO));
                nativeItem.uFlags = item.uFlags;
                nativeItem.hwnd = new IntPtr(item.hwnd);
                nativeItem.uId = item.uId;
                nativeItem.rect = item.rect;
                nativeItem.hinst = new IntPtr(item.hinst);
                nativeItem.pszText = new IntPtr(item.pszText);
                nativeItem.lParam = new IntPtr(item.lParam);

                return nativeItem;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOOLINFO_64
        {
            internal int cbSize;
            internal int uFlags;
            internal long hwnd;
            internal int uId;
            internal NativeMethods.Win32Rect rect;
            internal long hinst;
            internal long pszText;
            internal long lParam;

            // This constructor should only be called with LVGROUP is a 32 bit structure
            internal TOOLINFO_64(NativeMethods.TOOLINFO item)
            {
                cbSize = Marshal.SizeOf(typeof(TOOLINFO_64));
                uFlags = item.uFlags;
                hwnd = (long)item.hwnd;
                uId = item.uId;
                rect = item.rect;
                hinst = (long)item.hinst;
                pszText = (long)item.pszText;
                lParam = (long)item.lParam;
            }

            // This operator should only be called when LVGROUP is a 32 bit structure
            static public explicit operator NativeMethods.TOOLINFO(TOOLINFO_64 item)
            {
                NativeMethods.TOOLINFO nativeItem = new NativeMethods.TOOLINFO();

                nativeItem.cbSize = Marshal.SizeOf(typeof(NativeMethods.TOOLINFO));
                nativeItem.uFlags = item.uFlags;
                nativeItem.hwnd = IntPtr.Zero;
                nativeItem.uId = item.uId;
                nativeItem.rect = item.rect;
                nativeItem.hinst = IntPtr.Zero;
                nativeItem.pszText = IntPtr.Zero;
                nativeItem.lParam = new IntPtr(unchecked((int)item.lParam));

                return nativeItem;
            }
        }
        #endregion
    }
}
