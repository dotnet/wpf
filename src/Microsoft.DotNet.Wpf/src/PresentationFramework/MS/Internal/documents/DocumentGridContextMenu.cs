// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Context menu for DocumentGrid
//

namespace MS.Internal.Documents
{
    using MS.Internal;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using System.Runtime.InteropServices;
    using System.Security;
    using MS.Internal.Documents;
    using MS.Win32;
    using System.Windows.Interop;

    // A Component of DocumentViewer supporting the default ContextMenu.
    internal static class DocumentGridContextMenu
    {
        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        // Registers the event handler for DocumentGrid.
        internal static void RegisterClassHandler()
        {
            EventManager.RegisterClassHandler(typeof(DocumentGrid), FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpening));
            EventManager.RegisterClassHandler(typeof(DocumentApplicationDocumentViewer), FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnDocumentViewerContextMenuOpening));
        }

        #endregion Class Internal Methods        

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Callback for FrameworkElement.ContextMenuOpeningEvent, when fired from DocumentViewer.  This is
        /// here to catch the event when it is fired by the keyboard rather than the mouse.
        /// </summary>
        private static void OnDocumentViewerContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (e.CursorLeft == KeyboardInvokedSentinel)
            {
                DocumentViewer dv = sender as DocumentViewer;
                if (dv != null && dv.ScrollViewer != null)
                {
                    OnContextMenuOpening(dv.ScrollViewer.Content, e);
                }
            }
        }

        // Callback for FrameworkElement.ContextMenuOpeningEvent.
        // If the control is using the default ContextMenu, we initialize it
        // here.
        private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DocumentGrid documentGrid = sender as DocumentGrid;
            ContextMenu contextMenu;

            if (documentGrid == null)
            {
                return;
            }

            // We only want to programmatically generate the menu for Mongoose
            if (!(documentGrid.DocumentViewerOwner is DocumentApplicationDocumentViewer))
                return;

            // If the DocumentViewer or ScrollViewer has a ContextMenu set, the DocumentGrid menu should be ignored
            if (documentGrid.DocumentViewerOwner.ContextMenu != null || documentGrid.DocumentViewerOwner.ScrollViewer.ContextMenu != null)
                return;

            // Start by grabbing whatever's set to the UiScope's ContextMenu property.
            contextMenu = documentGrid.ContextMenu;

            // If someone explicitly set it null -- don't mess with it.
            if (documentGrid.ReadLocalValue(FrameworkElement.ContextMenuProperty) == null)
                return;

            // If it's not null, someone's overriding our default -- don't mess with it.
            if (contextMenu != null)
                return;

            // It's a default null, so spin up a temporary ContextMenu now.
            contextMenu = new ViewerContextMenu();
            contextMenu.Placement = PlacementMode.RelativePoint;
            contextMenu.PlacementTarget = documentGrid;
            ((ViewerContextMenu)contextMenu).AddMenuItems(documentGrid, e.UserInitiated);

            Point uiScopeMouseDownPoint;
            if (e.CursorLeft == KeyboardInvokedSentinel)
            {
                uiScopeMouseDownPoint = new Point(.5 * documentGrid.ViewportWidth, .5 * documentGrid.ViewportHeight);
            }
            else
            {
                uiScopeMouseDownPoint = Mouse.GetPosition(documentGrid);
            }

            contextMenu.HorizontalOffset = uiScopeMouseDownPoint.X;
            contextMenu.VerticalOffset = uiScopeMouseDownPoint.Y;

            // This line raises a public event.
            contextMenu.IsOpen = true;

            e.Handled = true;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Constants
        //
        //------------------------------------------------------
        #region Private Constants

        private const double KeyboardInvokedSentinel = -1.0; // e.CursorLeft has this value when the menu is invoked with the keyboard.
        #endregion
        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Default ContextMenu for TextBox and RichTextBox.
        private class ViewerContextMenu : ContextMenu
        {
            // Initialize the context menu.
            // Creates a new instance.
            internal void AddMenuItems(DocumentGrid dg, bool userInitiated)
            {
                this.Name = "ViewerContextMenu";

                SetMenuProperties(new EditorMenuItem(), dg, ApplicationCommands.Copy); // Copy will be marked as user initiated

                // build menu for XPSViewer
                SetMenuProperties(new MenuItem(), dg, ApplicationCommands.SelectAll);

                AddSeparator();

                SetMenuProperties(
                    new MenuItem(),
                    dg,
                    NavigationCommands.PreviousPage,
                    SR.Get(SRID.DocumentApplicationContextMenuPreviousPageHeader),
                    SR.Get(SRID.DocumentApplicationContextMenuPreviousPageInputGesture));

                SetMenuProperties(
                    new MenuItem(),
                    dg,
                    NavigationCommands.NextPage,
                    SR.Get(SRID.DocumentApplicationContextMenuNextPageHeader),
                    SR.Get(SRID.DocumentApplicationContextMenuNextPageInputGesture));

                SetMenuProperties(
                    new MenuItem(),
                    dg,
                    NavigationCommands.FirstPage,
                    null, //menu header
                    SR.Get(SRID.DocumentApplicationContextMenuFirstPageInputGesture));

                SetMenuProperties(
                    new MenuItem(),
                    dg,
                    NavigationCommands.LastPage,
                    null, //menu header
                    SR.Get(SRID.DocumentApplicationContextMenuLastPageInputGesture));

                AddSeparator();

                SetMenuProperties(new MenuItem(), dg, ApplicationCommands.Print);
            }

            private void AddSeparator()
            {
                this.Items.Add(new Separator());
            }

            //Helper to set properties on the menu items based on the command
            private void SetMenuProperties(MenuItem menuItem, DocumentGrid dg, RoutedUICommand command)
            {
                SetMenuProperties(menuItem, dg, command, null, null);
            }

            private void SetMenuProperties(MenuItem menuItem, DocumentGrid dg, RoutedUICommand command, string header, string inputGestureText)
            {
                menuItem.Command = command;
                menuItem.CommandTarget = dg.DocumentViewerOwner; // the text editor expects the commands to come from the DocumentViewer
                if (header == null)
                {
                    menuItem.Header = command.Text; // use default menu text for this command
                }
                else
                {
                    menuItem.Header = header;
                }

                if (inputGestureText != null)
                {
                    menuItem.InputGestureText = inputGestureText;
                }

                menuItem.Name = "ViewerContextMenu_" + command.Name; // does not require localization
                this.Items.Add(menuItem);
            }
        }

        // Default EditorContextMenu item base class.
        // Used to distinguish our items from anything an application
        // may have added.
        private class EditorMenuItem : MenuItem
        {
            internal EditorMenuItem() : base() { }

            internal override void OnClickCore(bool userInitiated)
            {
                OnClickImpl(userInitiated);
            }
        }

        #endregion Private Types
    }
}
