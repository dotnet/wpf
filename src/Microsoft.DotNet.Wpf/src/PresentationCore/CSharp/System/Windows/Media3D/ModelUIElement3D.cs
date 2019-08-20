// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: 
//              
//

using MS.Internal;
using MS.Internal.Media;
using MS.Internal.Media3D;
using System;
using System.Diagnostics;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Media.Composition;
using System.Windows.Markup;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     ModelUIElement3D is a UIElement3D which draws the given Model3D.
    ///     ModelUIElement3D is usable from Xaml.
    /// </summary>
    [ContentProperty("Model")]
    public sealed class ModelUIElement3D : UIElement3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        
        /// <summary>
        ///     Default ctor
        /// </summary>
        public ModelUIElement3D()
        {
        }
        
        #endregion Constructors
                
        /// <summary>
        ///    DependencyProperty which backs the ModelUIElement3D.Content property.
        /// </summary>
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                    "Model",
                    /* propertyType = */ typeof(Model3D),
                    /* ownerType = */ typeof(ModelUIElement3D),
                    new PropertyMetadata(ModelPropertyChanged),
                    (ValidateValueCallback) delegate { return MediaContext.CurrentMediaContext.WriteAccessEnabled; });

        private static void ModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModelUIElement3D owner = ((ModelUIElement3D) d);

            // if it's not a subproperty change, then we need to change the protected Model property of Visual3D
            if (!e.IsASubPropertyChange)
            {
                owner.Visual3DModel = (Model3D)e.NewValue;
            }
        }
        
        /// <summary>
        ///     The Model3D to render
        /// </summary>
        public Model3D Model
        {
            get
            {
                return (Model3D) GetValue(ModelProperty);
            }

            set
            {
                SetValue(ModelProperty, value);
            }
        }        

        /// <summary>
        /// Called by the Automation infrastructure when AutomationPeer
        /// is requested for this element.
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new UIElement3DAutomationPeer(this);
        }
    }
}

