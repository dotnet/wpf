// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;

namespace System.Windows.Markup
{
    /// <summary>
    /// This class converts DateTime values to/from string.
    /// We don't use the DateTimeConverter because it doesn't support
    /// custom cultures, and in Xaml we require the converter to
    /// support en-us culture.
    /// </summary>
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class DateTimeValueSerializer : ValueSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeConverter" /> class.
        /// </summary>
        public DateTimeValueSerializer() { }

        /// <summary>
        /// Indicate that we do convert DateTime's from string.
        /// </summary>
        public override bool CanConvertFromString(string? value, IValueSerializerContext? context) => true;

        /// <summary>
        /// Indicate that we do convert a DateTime to string.
        /// </summary>
        public override bool CanConvertToString(object? value, IValueSerializerContext? context) => value is DateTime;

        /// <summary>
        /// Converts the given value object to a <see cref="DateTime" />.
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext? context)
        {
            if (value is null)
            {
                throw GetConvertFromException(value);
            }

            if (value.Length == 0)
            {
                return DateTime.MinValue;
            }

            // Set the formatting style for round-tripping and to trim the string.
            const DateTimeStyles DateTimeStyles = DateTimeStyles.RoundtripKind
                                                | DateTimeStyles.NoCurrentDateDefault
                                                | DateTimeStyles.AllowLeadingWhite
                                                | DateTimeStyles.AllowTrailingWhite;
            return DateTime.Parse(value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles);
        }

        /// <summary>
        /// Converts the given value object to a <see cref="DateTime" /> using the arguments.
        /// </summary>
        public override string ConvertToString(object? value, IValueSerializerContext? context)
        {
            if (value is not DateTime dateTime)
            {
                throw GetConvertToException(value, typeof(string));
            }

            // Build up the format string to be used in DateTime.TryParse()
            Span<char> dateTimeSpan = stackalloc char[28];
            Span<char> formatSpan = stackalloc char[36];
            int formatLength = 0;

            "yyyy-MM-dd".CopyTo(formatSpan);
            formatLength += 10;
            if (dateTime.TimeOfDay == TimeSpan.Zero)
            {
                // The time portion of this DateTime is exactly at midnight.
                // We don't include the time component if the Kind is unspecified.
                // Otherwise, we're going to be including the time zone info, so'll
                // we'll have to include the time.
                if (dateTime.Kind != DateTimeKind.Unspecified)
                {
                    "'T'HH':'mm".CopyTo(formatSpan.Slice(formatLength));
                    formatLength += 10;
                }
            }
            else
            {
                long digitsAfterSecond = dateTime.Ticks % 10000000;
                int second = dateTime.Second;
                // We're going to write out at least the hours/minutes
                "'T'HH':'mm".CopyTo(formatSpan.Slice(formatLength));
                formatLength += 10;
                if (second != 0 || digitsAfterSecond != 0)
                {
                    // Need to write out seconds
                    "':'ss".CopyTo(formatSpan.Slice(formatLength));
                    formatLength += 5;
                    if (digitsAfterSecond != 0)
                    {
                        // Need to write out digits after seconds
                        "'.'FFFFFFF".CopyTo(formatSpan.Slice(formatLength));
                        formatLength += 10;
                    }
                }
            }

            // Add the format specifier that indicates we want the DateTimeKind to be
            // included in the output formulation -- UTC gets written out with a "Z",
            // and Local gets written out with e.g. "-08:00" for Pacific Standard Time.
            "K".CopyTo(formatSpan.Slice(formatLength));
            formatLength++;

            // We've finally got our format string built, we can create the string.
            // Note: Technically there's no way this should ever fail, MinSupportedDateTime/MaxSupportedDateTime
            // on InvariantCulture is the same as the Min/Max value for DateTime itself, that means
            // we covered ArgumentOutOfRangeException from ToString(); and FormatException is covered here.
            if (!dateTime.TryFormat(dateTimeSpan, out _, formatSpan.Slice(0, formatLength), DateTimeFormatInfo.InvariantInfo))
                Debug.Assert(false, "TryFormat has failed");

            return new string(dateTimeSpan);
        }
    }
}
