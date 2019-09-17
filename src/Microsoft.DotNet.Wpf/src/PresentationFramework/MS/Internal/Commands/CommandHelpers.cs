// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Security;

namespace MS.Internal.Commands
{
    internal static class CommandHelpers
    {
        // Lots of specialized registration methods to avoid new'ing up more common stuff (like InputGesture's) at the callsite, as that's frequently
        // repeated and increases code size.  Do it once, here.  

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, null, null);
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    InputGesture inputGesture)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, null, inputGesture);
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    Key key)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, null, new KeyGesture(key));
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    InputGesture inputGesture, InputGesture inputGesture2)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, null, inputGesture, inputGesture2);
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    CanExecuteRoutedEventHandler canExecuteRoutedEventHandler)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, canExecuteRoutedEventHandler, null);
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    CanExecuteRoutedEventHandler canExecuteRoutedEventHandler, InputGesture inputGesture)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, canExecuteRoutedEventHandler, inputGesture);
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    CanExecuteRoutedEventHandler canExecuteRoutedEventHandler, Key key)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, canExecuteRoutedEventHandler, new KeyGesture(key));
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    CanExecuteRoutedEventHandler canExecuteRoutedEventHandler, InputGesture inputGesture, InputGesture inputGesture2)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, canExecuteRoutedEventHandler, inputGesture, inputGesture2);
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
                                                    InputGesture inputGesture, InputGesture inputGesture2, InputGesture inputGesture3, InputGesture inputGesture4)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, canExecuteRoutedEventHandler,
                                          inputGesture, inputGesture2, inputGesture3, inputGesture4);
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, Key key, ModifierKeys modifierKeys,
                                                    ExecutedRoutedEventHandler executedRoutedEventHandler, CanExecuteRoutedEventHandler canExecuteRoutedEventHandler)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, canExecuteRoutedEventHandler, new KeyGesture(key, modifierKeys));
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    string srid1, string srid2)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, null,
                                                  KeyGesture.CreateFromResourceStrings(SR.Get(srid1), SR.Get(srid2)));
        }

        internal static void RegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                    CanExecuteRoutedEventHandler canExecuteRoutedEventHandler, string srid1, string srid2)
        {
            PrivateRegisterCommandHandler(controlType, command, executedRoutedEventHandler, canExecuteRoutedEventHandler,
                                                  KeyGesture.CreateFromResourceStrings(SR.Get(srid1), SR.Get(srid2)));
        }

        // 'params' based method is private.  Call sites that use this bloat unwittingly due to implicit construction of the params array that goes into IL.
        private static void PrivateRegisterCommandHandler(Type controlType, RoutedCommand command, ExecutedRoutedEventHandler executedRoutedEventHandler,
                                                          CanExecuteRoutedEventHandler canExecuteRoutedEventHandler, params InputGesture[] inputGestures)
        {
            // Validate parameters
            Debug.Assert(controlType != null);
            Debug.Assert(command != null);
            Debug.Assert(executedRoutedEventHandler != null);
            // All other parameters may be null

            // Create command link for this command
            CommandManager.RegisterClassCommandBinding(controlType, new CommandBinding(command, executedRoutedEventHandler, canExecuteRoutedEventHandler));

            // Create additional input binding for this command
            if (inputGestures != null)
            {
                for (int i = 0; i < inputGestures.Length; i++)
                {
                    CommandManager.RegisterClassInputBinding(controlType, new InputBinding(command, inputGestures[i]));
                }
            }
        }

        internal static bool CanExecuteCommandSource(ICommandSource commandSource)
        {
            ICommand command = commandSource.Command;
            if (command != null)
            {
                object parameter = commandSource.CommandParameter;
                IInputElement target = commandSource.CommandTarget;

                RoutedCommand routed = command as RoutedCommand;
                if (routed != null)
                {
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

            return false;
        }

        /// <summary>
        ///     Executes the command on the given command source.
        /// </summary>
        internal static void ExecuteCommandSource(ICommandSource commandSource)
        {
            CriticalExecuteCommandSource(commandSource, false);
        }

        /// <summary>
        ///     Executes the command on the given command source.
        /// </summary>
        internal static void CriticalExecuteCommandSource(ICommandSource commandSource, bool userInitiated)
        {
            ICommand command = commandSource.Command;
            if (command != null)
            {
                object parameter = commandSource.CommandParameter;
                IInputElement target = commandSource.CommandTarget;

                RoutedCommand routed = command as RoutedCommand;
                if (routed != null)
                {
                    if (target == null)
                    {
                        target = commandSource as IInputElement;
                    }
                    if (routed.CanExecute(parameter, target))
                    {
                        routed.ExecuteCore(parameter, target, userInitiated);
                    }
                }
                else if (command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }
        }
        // This allows a caller to override its ICommandSource values (used by Button and ScrollBar)
        internal static void ExecuteCommand(ICommand command, object parameter, IInputElement target)
        {
            RoutedCommand routed = command as RoutedCommand;
            if (routed != null)
            {
                if (routed.CanExecute(parameter, target))
                {
                    routed.Execute(parameter, target);
                }
            }
            else if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
    }
}
