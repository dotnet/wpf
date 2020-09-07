// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    internal static class MarshalLocal
    {
        // Temporary workaround until https://github.com/dotnet/corefx/issues/30393 is fixed
        public static bool IsTypeVisibleFromCom(Type type)
        {
            try
            {
                int unused = Marshal.GetStartComSlot(type);
            }
            catch (ArgumentException)
            {
                if (type == null)
                {
                    throw;
                }
                return false;
            }
            return true;
        }
    }
}