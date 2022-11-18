// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Security;
using System.Windows.Forms;
using System.IO.Packaging;
using System.Windows.TrustUI;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents
{

    /// <summary>
    /// DialogDivider draws a simple divider used in XPS Viewer's WinForms dialogs.
    /// It always assumes 1 pixel of height and the width of its parent (or 0 if null)
    /// regardless of any user settings.
    /// </summary>
    internal class DialogDivider : System.Windows.Forms.Control
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// The constructor
        /// </summary>
        internal DialogDivider()
        {
            TabStop = false;
        }

        #endregion Constructors

        /// <summary>
        /// SetBoundsCore override. We enforce a 1 pixel
        /// height and a width equal to the width of its parent.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="specified"></param>
        protected override void SetBoundsCore( 
            int x, 
            int y, 
            int width, 
            int height, 
            BoundsSpecified specified )
        {            
            if (Parent != null)
            {
                //Force a 1-pixel height, with the width of our immediate parent
                base.SetBoundsCore(Parent.Location.X, y, Parent.Size.Width, 1, specified);
            }
            else
            {
                //No parent, just assume 0 by 0.
                base.SetBoundsCore(x, y, 0, 0, specified);
            }
        }

        /// <summary>
        /// OnPaint override.  We draw a 1-pixel-high line from one end of our client
        /// region to the other, thus drawing the divider.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            //Draw a line from one side of our client bounds to the other.
            e.Graphics.DrawLine(
                new Pen(new SolidBrush(System.Drawing.SystemColors.ControlDark)), 
                ClientRectangle.Left, 
                0, 
                ClientRectangle.Right, 
                0);
        }      
    }
}
