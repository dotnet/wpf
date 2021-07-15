// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows
{
    // An item in the source context
    internal struct SourceItem
    {
        #region Construction

        // Constructor for SourceItem
        internal SourceItem(int startIndex, object source)
        {
            _startIndex = startIndex;
            _source = source;
        }
        
        #endregion Construction

        #region Operations

        // Gettor for StartIndex
        internal int StartIndex
        {
            get { return _startIndex; }
        }

        // Gettor for Source
        internal object Source
        {
            get { return _source; }
        }

        /*
        Commented out to avoid "uncalled private code" fxcop violation

        /// <summary>
        ///     Cleanup all the references within the data
        /// </summary>
        internal void Clear()
        {
            _startIndex = -1;
            _source = null;
        }
        */

        /// <summary>
        ///     Is the given object equals the current
        /// </summary>
        public override bool Equals(object o)
        {
            return Equals((SourceItem)o);
        }

        /// <summary>
        ///     Is the given SourceItem equals the current
        /// </summary>
        public bool Equals(SourceItem sourceItem)
        {
            return (
                sourceItem._startIndex == this._startIndex &&
                sourceItem._source == this._source);
        }

        /// <summary>
        ///     Serves as a hash function for a particular type, suitable for use in 
        ///     hashing algorithms and data structures like a hash table
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        /// <summary>
        ///     Equals operator overload
        /// </summary>
        public static bool operator== (SourceItem sourceItem1, SourceItem sourceItem2)
        {
            return sourceItem1.Equals(sourceItem2);
        }

        /// <summary>
        ///     NotEquals operator overload
        /// </summary>
        public static bool operator!= (SourceItem sourceItem1, SourceItem sourceItem2)
        {
            return !sourceItem1.Equals(sourceItem2);
        }
        
        #endregion Operations

        #region Data

        private int _startIndex;
        private object _source;

        #endregion Data
    }
}
