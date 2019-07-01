// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Test.Logging;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Serialization.CustomElements
{
    /// <summary>
    /// Used by custom elements to do verification and close their windows.
    /// </summary>
    internal static class CustomElementHelper
    {
        static CustomElementHelper()
        {
            _postedElements = new Hashtable();
        }

        /// <summary>
        /// Posts an item to the dispatcher queue to call the
        /// verifier routine or stop the dispatcher.
        /// </summary>
        /// <param name="verify">Whether to post the verifier routine first or immediately post the routine to quite the dispatcher.</param>
        /// <param name="visual">Visual element to pass to the posted item.</param>
        /// <returns>The operation object.</returns>
        static public void PostItem(bool verify, Visual visual)
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

            // Each custom object may only post one item.
            if (_postedElements.Contains(visual))
            {
                return;
            }
            _postedElements.Add(visual, null);

            // Attach a handler for ShutdownFinished if we haven't already.
            if (dispatcher != null && _shutdownFinishedHandler == null)
            {
                _shutdownFinishedHandler = new EventHandler(_OnExitDispatcher);
                dispatcher.ShutdownFinished += _shutdownFinishedHandler;
            }

            _postedCount++; 
            dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new DispatcherOperationCallback(_InvokeAfterRender), new object[] { visual, verify });
        }

        // Clears the list of previously-posted items.
        static private void _OnExitDispatcher(object o, EventArgs args)
        {
            if (_postedCount > 0)
            {
                throw new Exception("Dispatcher was shutdown while there are posted items remaining.");
            }

            _shutdownFinishedHandler = null;
        }

        // Handler for Dispatcher.ShutdownFinished event.
        static private EventHandler _shutdownFinishedHandler = null;

        /// <summary>
        /// Calls RenderedEvent handlers, calls verify routine, closes the window, 
        /// and initiates quitting the dispatcher if the verify routine did not
        /// return false.
        /// </summary>
        static private object _InvokeAfterRender(object obj)
        {
            bool shouldQuit = true;
            object element = null;
            bool verify = false;
            object[] vars = (object[])obj;

            _postedCount--;

            try
            {
                element = vars[0];
                verify = (bool)vars[1];

                if (verify)
                {
                    shouldQuit = _Verify(element);
                }
            }
            catch (Exception err)
            {
                GlobalLog.LogStatus("Storing exception...");
                SerializationHelper.StoreException(err);
            }


            // If the return value is 'false', that means
            // it is not done yet. We must not quit the dispatcher.
            // Instead, we must queue up again to enable
            // the verifier routine another shot.
            if (shouldQuit)
            {
                if (_postedCount <= 0)
                {
                    _QuitDispatcher();
                }
            }
            else
            {
                // Enqueue self to call the verify routine again.
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new DispatcherOperationCallback(_InvokeAfterRender), new object[] {element, verify});
                _postedCount++;
            }

            return null;
        }
        /// <summary>
        /// Effectively closes the window by shutting down the dispatcher.
        /// </summary>
        static private object _QuitDispatcher()
        {
            try
            {
                GlobalLog.LogStatus("Calling SerializationHelper.CloseDisplayedTree...");
                SerializationHelper.CloseDisplayedTree();
            }
            catch (Exception err)
            {
                GlobalLog.LogStatus("Storing exception...");
                SerializationHelper.StoreException(err);
            }

            return null;
        }


        /// <summary>
        /// Given a VisualTreeNode, walks up the parent
        /// chain to find the root of the VisualTree.
        /// </summary>
        static private Visual _FindVisualRoot(Visual visualTreeNode)
        {
            if (visualTreeNode == null)
            {
                throw new ArgumentNullException("visualTreeNode");
            }

            Visual walkerNode = visualTreeNode;
            Visual parentNode = (Visual)VisualTreeHelper.GetParent(walkerNode);

            // Call "Parent" repeatedly until we hit a null.
            while (parentNode != null)
            {
                walkerNode = parentNode;
                parentNode = (Visual)VisualTreeHelper.GetParent(parentNode);
            }

            // Return the last non-null node we saw
            return walkerNode;
        }

        /// <summary>
        /// Using the Verify property, invokes the verification handler.
        /// </summary>
        /// <param name="element"></param>
        static private bool _Verify(object element)
        {
            // Cast element to ICustomElement.
            ICustomElement customElement = (ICustomElement)element;

            // Get class and method name of verifier routine.
            string assemblyName = String.Empty;
            string className = String.Empty;
            string methodName = String.Empty;
            string verifier = customElement.Verifier;
            Assembly assembly = null;

            int period = verifier.IndexOf("#");

            if (period <= 0)
            {
                throw new Exception("Verifier property is not in the correct format: <AssemblyName>#<TypeName>.<MethodName>.");
            }

            assemblyName = customElement.Verifier.Substring(0, period);
            assemblyName = Path.GetFileNameWithoutExtension(assemblyName);
            //Using LoadWithPartialName rather than LoadFrom, in order to avoid the same Assembly from being loaded twice.
            #pragma warning disable 618
            assembly = Assembly.LoadWithPartialName(assemblyName);
            #pragma warning restore 618

            verifier = verifier.Substring(period + 1, verifier.Length - period - 1);
            period = verifier.LastIndexOf(".");

            if (period <= 0)
            {
                throw new Exception("Verifier property is not in the correct format: <AssemblyName>#<TypeName>.<MethodName>.");
            }

            className = verifier.Substring(0, period);
            methodName = verifier.Substring(period + 1, verifier.Length - period - 1);

            Type originalType = assembly.GetType(className, true, false);
            MethodInfo methodInfo = originalType.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new Exception("Could not find method '" + methodName + "' on type '" + className + ". Method must be public and declared directly on the class, i.e. not inherited.");
            }

            //
            // Use reflection to call the verification routine specified in this.Verifier.
            // For better readability, only print this info once for multiple-verification scenarios
            //
            if (_verifierFirstCalled)
            {
                _verifierFirstCalled = false;
                GlobalLog.LogStatus("Calling verification routine: " + assemblyName + "#" + className + "." + methodName + "...");
            }

            object[] paramarray = new object[] { customElement };
            object returnval = null;

            if (methodInfo.IsStatic)
            {
                returnval = methodInfo.Invoke(null, paramarray);
            }
            else
            {
                object obj = Activator.CreateInstance(
                    originalType,
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new object[0],
                    null
                );

                returnval = methodInfo.Invoke(obj, paramarray);
            }

            // If the return value was void, return true to specify
            // that all verification is done.  Otherwise, convert the return value
            // to bool and return that.  A 'false' value specifies that there is 
            // more verification to do; the verifier should be called again.  
            // This allows interactive tests for animations, etc.
            if (returnval == null)
            {
                return true;
            }
            else
            {
                return Convert.ToBoolean(returnval);
            }
        }

        static private int _postedCount = 0;
        static private bool _verifierFirstCalled = true;
        static private Hashtable _postedElements = null;

    }
}

