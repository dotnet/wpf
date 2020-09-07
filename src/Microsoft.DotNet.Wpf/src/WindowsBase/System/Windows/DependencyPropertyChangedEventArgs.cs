// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MS.Internal.WindowsBase;  // FriendAccessAllowed

namespace System.Windows
{
    /// <summary>
    ///     Provides data for the various property changed events.
    /// </summary>
    public struct DependencyPropertyChangedEventArgs
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the DependencyPropertyChangedEventArgs class.
        /// </summary>
        /// <param name="property">
        ///     The property whose value changed.
        /// </param>
        /// <param name="oldValue">
        ///     The value of the property before the change.
        /// </param>
        /// <param name="newValue">
        ///     The value of the property after the change.
        /// </param>
        public DependencyPropertyChangedEventArgs(DependencyProperty property, object oldValue, object newValue)
        {
            _property = property;
            _metadata = null;
            _oldEntry = new EffectiveValueEntry(property);
            _newEntry = _oldEntry;
            _oldEntry.Value = oldValue;
            _newEntry.Value = newValue;

            _flags = 0;
            _operationType = OperationType.Unknown;
            IsAValueChange        = true;
        }

        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal DependencyPropertyChangedEventArgs(DependencyProperty property, PropertyMetadata metadata, object oldValue, object newValue)
        {
            _property = property;
            _metadata = metadata;
            _oldEntry = new EffectiveValueEntry(property);
            _newEntry = _oldEntry;
            _oldEntry.Value = oldValue;
            _newEntry.Value = newValue;

            _flags = 0;
            _operationType = OperationType.Unknown;
            IsAValueChange        = true;
        }

        internal DependencyPropertyChangedEventArgs(DependencyProperty property, PropertyMetadata metadata, object value)
        {
            _property = property;
            _metadata = metadata;
            _oldEntry = new EffectiveValueEntry(property);
            _oldEntry.Value = value;
            _newEntry = _oldEntry;

            _flags = 0;
            _operationType = OperationType.Unknown;
            IsASubPropertyChange = true;
        }

        internal DependencyPropertyChangedEventArgs(
            DependencyProperty  property,
            PropertyMetadata    metadata,
            bool                isAValueChange,
            EffectiveValueEntry oldEntry,
            EffectiveValueEntry newEntry,
            OperationType       operationType)
        {
            _property             = property;
            _metadata             = metadata;
            _oldEntry             = oldEntry;
            _newEntry             = newEntry;

            _flags = 0;
            _operationType        = operationType;
            IsAValueChange        = isAValueChange;

            // This is when a mutable default is promoted to a local value. On this operation mutable default 
            // value acquires a freezable context. However this value promotion operation is triggered 
            // whenever there has been a sub property change to the mutable default. Eg. Adding a TextEffect 
            // to a TextEffectCollection instance which is the mutable default. Since we missed the sub property 
            // change due to this add, we flip the IsASubPropertyChange bit on the following change caused by 
            // the value promotion to coalesce these operations. 
            IsASubPropertyChange = (operationType == OperationType.ChangeMutableDefaultValue);
        }

        #endregion Constructors


        #region Properties

        /// <summary>
        ///     The property whose value changed.
        /// </summary>
        public DependencyProperty Property
        {
            get { return _property; }
        }

        /// <summary>
        ///     Whether or not this change indicates a change to the property value
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool IsAValueChange
        {
            get { return ReadPrivateFlag(PrivateFlags.IsAValueChange); }
            set { WritePrivateFlag(PrivateFlags.IsAValueChange, value); }
        }
               
        /// <summary>
        ///     Whether or not this change indicates a change to the subproperty
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool IsASubPropertyChange
        {
            get { return ReadPrivateFlag(PrivateFlags.IsASubPropertyChange); }
            set { WritePrivateFlag(PrivateFlags.IsASubPropertyChange, value); }
        }

        /// <summary>
        ///     Metadata for the property
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal PropertyMetadata Metadata
        {
            get { return _metadata; }
        }

        /// <summary>
        ///     Says what operation caused this property change
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal OperationType OperationType
        {
            get { return _operationType; }
        }


        /// <summary>
        ///     The old value of the property.
        /// </summary>
        public object OldValue
        {
            get 
            {
                EffectiveValueEntry oldEntry = OldEntry.GetFlattenedEntry(RequestFlags.FullyResolved);
                if (oldEntry.IsDeferredReference)
                {
                    // The value for this property was meant to come from a dictionary
                    // and the creation of that value had been deferred until this
                    // time for better performance. Now is the time to actually instantiate
                    // this value by querying it from the dictionary. Once we have the
                    // value we can actually replace the deferred reference marker
                    // with the actual value.
                    oldEntry.Value = ((DeferredReference) oldEntry.Value).GetValue(oldEntry.BaseValueSourceInternal);
                }

                return oldEntry.Value; 
            }
        }

        /// <summary>
        ///     The entry for the old value (contains value and all modifier info)
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal EffectiveValueEntry OldEntry
        {
            get { return _oldEntry; }
        }

        /// <summary>
        ///     The source of the old value
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal BaseValueSourceInternal OldValueSource
        {
            get { return _oldEntry.BaseValueSourceInternal; }
        }
               
        /// <summary>
        ///     Says if the old value was a modified value (coerced, animated, expression)
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool IsOldValueModified
        {
            get { return _oldEntry.HasModifiers; }
        }
               
        /// <summary>
        ///     Says if the old value was a deferred value
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool IsOldValueDeferred
        {
            get { return _oldEntry.IsDeferredReference; }
        }
                
        /// <summary>
        ///     The new value of the property.
        /// </summary>
        public object NewValue
        {
            get 
            {
                EffectiveValueEntry newEntry = NewEntry.GetFlattenedEntry(RequestFlags.FullyResolved);
                if (newEntry.IsDeferredReference)
                {
                    // The value for this property was meant to come from a dictionary 
                    // and the creation of that value had been deferred until this 
                    // time for better performance. Now is the time to actually instantiate 
                    // this value by querying it from the dictionary. Once we have the 
                    // value we can actually replace the deferred reference marker 
                    // with the actual value.
                    newEntry.Value = ((DeferredReference) newEntry.Value).GetValue(newEntry.BaseValueSourceInternal);
                }

                return newEntry.Value; 
            }
        }

        /// <summary>
        ///     The entry for the new value (contains value and all modifier info)
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal EffectiveValueEntry NewEntry
        {
            get { return _newEntry; }
        }
        
        /// <summary>
        ///     The source of the new value
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal BaseValueSourceInternal NewValueSource
        {
            get { return _newEntry.BaseValueSourceInternal; }
        }
               
        /// <summary>
        ///     Says if the new value was a modified value (coerced, animated, expression)
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool IsNewValueModified
        {
            get { return _newEntry.HasModifiers; }
        }

        /// <summary>
        ///     Says if the new value was a deferred value
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool IsNewValueDeferred
        {
            get { return _newEntry.IsDeferredReference; }
        }
                
        #endregion Properties

        /// <summary>
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals((DependencyPropertyChangedEventArgs)obj);
        }

        /// <summary>
        /// </summary>
        public bool Equals(DependencyPropertyChangedEventArgs args)
        {
            return (_property == args._property &&
                    _metadata == args._metadata &&
                    _oldEntry.Value == args._oldEntry.Value &&
                    _newEntry.Value == args._newEntry.Value &&
                    _flags == args._flags &&
                    _oldEntry.BaseValueSourceInternal == args._oldEntry.BaseValueSourceInternal &&
                    _newEntry.BaseValueSourceInternal == args._newEntry.BaseValueSourceInternal &&
                    _oldEntry.HasModifiers == args._oldEntry.HasModifiers &&
                    _newEntry.HasModifiers == args._newEntry.HasModifiers &&
                    _oldEntry.IsDeferredReference == args._oldEntry.IsDeferredReference &&
                    _newEntry.IsDeferredReference == args._newEntry.IsDeferredReference &&
                    _operationType == args._operationType);
        }

        /// <summary>
        /// </summary>
        public static bool operator ==(DependencyPropertyChangedEventArgs left, DependencyPropertyChangedEventArgs right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// </summary>
        public static bool operator !=(DependencyPropertyChangedEventArgs left, DependencyPropertyChangedEventArgs right)
        {
            return !left.Equals(right);
        }

        #region PrivateMethods

        private void WritePrivateFlag(PrivateFlags bit, bool value)
        {
            if (value)
            {
                _flags |= bit;
            }
            else
            {
                _flags &= ~bit;
            }
        }

        private bool ReadPrivateFlag(PrivateFlags bit)
        {
            return (_flags & bit) != 0;
        }

        #endregion PrivateMethods

        #region PrivateDataStructures

        private enum PrivateFlags : byte
        {
            IsAValueChange        = 0x01,
            IsASubPropertyChange  = 0x02,
        }

        #endregion PrivateDataStructures

        #region Data

        private DependencyProperty  _property;
        private PropertyMetadata    _metadata;

        private PrivateFlags        _flags;

        private EffectiveValueEntry _oldEntry;
        private EffectiveValueEntry _newEntry;
        
        private OperationType       _operationType;

        #endregion Data
    }

    [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
    internal enum OperationType : byte
    {
        Unknown                     = 0,
        AddChild                    = 1,
        RemoveChild                 = 2,
        Inherit                     = 3,
        ChangeMutableDefaultValue   = 4,
    }
}
