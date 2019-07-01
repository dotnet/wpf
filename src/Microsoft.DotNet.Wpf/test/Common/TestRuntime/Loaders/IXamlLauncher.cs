// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;


namespace Microsoft.Test.Loaders
{
        /// <summary />
    public interface IXamlLauncher
    {
        /// <summary />
        void Start();
        /// <summary />
        void Stop();
        /// <summary />
        string XamlToLoad { get;set; }
        /// <summary />
        System.Drawing.Size ClientSize { get;set;}        
        /// <summary />
        System.Drawing.Point WindowLocation { get;set;}
        /// <summary />
        Bitmap CapturedImage  { get;}
    }
}
