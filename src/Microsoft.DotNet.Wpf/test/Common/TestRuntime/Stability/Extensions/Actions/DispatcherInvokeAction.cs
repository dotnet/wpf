// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class DispatcherInvokeAction : SimpleDiscoverableAction
    {
        #region Public Members

        public bool IsTimeSpanSet { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public InvokeCallbackTypes InvokeCallbackType { get; set; }

        public Dispatcher Dispatcher { get; set; }

        public int PriorityValue { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DispatcherPriority priority = ChooseValidPriority(PriorityValue);
            switch (InvokeCallbackType)
            {
                case InvokeCallbackTypes.DispatcherOperationCallback:
                    // Using DispatcherOperationCallback.  
                    if (IsTimeSpanSet)
                    {
                        Dispatcher.Invoke(priority, TimeSpan, new DispatcherOperationCallback(DispatcherOperationCallbackMethod), this);
                    }
                    else
                    {
                        Dispatcher.Invoke(priority, new DispatcherOperationCallback(DispatcherOperationCallbackMethod), this);
                    }
                    break;
                case InvokeCallbackTypes.SendOrPostCallback:
                    // Using SendOrPostCallback.
                    if (IsTimeSpanSet)
                    {
                        Dispatcher.Invoke(priority, TimeSpan, new SendOrPostCallback(SendOrPostMethod), this);
                    }
                    else
                    {
                        Dispatcher.Invoke(priority, new SendOrPostCallback(SendOrPostMethod), this);
                    }
                    break;
                case InvokeCallbackTypes.OneParamGeneric:
                    // Using a generic 1 parameter delegate.
                    if (IsTimeSpanSet)
                    {
                        Dispatcher.Invoke(priority, TimeSpan, new OneParamGeneric(OneParamMethod), this);
                    }
                    else
                    {
                        Dispatcher.Invoke(priority, new OneParamGeneric(OneParamMethod), this);
                    }
                    break;
                case InvokeCallbackTypes.TwoParamGeneric:
                    // Using a generic 2 parameters delegate.
                    if (IsTimeSpanSet)
                    {
                        Dispatcher.Invoke(priority, TimeSpan, new TwoParamGeneric(TwoParamMethod), this, new object());

                    }
                    else
                    {
                        Dispatcher.Invoke(priority, new TwoParamGeneric(TwoParamMethod), this, new object());
                    }
                    break;
                case InvokeCallbackTypes.ThreeParamGeneric:
                    // Using a generic 3 parameters delegate.
                    if (IsTimeSpanSet)
                    {
                        Dispatcher.Invoke(priority, TimeSpan, new ThreeParamGeneric(ThreeParamMethod), this, new object(), new object());
                    }
                    else
                    {
                        Dispatcher.Invoke(priority, new ThreeParamGeneric(ThreeParamMethod), this, new object(), new object());
                    }
                    break;
                case InvokeCallbackTypes.ZeroParamGeneric:
                    // Using a generic 0 parameters delegate.
                    if (IsTimeSpanSet)
                    {
                        Dispatcher.Invoke(priority, TimeSpan, new ZeroParamGeneric(ZeroParamMethod));
                    }
                    else
                    {
                        Dispatcher.Invoke(priority, new ZeroParamGeneric(ZeroParamMethod));
                    }
                    break;
            }
        }

        #endregion

        #region Private Members

        private DispatcherPriority ChooseValidPriority(int priorityValue)
        {
            DispatcherPriority[] validPriorities = { DispatcherPriority.SystemIdle, DispatcherPriority.ApplicationIdle, DispatcherPriority.ContextIdle, DispatcherPriority.Background, DispatcherPriority.Input, DispatcherPriority.Loaded, DispatcherPriority.Render, DispatcherPriority.DataBind, DispatcherPriority.Normal, DispatcherPriority.Send };

            return validPriorities[priorityValue % validPriorities.Length];
        }

        private object DispatcherOperationCallbackMethod(object o)
        {
            CommonValidation(o);
            return null;
        }

        private void SendOrPostMethod(object o)
        {
            CommonValidation(0);
        }

        private int OneParamMethod(object o)
        {
            CommonValidation(0);
            return 0;
        }

        private void TwoParamMethod(object o1, object o2)
        {
            CommonValidation(o1);
            CommonValidation(o2);
        }

        private void ThreeParamMethod(object o1, object o2, object o3)
        {
            CommonValidation(o1);
            CommonValidation(o2);
            CommonValidation(o2);
        }

        private void ZeroParamMethod() { }

        private void CommonValidation(object o)
        {
            Trace.WriteLine("Execute CommonValidation method.");
        }

        #endregion

        /// <summary>
        /// Types of callbacks that can be used on the Invoke.
        /// </summary>   
        public enum InvokeCallbackTypes
        {
            DispatcherOperationCallback = 0,
            SendOrPostCallback = 1,
            OneParamGeneric = 2,
            TwoParamGeneric = 3,
            ThreeParamGeneric = 4,
            ZeroParamGeneric = 5
        }

        /// <summary>
        /// Generic 1 argument delegate to exercise all the callpaths for BeginInvoke.
        /// </summary>
        public delegate void ZeroParamGeneric();

        /// <summary>
        /// Generic 1 argument delegate to exercise all the callpaths for BeginInvoke.
        /// </summary>
        public delegate int OneParamGeneric(object o);

        /// <summary>
        /// Generic 2 arguments delegate to exercise all the callpaths for BeginInvoke.
        /// </summary>
        public delegate void TwoParamGeneric(object o1, object o2);

        /// <summary>
        /// Generic 3 arguments delegate to exercise all the callpaths for BeginInvoke.    
        /// </summary>
        public delegate void ThreeParamGeneric(object o1, object o2, object o3);
    }
}
