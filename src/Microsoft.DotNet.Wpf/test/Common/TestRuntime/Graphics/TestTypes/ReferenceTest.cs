// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Graphics.Factories;
using Microsoft.Test.Graphics.ReferenceRender;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Compare two renderings of different types - 
    /// i.e. Software vs Hardware, Hardware vs RefRast, etc.
    /// </summary>
    public class ReferenceTest : RenderingTest
    {
        /// <summary/>
        public override void Init(Variation v)
        {
            base.Init(v);
            v.AssertExistenceOf("RenderingMode", "Visual", "ReferenceWindowPosition");

            renderingMode = v["RenderingMode"];

            // Create two copies so that we don't have aliasing problems
            firstVisual = VisualFactory.MakeVisual(v["Visual"]);
            secondVisual = VisualFactory.MakeVisual(v["Visual"]);
            firstPass = true;

            referenceWindowPosition = StringConverter.ToPoint(v["ReferenceWindowPosition"]);
        }

        /// <summary/>
        public override void RunTheTest()
        {
            RenderWindowContent();
            firstCapture = GetScreenCapture();

            ExchangeWindows();

            RenderWindowContent();
            VerifyWithinContext();
        }

        /// <summary/>
        public override Visual GetWindowContent()
        {
            if (firstPass)
            {
                firstPass = false;
                return firstVisual;
            }
            else
            {
                return secondVisual;
            }
        }

        /// <summary/>
        public override void Verify()
        {
            // Compare the two screen captures

            RenderBuffer renderBuffer = new RenderBuffer(firstCapture, BackgroundColor);
            Color[,] screenCapture = GetScreenCapture();
            int differences = RenderVerifier.VerifyRender(screenCapture, renderBuffer);

            // Log failures, if any
            if (differences > 0)
            {
                AddFailure("{0} pixels did not meet the tolerance criteria.", differences);
            }
            if (Failures != 0)
            {
                RenderBuffer diff = RenderVerifier.ComputeDifference(screenCapture, renderBuffer);
                PhotoConverter.SaveImageAs(screenCapture, logPrefix + "_Rendered.png");
                PhotoConverter.SaveImageAs(renderBuffer.FrameBuffer, logPrefix + "_Expected_fb.png");
                PhotoConverter.SaveImageAs(diff.ToleranceBuffer, logPrefix + "_Diff_tb.png");
                PhotoConverter.SaveImageAs(diff.FrameBuffer, logPrefix + "_Diff_fb.png");
            }
        }

        /// <summary/>
        protected override RenderingWindow GetNewWindow()
        {
            Point oldPosition = variation.WindowPosition;
            variation.WindowPosition = referenceWindowPosition;

            RenderingWindow window = RenderingWindow.Create(variation);

            variation.WindowPosition = oldPosition;

            return window;
        }

        private Color[,] firstCapture;
        private string renderingMode;
        private Visual firstVisual;
        private Visual secondVisual;
        private bool firstPass;
        private Point referenceWindowPosition;
    }
}
