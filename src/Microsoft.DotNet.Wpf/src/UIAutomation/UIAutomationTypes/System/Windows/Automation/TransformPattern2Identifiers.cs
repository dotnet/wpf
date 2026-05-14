// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Automation Identifiers for Transform Pattern

using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Contains possible values for the ITransformProvider2.ZoomByUnit method, which zooms the viewport of a control by the specified unit.
    /// </summary>
    public enum ZoomUnit
    {
        /// <summary>
        /// No increase or decrease in zoom.
        /// </summary>
        NoAmount = 0,

        /// <summary>
        /// Decrease zoom by a large decrement.
        /// </summary>
        LargeDecrement = 1,

        /// <summary>
        /// Decrease zoom by a small decrement.
        /// </summary>
        SmallDecrement = 2,

        /// <summary>
        /// Increase zoom by a large increment.
        /// </summary>
        LargeIncrement = 3,

        /// <summary>
        /// Increase zoom by a small increment.
        /// </summary>
        SmallIncrement = 4
    };

    ///<summary>wrapper class for Transform2 pattern </summary>
#if (INTERNAL_COMPILE)
    internal static class TransformPattern2Identifiers
#else
    public static class TransformPattern2Identifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Returns the Transform pattern identifier</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Transform2, "TransformPattern2Identifiers.Pattern");

        /// <summary>Property ID: CanZoom - This window can be zoom</summary>
        public static readonly AutomationProperty CanZoomProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.Transform2CanZoom, "TransformPattern2Identifiers.CanZoomProperty");

        /// <summary>Property ID: ZoomLevelProperty - The zoomlevel of the window</summary>
        public static readonly AutomationProperty ZoomLevelProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.Transform2ZoomLevel, "TransformPattern2Identifiers.ZoomLevelProperty");

        /// <summary>Property ID: ZoomMaximum - This maximum this window can be zoomed</summary>
        public static readonly AutomationProperty ZoomMaximumProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.Transform2ZoomMaximum, "TransformPattern2Identifiers.ZoomMaximumProperty");

        /// <summary>Property ID: ZoomMinimum - This minimum this window can be zoomed</summary>
        public static readonly AutomationProperty ZoomMinimumProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.Transform2ZoomMinimum, "TransformPattern2Identifiers.ZoomMinimumProperty");
		

        #endregion Public Constants and Readonly Fields
    }
}
