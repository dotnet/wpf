// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Declaration of the GeneralTransform3DTo2DTo3D class.
//

using MS.Internal;
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
using MS.Internal.PresentationCore;

namespace MS.Internal.Media3D
{
    /// <summary>
    /// GeneralTransform3DTo2DTo3D class provides services to transform points from 3D to 2D to 3D
    /// </summary>
    internal class GeneralTransform3DTo2DTo3D : GeneralTransform3D
    {
        internal GeneralTransform3DTo2DTo3D()
        {
        }
        
        internal GeneralTransform3DTo2DTo3D(GeneralTransform3DTo2D transform3DTo2D,
                                            GeneralTransform2DTo3D transform2DTo3D)
        {   
            Debug.Assert(transform3DTo2D != null && transform2DTo3D != null);
            
            _transform3DTo2D = (GeneralTransform3DTo2D)transform3DTo2D.GetAsFrozen();
            _transform2DTo3D = (GeneralTransform2DTo3D)transform2DTo3D.GetAsFrozen();
        }
        
        /// <summary>
        /// Transform a point
        /// </summary>
        /// <param name="inPoint">Input point</param>
        /// <param name="result">Output point</param>
        /// <returns>True if the point was transformed successfuly, false otherwise</returns>
        public override bool TryTransform(Point3D inPoint, out Point3D result)
        {
            Point intermediate2DPoint = new Point();
            result = new Point3D();
            
            if (_transform3DTo2D == null ||
                !_transform3DTo2D.TryTransform(inPoint, out intermediate2DPoint))
            {
                return false;
            }

            if (_transform2DTo3D == null || 
                !_transform2DTo3D.TryTransform(intermediate2DPoint, out result))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Returns the inverse transform if it has an inverse, null otherwise.
        /// In this case we can only transform in one direction due to the ray being created
        /// so the inverse is null.
        /// </summary>        
        public override GeneralTransform3D Inverse
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a best effort affine transform
        /// </summary>
        internal override Transform3D AffineTransform
        {
            [FriendAccessAllowed] // Built into Core, also used by Framework.
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Transforms the bounding box to the smallest axis aligned bounding box
        /// that contains all the points in the original bounding box
        /// </summary>
        /// <param name="rect">Bounding box</param>
        /// <returns>The transformed bounding box</returns>
        public override Rect3D TransformBounds(Rect3D rect)
        {            
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new GeneralTransform3DTo2DTo3D();
        }
        

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2DTo3D transform = (GeneralTransform3DTo2DTo3D)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2DTo3D transform = (GeneralTransform3DTo2DTo3D)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2DTo3D transform = (GeneralTransform3DTo2DTo3D)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2DTo3D transform = (GeneralTransform3DTo2DTo3D)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }

        /// <summary>
        /// Clones values that do not have corresponding DPs
        /// </summary>
        /// <param name="transform"></param>
        private void CopyCommon(GeneralTransform3DTo2DTo3D transform)
        {
            _transform3DTo2D = transform._transform3DTo2D;
            _transform2DTo3D = transform._transform2DTo3D;
        }

        GeneralTransform3DTo2D _transform3DTo2D;
        GeneralTransform2DTo3D _transform2DTo3D;
    } 
}


