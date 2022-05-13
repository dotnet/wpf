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
            if (!Path.IsPathRooted(thePath) )
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

            string sourceDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;

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
        internal static CompilerWrapper CreateCompilerWrapper()
        {
            return new CompilerWrapper();
        }

        #endregion Internal Methods

     }

    #endregion TaskHelper class
}

