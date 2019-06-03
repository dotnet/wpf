// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                               
    Description:
       A concrete container that can be used to put together to group different or same types of containers         
--*/

namespace System.Windows.Documents
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Media;
    using System.Globalization;
    
    
    internal class FixedSOMGroup :FixedSOMPageElement, IComparable
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMGroup(FixedSOMPage page) : base(page)
        {
        }
        #endregion Constructors        

        #region IComparable

        int IComparable.CompareTo(object comparedObj)
        {
            int result = Int32.MinValue;

            FixedSOMGroup compared = comparedObj as FixedSOMGroup;
            
            Debug.Assert(compared != null);
            
            if (compared == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, comparedObj.GetType(), typeof(FixedSOMGroup)), "comparedObj");
            }

            bool RTL = this.IsRTL && compared.IsRTL;
            SpatialComparison compareHor = base._CompareHorizontal(compared, RTL);
            SpatialComparison compareVer = base._CompareVertical(compared);

            Debug.Assert(compareHor != SpatialComparison.None);
            Debug.Assert(compareVer != SpatialComparison.None);

            switch (compareVer)
            {
            case SpatialComparison.Before:
                result = -1;
                break;

            case SpatialComparison.After:
                result = 1;
                break;

            case SpatialComparison.OverlapBefore:
                if ((int)compareHor <= (int)SpatialComparison.Equal)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
                break;

            case SpatialComparison.OverlapAfter:
                if ((int)compareHor >= (int)SpatialComparison.Equal)
                {
                    result = 1;
                }
                else 
                {
                    result = -1;
                }
                break;


            case SpatialComparison.Equal:
                switch (compareHor)
                {
                case SpatialComparison.After:
                case SpatialComparison.OverlapAfter:
                    result = 1;
                    break;
                case SpatialComparison.Before:
                case SpatialComparison.OverlapBefore:
                    result = -1;
                    break;
                case SpatialComparison.Equal:
                    result = 0;
                    break;
                default:
                    Debug.Assert(false);
                    break;
                }
                break;

            default:
                //Shouldn't happen
                Debug.Assert(false);
                break;
            }

            return result;
        }

        #endregion
        

        #region Public methods
        //--------------------------------------------------------------------
        //
        // Public methods
        //
        //---------------------------------------------------------------------

        public void AddContainer(FixedSOMPageElement pageElement)
        {
            FixedSOMFixedBlock block = pageElement as FixedSOMFixedBlock;
            if (block == null || (!block.IsFloatingImage && !block.IsWhiteSpace))
            {
                if (pageElement.IsRTL)
                {
                    _RTLCount++;
                }
                else
                {
                    _LTRCount++;
                }
            }
           
            _semanticBoxes.Add(pageElement);
            
            if (_boundingRect.IsEmpty)
            {
                _boundingRect = pageElement.BoundingRect;
            }
            else
            {
                _boundingRect.Union(pageElement.BoundingRect);
            }
        }
        
#if DEBUG      
        public override void Render(DrawingContext dc, string label, DrawDebugVisual debugVisual)
        {
            Pen pen = new Pen(Brushes.Maroon, 3);
            Rect rect = _boundingRect;
            rect.Inflate(5,5);
            dc.DrawRectangle(null, pen , rect);
            
            if (label != null)
            {
                if (this.IsRTL)
                {
                    label += "R";
                }
                base.RenderLabel(dc, label);
            }
            
            foreach (FixedSOMSemanticBox box in _semanticBoxes)
            {
                box.Render(dc, "", debugVisual);
            }

        }

#endif

        #endregion Public methods

        #region Public Properties
        public override bool IsRTL
        {
            get
            {
                return _RTLCount > _LTRCount;
            }
        }
        #endregion Public Properties

        #region Private fields
        private int _RTLCount;
        private int _LTRCount;        
        #endregion Private fields
        

    }
}

