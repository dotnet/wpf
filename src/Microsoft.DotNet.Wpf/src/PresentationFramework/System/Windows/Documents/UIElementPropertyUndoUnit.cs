// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Undo unit for property sets on UIElement. 
// It is created for:
//  1. FlowDirectionProperty sets on a TextEditor's UI scope element.
//  2. HorizontalAlignmentProperty sets on UIElement child of BlockUIContainer.
//

using System;
using System.Windows;
using MS.Internal;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    // 1. Undo unit for FlowDirectionProperty sets on a TextEditor's UI scope element.
    //
    // The TextEditor lets users configure the FlowDirection of text dynamically
    // even when running on TextBoxes (TextEditor.AcceptsRichContent == false).
    // In this case, there's no TextElement to hang property values on, so we must
    // resort to assigning values directly to the TextEditor's UiScope, which is
    // not covered by the TextContainer's undo infrastructure.
    // 
    // This class encapsulates an undo unit used to track FlowDirection changes on
    // plain text documents.
    // 
    // 2. Undo unit for HorizontalAlignmentProperty sets on UIElement child of BlockUIContainer.
    // 
    // When applying TextAlignment property to BlockUIContainer elements in rich text documents, 
    // text alignment must be translated into HorizontalAlignment of its child UIElement. 
    // We must assign this property value directly to the embedded UIElement. 
    // Since TextEditor's undo mechanism for property changes on TextElements in the tree relies 
    // on the OnPropertyChanged event listener, it is unable to track this property change. 
    // Also, it is infeasible to have a OnPropertyChanged listener for all UIElements.
    // So we resort to explicitly adding this special undo unit in such situation.
    // 
    internal class UIElementPropertyUndoUnit : IUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Create a new undo unit instance.
        private UIElementPropertyUndoUnit(UIElement uiElement, DependencyProperty property, object oldValue)
        {
            _uiElement = uiElement;
            _property = property;
            _oldValue = oldValue;
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // Called by the undo manager.  Restores tree state to its condition
        // when the unit was created.
        public void Do()
        {
            if (_oldValue != DependencyProperty.UnsetValue)
            {
                _uiElement.SetValue(_property, _oldValue);
            }
            else
            {
                _uiElement.ClearValue(_property);
            }
        }

        // Called by the undo manager.
        public bool Merge(IUndoUnit unit)
        {
            Invariant.Assert(unit != null);
            return false;
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Note that following strongly typed overloads of Add are meant to restrict the usage of UIElementPropertyUndoUnit 
        // for tracking changes to only a limited set of properties of UIElements.
        // In general, it is not safe to keep a reference to the original property value, however for enum types, it is safe to do so.

        // Add a new UIElementPropertyUndoUnit to the undo stack for HorizontalAlignment property.
        internal static void Add(ITextContainer textContainer, UIElement uiElement, DependencyProperty property, HorizontalAlignment newValue)
        {
            AddPrivate(textContainer, uiElement, property, newValue);
        }

        // Add a new UIElementPropertyUndoUnit to the undo stack for FlowDirection property.
        internal static void Add(ITextContainer textContainer, UIElement uiElement, DependencyProperty property, FlowDirection newValue)
        {
            AddPrivate(textContainer, uiElement, property, newValue);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Privte Methods
        //
        //------------------------------------------------------

        #region Private Methods
        
        // Private helper that adds a new UIElementPropertyUndoUnit to the undo stack.
        private static void AddPrivate(ITextContainer textContainer, UIElement uiElement, DependencyProperty property, object newValue)
        {
            UndoManager undoManager = TextTreeUndo.GetOrClearUndoManager(textContainer);

            if (undoManager == null)
            {
                return;
            }

            object currentValue = uiElement.ReadLocalValue(property);

            if (currentValue is Expression)
            {
                // Can't undo when old value is an expression, so clear the stack.
                if (undoManager.IsEnabled)
                {
                    undoManager.Clear();
                }
                return;
            }

            if (currentValue.Equals(newValue))
            {
                // No property change.
                return;
            }

            undoManager.Add(new UIElementPropertyUndoUnit(uiElement, property, currentValue));
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // UIElement associated with this undo unit.
        private readonly UIElement _uiElement;

        // DependencyProperty of the UIElement associated with this undo unit.
        private readonly DependencyProperty _property;

        // Original property value.
        private readonly object _oldValue;

        #endregion Private Fields
    }
}