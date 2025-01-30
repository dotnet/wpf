// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper methods for code that uses types from System.Core.
//

namespace MS.Internal
{
    internal static class SystemCoreHelper
    {
        // return true if the item implements IDynamicMetaObjectProvider
        internal static bool IsIDynamicMetaObjectProvider(object item)
        {
            SystemCoreExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemCore();
            return (extensions != null) ? extensions.IsIDynamicMetaObjectProvider(item) : false;
        }

        // return a new DynamicPropertyAccessor
        internal static object NewDynamicPropertyAccessor(Type ownerType, string propertyName)
        {
            SystemCoreExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemCore();
            return extensions?.NewDynamicPropertyAccessor(ownerType, propertyName);
        }

        // return a DynamicIndexerAccessor with the given number of arguments
        internal static object GetIndexerAccessor(int rank)
        {
            SystemCoreExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemCore();
            return extensions?.GetIndexerAccessor(rank);
        }
    }
}
