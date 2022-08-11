// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class implements the RM data transform for a compound file.
//
//
//


// Allow use of presharp warning numbers [6518] unknown to the compiler
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Windows;
using System.Security.RightsManagement;

using MS.Internal.IO.Packaging.CompoundFile;
using MS.Internal.Utility;

using CU = MS.Internal.IO.Packaging.CompoundFile.ContainerUtilities;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// This class implements the IDataTransform interface for the transform that
    /// implements RM encryption in a compound file.
    /// </summary>
    internal class RightsManagementEncryptionTransform : IDataTransform
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// <para>
        /// Constructor.
        /// </para>
        /// <para>
        /// Every transform class is required to have a constructor with this signature.
        /// The compound file code invokes this constructor via reflection for each transform 
        /// when it builds the transform stack for a dataspace. The transform object will
        /// use the properties of the <paramref name="transformEnvironment"/> parameter
        /// to locate and extract the transform's "instance data" from the compound file.
        /// </para>
        /// <para>
        /// The instance data for a RightsManagementEncryptionTransform consists of a
        /// PublishLicense object and zero or more UseLicense objects, each associated with
        /// a user.
        /// </para>
        /// </summary>
        internal
        RightsManagementEncryptionTransform(
            TransformEnvironment transformEnvironment
            )
        {
            Debug.Assert(transformEnvironment != null);
            
            Stream instanceDataStream = transformEnvironment.GetPrimaryInstanceData();

            Debug.Assert(instanceDataStream != null, SR.NoPublishLicenseStream);

            _useLicenseStorage = transformEnvironment.GetInstanceDataStorage();

            Debug.Assert(_useLicenseStorage != null, SR.NoUseLicenseStorage);

            // Create a wrapper that manages persistence and comparison of FormatVersion
            // in our InstanceData stream.  We can read/write to this stream as needed (though CompressionTransform
            // does not because we don't house any non-FormatVersion data in the instance data stream).
            // We need to give out our current code version so it can compare with any file version as appropriate.
            _publishLicenseStream = new VersionedStreamOwner(
                instanceDataStream,
                new FormatVersion(FeatureName, MinimumReaderVersion, MinimumUpdaterVersion, CurrentFeatureVersion));
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Read the publish license from the RM transform's primary instance data stream.
        /// </summary>
        /// <returns>
        /// The publish license, or null if the compound file does not contain a publish
        /// license (as it will not, for example, when the compound file is first created).
        /// </returns>
        /// <exception cref="FileFormatException">
        /// If the stream is corrupt, or if the RM instance data in this file cannot be
        /// read by the current version of this class.
        /// </exception>
        internal PublishLicense
        LoadPublishLicense()
        {
            if (_publishLicenseStream.Length <= 0)
                return null;

            // We seek to position 0 but under the covers, the VersionedStream maintains a FormatVersion
            // structure before our logical position zero.
            _publishLicenseStream.Seek(0, SeekOrigin.Begin);

            //
            // Construct a BinaryReader to read the rest of the instance data.
            //
            // Although BinaryReader is IDisposable, we must not Close or Dispose it,
            // as that would close the underlying stream, which we do not own. Simply
            // allowing the BinaryReader to be finalized after it goes out of scope
            // does -not- close the underlying stream.
            //

// Suppress 6518 Local IDisposable object not disposed: 
// Reason: The stream is not owned by the BlockManager, therefore we cannot 
// close the BinaryWriter, as that would Close the stream underneath.
#pragma warning disable 6518
            BinaryReader utf8Reader = new BinaryReader(_publishLicenseStream, Encoding.UTF8);
#pragma warning restore 6518

            //
            // There follows a variable-length header (not to be confused with the physical
            // stream header). This header allows future expansion, in case we want to store
            // something in addition to the publish license in the primary instance data stream
            // for this transform. The first field in the header is the header length in bytes
            // (including the headerLen field itself).
            //
            Int32 headerLen = utf8Reader.ReadInt32();
            if (headerLen < CU.Int32Size)
            {
                throw new FileFormatException(SR.PublishLicenseStreamCorrupt);
            }

            if (headerLen > MaxPublishLicenseHeaderLen)
            {
                throw new FileFormatException(
                                SR.Format(SR.PublishLicenseStreamHeaderTooLong,
                                headerLen,
                                MaxPublishLicenseHeaderLen
                                ));
            }

            //
            // Save any additional bytes in the header that we don't recognize, so we can
            // write them back out later if necessary. We've already read the headerLen field,
            // so subtract the size of that field from the amount we have to save.
            //
            // No need to use checked{} here since we already made sure that header length is greater than Int32Size
            Int32 numPublishLicenseHeaderExtraBytes = headerLen - CU.Int32Size;
            if (numPublishLicenseHeaderExtraBytes > 0)
            {
                _publishLicenseHeaderExtraBytes = new byte [numPublishLicenseHeaderExtraBytes];
                if (PackagingUtilities.ReliableRead(_publishLicenseStream, _publishLicenseHeaderExtraBytes, 0, numPublishLicenseHeaderExtraBytes)
                        != numPublishLicenseHeaderExtraBytes)
                {
                    throw new FileFormatException(SR.PublishLicenseStreamCorrupt);
                }
            }

            //
            // Read the publish license as a length-prefixed UTF-8 string. If the stream
            // is shorter than the length prefix implies, an exception will be thrown.
            //
            _publishLicense = new PublishLicense(
                                        ReadLengthPrefixedString(
                                            utf8Reader,
                                            Encoding.UTF8,
                                            PublishLicenseLengthMax
                                            )
                                        );

            return _publishLicense;
        }
        
        /// <summary>
        /// Save the publish license to the RM transform's instance data stream.
        /// </summary>
        /// <param name="publishLicense">
        /// The publish licence to be saved. The RM server returns a publish license as a string.
        /// </param>
        /// <remarks>
        /// The stream is rewritten from the beginning, so any existing publish license is
        /// overwritten.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="publishLicense"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the existing RM instance data in this file cannot be updated by the current version
        /// of this class.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the transform settings are fixed.
        /// </exception>
        internal void
        SavePublishLicense(
            PublishLicense publishLicense
            )
        {
            if (publishLicense == null)
            {
                throw new ArgumentNullException("publishLicense");
            }

            if (_fixedSettings)
            {
                throw new InvalidOperationException(SR.CannotChangePublishLicense);
            }

            // We seek to position 0 but under the covers, the VersionedStream maintains a FormatVersion
            // structure before our logical position zero.
            _publishLicenseStream.Seek(0, SeekOrigin.Begin);

            //
            // Construct a BinaryWriter to write the rest of the instance data.
            //
            // Although BinaryWriter is IDisposable, we must not Close or Dispose it,
            // as that would close the underlying stream, which we do not own. Simply
            // allowing the BinaryWriter to be finalized after it goes out of scope
            // does -not- close the underlying stream.
            //

// Suppress 6518 Local IDisposable object not disposed: 
// Reason: The stream is not owned by the BlockManager, therefore we cannot 
// close the BinaryWriter, as that would Close the stream underneath.
#pragma warning disable 6518
            BinaryWriter utf8Writer = new BinaryWriter(_publishLicenseStream, Encoding.UTF8);
#pragma warning restore 6518

            //
            // There follows a variable-length header (not to be confused with the physical
            // stream header). This header allows future expansion, in case we want to store
            // something in addition to the publish license in the primary instance data stream
            // for this transform. In this version, there is no additional information, so the
            // header consists only of the headerLen field itself, so its length is 4 bytes.
            //
            //
            // If we have previously read in the publish license stream from a file whose format
            // included extra header bytes that we didn't interpret, write those bytes back out
            // (and include them in the header length).
            //
            Int32 headerLen = CU.Int32Size;
            if (_publishLicenseHeaderExtraBytes != null)
            {
                checked { headerLen += _publishLicenseHeaderExtraBytes.Length; }
            }
            utf8Writer.Write(headerLen);

            if (_publishLicenseHeaderExtraBytes != null)
            {
                _publishLicenseStream.Write(
                    _publishLicenseHeaderExtraBytes,
                    0, 
                    _publishLicenseHeaderExtraBytes.Length
                    );
            }

            // 
            // Write out the publish license as a length-prefixed, UTF-8 encoded string.
            //
            WriteByteLengthPrefixedDwordPaddedString(publishLicense.ToString(), utf8Writer, Encoding.UTF8);

            utf8Writer.Flush();

            _publishLicense = publishLicense;
        }

        /// <summary>
        /// Load a use license for the specified user from the RM transform's instance data
        /// storage in the compound file.
        /// </summary>
        /// <param name="user">
        /// The user whose use license is desired.
        /// </param>
        /// <returns>
        /// The use license for the specified user, or null if the compound file does not
        /// contain a use license for the specified user.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="user"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the RM information in this file cannot be read by the current version of
        /// this class.
        /// </exception>
        internal UseLicense
        LoadUseLicense(
            ContentUser user
            )
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            LoadUseLicenseForUserParams param = new LoadUseLicenseForUserParams(user);

            EnumUseLicenseStreams(
                new UseLicenseStreamCallback(this.LoadUseLicenseForUser),
                param
                );

            return param.UseLicense;
        }

        /// <summary>
        /// Save a use license for the specified user into the RM transform's instance data
        /// storage in the compound file.
        /// </summary>
        /// <param name="user">
        /// The user to whom the use license was issued.
        /// </param>
        /// <param name="useLicense">
        /// The use license issued to that user.
        /// </param>
        /// <remarks>
        /// Any existing use license for the specified user is removed from the compound
        /// file before the new use license is saved.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="user"/> or <paramref name="useLicense"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the RM information in this file cannot be written by the current version of
        /// this class.
        /// </exception>
        internal void
        SaveUseLicense(
            ContentUser user,
            UseLicense useLicense
            )
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (useLicense == null)
            {
                throw new ArgumentNullException("useLicense");
            }

            if (user.AuthenticationType != AuthenticationType.Windows &&
                user.AuthenticationType != AuthenticationType.Passport)
            {
                throw new ArgumentException(
                    SR.OnlyPassportOrWindowsAuthenticatedUsersAreAllowed,
                    "user"
                    );
            }

            //
            // Delete any existing use license for this user.
            //
            EnumUseLicenseStreams(
                new UseLicenseStreamCallback(this.DeleteUseLicenseForUser),
                user
                );

            //
            // Save the new use license for this user in a new stream.
            //
            SaveUseLicenseForUser(user, useLicense);
        }

        /// <summary>
        /// Delete the use license for the specified user from the RM transform's instance
        /// data storage in the compound file.
        /// </summary>
        /// <param name="user">
        /// The user whose use license is to be deleted.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="user"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the RM information in this file cannot be updated by the current version of
        /// this class.
        /// </exception>
        internal void
        DeleteUseLicense(
            ContentUser user
            )
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            EnumUseLicenseStreams(
                new UseLicenseStreamCallback(this.DeleteUseLicenseForUser),
                user
                );
        }

        /// <summary>
        /// This method retrieves a reference to a dictionary with keys of type User and values
        /// of type UseLicense, containing one entry for each use license embedded in the compound
        /// file for this particular transform instance. The collection is a snapshot of the use
        /// licenses in the compound file at the time of the call. The term "Embedded" in the method
        /// name emphasizes that the dictionary returned by this method only includes those use
        /// licenses that are embedded in the compound file. It does not include any other use
        /// licenses that the application might have acquired from an RM server but not yet embedded
        /// into the  compound file. 
        /// </summary>
        internal IDictionary<ContentUser, UseLicense>
        GetEmbeddedUseLicenses()
        {
            UserUseLicenseDictionaryLoader loader = new UserUseLicenseDictionaryLoader (this);
            return new ReadOnlyDictionary<ContentUser, UseLicense>(loader.LoadedDictionary);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        #region IDataTransform Properties

        /// <value>
        /// Returns a value indicating whether this transform is ready for use.
        /// </value>
        public bool IsReady
        {
            get
            {
                return      (_cryptoProvider != null) 
                            && 
                                (_cryptoProvider.CanDecrypt ||_cryptoProvider.CanEncrypt);
            }
        }

        /// <value>
        /// <para>
        /// Returns true when the transform expects no further changes to its state.
        /// The contract is that if FixedSettings is false, an application can change
        /// any of the transform’s properties with the promise that an exception will
        /// not be thrown.
        /// </para>
        /// <para>
        /// For the RightsManagementEncryptionTransform, FixedSettings becomes true the
        /// first time the compound file code calls the object’s GetTransformedStream method.
        /// After that, any attempt to set the CryptoProvider property, or to call
        /// SavePublicLicense, throws InvalidOperationException.
        /// </para>
        /// </value>
        public bool FixedSettings 
        {
            get
            {
                return _fixedSettings; 
            }
        }

        /// <value>
        /// Returns a value that tells the Data Space Manager how to interpret the
        /// value of this transform's TransformIdentifierProperty.
        /// </value>
        /// <remarks>
        /// The original design of the Data Space Manager allowed for 3rd parties to
        /// implement their own transform classes. These transforms might be identified
        /// by the assembly-qualified name of the managed class that implements the
        /// transform, or by the CLSID of the COM object that implements the transform.
        /// A transform's TransformIdentifierType property was intended to tell the Data
        /// Space Manager whether the transform's TransformIdentifier property was to
        /// be interpreted as a managed class name, a CLSID, or something else. The only
        /// value of TransformIdentifierType that the Data Space Manager actually supports
        /// is TransformIdentifierTypes_PredefinedTransformName, which tells the Data
        /// Space Manager that the TransformIdentifier is one of a small number of well-
        /// known strings that identify the built-in transforms (compression and encryption).
        /// </remarks>
        internal int TransformIdentifierType 
        {   
            get
            {
                return DataSpaceManager.TransformIdentifierTypes_PredefinedTransformName;
            }
        }

        /// <value>
        /// Returns a value that identifies the transform implemented by this object.
        /// </value>
        public object TransformIdentifier 
        {
            get
            {
                return RightsManagementEncryptionTransform.ClassTransformIdentifier;
            }
        }

        #endregion IDataTransform Properties
        
        #region RightsManagementEncryptionTransform Properties 

        /// <value>
        /// This property represents the CryptoProvider object that will be used to determine
        /// what operations the current user is allowed to perform on the encrypted content.
        /// </value>
        internal CryptoProvider CryptoProvider
        {
            get
            {
                return _cryptoProvider;
            }
            set
            {
                if (_fixedSettings)
                {
                    throw new InvalidOperationException(SR.CannotChangeCryptoProvider);
                }

                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                
                if (!value.CanEncrypt && !value.CanDecrypt)
                {
                    throw new ArgumentException(SR.CryptoProviderIsNotReady, "value");
                }

                _cryptoProvider = value;
            }
        }

        /// <value>
        /// Expose the transform identifier for the use of the DataSpaceManager.
        /// </value>
        internal static string ClassTransformIdentifier
        {
            get
            {
                return "{C73DFACD-061F-43B0-8B64-0C620D2A8B50}";
            }
        }

        #endregion RightsManagementEncryptionTransform Properties

        #endregion Public Properties

        //------------------------------------------------------
        //
        // Interface Implementation Methods
        //
        //------------------------------------------------------

        #region Interface Implementation Methods

        /// <summary>
        /// Creates a stream into which the compound file code will write cleartext bytes,
        /// and from which it will read cleartext bytes. When the compound file code writes
        /// bytes to this stream, they will be encrypted and then written to the underlying
        /// stream (encodedStream). When the compound file code reads bytes from this
        /// stream, they will be read from the underlying stream and then decrypted.
        /// </summary>
        /// <param name="encodedStream">
        /// The underlying stream, into which encrypted bytes are written and from which
        /// encrypted bytes are read.
        /// </param>
        /// <param name="transformContext">
        /// No longer used. In the past, this dictionary was used to communicate information
        /// such as the contents of the publish license to clients. Arbitrary key/value pairs
        /// could be written into the dictionary. The Stream-derived class returned by this
        /// method was expected to expose IDictionary, and the dictionary methods were expected
        /// to access the information in the transformContext. This mechanism is no longer needed.
        /// The POR is that this parameter will be removed.
        /// </param>
        /// <returns>
        /// The stream into which the compound file code writes cleartext bytes.
        /// </returns>
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
            //
            // The compound file code shouldn't be calling us until it's made us ready.
            //
            Debug.Assert(this.IsReady);

            //
            // After a stream has been handed out, we can't change settings any more.
            //
            _fixedSettings = true;

            Stream s = new RightsManagementEncryptedStream(encodedStream, _cryptoProvider);

            // let the versioned stream update our FormatVersion information automatically
            return new VersionedStream(s, _publishLicenseStream);
        }

        #endregion Interface Implementation Methods

        //------------------------------------------------------
        //
        // Internal Nested Types
        //
        //------------------------------------------------------

        #region Internal Nested Types

        /// <summary>
        /// Delegate type used by EnumUseLicenseStreams.
        /// </summary>
        internal delegate void
        UseLicenseStreamCallback(
            RightsManagementEncryptionTransform rmet,
            StreamInfo si,
            object param,
            ref bool stop
            );

        #endregion Internal Nested Types

        //------------------------------------------------------
        //
        // Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Enumerate the use license streams in the compound file, invoking a caller-
        /// supplied delegate for each one.
        /// </summary>
        /// <param name="callback">
        /// The delegate to be invoked for each use license stream.
        /// </param>
        /// <param name="param">
        /// A caller-supplied parameter to be passed to the callback. Can be null if
        /// the callback requires no additional information.
        /// </param>
        /// <remarks>
        /// The use license streams are all the streams in the use license storage
        /// whose names begin with a certain prefix.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the RM information in this file cannot be read by the current version of
        /// this class.
        /// </exception>
        internal void
        EnumUseLicenseStreams(
            UseLicenseStreamCallback callback,
            object param
            )
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            bool stop = false;

            foreach (StreamInfo si in _useLicenseStorage.GetStreams())
            {
                // Stream names: we preserve casing, but do case-insensitive comparison (Native CompoundFile API behavior)
                if (String.CompareOrdinal(
                                LicenseStreamNamePrefix.ToUpperInvariant(), 0,
                                si.Name.ToUpperInvariant(), 0,
                                LicenseStreamNamePrefixLength
                                ) == 0)
                {
                    callback(this, si, param, ref stop);
                    if (stop)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Load the use license, and the user to whom it was issued, from the specified
        /// stream.
        /// </summary>
        /// <param name="utf8Reader">
        /// The UTF 8 BinaryReader from which the use license and user are to be loaded.
        /// </param>
        /// <param name="user">
        /// The user specified in the stream.
        /// </param>
        /// <returns>
        /// The use license from the stream.
        /// </returns>
        /// <remarks>
        /// This method is internal rather than private because it is used by
        /// UserUseLicenseDictionaryLoader.AddUseLicenseFromStreamToDictionary.
        /// </remarks>
        internal UseLicense
        LoadUseLicenseAndUserFromStream(
            BinaryReader utf8Reader,
            out ContentUser user
            )
        {
            utf8Reader.BaseStream.Seek(0, SeekOrigin.Begin);

            //
            // The stream begins with a header of the following format:
            //
            //      Int32   headerLength
            //      Int32   userNameLen
            //      Byte    userName[userNameLen]
            //
            // ... and then continues with:
            //
            //      In32    useLicenseLen
            //      Byte    useLicense[useLicenseLen];
            //
            Int32 headerLength = utf8Reader.ReadInt32();
            if (headerLength < UseLicenseStreamLengthMin)
            {
                throw new FileFormatException(SR.UseLicenseStreamCorrupt);
            }

            //
            // The type-prefixed user name string (e.g., "windows:domain\alias") was
            // treated as a sequence of little-Endian UTF-16 characters. The octet
            // sequence representing those characters in that encoding were Base-64 
            // encoded. The resulting character string was then UTF-8 encoded. The
            // resulting byte-length-prefixed UTF-8 byte sequence is what was stored
            // in the use license stream.
            //
            string base64UserName = ReadLengthPrefixedString(utf8Reader, Encoding.UTF8, UserNameLengthMax);
            byte[] userNameBytes = Convert.FromBase64String(base64UserName);

            string typePrefixedUserName =
                        new string(
                            _unicodeEncoding.GetChars(userNameBytes)
                            );

            //
            // Create and return the user object specified by the type-prefixed name.
            // If the type-prefixed name is not in a valid format, a FileFormatException
            // will be thrown.
            //
            AuthenticationType authenticationType;
            string userName;
            ParseTypePrefixedUserName(typePrefixedUserName, out authenticationType, out userName);
            user = new ContentUser(userName, authenticationType);

            //
            // Read the use license as a length-prefixed string, and return it. If the stream
            // is shorter than the length prefix implies, an exception will be thrown.
            //
            return new UseLicense(
                            ReadLengthPrefixedString(utf8Reader, Encoding.UTF8, UseLicenseLengthMax)
                            );
        }

        /// <summary>
        /// Callback function used by LoadUseLicense. Called once for each use license
        /// stream in the compound file. Extracts the use license for the specified
        /// user.
        /// </summary>
        /// <param name="rmet">
        /// The object that knows how to extract license information from the compound file.
        /// </param>
        /// <param name="si">
        /// A stream containing a user/user license pair.
        /// </param>
        /// <param name="param">
        /// Caller-supplied parameter to EnumUseLicenseStreams. In this case, it is a
        /// LoadUseLicenseForUserParams object.
        /// </param>
        /// <param name="stop">
        /// Set to true if the callback function wants to stop the enumeration. This callback
        /// function never wants to stop the enumeration, so this parameter is not used.
        /// </param>
        private void
        LoadUseLicenseForUser(
            RightsManagementEncryptionTransform rmet,
            StreamInfo si,
            object param,
            ref bool stop
            )
        {
            LoadUseLicenseForUserParams lulfup = param as LoadUseLicenseForUserParams;
            if (lulfup == null)
            {
                throw new ArgumentException(SR.CallbackParameterInvalid, "param");
            }

            ContentUser userDesired = lulfup.User;
            Debug.Assert(userDesired != null);

            ContentUser userFromStream = null;
            using (Stream stream = si.GetStream(FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader utf8Reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    userFromStream = rmet.LoadUserFromStream(utf8Reader);

                    if (userFromStream.GenericEquals(userDesired))
                    {
                        lulfup.UseLicense = rmet.LoadUseLicenseFromStream(utf8Reader);
                        stop = true;
                    }
                }
            }
}

        /// <summary>
        /// Callback function used by SaveUseLicense. Called once for each use license
        /// stream in the compound file. Deletes the use license for the specified
        /// user.
        /// </summary>
        /// <param name="rmet">
        /// The object that knows how the arrangement of license information in the compound file.
        /// </param>
        /// <param name="si">
        /// A stream containing a User-UseLicense pair.
        /// </param>
        /// <param name="param">
        /// Caller-supplied parameter to EnumUseLicenseStreams. In this case, it is a
        /// ContentUser object.
        /// </param>
        /// <param name="stop">
        /// Set to true if the callback function wants to stop the enumeration. This callback
        /// function never wants to stop the enumeration; it wants to located and delete
        /// -all- use license streams for the user specified by <paramref name="param"/>,
        /// even though, if the file was created using our APIs, there will never be
        /// more than one.
        /// </param>
        private void
        DeleteUseLicenseForUser(
            RightsManagementEncryptionTransform rmet,
            StreamInfo si,
            object param,
            ref bool stop
            )
        {
            ContentUser userToDelete = param as ContentUser;
            if (userToDelete == null)
            {
                throw new ArgumentException(SR.CallbackParameterInvalid, "param");
            }

            ContentUser userFromStream = null;
            using (Stream stream = si.GetStream(FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader utf8Reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    userFromStream = rmet.LoadUserFromStream(utf8Reader);
                }
            }

            if (userFromStream.GenericEquals(userToDelete))
            {
                si.Delete();
            }
        }

        /// <summary>
        /// Create a use license stream containing the use license for the specified
        /// user.
        /// </summary>
        /// <param name="user">
        /// The user whose use license is to be saved.
        /// </param>
        /// <param name="useLicense">
        /// The use license for the specified user.
        /// </param>
        /// <remarks>
        /// SaveUseLicense has removed any existing use licenses for this
        /// user before calling this internal function.
        /// </remarks>
        internal void
        SaveUseLicenseForUser(
            ContentUser user,
            UseLicense useLicense
            )
        {
            //
            // Generate a unique name for the use license stream, and create the stream.
            //
            string useLicenseStreamName = MakeUseLicenseStreamName();

            StreamInfo si = new StreamInfo(_useLicenseStorage, useLicenseStreamName);

            // This guarantees a call to Stream.Dispose, which is equivalent to Stream.Close.
            using (Stream licenseStream = si.Create())
            {
                // Create a BinaryWriter on the stream.
                using (BinaryWriter utf8Writer = new BinaryWriter(licenseStream, Encoding.UTF8))
                {
                    //
                    // Construct a type-prefixed user name of the form
                    // "Passport:john_smith@hotmail.com" or "Windows:domain\username",
                    // depending on the authentication type of the user.
                    //
                    string typePrefixedUserName = MakeTypePrefixedUserName(user);

                    //
                    // For compatibility with Office, Base64 encode the type-prefixed user name
                    // for the sake of some minimal obfuscation. The parameters to the
                    // UnicodeEncoding ctor mean: "UTF-16 little-endian, no byte order mark".
                    // Then convert the Base64 characters to UTF-8 encoding.
                    //
                    byte [] userNameBytes = _unicodeEncoding.GetBytes(typePrefixedUserName);
                    string base64UserName = Convert.ToBase64String(userNameBytes);

                    byte [] utf8Bytes = Encoding.UTF8.GetBytes(base64UserName);
                    Int32 utf8ByteLength = utf8Bytes.Length;

                    //
                    // Write out a header preceding the use license. The header is of the form:
                    //      Int32   headerLength
                    //      Int32   userNameLength          (in bytes)
                    //      Byte    userName[userNameLength]
                    //      Byte    paddings
                    Int32 headerLength =
                            checked (
                            2 * CU.Int32Size + 
                            utf8ByteLength +
                            CU.CalculateDWordPadBytesLength(utf8ByteLength));
                
                    utf8Writer.Write(headerLength);
                    utf8Writer.Write(utf8ByteLength);
                    utf8Writer.Write(utf8Bytes, 0, utf8ByteLength);
                    WriteDwordPadding(utf8ByteLength, utf8Writer);

                    //
                    // Write out the use license itself.
                    //
                    WriteByteLengthPrefixedDwordPaddedString(useLicense.ToString(), utf8Writer, Encoding.UTF8);
                }
            }
        }

        //------------------------------------------------------
        //
        // Private Nested Types
        //
        //------------------------------------------------------

        #region Private Nested Types

        /// <summary>
        /// This structure is passed by LoadUseLicense as the callback parameter to
        /// LoadUseLicenseForUser. LoadUseLicense initializes this structure with the
        /// user whose use license is desired. LoadUseLicenseForUser sets the UseLicense
        /// property when and if it encounters a use license for the specified user.
        /// </summary>
        private class LoadUseLicenseForUserParams
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="user">
            /// The user whose use license is desired.
            /// </param>
            internal
            LoadUseLicenseForUserParams(
                ContentUser user
                )
            {
                _user = user;
                _useLicense = null;
            }

            /// <value>
            /// The user whose use license is desired.
            /// </value>
            internal ContentUser User
            {
                get { return _user; }
            }
            
            /// <value>
            /// The use license for the specified user.
            /// </value>
            internal UseLicense UseLicense
            {
                get { return _useLicense; }
                set { _useLicense = value; }
            }

            private ContentUser _user;
            private UseLicense _useLicense;
        }

        #endregion Private Nested Types

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Convert a byte array into a string of printable characters by using each sequence
        /// of 5 bits as an index into a table of printable characters.
        /// </summary>
        /// <param name="bytes">
        /// Array containing the bytes to be base-32 encoded.
        /// </param>
        /// <remarks>
        /// This function dose NOT produce a proper Base32Encoding since it will NOT produce proper padding.
        /// </remarks>
        private static char[]
        Base32EncodeWithoutPadding(
            byte[] bytes
            )
        {
            int numBytes = bytes.Length;
            int numBits  = checked (numBytes * 8);
            int numChars = numBits / 5;

            // No need to do checked{} since numChars = numBits / 5 where numBits is int.
            if (numBits % 5 != 0)
                ++numChars;

            char[] chars = new char[numChars];

            for (int iChar = 0; iChar < numChars; ++iChar)
            {
                // Starting bit offset from start of byte array.
                // No need to use checked{} here since iChar cannot be bigger than numChars which cannot be
                //  bigger than (max int / 5)
                int iBitStart = iChar * 5;

                // Index into encoding table.
                int index = 0;

                for (int iBit = iBitStart;
                     iBit - iBitStart < 5 && iBit < numBits;
                     ++iBit)
                {
                    int iByte = iBit / 8;
                    int iBitWithinByte = iBit % 8;

                    if ((bytes[iByte] & (1 << iBitWithinByte)) != 0)
                        index += (1 << (iBit - iBitStart));
                }

                chars[iChar] = Base32EncodingTable[index];
            }

            return chars;
        }

        /// <summary>
        /// Create a unique name for a stream to hold a use license, of the form
        /// "EUL-&lt;Base32-encoded GUID>".
        /// </summary>
        private static string MakeUseLicenseStreamName()
        {
            return LicenseStreamNamePrefix +
                       new string(Base32EncodeWithoutPadding(Guid.NewGuid().ToByteArray()));
        }

        /// <summary>
        /// Construct a type-prefixed user name of the form "Passport:john_smith@hotmail.com"
        /// or "Windows:domain\username", depending on the authentication type of the User.
        /// </summary>
        /// <param name="user">
        /// The user whose type-prefixed name is to be constructed.
        /// </param>
        private static string
        MakeTypePrefixedUserName(
            ContentUser user
            )
        {
            return string.Create(null, stackalloc char[128], $"{user.AuthenticationType}:{user.Name}");
        }

        /// <summary>
        /// Parse a type-prefixed user name of the form "Passport:john_smith@hotmail.com"
        /// or "Windows:domain\username" into its "authentication type" and "user name"
        /// components (the parts before and after the colon, respectively).
        /// </summary>
        /// <param name="typePrefixedUserName">
        /// The string to be parsed.
        /// </param>
        /// <param name="authenticationType">
        /// Specifies whether the string represents a Windows or Passport user ID.
        /// </param>
        /// <param name="userName">
        /// The user's ID.
        /// </param>
        private static void
        ParseTypePrefixedUserName(
            string typePrefixedUserName,
            out AuthenticationType authenticationType,
            out string userName
            )
        {
            //
            // We don't actually know the authentication type yet, and we might find that
            // the type-prefixed user name doesn't even specify a valid authentication
            // type. But we have to assign to authenticationType because it's an out
            // parameter.
            //
            authenticationType = AuthenticationType.Windows;

            int colonIndex = typePrefixedUserName.IndexOf(':');
            if (colonIndex < 1 || colonIndex >= typePrefixedUserName.Length - 1)
            {
                throw new FileFormatException(SR.InvalidTypePrefixedUserName);
            }

            // No need to use checked{} here since colonIndex cannot be >= to (max int - 1)
            userName = typePrefixedUserName.Substring(colonIndex + 1);

            string authenticationTypeString = typePrefixedUserName.Substring(0, colonIndex);
            bool validEnum = false;

            // user names: case-insensitive comparison
            if (((IEqualityComparer) CU.StringCaseInsensitiveComparer).Equals(
                    authenticationTypeString,
                    Enum.GetName(typeof(AuthenticationType), AuthenticationType.Windows)))
            {
                authenticationType = AuthenticationType.Windows;
                validEnum = true;
            }
            else if (((IEqualityComparer) CU.StringCaseInsensitiveComparer).Equals(
                    authenticationTypeString,
                    Enum.GetName(typeof(AuthenticationType), AuthenticationType.Passport)))
            {
                authenticationType = AuthenticationType.Passport;
                validEnum = true;
            }

            //
            // Didn't find a matching enumeration constant.
            //
            if (!validEnum)
            {
                throw new FileFormatException(
                                SR.Format(
                                    SR.InvalidAuthenticationTypeString,
                                    typePrefixedUserName
                                    )
                                );
            }
        }

        /// <summary>
        /// Load the use license from the specified stream.
        /// </summary>
        /// <param name="utf8Reader">
        /// The Utf 8 BinaryReader from which the use license is to be loaded.
        /// </param>
        /// <returns>
        /// The use license from the stream.
        /// </returns>
        /// <remarks>
        /// For details of the stream format, see the comments in LoadUseLicenseAndUserFromStream.
        /// </remarks>
        private UseLicense
        LoadUseLicenseFromStream(
            BinaryReader utf8Reader
            )
        {
            utf8Reader.BaseStream.Seek(0, SeekOrigin.Begin);

            Int32 headerLength = utf8Reader.ReadInt32();
            if (headerLength < UseLicenseStreamLengthMin)
            {
                throw new FileFormatException(SR.UseLicenseStreamCorrupt);
            }

            //
            // Skip over the type-prefixed user name string, because we only want the
            // use license.
            //
            ReadLengthPrefixedString(utf8Reader, Encoding.UTF8, UserNameLengthMax);

            return new UseLicense(
                            ReadLengthPrefixedString(utf8Reader, Encoding.UTF8, UseLicenseLengthMax)
                            );
        }

        /// <summary>
        /// Load the user to whom a use license was issued from the specified stream.
        /// </summary>
        /// <param name="utf8Reader">
        /// The Utf8 BinaryReader from which the user is to be loaded.
        /// </param>
        /// <returns>
        /// The user specified in the stream.
        /// </returns>
        /// <remarks>
        /// For details of the stream format, see the comments in LoadUseLicenseAndUserFromStream.
        /// </remarks>
        private ContentUser
        LoadUserFromStream(
            BinaryReader utf8Reader
            )
        {
            utf8Reader.BaseStream.Seek(0, SeekOrigin.Begin);

            Int32 headerLength = utf8Reader.ReadInt32();
            if (headerLength < UseLicenseStreamLengthMin)
            {
                throw new FileFormatException(SR.UseLicenseStreamCorrupt);
            }

            string base64UserName = ReadLengthPrefixedString(utf8Reader, Encoding.UTF8, UserNameLengthMax);
            byte[] userNameBytes = Convert.FromBase64String(base64UserName);

            string typePrefixedUserName =
                        new string(
                            _unicodeEncoding.GetChars(userNameBytes)
                            );

            //
            // Create and return the user object specified by the type-prefixed name.
            // If the type-prefixed name is not in a valid format, a FileFormatException
            // will be thrown.
            //
            AuthenticationType authenticationType;
            string userName;
            ParseTypePrefixedUserName(typePrefixedUserName, out authenticationType, out userName);
            return new ContentUser(userName, authenticationType);
        }

        /// <summary>
        /// Read a string, encoded according to the specified encoding, and prefixed by the
        /// length in bytes of the encoded string. 
        /// </summary>
        /// <param name="reader">
        /// Binary reader from which the string is read.
        /// </param>
        /// <param name="encoding">
        /// Object that specifies how the string has been encoded.
        /// </param>
        /// <param name="maxLength">
        /// The maximum number of characters that the string can contain. This prevents a malformed
        /// file with a huge length prefix from making us allocate all our memory.
        /// </param>
        private static string
        ReadLengthPrefixedString(
            BinaryReader reader,
            Encoding encoding,
            int maxLength
            )
        {
            Int32 length = reader.ReadInt32();
            if (length > maxLength)
            {
                throw new FileFormatException(SR.Format(SR.ExcessiveLengthPrefix, length, maxLength));
            }

            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
            {
                throw new FileFormatException(SR.InvalidStringFormat);
            }

            string s = encoding.GetString(bytes);

            SkipDwordPadding(bytes.Length, reader);

            return s;
        }

        /// <summary>
        /// Skip past the DWORD padding bytes at the end of a string of the specified length.
        /// </summary>
        /// <param name="length">
        /// Length in bytes of the string that was read.
        /// </param>
        /// <param name="reader">
        /// Binary reader from which the string was read.
        /// </param>
        private static void
        SkipDwordPadding(
            int length,
            BinaryReader reader
            )
        {
            int extra = length % CU.Int32Size;
            if (extra != 0)
            {
                // No need to use checked{} here since we already made sure that extra is smaller than Int32Size
                byte[] bytes = reader.ReadBytes(CU.Int32Size - extra);
                if (bytes.Length != CU.Int32Size - extra)
                {
                    throw new FileFormatException(SR.InvalidStringFormat);
                }
            }
        }
        
        /// <summary>
        /// Write out the number of bytes needed to DWORD align a string of the specified
        /// length. Per the file format spec, the bytes must be 0s.
        /// </summary>
        private static void
        WriteDwordPadding(
            int length,
            BinaryWriter writer
            )
        {
            int extra = length % CU.Int32Size;
            if (extra != 0)
            {
                // No need to use checked{} here since we already made sure that extra is smaller than Int32Size
                writer.Write(Padding, 0, CU.Int32Size - extra);
            }
        }

        /// <summary>
        /// Write out a string in the specified encoding, preceded by the length in bytes
        /// of the encoded string. Pad the string with 0s to a DWORD boundary. The padding
        /// is not included in the length prefix.
        /// </summary>
        private static void
        WriteByteLengthPrefixedDwordPaddedString(
            string s,
            BinaryWriter writer,
            Encoding encoding
            )
        {
            byte[] bytes = encoding.GetBytes(s);
            Int32 length = bytes.Length;

            writer.Write(length);

            //
            // NOTE: If we wrote out the string with "Write(string)", the
            // writer would precede the output with the UTF-8-encoded array
            // length, which we don't want since we need to write the length
            // ourselves. Use Encoding class to get the bytes and call "Write(bytes)
            //  to avoid that problem.
            //
            writer.Write(bytes);

            WriteDwordPadding(length, writer);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private CryptoProvider _cryptoProvider;
        private PublishLicense _publishLicense;
        private bool _fixedSettings;

        //
        // Text encoding object used to read or write other strings used in the file
        // format. (false = little-endian, false = no byte order mark).
        //
        // NOTE: It doesn't always matter which encoding object we use. It matters when
        // we're reading or writing character data, but not when we're writing numeric
        // or byte data.
        //
        private static readonly UnicodeEncoding _unicodeEncoding = new UnicodeEncoding(false, false);

        //
        // The stream in which the FormatVersion and publish license is stored.
        //
        private VersionedStreamOwner _publishLicenseStream;

        //
        // Uninterpreted bytes from the publish license stream header.
        //
        private byte[] _publishLicenseHeaderExtraBytes;

        //
        // The storage under which use licenses are stored.
        //
        private StorageInfo _useLicenseStorage;

        //
        // All use licenses reside in streams whose names begin with this prefix:
        //
        private const string LicenseStreamNamePrefix = "EUL-";
        private static readonly int    LicenseStreamNamePrefixLength = LicenseStreamNamePrefix.Length;

        //
        // The RM version information for the current version of this class.
        //
        private const string FeatureName = "Microsoft.Metadata.DRMTransform";

        //
        //
        // Maximum permitted length, in characters, of a publish license.
        //
        private const int PublishLicenseLengthMax = 1000000;

        //
        // Maximum permitted length, in characters, of a use license.
        //
        private const int UseLicenseLengthMax = 1000000;

        //
        // Maximum permitted length, in characters, of a base-64-encoded type-prefixed user name.
        //
        private const int UserNameLengthMax = 1000;

        //
        // Minimum possible length, in bytes, of the header information in a user license
        // stream. The format of the header is:
        //      Int32   headerLength                    In bytes
        //      Int32   userNameLength                  In bytes
        //      Byte    userName[userNameLength]
        // So the shortest possible header, when userNameLength is 1 byte, is 2 Int32s
        // plus a Byte.
        //
        private static readonly  int UseLicenseStreamLengthMin = 2 * CU.Int32Size + SizeofByte;
        
        private static readonly VersionPair CurrentFeatureVersion = new VersionPair(1,0);

        //
        // The minimum version number that can read the file format that this version of
        // the software writes.
        //
        private static readonly VersionPair MinimumReaderVersion = new VersionPair(1, 0);

        //
        // The minimum version number that can update the file format that this version of
        // the software writes.
        //
        private static readonly VersionPair MinimumUpdaterVersion = new VersionPair(1, 0);

        //
        // Maximum permitted length of the variable-length header in the publish
        // license stream. This limit is a security issue. Since the first 4 bytes
        // of the header specify the header length, and since we will allocate a
        // buffer to hold the header contents, we don't want somebody to give us
        // a malformed file that specifies a header of length 2^31 or so.
        //
        private const int MaxPublishLicenseHeaderLen = 4096;

        private static readonly char[] Base32EncodingTable = {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            '2', '3', '4', '5', '6', '7', '='
            };

        //
        // Used to DWORD-align a stream after writing a string to it:
        //
        private static readonly byte[] Padding = {0, 0, 0};

        private const int SizeofByte  = 1;

        #endregion Private Fields
    }  
} 

