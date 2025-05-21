// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal.KnownBoxes;
// These types are aliased to match the unamanaged names used in interop

namespace System.Windows.Media
{
    sealed partial class StreamGeometry : Geometry
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
        public new StreamGeometry Clone()
        {
            return (StreamGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new StreamGeometry CloneCurrentValue()
        {
            return (StreamGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void FillRulePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StreamGeometry target = ((StreamGeometry) d);


            target.PropertyChanged(FillRuleProperty);
        }


        #region Public Properties

        /// <summary>
        ///     FillRule - FillRule.  Default value is FillRule.EvenOdd.
        /// </summary>
        public FillRule FillRule
        {
            get
            {
                return (FillRule) GetValue(FillRuleProperty);
            }
            set
            {
                SetValueInternal(FillRuleProperty, FillRuleBoxes.Box(value));
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
            return new StreamGeometry();
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
        ///     The DependencyProperty for the StreamGeometry.FillRule property.
        /// </summary>
        public static readonly DependencyProperty FillRuleProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const FillRule c_FillRule = FillRule.EvenOdd;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static StreamGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 



            // Initializations
            Type typeofThis = typeof(StreamGeometry);
            FillRuleProperty =
                  RegisterProperty("FillRule",
                                   typeof(FillRule),
                                   typeofThis,
                                   FillRule.EvenOdd,
                                   new PropertyChangedCallback(FillRulePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsFillRuleValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
