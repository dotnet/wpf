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
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Xps;
using System.Printing;

#pragma warning disable 1634, 1691 //Allows suppression of certain PreSharp messages

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Xps Document events definition
    /// </summary>        
    internal enum XpsDocumentEventType
    {
        /// <summary>
        /// No value
        /// </summary>        
        None  = 0,
        /// <summary>
        /// Xps Document events AddFixedDocumentSequencePre
        /// </summary>        
        AddFixedDocumentSequencePre = 1,
        /// <summary>
        /// Xps Document events AddFixedDocumentSequencePost
        /// </summary>        
        AddFixedDocumentSequencePost = 13,
        /// <summary>
        /// Xps Document events AddFixedDocumentPre
        /// </summary>        
        AddFixedDocumentPre = 2,
        /// <summary>
        /// Xps Document events AddFixedDocumentPost
        /// </summary>        
        AddFixedDocumentPost = 5,
        /// <summary>
        /// Xps Document events AddFixedPagePre
        /// </summary>        
        AddFixedPagePre = 3,
        /// <summary>
        /// Xps Document events AddFixedPagePost
        /// </summary>        
        AddFixedPagePost = 4,
        /// <summary>
        /// Xps Document events AddFixedDocumentSequencePrintTicketPre
        /// </summary>        
        AddFixedDocumentSequencePrintTicketPre = 7,
        /// <summary>
        /// Xps Document events AddFixedDocumentSequencePrintTicketPost
        /// </summary>        
        AddFixedDocumentSequencePrintTicketPost = 12,
        /// <summary>
        /// Xps Document events AddFixedDocumentPrintTicketPre
        /// </summary>        
        AddFixedDocumentPrintTicketPre = 8,
        /// <summary>
        /// Xps Document events AddFixedDocumentPrintTicketPost
        /// </summary>        
        AddFixedDocumentPrintTicketPost = 11,
        /// <summary>
        /// Xps Document events AddFixedPagePrintTicketPre
        /// </summary>        
        AddFixedPagePrintTicketPre = 9,
        /// <summary>
        /// Xps Document events AddFixedPagePrintTicketPost
        /// </summary>        
        AddFixedPagePrintTicketPost = 10,
        /// <summary>
        /// Xps serialization cancelled
        /// </summary>        
        XpsDocumentCancel = 6,        

    };

    internal class XpsDriverDocEventManager
    {
        #region Constructor
        /// <summary>
        /// Constructor to create and initialize the XpsDriverDocEventManager
        /// </summary>
        public
        XpsDriverDocEventManager(
            XpsSerializationManager     manager
            )
        {
            this._manager = manager;
            
            _documentEvent = XpsDocumentEventType.None;
            _currentCount  = 0;
            _currentPage   = 0;
            _currentDocSequence = 0;
            _currentFixedDocument = 0;
            _printTicket = null;
            _printTicketLevel = PrintTicketLevel.None;
        }
        #endregion Constructor

        internal
        void
        ForwardPackagingProgressEvent(        
            Object                     sender,
            PackagingProgressEventArgs e
            )
        {
            switch (e.Action)
            {
                case PackagingAction.AddingDocumentSequence:
                {
                    _currentDocSequence = 1;
                    _currentFixedDocument = 0;
                    _currentPage = 0;

                    _currentCount = _currentDocSequence;
                    _documentEvent = XpsDocumentEventType.AddFixedDocumentSequencePre;

                    OnXpsDriverDocEvent();
                    break;
                }
                case PackagingAction.DocumentSequenceCompleted:
                {
                    _currentCount = _currentDocSequence;
                    _documentEvent = XpsDocumentEventType.AddFixedDocumentSequencePost;

                    OnXpsDriverDocEvent();
                    break;
                }
                case PackagingAction.AddingFixedDocument:
                {
                    _currentFixedDocument++;
                    _currentPage = 0;

                    _currentCount = _currentFixedDocument;
                    _documentEvent = XpsDocumentEventType.AddFixedDocumentPre;

                    OnXpsDriverDocEvent();
                    break;
                }
                case PackagingAction.FixedDocumentCompleted:
                {
                    _currentCount = _currentFixedDocument;
                    _documentEvent = XpsDocumentEventType.AddFixedDocumentPost;

                    OnXpsDriverDocEvent();
                    break;
                }
                case PackagingAction.AddingFixedPage:
                {
                    _currentPage++;

                    _currentCount = _currentPage;
                    _documentEvent = XpsDocumentEventType.AddFixedPagePre;

                    OnXpsDriverDocEvent();
                    break;
                }
                case PackagingAction.FixedPageCompleted:
                {
                    _documentEvent = XpsDocumentEventType.AddFixedPagePost;
                    for (int i = e.NumberCompleted; i > 0; i--)
                    {
                        _currentCount = _currentPage - i + 1;
                        OnXpsDriverDocEvent();
                    }
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        internal
        void
        ForwardUserPrintTicket(
            Object                                          sender,
            XpsSerializationPrintTicketRequiredEventArgs    e
        )
        {
            Boolean mustCallXpsDriverDocEvent = true;

            _printTicket = e.PrintTicket;
            _printTicketLevel = e.PrintTicketLevel;

            switch (_printTicketLevel)
            {
                case PrintTicketLevel.FixedDocumentSequencePrintTicket:
                {
                    _currentCount = _currentDocSequence;
                    _documentEvent = XpsDocumentEventType.AddFixedDocumentSequencePrintTicketPre;
                    break;
                }
                case PrintTicketLevel.FixedDocumentPrintTicket:
                {
                    _currentCount = _currentFixedDocument;
                    _documentEvent = XpsDocumentEventType.AddFixedDocumentPrintTicketPre;
                    break;
                }
                case PrintTicketLevel.FixedPagePrintTicket:
                {
                    _currentCount = _currentPage + 1;
                    _documentEvent = XpsDocumentEventType.AddFixedPagePrintTicketPre;
                    break;
                }
                default:
                {
                    mustCallXpsDriverDocEvent = false;
                    break;
                }
            }

            if (mustCallXpsDriverDocEvent)
            {
                XpsSerializationXpsDriverDocEventArgs xpsEventArgs = OnXpsDriverDocEvent();

                if (xpsEventArgs.Modified)
                {
                    e.PrintTicket = xpsEventArgs.PrintTicket;
                }
            }
        }

        internal
        void
        ForwardSerializationCompleted(
            object                              sender,
            XpsSerializationCompletedEventArgs e
            )
        {
            if (e.Cancelled == true)
            {
                _documentEvent = XpsDocumentEventType.XpsDocumentCancel;

                OnXpsDriverDocEvent();
            }
        }

        XpsSerializationXpsDriverDocEventArgs
        OnXpsDriverDocEvent(
            )
        {
            XpsSerializationXpsDriverDocEventArgs e = new XpsSerializationXpsDriverDocEventArgs(_documentEvent,
                                                                                                _currentCount,
                                                                                                _printTicket);
            _manager.OnXpsDriverDocEvent(e);

            return e;
        }

        private 
        XpsSerializationManager         _manager;

        private
        XpsDocumentEventType            _documentEvent;

        private
        int                             _currentCount;

        private
        int                             _currentPage;

        private
        int                             _currentDocSequence;

        private
        int                             _currentFixedDocument;

        private
        PrintTicket                     _printTicket;

        private
        PrintTicketLevel                _printTicketLevel;

    };

    /// <summary>
    /// To be used to subscribe for get the PT which the app sends in and call XpsDocEvent
    /// into the XPS driver
    /// </summary>
    internal
    delegate
    void
    XpsSerializationXpsDriverDocEventHandler(
        object sender,
        XpsSerializationXpsDriverDocEventArgs e
        );

    /// <summary>
    ///  EventArgs for XpsSerializationXpsDriverDocEvent
    /// </summary>
    internal class XpsSerializationXpsDriverDocEventArgs :
                   EventArgs
    {
        /// <summary>
        /// ctr for XpsSerializationXpsDriverDocEventArgs
        /// </summary>
        public
        XpsSerializationXpsDriverDocEventArgs(
            XpsDocumentEventType    documentEvent,
            int                     currentCount,
            PrintTicket             printTicket
            )
        {
            _currentCount = currentCount;
            _documentEvent = documentEvent;
            _printTicket = printTicket;
            _modified = false;
        }

        /// <summary>
        /// Current Page/Doc Index
        /// </summary>
        public
        int
        CurrentCount
        {
            get
            {
                return _currentCount;
            }
        }

        /// <summary>
        /// PrintTicket
        /// </summary>
        public
        PrintTicket
        PrintTicket
        {
            set
            {
                _printTicket = value;
                _modified = true;
            }

            get
            {
                return _printTicket;
            }
        }

        /// <summary>
        /// DocumentEvent
        /// </summary>
        public
        XpsDocumentEventType
        DocumentEvent
        {
            get
            {
                return _documentEvent;
            }
        }

        /// <summary>
        /// Modified
        /// </summary>
        internal
        bool
        Modified
        {
            get
            {
                return _modified;
            }
        }

        private
        XpsDocumentEventType _documentEvent;
        
        private 
        int                 _currentCount;
        
        private 
        PrintTicket         _printTicket;

        private
        bool                _modified;

    };
}

