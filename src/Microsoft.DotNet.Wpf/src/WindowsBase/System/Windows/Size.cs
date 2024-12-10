// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Converters;
using System.Windows.Markup;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// Size - A value type which defined a size in terms of non-negative width and height
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(SizeConverter))]
    [ValueSerializer(typeof(SizeValueSerializer))] // Used by MarkupWriter
    public struct Size : IFormattable
    {
        private double _width;
        private double _height;

        /// <summary>
        /// Constructor which sets the size's initial values.  Width and Height must be non-negative
        /// </summary>
        /// <param name="width"> double - The initial Width </param>
        /// <param name="height"> double - THe initial Height </param>
        public Size(double width, double height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException(SR.Size_WidthAndHeightCannotBeNegative);
            }

            _width = width;
            _height = height;
        }

        /// <summary>
        /// Empty - a static property which provides an Empty size.  Width and Height are 
        /// negative-infinity.  This is the only situation
        /// where size can be negative.
        /// </summary>
        public static Size Empty { get; } = new()
        {
            _width = double.NegativeInfinity,
            _height = double.NegativeInfinity
        };


        /// <summary>
        /// IsEmpty - this returns true if this size is the Empty size.
        /// Note: If size is 0 this Size still contains a 0 or 1 dimensional set
        /// of points, so this method should not be used to check for 0 area.
        /// </summary>
        public readonly bool IsEmpty => _width < 0;

        /// <summary>
        /// Width - Default is 0, must be non-negative
        /// </summary>
        public double Width
        {
            readonly get => _width;
            set
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException(SR.Size_CannotModifyEmptySize);
                }

                if (value < 0)
                {
                    throw new ArgumentException(SR.Size_WidthCannotBeNegative);
                }

                _width = value;
            }
        }

        /// <summary>
        /// Height - Default is 0, must be non-negative.
        /// </summary>
        public double Height
        {
            readonly get => _height;
            set
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException(SR.Size_CannotModifyEmptySize);
                }

                if (value < 0)
                {
                    throw new ArgumentException(SR.Size_HeightCannotBeNegative);
                }

                _height = value;
            }
        }

        /// <summary>
        /// Explicit conversion to Vector.
        /// </summary>
        /// <returns>
        /// Vector - A Vector equal to this Size
        /// </returns>
        /// <param name="size"> Size - the Size to convert to a Vector </param>
        public static explicit operator Vector(Size size) => new(size._width, size._height);

        /// <summary>
        /// Explicit conversion to Point
        /// </summary>
        /// <returns>
        /// Point - A Point equal to this Size
        /// </returns>
        /// <param name="size"> Size - the Size to convert to a Point </param>
        public static explicit operator Point(Size size) => new(size._width, size._height);

        /// <summary>
        /// Compares two Size instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Size instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='size1'>The first Size to compare</param>
        /// <param name='size2'>The second Size to compare</param>
        public static bool operator ==(Size size1, Size size2) =>
            size1.Width == size2.Width && size1.Height == size2.Height;

        /// <summary>
        /// Compares two Size instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Size instances are exactly unequal, false otherwise
        /// </returns>
        /// <param name='size1'>The first Size to compare</param>
        /// <param name='size2'>The second Size to compare</param>
        public static bool operator !=(Size size1, Size size2) => !(size1 == size2);

        /// <summary>
        /// Compares two Size instances for object equality.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the two Size instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='size1'>The first Size to compare</param>
        /// <param name='size2'>The second Size to compare</param>
        public static bool Equals(Size size1, Size size2) =>
            size1.IsEmpty
                ? size2.IsEmpty
                : size1.Width.Equals(size2.Width) && size1.Height.Equals(size2.Height);

        /// <summary>
        /// Equals - compares this Size with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of Size and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override readonly bool Equals(object o) => o is Size size && Equals(this, size);

        /// <summary>
        /// Equals - compares this Size with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The Size to compare to "this"</param>
        public readonly bool Equals(Size value) => Equals(this, value);

        /// <summary>
        /// Returns the HashCode for this Size
        /// </summary>
        /// <returns>
        /// int - the HashCode for this Size
        /// </returns>
        public override readonly int GetHashCode() =>
            // Perform field-by-field XOR of HashCodes
            IsEmpty ? 0 : Width.GetHashCode() ^ Height.GetHashCode();

        /// <summary>
        /// Parse - returns an instance converted from the provided string using
        /// the culture "en-US"
        /// <param name="source"> string with Size data </param>
        /// </summary>
        public static Size Parse(string source)
        {
            IFormatProvider formatProvider = TypeConverterHelper.InvariantEnglishUS;

            TokenizerHelper th = new TokenizerHelper(source, formatProvider);

            string firstToken = th.NextTokenRequired();

            // The token will already have had whitespace trimmed so we can do a simple string compare.
            Size value = firstToken == "Empty"
                ? Empty
                : new Size(
                    Convert.ToDouble(firstToken, formatProvider),
                    Convert.ToDouble(th.NextTokenRequired(), formatProvider));

            // There should be no more tokens in this string.
            th.LastTokenRequired();

            return value;
        }

        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override readonly string ToString() => ConvertToString(format: null, provider: null);

        /// <summary>
        /// Creates a string representation of this object based on the IFormatProvider
        /// passed in.  If the provider is null, the CurrentCulture is used.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public readonly string ToString(IFormatProvider provider) => ConvertToString(format: null, provider);

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        readonly string IFormattable.ToString(string format, IFormatProvider provider) => ConvertToString(format, provider);

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal readonly string ConvertToString(string format, IFormatProvider provider)
        {
            if (IsEmpty)
            {
                return "Empty";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = TokenizerHelper.GetNumericListSeparator(provider);
            return string.Format(
                provider,
                $"{{1:{format}}}{{0}}{{2:{format}}}",
                separator,
                _width,
                _height);
        }
    }
}
