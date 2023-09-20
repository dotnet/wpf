// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;

namespace System.Windows.Markup
{
    /// <summary>
    /// A type to isolate our handling of System.Xml types so that we
    /// don't have to load System.Xml in the BAML case unless we really
    /// do have an XML Island.
    /// </summary>
    [ContentProperty("Text")]
    public sealed class XData
    {
        private XmlReader _reader;
        private string _text;

        public XData()
        {
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _reader = null;
            }
        }

        // XmlReader is typed "object" so that the calling code can read
        // and handle the value without loading System.Xml.dll.
        public object XmlReader
        {
            get => _reader ??= Xml.XmlReader.Create(new StringReader(Text));
            set
            {
                _reader = value as XmlReader;
                _text = null;
            }
        }
    }
}
