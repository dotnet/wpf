using System.Diagnostics;
using System.Xml.Linq;

namespace PresentationCore.Tests.TargetFramework
{
    public class OlderTFMTests
    {
        [Theory]
        [InlineData("net10.0")]
        [InlineData("net9.0")]
        [InlineData("net8.0")]
        [InlineData("net472")]
        [InlineData("net48")]
        [InlineData("net481")]
        public void OlderTFMTestFX(string targetFramework)
        {
            if (targetFramework != null)
            {
                Assert.True(true);
            }

            Console.WriteLine($"Starting test with target framework: {targetFramework}");
            try
            {
                targetFramework.Should().NotBeNullOrEmpty();
                CreateProject("WPFSampleApp", targetFramework);
                // Define the project file path
                string projectFile = @"D:\\FORK\\Harshita\\TFM\\wpf\\src\\Microsoft.DotNet.Wpf\\src\\WPFSampleApp\\WPFSampleApp\WPFSampleApp\\WPFSampleApp.csproj";

                if (!File.Exists(projectFile))
                {
                    Console.WriteLine($"Project file not found: {projectFile}");
                    throw new FileNotFoundException($"Project file not found: {projectFile}");
                }
                Console.WriteLine($"Project file found: {projectFile}");
                string publishPath = @"D:\\FORK\\Harshita\\TFM\\wpf\\src\\Microsoft.DotNet.Wpf\\src\\WPFSampleApp\\WPFSampleApp\\WPFSampleApp\\publish";
                RunDotnetPublish(projectFile, publishPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

        private static void CreateProject(string projectName, string? targetFramework)
        {
            
            // Define project path
            string rootPath = @"D:\\FORK\\Harshita\\TFM\\wpf\\src\\Microsoft.DotNet.Wpf\\src\\WPFSampleApp";
            if (!(Directory.Exists(rootPath)))
            {
                Directory.CreateDirectory(rootPath);
            }

            string projectPath = Path.Combine(rootPath, projectName);

            // Create project directory
            Directory.CreateDirectory(projectPath);

            // Command to create a new project
            string createProjectCommand = $"dotnet new wpf -n {projectName} --framework {targetFramework}";

            // Start the process to create the project
            ProcessStartInfo startInfo = new("cmd", "/c " + createProjectCommand)
            {
                WorkingDirectory = projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            // Check if startInfo is null
            if (startInfo == null)
            {
                Console.WriteLine("ProcessStartInfo is null, cannot start the process.");
                return;
            }

            using (Process? process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start the process.");
                    return;
                }
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                Console.WriteLine(output);
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Error: {error}");
                }
            }
            // Path to the .csproj file
            string csprojFilePath = Path.Combine(projectPath, $"{Path.GetFileName(projectPath)}.csproj");

            if (File.Exists(csprojFilePath))
            {
                // Load the .csproj file as an XML document
                XDocument csprojDoc = XDocument.Load(csprojFilePath);

                // Check if the WindowsBase reference exists, if not, add it
                var itemGroup = csprojDoc.Descendants("ItemGroup").FirstOrDefault();
                if(itemGroup != null)
                {
                    itemGroup.Add(new XElement("Reference", new XAttribute("Include", "$(WpfSourceDir)WindowsBase\\WindowsBase.csproj")));
                }              

                // Save the changes back to the .csproj file
                csprojDoc.Save(csprojFilePath);
            }
            Console.WriteLine($"Project '{projectName}' created with target framework '{targetFramework}'.");
         
        }

        private static void RunDotnetPublish(string projectPath, string publishPath)
        {
            // Set up the process start information to run the 'dotnet publish' command
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{projectPath}\" -c Release -r win-x64 --self-contained -o \"{publishPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                //return false;
            }

            // Read output and error streams
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            // Optionally log output and errors
            Console.WriteLine(output);
            Console.WriteLine(error);

            //return process.ExitCode == 0; // Return true if the exit code is 0 (success)
        }



    }
}
