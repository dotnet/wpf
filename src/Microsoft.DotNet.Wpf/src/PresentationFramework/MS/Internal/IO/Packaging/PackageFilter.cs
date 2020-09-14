// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Managed equivalent of IFilter implemenation for Package
//

using System;
using System.IO;
using System.IO.Packaging;
using System.Collections;
using System.Diagnostics;               // For Assert
using System.Runtime.InteropServices;   // For Marshal.ThrowExceptionForHR
using System.Windows;                   // for ExceptionStringTable
using Microsoft.Win32;                  // For RegistryKey
using MS.Internal.Interop;              // For STAT_CHUNK, etc.
using MS.Internal;                      // For ContentType
using MS.Internal.Utility;              // For BindUriHelper

using MS.Internal.IO.Packaging.Extensions;
using Package = System.IO.Packaging.Package;
using PackUriHelper = System.IO.Packaging.PackUriHelper;
using InternalPackUriHelper = MS.Internal.IO.Packaging.PackUriHelper;

namespace MS.Internal.IO.Packaging
{
    #region PackageFilter

    /// <summary>
    /// Managed Package Filter
    /// </summary>
    /// <remarks>
    /// This is where implementation goes
    /// </remarks>
    internal class PackageFilter : IFilter
    {
        #region Nested Types

        /// <summary>
        /// Indicates filtering progress.
        /// </summary>
        private enum Progress
        {
            FilteringNotStarted = 0,
            FilteringCoreProperties = 1,
            FilteringContent = 2,
            FilteringCompleted = 3
        }

        #endregion Nested Types

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="package">package to filter</param>
        internal PackageFilter(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;
            _partIterator = _package.GetParts().GetEnumerator();
        }
        #endregion constructor

        #region IFilter methods
        /// <summary>
        /// IFilter.Init
        /// </summary>
        /// <param name="grfFlags">usage flags</param>
        /// <param name="cAttributes">number of elements in aAttributes</param>
        /// <param name="aAttributes">array of Managed FULLPROPSPEC structs to restrict responses</param>
        /// <returns>flags</returns>
        /// <remarks>
        /// Returns systematically IFILTER_FLAGS_NONE insofar as flags are used to control property search,
        /// which is not supported (See GetValue).
        /// </remarks>
        public IFILTER_FLAGS Init(IFILTER_INIT grfFlags,       // IFILTER_INIT value     
            uint cAttributes,               // length of aAttributes
            FULLPROPSPEC[] aAttributes)     // restrict responses to the specified attributes
        {
            _grfFlags = grfFlags;
            _cAttributes = cAttributes;
            _aAttributes = aAttributes;

            _partIterator.Reset();
            _progress = Progress.FilteringNotStarted;

            return IFILTER_FLAGS.IFILTER_FLAGS_NONE;
        }

        /// <summary>
        /// Returns description of the next chunk.
        /// </summary>
        /// <returns>
        /// Chunk descriptor.
        /// </returns>
        /// <remarks>
        /// <para>
        /// On end of stream, this function will throw an exception so as to return FILTER_E_END_OF_CHUNKS
        /// to client code, in keeping with the IFilter specifications.
        /// </para>
        /// <para>
        /// Non-fatal exceptions from external filters, identified for all practical purposes
        /// with COMException and IOException, are `swallowed by this method.
        /// </para>
        /// </remarks>
        public STAT_CHUNK GetChunk()
        {
            //
            // _progress is Progress.FilteringNotStarted initially and
            // subsequently gets updated in MoveToNextFilter().
            //

            if (_progress == Progress.FilteringNotStarted)
            {
                MoveToNextFilter();
            }

            if (_progress == Progress.FilteringCompleted)
            {
                throw new COMException(SR.Get(SRID.FilterEndOfChunks), 
                    (int)FilterErrorCode.FILTER_E_END_OF_CHUNKS);
            }
                
            while(true)
            {
                try
                {
                    STAT_CHUNK chunk = _currentFilter.GetChunk();

                    //
                    // No exception raised. 
                    // If _currentFilter is internal filter,
                    // this might be end of chunks if chunk.idChunk is 0. 
                    //

                    if (!_isInternalFilter || chunk.idChunk != 0)
                    {
                        //
                        // There are more chunks.
                        //

                        //
                        // Consider value chunks only when filtering core properties.
                        // Else, ignore the chunk and move to the next.
                        //
                        if (_progress == Progress.FilteringCoreProperties
                            || (chunk.flags & CHUNKSTATE.CHUNK_VALUE) != CHUNKSTATE.CHUNK_VALUE)
                      {
                            //
                            // Found the next chunk to return.
                            //

                            // Replace ID from auxiliary filter by unique ID across the package.
                            chunk.idChunk = AllocateChunkID();

                            // Since pseudo-properties (a.k.a. internal values) are not supported,
                            // all chunks we return are expected to have idChunkSource equal to idChunk.
                            // (See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/indexsrv/html/ixufilt_8ib8.asp)
                            chunk.idChunkSource = chunk.idChunk;

                            // Some filters (such as the plain text filter) 
                            // will return "no break" before the first chunk.
                            // However, the correct break type at the beginning 
                            // of a part is "end of paragraph".
                            if (_firstChunkFromFilter)
                            {
                                chunk.breakType = CHUNK_BREAKTYPE.CHUNK_EOP;

                                // This flag gets set in MoveToNextFilter.
                                _firstChunkFromFilter = false;
                            }

                            return chunk;
                        }
                    }
                }
                // Ignore IO and COM exceptions raised by an external filter.
                catch (COMException)
                {
                    // Most of the time, this will be a FILTER_E_END_OF_CHUNKS exception.
                    // In general, we don't really care: when an external filter gets in trouble
                    // we simply move to the next filter.
                }
                catch (IOException)
                {
                    // Internal filters do not throw expected exceptions; so something bad
                    // must have happened. Let the client code get the exception and possibly
                    // choose to ignore it.
                    if (_isInternalFilter)
                    {
                        throw;
                    }
                }

                //
                // Either there was FILTER_E_END_OF_CHUNKS exception 
                // from _currentFilter.GetChunk(),
                // or _isInternalFilter is true and the returned chunk has 
                // idChunk as 0, which also indicates end of chunks.
                //
                // Move to the next filter.
                //

                MoveToNextFilter();

                if (_progress == Progress.FilteringCompleted)
                {
                    //
                    // No more filters. Filtering completed.
                    // Throw FILTER_E_END_OF_CHUNKS exception.
                    //

                    throw new COMException(SR.Get(SRID.FilterEndOfChunks),
                        (int)FilterErrorCode.FILTER_E_END_OF_CHUNKS);
                }
            } 
        }

        /// <summary>
        /// Gets text content corresponding to current chunk.
        /// </summary>
        public void GetText(ref uint bufferCharacterCount, IntPtr pBuffer)
        {
            if (_progress != Progress.FilteringContent)
            {
                throw new COMException(SR.Get(SRID.FilterGetTextNotSupported), 
                    (int)FilterErrorCode.FILTER_E_NO_TEXT);
            }

            _currentFilter.GetText(ref bufferCharacterCount, pBuffer);
        }

        /// <summary>
        /// Gets the property value corresponding to current chunk.
        /// </summary>
        /// <returns>Property value</returns>
        public IntPtr GetValue()
        {
            if (_progress != Progress.FilteringCoreProperties)
            {
                throw new COMException(SR.Get(SRID.FilterGetValueNotSupported),
                    (int)FilterErrorCode.FILTER_E_NO_VALUES);
            }

            return _currentFilter.GetValue();
        }

        /// <summary>
        /// BindRegion
        /// </summary>
        /// <param name="origPos"></param>
        /// <param name="riid"></param>
        /// <remarks>
        /// The MSDN specification requires this function to return E_NOTIMPL for the time being.
        /// </remarks>
        public IntPtr BindRegion(FILTERREGION origPos, ref Guid riid)
        {
            throw new NotImplementedException(SR.Get(SRID.FilterBindRegionNotImplemented));
        }
        
        #endregion IFilter methods

        #region Private methods
        /// <summary>
        /// Find a filter object given its CLSID.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the clsid is not a proper IFilter clsid, this function will return null.
        /// The caller will then ignore the current part and attempt to filter the next part.
        /// </para>
        /// <para>
        /// Non-fatal exceptions from external filters, identified for all practical purposes
        /// with InvalidCastException, COMException and IOException, are `swallowed by this method.
        /// </para>
        /// </remarks>
        IFilter GetFilterFromClsid(Guid clsid)
        {
            Type filterType = Type.GetTypeFromCLSID(clsid);
            IFilter filter;
            try
            {
                filter = (IFilter)Activator.CreateInstance(filterType);
            }
            catch(InvalidCastException)
            {
                // If the CLSID that was given is not a filter's CLSID, fail the creation silently.
                return null;
            }
            catch(COMException)
            {
                // If the creation failed at the COM level, fail silently.
                return null;
            }
            return filter;
        }

        /// <summary>
        /// Move iterator to the next part that has an associated filter and (re)initialize the
        /// relevant filter. 
        /// </summary>
        /// <remarks>
        /// This function results in _progress and _currentFilter being updated.
        /// </remarks>
        private void MoveToNextFilter()
        {            
            // Reset _isInternalFilter.
            _isInternalFilter = false;

            switch (_progress)
            {
                case Progress.FilteringNotStarted:

                    #region Progress.FilteringNotStarted

                    // Filtering not started yet. Start with core properties filter.

                    IndexingFilterMarshaler corePropertiesFilterMarshaler
                        = new IndexingFilterMarshaler(
                        new CorePropertiesFilter(_package.PackageProperties));

                    // Avoid exception on end of chunks from part filter.
                    corePropertiesFilterMarshaler.ThrowOnEndOfChunks = false;

                    _currentFilter = corePropertiesFilterMarshaler;
                    _currentFilter.Init(_grfFlags, _cAttributes, _aAttributes);
                    _isInternalFilter = true;
                    
                    // Update progress to indicate filtering core properties.
                    _progress = Progress.FilteringCoreProperties;
                    
                    break;

                    #endregion Progress.FilteringNotStarted

                case Progress.FilteringCoreProperties:

                    #region Progress.FilteringCoreProperties

                // Core properties were being filtered. Next move to content filtering.

                #endregion Progress.FilteringCoreProperties

                case Progress.FilteringContent:

                    #region Progress.FilteringContent

                    //
                    // Content being filtered. Move to next content part filter if it exists.
                    // Update progress to indicate filtering content if there is a next content
                    // filter, else to indicate filtering is completed.
                    //

                    if (_currentStream != null)
                    {
                        // Close the stream for the previous PackagePart.
                        _currentStream.Close();
                        _currentStream = null;
                    }

                    for (_currentFilter = null; _partIterator.MoveNext(); _currentFilter = null)
                    {
                        PackagePart currentPart = (PackagePart)_partIterator.Current;
                        ContentType contentType = currentPart.ValidatedContentType();

                        // Find the filter's CLSID based on the MIME content type.
                        string filterClsid = GetFilterClsid(contentType, currentPart.Uri);
                        if (filterClsid != null)
                        {
                            _currentFilter = GetFilterFromClsid(new Guid(filterClsid));
                            if (_currentFilter != null)
                            {
                                _currentStream = currentPart.GetSeekableStream();
                                ManagedIStream stream = new ManagedIStream(_currentStream);
                                try
                                {
                                    IPersistStreamWithArrays filterLoader = (IPersistStreamWithArrays)_currentFilter;
                                    filterLoader.Load(stream);
                                    _currentFilter.Init(_grfFlags, _cAttributes, _aAttributes);

                                    // Filter found and properly initialized. Search is over.
                                    break;
                                }
                                catch (InvalidCastException)
                                {
                                    // If a filter does not implement IPersistStream, then, by design, it should
                                    // be ignored.
                                }
                                catch (COMException)
                                {
                                    // Any initialization bug giving rise to an exception in the initialization
                                    // code should be ignored, since this will be due to faulty external code.
                                }
                                catch (IOException)
                                {
                                    // Initialization problem can be reported as IOException. See preceding comment.
                                }
                            }
                        }

                        //
                        // No valid externally registered filters found for this content part.
                        // If this is xaml part, use the internal XamlFilter.
                        //

                        if (BindUriHelper.IsXamlMimeType(contentType))
                        {
                            if (_currentStream == null)
                            {
                                _currentStream = currentPart.GetSeekableStream();
                            }

                            IndexingFilterMarshaler xamlFilterMarshaler
                                = new IndexingFilterMarshaler(new XamlFilter(_currentStream));

                            // Avoid exception on end of chunks from part filter.
                            xamlFilterMarshaler.ThrowOnEndOfChunks = false;

                            _currentFilter = xamlFilterMarshaler;
                            _currentFilter.Init(_grfFlags, _cAttributes, _aAttributes);
                            _isInternalFilter = true;

                            // Filter found and properly initialized. Search is over.
                            break;
                        }

                        if (_currentStream != null)
                        {
                            _currentStream.Close();
                            _currentStream = null;
                        }
                    }

                    if (_currentFilter == null)
                    {
                        // Update progress to indicate filtering is completed.
                        _progress = Progress.FilteringCompleted;
                    }
                    else
                    {
                        // Tell GetChunk that we are getting input from a new filter.
                        _firstChunkFromFilter = true;
 
                        // Update progress to indicate content being filtered.
                        _progress = Progress.FilteringContent;
                    }
                    break;

                    #endregion Progress.FilteringContent

                case Progress.FilteringCompleted:

                    #region Progress.FilteringCompleted

                    Debug.Assert(false);
                    break;

                    #endregion Progress.FilteringCompleted

                default:

                    #region Default

                    Debug.Assert(false);
                    break;

                    #endregion Default
            }
        }

         /// <summary>
        /// Allocates a unique and legal chunk ID.
        /// To be called prior to returning a chunk.
        /// </summary>
        /// <remarks>
        /// 0 is an illegal value, so this function never returns 0.
        /// After the counter reaches UInt32.MaxValue, it wraps around to 1.
        /// </remarks>
        private uint AllocateChunkID()
        {
            Invariant.Assert(_currentChunkID <= UInt32.MaxValue);
 
            ++_currentChunkID;
            
            return _currentChunkID;
        }

        /// <summary>
        /// Access the registry to get the filter's CLSID associated with contentType,
        /// unless contentType is empty, in which case the content type is inferred from
        /// the file's extension.
        /// </summary>
        private string GetFilterClsid(ContentType contentType, Uri partUri)
        {
            // Find the file type guid associated with the mime content or (as a second choice) with the extension.
            string fileTypeGuid = null;
            if (contentType != null && !ContentType.Empty.AreTypeAndSubTypeEqual(contentType))
            {
                fileTypeGuid = FileTypeGuidFromMimeType(contentType);
            }
            else
            {
                string extension = GetPartExtension(partUri);

                if (extension != null)
                {
                    fileTypeGuid = FileTypeGuidFromFileExtension(extension);
                }
            }

            // Get the default value of
            // /HKEY_CLASSES_ROOT/CLSID/<fileTypeGuid>/PersistentAddinsRegistered/<IID_IFilter>.
            if (fileTypeGuid == null)
            {
                return null;
            }
            RegistryKey iFilterIidKey = 
                FindSubkey(
                    Registry.ClassesRoot,
                    MakeRegistryPath(_IFilterAddinPath, fileTypeGuid));

            if (iFilterIidKey == null)
            {
                return null;
            }
            return (string)iFilterIidKey.GetValue(null);
        }

        /// <summary>
        /// Interprets an array of strings [keyPath] and returns the corresponding subkey
        /// if it exists, and null if it doesn't.
        /// </summary>
        private static RegistryKey FindSubkey(RegistryKey containingKey, string[] keyPath)
        {
            RegistryKey currentKey = containingKey;

            for (int keyNameIndex = 0; keyNameIndex < keyPath.Length; ++keyNameIndex)
            {
                if (currentKey == null)
                {
                    return null;
                }
                currentKey = currentKey.OpenSubKey(keyPath[keyNameIndex]);
            }
            return currentKey;
        }

        /// <summary>
        /// Gets the content type's GUID and find the associated PersistentHandler GUID.
        /// </summary>
        private string FileTypeGuidFromMimeType(ContentType contentType)
        {
            // This method is invoked only if contentType has been found to be non-empty.
            Debug.Assert(contentType != null && contentType.ToString().Length > 0);

            // Get the string in value Extension of key \HKEY_CLASSES_ROOT\MIME\Database\Content Type\<MIME type>.
            RegistryKey mimeContentType = FindSubkey(Registry.ClassesRoot, _mimeContentTypeKey);
            RegistryKey mimeTypeKey = (mimeContentType == null ? null : mimeContentType.OpenSubKey(contentType.ToString()));
            if (mimeTypeKey == null)
            {
                return null;
            }
            string extension = (string)mimeTypeKey.GetValue(_extension);
            if (extension == null)
            {
                return null;
            }

            // Use the extension to find the type GUID.
            return FileTypeGuidFromFileExtension(extension);
        }

        /// <summary>
        /// Get the PersistentHandler GUID associated with the parameter dottedExtensionName.
        /// </summary>
        /// <param name="dottedExtensionName">Extension name with a dot as the first character.</param>
        private string FileTypeGuidFromFileExtension(string dottedExtensionName)
        {
            // This method is invoked only if the part name has been found to have an extension.
            Debug.Assert(dottedExtensionName != null);

            // Extract \HKEY_CLASSES_ROOT\<extension>\PersistentHandler and return its default value.
            RegistryKey persistentHandlerKey =
                FindSubkey(
                    Registry.ClassesRoot, 
                    MakeRegistryPath(_persistentHandlerKey, dottedExtensionName));
            return (persistentHandlerKey == null ? null : (string)persistentHandlerKey.GetValue(null));
        }


        // Return uri's extension or null if no extension.
        private string GetPartExtension(Uri partUri)
        {
            // partUri is the part's path as exposed by its part uri, so it cannot be null.
            Invariant.Assert(partUri != null);

            string path = InternalPackUriHelper.GetStringForPartUri(partUri);
            string extension = Path.GetExtension(path);
            if (extension == string.Empty)
                return null;

            return extension;
        }

        //This method replaces null entries in pathWithGaps by stopGaps items in order of occurance
        private static string[] MakeRegistryPath(string[] pathWithGaps, params string[] stopGaps)
        {
            Debug.Assert(pathWithGaps != null && stopGaps != null);

            string[] path = (string[]) pathWithGaps.Clone();
            int nextStopGapToUse = 0;

            for (int i = 0; i < path.Length; ++i)
            {
                if (path[i] == null)
                {
                    Debug.Assert(stopGaps.Length > nextStopGapToUse);

                    // Values of pathWithGaps and stopGaps are entirely controlled by internal code.
                    path[i] = stopGaps[nextStopGapToUse];
                    ++nextStopGapToUse;
                }
            }

            Debug.Assert(stopGaps.Length == nextStopGapToUse);

            return path;
        }

        #endregion Private methods

        #region Constants

        // Registry paths.
        // Paths are represented by string arrays. Null entries are to be replaced by strings in order to
        // get a valid path (see method MakeRegistryPath).

        // The following path contains the IFilter IID, which can be found in the public SDK file filter.h.
        readonly string[] _IFilterAddinPath = new string[]
            {
                "CLSID",
                null,  // file type GUID expected
                "PersistentAddinsRegistered",
                "{89BCB740-6119-101A-BCB7-00DD010655AF}"
            };

        readonly string[] _mimeContentTypeKey = new string[]
            {
                "MIME",
                "Database",
                "Content Type"
            };

        readonly string[] _persistentHandlerKey = 
            {
                null,  // extension string expected
                "PersistentHandler"
            };
        
        #endregion Constants

        #region Fields

        private Package             _package;
        private uint                 _currentChunkID;       //defaults to 0
        private IEnumerator         _partIterator;          //defaults to null
        private IFilter             _currentFilter;         //defaults to null
        private Stream              _currentStream;         //defaults to null
        private bool                _firstChunkFromFilter;  //defaults to false
        private Progress            _progress               = Progress.FilteringNotStarted;
        private bool                _isInternalFilter;      //defaults to false

        private IFILTER_INIT        _grfFlags;              //defaults to 0
        private uint                _cAttributes;           //defaults to 0
        private FULLPROPSPEC[]      _aAttributes;           //defaults to null

        private const string        _extension              = "Extension";

        #endregion Fields
    }

    #endregion PackageFilter
}
