// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Miscellaneous utility functions for font handling code.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

using MS.Win32;
using MS.Internal;
using MS.Internal.FontFace;
using MS.Internal.PresentationCore;
using MS.Internal.Resources;
using MS.Utility;

using Microsoft.Win32.SafeHandles;

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace MS.Internal.FontCache
{
    /// <summary>
    /// CheckedPointer is used to reference a fixed size memory block.
    /// The purpose of the class is to protect the memory block from overruns.
    /// ArgumentOutOfRangeException is thrown when an overrun is detected.
    /// </summary>
    [FriendAccessAllowed]
    internal struct CheckedPointer
    {
        internal unsafe CheckedPointer(void * pointer, int size)
        {
            _pointer = pointer;
            _size = size;
        }

        internal CheckedPointer(UnmanagedMemoryStream stream)
        {
            Debug.Assert(stream.Position == 0);
            unsafe { _pointer = stream.PositionPointer; }
            long length = stream.Length;
            if (length < 0 || length > int.MaxValue)
                throw new ArgumentOutOfRangeException();
            _size = (int)length;
        }

        internal bool IsNull    
        {
            get
            {
                unsafe
                {
                    return (_pointer == null);
                }
            }
        }

        internal int Size
        {
            get
            {
                return _size;
            }
        }

        internal byte[] ToArray()
        {
            byte[] b = new byte[_size];
            unsafe
            {
                if (_pointer == null)
                    throw new ArgumentOutOfRangeException();

                Marshal.Copy((IntPtr)_pointer, b, 0, Size);
            }
            return b;
        }

        internal void CopyTo(CheckedPointer dest)
        {
            unsafe
            {
                if (_pointer == null)
                    throw new ArgumentOutOfRangeException();

                byte* s = (byte*)_pointer;
                byte * d = (byte *)dest.Probe(0, _size);
                for (int i = 0; i < _size; ++i)
                {
                    d[i] = s[i];
                }
            }
        }

        // Returns the offset of the given pointer within this mapping,
        // with a bounds check.  The returned offset may be equal to the size,
        // but not greater. Throws ArgumentOutOfRangeException if pointer
        // is not within the bounds of the mapping.
        internal unsafe int OffsetOf(void * pointer)
        {
            long offset = (byte*)pointer - (byte*)_pointer;
            if (offset < 0 || offset > _size || _pointer == null || pointer == null)
                throw new ArgumentOutOfRangeException();
            return (int)offset;//no truncation possible since _size is an int
        }

        // Returns the offset of the given pointer within this mapping,
        // with a bounds check.  The returned offset may be equal to the size,
        // but not greater. Throws ArgumentOutOfRangeException if pointer
        // is not within the bounds of the mapping.
        internal int OffsetOf(CheckedPointer pointer)
        {
            unsafe
            {
                return OffsetOf(pointer._pointer);
            }
        }

        public static CheckedPointer operator+(CheckedPointer rhs, int offset)
        {
            // In future I'll just use checked context. That'll require modifying callers to expect integer overflow exceptions.
            unsafe
            {
                if (offset < 0 || offset > rhs._size || rhs._pointer == null)
                    throw new ArgumentOutOfRangeException();
                rhs._pointer = (byte*)rhs._pointer + offset;
            }
            rhs._size -= offset;
            return rhs;
        }

        internal unsafe void * Probe(int offset, int length)
        {
            if (_pointer == null || offset < 0 || offset > _size || offset + length > _size || offset + length < 0)
                throw new ArgumentOutOfRangeException();
            return (byte *)_pointer + offset;
        }

        /// <summary>
        /// Same as Probe, but returns a CheckedPointer instead of an unsafe pointer
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal CheckedPointer CheckedProbe(int offset, int length)
        {
            unsafe
            {
                if (_pointer == null || offset < 0 || offset > _size || offset + length > _size || offset + length < 0)
                    throw new ArgumentOutOfRangeException();

                return new CheckedPointer((byte*)_pointer + offset, length);
            }
        }

        ///<summary>Changes the buffer size of this CheckedPointer object.  Used when memory block is resizable,
        ///like in a file mapping.</summary>
        internal void SetSize(int newSize)
        {
            _size = newSize;
        }

        internal bool PointerEquals(CheckedPointer pointer)
        {
            unsafe
            {
                return _pointer == pointer._pointer;
            }
        }

        internal void WriteBool(bool value)
        {
            unsafe
            {
                *(bool*)this.Probe(0, sizeof(bool)) = value;
            }
        }

        internal bool ReadBool()
        {
            unsafe
            {
                return *(bool*)this.Probe(0, sizeof(bool));
            }
        }

        private unsafe void *   _pointer;

        private int _size;
    }

    /// <summary>
    /// HashFn is a port of predefined hash functions from LKRHash
    /// </summary>
    [FriendAccessAllowed]
    internal static class HashFn
    {
        // Small prime number used as a multiplier in the supplied hash functions
        private const int HASH_MULTIPLIER = 101;

        internal static int HashMultiply(int hash)
        {
            return hash * HASH_MULTIPLIER;
        }

        /// <summary>
        /// Distributes accumulated hash value across a range.
        /// Should be called just before returning hash code from a hash function.
        /// </summary>
        /// <param name="hash">Hash value</param>
        /// <returns>Scrambed hash value</returns>
        internal static int HashScramble(int hash)
        {
            // Here are 10 primes slightly greater than 10^9
            //  1000000007, 1000000009, 1000000021, 1000000033, 1000000087,
            //  1000000093, 1000000097, 1000000103, 1000000123, 1000000181.

            // default value for "scrambling constant"
            const int RANDOM_CONSTANT = 314159269;
            // large prime number, also used for scrambling
            const uint RANDOM_PRIME =   1000000007;

            // we must cast to uint and back to int to correspond to current C++ behavior for operator%
            // since we have a matching hash function in native code
            uint a = (uint)(RANDOM_CONSTANT * hash);
            int b = (int)(a % RANDOM_PRIME);
            return b;
        }

        /// <summary>
        /// Computes a hash code for a block of memory.
        /// One should not forget to call HashScramble before returning the final hash code value to the client.
        /// </summary>
        /// <param name="pv">Pointer to a block of memory</param>
        /// <param name="numBytes">Size of the memory block in bytes</param>
        /// <param name="hash">Previous hash code to combine with</param>
        /// <returns>Hash code</returns>
        internal unsafe static int HashMemory(void * pv, int numBytes, int hash)
        {
            byte * pb = (byte*)pv;

            while (numBytes-- > 0)
            {
                hash = HashMultiply(hash)  +  *pb;
                ++pb;
            }

            return hash;
        }

        internal static int HashString(string s, int hash)
        {
            foreach (char c in s)
            {
                hash = HashMultiply(hash)  +  (ushort)c;
            }
            return hash;
        }
    }

    /// <summary>
    /// Utility functions for interaction with font cache service
    /// </summary>
    [FriendAccessAllowed]
    internal static class Util
    {
        internal const int nullOffset = -1;

        internal static string CompositeFontExtension
        {
            get
            {
                return SupportedExtensions[0];
            }
        }

        private static readonly string[] SupportedExtensions = new string[]
            {
                // .COMPOSITEFONT must remain the first entry in this array
                // because IsSupportedFontExtension and IsCompositeFont relies on this.
                ".COMPOSITEFONT",
                ".OTF",
                ".TTC",
                ".TTF",
                ".TTE"
            };

        
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        internal const UriComponents UriWithoutFragment = UriComponents.AbsoluteUri & ~UriComponents.Fragment;

        private const string WinDir = "windir";
        private const string EmptyFontFamilyReference = "#";
        private const string EmptyCanonicalName = "";

        private static object _dpiLock = new object();
        private static int    _dpi;
        private static bool   _dpiInitialized = false;

        static Util()
        {
            string s = Environment.GetEnvironmentVariable(WinDir) + @"\Fonts\";

            _windowsFontsLocalPath = s.ToUpperInvariant();

            _windowsFontsUriObject = new Uri(_windowsFontsLocalPath, UriKind.Absolute);

            // Characters that have reserved meanings (e.g., '%', '#', '/', etc.) remain escaped in the
            // string, but all others are not escaped.
            _windowsFontsUriString = _windowsFontsUriObject.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
        }


        /// <summary>
        /// Windows fonts Uri string in an unescaped form optimized for Uri manipulations.
        /// </summary>
        private static readonly string _windowsFontsLocalPath;

        internal static string WindowsFontsLocalPath
        {
            get
            {
                return _windowsFontsLocalPath;
            }
        }

        /// <summary>
        /// Gets the number of physical pixels per DIP. For example, if the DPI of the rendering surface is 96 this 
        /// value is 1.0f. If the DPI is 120, this value is 120.0f/96.
        /// </summary>
        internal static float PixelsPerDip
        {
            get
            {
                return ((float)Dpi) / 96;
            }
        }

        internal static int Dpi
        {
            get
            {
                if (!_dpiInitialized)
                {
                    lock (_dpiLock)
                    {
                        if (!_dpiInitialized)
                        {
                            HandleRef desktopWnd = new HandleRef(null, IntPtr.Zero);

                            // Win32Exception will get the Win32 error code so we don't have to
                            IntPtr dc = MS.Win32.UnsafeNativeMethods.GetDC(desktopWnd);

                            // Detecting error case from unmanaged call, required by PREsharp to throw a Win32Exception
                            if (dc == IntPtr.Zero)
                            {
                                throw new Win32Exception();
                            }

                            try
                            {
                                _dpi = MS.Win32.UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), NativeMethods.LOGPIXELSY);
                                _dpiInitialized = true;
                            }
                            finally
                            {
                                MS.Win32.UnsafeNativeMethods.ReleaseDC(desktopWnd, new HandleRef(null, dc));
                            }
                        }
                    }
                }
                return _dpi;
            }
        }

        /// <summary>
        /// Windows fonts Uri object.
        /// </summary>
        private static readonly Uri _windowsFontsUriObject;

        internal static Uri WindowsFontsUriObject
        {
            get
            {
                return _windowsFontsUriObject;
            }
        }

        /// <summary>
        /// Windows fonts Uri string in an unescaped form.
        /// </summary>
        private static readonly string _windowsFontsUriString;

        internal static string WindowsFontsUriString
        {
            get
            {
                return _windowsFontsUriString;
            }
        }


        /// <summary>
        /// Checks whether the specified location string or font family reference refers to the
        /// default Windows Fonts folder.
        /// </summary>
        /// <returns>
        /// Returns true if the location part is empty or is a simple file name with no path
        /// characters (e.g., "#ARIAL" or "arial.ttf#ARIAL"). Returns false if the location
        /// is invalid or includes a path (e.g., "./#ARIAL", "..#ARIAL", "fonts/#ARIAL").
        /// </returns>
        /// <remarks>
        /// This code is important for correcly interpreting font family references.
        /// For example if it returns true for ../arial.ttf#Arial (it shouldnt) then downstream code
        /// could be fooled into skipping a demand when loading data from a ttf file outside the 
        /// Windows Fonts folder.
        /// </remarks>
        internal static bool IsReferenceToWindowsFonts(string s)
        {
            // Empty location always refers to Windows Fonts.
            if (string.IsNullOrEmpty(s) || s[0] == '#')
                return true;

            // Get the length of the location part.
            int length = s.IndexOf('#');
            if (length < 0)
                length = s.Length;

            // Check for invalid file name characters in the location. These include reserved path
            // characters ('/', '\\', ':'), wildcards ('?', '*'), control characters, etc.
            if (s.IndexOfAny(InvalidFileNameChars, 0, length) >= 0)
                return false;

            // A font family reference is a URI reference and may contain escaped characters. We need
            // to check their unescaped values to protect against cases like this:
            //   1. Create canonical name by concatenating "file:///c:/windows/Fonts/" + "foo%2fbar.ttf#Arial"
            //   2. Later we create a Uri from the canonical name and pass to FontSource class
            //   3. FontSource gets Uri.LocalPath which returns "c:\windows\Fonts\foo\bar.ttf"
            //   4. Doh!
            for (int i = s.IndexOf('%', 0, length); i >= 0; i = s.IndexOf('%', i, length - i))
            {
                // Get the unescaped character; this always advances i by at least 1.
                char unescapedChar = Uri.HexUnescape(s, ref i);

                // Is it a reserved character?
                if (Array.IndexOf<char>(InvalidFileNameChars, unescapedChar) >= 0)
                    return false;
            }

            // Check for special names "." and ".." which represent directories. Also reject
            // variations like "...", ".. ", etc., as none of these are valid file names.
            if (s[0] == '.')
            {
                // Advance past one or more '.'
                int i = 1;
                while (i < length && s[i] == '.')
                    ++i;

                // advance past trailing spaces, e.g., ".. #Arial"
                while (i < length && char.IsWhiteSpace(s[i]))
                    ++i;

                // return false if the whole location is just repeated '. followed by spaces
                if (i == length)
                    return false;
            }

            // If we fall through to here the location part has no reserved or illegal characters.
            // The "file name" could still be a reserved device name like CON, PRN, etc., but since
            // none of these will have a known font extension we won't try to open the device.
            return true;
        }


        internal static bool IsSupportedSchemeForAbsoluteFontFamilyUri(Uri absoluteUri)
        {
            // The only absolute URIs we allow in font family references are "file:" URIs.
            return absoluteUri.IsFile;
        }


        internal static void SplitFontFaceIndex(Uri fontUri, out Uri fontSourceUri, out int faceIndex)
        {
            // extract face index
            string fragment = fontUri.GetComponents(UriComponents.Fragment, UriFormat.SafeUnescaped);
            if (!String.IsNullOrEmpty(fragment))
            {
                if (!int.TryParse(
                        fragment,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out faceIndex
                    ))
                {
                    throw new ArgumentException(SR.Get(SRID.FaceIndexMustBePositiveOrZero), "fontUri");
                }

                // face index was specified in a fragment, we need to strip off fragment from the source Uri
                fontSourceUri = new Uri(fontUri.GetComponents(Util.UriWithoutFragment, UriFormat.SafeUnescaped));
            }
            else
            {
                // simple case, no face index specified
                faceIndex = 0;
                fontSourceUri = fontUri;
            }
        }

        internal static Uri CombineUriWithFaceIndex(string fontUri, int faceIndex)
        {
            if (faceIndex == 0)
                return new Uri(fontUri);

            // the Uri roundtrip is necessary for escaping possible '#' symbols in the folder path,
            // so that they don't conflict with the fragment part.
            string canonicalPathUri = new Uri(fontUri).GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
            string faceIndexString = faceIndex.ToString(CultureInfo.InvariantCulture);
            return new Uri(canonicalPathUri + '#' + faceIndexString);
        }

        internal static bool IsSupportedFontExtension(string extension, out bool isComposite)
        {
            for (int i = 0; i < SupportedExtensions.Length; ++i)
            {
                string supportedExtension = SupportedExtensions[i];
                if (String.Compare(extension, supportedExtension, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    isComposite = (i == 0); // First array entry is *.CompositeFont
                    return true;
                }
            }
            isComposite = false;
            return false;
        }

        internal static bool IsCompositeFont(string extension)
        {
            return (String.Compare(extension, CompositeFontExtension, StringComparison.OrdinalIgnoreCase) == 0);
        }

        internal static bool IsEnumerableFontUriScheme(Uri fontLocation)
        {
            bool isEnumerable = false;

            // We only support file:// and pack:// application Uris to reference logical fonts.
            if (fontLocation.IsAbsoluteUri)
            {
                if (fontLocation.IsFile)
                {
                    // file scheme is always enumerable
                    isEnumerable = true;
                }
                else if (fontLocation.Scheme == PackUriHelper.UriSchemePack)
                {
                    // This is just an arbitrary file name which we use to construct a file URI.
                    const string fakeFileName = "X";

                    // The fontLocation could be a folder-like URI even though the pack scheme does not allow this.
                    // We simulate the concept of subfolders for packaged fonts. Before calling any PackUriHelper
                    // methods, create a Uri which we know to be a file-like (rather than folder-like) URI.
                    Uri fileUri;
                    if (Uri.TryCreate(fontLocation, fakeFileName, out fileUri))
                    {
                        isEnumerable = BaseUriHelper.IsPackApplicationUri(fileUri);
                    }
                }
            }

            return isEnumerable;
        }

        internal static bool IsAppSpecificUri(Uri fontLocation)
        {
            // Only file:// Uris that refer to local drives can be cached across applications.
            // This function filters out some easy options, such as app specific pack:// Uris and
            // UNC paths.
            // Note that we make an assumption here that local drive letters stay the same across user sessions.
            // Also, we rely on session 0 not having access to any of the mapped drives.
            return !fontLocation.IsAbsoluteUri || !fontLocation.IsFile || fontLocation.IsUnc;
        }

        internal static string GetUriExtension(Uri uri)
        {
            string unescapedPath = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            return Path.GetExtension(unescapedPath);
        }

        /// <summary>
        /// Converts the specified portion of a friendly name to a normalized font family reference.
        /// </summary>
        /// <remarks>
        /// Friendly names use commas as delimeters so any literal commas must be escaped by
        /// doubling. If any doubled commas are present in the specified substring they are unescaped
        /// in the normalized font family reference.
        /// </remarks>
        internal static string GetNormalizedFontFamilyReference(string friendlyName, int startIndex, int length)
        {
            if (friendlyName.IndexOf(',', startIndex, length) < 0)
            {
                // We don't need to unescape any commas.
                return NormalizeFontFamilyReference(friendlyName, startIndex, length);
            }
            else
            {
                // Unescape commas and then convert to normalized form.
                return NormalizeFontFamilyReference(friendlyName.Substring(startIndex, length).Replace(",,", ","));
            }
        }

        
        /// <summary>
        /// Converts a font family reference to a normalized form.
        /// </summary>
        private static string NormalizeFontFamilyReference(string fontFamilyReference)
        {
            return NormalizeFontFamilyReference(fontFamilyReference, 0, fontFamilyReference.Length);
        }

        /// <summary>
        /// Converts a font family reference to normalized form.
        /// </summary>
        /// <remarks>
        /// In normalized form, the fragment separator ('#') is always present, and the family
        /// name part (i.e., everything after the fragment separator) has been converted to
        /// upper case. However, the case of the location part (if any) is preserved.
        /// </remarks>
        /// <example>
        /// "Arial"             -->  "#ARIAL"
        /// "fonts/#My Font"    -->  "fonts/#MY FONT"
        /// "/fonts/#My Font"   -->  "/fonts/#MY FONT"
        /// </example>
        private static string NormalizeFontFamilyReference(string fontFamilyReference, int startIndex, int length)
        {
            if (length == 0)
                return EmptyFontFamilyReference;

            int fragmentIndex = fontFamilyReference.IndexOf('#', startIndex, length);
            if (fragmentIndex < 0)
            {
                // No fragment separator. The entire string is a family name so convert to uppercase
                // and add a fragment separator at the beginning.
                return "#" + fontFamilyReference.Substring(startIndex, length).ToUpperInvariant();
            }
            else if (fragmentIndex + 1 == startIndex + length)
            {
                // No family name part.
                return EmptyFontFamilyReference;
            }
            else if (fragmentIndex == startIndex)
            {
                // Empty location part; convert the whole string to uppercase.
                return fontFamilyReference.Substring(startIndex, length).ToUpperInvariant();
            }
            else
            {
                // Convert the fragment to uppercase, but preserve the case of the location.
                string location = fontFamilyReference.Substring(startIndex, fragmentIndex - startIndex);
                string fragment = fontFamilyReference.Substring(fragmentIndex, (startIndex + length) - fragmentIndex);
                return location + fragment.ToUpperInvariant();
            }
        }


        /// <summary>
        /// Converts a font family name and location to a font family reference.
        /// </summary>
        /// <param name="familyName">A font family name with no characters escaped</param>
        /// <param name="location">An optional location</param>
        /// <returns>Returns a font family reference, which may be either a URI reference or just a fragment
        /// (with or without the '#' prefix)</returns>
        internal static string ConvertFamilyNameAndLocationToFontFamilyReference(string familyName, string location)
        {
            // Escape reserved characters in the family name. In the fragment, we need only
            // worry about literal percent signs ('%') to avoid confusion with the escape prefix
            // and literal pound signs ('#') to avoid confusion with the fragment separator.
            string fontFamilyReference = familyName.Replace("%", "%25").Replace("#", "%23");

            // Is there a location part?
            if (!string.IsNullOrEmpty(location))
            {
                // We just escaped the family name and the location part should already be a valid URI reference.
                fontFamilyReference = string.Concat(
                    location,
                    "#",
                    fontFamilyReference
                    );
            }

            return fontFamilyReference;
        }

        /// <summary>
        /// Converts a font family reference to a friendly name by escaping any literal commas.
        /// </summary>
        /// <param name="fontFamilyReference">A font family reference</param>
        /// <returns>Returns a friendly name.</returns>
        /// <remarks>Single commas delimit multiple font family references in a friendly name so any
        /// commas in the specified string are replaced with double commas in the return value.
        /// </remarks>
        internal static string ConvertFontFamilyReferenceToFriendlyName(string fontFamilyReference)
        {
            return fontFamilyReference.Replace(",", ",,");
        }

        /// <summary>
        /// Compares string using character ordinals.
        /// The comparison is case insensitive based on InvariantCulture.
        /// We have our own custom wrapper because we need to sort using the same algorithm
        /// we use in incremental charater search.
        /// There are subtle things (e.g. surrogates) that String.Compare() does and we don't.
        /// </summary>
        /// <param name="a">First input string.</param>
        /// <param name="b">Second input string.</param>
        /// <returns>Same semantics as for String.Compare</returns>
        internal static int CompareOrdinalIgnoreCase(string a, string b)
        {
            int aLength = a.Length;
            int bLength = b.Length;
            int minLength = Math.Min(aLength, bLength);
            for (int i = 0; i < minLength; ++i)
            {
                int result = CompareOrdinalIgnoreCase(a[i], b[i]);
                if (result != 0)
                    return result;
            }
            return aLength - bLength;
        }

        private static int CompareOrdinalIgnoreCase(char a, char b)
        {
            char ca = Char.ToUpperInvariant(a);
            char cb = Char.ToUpperInvariant(b);
            return ca - cb;
        }

        /// <summary>
        /// This function performs job similar to CLR's internal __Error.WinIOError function:
        /// it maps win32 errors from file I/O to CLR exceptions and includes string where possible.
        /// However, we're interested only in errors when opening a file for reading.
        /// </summary>
        /// <param name="errorCode">Win32 error code.</param>
        /// <param name="fileName">File name string.</param>
        internal static void ThrowWin32Exception(int errorCode, string fileName)
        {
            switch (errorCode)
            {
                case NativeMethods.ERROR_FILE_NOT_FOUND:
                    throw new FileNotFoundException(SR.Get(SRID.FileNotFoundExceptionWithFileName, fileName), fileName);

                case NativeMethods.ERROR_PATH_NOT_FOUND:
                    throw new DirectoryNotFoundException(SR.Get(SRID.DirectoryNotFoundExceptionWithFileName, fileName));

                case NativeMethods.ERROR_ACCESS_DENIED:
                    throw new UnauthorizedAccessException(SR.Get(SRID.UnauthorizedAccessExceptionWithFileName, fileName));

                case NativeMethods.ERROR_FILENAME_EXCED_RANGE:
                    throw new PathTooLongException(SR.Get(SRID.PathTooLongExceptionWithFileName, fileName));

                default:
                    throw new IOException(SR.Get(SRID.IOExceptionWithFileName, fileName), NativeMethods.MakeHRFromErrorCode(errorCode));
            }
        }

        internal static Exception ConvertInPageException(FontSource fontSource, SEHException e)
        {
            string fileName;
            if (fontSource.IsFile)
            {
                fileName = fontSource.Uri.LocalPath;
            }
            else
            {
                fileName = fontSource.GetUriString();
            }

            return new IOException(SR.Get(SRID.IOExceptionWithFileName, fileName), e);
        }
    }

    /// <summary>
    /// A class that wraps operations with Win32 memory sections and file mappings
    /// </summary>
    [FriendAccessAllowed]
    internal class FileMapping : UnmanagedMemoryStream
    {
        ~FileMapping()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_viewHandle != null)
                        _viewHandle.Dispose();
                    if (_mappingHandle != null)
                        _mappingHandle.Dispose();
                }

                // We only handle flat disk files read only, should never be writeable.
                Invariant.Assert(!CanWrite);
            }
            _disposed = true;
        }

        internal void OpenFile(string fileName)
        {
            NativeMethods.SECURITY_ATTRIBUTES sa = new NativeMethods.SECURITY_ATTRIBUTES();
            try
            {
                unsafe
                {
                    // Disable PREsharp warning about not calling Marshal.GetLastWin32Error,
                    // because we already check the handle for invalid value and
                    // we are not particularly interested in specific Win32 error.

#pragma warning disable 6523

                    long size;

                    using (SafeFileHandle fileHandle = UnsafeNativeMethods.CreateFile(
                        fileName,
                        NativeMethods.GENERIC_READ,
                        NativeMethods.FILE_SHARE_READ,
                        null,
                        NativeMethods.OPEN_EXISTING,
                        0,
                        IntPtr.Zero
                        ))
                    {
                        if (fileHandle.IsInvalid)
                        {
                            Util.ThrowWin32Exception(Marshal.GetLastWin32Error(), fileName);
                        }

                        UnsafeNativeMethods.LARGE_INTEGER fileSize = new UnsafeNativeMethods.LARGE_INTEGER();
                        if (!UnsafeNativeMethods.GetFileSizeEx(fileHandle, ref fileSize))
                            throw new IOException(SR.Get(SRID.IOExceptionWithFileName, fileName));

                        size = (long)fileSize.QuadPart;
                        if (size == 0)
                            throw new FileFormatException(new Uri(fileName));

                        _mappingHandle = UnsafeNativeMethods.CreateFileMapping(
                            fileHandle,
                            sa,
                            UnsafeNativeMethods.PAGE_READONLY,
                            0,
                            0,
                            null);
                    }

                    if (_mappingHandle.IsInvalid)
                        throw new IOException(SR.Get(SRID.IOExceptionWithFileName, fileName));

                    _viewHandle = UnsafeNativeMethods.MapViewOfFileEx(_mappingHandle, UnsafeNativeMethods.FILE_MAP_READ, 0, 0, IntPtr.Zero, IntPtr.Zero);
                    if (_viewHandle.IsInvalid)
                        throw new IOException(SR.Get(SRID.IOExceptionWithFileName, fileName));

#pragma warning restore 6523

                    Initialize((byte*)_viewHandle.Memory, size, size, FileAccess.Read);
                }
            }
            finally
            {
                sa.Release();
                sa = null;
            }
        }

        private UnsafeNativeMethods.SafeViewOfFileHandle _viewHandle;
        private UnsafeNativeMethods.SafeFileMappingHandle _mappingHandle;

        private bool _disposed = false;
    }

    internal class LocalizedName
    {
        internal LocalizedName(XmlLanguage language, string name) : this(language, name, language.GetEquivalentCulture().LCID)
        {}

        internal LocalizedName(XmlLanguage language, string name, int originalLCID)
        {
            _language = language;
            _name = name;
            _originalLCID = originalLCID;
        }

        internal XmlLanguage Language
        {
            get
            {
                return _language;
            }
        }

        internal string Name
        {
            get
            {
                return _name;
            }
        }

        internal int OriginalLCID
        {
            get
            {
                return _originalLCID;
            }
        }

        internal static IComparer<LocalizedName> NameComparer
        {
            get
            {
                return _nameComparer;
            }
        }

        internal static IComparer<LocalizedName> LanguageComparer
        {
            get
            {
                return _languageComparer;
            }
        }

        private class NameComparerClass : IComparer<LocalizedName>
        {
            #region IComparer<LocalizedName> Members

            int IComparer<LocalizedName>.Compare(LocalizedName x, LocalizedName y)
            {
                return Util.CompareOrdinalIgnoreCase(x._name, y._name);
            }

            #endregion
        }

        private class LanguageComparerClass : IComparer<LocalizedName>
        {
            #region IComparer<LocalizedName> Members

            int IComparer<LocalizedName>.Compare(LocalizedName x, LocalizedName y)
            {
                return String.Compare(x._language.IetfLanguageTag, y._language.IetfLanguageTag, StringComparison.OrdinalIgnoreCase);
            }

            #endregion
        }

        private XmlLanguage _language;  // the language identifier
        private string      _name;      // name converted to Unicode
        private int         _originalLCID; // original LCID, used in cases when we want to preserve it to avoid information loss

        private static NameComparerClass _nameComparer = new NameComparerClass();
        private static LanguageComparerClass _languageComparer = new LanguageComparerClass();
    }
}

