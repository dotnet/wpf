// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media;

#if !STANDALONE_BUILD
using TrustedPath = Microsoft.Test.Security.Wrappers.PathSW;
#else
using TrustedPath = System.IO.Path;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Base test class for all XAML testing
    /// </summary>
    public class XamlTest : VisualVerificationTest
    {

        /// <summary/>
        public override void Init(Variation v)
        {
            base.Init(v);
            parameters = new XamlTestObjects(v);
            logPrefix = TrustedPath.GetFileNameWithoutExtension(parameters.Filename) + "_" + TwoDigitVariationID(v.ID);
        }

        /// <summary/>
        public override Visual GetWindowContent()
        {
            return parameters.Content;
        }

        /// <summary/>
        public override void Verify()
        {
            VerifyWithSceneRenderer(parameters.SceneRenderer);
        }

        /// <summary/>
        protected XamlTestObjects parameters;
    }
}
