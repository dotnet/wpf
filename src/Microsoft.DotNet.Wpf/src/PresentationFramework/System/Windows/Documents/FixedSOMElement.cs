// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
    Description:
       Abstract class that provides a common base class for all non-container semantic elements.
       These elements have a fixed node and start and end symbol indices associated with them.

--*/

namespace System.Windows.Documents
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Windows.Controls;

    internal abstract class FixedSOMElement : FixedSOMSemanticBox
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        protected FixedSOMElement(FixedNode fixedNode, int startIndex, int endIndex, GeneralTransform transform)
        {
            _fixedNode = fixedNode;
            _startIndex = startIndex;
            _endIndex = endIndex;
            Transform trans = transform.AffineTransform;
            if (trans != null)
            {
                _mat = trans.Value;
            }
            else
            {
                _mat = Transform.Identity.Value;
            }
        }
        
        protected FixedSOMElement(FixedNode fixedNode, GeneralTransform transform)
        {
            _fixedNode = fixedNode;

            Transform trans = transform.AffineTransform;
            if (trans != null)
            {
                _mat = trans.Value;
            }
            else
            {
                _mat = Transform.Identity.Value;
            }
        }
        
        #endregion Constructors


        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Static methods

        public static FixedSOMElement CreateFixedSOMElement(FixedPage page, UIElement uiElement, FixedNode fixedNode, int startIndex, int endIndex)
        {
            FixedSOMElement element = null;
            if (uiElement is Glyphs)
            {
                Glyphs glyphs = uiElement as Glyphs;
                if (glyphs.UnicodeString.Length > 0)
                {
                    GlyphRun glyphRun = glyphs.ToGlyphRun();
                    Rect alignmentBox = glyphRun.ComputeAlignmentBox();
                    alignmentBox.Offset(glyphs.OriginX, glyphs.OriginY);
                    GeneralTransform transform = glyphs.TransformToAncestor(page);
                    
                    if (startIndex < 0)
                    {
                        startIndex = 0;
                    }
                    if (endIndex < 0)
                    {
                        endIndex = glyphRun.Characters == null ? 0 : glyphRun.Characters.Count;
                    }
                    element = FixedSOMTextRun.Create(alignmentBox, transform, glyphs, fixedNode, startIndex, endIndex, false);
                }
            }
            else if (uiElement is Image)
            {
                element = FixedSOMImage.Create(page, uiElement as Image, fixedNode);
            }
            else if (uiElement is Path)
            {
                element = FixedSOMImage.Create(page, uiElement as Path, fixedNode);
            }
            return element;
        }

        #endregion Static methods


        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties

        public FixedNode FixedNode
        {
            get
            {
                return _fixedNode;
            }
        }

        public int StartIndex
        {
            get
            {
                return _startIndex;
            }
        }

        public int EndIndex
        {
            get
            {
                return _endIndex;
            }
        }

        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        internal FlowNode FlowNode
        {
            get
            {
                return _flowNode;
            }
            set
            {
                _flowNode = value;
            }
        }

        internal int OffsetInFlowNode
        {
            get
            {
                return _offsetInFlowNode;
            }
            set
            {
                _offsetInFlowNode = value;
            }
        }

        internal Matrix Matrix
        {
            get
            {
                return _mat;
            }
        }

        #endregion Internal Properties


        //--------------------------------------------------------------------
        //
        // Protected Fields
        //
        //---------------------------------------------------------------------

        #region Protected Fields
        protected FixedNode _fixedNode ;
        protected int _startIndex;
        protected int _endIndex;
        protected Matrix _mat;
        #endregion Protected Fields

        #region Private Fields
        private FlowNode _flowNode;
        private int _offsetInFlowNode;
        #endregion Private Fields

    }
}

