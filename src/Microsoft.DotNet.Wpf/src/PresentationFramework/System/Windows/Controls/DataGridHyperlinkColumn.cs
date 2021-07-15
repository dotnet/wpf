// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A column that displays a hyperlink.
    /// </summary>
    public class DataGridHyperlinkColumn : DataGridBoundColumn
    {
        static DataGridHyperlinkColumn()
        {
            ElementStyleProperty.OverrideMetadata(typeof(DataGridHyperlinkColumn), new FrameworkPropertyMetadata(DefaultElementStyle));
            EditingElementStyleProperty.OverrideMetadata(typeof(DataGridHyperlinkColumn), new FrameworkPropertyMetadata(DefaultEditingElementStyle));
        }

        #region Hyperlink Column Properties

        /// <summary>
        /// Dependecy property for TargetName Property
        /// </summary>
        public static readonly DependencyProperty TargetNameProperty =
            Hyperlink.TargetNameProperty.AddOwner(
                typeof(DataGridHyperlinkColumn),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(DataGridColumn.NotifyPropertyChangeForRefreshContent)));

        /// <summary>
        /// The property which determines the target name of the hyperlink
        /// </summary>
        public string TargetName
        {
            get { return (string)GetValue(TargetNameProperty); }
            set { SetValue(TargetNameProperty, value); }
        }

        /// <summary>
        /// The binding to the content to be display under hyperlink
        /// </summary>
        public BindingBase ContentBinding
        {
            get
            {
                return _contentBinding;
            }

            set
            {
                if (_contentBinding != value)
                {
                    BindingBase oldValue = _contentBinding;
                    _contentBinding = value;
                    OnContentBindingChanged(oldValue, value);
                }
            }
        }

        /// <summary>
        ///     Called when ContentBinding changes.
        /// </summary>
        /// <remarks>
        ///     Default implementation notifies the DataGrid and its subtree about the change.
        /// </remarks>
        /// <param name="oldBinding">The old binding.</param>
        /// <param name="newBinding">The new binding.</param>
        protected virtual void OnContentBindingChanged(BindingBase oldBinding, BindingBase newBinding)
        {
            NotifyPropertyChanged("ContentBinding");
        }

        /// <summary>
        /// Try applying ContentBinding. If it doesnt work out apply Binding.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        private void ApplyContentBinding(DependencyObject target, DependencyProperty property)
        {
            if (ContentBinding != null)
            {
                BindingOperations.SetBinding(target, property, ContentBinding);
            }
            else if (Binding != null)
            {
                BindingOperations.SetBinding(target, property, Binding);
            }
            else
            {
                BindingOperations.ClearBinding(target, property);
            }
        }

        #endregion

        #region Property Changed Handler

        /// <summary>
        /// Override which rebuilds the cell's visual tree for ContentBinding change
        /// and Modifies Hyperlink for TargetName change
        /// </summary>
        /// <param name="element"></param>
        /// <param name="propertyName"></param>
        protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
        {
            DataGridCell cell = element as DataGridCell;
            if (cell != null && !cell.IsEditing)
            {
                if (string.Compare(propertyName, "ContentBinding", StringComparison.Ordinal) == 0)
                {
                    cell.BuildVisualTree();
                }
                else if (string.Compare(propertyName, "TargetName", StringComparison.Ordinal) == 0)
                {
                    TextBlock outerBlock = cell.Content as TextBlock;
                    if (outerBlock != null && outerBlock.Inlines.Count > 0)
                    {
                        Hyperlink link = outerBlock.Inlines.FirstInline as Hyperlink;
                        if (link != null)
                        {
                            link.TargetName = TargetName;
                        }
                    }
                }
            }
            else
            {
                base.RefreshCellContent(element, propertyName);
            }
        }

        #endregion

        #region Styles

        /// <summary>
        ///     The default value of the ElementStyle property.
        ///     This value can be used as the BasedOn for new styles.
        /// </summary>
        public static Style DefaultElementStyle
        {
            get { return DataGridTextColumn.DefaultElementStyle; }
        }

        /// <summary>
        ///     The default value of the EditingElementStyle property.
        ///     This value can be used as the BasedOn for new styles.
        /// </summary>
        public static Style DefaultEditingElementStyle
        {
            get { return DataGridTextColumn.DefaultEditingElementStyle; }
        }

        #endregion

        #region Element Generation

        /// <summary>
        ///     Creates the visual tree for cells.
        /// </summary>
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            TextBlock outerBlock = new TextBlock();
            Hyperlink link = new Hyperlink();
            InlineUIContainer inlineContainer = new InlineUIContainer();
            ContentPresenter innerContentPresenter = new ContentPresenter();

            outerBlock.Inlines.Add(link);
            link.Inlines.Add(inlineContainer);
            inlineContainer.Child = innerContentPresenter;

            link.TargetName = TargetName;

            ApplyStyle(/* isEditing = */ false, /* defaultToElementStyle = */ false, outerBlock);
            ApplyBinding(link, Hyperlink.NavigateUriProperty);
            ApplyContentBinding(innerContentPresenter, ContentPresenter.ContentProperty);

            DataGridHelper.RestoreFlowDirection(outerBlock, cell);

            return outerBlock;
        }

        /// <summary>
        ///     Creates the visual tree for cells.
        /// </summary>
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            TextBox textBox = new TextBox();

            ApplyStyle(/* isEditing = */ true, /* defaultToElementStyle = */ false, textBox);
            ApplyBinding(textBox, TextBox.TextProperty);

            DataGridHelper.RestoreFlowDirection(textBox, cell);

            return textBox;
        }

        #endregion

        #region Editing

        /// <summary>
        ///     Called when a cell has just switched to edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="editingEventArgs">The event args of the input event that caused the cell to go into edit mode. May be null.</param>
        /// <returns>The unedited value of the cell.</returns>
        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            TextBox textBox = editingElement as TextBox;
            if (textBox != null)
            {
                textBox.Focus();

                string originalValue = textBox.Text;

                TextCompositionEventArgs textArgs = editingEventArgs as TextCompositionEventArgs;
                if (textArgs != null)
                {
                    // If text input started the edit, then replace the text with what was typed.
                    string inputText = textArgs.Text;
                    textBox.Text = inputText;

                    // Place the caret after the end of the text.
                    textBox.Select(inputText.Length, 0);
                }
                else
                {
                    // If something else started the edit, then select the text
                    textBox.SelectAll();
                }

                return originalValue;
            }

            return null;
        }

        /// <summary>
        ///     Called when a cell's value is to be restored to its original value,
        ///     just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="uneditedValue">The original, unedited value of the cell.</param>
        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            DataGridHelper.CacheFlowDirection(editingElement, editingElement != null ? editingElement.Parent as DataGridCell : null);
            
            base.CancelCellEdit(editingElement, uneditedValue);
        }

        /// <summary>
        ///     Called when a cell's value is to be committed, just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>false if there is a validation error. true otherwise.</returns>
        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            DataGridHelper.CacheFlowDirection(editingElement, editingElement != null ? editingElement.Parent as DataGridCell : null);

            return base.CommitCellEdit(editingElement);
        }

        internal override void OnInput(InputEventArgs e)
        {
            // Text input will start an edit.
            // Escape is meant to be for CancelEdit. But DataGrid
            // may not handle KeyDown event for Escape if nothing
            // is cancelable. Such KeyDown if unhandled by others
            // will ultimately get promoted to TextInput and be handled
            // here. But BeginEdit on escape could be confusing to the user.
            // Hence escape key is special case and BeginEdit is performed if
            // there is atleast one non espace key character.
            if (DataGridHelper.HasNonEscapeCharacters(e as TextCompositionEventArgs))
            {
                BeginEdit(e, true);
            }
            else if (DataGridHelper.IsImeProcessed(e as KeyEventArgs))
            {
                if (DataGridOwner != null)
                {
                    DataGridCell cell = DataGridOwner.CurrentCellContainer; 
                    if (cell != null && !cell.IsEditing)
                    {
                        Debug.Assert(e.RoutedEvent == Keyboard.PreviewKeyDownEvent, "We should only reach here on the PreviewKeyDown event because the TextBox within is expected to handle the preview event and hence trump the successive KeyDown event.");
                        
                        BeginEdit(e, false);

                        //
                        // The TextEditor for the TextBox establishes contact with the IME 
                        // engine lazily at background priority. However in this case we 
                        // want to IME engine to know about the TextBox in earnest before 
                        // PostProcessing this input event. Only then will the IME key be 
                        // recorded in the TextBox. Hence the call to synchronously drain 
                        // the Dispatcher queue.
                        //
                        Dispatcher.Invoke((Action)delegate(){}, System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
            }
        }

        #endregion

        #region Data

        private BindingBase _contentBinding = null;

        #endregion
    }
}
