// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    public class DispatcherOperationFactory : DiscoverableFactory<DispatcherOperation>
    {
        #region Public Members

        public InvokeCallbackTypes InvokeCallbackType { get; set; }

        public Dispatcher Dispatcher { get; set; }

        public int PriorityValue { get; set; }

        #endregion

        #region Public Override Members

        public override DispatcherOperation Create(DeterministicRandom random)
        {
            DispatcherPriority priority = ChooseValidPriority(PriorityValue);
            DispatcherOperation operation = null;
            switch (InvokeCallbackType)
            {
                case InvokeCallbackTypes.DispatcherOperationCallback:
                    operation = Dispatcher.BeginInvoke(priority, new DispatcherOperationCallback(DispatcherOperationCallbackMethod), this);
                    break;
                case InvokeCallbackTypes.SendOrPostCallback:
                    operation = Dispatcher.BeginInvoke(priority, new SendOrPostCallback(SendOrPostMethod), this);
                    break;
                case InvokeCallbackTypes.OneParamGeneric:
                    operation = Dispatcher.BeginInvoke(priority, new OneParamGeneric(OneParamMethod), this);
                    break;
                case InvokeCallbackTypes.TwoParamGeneric:
                    operation = Dispatcher.BeginInvoke(priority, new TwoParamGeneric(TwoParamMethod), this, new object());
                    break;
                case InvokeCallbackTypes.ThreeParamGeneric:
                    operation = Dispatcher.BeginInvoke(priority, new ThreeParamGeneric(ThreeParamMethod), this, new object(), new object());
                    break;
                case InvokeCallbackTypes.ZeroParamGeneric:
                    operation = Dispatcher.BeginInvoke(priority, new ZeroParamGeneric(ZeroParamMethod));
                    break;
            }

            operation.Aborted += new EventHandler(OperationAborted);
            operation.Completed += new EventHandler(OperationCompleted);

            //Save the DispatcherOperation, Some combinations of actions need do on the same DispatcherOperation.
            lock (DispatcherOperations)
            {
                DispatcherOperations.Add(operation);
                if (DispatcherOperations.Count > 10)
                {
                    DispatcherOperations.RemoveAt(random.Next(DispatcherOperations.Count));
                }

                return DispatcherOperations[random.Next(DispatcherOperations.Count)];
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

        private void CommonValidation(object o) { }

        private void OperationCompleted(object sender, EventArgs e) { }

        private void OperationAborted(object sender, EventArgs e) { }

        #endregion

        #region Private Data

        private static List<DispatcherOperation> DispatcherOperations = new List<DispatcherOperation>();

        #endregion

        /// <summary>
        /// Types of callbacks that can be used on the BeginInvoke.
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
