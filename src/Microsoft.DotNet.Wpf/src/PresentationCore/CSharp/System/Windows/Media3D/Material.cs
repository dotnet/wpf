// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D material implementation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Material is the abstract base class for materials
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string        
    public abstract partial class Material : Animatable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Prevent 3rd parties from extending this abstract base class.
        internal Material() {}

        #endregion Constructors
    }
}

