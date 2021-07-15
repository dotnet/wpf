// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of public PrintTicket class.



--*/

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents Print Ticket settings.
    /// </summary>
    internal class InternalPrintTicket
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the InternalPrintTicket class with no settings.
        /// </summary>
        public InternalPrintTicket()
        {
            #if _DEBUG
            InitTrace();
            #endif

            _xmlDoc = new XmlDocument();

            // If client doesn't provide an XML PrintTicket, we will start with the
            // built-in empty PrintTicket. We don't expect this loading will cause
            // any exceptions.
            _xmlDoc.LoadXml(PrintSchemaTags.Framework.EmptyPrintTicket);

            SetupNamespaceManager();
        }

        /// <summary>
        /// Constructs a new instance of the InternalPrintTicket class with settings based on the XML form of PrintTicket.
        /// </summary>
        /// <param name="xmlStream">Stream object containing the XML form of PrintTicket.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="xmlStream"/> parameter is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// The stream object specified by <paramref name="xmlStream"/> parameter doesn't contain a well-formed XML PrintTicket.
        /// The exception object's <see cref="Exception.Message"/> property describes why the XML is not well-formed.
        /// And if not null, the exception object's <see cref="Exception.InnerException"/> property provides more details.
        /// </exception>
        public InternalPrintTicket(Stream xmlStream)
        {
            // Verify input parameter
            if (xmlStream == null)
            {
                throw new ArgumentNullException("xmlStream");
            }

            #if _DEBUG
            InitTrace();
            #endif

            _xmlDoc = new XmlDocument();

            try
            {
                // After reading the data from the user supplied input stream, we do NOT
                // reset the stream position. This allows us to provide consistent behavior
                // for seekable and non-seekable streams. It's trivial for application to
                // reset the stream position based on their own needs.

                _xmlDoc.Load(xmlStream);

                // Make sure the XML content we write out is in UTF-8.

                XmlDeclaration xmlDecl = _xmlDoc.FirstChild as XmlDeclaration;

		        // WARNING: windows/wcp/Print/Reach/PrintConfig/PrtTicket_Public_Simple.cs
		        //          assumes that our xml stream is _always_ UTF-8 encoded
                if (xmlDecl != null)
                {
                    // XML declaration already exists in the input PrintTicket XML stream.
                    // So enforce the UTF-8 encoding.
                    xmlDecl.Encoding = "UTF-8";
                }
                else
                {
                    // There's no XML declaration in the input PrintTicket XML stream.
                    // So create the XML declaration with UTF-8 encoding.
                    xmlDecl = _xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    _xmlDoc.InsertBefore(xmlDecl, _xmlDoc.DocumentElement);
                }
            }
            catch (XmlException e)
            {
                // We catch the XmlException thrown by XML parser and rethrow our own FormatException
                // so that our client only needs to care about FormatException for non-well-formed Print Ticket.
                throw NewPTFormatException(e.Message, e);
            }

            SetupNamespaceManager();

            // Now the client supplied XML PrintTicket has been verified by XML
            // parser as well-formed XML, but we still need to further verify that
            // it is also well-formed PrintTicket

            // Ideally we should delay load the XML PrintTicket into DOM only
            // when client is reading or writing the content. But can we do delay
            // loading and still be able to report non-well-formed PrintTicket error
            // at constructor time?

            PrintTicketEditor.CheckIsWellFormedPrintTicket(this);

            PrintTicketEditor.CheckAndAddMissingStdNamespaces(this);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Creates a duplicate of this InternalPrintTicket object.
        /// </summary>
        /// <returns>The duplicated InternalPrintTicket object.</returns>
        public InternalPrintTicket Clone()
        {
            // Design Guideline says do not implement ICloneable since that interface doesn't
            // specify whether the clone is a deep copy or non-deep copy. So it's recommended
            // that each type defines its own Clone or Copy methodology.
            InternalPrintTicket clonePT = new InternalPrintTicket(this.XmlStream);

            return clonePT;
        }

        /// <summary>
        /// Saves this InternalPrintTicket object to the specified stream.
        /// </summary>
        /// <param name="outStream">The stream to which you want to save.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="outStream"/> parameter is null.
        /// </exception>
        public void SaveTo(Stream outStream)
        {
            // Verify input parameter
            if (outStream == null)
            {
                throw new ArgumentNullException("outStream");
            }

            // After writing the data to the user supplied output stream, we do NOT
            // reset the stream position. This allows us to provide consistent behavior
            // for seekable and non-seekable streams. It's trivial for application to
            // reset the stream position based on their own needs.

            _xmlDoc.Save(outStream);
        }

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// Gets a <see cref="DocumentCollateSetting"/> object that specifies the document collate setting.
        /// </summary>
        public DocumentCollateSetting DocumentCollate
        {
            get
            {
                if (_docCollate == null)
                {
                    _docCollate = new DocumentCollateSetting(this);
                }

                return _docCollate;
            }
        }

        /// <summary>
        /// Gets a <see cref="JobDuplexSetting"/> object that specifies the job duplex setting.
        /// </summary>
        public JobDuplexSetting JobDuplex
        {
            get
            {
                if (_jobDuplex == null)
                {
                    _jobDuplex = new JobDuplexSetting(this);
                }

                return _jobDuplex;
            }
        }

        /// <summary>
        /// Gets a <see cref="JobNUpSetting"/> object that specifies the job NUp setting.
        /// </summary>
        public JobNUpSetting JobNUp
        {
            get
            {
                if (_jobNUp == null)
                {
                    _jobNUp = new JobNUpSetting(this);
                }

                return _jobNUp;
            }
        }

        /// <summary>
        /// Gets a <see cref="JobStapleSetting"/> object that specifies the job output stapling setting.
        /// </summary>
        public JobStapleSetting JobStaple
        {
            get
            {
                if (_jobStaple == null)
                {
                    _jobStaple = new JobStapleSetting(this);
                }

                return _jobStaple;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageDeviceFontSubstitutionSetting"/> object that specifies the page device font substitution setting.
        /// </summary>
        public PageDeviceFontSubstitutionSetting PageDeviceFontSubstitution
        {
            get
            {
                if (_pageDeviceFontSubst == null)
                {
                    _pageDeviceFontSubst = new PageDeviceFontSubstitutionSetting(this);
                }

                return _pageDeviceFontSubst;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageMediaSizeSetting"/> object that specifies the page media size setting.
        /// </summary>
        public PageMediaSizeSetting PageMediaSize
        {
            get
            {
                if (_pageMediaSize == null)
                {
                    _pageMediaSize = new PageMediaSizeSetting(this);
                }

                return _pageMediaSize;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageMediaTypeSetting"/> object that specifies the page media type setting.
        /// </summary>
        public PageMediaTypeSetting PageMediaType
        {
            get
            {
                if (_pageMediaType == null)
                {
                    _pageMediaType = new PageMediaTypeSetting(this);
                }

                return _pageMediaType;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageOrientationSetting"/> object that specifies the page orientation setting.
        /// </summary>
        public PageOrientationSetting PageOrientation
        {
            get
            {
                if (_pageOrientation == null)
                {
                    _pageOrientation = new PageOrientationSetting(this);
                }

                return _pageOrientation;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageOutputColorSetting"/> object that specifies the page output color setting.
        /// </summary>
        public PageOutputColorSetting PageOutputColor
        {
            get
            {
                if (_pageOutputColor == null)
                {
                    _pageOutputColor = new PageOutputColorSetting(this);
                }

                return _pageOutputColor;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageResolutionSetting"/> object that specifies the page resolution setting.
        /// </summary>
        public PageResolutionSetting PageResolution
        {
            get
            {
                if (_pageResolution == null)
                {
                    _pageResolution = new PageResolutionSetting(this);
                }

                return _pageResolution;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageScalingSetting"/> object that specifies the page scaling setting.
        /// </summary>
        public PageScalingSetting PageScaling
        {
            get
            {
                if (_pageScaling == null)
                {
                    _pageScaling = new PageScalingSetting(this);
                }

                return _pageScaling;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageTrueTypeFontModeSetting"/> object that specifies the page TrueType font handling mode setting.
        /// </summary>
        public PageTrueTypeFontModeSetting PageTrueTypeFontMode
        {
            get
            {
                if (_pageTrueTypeFontMode == null)
                {
                    _pageTrueTypeFontMode = new PageTrueTypeFontModeSetting(this);
                }

                return _pageTrueTypeFontMode;
            }
        }

        /// <summary>
        /// Gets a <see cref="JobPageOrderSetting"/> object that specifies the job page order setting.
        /// </summary>
        public JobPageOrderSetting JobPageOrder
        {
            get
            {
                if (_jobPageOrder == null)
                {
                    _jobPageOrder = new JobPageOrderSetting(this);
                }

                return _jobPageOrder;
            }
        }

        /// <summary>
        /// Gets a <see cref="PagePhotoPrintingIntentSetting"/> object that specifies the page photo printing intent setting.
        /// </summary>
        public PagePhotoPrintingIntentSetting PagePhotoPrintingIntent
        {
            get
            {
                if (_pagePhotoIntent == null)
                {
                    _pagePhotoIntent = new PagePhotoPrintingIntentSetting(this);
                }

                return _pagePhotoIntent;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageBorderlessSetting"/> object that specifies the page borderless setting.
        /// </summary>
        public PageBorderlessSetting PageBorderless
        {
            get
            {
                if (_pageBorderless == null)
                {
                    _pageBorderless = new PageBorderlessSetting(this);
                }

                return _pageBorderless;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageOutputQualitySetting"/> object that specifies the page output quality setting.
        /// </summary>
        public PageOutputQualitySetting PageOutputQuality
        {
            get
            {
                if (_pageOutputQuality == null)
                {
                    _pageOutputQuality = new PageOutputQualitySetting(this);
                }

                return _pageOutputQuality;
            }
        }

        /// <summary>
        /// Gets a <see cref="JobInputBinSetting"/> object that specifies the job input bin setting.
        /// </summary>
        public JobInputBinSetting JobInputBin
        {
            get
            {
                if (_jobInputBin == null)
                {
                    _jobInputBin = new JobInputBinSetting(this);
                }

                return _jobInputBin;
            }
        }

        /// <summary>
        /// Gets a <see cref="DocumentInputBinSetting"/> object that specifies the document input bin setting.
        /// </summary>
        public DocumentInputBinSetting DocumentInputBin
        {
            get
            {
                if (_documentInputBin == null)
                {
                    _documentInputBin = new DocumentInputBinSetting(this);
                }

                return _documentInputBin;
            }
        }
        /// <summary>
        /// Gets a <see cref="PageInputBinSetting"/> object that specifies the page input bin setting.
        /// </summary>
        public PageInputBinSetting PageInputBin
        {
            get
            {
                if (_pageInputBin == null)
                {
                    _pageInputBin = new PageInputBinSetting(this);
                }

                return _pageInputBin;
            }
        }

        /// <summary>
        /// Gets a <see cref="JobCopyCountSetting"/> object that specifies the job copy count setting.
        /// </summary>
        public JobCopyCountSetting JobCopyCount
        {
            get
            {
                if (_jobCopyCount == null)
                {
                    _jobCopyCount = new JobCopyCountSetting(this);
                }

                return _jobCopyCount;
            }
        }

        /// <summary>
        /// Gets a <see cref="MemoryStream"/> object that contains the XML form of this PrintTicket object.
        /// </summary>
        public MemoryStream XmlStream
        {
            get
            {
                // There are clients that assume the backing buffer is publicly visible
                // Do not call the MemoryStream constructor that makes the backing buffer private
                MemoryStream ms = new MemoryStream();
                _xmlDoc.Save(ms);
                ms.Position = 0;
                return ms;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintTicket XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        internal static FormatException NewPTFormatException(string detailMsg)
        {
            return NewPTFormatException(detailMsg, null);
        }

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintTicket XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <param name="innerException">the exception that causes the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        internal static FormatException NewPTFormatException(string detailMsg, Exception innerException)
        {
            return new FormatException(String.Format(CultureInfo.CurrentCulture,
                                                     "{0} {1} {2}",
                                                     PrintSchemaTags.Framework.PrintTicketRoot,
                                                     PTUtility.GetTextFromResource("FormatException.XMLNotWellFormed"),
                                                     detailMsg),
                                       innerException);
        }

        internal PrintTicketFeature GetBasePTFeatureObject(CapabilityName feature)
        {
            switch (feature)
            {
                case CapabilityName.DocumentCollate:
                    return this.DocumentCollate;

                case CapabilityName.PageDeviceFontSubstitution:
                    return this.PageDeviceFontSubstitution;

                case CapabilityName.JobDuplex:
                    return this.JobDuplex;

                case CapabilityName.JobInputBin:
                    return this.JobInputBin;

                case CapabilityName.DocumentInputBin:
                    return this.DocumentInputBin;

                case CapabilityName.PageInputBin:
                    return this.PageInputBin;

                case CapabilityName.PageOutputColor:
                    return this.PageOutputColor;

                case CapabilityName.PageOutputQuality:
                    return this.PageOutputQuality;

                case CapabilityName.PageBorderless:
                    return this.PageBorderless;

                case CapabilityName.PageMediaType:
                    return this.PageMediaType;

                case CapabilityName.JobPageOrder:
                    return this.JobPageOrder;

                case CapabilityName.PageOrientation:
                    return this.PageOrientation;

                case CapabilityName.JobNUp:
                    return this.JobNUp.PresentationDirection;

                case CapabilityName.PagePhotoPrintingIntent:
                    return this.PagePhotoPrintingIntent;

                case CapabilityName.JobStaple:
                    return this.JobStaple;

                case CapabilityName.PageTrueTypeFontMode:
                    return this.PageTrueTypeFontMode;

                default:
                    return null;
            }
        }

        #endregion internal Methods

        #region Internal Properties

        internal XmlDocument XmlDoc
        {
            get
            {
                return _xmlDoc;
            }
        }

        internal XmlNamespaceManager NamespaceManager
        {
            get
            {
                return _nsMgr;
            }
        }

        #endregion Internal Properties

        #region Private Methods

        #if _DEBUG
        private void InitTrace()
        {
            // direct Trace output to console
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        }
        #endif

        private void SetupNamespaceManager()
        {
            _nsMgr = new XmlNamespaceManager(new NameTable());

            _nsMgr.AddNamespace(PrintSchemaPrefixes.Framework, PrintSchemaNamespaces.Framework);
            _nsMgr.AddNamespace(PrintSchemaPrefixes.StandardKeywordSet, PrintSchemaNamespaces.StandardKeywordSet);
            _nsMgr.AddNamespace(PrintSchemaPrefixes.xsi, PrintSchemaNamespaces.xsi);
            _nsMgr.AddNamespace(PrintSchemaPrefixes.xsd, PrintSchemaNamespaces.xsd);
        }

        #endregion Private Methods

        #region Private Fields

        private XmlDocument             _xmlDoc;
        private XmlNamespaceManager     _nsMgr;

        // Features
        private DocumentCollateSetting  _docCollate;
        private JobDuplexSetting        _jobDuplex;
        private JobNUpSetting           _jobNUp;
        private JobStapleSetting        _jobStaple;
        private PageDeviceFontSubstitutionSetting _pageDeviceFontSubst;
        private PageMediaSizeSetting    _pageMediaSize;
        private PageMediaTypeSetting    _pageMediaType;
        private PageOrientationSetting  _pageOrientation;
        private PageOutputColorSetting  _pageOutputColor;
        private PageResolutionSetting   _pageResolution;
        private PageScalingSetting      _pageScaling;
        private PageTrueTypeFontModeSetting _pageTrueTypeFontMode;
        private JobPageOrderSetting     _jobPageOrder;
        private PagePhotoPrintingIntentSetting _pagePhotoIntent;
        private PageBorderlessSetting   _pageBorderless;
        private PageOutputQualitySetting _pageOutputQuality;
        private JobInputBinSetting       _jobInputBin;
        private DocumentInputBinSetting  _documentInputBin;
        private PageInputBinSetting      _pageInputBin;

        // Parameters
        private JobCopyCountSetting      _jobCopyCount;

        #endregion Private Fields
    }
}
