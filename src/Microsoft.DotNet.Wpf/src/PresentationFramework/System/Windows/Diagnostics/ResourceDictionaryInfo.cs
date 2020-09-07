// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Diagnostics;
using System.Reflection;

namespace System.Windows.Diagnostics
{
    /// <summary>
    /// Represents information about a <see cref="ResourceDictionary"/>
    /// </summary>
    [DebuggerDisplay("Assembly = {Assembly?.GetName()?.Name}, ResourceDictionary SourceUri = {SourceUri?.AbsoluteUri}")]
    public class ResourceDictionaryInfo
    {
        internal ResourceDictionaryInfo(
            Assembly assembly, 
            Assembly resourceDictionaryAssembly, 
            ResourceDictionary resourceDictionary, 
            Uri sourceUri)
        {
            Assembly = assembly;
            ResourceDictionaryAssembly = resourceDictionaryAssembly;
            ResourceDictionary = resourceDictionary;
            SourceUri = sourceUri;
        }

        /// <summary>
        /// Assembly that uses the <see cref="ResourceDictionaryInfo.ResourceDictionary"/> loaded 
        /// from <see cref="ResourceDictionaryAssembly"/>
        /// </summary>
        public Assembly Assembly {get; private set; }

        /// <summary>
        /// Assembly from which resource dictionary is loaded.
        /// </summary>
        public Assembly ResourceDictionaryAssembly { get; private set; }

        /// <summary>
        /// Resource dictionary for which additional information is described by this <see cref="ResourceDictionaryInfo"/> instance.
        /// </summary>
        public ResourceDictionary ResourceDictionary { get; private set; }

        /// <summary>
        /// Pack Uri of the compile BAML file embedded in <see cref="ResourceDictionaryAssembly"/> from which the resource dictionary is loaded.
        /// </summary>
        public Uri SourceUri { get; private set; }
    }
}