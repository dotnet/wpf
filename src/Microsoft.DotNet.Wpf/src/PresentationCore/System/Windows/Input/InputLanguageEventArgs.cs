// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Threading;
using System.Windows;
using System.Globalization;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The InputLanguageEventArgs class represents a type of
    ///     RoutedEventArgs that are relevant to events raised to indicate
    ///     changes.
    /// </summary>
    public abstract class InputLanguageEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        /// <summary>
        ///     Constructs an instance of the InputLanguageEventArgs class.
        /// </summary>
        /// <param name="newLanguageId">
        ///     The new language id.
        /// </param>
        /// <param name="previousLanguageId">
        ///     The previous language id.
        /// </param>
        protected InputLanguageEventArgs(CultureInfo newLanguageId, CultureInfo previousLanguageId)
        {
            _newLanguageId = newLanguageId;
            _previousLanguageId = previousLanguageId;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        /// <summary>
        ///     New Language Id.
        /// </summary>
        public virtual CultureInfo NewLanguage
        {
            get { return _newLanguageId; }
        }

        /// <summary>
        ///     Previous Language Id.
        /// </summary>
        public virtual CultureInfo PreviousLanguage
        {
            get { return _previousLanguageId; }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields
        // the new input language.
        private CultureInfo _newLanguageId;
        // the previous input language.
        private CultureInfo _previousLanguageId;
        #endregion Private Fields
    }

    /// <summary>
    ///     The InputLanguageEventArgs class represents a type of
    ///     RoutedEventArgs that are relevant to events raised to indicate
    ///     changes.
    /// </summary>
    public class InputLanguageChangedEventArgs : InputLanguageEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        /// <summary>
        ///     Constructs an instance of the InputLanguageEventArgs class.
        /// </summary>
        /// <param name="newLanguageId">
        ///     The new language id.
        /// </param>
        /// <param name="previousLanguageId">
        ///     The new language id.
        /// </param>
        public InputLanguageChangedEventArgs(CultureInfo newLanguageId, CultureInfo previousLanguageId) : base(newLanguageId, previousLanguageId)
        {
        }
    }

    /// <summary>
    ///     The InputLanguageEventArgs class represents a type of
    ///     RoutedEventArgs that are relevant to events raised to indicate
    ///     changes.
    /// </summary>
    /// <ExternalAPI/>
    public class InputLanguageChangingEventArgs : InputLanguageEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        ///     Constructs an instance of the InputLanguageEventArgs class.
        /// </summary>
        /// <param name="newLanguageId">
        ///     The new language id.
        /// </param>
        /// <param name="previousLanguageId">
        ///     The previous language id. 
        /// </param>
        public InputLanguageChangingEventArgs(CultureInfo newLanguageId, CultureInfo previousLanguageId) : base(newLanguageId, previousLanguageId)
        {
            _rejected = false;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        /// <summary>
        ///     This is a value to reject the input language change.
        /// </summary>
        public bool Rejected
        {
            get { return _rejected; }
            set { _rejected = value; }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields
        // bool to reject the input language change.
        private bool _rejected;
        #endregion Private Fields
    }
}

