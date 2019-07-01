// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Threading;
using System.Timers;
using System.Windows.Interop;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherWin32ThreadAction : SimpleDiscoverableAction
    {
        #region Public Members

        public TimeSpan TimeOut { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Thread thread = new Thread(new ParameterizedThreadStart(Win32WorkerThread));
            thread.Name = "Ownership Thread Win32";

            thread.Start(TimeOut);
        }

        #endregion

        #region Private Members

        private static void Win32WorkerThread(object o)
        {
            TimeSpan timeSpan = (TimeSpan)o;

            OwnershipTimerInternal timer = new OwnershipTimerInternal(timeSpan);

            Win32GenericMessagePump.Run();
        }

        private class OwnershipTimerInternal
        {
            public OwnershipTimerInternal(TimeSpan timeout)
            {
                timer = new System.Timers.Timer();
                win32Obj = Win32GenericMessagePump.Current;
                timer.Elapsed += new ElapsedEventHandler(TimeoutCallback);
                timer.Interval = timeout.TotalMilliseconds;
                timer.Start();
            }

            private void TimeoutCallback(Object sender, ElapsedEventArgs e)
            {
                timer.Stop();
                win32Obj.Stop();
            }

            private Win32GenericMessagePumpObj win32Obj;
            private System.Timers.Timer timer;
        }

        private class Win32GenericMessagePump
        {
            [ThreadStatic]
            public static int Count = 0;

            public static Win32GenericMessagePumpObj Current
            {
                get
                {
                    return new Win32GenericMessagePumpObj();
                }
            }

            public static void Run()
            {
                MSG msg = new MSG();
                try
                {
                    Count++;
                    while (NativeMethods.GetMessage(ref msg, IntPtr.Zero, 0, 0) != 0)
                    {
                        NativeMethods.TranslateMessage(ref msg);
                        NativeMethods.DispatchMessage(ref msg);
                        msg = new MSG();
                    }
                }
                finally
                {
                    Count--;
                }
            }
        }

        private class Win32GenericMessagePumpObj
        {
            internal Win32GenericMessagePumpObj()
            {
                threadId = NativeMethods.GetCurrentThreadId();
            }

            public void Stop()
            {
                if (threadId != 0)
                {
                    NativeMethods.PostThreadMessage(threadId, NativeConstants.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                }
            }

            private int threadId = 0;
        }

        #endregion
    }
}

