// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;              // SecurityCritical, TreatAsSafe
using System.Security.Permissions;
using System.Windows;
using System.Windows.Markup;
using System.ComponentModel;

using MS.Internal;
using MS.Internal.PresentationCore;


namespace System.Windows.Input 
{
    /// <summary>
    /// InputBinding - InputGesture and ICommand combination
    ///                Used to specify the binding between Gesture and Command at Element level.
    /// </summary>
    public class InputBinding : Freezable, ICommandSource
    {
#region Constructor

        /// <summary>
        ///     Default Constructor - needed to allow markup creation
        /// </summary>
        protected InputBinding()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="gesture">Input Gesture</param>
        /// <SecurityNote>
        /// Critical - may associate a secure command with a gesture, in 
        ///            these cases we need to demand the appropriate permission.
        /// TreatAsSafe - Calls CheckSecureCommand which does the appropriate demand.
        /// </SecurityNote>
        public InputBinding(ICommand command, InputGesture gesture) 
        {   
            if (command == null)
                throw new ArgumentNullException("command");

            if (gesture == null)
                throw new ArgumentNullException("gesture");

            // Check before assignment to avoid continuation
            CheckSecureCommand(command, gesture);

            Command = command;
            _gesture = gesture;
        }

#endregion Constructor
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
#region Public Methods       
 
        /// <summary>
        ///     Dependency Property for Command property
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(InputBinding), new UIPropertyMetadata(null, new PropertyChangedCallback(OnCommandPropertyChanged)));

        /// <summary>
        /// Command Object associated
        /// </summary>
        /// <SecurityNote>
        /// Critical - may associate a secure command with a gesture, in 
        ///            these cases we need to demand the appropriate permission.
        /// PublicOk - Calls CheckSecureCommand which does the appropriate demand.
        /// </SecurityNote>
        [TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [Localizability(LocalizationCategory.NeverLocalize)] // cannot be localized
        public ICommand Command
        {
            get     
            {
                return (ICommand)GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        /// <summary>
        ///     Property changed callback for Command property
        /// </summary>
        /// <SecurityNote>
        /// Critical - may associate a secure command with a gesture, in 
        ///            these cases we need to demand the appropriate permission.
        /// TreatAsSafe - Calls CheckSecureCommand which does the appropriate demand.
        /// </SecurityNote>
        private static void OnCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InputBinding inputBinding = (InputBinding)d;
            inputBinding.CheckSecureCommand((ICommand)e.NewValue, inputBinding.Gesture);
        }

        /// <summary>
        ///     Dependency Property for Command Parameter
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(InputBinding));

        /// <summary>
        ///     A parameter for the command.
        /// </summary>
        public object CommandParameter
        {
            get
            {
                return GetValue(CommandParameterProperty);
            }
            set
            {
                SetValue(CommandParameterProperty, value);
            }
        }

        /// <summary>
        ///     Dependency property for command target
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register("CommandTarget", typeof(IInputElement), typeof(InputBinding));

        /// <summary>
        ///     Where the command should be raised.
        /// </summary>
        public IInputElement CommandTarget
        {
            get
            {
                return (IInputElement)GetValue(CommandTargetProperty);
            }
            set
            {
                SetValue(CommandTargetProperty, value);
            }
        }

        /// <summary>
        /// InputGesture associated with the Command
        /// </summary>
        /// <SecurityNote>
        /// Critical - may associate a secure command with a gesture, in 
        ///            these cases we need to demand the appropriate permission.
        /// PublicOk - Calls CheckSecureCommand which does the appropriate demand.
        /// </SecurityNote>
        public virtual InputGesture Gesture
        {
            // We would like to make this getter non-virtual but that's not legal
            // in C#.  Luckily there is no security issue with leaving it virtual.
            get
            {
                ReadPreamble();
                return _gesture;
            }
            
            set
            {
                WritePreamble();
                if (value == null)
                    throw new ArgumentNullException("value");

                lock (_dataLock)
                {
                    // Check before assignment to avoid continuation
                    //
                    CheckSecureCommand(Command, value);

                    _gesture = value;
                }
                WritePostscript();
            }
        }
#endregion Public Methods
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <SecurityNote>
        /// Critical - determines if a command will later make an
        ///            assert. This is critical to be right, because
        ///            later we assume that the binding was protected.
        /// TreatAsSafe - Demand() is not an unsafe operation
        /// </SecurityNote>
        void CheckSecureCommand(ICommand command, InputGesture gesture)
        {
            // In v3.5, only ApplicationCommands.Paste was treated as ISecureCommand,
            // causing the below demand to fail if a binding for it was created. As
            // there's no provable security vulnerability and for backwards compat
            // reasons, we special-case Cut and Copy not to be subject to this check
            // even though they have been promoted to ISecureCommand in v4.0.
            ISecureCommand secure = command as ISecureCommand;
            if (   secure != null
                && command != ApplicationCommands.Cut
                && command != ApplicationCommands.Copy)
            {
                secure.UserInitiatedPermission.Demand();
            }
        }

        #region Freezable

        /// <summary>
        ///     Freezable override to create the instance (used for cloning).
        /// </summary>
        protected override Freezable CreateInstanceCore()
        {
            return new InputBinding();
        }

        /// <summary>
        ///     Freezable override to clone the non dependency properties
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
            _gesture = ((InputBinding)sourceFreezable).Gesture;
        }

        /// <summary>
        ///     Freezable override to clone the non dependency properties
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            base.CloneCurrentValueCore(sourceFreezable);
            _gesture = ((InputBinding)sourceFreezable).Gesture;
        }

        /// <summary>
        ///     Freezable override of GetAsFrozenCore
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetAsFrozenCore(sourceFreezable);
            _gesture = ((InputBinding)sourceFreezable).Gesture;
        }

        /// <summary>
        ///     Freezable override of GetCurrentValueAsFrozenCore
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            _gesture = ((InputBinding)sourceFreezable).Gesture;
        }

        #endregion

        #region Inheirtance context

        /// <summary>
        ///     Define the DO's inheritance context
        /// </summary>
        internal override DependencyObject InheritanceContext
        {
            get { return _inheritanceContext; }
        }

        /// <summary>
        ///     Receive a new inheritance context
        /// </summary>
        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            InheritanceContextHelper.AddInheritanceContext(context,
                                                           this,
                                                           ref _hasMultipleInheritanceContexts,
                                                           ref _inheritanceContext);
        }

        /// <summary>
        ///     Remove an inheritance context
        /// </summary>
        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            InheritanceContextHelper.RemoveInheritanceContext(context,
                                                              this,
                                                              ref _hasMultipleInheritanceContexts,
                                                              ref _inheritanceContext);
        }

        /// <summary>
        ///     Says if the current instance has multiple InheritanceContexts
        /// </summary>
        internal override bool HasMultipleInheritanceContexts
        {
            get { return _hasMultipleInheritanceContexts; }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

#region Private Fields
        private     InputGesture  _gesture = null ;

        internal static object _dataLock = new object();

        // Fields to implement DO's inheritance context
        private DependencyObject _inheritanceContext = null;
        private bool _hasMultipleInheritanceContexts = false;
#endregion Private Fields
    }
}
