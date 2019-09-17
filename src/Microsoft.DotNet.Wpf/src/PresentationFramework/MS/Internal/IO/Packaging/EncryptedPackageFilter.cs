// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Implements indexing filter for EncryptedPackageEnvelope.
//              Invoked by XpsFilter if the file/stream being filtered
//              is an EncryptedPackageEnvelope.
//

using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.IO.Packaging;
using MS.Internal.Interop;

namespace MS.Internal.IO.Packaging
{
    #region EncryptedPackageFilter

    /// <summary>
    /// Implements IFilter methods to support indexing on EncryptedPackageEnvelope. 
    /// </summary>
    internal class EncryptedPackageFilter : IFilter
    {
        #region Constructor

        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="encryptedPackage">EncryptedPackageEnvelope to filter on</param>
        internal EncryptedPackageFilter(EncryptedPackageEnvelope encryptedPackage)
        {
            if (encryptedPackage == null)
            {
                throw new ArgumentNullException("encryptedPackage");
            }

            //
            // Since CorePropertiesFilter is implemented as
            // a managed filter (supports IManagedFilter interface),
            // IndexingFilterMarshaler is used to get IFilter interface out of it.
            //
            _filter = new IndexingFilterMarshaler(
                new CorePropertiesFilter(
                encryptedPackage.PackageProperties
                ));
        }

        #endregion Constructor

        #region IFilter methods

        /// <summary>
        /// Initialzes the session for this filter.
        /// </summary>
        /// <param name="grfFlags">usage flags</param>
        /// <param name="cAttributes">number of elements in aAttributes array</param>
        /// <param name="aAttributes">array of FULLPROPSPEC structs to restrict responses</param>
        /// <returns>IFILTER_FLAGS_NONE. Return value is effectively ignored by the caller.</returns>
        public IFILTER_FLAGS Init(
            [In] IFILTER_INIT grfFlags,
            [In] uint cAttributes,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] FULLPROPSPEC[] aAttributes)
        {
            return _filter.Init(grfFlags, cAttributes, aAttributes);
        }

        /// <summary>
        /// Returns description of the next chunk.
        /// </summary>
        /// <returns>Chunk descriptor</returns>
        public STAT_CHUNK GetChunk()
        {
            return _filter.GetChunk();
        }

        /// <summary>
        /// Gets text content corresponding to current chunk.
        /// </summary>
        /// <param name="bufCharacterCount"></param>
        /// <param name="pBuffer"></param>
        /// <remarks>Not supported in indexing of core properties.</remarks>
        public void GetText(ref uint bufCharacterCount, IntPtr pBuffer)
        {
            throw new COMException(SR.Get(SRID.FilterGetTextNotSupported),
                (int)FilterErrorCode.FILTER_E_NO_TEXT);
        }

        /// <summary>
        /// Gets the property value corresponding to current chunk.
        /// </summary>
        /// <returns>property value</returns>
        public IntPtr GetValue()
        {
            return _filter.GetValue();
        }

        /// <summary>
        /// Retrieves an interface representing the specified portion of the object.
        /// </summary>
        /// <param name="origPos"></param>
        /// <param name="riid"></param>
        /// <returns>Not implemented. Reserved for future use.</returns>
        public IntPtr BindRegion([In] FILTERREGION origPos, [In] ref Guid riid)
        {
            throw new NotImplementedException(SR.Get(SRID.FilterBindRegionNotImplemented));
        }

        #endregion IFilter methods

        #region Fields

        /// <summary>
        /// Only filtering that is supported on EncryptedPackageEnvelope 
        /// is of core properties. This points to EncryptedPackageCorePropertiesFilter 
        /// wrapped by FilterMarshaler.
        /// </summary>
        private IFilter _filter = null;

        #endregion Fields
    }

    #endregion EncryptedPackageFilter
}
