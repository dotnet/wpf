// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Data holder/Test creator
    /// </summary>
    public class Variation
    {

        /// <summary/>
        public Variation(TokenList tokens)
            : this(tokens, false)
        {
        }

        private Variation(TokenList tokens, bool isGlobalVariation)
        {
            localParameters = tokens;
            ParseParameters(isGlobalVariation);
            id = ++totalVariations;
        }

        private void ParseParameters(bool isGlobalVariation)
        {
            testClass = this["Class"];

            if (testClass == null && !isGlobalVariation)
            {
                throw new ApplicationException("You must specify Class=");
            }

            string rti = this["RenderToImage"];
            string pos = this["WindowPosition"];
            string width = this["WindowWidth"];
            string height = this["WindowHeight"];
            string vp3d = this["UseViewport3D"];
            string color = this["BackgroundColor"];
            string vp = this["ViewportRect"];
            renderingMode = this["RenderingMode"];
            string fld = this["FlowDirection"];

            // double.NaN means to use system defaults
            windowPosition = (pos == null) ? new Point(double.NaN, double.NaN) : StringConverter.ToPoint(pos);

            windowWidth = (width == null) ? 200 : StringConverter.ToInt(width);
            windowHeight = (height == null) ? 200 : StringConverter.ToInt(height);

            // If no viewport rect specified, use the window client area rect
            viewportRect = (vp == null) ? new Rect(0, 0, windowWidth, windowHeight) : StringConverter.ToRect(vp);

            bool defaultUseVP3D = (testClass == null) ? false : testClass.Contains("Xaml");
            useViewport3D = (vp3d == null) ? defaultUseVP3D : StringConverter.ToBool(vp3d);
            backgroundColor = (color == null) ? RenderingWindow.DefaultBackgroundColor : StringConverter.ToColor(color);
            renderToImage = (rti == null) ? false : StringConverter.ToBool(rti);
            flowDirection = (fld == null) ? FlowDirection.LeftToRight :
                    (FlowDirection)Enum.Parse(typeof(FlowDirection), fld);

            if (!isGlobalVariation)
            {
                SetTestType();
            }
        }

        private Type FindTestTypeInAssembly(Assembly assembly)
        {
            if(assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            Type[] testTypes = assembly.GetTypes();

            foreach (Type t in testTypes)
            {
                if ((t.Name == testClass)&&(t.Namespace.Contains("Graphics")))
                {
                    return t;
                }
            }
            return null;
        }

        private void SetTestType()
        {
            Type t;
            
            //Search test classes from TestRuntime code assembly
            if ((t = FindTestTypeInAssembly(Assembly.GetAssembly(typeof(Variation)))) != null)
            {
                testType = t;
            }
                
            //Search tests classes from launching feature team assembly - needed when running via driver
            else if ((t = FindTestTypeInAssembly(TestLauncher.TestAssembly)) != null)
            {
                testType = t;
            }

            else
            {
                throw new ApplicationException("Could not locate class \"" + testClass + "\"");
            }
        }

        /// <summary/>
        public string this[string s]
        {
            get
            {
                if (localParameters.ContainsKey(s))
                {
                    return localParameters[s];
                }
                else if (globalParameters.ContainsKey(s))
                {
                    return globalParameters[s];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary/>
        public bool this[string s, bool defaultValue]
        {
            get
            {
                string value = this[s];
                return (value != null) ? StringConverter.ToBool(value) : defaultValue;
            }
        }

        /// <summary/>
        public void AssertExistenceOf(params object[] parameters)
        {
            string missingParameters = string.Empty;
            foreach (string parameter in parameters)
            {
                if (this[parameter] == null)
                {
                    missingParameters += parameter + "=, ";
                }
            }
            if (missingParameters.Length != 0)
            {
                missingParameters = missingParameters.Substring(0, missingParameters.Length - 2);  // Remove the trailing ", "
                string msg = string.Format("The following required parameters are missing in Variation {0}: {1}", id, missingParameters);
                throw new ApplicationException(msg);
            }
        }

        /// <summary/>
        public void AssertAbsenceOf(params object[] parameters)
        {
            string offendingParameters = string.Empty;
            foreach (string parameter in parameters)
            {
                if (this[parameter] != null)
                {
                    offendingParameters += parameter + "=, ";
                }
            }
            if (offendingParameters.Length != 0)
            {
                offendingParameters = offendingParameters.Substring(0, offendingParameters.Length - 2);  // Remove the trailing ", "
                string msg = string.Format(
                                    "The following parameters must be removed from Variation {0}: {1}\r\n" +
                                    "They conflict with another parameter - probably a built-in default object like Camera=, Light=, etc",
                                    id,
                                    offendingParameters
                                    );

                throw new ApplicationException(msg);
            }
        }

        internal CoreGraphicsTest CreateTest()
        {
            CoreGraphicsTest test = (CoreGraphicsTest)Activator.CreateInstance(testType, null);

            if (test == null)
            {
                throw new ApplicationException("Could not create test class: " + testClass);
            }

            if (test is RenderingTest)
            {
                RenderingTest.Invoke(DispatcherPriority.Send, InitRenderingTest, test);
            }
            else
            {
                test.Init(this);
            }

            return test;
        }

        private object InitRenderingTest(object renderingTest)
        {
            RenderingTest test = renderingTest as RenderingTest;
            if (test == null)
            {
                throw new ApplicationException("Don't create Application and Dispatcher for non-rendering tests");
            }

            test.Init(this);
            return null;
        }

        internal static void SetGlobalParameters(TokenList tokens)
        {
            globalParameters = tokens;

            string fresh = tokens.GetValue("NewWindowPerVariation");
            newWindowPerVariation = (fresh == null) ? false : StringConverter.ToBool(fresh);
            if (newWindowPerVariation)
            {
                // Global variation will not be used. Do not create it.
                totalVariations = 0;
                globalVariation = null;
            }
            else
            {
                // Create a special variation "0" with all the global values.
                totalVariations = -1;
                globalVariation = new Variation(tokens, true);
            }
        }

        /// <summary/>
        public int WindowWidth { get { return windowWidth; } }

        /// <summary/>
        public int WindowHeight { get { return windowHeight; } }

        /// <summary/>
        public Size WindowSize { get { return new Size(windowWidth, windowHeight); } }

        /// <summary/>
        public int DpiScaledWindowWidth { get { return (int)MathEx.ConvertToAbsolutePixelsX(windowWidth); } }

        /// <summary/>
        public int DpiScaledWindowHeight { get { return (int)MathEx.ConvertToAbsolutePixelsY(windowHeight); } }

        /// <summary/>
        public Rect ViewportRect { get { return viewportRect; } }

        /// <summary/>
        public string TestClass { get { return testClass; } }

        /// <summary/>
        public bool UseViewport3D { get { return useViewport3D; } }

        /// <summary/>
        public int ID { get { return id; } }

        /// <summary/>
        public Color BackgroundColor { get { return backgroundColor; } }

        /// <summary/>
        public string RenderingMode { get { return renderingMode; } }

        /// <summary/>
        public bool RenderToImage { get { return renderToImage; } }

        /// <summary/>
        public Point WindowPosition
        {
            get { return windowPosition; }
            set { windowPosition = value; }
        }

        /// <summary/>
        public bool IsWindowPositionValid
        {
            get { return MathEx.NotEquals(windowPosition, new Point(double.NaN, double.NaN)); }
        }

        /// <summary/>
        public FlowDirection FlowDirection { get { return flowDirection; } }

        internal static bool NewWindowPerVariation { get { return newWindowPerVariation; } }
        internal static Variation GlobalParameters { get { return globalVariation; } }
        internal static int TotalVariations
        {
            get { return totalVariations; }
            set { totalVariations = value; }
        }

        private static Variation globalVariation;
        private static int totalVariations;
        private static bool newWindowPerVariation;
        private static TokenList globalParameters;   // Anything left over that we don't special-case
        private Type testType;
        private TokenList localParameters;
        private int id;
        private Color backgroundColor;
        private Rect viewportRect;
        private Point windowPosition;
        private int windowWidth;
        private int windowHeight;
        private string testClass;
        private bool useViewport3D;
        private string renderingMode;
        private bool renderToImage;
        private FlowDirection flowDirection;
    }
}

