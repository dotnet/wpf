// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Windows
{
    /// <summary>
    /// This interface is implemented by layouts which host ContentElements.
    /// </summary>
    public interface IContentHost 
    {
        /// <summary>
        /// Performs hit-testing for child elements.
        /// </summary>
        /// <param name="point">
        /// Mouse coordinates relative to the ContentHost.
        /// </param>
        /// <remarks>
        /// Must return a descendant IInputElement, or NULL if no such 
        /// element exists.
        /// </remarks>
        IInputElement InputHitTest(Point point);

        /// <summary>
        /// Returns a collection of bounding rectangles for a child element.
        /// </summary>
        /// <param name="child">
        /// Child element.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If child is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If element is not a direct descendant (i.e. element must be a child 
        /// of the IContentHost or a ContentElement which is a direct descendant 
        /// of the IContentHost).
        /// </exception>
        ReadOnlyCollection<Rect> GetRectangles(ContentElement child);

        /// <summary>
        /// This enumeration contains all descendant ContentElement-derived classes, 
        /// as well as all UIElement-derived classes that are a direct descendant 
        /// of the IContentHost or one of its descendant ContentElement classes. 
        /// In other words, elements for which the IContentHost creates a visual 
        /// representation (ContentElement-derived classes) or whose layout is driven 
        /// by the IContentHost (the first-level descendant UIElement-derived classes).
        /// </summary>
        IEnumerator<IInputElement> HostedElements 
        { 
            get; 
        }
    
        /// <summary>
        /// Called when a UIElement-derived class which is hosted by a IContentHost 
        /// changes it’s DesiredSize.
        /// </summary>
        /// <param name="child">
        /// Child element whose DesiredSize has changed.
        /// </param> 
        /// <exception cref="ArgumentNullException">
        /// If child is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If child is not a direct descendant (i.e. child must be a child 
        /// of the IContentHost or a ContentElement which is a direct descendant 
        /// of the IContentHost).
        /// </exception>
        void OnChildDesiredSizeChanged(UIElement child);
    }    
}
