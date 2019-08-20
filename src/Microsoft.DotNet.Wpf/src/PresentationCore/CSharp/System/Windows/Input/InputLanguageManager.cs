// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: InputLanguageManager class and InputLanguage APIs.
//
//

using System.Collections;
using System.Windows.Threading;
using System.Windows;
using System.Globalization;
using MS.Win32;
using System;
using System.Security;
using System.Runtime.InteropServices;
using System.ComponentModel;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The InputLanguageManager class is responsible for mmanaging 
    ///     the input language in Avalon.
    /// </summary>
    public sealed class InputLanguageManager : DispatcherObject
    {
        /// <summary>
        /// This is the property of the preferred input language of the 
        /// element. 
        /// When the element get focus, this input language is selected 
        /// automatically.
        /// </summary>
        public static readonly DependencyProperty InputLanguageProperty =
                    DependencyProperty.RegisterAttached(
                        "InputLanguage",
                        typeof(CultureInfo),
                        typeof(InputLanguageManager),
                        new PropertyMetadata(CultureInfo.InvariantCulture)
                        );

        /// <summary>
        /// Setter for InputLanguage DependencyProperty
        /// </summary>
        public static void SetInputLanguage(DependencyObject target, CultureInfo inputLanguage)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(InputLanguageProperty, inputLanguage);
        }

        /// <summary>
        /// Getter for InputLanguage DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        [TypeConverter(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public static CultureInfo GetInputLanguage(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (CultureInfo)(target.GetValue(InputLanguageProperty));
        }


        /// <summary>
        /// If this is true, the previous inputlanguage is restored 
        /// when the element with InputLanguage dynamicproperty lose the focus.
        /// This property is ignored if InputLanguage dynamic property is
        ///  not available in the element.
        /// </summary>
        public static readonly DependencyProperty RestoreInputLanguageProperty =
                    DependencyProperty.RegisterAttached(
                        "RestoreInputLanguage",
                        typeof(bool),
                        typeof(InputLanguageManager),
                        new PropertyMetadata(false)
                        );

        /// <summary>
        /// Setter for RestoreInputLanguage DependencyProperty
        /// </summary>
        public static void SetRestoreInputLanguage(DependencyObject target, bool restore)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.SetValue(RestoreInputLanguageProperty, restore);
        }

        /// <summary>
        /// Getter for RestoreInputLanguage DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetRestoreInputLanguage(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return (bool)(target.GetValue(RestoreInputLanguageProperty));
        }


        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        private InputLanguageManager()
        {
            // register our default input language source.
            RegisterInputLanguageSource(new InputLanguageSource(this));
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        ///     Registers an input language soruces with the input 
        ///     input language manager.
        /// </summary>
        /// <param name="inputLanguageSource">
        ///     The input language source to register.
        /// </param>
        public void RegisterInputLanguageSource(IInputLanguageSource inputLanguageSource)
        {
            if (inputLanguageSource == null)
            {
                throw new ArgumentNullException("inputLanguageSource");
            }
            
            _source = inputLanguageSource;

            if (((_InputLanguageChanged != null) || 
                 (_InputLanguageChanging != null)) &&
                IsMultipleKeyboardLayout)
                _source.Initialize();

            return;
        }

        /// <summary>
        ///     Report the input language is changed from the source.
        /// </summary>
        /// <param name="newLanguageId">
        ///     The new language id.
        /// </param>
        /// <param name="previousLanguageId">
        ///     The previous language id.
        /// </param>
        public void ReportInputLanguageChanged(
                        CultureInfo newLanguageId, 
                        CultureInfo previousLanguageId)
        {
            if (newLanguageId == null)
            {
                throw new ArgumentNullException("newLanguageId");
            }

            if (previousLanguageId == null)
            {
                throw new ArgumentNullException("previousLanguageId");
            }

            //
            // if this language change was not done by SetFocus() and
            // InputLanguage Property of the element, we clear _previousLanguageId.
            //
            if (!previousLanguageId.Equals(_previousLanguageId))
            {
                _previousLanguageId = null;
            }

            if (_InputLanguageChanged != null)
            {
                InputLanguageChangedEventArgs args = new InputLanguageChangedEventArgs(newLanguageId, previousLanguageId);

                // Stability Review: Task#32416
                //   - No state to be restored even exception happens while this callback.
                _InputLanguageChanged(this, args);
            }
        }

        /// <summary>
        ///     Report the input language is being changed from the source.
        /// </summary>
        /// <param name="newLanguageId">
        ///     The new language id.
        /// </param>
        /// <param name="previousLanguageId">
        ///     The previous language id.
        /// </param>
        public bool ReportInputLanguageChanging(
                        CultureInfo newLanguageId, 
                        CultureInfo previousLanguageId)
        {
            if (newLanguageId == null)
            {
                throw new ArgumentNullException("newLanguageId");
            }

            if (previousLanguageId == null)
            {
                throw new ArgumentNullException("previousLanguageId");
            }

            bool accepted = true;

            if (_InputLanguageChanging != null)
            {
                InputLanguageChangingEventArgs args = new InputLanguageChangingEventArgs(newLanguageId, previousLanguageId);

                // Stability Review: Task#32416
                //   - No state to be restored even exception happens while this callback.
                _InputLanguageChanging(this, args);

                accepted = args.Rejected ? false : true;
            }
            return accepted;
        }
 
        #endregion Public Methods        
 
        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------
 
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        /// <summary>
        ///     Return the input language manager associated 
        ///     with the current context.
        /// </summary>
        public static InputLanguageManager Current
        {
            get
            {
                // InputLanguageManager for the current Dispatcher is stored in InputMethod of
                // the current Dispatcher.
                if(InputMethod.Current.InputLanguageManager == null)
                {
                    InputMethod.Current.InputLanguageManager = new InputLanguageManager();
                }
                return InputMethod.Current.InputLanguageManager;
            }
        }

        /// <summary>
        ///     This accesses the input language associated 
        ///     with the current context.
        /// </summary>
        [TypeConverter(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public CultureInfo CurrentInputLanguage
        {
            get
            {
                // If the source is available, we should use it.
                if (_source != null)
                {
                    return _source.CurrentInputLanguage;
                }

                IntPtr hkl = SafeNativeMethods.GetKeyboardLayout(0);
                if (hkl == IntPtr.Zero)
                {
                    return CultureInfo.InvariantCulture;
                }

                // This needs to work before source is attached.
                return new CultureInfo((short)NativeMethods.IntPtrToInt32(hkl));
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                SetSourceCurrentLanguageId(value);
            }
        }

        /// <summary>
        /// Return enumerator for available input languages.
        /// </summary>
        public IEnumerable AvailableInputLanguages 
        {
            get
            {
                if (_source == null)
                {
                    return null;
                }

                return (IEnumerable)_source.InputLanguageList;
            }
        }

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        ///     This is an event when the input language is changed.
        /// </summary>
        public event InputLanguageEventHandler InputLanguageChanged
        {
            add
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if ((_InputLanguageChanged == null) && 
                    (_InputLanguageChanging == null) &&
                    IsMultipleKeyboardLayout &&
                    (_source != null))
                    _source.Initialize();

                _InputLanguageChanged += value;
            }
            remove
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _InputLanguageChanged -= value;
                if ((_InputLanguageChanged == null) && 
                    (_InputLanguageChanging == null) &&
                    IsMultipleKeyboardLayout &&
                    (_source != null))
                    _source.Uninitialize();
            }
        }

        /// <summary>
        ///     This is an event when the input language is being changed.
        /// </summary>
        public event InputLanguageEventHandler InputLanguageChanging
        {
            add
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if ((_InputLanguageChanged == null) && 
                    (_InputLanguageChanging == null) &&
                    IsMultipleKeyboardLayout &&
                    (_source != null))
                    _source.Initialize();

                _InputLanguageChanging += value;
            }
            remove
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _InputLanguageChanging -= value;
                if ((_InputLanguageChanged == null) && 
                    (_InputLanguageChanging == null) &&
                    IsMultipleKeyboardLayout &&
                    (_source != null))
                    _source.Uninitialize();
            }
        }


        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        /// <summary>
        ///     When keyboard device gets focus, this is called.
        ///     We will check the preferred input language of focus element.
        /// </summary>
        internal void Focus(DependencyObject focus, DependencyObject focused)
        {
            CultureInfo culture = null;

            if (focus != null)
            {
                //
                // Check the InputLanguageProperty of the focus element.
                //
                culture = (CultureInfo)focus.GetValue(InputLanguageProperty);
            }
            
            if ((culture == null) ||
                (culture.Equals(CultureInfo.InvariantCulture)))
            {
                //
                // If the focus element does not have InputLanguage property,
                // we may need to restore the previous input language.
                //
                if (focused != null)
                {
                    if ((_previousLanguageId != null) &&
                        (bool)focused.GetValue(RestoreInputLanguageProperty))
                    {
                        //
                        // set the current language id of source.
                        //
                        SetSourceCurrentLanguageId(_previousLanguageId);
                    }
                    _previousLanguageId = null;
                }
            }
            else
            {
                // Cache the previous language id.
                // _previousLanguageId can be clear during SetSourceCurrentLanguageId() because the call back
                // ReportInputLanguageChanged() is called. We need to remember the current input language
                // to update _previousLanguageId.
                CultureInfo previousLanguageId = _source.CurrentInputLanguage;

                //
                // set the current language id of source.
                //
                SetSourceCurrentLanguageId(culture);

                _previousLanguageId = previousLanguageId;
            }
        }

 
        #endregion Internal methods
        
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        
        #region Internal Properties

        internal IInputLanguageSource Source
        {
            get
            {
                return _source;
            }
        }

        /// <summary>
        ///     This checks if there is two or more keyboard layouts.
        /// </summary>
        static internal bool IsMultipleKeyboardLayout
        {
            get
            {
                int count = SafeNativeMethods.GetKeyboardLayoutList(0, null);

                return (count > 1);
            }
        }

        #endregion Internal Properties
 
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        private void SetSourceCurrentLanguageId(CultureInfo languageId)
        {
            if (_source == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.InputLanguageManager_NotReadyToChangeCurrentLanguage));
            }

            _source.CurrentInputLanguage = languageId;
        }

        //------------------------------------------------------
        //
        //  Private Events
        //
        //------------------------------------------------------

        /// <summary>
        ///     This is an event when the input language is changed.
        /// </summary>
        private event InputLanguageEventHandler _InputLanguageChanged;

        /// <summary>
        ///     This is an event when the input language is being changed.
        /// </summary>
        private event InputLanguageEventHandler _InputLanguageChanging;

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        // the previous input language id this is used for restring input language at
        // losing focus.
        private CultureInfo _previousLanguageId;

        // The reference to the source of input language.
        private IInputLanguageSource _source;

        #endregion Private Fields
    }

    /// <summary>
    ///     This is a delegate for InputLanguageChanged and 
    ///     InputLanguageChanging events.
    /// </summary>
    public delegate void InputLanguageEventHandler(Object sender, InputLanguageEventArgs e);
}
