// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal.WindowsBase;
using MS.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Windows
{
    /// <summary>
    /// This class attempts to verify that core WPF binaries are all loaded from the same
    /// location.  Due to the fact that WPF can load binaries in different orders, this class
    /// needs to be called from all of the core managed binaries in static constructors of
    /// commonly used classes.
    /// 
    /// This can also be used to verify specific additional binaries as needed (say if they are
    /// optionally loaded).
    /// </summary>
    /// <remarks>
    /// If the binaries fail to load from the same location an exception is thrown as we cannot 
    /// guarantee there won't be difficult to debug issues.
    /// </remarks>
    internal class WpfDllVerifier
    {
        private const string PackageRootMarker = @"\runtimes\";

        /// <summary>
        /// Names of DLLs to consistency check.  Once a DLL is checked it will be removed from this list.
        /// </summary>
        private static List<string> s_CoreDllNames = new List<string>()
        {
            ExternDll.PresentationCore,
            ExternDll.PresentationFramework,
            ExternDll.WindowsBase,
            ExternDll.WpfGfx,
            ExternDll.PresentationNativeDll
        };

        /// <summary>
        /// The path to check against, filled from the first caller's path.
        /// </summary>
        private static string s_AssemblyLoadPath = null;

        /// <summary>
        /// The root of the NuGet package being used to load WPF.
        /// </summary>
        private static string s_PackageRootPath = null;

        /// <summary>
        /// Returns the package root of the path used to load the assembly.
        /// </summary>
        /// <param name="path">The path to extract the package root from.</param>
        /// <returns>The package root if found, null otherwise.</returns>
        private static string GetPackageRootFromPath(string path)
        {
            int packageIndex = path.IndexOf(PackageRootMarker);

            if (packageIndex != -1)
            {
                return path.Substring(0, packageIndex);
            }

            return null;
        }

        /// <summary>
        /// Verifies the core set of DLLs is loaded from the same location.  Also allows specification of
        /// additional DLLs that may be verified in specific circumstances.
        /// 
        /// If all the core DLLs are verified and no additional DLLs are specific, this returns immediately.
        /// </summary>
        /// <param name="additionalDlls">A set of DLL names to verify along with the core DLL set.</param>
        /// <returns>The list of DLLs that remain unchecked.</returns>
        internal static List<string> VerifyWpfDllSet(params string[] additionalDlls)
        {
            // Don't check again once all needed DLLs have been checked.
            if (s_CoreDllNames.Count == 0 && additionalDlls.Length == 0)
            {
                return s_CoreDllNames;
            }

            // Set this so that subsequent calls to this function will check against the path of the initial caller.
            if (s_AssemblyLoadPath == null)
            {
                // The assembly we are verifying from.
                var callingAssembly = Assembly.GetCallingAssembly();
                var callingAssemblyName = callingAssembly.ManifestModule.Name.Split('.').First();
                s_AssemblyLoadPath = Path.GetDirectoryName(Path.GetFullPath(callingAssembly.Location));
                s_PackageRootPath = GetPackageRootFromPath(s_AssemblyLoadPath);
            }

            var consistencyErrors = new List<Tuple<string, string>>();

            // Check whatever is not yet done, plus any additional specified DLLs.
            var dllsToCheck = new List<string>(s_CoreDllNames);
            dllsToCheck.AddRange(additionalDlls);

            // Loop through all modules loaded in the process, matching against the list of DLLs to check.
            foreach (ProcessModule procModule in Process.GetCurrentProcess().Modules)
            {
                var dllName = dllsToCheck.FirstOrDefault(x => procModule.ModuleName.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(dllName)) continue;

                // First get the full path as that ensures there are no 8.3 shortened names in the path.
                string dllLocation = Path.GetDirectoryName(Path.GetFullPath(procModule.FileName));

                // Any matched DLL should come from the same location as the calling assembly.  For verification, we assume
                // the calling assembly has been loaded from the correct path.  If we're loading from a NuGet package, just 
                // ensure all binaries are loading from the same package.
                if (dllLocation != s_AssemblyLoadPath
                    && (string.IsNullOrEmpty(s_PackageRootPath) || s_PackageRootPath != GetPackageRootFromPath(dllLocation)))
                {
                    consistencyErrors.Add(new Tuple<string, string>(dllName, dllLocation));
                }

                s_CoreDllNames.Remove(dllName);
                dllsToCheck.Remove(dllName);
            }

            if (consistencyErrors.Count > 0)
            {
                var message = new StringBuilder();

                message.AppendLine(SR.Get(SRID.WpfDllConsistencyErrorHeader, s_AssemblyLoadPath));

                foreach (var error in consistencyErrors)
                {
                    message.AppendLine(SR.Get(SRID.WpfDllConsistencyErrorData, error.Item1, error.Item2));
                }

                // If you see this error, examine the set of DLLs reported, then examine the set of DLLs contained in the initial list
                // of core DLLs.  You should be able to detect whether the calling assembly path or the path of the checked DLL is incorrect
                // by seeing where the majority of DLLs are loaded from.
                throw new InvalidProgramException(message.ToString());
            }

            return dllsToCheck;
        }
    }
}
