// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Graphics.TestTypes
{
    internal sealed class RunScriptLoader : GraphicsTestLoader
    {
        internal RunScriptLoader(TokenList tokens, string script)
            : base(tokens)
        {
            info = new InfoOrganizer(tokens, script);
            string skip = tokens.GetValue("goto");
            skipToVariation = (skip == null) ? -1 : StringConverter.ToInt(skip);
            if (skipToVariation == 0 || skipToVariation > Variation.TotalVariations)
            {
                throw new ArgumentOutOfRangeException("goto", "The specified parameter must be in the range 1-" + Variation.TotalVariations);
            }
        }

        public override bool RunMyTests()
        {
            foreach (Variation variation in info.Variations)
            {
                if (skipToVariation > 0 && variation.ID != skipToVariation)
                {
                    continue;
                }

                CoreGraphicsTest test = variation.CreateTest();

                test.RunTheTest();
                test.LogResult();
            }

            if (skipToVariation > 0)
            {
                Variation.TotalVariations = 1;
            }
            return CoreGraphicsTest.EndTheTest();
        }

        private InfoOrganizer info;
        private int skipToVariation;
    }
}
