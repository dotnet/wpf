// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: TextDecorationCollectionConverter class
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    /// TypeConverter for TextDecorationCollection 
    /// </summary>     
    public sealed class TextDecorationCollectionConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertTo method
        /// </summary>
        /// <param name="context"> ITypeDescriptorContext </param>
        /// <param name="destinationType"> Type to convert to </param>
        /// <returns> false will always be returned because TextDecorations cannot be converted to any other type. </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }

            // return false for any other target type. Don't call base.CanConvertTo() because it would be confusing 
            // in some cases. For example, for destination typeof(String), base convertor just converts the TDC to the 
            // string full name of the type. 
            return false; 
        }

        /// <summary>
        /// CanConvertFrom
        /// </summary>
        /// <param name="context"> ITypeDescriptorContext </param>
        /// <param name="sourceType">Type to convert to </param>
        /// <returns> true if it can convert from sourceType to TextDecorations, false otherwise </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// ConvertFrom
        /// </summary>
        /// <param name="context"> ITypeDescriptorContext </param>
        /// <param name="culture"> CultureInfo </param>        
        /// <param name="input"> The input object to be converted to TextDecorations </param>
        /// <returns> the converted value of the input object </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object input)
        {
            if (input == null)
            {
                throw GetConvertFromException(input);
            }

            string value = input as string; 
            
            if (null == value)
            {
                throw new ArgumentException(SR.Get(SRID.General_BadType, "ConvertFrom"), "input");
            }                       
                        
            return ConvertFromString(value);            
        }

        /// <summary>
        /// ConvertFromString
        /// </summary>
        /// <param name="text"> The string to be converted into TextDecorationCollection object </param>
        /// <returns> the converted value of the string flag </returns>
        /// <remarks>
        /// The text parameter can be either string "None" or a combination of the predefined 
        /// TextDecoration names delimited by commas (,). One or more blanks spaces can precede 
        /// or follow each text decoration name or comma. There can't be duplicate TextDecoration names in the 
        /// string. The operation is case-insensitive. 
        /// </remarks>
        public static new TextDecorationCollection ConvertFromString(string text)
        {   
            if (text == null)
            {
               return null;
            }

            TextDecorationCollection textDecorations = new TextDecorationCollection();

            // Flags indicating which Predefined textDecoration has alrady been added.
            byte MatchedTextDecorationFlags = 0;
            
            // Start from the 1st non-whitespace character
            // Negative index means error is encountered
            int index = AdvanceToNextNonWhiteSpace(text, 0);                 
            while (index >= 0 && index < text.Length)
            {
                if (Match(None, text, index))
                {
                    // Matched "None" in the input
                    index = AdvanceToNextNonWhiteSpace(text, index + None.Length);                    
                    if (textDecorations.Count > 0 || index < text.Length)
                    {                        
                        // Error: "None" can only be specified by its own
                        index = -1;                        
                    }
                }
                else
                {
                    // Match the input with one of the predefined text decoration names
                    int i;
                    for(i = 0; 
                           i < TextDecorationNames.Length 
                        && !Match(TextDecorationNames[i], text, index);
                       i++
                    );

                    if (i < TextDecorationNames.Length)
                    {
                        // Found a match within the predefined names
                        if ((MatchedTextDecorationFlags & (1 << i)) > 0)
                        {
                            // Error: The matched value is duplicated.
                            index = -1;
                        }
                        else
                        {
                            // Valid match. Add to the collection and remember that this text decoration
                            // has been added
                            textDecorations.Add(PredefinedTextDecorations[i]);
                            MatchedTextDecorationFlags |= (byte)(1 << i);

                            // Advance to the start of next name
                            index = AdvanceToNextNameStart(text, index + TextDecorationNames[i].Length);                            
                        }
                    }
                    else
                    {
                        // Error: no match found in the predefined names
                        index = -1;
                    }
                }
            }

            if (index < 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidTextDecorationCollectionString, text));
            }
            
            return textDecorations;            
        }        

        /// <summary>
        /// ConvertTo
        /// </summary>
        /// <param name="context"> ITypeDescriptorContext </param>
        /// <param name="culture"> CultureInfo </param>        
        /// <param name="value"> the object to be converted to another type </param>
        /// <param name="destinationType"> The destination type of the conversion </param>
        /// <returns> null will always be returned because TextDecorations cannot be converted to any other type. </returns>        
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor) && value is IEnumerable<TextDecoration>)
            {
                ConstructorInfo ci = typeof(TextDecorationCollection).GetConstructor(
                    new Type[]{typeof(IEnumerable<TextDecoration>)}
                    );
                    
                return new InstanceDescriptor(ci, new object[]{value});                
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);              
        }

        //---------------------------------
        // Private methods
        //---------------------------------
        /// <summary>
        /// Match the input against a predefined pattern from certain index onwards
        /// </summary>
        private static bool Match(string pattern, string input, int index)
        {
            int i = 0;
            for (;
                    i < pattern.Length
                 && index + i < input.Length
                 && pattern[i] == Char.ToUpperInvariant(input[index + i]);
                 i++) ;

            return (i == pattern.Length);
        }

        /// <summary>
        /// Advance to the start of next name
        /// </summary>
        private static int AdvanceToNextNameStart(string input, int index)
        {
            // Two names must be seperated by a comma and optionally spaces
            int separator = AdvanceToNextNonWhiteSpace(input, index);

            int nextNameStart;
            if (separator >= input.Length)
            {
                // reach the end
                nextNameStart = input.Length;
            }
            else
            {
                if (input[separator] == Separator)
                {                    
                    nextNameStart = AdvanceToNextNonWhiteSpace(input, separator + 1);
                    if (nextNameStart >= input.Length)
                    {
                        // Error: Separator is at the end of the input
                        nextNameStart = -1;
                    }
                }
                else
                {
                    // Error: There is a non-whitespace, non-separator character following
                    // the matched value
                    nextNameStart = -1;
                }
            }

            return nextNameStart;
        }

        /// <summary>
        /// Advance to the next non-whitespace character
        /// </summary>
        private static int AdvanceToNextNonWhiteSpace(string input, int index)
        {
            for (; index < input.Length && Char.IsWhiteSpace(input[index]); index++) ;
            return (index > input.Length) ? input.Length : index;
        }

        //---------------------------------
        // Private members
        //---------------------------------

        //
        // Predefined valid names for TextDecorations
        // Names should be normalized to be upper case
        //
        private const string None    = "NONE";
        private const char Separator = ',';        
        
        private static readonly string[] TextDecorationNames = new string[] {            
            "OVERLINE",  
            "BASELINE",
            "UNDERLINE",
            "STRIKETHROUGH"
            };

        // Predefined TextDecorationCollection values. It should match 
        // the TextDecorationNames array
        private static readonly TextDecorationCollection[] PredefinedTextDecorations = 
            new TextDecorationCollection[] {
                TextDecorations.OverLine,
                TextDecorations.Baseline,
                TextDecorations.Underline,
                TextDecorations.Strikethrough           
                };       
}             
}
