// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//---------------------------------------------------------------------------

//
// This file is automatically generated.  Please do not edit it directly.
//
// File name: renderdata_generated.cpp
//---------------------------------------------------------------------------


#include "precomp.hpp"

HRESULT
CMilSlaveRenderData::GetHandles(CMilSlaveHandleTable *pHandleTable)
{
    HRESULT hr = S_OK;
    UINT nItemID;
    PVOID pItemData;
    UINT nItemDataSize;
    int stackDepth;

    //
    // Set up the command enumeration.
    //

    CMilDataBlockReader cmdReader(m_instructions.FlushData());

    IFC(m_rgpResources.Add(NULL));

    stackDepth = 0;

    //
    // Now get the first item and start executing the render buffer.
    //

    IFC(cmdReader.GetFirstItemSafe(&nItemID, &pItemData, &nItemDataSize));

    while (hr == S_OK)
    {

        //
        // Dispatch the current command to the appropriate handler routine.
        //

        if (SUCCEEDED(hr))
        {
            switch (nItemID)
            {
            default:
                MIL_THR( WGXERR_UCE_MALFORMEDPACKET );
                break;

            case MilDrawLine:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_LINE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_LINE *pData = static_cast<MILCMD_DRAW_LINE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawLineAnimate:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_LINE_ANIMATE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_LINE_ANIMATE *pData = static_cast<MILCMD_DRAW_LINE_ANIMATE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPoint0Animations), TYPE_POINTRESOURCE, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPoint1Animations), TYPE_POINTRESOURCE, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawRectangle:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_RECTANGLE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_RECTANGLE *pData = static_cast<MILCMD_DRAW_RECTANGLE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawRectangleAnimate:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_RECTANGLE_ANIMATE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_RECTANGLE_ANIMATE *pData = static_cast<MILCMD_DRAW_RECTANGLE_ANIMATE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRectangleAnimations), TYPE_RECTRESOURCE, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawRoundedRectangle:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_ROUNDED_RECTANGLE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_ROUNDED_RECTANGLE *pData = static_cast<MILCMD_DRAW_ROUNDED_RECTANGLE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawRoundedRectangleAnimate:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE *pData = static_cast<MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRectangleAnimations), TYPE_RECTRESOURCE, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRadiusXAnimations), TYPE_DOUBLERESOURCE, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRadiusYAnimations), TYPE_DOUBLERESOURCE, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawEllipse:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_ELLIPSE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_ELLIPSE *pData = static_cast<MILCMD_DRAW_ELLIPSE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawEllipseAnimate:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_ELLIPSE_ANIMATE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_ELLIPSE_ANIMATE *pData = static_cast<MILCMD_DRAW_ELLIPSE_ANIMATE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hCenterAnimations), TYPE_POINTRESOURCE, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRadiusXAnimations), TYPE_DOUBLERESOURCE, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRadiusYAnimations), TYPE_DOUBLERESOURCE, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawGeometry:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_GEOMETRY))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_GEOMETRY *pData = static_cast<MILCMD_DRAW_GEOMETRY*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hPen), TYPE_PEN, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hGeometry), TYPE_GEOMETRY, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawImage:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_IMAGE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_IMAGE *pData = static_cast<MILCMD_DRAW_IMAGE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hImageSource), TYPE_IMAGESOURCE, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawImageAnimate:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_IMAGE_ANIMATE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_IMAGE_ANIMATE *pData = static_cast<MILCMD_DRAW_IMAGE_ANIMATE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hImageSource), TYPE_IMAGESOURCE, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRectangleAnimations), TYPE_RECTRESOURCE, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawGlyphRun:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_GLYPH_RUN))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_GLYPH_RUN *pData = static_cast<MILCMD_DRAW_GLYPH_RUN*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hForegroundBrush), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hGlyphRun), TYPE_GLYPHRUN, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawDrawing:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_DRAWING))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_DRAWING *pData = static_cast<MILCMD_DRAW_DRAWING*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hDrawing), TYPE_DRAWING, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawVideo:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_VIDEO))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_VIDEO *pData = static_cast<MILCMD_DRAW_VIDEO*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hPlayer), TYPE_MEDIAPLAYER, &m_rgpResources, pHandleTable));
                }


                break;

            case MilDrawVideoAnimate:
                {
                    if (nItemDataSize < sizeof(MILCMD_DRAW_VIDEO_ANIMATE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_DRAW_VIDEO_ANIMATE *pData = static_cast<MILCMD_DRAW_VIDEO_ANIMATE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hPlayer), TYPE_MEDIAPLAYER, &m_rgpResources, pHandleTable));
                    IFC(AddHandleToArrayAndReplace(&(pData->hRectangleAnimations), TYPE_RECTRESOURCE, &m_rgpResources, pHandleTable));
                }


                break;

            case MilPushClip:
                {
                    if (nItemDataSize < sizeof(MILCMD_PUSH_CLIP))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_PUSH_CLIP *pData = static_cast<MILCMD_PUSH_CLIP*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hClipGeometry), TYPE_GEOMETRY, &m_rgpResources, pHandleTable));
                }

                stackDepth++;
                break;

            case MilPushOpacityMask:
                {
                    if (nItemDataSize < sizeof(MILCMD_PUSH_OPACITY_MASK))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_PUSH_OPACITY_MASK *pData = static_cast<MILCMD_PUSH_OPACITY_MASK*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hOpacityMask), TYPE_BRUSH, &m_rgpResources, pHandleTable));
                }

                stackDepth++;
                break;

            case MilPushOpacity:
                stackDepth++;
                break;

            case MilPushOpacityAnimate:
                {
                    if (nItemDataSize < sizeof(MILCMD_PUSH_OPACITY_ANIMATE))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_PUSH_OPACITY_ANIMATE *pData = static_cast<MILCMD_PUSH_OPACITY_ANIMATE*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hOpacityAnimations), TYPE_DOUBLERESOURCE, &m_rgpResources, pHandleTable));
                }

                stackDepth++;
                break;

            case MilPushTransform:
                {
                    if (nItemDataSize < sizeof(MILCMD_PUSH_TRANSFORM))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_PUSH_TRANSFORM *pData = static_cast<MILCMD_PUSH_TRANSFORM*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hTransform), TYPE_TRANSFORM, &m_rgpResources, pHandleTable));
                }

                stackDepth++;
                break;

            case MilPushGuidelineSet:
                {
                    if (nItemDataSize < sizeof(MILCMD_PUSH_GUIDELINE_SET))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_PUSH_GUIDELINE_SET *pData = static_cast<MILCMD_PUSH_GUIDELINE_SET*>(pItemData);
                    IFC(AddHandleToArrayAndReplace(&(pData->hGuidelines), TYPE_GUIDELINESET, &m_rgpResources, pHandleTable));
                }

                stackDepth++;
                break;

            case MilPushGuidelineY1:
                {
                    if (nItemDataSize < sizeof(MILCMD_PUSH_GUIDELINE_Y1))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_PUSH_GUIDELINE_Y1 *pData = static_cast<MILCMD_PUSH_GUIDELINE_Y1*>(pItemData);

                    CGuidelineCollection* pGuidelineCollection = NULL;
                    float coordinates[2] = {
                        static_cast<float>(pData->coordinate),
                        0
                        };

                    MIL_THR(CDynamicGuidelineCollection::Create(
                        0, //uCountX
                        2, //uCountY
                        coordinates,
                        &pGuidelineCollection
                        ));
                    if (FAILED(hr))
                    {
                        Assert(pGuidelineCollection == NULL);
                        if (hr != WGXERR_MALFORMED_GUIDELINE_DATA)
                            goto Cleanup;
                        // WGXERR_MALFORMED_GUIDELINE_DATA handling:
                        // allow NULL in m_rgpGuidelineKits that will
                        // cause pushing empty guideline frame
                    }

                    MIL_THR(m_rgpGuidelineKits.Add(pGuidelineCollection));
                    if (FAILED(hr))
                    {
                        delete pGuidelineCollection;
                        goto Cleanup;
                    }

                    *(reinterpret_cast<UINT*>(&pData->coordinate)) = m_rgpGuidelineKits.GetCount() - 1;
                }              

                stackDepth++;
                break;

            case MilPushGuidelineY2:
                {
                    if (nItemDataSize < sizeof(MILCMD_PUSH_GUIDELINE_Y2))
                    {
                        IFC(WGXERR_UCE_MALFORMEDPACKET);
                    }

                    MILCMD_PUSH_GUIDELINE_Y2 *pData = static_cast<MILCMD_PUSH_GUIDELINE_Y2*>(pItemData);

                    float coordinates[2] =
                    {
                        static_cast<float>(pData->leadingCoordinate),
                        static_cast<float>(pData->offsetToDrivenCoordinate)
                    };

                    CGuidelineCollection* pGuidelineCollection = NULL;

                    MIL_THR(CDynamicGuidelineCollection::Create(
                        0, //uCountX
                        2, //uCountY
                        coordinates,
                        &pGuidelineCollection
                        ));
                    if (FAILED(hr))
                    {
                        Assert(pGuidelineCollection == NULL);
                        if (hr != WGXERR_MALFORMED_GUIDELINE_DATA)
                            goto Cleanup;
                        // WGXERR_MALFORMED_GUIDELINE_DATA handling:
                        // allow NULL in m_rgpGuidelineKits that will
                        // cause pushing empty guideline frame
                    }

                    MIL_THR(m_rgpGuidelineKits.Add(pGuidelineCollection));
                    if (FAILED(hr))
                    {
                        delete pGuidelineCollection;
                        goto Cleanup;
                    }

                    *(reinterpret_cast<UINT*>(&pData->leadingCoordinate)) = m_rgpGuidelineKits.GetCount() - 1;
                }

                stackDepth++;
                break;

            case MilPushEffect:
                stackDepth++;
                break;

            case MilPop:
                stackDepth--;
                break;
            }
        }

        IFC(cmdReader.GetNextItemSafe(
            &nItemID,
            &pItemData,
            &nItemDataSize
            ));
    }

    //
    // S_FALSE means that we reached the end of the stream. Hence we executed the stream
    // correctly and therefore we should return S_OK.
    //

    if (hr == S_FALSE)
    {
        hr = S_OK;
    }

    //
    // Ensure that we have a matched number of Push/
    // Pop calls, otherwise the render data is invalid
    //
    if (stackDepth != 0)
    {
        IFC(WGXERR_UCE_MALFORMEDPACKET);
    }

Cleanup:

    Assert(SUCCEEDED(hr));
    RRETURN(hr);
}

