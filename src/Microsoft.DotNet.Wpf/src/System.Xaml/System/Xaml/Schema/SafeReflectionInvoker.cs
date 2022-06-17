// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Xaml.Schema
{
    static class SafeReflectionInvoker
    {
        static readonly Assembly SystemXaml = typeof(SafeReflectionInvoker).Assembly;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Retained per servicing policy.")]
        public static bool IsInSystemXaml(Type type)
        {
            if (type.Assembly == SystemXaml)
            {
                return true;
            }
            if (type.IsGenericType)
            {
                foreach (Type typeArg in type.GetGenericArguments())
                {
                    if (IsInSystemXaml(typeArg))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // ^^^^^----- End of unused members.  -----^^^^^

        // vvvvv---- Unused members.  Servicing policy is to retain these anyway.  -----vvvvv
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Retained per servicing policy.")]
        internal static bool IsSystemXamlNonPublic(MethodInfo method)
        {
            Type declaringType = method.DeclaringType;
            if (IsInSystemXaml(declaringType) && (!method.IsPublic || !declaringType.IsVisible))
            {
                return true;
            }
            if (method.IsGenericMethod)
            {
                foreach (Type typeArg in method.GetGenericArguments())
                {
                    if (IsInSystemXaml(typeArg) && !typeArg.IsVisible)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // ^^^^^----- End of unused members.  -----^^^^^
    }
}
