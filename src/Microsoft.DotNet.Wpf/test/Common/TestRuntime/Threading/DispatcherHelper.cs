// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Test.Logging;
// TODO-Miguep: uncomment
//using Microsoft.Test.Security.Wrappers;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Threading
{

    /// <summary>
    /// Match the events for Dispatcher Shutdown.
    /// </summary>
    public enum DispatcherShutdownEventsNames
    {
        /// <summary>
        /// Match the events for Dispatcher Shutdown.
        /// </summary>
        ShutdownStarted = 0,

        /// <summary>
        /// Match the events for Dispatcher Shutdown.
        /// </summary>
        ShutdownFinished
    }

    ///<summary>
    /// A delegate to use for dispatcher operations on DispatcherHelper.
    ///</summary>
    public delegate object TestCallback(object obj);

    ///<summary>
    /// This class generalizes the usage for common Dispatcher
    /// operation, such as, BeginInvoke. Shutting the Dispatcher down.
    ///</summary>
    public static class DispatcherHelper
    {
        static DispatcherHelper()
        {
            _voidWin32Message = NativeMethods.RegisterWindowMessage("TestVoidWin32Message");
        }

        ///<summary>
        /// We register a Win32 Message that we know that any WNDPROC is expecting.
        ///</summary>
        static public int VoidWin32Message
        {
            get
            {
                return _voidWin32Message;
            }
        }

        ///<summary>
        /// Generalization for synchronously shutting the current Thread Dispatcher down performing 
        /// a call to InvokeShutDown.    
        ///</summary>
        public static void ShutDown()
        {
            ShutDown(Dispatcher.CurrentDispatcher);
        }

        ///<summary>
        /// Generalization for asynchronously shutting the current Thread Dispatcher down at 
        /// background priority.
        ///</summary>
        public static void ShutDownBackground()
        {
            ShutDownPriorityBackground(Dispatcher.CurrentDispatcher);
        }

        ///<summary>
        /// Generalization for asynchronously shutting the current Thread Dispatcher down at 
        /// SystemIdle priority.
        ///</summary>
        public static void ShutDownPrioritySystemIdle()
        {
            ShutDownPrioritySystemIdle(Dispatcher.CurrentDispatcher);
        }

        ///<summary>
        /// Generalization for synchronously shutting the specified Dispatcher down  performing 
        /// a call to InvokeShutDown.    
        ///</summary>
        /// <param name="dispatcher">
        ///     The dispatcher reference where the InvokeShutDown is performed.
        /// </param>
        public static void ShutDown(Dispatcher dispatcher)
        {
            ShutDown(true, DispatcherPriority.Background, dispatcher);
        }

        ///<summary>
        /// Generalization for asynchronously shutting the specified Thread Dispatcher down at 
        /// normal priority.
        ///</summary>
        /// <param name="dispatcher">
        ///     The dispatcher reference where the BeginInvokeShutDown is performed.
        /// </param>
        public static void ShutDownPriorityNormal(Dispatcher dispatcher)
        {
            ShutDown(false, DispatcherPriority.Normal, dispatcher);
        }

        ///<summary>
        /// Generalization for asynchronously shutting the specified Thread Dispatcher down at 
        /// normal priority.
        ///</summary>
        /// <param name="dispatcher">
        ///     The dispatcher reference where the BeginInvokeShutDown is performed.
        /// </param>
        public static void ShutDownPriorityBackground(Dispatcher dispatcher)
        {
            ShutDown(false, DispatcherPriority.Background, dispatcher);
        }

        ///<summary>
        /// Generalization for asynchronously shutting the specified Thread Dispatcher down at 
        /// SystemIdle priority.
        ///</summary>
        /// <param name="dispatcher">
        ///     The dispatcher reference where the BeginInvokeShutDown is performed.
        /// </param>
        public static void ShutDownPrioritySystemIdle(Dispatcher dispatcher)
        {
            ShutDown(false, DispatcherPriority.SystemIdle, dispatcher);
        }

        ///<summary>
        /// Generalization for shutting the specified Thread Dispatcher down at 
        /// specified priority either sync or async.
        ///</summary>
        /// <param name="synchronous">
        ///     True value, the ShutDown will happen synchronously
        /// </param>
        /// <param name="priority">
        ///     When the ShutDown is set to asynchronous, the value of this parameter
        ///     specified the priority were the ShutDown will be called.
        /// </param>
        /// <param name="dispatcher">
        ///     The dispatcher reference where the ShutDown will be performed.
        /// </param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private static void ShutDown(bool synchronous, DispatcherPriority priority, Dispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }

            if (synchronous)
            {
                dispatcher.InvokeShutdown();
            }
            else
            {
                dispatcher.BeginInvokeShutdown(priority);
            }
        }


        ///<summary>
        /// Generalization for ExitAllFrames
        ///</summary>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static void ExitFrames()
        {
            Dispatcher.ExitAllFrames();
        }

        ///<summary>
        /// This is just a little wrapper around Dispatcher.Run.
        ///</summary>
        public static void RunDispatcher()
        {
            Dispatcher.Run();
        }

        ///<summary>
        /// This is just a little wrapper around Dispatcher.PushFrame.
        ///</summary>
        public static void PushFrame(DispatcherFrame frame)
        {
            Dispatcher.PushFrame(frame);
        }

        ///<summary>
        /// Perform a BeginInvoke call on the current Dispatcher
        /// with Normal Priority, using the method and object as part
        /// of the Dispatcher.BeginInvoke call
        ///</summary>
        /// <param name="method">
        ///     Callback delegate that will be executed async.
        /// </param>
        /// <param name="arg">
        ///     Argument that is passed a part of the BeginInvoke call
        /// </param>
        public static CallbackResult EnqueueNormalCallback(DispatcherOperationCallback method, object arg)
        {
            return EnqueueCallback(Dispatcher.CurrentDispatcher, DispatcherPriority.Normal, method, arg);
        }

        ///<summary>
        /// Perform a BeginInvoke call on the current Dispatcher
        /// with Input Priority, using the method and object as part
        /// of the Dispatcher.BeginInvoke call
        ///</summary>
        /// <param name="method">
        ///     Callback delegate that will be executed async.
        /// </param>
        /// <param name="arg">
        ///     Argument that is passed a part of the BeginInvoke call
        /// </param>
        public static CallbackResult EnqueueInputCallback(DispatcherOperationCallback method, object arg)
        {
            return EnqueueCallback(Dispatcher.CurrentDispatcher, DispatcherPriority.Input, method, arg);
        }

        ///<summary>
        /// Perform a BeginInvoke call on the current Dispatcher
        /// with Background Priority, using the method and object as part
        /// of the Dispatcher.BeginInvoke call
        ///</summary>
        /// <param name="method">
        ///     Callback delegate that will be executed async.
        /// </param>
        /// <param name="arg">
        ///     Argument that is passed a part of the BeginInvoke call
        /// </param>
        public static CallbackResult EnqueueBackgroundCallback(DispatcherOperationCallback method, object arg)
        {
            return EnqueueCallback(Dispatcher.CurrentDispatcher, DispatcherPriority.Background, method, arg);
        }




        ///<summary>
        /// Perform a BeginInvoke call on the specified Dispatcher
        /// with Normal Priority, using the method and object as part
        /// of the Dispatcher.BeginInvoke call
        ///</summary>
        /// <param name="dispatcher">
        ///     Refence to the dispatcher where the BeginInvoke call
        ///     will be perform.
        /// </param>
        /// <param name="method">
        ///     Callback delegate that will be executed async.
        /// </param>
        /// <param name="arg">
        ///     Argument that is passed a part of the BeginInvoke call
        /// </param>
        public static CallbackResult EnqueueNormalCallback(Dispatcher dispatcher, DispatcherOperationCallback method, object arg)
        {
            return EnqueueCallback(dispatcher, DispatcherPriority.Normal, method, arg);
        }

        ///<summary>
        /// Perform a BeginInvoke call on the specified Dispatcher
        /// with Input Priority, using the method and object as part
        /// of the Dispatcher.BeginInvoke call
        ///</summary>
        /// <param name="dispatcher">
        ///     Refence to the dispatcher where the BeginInvoke call
        ///     will be perform.
        /// </param>
        /// <param name="method">
        ///     Callback delegate that will be executed async.
        /// </param>
        /// <param name="arg">
        ///     Argument that is passed a part of the BeginInvoke call
        /// </param>
        public static CallbackResult EnqueueInputCallback(Dispatcher dispatcher, DispatcherOperationCallback method, object arg)
        {
            return EnqueueCallback(dispatcher, DispatcherPriority.Input, method, arg);
        }

        ///<summary>
        /// Perform a BeginInvoke call on the specified Dispatcher
        /// with Background Priority, using the method and object as part
        /// of the Dispatcher.BeginInvoke call
        ///</summary>
        /// <param name="dispatcher">
        ///     Refence to the dispatcher where the BeginInvoke call
        ///     will be perform.
        /// </param>
        /// <param name="method">
        ///     Callback delegate that will be executed async.
        /// </param>
        /// <param name="arg">
        ///     Argument that is passed a part of the BeginInvoke call
        /// </param>
        public static CallbackResult EnqueueBackgroundCallback(Dispatcher dispatcher, DispatcherOperationCallback method, object arg)
        {
            return EnqueueCallback(dispatcher, DispatcherPriority.Background, method, arg);
        }



        ///<summary>
        /// This is a private wrapper for encapsulating the CallbackResult class creation
        ///</summary>
        public static CallbackResult EnqueueCallback(
            DispatcherPriority priority,
            DispatcherOperationCallback method,
            object args)
        {
            DispatcherOperation operation = BeginInvoke(Dispatcher.CurrentDispatcher, priority, method, args);

            return new CallbackResult(operation);
        }


        ///<summary>
        /// This is a private wrapper for encapsulating the CallbackResult class creation
        ///</summary>
        public static CallbackResult EnqueueCallback(
            Dispatcher dispatcher,
            DispatcherPriority priority,
            DispatcherOperationCallback method,
            object args)
        {
            DispatcherOperation operation = BeginInvoke(dispatcher, priority, method, args);

            return new CallbackResult(operation);
        }


        ///<summary>
        /// This is a private wrapper for the real BeginInvoke call on the Dispatcher
        ///</summary>
        public static DispatcherOperation BeginInvokeWrapper
            (DispatcherPriority priority,
            Delegate method,
            object obj)
        {
            return BeginInvoke(Dispatcher.CurrentDispatcher, priority, method, obj);
        }

        ///<summary>
        /// This is a private wrapper for the real BeginInvoke call on the Dispatcher
        ///</summary>
        public static DispatcherOperation BeginInvokeWrapper
            (Dispatcher dispatcher,
            DispatcherPriority priority,
            Delegate method,
            object obj)
        {
            return BeginInvoke(dispatcher, priority, method, obj);
        }



        ///<summary>
        /// This is a private wrapper for the real BeginInvoke call on the Dispatcher
        ///</summary>
        private static DispatcherOperation BeginInvoke
            (Dispatcher dispatcher,
            DispatcherPriority priority,
            Delegate method,
            object obj)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }

            return dispatcher.BeginInvoke(
                priority,
                method,
                obj);
        }


        ///<summary>
        /// Invoke the specified delegate on the specified Dispatcher at Normal priority
        ///</summary>
        public static object InvokeNormal
            (Dispatcher dispatcher,
            Delegate method,
            object obj)
        {

            return Invoke(dispatcher, DispatcherPriority.Normal, method, obj);
        }

        ///<summary>
        /// Invoke the specified delegate on the specified Dispatcher at Background priority
        ///</summary>
        public static object InvokeBackground
            (Dispatcher dispatcher,
            Delegate method,
            object obj)
        {

            return Invoke(dispatcher, DispatcherPriority.Background, method, obj);
        }

        ///<summary>
        /// Invoke the specified delegate on the specified Dispatcher at specified priority
        ///</summary>
        private static object Invoke
            (Dispatcher dispatcher,
            DispatcherPriority priority,
            Delegate method,
            object obj)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }

            return dispatcher.Invoke(
                priority,
                method,
                obj);
        }


        /// <summary>
        /// Adds the specified callback on the specified Dispatcher ExceptionFilter event.
        /// </summary>
        public static void AddExceptionFilter(Dispatcher dispatcher, DispatcherUnhandledExceptionFilterEventHandler hook)
        {
            dispatcher.UnhandledExceptionFilter += hook;
        }

        /// <summary>
        /// Adds the specified callback on the current Dispatcher ExceptionFilter event.
        /// </summary>
        public static void AddExceptionFilter(DispatcherUnhandledExceptionFilterEventHandler hook)
        {
            AddExceptionFilter(Dispatcher.CurrentDispatcher, hook);
        }

        /// <summary>
        /// Removes the specified callback on the specified Dispatcher ExceptionFilter event.
        /// </summary>
        public static void RemoveExceptionFilter(Dispatcher dispatcher, DispatcherUnhandledExceptionFilterEventHandler hook)
        {
            dispatcher.UnhandledExceptionFilter -= hook;
        }

        /// <summary>
        /// Removes the specified callback on the current Dispatcher ExceptionFilter event.
        /// </summary>
        public static void RemoveExceptionFilter(DispatcherUnhandledExceptionFilterEventHandler hook)
        {
            AddExceptionFilter(Dispatcher.CurrentDispatcher, hook);
        }


        #region DoEvents
      

        /// <summary>
        /// Empties the queue at all priorities above SystemIdle.  This
        /// effectively enables a caller to do all pending Avalon work
        /// before continuing.
        /// </summary>
        /// <remarks>
        /// This enqueues a dummy SystemIdle item and pushes a
        /// dispatcher frame.  When the item is eventually
        /// dispatched, it discontinues the dispatcher frame and control
        /// returns to the caller of DoEvents().
        /// 
        /// Pushing a frame causes the dispatcher to pump messages in 
        /// a nested loop.  Those messages are the way all Avalon
        /// work gets initiated.
        /// </remarks>
        static public void DoEvents()
        {
            DispatcherHelper.DoEvents(0);
        }

        /// <summary>
        /// Empties the queue at all priority until the specified time expires after that time
        /// it will drain the queue past the specified priority.  This
        /// effectively enables a caller to do all pending Avalon work
        /// before continuing.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="minimumWait">An optional minimum number of milliseconds to empty the queue repeatedly. Default: 0.</param>
        /// <param name="priority"></param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        static public void DoEvents(int minimumWait, DispatcherPriority priority)
        {
            if ((Dispatcher.CurrentDispatcher != null) && (!Dispatcher.CurrentDispatcher.HasShutdownStarted))
            {
                // Create a timer for the minimum wait time.
                // When the time passes, the Tick handler will be called,
                // which allows us to stop the dispatcher frame.
                DispatcherTimer timer = new DispatcherTimer(priority);
                timer.Tick += new EventHandler(OnDispatched);
                timer.Interval = TimeSpan.FromMilliseconds(minimumWait);


                // Run a dispatcher frame.
                DispatcherFrame dispatcherFrame = new DispatcherFrame(false);
                timer.Tag = dispatcherFrame;
                timer.Start();
                Dispatcher.PushFrame(dispatcherFrame);
            }
            else
            {
                Microsoft.Test.Logging.LogManager.LogMessageDangerously("Hit DispatcherHelper.DoEvents, but Shutdown had already begun or the dispatcher was null");
            }

        }

        /// <summary>
        /// Empties the queue at all priority until the specified time expires after that time
        /// it will drain the queue past SystemIdle priority.  This
        /// effectively enables a caller to do all pending Avalon work
        /// before continuing.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="minimumWait">An optional minimum number of milliseconds to empty the queue repeatedly. Default: 0.</param>
        static public void DoEvents(int minimumWait)
        {
            DoEvents(minimumWait, DispatcherPriority.SystemIdle);
        }

        /// <summary>
        /// Empties the queue at all priorities above or equal to Input.  This
        /// effectively enables a caller to do all pending Avalon work
        /// before continuing.
        /// </summary>
        static public void DoEventsPastInput()
        {
            DoEvents(0, DispatcherPriority.Input);
        }


        /// <summary>
        /// Empties the queue at all priorities above or equal the specified priority.  This
        /// effectively enables a caller to do all pending Avalon work
        /// before continuing.
        /// </summary>
        static public void DoEvents(DispatcherPriority minimumPriority)
        {
            DoEvents(0, minimumPriority);
        }

        /// <summary>
        /// Dummy SystemIdle dispatcher item.  This discontinues the current
        /// dispatcher frame so control can return to the caller of DoEvents().
        /// </summary>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        static private void OnDispatched(object sender, EventArgs args)
        {
            // Stop the timer now.
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            DispatcherFrame frame = (DispatcherFrame)timer.Tag;
            frame.Continue = false;
        }


        #endregion


        /// <summary>
        /// Pass the WaitHandle that after you signal it will BeginInvoke the callback passed as parameter
        /// with the priority on the parameter
        /// </summary>
        static public RegisteredWaitHandle BeginInvokeOnSignalHandle(WaitHandle handle, Dispatcher dispatcher, DispatcherPriority priority, DispatcherOperationCallback callback, object o)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            if (callback == null)
                throw new ArgumentNullException("callback");

            RegisteredWaitHandle rHandle = ThreadPool.RegisterWaitForSingleObject(
                handle, new WaitOrTimerCallback(WaitorTimerCallback),
                new SignalPackage(dispatcher, callback, priority, o), 180000, false);

            return rHandle;

        }


        /// <summary>
        /// Enters on a nested loop and later it will execute the specified callback
        /// on a Normal Priority. The dispatcherFrame will be constructed using the 
        /// first parameter on the function.  The  DispatcherFrame that it 
        /// is been used will be passed on the firt parameter on the callback.
        /// </summary>
        static public void EnterLoopNormal(bool dispatcherFrame, EventHandler callback)
        {
            DispatcherFrame frame = new DispatcherFrame(dispatcherFrame);
            EnterLoopNormal(frame, callback);
        }

        /// <summary>
        /// Enters on a nested loop and later it will execute the specified callback
        /// on a Normal Priority. The nested loop will use the specified DispatcherFrame.
        /// The  DispatcherFrame that it is been used will be passed on the first 
        /// parameter on the callback.
        /// </summary>
        static public void EnterLoopNormal(DispatcherFrame frame, EventHandler callback)
        {
            EnterLoop(frame, callback, DispatcherPriority.Normal);
        }

        /// <summary>
        /// Enters on a nested loop and later it will execute the specified callback
        /// on a Background Priority. The nested loop will use the specified DispatcherFrame.
        /// The  DispatcherFrame that it is been used will be passed on the first 
        /// parameter on the callback.
        /// </summary>
        static public void EnterLoopBackground(DispatcherFrame frame, EventHandler callback)
        {
            EnterLoop(frame, callback, DispatcherPriority.Background);
        }

        /// <summary>
        /// Enters on a nested loop and later it will execute the specified callback
        /// on the speficied Priority. The nested loop will use the specified DispatcherFrame.
        /// The  DispatcherFrame that it is been used will be passed on the first 
        /// parameter on the callback.
        /// </summary>
        static public void EnterLoop(DispatcherFrame frame, EventHandler callback, DispatcherPriority priority)
        {
            if (callback != null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(priority,
                    (DispatcherOperationCallback)delegate(object o)
                    {
                        EnterLoopHelperObject helperObject = (EnterLoopHelperObject)o;

                        helperObject.Callback(helperObject.Frame, EventArgs.Empty);
                        return null;
                    }, new EnterLoopHelperObject(callback, frame));
            }
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Validates the HasShutDownStarted and HasShutdownFinished properties
        /// on the current dispatcher against the specified values.
        /// </summary>
        static public void ValidHasShutdownStatus(bool expectedStarted, bool expectedFinished)
        {
            ValidHasShutdownStatus(Dispatcher.CurrentDispatcher, expectedStarted, expectedFinished);
        }

        /// <summary>
        /// Validates the HasShutDownStarted and HasShutdownFinished properties
        /// on the specified dispatcher against the specified values.
        /// </summary>
        static public void ValidHasShutdownStatus(Dispatcher dispatcher, bool expectedStarted, bool expectedFinished)
        {
            if (dispatcher.HasShutdownStarted != expectedStarted)
            {
                Log(false, "HasShutdownStarted is not the expected");
            }

            if (dispatcher.HasShutdownFinished != expectedFinished)
            {
                Log(false, "HasShutdownFinished is not the expected");
            }
        }

        /// <summary>
        /// Adds a hook to the messageonlywindow for the current Dispatcher.
        /// The MethodInfo has to match the MS.Win32.HwndHookWrapper.
        /// We return the delegate use as parameter on the AddHook call, because
        /// you need to hold a strong ref to it.
        /// </summary>
        /// <param name="methodInfoCallback">callback that will be called. It must be a static method.</param>
        static public Delegate AddHookToDispatcherMessageOnlyWindow(MethodInfo methodInfoCallback)
        {
            return AddHookToDispatcherMessageOnlyWindow(Dispatcher.CurrentDispatcher, methodInfoCallback);
        }


        /// <summary>
        /// Adds a hook to the messageonlywindow for the current Dispatcher.
        /// The MethodInfo has to match the MS.Win32.HwndHookWrapper.
        /// We return the delegate use as parameter on the AddHook call, because
        /// you need to hold a strong ref to it.        
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="methodInfoCallback">callback that will be called. It must be a static method.</param>
        static public Delegate AddHookToDispatcherMessageOnlyWindow(Dispatcher dispatcher, MethodInfo methodInfoCallback)
        {
            if (!methodInfoCallback.IsStatic)
            {
                throw new ArgumentException("The callback should be static");
            }

            object mowObject = GetDispatcherMessageOnlyWindow(dispatcher);

            //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
            // {
                MethodInfo addHookMethod = mowObject.GetType().GetMethod("AddHook");

                ParameterInfo[] pInfo = addHookMethod.GetParameters();

                Type delegateType = pInfo[0].ParameterType;

                Delegate del = Delegate.CreateDelegate(delegateType, methodInfoCallback);

                object[] arg = { del };
                addHookMethod.Invoke(mowObject, arg);

                return del;
            //}
        }

        /// <summary>
        /// Returns the MessageOnlyWindow object for the specified Dispatcher.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        static object GetDispatcherMessageOnlyWindow(Dispatcher dispatcher)
        {
            //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
            // {
                object scdObject = dispatcher.GetType().InvokeMember("_window",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField, null, dispatcher, null);

                object mowObject = scdObject.GetType().InvokeMember("Value",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty, null, scdObject, null);
            //}
            return mowObject;

        }

        /// <summary>
        /// Send a win32 message to the WNDPROC of the internal message only Window that it is on the current Dispatcher.
        /// </summary>
        static public IntPtr SendMessageToDispatcherWndProc(int msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessageToDispatcherWndProc(Dispatcher.CurrentDispatcher, msg, wParam, lParam);
        }

        /// <summary>
        /// Send a win32 message to the WNDPROC of the internal message only Window that it is on the specified Dispatcher.
        /// </summary>
        static public IntPtr SendMessageToDispatcherWndProc(Dispatcher dispatcher, int msg, IntPtr wParam, IntPtr lParam)
        {
            object hwndWrapperObj = GetDispatcherMessageOnlyWindow(dispatcher);

            if (hwndWrapperObj == null)
            {
                return IntPtr.Zero;
            }
            //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
            // {
                object intPtrObj = hwndWrapperObj.GetType().InvokeMember("Handle",
                            BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                            | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                            null,
                            hwndWrapperObj,
                            null);
            //}

            if (intPtrObj == null)
            {
                return IntPtr.Zero;
            }


            IntPtr hwnd = (IntPtr)intPtrObj;
            HandleRef hwndRef = new HandleRef(null, hwnd);


            if (!NativeMethods.IsWindow(hwndRef))
            {
                return IntPtr.Zero;
            }

            return NativeMethods.SendMessage(hwndRef, msg, wParam, lParam);
        }

        static void Log(bool bResult, string message)
        {
            if (TestLog.Current != null)
            {
                TestLog.Current.LogEvidence(message);

                if (bResult == true)
                    TestLog.Current.Result = TestResult.Pass;
                else
                    TestLog.Current.Result = TestResult.Fail;
            }
            else
            {
                if (!bResult)
                {
                    throw new Exception(message);
                }
            }
        }

        //TODO-Miguep: uncomment
        ///// <summary>
        ///// Return a full trusted wrapper of the DispatcherHooks for
        ///// the current dispatcher.
        ///// </summary>
        ///// <returns>Full-Trusted Wrapper of a DispatcherHooks</returns>
        //[CLSCompliant(false)]
        //public static DispatcherHooksSW GetHooks()
        //{
        //    return GetHooks(Dispatcher.CurrentDispatcher);
        //}

        ///// <summary>
        ///// Return a full trusted wrapper of the DispatcherHooks for
        ///// the specified dispatcher.
        ///// </summary>
        ///// <param name="dispatcher">Dispatcher</param>
        ///// <returns>Full-Trusted Wrapper of a DispatcherHooks</returns>
        //[CLSCompliant(false)]
        //public static DispatcherHooksSW GetHooks(Dispatcher dispatcher)
        //{
        //    return DispatcherSW.Wrap(dispatcher).Hooks;
        //}

        /// <summary>
        /// Given a DispatcherOperation, retrieves the value of its "Name" property.
        /// This property is internal, so reflection is used.
        /// </summary>
        /// <param name="operation">DispatcherOperation</param>
        /// <returns>Name property of the provided DispatcherOperation</returns>
        public static String GetNameFromDispatcherOperation(DispatcherOperation operation)
        {
            //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
            // {
                object nameObj = operation.GetType().InvokeMember("Name",
                            BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                            | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                            null,
                            operation,
                            null);
            //}
            return (nameObj as String);
        }


        /// <summary>
        /// Return if the specified DispatcherOperation is still on the Dispatcher queue.
        /// </summary>
        /// <param name="operation">DispatcherOperation</param>
        /// <returns>true if the dispatcher is still enqueued.  This is not thread safe</returns>
        public static bool IsDispatcherOperationEnqueued(DispatcherOperation operation)
        {
            //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
            // {
                object itemObj = operation.GetType().InvokeMember("_item",
                                BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Public
                                | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                                null,
                                operation,
                                null);

                Type priorityItemType = itemObj.GetType();

                bool value = (bool)priorityItemType.InvokeMember("IsQueued",
                            BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                            | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                            null,
                            itemObj,
                            null);

            //}


            return value;
        }


        /// <summary>
        /// Return if the Current dispatcher has disabled processing the queues.
        /// </summary>
        /// <returns></returns>
        public static bool IsDispatcherDisabledProcessing()
        {
            return IsDispatcherDisabledProcessing(Dispatcher.CurrentDispatcher);
        }


        /// <summary>
        /// Return if the specified dispatcher has disabled processing the queues.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public static bool IsDispatcherDisabledProcessing(Dispatcher dispatcher)
        {
            //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
            // {
                object countObj = dispatcher.GetType().InvokeMember("_disableProcessingCount",
                            BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Public
                            | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                            null,
                            dispatcher,
                            null);
            //}

            int count = (int)countObj;

            if (count < 0)
            {
                throw new InvalidOperationException("The internal _disableProcessingCount count cannot be less than 0.");
            }

            if (count > 0)
            {
                return true;
            }

            return false;
        }



        /// <summary>
        /// Dumps to a file the Avalon Dispatcher Queue
        /// </summary>
        /// <param name="fileName">The complete file path that the queue is dumped to. path can be a filename</param>
        /// <param name="dispatcher">Dump the queue for dispatcher that is passed</param>
        /// <param name="verbose">True will display all the delegates names that are enqueue per
        /// PriorityChain on the queue
        /// </param>
        static public void DumpToFile(string fileName, Dispatcher dispatcher, bool verbose)
        {
            //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
            // {
                StreamWriter writer = null;
                try
                {
                    writer = new StreamWriter(fileName);

                    Dump(writer, dispatcher, verbose);

                }
            //}
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Dumps to a console the Avalon Dispatcher Queue
        /// </summary>
        /// <param name="dispatcher">Dump the queue for dispatcher that is passed</param>
        /// <param name="verbose">True will display all the delegates names that are enqueue per
        /// PriorityChain on the queue
        /// </param>
        static public void DumpToConsole(Dispatcher dispatcher, bool verbose)
        {
            Dump(Console.Out, dispatcher, verbose);
        }

        /// <summary>
        /// Dumps the Avalon Dispatcher Queue
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="dispatcher">Dump the queue for dispatcher that is passed</param>
        /// <param name="verbose">True will display all the delegates names that are enqueue per
        /// PriorityChain on the queue
        /// </param>
        static public void Dump(TextWriter writer, Dispatcher dispatcher, bool verbose)
        {

            DispatcherQueueWrapper dispatcherObj = new DispatcherQueueWrapper(dispatcher);

            // Writing out the Queue MaxPriority Item available on the queue and
            // the total amount of items on the queue

            writer.WriteLine("Maxium Priority on the Queues: " + dispatcherObj.MaxPriority);
            writer.WriteLine("Queues Item Count: " + dispatcherObj.Count);
            writer.WriteLine("");

            // Writing Queues Content

            List<PriorityQueueChain> list = dispatcherObj.PriorityQueueChains;

            // We are going to loop on each available Priority Chain item to display
            // the Number of total enqueued items per chain and if the verbose parameter
            // is true, we will dispatcher all the delegates names

            for (int i = 0; i < list.Count; i++)
            {
                PriorityQueueChain currentChain = list[i];

                writer.WriteLine("Chain Name: " + currentChain.Name);

                List<ItemInfo> itemsInfo = currentChain.Items;
                writer.WriteLine("\tTotal Amount of Items: " + itemsInfo.Count.ToString());

                if (verbose)
                {
                    for (int j = 0; j < itemsInfo.Count; j++)
                    {
                        ItemInfo currentItemInfo = itemsInfo[j];
                        writer.WriteLine("\t" + currentItemInfo.ID);
                    }
                }

                writer.WriteLine("");
            }

        }


        /// <summary>
        /// Adds or removes handlers to Shutdown dispatcher event depending on the specified
        /// parameters.
        /// </summary>
        static public void AddHandlerShutdownEvents(DispatcherShutdownEventsNames eventName, EventHandler callback)
        {
            AddRemoveShutdownEvents(Dispatcher.CurrentDispatcher, eventName, callback, true);
        }

        /// <summary>
        /// Adds or removes handlers to Shutdown dispatcher event depending on the specified
        /// parameters.
        /// </summary>
        static public void RemoveHandlerShutdownEvents(DispatcherShutdownEventsNames eventName, EventHandler callback)
        {
            AddRemoveShutdownEvents(Dispatcher.CurrentDispatcher, eventName, callback, false);
        }

        /// <summary>
        /// Adds or removes handlers to Shutdown dispatcher event depending on the specified
        /// parameters.
        /// </summary>
        static public void AddHandlerShutdownEvents(Dispatcher dispatcher, DispatcherShutdownEventsNames eventName, EventHandler callback)
        {
            AddRemoveShutdownEvents(dispatcher, eventName, callback, true);
        }


        /// <summary>
        /// Adds or removes handlers to Shutdown dispatcher event depending on the specified
        /// parameters.
        /// </summary>
        static public void RemoveHandlerShutdownEvents(Dispatcher dispatcher, DispatcherShutdownEventsNames eventName, EventHandler callback)
        {
            AddRemoveShutdownEvents(dispatcher, eventName, callback, false);
        }


        /// <summary>
        /// Adds or removes handlers to Shutdown dispatcher event depending on the specified
        /// parameters.
        /// </summary>
        static private void AddRemoveShutdownEvents(Dispatcher dispatcher, DispatcherShutdownEventsNames eventName, EventHandler callback, bool add)
        {
            int action = (int)eventName;

            switch (action)
            {
                case 0:

                    if (add)
                    {
                        dispatcher.ShutdownStarted += callback;
                    }
                    else
                    {
                        dispatcher.ShutdownStarted -= callback;
                    }

                    break;

                case 1:

                    if (add)
                    {
                        dispatcher.ShutdownFinished += callback;
                    }
                    else
                    {
                        dispatcher.ShutdownFinished -= callback;
                    }

                    break;
            }

        }


        /// <summary>
        /// Pass the WaitHandle that after you signal it will BeginInvoke the callback passed as parameter
        /// at Background priority.
        /// </summary>
        static public RegisteredWaitHandle SignalBeginInvokeBackground(WaitHandle handle, DispatcherOperationCallback callback, object o)
        {
            return SignalBeginInvoke(handle, Dispatcher.CurrentDispatcher, DispatcherPriority.Background, callback, o);
        }

        /// <summary>
        /// Pass the WaitHandle that after you signal it will BeginInvoke the callback passed as parameter
        /// with the priority on the parameter
        /// </summary>
        static public RegisteredWaitHandle SignalBeginInvoke(WaitHandle handle, Dispatcher dispatcher, DispatcherPriority priority, DispatcherOperationCallback callback, object o)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            if (callback == null)
                throw new ArgumentNullException("callback");

            RegisteredWaitHandle rHandle = ThreadPool.RegisterWaitForSingleObject(
                handle, new WaitOrTimerCallback(WaitorTimerCallback),
                new SignalPackage(dispatcher, callback, priority, o), 180000, false);

            return rHandle;
        }

        /// <summary>
        /// The threadpool is going to call this method when the WaitHandle is signaled
        /// </summary>
        static private void WaitorTimerCallback(object o, bool timeOut)
        {
            if (!timeOut)
            {
                SignalPackage sp = (SignalPackage)o;

                sp._Dispatcher.BeginInvoke(
                    sp._Priority,
                    sp._Callback,
                    sp._Tag
                    );
            }
        }


        /// <summary>
        /// This class is for internal purposes for the BeginInvokeSignalHandle
        /// </summary>
        class EnterLoopHelperObject
        {
            public EnterLoopHelperObject(EventHandler callback, DispatcherFrame frame)
            {
                Callback = callback;
                Frame = frame;
            }

            public EventHandler Callback;
            public DispatcherFrame Frame;
        }

        /// <summary>
        /// This class is for internal purposes for the BeginInvokeSignalHandle
        /// </summary>
        class SignalPackage
        {
            /// <summary>
            /// 
            /// </summary>
            public SignalPackage(Dispatcher d, DispatcherOperationCallback callback, DispatcherPriority p, object t)
            {
                _Dispatcher = d;
                _Priority = p;
                _Tag = t;
                _Callback = callback;
            }

            /// <summary>
            /// 
            /// </summary>            
            public Dispatcher _Dispatcher = null;

            /// <summary>
            /// 
            /// </summary>            
            public DispatcherPriority _Priority;

            /// <summary>
            /// 
            /// </summary>
            public object _Tag = null;

            /// <summary>
            /// 
            /// </summary>
            public DispatcherOperationCallback _Callback = null;

        }


        private static int _voidWin32Message;

    }

    ///<summary>
    /// This class encapsulates the result of a Dispatcher.BeginInvoke 
    /// that was created using the DispatcherHelper.EnqueueCallback API
    ///</summary>    
    public class CallbackResult
    {
        ///<summary>
        /// Internal constructor that is called from the DispatcherHelper class.
        ///</summary>    
        internal CallbackResult(DispatcherOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            _operation = operation;
        }

        ///<summary>
        ///     Returns the result of the operation if it has completed.
        ///</summary>    
        public object Result
        {
            get
            {
                return _operation.Result;
            }
        }

        private DispatcherOperation _operation = null;
    }


    /// <summary>
    /// This class is for internal purposes for the BeginInvokeSignalHandle
    /// </summary>
    class SignalPackage
    {
        /// <summary>
        /// 
        /// </summary>
        public SignalPackage(Dispatcher d, DispatcherOperationCallback callback, DispatcherPriority p, object t)
        {
            _Dispatcher = d;
            _Priority = p;
            _Tag = t;
            _Callback = callback;
        }

        /// <summary>
        /// 
        /// </summary>            
        public Dispatcher _Dispatcher = null;

        /// <summary>
        /// 
        /// </summary>            
        public DispatcherPriority _Priority;

        /// <summary>
        /// 
        /// </summary>
        public object _Tag = null;

        /// <summary>
        /// 
        /// </summary>
        public DispatcherOperationCallback _Callback = null;

    }

}

