// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using MS.Internal;

namespace System.Windows;

public sealed partial class DataObject
{
    private partial class DataStore : IDataObject
    {
        // Data hash table.
        private readonly Dictionary<string, DataStoreEntry> _data = [];

        public DataStore()
        {
        }

        public object? GetData(string format) => GetData(format, autoConvert: true);

        public object? GetData(Type format) => GetData(format.FullName!);

        public object? GetData(string format, bool autoConvert)
        {
            _data.TryGetValue(format, out DataStoreEntry? entry);
            object? baseVar = entry?.Data;
            object? original = baseVar;

            if (!autoConvert
                || (entry is not null && !entry.AutoConvert)
                || (baseVar is not null && baseVar is not MemoryStream)
                || GetMappedFormats(format) is not { } mappedFormats)
            {
                return original ?? baseVar;
            }

            for (int i = 0; i < mappedFormats.Length; i++)
            {
                if (format != mappedFormats[i])
                {
                    _data.TryGetValue(format, out DataStoreEntry? mappedEntry);
                    baseVar = mappedEntry?.Data;

                    if (baseVar is not null and not MemoryStream)
                    {
                        if (baseVar is BitmapSource || SystemDrawingHelper.IsBitmap(baseVar))
                        {
                            // Ensure Bitmap(BitmapSource or System.Drawing.Bitmap) data which
                            // match with the requested format.
                            baseVar = EnsureBitmapDataFromFormat(format, autoConvert, baseVar);
                        }

                        original = null;
                        break;
                    }
                }
            }

            return original ?? baseVar;
        }
        public bool GetDataPresent(string format) => GetDataPresent(format, autoConvert: true);

        public bool GetDataPresent(Type format) => GetDataPresent(format.FullName!);

        public bool GetDataPresent(string format, bool autoConvert)
        {
            if (autoConvert)
            {
                string[] formats = GetFormats(autoConvert);

                for (int i = 0; i < formats.Length; i++)
                {
                    if (format == formats[i])
                    {
                        return true;
                    }
                }

                return false;
            }

            return _data.ContainsKey(format);
        }

        public string[] GetFormats() => GetFormats(autoConvert: true);

        public string[] GetFormats(bool autoConvert)
        {
            bool serializationCheckFailedForThisFunction = false;

            string[] definedFormats = new string[_data.Keys.Count];
            _data.Keys.CopyTo(definedFormats, 0);

            if (!autoConvert)
            {
                return definedFormats;
            }

            List<string> formats = [];

            for (int i = 0; i < definedFormats.Length; i++)
            {
                DataStoreEntry current = _data[definedFormats[i]]!;

                if (!current.AutoConvert)
                {
                    if (!serializationCheckFailedForThisFunction)
                    {
                        formats.Add(definedFormats[i]);
                    }
                }
                else
                {
                    string[] mappedFormats = GetMappedFormats(definedFormats[i]);
                    for (int mappedFormatIndex = 0; mappedFormatIndex < mappedFormats.Length; mappedFormatIndex++)
                    {
                        bool anySerializationFailure = false;
                        if (IsFormatAndDataSerializable(mappedFormats[mappedFormatIndex], current.Data)
                            && serializationCheckFailedForThisFunction)
                        {
                            serializationCheckFailedForThisFunction = true;
                            anySerializationFailure = true;
                        }

                        if (!anySerializationFailure)
                        {
                            formats.Add(mappedFormats[mappedFormatIndex]);
                        }
                    }
                }
            }

            return GetDistinctStrings(formats);
        }

        public void SetData(object? data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data is ISerializable && !_data.ContainsKey(DataFormats.Serializable))
            {
                SetData(DataFormats.Serializable, data);
            }

            SetData(data.GetType(), data);
        }

        public void SetData(string format, object? data) => SetData(format, data, autoConvert: true);

        public void SetData(Type format, object? data) => SetData(format.FullName!, data);

        public void SetData(string format, object? data, bool autoConvert)
        {
            // We do not have proper support for Dibs, so if the user explicitly asked
            // for Dib and provided a Bitmap object we can't convert.  Instead, publish as an HBITMAP
            // and let the system provide the conversion for us.
            if (format == DataFormats.Dib && autoConvert && (SystemDrawingHelper.IsBitmap(data) || data is BitmapSource))
            {
                format = DataFormats.Bitmap;
            }

            _data[format] = new(data, autoConvert);
        }
    }
}
