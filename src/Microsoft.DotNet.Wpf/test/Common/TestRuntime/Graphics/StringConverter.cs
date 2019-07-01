// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics
{
    /// <summary>
    /// "Turn strings into useful stuff"
    /// </summary>
    public class StringConverter
    {
        /// <summary/>
        public static double ToDouble(string s)
        {
            return Convert.ToDouble(s, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary/>
        public static int ToInt(string s)
        {
            return Convert.ToInt32(s, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary/>
        public static bool ToBool(string s)
        {
            switch (s)
            {
                case "true":
                    return true;

                case "false":
                    return false;
            }
            throw new ArgumentException(string.Format("The following cannot be parsed as a boolean: {0}\r\nOnly \"true\" and \"false\" are acceptable (case-sensitive)", s));
        }

        /// <summary/>
        public static byte ToByte(string s)
        {
            return Convert.ToByte(s, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary/>
        public static Point3D ToPoint3D(string s)
        {
            string[] coords = s.Split(new char[] { ',' });
            if (coords.Length != 3)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a point3D: {0}", s));
            }
            return new Point3D(ToDouble(coords[0]), ToDouble(coords[1]), ToDouble(coords[2]));
        }

        /// <summary/>
        public static Point4D ToPoint4D(string s)
        {
            string[] coords = s.Split(new char[] { ',' });
            if (coords.Length != 4)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a point4D: {0}", s));
            }
            return new Point4D(ToDouble(coords[0]), ToDouble(coords[1]), ToDouble(coords[2]), ToDouble(coords[3]));
        }

        /// <summary/>
        public static Point ToPoint(string s)
        {
            string[] coords = s.Split(new char[] { ',' });
            if (coords.Length != 2)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a point: {0}", s));
            }
            return new Point(ToDouble(coords[0]), ToDouble(coords[1]));
        }

        /// <summary/>
        public static Vector ToVector(string s)
        {
            string[] coords = s.Split(new char[] { ',' });
            if (coords.Length != 2)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a vector: {0}", s));
            }
            return new Vector(ToDouble(coords[0]), ToDouble(coords[1]));
        }

        /// <summary/>
        public static Vector3D ToVector3D(string s)
        {
            string[] coords = s.Split(new char[] { ',' });
            if (coords.Length != 3)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a vector: {0}", s));
            }
            return new Vector3D(ToDouble(coords[0]), ToDouble(coords[1]), ToDouble(coords[2]));
        }

        /// <summary/>
        public static Color ToColor(string s)
        {
            string[] channels = s.Split(new char[] { ',' });
            if (channels.Length != 4)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a color: {0}", s));
            }
            return Color.FromArgb(ToByte(channels[0]), ToByte(channels[1]), ToByte(channels[2]), ToByte(channels[3]));
        }

        /// <summary/>
        public static Quaternion ToQuaternion(string s)
        {
            if (s == "Identity")
            {
                return Quaternion.Identity;
            }
            string[] vals = s.Split(new char[] { ',' });
            if (vals.Length != 4)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a quaternion: {0}", s));
            }
            return new Quaternion(ToDouble(vals[0]), ToDouble(vals[1]), ToDouble(vals[2]), ToDouble(vals[3]));
        }

        /// <summary/>
        public static Rect3D ToRect3D(string s)
        {
            if (s == "Empty")
            {
                return Rect3D.Empty;
            }
            string[] vals = s.Split(new char[] { ',' });
            if (vals.Length != 6)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a rect: {0}", s));
            }
            return new Rect3D(ToDouble(vals[0]), ToDouble(vals[1]), ToDouble(vals[2]), ToDouble(vals[3]), ToDouble(vals[4]), ToDouble(vals[5]));
        }

        /// <summary/>
        public static Rect ToRect(string s)
        {
            if (s == "Empty")
            {
                return Rect.Empty;
            }
            string[] vals = s.Split(new char[] { ',' });
            if (vals.Length != 4)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a rect: {0}", s));
            }
            return new Rect(ToDouble(vals[0]), ToDouble(vals[1]), ToDouble(vals[2]), ToDouble(vals[3]));
        }

        /// <summary/>
        public static Size3D ToSize3D(string s)
        {
            if (s == "Empty")
            {
                return Size3D.Empty;
            }
            string[] vals = s.Split(new char[] { ',' });
            if (vals.Length != 3)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a size: {0}", s));
            }
            return new Size3D(ToDouble(vals[0]), ToDouble(vals[1]), ToDouble(vals[2]));
        }

        /// <summary/>
        public static Size ToSize(string s)
        {
            if (s == "Empty")
            {
                return Size.Empty;
            }
            string[] vals = s.Split(new char[] { ',' });
            if (vals.Length != 2)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a size: {0}", s));
            }
            return new Size(ToDouble(vals[0]), ToDouble(vals[1]));
        }

        /// <summary/>
        public static Matrix3D ToMatrix3D(string s)
        {
            if (s == "Identity")
            {
                return Matrix3D.Identity;
            }
            string[] vals = s.Split(new char[] { ',' });
            if (vals.Length != 16)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a matrix: {0}", s));
            }
            return new Matrix3D(ToDouble(vals[0]), ToDouble(vals[1]), ToDouble(vals[2]), ToDouble(vals[3]),
                                 ToDouble(vals[4]), ToDouble(vals[5]), ToDouble(vals[6]), ToDouble(vals[7]),
                                 ToDouble(vals[8]), ToDouble(vals[9]), ToDouble(vals[10]), ToDouble(vals[11]),
                                 ToDouble(vals[12]), ToDouble(vals[13]), ToDouble(vals[14]), ToDouble(vals[15]));
        }

        /// <summary/>
        public static Matrix ToMatrix(string s)
        {
            if (s == "Identity")
            {
                return Matrix.Identity;
            }
            string[] vals = s.Split(new char[] { ',' });
            if (vals.Length != 6)
            {
                throw new ArgumentException(string.Format("The following cannot be parsed as a matrix: {0}", s));
            }
            return new Matrix(ToDouble(vals[0]), ToDouble(vals[1]),
                               ToDouble(vals[2]), ToDouble(vals[3]),
                               ToDouble(vals[4]), ToDouble(vals[5]));
        }
    }
}

