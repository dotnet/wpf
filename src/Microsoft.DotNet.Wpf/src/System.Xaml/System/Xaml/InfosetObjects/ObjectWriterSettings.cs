// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Xaml
{
#if SILVERLIGHTXAML
    internal
#else
    public
#endif
    class ObjectWriterSettings
    {
        public EventHandler<XamlCreatedObjectEventArgs> ObjectCreatedHandler { get; set; }
        public Object RootObjectInstance { get; set; }
        public bool IgnoreCanConvert { get; set; }
        public System.Windows.Markup.INameScope NameScope { get; set; }
    }
}
