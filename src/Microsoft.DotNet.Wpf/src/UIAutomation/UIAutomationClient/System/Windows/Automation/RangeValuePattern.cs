// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for RangeValue Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Exposes a related set of properties that reflect a control's ability to manage a value
    /// within a finite range.  It conveys a controls valid minimum and maximum values and its
    /// current value.
    ///
    ///  Pattern requires MinValue less than MaxValue. 
    ///  MinimumValue and MaximumValue must be the same Object type as ValueAsObject.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class RangeValuePattern : BasePattern
#else
    public class RangeValuePattern : BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private RangeValuePattern( AutomationElement el, SafePatternHandle hPattern, bool cached )
            : base(el, hPattern)
        {
            _hPattern = hPattern;
            _cached = cached;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Value pattern</summary>
        public static readonly AutomationPattern Pattern = RangeValuePatternIdentifiers.Pattern;

        /// <summary>Property ID: Value - Value of a value control, as a double</summary>
        public static readonly AutomationProperty ValueProperty = RangeValuePatternIdentifiers.ValueProperty;

        /// <summary>Property ID: IsReadOnly - Indicates that the value can only be read, not modified.</summary>
        public static readonly AutomationProperty IsReadOnlyProperty = RangeValuePatternIdentifiers.IsReadOnlyProperty;

        /// <summary>Property ID: Maximum value</summary>
        public static readonly AutomationProperty MinimumProperty = RangeValuePatternIdentifiers.MinimumProperty;

        /// <summary>Property ID: Maximum value</summary>
        public static readonly AutomationProperty MaximumProperty = RangeValuePatternIdentifiers.MaximumProperty;

        /// <summary>Property ID: LargeChange - Indicates a value to be added to or subtracted from the Value property when the element is moved a large distance.</summary>
        public static readonly AutomationProperty LargeChangeProperty = RangeValuePatternIdentifiers.LargeChangeProperty;

        /// <summary>Property ID: SmallChange - Indicates a value to be added to or subtracted from the Value property when the element is moved a small distance.</summary>
        public static readonly AutomationProperty SmallChangeProperty = RangeValuePatternIdentifiers.SmallChangeProperty;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods
        /// <summary>
        /// Request to set the value that this UI element is representing
        /// </summary>
        /// <param name="value">Value to set the UI to, as a double</param>
        public void SetValue(double value)
        {
            // Test the Enabled state prior to the more general Read-Only state.            
            object enabled = _el.GetCurrentPropertyValue(AutomationElementIdentifiers.IsEnabledProperty);
            if (enabled is bool && !(bool)enabled)
            {
                throw new ElementNotEnabledException();
            }

            // Test the Read-Only state after the more specific Enabled state.
            object readOnly = _el.GetCurrentPropertyValue(IsReadOnlyProperty);
            if (readOnly is bool && (bool)readOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.ValueReadonly));
            }
            UiaCoreApi.RangeValuePattern_SetValue(_hPattern, value);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties
        /// <summary>
        /// This member allows access to previously requested
        /// cached properties for this element. The returned object
        /// has accessors for each property defined for this pattern.
        /// </summary>
        /// <remarks>
        /// Cached property values must have been previously requested
        /// using a CacheRequest. If you try to access a cached
        /// property that was not previously requested, an InvalidOperation
        /// Exception will be thrown.
        /// 
        /// To get the value of a property at the current point in time,
        /// access the property via the Current accessor instead of
        /// Cached.
        /// </remarks>
        public RangeValuePatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new RangeValuePatternInformation(_el, true);
            }
        }

        /// <summary>
        /// This member allows access to current property values
        /// for this element. The returned object has accessors for
        /// each property defined for this pattern.
        /// </summary>
        /// <remarks>
        /// This pattern must be from an AutomationElement with a
        /// Full reference in order to get current values. If the
        /// AutomationElement was obtained using AutomationElementMode.None,
        /// then it contains only cached data, and attempting to get
        /// the current value of any property will throw an InvalidOperationException.
        /// 
        /// To get the cached value of a property that was previously
        /// specified using a CacheRequest, access the property via the
        /// Cached accessor instead of Current.
        /// </remarks>
        public RangeValuePatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new RangeValuePatternInformation(_el, false);
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        static internal object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new RangeValuePattern(el, hPattern, cached);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;
        private bool _cached;

        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Nested Classes
        //
        //------------------------------------------------------

        #region Nested Classes

        /// <summary>
        /// This class provides access to either Cached or Current
        /// properties on a pattern via the pattern's .Cached or
        /// .Current accessors.
        /// </summary>
        public struct RangeValuePatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal RangeValuePatternInformation(AutomationElement el, bool useCache)
            {
                _el = el;
                _useCache = useCache;
            }

            #endregion Constructors


            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------
 
            #region Public Properties

            ///<summary>Value of a value control</summary>
            public double Value
            {
                get
                {
                    object propValue = _el.GetPatternPropertyValue(ValueProperty, _useCache);
                    if (propValue is int)
                    {
                        return (double)(int)propValue;
                    }
                    else if (propValue is Int32)
                    {
                        return (double)(Int32)propValue;
                    }
                    else if (propValue is byte)
                    {
                        return (double)(byte)propValue;
                    }
                    else if (propValue is DateTime)
                    {
                        return (double)((DateTime)propValue).Year;
                    }
                    else
                    {
                        return (double)propValue;
                    }
                }
            }

            ///<summary>Indicates that the value can only be read, not modified.
            ///returns True if the control is read-only</summary>
            public bool IsReadOnly
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(IsReadOnlyProperty, _useCache);
                }
            }

            ///<summary>maximum value </summary>
            public double Maximum
            {
                get
                {
                    object propValue = _el.GetPatternPropertyValue(MaximumProperty, _useCache);
                    if (propValue is int)
                    {
                        return (double)(int)propValue;
                    }
                    else if (propValue is Int32)
                    {
                        return (double)(Int32)propValue;
                    }
                    else if (propValue is byte)
                    {
                        return (double)(byte)propValue;
                    }
                    else if (propValue is DateTime)
                    {
                        return (double)((DateTime)propValue).Year;
                    }
                    else
                    {
                        return (double)propValue;
                    }
                }
            }

            ///<summary>minimum value</summary>
            public double Minimum
            {
                get
                {
                    object propValue = _el.GetPatternPropertyValue(MinimumProperty, _useCache);
                    if (propValue is int)
                    {
                        return (double)(int)propValue;
                    }
                    else if (propValue is Int32)
                    {
                        return (double)(Int32)propValue;
                    }
                    else if (propValue is byte)
                    {
                        return (double)(byte)propValue;
                    }
                    else if (propValue is DateTime)
                    {
                        return (double)((DateTime)propValue).Year;
                    }
                    else
                    {
                        return (double)propValue;
                    }
                }
            }


            ///<summary>
            /// Gets a value to be added to or subtracted from the Value property 
            /// when the element is moved a large distance.
            /// </summary>
            public double LargeChange
            {
                get
                {
                    object propValue = _el.GetPatternPropertyValue(LargeChangeProperty, _useCache);
                    if (propValue is int)
                    {
                        return (double)(int)propValue;
                    }
                    else if (propValue is Int32)
                    {
                        return (double)(Int32)propValue;
                    }
                    else if (propValue is byte)
                    {
                        return (double)(byte)propValue;
                    }
                    else if (propValue is DateTime)
                    {
                        return (double)((DateTime)propValue).Year;
                    }
                    else
                    {
                        return (double)propValue;
                    }
                }
            }

            ///<summary>
            /// Gets a value to be added to or subtracted from the Value property 
            /// when the element is moved a small distance.
            /// </summary>
            public double SmallChange
            {
                get
                {
                    object propValue = _el.GetPatternPropertyValue(SmallChangeProperty, _useCache);
                    if (propValue is int)
                    {
                        return (double)(int)propValue;
                    }
                    else if (propValue is Int32)
                    {
                        return (double)(Int32)propValue;
                    }
                    else if (propValue is byte)
                    {
                        return (double)(byte)propValue;
                    }
                    else if (propValue is DateTime)
                    {
                        return (double)((DateTime)propValue).Year;
                    }
                    else
                    {
                        return (double)propValue;
                    }
                }
            }

            #endregion Public Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private AutomationElement _el; // AutomationElement that contains the cache or live reference
            private bool _useCache; // true to use cache, false to use live reference to get current values

            #endregion Private Fields
        }
        #endregion Nested Classes
    }
}
