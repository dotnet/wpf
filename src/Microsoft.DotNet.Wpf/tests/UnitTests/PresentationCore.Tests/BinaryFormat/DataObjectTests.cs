// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace PresentationCore.Tests.BinaryFormat;

public class DataObjectTests
{
    [Theory]
    [MemberData(nameof(DataObject_TestData_UnSupportedObjects))]
    public void DataObject_UnSupportedObjects_SetData(object value)
    {

        // Create a thread to run the test logic
        Thread testThread = new Thread(() => TestLogic(value));
        testThread.SetApartmentState(ApartmentState.STA); // Set the thread to STA
        testThread.Start();
        testThread.Join(); // Wait for the thread to complete
    }

    private void TestLogic(object value)
    {
        using BinaryFormatterScope formatterScope = new(enable: false);
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.Serializable, value);

        Clipboard.SetDataObject(dataObject, true);
        IDataObject? ClipboardDataObject = Clipboard.GetDataObject();
        try
        {
            if (ClipboardDataObject is not null)
            {
                Assert.Throws<System.Runtime.InteropServices.COMException>(() => ClipboardDataObject.GetData(DataFormats.Serializable));
            }
            else
            {
                Assert.Fail("ClipboardDataObject is null.");
            }
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    public static TheoryData<object> DataObject_TestData_UnSupportedObjects => new()
    {
        new SerializableData(),
        new Dictionary<int, string>(),
        new object(),
        Color.DeepSkyBlue, // Use a static property for Color
        new Pen(Brushes.DeepSkyBlue, 1),
        new Bitmap(1, 1), // Use Bitmap instead of Image
        new System.Drawing.Printing.PrintDocument(),
        new System.Drawing.Printing.PrinterSettings(),
        new System.Drawing.Printing.PageSettings(),
        new System.Drawing.Printing.PaperSize(),
        new System.Drawing.Printing.PaperSource(),
        new System.Drawing.Printing.PrinterResolution(),
    };
}

[Serializable]
public class SerializableData
{
    public string Name { get; set; }
    public DateTime BirthDate { get; set; }
    public int Age { get; set; }
    public double Salary { get; set; }
    public bool IsEmployed { get; set; }
    public char Gender { get; set; }
    public byte[] BinaryData { get; set; }

    // Constructor
    public SerializableData()
    {
        // Initialize properties with default values
        Name = "John Doe";
        BirthDate = DateTime.Now.AddYears(-30);
        Age = 30;
        Salary = 50000.0;
        IsEmployed = true;
        Gender = 'M';
        BinaryData = [1, 2, 3, 4, 5]; // Example binary data
    }
}
