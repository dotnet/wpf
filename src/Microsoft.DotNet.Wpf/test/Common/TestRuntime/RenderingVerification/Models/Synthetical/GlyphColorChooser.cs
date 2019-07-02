// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
//        using System.Threading; 
//        using System.Collections;
        using System.Windows.Forms;
        using System.Drawing.Design;
        using System.ComponentModel;
//        using System.Drawing.Drawing2D;
//        using System.Runtime.Serialization.Formatters.Soap;
//        using Microsoft.Test.RenderingVerification.Filters;
    #endregion using
    /// <summary>
    /// The dedicated color editor for glyphs
    /// </summary>
    public class GlyphColorChooser: UITypeEditor
    {
        /// <summary>
        /// returns the edit style (modal in this case)
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }


        /// <summary>
        /// returns the value from the property editor
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IColor vlc = (IColor)context.PropertyDescriptor.GetValue(context.Instance);
            ColorDialog cdg = new ColorDialog();
            Color refc = vlc.ToColor();

            cdg.Color = refc;
            if (cdg.ShowDialog() == DialogResult.OK)
            {
                vlc = (ColorByte)cdg.Color;
                vlc.IsEmpty = false;
                cdg.Dispose();
            }

            return vlc;
        }
    }
}
