// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Numerics;
namespace System.Windows
{
    internal static class IntrinsicExtensions
    {
        /// <summary>
        ///  Throws if the given number is negative, otherwise returns it.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
        public static T OrThrowIfNegative<T>(this T value)
            where T : ISignedNumber<T>
        {
            if (T.IsNegative(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            return value;
        }
    }
}
