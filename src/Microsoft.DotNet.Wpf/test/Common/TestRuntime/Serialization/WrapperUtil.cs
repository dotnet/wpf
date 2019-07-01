// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Reflection;

namespace Microsoft.Test.Serialization
{
    /// <summary>
    /// Class containing utility methods to load assemblies.
    /// </summary>
    internal static class WrapperUtil
    {
        /// <summary>
        /// Reference to framework assembly.
        /// </summary>
        public static Assembly AssemblyPF = LoadAssemblyPF();

        /// <summary>
        /// Reference to core assembly.
        /// </summary>
        public static Assembly AssemblyPC = LoadAssemblyPC();

        /// <summary>
        /// Reference to base assembly.
        /// </summary>
        public static Assembly AssemblyWB = LoadAssemblyWB();

        /// <summary>
        /// Default flags to reflect into methods.
        /// </summary>
        public static BindingFlags MethodBindFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod;

        /// <summary>
        /// Default flags to reflect into properties.
        /// </summary>
        public static BindingFlags PropertyBindFlags = BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetProperty;

        private const string PRESENTATIONCOREASSEMBLYNAME = @"PresentationCore";
        private const string PRESENTATIONFRAMEWORKASSEMBLYNAME = @"PresentationFramework";
        private const string WINDOWSBASEASSEMBLYNAME = @"WindowsBase";
        private const string DOTNETCULTURE = @"en-us";

        private static Assembly LoadAssemblyPF()
        {
            Assembly assm = FindAssemblyInDomain(PRESENTATIONFRAMEWORKASSEMBLYNAME);
            if (assm == null)
            {
                // A quick way to load the compatible version of "PresentationFramework" 
                Type parserType = typeof(System.Windows.Markup.XamlReader);
                assm = parserType.Assembly;
            }

            return assm;
        }

        private static Assembly LoadAssemblyPC()
        {
            Assembly assm = FindAssemblyInDomain(PRESENTATIONCOREASSEMBLYNAME);
            if (assm == null)
            {
                // A quick way to load the compatible version of "PresentationCore" 
                Type parserType = typeof(System.Windows.UIElement);
                assm = parserType.Assembly;
            }

            return assm;
        }

        private static Assembly LoadAssemblyWB()
        {
            Assembly assm = FindAssemblyInDomain(WINDOWSBASEASSEMBLYNAME);
            if (assm == null)
            {
                // A quick way to load the compatible version of "WindowsBase" 
                Type dispatcherType = typeof(System.Windows.Threading.Dispatcher);
                assm = dispatcherType.Assembly;
            }

            return assm;
        }

        // Loads the Assembly with the specified name.
        internal static Assembly FindAssemblyInDomain(string assemblyName)
        {
            return null;
        }


    }

}











