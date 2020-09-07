// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: InputScopeName enumeration
//
//

using System;

namespace System.Windows.Input
{
    /// <summary>
    ///     This is the InputScope enumeration , which can be specified as a document context.
    /// </summary>
    public enum InputScopeNameValue
    {
        // common input scopes
        /// <summary></summary>
        Default                       = 0,  // IS_DEFAULT
        /// <summary></summary>
        Url                           = 1,  // IS_URL
        /// <summary></summary>
        FullFilePath                  = 2,  // IS_FILE_FULLFILEPATH
        /// <summary></summary>
        FileName                      = 3,  // IS_FILE_FILENAME
        /// <summary></summary>
        EmailUserName                 = 4,  // IS_EMAIL_USERNAME
        /// <summary></summary>
        EmailSmtpAddress              = 5,  // IS_EMAIL_SMTPEMAILADDRESS
        /// <summary></summary>
        LogOnName                     = 6,  // IS_LOGINNAME
        /// <summary></summary>
        PersonalFullName              = 7,  // IS_PERSONALNAME_FULLNAME
        /// <summary></summary>
        PersonalNamePrefix            = 8,  // IS_PERSONALNAME_PREFIX
        /// <summary></summary>
        PersonalGivenName             = 9,  // IS_PERSONALNAME_GIVENNAME
        /// <summary></summary>
        PersonalMiddleName            = 10, // IS_PERSONALNAME_MIDDLENAME
        /// <summary></summary>
        PersonalSurname               = 11, // IS_PERSONALNAME_SURNAME
        /// <summary></summary>
        PersonalNameSuffix            = 12, // IS_PERSONALNAME_SUFFIX
        /// <summary></summary>
        PostalAddress                 = 13, // IS_ADDRESS_FULLPOSTALADDRESS
        /// <summary></summary>
        PostalCode                    = 14, // IS_ADDRESS_POSTALCODE
        /// <summary></summary>
        AddressStreet                 = 15, // IS_ADDRESS_STREET
        /// <summary></summary>
        AddressStateOrProvince        = 16, // IS_ADDRESS_STATEORPROVINCE
        /// <summary></summary>
        AddressCity                   = 17, // IS_ADDRESS_CITY
        /// <summary></summary>
        AddressCountryName            = 18, // IS_ADDRESS_COUNTRYNAME
        /// <summary></summary>
        AddressCountryShortName       = 19, // IS_ADDRESS_COUNTRYSHORTNAME
        /// <summary></summary>
        CurrencyAmountAndSymbol       = 20, // IS_CURRENCY_AMOUNTANDSYMBOL
        /// <summary></summary>
        CurrencyAmount                = 21, // IS_CURRENCY_AMOUNT
        /// <summary></summary>
        Date                          = 22, // IS_DATE_FULLDATE
        /// <summary></summary>
        DateMonth                     = 23, // IS_DATE_MONTH
        /// <summary></summary>
        DateDay                       = 24, // IS_DATE_DAY
        /// <summary></summary>
        DateYear                      = 25, // IS_DATE_YEAR
        /// <summary></summary>
        DateMonthName                 = 26, // IS_DATE_MONTHNAME
        /// <summary></summary>
        DateDayName                   = 27, // IS_DATE_DAYNAME
        /// <summary></summary>
        Digits                        = 28, // IS_DIGITS
        /// <summary></summary>
        Number                        = 29, // IS_NUMBER
        /// <summary></summary>
        OneChar                       = 30, // IS_ONECHAR
        /// <summary></summary>
        Password                      = 31, // is_password
        /// <summary></summary>
        TelephoneNumber               = 32, // IS_TELEPHONE_FULLTELEPHONENUMBER
        /// <summary></summary>
        TelephoneCountryCode          = 33, // IS_TELEPHONE_COUNTRYCODE
        /// <summary></summary>
        TelephoneAreaCode             = 34, // IS_TELEPHONE_AREACODE
        /// <summary></summary>
        TelephoneLocalNumber          = 35, // IS_TELEPHONE_LOCALNUMBER
        /// <summary></summary>
        Time                          = 36, // IS_TIME_FULLTIME
        /// <summary></summary>
        TimeHour                      = 37, // IS_TIME_HOUR
        /// <summary></summary>
        TimeMinorSec                  = 38, // IS_TIME_MINORSEC
        /// <summary></summary>
        NumberFullWidth               = 39, // IS_NUMBER_FULLWIDTH
        /// <summary></summary>
        AlphanumericHalfWidth         = 40, // IS_ALPHANUMERIC_HALFWIDTH
        /// <summary></summary>
        AlphanumericFullWidth         = 41, // IS_ALPHANUMERIC_FULLWIDTH
        /// <summary></summary>
        CurrencyChinese               = 42, // IS_CURRENCY_CHINESE
        /// <summary></summary>
        Bopomofo                      = 43, // IS_BOPOMOFO
        /// <summary></summary>
        Hiragana                      = 44, // IS_HIRAGANA
        /// <summary></summary>
        KatakanaHalfWidth             = 45, // IS_KATAKANA_HALFWIDTH
        /// <summary></summary>
        KatakanaFullWidth             = 46, // IS_KATAKANA_FULLWIDTH
        /// <summary></summary>
        Hanja                         = 47, // IS_HANJA
    
    
        // special input scopes for ITfInputScope
        /// <summary></summary>
        PhraseList                     = -1, // IS_PHRASELIST
        /// <summary></summary>
        RegularExpression              = -2, // IS_REGULAREXPRESSION
        /// <summary></summary>
        Srgs                           = -3, // IS_SRGS
        /// <summary></summary>
        Xml                            = -4, // IS_XML
    }
}
