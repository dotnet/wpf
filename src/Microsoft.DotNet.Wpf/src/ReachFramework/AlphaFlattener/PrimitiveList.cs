// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Diagnostics;
using System.Collections;              // for ArrayList
using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageData. PresentationCore.dll
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Internal.AlphaFlattener
{
    internal class DisplayList 
    {
        public bool   m_DisJoint;
        public double m_width;
        public double m_height;

        public DisplayList(bool disJoint, double width, double height)
        {
            m_DisJoint       = disJoint;
            m_width          = width;
            m_height         = height;
        }

        #region Public Methods

#if DEBUG
        static int overlapcount;

        internal static string LeftPad(object obj, int len)
        {
            string s;
        
            List<int> l = obj as List<int>;

            if (l != null)
            {
                s = "<";

                foreach (int i in l)
                {
                    if (s.Length > 1)
                    {
                        s = s + ' ';
                    }
                    
                    s = s + i;
                }

                return s + ">";
            }
            else if (obj is Double)
            {
                s = ((Double)obj).ToString("F1", CultureInfo.InvariantCulture);
            }
            else if (obj is Rect)
            {
                Rect r = (Rect) obj;

                return " [" + LeftPad(r.Left,   6) + ' '
                            + LeftPad(r.Top,    6) + ' '
                            + LeftPad(r.Width,  6) + ' '
                            + LeftPad(r.Height, 6) + "]";
            }
            else
            {
                s = obj.ToString();
            }

            return s.PadLeft(len, ' ');
        }

        static internal void PrintPrimitive(PrimitiveInfo info, int index, bool verbose)
        {
            if (index < 0)
            {
                Console.WriteLine();

                Console.WriteLine(" No      Type           Und Ovr TrO           Bounding Box                   Clipping");
        
                if (verbose)
                {
                    Console.Write("                     Transform");
                }

                Console.WriteLine();
                return;
            }
            
            Primitive p = info.primitive;

            string typ = p.GetType().ToString();

            typ = typ.Substring(typ.LastIndexOf('.') + 1);

            Console.Write(LeftPad(index, 4) + LeftPad(typ, 18) + ":");
            
            List<int> extra = null;

            if (p.IsOpaque)
            {
                Console.Write(' ');
            }
            else
            {
                Console.Write('@');
            }

            if (info.underlay == null)
            {
                Console.Write("   ");
            }
            else
            {
                extra = info.underlay;

                Console.Write(LeftPad(info.underlay.Count, 3));
            }

            if (info.overlap != null)
            {
                Console.Write(' ' + LeftPad(info.overlap.Count, 3));

                if (info.overlapHasTransparency != 0)
                {
                    Console.Write('$');
                    Console.Write(LeftPad(info.overlapHasTransparency, 3));
                }
                else
                {
                    Console.Write("    ");
                }

                Console.Write(' ');
            }
            else
            {
                Console.Write("         ");
            }
            
            Console.Write(LeftPad(info.bounds, 0));
            
            Geometry clip = p.Clip;

            if (clip != null)
            {
                Console.Write(LeftPad(clip.Bounds, 0));
            }
            
            if (verbose)
            {
                Matrix m = p.Transform;

                Console.Write(" {");
                Console.Write(LeftPad(m.M11, 3) + ' ');
                Console.Write(LeftPad(m.M12, 3) + ' ');
                Console.Write(LeftPad(m.M21, 3) + ' ');
                Console.Write(LeftPad(m.M22, 3) + ' ');
                Console.Write(LeftPad(m.OffsetX, 6) + ' ');
                Console.Write(LeftPad(m.OffsetY, 6));
                Console.Write("} ");
            }
            
            if (verbose)
            {
                GlyphPrimitive gp = p as GlyphPrimitive;

                if (gp != null)
                {
                    IList<char> chars = gp.GlyphRun.Characters;
                                
                    Console.Write(" \"");

                    for (int i = 0; i < chars.Count; i ++)
                    {
                        Console.Write(chars[i]);
                    }
                    
                    Console.Write('"');
                }
            }

            if (verbose)
            {
                GeometryPrimitive gp = p as GeometryPrimitive;

                if ((gp != null) && (gp.Brush != null) && (gp.Pen == null))
                {
                    Brush b = gp.Brush.Brush;

                    if (b != null)
                    {
                        SolidColorBrush sb = b as SolidColorBrush;

                        if (sb != null)
                        {
                            Console.Write(" SolidColorBrush({0})", sb.Color);
                        }
                    }
                }
            }

            if (extra != null)
            {
                Console.Write(' ');
                Console.Write(LeftPad(extra, 0));
            }   

            Console.WriteLine();
        }
#endif

        /// <summary>
        /// Check if a primitive only contains white color
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        internal static bool IsWhitePrimitive(Primitive p)
        {
            GeometryPrimitive gp = p as GeometryPrimitive;

            if (gp != null)
            {
                if ((gp.Brush != null) && (gp.Pen == null))
                {
                    return gp.Brush.IsWhite();
                }

                if ((gp.Pen != null) && (gp.Brush == null))
                {
                    return gp.Pen.StrokeBrush.IsWhite();
                }
            }

            return false;
        }

        /// <summary>
        /// Add a primitve to display list
        /// </summary>
        /// <param name="p"></param>
        public void RecordPrimitive(Primitive p)
        {
            if (null == _commands)
            {
                _commands = new List<PrimitiveInfo>();
            }

            int index = _commands.Count;

            PrimitiveInfo info = new PrimitiveInfo(p);

            // Skip empty and totally clipped primitive

            bool skip = !Utility.IsRenderVisible(info.bounds) || Utility.Disjoint(p.Clip, info.bounds);

            if (!skip)
            {
                if (!m_DisJoint && (index == 0))
                {
                    // Skip white objects in the front
                    skip = IsWhitePrimitive(p);
                }

                if (!skip)
                {
                    _commands.Add(info);
                }
            }

            if ((p.Clip != null) && Utility.IsRectangle(p.Clip))
            {
                Rect bounds = p.Clip.Bounds;

                if ((bounds.Left <= 0) && (bounds.Top <= 0) &&
                    (bounds.Right >= m_width) && (bounds.Bottom >= m_height))
                {
                    // Remove page full clipping
                    p.Clip = null;
                }
                else if ((bounds.Left <= info.bounds.Left) &&
                         (bounds.Top  <= info.bounds.Top) &&
                         (bounds.Right >= info.bounds.Right) &&
                         (bounds.Bottom >= info.bounds.Bottom))
                {
                    // Remove clipping larger than primitive
                    p.Clip = null;
                }
            }
#if DEBUG
            if (skip && (Configuration.Verbose >= 2))
            {
                Console.Write("skip ");

                PrintPrimitive(info, index, true);
            }
#endif
        }

        public Rect this[int index]
        {
            get
            {
                return (_commands[index]).bounds;
            }
        }

        static bool OrderedInsert(List<int> list, int n)
        {
            int pos = list.Count;

            // Sort in increasing order, remove duplication
            for (int i = 0; i < pos; i++)
            {
                if (list[i] == n)
                {
                    return false;
                }

                if (list[i] > n)
                {
                    pos = i;
                    break;
                }
            }

            list.Insert(pos, n);

            return true;
        }

        public void ReportOverlapping(int one, int two)
        {
            if (one > two)
            {
                int t = one; one = two; two = t;
            }

            PrimitiveInfo pi = _commands[one];

            if (pi.overlap == null)
            {
                pi.overlap = new List<int>();
            }

            if (OrderedInsert(pi.overlap, two))
            {
                PrimitiveInfo qi = _commands[two];

                if (!qi.primitive.IsTransparent && !qi.primitive.IsOpaque)
                {
                    pi.overlapHasTransparency ++;
                }

                // if (! qi.primitive.IsOpaque) // Remember primitves covered by a primitive having transparency
                {
                    if (qi.underlay == null)
                    {
                        qi.underlay = new List<int>();
                    }

                    OrderedInsert(qi.underlay, one);
                }
            }
#if DEBUG
            if (Configuration.Verbose >= 2)
            {
                Console.Write(" <{0} {1}>", one, two);

                overlapcount ++;

                if (overlapcount >= 10)
                {
                    Console.WriteLine();
                    overlapcount = 0;
                }
            }
#endif
        }

#if UNIT_TEST
        internal void Add(double x0, double x1, double y0, double y1)
        {
            if (_commands == null)
            {
                _commands = new ArrayList();
            }

            _commands.Add(new PrimitiveInfo(new Rect(x0, y0, x1 - x0, y1 - y0)));
        }
#endif

        /// <summary>
        /// Find all primitive bounding box intersections
        /// </summary>
        /// <param name="count">Number of primitives need to be considered. The rest of primitives can be ignored as they are all opaque</param>
        public void CalculateIntersections(int count)
        {
            RectangleIntersection ri = new RectangleIntersection();

            ri.CalculateIntersections(this, count);
        }

        #endregion

        #region Public Properties
        public List<PrimitiveInfo> Commands
        {
            get
            {
                return _commands;
            }
        }
        #endregion

        #region Protected Attributes
        protected List<PrimitiveInfo>    _commands; //    = null;
        #endregion

    }
} // end of namespace
