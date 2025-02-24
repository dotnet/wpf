// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Clipboard implementation to provide methods to place/get data from/to the system 
//              clipboard.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Clipboard.htm

using MS.Win32;
using MS.Internal;
using System.Collections.Specialized;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Imaging;

using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace System.Windows;

/// <summary>
///  Provides methods to place data on and retrieve data from the system clipboard.
///  This class cannot be inherited.
/// </summary>
public static class Clipboard
{
    /// <summary>
    ///  The number of times to retry OLE clipboard operations.
    /// </summary>
    private const int OleRetryCount = 10;

    /// <summary>
    ///  The amount of time in milliseconds to sleep between retrying OLE clipboard operations.
    /// </summary>
    private const int OleRetryDelay = 100;

    /// <summary>
    ///  The amount of time in milliseconds to sleep before flushing the clipboard after a set.
    /// </summary>
    private const int OleFlushDelay = 10;

    /// <summary>
    ///  Clear the system clipboard.
    /// </summary>
    public static void Clear()
    {
        // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

        int i = OleRetryCount;

        while (true)
        {
            // Clear the system clipboard by calling OleSetClipboard with null parameter.
            int hr = OleServicesContext.CurrentOleServicesContext.OleSetClipboard(null);

            if (NativeMethods.Succeeded(hr))
            {
                break;
            }

            if (--i == 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            Thread.Sleep(OleRetryDelay);
        }
    }

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the audio data. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsAudio() => ContainsDataInternal(DataFormats.WaveAudio);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the specified data format. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsData(string format)
    {
        ArgumentNullException.ThrowIfNull(format);

        if (format.Length == 0)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        return ContainsDataInternal(format);
    }

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the file drop list format. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsFileDropList() => ContainsDataInternal(DataFormats.FileDrop);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the image format. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsImage() => ContainsDataInternal(DataFormats.Bitmap);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the text data format which is unicode.
    ///  Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsText() => ContainsDataInternal(DataFormats.UnicodeText);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the specified text data format which is unicode. 
    ///  Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsText(TextDataFormat format)
    {
        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        return ContainsDataInternal(DataFormats.ConvertToDataFormats(format));
    }

    /// <summary>
    ///  Permanently renders the contents of the last IDataObject that was set onto the clipboard.
    /// </summary>
    public static void Flush()
    {
        // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

        int i = OleRetryCount;

        while (true)
        {
            int hr = OleServicesContext.CurrentOleServicesContext.OleFlushClipboard();

            if (NativeMethods.Succeeded(hr))
            {
                break;
            }

            if (--i == 0)
            {
                SecurityHelper.ThrowExceptionForHR(hr);
            }

            Thread.Sleep(OleRetryDelay);
        }
    }

    /// <summary>
    ///  Get audio data as Stream from Clipboard.
    /// </summary>
    public static Stream GetAudioStream() => GetDataInternal(DataFormats.WaveAudio) as Stream;

    /// <summary>
    ///  Get data for the specified data format from Clipboard.
    /// </summary>
    public static object GetData(string format)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return GetDataInternal(format);
    }

    /// <summary>
    ///  Get the file drop list as StringCollection from Clipboard.
    /// </summary>
    public static StringCollection GetFileDropList()
    {
        StringCollection fileDropListCollection = [];

        if (GetDataInternal(DataFormats.FileDrop) is string[] fileDropList)
        {
            fileDropListCollection.AddRange(fileDropList);
        }

        return fileDropListCollection;
    }

    /// <summary>
    ///  Get the image from Clipboard.
    /// </summary>
    public static BitmapSource GetImage() => GetDataInternal(DataFormats.Bitmap) as BitmapSource;

    /// <summary>
    ///  Get text from Clipboard.
    /// </summary>
    public static string GetText() => GetText(TextDataFormat.UnicodeText);

    /// <summary>
    ///  Get text from Clipboard.
    /// </summary>
    public static string GetText(TextDataFormat format)
    {
        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        string text = (string)GetDataInternal(DataFormats.ConvertToDataFormats(format));
        return text ?? string.Empty;
    }

    /// <summary>
    ///  Set the audio data to Clipboard.
    /// </summary>
    public static void SetAudio(byte[] audioBytes)
    {
        ArgumentNullException.ThrowIfNull(audioBytes);
        SetAudio(new MemoryStream(audioBytes));
    }

    /// <summary>
    ///  Set the audio data to Clipboard.
    /// </summary>
    public static void SetAudio(Stream audioStream)
    {
        ArgumentNullException.ThrowIfNull(audioStream);
        SetDataInternal(DataFormats.WaveAudio, audioStream);
    }

    /// <summary>
    ///  Set the specified data to Clipboard.
    /// </summary>
    public static void SetData(string format, object data)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        ArgumentNullException.ThrowIfNull(data);
        SetDataInternal(format, data);
    }

    /// <summary>
    ///  Set the file drop list to Clipboard.
    /// </summary>
    public static void SetFileDropList(StringCollection fileDropList)
    {
        ArgumentNullException.ThrowIfNull(fileDropList);

        if (fileDropList.Count == 0)
        {
            throw new ArgumentException(SR.Format(SR.DataObject_FileDropListIsEmpty, fileDropList));
        }

        foreach (string fileDrop in fileDropList)
        {
            try
            {
                string filePath = Path.GetFullPath(fileDrop);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(SR.Format(SR.DataObject_FileDropListHasInvalidFileDropPath, fileDropList));
            }
        }

        string[] fileDropListStrings;

        fileDropListStrings = new string[fileDropList.Count];
        fileDropList.CopyTo(fileDropListStrings, 0);

        SetDataInternal(DataFormats.FileDrop, fileDropListStrings);
    }

    /// <summary>
    ///  Set the image data to Clipboard.
    /// </summary>
    public static void SetImage(BitmapSource image)
    {
        ArgumentNullException.ThrowIfNull(image);
        SetDataInternal(DataFormats.Bitmap, image);
    }

    /// <summary>
    ///  Set the text data to Clipboard.
    /// </summary>
    public static void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        SetText(text, TextDataFormat.UnicodeText);
    }

    /// <summary>
    ///  Set the text data to Clipboard.
    /// </summary>
    public static void SetText(string text, TextDataFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        SetDataInternal(DataFormats.ConvertToDataFormats(format), text);
    }

    /// <summary>
    ///  Retrieves the data object that is currently on the system clipboard.
    /// </summary>
    public static IDataObject GetDataObject() => GetDataObjectInternal();

    /// <summary>
    ///  Determines whether the data object previously placed on the clipboard
    ///  by the SetDataObject is still on the clipboard.
    /// </summary>
    /// <param name="data">
    ///  Data object from the current containing clipboard which the caller
    ///  previously placed on the clipboard.
    /// </param>
    public static bool IsCurrent(IDataObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        bool bReturn = false;

        if (data is IComDataObject comDataObject)
        {
            // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

            int i = OleRetryCount;
            int hr;
            while (true)
            {
                hr = OleServicesContext.CurrentOleServicesContext.OleIsCurrentClipboard(comDataObject);

                if (NativeMethods.Succeeded(hr) || (--i == 0))
                {
                    break;
                }

                Thread.Sleep(OleRetryDelay);
            }

            if (hr == NativeMethods.S_OK)
            {
                bReturn = true;
            }
            else if (!NativeMethods.Succeeded(hr))
            {
                throw new ExternalException("OleIsCurrentClipboard()", hr);
            }
        }

        return bReturn;
    }

    /// <summary>
    ///  Places nonpersistent data on the system clipboard.
    /// </summary>
    /// <param name="data">
    ///  The specific data to be on clipboard.
    /// </param>
    public static void SetDataObject(object data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetDataObject(data, copy: false);
    }

    /// <summary>
    ///  Places data on the system Clipboard and uses copy to specify whether the data 
    ///  should remain on the Clipboard after the application exits.
    /// </summary>
    /// <param name="data">
    ///  The specific data to be on clipboard.
    /// </param>
    /// <param name="copy">
    ///  Specify whether the data should remain on the clipboard after the application exits.
    /// </param>
    public static void SetDataObject(object data, bool copy)
    {
        ArgumentNullException.ThrowIfNull(data);

        IComDataObject dataObject;

        if (data is DataObject @object)
        {
            dataObject = @object;
        }
        else if (data is IComDataObject comDataObject)
        {
            dataObject = comDataObject;
        }
        else
        {
            dataObject = new DataObject(data);
        }

        // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

        int i = OleRetryCount;

        while (true)
        {
            // Clear the system clipboard by calling OleSetClipboard with null parameter.
            int hr = OleServicesContext.CurrentOleServicesContext.OleSetClipboard(dataObject);

            if (NativeMethods.Succeeded(hr))
            {
                break;
            }

            if (--i == 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            Thread.Sleep(OleRetryDelay);
        }

        if (copy)
        {
            // OleSetClipboard and OleFlushClipboard both modify the clipboard
            // and cause notifications to be sent to clipboard listeners. We sleep a bit here to
            // mitigate issues with clipboard listeners (like TS) corrupting the clipboard contents
            // as a result of these two calls being back to back.
            Thread.Sleep(OleFlushDelay);

            Flush();
        }
    }

    private static IDataObject GetDataObjectInternal()
    {
        // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

        int i = OleRetryCount;

        IComDataObject oleDataObject;
        while (true)
        {
            oleDataObject = null;
            int hr = OleServicesContext.CurrentOleServicesContext.OleGetClipboard(ref oleDataObject);

            if (NativeMethods.Succeeded(hr))
            {
                break;
            }

            if (--i == 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            Thread.Sleep(OleRetryDelay);
        }

        IDataObject dataObject;
        if (oleDataObject is IDataObject iDataObject && !Marshal.IsComObject(oleDataObject))
        {
            dataObject = iDataObject;
        }
        else if (oleDataObject is not null)
        {
            // Wrap any COM objects or objects that don't implement <see cref="T:System.Windows.IDataObject"/>.
            // In the case of COM objects, this protects us from a <see cref="T:System.InvalidOperationException"/> from the marshaler 
            // when calling <see cref="M:System.Windows.IDataObject.GetData(T:System.Type)"/> due to <see cref="T:System.Type"/> 
            // not being marked with the <see cref="T:System.Runtime.InteropServices.COMVisibleAttribute"/>.
            dataObject = new DataObject(oleDataObject);
        }
        else
        {
            dataObject = null;
        }

        return dataObject;
    }

    /// <summary>
    ///  Query the specified data format from Clipboard.
    /// </summary>
    private static bool ContainsDataInternal(string format)
    {
        bool isFormatAvailable = false;

        if (IsDataFormatAutoConvert(format))
        {
            string[] formats = DataObject.GetMappedFormats(format);
            for (int i = 0; i < formats.Length; i++)
            {
                if (SafeNativeMethods.IsClipboardFormatAvailable(DataFormats.GetDataFormat(formats[i]).Id))
                {
                    isFormatAvailable = true;
                    break;
                }
            }
        }
        else
        {
            isFormatAvailable = SafeNativeMethods.IsClipboardFormatAvailable(DataFormats.GetDataFormat(format).Id);
        }

        return isFormatAvailable;
    }

    /// <summary>
    /// Get the specified format from Clipboard.
    /// </summary>
    private static object GetDataInternal(string format) => GetDataObject() is { } dataObject
        ? dataObject.GetData(format, IsDataFormatAutoConvert(format))
        : null;

    /// <summary>
    /// Set the specified data into Clipboard.
    /// </summary>
    private static void SetDataInternal(string format, object data)
    {
        DataObject dataObject = new();
        dataObject.SetData(format, data, IsDataFormatAutoConvert(format));

        SetDataObject(dataObject, copy: true);
    }

    /// <summary>
    ///  Check the auto convert for the specified data format.
    /// </summary>
    private static bool IsDataFormatAutoConvert(string format) =>
        format == DataFormats.FileDrop || format == DataFormats.Bitmap;
}
