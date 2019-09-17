// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Input.StylusPlugIns;
using System.Security;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Internal;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;
using System.Windows.Input.StylusWisp;

namespace System.Windows.Input
{
    /// <summary>
    ///     The RawStylusInputReport class encapsulates the raw input provided
    ///     from a stylus.
    /// </summary>
    /// <remarks>
    ///     It is important to note that the InputReport class only contains
    ///     blittable types.  This is required so that the report can be
    ///     marshalled across application domains.
    /// </remarks>
    internal class RawStylusInputReport : InputReport
    {
        #region Member Variables

        /// <summary>
        /// The actions represent by this input report
        /// </summary>
        RawStylusActions _actions;

        /// <summary>
        /// The id of the tablet associated with this input report
        /// </summary>
        int _tabletDeviceId;

        /// <summary>
        /// The id of the stylus associated with this input report
        /// </summary>
        int _stylusDeviceId;

        /// <summary>
        /// DevDiv: 652804 - Used show status in StylusInputQueue
        /// </summary>
        bool _isQueued; 

        /// <summary>
        /// The raw data for this input report
        /// </summary>
        int[] _data;

        /// <summary>
        /// cached value looked up from _stylusDeviceId
        /// </summary>
        StylusDevice _stylusDevice;

        /// <summary>
        /// The raw input used for stylus plugins
        /// </summary>
        SecurityCriticalDataForSet<RawStylusInput> _rawStylusInput;

        /// <summary>
        /// Set from StylusDevice.Synchronize.
        /// </summary>
        bool _isSynchronize; 

        /// <summary>
        /// Function to return the StylusPointDescription for the device associated with
        /// this input report.
        /// </summary>
        Func<StylusPointDescription> _stylusPointDescGenerator;

        #endregion

        #region Properties

        internal RawStylusInput RawStylusInput
        {
            get { return _rawStylusInput.Value; }

            set { _rawStylusInput.Value = value; }
        }

        internal bool Synchronized
        {
            get { return _isSynchronize; }
            set { _isSynchronize = value; }
        }

        /// <summary>
        ///     Read-only access to the set of actions that were reported.
        /// </summary>
        internal RawStylusActions Actions { get { return _actions; } }

        /// <summary>
        ///     Read-only access to stylus context id that reported the data.
        /// </summary>
        internal int TabletDeviceId { get { return _tabletDeviceId; } }

        /// <summary>
        ///     Read-only access to stylus context id that reported the data.
        /// </summary>
        internal PenContext PenContext
        {
            get;
            private set;
        }

        /// <summary>
        ///     Read-only access to stylus context id that reported the data.
        /// </summary>
        internal StylusPointDescription StylusPointDescription
        {
            get { return _stylusPointDescGenerator(); }
        }

        /// <summary>
        ///     Read-only access to stylus device id that reported the data.
        /// </summary>
        internal int StylusDeviceId { get { return _stylusDeviceId; } }

        /// <summary>
        /// The StylusDevice associated with this input report.
        /// </summary>
        internal StylusDevice StylusDevice
        {
            get { return _stylusDevice; }
            set { _stylusDevice = value; }
        }

        /// <summary>
        /// DevDiv:652804
        /// Determine if this item is currently queued in the StylusInputQueue
        /// </summary>
        internal bool IsQueued
        {
            get { return _isQueued; }
            set { _isQueued = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs an instance of the RawStylusInputReport class.
        /// </summary>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="inputSource">
        ///     The PresentationSource over which the stylus moved.
        /// </param>
        /// <param name="penContext">
        ///     The PenContext.
        /// </param>
        /// <param name="actions">
        ///     The set of actions being reported.
        /// </param>
        /// <param name="tabletDeviceId">
        ///     Tablet device id.
        /// </param>
        /// <param name="stylusDeviceId">
        ///     Stylus device id.
        /// </param>
        /// <param name="data">
        ///     Raw stylus data.
        /// </param>
        internal RawStylusInputReport(
            InputMode mode,
            int timestamp,
            PresentationSource inputSource,
            PenContext penContext,
            RawStylusActions actions,
            int tabletDeviceId,
            int stylusDeviceId,
            int[] data)
            : this(mode, timestamp, inputSource, actions, () => { return penContext.StylusPointDescription; }, tabletDeviceId, stylusDeviceId, data)
        {
            // Validate parameters
            if (!RawStylusActionsHelper.IsValid(actions))
            {
                throw new InvalidEnumArgumentException(SR.Get(SRID.Enum_Invalid, nameof(actions)));
            }
            if (data == null && actions != RawStylusActions.InRange)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _actions = actions;
            _data = data;
            _isSynchronize = false;
            _tabletDeviceId = tabletDeviceId;
            _stylusDeviceId = stylusDeviceId;
            PenContext = penContext;
        }

        /// <summary>
        ///     Constructs an instance of the RawStylusInputReport class.
        /// </summary>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="inputSource">
        ///     The PresentationSource over which the stylus moved.
        /// <param name="actions">
        ///     The set of actions being reported.
        /// </param>
        ///  /// </param>
        /// <param name="stylusPointDescGenerator">
        ///     Function to generate the stylus point description.
        /// </param>
        /// <param name="tabletDeviceId">
        ///     Tablet device id.
        /// </param>
        /// <param name="stylusDeviceId">
        ///     Stylus device id.
        /// </param>
        /// <param name="data">
        ///     Raw stylus data.
        /// </param>
        internal RawStylusInputReport(
            InputMode mode,
            int timestamp,
            PresentationSource inputSource,
            RawStylusActions actions,
            Func<StylusPointDescription> stylusPointDescGenerator,
            int tabletDeviceId,
            int stylusDeviceId,
            int[] data)
            : base(inputSource, InputType.Stylus, mode, timestamp)
        {
            // Validate parameters
            if (!RawStylusActionsHelper.IsValid(actions))
            {
                throw new InvalidEnumArgumentException(SR.Get(SRID.Enum_Invalid, nameof(actions)));
            }
            if (data == null && actions != RawStylusActions.InRange)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _actions = actions;
            _stylusPointDescGenerator = stylusPointDescGenerator;
            _data = data;
            _isSynchronize = false;
            _tabletDeviceId = tabletDeviceId;
            _stylusDeviceId = stylusDeviceId;
        }

        #endregion

        #region Internal API

        /// <summary>
        ///     Read-only access to the raw data that was reported.
        /// </summary>
        internal int[] GetRawPacketData()
        {
            if (_data == null)
                return null;
            return (int[])_data.Clone();
        }

        internal Point GetLastTabletPoint()
        {
            int packetLength = StylusPointDescription.GetInputArrayLengthPerPoint();
            int lastXIndex = _data.Length - packetLength;
            return new Point(_data[lastXIndex], _data[lastXIndex + 1]);
        }

        internal int[] Data
        {
            get { return _data; }
        }

        #endregion
    }
}
