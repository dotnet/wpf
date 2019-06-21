// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: ListParaClient is responsible for handling display
//              related data of list paragraphs.
//


using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// ListParaClient is responsible for handling display related data 
    /// of break paragraphs. 
    /// </summary>
    internal sealed class ListParaClient : ContainerParaClient
    {        
        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="paragraph">
        /// ListParagraph associated with this para client.
        /// </param>
        internal ListParaClient(ListParagraph paragraph) : base(paragraph)
        {
        }

        /// <summary>
        /// Validate visual node associated with paragraph.  
        /// </summary>
        /// <param name="fskupdInherited">
        /// Inherited update info
        /// </param>
        internal override void ValidateVisual(PTS.FSKUPDATE fskupdInherited)
        {
            // Query paragraph details
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // Draw border and background info.
            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);
            if (ThisFlowDirection != PageFlowDirection)
            {
                mbp.MirrorBP();
            }

            uint fswdir = PTS.FlowDirectionToFswdir((FlowDirection)Paragraph.Element.GetValue(FrameworkElement.FlowDirectionProperty));

            Brush backgroundBrush = (Brush)Paragraph.Element.GetValue(TextElement.BackgroundProperty);
            
            // This textProperties object is eventually used in creation of LineProperties, which leads to creation of a TextMarkerSource. TextMarkerSource relies on PixelsPerDip
            // from TextProperties, therefore it must be set here properly.
            TextProperties textProperties = new TextProperties(Paragraph.Element, StaticTextPointer.Null, false /* inline objects */, false /* get background */,
                Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            // There might be possibility to get empty sub-track, skip the sub-track in such case.
            if (subtrackDetails.cParas != 0)
            {
                PTS.FSPARADESCRIPTION [] arrayParaDesc;
                PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                using(DrawingContext ctx = _visual.RenderOpen())
                {
                    _visual.DrawBackgroundAndBorderIntoContext(ctx, backgroundBrush, mbp.BorderBrush, mbp.Border, _rect.FromTextDpi(), IsFirstChunk, IsLastChunk);

                        // Get list of paragraphs
                    ListMarkerLine listMarkerLine = new ListMarkerLine(Paragraph.StructuralCache.TextFormatterHost, this);
                    int indexFirstParaInSubtrack = 0;

                    for(int index = 0; index < subtrackDetails.cParas; index++)
                    {
                        List list = Paragraph.Element as List;

                        BaseParaClient listItemParaClient = PtsContext.HandleToObject(arrayParaDesc[index].pfsparaclient) as BaseParaClient;
                        PTS.ValidateHandle(listItemParaClient);

                        if(index == 0)
                        {
                            indexFirstParaInSubtrack = list.GetListItemIndex(listItemParaClient.Paragraph.Element as ListItem);
                        }

                        if(listItemParaClient.IsFirstChunk)
                        {
                            int dvBaseline = listItemParaClient.GetFirstTextLineBaseline();

                            if(PageFlowDirection != ThisFlowDirection)
                            {
                                ctx.PushTransform(new MatrixTransform(-1.0, 0.0, 0.0, 1.0, TextDpi.FromTextDpi(2 * listItemParaClient.Rect.u + listItemParaClient.Rect.du), 0.0));
                            }

                            int adjustedIndex;
                            if (int.MaxValue - index < indexFirstParaInSubtrack)
                            {
                                adjustedIndex = int.MaxValue;
                            }
                            else
                            {
                                adjustedIndex = indexFirstParaInSubtrack + index;
                            }
                            LineProperties lineProps = new LineProperties(Paragraph.Element, Paragraph.StructuralCache.FormattingOwner, textProperties, new MarkerProperties(list, adjustedIndex));
                            listMarkerLine.FormatAndDrawVisual(ctx, lineProps, listItemParaClient.Rect.u, dvBaseline);

                            if(PageFlowDirection != ThisFlowDirection)
                            {
                                ctx.Pop();
                            }
                        }
                    }


                    listMarkerLine.Dispose();
                }

                // Render list of paragraphs
                PtsHelper.UpdateParaListVisuals(PtsContext, _visual.Children, fskupdInherited, arrayParaDesc);
            }
            else
            {
                _visual.Children.Clear();
            }
}
    }
}
