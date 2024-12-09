// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Material group
//

using System.Windows.Markup;

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
        public MaterialGroup() { }

        #endregion Constructors
    }
}
