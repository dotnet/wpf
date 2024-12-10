// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Converters;
using System.Windows.Markup;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// Int32Rect - The primitive which represents an integer rectangle.
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int32RectConverter))]
    [ValueSerializer(typeof(Int32RectValueSerializer))] // Used by MarkupWriter
    public struct Int32Rect : IFormattable
    {
        internal int _x;
        internal int _y;
        internal int _width;
        internal int _height;

        /// <summary>
        /// Constructor which sets the initial values to the values of the parameters.
        /// </summary>
        public Int32Rect(int x, int y, int width, int height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        /// <summary>
        ///     X - int.  Default value is 0.
        /// </summary>
        public int X
        {
            readonly get => _x;
            set => _x = value;
        }

        /// <summary>
        ///     Y - int.  Default value is 0.
        /// </summary>
        public int Y
        {
            readonly get => _y;
            set => _y = value;
        }

        /// <summary>
        ///     Width - int.  Default value is 0.
        /// </summary>
        public int Width
        {
            readonly get => _width;
            set => _width = value;
        }

        /// <summary>
        ///     Height - int.  Default value is 0.
        /// </summary>
        public int Height
        {
            readonly get => _height;
            set => _height = value;
        }

        /// <summary>
        /// Empty - a static property which provides an Empty Int32Rectangle.
        /// </summary>
        public static Int32Rect Empty { get; }

        /// <summary>
        /// Returns true if this Int32Rect is the Empty integer rectangle.
        /// </summary>
        public readonly bool IsEmpty => _x == 0 && _y == 0 && _width == 0 && _height == 0;

        /// <summary>
        /// Returns true if this Int32Rect has area.
        /// </summary>
        public readonly bool HasArea => _width > 0 && _height > 0;

        // Various places use an Int32Rect to specify a dirty rect for a
        // bitmap.  The logic for validation is centralized here.  Note that
        // we could do a much better job of validation, but compatibility
        // concerns prevent this until a side-by-side release.
        internal readonly void ValidateForDirtyRect(string paramName, int width, int height)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(_x, paramName);
            ArgumentOutOfRangeException.ThrowIfNegative(_y, paramName);
            ArgumentOutOfRangeException.ThrowIfNegative(_width, paramName);
            ArgumentOutOfRangeException.ThrowIfNegative(_height, paramName);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_width, width, paramName);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(_height, height, paramName);
        }

        /// <summary>
        /// Compares two Int32Rect instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Int32Rect instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='int32Rect1'>The first Int32Rect to compare</param>
        /// <param name='int32Rect2'>The second Int32Rect to compare</param>
        public static bool operator ==(Int32Rect int32Rect1, Int32Rect int32Rect2) =>
            int32Rect1.X == int32Rect2.X
                && int32Rect1.Y == int32Rect2.Y
                && int32Rect1.Width == int32Rect2.Width
                && int32Rect1.Height == int32Rect2.Height;

        /// <summary>
        /// Compares two Int32Rect instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Int32Rect instances are exactly unequal, false otherwise
        /// </returns>
        /// <param name='int32Rect1'>The first Int32Rect to compare</param>
        /// <param name='int32Rect2'>The second Int32Rect to compare</param>
        public static bool operator !=(Int32Rect int32Rect1, Int32Rect int32Rect2) =>
            !(int32Rect1 == int32Rect2);

        /// <summary>
        /// Compares two Int32Rect instances for object equality.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the two Int32Rect instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='int32Rect1'>The first Int32Rect to compare</param>
        /// <param name='int32Rect2'>The second Int32Rect to compare</param>
        public static bool Equals(Int32Rect int32Rect1, Int32Rect int32Rect2) =>
            int32Rect1.IsEmpty
                ? int32Rect2.IsEmpty
                : int32Rect1.X.Equals(int32Rect2.X)
                    && int32Rect1.Y.Equals(int32Rect2.Y)
                    && int32Rect1.Width.Equals(int32Rect2.Width)
                    && int32Rect1.Height.Equals(int32Rect2.Height);

        /// <summary>
        /// Equals - compares this Int32Rect with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of Int32Rect and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override readonly bool Equals(object o) => o is Int32Rect rect && Equals(this, rect);

        /// <summary>
        /// Equals - compares this Int32Rect with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The Int32Rect to compare to "this"</param>
        public readonly bool Equals(Int32Rect value) => Equals(this, value);

        /// <summary>
        /// Returns the HashCode for this Int32Rect
        /// </summary>
        /// <returns>
        /// int - the HashCode for this Int32Rect
        /// </returns>
        public override readonly int GetHashCode() =>
            // Perform field-by-field XOR of HashCodes
            IsEmpty ? 0 : X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();

        /// <summary>
        /// Parse - returns an instance converted from the provided string using
        /// the culture "en-US"
        /// <param name="source"> string with Int32Rect data </param>
        /// </summary>
        public static Int32Rect Parse(string source)
        {
            IFormatProvider formatProvider = TypeConverterHelper.InvariantEnglishUS;

            TokenizerHelper th = new TokenizerHelper(source, formatProvider);

            string firstToken = th.NextTokenRequired();

            // The token will already have had whitespace trimmed so we can do a simple string compare.
            Int32Rect value = firstToken == "Empty"
                ? Empty
                : new Int32Rect(
                    Convert.ToInt32(firstToken, formatProvider),
                    Convert.ToInt32(th.NextTokenRequired(), formatProvider),
                    Convert.ToInt32(th.NextTokenRequired(), formatProvider),
                    Convert.ToInt32(th.NextTokenRequired(), formatProvider));

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
                $"{{1:{format}}}{{0}}{{2:{format}}}{{0}}{{3:{format}}}{{0}}{{4:{format}}}",
                separator,
                _x,
                _y,
                _width,
                _height);
        }
    }

}
