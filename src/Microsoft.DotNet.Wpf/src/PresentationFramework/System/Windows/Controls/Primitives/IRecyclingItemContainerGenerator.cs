// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IRecyclingItemContainerGenerator interface
//
// Spec: Item Container Recycling Spec.docx
//

using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Interface through which a layout element (such as a panel) marked
    ///     as an ItemsHost communicates with the ItemContainerGenerator of its
    ///     items owner.
    ///     
    ///     This interface adds the notion of recycling a container to the 
    ///     IItemContainerGenerator interface.  This is used for virtualizing
    ///     panels.
    /// </summary>
    public interface IRecyclingItemContainerGenerator : IItemContainerGenerator
    {
        /// <summary>
        /// Recycle generated elements.
        /// </summary>
        /// <remarks>
        /// Equivalent to Remove() except that the Generator retains this container in a list.
        /// This container will be handed back in a future call to GenerateNext()
        ///
        /// The position must refer to a previously generated item, i.e. its
        /// Offset must be 0.
        /// </remarks>
        void Recycle(GeneratorPosition position, int count);
    }
}
