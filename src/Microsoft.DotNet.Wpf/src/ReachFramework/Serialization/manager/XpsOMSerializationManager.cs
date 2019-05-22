// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using MS.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using System.Xml;


#pragma warning disable 1634, 1691 //Allows suppression of certain PreSharp messages

namespace System.Windows.Xps.Serialization
{
    internal class XpsOMSerializationManager : 
                          PackageSerializationManager,
                          IXpsSerializationManager        
    {
        #region Constructor
        public 
        XpsOMSerializationManager(
            XpsOMPackagingPolicy xpsOMManager, 
            bool batchMode
            ):
        base()
        {
            _packagingPolicy = xpsOMManager;
            _isBatchMode = batchMode;

            _reachSerializationServices = new ReachSerializationServices();
            _reachSerializationServices.RegisterNameSpacesForTypes();
            _reachSerializationServices.RegisterSerializableDependencyPropertiesForReachTypes();
            _reachSerializationServices.RegisterNoneSerializableClrPropertiesForReachTypes();
            _visualSerializationService = new VisualSerializationService(this);

            XpsResourcePolicy resourcePolicy = new XpsResourcePolicy(XpsResourceSharing.NoResourceSharing);
            resourcePolicy.RegisterService(new XpsImageSerializationService(), typeof(XpsImageSerializationService));
            resourcePolicy.RegisterService(new XpsFontSerializationService(_packagingPolicy), typeof(XpsFontSerializationService));
            _resourcePolicy = resourcePolicy;

            SetFontSubsettingPolicy(FontSubsetterCommitPolicies.CommitPerPage);
            SetFontSubsettingCountPolicy(4);
        }

        #endregion Constructor

        #region PackageSerializationManager override
        public
        override
        void
        SaveAsXaml(
            object serializedObject
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSaveXpsBegin);

            if (_packagingPolicy.IsValid)
            {

                XmlWriter pageWriter = null;

                if (serializedObject == null)
                {
                    throw new ArgumentNullException(nameof(serializedObject));
                }

                if (!XpsSerializationManager.IsSerializedObjectTypeSupported(serializedObject, _isBatchMode))
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
                }

                if (serializedObject is DocumentPaginator)
                {
                    // Prefast complains that serializedObject is not tested for null
                    // It is tested a few lines up
                    #pragma warning suppress 56506
                    if ((serializedObject as DocumentPaginator).Source is FixedDocument &&
                        serializedObject.GetType().ToString().Contains("FixedDocumentPaginator"))
                    {
                        serializedObject = (serializedObject as DocumentPaginator).Source;
                    }
                    else
                        if ((serializedObject as DocumentPaginator).Source is FixedDocumentSequence &&
                            serializedObject.GetType().ToString().Contains("FixedDocumentSequencePaginator"))
                        {
                            serializedObject = (serializedObject as DocumentPaginator).Source;
                        }
                }

                if (_simulator == null)
                {
                    _simulator = new XpsOMHierarchySimulator(this,
                                                             serializedObject);

                }

                if (!_isSimulating)
                {
                    _simulator.BeginConfirmToXPSStructure(_isBatchMode);
                    _isSimulating = true;
                }

                if (_isBatchMode)
                {
                    pageWriter = _simulator.SimulateBeginFixedPage();
                }

                ReachSerializer reachSerializer = GetSerializer(serializedObject);

                if (reachSerializer != null)
                {
                
                     //Things that need to be done at this stage
                     //1. Setting the stack context
                     //2. Setting the root of the graph for future references
                
                    reachSerializer.SerializeObject(serializedObject);

                    if (_isBatchMode)
                    {
                        _simulator.SimulateEndFixedPage(pageWriter);
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

            }
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSaveXpsEnd);
        }

        internal
        override
        Type
        GetSerializerType(
            Type objectType
            )
        {
            Type serializerType = null;


            if ((serializerType = base.GetSerializerType(objectType)) == null)
            {
                if (typeof(System.Windows.Documents.FixedDocument).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(XpsOMFixedDocumentSerializer);
                }
                else if (typeof(System.Windows.Documents.PageContentCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachPageContentCollectionSerializer);
                }
                else if (typeof(System.Windows.Documents.PageContent).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachPageContentSerializer);
                }
                else if (typeof(System.Windows.Controls.UIElementCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(ReachUIElementCollectionSerializer);
                }
                else if (typeof(System.Windows.Documents.FixedPage).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(XpsOMFixedPageSerializer);
                }
                else if (typeof(System.Windows.Documents.FixedDocumentSequence).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(XpsOMDocumentSequenceSerializer);
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
                    serializerType = typeof(XpsOMDocumentPaginatorSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentPage).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(XpsOMDocumentPageSerializer);
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
        GetTypeConverter(
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
        GetTypeConverter(
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
        bool
        CanSerializeDependencyProperty(
            Object serializableObject,
            TypeDependencyPropertyCache dependencyProperty
            )
        {
            return XpsSerializationManager.CanSerializeDependencyProperty(serializableObject, dependencyProperty, _reachSerializationServices);
        }

        internal
        override
        bool
        CanSerializeClrProperty(
            Object serializableObject,
            TypePropertyCache property
            )
        {
            return XpsSerializationManager.CanSerializeClrProperty(serializableObject, property, _reachSerializationServices);
        }
        
        internal
        override
        string
        GetXmlNSForType(
            Type objectType
            )
        {
            return (string)_reachSerializationServices.TypesXmlNSMapping[objectType];
        }

        internal override Xml.XmlWriter AcquireXmlWriter(Type writerType)
        {
            XmlWriter xmlWriter = null;
            if (_packagingPolicy != null)
            {
                if (writerType == typeof(FixedPage))
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

        internal
        void
        signalReleaseToFontService(
            Type writerType
            )
        {
            bool subsetComplete = false;
            int refCnt = DecrementRefCntByType(writerType);

            // signal the font sub-setter that we completed a node
            if (_resourcePolicy != null)
            {
                XpsFontSerializationService fontService = (XpsFontSerializationService)_resourcePolicy.GetService(typeof(XpsFontSerializationService));
                //
                // The font subsetter will determine if based on this
                // signal we have completed a subset
                //
                if (fontService != null && refCnt == 0)
                {
                    subsetComplete = fontService.SignalCommit(writerType);
                }
            }
        }

        internal
        void
        ReleaseXpsOMWriterForFixedDocumentSequence()
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterStart);
            signalReleaseToFontService(typeof(FixedDocumentSequence));

            _packagingPolicy?.CloseXpsOMPackageWriter();
                
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterEnd);
        }

        internal
        void
        ReleaseXpsOMWriterForFixedDocument()
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterStart);
            signalReleaseToFontService(typeof(FixedDocument));

            _packagingPolicy?.ReleaseXpsOMWriterForFixedDocument();

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterEnd);
        }

        internal
        override
        void
        ReleaseXmlWriter(
            Type writerType
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterStart);

            signalReleaseToFontService(writerType);

            //
            // Allow the packaging policy to release the stream
            //
            if (_packagingPolicy != null)
            {
                if (writerType == typeof(FixedPage))
                {
                    _packagingPolicy.ReleaseXmlWriterForFixedPage();
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
                }
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXReleaseWriterEnd);
        }

        internal
        override
        XpsResourceStream
        AcquireResourceStream(
            Type resourceType
            )
        {
            throw new NotImplementedException();
        }

        internal 
        override
        XpsResourceStream
        AcquireResourceStream(
            Type resourceType,
            string resourceID
            )
        {
            XpsResourceStream resourceStream = null;

            if (_packagingPolicy != null)
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
            Type resourceType
            )
        {
            if (_packagingPolicy != null)
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

        /// <summary>
        /// We must implement this method from abstract class PackageSerializationManager, it is
        /// actually not used during printing/serialization, so it should throw if it is ever called
        /// </summary>
        internal
        override
        void
        ReleaseResourceStream(
            Type resourceType,
            string resourceID
            )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// It is no longer necessary to add the relationships explicitly since XpsOM will 
        /// do it automatically, however, the serialization engine will still call into this 
        /// method, so just no-op and continue.
        /// </summary>
        internal
        override
        void
        AddRelationshipToCurrentPage(
            Uri targetUri,
            string relationshipName
            )
        {
            return;
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
        
        #endregion PackageSerializationManager override

        #region Private methods

        /// <summary>
        /// Sets the Font subsetting policy on the
        /// Font Subset service
        /// </summary>
        private
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

        internal
        virtual
        void
        Commit()
        {
            if (_isBatchMode && _isSimulating && _packagingPolicy.IsValid)
            {
                _simulator.EndConfirmToXPSStructure(_isBatchMode);
            }
        }


        /// <summary>
        /// Sets the Font subsetting count policy on the
        /// Font Subset service.  This is used to determine
        /// how many page or document signals are required before
        /// the subsetted font is flushed
        /// </summary>
        private
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

        private
        int
        DecrementRefCntByType(Type writerType)
        {
            int refCnt = 0;
            if (writerType == typeof(FixedDocumentSequence))
            {
                refCnt = --_currentDocumentSequenceWriterRef;
            }
            else if (writerType == typeof(FixedDocument))
            {
                refCnt = --_currentFixedDocumentWriterRef;
            }
            else if (writerType == typeof(FixedPage))
            {
                refCnt = --_currentFixedPageWriterRef;
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }
            return refCnt;
        }

        #endregion Private methods

        #region Internal methods
        
        internal
        void
        EnsureXpsOMPackageWriter()
        {
            _currentDocumentSequenceWriterRef++;
            _packagingPolicy.EnsureXpsOMPackageWriter();
        }

        internal
        void
        StartNewDocument()
        {
            _currentFixedDocumentWriterRef++;
            _packagingPolicy.StartNewDocument();
        }

        internal
        void
        OnXPSSerializationPrintTicketRequired(
            object operationState
            )
        {
            XpsSerializationPrintTicketRequiredEventArgs e = operationState as XpsSerializationPrintTicketRequiredEventArgs;

            if (XpsSerializationPrintTicketRequired != null)
            {
                e.Modified = true;

                XpsSerializationPrintTicketRequired(this, e);
            }
        }

        internal
        void
        OnXPSSerializationProgressChanged(
            object operationState
            )
        {
            XpsSerializationProgressChangedEventArgs e = operationState as XpsSerializationProgressChangedEventArgs;

            if (XpsSerializationProgressChanged != null)
            {
                XpsSerializationProgressChanged(this, e);
            }
        }

        internal
        void 
        RegisterPageStart()
        {
            if (_pageStartState)
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

        internal
        void
        RegisterPageEnd()
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
            if (_documentNumber <= 0)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedDocuments));
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
            if (_pageNumber <= 0)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPages));
            }
            //
            // Exiting Document  Started state
            //
            _documentStartState = false;
        }

        #endregion Internal methods

        #region IXpsSerializationManager implementation

        void
        IXpsSerializationManager.OnXPSSerializationPrintTicketRequired(
            object operationState
            )
        {
            OnXPSSerializationPrintTicketRequired(operationState);
        }

        void
        IXpsSerializationManager.OnXPSSerializationProgressChanged(
            object operationState
            )
        {
            OnXPSSerializationProgressChanged(operationState);
        }

        void
        IXpsSerializationManager.RegisterPageStart()
        {
            RegisterPageStart();
        }

        void
        IXpsSerializationManager.RegisterPageEnd()
        {
            RegisterPageEnd();
        }

        PrintTicket
        IXpsSerializationManager.FixedPagePrintTicket
        {
            get
            {
                return FixedPagePrintTicket;
            }

            set
            {
                FixedPagePrintTicket = value;
            }
        }

        Size
        IXpsSerializationManager.FixedPageSize
        {
            get
            {
                return FixedPageSize;
            }

            set
            {
                FixedPageSize = value;
            }
        }


        VisualSerializationService
        IXpsSerializationManager.VisualSerializationService
        {
            get
            {
                return VisualSerializationService;
            }
        }

        #endregion IXpsSerializationManager properties

        #region internal properties

        internal
        PrintTicket
        FixedPagePrintTicket
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

        internal
        Size
        FixedPageSize
        {
            get
            {
                return _fixedPageSize;
            }

            set
            {
                _fixedPageSize = value;
                _packagingPolicy.FixedPageSize = _fixedPageSize;
            }
        }

        internal
        VisualSerializationService
        VisualSerializationService
        {
            get
            {
                return _visualSerializationService;
            }
        }

        /// <summary>
        /// Gets/Sets the hierarchy simulator used by this manager
        /// </summary>
        internal
        XpsOMHierarchySimulator
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

        /// <summary>
        /// Gets/sets whether Xps hierachy simulation was necessary for this manager
        /// </summary>
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
        /// Returns whether this manager is accepting multiple SaveAsXaml calls
        /// to serialize each Visual individually, this is done via VisualsToXpsDocument
        /// </summary>
        internal
        bool
        IsBatchMode
        {
            get
            {
                return _isBatchMode;
            }
        }

        #endregion internal properties

        #region Private members

        private
        XpsOMPackagingPolicy _packagingPolicy;

        private
        bool _isBatchMode;

        private
        XpsOMHierarchySimulator _simulator;

        private
        bool _isSimulating;


        private
        int _currentDocumentSequenceWriterRef;

        private
        int _currentFixedDocumentWriterRef;

        private
        int _currentFixedPageWriterRef;

        //
        //  This counter is used to ensure at least one document is added
        //
        private
        int _documentNumber;

        //
        //  This counter is used to ensure at least one page is added
        //  This is reset by a document completing
        //
        private
        int _pageNumber;

        //
        //  This flag is used to test if in document serialization
        //  This is to prevent writing of a document in a document
        //
        private
        bool _documentStartState;

        //
        //  This flag is used to test if in page serialization
        //  This is to prevent writing of a page in a page
        //
        private
        bool _pageStartState;

        //
        // This print ticket is cached when we get it inside of the Page Content Serializer
        // and we need to retrieve it inside of the FixedPage Serializer
        //
        private
        PrintTicket _fixedPagePrintTicket;

        private
        Size _fixedPageSize = new Size(816, 1056);

        private
        XpsResourcePolicy _resourcePolicy;

        private
        ReachSerializationServices _reachSerializationServices;

        private 
        VisualSerializationService _visualSerializationService;

        #endregion Private members

        #region internal events

        internal
        event
        XpsSerializationPrintTicketRequiredEventHandler XpsSerializationPrintTicketRequired;

        internal
        event
        XpsSerializationProgressChangedEventHandler XpsSerializationProgressChanged;

        #endregion

    }
}
