// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    /// <summary>
    /// Is hard to get appropriate dependency properties to do some animations when using existing FrameworkElement, so created this custom control class with specific Dependency properties
    /// </summary>
    public class CustomControlForAnimaion : UserControl
    {
        #region Register Int16 DependencyProperty for animation

        public static readonly DependencyProperty Int16ValueProperty = DependencyProperty.Register("Int16Value", typeof(Int16), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnInt16ValueChanged)));

        public Int16 Int16Value
        {
            get { return (Int16)GetValue(Int16ValueProperty); }
            set { SetValue(Int16ValueProperty, value); }
        }

        private static void OnInt16ValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(Convert.ToDouble(args.NewValue));
        }

        #endregion

        #region Register Int32 DependencyProperty for animation

        public static readonly DependencyProperty Int32ValueProperty = DependencyProperty.Register("Int32Value", typeof(Int32), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnInt32ValueChanged)));

        public Int32 Int32Value
        {
            get { return (Int32)GetValue(Int32ValueProperty); }
            set { SetValue(Int32ValueProperty, value); }
        }

        private static void OnInt32ValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(Convert.ToDouble(args.NewValue));
        }

        #endregion

        #region Register Int64 DependencyProperty for animation

        public static readonly DependencyProperty Int64ValueProperty = DependencyProperty.Register("Int64Value", typeof(Int64), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnInt64ValueChanged)));

        public Int64 Int64Value
        {
            get { return (Int64)GetValue(Int64ValueProperty); }
            set { SetValue(Int64ValueProperty, value); }
        }

        private static void OnInt64ValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(Convert.ToDouble(args.NewValue));
        }

        #endregion

        #region Register Char DependencyProperty for animation

        public static readonly DependencyProperty CharValueProperty = DependencyProperty.Register("CharValue", typeof(char), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCharValueChanged)));

        public char CharValue
        {
            get { return (char)GetValue(CharValueProperty); }
            set { SetValue(CharValueProperty, value); }
        }

        private static void OnCharValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Convert.ToDouble(Convert.ToInt32(args.NewValue));
        }

        #endregion

        #region Register Byte DependencyProperty for animation

        public static readonly DependencyProperty ByteValueProperty = DependencyProperty.Register("ByteValue", typeof(byte), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnByteValueChanged)));

        public byte ByteValue
        {
            get { return (byte)GetValue(ByteValueProperty); }
            set { SetValue(ByteValueProperty, value); }
        }

        private static void OnByteValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(Convert.ToDouble(args.NewValue));
        }

        #endregion

        #region Register Decimal DependencyProperty for animation

        public static readonly DependencyProperty DecimalValueProperty = DependencyProperty.Register("DecimalValue", typeof(decimal), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDecimalValueChanged)));

        public decimal DecimalValue
        {
            get { return (decimal)GetValue(DecimalValueProperty); }
            set { SetValue(DecimalValueProperty, value); }
        }

        private static void OnDecimalValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(Convert.ToDouble(args.NewValue));
        }

        #endregion

        #region Register Quaternion DependencyProperty for animation

        public static readonly DependencyProperty QuaternionValueProperty = DependencyProperty.Register("QuaternionValue", typeof(Quaternion), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnQuaternionValueChanged)));

        public Quaternion QuaternionValue
        {
            get { return (Quaternion)GetValue(QuaternionValueProperty); }
            set { SetValue(QuaternionValueProperty, value); }
        }

        private static void OnQuaternionValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(((Quaternion)args.NewValue).X + ((Quaternion)args.NewValue).Y);
            customControl.Width = Math.Abs(((Quaternion)args.NewValue).W);
            customControl.Opacity = Math.Abs(((Quaternion)args.NewValue).Z);
        }

        #endregion

        #region Register Single DependencyProperty for animation

        public static readonly DependencyProperty SingleValueProperty = DependencyProperty.Register("SingleValue", typeof(Single), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnSingleValueChanged)));

        public Single SingleValue
        {
            get { return (Single)GetValue(SingleValueProperty); }
            set { SetValue(SingleValueProperty, value); }
        }

        private static void OnSingleValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(Convert.ToDouble(args.NewValue));
        }

        #endregion

        #region Register Size DependencyProperty for animation

        public static readonly DependencyProperty SizeValueProperty = DependencyProperty.Register("SizeValue", typeof(Size), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnSizeValueChanged)));

        public Size SizeValue
        {
            get { return (Size)GetValue(SizeValueProperty); }
            set { SetValue(SizeValueProperty, value); }
        }

        private static void OnSizeValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(((Size)args.NewValue).Height);
            customControl.Width = Math.Abs(((Size)args.NewValue).Width);
        }

        #endregion

        #region Register Vector DependencyProperty for animation

        public static readonly DependencyProperty VectorValueProperty = DependencyProperty.Register("VectorValue", typeof(Vector), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnVectorValueChanged)));

        public Vector VectorValue
        {
            get { return (Vector)GetValue(VectorValueProperty); }
            set { SetValue(VectorValueProperty, value); }
        }

        private static void OnVectorValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Height = Math.Abs(((Vector)args.NewValue).X);
            customControl.Width = Math.Abs(((Vector)args.NewValue).Y);
        }

        #endregion

        #region Register Object DependencyProperty for animation

        public static readonly DependencyProperty ObjectValueProperty = DependencyProperty.Register("ObjectValue", typeof(object), typeof(CustomControlForAnimaion), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnObjectValueChanged)));

        public object ObjectValue
        {
            get { return (object)GetValue(ObjectValueProperty); }
            set { SetValue(ObjectValueProperty, value); }
        }

        private static void OnObjectValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CustomControlForAnimaion customControl = (CustomControlForAnimaion)obj;
            customControl.Content = args.NewValue;
        }

        #endregion
    }
}
