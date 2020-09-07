// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Implements indexing filter for XPS documents,
//              which could be Package or EncryptedPackageEnvelope.
//              Uses PackageFilter or EncryptedPackageFilter accordingly.
//
//              code moved from previous implementation of 
//              ContainerFilterImpl and IndexingFilterMarshaler.
//

using System;
using System.IO;
using System.IO.Packaging;
using System.Diagnostics;                       // For Assert
using System.Runtime.InteropServices;           // For Marshal.ThrowExceptionForHR
using System.Globalization;                     // For CultureInfo
using System.Windows;                           // for ExceptionStringTable
using System.Security;                          // For SecurityCritical

using MS.Win32;
using MS.Internal.Interop;                      // For STAT_CHUNK, etc.
using MS.Internal.IO.Packaging;                 // For ManagedIStream
using MS.Internal;

namespace MS.Internal.IO.Packaging
{
    #region XpsFilter

    /// <summary>
    /// Implements IFilter, IPersistFile and IPersistStream methods 
    /// to support indexing on XPS files. 
    /// </summary>
    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [Guid("0B8732A6-AF74-498c-A251-9DC86B0538B0")]
    internal sealed class XpsFilter : IFilter, IPersistFile, IPersistStream
    {
        #region IFilter methods

        /// <summary>
        /// Initialzes the session for this filter.
        /// </summary>
        /// <param name="grfFlags">usage flags</param>
        /// <param name="cAttributes">number of elements in aAttributes array</param>
        /// <param name="aAttributes">array of FULLPROPSPEC structs to restrict responses</param>
        /// <returns>
        /// IFILTER_FLAGS_NONE to indicate that the caller should not use the IPropertySetStorage
        /// and IPropertyStorage interfaces to locate additional properties.
        /// </returns>
        IFILTER_FLAGS IFilter.Init(
            [In] IFILTER_INIT grfFlags,
            [In] uint cAttributes,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] FULLPROPSPEC[] aAttributes)
        {
            if (_filter == null)
            {
                throw new COMException(SR.Get(SRID.FileToFilterNotLoaded),
                    (int)NativeMethods.E_FAIL);
            }

            if (cAttributes > 0 && aAttributes == null)
            {
                // Attributes count and array do not match.
                throw new COMException(SR.Get(SRID.FilterInitInvalidAttributes),
                    (int)NativeMethods.E_INVALIDARG);
            }

            return _filter.Init(grfFlags, cAttributes, aAttributes);
        }

        /// <summary>
        /// Returns description of the next chunk.
        /// </summary>
        /// <returns>Chunk descriptor</returns>
        STAT_CHUNK IFilter.GetChunk()
        {
            if (_filter == null)
            {
                throw new COMException(SR.Get(SRID.FileToFilterNotLoaded),
                    (int)FilterErrorCode.FILTER_E_ACCESS);
            }

            try
            {
                return _filter.GetChunk();
            }
            catch (COMException ex)
            {
                // End-of-data?  If so, release the package.
                if (ex.ErrorCode == (int)FilterErrorCode.FILTER_E_END_OF_CHUNKS)
                    ReleaseResources();

                throw ex;
            }
        }

        /// <summary>
        /// Gets text content corresponding to current chunk.
        /// </summary>
        /// <param name="bufCharacterCount">size of buffer in characters</param>
        /// <param name="pBuffer">buffer pointer</param>
        /// <remarks>Supported for indexing content of Package.</remarks>
        void IFilter.GetText(ref uint bufCharacterCount, IntPtr pBuffer)
        {
            if (_filter == null)
            {
                throw new COMException(SR.Get(SRID.FileToFilterNotLoaded),
                    (int)FilterErrorCode.FILTER_E_ACCESS);
            }

            // NULL is not an acceptable value for pBuffer
            if (pBuffer == IntPtr.Zero)
            {
                throw new NullReferenceException(SR.Get(SRID.FilterNullGetTextBufferPointer));
            }

            // If there is 0 byte to write, this is a no-op.
            if (bufCharacterCount == 0)
            {
                return;
            }

            // Because we should always return the string with null terminator, a buffer size
            // of one character can hold the null terminator only, we can always write the 
            // terminator to the buffer and return directly.
            if (bufCharacterCount == 1)
            {
                Marshal.WriteInt16(pBuffer, 0);
                return;
            }

            // Record the original buffer size. bufCharacterCount may be changed later.
            // The original buffer size will be used to identify a special
            // case later. 
            uint origianlBufferSize = bufCharacterCount;

            // Normalize the buffer size, for a very large size could be due to a bug or an attempted attack.
            if (bufCharacterCount > _maxTextBufferSizeInCharacters)
            {
                bufCharacterCount = _maxTextBufferSizeInCharacters;
            }

            // Memorize the buffer size. 
            // We need to reserve a character for the terminator because we don't know 
            // whether the underlying layer will take care of it.
            uint maxSpaceForContent = --bufCharacterCount;

            // Retrieve the result and its size.
            _filter.GetText(ref bufCharacterCount, pBuffer);

            // An increase in the in/out size parameter would be anomalous, and could be ill-intentioned.
            if (bufCharacterCount > maxSpaceForContent)
            {
                throw new COMException(SR.Get(SRID.AuxiliaryFilterReturnedAnomalousCountOfCharacters),
                    (int)FilterErrorCode.FILTER_E_ACCESS);
            }

            // We need to handle a tricky case if the input buffer size is 2 characters.
            // 
            // In this case, we actually request 1 character from the underlying layer 
            // because we always reserve one character for the terminator. 
            // 
            // There are two possible scenarios for the returned character in the buffer:
            // 1.   If the underlying layer will pad the returning string 
            // with the null terminator, then the returned character in the buffer is null.
            // In this case we cannot return anything useful to the user, which is not expected. 
            // What the users would expect is getting a string with one character 
            // and one null terminator when passing a buffer with size of 2 characters to us. 
            // 2.   If the underlying layer will NOT pad the returning string 
            // with the null terminator, then we have a useful character returned.
            // Then we pad the buffer with string terminator null, and give back to the user.
            // This case meets the users' expectation.
            // 
            // So we need to discover the behavior of the underlying layer and act properly.
            // Following is a solution:
            // 1.   Check the returned character in the buffer.
            //      If it's a null, then we have scenario 1. Goto step 2.
            //      If it's not a null, then we have scenario 2. Goto step 3.
            // 2.   Call the underlying layer's GetText() again, but passing buffer size of 2.
            // 3.   Pad the buffer with null string terminator and return.
            if (origianlBufferSize == 2)
            {
                short shCharacter = Marshal.ReadInt16(pBuffer);
                if (shCharacter == '\0')
                {
                    // Scenario 1. Call underlying layer again with the actual buffer size.
                    bufCharacterCount = 2;
                    _filter.GetText(ref bufCharacterCount, pBuffer);

                    // An increase in the in/out size parameter would be anomalous, and could be ill-intentioned.
                    if (bufCharacterCount > 2)
                    {
                        throw new COMException(SR.Get(SRID.AuxiliaryFilterReturnedAnomalousCountOfCharacters),
                            (int)FilterErrorCode.FILTER_E_ACCESS);
                    }

                    if (bufCharacterCount == 2)
                    {
                        // If the underlying layer GetText() returns 2 characters, we need to check
                        // whether the second character is null. If it's not, then its behavior
                        // does not match the scenario 1, we cannot handle it.
                        shCharacter = Marshal.ReadInt16(pBuffer, _int16Size);

                        // We don't throw exception because such a behavior violation is not acceptable. 
                        // We'd better terminate the entire process.
                        Invariant.Assert(shCharacter == '\0');

                        // Then we adjust the point where we should put our null terminator.
                        bufCharacterCount = 1;
                    }
                    // If the underlying layer GetText() returns 0 or 1 character, we 
                    // don't need to do anything.
                }
            }
            // If the buffer size is bigger than 2, then we don't care the behavior of the 
            // underlying layer. The string buffer we return may contain 2 null terminators
            // if the underlying layer also pads the terminator. But there will be at least one
            // non-null character in the buffer if there is any text to get. So the users will get
            // something useful.
            //
            // One possible proposal is to generalize the special case: why not make the returned
            // string more uniform, in which there is only one terminator always? We discussed this 
            // proposal. To achieve this, we must know the behavior of the underlying layer.
            // We need to call the underlying layer twice. 
            
            // The first call is to request for one character to test the behavior. 
            // If the returned character is null, then the underlying
            // layer is a conforming filter, which will pad a null terminator for the string it 
            // returns. Otherwise, the underlying layer is non-conforming.
            //
            // Suppose the input buffer size is N, then if underlying layer is conforming, we make
            // a second call to it requesting for N characters. Then we can return. 
            // 
            // If the underlying layer is non-conforming, things are tricky. 
            // First, the character returned
            // by the first call is useful and we cannot discard it. We should let it sit at the 
            // beginning of the input buffer. So when we make the second call requesting for (N-2)
            // charaters, we have to use a temporary buffer. The reason is: the input buffer is
            // specified as an IntPtr. We cannot change its offset like a pointer without using
            // unsafe context, which we want to avoid. So we need to copy the characters in the 
            // temporary buffer to the input buffer when the call returns, which might be expensive.
            //
            // Second, a side effect of making 2 calls to the underlying layer
            // is the second call may trigger a FILTER_E_NO_MORE_TEXT exception if the first call
            // exhausts all texts in the stream. We need to catch this exception, otherwise the COM
            // will catch it and return an error HRESULT to the user, which sould not happen. So,
            // we need to add a try-catch block for the second call to the non-conforming underlying 
            // layer, which is expensive.
            //
            // Given the overheads that can incur, we dropped this idea eventhough it provides a
            // cleaner string format returned to the user. If the filter interface requires
            // the underlying filter to provide a property field indicating its behavior, then
            // we can implement this idea much cheaper.

            // Make sure the returned buffer always contains a terminating zero.
            //    Note the conversion of uint to int involves no risk of an arithmetic overflow thanks
            // to the truncations performed above.
            //    Provided pBuffer points to a buffer of size the minimum of _maxTextBufferSizeInCharacters
            // and the initial value of bufCharacterCount, the following write occurs within range.
            Marshal.WriteInt16(pBuffer, (int)bufCharacterCount * _int16Size, 0);

            // Count the terminator in the size that is returned.
            bufCharacterCount++;
        }

        /// <summary>
        /// Gets the property value corresponding to current chunk.
        /// </summary>
        /// <returns>property value</returns>
        /// <remarks>
        /// Supported for indexing core properties
        /// for Package and EncryptedPackageEnvelope.
        /// </remarks>
        IntPtr IFilter.GetValue()
        {
            if (_filter == null)
            {
                throw new COMException(SR.Get(SRID.FileToFilterNotLoaded),
                    (int)FilterErrorCode.FILTER_E_ACCESS);
            }

            return _filter.GetValue();
        }

        /// <summary>
        /// Retrieves an interface representing the specified portion of the object.
        /// </summary>
        /// <param name="origPos"></param>
        /// <param name="riid"></param>
        /// <returns>Not implemented. Reserved for future use.</returns>
        IntPtr IFilter.BindRegion([In] FILTERREGION origPos, [In] ref Guid riid)
        {
            // The following exception maps to E_NOTIMPL.
            throw new NotImplementedException(SR.Get(SRID.FilterBindRegionNotImplemented));
        }

        #endregion IFilter methods

        #region IPersistFile methods

        /// <summary>
        /// Return the CLSID for the XAML filtering component.
        /// </summary>
        /// <param name="pClassID">On successful return, a reference to the CLSID.</param>
        void IPersistFile.GetClassID(out Guid pClassID)
        {
            pClassID = _filterClsid;
        }

        /// <summary>
        /// Return the path to the current working file or the file prompt ("*.xps").
        /// </summary>
        [PreserveSig]
        int IPersistFile.GetCurFile(out string ppszFileName)
        {
            ppszFileName = null;

            if (_filter == null || _xpsFileName == null)
            {
                ppszFileName = "*." + PackagingUtilities.ContainerFileExtension;
                return NativeMethods.S_FALSE;
            }

            ppszFileName = _xpsFileName;
            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Checks an object for changes since it was last saved to its current file.
        /// </summary> 
        /// <returns>
        /// S_OK if the file has changed since it was last saved; 
        /// S_FALSE if the file has not changed since it was last saved.
        /// </returns>
        /// <remarks>
        /// Since the file is accessed only for reading, this function always returns S_FALSE.
        /// </remarks>
        [PreserveSig]
        int IPersistFile.IsDirty()
        {
            return NativeMethods.S_FALSE;
        }

        /// <summary>
        /// Opens the specified file with the specified mode..
        /// This can return any of the STG_E_* error codes, along
        /// with S_OK, E_OUTOFMEMORY, and E_FAIL.
        /// </summary>
        /// <param name="pszFileName">
        /// A zero-terminated string containing the absolute path of the file to open. 
        /// </param>
        /// <param name="dwMode">The mode in which to open pszFileName. </param>
        void IPersistFile.Load(string pszFileName, int dwMode)
        {
            FileMode fileMode;
            FileAccess fileAccess;
            FileShare fileSharing;

            // Check argument.
            if (pszFileName == null || pszFileName == String.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.FileNameNullOrEmpty), "pszFileName");
            }

            // Convert mode information in flag.
            switch ((STGM_FLAGS)(dwMode & (int)STGM_FLAGS.MODE))
            {
                case STGM_FLAGS.CREATE:
                    throw new ArgumentException(SR.Get(SRID.FilterLoadInvalidModeFlag), "dwMode");

                default:
                    fileMode = FileMode.Open;
                    break;
            }

            // Convert access flag.
            switch ((STGM_FLAGS)(dwMode & (int)STGM_FLAGS.ACCESS))
            {
                case STGM_FLAGS.READ:
                case STGM_FLAGS.READWRITE:
                    fileAccess = FileAccess.Read;
                    break;

                default:
                    throw new ArgumentException(SR.Get(SRID.FilterLoadInvalidModeFlag), "dwMode");
            }

            // Sharing flags are ignored. Since managed filters do not have the equivalent
            // of a destructor to release locks on files as soon as they get disposed of from
            // unmanaged code, the option taken is not to lock at all while filtering.
            // (See call to FileToStream further down.)
            fileSharing = FileShare.ReadWrite;

            // Only one of _package and _encryptedPackage can be non-null at a time.
            Invariant.Assert(_package == null || _encryptedPackage == null);

            // If there has been a previous call to Load, reinitialize everything.
            // Note closing a closed stream does not cause any exception.
            ReleaseResources();

            _filter = null;
            _xpsFileName = null;

            bool encrypted = EncryptedPackageEnvelope.IsEncryptedPackageEnvelope(pszFileName);

            try
            {
                // opens to MemoryStream or just returns FileStream if file exceeds _maxMemoryStreamBuffer
                _packageStream = FileToStream(pszFileName, fileMode, fileAccess, fileSharing, _maxMemoryStreamBuffer);

                if (encrypted)
                {
                    // Open the encrypted package.
                    _encryptedPackage = EncryptedPackageEnvelope.Open(_packageStream);
                    _filter = new EncryptedPackageFilter(_encryptedPackage);
                }
                else
                {
                    // Open the package.
                    _package = Package.Open(_packageStream);
                    _filter = new PackageFilter(_package);
                }
            }
            catch (IOException ex)
            {
                throw new COMException(ex.Message, (int)FilterErrorCode.FILTER_E_ACCESS);
            }
            catch (FileFormatException ex)
            {
                throw new COMException(ex.Message, (int)FilterErrorCode.FILTER_E_UNKNOWNFORMAT);
            }
            finally
            {
                // failure?
                if (_filter == null)
                {
                    // clean up
                    ReleaseResources();
                }
}

            _xpsFileName = pszFileName;
        }

        /// <summary>
        /// Saves a copy of the object into the specified file.
        /// </summary>
        /// <param name="pszFileName">
        /// A zero-terminated string containing the absolute path 
        /// of the file to which the object is saved.
        /// </param>
        /// <param name="fRemember">
        /// Indicates whether pszFileName is to be used as the current working file. 
        /// </param>
        /// <remarks>
        /// On the odd chance that this link is still valid when it's needed, 
        /// expected error codes are described at
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/com/html/da9581e8-98c7-4592-8ee1-a1bc8232635b.asp
        /// </remarks>
        void IPersistFile.Save(string pszFileName, bool fRemember)
        {
            throw new COMException(SR.Get(SRID.FilterIPersistFileIsReadOnly), NativeMethods.STG_E_CANTSAVE);
        }

        /// <summary>
        /// Notifies the object that it can write to its file.
        /// </summary>
        /// <param name="pszFileName">
        /// The absolute path of the file where the object was previously saved. 
        /// </param>
        /// <remarks>
        /// On the odd chance that this link is still valid when it's needed, 
        /// expected error codes are described at
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/com/html/da9581e8-98c7-4592-8ee1-a1bc8232635b.asp
        /// This function should always return S_OK when Save is not supported.
        /// </remarks>
        void IPersistFile.SaveCompleted(string pszFileName)
        {
            return; // return S_OK
        }

        #endregion IPersistFile methods

        #region IPersistStream methods

        /// <summary>
        /// Return the CLSID for the XAML filtering component.
        /// </summary>
        /// <param name="pClassID">On successful return, a reference to the CLSID.</param>
        void IPersistStream.GetClassID(out Guid pClassID)
        {
            pClassID = _filterClsid;
        }

        /// <summary>
        /// Checks an object for changes since it was last saved to its current file.
        /// </summary> 
        /// <returns>
        /// S_OK if the file has changed since it was last saved; 
        /// S_FALSE if the file has not changed since it was last saved.
        /// </returns>
        /// <remarks>
        /// Since the file is accessed only for reading, this function always returns S_FALSE.
        /// </remarks>
        [PreserveSig]
        int IPersistStream.IsDirty()
        {
            return NativeMethods.S_FALSE;
        }

        /// <summary>
        /// Retrieve the container on the specified IStream.
        /// </summary>
        /// <param name="stream">The OLE stream from which the container's contents are to be read.</param>
        /// <remarks>
        /// The interface implemented by 'stream' is defined in 
        /// MS.Internal.Interop.IStream rather than the standard
        /// managed so as to allow optimized marshaling in UnsafeIndexingFilterStream.
        /// </remarks>
        void IPersistStream.Load(MS.Internal.Interop.IStream stream)
        {
            // Check argument.
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            // Only one of _package and _encryptedPackage can be non-null at a time.
            Invariant.Assert(_package == null || _encryptedPackage == null);

            // If there has been a previous call to Load, reinitialize everything.
            // Note closing a closed stream does not cause any exception.
            ReleaseResources();

            _filter = null;
            _xpsFileName = null;

            try
            {
                _packageStream = new UnsafeIndexingFilterStream(stream);

                // different filter for encrypted package
                if (EncryptedPackageEnvelope.IsEncryptedPackageEnvelope(_packageStream))
                {
                    // Open the encrypted package.
                    _encryptedPackage = EncryptedPackageEnvelope.Open(_packageStream);
                    _filter = new EncryptedPackageFilter(_encryptedPackage);
                }
                else
                {
                    // Open the package.
                    _package = Package.Open(_packageStream);
                    _filter = new PackageFilter(_package);
                }
            }
            catch (IOException ex)
            {
                throw new COMException(ex.Message, (int)FilterErrorCode.FILTER_E_ACCESS);
            }
            catch (Exception ex)
            {
                throw new COMException(ex.Message, (int)FilterErrorCode.FILTER_E_UNKNOWNFORMAT);
            }
            finally
            {
                // clean-up if we failed
                if (_filter == null)
                {
                    ReleaseResources();
                }
            }
        }

        /// <summary>
        /// Saves a copy of the object into the specified stream.
        /// </summary>
        /// <param name="stream">The stream to which the object is saved. </param>
        /// <param name="fClearDirty">Indicates whether the dirty state is to be cleared. </param>
        /// <remarks>
        /// Expected error codes are described at
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/com/html/b748b4f9-ef9c-486b-bdc4-4d23c4640ff7.asp
        /// </remarks>
        void IPersistStream.Save(MS.Internal.Interop.IStream stream, bool fClearDirty)
        {
            throw new COMException(SR.Get(SRID.FilterIPersistStreamIsReadOnly), NativeMethods.STG_E_CANTSAVE);
        }

        /// <summary>
        /// The purpose of this function when implemented by a persistent object is to return
        /// the size in bytes of the stream needed to save the object.
        /// Always returns COR_E_NOTSUPPORTED insofar as the filter does not use this interface for persistence.
        /// </summary>
        void IPersistStream.GetSizeMax(out Int64 pcbSize)
        {
            throw new NotSupportedException(SR.Get(SRID.FilterIPersistFileIsReadOnly));
        }

        #endregion IPersistStream methods

        #region Private methods

        /// <summary>
        /// Shared implementation for releasing package/encryptedPackage and underlying stream
        /// </summary>
        private void ReleaseResources()
        {
            if (_encryptedPackage != null)
            {
                _encryptedPackage.Close();
                _encryptedPackage = null;
            }
            else if (_package != null)
            {
                _package.Close();
                _package = null;
            }
            if (_packageStream != null)
            {
                _packageStream.Close();
                _packageStream = null;
            }
        }

        /// <summary>
        /// Auxiliary function of IPersistFile.Load.
        /// </summary>
        /// <returns>
        /// <value>A MemoryStream of the package file or a FileStream if the file is too big.</value>
        /// </returns>
        /// <remarks>
        /// <para>Use this method to load a package file completely to a memory buffer. 
        /// After loading the file we can close the file, thus we can release the file
        /// lock quickly. However, there is a size limit on the file. If the 
        /// file is too big (greater than _maxMemoryStreamBuffer), we cannot allow
        /// this method to consume too much memory. So we simply return the fileStream.</para>
        /// <para>Mode, access and sharing have already been checked or adjusted and can be assumed
        /// to be compatible with the goal of reading from the file.</para>
        /// </remarks>
        private static Stream FileToStream(
            string filePath,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileSharing,
            long maxMemoryStream)
        {
            FileInfo fi = new FileInfo(filePath);
            long byteCount = fi.Length;
            Stream s = new FileStream(filePath, fileMode, fileAccess, fileSharing);

            // There is a size limit of the file that we allow to be uploaded to a
            // memory stream. If the file size is bigger than the limit, simply return the fileStream.
            if (byteCount < maxMemoryStream)
            {
                // unchecked cast is safe because _maxMemoryStreamBuffer is less than Int32.Max
                MemoryStream ms = new MemoryStream(unchecked((int)byteCount));
                using (s)
                {
                    PackagingUtilities.CopyStream(s, ms, byteCount, 0x1000);
                }
                s = ms;
            }

            return s;
        }
        #endregion Private methods

        #region Fields

        /// <summary>
        /// CLSID for the XPS filter.
        /// </summary>
        [ComVisible(false)]
        private static readonly Guid _filterClsid = new Guid(0x0B8732A6,
                                                    0xAF74,
                                                    0x498c,
                                                    0xA2 , 0x51 ,
                                                    0x9D , 0xC8 , 0x6B , 0x05 , 0x38 , 0xB0);

        /// <summary>
        /// Internal IFilter implementation being used by XpsFilter.
        /// This could be PackageFilter or EncryptedPackageFilter.
        /// </summary>
        [ComVisible(false)]
        private IFilter _filter;

        /// <summary>
        /// If the XPS file/stream is a Package, reference to the Package.
        /// </summary>
        [ComVisible(false)]
        private Package _package;

        /// <summary>
        /// If the XPS file/stream is a EncryptedPackageEnvelope, reference to the EncryptedPackageEnvelope.
        /// </summary>
        [ComVisible(false)]
        private EncryptedPackageEnvelope _encryptedPackage;

        /// <summary>
        /// If an XPS file is being filtered, refers to the file name.
        /// </summary>
        [ComVisible(false)]
        private string _xpsFileName;

        /// <summary>
        /// Stream wrapper we have opened our Package or EncryptedPackage on
        /// </summary>
        [ComVisible(false)]
        private Stream _packageStream;

        /// <summary>
        /// Cache frequently used size values to incur reflection cost just once.
        /// </summary>
        [ComVisible(false)]
        private const Int32 _int16Size = 2;

        #region Constants

        /// <summary>
        /// The number of characters to copy in a chunk buffer is limited as a 
        /// defense-in-depth device without any expected performance deterioration.
        /// </summary>
        [ComVisible(false)]
        private const uint _maxTextBufferSizeInCharacters = 4096;


        /// <summary>
        /// The size of memory stream buffer used in FileToStream() 
        /// should be limited. If the package file size is bigger than the limit,
        /// we cannot allow the buffer allocation, and return the fileStream itself.
        /// </summary>
        [ComVisible(false)]
        private const Int32 _maxMemoryStreamBuffer = 1024 * 1024;

        #endregion Constants

        #endregion Fields
    }

    #endregion XpsFilter
}
