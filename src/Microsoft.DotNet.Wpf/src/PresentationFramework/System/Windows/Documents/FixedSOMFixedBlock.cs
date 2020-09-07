// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                              
    Description:
        This class reprsents a table row on the page. It would contain several table cells           
--*/

namespace System.Windows.Documents
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Media;
    using System.Globalization;
    using System.Diagnostics;
    using System.Text;
    
    internal sealed class FixedSOMFixedBlock : FixedSOMPageElement
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMFixedBlock(FixedSOMPage page) : base(page)
        {
        }
        #endregion Constructors

        #region Public Properties

        public double LineHeight
        {
            get
            {
                FixedSOMTextRun lastRun = this.LastTextRun;
                if (lastRun != null)
                {
                    //Need to check for edge case - subscript or superscript at the end of a line
                    if (this.SemanticBoxes.Count > 1)
                    {
                         FixedSOMTextRun run = this.SemanticBoxes[this.SemanticBoxes.Count - 2] as FixedSOMTextRun;
                         if (run != null &&
                             lastRun.BoundingRect.Height / run.BoundingRect.Height < 0.75 &&
                             run.BoundingRect.Left != lastRun.BoundingRect.Left &&
                             run.BoundingRect.Right != lastRun.BoundingRect.Right &&
                             run.BoundingRect.Top != lastRun.BoundingRect.Top &&
                             run.BoundingRect.Bottom != lastRun.BoundingRect.Bottom)
                         {
                            return run.BoundingRect.Height;
                         }
                    }
                    return lastRun.BoundingRect.Height;
                }
                else
                {
                    return 0;
                }
            }
        }
        
        //return true if this FixedBlock is a wrapper around a floating image
        public bool IsFloatingImage
        {
            get
            {
                return (_semanticBoxes.Count == 1 && (_semanticBoxes[0] is FixedSOMImage));
            }
        }

        internal override FixedElement.ElementType[] ElementTypes
        {
            get
            {
                return new FixedElement.ElementType[1] { FixedElement.ElementType.Paragraph };
            }
        }

        public bool IsWhiteSpace
        {
            get
            {
                if (_semanticBoxes.Count == 0)
                {
                    return false;
                }
                foreach (FixedSOMSemanticBox box in _semanticBoxes)
                {
                    FixedSOMTextRun run = box as FixedSOMTextRun;
                    if (run == null ||
                        !run.IsWhiteSpace)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override bool IsRTL
        {
            get
            {
                return _RTLCount > _LTRCount;
            }
        }


        public Matrix Matrix
        {
            get
            {
                return _matrix;
            }
        }


        #endregion Public Properties

        #region Private Properties

        private FixedSOMTextRun LastTextRun
        {
            get
            {
                FixedSOMTextRun run = null;
                for (int i=_semanticBoxes.Count - 1; i>=0 && run==null; i--)
                {
                    run = _semanticBoxes[i] as FixedSOMTextRun;
                }
                    
                return run;
            }
        }

        #endregion Private Properties

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region Public Methods
#if DEBUG
        public override void Render(DrawingContext dc, string label, DrawDebugVisual debugVisual)
        {
            Pen pen = new Pen(Brushes.Blue, 2);
            Rect rect = _boundingRect;
            rect.Inflate(3,3);
            dc.DrawRectangle(null, pen , rect);

            if (debugVisual == DrawDebugVisual.Paragraphs && label != null)
            {
                base.RenderLabel(dc, label);
            }

            for (int i=0; i<SemanticBoxes.Count; i++)
            {
                Debug.Assert(SemanticBoxes[i] is FixedSOMTextRun || SemanticBoxes[i] is FixedSOMImage);
                SemanticBoxes[i].Render(dc, i.ToString(),debugVisual);
            }
            
        }
#endif
        public void CombineWith(FixedSOMFixedBlock block)
        {
            foreach (FixedSOMSemanticBox box in block.SemanticBoxes)
            {
                FixedSOMTextRun run = box as FixedSOMTextRun;
                if (run != null)
                {
                    AddTextRun(run);
                }
                else
                {
                    base.Add(box);
                }
            }
        }

        public void AddTextRun(FixedSOMTextRun textRun)
        {
            _AddElement(textRun);
            textRun.FixedBlock = this;

            if (!textRun.IsWhiteSpace)
            {
                if (textRun.IsLTR)
                {
                    _LTRCount++;
                }
                else
                {
                    _RTLCount++;
                }
            }
        }

        public void AddImage(FixedSOMImage image)
        {
            _AddElement(image);
        }

        public override void SetRTFProperties(FixedElement element)
        {
            if (this.IsRTL)
            {
                element.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection.RightToLeft);
            }
        }        


#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (FixedSOMSemanticBox box in _semanticBoxes)
            {
                FixedSOMTextRun run  = box as FixedSOMTextRun;
                if (run != null)
                {
                    builder.Append(run.Text);
                    builder.Append(" ");
                }
            }
            return builder.ToString();
        }
#endif


        #endregion Public methods

        #region Private methods
        private void _AddElement(FixedSOMElement element)
        {
            base.Add(element);
            
            if (_semanticBoxes.Count == 1)
            {
                _matrix = element.Matrix;
                _matrix.OffsetX = 0;
                _matrix.OffsetY = 0;
            }            
        }

        #endregion Private methods        

        #region Private fields
        private int _RTLCount;
        private int _LTRCount;
        private Matrix _matrix; //This matrix is to keep track of rotation and scale. Offsets are not stored
        #endregion Private fields
    }
}

