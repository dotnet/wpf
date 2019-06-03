// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MS.Internal.PresentationCore
{
    /// <summary>
    /// Note: The contents of each region are in alphabetical order by type,
    /// please maintain this.
    /// </summary>
    internal static class AnimatedTypeHelpers
    {
        #region Interpolation Methods

        internal static Byte InterpolateByte(Byte from, Byte to, Double progress)
        {
            return (Byte)((Int32)from + (Int32)((((Double)(to - from)) + (Double)0.5) * progress));
        }
 
        internal static Color InterpolateColor(Color from, Color to, Double progress)
        {
            return from + ((to - from) * (Single)progress);
        }

        internal static Decimal InterpolateDecimal(Decimal from, Decimal to, Double progress)
        {
            return from + ((to - from) * (Decimal)progress);
        }

        internal static Double InterpolateDouble(Double from, Double to, Double progress)
        {
            return from + ((to - from) * progress);
        }

        internal static Int16 InterpolateInt16(Int16 from, Int16 to, Double progress)
        {
            if (progress == 0.0)
            {
                return from;
            }
            else if (progress == 1.0)
            {
                return to;
            }
            else
            {
                Double addend = (Double)(to - from);
                addend *= progress;
                addend += (addend > 0.0) ? 0.5 : -0.5;

                return (Int16)(from + (Int16)addend);
            }
        }

        internal static Int32 InterpolateInt32(Int32 from, Int32 to, Double progress)
        {
            if (progress == 0.0)
            {
                return from;
            }
            else if (progress == 1.0)
            {
                return to;
            }
            else
            {
                Double addend = (Double)(to - from);
                addend *= progress;
                addend += (addend > 0.0) ? 0.5 : -0.5;

                return from + (Int32)addend;
            }
        }

        internal static Int64 InterpolateInt64(Int64 from, Int64 to, Double progress)
        {
            if (progress == 0.0)
            {
                return from;
            }
            else if (progress == 1.0)
            {
                return to;
            }
            else
            {
                Double addend = (Double)(to - from);
                addend *= progress;
                addend += (addend > 0.0) ? 0.5 : -0.5;

                return from + (Int64)addend;
            }
        }

        internal static Point InterpolatePoint(Point from, Point to, Double progress)
        {
            return from + ((to - from) * progress);
        }

        internal static Point3D InterpolatePoint3D(Point3D from, Point3D to, Double progress)
        {
            return from + ((to - from) * progress);
        }

        internal static Quaternion InterpolateQuaternion(Quaternion from, Quaternion to, Double progress, bool useShortestPath)
        {
            return Quaternion.Slerp(from, to, progress, useShortestPath);
        }

        internal static Rect InterpolateRect(Rect from, Rect to, Double progress)
        {
            Rect temp = new Rect();

            // from + ((from - to) * progress)
            temp.Location = new Point(
                from.Location.X + ((to.Location.X - from.Location.X) * progress),
                from.Location.Y + ((to.Location.Y - from.Location.Y) * progress));
            temp.Size = new Size(
                from.Size.Width + ((to.Size.Width - from.Size.Width) * progress),
                from.Size.Height + ((to.Size.Height - from.Size.Height) * progress));

            return temp;
        }

        internal static Rotation3D InterpolateRotation3D(Rotation3D from, Rotation3D to, Double progress)
        {
            return new QuaternionRotation3D(InterpolateQuaternion(from.InternalQuaternion, to.InternalQuaternion, progress, /* useShortestPath = */ true));
        }
        
        internal static Single InterpolateSingle(Single from, Single to, Double progress)
        {
            return from + (Single)((to - from) * progress);
        }

        internal static Size InterpolateSize(Size from, Size to, Double progress)
        {
            return (Size)InterpolateVector((Vector)from, (Vector)to, progress);
        }

        internal static System.Windows.Vector InterpolateVector(System.Windows.Vector from, System.Windows.Vector to, Double progress)
        {
            return from + ((to - from) * progress);
        }

        internal static Vector3D InterpolateVector3D(Vector3D from, Vector3D to, Double progress)
        {
            return from + ((to - from) * progress);
        }

        #endregion

        #region Add Methods

        internal static Byte AddByte(Byte value1, Byte value2)
        {
            return (Byte)(value1 + value2);
        }

        internal static Color AddColor(Color value1, Color value2)
        {
            return value1 + value2;
        }

        internal static Decimal AddDecimal(Decimal value1, Decimal value2)
        {
            return value1 + value2;
        }

        internal static Double AddDouble(Double value1, Double value2)
        {
            return value1 + value2;
        }

        internal static Int16 AddInt16(Int16 value1, Int16 value2)
        {
            return (Int16)(value1 + value2);
        }

        internal static Int32 AddInt32(Int32 value1, Int32 value2)
        {
            return value1 + value2;
        }

        internal static Int64 AddInt64(Int64 value1, Int64 value2)
        {
            return value1 + value2;
        }

        internal static Point AddPoint(Point value1, Point value2)
        {
            return new Point(
                value1.X + value2.X, 
                value1.Y + value2.Y);
        }

        internal static Point3D AddPoint3D(Point3D value1, Point3D value2)
        {
            return new Point3D(
                value1.X + value2.X,
                value1.Y + value2.Y,
                value1.Z + value2.Z);
        }

        internal static Quaternion AddQuaternion(Quaternion value1, Quaternion value2)
        {
            return value1 * value2;
        }

        internal static Single AddSingle(Single value1, Single value2)
        {
            return value1 + value2;
        }

        internal static Size AddSize(Size value1, Size value2)
        {
            return new Size(
                value1.Width + value2.Width,
                value1.Height + value2.Height);
        }

        internal static System.Windows.Vector AddVector(System.Windows.Vector value1, System.Windows.Vector value2)
        {
            return value1 + value2;
        }

        internal static Vector3D AddVector3D(Vector3D value1, Vector3D value2)
        {
            return value1 + value2;
        }

        internal static Rect AddRect(Rect value1, Rect value2)
        {
            return new Rect(
                AddPoint(value1.Location, value2.Location),
                AddSize(value1.Size, value2.Size));
        }

        internal static Rotation3D AddRotation3D(Rotation3D value1, Rotation3D value2)
        {
            if (value1 == null)
            {
                value1 = Rotation3D.Identity;
            }
            if (value2 == null)
            {
                value2 = Rotation3D.Identity;
            }

            return new QuaternionRotation3D(AddQuaternion(value1.InternalQuaternion, value2.InternalQuaternion));
        }

        #endregion

        #region Subtract Methods

        internal static Byte SubtractByte(Byte value1, Byte value2)
        {
            return (Byte)(value1 - value2);
        }

        internal static Color SubtractColor(Color value1, Color value2)
        {
            return value1 - value2;
        }

        internal static Decimal SubtractDecimal(Decimal value1, Decimal value2)
        {
            return value1 - value2;
        }

        internal static Double SubtractDouble(Double value1, Double value2)
        {
            return value1 - value2;
        }

        internal static Int16 SubtractInt16(Int16 value1, Int16 value2)
        {
            return (Int16)(value1 - value2);
        }

        internal static Int32 SubtractInt32(Int32 value1, Int32 value2)
        {
            return value1 - value2;
        }

        internal static Int64 SubtractInt64(Int64 value1, Int64 value2)
        {
            return value1 - value2;
        }

        internal static Point SubtractPoint(Point value1, Point value2)
        {
            return new Point(
                value1.X - value2.X,
                value1.Y - value2.Y);
        }

        internal static Point3D SubtractPoint3D(Point3D value1, Point3D value2)
        {
            return new Point3D(
                value1.X - value2.X,
                value1.Y - value2.Y,
                value1.Z - value2.Z);
        }

        internal static Quaternion SubtractQuaternion(Quaternion value1, Quaternion value2)
        {
            value2.Invert();
            
            return value1 * value2;
        }

        internal static Single SubtractSingle(Single value1, Single value2)
        {
            return value1 - value2;
        }

        internal static Size SubtractSize(Size value1, Size value2)
        {
            return new Size(
                value1.Width - value2.Width,
                value1.Height - value2.Height);
        }

        internal static System.Windows.Vector SubtractVector(System.Windows.Vector value1, System.Windows.Vector value2)
        {
            return value1 - value2;
        }

        internal static Vector3D SubtractVector3D(Vector3D value1, Vector3D value2)
        {
            return value1 - value2;
        }

        internal static Rect SubtractRect(Rect value1, Rect value2)
        {
            return new Rect(
                SubtractPoint(value1.Location, value2.Location),
                SubtractSize(value1.Size, value2.Size));
        }

        internal static Rotation3D SubtractRotation3D(Rotation3D value1, Rotation3D value2)
        {
            return new QuaternionRotation3D(SubtractQuaternion(value1.InternalQuaternion, value2.InternalQuaternion));
        }

        #endregion

        #region GetSegmentLength Methods

        internal static Double GetSegmentLengthBoolean(Boolean from, Boolean to)
        {
            if (from != to)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        internal static Double GetSegmentLengthByte(Byte from, Byte to)
        {
            return Math.Abs((Int32)to - (Int32)from);
        }

        internal static Double GetSegmentLengthChar(Char from, Char to)
        {
            if (from != to)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        internal static Double GetSegmentLengthColor(Color from, Color to)
        {
            return Math.Abs(to.ScA - from.ScA) 
                 + Math.Abs(to.ScR - from.ScR)
                 + Math.Abs(to.ScG - from.ScG)
                 + Math.Abs(to.ScB - from.ScB);
        }

        internal static Double GetSegmentLengthDecimal(Decimal from, Decimal to)
        {
            // We may lose precision here, but it's not likely going to be a big deal
            // for the purposes of this method.  The relative lengths of Decimal
            // segments will still be adequately represented.
            return (Double)Math.Abs(to - from);
        }

        internal static Double GetSegmentLengthDouble(Double from, Double to)
        {
            return Math.Abs(to - from);
        }

        internal static Double GetSegmentLengthInt16(Int16 from, Int16 to)
        {
            return Math.Abs(to - from);
        }

        internal static Double GetSegmentLengthInt32(Int32 from, Int32 to)
        {
            return Math.Abs(to - from);
        }

        internal static Double GetSegmentLengthInt64(Int64 from, Int64 to)
        {
            return Math.Abs(to - from);
        }

        internal static Double GetSegmentLengthMatrix(Matrix from, Matrix to)
        {
            if (from != to)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        internal static Double GetSegmentLengthObject(Object from, Object to)
        {
            return 1.0;
        }

        internal static Double GetSegmentLengthPoint(Point from, Point to)
        {
            return Math.Abs((to - from).Length);
        }

        internal static Double GetSegmentLengthPoint3D(Point3D from, Point3D to)
        {
            return Math.Abs((to - from).Length);
        }

        internal static Double GetSegmentLengthQuaternion(Quaternion from, Quaternion to)
        {
            from.Invert();

            return (to * from).Angle;
        }

        internal static Double GetSegmentLengthRect(Rect from, Rect to)
        {
            // This seems to me to be the most logical way to define the
            // distance between two rects.  Lots of sqrt, but since paced
            // rectangle animations are such a rare thing, we may as well do
            // them right since the user obviously knows what they want.
            Double a = GetSegmentLengthPoint(from.Location, to.Location);
            Double b = GetSegmentLengthSize(from.Size, to.Size);

            // Return c.
            return Math.Sqrt((a * a) + (b * b));
        }

        internal static Double GetSegmentLengthRotation3D(Rotation3D from, Rotation3D to)
        {
            return GetSegmentLengthQuaternion(from.InternalQuaternion, to.InternalQuaternion);
        }

        internal static Double GetSegmentLengthSingle(Single from, Single to)
        {
            return Math.Abs(to - from);
        }

        internal static Double GetSegmentLengthSize(Size from, Size to)
        {
            return Math.Abs(((System.Windows.Vector)to - (System.Windows.Vector)from).Length);
        }

        internal static Double GetSegmentLengthString(String from, String to)
        {
            if (from != to)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        internal static Double GetSegmentLengthVector(System.Windows.Vector from, System.Windows.Vector to)
        {
            return Math.Abs((to - from).Length);
        }

        internal static Double GetSegmentLengthVector3D(Vector3D from, Vector3D to)
        {
            return Math.Abs((to - from).Length);
        }

        #endregion

        #region Scale Methods

        internal static Byte ScaleByte(Byte value, Double factor)
        {
            return (Byte)((Double)value * factor);
        }
 
        internal static Color ScaleColor(Color value, Double factor)
        {
            return value * (Single)factor;
        }

        internal static Decimal ScaleDecimal(Decimal value, Double factor)
        {
            return value * (Decimal)factor;
        }

        internal static Double ScaleDouble(Double value, Double factor)
        {
            return value * factor;
        }

        internal static Int16 ScaleInt16(Int16 value, Double factor)
        {
            return (Int16)((Double)value * factor);
        }

        internal static Int32 ScaleInt32(Int32 value, Double factor)
        {
            return (Int32)((Double)value * factor);
        }

        internal static Int64 ScaleInt64(Int64 value, Double factor)
        {
            return (Int64)((Double)value * factor);
        }

        internal static Point ScalePoint(Point value, Double factor)
        {
            return new Point(
                value.X * factor,
                value.Y * factor);
        }

        internal static Point3D ScalePoint3D(Point3D value, Double factor)
        {
            return new Point3D(
                value.X * factor,
                value.Y * factor,
                value.Z * factor);
        }

        internal static Quaternion ScaleQuaternion(Quaternion value, Double factor)
        {
            return new Quaternion(value.Axis, value.Angle * factor);
        }

        internal static Rect ScaleRect(Rect value, Double factor)
        {
            Rect temp = new Rect();

            temp.Location = new Point(
                value.Location.X * factor,
                value.Location.Y * factor);
            temp.Size = new Size(
                value.Size.Width * factor,
                value.Size.Height * factor);

            return temp;
        }

        internal static Rotation3D ScaleRotation3D(Rotation3D value, Double factor)
        {
            return new QuaternionRotation3D(ScaleQuaternion(value.InternalQuaternion, factor));
        }

        internal static Single ScaleSingle(Single value, Double factor)
        {
            return (Single)((Double)value * factor);
        }

        internal static Size ScaleSize(Size value, Double factor)
        {
            return (Size)((System.Windows.Vector)value * factor);
        }

        internal static System.Windows.Vector ScaleVector(System.Windows.Vector value, Double factor)
        {
            return value * factor;
        }

        internal static Vector3D ScaleVector3D(Vector3D value, Double factor)
        {
            return value * factor;
        }

        #endregion

        #region EnsureValidAnimationValue Methods

        internal static bool IsValidAnimationValueBoolean(Boolean value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueByte(Byte value)
        {
            return true;
        }
 
        internal static bool IsValidAnimationValueChar(Char value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueColor(Color value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueDecimal(Decimal value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueDouble(Double value)
        {
            if (IsInvalidDouble(value))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueInt16(Int16 value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueInt32(Int32 value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueInt64(Int64 value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueMatrix(Matrix value)
        {
            return true;
        }

        internal static bool IsValidAnimationValuePoint(Point value)
        {
            if (IsInvalidDouble(value.X) || IsInvalidDouble(value.Y))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValuePoint3D(Point3D value)
        {
            if (IsInvalidDouble(value.X) || IsInvalidDouble(value.Y) || IsInvalidDouble(value.Z))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueQuaternion(Quaternion value)
        {
            if (   IsInvalidDouble(value.X) || IsInvalidDouble(value.Y)
                || IsInvalidDouble(value.Z) || IsInvalidDouble(value.W))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueRect(Rect value)
        {
            if (   IsInvalidDouble(value.Location.X) || IsInvalidDouble(value.Location.Y)
                || IsInvalidDouble(value.Size.Width) || IsInvalidDouble(value.Size.Height)
                || value.IsEmpty)
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueRotation3D(Rotation3D value)
        {
            return IsValidAnimationValueQuaternion(value.InternalQuaternion);
        }

        internal static bool IsValidAnimationValueSingle(Single value)
        {
            if (IsInvalidDouble(value))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueSize(Size value)
        {
            if (IsInvalidDouble(value.Width) || IsInvalidDouble(value.Height))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueString(String value)
        {
            return true;
        }

        internal static bool IsValidAnimationValueVector(System.Windows.Vector value)
        {
            if (IsInvalidDouble(value.X) || IsInvalidDouble(value.Y))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueVector3D(Vector3D value)
        {
            if (IsInvalidDouble(value.X) || IsInvalidDouble(value.Y) || IsInvalidDouble(value.Z))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region GetZeroValueMethods

        internal static Byte GetZeroValueByte(Byte baseValue)
        {
            return 0;
        }

        internal static Color GetZeroValueColor(Color baseValue)
        {
            return Color.FromScRgb(0.0F, 0.0F, 0.0F, 0.0F);
        }

        internal static Decimal GetZeroValueDecimal(Decimal baseValue)
        {
            return Decimal.Zero;
        }

        internal static Double GetZeroValueDouble(Double baseValue)
        {
            return 0.0;
        }

        internal static Int16 GetZeroValueInt16(Int16 baseValue)
        {
            return 0;
        }

        internal static Int32 GetZeroValueInt32(Int32 baseValue)
        {
            return 0;
        }

        internal static Int64 GetZeroValueInt64(Int64 baseValue)
        {
            return 0;
        }

        internal static Point GetZeroValuePoint(Point baseValue)
        {
            return new Point();
        }

        internal static Point3D GetZeroValuePoint3D(Point3D baseValue)
        {
            return new Point3D();
        }

        internal static Quaternion GetZeroValueQuaternion(Quaternion baseValue)
        {
            return Quaternion.Identity;
        }

        internal static Single GetZeroValueSingle(Single baseValue)
        {
            return 0.0F;
        }

        internal static Size GetZeroValueSize(Size baseValue)
        {
            return new Size();
        }

        internal static System.Windows.Vector GetZeroValueVector(System.Windows.Vector baseValue)
        {
            return new System.Windows.Vector();
        }

        internal static Vector3D GetZeroValueVector3D(Vector3D baseValue)
        {
            return new Vector3D();
        }

        internal static Rect GetZeroValueRect(Rect baseValue)
        {
            return new Rect(new Point(), new System.Windows.Vector());
        }

        internal static Rotation3D GetZeroValueRotation3D(Rotation3D baseValue)
        {
            return Rotation3D.Identity;
        }

        #endregion

        #region Helpers

        private static Boolean IsInvalidDouble(Double value)
        {
            return Double.IsInfinity(value)
                || DoubleUtil.IsNaN(value);
        }

        #endregion
    }
}
