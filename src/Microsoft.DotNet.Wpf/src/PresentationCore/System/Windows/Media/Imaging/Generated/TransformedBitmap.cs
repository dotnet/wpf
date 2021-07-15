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
using MS.Internal.PresentationCore;
using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Diagnostics;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows.Media.Imaging
{
    sealed partial class TransformedBitmap : BitmapSource
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
        public new TransformedBitmap Clone()
        {
            return (TransformedBitmap)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TransformedBitmap CloneCurrentValue()
        {
            return (TransformedBitmap)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TransformedBitmap target = ((TransformedBitmap) d);


            target.SourcePropertyChangedHook(e);



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



            target.PropertyChanged(SourceProperty);
        }
        private static void TransformPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TransformedBitmap target = ((TransformedBitmap) d);


            target.TransformPropertyChangedHook(e);



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



            target.PropertyChanged(TransformProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Source - BitmapSource.  Default value is null.
        /// </summary>
        public BitmapSource Source
        {
            get
            {
                return (BitmapSource) GetValue(SourceProperty);
            }
            set
            {
                SetValueInternal(SourceProperty, value);
            }
        }

        /// <summary>
        ///     Transform - Transform.  Default value is Transform.Identity.
        /// </summary>
        public Transform Transform
        {
            get
            {
                return (Transform) GetValue(TransformProperty);
            }
            set
            {
                SetValueInternal(TransformProperty, value);
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
            return new TransformedBitmap();
        }
        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceTransformedBitmap);

            base.CloneCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceTransformedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceTransformedBitmap);

            base.CloneCurrentValueCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceTransformedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceTransformedBitmap);

            base.GetAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceTransformedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceTransformedBitmap);

            base.GetCurrentValueAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceTransformedBitmap);
        }


        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods









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
        ///     The DependencyProperty for the TransformedBitmap.Source property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty;
        /// <summary>
        ///     The DependencyProperty for the TransformedBitmap.Transform property.
        /// </summary>
        public static readonly DependencyProperty TransformProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static BitmapSource s_Source = null;
        internal static Transform s_Transform = Transform.Identity;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static TransformedBitmap()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 

            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value TransformedBitmap.s_Source (See OS Bug #947272).");


            Debug.Assert(s_Transform == null || s_Transform.IsFrozen,
                "Detected context bound default value TransformedBitmap.s_Transform (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(TransformedBitmap);
            SourceProperty =
                  RegisterProperty("Source",
                                   typeof(BitmapSource),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(SourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSource));
            TransformProperty =
                  RegisterProperty("Transform",
                                   typeof(Transform),
                                   typeofThis,
                                   Transform.Identity,
                                   new PropertyChangedCallback(TransformPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceTransform));
        }

        #endregion Constructors
    }
}
