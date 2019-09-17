// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: Row visual is used to group cell visuals of cells 
//              belonging to the row. 
//              The only reason for RowVisual existence is keeping 
//              a reference to the associated row object. 
//
//

using System;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// RowVisual.
    /// </summary>
    internal sealed class RowVisual : ContainerVisual
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="row">Row associated with this visual.</param>
        internal RowVisual(TableRow row)
        {
            _row = row;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Row.
        /// </summary>
        internal TableRow Row
        {
            get { return (_row); }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields 
        private readonly TableRow _row;
        #endregion Private Fields 
    }
}
