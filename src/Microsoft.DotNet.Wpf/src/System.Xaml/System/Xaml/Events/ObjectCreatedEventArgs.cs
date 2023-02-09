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
    public class XamlCreatedObjectEventArgs : EventArgs
    {
        public XamlCreatedObjectEventArgs(Object createdObject)
        {
            ArgumentNullException.ThrowIfNull(createdObject);

            CreatedObject = createdObject;
        }

        public Object CreatedObject { get; private set; }
    }
}
