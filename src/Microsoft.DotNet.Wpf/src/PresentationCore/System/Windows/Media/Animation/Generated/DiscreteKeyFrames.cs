// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using MS.Internal.PresentationCore;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This class is used as part of a BooleanKeyFrameCollection in
    /// conjunction with a KeyFrameBooleanAnimation to animate a
    /// Boolean property value along a set of key frames.
    ///
    /// This BooleanKeyFrame changes from the Boolean Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteBooleanKeyFrame : BooleanKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteBooleanKeyFrame.
        /// </summary>
        public DiscreteBooleanKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteBooleanKeyFrame.
        /// </summary>
        public DiscreteBooleanKeyFrame(Boolean value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteBooleanKeyFrame.
        /// </summary>
        public DiscreteBooleanKeyFrame(Boolean value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteBooleanKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region BooleanKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Boolean InterpolateValueCore(Boolean baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a ByteKeyFrameCollection in
    /// conjunction with a KeyFrameByteAnimation to animate a
    /// Byte property value along a set of key frames.
    ///
    /// This ByteKeyFrame changes from the Byte Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteByteKeyFrame : ByteKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteByteKeyFrame.
        /// </summary>
        public DiscreteByteKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteByteKeyFrame.
        /// </summary>
        public DiscreteByteKeyFrame(Byte value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteByteKeyFrame.
        /// </summary>
        public DiscreteByteKeyFrame(Byte value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteByteKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region ByteKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Byte InterpolateValueCore(Byte baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a CharKeyFrameCollection in
    /// conjunction with a KeyFrameCharAnimation to animate a
    /// Char property value along a set of key frames.
    ///
    /// This CharKeyFrame changes from the Char Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteCharKeyFrame : CharKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteCharKeyFrame.
        /// </summary>
        public DiscreteCharKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteCharKeyFrame.
        /// </summary>
        public DiscreteCharKeyFrame(Char value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteCharKeyFrame.
        /// </summary>
        public DiscreteCharKeyFrame(Char value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteCharKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region CharKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Char InterpolateValueCore(Char baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a ColorKeyFrameCollection in
    /// conjunction with a KeyFrameColorAnimation to animate a
    /// Color property value along a set of key frames.
    ///
    /// This ColorKeyFrame changes from the Color Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteColorKeyFrame : ColorKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteColorKeyFrame.
        /// </summary>
        public DiscreteColorKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteColorKeyFrame.
        /// </summary>
        public DiscreteColorKeyFrame(Color value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteColorKeyFrame.
        /// </summary>
        public DiscreteColorKeyFrame(Color value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteColorKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region ColorKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Color InterpolateValueCore(Color baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a DecimalKeyFrameCollection in
    /// conjunction with a KeyFrameDecimalAnimation to animate a
    /// Decimal property value along a set of key frames.
    ///
    /// This DecimalKeyFrame changes from the Decimal Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteDecimalKeyFrame : DecimalKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteDecimalKeyFrame.
        /// </summary>
        public DiscreteDecimalKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteDecimalKeyFrame.
        /// </summary>
        public DiscreteDecimalKeyFrame(Decimal value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteDecimalKeyFrame.
        /// </summary>
        public DiscreteDecimalKeyFrame(Decimal value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteDecimalKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region DecimalKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Decimal InterpolateValueCore(Decimal baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a DoubleKeyFrameCollection in
    /// conjunction with a KeyFrameDoubleAnimation to animate a
    /// Double property value along a set of key frames.
    ///
    /// This DoubleKeyFrame changes from the Double Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteDoubleKeyFrame : DoubleKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteDoubleKeyFrame.
        /// </summary>
        public DiscreteDoubleKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteDoubleKeyFrame.
        /// </summary>
        public DiscreteDoubleKeyFrame(Double value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteDoubleKeyFrame.
        /// </summary>
        public DiscreteDoubleKeyFrame(Double value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteDoubleKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region DoubleKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Double InterpolateValueCore(Double baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Int16KeyFrameCollection in
    /// conjunction with a KeyFrameInt16Animation to animate a
    /// Int16 property value along a set of key frames.
    ///
    /// This Int16KeyFrame changes from the Int16 Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteInt16KeyFrame : Int16KeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteInt16KeyFrame.
        /// </summary>
        public DiscreteInt16KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteInt16KeyFrame.
        /// </summary>
        public DiscreteInt16KeyFrame(Int16 value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteInt16KeyFrame.
        /// </summary>
        public DiscreteInt16KeyFrame(Int16 value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteInt16KeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Int16KeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Int16 InterpolateValueCore(Int16 baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Int32KeyFrameCollection in
    /// conjunction with a KeyFrameInt32Animation to animate a
    /// Int32 property value along a set of key frames.
    ///
    /// This Int32KeyFrame changes from the Int32 Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteInt32KeyFrame : Int32KeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteInt32KeyFrame.
        /// </summary>
        public DiscreteInt32KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteInt32KeyFrame.
        /// </summary>
        public DiscreteInt32KeyFrame(Int32 value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteInt32KeyFrame.
        /// </summary>
        public DiscreteInt32KeyFrame(Int32 value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteInt32KeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Int32KeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Int32 InterpolateValueCore(Int32 baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Int64KeyFrameCollection in
    /// conjunction with a KeyFrameInt64Animation to animate a
    /// Int64 property value along a set of key frames.
    ///
    /// This Int64KeyFrame changes from the Int64 Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteInt64KeyFrame : Int64KeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteInt64KeyFrame.
        /// </summary>
        public DiscreteInt64KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteInt64KeyFrame.
        /// </summary>
        public DiscreteInt64KeyFrame(Int64 value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteInt64KeyFrame.
        /// </summary>
        public DiscreteInt64KeyFrame(Int64 value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteInt64KeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Int64KeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Int64 InterpolateValueCore(Int64 baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a MatrixKeyFrameCollection in
    /// conjunction with a KeyFrameMatrixAnimation to animate a
    /// Matrix property value along a set of key frames.
    ///
    /// This MatrixKeyFrame changes from the Matrix Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteMatrixKeyFrame : MatrixKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteMatrixKeyFrame.
        /// </summary>
        public DiscreteMatrixKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteMatrixKeyFrame.
        /// </summary>
        public DiscreteMatrixKeyFrame(Matrix value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteMatrixKeyFrame.
        /// </summary>
        public DiscreteMatrixKeyFrame(Matrix value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteMatrixKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region MatrixKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Matrix InterpolateValueCore(Matrix baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a ObjectKeyFrameCollection in
    /// conjunction with a KeyFrameObjectAnimation to animate a
    /// Object property value along a set of key frames.
    ///
    /// This ObjectKeyFrame changes from the Object Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteObjectKeyFrame : ObjectKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteObjectKeyFrame.
        /// </summary>
        public DiscreteObjectKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteObjectKeyFrame.
        /// </summary>
        public DiscreteObjectKeyFrame(Object value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteObjectKeyFrame.
        /// </summary>
        public DiscreteObjectKeyFrame(Object value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteObjectKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region ObjectKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Object InterpolateValueCore(Object baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a PointKeyFrameCollection in
    /// conjunction with a KeyFramePointAnimation to animate a
    /// Point property value along a set of key frames.
    ///
    /// This PointKeyFrame changes from the Point Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscretePointKeyFrame : PointKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscretePointKeyFrame.
        /// </summary>
        public DiscretePointKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscretePointKeyFrame.
        /// </summary>
        public DiscretePointKeyFrame(Point value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscretePointKeyFrame.
        /// </summary>
        public DiscretePointKeyFrame(Point value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscretePointKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region PointKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Point InterpolateValueCore(Point baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Point3DKeyFrameCollection in
    /// conjunction with a KeyFramePoint3DAnimation to animate a
    /// Point3D property value along a set of key frames.
    ///
    /// This Point3DKeyFrame changes from the Point3D Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscretePoint3DKeyFrame : Point3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        public DiscretePoint3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        public DiscretePoint3DKeyFrame(Point3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        public DiscretePoint3DKeyFrame(Point3D value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscretePoint3DKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Point3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Point3D InterpolateValueCore(Point3D baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    ///
    /// This QuaternionKeyFrame changes from the Quaternion Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteQuaternionKeyFrame : QuaternionKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        public DiscreteQuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        public DiscreteQuaternionKeyFrame(Quaternion value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        public DiscreteQuaternionKeyFrame(Quaternion value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteQuaternionKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region QuaternionKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Quaternion InterpolateValueCore(Quaternion baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Rotation3DKeyFrameCollection in
    /// conjunction with a KeyFrameRotation3DAnimation to animate a
    /// Rotation3D property value along a set of key frames.
    ///
    /// This Rotation3DKeyFrame changes from the Rotation3D Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteRotation3DKeyFrame : Rotation3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        public DiscreteRotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        public DiscreteRotation3DKeyFrame(Rotation3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        public DiscreteRotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteRotation3DKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Rotation3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Rotation3D InterpolateValueCore(Rotation3D baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a RectKeyFrameCollection in
    /// conjunction with a KeyFrameRectAnimation to animate a
    /// Rect property value along a set of key frames.
    ///
    /// This RectKeyFrame changes from the Rect Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteRectKeyFrame : RectKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteRectKeyFrame.
        /// </summary>
        public DiscreteRectKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteRectKeyFrame.
        /// </summary>
        public DiscreteRectKeyFrame(Rect value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteRectKeyFrame.
        /// </summary>
        public DiscreteRectKeyFrame(Rect value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteRectKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region RectKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Rect InterpolateValueCore(Rect baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a SingleKeyFrameCollection in
    /// conjunction with a KeyFrameSingleAnimation to animate a
    /// Single property value along a set of key frames.
    ///
    /// This SingleKeyFrame changes from the Single Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteSingleKeyFrame : SingleKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteSingleKeyFrame.
        /// </summary>
        public DiscreteSingleKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteSingleKeyFrame.
        /// </summary>
        public DiscreteSingleKeyFrame(Single value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteSingleKeyFrame.
        /// </summary>
        public DiscreteSingleKeyFrame(Single value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteSingleKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region SingleKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Single InterpolateValueCore(Single baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a SizeKeyFrameCollection in
    /// conjunction with a KeyFrameSizeAnimation to animate a
    /// Size property value along a set of key frames.
    ///
    /// This SizeKeyFrame changes from the Size Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteSizeKeyFrame : SizeKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteSizeKeyFrame.
        /// </summary>
        public DiscreteSizeKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteSizeKeyFrame.
        /// </summary>
        public DiscreteSizeKeyFrame(Size value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteSizeKeyFrame.
        /// </summary>
        public DiscreteSizeKeyFrame(Size value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteSizeKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region SizeKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Size InterpolateValueCore(Size baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a StringKeyFrameCollection in
    /// conjunction with a KeyFrameStringAnimation to animate a
    /// String property value along a set of key frames.
    ///
    /// This StringKeyFrame changes from the String Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteStringKeyFrame : StringKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteStringKeyFrame.
        /// </summary>
        public DiscreteStringKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteStringKeyFrame.
        /// </summary>
        public DiscreteStringKeyFrame(String value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteStringKeyFrame.
        /// </summary>
        public DiscreteStringKeyFrame(String value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteStringKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region StringKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override String InterpolateValueCore(String baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a VectorKeyFrameCollection in
    /// conjunction with a KeyFrameVectorAnimation to animate a
    /// Vector property value along a set of key frames.
    ///
    /// This VectorKeyFrame changes from the Vector Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteVectorKeyFrame : VectorKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteVectorKeyFrame.
        /// </summary>
        public DiscreteVectorKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteVectorKeyFrame.
        /// </summary>
        public DiscreteVectorKeyFrame(Vector value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteVectorKeyFrame.
        /// </summary>
        public DiscreteVectorKeyFrame(Vector value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteVectorKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region VectorKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Vector InterpolateValueCore(Vector baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Vector3DKeyFrameCollection in
    /// conjunction with a KeyFrameVector3DAnimation to animate a
    /// Vector3D property value along a set of key frames.
    ///
    /// This Vector3DKeyFrame changes from the Vector3D Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteVector3DKeyFrame : Vector3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        public DiscreteVector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        public DiscreteVector3DKeyFrame(Vector3D value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        public DiscreteVector3DKeyFrame(Vector3D value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteVector3DKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region Vector3DKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Vector3D InterpolateValueCore(Vector3D baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }
}
