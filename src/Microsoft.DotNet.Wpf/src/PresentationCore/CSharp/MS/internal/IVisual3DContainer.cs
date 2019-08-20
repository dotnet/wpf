// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using MS.Internal;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MS.Internal
{
    /// <summary>
    ///     IVisual3DContainer is the common interface for objects that contain 3D children
    /// </summary>
    internal interface IVisual3DContainer
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        
        #region Internal Methods
        
        /// <summary>
        ///     Notifies the element that you have added a child.  The Element
        ///     will update the parent pointer, fire the correct events, etc.
        /// </summary>
        void AddChild(Visual3D child);

        /// <summary>
        ///     Notifies the element that you have removed a child.  The Element
        ///     will update the parent pointer, fire the correct events, etc.
        /// </summary>
        void RemoveChild(Visual3D child);

        /// <summary>
        ///     Gets the number of Visual3D children that the IVisual3DContainer
        ///     contains.
        /// </summary>
        int GetChildrenCount();

        /// <summary>
        ///     Gets the index children of the IVisual3DContainer
        /// </summary>
        Visual3D GetChild(int index);

        /// <summary>
        /// Applies various API checks
        /// </summary>
        void VerifyAPIReadOnly();

        /// <summary>
        /// Applies various API checks
        /// </summary>
        void VerifyAPIReadOnly(DependencyObject other);

        /// <summary>
        /// Applies various API checks for read/write
        /// </summary>
        void VerifyAPIReadWrite();

        /// <summary>
        /// Applies various API checks
        /// </summary>
        void VerifyAPIReadWrite(DependencyObject other);

        #endregion Internal Methods
    }
}

