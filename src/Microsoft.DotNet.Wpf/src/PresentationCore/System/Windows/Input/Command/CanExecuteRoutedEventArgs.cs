// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Input
{
    /// <summary>
    ///     Event handler associated with the CanExecute events.
    /// </summary>
    public delegate void CanExecuteRoutedEventHandler(object sender, CanExecuteRoutedEventArgs e);

    /// <summary>
    ///     Event arguments for the CanExecute events.
    /// </summary>
    public sealed class CanExecuteRoutedEventArgs : RoutedEventArgs
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of this class.
        /// </summary>
        /// <param name="command">The command that is being executed.</param>
        /// <param name="parameter">The parameter that was passed when executing the command.</param>
        internal CanExecuteRoutedEventArgs(ICommand command, object parameter)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            _command = command;
            _parameter = parameter;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     The command that could be executed.
        /// </summary>
        public ICommand Command
        {
            get { return _command; }
        }

        /// <summary>
        ///     The parameter passed when considering executing the command.
        /// </summary>
        public object Parameter
        {
            get { return _parameter; }
        }

        /// <summary>
        ///     Whether the command with the specified parameter can be executed.
        /// </summary>
        public bool CanExecute
        {
            get { return _canExecute; }
            set { _canExecute = value; }
        }

        /// <summary>
        ///     Whether the input event (if any) that caused the command
        ///     should continue its route.
        /// </summary>
        public bool ContinueRouting
        {
            get { return _continueRouting; }
            set { _continueRouting = value; }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Calls the handler.
        /// </summary>
        /// <param name="genericHandler">Handler delegate to invoke</param>
        /// <param name="target">Target element</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object target)
        {
            CanExecuteRoutedEventHandler handler = (CanExecuteRoutedEventHandler)genericHandler;
            handler(target as DependencyObject, this);
        }

        #endregion

        #region Data

        private ICommand _command;
        private object _parameter;
        private bool _canExecute;       // Defaults to false
        private bool _continueRouting;  // Defaults to false

        #endregion
    }
}


