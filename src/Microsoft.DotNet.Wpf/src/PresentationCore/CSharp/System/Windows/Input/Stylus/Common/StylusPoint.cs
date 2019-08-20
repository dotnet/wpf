// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS.Utility;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// Represents a single sampling point from a stylus input device
    /// </summary>
    public struct StylusPoint : IEquatable<StylusPoint>
    {
        internal static readonly float DefaultPressure = 0.5f;


        private double _x;
        private double _y;
        private float _pressureFactor;
        private int[] _additionalValues;
        private StylusPointDescription _stylusPointDescription;

        #region Constructors
        /// <summary>
        /// StylusPoint
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public StylusPoint(double x, double y)
            : this(x, y, DefaultPressure, null, null, false, false)
        {
        }

        /// <summary>
        /// StylusPoint
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="pressureFactor">pressureFactor</param>
        public StylusPoint(double x, double y, float pressureFactor)
            : this(x, y, pressureFactor, null, null, false, true)
        {
        }
        

        /// <summary>
        /// StylusPoint
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="pressureFactor">pressureFactor</param>
        /// <param name="stylusPointDescription">stylusPointDescription</param>
        /// <param name="additionalValues">additionalValues</param>
        public StylusPoint(double x, double y, float pressureFactor, StylusPointDescription stylusPointDescription, int[] additionalValues)
            : this(x, y, pressureFactor, stylusPointDescription, additionalValues, true, true)
        {
        }

        /// <summary>
        /// internal ctor
        /// </summary>
        internal StylusPoint(   
            double x, 
            double y, 
            float pressureFactor, 
            StylusPointDescription stylusPointDescription, 
            int[] additionalValues, 
            bool validateAdditionalData,
            bool validatePressureFactor)
        {
            if (Double.IsNaN(x))
            {
                throw new ArgumentOutOfRangeException("x", SR.Get(SRID.InvalidStylusPointXYNaN));
            }
            if (Double.IsNaN(y))
            {
                throw new ArgumentOutOfRangeException("y", SR.Get(SRID.InvalidStylusPointXYNaN));
            }


            //we don't validate pressure when called by StylusPointDescription.Reformat
            if (validatePressureFactor &&
                (pressureFactor == Single.NaN || pressureFactor < 0.0f || pressureFactor > 1.0f))
            {
                throw new ArgumentOutOfRangeException("pressureFactor", SR.Get(SRID.InvalidPressureValue));
            }
            //
            // only accept values between MaxXY and MinXY
            // we don't throw when passed a value outside of that range, we just silently trunctate
            //
            _x = GetClampedXYValue(x);
            _y = GetClampedXYValue(y);
            _stylusPointDescription = stylusPointDescription;
            _additionalValues = additionalValues;
            _pressureFactor = pressureFactor;

            if (validateAdditionalData)
            {
                //
                // called from the public verbose ctor
                //
                if (null == stylusPointDescription)
                {
                    throw new ArgumentNullException("stylusPointDescription");
                }

                //
                // additionalValues can be null if PropertyCount == 3 (X, Y, P)
                //
                if (stylusPointDescription.PropertyCount > StylusPointDescription.RequiredCountOfProperties &&
                    null == additionalValues)
                {
                    throw new ArgumentNullException("additionalValues");
                }


                if (additionalValues != null)
                {
                    ReadOnlyCollection<StylusPointPropertyInfo> properties
                        = stylusPointDescription.GetStylusPointProperties();

                    int expectedAdditionalValues = properties.Count - StylusPointDescription.RequiredCountOfProperties; //for x, y, pressure
                    if (additionalValues.Length != expectedAdditionalValues)
                    {
                        throw new ArgumentException(SR.Get(SRID.InvalidAdditionalDataForStylusPoint), "additionalValues");
                    }

                    //
                    // any buttons passed in must each be in their own int.  We need to 
                    // pack them all into one int here
                    //
                    int[] newAdditionalValues =
                        new int[stylusPointDescription.GetExpectedAdditionalDataCount()];

                    _additionalValues = newAdditionalValues;
                    for (int i = StylusPointDescription.RequiredCountOfProperties, j = 0; i < properties.Count; i++, j++)
                    {
                        //
                        // use SetPropertyValue, it validates buttons, but does not copy the 
                        // int[] on writes (since we pass the bool flag)
                        //
                        SetPropertyValue(properties[i], additionalValues[j], false/*copy on write*/);
                    }
                }
            } 
        }



        #endregion Constructors

        /// <summary>
        /// The Maximum X or Y value supported for backwards compatibility with previous inking platforms
        /// </summary>
        public static readonly double MaxXY = 81164736.28346430d;

        /// <summary>
        /// The Minimum X or Y value supported for backwards compatibility with previous inking platforms
        /// </summary>
        public static readonly double MinXY = -81164736.32125960d;

        /// <summary>
        /// X
        /// </summary>
        public double X 
        {
            get { return _x; }
            set 
            {
                if (Double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException("X", SR.Get(SRID.InvalidStylusPointXYNaN));
                }
                //
                // only accept values between MaxXY and MinXY
                // we don't throw when passed a value outside of that range, we just silently trunctate
                //
                _x = GetClampedXYValue(value); 
            }
        }

        /// <summary>
        /// Y
        /// </summary>
        public double Y
        {
            get { return _y; }
            set 
            {
                if (Double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException("Y", SR.Get(SRID.InvalidStylusPointXYNaN));
                }
                //
                // only accept values between MaxXY and MinXY
                // we don't throw when passed a value outside of that range, we just silently trunctate
                //
                _y = GetClampedXYValue(value); 
            }
        }

        /// <summary>
        /// PressureFactor.  A value between 0.0 (no pressure) and 1.0 (max pressure)
        /// </summary>
        public float PressureFactor
        {
            get 
            {
                //
                // note that pressure can be stored a > 1 or < 0. 
                // we need to clamp if this is the case
                //
                if (_pressureFactor > 1.0f)
                {
                    return 1.0f;
                }
                if (_pressureFactor < 0.0f)
                {
                    return 0.0f;
                }
                return _pressureFactor;
            }
            set 
            {
                if (value < 0.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException("PressureFactor", SR.Get(SRID.InvalidPressureValue));
                }
                _pressureFactor = value; 
            }
        }

        /// <summary>
        /// Describes the properties this StylusPoint contains
        /// </summary>
        public StylusPointDescription Description 
        {
            get
            {
                if (null == _stylusPointDescription)
                {
                    // this can happen when you call new StylusPoint() 
                    // a few of the ctor's lazy init this as well
                    _stylusPointDescription = new StylusPointDescription();
                }
                return _stylusPointDescription;
            }
            internal set
            {
                //
                // called by StylusPointCollection.Add / Set
                // to replace the StylusPoint.Description with the collections.
                //
                Debug.Assert(value != null &&
                    StylusPointDescription.AreCompatible(value, this.Description));

                _stylusPointDescription = value;
            }
        }

        /// <summary>
        /// Returns true if this StylusPoint supports the specified property
        /// </summary>
        /// <param name="stylusPointProperty">The StylusPointProperty to see if this StylusPoint supports</param>
        public bool HasProperty(StylusPointProperty stylusPointProperty)
        {
            return this.Description.HasProperty(stylusPointProperty);
        }

        /// <summary>
        /// Provides read access to all stylus properties
        /// </summary>
        /// <param name="stylusPointProperty">The StylusPointPropertyIds of the property to retrieve</param>
        public int GetPropertyValue(StylusPointProperty stylusPointProperty)
        {
            if (null == stylusPointProperty)
            {
                throw new ArgumentNullException("stylusPointProperty");
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.X)
            {
                return (int)_x;
            }
            else if (stylusPointProperty.Id == StylusPointPropertyIds.Y)
            {
                return (int)_y;
            }
            else if (stylusPointProperty.Id == StylusPointPropertyIds.NormalPressure)
            {
                StylusPointPropertyInfo info =
                    this.Description.GetPropertyInfo(StylusPointProperties.NormalPressure);

                int max = info.Maximum;
                return (int)(_pressureFactor * (float)max);
            }
            else
            {
                int propertyIndex = this.Description.GetPropertyIndex(stylusPointProperty.Id);
                if (-1 == propertyIndex)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidStylusPointProperty), "stylusPointProperty");
                }
                if (stylusPointProperty.IsButton)
                {
                    //
                    // we get button data from a single int in the array
                    //
                    int buttonData = _additionalValues[_additionalValues.Length - 1];
                    int buttonBitPosition = this.Description.GetButtonBitPosition(stylusPointProperty);
                    int bit = 1 << buttonBitPosition;
                    if ((buttonData & bit) != 0)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return _additionalValues[propertyIndex - 3];
                }
            }
        }

        /// <summary>
        /// Allows supported properties to be set
        /// </summary>
        /// <param name="stylusPointProperty">The property to set, it must exist on this StylusPoint</param>
        /// <param name="value">value</param>
        public void SetPropertyValue(StylusPointProperty stylusPointProperty, int value)
        {
            SetPropertyValue(stylusPointProperty, value, true);
        }
        /// <summary>
        /// Optimization that lets the ctor call setvalue repeatly without causing a copy of the int[]
        /// </summary>
        /// <param name="stylusPointProperty">stylusPointProperty</param>
        /// <param name="value">value</param>
        /// <param name="copyBeforeWrite"></param>
        internal void SetPropertyValue(StylusPointProperty stylusPointProperty, int value, bool copyBeforeWrite)
        {
            if (null == stylusPointProperty)
            {
                throw new ArgumentNullException("stylusPointProperty");
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.X)
            {
                double dVal = (double)value;
                //
                // only accept values between MaxXY and MinXY
                // we don't throw when passed a value outside of that range, we just silently trunctate
                //
                _x = GetClampedXYValue(dVal); 
            }
            else if (stylusPointProperty.Id == StylusPointPropertyIds.Y)
            {
                double dVal = (double)value;
                //
                // only accept values between MaxXY and MinXY
                // we don't throw when passed a value outside of that range, we just silently trunctate
                //
                _y = GetClampedXYValue(dVal); 
            }
            else if (stylusPointProperty.Id == StylusPointPropertyIds.NormalPressure)
            {
                StylusPointPropertyInfo info =
                    this.Description.GetPropertyInfo(StylusPointProperties.NormalPressure);

                int min = info.Minimum;
                int max = info.Maximum;
                if (max == 0)
                {
                    _pressureFactor = 0.0f;
                }
                else
                {
                    _pressureFactor = (float)(Convert.ToSingle(min + value) / Convert.ToSingle(max));
                }
            }
            else
            {
                int propertyIndex = this.Description.GetPropertyIndex(stylusPointProperty.Id);
                if (-1 == propertyIndex)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidStylusPointProperty), "propertyId");
                }
                if (stylusPointProperty.IsButton)
                {
                    if (value < 0 || value > 1)
                    {
                        throw new ArgumentOutOfRangeException("value", SR.Get(SRID.InvalidMinMaxForButton));
                    }

                    if (copyBeforeWrite)
                    {
                        CopyAdditionalData();
                    }

                    //
                    // we get button data from a single int in the array
                    //
                    int buttonData = _additionalValues[_additionalValues.Length - 1];
                    int buttonBitPosition = this.Description.GetButtonBitPosition(stylusPointProperty);
                    int bit = 1 << buttonBitPosition;
                    if (value == 0)
                    {
                        //turn the bit off
                        buttonData &= ~bit;
                    }
                    else
                    {
                        //turn the bit on
                        buttonData |= bit;
                    }
                    _additionalValues[_additionalValues.Length - 1] = buttonData;
                }
                else
                {
                    if (copyBeforeWrite)
                    {
                        CopyAdditionalData();
                    }
                    _additionalValues[propertyIndex - 3] = value;
                }
            }
        }

        /// <summary>
        /// Explicit cast converter between StylusPoint and Point
        /// </summary>
        /// <param name="stylusPoint">stylusPoint</param>
        public static explicit operator Point(StylusPoint stylusPoint)
        {
            return new Point(stylusPoint.X, stylusPoint.Y);
        }

        /// <summary>
        /// Allows languages that don't support operator overloading
        /// to convert to a point
        /// </summary>
        public Point ToPoint()
        {
            return new Point(this.X, this.Y);
        }


        /// <summary>
        /// Compares two StylusPoint instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// Descriptions must match for equality to succeed and additional values must match
        /// </summary>
        /// <returns>
        /// bool - true if the two Stylus instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='stylusPoint1'>The first StylusPoint to compare</param>
        /// <param name='stylusPoint2'>The second StylusPoint to compare</param>
        public static bool operator ==(StylusPoint stylusPoint1, StylusPoint stylusPoint2)
        {
            return StylusPoint.Equals(stylusPoint1, stylusPoint2);
        }

        /// <summary>
        /// Compares two StylusPoint instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Stylus instances are exactly inequal, false otherwise
        /// </returns>
        /// <param name='stylusPoint1'>The first StylusPoint to compare</param>
        /// <param name='stylusPoint2'>The second StylusPoint to compare</param>
        public static bool operator !=(StylusPoint stylusPoint1, StylusPoint stylusPoint2)
        {
            return !StylusPoint.Equals(stylusPoint1, stylusPoint2);
        }

        /// <summary>
        /// Compares two StylusPoint instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// Descriptions must match for equality to succeed and additional values must match
        /// </summary>
        /// <returns>
        /// bool - true if the two Stylus instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='stylusPoint1'>The first StylusPoint to compare</param>
        /// <param name='stylusPoint2'>The second StylusPoint to compare</param>
        public static bool Equals(StylusPoint stylusPoint1, StylusPoint stylusPoint2)
        {
            //
            // do the cheap comparison first
            //
            bool membersEqual =
                stylusPoint1._x == stylusPoint2._x &&
                stylusPoint1._y == stylusPoint2._y &&
                stylusPoint1._pressureFactor == stylusPoint2._pressureFactor;

            if (!membersEqual)
            {
                return false;
            }

            //
            // before we go checking the descriptions... check to see if both additionalData's are null
            // we can infer that the SPD's are just X,Y,P and that they are compatible.
            //
            if (stylusPoint1._additionalValues == null &&
                stylusPoint2._additionalValues == null)
            {
                Debug.Assert(StylusPointDescription.AreCompatible(stylusPoint1.Description, stylusPoint2.Description));
                return true;
            }

            //
            // ok, the members are equal.  compare the description and then additional data
            //
            if (object.ReferenceEquals(stylusPoint1.Description, stylusPoint2.Description) ||
                StylusPointDescription.AreCompatible(stylusPoint1.Description, stylusPoint2.Description))
            {
                //
                // descriptions match and there are equal numbers of additional values
                // let's check the values
                //
                for (int x = 0; x < stylusPoint1._additionalValues.Length; x++)
                {
                    if (stylusPoint1._additionalValues[x] != stylusPoint2._additionalValues[x])
                    {
                        return false;
                    }
                }

                //
                // Ok, ok already, we're equal
                //
                return true;
            }

            return false;
        }

        /// <summary>
        /// Compares two StylusPoint instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// Descriptions must match for equality to succeed and additional values must match
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of StylusPoint and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override bool Equals(object o)
        {
            if ((null == o) || !(o is StylusPoint))
            {
                return false;
            }

            StylusPoint value = (StylusPoint)o;
            return StylusPoint.Equals(this, value);
        }

        /// <summary>
        /// Equals - compares this StylusPoint with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The StylusPoint to compare to "this"</param>
        public bool Equals(StylusPoint value)
        {
            return StylusPoint.Equals(this, value);
        }
        /// <summary>
        /// Returns the HashCode for this StylusPoint
        /// </summary>
        /// <returns>
        /// int - the HashCode for this StylusPoint
        /// </returns>
        public override int GetHashCode()
        {
            int hash = 
                _x.GetHashCode() ^
                _y.GetHashCode() ^
                _pressureFactor.GetHashCode();

            if (_stylusPointDescription != null)
            {
                hash ^= _stylusPointDescription.GetHashCode();
            }

            if (_additionalValues != null)
            {
                for (int x = 0; x < _additionalValues.Length; x++)
                {
                    hash ^= _additionalValues[x]; //don't call GetHashCode on integers, it just returns the int
                }
            }

            return hash;
        }

        /// <summary>
        /// Used by the StylusPointCollection.ToHimetricArray method
        /// </summary>
        /// <returns></returns>
        internal int[] GetAdditionalData()
        {
            //return a direct ref
            return _additionalValues;
        }

        /// <summary>
        /// Internal helper used by SPC.Reformat to preserve the pressureFactor
        /// </summary>
        internal float GetUntruncatedPressureFactor()
        {
            return _pressureFactor;
        }

        /// <summary>
        /// GetPacketData - returns avalon space packet data with true pressure if it exists
        /// </summary>
        internal int[] GetPacketData()
        {
            int count = 2; //x, y
            if (_additionalValues != null)
            {
                count += _additionalValues.Length;
            }
            if (this.Description.ContainsTruePressure)
            {
                count++;
            }
            int[] data = new int[count];
            data[0] = (int)_x;
            data[1] = (int)_y;
            int startIndex = 2;
            if (this.Description.ContainsTruePressure)
            {
                startIndex = 3;
                data[2] = GetPropertyValue(StylusPointProperties.NormalPressure);
            }
            if (_additionalValues != null)
            {
                for (int x = 0; x < _additionalValues.Length; x++)
                {
                    data[x + startIndex] = _additionalValues[x];
                }
            }
            return data;
        }

        /// <summary>
        /// Internal helper to determine if a stroke has default pressure
        /// This is used by ISF serialization to not serialize pressure
        /// </summary>
        internal bool HasDefaultPressure
        {
            get
            {
                return (_pressureFactor == DefaultPressure);
            }
        }

        /// <summary>
        /// Used by the SetPropertyData to make a copy of the data
        /// before modifying it.  This is required so that we don't 
        /// have two StylusPoint's sharing the same int[]
        /// which can happen when you call: StylusPoint p = otherStylusPoint
        /// because the CLR just does a memberwise copy
        /// </summary>
        /// <returns></returns>        
        private void CopyAdditionalData()
        {
            if (null != _additionalValues)
            {
                int[] newData = new int[_additionalValues.Length];
                for (int x = 0; x < _additionalValues.Length; x++)
                {
                    newData[x] = _additionalValues[x];
                }

                _additionalValues = newData;
            }
        }

        /// <summary>
        /// Private helper that returns a double clamped to MaxXY or MinXY
        /// We only accept values in this range to support ISF serialization
        /// </summary>
        private static double GetClampedXYValue(double xyValue)
        {
            if (xyValue > MaxXY)
            {
                return MaxXY;
            }
            if (xyValue < MinXY)
            {
                return MinXY;
            }

            return xyValue;
        }
    }
}
