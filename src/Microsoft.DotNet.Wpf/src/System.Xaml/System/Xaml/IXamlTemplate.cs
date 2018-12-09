// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using System.Text;

#if SILVERLIGHTXAML
namespace MS.Internal.Xaml
#else
namespace System.Xaml
#endif    
{
#if SILVERLIGHTXAML
    internal
#else
    public
#endif
    interface IXamlTemplate
    {
        void RecordXaml(XamlReader reader, XamlSavedContext templateContext);
        XamlReader PlayXaml();
        XamlSavedContext TemplateContext { get; }
    }

#if SILVERLIGHTXAML
    internal
#else
    public
#endif
    interface IXamlTemplate<T>: IXamlTemplate
    {
    }
}
