// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Threading;
using System.Reflection;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Launch tests defined in an assembly. 
    /// </summary>
    public class TestLauncher
    {
        /// <summary>
        /// Set the TestAssembly, launch tests in the test assembly based on args, and 
        /// revert the TestAssembly value to original value. 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="testAssembly"></param>
        public static void Launch(string[] args, Assembly testAssembly)
        {
            //Set TestAssembly. 
            Assembly originalTestAssembly = TestAssembly;
            TestAssembly = testAssembly;

            TokenList tokens = new TokenList(args);
            foreach (Object o in tokens)
            {
                LogHeader(o.ToString());
            }
            string runAll = tokens.GetValue("RunAll");
            string runAllScripts = tokens.GetValue("RunAllScripts");
            string dontCatch = tokens.GetValue("DontCatch");
            string logTime = tokens.GetValue("LogTime");
            string script = tokens.GetValue("script");
            string className = tokens.GetValue("Class");

            if (tokens.ContainsKey("?") || tokens.ContainsKey("help") || tokens.ContainsKey("Help"))
            {
                PrintHelp();
                return;
            }

            //Catch Exceptions on dispatcher, and rethrow on main thread
            if (dontCatch == null)
            {
                RenderingTest.catchExceptionsOnDispatcherThread = true;
            }
            else
            {
                RenderingTest.catchExceptionsOnDispatcherThread = !StringConverter.ToBool(dontCatch);
            }
            if (logTime != null)
            {
                LogTime = StringConverter.ToBool(logTime);
            }

            GraphicsTestLoader tester = null;
            if (runAll != null && StringConverter.ToBool(runAll))
            {
                tester = new RunAllLoader(tokens);
            }
            else if (runAllScripts != null && StringConverter.ToBool(runAllScripts))
            {
                tester = new RunAllLoader(tokens, false);
            }
            else if (script == null && className == null)
            {
                PrintHelp();
                return;
            }
            else
            {
                tester = new RunScriptLoader(tokens, script);
            }

            //Intercept and log any uncaught exceptions
            try
            {
                tester.RunMyTests();
            }

            catch (Exception e)
            {
                TestLog.Current.Result = TestResult.Fail;
                TestLog.Current.LogStatus("Test Failure - Unhandled Exception Caught");
                TestLog.Current.LogEvidence(e.ToString());
                Logger.LogFinalResults(1, 1);
            }

            if (RenderingTest.application != null)
            {
                RenderingTest.application.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new DispatcherOperationCallback(Shutdown),
                        null
                        );
            }

            //revert TestAssembly. 
            TestAssembly = originalTestAssembly;
        }

        /// <summary/>
        public static void PrintHelp()
        {
            string exe = Environment.CommandLine;

            originalColor = Console.ForegroundColor;     // Make sure we remember the original color
            columnOffset = 33;

            Console.WriteLine();
            LogHeader(string.Format("{0} [ options ]", exe), ConsoleColor.White);

            LogHeader("Mandatory Parameters: (you must use ONE of these to run a test)");
            LogParameter("Class", "ClassName", "Run the test code written for the ClassName object");
            LogParameter("script", "Filename.xml", "Run the Variations described in Filename.xml");
            LogParameter("RunAllScripts", "", "Run the tests for all valid .xml files in the current directory");
            LogParameter("RunAll", "", "Run all .xml files in the current directory", "and all test code compiled into the test harness");
            Console.WriteLine();

            LogHeader("Syntax for optional parameters (everything is case-sensitive):");
            LogParameter("Param", "type", "(if \"type\" is bool and you want the value to be true,", "you may omit the =true part of the parameter)");
            Console.WriteLine();

            LogHeader("Optional Parameters:");
            LogParameter("goto", "int", "Run only a single variation defined in an Xml script", "(if a script has 10 variations, valid values are 1-10)");
            LogParameter("DontCatch", "bool", "Don't catch Dispatcher exceptions and rethrow them on the main thread");
            LogParameter("LogTime", "bool", "Print out the total elapsed time when a new Variation starts");
            LogParameter("NewWindowPerVariation", "bool", "Create a new window for each Variation in the Xml script");
            LogParameter("RenderingMode", "string", "Force a rendering mode", "Valid values are: Hardware, Software, HardwareReference");
            Console.WriteLine();

            LogHeader("Per-Variation Parameters:");
            LogParameter("MaxLogFails", "int", "Set the maximum number of failures whose error messages are logged");
            LogParameter("Priority", "int", "Used by some unit tests to decide whether or not to test invalid parameters");
            LogParameter("Fail", "bool", "Used by some unit tests to force everything to fail (verifies logging)");
            LogParameter("LogFilePrefix", "string", "Give a unique name to the log files");
            LogParameter("Description", "string", "Log this when a variation begins");
            LogParameter("Expectation", "string", "Log this when a variation begins");
            Console.WriteLine();

            LogHeader("RenderingTest Parameters:");
            LogParameter("RenderToImage", "bool", "Render the Variation content into an ImageBrush  -  default: false");
            LogParameter("WindowPosition", "Point", "Set the window's top left corner  -  default: 0,0 or system positioned");
            LogParameter("WindowWidth", "double", "Set the window's width  -  default: 200");
            LogParameter("WindowHeight", "double", "Set the window's height  -  default: 200");
            LogParameter("UseViewport3D", "bool", "Use Viewport3D instead of Viewport3DVisual  -  default: false");
            LogParameter("BackgroundColor", "Color", "Set the background color of the window  -  default: 255,255,255,255");
            LogParameter("ViewportRect", "Rect", "Set the Viewport3D[Visual]'s rendering area  -  default: window size");
            LogParameter("VerifySerialization", "bool", "Set whether we verify round-trip serialization of a scene  -  default: false");
            LogParameter("FlowDirection", "string", "Set the FlowDirection on the Viewport3D[Visual]", "Valid values are: LeftToRight, RightToLeft");
            Console.WriteLine();

            columnOffset = 44;
            LogHeader("VisualVerificationTest Parameters:");
            LogParameter("ForceSave", "bool", "Save RenderBuffers for each scene rendered (verify SceneRenderer)");
            LogParameter("SaveTextures", "bool", "Save the textures that TextureGenerator creates");
            LogParameter("SaveXamlRepro", "bool", "Save triangles causing failed pixels into a Xaml file");
            LogParameter("SaveExpectedFrameBuffer", "bool", "Enable/Disable saving SceneRenderer's Framebuffer");
            LogParameter("SaveExpectedToleranceBuffer", "bool", "Enable/Disable saving SceneRenderer's Tolerance buffer");
            LogParameter("SaveExpectedZBuffer", "bool", "Enable/Disable saving SceneRenderer's Z buffer");
            LogParameter("SaveDiffFrameBuffer", "bool", "Enable/Disable saving ( Actual - Expected Framebuffer ) image");
            LogParameter("SaveDiffToleranceBuffer", "bool", "Enable/Disable saving the enhanced difference image");
            LogParameter("SaveDiffZBuffer", "bool", "Don't really remember what this one is");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("        - using ForceSave & SaveXamlRepro will give you a Xaml containing the entire scene");
            Console.WriteLine();
            Console.WriteLine("        - SaveExpected* and SaveDiff* are all enabled by default,");
            Console.WriteLine("          but images are only saved when failures occur unless ForceSave is true");
            Console.ForegroundColor = originalColor;
            Console.WriteLine();
            Console.WriteLine();

            LogHeader("Rendering Tolerances:");
            LogParameter("DefaultColorTolerance", "Color", "Ignore color differences of this much per pixel");
            LogParameter("SilhouetteEdgeTolerance", "double", "Ignore pixels this close to a mesh edge");
            LogParameter("PixelToEdgeTolerance", "double", "Ignore pixels this close to a triangle edge");
            LogParameter("TextureLookUpTolerance", "double", "Include pixels this far away in the color tolerance value");
            LogParameter("ZBufferTolerance", "double", "Ignore pixels this close to each other in the z buffer");
            LogParameter("ViewportClippingTolerance", "double", "Ignore pixels this close to the viewport Rect");
            LogParameter("LightingRangeTolerance", "double", "Ignore pixels this close to the light range cutoff");
            LogParameter("SpotLightAngleTolerance", "double", "Leave this alone");
            LogParameter("SpecularLightDotProductTolerance", "double", "Leave this alone");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Each RenderingTest has its own parameter requirements.  Look at TestObjects.cs");
            Console.WriteLine("    and FactoryParser.cs for more information on these values.");
            Console.ForegroundColor = originalColor;
            Console.WriteLine();
            Console.WriteLine();

            LogHeader("Usage examples:");
            LogHeader(string.Format("{0} /Class=Point3DTest", exe), ConsoleColor.White, false);
            LogHeader(string.Format("{0} /script=AmbientLight.xml", exe), ConsoleColor.White, false);
            LogHeader(string.Format("{0} /script=Visual3D.xml /goto=2 /ForceSave", exe), ConsoleColor.White, false);
            LogHeader(string.Format("{0} /RunAllScripts", exe), ConsoleColor.White, false);
            LogHeader(string.Format("{0} /RunAll", exe), ConsoleColor.White, false);
            Console.WriteLine();
        }

        private static void LogHeader(string header)
        {
            LogHeader(header, true);
        }

        private static void LogHeader(string header, bool appendNewline)
        {
            //Console.CursorLeft = 4;
            Console.WriteLine(header);
            if (appendNewline)
            {
                Console.WriteLine();
            }
        }

        private static void LogHeader(string header, ConsoleColor color)
        {
            LogHeader(header, color, true);
        }

        private static void LogHeader(string header, ConsoleColor color, bool appendNewline)
        {
            Console.ForegroundColor = color;
            LogHeader(header, appendNewline);
            Console.ForegroundColor = originalColor;
        }

        private static void LogParameter(string name, string type, string explanation, params string[] additionalExplanations)
        {
            //Console.CursorLeft = 8;
            Console.Write("/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(name);
            Console.ForegroundColor = originalColor;
            if (type.Length != 0)
            {
                Console.Write("=");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("<{0}>", type);
                Console.ForegroundColor = originalColor;
            }
            int offset = Math.Max(columnOffset, name.Length + type.Length + 13);
            //Console.CursorLeft = offset;
            Console.Write("- " + explanation);
            foreach (string more in additionalExplanations)
            {
                Console.WriteLine();
                //Console.CursorLeft = offset + 2;
                Console.Write(more);
            }
            Console.WriteLine();
        }

        private static object Shutdown(object notUsed)
        {
            PT.Trust(RenderingTest.application).Shutdown();
            return null;
        }

        /// <summary/>
        public static bool LogTime = false;
        /// <summary/>
        public static DateTime BeginTime = DateTime.Now;
        private static ConsoleColor originalColor;
        private static int columnOffset;

        /// <summary>
        /// Assembly from which tests would be loaded. 
        /// </summary>
        internal static Assembly TestAssembly { get; set; }

    }
}
