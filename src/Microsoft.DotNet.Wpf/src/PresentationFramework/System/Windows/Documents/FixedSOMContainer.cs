// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                                    
    Description:
       Abstract class that provides a common base class for all semantic containers            
--*/

namespace System.Windows.Documents
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Media;
    
    internal abstract class FixedSOMContainer :FixedSOMSemanticBox, IComparable
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        protected FixedSOMContainer()
        {
            _semanticBoxes = new List<FixedSOMSemanticBox>();
        }
        #endregion Constructors        

        int IComparable.CompareTo(object comparedObj)
        {
            int result = Int32.MinValue;

            FixedSOMPageElement compared = comparedObj as FixedSOMPageElement;
            FixedSOMPageElement This = this as FixedSOMPageElement;
            
            Debug.Assert(compared != null);
            Debug.Assert(This != null);
            if (compared == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, comparedObj.GetType(), typeof(FixedSOMContainer)), "comparedObj");
            }
            SpatialComparison compareHor = base._CompareHorizontal(compared, false);
            SpatialComparison compareVer = base._CompareVertical(compared);

            Debug.Assert(compareHor != SpatialComparison.None);
            Debug.Assert(compareVer != SpatialComparison.None);

            switch (compareHor)
            {
            case SpatialComparison.Before:
                if (compareVer != SpatialComparison.After)
                {
                    result = -1;
                }
                break;

            case SpatialComparison.After:
                if (compareVer != SpatialComparison.Before)
                {
                    result = 1;
                }
                break;

            case SpatialComparison.OverlapBefore:
                if (compareVer == SpatialComparison.Before)
                {
                    result =  -1;
                }
                else if (compareVer == SpatialComparison.After)
                {
                    result = 1;
                }
                break;

            case SpatialComparison.OverlapAfter:
                if (compareVer == SpatialComparison.After)
                {
                    result = 1;
                }
                else if (compareVer == SpatialComparison.Before)
                {
                    result = -1;
                }
                break;

            case SpatialComparison.Equal:
                switch (compareVer)
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

            if (result == Int32.MinValue)
            {
                //Indecisive. Does markup order help?

                if (This.FixedNodes.Count == 0 || compared.FixedNodes.Count == 0)
                {
                    result = 0;
                }
                else
                {
                    FixedNode thisObjFirstNode = This.FixedNodes[0];
                    FixedNode thisObjLastNode = This.FixedNodes[This.FixedNodes.Count - 1];

                    FixedNode otherObjFirstNode = compared.FixedNodes[0];
                    FixedNode otherObjLastNode = compared.FixedNodes[compared.FixedNodes.Count - 1];

                    if (This.FixedSOMPage.MarkupOrder.IndexOf(otherObjFirstNode) - This.FixedSOMPage.MarkupOrder.IndexOf(thisObjLastNode) == 1)
                    {
                        result = -1;
                    }
                    else if (This.FixedSOMPage.MarkupOrder.IndexOf(otherObjLastNode) - This.FixedSOMPage.MarkupOrder.IndexOf(thisObjFirstNode) == 1)
                    {
                        result =  1;
                    }
                    else
                    {
                        //Indecisive. Whichever is below comes after; if same whichever is on the right comes after
                        int absVerComparison = _SpatialToAbsoluteComparison(compareVer);
                        result = absVerComparison != 0 ? absVerComparison : _SpatialToAbsoluteComparison(compareHor);
                    }
                }
            }
                
            return result;
        }
        #region Protected Methods

        protected void AddSorted(FixedSOMSemanticBox box)
        {
            int i=_semanticBoxes.Count-1;
            for (; i>=0; i--)
            {
                if (box.CompareTo(_semanticBoxes[i]) == 1)
                {
                    break;
                }
            }
            _semanticBoxes.Insert(i+1, box);

            _UpdateBoundingRect(box.BoundingRect);
        }

        protected void Add(FixedSOMSemanticBox box)
        {
            _semanticBoxes.Add(box);
            _UpdateBoundingRect(box.BoundingRect);
        }
        #endregion

        #region public Properties

        internal virtual FixedElement.ElementType[] ElementTypes
        {
            get
            {
                return Array.Empty<FixedElement.ElementType>();
            }
        }

        public List<FixedSOMSemanticBox> SemanticBoxes
        {
            get
            {
                return _semanticBoxes;
            }
            set
            {
                _semanticBoxes = value;
            }
        }

        public List<FixedNode> FixedNodes
        {
            get
            {
                if (_fixedNodes == null)
                {
                    _ConstructFixedNodes();
                }
                return _fixedNodes;
            }
        }

        #endregion

        #region Private methods

        void _ConstructFixedNodes()
        {
            _fixedNodes = new List<FixedNode>();
            foreach (FixedSOMSemanticBox box in _semanticBoxes)
            {
                FixedSOMElement element = box as FixedSOMElement;
                if (element != null)
                {
                    _fixedNodes.Add(element.FixedNode);
                }
                else
                {
                    FixedSOMContainer container = box as FixedSOMContainer;
                    Debug.Assert(container != null);
                    List<FixedNode> nodes = container.FixedNodes;
                    foreach (FixedNode node in nodes)
                    {
                        _fixedNodes.Add(node);
                    }
                }
            }
        }

        void _UpdateBoundingRect(Rect rect)
        {
            if (_boundingRect.IsEmpty)
            {
                _boundingRect = rect;
            }
            else
            {
                _boundingRect.Union(rect);
            }
        }


        #endregion Private methods

        //--------------------------------------------------------------------
        //
        // Protected Fields
        //
        //---------------------------------------------------------------------

        #region Protected Fields
        protected List<FixedSOMSemanticBox> _semanticBoxes;
        protected List<FixedNode> _fixedNodes;
        #endregion Protected Fields
        
    }
}
