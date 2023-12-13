// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using MS.Internal; 
using System.Security; 

namespace System.Windows.Input 
{
    /// <summary>
    /// CommandBinding - Command-EventHandlers map
    ///         CommandBinding acts like a map for EventHandlers and Commands. 
    ///         PreviewExecute/Execute, PreviewCanExecute/CanExecute handlers 
    ///         can be added at CommandBinding which will exist at Element level 
    ///         in the form of a Collection and will be invoked when the system 
    ///         is routing the corresponding RoutedEvents.
    /// </summary>
    public class CommandBinding
    {
        #region Constructors

        /// <summary>
        ///     Default Constructor - required to allow creation from markup
        /// </summary>
        public CommandBinding()
        {   
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="command">Command associated with this binding.</param>
        public CommandBinding(ICommand command)
            : this(command, null, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="command">Command associated with this binding.</param>
        /// <param name="executed">Handler associated with executing the command.</param>
        public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed)
            : this(command, executed, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="command">Command associated with this binding.</param>
        /// <param name="executed">Handler associated with executing the command.</param>
        /// <param name="canExecute">Handler associated with determining if the command can execute.</param>
        public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute)
        {
            ArgumentNullException.ThrowIfNull(command);
            _command = command;

            if (executed is not null)
            {
                Executed += executed;
            }
            if (canExecute is not null)
            {
                CanExecute += canExecute;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Command associated with this binding
        /// </summary>
        [Localizability(LocalizationCategory.NeverLocalize)] // cannot be localized        
        public ICommand Command
        {
            get => _command;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _command = value;
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     Called before the command is executed.
        /// </summary>
        public event ExecutedRoutedEventHandler PreviewExecuted;

        /// <summary>
        ///     Called when the command is executed.
        /// </summary>
        public event ExecutedRoutedEventHandler Executed;

        /// <summary>
        ///     Called before determining if the command can be executed.
        /// </summary>
        public event CanExecuteRoutedEventHandler PreviewCanExecute;

        /// <summary>
        ///     Called to determine if the command can be executed.
        /// </summary>
        public event CanExecuteRoutedEventHandler CanExecute;

        #endregion

        #region Implementation

        /// <summary>
        ///     Calls the CanExecute or PreviewCanExecute event based on the event argument's RoutedEvent.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        internal void OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Handled) return;
            if (e.RoutedEvent == CommandManager.CanExecuteEvent)
            {
                if (CanExecute is null)
                {
                    if (e.CanExecute) return;
                    // If there is an Executed handler, then the command can be executed.
                    if (Executed is null) return;
                    e.CanExecute = true;
                    e.Handled = true;
                }
                else
                {
                    CanExecute(sender, e);
                    if (e.CanExecute)
                    {
                        e.Handled = true;
                    }
                }
            }
            else // e.RoutedEvent == CommandManager.PreviewCanExecuteEvent
            {
                if (PreviewCanExecute is null) return;
                PreviewCanExecute(sender, e);
                if (e.CanExecute)
                {
                    e.Handled = true;
                }
            }
        }

        private bool CheckCanExecute(object sender, ExecutedRoutedEventArgs e)
        {
            CanExecuteRoutedEventArgs canExecuteArgs = new(e.Command, e.Parameter)
            {
                RoutedEvent = CommandManager.CanExecuteEvent,
                // Since we don't actually raise this event, we have to explicitly set the source.
                Source = e.OriginalSource
            };

            canExecuteArgs.OverrideSource(e.Source);
            
            OnCanExecute(sender, canExecuteArgs);

            return canExecuteArgs.CanExecute;
        }

        /// <summary>
        ///     Calls Executed or PreviewExecuted based on the event argument's RoutedEvent.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        internal void OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Handled) return;
            if (e.RoutedEvent == CommandManager.ExecutedEvent)
            {
                if (Executed is null) return;
                if (!CheckCanExecute(sender, e)) return;
                Debug.Assert(Executed != null, nameof(Executed) + " != null");
                Executed(sender, e);
                e.Handled = true;
            }
            else // e.RoutedEvent == CommandManager.PreviewExecutedEvent
            {
                if (PreviewExecuted is null) return;
                if (!CheckCanExecute(sender, e)) return;
                Debug.Assert(PreviewExecuted != null, nameof(PreviewExecuted) + " != null");
                PreviewExecuted(sender, e);
                e.Handled = true;
            }
        }

        #endregion

        #region Data

        private ICommand _command;

        #endregion
    }
}
