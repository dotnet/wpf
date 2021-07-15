// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A trace logging manager for collecting debug information. 
//              See doc comments below for more details. 
//
//              The underlying logging infrastructure does the right thing with
//              respect to user opt-in for CEIP. 
//

using MS.Internal.Telemetry;
using System.Collections;
using System.Diagnostics.Tracing;

namespace System.Windows.Documents
{
    // The main definition for Speller is found in Speller.cs. 
    // This file holds only the definition for TextMapOffsetErrorLogger.
    internal partial class Speller
    {
        /// <summary>
        /// <see cref="TextMap.MapOffsetToPosition(int)"/> sometimes fails with an assertion 
        /// because the offset passed to it is outside the range [0.._textLength]. 
        /// This logger will be used to collected contextual information just before such 
        /// failures occur and log them so that the data coudl enable debugging and 
        /// a future fix. 
        /// </summary>
        /// <remarks>
        /// Once a future fix is done, this class and any references to it can be 
        /// safely removed. 
        /// 
        /// <see cref="Speller"/> has been marked as partial to enable
        /// this class to be declared in a separate source file - reverting this 
        /// change should also be considered. 
        /// 
        /// <see cref="TextMapOffsetErrorLogger"/> is declared as a nested type 
        /// within Speller because we need access to the internals of <see cref="Speller.TextMap"/>
        /// </remarks>
        private partial class TextMapOffsetErrorLogger
        {
            private static readonly string TextMapOffsetError = "TextMapOffsetError";

            private DebugInfo _debugInfo;
            public enum CalculationModes : int
            {
                ContentPosition = 0,
                ContextPosition = 1
            };

            /// <summary>
            /// In the calculations within <see cref="ExpandToWordBreakAndContext"/>, the smallest value 
            /// expected for various indices and offsets is -1. Therefore -2 can act as a handy way to 
            /// represent an "unset" or "uninitialized" value;
            /// </summary>
            private static readonly int UnsetValue = -2;

            public TextMapOffsetErrorLogger(LogicalDirection direction, TextMap textMap, ArrayList segments, int positionInSegmentList, int leftWordBreak, int rightWordBreak, int contentOffset)
            {
                _debugInfo = new DebugInfo
                {
                    Direction = direction.ToString(),
                    SegmentCount = segments.Count,
                    SegmentStartsAndLengths = new SegmentInfo[segments.Count],

                    PositionInSegmentList = positionInSegmentList,
                    LeftWordBreak = leftWordBreak,
                    RightWordBreak = rightWordBreak,

                    ContentOffSet = contentOffset,
                    ContextOffset = UnsetValue,

                    CalculationMode = CalculationModes.ContentPosition,

                    TextMapText = string.Join(string.Empty, textMap.Text),
                    TextMapTextLength = textMap.TextLength,
                    TextMapContentStartOffset = textMap.ContentStartOffset,
                    TextMapContentEndOffset = textMap.ContentEndOffset
                };

                for (int i = 0; i < segments.Count; i++)
                {
                    var textSegment = segments[i] as SpellerInteropBase.ITextRange;
                    if (textSegment != null)
                    {
                        _debugInfo.SegmentStartsAndLengths[i] = new SegmentInfo
                        {
                            Start = textSegment.Start,
                            Length = textSegment.Length
                        };
                    }
                }
            }

            public int ContextOffset
            {
                set
                {
                    _debugInfo.ContextOffset = value;
                    _debugInfo.CalculationMode = CalculationModes.ContextPosition;
                }
            }

            public void LogDebugInfo()
            {
                int offset = (_debugInfo.CalculationMode == CalculationModes.ContentPosition ? _debugInfo.ContentOffSet : _debugInfo.ContextOffset);

                // Error condition occurs when the offsets are outside the valid range.
                // The next call to TextMap.MapOffsetToPosition would fail an Invariant.Assert
                // We log relevant details here.
                if ((offset < 0) || (offset > _debugInfo.TextMapTextLength))
                {
                    var logger = MS.Internal.Telemetry.PresentationFramework.TraceLoggingProvider.GetProvider();

                    // The data we are about to log contains text being spell-checked. Although this is 
                    // not normally expected to be PII, it might very well contain such information. 
                    // We'd want to prevent Part A fields that might allow identification or 
                    // cross-correlation would be dropped from the event.
                    var eventSourceOptions = new EventSourceOptions
                    {
                        Keywords = MS.Internal.Telemetry.PresentationFramework.TelemetryEventSource.MeasuresKeyword, 
                        Tags     = MS.Internal.Telemetry.PresentationFramework.TelemetryEventSource.DropPii
                    };

                    logger?.Write<DebugInfo>(TextMapOffsetError, eventSourceOptions, _debugInfo);
                }
            }

            /// <summary>
            /// Structure encoding contextual debug information 
            /// during execution of TextMap.MapOffsetPosition
            /// coming from within ExpandToWordBreakAndContext.
            /// </summary>
            /// <remarks>
            /// Because this type is marked as <see cref="System.Diagnostics.Tracing.EventDataAttribute"/>, 
            /// the properties must be either a simple framework type such as Int16, Int32, Int64, String, Guid, 
            /// DateTime or DateTimeOffset, or an array of primitive types. 
            /// </remarks>
            [EventData]
            private struct DebugInfo
            {
                #region Contextual Information from ExpandToWordBreakAndContext

                /// <summary>
                /// string representation of a <see cref="LogicalDirection"/> instance. 
                /// Values could be "Backward" or "Forward"
                /// </summary>
                public string Direction { get; set; }

                /// <summary>
                /// Number of segments (i.e., words) returned by the call to <see cref="SpellerInteropBase.EnumTextSegments"/>
                /// </summary>
                public int SegmentCount { get; set; }

                /// <summary>
                /// Each segment returned by <see cref="SpellerInteropBase.EnumTextSegments"/> is an <see cref="SpellerInteropBase.ITextRange"/> 
                /// instance containing <see cref="SpellerInteropBase.ITextRange.Start"/> and <see cref="SpellerInteropBase.ITextRange.Length"/> values. 
                /// This array represents the list of Start, Length values for each segment. 
                /// </summary>
                public SegmentInfo[] SegmentStartsAndLengths { get; set; }

                /// <summary>
                /// This is the value returned by <see cref="FindPositionInSegmentList"/>. 
                /// The crash dumps we have seen typically have this value set to -1 or 0.
                /// </summary>
                public int PositionInSegmentList { get; set; }

                /// <summary>
                /// 'leftWordBreak' returned by <see cref="FindPositionInSegmentList"/>
                /// </summary>
                public int LeftWordBreak { get; set; }

                /// <summary>
                /// 'rightWordBreak' returned by <see cref="FindPositionInSegmentList"/>
                /// </summary>
                public int RightWordBreak { get; set; }

                /// <summary>
                /// 'contentOffset' value calculated within <see cref="ExpandToWordBreakAndContext"/>
                /// </summary>
                public int ContentOffSet { get; set; }

                /// <summary>
                /// 'contextOffset' value calculated within <see cref="ExpandToWordBreakAndContext"/>.
                /// This is only set and valid when <see cref="CalculationMode"/> is "ContextOffset". 
                /// </summary>
                public int ContextOffset { get; set; }

                /// <summary>
                /// Within <see cref="ExpandToWordBreakAndContext"/>, <see cref="TextMap.MapOffsetToPosition"/>
                /// is called twice - once to calculate 'contentPosition' and again to calcualte 'contextPosition'. 
                /// This property identifies which value is being calculated. 
                /// </summary>
                public CalculationModes CalculationMode { get; set; }

                #endregion

                #region Contextual Information from TextMap instance
                /// <summary>
                /// <see cref="TextMap.Text"/> property of the 'textMap' instance 
                /// </summary>
                public string TextMapText { get; set; }

                /// <summary>
                /// <see cref="TextMap.TextLength"/> property of the 'textMap' instance
                /// </summary>
                public int TextMapTextLength { get; set; }

                /// <summary>
                /// <see cref="TextMap.ContentStartOffset"/> property of the 'textMap' instance
                /// </summary>
                public int TextMapContentStartOffset { get; set; }

                /// <summary>
                /// <see cref="TextMap.ContentEndOffset"/> property of the 'textMap' instance.
                /// </summary>
                public int TextMapContentEndOffset { get; set; }

                #endregion
            }

            /// <summary>
            /// Represents the Start and Length fields of a <see cref="SpellerInteropBase.ITextRange"/>
            /// instance.
            /// </summary>
            [EventData]
            private struct SegmentInfo
            {
                public int Start { get; set; }
                public int Length { get; set; }
            }
        }
    }
}
