// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Security;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPlugIns
{
    /// <summary>
    /// Collection of StylusPlugIn objects
    /// </summary>
    /// <remarks>
    /// The collection order is based on the order that StylusPlugIn objects are
    /// added to the collection via the IList interfaces. The order of the StylusPlugIn
    /// objects in the collection is modifiable.
    /// Some of the methods are designed to be called from both the App thread and the Pen thread,
    /// but some of them are supposed to be called from one thread only. Please look at the 
    /// comments of each method for such an information.
    /// </remarks>
    public sealed class StylusPlugInCollection : Collection<StylusPlugIn>
    {
        #region Protected APIs

        /// <summary>
        /// Insert a StylusPlugIn in the collection at a specific index. 
        /// This method should be called from the application context only
        /// </summary>
        /// <param name="index">index at which to insert the StylusPlugIn object</param>
        /// <param name="plugIn">StylusPlugIn object to insert, downcast to an object</param>
        protected override void InsertItem(int index, StylusPlugIn plugIn)
        {
            // Verify it's called from the app dispatcher
            _element.VerifyAccess();

            // Validate the input parameter
            if (null == plugIn)
            {
                throw new ArgumentNullException(nameof(plugIn), SR.Get(SRID.Stylus_PlugInIsNull));
            }

            if (IndexOf(plugIn) != -1)
            {
                throw new ArgumentException(SR.Get(SRID.Stylus_PlugInIsDuplicated), nameof(plugIn));
            }

            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy
            // which a call to Lock() can cause.
            ExecuteWithPotentialDispatcherDisable(() =>
            {
                if (_stylusPlugInCollectionImpl.IsActiveForInput)
                {
                    // If we are currently active for input then we have a _penContexts that we must lock!
                    ExecuteWithPotentialLock(() =>
                     {
                         System.Diagnostics.Debug.Assert(this.Count > 0); // If active must have more than one plugin already
                         base.InsertItem(index, plugIn);
                         plugIn.Added(this);
                     });
                }
                else
                {
                    EnsureEventsHooked(); // Hook up events to track changes to the plugin's element
                    base.InsertItem(index, plugIn);
                    try
                    {
                        plugIn.Added(this); // Notify plugin that it has been added to collection
                    }
                    finally
                    {
                        _stylusPlugInCollectionImpl.UpdateState(_element); // Add to PenContexts if element is in proper state (can fire isactiveforinput).
                    }
                }
            });
        }

        /// <summary>
        /// Remove all the StylusPlugIn objects from the collection.
        /// This method should be called from the application context only.
        /// </summary>
        protected override void ClearItems()
        {
            // Verify it's called from the app dispatcher
            _element.VerifyAccess();

            if (this.Count != 0)
            {
                // Disable processing of the queue during blocking operations to prevent unrelated reentrancy
                // which a call to Lock() can cause.
                ExecuteWithPotentialDispatcherDisable(() =>
                {
                    if (_stylusPlugInCollectionImpl.IsActiveForInput)
                    {
                        // If we are currently active for input then we have a _penContexts that we must lock!
                        ExecuteWithPotentialLock(() =>
                         {
                             while (this.Count > 0)
                             {
                                 RemoveItem(0);  // Does work to fire event and remove from collection and pencontexts
                             }
                         });
                    }
                    else
                    {
                        while (this.Count > 0)
                        {
                            RemoveItem(0);  // Does work to fire event and remove from collection.
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Remove the StylusPlugIn in the collection at the specified index.         
        /// This method should be called from the application context only.
        /// </summary>
        /// <param name="index"></param>
        protected override void RemoveItem(int index)
        {
            // Verify it's called from the app dispatcher
            _element.VerifyAccess();

            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy
            // which a call to Lock() can cause.
            ExecuteWithPotentialDispatcherDisable(() =>
            {
                if (_stylusPlugInCollectionImpl.IsActiveForInput)
                {
                    // If we are currently active for input then we have a _penContexts that we must lock!
                    ExecuteWithPotentialLock(() =>
                     {
                         StylusPlugIn removedItem = base[index];
                         base.RemoveItem(index);
                         try
                         {
                             EnsureEventsUnhooked(); // Clean up events and remove from pencontexts
                         }
                         finally
                         {
                             removedItem.Removed(); // Notify plugin it has been removed
                         }
                     });
                }
                else
                {
                    StylusPlugIn removedItem = base[index];
                    base.RemoveItem(index);
                    try
                    {
                        EnsureEventsUnhooked(); // Clean up events and remove from pencontexts
                    }
                    finally
                    {
                        removedItem.Removed(); // Notify plugin it has been removed
                    }
                }
            });
        }

        /// <summary>
        /// Indexer to retrieve/set a StylusPlugIn at a given index in the collection
        /// Accessible from both the real time context and application context.
        /// </summary>
        protected override void SetItem(int index, StylusPlugIn plugIn)
        {
            // Verify it's called from the app dispatcher
            _element.VerifyAccess();

            if (null == plugIn)
            {
                throw new ArgumentNullException("plugIn", SR.Get(SRID.Stylus_PlugInIsNull));
            }

            if (IndexOf(plugIn) != -1)
            {
                throw new ArgumentException(SR.Get(SRID.Stylus_PlugInIsDuplicated), "plugIn");
            }

            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy
            // which a call to Lock() can cause.
            ExecuteWithPotentialDispatcherDisable(() =>
            {
                if (_stylusPlugInCollectionImpl.IsActiveForInput)
                {
                    // If we are currently active for input then we have a _penContexts that we must lock!
                    ExecuteWithPotentialLock(() =>
                     {
                         StylusPlugIn originalPlugIn = base[index];
                         base.SetItem(index, plugIn);
                         try
                         {
                             originalPlugIn.Removed();
                         }
                         finally
                         {
                             plugIn.Added(this);
                         }
                     });
                }
                else
                {
                    StylusPlugIn originalPlugIn = base[index];
                    base.SetItem(index, plugIn);
                    try
                    {
                        originalPlugIn.Removed();
                    }
                    finally
                    {
                        plugIn.Added(this);
                    }
                }
            });
        }

        #endregion

        #region Internal APIs

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="element"></param>
        internal StylusPlugInCollection(UIElement element)
        {
            _stylusPlugInCollectionImpl = StylusPlugInCollectionBase.Create(this);

            _element = element;

            _isEnabledChangedEventHandler = new DependencyPropertyChangedEventHandler(OnIsEnabledChanged);
            _isVisibleChangedEventHandler = new DependencyPropertyChangedEventHandler(OnIsVisibleChanged);
            _isHitTestVisibleChangedEventHandler = new DependencyPropertyChangedEventHandler(OnIsHitTestVisibleChanged);
            _sourceChangedEventHandler = new SourceChangedEventHandler(OnSourceChanged);
            _layoutChangedEventHandler = new EventHandler(OnLayoutUpdated);
        }

        /// <summary>
        /// Get the UIElement
        /// This method is called from the real-time context.
        /// </summary>
        internal UIElement Element
        {
            get
            {
                return _element;
            }
        }

        /// <summary>
        /// Update the rectangular bound of the element
        /// This method is called from the application context.
        /// </summary>
        internal void UpdateRect()
        {
            // The RenderSize is only valid if IsArrangeValid is true.
            if (_element.IsArrangeValid && _element.IsEnabled && _element.IsVisible && _element.IsHitTestVisible)
            {
                _rc = new Rect(new Point(), _element.RenderSize);// _element.GetContentBoundingBox();
                Visual root = VisualTreeHelper.GetContainingVisual2D(InputElement.GetRootVisual(_element));

                try
                {
                    _viewToElement = root.TransformToDescendant(_element);
                }
                catch (System.InvalidOperationException)
                {
                    // This gets hit if the transform is not invertable.  In that case
                    // we will just not allow this plugin to be hit.
                    _rc = new Rect(); // empty rect so we don't hittest it.
                    _viewToElement = Transform.Identity;
                }
            }
            else
            {
                _rc = new Rect(); // empty rect so we don't hittest it.
            }

            if (_viewToElement == null)
            {
                _viewToElement = Transform.Identity;
            }
        }

        /// <summary>
        /// Check whether a point hits the element
        /// This method is called from the real-time context.
        /// </summary>
        /// <param name="pt">a point to check</param>
        /// <returns>true if the point is within the bound of the element; false otherwise</returns>
        internal bool IsHit(Point pt)
        {
            Point ptElement = pt;
            _viewToElement.TryTransform(ptElement, out ptElement);
            return _rc.Contains(ptElement);
        }

        /// <summary>
        /// Get the transform matrix from the root visual to the current UIElement
        /// This method is called from the real-time context.
        /// </summary>
        internal GeneralTransform ViewToElement
        {
            get
            {
                return _viewToElement;
            }
        }

        /// <summary>
        /// Get the current rect for the Element that the StylusPlugInCollection is attached to.
        /// May be empty rect if plug in is not in tree.
        /// </summary>
        internal Rect Rect
        {
            get
            {
                return _rc;
            }
        }

        /// <summary>
        /// Fire the Enter notification.
        /// This method is called from pen threads and app thread.
        /// </summary>
        internal void FireEnterLeave(bool isEnter, RawStylusInput rawStylusInput, bool confirmed)
        {
            if (_stylusPlugInCollectionImpl.IsActiveForInput)
            {
                // If we are currently active for input then we have a _penContexts that we must lock!
                ExecuteWithPotentialLock(() =>
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        base[i].StylusEnterLeave(isEnter, rawStylusInput, confirmed);
                    }
                });
            }
            else
            {
                for (int i = 0; i < this.Count; i++)
                {
                    base[i].StylusEnterLeave(isEnter, rawStylusInput, confirmed);
                }
            }
        }

        /// <summary>
        /// Fire RawStylusInputEvent for all the StylusPlugIns
        /// This method is called from the real-time context (pen thread) only
        /// </summary>
        /// <param name="args"></param>
        internal void FireRawStylusInput(RawStylusInput args)
        {
            try
            {
                if (_stylusPlugInCollectionImpl.IsActiveForInput)
                {
                    // If we are currently active for input then we have a _penContexts that we must lock!
                    ExecuteWithPotentialLock(() =>
                    {
                        for (int i = 0; i < this.Count; i++)
                        {
                            StylusPlugIn plugIn = base[i];
                            // set current plugin so any callback data gets an owner.
                            args.CurrentNotifyPlugIn = plugIn;
                            plugIn.RawStylusInput(args);
                        }
                    });
                }
                else
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        StylusPlugIn plugIn = base[i];
                        // set current plugin so any callback data gets an owner.
                        args.CurrentNotifyPlugIn = plugIn;
                        plugIn.RawStylusInput(args);
                    }
                }
            }
            finally
            {
                args.CurrentNotifyPlugIn = null;
            }
        }

        internal bool IsActiveForInput
        {
            get { return _stylusPlugInCollectionImpl.IsActiveForInput; }
        }

        internal object SyncRoot
        {
            get { return _stylusPlugInCollectionImpl.SyncRoot; }
        }

        internal void OnLayoutUpdated(object sender, EventArgs e)
        {
            // Make sure our rect and transform is up to date on layout changes.

            // NOTE: We need to make sure we do this under a lock if we are active for input since we don't
            // want the PenContexts code to get a mismatched set of state for this element.
            if (_stylusPlugInCollectionImpl.IsActiveForInput)
            {
                // Disable processing of the queue during blocking operations to prevent unrelated reentrancy
                // which a call to Lock() can cause.
                ExecuteWithPotentialDispatcherDisable(() =>
                {
                    // If we are currently active for input then we have a _penContexts that we must lock!
                    ExecuteWithPotentialLock(() =>
                     {
                         UpdateRect();
                     });
                });
            }
            else
            {
                UpdateRect();
            }

            if (_lastRenderTransform != _element.RenderTransform)
            {
                if (_renderTransformChangedEventHandler != null)
                {
                    _lastRenderTransform.Changed -= _renderTransformChangedEventHandler;
                    _renderTransformChangedEventHandler = null;
                }

                _lastRenderTransform = _element.RenderTransform;
            }

            if (_lastRenderTransform != null)
            {
                if (_lastRenderTransform.IsFrozen)
                {
                    if (_renderTransformChangedEventHandler != null)
                    {
                        _renderTransformChangedEventHandler = null;
                    }
                }
                else
                {
                    if (_renderTransformChangedEventHandler == null)
                    {
                        _renderTransformChangedEventHandler = new EventHandler(OnRenderTransformChanged);
                        _lastRenderTransform.Changed += _renderTransformChangedEventHandler;
                    }
                }
            }
        }

        /// <summary>
        ///
        /// Due to refactoring of the touch stack, this lock may not be needed (in the WM_POINTER stack).
        /// Therefore we introduce an optional wrapper that can take the lock based on the implementation
        /// defined in the private inheritance hierarchy (since this public facing class is sealed).
        /// </summary>
        /// <param name="action">The action to potentially lock</param>
        internal void ExecuteWithPotentialLock(Action action)
        {
            // Only lock here if needed.
            // Our impl has provided a SyncRoot which indicates a lock being used.
            if (_stylusPlugInCollectionImpl.SyncRoot != null)
            {
                lock (_stylusPlugInCollectionImpl.SyncRoot)
                {
                    action.Invoke();
                }
            }
            else
            {
                action.Invoke();
            }
        }

        /// <summary>
        ///
        /// Due to refactoring of the touch stack, this disable may not be needed (in the WM_POINTER stack).
        /// Therefore we introduce an optional wrapper that can disable based on the implementation
        /// defined in the private inheritance hierarchy (since this public facing class is sealed).
        /// </summary>
        /// <param name="action">The action to potentially lock</param>
        internal void ExecuteWithPotentialDispatcherDisable(Action action)
        {
            // Only disable the dispatcher here if needed .
            // Our impl has provided a SyncRoot which indicates a lock being used.
            if (_stylusPlugInCollectionImpl.SyncRoot != null)
            {
                using (_element.Dispatcher.DisableProcessing())
                {
                    action.Invoke();
                }
            }
            else
            {
                action.Invoke();
            }
        }

        #endregion

        #region Private APIs

        /// <summary>
        /// Add this StylusPlugInCollection to the StylusPlugInCollectionList when it the first 
        /// element is added.
        /// </summary>
        private void EnsureEventsHooked()
        {
            if (this.Count == 0)
            {
                // Grab current element info
                UpdateRect();
                // Now hook up events to track on this element.
                _element.IsEnabledChanged += _isEnabledChangedEventHandler;
                _element.IsVisibleChanged += _isVisibleChangedEventHandler;
                _element.IsHitTestVisibleChanged += _isHitTestVisibleChangedEventHandler;
                PresentationSource.AddSourceChangedHandler(_element, _sourceChangedEventHandler);  // has a security linkdemand
                _element.LayoutUpdated += _layoutChangedEventHandler;

                if (_element.RenderTransform != null &&
                    !_element.RenderTransform.IsFrozen)
                {
                    if (_renderTransformChangedEventHandler == null)
                    {
                        _renderTransformChangedEventHandler = new EventHandler(OnRenderTransformChanged);
                        _element.RenderTransform.Changed += _renderTransformChangedEventHandler;
                    }
                }
            }
        }

        /// <summary>
        /// Remove this StylusPlugInCollection from the StylusPlugInCollectionList when it the last 
        /// element is removed for this collection.
        /// </summary>
        private void EnsureEventsUnhooked()
        {
            if (this.Count == 0)
            {
                // Unhook events.
                _element.IsEnabledChanged -= _isEnabledChangedEventHandler;
                _element.IsVisibleChanged -= _isVisibleChangedEventHandler;
                _element.IsHitTestVisibleChanged -= _isHitTestVisibleChangedEventHandler;
                if (_renderTransformChangedEventHandler != null)
                {
                    _element.RenderTransform.Changed -= _renderTransformChangedEventHandler;
                }
                PresentationSource.RemoveSourceChangedHandler(_element, _sourceChangedEventHandler);
                _element.LayoutUpdated -= _layoutChangedEventHandler;

                // Disable processing of the queue during blocking operations to prevent unrelated reentrancy
                // which a call to Lock() can cause.
                ExecuteWithPotentialDispatcherDisable(() =>
                {
                    // Make sure we are unhooked from PenContexts if we don't have any plugins.
                    _stylusPlugInCollectionImpl.Unhook();
                });
            }
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(_element.IsEnabled == (bool)e.NewValue);
            _stylusPlugInCollectionImpl.UpdateState(_element);
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(_element.IsVisible == (bool)e.NewValue);
            _stylusPlugInCollectionImpl.UpdateState(_element);
        }

        private void OnIsHitTestVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(_element.IsHitTestVisible == (bool)e.NewValue);
            _stylusPlugInCollectionImpl.UpdateState(_element);
        }

        private void OnRenderTransformChanged(object sender, EventArgs e)
        {
            OnLayoutUpdated(sender, e);
        }

        private void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            // This means that the element has been added or remvoed from its source.
            _stylusPlugInCollectionImpl.UpdateState(_element);
        }

        #endregion

        #region Fields

        private StylusPlugInCollectionBase _stylusPlugInCollectionImpl;

        private UIElement _element;
        private Rect _rc; // In window root measured units
        private GeneralTransform _viewToElement;

        private Transform _lastRenderTransform;

        private DependencyPropertyChangedEventHandler _isEnabledChangedEventHandler;
        private DependencyPropertyChangedEventHandler _isVisibleChangedEventHandler;
        private DependencyPropertyChangedEventHandler _isHitTestVisibleChangedEventHandler;
        private EventHandler _renderTransformChangedEventHandler;
        private SourceChangedEventHandler _sourceChangedEventHandler;
        private EventHandler _layoutChangedEventHandler;

        #endregion
    }
}
