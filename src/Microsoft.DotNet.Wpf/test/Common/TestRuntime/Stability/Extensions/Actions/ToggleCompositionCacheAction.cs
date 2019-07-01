// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    public class ToggleCompositionCacheAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public UIElement Target { get; set; }

        public BitmapCache CacheMode;

        //Toggle whether this UIElement has a cache or not
        //This will lead to awesomely random combinations of caches
        public override void Perform()
        {
            if(Target.CacheMode == null)
            {
                Target.CacheMode = CacheMode;
            }
            else
            {
                Target.CacheMode = null;
            }
        }
    }
#endif
}
