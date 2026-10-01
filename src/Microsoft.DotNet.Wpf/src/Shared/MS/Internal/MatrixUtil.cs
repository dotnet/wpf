// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: This file contains the implementation of MatrixUtil, which 
//              provides matrix multiply code.

using System.Windows;
using System.Windows.Media;

namespace MS.Internal
{
    // MatrixTypes
    [System.Flags]
    internal enum MatrixTypes
    {
        TRANSFORM_IS_IDENTITY = 0,
        TRANSFORM_IS_TRANSLATION = 1,
        TRANSFORM_IS_SCALING     = 2,
        TRANSFORM_IS_UNKNOWN     = 4
    }

    internal static class MatrixUtil
    {
        /// <summary>
        /// TransformRect - Internal helper for perf
        /// </summary>
        /// <param name="rect"> The Rect to transform. </param>
        /// <param name="matrix"> The Matrix with which to transform the Rect. </param>
        internal static void TransformRect(ref Rect rect, ref Matrix matrix)
        {
            if (rect.IsEmpty)
            {
                return;
            }

            MatrixTypes matrixType = matrix._type;

            // If the matrix is identity, don't worry.
            if (matrixType == MatrixTypes.TRANSFORM_IS_IDENTITY)
            {
                return;
            }

            // Scaling
            if (0 != (matrixType & MatrixTypes.TRANSFORM_IS_SCALING))
            {
                rect._x *= matrix._m11;
                rect._y *= matrix._m22;
                rect._width *= matrix._m11;
                rect._height *= matrix._m22;

                // Ensure the width is always positive.  For example, if there was a reflection about the
                // y axis followed by a translation into the visual area, the width could be negative.
                if (rect._width < 0.0)
                {
                    rect._x += rect._width;
                    rect._width = -rect._width;
                }

                // Ensure the height is always positive.  For example, if there was a reflection about the
                // x axis followed by a translation into the visual area, the height could be negative.
                if (rect._height < 0.0)
                {
                    rect._y += rect._height;
                    rect._height = -rect._height;
                }
            }

            // Translation
            if (0 != (matrixType & MatrixTypes.TRANSFORM_IS_TRANSLATION))
            {
                // X
                rect._x += matrix._offsetX;

                // Y
                rect._y += matrix._offsetY;
            }

            if (matrixType == MatrixTypes.TRANSFORM_IS_UNKNOWN)
            {
                // Al Bunny implementation.
                Point point0 = matrix.Transform(rect.TopLeft);
                Point point1 = matrix.Transform(rect.TopRight);
                Point point2 = matrix.Transform(rect.BottomRight);
                Point point3 = matrix.Transform(rect.BottomLeft);

                // Width and height is always positive here.
                rect._x = Math.Min(Math.Min(point0.X, point1.X), Math.Min(point2.X, point3.X));
                rect._y = Math.Min(Math.Min(point0.Y, point1.Y), Math.Min(point2.Y, point3.Y));

                rect._width = Math.Max(Math.Max(point0.X, point1.X), Math.Max(point2.X, point3.X)) - rect._x;
                rect._height = Math.Max(Math.Max(point0.Y, point1.Y), Math.Max(point2.Y, point3.Y)) - rect._y;
            }
        }

        /// <summary>
        /// Multiplies two transformations, where the behavior is matrix1 *= matrix2.
        /// This code exists so that we can efficient combine matrices without copying
        /// the data around, since each matrix is 52 bytes.
        /// To reduce duplication and to ensure consistent behavior, this is the
        /// method which is used to implement Matrix * Matrix as well.
        /// </summary>
        internal static void MultiplyMatrix(ref Matrix matrix1, ref Matrix matrix2)
        {
            // Assuming Unknown is correctly exclusive with other flags,
            // there are only 25 allowed matrix combinations,
            // which can easily be numbered from 0 to 25 using arithmetic encoding.
            // Of those 25, many coalesce into the same cases, but it remains to be seen
            // if there is an easy way to coalesce the numeric cases further.

            switch ((uint)matrix1._type * 5 + (uint)matrix2._type)
            {
                case 1:  // I * T
                case 2:  // I * S
                case 3:  // I * S|T
                case 4:  // I * U
                    matrix1 = matrix2;
                    goto case 0;
                case 0:  // I * I
                case 5:  // T * I
                case 10: // S * I
                case 15: // S|T * I
                case 20: // U * I
                    return;
                case 11:  // S * T
                    matrix1._type = MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION;
                    goto case 6;
                case 6:  // T * T
                case 16: // S|T * T
                case 21: // U * T

                    // 2 additions
                    matrix1._offsetX += matrix2._offsetX;
                    matrix1._offsetY += matrix2._offsetY;
                    return;
                case 7:  // T * S
                case 8:  // T * S|T
                case 9:  // T * U

                    // Save off the old offsets
                    double offsetX = matrix1._offsetX;
                    double offsetY = matrix1._offsetY;

                    // Copy the matrix
                    matrix1 = matrix2;

                    matrix1._offsetX = offsetX * matrix2._m11 + offsetY * matrix2._m21 + matrix2._offsetX;
                    matrix1._offsetY = offsetX * matrix2._m12 + offsetY * matrix2._m22 + matrix2._offsetY;
                    matrix1._type = matrix2._type == MatrixTypes.TRANSFORM_IS_UNKNOWN ? MatrixTypes.TRANSFORM_IS_UNKNOWN : MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION;
                    return;

                case 13: // S * S|T
                    matrix1._offsetX = matrix2._offsetX;
                    matrix1._offsetY = matrix2._offsetY;
                    matrix1._type = MatrixTypes.TRANSFORM_IS_TRANSLATION | MatrixTypes.TRANSFORM_IS_SCALING;
                    goto case 12;

                case 17: // S|T * S
                    matrix1._offsetX *= matrix2._m11;
                    matrix1._offsetY *= matrix2._m22;
                    goto case 12;

                case 18: // S|T * S|T
                    matrix1._offsetX = matrix2._m11 * matrix1._offsetX + matrix2._offsetX;
                    matrix1._offsetY = matrix2._m22 * matrix1._offsetY + matrix2._offsetY;
                    goto case 12;

                case 12: // S * S
                    matrix1._m11 *= matrix2._m11;
                    matrix1._m22 *= matrix2._m22;
                    return;
#if DEBUG
                case 14: // S * U
                case 19: // S|T * U
                case 22: // U * S
                case 23: // U * S|T
                case 24: // U * U
#else
                default: // S * U
                         // S|T * U
                         // U * S
                         // U * S|T
                         // U * U
#endif
                    matrix1 = new Matrix(
                        matrix1._m11 * matrix2._m11 + matrix1._m12 * matrix2._m21,
                        matrix1._m11 * matrix2._m12 + matrix1._m12 * matrix2._m22,

                        matrix1._m21 * matrix2._m11 + matrix1._m22 * matrix2._m21,
                        matrix1._m21 * matrix2._m12 + matrix1._m22 * matrix2._m22,

                        matrix1._offsetX * matrix2._m11 + matrix1._offsetY * matrix2._m21 + matrix2._offsetX,
                        matrix1._offsetX * matrix2._m12 + matrix1._offsetY * matrix2._m22 + matrix2._offsetY);
                    return;
#if DEBUG
                default:
                    Debug.Fail("Matrix multiply hit an invalid case: " + ((uint)matrix1._type * 5 + (uint)matrix2._type));
                    break;
#endif
            }
        }        

        /// <summary>
        /// Applies an offset to the specified matrix in place.
        /// </summary>
        internal static void PrependOffset(
            ref Matrix matrix,
            double offsetX,
            double offsetY)
        {
            if (matrix._type == MatrixTypes.TRANSFORM_IS_IDENTITY)
            {
                matrix = new Matrix(1, 0, 0, 1, offsetX, offsetY)
                {
                    _type = MatrixTypes.TRANSFORM_IS_TRANSLATION
                };
            }
            else
            {
                // 
                //  / 1   0   0 \       / m11   m12   0 \
                //  | 0   1   0 |   *   | m21   m22   0 |
                //  \ tx  ty  1 /       \  ox    oy   1 /
                //
                //       /   m11                  m12                     0 \
                //  =    |   m21                  m22                     0 |
                //       \   m11*tx+m21*ty+ox     m12*tx + m22*ty + oy    1 /
                //

                matrix._offsetX += matrix._m11 * offsetX + matrix._m21 * offsetY;
                matrix._offsetY += matrix._m12 * offsetX + matrix._m22 * offsetY;

                // It just gained a translate if was a scale transform. Identity transform is handled above.
                Debug.Assert(matrix._type != MatrixTypes.TRANSFORM_IS_IDENTITY);
                if (matrix._type != MatrixTypes.TRANSFORM_IS_UNKNOWN)
                {
                    matrix._type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
                }
            }
        }
    }
}
