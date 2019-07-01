// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Graphics.ReferenceRender;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Base test class for all tests that use tolerance
    /// </summary>
    public abstract class VisualVerificationTest : RenderingTest
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public override void Init(Variation v)
        {
            base.Init(v);
            ParseOptionalParameters(v);
            FactoryParser.MakeTolerance(v);
        }


        /// <summary/>
        protected virtual void ParseOptionalParameters(Variation v)
        {
#if !STANDALONE_BUILD
            // For logging purposes in the lab, we don't need to save ALL of these images...
            bool enableLessCrucialOptions = false;
#else
            bool enableLessCrucialOptions = true;
#endif

            forceSave = v["ForceSave", false];
            saveXamlRepro = v["SaveXamlRepro", false];
            saveExpectedFB = v["SaveExpectedFrameBuffer", true];
            saveExpectedTB = v["SaveExpectedToleranceBuffer", true];
            saveExpectedZB = v["SaveExpectedZBuffer", enableLessCrucialOptions];
            saveDiffFB = true; // v["SaveDiffFrameBuffer", enableLessCrucialOptions];
            saveDiffTB = v["SaveDiffToleranceBuffer", true];
            saveDiffZB = v["SaveDiffZBuffer", enableLessCrucialOptions];
            verifySerialization = v["VerifySerialization", false];

            renderingEffect = (v["RenderingEffect"] != null)
                                ? (RenderingEffect)Enum.Parse(typeof(RenderingEffect), v["RenderingEffect"])
                                : RenderingEffect.None;
            interpolationMode = (v["InterpolationMode"] != null)
                                ? (InterpolationMode)Enum.Parse(typeof(InterpolationMode), v["InterpolationMode"])
                                : InterpolationMode.Gouraud;

            SceneRenderer.EnableAntiAliasedRendering = v["EnableAntiAliasedRendering", false];
            TextureFilter.SaveTextures = v["SaveTextures", false];

            numAllowableMismatches = -1;
            VScanToleranceFile = null;

            // First try getting them from the variation parameters
            if (v["VScanToleranceFile"] != null)
            {
                Int32.TryParse((string)v["NumAllowableMismatches"], out numAllowableMismatches);
            }
            if (v["VScanToleranceFile"] != null)
            {
                VScanToleranceFile = (string)v["VScanToleranceFile"];
            }

            // If not found, try the test driver parameters
            if (numAllowableMismatches < 0)
            {
                Int32.TryParse((string)Application.Current.Properties["NumAllowableMismatches"], out numAllowableMismatches);
            }
            if (VScanToleranceFile == null)
            {
                VScanToleranceFile = (string)Application.Current.Properties["VScanToleranceFile"];
            }
        }

        /// <summary/>
        protected void VerifySerialization(Visual content)
        {
            if (verifySerialization)
            {
                Log("");
                Log("Round tripping the Visual...");

                object roundTrippedContent = SerializationTest.RoundTrip(content);
                if (!ObjectUtils.DeepEqualsToAnimatable(content, roundTrippedContent))
                {
                    AddFailure("Round tripping this Visual failed");
                }
            }
        }
        /// <summary/>
        protected void VerifyWithSceneRenderer(SceneRenderer sceneRenderer)
        {
            Color[,] screenCapture = GetScreenCapture();

            this.sceneRenderer = sceneRenderer;

            Log("Invoking SceneRenderer...");
            if (variation["LogRendererPerformance"] != null &&
                 StringConverter.ToBool(variation["LogRendererPerformance"]) == true)
            {
                RenderAndLogPerf();
            }
            else
            {
                RenderWithSceneRenderer();
            }

            Log(RenderVerifier.GetErrorStatistics(renderBuffer));
            Log("Verifying using SceneRenderer error metric.");

            int differences = RenderVerifier.VerifyRender(screenCapture, renderBuffer, numAllowableMismatches, VScanToleranceFile);
            if (differences > 0)
            {
                AddFailure("{0} pixels did not meet the tolerance criteria", differences);
                if (saveXamlRepro)
                {
                    Point[] failPoints = RenderVerifier.GetPointsWithFailures(screenCapture, renderBuffer);
                    sceneRenderer.SaveSelectedSubSceneAsXaml(failPoints, logPrefix + "_Repro.xaml");
                    Log("Failing triangles repro saved as: " + logPrefix + "_Repro.xaml");
                }
            }
            else if (saveXamlRepro && forceSave)
            {
                sceneRenderer.SaveSelectedSubSceneAsXaml(null, logPrefix + "_Serialized.xaml");
                Log("Current variation serialized as: " + logPrefix + "_Serialized.xaml");
            }

            if (differences > 0 || forceSave)
            {
                PhotoConverter.SaveImageAs(screenCapture, logPrefix + "_Rendered.png", true);
                LogImageSaved("Rendered Image:", logPrefix + "_Rendered.png");
                SaveBuffers(screenCapture);
            }
        }

        private void RenderAndLogPerf()
        {
            Log("Drawing combined pass ...  ");
            DateTime before = DateTime.Now;
            RenderWithSceneRenderer();
            DateTime after = DateTime.Now;
            Log("DONE. Render time= " + (after - before).ToString());
        }


        /// <summary/>
        protected virtual void RenderWithSceneRenderer()
        {
            switch (renderingEffect)
            {
                case RenderingEffect.None:
                    renderBuffer = sceneRenderer.Render(interpolationMode);
                    break;

                case RenderingEffect.Silhouette:
                    renderBuffer = sceneRenderer.Render(interpolationMode);
                    renderBuffer.MakeSilhouette();
                    break;

                case RenderingEffect.NoRendering:
                    // Nothing should render in bad parameter cases, so create a RenderBuffer that only
                    //  shows the background color of the window.

                    if (BackgroundColor.A != 255)
                    {
                        throw new ApplicationException("The window's background must be opaque");
                    }
                    renderBuffer = new RenderBuffer(WindowWidth, WindowHeight, BackgroundColor);
                    break;

                default:
                    throw new NotImplementedException("Support for this effect has not yet been implemented");
            }
        }


        /// <summary/>
        protected void SaveBuffers(Color[,] screenCapture)
        {
            string filename = string.Empty;
            if (saveExpectedFB)
            {
                filename = logPrefix + "_Expected_fb.png";
                PhotoConverter.SaveImageAs(renderBuffer.FrameBuffer, filename, true);
                LogImageSaved("Expected Image", filename);
            }
            if (saveExpectedTB)
            {
                filename = logPrefix + "_Expected_tb.png";
                PhotoConverter.SaveImageAs(renderBuffer.ToleranceBuffer, filename, false);
                LogImageSaved("Expected Tolerance", filename);
            }
            if (saveExpectedZB)
            {
                Color[,] zbuffer = PhotoConverter.ToColorArray(renderBuffer.ZBuffer);
                filename = logPrefix + "_Expected_zb.png";
                PhotoConverter.SaveImageAs(zbuffer, filename, false);
                LogImageSaved("Expected Z Buffer", filename);
            }

            RenderBuffer diff = RenderVerifier.ComputeDifference(screenCapture, renderBuffer);
            if (saveDiffFB)
            {
                filename = logPrefix + "_Diff_fb.png";
                PhotoConverter.SaveImageAs(diff.FrameBuffer, filename, true);
                LogImageSaved("Diff Image", filename);
            }
            if (saveDiffTB)
            {
                filename = logPrefix + "_Diff_tb.png";
                PhotoConverter.SaveImageAs(diff.ToleranceBuffer, filename, false);
                LogImageSaved("Diff Tolerance", filename);
            }
            if (saveDiffZB)
            {
                Color[,] zbuffer = PhotoConverter.ToColorArray(diff.ZBuffer);
                filename = logPrefix + "_Diff_zb.png";
                PhotoConverter.SaveImageAs(zbuffer, filename, false);
                LogImageSaved("Diff Z Buffer", filename);
            }
        }

        /// <summary/>
        protected void LogImageSaved(string description, string filename)
        {
            Log("{0} saved as {1}", description, filename);
        }

        private enum RenderingEffect
        {
            None,
            Silhouette,
            NoRendering,
        }

        // Verification helpers
        private SceneRenderer sceneRenderer;
        /// <summary/>
        protected RenderBuffer renderBuffer;

        // Optional paramters

        /// <summary>
        /// Force-save the xaml repro
        /// </summary>
        protected bool forceSave;
        private bool saveXamlRepro;
        private bool saveExpectedFB;
        private bool saveExpectedTB;
        private bool saveExpectedZB;
        private bool saveDiffFB;
        private bool saveDiffTB;
        private bool saveDiffZB;
        private bool verifySerialization;
        private RenderingEffect renderingEffect;
        private InterpolationMode interpolationMode;
        protected int numAllowableMismatches; // Number of pixel mismatches that may be ignored
        protected string VScanToleranceFile;  // Path to VScan tolerance file (if any)
    }
}
