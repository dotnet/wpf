// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace System.Windows
{
    /// <summary>
    /// Provides a type converter to convert from <see cref="string"/> to <see cref="TextDecorationCollection"/> only.
    /// </summary>     
    public sealed class TextDecorationCollectionConverter : TypeConverter
    {
        /// <summary>
        /// Returns whether this converter can convert the object to the specified
        /// <paramref name="destinationType"/>, using the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context">Context information used for conversion.</param>
        /// <param name="destinationType">Type being evaluated for conversion.</param>
        /// <returns>
        /// <see langword="false"/> will always be returned because <see cref="TextDecorations"/> cannot be converted to any other type.
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // Return false for any other target type. Don't call base.CanConvertTo() because it would be confusing 
            // in some cases. For example, for destination typeof(string), base TypeConverter just converts the
            // ITypeDescriptorContext to the full name string of the given type.
            return destinationType == typeof(InstanceDescriptor);
        }

        /// <summary>
        /// Returns whether this class can convert specific <see cref="Type"/> into <see cref="TextDecorationCollection"/>.
        /// </summary>
        /// <param name="context"> ITypeDescriptorContext </param>
        /// <param name="sourceType">Type being evaluated for conversion.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="sourceType"/> is <see langword="string"/>, otherwise <see langword="false"/>.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Converts <paramref name="input"/> of <see langword="string"/> type to its <see cref="TextDecorationCollection"/> representation.
        /// </summary>
        /// <param name="context">Context information used for conversion, ignored currently.</param>
        /// <param name="culture">The culture specifier to use, ignored currently.</param>        
        /// <param name="input">The string to convert from.</param>
        /// <returns>A <see cref="TextDecorationCollection"/> representing the <see langword="string"/> specified by <paramref name="input"/>.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object input)
        {
            if (input is null)
                throw GetConvertFromException(input);

            if (input is not string value)
                throw new ArgumentException(SR.Format(SR.General_BadType, "ConvertFrom"), nameof(input));

            return ConvertFromString(value);
        }

        /// <summary>
        /// Converts <paramref name="text"/> to its <see cref="TextDecorationCollection"/> representation.
        /// </summary>
        /// <param name="text">The string to be converted into TextDecorationCollection object.</param>
        /// <returns>A <see cref="TextDecorationCollection"/> representing the <see cref="string"/> specified by <paramref name="text"/>.</returns>
        /// <remarks>
        /// The text parameter can be either be <see langword="null"/>; <see cref="string.Empty"/>; the string "None"
        /// or a combination of the predefined <see cref="TextDecorations"/> names delimited by commas (,).
        /// One or more blanks spaces can precede  or follow each text decoration name or comma.
        /// There can't be duplicate TextDecoration names in the string. The operation is case-insensitive. 
        /// </remarks>
        public static new TextDecorationCollection ConvertFromString(string text)
        {
            if (text is null)
                return null;

            // Flags indicating which pre-defined TextDecoration have been matched
            Decorations matchedDecorations = Decorations.None;

            // Sanitize the input
            ReadOnlySpan<char> decorationsSpan = text.AsSpan().Trim();

            // Test for "None", which equals to empty collection and needs to be specified alone
            if (decorationsSpan.IsEmpty || decorationsSpan.Equals("None", StringComparison.OrdinalIgnoreCase))
                return new TextDecorationCollection();

            // Create new collection, save re-allocations
            TextDecorationCollection textDecorations = new(1 + decorationsSpan.Count(','));
            foreach (Range segment in decorationsSpan.Split(','))
            {
                ReadOnlySpan<char> decoration = decorationsSpan[segment].Trim();

                if (decoration.Equals("Overline", StringComparison.OrdinalIgnoreCase) && !matchedDecorations.HasFlag(Decorations.OverlineMatch))
                {
                    textDecorations.Add(TextDecorations.OverLine[0]);
                    matchedDecorations |= Decorations.OverlineMatch;
                }
                else if (decoration.Equals("Baseline", StringComparison.OrdinalIgnoreCase) && !matchedDecorations.HasFlag(Decorations.BaselineMatch))
                {
                    textDecorations.Add(TextDecorations.Baseline[0]);
                    matchedDecorations |= Decorations.BaselineMatch;
                }
                else if (decoration.Equals("Underline", StringComparison.OrdinalIgnoreCase) && !matchedDecorations.HasFlag(Decorations.UnderlineMatch))
                {
                    textDecorations.Add(TextDecorations.Underline[0]);
                    matchedDecorations |= Decorations.UnderlineMatch;
                }
                else if (decoration.Equals("Strikethrough", StringComparison.OrdinalIgnoreCase) && !matchedDecorations.HasFlag(Decorations.StrikethroughMatch))
                {
                    textDecorations.Add(TextDecorations.Strikethrough[0]);
                    matchedDecorations |= Decorations.StrikethroughMatch;
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.InvalidTextDecorationCollectionString, text));
                }
            }

            return textDecorations;
        }

        /// <summary>
        /// Converts a <paramref name="value"/> of <see cref="TextDecorationCollection"/> to the specified <paramref name="destinationType"/>.
        /// </summary>
        /// <param name="context">Context information used for conversion.</param>
        /// <param name="culture">The culture specifier to use.</param>
        /// <param name="value">Duration value to convert from.</param>
        /// <param name="destinationType">Type being evaluated for conversion.</param>
        /// <returns><see langword="null"/> will always be returned because <see cref="TextDecorations"/> cannot be converted to any other type.</returns>        
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor) && value is IEnumerable<TextDecoration>)
            {
                ConstructorInfo ci = typeof(TextDecorationCollection).GetConstructor(new Type[] { typeof(IEnumerable<TextDecoration>) });

                return new InstanceDescriptor(ci, new object[] { value });
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Abstraction helper of matched decorations during conversion.
        /// </summary>
        [Flags]
        private enum Decorations : byte
        {
            None = 0,
            OverlineMatch = 1 << 0,
            BaselineMatch = 1 << 1,
            UnderlineMatch = 1 << 2,
            StrikethroughMatch = 1 << 3,
        }
    }
}
