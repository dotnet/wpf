// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Converters for IList<double>, IList<ushort>, IList<Point>
//              IList<bool> and IList<char>. 


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using MS.Internal;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Converters
{
    //
    // The following are IList converters for IList<double>, IList<ushort>, IList<bool>, IList<Point>, IList<char>. 
    // More code sharing might be achieved by creating a generic TypeConverter. But XAML parser doesn't 
    // support generic TypeConverter now. 
    //

    /// <summary>
    /// The base converter for IList of T to string conversion in XAML serialization
    /// </summary>    
    [FriendAccessAllowed]   // all implementations are used by Framework at serialization    
    public abstract class BaseIListConverter : TypeConverter 
    { 
        /// <Summary>
        /// Indicates if a type can be converted from (returns true for string).
        /// </Summary>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            return t == typeof(string); // can only convert from string 
        }

        /// <Summary>
        /// Indicates if a type can be converted to (returns true for string).
        /// </Summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) 
        {
            return destinationType == typeof(string); // can only convert to string
        }

        /// <Summary>
        /// Converts from a string.
        /// </Summary>
        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo ci, object value)
        {
            if (null == value)
            {
                throw GetConvertFromException(value);
            }

            string s = value as string;

            if (null == s)
            {
                throw new ArgumentException(SR.Get(SRID.General_BadType, "ConvertFrom"), "value");
            }

            return ConvertFromCore(td, ci, s);            
        }

        /// <Summary>
        /// Converts to a string
        /// </Summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != null)
                return ConvertToCore(context, culture, value, destinationType);

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);
        }
        
        /// <summary>
        /// To be implemented by subclasses to convert to various types from string. 
        /// </summary>
        internal abstract object ConvertFromCore(ITypeDescriptorContext td, CultureInfo ci, string value);

        /// <summary>
        /// To be implemented by subclasses to convert string to various types. 
        /// </summary>
        internal abstract object ConvertToCore(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType);

        internal TokenizerHelper _tokenizer; 
        internal  const char DelimiterChar = ' ';
    }

    /// <summary>
    /// TypeConverter to convert IList of double to and from string.
    /// The converted string is a sequence of delimited double number.
    /// </summary>
    public sealed class DoubleIListConverter : BaseIListConverter
    {
        internal sealed override object ConvertFromCore(ITypeDescriptorContext td, CultureInfo ci, string value)
        {
            _tokenizer = new TokenizerHelper(value, '\0' /* quote char */, DelimiterChar);
            
            // Estimate the output list's capacity from length of the input string. 
            List<double> list = new List<double>(Math.Min(256, value.Length / EstimatedCharCountPerItem + 1));
            
            while (_tokenizer.NextToken())
            {                                
                list.Add(Convert.ToDouble(_tokenizer.GetCurrentToken(), ci));
            }
            return list;
        }

        internal override object ConvertToCore(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            IList<double> list = value as IList<double>;
            if (list == null)
            {
               throw GetConvertToException(value, destinationType);            
            }

            // Estimate the output string's length from the element count of the List. 
            StringBuilder builder = new StringBuilder(EstimatedCharCountPerItem * list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) 
                    builder.Append(DelimiterChar);

                builder.Append(list[i].ToString(culture));
            }

            return builder.ToString();
        }        

        // A double is estimated to take 5 characters on average, plus 1 delimiter, e.g. "5.328 ". 
        private const int EstimatedCharCountPerItem = 6;
    }

    /// <summary>
    /// TypeConverter to convert IList of ushort to and from string.
    /// The converted string is a sequence of delimited ushort number.
    /// </summary>
    public sealed class UShortIListConverter : BaseIListConverter
    {
        internal override object ConvertFromCore(ITypeDescriptorContext td, CultureInfo ci, string value)
        {
            _tokenizer = new TokenizerHelper(value, '\0' /* quote char */, DelimiterChar);
            List<ushort> list = new List<ushort>(Math.Min(256, value.Length / EstimatedCharCountPerItem + 1));
            while (_tokenizer.NextToken())
            {
                list.Add(Convert.ToUInt16(_tokenizer.GetCurrentToken(), ci));
            }
            return list;
        }

        internal override object ConvertToCore(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            IList<ushort> list = value as IList<ushort>;
            if (list == null)
            {
                throw GetConvertToException(value, destinationType);            
            }
            
            StringBuilder builder = new StringBuilder(EstimatedCharCountPerItem * list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) 
                    builder.Append(DelimiterChar);

                builder.Append(list[i].ToString(culture));
            }

            return builder.ToString();
        }        

        // A ushort is estimated to take 2 characters on average, plus 1 delimiter. 
        private const int EstimatedCharCountPerItem = 3;    
    }

    /// <summary>
    /// TypeConverter to convert IList of bool to and from string.
    /// The converted string is a sequence of delimited 0s and 1s. "0" for false and "1" for true. 
    /// </summary>
    public sealed class BoolIListConverter : BaseIListConverter
    {
        internal override object ConvertFromCore(ITypeDescriptorContext td, CultureInfo ci, string value)
        {
             _tokenizer = new TokenizerHelper(value, '\0' /* quote char */, DelimiterChar);            
            List<bool> list = new List<bool>(Math.Min(256, value.Length / EstimatedCharCountPerItem + 1));
            while (_tokenizer.NextToken())
            {
                list.Add(Convert.ToInt32(_tokenizer.GetCurrentToken(), ci) != 0);
            }            
            return list;                                    
        }

        internal override object ConvertToCore(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            IList<bool> list = value as IList<bool>;
            if (list == null)
            {
                throw GetConvertToException(value, destinationType);            
            }
            
            StringBuilder builder = new StringBuilder(EstimatedCharCountPerItem * list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) 
                    builder.Append(DelimiterChar);

                builder.Append((list[i] ? 1 : 0));
            }

            return builder.ToString();      
        }        

        // A bool takes 1 character plus 1 delimiter. 
        private const int EstimatedCharCountPerItem = 2;            
    }

    /// <summary>
    /// TypeConverter to convert IList of Point to and from string.
    /// The converted string is a sequence of delimited Point values. Point values are converted by PointConverter.
    /// </summary>    
    public sealed class PointIListConverter : BaseIListConverter
    {
        internal override object ConvertFromCore(ITypeDescriptorContext td, CultureInfo ci, string value)
        {
            _tokenizer = new TokenizerHelper(value, '\0' /* quote char */, DelimiterChar);
            
            List<Point> list = new List<Point>(Math.Min(256, value.Length / EstimatedCharCountPerItem + 1));
            while (_tokenizer.NextToken())
            {
                list.Add((Point) converter.ConvertFrom(td, ci, _tokenizer.GetCurrentToken()));
            }            
            return list;
        }

        internal override object ConvertToCore(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            IList<Point> list = value as IList<Point>;
            if (list == null)
            {
                throw GetConvertToException(value, destinationType);            
            }
            
            StringBuilder builder = new StringBuilder(EstimatedCharCountPerItem * list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) 
                    builder.Append(DelimiterChar);
                    
                builder.Append((string) converter.ConvertTo(context, culture, list[i], typeof(string)));
            }

            return builder.ToString();
        }        

        private PointConverter converter = new PointConverter();

        // A point takes 2 double and 2 delimiters. Estimated to be 12 characters.         
        private const int EstimatedCharCountPerItem = 12;
    }


    /// <summary>
    /// TypeConverter to convert IList of Char to and from string.
    /// The converted string is a concatenation of all the chars in the IList. There is no delimiter.
    /// </summary>     
    public sealed class CharIListConverter : BaseIListConverter
    {
        internal override object ConvertFromCore(ITypeDescriptorContext td, CultureInfo ci, string value)
        {
            return value.ToCharArray();
        }

        internal override object ConvertToCore(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            IList<char> list = value as IList<char>; 
            if (list == null)
            {
                throw GetConvertToException(value, destinationType);            
            }

            // An intermediate allocation is needed to construct the string as string doesn't take IList<char> 
            // in its constructor. 
            char[] chars = new char[list.Count];
            list.CopyTo(chars, 0);
            return new string(chars);            
        }        
    }
}

