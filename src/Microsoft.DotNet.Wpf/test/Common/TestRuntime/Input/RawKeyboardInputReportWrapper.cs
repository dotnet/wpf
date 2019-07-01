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
    /// Wrapper for RawKeyboardInputReport class.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class RawKeyboardInputReportWrapper : InputReportWrapper
    {
        static RawKeyboardInputReportWrapper()
        {
            ReportType = WrapperUtil.AssemblyPC.GetType("System.Windows.Input.RawKeyboardInputReport");
            Trace.Assert(ReportType != null, "Uh oh, not an input report type");
        }

        /// <summary>
        /// Construct RawKeyboardInputReportWrapper.
        /// </summary>
        /// <param name="inputSource">inputSource</param>
        /// <param name="mode">mode</param>
        /// <param name="timestamp">timestamp</param>
        /// <param name="actions">actions</param>
        /// <param name="scanCode">scanCode</param>
        /// <param name="isExtendedKey">isExtendedKey</param>
        /// <param name="isSystemKey">isSystemKey</param>
        /// <param name="virtualKey">virtualKey</param>
        /// <param name="extraInformation">extraInformation</param>
        public RawKeyboardInputReportWrapper(PresentationSource inputSource, InputMode mode, int timestamp, RawKeyboardActions actions, int scanCode, bool isExtendedKey, bool isSystemKey, int virtualKey, IntPtr extraInformation)
            : base()
        {
            object[] objArrayInputReportConstructor = new object[] { inputSource, mode, timestamp, actions, scanCode, isExtendedKey, isSystemKey, virtualKey, extraInformation };
            this.InnerObject = Activator.CreateInstance(ReportType, objArrayInputReportConstructor);
            Trace.Assert(this.InnerObject != null, "Uh oh, not an input report");
        }

        /// <summary>
        /// Construct RawKeyboardInputReportWrapper with keyboard state
        /// </summary>
        /// <param name="inputSource">inputSource</param>
        /// <param name="mode">mode</param>
        /// <param name="timestamp">timestamp</param>
        /// <param name="actions">actions</param>
        /// <param name="scanCode">scanCode</param>
        /// <param name="isExtendedKey">isExtendedKey</param>
        /// <param name="isSystemKey">isSystemKey</param>
        /// <param name="virtualKey">virtualKey</param>
        /// <param name="extraInformation">extraInformation</param>
        /// <param name="keyboardState">keyboardState</param>
        public RawKeyboardInputReportWrapper(PresentationSource inputSource, InputMode mode, int timestamp, RawKeyboardActions actions, int scanCode, bool isExtendedKey, bool isSystemKey, int virtualKey, IntPtr extraInformation, RawKeyboardState keyboardState)
            : base()
        {
            object[] objArrayInputReportConstructor = new object[] { inputSource, mode, timestamp, actions, scanCode, isExtendedKey, isSystemKey, virtualKey, extraInformation, keyboardState };
            this.InnerObject = Activator.CreateInstance(ReportType, objArrayInputReportConstructor);
            Trace.Assert(this.InnerObject != null, "Uh oh, not an input report");
        }

        /// <summary>
        /// Construct RawKeyboardInputReportWrapper from basic report wrapper.
        /// </summary>
        /// <param name="reportWrapper">Report wrapper</param>
        public RawKeyboardInputReportWrapper(InputReportWrapper reportWrapper)
            : base(reportWrapper.InnerObject)
        {
        }


        /// <summary>
        /// </summary>
	public static bool IsRawKeyboardInputReport(object o)
        {
             Type reportType = o.GetType();
             PropertyInfo info = reportType.GetProperty("KeyboardState", WrapperUtil.PropertyBindFlags);

             if (info == null)
             {
                 return false;
             }
             return true;

        }


        /// <summary>
        /// Actions
        /// </summary>
        public RawKeyboardActions Actions
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("Actions", WrapperUtil.PropertyBindFlags);
                return ((RawKeyboardActions)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// ExtraInformation
        /// </summary>
        public IntPtr ExtraInformation
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("ExtraInformation", WrapperUtil.PropertyBindFlags);
                return ((IntPtr)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// IsExtendedKey
        /// </summary>
        public bool IsExtendedKey
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("IsExtendedKey", WrapperUtil.PropertyBindFlags);
                object o = info.GetValue(this.InnerObject, null);
                return (bool)o;
            }
        }

        /// <summary>
        /// IsSystemKey
        /// </summary>
        public bool IsSystemKey
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("IsSystemKey", WrapperUtil.PropertyBindFlags);
                object o = info.GetValue(this.InnerObject, null);
                return (bool)o;
            }
        }

        /// <summary>
        /// KeyboardState
        /// </summary>
        public RawKeyboardState KeyboardState
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("KeyboardState", WrapperUtil.PropertyBindFlags);
                object o = info.GetValue(this.InnerObject, null);
                return (o as RawKeyboardState);
            }
        }

        /// <summary>
        /// ScanCode
        /// </summary>
        public int ScanCode
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("ScanCode", WrapperUtil.PropertyBindFlags);
                object o = info.GetValue(this.InnerObject, null);
                return (int)o;
            }
        }

        /// <summary>
        /// VirtualKey
        /// </summary>
        public int VirtualKey
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("VirtualKey", WrapperUtil.PropertyBindFlags);
                object o = info.GetValue(this.InnerObject, null);
                return (int)o;
            }
        }
    }
}
