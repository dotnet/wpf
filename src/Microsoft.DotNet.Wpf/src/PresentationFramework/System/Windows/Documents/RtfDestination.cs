// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Define the rtf destination to parse rtf conttent on RtfToXaml 
//              converter.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// RTF parsing destination
    /// </summary>
    internal enum RtfDestination
    {
        DestNormal,
        DestColorTable,
        DestFontTable,
        DestFontName,
        DestListTable,
        DestListOverrideTable,
        DestList,
        DestListLevel,
        DestListOverride,
        DestListPicture,
        DestListText,
        DestUPR,
        DestField,
        DestFieldInstruction,
        DestFieldResult,
        DestFieldPrivate,
        DestShape,
        DestShapeInstruction,
        DestShapeResult,
        DestShapePicture,
        DestNoneShapePicture,
        DestPicture,
        DestPN,
        DestUnknown
    };
}
