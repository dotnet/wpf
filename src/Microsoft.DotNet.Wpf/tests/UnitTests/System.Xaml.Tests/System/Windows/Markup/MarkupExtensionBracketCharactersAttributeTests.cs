// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Windows.Markup.Tests;

public class MarkupExtensionBracketCharactersAttributeTests
{
    [Theory]
    [InlineData('\0', '\0')]
    [InlineData('a', 'b')]
    public void Ctor_Char_Char(char openingBracket, char closingBracket)
    {
        var attribute = new MarkupExtensionBracketCharactersAttribute(openingBracket, closingBracket);
        Assert.Equal(openingBracket, attribute.OpeningBracket);
        Assert.Equal(closingBracket, attribute.ClosingBracket);
    }
}
