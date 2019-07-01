// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Make a Visual
    /// </summary>
    public class VisualFactory
    {
        /// <summary/>
        public static Visual MakeVisual(string visual)
        {
            switch (visual)
            {
                case "Rectangle":
                    return Rectangle;

                case "Rectangle2":
                    return Rectangle2;

                case "RoundedRectangle":
                    return RoundedRectangle;

                case "Ellipse":
                    return Ellipse;

                case "Text":
                    return Text;

                case "Button":
                    return Button;

                case "CheckBox":
                    return CheckBox;

                case "RadioButton":
                    return RadioButton;

                case "TextBox":
                    return TextBox;

                case "RedCanvas":
                    return RedCanvas;

                default:
                    throw new ApplicationException("Specified visual (" + visual + ") cannot be created");
            }
        }

        private static Visual RedCanvas
        {
            get
            {
                Canvas c = new Canvas();
                c.Background = Brushes.Red;
                return c;
            }
        }

        /// <summary/>
        public static Visual Rectangle
        {
            get
            {
                DrawingVisual v = new DrawingVisual();
                using (DrawingContext ctx = v.RenderOpen())
                {
                    ctx.DrawRectangle(Brushes.Blue, null, new Rect(50, 50, 100, 100));
                }
                return v;
            }
        }

        /// <summary/>
        public static Visual Rectangle2
        {
            get
            {
                DrawingVisual v = new DrawingVisual();
                using (DrawingContext ctx = v.RenderOpen())
                {
                    ctx.DrawRectangle(Brushes.Red, null, new Rect(50, 50, 100, 100));
                }
                return v;
            }
        }

        /// <summary/>
        public static Visual RoundedRectangle
        {
            get
            {
                DrawingVisual v = new DrawingVisual();
                using (DrawingContext ctx = v.RenderOpen())
                {
                    ctx.DrawRoundedRectangle(Brushes.Red, new Pen(Brushes.Green, 4.0), new Rect(50, 50, 100, 100), 34, 23);
                }
                return v;
            }
        }

        /// <summary/>
        public static Visual Ellipse
        {
            get
            {
                DrawingVisual v = new DrawingVisual();
                using (DrawingContext ctx = v.RenderOpen())
                {
                    ctx.DrawEllipse(Brushes.Brown, new Pen(Brushes.DarkMagenta, 2.0), new Point(75, 125), 65, 45);
                }
                return v;
            }
        }

        /// <summary/>
        public static Visual Text
        {
            get
            {
                DrawingVisual v = new DrawingVisual();
                using (DrawingContext ctx = v.RenderOpen())
                {
                    FormattedText text = new FormattedText(
                                                "Draw me\nsome text!",
                                                System.Globalization.CultureInfo.CurrentCulture,
                                                FlowDirection.LeftToRight,
                                                new Typeface("Georgia"),
                                                36,
                                                Brushes.DarkOrange
                                                );
                    ctx.DrawText(text, new Point(10, 35));
                }
                return v;
            }
        }

        /// <summary/>
        public static Visual Button
        {
            get
            {
                Button b = new Button();
                b.Content = "Avalon Button";
                return b;
            }
        }

        /// <summary/>
        public static Visual CheckBox
        {
            get
            {
                CheckBox c = new CheckBox();
                c.IsChecked = true;
                c.Content = "Check me out!";
                return c;
            }
        }

        /// <summary/>
        public static Visual RadioButton
        {
            get
            {
                RadioButton r = new RadioButton();
                r.IsChecked = true;
                r.Content = "Choo, choo, choose me";
                return r;
            }
        }

        /// <summary/>
        public static Visual TextBox
        {
            get
            {
                TextBox t = new TextBox();
                t.Text = "Enter your password";
                return t;
            }
        }
    }
}
