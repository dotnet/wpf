// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: XamlToRtfWriter write Rtf content from Xaml content.
//

using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Windows.Media; // Color
using System.Globalization;
using System.IO;
using MS.Internal.Globalization;

#if WindowsMetaFile // GetWinMetaFileBits
using System.Runtime.InteropServices;
using MS.Win32;
#endif // WindowsMetaFile

namespace System.Windows.Documents
{
    /// <summary>
    /// XamlToRtfWriter will write the rtf content that based on converting
    /// from xaml content.
    /// </summary>
    internal class XamlToRtfWriter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        internal XamlToRtfWriter(string xaml)
        {
            _xaml = xaml;

            _rtfBuilder = new StringBuilder();

            _xamlIn = new XamlIn(this, xaml);

            _converterState = new ConverterState();

            // Initialize the reader state with necessary colors and fonts for defaults
            ColorTable colorTable = _converterState.ColorTable;
            colorTable.AddColor(Color.FromArgb(0xff, 0, 0, 0));
            colorTable.AddColor(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

            FontTable fontTable = _converterState.FontTable;
            FontTableEntry fontTableEntry = fontTable.DefineEntry(0);
            fontTableEntry.Name = "Times New Roman";
            fontTableEntry.ComputePreferredCodePage();
        }

        #endregion Constructors

        // ---------------------------------------------------------------------
        //
        // internal Methods
        //
        // ---------------------------------------------------------------------

        #region internal Methods

        /// <summary>
        /// Start the processing of the XamlToRtf converter.
        /// </summary>
        /// <returns></returns>
        internal XamlToRtfError Process()
        {
            XamlToRtfError xamlToRtfError = XamlToRtfError.None;

            // Do the parse of Xaml
            xamlToRtfError = _xamlIn.Parse();

            // Make the array of nodes be a "tree"
            XamlParserHelper.EnsureParagraphClosed(_converterState);
            _converterState.DocumentNodeArray.EstablishTreeRelationships();

            // Now roll it up
            WriteOutput();

            return xamlToRtfError;
        }

        #endregion internal Methods

        // ---------------------------------------------------------------------
        //
        // internal Properties
        //
        // ---------------------------------------------------------------------

        #region internal Properties

        internal string Output
        {
            get
            {
                return _rtfBuilder.ToString();
            }
        }

        internal bool GenerateListTables
        {
            get
            {
                return _xamlIn.GenerateListTables;
            }
        }

        // WpfPayload package that containing the image for the specified Xaml
        internal WpfPayload WpfPayload
        {
            set
            {
                _wpfPayload = value;
            }
        }

        #endregion internal Properties

        // ---------------------------------------------------------------------
        //
        // Internal Properties
        //
        // ---------------------------------------------------------------------

        #region Internal Properties

        internal ConverterState ConverterState
        {
            get
            {
                return _converterState;
            }
        }

        #endregion Internal Properties

        // ---------------------------------------------------------------------
        //
        // Private Methods
        //
        // ---------------------------------------------------------------------

        #region Private Methods

        private void BuildListTable()
        {
            ListLevelTable[] levels = new ListLevelTable[9];

            int i, j;

            for (i = 0; i < 9; i++)
            {
                levels[i] = new ListLevelTable();
            }

            // Tracks current open list
            ArrayList openLists = new ArrayList();

            // Find paragraphs in lists and build up the necessary list styles
            int nListStyles = BuildListStyles(levels, openLists);

            // Now build the actual list style and list override tables.  Basic approach is that each list style has
            // a list override.
            ListOverrideTable listOverrideTable = _converterState.ListOverrideTable;

            for (i = 0; i < nListStyles; i++)
            {
                ListOverride listOverride = listOverrideTable.AddEntry();

                listOverride.ID = i + 1;
                listOverride.Index = i + 1;
            }

            ListTable listTable = _converterState.ListTable;

            for (i = 0; i < nListStyles; i++)
            {
                ListTableEntry listTableEntry = listTable.AddEntry();

                listTableEntry.ID = i + 1;

                ListLevelTable listLevelTable = listTableEntry.Levels;

                for (j = 0; j < 9; j++)
                {
                    ListLevel listLevel = listLevelTable.AddEntry();

                    ListLevelTable lltComputed = levels[j];

                    if (lltComputed.Count > i)
                    {
                        ListLevel llComputed = lltComputed.EntryAt(i);

                        listLevel.Marker = llComputed.Marker;
                        listLevel.StartIndex = llComputed.StartIndex;
                    }
                }
            }
        }

        private int BuildListStyles(ListLevelTable[] levels, ArrayList openLists)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            int i;
            int j;

            // Tracks when the innermost list goes out of scope
            int nEndList = -1;

            // Tracks if this is the first list item in the list
            bool bFirst = false;

            int nListStyles = 0;

            for (i = 0; i < dna.Count; i++)
            {
                // Did the innermost list go out of scope?
                while (i == nEndList)
                {
                    Debug.Assert(openLists.Count > 0);
                    if (openLists.Count > 0)
                    {
                        openLists.RemoveRange(openLists.Count - 1, 1);
                        if (openLists.Count > 0)
                        {
                            DocumentNode dn1 = (DocumentNode)openLists[openLists.Count - 1];
                            nEndList = dn1.Index + dn1.ChildCount + 1;
                        }
                        else
                        {
                            nEndList = -1;
                        }
                    }
                    else
                    {
                        nEndList = -1;
                    }
                }

                // OK, handle lists, listitems and paragraphs
                // At the end of this, paragraphs will have their ILVL and ILS properties set to
                // correspond to the entries in the list table.
                // I also store the nested depth of the lists in certain properties in the list node FormatState.
                // So:
                //      list.FormatState.PNLVL will have the max list depth of any paragraph under the list.
                //      list.FormatState.ILS is:
                //                                  -1 when unset
                //                                  0 if there is a conflict for any set of paragraphs underneath
                //                                  >0 (value of ILS) for paragraphs underneath it.
                //
                DocumentNode dn = dna.EntryAt(i);

                switch (dn.Type)
                {
                    case DocumentNodeType.dnList:
                        openLists.Add(dn);
                        nEndList = dn.Index + dn.ChildCount + 1;
                        bFirst = true;
                        break;

                    case DocumentNodeType.dnListItem:
                        bFirst = true;
                        break;

                    case DocumentNodeType.dnParagraph:
                        if (bFirst && openLists.Count > 0)
                        {
                            bFirst = false;
                            DocumentNode dnList = (DocumentNode)openLists[openLists.Count - 1];
                            int iLevel = openLists.Count;
                            MarkerStyle marker = dnList.FormatState.Marker;
                            long nStartIndex = dnList.FormatState.StartIndex;
                            if (nStartIndex < 0)
                            {
                                nStartIndex = 1;
                            }
                            if (iLevel > 9)
                            {
                                iLevel = 9;
                            }

                            ListLevelTable listLevelTable = levels[iLevel - 1];
                            ListLevel listLevel;

                            for (j = 0; j < listLevelTable.Count; j++)
                            {
                                listLevel = listLevelTable.EntryAt(j);

                                if (listLevel.Marker == marker && listLevel.StartIndex == nStartIndex)
                                {
                                    break;
                                }
                            }

                            if (j == listLevelTable.Count)
                            {
                                listLevel = listLevelTable.AddEntry();

                                listLevel.Marker = marker;
                                listLevel.StartIndex = nStartIndex;

                                // Remember max number of different styles used.
                                if (listLevelTable.Count > nListStyles)
                                {
                                    nListStyles = listLevelTable.Count;
                                }
                            }

                            if (iLevel > 1)
                                dn.FormatState.ILVL = iLevel - 1;

                            dn.FormatState.ILS = j + 1;

                            for (j = 0; j < openLists.Count; j++)
                            {
                                dnList = (DocumentNode)openLists[j];

                                if (dnList.FormatState.PNLVL < iLevel)
                                {
                                    dnList.FormatState.PNLVL = iLevel;
                                }
                                if (dnList.FormatState.ILS == -1)
                                {
                                    dnList.FormatState.ILS = dn.FormatState.ILS;
                                }
                                else if (dnList.FormatState.ILS != dn.FormatState.ILS)
                                {
                                    dnList.FormatState.ILS = 0;
                                }
                            }
                        }
                        break;
                }
            }

            return nListStyles;
        }

        private void MergeParagraphMargins()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            // In RTF, the paragraph owns the margins for containing elements.  Walk through the document and
            // for each paragraph, walk up adding any list-item and list margins to the paragraphs margins.

            for (int i = 0; i < dna.Count; i++)
            {
                DocumentNode dn = dna.EntryAt(i);

                if (dn.Type == DocumentNodeType.dnParagraph)
                {
                    long li = dn.FormatState.LI;
                    long ri = dn.FormatState.RI;

                    for (DocumentNode dnParent = dn.Parent; dnParent != null; dnParent = dnParent.Parent)
                    {
                        // Computation halts at cell boundary.
                        if (dnParent.Type == DocumentNodeType.dnCell)
                        {
                            break;
                        }
                        if (dnParent.Type == DocumentNodeType.dnListItem || dnParent.Type == DocumentNodeType.dnList)
                        {
                            li += dnParent.FormatState.LI;
                            ri += dnParent.FormatState.RI;
                        }
                    }

                    dn.FormatState.LI = li;
                    dn.FormatState.RI = ri;
                }
            }
        }

        private void GenerateListLabels()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            // Tracks current open list
            ArrayList openLists = new ArrayList();

            // Tracks listitem number for the open list
            long[] openCounts = new long[dna.Count];
            long[] openStarts = new long[dna.Count];

            // Tracks when the innermost list goes out of scope
            int nEndList = -1;

            for (int i = 0; i < dna.Count; i++)
            {
                // Did the innermost list go out of scope?
                while (i == nEndList)
                {
                    Debug.Assert(openLists.Count > 0);
                    if (openLists.Count > 0)
                    {
                        openLists.RemoveRange(openLists.Count - 1, 1);
                        if (openLists.Count > 0)
                        {
                            DocumentNode dn1 = (DocumentNode)openLists[openLists.Count - 1];
                            nEndList = dn1.Index + dn1.ChildCount + 1;
                        }
                        else
                            nEndList = -1;
                    }
                    else
                        nEndList = -1;
                }

                // OK, handle lists, listitems and paragraphs
                DocumentNode dn = dna.EntryAt(i);

                switch (dn.Type)
                {
                    case DocumentNodeType.dnList:
                        openLists.Add(dn);

                        // Record StartIndex - 1 so I can just increment at first ListItem
                        openCounts[openLists.Count - 1] = dn.FormatState.StartIndex - 1;
                        openStarts[openLists.Count - 1] = dn.FormatState.StartIndex;

                        nEndList = dn.Index + dn.ChildCount + 1;
                        break;
                    case DocumentNodeType.dnListItem:
                        Debug.Assert(openLists.Count > 0);

                        // Increment current listitem number
                        if (openLists.Count > 0)
                        {
                            openCounts[openLists.Count - 1] = openCounts[openLists.Count - 1] + 1;
                        }
                        break;
                    case DocumentNodeType.dnParagraph:
                        if (dn.FormatState.ListLevel > 0 && openLists.Count > 0)
                        {
                            DocumentNode dnList = (DocumentNode)openLists[openLists.Count - 1];
                            long nCount = openCounts[openLists.Count - 1];
                            long nStart = openStarts[openLists.Count - 1];
                            dn.FormatState.StartIndex = nStart; // Record this here for later use in \\pnstart
                            dn.ListLabel = Converters.MarkerCountToString(dnList.FormatState.Marker, nCount);
                        }
                        break;
                }
            }
        }

        private void SetParagraphStructureProperties()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            for (int i = 0; i < dna.Count; i++)
            {
                DocumentNode dn = dna.EntryAt(i);

                if (dn.Type == DocumentNodeType.dnParagraph)
                {
                    // Table properties
                    long iLevel = 0;

                    for (DocumentNode dnParent = dn.Parent;
                         dnParent != null;
                         dnParent = dnParent.Parent)
                    {
                        if (dnParent.Type == DocumentNodeType.dnCell)
                        {
                            iLevel++;
                        }
                    }

                    if (iLevel > 1)
                    {
                        dn.FormatState.ITAP = iLevel;
                    }
                    if (iLevel != 0)
                    {
                        dn.FormatState.IsInTable = true;
                    }
                }
            }
        }

        private void WriteProlog()
        {
            // Note htmautsp defines HTML (XAML) compatible margin collapsing on paragraphs
            _rtfBuilder.Append("{\\rtf1\\ansi\\ansicpg1252\\uc1\\htmautsp");

            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            for (int i = 0; i < dna.Count; i++)
            {
                DocumentNode dn = dna.EntryAt(i);

                if (dn.FormatState.Font >= 0)
                {
                    _rtfBuilder.Append("\\deff");
                    _rtfBuilder.Append(dn.FormatState.Font.ToString(CultureInfo.InvariantCulture));
                    break;
                }
            }
        }

        private void WriteHeaderTables()
        {
            WriteFontTable();
            WriteColorTable();

            if (GenerateListTables)
            {
                WriteListTable();
            }
        }

        private void WriteFontTable()
        {
            // Font Table
            FontTable fontTable = _converterState.FontTable;
            int i;

            _rtfBuilder.Append("{\\fonttbl");

            for (i = 0; i < fontTable.Count; i++)
            {
                FontTableEntry entry = fontTable.EntryAt(i);

                _rtfBuilder.Append("{");
                _rtfBuilder.Append("\\f");
                _rtfBuilder.Append(entry.Index.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append("\\fcharset");
                _rtfBuilder.Append(entry.CharSet.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append(" ");
                XamlParserHelper.AppendRTFText(_rtfBuilder, entry.Name, entry.CodePage);
                _rtfBuilder.Append(";}");
            }

            _rtfBuilder.Append("}");
        }

        private void WriteColorTable()
        {
            // Color Table
            ColorTable colorTable = _converterState.ColorTable;

            _rtfBuilder.Append("{\\colortbl");

            for (int i = 0; i < colorTable.Count; i++)
            {
                Color color = colorTable.ColorAt(i);

                _rtfBuilder.Append("\\red");
                _rtfBuilder.Append(color.R.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append("\\green");
                _rtfBuilder.Append(color.G.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append("\\blue");
                _rtfBuilder.Append(color.B.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append(";");
            }

            _rtfBuilder.Append("}");
        }

        private void WriteListTable()
        {
            // List Tables
            ListTable listTable = _converterState.ListTable;

            if (listTable.Count > 0)
            {
                _rtfBuilder.Append("\r\n{\\*\\listtable");
                int nID = 5;

                for (int i = 0; i < listTable.Count; i++)
                {
                    ListTableEntry listTableEntry = listTable.EntryAt(i);

                    _rtfBuilder.Append("\r\n{\\list");
                    _rtfBuilder.Append("\\listtemplateid");
                    _rtfBuilder.Append(listTableEntry.ID.ToString(CultureInfo.InvariantCulture));
                    _rtfBuilder.Append("\\listhybrid");

                    ListLevelTable listLevelTable = listTableEntry.Levels;

                    for (int j = 0; j < listLevelTable.Count; j++)
                    {
                        ListLevel listLevel = listLevelTable.EntryAt(j);
                        long lMarker = (long)listLevel.Marker;

                        _rtfBuilder.Append("\r\n{\\listlevel");
                        _rtfBuilder.Append("\\levelnfc");
                        _rtfBuilder.Append(lMarker.ToString(CultureInfo.InvariantCulture));
                        _rtfBuilder.Append("\\levelnfcn");
                        _rtfBuilder.Append(lMarker.ToString(CultureInfo.InvariantCulture));
                        _rtfBuilder.Append("\\leveljc0");
                        _rtfBuilder.Append("\\leveljcn0");
                        _rtfBuilder.Append("\\levelfollow0");
                        _rtfBuilder.Append("\\levelstartat");
                        _rtfBuilder.Append(listLevel.StartIndex);
                        _rtfBuilder.Append("\\levelspace0");
                        _rtfBuilder.Append("\\levelindent0");
                        _rtfBuilder.Append("{\\leveltext");
                        _rtfBuilder.Append("\\leveltemplateid");
                        _rtfBuilder.Append(nID.ToString(CultureInfo.InvariantCulture));
                        nID++;
                        if (listLevel.Marker == MarkerStyle.MarkerBullet)
                        {
                            _rtfBuilder.Append("\\'01\\'b7}");
                            _rtfBuilder.Append("{\\levelnumbers;}");
                        }
                        else
                        {
                            _rtfBuilder.Append("\\'02\\'0");
                            _rtfBuilder.Append(j.ToString(CultureInfo.InvariantCulture));
                            _rtfBuilder.Append(".;}");
                            _rtfBuilder.Append("{\\levelnumbers\\'01;}");
                        }
                        _rtfBuilder.Append("\\fi-360");      // 1/4" from bullet
                        _rtfBuilder.Append("\\li");
                        string indent = ((j + 1) * 720).ToString(CultureInfo.InvariantCulture);
                        _rtfBuilder.Append(indent);
                        _rtfBuilder.Append("\\lin");
                        _rtfBuilder.Append(indent);
                        _rtfBuilder.Append("\\jclisttab\\tx");
                        _rtfBuilder.Append(indent);
                        _rtfBuilder.Append("}");
                    }

                    _rtfBuilder.Append("\r\n{\\listname ;}");
                    _rtfBuilder.Append("\\listid");
                    _rtfBuilder.Append(listTableEntry.ID.ToString(CultureInfo.InvariantCulture));
                    _rtfBuilder.Append("}");
                }

                _rtfBuilder.Append("}\r\n");
            }

            ListOverrideTable listOverrideTable = _converterState.ListOverrideTable;

            if (listOverrideTable.Count > 0)
            {
                _rtfBuilder.Append("{\\*\\listoverridetable");

                for (int i = 0; i < listOverrideTable.Count; i++)
                {
                    ListOverride lo = listOverrideTable.EntryAt(i);

                    _rtfBuilder.Append("\r\n{\\listoverride");
                    _rtfBuilder.Append("\\listid");
                    _rtfBuilder.Append(lo.ID.ToString(CultureInfo.InvariantCulture));
                    _rtfBuilder.Append("\\listoverridecount0");
                    if (lo.StartIndex > 0)
                    {
                        _rtfBuilder.Append("\\levelstartat");
                        _rtfBuilder.Append(lo.StartIndex.ToString(CultureInfo.InvariantCulture));
                    }
                    _rtfBuilder.Append("\\ls");
                    _rtfBuilder.Append(lo.Index.ToString(CultureInfo.InvariantCulture));
                    _rtfBuilder.Append("}");
                }

                _rtfBuilder.Append("\r\n}\r\n");
            }
        }

        private void WriteEmptyChild(DocumentNode documentNode)
        {
            switch (documentNode.Type)
            {
                case DocumentNodeType.dnLineBreak:
                    _rtfBuilder.Append("\\line ");
                    break;
            }
        }

        private void WriteInlineChild(DocumentNode documentNode)
        {
            // Handle empty nodes first
            if (documentNode.IsEmptyNode)
            {
                WriteEmptyChild(documentNode);
                return;
            }

            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            FormatState fsThis = documentNode.FormatState;
            FormatState fsParent = documentNode.Parent != null
                                        ? documentNode.Parent.FormatState
                                        : FormatState.EmptyFormatState;

            bool outFont = fsThis.Font != fsParent.Font;
            bool outBold = fsThis.Bold != fsParent.Bold;
            bool outItalic = fsThis.Italic != fsParent.Italic;
            bool outUL = fsThis.UL != fsParent.UL;
            bool outFontSize = fsThis.FontSize != fsParent.FontSize;
            bool outCF = fsThis.CF != fsParent.CF;
            bool outCB = fsThis.CB != fsParent.CB;
            bool outS = fsThis.Strike != fsParent.Strike;
            bool outSuper = fsThis.Super != fsParent.Super;
            bool outSub = fsThis.Sub != fsParent.Sub;
            bool outLang = fsThis.Lang != fsParent.Lang && fsThis.Lang > 0;
            bool outDir = fsThis.DirChar != DirState.DirDefault
                            && (documentNode.Parent == null
                                || !documentNode.Parent.IsInline
                                || fsThis.Lang != fsParent.Lang);
            bool outAny = outFont || outBold || outItalic || outUL || outLang || outDir ||
                          outFontSize || outCF || outCB || outS || outSuper || outSub;

            // Start a context so any properties only apply here
            if (outAny)
            {
                _rtfBuilder.Append("{");
            }

            // Write properties
            if (outLang)
            {
                _rtfBuilder.Append("\\lang");
                _rtfBuilder.Append(fsThis.Lang.ToString(CultureInfo.InvariantCulture));
            }
            if (outFont)
            {
                _rtfBuilder.Append("\\loch");
                _rtfBuilder.Append("\\f");
                _rtfBuilder.Append(fsThis.Font.ToString(CultureInfo.InvariantCulture));
            }
            if (outBold)
            {
                if (fsThis.Bold)
                {
                    _rtfBuilder.Append("\\b");
                }
                else
                {
                    _rtfBuilder.Append("\\b0");
                }
            }
            if (outItalic)
            {
                if (fsThis.Italic)
                {
                    _rtfBuilder.Append("\\i");
                }
                else
                {
                    _rtfBuilder.Append("\\i0");
                }
            }
            if (outUL)
            {
                if (fsThis.UL != ULState.ULNone)
                {
                    _rtfBuilder.Append("\\ul");
                }
                else
                {
                    _rtfBuilder.Append("\\ul0");
                }
            }
            if (outS)
            {
                if (fsThis.Strike != StrikeState.StrikeNone)
                {
                    _rtfBuilder.Append("\\strike");
                }
                else
                {
                    _rtfBuilder.Append("\\strike0");
                }
            }
            if (outFontSize)
            {
                _rtfBuilder.Append("\\fs");
                _rtfBuilder.Append(fsThis.FontSize.ToString(CultureInfo.InvariantCulture));
            }
            if (outCF)
            {
                _rtfBuilder.Append("\\cf");
                _rtfBuilder.Append(fsThis.CF.ToString(CultureInfo.InvariantCulture));
            }
            if (outCB)
            {
                _rtfBuilder.Append("\\highlight");
                _rtfBuilder.Append(fsThis.CB.ToString(CultureInfo.InvariantCulture));
            }
            if (outSuper)
            {
                if (fsThis.Super)
                {
                    _rtfBuilder.Append("\\super");
                }
                else
                {
                    _rtfBuilder.Append("\\super0");
                }
            }
            if (outSub)
            {
                if (fsThis.Sub)
                {
                    _rtfBuilder.Append("\\sub");
                }
                else
                {
                    _rtfBuilder.Append("\\sub0");
                }
            }
            if (outDir)
            {
                if (fsThis.DirChar == DirState.DirLTR)
                {
                    _rtfBuilder.Append("\\ltrch");
                }
                else
                {
                    _rtfBuilder.Append("\\rtlch");
                }
            }

            // Ensure space delimiter after control word
            if (outAny)
            {
                _rtfBuilder.Append(" ");
            }

            // Write contents here
            if (documentNode.Type == DocumentNodeType.dnHyperlink && !string.IsNullOrEmpty(documentNode.NavigateUri))
            {
                _rtfBuilder.Append("{\\field{\\*\\fldinst { HYPERLINK \"");

                // Unescape the escape sequences added in Xaml
                documentNode.NavigateUri = BamlResourceContentUtil.UnescapeString(documentNode.NavigateUri);

                // Add the additional backslash which rtf expected
                for (int i = 0; i < documentNode.NavigateUri.Length; i++)
                {
                    if (documentNode.NavigateUri[i] == '\\')
                    {
                        _rtfBuilder.Append("\\\\");
                    }
                    else
                    {
                        _rtfBuilder.Append(documentNode.NavigateUri[i]);
                    }
                }

                _rtfBuilder.Append("\" }}{\\fldrslt {");
            }
            else
            {
                _rtfBuilder.Append(documentNode.Content);
            }

            if (documentNode.Type == DocumentNodeType.dnImage)
            {
                // Write image control and image hex data to the rtf content
                WriteImage(documentNode);
            }

            // Write child contents
            int nIndex = documentNode.Index;
            int nStart = nIndex + 1;
            
            for (; nStart <= nIndex + documentNode.ChildCount; nStart++)
            {
                DocumentNode documentNodeChild = dna.EntryAt(nStart);

                // Ignore non-direct children - they get written out by their parent
                if (documentNodeChild.Parent == documentNode)
                {
                    WriteInlineChild(documentNodeChild);
                }
            }

            // Terminate contents here
            if (documentNode.Type == DocumentNodeType.dnHyperlink && !string.IsNullOrEmpty(documentNode.NavigateUri))
            {
                _rtfBuilder.Append("}}}");
            }

            // End context
            if (outAny)
            {
                _rtfBuilder.Append("}");
            }
        }

        private void WriteUIContainerChild(DocumentNode documentNode)
        {
            _rtfBuilder.Append("{");

            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            // Write child contents
            int nIndex = documentNode.Index;
            int nStart = nIndex + 1;

            for (; nStart <= nIndex + documentNode.ChildCount; nStart++)
            {
                DocumentNode documentNodeChild = dna.EntryAt(nStart);

                // Ignore non-direct children - they get written out by their parent
                if (documentNodeChild.Parent == documentNode && documentNodeChild.Type == DocumentNodeType.dnImage)
                {
                    // Write image control and image hex data to the rtf content
                    WriteImage(documentNodeChild);
                }
            }

            if (documentNode.Type == DocumentNodeType.dnBlockUIContainer)
            {
                _rtfBuilder.Append("\\par");
            }

            // Close Section writing
            _rtfBuilder.Append("}");
            _rtfBuilder.Append("\r\n");
        }

        private void WriteSection(DocumentNode dnThis)
        {
            int nIndex = dnThis.Index;
            int nStart = nIndex + 1;
            int nAt;

            FormatState fsThis = dnThis.FormatState;
            FormatState fsParent = dnThis.Parent != null ? dnThis.Parent.FormatState : FormatState.EmptyFormatState;
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            _rtfBuilder.Append("{");

            // CultureInfo
            if (fsThis.Lang != fsParent.Lang && fsThis.Lang > 0)
            {
                _rtfBuilder.Append("\\lang");
                _rtfBuilder.Append(fsThis.Lang.ToString(CultureInfo.InvariantCulture));
            }

            // FlowDirection
            if (fsThis.DirPara == DirState.DirRTL)
            {
                _rtfBuilder.Append("\\rtlpar");
            }

            // Write the font information
            if (WriteParagraphFontInfo(dnThis, fsThis, fsParent))
            {
                _rtfBuilder.Append(" ");
            }

            // Foreground
            if (fsThis.CF != fsParent.CF)
            {
                _rtfBuilder.Append("\\cf");
                _rtfBuilder.Append(fsThis.CF.ToString(CultureInfo.InvariantCulture));
            }

            // TextAlignment
            switch (fsThis.HAlign)
            {
                case HAlign.AlignLeft:
                    if (fsThis.DirPara != DirState.DirRTL)
                    {
                        _rtfBuilder.Append("\\ql");
                    }
                    else
                    {
                        _rtfBuilder.Append("\\qr");
                    }
                    break;

                case HAlign.AlignRight:
                    if (fsThis.DirPara != DirState.DirRTL)
                    {
                        _rtfBuilder.Append("\\qr");
                    }
                    else
                    {
                        _rtfBuilder.Append("\\ql");
                    }
                    break;

                case HAlign.AlignCenter:
                    _rtfBuilder.Append("\\qc");
                    break;

                case HAlign.AlignJustify:
                    _rtfBuilder.Append("\\qj");
                    break;
            }

            // LineHeight
            if (fsThis.SL != 0)
            {
                _rtfBuilder.Append("\\sl");
                _rtfBuilder.Append(fsThis.SL.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append("\\slmult0");
            }

            // Now write out the direct children.
            for (nAt = nStart; nAt <= nIndex + dnThis.ChildCount; nAt++)
            {
                DocumentNode dnChild = dna.EntryAt(nAt);

                // Ignore non-direct children - they get written out by their parent
                if (dnChild.Parent == dnThis)
                {
                    WriteStructure(dnChild);
                }
            }

            // Close Section writing
            _rtfBuilder.Append("}");
            _rtfBuilder.Append("\r\n");
        }

        private void WriteParagraph(DocumentNode dnThis)
        {
            int nIndex = dnThis.Index;
            int nStart = nIndex + 1;
            int nAt;

            FormatState fsThis = dnThis.FormatState;
            FormatState fsParent = dnThis.Parent != null ? dnThis.Parent.FormatState : FormatState.EmptyFormatState;
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            _rtfBuilder.Append("{");

            bool bOutControl = WriteParagraphFontInfo(dnThis, fsThis, fsParent);

            // Structure properties
            // NB: RE 4.01 seems to require \intbl keyword to come before inline content
            if (fsThis.IsInTable)
            {
                _rtfBuilder.Append("\\intbl");
                bOutControl = true;
            }
            if (bOutControl)
            {
                _rtfBuilder.Append(" ");
            }

            bOutControl = WriteParagraphListInfo(dnThis, fsThis);
            if (bOutControl)
            {
                _rtfBuilder.Append(" ");
            }

            // FlowDirection control - state it before writing nested inline node.
            // MsWord expect "rtlpar" control before writing inline, but Wordpad
            // doesn't matter state it before or after of inline writing.
            if (fsThis.DirPara == DirState.DirRTL)
            {
                _rtfBuilder.Append("\\rtlpar");
            }

            // OK, now write out the inline children.
            for (nAt = nStart; nAt <= nIndex + dnThis.ChildCount; nAt++)
            {
                DocumentNode dnChild = dna.EntryAt(nAt);

                // Ignore non-direct children - they get written out by their parent
                if (dnChild.Parent == dnThis)
                {
                    WriteInlineChild(dnChild);
                }
            }

            // Structure properties
            if (fsThis.ITAP > 1)
            {
                _rtfBuilder.Append("\\itap");
                _rtfBuilder.Append(fsThis.ITAP.ToString(CultureInfo.InvariantCulture));
            }

            // Margins
            _rtfBuilder.Append("\\li");
            _rtfBuilder.Append(fsThis.LI.ToString(CultureInfo.InvariantCulture));
            _rtfBuilder.Append("\\ri");
            _rtfBuilder.Append(fsThis.RI.ToString(CultureInfo.InvariantCulture));
            _rtfBuilder.Append("\\sa");
            _rtfBuilder.Append(fsThis.SA.ToString(CultureInfo.InvariantCulture));
            _rtfBuilder.Append("\\sb");
            _rtfBuilder.Append(fsThis.SB.ToString(CultureInfo.InvariantCulture));

            // Borders
            if (fsThis.HasParaBorder)
            {
                _rtfBuilder.Append(fsThis.ParaBorder.RTFEncoding);
            }

            // TextIndent
            if (dnThis.ListLabel != null)
            {
                _rtfBuilder.Append("\\jclisttab\\tx");
                _rtfBuilder.Append(fsThis.LI.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append("\\fi-360");
            }
            else
            {
                _rtfBuilder.Append("\\fi");
                _rtfBuilder.Append(fsThis.FI.ToString(CultureInfo.InvariantCulture));
            }

            // Alignment
            switch (fsThis.HAlign)
            {
                case HAlign.AlignLeft:
                    if (fsThis.DirPara != DirState.DirRTL)
                    {
                        _rtfBuilder.Append("\\ql");
                    }
                    else
                    {
                        _rtfBuilder.Append("\\qr");
                    }
                    break;
                case HAlign.AlignRight:
                    if (fsThis.DirPara != DirState.DirRTL)
                    {
                        _rtfBuilder.Append("\\qr");
                    }
                    else
                    {
                        _rtfBuilder.Append("\\ql");
                    }
                    break;
                case HAlign.AlignCenter:
                    _rtfBuilder.Append("\\qc");
                    break;
                case HAlign.AlignJustify:
                    _rtfBuilder.Append("\\qj");
                    break;
            }

            // Background color
            if (fsThis.CBPara >= 0)
            {
                _rtfBuilder.Append("\\cbpat");
                _rtfBuilder.Append(fsThis.CBPara.ToString(CultureInfo.InvariantCulture));
            }

            // LineHeight
            if (fsThis.SL != 0)
            {
                _rtfBuilder.Append("\\sl");
                _rtfBuilder.Append(fsThis.SL.ToString(CultureInfo.InvariantCulture));
                _rtfBuilder.Append("\\slmult0");
            }

            // omit \par if last paragraph in cell
            if (dnThis.IsLastParagraphInCell())
            {
                DocumentNode dnCell = dnThis.GetParentOfType(DocumentNodeType.dnCell);
                dnCell.IsTerminated = true;
                if (fsThis.ITAP > 1)
                {
                    _rtfBuilder.Append("\\nestcell");
                    _rtfBuilder.Append("{\\nonesttables\\par}");
                }
                else
                {
                    _rtfBuilder.Append("\\cell");
                }
                _rtfBuilder.Append("\r\n");
            }
            else
            {
                _rtfBuilder.Append("\\par");
            }
            _rtfBuilder.Append("}");
            _rtfBuilder.Append("\r\n");
        }

        private bool WriteParagraphFontInfo(DocumentNode dnThis, FormatState fsThis, FormatState fsParent)
        {
            int nIndex = dnThis.Index;
            int nStart = nIndex + 1;
            int nAt;
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            bool bOutControl = false;

            // In order to minimize RTF output, pull fontsize and font info into paragraph level if possible
            long fsAll = -2;
            long fontAll = -2;
            for (nAt = nStart; nAt <= nIndex + dnThis.ChildCount; nAt++)
            {
                DocumentNode dnChild = dna.EntryAt(nAt);

                if (dnChild.Parent == dnThis)
                {
                    if (fsAll == -2)
                    {
                        fsAll = dnChild.FormatState.FontSize;
                    }
                    else if (fsAll != dnChild.FormatState.FontSize)
                    {
                        fsAll = -3;
                    }
                    if (fontAll == -2)
                    {
                        fontAll = dnChild.FormatState.Font;
                    }
                    else if (fontAll != dnChild.FormatState.Font)
                    {
                        fontAll = -3;
                    }
                }
            }
            if (fsAll >= 0)
            {
                fsThis.FontSize = fsAll;
            }
            if (fontAll >= 0)
            {
                fsThis.Font = fontAll;
            }

            // Workaround for Word 11 \f behavior.  See bug 1636475.
            // Word 11 does not respect \f applied above the paragraph
            // level with \rtlpara.  This is a targeted work-around
            // which is probably not complete, but additional repros
            // are currently lacking.
            bool isTopLevelParagraph = dnThis.Type == DocumentNodeType.dnParagraph &&
                                       dnThis.Parent != null &&
                                       dnThis.Parent.Type == DocumentNodeType.dnSection &&
                                       dnThis.Parent.Parent == null;

            if (fsThis.FontSize != fsParent.FontSize)
            {
                _rtfBuilder.Append("\\fs");
                _rtfBuilder.Append(fsThis.FontSize.ToString(CultureInfo.InvariantCulture));
                bOutControl = true;
            }
            if (fsThis.Font != fsParent.Font || isTopLevelParagraph)
            {
                _rtfBuilder.Append("\\f");
                _rtfBuilder.Append(fsThis.Font.ToString(CultureInfo.InvariantCulture));
                bOutControl = true;
            }
            if (fsThis.Bold != fsParent.Bold)
            {
                _rtfBuilder.Append("\\b");
                bOutControl = true;
            }
            if (fsThis.Italic != fsParent.Italic)
            {
                _rtfBuilder.Append("\\i");
                bOutControl = true;
            }
            if (fsThis.UL != fsParent.UL)
            {
                _rtfBuilder.Append("\\ul");
                bOutControl = true;
            }
            if (fsThis.Strike != fsParent.Strike)
            {
                _rtfBuilder.Append("\\strike");
                bOutControl = true;
            }
            if (fsThis.CF != fsParent.CF)
            {
                _rtfBuilder.Append("\\cf");
                _rtfBuilder.Append(fsThis.CF.ToString(CultureInfo.InvariantCulture));
                bOutControl = true;
            }

            return bOutControl;
        }

        private bool WriteParagraphListInfo(DocumentNode dnThis, FormatState fsThis)
        {
            bool bOutControl = false;

            bool bNewStyle = GenerateListTables;
            if (dnThis.ListLabel != null)
            {
                DocumentNode dnList = dnThis.GetParentOfType(DocumentNodeType.dnList);
                if (dnList != null)
                {
                    // Old style list info for RichEdit and other non-Word client compat if I can.
                    // Only do this for simple, non multi-level lists.
                    if (bNewStyle && dnList.FormatState.PNLVL == 1)
                    {
                        bNewStyle = false;
                    }

                    if (bNewStyle)
                    {
                        _rtfBuilder.Append("{\\listtext ");
                        _rtfBuilder.Append(dnThis.ListLabel);
                        if (dnList.FormatState.Marker != MarkerStyle.MarkerBullet
                            && dnList.FormatState.Marker != MarkerStyle.MarkerNone)
                        {
                            _rtfBuilder.Append(".");
                        }
                        _rtfBuilder.Append("\\tab}");

                        // NB: RichEdit requires \ls keyword to occur immediately after \listtext
                        if (fsThis.ILS > 0)
                        {
                            _rtfBuilder.Append("\\ls");
                            _rtfBuilder.Append(fsThis.ILS.ToString(CultureInfo.InvariantCulture));
                            bOutControl = true;
                        }
                        if (fsThis.ILVL > 0)
                        {
                            _rtfBuilder.Append("\\ilvl");
                            _rtfBuilder.Append(fsThis.ILVL.ToString(CultureInfo.InvariantCulture));
                            bOutControl = true;
                        }
                    }
                    else
                    {
                        _rtfBuilder.Append("{\\pntext ");
                        _rtfBuilder.Append(dnThis.ListLabel);
                        if (dnList.FormatState.Marker != MarkerStyle.MarkerBullet
                            && dnList.FormatState.Marker != MarkerStyle.MarkerNone)
                        {
                            _rtfBuilder.Append(".");
                        }
                        _rtfBuilder.Append("\\tab}{\\*\\pn");
                        _rtfBuilder.Append(Converters.MarkerStyleToOldRTFString(dnList.FormatState.Marker));
                        if (fsThis.ListLevel > 0 && dnList.FormatState.PNLVL > 1)
                        {
                            _rtfBuilder.Append("\\pnlvl");
                            _rtfBuilder.Append(fsThis.ListLevel.ToString(CultureInfo.InvariantCulture));
                        }
                        if (fsThis.FI > 0)
                        {
                            _rtfBuilder.Append("\\pnhang");
                        }
                        if (fsThis.StartIndex >= 0)
                        {
                            _rtfBuilder.Append("\\pnstart");
                            _rtfBuilder.Append(fsThis.StartIndex.ToString(CultureInfo.InvariantCulture));
                        }
                        if (dnList.FormatState.Marker == MarkerStyle.MarkerBullet)
                        {
                            _rtfBuilder.Append("{\\pntxtb\\'B7}}");
                        }
                        else if (dnList.FormatState.Marker == MarkerStyle.MarkerNone)
                        {
                            _rtfBuilder.Append("{\\pntxta }{\\pntxtb }}");
                        }
                        else
                        {
                            _rtfBuilder.Append("{\\pntxta .}}");
                        }

                        // Already terminated with curly, no need for extra space.
                        bOutControl = false;
                    }
                }
            }

            return bOutControl;
        }

        private void WriteRow(DocumentNode dnRow)
        {
            int nDepth = dnRow.GetTableDepth();

            // Row is:
            //  [RowStart][Overall Row Settings][Row Default Borders][Per Cell Properties \cell]
            //  [Cell Contents]
            //  [Repeat] \row
            //
            _rtfBuilder.Append("\r\n");
            _rtfBuilder.Append("{");
            if (nDepth == 1)
            {
                WriteRowStart(dnRow);
                WriteRowSettings(dnRow);
                WriteRowsCellProperties(dnRow);
            }
            else if (nDepth > 1)
            {
                _rtfBuilder.Append("\\intbl\\itap");
                _rtfBuilder.Append(nDepth.ToString(CultureInfo.InvariantCulture));
            }
            WriteRowsCellContents(dnRow);

            // Rewrite row properties for word compatibility
            if (nDepth > 1)
            {
                _rtfBuilder.Append("\\intbl\\itap");
                _rtfBuilder.Append(nDepth.ToString(CultureInfo.InvariantCulture));
            }
            _rtfBuilder.Append("{");
            if (nDepth > 1)
            {
                _rtfBuilder.Append("\\*\\nesttableprops");
            }
            WriteRowStart(dnRow);
            WriteRowSettings(dnRow);
            WriteRowsCellProperties(dnRow);

            if (nDepth > 1)
            {
                _rtfBuilder.Append("\\nestrow");
            }
            else
            {
                _rtfBuilder.Append("\\row");
            }

            _rtfBuilder.Append("}}");
            _rtfBuilder.Append("\r\n");
        }

        private void WriteRowStart(DocumentNode dnRow)
        {
            _rtfBuilder.Append("\\trowd");
        }

        private void WriteRowSettings(DocumentNode dnRow)
        {
            DocumentNode dnTable = dnRow.GetParentOfType(DocumentNodeType.dnTable);
            DirState dirHere = dnTable != null ? dnTable.XamlDir : DirState.DirLTR;
            DirState dirPa = dnTable != null ? dnTable.ParentXamlDir : DirState.DirLTR;

            if (dnTable != null)
            {
                // Note: Parent directionality determines margin interpretation.
                long l = dirPa == DirState.DirLTR ? dnTable.FormatState.LI : dnTable.FormatState.RI;
                string s = l.ToString(CultureInfo.InvariantCulture);
                _rtfBuilder.Append("\\trleft");
                _rtfBuilder.Append(s);
                _rtfBuilder.Append("\\trgaph-");
                _rtfBuilder.Append(s);
            }
            else
            {
                _rtfBuilder.Append("\\trgaph0");
                _rtfBuilder.Append("\\trleft0");
            }
            WriteRowBorders(dnRow);
            WriteRowDimensions(dnRow);
            WriteRowPadding(dnRow);
            _rtfBuilder.Append("\\trql");
            if (dirHere == DirState.DirRTL)
            {
                _rtfBuilder.Append("\\rtlrow");
            }
            else
            {
                _rtfBuilder.Append("\\ltrrow");
            }
        }

        private void WriteRowBorders(DocumentNode dnRow)
        {
            // XAML doesn't have notion of default row borders, so there is no explicit attribute
            // in the XAML content to use here and in fact we will always override these values with
            // explicit cell border properties.
            // However, so they are not nonsensical, pick the first cell's properties to write out and if
            // borders are uniform for the row, this will actually be accurate.
            DocumentNodeArray cellArray = dnRow.GetRowsCells();
            if (cellArray.Count > 0)
            {
                DocumentNode dnCell = cellArray.EntryAt(0);
                if (dnCell.FormatState.HasRowFormat)
                {
                    CellFormat cf = dnCell.FormatState.RowFormat.RowCellFormat;

                    WriteBorder("\\trbrdrt", cf.BorderTop);
                    WriteBorder("\\trbrdrb", cf.BorderBottom);
                    WriteBorder("\\trbrdrr", cf.BorderRight);
                    WriteBorder("\\trbrdrl", cf.BorderLeft);
                    WriteBorder("\\trbrdrv", cf.BorderLeft);
                    WriteBorder("\\trbrdrh", cf.BorderTop);
                }
            }
        }

        private void WriteRowDimensions(DocumentNode dnRow)
        {
            _rtfBuilder.Append("\\trftsWidth1");
            _rtfBuilder.Append("\\trftsWidthB3");
        }

        private void WriteRowPadding(DocumentNode dnRow)
        {
            _rtfBuilder.Append("\\trpaddl10");
            _rtfBuilder.Append("\\trpaddr10");
            _rtfBuilder.Append("\\trpaddb10");
            _rtfBuilder.Append("\\trpaddt10");
            _rtfBuilder.Append("\\trpaddfl3");
            _rtfBuilder.Append("\\trpaddfr3");
            _rtfBuilder.Append("\\trpaddft3");
            _rtfBuilder.Append("\\trpaddfb3");
        }

        private void WriteRowsCellProperties(DocumentNode dnRow)
        {
            DocumentNodeArray cellArray = dnRow.GetRowsCells();

            int nCol = 0;
            long lastCellX = 0;

            for (int i = 0; i < cellArray.Count; i++)
            {
                DocumentNode dnCell = cellArray.EntryAt(i);

                lastCellX = WriteCellProperties(dnCell, nCol, lastCellX);
                nCol += dnCell.ColSpan;
            }
        }

        private void WriteRowsCellContents(DocumentNode dnRow)
        {
            DocumentNodeArray cellArray = dnRow.GetRowsCells();

            _rtfBuilder.Append("{");
            for (int i = 0; i < cellArray.Count; i++)
            {
                DocumentNode dnCell = cellArray.EntryAt(i);

                WriteStructure(dnCell);
            }
            _rtfBuilder.Append("}");
        }

        private long WriteCellProperties(DocumentNode dnCell, int nCol, long lastCellX)
        {
            WriteCellColor(dnCell);
            if (dnCell.FormatState.HasRowFormat)
            {
                if (dnCell.FormatState.RowFormat.RowCellFormat.IsVMergeFirst)
                {
                    _rtfBuilder.Append("\\clvmgf");
                }
                else if (dnCell.FormatState.RowFormat.RowCellFormat.IsVMerge)
                {
                    _rtfBuilder.Append("\\clvmrg");
                }
            }
            WriteCellVAlignment(dnCell);
            WriteCellBorders(dnCell);
            WriteCellPadding(dnCell);

            // Return the last cell position
            return WriteCellDimensions(dnCell, nCol, lastCellX);
        }

        private void WriteCellVAlignment(DocumentNode dnCell)
        {
            _rtfBuilder.Append("\\clvertalt");
        }

        private void WriteCellBorders(DocumentNode dnCell)
        {
            if (dnCell.FormatState.HasRowFormat)
            {
                CellFormat cf = dnCell.FormatState.RowFormat.RowCellFormat;

                WriteBorder("\\clbrdrt", cf.BorderTop);
                WriteBorder("\\clbrdrl", cf.BorderLeft);
                WriteBorder("\\clbrdrb", cf.BorderBottom);
                WriteBorder("\\clbrdrr", cf.BorderRight);
            }
            else
            {
                WriteBorder("\\clbrdrt", BorderFormat.EmptyBorderFormat);
                WriteBorder("\\clbrdrl", BorderFormat.EmptyBorderFormat);
                WriteBorder("\\clbrdrb", BorderFormat.EmptyBorderFormat);
                WriteBorder("\\clbrdrr", BorderFormat.EmptyBorderFormat);
            }
        }

        private void WriteCellPadding(DocumentNode dnCell)
        {
        }

        private void WriteCellColor(DocumentNode dnCell)
        {
            FormatState fs = null;

            // Pickup background from cell or row.
            if (dnCell.FormatState.CBPara >= 0)
            {
                fs = dnCell.FormatState;
            }
            else if (dnCell.Parent != null && dnCell.Parent.FormatState.CBPara >= 0)
            {
                fs = dnCell.Parent.FormatState;
            }

            if (fs != null)
            {
                _rtfBuilder.Append("\\clcbpat");
                _rtfBuilder.Append(fs.CBPara.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Wirte the CellX control and value to layout the cell position on the table and
        /// return the last cell x position.
        /// There is no smart calculation for getting cell width without the specified value,
        /// so we use DefaultCellXAsTwips(1440) magic number which is the default CellX value on Word.
        /// </summary>
        private long WriteCellDimensions(DocumentNode dnCell, int nCol, long lastCellX)
        {
            DocumentNode dnTable = dnCell.GetParentOfType(DocumentNodeType.dnTable);

            if (dnTable.FormatState.HasRowFormat)
            {
                RowFormat rf = dnTable.FormatState.RowFormat;
                CellFormat cf = rf.NthCellFormat(nCol);

                if (dnCell.ColSpan > 1)
                {
                    CellFormat cfSpanned = new CellFormat(cf);

                    for (int i = 1; i < dnCell.ColSpan; i++)
                    {
                        cf = rf.NthCellFormat(nCol + i);
                        cfSpanned.Width.Value += cf.Width.Value;
                        cfSpanned.CellX = cf.CellX;
                    }

                    // Calculate the default value if CellX never set or has zero cell count
                    if (cfSpanned.CellX == -1 || rf.CellCount == 0)
                    {
                        // Calculate the default CellX value with tables width
                        cfSpanned.CellX = lastCellX +
                            dnCell.ColSpan * DefaultCellXAsTwips +
                            GetDefaultAllTablesWidthFromCell(dnCell);
                    }

                    // Write the encoded width information like as CellX control and value
                    _rtfBuilder.Append(cfSpanned.RTFEncodingForWidth);

                    // Save the last CellX value to accumulate it with the next cell
                    lastCellX = cfSpanned.CellX;
                }
                else
                {
                    if (cf.CellX == -1 || rf.CellCount == 0)
                    {
                        // Calculate the default CellX value
                        cf.CellX = lastCellX + DefaultCellXAsTwips + GetDefaultAllTablesWidthFromCell(dnCell);
                    }

                    // Write the encoded width information like as CellX control and value
                    _rtfBuilder.Append(cf.RTFEncodingForWidth);

                    lastCellX = cf.CellX;
                }
            }
            else
            {
                _rtfBuilder.Append("\\clftsWidth1");
                _rtfBuilder.Append("\\cellx");

                // Set the CellX value and write the CellX control and value
                long cellX = lastCellX + dnCell.ColSpan * DefaultCellXAsTwips;
                _rtfBuilder.Append(cellX.ToString(CultureInfo.InvariantCulture));

                lastCellX = cellX;
            }

            return lastCellX;
        }

        /// <summary>
        /// Get the all tables width under the specified cell.
        /// </summary>
        private long GetDefaultAllTablesWidthFromCell(DocumentNode dnCell)
        {
            long tablesWidth = 0;

            // Find the table node which need to calculate the table width to apply the right CellX value
            for (int childIndex = dnCell.Index + 1; childIndex <= dnCell.Index + dnCell.ChildCount; childIndex++)
            {
                DocumentNode dnChildTable = _converterState.DocumentNodeArray.EntryAt(childIndex);
                if (dnChildTable.Type == DocumentNodeType.dnTable)
                {
                    // Calculate the table width to apply the right CellX value
                    tablesWidth += CalculateDefaultTableWidth(dnChildTable);
                }
            }

            return tablesWidth;
        }

        /// <summary>
        /// Calculate the table width which is the maxium width value of row.
        /// </summary>
        private long CalculateDefaultTableWidth(DocumentNode dnTable)
        {
            long lastCellX = 0;
            long tableWidth = 0;

            for (int tableChildIndex = dnTable.Index+1; tableChildIndex <= dnTable.Index+dnTable.ChildCount; tableChildIndex++)
            {
                DocumentNode dnChild = _converterState.DocumentNodeArray.EntryAt(tableChildIndex);

                if (dnChild.Type == DocumentNodeType.dnRow)
                {
                    // Reset the last CellX value for the new row
                    lastCellX = 0;

                    // Get the cell list in the row
                    DocumentNodeArray cellArray = dnChild.GetRowsCells();

                    for (int cellIndex = 0; cellIndex < cellArray.Count; cellIndex++)
                    {
                        DocumentNode dnCell = cellArray.EntryAt(cellIndex);

                        // Calculate the lastCellX position with column span and 1440(default CellX value)
                        lastCellX += dnCell.ColSpan * DefaultCellXAsTwips;
                    }
                }
                else if (dnChild.Type == DocumentNodeType.dnTable)
                {
                    // Skip the nested table node since GetDefaultAllTablesWidthFromCell will
                    // visit this table node for table calculation
                    tableChildIndex += dnChild.ChildCount;
                }

                tableWidth = Math.Max(tableWidth, lastCellX);
            }

            return tableWidth;
        }

        private void WriteBorder(string borderControlWord, BorderFormat bf)
        {
            _rtfBuilder.Append(borderControlWord);
            _rtfBuilder.Append(bf.RTFEncoding);
        }

        private void PatchVerticallyMergedCells(DocumentNode dnThis)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            DocumentNodeArray dnaRows = dnThis.GetTableRows();
            DocumentNodeArray dnaSpanCells = new DocumentNodeArray();
            ArrayList spanCounts = new ArrayList();
            int nCol = 0;
            int nColExtra = 0;

            for (int i = 0; i < dnaRows.Count; i++)
            {
                DocumentNode dnRow = dnaRows.EntryAt(i);

                DocumentNodeArray dnaCells = dnRow.GetRowsCells();
                int nColHere = 0;
                for (int j = 0; j < dnaCells.Count; j++)
                {
                    DocumentNode dnCell = dnaCells.EntryAt(j);

                    // Insert vmerged cell placeholder if necessary
                    nColExtra = nColHere;
                    while (nColExtra < spanCounts.Count && ((int)spanCounts[nColExtra]) > 0)
                    {
                        DocumentNode dnSpanningCell = dnaSpanCells.EntryAt(nColExtra);
                        DocumentNode dnNew = new DocumentNode(DocumentNodeType.dnCell);
                        dna.InsertChildAt(dnRow, dnNew, dnCell.Index, 0);
                        dnNew.FormatState = new FormatState(dnSpanningCell.FormatState);
                        if (dnSpanningCell.FormatState.HasRowFormat)
                        {
                            dnNew.FormatState.RowFormat = new RowFormat(dnSpanningCell.FormatState.RowFormat);
                        }
                        dnNew.FormatState.RowFormat.RowCellFormat.IsVMergeFirst = false;
                        dnNew.FormatState.RowFormat.RowCellFormat.IsVMerge = true;
                        dnNew.ColSpan = dnSpanningCell.ColSpan;
                        nColExtra += dnNew.ColSpan;
                    }

                    // Take care of any cells hanging down from above.
                    while (nColHere < spanCounts.Count && ((int)spanCounts[nColHere]) > 0)
                    {
                        // Update span count for this row
                        spanCounts[nColHere] = ((int)spanCounts[nColHere]) - 1;
                        if ((int)spanCounts[nColHere] == 0)
                        {
                            dnaSpanCells[nColHere] = null;
                        }
                        nColHere++;
                    }

                    // Now update colHere and spanCounts
                    for (int k = 0; k < dnCell.ColSpan; k++)
                    {
                        if (nColHere < spanCounts.Count)
                        {
                            spanCounts[nColHere] = dnCell.RowSpan - 1;
                            dnaSpanCells[nColHere] = (dnCell.RowSpan > 1) ? dnCell : null;
                        }
                        else
                        {
                            spanCounts.Add(dnCell.RowSpan - 1);
                            dnaSpanCells.Add((dnCell.RowSpan > 1) ? dnCell : null);
                        }
                        nColHere++;
                    }

                    // Mark this cell as first in vmerged list if necessary
                    if (dnCell.RowSpan > 1)
                    {
                        CellFormat cf = dnCell.FormatState.RowFormat.RowCellFormat;

                        cf.IsVMergeFirst = true;
                    }
                }

                // Insert vmerged cell placeholder if necessary
                nColExtra = nColHere;
                while (nColExtra < spanCounts.Count)
                {
                    if (((int)spanCounts[nColExtra]) > 0)
                    {
                        // Insert vmerged cell here.
                        DocumentNode dnSpanningCell = dnaSpanCells.EntryAt(nColExtra);
                        DocumentNode dnNew = new DocumentNode(DocumentNodeType.dnCell);
                        dna.InsertChildAt(dnRow, dnNew, dnRow.Index + dnRow.ChildCount + 1, 0);
                        dnNew.FormatState = new FormatState(dnSpanningCell.FormatState);
                        if (dnSpanningCell.FormatState.HasRowFormat)
                        {
                            dnNew.FormatState.RowFormat = new RowFormat(dnSpanningCell.FormatState.RowFormat);
                        }
                        dnNew.FormatState.RowFormat.RowCellFormat.IsVMergeFirst = false;
                        dnNew.FormatState.RowFormat.RowCellFormat.IsVMerge = true;
                        dnNew.ColSpan = dnSpanningCell.ColSpan;
                        nColExtra += dnNew.ColSpan;
                    }
                    else
                    {
                        nColExtra++;
                    }
                }

                // Take care of remaining cells hanging down.
                while (nColHere < spanCounts.Count)
                {
                    if (((int)spanCounts[nColHere]) > 0)
                    {
                        spanCounts[nColHere] = ((int)spanCounts[nColHere]) - 1;
                        if ((int)spanCounts[nColHere] == 0)
                        {
                            dnaSpanCells[nColHere] = null;
                        }
                    }
                    nColHere++;
                }

                // Track max
                if (nColHere > nCol)
                {
                    nCol = nColHere;
                }
            }
        }

        private void WriteStructure(DocumentNode dnThis)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            bool nested = dnThis.GetParentOfType(DocumentNodeType.dnCell) != null;

            // Prolog
            switch (dnThis.Type)
            {
                case DocumentNodeType.dnSection:
                    {
                        WriteSection(dnThis);
                        return;
                    }

                case DocumentNodeType.dnParagraph:
                    {
                        WriteParagraph(dnThis);
                        return;
                    }

                case DocumentNodeType.dnInline:
                    {
                        WriteInlineChild(dnThis);
                        return;
                    }

                case DocumentNodeType.dnInlineUIContainer:
                case DocumentNodeType.dnBlockUIContainer:
                    {
                        WriteUIContainerChild(dnThis);
                        return;
                    }

                case DocumentNodeType.dnList:
                case DocumentNodeType.dnListItem:
                    // Handled as paragraph properties
                    break;

                case DocumentNodeType.dnTable:
                    // Make sure column format is canonicalized
                    if (dnThis.FormatState.HasRowFormat)
                    {
                        dnThis.FormatState.RowFormat.Trleft = dnThis.FormatState.LI;
                        dnThis.FormatState.RowFormat.CanonicalizeWidthsFromXaml();
                    }
                    PatchVerticallyMergedCells(dnThis);
                    break;

                case DocumentNodeType.dnTableBody:
                    // For RTF, we only write row properties.
                    break;

                case DocumentNodeType.dnRow:
                    WriteRow(dnThis);
                    break;

                case DocumentNodeType.dnCell:
                    break;

                default:
                    // Not really structure
                    return;
            }

            // Write direct children, except for row
            if (dnThis.Type != DocumentNodeType.dnRow)
            {
                int nIndex = dnThis.Index;
                int nStart = nIndex + 1;

                for (; nStart <= nIndex + dnThis.ChildCount; nStart++)
                {
                    DocumentNode dnChild = dna.EntryAt(nStart);

                    if (dnChild.Parent == dnThis)
                    {
                        WriteStructure(dnChild);
                    }
                }
            }

            // Epilog
            switch (dnThis.Type)
            {
                case DocumentNodeType.dnList:
                case DocumentNodeType.dnListItem:
                    // Handled as paragraph properties
                    break;

                case DocumentNodeType.dnTable:
                    break;

                case DocumentNodeType.dnTableBody:
                    break;

                case DocumentNodeType.dnRow:
                    // Handled above
                    break;

                case DocumentNodeType.dnCell:
                    if (!dnThis.IsTerminated)
                    {
                        _rtfBuilder.Append(nested ? "\\nestcell" : "\\cell");
                        _rtfBuilder.Append("\r\n");
                    }
                    break;
            }
        }

        private void WriteDocumentContents()
        {
            // Set things up
            _rtfBuilder.Append("\\loch\\hich\\dbch\\pard\\plain\\ltrpar\\itap0");

            // Walk through paragraphs
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            int i = 0;

            while (i < dna.Count)
            {
                DocumentNode dn = dna.EntryAt(i);
                WriteStructure(dn);
                i += dn.ChildCount + 1;
            }
        }

        private void WriteEpilog()
        {
            _rtfBuilder.Append("}");
        }

        private void WriteOutput()
        {
            BuildListTable();
            SetParagraphStructureProperties();
            MergeParagraphMargins();
            GenerateListLabels();

            WriteProlog();
            WriteHeaderTables();
            WriteDocumentContents();
            WriteEpilog();
        }

        // Write image control and image hex data to the rtf content
        private void WriteImage(DocumentNode documentNode)
        {
            if (_wpfPayload == null)
            {
                // Package is not available. Skip the image.
                return;
            }

            // Read the image binary data from WpfPayLoad that contains Xaml and Images.
            // Xaml content have the image source like as "./Image1.png" so that we can
            // query the image from the container of WpfPayLoad with the image source name.
            Stream imageStream = _wpfPayload.GetImageStream(documentNode.FormatState.ImageSource);

            // Get image type to be added to rtf content
            RtfImageFormat imageFormat = GetImageFormatFromImageSourceName(documentNode.FormatState.ImageSource);

            // Write the shape image like as "\pngblip" or "\jpegblip" rtf control. We wrap the stream that comes
            // from the package because we require the stream to be seekable.
            Debug.Assert(!imageStream.CanSeek);
            using (var seekableStream = new MemoryStream((int)imageStream.Length))
            {
                imageStream.CopyTo(seekableStream);
                WriteShapeImage(documentNode, seekableStream, imageFormat);
            }

#if WindowsMetaFile
            // This block is disabled because of performance.
            //  WindowsMetafile format is needed only for WordPad or legacy applications,
            //  so considering its high cost and very low quality, we can (temporarily or permanently)
            //  cut this feature.

            // Write the none shape image with control "\nonshppict" so that
            // we can support copy/paste image on Wordpad or legacy apps that only
            // handle windows metafile as the cotnrol "\wmetafileN"
            WriteNoneShapeImage(documentNode, imageStream, imageFormat);
#endif // WindowsMetaFile
        }

        // Write the shape image with control "\shppict"
        private void WriteShapeImage(DocumentNode documentNode, Stream imageStream, RtfImageFormat imageFormat)
        {
            // Add the image(picture) control
            _rtfBuilder.Append("{\\*\\shppict{\\pict");

            // Get the current image input size
            Size imageInputSize = new Size(documentNode.FormatState.ImageWidth, documentNode.FormatState.ImageHeight);

            // Get the natural size that is on the bitmap source
            Size imageNaturalSize;
            System.Windows.Media.Imaging.BitmapSource bitmapSource = (System.Windows.Media.Imaging.BitmapSource)System.Windows.Media.Imaging.BitmapFrame.Create(imageStream);
            if (bitmapSource != null)
            {
                imageNaturalSize = new Size(bitmapSource.Width, bitmapSource.Height);
            }
            else
            {
                imageNaturalSize = new Size(imageInputSize.Width, imageInputSize.Height);
            }

            // Get the stretch and stretch direction to apply the image scale factor
            System.Windows.Media.Stretch imageStretch = GetImageStretch(documentNode.FormatState.ImageStretch);
            System.Windows.Controls.StretchDirection imageStretchDirection = GetImageStretchDirection(documentNode.FormatState.ImageStretchDirection);

            // Do a simple fixup to handle "0" input size,
            // which in practice means unspecified.
            if (imageInputSize.Width == 0)
            {
                if (imageInputSize.Height == 0)
                {
                    imageInputSize.Width = imageNaturalSize.Width;
                }
                else
                {
                    // this ignores stretch.
                    imageInputSize.Width = imageNaturalSize.Width * (imageInputSize.Height / imageNaturalSize.Height);
                }
            }
            if (imageInputSize.Height == 0)
            {
                if (imageInputSize.Width == 0)
                {
                    imageInputSize.Height = imageNaturalSize.Height;
                }
                else
                {
                    //  this ignores stretch.
                    imageInputSize.Height = imageNaturalSize.Height * (imageInputSize.Width / imageNaturalSize.Width);
                }
            }

            // Get computed image scale factor
            Size scaleFactor = System.Windows.Controls.Viewbox.ComputeScaleFactor(
                                   imageInputSize,
                                   imageNaturalSize,
                                   imageStretch,
                                   imageStretchDirection);

            // Add the image baselineoffset data
            if (documentNode.FormatState.IncludeImageBaselineOffset)
            {
                _rtfBuilder.Append("\\dn");

                // RTF format requries the offset property (\dn) in half-points
                _rtfBuilder.Append(
                    Converters.PxToHalfPointRounded((imageNaturalSize.Height * scaleFactor.Height) -
                                                    documentNode.FormatState.ImageBaselineOffset));
            }

            // Add the image(picture) width control
            _rtfBuilder.Append("\\picwgoal");
            _rtfBuilder.Append(Converters.PxToTwipRounded(imageNaturalSize.Width * scaleFactor.Width).ToString(CultureInfo.InvariantCulture));

            // Add the image(picture) height control
            _rtfBuilder.Append("\\pichgoal");
            _rtfBuilder.Append(Converters.PxToTwipRounded(imageNaturalSize.Height * scaleFactor.Height).ToString(CultureInfo.InvariantCulture));

            // Add the image(picture)type control according to image type(name)
            switch (imageFormat)
            {
                case RtfImageFormat.Gif:
                case RtfImageFormat.Tif:
                case RtfImageFormat.Bmp:
                case RtfImageFormat.Dib:
                case RtfImageFormat.Png:
                    _rtfBuilder.Append("\\pngblip");
                    break;

                case RtfImageFormat.Jpeg:
                    _rtfBuilder.Append("\\jpegblip");
                    break;
            }

            // Add new line to put the image hexa data
            _rtfBuilder.Append("\r\n");

            if (imageFormat != RtfImageFormat.Unknown)
            {
                // Convert the image binary data to hex data string that is the default image
                // data type on Rtf content
                string imageHexDataString = ConvertToImageHexDataString(imageStream);

                // Add the image(picture) hex data
                _rtfBuilder.Append(imageHexDataString);
            }

            // Add the curly bracket for closing image(picture) control
            _rtfBuilder.Append("}}");
        }

#if WindowsMetaFile
        //  This block is disabled because of performance.
        //  WindowsMetafile format is needed only for WordPad or legacy applications,
        //  so considering its high cost and very low quality, we can (temporarily or permanently)
        //  cut this feature.

        // Write the none shape image with control "\nonshppict" so that
        // we can support copy/paste image on Wordpad or legacy apps that only
        // handle windows metafile as the cotnrol "\wmetafileN"
        private void WriteNoneShapeImage(DocumentNode documentNode, Stream imageStream, RtfImageFormat imageFormat)
        {
            // Add the image(picture) control
            _rtfBuilder.Append("{\\nonshppict{\\pict");

            // Add the image(picture) width control
            _rtfBuilder.Append("\\picwgoal");
            _rtfBuilder.Append(Converters.PxToTwipRounded(documentNode.FormatState.ImageWidth).ToString(CultureInfo.InvariantCulture));

            // Add the image(picture) height control
            _rtfBuilder.Append("\\pichgoal");
            _rtfBuilder.Append(Converters.PxToTwipRounded(documentNode.FormatState.ImageHeight).ToString(CultureInfo.InvariantCulture));

            _rtfBuilder.Append("\\wmetafile8");

            // Add new line to put the image hexa data
            _rtfBuilder.Append("\n");

            if (imageFormat != RtfImageFormat.Unknown)
            {
                string metafileHexDataString = SystemDrawingHelper.ConvertToMetafileHexDataString(imageStream);

                _rtfBuilder.Append(metafileHexDataString);
            }

            // Add the curly bracket for closing image(picture) control
            _rtfBuilder.Append("}}");
        }

        // Convert to the image hex data string from image binary data
        private string ConvertToImageHexDataString(byte[] imageBytes)
        {
            byte[] imageHexBytes = new byte[imageBytes.Length * 2];

            for (int i = 0; i < imageBytes.Length; i++)
            {
                // Convert byte to the hex data(0x3a ==> 0x33 and 0x61)
                Converters.ByteToHex(imageBytes[i], out imageHexBytes[i * 2], out imageHexBytes[i * 2 + 1]);
            }

            // Return the image hex data string that is the default image data type on Rtf
            return Encoding.GetEncoding(XamlRtfConverter.RtfCodePage).GetString(imageHexBytes);
        }
#endif // WindowsMetaFile

        private string ConvertToImageHexDataString(Stream imageStream)
        {
            byte imageByte;
            byte[] imageHexBytes = new byte[imageStream.Length * 2];

            // Set the position to the begin of image stream
            imageStream.Position = 0;

            for (int i = 0; i < imageStream.Length; i++)
            {
                imageByte = (byte)imageStream.ReadByte();

                // Convert byte to the hex data(0x3a ==> 0x33 and 0x61)
                Converters.ByteToHex(imageByte, out imageHexBytes[i * 2], out imageHexBytes[i * 2 + 1]);
            }

            // Return the image hex data string that is the default image data type on Rtf
            return Encoding.GetEncoding(XamlRtfConverter.RtfCodePage).GetString(imageHexBytes);
        }

        // Get the image type from image source name
        private RtfImageFormat GetImageFormatFromImageSourceName(string imageName)
        {
            RtfImageFormat imageFormat = RtfImageFormat.Unknown;

            int extensionIndex = imageName.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);

            if (extensionIndex >= 0)
            {
                string imageFormatName = imageName.Substring(extensionIndex);

                if (string.Compare(".png", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    imageFormat = RtfImageFormat.Png;
                }
                if (string.Compare(".jpeg", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(".jpg", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    imageFormat = RtfImageFormat.Jpeg;
                }
                if (string.Compare(".gif", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    imageFormat = RtfImageFormat.Gif;
                }
                if (string.Compare(".tif", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(".tiff", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    imageFormat = RtfImageFormat.Tif;
                }
                if (string.Compare(".bmp", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    imageFormat = RtfImageFormat.Bmp;
                }
                if (string.Compare(".dib", imageFormatName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    imageFormat = RtfImageFormat.Dib;
                }
            }

            return imageFormat;
        }

        // Get the image stretch type
        private System.Windows.Media.Stretch GetImageStretch(string imageStretch)
        {
            if (string.Compare("Fill", imageStretch, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return System.Windows.Media.Stretch.Fill;
            }
            else if (string.Compare("UniformToFill", imageStretch, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return System.Windows.Media.Stretch.UniformToFill;
            }
            else
            {
                return System.Windows.Media.Stretch.Uniform;
            }
        }

        // Get the image stretch direction type
        private System.Windows.Controls.StretchDirection GetImageStretchDirection(string imageStretchDirection)
        {
            if (string.Compare("UpOnly", imageStretchDirection, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return System.Windows.Controls.StretchDirection.UpOnly;
            }
            else if (string.Compare("DownOnly", imageStretchDirection, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return System.Windows.Controls.StretchDirection.DownOnly;
            }
            else
            {
                return System.Windows.Controls.StretchDirection.Both;
            }
        }

        #endregion Private Methods

        // ---------------------------------------------------------------------
        //
        // Private Fields
        //
        // ---------------------------------------------------------------------

        #region Private Fields

        private string _xaml;
        private StringBuilder _rtfBuilder;

        private ConverterState _converterState;

        private XamlIn _xamlIn;

        // WpfPayload package that containing the image for the specified Xaml
        private WpfPayload _wpfPayload;

        private const int DefaultCellXAsTwips = 1440;

        #endregion Private Fields

        // ---------------------------------------------------------------------
        //
        // Internal Enum
        //
        // ---------------------------------------------------------------------

        #region Internal Enum

        /// <summary>
        /// XamlTag
        /// </summary>
        internal enum XamlTag
        {
            XTUnknown,
            XTBold,
            XTItalic,
            XTUnderline,
            XTHyperlink,
            XTInline,
            XTLineBreak,
            XTParagraph,
            XTInlineUIContainer,
            XTBlockUIContainer,
            XTImage,
            XTBitmapImage,
            XTList,
            XTListItem,
            XTTable,
            XTTableBody,
            XTTableRow,
            XTTableCell,
            XTTableColumn,
            XTSection,
            XTFloater,
            XTFigure,
            XTTextDecoration            // Complex Attributes
        };

        #endregion Internal Enum

        // ---------------------------------------------------------------------
        //
        // Private Class
        //
        // ---------------------------------------------------------------------

        #region Private Class

        internal class XamlIn : IXamlContentHandler, IXamlErrorHandler
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            /// <summary>
            ///
            /// </summary>
            internal XamlIn(XamlToRtfWriter writer, string xaml)
            {
                _writer = writer;
                _xaml = xaml;

                _parser = new XamlToRtfParser(_xaml);

                _parser.SetCallbacks(this, this);

                _bGenListTables = true;
            }

            #endregion Constructors

            // ---------------------------------------------------------------------
            //
            // internal Methods
            //
            // ---------------------------------------------------------------------

            #region internal Properties

            internal bool GenerateListTables
            {
                get
                {
                    return _bGenListTables;
                }
            }

            #endregion internal Properties

            #region internal Methods

            /// <summary>
            /// Pasrse the xaml.
            /// </summary>
            /// <returns></returns>
            internal XamlToRtfError Parse()
            {
                return _parser.Parse();
            }

            XamlToRtfError IXamlContentHandler.Characters(string characters)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                ConverterState converterState = _writer.ConverterState;
                DocumentNodeArray dna = converterState.DocumentNodeArray;
                DocumentNode dnTop = dna.TopPending();
                DocumentNode dn;

                int index = 0;

                while (xamlToRtfError == XamlToRtfError.None && index < characters.Length)
                {
                    // Move past opening CRLF
                    while (index < characters.Length && IsNewLine(characters[index]))
                    {
                        index++;
                    }

                    int end = index;

                    while (end < characters.Length && !IsNewLine(characters[end]))
                    {
                        end++;
                    }

                    if (index != end)
                    {
                        string newCharacters = characters.Substring(index, end - index);

                        dn = new DocumentNode(DocumentNodeType.dnText);
                        if (dnTop != null)
                        {
                            dn.InheritFormatState(dnTop.FormatState);
                        }

                        dna.Push(dn);
                        dn.IsPending = false;

                        if (xamlToRtfError == XamlToRtfError.None)
                        {
                            FontTableEntry e = converterState.FontTable.FindEntryByIndex((int)dn.FormatState.Font);
                            int cp = (e == null) ? 1252 : e.CodePage;
                            XamlParserHelper.AppendRTFText(dn.Content, newCharacters, cp);
                        }
                    }

                    index = end;
                }

                return xamlToRtfError;
            }

            XamlToRtfError IXamlContentHandler.StartDocument()
            {
                return XamlToRtfError.None;
            }

            XamlToRtfError IXamlContentHandler.EndDocument()
            {
                return XamlToRtfError.None;
            }

            /// <summary>
            /// Implemnetation of IXamlContentHandler.StartElement
            /// </summary>
            /// <param name="nameSpaceUri"></param>
            /// <param name="localName"></param>
            /// <param name="qName"></param>
            /// <param name="attributes"></param>
            /// <returns></returns>
            XamlToRtfError IXamlContentHandler.StartElement(string nameSpaceUri, string localName, string qName, IXamlAttributes attributes)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                ConverterState converterState = _writer.ConverterState;
                DocumentNodeArray dna = converterState.DocumentNodeArray;

                DocumentNodeType documentNodeType = DocumentNodeType.dnUnknown;
                DocumentNode dnTop = dna.TopPending();
                DocumentNode documentNode = null;
                XamlTag xamlTag = XamlTag.XTUnknown;
                bool bNewNode = true;

                if (!XamlParserHelper.ConvertToTag(converterState, localName, ref xamlTag))
                {
                    return xamlToRtfError;
                }

                // Complex Attributes?
                if (xamlTag == XamlTag.XTTextDecoration
                    || xamlTag == XamlTag.XTTableColumn
                    || xamlTag == XamlTag.XTBitmapImage)
                {
                    if (dnTop == null)
                    {
                        return xamlToRtfError;
                    }
                    documentNode = dnTop;
                    bNewNode = false;
                }

                if (bNewNode)
                {
                    if (!XamlParserHelper.ConvertTagToNodeType(xamlTag, ref documentNodeType))
                    {
                        return xamlToRtfError;
                    }

                    documentNode = CreateDocumentNode(converterState, documentNodeType, dnTop, xamlTag);
                }

                // Handle attributes
                if (attributes != null && documentNode != null)
                {
                    xamlToRtfError = HandleAttributes(converterState, attributes, documentNode, xamlTag, dna);
                }

                // Now push on element stack
                if (xamlToRtfError == XamlToRtfError.None && documentNode != null && bNewNode)
                {
                    // For inline elements, first ensure that there is a paragraph node.
                    if (!documentNode.IsInline)
                    {
                        XamlParserHelper.EnsureParagraphClosed(converterState);
                    }

                    dna.Push(documentNode);
                }

                return xamlToRtfError;
            }

            /// <summary>
            /// Implementation of IXamlContentHandler.EndElement
            /// </summary>
            /// <param name="nameSpaceUri"></param>
            /// <param name="localName"></param>
            /// <param name="qName"></param>
            /// <returns></returns>
            XamlToRtfError IXamlContentHandler.EndElement(string nameSpaceUri, string localName, string qName)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;
                ConverterState converterState = _writer.ConverterState;

                // Ignore unknown tags
                XamlTag xamlTag = XamlTag.XTUnknown;

                if (!XamlParserHelper.ConvertToTag(converterState, localName, ref xamlTag))
                {
                    return xamlToRtfError;
                }

                DocumentNodeType documentNodeType = DocumentNodeType.dnUnknown;

                if (!XamlParserHelper.ConvertTagToNodeType(xamlTag, ref documentNodeType))
                {
                    return xamlToRtfError;
                }

                // Try to close this tag.
                DocumentNodeArray dna = converterState.DocumentNodeArray;

                int nCloseAt = dna.FindPending(documentNodeType);
                if (nCloseAt >= 0)
                {
                    DocumentNode documentNode = dna.EntryAt(nCloseAt);

                    // Might also have implicit paragraph to close
                    if (documentNodeType != DocumentNodeType.dnParagraph && !documentNode.IsInline)
                    {
                        XamlParserHelper.EnsureParagraphClosed(converterState);
                    }

                    dna.CloseAt(nCloseAt);
                }

                return xamlToRtfError;
            }

            /// <summary>
            /// Implementation of IXamlContentHandler.IgnorableWhitespace
            /// </summary>
            /// <param name="xaml"></param>
            /// <returns></returns>
            XamlToRtfError IXamlContentHandler.IgnorableWhitespace(string xaml)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;
                ConverterState converterState = _writer.ConverterState;

                // If we have a paragraph open, insert this WS as characters
                if (converterState.DocumentNodeArray.FindPending(DocumentNodeType.dnParagraph) >= 0 ||
                    converterState.DocumentNodeArray.FindPending(DocumentNodeType.dnInline) >= 0)
                {
                    for (int i = 0; i < xaml.Length; )
                    {
                        int iStart = i;
                        int iEnd = i;
                        int iRestart = -1;
                        for (; iEnd < xaml.Length; iEnd++)
                        {
                            if (xaml[iEnd] == '\r' || xaml[iEnd] == '\n')
                            {
                                if (xaml[iEnd] == '\r' && iEnd + 1 < xaml.Length && xaml[iEnd + 1] == '\n')
                                {
                                    iRestart = iEnd + 2;
                                }
                                else
                                {
                                    iRestart = iEnd + 1;
                                }
                            }
                        }

                        // Common case - no newlines
                        if (iStart == 0 && iEnd == xaml.Length)
                        {
                            return ((IXamlContentHandler)this).Characters(xaml);
                        }

                        // OK, need to handle newlines
                        // First handle any leading space
                        if (iEnd != iStart)
                        {
                            string prefix = xaml.Substring(iStart, iEnd - iStart);
                            xamlToRtfError = ((IXamlContentHandler)this).Characters(prefix);
                            if (xamlToRtfError != XamlToRtfError.None)
                            {
                                return xamlToRtfError;
                            }
                        }
                        // Now insert new line
                        xamlToRtfError = ((IXamlContentHandler)this).StartElement(null, "LineBreak", null, null);
                        if (xamlToRtfError != XamlToRtfError.None)
                        {
                            return xamlToRtfError;
                        }
                        xamlToRtfError = ((IXamlContentHandler)this).EndElement(null, "LineBreak", null);
                        if (xamlToRtfError != XamlToRtfError.None)
                        {
                            return xamlToRtfError;
                        }

                        // Continue looping after this
                        i = (iEnd == xaml.Length) ? iEnd : iRestart;
                    }
                    return ((IXamlContentHandler)this).Characters(xaml);
                }

                return xamlToRtfError;
            }

            /// <summary>
            /// Implemenation of IXamlContentHandler.StartPrefixMapping
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="uri"></param>
            /// <returns></returns>
            XamlToRtfError IXamlContentHandler.StartPrefixMapping(string prefix, string uri)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                return xamlToRtfError;
            }

            /*
            /// <summary>
            /// Implemenation of EndPrefixMapping
            /// </summary>
            /// <param name="prefix"></param>
            /// <returns></returns>
            XamlToRtfError EndPrefixMapping(string prefix)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                return xamlToRtfError;
            }
            */

            XamlToRtfError IXamlContentHandler.ProcessingInstruction(string target, string data)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                return xamlToRtfError;
            }

            XamlToRtfError IXamlContentHandler.SkippedEntity(string name)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                if (string.Compare(name, "&gt;", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return ((IXamlContentHandler)this).Characters(">");
                }
                else if (string.Compare(name, "&lt;", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return ((IXamlContentHandler)this).Characters("<");
                }
                else if (string.Compare(name, "&amp;", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return ((IXamlContentHandler)this).Characters("&");
                }
                else if (name.IndexOf("&#x", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    xamlToRtfError = XamlToRtfError.InvalidFormat;
                    if (name.Length >= 5)
                    {
                        string num = name.Substring(3, name.Length - 4);
                        int i = 0;
                        bool ret = Converters.HexStringToInt(num, ref i);
                        if (i >= 0 && i <= 0xFFFF)
                        {
                            char[] ac = new char[1];
                            ac[0] = (char)i;
                            string s = new string(ac);
                            return ((IXamlContentHandler)this).Characters(s);
                        }
                    }
                }
                else if (name.IndexOf("&#", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (name.Length >= 4)
                    {
                        string num = name.Substring(2, name.Length - 3);
                        int i = 0;
                        bool ret = Converters.StringToInt(num, ref i);
                        if (i >= 0 && i <= 0xFFFF)
                        {
                            char[] ac = new char[1];
                            ac[0] = (char)i;
                            string s = new string(ac);
                            return ((IXamlContentHandler)this).Characters(s);
                        }
                    }
                }

                return xamlToRtfError;
            }


            void IXamlErrorHandler.Error(string message, XamlToRtfError xamlToRtfError)
            {
            }

            void IXamlErrorHandler.FatalError(string message, XamlToRtfError xamlToRtfError)
            {
            }

            void IXamlErrorHandler.IgnorableWarning(string message, XamlToRtfError xamlToRtfError)
            {
            }

            #endregion internal Methods

            // ---------------------------------------------------------------------
            //
            // Private Methods
            //
            // ---------------------------------------------------------------------

            #region Private Methods

            private bool IsNewLine(char character)
            {
                return (character == '\r' || character == '\n');
            }

            // Helper for IXamlContentHandler.StartElement.
            private DocumentNode CreateDocumentNode(ConverterState converterState, DocumentNodeType documentNodeType, DocumentNode dnTop, XamlTag xamlTag)
            {
                DocumentNode documentNode = new DocumentNode(documentNodeType);
                if (dnTop != null)
                {
                    documentNode.InheritFormatState(dnTop.FormatState);
                }

                // Handle implicit formatting properties.
                switch (xamlTag)
                {
                    case XamlTag.XTBold:
                        documentNode.FormatState.Bold = true;
                        break;

                    case XamlTag.XTHyperlink:
                        {
                            long lColor = 0;
                            documentNode.FormatState.UL = ULState.ULNormal;
                            if (XamlParserHelper.ConvertToColor(converterState, "#FF0000FF", ref lColor))
                            {
                                documentNode.FormatState.CF = lColor;
                            }
                        }
                        break;

                    case XamlTag.XTItalic:
                        documentNode.FormatState.Italic = true;
                        break;

                    case XamlTag.XTUnderline:
                        documentNode.FormatState.UL = ULState.ULNormal;
                        break;

                    case XamlTag.XTList:
                        documentNode.FormatState.Marker = MarkerStyle.MarkerBullet;
                        documentNode.FormatState.StartIndex = 1;

                        // Set the default left margin for a list.
                        documentNode.FormatState.LI = 720;
                        break;
                }

                return documentNode;
            }

            // Helper for IXamlContentHandler.StartElement.
            private XamlToRtfError HandleAttributes(ConverterState converterState, IXamlAttributes attributes,
                DocumentNode documentNode, XamlTag xamlTag, DocumentNodeArray dna)
            {
                int nLen = 0;

                XamlToRtfError xamlToRtfError = attributes.GetLength(ref nLen);

                if (xamlToRtfError == XamlToRtfError.None)
                {
                    string uri = string.Empty;
                    string newLocalName = string.Empty;
                    string newQName = string.Empty;
                    string valueString = string.Empty;

                    FormatState formatState = documentNode.FormatState;
                    XamlAttribute attribute = XamlAttribute.XAUnknown;
                    long valueData = 0;

                    for (int i = 0; xamlToRtfError == XamlToRtfError.None && i < nLen; i++)
                    {
                        xamlToRtfError = attributes.GetName(i, ref uri, ref newLocalName, ref newQName);

                        if (xamlToRtfError == XamlToRtfError.None)
                        {
                            xamlToRtfError = attributes.GetValue(i, ref valueString);

                            if (xamlToRtfError == XamlToRtfError.None &&
                                XamlParserHelper.ConvertToAttribute(converterState, newLocalName, ref attribute))
                            {
                                switch (attribute)
                                {
                                    case XamlAttribute.XAUnknown:
                                        break;

                                    case XamlAttribute.XAFontWeight:
                                        if (string.Compare(valueString, "Normal", StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            formatState.Bold = false;
                                        }
                                        else if (string.Compare(valueString, "Bold", StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            formatState.Bold = true;
                                        }
                                        break;

                                    case XamlAttribute.XAFontSize:
                                        double fs = 0f;

                                        if (XamlParserHelper.ConvertToFontSize(converterState, valueString, ref fs))
                                        {
                                            formatState.FontSize = (long)Math.Round(fs);
                                        }
                                        break;

                                    case XamlAttribute.XAFontStyle:
                                        if (string.Compare(valueString, "Italic", StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            formatState.Italic = true;
                                        }
                                        break;

                                    case XamlAttribute.XAFontFamily:
                                        if (XamlParserHelper.ConvertToFont(converterState, valueString, ref valueData))
                                        {
                                            formatState.Font = valueData;
                                        }
                                        break;

                                    case XamlAttribute.XAFontStretch:
                                        if (XamlParserHelper.ConvertToFontStretch(converterState, valueString, ref valueData))
                                        {
                                            formatState.Expand = valueData;
                                        }
                                        break;

                                    case XamlAttribute.XABackground:
                                        if (XamlParserHelper.ConvertToColor(converterState, valueString, ref valueData))
                                        {
                                            if (documentNode.IsInline)
                                            {
                                                formatState.CB = valueData;
                                            }
                                            else
                                            {
                                                formatState.CBPara = valueData;
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XAForeground:
                                        if (XamlParserHelper.ConvertToColor(converterState, valueString, ref valueData))
                                        {
                                            formatState.CF = valueData;
                                        }
                                        break;

                                    case XamlAttribute.XAFlowDirection:
                                        DirState dirState = DirState.DirDefault;

                                        if (XamlParserHelper.ConvertToDir(converterState, valueString, ref dirState))
                                        {
                                            if (documentNode.IsInline)
                                            {
                                                formatState.DirChar = dirState;
                                            }
                                            else if (documentNode.Type == DocumentNodeType.dnTable)
                                            {
                                                formatState.RowFormat.Dir = dirState;
                                            }
                                            else
                                            {
                                                formatState.DirPara = dirState;

                                                // Set the default inline flow direction as the paragraph's flow direction
                                                formatState.DirChar = dirState;
                                            }

                                            if (documentNode.Type == DocumentNodeType.dnList)
                                            {
                                                // Reset the left/right margin for List as the default value(720)
                                                // on RTL flow direction. Actually LI is set as 720 as the default
                                                // CreateDocumentNode().
                                                if (formatState.DirPara == DirState.DirRTL)
                                                {
                                                    formatState.LI = 0;
                                                    formatState.RI = 720;
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XATextDecorations:
                                        {
                                            ULState ulState = ULState.ULNormal;
                                            StrikeState strikeState = StrikeState.StrikeNormal;

                                            if (XamlParserHelper.ConvertToDecoration(converterState, valueString,
                                                                                     ref ulState,
                                                                                     ref strikeState))
                                            {
                                                if (ulState != ULState.ULNone)
                                                {
                                                    formatState.UL = ulState;
                                                }
                                                if (strikeState != StrikeState.StrikeNone)
                                                {
                                                    formatState.Strike = strikeState;
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XALocation:
                                        {
                                            ULState ulState = ULState.ULNormal;
                                            StrikeState strikeState = StrikeState.StrikeNormal;

                                            if (XamlParserHelper.ConvertToDecoration(converterState, valueString,
                                                                                     ref ulState,
                                                                                     ref strikeState))
                                            {
                                                if (ulState != ULState.ULNone)
                                                {
                                                    formatState.UL = ulState;
                                                }
                                                if (strikeState != StrikeState.StrikeNone)
                                                {
                                                    formatState.Strike = strikeState;
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XARowSpan:
                                        {
                                            int nRowSpan = 0;

                                            if (Converters.StringToInt(valueString, ref nRowSpan))
                                            {
                                                if (documentNode.Type == DocumentNodeType.dnCell)
                                                {
                                                    documentNode.RowSpan = nRowSpan;
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XAColumnSpan:
                                        {
                                            int nColSpan = 0;

                                            if (Converters.StringToInt(valueString, ref nColSpan))
                                            {
                                                if (documentNode.Type == DocumentNodeType.dnCell)
                                                {
                                                    documentNode.ColSpan = nColSpan;
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XACellSpacing:
                                        {
                                            double d = 0f;

                                            if (Converters.StringToDouble(valueString, ref d))
                                            {
                                                if (documentNode.Type == DocumentNodeType.dnTable)
                                                {
                                                    formatState.RowFormat.Trgaph = Converters.PxToTwipRounded(d);
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XANavigateUri:
                                        if (xamlTag == XamlTag.XTHyperlink && valueString.Length > 0)
                                        {
                                            StringBuilder sb = new StringBuilder();

                                            XamlParserHelper.AppendRTFText(sb, valueString, 0);
                                            documentNode.NavigateUri = sb.ToString();
                                        }
                                        break;

                                    case XamlAttribute.XAWidth:
                                        if (xamlTag == XamlTag.XTTableColumn)
                                        {
                                            double d = 0f;

                                            if (Converters.StringToDouble(valueString, ref d))
                                            {
                                                int nTableAt = dna.FindPending(DocumentNodeType.dnTable);
                                                if (nTableAt >= 0)
                                                {
                                                    DocumentNode dnTable = dna.EntryAt(nTableAt);
                                                    RowFormat rf = dnTable.FormatState.RowFormat;
                                                    CellFormat cf = rf.NextCellFormat();

                                                    cf.Width.Type = WidthType.WidthTwips;
                                                    cf.Width.Value = Converters.PxToTwipRounded(d);
                                                }
                                            }
                                        }
                                        else if (xamlTag == XamlTag.XTImage)
                                        {
                                            double d = 0f;
                                            Converters.StringToDouble(valueString, ref d);
                                            documentNode.FormatState.ImageWidth = d;
                                        }
                                        break;

                                    case XamlAttribute.XAHeight:
                                        if (xamlTag == XamlTag.XTImage)
                                        {
                                            double d = 0f;
                                            Converters.StringToDouble(valueString, ref d);
                                            documentNode.FormatState.ImageHeight = d;
                                        }
                                        break;

                                    case XamlAttribute.XABaselineOffset:
                                        if (xamlTag == XamlTag.XTImage)
                                        {
                                            double d = 0f;
                                            Converters.StringToDouble(valueString, ref d);
                                            documentNode.FormatState.ImageBaselineOffset = d;
                                            documentNode.FormatState.IncludeImageBaselineOffset = true;
                                        }
                                        break;

                                    case XamlAttribute.XASource:
                                        if (xamlTag == XamlTag.XTImage)
                                        {
                                            documentNode.FormatState.ImageSource = valueString;
                                        }
                                        break;

                                    case XamlAttribute.XAUriSource:
                                        if (xamlTag == XamlTag.XTBitmapImage)
                                        {
                                            documentNode.FormatState.ImageSource = valueString;
                                        }
                                        break;

                                    case XamlAttribute.XAStretch:
                                        if (xamlTag == XamlTag.XTImage)
                                        {
                                            documentNode.FormatState.ImageStretch = valueString;
                                        }
                                        break;

                                    case XamlAttribute.XAStretchDirection:
                                        if (xamlTag == XamlTag.XTImage)
                                        {
                                            documentNode.FormatState.ImageStretchDirection = valueString;
                                        }
                                        break;

                                    case XamlAttribute.XATypographyVariants:
                                        RtfSuperSubscript ss = RtfSuperSubscript.None;

                                        if (XamlParserHelper.ConvertToSuperSub(converterState, valueString, ref ss))
                                        {
                                            if (ss == RtfSuperSubscript.Super)
                                            {
                                                formatState.Super = true;
                                            }
                                            else if (ss == RtfSuperSubscript.Sub)
                                            {
                                                formatState.Sub = true;
                                            }
                                            else if (ss == RtfSuperSubscript.Normal)
                                            {
                                                formatState.Sub = false;
                                                formatState.Super = false;
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XAMarkerStyle:
                                        MarkerStyle ms = MarkerStyle.MarkerBullet;

                                        if (XamlParserHelper.ConvertToMarkerStyle(converterState, valueString, ref ms))
                                        {
                                            formatState.Marker = ms;
                                        }
                                        break;

                                    case XamlAttribute.XAStartIndex:
                                        int nStart = 0;

                                        if (XamlParserHelper.ConvertToStartIndex(converterState, valueString, ref nStart))
                                        {
                                            formatState.StartIndex = nStart;
                                        }
                                        break;

                                    case XamlAttribute.XAMargin:
                                        {
                                            XamlThickness thickness = new XamlThickness(0f, 0f, 0f, 0f);
                                            if (XamlParserHelper.ConvertToThickness(converterState, valueString, ref thickness))
                                            {
                                                formatState.LI = Converters.PxToTwipRounded(thickness.Left);
                                                formatState.RI = Converters.PxToTwipRounded(thickness.Right);
                                                formatState.SB = Converters.PxToTwipRounded(thickness.Top);
                                                formatState.SA = Converters.PxToTwipRounded(thickness.Bottom);
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XAPadding:
                                        {
                                            XamlThickness t = new XamlThickness(0f, 0f, 0f, 0f);
                                            if (XamlParserHelper.ConvertToThickness(converterState, valueString, ref t))
                                            {
                                                if (xamlTag == XamlTag.XTParagraph)
                                                {
                                                    // RTF only supports a single border padding value.
                                                    formatState.ParaBorder.Spacing = Converters.PxToTwipRounded(t.Left);
                                                }
                                                else
                                                {
                                                    RowFormat rf = formatState.RowFormat;
                                                    CellFormat cf = rf.RowCellFormat;
                                                    cf.PaddingLeft = Converters.PxToTwipRounded(t.Left);
                                                    cf.PaddingRight = Converters.PxToTwipRounded(t.Right);
                                                    cf.PaddingTop = Converters.PxToTwipRounded(t.Top);
                                                    cf.PaddingBottom = Converters.PxToTwipRounded(t.Bottom);
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XABorderThickness:
                                        {
                                            XamlThickness t = new XamlThickness(0f, 0f, 0f, 0f);
                                            if (XamlParserHelper.ConvertToThickness(converterState, valueString, ref t))
                                            {
                                                if (xamlTag == XamlTag.XTParagraph)
                                                {
                                                    ParaBorder pf = formatState.ParaBorder;
                                                    pf.BorderLeft.Type = BorderType.BorderSingle;
                                                    pf.BorderLeft.Width = Converters.PxToTwipRounded(t.Left);
                                                    pf.BorderRight.Type = BorderType.BorderSingle;
                                                    pf.BorderRight.Width = Converters.PxToTwipRounded(t.Right);
                                                    pf.BorderTop.Type = BorderType.BorderSingle;
                                                    pf.BorderTop.Width = Converters.PxToTwipRounded(t.Top);
                                                    pf.BorderBottom.Type = BorderType.BorderSingle;
                                                    pf.BorderBottom.Width = Converters.PxToTwipRounded(t.Bottom);
                                                }
                                                else
                                                {
                                                    RowFormat rf = formatState.RowFormat;
                                                    CellFormat cf = rf.RowCellFormat;
                                                    cf.BorderLeft.Type = BorderType.BorderSingle;
                                                    cf.BorderLeft.Width = Converters.PxToTwipRounded(t.Left);
                                                    cf.BorderRight.Type = BorderType.BorderSingle;
                                                    cf.BorderRight.Width = Converters.PxToTwipRounded(t.Right);
                                                    cf.BorderTop.Type = BorderType.BorderSingle;
                                                    cf.BorderTop.Width = Converters.PxToTwipRounded(t.Top);
                                                    cf.BorderBottom.Type = BorderType.BorderSingle;
                                                    cf.BorderBottom.Width = Converters.PxToTwipRounded(t.Bottom);
                                                }
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XABorderBrush:
                                        if (XamlParserHelper.ConvertToColor(converterState, valueString, ref valueData))
                                        {
                                            if (xamlTag == XamlTag.XTParagraph)
                                            {
                                                formatState.ParaBorder.CF = valueData;
                                            }
                                            else
                                            {
                                                RowFormat rf = formatState.RowFormat;
                                                CellFormat cf = rf.RowCellFormat;
                                                cf.BorderLeft.CF = valueData;
                                                cf.BorderRight.CF = valueData;
                                                cf.BorderTop.CF = valueData;
                                                cf.BorderBottom.CF = valueData;
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XATextIndent:
                                        double ti = 0;

                                        if (XamlParserHelper.ConvertToTextIndent(converterState, valueString, ref ti))
                                        {
                                            formatState.FI = Converters.PxToTwipRounded(ti);
                                        }
                                        break;

                                    case XamlAttribute.XALineHeight:
                                        double sl = 0;

                                        if (XamlParserHelper.ConvertToLineHeight(converterState, valueString, ref sl))
                                        {
                                            formatState.SL = Converters.PxToTwipRounded(sl);
                                            formatState.SLMult = false;
                                        }
                                        break;

                                    case XamlAttribute.XALang:
                                        {
                                            try
                                            {
                                                CultureInfo ci = new CultureInfo(valueString);

                                                if (ci.LCID > 0)
                                                {
                                                    // Extract LANGID from LCID
                                                    formatState.Lang = (long)(ushort)ci.LCID;
                                                }
                                            }
                                            catch (System.ArgumentException)
                                            {
                                                // Just omit if this is not a legal language value
                                            }
                                        }
                                        break;

                                    case XamlAttribute.XATextAlignment:
                                        HAlign halign = HAlign.AlignDefault;

                                        if (XamlParserHelper.ConvertToHAlign(converterState, valueString, ref halign))
                                        {
                                            formatState.HAlign = halign;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }

                return xamlToRtfError;
            }

            #endregion Private Methods

            // ---------------------------------------------------------------------
            //
            // Private Fields
            //
            // ---------------------------------------------------------------------

            #region Private Fields

            private string _xaml;
            private XamlToRtfWriter _writer;
            private XamlToRtfParser _parser;
            private bool _bGenListTables;

            #endregion Private Fields
        }

        internal static class XamlParserHelper
        {
            internal static LookupTableEntry[] TagTable =
            {
                new LookupTableEntry("",                (int)XamlTag.XTUnknown),
                new LookupTableEntry("",                (int)XamlTag.XTUnknown),
                new LookupTableEntry("Bold",            (int)XamlTag.XTBold),
                new LookupTableEntry("Italic",          (int)XamlTag.XTItalic),
                new LookupTableEntry("Underline",       (int)XamlTag.XTUnderline),
                new LookupTableEntry("Hyperlink",       (int)XamlTag.XTHyperlink),
                new LookupTableEntry("Span",            (int)XamlTag.XTInline),
                new LookupTableEntry("Run",             (int)XamlTag.XTInline),
                new LookupTableEntry("LineBreak",       (int)XamlTag.XTLineBreak),
                new LookupTableEntry("Paragraph",       (int)XamlTag.XTParagraph),
                new LookupTableEntry("InlineUIContainer",           (int)XamlTag.XTInline),
                new LookupTableEntry("BlockUIContainer",            (int)XamlTag.XTBlockUIContainer),
                new LookupTableEntry("Image",           (int)XamlTag.XTImage),
                new LookupTableEntry("BitmapImage",     (int)XamlTag.XTBitmapImage),
                new LookupTableEntry("List",            (int)XamlTag.XTList),
                new LookupTableEntry("ListItem",        (int)XamlTag.XTListItem),
                new LookupTableEntry("Table",           (int)XamlTag.XTTable),
                new LookupTableEntry("TableRowGroup",   (int)XamlTag.XTTableBody),
                new LookupTableEntry("TableRow",        (int)XamlTag.XTTableRow),
                new LookupTableEntry("TableCell",       (int)XamlTag.XTTableCell),
                new LookupTableEntry("TableColumn",     (int)XamlTag.XTTableColumn),
                new LookupTableEntry("Section",         (int)XamlTag.XTSection),
                new LookupTableEntry("Figure",          (int)XamlTag.XTFigure),
                new LookupTableEntry("Floater",         (int)XamlTag.XTFloater),

                // Complex Attributes
                new LookupTableEntry("TextDecoration",  (int)XamlTag.XTTextDecoration)
            };

            internal static LookupTableEntry[] AttributeTable =
            {
                new LookupTableEntry("",                    (int)XamlAttribute.XAUnknown),
                new LookupTableEntry("FontWeight",          (int)XamlAttribute.XAFontWeight),
                new LookupTableEntry("FontSize",            (int)XamlAttribute.XAFontSize),
                new LookupTableEntry("FontStyle",           (int)XamlAttribute.XAFontStyle),
                new LookupTableEntry("FontFamily",          (int)XamlAttribute.XAFontFamily),
                new LookupTableEntry("Background",          (int)XamlAttribute.XABackground),
                new LookupTableEntry("Foreground",          (int)XamlAttribute.XAForeground),
                new LookupTableEntry("FlowDirection",       (int)XamlAttribute.XAFlowDirection),
                new LookupTableEntry("TextDecorations",     (int)XamlAttribute.XATextDecorations),
                new LookupTableEntry("TextAlignment",       (int)XamlAttribute.XATextAlignment),
                new LookupTableEntry("MarkerStyle",         (int)XamlAttribute.XAMarkerStyle),
                new LookupTableEntry("TextIndent",          (int)XamlAttribute.XATextIndent),
                new LookupTableEntry("ColumnSpan",          (int)XamlAttribute.XAColumnSpan),
                new LookupTableEntry("RowSpan",             (int)XamlAttribute.XARowSpan),
                new LookupTableEntry("StartIndex",          (int)XamlAttribute.XAStartIndex),
                new LookupTableEntry("MarkerOffset",        (int)XamlAttribute.XAMarkerOffset),
                new LookupTableEntry("BorderThickness",     (int)XamlAttribute.XABorderThickness),
                new LookupTableEntry("BorderBrush",         (int)XamlAttribute.XABorderBrush),
                new LookupTableEntry("Padding",             (int)XamlAttribute.XAPadding),
                new LookupTableEntry("Margin",              (int)XamlAttribute.XAMargin),
                new LookupTableEntry("KeepTogether",        (int)XamlAttribute.XAKeepTogether),
                new LookupTableEntry("KeepWithNext",        (int)XamlAttribute.XAKeepWithNext),
                new LookupTableEntry("BaselineAlignment",   (int)XamlAttribute.XABaselineAlignment),
                new LookupTableEntry("BaselineOffset",      (int)XamlAttribute.XABaselineOffset),
                new LookupTableEntry("NavigateUri",         (int)XamlAttribute.XANavigateUri),
                new LookupTableEntry("TargetName",          (int)XamlAttribute.XATargetName),
                new LookupTableEntry("LineHeight",          (int)XamlAttribute.XALineHeight),
                new LookupTableEntry("xml:lang",            (int)XamlAttribute.XALang),
                new LookupTableEntry("Height",              (int)XamlAttribute.XAHeight),
                new LookupTableEntry("Source",              (int)XamlAttribute.XASource),
                new LookupTableEntry("UriSource",           (int)XamlAttribute.XAUriSource),
                new LookupTableEntry("Stretch",             (int)XamlAttribute.XAStretch),
                new LookupTableEntry("StretchDirection",    (int)XamlAttribute.XAStretchDirection),

                // Complex Attributes
                new LookupTableEntry("Location",            (int)XamlAttribute.XALocation),
                new LookupTableEntry("Width",               (int)XamlAttribute.XAWidth),
                new LookupTableEntry("Typography.Variants", (int)XamlAttribute.XATypographyVariants)
            };

            internal static LookupTableEntry[] MarkerStyleTable =
            {
                new LookupTableEntry("",                (int)MarkerStyle.MarkerBullet),
                new LookupTableEntry("None",            (int)MarkerStyle.MarkerNone),
                new LookupTableEntry("Decimal",         (int)MarkerStyle.MarkerArabic),
                new LookupTableEntry("UpperRoman",      (int)MarkerStyle.MarkerUpperRoman),
                new LookupTableEntry("LowerRoman",      (int)MarkerStyle.MarkerLowerRoman),
                new LookupTableEntry("UpperLatin",      (int)MarkerStyle.MarkerUpperAlpha),
                new LookupTableEntry("LowerLatin",      (int)MarkerStyle.MarkerLowerAlpha),
                new LookupTableEntry("Ordinal",         (int)MarkerStyle.MarkerOrdinal),
                new LookupTableEntry("Decimal",         (int)MarkerStyle.MarkerCardinal),   // Note no support in XAML
                new LookupTableEntry("Disc",            (int)MarkerStyle.MarkerBullet),
                new LookupTableEntry("Box",             (int)MarkerStyle.MarkerBullet),
                new LookupTableEntry("Circle",          (int)MarkerStyle.MarkerBullet),
                new LookupTableEntry("Square",          (int)MarkerStyle.MarkerBullet)
            };

            internal static LookupTableEntry[] HAlignTable =
            {
                new LookupTableEntry("",                (int)HAlign.AlignDefault),
                new LookupTableEntry("Left",            (int)HAlign.AlignLeft),
                new LookupTableEntry("Right",           (int)HAlign.AlignRight),
                new LookupTableEntry("Center",          (int)HAlign.AlignCenter),
                new LookupTableEntry("Justify",         (int)HAlign.AlignJustify),
            };

            internal static LookupTableEntry[] FontStretchTable =
            {
                new LookupTableEntry("",               0),
                new LookupTableEntry("Normal",         0),
                new LookupTableEntry("UltraCondensed", -80),
                new LookupTableEntry("ExtraCondensed", -60),
                new LookupTableEntry("Condensed",      -40),
                new LookupTableEntry("SemiCondensed",  -20),
                new LookupTableEntry("SemiExpanded",   20),
                new LookupTableEntry("Expanded",       40),
                new LookupTableEntry("ExtraExpanded",  60),
                new LookupTableEntry("UltraExpanded",  80),
            };

            internal static LookupTableEntry[] TypographyVariantsTable =
            {
                new LookupTableEntry("Normal", (int)RtfSuperSubscript.Normal),
                new LookupTableEntry("Superscript", (int)RtfSuperSubscript.Super),
                new LookupTableEntry("Subscript", (int)RtfSuperSubscript.Sub),
            };

            internal static int BasicLookup(LookupTableEntry[] entries, string name)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (string.Compare(entries[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return entries[i].Value;
                    }
                }

                return 0;
            }

            internal static bool ConvertToTag(ConverterState converterState, string tagName, ref XamlTag xamlTag)
            {
                if (tagName.Length == 0)
                {
                    return false;
                }

                xamlTag = (XamlTag)BasicLookup(TagTable, tagName);

                return xamlTag != XamlTag.XTUnknown;
            }

            internal static bool ConvertToSuperSub(ConverterState converterState, string s, ref RtfSuperSubscript ss)
            {
                if (s.Length == 0)
                {
                    return false;
                }

                ss = (RtfSuperSubscript)BasicLookup(TypographyVariantsTable, s);

                return ss != RtfSuperSubscript.None;
            }

            internal static bool ConvertToAttribute(ConverterState converterState, string attributeName, ref XamlAttribute xamlAttribute)
            {
                if (attributeName.Length == 0)
                {
                    return false;
                }

                xamlAttribute = (XamlAttribute)BasicLookup(AttributeTable, attributeName);

                return xamlAttribute != XamlAttribute.XAUnknown;
            }

            internal static bool ConvertToFont(ConverterState converterState, string attributeName, ref long fontIndex)
            {
                if (attributeName.Length == 0)
                {
                    return false;
                }

                FontTable fontTable = converterState.FontTable;
                FontTableEntry fontTableEntry = fontTable.FindEntryByName(attributeName);

                if (fontTableEntry == null)
                {
                    fontTableEntry = fontTable.DefineEntry(fontTable.Count + 1);

                    if (fontTableEntry != null)
                    {
                        fontTableEntry.Name = attributeName;
                        fontTableEntry.ComputePreferredCodePage();
                    }
                }

                if (fontTableEntry == null)
                {
                    return false;
                }

                fontIndex = fontTableEntry.Index;

                return true;
            }

            internal static bool ConvertToFontSize(ConverterState converterState, string s, ref double d)
            {
                if (s.Length == 0)
                {
                    return false;
                }

                // Peel trailing units
                int n = s.Length - 1;
                while (n >= 0 && (s[n] < '0' || s[n] > '9') && s[n] != '.')
                {
                    n--;
                }

                string units = null;
                if (n < s.Length - 1)
                {
                    units = s.Substring(n + 1);
                    s = s.Substring(0, n + 1);
                }

                // Now convert number part
                bool ret = Converters.StringToDouble(s, ref d);

                if (ret)
                {
                    // No units mean pixels
                    if (units == null || units.Length == 0)
                    {
                        d = Converters.PxToPt(d);
                    }
                    // else
                    // Otherwise assume points - no conversion necessary.

                    // Convert to half-points used by RTF
                    d *= 2;
                }

                return ret && d > 0;
            }

            internal static bool ConvertToTextIndent(ConverterState converterState, string s, ref double d)
            {
                return Converters.StringToDouble(s, ref d);
            }

            internal static bool ConvertToLineHeight(ConverterState converterState, string s, ref double d)
            {
                return Converters.StringToDouble(s, ref d);
            }

            internal static bool ConvertToColor(ConverterState converterState, string brush, ref long colorIndex)
            {
                if (brush.Length == 0)
                {
                    return false;
                }

                ColorTable colorTable = converterState.ColorTable;

                // Hex?
                if (brush[0] == '#')
                {
                    // Move past # symbol
                    int brushStringIndex = 1;

                    // Now gather RGB
                    uint colorValue = 0;

                    for (; brushStringIndex < brush.Length && brushStringIndex < 9; brushStringIndex++)
                    {
                        char colorChar = brush[brushStringIndex];

                        if (colorChar >= '0' && colorChar <= '9')
                        {
                            colorValue = (uint)((colorValue << 4) + (colorChar - '0'));
                        }
                        else if (colorChar >= 'A' && colorChar <= 'F')
                        {
                            colorValue = (uint)((colorValue << 4) + (colorChar - 'A' + 10));
                        }
                        else if (colorChar >= 'a' && colorChar <= 'f')
                        {
                            colorValue = (uint)((colorValue << 4) + (colorChar - 'a' + 10));
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Computation above actually has r and b flipped.
                    Color color = Color.FromRgb((byte)((colorValue & 0x00ff0000) >> 16),
                                                (byte)((colorValue & 0x0000ff00) >> 8),
                                                (byte)(colorValue & 0x000000ff));

                    colorIndex = colorTable.AddColor(color);

                    return colorIndex >= 0;   // -1 indicates failure, otherwise 0-based offset into colortable.
                }
                else
                {
                    try
                    {
                        Color color = (Color)ColorConverter.ConvertFromString(brush);

                        colorIndex = colorTable.AddColor(color);

                        return colorIndex >= 0;   // -1 indicates failure, otherwise 0-based offset into colortable.
                    }
                    catch (System.NotSupportedException)
                    {
                        return false;
                    }
                    catch (System.FormatException)
                    {
                        return false;
                    }
                }
            }

            internal static bool ConvertToDecoration(
                ConverterState converterState,
                string decoration,
                ref ULState ulState,
                ref StrikeState strikeState
                )
            {
                ulState = ULState.ULNone;
                strikeState = StrikeState.StrikeNone;
                if (decoration.IndexOf("Underline", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    ulState = ULState.ULNormal;
                }
                if (decoration.IndexOf("Strikethrough", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    strikeState = StrikeState.StrikeNormal;
                }

                return ulState != ULState.ULNone || strikeState != StrikeState.StrikeNone;
            }

            internal static bool ConvertToDir(ConverterState converterState, string dirName, ref DirState dirState)
            {
                if (dirName.Length == 0)
                    return false;

                if (string.Compare("RightToLeft", dirName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    dirState = DirState.DirRTL;
                    return true;
                }
                else if (string.Compare("LeftToRight", dirName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    dirState = DirState.DirLTR;
                    return true;
                }
                return false;
            }

            internal static bool ConvertTagToNodeType(XamlTag xamlTag, ref DocumentNodeType documentNodeType)
            {
                documentNodeType = DocumentNodeType.dnUnknown;

                switch (xamlTag)
                {
                    default:
                    case XamlTag.XTUnknown:
                        return false;

                    case XamlTag.XTInline:
                    case XamlTag.XTItalic:
                    case XamlTag.XTUnderline:
                    case XamlTag.XTBold:
                        documentNodeType = DocumentNodeType.dnInline;
                        break;

                    case XamlTag.XTHyperlink:
                        documentNodeType = DocumentNodeType.dnHyperlink;
                        break;

                    case XamlTag.XTLineBreak:
                        documentNodeType = DocumentNodeType.dnLineBreak;
                        break;

                    case XamlTag.XTInlineUIContainer:
                        documentNodeType = DocumentNodeType.dnInlineUIContainer;
                        break;

                    case XamlTag.XTBlockUIContainer:
                        documentNodeType = DocumentNodeType.dnBlockUIContainer;
                        break;

                    case XamlTag.XTImage:
                        documentNodeType = DocumentNodeType.dnImage;
                        break;

                    case XamlTag.XTParagraph:
                        documentNodeType = DocumentNodeType.dnParagraph;
                        break;

                    case XamlTag.XTSection:
                        documentNodeType = DocumentNodeType.dnSection;
                        break;

                    case XamlTag.XTList:
                        documentNodeType = DocumentNodeType.dnList;
                        break;

                    case XamlTag.XTListItem:
                        documentNodeType = DocumentNodeType.dnListItem;
                        break;

                    case XamlTag.XTTable:
                        documentNodeType = DocumentNodeType.dnTable;
                        break;

                    case XamlTag.XTTableBody:
                        documentNodeType = DocumentNodeType.dnTableBody;
                        break;

                    case XamlTag.XTTableRow:
                        documentNodeType = DocumentNodeType.dnRow;
                        break;

                    case XamlTag.XTTableCell:
                        documentNodeType = DocumentNodeType.dnCell;
                        break;
                }

                return true;
            }

            internal static bool ConvertToMarkerStyle(ConverterState converterState, string styleName, ref MarkerStyle ms)
            {
                ms = MarkerStyle.MarkerBullet;

                if (styleName.Length == 0)
                {
                    return false;
                }

                ms = (MarkerStyle)BasicLookup(MarkerStyleTable, styleName);

                return true;
            }

            internal static bool ConvertToStartIndex(ConverterState converterState, string s, ref int i)
            {
                bool ret = true;

                try
                {
                    i = System.Convert.ToInt32(s, CultureInfo.InvariantCulture);
                }
                catch (System.OverflowException)
                {
                    ret = false;
                }
                catch (System.FormatException)
                {
                    ret = false;
                }
                return ret;
            }

            internal static bool ConvertToThickness(ConverterState converterState, string thickness, ref XamlThickness xthickness)
            {
                int numints = 0;
                int s = 0;

                while (s < thickness.Length)
                {
                    int e = s;

                    while (e < thickness.Length && thickness[e] != ',')
                    {
                        e++;
                    }

                    string onenum = thickness.Substring(s, e - s);
                    if (onenum.Length > 0)
                    {
                        double d = 0.0f;
                        if (!Converters.StringToDouble(onenum, ref d))
                        {
                            return false;
                        }
                        switch (numints)
                        {
                            case 0:
                                xthickness.Left = (float)d;
                                break;
                            case 1:
                                xthickness.Top = (float)d;
                                break;
                            case 2:
                                xthickness.Right = (float)d;
                                break;
                            case 3:
                                xthickness.Bottom = (float)d;
                                break;
                            default:
                                return false;
                        }
                        numints++;
                    }
                    s = e + 1;
                }

                // One value means same value for all sides.
                if (numints == 1)
                {
                    xthickness.Top = xthickness.Left;
                    xthickness.Right = xthickness.Left;
                    xthickness.Bottom = xthickness.Left;
                    numints = 4;
                }
                return (numints == 4);
            }

            internal static bool ConvertToHAlign(ConverterState converterState, string alignName, ref HAlign align)
            {
                if (alignName.Length == 0)
                {
                    return false;
                }

                align = (HAlign)BasicLookup(HAlignTable, alignName);

                return true;
            }

            internal static bool ConvertToFontStretch(ConverterState converterState, string stretchName, ref long twips)
            {
                if (stretchName.Length == 0)
                {
                    return false;
                }

                twips = (long)BasicLookup(HAlignTable, stretchName);

                return true;
            }

            internal static void AppendRTFText(StringBuilder sb, string s, int cp)
            {
                // Default encoding is 1252
                if (cp <= 0)
                {
                    cp = 1252;
                }

                Encoding e = null;
                byte[] rgAnsi = new byte[20];
                char[] rgChar = new char[20];

                for (int i = 0; i < s.Length; i++)
                {
                    AppendRtfChar(sb, s[i], cp, ref e, rgAnsi, rgChar);
                }
            }

            internal static void EnsureParagraphClosed(ConverterState converterState)
            {
                DocumentNodeArray dna = converterState.DocumentNodeArray;

                int paragraphIndex = dna.FindPending(DocumentNodeType.dnParagraph);

                if (paragraphIndex >= 0)
                {
                    DocumentNode documentNodeParagraph = dna.EntryAt(paragraphIndex);

                    dna.CloseAt(paragraphIndex);
                }
            }

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            private static void AppendRtfChar(StringBuilder sb, char c, int cp, ref Encoding e, byte[] rgAnsi, char[] rgChar)
            {
                // Escape special characters
                if (c == '{' || c == '}' || c == '\\')
                {
                    sb.Append('\\');
                }

                // LOW-1252 is encoded directly
                if (c == '\t')
                {
                    sb.Append("\\tab ");
                }
                else if (c == '\f')
                {
                    sb.Append("\\page ");
                }
                else if (c < 128)
                {
                    sb.Append(c);
                }
                else
                {
                    // Certain characters are handled as keywords for max interoperability
                    switch (c)
                    {
                        case '\xA0':
                            sb.Append("\\~");   // NBSP
                            break;
                        case '\x2014':
                            sb.Append("\\emdash ");
                            break;
                        case '\x2013':
                            sb.Append("\\endash ");
                            break;
                        case '\x2003':
                            sb.Append("\\emspace ");
                            break;
                        case '\x2002':
                            sb.Append("\\enspace ");
                            break;
                        case '\x2005':
                            sb.Append("\\qmspace ");
                            break;
                        case '\x2022':
                            sb.Append("\\bullet ");
                            break;
                        case '\x2018':
                            sb.Append("\\lquote ");
                            break;
                        case '\x2019':
                            sb.Append("\\rquote ");
                            break;
                        case '\x201c':
                            sb.Append("\\ldblquote ");
                            break;
                        case '\x201d':
                            sb.Append("\\rdblquote ");
                            break;
                        case '\x200d':
                            sb.Append("\\zwj ");
                            break;
                        case '\x200c':
                            sb.Append("\\zwnj ");
                            break;
                        case '\x200e':
                            sb.Append("\\ltrmark ");
                            break;
                        case '\x200f':
                            sb.Append("\\rtlmark ");
                            break;
                        case '\x2011':
                            sb.Append("\\_");
                            break;

                        // Other Unicode is encoded as hex or \u
                        default:
                            AppendRtfUnicodeChar(sb, c, cp, ref e, rgAnsi, rgChar);
                            break;
                    }
                }
            }

            private static void AppendRtfUnicodeChar(StringBuilder sb, char c, int cp, ref Encoding e, byte[] rgAnsi, char[] rgChar)
            {
                if (e == null)
                {
                    e = Encoding.GetEncoding(cp);
                }
                int cb = e.GetBytes(new char[] { c }, 0, 1, rgAnsi, 0);
                int cch = e.GetChars(rgAnsi, 0, cb, rgChar, 0);

                // If I successfully encoded, cch should be 1 and rgChars[0] == c
                if (cch == 1 && rgChar[0] == c)
                {
                    for (int k = 0; k < cb; k++)
                    {
                        sb.Append("\\'");
                        sb.Append(rgAnsi[k].ToString("x", CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    sb.Append("\\u");
                    short sc = (short)c;
                    sb.Append(sc.ToString(CultureInfo.InvariantCulture));
                    sb.Append("?");
                }
            }

            #endregion Private Methods

            //------------------------------------------------------
            //
            //  Private Class
            //
            //------------------------------------------------------

            #region Private Class

            internal struct LookupTableEntry
            {
                internal LookupTableEntry(string name, int value)
                {
                    _name = name;
                    _value = value;
                }

                internal string Name
                {
                    get
                    {
                        return _name;
                    }
                }

                internal int Value
                {
                    get
                    {
                        return _value;
                    }
                }

                private string _name;
                private int _value;
            }

            #endregion Private Class
        }

        internal struct XamlThickness
        {
            internal XamlThickness(float l, float t, float r, float b)
            {
                _left = l;
                _top = t;
                _right = r;
                _bottom = b;
            }

            internal float Left
            {
                get
                {
                    return _left;
                }
                set
                {
                    _left = value;
                }
            }

            internal float Top
            {
                get
                {
                    return _top;
                }
                set
                {
                    _top = value;
                }
            }

            internal float Right
            {
                get
                {
                    return _right;
                }
                set
                {
                    _right = value;
                }
            }

            internal float Bottom
            {
                get
                {
                    return _bottom;
                }
                set
                {
                    _bottom = value;
                }
            }

            private float _left;
            private float _top;
            private float _right;
            private float _bottom;
        }

        #endregion Private Class
    }
}
