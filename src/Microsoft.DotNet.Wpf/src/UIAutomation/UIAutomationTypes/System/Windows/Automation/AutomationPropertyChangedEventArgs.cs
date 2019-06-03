// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: PropertyChanged event args class

using System;
using System.Windows.Automation;

namespace System.Windows.Automation 
{
    /// <summary>
    /// Delegate to handle Automation Property change events
    /// </summary>
#if (INTERNAL_COMPILE)
    internal delegate void AutomationPropertyChangedEventHandler( object sender, AutomationPropertyChangedEventArgs e );
#else
    public delegate void AutomationPropertyChangedEventHandler( object sender, AutomationPropertyChangedEventArgs e );
#endif

    /// <summary>
    /// PropertyChanged event args class
    /// </summary>
#if (INTERNAL_COMPILE)
    internal sealed class AutomationPropertyChangedEventArgs : AutomationEventArgs
#else
    public sealed class AutomationPropertyChangedEventArgs : AutomationEventArgs
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Constructor for PropertyChanged event args.
        /// </summary>
        public AutomationPropertyChangedEventArgs(AutomationProperty property, object oldValue, object newValue)
            : base(AutomationElementIdentifiers.AutomationPropertyChangedEvent)
        {
            _oldValue = oldValue;
            _newValue = newValue;
            _property = property;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Guid indicating which property changed.
        /// </summary>
        public AutomationProperty Property
        { 
            get
            {
                return _property; 
            }
        }

        /// <summary>
        /// Old value of the property that changed
        /// </summary>
        public object OldValue 
        { 
            get
            {
                return _oldValue;
            } 
        }

        /// <summary>
        /// New value of the property that changed
        /// </summary>
        public object NewValue 
        { 
            get 
            {
                return _newValue;
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationProperty _property;
        private object _oldValue;
        private object _newValue;

        #endregion Private Fields
    }
}
