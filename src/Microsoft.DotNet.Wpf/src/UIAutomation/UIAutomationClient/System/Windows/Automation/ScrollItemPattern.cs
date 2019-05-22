// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for ScrollItem Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents UI elements in a scrollable area that can be scrolled to.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class ScrollItemPattern: BasePattern
#else
    public class ScrollItemPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private ScrollItemPattern(AutomationElement el, SafePatternHandle hPattern)
            : base(el, hPattern)
        {
            _hPattern = hPattern;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Scroll pattern</summary>
        public static readonly AutomationPattern Pattern = ScrollItemPatternIdentifiers.Pattern;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Scrolls the windows containing this automation element to make this element visible.
        /// InvalidOperationException should be thrown if item becomes unable to be scrolled. Makes
        /// no guarantees about where the item will be in the scrolled window.
        /// </summary>
       public void ScrollIntoView()
        {
            UiaCoreApi.ScrollItemPattern_ScrollIntoView(_hPattern);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        static internal object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new ScrollItemPattern(el, hPattern);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;

        #endregion Private Fields
    }
}
