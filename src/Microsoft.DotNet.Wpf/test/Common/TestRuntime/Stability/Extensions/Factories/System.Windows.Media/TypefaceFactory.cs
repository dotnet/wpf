// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class TypefaceFactory : DiscoverableFactory<Typeface>
    {
        #region Private Members

        private Typeface typeface = null;

        #endregion

        #region Public Members

        public FontFamily FallbackFontFamily { get; set; }

        /// <summary>
        /// FontFamily can't be null.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public FontFamily FontFamily { get; set; }

        #endregion

        public override Typeface Create(DeterministicRandom random)
        {
            FontStyle fontStyle = random.NextStaticProperty<FontStyle>(typeof(FontStyles));
            FontWeight fontWeight = random.NextStaticProperty<FontWeight>(typeof(FontWeights));
            FontStretch fontStretch = random.NextStaticProperty<FontStretch>(typeof(FontStretches));

            switch (random.Next(3))
            {
                case 0:
                    typeface = new Typeface(FontFamily.Source);
                    break;
                case 1:
                    typeface = new Typeface(FontFamily, fontStyle, fontWeight, fontStretch);
                    break;
                case 2:
                    typeface = new Typeface(FontFamily, fontStyle, fontWeight, fontStretch, FallbackFontFamily);
                    break;
            }

            return typeface;
        }
    }
}
