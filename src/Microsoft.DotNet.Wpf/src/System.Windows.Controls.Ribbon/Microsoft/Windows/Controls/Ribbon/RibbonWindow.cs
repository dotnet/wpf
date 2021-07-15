// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations

    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;

    #endregion

    /// <summary>
    ///   A Ribbon specific Window class which allos the Ribbon to draw onto
    ///   the non-client area to overdraw the application menu and contextual tab groups.
    /// </summary>
    [TemplatePart(Name = RibbonWindow._clientAreaBorderTemplateName, Type = typeof(Border))]
    [TemplatePart(Name = RibbonWindow._iconTemplateName, Type = typeof(Image))]
    public class RibbonWindow : Window
    {
        #region Constructors

#if RIBBON_IN_FRAMEWORK
        private static ICommand _minimizeWindowCommand = System.Windows.SystemCommands.MinimizeWindowCommand;
        private static ICommand _maximizeWindowCommand = System.Windows.SystemCommands.MaximizeWindowCommand;
        private static ICommand _restoreWindowCommand = System.Windows.SystemCommands.RestoreWindowCommand;
        private static ICommand _closeWindowCommand = System.Windows.SystemCommands.CloseWindowCommand;
        private static ICommand _showSystemMenuCommand = System.Windows.SystemCommands.ShowSystemMenuCommand;
#else
        private static ICommand _minimizeWindowCommand = Microsoft.Windows.Shell.SystemCommands.MinimizeWindowCommand;
        private static ICommand _maximizeWindowCommand = Microsoft.Windows.Shell.SystemCommands.MaximizeWindowCommand;
        private static ICommand _restoreWindowCommand = Microsoft.Windows.Shell.SystemCommands.RestoreWindowCommand;
        private static ICommand _closeWindowCommand = Microsoft.Windows.Shell.SystemCommands.CloseWindowCommand;
        private static ICommand _showSystemMenuCommand = Microsoft.Windows.Shell.SystemCommands.ShowSystemMenuCommand;
#endif

        /// <summary>
        ///     Static constructor.
        ///     Initializes static members of the RibbonWindow class.
        /// </summary>
        static RibbonWindow()
        {
            Type ownerType = typeof(RibbonWindow);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new ComponentResourceKey(typeof(Ribbon), "RibbonWindowStyle")));

            // We override Window.Title metadata so that we can receive change notifications and then coerce Ribbon.Title.
            Window.TitleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(String.Empty, new PropertyChangedCallback(OnTitleChangedCallback)));

            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(_minimizeWindowCommand, MinimizeWindowExecuted, MinimizeWindowCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(_maximizeWindowCommand, MaximizeWindowExecuted, MaximizeWindowCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(_restoreWindowCommand, RestoreWindowExecuted, RestoreWindowCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(_closeWindowCommand, CloseWindowExecuted, CloseWindowCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(_showSystemMenuCommand, SystemMenuExecuted, SystemMenuCanExecute));
        }

        #endregion

        #region OnTitleChanged

        private static void OnTitleChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonWindow rw = d as RibbonWindow;
            rw.OnTitleChanged(null);
        }

        internal void OnTitleChanged(EventArgs e)
        {
            if (TitleChanged != null)
            {
                TitleChanged(this, e);
            }
        }

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Hook up events to the system icon.
            _icon = GetTemplateChild(_iconTemplateName) as Image;

            if (_icon != null)
            {
                _icon.MouseLeftButtonDown += new MouseButtonEventHandler(IconMouseLeftButtonDown);
                _icon.MouseRightButtonDown += new MouseButtonEventHandler(IconMouseRightButtonDown);
            }

            _clientAreaBorder = GetTemplateChild(RibbonWindow._clientAreaBorderTemplateName) as Border;
        }

#if !RIBBON_IN_FRAMEWORK
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (FlowDirection == FlowDirection.RightToLeft &&
                _icon != null &&
                _icon.IsVisible)
            {
                int currentTickCount = Environment.TickCount;
                int timeSinceLastClickOnSystemIcon = currentTickCount - _lastSystemIconClickTickCount;
                int systemDoubleClickInterval = NativeMethods.GetDoubleClickTime();

                if (timeSinceLastClickOnSystemIcon <= systemDoubleClickInterval)
                {
                    Point mouseDownLogicalCoordinates = e.GetPosition(this);
                    Point mouseDownScreenCoordinates = this.PointToScreen(mouseDownLogicalCoordinates);

                    Point iconPhysicalTopLeft = _icon.PointToScreen(new Point(0,0));
                    Point iconPhysicalBottomRight = _icon.PointToScreen(new Point(_icon.ActualWidth, _icon.ActualHeight));

                    bool clickIsInIconHorizontalRange =
                        mouseDownScreenCoordinates.X >= iconPhysicalTopLeft.X && mouseDownScreenCoordinates.X <= iconPhysicalBottomRight.X;
                    bool clickIsInIconVerticalRange =
                        mouseDownScreenCoordinates.Y >= iconPhysicalTopLeft.Y && mouseDownScreenCoordinates.Y <= iconPhysicalBottomRight.Y;

                    if (clickIsInIconHorizontalRange && clickIsInIconVerticalRange)
                    {
                        // Using Close() here is more effective.  CloseWindowCommand doesn't always close the window (e.g. it doesn't work
                        // when the system menu is open already and then we double click on the system icon... this scenario is somehow
                        // interfering with the message we post to close the window).
                        this.Close();
                    }
                }
            }
        }
#endif

        #endregion

        #region Private Data

        /// <summary>
        ///     The Window Icon.
        /// </summary>
        private Image _icon;

        /// <summary>
        ///     The Border that hosts the client content of the RibbonWindow.  Also used to position the SystemMenu.
        /// </summary>
        private Border _clientAreaBorder;

        private const string _iconTemplateName = "PART_Icon";
        private const string _clientAreaBorderTemplateName = "PART_ClientAreaBorder";

#if !RIBBON_IN_FRAMEWORK
        private int _lastSystemIconClickTickCount;
#endif

        #endregion

        #region Event and Command Handlers

        internal event EventHandler TitleChanged;

        private static void MinimizeWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null &&
                rw.WindowState != WindowState.Minimized)
            {
                args.CanExecute = true;
            }
        }

        private static void MinimizeWindowExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null)
            {
#if RIBBON_IN_FRAMEWORK
                SystemCommands.MinimizeWindow(rw);
#else
                Microsoft.Windows.Shell.SystemCommands.MinimizeWindow(rw);
#endif
                args.Handled = true;
            }
        }

        private static void MaximizeWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null
                && rw.WindowState != WindowState.Maximized)
            {
                args.CanExecute = true;
            }
        }

        private static void MaximizeWindowExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null)
            {
#if RIBBON_IN_FRAMEWORK
                SystemCommands.MaximizeWindow(rw);
#else
                Microsoft.Windows.Shell.SystemCommands.MaximizeWindow(rw);
#endif
                args.Handled = true;
            }
        }

        private static void RestoreWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null &&
                rw.WindowState != WindowState.Normal)
            {
                args.CanExecute = true;
            }
        }

        private static void RestoreWindowExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null)
            {
#if RIBBON_IN_FRAMEWORK
                SystemCommands.RestoreWindow(rw);
#else
                Microsoft.Windows.Shell.SystemCommands.RestoreWindow(rw);
#endif
                args.Handled = true;
            }
        }

        private static void CloseWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        private static void CloseWindowExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null)
            {
#if RIBBON_IN_FRAMEWORK
                SystemCommands.CloseWindow(rw);
#else
                Microsoft.Windows.Shell.SystemCommands.CloseWindow(rw);
#endif
                args.Handled = true;
            }
        }

        private static void SystemMenuCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        private static void SystemMenuExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            RibbonWindow rw = sender as RibbonWindow;
            if (rw != null)
            {
                // For right-clicks, display the system menu from the point of the mouse click.
                // For left-clicks, display the system menu in the top-left corner of the client area.
                Point devicePoint;
                MouseButtonEventArgs e = args.Parameter as MouseButtonEventArgs;
                if (e != null)
                {
                    // This is the right-click handler.  The presence of a MouseButtonEventArgs as args.Parameter
                    // indicates we are handling right-click.
                    devicePoint = rw.PointToScreen(e.GetPosition(rw));
                }
                else if (rw._clientAreaBorder != null) 
                {
                    // This is the left-click handler.  We can only handle it correctly if the _clientAreaBorder
                    // template part is defined, because that is where we want to position the system menu.
                    devicePoint = rw._clientAreaBorder.PointToScreen(new Point(0, 0));
                }
                else
                {
                    // We can't handle this correctly, so exit.
                    return;
                }

                CompositionTarget compositionTarget = PresentationSource.FromVisual(rw).CompositionTarget;
#if RIBBON_IN_FRAMEWORK
                SystemCommands.ShowSystemMenu(rw, compositionTarget.TransformFromDevice.Transform(devicePoint));
#else
                Microsoft.Windows.Shell.SystemCommands.ShowSystemMenu(rw, compositionTarget.TransformFromDevice.Transform(devicePoint));
#endif
                args.Handled = true;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///   This method allows Ribbon to propagate the WindowIconVisibility property to its containing RibbonWindow.
        /// </summary>
        internal void ChangeIconVisibility(Visibility newVisibility)
        {
            if (_icon != null)
            {
                _icon.Visibility = newVisibility;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///   This handles the click events on the window icon.
        /// </summary>
        /// <param name="sender">Click event sender</param>
        /// <param name="e">event args</param>
        private void IconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
#if RIBBON_IN_FRAMEWORK
            if (e.ClickCount == 1)
            {
                if (SystemCommands.ShowSystemMenuCommand.CanExecute(null, this))
                {
                    SystemCommands.ShowSystemMenuCommand.Execute(null, this);
                }
            }
            else if (e.ClickCount == 2)
            {
                if (SystemCommands.CloseWindowCommand.CanExecute(null, this))
                {
                    SystemCommands.CloseWindowCommand.Execute(null, this);
                }
            }
#else
            if (e.ClickCount == 1)
            {
                if (Microsoft.Windows.Shell.SystemCommands.ShowSystemMenuCommand.CanExecute(null, this))
                {
                    // Workaround for Dev11 42912 - RibbonWindow RTL - double-clicking system icon does not close app
                    // We can't fix the root WPF input bug in the OOB product, but we can work around it pretty well.
                    // We record Environment.TickCount for the first click on the system icon.  If the next click is
                    // on the system icon within the system double click interval, we handle it as a double-click.
                    // (the next click appears mirrored instead of on the system icon directly, so we have to handle
                    // it in RibbonWindow.OnMouseLeftButtonDown instead of on the system icon's click handler).
					if (FlowDirection == FlowDirection.RightToLeft)
                    {
                        _lastSystemIconClickTickCount = Environment.TickCount;
                    }

                    Microsoft.Windows.Shell.SystemCommands.ShowSystemMenuCommand.Execute(null, this);
                }
            }
            else if (e.ClickCount == 2)
            {
                if (Microsoft.Windows.Shell.SystemCommands.CloseWindowCommand.CanExecute(null, this))
                {
                    Microsoft.Windows.Shell.SystemCommands.CloseWindowCommand.Execute(null, this);
                }
            }
#endif
        }

        /// <summary>
        ///   This handles right-click events on the window icon.
        ///
        ///   For right-clicking, we want to display the system menu from the point of the mouse click
        ///   instead of from the top-left corner of the client area like we do with left clicks. So,
        ///   we pass the MouseButtonEventArgs to the SystemMenuExecuted handler.
        /// </summary>
        /// <param name="sender">Click event sender</param>
        /// <param name="e">event args</param>
        private void IconMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
#if RIBBON_IN_FRAMEWORK
            if (SystemCommands.ShowSystemMenuCommand.CanExecute(e, this))
            {
                SystemCommands.ShowSystemMenuCommand.Execute(e, this);
            }
#else
            if (Microsoft.Windows.Shell.SystemCommands.ShowSystemMenuCommand.CanExecute(e, this))
            {
                Microsoft.Windows.Shell.SystemCommands.ShowSystemMenuCommand.Execute(e, this);
            }
#endif
        }

        #endregion
    }
}
