// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System.Collections;
using MS.Win32;

namespace System.Windows.Documents
{
    //------------------------------------------------------
    //
    //  TextServicesDisplayAttributePropertyRanges class
    //
    //------------------------------------------------------

    /// <summary>
    ///   The class for readind string property ranges of EditRecord
    /// </summary>
    internal class TextServicesDisplayAttributePropertyRanges : TextServicesPropertyRanges
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TextServicesDisplayAttributePropertyRanges(TextStore textstore)
            : base(textstore, UnsafeNativeMethods.GUID_PROP_ATTRIBUTE)
        { 
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///     Apply attribute for the range and display attribute property.
        /// </summary>
        internal override void OnRange(
            UnsafeNativeMethods.ITfProperty property,
            int ecReadOnly, 
            UnsafeNativeMethods.ITfRange range)
        {
            Int32 guidatom = GetInt32Value(ecReadOnly, property, range);
            if (guidatom != 0)
            {
                TextServicesDisplayAttribute  attr;
                attr = GetDisplayAttribute(guidatom);
                if (attr != null)
                {
                    ITextPointer start;
                    ITextPointer end;

                    ConvertToTextPosition(range, out start, out end);

                    attr.Apply(start, end);
                }
            }
        }

        /// <summary>
        ///    Calback function for TextEditSink
        ///    we track the property change here.
        /// </summary>
        internal override void OnEndEdit(UnsafeNativeMethods.ITfContext context,
                                        int ecReadOnly, 
                                        UnsafeNativeMethods.ITfEditRecord editRecord) 
        {
            Guid displayAttributeGuid;
            UnsafeNativeMethods.ITfProperty displayAttributeProperty;

            //
            // Remove any existing display attribute highlights.
            //

#if UNUSED_IME_HIGHLIGHT_LAYER
            if (_highlightLayer != null)
            {
                this.TextStore.TextContainer.Highlights.RemoveLayer(_highlightLayer);
                _highlightLayer = null;
            }
#endif

            //
            // Remove any existing composition adorner for display attribute.
            //

            _compositionAdorner?.Uninitialize();
            _compositionAdorner = null;

            //
            // Look for new ones.
            //

            // Get the DisplayAttributeProperty.
            displayAttributeGuid = Guid;
            context.GetProperty(ref displayAttributeGuid, out displayAttributeProperty);

            //
            // Enumerate the display attribute property only where display attributes
            // can currently be, instead of over the whole document.
            //
            // Passing null as the target range of EnumRanges enumerates the property
            // across the entire document. The property keeps a range for every run the
            // input method has composed, so on a long document this is a COM round trip
            // per historical range, on every keystroke. Measured with the Windows 11
            // Korean TSF IME on a 1,200 character document, a single OnEndEdit
            // enumerated 1,077 ranges of which exactly one carried an attribute, and
            // blocked the UI thread for roughly three seconds.
            //
            // Display attributes are decorations the input method places on text it is
            // composing, so they can only be (a) inside an active composition or
            // (b) on a range this edit just changed (which also covers ranges an ending
            // composition just cleared). Enumerating the smallest span that covers both
            // finds every range the whole-document scan would find - for any IME,
            // including ones whose composition holds several attribute ranges at once,
            // such as Japanese clause conversion, and edits that touch only some of
            // them - while keeping the cost proportional to the composition rather than
            // to the document.
            //
            if (TryGetAttributeScanWindow(context, ecReadOnly, editRecord, out int scanStart, out int scanEnd))
            {
                context.GetStart(ecReadOnly, out UnsafeNativeMethods.ITfRange scanRange);
                ((UnsafeNativeMethods.ITfRangeACP)scanRange).SetExtent(scanStart, scanEnd - scanStart);

                AddAttributeRanges(ecReadOnly, displayAttributeProperty, scanRange);

                Marshal.ReleaseComObject(scanRange);
            }

#if UNUSED_IME_HIGHLIGHT_LAYER
            if (_highlightLayer != null)
            {
                this.TextStore.TextContainer.Highlights.AddLayer(_highlightLayer);
            }
#endif

            if (_compositionAdorner != null)
            {
                // Update the layout to get the acurated rectangle from calling GetRectangleFromTextPosition
                this.TextStore.RenderScope.UpdateLayout();

                // Invalidate the composition adorner to render the composition attribute ranges.
                _compositionAdorner.InvalidateAdorner();
            }

            Marshal.ReleaseComObject(displayAttributeProperty);
        }

        /// <summary>
        ///     Computes the smallest ACP span covering every active composition and
        ///     every range whose display attribute this edit changed. Returns false
        ///     when there is nothing to scan.
        /// </summary>
        private bool TryGetAttributeScanWindow(
            UnsafeNativeMethods.ITfContext context,
            int ecReadOnly,
            UnsafeNativeMethods.ITfEditRecord editRecord,
            out int scanStart,
            out int scanEnd)
        {
            scanStart = int.MaxValue;
            scanEnd = int.MinValue;
            int fetched;

            // (a) Active compositions.
            if (context is UnsafeNativeMethods.ITfContextComposition contextComposition)
            {
                contextComposition.EnumCompositions(out UnsafeNativeMethods.IEnumITfCompositionView compositionEnumerator);
                if (compositionEnumerator != null)
                {
                    UnsafeNativeMethods.ITfCompositionView[] views = new UnsafeNativeMethods.ITfCompositionView[1];
                    while (compositionEnumerator.Next(1, views, out fetched) == NativeMethods.S_OK && fetched == 1)
                    {
                        views[0].GetRange(out UnsafeNativeMethods.ITfRange compositionRange);
                        if (compositionRange != null)
                        {
                            ExtendScanWindow(compositionRange, ref scanStart, ref scanEnd);
                            Marshal.ReleaseComObject(compositionRange);
                        }
                        Marshal.ReleaseComObject(views[0]);
                    }
                    Marshal.ReleaseComObject(compositionEnumerator);
                }
            }

            // (b) Ranges whose display attribute changed in this edit.
            UnsafeNativeMethods.IEnumTfRanges updatedRanges = GetPropertyUpdate(editRecord);
            if (updatedRanges != null)
            {
                UnsafeNativeMethods.ITfRange[] updated = new UnsafeNativeMethods.ITfRange[1];
                while (updatedRanges.Next(1, updated, out fetched) == NativeMethods.S_OK && fetched == 1)
                {
                    ExtendScanWindow(updated[0], ref scanStart, ref scanEnd);
                    Marshal.ReleaseComObject(updated[0]);
                }
                Marshal.ReleaseComObject(updatedRanges);
            }

            return scanStart <= scanEnd;
        }

        private static void ExtendScanWindow(UnsafeNativeMethods.ITfRange range, ref int scanStart, ref int scanEnd)
        {
            ((UnsafeNativeMethods.ITfRangeACP)range).GetExtent(out int start, out int count);

            // Cicero can report a negative length; ConvertToTextPosition guards the same way.
            if (count < 0)
            {
                return;
            }

            if (start < scanStart)
            {
                scanStart = start;
            }
            if (start + count > scanEnd)
            {
                scanEnd = start + count;
            }
        }

        /// <summary>
        ///     Adds every display attribute range found inside targetRange to the
        ///     composition adorner.
        /// </summary>
        private void AddAttributeRanges(
            int ecReadOnly,
            UnsafeNativeMethods.ITfProperty displayAttributeProperty,
            UnsafeNativeMethods.ITfRange targetRange)
        {
            UnsafeNativeMethods.IEnumTfRanges attributeRangeEnumerator;

            if (displayAttributeProperty.EnumRanges(ecReadOnly, out attributeRangeEnumerator, targetRange) != NativeMethods.S_OK)
            {
                return;
            }

            UnsafeNativeMethods.ITfRange[] attributeRanges = new UnsafeNativeMethods.ITfRange[1];
            int fetched;

            // Walk each range.
            while (attributeRangeEnumerator.Next(1, attributeRanges, out fetched) == NativeMethods.S_OK)
            {
                // Get a DisplayAttribute for this range.
                int guidAtom = GetInt32Value(ecReadOnly, displayAttributeProperty, attributeRanges[0]);
                TextServicesDisplayAttribute displayAttribute = GetDisplayAttribute(guidAtom);

                if (displayAttribute != null && !displayAttribute.IsEmptyAttribute())
                {
                    // Set a matching highlight for the attribute range.
                    ITextPointer start;
                    ITextPointer end;
                    ConvertToTextPosition(attributeRanges[0], out start, out end);

                    if (start != null)
                    {
#if UNUSED_IME_HIGHLIGHT_LAYER
                        // Demand create the highlight layer.
                        if (_highlightLayer == null)
                        {
                            _highlightLayer = new DisplayAttributeHighlightLayer();
                        }
#endif

                        if (_compositionAdorner == null)
                        {
                            _compositionAdorner = new CompositionAdorner(this.TextStore.TextView);
                            _compositionAdorner.Initialize(this.TextStore.TextView);
                        }

#if UNUSED_IME_HIGHLIGHT_LAYER
                        // Need to pass the foreground and background color of the composition
                        _highlightLayer.Add(start, end, /*TextDecorationCollection:*/null);
#endif

                        // Add the attribute range into CompositionAdorner.
                        _compositionAdorner.AddAttributeRange(start, end, displayAttribute);
                    }
                }

                Marshal.ReleaseComObject(attributeRanges[0]);
            }

            Marshal.ReleaseComObject(attributeRangeEnumerator);
        }

        // Callback from TextServicesProperty.OnLayoutUpdated.
        // Updates composition display attribute adorner on-screen location.
        internal void OnLayoutUpdated()
        {
            _compositionAdorner?.InvalidateAdorner();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Access DisplayAttributeManager
        /// </summary>
        private static TextServicesDisplayAttribute GetDisplayAttribute(Int32 guidatom)
        {
            TextServicesDisplayAttribute attr = null;

            // Demand create the cache.
            if (_attributes == null)
            {
                _attributes = new Hashtable();
            }

            attr = (TextServicesDisplayAttribute)_attributes[guidatom];

            if (attr != null)
                return attr;

            //
            // Use category manager to convert guidatom to GUID.
            //
            UnsafeNativeMethods.ITfCategoryMgr catmgr;
            if (UnsafeNativeMethods.TF_CreateCategoryMgr(out catmgr) != NativeMethods.S_OK)
                return null;

            if (catmgr == null)
                return null;
        
            Guid guid;
            catmgr.GetGUID(guidatom, out guid);
            Marshal.ReleaseComObject(catmgr);

            // GetGUID could fail and reutrn GUID_NULL.
            if (guid.Equals(UnsafeNativeMethods.Guid_Null))
                return null;

            //
            // Use DisplayAttributeMgr to get TF_DISPLAYATTRIBUTE.
            //
            UnsafeNativeMethods.ITfDisplayAttributeMgr dam;
            UnsafeNativeMethods.ITfDisplayAttributeInfo dai;
            UnsafeNativeMethods.TF_DISPLAYATTRIBUTE tfattr;
            if (UnsafeNativeMethods.TF_CreateDisplayAttributeMgr(out dam) != NativeMethods.S_OK)
                return null;

            if (dam == null)
                return null;

            Guid clsid;
            dam.GetDisplayAttributeInfo(ref guid, out dai, out clsid);
            if (dai != null)
            {
                dai.GetAttributeInfo(out tfattr);
                attr = new TextServicesDisplayAttribute(tfattr);
                Marshal.ReleaseComObject(dai);
 
                //
                // cache this into our hashtable.
                //
                _attributes[guidatom] = attr;
            }

            Marshal.ReleaseComObject(dam);
            return attr;
        }

        private Int32 GetInt32Value(int ecReadOnly, UnsafeNativeMethods.ITfProperty property, UnsafeNativeMethods.ITfRange range)
        {
            Object obj = GetValue(ecReadOnly, property, range);
            if (obj == null)
                return 0;

            return (Int32)obj;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Attribute cache.
        // this is an unbounded cache, it should have an upper bound.  Use MRUTinyCache instead?
        private static Hashtable _attributes;

#if UNUSED_IME_HIGHLIGHT_LAYER
        // Highlights for our display attributes.
        private DisplayAttributeHighlightLayer _highlightLayer;
#endif

        // CompositionAdorner for displaying the composition attributes.
        private CompositionAdorner _compositionAdorner;

        #endregion Private Fields
    }
}
