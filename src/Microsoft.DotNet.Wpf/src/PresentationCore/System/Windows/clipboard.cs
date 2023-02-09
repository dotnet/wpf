// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Clipboard implementation to provide methods to place/get data from/to the system 
//              clipboard.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Clipboard.htm
// 
//

using MS.Win32;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using System.Collections.Specialized;
using System.IO;
using System.Security;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace System.Windows
{
    #region Clipboard class

    /// <summary>
    /// Provides methods to place data on and retrieve data from the system clipboard. 
    /// This class cannot be inherited.
    /// </summary>
    public static class Clipboard 
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Clear the system clipboard which the clipboard is emptied.
        /// SetDataObject.
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
        /// Return true if Clipboard contains the audio data. Otherwise, return false.
        /// </summary>
        public static bool ContainsAudio()
        {
            return ContainsDataInternal(DataFormats.WaveAudio);
        }

        /// <summary>
        /// Return true if Clipboard contains the specified data format. Otherwise, return false.
        /// </summary>
        public static bool ContainsData(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (format.Length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            return ContainsDataInternal(format);
        }

        /// <summary>
        /// Return true if Clipboard contains the file drop list format. Otherwise, return false.
        /// </summary>
        public static bool ContainsFileDropList()
        {
            return ContainsDataInternal(DataFormats.FileDrop);
        }

        /// <summary>
        /// Return true if Clipboard contains the image format. Otherwise, return false.
        /// </summary>
        public static bool ContainsImage()
        {
            return ContainsDataInternal(DataFormats.Bitmap);
        }

        /// <summary>
        /// Return true if Clipboard contains the text data format which is unicode. 
        /// Otherwise, return false.
        /// </summary>
        public static bool ContainsText()
        {
            return ContainsDataInternal(DataFormats.UnicodeText);
        }

        /// <summary>
        /// Return true if Clipboard contains the specified text data format which is unicode. 
        /// Otherwise, return false.
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
        /// Permanently renders the contents of the last IDataObject that was set onto the clipboard.
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
        /// Get audio data as Stream from Clipboard.
        /// </summary>
        public static Stream GetAudioStream()
        {
            return GetDataInternal(DataFormats.WaveAudio) as Stream;
        }

        /// <summary>
        /// Get data for the specified data format from Clipboard.
        /// </summary>
        public static object GetData(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            return GetDataInternal(format);
        }

        /// <summary>
        /// Get the file drop list as StringCollection from Clipboard.
        /// </summary>
        public static StringCollection GetFileDropList()
        {
            StringCollection fileDropListCollection;
            string[] fileDropList;

            fileDropListCollection = new StringCollection();

            fileDropList = GetDataInternal(DataFormats.FileDrop) as string[];
            if (fileDropList != null)
            {
                fileDropListCollection.AddRange(fileDropList);
            }

            return fileDropListCollection;
        }

        /// <summary>
        /// Get the image from Clipboard.
        /// </summary>
        public static BitmapSource GetImage()
        {
            return GetDataInternal(DataFormats.Bitmap) as BitmapSource;
        }

        /// <summary>
        /// Get text from Clipboard.
        /// </summary>
        public static string GetText()
        {
            return GetText(TextDataFormat.UnicodeText);
        }

        /// <summary>
        /// Get text from Clipboard.
        /// </summary>
        public static string GetText(TextDataFormat format)
        {
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            string text;

            text = (string)GetDataInternal(DataFormats.ConvertToDataFormats(format));

            if (text != null)
            {
                return text;
            }

            return string.Empty;
        }

        /// <summary>
        /// Set the audio data to Clipboard.
        /// </summary>
        public static void SetAudio(byte[] audioBytes)
        {
            if (audioBytes == null)
            {
                throw new ArgumentNullException(nameof(audioBytes));
            }

            SetAudio(new MemoryStream(audioBytes));
        }

        /// <summary>
        /// Set the audio data to Clipboard.
        /// </summary>
        public static void SetAudio(Stream audioStream)
        {
            if (audioStream == null)
            {
                throw new ArgumentNullException(nameof(audioStream));
            }

            SetDataInternal(DataFormats.WaveAudio, audioStream);
        }

        /// <summary>
        /// Set the specified data to Clipboard.
        /// </summary>
        public static void SetData(string format, object data)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            SetDataInternal(format, data);
        }

        /// <summary>
        /// Set the file drop list to Clipboard.
        /// </summary>
        public static void SetFileDropList(StringCollection fileDropList)
        {
            if (fileDropList == null)
            {
                throw new ArgumentNullException(nameof(fileDropList));
            }

            if (fileDropList.Count == 0)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_FileDropListIsEmpty, fileDropList));
            }

            foreach (string fileDrop in fileDropList)
            {
                try
                {
                    string filePath = Path.GetFullPath(fileDrop);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(SR.Get(SRID.DataObject_FileDropListHasInvalidFileDropPath, fileDropList));
                }
            }

            string[] fileDropListStrings;

            fileDropListStrings = new string[fileDropList.Count];
            fileDropList.CopyTo(fileDropListStrings, 0);

            SetDataInternal(DataFormats.FileDrop, fileDropListStrings);
        }

        /// <summary>
        /// Set the image data to Clipboard.
        /// </summary>
        public static void SetImage(BitmapSource image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            SetDataInternal(DataFormats.Bitmap, image);
        }

        /// <summary>
        /// Set the text data to Clipboard.
        /// </summary>
        public static void SetText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            SetText(text, TextDataFormat.UnicodeText);
        }

        /// <summary>
        /// Set the text data to Clipboard.
        /// </summary>
        public static void SetText(string text, TextDataFormat format)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            SetDataInternal(DataFormats.ConvertToDataFormats(format), text);
        }

        /// <summary>
        /// Retrieves the data object that is currently on the system clipboard.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionClipboard.AllClipboard) to call this API.
        /// </remarks>
        public static IDataObject GetDataObject() 
        {

            return GetDataObjectInternal();
        }

        /// <summary>
        /// Determines whether the data object previously placed on the clipboard
        /// by the SetDataObject is still on the clipboard.
        /// </summary>
        /// <param name="data">
        /// Data object from the current containing clipboard which the caller
        /// previously placed on the clipboard.
        /// </param>
        public static bool IsCurrent(IDataObject data) 
        {
            bool bReturn;

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            bReturn = false;

            if (data is IComDataObject)
            {
                int hr;

                // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

                int i = OleRetryCount;

                while (true)
                {
                    hr = OleServicesContext.CurrentOleServicesContext.OleIsCurrentClipboard((IComDataObject)data);

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
        /// Places nonpersistent data on the system clipboard.
        /// </summary>
        /// <param name="data">
        /// The specific data to be on clipboard.
        /// </param>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionClipboard.AllClipboard) to call this API.
        /// </remarks>
        public static void SetDataObject(object data) 
        {

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            SetDataObject(data, false);
        }

        /// <summary>
        /// Places data on the system Clipboard and uses copy to specify whether the data 
        /// should remain on the Clipboard after the application exits.
        /// </summary>
        /// <param name="data">
        /// The specific data to be on clipboard.
        /// </param>
        /// <param name="copy">
        /// Specify whether the data should remain on the clipboard after the application exits.
        /// </param>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionClipboard.AllClipboard) to call this API.
        /// </remarks>
        public static void SetDataObject(object data, bool copy)
        {
            CriticalSetDataObject(data,copy);
        }

        #endregion Public Methods

        #region Internal Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Places data on the system Clipboard and uses copy to specify whether the data 
        /// should remain on the Clipboard after the application exits.
        /// </summary>
        /// <param name="data">
        /// The specific data to be on clipboard.
        /// </param>
        /// <param name="copy">
        /// Specify whether the data should remain on the clipboard after the application exits.
        /// </param>
        [FriendAccessAllowed]
        internal static void CriticalSetDataObject(object data, bool copy)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            IComDataObject dataObject;

            if (data is DataObject)
            {
                dataObject = (DataObject)data;
            }
            else if (data is IComDataObject)
            {
                dataObject = (IComDataObject)data;
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

        [FriendAccessAllowed]
        internal static bool IsClipboardPopulated()
        {
            return (GetDataObjectInternal() != null);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Calls IsDynamicCodePolicyEnabled to determine if DeviceGuard is enabled, then caches it so subsequent calls only return the cached value.
        /// </summary>
        private static bool IsDeviceGuardEnabled
        {
            get
            {
                if (_isDeviceGuardEnabled < 0) return false;
                if (_isDeviceGuardEnabled > 0) return true;

                bool isDynamicCodePolicyEnabled = IsDynamicCodePolicyEnabled();
                _isDeviceGuardEnabled = isDynamicCodePolicyEnabled ? 1 : -1;

                return isDynamicCodePolicyEnabled;
            }
        }

        /// <summary>
        /// Loads Wldp.dll and looks for WldpIsDynamicCodePolicyEnabled to determine whether DeviceGuard is enabled.
        /// </summary>
        private static bool IsDynamicCodePolicyEnabled()
        {
            bool isEnabled = false;

            IntPtr hModule = IntPtr.Zero;
            try
            {
                hModule = LoadLibraryHelper.SecureLoadLibraryEx(ExternDll.Wldp, IntPtr.Zero, UnsafeNativeMethods.LoadLibraryFlags.LOAD_LIBRARY_SEARCH_SYSTEM32);
                if (hModule != IntPtr.Zero)
                {
                    IntPtr entryPoint = UnsafeNativeMethods.GetProcAddressNoThrow(new HandleRef(null, hModule), "WldpIsDynamicCodePolicyEnabled");
                    if (entryPoint != IntPtr.Zero)
                    {
                        int hResult = UnsafeNativeMethods.WldpIsDynamicCodePolicyEnabled(out isEnabled);

                        if (hResult != NativeMethods.S_OK)
                        {
                            isEnabled = false;
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (hModule != IntPtr.Zero)
                {
                    UnsafeNativeMethods.FreeLibrary(hModule);
                }
            }

            return isEnabled;
        }

        private static IDataObject GetDataObjectInternal()
        {
            IDataObject dataObject;
            IComDataObject oleDataObject;

            // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

            int i = OleRetryCount;

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

            if (oleDataObject is IDataObject && !Marshal.IsComObject(oleDataObject))
            {
                dataObject = (IDataObject)oleDataObject;
            }
            else if (oleDataObject != null)
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
        /// Query the specified data format from Clipboard.
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
        private static object GetDataInternal(string format)
        {
            IDataObject dataObject;

            dataObject = Clipboard.GetDataObject();

            if (dataObject != null)
            {
                bool autoConvert;

                if (IsDataFormatAutoConvert(format))
                {
                    autoConvert = true;
                }
                else
                {
                    autoConvert = false;
                }

                return dataObject.GetData(format, autoConvert);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Set the specified data into Clipboard.
        /// </summary>
        private static void SetDataInternal(string format, object data)
        {
            IDataObject dataObject;
            bool autoConvert;

            if (IsDataFormatAutoConvert(format))
            {
                autoConvert = true;
            }
            else
            {
                autoConvert = false;
            }

            dataObject = new DataObject();
            dataObject.SetData(format, data, autoConvert);

            Clipboard.SetDataObject(dataObject, /*copy*/true);
        }

        /// <summary>
        /// Check the auto convert for the specified data format.
        /// </summary>
        private static bool IsDataFormatAutoConvert(string format)
        {
            bool autoConvert;

            if (String.CompareOrdinal(format, DataFormats.FileDrop) == 0 ||
                String.CompareOrdinal(format, DataFormats.Bitmap) == 0)
            {
                autoConvert = true;
            }
            else
            {
                autoConvert = false;
            }

            return autoConvert;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Constants
        //
        //------------------------------------------------------

        #region Private Constants

        /// <summary>
        /// The number of times to retry OLE clipboard operations.
        /// </summary>
        /// <remarks>
        /// This is mitigation for clipboard locking issues in TS sessions.
        /// </remarks>
        private const int OleRetryCount = 10;

        /// <summary>
        /// The amount of time in milliseconds to sleep between retrying OLE clipboard operations.
        /// </summary>
        /// <remarks>
        /// This is mitigation for clipboard locking issues in TS sessions. 
        /// </remarks>
        private const int OleRetryDelay = 100;

        /// <summary>
        /// The amount of time in milliseconds to sleep before flushing the clipboard after a set.
        /// </summary>
        /// <remarks>
        /// This is mitigation for clipboard listener issues.
        /// </remarks>
        private const int OleFlushDelay = 10;

        #endregion Private Constants

        private static int _isDeviceGuardEnabled = 0;
    }

    #endregion Clipboard class
}
