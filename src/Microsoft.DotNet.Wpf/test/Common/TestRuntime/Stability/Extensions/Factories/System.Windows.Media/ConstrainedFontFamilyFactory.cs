// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    [TargetTypeAttribute(typeof(FontFamily))]
    internal class ConstrainedFontFamilyFactory : DiscoverableFactory<FontFamily>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string FontUriString { get; set; }

        #endregion

        #region Override Memebers

        public override FontFamily Create(DeterministicRandom random)
        {
            return new FontFamily(FontUriString);
        }

        #endregion
    }
}
