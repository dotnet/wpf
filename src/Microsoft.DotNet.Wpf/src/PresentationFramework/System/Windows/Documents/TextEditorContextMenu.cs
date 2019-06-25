// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A Component of TextEditor supporting the default ContextMenu.
//

namespace System.Windows.Documents
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
    using MS.Win32;
    using System.Windows.Interop;

    // A Component of TextEditor supporting the default ContextMenu.
    internal static class TextEditorContextMenu
    {
        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        // Registers all text editing command handlers for a given control type.
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners)
        {
            if (registerEventListeners)
            {
                EventManager.RegisterClassHandler(controlType, FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpening));
            }
        }

        // Callback for FrameworkElement.ContextMenuOpeningEvent.
        // If the control is using the default ContextMenu, we initialize it
        // here.
        internal static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);
            const double KeyboardInvokedSentinel = -1.0; // e.CursorLeft has this value when the menu is invoked with the keyboard.

            if (This == null || This.TextView == null)
            {
                return;
            }

            // Get the mouse position that base on RenderScope which we will set
            // the caret on the RenderScope.
            Point renderScopeMouseDownPoint = Mouse.GetPosition(This.TextView.RenderScope);
            ContextMenu contextMenu = null;
            bool startPositionCustomElementMenu = false;

            if (This.IsReadOnly)
            {
                // If the TextEditor is ReadOnly, only take action if
                // 1. The selection is non-empty AND
                // 2. The user clicked inside the selection.
                if ((e.CursorLeft != KeyboardInvokedSentinel && !This.Selection.Contains(renderScopeMouseDownPoint)) ||
                    (e.CursorLeft == KeyboardInvokedSentinel && This.Selection.IsEmpty))
                {
                    return;
                }
            }
            else if ((This.Selection.IsEmpty || e.TargetElement is TextElement) &&
                     e.TargetElement != null)
            {
                // Targeted element has its own ContextMenu, don't override it.
                contextMenu = (ContextMenu)e.TargetElement.GetValue(FrameworkElement.ContextMenuProperty);
            }
            else if (e.CursorLeft == KeyboardInvokedSentinel)
            {
                // If the menu was invoked from the keyboard, walk up the tree
                // from the selection.Start looking for a custom menu.
                TextPointer start = GetContentPosition(This.Selection.Start) as TextPointer;
                if (start != null)
                {
                    TextElement element = start.Parent as TextElement;

                    while (element != null)
                    {
                        contextMenu = (ContextMenu)element.GetValue(FrameworkElement.ContextMenuProperty);
                        if (contextMenu != null)
                        {
                            startPositionCustomElementMenu = true;
                            break;
                        }
                        element = element.Parent as TextElement;
                    }
                }
            }

            // Update the selection caret.
            //
            // A negative offset for e.CursorLeft means the user invoked
            // the menu with a hotkey (shift-F10).  Don't mess with the caret
            // unless the user right-clicked.
            if (e.CursorLeft != KeyboardInvokedSentinel)
            {
                if (!TextEditorMouse.IsPointWithinInteractiveArea(This, Mouse.GetPosition(This.UiScope)))
                {
                    // Don't bring up a context menu if the user clicked on non-editable space.
                    return;
                }

                // Don't update the selection caret if we're bringing up a custom UIElement
                // ContextMenu.
                if (contextMenu == null || !(e.TargetElement is UIElement))
                {
                    using (This.Selection.DeclareChangeBlock()) // NB: This raises a PUBLIC EVENT.
                    {
                        // If we're not over the selection, move the caret.
                        if (!This.Selection.Contains(renderScopeMouseDownPoint))
                        {
                            TextEditorMouse.SetCaretPositionOnMouseEvent(This, renderScopeMouseDownPoint, MouseButton.Right, 1 /* clickCount */);
                        }
                    }
                }
            }

            if (contextMenu == null)
            {
                // If someone explicitly set it null -- don't mess with it.
                if (This.UiScope.ReadLocalValue(FrameworkElement.ContextMenuProperty) == null)
                    return;

                // Grab whatever's set to the UiScope's ContextMenu property.
                contextMenu = This.UiScope.ContextMenu;
            }

            // If we are here, it means that either a custom context menu or our default context menu will be opened.
            // Setting this flag ensures that we dont loose selection highlight while the context menu is open.
            This.IsContextMenuOpen = true;

            // If it's not null, someone's overriding our default -- don't mess with it.
            if (contextMenu != null && !startPositionCustomElementMenu)
            {
                // If the user previously raised the ContextMenu with the keyboard,
                // we've left h/v offsets non-zero, and they need to be cleared now
                // for mouse placement to work.
                contextMenu.HorizontalOffset = 0;
                contextMenu.VerticalOffset = 0;

                // Since ContextMenuService doesn't open the menu, it won't fire a ContextMenuClosing event.
                // We need to listen to the Closed event of the ContextMenu itself so we can clear the
                // IsContextMenuOpen flag.  We also do this for the default menu later in this method.
                contextMenu.Closed += new RoutedEventHandler(OnContextMenuClosed);
                return;
            }

            // Complete the composition before creating the editor context menu.
            This.CompleteComposition();

            if (contextMenu == null)
            {
                // It's a default null, so spin up a temporary ContextMenu now.
                contextMenu = new EditorContextMenu();
                ((EditorContextMenu)contextMenu).AddMenuItems(This);
            }
            contextMenu.Placement = PlacementMode.RelativePoint;
            contextMenu.PlacementTarget = This.UiScope;

            ITextPointer position = null;
            LogicalDirection direction;

            // Position the ContextMenu.

            SpellingError spellingError = (contextMenu is EditorContextMenu) ? This.GetSpellingErrorAtSelection() : null;

            if (spellingError != null)
            {
                // If we have a matching speller error at the selection
                // start, position relative to the end of the error.
                position = spellingError.End;
                direction = LogicalDirection.Backward;
            }
            else if (e.CursorLeft == KeyboardInvokedSentinel)
            {
                // A negative offset for e.CursorLeft means the user invoked
                // the menu with a hotkey (shift-F10).  Place the menu
                // relative to Selection.Start.
                position = This.Selection.Start;
                direction = LogicalDirection.Forward;
            }
            else
            {
                direction = LogicalDirection.Forward;
            }

            // Calculate coordinats for the ContextMenu.
            // They must be set relative to UIScope - as EditorContextMenu constructor assumes.
            if (position != null && position.CreatePointer(direction).HasValidLayout)
            {
                double horizontalOffset;
                double verticalOffset;

                GetClippedPositionOffsets(This, position, direction, out horizontalOffset, out verticalOffset);

                contextMenu.HorizontalOffset = horizontalOffset;
                contextMenu.VerticalOffset = verticalOffset;
            }
            else
            {
                Point uiScopeMouseDownPoint = Mouse.GetPosition(This.UiScope);

                contextMenu.HorizontalOffset = uiScopeMouseDownPoint.X;
                contextMenu.VerticalOffset = uiScopeMouseDownPoint.Y;
            }

            // Since ContextMenuService doesn't open the menu, it won't fire a ContextMenuClosing event.
            // We need to listen to the Closed event of the ContextMenu itself so we can clear the
            // IsContextMenuOpen flag.
            contextMenu.Closed += new RoutedEventHandler(OnContextMenuClosed);

            // This line raises a public event.
            contextMenu.IsOpen = true;

            e.Handled = true;
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // We listen to this event to reset TextEditor._isContextMenuOpen flag.
        private static void OnContextMenuClosed(object sender, RoutedEventArgs e)
        {
            UIElement placementTarget = ((ContextMenu)sender).PlacementTarget;

            if (placementTarget != null)
            {
                TextEditor This = TextEditor._GetTextEditor(placementTarget);

                if (This != null)
                {
                    This.IsContextMenuOpen = false;
                    This.Selection.UpdateCaretAndHighlight();
                    ((ContextMenu)sender).Closed -= new RoutedEventHandler(OnContextMenuClosed);
                }
            }
        }

        /// <summary>
        /// Calculates x, y offsets for a ContextMenu based on an ITextPointer and
        /// the viewports of its containers.
        /// </summary>
        private static void GetClippedPositionOffsets(TextEditor This, ITextPointer position, LogicalDirection direction,
            out double horizontalOffset, out double verticalOffset)
        {
            // GetCharacterRect will return the position that base on UiScope.
            Rect positionRect = position.GetCharacterRect(direction);

            // Get the base offsets for our ContextMenu.
            horizontalOffset = positionRect.X;
            verticalOffset = positionRect.Y + positionRect.Height;

            // Clip to the child render scope.
            FrameworkElement element = This.TextView.RenderScope as FrameworkElement;
            if (element != null)
            {
                GeneralTransform transform = element.TransformToAncestor(This.UiScope);
                if (transform != null)
                {
                    ClipToElement(element, transform, ref horizontalOffset, ref verticalOffset);
                }
            }

            // Clip to parent visuals.
            // This is unintuitive -- you might expect parents to have increasingly
            // larger viewports.  But any parent that behaves like a ScrollViewer
            // will have a smaller view port that we need to clip against.
            for (Visual visual = This.UiScope; visual != null; visual = VisualTreeHelper.GetParent(visual) as Visual)
            {
                element = visual as FrameworkElement;
                if (element != null)
                {
                    GeneralTransform transform = visual.TransformToDescendant(This.UiScope);
                    if (transform != null)
                    {
                        ClipToElement(element, transform, ref horizontalOffset, ref verticalOffset);
                    }
                }
            }

            // Clip to the window client rect.
            PresentationSource source = PresentationSource.CriticalFromVisual(This.UiScope);
            IWin32Window window = source as IWin32Window;
            if (window != null)
            {
                IntPtr hwnd = IntPtr.Zero;
                hwnd = window.Handle;

                NativeMethods.RECT rc = new NativeMethods.RECT(0, 0, 0, 0);
                SafeNativeMethods.GetClientRect(new HandleRef(null, hwnd), ref rc);

                // Convert to mil measure units.
                Point minPoint = new Point(rc.left, rc.top);
                Point maxPoint = new Point(rc.right, rc.bottom);

                CompositionTarget compositionTarget = source.CompositionTarget;
                minPoint = compositionTarget.TransformFromDevice.Transform(minPoint);
                maxPoint = compositionTarget.TransformFromDevice.Transform(maxPoint);

                // Convert to local coordinates.
                GeneralTransform transform = compositionTarget.RootVisual.TransformToDescendant(This.UiScope);
                if (transform != null)
                {
                    transform.TryTransform(minPoint, out minPoint);
                    transform.TryTransform(maxPoint, out maxPoint);

                    // Finally, do the clip.
                    horizontalOffset = ClipToBounds(minPoint.X, horizontalOffset, maxPoint.X);
                    verticalOffset = ClipToBounds(minPoint.Y, verticalOffset, maxPoint.Y);
                }

                // ContextMenu code takes care of clipping to desktop.
            }
        }

        // Clips a Point to the ActualWidth/Height of a containing FrameworkElement.
        private static void ClipToElement(FrameworkElement element, GeneralTransform transform,
            ref double horizontalOffset, ref double verticalOffset)
        {
            Point minPoint;
            Point maxPoint;

            Geometry clip = VisualTreeHelper.GetClip(element);

            if (clip != null)
            {
                Rect bounds = clip.Bounds;
                minPoint = new Point(bounds.X, bounds.Y);
                maxPoint = new Point(bounds.X + bounds.Width, bounds.Y + bounds.Height);
            }
            else
            {
                if (element.ActualWidth == 0 && element.ActualHeight == 0)
                {
                    // Some elements, noteably Canvas, have a (0, 0) desired size
                    // and should be ignored.
                    return;
                }

                minPoint = new Point(0, 0);
                maxPoint = new Point(element.ActualWidth, element.ActualHeight);
            }

            transform.TryTransform(minPoint, out minPoint);
            transform.TryTransform(maxPoint, out maxPoint);

            // NB: ClipToBounds will handle the case where transform flips a coordinate
            // axis.  In that case, minPoint.X will be > maxPoint.X.
            horizontalOffset = ClipToBounds(minPoint.X, horizontalOffset, maxPoint.X);
            verticalOffset = ClipToBounds(minPoint.Y, verticalOffset, maxPoint.Y);
        }

        // Clips value to the range min to (max - 1).
        private static double ClipToBounds(double min, double value, double max)
        {
            // If we're clipping against something with an inverted coordinate axis
            // (the common case is an RTL control in an LTR environment), then
            // "min" in the parent space is a max for the child.
            if (min > max)
            {
                double temp = min;
                min = max;
                max = temp;
            }

            if (value < min)
            {
                value = min;
            }
            else if (value >= max)
            {
                value = max - 1;
            }

            return value;
        }

        // Returns a position ajacent to the supplied position, skipping any
        // intermediate Inlines.
        // This is useful for sliding inside the context of adjacent Hyperlinks,
        // Spans, etc.
        private static ITextPointer GetContentPosition(ITextPointer position)
        {
            while (position.GetAdjacentElement(LogicalDirection.Forward) is Inline)
            {
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            return position;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Default ContextMenu for TextBox and RichTextBox.
        private class EditorContextMenu : ContextMenu
        {
            // Initialize the context menu.
            // Creates a new instance.
            internal void AddMenuItems(TextEditor textEditor)
            {
                if (!textEditor.IsReadOnly)
                {
                    if (AddReconversionItems(textEditor))
                    {
                        AddSeparator();
                    }
                }

                if (AddSpellerItems(textEditor))
                {
                    AddSeparator();
                }
                AddClipboardItems(textEditor);
            }
            // Finalizer release the candidate list if it remains.
            ~EditorContextMenu()
            {
                ReleaseCandidateList(null);
            }

            // Called when the ContextMenu is shutting down.
            protected override void OnClosed(RoutedEventArgs e)
            {
                base.OnClosed(e);

                // OnClick for the menu item might be called after the context menu is closed. It depends
                // on how this context menu is created.
                // We will release CandidateList after all the current events are handled.
                DelayReleaseCandidateList();
            }

            // Helper which appends a separator item.
            private void AddSeparator()
            {
                this.Items.Add(new Separator());
            }

            // Appends spell check related items.
            // Returns false if no items are added.
            private bool AddSpellerItems(TextEditor textEditor)
            {
                SpellingError spellingError;
                MenuItem menuItem;

                spellingError = textEditor.GetSpellingErrorAtSelection();
                if (spellingError == null)
                    return false;

                bool addedSuggestion = false;

                foreach (string suggestion in spellingError.Suggestions)
                {
                    menuItem = new EditorMenuItem();
                    TextBlock text = new TextBlock();
                    text.FontWeight = FontWeights.Bold;
                    text.Text = suggestion;
                    menuItem.Header = text;
                    menuItem.Command = EditingCommands.CorrectSpellingError;
                    menuItem.CommandParameter = suggestion;
                    this.Items.Add(menuItem);
                    menuItem.CommandTarget = textEditor.UiScope;

                    addedSuggestion = true;
                }

                if (!addedSuggestion)
                {
                    menuItem = new EditorMenuItem();
                    menuItem.Header = SR.Get(SRID.TextBox_ContextMenu_NoSpellingSuggestions);
                    menuItem.IsEnabled = false;
                    this.Items.Add(menuItem);
                }

                AddSeparator();

                menuItem = new EditorMenuItem();
                menuItem.Header = SR.Get(SRID.TextBox_ContextMenu_IgnoreAll);
                menuItem.Command = EditingCommands.IgnoreSpellingError;
                this.Items.Add(menuItem);
                menuItem.CommandTarget = textEditor.UiScope;

                return true;
            }

            // Add the description to the candidate string of Cicero's
            // reconversion if necessary.
            private string GetMenuItemDescription(string suggestion)
            {
                if (suggestion.Length == 1)
                {
                    if (suggestion[0] == 0x0020)
                    {
                        return SR.Get(SRID.TextBox_ContextMenu_Description_SBCSSpace);
                    }
                    else if (suggestion[0] == 0x3000)
                    {
                        return SR.Get(SRID.TextBox_ContextMenu_Description_DBCSSpace);
                    }
                }
                return null;
            }

            // Appends Cicero reconversion related items.
            // Returns false if no items are added.
            private bool AddReconversionItems(TextEditor textEditor)
            {
                MenuItem menuItem;
                TextStore textStore = textEditor.TextStore;

                if (textStore == null)
                {
                    GC.SuppressFinalize(this);
                    return false;
                }

                ReleaseCandidateList(null);
                _candidateList = new  SecurityCriticalDataClass<UnsafeNativeMethods.ITfCandidateList>(textStore.GetReconversionCandidateList());
                if (CandidateList == null)
                {
                    GC.SuppressFinalize(this);
                    return false;
                }

                int count = 0;
                CandidateList.GetCandidateNum(out count);

                if (count > 0)
                {
                    // Like Winword, we show the first 5 candidates in the context menu.
                    int i;
                    for (i = 0; i < 5 && i < count; i++)
                    {
                        string suggestion;
                        UnsafeNativeMethods.ITfCandidateString candString;

                        CandidateList.GetCandidate(i, out candString);
                        candString.GetString(out suggestion);

                        menuItem = new ReconversionMenuItem(this, i);
                        menuItem.Header = suggestion;
                        menuItem.InputGestureText = GetMenuItemDescription(suggestion);
                        this.Items.Add(menuItem);

                        Marshal.ReleaseComObject(candString);
                    }
                }

                // Like Winword, we show "More" menu to open TIP's candidate list if there are more
                // than 5 candidates.
                if (count > 5)
                {
                    menuItem = new EditorMenuItem();
                    menuItem.Header = SR.Get(SRID.TextBox_ContextMenu_More);
                    menuItem.Command = ApplicationCommands.CorrectionList;
                    this.Items.Add(menuItem);
                    menuItem.CommandTarget = textEditor.UiScope;
                }

                return (count > 0) ? true : false;
            }

            // Appends clipboard related items.
            // Returns false if no items are added.
            private bool AddClipboardItems(TextEditor textEditor)
            {
                MenuItem menuItem;

                menuItem = new EditorMenuItem();
                menuItem.Header = SR.Get(SRID.TextBox_ContextMenu_Cut);
                menuItem.CommandTarget = textEditor.UiScope;
                menuItem.Command = ApplicationCommands.Cut;
                this.Items.Add(menuItem);

                menuItem = new EditorMenuItem();
                menuItem.Header = SR.Get(SRID.TextBox_ContextMenu_Copy);
                menuItem.CommandTarget = textEditor.UiScope;
                menuItem.Command = ApplicationCommands.Copy;
                this.Items.Add(menuItem);

                menuItem = new EditorMenuItem();
                menuItem.Header = SR.Get(SRID.TextBox_ContextMenu_Paste);
                menuItem.CommandTarget = textEditor.UiScope;
                menuItem.Command = ApplicationCommands.Paste;
                this.Items.Add(menuItem);

                return true;
            }

            private void DelayReleaseCandidateList()
            {
                if (CandidateList != null)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ReleaseCandidateList), null);
                }
            }


            private object ReleaseCandidateList(object o)
            {
                if (CandidateList != null)
                {
                    Marshal.ReleaseComObject(CandidateList);
                    _candidateList = null;

                    // We released CandidateList and Finalizer does not need to be called.
                    GC.SuppressFinalize(this);
                }
                return null;
            }

            // ReconversionMenuItem uses this to finalzie the candidate string.

            internal UnsafeNativeMethods.ITfCandidateList CandidateList
            {
                 get
                 {
                     if ( _candidateList == null)
                     {
                         return null;
                     }

                     return _candidateList.Value;
                 }
            }

            // The candidate list for Cicero Reconversion.
            // We need to use same ITfCandidateList object for both listing up and finalizing because
            // the index of the candidate string needs to match.
            private SecurityCriticalDataClass<UnsafeNativeMethods.ITfCandidateList> _candidateList;
        }

        // Default EditorContextMenu item base class.
        // Used to distinguish our items from anything an application
        // may have added.
        private class EditorMenuItem : MenuItem
        {
            internal EditorMenuItem() : base() {}

            internal override void OnClickCore(bool userInitiated)
            {
                OnClickImpl(userInitiated);
            }
        }

        // Reconversion menu item
        // We finalize the candidate in the context menu if one of reconversion menu item is selected.
        private class ReconversionMenuItem : EditorMenuItem
        {
            internal ReconversionMenuItem(EditorContextMenu menu, int index) : base()
            {
                _menu = menu;
                _index = index;
            }

            // OnClick handler.
            // This is called when the item is selected.
            internal override void OnClickCore(bool userInitiated)
            {
                Invariant.Assert(_menu.CandidateList != null);

                try
                {
                    _menu.CandidateList.SetResult(_index, UnsafeNativeMethods.TfCandidateResult.CAND_FINALIZED);
                }
                catch (COMException)
                {
                    // When TextBox.MaxLength is smaller than the candidate item,
                    // TextStore.SetText will reject the insert with E_FAIL and
                    // we end up here.  In this case, we want to silently eat the exception
                    // since it derives from user action and our code.
                    // Bug 107395 is tracking a fundamental fix to the problem, rather
                    // than this workaround.
                }

                // always passes in false for userInitiated. This won't call command manager.
                base.OnClickCore(false);
            }

            // The index for this candidate string.
            private int _index;

            // The context menu of this item.
            private EditorContextMenu _menu;
        }

        #endregion Private Types
    }
}
