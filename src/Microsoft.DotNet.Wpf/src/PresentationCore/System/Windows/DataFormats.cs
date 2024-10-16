// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Manage the data formats.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Data%20Transfer%20Object.htm
//
//

using MS.Win32;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using MS.Internal.PresentationCore;
using SecurityHelper = MS.Internal.SecurityHelper;

namespace System.Windows
{
    #region DataFormats class

    /// <summary>
    /// Translates between Windows Design text-based formats and
    /// 32-bit signed integer-based clipboard formats.
    /// </summary>
    public static class DataFormats
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Gets the data format with the Windows Clipboard numeric ID and name for the specified ID.
        /// </summary>
        public static DataFormat GetDataFormat(int id) => InternalGetDataFormat(id);

        /// <summary>
        /// Gets the data format with the Windows Clipboard numeric ID and name for the specified data format.
        /// </summary>
        public static DataFormat GetDataFormat(string format)
        {
            ArgumentNullException.ThrowIfNull(format);

            if (format == string.Empty)
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);

            // Ensures the predefined Win32 data formats into our format list.
            EnsurePredefined();

            // Lock the data format list to obtain the mutual-exclusion.
            lock (_formatListlock)
            {
                for (int i = 0; i < _formatList.Count; i++)
                {
                    DataFormat formatItem = _formatList[i];

                    if (formatItem.Name.Equals(format, StringComparison.OrdinalIgnoreCase))
                        return formatItem;
                }

                // Register this format string
                int formatId = UnsafeNativeMethods.RegisterClipboardFormat(format);

                if (formatId == 0)
                    throw new System.ComponentModel.Win32Exception();

                // Create a new format and store it
                DataFormat newFormat = new(format, formatId);
                _formatList.Add(newFormat);

                return newFormat;
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        /// <summary>
        /// Specifies the standard ANSI text format. This field is read-only.
        /// </summary>
        public static readonly string Text = "Text";

        /// <summary>
        /// Specifies the standard Windows Unicode text format. This
        /// field is read-only.
        /// </summary>
        public static readonly string UnicodeText = "UnicodeText";

        /// <summary>
        /// Specifies the Windows Device Independent Bitmap (DIB)
        /// format. This field is read-only.
        /// </summary>
        public static readonly string Dib = "DeviceIndependentBitmap";

        /// <summary>
        /// Specifies a Windows bitmap format. This field is read-only.
        /// </summary>
        public static readonly string Bitmap = "Bitmap";

        /// <summary>
        /// Specifies the Windows enhanced metafile format. This
        /// field is read-only.
        /// </summary>
        public static readonly string EnhancedMetafile = "EnhancedMetafile";

        /// <summary>
        /// Specifies the Windows metafile format. This field is read-only.
        /// </summary>
        public static readonly string MetafilePicture = "MetaFilePict";

        /// <summary>
        /// Specifies the Windows symbolic link format. This
        /// field is read-only.
        /// </summary>
        public static readonly string SymbolicLink = "SymbolicLink";

        /// <summary>
        /// Specifies the Windows data interchange format. This
        /// field is read-only.
        /// </summary>
        public static readonly string Dif = "DataInterchangeFormat";

        /// <summary>
        /// Specifies the Tagged Image File Format (TIFF). This
        /// field is read-only.
        /// </summary>
        public static readonly string Tiff = "TaggedImageFileFormat";

        /// <summary>
        /// Specifies the standard Windows original equipment
        /// manufacturer (OEM) text format. This field is read-only.
        /// </summary>
        public static readonly string OemText = "OEMText";

        /// <summary>
        /// Specifies the Windows palette format. This
        /// field is read-only.
        /// </summary>
        public static readonly string Palette = "Palette";

        /// <summary>
        /// Specifies the Windows pen data format, which consists of
        /// pen strokes for handwriting software; This field is read-only.
        /// </summary>
        public static readonly string PenData = "PenData";

        /// <summary>
        /// Specifies the Resource Interchange File Format (RIFF)
        /// audio format. This field is read-only.
        /// </summary>
        public static readonly string Riff = "RiffAudio";

        /// <summary>
        /// Specifies the wave audio format, which Windows Design does not
        /// directly use. This field is read-only.
        /// </summary>
        public static readonly string WaveAudio = "WaveAudio";

        /// <summary>
        /// Specifies the Windows file drop format, which Windows Design
        /// does not directly use. This field is read-only.
        /// </summary>
        public static readonly string FileDrop = "FileDrop";

        /// <summary>
        /// Specifies the Windows culture format, which Windows Design does
        /// not directly use. This field is read-only.
        /// </summary>
        public static readonly string Locale = "Locale";

        /// <summary>
        /// Specifies text consisting of HTML data. This
        /// field is read-only.
        /// </summary>
        public static readonly string Html = "HTML Format";

        /// <summary>
        /// Specifies text consisting of Rich Text Format (RTF) data. This
        /// field is read-only.
        /// </summary>
        public static readonly string Rtf = "Rich Text Format";

        /// <summary>
        /// Specifies a comma-separated value (CSV) format, which is a
        /// common interchange format used by spreadsheets. This format is not used directly
        /// by Windows Design. This field is read-only.
        /// </summary>
        public static readonly string CommaSeparatedValue = "CSV";

        /// <summary>
        /// Specifies the Windows Design string class format, which Win
        /// Forms uses to store string objects. This
        /// field is read-only.
        /// </summary>
        public static readonly string StringFormat = typeof(string).FullName;

        /// <summary>
        /// Specifies a format that encapsulates any type of Windows Design
        /// object. This field is read-only.
        /// </summary>
        public static readonly string Serializable = "PersistentObject";


        /// <summary>
        /// Specifies a data format as Xaml. This field is read-only.
        /// </summary>
        public static readonly string Xaml = "Xaml";

        /// <summary>
        /// Specifies a data format as Xaml Package. This field is read-only.
        /// </summary>
        public static readonly string XamlPackage = "XamlPackage";
        #endregion Public Fields

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields
        /// <summary>
        /// Specifies a data format as ApplicationTrust which is used to block
        /// paste from partial trust to full trust applications. The intent of this 
        /// format is to store the permission set of the source application where the content came from.
        /// This is then compared at paste time
        /// </summary>
        internal const string ApplicationTrust = "ApplicationTrust";

        internal const string FileName = "FileName";
        internal const string FileNameW = "FileNameW";

        #endregion  Internal Fields

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Convert TextDataFormat to Dataformats.
        /// </summary>
        internal static string ConvertToDataFormats(TextDataFormat textDataformat) => textDataformat switch
        {
            TextDataFormat.Text => DataFormats.Text,
            TextDataFormat.UnicodeText => DataFormats.UnicodeText,
            TextDataFormat.Rtf => DataFormats.Rtf,
            TextDataFormat.Html => DataFormats.Html,
            TextDataFormat.CommaSeparatedValue => DataFormats.CommaSeparatedValue,
            TextDataFormat.Xaml => DataFormats.Xaml,
            _ => DataFormats.UnicodeText
        };

        /// <summary>
        /// Validate the text data format.
        /// </summary>
        ///
        internal static bool IsValidTextDataFormat(TextDataFormat textDataFormat) => textDataFormat switch
        {
            TextDataFormat.Text => true,
            TextDataFormat.UnicodeText => true,
            TextDataFormat.Rtf => true,
            TextDataFormat.Html => true,
            TextDataFormat.CommaSeparatedValue => true,
            TextDataFormat.Xaml => true,
            _ => false
        };

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Allows a the new format name to be specified if the requested format is not
        /// in the list
        /// </summary>
        private static DataFormat InternalGetDataFormat(int id)
        {
            // Ensures the predefined Win32 data formats into our format list.
            EnsurePredefined();

            // Lock the data format list to obtain the mutual-exclusion.
            lock (_formatListlock)
            {
                DataFormat formatItem;
                StringBuilder sb;

                for (int i = 0; i < _formatList.Count; i++)
                {
                    formatItem = _formatList[i];

                    // OLE FORMATETC defined CLIPFORMAT as the unsigned short, so we should ignore
                    // high 2bytes to find the matched CLIPFORMAT ID. 
                    if ((formatItem.Id & 0x0000ffff) == (id & 0x0000ffff))
                        return formatItem;
                }

                sb = new StringBuilder(NativeMethods.MAX_PATH);

                // This can happen if windows adds a standard format that we don't know about,
                // so we should play it safe.
                if (UnsafeNativeMethods.GetClipboardFormatName(id, sb, sb.Capacity) == 0)
                {
                    sb.Length = 0; // Same as Clear()
                    sb.Append("Format").Append(id);
                }

                // Create a new format and store it
                formatItem = new(sb.ToString(), id);
                _formatList.Add(formatItem);

                return formatItem;
            }
        }

        /// <summary>
        /// Ensures that the Win32 predefined formats are setup in our format list.  This
        /// is called anytime we need to search the list
        /// </summary>
        private static void EnsurePredefined()
        {
            // Lock the data format list to obtain the mutual-exclusion.
            lock (_formatListlock)
            {
                if (_formatList is not null)
                    return;

                // Create format list for the default formats.
                _formatList = new List<DataFormat>(19)
                {
                    new(UnicodeText, NativeMethods.CF_UNICODETEXT),
                    new(Text, NativeMethods.CF_TEXT),
                    new(Bitmap, NativeMethods.CF_BITMAP),
                    new(MetafilePicture, NativeMethods.CF_METAFILEPICT),
                    new(EnhancedMetafile, NativeMethods.CF_ENHMETAFILE),
                    new(Dif, NativeMethods.CF_DIF),
                    new(Tiff, NativeMethods.CF_TIFF),
                    new(OemText, NativeMethods.CF_OEMTEXT),
                    new(Dib, NativeMethods.CF_DIB),
                    new(Palette, NativeMethods.CF_PALETTE),
                    new(PenData, NativeMethods.CF_PENDATA),
                    new(Riff, NativeMethods.CF_RIFF),
                    new(WaveAudio, NativeMethods.CF_WAVE),
                    new(SymbolicLink, NativeMethods.CF_SYLK),
                    new(FileDrop, NativeMethods.CF_HDROP),
                    new(Locale, NativeMethods.CF_LOCALE)
                };

                int xamlFormatId = UnsafeNativeMethods.RegisterClipboardFormat(Xaml);
                if (xamlFormatId != 0)
                    _formatList.Add(new(Xaml, xamlFormatId));

                // This is the format to store trust boundary information. Essentially this is accompalished by storing 
                // the permission set of the source application where the content comes from. During paste we compare this to
                // the permission set of the target application.
                int applicationTrustFormatId = UnsafeNativeMethods.RegisterClipboardFormat(DataFormats.ApplicationTrust);
                if (applicationTrustFormatId != 0)
                    _formatList.Add(new(ApplicationTrust, applicationTrustFormatId));

                // RegisterClipboardFormat returns 0 on failure
                int inkServicesFrameworkFormatId = UnsafeNativeMethods.RegisterClipboardFormat(Ink.StrokeCollection.InkSerializedFormat);
                if (inkServicesFrameworkFormatId != 0)
                    _formatList.Add(new(Ink.StrokeCollection.InkSerializedFormat, inkServicesFrameworkFormatId));
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The registered data format list.
        private static List<DataFormat> _formatList;

        // This object is for locking the _formatList to access safe in the multi-thread.
        private static readonly object _formatListlock = new();

        #endregion Private Fields
    }

    #endregion DataFormats class
}
