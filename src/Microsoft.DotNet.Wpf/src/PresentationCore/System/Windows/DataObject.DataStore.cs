// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using MS.Internal;

namespace System.Windows;

public sealed partial class DataObject
{
    private partial class DataStore : IDataObject
    {
        // Data hash table.
        private readonly Hashtable _data = [];

        public DataStore()
        {
        }

        public object? GetData(string format) => GetData(format, autoConvert: true);

        public object? GetData(Type format) => GetData(format.FullName!);

        public object? GetData(string format, bool autoConvert) => GetData(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);

        public bool GetDataPresent(string format) => GetDataPresent(format, autoConvert: true);

        public bool GetDataPresent(Type format) => GetDataPresent(format.FullName!);

        public bool GetDataPresent(string format, bool autoConvert) => GetDataPresent(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);

        public string[] GetFormats() => GetFormats(autoConvert: true);

        public string[] GetFormats(bool autoConvert)
        {
            bool serializationCheckFailedForThisFunction = false;

            string[] baseVar = new string[_data.Keys.Count];

            _data.Keys.CopyTo(baseVar, 0);

            if (!autoConvert)
            {
                return baseVar;
            }

            List<string> formats = [];

            for (int baseFormatIndex = 0; baseFormatIndex < baseVar.Length; baseFormatIndex++)
            {
                DataStoreEntry[] entries = (DataStoreEntry[])_data[baseVar[baseFormatIndex]]!;
                bool canAutoConvert = true;

                for (int dataStoreIndex = 0; dataStoreIndex < entries.Length; dataStoreIndex++)
                {
                    if (!entries[dataStoreIndex].AutoConvert)
                    {
                        canAutoConvert = false;
                        break;
                    }
                }

                if (!canAutoConvert)
                {
                    if (!serializationCheckFailedForThisFunction)
                    {
                        formats.Add(baseVar[baseFormatIndex]);
                    }
                }
                else
                {
                    string[] cur = GetMappedFormats(baseVar[baseFormatIndex]);
                    for (int mappedFormatIndex = 0; mappedFormatIndex < cur.Length; mappedFormatIndex++)
                    {
                        bool anySerializationFailure = false;
                        for (int dataStoreIndex = 0; !anySerializationFailure && dataStoreIndex < entries.Length; dataStoreIndex++)
                        {
                            if (IsFormatAndDataSerializable(cur[mappedFormatIndex], entries[dataStoreIndex].Data)
                                && serializationCheckFailedForThisFunction)
                            {
                                serializationCheckFailedForThisFunction = true;
                                anySerializationFailure = true;
                            }
                        }

                        if (!anySerializationFailure)
                        {
                            formats.Add(cur[mappedFormatIndex]);
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

            SetData(format, data, autoConvert, DVASPECT.DVASPECT_CONTENT, 0);
        }

        private object? GetData(string format, bool autoConvert, DVASPECT aspect, int index)
        {
            DataStoreEntry? dataStoreEntry = FindDataStoreEntry(format, aspect, index);
            object? baseVar = dataStoreEntry?.Data;
            object? original = baseVar;

            if (!autoConvert
                || (dataStoreEntry is not null && !dataStoreEntry.AutoConvert)
                || (baseVar is not null && baseVar is not MemoryStream))
            {
                return original ?? baseVar;
            }

            if (GetMappedFormats(format) is { } mappedFormats)
            {
                for (int i = 0; i < mappedFormats.Length; i++)
                {
                    if (format != mappedFormats[i])
                    {
                        DataStoreEntry? foundDataStoreEntry = FindDataStoreEntry(mappedFormats[i], aspect, index);

                        baseVar = foundDataStoreEntry?.Data;

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
            }

            return original ?? baseVar;
        }

        private bool GetDataPresent(string format, bool autoConvert, DVASPECT aspect, int index)
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

            if (!_data.ContainsKey(format))
            {
                return false;
            }

            DataStoreEntry[] entries = (DataStoreEntry[])_data[format]!;
            DataStoreEntry? dse = null;
            DataStoreEntry? naturalDse = null;

            // Find the entry with the given aspect and index
            for (int i = 0; i < entries.Length; i++)
            {
                DataStoreEntry entry = entries[i];

                if (entry.Aspect == aspect && (index == -1 || entry.Index == index))
                {
                    dse = entry;
                    break;
                }

                if (entry.Aspect == DVASPECT.DVASPECT_CONTENT && entry.Index == 0)
                {
                    naturalDse = entry;
                }
            }

            // If we couldn't find a specific entry, we'll use aspect == Content and index == 0.
            if (dse is null && naturalDse is not null)
            {
                dse = naturalDse;
            }

            // If we still didn't find data, return false.
            return dse is not null;
        }

        private void SetData(string format, object? data, bool autoConvert, DVASPECT aspect, int index)
        {
            DataStoreEntry dse = new DataStoreEntry(data, autoConvert, aspect, index);

            if (_data[format] is not DataStoreEntry[] datalist)
            {
                datalist = new DataStoreEntry[1];
            }
            else
            {
                DataStoreEntry[] newlist = new DataStoreEntry[datalist.Length + 1];
                datalist.CopyTo(newlist, 1);
                datalist = newlist;
            }

            datalist[0] = dse;
            _data[format] = datalist;
        }

        private DataStoreEntry? FindDataStoreEntry(string format, DVASPECT aspect, int index)
        {
            DataStoreEntry[]? dataStoreEntries = _data[format] as DataStoreEntry[];
            DataStoreEntry? dataStoreEntry = null;
            DataStoreEntry? naturalDataStoreEntry = null;

            // Find the entry with the given aspect and index
            if (dataStoreEntries is not null)
            {
                for (int i = 0; i < dataStoreEntries.Length; i++)
                {
                    DataStoreEntry entry = dataStoreEntries[i];
                    if (entry.Aspect == aspect)
                    {
                        if (index == -1 || entry.Index == index)
                        {
                            dataStoreEntry = entry;
                            break;
                        }
                    }

                    if (entry.Aspect == DVASPECT.DVASPECT_CONTENT && entry.Index == 0)
                    {
                        naturalDataStoreEntry = entry;
                    }
                }
            }

            // If we couldn't find a specific entry, we'll use aspect == Content and index == 0.
            if (dataStoreEntry is null && naturalDataStoreEntry is not null)
            {
                dataStoreEntry = naturalDataStoreEntry;
            }

            return dataStoreEntry;
        }
    }
}
