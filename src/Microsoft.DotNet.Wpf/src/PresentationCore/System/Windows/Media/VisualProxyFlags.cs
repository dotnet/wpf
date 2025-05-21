// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Per-channel flags for Visuals.
//

namespace System.Windows.Media
{
    /// <summary>
    /// Per-channel flags for Visuals.
    /// </summary>
    [System.Flags]
    internal enum VisualProxyFlags : uint
    {
        /// <summary>
        /// No flags are set for this visual.
        /// </summary>
        None                                = 0x0,

        // IsSubtreeDirtyForRender indicates that at least one Visual 
        // in the sub-graph of this Visual needs to be re-rendered.
        IsSubtreeDirtyForRender             = 0x1,

        // This flag must be set to true when the transform property 
        // of a Visual is set. It ensures that the new transform is 
        // attached to the visual resource.
        IsTransformDirty                    = 0x2,

        // This flag must be set to true when the clip property of a Visual is set. It ensures that the
        // new clip is attached to the visual resource.
        IsClipDirty                         = 0x4,

        // This flag indicates that the content of the visual resource needs to
        // be updated. This is done by calling the virtual method RenderContent.
        // When this flag is set usually the IsSubtreeDirtyForRender is propagated.
        IsContentDirty                      = 0x8,

        // This flag indicates that the opacity needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.
        IsOpacityDirty                      = 0x10,

        // This flag must be set to true when the opacity mask property of 
        // a Visual is set. It ensures that the new opacity mask is attached 
        // to the visual resource.            
        IsOpacityMaskDirty                  = 0x20,           

        // Indicates that the offset has changed and we need to update the visual resource.
        IsOffsetDirty                       = 0x40,

        // This flag indicates that the ClearTypeHint needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.            
        IsClearTypeHintDirty                = 0x80,

        // Indicates that at least one of (GuidelinesXField, GuidelinesYField)
        // has been changed, and visual resource needs corresponding update.
        IsGuidelineCollectionDirty          = 0x100,

        // This flag indicates that the EdgeMode needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.
        IsEdgeModeDirty                     = 0x200,

        // Needs documentation
        IsContentConnected                  = 0x400,

        // Specifies whether this visual has child visual resource with content connected
        // (currently HostVisual may have one). This value is used to properly manage child
        // visual node insertion.
        IsContentNodeConnected              = 0x800,

        // Indicates that a node is connected to parent in the compositor.
        IsConnectedToParent                 = 0x1000,

        // This flag must be set to true when the Camera property of a Viewport3DVisual 
        // is set. It ensures that the new camera is attached to the viewport resource.
        Viewport3DVisual_IsCameraDirty      = 0x2000,

        // This flag must be set to true when the Viewport property of a Viewport3DVisual 
        // is set. It ensures that the new viewport is attached to the viewport resource.
        Viewport3DVisual_IsViewportDirty    = 0x4000,

        // This flag indicates that the ScaleOption needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.
        IsBitmapScalingModeDirty            = 0x8000,

        // This flag must be set to true if we are in the process of deleting the resource
        // on the channel. If the resource is deleted from the channel, then we don't need to
        // reset the flag (since the flag for this visual is per channel) but if we only 
        // decreased the ref count then we need to set it to false.
        //
        // This flag is needed for cyclic resources since they will cause reentrancy.
        // Even though we will only send one release command to the compositor if the
        // flag is set, that is fine because the compositor will do its own cleanup
        // if it encounters cyclic resources.
        IsDeleteResourceInProgress          = 0x10000,

        // This flag indicates that the visual's children have been reordered and we need to
        // update the visual resource.
        IsChildrenZOrderDirty               = 0x20000,

        // This flag must be set to true when the Effect property 
        // of a Visual is set. It ensures that the new Effect is 
        // attached to the visual resource.
        IsEffectDirty                       = 0x40000,

        // This flag indicates that the CacheMode needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.
        IsCacheModeDirty                    = 0x80000,

        // Indicates when the ScrollableAreaClip needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.
        IsScrollableAreaClipDirty           = 0x100000,

        // Indicates when the TextRenderingMode needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.
        IsTextRenderingModeDirty            = 0x200000,

        // Indicates when the TextHintingMode needs to be updated on the visual
        // resource. When this flag is set, IsSubtreeDirtyForRender is propagated.
        IsTextHintingModeDirty              = 0x400000,
    }
}


