// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml.MS.Impl
{
    internal class PositionalParameterDescriptor
    {
        public object Value { get; set; }
        public bool WasText { get; set; }

        public PositionalParameterDescriptor(object value, bool wasText)
        {
            Value = value;
            WasText = wasText;
        }
    }
}
