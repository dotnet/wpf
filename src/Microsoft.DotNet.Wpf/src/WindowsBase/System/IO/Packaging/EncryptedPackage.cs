// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class represents an OLE compound file that contains an encrypted package.
//
//
//
//

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Security.RightsManagement;
using System.Windows;
using System.Collections;
using System.Collections.Generic;

using MS.Internal;                  // Invariant.Assert
using MS.Internal.IO.Packaging;
using MS.Internal.IO.Packaging.CompoundFile;    // RightsManagementEncryptionTransform
using MS.Internal.WindowsBase;

namespace System.IO.Packaging
{
    /// <summary>
    /// This class represents an OLE compound file that contains an encrypted package.
    /// </summary>
    public class EncryptedPackageEnvelope : IDisposable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor. Creates an EncryptedPackageEnvelope on a new compound file. The file
        /// is overwritten if it already exists, and it is opened for read-write access
        /// with no sharing.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being created to hold the encrypted package.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed to perform
        /// on the encrypted content.
        /// </param>
        internal
        EncryptedPackageEnvelope(
            string envelopeFileName,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            if (envelopeFileName == null)
                throw new ArgumentNullException("envelopeFileName");

            ThrowIfRMEncryptionInfoInvalid(publishLicense, cryptoProvider);


            _root = StorageRoot.Open(
                                    envelopeFileName,
                                    _defaultFileModeForCreate,
                                    _defaultFileAccess,
                                    _defaultFileShare
                                    );

            InitializeRMForCreate(publishLicense, cryptoProvider);
            EmbedPackage(null);
        }

        /// <summary>
        /// Constructor. Creates an EncryptedPackageEnvelope on the specified stream.
        /// </summary>
        /// <param name="envelopeStream">
        /// The stream on which to create the compound file that will hold the
        /// encrypted package.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed
        /// to perform on the encrypted content.
        /// </param>
        internal
        EncryptedPackageEnvelope(
            Stream envelopeStream,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            if (envelopeStream == null)
                throw new ArgumentNullException("envelopeStream");

            ThrowIfRMEncryptionInfoInvalid(publishLicense, cryptoProvider);

            _root = StorageRoot.CreateOnStream(envelopeStream, _defaultFileModeForCreate);

            //
            // CreateOnStream opens the stream for read access if it's readable, and for
            // read/write access if it's writable. We're going to need it to be writable,
            // so check that it is.
            //
            if (_root.OpenAccess != FileAccess.ReadWrite)
            {
                throw new NotSupportedException(SR.StreamNeedsReadWriteAccess);
            }

            InitializeRMForCreate(publishLicense, cryptoProvider);
            EmbedPackage(null);
        }

        /// <summary>
        /// Constructor. Create an EncryptedPackageEnvelope on the compound file, using
        /// an existing package as the content.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being created to hold the encrypted package.
        /// </param>
        /// <param name="packageStream">
        /// A stream containing an unencrypted package which is to be stored in
        /// the compound file being created in <paramref name="envelopeFileName"/>.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed
        /// to perform on the encrypted content.
        /// </param>
        internal
        EncryptedPackageEnvelope(
            string envelopeFileName, 
            Stream packageStream,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            if (envelopeFileName == null)
                throw new ArgumentNullException("envelopeFileName");

            if (packageStream == null)
                throw new ArgumentNullException("packageStream");

            ThrowIfRMEncryptionInfoInvalid(publishLicense, cryptoProvider);

            _root = StorageRoot.Open(
                                    envelopeFileName,
                                    _defaultFileModeForCreate,
                                    _defaultFileAccess,
                                    _defaultFileShare
                                    );

            InitializeRMForCreate(publishLicense, cryptoProvider);
            EmbedPackage(packageStream);
        }

        /// <summary>
        /// Constructor. Create an EncryptedPackageEnvelope on the specified stream, using
        /// an existing package as the content.
        /// </summary>
        /// <param name="envelopeStream">
        /// The stream on which to create the compound file that will hold the
        /// encrypted package.
        /// </param>
        /// <param name="packageStream">
        /// A stream containing an unencrypted package which is to be stored in
        /// the compound file being created on <paramref name="envelopeStream"/>.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed
        /// to perform on the encrypted content.
        /// </param>
        internal
        EncryptedPackageEnvelope(
            Stream envelopeStream, 
            Stream packageStream,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            if (envelopeStream == null)
                throw new ArgumentNullException("envelopeStream");

            if (packageStream == null)
                throw new ArgumentNullException("packageStream");

            ThrowIfRMEncryptionInfoInvalid(publishLicense, cryptoProvider);

            _root = StorageRoot.CreateOnStream(envelopeStream, _defaultFileModeForCreate);

            //
            // CreateOnStream opens the stream for read access if it's readable, and for
            // read/write access if it's writable. We're going to need it to be writable,
            // so check that it is.
            //
            if (_root.OpenAccess != FileAccess.ReadWrite)
            {
                throw new NotSupportedException(SR.StreamNeedsReadWriteAccess);
            }

            InitializeRMForCreate(publishLicense, cryptoProvider);
            EmbedPackage(packageStream);
        }

        /// <summary>
        /// Constructor. Create an EncryptedPackageEnvelope object by opening the specified
        /// file, which must contain an RM-protected package.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being created to hold the encrypted package.
        /// </param>
        /// <param name="access">
        /// Specifies whether the package is to be opened for read, write, or
        /// read/write access.
        /// </param>
        /// <param name="sharing">
        /// Specifies whether another process can have the file open simultaneously.
        /// </param>
        internal
        EncryptedPackageEnvelope(
            string envelopeFileName,
            FileAccess access,
            FileShare sharing
            )
        {
            if (envelopeFileName == null)
                throw new ArgumentNullException("envelopeFileName");

            _root = StorageRoot.Open(
                                    envelopeFileName,
                                    _defaultFileModeForOpen,
                                    access,
                                    sharing
                                    );

            InitForOpen();
        }

        /// <summary>
        /// Constructor. Create an EncryptedPackageEnvelope object by opening the specified
        /// stream, which must contain an RM-protected package.
        /// </summary>
        /// <param name="envelopeStream">
        /// A stream containing an RM-protected package.
        /// </param>
        internal
        EncryptedPackageEnvelope(
            Stream envelopeStream
            )
        {
            if (envelopeStream == null)
                throw new ArgumentNullException("envelopeStream");

            _root = StorageRoot.CreateOnStream(envelopeStream, _defaultFileModeForOpen);

            InitForOpen();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Create an encrypted package in the specified file with the specified license
        /// information. The file is created if it does not exist and overwritten
        /// if it does exist, it is opened for read/write, and it is opened with
        /// no sharing.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being created to hold the encrypted package.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed to perform
        /// on the encrypted content.
        /// </param>
        public static EncryptedPackageEnvelope
        Create(
            string envelopeFileName,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            return new EncryptedPackageEnvelope(envelopeFileName, publishLicense, cryptoProvider);
        }

        /// <summary>
        /// Create an encrypted package on the specified stream with the specified license
        /// information. The stream  opened for read/write.
        /// </summary>
        /// <param name="envelopeStream">
        /// The stream on which to create the compound file that will hold the
        /// encrypted package.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed to perform
        /// on the encrypted content.
        /// </param>
        public static EncryptedPackageEnvelope
        Create(
            Stream envelopeStream,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            return new EncryptedPackageEnvelope(envelopeStream, publishLicense, cryptoProvider);
        }

        /// <summary>
        /// Create an encrypted package from an existing package.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being created to hold the encrypted package.
        /// </param>
        /// <param name="packageStream">
        /// A stream from which to obtain the clear-text contents of an existing package.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed to perform
        /// on the encrypted content.
        /// </param>
        public static EncryptedPackageEnvelope
        CreateFromPackage(
            string envelopeFileName, 
            Stream packageStream,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            return new EncryptedPackageEnvelope(
                            envelopeFileName,
                            packageStream,
                            publishLicense,
                            cryptoProvider
                            );
        }

        /// <summary>
        /// Create an encrypted package from an existing package.
        /// </summary>
        /// <param name="envelopeStream">
        /// The stream on which to create the compound file that will hold the
        /// encrypted package.
        /// </param>
        /// <param name="packageStream">
        /// A stream from which to obtain the clear-text contents of an existing package.
        /// </param>
        /// <param name="publishLicense">
        /// The publish license to be embedded in the compound file.
        /// </param>
        /// <param name="cryptoProvider">
        /// The object that determines what operations the current user is allowed to perform
        /// on the encrypted content.
        /// </param>
        public static EncryptedPackageEnvelope
        CreateFromPackage(
            Stream envelopeStream, 
            Stream packageStream,
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            return new EncryptedPackageEnvelope(
                            envelopeStream,
                            packageStream,
                            publishLicense,
                            cryptoProvider
                            );
        }

        /// <summary>
        /// Open the encrypted package in the specified compound file. The file
        /// must already exist. The file is opened read-only with no sharing.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being opened.
        /// </param>
        public static EncryptedPackageEnvelope
        Open(
            string envelopeFileName
            )
        {
            return Open(envelopeFileName, _defaultFileAccess, _defaultFileShare);
        }

        /// <summary>
        /// Open the encrypted package in the specified compound file. The file
        /// must already exist. The file is opened for the specified access,
        /// with no sharing.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being opened.
        /// </param>
        /// <param name="access">
        /// Specifies whether the file is to be opened for read, write, or
        /// read/write access.
        /// </param>
        public static EncryptedPackageEnvelope
        Open(
            string envelopeFileName,
            FileAccess access
            )
        {
            return Open(envelopeFileName, access, _defaultFileShare);
        }

        /// <summary>
        /// Open the encrypted package in the specified compound file. The file
        /// must already exist.
        /// </summary>
        /// <param name="envelopeFileName">
        /// The path name of the compound file being opened.
        /// </param>
        /// <param name="access">
        /// Specifies whether the file is to be opened for read, write, or
        /// read/write access.
        /// </param>
        /// <param name="sharing">
        /// Specifies whether another process can have the file open simultaneously.
        /// </param>
        public static EncryptedPackageEnvelope
        Open(
            string envelopeFileName,
            FileAccess access,
            FileShare sharing
            )
        {
            return new EncryptedPackageEnvelope(envelopeFileName, access, sharing);
        }

        /// <summary>
        /// Open the encrypted package in the specified stream, which must
        /// contain an RM-protected package.
        /// </summary>
        /// <param name="envelopeStream">
        /// A stream containing an RM-protected package.
        /// </param>
        public static EncryptedPackageEnvelope
        Open(
            Stream envelopeStream
            )
        {
            return new EncryptedPackageEnvelope(envelopeStream);
        }


        /// <summary>
        /// Probe to see if a file is an RM-protected file. Returns true if the file
        /// is an OLE compound file with the well-known "EncryptedPackage" stream.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file being probed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="fileName"/> is null.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// If the file specified by <paramref name="fileName"/> does not exist.
        /// </exception>
        public static bool
        IsEncryptedPackageEnvelope(
            string fileName
            )
        {
            bool retval = false;

            if (fileName == null)
                throw new ArgumentNullException("fileName");

            StorageRoot root = null;

            try
            {
                //
                // When StorageRoot.Open is called on a file that is not a compound file,
                // it throws an IOException whose inner exception is a COMException whose
                // error code is 0x80030050, STG_E_FILEALREADYEXISTS. Check for that case
                // and return false because that means that this is not an RM-protected file.
                //
                // Any other exception is a real error. For example, StorageRoot.Open
                // throws FileNotFoundException if path does not exist, and we let that
                // flow through.
                //
                root = StorageRoot.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                //
                // It's a compound file. Does it contain an "EncryptedPackage" stream?
                //
                retval = ContainsEncryptedPackageStream(root);
            }
            catch (IOException ex)
            {
                COMException comException = ex.InnerException as COMException;
                if (comException != null && comException.ErrorCode == STG_E_FILEALREADYEXISTS)
                    return false;

                throw;  // Any other kind of IOException is a real error.
            }
            finally
            {
                if (root != null)
                {
                    root.Close();
                }
            }

            return retval;
        }

        /// <summary>
        /// Probe to see if a stream is an RM-protected file. Returns true if the
        /// stream is an OLE compound file with the well-known "EncryptedPackage" stream.
        /// </summary>
        /// <param name="stream">
        /// The stream being probed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="stream"/> is null.
        /// </exception>
        public static bool
        IsEncryptedPackageEnvelope(
            Stream stream
            )
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            bool retval = false;
            StorageRoot root = null;

            try
            {
                //
                // When StorageRoot.CreateOnStream is called on a stream that is not
                // a storage object, it throws an IOException whose inner exception is
                // a COMException whose error code is 0x80030050, STG_E_FILEALREADYEXISTS.
                // Check for that case and return false because that means that this
                // stream is not an RM-protected file.
                //
                // Any other exception is a real error.
                //
                root = StorageRoot.CreateOnStream(stream, FileMode.Open);

                //
                // It's a compound file. Does it contain an "EncryptedPackage" stream?
                //
                retval = ContainsEncryptedPackageStream(root);
            }
            catch (IOException ex)
            {
                COMException comException = ex.InnerException as COMException;
                if (comException != null && comException.ErrorCode == STG_E_FILEALREADYEXISTS)
                    return false;

                throw;  // Any other kind of IOException is a real error.
            }
            finally
            {
                if (root != null)
                {
                    root.Close();
                }
            }

            return retval;
        }

        /// <summary>
        /// Flush the package and the underlying compound file.
        /// </summary>
        public void Flush()
        {
            CheckDisposed();

            //
            // Since _package is only initialized when the client calls GetPackage, it might
            // not be set when the client calls Flush, so we have to check.
            //
            if (_package != null)
            {
                _package.Flush();
            }

            if (_packageStream != null)
            {
                _packageStream.Flush();
            }

            Invariant.Assert(_root != null, "The envelope cannot be null");

            _root.Flush();
        }

        /// <summary>
        /// Close the package and the underlying compound file.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  IDisposable
        //
        //------------------------------------------------------

        /// <summary>
        /// Clean up all resources held by this object (both managed and
        /// unmanaged), and ensure that the resources won't be released a
        /// second time by removing it from the finalization queue.
        /// For this class, Dispose() ensures that the encrypted package
        /// is flushed and closed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            
            GC.SuppressFinalize(this);
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <value>
        /// An object representing the rights-management information stored in
        /// the EncryptedPackageEnvelope, specifically, the PublishLicense and the
        /// UseLicenses stored in the compound file that embodies the RM protected Package.
        /// </value>
        public RightsManagementInformation RightsManagementInformation
        {
            get
            {
                CheckDisposed();
                return _rmi;
            }
        }

        /// <value>
        /// An object representing the core properties (such as Title and Subject)
        /// of the RM-protected document.
        /// </value>
        /// <remarks>
        /// These core properties are stored in the standard OLE property streams
        /// \005SummaryInformation and \005DocumentSummaryInformation, not the ones
        /// that are stored in the package itself. It is the responsibility
        /// of the application to keep the two sets of properties synchronized.
        /// properties.
        /// </remarks>
        public PackageProperties PackageProperties
        {
            get
            {
                CheckDisposed();
                if (_packageProperties == null)
                {
                    _packageProperties = new StorageBasedPackageProperties(_root);
                }
                return _packageProperties;
            }
        }

        /// <value>
        /// Access with which the compound file was opened.
        /// </value>
        public FileAccess FileOpenAccess
        {
            get
            {
                CheckDisposed();
                return _root.OpenAccess;
            }
        }

        /// <summary>
        /// Retrieve the package contained in the compound file.
        /// </summary>
        public Package GetPackage()
        {
            CheckDisposed();

            Invariant.Assert(!_handedOutPackageStream, "Copy of package stream has been already handed out");

            if (_package == null)
            {
                //
                // Open the package on the package stream, again with the same level
                // of access.
                // NOTE: The _packageStream must remain open as long as the _package
                // is open, that is to say, for the life of the EncryptedPackageEnvelope object.
                // Dispose takes care of flushing and closing the stream.
                //
                EnsurePackageStream();

                //We need to inspect the package stream's CanRead and CanWrite properties
                //to determine if we can open the package with the same access mode as 
                //that of the EncryptedPackage. 
                //We will try to open the package with the max possible permissions based
                //on the current EncryptedPackage access and the stream read/write properties.

                FileAccess fileAccessForPackage = 0 ;

                //Convert CanRead to FileAccess.Read
                if (_packageStream.CanRead)
                    fileAccessForPackage |= FileAccess.Read;
                //Convert CanWrite to FileAccess.Write
                if (_packageStream.CanWrite)
                    fileAccessForPackage |= FileAccess.Write;

                //check it against the mode of EncryptedPackage
                fileAccessForPackage &= this.FileOpenAccess;

                 _package = Package.Open(
                    _packageStream,
                    FileMode.Open,
                    fileAccessForPackage);
            }

            _handedOutPackage = true;

            return _package;
        }

        /// <value>
        /// Provides access to compound file streams outside of the encrypted package.
        /// </value>
        public StorageInfo StorageInfo
        {
            get
            {
                CheckDisposed();
                return (StorageInfo)_root;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //   Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Retrieve the package stream contained in the compound file.
        /// </summary>
        internal Stream GetPackageStream() 
        {
            CheckDisposed();

            Invariant.Assert(!_handedOutPackage, "Copy of package has been already handed out");

            EnsurePackageStream();
            _handedOutPackageStream = true;

            if (_package != null)
            {
                try
                {
                    _package.Close();
                }
                finally
                {
                    _package = null;
                }
            }

            return _packageStream;
        }

        #endregion


        //------------------------------------------------------
        //
        //   Internal Methods
        //
        //------------------------------------------------------

        #region Internal Properties

        //
        // Instance name for the encryption transform. This is used in StorageInfo as well.
        //
        internal static string EncryptionTransformName
        {
            get
            {
                return _encryptionTransformName;
            }
        }

        //
        // Name of the stream containing the encrypted content. This is used in StorageInfo as well.
        //
        internal static string PackageStreamName
        {
            get
            {
                return _packageStreamName;
            }
        }

        //Name of the dataspace label. This is used in StorageInfo as well.     
        internal static string DataspaceLabelRMEncryptionNoCompression
        {
            get
            {
                return _dataspaceLabelRMEncryptionNoCompression;
            }
        }
        
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void
        InitializeRMForCreate(
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider
            )
        {
            //
            // Define a data space consisting of a single transform, namely the
            // RightsManagementEncryptionTransform.
            //
            DataSpaceManager dsm = _root.GetDataSpaceManager();

            dsm.DefineTransform(
                    RightsManagementEncryptionTransform.ClassTransformIdentifier,
                    EncryptionTransformName
                    );

            string[] transformStack = new string[1];
            transformStack[0] = EncryptionTransformName;
            _dataSpaceName = DataspaceLabelRMEncryptionNoCompression;
            dsm.DefineDataSpace(transformStack, _dataSpaceName);

            //
            // The call to DefineTransform created a RightsManagementEncryptionTransform
            // object. Obtain this object from the DataSpaceManager, and wrap it in a
            // RightsManagementInformation object. This makes the RM information in the
            // compound file available to the application (through the RightsManagementInformation
            // property), without exposing to the application the implementation detail
            // that there -is- such a thing as a "transform".
            //
            RightsManagementEncryptionTransform rmet =
                dsm.GetTransformFromName(EncryptionTransformName) as RightsManagementEncryptionTransform;

            //
            // We just defined this transform, so it must exist.
            //
            Debug.Assert(
                rmet != null,
                "RightsManagementEncryptionTransform not found"
                );

            _rmi = new RightsManagementInformation(rmet);

            //
            // Prepare the transform object for use.
            //
            rmet.SavePublishLicense(publishLicense);
            rmet.CryptoProvider = cryptoProvider;

            //
            // The transform object is now ready for use. When the data space manager
            // queries the transform's IsReady property, it will return true. So there
            // is no need to sign up for the TransformInitializationEvent.
            //
        }

        private void
        InitForOpen()
        {           
            StreamInfo siPackage = new StreamInfo(_root, PackageStreamName);
            if (!siPackage.InternalExists())
            {
                throw new FileFormatException(SR.PackageNotFound);
            }

            //If the StreamInfo exists we go on to check if correct transform has been
            //applied to the Stream

            DataSpaceManager dsm = _root.GetDataSpaceManager();

            List<IDataTransform> transforms = dsm.GetTransformsForStreamInfo(siPackage);

            RightsManagementEncryptionTransform rmet = null;

            foreach (IDataTransform dataTransform in transforms)
            {
                string id = dataTransform.TransformIdentifier as string;
                if (id != null &&
                        String.CompareOrdinal(id.ToUpperInvariant(),
                            RightsManagementEncryptionTransform.ClassTransformIdentifier.ToUpperInvariant()) == 0)
                {
                    // Do not allow more than one RM Transform
                    if (rmet != null)
                    {
                        throw new FileFormatException(SR.MultipleRightsManagementEncryptionTransformFound);
                    }

                    rmet = dataTransform as RightsManagementEncryptionTransform;
                }
            }

            if (rmet == null)
            {
                throw new FileFormatException(SR.RightsManagementEncryptionTransformNotFound);
            }

            //
            //  There is no reason to further push initialization of the Rights Management 
            //  data (parsing publish / use license). It will add unnecessary costs to the 
            //  scenarios where RM license are not relevant, for example indexing and 
            //  working with document properties            
            //
            
            //
            // Make the rights management information stored in the compound file
            // available to the application through the RightsManagementInformation
            // property.
            //
            _rmi = new RightsManagementInformation(rmet);
        }

        /// <summary>
        /// Determine if the specified StorageRoot contains a stream with the well-
        /// known name that denotes the encrypted package.
        /// </summary>
        /// <param name="root">
        /// The root storage or a compound file.
        /// </param>
        private static bool
        ContainsEncryptedPackageStream(
            StorageRoot root
            )
        {
            return ((new StreamInfo(root, PackageStreamName)).InternalExists());
        }

        /// <summary>
        /// Retrieve the package stream contained in the compound file.
        /// </summary>
        private void EnsurePackageStream()
        {
            if (_packageStream == null)
            {
                StreamInfo siPackage = new StreamInfo(_root, PackageStreamName);
                if (siPackage.InternalExists())
                {
                    //
                    // Open the existing package stream with the same level of access
                    // that the compound file is open.
                    //
                    _packageStream = siPackage.GetStream(FileMode.Open, this.FileOpenAccess);
                }
                else
                {
                    //Error. This package is created in InitForCreate while creating EncryptedPackageEnvelope.
                    //If it does not exist, throw an error.
                    throw new FileFormatException(SR.PackageNotFound);
                }
            }
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        ///
        /// If disposing equals false, the method has been called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        ///
        /// This class has no unmanaged resources, so it has no finalizer,
        /// so this method will only ever be called with disposing=true,
        /// -unless- a subclass overrides Dispose(bool) and calls the
        /// base class method.
        /// </summary>
        /// <param name="disposing">
        /// true if called from Dispose(); false if called from the finalizer.
        /// </param>
        protected virtual void
        Dispose(
            bool disposing
            )
        {
            try
            {
                //
                // If disposing is true, dispose all managed and unmanaged
                // resources. This class has managed resources _package, _root,
                // and _packageProperties that need to be cleaned up.
                //
                if (disposing)
                {
                    try
                    {
                        //
                        // Close the package, if we had it open. It might not be open because we
                        // might have opened the compound file just to look at the properties, and
                        // never even opened the package.
                        //
                        if (_package != null)
                        {
                            _package.Close();
                        }
                    }
                    finally
                    {
                        _package = null;

                        try
                        {
                            if (_packageStream != null)
                            {
                                _packageStream.Close();
                            }
                        }
                        finally
                        {
                            _packageStream = null;

                            try
                            {
                                if (_packageProperties != null)
                                {
                                    _packageProperties.Dispose();
                                }
                            }
                            finally
                            {
                                _packageProperties = null;

                                try
                                {
                                    if (_root != null)
                                    {
                                        _root.Close();
                                    }
                                }
                                finally
                                {
                                    _root = null;
                                }
                            }
                        }
                    }
                }

                //
                // If disposing is false, only clean up unmanaged resources.
                // This class has no unmanaged resources.
                //
            }
            finally
            {
                //
                // By setting _disposed = true, we ensure that all future accesses to
                // this object will fail (because all public methods and property accessors
                // call CheckDisposed). Note that we do -not- wrap the entire body of
                // Dispose(bool) (this method) in an "if (!_disposed)". This is safe
                // because we set each reference to null immediately after attempting
                // to release it, so we never attempt to release any reference more
                // than once.
                //
                _disposed = true;         
            }
        }

        private void
        CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, SR.EncryptedPackageEnvelopeDisposed);
        }

        /// <summary>
        /// Create a stream in the "encrypted" data space to hold the package
        /// contents, and copy the package into that stream.
        /// </summary>
        /// <param name="packageStream">
        /// A stream containing an unencrypted package which is to be stored in
        /// the compound file.
        /// </param>
        private void
        EmbedPackage(Stream packageStream)
        {
            StreamInfo siPackage = new StreamInfo(_root, PackageStreamName);

            Debug.Assert(!siPackage.InternalExists());

            //
            // Create a stream to hold the document content. Create it in the
            // dataspace containing the RightsManagementEncryptionTransform. This
            // will cause the compound file code (specifically, the DataSpaceManager)
            // to create a RightsManagementEncryptionTransform object, and then
            // to trigger the TransformInitializationEvent, which will allow us to
            // retrieve the transform object.
            //
             _packageStream = siPackage.Create(
                                                FileMode.Create,
                                                _root.OpenAccess,
                                                _dataSpaceName
                                                );

            if (packageStream != null)
            {
                //copy the stream

                PackagingUtilities.CopyStream(packageStream, _packageStream,
                                                Int64.MaxValue, /*bytes to copy*/
                                                4096 /*buffer size */);
                _package = Package.Open(_packageStream, FileMode.Open, this.FileOpenAccess);
            }
            else
            {
                //
                // Create the package on the package stream
                //                   
                _package = Package.Open(_packageStream, FileMode.Create, FileAccess.ReadWrite);
                _package.Flush();
                _packageStream.Flush();
            }
        }

        private void ThrowIfRMEncryptionInfoInvalid(
            PublishLicense publishLicense,
            CryptoProvider cryptoProvider)
        {
            if (publishLicense == null)
                throw new ArgumentNullException("publishLicense");

            if (cryptoProvider == null)
                throw new ArgumentNullException("cryptoProvider");
}

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _disposed;
        private bool _handedOutPackage;
        private bool _handedOutPackageStream;

        // Resources managed by this class.
        private StorageRoot                     _root;
        private Package                         _package;

        private string                          _dataSpaceName;
        private Stream                          _packageStream;
        private StorageBasedPackageProperties   _packageProperties;
        private RightsManagementInformation     _rmi;

        //
        // Instance name for the encryption transform. This is used in StorageInfo as well.
        //
        private const string _encryptionTransformName = "EncryptionTransform";

        //
        // Name of the stream containing the encrypted content. This is used in StorageInfo as well.
        //
        private const string _packageStreamName = "EncryptedPackage";

        //Name of the dataspace label. This is used in StorageInfo as well.      
        private const string _dataspaceLabelRMEncryptionNoCompression = "RMEncryptionNoCompression";

        private const int STG_E_FILEALREADYEXISTS = unchecked((int)0x80030050);

        private const FileMode   _defaultFileModeForCreate   = FileMode.Create;
        private const FileAccess _defaultFileAccess = FileAccess.ReadWrite;
        private const FileShare  _defaultFileShare  = FileShare.None;

        private const FileMode   _defaultFileModeForOpen   = FileMode.Open;

        #endregion Private Fields
    }
}
