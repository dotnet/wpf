// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: 3D transform collection.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//



using System.Windows.Markup;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// 3D transform group.
    /// </summary>
    [ContentProperty("Children")]
    public sealed partial class Transform3DGroup : Transform3D
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
        public Transform3DGroup() {}

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        ///<summary>
        ///     Return the current transformation value.
        ///</summary>
        public override Matrix3D Value
        {
            get
            {
                ReadPreamble();

                Matrix3D transform = new Matrix3D();
                Append(ref transform);

                return transform;
            }
        }

        /// <summary>
        ///     Whether the transform is affine.
        /// </summary>
        public override bool IsAffine
        {
            get
            {
                ReadPreamble();

                Transform3DCollection children = Children;
                if (children != null)
                {
                    for (int i = 0, count = children.Count; i < count; ++i)
                    {
                        Transform3D transform = children.Internal_GetItem(i);
                        if (!transform.IsAffine)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        internal override void Append(ref Matrix3D matrix)
        {
            Transform3DCollection children = Children;
            if (children != null)
            {
                for (int i = 0, count = children.Count; i < count; i++)
                {
                    children.Internal_GetItem(i).Append(ref matrix);
                }
            }           
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
    }
}
