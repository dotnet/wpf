// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MS.Internal.KnownBoxes;
using MS.Internal.WindowsBase;  // for FriendAccessAllowed

namespace System.Windows
{
    /// <summary>
    ///
    /// </summary>
    [FriendAccessAllowed] // Built into Base, used by Core and Framework
    internal class UncommonField<T>
    {
        /// <summary>
        ///     Create a new UncommonField.
        /// </summary>
        public UncommonField() : this(default(T))
        {
        }

        /// <summary>
        ///     Create a new UncommonField.
        /// </summary>
        /// <param name="defaultValue">The default value of the field.</param>
        public UncommonField(T defaultValue)
        {
            _defaultValue = defaultValue;
            _hasBeenSet = false;

            lock (DependencyProperty.Synchronized)
            {
                _globalIndex = DependencyProperty.GetUniqueGlobalIndex(null, null);

                DependencyProperty.RegisteredPropertyList.Add();
            }
        }

        /// <summary>
        ///     Write the given value onto a DependencyObject instance.
        /// </summary>
        /// <param name="instance">The DependencyObject on which to set the value.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(DependencyObject instance, T value)
        {
            if (instance != null)
            {
                EntryIndex entryIndex = instance.LookupEntry(_globalIndex);

                // Set the value if it's not the default, otherwise remove the value.
                if (!object.ReferenceEquals(value, _defaultValue))
                {
                    object valueObject;

                    if (typeof(T) == typeof(bool))
                    {
                        // Use shared boxed instances rather than creating new objects for each SetValue call.
                        valueObject = BooleanBoxes.Box(Unsafe.As<T, bool>(ref value));
                    }
                    else
                    {
                        valueObject = value;
                    }

                    instance.SetEffectiveValue(entryIndex, dp: null, _globalIndex, metadata: null, valueObject, BaseValueSourceInternal.Local);
                    _hasBeenSet = true;
                }
                else
                {
                    instance.UnsetEffectiveValue(entryIndex, dp: null, metadata: null);
                }
            }
            else
            {
                throw new ArgumentNullException("instance");
            }
        }

        /// <summary>
        ///     Read the value of this field on a DependencyObject instance.
        /// </summary>
        /// <param name="instance">The DependencyObject from which to get the value.</param>
        /// <returns></returns>
        public T GetValue(DependencyObject instance)
        {
            if (instance != null)
            {
                if (_hasBeenSet)
                {
                    EntryIndex entryIndex = instance.LookupEntry(_globalIndex);

                    if (entryIndex.Found)
                    {
                        object value = instance.EffectiveValues[entryIndex.Index].LocalValue;

                        if (value != DependencyProperty.UnsetValue)
                        {
                            return (T)value;
                        }
                    }
                    return _defaultValue;
                }
                else
                {
                    return _defaultValue;
                }
            }
            else
            {
                throw new ArgumentNullException("instance");
            }
        }


        /// <summary>
        ///     Clear this field from the given DependencyObject instance.
        /// </summary>
        /// <param name="instance"></param>
        public void ClearValue(DependencyObject instance)
        {
            if (instance != null)
            {
                EntryIndex entryIndex = instance.LookupEntry(_globalIndex);

                instance.UnsetEffectiveValue(entryIndex, null /* dp */, null /* metadata */);
            }
            else
            {
                throw new ArgumentNullException("instance");
            }
        }

        internal int GlobalIndex
        {
            get
            {
                return _globalIndex;
            }
        }

        #region Private Fields

        private T _defaultValue;
        private int _globalIndex;
        private bool _hasBeenSet;

        #endregion
    }
}
