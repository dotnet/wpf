// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Transform Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    ///<summary>wrapper class for Transform pattern </summary>
#if (INTERNAL_COMPILE)
    internal class TransformPattern: BasePattern
#else
    public class TransformPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TransformPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
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

        /// <summary>Returns the Transform pattern identifier</summary>
        public static readonly AutomationPattern Pattern = TransformPatternIdentifiers.Pattern;

        /// <summary>Property ID: CanMove - This window can be moved</summary>
        public static readonly AutomationProperty CanMoveProperty = TransformPatternIdentifiers.CanMoveProperty;

        /// <summary>Property ID: CanResize - This window can be resized</summary>
        public static readonly AutomationProperty CanResizeProperty = TransformPatternIdentifiers.CanResizeProperty;

        /// <summary>Property ID: CanRotate - This window can be rotated</summary>
        public static readonly AutomationProperty CanRotateProperty = TransformPatternIdentifiers.CanRotateProperty;


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
        public void Move( double x, double y )
        {
            UiaCoreApi.TransformPattern_Move(_hPattern, x, y);
        }

        /// <summary>
        /// Used to modify element's on-screen dimensions (affects the 
        /// BoundingRectangle and BoundingGeometry properties)
        /// When called on a split pane, it may have the side-effect of resizing
        /// other surrounding panes.
        /// </summary>
        /// <param name="width">The requested width of the window.</param>
        /// <param name="height">The requested height of the window.</param>
        public void Resize( double width, double height )
        {
            UiaCoreApi.TransformPattern_Resize(_hPattern, width, height);
        }

        /// <summary>
        /// Rotate the element the specified number of degrees.
        /// </summary>
        /// <param name="degrees">The requested degrees to rotate the element.  A positive number rotates clockwise
        /// a negative number rotates counter clockwise</param>
        public void Rotate( double degrees )
        {
            UiaCoreApi.TransformPattern_Rotate(_hPattern, degrees);
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
        public TransformPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new TransformPatternInformation(_el, true);
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
        public TransformPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new TransformPatternInformation(_el, false);
            }
        }


        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new TransformPattern(el, hPattern, cached);
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
        public struct TransformPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal TransformPatternInformation(AutomationElement el, bool useCache)
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
            public bool CanMove
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(CanMoveProperty, _useCache);
                }
            }

            /// <summary>Returns true if the element can be resized otherwise returns false.</summary>
            public bool CanResize
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(CanResizeProperty, _useCache);
                }
            }

            /// <summary>Returns true if the element can be rotated otherwise returns false.</summary>
            public bool CanRotate
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(CanRotateProperty, _useCache);
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
