// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MS.Internal.WindowsBase;

namespace System.Windows.Threading
{
    /// <summary>
    ///     Represents a range of priorities.
    /// </summary>
    internal struct PriorityRange
    {
        /// <summary>
        ///     The range of all possible priorities.
        /// </summary>
        public static readonly PriorityRange All = new PriorityRange(DispatcherPriority.Inactive, DispatcherPriority.Send, true);  // NOTE: should be Priority

        /// <summary>
        ///     A range that includes no priorities.
        /// </summary>
        public static readonly PriorityRange None = new PriorityRange(DispatcherPriority.Invalid, DispatcherPriority.Invalid, true); // NOTE: should be Priority

        /// <summary>
        ///     Constructs an instance of the PriorityRange class.
        /// </summary>
        public PriorityRange(DispatcherPriority min, DispatcherPriority max) : this() // NOTE: should be Priority
        {
            Initialize(min, true, max, true);
        }
        
        /// <summary>
        ///     Constructs an instance of the PriorityRange class.
        /// </summary>
        public PriorityRange(DispatcherPriority min, bool isMinInclusive, DispatcherPriority max, bool isMaxInclusive) : this() // NOTE: should be Priority
        {
            Initialize(min, isMinInclusive, max, isMaxInclusive);
        }
        
        /// <summary>
        ///     The minimum priority of this range.
        /// </summary>
        public DispatcherPriority Min // NOTE: should be Priority
        {
            get
            {
                return _min;
            }
        }

        /// <summary>
        ///     The maximum priority of this range.
        /// </summary>
        public DispatcherPriority Max // NOTE: should be Priority
        {
            get
            {
                return _max;
            }
        }

        /// <summary>
        ///     Whether or not the minimum priority in included in this range.
        /// </summary>
        public bool IsMinInclusive
        {
            get
            {
                return _isMinInclusive;
            }
        }
        
        /// <summary>
        ///     Whether or not the maximum priority in included in this range.
        /// </summary>
        public bool IsMaxInclusive
        {
            get
            {
                return _isMaxInclusive;
            }
        }
        
        /// <summary>
        ///     Whether or not this priority range is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                // return _min != null && _min.IsValid && _max != null && _max.IsValid;
                return (_min > DispatcherPriority.Invalid && _min <= DispatcherPriority.Send &&
                        _max > DispatcherPriority.Invalid && _max <= DispatcherPriority.Send);
            }
        }

        /// <summary>
        ///     Whether or not this priority range contains the specified
        ///     priority.
        /// </summary>
        public bool Contains(DispatcherPriority priority) // NOTE: should be Priority
        {
            /*
            if (priority == null || !priority.IsValid)
            {
                return false;
            }
            */
            if(priority <= DispatcherPriority.Invalid || priority > DispatcherPriority.Send)
            {
                return false;
            }

            if (!IsValid)
            {
                return false;
            }

            bool contains = false;

            if (_isMinInclusive)
            {
                contains = (priority >= _min);
            }
            else
            {
                contains = (priority > _min);
            }

            if (contains)
            {
                if (_isMaxInclusive)
                {
                    contains = (priority <= _max);
                }
                else
                {
                    contains = (priority < _max);
                }
            }

            return contains;
        }
        
        /// <summary>
        ///     Whether or not this priority range contains the specified
        ///     priority range.
        /// </summary>
        public bool Contains(PriorityRange priorityRange)
        {
            if (!priorityRange.IsValid)
            {
                return false;
            }

            if (!IsValid)
            {
                return false;
            }

            bool contains = false;

            if (priorityRange._isMinInclusive)
            {
                contains = Contains(priorityRange.Min);
            }
            else
            {
                if(priorityRange.Min >= _min && priorityRange.Min < _max)
                {
                    contains = true;
                }
            }

            if (contains)
            {
                if (priorityRange._isMaxInclusive)
                {
                    contains = Contains(priorityRange.Max);
                }
                else
                {
                    if(priorityRange.Max > _min && priorityRange.Max <= _max)
                    {
                        contains = true;
                    }
                }
            }
                
            return contains;
        }

        /// <summary>
        ///     Equality method for two PriorityRange
        /// </summary>
        public override bool Equals(object o)
        {
            if(o is PriorityRange)
            {
                return Equals((PriorityRange) o);
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        ///     Equality method for two PriorityRange
        /// </summary>
        public bool Equals(PriorityRange priorityRange)
        {
            return priorityRange._min == this._min &&
                   priorityRange._isMinInclusive == this._isMinInclusive &&
                   priorityRange._max == this._max &&
                   priorityRange._isMaxInclusive == this._isMaxInclusive;
        }

        /// <summary>
        ///     Equality operator
        /// </summary>
        public static bool operator== (PriorityRange priorityRange1, PriorityRange priorityRange2)
        {
            return priorityRange1.Equals(priorityRange2);
        }

        /// <summary>
        ///     Inequality operator
        /// </summary>
        public static bool operator!= (PriorityRange priorityRange1, PriorityRange priorityRange2)
        {
            return !(priorityRange1 == priorityRange2);
        }

        /// <summary>
        ///     Returns a reasonable hash code for this PriorityRange instance.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        private void Initialize(DispatcherPriority min, bool isMinInclusive, DispatcherPriority max, bool isMaxInclusive) // NOTE: should be Priority
        {
            /*
            if(min == null)
            {
                throw new ArgumentNullException("min");
            }
            
            if (!min.IsValid)
            {
                throw new ArgumentException("Invalid priority.", "min");
            }
            */
            if(min < DispatcherPriority.Invalid || min > DispatcherPriority.Send)
            {
                // If we move to a Priority class, this exception will have to change too.
                throw new System.ComponentModel.InvalidEnumArgumentException("min", (int)min, typeof(DispatcherPriority));
            }
            if(min == DispatcherPriority.Inactive)
            {
                throw new ArgumentException(SR.InvalidPriority, "min");
            }

            /*            
            if(max == null)
            {
                throw new ArgumentNullException("max");
            }

            if (!max.IsValid)
            {
                throw new ArgumentException("Invalid priority.", "max");
            }
            */
            if(max < DispatcherPriority.Invalid || max > DispatcherPriority.Send)
            {
                // If we move to a Priority class, this exception will have to change too.
                throw new System.ComponentModel.InvalidEnumArgumentException("max", (int)max, typeof(DispatcherPriority));
            }
            if(max == DispatcherPriority.Inactive)
            {
                throw new ArgumentException(SR.InvalidPriority, "max");
            }
            
            if (max < min)
            {
                throw new ArgumentException(SR.InvalidPriorityRangeOrder);
            }

            _min = min;
            _isMinInclusive = isMinInclusive;
            _max = max;
            _isMaxInclusive = isMaxInclusive;
        }

        // This is a constructor for our special static members.
        private PriorityRange(DispatcherPriority min, DispatcherPriority max, bool ignored) // NOTE: should be Priority
        {
            _min = min;
            _isMinInclusive = true;
            _max = max;
            _isMaxInclusive = true;
        }

        private DispatcherPriority _min;  // NOTE: should be Priority
        private bool _isMinInclusive;
        private DispatcherPriority _max;  // NOTE: should be Priority
        private bool _isMaxInclusive;
}
}
