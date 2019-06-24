// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace MS.Internal.Text
{
    // We use encodings that are not provided by default in core.
    // This class makes sure that we register extra providers that are required before use.
    internal static class InternalEncoding
    {

        static InternalEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        internal static Encoding GetEncoding(int codepage)
        {
            return Encoding.GetEncoding(codepage);
        }

        internal static Encoding Default
        {
            get
            {
                return Encoding.Default;
            }
        }

    };
}
