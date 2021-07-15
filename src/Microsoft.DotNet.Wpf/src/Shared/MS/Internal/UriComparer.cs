// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
    Module Name:                                                                
        UriEqualityComparer.cs                                                             
                                                                              
    Abstract:
        This file implements an IEqualityComparer for System.Uri
	System.Uri.Equals ignores the uri fragment which means
	http://example.com/page is equal to http://example.com/page#fragment
        This causes cache lookups in hash tables and dictionaries to fail
                
--*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MS.Internal
{
    /// <summary>
    /// Implements the functionality to generate font subsets
    /// based on glyph runs obtained.  This class uses the
    /// serialization manager to write data to the Xps
    /// package.
    /// </summary>
    internal class UriComparer : IEqualityComparer<Uri>
    {
        #region Public properties

        /// <summary>
        /// Ordinal UriComparer.
        /// </summary>
        public static UriComparer Default
        {
            get { return _default; }
        }

        #endregion Public properties

        #region Public methods

        /// <summary>
        /// Compares two Uri's for equality
        /// </summary>
        /// <param name="a">
        /// Uri to compare
        /// </param>
        /// <param name="b">
        /// Uri to compare
        /// </param>
        /// <returns>
        /// True if the Uri's are equal
        /// </returns>
        public bool Equals(Uri a, Uri b)
        {
            if(object.ReferenceEquals(a, b))
            {
                return true;
            }
            
            if (a == null || b == null)
            {
                return false;
            }

            return String.Equals(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets a hash code that agrees with the comparers definition of equality.
        /// </summary>
        /// <param name="uri">
        /// Uri to get hash code for.
        /// </param>
        /// <return>
        /// A hash code that agrees with the comparers definition of equality.
        /// </return>
        public int GetHashCode(Uri uri)
        {
            if (uri == null)
            {
                return _nullHashcode;
            }

            string uriAsString = uri.ToString();
            if(uriAsString == null)
            {
                return _nullHashcode;
            }

            return uriAsString.GetHashCode();
        }

        #endregion Public methods

        #region Private data

        private static readonly UriComparer _default = new UriComparer();
        private static readonly int _nullHashcode = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(null);

        #endregion Private data
    }
}