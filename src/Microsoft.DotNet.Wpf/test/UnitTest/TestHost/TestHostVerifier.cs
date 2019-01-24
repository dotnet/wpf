using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace TestHost
{
    /// <summary>
    /// Verifies that WPF assemblies are loaded from the configured TestHost.
    /// </summary>
    public class TestHostVerifier
    {
        /// <summary>
        /// All managed WPF assemblies that are a part of Microsoft.WindowsDesktop.App.deps.json
        /// </summary>
        static readonly string[] WpfManagedAssemblies = new string[]
        {
            "WindowsBase",
            "PresentationFramework",
            "PresentationCore",
            "WindowsFormsIntegration",
            "System.Windows.Controls.Ribbon",
            "PresentationFramework.Aero",
            "PresentationFramework.Aero2",
            "PresentationFramework.AeroLite",
            "PresentationFramework.Classic",
            "PresentationFramework.Luna",
            "PresentationFramework.Royale",
            "UIAutomationClient",
            "UIAutomationClientSideProviders",
            "UIAutomationProvider",
            "UIAutomationTypes",
            "PresentationFramework-SystemCore",
            "PresentationFramework-SystemData",
            "PresentationFramework-SystemDrawing",
            "PresentationFramework-SystemXml",
            "PresentationFramework-SystemXmlLinq",
            "System.Windows.Input.Manipulations",
            "System.Windows.Presentation",
            "PresentationUI",
            "ReachFramework",
            "System.Printing",
            "System.Xaml",
        };

        /// <summary>
        /// XUnit output
        /// </summary>
        ITestOutputHelper _output;

        public TestHostVerifier(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Scans the path variable of this process for the configured TestHost.
        /// The first dotnet.exe found is the appropriate host.
        /// </summary>
        /// <returns>The path of the TestHost</returns>
        private string FindTestHostFromPaths()
        {
            var paths = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Process)?.Split(';');

            _output.WriteLine("Searching for TestHost in {0} paths", paths.Length);

            string testHostPath = paths.FirstOrDefault(x => x.Contains("artifacts") && File.Exists(Path.Combine(x, "dotnet.exe")));

            Xunit.Assert.False(string.IsNullOrEmpty(testHostPath), "Could not find TestHost in process path variable");

            return testHostPath;
        }

        /// <summary>
        /// Verifies the load location of each WPF assembly is under the configured TestHost.
        /// </summary>
        [Fact]
        public void VerifyWpfAssemblyLoadLocations()
        {
            string testHostPath = FindTestHostFromPaths();

            _output.WriteLine("Found TestHost at: {0}", testHostPath);

            _output.WriteLine("Verifying WPF managed assembly load locations");

            foreach (var asm in WpfManagedAssemblies)
            {
                var loadedAsm = Assembly.Load(asm);

                Xunit.Assert.True(loadedAsm != null, $"Could not load WPF managed assembly {asm}");

                _output.WriteLine("\t Loaded {0}", asm);

                Xunit.Assert.True(loadedAsm.Location.StartsWith(testHostPath), $"Assembly {asm} loaded from: {loadedAsm.Location} expected under: {testHostPath}");

                _output.WriteLine("\t\t Verified Path: {0}", loadedAsm.Location);
            }
        }
    }
}
