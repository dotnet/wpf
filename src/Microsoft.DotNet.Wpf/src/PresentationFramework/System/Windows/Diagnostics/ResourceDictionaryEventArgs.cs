// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Contains EventArg types raised to communicate ResourceDictionary loaded
//      and unloaded events.

namespace System.Windows.Diagnostics
{
    /// <summary>
    /// Provides data for <see cref="ResourceDictionaryDiagnostics.GenericResourceDictionaryLoaded"/> 
    /// and <see cref="ResourceDictionaryDiagnostics.ThemedResourceDictionaryLoaded"/> events.
    /// </summary>
    public class ResourceDictionaryLoadedEventArgs : EventArgs
    {
        internal ResourceDictionaryLoadedEventArgs(ResourceDictionaryInfo resourceDictionaryInfo)
        {
            ResourceDictionaryInfo = resourceDictionaryInfo;
        }

        public ResourceDictionaryInfo ResourceDictionaryInfo
        {
            get; private set;
        }
    }

    /// <summary>
    /// Provides data for <see cref="ResourceDictionaryDiagnostics.ThemedResourceDictionaryUnloaded"/> event.
    /// </summary>
    public class ResourceDictionaryUnloadedEventArgs: EventArgs
    {
        internal ResourceDictionaryUnloadedEventArgs(ResourceDictionaryInfo resourceDictionaryInfo)
        {
            ResourceDictionaryInfo = resourceDictionaryInfo;
        }

        public ResourceDictionaryInfo ResourceDictionaryInfo
        {
            get; private set;
        }
    }
}