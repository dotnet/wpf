// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Diagnostics.CodeAnalysis;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents a 2D manipulator at an instant in time.
    /// </summary>
    public struct Manipulator2D
    {
        private int id;
        private float x;
        private float y;

        /// <summary>
        /// Determines whether two specified 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulator2D"></see></strong> 
        /// objects have the same value.
        /// </summary>
        /// <param name="manipulator1">The first <strong>Manipulator2D</strong> object to compare.</param>
        /// <param name="manipulator2">The second <strong>Manipulator2D</strong> object to compare.</param>
        /// <returns><strong>true</strong> if the two <strong>Manipulator2D</strong> objects have the same value;
        /// otherwise, <strong>false</strong>.</returns>
        public static bool operator ==(Manipulator2D manipulator1, Manipulator2D manipulator2)
        {
            return manipulator1.Id == manipulator2.Id &&
                manipulator1.X == manipulator2.X &&
                manipulator1.Y == manipulator2.Y;
        }

        /// <summary>
        /// Determines whether two specified 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulator2D"></see></strong>
        /// objects have different values.
        /// </summary>
        /// <param name="manipulator1">The first <strong>Manipulator2D</strong> object to compare.</param>
        /// <param name="manipulator2">The second <strong>Manipulator2D</strong> object to compare.</param>
        /// <returns><strong>true</strong> if the two <strong>Manipulator2D</strong> objects have different values;
        /// otherwise, <strong>false</strong>.</returns>
        public static bool operator !=(Manipulator2D manipulator1, Manipulator2D manipulator2)
        {
            return !(manipulator1 == manipulator2);
        }

        /// <summary>
        /// Determines whether this
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulator2D"></see></strong> 
        /// object has the same value as the specified <strong>Manipulator2D</strong> object.
        /// </summary>
        /// <param name="obj">The <strong>Manipulator2D</strong> object to compare this object to.</param>
        /// <returns><strong>true</strong> if the two <strong>Manipulator2D</strong> objects are the same type and 
        /// represent the same value; otherwise, <strong>false</strong>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Manipulator2D)
            {
                return (Manipulator2D)obj == this;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return (id.GetHashCode() ^ x.GetHashCode() ^ y.GetHashCode());
        }

        /// <summary>
        /// Creates a new <strong><see cref="System.Windows.Input.Manipulations.Manipulator2D"></see></strong> object with the specified properties.
        /// </summary>
        /// <param name="id">The unique ID for this manipulator.</param>
        /// <param name="x">The x-coordinate of the manipulator.</param>
        /// <param name="y">The y-coordinate of the manipulator.</param>
        /// <remarks>
        /// The <em>x</em> and <em>y</em> parameters must both be a finite number.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The x-coordinate or y-coordinate are <strong>float.NaN</strong>,
        /// <strong>float.PositiveInfinity</strong>, or <strong>float.NegativeInfinity</strong>. These values are are invalid.</exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x", Justification = "This is actually the best name for the parameter")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y", Justification = "This is actually the best name for the parameter")]
        public Manipulator2D(int id, float x, float y)
        {
            Validations.CheckFinite(x, "x");
            Validations.CheckFinite(y, "y");

            this.id = id;
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets or sets the unique ID for this 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulator2D"></see></strong>
        /// object.
        /// </summary>
        public int Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }

        /// <summary>
        /// Gets or sets the x-coordinate of this 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulator2D"></see></strong>
        /// object.
        /// </summary>
        /// <remarks>
        /// When setting this property, the value must be a finite value. 
        /// The default value for this property is zero (0).
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "X", Justification = "This is actually the best name for the parameter")]
        public float X
        {
            get
            {
                return this.x;
            }
            set
            {
                Validations.CheckFinite(value, "X");
                this.x = value;
            }
        }

        /// <summary>
        /// Gets or sets the y-coordinate of this 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulator2D"></see></strong>
        /// object.
        /// </summary>
        /// <remarks>
        /// When setting this property, the value must be a finite value.
        /// The default value for this property is zero (0).
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Y", Justification = "This is actually the best name for the parameter")]
        public float Y
        {
            get
            {
                return this.y;
            }
            set
            {
                Validations.CheckFinite(value, "Y");
                this.y = value;
            }
        }
    }
}
