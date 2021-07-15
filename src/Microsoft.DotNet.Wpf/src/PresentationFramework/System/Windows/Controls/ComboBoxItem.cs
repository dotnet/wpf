// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.KnownBoxes;
using System;
using System.ComponentModel;

using System.Diagnostics;
using System.Windows.Threading;

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Control that implements a selectable item inside a ComboBox.
    /// </summary>
    [Localizability(LocalizationCategory.ComboBox)]
#if OLD_AUTOMATION
    [Automation(AccessibilityControlType = "ListItem")]
#endif
    public class ComboBoxItem : ListBoxItem
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ComboBoxItem() : base()
        {
        }

        static ComboBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ComboBoxItem), new FrameworkPropertyMetadata(typeof(ComboBoxItem)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ComboBoxItem));
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsHighlightedPropertyKey =
            DependencyProperty.RegisterReadOnly("IsHighlighted", typeof(bool), typeof(ComboBoxItem), 
                                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// DependencyProperty for the IsHighlighted property
        /// </summary>
        public static readonly DependencyProperty IsHighlightedProperty =
            IsHighlightedPropertyKey.DependencyProperty;

        /// <summary>
        /// Indicates if the item is highlighted or not.  Styles that want to
        /// show a highlight for selection should trigger off of this value.
        /// </summary>
        /// <value></value>
        public bool IsHighlighted
        {
            get
            {
                return (bool)GetValue(IsHighlightedProperty);
            }
            protected set
            {
                SetValue(IsHighlightedPropertyKey, BooleanBoxes.Box(value));
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///     This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;

            ComboBox parent = ParentComboBox;

            if (parent != null)
            {
                parent.NotifyComboBoxItemMouseDown(this);
            }

            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            e.Handled = true;

            ComboBox parent = ParentComboBox;

            if (parent != null)
            {
                parent.NotifyComboBoxItemMouseUp(this);
            }

            base.OnMouseLeftButtonUp(e);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            e.Handled = true;

            ComboBox parent = ParentComboBox;

            if (parent != null)
            {
                parent.NotifyComboBoxItemEnter(this);
            }

            base.OnMouseEnter(e);
        }

        /// <summary>
        /// Called when Content property has been changed.
        /// </summary>
        /// <param name="oldContent"></param>
        /// <param name="newContent"></param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            
            // If this is selected, we need to update ParentComboBox.Text
            // Scenario:
            //      <ComboBox Width="200" Height="20" IsEditable="True" MaxDropDownHeight="50">
            //          <Text>item1</Text>
            //          <Text>Item2</Text>
            //          <ComboBoxItem IsSelected="True">item3</ComboBoxItem>
            //      </ComboBox>
            // In this case ComboBox will try to update Text property as soon as it get
            // SelectionChanged event. However, at that time ComboBoxItem.Content is not
            // parse yet. So, Content is null. This causes ComboBox.Text to be "".
            //
            ComboBox parent;
            if (IsSelected && (null != (parent = ParentComboBox)))
            {
                parent.SelectedItemUpdated();
            }

            // When the content of the combobox item is a UIElement,
            // combobox will create a visual clone of the item which needs
            // to update even when the combobox is closed
            SetFlags(newContent is UIElement, VisualFlags.IsLayoutIslandRoot);
        }

        /// <summary>
        ///     Called when this element gets focus.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;
            
            ComboBox parent = ParentComboBox;

            if (parent != null)
            {
                parent.NotifyComboBoxItemEnter(this);
            }

            base.OnGotKeyboardFocus(e);
        }
        
        #endregion

        //-------------------------------------------------------------------
        //
        //  Implementation
        //
        //-------------------------------------------------------------------

        #region Implementation

        private ComboBox ParentComboBox
        {
            get
            {
                return ParentSelector as ComboBox;
            }
        }

        internal void SetIsHighlighted(bool isHighlighted)
        {
            IsHighlighted = isHighlighted;
        }

        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default 
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}
