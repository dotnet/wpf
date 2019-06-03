// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements fast, paginated search functionality for Fixed documents
//      
//

namespace System.Windows.Documents
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Text;
    using System.Globalization;
    using System.Diagnostics;
    using System.Windows.Markup;

    internal sealed class FixedFindEngine
    {
        //Searches for the specified pattern and updates start *or* end pointers depending on search direction
        //At the end of the operation, start or end should be pointing to the beginning/end of the page
        //of occurance of pattern respectively
        internal static TextRange Find  ( ITextPointer start, 
                                           ITextPointer end,
                                           string findPattern,
                                           CultureInfo cultureInfo,
                                           bool matchCase,
                                           bool matchWholeWord,
                                           bool matchLast,
                                           bool matchDiacritics,
                                           bool matchKashida,
                                           bool matchAlefHamza)
        {
            Debug.Assert(start != null);
            Debug.Assert(end != null);
            Debug.Assert( ((start is DocumentSequenceTextPointer) && (end is DocumentSequenceTextPointer)) ||
                          ((start is FixedTextPointer) && (end is FixedTextPointer)) );
            Debug.Assert(findPattern != null);
            
            if (findPattern.Length == 0)
            {
                return null;
            }

            IDocumentPaginatorSource paginatorSource = start.TextContainer.Parent as IDocumentPaginatorSource;
            DynamicDocumentPaginator paginator = paginatorSource.DocumentPaginator as DynamicDocumentPaginator;
            Debug.Assert(paginator != null);
            
            int pageNumber = -1;
            int endPageNumber = -1;

            if (matchLast)
            {
                endPageNumber = paginator.GetPageNumber( (ContentPosition) start);
                pageNumber = paginator.GetPageNumber( (ContentPosition) end);
            }
            else
            {
                endPageNumber = paginator.GetPageNumber( (ContentPosition) end);
                pageNumber = paginator.GetPageNumber( (ContentPosition) start);
            }
            
            TextRange result = null;

            CompareInfo compareInfo = cultureInfo.CompareInfo;
            bool replaceAlefWithAlefHamza = false;
            CompareOptions compareOptions = _InitializeSearch(cultureInfo, matchCase, matchAlefHamza, matchDiacritics, ref findPattern, out replaceAlefWithAlefHamza);

            //Translate the page number
            int translatedPageNumber = pageNumber;
            //If this is a DocumentSequence, we need to pass translated page number to the below call
            FixedDocumentSequence documentSequence = paginatorSource as FixedDocumentSequence;
            DynamicDocumentPaginator childPaginator = null;

            if (documentSequence != null)
            {
                documentSequence.TranslatePageNumber(pageNumber, out childPaginator, out translatedPageNumber);
            }
            
            if (pageNumber - endPageNumber != 0)
            {
                ITextPointer firstSearchPageStart = null;
                ITextPointer firstSearchPageEnd = null;
                    
                _GetFirstPageSearchPointers(start, end, translatedPageNumber, matchLast, out firstSearchPageStart, out firstSearchPageEnd);

                Debug.Assert(firstSearchPageStart != null);
                Debug.Assert(firstSearchPageEnd != null);

                //Need to search the first page using TextFindEngine to start exactly from the requested search location to avoid false positives
                result = TextFindEngine.InternalFind( firstSearchPageStart, 
                                                      firstSearchPageEnd, 
                                                      findPattern, 
                                                      cultureInfo, 
                                                      matchCase, 
                                                      matchWholeWord, 
                                                      matchLast,
                                                      matchDiacritics,
                                                      matchKashida,
                                                      matchAlefHamza);
                if (result == null)
                {
                    //Start from the next page and check all pages until the end
                    pageNumber = matchLast ? pageNumber-1 : pageNumber+1;
                    int increment = matchLast ? -1 : 1;
                    for (; matchLast ? pageNumber >= endPageNumber : pageNumber <= endPageNumber; pageNumber+=increment)
                    {
                        FixedDocument fixedDoc = null;

                        translatedPageNumber = pageNumber;
                        childPaginator = null;
                        if (documentSequence != null)
                        {
                            documentSequence.TranslatePageNumber(pageNumber, out childPaginator, out translatedPageNumber);
                            fixedDoc = (FixedDocument) childPaginator.Source;
                        }
                        else 
                        {
                            fixedDoc = paginatorSource as FixedDocument;
                        }
                        
                        Debug.Assert(fixedDoc != null);
                        
                        String pageString = _GetPageString(fixedDoc, translatedPageNumber, replaceAlefWithAlefHamza);

                        if (pageString == null)
                        {
                            //This is not a page-per-stream
                            //Default back to slow search
                           return TextFindEngine.InternalFind( start, 
                                                      end, 
                                                      findPattern, 
                                                      cultureInfo, 
                                                      matchCase, 
                                                      matchWholeWord, 
                                                      matchLast,
                                                      matchDiacritics,
                                                      matchKashida,
                                                      matchAlefHamza);
                        }

                        if ( _FoundOnPage(pageString, findPattern, cultureInfo, compareOptions) )
                        {
                            //Update end or start pointer depending on search direction
                            if (documentSequence != null)
                            {
                                ChildDocumentBlock childBlock = documentSequence.TextContainer.FindChildBlock(fixedDoc.DocumentReference);
                                if (matchLast)
                                {
                                    end = new DocumentSequenceTextPointer(childBlock, new FixedTextPointer(false, LogicalDirection.Backward, fixedDoc.FixedContainer.FixedTextBuilder.GetPageEndFlowPosition(translatedPageNumber)));
                                    start = new DocumentSequenceTextPointer(childBlock, new FixedTextPointer(false, LogicalDirection.Forward, fixedDoc.FixedContainer.FixedTextBuilder.GetPageStartFlowPosition(translatedPageNumber)));
                                }
                                else
                                {
                                    start = new DocumentSequenceTextPointer(childBlock, new FixedTextPointer(false, LogicalDirection.Forward, fixedDoc.FixedContainer.FixedTextBuilder.GetPageStartFlowPosition(translatedPageNumber)));
                                    end = new DocumentSequenceTextPointer(childBlock, new FixedTextPointer(false, LogicalDirection.Backward, fixedDoc.FixedContainer.FixedTextBuilder.GetPageEndFlowPosition(translatedPageNumber)));
                                }
                            }
                            else
                            {
                                //We are working on a FixedDocument
                                FixedTextBuilder textBuilder = ((FixedDocument)(paginatorSource)).FixedContainer.FixedTextBuilder;
                                if (matchLast)
                                {
                                    end = new FixedTextPointer(false, LogicalDirection.Backward, textBuilder.GetPageEndFlowPosition(pageNumber));
                                    start = new FixedTextPointer(false, LogicalDirection.Forward, textBuilder.GetPageStartFlowPosition(pageNumber));
                                }
                                else
                                {
                                    start = new FixedTextPointer(false, LogicalDirection.Forward, textBuilder.GetPageStartFlowPosition(pageNumber));
                                    end = new FixedTextPointer(false, LogicalDirection.Backward, textBuilder.GetPageEndFlowPosition(pageNumber));
                                }
                            }
                            result =  TextFindEngine.InternalFind( start, 
                                                  end, 
                                                  findPattern, 
                                                  cultureInfo, 
                                                  matchCase, 
                                                  matchWholeWord, 
                                                  matchLast,
                                                  matchDiacritics,
                                                  matchKashida,
                                                  matchAlefHamza);

                            //If the result is null, this means we had a false positive
                            if (result != null)
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            
            else
            {
                //Make sure fast search result and slow search result are consistent
                FixedDocument fixedDoc = childPaginator != null ? childPaginator.Source as FixedDocument : paginatorSource as FixedDocument;
                String pageString = _GetPageString(fixedDoc, translatedPageNumber, replaceAlefWithAlefHamza);
                if (pageString == null ||
                    _FoundOnPage(pageString, findPattern, cultureInfo, compareOptions))
                {
                    //The search is only limited to the current page
                    result = TextFindEngine.InternalFind( start, 
                                                      end, 
                                                      findPattern, 
                                                      cultureInfo, 
                                                      matchCase, 
                                                      matchWholeWord, 
                                                      matchLast,
                                                      matchDiacritics,
                                                      matchKashida,
                                                      matchAlefHamza);
                }
            }
            
            return result;
        }

        private static bool _FoundOnPage(string pageString, 
                                                string findPattern, 
                                                CultureInfo cultureInfo, 
                                                CompareOptions compareOptions)
        {
            CompareInfo compareInfo = cultureInfo.CompareInfo;
            //We don't need to use LastIndexOf in this function even if mathcLast is true because
            //we are only trying to determine whether the pattern exists on a specific page or not.
            //Exact location will be determined in TextFindEngine.InternalFind

            string[] tokens = findPattern.Split(null);
            if (tokens != null)
            {
                foreach (string token in tokens)
                {
                    if (!String.IsNullOrEmpty(token) &&
                        compareInfo.IndexOf(pageString, token, compareOptions) == -1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static CompareOptions _InitializeSearch (CultureInfo cultureInfo, 
                                                            bool matchCase, 
                                                            bool matchAlefHamza, 
                                                            bool matchDiacritics, 
                                                            ref string findPattern, 
                                                            out bool replaceAlefWithAlefHamza)
        {
            CompareOptions compareOptions = CompareOptions.None;
            replaceAlefWithAlefHamza = false;
            
            if (!matchCase)
            {
                compareOptions |= CompareOptions.IgnoreCase;
            }

            bool stringContainedBidiCharacter;
            bool stringContainedAlefCharacter;

            // Initialize Bidi flags whether the string contains the bidi characters
            // or alef character.
            TextFindEngine.InitializeBidiFlags(
                findPattern, 
                out stringContainedBidiCharacter, 
                out stringContainedAlefCharacter);

            if (stringContainedAlefCharacter && !matchAlefHamza)
            {
                // Replace the alef-hamza with the alef.
                findPattern = TextFindEngine.ReplaceAlefHamzaWithAlef(findPattern);
                replaceAlefWithAlefHamza = true;
            }

            // Ignore Bidi diacritics that use for only Bidi language.
            if (!matchDiacritics && stringContainedBidiCharacter)
            {
                // Ignore Bidi diacritics with checking non-space character.
                compareOptions |= CompareOptions.IgnoreNonSpace;
            }
            return compareOptions;
        }


        private static void _GetFirstPageSearchPointers ( ITextPointer start, 
                                                                   ITextPointer end, 
                                                                   int pageNumber,
                                                                   bool matchLast,
                                                                   out ITextPointer firstSearchPageStart, 
                                                                   out ITextPointer firstSearchPageEnd)
        {
            if (matchLast)
            {
                //The page in question is the last page
                //Need to search between the start of the last page and the end pointer
                DocumentSequenceTextPointer endAsDSTP = end as DocumentSequenceTextPointer;
                if (endAsDSTP != null)
                {
                    FlowPosition pageStartFlowPosition = ((FixedTextContainer)(endAsDSTP.ChildBlock.ChildContainer)).FixedTextBuilder.GetPageStartFlowPosition(pageNumber);
                    firstSearchPageStart = new DocumentSequenceTextPointer(endAsDSTP.ChildBlock, 
                                                                           new FixedTextPointer(false, LogicalDirection.Forward,pageStartFlowPosition));
                }
                else
                {
                    FixedTextPointer endAsFTP = end as FixedTextPointer;
                    Debug.Assert(endAsFTP != null);
                    firstSearchPageStart = new FixedTextPointer(false, LogicalDirection.Forward, endAsFTP.FixedTextContainer.FixedTextBuilder.GetPageStartFlowPosition(pageNumber));
                }
               
                firstSearchPageEnd = end;
            }
            else
            {
                //The page in question is the first page
                //Need to search between the start pointer and the end of the first page
                DocumentSequenceTextPointer startAsDSTP = start as DocumentSequenceTextPointer;
                if (startAsDSTP != null)
                {
                    FlowPosition pageEndFlowPosition = ((FixedTextContainer)startAsDSTP.ChildBlock.ChildContainer).FixedTextBuilder.GetPageEndFlowPosition(pageNumber);
                    firstSearchPageEnd = new DocumentSequenceTextPointer( startAsDSTP.ChildBlock,
                                                                          new FixedTextPointer(false, LogicalDirection.Backward, pageEndFlowPosition));
                }
                else
                {
                    FixedTextPointer startAsFTP = start as FixedTextPointer;
                    Debug.Assert(startAsFTP != null);
                    firstSearchPageEnd = new FixedTextPointer(false, LogicalDirection.Backward, startAsFTP.FixedTextContainer.FixedTextBuilder.GetPageEndFlowPosition(pageNumber));
                }
                firstSearchPageStart = start;
            }
        }

        private static String _GetPageString(FixedDocument doc, int translatedPageNo, bool replaceAlefWithAlefHamza)
        {
            String pageString = null;
            
            Debug.Assert(doc != null);
            Debug.Assert(translatedPageNo >= 0 && translatedPageNo < doc.PageCount);
            
            PageContent pageContent = doc.Pages[translatedPageNo];
            Stream pageStream = pageContent.GetPageStream();
            bool reverseRTL = true;
            if (doc.HasExplicitStructure)
            {
                reverseRTL = false;
            }
            if (pageStream != null)
            {
                pageString = _ConstructPageString(pageStream, reverseRTL);

                if (replaceAlefWithAlefHamza)
                {
                    // Replace the alef-hamza with the alef.
                    pageString = TextFindEngine.ReplaceAlefHamzaWithAlef(pageString);
                }                
            }
            return pageString;
        }


        private static String _ConstructPageString(Stream pageStream, bool reverseRTL)
        {
            Debug.Assert(pageStream != null);
            
            XmlTextReader xmlTextReader = new XmlTextReader(pageStream);

            //Wrap around a compatibility reader
            XmlReader xmlReader = new XmlCompatibilityReader(xmlTextReader, _predefinedNamespaces);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            settings.ProhibitDtd = true;

            xmlReader = XmlReader.Create(xmlReader, settings);

            xmlReader.MoveToContent();

            StringBuilder pageString = new StringBuilder();
            bool isSideways = false;
            string unicodeStr = null;
            
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                    {
                        if (xmlReader.Name == "Glyphs")
                        {
                            unicodeStr = xmlReader.GetAttribute("UnicodeString");

                            if (!String.IsNullOrEmpty(unicodeStr))
                            {
                                string sidewaysString = xmlReader.GetAttribute("IsSideways");
                                isSideways = false;
                                if (sidewaysString != null &&
                                    String.Compare(sidewaysString, Boolean.TrueString, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    isSideways = true;
                                }
                                
                                if (reverseRTL)
                                {
                                    //This is to cover for MXDW generation
                                    //RTL Glyphs are saved LTR and bidi level is not set
                                    //In this case we need to reverse the UnicodeString
                                    string bidiLevelAsString = xmlReader.GetAttribute("BidiLevel");
                                    int bidiLevel = 0;
                                    if (!String.IsNullOrEmpty(bidiLevelAsString))
                                    {
                                        try
                                        {
                                            bidiLevel = Convert.ToInt32(bidiLevelAsString, CultureInfo.InvariantCulture);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                
                                    string caretStops = xmlReader.GetAttribute("CaretStops");
                                                                        
                                    if (bidiLevel == 0 && 
                                        !isSideways &&
                                        String.IsNullOrEmpty(caretStops) &&
                                        FixedTextBuilder.MostlyRTL(unicodeStr))
                                    {
                                        char[] chars = unicodeStr.ToCharArray();
                                        Array.Reverse(chars);
                                        unicodeStr = new String(chars);
                                    }
                                }
                                

                                pageString.Append(unicodeStr);
                            }
                        }
                    }
                    break;
                }
            }
            return pageString.ToString();
        }

     
        //Private constructor to prevent the compiler from generating a default constructor (fxcop)
        private FixedFindEngine()
        {
        }


        static private string [] _predefinedNamespaces = new string [2] { 
            "http://schemas.microsoft.com/xps/2005/06",
            XamlReaderHelper.DefinitionMetroNamespaceURI
        };
    }
}

