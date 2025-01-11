namespace System.Windows.Media;

public class KnownColorsTests
{
    [Theory]
    // Supported values.
    [InlineData(KnownColor.AliceBlue, "#FFF0F8FF")]
    [InlineData(KnownColor.AliceBlue, " #FFF0F8FF")]
    [InlineData(KnownColor.AliceBlue, " #FFF0F8FF ")]
    [InlineData(KnownColor.AliceBlue, "#FFF0F8FF ")]
    // Unsupported values.
    [InlineData(KnownColor.UnknownColor, "")]
    [InlineData(KnownColor.UnknownColor, " ")]
    [InlineData(KnownColor.UnknownColor, "#020B37EF")] // Random ARGB that is not a known color.
    [InlineData(KnownColor.UnknownColor, "# FFF0F8FF")]
    public void ArgbStringToKnownColor_ReturnsExpected(object expected, string? argbString)
    {
        Assert.Equal((KnownColor)expected, KnownColors.ArgbStringToKnownColor(argbString));
    }

    [Fact]
    public void ArgbStringToKnownColor_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => KnownColors.ArgbStringToKnownColor(argbString: null));
    }
}
