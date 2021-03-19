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
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;
using System.Printing;
using MS.Utility;

using Microsoft.Internal.AlphaFlattener;
//
// Ngc = Next Generation Converter. It means to convert the avalon element tree
//  to the downlevel GDI primitives.
//
namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// NgcSerializationManager is a class to help print avalon content (element tree) to traditional printer
    /// </summary>
    //[CLSCompliant(false)]
    internal sealed class NgcSerializationManager :
                          PackageSerializationManager
    {
        #region Constructor
        /// <summary>
        /// This constructor take PrintQueue parameter
        /// </summary>
        /// <exception cref="ArgumentNullException">queue is NULL.</exception>
        public
        NgcSerializationManager(
            PrintQueue   queue,
            bool         isBatchMode
            ):
        base()
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }
            _printQueue                 = queue;
            this._isBatchMode           = isBatchMode;
            this._isSimulating          = false;
            this._printTicketManager    = new NgcPrintTicketManager(_printQueue);
        }
        #endregion Construtor

        #region PackageSerializationManager override
        /// <summary>
        /// The function will serializer the avalon content to the printer spool file.
        /// SaveAsXaml is not a propriate name. Maybe it should be "Print"
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

            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }

            if(!IsSerializedObjectTypeSupported(serializedObject))
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }

            if(_isBatchMode && !_isSimulating)
            {
                XpsSerializationPrintTicketRequiredEventArgs printTicketEvent =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentSequencePrintTicket,
                                                                 0);
                OnNGCSerializationPrintTicketRequired(printTicketEvent);

                StartDocument(null,true);
                _isSimulating = true;
            }

            if(_isBatchMode)
            {
                StartPage();
            }

            if(!_isBatchMode &&
               IsDocumentSequencePrintTicketRequired(serializedObject))
            {
                XpsSerializationPrintTicketRequiredEventArgs printTicketEvent =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentSequencePrintTicket,
                                                                 0);
                OnNGCSerializationPrintTicketRequired(printTicketEvent);
            }


            ReachSerializer reachSerializer = GetSerializer(serializedObject);


            if(reachSerializer != null)
            {
                //
                // Call top-level type serializer, it will walk through the contents and
                // all CLR and DP properties of the object and invoke the proper serializer
                // and typeconverter respectively
                //

                reachSerializer.SerializeObject(serializedObject);

                if(_isBatchMode)
                {
                    EndPage();
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
        void
        Cancel(
            )
        {
            if(_startDocCnt != 0 )
            {
                _device.AbortDocument();
                _startDocCnt = 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        void
        Commit(
            )
        {
            if(_isBatchMode && _isSimulating)
            {
                EndDocument();
            }
            else
            {
                //
                // throw the appropariate exception
                //
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
        /// This function GetXmlNSForType and the other four XML writer functiosn (like AcquireXmlWriter)
        /// and the two stream function (like ReleaseResourceStream) should be removed from
        /// the base PackageSerializationManager class.
        /// </summary>
        internal
        override
        String
        GetXmlNSForType(
            Type    objectType
            )
        {
            return null;
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
                if (typeof(System.Windows.Documents.FixedDocumentSequence).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NgcDocumentSequenceSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentReferenceCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NgcDocumentReferenceCollectionSerializer);
                }
                else if (typeof(System.Windows.Documents.FixedDocument).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NgcFixedDocumentSerializer);
                }
                else if(typeof(System.Windows.Documents.PageContentCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NGCReachPageContentCollectionSerializer);
                }
                else if(typeof(System.Windows.Documents.PageContent).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NGCReachPageContentSerializer);
                }
                else if(typeof(System.Windows.Controls.UIElementCollection).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NGCReachUIElementCollectionSerializer);
                }
                else if(typeof(System.Windows.Documents.FixedPage).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NgcFixedPageSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NgcDocumentPaginatorSerializer);
                }
                else if (typeof(System.Windows.Documents.DocumentPage).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NgcDocumentPageSerializer);
                }
                else if (typeof(System.Windows.Media.Visual).IsAssignableFrom(objectType))
                {
                    serializerType = typeof(NgcReachVisualSerializer);
                }


            }

            return serializerType;
        }

        internal
        override
        XmlWriter
        AcquireXmlWriter(
            Type    writerType
            )
        {
            return null;
        }

        internal
        override
        void
        ReleaseXmlWriter(
            Type    writerType
            )
        {
            return;
        }

        internal
        override
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType
            )
        {
            return null;
        }

        internal
        override
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType,
            String  resourceID
            )
        {
            return null;
        }

        internal
        override
        void
        ReleaseResourceStream(
            Type    resourceType
            )
        {
            return;
        }

        internal
        override
        void
        ReleaseResourceStream(
            Type    resourceType,
            String  resourceID
            )
        {
            return;
        }

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
        XpsResourcePolicy
        ResourcePolicy
        {
            get
            {
                return null;
            }
        }

        internal
        override
        BasePackagingPolicy
        PackagingPolicy
        {
            get
            {
                return null;
            }
        }

        #endregion PackageSerializationManager override

        #region Internal Properties
        /// <summary>
        ///
        /// </summary>
        internal
        PrintQueue
        PrintQueue
        {
            get
            {
                return _printQueue;
            }
        }

        internal
        String
        JobName
        {
            set
            {
                if (_jobName == null)
                {
                    _jobName = value;
                }
            }

            get
            {
                return _jobName;
            }
        }

        internal
        Size
        PageSize
        {
            set
            {
                _pageSize = value;
            }
            get
            {
                return _pageSize;
            }
        }

        #endregion Internal Properties

        #region Internal Methods
        internal
        void
        StartDocument(
            Object o,
            bool   documentPrintTicketRequired
            )
        {
            if(documentPrintTicketRequired)
            {
                XpsSerializationPrintTicketRequiredEventArgs e =
                    new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentPrintTicket,
                                                                     0);
                OnNGCSerializationPrintTicketRequired(e);
            }

            if( _startDocCnt == 0 )
            {
                JobName = _printQueue.CurrentJobSettings.Description;

                if(JobName == null)
                {
                    JobName = NgcSerializerUtil.InferJobName(o);
                }

                _device  = new MetroToGdiConverter(PrintQueue);

                if (!_isSimulating)
                {
                    JobIdentifier = _device.StartDocument(_jobName, _printTicketManager.ConsumeActivePrintTicket(true));
                }
            }

            _startDocCnt++;
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        EndDocument()
        {
            if( _startDocCnt == 1 )
            {
                _device.EndDocument();

                //
                // Inform any potential listeners that the doucment has been printed
                //
                XpsSerializationProgressChangedEventArgs e =
                new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                             0,
                                                             0,
                                                             null);
                OnNGCSerializationProgressChagned(e);
            }
            _startDocCnt--;
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        bool
        StartPage()
        {
            bool    bManulStartDoc = false;

            if (_startDocCnt == 0)
            {
                StartDocument(null,true);
                bManulStartDoc = true;
            }

            if(!_isStartPage)
            {
                if (_isPrintTicketMerged == false)
                {
                    XpsSerializationPrintTicketRequiredEventArgs e =
                    new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedPagePrintTicket,
                                                                     0);
                    OnNGCSerializationPrintTicketRequired(e);
                }

                _device.StartPage(_printTicketManager.ConsumeActivePrintTicket(true));
                _isStartPage = true;
            }

            return bManulStartDoc;
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        EndPage()
        {
            if (_isStartPage)
            {
                try
                {
                    _device.FlushPage();
                }
                catch (PrintingCanceledException )
                {
                    _device.EndDocument();
                    throw;
                }
            }

            _isStartPage = false;
            _isPrintTicketMerged = false;

            //
            // Inform any potential listeners that the page has been printed
            //
            XpsSerializationProgressChangedEventArgs e =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedPageWritingProgress,
                                                         0,
                                                         0,
                                                         null);
            OnNGCSerializationProgressChagned(e);
        }

        internal
        void
        OnNGCSerializationPrintTicketRequired(
            object operationState
            )
        {
            XpsSerializationPrintTicketRequiredEventArgs e = operationState as XpsSerializationPrintTicketRequiredEventArgs;

            if(XpsSerializationPrintTicketRequired != null)
            {
                e.Modified = true;

                if (e.PrintTicketLevel == PrintTicketLevel.FixedPagePrintTicket)
                {
                    _isPrintTicketMerged = true;
                }

                XpsSerializationPrintTicketRequired(this,e);

                _printTicketManager.ConstructPrintTicketTree(e);
            }
        }

        internal
        void
        OnNGCSerializationProgressChagned(
            object operationState
            )
        {
            XpsSerializationProgressChangedEventArgs e = operationState as XpsSerializationProgressChangedEventArgs;

            if(XpsSerializationProgressChanged != null)
            {
                XpsSerializationProgressChanged(this,e);
            }
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        WalkVisual(
            Visual visual
            )
        {
            bool    bManulStartDoc = false;
            bool    bManulStartpage = false;

            if (_startDocCnt == 0)
            {
                StartDocument(visual,true);
                bManulStartDoc = true;
            }
            if (!_isStartPage)
            {
                StartPage();
                bManulStartpage = true;
            }

            //
            // Call VisualTreeFlattener to flatten the visual on IMetroDrawingContext
            //
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXLoadPrimitiveBegin);

            VisualTreeFlattener.Walk(visual, _device, PageSize, new TreeWalkProgress());

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXLoadPrimitiveEnd);

            if (bManulStartpage)
            {
                EndPage();
            }
            if (bManulStartDoc)
            {
                EndDocument();
            }
        }

        internal
        PrintTicket
        GetActivePrintTicket()
        {
            return _printTicketManager.ActivePrintTicket;
        }

        internal
        bool
        IsPrintTicketEventHandlerEnabled
        {
            get
            {
                if(XpsSerializationPrintTicketRequired!=null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private
        bool
        IsSerializedObjectTypeSupported(
            Object  serializedObject
            )
        {
            bool isSupported = false;

            Type serializedObjectType = serializedObject.GetType();

            if(_isBatchMode)
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
                     (serializedObjectType == typeof(System.Windows.Documents.FixedDocument))    ||
                     (serializedObjectType == typeof(System.Windows.Documents.FixedPage))        ||
                     (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(serializedObjectType)) ||
                     (typeof(System.Windows.Media.Visual).IsAssignableFrom(serializedObjectType)) )
                {
                    isSupported = true;
                }
            }

            return isSupported;
        }

        private
        bool
        IsDocumentSequencePrintTicketRequired(
            Object  serializedObject
            )
        {
            bool isRequired = false;

            Type serializedObjectType = serializedObject.GetType();

            if ((serializedObjectType == typeof(System.Windows.Documents.FixedDocument))    ||
                (serializedObjectType == typeof(System.Windows.Documents.FixedPage))        ||
                (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(serializedObjectType)) ||
                (typeof(System.Windows.Media.Visual).IsAssignableFrom(serializedObjectType)) )
            {
                isRequired = true;
            }

            return isRequired;
        }

        #endregion Internal Methods

        #region Private Member
        private     PrintQueue             _printQueue;
        private     int                    _startDocCnt;
        private     bool                   _isStartPage;
        private     MetroToGdiConverter    _device;
        private     String                 _jobName;
        private     bool                   _isBatchMode;
        private     bool                   _isSimulating;
        private     NgcPrintTicketManager  _printTicketManager;
        private     bool                   _isPrintTicketMerged;
        private     Size                    _pageSize = new Size(816,1056);
        #endregion Private Member
    };

    internal sealed class NgcPrintTicketManager
    {
        #region constructor

        public
        NgcPrintTicketManager(
            PrintQueue printQueue
            )
        {
            this._printQueue        = printQueue;
            this._fdsPrintTicket    = null;
            this._fdPrintTicket     = null;
            this._fpPrintTicket     = null;
            this._rootPrintTicket   = null;
            this._activePrintTicket = null;
            this._isActiveUpdated   = false;

            this.m_validatedPrintTicketCache = new MS.Internal.Printing.MostFrequentlyUsedCache<string, PrintTicket>(s_PrintTicketCacheMaxCount);
        }

        #endregion constructor

        #region Public Methods

        public
        void
        ConstructPrintTicketTree(
            XpsSerializationPrintTicketRequiredEventArgs    args
            )
        {
            switch(args.PrintTicketLevel)
            {
                case PrintTicketLevel.FixedDocumentSequencePrintTicket:
                {
                    if(args.PrintTicket != null)
                    {
                        _fdsPrintTicket  = args.PrintTicket;
                        _rootPrintTicket = _fdsPrintTicket;
                    }
                    else
                    {
                        _rootPrintTicket = new PrintTicket(_printQueue.UserPrintTicket.GetXmlStream());
                    }

                    _activePrintTicket = _rootPrintTicket;
                    _isActiveUpdated   = true;

                    break;
                }

                case PrintTicketLevel.FixedDocumentPrintTicket:
                {
                    if(args.PrintTicket != null)
                    {
                        _fdPrintTicket = args.PrintTicket;

                        if(_fdsPrintTicket!=null)
                        {
                            //
                            // we have to merge the 2 print tickets
                            //
                            _rootPrintTicket = _printQueue.
                                               MergeAndValidatePrintTicket(_fdsPrintTicket, _fdPrintTicket).
                                               ValidatedPrintTicket;
                        }
                        else
                        {
                            _rootPrintTicket = _fdPrintTicket;
                        }
                    }
                    else
                    {
                        _fdPrintTicket   = null;
                        _rootPrintTicket = _fdsPrintTicket;
                    }

                    if(_rootPrintTicket == null)
                    {
                        _rootPrintTicket = new PrintTicket(_printQueue.UserPrintTicket.GetXmlStream());
                    }

                    _activePrintTicket = _rootPrintTicket;
                    _isActiveUpdated   = true;

                    break;
                }

                case PrintTicketLevel.FixedPagePrintTicket:
                {
                    if(args.PrintTicket != null)
                    {
                        _fpPrintTicket = args.PrintTicket;

                        // The NULL check might not be needed but just in case...
                        String printTicketPairXMLStr =
                            (_rootPrintTicket == null ? "" : _rootPrintTicket.ToXmlString()) +
                            (_fpPrintTicket == null ? "" : _fpPrintTicket.ToXmlString());

                        PrintTicket newActivePrintTicket;

                        if (!m_validatedPrintTicketCache.TryGetValue(printTicketPairXMLStr, out newActivePrintTicket))
                        {
                            //
                            // we have to merge the 2 print tickets
                            //
                            newActivePrintTicket = _printQueue.
                                                 MergeAndValidatePrintTicket(_rootPrintTicket, _fpPrintTicket).
                                                 ValidatedPrintTicket;

                            m_validatedPrintTicketCache.CacheValue(printTicketPairXMLStr, newActivePrintTicket);
                        }

                        // Ensure cache values are immutable.
                        _activePrintTicket = newActivePrintTicket.Clone ();

                        _isActiveUpdated   = true;
                    }
                    else
                    {
                        if(_fpPrintTicket != null)
                        {
                            _fpPrintTicket     = null;
                            _activePrintTicket = _rootPrintTicket;
                            _isActiveUpdated   = true;
                        }
                    }
                    break;
                }

                default:
                {
                    break;
                }
            }

        }

        public
        PrintTicket
        ConsumeActivePrintTicket(
            bool consumePrintTicket
            )
        {
            PrintTicket printTicket = null;

            if (_activePrintTicket == null &&
               _rootPrintTicket == null)
            {
                _activePrintTicket = new PrintTicket(_printQueue.UserPrintTicket.GetXmlStream());
                _rootPrintTicket = _activePrintTicket;
                printTicket = _activePrintTicket;
            }
            else if (_isActiveUpdated)
            {
                printTicket = _activePrintTicket;
                if (consumePrintTicket)
                {
                    _isActiveUpdated = false;
                }
            }

            return printTicket;
        }

        #endregion Public Methods

        #region Public Properties

        public
        PrintTicket
        ActivePrintTicket
        {
            get
            {
                //Call Consume Active Print Ticket with false flag, to get the current print ticket but do not consume it
                //so it can be retrieved again
                return ConsumeActivePrintTicket(false);
            }
        }

        #endregion Public Properties


        #region Private Members

        // duplicated code with metrodevice.cs is bad

        // PrintTicket XML Strings start at 4KB so we don't want the keys
        // from chewing all memory.  Limit to a small amount.
        private const int s_PrintTicketCacheMaxCount = 10;

        // Key = Pair of PrintTicket's whose XML (as strings) are concatenated
        // Value = PrintTicket
        //
        // 1. Dictionary keys and values must never be:
        //    - null
        //    - mutated
        // 2. Only cache per-page calculations because cache entries are preciously
        //    limited.
        private
        MS.Internal.Printing.MostFrequentlyUsedCache<string, PrintTicket> m_validatedPrintTicketCache;

        private
        PrintQueue      _printQueue;

        private
        PrintTicket     _fdsPrintTicket;

        private
        PrintTicket     _fdPrintTicket;

        private
        PrintTicket     _fpPrintTicket;

        private
        PrintTicket     _rootPrintTicket;

        private
        PrintTicket     _activePrintTicket;

        private
        bool            _isActiveUpdated;


        #endregion Private Members
    }


    internal sealed class MXDWSerializationManager
    {

        #region constructor

        public
        MXDWSerializationManager(
            PrintQueue   queue
            )
        {
            this._jobName       = null;
            this._gdiDevice = null;
            this._mxdwFileName  = null;

            _printQueue = queue;

            _jobName = _printQueue.CurrentJobSettings.Description;

            if(_jobName == null)
            {
                _jobName = NgcSerializerUtil.InferJobName(null);
            }

            _gdiDevice = new MetroToGdiConverter(_printQueue);
            GdiDevice.CreateDeviceContext(_jobName, InferPrintTicket());
            _isPassThruSupported = GdiDevice.ExtEscMXDWPassThru();
            GdiDevice.DeleteDeviceContext();
        }

        #endregion constructor

        public
        void
        EnablePassThru(
            )
        {
            GdiDevice.CreateDeviceContext(_jobName, InferPrintTicket());

            GdiDevice.ExtEscMXDWPassThru();

            GdiDevice.StartDocumentWithoutCreatingDC(_jobName);

            _mxdwFileName = GdiDevice.ExtEscGetName();
        }

        public
        String
        MxdwFileName
        {
            get
            {
                return _mxdwFileName;
            }
        }

        public
        bool
        IsPassThruSupported
        {
            get
            {
                return _isPassThruSupported;
            }
        }

        public
        void
        Commit(
            )
        {
            GdiDevice.EndDocument();
        }

        private
        PrintTicket
        InferPrintTicket(
            )
        {
            PrintTicket printTicket = new PrintTicket(_printQueue.UserPrintTicket.GetXmlStream());

            return printTicket;
        }

        private MetroToGdiConverter GdiDevice
        {
            get
            {
                return _gdiDevice;
            }
        }

        private
        PrintQueue              _printQueue;

        private
        MetroToGdiConverter     _gdiDevice;

        private
        String                  _jobName;

        private
        String                  _mxdwFileName;

        private
        bool                    _isPassThruSupported;
    };

}

