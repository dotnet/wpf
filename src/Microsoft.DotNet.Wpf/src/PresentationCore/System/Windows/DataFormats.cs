// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Private.Windows.Ole;

namespace System.Windows;

/// <summary>
///  Translates between Windows Design text-based formats and 32-bit signed integer-based clipboard formats.
/// </summary>
public static class DataFormats
{
    private static bool s_initialized;

    /// <summary>
    ///  Gets an object with the Windows Clipboard numeric ID and name for the specified ID.
    /// </summary>
    public static DataFormat GetDataFormat(int id) => DataFormatsCore.GetOrAddFormat(id);

    /// <summary>
    ///  Gets the data format with the Windows Clipboard numeric ID and name for the specified data format.
    /// </summary>
    public static DataFormat GetDataFormat(string format)
    {
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        // Ensures the predefined Win32 data formats into our format list.
        EnsurePredefined();

        return DataFormatsCore.GetOrAddFormat(format);
    }

    /// <summary>
    ///  Specifies the standard ANSI text format.
    /// </summary>
    public static readonly string Text = DataFormatNames.Text;

    /// <summary>
    ///  Specifies the standard Windows Unicode text format.
    /// </summary>
    public static readonly string UnicodeText = DataFormatNames.UnicodeText;

    /// <summary>
    ///  Specifies the Windows Device Independent Bitmap (DIB) format.
    /// </summary>
    public static readonly string Dib = DataFormatNames.Dib;

    /// <summary>
    ///  Specifies a Windows bitmap format.
    /// </summary>
    public static readonly string Bitmap = DataFormatNames.Bitmap;

    /// <summary>
    ///  Specifies the Windows enhanced metafile format.
    /// </summary>
    public static readonly string EnhancedMetafile = DataFormatNames.Emf;

    /// <summary>
    ///  Specifies the Windows metafile format.
    /// </summary>
    public static readonly string MetafilePicture = DataFormatNames.Wmf;

    /// <summary>
    ///  Specifies the Windows symbolic link format.
    /// </summary>
    public static readonly string SymbolicLink = DataFormatNames.SymbolicLink;

    /// <summary>
    ///  Specifies the Windows data interchange format.
    /// </summary>
    public static readonly string Dif = DataFormatNames.Dif;

    /// <summary>
    ///  Specifies the Tagged Image File Format (TIFF).
    /// </summary>
    public static readonly string Tiff = DataFormatNames.Tiff;

    /// <summary>
    ///  Specifies the standard Windows original equipment manufacturer (OEM) text format.
    /// </summary>
    public static readonly string OemText = DataFormatNames.OemText;

    /// <summary>
    ///  Specifies the Windows palette format.
    /// </summary>
    public static readonly string Palette = DataFormatNames.Palette;

    /// <summary>
    ///  Specifies the Windows pen data format, which consists of pen strokes for handwriting software.
    /// </summary>
    public static readonly string PenData = DataFormatNames.PenData;

    /// <summary>
    ///  Specifies the Resource Interchange File Format (RIFF) audio format.
    /// </summary>
    public static readonly string Riff = DataFormatNames.Riff;

    /// <summary>
    ///  Specifies the wave audio format, which Windows Design does not directly use.
    /// </summary>
    public static readonly string WaveAudio = DataFormatNames.WaveAudio;

    /// <summary>
    ///  Specifies the Windows file drop format, which Windows Design does not directly use.
    /// </summary>
    public static readonly string FileDrop = DataFormatNames.FileDrop;

    /// <summary>
    ///  Specifies the Windows culture format, which Windows Design does not directly use.
    /// </summary>
    public static readonly string Locale = DataFormatNames.Locale;

    /// <summary>
    ///  Specifies text consisting of HTML data.
    /// </summary>
    public static readonly string Html = DataFormatNames.Html;

    /// <summary>
    ///  Specifies text consisting of Rich Text Format (RTF) data. This
    /// </summary>
    public static readonly string Rtf = DataFormatNames.Rtf;

    /// <summary>
    ///  Specifies a comma-separated value (CSV) format, which is a common interchange format used by spreadsheets.
    ///  This format is not used directly by Windows Design.
    /// </summary>
    public static readonly string CommaSeparatedValue = DataFormatNames.Csv;

    /// <summary>
    ///  Specifies the Windows Design string class format, which WinForms uses to store string objects.
    /// </summary>
    public static readonly string StringFormat = DataFormatNames.String;

    /// <summary>
    ///  Specifies a format that encapsulates any type of Windows Design object.
    /// </summary>
    public static readonly string Serializable = DataFormatNames.Serializable;

    /// <summary>
    ///  Specifies a data format as Xaml.
    /// </summary>
    public static readonly string Xaml = DataFormatNames.Xaml;

    /// <summary>
    ///  Specifies a data format as Xaml Package.
    /// </summary>
    public static readonly string XamlPackage = DataFormatNames.XamlPackage;

    /// <summary>
    ///  Convert TextDataFormat to Dataformats.
    /// </summary>
    internal static string ConvertToDataFormats(TextDataFormat textDataformat) => textDataformat switch
    {
        TextDataFormat.Text => DataFormatNames.Text,
        TextDataFormat.UnicodeText => DataFormatNames.UnicodeText,
        TextDataFormat.Rtf => DataFormatNames.Rtf,
        TextDataFormat.Html => DataFormatNames.Html,
        TextDataFormat.CommaSeparatedValue => DataFormatNames.Csv,
        TextDataFormat.Xaml => DataFormatNames.Xaml,
        _ => DataFormatNames.UnicodeText,
    };

    /// <summary>
    ///  Validate the text data format.
    /// </summary>
    internal static bool IsValidTextDataFormat(TextDataFormat textDataFormat) =>
        textDataFormat is TextDataFormat.Text
            or TextDataFormat.UnicodeText
            or TextDataFormat.Rtf
            or TextDataFormat.Html
            or TextDataFormat.CommaSeparatedValue
            or TextDataFormat.Xaml;

    private static void EnsurePredefined()
    {
        if (!s_initialized)
        {
            s_initialized = true;

            // Add the WPF specific data formats by getting the ids for each.
            DataFormatsCore.GetOrAddFormat(DataFormatNames.Xaml);
            DataFormatsCore.GetOrAddFormat(DataFormatNames.InkSerializedFormat);
        }
    }
}
