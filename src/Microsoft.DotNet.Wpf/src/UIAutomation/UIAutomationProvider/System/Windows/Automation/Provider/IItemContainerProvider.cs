// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Item Container pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// 
    /// Exposes a container control's ability to search over the items it contains.
    /// This pattern must be implemented by containers which suppots virtualization and have 
    /// no other means to find the virtualized element though it's orthogonal to virtualization
    /// and can be implemented by any containers which has items in it.
    /// 
    /// Examples of UI Item containers that implements this includes:
    /// - ListBox
    /// - ListView
    /// - ComboBox
    /// - TabControl
    /// </summary>
    [ComVisible(true)]
    [Guid("e747770b-39ce-4382-ab30-d8fb3f336f24")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IItemContainerProvider
#else
    public interface IItemContainerProvider
#endif
    {
        ///<summary>
        /// Find item by specified property/value. It will return
        /// placeholder which depending upon it's virtualization state may
        /// or may not have the information of the complete peer/Wrapper.
        /// 
        /// Throws ArgumentException if the property requested is not one that the
        /// container supports searching over. Supports Name property, AutomationId,
        /// IsSelected and ControlType.
        /// 
        /// This method is expected to be relatively slow, since it may need to
        /// traverse multiple objects in order to find a matching one.
        /// When used in a loop to return multiple items, no specific order is
        /// defined so long as each item is returned only once (ie. loop should
        /// terminate). This method is also item-centric, not UI-centric, so items
        /// with multiple UI representations need only be returned once.
        ///
        /// A special propertyId of 0 means ‘match all items’. This can be used
        /// with startAfter=NULL to get the first item, and then to get successive
        /// items.
        /// </summary>
        /// <param name="startAfter">this represents the item after which the container wants to begin search</param>
        /// <param name="propertyId">corresponds to property for whose value it want to search over.</param>
        /// <param name="value">value to be searched for, for specified property</param>
        /// <returns>The first item which matches the searched criterion, if no item matches, it returns null  </returns>
        IRawElementProviderSimple FindItemByProperty(IRawElementProviderSimple startAfter, int propertyId, object value);

    }
}
