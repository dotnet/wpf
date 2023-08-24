// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   This class implements IDataTransform for the Compression transform.
//
//
//
//
//
//
//
//

using System;
using System.Collections;                       // for IDictionary
using System.IO;                                // for Stream
using System.IO.Packaging;
using System.Globalization;                     // for CultureInfo
using System.Windows;                           // ExceptionStringTable

using MS.Internal.IO.Packaging;                 // CompoundFileEmulationStream
using MS.Internal.IO.Packaging.CompoundFile;

//using System.Windows;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// CompressionTransform for use in Compound File DataSpaces
    /// </summary>
    internal class CompressionTransform : IDataTransform
    {
        #region IDataTransform
        /// <summary>
        /// Transform readiness
        /// </summary>
        public bool IsReady
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// No configuration parameters for this transform
        /// </summary>
        public bool FixedSettings
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the type name identifier string.
        /// </summary>
        public object TransformIdentifier
        {
            get
            {
                return CompressionTransform.ClassTransformIdentifier;
            }
        }

        /// <summary>
        /// Expose the transform identifier for the use of the DataSpaceManager.
        /// </summary>
        internal static string ClassTransformIdentifier
        {
            get
            {
                return "{86DE7F2B-DDCE-486d-B016-405BBE82B8BC}";
            }
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Given output stream returns stream for encoding/decoding
        /// </summary>
        /// <param name="encodedStream">the encoded stream that this transform acts on</param>
        /// <param name="transformContext">Dictionary object used to store any additional context information</param>
        /// <returns>the stream that implements the transform</returns>
        /// <remarks>
        /// This method is used only by the DataSpaceManager, so we declare it as an explicit
        /// interface implementation to hide it from the public interface.
        /// </remarks>
        Stream
        IDataTransform.GetTransformedStream(
            Stream encodedStream,
            IDictionary transformContext
            )
        {
            Stream tempStream = new SparseMemoryStream(_lowWaterMark, _highWaterMark);
            tempStream = new CompressEmulationStream(encodedStream, tempStream, 0, new CompoundFileDeflateTransform());

            // return a VersionedStream that works with the VersionedStreamOwner
            // to verify/update our FormatVersion info
            return new VersionedStream(tempStream, _versionedStreamOwner);
        }
        #endregion

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="myEnvironment">environment</param>
        /// <remarks>this should only be used by the DataSpaceManager class</remarks>
        public CompressionTransform(TransformEnvironment myEnvironment)
        {
            _transformEnvironment = myEnvironment;

            // Create a wrapper that manages persistence and comparison of FormatVersion
            // in our InstanceData stream.  We can read/write to this stream as needed (though CompressionTransform
            // does not because we don't house any non-FormatVersion data in the instance data stream).
            // We need to give out our current code version so it can compare with any file version as appropriate.
            _versionedStreamOwner = new VersionedStreamOwner(
                _transformEnvironment.GetPrimaryInstanceData(),
                new FormatVersion(_featureName, _minimumReaderVersion, _minimumUpdaterVersion, _currentFeatureVersion));
        }

        //------------------------------------------------------
        //
        //  Private Data
        //
        //------------------------------------------------------
        private TransformEnvironment _transformEnvironment;
        private VersionedStreamOwner _versionedStreamOwner;     // our instance data stream wrapped
        private static readonly string _featureName = "Microsoft.Metadata.CompressionTransform";

        private static readonly VersionPair _currentFeatureVersion = new VersionPair(1, 0);
        private static readonly VersionPair _minimumReaderVersion = new VersionPair(1, 0);
        private static readonly VersionPair _minimumUpdaterVersion = new VersionPair(1, 0);

        private const long _lowWaterMark = 0x19000;     // we definitely would like to keep everything under 100 KB in memory  
        private const long _highWaterMark = 0xA00000;   // we would like to keep everything over 10 MB on disk
    }
}
