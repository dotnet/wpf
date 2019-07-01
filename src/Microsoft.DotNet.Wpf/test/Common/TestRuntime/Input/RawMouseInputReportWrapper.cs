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
    /// Wrapper for RawMouseInputReport class.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class RawMouseInputReportWrapper : InputReportWrapper
    {
        static RawMouseInputReportWrapper()
        {
            ReportType = WrapperUtil.AssemblyPC.GetType("System.Windows.Input.RawMouseInputReport");
            Trace.Assert(ReportType != null, "Uh oh, not an input report type");
        }

        /// <summary>
        /// Construct RawMouseInputReportWrapper.
        /// </summary>
        /// <param name="mode">mode</param>
        /// <param name="timestamp">timestamp</param>
        /// <param name="inputSource">inputSource</param>
        /// <param name="actions">actions</param>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="wheel">wheel</param>
        /// <param name="extraInformation">extraInformation</param>
        public RawMouseInputReportWrapper(InputMode mode, int timestamp, PresentationSource inputSource, RawMouseActions actions, int x, int y, int wheel, IntPtr extraInformation)
            : base()
        {
            object[] objArrayInputReportConstructor = new object[] { mode, timestamp, inputSource, actions, x, y, wheel, extraInformation };
            this.InnerObject = Activator.CreateInstance(ReportType, objArrayInputReportConstructor);
            Trace.Assert(this.InnerObject != null, "Uh oh, not an input report");
        }

        /// <summary>
        /// Construct RawMouseInputReportWrapper with mouse state.
        /// </summary>
        /// <param name="mode">mode</param>
        /// <param name="timestamp">timestamp</param>
        /// <param name="inputSource">inputSource</param>
        /// <param name="actions">actions</param>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="wheel">wheel</param>
        /// <param name="extraInformation">extraInformation</param>
        /// <param name="state">state</param>
        public RawMouseInputReportWrapper(InputMode mode, int timestamp, PresentationSource inputSource, RawMouseActions actions, int x, int y, int wheel, IntPtr extraInformation, RawMouseState state)
            : base()
        {
            object[] objArrayInputReportConstructor = new object[] { mode, timestamp, inputSource, actions, x, y, wheel, extraInformation, state };
            this.InnerObject = Activator.CreateInstance(ReportType, objArrayInputReportConstructor);
            Trace.Assert(this.InnerObject != null, "Uh oh, not an input report");
        }

        /// <summary>
        /// Construct RawMouseInputReportWrapper from basic report wrapper.
        /// </summary>
        /// <param name="reportWrapper">Report wrapper</param>
        public RawMouseInputReportWrapper(InputReportWrapper reportWrapper)
            : base(reportWrapper.InnerObject)
        {
        }


        /// <summary>
        /// 
        /// </summary>
        public static bool IsRawMouseInputReport(object o)
        {
             Type reportType = o.GetType();         
             PropertyInfo info = reportType.GetProperty("MouseState", WrapperUtil.PropertyBindFlags);

             if (info == null)
             {
                 return false;
             }
             return true;
        }

        

        /// <summary>
        /// Actions
        /// </summary>
        public RawMouseActions Actions
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("Actions", WrapperUtil.PropertyBindFlags);
                return ((RawMouseActions)(info.GetValue(this.InnerObject, null)));
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
        /// MouseState
        /// </summary>
        public RawMouseState MouseState
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("MouseState", WrapperUtil.PropertyBindFlags);
                object o = info.GetValue(this.InnerObject, null);
                return (o as RawMouseState);
            }
        }
        
        /// <summary>
        /// Wheel
        /// </summary>
        public int Wheel
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("Wheel", WrapperUtil.PropertyBindFlags);
                return ((int)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// X
        /// </summary>
        public int X
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("X", WrapperUtil.PropertyBindFlags);
                return ((int)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// Y
        /// </summary>
        public int Y
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("Y", WrapperUtil.PropertyBindFlags);
                return ((int)(info.GetValue(this.InnerObject, null)));
            }
        }
    }
}
