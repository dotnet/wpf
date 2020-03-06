// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: PageTextBox is a derived TextBox with some extra 
//              functionality for the Page related behaviours of 
//              DocumentApplicationUI.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;         // For event args
using System.Windows.TrustUI;       // For string resources

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// A derived TextBox with some extra functionality for the Page related behaviours of DocumentApplicationUI.
    /// </summary>
    internal sealed class PageTextBox : TextBox
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Default PageTextBox constructor.
        /// </summary>
        internal PageTextBox()
        {
            // Set any TextBox properties.
            SetDefaults();

            // Setup any TextBox event handlers.
            SetHandlers();
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Public Properties
        public static readonly DependencyProperty PageNumberProperty =
            DependencyProperty.Register(
                    "PageNumber",
                    typeof(int),
                    typeof(PageTextBox),
                    new FrameworkPropertyMetadata(
                            _pageNumberDefault, //default value
                            FrameworkPropertyMetadataOptions.None, //MetaData flags
                            new PropertyChangedCallback(OnPageNumberChanged)));  //changed callback

        /// <summary>
        /// Sets the page number to be displayed in the PageTextBox.
        /// </summary>
        public void SetPageNumber(int pageNumber)
        {
            if (CultureInfo.CurrentCulture != null)
            {
                this.Text = pageNumber.ToString(CultureInfo.CurrentCulture);

                // If this is currently focused, refocus to reset text selection.
                if (IsFocused)
                {
                    SelectAll();
                }
            }
        }

        #endregion Public Properties
        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events
        /// <summary>
        /// This event will be fired anytime a user was editing the TextBox but applied
        /// the new value (ie pressed enter, or tab).
        /// </summary>
        internal event EventHandler PageNumberEdited;

        /// <summary>
        /// This will fire a PageNumberEdited event if required.
        /// </summary>
        internal void OnPageNumberEdited()
        {
            // Check if the TextBox is being edited
            if (_isEditingText)
            {
                _isEditingText = false;

                // Since the TextBox was being edited, fire a cancelled event so that the value
                // may be applied (if desired by the UI).
                PageNumberEdited(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// This event will be fired anytime a user was editing the TextBox but chose not to apply
        /// the new value (ie change of focus, or escape pressed).
        /// </summary>
        internal event EventHandler PageNumberEditCancelled;

        /// <summary>
        /// This will fire a PageNumberEditCancelled event if required.
        /// </summary>
        internal void OnPageNumberEditCancelled()
        {
            // Check if the TextBox is being edited
            if (_isEditingText)
            {
                _isEditingText = false;

                // Since the TextBox was being edited, fire a cancelled event so that the value
                // may be reset (if desired by the UI).
                PageNumberEditCancelled(this, EventArgs.Empty);
            }
        }
        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new PageTextBoxAutomationPeer(this);
        }

        /// <summary>
        /// Highlight the entire text when the user gives focus to the PageTextBox by
        /// clicking on it.  In this case we must not pass the event to the base handler
        /// since the base handler will place the cursor at the current position after
        /// setting focus.  If we are already focused, then placing the cursor is the
        /// correct behavior so the base handler will be used.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event (not used)</param>
        /// <param name="e">Arguments to the event (not used)</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!IsFocused)
            {
                Focus();
                e.Handled = true;
            }
            else
            {
                base.OnMouseDown(e);
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Select the text when we receive focus.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event (not used)</param>
        /// <param name="e">Arguments to the event (not used)</param>
        private void OnGotFocus(object sender, EventArgs e)
        {
            SelectAll();
        }

        /// <summary>
        /// This will check incoming key presses for 'enter', 'escape', and 'tab' and take the
        /// appropriate action.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event.</param>
        /// <param name="e">Arguments to the event, used for the key reference.</param>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // This will check for the use of 'enter', 'escape' and 'tab' and take the appropriate action

            // Ensure the arguments are not null.
            if (e != null)
            {
                // Check which Key was pressed.
                switch (e.Key)
                {
                    // Erasure keys -- these don't trigger OnPreviewTextInput but should
                    // set _isEditingText nonetheless
                    case Key.Delete:
                    case Key.Back:
                        _isEditingText = true;
                        break;
                        
                    // Submission Keys
                    case Key.Return:  // This also covers: case Key.Enter
                    case Key.Tab:
                    case Key.Execute:
                        // Enter pressed, issue submit, mark input as handled.
                        OnPageNumberEdited();
                        break;

                    // Rejection Keys
                    case Key.Cancel:
                    case Key.Escape:
                        // Escape pressed, issue cancel, mark input as handled.
                        OnPageNumberEditCancelled();
                        break;
                }
            }
        }

        /// <summary>
        /// This will check the characters that have been entered into the TextBox, and restrict it
        /// to only the valid set.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event.</param>
        /// <param name="e">Arguments to the event, used for the input text.</param>
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if ((e != null) && (!String.IsNullOrEmpty(e.Text)) && (IsValidInputChar(e.Text[0])))
            {
                // Set editing mode and allow the ComboBox to handle it.
                _isEditingText = true;
            }
            else
            {
                // Do not allow any remaining characters for text input.
                e.Handled = true;
            }
        }

        /// <summary>
        /// This will validate incoming strings pasted into the textbox, only pasting
        /// valid (all digit) strings.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event.</param>
        /// <param name="e">Arguments to the event, used to determine the input.</param>
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            // Validate the parameters, return if data is null.
            if ((e == null) || (e.DataObject == null) || (String.IsNullOrEmpty(e.FormatToApply)))
            {
                return;
            }

            // Acquire a reference to the new string.
            string incomingString = e.DataObject.GetData(e.FormatToApply) as string;

            if (IsValidInputString(incomingString))
            {
                // Since the new content is valid set the PageTextBox as in edit mode
                // and allow the text to be processed normally (ie don't CancelCommand).
                _isEditingText = true;
            }
            else
            {
                // Cancel the paste if the string is null, empty, or otherwise invalid.
                e.Handled = true;
                e.CancelCommand();
            }
        }

        /// <summary>
        /// Checks if the given string contains valid characters for this control.  Used for pasting
        /// and UI Automation.
        /// </summary>
        /// <param name="value">The string to test.</param>
        private bool IsValidInputString(string incomingString)
        {
            if (String.IsNullOrEmpty(incomingString))
            {
                return false;
            }
            // Check that each character is valid (a digit).
            foreach (char c in incomingString.ToCharArray())
            {
                if (!(IsValidInputChar(c)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if the character is valid for input to this control.
        /// </summary>
        /// <param name="c">The character to test.</param>
        private bool IsValidInputChar(char c)
        {
            // This will limit which characters are allowed to be entered into the TextBox
            // Currently this is limited to 0-9
            // This method (by using IsDigit) is localization independent, since (as localization
            // informed me) this avoids worrying about the mapping of the keys, and instead the
            // actual value that will be input.
            return (Char.IsDigit(c));
        }

        /// <summary>
        /// Callback for the PageNumber DependencyProperty.  When this event is fired
        /// PageTextBox updates the page number it is displaying.
        /// </summary>
        /// <param name="d">The PageTextBox to update.</param>
        /// <param name="e">The associated arguments.</param>
        private static void OnPageNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PageTextBox PageTextBox = (PageTextBox)d;

            PageTextBox.SetPageNumber((int)e.NewValue);
        }

        /// <summary>
        /// Set the default ComboBox properties
        /// </summary>
        private void SetDefaults()
        {
            ToolTip = SR.Get(SRID.PageTextBoxToolTip);
            IsReadOnly = false;
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            TextAlignment = TextAlignment.Right;
            Padding = new Thickness(1);
            Margin = new Thickness(0);

            // Since PageTextBox only supports input as numbers, we should disable
            // IME options so that we don't need multiple Enter presses to parse 
            // the input.
            InputMethod.SetIsInputMethodEnabled(this, false);
        }

        /// <summary>
        /// Attach any needed TextBox event handlers.
        /// </summary>
        private void SetHandlers()
        {
            GotFocus += OnGotFocus;
            PreviewTextInput += OnPreviewTextInput;
            PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(this, new DataObjectPastingEventHandler(OnPaste));
        }
        #endregion Private Methods

        #region Nested Classes
        /// <summary>
        /// AutomationPeer associated with PageTextBox
        /// </summary>
        private class PageTextBoxAutomationPeer : TextBoxAutomationPeer, IValueProvider
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner">Owner of the AutomationPeer.</param>
            public PageTextBoxAutomationPeer(PageTextBox owner)
                : base(owner)
            { }

            /// <summary>
            /// <see cref="AutomationPeer.GetClassNameCore"/>
            /// </summary>
            override protected string GetClassNameCore()
            {
                return "PageTextBox";
            }

            /// <summary>
            /// <see cref="AutomationPeer.GetPattern"/>
            /// </summary>
            override public object GetPattern(PatternInterface patternInterface)
            {
                if (patternInterface == PatternInterface.Value)
                {
                    return this;
                }
                else
                {
                    return base.GetPattern(patternInterface);
                }
            }

            void IValueProvider.SetValue(string value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (!IsEnabled())
                {
                    throw new ElementNotEnabledException();
                }

                PageTextBox owner = (PageTextBox)Owner;

                if (owner.IsReadOnly)
                {
                    throw new ElementNotEnabledException();
                }

                if (owner.IsValidInputString(value))
                {
                    owner.Text = value;
                    owner._isEditingText = true;
                    owner.OnPageNumberEdited();
                }
            }
        }
        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private bool _isEditingText;
        private const int _pageNumberDefault = 0;
    }
}