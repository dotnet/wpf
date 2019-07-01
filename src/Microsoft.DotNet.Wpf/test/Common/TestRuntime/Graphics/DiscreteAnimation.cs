// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Markup;

namespace Microsoft.Test.Graphics
{
    /// <Summary>
    /// Discrete Animation object.
    /// Takes a timeline and set of objects to be alternated
    /// during that timeline.
    /// </Summary>
    public class DiscreteAnimation : AnimationTimeline
    {
        #region Fields
        private object[] _values;

        /// <summary>
        /// Sequence of Discrete Animation states
        /// </summary>
        protected object[] Values
        {
            get { return _values; }
            set { _values=value; }
        }
        #endregion

        #region Constructors
        /// <Summary>
        /// Constructor requires an object array.
        /// </Summary>
        public DiscreteAnimation(object[] values)
            : base()
        {
            _values = values;
        }

        /// <Summary>
        /// default constructor.
        /// </Summary>
        public DiscreteAnimation()
            : this(new object[0])
        {
        }

        /// <Summary>
        /// Constructor for toggle animation.
        /// </Summary>
        public DiscreteAnimation(object from, object to, Duration duration)
            : this(new object[] { from, to })
        {
            this.Duration = duration;
        }
        #endregion

        #region Freezable
        /// <summary>
        /// Creates a copy of this DiscreteAnimation
        /// </summary>
        /// <returns>The copy</returns>
        public new DiscreteAnimation Clone()
        {
            return (DiscreteAnimation)base.Clone();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore()">Freezable.CreateInstanceCore</see>.
        /// </summary>
        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new DiscreteAnimation();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);

            DiscreteAnimation original = (DiscreteAnimation)sourceFreezable;
            this._values = new object[original._values.Length];
            original._values.CopyTo(this._values, 0);
        }
        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetAsFrozenCore(sourceFreezable);

            DiscreteAnimation original = (DiscreteAnimation)sourceFreezable;
            this._values = new object[original._values.Length];
            original._values.CopyTo(this._values, 0);
        }
        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            DiscreteAnimation original = (DiscreteAnimation)sourceFreezable;
            this._values = new object[original._values.Length];
            original._values.CopyTo(this._values, 0);
        }

        /// <summary/>
        public new DiscreteAnimation GetAsFrozen()
        {
            return (DiscreteAnimation)base.GetAsFrozen();
        }
        #endregion

        #region IAnimation
        /// <summary>
        /// Returns true if the animation can animate a value of a given type.
        /// </summary>
        public override System.Type TargetPropertyType
        {
            get
            {
                if (_values != null && _values.Length > 0)
                {
                    return _values[0].GetType();
                }
                else
                {
                    return typeof(object);
                }
            }
        }

        /// <summary>
        /// Calculates the value of the animation at the current time.
        /// </summary>
        public override object GetCurrentValue(object defaultOriginValue, object baseValue, System.Windows.Media.Animation.AnimationClock animClock)
        {
            // return discrete value ...
            int index = (int)Math.Floor((double)(animClock.CurrentProgress * (_values.Length)));
            return _values[(index < _values.Length) ? index : _values.Length - 1];
        }
        #endregion
        
        /// <summary/>
        protected virtual void AddChild(object o)
        {
            object[] newValues = new object[_values.Length + 1];
            _values.CopyTo(newValues, 0);
            newValues[_values.Length] = o;
            _values = newValues;
        }
    }
}
