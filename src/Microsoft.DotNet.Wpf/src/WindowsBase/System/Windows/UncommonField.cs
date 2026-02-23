// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using MS.Internal.KnownBoxes;

namespace System.Windows
{
    internal class UncommonField<T>
    {
        /// <summary>
        /// Zero-based, globally unique index, retrieved via <see cref="DependencyProperty.GetUniqueGlobalIndex"/>.
        /// </summary>
        internal int GlobalIndex { get; }

        /// <summary>
        /// Determines the default value for this field, assigned during initial construction.
        /// </summary>
        private readonly T _defaultValue;
        /// <summary>
        /// An optimization detail representing whether a non-default value is currently stored.
        /// </summary>
        private bool _hasBeenSet;

        /// <summary>
        /// Create a new UncommonField. Stores <see langword="default"/>(<typeparamref name="T"/>) value as default.
        /// </summary>
        public UncommonField() : this(default)
        {
        }

        /// <summary>
        /// Create a new UncommonField.
        /// </summary>
        /// <param name="defaultValue">The default value of the field.</param>
        public UncommonField(T defaultValue)
        {
            _defaultValue = defaultValue;

            lock (DependencyProperty.Synchronized)
            {
                GlobalIndex = DependencyProperty.GetUniqueGlobalIndex(null, null);

                DependencyProperty.RegisteredPropertyList.Add();
            }
        }

        /// <summary>
        /// Write the given value onto a DependencyObject instance.
        /// </summary>
        /// <param name="instance">The DependencyObject on which to set the value.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(DependencyObject instance, T value)
        {
            ArgumentNullException.ThrowIfNull(instance);

            EntryIndex entryIndex = instance.LookupEntry(GlobalIndex);

            // Special case boolean operations to avoid boxing with (mostly) UIA code paths
            if (typeof(T) == typeof(bool))
            {
                // Use shared boxed instances rather than creating new objects for each SetValue call.
                object valueObject = BooleanBoxes.Box(Unsafe.BitCast<T, bool>(value));

                instance.SetEffectiveValue(entryIndex, dp: null, GlobalIndex, metadata: null, valueObject, BaseValueSourceInternal.Local);
                _hasBeenSet = true;
            }
            // Set the value if it's not the default, otherwise remove the value.
            else if (!ReferenceEquals(value, _defaultValue))
            {
                instance.SetEffectiveValue(entryIndex, dp: null, GlobalIndex, metadata: null, value, BaseValueSourceInternal.Local);
                _hasBeenSet = true;
            }
            else
            {
                instance.UnsetEffectiveValue(entryIndex, dp: null, metadata: null);
            }
        }

        /// <summary>
        /// Read the value of this field on a DependencyObject instance.
        /// </summary>
        /// <param name="instance">The DependencyObject from which to get the value.</param>
        /// <returns></returns>
        public T GetValue(DependencyObject instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (_hasBeenSet)
            {
                EntryIndex entryIndex = instance.LookupEntry(GlobalIndex);

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
        /// Clear this field from the given DependencyObject instance.
        /// </summary>
        /// <param name="instance">An instance on which to clear the value from.</param>
        public void ClearValue(DependencyObject instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            EntryIndex entryIndex = instance.LookupEntry(GlobalIndex);

            instance.UnsetEffectiveValue(entryIndex, dp: null, metadata: null);
        }
    }
}
