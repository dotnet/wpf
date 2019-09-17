// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using System;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This animation can be used inside of a MatrixAnimationCollection to move
    /// a visual object along a path.
    /// </summary>
    public class PointAnimationUsingPath : PointAnimationBase
    {
        #region Data

		private bool _isValid;

        /// <summary>
        /// If IsCumulative is set to true, this value represents the value that
        /// is accumulated with each repeat.  It is the end value of the path
        /// output value for the path.
        /// </summary>
        private Vector _accumulatingVector = new Vector();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new PathPointAnimation class.
        /// </summary>
        /// <remarks>
        /// There is no default PathGeometry so the user must specify one.
        /// </remarks>
        public PointAnimationUsingPath() 
            : base()
        {
        }

        #endregion

        #region Public

        /// <summary>
        /// PathGeometry Property
        /// </summary>
        public static readonly DependencyProperty PathGeometryProperty =
            DependencyProperty.Register(
                    "PathGeometry",
                    typeof(PathGeometry),
                    typeof(PointAnimationUsingPath),
                    new PropertyMetadata(
                        (PathGeometry)null));

        /// <summary>
        /// This geometry specifies the path.
        /// </summary>
        public PathGeometry PathGeometry
        {
            get
            {
                return (PathGeometry)GetValue(PathGeometryProperty);
            }
            set
            {
                SetValue(PathGeometryProperty, value);
            }
        }

        #endregion

        #region Freezable

		/// <summary>
        /// Creates a copy of this PointAnimationUsingPath.
        /// </summary>
        /// <returns>The copy.</returns>
        public new PointAnimationUsingPath Clone()
        {
            return (PointAnimationUsingPath)base.Clone();
        }

		/// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new PointAnimationUsingPath();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.OnChanged">Freezable.OnChanged</see>.
        /// </summary>
        protected override void OnChanged()
        {
            _isValid = false;

            base.OnChanged();
        }

        #endregion

        #region PointAnimationBase

        /// <summary>
        /// Calculates the value this animation believes should be the current value for the property.
        /// </summary>
        /// <param name="defaultOriginValue">
        /// This value is the suggested origin value provided to the animation
        /// to be used if the animation does not have its own concept of a
        /// start value. If this animation is the first in a composition chain
        /// this value will be the snapshot value if one is available or the
        /// base property value if it is not; otherise this value will be the 
        /// value returned by the previous animation in the chain with an 
        /// animationClock that is not Stopped.
        /// </param>
        /// <param name="defaultDestinationValue">
        /// This value is the suggested destination value provided to the animation
        /// to be used if the animation does not have its own concept of an
        /// end value. This value will be the base value if the animation is
        /// in the first composition layer of animations on a property; 
        /// otherwise this value will be the output value from the previous 
        /// composition layer of animations for the property.
        /// </param>
        /// <param name="animationClock">
        /// This is the animationClock which can generate the CurrentTime or
        /// CurrentProgress value to be used by the animation to generate its
        /// output value.
        /// </param>
        /// <returns>
        /// The value this animation believes should be the current value for the property.
        /// </returns>
        protected override Point GetCurrentValueCore(Point defaultOriginValue, Point defaultDestinationValue, AnimationClock animationClock)
        {
            Debug.Assert(animationClock.CurrentState != ClockState.Stopped);

            PathGeometry pathGeometry = PathGeometry;
            
            if (pathGeometry == null)
            {
                return defaultDestinationValue;
            }

            if (!_isValid)
            {
                Validate();
            }

            Point pathPoint;
            Point pathTangent;

            pathGeometry.GetPointAtFractionLength(animationClock.CurrentProgress.Value, out pathPoint, out pathTangent);

            double currentRepeat = (double)(animationClock.CurrentIteration - 1);

            if (   IsCumulative
                && currentRepeat > 0)
            {
                pathPoint = pathPoint + (_accumulatingVector * currentRepeat);
            }

            if (IsAdditive) 
            {
                return defaultOriginValue + (Vector)pathPoint;
            }
            else
            {
                return pathPoint;
            }
        }

        /// <summary>
        /// IsAdditive
        /// </summary>
        public bool IsAdditive         
        { 
            get
            {
                return (bool)GetValue(IsAdditiveProperty);
            }
            set
            {
                SetValue(IsAdditiveProperty, value);
            }
        }

        /// <summary>
        /// IsCumulative
        /// </summary>
        public bool IsCumulative      
        { 
            get
            {
                return (bool)GetValue(IsCumulativeProperty);
            }
            set
            {
                SetValue(IsCumulativeProperty, value);
            }
        }

        #endregion

        #region Private Methods

        private void Validate()
        {
            Debug.Assert(!_isValid);

            if (IsCumulative)
            {
                Point startPoint;
                Point startTangent;
                Point endPoint;
                Point endTangent;
                PathGeometry pathGeometry = PathGeometry;

                // Get values at the beginning of the path.
                pathGeometry.GetPointAtFractionLength(0.0, out startPoint, out startTangent);

                // Get values at the end of the path.
                pathGeometry.GetPointAtFractionLength(1.0, out endPoint, out endTangent);

                _accumulatingVector.X = endPoint.X - startPoint.X;
                _accumulatingVector.Y = endPoint.Y - startPoint.Y;
            }

            _isValid = true;
        }

        #endregion
    }
}

