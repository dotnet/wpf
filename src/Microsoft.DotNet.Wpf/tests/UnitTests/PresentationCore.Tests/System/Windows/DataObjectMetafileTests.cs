// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Imaging;

namespace System.Windows;

public sealed class DataObjectMetafileTests
{
    // Defines the managed DataObject auto-conversion contract independently from the system clipboard.
    [WpfTheory]
    [InlineData("EnhancedMetafile", "System.Drawing.Imaging.Metafile")]
    [InlineData("System.Drawing.Imaging.Metafile", "EnhancedMetafile")]
    public void GetData_MetafileMappedFormat_RespectsAutoConvert(
        string sourceFormat,
        string mappedFormat)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(sourceFormat, source, autoConvert: true);

        Assert.True(dataObject.GetDataPresent(sourceFormat, autoConvert: false));
        Assert.False(dataObject.GetDataPresent(mappedFormat, autoConvert: false));
        Assert.True(dataObject.GetDataPresent(mappedFormat, autoConvert: true));
        Assert.Same(source, dataObject.GetData(sourceFormat, autoConvert: false));
        Assert.Null(dataObject.GetData(mappedFormat, autoConvert: false));
        Assert.Same(source, dataObject.GetData(mappedFormat, autoConvert: true));
        Assert.Contains(sourceFormat, dataObject.GetFormats(autoConvert: false));
        Assert.Contains(mappedFormat, dataObject.GetFormats(autoConvert: true));
        EmfTestData.AssertValid(source);
    }
}
