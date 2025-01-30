// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xaml;

namespace System.Windows.Baml2006
{
    internal class Baml2006ReaderSettings : XamlReaderSettings
    {
        public Baml2006ReaderSettings()
        {
        }

        public Baml2006ReaderSettings(Baml2006ReaderSettings settings) : base(settings)
        {
            OwnsStream = settings.OwnsStream;
            IsBamlFragment = settings.IsBamlFragment;
        }

        public Baml2006ReaderSettings(XamlReaderSettings settings)
            : base(settings)
        {
        }

        internal bool OwnsStream { get; set; }
        internal bool IsBamlFragment { get; set; }
    }
}