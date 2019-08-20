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
    /// This class is used as part of a ByteKeyFrameCollection in
    /// conjunction with a KeyFrameByteAnimation to animate a
    /// Byte property value along a set of key frames.
    ///
    /// This ByteKeyFrame interpolates the between the Byte Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingByteKeyFrame : ByteKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingByteKeyFrame.
        /// </summary>
        public EasingByteKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingByteKeyFrame.
        /// </summary>
        public EasingByteKeyFrame(Byte value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingByteKeyFrame.
        /// </summary>
        public EasingByteKeyFrame(Byte value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingByteKeyFrame.
        /// </summary>
        public EasingByteKeyFrame(Byte value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingByteKeyFrame();
        }

        #endregion

        #region ByteKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Byte InterpolateValueCore(Byte baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateByte(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingByteKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a ColorKeyFrameCollection in
    /// conjunction with a KeyFrameColorAnimation to animate a
    /// Color property value along a set of key frames.
    ///
    /// This ColorKeyFrame interpolates the between the Color Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingColorKeyFrame : ColorKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingColorKeyFrame.
        /// </summary>
        public EasingColorKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingColorKeyFrame.
        /// </summary>
        public EasingColorKeyFrame(Color value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingColorKeyFrame.
        /// </summary>
        public EasingColorKeyFrame(Color value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingColorKeyFrame.
        /// </summary>
        public EasingColorKeyFrame(Color value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingColorKeyFrame();
        }

        #endregion

        #region ColorKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Color InterpolateValueCore(Color baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateColor(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingColorKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a DecimalKeyFrameCollection in
    /// conjunction with a KeyFrameDecimalAnimation to animate a
    /// Decimal property value along a set of key frames.
    ///
    /// This DecimalKeyFrame interpolates the between the Decimal Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingDecimalKeyFrame : DecimalKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingDecimalKeyFrame.
        /// </summary>
        public EasingDecimalKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingDecimalKeyFrame.
        /// </summary>
        public EasingDecimalKeyFrame(Decimal value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingDecimalKeyFrame.
        /// </summary>
        public EasingDecimalKeyFrame(Decimal value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingDecimalKeyFrame.
        /// </summary>
        public EasingDecimalKeyFrame(Decimal value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingDecimalKeyFrame();
        }

        #endregion

        #region DecimalKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Decimal InterpolateValueCore(Decimal baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateDecimal(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingDecimalKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a DoubleKeyFrameCollection in
    /// conjunction with a KeyFrameDoubleAnimation to animate a
    /// Double property value along a set of key frames.
    ///
    /// This DoubleKeyFrame interpolates the between the Double Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingDoubleKeyFrame : DoubleKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingDoubleKeyFrame.
        /// </summary>
        public EasingDoubleKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingDoubleKeyFrame.
        /// </summary>
        public EasingDoubleKeyFrame(Double value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingDoubleKeyFrame.
        /// </summary>
        public EasingDoubleKeyFrame(Double value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingDoubleKeyFrame.
        /// </summary>
        public EasingDoubleKeyFrame(Double value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingDoubleKeyFrame();
        }

        #endregion

        #region DoubleKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Double InterpolateValueCore(Double baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateDouble(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingDoubleKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Int16KeyFrameCollection in
    /// conjunction with a KeyFrameInt16Animation to animate a
    /// Int16 property value along a set of key frames.
    ///
    /// This Int16KeyFrame interpolates the between the Int16 Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingInt16KeyFrame : Int16KeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingInt16KeyFrame.
        /// </summary>
        public EasingInt16KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingInt16KeyFrame.
        /// </summary>
        public EasingInt16KeyFrame(Int16 value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingInt16KeyFrame.
        /// </summary>
        public EasingInt16KeyFrame(Int16 value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingInt16KeyFrame.
        /// </summary>
        public EasingInt16KeyFrame(Int16 value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingInt16KeyFrame();
        }

        #endregion

        #region Int16KeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Int16 InterpolateValueCore(Int16 baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateInt16(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingInt16KeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Int32KeyFrameCollection in
    /// conjunction with a KeyFrameInt32Animation to animate a
    /// Int32 property value along a set of key frames.
    ///
    /// This Int32KeyFrame interpolates the between the Int32 Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingInt32KeyFrame : Int32KeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingInt32KeyFrame.
        /// </summary>
        public EasingInt32KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingInt32KeyFrame.
        /// </summary>
        public EasingInt32KeyFrame(Int32 value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingInt32KeyFrame.
        /// </summary>
        public EasingInt32KeyFrame(Int32 value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingInt32KeyFrame.
        /// </summary>
        public EasingInt32KeyFrame(Int32 value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingInt32KeyFrame();
        }

        #endregion

        #region Int32KeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Int32 InterpolateValueCore(Int32 baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateInt32(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingInt32KeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Int64KeyFrameCollection in
    /// conjunction with a KeyFrameInt64Animation to animate a
    /// Int64 property value along a set of key frames.
    ///
    /// This Int64KeyFrame interpolates the between the Int64 Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingInt64KeyFrame : Int64KeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingInt64KeyFrame.
        /// </summary>
        public EasingInt64KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingInt64KeyFrame.
        /// </summary>
        public EasingInt64KeyFrame(Int64 value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingInt64KeyFrame.
        /// </summary>
        public EasingInt64KeyFrame(Int64 value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingInt64KeyFrame.
        /// </summary>
        public EasingInt64KeyFrame(Int64 value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingInt64KeyFrame();
        }

        #endregion

        #region Int64KeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Int64 InterpolateValueCore(Int64 baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateInt64(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingInt64KeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a PointKeyFrameCollection in
    /// conjunction with a KeyFramePointAnimation to animate a
    /// Point property value along a set of key frames.
    ///
    /// This PointKeyFrame interpolates the between the Point Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingPointKeyFrame : PointKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingPointKeyFrame.
        /// </summary>
        public EasingPointKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingPointKeyFrame.
        /// </summary>
        public EasingPointKeyFrame(Point value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingPointKeyFrame.
        /// </summary>
        public EasingPointKeyFrame(Point value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingPointKeyFrame.
        /// </summary>
        public EasingPointKeyFrame(Point value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingPointKeyFrame();
        }

        #endregion

        #region PointKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Point InterpolateValueCore(Point baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolatePoint(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingPointKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Point3DKeyFrameCollection in
    /// conjunction with a KeyFramePoint3DAnimation to animate a
    /// Point3D property value along a set of key frames.
    ///
    /// This Point3DKeyFrame interpolates the between the Point3D Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingPoint3DKeyFrame : Point3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame(Point3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame(Point3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingPoint3DKeyFrame.
        /// </summary>
        public EasingPoint3DKeyFrame(Point3D value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingPoint3DKeyFrame();
        }

        #endregion

        #region Point3DKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Point3D InterpolateValueCore(Point3D baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolatePoint3D(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingPoint3DKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    ///
    /// This QuaternionKeyFrame interpolates the between the Quaternion Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingQuaternionKeyFrame : QuaternionKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame(Quaternion value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame(Quaternion value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingQuaternionKeyFrame.
        /// </summary>
        public EasingQuaternionKeyFrame(Quaternion value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingQuaternionKeyFrame();
        }

        #endregion

        #region QuaternionKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Quaternion InterpolateValueCore(Quaternion baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateQuaternion(baseValue, Value, keyFrameProgress, UseShortestPath);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingQuaternionKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Rotation3DKeyFrameCollection in
    /// conjunction with a KeyFrameRotation3DAnimation to animate a
    /// Rotation3D property value along a set of key frames.
    ///
    /// This Rotation3DKeyFrame interpolates the between the Rotation3D Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingRotation3DKeyFrame : Rotation3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame(Rotation3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingRotation3DKeyFrame.
        /// </summary>
        public EasingRotation3DKeyFrame(Rotation3D value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingRotation3DKeyFrame();
        }

        #endregion

        #region Rotation3DKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Rotation3D InterpolateValueCore(Rotation3D baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateRotation3D(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingRotation3DKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a RectKeyFrameCollection in
    /// conjunction with a KeyFrameRectAnimation to animate a
    /// Rect property value along a set of key frames.
    ///
    /// This RectKeyFrame interpolates the between the Rect Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingRectKeyFrame : RectKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingRectKeyFrame.
        /// </summary>
        public EasingRectKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingRectKeyFrame.
        /// </summary>
        public EasingRectKeyFrame(Rect value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingRectKeyFrame.
        /// </summary>
        public EasingRectKeyFrame(Rect value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingRectKeyFrame.
        /// </summary>
        public EasingRectKeyFrame(Rect value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingRectKeyFrame();
        }

        #endregion

        #region RectKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Rect InterpolateValueCore(Rect baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateRect(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingRectKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a SingleKeyFrameCollection in
    /// conjunction with a KeyFrameSingleAnimation to animate a
    /// Single property value along a set of key frames.
    ///
    /// This SingleKeyFrame interpolates the between the Single Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingSingleKeyFrame : SingleKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingSingleKeyFrame.
        /// </summary>
        public EasingSingleKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingSingleKeyFrame.
        /// </summary>
        public EasingSingleKeyFrame(Single value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingSingleKeyFrame.
        /// </summary>
        public EasingSingleKeyFrame(Single value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingSingleKeyFrame.
        /// </summary>
        public EasingSingleKeyFrame(Single value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingSingleKeyFrame();
        }

        #endregion

        #region SingleKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Single InterpolateValueCore(Single baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateSingle(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingSingleKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a SizeKeyFrameCollection in
    /// conjunction with a KeyFrameSizeAnimation to animate a
    /// Size property value along a set of key frames.
    ///
    /// This SizeKeyFrame interpolates the between the Size Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingSizeKeyFrame : SizeKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingSizeKeyFrame.
        /// </summary>
        public EasingSizeKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingSizeKeyFrame.
        /// </summary>
        public EasingSizeKeyFrame(Size value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingSizeKeyFrame.
        /// </summary>
        public EasingSizeKeyFrame(Size value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingSizeKeyFrame.
        /// </summary>
        public EasingSizeKeyFrame(Size value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingSizeKeyFrame();
        }

        #endregion

        #region SizeKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Size InterpolateValueCore(Size baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateSize(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingSizeKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a VectorKeyFrameCollection in
    /// conjunction with a KeyFrameVectorAnimation to animate a
    /// Vector property value along a set of key frames.
    ///
    /// This VectorKeyFrame interpolates the between the Vector Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingVectorKeyFrame : VectorKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingVectorKeyFrame.
        /// </summary>
        public EasingVectorKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingVectorKeyFrame.
        /// </summary>
        public EasingVectorKeyFrame(Vector value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingVectorKeyFrame.
        /// </summary>
        public EasingVectorKeyFrame(Vector value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingVectorKeyFrame.
        /// </summary>
        public EasingVectorKeyFrame(Vector value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingVectorKeyFrame();
        }

        #endregion

        #region VectorKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Vector InterpolateValueCore(Vector baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateVector(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingVectorKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }


    /// <summary>
    /// This class is used as part of a Vector3DKeyFrameCollection in
    /// conjunction with a KeyFrameVector3DAnimation to animate a
    /// Vector3D property value along a set of key frames.
    ///
    /// This Vector3DKeyFrame interpolates the between the Vector3D Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingVector3DKeyFrame : Vector3DKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame(Vector3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame(Vector3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingVector3DKeyFrame.
        /// </summary>
        public EasingVector3DKeyFrame(Vector3D value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingVector3DKeyFrame();
        }

        #endregion

        #region Vector3DKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Vector3D InterpolateValueCore(Vector3D baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                return AnimatedTypeHelpers.InterpolateVector3D(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingVector3DKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }
}
