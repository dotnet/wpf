// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Data object interface.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Data%20Transfer%20Object.htm 
// 
//

using System.Security;
using System.Security.Permissions;

namespace System.Windows
{
    #region IDataObject interface

    /// <summary>
    /// Provides a format-independent mechanism for transferring data.
    /// </summary>
    public interface IDataObject
    {
        /// <summary>
        /// Retrieves the data associated with the specified data format.
        /// </summary>
        /// <param name="format">The format of the data to retrieve.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        object GetData(string format);

        /// <summary>
        /// Retrieves the data associated with the specified class 
        /// type format.
        /// </summary>
        /// <param name="format">A Type representing the format of the data to 
        /// retrieve.</param> 
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        object GetData(Type format);

        /// <summary>
        /// Retrieves the data associated with the specified data 
        /// format, using autoConvert to determine whether to convert the data to the
        /// format. 
        /// </summary>
        /// <param name="format">The format of the data to retrieve.</param>
        /// <param name="autoConvert">true to convert the data to the specified format; 
        /// otherwise, false.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        object GetData(string format, bool autoConvert);

        /// <summary>
        /// Determines whether data stored in this instance is associated with, 
        /// or can be converted to, the specified format.
        /// </summary>
        /// <param name="format">The format for which to check.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        bool GetDataPresent(string format);

        /// <summary>
        /// Determines whether data stored in this instance is associated with,
        /// or can be converted to, the specified format.
        /// </summary>
        /// <param name="format">A Type representing the format for which to check.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        bool GetDataPresent(Type format);

        /// <summary>
        /// Determines whether data stored in this instance is 
        /// associated with the specified format, using autoConvert to determine whether to
        /// convert the data to the format.
        /// </summary>
        /// <param name="format">The format for which to check.</param>
        /// <param name="autoConvert">true to determine whether data stored in this instance 
        /// can be converted.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        bool GetDataPresent(string format, bool autoConvert);

        /// <summary>
        /// Gets a list of all formats that data stored in this instance is associated
        /// with or can be converted to.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        string[] GetFormats();

        /// <summary>
        /// Gets a list of all formats that data stored in this 
        /// instance is associated with or can be converted to, using autoConvert to determine whether to
        /// retrieve all formats that the data can be converted to or only native data
        /// formats.
        /// </summary>
        /// <param name="autoConvert">true to retrieve all formats that data stored in this instance is
        /// associated with or can be converted to;
        /// false to retrieve only native data formats.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        string[] GetFormats(bool autoConvert);

        /// <summary>
        /// Stores the specified data in this instance, using the class of the
        /// data for the format.
        /// </summary>
        /// <param name="data">The data to store.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        void SetData(object data);

        /// <summary>
        /// Stores the specified data and its associated format in this
        /// instance.
        /// </summary>
        /// <param name="format">The format associated with the data.</param>
        /// <param name="data">The data to store.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        void SetData(string format, object data);

         /// <summary>
        /// Stores the specified data and its associated class type in this
        /// instance.
        /// </summary>
        /// <param name="format">A Type representing the format associated with the data.</param>
        /// <param name="data">The data to store.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        void SetData(Type format, object data);

        /// <summary>
        /// Stores the specified data and its associated format in 
        /// this instance, using autoConvert to specify whether the data 
        /// can be converted to another format.
        /// </summary>
        /// <param name="format">The format associated with the data.</param>
        /// <param name="data">The data to store.</param>
        /// <param name="autoConvert">true to allow the data to be converted to another format;
        /// Otherwise, false.</param>
        /// <SecurityNote>
        ///     Critical: This code has an inheritance demand since we do not want to allow subclassing of 
        ///     this API in partial trust
        /// </SecurityNote>
        void SetData(string format, object data, bool autoConvert);
     }

    #endregion IDataObject interface
}

