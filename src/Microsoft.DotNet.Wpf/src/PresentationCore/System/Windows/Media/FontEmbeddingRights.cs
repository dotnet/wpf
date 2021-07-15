// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Definition of FontEmbeddingRight enum. The FontEmbeddingRight enum is used to describe
// font embedding permissions specified in an OpenType font file.
//
//


namespace System.Windows.Media
{
    /// <summary>
    /// The FontEmbeddingRight enumeration is used to describe font embedding permissions
    /// specified in an OpenType font file.
    /// </summary>
    public enum FontEmbeddingRight
    {
        /// <summary>
        /// Fonts with this setting indicate that they may be embedded and permanently
        /// installed on the remote system by an application. The user of the remote system acquires the identical rights,
        /// obligations and licenses for that font as the original purchaser of the font,
        /// and is subject to the same end-user license agreement, copyright, design patent,
        /// and/or trademark as was the original purchaser.
        /// </summary>
        Installable = 0,

        /// <summary>
        /// Fonts with this setting indicate that they may be embedded and permanently
        /// installed on the remote system by an application.
        /// They may not be subsetted prior to embedding.
        /// </summary>
        InstallableButNoSubsetting,

        /// <summary>
        /// Fonts with this setting indicate that they may be embedded and permanently
        /// installed on the remote system by an application.
        /// Only bitmaps contained in the fonts may be embedded. No outline data may be embedded.
        /// </summary>
        InstallableButWithBitmapsOnly,

        /// <summary>
        /// Fonts with this setting indicate that they may be embedded and permanently
        /// installed on the remote system by an application.
        /// They may not be subsetted prior to embedding.
        /// Only bitmaps contained in the fonts may be embedded. No outline data may be embedded.
        /// </summary>
        InstallableButNoSubsettingAndWithBitmapsOnly,

        /// <summary>
        /// Fonts with this setting must not be modified, embedded or exchanged in any manner
        /// without first obtaining permission of the legal owner. 
        /// </summary>
        RestrictedLicense,

        /// <summary>
        /// The font may be embedded and temporarily loaded on the remote system.
        /// Documents containing the font must be opened in a read-only mode.
        /// </summary>
        PreviewAndPrint,

        /// <summary>
        /// The font may be embedded and temporarily loaded on the remote system.
        /// Documents containing the font must be opened in a read-only mode.
        /// The font may not be subsetted prior to embedding.
        /// </summary>
        PreviewAndPrintButNoSubsetting,

        /// <summary>
        /// The font may be embedded and temporarily loaded on the remote system.
        /// Documents containing the font must be opened in a read-only mode.
        /// Only bitmaps contained in the font may be embedded. No outline data may be embedded. 
        /// </summary>
        PreviewAndPrintButWithBitmapsOnly,

        /// <summary>
        /// The font may be embedded and temporarily loaded on the remote system.
        /// Documents containing the font must be opened in a read-only mode.
        /// The font may not be subsetted prior to embedding.
        /// Only bitmaps contained in the font may be embedded. No outline data may be embedded.
        /// </summary>
        PreviewAndPrintButNoSubsettingAndWithBitmapsOnly,

        /// <summary>
        /// The font may be embedded but must only be installed temporarily on other systems.
        /// In contrast to the PreviewAndPrint setting, documents containing Editable fonts may be opened for reading,
        /// editing is permitted, and changes may be saved. 
        /// </summary>
        Editable,

        /// <summary>
        /// The font may be embedded but must only be installed temporarily on other systems.
        /// Documents containing the font may be opened for reading, editing is permitted, and changes may be saved. 
        /// The font may not be subsetted prior to embedding.
        /// </summary>
        EditableButNoSubsetting,

        /// <summary>
        /// The font may be embedded but must only be installed temporarily on other systems.
        /// Documents containing the font may be opened for reading, editing is permitted, and changes may be saved. 
        /// Only bitmaps contained in the font may be embedded. No outline data may be embedded.
        /// </summary>
        EditableButWithBitmapsOnly,

        /// <summary>
        /// The font may be embedded but must only be installed temporarily on other systems.
        /// Documents containing the font may be opened for reading, editing is permitted, and changes may be saved. 
        /// The font may not be subsetted prior to embedding.
        /// Only bitmaps contained in the font may be embedded. No outline data may be embedded.
        /// </summary>
        EditableButNoSubsettingAndWithBitmapsOnly
    }
}

