// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                       
    Description:
        This class represents a (partial) Glyphs element on the page. Most of the time it will be a full glyphs element
        Partial elements are necessary when we decide that a single Glyphs element represents multiple semantic entitites such as table cells         
--*/

namespace System.Windows.Documents
{
    using System.Windows.Shapes;
    using System.Windows.Markup;    // for XmlLanguage
    using System.Windows.Media;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Collections.Generic;

    //a set of characters that have the same font, face and size
    internal sealed class FixedSOMTextRun : FixedSOMElement, IComparable
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors

        private FixedSOMTextRun(Rect boundingRect, GeneralTransform trans, FixedNode fixedNode, int startIndex, int endIndex) : base(fixedNode, startIndex, endIndex, trans)
        {
            _boundingRect = trans.TransformBounds(boundingRect);
        }
        

        #endregion Constructors

        int IComparable.CompareTo(object comparedObj)
        {
            FixedSOMTextRun otherRun = comparedObj as FixedSOMTextRun;
            Debug.Assert(otherRun != null);
            int result = 0;

            if (_fixedBlock.IsRTL)
            {
                Rect thisRect = this.BoundingRect;
                Rect otherRect = otherRun.BoundingRect;

                if (!this.Matrix.IsIdentity)
                {
                    Matrix inversionMat = _mat;
                    inversionMat.Invert();
                    thisRect.Transform(inversionMat);
                    thisRect.Offset(_mat.OffsetX, _mat.OffsetY);

                    otherRect.Transform(inversionMat);
                    otherRect.Offset(_mat.OffsetX, _mat.OffsetY);
                }

                thisRect.Offset(_mat.OffsetX, _mat.OffsetY);
                otherRect.Offset(otherRun.Matrix.OffsetX, otherRun.Matrix.OffsetY);
                
                if (FixedTextBuilder.IsSameLine(otherRect.Top - thisRect.Top, thisRect.Height, otherRect.Height))
                {
                    result = (thisRect.Left < otherRect.Left) ? 1 : -1;
                }
                else
                {
                    result = (thisRect.Top < otherRect.Top) ? -1 : +1;
                }
            }

            else
            {
                //Markup order for LTR languages
                
                List<FixedNode> markupOrder = this.FixedBlock.FixedSOMPage.MarkupOrder;
                result = markupOrder.IndexOf(this.FixedNode) - markupOrder.IndexOf(otherRun.FixedNode);
            }
            
            return result;
        }        


        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region public Methods

        public static FixedSOMTextRun Create(Rect boundingRect, GeneralTransform transform, Glyphs glyphs, FixedNode fixedNode, int startIndex, int endIndex, bool allowReverseGlyphs)
        {
            if (String.IsNullOrEmpty(glyphs.UnicodeString) ||
                glyphs.FontRenderingEmSize <= 0)
            {
                return null;
            }
            FixedSOMTextRun run = new FixedSOMTextRun(boundingRect, transform, fixedNode, startIndex, endIndex);
            run._fontUri = glyphs.FontUri;
            run._cultureInfo = glyphs.Language.GetCompatibleCulture();
            run._bidiLevel = glyphs.BidiLevel;
            run._isSideways = glyphs.IsSideways;
            run._fontSize = glyphs.FontRenderingEmSize;

            GlyphRun glyphRun = glyphs.ToGlyphRun();

            GlyphTypeface gtf = glyphRun.GlyphTypeface;
            
            // Find font family
            // glyphs.FontUri, glyphRun.glyphTypeface

            gtf.FamilyNames.TryGetValue(run._cultureInfo, out run._fontFamily);
            if (run._fontFamily == null)
            {
                //Try getting the English name
                gtf.FamilyNames.TryGetValue(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS, out run._fontFamily);
            }
            
            // Find font style (normal, italics, Oblique)
            // need to open Font file.
            run._fontStyle = gtf.Style;

            // Find font weight (bold, semibold, ExtraLight)
            run._fontWeight = gtf.Weight;

            // Find font stretch (UltraCondensed, SemiExpanded, etc)
            run._fontStretch = gtf.Stretch;

            //Height and width should be the same for x character
            run._defaultCharWidth = gtf.XHeight > 0 ? gtf.XHeight * glyphs.FontRenderingEmSize : glyphRun.AdvanceWidths[startIndex];

            Transform trans = transform.AffineTransform;
            if (trans != null &&
                !(trans.Value.IsIdentity))
            {
                Matrix mat = trans.Value;
                double yScale = Math.Sqrt(mat.M12*mat.M12 + mat.M22*mat.M22);
                double xScale = Math.Sqrt(mat.M11 * mat.M11 + mat.M21 * mat.M21);
                run._fontSize *= yScale;
                run._defaultCharWidth *= xScale;
            }

            run._foreground = glyphs.Fill;
            String s = glyphs.UnicodeString;
            run.Text = s.Substring(startIndex, endIndex-startIndex);

            if (allowReverseGlyphs && run._bidiLevel == 0 && !run._isSideways && 
                startIndex == 0 && endIndex == s.Length
                && String.IsNullOrEmpty(glyphs.CaretStops)
                && FixedTextBuilder.MostlyRTL(s))
            {
                char[] chars = new char[run.Text.Length];
                for (int i=0; i<run.Text.Length; i++)
                {
                    chars[i] = run.Text[run.Text.Length - 1 - i];
                }
                run._isReversed = true;
                run.Text = new string(chars);
            }

            if (s == "" && glyphs.Indices != null && glyphs.Indices.Length > 0)
            {
                run._isWhiteSpace = false;
            }
            else
            {
                run._isWhiteSpace = true;
                for (int i = 0; i < s.Length; i++)
                {
                    if (!Char.IsWhiteSpace(s[i]))
                    {
                        run._isWhiteSpace = false;
                        break;
                    }
                }
            }
            return run;
        }

#if DEBUG      
        public override void Render(DrawingContext dc, string label, DrawDebugVisual debugVisual)
        {
            Pen pen = new Pen(Brushes.Blue, 1);
            Rect rect = _boundingRect;
            rect.Inflate(-1,-1);
            dc.DrawRectangle(null, pen , rect);
            if (label != null && debugVisual == DrawDebugVisual.TextRuns)
            {
                base.RenderLabel(dc, label);
            }
        }

        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            return _text;
        }
#endif

        public bool HasSameRichProperties(FixedSOMTextRun run)
        {
            if (run.FontRenderingEmSize == this.FontRenderingEmSize &&
                run.CultureInfo.Equals(this.CultureInfo) &&
                run.FontStyle.Equals(this.FontStyle) &&
                run.FontStretch.Equals(this.FontStretch) &&
                run.FontWeight.Equals(this.FontWeight) &&
                run.FontFamily == this.FontFamily &&
                run.IsRTL == this.IsRTL)
            {
                SolidColorBrush thisBrush = this.Foreground as SolidColorBrush;
                SolidColorBrush otherBrush = run.Foreground as SolidColorBrush;
                if ((run.Foreground == null && this.Foreground == null) ||
                     thisBrush != null && otherBrush != null && thisBrush.Color == otherBrush.Color && thisBrush.Opacity == otherBrush.Opacity)
                {
                    return true;    
                }
            }
            return false;
        }

        public override void SetRTFProperties(FixedElement element)
        {
            if (_cultureInfo != null)
            {
                element.SetValue(FrameworkElement.LanguageProperty, XmlLanguage.GetLanguage(_cultureInfo.IetfLanguageTag));
            }
            element.SetValue(TextElement.FontSizeProperty, _fontSize);
            element.SetValue(TextElement.FontWeightProperty, _fontWeight);
            element.SetValue(TextElement.FontStretchProperty, _fontStretch);
            element.SetValue(TextElement.FontStyleProperty, _fontStyle);
            if (this.IsRTL)
            {
                element.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection.RightToLeft);
            }
            else
            {
                element.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight);
            }

            if (_fontFamily != null)
            {
                element.SetValue(TextElement.FontFamilyProperty, new FontFamily(_fontFamily));
            }
            element.SetValue(TextElement.ForegroundProperty, _foreground);
        }
        #endregion Public Methods


        #region Public Properties

        public double DefaultCharWidth
        {
            get
            {
                return _defaultCharWidth;
            }
        }

        public bool IsSideways
        {
            get
            {
                return _isSideways;
            }
        }

        public bool IsWhiteSpace
        {
            get
            {
                return _isWhiteSpace;
            }
        }

        public CultureInfo CultureInfo
        {
            get 
            {
                return _cultureInfo;
            }
        }

        public bool IsLTR
        {
            get
            {
                return ((_bidiLevel & 1) == 0) && !_isReversed;
            }
        }

        public bool IsRTL
        {
            get
            {
                return !(this.IsLTR);
            }
        }

        public String Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }

        public FixedSOMFixedBlock FixedBlock
        {
            get
            {
                return _fixedBlock;
            }
            set
            {
                _fixedBlock = value;
            }
        }

        public String FontFamily
        {
            get
            {
                return _fontFamily;
            }
        }
        /// <summary>
        /// Returns designed style (regular/italic/oblique) of this font face
        /// </summary>
        /// <value>Designed style of this font face.</value>
        public FontStyle FontStyle
        {
            get
            {
                return _fontStyle;
            }
        }

        /// <summary>
        /// Returns designed weight of this font face.
        /// </summary>
        /// <value>Designed weight of this font face.</value>
        public FontWeight FontWeight
        {
            get
            {
                return _fontWeight;
            }
        }

        /// <summary>
        /// Returns designed stretch of this font face.
        /// </summary>
        /// <value>Designed stretch of this font face.</value>
        public FontStretch FontStretch
        {
            get
            {
                return _fontStretch;
            }
        }
        
        public double FontRenderingEmSize
        {
            get
            {
                return _fontSize;
            }
        }

        public Brush Foreground
        {
            get
            {
                return _foreground;
            }
        }

        public bool IsReversed
        {
            get
            {
                return _isReversed;
            }
        }


        #endregion public Properties

        #region Internal Properties
        internal int LineIndex
        {
            get { return _lineIndex; }
            set { _lineIndex = value; }
        }
        #endregion Internal Properties
        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------
        #region Private Fields

        private double _defaultCharWidth;
        private Uri _fontUri;
        private CultureInfo _cultureInfo;
        private bool _isSideways;
        private int _bidiLevel;
        private bool _isWhiteSpace;
        private bool _isReversed;
        private FixedSOMFixedBlock _fixedBlock;
        private int _lineIndex;
        private String _text;
        private Brush _foreground;
        private double _fontSize;
        private String _fontFamily;
        private FontStyle _fontStyle;
        private FontWeight _fontWeight;
        private FontStretch _fontStretch;

        #endregion Private Fields
    }
}



