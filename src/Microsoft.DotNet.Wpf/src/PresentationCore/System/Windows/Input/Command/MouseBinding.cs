// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: The MouseBinding class is used by the developer to create Mouse Input Bindings 
//
//                  See spec at : http://avalon/coreui/Specs/Commanding(new).mht  
// 
//* MouseBinding class serves the purpose of Input Bindings for Mouse Device.
//

using System;
using System.Windows.Input;
using System.Windows;
using System.Windows.Markup;
using System.ComponentModel;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// MouseBinding - Implements InputBinding (generic InputGesture-Command map)
    ///         MouseBinding acts like a map for MouseGesture and Commands. 
    ///         Most of the logic is in InputBinding and MouseGesture, this only 
    ///         facilitates user  to specify MouseAction directly without going in
    ///         MouseGesture path. Also it provides the MouseGestureTypeConverter 
    ///         on the Gesture property to have MouseGesture, like "RightClick"
    ///         defined in Markup as Gesture="RightClick" working.
    /// </summary>
    public class MouseBinding : InputBinding
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///  constructor
        /// </summary>
        public MouseBinding() : base()
        {
        }
                
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="command">Command Associated</param>
        /// <param name="mouseAction">Mouse Action</param>
        internal MouseBinding(ICommand command, MouseAction mouseAction)
            : this(command, new MouseGesture(mouseAction))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="command">Command Associated</param>
        /// <param name="gesture">Mmouse Gesture associated</param>
        public MouseBinding(ICommand command, MouseGesture gesture) : base(command, gesture)
        {
            SynchronizePropertiesFromGesture(gesture);

            // Hooking the handler explicitly becuase base constructor uses _gesture
            // It cannot use Gesture property itself because it is a virtual
            gesture.PropertyChanged += new PropertyChangedEventHandler(OnMouseGesturePropertyChanged);
        }

        #endregion Constructors
        
        //------------------------------------------------------
        //
        //  Properties
        //
        //------------------------------------------------------
        #region Properties       

        /// <summary>
        /// MouseGesture
        /// </summary>
        [TypeConverter(typeof(MouseGestureConverter))]
        [ValueSerializer(typeof(MouseGestureValueSerializer))]
        public override InputGesture Gesture
        {
            get
            {
                return base.Gesture as MouseGesture;
            }
            set
            {
                MouseGesture oldMouseGesture = Gesture as MouseGesture;
                MouseGesture mouseGesture = value as MouseGesture;
                if (mouseGesture != null)
                {
                     base.Gesture  = mouseGesture;
                     SynchronizePropertiesFromGesture(mouseGesture);
                     if (oldMouseGesture != mouseGesture)
                     {
                         if (oldMouseGesture != null)
                         {
                             oldMouseGesture.PropertyChanged -= new PropertyChangedEventHandler(OnMouseGesturePropertyChanged);
                         }
                         mouseGesture.PropertyChanged += new PropertyChangedEventHandler(OnMouseGesturePropertyChanged);
                     }
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.InputBinding_ExpectedInputGesture, typeof(MouseGesture)));
                }
             }
        }

        /// <summary>
        ///     Dependency property for MouseAction
        /// </summary>
        public static readonly DependencyProperty MouseActionProperty =
            DependencyProperty.Register("MouseAction", typeof(MouseAction), typeof(MouseBinding), new UIPropertyMetadata(MouseAction.None, new PropertyChangedCallback(OnMouseActionPropertyChanged)));

        /// <summary>
        ///     MouseAction
        /// </summary>
        public MouseAction MouseAction
        {
            get
            {
                return (MouseAction)GetValue(MouseActionProperty);
            }
            set
            {
                SetValue(MouseActionProperty, value);
            }
        }

        private static void OnMouseActionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MouseBinding mouseBinding = (MouseBinding)d;
            mouseBinding.SynchronizeGestureFromProperties((MouseAction)(e.NewValue));
        }

        #endregion

        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new MouseBinding();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
            CloneGesture();
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            base.CloneCurrentValueCore(sourceFreezable);
            CloneGesture();
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetAsFrozenCore(sourceFreezable);
            CloneGesture();
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CloneGesture();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Synchronized Properties from Gesture
        /// </summary>
        private void SynchronizePropertiesFromGesture(MouseGesture mouseGesture)
        {
            if (!_settingGesture)
            {
                _settingGesture = true;
                try
                {
                    MouseAction = mouseGesture.MouseAction;
                }
                finally
                {
                    _settingGesture = false;
                }
            }
        }

        /// <summary>
        ///     Synchronized Gesture from properties
        /// </summary>
        private void SynchronizeGestureFromProperties(MouseAction mouseAction)
        {
            if (!_settingGesture)
            {
                _settingGesture = true;
                try
                {
                    if (Gesture == null)
                    {
                        Gesture = new MouseGesture(mouseAction);
                    }
                    else
                    {
                        ((MouseGesture)Gesture).MouseAction = mouseAction;
                    }
                }
                finally
                {
                    _settingGesture = false;
                }
            }
        }

        private void OnMouseGesturePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, "MouseAction", StringComparison.Ordinal) == 0)
            {
                MouseGesture mouseGesture = Gesture as MouseGesture;
                if (mouseGesture != null)
                {
                    SynchronizePropertiesFromGesture(mouseGesture);
                }
            }
        }

        private void CloneGesture()
        {
            MouseGesture mouseGesture = Gesture as MouseGesture;
            if (mouseGesture != null)
            {
                mouseGesture.PropertyChanged += new PropertyChangedEventHandler(OnMouseGesturePropertyChanged);
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Data
        private bool _settingGesture = false;
        #endregion
    }
 }
