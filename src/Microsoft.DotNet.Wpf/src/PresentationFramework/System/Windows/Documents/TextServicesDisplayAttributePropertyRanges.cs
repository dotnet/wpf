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
            UnsafeNativeMethods.IEnumTfRanges updatedRangeEnumerator;
            UnsafeNativeMethods.ITfRange[] updatedRanges;
            int fetched;

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
            // Only look at the ranges this edit actually changed.
            //
            // Passing null as the target range of EnumRanges enumerates the display
            // attribute property over the WHOLE document. The property accumulates a
            // range per previously composed run, so the enumeration - and the COM
            // round trip it costs per range - grows with the length of the document,
            // on every single keystroke.
            //
            // Measured with a modern Korean TSF IME on a 1,200 character document:
            // 1,077 ranges returned, of which exactly one carried an attribute, for
            // roughly 3 seconds of blocked UI thread on one keystroke. Restricting
            // the enumeration to the updated ranges removes the stalls entirely.
            //
            // This mirrors what the base class TextServicesPropertyRanges.OnEndEdit
            // already does; this override had lost that scoping.
            //
            updatedRangeEnumerator = GetPropertyUpdate(editRecord);
            if (updatedRangeEnumerator != null)
            {
                updatedRanges = new UnsafeNativeMethods.ITfRange[1];

                while (updatedRangeEnumerator.Next(1, updatedRanges, out fetched) == NativeMethods.S_OK)
                {
                    AddAttributeRanges(ecReadOnly, displayAttributeProperty, updatedRanges[0]);
                    Marshal.ReleaseComObject(updatedRanges[0]);
                }

                Marshal.ReleaseComObject(updatedRangeEnumerator);
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
