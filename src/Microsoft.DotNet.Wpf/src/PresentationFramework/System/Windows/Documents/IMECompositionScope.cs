// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Documents
{
    // Identifies an ongoing IME composition in a TextContainer.
    //
    // There are two cases where we cannot fulfil the edits IME thinks it had made:
    // 1. Typing into a BlockUIContainer - we might need to add an extra Paragraph next to it to contain the text.
    // 2. Deleting boundary between a BlockUIContainer and another block - we cannot merge these two.
    //
    // Elements have edges that count as characters; if we have elements IME does not know about, all counts and
    // offsets get out of sync.
    //
    // - Typing with IME that uses SetText is the main problem, because that method does not have any feedback
    //   back to IME while it holds a lock during which we are not supposed to change the document.
    //   We need to make sure our offsets stay what the IME thinks they are, i.e. pretend the extra elements we have
    //   don't exist. We can do that by deferring left edges (sometimes of the new elements, sometimes of neighboring
    //   ones) until the composition is done. Once the composition is over and all locks are released,
    //   we can notify IME we had added extra elements.
    //
    // - Typing with IME that uses InsertTextAtSelection is fine, because that method can return
    //   "actually the edit went here and changed that many characters".
    //
    // - Typing without IME is fine, because we are allowed to notify the store about any changes.
    //
    // This class keeps a list of elements that have the left edge deferred. We cannot walk the document tree to find
    // such elements, because they might have been removed from it (deleted elements get their own tree each).
    // When a composition causes an edit that needs to be deferred, the tree root node gets an instance of this class,
    // and the elements with deferred edges get added to the list.
    internal sealed class IMECompositionScope
    {
        // Elements whose left edge was deferred by this composition. The replay undoes and redoes deferrals and
        // recreates elements, so this may contain duplicates, elements whose deferral was undone, and elements
        // no longer in the tree. RevealDeferredIMELeftEdges skips the ones whose edge is no longer deferred.
        internal List<TextElement> DeferredElements { get; } = new List<TextElement>();
    }
}
