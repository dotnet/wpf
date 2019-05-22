// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Helpers for figure formatting
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;

using MS.Internal.Text;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Helper services to assist formatting figure
    // ----------------------------------------------------------------------
    internal static class FigureHelper
    {
        // ------------------------------------------------------------------
        // Returns whether a vertical anchor is relative to page
        // ------------------------------------------------------------------
        internal static bool IsVerticalPageAnchor(FigureVerticalAnchor verticalAnchor)
        {
            return verticalAnchor == FigureVerticalAnchor.PageTop || 
                   verticalAnchor == FigureVerticalAnchor.PageBottom || 
                   verticalAnchor == FigureVerticalAnchor.PageCenter;
        }
    
        // ------------------------------------------------------------------
        // Returns whether a vertical anchor is relative to content
        // ------------------------------------------------------------------
        internal static bool IsVerticalContentAnchor(FigureVerticalAnchor verticalAnchor)
        {
            return verticalAnchor == FigureVerticalAnchor.ContentTop || 
                   verticalAnchor == FigureVerticalAnchor.ContentBottom || 
                   verticalAnchor == FigureVerticalAnchor.ContentCenter;
        }
    
    
        // ------------------------------------------------------------------
        // Returns whether a horizontal anchor is relative to page
        // ------------------------------------------------------------------
        internal static bool IsHorizontalPageAnchor(FigureHorizontalAnchor horizontalAnchor)
        {
            return horizontalAnchor == FigureHorizontalAnchor.PageLeft  ||
                   horizontalAnchor == FigureHorizontalAnchor.PageRight ||
                   horizontalAnchor == FigureHorizontalAnchor.PageCenter;
        }
    
        // ------------------------------------------------------------------
        // Returns whether a horizontal anchor is relative to content
        // ------------------------------------------------------------------
        internal static bool IsHorizontalContentAnchor(FigureHorizontalAnchor horizontalAnchor)
        {
            return horizontalAnchor == FigureHorizontalAnchor.ContentLeft  ||
                   horizontalAnchor == FigureHorizontalAnchor.ContentRight ||
                   horizontalAnchor == FigureHorizontalAnchor.ContentCenter;
        }
    
        // ------------------------------------------------------------------
        // Returns whether a horizontal anchor is relative to column
        // ------------------------------------------------------------------
        internal static bool IsHorizontalColumnAnchor(FigureHorizontalAnchor horizontalAnchor)
        {
            return horizontalAnchor == FigureHorizontalAnchor.ColumnLeft  ||
                   horizontalAnchor == FigureHorizontalAnchor.ColumnRight ||
                   horizontalAnchor == FigureHorizontalAnchor.ColumnCenter;
        }

        // ------------------------------------------------------------------
        // Width figure size calculation
        // ------------------------------------------------------------------
        internal static double CalculateFigureWidth(StructuralCache structuralCache, Figure figure, FigureLength figureLength, out bool isWidthAuto)
        {
            double value;
    
            isWidthAuto = figureLength.IsAuto ? true : false;
    
            // Check figure's horizontal anchor. If anchored to page, use page width to format
    
            FigureHorizontalAnchor horizontalAnchor = figure.HorizontalAnchor;
    
            if(figureLength.IsPage || (figureLength.IsAuto && IsHorizontalPageAnchor(horizontalAnchor)))
            {
                value = structuralCache.CurrentFormatContext.PageWidth * figureLength.Value;
            }
            else if (figureLength.IsAbsolute)
            {
                value = CalculateFigureCommon(figureLength);
            }
            else // figureLength.IsColumn || figureLength.IsContent || figureLength.IsAuto 
            {
                double columnWidth, gap, rule;
                int cColumns;
    
                GetColumnMetrics(structuralCache, out cColumns, out columnWidth, out gap, out rule);
    
                if (figureLength.IsContent || (figureLength.IsAuto && IsHorizontalContentAnchor(horizontalAnchor)))
                {
                    // Use content width for figure
                    value = (columnWidth * cColumns + gap * (cColumns - 1)) * figureLength.Value;
                }
                else // figureLength.IsColumn || figureLength.IsAuto
                {
                    // We do this to prevent a 2.0 columns from spanning 2.0 + gap, so we just check for edge
                    double lengthValue = figureLength.Value;
    
                    int columnGapsSpanned = (int) lengthValue;
                    if(columnGapsSpanned == lengthValue && columnGapsSpanned > 0)
                    {
                        columnGapsSpanned -= 1;
                    }
    
                    value = (columnWidth * lengthValue) + gap * columnGapsSpanned;
                }
            }
    
            Invariant.Assert(!DoubleUtil.IsNaN(value));
    
            return value;
        }

        // ------------------------------------------------------------------
        // Height figure size calculation
        // ------------------------------------------------------------------
        internal static double CalculateFigureHeight(StructuralCache structuralCache, Figure figure, FigureLength figureLength, out bool isHeightAuto)
        {
            double value; 

            if(figureLength.IsPage) 
            { 
                value = (structuralCache.CurrentFormatContext.PageHeight) * figureLength.Value;
            }
            else if(figureLength.IsContent) // Column to be treated same as content
            {
                Thickness pageMargin = structuralCache.CurrentFormatContext.PageMargin;

                value = (structuralCache.CurrentFormatContext.PageHeight - pageMargin.Top - pageMargin.Bottom) * figureLength.Value;
            }
            else if (figureLength.IsColumn)
            {
                // Height is calculated based on column width, since column height is the same as content. Per spec.
                // Retrieve all column metrics for current page
                int cColumns;
                double columnWidth;
                double gap;
                double rule;
                FigureHelper.GetColumnMetrics(structuralCache, out cColumns, out columnWidth, out gap, out rule);

                // We do this to prevent a 2.0 columns from spanning 2.0 + gap, so we just check for edge
                double lengthValue = figureLength.Value;
                if (lengthValue > cColumns)
                {
                    lengthValue = cColumns;
                }
                int columnGapsSpanned = (int)lengthValue;
                if (columnGapsSpanned == lengthValue && columnGapsSpanned > 0)
                {
                    columnGapsSpanned -= 1;
                }

                value = (columnWidth * lengthValue) + gap * columnGapsSpanned;
            }
            else
            {
                value = FigureHelper.CalculateFigureCommon(figureLength);
            }

            if(!DoubleUtil.IsNaN(value))
            {
                FigureVerticalAnchor verticalAnchor = figure.VerticalAnchor;

                // Value is in pixels. Now we limit value to max out depending on anchoring.
                if(FigureHelper.IsVerticalPageAnchor(verticalAnchor))
                {
                    value = Math.Max(1, Math.Min(value, structuralCache.CurrentFormatContext.PageHeight));
                }
                else // Column and paragraph anchoring still max out at content height
                {
                    Thickness pageMargin = structuralCache.CurrentFormatContext.PageMargin;
                    value = Math.Max(1, Math.Min(value, structuralCache.CurrentFormatContext.PageHeight - pageMargin.Top - pageMargin.Bottom));
                }

                TextDpi.EnsureValidPageWidth(ref value);

                isHeightAuto = false;
            }
            else
            {
                value = structuralCache.CurrentFormatContext.PageHeight;
                isHeightAuto = true;
            }

            return value;
        }

    
        // ------------------------------------------------------------------
        // Common figure size calculation
        // ------------------------------------------------------------------
        internal static double CalculateFigureCommon(FigureLength figureLength)
        {
            double value;
    
            if(figureLength.IsAuto)
            {
                value = Double.NaN;
            } 
            else if(figureLength.IsAbsolute)
            {
                value = figureLength.Value;
            }
            else
            {
                Invariant.Assert(false, "Unknown figure length type specified.");
                value = 0.0;
            }
    
            return value;
        }
    
    
        // ------------------------------------------------------------------
        // Returns calculated column information -  count, width, gap and rule.
        // ------------------------------------------------------------------
        internal static void GetColumnMetrics(StructuralCache structuralCache, out int cColumns, out double width, out double gap, out double rule)
        {
            ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(structuralCache.PropertyOwner);
            FontFamily pageFontFamily = (FontFamily)structuralCache.PropertyOwner.GetValue(Block.FontFamilyProperty);
            double lineHeight = DynamicPropertyReader.GetLineHeightValue(structuralCache.PropertyOwner);
            double pageFontSize = (double)structuralCache.PropertyOwner.GetValue(Block.FontSizeProperty);
            Size pageSize = structuralCache.CurrentFormatContext.PageSize;
            Thickness pageMargin = structuralCache.CurrentFormatContext.PageMargin;
            double pageWidth = pageSize.Width - (pageMargin.Left + pageMargin.Right);
    
            cColumns = PtsHelper.CalculateColumnCount(columnProperties, lineHeight, pageWidth, pageFontSize, pageFontFamily, true);
    
            double freeSpace;
            rule = columnProperties.ColumnRuleWidth;
            PtsHelper.GetColumnMetrics(columnProperties, pageWidth,
                                       pageFontSize, pageFontFamily, true, cColumns,
                                       ref lineHeight, out width, out freeSpace, out gap);
    
            if (columnProperties.IsColumnWidthFlexible && columnProperties.ColumnSpaceDistribution == ColumnSpaceDistribution.Between)
            {
                width += freeSpace / cColumns;
            }
    
            width = Math.Min(width, pageWidth);
        }
    }
}

#pragma warning enable 1634, 1691

