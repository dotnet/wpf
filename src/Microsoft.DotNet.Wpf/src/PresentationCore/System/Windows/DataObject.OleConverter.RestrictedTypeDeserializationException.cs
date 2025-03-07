// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows
{
    public sealed partial class DataObject
    {
        private partial class OleConverter
        {
            /// <summary>
            /// Private exception to signal when a restricted type was encountered during deserialization.
            /// </summary>
            private class RestrictedTypeDeserializationException : Exception
            {
            }
        }
    }
}
