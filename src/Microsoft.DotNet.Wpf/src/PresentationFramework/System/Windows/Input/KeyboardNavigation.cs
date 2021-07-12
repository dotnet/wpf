// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Security;
using MS.Utility;
using MS.Internal.Controls;
using MS.Internal;
using MS.Internal.KnownBoxes;
using Microsoft.Win32;

using CommonDependencyProperty=MS.Internal.PresentationFramework.CommonDependencyPropertyAttribute;

namespace System.Windows.Input
{
    #region public enum types
    /// <summary>
    /// These options specify how the container will move the focus when tab and directional navigation occurs
    /// </summary>
    public enum KeyboardNavigationMode
    {
        /// <summary>
        /// The container does not handle the keyboard navigation;
        /// each element receives keyboard focus as long as it is a key navigation stop.
        /// </summary>
        Continue,

        /// <summary>
        /// The container and all of its child elements as a whole only receive focus once.
        /// Either the first tree child or the ActiveElement receive focus
        /// </summary>
        Once,

        /// <summary>
        /// Depending on the direction of the navigation,
        /// the focus returns to the first or the last item when the end or
        /// the beginning of the container is reached, respectively.
        /// </summary>
        Cycle,

        /// <summary>
        /// No keyboard navigation is allowed inside this container
        /// </summary>
        None,

        /// <summary>
        /// Like cycle but does not move past the beginning or end of the container.
        /// </summary>
        Contained,

        /// <summary>
        /// TabIndexes are considered on local subtree only inside this container
        /// </summary>
        Local,

        // NOTE: if you add or remove any values in this enum, be sure to update KeyboardNavigation.IsValidKeyNavigationMode()
    }
    #endregion public enum types

    ///<summary>
    /// KeyboardNavigation class provide methods for logical (Tab) and directional (arrow) navigation between focusable controls
    ///</summary>
    public sealed class KeyboardNavigation
    {
        #region Constructors

        internal KeyboardNavigation()
        {
            InputManager inputManager = InputManager.Current;

            inputManager.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
            inputManager.TranslateAccelerator += new KeyEventHandler(TranslateAccelerator);
        }

        #endregion Constructors

        #region public API

        #region Properties

        private static readonly DependencyProperty TabOnceActiveElementProperty
            = DependencyProperty.RegisterAttached("TabOnceActiveElement", typeof(WeakReference), typeof(KeyboardNavigation));

        internal static DependencyObject GetTabOnceActiveElement(DependencyObject d)
        {
            WeakReference weakRef = (WeakReference)d.GetValue(TabOnceActiveElementProperty);
            if (weakRef != null && weakRef.IsAlive)
            {
                DependencyObject activeElement = weakRef.Target as DependencyObject;
                // Verify if the element is still in the same visual tree
                if (GetVisualRoot(activeElement) == GetVisualRoot(d))
                    return activeElement;
                else
                    d.SetValue(TabOnceActiveElementProperty, null);
            }
            return null;
        }

        internal static void SetTabOnceActiveElement(DependencyObject d, DependencyObject value)
        {
            d.SetValue(TabOnceActiveElementProperty, new WeakReference(value));
        }

        internal static readonly DependencyProperty ControlTabOnceActiveElementProperty
            = DependencyProperty.RegisterAttached("ControlTabOnceActiveElement", typeof(WeakReference), typeof(KeyboardNavigation));

        private static DependencyObject GetControlTabOnceActiveElement(DependencyObject d)
        {
            WeakReference weakRef = (WeakReference)d.GetValue(ControlTabOnceActiveElementProperty);
            if (weakRef != null && weakRef.IsAlive)
            {
                DependencyObject activeElement = weakRef.Target as DependencyObject;
                // Verify if the element is still in the same visual tree
                if (GetVisualRoot(activeElement) == GetVisualRoot(d))
                    return activeElement;
                else
                    d.SetValue(ControlTabOnceActiveElementProperty, null);
            }
            return null;
        }

        private static void SetControlTabOnceActiveElement(DependencyObject d, DependencyObject value)
        {
            d.SetValue(ControlTabOnceActiveElementProperty, new WeakReference(value));
        }

        private DependencyObject GetActiveElement(DependencyObject d)
        {
            return _navigationProperty == ControlTabNavigationProperty ? GetControlTabOnceActiveElement(d) : GetTabOnceActiveElement(d);
        }

        private void SetActiveElement(DependencyObject d, DependencyObject value)
        {
            if (_navigationProperty == TabNavigationProperty)
                SetTabOnceActiveElement(d, value);
            else
                SetControlTabOnceActiveElement(d, value);
        }

        internal static Visual GetVisualRoot(DependencyObject d)
        {
            if (d is Visual || d is Visual3D)
            {
                PresentationSource source = PresentationSource.CriticalFromVisual(d);

                if (source != null)
                    return source.RootVisual;
            }
            else
            {
                FrameworkContentElement fce = d as FrameworkContentElement;
                if (fce != null)
                    return GetVisualRoot(fce.Parent);
            }

            return null;
        }

        // This internal property is used by GetRectagle method to deflate the bounding box of the element
        // If we expose this in the future - make sure it works with ContentElements too
        internal static readonly DependencyProperty DirectionalNavigationMarginProperty =
                DependencyProperty.RegisterAttached("DirectionalNavigationMargin",
                        typeof(Thickness),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(new Thickness()));


        /// <summary>
        ///     The DependencyProperty for the TabIndex property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      Int32.MaxValue
        /// </summary>
        public static readonly DependencyProperty TabIndexProperty =
                DependencyProperty.RegisterAttached(
                        "TabIndex",
                        typeof(int),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(Int32.MaxValue));

        /// <summary>
        ///     The DependencyProperty for the IsTabStop property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      true
        /// </summary>
        public static readonly DependencyProperty IsTabStopProperty =
                DependencyProperty.RegisterAttached(
                        "IsTabStop",
                        typeof(bool),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));


        /// <summary>
        /// Controls the behavior of logical navigation on the children of the element this property is set on.
        /// TabNavigation is invoked with the TAB key.
        /// </summary>
        [CustomCategory("Accessibility")]
        [Localizability(LocalizationCategory.NeverLocalize)]
        [CommonDependencyProperty]
        public static readonly DependencyProperty TabNavigationProperty =
                DependencyProperty.RegisterAttached(
                        "TabNavigation",
                        typeof(KeyboardNavigationMode),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(KeyboardNavigationMode.Continue),
                        new ValidateValueCallback(IsValidKeyNavigationMode));

        /// <summary>
        /// Controls the behavior of logical navigation on the children of the element this property is set on.
        /// ControlTabNavigation is invoked with the CTRL+TAB key.
        /// </summary>
        [CustomCategory("Accessibility")]
        [Localizability(LocalizationCategory.NeverLocalize)]
        [CommonDependencyProperty]
        public static readonly DependencyProperty ControlTabNavigationProperty =
                DependencyProperty.RegisterAttached(
                        "ControlTabNavigation",
                        typeof(KeyboardNavigationMode),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(KeyboardNavigationMode.Continue),
                        new ValidateValueCallback(IsValidKeyNavigationMode));

        /// <summary>
        /// Controls the behavior of directional navigation on the children of the element this property is set on.
        /// Directional navigation is invoked with the arrow keys.
        /// </summary>
        [CustomCategory("Accessibility")]
        [Localizability(LocalizationCategory.NeverLocalize)]
        [CommonDependencyProperty]
        public static readonly DependencyProperty DirectionalNavigationProperty =
                DependencyProperty.RegisterAttached(
                        "DirectionalNavigation",
                        typeof(KeyboardNavigationMode),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(KeyboardNavigationMode.Continue),
                        new ValidateValueCallback(IsValidKeyNavigationMode));

        /// <summary>
        /// Attached property set on elements registered with AccessKeyManager when AccessKeyCues should be shown.
        /// </summary>
        internal static readonly DependencyProperty ShowKeyboardCuesProperty =
                DependencyProperty.RegisterAttached(
                        "ShowKeyboardCues",
                        typeof(bool),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.OverridesInheritanceBehavior,
                                null /* No PropertyChangedCallback */,
                                new CoerceValueCallback(CoerceShowKeyboardCues)));

        // Coercion for ShowKeyboardCuesProperty
        private static object CoerceShowKeyboardCues(DependencyObject d, object value)
        {
            // Always return true if the user has requested that KeyboardCues always
            // be on (accessibility setting).
            return SystemParameters.KeyboardCues ? BooleanBoxes.TrueBox : value;
        }

        /// <summary>
        /// Indicates if VK_Return character is accepted by a control
        ///
        /// Default: false.
        /// </summary>
        public static readonly DependencyProperty AcceptsReturnProperty =
                DependencyProperty.RegisterAttached(
                        "AcceptsReturn",
                        typeof(bool),
                        typeof(KeyboardNavigation),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        #region Workaround for Bug 908235 -- when focus change events go to PostProcessInput we don't need this glue.
        internal event KeyboardFocusChangedEventHandler FocusChanged
        {
            add
            {
                lock (_weakFocusChangedHandlers)
                {
                    _weakFocusChangedHandlers.Add(value);
                }
            }
            remove
            {
                lock (_weakFocusChangedHandlers)
                {
                    _weakFocusChangedHandlers.Remove(value);
                }
            }
        }

        internal void NotifyFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            _weakFocusChangedHandlers.Process(
                        delegate(object item)
                        {
                            KeyboardFocusChangedEventHandler handler = item as KeyboardFocusChangedEventHandler;
                            if (handler != null)
                            {
                                handler(sender, e);
                            }
                            return false;
                        } );
        }

        private WeakReferenceList _weakFocusChangedHandlers = new WeakReferenceList();
        #endregion

        #endregion Properties

        #region methods

        /// <summary>
        /// Writes the attached property TabIndex to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="index">The property value to set</param>
        /// <seealso cref="KeyboardNavigation.TabIndexProperty" />
        public static void SetTabIndex(DependencyObject element, int index)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(TabIndexProperty, index);
        }

        /// <summary>
        /// Reads the attached property TabIndex from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="KeyboardNavigation.TabIndexProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetTabIndex(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return GetTabIndexHelper(element);
        }

        /// <summary>
        /// Writes the attached property IsTabStop to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="isTabStop">The property value to set</param>
        /// <seealso cref="KeyboardNavigation.IsTabStopProperty" />
        public static void SetIsTabStop(DependencyObject element, bool isTabStop)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsTabStopProperty, BooleanBoxes.Box(isTabStop));
        }

        /// <summary>
        /// Reads the attached property IsTabStop from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="KeyboardNavigation.IsTabStopProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsTabStop(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsTabStopProperty);
        }

        /// <summary>
        /// Writes the attached property TabNavigation to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="mode">The property value to set</param>
        /// <seealso cref="KeyboardNavigation.TabNavigationProperty" />
        public static void SetTabNavigation(DependencyObject element, KeyboardNavigationMode mode)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(TabNavigationProperty, mode);
        }

        /// <summary>
        /// Reads the attached property TabNavigation from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="KeyboardNavigation.TabNavigationProperty" />
        [CustomCategory("Accessibility")]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static KeyboardNavigationMode GetTabNavigation(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (KeyboardNavigationMode)element.GetValue(TabNavigationProperty);
        }

        /// <summary>
        /// Writes the attached property ControlTabNavigation to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="mode">The property value to set</param>
        /// <seealso cref="KeyboardNavigation.ControlTabNavigationProperty" />
        public static void SetControlTabNavigation(DependencyObject element, KeyboardNavigationMode mode)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ControlTabNavigationProperty, mode);
        }

        /// <summary>
        /// Reads the attached property ControlTabNavigation from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="KeyboardNavigation.ControlTabNavigationProperty" />
        [CustomCategory("Accessibility")]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static KeyboardNavigationMode GetControlTabNavigation(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (KeyboardNavigationMode)element.GetValue(ControlTabNavigationProperty);
        }

        /// <summary>
        /// Writes the attached property DirectionalNavigation to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="mode">The property value to set</param>
        /// <seealso cref="KeyboardNavigation.DirectionalNavigationProperty" />
        public static void SetDirectionalNavigation(DependencyObject element, KeyboardNavigationMode mode)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(DirectionalNavigationProperty, mode);
        }

        /// <summary>
        /// Reads the attached property DirectionalNavigation from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="KeyboardNavigation.DirectionalNavigationProperty" />
        [CustomCategory("Accessibility")]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static KeyboardNavigationMode GetDirectionalNavigation(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (KeyboardNavigationMode)element.GetValue(DirectionalNavigationProperty);
        }

        /// <summary>
        /// Writes the attached property AcceptsReturn to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="enabled">The property value to set</param>
        /// <seealso cref="KeyboardNavigation.AcceptsReturnProperty" />
        public static void SetAcceptsReturn(DependencyObject element, bool enabled)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(AcceptsReturnProperty, BooleanBoxes.Box(enabled));
        }

        /// <summary>
        /// Reads the attached property AcceptsReturn from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="KeyboardNavigation.AcceptsReturnProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetAcceptsReturn(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(AcceptsReturnProperty);
        }

        private static bool IsValidKeyNavigationMode(object o)
        {
            KeyboardNavigationMode value = (KeyboardNavigationMode)o;
            return value == KeyboardNavigationMode.Contained
                || value == KeyboardNavigationMode.Continue
                || value == KeyboardNavigationMode.Cycle
                || value == KeyboardNavigationMode.None
                || value == KeyboardNavigationMode.Once
                || value == KeyboardNavigationMode.Local;
        }

        #endregion methods

        #endregion public API

        #region FocusVisualStyle API
        // This class is used by AdornerLayer which adds it to its visual tree
        // Once AdornerLayer and Adorner are UIElement we can remove this class and
        // apply FrameworkElement.FocusVisualStyle directly to AdornerLayer
        // Note:- This class is sealed because it calls OnVisualChildrenChanged virtual in the
        //              constructor and it does not override it, but derived classes could.
        private sealed class FocusVisualAdorner: Adorner
        {
            public FocusVisualAdorner(UIElement adornedElement, Style focusVisualStyle) : base(adornedElement)
            {
                Debug.Assert(adornedElement != null, "adornedElement should not be null");
                Debug.Assert(focusVisualStyle != null, "focusVisual should not be null");

                Control control = new Control();
                control.Style = focusVisualStyle;
                _adorderChild = control;
                IsClipEnabled = true;
                IsHitTestVisible = false;
                IsEnabled = false;
                AddVisualChild(_adorderChild);
            }

            public FocusVisualAdorner(ContentElement adornedElement, UIElement adornedElementParent, IContentHost contentHostParent, Style focusVisualStyle)
                : base(adornedElementParent)
            {
                Debug.Assert(adornedElement != null, "adornedElement should not be null");
                Debug.Assert(adornedElementParent != null, "adornedElementParent should not be null");
                Debug.Assert(contentHostParent != null, "contentHostParent should not be null");
                Debug.Assert(contentHostParent is Visual, "contentHostParent should be Visual");
                Debug.Assert(focusVisualStyle != null, "focusVisual should not be null");

                _contentHostParent = contentHostParent;
                _adornedContentElement = adornedElement;
                _focusVisualStyle = focusVisualStyle;

                Canvas canvas = new Canvas();
                _canvasChildren = canvas.Children;
                _adorderChild = canvas;
                AddVisualChild(_adorderChild);

                IsClipEnabled = true;
                IsHitTestVisible = false;
                IsEnabled = false;
            }

            /// <summary>
            /// Measure adorner. Default behavior is to size to match the adorned element.
            /// </summary>
            protected override Size MeasureOverride(Size constraint)
            {
                Size desiredSize = new Size();

                // If the focus visual is adorning a content element,
                // the child will be a canvas that doesn't need to be measured.
                if (_adornedContentElement == null)
                {
                    desiredSize = AdornedElement.RenderSize;
                    constraint = desiredSize;
                }

                // Measure the child
                ((UIElement)GetVisualChild(0)).Measure(constraint);

                return desiredSize;
           }

            /// <summary>
            ///     Default control arrangement is to only arrange
            ///     the first visual child. No transforms will be applied.
            /// </summary>
            protected override Size ArrangeOverride(Size size)
            {
                Size finalSize = base.ArrangeOverride(size);

                // In case we adorn ContentElement we have to update the rectangles
                if (_adornedContentElement != null)
                {
                    if (_contentRects == null)
                    {
                        // Clear rects
                        _canvasChildren.Clear();
                    }
                    else
                    {
                        IContentHost contentHost = ContentHost;

                        if (!(contentHost is Visual) || !AdornedElement.IsAncestorOf((Visual)contentHost))
                        {
                            // Content elements is not in the tree, clear children and give up.
                            _canvasChildren.Clear();
                            return new Size();
                        }

                        Rect desiredRect = Rect.Empty;

                        IEnumerator<Rect> enumerator = _contentRects.GetEnumerator();

                        if (_canvasChildren.Count == _contentRects.Count)
                        {
                            // Reuse the controls and update the controls position
                            for (int i = 0; i < _canvasChildren.Count; i++)
                            {
                                enumerator.MoveNext();
                                Rect rect = enumerator.Current;

                                rect = _hostToAdornedElement.TransformBounds(rect);

                                Control control = (Control)_canvasChildren[i];
                                control.Width = rect.Width;
                                control.Height = rect.Height;
                                Canvas.SetLeft(control, rect.X);
                                Canvas.SetTop(control, rect.Y);
                            }
                            _adorderChild.InvalidateArrange();
                        }
                        else // Rebuild the visual tree to correspond to current bounding rectangles
                        {
                            _canvasChildren.Clear();
                            while (enumerator.MoveNext())
                            {
                                Rect rect = enumerator.Current;

                                rect = _hostToAdornedElement.TransformBounds(rect);

                                Control control = new Control();
                                control.Style = _focusVisualStyle;
                                control.Width = rect.Width;
                                control.Height = rect.Height;
                                Canvas.SetLeft(control, rect.X);
                                Canvas.SetTop(control, rect.Y);
                                _canvasChildren.Add(control);
                            }
                        }
                    }
                }

                ((UIElement)GetVisualChild(0)).Arrange(new Rect(new Point(), finalSize));

                return finalSize;
            }

            /// <summary>
            ///  Derived classes override this property to enable the Visual code to enumerate
            ///  the Visual children. Derived classes need to return the number of children
            ///  from this method.
            ///
            ///    By default a Visual does not have any children.
            ///
            ///  Remark:
            ///      During this virtual method the Visual tree must not be modified.
            /// </summary>
            protected override int VisualChildrenCount
            {
                get
                {
                    return 1; // _adorderChild created in ctor.
                }
            }

            /// <summary>
            ///   Derived class must implement to support Visual children. The method must return
            ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
            ///
            ///    By default a Visual does not have any children.
            ///
            ///  Remark:
            ///       During this virtual call it is not valid to modify the Visual tree.
            /// </summary>
            protected override Visual GetVisualChild(int index)
            {
                if (index == 0)
                {
                    return _adorderChild;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
                }
            }

            private IContentHost ContentHost
            {
                get
                {
                    // Re-query IContentHost if the old one was disposed
                    if (_adornedContentElement != null && (_contentHostParent==null || VisualTreeHelper.GetParent(_contentHostParent as Visual) == null))
                    {
                        _contentHostParent = MS.Internal.Documents.ContentHostHelper.FindContentHost(_adornedContentElement);
                    }

                    return _contentHostParent;
                }
            }

            /// <summary>
            /// Says if the Adorner needs update based on the
            /// previously cached size if the AdornedElement.
            /// </summary>
            internal override bool NeedsUpdate(Size oldSize)
            {
                if (_adornedContentElement != null)
                {
                    ReadOnlyCollection<Rect> oldRects = _contentRects;
                    _contentRects = null;

                    IContentHost contentHost = ContentHost;
                    if (contentHost != null)
                    {
                        _contentRects = contentHost.GetRectangles(_adornedContentElement);
                    }

                    // The positions of the focus rects are dependent on the rects returned from
                    // host.GetRectangles and the transform from the host to the adorned element
                    GeneralTransform oldTransform = _hostToAdornedElement;

                    if (contentHost is Visual && AdornedElement.IsAncestorOf((Visual)contentHost))
                    {
                        _hostToAdornedElement = ((Visual)contentHost).TransformToAncestor(AdornedElement);
                    }
                    else
                    {
                        _hostToAdornedElement = Transform.Identity;
                    }

                    // See if these are the same transform
                    if (oldTransform != _hostToAdornedElement)
                    {
                        // Allow two identical matrix transforms
                        if (!(oldTransform is MatrixTransform) ||
                            !(_hostToAdornedElement is MatrixTransform) ||
                            !Matrix.Equals(((MatrixTransform)oldTransform).Matrix, ((MatrixTransform)_hostToAdornedElement).Matrix))
                        {
                            // one is a general transform or the matrices are not equal, need to update
                            return true;
                        }
                    }

                    if (_contentRects != null && oldRects != null && _contentRects.Count == oldRects.Count)
                    {
                        for (int i=0; i<oldRects.Count; i++)
                        {
                            if (!DoubleUtil.AreClose(oldRects[i].Size, _contentRects[i].Size))
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    return _contentRects != oldRects;
                }
                else
                {
                    return !DoubleUtil.AreClose(AdornedElement.RenderSize, oldSize);
                }
            }


            GeneralTransform _hostToAdornedElement = Transform.Identity;
            private IContentHost _contentHostParent;
            private ContentElement _adornedContentElement;
            private Style _focusVisualStyle;
            private UIElement _adorderChild;
            private UIElementCollection _canvasChildren;
            private ReadOnlyCollection<Rect> _contentRects;
        }

        internal static UIElement GetParentUIElementFromContentElement(ContentElement ce)
        {
            IContentHost ichParent = null;
            return GetParentUIElementFromContentElement(ce, ref ichParent);
        }

        internal static UIElement GetParentUIElementFromContentElement(ContentElement ce, ref IContentHost ichParent)
        {
            if (ce == null)
                return null;

            IContentHost ich = MS.Internal.Documents.ContentHostHelper.FindContentHost(ce);
            if (ichParent == null)
                ichParent = ich;

            DependencyObject parent =  ich as DependencyObject;
            if(parent != null)
            {
                // Case 1: UIElement
                // return the element
                UIElement eParent = parent as UIElement;
                if(eParent != null)
                    return eParent;

                // Case 2: Visual
                // Walk up the visual tree until we find UIElement
                Visual visualParent = parent as Visual;
                while (visualParent != null)
                {
                    visualParent = VisualTreeHelper.GetParent(visualParent) as Visual;
                    UIElement uielement = visualParent as UIElement;
                    if (uielement != null)
                        return uielement;
                }

                // Case 3: ContentElement
                ContentElement ceParent = parent as ContentElement;
                if(ceParent != null)
                    return GetParentUIElementFromContentElement(ceParent, ref ichParent);
            }

            return null;
        }

        internal void HideFocusVisual()
        {
            // Remove the existing focus visual
            if (_focusVisualAdornerCache != null)
            {
                AdornerLayer adornerlayer = VisualTreeHelper.GetParent(_focusVisualAdornerCache) as AdornerLayer;
                if (adornerlayer != null)
                {
                    adornerlayer.Remove(_focusVisualAdornerCache);
                }
                _focusVisualAdornerCache = null;
            }
        }
        internal static bool IsKeyboardMostRecentInputDevice()
        {
            return InputManager.Current.MostRecentInputDevice is KeyboardDevice;
        }

        internal static bool AlwaysShowFocusVisual
        {
            get
            {
                return _alwaysShowFocusVisual;
            }
            set
            {
                _alwaysShowFocusVisual = value;
            }
        }
        private static bool _alwaysShowFocusVisual = SystemParameters.KeyboardCues;

        internal static void ShowFocusVisual()
        {
            Current.ShowFocusVisual(Keyboard.FocusedElement as DependencyObject);
        }

        private void ShowFocusVisual(DependencyObject element)
        {
            // Always hide the existing focus visual
            HideFocusVisual();

            // Disable keyboard cues (accesskey underline) if keyboard device is not MostRecentInputDevice
            if (!IsKeyboardMostRecentInputDevice())
            {
                EnableKeyboardCues(element, false);
            }

            // Show focus visual if system metric is true or keyboard is used last
            if (AlwaysShowFocusVisual || IsKeyboardMostRecentInputDevice())
            {
                FrameworkElement fe = element as FrameworkElement;
                if (fe != null)
                {
                    AdornerLayer adornerlayer = AdornerLayer.GetAdornerLayer(fe);
                    if (adornerlayer == null)
                        return;

                    Style fvs = fe.FocusVisualStyle;

                    // WORKAROUND: (Bug 1016350) If FocusVisualStyle is the "default" value
                    // then we load the default FocusVisualStyle from ResourceDictionary.
                    if (fvs == FrameworkElement.DefaultFocusVisualStyle)
                    {
                        fvs = FrameworkElement.FindResourceInternal(fe, null /* fce */, SystemParameters.FocusVisualStyleKey) as Style;
                    }

                    if (fvs != null)
                    {
                        _focusVisualAdornerCache = new FocusVisualAdorner(fe, fvs);
                        adornerlayer.Add(_focusVisualAdornerCache);
                    }
                }
                else // If not FrameworkElement
                {
                    FrameworkContentElement fce = element as FrameworkContentElement;
                    if (fce != null)
                    {
                        IContentHost parentICH = null;
                        UIElement parentUIElement = GetParentUIElementFromContentElement(fce, ref parentICH);
                        if (parentICH != null && parentUIElement != null)
                        {
                            AdornerLayer adornerlayer = AdornerLayer.GetAdornerLayer(parentUIElement);
                            if (adornerlayer != null)
                            {
                                Style fvs = fce.FocusVisualStyle;

                                // WORKAROUND: (Bug 1016350) If FocusVisualStyle is the "default" value
                                // then we load the default FocusVisualStyle from ResourceDictionary.
                                if (fvs == FrameworkElement.DefaultFocusVisualStyle)
                                {
                                    fvs = FrameworkElement.FindResourceInternal(null /* fe */, fce, SystemParameters.FocusVisualStyleKey) as Style;
                                }

                                if (fvs != null)
                                {
                                    _focusVisualAdornerCache = new FocusVisualAdorner(fce, parentUIElement, parentICH, fvs);
                                    adornerlayer.Add(_focusVisualAdornerCache);
                                }
                            }
                        }
                    }
                }
            }
        }

        private FocusVisualAdorner _focusVisualAdornerCache = null;

        #endregion FocusVisualStyle API

        #region Navigate helpers

        internal static void UpdateFocusedElement(DependencyObject focusTarget)
        {
            DependencyObject focusScope = FocusManager.GetFocusScope(focusTarget);
            if (focusScope != null && focusScope != focusTarget)
            {
                FocusManager.SetFocusedElement(focusScope, focusTarget as IInputElement);

                // Raise FocusEnterMainFocusScope event
                Visual visualRoot = GetVisualRoot(focusTarget);
                if (visualRoot != null && focusScope == visualRoot)
                {
                    Current.NotifyFocusEnterMainFocusScope(visualRoot, EventArgs.Empty);
                }
            }
        }

        internal void UpdateActiveElement(DependencyObject activeElement)
        {
            // Update TabNavigation = Once groups
            UpdateActiveElement(activeElement, TabNavigationProperty);

            // Update ControlTabNavigation = Once groups
            UpdateActiveElement(activeElement, ControlTabNavigationProperty);
        }

        private void UpdateActiveElement(DependencyObject activeElement, DependencyProperty dp)
        {
            _navigationProperty = dp;
            DependencyObject container = GetGroupParent(activeElement);
            UpdateActiveElement(container, activeElement, dp);
        }

        internal void UpdateActiveElement(DependencyObject container, DependencyObject activeElement)
        {
            // Update TabNavigation = Once groups
            UpdateActiveElement(container, activeElement, TabNavigationProperty);

            // Update ControlTabNavigation = Once groups
            UpdateActiveElement(container, activeElement, ControlTabNavigationProperty);
        }

        private void UpdateActiveElement(DependencyObject container, DependencyObject activeElement, DependencyProperty dp)
        {
            _navigationProperty = dp;

            if (activeElement == container)
                return;

            // Update ActiveElement only if container has TabNavigation = Once
            if (GetKeyNavigationMode(container) == KeyboardNavigationMode.Once)
            {
                SetActiveElement(container, activeElement);
            }
        }

        // Called from FrameworkElement.MoveFocus
        internal bool Navigate(DependencyObject currentElement, TraversalRequest request)
        {
            return Navigate(currentElement, request, Keyboard.Modifiers);
        }

        // Navigate needs the extra fromProcessInputTabKey parameter to know if this call is coming directly from a keyboard tab key ProcessInput call
        // so it can return false and let the key message go unhandled, we don't want to do this if Navigate was called by MoveFocus, or recursively.
        private bool Navigate(DependencyObject currentElement, TraversalRequest request, ModifierKeys modifierKeys, bool fromProcessInputTabKey = false)
        {
            return Navigate(currentElement, request, modifierKeys, null, fromProcessInputTabKey);
        }

        private bool Navigate(DependencyObject currentElement, TraversalRequest request, ModifierKeys modifierKeys, DependencyObject firstElement, bool fromProcessInputTabKey = false)
        {
            Debug.Assert(currentElement != null, "currentElement should not be null");
            DependencyObject nextTab = null;
            IKeyboardInputSink inputSink = null;

            switch (request.FocusNavigationDirection)
            {
                case FocusNavigationDirection.Next:
                    _navigationProperty = (modifierKeys & ModifierKeys.Control) == ModifierKeys.Control ? ControlTabNavigationProperty : TabNavigationProperty;
                    nextTab = GetNextTab(currentElement, GetGroupParent(currentElement, true /*includeCurrent*/), false);
                    break;

                case FocusNavigationDirection.Previous:
                    _navigationProperty = (modifierKeys & ModifierKeys.Control) == ModifierKeys.Control ? ControlTabNavigationProperty : TabNavigationProperty;
                    nextTab = GetPrevTab(currentElement, null, false);
                    break;

                case FocusNavigationDirection.First:
                    _navigationProperty = (modifierKeys & ModifierKeys.Control) == ModifierKeys.Control ? ControlTabNavigationProperty : TabNavigationProperty;
                    nextTab = GetNextTab(null, currentElement, true);
                    break;

                case FocusNavigationDirection.Last:
                    _navigationProperty = (modifierKeys & ModifierKeys.Control) == ModifierKeys.Control ? ControlTabNavigationProperty : TabNavigationProperty;
                    nextTab = GetPrevTab(null, currentElement, true);
                    break;

                case FocusNavigationDirection.Left:
                case FocusNavigationDirection.Right:
                case FocusNavigationDirection.Up:
                case FocusNavigationDirection.Down:
                    _navigationProperty = DirectionalNavigationProperty;
                    nextTab = GetNextInDirection(currentElement, request.FocusNavigationDirection);
                    break;
            }

            // If there are no other tabstops, try to pass focus outside PresentationSource
            if (nextTab == null)
            {
                // If Wrapped is true we should not searach outside this container
                if (request.Wrapped || request.FocusNavigationDirection == FocusNavigationDirection.First || request.FocusNavigationDirection == FocusNavigationDirection.Last)
                    return false;

                bool shouldCycle = true;
                // Try to navigate outside the PresentationSource
                // Depending on whether this Navigate call came directly from a ProcessInput tab key call,
                // NavigateOutsidePresentationSource may determine that we should not cycle, and instead bubble up the failure to find a next element for focus.
                bool navigatedOutside = NavigateOutsidePresentationSource(currentElement, request, fromProcessInputTabKey, ref shouldCycle);
                if (navigatedOutside)
                {
                    return true;
                }
                else if (shouldCycle && (request.FocusNavigationDirection == FocusNavigationDirection.Next || request.FocusNavigationDirection == FocusNavigationDirection.Previous))
                {
                    // In case focus cannot navigate outside - we should cycle
                    Visual visualRoot = GetVisualRoot(currentElement);
                    if (visualRoot != null)
                        return Navigate(visualRoot, new TraversalRequest(request.FocusNavigationDirection == FocusNavigationDirection.Next ? FocusNavigationDirection.First : FocusNavigationDirection.Last));
                }

                return false;
            }

            inputSink = nextTab as IKeyboardInputSink;
            if (inputSink == null)
            {
                // If target element does not support IKeyboardInputSink then we try to set focus
                // In TextBox scenario Focus() return false although the focus is set to TextBox content
                // So we need to verify IsKeyboardFocusWithin property (bugfix 954000)
                IInputElement iie = nextTab as IInputElement;
                iie.Focus();

                return iie.IsKeyboardFocusWithin;
            }
            else
            {
                // If target element supports IKeyboardInputSink then we pass the focus there
                bool traversed = false;

                if (request.FocusNavigationDirection == FocusNavigationDirection.First || request.FocusNavigationDirection == FocusNavigationDirection.Next)
                {
                    traversed = inputSink.TabInto(new TraversalRequest(FocusNavigationDirection.First));
                }
                else if (request.FocusNavigationDirection == FocusNavigationDirection.Last || request.FocusNavigationDirection == FocusNavigationDirection.Previous)
                {
                    traversed = inputSink.TabInto(new TraversalRequest(FocusNavigationDirection.Last));
                }
                else // FocusNavigationDirection
                {
                    TraversalRequest tr = new TraversalRequest(request.FocusNavigationDirection);
                    tr.Wrapped = true;
                    traversed = inputSink.TabInto(tr);
                }

                // If we fail to navigate into IKeyboardInputSink then move to the next element
                if (!traversed && firstElement != nextTab)
                {
                    // Navigate to next element in the tree
                    traversed = Navigate(nextTab, request, modifierKeys, firstElement == null ? nextTab : firstElement);
                }

                return traversed;
            }
        }

        /// <param name="currentElement">The element from which Navigation is starting.</param>
        /// <param name="request">The TraversalRequest that determines the navigation direction.</param>
        /// <param name="fromProcessInput">Whether this call comes from a ProcessInput call.</param>
        /// <param name="shouldCycle">A recommendation on whether navigation should cycle in case this call can't navigate outside the PresentationSource.</param>
        private bool NavigateOutsidePresentationSource(DependencyObject currentElement, TraversalRequest request, bool fromProcessInput, ref bool shouldCycle)
        {
            Visual visual = currentElement as Visual;
            if (visual == null)
            {
                visual = GetParentUIElementFromContentElement(currentElement as ContentElement);
                if (visual == null)
                    return false;
            }

            IKeyboardInputSink inputSink = PresentationSource.CriticalFromVisual(visual) as IKeyboardInputSink;
            if (inputSink != null)
            {
                IKeyboardInputSite ikis = null;
                ikis = inputSink.KeyboardInputSite;

                if (ikis != null && ShouldNavigateOutsidePresentationSource(currentElement, request))
                {
                    // Only call into the specialized OnNoMoreTabStops if App context flag is set, 
                    // otherwise we call into the regular OnNoMoreTabStops and leave shouldCycle as it is
                    if (!AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures)
                    {
                        System.Windows.Input.IAvalonAdapter avalonAdapter = ikis as System.Windows.Input.IAvalonAdapter;
                        if (avalonAdapter != null && fromProcessInput)
                        {
                            return avalonAdapter.OnNoMoreTabStops(request, ref shouldCycle);
                        }
                    }
                    return ikis.OnNoMoreTabStops(request);
                }
            }

            return false;
        }

        private bool ShouldNavigateOutsidePresentationSource(DependencyObject currentElement, TraversalRequest request)
        {
            // One should not navigate (directional) outside the
            // presentation source if any of the parent has
            // Contained or Cycle navigation mode.
            if (request.FocusNavigationDirection == FocusNavigationDirection.Left ||
                request.FocusNavigationDirection == FocusNavigationDirection.Right ||
                request.FocusNavigationDirection == FocusNavigationDirection.Up ||
                request.FocusNavigationDirection == FocusNavigationDirection.Down)
            {
                DependencyObject parent = null;

                // The looping should stop as soon as either the GroupParent is
                // null or is currentElement itself.
                while ((parent = GetGroupParent(currentElement)) != null &&
                    parent != currentElement)
                {
                    KeyboardNavigationMode mode = GetKeyNavigationMode(parent);
                    if (mode == KeyboardNavigationMode.Contained || mode == KeyboardNavigationMode.Cycle)
                    {
                        return false;
                    }
                    currentElement = parent;
                }
            }
            return true;
        }

        internal static KeyboardNavigation Current
        {
            get
            {
                return FrameworkElement.KeyboardNavigation;
            }
        }

        /////////////////////////////////////////////////////////////////////
        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            // Call Forwarded
            ProcessInput(e.StagingItem.Input);
        }

        /////////////////////////////////////////////////////////////////////
        private void TranslateAccelerator(object sender, KeyEventArgs e)
        {
            // Call Forwarded
            ProcessInput(e);
        }

        /////////////////////////////////////////////////////////////////////
        private void ProcessInput(InputEventArgs inputEventArgs)
        {
            ProcessForMenuMode(inputEventArgs);
            ProcessForUIState(inputEventArgs);

            // Process keyboard navigation for keydown event for Tab,Left,Right,Up,Down keys.
            if(inputEventArgs.RoutedEvent != Keyboard.KeyDownEvent)
                return;

            KeyEventArgs keyEventArgs = (KeyEventArgs)inputEventArgs;
            if (keyEventArgs.Handled)
                return;

            DependencyObject sourceElement = keyEventArgs.OriginalSource as DependencyObject;

            // For Keyboard Interop with Avalon-inside-Avalon via HwndHost.
            // In Keyboard interop, the target (called OriginalSource here) is "forced"
            // to point at the HwndHost containing the Hwnd with focus.  This allows
            // us to tunnel/bubble the keystroke across the outer HwndSource to the
            // child hwnd that has focus.  (see HwndSource.TranslateAccelerator)
            // But this "forced" target is wrong for Tab Navigation; eg. tabbing
            // across an inner avalon under the HwndHost.   For that we need the
            // real original target element, which we happen to find in KeyboardDevice.Target.
            //
            // sourceElement and innerElement I don't expect will ever be different
            // except in this case.  And I added a check that the "forced" target
            // is an HwndHost for good measure.
            DependencyObject innerElement = keyEventArgs.KeyboardDevice.Target as DependencyObject;
            if( innerElement != null && sourceElement != innerElement )
            {
                if(sourceElement is HwndHost)
                    sourceElement = innerElement;
            }

            // When nothing has focus - we should start from the root of the visual tree
            if (sourceElement == null)
            {
                HwndSource hwndSource = keyEventArgs.UnsafeInputSource as HwndSource;
                if (hwndSource == null)
                    return;

                sourceElement = hwndSource.RootVisual;
                if (sourceElement == null)
                    return;
            }

            // Focus visual support
            switch (GetRealKey(keyEventArgs))
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    ShowFocusVisual();
                    EnableKeyboardCues(sourceElement, true);
                    break;
                case Key.Tab:
                case Key.Right:
                case Key.Left:
                case Key.Up:
                case Key.Down:
                    ShowFocusVisual();
                    break;
            }

            keyEventArgs.Handled = Navigate(sourceElement, keyEventArgs.Key, keyEventArgs.KeyboardDevice.Modifiers, fromProcessInput: true);
        }

        internal static void EnableKeyboardCues(DependencyObject element, bool enable)
        {
            Visual visual = element as Visual;
            if (visual == null)
            {
                visual = GetParentUIElementFromContentElement(element as ContentElement);
                if (visual == null)
                    return;
            }

            Visual rootVisual = GetVisualRoot(visual);
            if (rootVisual != null)
            {
                rootVisual.SetValue(ShowKeyboardCuesProperty, enable ? BooleanBoxes.TrueBox : BooleanBoxes.FalseBox);
            }
        }

        internal static FocusNavigationDirection KeyToTraversalDirection(Key key)
        {
            switch (key)
            {
                case Key.Left:
                    return FocusNavigationDirection.Left;

                case Key.Right:
                    return FocusNavigationDirection.Right;

                case Key.Up:
                    return FocusNavigationDirection.Up;

                case Key.Down:
                    return FocusNavigationDirection.Down;
            }

            throw new NotSupportedException();
        }

        internal DependencyObject PredictFocusedElement(DependencyObject sourceElement, FocusNavigationDirection direction)
        {
            return PredictFocusedElement(sourceElement, direction, /*treeViewNavigation*/ false);
        }

        internal DependencyObject PredictFocusedElement(DependencyObject sourceElement, FocusNavigationDirection direction, bool treeViewNavigation)
        {
            return PredictFocusedElement(sourceElement, direction, treeViewNavigation, considerDescendants:true);
        }

        internal DependencyObject PredictFocusedElement(DependencyObject sourceElement,
            FocusNavigationDirection direction,
            bool treeViewNavigation,
            bool considerDescendants)
        {
            if (sourceElement == null)
            {
                return null;
            }

            _navigationProperty = DirectionalNavigationProperty;
            _verticalBaseline = BASELINE_DEFAULT;
            _horizontalBaseline = BASELINE_DEFAULT;
            return GetNextInDirection(sourceElement, direction, treeViewNavigation, considerDescendants);
        }

        internal DependencyObject PredictFocusedElementAtViewportEdge(
            DependencyObject sourceElement,
            FocusNavigationDirection direction,
            bool treeViewNavigation,
            FrameworkElement viewportBoundsElement,
            DependencyObject container)
        {
            try
            {
                _containerHashtable.Clear();

                return PredictFocusedElementAtViewportEdgeRecursive(
                    sourceElement,
                    direction,
                    treeViewNavigation,
                    viewportBoundsElement,
                    container);
            }
            finally
            {
                _containerHashtable.Clear();
            }
        }

        private DependencyObject PredictFocusedElementAtViewportEdgeRecursive(
            DependencyObject sourceElement,
            FocusNavigationDirection direction,
            bool treeViewNavigation,
            FrameworkElement viewportBoundsElement,
            DependencyObject container)
        {
            _navigationProperty = DirectionalNavigationProperty;
            _verticalBaseline = BASELINE_DEFAULT;
            _horizontalBaseline = BASELINE_DEFAULT;

            if (container == null)
            {
                container = GetGroupParent(sourceElement);
                Debug.Assert(container != null, "container cannot be null");
            }

            // If we get to the tree root, return null
            if (container == sourceElement)
                return null;

            if (IsEndlessLoop(sourceElement, container))
                return null;

            DependencyObject result = FindElementAtViewportEdge(sourceElement,
                viewportBoundsElement,
                container,
                direction,
                treeViewNavigation);

            if (result != null)
            {
                if (IsElementEligible(result, treeViewNavigation))
                {
                    return result;
                }

                DependencyObject groupResult = result;

                // Try to find focus inside the element
                // result is not TabStop, which means it is a group
                result = PredictFocusedElementAtViewportEdgeRecursive(
                    sourceElement,
                    direction,
                    treeViewNavigation,
                    viewportBoundsElement,
                    result);
                if (result != null)
                {
                    return result;
                }

                result = PredictFocusedElementAtViewportEdgeRecursive(
                    groupResult,
                    direction,
                    treeViewNavigation,
                    viewportBoundsElement,
                    null);
            }

            return result;
        }

        internal bool Navigate(DependencyObject sourceElement, Key key, ModifierKeys modifiers, bool fromProcessInput = false)
        {
            bool success = false;

            switch (key)
            {
                // Logical (Tab) navigation
                case Key.Tab:
                    success = Navigate(sourceElement,
                        new TraversalRequest(((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) ?
                        FocusNavigationDirection.Previous : FocusNavigationDirection.Next), modifiers, fromProcessInput);
                    break;

                case Key.Right:
                    success = Navigate(sourceElement, new TraversalRequest(FocusNavigationDirection.Right), modifiers);
                    break;

                case Key.Left:
                    success = Navigate(sourceElement, new TraversalRequest(FocusNavigationDirection.Left), modifiers);
                    break;

                case Key.Up:
                    success = Navigate(sourceElement, new TraversalRequest(FocusNavigationDirection.Up), modifiers);
                    break;

                case Key.Down:
                    success = Navigate(sourceElement, new TraversalRequest(FocusNavigationDirection.Down), modifiers);
                    break;
            }
            return success;
        }

        #endregion Navigate helpers

        #region Tree navigation

        // Filter the visual tree and return true if:
        // 1. visual is visible UIElement
        // 2. visual is visible UIElement3D
        // 3. visual is IContentHost but not UIElementIsland
        // Note: UIElementIsland is a special element that has only one child and should be excluded
        private static bool IsInNavigationTree(DependencyObject visual)
        {
            UIElement uiElement = visual as UIElement;
            if (uiElement != null && uiElement.IsVisible)
                return true;

            if (visual is IContentHost && !(visual is MS.Internal.Documents.UIElementIsland))
                return true;

            UIElement3D uiElement3D = visual as UIElement3D;
            if (uiElement3D != null && uiElement3D.IsVisible)
                return true;

            return false;
        }

        private DependencyObject GetPreviousSibling(DependencyObject e)
        {
            DependencyObject parent = GetParent(e);

            // If parent is IContentHost - get next from the enumerator
            IContentHost ich = parent as IContentHost;
            if (ich != null)
            {
                IInputElement previousElement = null;
                IEnumerator<IInputElement> enumerator = ich.HostedElements;
                while (enumerator.MoveNext())
                {
                    IInputElement current = enumerator.Current;
                    if (current == e)
                        return previousElement as DependencyObject;

                    if (current is UIElement || current is UIElement3D)
                        previousElement = current;
                    else
                    {
                        ContentElement ce = current as ContentElement;
                        if (ce != null && IsTabStop(ce))
                            previousElement = current;
                    }
                }
                return null;
            }
            else
            {
                // If parent is UIElement(3D) - return visual sibling
                DependencyObject parentAsUIElement = parent as UIElement;
                if (parentAsUIElement == null)
                {
                    parentAsUIElement = parent as UIElement3D;
                }
                DependencyObject elementAsVisual = e as Visual;
                if (elementAsVisual == null)
                {
                    elementAsVisual = e as Visual3D;
                }

                if (parentAsUIElement != null && elementAsVisual != null)
                {
                    int count = VisualTreeHelper.GetChildrenCount(parentAsUIElement);
                    DependencyObject prev = null;
                    for(int i = 0; i < count; i++)
                    {
                        DependencyObject vchild = VisualTreeHelper.GetChild(parentAsUIElement, i);
                        if(vchild == elementAsVisual) break;
                        if (IsInNavigationTree(vchild))
                            prev = vchild;
                    }
                    return prev;
                }
            }
            return null;
        }

        private DependencyObject GetNextSibling(DependencyObject e)
        {
            DependencyObject parent = GetParent(e);

            // If parent is IContentHost - get next from the enumerator
            IContentHost ich = parent as IContentHost;
            if (ich != null)
            {
                IEnumerator<IInputElement> enumerator = ich.HostedElements;
                bool found = false;
                while (enumerator.MoveNext())
                {
                    IInputElement current = enumerator.Current;
                    if (found)
                    {
                        if (current is UIElement || current is UIElement3D)
                            return current as DependencyObject;
                        else
                        {
                            ContentElement ce = current as ContentElement;
                            if (ce != null && IsTabStop(ce))
                                return ce;
                        }
                    }
                    else if (current == e)
                    {
                        found = true;
                    }
                }
            }
            else
            {
                // If parent is UIElement(3D) - return visual sibling
                DependencyObject parentAsUIElement = parent as UIElement;
                if (parentAsUIElement == null)
                {
                    parentAsUIElement = parent as UIElement3D;
                }
                DependencyObject elementAsVisual = e as Visual;
                if (elementAsVisual == null)
                {
                    elementAsVisual = e as Visual3D;
                }

                if (parentAsUIElement != null && elementAsVisual != null)
                {
                    int count = VisualTreeHelper.GetChildrenCount(parentAsUIElement);
                    int i = 0;
                    //go till itself
                    for(; i < count; i++)
                    {
                        DependencyObject vchild = VisualTreeHelper.GetChild(parentAsUIElement, i);
                        if(vchild == elementAsVisual) break;
                    }
                    i++;
                    //search ahead
                    for(; i < count; i++)
                    {
                        DependencyObject visual = VisualTreeHelper.GetChild(parentAsUIElement, i);
                        if (IsInNavigationTree(visual))
                            return visual;
                    }
                }
            }

            return null;
        }

        // For Control+Tab navigation or TabNavigation when fe is not a FocusScope:
        // Scenarios:
        // 1. UserControl can set its FocusedElement to delegate focus when Tab navigation happens
        // 2. ToolBar or Menu (which have IsFocusScope=true) both have FocusedElement but included only in Control+Tab navigation
        private DependencyObject FocusedElement(DependencyObject e)
        {
            IInputElement iie = e as IInputElement;
            // Focus delegation is enabled only if keyboard focus is outside the container
            if (iie != null && !iie.IsKeyboardFocusWithin)
            {
                DependencyObject focusedElement = FocusManager.GetFocusedElement(e) as DependencyObject;
                if (focusedElement != null)
                {
                    if (_navigationProperty == ControlTabNavigationProperty || !IsFocusScope(e))
                    {
                        // Verify if focusedElement is a visual descendant of e
                        Visual visualFocusedElement = focusedElement as Visual;
                        if (visualFocusedElement == null)
                        {
                            Visual3D visual3DFocusedElement = focusedElement as Visual3D;
                            if (visual3DFocusedElement == null)
                            {
                                visualFocusedElement = GetParentUIElementFromContentElement(focusedElement as ContentElement);
                            }
                            else
                            {
                                if (visual3DFocusedElement != e && visual3DFocusedElement.IsDescendantOf(e))
                                {
                                    return focusedElement;
                                }
                            }
                        }
                        if (visualFocusedElement != null && visualFocusedElement != e && visualFocusedElement.IsDescendantOf(e))
                        {
                            return focusedElement;
                        }
                    }
                }
            }

            return null;
        }

        // We traverse only UIElement(3D) or ContentElement
        private DependencyObject GetFirstChild(DependencyObject e)
        {
            // If the element has a FocusedElement it should be its first child
            DependencyObject focusedElement = FocusedElement(e);
            if (focusedElement != null)
                return focusedElement;

            // If the element is IContentHost - return the first child
            IContentHost ich = e as IContentHost;
            if (ich != null)
            {
                IEnumerator<IInputElement> enumerator = ich.HostedElements;
                while (enumerator.MoveNext())
                {
                    IInputElement current = enumerator.Current;
                    if (current is UIElement || current is UIElement3D)
                    {
                        return current as DependencyObject;
                    }
                    else
                    {
                        ContentElement ce = current as ContentElement;
                        if (ce != null && IsTabStop(ce))
                            return ce;
                    }
                }
                return null;
            }

            // Return the first visible UIElement(3D) or IContentHost
            DependencyObject uiElement = e as UIElement;
            if (uiElement == null)
            {
                uiElement = e as UIElement3D;
            }

            if (uiElement == null ||
                UIElementHelper.IsVisible(uiElement))
            {
                DependencyObject elementAsVisual = e as Visual;
                if (elementAsVisual == null)
                {
                    elementAsVisual = e as Visual3D;
                }

                if (elementAsVisual != null)
                {
                    int count = VisualTreeHelper.GetChildrenCount(elementAsVisual);
                    for (int i = 0; i < count; i++)
                    {
                        DependencyObject visual = VisualTreeHelper.GetChild(elementAsVisual, i);
                        if (IsInNavigationTree(visual))
                            return visual;
                        else
                        {
                            DependencyObject firstChild = GetFirstChild(visual);
                            if (firstChild != null)
                                return firstChild;
                        }
                    }
                }
            }

            // If element is ContentElement for example
            return null;
        }

        private DependencyObject GetLastChild(DependencyObject e)
        {
            // If the element has a FocusedElement it should be its last child
            DependencyObject focusedElement = FocusedElement(e);
            if (focusedElement != null)
                return focusedElement;

            // If the element is IContentHost - return the last child
            IContentHost ich = e as IContentHost;
            if (ich != null)
            {
                IEnumerator<IInputElement> enumerator = ich.HostedElements;
                IInputElement last = null;
                while (enumerator.MoveNext())
                {
                    IInputElement current = enumerator.Current;
                    if (current is UIElement || current is UIElement3D)
                        last = current;
                    else
                    {
                        ContentElement ce = current as ContentElement;
                        if (ce != null && IsTabStop(ce))
                            last = current;
                    }
                }
                return last as DependencyObject;
            }

            // Return the last visible UIElement(3D) or IContentHost
            DependencyObject uiElement = e as UIElement;
            if (uiElement == null)
            {
                uiElement = e as UIElement3D;
            }

            if (uiElement == null || UIElementHelper.IsVisible(uiElement))
            {
                DependencyObject elementAsVisual = e as Visual;
                if (elementAsVisual == null)
                {
                    elementAsVisual = e as Visual3D;
                }

                if (elementAsVisual != null)
                {
                    int count = VisualTreeHelper.GetChildrenCount(elementAsVisual);
                    for (int i = count - 1; i >= 0; i--)
                    {
                        DependencyObject visual = VisualTreeHelper.GetChild(elementAsVisual, i);
                        if (IsInNavigationTree(visual))
                            return visual;
                        else
                        {
                            DependencyObject lastChild = GetLastChild(visual);
                            if (lastChild != null)
                                return lastChild;
                        }
                    }
                }
            }

            return null;
        }

        internal static DependencyObject GetParent(DependencyObject e)
        {
            // For Visual - go up the visual parent chain until we find Visual, Visual3D or IContentHost
            if (e is Visual || e is Visual3D)
            {
                DependencyObject visual = e;

                while ((visual = VisualTreeHelper.GetParent(visual)) != null)
                {
                    // bug here related to movemement when you need to go from a child UI3D
                    // and you get a parent Viewport3D

                    if (IsInNavigationTree(visual))
                        return visual;
                }
            }
            else
            {
                // For ContentElement - return the host element (which is IContentHost)
                ContentElement contentElement = e as ContentElement;
                if (contentElement != null)
                {
                    return MS.Internal.Documents.ContentHostHelper.FindContentHost(contentElement) as DependencyObject;
                }
            }

            return null;
        }

        /***************************************************************************\
        *
        * GetNextInTree(DependencyObject e, DependencyObject container)
        * Search the subtree with container root; Don't go inside TabGroups
        *
        * Return the next Element in tree in depth order (self-child-sibling).
        *            1
        *           / \
        *          2   5
        *         / \
        *        3   4
        *
        \***************************************************************************/
        private DependencyObject GetNextInTree(DependencyObject e, DependencyObject container)
        {
            Debug.Assert(e != null, "e should not be null");
            Debug.Assert(container != null, "container should not be null");

            DependencyObject result = null;

            if (e == container || !IsGroup(e))
                result = GetFirstChild(e);

            if (result != null || e == container)
                return result;

            DependencyObject parent = e;
            do
            {
                DependencyObject sibling = GetNextSibling(parent);
                if (sibling != null)
                    return sibling;

                parent = GetParent(parent);
            } while (parent != null && parent != container);

            return null;
        }


        /***************************************************************************\
        *
        * GetPreviousInTree(DependencyObject e, DependencyObject container)
        * Don't go inside TabGroups
        * Return the previous Element in tree in depth order (self-child-sibling).
        *            5
        *           / \
        *          4   1
        *         / \
        *        3   2
        \***************************************************************************/
        private DependencyObject GetPreviousInTree(DependencyObject e, DependencyObject container)
        {
            if (e == container)
                return null;

            DependencyObject result = GetPreviousSibling(e);

            if (result != null)
            {
                if (IsGroup(result))
                    return result;
                else
                    return GetLastInTree(result);
            }
            else
                return GetParent(e);
        }

        // Find the last element in the subtree
        private DependencyObject GetLastInTree(DependencyObject container)
        {
            DependencyObject result;
            do
            {
                result = container;
                container = GetLastChild(container);
            } while (container != null && !IsGroup(container));

            if (container != null)
                return container;

            return result;
        }

        private DependencyObject GetGroupParent(DependencyObject e)
        {
            return GetGroupParent(e, false /*includeCurrent*/);
        }

        // Go up thru the parent chain until we find TabNavigation != Continue
        // In case all parents are Continue then return the root
        private DependencyObject GetGroupParent(DependencyObject e, bool includeCurrent)
        {
            Debug.Assert(e != null, "e cannot be null");

            DependencyObject result = e; // Keep the last non null element

            // If we don't want to include the current element,
            // start at the parent of the element.  If the element
            // is the root, then just return it as the group parent.
            if (!includeCurrent)
            {
                result = e;
                e = GetParent(e);
                if (e == null)
                {
                    return result;
                }
            }

            while (e != null)
            {
                if (IsGroup(e))
                    return e;

                result = e;
                e = GetParent(e);
            }

            return result;
        }

        #endregion Tree navigation

        #region Logical Navigation

        private bool IsTabStop(DependencyObject e)
        {
            FrameworkElement fe = e as FrameworkElement;
            if (fe != null)
                return
                    (fe.Focusable
                    && (bool)fe.GetValue(IsTabStopProperty))
                    && fe.IsEnabled
                    && fe.IsVisible;

            FrameworkContentElement fce = e as FrameworkContentElement;
            return fce != null && fce.Focusable && (bool)fce.GetValue(IsTabStopProperty) && fce.IsEnabled;
        }

        private bool IsGroup(DependencyObject e)
        {
            return GetKeyNavigationMode(e) != KeyboardNavigationMode.Continue;
        }

        internal bool IsFocusableInternal(DependencyObject element)
        {
            UIElement uie = element as UIElement;
            if (uie != null)
            {
                return (uie.Focusable && uie.IsEnabled && uie.IsVisible);
            }

            ContentElement ce = element as ContentElement;
            if (ce != null)
            {
                return (ce != null && ce.Focusable && ce.IsEnabled);
            }

            return false;
        }

        private bool IsElementEligible(DependencyObject element, bool treeViewNavigation)
        {
            if (treeViewNavigation)
            {
                return (element is TreeViewItem) && IsFocusableInternal(element);
            }
            else
            {
                return IsTabStop(element);
            }
        }

        private bool IsGroupElementEligible(DependencyObject element, bool treeViewNavigation)
        {
            if (treeViewNavigation)
            {
                return (element is TreeViewItem) && IsFocusableInternal(element);
            }
            else
            {
                return IsTabStopOrGroup(element);
            }
        }

        private KeyboardNavigationMode GetKeyNavigationMode(DependencyObject e)
        {
            return (KeyboardNavigationMode)e.GetValue(_navigationProperty);
        }

        private bool IsTabStopOrGroup(DependencyObject e)
        {
            return IsTabStop(e) || IsGroup(e);
        }

        private static int GetTabIndexHelper(DependencyObject d)
        {
            return (int)d.GetValue(TabIndexProperty);
        }

        #region Tab Navigation

        // Find the element with highest priority (lowest index) inside the group
        internal DependencyObject GetFirstTabInGroup(DependencyObject container)
        {
            DependencyObject firstTabElement = null;
            int minIndexFirstTab = Int32.MinValue;

            DependencyObject currElement = container;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (IsTabStopOrGroup(currElement))
                {
                    int currPriority = GetTabIndexHelper(currElement);

                    if (currPriority < minIndexFirstTab || firstTabElement == null)
                    {
                        minIndexFirstTab = currPriority;
                        firstTabElement = currElement;
                    }
                }
            }
            return firstTabElement;
        }

        // Find the element with the same TabIndex after the current element
        private DependencyObject GetNextTabWithSameIndex(DependencyObject e, DependencyObject container)
        {
            int elementTabPriority = GetTabIndexHelper(e);
            DependencyObject currElement = e;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (IsTabStopOrGroup(currElement) && GetTabIndexHelper(currElement) == elementTabPriority)
                {
                    return currElement;
                }
            }

            return null;
        }

        // Find the element with the next TabIndex after the current element
        private DependencyObject GetNextTabWithNextIndex(DependencyObject e, DependencyObject container, KeyboardNavigationMode tabbingType)
        {
            // Find the next min index in the tree
            // min (index>currentTabIndex)
            DependencyObject nextTabElement = null;
            DependencyObject firstTabElement = null;
            int minIndexFirstTab = Int32.MinValue;
            int minIndex = Int32.MinValue;
            int elementTabPriority = GetTabIndexHelper(e);

            DependencyObject currElement = container;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (IsTabStopOrGroup(currElement))
                {
                    int currPriority = GetTabIndexHelper(currElement);
                    if (currPriority > elementTabPriority)
                    {
                        if (currPriority < minIndex || nextTabElement == null)
                        {
                            minIndex = currPriority;
                            nextTabElement = currElement;
                        }
                    }

                    if (currPriority < minIndexFirstTab || firstTabElement == null)
                    {
                        minIndexFirstTab = currPriority;
                        firstTabElement = currElement;
                    }
                }
            }

            // Cycle groups: if not found - return first element
            if (tabbingType == KeyboardNavigationMode.Cycle && nextTabElement == null)
                nextTabElement = firstTabElement;

            return nextTabElement;
        }

        private DependencyObject GetNextTabInGroup(DependencyObject e, DependencyObject container, KeyboardNavigationMode tabbingType)
        {
            // None groups: Tab navigation is not supported
            if (tabbingType == KeyboardNavigationMode.None)
                return null;

            // e == null or e == container -> return the first TabStopOrGroup
            if (e == null || e == container)
            {
                return GetFirstTabInGroup(container);
            }

            if (tabbingType == KeyboardNavigationMode.Once)
                return null;

            DependencyObject nextTabElement = GetNextTabWithSameIndex(e, container);
            if (nextTabElement != null)
                return nextTabElement;

            return GetNextTabWithNextIndex(e, container, tabbingType);
        }

        private DependencyObject GetNextTab(DependencyObject e, DependencyObject container, bool goDownOnly)
        {
            Debug.Assert(container != null, "container should not be null");

            KeyboardNavigationMode tabbingType = GetKeyNavigationMode(container);

            if (e == null)
            {
                if (IsTabStop(container))
                    return container;

                // Using ActiveElement if set
                DependencyObject activeElement = GetActiveElement(container);
                if (activeElement != null)
                    return GetNextTab(null, activeElement, true);
            }
            else
            {
                if (tabbingType == KeyboardNavigationMode.Once || tabbingType == KeyboardNavigationMode.None)
                {
                    if (container != e)
                    {
                        if (goDownOnly)
                            return null;
                        DependencyObject parentContainer = GetGroupParent(container);
                        return GetNextTab(container, parentContainer, goDownOnly);
                    }
                }
            }

            // All groups
            DependencyObject loopStartElement = null;
            DependencyObject nextTabElement = e;
            KeyboardNavigationMode currentTabbingType = tabbingType;

            // Search down inside the container
            while ((nextTabElement = GetNextTabInGroup(nextTabElement, container, currentTabbingType)) != null)
            {
                Debug.Assert(IsTabStopOrGroup(nextTabElement), "nextTabElement should be IsTabStop or group");

                // Avoid the endless loop here for Cycle groups
                if (loopStartElement == nextTabElement)
                    break;
                if (loopStartElement == null)
                    loopStartElement = nextTabElement;

                DependencyObject firstTabElementInside = GetNextTab(null, nextTabElement, true);
                if (firstTabElementInside != null)
                    return firstTabElementInside;

                // If we want to continue searching inside the Once groups, we should change the navigation mode
                if (currentTabbingType == KeyboardNavigationMode.Once)
                    currentTabbingType = KeyboardNavigationMode.Contained;
            }

            // If there is no next element in the group (nextTabElement == null)

            // Search up in the tree if allowed
            // consider: Use original tabbingType instead of currentTabbingType
            if (!goDownOnly && currentTabbingType != KeyboardNavigationMode.Contained && GetParent(container) != null)
            {
                return GetNextTab(container, GetGroupParent(container), false);
            }

            return null;
        }

        #endregion Tab Navigation

        #region Shift+Tab Navigation

        internal DependencyObject GetLastTabInGroup(DependencyObject container)
        {
            DependencyObject lastTabElement = null;
            int maxIndexFirstTab = Int32.MaxValue;
            DependencyObject currElement = GetLastInTree(container);
            while (currElement != null && currElement != container)
            {
                if (IsTabStopOrGroup(currElement))
                {
                    int currPriority = GetTabIndexHelper(currElement);

                    if (currPriority > maxIndexFirstTab || lastTabElement == null)
                    {
                        maxIndexFirstTab = currPriority;
                        lastTabElement = currElement;
                    }
                }
                currElement = GetPreviousInTree(currElement, container);
            }
            return lastTabElement;
        }

        // Look for element with the same TabIndex before the current element
        private DependencyObject GetPrevTabWithSameIndex(DependencyObject e, DependencyObject container)
        {
            int elementTabPriority = GetTabIndexHelper(e);
            DependencyObject currElement = GetPreviousInTree(e, container);
            while (currElement != null)
            {
                if (IsTabStopOrGroup(currElement) && GetTabIndexHelper(currElement) == elementTabPriority && currElement != container)
                {
                    return currElement;
                }
                currElement = GetPreviousInTree(currElement, container);
            }
            return null;
        }

        private DependencyObject GetPrevTabWithPrevIndex(DependencyObject e, DependencyObject container, KeyboardNavigationMode tabbingType)
        {
            // Find the next max index in the tree
            // max (index<currentTabIndex)
            DependencyObject lastTabElement = null;
            DependencyObject nextTabElement = null;
            int elementTabPriority = GetTabIndexHelper(e);
            int maxIndexFirstTab = Int32.MaxValue;
            int maxIndex = Int32.MaxValue;
            DependencyObject currElement = GetLastInTree(container);
            while (currElement != null)
            {
                if (IsTabStopOrGroup(currElement) && currElement != container)
                {
                    int currPriority = GetTabIndexHelper(currElement);
                    if (currPriority < elementTabPriority)
                    {
                        if (currPriority > maxIndex || nextTabElement == null)
                        {
                            maxIndex = currPriority;
                            nextTabElement = currElement;
                        }
                    }

                    if (currPriority > maxIndexFirstTab || lastTabElement == null)
                    {
                        maxIndexFirstTab = currPriority;
                        lastTabElement = currElement;
                    }
                }

                currElement = GetPreviousInTree(currElement, container);
            }

            // Cycle groups: if not found - return first element
            if (tabbingType == KeyboardNavigationMode.Cycle && nextTabElement == null)
                nextTabElement = lastTabElement;

            return nextTabElement;
        }

        private DependencyObject GetPrevTabInGroup(DependencyObject e, DependencyObject container, KeyboardNavigationMode tabbingType)
        {
            // None groups: Tab navigation is not supported
            if (tabbingType == KeyboardNavigationMode.None)
                return null;

            // Search the last index inside the group
            if (e==null)
            {
                return GetLastTabInGroup(container);
            }

            if (tabbingType == KeyboardNavigationMode.Once)
                return null;

            if (e == container)
                return null;

            DependencyObject nextTabElement = GetPrevTabWithSameIndex(e, container);
            if (nextTabElement != null)
                return nextTabElement;

            return GetPrevTabWithPrevIndex(e, container, tabbingType);
        }

        private DependencyObject GetPrevTab(DependencyObject e, DependencyObject container, bool goDownOnly)
        {
            Debug.Assert(e != null || container != null, "e or container should not be null");

            if (container == null)
                container = GetGroupParent(e);

            KeyboardNavigationMode tabbingType = GetKeyNavigationMode(container);

            if (e == null)
            {
                // Using ActiveElement if set
                DependencyObject activeElement = GetActiveElement(container);
                if (activeElement != null)
                    return GetPrevTab(null, activeElement, true);
                else
                {
                    // If we Shift+Tab on a container with KeyboardNavigationMode=Once, and ActiveElement is null
                    // then we want to go to the fist item (not last) within the container
                    if (tabbingType == KeyboardNavigationMode.Once)
                    {
                        DependencyObject firstTabElement = GetNextTabInGroup(null, container, tabbingType);
                        if (firstTabElement == null)
                        {
                            if (IsTabStop(container))
                                return container;
                            if (goDownOnly)
                                return null;

                            return GetPrevTab(container, null, false);
                        }
                        else
                        {
                            return GetPrevTab(null, firstTabElement, true);
                        }
                    }
                }
            }
            else
            {
                if (tabbingType == KeyboardNavigationMode.Once || tabbingType == KeyboardNavigationMode.None)
                {
                    if (goDownOnly || container==e)
                        return null;

                    // FocusedElement should not be e otherwise we will delegate focus to the same element
                    if (IsTabStop(container))
                        return container;

                    return GetPrevTab(container, null, false);
                }
            }

            // All groups (except Once) - continue
            DependencyObject loopStartElement = null;
            DependencyObject nextTabElement = e;

            // Look for element with the same TabIndex before the current element
            while ((nextTabElement = GetPrevTabInGroup(nextTabElement, container, tabbingType)) != null)
            {
                if (nextTabElement == container && tabbingType == KeyboardNavigationMode.Local)
                    break;

                // At this point nextTabElement is TabStop or TabGroup
                // In case it is a TabStop only return the element
                if (IsTabStop(nextTabElement) && !IsGroup(nextTabElement))
                    return nextTabElement;

                // Avoid the endless loop here
                if (loopStartElement == nextTabElement)
                    break;
                if (loopStartElement == null)
                    loopStartElement = nextTabElement;

                // At this point nextTabElement is TabGroup
                DependencyObject lastTabElementInside = GetPrevTab(null, nextTabElement, true);
                if (lastTabElementInside != null)
                    return lastTabElementInside;
            }

            if (tabbingType == KeyboardNavigationMode.Contained)
                return null;

            if (e != container && IsTabStop(container))
                return container;

            // If end of the subtree is reached or there no other elements above
            if (!goDownOnly && GetParent(container) != null)
            {
                return GetPrevTab(container, null, false);
            }

            return null;
        }

        #endregion Shift+Tab Navigation

        #endregion Logical Navigation

        #region Directional Navigation

        /// <summary>
        ///     Returns the element rectange relative to the root.
        ///     Also calls UpdateLayout if the layout of element is
        ///     not valid.
        /// </summary>
        internal static Rect GetRectangle(DependencyObject element)
        {
            UIElement uiElement = element as UIElement;
            if (uiElement != null)
            {
                if (!uiElement.IsArrangeValid)
                {
                    // Call UpdateLayout if qualifies.
                    uiElement.UpdateLayout();
                }

                Visual rootVisual = GetVisualRoot(uiElement);

                if (rootVisual != null)
                {
                    GeneralTransform transform = uiElement.TransformToAncestor(rootVisual);
                    Thickness deflateThickness = (Thickness)uiElement.GetValue(DirectionalNavigationMarginProperty);
                    double x = -deflateThickness.Left;
                    double y = -deflateThickness.Top;
                    double width = uiElement.RenderSize.Width + deflateThickness.Left + deflateThickness.Right;
                    double height = uiElement.RenderSize.Height + deflateThickness.Top + deflateThickness.Bottom;
                    if (width < 0)
                    {
                        x = uiElement.RenderSize.Width * 0.5;
                        width = 0d;
                    }
                    if (height < 0)
                    {
                        y = uiElement.RenderSize.Height * 0.5;
                        height = 0d;
                    }
                    return transform.TransformBounds(new Rect(x, y, width, height));
                }
            }
            else
            {
                ContentElement ce = element as ContentElement;
                if (ce != null)
                {
                    IContentHost parentICH = null;
                    UIElement parentUIElement = GetParentUIElementFromContentElement(ce, ref parentICH);
                    Visual parent = parentICH as Visual;
                    if (parentICH != null && parent != null && parentUIElement != null)
                    {
                        Visual rootVisual = GetVisualRoot(parent);
                        if (rootVisual != null)
                        {
                            if (!parentUIElement.IsMeasureValid)
                            {
                                // Call UpdateLayout if qualifies.
                                parentUIElement.UpdateLayout();
                            }

                            // Note: Here we consider only the fist rectangle
                            // Do we need to consider all of them as one combined rectangle?
                            ReadOnlyCollection<Rect> rects = parentICH.GetRectangles(ce);
                            IEnumerator<Rect> enumerator = rects.GetEnumerator();
                            if (enumerator.MoveNext())
                            {
                                GeneralTransform transform = parent.TransformToAncestor(rootVisual);
                                Rect rect = enumerator.Current;
                                return transform.TransformBounds(rect);
                            }
                        }
                    }
                }
                else
                {
                    UIElement3D uiElement3D = element as UIElement3D;
                    if (uiElement3D != null)
                    {
                        Visual rootVisual = GetVisualRoot(uiElement3D);
                        Visual containingVisual2D = VisualTreeHelper.GetContainingVisual2D(uiElement3D);

                        if (rootVisual != null && containingVisual2D != null)
                        {
                            Rect rectElement = uiElement3D.Visual2DContentBounds;
                            GeneralTransform transform = containingVisual2D.TransformToAncestor(rootVisual);

                            return transform.TransformBounds(rectElement);
                        }
                    }
                }
            }

            return Rect.Empty;
        }

        // return the rectangle representing the given element.  Usually this is
        // just GetRectangle(element).  But a TreeViewItem surrounds its children,
        // which produces wrong results.  Instead use a rectangle that excludes
        // the children in the vertical direction, and extends as much as the
        // TreeViewItem in the horizontal direction.
        private Rect GetRepresentativeRectangle(DependencyObject element)
        {
            Rect rect = GetRectangle(element);
            TreeViewItem tvi = element as TreeViewItem;
            if (tvi != null)
            {
                Panel itemsHost = tvi.ItemsHost;
                if (itemsHost != null && itemsHost.IsVisible)
                {
                    Rect itemsHostRect = GetRectangle(itemsHost);
                    if (itemsHostRect != Rect.Empty)
                    {
                        bool? placeBeforeChildren = null;

                        // if there's a header, put the representative Rect on
                        // the same side of the children as the header.
                        FrameworkElement header = tvi.TryGetHeaderElement();
                        if (header != null && header != tvi && header.IsVisible)
                        {
                            Rect headerRect = GetRectangle(header);
                            if (!headerRect.IsEmpty)
                            {
                                if (DoubleUtil.LessThan(headerRect.Top, itemsHostRect.Top))
                                {
                                    // header starts before children - put rect before
                                    // (this is the most common case, used by the default layout)
                                    placeBeforeChildren = true;
                                }
                                else if (DoubleUtil.GreaterThan(headerRect.Bottom, itemsHostRect.Bottom))
                                {
                                    // header ends after children - put rect after
                                    placeBeforeChildren = false;
                                }
                            }
                        }

                        double before = itemsHostRect.Top - rect.Top;
                        double after = rect.Bottom - itemsHostRect.Bottom;

                        // If there is no header, or if the header doesn't extend
                        // past the children, put the representative Rect on
                        // whichever side of the children has more room.
                        // This handles the case where the TVI's template has
                        // content that's not explicitly marked as "header".
                        if (placeBeforeChildren == null)
                        {
                            placeBeforeChildren = DoubleUtil.GreaterThanOrClose(before, after);
                        }

                        // adjust the rect according to the placement computed above.
                        // Ensure that its height is non-negative, but no taller than the TVI.
                        if (placeBeforeChildren == true)
                        {
                            rect.Height = Math.Min(Math.Max(before, 0.0d), rect.Height);
                        }
                        else
                        {
                            double height = Math.Min(Math.Max(after, 0.0d), rect.Height);
                            rect.Y = rect.Bottom - height;
                            rect.Height = height;
                        }
                    }
                }
            }

            return rect;
        }

        // distance between two points
        private double GetDistance(Point p1, Point p2)
        {
            double deltaX = p1.X - p2.X;
            double deltaY = p1.Y - p2.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        private double GetPerpDistance(Rect sourceRect, Rect targetRect, FocusNavigationDirection direction)
        {
            switch (direction)
            {
                case FocusNavigationDirection.Right :
                    return targetRect.Left - sourceRect.Left;

                case FocusNavigationDirection.Left :
                    return sourceRect.Right - targetRect.Right;

                case FocusNavigationDirection.Up :
                    return sourceRect.Bottom - targetRect.Bottom;

                case FocusNavigationDirection.Down :
                    return targetRect.Top - sourceRect.Top;

                default :
                    throw new System.ComponentModel.InvalidEnumArgumentException("direction", (int)direction, typeof(FocusNavigationDirection));
            }
        }

        // Example when moving down:
        // distance between sourceRect.TopLeft (or Y=vertical baseline)
        // and targetRect.TopLeft
        private double GetDistance(Rect sourceRect, Rect targetRect, FocusNavigationDirection direction)
        {
            Point startPoint;
            Point endPoint;
            switch (direction)
            {
                case FocusNavigationDirection.Right :
                    startPoint = sourceRect.TopLeft;
                    if (_horizontalBaseline != BASELINE_DEFAULT)
                        startPoint.Y = _horizontalBaseline;
                    endPoint = targetRect.TopLeft;
                    break;

                case FocusNavigationDirection.Left :
                    startPoint = sourceRect.TopRight;
                    if (_horizontalBaseline != BASELINE_DEFAULT)
                        startPoint.Y = _horizontalBaseline;
                    endPoint = targetRect.TopRight;
                    break;

                case FocusNavigationDirection.Up :
                    startPoint = sourceRect.BottomLeft;
                    if (_verticalBaseline != BASELINE_DEFAULT)
                        startPoint.X = _verticalBaseline;
                    endPoint = targetRect.BottomLeft;
                    break;

                case FocusNavigationDirection.Down :
                    startPoint = sourceRect.TopLeft;
                    if (_verticalBaseline != BASELINE_DEFAULT)
                        startPoint.X = _verticalBaseline;
                    endPoint = targetRect.TopLeft;
                    break;

                default :
                    throw new System.ComponentModel.InvalidEnumArgumentException("direction", (int)direction, typeof(FocusNavigationDirection));
            }
            return GetDistance(startPoint, endPoint);
        }

        // Example when moving down:
        // true if the top of the toRect is below the bottom of fromRect
        private bool IsInDirection(Rect fromRect, Rect toRect, FocusNavigationDirection direction)
        {
            switch (direction)
            {
                case FocusNavigationDirection.Right:
                    return DoubleUtil.LessThanOrClose(fromRect.Right, toRect.Left);
                case FocusNavigationDirection.Left:
                    return DoubleUtil.GreaterThanOrClose(fromRect.Left, toRect.Right);
                case FocusNavigationDirection.Up :
                    return DoubleUtil.GreaterThanOrClose(fromRect.Top, toRect.Bottom);
                case FocusNavigationDirection.Down :
                    return DoubleUtil.LessThanOrClose(fromRect.Bottom, toRect.Top);
                default:
                    throw new System.ComponentModel.InvalidEnumArgumentException("direction", (int)direction, typeof(FocusNavigationDirection));
            }
        }

        // The element is focus scope if IsFocusScope is true or it is the visual tree root
        private bool IsFocusScope(DependencyObject e)
        {
            return FocusManager.GetIsFocusScope(e) || GetParent(e) == null;
        }

        private bool IsAncestorOf(DependencyObject sourceElement, DependencyObject targetElement)
        {
            Visual sourceVisual = sourceElement as Visual;
            Visual targetVisual = targetElement as Visual;
            if (sourceVisual == null || targetVisual == null)
                return false;

            return sourceVisual.IsAncestorOf(targetVisual);
        }

        // this is like the previous method, except it works when targetElement is
        // a ContentElement (e.g. Hyperlink).  "Works" means it gives results consistent
        // with the tree navigation methods GetParent, Get*Child, Get*Sibling.
        // [It might be correct to have only one method - this one - but we need
        // to keep the existing calls to the previous method around for compat.]
        internal bool IsAncestorOfEx(DependencyObject sourceElement, DependencyObject targetElement)
        {
            Debug.Assert(sourceElement != null, "sourceElement must not be null");

            while (targetElement != null && targetElement != sourceElement)
            {
                targetElement = GetParent(targetElement);
            }

            return (targetElement == sourceElement);
        }

        // Example: When moving down:
        // Range is the sourceRect width extended to the vertical baseline
        // targetRect.Top > sourceRect.Top (target is below the source)
        // targetRect.Right > sourceRect.Left || targetRect.Left < sourceRect.Right
        private bool IsInRange(DependencyObject sourceElement, DependencyObject targetElement, Rect sourceRect, Rect targetRect, FocusNavigationDirection direction, double startRange, double endRange)
        {
            switch (direction)
            {
                case FocusNavigationDirection.Right :
                case FocusNavigationDirection.Left :
                    if (_horizontalBaseline != BASELINE_DEFAULT)
                    {
                        startRange = Math.Min(startRange, _horizontalBaseline);
                        endRange = Math.Max(endRange, _horizontalBaseline);
                    }

                    if (DoubleUtil.GreaterThan(targetRect.Bottom, startRange) && DoubleUtil.LessThan(targetRect.Top, endRange))
                    {
                        // If there is no sourceElement - checking the range is enough
                        if (sourceElement == null)
                            return true;

                        if (direction == FocusNavigationDirection.Right)
                            return DoubleUtil.GreaterThan(targetRect.Left, sourceRect.Left) || (DoubleUtil.AreClose(targetRect.Left, sourceRect.Left) && IsAncestorOfEx(sourceElement, targetElement));
                        else
                            return DoubleUtil.LessThan(targetRect.Right, sourceRect.Right) || (DoubleUtil.AreClose(targetRect.Right, sourceRect.Right) && IsAncestorOfEx(sourceElement, targetElement));
                    }
                    break;

                case FocusNavigationDirection.Up :
                case FocusNavigationDirection.Down :
                    if (_verticalBaseline != BASELINE_DEFAULT)
                    {
                        startRange = Math.Min(startRange, _verticalBaseline);
                        endRange = Math.Max(endRange, _verticalBaseline);
                    }

                    if (DoubleUtil.GreaterThan(targetRect.Right, startRange) && DoubleUtil.LessThan(targetRect.Left, endRange))
                    {
                        // If there is no sourceElement - checking the range is enough
                        if (sourceElement == null)
                            return true;

                        if (direction == FocusNavigationDirection.Down)
                            return DoubleUtil.GreaterThan(targetRect.Top, sourceRect.Top) || (DoubleUtil.AreClose (targetRect.Top, sourceRect.Top) && IsAncestorOfEx(sourceElement, targetElement));
                        else
                            return DoubleUtil.LessThan(targetRect.Bottom, sourceRect.Bottom) || (DoubleUtil.AreClose(targetRect.Bottom, sourceRect.Bottom) && IsAncestorOfEx(sourceElement, targetElement));
                    }
                    break;

                default :
                    throw new System.ComponentModel.InvalidEnumArgumentException("direction", (int)direction, typeof(FocusNavigationDirection));
            }

            return false;
        }

        private DependencyObject GetNextInDirection(DependencyObject sourceElement, FocusNavigationDirection direction)
        {
            return GetNextInDirection(sourceElement, direction, /*treeViewNavigation*/ false);
        }

        private DependencyObject GetNextInDirection(DependencyObject sourceElement, FocusNavigationDirection direction, bool treeViewNavigation)
        {
            return GetNextInDirection(sourceElement, direction, treeViewNavigation, considerDescendants:true);
        }

        private DependencyObject GetNextInDirection(DependencyObject sourceElement,
            FocusNavigationDirection direction,
            bool treeViewNavigation,
            bool considerDescendants)
        {
            _containerHashtable.Clear();
            DependencyObject targetElement = MoveNext(sourceElement, null, direction, BASELINE_DEFAULT, BASELINE_DEFAULT, treeViewNavigation, considerDescendants);

            if (targetElement != null)
            {
                UIElement sourceUIElement = sourceElement as UIElement;
                if (sourceUIElement != null)
                    sourceUIElement.RemoveHandler(Keyboard.PreviewLostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(_LostFocus));
                else
                {
                    ContentElement sourceContentElement = sourceElement as ContentElement;
                    if (sourceContentElement != null)
                        sourceContentElement.RemoveHandler(Keyboard.PreviewLostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(_LostFocus));
                }

                UIElement targetUIElement = targetElement as UIElement;
                if (targetUIElement == null)
                    targetUIElement = GetParentUIElementFromContentElement(targetElement as ContentElement);
                else
                {
                    ContentElement targetContentElement = targetElement as ContentElement;
                    if (targetContentElement != null)
                    {
                        // When Focus is changed we need to reset the base line
                        targetContentElement.AddHandler(Keyboard.PreviewLostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(_LostFocus), true);
                    }
                }

                if (targetUIElement != null)
                {
                    // When layout is changed we need to reset the base line
                    // Set up a layout invalidation listener.
                    targetUIElement.LayoutUpdated += new EventHandler(OnLayoutUpdated);

                    // When Focus is changed we need to reset the base line
                    if (targetElement == targetUIElement)
                        targetUIElement.AddHandler(Keyboard.PreviewLostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(_LostFocus), true);
                }
            }

            _containerHashtable.Clear();
            return targetElement;
        }

        // LayoutUpdated handler.
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            UIElement uiElement = sender as UIElement;
            // Disconnect the layout listener.
            if (uiElement != null)
            {
                uiElement.LayoutUpdated -= new EventHandler(OnLayoutUpdated);
            }

            _verticalBaseline = BASELINE_DEFAULT;
            _horizontalBaseline = BASELINE_DEFAULT;
        }

        private void _LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _verticalBaseline = BASELINE_DEFAULT;
            _horizontalBaseline = BASELINE_DEFAULT;

            if (sender is UIElement)
                ((UIElement)sender).RemoveHandler(Keyboard.PreviewLostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(_LostFocus));
            else if (sender is ContentElement)
                ((ContentElement)sender).RemoveHandler(Keyboard.PreviewLostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(_LostFocus));
        }

        private bool IsEndlessLoop(DependencyObject element, DependencyObject container)
        {
            object elementObject = element != null ? (object)element : _fakeNull;

            // If entry exists then we have endless loop
            Hashtable elementTable = _containerHashtable[container] as Hashtable;
            if (elementTable != null)
            {
                if (elementTable[elementObject] != null)
                    return true;
            }
            else
            {
                // Adding the entry to the collection
                elementTable = new Hashtable(10);
                _containerHashtable[container] = elementTable;
            }

            elementTable[elementObject] = BooleanBoxes.TrueBox;
            return false;
        }

        private void ResetBaseLines(double value, bool horizontalDirection)
        {
            if (horizontalDirection)
            {
                _verticalBaseline = BASELINE_DEFAULT;
                if (_horizontalBaseline == BASELINE_DEFAULT)
                    _horizontalBaseline = value;
            }
            else // vertical direction
            {
                _horizontalBaseline = BASELINE_DEFAULT;
                if (_verticalBaseline == BASELINE_DEFAULT)
                    _verticalBaseline = value;
            }
        }

        private DependencyObject FindNextInDirection(DependencyObject sourceElement,
            Rect sourceRect,
            DependencyObject container,
            FocusNavigationDirection direction,
            double startRange,
            double endRange,
            bool treeViewNavigation,
            bool considerDescendants)   // when false, descendants of sourceElement are not candidates
        {
            DependencyObject result = null;
            Rect resultRect = Rect.Empty;
            double resultScore = 0d;
            bool searchInsideContainer = sourceElement == null;
            DependencyObject currElement = container;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (currElement != sourceElement &&
                    IsGroupElementEligible(currElement, treeViewNavigation))
                {
                    Rect currentRect = GetRepresentativeRectangle(currElement);

                    // Consider the current element as a result candidate only if its layout is valid.
                    if (currentRect != Rect.Empty)
                    {
                        bool isInDirection = IsInDirection(sourceRect, currentRect, direction);
                        bool isInRange = IsInRange(sourceElement, currElement, sourceRect, currentRect, direction, startRange, endRange);
                        if (searchInsideContainer || isInDirection || isInRange)
                        {
                            double score = isInRange ? GetPerpDistance(sourceRect, currentRect, direction) : GetDistance(sourceRect, currentRect, direction);

                            if (double.IsNaN(score))
                            {
                                continue;
                            }
                            // Keep the first element in the result
                            if (result == null &&
                                (considerDescendants || !IsAncestorOfEx(sourceElement, currElement)))
                            {
                                result = currElement;
                                resultRect = currentRect;
                                resultScore = score;
                            }
                            else if ((DoubleUtil.LessThan(score, resultScore) || (DoubleUtil.AreClose(score, resultScore) && GetDistance(sourceRect, resultRect, direction) > GetDistance(sourceRect, currentRect, direction)))
                                && (considerDescendants || !IsAncestorOfEx(sourceElement, currElement)))
                            {
                                result = currElement;
                                resultRect = currentRect;
                                resultScore = score;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private DependencyObject MoveNext(DependencyObject sourceElement,
            DependencyObject container,
            FocusNavigationDirection direction,
            double startRange,
            double endRange,
            bool treeViewNavigation,
            bool considerDescendants)
        {
            Debug.Assert(!(sourceElement == null && container == null), "Both sourceElement and container cannot be null");

            if (container == null)
            {
                container = GetGroupParent(sourceElement);
                Debug.Assert(container != null, "container cannot be null");
            }

            // If we get to the tree root, return null
            if (container == sourceElement)
                return null;

            if (IsEndlessLoop(sourceElement, container))
                return null;

            KeyboardNavigationMode mode = GetKeyNavigationMode(container);
            bool searchInsideContainer = (sourceElement == null);

            // Don't navigate inside None containers
            if (mode == KeyboardNavigationMode.None && searchInsideContainer)
                return null;

            Rect sourceRect = searchInsideContainer ? GetRectangle(container)
                                : GetRepresentativeRectangle(sourceElement);
            bool horizontalDirection = direction == FocusNavigationDirection.Right || direction == FocusNavigationDirection.Left;

            // Reset the baseline when we change the direction
            ResetBaseLines(horizontalDirection ? sourceRect.Top : sourceRect.Left, horizontalDirection);

            // If range is not set - use source rect
            if (startRange == BASELINE_DEFAULT || endRange == BASELINE_DEFAULT)
            {
                startRange = horizontalDirection ? sourceRect.Top : sourceRect.Left;
                endRange = horizontalDirection ? sourceRect.Bottom : sourceRect.Right;
            }

            // Navigate outside the container
            if (mode == KeyboardNavigationMode.Once && !searchInsideContainer)
                return MoveNext(container, null, direction, startRange, endRange, treeViewNavigation, considerDescendants:true);

            DependencyObject result = FindNextInDirection(sourceElement, sourceRect, container, direction, startRange, endRange, treeViewNavigation, considerDescendants);

            // If there is no next element in current container
            if (result == null)
            {
                switch (mode)
                {
                    case KeyboardNavigationMode.Cycle:
                        return MoveNext(null, container, direction, startRange, endRange, treeViewNavigation, considerDescendants:true);

                    case KeyboardNavigationMode.Contained:
                        return null;

                    default: // Continue, Once, None, Local - search outside the container
                        return MoveNext(container, null, direction, startRange, endRange, treeViewNavigation, considerDescendants:true);
                }
            }

            if (IsElementEligible(result, treeViewNavigation))
                return result;

            // Using ActiveElement if set
            DependencyObject activeElement = GetActiveElementChain(result, treeViewNavigation);
            if (activeElement != null)
                return activeElement;

            // Try to find focus inside the element
            // result is not TabStop, which means it is a group
            DependencyObject insideElement = MoveNext(null, result, direction, startRange, endRange, treeViewNavigation, considerDescendants:true);
            if (insideElement != null)
                return insideElement;

            return MoveNext(result, null, direction, startRange, endRange, treeViewNavigation, considerDescendants:true);
        }

        private DependencyObject GetActiveElementChain(DependencyObject element, bool treeViewNavigation)
        {
            DependencyObject validActiveElement = null;
            DependencyObject activeElement = element;
            while ((activeElement = GetActiveElement(activeElement)) != null)
            {
                if (IsElementEligible(activeElement, treeViewNavigation))
                    validActiveElement = activeElement;
            }

            return validActiveElement;
        }

        #endregion Directional Navigation

        #region Page Navigation

        private DependencyObject FindElementAtViewportEdge(
            DependencyObject sourceElement,
            FrameworkElement viewportBoundsElement,
            DependencyObject container,
            FocusNavigationDirection direction,
            bool treeViewNavigation)
        {
            Rect sourceRect = new Rect(0, 0, 0, 0);
            if (sourceElement != null)
            {
                // Find sourceElement's position wrt viewport.
                ElementViewportPosition sourceElementPosition = ItemsControl.GetElementViewportPosition(viewportBoundsElement,
                    ItemsControl.TryGetTreeViewItemHeader(sourceElement) as UIElement,
                    direction,
                    false /*fullyVisible*/,
                    out sourceRect);
                if (sourceElementPosition == ElementViewportPosition.None)
                {
                    sourceRect = new Rect(0, 0, 0, 0);
                }
            }

            DependencyObject result = null;
            double resultDirectionScore = double.NegativeInfinity;
            double resultRangeScore = double.NegativeInfinity;

            DependencyObject partialResult = null;
            double partialResultDirectionScore = double.NegativeInfinity;
            double partialResultRangeScore = double.NegativeInfinity;

            DependencyObject currElement = container;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (IsGroupElementEligible(currElement, treeViewNavigation))
                {
                    DependencyObject currentRectElement = currElement;
                    if (treeViewNavigation)
                    {
                        currentRectElement = ItemsControl.TryGetTreeViewItemHeader(currElement);
                    }

                    Rect currentRect;
                    ElementViewportPosition currentViewportPosition = ItemsControl.GetElementViewportPosition(
                        viewportBoundsElement,
                        currentRectElement as UIElement,
                        direction,
                        false /*fullyVisible*/,
                        out currentRect);

                    // Compute directionScore of the current element. Higher the
                    // directionScore more suitable the element is.
                    if (currentViewportPosition == ElementViewportPosition.CompletelyInViewport ||
                        currentViewportPosition == ElementViewportPosition.PartiallyInViewport)
                    {
                        double directionScore = double.NegativeInfinity;
                        switch (direction)
                        {
                            case FocusNavigationDirection.Up:
                                directionScore = -currentRect.Top;
                                break;
                            case FocusNavigationDirection.Down:
                                directionScore = currentRect.Bottom;
                                break;
                            case FocusNavigationDirection.Left:
                                directionScore = -currentRect.Left;
                                break;
                            case FocusNavigationDirection.Right:
                                directionScore = currentRect.Right;
                                break;
                        }

                        // Compute the rangeScore of the current element with respect to
                        // the starting element. When directionScores of two elements match,
                        // rangeScore is used to resolve the conflict. The one with higher
                        // rangeScore gets chosen.
                        double rangeScore = double.NegativeInfinity;
                        switch (direction)
                        {
                            case FocusNavigationDirection.Up:
                            case FocusNavigationDirection.Down:
                                rangeScore = ComputeRangeScore(sourceRect.Left, sourceRect.Right, currentRect.Left, currentRect.Right);
                                break;
                            case FocusNavigationDirection.Left:
                            case FocusNavigationDirection.Right:
                                rangeScore = ComputeRangeScore(sourceRect.Top, sourceRect.Bottom, currentRect.Top, currentRect.Bottom);
                                break;
                        }

                        if (currentViewportPosition == ElementViewportPosition.CompletelyInViewport)
                        {
                            if (result == null ||
                                DoubleUtil.GreaterThan(directionScore, resultDirectionScore) ||
                                (DoubleUtil.AreClose(directionScore, resultDirectionScore) && DoubleUtil.GreaterThan(rangeScore, resultRangeScore)))
                            {
                                result = currElement;
                                resultDirectionScore = directionScore;
                                resultRangeScore = rangeScore;
                            }
                        }
                        else // currentViewportPosition == ElementViewportPosition.PartiallyInViewport
                        {
                            if (partialResult == null ||
                                DoubleUtil.GreaterThan(directionScore, partialResultDirectionScore) ||
                                (DoubleUtil.AreClose(directionScore, partialResultDirectionScore) && DoubleUtil.GreaterThan(rangeScore, partialResultRangeScore)))
                            {
                                partialResult = currElement;
                                partialResultDirectionScore = directionScore;
                                partialResultRangeScore = rangeScore;
                            }
                        }
                    }
                }
            }
            return result != null ? result : partialResult;
        }

        /// <summary>
        /// Computes RangeScore which reflects the closeness of two
        /// position ranges.
        /// </summary>
        private double ComputeRangeScore(double rangeStart1,
            double rangeEnd1,
            double rangeStart2,
            double rangeEnd2)
        {
            Debug.Assert(DoubleUtil.GreaterThanOrClose(rangeEnd1, rangeStart1));
            Debug.Assert(DoubleUtil.GreaterThanOrClose(rangeEnd2, rangeStart2));

            // Ensure that rangeStart1 <= rangeStart2
            if (DoubleUtil.GreaterThan(rangeStart1, rangeStart2))
            {
                double tempValue = rangeStart1;
                rangeStart1 = rangeStart2;
                rangeStart2 = tempValue;
                tempValue = rangeEnd1;
                rangeEnd1 = rangeEnd2;
                rangeEnd2 = tempValue;
            }

            if (DoubleUtil.LessThan(rangeEnd1, rangeEnd2))
            {
                // Computes rangeScore for scenarios where range1
                // does not completely include range2 in itself.
                // This includes cases where the candidate range is
                // partially or totally outside the source range.
                return (rangeEnd1 - rangeStart2);
            }
            else
            {
                // Computes rangesScore for scenarios where range1
                // completely includes range2 in itself.
                // This includes cases where either the candidate range is
                // completely within the source range or the source range
                // is completely within the candidate range.
                return (rangeEnd2 - rangeStart2);
            }
        }

        #endregion

        #region Global tracking for entering MenuMode

        // ISSUE: how do we deal with deactivate?

        /////////////////////////////////////////////////////////////////////
        private void ProcessForMenuMode(InputEventArgs inputEventArgs)
        {
            // When ALT or F10 key up happens we should fire the EnterMenuMode event.
            // We should not fire if:
            // * there were any handled input events in between the key down and corresponding key up.
            // * another unmatched keydown or keyup happened
            // * an unhandled mouse down/up happens

            if (inputEventArgs.RoutedEvent == Keyboard.LostKeyboardFocusEvent)
            {
                KeyboardFocusChangedEventArgs args = inputEventArgs as KeyboardFocusChangedEventArgs;
                if (((args != null) && (args.NewFocus == null)) || inputEventArgs.Handled)
                {
                    // Focus went to null, stop tracking the last key down
                    _lastKeyPressed = Key.None;
                }
            }
            // If a key is pressed down, remember it until the corresponding
            // key up.  Ignore repeated keydowns.
            else if (inputEventArgs.RoutedEvent == Keyboard.KeyDownEvent)
            {
                if (inputEventArgs.Handled)
                    _lastKeyPressed = Key.None;
                else
                {
                    KeyEventArgs keyEventArgs = inputEventArgs as KeyEventArgs;

                    if (!keyEventArgs.IsRepeat)
                    {
                        if (_lastKeyPressed == Key.None)
                        {
                            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Windows)) == ModifierKeys.None)
                            {
                                _lastKeyPressed = GetRealKey(keyEventArgs);
                            }
                        }
                        else
                        {
                            // Another key was pressed down in between the one that we're tracking, so reset.
                            _lastKeyPressed = Key.None;
                        }

                        // Clear this bit, Win32 will see message and clear QF_FMENUSTATUS.
                        _win32MenuModeWorkAround = false;
                    }
                }
            }
            // If a key up is received and matches the last key down
            // and is a key that would cause us to enter menumode,
            // raise the (internal) EnterMenuMode event.
            else if (inputEventArgs.RoutedEvent == Keyboard.KeyUpEvent)
            {
                if (!inputEventArgs.Handled)
                {
                    KeyEventArgs keyEventArgs = inputEventArgs as KeyEventArgs;
                    Key realKey = GetRealKey(keyEventArgs);

                    if (realKey == _lastKeyPressed && IsMenuKey(realKey))
                    {
                        EnableKeyboardCues(keyEventArgs.Source as DependencyObject, true);
                        keyEventArgs.Handled = OnEnterMenuMode(keyEventArgs.Source);
                    }

                    if (_win32MenuModeWorkAround)
                    {
                        if (IsMenuKey(realKey))
                        {
                            _win32MenuModeWorkAround = false;

                            // Mark the event args as handled so that Win32 never
                            // sees this key up and doesn't enter menu-mode.
                            keyEventArgs.Handled = true;
                        }
                    }
                    // If someone was listening for MenuMode and did something,
                    // we need to make sure we don't let Win32 enter menu mode.
                    else if (keyEventArgs.Handled)
                    {
                        // Set this bit to true, this means that we will handle
                        // the next ALT-up if no one else does.
                        _win32MenuModeWorkAround = true;
                    }
                }
                // No matter what we should reset and not track the last key anymore.
                _lastKeyPressed = Key.None;
            }
            // The following input events act to "cancel" the EnterMenuMode event
            else if (inputEventArgs.RoutedEvent == Mouse.MouseDownEvent
                  || inputEventArgs.RoutedEvent == Mouse.MouseUpEvent)
            {
                _lastKeyPressed = Key.None;

                // Win32 will see this message and will set QF_FMENUSTATUS to false.
                _win32MenuModeWorkAround = false;
            }
        }

        private bool IsMenuKey(Key key)
        {
            return (key == Key.LeftAlt || key == Key.RightAlt || key == Key.F10);
        }

        private Key GetRealKey(KeyEventArgs e)
        {
            return (e.Key == Key.System) ? e.SystemKey : e.Key;
        }

        private bool OnEnterMenuMode(object eventSource)
        {
            if (_weakEnterMenuModeHandlers == null)
                return false;

            lock (_weakEnterMenuModeHandlers)
            {
                if (_weakEnterMenuModeHandlers.Count == 0)
                {
                    return false;
                }

                // Bug 940610: no way to get PresentationSource of event in PostProcessInput
                // WORKAROUND: For now I will try to get the source of the event with
                //             PresentationSource.FromVisual.  If that fails, try to get the
                //             source of the active window.
                PresentationSource source = null;

                if (eventSource != null)
                {
                    Visual eventSourceVisual = eventSource as Visual;
                    source = (eventSourceVisual != null) ? PresentationSource.CriticalFromVisual(eventSourceVisual) : null;
                }
                else
                {
                    // If Keyboard.FocusedElement is null we'll have to fall back here.
                    IntPtr activeWindow = MS.Win32.UnsafeNativeMethods.GetActiveWindow();

                    if (activeWindow != IntPtr.Zero)
                    {
                        source = HwndSource.CriticalFromHwnd(activeWindow);
                    }
                }

                // Can't fire the event if the event didn't happen in any source
                if (source == null)
                {
                    return false;
                }

                EventArgs e = EventArgs.Empty;
                bool handled = false;

                _weakEnterMenuModeHandlers.Process(
                            delegate(object obj)
                            {
                                EnterMenuModeEventHandler currentHandler = obj as EnterMenuModeEventHandler;

                                if (currentHandler != null)
                                {
                                    if (currentHandler(source, e))
                                    {
                                        handled = true;
                                    }
                                }

                                return handled;
                            });

                return handled;
            }
        }

        /// <summary>
        ///     Called when ALT or F10 is pressed anywhere in the global scope
        /// </summary>
        internal event EnterMenuModeEventHandler EnterMenuMode
        {
            add
            {

                if (_weakEnterMenuModeHandlers == null)
                    _weakEnterMenuModeHandlers = new WeakReferenceList();

                lock (_weakEnterMenuModeHandlers)
                {
                    _weakEnterMenuModeHandlers.Add(value);
                }
            }
            remove
            {
                if (_weakEnterMenuModeHandlers != null)
                {
                    lock (_weakEnterMenuModeHandlers)
                    {
                        _weakEnterMenuModeHandlers.Remove(value);
                    }
                }
            }
        }

        internal delegate bool EnterMenuModeEventHandler(object sender, EventArgs e);

        // Used to track what the last key was pressed so that
        // we can fire the EnterMenuMode event.
        // Will be reset to Key.None when an unmatched KeyUp or other input event happens
        private Key _lastKeyPressed = Key.None;

        // List of WeakReferences to delegates to be invoked when EnterMenuMode happens
        private WeakReferenceList _weakEnterMenuModeHandlers;

        // Fix for bug 936302: (JevanSa)
        //     The DefaultWindowProcWorker (windows/core/ntuser/kernel/dwp.c)
        //     listens for ALT down followed by ALT up with nothing in between.
        //     When ALT goes down they set QF_FMENUSTATUS.  When ALT up happens,
        //     if QF_FMENUSTATUS is still set, they open the system menu (or
        //     menu for the window if there is one).  If any keystrokes happen
        //     in between, they clear QF_FMENUSTATUS.
        //
        //     Consider the following sequence:
        //       1) KeyDown(Alt) - neither Win32 nor Avalon respond
        //       2) KeyUp(Alt) - Avalon handles the event, Win32 is skipped
        //       3) KeyDown(Alt) - Avalon handles the event, Win32 is skipped
        //       4) KeyUp(Alt) - Avalon does not respond, Win32 handles the message
        //                       (and enters "Invisible" MenuMode)
        //
        //     Here, from the point of view of the DWP, there was just ALT down
        //     followed by ALT up.  We must fool the DWP somehow so that they
        //     clear clear the QF_FMENUSTATUS bit before #4.
        //
        //     Currently the best way DwayneN and I have come up with is to
        //     mark the event has handled in case #4 so that the DWP
        //     never sees the ALT up in #4.  We set this bit when #2 happens.
        //     If we see an unhandled ALT-up and this bit is set, we mark the
        //     event as handled.  If we see any unhandled key down or mouse up/down
        //     we can clear this bit.
        private bool _win32MenuModeWorkAround;

        #endregion

        #region UIState

        private void ProcessForUIState(InputEventArgs inputEventArgs)
        {
            PresentationSource source;
            RawUIStateInputReport report = ExtractRawUIStateInputReport(inputEventArgs, InputManager.InputReportEvent);

            if (report != null && (source = report.InputSource) != null)
            {
                // handle accelerator cue display
                if ((report.Targets & RawUIStateTargets.HideAccelerators) != 0)
                {
                    Visual root = source.RootVisual;
                    bool enable = (report.Action == RawUIStateActions.Clear);

                    EnableKeyboardCues(root, enable);
                }
            }
        }

        private RawUIStateInputReport ExtractRawUIStateInputReport(InputEventArgs e, RoutedEvent Event)
        {
            RawUIStateInputReport uiStateInputReport = null;
            InputReportEventArgs input = e as InputReportEventArgs;

            if (input != null)
            {
                if (input.Report.Type == InputType.Keyboard && input.RoutedEvent == Event)
                {
                    uiStateInputReport = input.Report as RawUIStateInputReport;
                }
            }

            return uiStateInputReport;
        }

        #endregion UIState

        #region FocusEnterMainFocusScope weak event

        // The event is raised when KeyboardFocus enters the main focus scope (visual tree root)
        // Selector and TreeView listen for this event to update their ActiveSelection property
        internal event EventHandler FocusEnterMainFocusScope
        {
            add
            {
                lock (_weakFocusEnterMainFocusScopeHandlers)
                {
                    _weakFocusEnterMainFocusScopeHandlers.Add(value);
                }
            }
            remove
            {
                lock (_weakFocusEnterMainFocusScopeHandlers)
                {
                    _weakFocusEnterMainFocusScopeHandlers.Remove(value);
                }
            }
        }

        private void NotifyFocusEnterMainFocusScope(object sender, EventArgs e)
        {
            _weakFocusEnterMainFocusScopeHandlers.Process(
                        delegate(object item)
                        {
                            EventHandler handler = item as EventHandler;
                            if (handler != null)
                            {
                                handler(sender, e);
                            }
                            return false;
                        } );
        }

        private WeakReferenceList _weakFocusEnterMainFocusScopeHandlers = new WeakReferenceList();
        #endregion

        #region Data

        private const double BASELINE_DEFAULT = Double.MinValue;
        private double _verticalBaseline = BASELINE_DEFAULT;
        private double _horizontalBaseline = BASELINE_DEFAULT;
        private DependencyProperty _navigationProperty = null;
        private Hashtable _containerHashtable = new Hashtable(10);
        private static object _fakeNull = new object();

        #endregion Data

        #region WeakReferenceList

        private class WeakReferenceList : DispatcherObject
        {
            public int Count { get { return _list.Count; } }

            // add a weak reference to the item
            public void Add(object item)
            {
                // before growing the list, purge it of dead entries.
                // The expense of purging amortizes to O(1) per entry, because
                // the the list doubles its capacity when it grows.
                if (_list.Count == _list.Capacity)
                {
                    Purge();
                }

                _list.Add(new WeakReference(item));
            }

            // remove all references to the target item
            public void Remove(object target)
            {
                bool hasDeadEntries = false;
                for (int i=0;  i<_list.Count;  ++i)
                {
                    object item = _list[i].Target;
                    if (item != null)
                    {
                        if (item == target)
                        {
                            _list.RemoveAt(i);
                            --i;
                        }
                    }
                    else
                    {
                        hasDeadEntries = true;
                    }
                }

                if (hasDeadEntries)
                {
                    Purge();
                }
            }

            // invoke the given action on each item
            public void Process(Func<object, bool> action)
            {
                bool hasDeadEntries = false;
                for (int i=0;  i<_list.Count;  ++i)
                {
                    object item = _list[i].Target;
                    if (item != null)
                    {
                        if (action(item))
                            break;
                    }
                    else
                    {
                        hasDeadEntries = true;
                    }
                }

                if (hasDeadEntries)
                {
                    // some actions cause the loop to exit early (often after
                    // the first call.  Don't penalize them with a synchronous
                    // purge;  instead purge later when there's nothing more
                    // important to do.
                    ScheduleCleanup();
                }
            }

            // purge the list of dead references
            private void Purge()
            {
                int destIndex = 0;
                int n = _list.Count;

                // move valid entries toward the beginning, into one
                // contiguous block
                for (int i=0; i<n; ++i)
                {
                    if (_list[i].IsAlive)
                    {
                        _list[destIndex++] = _list[i];
                    }
                }

                // remove the remaining entries and shrink the list
                if (destIndex < n)
                {
                    _list.RemoveRange(destIndex, n - destIndex);

                    // shrink the list if it would be less than half full otherwise.
                    // This is more liberal than List<T>.TrimExcess(), because we're
                    // probably in the situation where additions to the list are common.
                    int newCapacity = destIndex << 1;
                    if (newCapacity < _list.Capacity)
                    {
                        _list.Capacity = newCapacity;
                    }
                }
            }

            // schedule a cleanup pass
            private void ScheduleCleanup()
            {
                if (!_isCleanupRequested)
                {
                    _isCleanupRequested = true;
                    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                            (DispatcherOperationCallback)delegate(object unused)
                            {
                                lock(this)
                                {
                                    Purge();

                                    // cleanup is done
                                    _isCleanupRequested = false;
                                }
                                return null;
                            }, null);
                }
            }

            List<WeakReference> _list = new List<WeakReference>(1);
            bool _isCleanupRequested;
        }

        #endregion WeakReferenceList
    }
}





