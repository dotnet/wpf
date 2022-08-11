// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Implementation of the CompoundFileStorageReference class.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

using System.IO.Packaging;

using System.Windows;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// Logical reference to a container storage
    /// </summary>
    /// <remarks>
    /// Use this class to represent a logical reference to a container storage,
    /// </remarks>
    internal class CompoundFileStorageReference : CompoundFileReference, IComparable
    {
        //------------------------------------------------------
        //
        //   Public Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Full path from the container root to this storage
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
        //   public methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Make a new Storage Reference
        /// </summary>
        /// <param name="fullName">whack-delimited name</param>
        /// <remarks>pass null or String.Empty to create a reference to the root storage</remarks>
        public CompoundFileStorageReference(string fullName)
        {
            SetFullName(fullName);
        }

        #region Operators
        /// <summary>Compare for equality</summary>
        /// <param name="o">the CompoundFileReference to compare to</param>
        public override bool Equals(object o)
        {
            if (o == null)
                return false;   // Standard behavior.

            // support subclassing - our subclasses can call us and do any additive work themselves
            if (o.GetType() != GetType())
                return false;

            // Note that because of the GetType() checking above, the casting must be valid.
            CompoundFileStorageReference r = (CompoundFileStorageReference)o;
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
        /// <returns>less than zero if this instance is less than the given reference, zero if they are equal
        /// and greater than zero if this instance is greater than the given reference</returns>
        int IComparable.CompareTo(object o)
        {
            if (o == null)
                return 1;   // Standard behavior.

            // different type?
            if (o.GetType() != GetType())
                throw new ArgumentException(
                    SR.CanNotCompareDiffTypes);

            // Note that because of the GetType() checking above, the casting must be valid.
            CompoundFileStorageReference r = (CompoundFileStorageReference)o;
            return String.CompareOrdinal(_fullName.ToUpperInvariant(), r._fullName.ToUpperInvariant());
        }
        #endregion

        //------------------------------------------------------
        //
        //   Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Assign the fullName
        /// </summary>
        /// <param name="fullName">name</param>
        /// <remarks>cache a duplicate copy of the storage name to save having to do this for 
        /// every call to get_Name</remarks>
        /// <exception cref="ArgumentException">if leading or trailing path delimiter</exception>
        private void SetFullName(string fullName)
        {
            if (fullName == null || fullName.Length == 0)
            {
                _fullName = String.Empty;
            }
            else
            {
                // fail on leading path separator to match functionality across the board
                // Although we need to do ToUpperInvariant before we do string comparison, in this case
                //  it is not necessary since PathSeparatorAsString is a path symbol
                if (fullName.StartsWith(ContainerUtilities.PathSeparatorAsString, StringComparison.Ordinal))
                    throw new ArgumentException(
                        SR.DelimiterLeading, "fullName");

                _fullName = fullName;

                // ensure that the string is a legal whack-path
                string[] strings = ContainerUtilities.ConvertBackSlashPathToStringArrayPath(_fullName);
                if (strings.Length == 0)
                    throw new ArgumentException (
                        SR.CompoundFilePathNullEmpty, "fullName");
            }
        }

        //------------------------------------------------------
        //
        //   Private members
        //
        //------------------------------------------------------
        // this can never be null - use String.Empty
        private String  _fullName;  // whack-path
    }
}
