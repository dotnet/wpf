// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Section is representing a portion of a document in which 
// certain page formatting properties can be changed, such as line numbering, 
// number of columns, headers and footers. 
//


using System;
using System.Windows;
using System.Security;
using System.Windows.Documents;
using System.Windows.Media;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Section is representing a portion of a document in which certain page 
    /// formatting properties can be changed, such as line numbering, 
    /// number of columns, headers and footers. 
    /// </summary>
    internal sealed class Section : UnmanagedHandle
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="structuralCache">
        /// Content's structural cache
        /// </param>
        internal Section(StructuralCache structuralCache) : base(structuralCache.PtsContext)
        {
            _structuralCache = structuralCache;
        }

        /// <summary>
        /// Dispose unmanaged resources. 
        /// </summary>
        public override void Dispose()
        {
            DestroyStructure();
            base.Dispose();
        }

        #endregion Constructors

        // ------------------------------------------------------------------
        // 
        //  PTS callbacks
        //
        // ------------------------------------------------------------------

        #region PTS callbacks

        /// <summary>
        /// Indicates whether to skip a page
        /// </summary>
        /// <param name="fSkip">
        /// OUT: skip page due to odd/even page issue
        /// </param>
        internal void FSkipPage(
            out int fSkip)                   
        {
            // Never skip a page
            fSkip = PTS.False;
        }

        // ------------------------------------------------------------------
        // GetPageDimensions
        // ------------------------------------------------------------------
        
        /// <summary>
        /// Get page dimensions
        /// </summary>
        /// <param name="fswdir">
        /// OUT: direction of main text
        /// </param>
        /// <param name="fHeaderFooterAtTopBottom">
        /// OUT: header/footer position on the page
        /// </param>
        /// <param name="durPage">
        /// OUT: page width
        /// </param>
        /// <param name="dvrPage">
        /// OUT: page height
        /// </param>
        /// <param name="fsrcMargin">
        /// OUT: rectangle within page margins
        /// </param>
        internal void GetPageDimensions(
            out uint fswdir,                
            out int fHeaderFooterAtTopBottom,
            out int durPage,                 
            out int dvrPage,                 
            ref PTS.FSRECT fsrcMargin)       
        {
            // Set page dimentions
            Size pageSize = _structuralCache.CurrentFormatContext.PageSize;
            durPage = TextDpi.ToTextDpi(pageSize.Width);
            dvrPage = TextDpi.ToTextDpi(pageSize.Height);

            // Set page margin
            Thickness pageMargin = _structuralCache.CurrentFormatContext.PageMargin;
            TextDpi.EnsureValidPageMargin(ref pageMargin, pageSize);
            fsrcMargin.u = TextDpi.ToTextDpi(pageMargin.Left);
            fsrcMargin.v = TextDpi.ToTextDpi(pageMargin.Top);
            fsrcMargin.du = durPage - TextDpi.ToTextDpi(pageMargin.Left + pageMargin.Right);
            fsrcMargin.dv = dvrPage - TextDpi.ToTextDpi(pageMargin.Top + pageMargin.Bottom);

            StructuralCache.PageFlowDirection = (FlowDirection)_structuralCache.PropertyOwner.GetValue(FrameworkElement.FlowDirectionProperty); 
            fswdir = PTS.FlowDirectionToFswdir(StructuralCache.PageFlowDirection);

            //      Needs Header/footer support.
            fHeaderFooterAtTopBottom = PTS.False;
        }

        /// <summary>
        /// Get justification properties
        /// </summary>
        /// <param name="rgnms">
        /// IN: array of the section names on the page
        /// </param>
        /// <param name="cnms">
        /// IN: number of sections on the page
        /// </param>
        /// <param name="fLastSectionNotBroken">
        /// IN: is last section on the page broken?
        /// </param>
        /// <param name="fJustify">
        /// OUT: apply justification/alignment to the page?
        /// </param>
        /// <param name="fskal">
        /// OUT: kind of vertical alignment for the page
        /// </param>
        /// <param name="fCancelAtLastColumn">
        /// OUT: cancel justification for the last column of the page?
        /// </param>
        internal unsafe void GetJustificationProperties(
            IntPtr* rgnms,                  
            int cnms,                        
            int fLastSectionNotBroken,      
            out int fJustify,                
            out PTS.FSKALIGNPAGE fskal,      
            out int fCancelAtLastColumn)     
        {
            // NOTE: use the first section to report values (array is only for word compat).
            fJustify = PTS.False;
            fCancelAtLastColumn = PTS.False;
            fskal = PTS.FSKALIGNPAGE.fskalpgTop;
        }

        /// <summary>
        /// Get next section
        /// </summary>
        /// <param name="fSuccess">
        /// OUT: next section exists
        /// </param>
        /// <param name="nmsNext">
        /// OUT: name of the next section
        /// </param>
        internal void GetNextSection(
            out int fSuccess,                
            out IntPtr nmsNext)              
        {
            fSuccess = PTS.False;
            nmsNext = IntPtr.Zero;
        }

        // ------------------------------------------------------------------
        // GetSectionProperties
        // ------------------------------------------------------------------
        
        /// <summary>
        /// Get section properties
        /// </summary>
        /// <param name="fNewPage">
        /// OUT: stop page before this section?
        /// </param>
        /// <param name="fswdir">
        /// OUT: direction of this section
        /// </param>
        /// <param name="fApplyColumnBalancing">
        /// OUT: apply column balancing to this section?
        /// </param>
        /// <param name="ccol">
        /// OUT: number of columns in the main text segment
        /// </param>
        /// <param name="cSegmentDefinedColumnSpanAreas">
        /// OUT: number of segment-defined columnspan areas
        /// </param>
        /// <param name="cHeightDefinedColumnSpanAreas">
        /// OUT: number of height-defined columnsapn areas
        /// </param>
        internal void GetSectionProperties(
            out int fNewPage,               
            out uint fswdir,                 
            out int fApplyColumnBalancing,   
            out int ccol,                    
            out int cSegmentDefinedColumnSpanAreas, 
            out int cHeightDefinedColumnSpanAreas)  
        {
            ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(Element);
            Size pageSize = _structuralCache.CurrentFormatContext.PageSize;
            double lineHeight = DynamicPropertyReader.GetLineHeightValue(Element);
            Thickness pageMargin = _structuralCache.CurrentFormatContext.PageMargin;
            double pageFontSize = (double)_structuralCache.PropertyOwner.GetValue(Block.FontSizeProperty);
            FontFamily pageFontFamily = (FontFamily)_structuralCache.PropertyOwner.GetValue(Block.FontFamilyProperty);
            bool enableColumns = _structuralCache.CurrentFormatContext.FinitePage;

            fNewPage = PTS.False; // Since only one section is supported, don't force page break before.
            fswdir = PTS.FlowDirectionToFswdir((FlowDirection)_structuralCache.PropertyOwner.GetValue(FrameworkElement.FlowDirectionProperty));
            fApplyColumnBalancing = PTS.False;
            ccol = PtsHelper.CalculateColumnCount(columnProperties, lineHeight, pageSize.Width - (pageMargin.Left + pageMargin.Right), pageFontSize, pageFontFamily, enableColumns);
            cSegmentDefinedColumnSpanAreas = 0;
            cHeightDefinedColumnSpanAreas = 0;
        }

        /// <summary>
        /// Get main TextSegment
        /// </summary>
        /// <param name="nmSegment">
        /// OUT: name of the main text segment for this section
        /// </param>
        internal void GetMainTextSegment(
            out IntPtr nmSegment)           
        {
            if (_mainTextSegment == null)
            {
                // Create the main text segment
                _mainTextSegment = new ContainerParagraph(Element, _structuralCache);
            }
            nmSegment = _mainTextSegment.Handle;
        }

        /// <summary>
        /// Get header segment
        /// </summary>
        /// <param name="pfsbrpagePrelim">
        /// IN: ptr to page break record of main page
        /// </param>
        /// <param name="fswdir">
        /// IN: direction for dvrMaxHeight/dvrFromEdge
        /// </param>
        /// <param name="fHeaderPresent">
        /// OUT: is there header on this page?
        /// </param>
        /// <param name="fHardMargin">
        /// OUT: does margin increase with header?
        /// </param>
        /// <param name="dvrMaxHeight">
        /// OUT: maximum size of header
        /// </param>
        /// <param name="dvrFromEdge">
        /// OUT: distance from top edge of the paper
        /// </param>
        /// <param name="fswdirHeader">
        /// OUT: direction for header
        /// </param>
        /// <param name="nmsHeader">
        /// OUT: name of header segment
        /// </param>
        internal void GetHeaderSegment(
            IntPtr pfsbrpagePrelim, 
            uint fswdir,                     
            out int fHeaderPresent,          
            out int fHardMargin,             
            out int dvrMaxHeight,            
            out int dvrFromEdge,             
            out uint fswdirHeader,           
            out IntPtr nmsHeader)            
        {
            fHeaderPresent = PTS.False;
            fHardMargin = PTS.False;
            dvrMaxHeight = dvrFromEdge = 0;
            fswdirHeader = fswdir;
            nmsHeader = IntPtr.Zero;
        }

        /// <summary>
        /// Get footer segment
        /// </summary>
        /// <param name="pfsbrpagePrelim">
        /// IN: ptr to page break record of main page
        /// </param>
        /// <param name="fswdir">
        /// IN: direction for dvrMaxHeight/dvrFromEdge
        /// </param>
        /// <param name="fFooterPresent">
        /// OUT: is there footer on this page?
        /// </param>
        /// <param name="fHardMargin">
        /// OUT: does margin increase with footer?
        /// </param>
        /// <param name="dvrMaxHeight">
        /// OUT: maximum size of footer
        /// </param>
        /// <param name="dvrFromEdge">
        /// OUT: distance from bottom edge of the paper
        /// </param>
        /// <param name="fswdirFooter">
        /// OUT: direction for footer
        /// </param>
        /// <param name="nmsFooter">
        /// OUT: name of footer segment
        /// </param>
        internal void GetFooterSegment(
            IntPtr pfsbrpagePrelim, 
            uint fswdir,                     
            out int fFooterPresent,          
            out int fHardMargin,             
            out int dvrMaxHeight,            
            out int dvrFromEdge,             
            out uint fswdirFooter,           
            out IntPtr nmsFooter)           
        {
            fFooterPresent = PTS.False;
            fHardMargin = PTS.False;
            dvrMaxHeight = dvrFromEdge = 0;
            fswdirFooter = fswdir;
            nmsFooter = IntPtr.Zero;
        }

        /// <summary>
        /// Get section column info
        /// </summary>
        /// <param name="fswdir">
        /// IN: direction of section
        /// </param>
        /// <param name="ncol">
        /// IN: size of the preallocated fscolinfo array
        /// </param>
        /// <param name="pfscolinfo">
        /// OUT: array of the colinfo structures
        /// </param>
        /// <param name="ccol">
        /// OUT: actual number of the columns in the segment
        /// </param>
        internal unsafe void GetSectionColumnInfo(
            uint fswdir,                    
            int ncol,                        
            PTS.FSCOLUMNINFO* pfscolinfo,    
            out int ccol)                    
        {
            ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(Element);
            Size pageSize = _structuralCache.CurrentFormatContext.PageSize;
            double lineHeight = DynamicPropertyReader.GetLineHeightValue(Element);
            Thickness pageMargin = _structuralCache.CurrentFormatContext.PageMargin;
            double pageFontSize = (double)_structuralCache.PropertyOwner.GetValue(Block.FontSizeProperty);
            FontFamily pageFontFamily = (FontFamily)_structuralCache.PropertyOwner.GetValue(Block.FontFamilyProperty);
            bool enableColumns = _structuralCache.CurrentFormatContext.FinitePage;

            ccol = ncol;
            PtsHelper.GetColumnsInfo(columnProperties, lineHeight, pageSize.Width - (pageMargin.Left + pageMargin.Right), pageFontSize, pageFontFamily, ncol, pfscolinfo, enableColumns);
        }

        /// <summary>
        /// Get end note segment
        /// </summary>
        /// <param name="fEndnotesPresent">
        /// OUT: are there endnotes for this segment?
        /// </param>
        /// <param name="nmsEndnotes">
        /// OUT: name of endnote segment
        /// </param>
        internal void GetEndnoteSegment(
            out int fEndnotesPresent,        
            out IntPtr nmsEndnotes)          
        {
            fEndnotesPresent = PTS.False;
            nmsEndnotes = IntPtr.Zero;
        }

        /// <summary>
        /// Get end note separators
        /// </summary>
        /// <param name="nmsEndnoteSeparator">
        /// OUT: name of the endnote separator segment
        /// </param>
        /// <param name="nmsEndnoteContSeparator">
        /// OUT: name of endnote cont separator segment
        /// </param>
        /// <param name="nmsEndnoteContNotice">
        /// OUT: name of the endnote cont notice segment
        /// </param>
        internal void GetEndnoteSeparators(
            out IntPtr nmsEndnoteSeparator,      
            out IntPtr nmsEndnoteContSeparator,  
            out IntPtr nmsEndnoteContNotice)    
        {
            nmsEndnoteSeparator = IntPtr.Zero;
            nmsEndnoteContSeparator = IntPtr.Zero;
            nmsEndnoteContNotice = IntPtr.Zero;
        }

        #endregion PTS callbacks

        // ------------------------------------------------------------------
        // 
        //  Internal Methods
        //
        // ------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Invalidate format caches accumulated in the section. 
        /// </summary>
        internal void InvalidateFormatCache()
        {
            if (_mainTextSegment != null)
            {
                _mainTextSegment.InvalidateFormatCache();
            }
        }

        /// <summary>
        /// Clear previously accumulated update info. 
        /// </summary>
        internal void ClearUpdateInfo()
        {
            if (_mainTextSegment != null)
            {
                _mainTextSegment.ClearUpdateInfo();
            }
        }

        /// <summary>
        /// Invalidate content's structural cache. 
        /// </summary>
        internal void InvalidateStructure()
        {
            if (_mainTextSegment != null)
            {
                DtrList dtrs = _structuralCache.DtrList;
                if (dtrs != null)
                {
                    _mainTextSegment.InvalidateStructure(dtrs[0].StartIndex);
                }
            }
        }

        /// <summary>
        /// Destroy content's structural cache. 
        /// </summary>
        internal void DestroyStructure()
        {
            if (_mainTextSegment != null)
            {
                _mainTextSegment.Dispose();
                _mainTextSegment = null;
            }
        }

        /// <summary>
        /// Update number of characters consumed by the main text segment. 
        /// </summary>
        internal void UpdateSegmentLastFormatPositions()
        {
            if(_mainTextSegment != null)
            {
                _mainTextSegment.UpdateLastFormatPositions();
            }
        }

        /// <summary>
        /// Can update section?
        /// </summary>
        internal bool CanUpdate
        {
            get 
            { 
                return _mainTextSegment != null; 
            }
        }

        /// <summary>
        /// StructuralCache. 
        /// </summary>
        internal StructuralCache StructuralCache
        {
            get 
            { 
                return _structuralCache; 
            }
        }

        /// <summary>
        /// Element owner. 
        /// </summary>
        internal DependencyObject Element
        {
            get 
            { 
                return _structuralCache.PropertyOwner;
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields
        
        /// <summary>
        /// Main text segment. 
        /// </summary>
        private BaseParagraph _mainTextSegment;

        /// <summary>
        /// Structural cache. 
        /// </summary>
        private readonly StructuralCache _structuralCache;

        #endregion Private Fields
    }
}
