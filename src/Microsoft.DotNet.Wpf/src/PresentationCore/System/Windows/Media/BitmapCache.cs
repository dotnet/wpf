// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

namespace System.Windows.Media
{
    public partial class BitmapCache : CacheMode
    {
        public BitmapCache()
        {
        }

        public BitmapCache(double renderAtScale)
        {
            RenderAtScale = renderAtScale;
        }
    }
}
