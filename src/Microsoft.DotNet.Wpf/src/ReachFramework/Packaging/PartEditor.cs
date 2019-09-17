// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                       
    Module Name:                                                                
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for PartEditor and XmlPartEditor classes.  These classes
        are used as wrappers around the Metro package API part
        classes.
                
                                     
                                                                             
--*/
using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

using MS.Internal.IO.Packaging.Extensions;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// Provides access to the stream of a Metro part
    /// </summary>
    internal class PartEditor : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Attach an editor to a part
        /// </summary>
        /// <param name="metroPart"></param>
        internal
        PartEditor(
            PackagePart     metroPart
            )
        {
            if (null == metroPart)
            {
                throw new ArgumentNullException("metroPart");
            }

            _metroPart = metroPart;
        }

        #endregion Constructors

        #region Protected properties

        protected PackagePart MetroPart
        {
            get
            {
                return _metroPart;
            }
        }

        #endregion Protected properties

        #region Internal properties

        /// <summary>
        /// Return a data stream for the part
        /// </summary>
        /// <value></value>
        internal Stream DataStream
        {
            get
            {
                if (null == _partDataStream)
                {
                    if (_metroPart.Package.FileOpenAccess == FileAccess.Write)
                    {
                        _partDataStream = _metroPart.GetStream(FileMode.Create);
                    }
                    else
                    {
                        _partDataStream = _metroPart.GetStream(FileMode.OpenOrCreate);
                    }
}

                return _partDataStream;
            }
        }

        #endregion Internal properties

        #region Internal methods

        /// <summary>
        /// Close the part data stream
        /// </summary>
        internal
        virtual
        void
        Close(
            )
        {
            if (null != _partDataStream)
            {
                if (_partDataStream.CanWrite)
                {
                    _partDataStream.Close();
                }

                GC.SuppressFinalize(this);
                _partDataStream = null;
            }
        }

        /// <summary>
        /// Forcibly flush the part data stream
        /// </summary>
        internal
        virtual
        void
        Flush(
            )
        {
            if (null != _partDataStream)
            {
                _partDataStream.Flush();
            }
        }
        
        #endregion Internal methods

        #region Private data

        private PackagePart _metroPart;
        private Stream _partDataStream;

        #endregion Private data

        #region IDisposable implementation

        void
        IDisposable.Dispose(
            )
        {
            Close();
        }

        #endregion IDisposable implementation
    }

    /// <summary>
    /// Part editor with XML content
    /// </summary>
    internal class XmlPartEditor : PartEditor
    {
        #region Constructors

        /// <summary>
        /// Attach an editor to an existing part
        /// </summary>
        /// <param name="metroPart"></param>
        internal
        XmlPartEditor(
            PackagePart     metroPart
            )
            : base(metroPart)
        {
            _doesWriteStartEndTags = true;
            _isStartElementWritten = false;
        }

        #endregion Constructors

        #region Internal properties

        internal bool DoesWriteStartEndTags
        {
            get
            {
                return _doesWriteStartEndTags;
            }
            set
            {
                _doesWriteStartEndTags = value;
            }
        }

        internal bool IsStartElementWritten
        {
            get
            {
                return _isStartElementWritten;
            }
        }

        internal XmlTextWriter XmlWriter
        {
            get
            {
                if (null == _xmlWriter)
                {
                    OpenDocumentForWrite();
                }

                return _xmlWriter;
            }
        }
        
        internal XmlTextReader XmlReader
        {
            get
            {
                if (null == _xmlReader)
                {
                    OpenDocumentForRead();
                }

                return _xmlReader;
            }
        }

        #endregion Internal properties

        #region Internal methods

        /// <summary>
        /// Setup for writing
        /// </summary>
        internal
        void
        OpenDocumentForRead(
            )
        {
            if (_xmlReader != null)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_OpenDocOrElementAlreadyCalled));
            }

            Stream stream = MetroPart.GetStream(FileMode.Open);
  
            _xmlReader = new XmlTextReader(stream);
        }
        
        /// <summary>
        /// Setup for writing
        /// </summary>
        internal
        void
        OpenDocumentForWrite(
            )
        {
            if (_xmlWriter != null)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_OpenDocOrElementAlreadyCalled));
            }

            Stream stream = MetroPart.GetStream(FileMode.Create);

            _xmlWriter = new XmlTextWriter(stream, System.Text.Encoding.UTF8);
        }


        internal
        void
        PrepareXmlWriter(
            string      startTag,
            string      namespaceUri
            )
        {
            if (null == _xmlWriter)
            {
                //
                // Stream not open yet, create it and write open tag
                //
                OpenDocumentForWrite();

                if (_doesWriteStartEndTags)
                {
                    _xmlWriter.WriteStartDocument();
                }

                _xmlWriter.WriteStartElement(startTag, namespaceUri);
                _isStartElementWritten = true;
            }
        }



        /// <summary>
        /// Flush any XML written so far for sequence (does not end Sequence tag)
        /// </summary>
        internal
        override
        void
        Flush(
            )
        {
            if (null != _xmlWriter)
            {
                _xmlWriter.Flush();
            }
        }

        /// <summary>
        /// Close XML written for sequence (adds end Sequence tag)
        /// </summary>
        internal
        override
        void
        Close(
            )
        {
            if (null != _xmlWriter)
            {
                _xmlWriter.Close();
                _xmlWriter = null;
            }

            if (null != _xmlReader)
            {
                _xmlReader.Close();
                _xmlReader = null;
            }
            base.Close();
        }

        #endregion Internal methods

        #region Private data

        private bool          _doesWriteStartEndTags;
        private XmlTextWriter _xmlWriter;
        private XmlTextReader _xmlReader;
        private bool          _isStartElementWritten;

        #endregion Private data
    }
}
