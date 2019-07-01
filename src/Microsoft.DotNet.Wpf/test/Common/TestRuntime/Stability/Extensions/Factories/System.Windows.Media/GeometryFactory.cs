// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete Geometry factory.
    /// Transform Property is covered.
    /// </summary>
    /// <typeparam name="GeometryType"/>
    internal abstract class GeometryFactory<GeometryType> : DiscoverableFactory<GeometryType> where GeometryType : Geometry
    {
        #region Public Members

        public Transform Transform { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use default constructor or not.
        /// </summary>
        public bool UseDefaultConstructor { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="geometry"/>
        protected void ApplyTransform(Geometry geometry)
        {
            geometry.Transform = Transform;
            //HACK Unfrozen freezables may cause memory leak!
        }

        #endregion
    }
}
