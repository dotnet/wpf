// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;
using System.Windows.Input;
using System.Windows;
using Microsoft.Test.Serialization;

namespace Microsoft.Test.Input
{
    /// <summary>
    /// Wrapper for InputReportEventArgs class.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class InputReportEventArgsWrapper
    {
        static InputReportEventArgsWrapper()
        {
            _argsType = WrapperUtil.AssemblyPC.GetType("System.Windows.Input.InputReportEventArgs");
            Trace.Assert(_argsType != null, "Uh oh, not an input report event args type");
        }

        /// <summary>
        /// Construct InputReportEventArgsWrapper.
        /// </summary>
        /// <param name="inputDevice">inputDevice</param>
        /// <param name="reportWrapper">reportWrapper</param>
        public InputReportEventArgsWrapper(InputDevice inputDevice, InputReportWrapper reportWrapper)
        {
            object[] objArrayInputReportConstructor = new object[] { inputDevice, reportWrapper.InnerObject };
            this._inputReportEventArgs = (InputEventArgs)(Activator.CreateInstance(_argsType, objArrayInputReportConstructor));
            Trace.Assert(_inputReportEventArgs != null, "Uh oh, not an input report event args");
        }

        /// <summary>
        /// Construct InputReportEventArgsWrapper.
        /// </summary>
        /// <param name="inputReportEventArgs">Basic input event args package.  Must be an input report event args package</param>
        public InputReportEventArgsWrapper(InputEventArgs inputReportEventArgs)
        {
            if (!IsCorrectType(inputReportEventArgs))
            {
                throw new ArgumentException("inputReportEventArgs", "The type is not correct.");
            }

            this._inputReportEventArgs = inputReportEventArgs;
        }

        /// <summary>
        /// Report (InputReport)
        /// </summary>
        public InputReportWrapper Report
        {
            get
            {
                PropertyInfo info = _argsType.GetProperty("Report", WrapperUtil.PropertyBindFlags);

		if (info != null)
                {
                    object oReport = info.GetValue(this._inputReportEventArgs, null);
                    return new InputReportWrapper(oReport);
                }
                return null;
            }
        }


        /// <summary>
        /// Report (InputReport)
        /// </summary>
        public RoutedEvent RoutedEvent
        {
            get
            {

                PropertyInfo info = _argsType.GetProperty("RoutedEvent", WrapperUtil.PropertyBindFlags);

		if (info != null)
                {
                    object oRoutedEvent = info.GetValue(this._inputReportEventArgs, null);
                    return (RoutedEvent)oRoutedEvent;
                }
                return null;
            }
        }

        /// <summary>
        /// Checks if the given object is an InputReportEventArgs instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsCorrectType(object obj)
        {
            return _argsType == obj.GetType();
        }


        private static Type _argsType;

        private InputEventArgs _inputReportEventArgs;

    }

    /// <summary>
    /// Delegate for InputReportEventArgsWrapper class.
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="e">e</param>
    public delegate void InputReportEventWrapperHandler(object sender, InputReportEventArgsWrapper e);
}
