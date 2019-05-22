// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows.Input;

namespace System.Windows.Input
{
    ///<summary>
    ///     An interface for classes that know how to invoke a Command.
    ///</summary>
    public interface ICommandSource
    {
        /// <summary>
        ///     The command that will be executed when the class is "invoked."
        ///     Classes that implement this interface should enable or disable based on the command's CanExecute return value.
        ///     The property may be implemented as read-write if desired.
        /// </summary>
        ICommand Command
        {
            get;
        }

        /// <summary>
        ///     The parameter that will be passed to the command when executing the command.
        ///     The property may be implemented as read-write if desired.
        /// </summary>
        object CommandParameter
        {
            get;
        }

        /// <summary>
        ///     An element that an implementor may wish to target as the destination for the command.
        ///     The property may be implemented as read-write if desired.
        /// </summary>
        IInputElement CommandTarget
        {
            get;
        }
    }
}
