// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
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
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Xps;
using MS.Utility;

#pragma warning disable 1634, 1691 //Allows suppression of certain PreSharp messages

namespace System.Windows.Xps.Serialization
{

    /// <summary>
    /// This class defines all necessary methods that are necessary to provide
    /// serialization services for persisting an AVALON root object
    /// into an XPS package. It glues together all necessary serializers and
    /// type converters for different type of objects to produce the correct
    /// serialized content in the package.
    /// </summary>
    public class XpsSerializationManager :
                        PackageSerializationManager,
                        IXpsSerializationManager
    {
        #region Constructor


        /// <summary>
        /// Constructor to create and initialize the XpsSerializationManager
        /// </summary>
        public
        XpsSerializationManager(
            BasePackagingPolicy  packagingPolicy,
            bool                 batchMode
             ):
        base()
        {
            this._packagingPolicy    = packagingPolicy;
            this._isBatchMode        = batchMode;
            this._isSimulating       = false;
            this._simulator          = null;

            _reachSerializationServices = new ReachSerializationServices();
            _visualSerializationService = new VisualSerializationService(this);

            _reachSerializationServices.RegisterNameSpacesForTypes();
            _reachSerializationServices.RegisterSerializableDependencyPropertiesForReachTypes();
            _reachSerializationServices.RegisterNoneSerializableClrPropertiesForReachTypes();

            XpsResourcePolicy resourcePolicy = new XpsResourcePolicy( XpsResourceSharing.NoResourceSharing );
            resourcePolicy.RegisterService(new XpsImageSerializationService(), typeof(XpsImageSerializationService));
            resourcePolicy.RegisterService(new XpsFontSerializationService(packagingPolicy), typeof(XpsFontSerializationService));

            this._resourcePolicy     = resourcePolicy;
            _documentNumber = 0;
            _pageNumber = 0;
            _documentStartState = false;
            _pageStartState = false;

            XpsPackagingPolicy xpsPackagingPolicy = _packagingPolicy as XpsPackagingPolicy;
            if (xpsPackagingPolicy != null)
            {
                _xpsDocEventManager = new XpsDriverDocEventManager(this);

                xpsPackagingPolicy.PackagingProgressEvent += new PackagingProgressEventHandler(_xpsDocEventManager.ForwardPackagingProgressEvent);

                this.XpsSerializationPrintTicketRequiredOnXpsDriverDocEvent += new XpsSerializationPrintTicketRequiredEventHandler(_xpsDocEventManager.ForwardUserPrintTicket);
            }
         }

        #endregion Constructor

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="ArgumentNullException">serializedObject is NULL.</exception>
        /// <exception cref="XpsSerializationException">serializedObject is not a supported type.</exception>
        public
        override
        void
        SaveAsXaml(
             Object  serializedObject
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSaveXpsBegin);

            XmlWriter pageXmlWriter             = null;

            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }

            if(!IsSerializedObjectTypeSupported(serializedObject))
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }

            if( serializedObject is DocumentPaginator )
            {
                // Prefast complains that serializedObject is not tested for null
                // It is tested a few lines up
                #pragma warning suppress 56506
                if((serializedObject as DocumentPaginator).Source is FixedDocument &&
                    serializedObject.GetType().ToString().Contains( "FixedDocumentPaginator") )
                {
                    serializedObject = (serializedObject as DocumentPaginator).Source;
                }
                else
                if( (serializedObject as DocumentPaginator).Source is FixedDocumentSequence&&
                    serializedObject.GetType().ToString().Contains( "FixedDocumentSequencePaginator")  )
                {
                    serializedObject = (serializedObject as DocumentPaginator).Source;
                }
            }

            if(_simulator == null)
            {
                _simulator = new ReachHierarchySimulator(this,
                                                         serializedObject);

            }

            if(!_isSimulating)
            {
                _simulator.BeginConfirmToXPSStructure(_isBatchMode);
                _isSimulating = true;
            }

            if(_isBatchMode)
            {
                pageXmlWriter = _simulator.SimulateBeginFixedPage();
            }

            ReachSerializer reachSerializer = GetSerializer(serializedObject);

            if(reachSerializer != null)
            {
                //
                // Things that need to be done at this stage
                // 1. Setting the stack context
                // 2. Setting the root of the graph for future references
                //
                reachSerializer.SerializeObject(serializedObject);

                if(_isBatchMode)
                {
                    _simulator.SimulateEndFixedPage(pageXmlWriter);
                }
                else
                {
                    _simulator.EndConfirmToXPSStructure(_isBatchMode);
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSaveXpsEnd);
        }

        /// <summary>
        ///
        /// </summary>
        public
        virtual
        void
        Commit(
            )
        {
            if(_isBatchMode && _isSimulating)
            {
                _simulator.EndConfirmToXPSStructure(_isBatchMode);
            }
            else
            {
                //
                // throw the appropariate exception
                //
            }
        }

        /// <summary>
        /// Sets the Font subsetting policy on the
        /// Font Subset service
        /// </summary>
        public
        void
        SetFontSubsettingPolicy(FontSubsetterCommitPolicies policy)
        {
            IServiceProvider resourceServiceProvider = (IServiceProvider)ResourcePolicy;
            XpsFontSerializationService fontService = (XpsFontSerializationService)resourceServiceProvider.GetService(typeof(XpsFontSerializationService));
            if (fontService != null)
            {
                XpsFontSubsetter fontSubsetter = fontService.FontSubsetter;
                fontSubsetter.SetSubsetCommitPolicy(policy);
            }
        }

        /// <summary>
        /// Sets the Font subsetting count policy on the
        /// Font Subset service.  This is used to determine
        /// how many page or document signals are required before
        /// the subsetted font is flushed
        /// </summary>
        public
        void
        SetFontSubsettingCountPolicy(int countPolicy)
        {
            IServiceProvider resourceServiceProvider = (IServiceProvider)ResourcePolicy;
            XpsFontSerializationService fontService = (XpsFontSerializationService)resourceServiceProvider.GetService(typeof(XpsFontSerializationService));
            if (fontService != null)
            {
                XpsFontSubsetter fontSubsetter = fontService.FontSubsetter;
                fontSubsetter.SetSubsetCommitCountPolicy(countPolicy);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        event
        XpsSerializationPrintTicketRequiredEventHandler     XpsSerializationPrintTicketRequired;

        /// <summary>
        ///
        /// </summary>
        public
        event
        XpsSerializationProgressChangedEventHandler         XpsSerializationProgressChanged;

        /// <summary>
        /// XpsDriverDocEventManager subscribes for this event to take the PT that the app supplied and
        /// call XpsDocEvent into the XPS driver.
        /// This will might back with a different PT
        /// </summary>
        internal
        event
        XpsSerializationPrintTicketRequiredEventHandler XpsSerializationPrintTicketRequiredOnXpsDriverDocEvent;


        /// <summary>
        /// PrintQueue will register for this event to get the PT which the app send in and cann XpsDocEvent
        /// into the XPS driver
        /// </summary>
        internal
        event
        XpsSerializationXpsDriverDocEventHandler            XpsSerializationXpsDriverDocEvent;



        /// <summary>
        ///
        /// </summary>
        internal
        override
        String
        GetXmlNSForType(
            Type    objectType
            )
        {
            return (String)_reachSerializationServices.TypesXmlNSMapping[objectType];
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        ReachSerializer
        GetSerializer(
            Object serializedObject
            )
        {
            ReachSerializer reachSerializer = null;

            if((reachSerializer = base.GetSerializer(serializedObject)) == null)
            {
                reachSerializer = this.SerializersCacheManager.GetSerializer(serializedObject);
            }

            return reachSerializer;
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        Type
        GetSerializerType(
            Type objectType
            )
        {
            Type serializerType = null;


            if((serializerType = base.GetSerializerType(objectType)) == null)
            {
                if (typeof(System.Windows.Documents.FixedDocument).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(FixedDocumentSerializer);
                }
                else if (typeof(System.Windows.Documents.PageContentCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachPageContentCollectionSerializer);
                }
                else if(typeof(System.Windows.Documents.PageContent).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachPageContentSerializer);
                }
                else if(typeof(System.Windows.Controls.UIElementCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachUIElementCollectionSerializer);
                }
                else if(typeof(System.Windows.Documents.FixedPage).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(FixedPageSerializer);
                }
                else if (typeof(System.Windows.Documents.FixedDocumentSequence).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(DocumentSequenceSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentReferenceCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachDocumentReferenceCollectionSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentReference).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachDocumentReferenceSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(DocumentPaginatorSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentPage).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(DocumentPageSerializer);
                }
                else if (typeof(System.Windows.Media.Visual).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachVisualSerializer);
                }
                else if (typeof(System.Printing.PrintTicket).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(PrintTicketSerializer);
                }
            }

            return serializerType;
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        TypeConverter
        GetTypeConverter (
            Object serializedObject
            )
        {
            TypeConverter typeConverter = null;

            if (typeof(BitmapSource).IsAssignableFrom(serializedObject.GetType()))
            {
                return new ImageSourceTypeConverter();
            }
            else if (typeof(GlyphRun).IsAssignableFrom(serializedObject.GetType()))
            {
                return new FontTypeConverter();
            }
            else if (typeof(Color).IsAssignableFrom(serializedObject.GetType()))
            {
                return new ColorTypeConverter();
            }
            else
            {
                typeConverter = base.GetTypeConverter(serializedObject);
            }

            return typeConverter;
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        TypeConverter
        GetTypeConverter (
            Type serializedObjectType
            )
        {
            TypeConverter typeConverter = null;

            if (typeof(BitmapSource).IsAssignableFrom(serializedObjectType))
            {
                return new ImageSourceTypeConverter();
            }
            else if (typeof(GlyphRun).IsAssignableFrom(serializedObjectType))
            {
                return new FontTypeConverter();
            }
            else if (typeof(Color).IsAssignableFrom(serializedObjectType))
            {
                return new ColorTypeConverter();
            }
            else
            {
                typeConverter = base.GetTypeConverter(serializedObjectType);
            }

            return typeConverter;
        }



        internal
        override
        XmlWriter
        AcquireXmlWriter(
            Type    writerType
            )
        {
            XmlWriter xmlWriter = null;

            if(_packagingPolicy != null)
            {
                if (writerType == typeof(FixedDocumentSequence))
                {
                    _currentDocumentSequenceWriterRef++;
                    xmlWriter = _packagingPolicy.AcquireXmlWriterForFixedDocumentSequence();
                }
                else if (writerType == typeof(FixedDocument))
                {
                    _currentFixedDocumentWriterRef++;
                    xmlWriter = _packagingPolicy.AcquireXmlWriterForFixedDocument();
                }
                else if (writerType == typeof(FixedPage))
                {
                    _currentFixedPageWriterRef++;
                    xmlWriter = _packagingPolicy.AcquireXmlWriterForFixedPage();
                }
                else if (writerType == typeof(Visual))
                {
                    _currentFixedPageWriterRef++;
                    xmlWriter = _packagingPolicy.AcquireXmlWriterForFixedPage();
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
                }
            }

            return xmlWriter;
        }

        private
        int
        GetTypeRefCnt(Type    writerType)
        {
            int refCnt = 0;
            if( writerType == typeof(FixedDocumentSequence) )
            {
                refCnt = _currentDocumentSequenceWriterRef;
            }
            else
            if( writerType == typeof(FixedDocument) )
            {
                refCnt = _currentFixedDocumentWriterRef;
            }
            else
            if( writerType == typeof(FixedPage) )
            {
                refCnt = _currentFixedPageWriterRef;
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }
            return refCnt;
        }

        private
        int
        DecrementRefCntByType(Type    writerType)
        {
            int refCnt = 0;
            if( writerType == typeof(FixedDocumentSequence) )
            {
                refCnt = --_currentDocumentSequenceWriterRef;
            }
            else
            if( writerType == typeof(FixedDocument) )
            {
                refCnt = --_currentFixedDocumentWriterRef;
            }
            else
            if( writerType == typeof(FixedPage) )
            {
                refCnt = --_currentFixedPageWriterRef;
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }
            return refCnt;
        }

        internal
        override
        void
        ReleaseXmlWriter(
            Type    writerType
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterStart);

            bool subsetComplete = false;
            int refCnt = DecrementRefCntByType(writerType);

            //
            // signal the font sub-setter that we completed a node
             if( _resourcePolicy != null )
            {
                XpsFontSerializationService fontService = (XpsFontSerializationService)_resourcePolicy.GetService(typeof(XpsFontSerializationService));
                //
                // The font subsetter will determine if based on this
                // signal we have completed a subset
                //
                if( fontService != null && refCnt == 0 )
                {
                    subsetComplete = fontService.SignalCommit(writerType);
                }
            }


            //
            // Allow the packaging policy to release the stream
            //
            if(_packagingPolicy != null)
            {
                if (writerType == typeof(FixedDocumentSequence))
                {
                    _packagingPolicy.ReleaseXmlWriterForFixedDocumentSequence();
                }
                else if (writerType == typeof(FixedDocument))
                {
                    _packagingPolicy.ReleaseXmlWriterForFixedDocument();
                }
                else if (writerType == typeof(FixedPage))
                {
                    _packagingPolicy.ReleaseXmlWriterForFixedPage();
                }
                else if (writerType == typeof(Visual))
                {
                    _packagingPolicy.ReleaseXmlWriterForFixedPage();
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
                }
            }

            //
            // If the subsetting is complete we need to notify the interleaving policy
            // so it can flush.
            //
            if( subsetComplete && refCnt == 0 )
            {
                XpsPackagingPolicy xpsPackagingPolicy = _packagingPolicy as  XpsPackagingPolicy;
                if(xpsPackagingPolicy != null )
                {
                    xpsPackagingPolicy.InterleavingPolicy.SignalSubsetComplete();
                }
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterEnd);
        }

        internal
        override
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType
            )
        {
            XpsResourceStream resourceStream = null;

            if(_packagingPolicy != null)
            {
                if (resourceType == typeof(GlyphRun))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsFont();
                }
                else if (resourceType == typeof(BitmapSource))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsImage(XpsS0Markup.ImageUriPlaceHolder);
                }
                else if (resourceType == typeof(ColorContext))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsColorContext(XpsS0Markup.ColorContextUriPlaceHolder);
                }
                else if (resourceType == typeof(ResourceDictionary))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsResourceDictionary(XpsS0Markup.ResourceDictionaryUriPlaceHolder);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
                }
            }

            return resourceStream;
        }

        internal
        override
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType,
            String  resourceID
            )
        {
            XpsResourceStream resourceStream = null;

            if(_packagingPolicy != null)
            {
                if (resourceType == typeof(GlyphRun))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsFont(resourceID);
                }
                else if (resourceType == typeof(BitmapSource))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsImage(resourceID);
                }
                else if (resourceType == typeof(ColorContext))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsColorContext(resourceID);
                }
                else if (resourceType == typeof(ResourceDictionary))
                {
                    resourceStream = _packagingPolicy.AcquireResourceStreamForXpsResourceDictionary(resourceID);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
                }
            }

            return resourceStream;
        }

        internal
        override
        void
        ReleaseResourceStream(
            Type    resourceType
            )
        {
            if(_packagingPolicy != null)
            {
               if (resourceType == typeof(GlyphRun))
               {
                    _packagingPolicy.ReleaseResourceStreamForXpsFont();
               }
               else if (resourceType == typeof(BitmapSource))
               {
                    _packagingPolicy.ReleaseResourceStreamForXpsImage();
               }
               else if (resourceType == typeof(ColorContext))
               {
                    _packagingPolicy.ReleaseResourceStreamForXpsColorContext();
               }
               else if (resourceType == typeof(ResourceDictionary))
               {
                    _packagingPolicy.ReleaseResourceStreamForXpsResourceDictionary();
               }
               else
               {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
               }
            }
        }

        internal
        override
        void
        ReleaseResourceStream(
            Type    resourceType,
            String  resourceID
            )
        {
            if(_packagingPolicy != null)
            {
                if (resourceType == typeof(GlyphRun))
                {
                    _packagingPolicy.ReleaseResourceStreamForXpsFont(resourceID);
                }
                else if (resourceType == typeof(BitmapSource))
                {
                    _packagingPolicy.ReleaseResourceStreamForXpsImage();
                }
                else if (resourceType == typeof(ColorContext))
                {
                    _packagingPolicy.ReleaseResourceStreamForXpsColorContext();
                }
                else if (resourceType == typeof(ResourceDictionary))
                {
                    _packagingPolicy.ReleaseResourceStreamForXpsResourceDictionary();
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
                }
            }
        }

        internal
        override
        bool
        CanSerializeDependencyProperty(
            Object                      serializableObject,
            TypeDependencyPropertyCache dependencyProperty
            )
        {
            return CanSerializeDependencyProperty(serializableObject, dependencyProperty, _reachSerializationServices);
        }

        internal
        static
        bool
        CanSerializeDependencyProperty(
            Object                      serializableObject,
            TypeDependencyPropertyCache dependencyProperty,
            ReachSerializationServices  reachSerializationServices
            )
        {
            bool canSerialize = false;

            if(serializableObject != null &&
               dependencyProperty != null &&
               ((dependencyProperty.PropertyInfo != null) ||
                (((DependencyProperty)(dependencyProperty.DependencyProperty)).Name != null)))
            {
                String name = (dependencyProperty.PropertyInfo != null) ?
                              dependencyProperty.PropertyInfo.Name :
                              ((DependencyProperty)(dependencyProperty.DependencyProperty)).Name;

                Hashtable dependencyPropertiesTable = (Hashtable)reachSerializationServices.
                                                      TypeSerializableDependencyProperties[serializableObject.GetType()];

                if(dependencyPropertiesTable != null)
                {
                    if(dependencyPropertiesTable.Contains(name))
                    {
                        canSerialize = true;
                    }
                }
                else
                {
                    canSerialize = true;
                }
            }
            else
            {
                canSerialize = true;
            }

            return canSerialize;
        }

        internal
        override
        bool
        CanSerializeClrProperty(
            Object              serializableObject,
            TypePropertyCache   property
            )
        {
            return CanSerializeClrProperty(serializableObject, property, _reachSerializationServices);
        }

        internal
        static
        bool
        CanSerializeClrProperty(
            Object                      serializableObject,
            TypePropertyCache           property,
            ReachSerializationServices  reachSerializationServices
            )
        {
            bool canSerialize = true;

            if (serializableObject != null &&
               property != null &&
               property.PropertyInfo != null)
            {
                String name = property.PropertyInfo.Name;

                Hashtable clrPropertiesTable = (Hashtable)reachSerializationServices.
                                               TypeNoneSerializableClrProperties[serializableObject.GetType()];

                if (clrPropertiesTable != null)
                {
                    if (clrPropertiesTable.Contains(name))
                    {
                        canSerialize = false;
                    }
                }
            }

            return canSerialize;
        }

        /// <summary>
        /// This method adds a relationship to the current active
        /// page using the specified target and relationship name.
        /// </summary>
        /// <param name="targetUri">
        /// Uri to Target for relationship.
        /// </param>
        /// <param name="relationshipName">
        /// Relationship name to add.
        /// </param>
        internal
        override
        void
        AddRelationshipToCurrentPage(
            Uri targetUri,
            string relationshipName
            )
        {
            if (_packagingPolicy != null)
            {
                _packagingPolicy.RelateResourceToCurrentPage(targetUri, relationshipName);
            }
        }

        internal
        void
        RegisterDocumentStart()
        {
            if( _documentStartState )
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_FixedDocumentInDocument));
            }
            if( _pageStartState)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_FixedDocumentInPage));
            }
            //
            // Entering Document  Started state
            //
            _documentStartState = true;
            //
            // Increment the number of documents serialized
            //
            _documentNumber += 1;
            //
            // Clearing the number of pages for this document
            //
            _pageNumber = 0;
        }


        internal
        void
        RegisterDocumentEnd()
        {
            //
            // It is invalid to have a document with no fixed pages
            // If no fixed pages have been searialzed throw
            //
            if( _pageNumber <= 0 )
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPages));
            }
            //
            // Exiting Document  Started state
            //
            _documentStartState = false;
        }

        void
        IXpsSerializationManager.RegisterPageStart()
        {
            if( _pageStartState )
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_FixedPageInPage));
            }
            //
            // Entering Page  Started state
            //
            _pageStartState = true;
            //
            // Increment the number of documents serialized
            //
            _pageNumber += 1;
        }


        void
        IXpsSerializationManager.RegisterPageEnd()
        {
            //
            // Exiting Page  Started state
            //
            _pageStartState = false;
        }

        internal
        void
        RegisterDocumentSequenceStart()
        {
            _documentNumber = 0;
        }


        internal
        void
        RegisterDocumentSequenceEnd()
        {
            //
            // It is invalid to have a document sequence with no fixed documents
            // If no fixed documents have been searialzed throw
            //
            if( _pageNumber <= 0 )
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedDocuments));
            }
        }


        internal
        override
        BasePackagingPolicy
        PackagingPolicy
        {
            get
            {
                return _packagingPolicy;
            }
        }

        internal
        override
        XpsResourcePolicy
        ResourcePolicy
        {
            get
            {
                return _resourcePolicy;
            }
        }

        internal
        ReachHierarchySimulator
        Simulator
        {
            get
            {
                return _simulator;
            }

            set
            {
                _simulator = value;
            }
        }

        internal
        bool
        IsSimulating
        {
            get
            {
                return _isSimulating;
            }

            set
            {
                _isSimulating = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        bool
        IsBatchMode
        {
            get
            {
                return _isBatchMode;
            }
        }

        
        VisualSerializationService
        IXpsSerializationManager.VisualSerializationService
        {
            get
            {
                return _visualSerializationService;
            }
        }

        PrintTicket
        IXpsSerializationManager.FixedPagePrintTicket
        {
            get
            {
                return _fixedPagePrintTicket;
            }

            set
            {
                _fixedPagePrintTicket = value;
            }
        }

        Size
        IXpsSerializationManager.FixedPageSize
        {
            get
            {
                return _fixedPageSize;
            }

            set
            {
                _fixedPageSize = value;
            }
        }

        internal
        bool
        IsSerializedObjectTypeSupported(
            Object serializedObject
            )
        {
            return IsSerializedObjectTypeSupported(serializedObject,_isBatchMode);
        }

        internal
        static
        bool
        IsSerializedObjectTypeSupported(
            Object  serializedObject,
            bool isBatchMode
            )
        {
            bool isSupported = false;

            Type serializedObjectType = serializedObject.GetType();

            if(isBatchMode)
            {
                if((typeof(System.Windows.Media.Visual).IsAssignableFrom(serializedObjectType)) &&
                    (serializedObjectType != typeof(System.Windows.Documents.FixedPage)))
                {
                    isSupported = true;
                }
            }
            else
            {
                if ( (serializedObjectType == typeof(System.Windows.Documents.FixedDocumentSequence)) ||
                     (serializedObjectType == typeof(System.Windows.Documents.FixedDocument))         ||
                     (serializedObjectType == typeof(System.Windows.Documents.FixedPage))             ||
                     (typeof(System.Windows.Media.Visual).IsAssignableFrom(serializedObjectType))     ||
                     (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(serializedObjectType)) )
                {
                    isSupported = true;
                }

            }

            return isSupported;
        }

        internal
        XpsDriverDocEventManager
        GetXpsDriverDocEventManager()
        {
            return _xpsDocEventManager;
        }

        void
        IXpsSerializationManager.OnXPSSerializationPrintTicketRequired(
            object operationState
            )
        {
            XpsSerializationPrintTicketRequiredEventArgs e = operationState as XpsSerializationPrintTicketRequiredEventArgs;

            if(XpsSerializationPrintTicketRequired != null)
            {
                e.Modified = true;

                XpsSerializationPrintTicketRequired(this,e);

                if (XpsSerializationPrintTicketRequiredOnXpsDriverDocEvent != null)
                {
                    XpsSerializationPrintTicketRequiredOnXpsDriverDocEvent(this, e);
                }
            }
        }

        void
        IXpsSerializationManager.OnXPSSerializationProgressChanged(
            object operationState
            )
        {
            XpsSerializationProgressChangedEventArgs e = operationState as XpsSerializationProgressChangedEventArgs;

            if(XpsSerializationProgressChanged != null)
            {
                XpsSerializationProgressChanged(this,e);
            }
        }

        internal
        void
        OnXpsDriverDocEvent(
            XpsSerializationXpsDriverDocEventArgs e
            )
        {
            if (XpsSerializationXpsDriverDocEvent != null)
            {
                XpsSerializationXpsDriverDocEvent(this, e);
            }
        }

        private
        BasePackagingPolicy         _packagingPolicy;

        private
        XpsResourcePolicy           _resourcePolicy;

        private
        ReachSerializationServices  _reachSerializationServices;

        private
        VisualSerializationService  _visualSerializationService;

        private
        bool                        _isBatchMode;

        private
        bool                        _isSimulating;

        private
        ReachHierarchySimulator     _simulator;

        private
        int                         _currentDocumentSequenceWriterRef;

        private
        int                         _currentFixedDocumentWriterRef;

        private
        int                         _currentFixedPageWriterRef;

        //
        //  This counter is used to ensure at least one document is added
        //
        private
        int                         _documentNumber;

        //
        //  This counter is used to ensure at least one page is added
        //  This is reset by a document completing
        //
        private
        int                         _pageNumber;

        //
        //  This flag is used to test if in document serialization
        //  This is to prevent writing of a document in a document
        //
        private
        bool                        _documentStartState;

        //
        //  This flag is used to test if in page serialization
        //  This is to prevent writing of a page in a page
        //
        private
        bool                        _pageStartState;

        //
        // This print ticket is cached when we get it inside of the Page Content Serializer
        // and we need to retrieve it inside of the FixedPage Serializer
        //
        private
        PrintTicket        _fixedPagePrintTicket;

        private
        Size                _fixedPageSize = new Size(816,1056);

        private
        XpsDriverDocEventManager    _xpsDocEventManager;

        internal const string NullString   = "*null";
        internal const string TypeOfString = "{x:Type ";
    };

    internal class VisualSerializationService
    {
        public
        VisualSerializationService(
            PackageSerializationManager   serializationManager
            )
        {
            _serializationManager = serializationManager;
            _visualTreeFlattener = null;
        }


        public
        VisualTreeFlattener
        AcquireVisualTreeFlattener(
            XmlWriter resWriter,
            XmlWriter bodyWriter,
            Size pageSize
            )
        {
            if(_visualTreeFlattener == null)
            {
                _visualTreeFlattener = new VisualTreeFlattener(resWriter,
                                                               bodyWriter,
                                                               _serializationManager,
                                                               pageSize,
                                                               new TreeWalkProgress());
            }

            return _visualTreeFlattener;
        }

        public
        void
        ReleaseVisualTreeFlattener(
            )
        {
            _visualTreeFlattener = null;
        }

        private
        VisualTreeFlattener         _visualTreeFlattener;

        private
        PackageSerializationManager   _serializationManager;
    };


    internal class ReachSerializationServices
    {
        #region Constructor

        public
        ReachSerializationServices(
            )
        {
            _typesXmlNSMapping                    = null;
            _typeSerializableDependencyProperties = null;
            _typeNoneSerializableClrProperties    = null;
        }

        #endregion Constructor

        #region Public Methods

        public
        void
        RegisterNameSpacesForTypes(
            )
        {
            if(_typesXmlNSMapping == null)
            {
                _typesXmlNSMapping = new Hashtable(11);

                for(int IndexInTypes = 0;
                    IndexInTypes < ReachSerializationServices._xpsTypesRequiringXMLNS.Length;
                    IndexInTypes++)
                {
                    _typesXmlNSMapping[ReachSerializationServices._xpsTypesRequiringXMLNS[IndexInTypes]] =
                    XpsS0Markup.XmlnsUri[IndexInTypes];
                }
            }
        }

        public
        void
        RegisterSerializableDependencyPropertiesForReachTypes(
            )
        {
            if(_typeSerializableDependencyProperties == null)
            {
                _typeSerializableDependencyProperties = new Hashtable(11);
                //
                // We create a separate hashtable for each set of properties
                // belonging to a certain type and save those with their
                // corresponding type in the master hashtable.
                // 1. FixedPage Properties
                //
                Hashtable fixedPageDependencyProperties = new Hashtable(11);

                for(int numberOfDependencyPropertiesInFixedPage = 0;
                    numberOfDependencyPropertiesInFixedPage < _fixedPageDependencyProperties.Length;
                    numberOfDependencyPropertiesInFixedPage++)
                {
                    fixedPageDependencyProperties.
                    Add(_fixedPageDependencyProperties[numberOfDependencyPropertiesInFixedPage],
                        null);
                }

                _typeSerializableDependencyProperties[typeof(System.Windows.Documents.FixedPage)] =
                fixedPageDependencyProperties;

                //
                // 2.FixedDocument Properties
                //
                Hashtable fixedDocumentDependencyProperties = new Hashtable(11);

                for(int numberOfDependencyPropertiesInFixedDocument = 0;
                    numberOfDependencyPropertiesInFixedDocument < _fixedDocumentDependencyProperties.Length;
                    numberOfDependencyPropertiesInFixedDocument++)
                {
                    fixedDocumentDependencyProperties.
                    Add(_fixedDocumentDependencyProperties[numberOfDependencyPropertiesInFixedDocument],
                        null);
                }

                _typeSerializableDependencyProperties[typeof(System.Windows.Documents.FixedDocument)] =
                fixedDocumentDependencyProperties;

                //
                // 2.FixedDocumentSequence Properties
                //
                Hashtable fixedDocumentSequenceDependencyProperties = new Hashtable(11);

                for(int numberOfDependencyPropertiesInFixedDS = 0;
                    numberOfDependencyPropertiesInFixedDS < _fixedDocumentSequenceDependencyProperties.Length;
                    numberOfDependencyPropertiesInFixedDS++)
                {
                    fixedDocumentSequenceDependencyProperties.
                    Add(_fixedDocumentSequenceDependencyProperties[numberOfDependencyPropertiesInFixedDS],
                        null);
                }

                _typeSerializableDependencyProperties[typeof(System.Windows.Documents.FixedDocumentSequence)] =
                fixedDocumentSequenceDependencyProperties;
            }
        }

        public
        void
        RegisterNoneSerializableClrPropertiesForReachTypes(
            )
        {
            if(_typeNoneSerializableClrProperties == null)
            {
                _typeNoneSerializableClrProperties = new Hashtable(11);
                //
                // We create a separate hashtable for each set of excluded
                // propertiesbelonging to a certain type and save those with
                // their corresponding type in the master hashtable.
                // 1.FixedDocument Excluded Properties
                //
                Hashtable fixedDocumentExcludedClrProperties = new Hashtable(11);

                for(int numberOfClrPropertiesInFixedDocument = 0;
                    numberOfClrPropertiesInFixedDocument < _fixedDocumentExcludedClrProperties.Length;
                    numberOfClrPropertiesInFixedDocument++)
                {
                    fixedDocumentExcludedClrProperties.
                    Add(_fixedDocumentExcludedClrProperties[numberOfClrPropertiesInFixedDocument],
                        null);
                }

                _typeNoneSerializableClrProperties[typeof(System.Windows.Documents.FixedDocument)] =
                fixedDocumentExcludedClrProperties;

                //
                // 2.FixedDocumentSequence Excluded Properties
                //
                Hashtable fixedDocumentSequenceExcludedClrProperties = new Hashtable(11);

                for(int numberOfClrPropertiesInFixedDS = 0;
                    numberOfClrPropertiesInFixedDS < _fixedDocumentSequenceExcludedClrProperties.Length;
                    numberOfClrPropertiesInFixedDS++)
                {
                    fixedDocumentSequenceExcludedClrProperties.
                    Add(_fixedDocumentSequenceExcludedClrProperties[numberOfClrPropertiesInFixedDS],
                        null);
                }

                _typeNoneSerializableClrProperties[typeof(System.Windows.Documents.FixedDocumentSequence)] =
                fixedDocumentSequenceExcludedClrProperties;
            }
        }



        #endregion Public Methods

        #region Public Properties

        public
        Hashtable
        TypesXmlNSMapping
        {
            get
            {
                return _typesXmlNSMapping;
            }
        }

        public
        IDictionary
        TypeSerializableDependencyProperties
        {
            get
            {
                return _typeSerializableDependencyProperties;
            }
        }

        public
        IDictionary
        TypeNoneSerializableClrProperties
        {
            get
            {
                return _typeNoneSerializableClrProperties;
            }
        }

        #endregion Public Properties

        #region Private Data

        private
        Hashtable       _typesXmlNSMapping;

        private
        IDictionary     _typeSerializableDependencyProperties;

        private
        IDictionary     _typeNoneSerializableClrProperties;

        static
        private
        Type[]          _xpsTypesRequiringXMLNS =
        {
            typeof(FixedDocumentSequence),
            typeof(FixedDocument),
            typeof(FixedPage)
        };

        private
        string[]        _fixedPageDependencyProperties =
        {
            "Width",
            "Height",
            "ContentBox",
            "BleedBox",
            "Name",
            "PrintTicket"
        };


        private
        string[]        _fixedDocumentDependencyProperties =
        {
            "PageWidth",
            "PageHeight"
        };

        private
        string[]        _fixedDocumentSequenceDependencyProperties =
        {
            "PageWidth",
            "PageHeight"
        };

        private
        string[]        _fixedDocumentExcludedClrProperties =
        {
            "IsBackgroundPaginationEnabled",
            "PageSize"
        };

        private
        string[]        _fixedDocumentSequenceExcludedClrProperties =
        {
            "IsBackgroundPaginationEnabled",
            "PageSize"
        };

        #endregion Private Data
    };

}

