// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Globalization
{
	/// <summary>
	/// Summary description for AutoDataEnums.
	/// </summary>
	/// <summary>
	/// An enumeration of all the calendar available
	/// </summary>
	public enum CalendarEnum
	{
        /// <summary>
        /// Gregorian Calendar, US English
        /// </summary>
		GregorianUSEnglish = 0,
        /// <summary>
        /// Gregorian Calendar, generic localized version
        /// </summary>
		GregorianLocalized,
        /// <summary>
        /// GregorianTransliteralEnglish calendar
        /// </summary>
		GregorianTransliteralEnglish,
        /// <summary>
        /// GregorianTransliteralFrench calendar
        /// </summary>
		GregorianTransliteralFrench,
        /// <summary>
        /// GregorianMiddleEastFrench (Arabic complement) calendar
        /// </summary>
		GregorianMiddleEastFrench,
        /// <summary>
        /// GregorianArabic calendar
        /// </summary>
		GregorianArabic,
        /// <summary>
        /// Hebrew Calendar
        /// </summary>
		Hebrew,
        /// <summary>
        /// Hirji calendar
        /// </summary>
		Hirji,
        /// <summary>
        /// Japanese calendar
        /// </summary>
		Japanese,
        /// <summary>
        /// Julian calendar 
        /// </summary>
		Julian,
        /// <summary>
        /// Korean calendar
        /// </summary>
		Korean,
        /// <summary>
        /// Taiwan Calendar
        /// </summary>
		Taiwan, 
        /// <summary>
        /// ThaiBuddhist calendar
        /// </summary>
		ThaiBuddhist
	}

	/// <summary>
	/// An enumeration of all the scriptType available
	/// </summary>
	public enum ScriptTypeEnum
	{
        /// <summary>
        /// Multiple script types
        /// </summary>
		Mixed =  -1,
        /// <summary>
        /// Latin Script
        /// </summary>
		Latin,
        /// <summary>
        ///  Arabic (RTL) script
        /// </summary>
		Arabic,
        /// <summary>
        /// Armenian script
        /// </summary>
		Armenian,
        /// <summary>
        /// Bengali Script
        /// </summary>
		Bengali,
        /// <summary>
        /// Braille script
        /// </summary>
		Braille,
        /// <summary>
        /// Canadian Aborignal Syllable script
        /// </summary>
		CanadianArboriginalSyllabics,
        /// <summary>
        ///  Cherokee script
        /// </summary>
		Cherokee,
        /// <summary>
        /// Simplified Chinese script
        /// </summary>
		ChineseSimplified,
        /// <summary>
        /// Traditional Chinese script
        /// </summary>
		ChineseTraditional,
        /// <summary>
        ///  Cyrillic script
        /// </summary>
		Cyrillic,
        /// <summary>
        /// Devanagari script
        /// </summary>
		Devanagari,
        /// <summary>
        /// Ethiopic script
        /// </summary>
		Ethiopic,
        /// <summary>
        /// Georgian Script
        /// </summary>
		Georgian,
        /// <summary>
        /// Greek script
        /// </summary>
		Greek,
        /// <summary>
        /// Gujarati script
        /// </summary>
		Gujarati,
        /// <summary>
        /// Gurmukhi Script
        /// </summary>
		Gurmukhi,		
        /// <summary>
        /// Hebrew Script
        /// </summary>
		Hebrew,
        /// <summary>
        /// Japanese script
        /// </summary>
		Japanese,
        /// <summary>
        /// Kannada script
        /// </summary>
		Kannada,
        /// <summary>
        ///  Khmer script
        /// </summary>
		Khmer,
        /// <summary>
        /// Korean Script
        /// </summary>
		Korean,
        /// <summary>
        /// Laotian script
        /// </summary>
		Lao,
        /// <summary>
        /// Malayalam script
        /// </summary>
		Malayalam,
        /// <summary>
        /// Mongolian script
        /// </summary>
		Mongolian,
        /// <summary>
        /// Myanmar script
        /// </summary>
		Myanmar,
        /// <summary>
        /// Ogham script
        /// </summary>
		Ogham,
        /// <summary>
        /// Oriya Script
        /// </summary>
		Oriya,
        /// <summary>
        /// Runnic script
        /// </summary>
		Runnic,
        /// <summary>
        /// Sinhala script
        /// </summary>
		Sinhala,
        /// <summary>
        /// Syriac script
        /// </summary>
		Syriac,
        /// <summary>
        /// Tamil script
        /// </summary>
		Tamil,
        /// <summary>
        /// Telugu script
        /// </summary>
		Telugu,
        /// <summary>
        /// Thaana script
        /// </summary>
		Thaana,
        /// <summary>
        /// Thai script
        /// </summary>
		Thai,
        /// <summary>
        /// Tibetan script
        /// </summary>
		Tibetan,
        /// <summary>
        /// Yi script
        /// </summary>
		Yi,
        /// <summary>
        ///  Surrogate form of script
        /// </summary>
		Surrogate
	}
}