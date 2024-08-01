// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;
using System.Formats.Nrbf;

namespace PresentationCore.Tests.BinaryFormat;

public class SystemDrawingTests
{
    [Fact]
    public void PointF_Parse()
    {
        PointF input = new(1.5f, 2.1f);
        SerializationRecord record = input.SerializeAndParse();

        Assert.True(record.TryGetPointF(out object? read));

        Assert.Equal(input.X, ((PointF)read!).X);
        Assert.Equal(input.Y, ((PointF)read).Y);
    }

    [Fact]
    public void RectangleF_Parse()
    {
        RectangleF input = new(1.5f, 2.1f, 100.7f, 15.9f);
        SerializationRecord record = input.SerializeAndParse();

        Assert.True(record.TryGetRectangleF(out object? read));

        Assert.Equal(input.X, ((RectangleF)read!).X);
        Assert.Equal(input.Y, ((RectangleF)read).Y);
        Assert.Equal(input.Width, ((RectangleF)read).Width);
        Assert.Equal(input.Height, ((RectangleF)read).Height);
    }

    public static TheoryData<object> SystemDrawing_TestData => new()
    {
        new PointF(),
        new RectangleF()
    };
}
