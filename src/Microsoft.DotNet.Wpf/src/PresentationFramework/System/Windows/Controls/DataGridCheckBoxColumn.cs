// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A column that displays a check box.
    /// </summary>
    public class DataGridCheckBoxColumn : DataGridBoundColumn
    {
        static DataGridCheckBoxColumn()
        {
            ElementStyleProperty.OverrideMetadata(typeof(DataGridCheckBoxColumn), new FrameworkPropertyMetadata(DefaultElementStyle));
            EditingElementStyleProperty.OverrideMetadata(typeof(DataGridCheckBoxColumn), new FrameworkPropertyMetadata(DefaultEditingElementStyle));
        }

        #region Styles

        /// <summary>
        ///     The default value of the ElementStyle property.
        ///     This value can be used as the BasedOn for new styles.
        /// </summary>
        public static Style DefaultElementStyle
        {
            get
            {
                if (_defaultElementStyle == null)
                {
                    Style style = new Style(typeof(CheckBox));

                    // When not in edit mode, the end-user should not be able to toggle the state
                    style.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
                    style.Setters.Add(new Setter(UIElement.FocusableProperty, false));
                    style.Setters.Add(new Setter(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                    style.Setters.Add(new Setter(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Top));

                    style.Seal();
                    _defaultElementStyle = style;
                }

                return _defaultElementStyle;
            }
        }

        /// <summary>
        ///     The default value of the EditingElementStyle property.
        ///     This value can be used as the BasedOn for new styles.
        /// </summary>
        public static Style DefaultEditingElementStyle
        {
            get
            {
                if (_defaultEditingElementStyle == null)
                {
                    Style style = new Style(typeof(CheckBox));

                    style.Setters.Add(new Setter(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                    style.Setters.Add(new Setter(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Top));

                    style.Seal();
                    _defaultEditingElementStyle = style;
                }

                return _defaultEditingElementStyle;
            }
        }

        #endregion

        #region Element Generation

        /// <summary>
        ///     Creates the visual tree for boolean based cells.
        /// </summary>
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            return GenerateCheckBox(/* isEditing = */ false, cell);
        }

        /// <summary>
        ///     Creates the visual tree for boolean based cells.
        /// </summary>
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            return GenerateCheckBox(/* isEditing = */ true, cell);
        }

        private CheckBox GenerateCheckBox(bool isEditing, DataGridCell cell)
        {
            CheckBox checkBox = (cell != null) ? (cell.Content as CheckBox) : null;
            if (checkBox == null)
            {
                checkBox = new CheckBox();
            }

            checkBox.IsThreeState = IsThreeState;

            ApplyStyle(isEditing, /* defaultToElementStyle = */ true, checkBox);
            ApplyBinding(checkBox, CheckBox.IsCheckedProperty);

            return checkBox;
        }

        protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
        {
            DataGridCell cell = element as DataGridCell;
            if (cell != null &&
                string.Compare(propertyName, "IsThreeState", StringComparison.Ordinal) == 0)
            {
                var checkBox = cell.Content as CheckBox;
                if (checkBox != null)
                {
                    checkBox.IsThreeState = IsThreeState;
                }
            }
            else
            {
                base.RefreshCellContent(element, propertyName);
            }
        }

        #endregion

        #region Editing

        /// <summary>
        ///     The DependencyProperty for the IsThreeState property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsThreeStateProperty =
                CheckBox.IsThreeStateProperty.AddOwner(
                        typeof(DataGridCheckBoxColumn),
                        new FrameworkPropertyMetadata(false, DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     The IsThreeState property determines whether the control supports two or three states.
        ///     IsChecked property can be set to null as a third state when IsThreeState is true
        /// </summary>
        public bool IsThreeState
        {
            get { return (bool)GetValue(IsThreeStateProperty); }
            set { SetValue(IsThreeStateProperty, value); }
        }

        /// <summary>
        ///     Called when a cell has just switched to edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="editingEventArgs">The event args of the input event that caused the cell to go into edit mode. May be null.</param>
        /// <returns>The unedited value of the cell.</returns>
        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            CheckBox checkBox = editingElement as CheckBox;
            if (checkBox != null)
            {
                checkBox.Focus();
                bool? uneditedValue = checkBox.IsChecked;

                // If a click or a space key invoked the begin edit, then do an immediate toggle
                if ((IsMouseLeftButtonDown(editingEventArgs) && IsMouseOver(checkBox, editingEventArgs)) ||
                    IsSpaceKeyDown(editingEventArgs))
                {
                    checkBox.IsChecked = (uneditedValue != true);
                }

                return uneditedValue;
            }

            return (bool?) false;
        }

        internal override void OnInput(InputEventArgs e)
        {
            // Space key down will begin edit mode
            if (IsSpaceKeyDown(e))
            {
                BeginEdit(e, true);
            }
        }

        private static bool IsMouseLeftButtonDown(RoutedEventArgs e)
        {
            MouseButtonEventArgs mouseArgs = e as MouseButtonEventArgs;
            return (mouseArgs != null) &&
                   (mouseArgs.ChangedButton == MouseButton.Left) &&
                   (mouseArgs.ButtonState == MouseButtonState.Pressed);
        }

        private static bool IsMouseOver(CheckBox checkBox, RoutedEventArgs e)
        {
            // This element is new, so the IsMouseOver property will not have been updated
            // yet, but there is enough information to do a hit-test.
            return checkBox.InputHitTest(((MouseButtonEventArgs)e).GetPosition(checkBox)) != null;
        }

        private static bool IsSpaceKeyDown(RoutedEventArgs e)
        {
            KeyEventArgs keyArgs = e as KeyEventArgs;
            return (keyArgs != null) &&
                    keyArgs.RoutedEvent == Keyboard.KeyDownEvent && 
                   ((keyArgs.KeyStates & KeyStates.Down) == KeyStates.Down) &&
                   (keyArgs.Key == Key.Space);
        }

        #endregion

        #region Data

        private static Style _defaultElementStyle;
        private static Style _defaultEditingElementStyle;

        #endregion
    }
}