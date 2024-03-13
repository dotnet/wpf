// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace PresentationCore.Tests.FluentAssertions;

public class PointFAssertions(PointF value)
{
    public PointF Subject { get; } = value;
}