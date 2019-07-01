// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Globalization
{
    /// <summary>
    /// Enum representation of Wpf binaries.
    /// </summary>
    public enum WpfBinaries
    {
        /// <summary>
        /// Represents WindowsBase.dll
        /// </summary>
        WindowsBase,

        /// <summary>
        /// Represents PresentationCore.dll
        /// </summary>
        PresentationCore,

        /// <summary>
        /// Represents PresentationFramework.dll
        /// </summary>
        PresentationFramework,

#if TESTBUILD_CLR40
        /// <summary>
        /// Represents System.Xaml.dll
        /// </summary>
        SystemXaml
#endif
    }

    /// <summary>
    /// Used for comparing exceptions strings.
    /// </summary>
    public static class Exceptions
    {
        /// <summary>
        /// Compare an exception string thrown at runtime to the expected string in a resource file
        /// </summary>
        /// <param name="actual">String that you would like to compare to.</param>
        /// <param name="resourceId">ID of resource to look up.</param>
        /// <param name="targetBinary">WPF Resource that contains the object throwing the exception.</param>
        /// <returns></returns>
        public static bool CompareMessage(string actual, string resourceId, WpfBinaries targetBinary)
        {
            string expected = GetMessage(resourceId, targetBinary);

            if (expected == null)
            {
                GlobalLog.LogEvidence(string.Format("Could not find message with resourceId {0} in binary {1}.", resourceId, targetBinary.ToString()));
                return false;
            }

            return CompareMessage(actual, expected);
        }

        /// <summary>
        /// Compare an exception string thrown at runtime to the expected string in a resource file
        /// </summary>
        /// <param name="actual">String that you would like to compare to.</param>
        /// <param name="resourceId">The Id of the exception that will be looked up.</param>
        /// <param name="assembly">assembly that contains the object throwing the exception</param>
        /// <param name="resourceName">the resource containing the exception string</param>
        /// <returns></returns>
        public static bool CompareMessage(string actual, string resourceId, Assembly assembly, string resourceName)
        {
            string expected = GetMessage(resourceId, assembly, resourceName);

            if (expected == null)
            {
                GlobalLog.LogEvidence(string.Format("Could not find message with resourceId {0} in {1} assembly's {2} resource.", resourceId, assembly.ToString(), resourceName));
                return false;
            }

            return CompareMessage(actual, expected);
        }

        /// <summary>
        /// Compare an exception string to the expected string using regular expression
        /// </summary>
        /// <param name="actual">String that you would like to compare to.</param>
        /// <param name="expected">Expected string from exception string table.</param>
        /// <returns></returns>
        public static bool CompareMessage(string actual, string expected)
        {
            // Pattern to find all special characters in a string.
            string specialPattern = @"[\^$|?*+()]";

            // Pattern to match arguments in string comparison
            string argumentPattern = ".*";

            // Need to escape metacharacters (specialChars) in string to ensure literal meaning.
            Regex escapeExpression = new Regex(specialPattern);
            MatchCollection escapeMatches = escapeExpression.Matches(expected);
            foreach (Match escapeMatch in escapeMatches)
            {
                expected = expected.Replace(escapeMatch.Value, string.Format(@"\{0}", escapeMatch.Value));
            }

            // Need to find and replace string arguments ({0},{1},etc...) in string from resource with 
            // argument pattern to match any argument that gets passed into the string.
            Regex argumentExpression = new Regex(@"{\d*}");
            MatchCollection argumentMatches = argumentExpression.Matches(expected);
            foreach (Match match in argumentMatches)
            {
                expected = expected.Replace(match.Value, argumentPattern);
            }

            // Create regex expression from modified string from resource and compare it to actual string.
            Regex newExpression = new Regex(expected);
            //This code previously had an issue leading to false failures.
            //The exception string for a XamlParseException contains line/character information after the exception string, but the comparison below
            //was doing a length comparison between the two strings on top of the regex comparison.  This clearly could never match for exceptions
            //containing line/character information.  Just regex-comparing the strings themselves is sufficient and will actually work properly in all cases.
            if (newExpression.IsMatch(actual))
            {
                return true;
            }
            else
            {
                GlobalLog.LogDebug("Expected pattern: " + expected);
                GlobalLog.LogDebug("Actual string: " + actual);
                return false;
            }
        }

        /// <summary>
        /// Look up exception string as resources and return localized string of exception message.
        /// </summary>
        /// <param name="resourceId">The Id of the exception that will be looked up.</param>
        /// <param name="targetBinary">Wpf Resource that contains the object throwing the exception</param>
        /// <returns>Localized string of the specified exception message</returns>
        public static string GetMessage(string resourceId, WpfBinaries targetBinary)
        {
            Assembly assembly = null;

            switch (targetBinary)
            {
                // Will look for resource in WindowsBase.dll
                case WpfBinaries.WindowsBase:
                    assembly = typeof(DependencyObject).Assembly;
                    break;
                // Will look for resource in PresentationCore.dll
                case WpfBinaries.PresentationCore:
                    assembly = typeof(UIElement).Assembly;
                    break;
                // Will look for resource in PresentationFramework.dll
                case WpfBinaries.PresentationFramework:
                    assembly = typeof(FrameworkElement).Assembly;
                    break;
#if TESTBUILD_CLR40
                // Will look for resource in System.Xaml.dll
                case WpfBinaries.SystemXaml:
                    assembly = typeof(System.Xaml.XamlReader).Assembly;
                    break;
#endif
                default:
                    break;
            }

            if (assembly != null)
            {
                // Use a ResourceManager to locate the resource in the desired assembly.
                ResourceManager resourceManager = new ResourceManager(String.Format("FxResources.{0}.SR", assembly.GetName().Name), assembly);
                return resourceManager.GetString(resourceId);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Look up exception string as resources and return localized string of exception message.
        /// </summary>
        /// <param name="resourceId">The Id of the exception that will be looked up.</param>
        /// <param name="assembly">assembly that contains the object throwing the exception</param>
        /// <param name="resourceName">the resource containing the exception string</param>
        /// <returns>Localized string of the specified exception message</returns>
        public static string GetMessage(string resourceId, Assembly assembly, string resourceName)
        {
            if (assembly != null)
            {
                ResourceManager resourceManager = new ResourceManager(resourceName, assembly);
                return resourceManager.GetString(resourceId);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get localized exception string for one type of Exception with a list of arguments. 
        /// </summary>
        /// <param name="exceptionType">type of exception</param>
        /// <param name="args">argument list</param>
        /// <returns></returns>
        public static string GetExceptionMessage(Type exceptionType, params object[] args)
        {
            string message = string.Empty;

            //it is possible that the we cannot create the exception. 
            try
            {
                Exception exception = Activator.CreateInstance(exceptionType, args) as Exception;
                message = exception.Message;
            }
            catch (Exception creationException)
            {
                message = string.Format("Got an exception while creating the exception of type: {0}: {1}", exceptionType.FullName, creationException.Message);
            }

            return message;
        }
    }
}