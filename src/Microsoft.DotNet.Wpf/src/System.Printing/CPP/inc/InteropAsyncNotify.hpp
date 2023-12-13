// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __INTEROPASYNCNOTIFY_HPP__
#define __INTEROPASYNCNOTIFY_HPP__

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace Win32ApiThunk
{

    private ref class AsyncNotifyNativeMethods abstract
    {
        public:

        [DllImportAttribute("winspool.drv",EntryPoint="RegisterForPrintAsyncNotifications",
                             CharSet=CharSet::Unicode,
                             SetLastError=false, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        UInt32
        RegisterForPrintAsyncNotifications(
            String^,   
            System::Guid^,
            System::Printing::AsyncNotify::UserNotificationFilter,
            PrintAsyncNotifyConversationStyle,
            IPrintAsyncNotifyCallback*,
            IntPtr^);

        [DllImportAttribute("winspool.drv",EntryPoint="UnRegisterForPrintAsyncNotifications",
                             CharSet=CharSet::Unicode,
                             SetLastError=false, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        UInt32
        UnRegisterForPrintAsyncNotifications(
            IntPtr);

    };
    
}
}
}
}
#endif
