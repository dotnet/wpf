// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                               
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for the XpsResourcePolicy class.  This class controls
        how resources are shared and serialized within the Xps
        package.
                                                  
                                                                             
--*/
using System;
using System.Collections.Generic;
using System.Windows.Xps.Serialization;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    ///
    /// </summary>
    internal class XpsResourcePolicy : IServiceProvider
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of a XpsResourcePolicy used to
        /// determine how Xps resources are shared/stores/converted
        /// when being added to the Xps package.
        /// </summary>
        public
        XpsResourcePolicy(
            XpsResourceSharing sharingMode
            )
        {
            _sharingMode = sharingMode;

            _imageCrcTable = null;
            _imageUriHashTable = null;
            _currentPageImageTable = null;
            
            _colorContextTable = null;
            _currentPageColorContextTable = null;
            
            _resourceDictionaryTable = null;
            _currentPageResourceDictionaryTable = null;
        }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets the current sharing mode for this resource policy.
        /// </summary>
        public XpsResourceSharing ResourceSharingMode
        {
            get
            {
                return _sharingMode;
            }
        }

        #endregion Public properties

        #region Public methods

        /// <summary>
        /// This method registers a resource service with this resource
        /// policy for use by the packaging and serialization APIs.
        /// </summary>
        /// <exception cref="ArgumentNullException">service or serviceType is null..</exception>
        /// <exception cref="XpsPackagingException">serviceType has already been registered..</exception>
        public
        void
        RegisterService(
            object      service,
            Type        serviceType
            )
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            if (!_objDict.ContainsKey(serviceType))
            {
                _objDict.Add(serviceType, service);
            }
            else if (_objDict[serviceType] != service)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_ServiceTypeAlreadyAdded, serviceType));
            }
        }
        
        internal
        bool
        SubsetComplete(INode node)
        {
            FontSubsetterCommitPolicies signal = FontSubsetterCommitPolicies.CommitPerPage;
            bool validSubsetNode = true;
            bool subsetComplete = false;
            if( node is IXpsFixedDocumentSequenceWriter )
            {
                signal  = FontSubsetterCommitPolicies.CommitEntireSequence;
            }
            else
            if( node is IXpsFixedDocumentWriter )
            {
                signal  = FontSubsetterCommitPolicies.CommitPerDocument;
            }
            else
            if( node is IXpsFixedPageWriter )
            {
                signal  = FontSubsetterCommitPolicies.CommitPerPage;
             }
            else
            {
                validSubsetNode = false;
            }

            if( validSubsetNode )
            {
                XpsFontSerializationService fontService = (XpsFontSerializationService)GetService(typeof(XpsFontSerializationService));
                if( fontService != null )
                {
                    XpsFontSubsetter fontSubsetter = fontService.FontSubsetter;
                    subsetComplete = fontSubsetter.CommitFontSubsetsSignal(signal);
                }
            }
            return subsetComplete;
        }


        #endregion Public methods

        #region Protected methods

        /// <summary>
        /// Retrieves a service interface given the
        /// specified service type.
        /// </summary>
        /// <param name="serviceType">
        /// Service type to retrieve.
        /// </param>
        /// <returns>
        /// A instance of the service interface.
        /// </returns>
        internal
        object
        GetService(
            Type serviceType
            )
        {
            object service = null;

            if (_objDict.ContainsKey(serviceType))
            {
                service = _objDict[serviceType];
            }

            return service;
        }

        #endregion Protected methods

        #region IServiceProvider implementation

        object
        IServiceProvider.GetService(
            Type        serviceType
            )
        {
            return GetService(serviceType);
        }

        #endregion IServiceProvider implementation

        #region Internal Properties

        internal
        Dictionary<UInt32, Uri>
        ImageCrcTable
        {
            get
            {
                return _imageCrcTable;
            }
            set
            {
                _imageCrcTable = value;
            }
        }

        internal
        Dictionary<int, Uri>
        ImageUriHashTable
        {
            get
            {
                return _imageUriHashTable;
            }
            set
            {
                _imageUriHashTable = value;
            }
        }
        



        internal
        Dictionary<int, Uri>
        CurrentPageImageTable
        {
            get
            {
                return _currentPageImageTable;
            }
            set
            {
                _currentPageImageTable = value;
            }
        }

        internal
        Dictionary<int, Uri>
        ColorContextTable
        {
            get
            {
                return _colorContextTable;
            }
            set
            {
                _colorContextTable = value;
            }
        }

        internal
        Dictionary<int, Uri>
        CurrentPageColorContextTable
        {
            get
            {
                return _currentPageColorContextTable;
            }
            set
            {
                _currentPageColorContextTable = value;
            }
        }

        internal
        Dictionary<int, Uri>
        ResourceDictionaryTable
        {
            get
            {
                return _resourceDictionaryTable;
            }
            set
            {
                _resourceDictionaryTable = value;
            }
        }

        internal
        Dictionary<int, Uri>
        CurrentPageResourceDictionaryTable
        {
            get
            {
                return _currentPageResourceDictionaryTable;
            }
            set
            {
                _currentPageResourceDictionaryTable = value;
            }
        }

        #endregion Internal Properties
       
        #region Private data

        private
        Dictionary<UInt32, Uri>     _imageCrcTable;

        private 
        Dictionary<int,Uri>             _imageUriHashTable;

        private
        Dictionary<int, Uri>     _currentPageImageTable;

        private
        Dictionary<int, Uri>        _colorContextTable;

        private
        Dictionary<int, Uri>        _currentPageColorContextTable;

        private
        Dictionary<int, Uri>        _resourceDictionaryTable;

        private
        Dictionary<int, Uri>        _currentPageResourceDictionaryTable;

        private
        XpsResourceSharing          _sharingMode;

        private
        Dictionary<Type, object>    _objDict = new Dictionary<Type, object>();

        #endregion Private data
    }

    /// <summary>
    ///
    /// </summary>
    public enum XpsResourceSharing
    {
        /// <summary>
        ///
        /// </summary>
        ShareResources = 0,
        /// <summary>
        ///
        /// </summary>
        NoResourceSharing = 1
    }
}
