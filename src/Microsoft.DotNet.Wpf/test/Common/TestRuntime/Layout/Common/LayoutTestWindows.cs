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
using System.IO;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Threading; 
using System.Windows.Threading;

namespace Microsoft.Test.Layout
{
    /// <summary>
    /// Class for creating a generic Window for Test Applications.
    /// </summary>
	public class TestWin
	{
        /// <summary>
        /// Creates Window with specified content.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>Window</returns>
		public static Window Launch(UIElement element)
		{
			Window testWin = Init(); //typeof(Window) with default values of Height, Width, Left, and Top.

			testWin.Content = element;
			return testWin;
		}

        /// <summary>
        /// Creates a window with content specified as a xaml file.
        /// </summary>
        /// <param name="xamlFile"></param>
        /// <returns>Window</returns>
		public static Window Launch(string xamlFile)
		{
			Window testWin = Init(); //typeof(Window) with default values of Height, Width, Left, and Top.
			FileStream f = new FileStream(xamlFile, FileMode.Open, FileAccess.Read);

			testWin.Content = (FrameworkElement)XamlReader.Load(f);
			f.Close();
			return testWin;
		}

        /// <summary>
        /// Creates a very specific Window.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="element"></param>
        /// <param name="show"></param>
        /// <returns>Window</returns>
		public static Window Launch(Type type, double height, double width, double left, double top, UIElement element, bool show)
		{
			Window testWin = Init(type, height, width, left, top, show, null);

			testWin.Content = element;
			return testWin;
		}

        /// <summary>
        /// Creates very specific window with content specified as a xaml file.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="xamlFile"></param>
        /// <param name="show"></param>
        /// <returns>Window</returns>
		public static Window Launch(Type type, double height, double width, double left, double top, string xamlFile, bool show)
		{
			Window testWin = Init(type, height, width, left, top, show, null);

			if (type == typeof(NavigationWindow))
				((NavigationWindow)testWin).Navigate(new Uri(System.IO.Path.Combine(System.Environment.CurrentDirectory, xamlFile)));
			else
			{
				FileStream f = new FileStream(xamlFile, FileMode.Open, FileAccess.Read);

				testWin.Content = (FrameworkElement)XamlReader.Load(f);
				f.Close();
			}

			return testWin;
		}

        /// <summary></summary>
        /// <param name="type"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="element"></param>
        /// <param name="show"></param>
        /// <param name="title"></param>
        /// <returns></returns>
		public static Window Launch(Type type, double height, double width, double left, double top, UIElement element, bool show, string title)
		{
			Window testWin = Init(type, height, width, left, top, show, title);

			testWin.Content = element;
			return testWin;
		}

        /// <summary></summary>
        /// <param name="type"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="xamlFile"></param>
        /// <param name="show"></param>
        /// <param name="title"></param>
        /// <returns></returns>
		public static Window Launch(Type type, double height, double width, double left, double top, string xamlFile, bool show, string title)
		{
			Window testWin = Init(type, height, width, left, top, show, title);

			if (type == typeof(NavigationWindow))
				((NavigationWindow)testWin).Navigate(new Uri(System.IO.Path.Combine(System.Environment.CurrentDirectory, xamlFile)));
			else
			{
				FileStream f = new FileStream(xamlFile, FileMode.Open, FileAccess.Read);

				testWin.Content = (FrameworkElement)XamlReader.Load(f);
				f.Close();
			}

			return testWin;
		}

        /// <summary>Instantiate Window with default values:</summary>
		internal static Window Init()
		{
			Window w = new Window();

			w.Title = "Layout Test Window";
			w.Show();
			return w;
		}

		/// <summary>
        /// Instantiate specified type of Window with specified values. 
        /// </summary>
        internal static Window Init(Type type, double height, double width, double left, double top, bool show, string title)
		{
			Window w;

			if (type == typeof (NavigationWindow))
				w = new NavigationWindow();
			else
				w = new Window();

			w.Height = height;
			w.Width = width;
			w.Left = left;
			w.Top = top;
			w.Title = w.GetType().ToString();

			
			if (title == null)
				w.Title = "Layout Test Window";
			else
				w.Title = title;

			if (show){
				w.Show();
			}

			return w;
		}

		internal struct FrameworkElementResizeData
		{
			public FrameworkElement fe;
			public double newWidth, newHeight;

			public FrameworkElementResizeData(FrameworkElement fe, int newWidth, int newHeight)
			{
				this.fe = fe;
				this.newWidth = newWidth;
				this.newHeight = newHeight;
			}
		}

        /// <summary>
        /// Register an event when the window is resized
        /// </summary>
        public static event EventHandler WindowResized;

        /// <summary></summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        public static void ResizeMainWindow(double height, double width)
		{
			if (Application.Current != null)
			{
				if (Application.Current.MainWindow != null)
					ResizeWindow(Application.Current.MainWindow, height, width);
			}
		}

        /// <summary></summary>
        /// <param name="w"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        public static void ResizeWindow(Window w, double height, double width)
		{
			FrameworkElementResizeData data;
			data.fe = (FrameworkElement)w;
			data.newHeight = height;
			data.newWidth = width;

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background, data.fe.Dispatcher);
            timer.Interval = new TimeSpan(0,0,2);
            timer.Tick += ResizeWindowHandler;
            timer.Tag = data;
            timer.Start();
		}

        /// <summary></summary>
        /// <param name="w"></param>
        /// <param name="size"></param>
        public static void ResizeWindow(Window w, double size)
		{
			ResizeWindow(w, w.ActualHeight + size, w.ActualWidth + size);
		}

        /// <summary></summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ResizeWindowHandler(object sender, EventArgs e)
		{
            DispatcherTimer timer = (DispatcherTimer)sender;
			FrameworkElementResizeData data = (FrameworkElementResizeData)(timer.Tag);

			data.fe.Width = data.newWidth;
			data.fe.Height = data.newHeight;
			data.fe.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(NotifyResize), data.fe);
		}

        /// <summary></summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static object NotifyResize(object o)
		{
			if (WindowResized != null)
			{
				WindowResized(o, EventArgs.Empty);
			}

			return null;
		}
	}
}
