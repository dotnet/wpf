// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Interop;

namespace System.Windows;

public sealed class MessageBoxTests
{
    /*
     * MessageBoxButton range is from 0x00000000 to 0x00000006
     * Values outside are illegal.
     */
    public static TheoryData<MessageBoxButton> InvalidMessageBoxButtonData =>
        new TheoryData<MessageBoxButton>
        {
            unchecked((MessageBoxButton)((0xFFFFFFFF))),
            (MessageBoxButton)(0x00000007)
        };

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxButtonData))]
    public void Show_InvalidMessageBoxButton_ThrowsInvalidEnumArgumentException(MessageBoxButton invalidButton)
    {
        InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show("Test Message", "Test Caption", invalidButton));
        Assert.Contains("button", exception.Message);
    }

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxButtonData))]
    public void Show_WithOwner_InvalidMessageBoxButton_ThrowsInvalidEnumArgumentException(MessageBoxButton invalidButton)
    {
        InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(new Window(), "Test Message", "Test Caption", invalidButton));
        Assert.Contains("button", exception.Message);
    }

    /*
     * MessageBoxResult range is from 0 to 11 with 8 and 9 not used.
     * Values outside are illegal as well as values 8 and 9.
     */
    public static TheoryData<MessageBoxResult> InvalidMessageBoxResultData =>
        new TheoryData<MessageBoxResult>
        {
            (MessageBoxResult)(-1), // Just outside enum range
            (MessageBoxResult)8, // Not defined - hole in enum range
            (MessageBoxResult)9, // Not defined - hole in enum range
            (MessageBoxResult)12 // Just outside enum range
        };

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxResultData))]
    public void Show_InvalidMessageBoxResult_ThrowsInvalidEnumArgumentException(MessageBoxResult invalidResult)
    {
        InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show("Test Message", "Test Caption", MessageBoxButton.OK, MessageBoxImage.None, invalidResult));
        Assert.Contains("defaultResult", exception.Message);
    }

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxResultData))]
    public void Show_WithOwner_InvalidMessageBoxResult_ThrowsInvalidEnumArgumentException(MessageBoxResult invalidResult)
    {
        InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(new Window(), "Test Message", "Test Caption", MessageBoxButton.OK, MessageBoxImage.None, invalidResult));
        Assert.Contains("defaultResult", exception.Message);
    }

    /*
     * MessageBoxImage values are 0x00000000, 0x00000010, 0x00000020, 0x00000030, 0x00000040
     * Any other value is illegal.
     */
    public static TheoryData<MessageBoxImage> InvalidMessageBoxImageData =>
        new TheoryData<MessageBoxImage>
        {
            unchecked((MessageBoxImage)(0xFFFFFFFF)),
            unchecked((MessageBoxImage)(0x00000001)),
            unchecked((MessageBoxImage)(0x0000000F)),
            unchecked((MessageBoxImage)(0x00000011)),
            unchecked((MessageBoxImage)(0x0000001F)),
            unchecked((MessageBoxImage)(0x00000021)),
            unchecked((MessageBoxImage)(0x0000002F)),
            unchecked((MessageBoxImage)(0x00000031)),
            unchecked((MessageBoxImage)(0x0000003F)),
            unchecked((MessageBoxImage)(0x00000041)),
            unchecked((MessageBoxImage)(0x0000004F)),
        };

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxImageData))]
    public void Show_InvalidMessageBoxImage_ThrowsInvalidEnumArgumentException(MessageBoxImage invalidImage)
    {
        InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show("Test Message", "Test Caption", MessageBoxButton.OK, invalidImage, MessageBoxResult.None));
        Assert.Contains("icon", exception.Message);
    }

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxImageData))]
    public void Show_WithOwner_InvalidMessageBoxImage_ThrowsInvalidEnumArgumentException(MessageBoxImage invalidImage)
    {
        InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(new Window(), "Test Message", "Test Caption", MessageBoxButton.OK, invalidImage, MessageBoxResult.None));
        Assert.Contains("icon", exception.Message);
    }

    /*
     * MessageBoxOptions values are 0x00000000, 0x00020000, 0x00080000, 0x00100000, 0x00200000
     */
    public static TheoryData<MessageBoxOptions> InvalidMessageBoxOptionsData =>
        new TheoryData<MessageBoxOptions>
        {
            unchecked((MessageBoxOptions)(0xFFFFFFFF)),
            unchecked((MessageBoxOptions)(0x00000001)),
            unchecked((MessageBoxOptions)(0x0001FFFF)),
            unchecked((MessageBoxOptions)(0x00020001)),
            unchecked((MessageBoxOptions)(0x0007FFFF)),
            unchecked((MessageBoxOptions)(0x00080001)),
            unchecked((MessageBoxOptions)(0x000FFFFF)),
            unchecked((MessageBoxOptions)(0x00100001)),
            unchecked((MessageBoxOptions)(0x001FFFFF)),
            unchecked((MessageBoxOptions)(0x00200001))
        };

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxOptionsData))]
    public void Show_InvalidMessageBoxOptions_ThrowsInvalidEnumArgumentException(MessageBoxOptions invalidOptions)
    {
        InvalidEnumArgumentException exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show("Test Message", "Test Caption", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, invalidOptions));
        Assert.Contains("options", exception.Message);
    }

    [WpfTheory]
    [MemberData(nameof(InvalidMessageBoxOptionsData))]
    public void Show_WithOwner_InvalidMessageBoxOptions_ThrowsInvalidEnumArgumentException(MessageBoxOptions invalidOptions)
    {
        var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(new Window(), "Test Message", "Test Caption", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, invalidOptions));
        Assert.Contains("options", exception.Message);
    }

    /*
     * For either of these two MessageBoxOptions values, MessageBox.ShowCore throws an ArgumentException if the
     * owner window handle is non-zero (!= IntPtr.Zero).
     */
    [WpfTheory]
    [InlineData(MessageBoxOptions.ServiceNotification)]
    [InlineData(MessageBoxOptions.DefaultDesktopOnly)]
    [InlineData(MessageBoxOptions.DefaultDesktopOnly | MessageBoxOptions.RightAlign)]
    [InlineData(MessageBoxOptions.ServiceNotification | MessageBoxOptions.RightAlign)]
    public void Show_WithOwner_WithSpecificMessageBoxOptions_ThrowsArgumentException(MessageBoxOptions messageBoxOptions)
    {
        // Arrange
        Window owner = new Window();
        WindowInteropHelper windowInteropHelper = new WindowInteropHelper(owner);
        windowInteropHelper.EnsureHandle();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            MessageBox.Show(owner, "Test Message", "Test Caption", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, messageBoxOptions));
    }
}
