// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
        This file contains the implementation of the PageRange class
        and the PageRangeSelection enum for page range support in the
        dialog.
*/

using System;
using System.Globalization;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Enumeration of values for page range options.
    /// </summary>
    public enum PageRangeSelection
    {
        /// <summary>
        /// All pages are printed.
        /// </summary>
        AllPages,
        /// <summary>
        /// A set of user defined pages are printed.
        /// </summary>
        UserPages,
        /// <summary>
        /// The current pages (as defined by the application) are printed.
        /// </summary>
        CurrentPage,
        /// <summary>
        /// The currently selected pages (as defined by the application) are printed.
        /// </summary>
        SelectedPages
    }

    /// <summary>
    /// This class defines one single page range from
    /// a start page to an end page.
    /// </summary>
    public struct PageRange
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of PageRange with one specified page.
        /// </summary>
        /// <param name="page">
        /// Single page of this page range.
        /// </param>
        public
        PageRange(
            int     page
            )
        {
            _pageFrom = page;
            _pageTo = page;
        }

        /// <summary>
        /// Constructs an instance of PageRange with specified values.
        /// </summary>
        /// <param name="pageFrom">
        /// Starting page of this range.
        /// </param>
        /// <param name="pageTo">
        /// Ending page of this range.
        /// </param>
        public
        PageRange(
            int     pageFrom,
            int     pageTo
            )
        {
            _pageFrom = pageFrom;
            _pageTo = pageTo;
        }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets or sets the start page of the page range.
        /// </summary>
        public int PageFrom
        {
            get
            {
                return _pageFrom;
            }
            set
            {
                _pageFrom = value;
            }
        }

        /// <summary>
        /// Gets of sets the end page of the page range.
        /// </summary>
        public int PageTo
        {
            get
            {
                return _pageTo;
            }
            set
            {
                _pageTo = value;
            }
        }

        #endregion Public properties

        #region Private data

        private
        int         _pageFrom;

        private
        int         _pageTo;

        #endregion Private data

        #region Override methods

        /// <summary>
        /// Converts this PageRange structure to its string representation.
        /// </summary>
        /// <returns>
        /// A string value containing the range.
        /// </returns>
        public
        override
        string
        ToString(
            )
        {
            string rangeText;

            if (_pageTo != _pageFrom)
            {
                rangeText = String.Format(CultureInfo.InvariantCulture, SR.Get(SRID.PrintDialogPageRange), _pageFrom, _pageTo);
            }
            else
            {
                rangeText = _pageFrom.ToString(CultureInfo.InvariantCulture);
            }

            return rangeText;
        }

        /// <summary>
        /// Tests equality between this instance and the specified object.
        /// </summary>
        /// <param name="obj">
        /// The object to compare this instance to.
        /// </param>
        /// <returns>
        /// True if obj is equal to this object, else false.
        /// Returns false if obj is not of type PageRange.
        /// </returns>
        public
        override
        bool
        Equals(
            object obj
            )
        {
            if (obj == null || obj.GetType() != typeof(PageRange))
            {
                return false;
            }

            return Equals((PageRange) obj);
        }

        /// <summary>
        ///     Tests equality between this instance and the specified page range.
        /// </summary>
        /// <param name="pageRange">
        ///     The page range to compare this instance to.
        /// </param>
        /// <returns>
        ///     True if the page range is equal to this object, else false.
        /// </returns>
        public
        bool
        Equals(
            PageRange pageRange
            )
        {
            return (pageRange.PageFrom == this.PageFrom) && (pageRange.PageTo == this.PageTo);
        }

        /// <summary>
        /// Calculates a hash code for this PageRange.
        /// </summary>
        /// <returns>
        /// Returns an integer hashcode for this instance.
        /// </returns>
        public
        override
        int
        GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        public
        static
        bool
        operator ==(
            PageRange pr1,
            PageRange pr2
            )
        {
            return pr1.Equals(pr2);
        }

        /// <summary>
        /// Test for inequality.
        /// </summary>
        public
        static
        bool
        operator !=(
            PageRange pr1,
            PageRange pr2
            )
        {
            return !(pr1.Equals(pr2));
        }

        #endregion Override methods
    }
}