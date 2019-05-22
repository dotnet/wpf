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
    /// </summary>
    public abstract class BooleanKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new BooleanKeyFrame.
        /// </summary>
        protected BooleanKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new BooleanKeyFrame.
        /// </summary>
        protected BooleanKeyFrame(Boolean value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteBooleanKeyFrame.
        /// </summary>
        protected BooleanKeyFrame(Boolean value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(BooleanKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Boolean),
                    typeof(BooleanKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Boolean)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Boolean Value
        {
            get
            {
                return (Boolean)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Boolean InterpolateValue(
            Boolean baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Boolean InterpolateValueCore(
            Boolean baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a ByteKeyFrameCollection in
    /// conjunction with a KeyFrameByteAnimation to animate a
    /// Byte property value along a set of key frames.
    /// </summary>
    public abstract class ByteKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new ByteKeyFrame.
        /// </summary>
        protected ByteKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new ByteKeyFrame.
        /// </summary>
        protected ByteKeyFrame(Byte value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteByteKeyFrame.
        /// </summary>
        protected ByteKeyFrame(Byte value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(ByteKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Byte),
                    typeof(ByteKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Byte)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Byte Value
        {
            get
            {
                return (Byte)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Byte InterpolateValue(
            Byte baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Byte InterpolateValueCore(
            Byte baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a CharKeyFrameCollection in
    /// conjunction with a KeyFrameCharAnimation to animate a
    /// Char property value along a set of key frames.
    /// </summary>
    public abstract class CharKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new CharKeyFrame.
        /// </summary>
        protected CharKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new CharKeyFrame.
        /// </summary>
        protected CharKeyFrame(Char value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteCharKeyFrame.
        /// </summary>
        protected CharKeyFrame(Char value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(CharKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Char),
                    typeof(CharKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Char)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Char Value
        {
            get
            {
                return (Char)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Char InterpolateValue(
            Char baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Char InterpolateValueCore(
            Char baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a ColorKeyFrameCollection in
    /// conjunction with a KeyFrameColorAnimation to animate a
    /// Color property value along a set of key frames.
    /// </summary>
    public abstract class ColorKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new ColorKeyFrame.
        /// </summary>
        protected ColorKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new ColorKeyFrame.
        /// </summary>
        protected ColorKeyFrame(Color value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteColorKeyFrame.
        /// </summary>
        protected ColorKeyFrame(Color value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(ColorKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Color),
                    typeof(ColorKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Color)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Color Value
        {
            get
            {
                return (Color)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Color InterpolateValue(
            Color baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Color InterpolateValueCore(
            Color baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a DecimalKeyFrameCollection in
    /// conjunction with a KeyFrameDecimalAnimation to animate a
    /// Decimal property value along a set of key frames.
    /// </summary>
    public abstract class DecimalKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DecimalKeyFrame.
        /// </summary>
        protected DecimalKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DecimalKeyFrame.
        /// </summary>
        protected DecimalKeyFrame(Decimal value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteDecimalKeyFrame.
        /// </summary>
        protected DecimalKeyFrame(Decimal value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(DecimalKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Decimal),
                    typeof(DecimalKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Decimal)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Decimal Value
        {
            get
            {
                return (Decimal)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Decimal InterpolateValue(
            Decimal baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Decimal InterpolateValueCore(
            Decimal baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a DoubleKeyFrameCollection in
    /// conjunction with a KeyFrameDoubleAnimation to animate a
    /// Double property value along a set of key frames.
    /// </summary>
    public abstract class DoubleKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DoubleKeyFrame.
        /// </summary>
        protected DoubleKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DoubleKeyFrame.
        /// </summary>
        protected DoubleKeyFrame(Double value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteDoubleKeyFrame.
        /// </summary>
        protected DoubleKeyFrame(Double value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(DoubleKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Double),
                    typeof(DoubleKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Double)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Double Value
        {
            get
            {
                return (Double)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Double InterpolateValue(
            Double baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Double InterpolateValueCore(
            Double baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a Int16KeyFrameCollection in
    /// conjunction with a KeyFrameInt16Animation to animate a
    /// Int16 property value along a set of key frames.
    /// </summary>
    public abstract class Int16KeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Int16KeyFrame.
        /// </summary>
        protected Int16KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Int16KeyFrame.
        /// </summary>
        protected Int16KeyFrame(Int16 value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteInt16KeyFrame.
        /// </summary>
        protected Int16KeyFrame(Int16 value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Int16KeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Int16),
                    typeof(Int16KeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Int16)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Int16 Value
        {
            get
            {
                return (Int16)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Int16 InterpolateValue(
            Int16 baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Int16 InterpolateValueCore(
            Int16 baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a Int32KeyFrameCollection in
    /// conjunction with a KeyFrameInt32Animation to animate a
    /// Int32 property value along a set of key frames.
    /// </summary>
    public abstract class Int32KeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Int32KeyFrame.
        /// </summary>
        protected Int32KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Int32KeyFrame.
        /// </summary>
        protected Int32KeyFrame(Int32 value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteInt32KeyFrame.
        /// </summary>
        protected Int32KeyFrame(Int32 value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Int32KeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Int32),
                    typeof(Int32KeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Int32)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Int32 Value
        {
            get
            {
                return (Int32)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Int32 InterpolateValue(
            Int32 baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Int32 InterpolateValueCore(
            Int32 baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a Int64KeyFrameCollection in
    /// conjunction with a KeyFrameInt64Animation to animate a
    /// Int64 property value along a set of key frames.
    /// </summary>
    public abstract class Int64KeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Int64KeyFrame.
        /// </summary>
        protected Int64KeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Int64KeyFrame.
        /// </summary>
        protected Int64KeyFrame(Int64 value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteInt64KeyFrame.
        /// </summary>
        protected Int64KeyFrame(Int64 value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Int64KeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Int64),
                    typeof(Int64KeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Int64)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Int64 Value
        {
            get
            {
                return (Int64)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Int64 InterpolateValue(
            Int64 baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Int64 InterpolateValueCore(
            Int64 baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a MatrixKeyFrameCollection in
    /// conjunction with a KeyFrameMatrixAnimation to animate a
    /// Matrix property value along a set of key frames.
    /// </summary>
    public abstract class MatrixKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new MatrixKeyFrame.
        /// </summary>
        protected MatrixKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new MatrixKeyFrame.
        /// </summary>
        protected MatrixKeyFrame(Matrix value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteMatrixKeyFrame.
        /// </summary>
        protected MatrixKeyFrame(Matrix value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(MatrixKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Matrix),
                    typeof(MatrixKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Matrix)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Matrix Value
        {
            get
            {
                return (Matrix)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Matrix InterpolateValue(
            Matrix baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Matrix InterpolateValueCore(
            Matrix baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a ObjectKeyFrameCollection in
    /// conjunction with a KeyFrameObjectAnimation to animate a
    /// Object property value along a set of key frames.
    /// </summary>
    public abstract class ObjectKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new ObjectKeyFrame.
        /// </summary>
        protected ObjectKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new ObjectKeyFrame.
        /// </summary>
        protected ObjectKeyFrame(Object value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteObjectKeyFrame.
        /// </summary>
        protected ObjectKeyFrame(Object value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(ObjectKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Object),
                    typeof(ObjectKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Object)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Object Value
        {
            get
            {
                return (Object)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Object InterpolateValue(
            Object baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Object InterpolateValueCore(
            Object baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a PointKeyFrameCollection in
    /// conjunction with a KeyFramePointAnimation to animate a
    /// Point property value along a set of key frames.
    /// </summary>
    public abstract class PointKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new PointKeyFrame.
        /// </summary>
        protected PointKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new PointKeyFrame.
        /// </summary>
        protected PointKeyFrame(Point value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscretePointKeyFrame.
        /// </summary>
        protected PointKeyFrame(Point value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(PointKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Point),
                    typeof(PointKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Point)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Point Value
        {
            get
            {
                return (Point)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Point InterpolateValue(
            Point baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Point InterpolateValueCore(
            Point baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a Point3DKeyFrameCollection in
    /// conjunction with a KeyFramePoint3DAnimation to animate a
    /// Point3D property value along a set of key frames.
    /// </summary>
    public abstract class Point3DKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Point3DKeyFrame.
        /// </summary>
        protected Point3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Point3DKeyFrame.
        /// </summary>
        protected Point3DKeyFrame(Point3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscretePoint3DKeyFrame.
        /// </summary>
        protected Point3DKeyFrame(Point3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Point3DKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Point3D),
                    typeof(Point3DKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Point3D)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Point3D Value
        {
            get
            {
                return (Point3D)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Point3D InterpolateValue(
            Point3D baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Point3D InterpolateValueCore(
            Point3D baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    /// </summary>
    public abstract class QuaternionKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new QuaternionKeyFrame.
        /// </summary>
        protected QuaternionKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new QuaternionKeyFrame.
        /// </summary>
        protected QuaternionKeyFrame(Quaternion value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteQuaternionKeyFrame.
        /// </summary>
        protected QuaternionKeyFrame(Quaternion value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(QuaternionKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Quaternion),
                    typeof(QuaternionKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Quaternion)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Quaternion Value
        {
            get
            {
                return (Quaternion)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Quaternion InterpolateValue(
            Quaternion baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Quaternion InterpolateValueCore(
            Quaternion baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a Rotation3DKeyFrameCollection in
    /// conjunction with a KeyFrameRotation3DAnimation to animate a
    /// Rotation3D property value along a set of key frames.
    /// </summary>
    public abstract class Rotation3DKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Rotation3DKeyFrame.
        /// </summary>
        protected Rotation3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Rotation3DKeyFrame.
        /// </summary>
        protected Rotation3DKeyFrame(Rotation3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteRotation3DKeyFrame.
        /// </summary>
        protected Rotation3DKeyFrame(Rotation3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Rotation3DKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Rotation3D),
                    typeof(Rotation3DKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Rotation3D)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Rotation3D Value
        {
            get
            {
                return (Rotation3D)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Rotation3D InterpolateValue(
            Rotation3D baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Rotation3D InterpolateValueCore(
            Rotation3D baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a RectKeyFrameCollection in
    /// conjunction with a KeyFrameRectAnimation to animate a
    /// Rect property value along a set of key frames.
    /// </summary>
    public abstract class RectKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new RectKeyFrame.
        /// </summary>
        protected RectKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new RectKeyFrame.
        /// </summary>
        protected RectKeyFrame(Rect value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteRectKeyFrame.
        /// </summary>
        protected RectKeyFrame(Rect value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(RectKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Rect),
                    typeof(RectKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Rect)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Rect Value
        {
            get
            {
                return (Rect)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Rect InterpolateValue(
            Rect baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Rect InterpolateValueCore(
            Rect baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a SingleKeyFrameCollection in
    /// conjunction with a KeyFrameSingleAnimation to animate a
    /// Single property value along a set of key frames.
    /// </summary>
    public abstract class SingleKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new SingleKeyFrame.
        /// </summary>
        protected SingleKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new SingleKeyFrame.
        /// </summary>
        protected SingleKeyFrame(Single value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteSingleKeyFrame.
        /// </summary>
        protected SingleKeyFrame(Single value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(SingleKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Single),
                    typeof(SingleKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Single)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Single Value
        {
            get
            {
                return (Single)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Single InterpolateValue(
            Single baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Single InterpolateValueCore(
            Single baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a SizeKeyFrameCollection in
    /// conjunction with a KeyFrameSizeAnimation to animate a
    /// Size property value along a set of key frames.
    /// </summary>
    public abstract class SizeKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new SizeKeyFrame.
        /// </summary>
        protected SizeKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new SizeKeyFrame.
        /// </summary>
        protected SizeKeyFrame(Size value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteSizeKeyFrame.
        /// </summary>
        protected SizeKeyFrame(Size value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(SizeKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Size),
                    typeof(SizeKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Size)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Size Value
        {
            get
            {
                return (Size)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Size InterpolateValue(
            Size baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Size InterpolateValueCore(
            Size baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a StringKeyFrameCollection in
    /// conjunction with a KeyFrameStringAnimation to animate a
    /// String property value along a set of key frames.
    /// </summary>
    public abstract class StringKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new StringKeyFrame.
        /// </summary>
        protected StringKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new StringKeyFrame.
        /// </summary>
        protected StringKeyFrame(String value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteStringKeyFrame.
        /// </summary>
        protected StringKeyFrame(String value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(StringKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(String),
                    typeof(StringKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (String)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public String Value
        {
            get
            {
                return (String)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public String InterpolateValue(
            String baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract String InterpolateValueCore(
            String baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a VectorKeyFrameCollection in
    /// conjunction with a KeyFrameVectorAnimation to animate a
    /// Vector property value along a set of key frames.
    /// </summary>
    public abstract class VectorKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new VectorKeyFrame.
        /// </summary>
        protected VectorKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new VectorKeyFrame.
        /// </summary>
        protected VectorKeyFrame(Vector value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteVectorKeyFrame.
        /// </summary>
        protected VectorKeyFrame(Vector value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(VectorKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Vector),
                    typeof(VectorKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Vector)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Vector Value
        {
            get
            {
                return (Vector)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Vector InterpolateValue(
            Vector baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Vector InterpolateValueCore(
            Vector baseValue,
            double keyFrameProgress);

        #endregion
    }                 


    /// <summary>
    /// This class is used as part of a Vector3DKeyFrameCollection in
    /// conjunction with a KeyFrameVector3DAnimation to animate a
    /// Vector3D property value along a set of key frames.
    /// </summary>
    public abstract class Vector3DKeyFrame : Freezable, IKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new Vector3DKeyFrame.
        /// </summary>
        protected Vector3DKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new Vector3DKeyFrame.
        /// </summary>
        protected Vector3DKeyFrame(Vector3D value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DiscreteVector3DKeyFrame.
        /// </summary>
        protected Vector3DKeyFrame(Vector3D value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        #endregion

        #region IKeyFrame

        /// <summary>
        /// KeyTime Property
        /// </summary>
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                    "KeyTime",
                    typeof(KeyTime),
                    typeof(Vector3DKeyFrame),
                    new PropertyMetadata(KeyTime.Uniform));

        /// <summary>
        /// The time at which this KeyFrame's value should be equal to the Value
        /// property.
        /// </summary>
        public KeyTime KeyTime
        {
            get
            {
            return (KeyTime)GetValue(KeyTimeProperty);
            }
            set
            {
            SetValueInternal(KeyTimeProperty, value);
            }
        }

        /// <summary>
        /// Value Property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                    "Value",
                    typeof(Vector3D),
                    typeof(Vector3DKeyFrame),
                    new PropertyMetadata());

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        object IKeyFrame.Value
        {
            get
            {
                return Value;
            }
            set
            {
                Value = (Vector3D)value;
            }
        }

        /// <summary>
        /// The value of this key frame at the KeyTime specified.
        /// </summary>
        public Vector3D Value
        {
            get
            {
                return (Vector3D)GetValue(ValueProperty);
            }
            set
            {
                SetValueInternal(ValueProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the interpolated value of the key frame at the progress value
        /// provided.  The progress value should be calculated in terms of this 
        /// specific key frame.
        /// </summary>
        public Vector3D InterpolateValue(
            Vector3D baseValue, 
            double keyFrameProgress)
        {
            if (   keyFrameProgress < 0.0
                || keyFrameProgress > 1.0)
            {
                throw new ArgumentOutOfRangeException("keyFrameProgress");
            }

            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method should be implemented by derived classes to calculate
        /// the value of this key frame at the progress value provided.
        /// </summary>
        protected abstract Vector3D InterpolateValueCore(
            Vector3D baseValue,
            double keyFrameProgress);

        #endregion
    }                 
}
