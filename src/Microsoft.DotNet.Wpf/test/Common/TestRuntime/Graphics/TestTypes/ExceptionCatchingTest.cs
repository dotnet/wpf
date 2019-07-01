// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    ///   Create a bad scene that should throw an exception when rendered
    /// </summary>
    public class ExceptionCatchingTest : RenderingTest
    {
        /// <summary/>
        public override void Init(Variation v)
        {
            base.Init(v);
            v.AssertExistenceOf("ExpectedException", "Visual");

            exceptionType = Type.GetType(v["ExpectedException"]);
            visual = VisualFactory.MakeVisual(v["Visual"]);
        }

        /// <summary/>
        public override void RunTheTest()
        {
            // "Try" expects an exception to be thrown.
            // If it isn't, the test will fail

            Try(base.RunTheTest, exceptionType);
        }

        /// <summary/>
        public override Visual GetWindowContent()
        {
            return visual;
        }

        /// <summary/>
        public override void Verify()
        {
            // Do nothing.
            // If no exception is thrown, a failure will be logged by "Try" in "RunTheTest"
        }

        private Type exceptionType;
        private Visual visual;
    }
}
