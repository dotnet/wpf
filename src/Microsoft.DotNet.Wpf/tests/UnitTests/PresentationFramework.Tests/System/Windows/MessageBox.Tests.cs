// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Threading;

namespace System.Windows;

public sealed class MessageBoxTests
{
    [Fact]
    public void Show_InvalidMessageBoxButton_ThrowsInvalidEnumArgumentException()
    {
        // Arrange
        string messageBoxText = "Test Message";
        string caption = "Test Caption";
        MessageBoxButton invalidButton = (MessageBoxButton)999; // Invalid value

        // Act & Assert
        var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(messageBoxText, caption, invalidButton));
        Assert.Contains("button", exception.Message);
    }

    [Fact]
    public void Show_InvalidMessageBoxResult_ThrowsInvalidEnumArgumentException()
    {
        // Arrange
        string messageBoxText = "Test Message";
        string caption = "Test Caption";
        MessageBoxButton button = MessageBoxButton.OK;
        MessageBoxResult invalidResult = (MessageBoxResult)999; // Invalid value

        // Act & Assert
        var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(messageBoxText, caption, button, MessageBoxImage.None, invalidResult));
        Assert.Contains("defaultResult", exception.Message);
    }

    [Fact]
    public void Show_InvalidMessageBoxImage_ThrowsInvalidEnumArgumentException()
    {
        // Arrange
        string messageBoxText = "Test Message";
        string caption = "Test Caption";
        MessageBoxButton button = MessageBoxButton.OK;
        MessageBoxResult defaultResult = MessageBoxResult.None;
        MessageBoxImage invalidImage = (MessageBoxImage)999; // Invalid value

        // Act & Assert
        var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(messageBoxText, caption, button, invalidImage, defaultResult));
        Assert.Contains("icon", exception.Message);
    }

    [Fact]
    public void Show_InvalidMessageBoxOptions_ThrowsInvalidEnumArgumentException()
    {
        // Arrange
        string messageBoxText = "Test Message";
        string caption = "Test Caption";
        MessageBoxButton button = MessageBoxButton.OK;
        MessageBoxResult defaultResult = MessageBoxResult.None;
        MessageBoxImage image = MessageBoxImage.None;
        MessageBoxOptions invalidOptions = (MessageBoxOptions)999; // Invalid value

        // Act & Assert
        var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            MessageBox.Show(messageBoxText, caption, button, image, defaultResult, invalidOptions));
        Assert.Contains("options", exception.Message);
    }

    [Fact]
    public void Show_WithOwner_InvalidMessageBoxButton_ThrowsInvalidEnumArgumentException()
    {
        // Avoid annoying "The calling thread must be STA, because many UI components require this." exception
        Thread thread = new Thread(() =>
        {
            // Arrange
            var owner = new Window();
            string messageBoxText = "Test Message";
            string caption = "Test Caption";
            MessageBoxButton invalidButton = (MessageBoxButton)999; // Invalid value

            // Act & Assert
            var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
                MessageBox.Show(owner, messageBoxText, caption, invalidButton));
            Assert.Contains("button", exception.Message);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void Show_WithOwner_InvalidMessageBoxResult_ThrowsInvalidEnumArgumentException()
    {
        // Avoid annoying "The calling thread must be STA, because many UI components require this." exception
        Thread thread = new Thread(() =>
        {
            // Arrange
            var owner = new Window();
            string messageBoxText = "Test Message";
            string caption = "Test Caption";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxResult invalidResult = (MessageBoxResult)999; // Invalid value
            // Act & Assert
            var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
                MessageBox.Show(owner, messageBoxText, caption, button, MessageBoxImage.None, invalidResult));
            Assert.Contains("defaultResult", exception.Message);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void Show_WithOwner_InvalidMessageBoxImage_ThrowsInvalidEnumArgumentException()
    {
        // Avoid annoying "The calling thread must be STA, because many UI components require this." exception
        Thread thread = new Thread(() =>
        {
            // Arrange
            var owner = new Window();
            string messageBoxText = "Test Message";
            string caption = "Test Caption";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxResult defaultResult = MessageBoxResult.None;
            MessageBoxImage invalidImage = (MessageBoxImage)999; // Invalid value

            // Act & Assert
            var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
                MessageBox.Show(owner, messageBoxText, caption, button, invalidImage, defaultResult));
            Assert.Contains("icon", exception.Message);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void Show_WithOwner_InvalidMessageBoxOptions_ThrowsInvalidEnumArgumentException()
    {
        // Avoid annoying "The calling thread must be STA, because many UI components require this." exception
        Thread thread = new Thread(() =>
        {
            // Arrange
            var owner = new Window();
            string messageBoxText = "Test Message";
            string caption = "Test Caption";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxResult defaultResult = MessageBoxResult.None;
            MessageBoxImage image = MessageBoxImage.None;
            MessageBoxOptions invalidOptions = (MessageBoxOptions)999; // Invalid value

            // Act & Assert
            var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
                MessageBox.Show(owner, messageBoxText, caption, button, image, defaultResult, invalidOptions));
            Assert.Contains("options", exception.Message);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    /*
     * Tried to add this test also to test that an ArgumentException is thrown when passing in either
     * MessageBoxOptions.ServiceNotification or MessageBoxOptions.DefaultDesktopOnly when also passing in
     * a Window as the owner.
     *
     * However, this is not possible as the MessageBox.Show method passes the Handle property to the ShowCore
     * method, and the Handle property is null here - which in the ShowCore method results in owner being IntPtr.Zero,
     * and the intended test for not allowing the above MessageBoxOptions fail in the test.
     *
     * It is likely in a real world scenario that it will work because the owner window would be an already created
     * Window having a proper Handle set.
     */
    //[Theory]
    //[InlineData(MessageBoxOptions.ServiceNotification)]
    //[InlineData(MessageBoxOptions.DefaultDesktopOnly)]
    //public void Show_WithOwner_WithSpecificMessageBoxOptions_ThrowsArgumentException(MessageBoxOptions messageBoxOptions)
    //{
    //    // Avoid annoying "The calling thread must be STA, because many UI components require this." exception
    //    Thread thread = new Thread(() =>
    //    {
    //        // Arrange
    //        var owner = new Window();
    //        string messageBoxText = "Test Message";
    //        string caption = "Test Caption";
    //        MessageBoxButton button = MessageBoxButton.OK;
    //        MessageBoxResult defaultResult = MessageBoxResult.None;
    //        MessageBoxImage image = MessageBoxImage.None;

    //        // Act & Assert
    //        Assert.Throws<ArgumentException>(() =>
    //            MessageBox.Show(owner, messageBoxText, caption, button, image, defaultResult, messageBoxOptions));
    //    });
    //    thread.SetApartmentState(ApartmentState.STA);
    //    thread.Start();
    //    thread.Join();
    //}
}
