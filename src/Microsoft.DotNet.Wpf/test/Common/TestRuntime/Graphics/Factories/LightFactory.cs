// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;


namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class LightFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Light MakeLight(string light)
        {
            string[] parameters = light.Split(' ');

            switch (parameters[0])
            {
                case "WhiteAmbient": return WhiteAmbient;
                case "NonWhiteAmbient": return NonWhiteAmbient;
                case "WhiteDirectionalNegZ": return WhiteDirectionalNegZ;
                case "NonWhiteDirectionalNegZ": return NonWhiteDirectionalNegZ;
                case "WhiteDirectionalNegAll": return WhiteDirectionalNegAll;
                case "NonWhiteDirectionalNegAll": return NonWhiteDirectionalNegAll;
                case "WhitePoint": return WhitePoint;
                case "NonWhitePoint": return NonWhitePoint;
                case "WhitePointBelow": return WhitePointBelow;
                case "NonWhitePointBelow": return NonWhitePointBelow;
                case "WhiteSpot": return WhiteSpot;

                case "AmbientLight":
                    return new AmbientLight(StringConverter.ToColor(parameters[1]));

                case "DirectionalLight":
                    return new DirectionalLight(StringConverter.ToColor(parameters[1]), StringConverter.ToVector3D(parameters[2]));

                case "PointLight":
                    {
                        PointLight l = new PointLight();
                        l.Color = StringConverter.ToColor(parameters[1]);
                        l.Position = StringConverter.ToPoint3D(parameters[2]);
                        l.Range = StringConverter.ToDouble(parameters[3]);
                        if (parameters.Length > 4)
                        {
                            l.ConstantAttenuation = StringConverter.ToDouble(parameters[4]);
                            l.LinearAttenuation = StringConverter.ToDouble(parameters[5]);
                            l.QuadraticAttenuation = StringConverter.ToDouble(parameters[6]);
                        }
                        return l;
                    }

                case "SpotLight":
                    {
                        SpotLight l = new SpotLight();
                        l.Color = StringConverter.ToColor(parameters[1]);
                        l.Position = StringConverter.ToPoint3D(parameters[2]);
                        l.Direction = StringConverter.ToVector3D(parameters[3]);
                        l.Range = StringConverter.ToDouble(parameters[4]);
                        l.InnerConeAngle = StringConverter.ToDouble(parameters[5]);
                        l.OuterConeAngle = StringConverter.ToDouble(parameters[6]);
                        if (parameters.Length > 7)
                        {
                            l.ConstantAttenuation = StringConverter.ToDouble(parameters[7]);
                            l.LinearAttenuation = StringConverter.ToDouble(parameters[8]);
                            l.QuadraticAttenuation = StringConverter.ToDouble(parameters[9]);
                        }
                        return l;
                    }
            }
            throw new ArgumentException("Specified Light (" + light + ") cannot be created");
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AmbientLight WhiteAmbient
        {
            get
            {
                return new AmbientLight(Colors.White);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AmbientLight NonWhiteAmbient
        {
            get
            {
                return new AmbientLight(mix);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DirectionalLight WhiteDirectionalNegZ
        {
            get
            {
                return new DirectionalLight(Colors.White, new Vector3D(0, 0, -1));
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DirectionalLight NonWhiteDirectionalNegZ
        {
            get
            {
                return new DirectionalLight(mix, new Vector3D(0, 0, -1));
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DirectionalLight WhiteDirectionalNegAll
        {
            get
            {
                return new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DirectionalLight NonWhiteDirectionalNegAll
        {
            get
            {
                return new DirectionalLight(mix, new Vector3D(-1, -1, -1));
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PointLight WhitePoint
        {
            get
            {
                PointLight light = new PointLight(Colors.White, new Point3D(0, 0, 4));
                light.Range = 100;
                light.ConstantAttenuation = 1;

                return light;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PointLight NonWhitePoint
        {
            get
            {
                PointLight light = new PointLight(mix, new Point3D(0, 0, 4));
                light.Range = 100;
                light.ConstantAttenuation = 1;

                return light;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PointLight WhitePointBelow
        {
            get
            {
                PointLight light = new PointLight(Colors.White, new Point3D(-5, -2, 5));
                light.Range = 100;
                light.ConstantAttenuation = 1;

                return light;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PointLight NonWhitePointBelow
        {
            get
            {
                PointLight light = new PointLight(mix, new Point3D(-5, -2, 5));
                light.Range = 100;
                light.ConstantAttenuation = 1;

                return light;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static SpotLight WhiteSpot
        {
            get
            {
                SpotLight light = new SpotLight(Colors.White, new Point3D(0, 0, 4), new Vector3D(0, 0, -1), 45, 30);
                light.Range = 100;
                light.ConstantAttenuation = 1;

                return light;
            }
        }

        private static Color mix = Color.FromArgb(255, 143, 47, 239);
    }
}
