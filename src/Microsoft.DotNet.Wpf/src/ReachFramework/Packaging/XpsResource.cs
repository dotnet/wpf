// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                              
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for the XpsResource class.  This class acts as the
        base class for all resources that can be added to a
        Xps package.
                

--*/
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// Base class for all Xps Resources
    /// </summary>
    /// <exception cref="ArgumentNullException">part is null.</exception>
    public class XpsResource : XpsPartBase, INode, IDisposable
    {
        #region Constructors

        internal
        XpsResource(
            XpsManager    xpsManager,
            INode           parent,
            PackagePart     part
            )
            : base(xpsManager)
        {
            if (null == part)
            {
                throw new ArgumentNullException("part");
            }

            this.Uri = part.Uri;

            _parentNode = parent;
            _metroPart = part;

            _partEditor = new PartEditor(_metroPart);
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// This method retrieves the relative Uri for this resource
        /// based on a supplied absolute resource.
        /// </summary>
        /// <param name="inUri">
        /// Absolute Uri used for conversion.
        /// </param>
        /// <returns>
        /// A Uri to this resource relative to the supplied resource.
        /// </returns>
        public
        Uri
        RelativeUri(
            Uri inUri
            )
        {
            if( inUri == null )
            {
                throw new ArgumentNullException("inUri");
            }
            return new Uri(XpsManager.MakeRelativePath(this.Uri, inUri), UriKind.Relative);
        }

        /// <summary>
        /// This method retrieves a reference to the Stream that can
        /// be used to read and/or write data to/from this resource
        /// within the Metro package.
        /// </summary>
        /// <returns>
        /// A reference to a writable/readable stream.
        /// </returns>
        public
        virtual
        Stream
        GetStream(
            )
        {
            return _partEditor.DataStream;
        }

        /// <summary>
        /// This method commits all changes for this resource
        /// </summary>
        public
        void
        Commit(
            )
        {
            CommitInternal();
        }

        /// <summary>
        /// This method closes this resource part and frees all
        /// associated memory.
        /// </summary>
        internal
        override
        void
        CommitInternal()
        {
            if (_partEditor != null)
            {
                _partEditor.Close();

                _partEditor = null;
                _metroPart = null;
                _parentNode = null;
            }
        }

        #endregion Public methods

        #region Private data

        private INode _parentNode;
        private PackagePart _metroPart;

        private PartEditor _partEditor;

        #endregion Private data

        #region INode implementation

        void
        INode.Flush(
            )
        {
            if( _partEditor != null )
            {
                //
                // Flush the part editor
                //
                _partEditor.Flush();
            }
        }

        void
        INode.CommitInternal()
        {
            CommitInternal();
        }


        PackagePart
        INode.GetPart(
            )
        {
            return _metroPart;
        }

        #endregion INode implementation

        #region IDisposable implementation

        void
        IDisposable.Dispose()
        {
            if (_partEditor != null)
            {
                _partEditor.Close();
            }

            GC.SuppressFinalize(this);
        }

        #endregion IDisposable implementation
    }
}
