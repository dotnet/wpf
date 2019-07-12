// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.ComponentModel;
using System.Collections;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

using MS.Internal.PresentationCore;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    ///     A command that causes handlers associated with it to be called.
    /// </summary>
    [TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
    [ValueSerializer("System.Windows.Input.CommandValueSerializer, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
    public class RoutedCommand : ICommand
    {
        #region Constructors

        /// <summary>
        ///     Default Constructor - needed to allow markup creation
        /// </summary>
        public RoutedCommand()
        {
            _name = String.Empty;
            _ownerType = null;
            _inputGestureCollection = null;
        }

        /// <summary>
        /// RoutedCommand Constructor with Name and OwnerType
        /// </summary>
        /// <param name="name">Declared Name of the RoutedCommand for Serialization</param>
        /// <param name="ownerType">Type that is registering the property</param>
        public RoutedCommand(string name, Type ownerType) : this(name, ownerType, null)
        {
        }

        /// <summary>
        /// RoutedCommand Constructor with Name and OwnerType
        /// </summary>
        /// <param name="name">Declared Name of the RoutedCommand for Serialization</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="inputGestures">Default Input Gestures associated</param>
        public RoutedCommand(string name, Type ownerType, InputGestureCollection inputGestures)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.StringEmpty), "name");
            }

            if (ownerType == null)
            {
                throw new ArgumentNullException("ownerType");
            }

            _name = name;
            _ownerType = ownerType;
            _inputGestureCollection = inputGestures;
        }

        /// <summary>
        /// RoutedCommand Constructor with Name and OwnerType and command identifier.
        /// </summary>
        /// <param name="name">Declared Name of the RoutedCommand for Serialization</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="commandId">Byte identifier for the command assigned by the owning type</param>
        internal RoutedCommand(string name, Type ownerType, byte commandId) : this(name, ownerType, null)
        {
            _commandId = commandId;
        }

        #endregion

        #region ICommand

        /// <summary>
        ///     Executes the command with the given parameter on the currently focused element.
        /// </summary>
        /// <param name="parameter">Parameter to pass to any command handlers.</param>
        void ICommand.Execute(object parameter)
        {
            Execute(parameter, FilterInputElement(Keyboard.FocusedElement));
        }

        /// <summary>
        ///     Whether the command can be executed with the given parameter on the currently focused element.
        /// </summary>
        /// <param name="parameter">Parameter to pass to any command handlers.</param>
        /// <returns>true if the command can be executed, false otherwise.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            bool unused;
            return CanExecuteImpl(parameter, FilterInputElement(Keyboard.FocusedElement), false, out unused);
        }

        /// <summary>
        ///     Raised when CanExecute should be requeried on commands.
        ///     Since commands are often global, it will only hold onto the handler as a weak reference.
        ///     Users of this event should keep a strong reference to their event handler to avoid
        ///     it being garbage collected. This can be accomplished by having a private field
        ///     and assigning the handler as the value before or after attaching to this event.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Executes the command with the given parameter on the given target.
        /// </summary>
        /// <param name="parameter">Parameter to be passed to any command handlers.</param>
        /// <param name="target">Element at which to begin looking for command handlers.</param>
        public void Execute(object parameter, IInputElement target)
        {
            // We only support UIElement, ContentElement and UIElement3D
            if ((target != null) && !InputElement.IsValid(target))
            {
                throw new InvalidOperationException(SR.Get(SRID.Invalid_IInputElement, target.GetType()));
            }

            if (target == null)
            {
                target = FilterInputElement(Keyboard.FocusedElement);
            }

            ExecuteImpl(parameter, target, false);
        }

        /// <summary>
        ///     Whether the command can be executed with the given parameter on the given target.
        /// </summary>
        /// <param name="parameter">Parameter to be passed to any command handlers.</param>
        /// <param name="target">The target element on which to begin looking for command handlers.</param>
        /// <returns>true if the command can be executed, false otherwise.</returns>
        public bool CanExecute(object parameter, IInputElement target)
        {
            bool unused;
            return CriticalCanExecute(parameter, target, false, out unused);
        }

        /// <summary>
        ///     Whether the command can be executed with the given parameter on the given target.
        /// </summary>
        /// <param name="parameter">Parameter to be passed to any command handlers.</param>
        /// <param name="target">The target element on which to begin looking for command handlers.</param>
        /// <param name="trusted">Determines whether this call will elevate for userinitiated input or not.</param>
        /// <param name="continueRouting">Determines whether the input event (if any) that caused this command should continue its route.</param>
        /// <returns>true if the command can be executed, false otherwise.</returns>
        internal bool CriticalCanExecute(object parameter, IInputElement target, bool trusted, out bool continueRouting)
        {
            // We only support UIElement, ContentElement, and UIElement3D
            if ((target != null) && !InputElement.IsValid(target))
            {
                throw new InvalidOperationException(SR.Get(SRID.Invalid_IInputElement, target.GetType()));
            }

            if (target == null)
            {
                target = FilterInputElement(Keyboard.FocusedElement);
            }

            return CanExecuteImpl(parameter, target, trusted, out continueRouting);
        }


        #endregion

        #region Public Properties

        /// <summary>
        /// Name - Declared time Name of the property/field where it is
        ///              defined, for serialization/debug purposes only.
        ///     Ex: public static RoutedCommand New  { get { new RoutedCommand("New", .... ) } }
        ///          public static RoutedCommand New = new RoutedCommand("New", ... ) ;
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        ///     Owning type of the property
        /// </summary>
        public Type OwnerType
        {
            get
            {
                return _ownerType;
            }
        }

        /// <summary>
        ///     Identifier assigned by the owning Type. Note that this is not a global command identifier.
        /// </summary>
        internal byte CommandId
        {
            get
            {
                return _commandId;
            }
        }

        /// <summary>
        ///     Input Gestures associated with RoutedCommand
        /// </summary>
        public InputGestureCollection InputGestures
        {
            get
            {
                if(InputGesturesInternal == null)
                {
                    _inputGestureCollection = new InputGestureCollection();
                }
                return _inputGestureCollection;
            }
        }

        internal InputGestureCollection InputGesturesInternal
        {
            get
            {
                if(_inputGestureCollection == null && AreInputGesturesDelayLoaded)
                {
                    _inputGestureCollection = GetInputGestures();
                    AreInputGesturesDelayLoaded = false;
                }
                return _inputGestureCollection;
            }
        }

        /// <summary>
        ///    Fetches the default input gestures for the command by invoking the LoadDefaultGestureFromResource function on the owning type.
        /// </summary>
        /// <returns>collection of input gestures for the command</returns>
        private InputGestureCollection GetInputGestures()
        {
            if(OwnerType == typeof(ApplicationCommands))
            {
                return ApplicationCommands.LoadDefaultGestureFromResource(_commandId);
            }
            else if(OwnerType == typeof(NavigationCommands))
            {
                return NavigationCommands.LoadDefaultGestureFromResource(_commandId);
            }
            else if(OwnerType == typeof(MediaCommands))
            {
                return MediaCommands.LoadDefaultGestureFromResource(_commandId);
            }
            else if(OwnerType == typeof(ComponentCommands))
            {
                return ComponentCommands.LoadDefaultGestureFromResource(_commandId);
            }
            return new InputGestureCollection();
        }

        /// <summary>
        /// Rights Management Enabledness
        ///     Will be set by Rights Management code.
        /// </summary>
        /// <value></value>
        internal bool IsBlockedByRM
        {
            get
            {
                return ReadPrivateFlag(PrivateFlags.IsBlockedByRM);
            }

            set
            {
                WritePrivateFlag(PrivateFlags.IsBlockedByRM, value);
            }
        }

        internal bool AreInputGesturesDelayLoaded
        {
            get
            {
                return ReadPrivateFlag(PrivateFlags.AreInputGesturesDelayLoaded);
            }

            
            set
            {
                WritePrivateFlag(PrivateFlags.AreInputGesturesDelayLoaded, value);
            }
        }

        #endregion

        #region Implementation

        private static IInputElement FilterInputElement(IInputElement elem)
        {
            // We only support UIElement, ContentElement, and UIElement3D
            if ((elem != null) && InputElement.IsValid(elem))
            {
                return elem;
            }

            return null;
        }

        /// <param name="parameter"></param>
        /// <param name="target"></param>
        /// <param name="trusted"></param>
        /// <param name="continueRouting"></param>
        /// <returns></returns>
        private bool CanExecuteImpl(object parameter, IInputElement target, bool trusted, out bool continueRouting)
        {
            // If blocked by rights-management fall through and return false
            if ((target != null) && !IsBlockedByRM)
            {
                // Raise the Preview Event, check the Handled value, and raise the regular event.
                CanExecuteRoutedEventArgs args = new CanExecuteRoutedEventArgs(this, parameter);
                args.RoutedEvent = CommandManager.PreviewCanExecuteEvent;
                CriticalCanExecuteWrapper(parameter, target, trusted, args);
                if (!args.Handled)
                {
                    args.RoutedEvent = CommandManager.CanExecuteEvent;
                    CriticalCanExecuteWrapper(parameter, target, trusted, args);
                }

                continueRouting = args.ContinueRouting;
                return args.CanExecute;
            }
            else
            {
                continueRouting = false;
                return false;
            }
        }

        private void CriticalCanExecuteWrapper(object parameter, IInputElement target, bool trusted, CanExecuteRoutedEventArgs args)
        {
            // This cast is ok since we are already testing for UIElement, ContentElement, or UIElement3D
            // both of which derive from DO
            DependencyObject targetAsDO = (DependencyObject)target;
            
            if (InputElement.IsUIElement(targetAsDO))
            {
                ((UIElement)targetAsDO).RaiseEvent(args, trusted);
            }
            else if (InputElement.IsContentElement(targetAsDO))
            {
                ((ContentElement)targetAsDO).RaiseEvent(args, trusted);
            }
            else if (InputElement.IsUIElement3D(targetAsDO))
            {
                ((UIElement3D)targetAsDO).RaiseEvent(args, trusted);
            }            
        }
        internal bool ExecuteCore(object parameter, IInputElement target, bool userInitiated)
        {
            if (target == null)
            {
                target = FilterInputElement(Keyboard.FocusedElement);
            }

            return ExecuteImpl(parameter, target, userInitiated);
        }

        private bool ExecuteImpl(object parameter, IInputElement target, bool userInitiated)
        {
            // If blocked by rights-management fall through and return false
            if ((target != null) && !IsBlockedByRM)
            {
                UIElement targetUIElement = target as UIElement;
                ContentElement targetAsContentElement = null;
                UIElement3D targetAsUIElement3D = null;

                // Raise the Preview Event and check for Handled value, and
                // Raise the regular ExecuteEvent.
                ExecutedRoutedEventArgs args = new ExecutedRoutedEventArgs(this, parameter);
                args.RoutedEvent = CommandManager.PreviewExecutedEvent;
                
                if (targetUIElement != null)
                {
                    targetUIElement.RaiseEvent(args, userInitiated);
                }
                else
                {
                    targetAsContentElement = target as ContentElement;
                    if (targetAsContentElement != null)
                    {
                        targetAsContentElement.RaiseEvent(args, userInitiated);
                    }
                    else
                    {
                        targetAsUIElement3D = target as UIElement3D;
                        if (targetAsUIElement3D != null)
                        {
                            targetAsUIElement3D.RaiseEvent(args, userInitiated);
                        }
                    }                    
                }

                if (!args.Handled)
                {
                    args.RoutedEvent = CommandManager.ExecutedEvent;
                    if (targetUIElement != null)
                    {
                        targetUIElement.RaiseEvent(args, userInitiated);
                    }
                    else if (targetAsContentElement != null)
                    {
                        targetAsContentElement.RaiseEvent(args, userInitiated);
                    }
                    else if (targetAsUIElement3D != null)
                    {
                        targetAsUIElement3D.RaiseEvent(args, userInitiated);
                    }
                }

                return args.Handled;
            }

            return false;
        }

        #endregion

        #region PrivateMethods

        private void WritePrivateFlag(PrivateFlags bit, bool value)
        {
            if (value)
            {
                _flags.Value |= bit;
            }
            else
            {
                _flags.Value &= ~bit;
            }
        }

        private bool ReadPrivateFlag(PrivateFlags bit)
        {
            return (_flags.Value & bit) != 0;
        }

        #endregion PrivateMethods

        #region Data

        private string _name;

        private MS.Internal.SecurityCriticalDataForSet<PrivateFlags> _flags;

        private enum PrivateFlags : byte
        {
            IsBlockedByRM = 0x01,
            AreInputGesturesDelayLoaded = 0x02
        }

        private Type _ownerType;
        private InputGestureCollection _inputGestureCollection;
        private byte _commandId; //Note that this is NOT a global command identifier. It is specific to the owning type.
        //it represents one of the CommandID enums defined by each of the command types and will be cast to one of them when used.

        #endregion
    }
 }
