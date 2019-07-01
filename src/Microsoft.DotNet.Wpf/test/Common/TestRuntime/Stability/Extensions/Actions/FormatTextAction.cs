// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using Microsoft.Test.Stability.Extensions.Factories;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This class defines the action for formatting text and displaying it. It has a dependency on
    /// \\wpf\testscratch\TextTest\StressDataFiles\Strings where a set of input text files reside.
    /// </summary>
    [TargetTypeAttribute(typeof(FormatTextAction))]
    public class FormatTextAction : SimpleDiscoverableAction
    {
        private static readonly double MinScale = 0.5;
        private static readonly double MaxScale = 30;
        private static readonly int AnimTime = 2500;

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window WindowSource { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string[] TextToFormat { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public CustomTextSource TextSource { get; set; }

        public override void Perform()
        {
            TextFormatter formatter = TextFormatter.Create();
            ListBox listBox = new ListBox();

            DoubleAnimation anim = new DoubleAnimation(MinScale, MaxScale, TimeSpan.FromMilliseconds(AnimTime));
            anim.AutoReverse = true;
            ScaleTransform st = new ScaleTransform(MinScale, MinScale);

            for (int i = 0; i < TextToFormat.Length; i++)
            {
                TextSource.Text = TextToFormat[i];

                DrawingGroup drawingGroup = new DrawingGroup();
                DrawingContext dc = drawingGroup.Open();
                dc.PushTransform(st);
                GenerateFormattedText(TextSource, dc, formatter);
                dc.Close();

                Image image = new Image();
                DrawingImage drawingImage = new DrawingImage(drawingGroup);
                image.Source = drawingImage;
                image.Stretch = Stretch.None;
                image.HorizontalAlignment = HorizontalAlignment.Left;
                image.VerticalAlignment = VerticalAlignment.Top;
                image.Margin = new Thickness(1, 1, 1, 10);

                listBox.Items.Add(image);
            }

            st.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, anim);

            WindowSource.Width = Microsoft.Test.Display.Monitor.Dpi.x * 9; // 9"
            WindowSource.Height = Microsoft.Test.Display.Monitor.Dpi.y * 6; // 6"
            listBox.Height = WindowSource.Height;
            WindowSource.Content = listBox;

            DispatcherHelper.DoEvents(AnimTime * 2);
        }

        private void GenerateFormattedText(CustomTextSource textStore, DrawingContext dc, TextFormatter formatter)
        {
            int textStorePosition = 0;            //Index into the text of the textsource
            Point linePosition = new Point(0, 0); //current line

            // Format each line of text from the text store and draw it.
            while (textStorePosition < textStore.Text.Length)
            {
                TextLine textLine = formatter.FormatLine(textStore, textStorePosition, textStore.ParagraphWidth, textStore.CTPProperties, null);

                // Draw the formatted text into the drawing context.
                textLine.Draw(dc, linePosition, InvertAxes.None);

                // Update the index position in the text store.
                textStorePosition += textLine.Length;

                // Update the line position coordinate for the displayed line.
                linePosition.Y += textLine.Height;
            }
        }
    }
}
