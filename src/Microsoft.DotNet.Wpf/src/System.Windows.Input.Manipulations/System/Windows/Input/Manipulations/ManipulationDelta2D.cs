// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents the result of a 2D manipulation.
    /// </summary>
    /// <remarks>
    /// A <strong>ManipulationDelta2D</strong> object is used in the event arguments of
    /// <strong><see cref="T:System.Windows.Input.Manipulations.Manipulation2DDeltaEventArgs"/></strong>
    /// to inform the event handler of the changes (both incremental and cumulative) 
    /// involved in the manipulation, and in the event arguments of
    /// <strong><see cref="T:System.Windows.Input.Manipulations.Manipulation2DCompletedEventArgs"/></strong>
    /// to inform the event handler of the total changes involved in the manipulation.
    /// </remarks>
    public class ManipulationDelta2D
    {
        private readonly float translationX;
        private readonly float translationY;
        private readonly float rotation;
        private readonly float scaleX;
        private readonly float scaleY;
        private readonly float expansionX;
        private readonly float expansionY;

        /// <summary>
        /// Gets the translation along the x-axis, in coordinate units.
        /// </summary>
        public float TranslationX
        {
            get { return this.translationX; }
        }

        /// <summary>
        /// Gets the translation along the y-axis, in coordinate units.
        /// </summary>
        public float TranslationY
        {
            get { return this.translationY; }
        }

        /// <summary>
        /// Gets the amount of rotation, in radians.
        /// </summary>
        public float Rotation
        {
            get { return this.rotation; }
        }

        /// <summary>
        /// Gets the scale factor along the x-axis.
        /// </summary>
        public float ScaleX
        {
            get { return this.scaleX; }
        }

        /// <summary>
        /// Gets the scale factor along the y-axis.
        /// </summary>
        public float ScaleY
        {
            get { return this.scaleY; }
        }

        /// <summary>
        /// Gets the amount of expansion along the x-axis, in coordinate units.
        /// </summary>
        public float ExpansionX
        {
            get { return this.expansionX; }
        }

        /// <summary>
        /// Gets the amount of expansion along the y-axis, in coordinate units.
        /// </summary>
        public float ExpansionY
        {
            get { return this.expansionY; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="translationX">Translation along x-axis, in coordinate units.</param>
        /// <param name="translationY">Translation along y-axis, in coordinate units.</param>
        /// <param name="rotation">Rotation amount, in radians.</param>
        /// <param name="scaleX">Scale factor along x-axis.</param>
        /// <param name="scaleY">Scale factor along y-axis.</param>
        /// <param name="expansionX">Expansion along x-axis, in coordinate units.</param>
        /// <param name="expansionY">Expansion along y-axis, in coordinate units.</param>
        internal ManipulationDelta2D(
            float translationX,
            float translationY,
            float rotation,
            float scaleX,
            float scaleY,
            float expansionX,
            float expansionY)
        {
            Debug.Assert(Validations.IsFinite(translationX), "translationX should be finite");
            Debug.Assert(Validations.IsFinite(translationY), "translationY should be finite");
            Debug.Assert(Validations.IsFinite(rotation), "rotation should be finite");
            Debug.Assert(Validations.IsFiniteNonNegative(scaleX), "scaleX should be finite and not negative");
            Debug.Assert(Validations.IsFiniteNonNegative(scaleY), "scaleY should be finite and not negative");
            Debug.Assert(Validations.IsFinite(expansionX), "expansionX should be finite");
            Debug.Assert(Validations.IsFinite(expansionY), "expansionY should be finite");
            this.translationX = translationX;
            this.translationY = translationY;
            this.rotation = rotation;
            this.scaleX = scaleX;
            this.scaleY = scaleY;
            this.expansionX = expansionX;
            this.expansionY = expansionY;
        }
    }
}
