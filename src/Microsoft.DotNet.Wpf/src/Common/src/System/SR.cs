// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Resources;
using System.Runtime.CompilerServices;

#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif PRESENTATION_CORE
namespace MS.Internal.PresentationCore
#elif PBTCOMPILER
namespace MS.Utility
#elif AUTOMATION
namespace MS.Internal.Automation
#elif REACHFRAMEWORK
namespace System.Windows.Xps
#elif PRESENTATIONFRAMEWORK
namespace System.Windows
#elif PRESENTATIONUI
namespace System.Windows.TrustUI
#elif WINDOWSFORMSINTEGRATION
namespace System.Windows
#elif RIBBON_IN_FRAMEWORK
namespace Microsoft.Windows.Controls
#else
namespace System
#endif
{
    internal partial class SR
    {
        // This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format.
        // by default it returns false.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool UsingResourceKeys()
        {
            return false;
        }

        internal static string GetResourceString(string resourceKey)
        {
            string resourceString = null;
            try { resourceString = ResourceManager.GetString(resourceKey); }
            catch (MissingManifestResourceException) { }

            return resourceString;
        }

        internal static string GetResourceString(string resourceKey, string defaultString)
        {
            string resourceString = GetResourceString(resourceKey);

            if (defaultString is not null && resourceKey.Equals(resourceString, StringComparison.Ordinal))
            {
                return defaultString;
            }

            return resourceString;
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args is not null)
            {
                if (UsingResourceKeys())
                {
                    return resourceFormat + string.Join(", ", args);
                }

                return string.Format(resourceFormat, args);
            }

            return resourceFormat;
        }

        internal static string Format(string resourceFormat, object p1)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1);
            }

            return string.Format(resourceFormat, p1);
        }

        internal static string Format(string resourceFormat, object p1, object p2)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2);
            }

            return string.Format(resourceFormat, p1, p2);
        }

        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2, p3);
            }

            return string.Format(resourceFormat, p1, p2, p3);
        }
    }
}
