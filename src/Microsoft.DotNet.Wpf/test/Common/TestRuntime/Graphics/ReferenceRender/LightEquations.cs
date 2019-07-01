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
    internal abstract class LightEquation
    {
        public LightEquation(Color lightColor, Matrix3D viewMatrix)
        {
            this.lightColor = lightColor;
            this.lightColor.A = 255; // Force light to opaque
            lastIlluminationError = emptyColor;
            this.transform = viewMatrix;
        }

        virtual public Color IluminateDiffuse(Point3D modelPosition, Vector3D normal)
        {
            return unlitColor;
        }

        virtual public Color IluminateSpecular(Point3D modelPosition, Vector3D normal, double specularPower)
        {
            return unlitColor;
        }

        public Color GetLastError()
        {
            // Lights shouldn't have error in the alpha channel,
            //  but "ScaleOpaque" is often used to create this value.
            lastIlluminationError.A = 0;
            return lastIlluminationError;
        }

        protected double GetDiffuseContribution(Vector3D normal, Vector3D light)
        {
            // Make sure our inputs are unit vectors
            normal = MathEx.Normalize(normal);
            light = MathEx.Normalize(light);
            // Diffuse contribution is N dot L
            double diffuseContribution = MathEx.DotProduct(normal, light);
            // Make sure it stays positive so we don't iluminate angles > 180
            diffuseContribution = Math.Max(diffuseContribution, 0.0);
            return diffuseContribution;
        }

        protected double GetSpecularContribution(Vector3D view, Vector3D normal, Vector3D light, double exponent)
        {
            System.Diagnostics.Debug.Assert(RenderTolerance.SpecularLightDotProductTolerance >= 0);

            // Make vectors unit length
            normal = MathEx.Normalize(normal);
            light = MathEx.Normalize(light);
            view = MathEx.Normalize(view);

            double spec = 0.0;
            double nDotl = MathEx.DotProduct(light, normal);
            if (nDotl >= -RenderTolerance.SpecularLightDotProductTolerance)
            {
                Vector3D half = MathEx.Normalize(light + view);

                spec = Math.Max(MathEx.DotProduct(normal, half), 0);
                spec = Math.Pow(spec, exponent);

                // We want a +/- tolerance bound on the dot product computation
                if (Math.Abs(nDotl) <= RenderTolerance.SpecularLightDotProductTolerance)
                {
                    // This is so close, we may or may not light this point
                    // So we compute the maximum possible error by scaling (0,255,255,255) by the
                    // light contribution we would have if we DID light it.
                    Color expected = ColorOperations.ScaleOpaque(lightColor, spec);
                    Color specError = ColorOperations.Modulate(expected, lightToleranceColor);

                    // We then add this error to our accumulated error for this illumination call
                    lastIlluminationError = ColorOperations.Add(lastIlluminationError, specError);

                    // ignore it for our rendering
                    spec = 0.0;
                }
            }
            return spec;
        }

        public static LightEquation For(Light light, Matrix3D viewMatrix)
        {
            if (light is AmbientLight)
            {
                return new AmbientLightEquation((AmbientLight)light, viewMatrix);
            }
            if (light is SpotLight)
            {
                return new SpotLightEquation((SpotLight)light, viewMatrix);
            }
            if (light is PointLight)
            {
                return new PointLightEquation((PointLight)light, viewMatrix);
            }
            if (light is DirectionalLight)
            {
                return new DirectionalLightEquation((DirectionalLight)light, viewMatrix);
            }
            throw new NotImplementedException("The lighting equation is not implemented for lights of type " + light.GetType().ToString());
        }

        protected Matrix3D transform;
        protected Color lightColor;
        protected Color lastIlluminationError;

        protected static Color emptyColor = Color.FromArgb(0, 0, 0, 0);
        protected static Color unlitColor = Color.FromArgb(255, 0, 0, 0); // lights have no transparency
        protected static Color lightToleranceColor = Color.FromArgb(0, 255, 255, 255);
    }

    internal class AmbientLightEquation : LightEquation
    {
        public AmbientLightEquation(AmbientLight light, Matrix3D viewMatrix)
            : base(light.Color, viewMatrix)
        {
        }

        public override Color IluminateDiffuse(Point3D position, Vector3D normal)
        {
            return lightColor;
        }
    }

    internal class DirectionalLightEquation : LightEquation
    {
        public DirectionalLightEquation(DirectionalLight light, Matrix3D viewMatrix)
            : base(light.Color, viewMatrix)
        {
            // Account for model hierarchy transform
            direction = MatrixUtils.Transform(light.Direction, light.Transform);
            direction = MathEx.Normalize(direction);
            // Account for view dependent transform
            direction = MatrixUtils.Transform(direction, viewMatrix);
            direction = MathEx.Normalize(direction);
        }

        public override Color IluminateDiffuse(Point3D modelPosition, Vector3D normal)
        {
            // Start with no additional error
            lastIlluminationError = emptyColor;

            double diffuseContribution = GetDiffuseContribution(normal, -direction);
            Color diffuseLightColor = ColorOperations.ScaleOpaque(lightColor, diffuseContribution);
            return diffuseLightColor;
        }

        public override Color IluminateSpecular(Point3D modelPosition, Vector3D normal, double specularPower)
        {
            // Start with no additional error
            lastIlluminationError = emptyColor;

            Vector3D view = Const.p0 - modelPosition;
            double specularContribution = GetSpecularContribution(view, normal, -direction, specularPower);
            Color specularLightColor = ColorOperations.ScaleOpaque(lightColor, specularContribution);
            return specularLightColor;
        }

        Vector3D direction;
    }

    internal class PointLightEquation : LightEquation
    {
        public PointLightEquation(PointLightBase light, Matrix3D viewMatrix)
            : base(light.Color, viewMatrix)
        {
            // Account for model hierarchy transform
            position = MatrixUtils.Transform(light.Position, light.Transform);
            // Account for view dependent transform
            position = MatrixUtils.Transform(position, viewMatrix);

            // We want lights to scale with the transform, but don't want to do all lighting in
            // light space. Our compromise is to use the average scale factor of the matrix to
            // scale our lights. This means we're exactly right for uniform scales and arbitrarily
            // wrong for non-uniform scales. This was an Avalon design decision.

            double averageScaleFactor = 1.0;

            if (light.Transform != null)
            {
                averageScaleFactor = MatrixUtils.GetAverageScaleFactor(light.Transform.Value);
            }

            // Now we need to account for scaled attenuation and range
            constantAttenuation = light.ConstantAttenuation;
            linearAttenuation = light.LinearAttenuation / averageScaleFactor;
            quadraticAttenuation = light.QuadraticAttenuation / (averageScaleFactor * averageScaleFactor);
            range = light.Range * averageScaleFactor;
        }

        public override Color IluminateDiffuse(Point3D modelPosition, Vector3D normal)
        {
            // Start with no additional error
            lastIlluminationError = emptyColor;

            Vector3D lightDirection = this.position - modelPosition;
            double diffuseContribution = GetDiffuseContribution(normal, lightDirection);
            double attenuatedContribution = GetAttenuatedContribution(modelPosition, diffuseContribution);
            Color diffuseLightColor = ColorOperations.ScaleOpaque(lightColor, attenuatedContribution);
            return diffuseLightColor;
        }

        public override Color IluminateSpecular(Point3D modelPosition, Vector3D normal, double specularPower)
        {
            // Start with no additional error
            lastIlluminationError = emptyColor;

            Vector3D lightDirection = this.position - modelPosition;
            Vector3D view = Const.p0 - modelPosition;
            double specularContribution = GetSpecularContribution(view, normal, lightDirection, specularPower);
            double attenuatedContribution = GetAttenuatedContribution(modelPosition, specularContribution);
            Color specularLightColor = ColorOperations.ScaleOpaque(lightColor, attenuatedContribution);
            return specularLightColor;
        }

        protected virtual double GetAttenuatedContribution(Point3D modelPosition, double nonAttenuatedValue)
        {
            Vector3D lightDirection = this.position - modelPosition;
            double distance = MathEx.Length(lightDirection);

            double attenuation = constantAttenuation
                    + linearAttenuation * distance
                    + quadraticAttenuation * distance * distance;

            // We want to make sure the we have no augmenting or negative attenuation
            attenuation = Math.Max(1.0, attenuation);

            double finalContribution;
            if (distance > range) // We don't light things that are out of range
            {
                finalContribution = 0.0;
            }
            else
            {
                // We take specular and diffuse and divide to attenuate
                finalContribution = nonAttenuatedValue / attenuation;
            }

            // Prevent negative contributions
            finalContribution = Math.Max(finalContribution, 0);

            if (Math.Abs(distance - range) < RenderTolerance.LightingRangeTolerance)
            {
                // This is so close, we may or may not light this point
                // So we compute the maximum possible error by scaling (255,255,255) by the
                // light contribution we would have if we DID light it.
                Color expected = ColorOperations.ScaleOpaque(lightColor, finalContribution);
                Color rangeError = ColorOperations.Modulate(expected, lightToleranceColor);

                // We then add this error to our accumulated error for this illumination call
                lastIlluminationError = ColorOperations.Add(lastIlluminationError, rangeError);
            }

            // We don't clamp to 1, since this would break attenuating the falloff
            //  of a spotlight.

            return finalContribution;
        }

        protected Point3D position;
        protected double quadraticAttenuation;
        protected double linearAttenuation;
        protected double constantAttenuation;
        protected double range;
    }

    internal class SpotLightEquation : PointLightEquation
    {
        public SpotLightEquation(SpotLight light, Matrix3D viewMatrix)
            : base(light, viewMatrix)
        {
            // Account for model hierarchy transform
            direction = MatrixUtils.Transform(light.Direction, light.Transform);
            direction = MathEx.Normalize(direction);

            // Account for view dependent transform
            // Update inverse transpose matrix
            inverseTransposeTransform = viewMatrix;
            inverseTransposeTransform.Invert();
            inverseTransposeTransform = MatrixUtils.Transpose(inverseTransposeTransform);
            // forcing affine matrix - we really only care about the inner 3x3
            inverseTransposeTransform.M44 = 1.0;
            inverseTransposeTransform.M14 = 0.0;
            inverseTransposeTransform.M24 = 0.0;
            inverseTransposeTransform.M34 = 0.0;
            // update direction
            direction = MatrixUtils.Transform(direction, inverseTransposeTransform);
            direction = MathEx.Normalize(direction);

            // Our lighting model assumes angles are measured from light direction vector,
            // but DX and Avalon specify angles as total degrees of spread
            innerConeAngle = light.InnerConeAngle / 2.0;
            outerConeAngle = light.OuterConeAngle / 2.0;

            cosInnerConeAngle = Math.Cos(MathEx.ToRadians(innerConeAngle));
            cosOuterConeAngle = Math.Cos(MathEx.ToRadians(outerConeAngle));

            if (innerConeAngle > outerConeAngle)
            {
                Console.WriteLine("\n  WARNING: SpotLight OuterConeAngle is greater than InnerConeAngle!");
                Console.WriteLine("  This is an invalid light and should only be used with RenderingEffect.Silhouette\n");
            }
        }

        protected override double GetAttenuatedContribution(Point3D modelPosition, double nonAttenuatedValue)
        {
            // distance+falloff Attenuated raw contribution
            double attenuatedContribution = base.GetAttenuatedContribution(modelPosition, nonAttenuatedValue);

            Vector3D surfaceDirection = MathEx.Normalize(modelPosition - this.position);
            double cosSurfaceAngle = MathEx.DotProduct(surfaceDirection, direction);
            double surfaceAngle = MathEx.ToDegrees(Math.Acos(cosSurfaceAngle));

            double angleFactor;
            if (surfaceAngle <= innerConeAngle)
            {
                angleFactor = 1.0;
            }
            // Make sure we render in the larger bound, since it will affect our final error
            else if (surfaceAngle > innerConeAngle
                        && surfaceAngle < (outerConeAngle + RenderTolerance.SpotLightAngleTolerance))
            {
                // Do DX light equation fade
                angleFactor = cosSurfaceAngle - cosInnerConeAngle;
                angleFactor /= cosOuterConeAngle - cosInnerConeAngle;
                angleFactor = Math.Max(0, angleFactor);

                // TODO: (alsteven) Figure out why I need to do this next step
                angleFactor = 1.0 - angleFactor;
            }
            else // Greater than outer cone angle
            {
                angleFactor = 0.0;
            }

            double totalContribution = attenuatedContribution * angleFactor;

            // If we're within SpotLightAngleTolerance degrees of being unlit, then set
            // error based on how much light we expect
            if (Math.Abs(surfaceAngle - outerConeAngle) < RenderTolerance.SpotLightAngleTolerance)
            {
                // This is so close, we may or may not light this point
                // So we compute the maximum possible error by scaling (255,255,255) by the
                // light contribution we would have if we DID light it.
                Color expected = ColorOperations.ScaleOpaque(lightColor, totalContribution);
                Color angleError = ColorOperations.Modulate(expected, lightToleranceColor);

                lastIlluminationError = ColorOperations.Add(lastIlluminationError, angleError);
            }
            return totalContribution;
        }

        protected Vector3D direction;
        protected double innerConeAngle;
        protected double outerConeAngle;
        protected double cosInnerConeAngle;
        protected double cosOuterConeAngle;
        protected Matrix3D inverseTransposeTransform;
    }

}
