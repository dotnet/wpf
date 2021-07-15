// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: Background format information
//


using System;
using MS.Internal.Documents; // FlowDocumentFormatter
using System.Windows.Threading; // DispatcherTimer

namespace MS.Internal.PtsHost
{
    internal sealed class BackgroundFormatInfo
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Structural Cache contructor
        /// </summary>
        internal BackgroundFormatInfo(StructuralCache structuralCache) 
        { 
            _structuralCache = structuralCache;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Updates background layout information from a structuralCache
        /// </summary>
        internal void UpdateBackgroundFormatInfo()
        {
            _cpInterrupted = -1;
            _lastCPUninterruptible = 0;
            _doesFinalDTRCoverRestOfText = false;

            _cchAllText = _structuralCache.TextContainer.SymbolCount;

            if(_structuralCache.DtrList != null)
            {
                int positionsAdded = 0;

                // Sum for all dtrs but the last
                for(int dtrIndex = 0; dtrIndex < _structuralCache.DtrList.Length - 1; dtrIndex++)
                {
                    positionsAdded += _structuralCache.DtrList[dtrIndex].PositionsAdded - _structuralCache.DtrList[dtrIndex].PositionsRemoved;
                }

                DirtyTextRange dtrLast = _structuralCache.DtrList[_structuralCache.DtrList.Length - 1];

                if((dtrLast.StartIndex + positionsAdded + dtrLast.PositionsAdded) >= _cchAllText)
                {
                    _doesFinalDTRCoverRestOfText = true;
                    _lastCPUninterruptible = dtrLast.StartIndex + positionsAdded;
                }
            }
            else
            {
                _doesFinalDTRCoverRestOfText = true;
            }

            // And set a good stop time for formatting
            _backgroundFormatStopTime = DateTime.UtcNow.AddMilliseconds(_stopTimeDelta);
        }

        /// <summary>
        /// This method is called after user input.
        /// Temporarily disable background layout to cut down on ui latency.
        /// </summary>
        internal void ThrottleBackgroundFormatting()
        {
            if (_throttleBackgroundTimer == null)
            {
                // Start up a timer.  Until the timer fires, we'll disable
                // all background layout.  This leaves the control responsive
                // to user input.
                _throttleBackgroundTimer = new DispatcherTimer(DispatcherPriority.Background);
                _throttleBackgroundTimer.Interval = new TimeSpan(0, 0, (int)_throttleBackgroundSeconds);
                _throttleBackgroundTimer.Tick += new EventHandler(OnThrottleBackgroundTimeout);
            }
            else
            {
                // Reset the timer.
                _throttleBackgroundTimer.Stop();
            }

            _throttleBackgroundTimer.Start();
        }

        /// <summary>
        /// Run one iteration of background formatting.  Currently that simply requires
        /// that we invalidate the content.
        /// </summary>
        internal void BackgroundFormat(IFlowDocumentFormatter formatter, bool ignoreThrottle)
        {
            if (_throttleBackgroundTimer == null)
            {
                formatter.OnContentInvalidated(true);
            }
            else if (ignoreThrottle)
            {
                OnThrottleBackgroundTimeout(null, EventArgs.Empty);
            }
            else
            {
                // If we had recent user input, wait until the timeout passes
                // to invalidate.
                _pendingBackgroundFormatter = formatter;
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Last CP Uninterruptible
        /// </summary>
        internal int LastCPUninterruptible { get { return _lastCPUninterruptible; } }

        /// <summary>
        /// Stop time for background formatting timeslice
        /// </summary>
        internal DateTime BackgroundFormatStopTime { get { return _backgroundFormatStopTime; } }

        /// <summary>
        /// Cch of all text in container
        /// </summary>
        internal int CchAllText { get { return _cchAllText; } }

        /// <summary>
        /// Whether background layout is globally enabled
        /// </summary>
        static internal bool IsBackgroundFormatEnabled { get { return _isBackgroundFormatEnabled; } }

        /// <summary>
        /// Does the final dtr extend through the sum of the text
        /// </summary>
        internal bool DoesFinalDTRCoverRestOfText { get { return _doesFinalDTRCoverRestOfText; } }

        /// <summary>
        /// Current last cp formatted
        /// </summary>
        internal int CPInterrupted { get { return _cpInterrupted; } set { _cpInterrupted = value; } }

        /// <summary>
        /// Height of the viewport
        /// </summary>
        internal double ViewportHeight
        {
            get { return _viewportHeight; }
            set { _viewportHeight = value; }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // Callback for the background layout throttle timer.
        // Resumes backgound layout.
        private void OnThrottleBackgroundTimeout(object sender, EventArgs e)
        {
            _throttleBackgroundTimer.Stop();
            _throttleBackgroundTimer = null;

            if (_pendingBackgroundFormatter != null)
            {
                BackgroundFormat(_pendingBackgroundFormatter, true /* ignoreThrottle */);
                _pendingBackgroundFormatter = null;
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        //-------------------------------------------------------------------
        // Height of the viewport.
        //-------------------------------------------------------------------
        private double _viewportHeight;

        //-------------------------------------------------------------------
        // Does the final DTR cover the entirety of the range?
        //-------------------------------------------------------------------
        private bool _doesFinalDTRCoverRestOfText;

        //-------------------------------------------------------------------
        // What is the last uninterruptible cp ?
        //-------------------------------------------------------------------
        private int _lastCPUninterruptible;

        //-------------------------------------------------------------------
        // Stop time for background layout
        // Used for background layout
        //-------------------------------------------------------------------
        private DateTime _backgroundFormatStopTime;

        //-------------------------------------------------------------------
        // Cch of all text in container
        //-------------------------------------------------------------------
        private int _cchAllText;

        //-------------------------------------------------------------------
        // Cp Interrupted
        // Used for background layout
        //-------------------------------------------------------------------
        private int _cpInterrupted;

        //-------------------------------------------------------------------
        // Global enabling flag for whether background format is enabled.
        //-------------------------------------------------------------------
        private static bool _isBackgroundFormatEnabled = true;

        //-------------------------------------------------------------------
        // Structural cache
        //-------------------------------------------------------------------
        private StructuralCache _structuralCache;

        //-------------------------------------------------------------------
        // Time after a user input until which we use a minimal time slice
        // to remain responsive to future input.
        //-------------------------------------------------------------------
        private DateTime _throttleTimeout = DateTime.UtcNow;

        //-------------------------------------------------------------------
        // Timer used to disable background layout during user interaction.
        //-------------------------------------------------------------------
        private DispatcherTimer _throttleBackgroundTimer;

        //-------------------------------------------------------------------
        // Holds the formatter to invalidate when _throttleBackgroundTimer
        // fires.
        //-------------------------------------------------------------------
        IFlowDocumentFormatter _pendingBackgroundFormatter;

        //-------------------------------------------------------------------
        // Number of seconds to disable background layout after receiving
        // user input.
        //-------------------------------------------------------------------
        private const uint _throttleBackgroundSeconds = 2;

        //-------------------------------------------------------------------
        // Max time slice (ms) for one background format iteration.
        //-------------------------------------------------------------------
        private const uint _stopTimeDelta = 200;

        #endregion Private Fields
    }
}
