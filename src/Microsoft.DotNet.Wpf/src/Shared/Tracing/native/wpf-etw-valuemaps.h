// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

// mc.exe doesn't generate enums for valueMap types, so we
// manually define these enumerations here.

enum DispatcherPriority
{
    DispatcherPriority_Inactive = 0,
    DispatcherPriority_SystemIdle = 1,
    DispatcherPriority_ApplicationIdle = 2,
    DispatcherPriority_ContextIdlle = 3,
    DispatcherPriority_Background = 4,
    DispatcherPriority_Input = 5,
    DispatcherPriority_Loaded = 6,
    DispatcherPriority_Render = 7,
    DispatcherPriority_DataBind = 8,
    DispatcherPriority_Normal = 9,
    DispatcherPriority_Send = 10,
};

enum IntermediateRenderTargetReason
{
    IRT_Clip = 0,
    IRT_ClipAndOpacity = 1,
    IRT_Effect = 2,
    IRT_Opacity = 3,
    IRT_OpacityMask = 4,
    IRT_OpacityMask_Brush_Realization = 5,
    IRT_ShaderEffect_Input = 6,
    IRT_Software_Only_Effects = 7,
    IRT_TileBrush = 8,
};

enum LayoutSourceMap
{
    Layout_LayoutManager = 0,
    Layout_HwndSource_SetLayoutSize = 1,
    Layout_HwndSource_WM_SIZE = 2,
};

enum UnexpectedSoftwareFallback
{
    UnexpectedSWFallback_NoHardwareAvailable = 0,
    UnexpectedSWFallback_ResizeFailed = 1,
    UnexpectedSWFallback_OutOfVideoMemory = 2,
    UnexpectedSWFallback_UnexpectedPrimitiveFallback = 3,
};
