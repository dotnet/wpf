// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Security;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using System.Windows.Input;
using MS.Win32;
using MS.Utility;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    ///     PresentationSource is the abstract base for classes that
    ///     present content in another technology.  In addition, this
    ///     class provides static methods for working with these sources.
    /// </summary>
    /// <remarks>
    ///     We currently have one implementation - HwndSource - that
    ///     presents content in a Win32 HWND.
    /// </remarks>
    public abstract class PresentationSource : DispatcherObject
    {
        //------------------------------------------------------
        //
        // Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        ///     Constructs an instance of the PresentationSource object.
        /// </summary>
        /// <remarks>
        ///     This is protected since this is an abstract base class.
        /// </remarks>
        protected PresentationSource()
        {
        }

        static PresentationSource()
        {
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------        

        #region Public Methods
        /// <summary>
        ///     InputProvider given the Device type.
        /// </summary>
        internal virtual IInputProvider GetInputProvider(Type inputDevice) 
        { 
            return null; 
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Static Methods
        //
        //------------------------------------------------------  

        #region Public Static Methods
        /// <summary>
        ///     Returns the source in which the visual is being presented.
        /// </summary>
        /// <param name="visual">The visual to find the source for.</param>
        /// <returns>The source in which the visual is being presented.</returns>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks> 
        public static PresentationSource FromVisual(Visual visual)
        {

            return CriticalFromVisual(visual);
        }

        /// <summary>
        ///     Returns the source in which the Visual or Visual3D is being presented.
        /// </summary>
        /// <param name="dependencyObject">The dependency object to find the source for.</param>
        /// <returns>The source in which the dependency object is being presented.</returns>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks> 
        public static PresentationSource FromDependencyObject(DependencyObject dependencyObject)
        {

            return CriticalFromVisual(dependencyObject);
        }

        /// <summary>
        ///     Adds a handler for the SourceChanged event to the element.
        /// </summary>
        /// <param name="element">The element to add the handler too.</param>
        /// <param name="handler">The hander to add.</param>
        /// <remarks>
        ///     Even though this is a routed event handler, there are special
        ///     restrictions placed on this event.
        ///     1) You cannot use the UIElement or ContentElement AddHandler() method.
        ///     2) Class handlers are not allowed.
        ///     3) The handlers will receive the SourceChanged event even if it was handled.
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public static void AddSourceChangedHandler(IInputElement element, SourceChangedEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            // Either UIElement or ContentElement
            if (!InputElement.IsValid(element))
            {
                throw new ArgumentException(SR.Get(SRID.Invalid_IInputElement), "element");
            }
            DependencyObject o = (DependencyObject)element;

            //             o.VerifyAccess();


            // I would rather throw an exception here, but the CLR doesn't
            // so we won't either.
            if (handler != null)
            {
                FrugalObjectList<RoutedEventHandlerInfo> info;

                if (InputElement.IsUIElement(o))
                {
                    UIElement uie = o as UIElement;
                    uie.AddHandler(SourceChangedEvent, handler);
                    info = uie.EventHandlersStore[SourceChangedEvent];
                    if (1 == info.Count)
                    {
                        uie.VisualAncestorChanged += new Visual.AncestorChangedEventHandler(uie.OnVisualAncestorChanged);
                        AddElementToWatchList(uie);
                    }
                }
                else if (InputElement.IsUIElement3D(o))
                {
                    UIElement3D uie3D = o as UIElement3D;
                    uie3D.AddHandler(SourceChangedEvent, handler);
                    info = uie3D.EventHandlersStore[SourceChangedEvent];
                    if (1 == info.Count)
                    {
                        uie3D.VisualAncestorChanged += new Visual.AncestorChangedEventHandler(uie3D.OnVisualAncestorChanged);
                        AddElementToWatchList(uie3D);
                    }
                }
                else
                {
                    ContentElement ce = o as ContentElement;
                    ce.AddHandler(SourceChangedEvent, handler);
                    info = ce.EventHandlersStore[SourceChangedEvent];
                    if (1 == info.Count)
                        AddElementToWatchList(ce);
                }
            }
        }

        /// <summary>
        ///     Removes a handler for the SourceChanged event to the element.
        /// </summary>
        /// <param name="e">The element to remove the handler from.</param>
        /// <param name="handler">The hander to remove.</param>
        /// <remarks>
        ///     Even though this is a routed event handler, there are special
        ///     restrictions placed on this event.
        ///     1) You cannot use the UIElement or ContentElement RemoveHandler() method.
        /// </remarks>
        public static void RemoveSourceChangedHandler(IInputElement e, SourceChangedEventHandler handler)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (!InputElement.IsValid(e))
            {
                throw new ArgumentException(SR.Get(SRID.Invalid_IInputElement), "e");
            }
            DependencyObject o = (DependencyObject)e;

            //             o.VerifyAccess();

            // I would rather throw an exception here, but the CLR doesn't
            // so we won't either.
            if (handler != null)
            {
                FrugalObjectList<RoutedEventHandlerInfo> info = null;
                EventHandlersStore store;

                // Either UIElement or ContentElement.
                if (InputElement.IsUIElement(o))
                {
                    UIElement uie = o as UIElement;
                    uie.RemoveHandler(SourceChangedEvent, handler);
                    store = uie.EventHandlersStore;
                    if (store != null)
                    {
                        info = store[SourceChangedEvent];
                    }
                    if (info == null || info.Count == 0)
                    {
                        uie.VisualAncestorChanged -= new Visual.AncestorChangedEventHandler(uie.OnVisualAncestorChanged); ;
                        RemoveElementFromWatchList(uie);
                    }
                }
                else if (InputElement.IsUIElement3D(o))
                {
                    UIElement3D uie3D = o as UIElement3D;
                    uie3D.RemoveHandler(SourceChangedEvent, handler);
                    store = uie3D.EventHandlersStore;
                    if (store != null)
                    {
                        info = store[SourceChangedEvent];
                    }
                    if (info == null || info.Count == 0)
                    {
                        uie3D.VisualAncestorChanged -= new Visual.AncestorChangedEventHandler(uie3D.OnVisualAncestorChanged); ;
                        RemoveElementFromWatchList(uie3D);
                    }
                }
                else
                {
                    ContentElement ce = o as ContentElement;
                    ce.RemoveHandler(SourceChangedEvent, handler);
                    store = ce.EventHandlersStore;
                    if (store != null)
                    {
                        info = store[SourceChangedEvent];
                    }
                    if (info == null || info.Count == 0)
                    {
                        RemoveElementFromWatchList(ce);
                    }
                }
            }
        }

        /// <summary>
        ///     Called by FrameworkElements when a framework ancenstor link of
        ///     ContentElement has changed.
        /// </summary>
        /// <param name="ce">
        ///     The element whose ancestory may have changed.
        /// </param>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static void OnAncestorChanged(ContentElement ce)
        {
            if (ce == null)
            {
                throw new ArgumentNullException("ce");
            }


            if (true == (bool)ce.GetValue(GetsSourceChangedEventProperty))
            {
                UpdateSourceOfElement(ce, null, null);
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties
        /// <summary>
        ///     The visual target for the visuals being presented in the source.
        /// </summary>
        public CompositionTarget CompositionTarget
        {
            get
            {
                return GetCompositionTargetCore();
            }
        }

        /// <summary>
        ///     The root visual being presented in the source.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public abstract Visual RootVisual
        {
            get;
            set;
        }

        /// <summary>
        ///     Causes this PresentationSource to enter "menu mode".
        /// </summary>
        internal void PushMenuMode()
        {
            _menuModeCount += 1;
            if(1 == _menuModeCount)
            {
                OnEnterMenuMode();
            }
        }

        /// <summary>
        ///     Causes this PresentationSource to enter "menu mode".
        /// </summary>
        internal void PopMenuMode()
        {
            if(_menuModeCount <= 0)
            {
                throw new InvalidOperationException();
            }
            
            _menuModeCount -= 1;
            if(0 == _menuModeCount)
            {
                OnLeaveMenuMode();
            }
        }

        /// <summary>
        ///     Notification to derived classes to enter menu mode.
        /// </summary>
        internal virtual void OnEnterMenuMode()
        {
        }

        /// <summary>
        ///     Notification to derived classes to leave menu mode.
        /// </summary>
        internal virtual void OnLeaveMenuMode()
        {
        }
        
        private int _menuModeCount;

        /// <summary>
        ///     Whether or not the object is disposed.
        /// </summary>
        public abstract bool IsDisposed
        {
            get;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Static Properties
        //
        //------------------------------------------------------

        #region Public Static Properties
        /// <summary>
        ///   Return a WeakReferenceList which supports returning an Enumerator
        ///   over a ReadOnly SnapShot of the List of sources.  The Enumerator
        ///   skips over the any dead weak references in the list.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public static IEnumerable CurrentSources
        {
            get
            {
                return CriticalCurrentSources;
            }
        }


        #endregion

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events
        /// <summary>
        ///     This event fires when content is rendered and ready for user interaction.
        /// </summary>
        public event EventHandler ContentRendered;

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods
        /// <summary>
        ///     Returns visual target for the given source. Implemented by
        ///     the derived class.
        /// </summary>
        protected abstract CompositionTarget GetCompositionTargetCore();

        
        /// <summary>
        ///     Called by derived classes to indicate that the root visual has changed.
        /// </summary>
        /// <remarks>
        /// should we store the root visual for them?
        /// </remarks>
        /// <param name="oldRoot">The old root visual.</param>
        /// <param name="newRoot">The new root visual.</param>
        protected void RootChanged(Visual oldRoot, Visual newRoot)
        {
            PresentationSource oldSource = null;

            if (oldRoot == newRoot)
            {
                return;
            }

            // Always clear the RootSourceProperty on the old root.
            if (oldRoot != null)
            {
                oldSource = CriticalGetPresentationSourceFromElement(oldRoot, RootSourceProperty);
                oldRoot.ClearValue(RootSourceProperty);
            }
            
            // Always set the SourceProperty on the new root.
            if (newRoot != null)
            {
                newRoot.SetValue(RootSourceProperty, new SecurityCriticalDataForMultipleGetAndSet<PresentationSource>(this));
            }

            UIElement oldRootUIElement = oldRoot as UIElement;
            UIElement newRootUIElement = newRoot as UIElement;

            // The IsVisible property can only be true if root visual is connected to a presentation source.
            // For Read-Only force-inherited properties, use a private update method.
            if(oldRootUIElement != null)
            {
                oldRootUIElement.UpdateIsVisibleCache();
            }
            if(newRootUIElement != null)
            {
                newRootUIElement.UpdateIsVisibleCache();
            }

            // Broadcast the Unloaded event starting at the old root visual
            if (oldRootUIElement != null)
            {
                oldRootUIElement.OnPresentationSourceChanged(false);
            }

            // Broadcast the Loaded event starting at the root visual
            if (newRootUIElement != null)
            {
                newRootUIElement.OnPresentationSourceChanged(true);
            }

            // To fire PresentationSourceChanged when the RootVisual changes;
            // rather than simulate a "parent" pointer change, we just walk the
            // collection of all nodes that need the event.
            foreach (DependencyObject element in _watchers)
            {
                // We only need to update those elements that are in the
                // same context as this presentation source.
                if (element.Dispatcher == Dispatcher)
                {
                    PresentationSource testSource = CriticalGetPresentationSourceFromElement(element,CachedSourceProperty);
                    // 1) If we are removing the rootvisual, then fire on any node whos old
                    // PresetationSource was the oldSource.
                    // 2) If we are attaching a rootvisual then fire on any node whos old
                    // PresentationSource is null;
                    if (oldSource == testSource || null == testSource)
                    {
                        UpdateSourceOfElement(element, null, null);
                    }
                }
            }
        }

            

        /// <summary>
        ///     Called by derived classes to indicate that they need to be tracked.
        /// </summary>
        protected void AddSource()
        {
            _sources.Add(this);
        }

        /// <summary>
        ///     Called by derived classes to indicate that they no longer need to be tracked.
        /// </summary>
        protected void RemoveSource()
        {
            _sources.Remove(this);
        }

        /// <summary>
        ///     Sets the ContentRendered event to null.
        /// </summary>
        protected void ClearContentRenderedListeners()
        {
            ContentRendered = null;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Static Methods
        //
        //------------------------------------------------------

        #region Internal Static Methods
        /// <summary>
        ///     Called by UIElement(3D)s when a visual ancestor link has changed.
        /// </summary>
        /// <param name="uie">The UIElement whose ancestory may have changed.</param>
        /// <param name="e">  Event Args.</param>
        internal static void OnVisualAncestorChanged(DependencyObject uie, AncestorChangedEventArgs e)
        {
            Debug.Assert(InputElement.IsUIElement3D(uie) || InputElement.IsUIElement(uie));
            
            if (true == (bool)uie.GetValue(GetsSourceChangedEventProperty))
            {
                UpdateSourceOfElement(uie, e.Ancestor, e.OldParent);
            }
        }

        [FriendAccessAllowed] // To allow internal code paths to access this function 
        internal static PresentationSource CriticalFromVisual(DependencyObject v)
        {
            return CriticalFromVisual(v, true /* enable2DTo3DTransition */);
        }

        /// <param name="v">The dependency object to find the source for</param>
        /// <param name="enable2DTo3DTransition">
        ///     Determines whether when walking the tree to enable transitioning from a 2D child
        ///     to a 3D parent or to stop once a 3D parent is encountered.
        /// </param>
        [FriendAccessAllowed] // To allow internal code paths to access this function 
        internal static PresentationSource CriticalFromVisual(DependencyObject v, bool enable2DTo3DTransition)
        {
            if (v == null)
            {
                throw new ArgumentNullException("v");
            }

            PresentationSource source = FindSource(v, enable2DTo3DTransition);

            // Don't hand out a disposed source.
            if (null != source && source.IsDisposed)
            {
                source = null;
            }

            return source;
        }

        /// <summary>
        ///     Fire the event when content is rendered and ready for user interaction.
        /// </summary>
        /// <param name="arg"></param>
        internal static object FireContentRendered(object arg)
        {
            PresentationSource ps = (PresentationSource)arg;
            if (ps.ContentRendered != null)
            {
                ps.ContentRendered(arg, EventArgs.Empty);
            }
            return null;
        }

        /// <summary>
        ///     Helper method which returns true when all the given visuals 
        ///     are in the same presentation source.
        /// </summary>
        [FriendAccessAllowed] // To allow internal code paths to access this function 
        internal static bool UnderSamePresentationSource(params DependencyObject[] visuals)
        {
            if (visuals == null || visuals.Length == 0)
            {
                return true;
            }

            PresentationSource baseSource = CriticalFromVisual(visuals[0]);

            int count = visuals.Length;
            for (int i = 1; i < count; i++)
            {
                PresentationSource currentSource = CriticalFromVisual(visuals[i]);
                if (currentSource != baseSource)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Private Static Methods
        //
        //------------------------------------------------------  

        #region Private Static Methods
        /// <summary>
        ///   Return a WeakReferenceList which supports returning an Enumerator
        ///   over a ReadOnly SnapShot of the List of sources.  The Enumerator
        ///   skips over the any dead weak references in the list.
        /// </summary>
        internal static IEnumerable CriticalCurrentSources
        {
            get
            {
                return (IEnumerable)_sources;
            }
        }

        private static PresentationSource CriticalGetPresentationSourceFromElement(DependencyObject dObject,DependencyProperty dp)
        {
            PresentationSource testSource;
            SecurityCriticalDataForMultipleGetAndSet<PresentationSource> tempCriticalDataWrapper =
                (SecurityCriticalDataForMultipleGetAndSet < PresentationSource > )
                dObject.GetValue(dp);
            if (tempCriticalDataWrapper == null || tempCriticalDataWrapper.Value == null)
            {
                testSource = null;
            }
            else
            {   
                testSource = tempCriticalDataWrapper.Value;
            }
            return testSource;
        }

        private static void AddElementToWatchList(DependencyObject element)
        {
            if(_watchers.Add(element))
            {
                element.SetValue(CachedSourceProperty,new 
                    SecurityCriticalDataForMultipleGetAndSet<PresentationSource>(PresentationSource.FindSource(element)));
                element.SetValue(GetsSourceChangedEventProperty, true);
            }
        }


        private static void RemoveElementFromWatchList(DependencyObject element)
        {
            if(_watchers.Remove(element))
            {
                element.ClearValue(CachedSourceProperty);
                element.ClearValue(GetsSourceChangedEventProperty);
            }
        }

        private static PresentationSource FindSource(DependencyObject o)
        {
            return FindSource(o, true /* enable2DTo3DTransition */);
        }

        /// <param name="o">The dependency object to find the source for</param>
        /// <param name="enable2DTo3DTransition">
        ///     Determines whether when walking the tree to enable transitioning from a 2D child
        ///     to a 3D parent or to stop once a 3D parent is encountered.
        /// </param>
        private static PresentationSource FindSource(DependencyObject o, bool enable2DTo3DTransition)
        {
            PresentationSource source = null;

            // For "Visual" nodes GetRootVisual() climbs to the top of the
            //       visual tree (parent==null)
            // For "ContentElements" it climbs the logical parent until it
            // reaches a visual then climbs to the top of the visual tree.
            DependencyObject v = InputElement.GetRootVisual(o, enable2DTo3DTransition);
            if (v != null)
            {
               source = CriticalGetPresentationSourceFromElement(v, RootSourceProperty);
            }
            return source;
        }

        private static bool UpdateSourceOfElement(DependencyObject doTarget,
                                                  DependencyObject doAncestor,
                                                  DependencyObject doOldParent)
        {
            bool calledOut = false;
            
            PresentationSource realSource = FindSource(doTarget);
            PresentationSource cachedSource = CriticalGetPresentationSourceFromElement(doTarget, CachedSourceProperty);
            if (cachedSource != realSource)
            {
                doTarget.SetValue(CachedSourceProperty, new SecurityCriticalDataForMultipleGetAndSet<PresentationSource>(realSource));

                SourceChangedEventArgs args = new SourceChangedEventArgs(cachedSource, realSource);

                args.RoutedEvent=SourceChangedEvent;
                if (InputElement.IsUIElement(doTarget))
                {
                    ((UIElement)doTarget).RaiseEvent(args);
                }                
                else if (InputElement.IsContentElement(doTarget))
                {
                    ((ContentElement)doTarget).RaiseEvent(args);
                }
                else
                {
                    ((UIElement3D)doTarget).RaiseEvent(args);
                }

                calledOut = true;
            }

            return calledOut;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Static Members
        //
        //------------------------------------------------------  

        #region Private Static Members
        // We use a private DP for the RootSource (the connection from the root
        // element in a tree to the source it is displayed in).  Use the public
        // API FromVisual to get the source that a visual is displayed in.
        private static readonly DependencyProperty RootSourceProperty   
            = DependencyProperty.RegisterAttached("RootSource", typeof(SecurityCriticalDataForMultipleGetAndSet<PresentationSource>), typeof(PresentationSource),
                                          new PropertyMetadata((SecurityCriticalDataForMultipleGetAndSet<PresentationSource>)null));

        // We use a private DP for the CachedSource (stored on the elements 
        // that we are watching, so that we can send a change notification).
        // Use the public API FromVisual to get the source that a visual is
        // displayed in.
        private static readonly DependencyProperty CachedSourceProperty
            = DependencyProperty.RegisterAttached("CachedSource", typeof(SecurityCriticalDataForMultipleGetAndSet<PresentationSource>), typeof(PresentationSource),
                                          new PropertyMetadata((SecurityCriticalDataForMultipleGetAndSet<PresentationSource>)null));

        // We use a private DP to mark elements that we are watchin.
        private static readonly DependencyProperty GetsSourceChangedEventProperty = DependencyProperty.RegisterAttached("IsBeingWatched", typeof(bool), typeof(PresentationSource), new PropertyMetadata((bool)false));

        // We use a private direct-only event to notify elements of when the
        // source changes.  Use the public APIs AddSourceChangedHandler and
        // RemoveSourceChangedHandler to listen to this event.
        private static readonly RoutedEvent SourceChangedEvent = EventManager.RegisterRoutedEvent("SourceChanged", RoutingStrategy.Direct, typeof(SourceChangedEventHandler), typeof(PresentationSource));

        // The lock we use to protect our static data.
        private static object _globalLock = new object();

        // An array of weak-references to sources that we know about.
        private static WeakReferenceList _sources = new WeakReferenceList(_globalLock);

        // An array of weak-references to elements that need to know
        // about source changes.
        private static WeakReferenceList _watchers = new WeakReferenceList(_globalLock);

        #endregion
    }
}

