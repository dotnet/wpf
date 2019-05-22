// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: BaseParagraph represents a paragraph corresponding to part
//              of a text based content.
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// BaseParagraph represents a paragraph corresponding to part of a text
    /// based content.
    /// </summary>
    internal abstract class BaseParagraph : UnmanagedHandle
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.    
        /// </summary>
        /// <param name="element">
        /// Element associated with paragraph.
        /// </param>
        /// <param name="structuralCache">
        /// Content's structural cache
        /// </param>
        protected BaseParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(structuralCache.PtsContext)
        {
            _element = element;
            _structuralCache = structuralCache;
            _changeType = PTS.FSKCHANGE.fskchNone;
            _stopAsking = false;

            UpdateLastFormatPositions();
        }

        #endregion Constructors

        // ------------------------------------------------------------------
        //
        //  PTS callbacks
        //
        // ------------------------------------------------------------------

        #region PTS callbacks

        /// <summary>
        /// UpdGetParaChange
        /// </summary>
        /// <param name="fskch">
        /// OUT: kind of change
        /// </param>
        /// <param name="fNoFurtherChanges">
        /// OUT: no changes after?
        /// </param>
        internal virtual void UpdGetParaChange(
            out PTS.FSKCHANGE fskch,             
            out int fNoFurtherChanges)           
        {
            fskch = _changeType;
            fNoFurtherChanges = PTS.FromBoolean(_stopAsking);

#if TEXTPANELLAYOUTDEBUG
            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                TextPanelDebug.Log("Para.UpdGetParaChange, Para=" + this.GetType().Name + " Change=" + _changeType.ToString(), TextPanelDebug.Category.ContentChange);
            }
#endif
        }

        /// <summary>
        /// Collapse margins
        /// </summary>
        /// <param name="paraClient">
        /// IN: Paragraph's para client
        /// </param>
        /// <param name="mcs">
        /// IN: input margin collapsing state
        /// </param>
        /// <param name="fswdir">
        /// IN: current direction (of the track, in which margin collapsing is happening)
        /// </param>
        /// <param name="suppressTopSpace">
        /// IN: suppress empty space at the top of page
        /// </param>
        /// <param name="dvr">
        /// OUT: dvr, calculated based on margin collapsing state
        /// </param>
        internal virtual void CollapseMargin(
            BaseParaClient paraClient,          
            MarginCollapsingState mcs,           
            uint fswdir,                         
            bool suppressTopSpace,               
            out int dvr)                        
        {
            // Suppress top space only in paginated scenarios.
            dvr = (mcs == null || (suppressTopSpace)) ? 0 : mcs.Margin;
        }

        /// <summary>
        /// Get Para Properties
        /// </summary>
        /// <param name="fspap">
        /// OUT: paragraph properties 
        /// </param>
        internal abstract void GetParaProperties(
            ref PTS.FSPAP fspap);               

        /// <summary>
        /// CreateParaclient
        /// </summary>
        /// <param name="pfsparaclient">
        /// OUT: opaque to PTS paragraph client
        /// </param>
        internal abstract void CreateParaclient(
            out IntPtr pfsparaclient);           

        #endregion PTS callbacks

        // ------------------------------------------------------------------
        //
        //  Internal Methods
        //
        // ------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Set update info. Those flags are used later by PTS to decide
        /// if paragraph needs to be updated and when to stop asking for
        /// update information.
        /// </summary>
        /// <param name="fskch">
        /// Type of change within the paragraph.
        /// </param>
        /// <param name="stopAsking">
        /// Synchronization point is reached?
        /// </param>
        internal virtual void SetUpdateInfo(PTS.FSKCHANGE fskch, bool stopAsking)
        {
            _changeType = fskch;
            _stopAsking = stopAsking;
        }

        /// <summary>
        /// Clear previously accumulated update info.
        /// </summary>
        internal virtual void ClearUpdateInfo()
        {
            _changeType = PTS.FSKCHANGE.fskchNone;
            _stopAsking = true;
        }

        /// <summary>
        /// Invalidate content's structural cache. Returns: 'true' if entire paragraph 
        /// is invalid.
        /// </summary>
        /// <param name="startPosition">
        /// Position to start invalidation from.
        /// </param>
        internal virtual bool InvalidateStructure(int startPosition)
        {
            Debug.Assert(ParagraphEndCharacterPosition >= startPosition);
            int openEdgeCp = TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.BeforeStart);
            return (openEdgeCp == startPosition);
        }

        /// <summary>
        /// Invalidate accumulated format caches.
        /// </summary>
        internal virtual void InvalidateFormatCache() 
        { 
        }

        /// <summary>
        /// Update number of characters consumed by the paragraph. 
        /// </summary>
        internal void UpdateLastFormatPositions()
        {
            _lastFormatCch = Cch;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Retrieve PTS paragraph properties.
        /// </summary>
        /// <param name="fspap">
        /// Paragraph properties to initialize.
        /// </param>
        /// <param name="ignoreElementProps">
        /// Ignore element properties?
        /// </param>
        protected void GetParaProperties(ref PTS.FSPAP fspap, bool ignoreElementProps)
        {
            if (!ignoreElementProps)
            {
                fspap.fKeepWithNext      = PTS.FromBoolean(DynamicPropertyReader.GetKeepWithNext(_element));
                // Can be broken only if Block.BreakPageBefore is set
                fspap.fBreakPageBefore   = _element is Block ? PTS.FromBoolean(StructuralCache.CurrentFormatContext.FinitePage && ((Block)_element).BreakPageBefore) : PTS.FromBoolean(false);
                // Can be broken only if Block.BreakColumnBefore is set
                fspap.fBreakColumnBefore = _element is Block ? PTS.FromBoolean(((Block)_element).BreakColumnBefore) : PTS.FromBoolean(false);
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Properties / Fields
        //
        //-------------------------------------------------------------------

        #region Properties / Fields

        /// <summary>
        /// CharacterPosition at the start of paragraph.
        /// </summary>
        internal int ParagraphStartCharacterPosition
        {
            get
            {
                // This is done here, rather than deriving for two reasons - This is special cased for text paragraph - no other 
                // paras should use their cps in this way, and this is also not a virtual method, so can be used from C'Tor.
                if(this is TextParagraph)
                {
                    return TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.AfterStart);
                }
                else
                {
                    return TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.BeforeStart);
                }
            }
        }

        /// <summary>
        /// CharacterPosition at the end of paragraph. The first character
        /// that does not belong to the paragraph.
        /// </summary>
        internal int ParagraphEndCharacterPosition
        {
            get
            {
                // This is done here, rather than deriving for two reasons - This is special cased for text paragraph - no other 
                // paras should use their cps in this way, and this is also not a virtual method, so can be used from C'Tor.
                if(this is TextParagraph)
                {
                    return TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.BeforeEnd);
                }
                else
                {
                    return TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.AfterEnd);
                }
            }
        }

        /// <summary>
        /// Incremental update information for the paragraph.
        /// Those fields are used only during incremental update process. 
        /// </summary>
        protected PTS.FSKCHANGE _changeType;
        protected bool _stopAsking;

        /// <summary>
        /// Number of characters consumed by the paragraph.
        /// </summary>
        internal int Cch 
        { 
            get 
            {
                int cch = TextContainerHelper.GetCchFromElement(StructuralCache.TextContainer, Element);

                // This is done here, rather than deriving for two reasons - This is special cased for text paragraph - no other 
                // paras should use their cps in this way, and this is also not a virtual method, so can be used from C'Tor.
                if (this is TextParagraph && Element is TextElement)
                {
                    Invariant.Assert(cch >= 2);
                    cch -= 2;
                }
                
                return cch;
            } 
        }

        /// <summary>
        /// Cch paragraph had during last format
        /// </summary>
        internal int LastFormatCch
        {
            get
            {
                return _lastFormatCch;
            }
        }

        protected int _lastFormatCch;


        /// <summary>
        /// The next and previous sibling in paragraph list. 
        /// </summary>
        internal BaseParagraph Next;
        internal BaseParagraph Previous;


        /// <summary>
        /// Content's structural cache.
        /// </summary>
        internal StructuralCache StructuralCache 
        { 
            get 
            { 
                return _structuralCache; 
            } 
        }
        protected readonly StructuralCache _structuralCache;

        /// <summary>
        /// Object associated with the paragraph. 
        /// </summary>
        internal DependencyObject Element 
        { 
            get 
            { 
                return _element; 
            } 
        }
        protected readonly DependencyObject _element;

        #endregion Properties / Fields
    }
}
