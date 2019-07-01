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
    /// Wrapper for RawTextInputReport class.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class RawTextInputReportWrapper : InputReportWrapper
    {
        static RawTextInputReportWrapper()
        {
            ReportType = WrapperUtil.AssemblyPC.GetType("System.Windows.Input.RawTextInputReport");
            Trace.Assert(ReportType != null, "Uh oh, not an input report type");
        }

        /// <summary>
        /// Construct RawTextInputReportWrapper.
        /// </summary>
        /// <param name="inputSource">inputSource</param>
        /// <param name="mode">mode</param>
        /// <param name="timestamp">timestamp</param>
        /// <param name="isDeadCharacter">isDeadCharacter</param>
        /// <param name="isSystemCharacter">isSystemCharacter</param>
        /// <param name="isControlCharacter">isControlCharacter</param>
        /// <param name="characterCode">characterCode</param>
        public RawTextInputReportWrapper(PresentationSource inputSource, InputMode mode, int timestamp, bool isDeadCharacter, bool isSystemCharacter, bool isControlCharacter, char characterCode)
            : base()
        {
            object[] objArrayInputReportConstructor = new object[] { inputSource, mode, timestamp, isDeadCharacter, isSystemCharacter, isControlCharacter, characterCode };
            this.InnerObject = Activator.CreateInstance(ReportType, objArrayInputReportConstructor);
            Trace.Assert(this.InnerObject != null, "Uh oh, not an input report");
        }

        /// <summary>
        /// Construct RawTextInputReportWrapper from basic report wrapper.
        /// </summary>
        /// <param name="reportWrapper">Report wrapper</param>
        public RawTextInputReportWrapper(InputReportWrapper reportWrapper)
            : base(reportWrapper.InnerObject)
        {
        }

        /// <summary>
        /// CharacterCode
        /// </summary>
        public char CharacterCode
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("CharacterCode", WrapperUtil.PropertyBindFlags);
                return ((char)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// IsControlCharacter 
        /// </summary>
        public bool IsControlCharacter
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("IsControlCharacter", WrapperUtil.PropertyBindFlags);
                return ((bool)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// IsDeadCharacter
        /// </summary>
        public bool IsDeadCharacter
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("IsDeadCharacter", WrapperUtil.PropertyBindFlags);
                return ((bool)(info.GetValue(this.InnerObject, null)));
            }
        }

        /// <summary>
        /// IsSystemCharacter
        /// </summary>
        public bool IsSystemCharacter
        {
            get
            {
                PropertyInfo info = ReportType.GetProperty("IsSystemCharacter", WrapperUtil.PropertyBindFlags);
                return ((bool)(info.GetValue(this.InnerObject, null)));
            }
        }
    }
}
