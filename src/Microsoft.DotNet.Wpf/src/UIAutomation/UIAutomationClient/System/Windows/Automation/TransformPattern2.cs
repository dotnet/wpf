// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Client-side wrapper for Transform Pattern

using MS.Internal.Automation;

namespace System.Windows.Automation
{
    ///<summary>wrapper class for Transform pattern </summary>
#if (INTERNAL_COMPILE)
    internal class TransformPattern2: TransformPattern
#else
    public class TransformPattern2: TransformPattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TransformPattern2(AutomationElement el, SafePatternHandle hPattern, bool cached)
            : base(el, hPattern, cached)
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

        /// <summary>Returns the Transform pattern identifier</summary>
        public static readonly new AutomationPattern Pattern = TransformPattern2Identifiers.Pattern;

        /// <summary>Property ID: CanZoom - Indicates whether the control supports zooming of its viewport</summary>
        public static readonly AutomationProperty CanZoomProperty = TransformPattern2Identifiers.CanZoomProperty;

        public static readonly AutomationProperty ZoomLevelProperty = TransformPattern2Identifiers.ZoomLevelProperty;

        public static readonly AutomationProperty ZoomMaximumProperty = TransformPattern2Identifiers.ZoomMaximumProperty;

        public static readonly AutomationProperty ZoomMinimumProperty = TransformPattern2Identifiers.ZoomMinimumProperty;


        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Used to adjust an element's current location. The x, and y parameters represent the  
        /// absolute on-screen position of the top-left corner in pixels, not the delta between the 
        /// desired location and the window's current location. 
        /// </summary>
        /// 
        /// <param name="x">absolute on-screen position of the top left corner</param>
        /// <param name="y">absolute on-screen position of the top left corner</param>
        public void Zoom( double zoomValue )
        {
            UiaCoreApi.TransformPattern2_Zoom(_hPattern, zoomValue);
        }

        /// <summary>
        /// Used to modify element's on-screen dimensions (affects the 
        /// BoundingRectangle and BoundingGeometry properties)
        /// When called on a split pane, it may have the side-effect of resizing
        /// other surrounding panes.
        /// </summary>
        /// <param name="width">The requested width of the window.</param>
        /// <param name="height">The requested height of the window.</param>
        public void ZoomByUnit( ZoomUnit zoomUnit )
        {
            UiaCoreApi.TransformPattern2_ZoomByUnit(_hPattern, zoomUnit);
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
        public new TransformPattern2Information Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new TransformPattern2Information(_el, true);
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
        public new TransformPattern2Information Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new TransformPattern2Information(_el, false);
            }
        }


        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static new object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new TransformPattern2(el, hPattern, cached);
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
        public struct TransformPattern2Information
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal TransformPattern2Information(AutomationElement el, bool useCache)
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

            /// <summary>Returns true if the element can be moved otherwise returns false.</summary>
            public bool CanZoom
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(CanZoomProperty, _useCache);
                }
            }

            /// <summary>Returns true if the element can be resized otherwise returns false.</summary>
            public double ZoomLevel
            {
                get
                {
                    return (double)_el.GetPatternPropertyValue(ZoomLevelProperty, _useCache);
                }
            }

            public double ZoomMinimum
            {
                get
                {
                    return (double)_el.GetPatternPropertyValue(ZoomMinimumProperty, _useCache);
                }
            }

            /// <summary>Returns true if the element can be rotated otherwise returns false.</summary>
            public double ZoomMaximum
            {
                get
                {
                    return (double)_el.GetPatternPropertyValue(ZoomMaximumProperty, _useCache);
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
