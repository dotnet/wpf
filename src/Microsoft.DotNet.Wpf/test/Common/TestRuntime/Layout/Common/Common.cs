// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*This code is used by all the layout Testers.Any changes to this file can cause build breaks.Please follow the follwoing
guidelines.
Adding New functionality:-Adding new functionality in which the old code existing in the library doesn't get affected at all has to go through the following steps.
1)There should be justification for adding new functionality.
2)Make local changes and don't do sd submit.
3)Create a BBPack and send it to others for review and buddy build.Involve 2 other testers for code review.
4)Ensure that it builds fine without any troubles on other's machine.
5)do sd submit.Don't forget to submit the updated *.asmmeta file.

Modifying the Existing Functionality:-It is very critical.
Please don't modify any library functionality at all.If this is needed ,please contact rahulsh for making any changes.
*/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Test.Logging;
using Microsoft.Test.Discovery;

namespace Microsoft.Test.Layout
{
    /// <summary>
    /// 
    /// </summary>
    public class Common
    {
        /// <summary>
        /// Finds a Name for a Test object.
        /// It will check for attributes and defaults to Class name.
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static string ResolveName(object test)
        {           
            object[] attributes = test.GetType().GetCustomAttributes(typeof(TestAttribute), false);
            string value = string.Empty;

            foreach (object o in attributes)
            {
                if (o.GetType().Name == "TestAttribute")
                {
                    if (((TestAttribute)o).Name != null)
                    {
                        if (DriverState.TestName.Contains(((TestAttribute)o).Name))
                        {
                            value = ((TestAttribute)o).Name;
                        }
                    }
                }
            }

            //try object name
            if (value == string.Empty)
                value = test.GetType().Name;

            return value;
        }

        /// <summary>
        /// Finds VScan Master Path in TestAttribute Variables.
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static string ResolvePath(object test)
        {            
            object[] attributes = test.GetType().GetCustomAttributes(typeof(TestAttribute), false);

            string value = string.Empty;

            foreach (object o in attributes)
            {
                if (o.GetType().Name == "TestAttribute")
                {
                    if (((TestAttribute)o).Name != null)
                    {
                        if (DriverState.TestName.Contains(((TestAttribute)o).Name))        
                        {
                            //Variables Property is obsolete
                            #pragma warning disable 0618
                            if (((TestAttribute)o).Variables != null)
                            {
                                char[] firstSplitter = new char[] { '/' };
                                string[] variables = ((TestAttribute)o).Variables.Split(firstSplitter);
                                if (variables != null && variables.Length > 0)
                                {
                                    foreach (string variable in variables)
                                    {
                                        char[] secondSplitter = new char[] { '=' };
                                        string[] values = variable.Split(secondSplitter);
                                        if (values != null && values.Length > 1)
                                        {
                                            if (values[0].ToLower() == "vscanmasterpath")
                                            {
                                                value = values[1];
                                            }
                                        }
                                    }
                                }
                            }
                            #pragma warning restore 0618
                        }
                    }
                }
            }
            GlobalLog.LogStatus("VALUE ::: {0}",value);
            return value;
        }
    }

    /// <summary>
    /// Common code used by FlowLayout and ElementLayout.
    /// </summary>
    public class CommonFunctionality
    {
        /// <summary>
        /// Will flush dispatcher queue.  
        /// </summary>
        public static void FlushDispatcher()
        {
            FlushDispatcher(Dispatcher.CurrentDispatcher);
        }

        /// <summary>
        /// Will flush dispatcher queue.  
        /// </summary>
        /// <param name="ctx"></param>
        public static void FlushDispatcher(Dispatcher ctx)
        {
            FlushDispatcher(ctx, DispatcherPriority.SystemIdle);
        }

        /// <summary>
        /// Will flush dispatcher queue.  
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="priority"></param>
        public static void FlushDispatcher(Dispatcher ctx, DispatcherPriority priority)
        {
            ctx.Invoke(priority, new DispatcherOperationCallback(delegate { return null; }), null);
        }

        /// <summary>
        /// Will flush dispatcher queue.  
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="e"></param>
        /// <param name="delay"></param>
        public static void DelayedBeginInvoke(DispatcherPriority priority, EventHandler e, TimeSpan delay)
        {
            DelayedBeginInvoke(DispatcherPriority.Background, e, null, delay);
        }

        /// <summary>
        /// Will flush dispatcher queue.  
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="e"></param>
        /// <param name="args"></param>
        /// <param name="delay"></param>
        public static void DelayedBeginInvoke(DispatcherPriority priority, EventHandler e, object args, TimeSpan delay)
        {
            DispatcherTimer timer = new DispatcherTimer(priority);
            timer.Interval = delay;
            timer.Tick += delegate
            {
                timer.Stop();
                e(timer, EventArgs.Empty);
            };
            timer.Tag = args;
            timer.Start();
        }

        /// <summary>
        /// Will flush dispatcher queue.  
        /// </summary>
        /// <param name="d"></param>
        /// <param name="delay"></param>
        public static void DelayedBeginInvoke(DispatcherOperationCallback d, TimeSpan delay)
        {
            DelayedBeginInvoke(DispatcherPriority.Background, d, null, delay);
        }

        /// <summary>
        /// Will flush dispatcher queue.  
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="d"></param>
        /// <param name="args"></param>
        /// <param name="delay"></param>
        public static void DelayedBeginInvoke(DispatcherPriority priority, DispatcherOperationCallback d, object args, TimeSpan delay)
        {
            DispatcherTimer timer = new DispatcherTimer(priority);
            timer.Interval = delay;
            timer.Tick += delegate
            {
                timer.Stop();
                d(args);
            };
            timer.Tag = args;
            timer.Start();
        }

        internal static void WaitTillTimeout(TimeSpan timeout)
        {
            DispatcherFrame frame = new DispatcherFrame();

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Interval = timeout;
            timer.Tick += delegate(object sender, EventArgs e)
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();

            // Keep the thread busy processing events until the timeout has expired.
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Create a generic FlowDocumentScrollViewer with no text.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="bgColor"></param>
        /// <returns>FlowDocumentScrollViewer</returns>
        public static FlowDocumentScrollViewer CreateFlowDoc(double height, double width, SolidColorBrush bgColor)
        {
            FlowDocumentScrollViewer tp = new FlowDocumentScrollViewer();
            tp.Document = new FlowDocument();

            if (bgColor != null)
                tp.Document.Background = bgColor;

            tp.Height = height;
            tp.Width = width;

            return tp;
        }

        /// <summary>
        /// Create a generic FlowDocumentScrollViewer with text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bgColor"></param>
        /// <returns>FlowDocumentScrollViewer</returns>
        public static FlowDocumentScrollViewer CreateFlowDoc(string text, SolidColorBrush bgColor)
        {
            FlowDocumentScrollViewer tp = new FlowDocumentScrollViewer();
            tp.Document = new FlowDocument(new Paragraph(new Run(text)));

            if (bgColor != null)
                tp.Document.Background = bgColor;

            return tp;
        }

        /// <summary>
        /// Create  a generic Canvas.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="bgColor"></param>
        /// <returns>Canvas</returns>
        public static Canvas CreateCanvas(double height, double width, SolidColorBrush bgColor)
        {
            Canvas c = new Canvas();

            if (bgColor != null)
                c.Background = bgColor;

            c.Height = height;
            c.Width = width;

            return c;
        }

        /// <summary>
        /// Create a generic DockPanel.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="bgColor"></param>
        /// <returns>DockPanel</returns>
        public static DockPanel CreateDockPanel(double height, double width, SolidColorBrush bgColor)
        {
            DockPanel dp = new DockPanel();

            if (bgColor != null)
                dp.Background = bgColor;

            dp.Height = height;
            dp.Width = width;

            return dp;
        }

        /// <summary>
        /// Create a generic Border.
        /// </summary>
        /// <param name="background"></param>
        /// <param name="borderthick"></param>
        /// <param name="borderbrush"></param>
        /// <returns>Border</returns>
        public static Border CreateBorder(SolidColorBrush background, Thickness borderthick, Brush borderbrush)
        {
            Border bod = new Border();

            if (background != null)
                bod.Background = background;
            bod.BorderThickness = borderthick;
            bod.BorderBrush = borderbrush;
            return (bod);
        }

        /// <summary>
        /// Create a generic Border.
        /// </summary>
        /// <param name="background"></param>
        /// <param name="borderthick"></param>
        /// <param name="borderbrush"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <returns>Border</returns>
        public static Border CreateBorder(SolidColorBrush background, Thickness borderthick, Brush borderbrush, double width, double height)
        {
            Border bod = new Border();

            if (background != null)
                bod.Background = background;
            bod.BorderThickness = borderthick;
            bod.BorderBrush = borderbrush;
            bod.Width = width;
            bod.Height = height;
            return (bod);
        }

        /// <summary>
        /// Create a generic Border.
        /// </summary>
        /// <param name="background"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <returns>Border</returns>
        public static Border CreateBorder(SolidColorBrush background, double height, double width)
        {
            Border b = new Border();

            if (background != null)
                b.Background = background;

            b.Height = height;
            b.Width = width;

            return b;
        }

        /// <summary>
        /// Create a generic Textblock
        /// </summary>
        /// <param name="textContent"></param>
        /// <returns>TextBlock</returns>
        public static TextBlock CreateText(string textContent)
        {
            TextBlock txt = new TextBlock();
            txt.Text = textContent;

            return txt;
        }

        /// <summary>
        /// Create a generic Image.
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns>Image</returns>
        public static Image CreateImage(string imageName)
        {
            Image img = new Image();
            BitmapImage imgdata;

            if (imageName == null)
            {
                imgdata = new BitmapImage(new Uri("yellowbox.JPG", UriKind.RelativeOrAbsolute));
            }
            else
            {
                imgdata = new BitmapImage(new Uri(imageName, UriKind.RelativeOrAbsolute));
            }

            img.Source = imgdata;
            return img;
        }

        /// <summary>
        /// Create a generic Button.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="bgcolor"></param>
        /// <returns>Button</returns>
        public static Button CreateButton(double width, double height, SolidColorBrush bgcolor)
        {
            Button btn = new Button();

            btn.Content = "Button";

            if (bgcolor != null)
                btn.Background = bgcolor;

            btn.Width = width;
            btn.Height = height;

            return (btn);
        }

        /// <summary>
        /// Create a generic Button.
        /// </summary>
        /// <returns>Button</returns>
        public static Button CreateButton()
        {
            Button btn = new Button();
            btn.Content = "Button";
            return (btn);
        }

        /// <summary>
        /// Create a generic CheckBox.
        /// </summary>
        /// <param name="state"></param>
        /// <returns>CheckBox</returns>        
        public static CheckBox CreateCheckBox(bool? state)
        {
            CheckBox chk = new CheckBox();

            chk.IsChecked = state;
            return (chk);
        }

        /// <summary>
        /// Create a generic TextBox.
        /// </summary>
        /// <returns>TextBox</returns>
        public static TextBox CreateTextBox()
        {
            TextBox tb = new TextBox();

            tb.Height = 50;
            tb.Width = 100;
            return tb;
        }

        /// <summary>
        /// Create a generic TextBox.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="txt"></param>
        /// <returns>TextBox</returns>
        public static TextBox CreateTextBox(double width, double height, string txt)
        {
            TextBox tb = new TextBox();

            if (width != 0)
                tb.Width = width;
            if (height != 0)
                tb.Height = height;
            tb.Text = txt;
            return tb;
        }

        /// <summary>
        /// Create a generic ScollViewer.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>ScrollViewer</returns>
        public static ScrollViewer CreateScrollViewer(double height, double width)
        {
            ScrollViewer scroll = new ScrollViewer();

            scroll.Width = width;
            scroll.Height = height;
            return (scroll);
        }

        /// <summary>
        /// Create a generic ListBox.
        /// </summary>
        /// <param name="numItems"></param>
        /// <returns>ListBox</returns>
        public static ListBox CreateListBox(int numItems)
        {
            ListBox lb = new ListBox();

            for (int l = 1; l <= numItems; l++)
            {
                ListBoxItem li = new ListBoxItem();

                li.Content = "ListBox - List Item " + l.ToString();
                if (l == 3)
                    li.IsSelected = true;

                lb.Items.Add(li);
            }

            return lb;
        }

        /// <summary>
        /// Create a generic Menu.
        /// </summary>
        /// <param name="numMenu"></param>
        /// <returns>Menu</returns>
        public static Menu CreateMenu(int numMenu)
        {
            Menu menu = new Menu();

            for (int l = 1; l <= numMenu; l++)
            {
                MenuItem mi = new MenuItem();

                mi.Header = "Menu Item " + l;
                menu.Items.Add(mi);

                MenuItem subMenu = new MenuItem();

                subMenu.Header = "Sub Menu";
                mi.Items.Add(subMenu);
            }

            return menu;
        }

        /// <summary>
        /// Create a generic Ellipse.
        /// </summary>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        /// <param name="color"></param>
        /// <returns>Ellipse</returns>
        public static Ellipse CreateEllipse(double radiusX, double radiusY, SolidColorBrush color)
        {
            Ellipse e = new Ellipse();
            //e.RadiusX = radiusX;
            //e.RadiusY = radiusY;

            if (color == null)
            {
                e.Fill = new SolidColorBrush(Color.FromRgb(50, 50, 0));
            }
            else
            {
                e.Fill = color;
            }

            return e;
        }

        /// <summary>
        /// Create a generic Rectangle.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="color"></param>
        /// <returns>Rectangle</returns>
        public static Rectangle CreateRectangle(double width, double height, SolidColorBrush color)
        {
            Rectangle rec = new Rectangle();

            if (color == null)
            {
                rec.Fill = new SolidColorBrush(Color.FromRgb(50, 50, 0));
            }
            else
            {
                rec.Fill = color;
            }

            rec.Width = width;
            rec.Height = height;
            return rec;
        }

        /// <summary>
        /// Create a generic Line.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns>Line</returns>
        public static Line CreateLine(int x1, int y1, int x2, int y2)
        {
            Line l = new Line();

            l.Stroke = Brushes.Black;
            l.StrokeThickness = 1;
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
            return l;
        }

        /// <summary>
        /// Create a generic Path.
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="endX"></param>
        /// <param name="endY"></param>
        /// <returns>Path</returns>
        public static System.Windows.Shapes.Path CreatePath(int startX, int startY, int endX, int endY)
        {
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();

            path.Stroke = Brushes.Blue;
            path.StrokeThickness = 1;

            PathGeometry myPathGeometry = new PathGeometry();
            PathFigure myPathFigure = new PathFigure();

            myPathFigure.StartPoint = new System.Windows.Point(startX, startY);
            myPathFigure.Segments.Add(new LineSegment(new System.Windows.Point(endX, endY), true));
            myPathGeometry.Figures.Add(myPathFigure);
            path.Data = myPathGeometry;

            return path;
        }
    }
}