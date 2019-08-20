// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Allow suppression of certain presharp messages
#pragma warning disable 1634, 1691

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A KeyTime is use to specify when relative to the time of an animation
    /// that a KeyFrame takes place.
    /// </summary>
    [TypeConverter(typeof(KeyTimeConverter))]
    public struct KeyTime : IEquatable<KeyTime>
    {
        private object _value;
        private KeyTimeType _type;
        
        #region Static Create Methods

        /// <summary>
        /// Creates a KeyTime that represents a Percent value.
        /// </summary>
        /// <param name="percent">
        /// The percent value provided as a double value between 0.0 and 1.0.
        /// </param>
        public static KeyTime FromPercent(double percent)
        {
            if (percent < 0.0 || percent > 1.0)
            {
                throw new ArgumentOutOfRangeException("percent", SR.Get(SRID.Animation_KeyTime_InvalidPercentValue, percent));
            }

            KeyTime keyTime = new KeyTime();

            keyTime._value = percent;
            keyTime._type = KeyTimeType.Percent;

            return keyTime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSpan"></param>
        public static KeyTime FromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("timeSpan", SR.Get(SRID.Animation_KeyTime_LessThanZero, timeSpan));
            }

            KeyTime keyTime = new KeyTime();

            keyTime._value = timeSpan;
            keyTime._type = KeyTimeType.TimeSpan;

            return keyTime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public static KeyTime Uniform
        {
            get
            {
                KeyTime keyTime = new KeyTime();
                keyTime._type = KeyTimeType.Uniform;

                return keyTime;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public static KeyTime Paced
        {
            get
            {
                KeyTime keyTime = new KeyTime();
                keyTime._type = KeyTimeType.Paced;

                return keyTime;
            }
        }

        #endregion

        #region Operators

        /// <summary>
        /// Returns true if two KeyTimes are equal.
        /// </summary>
        public static bool Equals(KeyTime keyTime1, KeyTime keyTime2)
        {
            if (keyTime1._type == keyTime2._type)
            {
                switch (keyTime1._type)
                {
                    case KeyTimeType.Uniform:
                        break;

                    case KeyTimeType.Paced:
                        break;

                    case KeyTimeType.Percent:
                        if ((double)keyTime1._value != (double)keyTime2._value)
                        {
                            return false;
                        }
                        break;

                    case KeyTimeType.TimeSpan:
                        if ((TimeSpan)keyTime1._value != (TimeSpan)keyTime2._value)
                        {
                            return false;
                        }
                        break;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if two KeyTimes are equal.
        /// </summary>
        public static bool operator ==(KeyTime keyTime1, KeyTime keyTime2)
        {
            return KeyTime.Equals(keyTime1, keyTime2);
        }

        /// <summary>
        /// Returns true if two KeyTimes are not equal.
        /// </summary>
        public static bool operator !=(KeyTime keyTime1, KeyTime keyTime2)
        {
            return !KeyTime.Equals(keyTime1, keyTime2);
        }

        #endregion

        #region IEquatable<KeyTime>

        /// <summary>
        /// Returns true if two KeyTimes are equal.
        /// </summary>
        public bool Equals(KeyTime value)
        {
            return KeyTime.Equals(this, value);
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Implementation of <see cref="System.Object.Equals(object)">Object.Equals</see>.
        /// </summary>
        public override bool Equals(object value)
        {
            if (   value == null
                || !(value is KeyTime))
            {
                return false;
            }
            
            return this == (KeyTime)value;
        }

        /// <summary>
        /// Implementation of <see cref="System.Object.GetHashCode">Object.GetHashCode</see>.
        /// </summary>
        public override int GetHashCode()
        {
            // If we don't have a value (uniform, or paced) then use the type
            // to determine the hash code
            if (_value != null)
            {
                return _value.GetHashCode();
            }
            else
            {
                return _type.GetHashCode();
            }
        }
        
        /// <summary>
        /// Generates a string representing this KeyTime.
        /// </summary>
        /// <returns>The generated string.</returns>
        public override string ToString()
        {
            KeyTimeConverter converter = new KeyTimeConverter();

            return converter.ConvertToString(this);
        }

        #endregion

        #region Implicit Converters

        /// <summary>
        /// Implicitly creates a KeyTime value from a Time value.
        /// </summary>
        /// <param name="timeSpan">The Time value.</param>
        /// <returns>A new KeyTime.</returns>
        public static implicit operator KeyTime(TimeSpan timeSpan)
        {
            return KeyTime.FromTimeSpan(timeSpan);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The Time value for this KeyTime. 
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the type of this KeyTime isn't KeyTimeType.TimeSpan.
        /// </exception>
        public TimeSpan TimeSpan
        {
            get
            {
                if (_type == KeyTimeType.TimeSpan)
                {
                    return (TimeSpan)_value;
                }
                else
                {
#pragma warning suppress 56503 // Suppress presharp warning: Follows a pattern similar to Nullable.
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// The percent value for this KeyTime.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the type of this KeyTime isn't KeyTimeType.Percent.
        /// </exception>
        public double Percent
        {
            get
            {
                if (_type == KeyTimeType.Percent)
                {
                    return (double)_value;
                }
                else
                {
#pragma warning suppress 56503 // Suppress presharp warning: Follows a pattern similar to Nullable.
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// The type of this KeyTime.
        /// </summary>
        public KeyTimeType Type
        {
            get
            {
                return _type;
            }
        }

        #endregion
    }
}
