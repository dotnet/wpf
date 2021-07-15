// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MS.Internal.KnownBoxes;
using System.ComponentModel;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Service class that provides the system implementation for displaying ToolTips.
    /// </summary>
    public static class ToolTipService
    {
        #region Attached Properties

        /// <summary>
        ///     The DependencyProperty for the ToolTip property.
        /// </summary>
        public static readonly DependencyProperty ToolTipProperty = 
                DependencyProperty.RegisterAttached(
                        "ToolTip",              // Name
                        typeof(object),         // Type
                        typeof(ToolTipService), // Owner
                        new FrameworkPropertyMetadata((object) null));

        /// <summary>
        ///     Gets the value of the ToolTip property on the specified object.
        /// </summary>
        /// <param name="element">The object on which to query the ToolTip property.</param>
        /// <returns>The value of the ToolTip property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static object GetToolTip(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return element.GetValue(ToolTipProperty);
        }

        /// <summary>
        ///     Sets the ToolTip property on the specified object.
        /// </summary>
        /// <param name="element">The object on which to set the ToolTip property.</param>
        /// <param name="value">
        ///     The value of the ToolTip property. If the value is of type ToolTip, then
        ///     that is the ToolTip that will be used (without any modification). If the value
        ///     is of any other type, then that value will be used as the content for a ToolTip
        ///     provided by this service, and the other attached properties of this service
        ///     will be used to configure the ToolTip.
        /// </param>
        public static void SetToolTip(DependencyObject element, object value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ToolTipProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the HorizontalOffset property.
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.RegisterAttached("HorizontalOffset",     // Name
                                                typeof(double),         // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(0d)); // Default Value

        /// <summary>
        ///     Gets the value of the HorizontalOffset property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [TypeConverter(typeof(LengthConverter))]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static double GetHorizontalOffset(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (double)element.GetValue(HorizontalOffsetProperty);
        }

        /// <summary>
        ///     Sets the value of the HorizontalOffset property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetHorizontalOffset(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(HorizontalOffsetProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the VerticalOffset property.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset",       // Name
                                                typeof(double),         // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(0d)); // Default Value

        /// <summary>
        ///     Gets the value of the VerticalOffset property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [TypeConverter(typeof(LengthConverter))]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static double GetVerticalOffset(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (double)element.GetValue(VerticalOffsetProperty);
        }

        /// <summary>
        ///     Sets the value of the VerticalOffset property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetVerticalOffset(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(VerticalOffsetProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for HasDropShadow
        /// </summary>
        public static readonly DependencyProperty HasDropShadowProperty =
            DependencyProperty.RegisterAttached("HasDropShadow",        // Name
                                                typeof(bool),           // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox)); // Default Value

        /// <summary>
        ///     Gets the value of the HasDropShadow property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetHasDropShadow(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(HasDropShadowProperty);
        }

        /// <summary>
        ///     Sets the value of the HasDropShadow property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetHasDropShadow(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(HasDropShadowProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        ///     The DependencyProperty for the PlacementTarget property.
        /// </summary>
        public static readonly DependencyProperty PlacementTargetProperty =
            DependencyProperty.RegisterAttached("PlacementTarget",      // Name
                                                typeof(UIElement),      // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata((UIElement)null)); // Default Value

        /// <summary>
        ///     Gets the value of the PlacementTarget property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static UIElement GetPlacementTarget(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (UIElement)element.GetValue(PlacementTargetProperty);
        }

        /// <summary>
        ///     Sets the value of the PlacementTarget property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetPlacementTarget(DependencyObject element, UIElement value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(PlacementTargetProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the PlacementRectangle property.
        /// </summary>
        public static readonly DependencyProperty PlacementRectangleProperty =
            DependencyProperty.RegisterAttached("PlacementRectangle",   // Name
                                                typeof(Rect),           // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(Rect.Empty)); // Default Value

        /// <summary>
        ///     Gets the value of the PlacementRectangle property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static Rect GetPlacementRectangle(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Rect)element.GetValue(PlacementRectangleProperty);
        }

        /// <summary>
        ///     Sets the value of the PlacementRectangle property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetPlacementRectangle(DependencyObject element, Rect value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(PlacementRectangleProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the Placement property.
        /// </summary>
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.RegisterAttached("Placement",            // Name
                                                typeof(PlacementMode),  // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(PlacementMode.Mouse)); // Default Value

        /// <summary>
        ///     Gets the value of the Placement property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static PlacementMode GetPlacement(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (PlacementMode)element.GetValue(PlacementProperty);
        }

        /// <summary>
        ///     Sets the value of the Placement property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetPlacement(DependencyObject element, PlacementMode value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(PlacementProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the ShowOnDisabled property.
        /// </summary>
        public static readonly DependencyProperty ShowOnDisabledProperty =
            DependencyProperty.RegisterAttached("ShowOnDisabled",       // Name
                                                typeof(bool),           // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox)); // Default Value

        /// <summary>
        ///     Gets the value of the ShowOnDisabled property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetShowOnDisabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(ShowOnDisabledProperty);
        }

        /// <summary>
        ///     Sets the value of the ShowOnDisabled property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetShowOnDisabled(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ShowOnDisabledProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        ///     Read-only Key Token for the IsOpen property.
        /// </summary>
        private static readonly DependencyPropertyKey IsOpenPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("IsOpen",               // Name
                                                        typeof(bool),           // Type
                                                        typeof(ToolTipService), // Owner
                                                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox)); // Default Value

        /// <summary>
        ///     The DependencyProperty for the IsOpen property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the value of the IsOpen property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsOpen(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsOpenProperty);
        }

        /// <summary>
        ///     Sets the value of the IsOpen property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        private static void SetIsOpen(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsOpenPropertyKey, BooleanBoxes.Box(value));
        }

        /// <summary>
        ///     The DependencyProperty for the IsEnabled property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled",            // Name
                                                typeof(bool),           // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(BooleanBoxes.TrueBox)); // Default Value

        /// <summary>
        ///     Gets the value of the IsEnabled property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsEnabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsEnabledProperty);
        }

        /// <summary>
        ///     Sets the value of the IsEnabled property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));
        }

        private static bool PositiveValueValidation(object o)
        {
            return ((int)o) >= 0;
        }

        /// <summary>
        ///     The DependencyProperty for the ShowDuration property.
        /// </summary>
        public static readonly DependencyProperty ShowDurationProperty =
            DependencyProperty.RegisterAttached("ShowDuration",         // Name
                                                typeof(int),            // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(Int32.MaxValue),          // Default Value
                                                new ValidateValueCallback(PositiveValueValidation));    // Value validation

        /// <summary>
        ///     Gets the value of the ShowDuration property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetShowDuration(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (int)element.GetValue(ShowDurationProperty);
        }

        /// <summary>
        ///     Sets the value of the ShowDuration property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetShowDuration(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ShowDurationProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the InitialShowDelay property.
        /// </summary>
        public static readonly DependencyProperty InitialShowDelayProperty =
            DependencyProperty.RegisterAttached("InitialShowDelay",     // Name
                                                typeof(int),            // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(1000), // Default Value
                                                new ValidateValueCallback(PositiveValueValidation));    // Value validation

        /// <summary>
        ///     Gets the value of the InitialShowDelay property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetInitialShowDelay(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (int)element.GetValue(InitialShowDelayProperty);
        }

        /// <summary>
        ///     Sets the value of the InitialShowDelay property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetInitialShowDelay(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(InitialShowDelayProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the BetweenShowDelay property.
        /// </summary>
        public static readonly DependencyProperty BetweenShowDelayProperty =
            DependencyProperty.RegisterAttached("BetweenShowDelay",     // Name
                                                typeof(int),            // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(100),   // Default Value
                                                new ValidateValueCallback(PositiveValueValidation));    // Value validation

        /// <summary>
        ///     Gets the value of the BetweenShowDelay property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetBetweenShowDelay(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (int)element.GetValue(BetweenShowDelayProperty);
        }

        /// <summary>
        ///     Sets the value of the BetweenShowDelay property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetBetweenShowDelay(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(BetweenShowDelayProperty, value);
        }

        /// <summary>
        ///     The DependencyProperty for the ShowsToolTipOnKeyboardFocus property.
        /// </summary>
        public static readonly DependencyProperty ShowsToolTipOnKeyboardFocusProperty =
            DependencyProperty.RegisterAttached("ShowsToolTipOnKeyboardFocus",     // Name
                                                typeof(bool?),          // Type
                                                typeof(ToolTipService), // Owner
                                                new FrameworkPropertyMetadata(NullableBooleanBoxes.NullBox));   // Default Value

        /// <summary>
        ///     Gets the value of the ShowsToolTipOnKeyboardFocus property.
        /// </summary>
        /// <param name="element">The object on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        // Setting this property breaks accessibility, so don't expose it through the designer.  
        //[AttachedPropertyBrowsableForType(typeof(DependencyObject))]      
        public static bool? GetShowsToolTipOnKeyboardFocus(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool?)element.GetValue(ShowsToolTipOnKeyboardFocusProperty);
        }

        /// <summary>
        ///     Sets the value of the ShowsToolTipOnKeyboardFocus property.
        /// </summary>
        /// <param name="element">The object on which to set the value.</param>
        /// <param name="value">The desired value of the property.</param>
        public static void SetShowsToolTipOnKeyboardFocus(DependencyObject element, bool? value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ShowsToolTipOnKeyboardFocusProperty, NullableBooleanBoxes.Box(value));
        }

        #endregion

        #region Events

        /// <summary>
        ///     The event raised when a ToolTip is going to be shown on an element.
        /// 
        ///     Mark the event as handled if manually showing a ToolTip.
        /// 
        ///     Replacing the value of the ToolTip property is allowed 
        ///     (example: for delay-loading). Do not mark the event as handled 
        ///     in this case if the system is to show the ToolTip.
        /// </summary>
        public static readonly RoutedEvent ToolTipOpeningEvent = 
            EventManager.RegisterRoutedEvent("ToolTipOpening", 
                                               RoutingStrategy.Direct,
                                               typeof(ToolTipEventHandler),
                                               typeof(ToolTipService));

        /// <summary>
        ///     Adds a handler for the ToolTipOpening attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddToolTipOpeningHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.AddHandler(element, ToolTipOpeningEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the ToolTipOpening attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveToolTipOpeningHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.RemoveHandler(element, ToolTipOpeningEvent, handler);
        }

        /// <summary>
        ///     The event raised when a ToolTip on an element that was shown 
        ///     should now be hidden.
        /// </summary>
        public static readonly RoutedEvent ToolTipClosingEvent =
            EventManager.RegisterRoutedEvent("ToolTipClosing", 
                                               RoutingStrategy.Direct, 
                                               typeof(ToolTipEventHandler), 
                                               typeof(ToolTipService));


        /// <summary>
        ///     Adds a handler for the ToolTipClosing attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddToolTipClosingHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.AddHandler(element, ToolTipClosingEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the ToolTipClosing attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveToolTipClosingHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.RemoveHandler(element, ToolTipClosingEvent, handler);
        }

        #endregion

        #region Implementation

        internal static readonly RoutedEvent FindToolTipEvent =
           EventManager.RegisterRoutedEvent("FindToolTip",
                                              RoutingStrategy.Bubble,
                                              typeof(FindToolTipEventHandler),
                                              typeof(ToolTipService));

        static ToolTipService()
        {
            EventManager.RegisterClassHandler(typeof(UIElement), FindToolTipEvent, new FindToolTipEventHandler(OnFindToolTip));
            EventManager.RegisterClassHandler(typeof(ContentElement), FindToolTipEvent, new FindToolTipEventHandler(OnFindToolTip));
            EventManager.RegisterClassHandler(typeof(UIElement3D), FindToolTipEvent, new FindToolTipEventHandler(OnFindToolTip));
        }

        private static void OnFindToolTip(object sender, FindToolTipEventArgs e)
        {
            if (e.TargetElement == null)
            {
                DependencyObject o = sender as DependencyObject;
                if (o != null)
                {
                    if (ToolTipIsEnabled(o, e.TriggerAction))
                    {
                        // Store for later
                        e.TargetElement = o;
                        e.Handled = true;
                    }
                }
            }
        }

        internal static bool ToolTipIsEnabled(DependencyObject o, TriggerAction triggerAction)
        {
            object tooltipObject = GetToolTip(o);

            if ((tooltipObject != null) && GetIsEnabled(o))
            {
                // determine whether tooltip-on-keyboard-focus is enabled
                bool enableOnKeyboardFocus = true;
                if (triggerAction == TriggerAction.KeyboardFocus)
                {
                    // attached property on owner has first priority
                    bool? propertyValue = GetShowsToolTipOnKeyboardFocus(o);

                    // if that doesn't say, get the value from the ToolTip itself (if any)
                    if (propertyValue == (bool?)null)
                    {
                        ToolTip tooltip = tooltipObject as ToolTip;
                        if (tooltip != null)
                        {
                            propertyValue = tooltip.ShowsToolTipOnKeyboardFocus;
                        }
                    }

                    // the behavior is enabled, unless explicitly told otherwise
                    enableOnKeyboardFocus = (propertyValue != false);
                }

                if ((PopupControlService.IsElementEnabled(o) || GetShowOnDisabled(o)) && enableOnKeyboardFocus)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsFromKeyboard(ToolTipService.TriggerAction triggerAction)
        {
            return (triggerAction == TriggerAction.KeyboardFocus ||
                    triggerAction == TriggerAction.KeyboardShortcut);
        }

        #endregion


        internal enum TriggerAction
        {
            Mouse,
            KeyboardFocus,
            KeyboardShortcut
        }

    }

    /// <summary>
    ///     The callback type for the events when a ToolTip should open or close.
    /// </summary>
    public delegate void ToolTipEventHandler(object sender, ToolTipEventArgs e);

    /// <summary>
    ///     Event arguments for the events when a ToolTip should open or close.
    /// </summary>
    public sealed class ToolTipEventArgs : RoutedEventArgs
    {
        /// <summary>
        ///     Called internally to create opening or closing event arguments.
        /// </summary>
        /// <param name="opening">Whether this is the opening or closing event.</param>
        internal ToolTipEventArgs(bool opening)
        {
            if (opening)
            {
                RoutedEvent = ToolTipService.ToolTipOpeningEvent;
            }
            else
            {
                RoutedEvent = ToolTipService.ToolTipClosingEvent;
            }
        }

        /// <summary>
        ///     Invokes the event handler.
        /// </summary>
        /// <param name="genericHandler">The delegate to call.</param>
        /// <param name="genericTarget">The target of the event.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ToolTipEventHandler handler = (ToolTipEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }

    internal delegate void FindToolTipEventHandler(object sender, FindToolTipEventArgs e);

    internal sealed class FindToolTipEventArgs : RoutedEventArgs
    {
        internal FindToolTipEventArgs(ToolTipService.TriggerAction triggerAction)
        {
            RoutedEvent = ToolTipService.FindToolTipEvent;
            _triggerAction = triggerAction;
        }

        internal DependencyObject TargetElement
        {
            get { return _targetElement; }
            set { _targetElement = value; }
        }

        internal ToolTipService.TriggerAction TriggerAction
        {
            get { return _triggerAction; }
        }

        /// <summary>
        ///     Invokes the event handler.
        /// </summary>
        /// <param name="genericHandler">The delegate to call.</param>
        /// <param name="genericTarget">The target of the event.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            FindToolTipEventHandler handler = (FindToolTipEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private DependencyObject _targetElement;
        private ToolTipService.TriggerAction _triggerAction;
    }
}
