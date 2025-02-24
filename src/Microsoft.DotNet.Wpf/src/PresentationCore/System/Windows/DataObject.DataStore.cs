// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using MS.Internal;

namespace System.Windows;

public sealed partial class DataObject
{
    /// <summary>
    /// DataStore
    /// </summary>
    private partial class DataStore : IDataObject
    {
        // Data hash table.
        private Hashtable _data = new Hashtable();

        public DataStore()
        {
        }

        public Object GetData(string format)
        {
            return GetData(format, true);
        }

        public Object GetData(Type format)
        {
            return GetData(format.FullName);
        }

        public Object GetData(string format, bool autoConvert)
        {
            return GetData(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
        }

        public bool GetDataPresent(string format)
        {
            return GetDataPresent(format, true);
        }

        public bool GetDataPresent(Type format)
        {
            return GetDataPresent(format.FullName);
        }

        public bool GetDataPresent(string format, bool autoConvert)
        {
            return GetDataPresent(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
        }

        public string[] GetFormats()
        {
            return GetFormats(true);
        }

        public string[] GetFormats(bool autoConvert)
        {
            string[] baseVar;

            //************************************************************
            // important! this is cached security information. It will
            //            remain valid for this function, but do not
            //            let this value leak outside of this function.
            //
            bool serializationCheckFailedForThisFunction = false;

            baseVar = new string[_data.Keys.Count];

            _data.Keys.CopyTo(baseVar, 0);

            if (autoConvert)
            {
                List<string> formats = [];

                for (int baseFormatIndex = 0; baseFormatIndex < baseVar.Length; baseFormatIndex++)
                {
                    DataStoreEntry[] entries;
                    bool canAutoConvert;

                    entries = (DataStoreEntry[])_data[baseVar[baseFormatIndex]];
                    canAutoConvert = true;

                    for (int dataStoreIndex = 0; dataStoreIndex < entries.Length; dataStoreIndex++)
                    {
                        if (!entries[dataStoreIndex].AutoConvert)
                        {
                            canAutoConvert = false;
                            break;
                        }
                    }

                    if (canAutoConvert)
                    {
                        string[] cur;

                        cur = GetMappedFormats(baseVar[baseFormatIndex]);
                        for (int mappedFormatIndex = 0; mappedFormatIndex < cur.Length; mappedFormatIndex++)
                        {
                            bool anySerializationFailure = false;
                            for (int dataStoreIndex = 0;
                                !anySerializationFailure
                                  &&
                                dataStoreIndex < entries.Length;
                                dataStoreIndex++)
                            {
                                if (DataObject.IsFormatAndDataSerializable(cur[mappedFormatIndex], entries[dataStoreIndex].Data))
                                {
                                    if (serializationCheckFailedForThisFunction)
                                    {
                                        serializationCheckFailedForThisFunction = true;
                                        anySerializationFailure = true;
                                    }
                                }
                            }
                            if (!anySerializationFailure)
                            {
                                formats.Add(cur[mappedFormatIndex]);
                            }
}
                    }
                    else
                    {
                         if (!serializationCheckFailedForThisFunction)
                         {
                            formats.Add(baseVar[baseFormatIndex]);
                         }
                    }
                }

                baseVar = GetDistinctStrings(formats);
            }

            return baseVar;
        }

        public void SetData(Object data)
        {
            if (data is ISerializable
                && !this._data.ContainsKey(DataFormats.Serializable))
            {
                SetData(DataFormats.Serializable, data);
            }

            SetData(data.GetType(), data);
        }

        public void SetData(string format, Object data)
        {
            SetData(format, data, true);
        }

        public void SetData(Type format, Object data)
        {
            SetData(format.FullName, data);
        }

        public void SetData(string format, Object data, bool autoConvert)
        {
            // We do not have proper support for Dibs, so if the user explicitly asked
            // for Dib and provided a Bitmap object we can't convert.  Instead, publish as an HBITMAP
            // and let the system provide the conversion for us.
            //
            if (IsFormatEqual(format, DataFormats.Dib))
            {
                if (autoConvert && (SystemDrawingHelper.IsBitmap(data) || IsDataSystemBitmapSource(data)))
                {
                    format = DataFormats.Bitmap;
                }
            }

            SetData(format, data, autoConvert, DVASPECT.DVASPECT_CONTENT, 0);
        }

        private Object GetData(string format, bool autoConvert, DVASPECT aspect, int index)
        {
            Object baseVar;
            Object original;
            DataStoreEntry dataStoreEntry;

            dataStoreEntry = FindDataStoreEntry(format, aspect, index);

            baseVar = GetDataFromDataStoreEntry(dataStoreEntry, format);

            original = baseVar;

            if (autoConvert
                && (dataStoreEntry == null || dataStoreEntry.AutoConvert)
                && (baseVar == null || baseVar is MemoryStream))
            {
                string[] mappedFormats;

                mappedFormats = GetMappedFormats(format);
                if (mappedFormats != null)
                {
                    for (int i = 0; i < mappedFormats.Length; i++)
                    {
                        if (!IsFormatEqual(format, mappedFormats[i]))
                        {
                            DataStoreEntry foundDataStoreEntry;

                            foundDataStoreEntry = FindDataStoreEntry(mappedFormats[i], aspect, index);

                            baseVar = GetDataFromDataStoreEntry(foundDataStoreEntry, mappedFormats[i]);

                            if (baseVar != null && !(baseVar is MemoryStream))
                            {
                                if (IsDataSystemBitmapSource(baseVar) || SystemDrawingHelper.IsBitmap(baseVar))
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
            }

            if (original != null)
            {
                return original;
            }
            else
            {
                return baseVar;
            }
        }

        private bool GetDataPresent(string format, bool autoConvert, DVASPECT aspect, int index)
        {
            if (!autoConvert)
            {
                DataStoreEntry[] entries;
                DataStoreEntry dse;
                DataStoreEntry naturalDse;

                if (!_data.ContainsKey(format))
                {
                    return false;
                }

                entries = (DataStoreEntry[])_data[format];
                dse = null;
                naturalDse = null;

                // Find the entry with the given aspect and index
                for (int i = 0; i < entries.Length; i++)
                {
                    DataStoreEntry entry;

                    entry = entries[i];
                    if (entry.Aspect == aspect)
                    {
                        if (index == -1 || entry.Index == index)
                        {
                            dse = entry;
                            break;
                        }
                    }
                    if (entry.Aspect == DVASPECT.DVASPECT_CONTENT && entry.Index == 0)
                    {
                        naturalDse = entry;
                    }
                }

                // If we couldn't find a specific entry, we'll use
                // aspect == Content and index == 0.
                if (dse == null && naturalDse != null)
                {
                    dse = naturalDse;
                }

                // If we still didn't find data, return false.
                return (dse != null);
            }
            else
            {
                string[] formats;

                formats = GetFormats(autoConvert);
                for (int i = 0; i < formats.Length; i++)
                {
                    if (IsFormatEqual(format, formats[i]))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void SetData(string format, Object data, bool autoConvert, DVASPECT aspect, int index)
        {
            DataStoreEntry dse;
            DataStoreEntry[] datalist;

            dse = new DataStoreEntry(data, autoConvert, aspect, index);
            datalist = (DataStoreEntry[])this._data[format];

            if (datalist == null)
            {
                datalist = new DataStoreEntry[1];
            }
            else
            {
                DataStoreEntry[] newlist;

                newlist = new DataStoreEntry[datalist.Length + 1];
                datalist.CopyTo(newlist, 1);
                datalist = newlist;
            }

            datalist[0] = dse;
            this._data[format] = datalist;
        }

        private DataStoreEntry FindDataStoreEntry(string format, DVASPECT aspect, int index)
        {
            DataStoreEntry[] dataStoreEntries;
            DataStoreEntry dataStoreEntry;
            DataStoreEntry naturalDataStoreEntry;

            dataStoreEntries = (DataStoreEntry[])_data[format];
            dataStoreEntry = null;
            naturalDataStoreEntry = null;

            // Find the entry with the given aspect and index
            if (dataStoreEntries != null)
            {
                for (int i = 0; i < dataStoreEntries.Length; i++)
                {
                    DataStoreEntry entry;

                    entry = dataStoreEntries[i];
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

            // If we couldn't find a specific entry, we'll use
            // aspect == Content and index == 0.
            if (dataStoreEntry == null && naturalDataStoreEntry != null)
            {
                dataStoreEntry = naturalDataStoreEntry;
            }

            return dataStoreEntry;
        }

        private Object GetDataFromDataStoreEntry(DataStoreEntry dataStoreEntry, string format)
        {
            Object data;

            data = null;
            if (dataStoreEntry != null)
            {
                data = dataStoreEntry.Data;
            }

            return data;
        }
    }
}
