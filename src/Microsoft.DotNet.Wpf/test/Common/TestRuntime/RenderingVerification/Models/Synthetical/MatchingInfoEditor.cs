// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Windows.Forms;
        using System.ComponentModel;
        using System.Drawing.Design;
        using System.Windows.Forms.Design;
    #endregion using

    /// <summary>
    /// Implements a custom type editor for editing a MatchingInfo 
    /// </summary>
    public class MatchingInfoEditor : UITypeEditor
    {
        /// <summary>
        /// Internal class used for storing custom data in listviewitems
        /// </summary>
        internal class clbItem
        {
            private int _value;
            private string _text;
            private string _tooltip;

            /// <summary>
            /// Creates a new instance of the <c>clbItem</c>
            /// </summary>
            /// <param name="text">The string to display in the <c>ToString</c> method. 
            /// It will contains the name of the flag</param>
            /// <param name="itemValue">The integer value of the flag</param>
            /// <param name="tooltip">The tooltip to display in the <see cref="CheckedListBox"/></param>
            public clbItem(string text, int itemValue, string tooltip)
            {
                this._text = text;
                this._value = itemValue;
                this._tooltip = tooltip;
            }
            /// <summary>
            /// Gets the int value for this item
            /// </summary>
            public int Value
            {
                get{return _value;}
            }
            /// <summary>
            /// Gets the tooltip for this item
            /// </summary>
            public string Tooltip
            {
                get{return _tooltip;}
            }
            /// <summary>
            /// Gets the name of this item
            /// </summary>
            /// <returns>The name passed in the constructor</returns>
            public override string ToString()
            {
                return _text;
            }
        }

        private IWindowsFormsEditorService edSvc = null;
        private CheckedListBox clb;
        private ToolTip tooltipControl;

        /// <summary>
        /// Overrides the method used to provide basic behaviour for selecting editor.
        /// Shows our custom control for editing the value.
        /// </summary>
        /// <param name="context">The context of the editing control</param>
        /// <param name="provider">A valid service provider</param>
        /// <param name="value">The current value of the object to edit</param>
        /// <returns>The new value of the object</returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) 
        {
            if (context != null
                && context.Instance != null
                && provider != null) 
            {

                edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (edSvc != null) 
                {                    
                    // Create a CheckedListBox and populate it with all the enum values
                    clb = new CheckedListBox();
                    clb.BorderStyle = BorderStyle.FixedSingle;
                    clb.CheckOnClick = true;
                    clb.MouseDown += new MouseEventHandler(this.OnMouseDown);
                    clb.MouseMove += new MouseEventHandler(this.OnMouseMoved);

                    tooltipControl = new ToolTip();
                    tooltipControl.ShowAlways = true;

                    PropertyGrid pgrid = new PropertyGrid();
                    pgrid.SelectedObject = value;

                    // Show our CheckedListbox as a DropDownControl. 
                    // This methods returns only when the dropdowncontrol is closed
                    edSvc.DropDownControl(pgrid);

                
                    // return the right enum value corresponding to the result
                    return pgrid.SelectedObject;
                }
            }

            return value;
        }

        /// <summary>
        /// Shows a dropdown icon in the property editor
        /// </summary>
        /// <param name="context">The context of the editing control</param>
        /// <returns>Returns <c>UITypeEditorEditStyle.DropDown</c></returns>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
        {
            return UITypeEditorEditStyle.Modal;            
        }

        private bool handleLostfocus = false;

        /// <summary>
        /// When got the focus, handle the lost focus event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseDown(object sender, MouseEventArgs e) 
        {
            if(!handleLostfocus && clb.ClientRectangle.Contains(clb.PointToClient(new Point(e.X, e.Y))))
            {
                clb.LostFocus += new EventHandler(this.ValueChanged);
                handleLostfocus = true;
            }
        }

        /// <summary>
        /// Occurs when the mouse is moved over the checkedlistbox. 
        /// Sets the tooltip of the item under the pointer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseMoved(object sender, MouseEventArgs e) 
        {            
            int index = clb.IndexFromPoint(e.X, e.Y);
            if(index >= 0)
                tooltipControl.SetToolTip(clb, ((clbItem) clb.Items[index]).Tooltip);
        }

        /// <summary>
        /// Close the dropdowncontrol when the user has selected a value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValueChanged(object sender, EventArgs e) 
        {
            if (edSvc != null) 
            {
                edSvc.CloseDropDown();
            }
        }
    }
}
