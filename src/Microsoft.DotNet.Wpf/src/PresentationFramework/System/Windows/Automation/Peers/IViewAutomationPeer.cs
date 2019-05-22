// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IViewAutomationPeer interface
//

using System.Collections.Generic;
using System.Collections.Specialized;


namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// Interface through which a customized view can implement its automation peer
    /// </summary>
    public interface IViewAutomationPeer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        AutomationControlType GetAutomationControlType();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patternInterface"></param>
        /// <returns></returns>
        object GetPattern(PatternInterface patternInterface);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<AutomationPeer> GetChildren(List<AutomationPeer> children);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        ItemAutomationPeer CreateItemAutomationPeer(object item);

        /// <summary>
        /// ListView will call this method when items is changed
        /// </summary>
        /// <param name="e"></param>
        //Note: The following two reasons explain why we need the ItemsChanged method
        //      1 View must know when Items has been changed in order to fire event when IGridProvider.RowCount is changed
        //      2 ItemsControl doesn't fire a ItemsChanged event, the only way to do this is to override the OnItemsChanged event
        void ItemsChanged(NotifyCollectionChangedEventArgs e);

        /// <summary>
        /// ListView will call this method when the view is detached from it
        /// </summary>
        void ViewDetached();
    }
}