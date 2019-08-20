// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: The MouseGesture class is used by the developer to create Gestures for 
//                   Mouse Device 
//
//              See spec at : http://avalon/coreUI/Specs/Commanding%20--%20design.htm 
// 
//
//

using System;
using System.Windows.Input;
using System.Windows;
using System.Windows.Markup;
using System.ComponentModel;

namespace System.Windows.Input
{
    /// <summary>
    /// MouseGesture - MouseAction and Modifier combination.
    ///              Can be set on properties of MouseBinding and RoutedCommand.
    /// </summary>
    [TypeConverter(typeof(MouseGestureConverter))]
    [ValueSerializer(typeof(MouseGestureValueSerializer))]
    public class MouseGesture : InputGesture
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

#region Constructors
        /// <summary>
        ///  Constructor
        /// </summary>
        public MouseGesture()   // Mouse action
        {
        }

        /// <summary>
        ///  constructor
        /// </summary>
        /// <param name="mouseAction">Mouse Action</param>
        public MouseGesture(MouseAction mouseAction): this(mouseAction, ModifierKeys.None)
        {
        }

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="mouseAction">Mouse Action</param>
        /// <param name="modifiers">Modifiers</param>
        public MouseGesture( MouseAction mouseAction,ModifierKeys modifiers)   // acclerator action
        {
            if (!MouseGesture.IsDefinedMouseAction(mouseAction))
                throw new InvalidEnumArgumentException("mouseAction", (int)mouseAction, typeof(MouseAction));

            if (!ModifierKeysConverter.IsDefinedModifierKeys(modifiers))
                throw new InvalidEnumArgumentException("modifiers", (int)modifiers, typeof(ModifierKeys));

            _modifiers = modifiers;
            _mouseAction = mouseAction;

            //AttachClassListeners();
        }
#endregion Constructors
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

#region Public Methods
        /// <summary>
        /// Action 
        /// </summary>
        public MouseAction MouseAction
        {
            get 
            { 
                return _mouseAction; 
            }
            set 
            {
                if (!MouseGesture.IsDefinedMouseAction((MouseAction)value))
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(MouseAction));
                if (_mouseAction != value)
                {
                    _mouseAction = (MouseAction)value;
                    OnPropertyChanged("MouseAction");
                }
            }
        }

        /// <summary>
        /// Modifiers 
        /// </summary>
        public ModifierKeys Modifiers
        {
            get 
            { 
                return _modifiers; 
            }
            set 
            {
                if (!ModifierKeysConverter.IsDefinedModifierKeys((ModifierKeys)value))
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ModifierKeys));

                if (_modifiers != value)
                {
                    _modifiers = (ModifierKeys)value;
                    OnPropertyChanged("Modifiers");
                }
            }
        }

        /// <summary>
        ///     Compares InputEventArgs with current Input
        /// </summary>
        /// <param name="targetElement">the element to receive the command</param>
        /// <param name="inputEventArgs">inputEventArgs to compare to</param>
        /// <returns>True - if matches, false otherwise.
        /// </returns>
        public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
        {
            MouseAction mouseAction = GetMouseAction(inputEventArgs);
            if(mouseAction != MouseAction.None)
            {
                return ( ( (int)this.MouseAction == (int)mouseAction ) && ( this.Modifiers == Keyboard.Modifiers ) );
            }
            return false;
        }


        // Helper like Enum.IsDefined,  for MouseAction.
        internal static bool IsDefinedMouseAction(MouseAction mouseAction)
        {
            return (mouseAction >= MouseAction.None && mouseAction <= MouseAction.MiddleDoubleClick);
        }
#endregion Public Methods

        #region Internal NotifyProperty changed

        /// <summary>
        ///     PropertyChanged event Handler
        /// </summary>
        internal event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     PropertyChanged virtual
        /// </summary>
        internal virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
#region Internal Methods

        internal static MouseAction GetMouseAction(InputEventArgs inputArgs)
        {
            MouseAction MouseAction = MouseAction.None;

            MouseEventArgs mouseArgs = inputArgs as MouseEventArgs;
            if(mouseArgs != null)
            {
                if(inputArgs is MouseWheelEventArgs)
                {
                    MouseAction = MouseAction.WheelClick;
                }
                else
                {
                    MouseButtonEventArgs args = inputArgs as MouseButtonEventArgs;

                    switch(args.ChangedButton)
                    {
                        case MouseButton.Left:
                            {
                                if(args.ClickCount == 2)
                                    MouseAction = MouseAction.LeftDoubleClick;
                                else if(args.ClickCount == 1)
                                    MouseAction = MouseAction.LeftClick;
                            }
                            break;

                        case MouseButton.Right:
                            {
                                if(args.ClickCount == 2)
                                    MouseAction = MouseAction.RightDoubleClick;
                                else if(args.ClickCount == 1)
                                    MouseAction = MouseAction.RightClick;
                            }
                            break;

                        case MouseButton.Middle:
                            {
                                if(args.ClickCount == 2)
                                    MouseAction = MouseAction.MiddleDoubleClick;
                                else if(args.ClickCount == 1)
                                    MouseAction = MouseAction.MiddleClick;
                            }
                            break;
                    }
                }
            }
            return MouseAction;
        }

#endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
#region Private Fields
        private MouseAction  _mouseAction = MouseAction.None;
        private ModifierKeys  _modifiers = ModifierKeys.None;
 //       private static bool _classRegistered = false;
#endregion Private Fields
    }
 }
