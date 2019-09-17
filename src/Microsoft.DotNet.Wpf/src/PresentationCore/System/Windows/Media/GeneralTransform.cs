// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Declaration of the GeneralTransform class.
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
using System.Security;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using MS.Internal.PresentationCore;

namespace System.Windows.Media
{
    /// <summary>
    /// GeneralTransform class provides services to transform points and rects
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public abstract partial class GeneralTransform : Animatable
    {
        /// <summary>
        /// Transform a point
        /// </summary>
        /// <param name="inPoint">Input point</param>
        /// <param name="result">Output point</param>
        /// <returns>True if the point was transformed successfuly, false otherwise</returns>
        public abstract bool TryTransform(Point inPoint, out Point result);

        /// <summary>
        /// Transform a point
        /// 
        /// If the transformation does not succeed, this will throw an InvalidOperationException.
        /// If you don't want to try/catch, call TryTransform instead and check the boolean it
        /// returns.
        ///
        /// Note that this method will always succeed when called on a subclass of Transform
        /// </summary>
        /// <param name="point">Input point</param>
        /// <returns>The transformed point</returns>
        public Point Transform(Point point)
        {
            Point transformedPoint;

            if (!TryTransform(point, out transformedPoint))
            {
                throw new InvalidOperationException(SR.Get(SRID.GeneralTransform_TransformFailed, null));
            }

            return transformedPoint;
        }
        
        /// <summary>
        /// Transforms the bounding box to the smallest axis aligned bounding box
        /// that contains all the points in the original bounding box
        /// </summary>
        /// <param name="rect">Bounding box</param>
        /// <returns>The transformed bounding box</returns>
        public abstract Rect TransformBounds(Rect rect);


        /// <summary>
        /// Returns the inverse transform if it has an inverse, null otherwise
        /// </summary>        
        public abstract GeneralTransform Inverse { get; }

        /// <summary>
        /// Returns a best effort affine transform
        /// </summary>
        internal virtual Transform AffineTransform
        {
            [FriendAccessAllowed] // Built into Core, also used by Framework.
            get { return null; }
        }
    }
}
