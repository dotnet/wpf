// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: The FontSource class.
//
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using MS.Win32;
using MS.Utility;
using MS.Internal;
using MS.Internal.IO.Packaging;
using MS.Internal.PresentationCore;
using MS.Internal.Text.TextInterface;

namespace MS.Internal.FontCache
{
    internal class FontSourceFactory : IFontSourceFactory
    {
        public FontSourceFactory() { }
        
        public IFontSource Create(string uriString)
        {
            return new FontSource(new Uri(uriString), false);
        }
    }

    /// <summary>
    /// FontSource class encapsulates the logic for dealing with fonts in memory or on the disk.
    /// It may or may not have a Uri associated with it, but there has to be some way to obtain its contents.
    /// </summary>
    internal class FontSource : IFontSource
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        public FontSource(Uri fontUri, bool skipDemand)
        {
            Initialize(fontUri, skipDemand, false, isInternalCompositeFont: false);
        }

        public FontSource(Uri fontUri, bool skipDemand, bool isComposite)
        {
            Initialize(fontUri, skipDemand, isComposite, isInternalCompositeFont: false);
        }

        /// <summary>
        /// Allows WPF to construct its internal CompositeFonts from resource URIs.
        /// </summary>
        /// <param name="fontUri"></param>
        /// <param name="skipDemand"></param>
        /// <param name="isComposite"></param>
        public FontSource(Uri fontUri, bool skipDemand, bool isComposite, bool isInternalCompositeFont)
        {
            Initialize(fontUri, skipDemand, isComposite, isInternalCompositeFont);
        }

        private void Initialize(Uri fontUri, bool skipDemand, bool isComposite, bool isInternalCompositeFont)
        {
            _fontUri = fontUri;
            _skipDemand = skipDemand;
            _isComposite = isComposite;
            _isInternalCompositeFont = isInternalCompositeFont;
            Invariant.Assert(_isInternalCompositeFont || _fontUri.IsAbsoluteUri);
            Debug.Assert(_isInternalCompositeFont || String.IsNullOrEmpty(_fontUri.Fragment));
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Use this to ensure we don't call Uri.IsFile on a relative URI.
        /// </summary>
        public bool IsFile { get { return !_isInternalCompositeFont && _fontUri.IsFile; } }

        public bool IsComposite
        {
            get
            {
                return _isComposite;
            }
        }

        public string GetUriString()
        {
            return _fontUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
        }

        public string ToStringUpperInvariant()
        {
            return GetUriString().ToUpperInvariant();
        }

        public override int GetHashCode()
        {
            return HashFn.HashString(ToStringUpperInvariant(), 0);
        }


        public Uri Uri
        {
            get
            {
                return _fontUri;
            }
        }

        public bool IsAppSpecific
        {
            get
            {
                return Util.IsAppSpecificUri(_fontUri);
            }
        }

        internal long SkipLastWriteTime()
        {
            // clients may choose to use this temporary method because GetLastWriteTime call
            // results in touching the file system
            // we need to resurrect this code when we come up with a complete solution
            // for updating fonts on the fly
            return -1; // any non-zero value will do here
        }

        public DateTime GetLastWriteTimeUtc()
        {
            if (IsFile)
            {
                return Directory.GetLastWriteTimeUtc(_fontUri.LocalPath);
            }

            // Any special value will do here.
            return DateTime.MaxValue;
        }

        public UnmanagedMemoryStream GetUnmanagedStream()
        {
            if (IsFile)
            {
                FileMapping fileMapping = new FileMapping();

                fileMapping.OpenFile(_fontUri.LocalPath);
                return fileMapping;
            }

            byte[] bits;

            // Try our cache first.
            lock (_resourceCache)
            {
                bits = _resourceCache.Get(_fontUri);
            }

            if (bits == null)
            {
                Stream fontStream;

                if (_isInternalCompositeFont)
                {
                    // We should read this font from our framework resources
                    fontStream = GetCompositeFontResourceStream();
                }
                else
                {
                    WebResponse response = WpfWebRequestHelper.CreateRequestAndGetResponse(_fontUri);
                    fontStream = response.GetResponseStream();
                    if (String.Equals(response.ContentType, ObfuscatedContentType, StringComparison.Ordinal))
                    {
                        // The third parameter makes sure the original stream is closed
                        // when the deobfuscating stream is disposed.
                        fontStream = new DeobfuscatingStream(fontStream, _fontUri, false);
                    }
                }

                UnmanagedMemoryStream unmanagedStream = fontStream as UnmanagedMemoryStream;

                if (unmanagedStream != null)
                    return unmanagedStream;

                bits = StreamToByteArray(fontStream);

                fontStream?.Close();
            }

            lock (_resourceCache)
            {
                _resourceCache.Add(_fontUri, bits, false);
            }

            return ByteArrayToUnmanagedStream(bits);
        }

        /// <summary>
        /// Tries to open a file and throws exceptions in case of failures. This
        /// method is used to achieve the same exception throwing behavior after
        /// integrating DWrite.
        /// </summary>
        public void TestFileOpenable()
        {
            if (IsFile)
            {
                FileMapping fileMapping = new FileMapping();

                fileMapping.OpenFile(_fontUri.LocalPath);
                fileMapping.Close();
            }
        }

        public Stream GetStream()
        {
            if (IsFile)
            {
                FileMapping fileMapping = new FileMapping();

                fileMapping.OpenFile(_fontUri.LocalPath);
                return fileMapping;
            }

            byte[] bits;

            // Try our cache first.
            lock (_resourceCache)
            {
                bits = _resourceCache.Get(_fontUri);
            }

            if (bits != null)
                return new MemoryStream(bits);

            Stream fontStream;

            if (_isInternalCompositeFont)
            {
                // We should read this font from our framework resources
                fontStream = GetCompositeFontResourceStream();
            }
            else
            {
                WebRequest request = PackWebRequestFactory.CreateWebRequest(_fontUri);
                WebResponse response = request.GetResponse();

                fontStream = response.GetResponseStream();
                if (String.Equals(response.ContentType, ObfuscatedContentType, StringComparison.Ordinal))
                {
                    // The third parameter makes sure the original stream is closed
                    // when the deobfuscating stream is disposed.
                    fontStream = new DeobfuscatingStream(fontStream, _fontUri, false);
                }
            }

            return fontStream;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static UnmanagedMemoryStream ByteArrayToUnmanagedStream(byte[] bits)
        {
            return new PinnedByteArrayStream(bits);
        }

        private static byte [] StreamToByteArray(Stream fontStream)
        {
            byte[] memoryFont;

            if (fontStream.CanSeek)
            {
                checked
                {
                    memoryFont = new byte[(int)fontStream.Length];
                    PackagingUtilities.ReliableRead(fontStream, memoryFont, 0, (int)fontStream.Length);
                }
            }
            else
            {
                // this is inefficient, but works for now
                // we need to spend more time to implement a more performant
                // version of this code
                // ideally this should be a part of loader functionality

                // Initial file read buffer size is set to 1MB.
                int fileReadBufferSize = 1024 * 1024;
                byte[] fileReadBuffer = new byte[fileReadBufferSize];

                // Actual number of bytes read from the file.
                int memoryFontSize = 0;

                for (; ; )
                {
                    int availableBytes = fileReadBufferSize - memoryFontSize;
                    if (availableBytes < fileReadBufferSize / 3)
                    {
                        // grow the fileReadBuffer
                        fileReadBufferSize *= 2;
                        byte[] newBuffer = new byte[fileReadBufferSize];
                        Array.Copy(fileReadBuffer, newBuffer, memoryFontSize);
                        fileReadBuffer = newBuffer;
                        availableBytes = fileReadBufferSize - memoryFontSize;
                    }
                    int numberOfBytesRead = fontStream.Read(fileReadBuffer, memoryFontSize, availableBytes);
                    if (numberOfBytesRead == 0)
                        break;

                    memoryFontSize += numberOfBytesRead;
                }

                // Actual number of bytes read from the file is less or equal to the file read buffer size.
                Debug.Assert(memoryFontSize <= fileReadBufferSize);

                if (memoryFontSize == fileReadBufferSize)
                    memoryFont = fileReadBuffer;
                else
                {
                    // Trim the array if needed to that it contains the right length.
                    memoryFont = new byte[memoryFontSize];
                    Array.Copy(fileReadBuffer, memoryFont, memoryFontSize);
                }
            }

            return memoryFont;
        }

        /// <summary>
        /// Retrieves internal CompositeFont resources from the appropriate DLL resources.
        /// </summary>
        /// <returns>A stream to the requested CompositeFont resources.</returns>
        private Stream GetCompositeFontResourceStream()
        {
            string fontFilename = _fontUri.OriginalString.Substring(_fontUri.OriginalString.LastIndexOf('/') + 1).ToLowerInvariant();

            var fontResourceAssembly = Assembly.GetExecutingAssembly();
            ResourceManager rm = new ResourceManager($"{fontResourceAssembly.GetName().Name}.g", fontResourceAssembly);

            return rm?.GetStream($"fonts/{fontFilename}");
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes

        private class PinnedByteArrayStream : UnmanagedMemoryStream
        {
            internal PinnedByteArrayStream(byte [] bits)
            {
                _memoryHandle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                
                unsafe
                {
                    Initialize(
	                    (byte *)_memoryHandle.AddrOfPinnedObject(),
	                    bits.Length, 
	                    bits.Length, 
	                    FileAccess.Read
                    );
                }
            }

            ~PinnedByteArrayStream()
            {
                Dispose(false);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                Debug.Assert(_memoryHandle.IsAllocated);
                _memoryHandle.Free();
            }

            private GCHandle    _memoryHandle;
        }

        #endregion Private Classes

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _isComposite;

        /// <summary>
        /// Indicates that this composite font is to be read from internal WPF resources.
        /// </summary>
        private bool _isInternalCompositeFont;

        private Uri     _fontUri;

        private bool    _skipDemand;

        private static SizeLimitedCache<Uri, byte[]> _resourceCache = new SizeLimitedCache<Uri, byte[]>(MaximumCacheItems);

        /// <summary>
        /// The maximum number of fonts downloaded from pack:// Uris.
        /// </summary>
        private const int MaximumCacheItems = 10;
        private const string ObfuscatedContentType = "application/vnd.ms-package.obfuscated-opentype";

        #endregion Private Fields
    }
}
