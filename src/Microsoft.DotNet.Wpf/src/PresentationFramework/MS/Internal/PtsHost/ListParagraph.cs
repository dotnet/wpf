// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ListParagraph represents collection of list items.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using MS.Internal.Text;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// ListParagraph represents collection of list items.
    /// </summary>
    internal sealed class ListParagraph : ContainerParagraph
    {
        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="element">
        /// Element associated with paragraph.
        /// </param>
        /// <param name="structuralCache">
        /// Content's structural cache
        /// </param>
        internal ListParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        /// <summary>
        /// CreateParaclient 
        /// </summary>
        /// <param name="paraClientHandle">
        /// OUT: opaque to PTS paragraph client
        /// </param>
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)         
        {
#pragma warning disable 6518
            // Disable PRESharp warning 6518. ListParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            ListParaClient paraClient =  new ListParaClient(this);
            paraClientHandle = paraClient.Handle;
#pragma warning restore 6518
        }

        /// <summary>
        /// Determine paragraph type at the current TextPointer and 
        /// create it. Only ListItem elements are considered. Any other 
        /// content is skipped.
        /// </summary>
        /// <param name="textPointer">
        /// TextPointer at which paragraph is to be created
        /// </param>
        /// <param name="fEmptyOk">
        /// True if empty paragraph is acceptable
        /// </param>
        /// <returns>
        /// BaseParagraph that was created
        /// </returns>
        protected override BaseParagraph GetParagraph(ITextPointer textPointer, bool fEmptyOk)
        {
            Invariant.Assert(textPointer is TextPointer);

            BaseParagraph paragraph = null;

            while (paragraph == null)
            {
                TextPointerContext runType = textPointer.GetPointerContext(LogicalDirection.Forward);
                if (runType == TextPointerContext.ElementStart)
                {
                    TextElement element = ((TextPointer)textPointer).GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);
                    if (element is ListItem)
                    {
                        //      Need to handle visibility collapsed.
                        //Visibility visibility = Retriever.Visibility(treePtr.CPPtr.Element);
                        //if (visibility != Visibility.Collapsed)
                        //{
                        //    para = new 
                        //}
                        //else skip the element
                        paragraph = new ListItemParagraph(element, StructuralCache);
                        break;
                    }
                    else if (element is List)
                    {
                        //      Need to handle visibility collapsed.
                        //Visibility visibility = Retriever.Visibility(treePtr.CPPtr.Element);
                        //if (visibility != Visibility.Collapsed)
                        //{
                        //    para = new 
                        //}
                        //else skip the element
                        paragraph = new ListParagraph(element, StructuralCache);
                        break;
                    }
                    // Skip all elements, which are not valid list item children
                    if (((TextPointer)textPointer).IsFrozen)
                    {
                        // Need to clone TextPointer before moving it.
                        textPointer = textPointer.CreatePointer();
                    }
                    textPointer.MoveToPosition(element.ElementEnd);
                }
                else if (runType == TextPointerContext.ElementEnd)
                {
                    // End of list, if the same as Owner of associated element
                    // Skip content otherwise
                    if (Element == ((TextPointer)textPointer).Parent)
                    {
                        break;
                    }
                    if (((TextPointer)textPointer).IsFrozen)
                    {
                        // Need to clone TextPointer before moving it.
                        textPointer = textPointer.CreatePointer();
                    }
                    textPointer.MoveToNextContextPosition(LogicalDirection.Forward);
                }
                else
                {
                    // Skip content
                    if (((TextPointer)textPointer).IsFrozen)
                    {
                        // Need to clone TextPointer before moving it.
                        textPointer = textPointer.CreatePointer();
                    }
                    textPointer.MoveToNextContextPosition(LogicalDirection.Forward);
                }
            }

            if (paragraph != null)
            {
                StructuralCache.CurrentFormatContext.DependentMax = (TextPointer)textPointer;
            }

            return paragraph;
        }
    }
}

#pragma warning enable 1634, 1691

