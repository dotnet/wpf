// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml
{
    public class XamlXmlWriterSettings : XamlWriterSettings
    {
        public bool AssumeValidInput
        { get; set; }
        public bool CloseOutput
        { get; set; }

        public XamlXmlWriterSettings Copy()
        {
            return new XamlXmlWriterSettings()
            {
                AssumeValidInput = AssumeValidInput,
                CloseOutput = CloseOutput
            };
        }
    }
}
