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
    /// Make a ModelVisual3D
    /// </summary>

    public class ModelVisual3DFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D NoKids
        {
            get
            {
                return new ModelVisual3D();
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D OneKid
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                v.Children.Add(new ModelVisual3D());
                return v;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D FourKids
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                for (int n = 0; n < 4; n++)
                {
                    v.Children.Add(new ModelVisual3D());
                }
                return v;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D OneKidOneGrandkid
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                v.Children.Add(OneKid);
                return v;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D TwoKidsTwoGrandkids
        {
            get
            {
                ModelVisual3D v = OneKidOneGrandkid;
                v.Children.Add(OneKid);
                return v;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D FourKidsSixteenGrandkids
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                for (int n = 0; n < 4; n++)
                {
                    v.Children.Add(FourKids);
                }
                return v;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D SixteenDeep
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                for (int n = 0; n < 15; n++)
                {
                    // build the tree from the bottom up
                    ModelVisual3D temp = new ModelVisual3D();
                    temp.Children.Add(v);
                    v = temp;
                }
                return v;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D SixteenKids
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                for (int n = 0; n < 16; n++)
                {
                    v.Children.Add(new ModelVisual3D());
                }
                return v;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D Unbalanced
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                v.Children.Add(NoKids);
                v.Children.Add(SixteenDeep);
                v.Children.Add(FourKidsSixteenGrandkids);
                return v;
            }
        }
    }
}
