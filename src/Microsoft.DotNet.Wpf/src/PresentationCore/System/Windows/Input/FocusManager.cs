// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Security;
using MS.Internal.KnownBoxes;


namespace System.Windows.Input
{
    /// <summary>
    ///   FocusManager define attached property used for tracking the FocusedElement with a focus scope
    /// </summary>
    public static class FocusManager
    {
        #region Public Events

        /// <summary>
        ///     GotFocus event
        /// </summary>
        public static readonly RoutedEvent GotFocusEvent = EventManager.RegisterRoutedEvent("GotFocus", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FocusManager));

        /// <summary>
        ///     Adds a handler for the GotFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddGotFocusHandler(DependencyObject element, RoutedEventHandler handler)
        {
            UIElement.AddHandler(element, GotFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the GotFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveGotFocusHandler(DependencyObject element, RoutedEventHandler handler)
        {
            UIElement.RemoveHandler(element, GotFocusEvent, handler);
        }

        /// <summary>
        ///     LostFocus event
        /// </summary>
        public static readonly RoutedEvent LostFocusEvent = EventManager.RegisterRoutedEvent("LostFocus", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FocusManager));

        /// <summary>
        ///     Adds a handler for the LostFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddLostFocusHandler(DependencyObject element, RoutedEventHandler handler)
        {
            UIElement.AddHandler(element, LostFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the LostFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveLostFocusHandler(DependencyObject element, RoutedEventHandler handler)
        {
            UIElement.RemoveHandler(element, LostFocusEvent, handler);
        }

        #endregion Public Events

        #region Public Properties

        #region FocusedElement property

        /// <summary>
        /// The DependencyProperty for the FocusedElement property. This internal property tracks IsActive
        /// element ref inside TrackFocus
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty FocusedElementProperty =
                DependencyProperty.RegisterAttached(
                        "FocusedElement",
                        typeof(IInputElement),
                        typeof(FocusManager),
                        new PropertyMetadata(new PropertyChangedCallback(OnFocusedElementChanged)));

        #endregion FocusedElement property

        #region IsFocusScope Property
        /// <summary>
        ///     The DependencyProperty for the IsFocusScope property.
        /// This property is used to mark the special containers (like Window, Menu) so they can
        /// keep track of the FocusedElement element inside the container. Once focus is set
        /// on the container - it is delegated to the FocusedElement element
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsFocusScopeProperty
            = DependencyProperty.RegisterAttached("IsFocusScope", typeof(bool), typeof(FocusManager),
                                                        new PropertyMetadata(BooleanBoxes.FalseBox));

        #endregion IsFocusScope Property

        #endregion

        #region Public static methods

        /// <summary>
        /// Return the property value of FocusedElement property.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static IInputElement GetFocusedElement(DependencyObject element)
        {
            return GetFocusedElement(element, false);
        }

        /// <summary>
        /// Return the property value of FocusedElement property. The return value is validated
        /// to be in the subtree of element. If FocusedElement element is not a descendant of element this method return null
        /// </summary>
        /// <param name="element"></param>
        /// <param name="validate"></param>
        /// <returns></returns>
        internal static IInputElement GetFocusedElement(DependencyObject element, bool validate)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            DependencyObject focusedElement = (DependencyObject) element.GetValue(FocusedElementProperty);

            // Validate FocusedElement wrt to its FocusScope. If the two do not belong to the same PresentationSource 
            // then sever this link between them. The classic scenario for this is when an element with logical focus is 
            // dragged out into a floating widow. We want to prevent the MainWindow (focus scope) to point to the 
            // element in the floating window as its logical FocusedElement.
            
            if (validate && focusedElement != null)
            {
                DependencyObject focusScope = element;

                if (PresentationSource.CriticalFromVisual(focusScope) != PresentationSource.CriticalFromVisual(focusedElement))
                {
                    SetFocusedElement(focusScope, null);
                    focusedElement = null;
                }
            }

            return (IInputElement)focusedElement;
        }

        /// <summary>
        ///     Set FocusedElement property for element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetFocusedElement(DependencyObject element, IInputElement value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FocusedElementProperty, value);
        }

        /// <summary>
        /// Writes the attached property IsFocusScope to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetIsFocusScope(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsFocusScopeProperty, value);
        }

        /// <summary>
        /// Reads the attached property IsFocusScope from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        public static bool GetIsFocusScope(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsFocusScopeProperty);
        }

        /// <summary>
        /// Find the closest visual ancestor that has IsFocusScope set to true
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static DependencyObject GetFocusScope(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return _GetFocusScope(element);
        }

        #endregion Public methods

        #region private implementation
        // verify this works with focus changes to FCEs
        // When FocusedElement property changes we need to update IsFocusedElement property on the old
        // and new FocusedElement elements
        private static void OnFocusedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IInputElement newFocusedElement = (IInputElement)e.NewValue;
            DependencyObject oldVisual = (DependencyObject)e.OldValue;
            DependencyObject newVisual = (DependencyObject)e.NewValue;

            if (oldVisual != null)
            {
                oldVisual.ClearValue(UIElement.IsFocusedPropertyKey);
            }

            if (newVisual != null)
            {
                // set IsFocused on the element.  The element may redirect Keyboard focus
                // in response to this (e.g. Editable ComboBox redirects to the
                // child TextBox), so detect whether this happens.
                DependencyObject oldFocus = Keyboard.FocusedElement as DependencyObject;
                newVisual.SetValue(UIElement.IsFocusedPropertyKey, BooleanBoxes.TrueBox);
                DependencyObject newFocus = Keyboard.FocusedElement as DependencyObject;

                // set the Keyboard focus to the new element, provided that
                //  a) the element didn't already set Keyboard focus
                //  b) Keyboard focus is not already on the new element
                //  c) the new element is within the same focus scope as the current
                //      holder (if any) of Keyboard focus
                if (oldFocus == newFocus && newVisual != newFocus &&
                        (newFocus == null || GetRoot(newVisual) == GetRoot(newFocus)))
                {
                    Keyboard.Focus(newFocusedElement);
                }
            }

/*
            if (!_currentlyUpdatingTree)
            {
                _currentlyUpdatingTree = true;

                IInputElement newFocusedElement = (IInputElement) newValue;
                Visual oldVisual = GetNearestVisual(args.OldValue);
                Visual newVisual = GetNearestVisual(args.NewValue);

                if (oldVisual != null)
                {
                    oldVisual.ClearValue(UIElement.IsFocusedPropertyKey);

                    // reverse-inherit:  clear the property on all parents that aren't also in the
                    // new focused element ancestry
                    while (oldVisual != null)
                    {
                        oldVisual.ClearValue(FocusedElementProperty);
                        oldVisual = VisualTreeHelper.GetParent(oldVisual);
                        if ((oldVisual == newVisual) || oldVisual.IsAncestorOf(newVisual))
                        {
                            // only walk up until you reach a common parent -- the new value walk will take care of the rest
                            break;
                        }
                    }
                }

                if (newVisual != null)
                {
                    newVisual.SetValue(UIElement.IsFocusedPropertyKey, BooleanBoxes.TrueBox);

                    // reverse-inherit:  set the property on all parents
                    while (newVisual != null)
                    {
                        newVisual.SetValue(FocusedElementProperty, newFocusedElement);
                        newVisual = VisualTreeHelper.GetParent(newVisual);
                    }

                    // Set Keyboard focus to the element if not already focused && current focus is within the current focus scope
                    DependencyObject currentFocus = Keyboard.FocusedElement as DependencyObject;
                    if ((currentFocus == null) &&
                        (newVisual != currentFocus) &&
                        (GetRoot(newVisual) == GetRoot(currentFocus)))
                    {
                        Keyboard.Focus(newFocusedElement);
                    }
                }
                _currentlyUpdatingTree = false;
            }
*/
        }
        
/*
        private static Visual GetNearestVisual(object value)
        {
            Visual visual = null;
            if (value != null)
            {
                visual = value as Visual;
                if (visual == null)
                {
                    ContentElement ce = value as ContentElement;
                    if (ce != null)
                    {
                        visual = ce.GetUIParent() as Visual;
                    }
                }
            }
            return visual;
        }
*/

        private static DependencyObject GetRoot(DependencyObject element)
        {
            if (element == null)
                return null;

            DependencyObject parent = null;
            DependencyObject dependencyObject = element;
            
            ContentElement ce = element as ContentElement;
            if (ce != null)
                dependencyObject = ce.GetUIParent();
            
            while (dependencyObject != null)
            {
                parent = dependencyObject; 
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            return parent;
        }

        // Walk up the parent chain to find the closest element with IsFocusScope=true
        private static DependencyObject _GetFocusScope(DependencyObject d)
        {
            if (d == null)
                return null;

            if ((bool)d.GetValue(IsFocusScopeProperty))
                return d;

            // Step 1: Walk up the logical tree
            UIElement uiElement = d as UIElement;
            if (uiElement != null)
            {
                DependencyObject logicalParent = uiElement.GetUIParentCore();
                if (logicalParent != null)
                {
                    return GetFocusScope(logicalParent);
                }
            }
            else
            {
                ContentElement ce = d as ContentElement;
                if (ce != null)
                {
                    DependencyObject logicalParent = ce.GetUIParent(true);
                    if (logicalParent != null)
                    {
                        return _GetFocusScope(logicalParent);
                    }
                }
                else
                {
                    UIElement3D uiElement3D = d as UIElement3D;
                    if (uiElement3D != null)
                    {
                        DependencyObject logicalParent = uiElement3D.GetUIParentCore();
                        if (logicalParent != null)
                        {
                            return GetFocusScope(logicalParent);
                        }
                    }
                }
            }

            // Step 2: Walk up the visual tree            
            if (d is Visual || d is Visual3D)
            {
                DependencyObject visualParent = VisualTreeHelper.GetParent(d);
                if (visualParent != null)
                {
                    return _GetFocusScope(visualParent);
                }
            }

            // If visual and logical parent is null - then the element is implicit focus scope
            return d;
        }
        #endregion private implementation

        #region private data

        private static readonly UncommonField<bool> IsFocusedElementSet = new UncommonField<bool>();
        private static readonly UncommonField<WeakReference> FocusedElementWeakCacheField = new UncommonField<WeakReference>();
        private static readonly UncommonField<bool> IsFocusedElementCacheValid = new UncommonField<bool>();
        private static readonly UncommonField<WeakReference> FocusedElementCache = new UncommonField<WeakReference>();

//        private static bool _currentlyUpdatingTree = false;

        #endregion private data
    }
}

