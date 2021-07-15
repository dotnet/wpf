// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Represent a node in the doubly linked list a DocumentSequenceTextContainer
//      maintains for the child-TextContainers it aggregates.  Each child-TextContainer
//      would have a corresponding entry in the list.
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System;
    using System.Diagnostics;


    //=====================================================================
    /// <summary>
    /// Represent a node in the doubly linked list a DocumentSequenceTextContainer
    /// maintains for the child-TextContainers it aggregates.  Each child-TextContainer
    /// would have a corresponding entry in the list.
    /// </summary>
    internal sealed class ChildDocumentBlock
    {
        //--------------------------------------------------------------------
        //
        // Enum
        //
        //---------------------------------------------------------------------

        [Flags()]
        internal enum BlockStatus
        {
            None = 0,
            UnloadedBlock = 1,          // Child block has not been loaded
        }

        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------


        #region Ctors
        internal ChildDocumentBlock(DocumentSequenceTextContainer aggregatedContainer, ITextContainer childContainer)
        {
            Debug.Assert(childContainer != null);
            _aggregatedContainer = aggregatedContainer;
            _container = childContainer;
        }

        internal ChildDocumentBlock(DocumentSequenceTextContainer aggregatedContainer, DocumentReference docRef)
        {
            Debug.Assert(docRef != null);
            _aggregatedContainer = aggregatedContainer;
            _docRef = docRef;
            _SetStatus(BlockStatus.UnloadedBlock);
        }
        #endregion Ctors


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------
        #region Internal Methods

        // Insert a new node into this list.
        internal ChildDocumentBlock InsertNextBlock(ChildDocumentBlock newBlock)
        {
            Debug.Assert(newBlock != null);

            // Setup the new block correctly
            newBlock._nextBlock     = this._nextBlock;
            newBlock._previousBlock = this;

            // Link old next block to the new block
            if (this._nextBlock != null)
            {
                this._nextBlock._previousBlock = newBlock;
            }

            // Link this block to new block
            this._nextBlock = newBlock;

            return newBlock;
        }

        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        // The AggregatedContainer that holds the list of ChildDocumentBlock.
        internal DocumentSequenceTextContainer AggregatedContainer
        {
            get
            {
                return _aggregatedContainer;
            }
        }

        // Reference to the child TextContainer
        internal ITextContainer ChildContainer
        {
            get
            {
                _EnsureBlockLoaded();
                return _container;
            }
        }

        // The highlight layer used to notify child TextContainer
        // about TextSelection highlight change.
        internal DocumentSequenceHighlightLayer ChildHighlightLayer
        {
            get
            {
                if (_highlightLayer == null)
                {
                    _highlightLayer = new DocumentSequenceHighlightLayer(_aggregatedContainer);
                    Debug.Assert(ChildContainer.Highlights.GetLayer(typeof(TextSelection)) == null);
                    ChildContainer.Highlights.AddLayer(_highlightLayer);
                }
                return _highlightLayer;
            }
        }


        // Reference to the DocumentReference
        internal DocumentReference DocRef
        {
            get
            {
                return _docRef;
            }
        }

        // End TextPointer of the child container
        internal ITextPointer End
        {
            get
            {
                return ChildContainer.End;
            }
        }

#if DEBUG
        // Get if this entry is non-head/tail block in the linked list
        internal bool IsInsideBlock
        {
            get
            {
                return (this._previousBlock != null && this._nextBlock != null);
            }
        }
#endif

        // Get if this entry is Head of list
        internal bool IsHead
        {
            get
            {
                return (this._previousBlock == null);
            }
        }


        // Get if this entry is Tail of list
        internal bool IsTail
        {
            get
            {
                return (this._nextBlock == null);
            }
        }


        // Previous container entry in the link list
        internal ChildDocumentBlock PreviousBlock
        {
            get
            {
                return _previousBlock;
            }
        }

        // Next container entry in the link list
        internal ChildDocumentBlock NextBlock
        {
            get
            {
                return _nextBlock;
            }
        }

#if DEBUG
        internal uint DebugId
        {
            get
            {
                return _debugId;
            }
        }
#endif
        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods
        private void _EnsureBlockLoaded()
        {
            if (_HasStatus(BlockStatus.UnloadedBlock))
            {
                _ClearStatus(BlockStatus.UnloadedBlock);

                DocumentsTrace.FixedDocumentSequence.TextOM.Trace("Loading TextContainer " + _docRef.ToString());
                // Load the TextContainer
                IDocumentPaginatorSource idp = _docRef.GetDocument(false /*forceReload*/);
                IServiceProvider isp = idp as IServiceProvider;
                if (isp != null)
                {
                    ITextContainer tc = isp.GetService(typeof(ITextContainer)) as ITextContainer;
                    if (tc != null)
                    {
                        _container = tc;
                        DocumentsTrace.FixedDocumentSequence.TextOM.Trace("Got ITextContainer");
                    }
                }

                if (_container == null)
                {
                    _container = new NullTextContainer();
                }
            }
        }


        private bool _HasStatus(BlockStatus flags)
        {
            return ((_status & flags) == flags);
        }

        private void _SetStatus(BlockStatus flags)
        {
            _status |= flags;
        }

        private void _ClearStatus(BlockStatus flags)
        {
            _status &= (~flags);
        }
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly DocumentSequenceTextContainer _aggregatedContainer;
        private readonly DocumentReference _docRef;          // Reference to the sub-document
        private ITextContainer _container;           // Child TextContainer
        private DocumentSequenceHighlightLayer  _highlightLayer;  // Highlight layer used to notify child TextContainer highlight changes.
        private BlockStatus _status;                // Status of this block, such as Loaded, etc.
        private ChildDocumentBlock _previousBlock;  // Link to previous block
        private ChildDocumentBlock _nextBlock;      // Link to next block

#if DEBUG
        private uint _debugId = (_debugIdCounter++);
        private static uint _debugIdCounter = 0;
#endif
        #endregion  Private Fields
    }
}
