// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Visual flags.
//

namespace System.Windows.Media
{
    /// <summary>
    /// Visual flags.
    /// </summary>
    [System.Flags]
    internal enum VisualFlags : uint
    {
        /// <summary>
        /// No flags are set for this visual.
        /// </summary>
        None                                = 0x0,

        // IsSubtreeDirtyForPrecompute indicates that at least one Visual in the sub-graph of this Visual needs
        // a bounding box update.
        IsSubtreeDirtyForPrecompute                     = 0x00000001,

        // Should post render indicates that this is a root visual and therefore we need to indicate that this
        // visual tree needs to be re-rendered. Today we are doing this by posting a render queue item.
        ShouldPostRender                                = 0x00000002,

        // Needs documentation
        IsUIElement                                     = 0x00000004,

        // For UIElement -- It's in VisualFlags so that it can be propagated through the
        // Visual subtree without casting.
        IsLayoutSuspended                               = 0x00000008,

        // Are we in the process of iterating the visual children. 
        // This flag is set during a descendents walk, for property invalidation.
        IsVisualChildrenIterationInProgress             = 0x00000010,

        // Used on ModelVisual3D to signify that its content bounds
        // cache is valid.
        //
        // Stop over-invalidating _bboxSubgraph
        //
        // We use this flag to maintain a separate cache of a ModelVisual3D’s content
        // bounds.  A better solution that would be both a 2D and 3D win would be to
        // stop invalidating _bboxSubgraph when a visual’s transform changes.
        // 
        Are3DContentBoundsValid                         = 0x00000020,

        // FindCommonAncestor is used to find the common ancestor of a Visual.
        FindCommonAncestor                              = 0x00000040,

        // IsLayoutIslandRoot indicates that this Visual is a root of Element Layout Island.
        IsLayoutIslandRoot                              = 0x00000080,

        // UseLayoutRounding indicates that layout rounding should be applied during Measure/Arrange for this UIElement.
        UseLayoutRounding                               = 0x00000100,

        // These bits together make up UIElement.VisibilityCache
        VisibilityCache_Visible                         = 0x00000200,
        VisibilityCache_TakesSpace                      = 0x00000400,

        // Indicates that a given node is registered for AncestorChanged.
        RegisteredForAncestorChanged                    = 0x00000800,

        // Indicates that a node below this node is registered for AncestorChanged.
        SubTreeHoldsAncestorChanged                     = 0x00001000,

        // Indicates that this node is used by a cyclic brush
        NodeIsCyclicBrushRoot                           = 0x00002000,

        // Indicates that this node has an Effect
        NodeHasEffect                                   = 0x00004000,

        // Indicates that this node is of Viewport3DVisual class.
        IsViewport3DVisual                              = 0x00008000,

        // Used to discover cycles in VisualBrush scenarios.
        ReentrancyFlag                                  = 0x00010000,

        // Indicates if the visual has any children. Avoids calls to visualchildrencount while checking for presence of children.
        HasChildren                                     = 0x00020000,

        // Controls if the bitmap effect emulation layer is enabled. 
        BitmapEffectEmulationDisabled                   = 0x00040000,

        // These two DPI flags are used to determine the DPI value of a Visual.
        // Combination of these two flags point to 4 possible choices (DpiScaleFlag1 being the LSB) : Choice 0-2 directly 
        // represent the index in the static array (in UIElement) on which DPI is stored. Choice 3 indicates that the index is stored 
        // in an uncommon field on the Visual.
        DpiScaleFlag1                                   = 0x00080000,

        DpiScaleFlag2                                   = 0x00100000,

        //TreeLevel counter - occupies 11 bits. 
        //NOTE: The location of these bits in this ulong should be synchronized with 
        //Visual.TreeLevel property getter/setter.
        TreeLevelBit0                                   = 0x00200000,
        TreeLevelBit1                                   = 0x00400000,
        TreeLevelBit2                                   = 0x00800000,
        TreeLevelBit3                                   = 0x01000000,
        TreeLevelBit4                                   = 0x02000000,
        TreeLevelBit5                                   = 0x04000000,
        TreeLevelBit6                                   = 0x08000000,
        TreeLevelBit7                                   = 0x10000000,
        TreeLevelBit8                                   = 0x20000000,
        TreeLevelBit9                                   = 0x40000000,
        TreeLevelBit10                                  = 0x80000000,
    }
}


