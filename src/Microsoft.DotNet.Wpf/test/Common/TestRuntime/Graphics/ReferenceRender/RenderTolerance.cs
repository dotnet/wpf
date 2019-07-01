// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// Global container for rendering tolerance values
    /// </summary>
    public sealed class RenderTolerance
    {
        private RenderTolerance()
        {
        }

        static RenderTolerance()
        {
            // cache this values - only do the check once
            isVistaOrNewer = Const.IsVistaOrNewer;
            isSoftwareRendering = ( RenderCapability.Tier == 0 );
            isSquare96Dpi = ((MathEx.AreCloseEnough(Const.DpiX, Const.DpiY)) && (MathEx.AreCloseEnough(Const.DpiX, 96.0)));
            ResetDefaults();
        }

        /// <summary>
        /// Restores the tolerance to the initial defaults:
        ///     PixelToEdgeTolerance = 0.0001
        ///     LightingRangeTolerance = 0.000001
        ///     SpotLightAngleTolerance = 0.000001
        ///     ZBufferTolerance = 0.0000003
        ///     NearPlaneTolerance = 0.0000003
        ///     FarPlaneTolerance = 0.0000003
        ///     SpecularLightDotProductTolerance = 0.00001
        ///     TextureLookUpTolerance = 0.00001
        ///     SilhouetteEdgeTolerance = 1
        ///     DefaultColorTolerance = ( 0, 2, 2, 2 )
        ///     IgnoreViewportBorders = false;
        /// </summary>
        public static void ResetDefaults()
        {
            PixelToEdgeTolerance = DefaultPixelToEdgeTolerance;
            LightingRangeTolerance = DefaultLightingRangeTolerance;
            SpotLightAngleTolerance = DefaultSpotLightAngleTolerance;
            ZBufferTolerance = DefaultZBufferTolerance;
            NearPlaneTolerance = zBufferTolerance;
            FarPlaneTolerance = zBufferTolerance;
            SpecularLightDotProductTolerance = DefaultSpecularLightDotProductTolerance;
            TextureLookUpTolerance = DefaultTextureLookUpTolerance;
            SilhouetteEdgeTolerance = DefaultSilhouetteEdgeTolerance;
            DefaultColorTolerance = Color.FromArgb(0, 2, 2, 2);
            IgnoreViewportBorders = DefaultIgnoreViewportBorders;
            ViewportClippingTolerance = DefaultViewportClippingTolerance;
        }

        /// <summary>
        /// Simple printo of current values
        /// </summary>
        /// <returns>A string of current tolerance values.</returns>
        public static new string ToString()
        {
            return String.Format( "" +
                    "PixelToEdgeTolerance = {0}\n" +
                    "LightingRangeTolerance = {1}\n" +
                    "SpotLightAngleTolerance = {2}\n" +
                    "ZBufferTolerance = {3}\n" +
                    "SpecularLightDotProductTolerance = {4}\n" +
                    "TextureLookUpTolerance = {5}\n" +
                    "SilhouetteEdgeTolerance = {6}\n" +
                    "DefaultColorTolerance = {7}\n" +
                    "NearPlaneTolerance = {8}\n" +
                    "FarPlaneTolerance = {9}\n" +
                    "IgnoreViewportBorders = {10}\n" +
                    "ViewportClippingTolerance = {11}" +
                    "",
                    PixelToEdgeTolerance,
                    LightingRangeTolerance,
                    SpotLightAngleTolerance,
                    ZBufferTolerance,
                    SpecularLightDotProductTolerance,
                    TextureLookUpTolerance,
                    SilhouetteEdgeTolerance,
                    DefaultColorTolerance.ToString(),
                    NearPlaneTolerance,
                    FarPlaneTolerance,
                    IgnoreViewportBorders,
                    ViewportClippingTolerance );
        }

        /// <summary>
        /// If a pixel is this close to an edge
        /// </summary>
        public static double PixelToEdgeTolerance { get { return pixelToEdgeTolerance; } set { pixelToEdgeTolerance = value; } }

        /// <summary>
        /// If a pixel is close to the range limit of a point or spot light
        /// </summary>
        public static double LightingRangeTolerance { get { return lightingRangeTolerance; } set { lightingRangeTolerance = value; } }

        /// <summary>
        /// (In degrees) If a pixel is close to the outer cone of a spot-light
        /// </summary>
        public static double SpotLightAngleTolerance { get { return spotLightAngleTolerance; } set { spotLightAngleTolerance = value; } }

        /// <summary>
        /// If a pixel is this close to the z-buffer or the near/far planes
        /// </summary>
        public static double ZBufferTolerance { get { return zBufferTolerance; } set { zBufferTolerance = value; } }

        /// <summary>
        /// If the dot product between the light and the normal is greater than this
        /// </summary>
        public static double SpecularLightDotProductTolerance { get { return specularLightDotProductTolerance; } set { specularLightDotProductTolerance = value; } }

        /// <summary>
        /// Difference of UV for texel lookup, in screen-space
        /// </summary>
        public static double TextureLookUpTolerance { get { return textureLookUpTolerance; } set { textureLookUpTolerance = value; } }

        /// <summary>
        /// Distance in pixels to ignore around a primitive's screen space silhouette
        /// </summary>
        public static double SilhouetteEdgeTolerance { get { return silhouetteEdgeTolerance; } set { silhouetteEdgeTolerance = value; } }

        /// <summary>
        /// This value is set for every rendered pixel, independent of other error calculations
        /// </summary>
        public static Color DefaultColorTolerance
        {
            get
            {
                return defaultColorTolerance;
            }
            set
            {
                if ( isVistaOrNewer )
                {
                    // TODO: We really need a better way to get around the AA problem on Vista...
                    defaultColorTolerance = ColorOperations.Add( value, Color.FromArgb( 0, 5, 5, 5 ) );
                }
                else if ( isSoftwareRendering )
                {
                    defaultColorTolerance = ColorOperations.Add( value, Color.FromArgb( 0, 4, 4, 4 ) );
                }
                else
                {
                    defaultColorTolerance = value;
                }
            }
        }

        /// <summary>
        /// If a pixel is this close to the camera near plane, it will be ignored
        /// </summary>
        public static double NearPlaneTolerance
        {
            get { return nearPlaneTolerance; }
            set { nearPlaneTolerance = value; }
        }

        /// <summary>
        /// If a pixel is this close to the camera far plane, it will be ignored
        /// </summary>
        public static double FarPlaneTolerance
        {
            get { return farPlaneTolerance; }
            set { farPlaneTolerance = value; }
        }

        /// <summary>
        /// Ignore the entire border of a RenderBuffer
        /// </summary>
        public static bool IgnoreViewportBorders
        {
            get { return ignoreViewportBorders; }
            set { ignoreViewportBorders = value; }
        }

        /// <summary/>
        public static double ViewportClippingTolerance
        {
            get { return viewportClippingTolerance; }
            set { viewportClippingTolerance = value; }
        }

        /// <summary>
        /// True only when running in standard 96 by 96 Dpi
        /// HACK: Rendering Tolerances are relaxed for nonstandard DPI scenarios
        /// TODO: If ref-render support is changes, review all references to this property.
        /// </summary>
        internal static bool IsSquare96Dpi
        {
            get { return isSquare96Dpi; }
        }


        // tolerance defaults
        internal static readonly double DefaultPixelToEdgeTolerance = 0.0001;
        internal static readonly double DefaultLightingRangeTolerance = 0.000001;
        internal static readonly double DefaultSpotLightAngleTolerance = 0.000001;
        internal static readonly double DefaultZBufferTolerance = 0.0000003;
        internal static readonly double DefaultSpecularLightDotProductTolerance = 0.00001;
        internal static readonly double DefaultTextureLookUpTolerance = 0.00001;
        internal static readonly double DefaultSilhouetteEdgeTolerance = 1;
        internal static readonly bool DefaultIgnoreViewportBorders = false;
        internal static readonly double DefaultViewportClippingTolerance = 0.5;

        private static double pixelToEdgeTolerance;
        private static double lightingRangeTolerance;
        private static double spotLightAngleTolerance;
        private static double zBufferTolerance;
        private static double specularLightDotProductTolerance;
        private static double textureLookUpTolerance;
        private static double silhouetteEdgeTolerance;
        private static Color defaultColorTolerance;
        private static double nearPlaneTolerance;
        private static double farPlaneTolerance;
        private static bool ignoreViewportBorders;
        private static double viewportClippingTolerance;

        private static bool isVistaOrNewer;
        private static bool isSoftwareRendering;
        private static bool isSquare96Dpi;
    }
}
