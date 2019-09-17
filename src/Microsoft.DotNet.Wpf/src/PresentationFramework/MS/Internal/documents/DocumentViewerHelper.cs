// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines various helper methods used by document viewews.
//

using System;                           // EventHandler
using System.Windows;                   // Visibility
using System.Windows.Controls;          // Border
using System.Windows.Controls.Primitives;   // PlacementMode
using System.Windows.Input;             // KeyboardNavigation
using System.Windows.Documents;         // ITextRange
using System.Windows.Media;             // VisualTreeHelper
using System.Security;                  // SecurityCritical, SecurityTreatAsSafe
using System.Globalization;             // CultureInfo
using System.Windows.Markup;            // XmlLanguage
using System.Windows.Interop;           // HwndSource

namespace MS.Internal.Documents
{
    /// <summary>
    /// Defines various helper methods used by document viewews.
    /// </summary>
    internal static class DocumentViewerHelper
    {
        //-------------------------------------------------------------------
        //
        //  Find Support
        //
        //-------------------------------------------------------------------

        #region Find Support

        /// <summary>
        /// Enables/disables the FindToolbar.
        /// </summary>
        /// <param name="findToolBarHost">FindToolBar host.</param>
        /// <param name="handlerFindClicked">Event handler for FindClicked event.</param>
        /// <param name="enable">Whether to enable/disable FindToolBar.</param>
        internal static void ToggleFindToolBar(Decorator findToolBarHost, EventHandler handlerFindClicked, bool enable)
        {
            if (enable)
            {
                // Create FindToolBar and attach it to the host.
                FindToolBar findToolBar = new FindToolBar();
                findToolBarHost.Child = findToolBar;
                findToolBarHost.Visibility = Visibility.Visible;
                KeyboardNavigation.SetTabNavigation(findToolBarHost, KeyboardNavigationMode.Continue);
                FocusManager.SetIsFocusScope(findToolBarHost, true);

                // Initialize FindToolBar
                findToolBar.SetResourceReference(Control.StyleProperty, FindToolBarStyleKey);
                findToolBar.FindClicked += handlerFindClicked;
                findToolBar.DocumentLoaded = true;
                findToolBar.GoToTextBox();
            }
            else
            {
                // Reset FindToolBar state to its initial state.
                FindToolBar findToolBar = findToolBarHost.Child as FindToolBar;
                findToolBar.FindClicked -= handlerFindClicked;
                findToolBar.DocumentLoaded = false;

                // Remov FindToolBar form its host.
                findToolBarHost.Child = null;
                findToolBarHost.Visibility = Visibility.Collapsed;
                KeyboardNavigation.SetTabNavigation(findToolBarHost, KeyboardNavigationMode.None);
                findToolBarHost.ClearValue(FocusManager.IsFocusScopeProperty);
            }
        }

        /// <summary>
        /// Invoked when the "Find" button in the Find Toolbar is clicked.
        /// This method invokes the actual Find process.
        /// </summary>
        internal static ITextRange Find(FindToolBar findToolBar, TextEditor textEditor, ITextView textView, ITextView masterPageTextView)
        {
            string searchText;
            FindFlags findFlags;
            ITextContainer textContainer;
            ITextRange textSelection;
            ITextPointer contentStart;
            ITextPointer contentEnd;
            ITextPointer startPointer = null;
            ITextRange findResult = null;

            Invariant.Assert(findToolBar != null);
            Invariant.Assert(textEditor != null);

            // Set up our FindOptions from the options in the Find Toolbar.
            findFlags = FindFlags.None;
            findFlags |= (findToolBar.SearchUp ? FindFlags.FindInReverse : FindFlags.None);
            findFlags |= (findToolBar.MatchCase ? FindFlags.MatchCase : FindFlags.None);
            findFlags |= (findToolBar.MatchWholeWord ? FindFlags.FindWholeWordsOnly : FindFlags.None);
            findFlags |= (findToolBar.MatchDiacritic ? FindFlags.MatchDiacritics : FindFlags.None);
            findFlags |= (findToolBar.MatchKashida ? FindFlags.MatchKashida : FindFlags.None);
            findFlags |= (findToolBar.MatchAlefHamza ? FindFlags.MatchAlefHamza : FindFlags.None);

            // Get the text container for our content.
            textContainer = textEditor.TextContainer;
            textSelection = textEditor.Selection;

            // Initialize other Find parameters
            searchText = findToolBar.SearchText;
            CultureInfo cultureInfo = GetDocumentCultureInfo(textContainer);

            // The find behavior below is defined in section 2.2.3 of this spec:
            // http://d2/DRX/Development%20Documents/02.01.00%20-%20UI%20Design.DocumentViewer.mht

            // Determine if we have a starting selection
            if (textSelection.IsEmpty)
            {
                if (textView != null && !textView.IsValid)
                {
                    textView = null;
                }

                // Determine if the IP/Selection is in view.
                if (textView != null && textView.Contains(textSelection.Start))
                {
                    // Case 1: Selection is empty and IP is currently visible.
                    // Search from this IP to the start/end of the document.

                    //We treat the start of the selection as the IP.
                    contentStart = findToolBar.SearchUp ? textContainer.Start : textSelection.Start;
                    contentEnd = findToolBar.SearchUp ? textSelection.Start : textContainer.End;
                }
                else
                {
                    // Case 4: Selection is empty and IP is not currently visible.
                    // Search from the top of the current TextView to the end of the document,
                    // if searching down. If searchind up, search from the start of the document
                    // to the end position of the current TextView.
                    if (masterPageTextView != null && masterPageTextView.IsValid)
                    {
                        foreach (TextSegment textSegment in masterPageTextView.TextSegments)
                        {
                            if (textSegment.IsNull)
                            {
                                continue;
                            }

                            if (startPointer == null)
                            {
                                // Set initial masterPointer value.
                                startPointer = !findToolBar.SearchUp ? textSegment.Start : textSegment.End;
                            }
                            else
                            {
                                if (!findToolBar.SearchUp)
                                {
                                    if (textSegment.Start.CompareTo(startPointer) < 0)
                                    {
                                        // Start is before the current masterPointer
                                        startPointer = textSegment.Start;
                                    }
                                }
                                else
                                {
                                    // end is after than the current masterPointer
                                    if (textSegment.End.CompareTo(startPointer) > 0)
                                    {
                                        startPointer = textSegment.End;
                                    }
                                }
                            }
                        }
                    }

                    if (startPointer != null)
                    {
                        // Now build the content range from that pointer to the start/end of the document.
                        // Set content start/end pointer to the content of the find document
                        contentStart = findToolBar.SearchUp ? textContainer.Start : startPointer;
                        contentEnd = findToolBar.SearchUp ? startPointer : textContainer.End;
                    }
                    else
                    {
                        // We were unable to determine the viewing area (form TextView),
                        // just use the entire TextContainer.
                        contentStart = textContainer.Start;
                        contentEnd = textContainer.End;
                    }
                }
            }
            else
            {
                // Determine if the search text is already selected in the document.
                findResult = TextFindEngine.Find(textSelection.Start, textSelection.End, searchText, findFlags, cultureInfo);

                // To see if our Text ranges are the same, we will verify that
                // their start and end points are the same.
                if ((findResult != null) &&
                    (findResult.Start != null) &&
                    (findResult.Start.CompareTo(textSelection.Start) == 0) &&
                    (findResult.End.CompareTo(textSelection.End) == 0))
                {
                    // Case 2: Selection exists and it matches the search text.
                    // Search from the end of the given selection.

                    contentStart = findToolBar.SearchUp ? textSelection.Start : textSelection.End;
                    contentEnd = findToolBar.SearchUp ? textContainer.Start : textContainer.End;
                }
                else
                {
                    // Case 3: Selection exists and it does not match the search text.
                    // Search from the beginning of the given selection to the end of the document.

                    contentStart = findToolBar.SearchUp ? textSelection.End : textSelection.Start;
                    contentEnd = findToolBar.SearchUp ? textContainer.Start : textContainer.End;
                }
            }

            // We should have content. Try to find something.
            findResult = null;
            if (contentStart != null && contentEnd != null && contentStart.CompareTo(contentEnd) != 0)
            {
                // We might legimately have crossed start/end given our logic above.
                // It's easier to untangle the range here.
                if (contentStart.CompareTo(contentEnd) > 0)
                {
                    ITextPointer temp = contentStart;
                    contentStart = contentEnd;
                    contentEnd = temp;
                }

                findResult = TextFindEngine.Find(contentStart, contentEnd, searchText, findFlags, cultureInfo);
                if ((findResult != null) && (!findResult.IsEmpty))
                {
                    textSelection.Select(findResult.Start, findResult.End);
                }
            }

            return findResult;
        }

        /// <summary>
        /// Returns the CultureInfoculture of a TextContainer parent.
        /// </summary>
        private static CultureInfo GetDocumentCultureInfo(ITextContainer textContainer)
        {
            CultureInfo cultureInfo = null;

            if (textContainer.Parent != null)
            {
                XmlLanguage language = (XmlLanguage)textContainer.Parent.GetValue(FrameworkElement.LanguageProperty);
                if (language != null)
                {
                    try
                    {
                        cultureInfo = language.GetSpecificCulture();
                    }
                    catch (InvalidOperationException)
                    {
                        // Someone set a bogus language on the document.
                        cultureInfo = null;
                    }
                }
            }

            if (cultureInfo == null)
            {
                cultureInfo = CultureInfo.CurrentCulture;
            }

            return cultureInfo;
        }

        /// <summary>
        /// Shows Find unsuccessful dialog.
        /// </summary>
        /// <param name="findToolBar">FindToolBar instance.</param>
        internal static void ShowFindUnsuccessfulMessage(FindToolBar findToolBar)
        {
            string messageString;

            // No, we did not find anything. Alert the user.
            messageString = findToolBar.SearchUp ?
                        SR.Get(SRID.DocumentViewerSearchUpCompleteLabel) :
                        SR.Get(SRID.DocumentViewerSearchDownCompleteLabel);
            messageString = String.Format(System.Globalization.CultureInfo.CurrentCulture, messageString, findToolBar.SearchText);

            HwndSource hwndSource = PresentationSource.CriticalFromVisual(findToolBar) as HwndSource;
            IntPtr hwnd = (hwndSource != null) ? hwndSource.CriticalHandle : IntPtr.Zero;

            PresentationFramework.SecurityHelper.ShowMessageBoxHelper(
                hwnd,
                messageString,
                SR.Get(SRID.DocumentViewerSearchCompleteTitle),
                MessageBoxButton.OK,
                MessageBoxImage.Asterisk);
        }

        /// <summary>
        /// Key used to mark the style for use by the FindToolBar
        /// </summary>
        private static ResourceKey FindToolBarStyleKey
        {
            get
            {
                if (_findToolBarStyleKey == null)
                {
                    _findToolBarStyleKey = new ComponentResourceKey(typeof(PresentationUIStyleResources), "PUIFlowViewers_FindToolBar");
                }
                return _findToolBarStyleKey;
            }
        }
        private static ResourceKey _findToolBarStyleKey;

        #endregion Find Support

        /// <summary>
        /// Returns if the given child instance is a logical descendent of parent.
        /// </summary>
        internal static bool IsLogicalDescendent(DependencyObject child, DependencyObject parent)
        {
            while (child != null)
            {
                if (child == parent)
                {
                    return true;
                }
                child = LogicalTreeHelper.GetParent(child);
            }
            return false;
        }

        /// <summary>
        /// KeyDown handler used by flow viewers.
        /// </summary>
        internal static void KeyDownHelper(KeyEventArgs e, DependencyObject findToolBarHost)
        {
            // Only process key events if they haven't been handled.
            if (!e.Handled && findToolBarHost != null)
            {
                // If arrow key is pressed, check if KeyboardNavigation is moving focus within
                // FindToolBar. In such case move the focus and mark the event as handled.
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
                {
                    DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
                    if (focusedElement != null && focusedElement is Visual &&
                        VisualTreeHelper.IsAncestorOf(findToolBarHost, focusedElement))
                    {
                        FocusNavigationDirection direction = KeyboardNavigation.KeyToTraversalDirection(e.Key);
                        DependencyObject predictedFocus = KeyboardNavigation.Current.PredictFocusedElement(focusedElement, direction);
                        // If PredictedFocus is within FindToolBar, move the focus to PredictedFocus and handle
                        // the event. Otherwise do not handle the event and let the viewer to do 
                        // its default logic.
                        if (predictedFocus != null && predictedFocus is IInputElement &&
                            VisualTreeHelper.IsAncestorOf(findToolBarHost, focusedElement))
                        {
                            ((IInputElement)predictedFocus).Focus();
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when ContextMenuOpening is raised on FlowDocument viewer element.
        /// </summary>
        internal static void OnContextMenuOpening(FlowDocument document, Control viewer, ContextMenuEventArgs e)
        {
            // Get ContextMenu from TargetElement, if exests. Otherwise get ContextMenu from the viewer.
            ContextMenu cm = null;
            if (e.TargetElement != null)
            {
                cm = e.TargetElement.GetValue(FrameworkElement.ContextMenuProperty) as ContextMenu;
            }
            if (cm == null)
            {
                cm = viewer.ContextMenu;
            }

            // Add special handling for ContextMenu, if invoked through a hotkey.
            if (cm != null)
            {
                if (document != null)
                {
                    // A negative offset for e.CursorLeft means the user invoked
                    // the menu with a hotkey (shift-F10).
                    // For this case place the menu relative to Selection.Start,
                    // otherwise do not modify it.
                    if (DoubleUtil.LessThan(e.CursorLeft, 0))
                    {
                        // Retrieve desired ContextMenu position. If the TextSelection is not empty and visible, 
                        // use selection start position. Otherwise prefer TargetElements's start, if provided.
                        ITextContainer textContainer = (ITextContainer)((IServiceProvider)document).GetService(typeof(ITextContainer));
                        ITextPointer contextMenuPosition = null;
                        if (textContainer.TextSelection != null)
                        {
                            if ((textContainer.TextSelection.IsEmpty || !textContainer.TextSelection.TextEditor.UiScope.IsFocused) &&
                                e.TargetElement is TextElement)
                            {
                                contextMenuPosition = ((TextElement)e.TargetElement).ContentStart;
                            }
                            else
                            {
                                // Selection start is always normalized to have backward LogicalDirection. However, if selection starts at the beginning
                                // of a line this will cause the text view to return rectangle on the previous line. So we need to switch  logical direction.
                                contextMenuPosition = textContainer.TextSelection.Start.CreatePointer(LogicalDirection.Forward);
                            }
                        }
                        else if (e.TargetElement is TextElement)
                        {
                            contextMenuPosition = ((TextElement)e.TargetElement).ContentStart;
                        }

                        // If ContextMenu position has been found and it is visible, show ContextMenu there.
                        // Otherwise let default ContextMenu handling logic handle this event.
                        ITextView textView = textContainer.TextView;
                        if (contextMenuPosition != null && textView != null && textView.IsValid && textView.Contains(contextMenuPosition))
                        {
                            Rect positionRect = textView.GetRectangleFromTextPosition(contextMenuPosition);
                            if (positionRect != Rect.Empty)
                            {
                                positionRect = DocumentViewerHelper.CalculateVisibleRect(positionRect, textView.RenderScope);
                                if (positionRect != Rect.Empty)
                                {
                                    GeneralTransform transform = textView.RenderScope.TransformToAncestor(viewer);
                                    Point contextMenuOffset = transform.Transform(positionRect.BottomLeft);
                                    cm.Placement = PlacementMode.Relative;
                                    cm.PlacementTarget = viewer;
                                    cm.HorizontalOffset = contextMenuOffset.X;
                                    cm.VerticalOffset = contextMenuOffset.Y;
                                    cm.IsOpen = true;
                                    e.Handled = true;
                                }
                            }
                        }
                    }
                }
                if (!e.Handled)
                {
                    // Since we are not handling ContextMenu, clear all the values that
                    // could be set through explicit handling.
                    cm.ClearValue(ContextMenu.PlacementProperty);
                    cm.ClearValue(ContextMenu.PlacementTargetProperty);
                    cm.ClearValue(ContextMenu.HorizontalOffsetProperty);
                    cm.ClearValue(ContextMenu.VerticalOffsetProperty);
                }
            }
        }

        /// <summary>
        /// Calculates visible rectangle taking into account all clips and transforms 
        /// in the visual ancestors chain.
        /// </summary>
        /// <param name="visibleRect">Original rectangle relative to 'visual'.</param>
        /// <param name="originalVisual">Originating visual element.</param>
        internal static Rect CalculateVisibleRect(Rect visibleRect, Visual originalVisual)
        {
            Visual visual = VisualTreeHelper.GetParent(originalVisual) as Visual;
            while (visual != null && visibleRect != Rect.Empty)
            {
                if (VisualTreeHelper.GetClip(visual) != null)
                {
                    GeneralTransform transform = originalVisual.TransformToAncestor(visual).Inverse;
                    // Safer version of transform to descendent (doing the inverse ourself), 
                    // we want the rect inside of our space. (Which is always rectangular and much nicer to work with)
                    if (transform != null)
                    {
                        Rect rectBounds = VisualTreeHelper.GetClip(visual).Bounds;
                        rectBounds = transform.TransformBounds(rectBounds);
                        visibleRect.Intersect(rectBounds);
                    }
                    else
                    {
                        // No visibility if non-invertable transform exists.
                        visibleRect = Rect.Empty;
                    }
                }
                visual = VisualTreeHelper.GetParent(visual) as Visual;
            }
            return visibleRect;
        }
    }

    /// <summary>
    /// State of FlowDocument that has been changed for printing.
    /// </summary>
    internal class FlowDocumentPrintingState
    {
#if !DONOTREFPRINTINGASMMETA
        internal System.Windows.Xps.XpsDocumentWriter XpsDocumentWriter;
#endif //DONOTREFPRINTINGASMMETA
        internal Size PageSize;
        internal Thickness PagePadding;
        internal double ColumnWidth;
        internal bool IsSelectionEnabled;
    }
}
