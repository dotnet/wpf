// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace System.Windows.Controls
{
    public class OlderTFMTests
    {
        private static string s_projectPath = "";
        private string _projectName = "";
        private static string s_projectFile = "";

        [Theory]
        [InlineData("net9.0")]
        [InlineData("net8.0")]
        [InlineData("net47")]
        [InlineData("net471")]
        [InlineData("net472")]
        [InlineData("net48")]
        [InlineData("net481")]
        public void OlderTFMTestFX(string targetFramework)
        {
            if (targetFramework != null)
            {
                Assert.True(true);
                Console.WriteLine($"Starting test with target framework: {targetFramework}");
                try
                {
                    // Define project name
                    _projectName = "WPFSampleApp" + targetFramework;

                    // Create a new project
                    CreateProject(targetFramework, _projectName);

                    if (targetFramework.Contains("net48") || targetFramework.Contains("net481") || targetFramework.Contains("net472") || targetFramework.Contains("net471") || targetFramework.Contains("net47"))
                    {
                        ChangeTargetFramework(s_projectFile, targetFramework);
                    }
                    // Publish the project
                    string publishPath = Path.Combine(s_projectPath, "publish");
                    RunDotnetPublish(publishPath);

                    // Launch the WPF application
                    bool check = LaunchWPFApp(publishPath, _projectName);
                    if (check)
                    { Assert.True(true); }
                    else
                    { Assert.True(false); }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception: {e}");
                    throw;

                }
            }
            else
            {
                Console.WriteLine($"Missing or Incorrect target framework: {targetFramework}");
                Assert.True(false);
            }
        }

        private static void CreateProject(string targetFramework, string projectName)
        {
            // Define project path
            string? defaultDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (defaultDrive == null)
            {
                Console.WriteLine("Failed to get the default drive.");
                return;
            }

            string rootPath = Path.Combine(defaultDrive, "TFMProjects\\" + projectName);

            // Clean up the directory if it exists
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, true);
            }

            // Define project path
            s_projectPath = Path.Combine(rootPath, projectName);

            // Create project directory
            Directory.CreateDirectory(s_projectPath);

            // Command to create a new project
            string createProjectCommand;
            if (targetFramework is "net48" or "net481" or "net472" or "net471" or "net47")
            {
                createProjectCommand = $"dotnet new wpf -n {projectName}";
            }
            else
            {
                createProjectCommand = $"dotnet new wpf -n {projectName} --framework {targetFramework}";
            }

            // Start the process to create the project
            ProcessStartInfo startInfo = new("cmd", "/c " + createProjectCommand)
            {
                WorkingDirectory = rootPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("Failed to start the process.");
                return;
            }
            process.WaitForExit();

            // Read the output and error
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            Console.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Error: {error}");
            }

            // Define project file path
            s_projectFile = Path.Combine(s_projectPath, $"{projectName}.csproj");

            // Verify if the projects are created           
            Assert.True(File.Exists(s_projectFile));
        }

        private static void RunDotnetPublish(string publishPath)
        {
            // Create project directory
            Directory.CreateDirectory(publishPath);

            if (Directory.Exists(publishPath))
            {
                Assert.True(true);
            }
            else
            {
                Assert.True(false);
            }
            // Set up the process start information to run the 'dotnet publish' command
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"dotnet publish \"{s_projectPath}\" -c Release -r win-x64 -o \"{publishPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                // Read output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine("The publish has log  :--------------------- " + output);
                Console.WriteLine("The publish has error  :--------------------- " + error);
            }

            // Verify if the publish is successful
            if (Directory.Exists(publishPath))
            {
                Console.WriteLine("Publish successfully.");
                Assert.True(true);
            }
            else
            {
                Console.WriteLine("Publish failed.");
                Assert.True(false);
            }
        }

        private static bool LaunchWPFApp(string publishPath, string filenames)
        {
            string exePath = Path.Combine(publishPath, filenames + ".exe");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                }
            };

            // Start the WPF application
            bool processStarted = process.Start();
            if (!processStarted)
            {
                Console.WriteLine($"Failed to start process");
            }

            // Optionally wait for a moment to ensure the app is up
            Thread.Sleep(2000);

            // Close the main window of the process
            if (!process.CloseMainWindow())
            {
                // If the main window could not be closed, kill the process
                process.Kill();
            }
            process.WaitForExit();

            // Check if the process has exited
            Assert.True(process.HasExited);
            return process.ExitCode == 0;

        }

        //Change the TFM to latest version of .Net
        private static void ChangeTargetFramework(string WpfProjectPath, string updateTargetFramework)
        {
            if (!File.Exists(WpfProjectPath))
            {
                Console.WriteLine("Project file not found.");
                return;
            }

            // Load the project file
            XDocument doc = XDocument.Load(WpfProjectPath);
            XElement? propertyGroup = doc.Descendants("PropertyGroup").FirstOrDefault();

            if (propertyGroup != null)
            {
                // Add LangVersion for WPF Framework project type       
                XElement langVersionElement = new XElement("LangVersion", "10.0");
                propertyGroup.Add(langVersionElement);

                XElement? targetFrameworkElement = propertyGroup.Elements("TargetFramework").FirstOrDefault();
                if (targetFrameworkElement != null)
                {
                    targetFrameworkElement.Value = updateTargetFramework;
                }
                else
                {
                    propertyGroup.Add(new XElement("TargetFramework", updateTargetFramework));
                }

                // Save the changes
                doc.Save(WpfProjectPath);
                Console.WriteLine($"TLangVersion added in '{WpfProjectPath}'project .");
            }
            else
            {
                Console.WriteLine("No PropertyGroup found in the project file.");
            }
        }
    }
}
