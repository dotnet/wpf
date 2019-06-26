// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:  Crypto provider class which is a wrapper around unmanaged DRM SDK Bound License handle
//   provides ability to Encryt/Decrypt protected content, and enumerate granted rights. 
//
//
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Windows;
using System.Collections.ObjectModel;
using MS.Internal.Security.RightsManagement; 
using MS.Internal;        
using SecurityHelper=MS.Internal.WindowsBase.SecurityHelper; 

using MS.Internal.WindowsBase;

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// CryptoProvider class is built as a result of UseLicense.Bind call. This class represents a successful RightsManagement 
    /// Initialization, and as a result provides Encryption/Decryption services, and exposes list of Bound Grants, which are rights 
    /// that have been given by the publisher to the user, and were properly validated (expiration checks, secure environment, and so on)
    /// </summary>
    public class CryptoProvider : IDisposable
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// This method is responsible for tearing down crypto provider that was built as a result of UseLicense.Bind call.
        /// </summary>        
        ~CryptoProvider()
        {
            Dispose(false);
        }

        /// <summary>
        /// This method is responsible for tearing down crypto provider that was built as a result of UseLicense.Bind call.
        /// </summary>        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

         /// <summary>    
        /// This function encrypts clear text content.
        /// An application is expected to create a padding scheme based on the value returned from 
        /// BlockSize property. BlockSize property should be used to determine the amount of extra 
        /// padding to be added to the clear text. The length, in bytes, of the buffer holding content to 
        /// be encrypted should be a multiple of the block cipher block size. 
        /// RMS system currently uses AES block cipher. All blocks are encrypted independently, so that 2 blocks 
        /// of identical clear text will produce identical results after encryption.  An application 
        /// is encouraged to either compress data prior to encryption or create some other scheme to mitigate 
        /// threats potentially arising from independent block encryption.
        /// </summary> 
        public byte[] Encrypt(byte[] clearText)
        {
            CheckDisposed();
            
            if (clearText == null)
            {
                throw new ArgumentNullException("clearText");
            }

            // validation of the proper size of the clearText is done by the unmanaged libraries 
            
            if (!CanEncrypt)
            {
                throw new RightsManagementException(RightsManagementFailureCode.EncryptionNotPermitted);
            }
            
            // first get the size
            uint outputBufferSize=0;
            byte[] outputBuffer = null;
            int hr;

#if DEBUG
            hr= SafeNativeMethods.DRMEncrypt(
                            EncryptorHandle, 
                            0, 
                            (uint)clearText.Length, 
                            clearText,
                            ref outputBufferSize,
                            null);

            Errors.ThrowOnErrorCode(hr);

            // We do not expect Decryption to change the size of the buffer; otherwise it will break 
            // basic assumptions behind the encrypted compound file envelope format
            Invariant.Assert(outputBufferSize == clearText.Length); 
#else
            outputBufferSize = (uint)clearText.Length;
#endif

            outputBuffer = new byte[outputBufferSize];

            // This will decrypt content
            hr = SafeNativeMethods.DRMEncrypt(
                            EncryptorHandle,
                            0,
                            (uint)clearText.Length,
                            clearText,
                            ref outputBufferSize,
                            outputBuffer);
            Errors.ThrowOnErrorCode(hr);

            return outputBuffer;
        }

        /// <summary>    
        /// This function decrypts cipher text content.
        /// The length, in bytes, of the buffer holding content to be encrypted should be a multiple of the 
        /// block cipher block size. 
        /// </summary>    
        public byte[] Decrypt(byte[] cryptoText)
        {
            CheckDisposed();
        
            if (cryptoText == null)
            {
                throw new ArgumentNullException("cryptoText");
            }

            // validation of the proper size of the cryptoText is done by the unmanaged libraries 
            
            if (!CanDecrypt)
            {
                throw new RightsManagementException(RightsManagementFailureCode.RightNotGranted);
            }
            
            // first get the size
            uint outputBufferSize=0;
            byte[] outputBuffer = null;
            int hr;

#if DEBUG
            hr= SafeNativeMethods.DRMDecrypt(
                            DecryptorHandle,
                            0, 
                            (uint)cryptoText.Length, 
                            cryptoText,
                            ref outputBufferSize,
                            null);
            Errors.ThrowOnErrorCode(hr);

            // We do not expect Decryption changing the size of the buffer; otherwise it will break 
            // basic assumptions behind the encrypted compound file envelope format
            Invariant.Assert(outputBufferSize == cryptoText.Length); 
#else
            outputBufferSize = (uint)cryptoText.Length;
#endif

            outputBuffer = new byte[outputBufferSize];

            // THis will decrypt content
            hr = SafeNativeMethods.DRMDecrypt(
                            DecryptorHandle,
                            0,
                            (uint)cryptoText.Length,
                            cryptoText,
                            ref outputBufferSize,
                            outputBuffer);

            Errors.ThrowOnErrorCode(hr);
            return outputBuffer;
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // close bound license handle which is a DRMHANDLE 
            // Encryptor and Decryptor handles as well
            try
            {
                if (disposing)
                {
                    if (_decryptorHandle != null && !_decryptorHandle.IsInvalid)
                        _decryptorHandle.Close();
                    
                    if (_encryptorHandle != null && !_encryptorHandle.IsInvalid)
                        _encryptorHandle.Close();

                    if (_boundLicenseOwnerViewRightsHandle != null && !_boundLicenseOwnerViewRightsHandle.IsInvalid)
                        _boundLicenseOwnerViewRightsHandle.Close();
                
                    // dispose collection of the bound licenses that we have 
                    if (_boundLicenseHandleList != null)
                    {
                        foreach(SafeRightsManagementHandle boundLicenseHandle in _boundLicenseHandleList)
                        {
                            if (boundLicenseHandle != null && !boundLicenseHandle.IsInvalid) 
                                boundLicenseHandle.Close(); 
                        }
                    }
                }
            }
            finally
            {
                _disposed = true;
                _boundLicenseHandleList = null;
                _boundLicenseOwnerViewRightsHandle = null;
                _decryptorHandle    = null;
                _encryptorHandle    = null;               
            }
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>    
        /// This is bock size of the cypther that is currently in use. 
        /// </summary>    
        public int BlockSize
        {
            get
            {
                CheckDisposed();
            
                if (_blockSize ==0)
                {
                    _blockSize = QueryBlockSize(); 
                }
                return _blockSize; 
            }
        }

        /// <summary>    
        /// This bollean indicates wheter Encrypt Decrypt calls can be made on buffers that are multiple of the BlockSize value.
        /// If the value is false Encrypt Decrypt calls must be made on buffers of the size that is exactly equal to the BlockSize.
        /// </summary>    
        public bool CanMergeBlocks 
        {
            get
            {
                CheckDisposed();
            
                // convention is to return 1 for stream ciphers
                // Stream ciphers (although currently not used) return 1.
                // Block ciphers return size of the block that is always greater than 1.
                // Comparing block size to 1 is a conventional way to differentiate stream and 
                // block ciphers. Block Ciphers can accept data chunks (for both encryption and decryption) 
                // that fall on the block boundary and are a multiple of the block cipher block size. 
                return (BlockSize > 1);
            }
        }
        
        /// <summary>    
        /// This is a read only property. It enable client application to enumerate list of rights that were 
        /// granted to the user, and also passed all the verification checks.
        /// </summary>    
        public ReadOnlyCollection<ContentGrant> BoundGrants
        {
            get
            {
                CheckDisposed();

                if (_boundGrantReadOnlyCollection == null)
                {
                    // we need to enumerate all the rights and keep the ones that we can translate into enum
                    // we ignore the rights that we can not translate for forward compatibility reasons 
                    List<ContentGrant> grantList = new List<ContentGrant>(_boundRightsInfoList.Count);

                    foreach(RightNameExpirationInfoPair rightsInfo in _boundRightsInfoList)
                    {
                        Nullable<ContentRight> contentRight = ClientSession.GetRightFromString(rightsInfo.RightName);

                        if (contentRight != null)
                        {
                            grantList.Add(new ContentGrant(_owner, contentRight.Value, rightsInfo.ValidFrom, rightsInfo.ValidUntil));
                        }
                    }
                    _boundGrantReadOnlyCollection = new ReadOnlyCollection<ContentGrant> (grantList);
                }
                return _boundGrantReadOnlyCollection;
            }
        }


        /// <summary>    
        /// Depending on the set of rights ggranted to the user he or she can do Encryption, Decryption or both.
        /// this property checks whether user was granted rights to encrypt, which means that he or she was granted 
        /// either an Edit or an Owner right.
        /// </summary>    
        public bool CanEncrypt 
        {
            get
            {
                CheckDisposed();

                return (!EncryptorHandle.IsInvalid);
            }
        }
        
        /// <summary>    
        /// Depending on the set of rights granted to the user he or she can do Encryption, Decryption or both.
        /// this property checks whether user was granted rights to decrypt. Decryption is given to a user if he or 
        /// she was able to successfully bind any right (View, Edit, Print, Owner, ....)
        /// </summary>    
        public bool CanDecrypt 
        {
            get
            {
                CheckDisposed();

                return (!DecryptorHandle.IsInvalid);
            }
        }

        //------------------------------------------------------
        //
        //  Internal Constructor 
        //
        //------------------------------------------------------

        /// <summary>
        /// Constructor.
        /// </summary>
        internal CryptoProvider(List<SafeRightsManagementHandle> boundLicenseHandleList, 
                                                        List<RightNameExpirationInfoPair> rightsInfoList,
                                                        ContentUser owner)
        {
            Invariant.Assert(boundLicenseHandleList != null);
            Invariant.Assert(boundLicenseHandleList.Count > 0);

            Invariant.Assert(rightsInfoList != null);
            Invariant.Assert(rightsInfoList.Count > 0);

            // we expect a match between lists of the Right Information and the bound license handles 
            // we will be mapping those lists based on indexes 
            Invariant.Assert(rightsInfoList.Count == boundLicenseHandleList.Count);
                
            Invariant.Assert(owner != null);

            _boundLicenseHandleList = boundLicenseHandleList;
            _boundRightsInfoList = rightsInfoList; 

            _owner = owner;
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        internal UnsignedPublishLicense DecryptPublishLicense(string serializedPublishLicense)
        {
            Invariant.Assert(serializedPublishLicense != null);

            if ((BoundLicenseOwnerViewRightsHandle == null ) || BoundLicenseOwnerViewRightsHandle.IsInvalid)
            {
                throw new RightsManagementException(RightsManagementFailureCode.RightNotGranted);
            }            
            else
            {
                return new UnsignedPublishLicense(BoundLicenseOwnerViewRightsHandle, serializedPublishLicense);
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods  
        //
        //------------------------------------------------------

        /// <summary>
        /// Call this before accepting any API call 
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, SR.Get(SRID.CryptoProviderDisposed));
        }


        private int QueryBlockSize()
        {
            uint attributeSize = 0;
            byte[] dataBuffer = null;
            
            uint encodingType;

            int hr = SafeNativeMethods.DRMGetInfo(DecryptorHandle,
                                                    NativeConstants.QUERY_BLOCKSIZE,
                                                    out encodingType,
                                                    ref attributeSize,
                                                    null);
            Errors.ThrowOnErrorCode(hr);

            // it must return 4 bytes for block size 
            Invariant.Assert(attributeSize == 4);

            dataBuffer = new byte[(int)attributeSize];  // this cast is safe based on the Invariant.Assert above.

            hr = SafeNativeMethods.DRMGetInfo(DecryptorHandle,
                                                NativeConstants.QUERY_BLOCKSIZE,
                                                out encodingType,
                                                ref attributeSize,
                                                dataBuffer);
            Errors.ThrowOnErrorCode(hr);

            return BitConverter.ToInt32(dataBuffer,0);
        }

        //------------------------------------------------------
        //
        //  Private Properties 
        //
        //------------------------------------------------------

        private SafeRightsManagementHandle DecryptorHandle
        {
            get
            {
                if(!_decryptorHandleCalculated)
                {
                    for(int i=0; i< _boundLicenseHandleList.Count; i++)
                    {
                        SafeRightsManagementHandle decryptorHandle = null;

                        int hr = SafeNativeMethods.DRMCreateEnablingBitsDecryptor(
                            _boundLicenseHandleList[i],
                            _boundRightsInfoList[i].RightName,
                            0, 
                            null, 
                            out decryptorHandle);

                        if (hr == 0) 
                        {
                            Debug.Assert(decryptorHandle != null);

                            _decryptorHandle = decryptorHandle;
                             _decryptorHandleCalculated = true;
                             return _decryptorHandle;
                        }
                    }
                    _decryptorHandleCalculated = true; // if we got here it means we couldn't find anything; regardless 
                                                                         // we should still mark this calculation as complete  
                }
                return _decryptorHandle;
            }
        }

        private SafeRightsManagementHandle EncryptorHandle 
        {
            get
            {
                if(!_encryptorHandleCalculated)
                {
                    for(int i=0; i< _boundLicenseHandleList.Count; i++)
                    {
                        SafeRightsManagementHandle encryptorHandle = null;
                        int hr = SafeNativeMethods.DRMCreateEnablingBitsEncryptor(
                            _boundLicenseHandleList[i],
                            _boundRightsInfoList[i].RightName,
                            0,
                            null,
                            out encryptorHandle);

                        if (hr == 0) 
                        {
                            Debug.Assert(encryptorHandle != null);

                            _encryptorHandle = encryptorHandle;
                            _encryptorHandleCalculated = true;
                            return _encryptorHandle; 
                        }
                    }
                    _encryptorHandleCalculated = true; // if we got here it means we couldn't find anything; regardless 
                                                                             // we should still mark this calculation as complete  
                }
                return _encryptorHandle;
            }
        }

   
        private SafeRightsManagementHandle BoundLicenseOwnerViewRightsHandle 
        {
            get
            {
                if(!_boundLicenseOwnerViewRightsHandleCalculated)
                {
                    for(int i=0; i< _boundLicenseHandleList.Count; i++)
                    {
                        Nullable<ContentRight> right = 
                                        ClientSession.GetRightFromString(_boundRightsInfoList[i].RightName);
                            
                        if ((right != null) 
                                &&
                            ((right.Value == ContentRight.Owner) ||(right.Value== ContentRight.ViewRightsData)))
                        {
                            _boundLicenseOwnerViewRightsHandle = _boundLicenseHandleList[i];
                            _boundLicenseOwnerViewRightsHandleCalculated = true;
                            return _boundLicenseOwnerViewRightsHandle; 
                        }
                    }
                    _boundLicenseOwnerViewRightsHandleCalculated = true; // if we got here it means we couldn't find anything; regardless 
                                                                         // we should still mark this calculation as complete  
                }
                return _boundLicenseOwnerViewRightsHandle;
            }
        }
   
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private int _blockSize;

        private SafeRightsManagementHandle _decryptorHandle = SafeRightsManagementHandle.InvalidHandle;            
        private bool _decryptorHandleCalculated;

        private SafeRightsManagementHandle _encryptorHandle = SafeRightsManagementHandle.InvalidHandle;
        private bool _encryptorHandleCalculated;

        private SafeRightsManagementHandle _boundLicenseOwnerViewRightsHandle = SafeRightsManagementHandle.InvalidHandle;
        private bool _boundLicenseOwnerViewRightsHandleCalculated;

        // if this is Invalid, we are disposed
        private List<SafeRightsManagementHandle> _boundLicenseHandleList;
        private List<RightNameExpirationInfoPair> _boundRightsInfoList;
                                                            
        private ReadOnlyCollection<ContentGrant> _boundGrantReadOnlyCollection;
        private ContentUser _owner;

        private bool _disposed;
    }
}
