﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using MS.Internal.KnownBoxes;

namespace System.Windows
{
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
            ArgumentNullException.ThrowIfNull(instance);

            EntryIndex entryIndex = instance.LookupEntry(_globalIndex);

            // Special case boolean operations to avoid boxing with (mostly) UIA code paths
            if (typeof(T) == typeof(bool))
            {
                // Use shared boxed instances rather than creating new objects for each SetValue call.
                object valueObject = BooleanBoxes.Box(Unsafe.BitCast<T, bool>(value));

                instance.SetEffectiveValue(entryIndex, dp: null, _globalIndex, metadata: null, valueObject, BaseValueSourceInternal.Local);
                _hasBeenSet = true;
            }
            // Set the value if it's not the default, otherwise remove the value.
            else if (!ReferenceEquals(value, _defaultValue))
            {
                instance.SetEffectiveValue(entryIndex, dp: null, _globalIndex, metadata: null, value, BaseValueSourceInternal.Local);
                _hasBeenSet = true;
            }
            else
            {
                instance.UnsetEffectiveValue(entryIndex, dp: null, metadata: null);
            }
        }

        /// <summary>
        ///     Read the value of this field on a DependencyObject instance.
        /// </summary>
        /// <param name="instance">The DependencyObject from which to get the value.</param>
        /// <returns></returns>
        public T GetValue(DependencyObject instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

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


        /// <summary>
        ///     Clear this field from the given DependencyObject instance.
        /// </summary>
        /// <param name="instance"></param>
        public void ClearValue(DependencyObject instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            EntryIndex entryIndex = instance.LookupEntry(_globalIndex);

            instance.UnsetEffectiveValue(entryIndex, dp: null, metadata: null);
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
