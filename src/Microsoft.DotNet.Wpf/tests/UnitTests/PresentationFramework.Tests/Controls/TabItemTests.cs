// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Wpf.UnitTests.Controls;

/// <summary>
/// Unit tests for <see cref="TabItem"/> class.
/// </summary>
public sealed class TabItemTests
{
    /// <summary>
    /// Verifies that TabItem.OnPreviewGotKeyboardFocus does not cause a
    /// StackOverflowException when a GotKeyboardFocus handler on an ancestor
    /// redirects keyboard focus back to the TabItem after MoveFocus has moved
    /// focus into the tab's content.
    ///
    /// The cycle is:
    ///   1. TabItem.OnPreviewGotKeyboardFocus → MoveFocus(content) → content gets focus
    ///   2. GotKeyboardFocus on content bubbles → ancestor handler → Keyboard.Focus(tabItem)
    ///   3. TryChangeFocus → PreviewGotKeyboardFocus → back to step 1
    ///
    /// The MovingFocusToContent re-entrancy guard breaks this cycle by skipping
    /// MoveFocus when OnPreviewGotKeyboardFocus is already in progress.
    /// </summary>
    [WpfFact]
    public void OnPreviewGotKeyboardFocus_NoStackOverflow_WhenGotKeyboardFocusRedirectsFocusToTabItem()
    {
        // Arrange: Window > TabControl with 2 tabs, each containing a focusable TextBox
        Window window = new Window { Width = 400, Height = 300 };
        TabControl tabControl = new TabControl();

        TextBox textBox1 = new TextBox { Text = "Content1" };
        TabItem tabItem1 = new TabItem { Header = "Tab1", Content = textBox1 };

        TextBox textBox2 = new TextBox { Text = "Content2" };
        TabItem tabItem2 = new TabItem { Header = "Tab2", Content = textBox2 };

        tabControl.Items.Add(tabItem1);
        tabControl.Items.Add(tabItem2);
        window.Content = tabControl;
        window.Show();

        // Select Tab1 and put keyboard focus inside the TabControl
        // (IsKeyboardFocusWithin must be true for TabControl.OnSelectionChanged to call SetFocus)
        tabItem1.IsSelected = true;
        tabControl.UpdateLayout();
        textBox1.Focus();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

        int focusRedirectCount = 0;
        const int safetyLimit = 20;

        // Simulate an ancestor GotKeyboardFocus handler that redirects focus back
        // to the TabItem whenever content receives focus. This pattern occurs when
        // a hosting container tries to keep focus on the tab header element.
        // Without the re-entrancy guard, this causes infinite recursion.
        tabControl.AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler((_, e) =>
        {
            if (e.NewFocus == textBox2 && focusRedirectCount < safetyLimit)
            {
                focusRedirectCount++;
                Keyboard.Focus(tabItem2);
            }
        }));

        // Act: Switch to Tab2. This triggers:
        //   TabControl.OnSelectionChanged → TabItem2.SetFocus() (SetFocusOnContent=true)
        //   → Focus() → TryChangeFocus → PreviewGotKeyboardFocus
        //   → TabItem.OnPreviewGotKeyboardFocus → MoveFocus(content) → textBox2 gets focus
        //   → GotKeyboardFocus(textBox2) bubbles → our handler → Keyboard.Focus(tabItem2)
        //   → TryChangeFocus → PreviewGotKeyboardFocus → TabItem.OnPreviewGotKeyboardFocus
        //
        // Without the re-entrancy guard, this recurses until stack overflow.
        // With the guard, the second entry skips MoveFocus and the cycle stops.
        tabItem2.IsSelected = true;
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

        // Assert: focus redirect should fire only a small number of times (not hundreds)
        Assert.True(focusRedirectCount <= 5,
            $"Focus was redirected {focusRedirectCount} times, suggesting the re-entrancy guard is not working. " +
            "Expected ≤ 5 redirects.");

        window.Close();
    }
}
