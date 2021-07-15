// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Toggle Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents UI elements that have a set of states that can by cycled through such as a checkbox, or toggle button.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class TogglePattern: BasePattern
#else
    public class TogglePattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TogglePattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
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

        /// <summary>Toggle pattern</summary>
        public static readonly AutomationPattern Pattern = TogglePatternIdentifiers.Pattern;

        /// <summary>Property ID: ToggleState - Value of a toggleable control, as a ToggleState enum</summary>
        public static readonly AutomationProperty ToggleStateProperty = TogglePatternIdentifiers.ToggleStateProperty;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Request to change the state that this UI element is currently representing
        /// </summary>
        public void Toggle()
        {
            UiaCoreApi.TogglePattern_Toggle(_hPattern);
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
        public TogglePatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new TogglePatternInformation(_el, true);
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
        public TogglePatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new TogglePatternInformation(_el, false);
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
            return new TogglePattern(el, hPattern, cached);
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
        public struct TogglePatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal TogglePatternInformation(AutomationElement el, bool useCache)
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

            /// <summary>Value of a toggleable control, as a ToggleState enum</summary>
            public ToggleState ToggleState
            {
                get
                {
                    return (ToggleState)_el.GetPatternPropertyValue(ToggleStateProperty, _useCache);
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
