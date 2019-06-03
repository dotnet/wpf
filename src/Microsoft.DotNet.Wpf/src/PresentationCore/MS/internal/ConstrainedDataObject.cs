// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Internal class implemented to primarily disable the XAML cut and paste of content from a
//              partial trust source to a full trust target
//
// See spec at Rich%20Clipboard%20in%20Sandbox%20Spec.doc
// 
//

namespace MS.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows;

    // PreSharp uses message numbers that the C# compiler doesn't know about.
    // Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691
    #region ConstrainedDataObject Class
    /// <summary>
    /// Implements a wrapper class the helps prevent the copy paste of xaml content from partial trust to full trust
    /// This class is instantiated and returned in the case of copy from a partial trust source to a full trust or >partial trust
    /// target. The core functionality here is to strip and deny any requests for XAML content or ApplicationTrust Content in a DataObject
    /// Please note it is by intent that we create a blocked list versus an allowed list of allowed types so as to not block of scenarios like
    /// inking from getting their content in a full trust application if they want to.
    /// </summary>
    internal sealed class ConstrainedDataObject : System.Windows.IDataObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors


        /// <summary>
        /// Initializes a new instance of the  class, containing the specified data.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This acts as a cannister to store a dataobject that will contain XAML and ApplicationTrust format.
        ///     The intent is to prevent that from being exposed. We mark this critical to ensure that this is called an created
        ///     only from known locations. Also some of the interface methods that it implements have inheritance demand.
        /// </SecurityNote>
        [SecurityCritical]
        internal ConstrainedDataObject(System.Windows.IDataObject data)
        {
            // This check guarantees us that we can never create a Constrained data Object with a null dataobject
            Invariant.Assert(data != null);
            _innerData = data;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Retrieves the data associated with the specified data 
        /// format, using an automated conversion parameter to determine whether to convert
        /// the data to the format.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This accesses the _innerDataObject. 
        ///     TreatAsSafe: It filters for the risky information and fails in the case where consumer queries for Xaml or ApplicationTrust
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public object GetData(string format, bool autoConvert)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (IsCriticalFormat(format))
            {
                return null;
            }
            return _innerData.GetData(format, autoConvert);
        }

        /// <summary>
        /// Retrieves the data associated with the specified data 
        /// format.
        /// </summary>
        public object GetData(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            return GetData(format, true);
        }

        /// <summary>
        /// Retrieves the data associated with the specified class 
        /// type format.
        /// </summary>
        public object GetData(Type format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            return GetData(format.FullName);
        }

        /// <summary>
        /// Determines whether data stored in this instance is 
        /// associated with, or can be converted to, the specified
        /// format.
        /// </summary>
        public bool GetDataPresent(Type format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            return (GetDataPresent(format.FullName));
        }

        /// <summary>
        /// Determines whether data stored in this instance is 
        /// associated with the specified format, using an automatic conversion
        /// parameter to determine whether to convert the data to the format.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This accesses the _innerDataObject. 
        ///     TreatAsSafe: It filters for the risky information and fails in the case where consumer queries for Xaml or ApplicationTrust
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public bool GetDataPresent(string format, bool autoConvert)
        {
            bool dataPresent =  false;

            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (!IsCriticalFormat(format))
            {
                dataPresent = _innerData.GetDataPresent(format, autoConvert);
            }
            return dataPresent;
        }

        /// <summary>
        /// Determines whether data stored in this instance is 
        /// associated with, or can be converted to, the specified
        /// format.
        /// </summary>
        public bool GetDataPresent(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            return GetDataPresent(format, true);;
        }

        /// <summary>
        /// Gets a list of all formats that data stored in this 
        /// instance is associated with or can be converted to, using an automatic
        /// conversion parameter <paramref name="autoConvert"/> to
        /// determine whether to retrieve all formats that the data can be converted to or
        /// only native data formats.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code touches _innerData which can expose information about formats we do not want to publicly expose
        ///     for the partial trust to full trust paste scenario.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public string[] GetFormats(bool autoConvert)
        {
            string[] formats = _innerData.GetFormats(autoConvert);
            if (formats != null)
            {
                StripCriticalFormats(formats);
            }
            return formats;
}

        /// <summary>
        /// Gets a list of all formats that data stored in this instance is associated
        /// with or can be converted to.
        /// </summary>
        public string[] GetFormats()
        {
            return GetFormats(true);
        }

        /// <summary>
        /// Stores the specified data in
        /// this instance, using the class of the data for the format.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code touches _innerData
        ///     TreatAsSafe: It does not expose it and relies on the protection in DataObject 
        ///     to block illegitimate conditions
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        public void SetData(object data)
        {
            _innerData.SetData(data);
        }

        /// <summary>
        /// Stores the specified data and its associated format in this
        /// instance.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code touches _innerData
        ///     TreatAsSafe: It does not expose it and relies on the protection in DataObject 
        ///     to block illegitimate conditions
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void SetData(string format, object data)
        {
            _innerData.SetData(format, data);
        }

        /// <summary>
        /// Stores the specified data and 
        /// its associated class type in this instance.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code touches _innerData
        ///     TreatAsSafe: It does not expose it and relies on the protection in DataObject 
        ///     to block illegitimate conditions
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void SetData(Type format, object data)
        {
            _innerData.SetData(format, data);
        }

        /// <summary>
        /// Stores the specified data and its associated format in 
        /// this instance, using the automatic conversion parameter
        /// to specify whether the
        /// data can be converted to another format.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code touches _innerData
        ///     TreatAsSafe: It does not expose it and relies on the protection in DataObject 
        ///     to block illegitimate conditions
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void SetData(string format, Object data, bool autoConvert)
        {
            _innerData.SetData(format, data, autoConvert);
        }



        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Return true if the format string are equal(Case-senstive).
        /// </summary>
        private static bool IsFormatEqual(string format1, string format2)
        {
            return (String.CompareOrdinal(format1, format2) == 0);
        }


        /// <summary>
        /// This code looks for Xaml and ApplicationTrust strings in an array of strings and removed them. The reason for that is 
        /// that since the only scenario this class is used in is when the target application has more permissions than the source then
        /// we want to ensure that the target application cannot get to xaml and application trust formats if they come out of a partial trust source.
        /// </summary>
        private string[] StripCriticalFormats(string[] formats)
        {
            List<string> resultList = new List<string>();
            for (uint currentFormat = 0; currentFormat < formats.Length; currentFormat++)
            {
                if (!IsCriticalFormat(formats[currentFormat]))
                {
                    resultList.Add(formats[currentFormat]);
                }
            }
            return resultList.ToArray();
        }

        /// <SecurityNote>
        ///     Critical: This code is used to determine whether information returned is secure or not
        ///     TreatAsSafe: This function is critical only for tracking purposes
        /// </SecurityNote>
        /// <param name="format"></param>
        [SecurityCritical, SecurityTreatAsSafe]
        private bool IsCriticalFormat(string format)
        {
            return (IsFormatEqual(format, DataFormats.Xaml) ||
                IsFormatEqual(format, DataFormats.ApplicationTrust));
        }
        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        // Inner data object of IDataObject.
        /// <SecurityNote>
        ///     This member holds a reference to a dataobject which if exposed allows client code in an app to get to the XAML
        ///     content on the clipboard. This is deisabled for the scenario where target application has more permissions than source of 
        ///     data object and that is the only scenario where we create an instance of this class.
        /// </SecurityNote>
        [SecurityCritical]
        private System.Windows.IDataObject _innerData;
        #endregion Private Fields


    }
    #endregion ConstrainedDataObject Class
}
