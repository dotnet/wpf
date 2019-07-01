// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create StylusPointCollection
    /// </summary>
    internal class StylusPointCollectionFactory : DiscoverableCollectionFactory<StylusPointCollection, StylusPoint> 
    {
        /// <summary>
        /// StylusPointCollection cannot be empty when attached to a Stroke.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true, MinListSize = 1)]
        public override List<StylusPoint> ContentList { get; set; }
    }
}
