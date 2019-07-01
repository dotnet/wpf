// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Test.Serialization;

namespace Microsoft.Test.Input
{
    /// <summary>
    /// Wrapper for InputReport class.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class InputReportWrapper
    {
        /// <summary>
        /// </summary>
	public static InputReportWrapper BuildWrapper(InputReportWrapper oReport)
        {
            if (RawKeyboardInputReportWrapper.IsRawKeyboardInputReport(oReport))
            {
                return (InputReportWrapper)new RawKeyboardInputReportWrapper(oReport);
            }

            if (RawMouseInputReportWrapper.IsRawMouseInputReport(oReport))
            {
                return (InputReportWrapper)new RawMouseInputReportWrapper(oReport);
            }
            return null;
        }


        /// <summary>
        /// Construct blank input report wrapper.
        /// </summary>
        protected InputReportWrapper()
        {
        }

        /// <summary>
        /// Construct input report wrapper from existing input report.
        /// </summary>
        /// <param name="oReport">Object assumed to be input report.</param>
        public InputReportWrapper(object oReport): this()
        {
            _report = oReport;
            _reportType = oReport.GetType();
        }





        /// <summary>
        /// Mode
        /// </summary>
        public InputMode Mode
        {
            get
            {
                PropertyInfo info = _reportType.GetProperty("Mode", WrapperUtil.PropertyBindFlags);
                return ((InputMode)(info.GetValue(this.InnerObject, null)));
            }
        }
        /// <summary>
        /// Timestamp
        /// </summary>
        public int Timestamp
        {
            get
            {
                PropertyInfo info = _reportType.GetProperty("Timestamp", WrapperUtil.PropertyBindFlags);
                return ((int)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// Type
        /// </summary>
        public InputType Type
        {
            get
            {
                PropertyInfo info = _reportType.GetProperty("Type", WrapperUtil.PropertyBindFlags);
                return ((InputType)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// InputSource
        /// </summary>
        public PresentationSource InputSource
        {
            get
            {
                PropertyInfo info = _reportType.GetProperty("InputSource", WrapperUtil.PropertyBindFlags);
                return (info.GetValue(this.InnerObject, null) as PresentationSource);
            }
        }

        /// <summary>
        /// InnerObject
        /// </summary>
        /// <remarks>
        /// This should be set to an object of type InputReport.
        /// If it isn't, you are doing something wrong.
        /// </remarks>
        public object InnerObject
        {
            get
            {
                return _report;
            }
            set
            {
                _report = value;
            }
        }
        private object _report;

        /// <summary>
        /// What is the name of the concrete input report?
        /// </summary>
        /// <remarks>
        /// Can be RawKeyboardInputReport, RawMouseInputReport, RawTextInputReport
        /// or the empty string if this report is uninitialized.
        /// </remarks>
        public string Name
        {
            get
            {
                if (this.InnerObject != null)
                {
                    return this.InnerObject.GetType().Name;
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        /// <summary>
        /// Type of report instantiated.
        /// </summary>
        protected static Type ReportType
        {
            get
            {
                return _reportType; 
            }
            set
            {
                _reportType = value;
            }
        }

        private static Type _reportType=null;
    }
}
