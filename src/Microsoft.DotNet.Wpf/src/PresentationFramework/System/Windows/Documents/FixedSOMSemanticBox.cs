// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                     
    Description:
        This class is the abstract base class for all the objects in SOM. It consists of a bounding rectangle, and
        implements IComparable interface to figure out content ordering on the page          
--*/

namespace System.Windows.Documents
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Media;
    using System.Windows.Markup;
    
    internal abstract class FixedSOMSemanticBox : IComparable
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMSemanticBox()
        {
            _boundingRect = Rect.Empty;
        }
        public FixedSOMSemanticBox(Rect boundingRect)
        {
            _boundingRect = boundingRect;
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------
        #region Public Properties

        public Rect BoundingRect
        {
            get
            {
                return _boundingRect;
            }
            set
            {
                _boundingRect = value;
            }
        }

        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        #region Public Methods

#if DEBUG
        //For visualization purposes
        public abstract void Render(DrawingContext dc, string label, DrawDebugVisual debugVisuals) ;
        public void RenderLabel(DrawingContext dc, string label)
        {
            // This code only runs in DEBUG mode, and looks like has been abandoned for a while.
            // Initializing PixelsPerDip to system dpi as a safeguard, however, doesn't look like this is going to be used at all.
            FormattedText ft = new FormattedText(label, 
                                        System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                                        FlowDirection.LeftToRight,
                                        new Typeface("Arial"), 
                                        10,
                                        Brushes.White,
                                        MS.Internal.FontCache.Util.PixelsPerDip);

            Point labelLocation = new Point(_boundingRect.Left-25, (_boundingRect.Bottom + _boundingRect.Top)/2 - 10);
            Geometry geom = ft.BuildHighlightGeometry(labelLocation);
            Pen backgroundPen = new Pen(Brushes.Black,1);
            dc.DrawGeometry(Brushes.Black, backgroundPen, geom);
            dc.DrawText(ft, labelLocation);
        }
#endif

        public virtual void SetRTFProperties(FixedElement element)
        {
        }

        public int CompareTo(object o)
        {
            Debug.Assert(o != null);

            if (!(o is FixedSOMSemanticBox))
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, o.GetType(), typeof(FixedSOMSemanticBox)), "o");
            }

            SpatialComparison compareHor = _CompareHorizontal(o as FixedSOMSemanticBox, false);
            
            SpatialComparison compareVer = _CompareVertical(o as FixedSOMSemanticBox);
            Debug.Assert(compareHor != SpatialComparison.None && compareVer != SpatialComparison.None);

            int result;
            if (compareHor == SpatialComparison.Equal && compareVer == SpatialComparison.Equal)
            {
                result = 0;
            }
            else if (compareHor == SpatialComparison.Equal)
            {
                if (compareVer == SpatialComparison.Before || compareVer == SpatialComparison.OverlapBefore)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            else if (compareVer == SpatialComparison.Equal)
            {
                if (compareHor == SpatialComparison.Before || compareHor == SpatialComparison.OverlapBefore)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            else if (compareHor == SpatialComparison.Before)
            {
                result = -1;        
            }
            else if (compareHor == SpatialComparison.After)
            {
                result = 1;
            }
            else
            {
                //Objects overlap
                if (compareVer == SpatialComparison.Before)
                {
                    result = -1;
                }
                else if (compareVer == SpatialComparison.After)
                {
                    result = 1;
                }
                //These objects intersect.
                else if (compareHor == SpatialComparison.OverlapBefore)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            return result;
        }
                
        #endregion Abstract Methods


        //--------------------------------------------------------------------
        //
        // IComparable
        //
        //---------------------------------------------------------------------
        #region IComparable Methods

        int IComparable.CompareTo(object o)
        {
            return this.CompareTo(o);
        }

        #endregion IComparable Methods

        //--------------------------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------------------------
        #region Private Methods

        //Method that compares horizontally according to specific reading order
        protected SpatialComparison _CompareHorizontal(FixedSOMSemanticBox otherBox, bool RTL)
        {
            SpatialComparison result = SpatialComparison.None;

            Rect thisRect = this.BoundingRect;
            Rect otherRect = otherBox.BoundingRect;

            double thisRectRefX = RTL ? thisRect.Right : thisRect.Left;
            double otherRectRefX = RTL ? otherRect.Right : otherRect.Left;

            if (thisRectRefX == otherRectRefX)
            {
                result = SpatialComparison.Equal;
            }
            //Easiest way: calculate as if it's LTR and invert the result if RTL
            else if (thisRect.Right < otherRect.Left)
            {
                //Clearly before the other object
                result = SpatialComparison.Before;
            }
            else if (otherRect.Right < thisRect.Left)
            {
                //Clearly after the other object
                result = SpatialComparison.After;
            }
            else
            {
                double overlap = Math.Abs(thisRectRefX - otherRectRefX);
                double longerWidth = thisRect.Width > otherRect.Width ? thisRect.Width : otherRect.Width;
                
                if (overlap/longerWidth < 0.1)
                {
                    //If less than 10% overlap then assume these are equal in horizontal comparison
                    result = SpatialComparison.Equal;
                }

                //Objects overlap
                else if (thisRect.Left < otherRect.Left)
                {
                    result = SpatialComparison.OverlapBefore;
                }
                else
                {
                    result =  SpatialComparison.OverlapAfter;
                }
            }
            if (RTL && result != SpatialComparison.Equal)
            {
                result = _InvertSpatialComparison(result);
            }
            return result;
        }

        //Method that compares horizontally according to specific reading order
        //In the future we should take into account the document language and plug it into this algorithm
        protected SpatialComparison _CompareVertical(FixedSOMSemanticBox otherBox)
        {
            SpatialComparison result = SpatialComparison.None;

            Rect thisRect = this.BoundingRect;
            Rect otherRect = otherBox.BoundingRect;

            if (thisRect.Top == otherRect.Top)
            {
                result =  SpatialComparison.Equal;
            }
            else if (thisRect.Bottom <= otherRect.Top)
            {
                //Clearly before the other object
                result = SpatialComparison.Before;
            }
            else if (otherRect.Bottom <= thisRect.Top)
            {
                //Clearly after the other object
                result = SpatialComparison.After;
            }
            else
            {
                //Objects overlap
                if (thisRect.Top < otherRect.Top)
                {
                    result = SpatialComparison.OverlapBefore;
                }
                else
                {
                    result =  SpatialComparison.OverlapAfter;
                }
            }
            return result;
        }

        protected int _SpatialToAbsoluteComparison(SpatialComparison comparison)
        {
            int result=0;
            
            switch (comparison)
            {
            case SpatialComparison.Before:
            case SpatialComparison.OverlapBefore:
                result = -1;
                break;

            case SpatialComparison.After:
            case SpatialComparison.OverlapAfter:
                result = 1;
                break;
            case SpatialComparison.Equal:
                result = 0;
                break;
            default:
                Debug.Assert(false);
                break;
            }
            return result;
        }

        protected SpatialComparison _InvertSpatialComparison(SpatialComparison comparison)
        {
            SpatialComparison result = comparison;
            switch (comparison)
            {
                case SpatialComparison.Before:
                    result = SpatialComparison.After;
                    break;
                case SpatialComparison.After:
                    result = SpatialComparison.Before;
                    break;
                case SpatialComparison.OverlapBefore:
                    result = SpatialComparison.OverlapAfter;
                    break;
                case SpatialComparison.OverlapAfter:
                    result = SpatialComparison.OverlapBefore;
                    break;
                default:
                    break;
            }
            return result;
        }


        #endregion Protected Methods

        #region enums        
        protected enum SpatialComparison
        {
            None =0,
            Before,
            OverlapBefore,
            Equal,
            OverlapAfter,
            After
        };
        #endregion enums

        //--------------------------------------------------------------------
        //
        // Protected Fields
        //
        //---------------------------------------------------------------------

        #region Protected Fields
        protected Rect _boundingRect;
        #endregion Protected Fields

    }
}

