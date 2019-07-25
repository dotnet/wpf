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
using MS.Internal.KnownBoxes;
using MS.Internal.Collections;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Media.Converters;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows.Media
{
    sealed partial class Pen : Animatable, DUCE.IResource
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new Pen Clone()
        {
            return (Pen)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new Pen CloneCurrentValue()
        {
            return (Pen)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void BrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }


            Pen target = ((Pen) d);


            Brush oldV = (Brush) e.OldValue;
            Brush newV = (Brush) e.NewValue;
            System.Windows.Threading.Dispatcher dispatcher = target.Dispatcher;

            if (dispatcher != null)
            {
                DUCE.IResource targetResource = (DUCE.IResource)target;
                using (CompositionEngineLock.Acquire())
                {
                    int channelCount = targetResource.GetChannelCount();

                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        DUCE.Channel channel = targetResource.GetChannel(channelIndex);
                        Debug.Assert(!channel.IsOutOfBandChannel);
                        Debug.Assert(!targetResource.GetHandle(channel).IsNull);
                        target.ReleaseResource(oldV,channel);
                        target.AddRefResource(newV,channel);
                    }
                }
            }

            target.PropertyChanged(BrushProperty);
        }
        private static void ThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(ThicknessProperty);
        }
        private static void StartLineCapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(StartLineCapProperty);
        }
        private static void EndLineCapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(EndLineCapProperty);
        }
        private static void DashCapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(DashCapProperty);
        }
        private static void LineJoinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(LineJoinProperty);
        }
        private static void MiterLimitPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Pen target = ((Pen) d);


            target.PropertyChanged(MiterLimitProperty);
        }
        private static void DashStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }


            Pen target = ((Pen) d);


            DashStyle oldV = (DashStyle) e.OldValue;
            DashStyle newV = (DashStyle) e.NewValue;
            System.Windows.Threading.Dispatcher dispatcher = target.Dispatcher;

            if (dispatcher != null)
            {
                DUCE.IResource targetResource = (DUCE.IResource)target;
                using (CompositionEngineLock.Acquire())
                {
                    int channelCount = targetResource.GetChannelCount();

                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        DUCE.Channel channel = targetResource.GetChannel(channelIndex);
                        Debug.Assert(!channel.IsOutOfBandChannel);
                        Debug.Assert(!targetResource.GetHandle(channel).IsNull);
                        target.ReleaseResource(oldV,channel);
                        target.AddRefResource(newV,channel);
                    }
                }
            }

            target.PropertyChanged(DashStyleProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Brush - Brush.  Default value is null.
        /// </summary>
        public Brush Brush
        {
            get
            {
                return (Brush) GetValue(BrushProperty);
            }
            set
            {
                SetValueInternal(BrushProperty, value);
            }
        }

        /// <summary>
        ///     Thickness - double.  Default value is 1.0.
        /// </summary>
        public double Thickness
        {
            get
            {
                return (double) GetValue(ThicknessProperty);
            }
            set
            {
                SetValueInternal(ThicknessProperty, value);
            }
        }

        /// <summary>
        ///     StartLineCap - PenLineCap.  Default value is PenLineCap.Flat.
        /// </summary>
        public PenLineCap StartLineCap
        {
            get
            {
                return (PenLineCap) GetValue(StartLineCapProperty);
            }
            set
            {
                SetValueInternal(StartLineCapProperty, value);
            }
        }

        /// <summary>
        ///     EndLineCap - PenLineCap.  Default value is PenLineCap.Flat.
        /// </summary>
        public PenLineCap EndLineCap
        {
            get
            {
                return (PenLineCap) GetValue(EndLineCapProperty);
            }
            set
            {
                SetValueInternal(EndLineCapProperty, value);
            }
        }

        /// <summary>
        ///     DashCap - PenLineCap.  Default value is PenLineCap.Square.
        /// </summary>
        public PenLineCap DashCap
        {
            get
            {
                return (PenLineCap) GetValue(DashCapProperty);
            }
            set
            {
                SetValueInternal(DashCapProperty, value);
            }
        }

        /// <summary>
        ///     LineJoin - PenLineJoin.  Default value is PenLineJoin.Miter.
        /// </summary>
        public PenLineJoin LineJoin
        {
            get
            {
                return (PenLineJoin) GetValue(LineJoinProperty);
            }
            set
            {
                SetValueInternal(LineJoinProperty, value);
            }
        }

        /// <summary>
        ///     MiterLimit - double.  Default value is 10.0.
        /// </summary>
        public double MiterLimit
        {
            get
            {
                return (double) GetValue(MiterLimitProperty);
            }
            set
            {
                SetValueInternal(MiterLimitProperty, value);
            }
        }

        /// <summary>
        ///     DashStyle - DashStyle.  Default value is DashStyles.Solid.
        /// </summary>
        public DashStyle DashStyle
        {
            get
            {
                return (DashStyle) GetValue(DashStyleProperty);
            }
            set
            {
                SetValueInternal(DashStyleProperty, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new Pen();
        }



        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal override void UpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                base.UpdateResource(channel, skipOnChannelCheck);

                // Read values of properties into local variables
                Brush vBrush = Brush;
                DashStyle vDashStyle = DashStyle;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hBrush = vBrush != null ? ((DUCE.IResource)vBrush).GetHandle(channel) : DUCE.ResourceHandle.Null;
                DUCE.ResourceHandle hDashStyle = vDashStyle != null ? ((DUCE.IResource)vDashStyle).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hThicknessAnimations = GetAnimationResourceHandle(ThicknessProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_PEN data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdPen;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.hBrush = hBrush;
                    if (hThicknessAnimations.IsNull)
                    {
                        data.Thickness = Thickness;
                    }
                    data.hThicknessAnimations = hThicknessAnimations;
                    data.StartLineCap = StartLineCap;
                    data.EndLineCap = EndLineCap;
                    data.DashCap = DashCap;
                    data.LineJoin = LineJoin;
                    data.MiterLimit = MiterLimit;
                    data.hDashStyle = hDashStyle;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_PEN));
                }
            }
        }
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire()) 
            {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_PEN))
                {
                    Brush vBrush = Brush;
                    if (vBrush != null) ((DUCE.IResource)vBrush).AddRefOnChannel(channel);
                    DashStyle vDashStyle = DashStyle;
                    if (vDashStyle != null) ((DUCE.IResource)vDashStyle).AddRefOnChannel(channel);

                    AddRefOnChannelAnimations(channel);


                    UpdateResource(channel, true /* skip "on channel" check - we already know that we're on channel */ );
                }

                return _duceResource.GetHandle(channel);
            }
        }
        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire()) 
            {
                Debug.Assert(_duceResource.IsOnChannel(channel));

                if (_duceResource.ReleaseOnChannel(channel))
                {
                    Brush vBrush = Brush;
                    if (vBrush != null) ((DUCE.IResource)vBrush).ReleaseOnChannel(channel);
                    DashStyle vDashStyle = DashStyle;
                    if (vDashStyle != null) ((DUCE.IResource)vDashStyle).ReleaseOnChannel(channel);

                    ReleaseOnChannelAnimations(channel);
}
            }
        }
        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            DUCE.ResourceHandle h;
            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                h = _duceResource.GetHandle(channel);
            }
            return h;
        }
        int DUCE.IResource.GetChannelCount()
        {
            // must already be in composition lock here
            return _duceResource.GetChannelCount();
        }
        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            // in a lock already
            return _duceResource.GetChannel(index);
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the Pen.Brush property.
        /// </summary>
        public static readonly DependencyProperty BrushProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.Thickness property.
        /// </summary>
        public static readonly DependencyProperty ThicknessProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.StartLineCap property.
        /// </summary>
        public static readonly DependencyProperty StartLineCapProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.EndLineCap property.
        /// </summary>
        public static readonly DependencyProperty EndLineCapProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.DashCap property.
        /// </summary>
        public static readonly DependencyProperty DashCapProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.LineJoin property.
        /// </summary>
        public static readonly DependencyProperty LineJoinProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.MiterLimit property.
        /// </summary>
        public static readonly DependencyProperty MiterLimitProperty;
        /// <summary>
        ///     The DependencyProperty for the Pen.DashStyle property.
        /// </summary>
        public static readonly DependencyProperty DashStyleProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_Thickness = 1.0;
        internal const PenLineCap c_StartLineCap = PenLineCap.Flat;
        internal const PenLineCap c_EndLineCap = PenLineCap.Flat;
        internal const PenLineCap c_DashCap = PenLineCap.Square;
        internal const PenLineJoin c_LineJoin = PenLineJoin.Miter;
        internal const double c_MiterLimit = 10.0;
        internal static DashStyle s_DashStyle = DashStyles.Solid;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static Pen()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  

            Debug.Assert(s_DashStyle == null || s_DashStyle.IsFrozen,
                "Detected context bound default value Pen.s_DashStyle (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(Pen);
            BrushProperty =
                  RegisterProperty("Brush",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BrushPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ThicknessProperty =
                  RegisterProperty("Thickness",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ThicknessPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            StartLineCapProperty =
                  RegisterProperty("StartLineCap",
                                   typeof(PenLineCap),
                                   typeofThis,
                                   PenLineCap.Flat,
                                   new PropertyChangedCallback(StartLineCapPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            EndLineCapProperty =
                  RegisterProperty("EndLineCap",
                                   typeof(PenLineCap),
                                   typeofThis,
                                   PenLineCap.Flat,
                                   new PropertyChangedCallback(EndLineCapPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            DashCapProperty =
                  RegisterProperty("DashCap",
                                   typeof(PenLineCap),
                                   typeofThis,
                                   PenLineCap.Square,
                                   new PropertyChangedCallback(DashCapPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            LineJoinProperty =
                  RegisterProperty("LineJoin",
                                   typeof(PenLineJoin),
                                   typeofThis,
                                   PenLineJoin.Miter,
                                   new PropertyChangedCallback(LineJoinPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineJoinValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            MiterLimitProperty =
                  RegisterProperty("MiterLimit",
                                   typeof(double),
                                   typeofThis,
                                   10.0,
                                   new PropertyChangedCallback(MiterLimitPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            DashStyleProperty =
                  RegisterProperty("DashStyle",
                                   typeof(DashStyle),
                                   typeofThis,
                                   DashStyles.Solid,
                                   new PropertyChangedCallback(DashStylePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
