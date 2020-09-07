// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: DefaultvalueFactory for Freezables
//

using MS.Internal.WindowsBase;
using System;
using System.Diagnostics;
using System.Windows;

namespace MS.Internal
{
    // <summary>
    // FreezableDefaultValueFactory is a DefaultValueFactory implementation which 
    // is inserted by the property system for any DP registered with a default 
    // value of type Freezable. The user’s given default value is frozen and 
    // used as a template to create unfrozen copies on a per DP per DO basis. If 
    // the default value is modified it is automatically promoted from default to 
    // local.
    // </summary>
    [FriendAccessAllowed] // built into Base, used by Core + Framework
    internal class FreezableDefaultValueFactory : DefaultValueFactory
    {
        /// <summary>
        ///     Stores a frozen copy of defaultValue
        /// </summary>
        internal FreezableDefaultValueFactory(Freezable defaultValue)
        {
            Debug.Assert(defaultValue != null,
                "Null can not be made mutable.  Do not use FreezableDefaultValueFactory.");
            Debug.Assert(defaultValue.CanFreeze,
                "The defaultValue prototype must be freezable.");
            
            _defaultValuePrototype = defaultValue.GetAsFrozen();
        }

        /// <summary>
        ///     Returns our frozen sentinel
        /// </summary>        
        internal override object DefaultValue
        {
            get
            {
                Debug.Assert(_defaultValuePrototype.IsFrozen);

                return _defaultValuePrototype;
            }
        }

        /// <summary>
        ///     If the DO is frozen, we'll return our frozen sentinel. Otherwise we'll make
        ///     an unfrozen copy.
        /// </summary>
        internal override object CreateDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            Debug.Assert(owner != null && property != null,
                "It is the caller responsibility to ensure that owner and property are non-null.");
            
            Freezable result = _defaultValuePrototype;
            Freezable ownerFreezable = owner as Freezable;
            
            // If the owner is frozen, just return the frozen prototype.
            if (ownerFreezable != null && ownerFreezable.IsFrozen)
            {
                return result;
            }
            
            result = _defaultValuePrototype.Clone();

            // Wire up a FreezableDefaultPromoter to observe the default value we
            // just created and automatically promote it to local if it is modified.
            FreezableDefaultPromoter promoter = new FreezableDefaultPromoter(owner, property);
            promoter.SetFreezableDefaultValue(result);
            result.Changed += promoter.OnDefaultValueChanged;
                        
            return result;
        }

        // This is the prototype that CreateDefaultValue copies to create the
        // mutable default value for this property.  See also the ctor.
        private readonly Freezable _defaultValuePrototype;

        /// <summary>
        ///     The FreezableDefaultPromoter observes the mutable defaults we hand out
        ///     for changed events.  If the default is ever modified this class will
        ///     promote it to a local value by writing it to the local store and
        ///     clear the cached default value so we will generate a new default
        ///     the next time the property system is asked for one.
        /// </summary>
        private class FreezableDefaultPromoter
        {
            internal FreezableDefaultPromoter(DependencyObject owner, DependencyProperty property)
            {
                Debug.Assert(owner != null && property != null,
                    "Caller is responsible for ensuring that owner and property are non-null.");
                Debug.Assert(!(owner is Freezable) || !((Freezable)owner).IsFrozen,
                    "We should not be observing mutables on a frozen owner.");
                Debug.Assert(property.GetMetadata(owner.DependencyObjectType).UsingDefaultValueFactory,
                    "How did we end up observing a mutable if we were not registered for the factory pattern?");

                // We hang on to the property and owner so we can write the default
                // value back to the local store if it changes.  See also
                // OnDefaultValueChanged.
                _owner = owner;
                _property = property;
            }

            internal void OnDefaultValueChanged(object sender, EventArgs e)
            {
                Debug.Assert(_mutableDefaultValue != null,
                    "Promoter's creator should have called SetFreezableDefaultValue.");

                PropertyMetadata metadata = _property.GetMetadata(_owner.DependencyObjectType);

                // Remove this value from the DefaultValue cache so we stop
                // handing it out as the default value now that it has changed.
                metadata.ClearCachedDefaultValue(_owner, _property);

                // Since Changed is raised when the user freezes the default
                // value, we need to check before removing our handler.
                // (If the value is frozen, it will remove it's own handlers.)
                if (!_mutableDefaultValue.IsFrozen)
                {
                    _mutableDefaultValue.Changed -= OnDefaultValueChanged;
                }

                // If someone else hasn't already written a local local value,
                // promote the default value to local.
                if (_owner.ReadLocalValue(_property) == DependencyProperty.UnsetValue)
                {
                    _owner.SetMutableDefaultValue(_property, _mutableDefaultValue);
                }
            }

            private readonly DependencyObject _owner;
            private readonly DependencyProperty _property;

            #region DefaultValue

            // The creator of a FreezableDefaultValuePromoter should call this method
            // so that we can verify that the changed sender is the mutable default
            // value we handed out.
            internal void SetFreezableDefaultValue(Freezable mutableDefaultValue)
            {
                _mutableDefaultValue = mutableDefaultValue;
            }


            private Freezable _mutableDefaultValue;

            #endregion DefaultValue
        }
    }
}
