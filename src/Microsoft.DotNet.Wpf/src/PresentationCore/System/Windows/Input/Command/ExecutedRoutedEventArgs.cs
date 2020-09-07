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
    ///     Event handler for the Executed events.
    /// </summary>
    public delegate void ExecutedRoutedEventHandler(object sender, ExecutedRoutedEventArgs e);

    /// <summary>
    ///     Event arguments for the Executed events.
    /// </summary>
    public sealed class ExecutedRoutedEventArgs : RoutedEventArgs
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of this class.
        /// </summary>
        /// <param name="command">The command that is being executed.</param>
        /// <param name="parameter">The parameter that was passed when executing the command.</param>
        internal ExecutedRoutedEventArgs(ICommand command, object parameter)
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
        ///     The command being executed.
        /// </summary>
        public ICommand Command
        {
            get { return _command; }
        }

        /// <summary>
        ///     The parameter passed when executing the command.
        /// </summary>
        public object Parameter
        {
            get { return _parameter; }
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
            ExecutedRoutedEventHandler handler = (ExecutedRoutedEventHandler)genericHandler;
            handler(target as DependencyObject, this);
        }

        #endregion

        #region Data

        private ICommand _command;
        private object _parameter;

        #endregion
    }
}


