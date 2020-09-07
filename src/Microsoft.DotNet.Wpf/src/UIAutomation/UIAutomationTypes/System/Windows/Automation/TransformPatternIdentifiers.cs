// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Transform Pattern

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
       
    ///<summary>wrapper class for Transform pattern </summary>
#if (INTERNAL_COMPILE)
    internal static class TransformPatternIdentifiers
#else
    public static class TransformPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Returns the Transform pattern identifier</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Transform, "TransformPatternIdentifiers.Pattern");

        /// <summary>Property ID: CanMove - This window can be moved</summary>
        public static readonly AutomationProperty CanMoveProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TransformCanMove, "TransformPatternIdentifiers.CanMoveProperty");

        /// <summary>Property ID: CanResize - This window can be resized</summary>
        public static readonly AutomationProperty CanResizeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TransformCanResize, "TransformPatternIdentifiers.CanResizeProperty");

        /// <summary>Property ID: CanRotate - This window can be rotated</summary>
        public static readonly AutomationProperty CanRotateProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TransformCanRotate, "TransformPatternIdentifiers.CanRotateProperty");


        #endregion Public Constants and Readonly Fields
    }
}
