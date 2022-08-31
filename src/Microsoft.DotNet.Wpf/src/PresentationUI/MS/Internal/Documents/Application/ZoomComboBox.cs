// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: A derived ComboBox with some extra functionality 
// for the Zoom behaviours of DocumentApplicationUI.
#pragma warning disable 1634, 1691

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
    /// A derived ComboBox with some extra functionality for the Zoom behaviours of DocumentApplicationUI.
    /// </summary>
    internal sealed class ZoomComboBox : ComboBox
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Static ZoomComboBox constructor
        /// </summary>
        static ZoomComboBox()
        {
            // Override this ComboBox property so that any zoom values that are found in the TextBox
            // (either from user input, or databinding) are not looked up in the drop down list.
            IsTextSearchEnabledProperty.OverrideMetadata(typeof(ZoomComboBox), new FrameworkPropertyMetadata(false));
        }

        /// <summary>
        /// Default ZoomComboBox constructor.
        /// </summary>
        internal ZoomComboBox()
        {
            // Set any ComboBox properties.
            SetDefaults();

            // Setup any ComboBox event handlers.
            SetHandlers();
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties
        /// <summary>
        /// A reference to the TextBox contained within the ZoomComboBox.
        /// </summary>
        public TextBox TextBox
        {
            get
            {
                return _editableTextBox;
            }
        }

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(
                    "Zoom",
                    typeof(double),
                    typeof(ZoomComboBox),
                    new FrameworkPropertyMetadata(
                            _zoomDefault, //default value
                            FrameworkPropertyMetadataOptions.None, //MetaData flags
                            new PropertyChangedCallback(OnZoomChanged)));  //changed callback

        /// <summary>
        /// Process returns true if selection events generated from the ZoomComboBox
        /// should be used to update the actual zoom.  This is usually not the case
        /// since we want to ignore the user going through the combobox with the
        /// arrow keys until they actually apply the selection.
        /// </summary>
        public bool ProcessSelections
        {
            get
            {
                return _processSelections;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        /// <summary>
        /// OnApplyTemplate is called when the ComboBox's Template is applied,
        /// at this point we can get the TextBox from the Template.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _editableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;

            if (_editableTextBox != null)
            {
                _editableTextBox.TextAlignment = TextAlignment.Right;

                // Since ZoomComboBox primarily supports input as numbers, we should disable
                // IME options so that we don't need multiple Enter presses to parse 
                // the input. This means that we don't handle the IME equivalent of %, but 
                // we'll still handle everything else.
                InputMethod.SetIsInputMethodEnabled(_editableTextBox, false);
            }
        }

        /// <summary>
        /// Sets the current Zoom value being displayed in the ComboBox's TextBox.
        /// </summary>
        public void SetZoom(double zoom)
        {
            string zoomString;
            if (ZoomValueToString(zoom, out zoomString))
            {
                this.Text = zoomString;
                _isEditingText = false;
                // If this is currently focused, refocus to reset text selection.
                if ((_editableTextBox != null) && (_editableTextBox.IsFocused))
                {
                    _editableTextBox.SelectAll();
                }

            }
        }
        #endregion Public Methods

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
        internal event EventHandler ZoomValueEdited;

        /// <summary>
        /// This will fire a ZoomValueEdited event if required.
        /// </summary>
        internal void OnZoomValueEdited()
        {
            // Check if the TextBox is being edited
            if (_isEditingText)
            {
                _isEditingText = false;

                // Since the TextBox was being edited, fire a cancelled event so that the value
                // may be applied (if desired by the UI).
                ZoomValueEdited(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// This event will be fired anytime a user was editing the TextBox but chose not to apply
        /// the new value (ie change of focus, or escape pressed).
        /// </summary>
        internal event EventHandler ZoomValueEditCancelled;

        /// <summary>
        /// This will fire a ZoomValueEditCancelled event if required.
        /// </summary>
        internal void OnZoomValueEditCancelled()
        {
            // Check if the TextBox is being edited
            if (_isEditingText)
            {
                _isEditingText = false;

                // Since the TextBox was being edited, fire a cancelled event so that the value
                // may be reset (if desired by the UI).
                ZoomValueEditCancelled(this, EventArgs.Empty);
            }
        }
        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// This will be fired anytime a selection has been made from the list
        /// or a new value has been entered into the TextBox
        /// </summary>
        /// <param name="e">Event args.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            // Only process selections when responding to a mouse click
            if (ProcessSelections)
            {
                // If we were in edit mode, cancel it
                if (_isEditingText)
                {
                    _isEditingText = false;
                }

                // Since an item has been selected from the list, update the ComboBox
                base.OnSelectionChanged(e);

                // Reset the focus to highlight the new value.
                Focus();
            }
        }

        /// <summary>
        /// Clears the current selection whenever the dropdown is opened.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDropDownOpened(EventArgs e)
        {
            SelectedIndex = -1;
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ZoomComboBoxBoxAutomationPeer(this);
        }

        /// <summary>
        /// This will check incoming key presses for 'enter', 'escape', and 'tab' and take the
        /// appropriate action.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event.</param>
        /// <param name="e">Arguments to the event, used for the key reference.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
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
                        if (IsDropDownOpen)
                        {
                            // If the user presses the enter key while the drop down is
                            // open, this is the final selection from the dropdown and
                            // should be applied.  Since the selection of this item
                            // (via up/down) was ignored, first we must copy the selected
                            // value into the TextBox.  Then we process it as if the user
                            // had typed it in.
                            if (SelectedItem != null)
                            {
                                Text = ((ComboBoxItem)SelectedItem).Content.ToString();
                            }
                            _isEditingText = true;
                        }
                        // Enter pressed, issue submit, mark input as handled.
                        OnZoomValueEdited();
                        break;

                    // Rejection Keys
                    case Key.Cancel:
                    case Key.Escape:
                        // Escape pressed, issue cancel, mark input as handled.
                        OnZoomValueEditCancelled();
                        break;
                    case Key.Up:
                    case Key.Down:
                        // Open the drop down when up or down is pressed
                        if (!IsDropDownOpen)
                        {
                            IsDropDownOpen = true;
                            e.Handled = true;
                            // Always open with the first item (400%) selected
                            SelectedIndex = 0;
                        }
                        break;
                }


                if (!e.Handled)
                {
                    base.OnPreviewKeyDown(e);
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        /// <summary>
        /// When the left mouse button is released, the resulting selection (if any) should
        /// be processed.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event (not used)</param>
        /// <param name="e">Arguments to the event, used for the input text (not used)</param>
        private void OnPreviewMouseLeftButtonUp(object sender, EventArgs e)
        {
            _processSelections = true;
        }

        /// <summary>
        /// When the left mouse button press bubbles back up, we're done with it, so we
        /// won't process further selections.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event (not used)</param>
        /// <param name="e">Arguments to the event, used for the input text (not used)</param>
        private void OnMouseLeftButtonUp(object sender, EventArgs e)
        {
            _processSelections = false;
        }

        /// <summary>
        /// This will check the characters that have been entered into the TextBox, and restrict it
        /// to only the valid set.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event.</param>
        /// <param name="e">Arguments to the event, used for the input text.</param>
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // This will limit which characters are allowed to be entered into the TextBox
            // Currently this is limited to 0-9, ',', '.', '%'
            if ((e != null) && (!String.IsNullOrEmpty(e.Text)))
            {
                if (IsValidInputChar(e.Text[0]))
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
                // Since the new content is valid set the ZoomComboBox as in edit mode
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
            // Check that each character is valid
            foreach (char c in incomingString.ToCharArray())
            {
                // If the character is not a digit or acceptable symbol then refuse new content.
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
            // After discussing this with localization this is an approved method for
            // checking for digit input, as it works regardless of the keyboard mapping.
            // The ',' '.' and '%' are allowed, as they are the only other characters that
            // can be displayed (or input) in a percentage.  Localization informed me that
            // although not every culture uses them (ie North America might not use ',' in
            // a percentage) they are the only required characters, and as such we filter
            // to only allow them to be input.
            return (Char.IsDigit(c)) || (c == ',') || (c == '.') || (c == '%');
        }

        /// <summary>
        /// Callback for the Zoom DependencyProperty.
        /// </summary>
        /// <param name="d">The ZoomComboBox to update</param>
        /// <param name="e">The associated arguments.</param>
        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoomComboBox zoomComboBox = (ZoomComboBox)d;

            zoomComboBox.SetZoom((double)e.NewValue);            
        }

        /// <summary>
        /// Converts a double Zoom value to a corresponding string (with % sign)
        /// </summary>
        /// <param name="zoomValue">The zoom value to convert</param>
        /// <param name="zoomString">The converted string value</param>
        /// <returns></returns>
        private static bool ZoomValueToString(double zoomValue, out string zoomString)
        {
            // Check that value is a valid double.
            if (!(double.IsNaN(zoomValue)) && !(double.IsInfinity(zoomValue)))
            {
                try
                {
                    // Ensure output string is formatted to current globalization standards.
                    zoomString = String.Format(CultureInfo.CurrentCulture,
                        SR.Get(SRID.ZoomPercentageConverterStringFormat), zoomValue);
                    return true;
                }
                // Allow empty catch statements.
#pragma warning disable 56502

                catch (ArgumentNullException) { }
                catch (FormatException) { }

                // Disallow empty catch statements.
#pragma warning restore 56502
            }

            // Invalid zoom value encountered.
            zoomString = String.Empty;
            return false;
        }

        /// <summary>
        /// Set the default ComboBox properties
        /// </summary>
        private void SetDefaults()
        {
            ToolTip = SR.Get(SRID.ZoomComboBoxToolTip);
            IsReadOnly = false;
            IsEditable = true;
            IsTabStop = false;
            IsTextSearchEnabled = false;
        }

        /// <summary>
        /// Attach any needed ComboBox event handlers.
        /// </summary>
        private void SetHandlers()
        {
            PreviewTextInput += OnPreviewTextInput;
            PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            AddHandler(ComboBox.MouseLeftButtonUpEvent, new RoutedEventHandler(OnMouseLeftButtonUp), true);
            DataObject.AddPastingHandler(this, new DataObjectPastingEventHandler(OnPaste));
        }
        #endregion Private Methods

        #region Nested Classes
        /// <summary>
        /// AutomationPeer associated with ZoomComboBox
        /// </summary>
        private class ZoomComboBoxBoxAutomationPeer : ComboBoxAutomationPeer, IValueProvider
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner">Owner of the AutomationPeer.</param>
            public ZoomComboBoxBoxAutomationPeer(ZoomComboBox owner)
                : base(owner)
            { }

            /// <summary>
            /// <see cref="AutomationPeer.GetClassNameCore"/>
            /// </summary>
            override protected string GetClassNameCore()
            {
                return "ZoomComboBox";
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

                ZoomComboBox owner = (ZoomComboBox)Owner;

                if (owner.IsReadOnly)
                {
                    throw new ElementNotEnabledException();
                }

                if (owner.IsValidInputString(value))
                {
                    owner.Text = value;
                    owner._isEditingText = true;
                    owner.OnZoomValueEdited();
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
        private bool _processSelections = false;
        private TextBox _editableTextBox;
        private const string _editableTextBoxName = "PART_EditableTextBox";
        private const double _zoomDefault = 0.0;
    }
}
