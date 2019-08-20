// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Material group
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.Media3D;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Material group
    /// </summary>
    [ContentProperty("Children")]
    public sealed partial class MaterialGroup : Material
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public MaterialGroup() {}

        #endregion Constructors
    }
}
