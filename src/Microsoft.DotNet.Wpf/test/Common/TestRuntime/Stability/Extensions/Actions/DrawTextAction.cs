// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Draws formatted text at the specified location.
    /// </summary>
    public class DrawTextAction : SetDrawingContextAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Control Control { get; set; }

        //The location where the text is to be drawn.
        public Point Origin { get; set; }

        //The formatted text to be drawn.
        public FormattedText FormattedText { get; set; }

        #endregion

        public override void Perform()
        {
            DrawingContext Target = SetDrawingContext();

            // Draw a formatted text string into the DrawingContext.
            Target.DrawText(FormattedText, Origin);

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
