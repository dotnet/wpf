// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Win32.Pointer;
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// Provides access and data from the Windows Interaction Context engine.
    /// 
    /// This gives WPF access to gestures and other features based off WM_POINTER data.
    /// 
    /// This code is copied from Wisptis code in RS2
    /// </summary>
    internal class PointerFlickEngine
    {
        #region Constants

        // Note that the minimum time has implications on app compat. There is a window
        // between the minimum flick time (150 ms) and the WM_QUERYSYSTEMGESTURESTATUS
        // timeout (300ms), so decreasing this threshhold makes it worse
        private const double ThresholdTime = 150.0;
        private const double ThresholdLength = 100.0;

        private const double RelaxedFlickMinimumLength = 400.0;
        private const double RelaxedFlickMaximumLengthRatio = 1.1;
        private const double RelaxedFlickMinimumVelocity = 8.0;
        private const double RelaxedFlickMaximumTime = 300.0;
        private const double RelaxedFlickMaximumStationaryTime = 45.0;
        private const double RelaxedFlickMaxStationaryDispX = 150.0;
        private const double RelaxedFlickMaxStationaryDispY = 150.0;

        private const double PreciseFlickMinimumLength = 800.0;
        private const double PreciseFlickMaximumLengthRatio = 1.01;
        private const double PreciseFlickMinimumVelocity = 19;
        private const double PreciseFlickMaximumTime = 200.0;
        private const double PreciseFlickMaximumStationaryTime = 45.0;
        private const double PreciseFlickMaxStationaryDispX = 0;
        private const double PreciseFlickMaxStationaryDispY = 0;

        #endregion

        #region Helper Structs/Classes

        // 
        internal class FlickResult
        {
            internal Point PhysicalStart { get; set; }    // The starting point in physical coordinates (100ths of mm)
            internal Point TabletStart { get; set; }      // The starting point in tablet coordinates
            internal int PhysicalLength { get; set; }  // The length in physical coordinates (100ths of mm)
            internal int TabletLength { get; set; }  // The length in tablet coordinates
            internal int DirectionDeg { get; set; }         // The direction in degrees (digitizer coordinates)
            internal bool CanBeFlick { get; set; }        // Is it a flick or not
            internal bool IsLengthOk { get; set; }          // Is the stroke length enough to be a flick
            internal bool IsSpeedOk { get; set; }           // Is the speed enough to be a flick
            internal bool IsCurvatureOk { get; set; }       // Is the curvature of the stroke low enough to be a flick
            internal bool IsLiftOk { get; set; }           // Is the lift of the stroke quick enough to be a flick
        }

        // 
        private class FlickRecognitionData
        {
            internal Point PhysicalPoint { get; set; }
            internal double Time { get; set; }               // The time at which the point was received
            internal double Displacement { get; set; }       // The displacement from the previous point
            internal double Velocity { get; set; }           // The velocity between the previous and this point
            internal Point TabletPoint { get; set; }             // The x,y tablet coordinates
        }

        #endregion

        #region Enumerations

        [Flags]
        private enum FlickState
        {
}

        #endregion

        #region Member Variables

        private bool _collectingData;
        private bool _analyzingData;
        private bool _lastPhysicalPointValid;
        private bool _movedEnoughFromPenDown;
        private bool _canDetectFlick;
        private bool _allowPressFlicks;
        private bool _previousFlickDataValid;

        private Point _flickStartPhysical;
        private Point _flickStartTablet;
        private Point _lastPhysicalPoint;

        private PointerStylusDevice _stylusDevice = null;

        private double _distance;
        private double _flickDirectionRadians;
        private double _flickPathDistance;
        private double _flickLength;
        private double _flickTimeLowVelocity;
        private double _flickMaximumStationaryTime;
        private double _flickMaximumLengthRatio;
        private double _flickMinimumLength;
        private double _flickMinimumVelocity;
        private double _flickMaximumStationaryDisplacementX;
        private double _flickMaximumStationaryDisplacementY;
        private double _tolerance;

        private FlickRecognitionData _previousFlickData;

        private Rect _drag;

        #region Timing

        // The tick count between packets
        private double _timePeriod;
        private double _timePeriodAlpha;
        private int _previousTickCount;
        private double _elapsedTime;
        private double _flickTime;
        private double _flickMaximumTime;

        #endregion

        #endregion

        #region Properties

        internal FlickResult Result { get; private set; } = new FlickResult();

        #endregion

        #region Constructor/Initialization

        internal PointerFlickEngine(PointerStylusDevice stylusDevice)
        {
            _stylusDevice = stylusDevice;

            _timePeriod = 8;
            _timePeriodAlpha = .001;
            _collectingData = false;
            _analyzingData = false;
            _previousFlickDataValid = false;
            _allowPressFlicks = true;

            Reset();

            SetTolerance(.5);
        }

        internal void Reset()
        {
            ResetResult();

            _collectingData = false;
            _analyzingData = false;
            _movedEnoughFromPenDown = !_allowPressFlicks;
            _canDetectFlick = true;
            _lastPhysicalPointValid = false;
            _distance = 0;

            _drag = new Rect();

            _flickStartPhysical = new Point();
            _flickStartTablet = new Point();

            _elapsedTime = 0;

            _flickLength = 0;
            _flickDirectionRadians = 0;
            _flickPathDistance = 0;
            _flickTime = 0;
            _flickTimeLowVelocity = 0;

            _previousFlickDataValid = false;
        }

        internal void ResetResult()
        {
            Result.CanBeFlick = true;
            Result.IsLengthOk = false;
            Result.IsSpeedOk = false;
            Result.IsCurvatureOk = false;
            Result.IsLiftOk = false;
            Result.DirectionDeg = 0;
            Result.PhysicalLength = 0;
            Result.TabletLength = 0;
            Result.PhysicalStart = new Point();
            Result.TabletStart = new Point();
        }

        #endregion

        #region Message Processing API

        internal void Update(RawStylusInputReport rsir, bool initial = false)
        {
            // Do not process non-Pen input
            if (_stylusDevice.TabletDevice.Type != TabletDeviceType.Stylus)
            {
                return;
            }

            switch (rsir.Actions)
            {
                case RawStylusActions.Down:
                    {
                        // Always reset on a down.  Pens have one contact point so any down must be
                        // a new pointer and requires fresh tracking.
                        Reset();

                        // From this point on we can use inputs from manipulation tracked against the 
                        // current pointer id in order to tell if we need to update flick information.
                        _collectingData = true;

                        ProcessPacket(rsir, true);

                        if (_analyzingData)
                        {
                            Analyze(decide: false);
                        }
                    }
                    break;
                case RawStylusActions.Up:
                    {
                        if (_canDetectFlick)
                        {
                            ProcessPacket(rsir, false);

                            if (_analyzingData)
                            {
                                Analyze(decide: true);
                            }
                            else
                            {
                                // Set Flick Result To Can't Be Flick
                            }
                        }

                        _collectingData = false;
                        _analyzingData = false;
                    }
                    break;
                case RawStylusActions.Move:
                    {
                        if (_canDetectFlick)
                        {
                            ProcessPacket(rsir, initial);

                            if (_analyzingData)
                            {
                                Analyze(decide: false);
                            }
                        }
                    }
                    break;
            }
        }

        #endregion

        #region Data Processing/Analysis

        private void UpdateTimePeriod(int tickCount, bool initial)
        {
            if (!_collectingData)
            {
                return;
            }

            if (!initial)
            {
                double timeDelta = (double)(tickCount - _previousTickCount);

                if (timeDelta >= 0.0 && timeDelta <= 1000.0)
                {
                    _timePeriod = (1.0 - _timePeriodAlpha) * _timePeriod + _timePeriodAlpha * timeDelta;
                }
            }

            _previousTickCount = tickCount;
        }

        private void ProcessPacket(RawStylusInputReport rsir, bool initial)
        {
            UpdateTimePeriod(rsir.Timestamp, initial);

            if (!_collectingData)
            {
                return;
            }

            Point tabletPoint = rsir.GetLastTabletPoint();

            // Get the device coordinates in HiMetric
            Point physPoint = GetPhysicalCoordinates(tabletPoint);

            if (initial)
            {
                _flickStartPhysical = physPoint;
                _flickStartTablet = tabletPoint;
                _elapsedTime = 0;

                SetStableRect();
            }
            else
            {
                _elapsedTime += _timePeriod;
            }

            if (!_movedEnoughFromPenDown)
            {
                if (_lastPhysicalPointValid)
                {
                    double dist = Distance(_lastPhysicalPoint, physPoint);
                    _distance += dist;

                    // If at any time a fair distance is moved from packet to packet
                    // or the entire distance traveled thus far is greater than the side
                    // of m_rcdrag, we say the pen has moved enough and start the flick
                    // detection
                    if ((dist > PreciseFlickMinimumVelocity) || (dist >= _flickMaximumStationaryDisplacementX))
                    {
                        _movedEnoughFromPenDown = true;
                    }
                }

                if (!_movedEnoughFromPenDown)
                {
                    // If the pen has left the m_rcDrag rect or if adequate time
                    // has elapsed, start the flick detection
                    if (!_drag.Contains(physPoint) || (_elapsedTime > 3000))
                    {
                        _movedEnoughFromPenDown = true;
                    }
                }

                _lastPhysicalPoint = physPoint;
                _lastPhysicalPointValid = true;
            }

            if (_movedEnoughFromPenDown && !_analyzingData)
            {
                CheckWithThreshold(physPoint);
            }

            if (_analyzingData)
            {
                AddPoint(physPoint, tabletPoint);
            }
        }

        private void Analyze(bool decide)
        {
            Result.CanBeFlick = true;
            Result.IsLengthOk = true;
            Result.IsSpeedOk = true;
            Result.IsCurvatureOk = true;
            Result.IsLiftOk = true;

            Result.DirectionDeg = Convert.ToInt32(RadiansToDegrees(_flickDirectionRadians));
            Result.PhysicalStart = _flickStartPhysical;
            Result.TabletStart = _flickStartTablet;

            Result.PhysicalLength = Convert.ToInt32(.5 + Distance(Result.PhysicalStart, _previousFlickData.PhysicalPoint));
            Result.TabletLength = Convert.ToInt32(.5 + Distance(Result.TabletStart, _previousFlickData.TabletPoint));

            double flickPathDifference = _flickPathDistance - _flickLength;

            double flickLengthRatio = 1.0;

            if (_flickLength > 0)
            {
                flickLengthRatio = _flickPathDistance / _flickLength;
            }

            if (_flickTimeLowVelocity > _flickMaximumStationaryTime)
            {
                Result.CanBeFlick = false;
                Result.IsLiftOk = false;
            }

            if (_flickTime > _flickMaximumTime)
            {
                Result.CanBeFlick = false;
                Result.IsSpeedOk = false;
            }

            if ((flickLengthRatio > _flickMaximumLengthRatio && _flickLength > 500 && flickPathDifference > 200) || flickPathDifference > 300)
            {
                Result.CanBeFlick = false;
                Result.IsCurvatureOk = false;
            }

            if (_flickLength < _flickMinimumLength && decide)
            {
                Result.CanBeFlick = false;
                Result.IsLengthOk = false;
            }

            if (!Result.CanBeFlick || decide)
            {
                _collectingData = false;
                _analyzingData = false;
            }
        }

        private void AddPoint(Point physicalPoint, Point tabletPoint)
        {
            FlickRecognitionData newData = new FlickRecognitionData()
            {
                PhysicalPoint = physicalPoint,
                TabletPoint = tabletPoint,
                Time = 0,
                Displacement = 0,
                Velocity = 0,
            };

            if (_previousFlickDataValid)
            {
                newData.Time = _previousFlickData.Time + _timePeriod;
                newData.Displacement = Distance(physicalPoint, _previousFlickData.PhysicalPoint);
                newData.Velocity = newData.Displacement / _timePeriod;
            }
            else
            {
                _flickPathDistance = Distance(physicalPoint, _flickStartPhysical);
            }

            _flickLength = Distance(physicalPoint, _flickStartPhysical);

            _flickDirectionRadians = Math.Atan2(newData.PhysicalPoint.Y - _flickStartPhysical.Y, newData.PhysicalPoint.X - _flickStartPhysical.X);

            _flickPathDistance += newData.Displacement;

            _flickTime += _timePeriod;

            _flickTimeLowVelocity += (newData.Velocity < _flickMinimumVelocity) ? _timePeriod : 0;

            _previousFlickDataValid = true;
            _previousFlickData = newData;
        }

        #endregion

        #region Utility

        private void CheckWithThreshold(Point physicalPoint)
        {
            _analyzingData = Distance(physicalPoint, _flickStartPhysical) > ThresholdLength
                || _elapsedTime > ThresholdTime;
        }

        private void SetStableRect()
        {
            if (_collectingData)
            {
                _drag = new Rect(_flickStartPhysical, new Size(_flickMaximumStationaryDisplacementX, _flickMaximumStationaryDisplacementY));
            }
        }

        private double RadiansToDegrees(double radians)
        {
            return ((180 * radians / Math.PI) + 360) % 360;
        }

        /// <summary>
        /// Distance formula between two points
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The distance between the points</returns>
        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((p1.X - p2.X), 2) + Math.Pow((p1.Y - p2.Y), 2));
        }

        /// <summary>
        /// Converts a tablet point (from the stylus point definition of X and Y).
        /// To a pure device point in HiMetric units.
        /// </summary>
        /// <param name="tabletPoint">The point to convert</param>
        /// <returns>A physical device point in HiMetric units</returns>
        Point GetPhysicalCoordinates(Point tabletPoint)
        {
            // DeviceRect is a HiMetric unit RECT reported directly from WM_POINTER
            double deviceSizeX = _stylusDevice.PointerTabletDevice.DeviceInfo.DeviceRect.right - _stylusDevice.PointerTabletDevice.DeviceInfo.DeviceRect.left;
            double deviceSizeY = _stylusDevice.PointerTabletDevice.DeviceInfo.DeviceRect.top - _stylusDevice.PointerTabletDevice.DeviceInfo.DeviceRect.bottom;

            // The TabletSize is determined by the X and Y tablet Max/Min as reported from WM_POINTER
            double tabletSizeX = _stylusDevice.PointerTabletDevice.DeviceInfo.SizeInfo.TabletSize.Width;
            double tabletSizeY = _stylusDevice.PointerTabletDevice.DeviceInfo.SizeInfo.TabletSize.Height;

            return new Point(((tabletPoint.X * deviceSizeX) / tabletSizeX), ((tabletPoint.Y * deviceSizeY) / tabletSizeY));
        }

        private bool SetTolerance(double tolerance)
        {
            bool result = tolerance > 0 && tolerance < 1;

            if (result)
            {
                // Use a linear fit between the Relaxed and Precise settings for each value
                _flickMinimumLength = tolerance * PreciseFlickMinimumLength + (1.0 - tolerance) * RelaxedFlickMinimumLength;
                _flickMaximumLengthRatio = tolerance * PreciseFlickMaximumLengthRatio + (1.0 - tolerance) * RelaxedFlickMaximumLengthRatio;
                _flickMinimumVelocity = tolerance * PreciseFlickMinimumVelocity + (1.0 - tolerance) * RelaxedFlickMinimumVelocity;
                _flickMaximumTime = tolerance * PreciseFlickMaximumTime + (1.0 - tolerance) * RelaxedFlickMaximumTime;
                _flickMaximumStationaryTime = tolerance * PreciseFlickMaximumStationaryTime + (1.0 - tolerance) * RelaxedFlickMaximumStationaryTime;
                _flickMaximumStationaryDisplacementX = tolerance * PreciseFlickMaxStationaryDispX + (1.0 - tolerance) * RelaxedFlickMaxStationaryDispX;
                _flickMaximumStationaryDisplacementY = tolerance * PreciseFlickMaxStationaryDispY + (1.0 - tolerance) * RelaxedFlickMaxStationaryDispY;

                // Cache Tolerance
                _tolerance = tolerance;
            }

            return result;
        }

        #endregion
    }
}
