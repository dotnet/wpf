// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

// Allow suppression of certain presharp messages
#pragma warning disable 1634, 1691

using System.ComponentModel;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    /// Duration struct provides sentinel values that are not included in TimeSpan.
    /// This structure may represent a TimeSpan, Automatic, or Forever value.
    /// </summary>
    [TypeConverter(typeof(DurationConverter))]
    public struct Duration
    {
        private TimeSpan _timeSpan;
        private DurationType _durationType;

        /// <summary>
        /// Creates a Duration from a TimeSpan.
        /// </summary>
        /// <param name="timeSpan"></param>
        public Duration(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentException(SR.Get(SRID.Timing_InvalidArgNonNegative), "timeSpan");
            }
            _durationType = DurationType.TimeSpan;
            _timeSpan = timeSpan;
        }

        #region Operators

        //
        // Since Duration has two special values, for comparison purposes 
        // Duration.Forever behaves like Double.PositiveInfinity and
        // Duration.Automatic behaves almost entirely like Double.NaN
        // Any comparision with Automatic returns false, except for ==.
        // Unlike NaN, Automatic == Automatic is true.
        //
  

        /// <summary>
        /// Implicitly creates a Duration from a TimeSpan.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static implicit operator Duration(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentException(SR.Get(SRID.Timing_InvalidArgNonNegative), "timeSpan");
            }
            return new Duration(timeSpan);
        }

        /// <summary>
        /// Adds two Durations together.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>If both Durations have values then this returns a Duration 
        /// representing the sum of those two values; otherwise a Duration representing null.</returns>
        public static Duration operator +(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan + t2._timeSpan);
            }
            else if (t1._durationType != DurationType.Automatic
                     && t2._durationType != DurationType.Automatic)
            {
                // Neither t1 nor t2 are Automatic, so one is Forever
                // while the other is Forever or a TimeSpan.  Either way 
                // the sum is Forever.
                return Duration.Forever;
            }
            else
            {
                // Automatic + anything is Automatic
                return Duration.Automatic;
            }
        }

        /// <summary>
        /// Subracts one Duration from another.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>If both Durations have values then this returns a Duration
        /// representing the value of the second subtracted from the first; otherwise a Duration representing null.</returns>
        public static Duration operator -(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan - t2._timeSpan);
            }
            else if (t1._durationType == DurationType.Forever
                     && t2.HasTimeSpan)
            {
                // The only way for the result to be Forever is
                // if t1 is Forever and t2 is a TimeSpan
                return Duration.Forever;
            }
            else
            {
                // This covers the following conditions:
                // Forever - Forever
                // TimeSpan - Forever
                // TimeSpan - Automatic
                // Forever - Automatic
                // Automatic - Automatic
                // Automatic - Forever
                // Automatic - TimeSpan
                return Duration.Automatic;
            }
        }

        /// <summary>
        /// Indicates whether two Durations are equal.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>true if both of the Durations have values and those values are equal or if both
        /// Durations represent null; otherwise false.</returns>
        public static bool operator ==(Duration t1, Duration t2)
        {
            return t1.Equals(t2);
        }

        /// <summary>
        /// Indicates whether two Durations are not equal.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>true if exactly one of t1 and t2 represents a value or if they both represent
        /// values and those values are not equal; otherwise false.</returns>
        public static bool operator !=(Duration t1, Duration t2)
        {
            return !(t1.Equals(t2));
        }

        /// <summary>
        /// Indicates whether one Duration is greater than another.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>true if both t1 and t2 have values and the value of t1 
        /// is greater than the value of t2; otherwise false.  Forever is 
        /// considered greater than all finite values and any comparison 
        /// with Automatic returns false.</returns>
        public static bool operator >(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan > t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                // TimeSpan > Forever is false;
                return false;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                // Forever > TimeSpan is true;
                return true;
            }
            else
            {
                // Cases covered:
                // Either t1 or t2 are Automatic, 
                // or t1 and t2 are both Forever 
                return false;
            }
        }

        /// <summary>
        /// Indicates whether one Duration is greater than or equal to another.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>true if both t1 and t2 have values and the value of t1 
        /// is greater than or equal to the value of t2; otherwise false.  
        /// Forever is considered greater than all finite values and any 
        /// comparison with Automatic returns false.</returns>
        public static bool operator >=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                // Automatic == Automatic
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                // Automatic compared to anything else is false
                return false;
            }
            else
            {
                return !(t1 < t2);
            }
        }

        /// <summary>
        /// Indicates whether one Duration is less than another.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>true if both t1 and t2 have values and the value of t1 
        /// is less than the value of t2; otherwise false.  Forever is 
        /// considered greater than all finite values and any comparison 
        /// with Automatic returns false</returns>
        public static bool operator <(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan < t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                // TimeSpan < Forever is true;
                return true;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                // Forever < TimeSpan is true;
                return false;
            }
            else
            {
                // Cases covered:
                // Either t1 or t2 are Automatic, 
                // or t1 and t2 are both Forever 
                return false;
            }
        }

        /// <summary>
        /// Indicates whether one Duration is less than or equal to another.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>true if both t1 and t2 have values and the value of t1 
        /// is less than or equal to the value of t2; otherwise false.  
        /// Forever is considered greater than all finite values and any 
        /// comparison with Automatic returns false.</returns>
        public static bool operator <=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                // Automatic == Automatic
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                // Automatic compared to anything else is false
                return false;
            }
            else
            {
                return !(t1 > t2);
            }
        }

        /// <summary>
        /// Compares one Duration value to another.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>
        /// A negative value, zero or a positive value, respectively, if t1 is
        /// less than, equal or greater than t2.
        /// 
        /// Duration.Automatic is a special case and has the following return values:
        /// 
        ///  -1 if t1 is Automatic and t2 is not Automatic
        ///   0 if t1 and t2 are Automatic
        ///   1 if t1 is not Automatic and t2 is Automatic
        /// 
        /// This mirrors Double.CompareTo()'s treatment of Double.NaN
        /// </returns>
        public static int Compare(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic)
            {
                if (t2._durationType == DurationType.Automatic)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (t2._durationType == DurationType.Automatic)
            {
                return 1;
            }
            else // Neither are Automatic, do a standard comparison
            {
                if (t1 < t2)
                {
                    return -1;
                }
                else if (t1 > t2)
                {
                    return 1;
                }
                else  // Neither is greater than the other
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Returns the specified instance of Duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns>Returns duration.</returns>
        public static Duration Plus(Duration duration)
        {
            return duration;
        }

        /// <summary>
        /// Returns the specified instance of Duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns>Returns duration.</returns>
        public static Duration operator +(Duration duration)
        {
            return duration;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this Duration is a TimeSpan value.
        /// </summary>
        /// <value>true if this Duration is a TimeSpan value.</value>
        public bool HasTimeSpan
        {
            get
            {
                return (_durationType == DurationType.TimeSpan);
            }
        }

        /// <summary>
        /// Returns a Duration that represents an Automatic value.
        /// </summary>
        /// <value>A Duration that represents an Automatic value.</value>
        public static Duration Automatic
        {
            get
            {
                Duration duration = new Duration();
                duration._durationType = DurationType.Automatic;

                return duration;
            }
        }

        /// <summary>
        /// Returns a Duration that represents a Forever value.
        /// </summary>
        /// <value>A Duration that represents a Forever value.</value>
        public static Duration Forever
        {
            get
            {
                Duration duration = new Duration();
                duration._durationType = DurationType.Forever;

                return duration;
            }
        }

        /// <summary>
        /// Returns the TimeSpan value that this Duration represents.
        /// </summary>
        /// <value>The TimeSpan value that this Duration represents.</value>
        /// <exception cref="System.InvalidOperationException">Thrown if this Duration represents null.</exception>
        public TimeSpan TimeSpan
        {
            get
            {
                if (HasTimeSpan)
                {
                    return _timeSpan;
                }
                else
                {
#pragma warning suppress 56503 // Suppress presharp warning: Follows a pattern similar to Nullable.
                    throw new InvalidOperationException(SR.Get(SRID.Timing_NotTimeSpan, this));
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the specified Duration to this instance.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns>A Duration that represents the value of this instance plus the value of duration.</returns>
        public Duration Add(Duration duration)
        {
            return this + duration;
        }

        /// <summary>
        /// Indicates whether the specified Object is equal to this Duration.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true if value is a Duration and is equal to this instance; otherwise false.</returns>
        public override bool Equals(Object value)
        {
            if (value == null)
            {
                return false;
            }
            else if (value is Duration)
            {
                return Equals((Duration)value);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the specified Duration is equal to this Duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns>true if duration is equal to this instance; otherwise false.</returns>
        public bool Equals(Duration duration)
        {
            if (HasTimeSpan)
            {
                if (duration.HasTimeSpan)
                {
                    return _timeSpan == duration._timeSpan;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _durationType == duration._durationType;
            }
        }

        /// <summary>
        /// Indicates whether the specified Durations are equal.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>true if t1 equals t2; otherwise false.</returns>
        public static bool Equals(Duration t1, Duration t2)
        {
            return t1.Equals(t2);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            if (HasTimeSpan)
            {
                return _timeSpan.GetHashCode();
            }
            else
            {
                return _durationType.GetHashCode() + 17;
            }
        }

        /// <summary>
        /// Subtracts the specified Duration from this instance. 
        /// </summary>
        /// <param name="duration"></param>
        /// <returns>A Duration whose value is the result of the value of this instance minus the value of duration.</returns>
        public Duration Subtract(Duration duration)
        {
            return this - duration;
        }

        /// <summary>
        /// Creates a string representation of this Duration.
        /// </summary>
        /// <returns>A string representation of this Duration.</returns>
        public override string ToString()
        {
            if (HasTimeSpan)
            {
                return TypeDescriptor.GetConverter(_timeSpan).ConvertToString(_timeSpan);
            }
            else if (_durationType == DurationType.Forever)
            {
                return "Forever";
            }
            else // IsAutomatic
            {
                return "Automatic";
            }
        }

        #endregion

        /// <summary>
        /// An enumeration of the different types of Duration behaviors.
        /// </summary>
        private enum DurationType
        {
            Automatic,
            TimeSpan,
            Forever
        }

    }
}

