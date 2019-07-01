// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class CustomContentControlFactory : DiscoverableFactory<CustomContentControl>
    {
        #region Public Members

        public object ExtraContent { get; set; }

        #endregion

        #region Override Members

        public override CustomContentControl Create(DeterministicRandom random)
        {
            CustomContentControl customControl = new CustomContentControl();

            customControl.ExtraContent = ExtraContent;

            return customControl;
        }

        #endregion
    }
}
