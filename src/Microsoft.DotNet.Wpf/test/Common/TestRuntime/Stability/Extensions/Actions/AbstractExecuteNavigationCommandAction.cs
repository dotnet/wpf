// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Inherited this abstract class to implement an action which executes NavigationCommand.
    /// </summary>
    public abstract class AbstractExecuteNavigationCommandAction : SimpleDiscoverableAction
    {
        #region Public Members

        public Commands CommandToExecute { get; set; }

        public int GoToPage { get; set; }

        #endregion

        #region Protected Members

        protected void DoCommand(IInputElement target, int maxPage)
        {
            RoutedUICommand command;
            switch (CommandToExecute)
            {
                case Commands.IncreaseZoom:
                    command = NavigationCommands.IncreaseZoom;
                    command.Execute(null, target);
                    break;
                case Commands.DecreaseZoom:
                    command = NavigationCommands.DecreaseZoom;
                    command.Execute(null, target);
                    break;
                case Commands.FirstPage:
                    command = NavigationCommands.FirstPage;
                    command.Execute(null, target);
                    break;
                case Commands.LastPage:
                    command = NavigationCommands.LastPage;
                    command.Execute(null, target);
                    break;
                case Commands.NextPage:
                    command = NavigationCommands.NextPage;
                    command.Execute(null, target);
                    break;
                case Commands.PreviousPage:
                    command = NavigationCommands.PreviousPage;
                    command.Execute(null, target);
                    break;
                case Commands.GoToPage:
                    command = NavigationCommands.GoToPage;
                    if (maxPage > 0)
                    {
                        command.Execute(GoToPage % maxPage, target);
                    }
                    break;
            }
        }

        #endregion

        public enum Commands
        {
            IncreaseZoom,
            DecreaseZoom,
            NextPage,
            PreviousPage,
            LastPage,
            FirstPage,
            GoToPage
        }
    }
}
