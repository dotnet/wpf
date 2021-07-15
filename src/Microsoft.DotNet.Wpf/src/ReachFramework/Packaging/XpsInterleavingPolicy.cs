// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for the all classes associated with the interleaving
        implementation.

--*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Windows.Xps.Serialization;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// Enumerates the vairous types of interlaving provided
    /// by the XpsInterleavingPolicy
    /// </summary>
    public
    enum
    PackageInterleavingOrder
    {
        /// <summary>
        /// No interleaving.   Nodes are flushed when committed
        /// </summary>
        None = 0,
        /// <summary>
        /// When mark up references a dependedent the depenedent has been
        /// streamed out out first
        /// Resources, pages, documents, document sequence
        /// </summary>
        ResourceFirst,
        /// <summary>
        /// Knowledge of how the resource is to be used is streamed first.
        /// Document sequence, documents, pages resources
        /// </summary>
        ResourceLast,
        /// <summary>
        /// Most depenedents are made availble first except images
        /// so that text can be displayed before the often larger images
        /// has been streamed
        /// </summary>
        ImagesLast
    }
        
    /// <summary>
    /// Types of events that occur on the package
    /// </summary>
    public enum PackagingAction
    {
        /// <summary>
        /// No action was taken
        /// </summary>
        None = 0,
        /// <summary>
        /// A Document Sequence has been added
        /// </summary>
        AddingDocumentSequence = 1,
        /// <summary>
        /// A Document Sequence has been completed
        /// </summary>
        DocumentSequenceCompleted  = 2,
        /// <summary>
        /// A Document has been added
        /// </summary>
        AddingFixedDocument    = 3,
        /// <summary>
        ///
        /// </summary>
        FixedDocumentCompleted     = 4,
        /// <summary>
        /// A Page has been added
        /// </summary>
        AddingFixedPage        = 5,
        /// <summary>
        ///
        /// </summary>
        FixedPageCompleted     = 6,
        /// <summary>
        /// A resource has been added
        /// </summary>
        ResourceAdded          = 7,
        /// <summary>
        /// A font has been added
        /// </summary>
        FontAdded              = 8,
        /// <summary>
        /// A Image has been added
        /// </summary>
        ImageAdded             = 9,
        /// <summary>
        /// The Xps Document has been commited
        /// </summary>
        XpsDocumentCommitted   = 10
    };
    /// <summary>
    ///
    /// </summary>
    public class PackagingProgressEventArgs :
                 EventArgs
    {
        /// <summary>
        ///
        /// </summary>
        public
        PackagingProgressEventArgs(
            PackagingAction action,
            int             numberCompleted
            )
        {
            _numberCompleted = numberCompleted;
            _action = action;
        }

        /// <summary>
        /// Number of Pages Completed
        /// </summary>
        public
        int
        NumberCompleted
        {
            get
            {
                return _numberCompleted;
            }
        }
        
        /// <summary>
        /// Number of Pages Completed
        /// </summary>
        public
        PackagingAction
        Action
        {
            get
            {
                return _action;
            }
        }

        private PackagingAction _action;
        private int             _numberCompleted;
    };
    /// <summary>
    /// </summary>
    public
    delegate
    void
    PackagingProgressEventHandler(
        object                     sender,
        PackagingProgressEventArgs     e
        );

    /// <summary>
    /// This class implements the base of the interleaving policy
    /// for all Xps packages written using the XpsPackage class.
    /// The purpose of the class is to define an order in which the
    /// nodes currently being written to the package will be flushed
    /// to the package.
    /// </summary>
    internal class XpsInterleavingPolicy
    {
        #region Constructors

        /// <summary>
        /// Constructs a XpsInterleavingPolicy which is used to
        /// control the flushing order of all parts added to a
        /// package.
        /// </summary>
        public
        XpsInterleavingPolicy(
            PackageInterleavingOrder type,
            bool flushOnSubsetComplete
            )
        {
            _flushOrderItems = new Hashtable(11);
            _interleavingType = type;
            switch( type)
            {
            case PackageInterleavingOrder.None:
            break;
            
            case PackageInterleavingOrder.ResourceFirst:
            {
                InitializeResourceFirst();
                break;
            }
            
            case PackageInterleavingOrder.ResourceLast:
            {
                InitializeResourceLast();
                break;
            }
            
            case PackageInterleavingOrder.ImagesLast:
            {
                InitializeImagesLast();
                break;
            }
            
            default:
            break;
            }
           
           _flushOnSubsetComplete = flushOnSubsetComplete;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// This method registers a given Type with the supplied
        /// flush order.  The higher the order, the sooner the
        /// Type is flushed during a commit operation.
        /// </summary>
        /// <param name="flushOrder">
        /// The flush order of the given Type.
        /// </param>
        /// <param name="classType">
        /// The class Type to register.
        /// </param>
        public
        void
        RegisterFlushOrder(
            FlushOrder      flushOrder,
            Type            classType
            )
        {
            _flushOrderItems.Add(classType, new FlushItem(flushOrder, classType));
        }

        /// <summary>
        ///
        /// </summary>
        internal
        event
        PackagingProgressEventHandler PackagingProgressEvent;
        
        #endregion Public methods

        #region Internal methods

        /// <summary>
        /// This signals that subsetting is complete
        /// and if the interleaving is depenedent on subsetting
        /// the nodes can be flushed
        /// </summary>
        internal
        void
        SignalSubsetComplete()
        {
            if( _flushOnSubsetComplete )
            {
                Flush();            }
        }
        
        /// <summary>
        /// This method flushes all nodes in the tree using
        /// the supplied node to walk up and down the tree.
        /// The flush order is based on the registered types.
        /// </summary>
        /// <param name="node">
        /// The node used to walk up and down the tree to
        /// build the node flush list.
        /// </param>
        internal
        void
        Commit(
            INode       node
            )
        {
            if ( null == node )
            {
                throw new ArgumentNullException("node");
            }

            //
            // If the current node is not part of the flush order sequence
            // then we just flush it alone and return
            //
            if (!_flushOrderItems.ContainsKey(node.GetType()) ||
                _interleavingType == PackageInterleavingOrder.None)
            {
                RemoveNode(node);
                node.CommitInternal();
            }
            else
            {
                MarkNodeCommited( node );
                
                if( !_flushOnSubsetComplete )
                {
                    Flush();
                }
            }
        }

        internal
        void
        AddItem( INode n, int number, INode parent )
        {
            _interleavingNodes.Add( new InterleavingNode( n, number, parent ) );
            PackagingAction action = GetAddType(n);
            if (PackagingProgressEvent != null && action != PackagingAction.None )
            {
                PackagingProgressEvent( this, new PackagingProgressEventArgs( action, 1 ) );
            }
        }

        internal
        PackagingAction
        GetAddType( INode n )
        {
            PackagingAction action = PackagingAction.None;
            
            if( n is IXpsFixedDocumentSequenceWriter )
            {
                action = PackagingAction.AddingDocumentSequence;
            }
            else
            if( n is IXpsFixedDocumentWriter )
            {
                action = PackagingAction.AddingFixedDocument;
            }
            else
            if( n is IXpsFixedPageWriter )
            {
                action = PackagingAction.AddingFixedPage;
            }
            else
            if( n is XpsImage)
            {
                action = PackagingAction.ImageAdded;
            }
            else
            if( n is XpsFont)
            {
                action = PackagingAction.FontAdded;
            }
            else
            if( n is XpsResource)
            {
                action = PackagingAction.ResourceAdded;
            }
            return action;
}
        #endregion Internal methods

        #region Private methods
        private
        void
        InitializeResourceFirst()
        {
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsResource));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsImage));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsFont));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsColorContext));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsResourceDictionary));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsThumbnail));
           RegisterFlushOrder(FlushOrder.SecondOrder, typeof(XpsFixedPageReaderWriter));
           RegisterFlushOrder(FlushOrder.ThirdOrder, typeof(XpsFixedDocumentReaderWriter));
           RegisterFlushOrder(FlushOrder.FourthOrder, typeof(XpsFixedDocumentSequenceReaderWriter));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsDocument));
        }

        private
        void
        InitializeResourceLast()
        {
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsDocument));
           RegisterFlushOrder(FlushOrder.SecondOrder, typeof(XpsFixedDocumentSequenceReaderWriter));
           RegisterFlushOrder(FlushOrder.ThirdOrder,  typeof(XpsFixedDocumentReaderWriter));
           RegisterFlushOrder(FlushOrder.FourthOrder, typeof(XpsFixedPageReaderWriter));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsResource));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsImage));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsFont));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsColorContext));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsResourceDictionary));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsThumbnail));
        }
        
        private
        void
        InitializeImagesLast()
        {
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsResource));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsFont));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsColorContext));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsResourceDictionary));
           RegisterFlushOrder(FlushOrder.FirstOrder, typeof(XpsThumbnail));
           RegisterFlushOrder(FlushOrder.SecondOrder, typeof(XpsDocument));
           RegisterFlushOrder(FlushOrder.ThirdOrder, typeof(XpsFixedDocumentSequenceReaderWriter));
           RegisterFlushOrder(FlushOrder.FourthOrder,  typeof(XpsFixedDocumentReaderWriter));
           RegisterFlushOrder(FlushOrder.FifthOrder, typeof(XpsFixedPageReaderWriter));
           RegisterFlushOrder(FlushOrder.SixthOrder, typeof(XpsImage));
        }

        private
        void
        Flush()
        {
            //
            // If we are not interleaving there is nothing to do here
            // since each node will be flushed and closeed when committed.
            //
            if (_interleavingType != PackageInterleavingOrder.None)
            {
                //
                //Confirm the propert resources have been committed
                //
                ConfirmCommited();

                NodeComparer nodeComparer = new NodeComparer(_flushOrderItems);
                _interleavingNodes.Sort(nodeComparer);

                //
                // Flush the nodes in order
                //
                List<InterleavingNode> removeList = new List<InterleavingNode>();
                int pageCnt = 0;
                int documentCnt = 0;
                bool docSeqCommited = false;
                bool xpsDocCommited = false;
                foreach (InterleavingNode n in _interleavingNodes)
                {                    
                    if (n.Commited)
                    {
                        if( n.Node is XpsFixedPageReaderWriter )
                        {                            
                            pageCnt++;
                        }
                        else
                        if( n.Node is XpsFixedDocumentReaderWriter )
                        {
                            documentCnt++;
                        }
                        else
                        if( n.Node is XpsFixedDocumentSequenceReaderWriter )
                        {
                            docSeqCommited = true;
                        }
                        else
                        if( n.Node is XpsDocument )
                        {
                            xpsDocCommited = true;
                        }
                        n.Node.CommitInternal();
                        removeList.Add(n);
                    }
                    else
                    {
                        n.Node.Flush();
                    }
                }
                foreach (InterleavingNode n in removeList)
                {
                    _interleavingNodes.Remove(n);
                }
                if (PackagingProgressEvent != null )
                {
                    if( pageCnt != 0 )
                    {
                        PackagingProgressEvent( this, new PackagingProgressEventArgs( PackagingAction.FixedPageCompleted, pageCnt ) );
                    }
                    if( documentCnt != 0 )
                    {
                        PackagingProgressEvent( this, new PackagingProgressEventArgs( PackagingAction.FixedDocumentCompleted, documentCnt ) );
                    }
                    if( docSeqCommited )
                    {
                        PackagingProgressEvent( this, new PackagingProgressEventArgs( PackagingAction.DocumentSequenceCompleted, 1 ) );
                    }
                    if( xpsDocCommited )
                    {
                        PackagingProgressEvent( this, new PackagingProgressEventArgs( PackagingAction.XpsDocumentCommitted, 1 ) );
                    }
}
            }
        }


        private
        void 
        MarkNodeCommited(INode node )
        {
            foreach( InterleavingNode n in _interleavingNodes )
            {
                if( Object.ReferenceEquals(node, n.Node))
                {
                    n.Commited = true;
                }
            }
        }

        private
        void
        RemoveNode(INode node)
        {
            foreach (InterleavingNode n in _interleavingNodes)
            {
                if (Object.ReferenceEquals(node, n.Node))
                {
                    _interleavingNodes.Remove(n);
                    break;
                }
            }
        }

        //
        // When a flushing only the document,
        // the document sequence  and the XpsDocument could still not
        // be complete
        //
        private
        void
        ConfirmCommited()
        {
            foreach( InterleavingNode n in _interleavingNodes )
            {
                if(IsPartialFlushAllowed(n))
                {
                    continue;
                }
                if( !n.Commited )
                {
                    throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_DependantsNotCommitted));
                }
            }
        }

        /// <summary>
        /// Tests wether a part can be flushed with out being committed
        /// </summary>       
        private
        bool
        IsPartialFlushAllowed( InterleavingNode n )
        {
            bool ret = false;
            if(  n.Node is IXpsFixedDocumentWriter ||
                 n.Node is IXpsFixedDocumentSequenceWriter ||
                 n.Node is XpsDocument
                )
            {
                ret = true;
            }
            return ret;
        }
             
         #endregion Private methods

        #region Private data

        private Hashtable                   _flushOrderItems;
        private List<InterleavingNode>      _interleavingNodes = new List<InterleavingNode>();
        private bool                        _flushOnSubsetComplete;
        private PackageInterleavingOrder    _interleavingType;

        #endregion Private data
    }

    class NodeComparer : IComparer<InterleavingNode>
    {
        #region Constructors

        public
        NodeComparer(
            Hashtable       flushOrderTable
            )
        {
            _orderTable = flushOrderTable;
        }

        #endregion Constructors

        #region IComparer<InterleavingNode> Members

        int
        IComparer<InterleavingNode>.Compare(
            InterleavingNode       x,
            InterleavingNode       y
            )
        {
           FlushItem xOrder = (FlushItem)_orderTable[x.Node.GetType()];
           FlushItem yOrder = (FlushItem)_orderTable[y.Node.GetType()];
           return xOrder.FlushOrder.CompareTo(yOrder.FlushOrder);
        }


#endregion
        
        #region Object Members
        
        #endregion

        #region Private data

        private Hashtable _orderTable;

        #endregion Private data
    }

    internal interface INode
    {
        #region Public properties

        Uri Uri { get; }

        #endregion Public properties

        #region Public methods

        void
        Flush(
            );

        void
        CommitInternal(
            );

        PackagePart
        GetPart(
            );

        #endregion Public methods
    }

    /// <summary>
    /// Helper class to bind flush order with a specific type
    /// Used by the Node Comparer in ordering Nodes by type and Flush Order
    /// </summary>
    internal class FlushItem
    {
        #region Constructors

        internal
        FlushItem(
            FlushOrder      flushOrder,
            Type            classType
            )
        {
            _flushOrder = flushOrder;
            _classType = classType;
        }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets FlushOrder for this item.
        /// </summary>
        /// <value>
        /// A FlushOrder value.
        /// </value>
        internal FlushOrder FlushOrder
        {
            get
            {
                return _flushOrder;
            }
        }

        /// <summary>
        /// Gets the class Type of this item.
        /// </summary>
        /// <value>
        /// A Type value.
        /// </value>
        public Type ClassType
        {
            get
            {
                return _classType;
            }
        }

        #endregion Public properties

        #region Private data

        private FlushOrder  _flushOrder;
        private Type        _classType;

        #endregion Private data
    }

    internal
    class
    InterleavingNode
    {
        internal
        InterleavingNode(
            INode node,
            int   number,
            INode parent
        )
        {
            _node = node;
            _number = number;
            _parent = parent;
            _commited = false;
        }

        public
        bool 
        Commited
        {
            get
            {
                return _commited;
            }
            set
            {
                _commited = value;
            }
        }

        public
        INode 
        Node
        {
            get
            {
                return _node;
            }
        }

        public
        int 
        Number
        {
            get
            {
                return _number;
            }
        }

        public
        INode 
        Parent
        {
            get
            {
                return _parent;
            }
}
        private INode   _node;
        private int     _number;
        private INode   _parent;
        private bool    _commited;
}
    /// <summary>
    ///
    /// </summary>
    internal enum FlushOrder
    {
        /// <summary>
        ///
        /// </summary>
        None = 0,
        /// <summary>
        ///
        /// </summary>
        FirstOrder      = 1,
        /// <summary>
        ///
        /// </summary>
        SecondOrder     = 2,
        /// <summary>
        ///
        /// </summary>
        ThirdOrder      = 3,
        /// <summary>
        ///
        /// </summary>
        FourthOrder     = 4,
        /// <summary>
        ///
        /// </summary>
        FifthOrder      = 5,
        /// <summary>
        ///
        /// </summary>
        SixthOrder      = 6
    }
}
