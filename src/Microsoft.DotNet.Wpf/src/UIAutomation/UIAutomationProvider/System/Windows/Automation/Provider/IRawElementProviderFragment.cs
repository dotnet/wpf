// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Provider side interface for n-level UI

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;


namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Directions for navigation the UIAutomation tree
    /// </summary>
    [ComVisible(true)]
    [Guid("670c3006-bf4c-428b-8534-e1848f645122")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum NavigateDirection
#else
    public enum NavigateDirection
#endif
    {
        /// <summary>Navigate to parent</summary>
        Parent,
        /// <summary>Navigate to next sibling</summary>
        NextSibling,
        /// <summary>Navigate to previous sibling</summary>
        PreviousSibling,
        /// <summary>Navigate to first child</summary>
        FirstChild,
        /// <summary>Navigate to last child</summary>
        LastChild,
    }

    /// <summary>
    /// Implemented by providers to expose elements that are part of
    /// a structure more than one level deep. For simple one-level
    /// structures which have no children, IRawElementProviderSimple
    /// can be used instead.
    /// 
    /// The root node of the fragment must support the IRawElementProviderFragmentRoot
    /// interface, which is derived from this, and has some additional methods.
    /// </summary>
    [ComVisible(true)]
    [Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IRawElementProviderFragment : IRawElementProviderSimple
#else
    public interface IRawElementProviderFragment : IRawElementProviderSimple
#endif
    {
        /// <summary>
        /// Request to return the element in the specified direction
        /// </summary>
        /// <param name="direction">Indicates the direction in which to navigate</param>
        /// <returns>Returns the element in the specified direction</returns>
        IRawElementProviderFragment Navigate(NavigateDirection direction);


        /// <summary>
        /// Gets the runtime ID of an elemenent. This should be unique
        /// among elements on a desktop.
        /// </summary>
        /// <remarks>
        /// Proxy implementations should return null for the top-level proxy which
        /// correpsonds to the HWND; and should return an array which starts
        /// with AutomationInteropProvider.AppendRuntimeId, followed by values
        /// which are then unique within that proxy's HWNDs.
        /// </remarks>
        int [ ] GetRuntimeId();

        /// <summary>
        /// Return a bounding rectangle of this element, or Rect.Empty if this element is not visible
        /// </summary>
        Rect BoundingRectangle
        {
            get;
        }

        /// <summary>
        /// If this UI is capable of hosting other UI that also supports UIAutomation, and
        /// the subtree rooted at this element contains such hosted UI fragments, this should return
        /// an array of those fragments.
        /// 
        /// If this UI does not host other UI, it may return null.
        /// </summary>
        IRawElementProviderSimple [] GetEmbeddedFragmentRoots();
        
        /// <summary>
        /// Request that focus is set to this item.
        /// The UIAutomation framework will ensure that the UI hosting this fragment is already
        /// focused before calling this method, so this method should only update its internal
        /// focus state; it should not attempt to give its own HWND the focus, for example.
        /// </summary>
        void SetFocus();

        /// <summary>
        /// Return the element that is the root node of this fragment of UI.
        /// </summary>
        IRawElementProviderFragmentRoot FragmentRoot
        {
            get;
        }
    }
}
