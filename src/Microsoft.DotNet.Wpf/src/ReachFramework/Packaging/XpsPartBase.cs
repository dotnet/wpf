// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                             
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for the XpsPartBase class.  This class is the base
        class for all pieces that represent main parts in a
        Xps package such as fixed documents, fixed pages,
        resources, etc.
                

--*/
using System;
using System.IO.Packaging;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    ///
    /// </summary>
    public abstract class XpsPartBase
    {
        #region Constructors

        /// <summary>
        /// Constructs the base XpsPartBase instance.
        /// </summary>
        /// <param name="xpsManager">
        /// The xps manager controlling this xps object.
        /// </param>
        internal
        XpsPartBase(
            XpsManager    xpsManager
            )
        {
            if (null == xpsManager)
            {
                throw new ArgumentNullException("xpsManager");
            }

            _xpsManager = xpsManager;
        }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets or sets the Uri of the current Xps part.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return _uri;
            }
            set
            {
                _uri = value;
            }
        }

        #endregion Public properties

        #region Public methods

        /// <summary>
        /// Closes the XpsPartBase.
        /// </summary>
        internal
        virtual
        void
        CommitInternal(
            )
        {
            if (null != _xpsManager)
            {
                _xpsManager = null;
            }
        }

        #endregion Public methods

        #region Internal properties

        /// <summary>
        /// Gets a reference to the current Xps manager for
        /// this package.
        /// </summary>
        internal XpsManager CurrentXpsManager
        {
            get
            {
                return _xpsManager;
            }
        }

        #endregion Internal properties

        #region Private data

        private XpsManager _xpsManager;
        private Uri _uri;

        #endregion Private data
    }
}
