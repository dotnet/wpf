// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Windows;
using System.Drawing;
using Microsoft.Test.Display;

namespace Microsoft.Test.Input
{
    /// <summary>
    ///
    /// </summary>
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name="FullTrust")]
    public class UserInput
    {

        #region MouseInput

        /// <summary>
        /// Does a left mouse down and up at the center the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        public static void MouseLeftClickCenter(FrameworkElement elem)
        {
            MouseLeftDownCenter(elem);
            MouseLeftUpCenter(elem);
        }

        /// <summary>
        /// Does a left mouse down at the center the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        public static void MouseLeftDownCenter(FrameworkElement elem)
        {
            MouseButtonCenter(elem, "LeftDown");
        }

        /// <summary>
        /// Does a left mouse up at the center the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        public static void MouseLeftUpCenter(FrameworkElement elem)
        {
            MouseButtonCenter(elem, "LeftUp");
        }
        /// <summary>
        /// Does a Right mouse down and up at the center the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        public static void MouseRightClickCenter(FrameworkElement elem)
        {
            MouseRightDownCenter(elem);
            MouseRightUpCenter(elem);
        }

        /// <summary>
        /// Does a Right mouse down at the center the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        public static void MouseRightDownCenter(FrameworkElement elem)
        {
            MouseButtonCenter(elem, "RightDown");
        }

        /// <summary>
        /// Does a Right mouse up at the center the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        public static void MouseRightUpCenter(FrameworkElement elem)
        {
            MouseButtonCenter(elem, "RightUp");
        }

        /// <summary>
        /// Does a Middle mouse down and up at the center of the FrameworkElement.
        /// </summary>
        /// <param name="frameworkElement"></param>
        public static void MouseMiddleClickCenter(FrameworkElement frameworkElement)
        {
            MouseMiddleDownCenter(frameworkElement);
            MouseMiddleUpCenter(frameworkElement);
        }

        /// <summary>
        /// Does a Middle mouse down at the center of the FrameworkElement.
        /// </summary>
        /// <param name="frameworkElement"></param>
        public static void MouseMiddleDownCenter(FrameworkElement frameworkElement)
        {
            MouseButtonCenter(frameworkElement, "MiddleDown");
        }

        /// <summary>
        /// Does a Middle mouse up at the center of the FrameworkElement.
        /// </summary>
        /// <param name="frameworkElement"></param>
        public static void MouseMiddleUpCenter(FrameworkElement frameworkElement)
        {
            MouseButtonCenter(frameworkElement, "MiddleUp");
        }

        /// <summary>
        /// Does a mouse down at the center of the FrameworkElement with the specified button.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        /// <param name="strButton">The FrameworkElement value of the mouse button to use.</param>
        public static void MouseButtonCenter(FrameworkElement elem, string strButton)
        {
            UIElement uie = elem as UIElement;

	    System.Drawing.Rectangle rc = Microsoft.Test.RenderingVerification.ImageUtility.GetScreenBoundingRectangle(uie );

            SendMouseInputFlags pointerInputFlags = (SendMouseInputFlags)Enum.Parse(typeof(SendMouseInputFlags), strButton);

            Input.SendMouseInput((rc.Left + rc.Right) / 2, (rc.Top + rc.Bottom) / 2, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute );

            Input.SendMouseInput((rc.Left + rc.Right) / 2, (rc.Top + rc.Bottom) / 2, 0, pointerInputFlags | SendMouseInputFlags.Absolute );
        }

        /// <summary>
        /// Does a left mouse down at 4 pixels left and 4 pixels down from the
        /// upper left corner of the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement</param>
        public static void MouseLeftDown(FrameworkElement elem)
        {
            MouseLeftDown(elem, 4, 4);
        }

        /// <summary>
        /// Does a left mouse down at x,y on the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement that x,y values are offset from.</param>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position.</param>
        public static void MouseLeftDown(FrameworkElement elem, int x, int y)
        {
            MouseButton(elem, x, y, "LeftDown");
        }

        /// <summary>
        /// Does a left mouse up at 4 pixels left and 4 pixels down from the
        /// upper left corner of the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement.</param>
        public static void MouseLeftUp(FrameworkElement elem)
        {
            MouseLeftUp(elem, 4, 4);
        }

        /// <summary>
        /// Does a left mouse up at x,y on the FrameworkElement.
        /// </summary>
        /// <param name="elem">The FrameworkElement that x,y values are offset from.</param>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position.</param>
        public static void MouseLeftUp(FrameworkElement elem, int x, int y)
        {
            MouseButton(elem, x, y, "LeftUp");
        }

        /// <summary>
        /// Does a mouse down at x,y on the FrameworkElement with the specified button.
        /// </summary>
        /// <param name="elem">The FrameworkElement that x,y values are offset from.</param>
        /// <param name="x">X position in logical pixels.</param>
        /// <param name="y">Y position in logical pixels.</param>
        /// <param name="strButton">The FrameworkElement value of the mouse button to use.</param>
        public static void MouseButton(FrameworkElement elem, int x, int y, string strButton)
        {
            UIElement uie = elem as UIElement;

            System.Drawing.Rectangle rc = Microsoft.Test.RenderingVerification.ImageUtility.GetScreenBoundingRectangle(uie );


            SendMouseInputFlags pointerInputFlags = (SendMouseInputFlags)Enum.Parse(typeof(SendMouseInputFlags), strButton);

            // Convert logical pixel offset to screen pixels
            x = (int)Monitor.ConvertLogicalToScreen(Dimension.Width, x);
            y = (int)Monitor.ConvertLogicalToScreen(Dimension.Height, y);

            Input.SendMouseInput(rc.Left + x, rc.Top + y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute );

            Input.SendMouseInput(rc.Left + x, rc.Top + y, 0, pointerInputFlags | SendMouseInputFlags.Absolute );
        }

        /// <summary>
        /// Moves the mouse.
        /// </summary>
        /// <param name="elem">The FrameworkElement that x,y values are offset from.</param>
        /// <param name="x">X position in logical pixels.</param>
        /// <param name="y">Y position in logical pixels.</param>
        public static void MouseMove(FrameworkElement elem, int x, int y)
        {
            UIElement uie = elem as UIElement;
            System.Drawing.Rectangle rc = Microsoft.Test.RenderingVerification.ImageUtility.GetScreenBoundingRectangle(uie );

            // Convert logical pixel offset to screen pixels
            x = (int)Monitor.ConvertLogicalToScreen(Dimension.Width, x);
            y = (int)Monitor.ConvertLogicalToScreen(Dimension.Height, y);

            Input.SendMouseInput(rc.Left + x, rc.Top + y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute );
        }

        /// <summary>
        /// moves the mouse the the position on the screen
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        public static void MouseMove(int x, int y)
        {
            Input.MoveTo(new System.Windows.Point((double)x, (double)y));
        }

        /// <summary>
        /// Scrolls the mouse wheel.
        /// </summary>
        /// <param name="elem">The FrameworkElement that x,y values are offset from.</param>
        /// <param name="x">X position in logical pixels.</param>
        /// <param name="y">Y position in logical pixels.</param>
        /// <param name="wheel">wheel scroll distance.</param>
        public static void MouseWheel(FrameworkElement elem, int x, int y, int wheel)
        {
    	    UIElement uie = elem as UIElement;
	        System.Drawing.Rectangle rc = Microsoft.Test.RenderingVerification.ImageUtility.GetScreenBoundingRectangle(elem);

            // Convert logical pixel offset to screen pixels
            x = (int)Monitor.ConvertLogicalToScreen(Dimension.Width, x);
            y = (int)Monitor.ConvertLogicalToScreen(Dimension.Height, y);
        
            Input.SendMouseInput(rc.Left + x, rc.Top + y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
            Input.SendMouseInput(rc.Left + x, rc.Top + y, wheel, SendMouseInputFlags.Wheel | SendMouseInputFlags.Absolute );
        }

        #endregion MouseInput

        #region KeyboardInput

        /// <summary>
        /// Injects a key down.
        /// </summary>
        /// <param name="strKey">The string value of the key to inject.</param>
        public static void KeyDown(string strKey)
        {
            KeyPress(strKey,true);
        }

        /// <summary>
        /// Injects a key up.
        /// </summary>
        /// <param name="strKey">The string value of the key to inject.</param>
        public static void KeyUp(string strKey)
        {
            KeyPress(strKey,false);
        }

        /// <summary>
        /// Injects a key down and up.
        /// </summary>
        /// <param name="strKey">The string value of the key to inject.</param>
        public static void KeyPress(string strKey)
        {
            System.Windows.Input.Key key = GetKeyFromString(strKey);
            if (key != System.Windows.Input.Key.None)
            {
                KeyPress(key, true);
                KeyPress(key, false);
            }
            else
            {
                Console.WriteLine("Invalid strKey - " + strKey);
            }

        }

        /// <summary>
        /// Injects a key.
        /// </summary>
        /// <param name="strKey">The string value of the key to inject.</param>
        /// <param name="boolPress">Specifies whether the button is pressed or not.</param>
        public static void KeyPress(string strKey, bool boolPress)
        {
            System.Windows.Input.Key key = GetKeyFromString(strKey);

            if (key != System.Windows.Input.Key.None)
            {
                KeyPress(key, boolPress);
            }
            else
            {
                Console.WriteLine("Invalid strKey - " + strKey);
            }

        }

        /// <summary>
        /// Injects a key.
        /// </summary>
        /// <param name="vKey">The byte value of the vKey to inject. (See System.Windows.Automation.VKeys)</param>
        /// <param name="boolPress">Specifies whether the button is pressed or not.</param>
        public static void KeyPress(System.Windows.Input.Key vKey, bool boolPress)
        {
            Input.SendKeyboardInput(vKey, boolPress);
        }


        #endregion KeyboardInput

        #region KeyboardInputSupportCode

        /// <summary>
        /// Converts a string value to a byte value for VKeys using reflection.
        /// </summary>
        /// <param name="strKey">The string value of the VKeys constant to return. (See System.Windows.Automation.VKeys)</param>
        private static System.Windows.Input.Key GetKeyFromString(string strKey)
        {
            System.Windows.Input.Key key;

            try
            {
                FieldInfo myFieldInfo;
                Type myType = typeof(System.Windows.Input.Key);

                myFieldInfo = myType.GetField(strKey);

                key = (System.Windows.Input.Key)myFieldInfo.GetValue(myFieldInfo);
            }
            catch
            {
                throw new Exception("AutomationUserInputClass - Invalid strKey='" + strKey + "'.  Should be string of a System.Windows.Input.Key value.");
            }

            return key;
        }

        #endregion KeyboardInputSupportCode

    }  
}
