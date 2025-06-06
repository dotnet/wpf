﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Xaml
{
    public class XamlXmlReaderSettings : XamlReaderSettings
    {
        public string XmlLang { get; set; }
        public bool XmlSpacePreserve { get; set; }
        public bool SkipXmlCompatibilityProcessing { get; set; }
        public bool CloseInput { get; set; }

        internal Dictionary<string, string> _xmlnsDictionary;

        public XamlXmlReaderSettings()
        {
        }

        public XamlXmlReaderSettings(XamlXmlReaderSettings settings)
            : base(settings)
        {
            if (settings is not null)
            {
                if (settings._xmlnsDictionary is not null)
                {
                    _xmlnsDictionary = new Dictionary<string, string>(settings._xmlnsDictionary);
                }

                XmlLang = settings.XmlLang;
                XmlSpacePreserve = settings.XmlSpacePreserve;
                SkipXmlCompatibilityProcessing = settings.SkipXmlCompatibilityProcessing;
                CloseInput = settings.CloseInput;
            }
        }
    }
}
