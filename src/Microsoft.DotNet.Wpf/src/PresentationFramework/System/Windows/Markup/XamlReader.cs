// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description:
//   base Parser class that parses XML markup into an Avalon Element Tree
//

using System;
using System.Xml;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using MS.Utility;
using System.Security;
using System.Text;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Windows.Markup.Primitives;
using MS.Internal;

using MS.Internal.IO.Packaging;
using System.Windows.Baml2006;
using System.Threading;
using System.Windows.Threading;
using System.Xaml;
using System.Xaml.Permissions;
using System.Windows.Navigation;
using MS.Internal.Xaml.Context;

namespace System.Windows.Markup
{
    /// <summary>
    /// Parsing class used to create an Windows Presentation Platform Tree
    /// </summary>
    public class XamlReader
    {
        #region Public Methods

        /// <summary>
        /// Reads XAML using the passed xamlText string, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="xamlText">XAML text as a string</param>
        /// <returns>object root generated after xaml is parsed</returns>
        public static object Parse(string xamlText)
        {
            return Parse(xamlText, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Reads XAML using the passed xamlText string, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="xamlText">XAML text as a string</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xaml is parsed</returns>
        public static object Parse(string xamlText, bool useRestrictiveXamlReader)
        {
            StringReader stringReader = new StringReader(xamlText);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            return Load(xmlReader, useRestrictiveXamlReader);
        }

        /// <summary>
        /// Reads XAML using the passed xamlText, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="xamlText">XAML text as a string</param>
        /// <param name="parserContext">parser context</param>
        /// <returns>object root generated after xaml is parsed</returns>
        public static object Parse(string xamlText, ParserContext parserContext)
        {
            return Parse(xamlText, parserContext, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Reads XAML using the passed xamlText, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="xamlText">XAML text as a string</param>
        /// <param name="parserContext">parser context</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xaml is parsed</returns>
        public static object Parse(string xamlText, ParserContext parserContext, bool useRestrictiveXamlReader)
        {
            Stream xamlStream = new MemoryStream(UTF8Encoding.Default.GetBytes(xamlText));
            return Load(xamlStream, parserContext, useRestrictiveXamlReader);
        }

        /// <summary>
        /// Reads XAML from the passed stream,building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="stream">input as stream</param>
        /// <returns>object root generated after xml parsed</returns>
        public static object Load(Stream stream)
        {
            return Load(stream, null, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Reads XAML from the passed stream,building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="stream">input as stream</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xml parsed</returns>
        public static object Load(Stream stream, bool useRestrictiveXamlReader)
        {
            return Load(stream, null, useRestrictiveXamlReader);
        }

        /// <summary>
        /// Reads XAML using the passed XmlReader, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="reader">Reader of xml content.</param>
        /// <returns>object root generated after xml parsed</returns>
        public static object Load(XmlReader reader)
        {
            return Load(reader, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Reads XAML using the passed XmlReader, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="reader">Reader of xml content.</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xml parsed</returns>
        public static object Load(XmlReader reader, bool useRestrictiveXamlReader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return Load(reader, null, XamlParseMode.Synchronous, useRestrictiveXamlReader);
        }

        /// <summary>
        /// Reads XAML from the passed stream, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="stream">input as stream</param>
        /// <param name="parserContext">parser context</param>
        /// <returns>object root generated after xml parsed</returns>
        public static object Load(Stream stream, ParserContext parserContext)
        {
            return Load(stream, parserContext, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Reads XAML from the passed stream, building an object tree and returning the
        /// root of that tree.
        /// </summary>
        /// <param name="stream">input as stream</param>
        /// <param name="parserContext">parser context</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xml parsed</returns>
        public static object Load(Stream stream, ParserContext parserContext, bool useRestrictiveXamlReader )
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (parserContext == null)
            {
                parserContext = new ParserContext();
            }

            XmlReader reader = XmlReader.Create(stream, null, parserContext);
            object tree = Load(reader, parserContext, XamlParseMode.Synchronous, useRestrictiveXamlReader);
            stream.Close();
            return tree;
        }

        /// <summary>
        /// Loads XAML from the given stream, building an object tree.
        /// The load operation will be done asynchronously if the
        /// markup specifies x:SynchronousMode="async".
        /// </summary>
        /// <param name="stream">stream for the xml content</param>
        /// <returns>object root generated after xml parsed</returns>
        /// <remarks>
        /// Notice that this is an instance method
        /// </remarks>
        public object LoadAsync(Stream stream)
        {
            return LoadAsync(stream, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Loads XAML from the given stream, building an object tree.
        /// The load operation will be done asynchronously if the
        /// markup specifies x:SynchronousMode="async".
        /// </summary>
        /// <param name="stream">stream for the xml content</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xml parsed</returns>
        /// <remarks>
        /// Notice that this is an instance method
        /// </remarks>
        public object LoadAsync(Stream stream, bool useRestrictiveXamlReader)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            _stream = stream;

            if (_objectWriter != null)
            {
                // A XamlReader instance cannot be shared across two load operations
                throw new InvalidOperationException(SR.Get(SRID.ParserCannotReuseXamlReader));
            }

            return LoadAsync(stream, null, useRestrictiveXamlReader);
        }

        /// <summary>
        /// Reads XAML using the given XmlReader, building an object tree.
        /// The load operation will be done asynchronously if the markup
        /// specifies x:SynchronousMode="async".
        /// </summary>
        /// <param name="reader">Reader for xml content.</param>
        /// <returns>object root generated after xml parsed</returns>
        /// <remarks>
        /// Notice that this is an instance method
        /// </remarks>
        public object LoadAsync(XmlReader reader)
        {


            return LoadAsync(reader, null, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Reads XAML using the given XmlReader, building an object tree.
        /// The load operation will be done asynchronously if the markup
        /// specifies x:SynchronousMode="async".
        /// </summary>
        /// <param name="reader">Reader for xml content.</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xml parsed</returns>
        /// <remarks>
        /// Notice that this is an instance method
        /// </remarks>
        public object LoadAsync(XmlReader reader, bool useRestrictiveXamlReader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            return LoadAsync(reader, null, useRestrictiveXamlReader);
        }

        /// <summary>
        /// Loads XAML from the given stream, building an object tree.
        /// The load operation will be done asynchronously if the
        /// markup specifies x:SynchronousMode="async".
        /// </summary>
        /// <param name="stream">stream for the xml content</param>
        /// <param name="parserContext">parser context</param>
        /// <param name="useRestrictiveXamlReader">boolean flag to restrict xaml loading</param>
        /// <returns>object root generated after xml parsed</returns>
        /// <remarks>
        /// Notice that this is an instance method
        /// </remarks>
        public object LoadAsync(Stream stream, ParserContext parserContext)
        {
            return LoadAsync(stream, parserContext, useRestrictiveXamlReader:false);
        }

        /// <summary>
        /// Loads XAML from the given stream, building an object tree.
        /// The load operation will be done asynchronously if the
        /// markup specifies x:SynchronousMode="async".
        /// </summary>
        /// <param name="stream">stream for the xml content</param>
        /// <param name="parserContext">parser context</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xml parsed</returns>
        /// <remarks>
        /// Notice that this is an instance method
        /// </remarks>
        public object LoadAsync(Stream stream, ParserContext parserContext , bool useRestrictiveXamlReader)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            _stream = stream;

            if (_objectWriter != null)
            {
                // A XamlReader instance cannot be shared across two load operations
                throw new InvalidOperationException(SR.Get(SRID.ParserCannotReuseXamlReader));
            }

            if (parserContext == null)
            {
                parserContext = new ParserContext();
            }

            XmlTextReader reader = new XmlTextReader(stream, XmlNodeType.Document, parserContext);
            reader.DtdProcessing = DtdProcessing.Prohibit;
            return LoadAsync(reader, parserContext, useRestrictiveXamlReader);
        }

        internal static bool ShouldReWrapException(Exception e, Uri baseUri)
        {
            XamlParseException xpe = e as XamlParseException;
            if (xpe != null)
            {
                // If we can set, the BaseUri, rewrap; otherwise we don't need to
                return (xpe.BaseUri == null && baseUri != null);
            }
            // Not an XPE, so we need to wrap it
            return true;
        }

        private object LoadAsync(XmlReader reader, ParserContext parserContext)
        {
            return LoadAsync(reader, parserContext, useRestrictiveXamlReader: false);
        }

        private object LoadAsync(XmlReader reader, ParserContext parserContext, bool useRestrictiveXamlReader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (parserContext == null)
            {
                parserContext = new ParserContext();
            }

            _xmlReader = reader;
            object rootObject = null;
            if (parserContext.BaseUri == null ||
                 String.IsNullOrEmpty(parserContext.BaseUri.ToString()))
            {
                if (reader.BaseURI == null ||
                    String.IsNullOrEmpty(reader.BaseURI.ToString()))
                {
                    parserContext.BaseUri = BaseUriHelper.PackAppBaseUri;
                }
                else
                {
                    parserContext.BaseUri = new Uri(reader.BaseURI);
                }
            }
            _baseUri = parserContext.BaseUri;
            System.Xaml.XamlXmlReaderSettings settings = new System.Xaml.XamlXmlReaderSettings();
            settings.IgnoreUidsOnPropertyElements = true;
            settings.BaseUri = parserContext.BaseUri;
            settings.ProvideLineInfo = true;
            XamlSchemaContext schemaContext = parserContext.XamlTypeMapper != null ?
                parserContext.XamlTypeMapper.SchemaContext : GetWpfSchemaContext();

            try
            {
                _textReader = (useRestrictiveXamlReader) ? new RestrictiveXamlXmlReader(reader, schemaContext, settings) :
                                                           new System.Xaml.XamlXmlReader(reader, schemaContext, settings);

                _stack = new XamlContextStack<WpfXamlFrame>(() => new WpfXamlFrame());

                System.Xaml.XamlObjectWriterSettings objectSettings = XamlReader.CreateObjectWriterSettings();
                objectSettings.AfterBeginInitHandler = delegate(object sender, System.Xaml.XamlObjectEventArgs args)
                    {
                        if (rootObject == null)
                        {
                            rootObject = args.Instance;
                            _styleConnector = rootObject as IStyleConnector;
                        }

                        UIElement uiElement = args.Instance as UIElement;
                        if (uiElement != null)
                        {
                            uiElement.SetPersistId(_persistId++);
                        }

                        DependencyObject dObject = args.Instance as DependencyObject;
                        if (dObject != null && _stack.CurrentFrame.XmlnsDictionary != null)
                        {
                            XmlnsDictionary dictionary = _stack.CurrentFrame.XmlnsDictionary;
                            dictionary.Seal();

                            XmlAttributeProperties.SetXmlnsDictionary(dObject, dictionary);
                        }
                    };

                _objectWriter = new System.Xaml.XamlObjectWriter(_textReader.SchemaContext, objectSettings);
                _parseCancelled = false;
                _skipJournaledProperties = parserContext.SkipJournaledProperties;

                XamlMember synchronousModeProperty = _textReader.SchemaContext.GetXamlDirective("http://schemas.microsoft.com/winfx/2006/xaml", "SynchronousMode");
                XamlMember synchronousRecordProperty = _textReader.SchemaContext.GetXamlDirective("http://schemas.microsoft.com/winfx/2006/xaml", "AsyncRecords");

                System.Xaml.XamlReader xamlReader = _textReader;

                IXamlLineInfo xamlLineInfo = xamlReader as IXamlLineInfo;
                IXamlLineInfoConsumer xamlLineInfoConsumer = _objectWriter as IXamlLineInfoConsumer;
                bool shouldPassLineNumberInfo = false;
                if ((xamlLineInfo != null && xamlLineInfo.HasLineInfo)
                    && (xamlLineInfoConsumer != null && xamlLineInfoConsumer.ShouldProvideLineInfo))
                {
                    shouldPassLineNumberInfo = true;
                }

                bool async = false;
                bool lastPropWasSyncMode = false;
                bool lastPropWasSyncRecords = false;

                while (!_textReader.IsEof)
                {
                    WpfXamlLoader.TransformNodes(xamlReader, _objectWriter, true /*onlyLoadOneNode*/, _skipJournaledProperties, shouldPassLineNumberInfo, xamlLineInfo, xamlLineInfoConsumer, _stack, _styleConnector);

                    if (xamlReader.NodeType == System.Xaml.XamlNodeType.StartMember)
                    {
                        if (xamlReader.Member == synchronousModeProperty)
                        {
                            lastPropWasSyncMode = true;
                        }
                        else if (xamlReader.Member == synchronousRecordProperty)
                        {
                            lastPropWasSyncRecords = true;
                        }
                    }
                    else if (xamlReader.NodeType == System.Xaml.XamlNodeType.Value)
                    {
                        if (lastPropWasSyncMode == true)
                        {
                            if (xamlReader.Value as String == "Async")
                            {
                                async = true;
                            }
                        }
                        else if (lastPropWasSyncRecords == true)
                        {
                            if (xamlReader.Value is int)
                            {
                                _maxAsynxRecords = (int)xamlReader.Value;
                            }
                            else if (xamlReader.Value is String)
                            {
                                _maxAsynxRecords = Int32.Parse(xamlReader.Value as String, TypeConverterHelper.InvariantEnglishUS);
                            }
                        }
                    }
                    else if (xamlReader.NodeType == System.Xaml.XamlNodeType.EndMember)
                    {
                        lastPropWasSyncMode = false;
                        lastPropWasSyncRecords = false;
                    }

                    if (async && rootObject != null)
                        break;
                }
            }
            catch (Exception e)
            {
                // Don't wrap critical exceptions or already-wrapped exceptions.
                if (MS.Internal.CriticalExceptions.IsCriticalException(e) || !ShouldReWrapException(e, parserContext.BaseUri))
                {
                    throw;
                }
                RewrapException(e, parserContext.BaseUri);
            }

            if (!_textReader.IsEof)
            {
                Post();
                //ThreadStart threadStart = new ThreadStart(ReadXamlAsync);
                //Thread thread = new Thread(threadStart);
                //thread.Start();
            }
            else
            {
                TreeBuildComplete();
            }

            if (rootObject is DependencyObject)
            {
                if (parserContext.BaseUri != null && !String.IsNullOrEmpty(parserContext.BaseUri.ToString()))
                    (rootObject as DependencyObject).SetValue(BaseUriHelper.BaseUriProperty, parserContext.BaseUri);
                //else
                //    (rootObject as DependencyObject).SetValue(BaseUriHelper.BaseUriProperty, BaseUriHelper.PackAppBaseUri);
                WpfXamlLoader.EnsureXmlNamespaceMaps(rootObject, schemaContext);
            }

            Application app = rootObject as Application;
            if (app != null)
            {
                app.ApplicationMarkupBaseUri = GetBaseUri(settings.BaseUri);
            }

            return rootObject;
        }

        // We need to rewrap exceptions to get them into standard form of XPE -> base exception
        internal static void RewrapException(Exception e, Uri baseUri)
        {
            RewrapException(e, null, baseUri);
        }

        internal static void RewrapException(Exception e, IXamlLineInfo lineInfo, Uri baseUri)
        {
            throw WrapException(e, lineInfo, baseUri);
        }

        internal static XamlParseException WrapException(Exception e, IXamlLineInfo lineInfo, Uri baseUri)
        {
            Exception baseException = (e.InnerException == null) ? e : e.InnerException;
            if (baseException is System.Windows.Markup.XamlParseException)
            {
                var xe = ((System.Windows.Markup.XamlParseException)baseException);
                xe.BaseUri = xe.BaseUri ?? baseUri;
                if (lineInfo != null && xe.LinePosition == 0 && xe.LineNumber == 0)
                {
                    xe.LinePosition = lineInfo.LinePosition;
                    xe.LineNumber = lineInfo.LineNumber;
                }
                return xe;
            }
            if (e is System.Xaml.XamlException)
            {
                System.Xaml.XamlException xe = (System.Xaml.XamlException)e;
                return new XamlParseException(xe.Message, xe.LineNumber, xe.LinePosition, baseUri, baseException);
            }
            else if (e is XmlException)
            {
                XmlException xe = (XmlException)e;
                return new XamlParseException(xe.Message, xe.LineNumber, xe.LinePosition, baseUri, baseException);
            }
            else
            {
                if (lineInfo != null)
                {
                    return new XamlParseException(e.Message, lineInfo.LineNumber, lineInfo.LinePosition, baseUri, baseException);
                }
                else
                {
                    return new XamlParseException(e.Message, baseException);
                }
            }
        }

        /// <summary>
        ///  Post a queue item at default priority
        /// </summary>
        internal void Post()
        {
            Post(DispatcherPriority.Background);
        }

        /// <summary>
        ///  Post a queue item at the specified priority
        /// </summary>
        internal void Post(DispatcherPriority priority)
        {
            DispatcherOperationCallback callback = new DispatcherOperationCallback(Dispatch);
            Dispatcher.CurrentDispatcher.BeginInvoke(priority, callback, this);
        }

        /// <summary>
        ///  Dispatch delegate
        /// </summary>
        private object Dispatch(object o)
        {
            DispatchParserQueueEvent((XamlReader)o);
            return null;
        }

        private void DispatchParserQueueEvent(XamlReader xamlReader)
        {
            xamlReader.HandleAsyncQueueItem();
        }

        const int AsyncLoopTimeout = (int)200;
        /// <summary>
        /// called when in async mode when get a time slice to read and load the Tree
        /// </summary>
        internal virtual void HandleAsyncQueueItem()
        {
            try
            {
                int startTickCount = MS.Win32.SafeNativeMethods.GetTickCount();
                //bool moreData = true;

                // for debugging, can set the Maximum Async records to
                // read via markup
                // x:AsyncRecords="3" would loop three times
                int maxRecords = _maxAsynxRecords;

                System.Xaml.XamlReader xamlReader = _textReader;

                IXamlLineInfo xamlLineInfo = xamlReader as IXamlLineInfo;
                IXamlLineInfoConsumer xamlLineInfoConsumer = _objectWriter as IXamlLineInfoConsumer;
                bool shouldPassLineNumberInfo = false;
                if ((xamlLineInfo != null && xamlLineInfo.HasLineInfo)
                    && (xamlLineInfoConsumer != null && xamlLineInfoConsumer.ShouldProvideLineInfo))
                {
                    shouldPassLineNumberInfo = true;
                }

                XamlMember synchronousRecordProperty = _textReader.SchemaContext.GetXamlDirective(XamlLanguage.Xaml2006Namespace, "AsyncRecords");

                while (!xamlReader.IsEof && !_parseCancelled)
                {
                    WpfXamlLoader.TransformNodes(xamlReader, _objectWriter, true /*onlyLoadOneNode*/, _skipJournaledProperties, shouldPassLineNumberInfo, xamlLineInfo, xamlLineInfoConsumer, _stack, _styleConnector);

                    if (xamlReader.NodeType == System.Xaml.XamlNodeType.Value && _stack.CurrentFrame.Property == synchronousRecordProperty)
                    {
                        if (xamlReader.Value is int)
                        {
                            _maxAsynxRecords = (int)xamlReader.Value;
                        }
                        else if (xamlReader.Value is String)
                        {
                            _maxAsynxRecords = Int32.Parse(xamlReader.Value as String, TypeConverterHelper.InvariantEnglishUS);
                        }
                        maxRecords = _maxAsynxRecords;
                    }

                    //Debug.Assert (1 >= RootList.Count, "Multiple roots not supported in async mode");

                    // check the timeout
                    int elapsed = MS.Win32.SafeNativeMethods.GetTickCount() - startTickCount;

                    // check for rollover
                    if (elapsed < 0)
                    {
                        startTickCount = 0; // reset to 0,
                    }
                    else if (elapsed > AsyncLoopTimeout)
                    {
                        break;
                    }

                    // decrement and compare with zero so the unitialized -1  and zero case
                    // doesn't break the loop.
                    if (--maxRecords == 0)
                    {
                        break;
                    }
                }
            }
            catch (XamlParseException e)
            {
                _parseException = e;
            }
            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e) || !XamlReader.ShouldReWrapException(e, _baseUri))
                {
                    _parseException = e;
                }
                else
                {
                    _parseException = XamlReader.WrapException(e, null, _baseUri);
                }
            }
            finally
            {
                if (_parseException != null || _parseCancelled)
                {
                    TreeBuildComplete();
                }
                else
                {
                    // if not at the EndOfDocument then post another work item
                    if (false == _textReader.IsEof)
                    {
                        Post();
                    }
                    else
                    {
                        // Building of the Tree is complete.
                        TreeBuildComplete();
                    }
                }
            }
        }

        internal void TreeBuildComplete()
        {
            //if (ParseCompleted != null)
            //{
            if (LoadCompleted != null)
            {
                // Fire the ParseCompleted event asynchronously
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    (DispatcherOperationCallback)delegate(object obj)
                    {
                        LoadCompleted(this, new AsyncCompletedEventArgs(_parseException, _parseCancelled, null));
                        return null;
                    },
                    null);
            }
            //}
            _xmlReader.Close();
            _objectWriter = null;
            _stream = null;
            _textReader = null;
            _stack = null;
        }


        /// <summary>
        /// Aborts the current async load operation if there is one.
        /// </summary>
        /// <remarks>
        /// Notice that the cancellation is an asynchronous operation in itself which means
        /// that there may be some amount of loading that happens before the operation
        /// actually gets aborted.
        /// </remarks>
        public void CancelAsync()
        {
            _parseCancelled = true;
        }

        /// <summary>
        /// This event is fired when either a sync or an async load operation has completed
        /// or when an async load operation is aborted.
        /// </summary>
        public event AsyncCompletedEventHandler LoadCompleted;

        #endregion Public Methods

        #region Internal Methods
        internal static XamlObjectWriterSettings CreateObjectWriterSettings()
        {
            XamlObjectWriterSettings owSettings = new XamlObjectWriterSettings();
            owSettings.IgnoreCanConvert = true;
            owSettings.PreferUnconvertedDictionaryKeys = true;
            return owSettings;
        }

        internal static XamlObjectWriterSettings CreateObjectWriterSettings(XamlObjectWriterSettings parentSettings)
        {
            XamlObjectWriterSettings owSettings = CreateObjectWriterSettings();
            if (parentSettings != null)
            {
                owSettings.SkipDuplicatePropertyCheck = parentSettings.SkipDuplicatePropertyCheck;
                owSettings.AccessLevel = parentSettings.AccessLevel;
                owSettings.SkipProvideValueOnRoot = parentSettings.SkipProvideValueOnRoot;
                owSettings.SourceBamlUri = parentSettings.SourceBamlUri;
            }
            return owSettings;
        }

        internal static XamlObjectWriterSettings CreateObjectWriterSettingsForBaml()
        {
            XamlObjectWriterSettings owSettings = CreateObjectWriterSettings();
            owSettings.SkipDuplicatePropertyCheck = true;
            return owSettings;
        }

        internal static Baml2006ReaderSettings CreateBamlReaderSettings()
        {
            Baml2006ReaderSettings brSettings = new Baml2006ReaderSettings();
            brSettings.IgnoreUidsOnPropertyElements = true;
            return brSettings;
        }

        internal static XamlSchemaContextSettings CreateSchemaContextSettings()
        {
            XamlSchemaContextSettings xscSettings = new XamlSchemaContextSettings();
            xscSettings.SupportMarkupExtensionsWithDuplicateArity = true;
            return xscSettings;
        }

        internal static WpfSharedBamlSchemaContext BamlSharedSchemaContext { get { return _bamlSharedContext.Value; } }
        internal static WpfSharedBamlSchemaContext XamlV3SharedSchemaContext { get { return _xamlV3SharedContext.Value; } }
        public static XamlSchemaContext GetWpfSchemaContext()
        {
            return _xamlSharedContext.Value;
        }

        /// <summary>


        /// <summary>
        /// Reads XAML from the passed stream, building an object tree and returning the
        /// root of that tree.  Wrap a CompatibilityReader with another XmlReader that
        /// uses the passed reader settings to allow validation of xaml.
        /// </summary>
        /// <param name="reader">XmlReader to use.  This is NOT wrapped by any
        ///  other reader</param>
        /// <param name="context">Optional parser context.  May be null </param>
        /// <param name="parseMode">Sets synchronous or asynchronous parsing</param>
        /// <returns>object root generated after xml parsed</returns>
        // Note this is the internal entry point for XPS.  XPS calls here so
        // its reader is not wrapped with a Markup Compat Reader.
        internal static object Load(
            XmlReader reader,
            ParserContext parserContext,
            XamlParseMode parseMode)
        {
            return Load(reader, parserContext, parseMode, useRestrictiveXamlReader: false);
        }

        /// <summary>
        /// Reads XAML from the passed stream, building an object tree and returning the
        /// root of that tree.  Wrap a CompatibilityReader with another XmlReader that
        /// uses the passed reader settings to allow validation of xaml.
        /// </summary>
        /// <param name="reader">XmlReader to use.  This is NOT wrapped by any
        ///  other reader</param>
        /// <param name="context">Optional parser context.  May be null </param>
        /// <param name="parseMode">Sets synchronous or asynchronous parsing</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <returns>object root generated after xml parsed</returns>
        internal static object Load(
        XmlReader reader,
        ParserContext parserContext,
        XamlParseMode parseMode,
        bool useRestrictiveXamlReader)
        {
            return Load(reader, parserContext, parseMode, useRestrictiveXamlReader, null);
        }

        /// <summary>
        /// Reads XAML from the passed stream, building an object tree and returning the
        /// root of that tree.  Wrap a CompatibilityReader with another XmlReader that
        /// uses the passed reader settings to allow validation of xaml.
        /// </summary>
        /// <param name="reader">XmlReader to use.  This is NOT wrapped by any
        ///  other reader</param>
        /// <param name="context">Optional parser context.  May be null </param>
        /// <param name="parseMode">Sets synchronous or asynchronous parsing</param>
        /// <param name="useRestrictiveXamlReader">Whether or not this method should use 
        /// RestrictiveXamlXmlReader to restrict instantiation of potentially dangerous types</param>
        /// <param name="safeTypes">List of known safe Types to be allowed through the RestrictiveXamlXmlReader</param>
        /// <returns>object root generated after xml parsed</returns>
        internal static object Load(
            XmlReader reader,
            ParserContext parserContext,
            XamlParseMode parseMode,
            bool useRestrictiveXamlReader,
            List<Type> safeTypes)
        {
            if (parseMode == XamlParseMode.Uninitialized ||
                parseMode == XamlParseMode.Asynchronous)
            {
                XamlReader xamlReader = new XamlReader();
                return xamlReader.LoadAsync(reader, parserContext, useRestrictiveXamlReader);
            }

            if (parserContext == null)
            {
                parserContext = new ParserContext();
            }

#if DEBUG_CLR_MEM
            bool clrTracingEnabled = false;
            // Use local pass variable to correctly log nested parses.
            int pass = 0;

            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                clrTracingEnabled = true;
                pass = ++_CLRXamlPass;
                CLRProfilerControl.CLRLogWriteLine("Begin_XamlParse_{0}", pass);
            }
#endif // DEBUG_CLR_MEM

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Event.WClientParseXmlBegin, parserContext.BaseUri);

            if (TraceMarkup.IsEnabled)
            {
                TraceMarkup.Trace(TraceEventType.Start, TraceMarkup.Load);
            }
            object root = null;
            try
            {
                if (parserContext.BaseUri == null ||
                    String.IsNullOrEmpty(parserContext.BaseUri.ToString()))
                {
                    if (reader.BaseURI == null ||
                        String.IsNullOrEmpty(reader.BaseURI.ToString()))
                    {
                        parserContext.BaseUri = BaseUriHelper.PackAppBaseUri;
                    }
                    else
                    {
                        parserContext.BaseUri = new Uri(reader.BaseURI);
                    }
                }

                System.Xaml.XamlXmlReaderSettings settings = new System.Xaml.XamlXmlReaderSettings();
                settings.IgnoreUidsOnPropertyElements = true;
                settings.BaseUri = parserContext.BaseUri;
                settings.ProvideLineInfo = true;

                XamlSchemaContext schemaContext = parserContext.XamlTypeMapper != null ?
                    parserContext.XamlTypeMapper.SchemaContext : GetWpfSchemaContext();
                System.Xaml.XamlXmlReader xamlXmlReader = (useRestrictiveXamlReader) ? new RestrictiveXamlXmlReader(reader, schemaContext, settings, safeTypes) :
                                                                                       new System.Xaml.XamlXmlReader(reader, schemaContext, settings);
                root = Load(xamlXmlReader, parserContext);
                reader.Close();
            }
            catch (Exception e)
            {
                // Don't wrap critical exceptions or already-wrapped exceptions.
                if (MS.Internal.CriticalExceptions.IsCriticalException(e) || !ShouldReWrapException(e, parserContext.BaseUri))
                {
                    throw;
                }
                RewrapException(e, parserContext.BaseUri);
            }
            finally
            {
                if (TraceMarkup.IsEnabled)
                {
                    TraceMarkup.Trace(TraceEventType.Stop, TraceMarkup.Load, root);
                }

                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Event.WClientParseXmlEnd, parserContext.BaseUri);

#if DEBUG_CLR_MEM
                if (clrTracingEnabled && (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
                {
                    CLRProfilerControl.CLRLogWriteLine("End_XamlParse_{0}", pass);
                }
#endif // DEBUG_CLR_MEM
            }
            return root;
        }
        
            internal static object Load(
            System.Xaml.XamlReader xamlReader,
            ParserContext parserContext)
        {
            if (parserContext == null)
            {
                parserContext = new ParserContext();
            }

            // In some cases, the application constructor is not run prior to loading,
            // causing the loader not to recognize URIs beginning with "pack:" or "application:".
            MS.Internal.WindowsBase.SecurityHelper.RunClassConstructor(typeof(System.Windows.Application));

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientParseXamlBegin, parserContext.BaseUri);

            object root = WpfXamlLoader.Load(xamlReader, parserContext.SkipJournaledProperties, parserContext.BaseUri);

            DependencyObject dObject = root as DependencyObject;
            if (dObject != null)
            {
                if (parserContext.BaseUri != null && !String.IsNullOrEmpty(parserContext.BaseUri.ToString()))
                {
                    dObject.SetValue(BaseUriHelper.BaseUriProperty, parserContext.BaseUri);
                }
            }

            Application app = root as Application;
            if (app != null)
            {
                app.ApplicationMarkupBaseUri = GetBaseUri(parserContext.BaseUri);
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientParseXamlEnd, parserContext.BaseUri);

            return root;
        }

        public static object Load(System.Xaml.XamlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            object root = null;
            try
            {
                root = Load(reader, null);
            }
            catch (Exception e)
            {
                IUriContext uriContext = reader as IUriContext;
                Uri baseUri = (uriContext != null) ? uriContext.BaseUri : null;
                // Don't wrap critical exceptions or already-wrapped exceptions.
                if (MS.Internal.CriticalExceptions.IsCriticalException(e) || !ShouldReWrapException(e, baseUri))
                {
                    throw;
                }
                RewrapException(e, baseUri);
            }
            finally
            {
                if (TraceMarkup.IsEnabled)
                {
                    TraceMarkup.Trace(TraceEventType.Stop, TraceMarkup.Load, root);
                }

                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Event.WClientParseXmlEnd);

#if DEBUG_CLR_MEM
                if (clrTracingEnabled && (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
                {
                    CLRProfilerControl.CLRLogWriteLine("End_XamlParse_{0}", pass);
                }
#endif // DEBUG_CLR_MEM
            }
            return root;
        }

        /// <summary>
        /// Loads the given baml stream
        /// </summary>
        /// <param name="stream">input as stream</param>
        /// <param name="parent">parent owner element of baml tree</param>
        /// <param name="parserContext">parser context</param>
        /// <param name="closeStream">True if stream should be closed by the
        ///    parser after parsing is complete.  False if the stream should be left open</param>
        /// <returns>object root generated after baml parsed</returns>
        internal static object LoadBaml(
            Stream stream,
            ParserContext parserContext,
            object parent,
            bool closeStream)
        {
            object root = null;

#if DEBUG_CLR_MEM
            bool clrTracingEnabled = false;
            // Use local pass variable to correctly log nested parses.
            int pass = 0;

            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                clrTracingEnabled = true;
                pass = ++_CLRBamlPass;
                CLRProfilerControl.CLRLogWriteLine("Begin_BamlParse_{0}", pass);
            }
#endif // DEBUG_CLR_MEM

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientParseBamlBegin, parserContext.BaseUri);

            if (TraceMarkup.IsEnabled)
            {
                TraceMarkup.Trace(TraceEventType.Start, TraceMarkup.Load);
            }

            try
            {
                //
                // If the stream contains info about the Assembly that created it,
                // set StreamCreatedAssembly from the stream instance.
                //
                IStreamInfo streamInfo = stream as IStreamInfo;
                if (streamInfo != null)
                {
                    parserContext.StreamCreatedAssembly = streamInfo.Assembly;
                }

                Baml2006ReaderSettings readerSettings = XamlReader.CreateBamlReaderSettings();
                readerSettings.BaseUri = parserContext.BaseUri;
                readerSettings.LocalAssembly = streamInfo.Assembly;
                // We do not set OwnsStream = true so the Baml2006Reader will not close the stream.
                // Calling code is responsible for disposing the stream
                if (readerSettings.BaseUri == null || String.IsNullOrEmpty(readerSettings.BaseUri.ToString()))
                {
                    readerSettings.BaseUri = BaseUriHelper.PackAppBaseUri;
                }

                var reader = new Baml2006ReaderInternal(stream, new Baml2006SchemaContext(readerSettings.LocalAssembly), readerSettings, parent);

                // We don't actually use the GeneratedInternalTypeHelper any more.
                // But for v3 compat, don't allow loading of internals in PT unless there is one.
                Type internalTypeHelper = null;
                if (streamInfo.Assembly != null)
                {
                    try
                    {
                        internalTypeHelper = XamlTypeMapper.GetInternalTypeHelperTypeFromAssembly(parserContext);
                    }
                    // This can perform attribute reflection which will fail if the assembly has unresolvable
                    // attributes. If that happens, just assume there is no helper.
                    catch (Exception e)
                    {
                        if (MS.Internal.CriticalExceptions.IsCriticalException(e))
                        {
                            throw;
                        }
                    }
                }

                if (internalTypeHelper != null)
                {
                    XamlAccessLevel accessLevel = XamlAccessLevel.AssemblyAccessTo(streamInfo.Assembly);
                    root = WpfXamlLoader.LoadBaml(reader, parserContext.SkipJournaledProperties, parent, accessLevel, parserContext.BaseUri);
                }
                else
                {
                    root = WpfXamlLoader.LoadBaml(reader, parserContext.SkipJournaledProperties, parent, null, parserContext.BaseUri);
                }

                DependencyObject dObject = root as DependencyObject;
                if (dObject != null)
                {
                    dObject.SetValue(BaseUriHelper.BaseUriProperty, readerSettings.BaseUri);
                }

                Application app = root as Application;
                if (app != null)
                {
                    app.ApplicationMarkupBaseUri = GetBaseUri(readerSettings.BaseUri);
                }

                Debug.Assert(parent == null || root == parent);
            }

            finally
            {
                if (TraceMarkup.IsEnabled)
                {
                    TraceMarkup.Trace(TraceEventType.Stop, TraceMarkup.Load, root);
                }

                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientParseBamlEnd, parserContext.BaseUri);

                if (closeStream && stream != null)
                {
                    stream.Close();
                }

#if DEBUG_CLR_MEM
                if (clrTracingEnabled && (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
                {
                    CLRProfilerControl.CLRLogWriteLine("End_BamlParse_{0}", pass);
                }
#endif // DEBUG_CLR_MEM
            }

            return (root);
        }

        static Uri GetBaseUri(Uri uri)
        {
            if (uri == null)
            {
                return MS.Internal.Utility.BindUriHelper.BaseUri;
            }
            else if (uri.IsAbsoluteUri == false)
            {
                return new Uri(MS.Internal.Utility.BindUriHelper.BaseUri, uri);
            }

            return uri;
        }

        private static WpfSharedBamlSchemaContext CreateBamlSchemaContext()
        {
            XamlSchemaContextSettings settings = new XamlSchemaContextSettings();
            settings.SupportMarkupExtensionsWithDuplicateArity = true;
            return new WpfSharedBamlSchemaContext(settings);
        }

        private static WpfSharedXamlSchemaContext CreateXamlSchemaContext(bool useV3Rules)
        {
            XamlSchemaContextSettings settings = new XamlSchemaContextSettings();
            settings.SupportMarkupExtensionsWithDuplicateArity = true;
            return new WpfSharedXamlSchemaContext(settings, useV3Rules);
        }

        #endregion Internal Methods

        #region Data

        private Uri _baseUri;
        private System.Xaml.XamlReader _textReader;
        private XmlReader _xmlReader;
        private System.Xaml.XamlObjectWriter _objectWriter;
        private Stream _stream;
        private bool _parseCancelled;
        private Exception _parseException;
        private int _persistId = 1;
        private bool _skipJournaledProperties;
        private XamlContextStack<WpfXamlFrame> _stack;
        private int _maxAsynxRecords = -1;
        private IStyleConnector _styleConnector;
        private static readonly Lazy<WpfSharedBamlSchemaContext> _bamlSharedContext =
            new Lazy<WpfSharedBamlSchemaContext>(() => CreateBamlSchemaContext());
        private static readonly Lazy<WpfSharedXamlSchemaContext> _xamlSharedContext =
            new Lazy<WpfSharedXamlSchemaContext>(() => CreateXamlSchemaContext(false));
        private static readonly Lazy<WpfSharedXamlSchemaContext> _xamlV3SharedContext =
            new Lazy<WpfSharedXamlSchemaContext>(() => CreateXamlSchemaContext(true));

#if DEBUG_CLR_MEM
        private static int _CLRBamlPass = 0;
        private static int _CLRXamlPass = 0;
#endif

        #endregion Data
    }

    internal class WpfXamlFrame : XamlFrame
    {
        public WpfXamlFrame() { }
        public bool FreezeFreezable { get; set; }
        public XamlMember Property { get; set; }
        public XamlType Type { get; set; }
        public object Instance { get; set; }
        public XmlnsDictionary XmlnsDictionary { get; set; }
        public bool? XmlSpace { get; set; }

        public override void Reset()
        {
            Type = null;
            Property = null;
            Instance = null;
            XmlnsDictionary = null;
            XmlSpace = null;
            if (FreezeFreezable)
            {
                FreezeFreezable = false;
            }
        }
    }
}
