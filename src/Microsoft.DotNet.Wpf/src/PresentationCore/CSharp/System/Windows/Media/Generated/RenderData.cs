// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Security;

namespace System.Windows.Media
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_LINE
    {
       public MILCMD_DRAW_LINE (
           UInt32 hPen,
           Point point0,
           Point point1
           )
       {
           this.hPen = hPen;
           this.point0 = point0;
           this.point1 = point1;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public Point point0;
       [FieldOffset(16)] public Point point1;
       [FieldOffset(32)] public UInt32 hPen;
       [FieldOffset(36)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_LINE_ANIMATE
    {
       public MILCMD_DRAW_LINE_ANIMATE (
           UInt32 hPen,
           Point point0,
           UInt32 hPoint0Animations,
           Point point1,
           UInt32 hPoint1Animations
           )
       {
           this.hPen = hPen;
           this.point0 = point0;
           this.hPoint0Animations = hPoint0Animations;
           this.point1 = point1;
           this.hPoint1Animations = hPoint1Animations;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public Point point0;
       [FieldOffset(16)] public Point point1;
       [FieldOffset(32)] public UInt32 hPen;
       [FieldOffset(36)] public UInt32 hPoint0Animations;
       [FieldOffset(40)] public UInt32 hPoint1Animations;
       [FieldOffset(44)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_RECTANGLE
    {
       public MILCMD_DRAW_RECTANGLE (
           UInt32 hBrush,
           UInt32 hPen,
           Rect rectangle
           )
       {
           this.hBrush = hBrush;
           this.hPen = hPen;
           this.rectangle = rectangle;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public UInt32 hBrush;
       [FieldOffset(36)] public UInt32 hPen;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_RECTANGLE_ANIMATE
    {
       public MILCMD_DRAW_RECTANGLE_ANIMATE (
           UInt32 hBrush,
           UInt32 hPen,
           Rect rectangle,
           UInt32 hRectangleAnimations
           )
       {
           this.hBrush = hBrush;
           this.hPen = hPen;
           this.rectangle = rectangle;
           this.hRectangleAnimations = hRectangleAnimations;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public UInt32 hBrush;
       [FieldOffset(36)] public UInt32 hPen;
       [FieldOffset(40)] public UInt32 hRectangleAnimations;
       [FieldOffset(44)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_ROUNDED_RECTANGLE
    {
       public MILCMD_DRAW_ROUNDED_RECTANGLE (
           UInt32 hBrush,
           UInt32 hPen,
           Rect rectangle,
           double radiusX,
           double radiusY
           )
       {
           this.hBrush = hBrush;
           this.hPen = hPen;
           this.rectangle = rectangle;
           this.radiusX = radiusX;
           this.radiusY = radiusY;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public double radiusX;
       [FieldOffset(40)] public double radiusY;
       [FieldOffset(48)] public UInt32 hBrush;
       [FieldOffset(52)] public UInt32 hPen;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE
    {
       public MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE (
           UInt32 hBrush,
           UInt32 hPen,
           Rect rectangle,
           UInt32 hRectangleAnimations,
           double radiusX,
           UInt32 hRadiusXAnimations,
           double radiusY,
           UInt32 hRadiusYAnimations
           )
       {
           this.hBrush = hBrush;
           this.hPen = hPen;
           this.rectangle = rectangle;
           this.hRectangleAnimations = hRectangleAnimations;
           this.radiusX = radiusX;
           this.hRadiusXAnimations = hRadiusXAnimations;
           this.radiusY = radiusY;
           this.hRadiusYAnimations = hRadiusYAnimations;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public double radiusX;
       [FieldOffset(40)] public double radiusY;
       [FieldOffset(48)] public UInt32 hBrush;
       [FieldOffset(52)] public UInt32 hPen;
       [FieldOffset(56)] public UInt32 hRectangleAnimations;
       [FieldOffset(60)] public UInt32 hRadiusXAnimations;
       [FieldOffset(64)] public UInt32 hRadiusYAnimations;
       [FieldOffset(68)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_ELLIPSE
    {
       public MILCMD_DRAW_ELLIPSE (
           UInt32 hBrush,
           UInt32 hPen,
           Point center,
           double radiusX,
           double radiusY
           )
       {
           this.hBrush = hBrush;
           this.hPen = hPen;
           this.center = center;
           this.radiusX = radiusX;
           this.radiusY = radiusY;
       }

       [FieldOffset(0)] public Point center;
       [FieldOffset(16)] public double radiusX;
       [FieldOffset(24)] public double radiusY;
       [FieldOffset(32)] public UInt32 hBrush;
       [FieldOffset(36)] public UInt32 hPen;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_ELLIPSE_ANIMATE
    {
       public MILCMD_DRAW_ELLIPSE_ANIMATE (
           UInt32 hBrush,
           UInt32 hPen,
           Point center,
           UInt32 hCenterAnimations,
           double radiusX,
           UInt32 hRadiusXAnimations,
           double radiusY,
           UInt32 hRadiusYAnimations
           )
       {
           this.hBrush = hBrush;
           this.hPen = hPen;
           this.center = center;
           this.hCenterAnimations = hCenterAnimations;
           this.radiusX = radiusX;
           this.hRadiusXAnimations = hRadiusXAnimations;
           this.radiusY = radiusY;
           this.hRadiusYAnimations = hRadiusYAnimations;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public Point center;
       [FieldOffset(16)] public double radiusX;
       [FieldOffset(24)] public double radiusY;
       [FieldOffset(32)] public UInt32 hBrush;
       [FieldOffset(36)] public UInt32 hPen;
       [FieldOffset(40)] public UInt32 hCenterAnimations;
       [FieldOffset(44)] public UInt32 hRadiusXAnimations;
       [FieldOffset(48)] public UInt32 hRadiusYAnimations;
       [FieldOffset(52)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_GEOMETRY
    {
       public MILCMD_DRAW_GEOMETRY (
           UInt32 hBrush,
           UInt32 hPen,
           UInt32 hGeometry
           )
       {
           this.hBrush = hBrush;
           this.hPen = hPen;
           this.hGeometry = hGeometry;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public UInt32 hBrush;
       [FieldOffset(4)] public UInt32 hPen;
       [FieldOffset(8)] public UInt32 hGeometry;
       [FieldOffset(12)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_IMAGE
    {
       public MILCMD_DRAW_IMAGE (
           UInt32 hImageSource,
           Rect rectangle
           )
       {
           this.hImageSource = hImageSource;
           this.rectangle = rectangle;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public UInt32 hImageSource;
       [FieldOffset(36)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_IMAGE_ANIMATE
    {
       public MILCMD_DRAW_IMAGE_ANIMATE (
           UInt32 hImageSource,
           Rect rectangle,
           UInt32 hRectangleAnimations
           )
       {
           this.hImageSource = hImageSource;
           this.rectangle = rectangle;
           this.hRectangleAnimations = hRectangleAnimations;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public UInt32 hImageSource;
       [FieldOffset(36)] public UInt32 hRectangleAnimations;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_GLYPH_RUN
    {
       public MILCMD_DRAW_GLYPH_RUN (
           UInt32 hForegroundBrush,
           UInt32 hGlyphRun
           )
       {
           this.hForegroundBrush = hForegroundBrush;
           this.hGlyphRun = hGlyphRun;
       }

       [FieldOffset(0)] public UInt32 hForegroundBrush;
       [FieldOffset(4)] public UInt32 hGlyphRun;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_DRAWING
    {
       public MILCMD_DRAW_DRAWING (
           UInt32 hDrawing
           )
       {
           this.hDrawing = hDrawing;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public UInt32 hDrawing;
       [FieldOffset(4)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_VIDEO
    {
       public MILCMD_DRAW_VIDEO (
           UInt32 hPlayer,
           Rect rectangle
           )
       {
           this.hPlayer = hPlayer;
           this.rectangle = rectangle;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public UInt32 hPlayer;
       [FieldOffset(36)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_DRAW_VIDEO_ANIMATE
    {
       public MILCMD_DRAW_VIDEO_ANIMATE (
           UInt32 hPlayer,
           Rect rectangle,
           UInt32 hRectangleAnimations
           )
       {
           this.hPlayer = hPlayer;
           this.rectangle = rectangle;
           this.hRectangleAnimations = hRectangleAnimations;
       }

       [FieldOffset(0)] public Rect rectangle;
       [FieldOffset(32)] public UInt32 hPlayer;
       [FieldOffset(36)] public UInt32 hRectangleAnimations;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_CLIP
    {
       public MILCMD_PUSH_CLIP (
           UInt32 hClipGeometry
           )
       {
           this.hClipGeometry = hClipGeometry;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public UInt32 hClipGeometry;
       [FieldOffset(4)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_OPACITY_MASK
    {
       public MILCMD_PUSH_OPACITY_MASK (
           UInt32 hOpacityMask
           )
       {
           this.hOpacityMask = hOpacityMask;
           this.boundingBoxCacheLocalSpace = default(MilRectF);
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public MilRectF boundingBoxCacheLocalSpace;
       [FieldOffset(16)] public UInt32 hOpacityMask;
       [FieldOffset(20)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_OPACITY
    {
       public MILCMD_PUSH_OPACITY (
           double opacity
           )
       {
           this.opacity = opacity;
       }

       [FieldOffset(0)] public double opacity;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_OPACITY_ANIMATE
    {
       public MILCMD_PUSH_OPACITY_ANIMATE (
           double opacity,
           UInt32 hOpacityAnimations
           )
       {
           this.opacity = opacity;
           this.hOpacityAnimations = hOpacityAnimations;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public double opacity;
       [FieldOffset(8)] public UInt32 hOpacityAnimations;
       [FieldOffset(12)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_TRANSFORM
    {
       public MILCMD_PUSH_TRANSFORM (
           UInt32 hTransform
           )
       {
           this.hTransform = hTransform;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public UInt32 hTransform;
       [FieldOffset(4)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_GUIDELINE_SET
    {
       public MILCMD_PUSH_GUIDELINE_SET (
           UInt32 hGuidelines
           )
       {
           this.hGuidelines = hGuidelines;
           this.QuadWordPad0 = 0;
       }

       [FieldOffset(0)] public UInt32 hGuidelines;
       [FieldOffset(4)] private UInt32 QuadWordPad0;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_GUIDELINE_Y1
    {
       public MILCMD_PUSH_GUIDELINE_Y1 (
           double coordinate
           )
       {
           this.coordinate = coordinate;
       }

       [FieldOffset(0)] public double coordinate;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_GUIDELINE_Y2
    {
       public MILCMD_PUSH_GUIDELINE_Y2 (
           double leadingCoordinate,
           double offsetToDrivenCoordinate
           )
       {
           this.leadingCoordinate = leadingCoordinate;
           this.offsetToDrivenCoordinate = offsetToDrivenCoordinate;
       }

       [FieldOffset(0)] public double leadingCoordinate;
       [FieldOffset(8)] public double offsetToDrivenCoordinate;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_PUSH_EFFECT
    {
       public MILCMD_PUSH_EFFECT (
           UInt32 hEffect,
           UInt32 hEffectInput
           )
       {
           this.hEffect = hEffect;
           this.hEffectInput = hEffectInput;
       }

       [FieldOffset(0)] public UInt32 hEffect;
       [FieldOffset(4)] public UInt32 hEffectInput;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MILCMD_POP
    {
    }


    /// <summary>
    ///     RenderDataDrawingContext - A DrawingContext which produces a Drawing.
    /// </summary>
    internal partial class RenderData: DUCE.IResource
    {
        /// <summary>
        /// MarshalToDUCE - Marshalling code to the DUCE
        /// </summary>
        private void MarshalToDUCE(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource.IsOnChannel(channel));

            DUCE.MILCMD_RENDERDATA renderdataCmd;
            renderdataCmd.Type = MILCMD.MilCmdRenderData;
            renderdataCmd.Handle = _duceResource.GetHandle(channel);
            renderdataCmd.cbData = (uint)DataSize;

            // This is the total extra size required
            uint cbExtraData = renderdataCmd.cbData;

            // This cast is to ensure that cbExtraData can be cast to an int without
            // wrapping over, since in managed code indices are int, not uint.
            Debug.Assert(cbExtraData <= (uint)Int32.MaxValue);

            unsafe
            {
                channel.BeginCommand(
                    (byte*)&renderdataCmd,
                    sizeof(DUCE.MILCMD_RENDERDATA),
                    (int)cbExtraData
                    );

                // We shouldn't have any dependent resources if _curOffset is 0
                // (_curOffset == 0) -> (renderData._dependentResources.Count == 0)
                Debug.Assert((_curOffset > 0) || (_dependentResources.Count == 0));

                // The buffer being null implies that _curOffset must be 0.
                // (_buffer == null) -> (_curOffset == 0)
                Debug.Assert((_buffer != null) || (_curOffset == 0));

                // The _curOffset must be less than the length, if there is a buffer.
                Debug.Assert((_buffer == null) || (_curOffset <= _buffer.Length));

                Stack<PushType> pushStack = new Stack<PushType>();
                int pushedEffects = 0;

                if (_curOffset > 0)
                {
                    fixed (byte* pByte = this._buffer)
                    {
                        // This pointer points to the current read point in the
                        // instruction stream.
                        byte* pCur = pByte;

                        // This points to the first byte past the end of the
                        // instruction stream (i.e. when to stop)
                        byte* pEndOfInstructions = pByte + _curOffset;

                        while (pCur < pEndOfInstructions)
                        {
                            RecordHeader *pCurRecord = (RecordHeader*)pCur;

                            channel.AppendCommandData(
                            (byte*)pCurRecord,
                                sizeof(RecordHeader)
                                );

                             switch (pCurRecord->Id)
                             {
                                case MILCMD.MilDrawLine:
                                {
                                    MILCMD_DRAW_LINE data = *(MILCMD_DRAW_LINE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hPen != 0 )
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        40 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawLineAnimate:
                                {
                                    MILCMD_DRAW_LINE_ANIMATE data = *(MILCMD_DRAW_LINE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hPen != 0)
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPoint0Animations != 0)
                                    {
                                        data.hPoint0Animations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPoint0Animations - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPoint1Animations != 0)
                                    {
                                        data.hPoint1Animations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPoint1Animations - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        48 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRectangle:
                                {
                                    MILCMD_DRAW_RECTANGLE data = *(MILCMD_DRAW_RECTANGLE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hBrush != 0 )
                                    {
                                        data.hBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPen != 0 )
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        40 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRectangleAnimate:
                                {
                                    MILCMD_DRAW_RECTANGLE_ANIMATE data = *(MILCMD_DRAW_RECTANGLE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hBrush != 0)
                                    {
                                        data.hBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPen != 0)
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRectangleAnimations != 0)
                                    {
                                        data.hRectangleAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRectangleAnimations - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        48 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRoundedRectangle:
                                {
                                    MILCMD_DRAW_ROUNDED_RECTANGLE data = *(MILCMD_DRAW_ROUNDED_RECTANGLE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hBrush != 0 )
                                    {
                                        data.hBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPen != 0 )
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        56 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRoundedRectangleAnimate:
                                {
                                    MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE data = *(MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hBrush != 0)
                                    {
                                        data.hBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPen != 0)
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRectangleAnimations != 0)
                                    {
                                        data.hRectangleAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRectangleAnimations - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRadiusXAnimations != 0)
                                    {
                                        data.hRadiusXAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRadiusXAnimations - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRadiusYAnimations != 0)
                                    {
                                        data.hRadiusYAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRadiusYAnimations - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        72 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawEllipse:
                                {
                                    MILCMD_DRAW_ELLIPSE data = *(MILCMD_DRAW_ELLIPSE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hBrush != 0 )
                                    {
                                        data.hBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPen != 0 )
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        40 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawEllipseAnimate:
                                {
                                    MILCMD_DRAW_ELLIPSE_ANIMATE data = *(MILCMD_DRAW_ELLIPSE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hBrush != 0)
                                    {
                                        data.hBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPen != 0)
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hCenterAnimations != 0)
                                    {
                                        data.hCenterAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hCenterAnimations - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRadiusXAnimations != 0)
                                    {
                                        data.hRadiusXAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRadiusXAnimations - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRadiusYAnimations != 0)
                                    {
                                        data.hRadiusYAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRadiusYAnimations - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        56 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawGeometry:
                                {
                                    MILCMD_DRAW_GEOMETRY data = *(MILCMD_DRAW_GEOMETRY*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hBrush != 0 )
                                    {
                                        data.hBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hPen != 0 )
                                    {
                                        data.hPen = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPen - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hGeometry != 0 )
                                    {
                                        data.hGeometry = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hGeometry - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        16 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawImage:
                                {
                                    MILCMD_DRAW_IMAGE data = *(MILCMD_DRAW_IMAGE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hImageSource != 0 )
                                    {
                                        data.hImageSource = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hImageSource - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        40 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawImageAnimate:
                                {
                                    MILCMD_DRAW_IMAGE_ANIMATE data = *(MILCMD_DRAW_IMAGE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hImageSource != 0)
                                    {
                                        data.hImageSource = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hImageSource - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRectangleAnimations != 0)
                                    {
                                        data.hRectangleAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRectangleAnimations - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        40 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawGlyphRun:
                                {
                                    MILCMD_DRAW_GLYPH_RUN data = *(MILCMD_DRAW_GLYPH_RUN*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hForegroundBrush != 0 )
                                    {
                                        data.hForegroundBrush = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hForegroundBrush - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hGlyphRun != 0 )
                                    {
                                        data.hGlyphRun = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hGlyphRun - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawDrawing:
                                {
                                    MILCMD_DRAW_DRAWING data = *(MILCMD_DRAW_DRAWING*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hDrawing != 0 )
                                    {
                                        data.hDrawing = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hDrawing - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawVideo:
                                {
                                    MILCMD_DRAW_VIDEO data = *(MILCMD_DRAW_VIDEO*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hPlayer != 0 )
                                    {
                                        data.hPlayer = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPlayer - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        40 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilDrawVideoAnimate:
                                {
                                    MILCMD_DRAW_VIDEO_ANIMATE data = *(MILCMD_DRAW_VIDEO_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hPlayer != 0)
                                    {
                                        data.hPlayer = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hPlayer - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hRectangleAnimations != 0)
                                    {
                                        data.hRectangleAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hRectangleAnimations - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        40 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushClip:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_CLIP data = *(MILCMD_PUSH_CLIP*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hClipGeometry != 0 )
                                    {
                                        data.hClipGeometry = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hClipGeometry - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacityMask:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_OPACITY_MASK data = *(MILCMD_PUSH_OPACITY_MASK*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hOpacityMask != 0 )
                                    {
                                        data.hOpacityMask = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hOpacityMask - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        24 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacity:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_OPACITY data = *(MILCMD_PUSH_OPACITY*)(pCur + sizeof(RecordHeader));

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacityAnimate:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_OPACITY_ANIMATE data = *(MILCMD_PUSH_OPACITY_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hOpacityAnimations != 0)
                                    {
                                        data.hOpacityAnimations = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hOpacityAnimations - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        16 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushTransform:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_TRANSFORM data = *(MILCMD_PUSH_TRANSFORM*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hTransform != 0 )
                                    {
                                        data.hTransform = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hTransform - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineSet:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_GUIDELINE_SET data = *(MILCMD_PUSH_GUIDELINE_SET*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hGuidelines != 0 )
                                    {
                                        data.hGuidelines = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hGuidelines - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineY1:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_GUIDELINE_Y1 data = *(MILCMD_PUSH_GUIDELINE_Y1*)(pCur + sizeof(RecordHeader));

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineY2:
                                {
                                    pushStack.Push(PushType.Other);
                                    MILCMD_PUSH_GUIDELINE_Y2 data = *(MILCMD_PUSH_GUIDELINE_Y2*)(pCur + sizeof(RecordHeader));

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        16 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPushEffect:
                                {
                                    pushStack.Push(PushType.BitmapEffect);
                                    pushedEffects++;
                                    MILCMD_PUSH_EFFECT data = *(MILCMD_PUSH_EFFECT*)(pCur + sizeof(RecordHeader));

                                    // Marshal the Handles for the dependents

                                    if ( data.hEffect != 0 )
                                    {
                                        data.hEffect = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hEffect - 1)]).GetHandle(channel));
                                    }

                                    if ( data.hEffectInput != 0 )
                                    {
                                        data.hEffectInput = (uint)(((DUCE.IResource)_dependentResources[ (int)( data.hEffectInput - 1)]).GetHandle(channel));
                                    }

                                    channel.AppendCommandData(
                                        (byte*)&data,
                                        8 /* codegen'ed size of this instruction struct */
                                        );
                                }
                                break;
                                case MILCMD.MilPop:
                                {
                                    if (pushStack.Pop() == PushType.BitmapEffect)
                                    {
                                        pushedEffects -= 1;
                                    }
                                    /* instruction size is zero, do nothing */
                                }
                                break;

                                default:
                                {
                                    Debug.Assert(false);
                                }
                                break;
                             }
                            pCur += pCurRecord->Size;
                        }
                    }
                }
                channel.EndCommand();
            }
        }

        /// <summary>
        /// DrawingContextWalk - Iterates this renderdata and call out to methods on the
        /// provided DrawingContext, passing the current values to their parameters.
        /// </summary>
        public void DrawingContextWalk(DrawingContextWalker ctx)
        {
            // We shouldn't have any dependent resources if _curOffset is 0
            // (_curOffset == 0) -> (renderData._dependentResources.Count == 0)
            Debug.Assert((_curOffset > 0) || (_dependentResources.Count == 0));

            // The buffer being null implies that _curOffset must be 0.
            // (_buffer == null) -> (_curOffset == 0)
            Debug.Assert((_buffer != null) || (_curOffset == 0));

            // The _curOffset must be less than the length, if there is a buffer.
            Debug.Assert((_buffer == null) || (_curOffset <= _buffer.Length));

            if (_curOffset > 0)
            {
                unsafe
                {
                    fixed (byte* pByte = this._buffer)
                    {
                        // This pointer points to the current read point in the
                        // instruction stream.
                        byte* pCur = pByte;

                        // This points to the first byte past the end of the
                        // instruction stream (i.e. when to stop)
                        byte* pEndOfInstructions = pByte + _curOffset;

                        // Iterate across the entire list of instructions, stopping at the
                        // end or when the DrawingContextWalker has signalled a stop.
                        while ((pCur < pEndOfInstructions) && !ctx.ShouldStopWalking)
                        {
                            RecordHeader* pCurRecord = (RecordHeader*)pCur;

                            switch (pCurRecord->Id)
                            {
                                case MILCMD.MilDrawLine:
                                {
                                    MILCMD_DRAW_LINE* data = (MILCMD_DRAW_LINE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawLine(
                                        (Pen)DependentLookup(data->hPen),
                                        data->point0,
                                        data->point1
                                        );
                                }
                                break;
                                case MILCMD.MilDrawLineAnimate:
                                {
                                    MILCMD_DRAW_LINE_ANIMATE* data = (MILCMD_DRAW_LINE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawLine(
                                        (data->hPen == 0) ? null : (Pen)DependentLookup(data->hPen),
                                        (data->hPoint0Animations == 0) ? data->point0 : ((PointAnimationClockResource)DependentLookup(data->hPoint0Animations)).CurrentValue,
                                        (data->hPoint1Animations == 0) ? data->point1 : ((PointAnimationClockResource)DependentLookup(data->hPoint1Animations)).CurrentValue
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRectangle:
                                {
                                    MILCMD_DRAW_RECTANGLE* data = (MILCMD_DRAW_RECTANGLE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRectangle(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->rectangle
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRectangleAnimate:
                                {
                                    MILCMD_DRAW_RECTANGLE_ANIMATE* data = (MILCMD_DRAW_RECTANGLE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRectangle(
                                        (data->hBrush == 0) ? null : (Brush)DependentLookup(data->hBrush),
                                        (data->hPen == 0) ? null : (Pen)DependentLookup(data->hPen),
                                        (data->hRectangleAnimations == 0) ? data->rectangle : ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).CurrentValue
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRoundedRectangle:
                                {
                                    MILCMD_DRAW_ROUNDED_RECTANGLE* data = (MILCMD_DRAW_ROUNDED_RECTANGLE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRoundedRectangle(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->rectangle,
                                        data->radiusX,
                                        data->radiusY
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRoundedRectangleAnimate:
                                {
                                    MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE* data = (MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRoundedRectangle(
                                        (data->hBrush == 0) ? null : (Brush)DependentLookup(data->hBrush),
                                        (data->hPen == 0) ? null : (Pen)DependentLookup(data->hPen),
                                        (data->hRectangleAnimations == 0) ? data->rectangle : ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).CurrentValue,
                                        (data->hRadiusXAnimations == 0) ? data->radiusX : ((DoubleAnimationClockResource)DependentLookup(data->hRadiusXAnimations)).CurrentValue,
                                        (data->hRadiusYAnimations == 0) ? data->radiusY : ((DoubleAnimationClockResource)DependentLookup(data->hRadiusYAnimations)).CurrentValue
                                        );
                                }
                                break;
                                case MILCMD.MilDrawEllipse:
                                {
                                    MILCMD_DRAW_ELLIPSE* data = (MILCMD_DRAW_ELLIPSE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawEllipse(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->center,
                                        data->radiusX,
                                        data->radiusY
                                        );
                                }
                                break;
                                case MILCMD.MilDrawEllipseAnimate:
                                {
                                    MILCMD_DRAW_ELLIPSE_ANIMATE* data = (MILCMD_DRAW_ELLIPSE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawEllipse(
                                        (data->hBrush == 0) ? null : (Brush)DependentLookup(data->hBrush),
                                        (data->hPen == 0) ? null : (Pen)DependentLookup(data->hPen),
                                        (data->hCenterAnimations == 0) ? data->center : ((PointAnimationClockResource)DependentLookup(data->hCenterAnimations)).CurrentValue,
                                        (data->hRadiusXAnimations == 0) ? data->radiusX : ((DoubleAnimationClockResource)DependentLookup(data->hRadiusXAnimations)).CurrentValue,
                                        (data->hRadiusYAnimations == 0) ? data->radiusY : ((DoubleAnimationClockResource)DependentLookup(data->hRadiusYAnimations)).CurrentValue
                                        );
                                }
                                break;
                                case MILCMD.MilDrawGeometry:
                                {
                                    MILCMD_DRAW_GEOMETRY* data = (MILCMD_DRAW_GEOMETRY*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawGeometry(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        (Geometry)DependentLookup(data->hGeometry)
                                        );
                                }
                                break;
                                case MILCMD.MilDrawImage:
                                {
                                    MILCMD_DRAW_IMAGE* data = (MILCMD_DRAW_IMAGE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawImage(
                                        (ImageSource)DependentLookup(data->hImageSource),
                                        data->rectangle
                                        );
                                }
                                break;
                                case MILCMD.MilDrawImageAnimate:
                                {
                                    MILCMD_DRAW_IMAGE_ANIMATE* data = (MILCMD_DRAW_IMAGE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawImage(
                                        (data->hImageSource == 0) ? null : (ImageSource)DependentLookup(data->hImageSource),
                                        (data->hRectangleAnimations == 0) ? data->rectangle : ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).CurrentValue
                                        );
                                }
                                break;
                                case MILCMD.MilDrawGlyphRun:
                                {
                                    MILCMD_DRAW_GLYPH_RUN* data = (MILCMD_DRAW_GLYPH_RUN*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawGlyphRun(
                                        (Brush)DependentLookup(data->hForegroundBrush),
                                        (GlyphRun)DependentLookup(data->hGlyphRun)
                                        );
                                }
                                break;
                                case MILCMD.MilDrawDrawing:
                                {
                                    MILCMD_DRAW_DRAWING* data = (MILCMD_DRAW_DRAWING*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawDrawing(
                                        (Drawing)DependentLookup(data->hDrawing)
                                        );
                                }
                                break;
                                case MILCMD.MilDrawVideo:
                                {
                                    MILCMD_DRAW_VIDEO* data = (MILCMD_DRAW_VIDEO*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawVideo(
                                        (MediaPlayer)DependentLookup(data->hPlayer),
                                        data->rectangle
                                        );
                                }
                                break;
                                case MILCMD.MilDrawVideoAnimate:
                                {
                                    MILCMD_DRAW_VIDEO_ANIMATE* data = (MILCMD_DRAW_VIDEO_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawVideo(
                                        (MediaPlayer)DependentLookup(data->hPlayer),
                                        (data->hRectangleAnimations == 0) ? data->rectangle : ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).CurrentValue
                                        );
                                }
                                break;
                                case MILCMD.MilPushClip:
                                {
                                    MILCMD_PUSH_CLIP* data = (MILCMD_PUSH_CLIP*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushClip(
                                        (Geometry)DependentLookup(data->hClipGeometry)
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacityMask:
                                {
                                    MILCMD_PUSH_OPACITY_MASK* data = (MILCMD_PUSH_OPACITY_MASK*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushOpacityMask(
                                        (Brush)DependentLookup(data->hOpacityMask)
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacity:
                                {
                                    MILCMD_PUSH_OPACITY* data = (MILCMD_PUSH_OPACITY*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushOpacity(
                                        data->opacity
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacityAnimate:
                                {
                                    MILCMD_PUSH_OPACITY_ANIMATE* data = (MILCMD_PUSH_OPACITY_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushOpacity(
                                        (data->hOpacityAnimations == 0) ? data->opacity : ((DoubleAnimationClockResource)DependentLookup(data->hOpacityAnimations)).CurrentValue
                                        );
                                }
                                break;
                                case MILCMD.MilPushTransform:
                                {
                                    MILCMD_PUSH_TRANSFORM* data = (MILCMD_PUSH_TRANSFORM*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushTransform(
                                        (Transform)DependentLookup(data->hTransform)
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineSet:
                                {
                                    MILCMD_PUSH_GUIDELINE_SET* data = (MILCMD_PUSH_GUIDELINE_SET*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushGuidelineSet(
                                        (GuidelineSet)DependentLookup(data->hGuidelines)
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineY1:
                                {
                                    MILCMD_PUSH_GUIDELINE_Y1* data = (MILCMD_PUSH_GUIDELINE_Y1*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushGuidelineY1(
                                        data->coordinate
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineY2:
                                {
                                    MILCMD_PUSH_GUIDELINE_Y2* data = (MILCMD_PUSH_GUIDELINE_Y2*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushGuidelineY2(
                                        data->leadingCoordinate,
                                        data->offsetToDrivenCoordinate
                                        );
                                }
                                break;
                                // Disable warning about obsolete method.  This code must remain active 
                                // until we can remove the public BitmapEffect APIs.
                                #pragma warning disable 0618
                                case MILCMD.MilPushEffect:
                                {
                                    MILCMD_PUSH_EFFECT* data = (MILCMD_PUSH_EFFECT*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushEffect(
                                        (BitmapEffect)DependentLookup(data->hEffect),
                                        (BitmapEffectInput)DependentLookup(data->hEffectInput)
                                        );
                                }
                                break;
                                #pragma warning restore 0618
                                case MILCMD.MilPop:
                                {
                                    MILCMD_POP* data = (MILCMD_POP*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.Pop(

                                        );
                                }
                                break;
                                default:
                                {
                                    Debug.Assert(false);
                                }
                                break;
                            }

                            pCur += pCurRecord->Size;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// BaseValueDrawingContextWalk - Iterates this renderdata and call out to methods on the
        /// provided DrawingContext, passing base values and animations to their parameters.
        /// </summary>
        public void BaseValueDrawingContextWalk(DrawingContextWalker ctx)
        {
            // We shouldn't have any dependent resources if _curOffset is 0
            // (_curOffset == 0) -> (renderData._dependentResources.Count == 0)
            Debug.Assert((_curOffset > 0) || (_dependentResources.Count == 0));

            // The buffer being null implies that _curOffset must be 0.
            // (_buffer == null) -> (_curOffset == 0)
            Debug.Assert((_buffer != null) || (_curOffset == 0));

            // The _curOffset must be less than the length, if there is a buffer.
            Debug.Assert((_buffer == null) || (_curOffset <= _buffer.Length));

            if (_curOffset > 0)
            {
                unsafe
                {
                    fixed (byte* pByte = this._buffer)
                    {
                        // This pointer points to the current read point in the
                        // instruction stream.
                        byte* pCur = pByte;

                        // This points to the first byte past the end of the
                        // instruction stream (i.e. when to stop)
                        byte* pEndOfInstructions = pByte + _curOffset;

                        // Iterate across the entire list of instructions, stopping at the
                        // end or when the DrawingContextWalker has signalled a stop.
                        while ((pCur < pEndOfInstructions) && !ctx.ShouldStopWalking)
                        {
                            RecordHeader* pCurRecord = (RecordHeader*)pCur;

                            switch (pCurRecord->Id)
                            {
                                case MILCMD.MilDrawLine:
                                {
                                    MILCMD_DRAW_LINE* data = (MILCMD_DRAW_LINE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawLine(
                                        (Pen)DependentLookup(data->hPen),
                                        data->point0,
                                        data->point1
                                        );
                                }
                                break;
                                case MILCMD.MilDrawLineAnimate:
                                {
                                    MILCMD_DRAW_LINE_ANIMATE* data = (MILCMD_DRAW_LINE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawLine(
                                        (Pen)DependentLookup(data->hPen),
                                        data->point0,
                                        ((PointAnimationClockResource)DependentLookup(data->hPoint0Animations)).AnimationClock,
                                        data->point1,
                                        ((PointAnimationClockResource)DependentLookup(data->hPoint1Animations)).AnimationClock
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRectangle:
                                {
                                    MILCMD_DRAW_RECTANGLE* data = (MILCMD_DRAW_RECTANGLE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRectangle(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->rectangle
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRectangleAnimate:
                                {
                                    MILCMD_DRAW_RECTANGLE_ANIMATE* data = (MILCMD_DRAW_RECTANGLE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRectangle(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->rectangle,
                                        ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).AnimationClock
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRoundedRectangle:
                                {
                                    MILCMD_DRAW_ROUNDED_RECTANGLE* data = (MILCMD_DRAW_ROUNDED_RECTANGLE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRoundedRectangle(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->rectangle,
                                        data->radiusX,
                                        data->radiusY
                                        );
                                }
                                break;
                                case MILCMD.MilDrawRoundedRectangleAnimate:
                                {
                                    MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE* data = (MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawRoundedRectangle(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->rectangle,
                                        ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).AnimationClock,
                                        data->radiusX,
                                        ((DoubleAnimationClockResource)DependentLookup(data->hRadiusXAnimations)).AnimationClock,
                                        data->radiusY,
                                        ((DoubleAnimationClockResource)DependentLookup(data->hRadiusYAnimations)).AnimationClock
                                        );
                                }
                                break;
                                case MILCMD.MilDrawEllipse:
                                {
                                    MILCMD_DRAW_ELLIPSE* data = (MILCMD_DRAW_ELLIPSE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawEllipse(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->center,
                                        data->radiusX,
                                        data->radiusY
                                        );
                                }
                                break;
                                case MILCMD.MilDrawEllipseAnimate:
                                {
                                    MILCMD_DRAW_ELLIPSE_ANIMATE* data = (MILCMD_DRAW_ELLIPSE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawEllipse(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        data->center,
                                        ((PointAnimationClockResource)DependentLookup(data->hCenterAnimations)).AnimationClock,
                                        data->radiusX,
                                        ((DoubleAnimationClockResource)DependentLookup(data->hRadiusXAnimations)).AnimationClock,
                                        data->radiusY,
                                        ((DoubleAnimationClockResource)DependentLookup(data->hRadiusYAnimations)).AnimationClock
                                        );
                                }
                                break;
                                case MILCMD.MilDrawGeometry:
                                {
                                    MILCMD_DRAW_GEOMETRY* data = (MILCMD_DRAW_GEOMETRY*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawGeometry(
                                        (Brush)DependentLookup(data->hBrush),
                                        (Pen)DependentLookup(data->hPen),
                                        (Geometry)DependentLookup(data->hGeometry)
                                        );
                                }
                                break;
                                case MILCMD.MilDrawImage:
                                {
                                    MILCMD_DRAW_IMAGE* data = (MILCMD_DRAW_IMAGE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawImage(
                                        (ImageSource)DependentLookup(data->hImageSource),
                                        data->rectangle
                                        );
                                }
                                break;
                                case MILCMD.MilDrawImageAnimate:
                                {
                                    MILCMD_DRAW_IMAGE_ANIMATE* data = (MILCMD_DRAW_IMAGE_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawImage(
                                        (ImageSource)DependentLookup(data->hImageSource),
                                        data->rectangle,
                                        ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).AnimationClock
                                        );
                                }
                                break;
                                case MILCMD.MilDrawGlyphRun:
                                {
                                    MILCMD_DRAW_GLYPH_RUN* data = (MILCMD_DRAW_GLYPH_RUN*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawGlyphRun(
                                        (Brush)DependentLookup(data->hForegroundBrush),
                                        (GlyphRun)DependentLookup(data->hGlyphRun)
                                        );
                                }
                                break;
                                case MILCMD.MilDrawDrawing:
                                {
                                    MILCMD_DRAW_DRAWING* data = (MILCMD_DRAW_DRAWING*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawDrawing(
                                        (Drawing)DependentLookup(data->hDrawing)
                                        );
                                }
                                break;
                                case MILCMD.MilDrawVideo:
                                {
                                    MILCMD_DRAW_VIDEO* data = (MILCMD_DRAW_VIDEO*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawVideo(
                                        (MediaPlayer)DependentLookup(data->hPlayer),
                                        data->rectangle
                                        );
                                }
                                break;
                                case MILCMD.MilDrawVideoAnimate:
                                {
                                    MILCMD_DRAW_VIDEO_ANIMATE* data = (MILCMD_DRAW_VIDEO_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.DrawVideo(
                                        (MediaPlayer)DependentLookup(data->hPlayer),
                                        data->rectangle,
                                        ((RectAnimationClockResource)DependentLookup(data->hRectangleAnimations)).AnimationClock
                                        );
                                }
                                break;
                                case MILCMD.MilPushClip:
                                {
                                    MILCMD_PUSH_CLIP* data = (MILCMD_PUSH_CLIP*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushClip(
                                        (Geometry)DependentLookup(data->hClipGeometry)
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacityMask:
                                {
                                    MILCMD_PUSH_OPACITY_MASK* data = (MILCMD_PUSH_OPACITY_MASK*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushOpacityMask(
                                        (Brush)DependentLookup(data->hOpacityMask)
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacity:
                                {
                                    MILCMD_PUSH_OPACITY* data = (MILCMD_PUSH_OPACITY*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushOpacity(
                                        data->opacity
                                        );
                                }
                                break;
                                case MILCMD.MilPushOpacityAnimate:
                                {
                                    MILCMD_PUSH_OPACITY_ANIMATE* data = (MILCMD_PUSH_OPACITY_ANIMATE*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushOpacity(
                                        data->opacity,
                                        ((DoubleAnimationClockResource)DependentLookup(data->hOpacityAnimations)).AnimationClock
                                        );
                                }
                                break;
                                case MILCMD.MilPushTransform:
                                {
                                    MILCMD_PUSH_TRANSFORM* data = (MILCMD_PUSH_TRANSFORM*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushTransform(
                                        (Transform)DependentLookup(data->hTransform)
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineSet:
                                {
                                    MILCMD_PUSH_GUIDELINE_SET* data = (MILCMD_PUSH_GUIDELINE_SET*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushGuidelineSet(
                                        (GuidelineSet)DependentLookup(data->hGuidelines)
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineY1:
                                {
                                    MILCMD_PUSH_GUIDELINE_Y1* data = (MILCMD_PUSH_GUIDELINE_Y1*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushGuidelineY1(
                                        data->coordinate
                                        );
                                }
                                break;
                                case MILCMD.MilPushGuidelineY2:
                                {
                                    MILCMD_PUSH_GUIDELINE_Y2* data = (MILCMD_PUSH_GUIDELINE_Y2*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushGuidelineY2(
                                        data->leadingCoordinate,
                                        data->offsetToDrivenCoordinate
                                        );
                                }
                                break;
                                // Disable warning about obsolete method.  This code must remain active 
                                // until we can remove the public BitmapEffect APIs.
                                #pragma warning disable 0618
                                case MILCMD.MilPushEffect:
                                {
                                    MILCMD_PUSH_EFFECT* data = (MILCMD_PUSH_EFFECT*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.PushEffect(
                                        (BitmapEffect)DependentLookup(data->hEffect),
                                        (BitmapEffectInput)DependentLookup(data->hEffectInput)
                                        );
                                }
                                break;
                                #pragma warning restore 0618
                                case MILCMD.MilPop:
                                {
                                    MILCMD_POP* data = (MILCMD_POP*)(pCur + sizeof(RecordHeader));

                                    // Retrieve the resources for the dependents and call the context.
                                    ctx.Pop(

                                        );
                                }
                                break;
                                default:
                                {
                                    Debug.Assert(false);
                                }
                                break;
                            }

                            pCur += pCurRecord->Size;
                        }
                    }
                }
            }
        }
    }
}
