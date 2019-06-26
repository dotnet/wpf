// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Printing;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;
using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a Fixed Page.
    /// </summary>
    internal class XpsOMFixedPageSerializer :
                   ReachSerializer
    {
        #region Constructor

        public
        XpsOMFixedPageSerializer(
            PackageSerializationManager manager
            ) :
            base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManager
            /// 
            _xpsOMSerializationManager = (XpsOMSerializationManager)manager;
        }

        #endregion Constructor

        #region Public Methods

        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            Initialize();

            base.SerializeObject(serializedObject);
        }

        #endregion Public Methods

        #region Internal Methods

        internal
        override
        void
        SerializeObject(
            SerializablePropertyContext serializedProperty
            )
        {
            Initialize();

            base.SerializeObject(serializedProperty);
        }

        internal
        void
        Initialize(
            )
        {
            //
            // Create the ImageTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageImageTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageColorContextTable = new Dictionary<int, Uri>();
            //
            // Create the ResourceDictionaryTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageResourceDictionaryTable = new Dictionary<int, Uri>();
        }

        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext serializableObjectContext
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSerializationBegin);
            ReachTreeWalker treeWalker;

            bool needEndVisual = BeginPersistObjectData(serializableObjectContext, out treeWalker);

            if (serializableObjectContext.IsComplexValue)
            {
                SerializeObjectCore(serializableObjectContext);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_WrongPropertyTypeForFixedPage));
            }

            EndPersistObjectData(needEndVisual, treeWalker);

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSerializationEnd);
        }

        internal
        bool
        BeginPersistObjectData(SerializableObjectContext serializableObjectContext, out ReachTreeWalker treeWalker)
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            _xpsOMSerializationManager.RegisterPageStart();


            if (serializableObjectContext.IsComplexValue)
            {
                PrintTicket printTicket = _xpsOMSerializationManager.FixedPagePrintTicket;

                if (printTicket != null)
                {
                    PrintTicketSerializer serializer = new PrintTicketSerializer(SerializationManager);
                    serializer.SerializeObject(printTicket);
                    _xpsOMSerializationManager.FixedPagePrintTicket = null;
                }
            }

            FixedPage fixedPage = serializableObjectContext.TargetObject as FixedPage;

            treeWalker = new ReachTreeWalker(this);
            treeWalker.SerializeLinksInFixedPage((FixedPage)serializableObjectContext.TargetObject);
            
            String xmlnsForType = SerializationManager.GetXmlNSForType(serializableObjectContext.TargetObject.GetType());

            if (xmlnsForType == null)
            {
                XmlWriter.WriteStartElement(serializableObjectContext.Name);
            }
            else
            {
                XmlWriter.WriteStartElement(serializableObjectContext.Name);

                XmlWriter.WriteAttributeString(XpsS0Markup.Xmlns, xmlnsForType);
                XmlWriter.WriteAttributeString(XpsS0Markup.XmlnsX, XpsS0Markup.XmlnsXSchema);

                XmlLanguage language = fixedPage.Language;
                if (language == null)
                {
                    //If the language property is null, assign the language to the default
                    language = XmlLanguage.GetLanguage(XpsS0Markup.XmlLangValue);
                }

                SerializationManager.Language = language;

                XmlWriter.WriteAttributeString(XpsS0Markup.XmlLang, language.ToString());
            }

            Size fixedPageSize = new Size(fixedPage.Width, fixedPage.Height);
            _xpsOMSerializationManager.FixedPageSize = fixedPageSize;

            //
            // Before we serialize any properties on the FixedPage, we need to
            // serialize the FixedPage as a Visual
            //
            Visual fixedPageAsVisual = serializableObjectContext.TargetObject as Visual;

            bool needEndVisual = false;

            if (fixedPageAsVisual != null)
            {
                needEndVisual = SerializePageAsVisual(fixedPageAsVisual);
            }

            return needEndVisual;
        }

        internal
        void
        EndPersistObjectData(bool needEndVisual, ReachTreeWalker treeWalker)
        {
            if (needEndVisual)
            {
                XmlWriter pageWriter = SerializationManager.
                                                  PackagingPolicy.AcquireXmlWriterForPage();

                XmlWriter resWriter = SerializationManager.
                                                  PackagingPolicy.AcquireXmlWriterForResourceDictionary();

                Size fixedPageSize = _xpsOMSerializationManager.FixedPageSize;
                VisualTreeFlattener flattener = _xpsOMSerializationManager.
                                                  VisualSerializationService.AcquireVisualTreeFlattener(resWriter,
                                                                                                        pageWriter,
                                                                                                        fixedPageSize);

                flattener.EndVisual();
            }

            SerializationManager.PackagingPolicy.PreCommitCurrentPage();

            //
            // Copy hyperlinks into stream
            //
            treeWalker.CommitHyperlinks();

            XmlWriter.WriteEndElement();
            //
            //Release used resources
            //
            XmlWriter = null;
            //
            // Free the image table in use for this page
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageImageTable = null;
            //
            // Free the colorContext table in use for this page
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageColorContextTable = null;
            //
            // Free the resourceDictionary table in use for this page
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageResourceDictionaryTable = null;

            _xpsOMSerializationManager.VisualSerializationService.ReleaseVisualTreeFlattener();

            _xpsOMSerializationManager.RegisterPageEnd();

            //
            // Signal to any registered callers that the Page has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedPageWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            _xpsOMSerializationManager.OnXPSSerializationProgressChanged(progressEvent);
        }

        internal
        override
        void
        WriteSerializedAttribute(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if (serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }

            String attributeValue = String.Empty;

            attributeValue = GetValueOfAttributeAsString(serializablePropertyContext);

            if ((attributeValue != null) &&
                 (attributeValue.Length > 0))
            {
                //
                // Emit name="value" attribute
                //
                XmlWriter.WriteAttributeString(serializablePropertyContext.Name, attributeValue);
            }
        }

        private
        String
        GetValueOfAttributeAsString(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if (serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }

            String valueAsString = null;
            Object targetObjectContainingProperty = serializablePropertyContext.TargetObject;
            Object propertyValue = serializablePropertyContext.Value;

            if (propertyValue != null)
            {
                TypeConverter typeConverter = serializablePropertyContext.TypeConverter;

                valueAsString = typeConverter.ConvertToInvariantString(new XpsTokenContext(SerializationManager,
                                                                                             serializablePropertyContext),
                                                                       propertyValue);


                if (typeof(Type).IsInstanceOfType(propertyValue))
                {
                    int index = valueAsString.LastIndexOf('.');

                    if (index > 0)
                    {
                        valueAsString = valueAsString.Substring(index + 1);
                    }

                    valueAsString = XpsSerializationManager.TypeOfString + valueAsString + "}";
                }
            }
            else
            {
                valueAsString = XpsSerializationManager.NullString;
            }

            return valueAsString;
        }

        #endregion Internal Methods

        #region Public Properties

        /// <summary>
        /// Queries / Set the XmlWriter for a FixedPage.
        /// </summary>
        public
        override
        XmlWriter
        XmlWriter
        {
            get
            {
                if (base.XmlWriter == null)
                {
                    base.XmlWriter = SerializationManager.AcquireXmlWriter(typeof(FixedPage));
                }

                return base.XmlWriter;
            }

            set
            {
                base.XmlWriter = null;
                SerializationManager.ReleaseXmlWriter(typeof(FixedPage));
            }
        }

        #endregion Public Properties

        #region Private Methods

        private
        bool
        SerializePageAsVisual(
            Visual fixedPageAsVisual
            )
        {
            bool needEndVisual = false;

            ReachVisualSerializer serializer = new ReachVisualSerializer(SerializationManager);

            if (serializer != null)
            {
                needEndVisual = serializer.SerializeDisguisedVisual(fixedPageAsVisual);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
            }

            return needEndVisual;
        }

        #endregion Private Methods

        private XpsOMSerializationManager _xpsOMSerializationManager;
    };
}
