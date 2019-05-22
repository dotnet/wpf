// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: See <summary> below.
//

using System;
using System.ComponentModel;

/// <summary>
/// A simple INotifyPropertyChanged wrapper used by the SpellChecker class. 
/// This is used to register for and generate notifications whenever 
/// the unerlying ISpellChecker instance has to be recreated from the COM
/// factory. When such a reinstantiation happens, we need to keep track 
/// of any registered event handlers and migrate them over to the 
/// new ISpellChecker instance.
/// </summary>


namespace System.Windows.Documents.MsSpellCheckLib
{
    internal interface IChangeNotifyWrapper : INotifyPropertyChanged
    {
        object Value { get; set; }
    }

    internal interface IChangeNotifyWrapper<T> : IChangeNotifyWrapper
    {
        new T Value { get; set; }
    }

    internal class ChangeNotifyWrapper<T> : IChangeNotifyWrapper<T>
    {
        internal ChangeNotifyWrapper(T value = default(T), bool shouldThrowInvalidCastException = false)
        {
            Value = value;
            _shouldThrowInvalidCastException = shouldThrowInvalidCastException;
        }

        public T Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        object IChangeNotifyWrapper.Value
        {
            get
            {
                return Value;
            }

            set
            {
                T coercedValue = default(T);

                try
                {
                    coercedValue = (T)value;
                }
                catch (InvalidCastException)
                    when (!_shouldThrowInvalidCastException)
                {
                    return;
                }

                Value = coercedValue;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private T _value;
        private bool _shouldThrowInvalidCastException;
    }
}