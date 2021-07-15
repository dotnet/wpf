// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    ///     Container for the route to be followed 
    ///     by a RoutedEvent when raised
    /// </summary>
    /// <remarks>
    ///     EventRoute constitues <para/>
    ///     a non-null <see cref="RoutedEvent"/>
    ///     and <para/>
    ///     an ordered list of (target object, handler list)
    ///     pairs <para/>
    ///     <para/>
    ///
    ///     It facilitates adding new entries to this list
    ///     and also allows for the handlers in the list 
    ///     to be invoked
    /// </remarks>
    public sealed class EventRoute
    {
        #region Construction

        /// <summary>
        ///     Constructor for <see cref="EventRoute"/> given
        ///     the associated <see cref="RoutedEvent"/>
        /// </summary>
        /// <param name="routedEvent">
        ///     Non-null <see cref="RoutedEvent"/> to be associated with 
        ///     this <see cref="EventRoute"/>
        /// </param>
        public EventRoute(RoutedEvent routedEvent)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent"); 
            }
            
            _routedEvent = routedEvent;

            // Changed the initialization size to 16 
            // to achieve performance gain based 
            // on standard app behavior
            _routeItemList = new FrugalStructList<RouteItem>(16);
            _sourceItemList = new FrugalStructList<SourceItem>(16);
        }

        #endregion Construction

        #region External API

        /// <summary>
        ///     Adds this handler for the 
        ///     specified target to the route
        /// </summary>
        /// <remarks>
        ///     NOTE: It is not an error to add a 
        ///     handler for a particular target instance 
        ///     twice (handler will simply be called twice). 
        /// </remarks>
        /// <param name="target">
        ///     Target object whose handler is to be 
        ///     added to the route
        /// </param>
        /// <param name="handler">
        ///     Handler to be added to the route
        /// </param>
        /// <param name="handledEventsToo">
        ///     Flag indicating whether or not the listener wants to 
        ///     hear about events that have already been handled
        /// </param>
        public void Add(object target, Delegate handler, bool handledEventsToo)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target"); 
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler"); 
            }
            
            RouteItem routeItem = new RouteItem(target, new RoutedEventHandlerInfo(handler, handledEventsToo));

            _routeItemList.Add(routeItem);
        }

        /// <summary>
        ///     Invokes all the handlers that have been 
        ///     added to the route
        /// </summary>
        /// <remarks>
        ///     NOTE: If the <see cref="RoutingStrategy"/> 
        ///     of the associated <see cref="RoutedEvent"/> 
        ///     is <see cref="RoutingStrategy.Bubble"/>
        ///     the last handlers added are the 
        ///     last ones invoked <para/>
        ///     However if the <see cref="RoutingStrategy"/> 
        ///     of the associated <see cref="RoutedEvent"/> 
        ///     is <see cref="RoutingStrategy.Tunnel"/>, 
        ///     the last handlers added are the 
        ///     first ones invoked 
        /// </remarks>
        /// <param name="source">
        ///     <see cref="RoutedEventArgs.Source"/> 
        ///     that raised the RoutedEvent
        /// </param>
        /// <param name="args">
        ///     <see cref="RoutedEventArgs"/> that carry
        ///     all the details specific to this RoutedEvent
        /// </param>
        internal void InvokeHandlers(object source, RoutedEventArgs args)
        {
            InvokeHandlersImpl(source, args, false);
        }

        internal void ReInvokeHandlers(object source, RoutedEventArgs args)
        {
            InvokeHandlersImpl(source, args, true);
        }

        private void InvokeHandlersImpl(object source, RoutedEventArgs args, bool reRaised)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source"); 
            }

            if (args == null)
            {
                throw new ArgumentNullException("args"); 
            }

            if (args.Source == null)
            {
                throw new ArgumentException(SR.Get(SRID.SourceNotSet)); 
            }

            if (args.RoutedEvent != _routedEvent)
            {
                throw new ArgumentException(SR.Get(SRID.Mismatched_RoutedEvent));
            }

            // Check RoutingStrategy to know the order of invocation
            if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Bubble ||
                args.RoutedEvent.RoutingStrategy == RoutingStrategy.Direct)
            {
                int endSourceChangeIndex = 0;
                
                // If the RoutingStrategy of the associated is 
                // Bubble the handlers for the last target 
                // added are the last ones invoked
                // Invoke class listeners
                for (int i=0; i<_routeItemList.Count; i++)
                {
                    // Query for new source only if we are 
                    // past the range of the previous source change
                    if (i >= endSourceChangeIndex)
                    {
                        // Get the source at this point in the bubble route and also 
                        // the index at which this source change seizes to apply
                        object newSource = GetBubbleSource(i, out endSourceChangeIndex);
                        
                        // Set appropriate source
                        // The first call to setsource seems redundant 
                        // but is necessary because the source could have 
                        // been modified during BuildRoute call and hence 
                        // may need to be reset to the original source.
                        // Note: we skip this logic if reRaised is set, which is done when we're trying
                        //       to convert MouseDown/Up into a MouseLeft/RightButtonDown/Up
                        if(!reRaised)
                        {
                            if (newSource == null)
                                args.Source=source;
                            else
                                args.Source=newSource;
                        }
                    }
                    
                    // Invoke listeners

                    if( TraceRoutedEvent.IsEnabled )
                    {
                        TraceRoutedEvent.Trace(
                            TraceEventType.Start,
                            TraceRoutedEvent.InvokeHandlers,  
                            _routeItemList[i].Target,
                            args,
                            args.Handled );

                    }
                    
                    _routeItemList[i].InvokeHandler(args);

                    if( TraceRoutedEvent.IsEnabled )
                    {
                        TraceRoutedEvent.Trace(
                            TraceEventType.Stop,
                            TraceRoutedEvent.InvokeHandlers,  
                            _routeItemList[i].Target,
                            args,
                            args.Handled );
                    }


                }
            }
            else
            {
                int startSourceChangeIndex = _routeItemList.Count;
                int endTargetIndex =_routeItemList.Count-1;
                int startTargetIndex;
                
                // If the RoutingStrategy of the associated is 
                // Tunnel the handlers for the last target 
                // added are the first ones invoked
                while (endTargetIndex >= 0)
                {
                    // For tunnel events we need to invoke handlers for the last target first. 
                    // However the handlers for that individual target must be fired in the right order. 
                    // Eg. Class Handlers must be fired before Instance Handlers.
                    object currTarget = _routeItemList[endTargetIndex].Target;
                    for (startTargetIndex=endTargetIndex; startTargetIndex>=0; startTargetIndex--)
                    {
                        if (_routeItemList[startTargetIndex].Target != currTarget)
                        {
                            break;
                        }
                    }
                    
                    for (int i=startTargetIndex+1; i<=endTargetIndex; i++)
                    {
                        // Query for new source only if we are 
                        // past the range of the previous source change
                        if (i < startSourceChangeIndex)
                        {
                            // Get the source at this point in the tunnel route and also 
                            // the index at which this source change seizes to apply
                            object newSource = GetTunnelSource(i, out startSourceChangeIndex);
                            
                            // Set appropriate source
                            // The first call to setsource seems redundant 
                            // but is necessary because the source could have 
                            // been modified during BuildRoute call and hence 
                            // may need to be reset to the original source.
                            if (newSource == null)
                                args.Source=source;                            
                            else
                                args.Source=newSource;
                        }
                        
                        
                        if( TraceRoutedEvent.IsEnabled )
                        {
                            TraceRoutedEvent.Trace(
                                TraceEventType.Start,
                                TraceRoutedEvent.InvokeHandlers,  
                                _routeItemList[i].Target,
                                args,
                                args.Handled );
                        }

                        // Invoke listeners
                        _routeItemList[i].InvokeHandler(args);

                        if( TraceRoutedEvent.IsEnabled )
                        {
                            TraceRoutedEvent.Trace(
                                TraceEventType.Stop,
                                TraceRoutedEvent.InvokeHandlers,  
                                _routeItemList[i].Target,
                                args,
                                args.Handled );
                        }

                    }

                    endTargetIndex = startTargetIndex;
                }
            }            
        }

        /// <summary>
        ///     Pushes the given node at the top of the stack of branches.
        /// </summary>
        /// <remarks>
        ///     If a node in the tree has different visual and logical,
        ///     FrameworkElement will store the node on this stack of
        ///     branches.  If the route ever returns to the same logical
        ///     tree, the event source will be restored.
        ///     <para/>
        ///     NOTE: This method needs to be public because it is used
        ///     by FrameworkElement.
        /// </remarks>
        /// <param name="node">
        ///     The node where the visual parent is different from the logical
        ///     parent.
        /// </param>
        /// <param name="source">
        ///     The source that is currently being used, and which should be
        ///     restored when this branch is popped off the stack.
        /// </param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public void PushBranchNode(object node, object source)
        {
            BranchNode branchNode = new BranchNode();
            branchNode.Node = node;
            branchNode.Source = source;
            
            BranchNodeStack.Push(branchNode);
        }

        /// <summary>
        ///     Pops the given node from the top of the stack of branches.
        /// </summary>
        /// <remarks>
        ///     If a node in the tree has different visual and logical,
        ///     FrameworkElement will store the node on this stack of
        ///     branches.  If the route ever returns to the same logical
        ///     tree, the event source will be restored.
        ///     <para/>
        ///     NOTE: This method needs to be public because it is used
        ///     by FrameworkElement.
        /// </remarks>
        /// <returns>
        ///     The node where the visual parent was different from the
        ///     logical parent.
        /// </returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public object PopBranchNode()
        {
            if (BranchNodeStack.Count == 0)
                return null;
            
            BranchNode branchNode = BranchNodeStack.Pop();

            return branchNode.Node;
        }

        /// <summary>
        ///     Peeks the given node from the top of the stack of branches.
        /// </summary>
        /// <remarks>
        ///     If a node in the tree has different visual and logical,
        ///     FrameworkElement will store the node on this stack of
        ///     branches.  If the route ever returns to the same logical
        ///     tree, the event source will be restored.
        ///     <para/>
        ///     NOTE: This method needs to be public because it is used
        ///     by FrameworkElement.
        /// </remarks>
        /// <returns>
        ///     The node where the visual parent was different from the
        ///     logical parent.
        /// </returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public object PeekBranchNode()
        {
            if (BranchNodeStack.Count == 0)
                return null;

            BranchNode branchNode = BranchNodeStack.Peek();

            return branchNode.Node;
        }

        /// <summary>
        ///     Peeks the given source from the top of the stack of branches.
        /// </summary>
        /// <remarks>
        ///     If a node in the tree has different visual and logical,
        ///     FrameworkElement will store the node on this stack of
        ///     branches.  If the route ever returns to the same logical
        ///     tree, the event source will be restored.
        ///     <para/>
        ///     NOTE: This method needs to be public because it is used
        ///     by FrameworkElement.
        /// </remarks>
        /// <returns>
        ///     The source that was stored along with the node where the
        ///     visual parent was different from the logical parent.
        /// </returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public object PeekBranchSource()
        {
            if (BranchNodeStack.Count == 0)
                return null;

            BranchNode branchNode = BranchNodeStack.Peek();

            return branchNode.Source;
        }
        
        #endregion External API
        
        #region Operations

        // Return associated RoutedEvent
        internal RoutedEvent RoutedEvent
        {
            get {return _routedEvent;}
            set { _routedEvent = value; }
        }

        // A BranchNode indicates a point in the tree where the logical and
        // visual structure might diverge.  When building a route, we store
        // this branch node for every logical link we find.  Along with the
        // node where the possible divergence occurred, we store the source
        // that the event should use.  This is so that the source of an
        // event will always be in the logical tree of the element handling
        // the event.
        private struct BranchNode
        {
            public object Node;
            public object Source;
        }

        // Branch nodes are stored on a stack, which we create on-demand.
        private Stack<BranchNode> BranchNodeStack
        {
            get
            {
                if (_branchNodeStack == null)
                {
                    _branchNodeStack = new Stack<BranchNode>(1);
                }
                
                return _branchNodeStack;
            }
        }

        // Add the given source to the source item list
        // indicating what the source will be this point 
        // onwards in the route
        internal void AddSource(object source)
        {
            int startIndex = _routeItemList.Count;
            _sourceItemList.Add(new SourceItem(startIndex, source));
        }

        // Determine what the RoutedEventArgs.Source should be, at this
        // point in the bubble. Also the endIndex output parameter tells 
        // you the exact index of the handlersList at which this source 
        // change ceases to apply
        private object GetBubbleSource(int index, out int endIndex)
        {
            // If the Source never changes during the route execution,
            // then we're done (just return null).
            if (_sourceItemList.Count == 0)
            {
                endIndex = _routeItemList.Count;
                return null;
            }
            
            // Similarly, if we're not to the point of the route of the first Source
            // change, simply return null.
            if (index < _sourceItemList[0].StartIndex)
            {
                endIndex = _sourceItemList[0].StartIndex;
                return null;
            }
            
            // See if we should be using one of the intermediate
            // sources
            for (int i=0; i<_sourceItemList.Count -1; i++)
            {
                if (index >= _sourceItemList[i].StartIndex &&
                    index < _sourceItemList[i+1].StartIndex)
                {
                    endIndex = _sourceItemList[i+1].StartIndex;
                    return _sourceItemList[i].Source;
                }
            }

            // If we get here, we're on the last one,
            // so return that.            
            endIndex = _routeItemList.Count;
            return _sourceItemList[_sourceItemList.Count -1].Source;
        }

        // Determine what the RoutedEventArgs.Source should be, at this
        // point in the tunnel. Also the startIndex output parameter tells 
        // you the exact index of the handlersList at which this source 
        // change starts to apply
        private object GetTunnelSource(int index, out int startIndex)
        {
            // If the Source never changes during the route execution,
            // then we're done (just return null).
            if (_sourceItemList.Count == 0)
            {
                startIndex = 0;
                return null;
            }
            
            // Similarly, if we're past the point of the route of the first Source
            // change, simply return null.
            if (index < _sourceItemList[0].StartIndex)
            {
                startIndex = 0;
                return null;
            }
            
            // See if we should be using one of the intermediate
            // sources
            for (int i=0; i<_sourceItemList.Count -1; i++)
            {
                if (index >= _sourceItemList[i].StartIndex &&
                    index < _sourceItemList[i+1].StartIndex)
                {
                    startIndex = _sourceItemList[i].StartIndex;
                    return _sourceItemList[i].Source;
                }
            }
        
            // If we get here, we're on the last one, so return that.            
            startIndex = _sourceItemList[_sourceItemList.Count -1].StartIndex;
            return _sourceItemList[_sourceItemList.Count -1].Source;
        }

        /// <summary>
        ///     Cleanup all the references within the data
        /// </summary>
        internal void Clear()
        {
            _routedEvent = null;
            
            _routeItemList.Clear();

            if (_branchNodeStack != null)
            {
                _branchNodeStack.Clear();
            }

            _sourceItemList.Clear();
        }
        
        #endregion Operations
        
        #region Data

        private RoutedEvent _routedEvent;

        // Stores the routed event handlers to be 
        // invoked for the associated RoutedEvent
        private FrugalStructList<RouteItem> _routeItemList;

        // Stores the branching nodes in the route
        // that need to be backtracked while 
        // augmenting the route
        private Stack<BranchNode> _branchNodeStack;

        // Stores Source Items for separated trees
        private FrugalStructList<SourceItem> _sourceItemList;

        #endregion Data
    }
}

