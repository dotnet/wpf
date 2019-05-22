// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Implements indexing filters for core properties.
//              Invoked by PackageFilter and EncryptedPackageFilter
//              for filtering core properties in Package and
//              EncryptedPackageEnvelope respectively.
//

using System;
using System.Windows;
using System.IO.Packaging;
using MS.Internal.Interop;
using System.Runtime.InteropServices;
using System.Globalization;

namespace MS.Internal.IO.Packaging
{
    #region CorePropertiesFilter

    /// <summary>
    /// Class for indexing filters for core properties.
    /// Implements IManagedFilter to extract property chunks and values
    /// from CoreProperties. 
    /// </summary>
    internal class CorePropertiesFilter : IManagedFilter
    {
        #region Nested types

        /// <summary>
        /// Represents a property chunk.
        /// </summary>
        private class PropertyChunk : ManagedChunk
        {
            internal PropertyChunk(uint chunkId, Guid guid, uint propId)
                : base(
                    chunkId,
                    CHUNK_BREAKTYPE.CHUNK_EOS,
                    new ManagedFullPropSpec(guid, propId),
                    (uint)CultureInfo.InvariantCulture.LCID,
                    CHUNKSTATE.CHUNK_VALUE
                )
            {
            }
        }

        #endregion Nested types

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="coreProperties">core properties to filter</param>
        internal CorePropertiesFilter(PackageProperties coreProperties)
        {
            if (coreProperties == null)
            {
                throw new ArgumentNullException("coreProperties");
            }

            _coreProperties = coreProperties;
        }

        #endregion Constructor

        #region IManagedFilter methods

        /// <summary>
        /// Initialzes the session for this filter.
        /// </summary>
        /// <param name="grfFlags">usage flags</param>
        /// <param name="aAttributes">array of Managed FULLPROPSPEC structs to restrict responses</param>
        /// <returns>IFILTER_FLAGS_NONE. Return value is effectively ignored by the caller.</returns>
        public IFILTER_FLAGS Init(IFILTER_INIT grfFlags, ManagedFullPropSpec[] aAttributes)
        {
            // NOTE: Methods parameters have already been validated by XpsFilter.

            _grfFlags = grfFlags;
            _aAttributes = aAttributes;

            // Each call to Init() creates a new enumerator
            // with parameters corresponding to current Init() call.
            _corePropertyEnumerator = new CorePropertyEnumerator(
                _coreProperties, _grfFlags, _aAttributes);

            return IFILTER_FLAGS.IFILTER_FLAGS_NONE;
        }

        /// <summary>
        /// Returns description of the next chunk.
        /// </summary>
        /// <returns>Chunk descriptor if there is a next chunk, else null.</returns>
        public ManagedChunk GetChunk()
        {
            // No GetValue() call pending from this point on.
            _pendingGetValue = false;

            //
            // Move to the next core property that exists and has a value
            // and create a chunk descriptor out of it.
            //

            if (!CorePropertyEnumerator.MoveNext())
            {
                // End of chunks.
                return null;
            }

            ManagedChunk chunk = new PropertyChunk(
                AllocateChunkID(),
                CorePropertyEnumerator.CurrentGuid,
                CorePropertyEnumerator.CurrentPropId
                );

            // GetValue() call pending from this point on
            // for the current GetChunk call.
            _pendingGetValue = true;

            return chunk;
        }

        /// <summary>
        /// Gets text content corresponding to current chunk.
        /// </summary>
        /// <param name="bufferCharacterCount"></param>
        /// <returns></returns>
        /// <remarks>Not supported in indexing of core properties.</remarks>
        public string GetText(int bufferCharacterCount)
        {
            throw new COMException(SR.Get(SRID.FilterGetTextNotSupported),
                (int)FilterErrorCode.FILTER_E_NO_TEXT);
        }

        /// <summary>
        /// Gets the property value corresponding to current chunk.
        /// </summary>
        /// <returns>Property value</returns>
        public object GetValue()
        {
            // If GetValue() is already called for current chunk,
            // return error with FILTER_E_NO_MORE_VALUES.
            if (!_pendingGetValue)
            {
                throw new COMException(SR.Get(SRID.FilterGetValueAlreadyCalledOnCurrentChunk),
                    (int)FilterErrorCode.FILTER_E_NO_MORE_VALUES);
            }

            // No GetValue() call pending from this point on
            // until another call to GetChunk() is made successfully.
            _pendingGetValue = false;

            return CorePropertyEnumerator.CurrentValue;
        }

        #endregion IManagedFilter methods

        #region Private methods

        /// <summary>
        /// Generates unique and legal chunk ID.
        /// To be called prior to returning a chunk.
        /// </summary>
        /// <remarks>
        /// 0 is an illegal value, so this function never returns 0.
        /// After the counter reaches UInt32.MaxValue, it wraps around to 1.
        /// </remarks>
        private uint AllocateChunkID()
        {
            if (_chunkID == UInt32.MaxValue)
            {
                _chunkID = 1;
            }
            else
            {
                _chunkID++;
            }
            return _chunkID;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Enumerates core properties based on the
        /// core properties and parameters passed in to Init() call.
        /// </summary>
        private CorePropertyEnumerator CorePropertyEnumerator
        {
            get
            {
                if (_corePropertyEnumerator == null)
                {
                    _corePropertyEnumerator = new CorePropertyEnumerator(
                        _coreProperties, _grfFlags, _aAttributes);
                }

                return _corePropertyEnumerator;
            }
        }

        #endregion Properties

        #region Fields

        /// <summary>
        /// IFilter.Init parameters.
        /// Used to initialize CorePropertyEnumerator.
        /// </summary>
        IFILTER_INIT _grfFlags = 0;
        ManagedFullPropSpec[] _aAttributes = null;

        /// <summary>
        /// Chunk ID for the current chunk. Incremented for
        /// every next chunk.
        /// </summary>
        private uint _chunkID = 0;

        /// <summary>
        /// Indicates if GetValue() call is pending
        /// for the current chunk queried using GetChunk().
        /// If not, GetValue() returns FILTER_E_NO_MORE_VALUES.
        /// </summary>
        private bool _pendingGetValue = false;

        /// <summary>
        /// Enumerator used to iterate over the
        /// core properties and create chunk out of them. 
        /// </summary>
        private CorePropertyEnumerator _corePropertyEnumerator = null;

        /// <summary>
        /// Core properties being filtered.
        /// Could be PackageCoreProperties or EncryptedPackageCoreProperties.
        /// </summary>
        private PackageProperties _coreProperties = null;

        #endregion Fields
    }

    #endregion CorePropertiesFilter

    #region CorePropertyEnumerator

    /// <summary>
    /// Enumerator for CoreProperties. Used to iterate through the
    /// properties and obtain their property set GUID, property ID and value.
    /// </summary>
    internal class CorePropertyEnumerator
    {
        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="coreProperties">CoreProperties to enumerate</param>
        /// <param name="grfFlags">
        /// if IFILTER_INIT_APPLY_INDEX_ATTRIBUTES is specified,
        /// this indicates all core properties to be returned unless
        /// the parameter aAttributes is non-empty.
        /// </param>
        /// <param name="attributes">
        /// attributes specified corresponding to the properties to filter.
        /// </param>
        internal CorePropertyEnumerator(PackageProperties coreProperties,
            IFILTER_INIT grfFlags,
            ManagedFullPropSpec[] attributes)
        {
            if (attributes != null && attributes.Length > 0)
            {
                //
                // If attruibutes list specified,
                // return core properties for only those attributes.
                //

                _attributes = attributes;
            }
            else if ((grfFlags & IFILTER_INIT.IFILTER_INIT_APPLY_INDEX_ATTRIBUTES)
                == IFILTER_INIT.IFILTER_INIT_APPLY_INDEX_ATTRIBUTES)
            {
                //
                // If no attributes list specified,
                // but IFILTER_INIT_APPLY_INDEX_ATTRIBUTES is present in grfFlags,
                // return all core properties.
                //

                _attributes = new ManagedFullPropSpec[]
                {
                    //
                    // SummaryInformation
                    //
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.Title),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.Subject),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.Creator),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.Keywords),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.Description),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.LastModifiedBy),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.Revision),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.LastPrinted),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.DateCreated),
                    new ManagedFullPropSpec(FormatId.SummaryInformation, PropertyId.DateModified),

                    //
                    // DocumentSummaryInformation
                    //
                    new ManagedFullPropSpec(FormatId.DocumentSummaryInformation, PropertyId.Category),
                    new ManagedFullPropSpec(FormatId.DocumentSummaryInformation, PropertyId.Identifier),
                    new ManagedFullPropSpec(FormatId.DocumentSummaryInformation, PropertyId.ContentType),
                    new ManagedFullPropSpec(FormatId.DocumentSummaryInformation, PropertyId.Language),
                    new ManagedFullPropSpec(FormatId.DocumentSummaryInformation, PropertyId.Version),
                    new ManagedFullPropSpec(FormatId.DocumentSummaryInformation, PropertyId.ContentStatus)
                };
            }
            else
            {
                // No core properties to be returned.
            }

            _coreProperties = coreProperties;
            _currentIndex = -1;
        }

        #endregion Constructors

        #region Internal methods

        /// <summary>
        /// Move to next property in the enumeration
        /// which exists and has a value.
        /// </summary>
        /// <returns></returns>
        internal bool MoveNext()
        {
            if (_attributes == null)
            {
                return false;
            }

            _currentIndex++;

            // Move to next existing property with value present.
            while (_currentIndex < _attributes.Length)
            {
                if (   _attributes[_currentIndex].Property.PropType == PropSpecType.Id
                    && CurrentValue != null)
                {
                    return true;
                }
                _currentIndex++;
            }

            // End of properties.
            return false;
        }

        #endregion Internal methods

        #region Internal properties

        /// <summary>
        /// Property set GUID for current propery.
        /// </summary>
        internal Guid CurrentGuid
        {
            get
            {
                ValidateCurrent();
                return _attributes[_currentIndex].Guid;
            }
        }

        /// <summary>
        /// Property ID for current property.
        /// </summary>
        internal uint CurrentPropId
        {
            get
            {
                ValidateCurrent();
                return _attributes[_currentIndex].Property.PropId;
            }
        }

        /// <summary>
        /// Value for current property.
        /// </summary>
        internal object CurrentValue
        {
            get
            {
                ValidateCurrent();
                return GetValue(CurrentGuid, CurrentPropId);
            }
        }

        #endregion Internal properties

        #region Private methods

        /// <summary>
        /// Check if the current entry in enumeration is valid.
        /// </summary>
        private void ValidateCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _attributes.Length)
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.CorePropertyEnumeratorPositionedOutOfBounds));
            }
        }

        #endregion

        #region Private properties

        /// <summary>
        /// Get value for the property corresponding to the
        /// property set GUID and property ID.
        /// </summary>
        /// <param name="guid">property set GUID</param>
        /// <param name="propId">property ID</param>
        /// <returns>
        /// property value which could be string or DateTime,
        /// or null if it doesn't exist.
        /// </returns>
        private object GetValue(Guid guid, uint propId)
        {
            if (guid == FormatId.SummaryInformation)
            {
                switch (propId)
                {
                    case PropertyId.Title:
                        return _coreProperties.Title;

                    case PropertyId.Subject:
                        return _coreProperties.Subject;

                    case PropertyId.Creator:
                        return _coreProperties.Creator;

                    case PropertyId.Keywords:
                        return _coreProperties.Keywords;

                    case PropertyId.Description:
                        return _coreProperties.Description;

                    case PropertyId.LastModifiedBy:
                        return _coreProperties.LastModifiedBy;

                    case PropertyId.Revision:
                        return _coreProperties.Revision;

                    case PropertyId.LastPrinted:
                        if (_coreProperties.LastPrinted != null)
                        {
                            return _coreProperties.LastPrinted.Value;
                        }
                        return null;

                    case PropertyId.DateCreated:
                        if (_coreProperties.Created != null)
                        {
                            return _coreProperties.Created.Value;
                        }
                        return null;

                    case PropertyId.DateModified:
                        if (_coreProperties.Modified != null)
                        {
                            return _coreProperties.Modified.Value;
                        }
                        return null;
                }
}
            else if (guid == FormatId.DocumentSummaryInformation)
            {
                switch (propId)
                {
                    case PropertyId.Category:
                        return _coreProperties.Category;

                    case PropertyId.Identifier:
                        return _coreProperties.Identifier;

                    case PropertyId.ContentType:
                        return _coreProperties.ContentType;

                    case PropertyId.Language:
                        return _coreProperties.Language;

                    case PropertyId.Version:
                        return _coreProperties.Version;

                    case PropertyId.ContentStatus:
                        return _coreProperties.ContentStatus;
                }
            }

            // Property/value not found.
            return null;
        }

        #endregion Private properties

        #region Fields

        /// <summary>
        /// Reference to the CorePropeties to enumerate.
        /// </summary>
        private PackageProperties _coreProperties;

        /// <summary>
        /// Indicates the list of properties to be enumerated.
        /// </summary>
        private ManagedFullPropSpec[] _attributes = null;

        /// <summary>
        /// Index of the current property in enumeration.
        /// </summary>
        private int _currentIndex;

        #endregion Fields
    }

    #endregion CorePropertyEnumerator
}
