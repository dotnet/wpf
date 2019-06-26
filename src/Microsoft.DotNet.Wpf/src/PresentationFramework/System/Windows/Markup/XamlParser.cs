// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Class for compiling Xaml.
*
\***************************************************************************/


using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using MS.Utility;
using System.Runtime.InteropServices;
using MS.Internal;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

#if PBTCOMPILER
namespace MS.Internal.Markup
#else

using System.Windows;

namespace System.Windows.Markup
#endif
{
    #region enums

    /// <summary>
    /// Parser modes. indicates if the Xaml should be parsed sync or async.
    /// currently public so test can set these values.
    /// </summary>
    internal enum XamlParseMode
    {
        /// <summary>
        /// Not initialized
        /// </summary>
        Uninitialized,

        /// <summary>
        /// Sync
        /// </summary>
        Synchronous,

        /// <summary>
        /// Async
        /// </summary>
        Asynchronous,
    }

    #endregion enums

    /// <summary>
    /// XamlParser class. This class is used internally
    /// </summary>
    internal class XamlParser
    {
#if PBTCOMPILER
    #region Constructors

        /// <summary>
        /// Constructor that takes a stream and creates an XmlCompatibilityReader on it.
        /// </summary>
        public XamlParser(
            ParserContext parserContext,
            BamlRecordWriter bamlWriter,
            Stream xamlStream,
            bool multipleRoots) : this(parserContext, bamlWriter,
                                        new XmlTextReader(xamlStream,
                                        multipleRoots ? XmlNodeType.Element : XmlNodeType.Document,
                                        (XmlParserContext)parserContext)                             )
        {
        }

        protected XamlParser(
            ParserContext parserContext,
            BamlRecordWriter bamlWriter,
            XmlTextReader textReader) : this(parserContext, bamlWriter)
        {

            // When the XML 1.0 specification was authored, security was not a top concern, and as a result DTDs have the
            // unfortunate capability of severe Denial of Service (DoS) attacks, typically through the use of an internal
            // entity expansion technique. In System.Xml V2.0, in order to provide protection against DTD DoS attacks there
            // is the capability of turning off DTD parsing through the use of the ProhibitDtd property.

#pragma warning disable 0618 
            // CS0618: A class member was marked with the Obsolete attribute, such that a warning 
            // will be issued when the class member is referenced. 
            textReader.ProhibitDtd = true;
#pragma warning enable 0618 

            XmlCompatibilityReader xcr = new XmlCompatibilityReader(textReader,
                                                                    new IsXmlNamespaceSupportedCallback(IsXmlNamespaceSupported),
                                                                    _predefinedNamespaces );

            TokenReader = new XamlReaderHelper(this,parserContext,xcr);
        }

        protected XamlParser(
            ParserContext parserContext,
            BamlRecordWriter bamlWriter)
        {
            _parserContext = parserContext;
            _bamlWriter = bamlWriter;
        }


        // Default constructor to aid in subclassing
        protected XamlParser()
        {
        }

#endregion Constructors

    #region PublicMethods


        /// <summary>
        /// Main method to Parse the XAML.
        /// When in synchronous mode the entire file is parsed before
        /// this method returns.
        /// In asynchronous mode at least the root tag is parsed. This
        /// is necessary for the way binders currently work.
        /// </summary>
        public void Parse()
        {
            // if parseMode hasn't been set then set it now to synchronous.
            if (XamlParseMode == XamlParseMode.Uninitialized)
            {
                XamlParseMode = XamlParseMode.Synchronous;
            }

            _Parse();
        }



        /// <summary>
        /// Main function called by the XamlCompiler on a Compile.
        /// If singleRecordMode is true the Read returns after each item read.
        /// </summary>
        /// <returns>True if more nodes to read</returns>

        // !! Review - now that have separate out into XamlReaderHelper
        // review if still need all the virtuals or the caller of the TokenReader
        // can dispatch as they feel appropriate.
        public bool ReadXaml(bool singleRecordMode)
        {
            XamlNode xamlNode = null;
            bool     cleanup = !singleRecordMode;
            bool     done = false;

            try // What do we do with Exceptions we catch on this thread?.
            {

                while (TokenReader.Read(ref xamlNode))
                {
                    SetParserAction(xamlNode);

                    if (ParserAction == ParserAction.Normal)
                    {
                        // If processing of the xaml node determines that we are done,
                        // then stop now.  This can happen if ProcessXamlNode is
                        // overridden (such as when processing styles) and the parsed
                        // block is finished, but the overall file is not.  In that
                        // case exit this parser, but leave everything intact.
                        ProcessXamlNode(xamlNode, ref cleanup, ref done);

                        if (done)
                        {
                            break;
                        }

                        // return here in single record mode since we don't want
                        // to set the parser loop to Done.
                        if (singleRecordMode)
                        {
                            return true;
                        }

                    }
                }

            }
            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e))
                {
                   throw;
                }
                else
                {
                    if (e is XamlParseException)
                    {
                        throw;
                    }
                    // If the exception was a XamlParse exception on the other
                    // side of a Reflection Invoke, then just pull up the Parse exception.
                    if (e is TargetInvocationException &&  e.InnerException is XamlParseException)
                    {
                        throw e.InnerException;
                    }

                    int lineNumber = 0;
                    int linePosition = 0;
                    string newMessage = null;

                    if (e is XmlException)
                    {
                        XmlException xmlEx = (XmlException)e;
                        lineNumber = xmlEx.LineNumber;
                        linePosition = xmlEx.LinePosition;
                        newMessage = xmlEx.Message;
                    }
                    else
                    {
                        // generic exception, If have a xamlNode then use the
                        // nodes values for the line Numbers.
                        if (null != xamlNode)
                        {
                            lineNumber = xamlNode.LineNumber;
                            linePosition = xamlNode.LinePosition;
                        }
                        newMessage = e.Message + " " + SR.Get(SRID.ParserLineAndOffset,
                                                  lineNumber.ToString(CultureInfo.CurrentCulture),
                                                  linePosition.ToString(CultureInfo.CurrentCulture));
                    }
                    XamlParseException parseException = new XamlParseException(newMessage, lineNumber, linePosition, e);
                    ParseError(parseException);

                    // Recurse on error
                    cleanup = true;
                    throw parseException;
                }
            }
            finally
            {
                // Perform cleanup only if we encountered a non-recoverable error, or
                // we're finished reading the stream (EndDocument reached in single
                // record mode, or end of stream reached in multi-record mode)
                if (cleanup)
                {
                    // Close the reader, which will close the underlying stream.
                    TokenReader.Close();
                }
            }

            return false;
        }

       /// <summary>
       /// Big switch to handle all the records.
       /// </summary>
       /// <param name="xamlNode"> Node received from TokenReader to process</param>
       /// <param name="cleanup"> True if end of stream reached and document is
       ///                        totally finished and should be closed </param>
       /// <param name="done"> True if done processing and want to exit.  Doesn't
       ///                     necessarily mean document is finished (see cleanup) </param>
       internal virtual void ProcessXamlNode(
               XamlNode xamlNode,
           ref bool     cleanup,
           ref bool     done)
       {
            switch(xamlNode.TokenType)
            {
                case XamlNodeType.DocumentStart:
                    XamlDocumentStartNode xamlDocumentStartNode =
                        (XamlDocumentStartNode) xamlNode;

                    WriteDocumentStart(xamlDocumentStartNode);

                    break;
                case XamlNodeType.DocumentEnd:
                    XamlDocumentEndNode xamlEndDocumentNode =
                        (XamlDocumentEndNode) xamlNode;
                    cleanup = true;
                    done = true;
                    WriteDocumentEnd(xamlEndDocumentNode);

                    break;

                case XamlNodeType.ElementStart:
                    XamlElementStartNode xamlElementNode =
                        (XamlElementStartNode) xamlNode;
                    WriteElementStart(xamlElementNode);

                    break;

                case XamlNodeType.ElementEnd:
                    XamlElementEndNode xamlEndElementNode =
                        (XamlElementEndNode) xamlNode;

                    WriteElementEnd(xamlEndElementNode);

                    break;
                case XamlNodeType.UnknownTagStart:
                    XamlUnknownTagStartNode xamlUnknownTagStartNode =
                        (XamlUnknownTagStartNode) xamlNode;

                    WriteUnknownTagStart(xamlUnknownTagStartNode);

                    break;
                case XamlNodeType.UnknownTagEnd:
                    XamlUnknownTagEndNode xamlUnknownTagEndNode =
                        (XamlUnknownTagEndNode) xamlNode;

                    WriteUnknownTagEnd(xamlUnknownTagEndNode);

                    break;
                case XamlNodeType.XmlnsProperty:

                    XamlXmlnsPropertyNode xamlXmlnsPropertyNode =
                        (XamlXmlnsPropertyNode) xamlNode;

                    WriteNamespacePrefix(xamlXmlnsPropertyNode);

                    break;

                case XamlNodeType.Property:

                    XamlPropertyNode xamlPropertyNode =
                        (XamlPropertyNode) xamlNode;
                    if (xamlPropertyNode.AttributeUsage == BamlAttributeUsage.RuntimeName)
                    {
                        _parserContext.XamlTypeMapper.ValidateNames(
                                            xamlPropertyNode.Value,
                                            xamlPropertyNode.LineNumber,
                                            xamlPropertyNode.LinePosition);
                    }

                    WriteProperty(xamlPropertyNode);
                    break;

                case XamlNodeType.PropertyWithExtension:

                    XamlPropertyWithExtensionNode xamlPropertyWithExtensionNode =
                        (XamlPropertyWithExtensionNode)xamlNode;
                    WritePropertyWithExtension(xamlPropertyWithExtensionNode);
                    break;

                case XamlNodeType.PropertyWithType:

                    XamlPropertyWithTypeNode xamlPropertyWithTypeNode =
                        (XamlPropertyWithTypeNode) xamlNode;
                    WritePropertyWithType(xamlPropertyWithTypeNode);
                    break;

                case XamlNodeType.UnknownAttribute:
                    XamlUnknownAttributeNode xamlUnknownAttributeNode =
                        (XamlUnknownAttributeNode) xamlNode;

                    WriteUnknownAttribute(xamlUnknownAttributeNode);

                    break;

                case XamlNodeType.PropertyComplexStart:
                    XamlPropertyComplexStartNode xamlPropertyComplexStartNode =
                        (XamlPropertyComplexStartNode) xamlNode;
                    WritePropertyComplexStart(xamlPropertyComplexStartNode);
                    break;

                case XamlNodeType.PropertyComplexEnd:
                    XamlPropertyComplexEndNode xamlPropertyComplexEndNode =
                        (XamlPropertyComplexEndNode) xamlNode;
                    WritePropertyComplexEnd(xamlPropertyComplexEndNode);
                    break;

                case XamlNodeType.LiteralContent:
                    XamlLiteralContentNode xamlLiteralContentNode =
                        (XamlLiteralContentNode) xamlNode;
                    WriteLiteralContent(xamlLiteralContentNode);
                    break;

                case XamlNodeType.Text:
                    XamlTextNode xamlTextNode =
                        (XamlTextNode) xamlNode;
                    WriteText(xamlTextNode);
                    break;

                case XamlNodeType.ClrEvent:
                    XamlClrEventNode xamlClrEventNode =
                        (XamlClrEventNode) xamlNode;
                    WriteClrEvent(xamlClrEventNode);
                    break;


                case XamlNodeType.PropertyArrayStart:
                    XamlPropertyArrayStartNode xamlPropertyArrayStartNode =
                        (XamlPropertyArrayStartNode) xamlNode;
                    WritePropertyArrayStart(xamlPropertyArrayStartNode);
                    break;

                case XamlNodeType.PropertyArrayEnd:
                    XamlPropertyArrayEndNode xamlPropertyArrayEndNode =
                        (XamlPropertyArrayEndNode) xamlNode;
                    WritePropertyArrayEnd(xamlPropertyArrayEndNode);
                    break;

                case XamlNodeType.PropertyIListStart:
                    XamlPropertyIListStartNode xamlPropertyIListStartNode =
                        (XamlPropertyIListStartNode) xamlNode;
                    WritePropertyIListStart(xamlPropertyIListStartNode);
                    break;

                case XamlNodeType.PropertyIListEnd:
                    XamlPropertyIListEndNode xamlPropertyIListEndNode =
                        (XamlPropertyIListEndNode) xamlNode;
                    WritePropertyIListEnd(xamlPropertyIListEndNode);
                    break;

                case XamlNodeType.PropertyIDictionaryStart:
                    XamlPropertyIDictionaryStartNode xamlPropertyIDictionaryStartNode =
                        (XamlPropertyIDictionaryStartNode) xamlNode;
                    WritePropertyIDictionaryStart(xamlPropertyIDictionaryStartNode);
                    break;

                case XamlNodeType.PropertyIDictionaryEnd:
                    XamlPropertyIDictionaryEndNode xamlPropertyIDictionaryEndNode =
                        (XamlPropertyIDictionaryEndNode) xamlNode;
                    WritePropertyIDictionaryEnd(xamlPropertyIDictionaryEndNode);
                    break;

                case XamlNodeType.DefTag:
                    XamlDefTagNode xamlDefTagNode =
                        (XamlDefTagNode) xamlNode;
                    WriteDefTag(xamlDefTagNode);

                    break;

                case XamlNodeType.DefKeyTypeAttribute:
                    XamlDefAttributeKeyTypeNode xamlDefAttributeKeyTypeNode =
                        (XamlDefAttributeKeyTypeNode) xamlNode;
                    WriteDefAttributeKeyType(xamlDefAttributeKeyTypeNode);
                    break;

                case XamlNodeType.DefAttribute:
                    XamlDefAttributeNode xamlDefAttributeNode =
                        (XamlDefAttributeNode) xamlNode;

                    if (xamlDefAttributeNode.AttributeUsage == BamlAttributeUsage.RuntimeName)
                    {
                        _parserContext.XamlTypeMapper.ValidateNames(
                                            xamlDefAttributeNode.Value,
                                            xamlDefAttributeNode.LineNumber,
                                            xamlDefAttributeNode.LinePosition);
                    }

                    WriteDefAttributeCore(xamlDefAttributeNode);
                    break;

                case XamlNodeType.PresentationOptionsAttribute:
                    XamlPresentationOptionsAttributeNode xamlPresentationOptionsAttributeNode =
                        (XamlPresentationOptionsAttributeNode) xamlNode;
                    WritePresentationOptionsAttribute(xamlPresentationOptionsAttributeNode);
                    break;

                case XamlNodeType.PIMapping:
                    XamlPIMappingNode xamlPIMappingNode =
                        (XamlPIMappingNode) xamlNode;
                    WritePIMapping(xamlPIMappingNode);
                    break;

                // The following tokens that are used primarily by the markup compiler
                case XamlNodeType.EndAttributes:
                    XamlEndAttributesNode xamlEndAttributesNode =
                        (XamlEndAttributesNode) xamlNode;
                    // if first tag and haven't alredy set the ParseMode
                    // set it to synchronous.
                    if (0 == xamlEndAttributesNode.Depth)
                    {
                        if (XamlParseMode == XamlParseMode.Uninitialized)
                        {
                            XamlParseMode = XamlParseMode.Synchronous;
                        }
                    }
                    WriteEndAttributes(xamlEndAttributesNode);
                    break;

                case XamlNodeType.KeyElementStart:
                    XamlKeyElementStartNode xamlKeyElementStartNode =
                        (XamlKeyElementStartNode) xamlNode;
                    WriteKeyElementStart(xamlKeyElementStartNode);
                    break;

                case XamlNodeType.KeyElementEnd:
                    XamlKeyElementEndNode xamlKeyElementEndNode =
                        (XamlKeyElementEndNode) xamlNode;
                    WriteKeyElementEnd(xamlKeyElementEndNode);
                    break;

                case XamlNodeType.ConstructorParametersEnd:
                    XamlConstructorParametersEndNode xamlConstructorParametersEndNode =
                        (XamlConstructorParametersEndNode) xamlNode;
                    WriteConstructorParametersEnd(xamlConstructorParametersEndNode);
                    break;

                case XamlNodeType.ConstructorParametersStart:
                    XamlConstructorParametersStartNode xamlConstructorParametersStartNode =
                        (XamlConstructorParametersStartNode) xamlNode;
                    WriteConstructorParametersStart(xamlConstructorParametersStartNode);
                    break;

                case XamlNodeType.ContentProperty:
                    XamlContentPropertyNode xamlContentPropertyNode =
                        (XamlContentPropertyNode)xamlNode;
                    WriteContentProperty(xamlContentPropertyNode);
                    break;

                case XamlNodeType.ConstructorParameterType:
                    XamlConstructorParameterTypeNode xamlConstructorParameterTypeNode =
                        (XamlConstructorParameterTypeNode)xamlNode;
                    WriteConstructorParameterType(xamlConstructorParameterTypeNode);
                    break;
                default:
                    Debug.Assert(false,"Unknown Xaml Token.");
                    break;
            }

       }

#endregion // PublicMethods

    #region Virtuals

        /// <summary>
        /// Called when parsing begins
        /// </summary>
        public virtual void WriteDocumentStart(XamlDocumentStartNode XamlDocumentStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteDocumentStart(XamlDocumentStartNode);
            }
        }

        /// <summary>
        /// Called when parsing ends
        /// </summary>
        public virtual void WriteDocumentEnd(XamlDocumentEndNode xamlEndDocumentNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteDocumentEnd(xamlEndDocumentNode);
            }
        }

        /// <summary>
        /// Write Start of an Element, which is a tag of the form /<Classname />
        /// </summary>
        public virtual void WriteElementStart(XamlElementStartNode xamlElementStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteElementStart(xamlElementStartNode);
            }
        }

        /// <summary>
        /// Write start of an unknown tag
        /// </summary>
        public virtual void WriteUnknownTagStart(XamlUnknownTagStartNode xamlUnknownTagStartNode)
        {
            // The default action for unknown tags is throw an exception.
            ThrowException(SRID.ParserUnknownTag ,
                        xamlUnknownTagStartNode.Value,
                        xamlUnknownTagStartNode.XmlNamespace,
                        xamlUnknownTagStartNode.LineNumber,
                        xamlUnknownTagStartNode.LinePosition);
        }

        /// <summary>
        /// Write end of an unknown tag
        /// </summary>
        public virtual void WriteUnknownTagEnd(XamlUnknownTagEndNode xamlUnknownTagEndNode)
        {
            // The default action for unknown tags is throw an exception.  This should never
            // get here unless there is a coding error, since it would first hit
            // WriteUnknownTagStart
            ThrowException(SRID.ParserUnknownTag ,
                        "???",
                        xamlUnknownTagEndNode.LineNumber,
                        xamlUnknownTagEndNode.LinePosition);
        }

        /// <summary>
        /// Write End Element
        /// </summary>
        public virtual void WriteElementEnd(XamlElementEndNode xamlElementEndNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteElementEnd(xamlElementEndNode);
            }
        }

        /// <summary>
        /// Called when parsing hits literal content.
        /// </summary>
        public virtual void WriteLiteralContent(XamlLiteralContentNode xamlLiteralContentNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteLiteralContent(xamlLiteralContentNode);
            }
        }

        /// <summary>
        /// Write Start Complex Property, where the tag is of the
        /// form /<Classname.PropertyName/>
        /// </summary>
        public virtual void WritePropertyComplexStart(
            XamlPropertyComplexStartNode xamlPropertyComplexStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyComplexStart(xamlPropertyComplexStartNode);
            }
        }



        /// <summary>
        /// Write End Complex Property
        /// </summary>
        public virtual void WritePropertyComplexEnd(
            XamlPropertyComplexEndNode xamlPropertyComplexEndNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyComplexEnd(xamlPropertyComplexEndNode);
            }
        }

        /// <summary>
        /// Write Start element for a dictionary key section.
        /// </summary>
        public virtual void WriteKeyElementStart(
            XamlElementStartNode xamlKeyElementStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteKeyElementStart(xamlKeyElementStartNode);
            }
        }

        /// <summary>
        /// Write End element for a dictionary key section
        /// </summary>
        public virtual void WriteKeyElementEnd(
            XamlElementEndNode xamlKeyElementEndNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteKeyElementEnd(xamlKeyElementEndNode);
            }
        }

        /// <summary>
        /// Write unknown attribute
        /// </summary>
        public virtual void WriteUnknownAttribute(XamlUnknownAttributeNode xamlUnknownAttributeNode)
        {
            // The default action for unknown attributes is throw an exception.
            ThrowException(SRID.ParserUnknownAttribute ,
                        xamlUnknownAttributeNode.Name,
                        xamlUnknownAttributeNode.XmlNamespace,
                        xamlUnknownAttributeNode.LineNumber,
                        xamlUnknownAttributeNode.LinePosition);
        }

        /// <summary>
        /// Write a Property, which has the form in markup of property="value".
        /// </summary>
        /// <remarks>
        /// Note that for DependencyProperties, the assemblyName, TypeFullName, PropIdName
        /// refer to DependencyProperty field
        /// that the property was found on. This may be different from the ownerType
        /// of the propId if the property was registered as an alias so if the
        /// callback is persisting we want to persist the information that the propId
        /// was found on in case the alias is a private or internal field.
        /// </remarks>
        public virtual void WriteProperty(XamlPropertyNode xamlPropertyNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteProperty(xamlPropertyNode);
            }
        }

        internal void WriteBaseProperty(XamlPropertyNode xamlPropertyNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.BaseWriteProperty(xamlPropertyNode);
            }
        }

        /// <summary>
        /// Write a Property, which has the form in markup of property="value".
        /// </summary>
        /// <remarks>
        /// Note that for DependencyProperties, the assemblyName, TypeFullName, PropIdName
        /// refer to DependencyProperty field
        /// that the property was found on. This may be different from the ownerType
        /// of the propId if the property was registered as an alias so if the
        /// callback is persisting we want to persist the information that the propId
        /// was found on in case the alias is a private or internal field.
        /// </remarks>
        public virtual void WritePropertyWithType(XamlPropertyWithTypeNode xamlPropertyNode)
        {
            if (BamlRecordWriter != null)
            {
                if (xamlPropertyNode.ValueElementType == null)
                {
                    ThrowException(SRID.ParserNoType,
                                   xamlPropertyNode.ValueTypeFullName,
                                   xamlPropertyNode.LineNumber,
                                   xamlPropertyNode.LinePosition);
                }

                BamlRecordWriter.WritePropertyWithType(xamlPropertyNode);
            }
        }

        public virtual void WritePropertyWithExtension(XamlPropertyWithExtensionNode xamlPropertyWithExtensionNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyWithExtension(xamlPropertyWithExtensionNode);
            }
        }


        /// <summary>
        /// Write out Text, currently don't keep track if originally CData or Text
        /// </summary>
        public virtual void WriteText(XamlTextNode xamlTextNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteText(xamlTextNode);
            }
        }


        /// <summary>
        /// Write a new namespacePrefix to NamespaceURI map
        /// </summary>
        public virtual void WriteNamespacePrefix(XamlXmlnsPropertyNode xamlXmlnsPropertyNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteNamespacePrefix(xamlXmlnsPropertyNode);
            }
        }


        /// <summary>
        /// Xml - Clr namespace mapping
        /// </summary>
        public virtual void WritePIMapping(XamlPIMappingNode xamlPIMappingNode)
        {
            // The only case when the assembly name can be empty is when there is a local assembly
            // specified in the Mapping PI, but the compiler extension should have resolved it by
            // now. So if we are still seeing an empty string here that means we in the pure xaml
            // parsing scenario and should throw.
            if (xamlPIMappingNode.AssemblyName.Length == 0)
            {
                ThrowException(SRID.ParserMapPIMissingKey, xamlPIMappingNode.LineNumber, xamlPIMappingNode.LinePosition);
            }

            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePIMapping(xamlPIMappingNode);
            }
        }

        /// <summary>
        /// Write out the Clr event.
        /// </summary>
        public virtual void WriteClrEvent(XamlClrEventNode xamlClrEventNode)
        {
            // Parser currently doesn't support hooking up Events directly from
            // XAML so throw an exception. In the compile case this method
            // is overriden.

            if (null == ParserHooks)
            {
                ThrowException(SRID.ParserNoEvents,
                    xamlClrEventNode.LineNumber,
                    xamlClrEventNode.LinePosition);
            }
            else if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteClrEvent(xamlClrEventNode);
            }
        }

        /// <summary>
        /// Write Property Array Start
        /// </summary>
        public virtual void WritePropertyArrayStart(XamlPropertyArrayStartNode xamlPropertyArrayStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyArrayStart(xamlPropertyArrayStartNode);
            }
        }


        /// <summary>
        /// Write Property Array End
        /// </summary>
        public virtual void WritePropertyArrayEnd(XamlPropertyArrayEndNode xamlPropertyArrayEndNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyArrayEnd(xamlPropertyArrayEndNode);
            }
        }


        /// <summary>
        /// Write Property IList Start
        /// </summary>
        public virtual void WritePropertyIListStart(XamlPropertyIListStartNode xamlPropertyIListStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyIListStart(xamlPropertyIListStartNode);
            }
        }


        /// <summary>
        /// Write Property IList End
        /// </summary>
        public virtual void WritePropertyIListEnd(XamlPropertyIListEndNode xamlPropertyIListEndNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyIListEnd(xamlPropertyIListEndNode);
            }
        }

        /// <summary>
        /// Write Property IDictionary Start
        /// </summary>
        public virtual void WritePropertyIDictionaryStart(XamlPropertyIDictionaryStartNode xamlPropertyIDictionaryStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyIDictionaryStart(xamlPropertyIDictionaryStartNode);
            }
        }


        /// <summary>
        /// Write Property IDictionary End
        /// </summary>
        public virtual void WritePropertyIDictionaryEnd(XamlPropertyIDictionaryEndNode xamlPropertyIDictionaryEndNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePropertyIDictionaryEnd(xamlPropertyIDictionaryEndNode);
            }
        }


        /// <summary>
        /// WriteEndAttributes occurs after the last attribute (property, complex property or
        /// def record) is written.  Note that if there are none of the above, then WriteEndAttributes
        /// is not called for a normal start tag.
        /// </summary>
        public virtual void WriteEndAttributes(XamlEndAttributesNode xamlEndAttributesNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteEndAttributes(xamlEndAttributesNode);
            }
        }

        /// <summary>
        /// WriteDefTag occurs when a x:Whatever tag is encountered.
        /// Superclasses must interprete this since the base class doesn't understand
        /// any of them.
        /// </summary>
        public virtual void WriteDefTag(XamlDefTagNode xamlDefTagNode)
        {
            ThrowException(SRID.ParserDefTag,
                        xamlDefTagNode.Value,
                        xamlDefTagNode.LineNumber,
                        xamlDefTagNode.LinePosition);
        }

        /// <summary>
        /// Write out a key to a dictionary that has been resolved at compile or parse
        /// time to a Type object.
        /// </summary>
        public virtual void WriteDefAttributeKeyType(XamlDefAttributeKeyTypeNode xamlDefNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteDefAttributeKeyType(xamlDefNode);
            }
        }

        /// <summary>
        /// WriteDefAttribute when attributes of the form x:Whatever are encountered
        /// </summary>
        public virtual void WriteDefAttribute(XamlDefAttributeNode xamlDefAttributeNode)
        {
            string attributeValue = xamlDefAttributeNode.Value;

            // There are several known def attributes, and these are checked for
            // correctness by running the known type converters.
            switch(xamlDefAttributeNode.Name)
            {
               case XamlReaderHelper.DefinitionSynchronousMode:
                   if (BamlRecordWriter != null)
                   {
                       if (xamlDefAttributeNode.Value == "Async")
                       {
                           ThrowException(SRID.ParserNoBamlAsync, "Async",
                                      xamlDefAttributeNode.LineNumber,
                                      xamlDefAttributeNode.LinePosition);
                       }
                   }
                   break;

               case XamlReaderHelper.DefinitionAsyncRecords:
                    // Update the AsyncRecords and don't store this as a def attribute
                       ThrowException(SRID.ParserNoBamlAsync, xamlDefAttributeNode.Name,
                                      xamlDefAttributeNode.LineNumber,
                                      xamlDefAttributeNode.LinePosition);
                   break;

                case XamlReaderHelper.DefinitionShared:
                    Boolean.Parse(attributeValue);   // For validation only.
                    if (BamlRecordWriter != null)
                    {
                        BamlRecordWriter.WriteDefAttribute(xamlDefAttributeNode);
                    }
                    break;

                case XamlReaderHelper.DefinitionUid:
                case XamlReaderHelper.DefinitionRuntimeName:
                    //Error if x:Uid or x:Name are markup extensions
                    if (MarkupExtensionParser.LooksLikeAMarkupExtension(attributeValue))
                    {
                        string message = SR.Get(SRID.ParserBadUidOrNameME, attributeValue);
                        message += " ";
                        message += SR.Get(SRID.ParserLineAndOffset,
                                    xamlDefAttributeNode.LineNumber.ToString(CultureInfo.CurrentCulture),
                                    xamlDefAttributeNode.LinePosition.ToString(CultureInfo.CurrentCulture));

                        XamlParseException parseException = new XamlParseException(message,
                                xamlDefAttributeNode.LineNumber, xamlDefAttributeNode.LinePosition);

                        throw parseException;
                    }

                    if (BamlRecordWriter != null)
                    {
                        BamlRecordWriter.WriteDefAttribute(xamlDefAttributeNode);
                    }
                    break;

                case XamlReaderHelper.DefinitionName:
                    if (BamlRecordWriter != null)
                    {
                        BamlRecordWriter.WriteDefAttribute(xamlDefAttributeNode);
                    }
                    break;

                default:
                    string errorID;
                        errorID = SRID.ParserUnknownDefAttribute;
                        ThrowException(errorID,
                                   xamlDefAttributeNode.Name,
                                   xamlDefAttributeNode.LineNumber,
                                   xamlDefAttributeNode.LinePosition);
                    break;
            }
        }

        /// <summary>
        /// Write attributes of the form PresentationOptions:Whatever to Baml
        /// </summary>
        public virtual void WritePresentationOptionsAttribute(XamlPresentationOptionsAttributeNode xamlPresentationOptionsAttributeNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WritePresentationOptionsAttribute(xamlPresentationOptionsAttributeNode);
            }
        }

        /// <summary>
        /// Write the start of a constructor parameter section
        /// </summary>
        public virtual void WriteConstructorParametersStart(XamlConstructorParametersStartNode xamlConstructorParametersStartNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteConstructorParametersStart(xamlConstructorParametersStartNode);
            }
        }

        public virtual void WriteContentProperty(XamlContentPropertyNode xamlContentPropertyNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteContentProperty(xamlContentPropertyNode);
            }
        }

        /// <summary>
        /// Write the constructor parameter record where the parameter is a compile time
        /// resolved Type object.
        /// </summary>
        public virtual void WriteConstructorParameterType(
             XamlConstructorParameterTypeNode xamlConstructorParameterTypeNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteConstructorParameterType(xamlConstructorParameterTypeNode);
            }
        }

        /// <summary>
        /// Write the end of a constructor parameter section
        /// </summary>
        public virtual void WriteConstructorParametersEnd(XamlConstructorParametersEndNode xamlConstructorParametersEndNode)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteConstructorParametersEnd(xamlConstructorParametersEndNode);
            }
        }

        /// <summary>
        /// Can be used by ac to return the type of the element if a class attribute is
        /// present. If want to override default Type because of class= attribute can do so here.
        /// Should leave the XmlReader positioned at the Element so if read attributes
        /// to determine type need to call XmlReader.MoveToElement()
        /// </summary>
        public virtual bool GetElementType(
                XmlReader  reader,
                string     localName,
                string     namespaceUri,
            ref string     assemblyName,
            ref string     typeFullName,
            ref Type       baseType,
            ref Type       serializerType)
        {
            bool result = false;

            assemblyName   = string.Empty;
            typeFullName   = string.Empty;
            serializerType = null;
            baseType       = null;

            // if no namespaceURI or local name don't bother
            if (null == namespaceUri || null == localName)
            {
                return false;
            }

            TypeAndSerializer typeAndSerializer =
                XamlTypeMapper.GetTypeAndSerializer(namespaceUri, localName, null);

            if (typeAndSerializer != null &&
                typeAndSerializer.ObjectType != null)
            {
                serializerType = typeAndSerializer.SerializerType;
                baseType = typeAndSerializer.ObjectType;
                typeFullName = baseType.FullName;
                assemblyName = baseType.Assembly.FullName;
                result = true;

                Debug.Assert(null != assemblyName, "assembly name returned from GetBaseElement is null");
                Debug.Assert(null != typeFullName, "Type name returned from GetBaseElement is null");
            }

            return result;
        }


#endregion Virtuals


        /// <summary>
        /// Write a Connector Id for compiler.
        /// </summary>
        protected internal void WriteConnectionId(Int32 connectionId)
        {
            if (BamlRecordWriter != null)
            {
                BamlRecordWriter.WriteConnectionId(connectionId);
            }
        }

        /// <summary>
        /// A def attribute was encountered.  Perform synchonous mode checking
        /// prior to calling the virtual that may be overridden.
        /// </summary>
        void WriteDefAttributeCore(XamlDefAttributeNode xamlDefAttributeNode)
        {

            string attributeValue = xamlDefAttributeNode.Value;

            switch(xamlDefAttributeNode.Name)
            {
                case  XamlReaderHelper.DefinitionSynchronousMode:
                    XamlParseMode documentParseMode = XamlParseMode.Synchronous;

                    if (attributeValue.Equals("Async"))
                    {
                        documentParseMode = XamlParseMode.Asynchronous;
                    }
                    else if (attributeValue.Equals("Sync"))
                    {
                        documentParseMode = XamlParseMode.Synchronous;
                    }
                    else
                    {
                        ThrowException(SRID.ParserBadSyncMode,
                               xamlDefAttributeNode.LineNumber,
                               xamlDefAttributeNode.LinePosition );
                    }

                    // if we haven't initialized the the parseMode yet set it
                    if (XamlParseMode == XamlParseMode.Uninitialized)
                    {
                        XamlParseMode = documentParseMode;

                    }
                    break;
                default:
                    break;
            }

            WriteDefAttribute(xamlDefAttributeNode);
        }

    #region Methods

        // virtuals to override the default implementation. used by the compiler
        // for internal virtuals review why not public as the others?

        // Used when an exception is thrown.The default action is to shutdown the parser
        // and throw the exception.
        internal virtual void ParseError(XamlParseException e)
        {
        }

        /// <summary>
        /// Called when the parse was cancelled by the user.
        /// </summary>
        internal virtual void  ParseCancelled()
        {
        }

        /// <summary>
        ///  called when the parse has been completed successfully.
        /// </summary>
        internal virtual void ParseCompleted()
        {
        }



        /// <summary>
        ///  Default parsing is to synchronously read all the xaml nodes
        ///  until done.
        /// </summary>
        internal virtual void _Parse()
        {

            ReadXaml(false /* want to parse the entire thing */);

        }


        /// <summary>
        /// If there are ParserHooks, call it with the current xamlNode and perform
        /// as directed by the callback.
        /// </summary>
        private void SetParserAction(XamlNode xamlNode)
        {
            // if no ParserHooks then process as normal
            if (null == ParserHooks)
            {
                ParserAction = ParserAction.Normal;
                return;
            }

            // if ParserHooks want to skip the current node and its children,
            // check for end of scope where it asked to be skipped.
            if (ParserAction == ParserAction.Skip)
            {
                if (xamlNode.Depth <= SkipActionDepthCount &&
                    xamlNode.TokenType == SkipActionToken)
                {
                    // We found the end token at the correct depth.  Reset the depth count
                    // so that in the next call we won't skip calling the ParserHooks.  Don't
                    // reset the ParserAction since we want to skip this end token.
                    SkipActionDepthCount = -1;
                    SkipActionToken = XamlNodeType.Unknown;
                    return;
                }
                else if (SkipActionDepthCount >= 0)
                {
                    return;
                }
            }

            // If we get to here, the ParserHooks want to be called.
            ParserAction = ParserHooks.LoadNode(xamlNode);

            // if the ParserHooks want to skip the current node and its children then
            // set the callback depth so that we'll know when to start processing again.
            if (ParserAction == ParserAction.Skip)
            {
                // For tokens with no scope (eg = attributes), don't set the depth so
                // that we will only skip once
                Debug.Assert(SkipActionDepthCount == -1);
                int tokenIndex = ((IList)XamlNode.ScopeStartTokens).IndexOf(xamlNode.TokenType);
                if (tokenIndex != -1)
                {
                    SkipActionDepthCount = xamlNode.Depth;
                    SkipActionToken = XamlNode.ScopeEndTokens[tokenIndex];
                }
            }
        }



        // Return true if the passed namespace is known, meaning that it maps
        // to a set of assemblies and clr namespaces
        internal bool IsXmlNamespaceSupported(string xmlNamespace, out string newXmlNamespace)
        {
            newXmlNamespace = null;

            if (xmlNamespace.StartsWith(XamlReaderHelper.MappingProtocol, StringComparison.Ordinal))
            {
                return true;
            }
            else if (xmlNamespace == XamlReaderHelper.PresentationOptionsNamespaceURI)
            {
                // PresentationOptions is expected to be marked as 'ignorable' in most Xaml
                // so that other Xaml parsers don't have to interpret it, but this parser
                // does handle it to support it's Freeze attribute.
                return true;
            }
            else
            {
                return XamlTypeMapper.IsXmlNamespaceKnown(xmlNamespace, out newXmlNamespace) ||
                    TokenReader.IsXmlDataIsland();
            }
        }
#endregion Methods

    #region Properties

        /// <summary>
        /// TokenReader that is being used.
        /// </summary>
        internal XamlReaderHelper TokenReader
        {
            get { return _xamlTokenReader; }
            set { _xamlTokenReader = value; }
        }

        /// <summary>
        ///  ParserHooks implementation that any parse time callbacks
        ///  should be called on.
        /// </summary>
        internal  ParserHooks ParserHooks
        {
            get { return _parserHooks; }
            set { _parserHooks = value; }
        }

        // Set the depth count for how deep we are within
        // a ParserAction.Skip reference.
        int SkipActionDepthCount
        {
            get { return _skipActionDepthCount; }
            set {  _skipActionDepthCount = value; }
        }

        // Set and get the token to watch for when skipping a
        // section of a xaml file
        XamlNodeType SkipActionToken
        {
            get { return _skipActionToken; }
            set {  _skipActionToken = value; }
        }

        // set the operation mode of the parser as determined
        // by attached ParserHooks
        ParserAction ParserAction
        {
            get { return _parserAction; }
            set {  _parserAction = value; }
        }

        /// <summary>
        /// Instance of the XamlTypeMapper
        /// </summary>
        internal XamlTypeMapper XamlTypeMapper
        {
            get { return _parserContext.XamlTypeMapper; }
        }

        /// <summary>
        /// Instance of the BamlMapTable
        /// </summary>
        internal BamlMapTable MapTable
        {
            get { return _parserContext.MapTable; }
        }

        /// <summary>
        /// BamlRecordWriter being used by the Parser
        /// </summary>
        public BamlRecordWriter BamlRecordWriter
        {
            get { return _bamlWriter; }
            set
            {
                Debug.Assert(null == _bamlWriter || null == value, "XamlParser already had a bamlWriter");
                _bamlWriter = value;
            }
        }

        /// <summary>
        ///  ParseMode the Parser is in.
        /// </summary>
        internal XamlParseMode XamlParseMode
        {
            get { return _xamlParseMode; }
            set { _xamlParseMode = value; }
        }

        /// <summary>
        ///  Parser context
        /// </summary>
        internal ParserContext ParserContext
        {
            get { return _parserContext; }
            set { _parserContext = value; }
        }

        internal virtual bool CanResolveLocalAssemblies()
        {
            return false;
        }

        // Used to determine if strict or loose parsing rules should be enforced.  The TokenReader
        // does some validations that are difficult for the XamlParser to do and in strict parsing
        // mode the TokenReader should throw exceptions of standard Xaml rules are violated.
        internal virtual bool StrictParsing
        {
            get { return true; }
        }


#endregion Properties

    #region Data




        // private Data

        XamlReaderHelper             _xamlTokenReader;

        ParserContext                _parserContext; 

        XamlParseMode                _xamlParseMode;
        BamlRecordWriter             _bamlWriter;

        // ParserHooks related
        ParserHooks                  _parserHooks;
        ParserAction                 _parserAction = ParserAction.Normal;
        int                          _skipActionDepthCount = -1; // skip mode depth count.
        XamlNodeType                 _skipActionToken = XamlNodeType.Unknown;

        static private string []     _predefinedNamespaces = new string [3] {
            XamlReaderHelper.DefinitionNamespaceURI,
            XamlReaderHelper.DefaultNamespaceURI,
            XamlReaderHelper.DefinitionMetroNamespaceURI
        };
    #endregion Data

#endif

        // helper method called to throw an exception.
        internal static void ThrowException(string id, int lineNumber, int linePosition)
        {
            string message = SR.Get(id);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        // helper method called to throw an exception.
        internal static void ThrowException(string id, string value, int lineNumber, int linePosition)
        {
            string message = SR.Get(id, value);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        // helper method called to throw an exception.
        internal static void ThrowException(string id, string value1, string value2, int lineNumber, int linePosition)
        {
            string message = SR.Get(id, value1, value2);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        internal static void ThrowException(string id, string value1, string value2, string value3, int lineNumber, int linePosition)
        {
            string message = SR.Get(id, value1, value2, value3);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        internal static void ThrowException(string id, string value1, string value2, string value3, string value4, int lineNumber, int linePosition)
        {
            string message = SR.Get(id, value1, value2, value3, value4);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        private static void ThrowExceptionWithLine(string message, int lineNumber, int linePosition)
        {
            message += " ";
            message += SR.Get(SRID.ParserLineAndOffset,
                                    lineNumber.ToString(CultureInfo.CurrentCulture),
                                    linePosition.ToString(CultureInfo.CurrentCulture));

            XamlParseException parseException = new XamlParseException(message,
                lineNumber, linePosition);


            throw parseException;
        }
    }
}
