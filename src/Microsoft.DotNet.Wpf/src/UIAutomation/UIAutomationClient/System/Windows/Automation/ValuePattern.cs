// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Value Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents UI elements that are expressing a value
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class ValuePattern: BasePattern
#else
    public class ValuePattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // internal so that RangeValue can derive from this
        internal ValuePattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
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
        public static readonly AutomationPattern Pattern = ValuePatternIdentifiers.Pattern;

        /// <summary>Property ID: Value - Value of a value control, as a human-readable string</summary>
        public static readonly AutomationProperty ValueProperty = ValuePatternIdentifiers.ValueProperty;

        /// <summary>Property ID: IsReadOnly - Indicates that the value can only be read, not modified.</summary>
        public static readonly AutomationProperty IsReadOnlyProperty = ValuePatternIdentifiers.IsReadOnlyProperty;

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
        /// <param name="value">Value to set the UI to, the provider is responsible for converting from a string into the appropriate data type</param>
        public void SetValue( string value )
        {
            Misc.ValidateArgumentNonNull(value, "value");
            
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

            UiaCoreApi.ValuePattern_SetValue(_hPattern,  value);
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
        public ValuePatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new ValuePatternInformation(_el, true);
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
        public ValuePatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new ValuePatternInformation(_el, false);
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
            return new ValuePattern(el, hPattern, cached);
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
        public struct ValuePatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal ValuePatternInformation(AutomationElement el, bool useCache)
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

            ///<summary>Value of a value control, as a a string.</summary>
            public string Value
            {
                get
                {
                    // Ideally, this could just be:
                    // return (string)_el.GetPatternPropertyValue(ValueProperty, _useCache);
                    // But Value maps to ValueAsObject, so need to ToString it.
                    // Otherwise checkbox enums will break the cast.
                    object temp = _el.GetPatternPropertyValue(ValueProperty, _useCache);
                    return temp.ToString();
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
