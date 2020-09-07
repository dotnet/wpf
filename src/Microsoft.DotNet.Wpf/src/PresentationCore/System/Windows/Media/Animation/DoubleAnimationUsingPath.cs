// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using MS.Internal;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This animation can be used inside of a MatrixAnimationCollection to move
    /// a visual object along a path.
    /// </summary>
    public class DoubleAnimationUsingPath : DoubleAnimationBase
    {
        #region Data

        private bool _isValid;

        /// <summary>
        /// If IsCumulative is set to true, this value represents the value that
        /// is accumulated with each repeat.  It is the end value of the path
        /// output value for the path.
        /// </summary>
        private double _accumulatingValue;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new PathDoubleAnimation class.
        /// </summary>
        /// <remarks>
        /// There is no default PathGeometry so the user must specify one.
        /// </remarks>
        public DoubleAnimationUsingPath() 
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
                    typeof(DoubleAnimationUsingPath),
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

        /// <summary>
        /// Source Property
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                    "Source",
                    typeof(PathAnimationSource),
                    typeof(DoubleAnimationUsingPath),
                    new PropertyMetadata(PathAnimationSource.X));

        /// <summary>
        /// This property specifies which output property of a path this
        /// animation will represent.
        /// </summary>
        /// <value></value>
        public PathAnimationSource Source
        {
            get
            {
                return (PathAnimationSource)GetValue(SourceProperty);
            }
            set
            {
                SetValue(SourceProperty, value);
            }
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Creates a copy of this PathDoubleAnimation.
        /// </summary>
        /// <returns>The copy.</returns>
        public new DoubleAnimationUsingPath Clone()
        {
            return (DoubleAnimationUsingPath)base.Clone();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DoubleAnimationUsingPath();
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

        #region DoubleAnimationBase

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
        protected override double GetCurrentValueCore(double defaultOriginValue, double defaultDestinationValue, AnimationClock animationClock)
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
            double pathValue = 0.0;

            pathGeometry.GetPointAtFractionLength(animationClock.CurrentProgress.Value, out pathPoint, out pathTangent);

            switch (Source)
            {
                case PathAnimationSource.Angle:
                    pathValue = CalculateAngleFromTangentVector(pathTangent.X, pathTangent.Y);
                    break;

                case PathAnimationSource.X:
                    pathValue = pathPoint.X;
                    break;

                case PathAnimationSource.Y:
                    pathValue = pathPoint.Y;
                    break;
            }

            double currentRepeat = (double)(animationClock.CurrentIteration - 1);

            if (   IsCumulative
                && currentRepeat > 0)
            {
                pathValue += (_accumulatingValue * currentRepeat);
            }

            if (IsAdditive) 
            {
                return defaultOriginValue + pathValue;
            }
            else
            {
                return pathValue;
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

        /// <summary>
        /// The primary purpose of this method is to calculate the accumulating
        /// value if one of the properties changes.
        /// </summary>
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

                switch (Source)
                {
                    case PathAnimationSource.Angle:
                        _accumulatingValue = CalculateAngleFromTangentVector(endTangent.X, endTangent.Y)
                                             - CalculateAngleFromTangentVector(startTangent.X, startTangent.Y);
                        break;

                    case PathAnimationSource.X:
                        _accumulatingValue = endPoint.X - startPoint.X;
                        break;

                    case PathAnimationSource.Y:
                        _accumulatingValue = endPoint.Y - startPoint.Y;
                        break;
                }
            }

            _isValid = true;
        }

        internal static double CalculateAngleFromTangentVector(double x, double y)
        {
            double angle = Math.Acos(x) * (180.0 / Math.PI);

            if (y < 0.0)
            {
                angle = 360 - angle;
            }

            return angle;
        }

        #endregion
    }
}

