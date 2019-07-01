// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Stability
{
    #region Namespaces.
    
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;
    using System.Windows.Interop;
    
    #endregion Namespaces.

    /// <summary>Provides input emulation for DRT.</summary>
    internal static class MouseKeyBoardInput
    {
        #region Input emulation support.

        #region Mouse support.

        [DllImport("user32.dll", EntryPoint="SendInput")]
        private static extern uint SendMouseInput(uint nInputs,
            MouseInput [] pInputs, int cbSize);
        [DllImport("user32.dll", EntryPoint="SendInput")]
        private static extern uint SendMouseInput(uint nInputs,
            ref MouseInput pInputs, int cbSize);

        // Some marshalling notes:
        // - arrays are a-ok
        // - DWORD is UInt32
        // - UINT is UInt32
        // - CHAR is char is Char with ANSI decoration
        // - LONG is Int32
        // - WORD is UInt16

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public IntPtr type;

            public int dx;              // 32
            public int dy;              // 32 - 64
            public int mouseData;       // 32 - 96
            public uint dwFlags;        // 32 - 128
            public IntPtr time;         // 32 - 160
            public IntPtr dwExtraInfo;  // 32 - 192
        }
        
        /// <summary>
        /// Sets up a mouse move message, adjusting the coordinates appropriately.
        /// </summary>
        private static void SetupScreenMouseMove(ref MouseInput input, int x, int y)
        {
            input.type = new IntPtr(INPUT_MOUSE);

            // Hard-coded to a point inside the client area. Correct
            // thing to do is map from client area to screen points,
            // but it requires more P/Invoke.
            input.dx = x;
            input.dy = y;
            input.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;

            // Absolute pixels must be specified in a screen of 65,535 by
            // 65,535 regardless of real size. Add half a pixel to account
            // for rounding problems.
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            float ratioX = 65536 / screenWidth;
            float ratioY = 65536 / screenHeight;
            float halfX = 65536 / (screenWidth * 2);
            float halfY = 65536 / (screenHeight * 2);

            input.dx = (int) (input.dx * ratioX + halfX);
            input.dy = (int) (input.dy * ratioY + halfY);
        }

        /// <summary>GetSystemMetrics wrapper.</summary>
        /// <param name="nIndex">Index of metric to get.</param>
        /// <returns>System value.</returns>
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_MOVE        = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP      = 0x0004;
        private const uint MOUSEEVENTF_ABSOLUTE    = 0x8000;

        /// <summary>Width of the screen of the primary display monitor, in pixels.</summary>
        public const int SM_CXSCREEN = 0;
        /// <summary>Height of the screen of the primary display monitor, in pixels.</summary>
        public const int SM_CYSCREEN = 1;

        public static void ClickScreenPoint(int x, int y)
        {
            MouseInput[] input = new MouseInput[3];
            
            SetupScreenMouseMove(ref input[0], x, y);

            input[1].type = new IntPtr(INPUT_MOUSE);
            input[1].dwFlags = MOUSEEVENTF_LEFTDOWN;

            input[2].type = new IntPtr(INPUT_MOUSE);
            input[2].dwFlags = MOUSEEVENTF_LEFTUP;

            unsafe
            {
                SendMouseInput((uint)input.Length, input, sizeof(MouseInput));
            }
        }

        public static void MouseDown()
        {
            MouseInput[] input = new MouseInput[1];
            input[0].type = new IntPtr(INPUT_MOUSE);
            input[0].dwFlags = MOUSEEVENTF_LEFTDOWN;

            unsafe
            {
                SendMouseInput((uint)input.Length, input, sizeof(MouseInput));
            }
        }

        public static void MouseMove(int x, int y)
        {
            MouseInput[] input = new MouseInput[1];
            
            SetupScreenMouseMove(ref input[0], x, y);

            unsafe
            {
                SendMouseInput((uint)input.Length, input, sizeof(MouseInput));
            }
        }

        public static void MouseUp()
        {
            MouseInput[] input = new MouseInput[1];
            input[0].type = new IntPtr(INPUT_MOUSE);
            input[0].dwFlags = MOUSEEVENTF_LEFTUP;
            
            unsafe
            {
                SendMouseInput((uint)input.Length, input, sizeof(MouseInput));
            }
        }

        // NOTE: view the file history for a version that moves the mouse pixel-by-pixel
        
        // TODO: remove this commented code and leave the above comment

        /*

        /// <summary>
        /// This helper is required to generate all input in one run.
        /// </summary>
        internal static void GenerateMouseDrag(int x, int y, int xDest, int yDest,
            System.Threading.ThreadStart method)
        {
            new DragGenerator(x, y, xDest, yDest, method);
        }

        internal class DragGenerator
        {
            internal DragGenerator(int x, int y, int xDest, int yDest, 
                System.Threading.ThreadStart method)
            {
                int xSteps;         // Number of horizontal steps.
                int ySteps;         // Number of vertical steps.
                int stepCount;      // Number of steps to be performed.
                int xDelta;         // Horizontal movement per step.
                int yDelta;         // Vertical movement per step.
                int stepIndex;      // Index of next step.
                MouseInput[] steps; // Mouse input messages.

                xSteps = Math.Abs(xDest - x);
                ySteps = Math.Abs(yDest - y);
                stepCount = 1 + ((xSteps > ySteps)? xSteps : ySteps);

                steps = new MouseInput[stepCount];
                SetupScreenMouseMove(ref steps[0], x, y);

                xDelta = (x > xDest)? -1 : 1;
                yDelta = (y > yDest)? -1 : 1;
                stepIndex = 1;
                while (x != xDest || y != yDest)
                {
                    if (x != xDest) x += xDelta;
                    if (y != yDest) y += yDelta;
                    SetupScreenMouseMove(ref steps[stepIndex], x, y);
                    stepIndex++;
                }
                
                this._steps = steps;
                this._method = method;
                NextStep();
            }
            
            private void NextStep()
            {
                unsafe
                {
                    SendMouseInput(1, ref _steps[_stepIndex], sizeof(MouseInput));
                }
                _stepIndex++;
                if (_stepIndex == _steps.Length)
                {
                    if (_method != null) _method();
                }
                else
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                        new System.Threading.ThreadStart(NextStep));
                }
            }
            
            private System.Threading.ThreadStart _method;
            private int  _stepIndex;
            private MouseInput[] _steps;
        }
        */

        #endregion Mouse support.

        #region Keyboard support.

        [DllImport ("user32.dll", EntryPoint = "SendInput")]
        private static extern uint SendKeyboardInput(uint nInputs,
            KeyboardInput [] pInputs, int cbSize);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        private static extern ushort VkKeyScan(char ch);

        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyboardInput
        {
            public IntPtr type;

            /// <summary>
            /// Specifies a virtual-key code.
            /// </summary>
            public ushort wVk;          // 16
            /// <summary>
            /// Specifies a hardware scan code for the key.
            /// </summary>
            public ushort wScan;        // 16 - 32
            /// <summary>
            /// Specifies various aspects of a keystroke.
            /// </summary>
            public uint dwFlags;        // 32 - 64
            /// <summary>
            /// Time stamp for the event, in milliseconds.
            /// </summary>
            public IntPtr time;           // 32 - 96
            /// <summary>
            /// Specifies an additional value associated with the keystroke.
            /// </summary>
            public IntPtr dwExtraInfo;  // 32 - 128

            public uint pad1;           // 32 - 160
            public uint pad2;           // 32 - 192

            /// <summary>Copy constructor.</summary>
            /// <param name='keyboardInput'>Keyboard input to copy.</param>
            public KeyboardInput(KeyboardInput keyboardInput)
            {
                type = new IntPtr(INPUT_KEYBOARD);
                this.wVk = keyboardInput.wVk;
                this.wScan = keyboardInput.wScan;
                this.dwFlags = keyboardInput.dwFlags;
                this.time = keyboardInput.time;
                this.dwExtraInfo = keyboardInput.dwExtraInfo;
                this.pad1 = keyboardInput.pad1;
                this.pad2 = keyboardInput.pad2;
            }
        }

        /// <summary>
        /// If specified, the scan code was preceded by a prefix
        /// byte that has the value 0xE0 (224).
        /// </summary>
        internal const uint KEYEVENTF_EXTENDEDKEY = 1;
        /// <summary>
        /// If specified, the key is being released. If not specified, the
        /// key is being pressed.
        /// </summary>
        internal const uint KEYEVENTF_KEYUP = 2;
        /// <summary>
        /// Windows 2000/XP: If specified, the system synthesizes a
        /// VK_PACKET keystroke. The wVk parameter must be zero.
        /// </summary>
        internal const uint KEYEVENTF_UNICODE = 4;
        /// <summary>
        /// If specified, wScan identifies the key and wVk is ignored.
        /// </summary>
        internal const uint KEYEVENTF_SCANCODE = 8;
        
        internal const byte VK_SHIFT = 0x10;
        
        internal static void PressShift()
        {
            KeyboardInput[] input = new KeyboardInput[1];
            input[0].type = new IntPtr(INPUT_KEYBOARD);
            input[0].wVk = VK_SHIFT;
            unsafe
            {
                SendKeyboardInput((uint)input.Length, input, sizeof(KeyboardInput));
            }
        }

        internal static void ReleaseShift()
        {
            KeyboardInput[] input = new KeyboardInput[1];
            input[0].type = new IntPtr(INPUT_KEYBOARD);
            input[0].wVk = VK_SHIFT;
            input[0].dwFlags = KEYEVENTF_KEYUP;
            unsafe
            {
                SendKeyboardInput((uint)input.Length, input, sizeof(KeyboardInput));
            }
        }

        /// <summary>Emulates typing on the keyboard.</summary>
        /// <param name='text'>Text to type.</param>
        /// <remarks>
        /// Case is not respected - everything goes in lowercase.
        /// To get uppercase characters, add a "+" in front of the
        /// character. The original design had the "+" toggle the
        /// shift state, but by resetting it we make text string
        /// compatible with CLR's SendKeys.Send.
        /// <para />
        /// Eg, to type "Hello, WORLD!", pass "+hello, +W+O+R+L+D+1"
        /// <para />
        /// This method has not been globalized to keep it simple.
        /// Non-US keyboard may break this functionality.
        /// </remarks>
        public static void KeyboardType(string text)
        {
            ArrayList list = new ArrayList();
            bool shiftIsPressed = false;
            bool controlIsPressed = false;
            const byte VK_CONTROL = 0x11;
            const byte VK_RETURN  = 0x0D;
            const byte VK_END = 0x23;
            const byte VK_HOME = 0x24;
            const byte VK_LEFT = 0x25;
            const byte VK_UP = 0x26;
            const byte VK_RIGHT = 0x27;
            const byte VK_DOWN = 0x28;

            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '+')
                {
                    KeyboardInput input = new KeyboardInput();
                    input.type = new IntPtr(INPUT_KEYBOARD);
                    input.wVk = VK_SHIFT;
                    if (shiftIsPressed)
                        input.dwFlags = KEYEVENTF_KEYUP;
                    list.Add(input);
                    shiftIsPressed = !shiftIsPressed;
                    i++;
                }
                else if (c == '^')
                {
                    KeyboardInput input = new KeyboardInput();
                    input.type = new IntPtr(INPUT_KEYBOARD);
                    input.wVk = VK_CONTROL;
                    if (controlIsPressed)
                        input.dwFlags = KEYEVENTF_KEYUP;
                    list.Add(input);
                    controlIsPressed = !controlIsPressed;
                    i++;
                }
                else if (c == '{')
                {
                    i++;
                    int closeIndex = text.IndexOf('}', i);
                    if (closeIndex == -1)
                    {
                        throw new ArgumentException(
                            "Malformed typing text: no closing '}' to match " +
                            "opening '{' at position " + i + ": " + text);
                    }
                    int length = closeIndex - i;
                    string escapeCode = text.Substring(i, length);
                    KeyboardInput input;
                    switch (escapeCode)
                    {
                        case "ENTER":
                            input = new KeyboardInput();
                            input.type = new IntPtr(INPUT_KEYBOARD);
                            input.wVk = VK_RETURN;
                            list.Add(input);

                            input = new KeyboardInput(input);
                            input.dwFlags |= KEYEVENTF_KEYUP;
                            list.Add(input);
                            break;
                        case "END":
                            input = new KeyboardInput();
                            input.type = new IntPtr(INPUT_KEYBOARD);
                            input.wVk = VK_END;
                            list.Add(input);
                           
                            input = new KeyboardInput(input);
                            input.dwFlags |= KEYEVENTF_KEYUP;
                            list.Add(input);

                            KeyboardInput reset = new KeyboardInput();
                            reset.type = new IntPtr(INPUT_KEYBOARD);
                            reset.wVk = VK_SHIFT;
                            reset.dwFlags = KEYEVENTF_KEYUP;
                            list.Add(reset);
                            shiftIsPressed = false;
                            break;
                        case "HOME":
                            input = new KeyboardInput();
                            input.type = new IntPtr(INPUT_KEYBOARD);
                            input.wVk = VK_HOME;
                            list.Add(input);

                            input = new KeyboardInput(input);
                            input.dwFlags |= KEYEVENTF_KEYUP;
                            list.Add(input);

                            reset = new KeyboardInput();
                            reset.type = new IntPtr(INPUT_KEYBOARD);
                            reset.wVk = VK_SHIFT;
                            reset.dwFlags = KEYEVENTF_KEYUP;
                            list.Add(reset);
                            shiftIsPressed = false;
                            break;
                        case "LEFT":
                            input = new KeyboardInput();
                            input.type = new IntPtr(INPUT_KEYBOARD);
                            input.wVk = VK_LEFT;
                            list.Add(input);
                            
                            input = new KeyboardInput(input);
                            input.dwFlags |= KEYEVENTF_KEYUP;
                            list.Add(input);

                            reset = new KeyboardInput();
                            reset.type = new IntPtr(INPUT_KEYBOARD);
                            reset.wVk = VK_SHIFT;
                            reset.dwFlags = KEYEVENTF_KEYUP;
                            list.Add(reset);
                            shiftIsPressed = false;
                            break;
                        case "UP":
                            input = new KeyboardInput();
                            input.type = new IntPtr(INPUT_KEYBOARD);
                            input.wVk = VK_UP;
                            list.Add(input);
                            
                            input = new KeyboardInput(input);
                            input.dwFlags |= KEYEVENTF_KEYUP;
                            list.Add(input);

                            reset = new KeyboardInput();
                            reset.type = new IntPtr(INPUT_KEYBOARD);
                            reset.wVk = VK_SHIFT;
                            reset.dwFlags = KEYEVENTF_KEYUP;
                            list.Add(reset);
                            shiftIsPressed = false;
                            break;
                        case "RIGHT":
                            input = new KeyboardInput();
                            input.type = new IntPtr(INPUT_KEYBOARD);
                            input.wVk = VK_RIGHT;
                            list.Add(input);
                            
                            input = new KeyboardInput(input);
                            input.dwFlags |= KEYEVENTF_KEYUP;
                            list.Add(input);

                            reset = new KeyboardInput();
                            reset.type = new IntPtr(INPUT_KEYBOARD);
                            reset.wVk = VK_SHIFT;
                            reset.dwFlags = KEYEVENTF_KEYUP;
                            list.Add(reset);
                            shiftIsPressed = false;
                            break;
                        case "DOWN":
                            input = new KeyboardInput();
                            input.type = new IntPtr(INPUT_KEYBOARD);
                            input.wVk = VK_DOWN;
                            list.Add(input);
                            
                            input = new KeyboardInput(input);
                            input.dwFlags |= KEYEVENTF_KEYUP;
                            list.Add(input);

                            reset = new KeyboardInput();
                            reset.type = new IntPtr(INPUT_KEYBOARD);
                            reset.wVk = VK_SHIFT;
                            reset.dwFlags = KEYEVENTF_KEYUP;
                            list.Add(reset);
                            shiftIsPressed = false;
                            break;
                        default:
                            throw new ArgumentException(
                            "Malformed typing text: unknown escape code [" +
                            escapeCode + "]" + i + ": " + text);
                    }
                    i = closeIndex + 1;
                }
                else
                {
                    KeyboardInput input = new KeyboardInput();
                    input.type = new IntPtr(INPUT_KEYBOARD);
                    input.wVk = VkKeyScan(c);
                    list.Add(input);

                    input = new KeyboardInput(input);
                    input.dwFlags |= KEYEVENTF_KEYUP;
                    list.Add(input);

                    // Reset shift.
                    if (shiftIsPressed)
                    {
                        KeyboardInput reset = new KeyboardInput();
                        reset.type = new IntPtr(INPUT_KEYBOARD);
                        reset.wVk = VK_SHIFT;
                        reset.dwFlags = KEYEVENTF_KEYUP;
                        list.Add(reset);
                        shiftIsPressed = false;
                    }
                    // Reset shift.
                    if (controlIsPressed)
                    {
                        KeyboardInput reset = new KeyboardInput();
                        reset.type = new IntPtr(INPUT_KEYBOARD);
                        reset.wVk = VK_CONTROL;
                        reset.dwFlags = KEYEVENTF_KEYUP;
                        list.Add(reset);
                        controlIsPressed= false;
                    }
                    i++;
                }
            }

            KeyboardInput[] inputList = (KeyboardInput[])
                list.ToArray(typeof(KeyboardInput));
            unsafe
            {
                SendKeyboardInput((uint)inputList.Length, inputList, sizeof(KeyboardInput));
            }
        }

        #endregion Keyboard support.

        #endregion Input emulation support.
        
        #region Element positioning helpers.
        
        /// <summary>Defines the x- and y- coordinates of a point.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            /// <summary>Specifies the x-coordinate of the point.</summary>
            public int x;
            /// <summary>Specifies the x-coordinate of the point.</summary>
            public int y;

            /// <summary>Creates a new Test.Uis.Wrappers.Win32.POINT instance.</summary>
            /// <param name="x">Specifies the x-coordinate of the point.</param>
            /// <param name="y">Specifies the y-coordinate of the point.</param>
            public POINT(int x, int y)
            {
                this.x  = x;
                this.y  = y;
            }
        }

        /// <summary>
        /// Gets the rectangle that bounds the specified element, relative
        /// to the client area of the window the element is in.
        /// </summary>
        /// <param name='element'>Element to get rectangle for.</param>
        /// <returns>The System.Windows.Rect that bounds the element.</returns>
        public static Rect GetClientRelativeRect(UIElement element)
        {
            Visual parent;  // Topmost parent of element.
            Matrix m;       // Matrix to transform corodinates.
            Point[] points; // Points around element.
            
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            parent = GetTopMostVisual(element);
            points = GetRenderSizeBoxPoints(element);

            System.Windows.Media.GeneralTransform gt = element.TransformToAncestor(parent);
            System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
            if(t==null)
            {
	            throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
            }
            m = t.Value;
            m.Transform(points);
            
            // Assume there is no rotation.

            return new Rect(points[0], points[3]);
        }

        /// <summary>
        /// Gets an array of four bounding points for the computed
        /// size of the specified element. The top-left corner
        /// is (0;0) and the bottom-right corner is (width;height).
        /// </summary>
        private static Point[] GetRenderSizeBoxPoints(UIElement element)
        {
            // Get the points for the rectangle and transform them.
            double height = element.RenderSize.Height;
            double width = element.RenderSize.Width;
            Point[] points = new Point[4];
            points[0] = new Point(0, 0);
            points[1] = new Point(width, 0);
            points[2] = new Point(0, height);
            points[3] = new Point(width, height);
            return points;
        }

        /// <summary>
        /// Gets the rectangle that bounds the specified element, relative
        /// to the top-left corner of the screen.
        /// </summary>
        /// <param name='element'>Element to get rectangle for.</param>
        /// <returns>The rectangle that bounds the element.</returns>
        internal static Rect GetScreenRelativeRect(UIElement element)
        {
            POINT topLeft;
            Rect clientRect;

            PresentationSource source;

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            source = PresentationSource.FromVisual(element);

            if (source == null)
            {
                throw new InvalidOperationException("element is not connected to visual tree");
            }

            clientRect = GetClientRelativeRect(element);
           
            // Ignore high-DPI adjustment.
            topLeft = new POINT((int)Math.Round(clientRect.Left), (int)Math.Round(clientRect.Top));
            ClientToScreen(((HwndSource)source).Handle, ref topLeft);

            return new Rect(topLeft.x, topLeft.y, clientRect.Width, clientRect.Height);
        }

        /// <summary>
        /// Gets the top-most visual for the specified visual element.
        /// </summary>
        private static Visual GetTopMostVisual(Visual element)
        {           
            PresentationSource source;

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            source = PresentationSource.FromVisual(element);
            
            if (source == null)
            {
                throw new InvalidOperationException("The specified UiElement is not connected to a rendering Visual Tree.");
            }

            return source.RootVisual;
        }

        /// <summary>Converts the client-area coordinates of a specified point to screen coordinates.</summary>
        /// <param name="hwndFrom">Handle to the window whose client area is used for the conversion.</param>
        /// <param name="pt">POINT structure that contains the client coordinates to be converted.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hwndFrom, [In, Out] ref POINT pt);

        #endregion Element positiong helpers.
    }
}
