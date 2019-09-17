// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Implements an indexing filter for XAML streams.
//              Invoked by the PackageFilter.
//
#if DEBUG
//  #define TRACE
#endif

using System;
using System.IO;
using System.Xml;
using MS.Win32;                         // For SafeNativeMethods
using System.Globalization;             // For CultureInfo
using System.Diagnostics;               // For Assert
using System.Collections;               // For Stack and Hashtable
using System.Collections.Generic;       // For List<>
using System.Runtime.InteropServices;   // For COMException
using System.Runtime.InteropServices.ComTypes;   // For IStream, etc.
using System.Windows;                   // for ExceptionStringTable
using MS.Internal.Interop;  // for CHUNK_BREAKTYPE (and other IFilter-related definitions)
using MS.Internal;          // for Invariant

namespace MS.Internal.IO.Packaging
{
    #region XamlFilter

    /// <summary>
    /// The class that supports content extraction from XAML files for indexing purposes.
    /// Note: It would be nice to have fixed page content extractor look for flow elements in a fixed page. 
    /// This however, is not really doable: FixedPageContentExtractor is XSLT-based, not reader-based. 
    /// It cannot do anything more efficiently than what XamlFilter is currently doing. 
    /// The "flow pass" on a DOM reader for a fixed page does not entail any redundant IO or DOM building.
    /// </summary>
    internal partial class XamlFilter : IManagedFilter
    {
    #region Nested Types
        /// <summary>
        /// The following enumeration makes it easier to keep track of the filter's multi-modal behavior.
        ///
        /// Each state implements a distinct method for collecting the next content unit, as follows: 
        ///
        ///  Uninitialized         Return appropriate errors from GetChunk and GetText.
        ///  FindNextUnit          Standard mode. Return content as it is discovered in markup.
        ///  UseContentExtractor   Retrieve content from a FixedPageContentExtractor object (expected to
        ///                        perform adjacency analysis). 
        ///  FindNextFlowUnit      Look for content in markup ignoring fixed-format markup (second pass over a
        ///                        fixed page).
        ///  EndOfStream           Return appropriate errors from GetChunk and GetText.
        ///
        ///
        /// Transitions between these states are handled as follows:
        ///
        ///     state            |   transition     |     action                           |     next state
        ///    --------          |  ------------    |    --------                          |    ------------
        ///  Uninitialized       | constructor      | create an XML reader                 | FindNextUnit
        ///                      |                  |                                      |       
        ///  FindNextUnit        | end of reader    | clean up                             | EndOfStream
        ///                      |                  |                                      |       
        ///  FindNextUnit        | FixedPage tag    | create FixedPageContentExtractor,    | UseContentExtractor
        ///                      |                  | save a DOM of the FixedPage          |  
        ///                      |                  |                                      |              
        ///  UseContentExtractor | end of extractor | create sub-reader from FixedPage DOM,| FindNextFlowUnit
        ///                      |                  | save top-level reader                | 
        ///                      |                  |                                      |              
        ///  FindNextFlowUnit    | end of reader    | restore top-level reader             | FindNextUnit
        ///                      |                  |                                      |              
        ///
        /// </summary>
        internal enum FilterState
        {
            Uninitialized =1,
            FindNextUnit,
            FindNextFlowUnit,
            UseContentExtractor,
            EndOfStream
        };

        /// <summary>
        /// A single reader position on an element start may correspond to 3 distinct states depending on
        /// whether the title and/or content property in the start tag has already been processed.
        /// </summary>
        [Flags]
        internal enum AttributesToIgnore
        {
            None    =0,
            Title   =1,
            Content =2
        };
       
    #endregion Nested Types

    #region Internal Constructors


        /// <summary>
        /// Constructor. Does initialization.
        /// </summary>
        /// <param name="stream">xaml stream to filter</param>
        internal XamlFilter(Stream stream) 
        {
#if TRACE
            System.Diagnostics.Trace.TraceInformation("New Xaml filter created.");
#endif
            _lcidDictionary = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            _contextStack = new Stack(32);
            InitializeDeclaredFields();

            _xamlStream = stream;

            // Create a XAML reader (field _xamlReader) on the stream.
            CreateXmlReader();

            // Reflect load in filter's state.
            _filterState = FilterState.FindNextUnit;
        }

        /// <remarks>
        /// This function is called from the constructor. It makes the object re-initializable,
        /// which would come in handy if the XamlFilter is ever made visible to unmanaged code
        /// and Load is allowed to be called multiple times.
        /// </remarks>
        private void InitializeDeclaredFields()
        {
            // Initialize context variables.
            ClearStack();
            _filterState = FilterState.Uninitialized;

            // Initialize current ID.
            _currentChunkID = 0; 

            // Initialize the content model dictionary.
            // Note: Hashtable is not IDisposable.
            LoadContentDescriptorDictionary();

            // Misc. initializations.
            _countOfCharactersReturned = 0;
            _currentContent = null;
            _indexingContentUnit = null;
            _expectingBlockStart = true; // If text data occurred at top level, it would be a block start.
            _topLevelReader = null;
            _fixedPageContentExtractor = null;
            _fixedPageDomTree = null;
        }

    #endregion Internal Constructors

    #region Managed IFilter API

        /// <summary>
        /// Managed counterpart of IFilter.Init.
        /// </summary>
        /// <param name="grfFlags">Usage flags. Only IFILTER_INIT_CANON_PARAGRAPHS can be meaningfully
        /// honored by the XAML filter.</param>
        /// <param name="aAttributes">array of Managed FULLPROPSPEC structs to restrict responses</param>
        /// <returns>IFILTER_FLAGS_NONE, meaning the caller should not try to retrieve OLE property using
        /// IPropertyStorage on the Xaml part.</returns>
        /// <remarks>Input parameters are ignored because this filter never returns any property value.</remarks>
        public IFILTER_FLAGS Init(
            IFILTER_INIT grfFlags,    // IFILTER_INIT value     
            ManagedFullPropSpec[] aAttributes)    // restrict responses to the specified attributes
        {
            //
            // Content is filtered either if no attributes are specified,
            // or if there are attributes specified, the attribute with PSGUID_STORAGE
            // property set and PID_STG_CONTENTS property id is present.
            //

            _filterContents = true;

            if (aAttributes != null && aAttributes.Length > 0)
            {
                _filterContents = false;
            
                for (int i = 0; i < aAttributes.Length; i++)
                {
                    if (aAttributes[i].Guid == IndexingFilterMarshaler.PSGUID_STORAGE
                        && aAttributes[i].Property.PropType == PropSpecType.Id
                        && aAttributes[i].Property.PropId == (uint)MS.Internal.Interop.PID_STG.CONTENTS)
                    {
                        _filterContents = true;
                        break;
                    }
                }
            }

            // The only flag in grfFlags that makes sense to honor is IFILTER_INIT_CANON_PARAGRAPHS
            _returnCanonicalParagraphBreaks = 
                ((grfFlags & IFILTER_INIT.IFILTER_INIT_CANON_PARAGRAPHS) != 0);

            // Return zero value to indicate that the client code should not take any special steps
            // to retrieve OLE properties. This might have to change if filtering loose Xaml is supported.
            return IFILTER_FLAGS.IFILTER_FLAGS_NONE;
        }

        /// <summary>
        /// Managed counterpart of IFilter.GetChunk.
        /// </summary>
        /// <returns>
        /// Chunk descriptor.
        /// </returns>
        /// <remarks>
        /// On end of stream, this function will return null.
        /// </remarks>
        public ManagedChunk GetChunk()
        {
            if (!_filterContents)
            {
                // Contents not being filtered, no chunks to return in that case.
                _currentContent = null;

                // End of chunks.
                return null;
            }

            IndexingContentUnit     contentUnit;

            // If client code forgot to load the stream, throw appropriate exception.
            if (_xamlReader == null)
            {
                throw new COMException(SR.Get(SRID.FilterGetChunkNoStream), (int)FilterErrorCode.FILTER_E_ACCESS);
            }

            // If at end of chunks, report the condition.
            if (_filterState == FilterState.EndOfStream)
            {
                //Ensure _xamlReader has been closed
                EnsureXmlReaderIsClosed();

                // End of chunks.
                return null;
            }

            try
            {
                contentUnit = NextContentUnit();
            }
            catch (XmlException xmlException)
            {
                //Ensure _xamlReader has been closed
                EnsureXmlReaderIsClosed();

                // Return FILTER_E_UNKNOWNFORMAT for ill-formed documents.
                throw new COMException(xmlException.Message, (int)FilterErrorCode.FILTER_E_UNKNOWNFORMAT);
            }
            
            if (contentUnit == null)
            {
                // Update text information.
                _currentContent = null;

                //Ensure _xamlReader has been closed
                EnsureXmlReaderIsClosed();

                // Report end of stream by indicating end of chunks.
                return null;
            }

            // Store the text for returning in GetText.
            _currentContent = contentUnit.Text;

            // Record the fact that GetText hasn't been called on this chunk.
            _countOfCharactersReturned = 0;  

            return contentUnit;
        }

        /// <summary>
        /// Return a maximum of bufferCharacterCount characters (*not* bytes) from the current content unit.
        /// </summary>
        public String GetText(int bufferCharacterCount)
        {
            //BufferCharacterCount should be non-negative
            Debug.Assert(bufferCharacterCount >= 0);

            if (_currentContent == null)
            {
                SecurityHelper.ThrowExceptionForHR((int)FilterErrorCode.FILTER_E_NO_TEXT);
            }
            int numCharactersToReturn = _currentContent.Length - _countOfCharactersReturned;
            if (numCharactersToReturn <= 0)
            {
                SecurityHelper.ThrowExceptionForHR((int)FilterErrorCode.FILTER_E_NO_MORE_TEXT);
            }

            // Return at most bufferCharacterCount characters. The marshaler makes sure it can add a terminating
            // NULL beyond the end of the string that is returned.
            if (numCharactersToReturn > bufferCharacterCount)
            {
                numCharactersToReturn = bufferCharacterCount;
            }
            String  result = _currentContent.Substring(_countOfCharactersReturned, numCharactersToReturn);
            _countOfCharactersReturned += numCharactersToReturn;

            return result;
        }

        /// <summary>
        /// The XAML indexing filter never returns property values.
        /// </summary>
        public Object GetValue()
        {
            SecurityHelper.ThrowExceptionForHR((int)FilterErrorCode.FILTER_E_NO_VALUES);
            return null;
        }

    #endregion Managed IFilter API

    #region Internal Methods

    #if DEBUG
        internal string DumpElementTable()
        {
            ICollection keys = _xamlElementContentDescriptorDictionary.Keys;
            ICollection values = _xamlElementContentDescriptorDictionary.Values;
            int length = keys.Count;
            ElementTableKey[] keyList = new ElementTableKey[length];
            ContentDescriptor[] valueList = new ContentDescriptor[length];
            keys.CopyTo(keyList, 0);
            values.CopyTo(valueList,0);
            string result = "";
            for (int i = 0; i < length; ++i)
            {
                result += string.Format("{0}: [{1} -> {2}]\n", i, keyList[i], valueList[i]);
            }
            return result;
        }
    #endif

        ///<summary>Return the next text chunk, or null at end of stream.</summary>
        internal IndexingContentUnit NextContentUnit() 
        {
            // Loop until we are able to return some content or encounter an end of file.
            IndexingContentUnit nextContentUnit = null;
            while (nextContentUnit == null)
            {
                // If we have a content extractor delivering content units for us, use it.
                if (_filterState == FilterState.UseContentExtractor)
                {
                    Debug.Assert(_fixedPageContentExtractor != null);

                    // If we've consumed all the glyph run info, switch to a mode in which only the flow content
                    // of the fixed page just scanned will be returned.
                    if (_fixedPageContentExtractor.AtEndOfPage)
                    {
                        // Discard extractor.
                        _fixedPageContentExtractor = null;

                        // Set up reader.
                        _topLevelReader = _xamlReader;
                        _xamlReader = new XmlNodeReader(_fixedPageDomTree.DocumentElement);

                        // Transition to flow-only mode.
                        _filterState = FilterState.FindNextFlowUnit;
                    }
                    else
                    {
                        bool chunkIsInline;
                        uint lcid;

                        string chunk = _fixedPageContentExtractor.NextGlyphContent(out chunkIsInline, out lcid);
                        _expectingBlockStart = !chunkIsInline;
                        return BuildIndexingContentUnit(chunk, lcid);
                    }
                }

                if (_xamlReader.EOF)
                {
                    switch (_filterState)
                    {
                        // If in standard mode, return a null chunk to signal the end of all chunks.
                        case FilterState.FindNextUnit:
                            // A non-empty stack at this point could only be attributable to an internal error,
                            // for an early EOF would have been reported as an XML exception by the XML reader.
                            Debug.Assert(_contextStack.Count == 0);
                            _filterState = FilterState.EndOfStream;
                            return null;

                            // If processing a fixed page, revert to top-level XML reader.
                        case FilterState.FindNextFlowUnit:
                            Debug.Assert(_topLevelReader != null);
                            _xamlReader.Close();
                            _xamlReader = _topLevelReader;
                            _filterState = FilterState.FindNextUnit;
                            break;

                        default:
                            Debug.Assert(false);
                            break;
                    }
                }

                switch (_xamlReader.NodeType)
                {
                    // If current token is a text element, 
                    //    if it can be part of its parent's content, return a chunk;
                    //    else, skip.
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        nextContentUnit = HandleTextData();
                        continue;

                        // If current token is an element start, then,
                        //   if appropriate, extract chunk text from an attribute
                        //   else, record content information and recurse.
                    case XmlNodeType.Element:
                        nextContentUnit = HandleElementStart();
                        continue;

                        // On end of element, restore context data (pop, etc.) and look further.
                    case XmlNodeType.EndElement:
                        nextContentUnit = HandleElementEnd();
                        continue;
                
                        // Default action is to ignore current token and look further.
                        // Note that non-significant whitespace is handled here.
                    default:
                        _xamlReader.Read(); // Consume current token.
                        continue;
                }
            }
            return nextContentUnit;
        }

        /// <summary>
        /// Load a hash table to map qualified element names to content descriptors.
        /// </summary>
        private void LoadContentDescriptorDictionary()
        {
            // Invoke init function that is generated at build time.
            InitElementDictionary();
        } 
    #endregion Internal Methods

    #region Private Methods
        /// <summary>Ancillary function of NextContentUnit(). Create new chunk, taking _contextStack into
        /// account, and updating it if needed.</summary>
        private IndexingContentUnit BuildIndexingContentUnit(string text, uint lcid)
        {
            CHUNK_BREAKTYPE breakType = CHUNK_BREAKTYPE.CHUNK_NO_BREAK;

            // If a paragraph break is expected, reflect this in the new chunk.
            if (_expectingBlockStart)
            {
                breakType = CHUNK_BREAKTYPE.CHUNK_EOP;
                if (_returnCanonicalParagraphBreaks)
                    text = _paragraphSeparator + text;
            }
            
            if (_indexingContentUnit == null)
            {
                _indexingContentUnit = new IndexingContentUnit(text, AllocateChunkID(), breakType, _propSpec, lcid);
            }
            else
            {
                // Optimization: reuse indexing content unit.
               _indexingContentUnit.InitIndexingContentUnit(text, AllocateChunkID(), breakType, _propSpec, lcid);
            }

            // Until proven separated (by the occurrence of a block tag), right neighbors are contiguous.
            _expectingBlockStart = false;

            return _indexingContentUnit;
        }

        ///<summary>Obtain a content descriptor for a custom element not found in the dictionary.</summary>
        /// <remarks>
        /// There is currently no general way of extracting information about custom elements,
        /// so the default descriptor is systematically returned.
        /// </remarks>
        private ContentDescriptor GetContentInformationAboutCustomElement(ElementTableKey customElement)
        {
            return _defaultContentDescriptor;
        }

        ///<summary>
        /// If current token is a text element, 
        ///    assume it can be part of its parent's content and return a chunk.
        ///</summary>
        ///<remarks>
        /// Ancillary function of NextContentUnit.
        ///</remarks>
        private IndexingContentUnit HandleTextData()
        {
            ContentDescriptor topOfStack = TopOfStack();

            if (topOfStack != null)
            {
                // The descendants of elements with HasIndexableContent set to false get skipped.
                Debug.Assert(topOfStack.HasIndexableContent);

                // Return a chunk with appropriate block-break information.
                IndexingContentUnit result = BuildIndexingContentUnit(_xamlReader.Value, GetCurrentLcid());
                _xamlReader.Read(); // Move past data just processed.
                return result;
            }
            else 
            {
                // Bad Xaml (no top-level element). The Xaml filter should at some point raise an exception.
                // Just to be safe, ignore all content when in this state.
                _xamlReader.Read(); // Skip data.
                return null;
            }
        }

        ///<summary>
        /// If current token is an element start, then,
        ///   if appropriate, extract chunk text from an attribute
        ///   else, record content information and recurse.
        ///</summary>
        ///<remarks>
        /// Ancillary function of NextContentUnit.
        ///</remarks>
        private IndexingContentUnit HandleElementStart()
        {
            ElementTableKey         elementFullName = new ElementTableKey(_xamlReader.NamespaceURI, _xamlReader.LocalName);
            string                  propertyName;

            // Handle the case of a complex property (e.g. Button.Content).
            if (IsPrefixedPropertyName(elementFullName.BaseName, out propertyName))
            {
                ContentDescriptor   topOfStack = TopOfStack();

                // Handle the semantically incorrect case of a compound property occurring at the root
                // by ignoring it totally.
                if (topOfStack == null)
                {
                    SkipCurrentElement();
                    return null;
                }

                // Index the text children of property elements only if they are content or title properties.
                bool                    elementIsIndexable =
                    (    elementFullName.XmlNamespace.Equals(ElementTableKey.XamlNamespace, StringComparison.Ordinal)
                      && (    propertyName == topOfStack.ContentProp
                           || propertyName == topOfStack.TitleProp   ));
                if (!elementIsIndexable)
                {
                    // Skip element together with all its descendants.
                    SkipCurrentElement();
                    return null;
                }

                // Push descriptor, advance reader, and have caller look further.
                Push(
                     new ContentDescriptor(
                        elementIsIndexable,
                        TopOfStack().IsInline,
                        String.Empty,            // has potential text content, but no content property
                        null));                  // no title property                      
                _xamlReader.Read(); 
                return null;
            }

            // Handle fixed-format markup in a special way (because assumptions for building
            // content descriptors don't work for these and they require actions beyond what
            // is stated in content descriptors).
            // Note: The elementFullyHandled boolean is required as the nextUnit returned can 
            // be null in both cases - when element is fully handled and when its not.             
            bool elementFullyHandled;
            IndexingContentUnit nextUnit = HandleFixedFormatTag(elementFullName, out elementFullyHandled);
            if (elementFullyHandled)
                return nextUnit;
            else
            {
                // When HandleFixedFormatTag declines to handle a tag because it is not fixed-format, it
                // will return null.
                Invariant.Assert(nextUnit == null);
            }

            // Obtain a content descriptor for the current element.
            ContentDescriptor   elementDescriptor = 
                (ContentDescriptor) _xamlElementContentDescriptorDictionary[elementFullName];
            if (elementDescriptor == null)
            {
                if (elementFullName.XmlNamespace.Equals(ElementTableKey.XamlNamespace, StringComparison.Ordinal))
                {
                    elementDescriptor = _defaultContentDescriptor;
                }
                else if (elementFullName.XmlNamespace.Equals(_inDocumentCodeURI, StringComparison.Ordinal))
                {
                    elementDescriptor = _nonIndexableElementDescriptor;
                }
                else
                {
                    elementDescriptor = GetContentInformationAboutCustomElement(elementFullName);
                }
                _xamlElementContentDescriptorDictionary.Add(elementFullName, elementDescriptor);
            }

            // If the element has no indexable content, skip all its descendants.
            if (!elementDescriptor.HasIndexableContent)
            {
                SkipCurrentElement();
                return null;
            }

            // If appropriate, retrieve title from an attribute.
            string  title = null;
            if (   elementDescriptor.TitleProp != null
                && (_attributesToIgnore & AttributesToIgnore.Title) == 0 )
            {
                title = GetPropertyAsAttribute(elementDescriptor.TitleProp);
                if (title != null && title.Length > 0)
                {
                    // Leave the reader in its present state, but return the title as a block chunk,
                    // and mark this attribute as processed.
                    _attributesToIgnore |= AttributesToIgnore.Title;
                    _expectingBlockStart = true;
                    IndexingContentUnit titleContent = BuildIndexingContentUnit(title, GetCurrentLcid());
                    _expectingBlockStart = true; // Simulate a stack pop for a block element.
                    return titleContent;
                }
            }

            // If appropriate, retrieve content from an attribute.
            string  content = null;
            if (   elementDescriptor.ContentProp != null
                && (_attributesToIgnore & AttributesToIgnore.Content) == 0 )
            {
                content = GetPropertyAsAttribute(elementDescriptor.ContentProp);
                if (content != null && content.Length > 0)
                {
                    // Leave the reader in its present state, but mark the content attribute
                    // as processed.
                    _attributesToIgnore |= AttributesToIgnore.Content;

                    // Create a new chunk with appropriate break data.
                    if (!elementDescriptor.IsInline)
                    {
                        _expectingBlockStart = true;
                    }
                    IndexingContentUnit result = BuildIndexingContentUnit(content, GetCurrentLcid());
                    // Emulate a stack pop for the content attribute (which never gets pushed on the stack).
                    _expectingBlockStart = !elementDescriptor.IsInline;
                    return result;
                }
            }

            // Reset the attribute flag, since we are going to change the reader's state.
            _attributesToIgnore = AttributesToIgnore.None;

            // Handle the special case of an empty element: no descendants, but a possible paragraph break.
            if (_xamlReader.IsEmptyElement)
            {
                if (!elementDescriptor.IsInline)
                    _expectingBlockStart = true;
                // Have caller search for content past the tag.
                _xamlReader.Read();
                return null;
            }

            // Have caller look for content in descendants.
            Push(elementDescriptor);
            _xamlReader.Read(); // skip start-tag
            return null;
        }

        ///<summary>
        /// On end of element, restore context data (pop, etc.) and look further.
        ///</summary>
        ///<remarks>
        /// Ancillary function of NextContentUnit.
        ///</remarks>
        private IndexingContentUnit HandleElementEnd()
        {
            // Pop current descriptor.
            ContentDescriptor item = Pop();

            // Consume end-tag.
            _xamlReader.Read();

            return null;
        }

        /// <summary>
        /// If the current tag is one of Glyphs, FixedPage or PageContent, process it adequately
        /// and return the next content unit or null (if not supposed to return content from fixed format).
        /// Otherwise, set 'handled' to false to tell the caller we didn't do anything useful.
        /// </summary>
        private IndexingContentUnit HandleFixedFormatTag(ElementTableKey elementFullName, out bool handled)
        {
            handled = true; // Not true until we return, but this is the most convenient default.

            if (!elementFullName.XmlNamespace.Equals(ElementTableKey.FixedMarkupNamespace, StringComparison.Ordinal))
            {
                handled = false; // Let caller handle that tag.
                return null;
            }

            if (String.CompareOrdinal(elementFullName.BaseName, _glyphRunName) == 0)
            {
                // Ignore glyph runs during flow pass over a FixedPage.
                if (_filterState == FilterState.FindNextFlowUnit)
                {
                    SkipCurrentElement();
                    return null;
                }
                else
                {
                    return ProcessGlyphRun();
                }
            }

            if (String.CompareOrdinal(elementFullName.BaseName, _fixedPageName) == 0)
            {
                // Ignore FixedPage element (i.e. root element) during flow pass over a fixed page.
                if (_filterState == FilterState.FindNextFlowUnit)
                {
                    Push(_defaultContentDescriptor);
                    _xamlReader.Read();
                    return null;
                }
                else
                {
                    return ProcessFixedPage();
                }
            }

            if (String.CompareOrdinal(elementFullName.BaseName, _pageContentName) == 0)
            {
                // If the element has a Source attribute, any inlined content should be ignored.
                string sourceUri = _xamlReader.GetAttribute(_pageContentSourceAttribute);
                if (sourceUri != null)
                {
                    SkipCurrentElement();
                    return null;
                }
                else
                {
                    // Have NextContentUnit() look for content in descendants.
                    Push( _defaultContentDescriptor);
                    _xamlReader.Read();
                    return null;
                }
            }

            // No useful work was done. Report 'unhandled'.
            handled = false;
            return null;
        }

        /// <summary>
        /// Handle the presence of a glyph run in the middle of flow markup by extracting
        /// its UnicodeString attribute and considering it a separate paragraph.
        /// </summary>
        /// <remarks>
        /// The handling of glyph runs inside fixed pages is performed in ProcessFixedPage.
        /// </remarks>
        private IndexingContentUnit ProcessGlyphRun()
        {
            Debug.Assert(_xamlReader != null);

            string textContent = _xamlReader.GetAttribute(_unicodeStringAttribute);
            if (textContent == null || textContent.Length == 0)
            {
                SkipCurrentElement();
                return null;
            }
            _expectingBlockStart = true; 
            // Read Lcid at current position and advance reader to next element before returning.
            uint lcid = GetCurrentLcid();
            SkipCurrentElement(); 
            return BuildIndexingContentUnit(textContent, lcid);
        }

        /// <summary>
        /// Load FixedPage element into a DOM tree to initialize a FixedPageContentExtractor.
        /// The content extractor will then be used to incrementally return the content of the fixed page.
        /// </summary>
        private IndexingContentUnit ProcessFixedPage()
        {
            // Reader is positioned on the start-tag for a FixedPage element.
            Debug.Assert(String.CompareOrdinal(_xamlReader.LocalName, _fixedPageName) == 0);

            // A FixedPage nested in a FixedPage is invalid.
            // XmlException gets handled inside this class (see GetChunk).
            if (_filterState == FilterState.FindNextFlowUnit)
            {
                throw new XmlException(SR.Get(SRID.XamlFilterNestedFixedPage));
            }

            // Create a DOM for the current FixedPage.
            string fixedPageMarkup = _xamlReader.ReadOuterXml();
            XmlDocument fixedPageTree = new XmlDocument();
            fixedPageTree.LoadXml(fixedPageMarkup);

            // Preserve the current language ID
            if (_xamlReader.XmlLang.Length > 0)
            {
                fixedPageTree.DocumentElement.SetAttribute(_xmlLangAttribute, _xamlReader.XmlLang);
            }

            // Initialize a content extractor with this DOM tree.
            _fixedPageContentExtractor = new FixedPageContentExtractor(fixedPageTree.DocumentElement);

            // Save the DOM (to search for flow elements in it once the extractor is done)
            // and switch to extractor mode.
            _fixedPageDomTree = fixedPageTree;
            _filterState = FilterState.UseContentExtractor;

            // Have NextContentUnit look for the appropriate unit in the new mode.
            return null;
        }

        ///<summary>
        /// Create an XmlTextReader on _xamlStream with the appropriate settings.
        ///</summary>
        private void CreateXmlReader()
        {
            if (_xamlReader != null)
            {
                _xamlReader.Close();
            }
            _xamlReader = new XmlTextReader(_xamlStream);
            // Do not return pretty-pretting spacing between tags as data.
            ((XmlTextReader)_xamlReader).WhitespaceHandling = WhitespaceHandling.Significant;

            // Initialize reader state.
            _attributesToIgnore = AttributesToIgnore.None; // not in the middle of processing a start-tag
        }

        private void EnsureXmlReaderIsClosed()
        {
            if (_xamlReader != null)
            {
                _xamlReader.Close();                
            }
        }

        ///<summary>
        /// Return the LCID in scope for the current node or, if there is none,
        /// the system's default LCID.
        /// Note: XmlGlyphRunInfo.LanguageID is an internal property that also has
        /// similar logic and will default to CultureInfo.InvariantCulture.LCID
        /// CultureInfo.InvariantCulture will never be null
        ///</summary>
        private uint GetCurrentLcid()
        {
            string  languageString = GetLanguageString();

            if (languageString.Length == 0)
                return (uint)CultureInfo.InvariantCulture.LCID;
            else
                if (_lcidDictionary.ContainsKey(languageString))
                    return _lcidDictionary[languageString];
                else
                {  
                    CultureInfo cultureInfo = new CultureInfo(languageString);
                    _lcidDictionary.Add(languageString, (uint)cultureInfo.LCID);
                    return (uint)cultureInfo.LCID;
                }            
        }

        private string GetLanguageString()
        {
            string languageString = _xamlReader.XmlLang;
            if (languageString.Length == 0)
            {
                // Check whether there is a parent XAML reader.
                if (_topLevelReader != null)
                {
                    languageString = _topLevelReader.XmlLang;
                }
            }
            return languageString;
        }

        private  void SkipCurrentElement()
        {
            _xamlReader.Skip();
        }

        private bool IsPrefixedPropertyName(string name, out string propertyName)
        {
            int suffixStart = name.IndexOf('.');
            if (suffixStart == -1)
            {
                propertyName = null;
                return false;
            }
            propertyName = name.Substring(suffixStart + 1);
            return true;
        }

        /// <remarks>
        /// 0 is an illegal value, so this function never returns 0.
        /// After the counter reaches UInt32.MaxValue we assert, since such a 
        /// high number for chunks is most likely an indicator of some other 
        /// problem in the system/code.
        /// </remarks>
        private uint AllocateChunkID()
        {
            Invariant.Assert(_currentChunkID <= UInt32.MaxValue);
            
            ++_currentChunkID;

            return _currentChunkID;
        }

        /// <summary>
        /// Find an attribute named propertyName or X.propertyName.
        /// </summary>
        private string GetPropertyAsAttribute(string propertyName)
        {
            string value = _xamlReader.GetAttribute(propertyName);
            if (value != null)
            {
                return value;
            }

            bool  attributeFound = _xamlReader.MoveToFirstAttribute();
            while (attributeFound)
            {
                string attributePropertyName;

                if (   IsPrefixedPropertyName(_xamlReader.LocalName, out attributePropertyName)
                    && attributePropertyName.Equals(propertyName, StringComparison.Ordinal))
                {
                    value = _xamlReader.Value;
                    break;
                }
                
                // Advance reader.
                attributeFound = _xamlReader.MoveToNextAttribute();
            }
            // Reposition reader on owner element.
            _xamlReader.MoveToElement();
            return value;
        }
        
            

    #region Context Stack Accessors

        private ContentDescriptor TopOfStack()
        {
            return (ContentDescriptor) _contextStack.Peek();
        }

        private void Push(ContentDescriptor contentDescriptor)
        {
            if (!contentDescriptor.IsInline)
            {
                _expectingBlockStart = true;
            }
            _contextStack.Push(contentDescriptor);
        }

        private ContentDescriptor Pop()
        {
            ContentDescriptor topOfStack = (ContentDescriptor) _contextStack.Pop();

            // If we reach an end of block, we expect the next item to
            // start with a block separator.
            if (!topOfStack.IsInline)
            {
                _expectingBlockStart = true;
            }
            return topOfStack;
        }

        private void ClearStack()
        {
            _contextStack.Clear();
        }

    #endregion Context Stack Accessors

    #endregion Private Methods

    #region Private Constants

        ///<summary>XML namespace URI for in-document code.</summary>
        private const string _inDocumentCodeURI = "http://schemas.microsoft.com/winfx/2006/xaml";

        // Tag and attribute names.
        private const string _pageContentName               = "PageContent";
        private const string _glyphRunName                  = "Glyphs";
        private const string _pageContentSourceAttribute    = "Source";
        private const string _fixedPageName                 = "FixedPage";
        private const string _xmlLangAttribute              = "xml:lang";
        private const string _paragraphSeparator            = "\u2029";
        private const string _unicodeStringAttribute        = "UnicodeString";

        /// <summary>
        /// The default content descriptor has content in child nodes, no title, and block-type content.
        /// </summary>
        private readonly ContentDescriptor _defaultContentDescriptor  =
            new ContentDescriptor(true /*hasIndexableContent*/, false /*isInline*/, null, null);

        private readonly ContentDescriptor _nonIndexableElementDescriptor =
            new ContentDescriptor(false);

        // Static fields.
        private static readonly ManagedFullPropSpec _propSpec
            = new ManagedFullPropSpec(IndexingFilterMarshaler.PSGUID_STORAGE, (uint)MS.Internal.Interop.PID_STG.CONTENTS);

    #endregion Private Constants

    #region Private Fields

        // Variables initialized in constructor and InitializeDeclaredFields.
        private Stack                           _contextStack;
        private FilterState                     _filterState;
        private string                          _currentContent;
        private uint                            _currentChunkID;
        private int                             _countOfCharactersReturned;
        private IndexingContentUnit             _indexingContentUnit;
        private bool                            _expectingBlockStart;
        private XmlReader                       _topLevelReader;
        private FixedPageContentExtractor       _fixedPageContentExtractor;
        private XmlDocument                     _fixedPageDomTree;

        // Variables initialized in constructor and (potentially, if implemented some day) in IPersistFile.Load.
        private Stream                          _xamlStream;

        // Variables initialized in Init.
        private bool                            _filterContents;                 //defaults to false
        private bool                            _returnCanonicalParagraphBreaks; //defaults to false

        // Reader state variables (initialized in CreateXmlReader).
        private XmlReader                       _xamlReader;
        private AttributesToIgnore              _attributesToIgnore;

        ///<summary>Map from fully qualified element name to content location information.</summary>
        private Hashtable                       _xamlElementContentDescriptorDictionary;

        //Dictionary of Language strings and the corresponding LCID.
        private Dictionary<string, uint>        _lcidDictionary;

    #endregion Private Fields
    }   // class XamlFilter

    #endregion XamlFilter
}   // namespace MS.Internal.IO.Packaging
