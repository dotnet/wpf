// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Manage the data format.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Data%20Transfer%20Object.htm 
// 
//

using MS.Internal.PresentationCore;

namespace System.Windows
{
    #region DataFormat Class

    /// <summary>
    /// Represents a data format type.
    /// </summary>
    public sealed class DataFormat
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the DataFormat class and specifies format name and id.
        /// </summary>
        public DataFormat(string name, int id)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed)); 
            }

            this._name = name;
            this._id = id;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Specifies the name of this format. 
        /// This field is read-only.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Specifies the Id number for this format. 
        /// This field is read-only.
        /// </summary>
        public int Id
        {
            get
            {
                return _id;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The registered clipboard format name string.
        readonly string _name;

        // The registered clipboard format id.
        readonly int _id;

        #endregion Private Fields
    }

    #endregion DataFormat Class
}
