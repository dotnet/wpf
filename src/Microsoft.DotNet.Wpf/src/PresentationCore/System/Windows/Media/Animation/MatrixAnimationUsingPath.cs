// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

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
    public class MatrixAnimationUsingPath : MatrixAnimationBase
    {
        #region Data

        private bool _isValid;

        /// <summary>
        /// If IsCumulative is set to true, these values represents the values
        /// that are accumulated with each repeat.  They are the end values of
        /// the path.
        /// </summary>
        private Vector _accumulatingOffset = new Vector();
        private double _accumulatingAngle;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new PathMatrixAnimation class.
        /// </summary>
        /// <remarks>
        /// There is no default PathGeometry so the user must specify one.
        /// </remarks>
        public MatrixAnimationUsingPath() 
            : base()
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Creates a copy of this PathMatrixAnimation.
        /// </summary>
        /// <returns>The copy.</returns>
        public new MatrixAnimationUsingPath Clone()
        {
            return (MatrixAnimationUsingPath)base.Clone();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new MatrixAnimationUsingPath();
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

        #region Public

        /// <summary>
        /// DoesRotateWithTangent Property
        /// </summary>
        public static readonly DependencyProperty DoesRotateWithTangentProperty =
                DependencyProperty.Register(
                    "DoesRotateWithTangent",
                    typeof(bool),
                    typeof(MatrixAnimationUsingPath),
                    new PropertyMetadata(false));

        /// <summary>
        /// If this is set to true, the object will rotate along with the
        /// tangent to the path.
        /// </summary>
        public bool DoesRotateWithTangent
        {
            get
            {
                return (bool)GetValue(DoesRotateWithTangentProperty);
            }
            set
            {
                SetValue(DoesRotateWithTangentProperty, value);
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
        /// IsAngleCumulative Property
        /// </summary>
        public static readonly DependencyProperty IsAngleCumulativeProperty =
                DependencyProperty.Register(
                    "IsAngleCumulative",
                    typeof(bool),
                    typeof(MatrixAnimationUsingPath),
                    new PropertyMetadata(false));

        /// <summary>
        /// If this property is set to true, the rotation angle of the animated matrix
        /// will accumulate over repeats of the animation.  For instance if
        /// your path is a small arc a cumulative angle will cause your object
        /// to continuously rotate with each repeat instead of restarting the
        /// rotation.  When combined with IsOffsetCumulative, your object may
        /// appear to tumble while it bounces depending on your path.  
        /// See <seealso cref="System.Windows.Media.Animation.MatrixAnimationUsingPath.IsOffsetCumulative">PathMatrixAnimation.IsOffsetCumulative</seealso>
        /// for related information.
        /// </summary>
        /// <value>default value: false</value>
        public bool IsAngleCumulative
        {
            get
            {
                return (bool)GetValue(IsAngleCumulativeProperty);
            }
            set
            {
                SetValue(IsAngleCumulativeProperty, value);
            }
        }

        /// <summary>
        /// IsOffsetCumulative Property
        /// </summary>
        public static readonly DependencyProperty IsOffsetCumulativeProperty =
                DependencyProperty.Register(
                    "IsOffsetCumulative",
                    typeof(bool),
                    typeof(MatrixAnimationUsingPath),
                    new PropertyMetadata(false));

        /// <summary>
        /// If this property is set to true, the offset of the animated matrix
        /// will accumulate over repeats of the animation.  For instance if
        /// your path is a small arc a cumulative offset will cause your object
        /// to appear to bounce once for each repeat.  
        /// See <seealso cref="System.Windows.Media.Animation.MatrixAnimationUsingPath.IsAngleCumulative">PathMatrixAnimation.IsAngleCumulative</seealso>
        /// for related information.
        /// </summary>
        /// <value>default value: false</value>
        public bool IsOffsetCumulative
        {
            get
            {
                return (bool)GetValue(IsOffsetCumulativeProperty);
            }
            set
            {
                SetValue(IsOffsetCumulativeProperty, value);
            }
        }

        /// <summary>
        /// PathGeometry Property
        /// </summary>
        public static readonly DependencyProperty PathGeometryProperty =
                DependencyProperty.Register(
                    "PathGeometry",
                    typeof(PathGeometry),
                    typeof(MatrixAnimationUsingPath),
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

        #region MatrixAnimationBase

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
        protected override Matrix GetCurrentValueCore(Matrix defaultOriginValue, Matrix defaultDestinationValue, AnimationClock animationClock)
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

            double angle = 0.0;

            if (DoesRotateWithTangent)
            {
                angle = DoubleAnimationUsingPath.CalculateAngleFromTangentVector(pathTangent.X, pathTangent.Y);
            }

            Matrix matrix = new Matrix();

            double currentRepeat = (double)(animationClock.CurrentIteration - 1);

            if (currentRepeat > 0)
            {
                if (IsOffsetCumulative)
                {
                    pathPoint = pathPoint + (_accumulatingOffset * currentRepeat);
                }

                if (   DoesRotateWithTangent
                    && IsAngleCumulative)
                {
                    angle = angle + (_accumulatingAngle * currentRepeat);
                }
            }

            matrix.Rotate(angle);
            matrix.Translate(pathPoint.X, pathPoint.Y);

            if (IsAdditive) 
            {
                return Matrix.Multiply(matrix, defaultOriginValue);
            }
            else
            {
                return matrix;
            }
        }

        #endregion

        #region Private Methods

        private void Validate()
        {
            Debug.Assert(!_isValid);

            if (   IsOffsetCumulative
                || IsAngleCumulative)
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

                // Calculate difference.
                _accumulatingAngle = DoubleAnimationUsingPath.CalculateAngleFromTangentVector(endTangent.X, endTangent.Y)
                                     - DoubleAnimationUsingPath.CalculateAngleFromTangentVector(startTangent.X, startTangent.Y);

                _accumulatingOffset.X = endPoint.X - startPoint.X;
                _accumulatingOffset.Y = endPoint.Y - startPoint.Y;
            }

            _isValid = true;
        }

        #endregion
    }
}

