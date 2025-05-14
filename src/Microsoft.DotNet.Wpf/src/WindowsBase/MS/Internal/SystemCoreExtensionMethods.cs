// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal
{
    internal abstract class SystemCoreExtensionMethods
    {
        // return true if the item implements IDynamicMetaObjectProvider
        internal abstract bool IsIDynamicMetaObjectProvider(object item);

        // return a new DynamicPropertyAccessor
        internal abstract object NewDynamicPropertyAccessor(Type ownerType, string propertyName);

        // return a DynamicIndexerAccessor with the given number of arguments
        internal abstract object GetIndexerAccessor(int rank);
    }
}
