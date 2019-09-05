// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//  Description:    The static class PieceNameHelper contains utilities to parse
//                  and create piece names in an adaptor-independent way.
//                  This file also contains PieceNameInfo, a structured representation
//                  of a piece name, and its subclass PieceInfo, which provides
//                  piece name and zip file info for a piece.
//

using System;
using System.IO;
using System.IO.Packaging;                  // For ZipPackagePart, etc.
using System.Windows;                       // for ExceptionStringTable
using System.Globalization;                 // For CultureInfo
using System.Collections.Generic;           // IEqualityComparer

using MS.Internal;                          // For Invariant
using MS.Internal.IO.Zip;                   // For ZipFileInfo
using System.Diagnostics;                   // For Debug.Assert

using ZipPackage = MS.Internal.IO.Packaging.Extensions.ZipPackage;

namespace MS.Internal.IO.Packaging
{
    #region class PieceInfo

    /// <summary>
    /// A piece descriptor, made up of a ZipFileInfo and a PieceNameInfo.
    /// </summary>
    /// <remarks>
    /// PieceNameHelper implements IComparable in such a way as to enforce
    /// case-insensitive lexicographical order on &lt;name, number, isLast> triples.
    /// </remarks>
    internal class PieceInfo
    {
        //------------------------------------------------------
        //
        //   Constructors
        //
        //------------------------------------------------------

        #region Constructors 

        internal PieceInfo(ZipFileInfo zipFileInfo, PackUriHelper.ValidatedPartUri partUri, string prefixName, int pieceNumber, bool isLastPiece)
        {
            Debug.Assert(zipFileInfo != null);
            Debug.Assert(prefixName != null && prefixName != String.Empty);
            Debug.Assert(pieceNumber >= 0);
                
            _zipFileInfo = zipFileInfo;

            // partUri can be null to indicate that the prefixname is not a valid part name
            _partUri = partUri; 
            _prefixName = prefixName;
            _pieceNumber = pieceNumber;
            _isLastPiece = isLastPiece;

            // Currently as per the book, the prefix names/ logical names should be 
            // compared in a case-insensitive manner.
            _normalizedPieceNamePrefix = _prefixName.ToUpperInvariant();
        }

        #endregion Constructors 

        //------------------------------------------------------
        //
        //   public methods
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //   Internal properties
        //
        //------------------------------------------------------

        #region Internal properties

        internal string NormalizedPrefixName
        {
            get
            {
                return _normalizedPieceNamePrefix;
            }
        }
                
        internal string PrefixName
        {
            get
            {
                return _prefixName;
            }
        }

        internal int PieceNumber
        {
            get
            {
                return _pieceNumber;
            }
        }

        internal bool IsLastPiece
        {
            get
            {
                return _isLastPiece;
            }
        }

        internal System.Uri PartUri
        {
            get
            {
                return _partUri;
            }
        }

        internal ZipFileInfo ZipFileInfo
        {
            get
            {
                return _zipFileInfo;
            }
        }

        #endregion Internal properties

        //------------------------------------------------------
        //
        //   Private members
        //
        //------------------------------------------------------

        #region Private members

        private PackUriHelper.ValidatedPartUri _partUri;
        private string           _prefixName;
        private int              _pieceNumber;
        private bool             _isLastPiece;
        private ZipFileInfo      _zipFileInfo;
        private string           _normalizedPieceNamePrefix;

        #endregion Private members
    }

    #endregion class PieceInfo

    #region class PieceNameHelper

    /// <summary>
    /// The static class PieceNameHelper contains utilities to parse and create piece names
    /// in an adaptor-independent way.
    /// </summary>
    internal static class PieceNameHelper
    {
        #region Internal Properties

        internal static PieceNameComparer PieceNameComparer
        {
            get
            {
                return _pieceNameComparer;
            }
        }

        #endregion Internal Properties

        #region Internal Methods

        /// <summary>
        /// Build a piece name from its constituents: part name, piece number
        /// and terminal status.
        /// The linearized result obeys the piece name syntax:
        ///   piece_name = prefix_name "/" "[" 1*digit "]" [".last"] ".piece"
        /// </summary>
        /// <param name="partName">A part name or the zip item name corresponding to a part name.</param>
        /// <param name="pieceNumber">The 0-based order number of the piece.</param>
        /// <param name="isLastPiece">Whether the piece is last in the part.</param>
        /// <returns>A Metro piece name.</returns>
        /// <exception cref="ArgumentException">If partName is a piece uri.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If pieceNumber is negative.</exception>
        internal static string CreatePieceName(string partName, int pieceNumber, bool isLastPiece)
        {
            Invariant.Assert(pieceNumber >= 0, "Negative piece number.");

            return string.Format(CultureInfo.InvariantCulture, "{0}/[{1:D}]{2}.piece",
                partName,
                pieceNumber,
                isLastPiece ? ".last" : "");
        }
 
        /// <summary>
        /// Return true and create a PieceInfo if the name in the input ZipFileInfo parses
        /// as a piece name.
        /// </summary>
        /// <remarks>
        /// No Uri validation is carried out at this level. All that is checked is valid piece
        /// syntax. So the _prefixName returned as part of the PieceInfo will not necessarily
        /// a part name. For example, it could be the name of the content type stream.
        /// </remarks>
        internal static bool TryCreatePieceInfo(ZipFileInfo zipFileInfo, out PieceInfo pieceInfo)
        {
            Invariant.Assert(zipFileInfo != null);

            pieceInfo = null;

            // Try to parse as a piece name.
            PieceNameInfo pieceNameConstituents;
            bool result = PieceNameHelper.TryParseAsPieceName(zipFileInfo.Name,
                out pieceNameConstituents);

            // Return the result and the output parameter.
            if(result)
                pieceInfo = new PieceInfo(zipFileInfo, 
                    pieceNameConstituents.PartUri,
                    pieceNameConstituents.PrefixName,
                    pieceNameConstituents.PieceNumber,
                    pieceNameConstituents.IsLastPiece);

            return result;
        }

        #endregion Internal Methods

        #region Private Methods

        //------------------------------------------------------
        //
        //   Private Methods
        //
        //------------------------------------------------------

        #region Scan Steps

        // The functions in this region conform to the delegate type ScanStepDelegate
        // and implement the following automaton for scanning a piece name from right to left:

        //   state                      transition      new state
        //   -----                      ----------      ---------
        //   FindPieceExtension         ".piece"        FindIsLast
        //   FindIsLast                 "].last"        FindPieceNumber
        //   FindIsLast                 "]"             FindPieceNumber
        //   FindPieceNumber            "/[" 1*digit    FindPartName (terminal state)

        // On entering the step, position is at the beginning of the last portion that was recognized.
        // So left-to-right scanning starts at position - 1 in each step.
        private delegate bool ScanStepDelegate(
            string path, ref int position, ref ScanStepDelegate nextStep, ref PieceNameInfo parseResults);

        // Look for ".piece".
        private static bool FindPieceExtension(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            if (!FindString(path, ref position, ".piece"))
                return false;

            nextStep = FindIsLast;
            return true;
        }

        // Look for "]" or "].last".
        private static bool FindIsLast(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            // Case of no ".last" member:
            if (path[position - 1] == ']')
            {
                parseResults.IsLastPiece = false;
                --position;
                nextStep = FindPieceNumber;
                return true;
            }

            // There has to be "].last".
            if (!FindString(path, ref position, "].last"))
                return false;

            parseResults.IsLastPiece = true;
            nextStep = FindPieceNumber;
            return true;
        }

        // Look for "/[" followed by decimal digits.
        private static bool FindPieceNumber(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            if (!char.IsDigit(path[position - 1]))
                return false;

            int pieceNumber = 0;
            int multiplier = 1; // rightmost digit is for units
            --position;
            do
            {
                pieceNumber += multiplier * (int)char.GetNumericValue(path[position]);
                multiplier *= 10;
            } while (char.IsDigit(path[--position]));

            // Point to the last digit found.
            ++position;

            //If we have a leading 0, then its not correct piecename syntax
            if (multiplier > 10 && (int)char.GetNumericValue(path[position]) == 0)
                return false;

            if (!FindString(path, ref position, "/["))
                return false;

            parseResults.PieceNumber = pieceNumber;
            nextStep = FindPartName;
            return true;
        }

        // Retrieve part name. The position points to the slash past the part name.
        // So simply return the prefix up to that slash.
        private static bool FindPartName(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            parseResults.PrefixName = path.Substring(0, position);

            // Subtract the length of the part name from position.
            position = 0;

            if (parseResults.PrefixName.Length == 0)
                return false;

            Uri partUri = new Uri(ZipPackage.GetOpcNameFromZipItemName(parseResults.PrefixName), UriKind.Relative);
            PackUriHelper.TryValidatePartUri(partUri, out parseResults.PartUri);
            return true;
        }

        #endregion Scan Steps

        /// <summary>
        /// Attempts to parse a name as a piece name. Returns true and places the
        /// output in pieceNameConstituents. Otherwise, returns false and returns
        /// the default constituent values pieceName, 0, and false.
        /// </summary>
        /// <param name="path">The input string.</param>
        /// <param name="parseResults">An object containing the prefix name (i.e. generally the part name), the 0-based order number of the piece, and whether the piece is last in the part.</param>
        /// <returns>True for parse success.</returns>
        /// <remarks>
        /// Syntax of a piece name:
        ///   piece_name = part_name "/" "[" 1*digit "]" [".last"] ".piece"
        /// </remarks>
        private static bool TryParseAsPieceName(string path, out PieceNameInfo parseResults)
        {
            parseResults = new PieceNameInfo(); // initialize to CLR default values

            // Start from the end and look for ".piece".
            int position = path.Length;
            ScanStepDelegate nextStep = new ScanStepDelegate(FindPieceExtension);

            // Scan backward until the whole path has been scanned.
            while (position > 0)
            {
                if (!nextStep.Invoke(path, ref position, ref nextStep, ref parseResults))
                {
                    // Scan step failed. Return false.
                    parseResults.IsLastPiece = false;
                    parseResults.PieceNumber = 0;
                    parseResults.PrefixName = path;
                    parseResults.PartUri = null;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Look for 'query' backward in 'input' starting at 'position'.
        /// </summary>
        private static bool FindString(string input, ref int position, string query)
        {
            int queryPosition = query.Length;
            
            //The input string should have length that is greater than or equal to the
            //length of the query string.
            if (position < queryPosition)
                return false;
            
            while (--queryPosition >= 0)
            {
                --position;
                if (Char.ToUpperInvariant(input[position]) != Char.ToUpperInvariant(query[queryPosition]))
                    return false;
            }
            return true;
        }

        #endregion Private Methods

        #region Private Member Variables

        //------------------------------------------------------
        //
        //   Private Variables
        //
        //------------------------------------------------------

        private static PieceNameComparer _pieceNameComparer = new PieceNameComparer();

        #endregion Private Member Variables

        #region Private Struct : PieceNameInfo
 
        /// <summary>
        /// The result of parsing a piece name as returned by the parsing methods of PieceNameHelper.
        /// </summary>
        /// <remarks>
        /// <para>        /// The first member, _prefixName, will be a part name if the input to parse begins with
        /// a part name, and a zip item name if it starts with a zip item name.
        /// </para>
        /// <para>
        /// In other words, all that precedes the suffixes is returned unanalyzed as an "prefix name"
        /// by the parse functions of the PieceNameHelper.
        /// </para>
        /// </remarks>
        private struct PieceNameInfo
        {
            internal PackUriHelper.ValidatedPartUri PartUri;
            internal string PrefixName;
            internal int PieceNumber;
            internal bool IsLastPiece;
        }

        #endregion Private Struct : PieceNameInfo
    }

    #endregion class PieceNameHelper

    #region class PieceNameComparer

    internal sealed class PieceNameComparer : IComparer<PieceInfo>
    {
        //For comparing the piece names we consider the prefix name and piece numbers 
        //Pieces that are terminal and non terminal with the same number and same prefix
        //number will be treated as equivalent.
        //For example - /partA/[number].piece and /partA[number].last.piece will be treated 
        //to be equivalent, as in a well-formed package either one of them can be present, 
        //not both.
        int IComparer<PieceInfo>.Compare(PieceInfo pieceInfoA, PieceInfo pieceInfoB)
        {
            //Even though most comparers allow for comparisons with null, we assert here, as
            //this is an internal class and we are sure that pieceInfoA and pieceInfoB passed
            //in here should be non-null, else it would be a logical error.
            Invariant.Assert(pieceInfoA != null);
            Invariant.Assert(pieceInfoB != null);

            int result = string.Compare(
                pieceInfoA.NormalizedPrefixName,
                pieceInfoB.NormalizedPrefixName,
                StringComparison.Ordinal);

            if (result != 0)
                return result;

            result = pieceInfoA.PieceNumber - pieceInfoB.PieceNumber;

            return result;
        }
    }

    #endregion class PieceNameComparer
}

