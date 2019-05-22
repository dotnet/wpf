// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements FixedHighlight 
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Media;                 // Brush
    using System.Windows.Media.TextFormatting;  // CharacterHit
    using System.Windows.Shapes;                // Glyphs
    using System.Windows.Controls;              // Image



    //=====================================================================
    /// <summary>
    /// FixedHighlight represents partial glyph run that is highlighted on a fixed document. 
    /// </summary>
    internal sealed class FixedHighlight
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        /// <summary>
        ///    Create a new FixedHighlight for a Glyphs with character offset of
        ///    beginOffset to endOffset, to be rendered with a given brush.
        /// </summary>
        internal FixedHighlight(UIElement element, int beginOffset, int endOffset, FixedHighlightType t,
                                Brush foreground, Brush background)
        {
            Debug.Assert(element != null && beginOffset >= 0 && endOffset >= 0);
            _element = element;
            _gBeginOffset = beginOffset;
            _gEndOffset = endOffset;
            _type = t;
            _foregroundBrush = foreground;
            _backgroundBrush = background;
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region Public Methods
        /// <summary>
        /// Compares 2 FixedHighlight
        /// </summary>
        /// <param name="oCompare">the FixedHighlight to compare with</param>
        /// <returns>true if this FixedHighlight is on the same element with the same offset, and brush</returns>
        override public bool Equals(object oCompare)
        {
            FixedHighlight fh = oCompare as FixedHighlight;

            if (fh == null)
            {
                return false;
            }

            return (fh._element == _element) && (fh._gBeginOffset == _gBeginOffset) && (fh._gEndOffset == _gEndOffset) && (fh._type == _type);
        }

        /// <summary>
        /// Overloaded method from object
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return _element == null ? 0 : _element.GetHashCode() + _gBeginOffset + _gEndOffset + (int)_type;
        }
        #endregion Public Methods

        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        // Compute the rectangle that covers this highlight
        internal Rect ComputeDesignRect()
        {
            Glyphs g = _element as Glyphs;
            if (g == null)
            {
                Image im = _element as Image;
                if (im != null && im.Source != null)
                {
                    return new Rect(0, 0, im.Width, im.Height);
                }
                else
                {
                    Path p = _element as Path;
                    if (p != null)
                    {
                        return p.Data.Bounds;
                    }
                }

                return Rect.Empty;
            }

            GlyphRun run = g.MeasurementGlyphRun; // g.ToGlyphRun();
            if (run == null || _gBeginOffset >= _gEndOffset)
            {
                return Rect.Empty;
            }

            Rect designRect = run.ComputeAlignmentBox();
            designRect.Offset(g.OriginX, g.OriginY);

            int chrct = (run.Characters == null ? 0 : run.Characters.Count);

            Debug.Assert(_gBeginOffset >= 0);
            Debug.Assert(_gEndOffset <= chrct);

            double x1, x2, width;

            x1 = run.GetDistanceFromCaretCharacterHit(new CharacterHit(_gBeginOffset, 0));

            if (_gEndOffset == chrct)
            {
                x2 = run.GetDistanceFromCaretCharacterHit(new CharacterHit(chrct - 1, 1));
            }
            else
            {
                x2 = run.GetDistanceFromCaretCharacterHit(new CharacterHit(_gEndOffset, 0));
            }

            if (x2 < x1)
            {
                double temp = x1;
                x1 = x2;
                x2 = temp;
            }
            width = x2 - x1;

            if ((run.BidiLevel & 1) != 0)
            {
                // right to left
                designRect.X = g.OriginX - x2;
            }
            else
            {
                designRect.X = g.OriginX + x1;
            }

            designRect.Width = width;

#if DEBUG
            DocumentsTrace.FixedTextOM.Highlight.Trace(string.Format("DesignBound {0}", designRect));
#endif
            return designRect;
        }
        #endregion Internal Methods


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        internal FixedHighlightType HighlightType
        {
            get { return _type; }
        }

        internal Glyphs Glyphs
        {
            get { return _element as Glyphs; }
        }

        internal UIElement Element
        {
            get { return _element; }
        }

        internal Brush ForegroundBrush
        {
            get
            {
                return _foregroundBrush;
            }
        }

        internal Brush BackgroundBrush
        {
            get
            {
                return _backgroundBrush;
            }
        }

        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly UIElement _element;             // the Glyphs element, or possibly an image
        private readonly int _gBeginOffset;  // Begin character offset with Glyphs
        private readonly int _gEndOffset;    // End character offset with Glyphs
        private readonly FixedHighlightType _type;  // Type of highlight
        private readonly Brush _backgroundBrush; // highlight background brush
        private readonly Brush _foregroundBrush; // highlight foreground brush
        #endregion Private Fields
    }

    /// <summary>
    /// Flags determine type of highlight
    /// </summary>
    internal enum FixedHighlightType
    {
        None = 0,
        TextSelection = 1,
        AnnotationHighlight = 2
    }
}
