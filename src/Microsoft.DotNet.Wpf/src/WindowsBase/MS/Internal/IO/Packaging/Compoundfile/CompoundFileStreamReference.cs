// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Definition of the CompoundFileStreamReference class.


using System;
using System.IO;
using System.Text;                          // for StringBuilder

using System.Windows;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// Logical reference to a container stream
    /// </summary>
    /// <remarks>
    /// Use this class to represent a logical reference to a container stream,
    /// </remarks>
    internal class CompoundFileStreamReference : CompoundFileReference, IComparable
    {
        //------------------------------------------------------
        //
        //   Public Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Full path from the root to this stream
        /// </summary>
        public override string FullName 
        {
            get
            {
                return _fullName;
            }
        }


        //------------------------------------------------------
        //
        //   Public Methods
        //
        //------------------------------------------------------
        #region Constructors
        /// <summary>
        /// Use when you know the full path
        /// </summary>
        /// <param name="fullName">whack-delimited name including at least the stream name and optionally including preceding storage names</param>
        public CompoundFileStreamReference(string fullName)
        {
            SetFullName(fullName);
        }

        /// <summary>
        /// Full featured constructor
        /// </summary>
        /// <param name="storageName">optional string describing the storage name - may be null if stream exists in the root storage</param>
        /// <param name="streamName">stream name</param>
        /// <exception cref="ArgumentException">streamName cannot be null or empty</exception>
        public CompoundFileStreamReference(string storageName, string streamName)
        {
            ContainerUtilities.CheckStringAgainstNullAndEmpty( streamName, "streamName" );

            if ((storageName == null) || (storageName.Length == 0))
            {
                _fullName = streamName;
            }
            else
            {
                _fullName = $"{storageName}{ContainerUtilities.PathSeparator}{streamName}";
            }
        }
        #endregion

        #region Operators
        /// <summary>Compare for equality</summary>
        /// <param name="o">the CompoundFileReference to compare to</param>
        public override bool Equals(object o)
        {
            if (o == null)
                return false;   // Standard behavior.

            // support subclassing - our subclasses can call us and do any additive work themselves
            if (o.GetType() != this.GetType())
                return false;

            CompoundFileStreamReference r = (CompoundFileStreamReference)o;
            return (String.CompareOrdinal(_fullName.ToUpperInvariant(), r._fullName.ToUpperInvariant()) == 0);
        }

        /// <summary>Returns an integer suitable for including this object in a hash table</summary>
        public override int GetHashCode()
        {
            return _fullName.GetHashCode();
        }

        #endregion

        #region IComparable
        /// <summary>
        /// Compares two CompoundFileReferences
        /// </summary>
        /// <param name="o">CompoundFileReference to compare to this one</param>
        /// <remarks>Supports the IComparable interface</remarks>
        /// <returns>Less than zero if this instance is less than the given reference, zero if they are equal
        /// and greater than zero if this instance is greater than the given reference.  Not case sensitive.</returns>
        int IComparable.CompareTo(object o)
        {
            if (o == null)
                return 1;   // Standard behavior.

            // different type?
            if (o.GetType() != GetType())
                throw new ArgumentException(
                    SR.CanNotCompareDiffTypes);

            CompoundFileStreamReference r = (CompoundFileStreamReference)o;
            return String.CompareOrdinal(_fullName.ToUpperInvariant(), r._fullName.ToUpperInvariant());
        }
        #endregion

        //------------------------------------------------------
        //
        //   Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Initialize _fullName
        /// </summary>
        /// <remarks>this should only be called from constructors as references are immutable</remarks>
        /// <param name="fullName">string to parse</param>
        /// <exception cref="ArgumentException">if leading or trailing path delimiter</exception>
        private void SetFullName(string fullName)
        {
            ContainerUtilities.CheckStringAgainstNullAndEmpty(fullName, "fullName");

            // fail on leading path separator to match functionality across the board
            // Although we need to do ToUpperInvariant before we do string comparison, in this case
            //  it is not necessary since PathSeparatorAsString is a path symbol
            if (fullName.StartsWith(ContainerUtilities.PathSeparatorAsString, StringComparison.Ordinal))
                throw new ArgumentException(
                    SR.DelimiterLeading, "fullName");

            _fullName = fullName;
            string[] strings = ContainerUtilities.ConvertBackSlashPathToStringArrayPath(fullName);
            if (strings.Length == 0)
                throw new ArgumentException(
                    SR.CompoundFilePathNullEmpty, "fullName");
        }

        //------------------------------------------------------
        //
        //   Private members
        //
        //------------------------------------------------------
        // this can never be null - use String.Empty
        private String _fullName;  // whack-path
    }
}
