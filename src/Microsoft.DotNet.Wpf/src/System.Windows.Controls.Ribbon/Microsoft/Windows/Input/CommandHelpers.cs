// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Windows.Input
{
    #region Using declarations

    using System.Windows;
    using System.Windows.Input;
    using System.Diagnostics;

    #endregion Using declarations

    #region CommandHelpers Class

    /// <summary>
    ///     A helper class for executing Commands.   
    /// </summary>
    internal static class CommandHelpers
    {
        /// <summary>
        ///     Carries out the specified action for the CommandSource's command as a RoutedCommand if possible,
        ///     otherwise as a regular Command.
        /// </summary>
        /// <param name="parameter">The parameter to the ICommandSource.</param>
        /// <param name="commandSource">The ICommandSource being executed.</param>
        internal static void InvokeCommandSource(object parameter, object previewParameter, ICommandSource commandSource, CommandOperation operation)
        {
            ICommand command = commandSource.Command;
            
            if (command != null)
            {
                RoutedCommand routed = command as RoutedCommand;

                if (routed != null)
                {
                    IInputElement target = commandSource.CommandTarget;

                    if (target == null)
                    {
                        target = commandSource as IInputElement;
                    }

                    if (routed.CanExecute(parameter, target))
                    {
                        //Debug.Assert(operation == CommandOperation.Execute, "We do not support Previewing RoutedCommands.");

                        switch (operation)
                        {
                            case CommandOperation.Execute:
                                routed.Execute(parameter, target);
                                break;
                        }
                    }
                }
                else if (command.CanExecute(parameter))
                {
                    IPreviewCommand previewCommand;
                    switch (operation)
                    {
                        case CommandOperation.Preview:
                            previewCommand = command as IPreviewCommand;
                            if (previewCommand != null)
                            {
                                previewCommand.Preview(previewParameter);
                            }
                            break;
                        case CommandOperation.CancelPreview:
                            previewCommand = command as IPreviewCommand;
                            if (previewCommand != null)
                            {
                                previewCommand.CancelPreview();
                            }
                            break;
                        case CommandOperation.Execute:
                            command.Execute(parameter);
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     Queries CanExecute status for the CommandSource's command as a RoutedCommand if possible,
        ///     otherwise as a regular Command.
        /// </summary>
        /// <param name="parameter">The parameter to the ICommandSource.</param>
        /// <param name="commandSource">The ICommandSource being executed.</param>
        internal static bool CanExecuteCommandSource(object parameter, ICommandSource commandSource)
        {
            ICommand command = commandSource.Command;

            if (command != null)
            {
                RoutedCommand routed = command as RoutedCommand;

                if (routed != null)
                {
                    IInputElement target = commandSource.CommandTarget;

                    if (target == null)
                    {
                        target = commandSource as IInputElement;
                    }

                    return routed.CanExecute(parameter, target);
                }
                else
                {
                    return command.CanExecute(parameter);
                }
            }

            return true;
        }
    }

    internal enum CommandOperation
    {
        Preview,
        CancelPreview,
        Execute
    }

    #endregion CommandHelpers Class
}
