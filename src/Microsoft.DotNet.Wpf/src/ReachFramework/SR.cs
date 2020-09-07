// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Resources;

namespace System.Windows.Xps
{
    internal static partial class SR
    {
        public static string Get(string id)
        {
             return SR.Format(id);
        }
        
        public static string Get(string id, params object[] args)
        {
             return SR.Format(id, args);
        }
    }
}