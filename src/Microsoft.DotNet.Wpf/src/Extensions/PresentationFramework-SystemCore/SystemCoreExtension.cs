// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Helper methods for code that uses types from System.Core.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace MS.Internal
{
    //FxCop can't tell that this class is instantiated via reflection, so suppress the FxCop warning.
    [SuppressMessage("Microsoft.Performance","CA1812:AvoidUninstantiatedInternalClasses")]
    internal class SystemCoreExtension : SystemCoreExtensionMethods
    {
        // return true if the item implements IDynamicMetaObjectProvider
        internal override bool IsIDynamicMetaObjectProvider(object item)
        {
            return (item is IDynamicMetaObjectProvider);
        }

        // return a new DynamicPropertyAccessor
        internal override object NewDynamicPropertyAccessor(Type ownerType, string propertyName)
        {
            return new DynamicPropertyAccessorImpl(ownerType, propertyName);
        }

        // return a DynamicIndexerAccessor with the given number of arguments
        internal override object GetIndexerAccessor(int rank)
        {
            return DynamicIndexerAccessorImpl.GetIndexerAccessor(rank);
        }
    }
}
