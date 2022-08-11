// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    DialogBaseForm:  Base class for all DRP dialogs
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Windows.TrustUI;


namespace MS.Internal.Documents
{
    /// <summary>
    /// DialogBaseForm is the base class for all DRP dialogs 
    /// </summary>
    internal class DialogBaseForm : Form
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// The constructor
        /// </summary>
        public DialogBaseForm()
        {
            // Setup ToolTip object for dialogs
            _toolTip = new ToolTip();
            _toolTip.Active = true;
            _toolTip.ShowAlways = true;

            InitializeComponent();
            ApplyStyle();
            ApplyResources();
            ApplyRTL();
        }

        #endregion Constructors

        #region Protected Methods
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// InitializeComponent.
        /// </summary>
        protected virtual void InitializeComponent()
        {
        }

        /// <summary>
        /// ApplyStyle.
        /// </summary>
        protected virtual void ApplyStyle()
        {
            ApplyDialogFont(this);

            // Setup the visual styles for our winform dialog.
            System.Windows.Forms.Application.EnableVisualStyles();

            // Set the default background color
            BackColor = System.Drawing.SystemColors.Control;  
        }

        /// <summary>
        /// ApplyResources.
        /// </summary>
        protected virtual void ApplyResources()
        {
            Icon = (System.Drawing.Icon)Resources.DocumentApplication;
        }

        #endregion Protected Methods

        #region Private Methods
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Method for applying font to all controls on the form.
        /// </summary>
        private void ApplyDialogFont(Control control)
        {
            //loop through all child controls and apply font
            foreach (Control c in control.Controls)
            {
                ApplyDialogFont(c);

                //Set the Font
                //Note:  This doesn't handle menus or icons (currently DRP dialogs don't have these)
                c.Font = System.Drawing.SystemFonts.DialogFont;

                // Switch to GDI rendering for all controls that support it
                
                if (c is Label)
                {
                    (c as Label).UseCompatibleTextRendering = false;
                    
                    if (c is LinkLabel)
                    {
                        (c as LinkLabel).UseCompatibleTextRendering = false;
                    }
                }
                else if (c is ButtonBase)
                {
                    (c as ButtonBase).UseCompatibleTextRendering = false;
                }
                else if (c is CheckedListBox)
                {
                    (c as CheckedListBox).UseCompatibleTextRendering = false;
                }
                else if (c is GroupBox)
                {
                    (c as GroupBox).UseCompatibleTextRendering = false;
                }
                else if (c is PropertyGrid)
                {
                    ApplyDialogFontToPropertyGrid(c);
                }
            }
        }
        
        private void ApplyDialogFontToPropertyGrid(Control control)
        {
            (control as PropertyGrid).UseCompatibleTextRendering = false;
        }

        /// <summary>
        /// Applies the WinForms RightToLeft and RightToLeftLayout properties based on the FlowDirection of 
        /// DocumentApplicationDocumentViewer.
        /// </summary>
        private void ApplyRTL()
        {
            // Get the UI Language from the string table
            string uiLanguage = SR.Get(SRID.WPF_UILanguage);
            Invariant.Assert(!string.IsNullOrEmpty(uiLanguage), "No UILanguage was specified in stringtable.");

            // Set this dialog's RTL property based on the RTL property for the 
            // language specified in the string table.
            CultureInfo uiCulture = new CultureInfo(uiLanguage);
            if ( uiCulture.TextInfo.IsRightToLeft )
            {
                RightToLeft = RightToLeft.Yes;
                RightToLeftLayout = true;
            }
            else
            {
                RightToLeft = RightToLeft.No;
                RightToLeftLayout = false;
            }

        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Protected fields
        //
        //------------------------------------------------------

        #region Protected Fields
        // The maximum number of characters allowed in the "Location,"
        // "Name," and "Intent" fields for our Signing and RequestedSignature
        // dialogs.
        protected const int _maxLocationLength = 128;
        protected const int _maxNameLength = 128;
        protected const int _maxIntentLength = 256;

        // A reference to the general tooltip object used to assign tooltip
        // strings to controls.
        protected ToolTip _toolTip;

        #endregion Protected Fields
    }
}
