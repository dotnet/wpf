// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Implementation of Path shape element.
//

using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Security;
using System.Windows;
using System.Windows.Media;

using System;
using MS.Internal.PresentationFramework;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The Path shape element
    /// This element (like all shapes) belongs under a Canvas,
    /// and will be presented by the parent canvas.
    /// Since a Path is really a path which closes its path
    /// </summary>
    public sealed class Path : Shape 
    {
        #region Constructors

        /// <summary>
        /// Instantiates a new instance of a Path.
        /// </summary>
        public Path()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        /// Data property
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            "Data", 
            typeof(Geometry), 
            typeof(Path), 
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
            null);

        /// <summary>
        /// Data property
        /// </summary>
        public Geometry Data
        {
            get
            {
                return (Geometry)GetValue(DataProperty);
            }
            set
            {
                SetValue(DataProperty, value);
            }
        }
        #endregion

        #region Protected Methods and Properties
        
        /// <summary>
        /// Get the path that defines this shape
        /// </summary>
        protected override Geometry DefiningGeometry
        {
            get
            {
                Geometry data = Data;

                if (data == null)
                {
                    data = Geometry.Empty;
                }

                return data;
            }
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 13; }
        }

        #endregion
   }
}
