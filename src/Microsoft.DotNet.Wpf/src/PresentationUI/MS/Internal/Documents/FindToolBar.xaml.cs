// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Code behind file for the DocumentViewer FindToolBar.

using System.Security;

using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.TrustUI;

using System;
using System.Reflection;
using System.Text;
using System.Globalization;

using MS.Internal.Documents.Application;
using MS.Internal.PresentationUI;


namespace MS.Internal.Documents
{
    [FriendAccessAllowed]
    internal partial class FindToolBar
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors
        /// <summary>
        /// Constructor for FindToolBar
        /// </summary>
        public FindToolBar()
        {            
            InitializeComponent();
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Public Properties

        /// <summary>
        /// The text string to search for.  This reflects what is being 
        /// displayed in the "Find What" TextBox.
        /// </summary>
        /// <value></value>
        public string SearchText
        {
            get { return FindTextBox.Text; }
        }

        /// <summary>
        /// Specifies the direction of the search.
        /// If true, then the search will be in an upwardly direction
        /// from the current position.
        /// Otherwise it will continue downward.
        /// </summary>
        /// <value></value>
        public bool SearchUp
        {
            get
            {
                return _searchUp;
            }
            set
            {
                //Our parent control can set this, too.
                if (_searchUp != value)
                {
                    _searchUp = value;
                }
            }
        }

        /// <summary>
        /// Specifies whether the search should be case sensitive.
        /// </summary>
        /// <value></value>
        public bool MatchCase
        {
            get { return OptionsCaseMenuItem.IsChecked == true; }
        }

        /// <summary>
        /// Specifies whether the search should only consider whole word
        /// matches.
        /// </summary>
        /// <value></value>
        public bool MatchWholeWord
        {
            get { return OptionsWholeWordMenuItem.IsChecked == true; }
        }
        /// <summary>
        /// Specifies whether the search should match diacritics.
        /// </summary>
        /// <value></value>
        public bool MatchDiacritic
        {
            get { return OptionsDiacriticMenuItem.IsChecked == true; }
        }

        /// <summary>
        /// Specifies whether the search should match kashida.
        /// </summary>
        /// <value></value>
        public bool MatchKashida
        {
            get { return OptionsKashidaMenuItem.IsChecked == true; }
        }

        /// <summary>
        /// Specifies whether the search should match alef hamza.
        /// </summary>
        /// <value></value>
        public bool MatchAlefHamza
        {
            get { return OptionsAlefHamzaMenuItem.IsChecked == true; }
        }

        /// <summary>
        /// Specified by the parent application, if a document is available
        /// to be searched.
        /// </summary>
        /// <value></value>
        public bool DocumentLoaded
        {
            set
            {
                if (_documentLoaded != value)
                {
                    _documentLoaded = value;
                    UpdateButtonState();
                }
            }
        }

        /// <summary>
        /// Only if a document has been loaded will the 
        /// Find functionality be enabled.
        /// </summary>
        public bool FindEnabled
        {
            get
            {
                return _documentLoaded;
            }
        }

        #endregion Public Properties
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        #region Public Events

        /// <summary>
        /// The FindClicked event is fired when the "Find Next" button in the
        /// Dialog is clicked.
        /// </summary>
        public event EventHandler FindClicked;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Public Methods

        /// <summary>
        /// GoToTextBox gives the Find TextBox focus and selects all
        /// of the Text inside.
        /// </summary>
        public void GoToTextBox()
        {
            // Fire a dispatcher job to focus the TextBox.  This is done to support times when the
            // FindToolBar was collapsed, since the focus cannot be given to a non-visible element.
            // The dispatcher will call OnGoToTextBox after the now visible FindToolBar has been
            // rendered.
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(OnGoToTextBox), null);
        }

        #endregion  Public Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        /// <summary>
        /// Called to give the FindTextBox focus using a dispatcher.
        /// </summary>
        /// <param name="param">Not used.</param>
        /// <returns>null</returns>
        private object OnGoToTextBox(object param)
        {
            FindTextBox.Focus();
            return null;
        }

        /// <summary>
        /// Handles the TextChanged event for the FindTextBox.
        /// Determines if the FindNext and FindPrevious buttons should
        /// be enabled or not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFindTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            // If there is Text in the TextBox, then hide our label.
            // Otherwise, make sure it is visible.
            if (FindTextBox.Text.Length >= 1)
            {
                FindTextLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                FindTextLabel.Visibility = Visibility.Visible;
            }

            // Now ensure the button state is accurate.
            UpdateButtonState();
        }

        /// <summary>
        /// This will watch for the Return key being pressed and invoke
        /// a Find action if it has.
        /// </summary>
        /// <param name="sender">A reference to the sender of the event.</param>
        /// <param name="e">Arguments to the event, used for the key reference.</param>
        private void OnFindTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // If the find action is enabled and
            // if the return key has been pressed,
            // then invoke a find action.

            // Key.Return also covers Key.Enter
            if ((FindEnabled) &&
                (e != null) &&
                (e.Key == Key.Return || e.Key == Key.Execute))
            {
                e.Handled = true;
                OnFindClick();
            }
        }

        /// <summary>
        /// Updates the enabled state for the FindNext and FindPrevious 
        /// buttons.  Only enable the buttons if there is
        /// is at least one character in the FindTextBox, and a
        /// document has been loaded.
        /// </summary>
        private void UpdateButtonState()
        {
            if (FindNextButton != null)
            {
                FindNextButton.IsEnabled = FindEnabled;
            }

            if (FindPreviousButton != null)
            {
                FindPreviousButton.IsEnabled = FindEnabled;
            }
        }

        /// <summary>
        /// Handles the Click event for the "Find Next" button.
        /// Fires off our dialog's FindClicked event so that DocumentViewer knows when the
        /// button has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFindNextClick(object sender, EventArgs e)
        {
            // Mark our Search Up bool as false.
            _searchUp = false;
            
            //Fire our FindClicked event.
           OnFindClick();
        }        
        
        /// <summary>
        /// Handles the Click event for the "Find Previous" button.
        /// Fires off our dialog's FindClicked event so that DocumentViewer knows when the
        /// button has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFindPreviousClick(object sender, EventArgs e)
        {
            // Mark our Search Up bool as true.
            _searchUp = true;
            
            //Fire our FindClicked event.
           OnFindClick();
        }

        /// <summary>
        /// Handles the Click event for the "Find Next" and "Find Previous" button.
        /// Fires off our dialog's FindClicked event so that DocumentViewer knows when the
        /// button has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFindClick()
        {
            //Fire our FindClicked event.
            FindClicked(this, EventArgs.Empty);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        private bool    _searchUp;                     // Search up the document?
        private bool    _documentLoaded;               // Do we have a document to search?
       
        #endregion Private Fields
        
    }
}
