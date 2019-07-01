// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Draws the specified text. 
    /// </summary>
    public class DrawGlyphRunAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        //The brush used to paint the text.
        public Brush Brush { get; set; }

        //The text to draw.
        public GlyphRun GlyphRun { get; set; }

        #endregion

        public override void Perform()
        {
            DrawingContext Target = SetDrawingContext();

            //Draws the specified text. 
            Target.DrawGlyphRun(Brush, GlyphRun);

            Target.Close();

            //Add DrawingGroup to DrawingBrush's Drawing property
            ((DrawingBrush)(Control.Background)).Drawing = DrawingGroup;
        }

        /// <summary>
        /// Only can perform when the control's background type is DrawingBrush
        /// </summary>
        public override bool CanPerform()
        {
            if (Control.Background != null)
            {
                return Control.Background.GetType() == typeof(DrawingBrush);
            }
            else
            {
                return false;
            }
        }
    }
}
