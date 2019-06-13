// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//-----------------------------------------------------------------------------
//
// Description:
//       TaskHelper implements some common functions for all the WCP tasks
//       to call.
//
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;

using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Resources;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Microsoft.Build.Tasks;
using MS.Utility;
using System.Collections.Generic;


namespace MS.Internal.Tasks
{
    //
    // This is required by the unmanaged API GetGacPath.
    // the value indicates the source of the cached assembly.
    //
    // For our scenario, we just care about GACed assembly.
    //
    [Flags]
    internal enum AssemblyCacheFlags
    {
        ZAP = 1,
        GAC = 2,
        DOWNLOAD = 4,
        ROOT = 8,
        ROOT_EX = 0x80
    }

    #region BaseTask class
    //<summary>
    // TaskHelper which implements some helper methods.
    //</summary>
    internal static class TaskHelper
    {

        //------------------------------------------------------
        //
        //  Internal Helper Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // <summary>
        // Output the Logo information to the logger for a given task
        // (Console or registered Loggers).
        // </summary>
        internal static void DisplayLogo(TaskLoggingHelper log, string taskName)
        {
            string acPath = Assembly.GetExecutingAssembly().Location;
            FileVersionInfo acFileVersionInfo = FileVersionInfo.GetVersionInfo(acPath);

            string avalonFileVersion = acFileVersionInfo.FileVersion;

            log.LogMessage(MessageImportance.Low,Environment.NewLine);
            log.LogMessageFromResources(MessageImportance.Low, SRID.TaskLogo, taskName, avalonFileVersion);
            log.LogMessageFromResources(MessageImportance.Low, SRID.TaskRight);
            log.LogMessage(MessageImportance.Low, Environment.NewLine);
        }


        // <summary>
        // Create the full file path with the right root path
        // </summary>
        // <param name="thePath">The original file path</param>
        // <param name="rootPath">The root path</param>
        // <returns>The new fullpath</returns>
        internal static string CreateFullFilePath(string thePath, string rootPath)
        {
            // make it an absolute path if not already so
            if ( !Path.IsPathRooted(thePath) )
            {
                thePath = rootPath + thePath;
            }

            // get rid of '..' and '.' if any
            thePath = Path.GetFullPath(thePath);

            return thePath;
        }

        // <summary>
        // This helper returns the "relative" portion of a path
        // to a given "root"
        // - both paths need to be rooted
        // - if no match > return empty string
        // E.g.: path1 = C:\foo\bar\
        //       path2 = C:\foo\bar\baz
        //
        //       return value = "baz"
        // </summary>
        internal static string GetRootRelativePath(string path1, string path2)
        {
            string relPath = "";
            string fullpath1;
            string fullpath2;

            string sourceDir = Directory.GetCurrentDirectory() + "\\";

            // make sure path1 and Path2 are both full path
            // so that they can be compared on right base.

            fullpath1 = CreateFullFilePath (path1, sourceDir);
            fullpath2 = CreateFullFilePath (path2, sourceDir);

            if (fullpath2.StartsWith(fullpath1, StringComparison.OrdinalIgnoreCase))
            {
                relPath = fullpath2.Substring (fullpath1.Length);
            }

            return relPath;
        }

        // <summary>
        // Convert a string to a Boolean value using exactly the same rules as
        // MSBuild uses when it assigns project properties to Boolean CLR properties
        // in a task class.
        // </summary>
        // <param name="str">The string value to convert</param>
        // <returns>true if str is "true", "yes", or "on" (case-insensitive),
        // otherwise false.</returns>
        internal static bool BooleanStringValue(string str)
        {
            bool  isBoolean = false;

            if (str != null && str.Length > 0)
            {
                str = str.ToLower(CultureInfo.InvariantCulture);

                if (str.Equals("true") || str.Equals("yes") || str.Equals("on"))
                {
                    isBoolean = true;
                }
            }

            return isBoolean;
        }


        // <summary>
        // return a lower case string
        // </summary>
        // <param name="str"></param>
        // <returns></returns>
        internal static string GetLowerString(string str)
        {
            string lowerStr = null;

            if (str != null && str.Length > 0)
            {
                lowerStr = str.ToLower(CultureInfo.InvariantCulture);
            }

            return lowerStr;
        }

        //
        // Check if the passed string stands for a valid culture name.
        //
        internal static bool IsValidCultureName(string name)
        {

            bool  bValid = true;

            try
            {
                //
                // If the passed name is empty or null, we still want
                // to treat it as valid culture name.
                // It means no satellite assembly will be generated, all the
                // resource images will go to the main assembly.
                //
                if (name != null && name.Length > 0)
                {
                    CultureInfo   cl;

                    cl = new CultureInfo(name);

                    // if CultureInfo instance cannot be created for the given name,
                    // treat it as invalid culture name.

                    if (cl == null)
                      bValid = false;
                }
            }
            catch (ArgumentException)
            {
                bValid = false;
            }

            return bValid;
        }

#if NETFX 
        // The Global Assembly Cache does not exist on .NET Core.  Only include GacPath
        // check for .NET Framework. 
        [DllImport("fusion.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetCachePath(AssemblyCacheFlags cacheFlags, StringBuilder cachePath, ref int pcchPath);

        private static List<string> _gacPaths;
        internal static IEnumerable<string> GetGacPaths()
        {
            if (_gacPaths != null) return _gacPaths;

            List<string> gacPaths = new List<string>();

            AssemblyCacheFlags[] flags = new AssemblyCacheFlags[] {
                AssemblyCacheFlags.ROOT,
                AssemblyCacheFlags.ROOT_EX
            };

            foreach (AssemblyCacheFlags flag in flags)
            {
                int gacPathLength = 0;

                // Request the size of buffer for the path.
                int hresult = GetCachePath(flag, null, ref gacPathLength);

                //
                // When gacPathLength is set to 0 and passed to this method, the return value
                // is an error which indicates INSUFFICIENT_BUFFER, so the code here doesn't
                // check that return value, but just check whether the returned desired buffer
                // length is valid or not.
                //
                if (gacPathLength > 0)
                {
                    // Allocate the right size for that buffer.
                    StringBuilder gacPath = new StringBuilder(gacPathLength);

                    // Get the real path string to the buffer.
                    hresult = GetCachePath(flag, gacPath, ref gacPathLength);

                    if (hresult >= 0)
                    {
                        gacPaths.Add(gacPath.ToString());
                    }
                }
            }
            if (gacPaths.Count > 0)
            {
                _gacPaths = gacPaths;
            }

            return _gacPaths;
        }
#endif

        //
        // Detect whether the referenced assembly could be changed during the build procedure.
        //
        // Current logic:
        //      By default, assume it could be changed during the build.
        //      If knownChangedAssemblies are set, only those assemblies are changeable, all others are not.
        //
        //      If the assembly is not in the knownChangedAssemblies list,
        //              but it is under GAC or under knownUnchangedReferencePaths, it is not changeable.
        //
        internal static bool CouldReferenceAssemblyBeChanged(string assemblyPath, string[] knownUnchangedReferencePaths, string[] knownChangedAssemblies)
        {
            Debug.Assert(String.IsNullOrEmpty(assemblyPath) == false, "assemblyPath should not be empty.");

            bool bCouldbeChanged = true;

            if (String.Compare(Path.GetExtension(assemblyPath), SharedStrings.MetadataDll, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return false;
            }

            if (knownChangedAssemblies != null && knownChangedAssemblies.Length > 0)
            {
                int length = assemblyPath.Length;
                bool bInKnownChangedList = false;

                foreach (string changedAsm in knownChangedAssemblies)
                {
                    if (String.Compare(assemblyPath, 0, changedAsm, 0, length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        bInKnownChangedList = true;
                        break;
                    }
                }

                bCouldbeChanged = bInKnownChangedList;
            }
            else
            {
                if (knownUnchangedReferencePaths != null && knownUnchangedReferencePaths.Length > 0)
                {
                    foreach (string unchangePath in knownUnchangedReferencePaths)
                    {
                        if (assemblyPath.StartsWith(unchangePath, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            bCouldbeChanged = false;
                            break;
                        }
                    }
                }
#if NETFX
                // Check the Global Assembly Cache (GAC) on .NET Framework only.
                if (bCouldbeChanged)
                {
                    IEnumerable<string> gacRoots = GetGacPaths();
                    if (gacRoots != null)
                    {
                        foreach (string gacRoot in gacRoots)
                        {
                            if (!String.IsNullOrEmpty(gacRoot) && assemblyPath.StartsWith(gacRoot, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                bCouldbeChanged = false;
                            }
                        }
                    }
                }
#endif
            }

            return bCouldbeChanged;

        }


        internal static string GetWholeExceptionMessage(Exception exception)
        {
            Exception e = exception;
            string message = e.Message;

            while (e.InnerException != null)
            {
                Exception eInner = e.InnerException;
                if (e.Message.IndexOf(eInner.Message, StringComparison.Ordinal) == -1)
                {
                    message += ", ";
                    message += eInner.Message;
                }
                e = eInner;
            }

            if (message != null && message.EndsWith(".", StringComparison.Ordinal) == false)
            {
                message += ".";
            }

            return message;

        }

        //
        // Helper to create CompilerWrapper.
        //
        internal static CompilerWrapper CreateCompilerWrapper(bool fInSeparateDomain, ref AppDomain  appDomain)
        {
            return new CompilerWrapper();
        }

        #endregion Internal Methods

     }

    #endregion TaskHelper class
}

