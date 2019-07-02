// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Reflection;
    #endregion using;

    /// <summary>
    /// Summary description for AnimatedProperty.
    /// </summary>
    public class AnimatedProperty
    {

        #region Properties
            #region Private Properties
                private Animator _animator = null;
                private object _animated = null;
                private string _prop = null;
                private PropertyInfo _pinf = null;
            #endregion Private Properties
            #region Public Properties
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Constructor.
            /// </summary>
            public AnimatedProperty(object animated, string propertyValue, Animator animator)
            {
                _animator = animator;
                _animated = animated;
                _prop = propertyValue;
                if (animated != null && propertyValue != null)
                {
                    foreach (PropertyInfo inf in animated.GetType().GetProperties())
                    {
                        Console.WriteLine(inf.Name);
                    }
                    _pinf = animated.GetType().GetProperty(propertyValue);
                }

                animator.OnTick += new Animator.AnimationTickEventHandler(TickHandler);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                /// <summary>
                /// handle a tick event
                /// </summary>
                private void TickHandler(object sender, AnimationTickEventArgs e)
                {
                    Animate(e.Counter);
                }
                /// <summary>
                /// Set the animation rate
                /// </summary>
                private void Animate(long tick)
                {
                    if (tick < 0)
                    {
                        throw new ArgumentOutOfRangeException("tick", "value must be positive (or zero)");
                    }
                    try
                    {
                        _pinf.SetValue(_animated, (float)(8 + tick % 32), null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            #endregion Private Methods
            #region Public Methods
            #endregion Public Methods
        #endregion Methods

            
    }
    
}
