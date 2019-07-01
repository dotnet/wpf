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
    /// stock collection of well-known materials
    /// </summary>
    public class MaterialFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material MakeMaterial(string material)
        {
            switch (material)
            {
                case "Default": return Default;
                case "DefaultSpecular": return DefaultSpecular;
                case "DefaultEmissive": return DefaultEmissive;
                case "CompoundMaterial": return CompoundMaterial;
                case "GroupNullChildren": return GroupNullChildren;
                case "Null": return null;
            }

            return MakeMaterial(material, "Diffuse", 0);
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material MakeMaterial(string material, string type)
        {
            return MakeMaterial(material, type, 0);
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material MakeMaterial(string material, string type, double specularPower)
        {
            switch (type)
            {
                case "Specular":
                    return new SpecularMaterial(BrushFactory.MakeBrush(material), specularPower);
                case "Emissive":
                    return new EmissiveMaterial(BrushFactory.MakeBrush(material));
                case "Diffuse":
                    return new DiffuseMaterial(BrushFactory.MakeBrush(material));
                default:
                    throw new ArgumentException(string.Format(
                            "Specified material ({0}) of type ({1}) could not be created.", material, type));
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material GetRandomMaterial(int seed, bool opaque, bool allowNulls)
        {
            Random rand = new Random(seed);

            int brushTypeCount, materialTypeCount;
            if (allowNulls)
            {
                brushTypeCount = 7;
                materialTypeCount = 5;
            }
            else
            {
                brushTypeCount = 6;
                materialTypeCount = 4;
            }

            // Create Brush
            Brush brush = null;
            switch (rand.Next() % brushTypeCount)
            {
                case 0:
                    brush = BrushFactory.GetRandomLinearGradientBrush(rand.Next(), opaque);
                    break;

                case 1:
                    brush = BrushFactory.GetRandomRadialGradientBrush(rand.Next(), opaque);
                    break;

                case 2:
                    brush = BrushFactory.GetRandomImageBrush(rand.Next(), opaque);
                    break;

                case 3:
                    brush = BrushFactory.GetRandomDrawingBrush(rand.Next(), opaque);
                    break;

                case 4:
                    brush = BrushFactory.GetRandomVisualBrush(rand.Next(), opaque);
                    break;

                case 5:
                    brush = BrushFactory.GetRandomSolidColorBrush(rand.Next(), opaque);
                    break;

                default:
                    // null brush
                    break;
            }
            brush = BrushFactory.GetRandomDrawingBrush(rand.Next(), opaque);

            // Create material
            Material material = null;
            switch (rand.Next() % materialTypeCount)
            {
                case 0:
                    material = new DiffuseMaterial(brush);
                    break;

                case 1:
                    material = new EmissiveMaterial(brush);
                    break;

                case 2:
                    material = new SpecularMaterial(brush, rand.NextDouble() * 100.0);
                    break;

                case 3:
                    material = new MaterialGroup();
                    int childCount = rand.Next(0, 4);
                    for (int i = 0; i < childCount; i++)
                    {
                        ((MaterialGroup)material).Children.Add(GetRandomMaterial(rand.Next(), opaque, allowNulls));
                    }
                    break;

                default:
                    // null material
                    break;
            }
            return material;
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material Black
        {
            get
            {
                return new DiffuseMaterial(Brushes.Black);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material White
        {
            get
            {
                return new DiffuseMaterial(Brushes.White);
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material Red
        {
            get
            {
                return new DiffuseMaterial(Brushes.Red);
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material Green
        {
            get
            {
                return new DiffuseMaterial(Brushes.Green);
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material Blue
        {
            get
            {
                return new DiffuseMaterial(Brushes.Blue);
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material Gray
        {
            get
            {
                return new DiffuseMaterial(Brushes.Gray);
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material Yellow
        {
            get
            {
                return new DiffuseMaterial(Brushes.Yellow);
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material Default
        {
            get
            {
                return new DiffuseMaterial(new SolidColorBrush(
                        Color.FromArgb(0xFF, 0x3F, 0x7F, 0xAF)));
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material DefaultEmissive
        {
            get
            {
                return new EmissiveMaterial(new SolidColorBrush(Colors.DarkGray));
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material DefaultSpecular
        {
            get
            {
                return new SpecularMaterial(new SolidColorBrush(Colors.White), 20);
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material CompoundMaterial
        {
            get
            {
                MaterialGroup mg = new MaterialGroup();
                mg.Children = new MaterialCollection();
                mg.Children.Add(Default);
                mg.Children.Add(DefaultSpecular);
                mg.Children.Add(DefaultEmissive);
                return mg;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Material GroupNullChildren
        {
            get
            {
                MaterialGroup group = new MaterialGroup();
                group.Children = null;
                return group;
            }
        }
    }
}

