// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Resources;

namespace MS.Utility
{
    internal static partial class SR
    {
        public static string Get(string name)
        {
            return GetResourceString(name, null);
        }

        public static string Get(string name, params object[] args)
        {
            return Format(GetResourceString(name, null), args);
        }

        // Expose ResourceManager instance to allow PresentationBuildTask MSBuild tasks
        // that derive from Task to pass ResourceManager to their base class constructor.
        // (Generated SR.common.cs always defines ResourceManager as private. GenerateCommonSRSource
        // target should be updated to accept a parameter for ResourceManager property access 
        // modifier.)
        public static ResourceManager SharedResourceManager 
        {
            get
            {
                return ResourceManager;
            }
        }
    }
}
