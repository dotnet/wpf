// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: DefaultvalueFactory for ObservableCollection
//

using MS.Internal.WindowsBase;
using System;
using System.Diagnostics;
using System.Windows;
using System.Collections.ObjectModel; // ObservableCollection
using System.Collections.Specialized; // NotifyCollectionChangedEventHandler

namespace MS.Internal
{
    // <summary>
    // ObservableCollectionDefaultValueFactory is a DefaultValueFactory implementation which will
    // promote the default value to a local value if the collection is modified.
    // </summary>
    [FriendAccessAllowed] // built into Base, used by Framework
    internal class ObservableCollectionDefaultValueFactory<T> : DefaultValueFactory
    {
        internal ObservableCollectionDefaultValueFactory()
        {
            _default = new ObservableCollection<T>();
        }

        /// <summary>
        ///     This is used for Sealed objects.  ObservableCollections are inherently mutable, so they shouldn't
        ///     be used with sealed objects.  The PropertyDescriptor calls this, so we'll just return the same empty collection.
        /// </summary>        
        internal override object DefaultValue
        {
            get
            {
                return _default;
            }
        }

        /// <summary>
        /// </summary>
        internal override object CreateDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            Debug.Assert(owner != null && property != null,
                "It is the caller responsibility to ensure that owner and property are non-null.");
            
            var result = new ObservableCollection<T>();
            
            // Wire up a ObservableCollectionDefaultPromoter to observe the default value we
            // just created and automatically promote it to local if it is modified.
            // NOTE: We do not holding a reference to this because it should have the same lifetime as
            // the collection.  It will not be immediately GC'ed because it hooks the collections change event.
            new ObservableCollectionDefaultPromoter(owner, property, result);
                        
            return result;
        }

        /// <summary>
        ///     The ObservableCollectionDefaultPromoter observes the mutable defaults we hand out
        ///     for changed events.  If the default is ever modified this class will
        ///     promote it to a local value by writing it to the local store and
        ///     clear the cached default value so we will generate a new default
        ///     the next time the property system is asked for one.
        /// </summary>
        private class ObservableCollectionDefaultPromoter
        {
            internal ObservableCollectionDefaultPromoter(DependencyObject owner, DependencyProperty property, ObservableCollection<T> collection)
            {
                Debug.Assert(owner != null && property != null,
                    "Caller is responsible for ensuring that owner and property are non-null.");
                Debug.Assert(property.GetMetadata(owner.DependencyObjectType).UsingDefaultValueFactory,
                    "How did we end up observing a mutable if we were not registered for the factory pattern?");

                // We hang on to the property and owner so we can write the default
                // value back to the local store if it changes.  See also
                // OnDefaultValueChanged.
                _owner = owner;
                _property = property;
                _collection = collection;
                _collection.CollectionChanged += OnDefaultValueChanged;
            }

            internal void OnDefaultValueChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                PropertyMetadata metadata = _property.GetMetadata(_owner.DependencyObjectType);

                // Remove this value from the DefaultValue cache so we stop
                // handing it out as the default value now that it has changed.
                metadata.ClearCachedDefaultValue(_owner, _property);

                // If someone else hasn't already written a local value,
                // promote the default value to local.
                if (_owner.ReadLocalValue(_property) == DependencyProperty.UnsetValue)
                {
                    // Read-only properties must be set using the Key
                    if (_property.ReadOnly)
                    {
                        _owner.SetValue(_property.DependencyPropertyKey, _collection);
                    }
                    else
                    {
                        _owner.SetValue(_property, _collection);
                    }
                }
                
                // Unhook the change handler because we're finsihed promoting
                _collection.CollectionChanged -= OnDefaultValueChanged;
            }

            private readonly DependencyObject _owner;
            private readonly DependencyProperty _property;
            private readonly ObservableCollection<T> _collection;
        }


        private ObservableCollection<T> _default;
    }
}
