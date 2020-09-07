// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
//
// Description:
//      Helper class to expose XamlSourceInfo for objects loaded from BAML or XAML
//      for diagnostic scenarios. Source info will be stored as an attached dependency
//      property on dependency objects. Because storing additional data increases
//      memory footprint we will turn this on when ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO
//      environment variable is present and its trimmed lowercased value is NOT "0"
//      or "false" (the idea is to make it easy trning things on and off when needed).
//
//      IMPORTANT. This functionality depends on changes in System.Xaml. The info is
//      exposed via modified XamlObjectEventArgs (3 new properties added). Because
//      PresentationCore and PresentationFramework are serviced without System.Xaml
//      it is possible that this code runs against "old" System.Xaml. In that case we
//      need to turn this feature off. We should also use reflection to access 3 new
//      properties. SourceBamlUri, ElementLineNumber and ElementLinePosition.
//
//      NOTE that public API is exposed via VisualDiagnostics (centralized place).
//

using System.Reflection;
using System.Runtime.CompilerServices;  // ConditionalWeakTable
using System.Security;
using System.Xaml;

namespace System.Windows.Diagnostics
{
    internal static class XamlSourceInfoHelper
    {
        // Weak reference storage to map objects to their markup location. It is fast enough, e.g. it takes
        // about 50ms to add 100K entries (small test app, Debug build, under debugger, dev laptop).
        private static ConditionalWeakTable<object, XamlSourceInfo> s_sourceInfoTable; // no storage by default

        // While ConditionalWeakTable is thread safe we need to make multiple calls in a thread safe manner.
        private static object s_lock = new object();

        private static PropertyInfo s_sourceBamlUriProperty;
        private static PropertyInfo s_elementLineNumberProperty;
        private static PropertyInfo s_elementLinePositionProperty;

        internal static bool IsXamlSourceInfoEnabled
        {
            get { return (s_sourceInfoTable != null); }
        }

        static XamlSourceInfoHelper()
        {
            InitializeEnableXamlSourceInfo(null);
        }

        // this method is (also) called via private reflection from test code
        private static void InitializeEnableXamlSourceInfo(string value)
        {
            if (VisualDiagnostics.IsEnabled &&
                VisualDiagnostics.IsEnvironmentVariableSet(value, "ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO") &&
                InitializeXamlObjectEventArgs())
            {
                s_sourceInfoTable = new ConditionalWeakTable<object, XamlSourceInfo>();
            }
            else
            {
                s_sourceInfoTable = null;
            }
        }

        private static bool InitializeXamlObjectEventArgs()
        {
            // In case of a servicing scenario we might be dealing with old version of XamlObjectEventArgs
            // which does not have 3 new properties: SourceBamlUri, ElementLineNumber and ElementLinePosition.
            // We'll use reflection to access those properties. If they are not available then feature is disabled.
            // WE SHOULD REMOVE REFLECTION based access and start using direct access once we are sure it is safe.
            Type type = typeof(System.Xaml.XamlObjectEventArgs);
            s_sourceBamlUriProperty = type.GetProperty("SourceBamlUri", BindingFlags.Public | BindingFlags.Instance);
            s_elementLineNumberProperty = type.GetProperty("ElementLineNumber", BindingFlags.Public | BindingFlags.Instance);
            s_elementLinePositionProperty = type.GetProperty("ElementLinePosition", BindingFlags.Public | BindingFlags.Instance);

            // Make sure we've got all the properties and do sanity check
            if (s_sourceBamlUriProperty == null || s_sourceBamlUriProperty.PropertyType != typeof(Uri) ||
                s_elementLineNumberProperty == null || s_elementLineNumberProperty.PropertyType != typeof(int) ||
                s_elementLinePositionProperty == null || s_elementLinePositionProperty.PropertyType != typeof(int))
            {
                // Old System.Xaml.
                return false;
            }
            else
            {
                return true;
            }
        }

        internal static void SetXamlSourceInfo(object obj, XamlObjectEventArgs args, Uri overrideSourceUri)
        {
            if (s_sourceInfoTable != null && args != null)
            {
                Uri sourceUri = overrideSourceUri ?? (Uri)s_sourceBamlUriProperty.GetValue(args);
                int elementLineNumber = (int)s_elementLineNumberProperty.GetValue(args);
                int elementLinePosition = (int)s_elementLinePositionProperty.GetValue(args);
                SetXamlSourceInfo(obj, sourceUri, elementLineNumber, elementLinePosition);
            }
        }

        // this method is (also) called via private reflection from test code
        internal static void SetXamlSourceInfo(object obj, Uri sourceUri, int elementLineNumber, int elementLinePosition)
        {
            // Some BAML content - like system resources or Release builds - contains fake source info of line=0, pos=0. Ignore it.
            if (s_sourceInfoTable != null && obj != null && !(elementLineNumber == 0 && elementLinePosition == 0))
            {
                // We should not store basic values like strings, enums, numbers, etc.
                if (obj is string || obj.GetType().IsValueType)
                {
                    return;
                }

                // This method can be called concurrently on multiple UI threads.
                // Using lock to make sure remove+add are atomic.
                lock (s_lock)
                {
                    // We can see same object multiple times. Most common scenario is loading
                    // MyUserControl in context of Window1.xaml. When MyUserControl is constructed and
                    // thus loaded from MyUserControl.xaml its source info is set to MyUserControl.xaml.
                    // Then MyUserControl is added to Window1 visual tree and its source info is set
                    // to Window1.xaml, which is what we want. In other words use latest source.
                    s_sourceInfoTable.Remove(obj);
                    s_sourceInfoTable.Add(obj, new XamlSourceInfo(sourceUri, elementLineNumber, elementLinePosition));
                }
            }
        }

        internal static XamlSourceInfo GetXamlSourceInfo(object obj)
        {
            // ConditionalWeakTable is thread safe
            XamlSourceInfo info = null;
            if (s_sourceInfoTable != null && obj != null && s_sourceInfoTable.TryGetValue(obj, out info))
            {
                return info;
            }
            return null;
        }
    }
}
