// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//  Common container-related operations that can be shared among internal
//  components.
//

using System;
using System.Collections;   // for IList
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;          // for StringBuilder
using System.Diagnostics;   // for Debug.Assert
using System.Security;


#if PBTCOMPILER
using MS.Utility;     // For SR.cs
using MS.Internal.PresentationBuildTasks;
#else
using System.Windows;
using MS.Internal.WindowsBase;
#endif

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// ContainerUtilities
    /// </summary>
    static internal class ContainerUtilities
    {
        static private readonly Int32 _int16Size = SizeOfInt16();
        static private readonly Int32 _int32Size = SizeOfInt32();
        static private readonly byte[] _paddingBuf = new byte[4];        // for writing DWORD padding


#if !PBTCOMPILER
        static private readonly Int32 _int64Size = SizeOfInt64();

        /// Used by ConvertBackSlashPathToStringArrayPath and 
        ///     ConvertStringArrayPathToBackSlashPath to separate path elements.
        static readonly internal char PathSeparator = '\\';
        static private readonly char[] _PathSeparatorArray = new char[] { PathSeparator };
        static readonly internal string PathSeparatorAsString = new string(ContainerUtilities.PathSeparator, 1);

        static private readonly CaseInsensitiveOrdinalStringComparer _stringCaseInsensitiveComparer = new CaseInsensitiveOrdinalStringComparer();
#endif


        /// <summary>
        /// Byte size of Int16 Type
        /// </summary>
        static internal Int32 Int16Size
        {
            get
            {
                return _int16Size;
            }
        }

#if !PBTCOMPILER
        /// <summary>
        /// Byte size of Int32 Type
        /// </summary>
        static internal Int32 Int32Size
        {
            get
            {
                return _int32Size;
            }
        }

        /// <summary>
        /// Byte size of Int64 Type
        /// </summary>
        static internal Int32 Int64Size
        {
            get
            {
                return _int64Size;
            }
        }

        static internal CaseInsensitiveOrdinalStringComparer StringCaseInsensitiveComparer
        {
            get
            {
                return _stringCaseInsensitiveComparer;
            }
        }
#endif

        //The length should be verified by the caller to be non-negative
        internal static int CalculateDWordPadBytesLength(int length)
        {
            Debug.Assert(length >= 0, "The length cannot be negative. Caller must verify this");

            int padLen = length & 3;

            if (padLen > 0)
            {
                padLen = 4 - padLen;
            }

            return (padLen);
        }

        /// <summary>
        /// Write out a string via a BinaryWriter.  Prefixed with int32 length in 
        /// bytes and padded at the end to the next 32-bit (DWORD) boundary
        /// </summary>
        /// <param name="writer">BinaryWriter configured for Unicode characters</param>
        /// <param name="outputString">String to write out to BinaryWriter</param>
        /// <returns>Number of bytes written including Prefixed int32 length, Unicode string,
        /// and padding</returns>
        /// <remarks>If writer==null, it will return the number of bytes it will take
        /// to write it out; If outputString==null, it will write out 0 as length and
        /// an empty string; After this operation, the current position of BinaryWriter
        /// will be changed</remarks>
        internal static int WriteByteLengthPrefixedDWordPaddedUnicodeString(BinaryWriter writer, String outputString)
        {
            checked
            {
                Int32 strByteLen = 0;

                if (outputString != null)
                {
                    strByteLen = outputString.Length * 2;
                }

                if (writer != null)
                {
                    // Write length, in bytes, of the unicode string
                    writer.Write(strByteLen);

                    if (strByteLen != 0)
                    {
                        // Write the Unicode characters
                        writer.Write(outputString.ToCharArray());
                    }
                }

                if (strByteLen != 0)
                {
                    int padLength = CalculateDWordPadBytesLength(strByteLen);

                    if (padLength != 0)
                    {
                        strByteLen += padLength;

                        if (writer != null)
                        {
                            writer.Write(_paddingBuf, 0, padLength);
                        }
                    }
                }

                strByteLen += _int32Size;       // Size of the string length written at the beginning of this method

                return strByteLen;
            }
        }

#if !PBTCOMPILER
        /// <summary>
        /// Read a string whose length is specified by the first four bytes as a
        /// int32 and whose end is padded to the next 32-bit (DWORD) boundary.
        /// </summary>
        /// <param name="reader">A BinaryReader initialized for Unicode characters</param>
        /// <returns>Unicode string without padding; After this operation, the current
        /// position of BinaryWriter will be changed</returns>
        internal static String ReadByteLengthPrefixedDWordPaddedUnicodeString(BinaryReader reader)
        {
            int bytesRead;

            return ReadByteLengthPrefixedDWordPaddedUnicodeString(reader, out bytesRead);
        }

        /// <summary>
        /// Read a string whose length is specified by the first four bytes as a
        /// int32 and whose end is padded to the next 32-bit (DWORD) boundary.
        /// </summary>
        /// <param name="reader">A BinaryReader initialized for Unicode characters</param>
        /// <param name="bytesRead">Total bytes read including prefixed length and padding</param>
        /// <returns>Unicode string without padding</returns>
        /// <remarks>If the string length is 0, it returns an empty string; After this operation,
        /// the current position of BinaryWriter will be changed</remarks>
        internal static String ReadByteLengthPrefixedDWordPaddedUnicodeString(BinaryReader reader, out int bytesRead)
        {
            checked
            {
                bytesRead = 0;

                CheckAgainstNull(reader, "reader");

                bytesRead = reader.ReadInt32();   // Length of the string in bytes

                String inString = null;

                if (bytesRead > 0)
                {
                    try
                    {
                        if (reader.BaseStream.Length < bytesRead / 2)
                        {
#if !PBTCOMPILER
                            throw new FileFormatException(SR.Get(SRID.InvalidStringFormat));
#else
                        throw new SerializationException(SR.Get(SRID.InvalidStringFormat));
#endif
                        }
                    }
                    catch (NotSupportedException)
                    {
                        // if the stream does not support the Length operator, it will throw a NotSupportedException.
                    }

                    inString = new String(reader.ReadChars(bytesRead / 2));

                    // Make sure the length of string read matches the length specified
                    if (inString.Length != (bytesRead / 2))
                    {
#if !PBTCOMPILER
                        throw new FileFormatException(SR.Get(SRID.InvalidStringFormat));
#else
                    throw new SerializationException(SR.Get(SRID.InvalidStringFormat));
#endif
                    }
                }
                else if (bytesRead == 0)
                {
                    inString = String.Empty;
                }
                else
                {
#if !PBTCOMPILER
                    throw new FileFormatException(SR.Get(SRID.InvalidStringFormat));
#else
                throw new SerializationException(SR.Get(SRID.InvalidStringFormat));
#endif
                }

                // skip the padding
                int padLength = CalculateDWordPadBytesLength(bytesRead);

                if (padLength > 0)
                {
                    byte[] padding;

                    padding = reader.ReadBytes(padLength);

                    // Make sure the string is padded with the correct number of bytes
                    if (padding.Length != padLength)
                    {
#if !PBTCOMPILER
                        throw new FileFormatException(SR.Get(SRID.InvalidStringFormat));
#else
                    throw new SerializationException(SR.Get(SRID.InvalidStringFormat));
#endif
                    }
                    bytesRead += padLength;
                }

                bytesRead += _int32Size; //// Size of the string length read at the beginning of this method

                return inString;
            }
        }
#endif

        /// <summary>
        /// Subset of CheckStringAgainstNullAndEmpty - and just checks against null reference.
        /// </summary>
        static internal void CheckAgainstNull(object paramRef,
            string testStringIdentifier)
        {
            if (paramRef == null)
                throw new ArgumentNullException(testStringIdentifier);
        }

#if !PBTCOMPILER

#endif        
        private static int SizeOfInt16()
        {
            return Marshal.SizeOf(typeof(Int16));
        }
        
#if !PBTCOMPILER

#endif        
        private static int SizeOfInt32()
        {
            return Marshal.SizeOf(typeof(Int32));
        }
        
#if !PBTCOMPILER
        private static int SizeOfInt64()
        {
            return Marshal.SizeOf(typeof(Int64));
        }
#endif
        
#if !PBTCOMPILER
        /// <summary>
        ///     Interprets a single string by treating it as a set of names
        /// delimited by a special character.  The character is the backslash,
        /// serving the same role it has served since the original MS-DOS.
        ///     The individual names are extracted from this string and 
        /// returned as an array of string names.
        ///
        ///  string "images\button.jpg" -> string [] { "images", "button.jpg" }
        ///
        /// </summary>
        /// <param name="backSlashPath">
        ///     String path to be converted
        /// </param>
        /// <returns>
        ///     The elements of the path as a string array
        /// </returns>
        /// <remarks>
        ///     Mirror counterpart of ConvertStringArrayPathToBackSlashPath
        /// </remarks>

        // In theory, parsing strings should be done with regular expressions.
        // In practice, the RegEx class is too heavyweight for this application.

        // IMPORTANT: When updating this, make sure the counterpart is similarly
        //  updated.
        static internal string[] ConvertBackSlashPathToStringArrayPath(string backSlashPath)
        {
            // A null string will get a null array
            if ((null == backSlashPath) || (0 == backSlashPath.Length))
                return new string[0];

            // Reject leading/trailing whitespace
            if (Char.IsWhiteSpace(backSlashPath[0]) ||
                Char.IsWhiteSpace(backSlashPath[backSlashPath.Length - 1]))
            {
                throw new ArgumentException(SR.Get(SRID.MalformedCompoundFilePath));
            }

            // Build the array
            string[] splitArray =
                backSlashPath.Split(_PathSeparatorArray);

            // Look for empty strings in the array
            foreach (string arrayElement in splitArray)
            {
                if (0 == arrayElement.Length)
                    throw new ArgumentException(
                        SR.Get(SRID.PathHasEmptyElement), "backSlashPath");
            }

            // No empty strings, this array should be fine.
            return splitArray;
        }

        /// <summary>
        ///     Concatenates the names in an array of strings into a backslash-
        /// delimited string.
        ///
        ///  string[] { "images", "button.jpg" } -> string "images\button.jpg"
        ///
        /// </summary>
        /// <param name="arrayPath">
        ///     String array of names
        /// </param>
        /// <returns>
        ///     Concatenated path with all the names in the given array
        ///</returns>
        /// <remarks>
        ///     Mirror counterpart to ConvertBackSlashPathToStringArrayPath
        ///</remarks>

        // IMPORTANT: When updating this, make sure the counterpart is similarly
        //  updated.

        static internal string ConvertStringArrayPathToBackSlashPath(IList arrayPath)
        {
            // Null array gets a null string
            if ((null == arrayPath) || (1 > arrayPath.Count))
                return String.Empty;

            // Length of one gets that element returned
            if (1 == arrayPath.Count)
                return (string)arrayPath[0];

            // More than one - OK it's time to build something.
            CheckStringForEmbeddedPathSeparator((string)arrayPath[0], "Path array element");
            StringBuilder pathBuilder =
                new StringBuilder((string)arrayPath[0]);
            for (int counter = 1; counter < arrayPath.Count; counter++)
            {
                CheckStringForEmbeddedPathSeparator((String)arrayPath[counter], "Path array element");
                pathBuilder.Append(PathSeparator);
                pathBuilder.Append((String)arrayPath[counter]);
            }

            return pathBuilder.ToString();
        }

        /// <summary>
        /// Convert to path when storage array and stream name are separate
        /// </summary>
        /// <param name="storages">storage collection (strings)</param>
        /// <param name="streamName">stream name</param>
        static internal string ConvertStringArrayPathToBackSlashPath(IList storages, string streamName)
        {
            string result = ConvertStringArrayPathToBackSlashPath(storages);
            if (result.Length > 0)
                return result + PathSeparator + streamName;
            else
                return streamName;
        }

        /// <summary>
        ///     Utility function to check a string (presumably an input to the
        /// caller function) against null, then against empty.  Throwing an
        /// exception if either is true.
        /// </summary>
        /// <param name="testString">
        ///     The string to be tested
        /// </param>
        /// <param name="testStringIdentifier">
        ///     A label for the test string to be used in the exception message.
        /// </param>
        /// <returns>
        ///     No return value
        ///</returns>
        /// <remarks>
        ///
        ///     CheckStringAgainstNullAndEmpty( fooName, "The name parameter ");
        ///
        ///     may get:
        ///
        ///         ArgumentNullException "The name parameter cannot be a null string"
        ///     or
        ///         ArgumentException "The name parameter cannot be an empty string"
        ///
        ///</remarks>
        static internal void CheckStringAgainstNullAndEmpty(string testString,
            string testStringIdentifier)
        {
            if (testString == null)
                throw new ArgumentNullException(testStringIdentifier);

            if (testString.Length == 0)
                throw new ArgumentException(SR.Get(SRID.StringEmpty), testStringIdentifier);
        }

        /// <summary>
        /// Checks if the given string fits within the range of reserved
        /// names and throws an ArgumentException if it does.
        /// The 0x01 through 0x1F characters, serving as the first character of the stream/storage name, 
        /// are reserved for use by OLE. This is a compound file restriction. 
        /// </summary>
        static internal void CheckStringAgainstReservedName(string nameString,
            string nameStringIdentifier)
        {
            if (IsReservedName(nameString))
                throw new ArgumentException(
                    SR.Get(SRID.StringCanNotBeReservedName, nameStringIdentifier));
        }

        /// <summary>
        /// A certain subset of compound file storage and stream names are 
        /// reserved for use be OLE. These are names that start with a character in the
        /// range 0x01 to 0x1F.  (1-31)
        /// This is a compound file restriction
        /// </summary>
        static internal bool IsReservedName(string nameString)
        {
            CheckStringAgainstNullAndEmpty(nameString, "nameString");

            return (nameString[0] >= '\x0001'
                    &&
                    nameString[0] <= '\x001F');
        }

        /// <summary>
        /// Checks for embedded delimiter in the string (as well as Null and Empty)
        /// </summary>
        /// <param name="testString">string to test</param>
        /// <param name="testStringIdentifier">message for exception</param>
        static internal void CheckStringForEmbeddedPathSeparator(string testString,
            string testStringIdentifier)
        {
            CheckStringAgainstNullAndEmpty(testString, testStringIdentifier);

            if (testString.IndexOf(PathSeparator) != -1)
                throw new ArgumentException(
                    SR.Get(SRID.NameCanNotHaveDelimiter,
                        testStringIdentifier,
                        PathSeparator), "testString");
        }

#endif
    }
}
