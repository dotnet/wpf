// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows;

// Note: the OS Clipboard is a system wide resource and all access should be done sequentially to avoid
// collisions with other tests. We also retry as we cannot control other processes that may be using the clipboard.
[Collection("Sequential")]
[UISettings(MaxAttempts = 3)]
public class DataObjectTests
{
    [WpfFact]
    public void SetData_Invoke_GetReturnsExpected()
    {
        string testData = "test data";
        DataObject data = new();
        data.SetData(testData);
        data.GetData(testData.GetType().FullName!).Should().Be(testData);
    }

    [WpfFact]
    public void SetData_Null_ThrowsArgumentNullException()
    {
        DataObject data = new();
        Action action = () => data.SetData(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [WpfFact]
    public void GetData_NonExistentFormat_ReturnsNull()
    {
        DataObject data = new();
        data.GetData("non-existent format").Should().BeNull();
    }

    [WpfFact]
    public void ContainsData_ContainsText_ReturnsFalse()
    {
        string testData = "test data";
        DataObject data = new();
        data.SetData(testData);
        data.ContainsText().Should().BeFalse();
    }

    [WpfFact]
    public void SetData_WithTextFormat_CanRetrieve()
    {
        DataObject data = new();
        data.SetData(DataFormats.Text, "Hello World");
        data.GetData(DataFormats.Text).Should().Be("Hello World");
    }

    [WpfFact]
    public void SetData_WithCustomFormat_CanRetrieve()
    {
        DataObject data = new();
        const string customFormat = "CustomFormat";
        int testValue = 42;
        data.SetData(customFormat, testValue);
        data.GetData(customFormat).Should().Be(testValue);
    }

    [WpfFact]
    public void SetData_NullFormat_ThrowsArgumentNullException()
    {
        DataObject data = new();
        Action action = () => data.SetData((string)null!, "Some Data");
        action.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "format");
    }
}
