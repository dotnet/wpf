// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Windows.Input;

namespace Microsoft.Windows.Input
{
    /// <summary>
    ///     An interface for classes that know how to invoke a PreviewCommand.
    /// </summary>
    public interface IPreviewCommandSource : ICommandSource
    {
        /// <summary>
        ///     The parameter that will be passed to the command when previewing the command.
        ///     The property may be implemented as read-write if desired.
        /// </summary>
        object PreviewCommandParameter
        {
            get;
        }
    }
}
