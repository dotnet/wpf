// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Visual representing a section. 
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Visual representing a section.
    // ----------------------------------------------------------------------
    internal class SectionVisual : DrawingVisual
    {
        // ------------------------------------------------------------------
        // Constructor.
        // ------------------------------------------------------------------
        internal SectionVisual()
        {
        }

        // ------------------------------------------------------------------
        // Set information about column rules that are necessary for rendering
        // process. Draw column rules if necessary.
        //
        //      arrayColumnDesc - column description including rectangle coordinates, etc
        //      columnVStart - vertical start coordinate of column rule 
        //      columnHeight - height of column rule
        //      columnProperties - column properties.
        // ------------------------------------------------------------------
        internal void DrawColumnRules(ref PTS.FSTRACKDESCRIPTION[] arrayColumnDesc, double columnVStart, double columnHeight, ColumnPropertiesGroup columnProperties)
        {
            // Compute column rules data first.
            Point[] rulePositions = null;
            double ruleWidth = columnProperties.ColumnRuleWidth;
            if (arrayColumnDesc.Length > 1)
            {
                if (ruleWidth > 0)
                {
                    int gapWidth = (arrayColumnDesc[1].fsrc.u - (arrayColumnDesc[0].fsrc.u + arrayColumnDesc[0].fsrc.du)) / 2;
                    rulePositions = new Point[(arrayColumnDesc.Length - 1)*2];
                    for (int index = 1; index < arrayColumnDesc.Length; index++)
                    {
                        double u = TextDpi.FromTextDpi(arrayColumnDesc[index].fsrc.u - gapWidth);
                        double v = columnVStart;
                        double dv = columnHeight;
                        rulePositions[(index-1)*2].X = u;
                        rulePositions[(index-1)*2].Y = v;
                        rulePositions[(index-1)*2+1].X = u;
                        rulePositions[(index-1)*2+1].Y = v + dv;
                    }
                }
            }

            // Check if update of the visual render data is needed.
            bool needsUpdate = _ruleWidth != ruleWidth;
            if (!needsUpdate && _rulePositions != rulePositions)
            {
                int prevSize = _rulePositions == null ? 0 : _rulePositions.Length;
                int newSize = rulePositions == null ? 0 : rulePositions.Length;
                if (prevSize == newSize)
                {
                    for (int index = 0; index < rulePositions.Length; index++)
                    {
                        if (!DoubleUtil.AreClose(rulePositions[index].X, _rulePositions[index].X) || 
                            !DoubleUtil.AreClose(rulePositions[index].Y, _rulePositions[index].Y))
                        {
                            needsUpdate = true;
                            break;
                        }
                    }
                }
                else
                {
                    needsUpdate = true;
                }
            }

            // Draw column rules if necessary
            if (needsUpdate)
            {
                _ruleWidth = ruleWidth;
                _rulePositions = rulePositions;

                // Open DrawingContext and draw background.
                // If background is not set, Open will clear the render data, but it
                // will preserve visual children.
                using (DrawingContext dc = RenderOpen())
                {
                    if (rulePositions != null)
                    {
                        // We do not want to cause the user's Brush to become frozen when we
                        // freeze pen below, therefore we make our own copy of the Brush if
                        // it is not already frozen.
                        Brush columnRuleBrush = (Brush)FreezableOperations.GetAsFrozenIfPossible(columnProperties.ColumnRuleBrush);
                        
                        Pen pen = new Pen(columnRuleBrush, ruleWidth);
                        
                        // Freeze the pen if possible.  Doing this avoids the overhead of
                        // maintaining changed handlers.
                        if (pen.CanFreeze) { pen.Freeze(); }

                        for (int index = 0; index < rulePositions.Length; index += 2)
                        {
                            dc.DrawLine(pen, rulePositions[index], rulePositions[index + 1]);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Column rules positions
        // ------------------------------------------------------------------
        private Point[] _rulePositions;

        // ------------------------------------------------------------------
        // Pen for column rule.
        // ------------------------------------------------------------------
        private double _ruleWidth;
    }
}
