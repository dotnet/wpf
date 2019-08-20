// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: class for input scope definition
//
// Please refer to the design specfication http://avalon/Cicero/Specifications/Stylable%20InputScope.mht
// 
//

using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Markup;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using MS.Internal.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    ///<summary>
    /// InputScope class is a type which InputScope property holds. FrameworkElement.IputScope returns the current inherited InputScope
    /// instance for the element
    ///</summary>
    /// <speclink>http://avalon/Cicero/Specifications/Stylable%20InputScope.mht</speclink>

    [TypeConverter("System.Windows.Input.InputScopeConverter, PresentationCore, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
    public class InputScope
    {
        ///<summary>
        /// Names is of type InputScopeName enum. This is the simpliest way to specify InputScope for an element.
        /// PhraseList is a collection of suggested input patterns. 
        /// Each phrase is of type InputScopePhrase
        ///</summary>
        ///<remarks>
        /// We should support combination of input scope enum values in the future
        ///</remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public System.Collections.IList Names
        {
            get 
            { 
                return (System.Collections.IList)_scopeNames; 
            }
        }

        ///<summary>
        /// SrgsMarkup is currently speech specific. Will be used in non-speech 
        /// input methods in the near future too
        ///</summary>
        [DefaultValue(null)] 
        public String SrgsMarkup
        {
            get 
            { 
                return _srgsMarkup; 
            }
            set 
            { 
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _srgsMarkup = value; 
            }
        }
        
        ///<summary>
        /// RegularExpression is used as a suggested input text pattern
        /// for input processors.
        ///</summary>
        [DefaultValue(null)]
        public String RegularExpression
        {
            get 
            { 
                return _regexString; 
            }
            set 
            { 
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _regexString = value; 
            }
        }
        ///<summary>
        /// PhraseList is a collection of suggested input patterns. 
        /// Each phrase is of type InputScopePhrase
        ///</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public System.Collections.IList PhraseList
        {
            get 
            { 
                return (System.Collections.IList)_phraseList; 
            }
        }


        private  IList<InputScopeName>  _scopeNames = new List<InputScopeName>();
        private  IList<InputScopePhrase>  _phraseList = new List<InputScopePhrase>();
        private  String           _regexString;
        private  String           _srgsMarkup;
}


    
    ///<summary>
    /// InputScopePhrase is a class that implements InputScopePhrase tag
    /// Each InputScopePhrase represents a suggested input text pattern and ususally used to
    /// form a list
    ///</summary>
    [ContentProperty("NameValue")]
    [TypeConverter("System.Windows.Input.InputScopeNameConverter, PresentationCore, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
    public class InputScopeName : IAddChild
    {
        // NOTE: this is a class rather than a simple string so that we can add more hint information
        //           for input phrase such as typing stroke, pronouciation etc.
        //           should be enhanced as needed.
        
        //---------------------------------------------------------------------------
        //
        // Public Methods
        //
        //--------------------------------------------------------------------------- 

 #region Public Methods
        ///<summary>
        /// Default Constructor necesary for parser
        ///</summary>
        public InputScopeName()
        {
        }

        ///<summary>
        /// Constructor that takes name
        ///</summary>
        public InputScopeName(InputScopeNameValue nameValue)
        {
            _nameValue = nameValue;
        }


#region implementation of IAddChild 
        ///<summary>
        /// Called to Add the object as a Child. For InputScopePhrase tag this is ignored
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        public void AddChild(object value) 
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///  if text is present between InputScopePhrase tags, the text is added as a phrase name 
        /// </summary>
        ///<param name="name">
        /// Text string to add
        ///</param>
        public void AddText(string name)
        {
            // throw new System.NotImplementedException();
        }

#endregion IAddChild

#endregion Public Methods

#region class public properties
        ///<summary>
        /// Name property - this is used when accessing the string that is set to InputScopePhrase
        ///</summary>
        public InputScopeNameValue NameValue
        {
            get { return _nameValue; }
            set 
            { 
                if (!IsValidInputScopeNameValue(value))
                {
                    throw new ArgumentException(SR.Get(SRID.InputScope_InvalidInputScopeName, "value"));
                }
                _nameValue = value; 
            }
        }

#endregion class public properties

        ///<summary>
        /// This validates the value for InputScopeName.
        ///</summary>
        private bool IsValidInputScopeNameValue(InputScopeNameValue name)
        {
            switch (name)
            {
                case InputScopeNameValue.Default                   : break; // = 0,  // IS_DEFAULT
                case InputScopeNameValue.Url                       : break; // = 1,  // IS_URL
                case InputScopeNameValue.FullFilePath              : break; // = 2,  // IS_FILE_FULLFILEPATH
                case InputScopeNameValue.FileName                  : break; // = 3,  // IS_FILE_FILENAME
                case InputScopeNameValue.EmailUserName             : break; // = 4,  // IS_EMAIL_USERNAME
                case InputScopeNameValue.EmailSmtpAddress          : break; // = 5,  // IS_EMAIL_SMTPEMAILADDRESS
                case InputScopeNameValue.LogOnName                 : break; // = 6,  // IS_LOGINNAME
                case InputScopeNameValue.PersonalFullName          : break; // = 7,  // IS_PERSONALNAME_FULLNAME
                case InputScopeNameValue.PersonalNamePrefix        : break; // = 8,  // IS_PERSONALNAME_PREFIX
                case InputScopeNameValue.PersonalGivenName         : break; // = 9,  // IS_PERSONALNAME_GIVENNAME
                case InputScopeNameValue.PersonalMiddleName        : break; // = 10, // IS_PERSONALNAME_MIDDLENAME
                case InputScopeNameValue.PersonalSurname           : break; // = 11, // IS_PERSONALNAME_SURNAME
                case InputScopeNameValue.PersonalNameSuffix        : break; // = 12, // IS_PERSONALNAME_SUFFIX
                case InputScopeNameValue.PostalAddress             : break; // = 13, // IS_ADDRESS_FULLPOSTALADDRESS
                case InputScopeNameValue.PostalCode                : break; // = 14, // IS_ADDRESS_POSTALCODE
                case InputScopeNameValue.AddressStreet             : break; // = 15, // IS_ADDRESS_STREET
                case InputScopeNameValue.AddressStateOrProvince    : break; // = 16, // IS_ADDRESS_STATEORPROVINCE
                case InputScopeNameValue.AddressCity               : break; // = 17, // IS_ADDRESS_CITY
                case InputScopeNameValue.AddressCountryName        : break; // = 18, // IS_ADDRESS_COUNTRYNAME
                case InputScopeNameValue.AddressCountryShortName   : break; // = 19, // IS_ADDRESS_COUNTRYSHORTNAME
                case InputScopeNameValue.CurrencyAmountAndSymbol   : break; // = 20, // IS_CURRENCY_AMOUNTANDSYMBOL
                case InputScopeNameValue.CurrencyAmount            : break; // = 21, // IS_CURRENCY_AMOUNT
                case InputScopeNameValue.Date                      : break; // = 22, // IS_DATE_FULLDATE
                case InputScopeNameValue.DateMonth                 : break; // = 23, // IS_DATE_MONTH
                case InputScopeNameValue.DateDay                   : break; // = 24, // IS_DATE_DAY
                case InputScopeNameValue.DateYear                  : break; // = 25, // IS_DATE_YEAR
                case InputScopeNameValue.DateMonthName             : break; // = 26, // IS_DATE_MONTHNAME
                case InputScopeNameValue.DateDayName               : break; // = 27, // IS_DATE_DAYNAME
                case InputScopeNameValue.Digits                    : break; // = 28, // IS_DIGITS
                case InputScopeNameValue.Number                    : break; // = 29, // IS_NUMBER
                case InputScopeNameValue.OneChar                   : break; // = 30, // IS_ONECHAR
                case InputScopeNameValue.Password                  : break; // = 31, // IS_PASSWORD
                case InputScopeNameValue.TelephoneNumber           : break; // = 32, // IS_TELEPHONE_FULLTELEPHONENUMBER
                case InputScopeNameValue.TelephoneCountryCode      : break; // = 33, // IS_TELEPHONE_COUNTRYCODE
                case InputScopeNameValue.TelephoneAreaCode         : break; // = 34, // IS_TELEPHONE_AREACODE
                case InputScopeNameValue.TelephoneLocalNumber      : break; // = 35, // IS_TELEPHONE_LOCALNUMBER
                case InputScopeNameValue.Time                      : break; // = 36, // IS_TIME_FULLTIME
                case InputScopeNameValue.TimeHour                  : break; // = 37, // IS_TIME_HOUR
                case InputScopeNameValue.TimeMinorSec              : break; // = 38, // IS_TIME_MINORSEC
                case InputScopeNameValue.NumberFullWidth           : break; // = 39, // IS_NUMBER_FULLWIDTH
                case InputScopeNameValue.AlphanumericHalfWidth     : break; // = 40, // IS_ALPHANUMERIC_HALFWIDTH
                case InputScopeNameValue.AlphanumericFullWidth     : break; // = 41, // IS_ALPHANUMERIC_FULLWIDTH
                case InputScopeNameValue.CurrencyChinese           : break; // = 42, // IS_CURRENCY_CHINESE
                case InputScopeNameValue.Bopomofo                  : break; // = 43, // IS_BOPOMOFO
                case InputScopeNameValue.Hiragana                  : break; // = 44, // IS_HIRAGANA
                case InputScopeNameValue.KatakanaHalfWidth         : break; // = 45, // IS_KATAKANA_HALFWIDTH
                case InputScopeNameValue.KatakanaFullWidth         : break; // = 46, // IS_KATAKANA_FULLWIDTH
                case InputScopeNameValue.Hanja                     : break; // = 47, // IS_HANJA
                case InputScopeNameValue.PhraseList                : break; // = -1, // IS_PHRASELIST
                case InputScopeNameValue.RegularExpression         : break; // = -2, // IS_REGULAREXPRESSION
                case InputScopeNameValue.Srgs                      : break; // = -3, // IS_SRGS
                case InputScopeNameValue.Xml                       : break; // = -4, // IS_XML
                default: 
                    return false;
            }

            return true;
        }

        private InputScopeNameValue _nameValue;
    }
 
    ///<summary>
    /// InputScopePhrase is a class that implements InputScopePhrase tag
    /// Each InputScopePhrase represents a suggested input text pattern and ususally used to
    /// form a list
    ///</summary>
    [ContentProperty("Name")]
    public class InputScopePhrase : IAddChild
    {
        // NOTE: this is a class rather than a simple string so that we can add more hint information
        //           for input phrase such as typing stroke, pronouciation etc.
        //           should be enhanced as needed.
        
        //---------------------------------------------------------------------------
        //
        // Public Methods
        //
        //--------------------------------------------------------------------------- 

 #region Public Methods
        ///<summary>
        /// Default Constructor necesary for parser
        ///</summary>
        public InputScopePhrase()
        {
        }

        ///<summary>
        /// Constructor that takes name
        ///</summary>
        public InputScopePhrase(String name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            _phraseName = name;
        }

#region implementation of IAddChild 
        ///<summary>
        /// Called to Add the object as a Child. For InputScopePhrase tag this is ignored
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        public void AddChild(object value) 
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///  if text is present between InputScopePhrase tags, the text is added as a phrase name 
        /// </summary>
        ///<param name="name">
        /// Text string to add
        ///</param>
        public void AddText(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            _phraseName = name;
        }

#endregion IAddChild

#endregion Public Methods

#region class public properties
        ///<summary>
        /// Name property - this is used when accessing the string that is set to InputScopePhrase
        ///</summary>
        public String Name
        {
            get { return _phraseName; }
            set { _phraseName = value; }
        }
#endregion class public properties

        private String _phraseName;
    }
}

