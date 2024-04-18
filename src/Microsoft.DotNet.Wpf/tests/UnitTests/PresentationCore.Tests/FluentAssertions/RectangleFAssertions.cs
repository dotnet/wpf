// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace PresentationCore.Tests.FluentAssertions;
public class RectangleFAssertions(RectangleF value)
{
    public RectangleF Subject { get; } = value;
}