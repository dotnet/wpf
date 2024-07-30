// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Collections;

namespace System.Windows
{
    internal class ListConverter<TIn> : IReadOnlyList<object>
    {
        private readonly IList _values;
        private readonly Func<TIn, object> _converter;

        public ListConverter(IList values, Func<TIn, object> converter)
        {
            _values = values;
            _converter = converter;
        }

        public object this[int index] => _converter((TIn)_values[index]!);

        public int Count => _values.Count;

        // Saving a little bit of code by not writing an enumerator. It isn't huge, so this can be
        // added if needed.

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}
