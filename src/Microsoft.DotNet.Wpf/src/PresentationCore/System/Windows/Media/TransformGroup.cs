// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using MS.Internal;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Windows.Media.Composition;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    #region TransformGroup
    /// <summary>
    /// The class definition for TransformGroup
    /// </summary>
    [ContentProperty("Children")]
    public sealed partial class TransformGroup : Transform
    {
        #region Constructors

        ///<summary>
        /// Default Constructor
        ///</summary>
        public TransformGroup() { }

        #endregion
        
        #region Value
        ///<summary>
        /// Return the current transformation value.
        ///</summary>
        public override Matrix Value
        {
            get
            {
                ReadPreamble();

                TransformCollection children = Children;
                if ((children == null) || (children.Count == 0))
                {
                    return new Matrix();
                }

                Matrix transform = children.Internal_GetItem(0).Value;

                for (int i = 1; i < children.Count; i++)
                {
                    transform *= children.Internal_GetItem(i).Value;
                }

                return transform;
            }
        }
        #endregion

        #region IsIdentity
        ///<summary>
        /// Returns true if transformation matches the identity transform.
        ///</summary>
        internal override bool IsIdentity
        {
            get
            {
                TransformCollection children = Children;
                if ((children == null) || (children.Count == 0))
                {
                    return true;
                }

                for (int i = 0; i < children.Count; i++)
                {
                    if (!children.Internal_GetItem(i).IsIdentity)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        #endregion

        #region Internal Methods

        internal override bool CanSerializeToString() { return false; }

        #endregion Internal Methods
    }
    #endregion
}

