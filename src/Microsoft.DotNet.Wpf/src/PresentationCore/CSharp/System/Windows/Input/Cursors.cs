// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text;

namespace System.Windows.Input 
{
    /// <summary>
    ///     Cursors class to support stock cursors
    /// </summary>
    public static class Cursors
    {
        /// <summary>
        ///     A special value indicating that no cursor should be displayed.
        /// </summary>
        public static Cursor None
        {
            get
            {
                return EnsureCursor(CursorType.None);
            }
        }
        
        /// <summary>
        ///     Standard "no" cursor.
        /// </summary>
        public static Cursor No
        {
            get
            {
                return EnsureCursor(CursorType.No);
            }
        }
        
        /// <summary>
        ///     Standard "arrow" cursor.
        /// </summary>
        public static Cursor Arrow
        {
            get
            {
                return EnsureCursor(CursorType.Arrow);
            }
        }

        /// <summary>
        ///     Standard "arrow with hourglass" cursor.
        /// </summary>
        public static Cursor AppStarting
        {
            get
            {
                return EnsureCursor(CursorType.AppStarting);
            }
        }
        
        /// <summary>
        ///     Standard "crosshair arrow" cursor.
        /// </summary>
        public static Cursor Cross
        {
            get
            {
                return EnsureCursor(CursorType.Cross);
            }
        }
        
        /// <summary>
        ///     Standard "help" cursor.
        /// </summary>
        public static Cursor Help
        {
            get
            {
                return EnsureCursor(CursorType.Help);
            }
        }
        
        /// <summary>
        ///     Standard "text I-beam" cursor.
        /// </summary>
        public static Cursor IBeam
        {
            get
            {
                return EnsureCursor(CursorType.IBeam);
            }
        }
        
        /// <summary>
        ///     Standard "four-way arrow" cursor.
        /// </summary>
        public static Cursor SizeAll
        {
            get
            {
                return EnsureCursor(CursorType.SizeAll);
            }
        }

        /// <summary>
        ///     Standard "double arrow pointing NE and SW" cursor.
        /// </summary>
        public static Cursor SizeNESW
        {
            get
            {
                return EnsureCursor(CursorType.SizeNESW);
            }
        }

        /// <summary>
        ///     Standard "double arrow pointing N and S" cursor.
        /// </summary>
        public static Cursor SizeNS
        {
            get
            {
                return EnsureCursor(CursorType.SizeNS);
            }
        }

        /// <summary>
        ///     Standard "double arrow pointing NW and SE" cursor.
        /// </summary>
        public static Cursor SizeNWSE
        {
            get
            {
                return EnsureCursor(CursorType.SizeNWSE);
            }
        }

        /// <summary>
        ///     Standard "double arrow pointing W and E" cursor.
        /// </summary>
        public static Cursor SizeWE
        {
            get
            {
                return EnsureCursor(CursorType.SizeWE);
            }
        }
        
        /// <summary>
        ///     Standard "vertical up arrow" cursor.
        /// </summary>
        public static Cursor UpArrow
        {
            get
            {
                return EnsureCursor(CursorType.UpArrow);
            }
        }
        
        /// <summary>
        ///     Standard "hourglass" cursor.
        /// </summary>
        public static Cursor Wait
        {
            get
            {
                return EnsureCursor(CursorType.Wait);
            }
        }

        /// <summary>
        ///     Standard "hand" cursor.
        /// </summary>
        public static Cursor Hand
        {
            get
            {
                return EnsureCursor(CursorType.Hand);
            }
        }

        /// <summary>
        ///     Standard "pen" cursor.
        /// </summary>
        public static Cursor Pen
        {
            get
            {
                return EnsureCursor(CursorType.Pen);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing N and S" cursor.
        /// </summary>
        public static Cursor ScrollNS
        {
            get
            {
                return EnsureCursor(CursorType.ScrollNS);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing W and E" cursor.
        /// </summary>
        public static Cursor ScrollWE
        {
            get
            {
                return EnsureCursor(CursorType.ScrollWE);
            }
        }

        /// <summary>
        ///     Standard "scroll four-way arrow" cursor.
        /// </summary>
        public static Cursor ScrollAll
        {
            get
            {
                return EnsureCursor(CursorType.ScrollAll);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing N" cursor.
        /// </summary>
        public static Cursor ScrollN
        {
            get
            {
                return EnsureCursor(CursorType.ScrollN);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing S" cursor.
        /// </summary>
        public static Cursor ScrollS
        {
            get
            {
                return EnsureCursor(CursorType.ScrollS);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing W" cursor.
        /// </summary>
        public static Cursor ScrollW
        {
            get
            {
                return EnsureCursor(CursorType.ScrollW);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing E" cursor.
        /// </summary>
        public static Cursor ScrollE
        {
            get
            {
                return EnsureCursor(CursorType.ScrollE);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing N and W" cursor.
        /// </summary>
        public static Cursor ScrollNW
        {
            get
            {
                return EnsureCursor(CursorType.ScrollNW);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing N and E" cursor.
        /// </summary>
        public static Cursor ScrollNE
        {
            get
            {
                return EnsureCursor(CursorType.ScrollNE);
            }
        }

        /// <summary>
        ///     Standard "scroll arrow pointing S and W" cursor.
        /// </summary>
        public static Cursor ScrollSW
        {
            get
            {
                return EnsureCursor(CursorType.ScrollSW);
            }
        }

        /// <summary>
        ///     Standard "scrollSE" cursor.
        /// </summary>
        public static Cursor ScrollSE
        {
            get
            {
                return EnsureCursor(CursorType.ScrollSE);
            }
        }

        /// <summary>
        ///     Standard "arrow with CD" cursor.
        /// </summary>
        public static Cursor ArrowCD
        {
            get
            {
                return EnsureCursor(CursorType.ArrowCD);
            }
        }

        internal static Cursor EnsureCursor(CursorType cursorType)
        {
            if (_stockCursors[(int)cursorType] == null)
            {
                _stockCursors[(int)cursorType] = new Cursor(cursorType);
            }
            return _stockCursors[(int)cursorType];
        }

        private static int  _cursorTypeCount = ((int)CursorType.ArrowCD) + 1 ;

        private static Cursor[] _stockCursors = new Cursor[_cursorTypeCount];  //CursorType.ArrowCD = 27
    }
}

