// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Used to control an (RM or DigSig) InfoBar and ToolBar item in MongooseUI


using MS.Internal.Documents;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.TrustUI;

namespace MS.Internal.Documents.Application
{

    /// <summary>
    /// The type of StatusInfoItem (DigSig, RM)
    /// </summary>
    internal enum StatusInfoItemType
    {
        Unknown,
        DigSig,
        RM,
    }

    /// <summary>
    /// Class used to control an item of the InfoBar, and the relevant ToolBar Button.
    /// </summary>
    internal class StatusInfoItem
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructs a new StatusInfoItem
        /// </summary>
        /// <param name="type">Type of Item</param>
        /// <param name="infoBarButton">InfoBar button to update</param>
        /// <param name="toolBarButton">ToolBar button to update</param>
        public StatusInfoItem(StatusInfoItemType type, Button infoBarButton, Control toolBarControl)
        {
            // Set Type
            _type = type;

            // If InfoBar button is not null, set references to the Icon and Text.
            if (infoBarButton != null)
            {
                _infoBarButton = infoBarButton;
                _infoBarIcon = infoBarButton.Template.FindName("PUIInfoBarIcon", infoBarButton) as Rectangle;
                _infoBarText = infoBarButton.Template.FindName("PUIInfoBarText", infoBarButton) as TextBlock;
            }
            // Set ToolBar button reference.
            if (toolBarControl != null)
            {
                _toolBarControl = toolBarControl;
            }
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Controls the visibility of the InfoBar button.
        /// </summary>
        public Visibility Visibility
        {
            get
            {
                return _infoBarButton.Visibility;
            }
            set
            {
                _infoBarButton.Visibility = value;
                // When the Visibility is set, notify the DocumentViewer to check the visibility
                // of the entire InfoBar
                if (InfoBarVisibilityChanged != null)
                {
                    InfoBarVisibilityChanged(this, new EventArgs());
                }
            }
        }

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        /// Fired whenever the visibility has been set on the StatusInfoItem, useful for determining
        /// if the InfoBar should be hidden.
        /// </summary>
        public event EventHandler InfoBarVisibilityChanged;

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Used to be notified of possible DigSig status changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnStatusChange(object sender, DocumentSignatureManager.SignatureStatusEventArgs args)
        {
            if ((args != null) && (_type == StatusInfoItemType.DigSig))
            {
                UpdateUI(args.StatusResources);

                // If the Document is not signed, collapse the infobar button.
                if (args.SignatureStatus != SignatureStatus.NotSigned)
                {
                    this.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Visibility = Visibility.Collapsed;
                }
            }
        }
       
        /// <summary>
        /// Used to be notified of possible RM status changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnStatusChange(object sender, DocumentRightsManagementManager.RightsManagementStatusEventArgs args)
        {
            if ((args != null) && (_type == StatusInfoItemType.RM))
            {
                UpdateUI(args.StatusResources);

                // If the Document is not RM protected, collapse the infobar button.
                if (args.RMStatus == RightsManagementStatus.Protected)
                {
                    this.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Visibility = Visibility.Collapsed;
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------       

        /// <summary>
        /// Used to change the Icons and Text of the InfoBarItems and ToolBarButton.        
        /// </summary>
        /// <param name="resources"></param>
        private void UpdateUI(DocumentStatusResources resources)
        {
            if (_infoBarIcon != null)
            {
                // Set the InfoBar Image
                _infoBarIcon.Fill = resources.Image;
            }            
 
            if (_infoBarText != null)
            {
                // Set the InfoBar Text
                _infoBarText.Text = resources.Text;
            }

            if (_infoBarButton != null)
            {
                // Set the InfoBar ToolTip
                _infoBarButton.ToolTip = resources.ToolTip;
            }

            if (_toolBarControl != null)
            {
                // Set the ToolBarButton Image and ToolTip
                _toolBarControl.Background = resources.Image;
                _toolBarControl.ToolTip = resources.ToolTip;
            }
        }


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private StatusInfoItemType      _type;
        private Rectangle               _infoBarIcon;
        private TextBlock               _infoBarText;
        private Button                  _infoBarButton;
        private Control                 _toolBarControl;
    }

}
