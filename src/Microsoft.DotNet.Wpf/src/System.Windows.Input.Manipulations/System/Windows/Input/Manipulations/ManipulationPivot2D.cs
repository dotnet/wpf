// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents pivot information used by a manipulation processor
    /// for single-manipulator rotations. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a <strong>ManipulationPivot2D</strong> object is assigned to the 
    /// <strong><see cref="P:System.Windows.Input.Manipulations.ManipulationProcessor2D.Pivot"/></strong>
    /// property of a 
    /// <strong><see cref="T:System.Windows.Input.Manipulations.ManipulationProcessor2D"/></strong>
    /// object, it affects how the manipulation processor calculates rotational changes to an element 
    /// when the element is being manipulated by a single manipulator. If more than one manipulator 
    /// is being applied to the element during manipulation, the <strong>Pivot</strong> property is ignored.
    /// </para>
    /// <para>
    /// In a single-manipulator scenario, an element can rotate as it is being dragged. 
    /// The <strong><see cref="X"/></strong> and <strong><see cref="Y"/></strong>
    /// properties of the <strong>ManipulationPivot2D</strong> object determine what position 
    /// the element rotates around, and the 
    /// <strong><see cref="Radius"/></strong> property is used by the manipulation processor to calculate 
    /// the amount of rotational change.
    /// </para>
    /// <para>
    /// For instance, if the single manipulator is near the outer edge of the pivot point, the rotational change 
    /// to the element as it is being dragged will be fairly large (depending upon the size of the element).
    /// If the manipulator is close to the center of the pivot point, very little (if any) rotation will occur. 
    /// </para>
    /// <para>
    /// Typically, the <strong>X</strong> and <strong>Y</strong> properties represent the center of the 
    /// element that is being manipulated, and the <strong>Radius</strong> property represents the distance 
    /// from the center of the element to its farthest edge. 
    /// </para>
    /// <para>
    /// As the element moves, the <strong>X</strong> and <strong>Y</strong> properties of the 
    /// <strong>ManipulationPivot2D</strong> object need to be updated so that rotation will continue
    /// to occur around the proper point. 
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// The following code example shows how the <strong>X</strong> and <strong>Y</strong> properties
    /// for a pivot point are updated to match the center of the element that is being manipulated. 
    /// </para>
    /// <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="ManipulationProcessor2D"/>
    /// </example>
    /// <seealso cref="P:System.Windows.Input.Manipulations.ManipulationProcessor2D.MinimumScaleRotateRadius">ManipulationProcessor2D.MinimumScaleRotateRadius</seealso>
    public sealed class ManipulationPivot2D : ManipulationParameters2D
    {
        private float x = float.NaN;
        private float y = float.NaN;
        private float radius = float.NaN;

        /// <summary>
        /// Gets or sets the X position of the pivot.
        /// </summary>
        /// <remarks>
        /// The <strong>X</strong> property must be a finite value or <strong>NaN</strong>.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "X", Justification = "This is actually the best name for the parameter")]
        public float X
        {
            get { return this.x; }
            set
            {
                Validations.CheckFiniteOrNaN(value, "X");
                this.x = value;
            }
        }

        /// <summary>
        /// Gets or sets the Y position of the pivot.
        /// </summary>
        /// <remarks>
        /// The <strong>Y</strong> property must be a finite value or <strong>NaN</strong>.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Y", Justification = "This is actually the best name for the parameter")]
        public float Y
        {
            get { return this.y; }
            set
            {
                Validations.CheckFiniteOrNaN(value, "Y");
                this.y = value;
            }
        }

        /// <summary>
        /// Gets or sets the distance from the pivot point to the edge of the manipulatable region.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <strong>Radius</strong> property must be a positive, finite value that is 
        /// greater than or equal to 1.0, or <strong>NaN</strong>. 
        /// <strong>NaN</strong> indicates that there is no limit. The default value is <strong>NaN</strong>. 
        /// </para>
        /// <para>
        /// In practice, the pivot point is typically the center of the object that is being manipulated, 
        /// and the <strong>Radius</strong> value is the distance from the pivot point to the 
        /// farthest edge of the object. Any pivoting that occurs within the <strong>Radius</strong> 
        /// distance is dampened. See 
        /// <strong><see cref="T:System.Windows.Input.Manipulations.ManipulationPivot2D"/></strong>
        /// for more information.
        /// </para>
        /// </remarks>
        public float Radius
        {
            get { return this.radius; }
            set
            {
                CheckPivotRadius(value, "Radius");
                this.radius = value;
            }
        }

        /// <summary>
        /// Gets whether the pivot has a position.
        /// </summary>
        internal bool HasPosition
        {
            get
            {
                return !float.IsNaN(this.x) && !float.IsNaN(this.y);
            }
        }

        /// <summary>
        /// Called when this object is passed into the SetParameters
        /// method on a manipulation processor.
        /// </summary>
        /// <param name="processor"></param>
        internal override void Set(ManipulationProcessor2D processor)
        {
            Debug.Assert(processor != null);
            processor.Pivot = this;
        }

        /// <summary>
        /// Verifies that a value for a pivot radius is legal.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        private static void CheckPivotRadius(float value, string paramName)
        {
            if (!float.IsNaN(value) && (float.IsInfinity(value) || (value < 1.0F)))
            {
                throw Exceptions.IllegalPivotRadius(paramName, value);
            }
        }
    }
}
