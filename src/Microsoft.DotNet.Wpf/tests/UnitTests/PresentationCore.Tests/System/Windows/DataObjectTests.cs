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

    [WpfFact]
    public void SetData_MultipleTypes_GetReturnsExpected()
    {
        int intData = 20;
        string stringData = "test string";

        DataObject data = new();

        data.SetData(intData);
        data.SetData(stringData);
        data.GetData(intData.GetType().FullName!).Should().Be(intData);
        data.GetData(stringData.GetType().FullName!).Should().Be(stringData);
    }

    [WpfFact]
    public void SetData_SameTypeTwice_OverwritesData()
    {
        string data1 = "data 1";
        string data2 = "data 2";

        DataObject data = new();

        data.SetData(data1);
        data.SetData(data2);
        data.GetData(data1.GetType().FullName!).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_StringObject_Invoke_GetReturnsExpected()
    {
        string key = "key";
        string testData = "test data";

        DataObject data = new();

        data.SetData(key, testData);
        data.GetData(key).Should().Be(testData);
    }

    [WpfFact]
    public void SetData_StringObject_NullData_ThrowsArgumentNullException()
    {
        string key = "key";
        string? testData = null;

        DataObject data = new();

        Action act = () => data.SetData(key, testData);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_StringObject_EmptyStringKey_ThrowsArgumentException()
    {
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData(string.Empty, testData);
        act.Should().Throw<ArgumentException>();
    }

    [WpfFact]
    public void SetData_StringObject_SameKeyTwice_OverwritesData()
    {
        string key = "key";
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key, data1);
        data.SetData(key, data2);

        data.GetData(key).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_StringObject_DifferentKeys_DataIsStoredSeparately()
    {
        string key1 = "key1";
        string key2 = "key2";
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key1, data1);
        data.SetData(key2, data2);
        data.GetData(key1).Should().Be(data1);
        data.GetData(key2).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_TypeObject_Invoke_GetReturnsExpected()
    {
        Type stringKey = typeof(string);
        Type intKey = typeof(int);
        Type boolKey = typeof(bool);
        string stringTestData = "string test data";
        string intTestData = "int test data";
        string boolTestData = "bool test data";

        DataObject data = new();

        data.SetData(stringKey, stringTestData);
        data.SetData(intKey, intTestData);
        data.SetData(boolKey, boolTestData);

        data.GetData(stringKey.FullName!).Should().Be(stringTestData);
        data.GetData(intKey.FullName!).Should().Be(intTestData);
        data.GetData(boolKey.FullName!).Should().Be(boolTestData);
    }

    [WpfFact]
    public void SetData_TypeObject_NullTypeKey_ThrowsArgumentNullException()
    {
        Type? key = null;
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData(key!, testData);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_TypeObject_NullData_ThrowsArgumentNullException()
    {
        Type key = typeof(string);

        DataObject data = new();

        Action act = () => data.SetData(key, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void GetData_NonExistentKey_ReturnsNull()
    {
        Type key = typeof(string);

        DataObject data = new();

        data.GetData(key.FullName!).Should().BeNull();
    }

    [WpfFact]
    public void SetData_TypeObject_SameKeyTwice_OverwritesData()
    {
        Type key = typeof(string);
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key, data1);
        data.SetData(key, data2);

        data.GetData(key.FullName!).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_TypeObject_DifferentKeys_DataIsStoredSeparately()
    {
        Type key1 = typeof(string);
        Type key2 = typeof(int);
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key1, data1);
        data.SetData(key2, data2);
        data.GetData(key1.FullName!).Should().Be(data1);
        data.GetData(key2.FullName!).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_StringObjectBool_Invoke_GetReturnsExpected()
    {
        string key = "key";
        string testData = "test data";

        DataObject data = new();

        data.SetData(key, testData, true);
        data.GetData(key).Should().Be(testData);
    }

    [WpfFact]
    public void SetData_StringObjectBool_NullData_ThrowsArgumentNullException()
    {
        string key = "key";

        DataObject data = new();

        Action act = () => data.SetData(key, null, true);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_StringObjectBool_NullKey_ThrowsArgumentNullException()
    {
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData((string)null!, testData, true);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_StringObjectBool_EmptyKey_ThrowsArgumentException()
    {
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData(string.Empty, testData, true);
        act.Should().Throw<ArgumentException>();
    }
}
