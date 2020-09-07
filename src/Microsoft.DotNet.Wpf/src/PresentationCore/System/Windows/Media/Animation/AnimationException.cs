// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Runtime.Serialization;
using System.Windows.Media.Animation;

using MS.Internal.PresentationCore;     // SR, SRID

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This is a wrapped exception designed to be thrown when we encounter an exception in
    /// the process of animating.  It provides the AnimationClock controlling the animation, 
    /// the DependencyProperty on which the animation is applied, and IAnimatable target 
    /// element on which the DependencyProperty is set.
    /// </summary>
    [Serializable]
    public sealed class AnimationException : SystemException
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Internal Constructor
        /// </summary>
        /// <param name="clock"></param>
        /// <param name="property"></param>
        /// <param name="target"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        internal AnimationException(
            AnimationClock clock, DependencyProperty property, IAnimatable target,
            string message, Exception innerException)
            : base(message, innerException)
        {
            _clock = clock;
            _property = property;
            _targetElement = target;
        }

        /// <summary>
        /// Constructor used to deserialize the exception
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private AnimationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion // Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Clock represents the AnimationClock currently animating the 
        /// DependencyObject / DependencyProperty pair.
        /// </summary>
        public AnimationClock Clock
        {
            get
            {
                return _clock;
            }
        }

        /// <summary>
        /// Property represents the DependencyProperty that is being animated. The DependencyObject
        /// on which this property is set is the Target.
        /// </summary>
        public DependencyProperty Property
        {
            get
            {
                return _property;
            }
        }

        /// <summary>
        /// Target represents the IAnimatable on which the animation is being applied;
        /// it is the IAnimatable DependencyObject on which 'Property' has been set
        /// </summary>
        public IAnimatable Target
        {
            get
            {
                return _targetElement;
            }
        }

        #endregion // Public Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        [NonSerialized]
        private AnimationClock _clock;

        [NonSerialized]
        private DependencyProperty _property;

        [NonSerialized]
        private IAnimatable _targetElement;

        #endregion // Private Fields
    }
}
