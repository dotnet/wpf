// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Security;
    using MS.Internal.PrintWin32Thunk;

    /// <summary>
    /// Helper class to obtain printer capabilities from the WINSPOOL API
    /// </summary>
    internal class WinSpoolPrinterCapabilities
    {
        public WinSpoolPrinterCapabilities(string deviceName, string driverName, string portName, DevMode devMode)
        {
            this._deviceName = deviceName;
            this._driverName = driverName;
            this._portName = portName;
            if (devMode != null)
            {
                SafeMemoryHandle buffer = SafeMemoryHandle.Create(devMode.ByteData.Length);
                buffer.CopyFromArray(devMode.ByteData, 0, devMode.ByteData.Length);
                this._devMode = buffer;
            }
            else
            {
                this._devMode = SafeMemoryHandle.Null;
            }
        }

        public void Release()
        {
            if (this._devMode != null)
            {
                this._devMode.Dispose();
                this._devMode = null;
            }
        }

        /// <summary>
        /// Gets printer settings from GetDeviceCaps
        /// </summary>
        /// <param name="logicalPixelsX">LOGPIXELSX device capability</param>
        /// <param name="logicalPixelsY">LOGPIXELSY device capability</param>
        /// <param name="physicalWidth">PHYSICALWIDTH device capability</param>
        /// <param name="physicalHeight">PHYSICALHEIGHT device capability</param>
        /// <param name="physicalOffsetX">PHYSICALOFFSETX device capability</param>
        /// <param name="physicalOffsetY">PHYSICALOFFSETY device capability</param>
        /// <param name="horizontalResolution">HORZRES device capability</param>
        /// <param name="verticalResolution">VERTRES device capability</param>
        /// <returns>True if the call succedes</returns>
        public bool TryGetDeviceCapabilities(
            out int logicalPixelsX, out int logicalPixelsY,
            out int physicalWidth, out int physicalHeight,
            out int physicalOffsetX, out int physicalOffsetY,
            out int horizontalResolution, out int verticalResolution)
        {
            IntPtr hdc = UnsafeNativeMethods.CreateICW(this._driverName, this._deviceName, null, this._devMode);
            if (hdc != IntPtr.Zero)
            {
                HandleRef hdcRef = new HandleRef(this, hdc);
                
                try
                {
                    logicalPixelsX = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.LOGPIXELSX);
                    logicalPixelsY = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.LOGPIXELSY);
                    physicalWidth = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.PHYSICALWIDTH);
                    physicalHeight = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.PHYSICALHEIGHT);
                    physicalOffsetX = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.PHYSICALOFFSETX);
                    physicalOffsetY = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.PHYSICALOFFSETY);
                    horizontalResolution = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.HORZRES);
                    verticalResolution = UnsafeNativeMethods.GetDeviceCaps(hdcRef, DeviceCap.VERTRES);
                    return true;
                }
                finally
                {
                     UnsafeNativeMethods.DeleteDC(hdcRef);
                }
            }
            else
            {
                logicalPixelsX = 0;
                logicalPixelsY = 0;
                physicalWidth = 0;
                physicalHeight = 0;
                physicalOffsetX = 0;
                physicalOffsetY = 0;
                horizontalResolution = 0;
                verticalResolution = 0;
                return false;
            }
        }
    
        /// <summary>
        /// Get the minimum page width and height
        /// </summary>
        /// <param name="minPageWidth">Is set to the minimum page width</param>
        /// <param name="minPageHeight">Is set to the minimum page height</param>
        public void GetMinExtent(out int minPageWidth, out int minPageHeight)
        {
            uint minExtent = GetIntCapability(DeviceCapability.DC_MINEXTENT);
            minPageWidth = (int)(minExtent & 0x0000FFFF);
            minPageHeight = (int)((minExtent & 0xFFFF0000) >> 16);
        }

        /// <summary>
        /// Get the maximum page width and height
        /// </summary>
        /// <param name="maxPaperWidth">Is set to the maximum page width</param>
        /// <param name="maxPaperHeight">Is set to the maximum page height</param>
        public void GetMaxExtent(out int maxPaperWidth, out int maxPaperHeight)
        {
            uint maxExtent = GetIntCapability(DeviceCapability.DC_MAXEXTENT);
            maxPaperWidth = (int)(maxExtent & 0x0000FFFF);
            maxPaperHeight = (int)((maxExtent & 0xFFFF0000) >> 16);
        }

        /// <summary>
        /// Get the DC_COLLATE capability
        /// </summary>
        public bool CanCollate
        {
            get { return GetBoolCapability(DeviceCapability.DC_COLLATE); }
        }

        /// <summary>
        /// Get DC_COLORDEVICE the capability
        /// </summary>
        public bool HasColor
        {
            get { return GetBoolCapability(DeviceCapability.DC_COLORDEVICE); }
        }

        /// <summary>
        /// Get the minimum number of copies the device can print per job
        /// </summary>
        public int MinCopies
        {
            get { return 1; }
        }

        /// <summary>
        /// Get DC_COPIES the capability
        /// </summary>
        public int MaxCopies
        {
            get { return (int)GetIntCapability(DeviceCapability.DC_COPIES); }
        }

        /// <summary>
        /// Get DC_DUPLEX the capability
        /// </summary>
        public bool CanDuplex
        {
            get { return GetBoolCapability(DeviceCapability.DC_DUPLEX); }
        }

        /// <summary>
        /// Get DM_ICMINTENT the capability
        /// </summary>
        public bool HasICMIntent
        {
            get { return GetBoolCapability(DevModeFields.DM_ICMINTENT); }
        }

        /// <summary>
        /// Get DM_ICMMETHOD the capability
        /// </summary>
        public bool HasICMMethod
        {
            get { return GetBoolCapability(DevModeFields.DM_ICMMETHOD); }
        }

        /// <summary>
        /// Get the array of DC_BINS capability
        /// </summary>
        public IList<short> Bins
        {
            get { return GetArrayCapability<short>(DeviceCapability.DC_BINS, ReadWORDArray); }
        }

        /// <summary>
        /// Get the array of DC_BINNAMES capability
        /// </summary>
        public IList<string> BinNames
        {
            get 
            {
                // 24 chars, 2 bytes per char
                return GetArrayCapability<string>(DeviceCapability.DC_BINNAMES, ReadUnicodeStringArray, 24 * 2); 
            }
        }

        public IList<uint> NUp
        {
            get { return GetArrayCapability<uint>(DeviceCapability.DC_NUP, ReadDWORDArray); }
        }

        /// <summary>
        /// Get the array of DC_PAPERS capability
        /// </summary>
        public IList<short> Papers
        {
            get { return GetArrayCapability<short>(DeviceCapability.DC_PAPERS, ReadWORDArray); }
        }

        /// <summary>
        /// Get the array of DC_PAPERNAMES capability
        /// </summary>
        public IList<string> PaperNames
        {
            get
            {
                // 64 chars, 2 bytes per char
                return GetArrayCapability<string>(DeviceCapability.DC_PAPERNAMES, ReadUnicodeStringArray, 64 * 2); 
            }
        }

        /// <summary>
        /// Get the array of DC_PAPERSIZE capability
        /// </summary>
        public IList<DC_PAPER_SIZE> PaperSizes
        {
            get { return GetArrayCapability<DC_PAPER_SIZE>(DeviceCapability.DC_PAPERSIZE, ReadDC_PAPER_SIZEArray); }
        }

        /// <summary>
        /// Get the array of DC_PAPERSIZE capability
        /// </summary>
        public IList<uint> MediaTypes
        {
            get { return GetArrayCapability<uint>(DeviceCapability.DC_MEDIATYPES, ReadDWORDArray); }
        }

        /// <summary>
        /// Get the array of DC_MEDIATYPENAMES capability
        /// </summary>
        public IList<string> MediaTypeNames
        {
            get 
            {
                // 64 chars, 2 bytes per char
                return GetArrayCapability<string>(DeviceCapability.DC_MEDIATYPENAMES, ReadUnicodeStringArray, 64 * 2); 
            }
        }

        /// <summary>
        /// Get the DC_ORIENTATION capability
        /// </summary>
        public int LandscapeOrientation
        {
            get { return (int)GetIntCapability(DeviceCapability.DC_ORIENTATION); }
        }

        /// <summary>
        /// Get the array of DC_ENUMRESOLUTIONS capability
        /// </summary>
        public IList<DC_RESOLUTION> Resolutions
        {
            get { return GetArrayCapability<DC_RESOLUTION>(DeviceCapability.DC_ENUMRESOLUTIONS, ReadDC_RESOLUTIONArray); }
        }

        /// <summary>
        /// Get the DM_SCALE capability
        /// </summary>
        public bool CanScale
        {
            get { return GetBoolCapability(DevModeFields.DM_SCALE); }
        }

        /// <summary>
        /// Get the DC_TRUETYPE capability
        /// </summary>
        public bool TrueType
        {
            get { return GetBoolCapability(DeviceCapability.DC_TRUETYPE); }
        }


        public DevModeFields Fields
        {
            get
            {
                return (DevModeFields)GetIntCapability(DeviceCapability.DC_FIELDS);
            }
        }

        /// <summary>
        /// Get the default paper width and height
        /// </summary>
        /// <param name="defaultDevMode">devMode that contains the code for the default paper size</param>
        /// <param name="mediaSizeCodes">List of supported paperSizeCodes</param>
        /// <param name="mediaSizes">List of supported paperSize widths and heights</param>
        /// <param name="defaultPaperSize">Out parameter that recieves the default paper width and height</param>
        /// <returns>True if the call succeded</returns>
        public bool GetDefaultPaperSize(DevMode defaultDevMode, IList<short> paperSizeCodes, IList<DC_PAPER_SIZE> paperSizes, out DC_PAPER_SIZE defaultPaperSize)
        {
            defaultPaperSize = new DC_PAPER_SIZE();

            if (defaultDevMode == null)
            {
                if (paperSizes.Count > 0)
                {
                    defaultPaperSize = paperSizes[0];
                    return true;
                }

                return false;
            }
            else
            {
                bool hasWidth = false;
                hasWidth = defaultDevMode.IsFieldSet(DevModeFields.DM_PAPERWIDTH);
                if (hasWidth)
                {
                    defaultPaperSize.Width = defaultDevMode.PaperWidth;
                }

                bool hasHeight = false;
                hasHeight = defaultDevMode.IsFieldSet(DevModeFields.DM_PAPERWIDTH);
                if (hasHeight)
                {
                    defaultPaperSize.Height = defaultDevMode.PaperLength;
                }

                if (!hasWidth || !hasHeight)
                {
                    if (defaultDevMode.IsFieldSet(DevModeFields.DM_PAPERSIZE))
                    {
                        int defaultPaperSizeIndex = paperSizeCodes.IndexOf(defaultDevMode.PaperSize);
                        if (0 <= defaultPaperSizeIndex && defaultPaperSizeIndex < paperSizes.Count)
                        {
                            if (!hasWidth)
                            {
                                defaultPaperSize.Width = paperSizes[defaultPaperSizeIndex].Width;
                            }

                            if (!hasHeight)
                            {
                                defaultPaperSize.Height = paperSizes[defaultPaperSizeIndex].Height;
                            }

                            hasWidth = hasHeight = true;
                        }
                    }
                }

                return hasWidth && hasHeight;
            }
        }

        private bool GetBoolCapability(DeviceCapability capability)
        {
            return 0 != GetIntCapability(capability);
        }

        private bool GetBoolCapability(DevModeFields capability)
        {
            // Several capabilities are packed into the DC_FIELDS capability
            // Call it once and cache the result to save on extra calls to GetIntCapability
            if (!this._dmFieldsIsCached)
            {
                this._dmFields = (DevModeFields)GetIntCapability(DeviceCapability.DC_FIELDS);
                this._dmFieldsIsCached = true;
            }

            return (capability & this._dmFields) == capability;
        }

        /// <summary>
        /// Gets a device capability represented as an integer
        /// </summary>
        /// <param name="capability">Capability to retrieve</param>
        /// <returns>value of capability specified in capability argument</returns>
        private uint GetIntCapability(DeviceCapability capability)
        {
            return UnsafeNativeMethods.DeviceCapabilitiesW(this._deviceName, this._portName, capability, SafeMemoryHandle.Null, this._devMode);
        }

        /// <summary>
        /// Gets a device capability represented as an array
        /// </summary>
        /// <typeparam name="T">Capability item type</typeparam>
        /// <param name="capability">Capability to retrieve</param>
        /// <param name="readItem">Delegate to marshal a single capability item from unmanaged memory</param>
        /// <returns>An array of capabilities specified by the capability arg or null if the call fails</returns>
        private T[] GetArrayCapability<T>(DeviceCapability capability, ReadArray<T> readItem) where T : struct
        {
            return GetArrayCapability<T>(capability, readItem, Marshal.SizeOf(typeof(T)));
        }

        /// <summary>
        /// Delegate to marshal a single capability item from unmanaged memory
        /// </summary>
        /// <typeparam name="T">Capability item type</typeparam>
        /// <param name="src">Pointer to unmanaged memory</param>
        /// <param name="itemByteSize">Bytes per item</param>
        /// <returns>Capability marshaled from src</returns>
        private delegate T[] ReadArray<T>(HGlobalBuffer buffer, int itemByteSize);

        /// <summary>
        /// Gets a device capability represented as an array
        /// </summary>
        /// <param name="capability">Capability to retrieve</param>
        /// <returns>value of capability specified in capability argument</returns>
        private T[] GetArrayCapability<T>(DeviceCapability capability, ReadArray<T> readArray, int itemByteSize)
        {
            uint numOutputs = UnsafeNativeMethods.DeviceCapabilitiesW(this._deviceName, this._portName, capability, SafeMemoryHandle.Null, this._devMode);

            if (numOutputs < 1)
            {
                return Array.Empty<T>();
            }

            HGlobalBuffer buffer = new HGlobalBuffer((int)(numOutputs * itemByteSize));
            try
            {
                numOutputs = UnsafeNativeMethods.DeviceCapabilitiesW(this._deviceName, this._portName, capability, buffer.Handle, this._devMode);
                if (numOutputs >= 0)
                {
                    return readArray(buffer, itemByteSize);
                }
            }
            finally
            {
                buffer.Release();
            }

            return null;
        }

        private uint[] ReadDWORDArray(HGlobalBuffer buffer, int itemByteSize)
        {
            int nItems = buffer.Length / itemByteSize;            
            uint[] result = new uint[nItems];

            bool shouldRelease = false;
            buffer.Handle.DangerousAddRef(ref shouldRelease);
            try 
            {
                IntPtr baseAddr = buffer.Handle.DangerousGetHandle();
                for (int i = 0, offset = 0; i < nItems; i++, offset += itemByteSize)
                {
                    result[i] = (uint)Marshal.ReadInt32(baseAddr, offset);
                }
                
                return result;
            }
            finally
            {
                if(shouldRelease)
                {
                    buffer.Handle.DangerousRelease();
                }
            }
        }

        private short[] ReadWORDArray(HGlobalBuffer buffer, int itemByteSize)
        {
            int nItems = buffer.Length / itemByteSize;
            short[] result = new short[nItems];

            bool shouldRelease = false;
            buffer.Handle.DangerousAddRef(ref shouldRelease);
            try 
            {
                IntPtr baseAddr = buffer.Handle.DangerousGetHandle();
                for (int i = 0, offset = 0; i < nItems; i++, offset += itemByteSize)
                {
                    result[i] = Marshal.ReadInt16(baseAddr, offset);
                }

                return result;
            }
            finally
            {
                if(shouldRelease)
                {
                    buffer.Handle.DangerousRelease();
                }
            }
        }

        private static DC_RESOLUTION[] ReadDC_RESOLUTIONArray(HGlobalBuffer buffer, int itemByteSize)
        {
            int nItems = buffer.Length / itemByteSize;
            DC_RESOLUTION[] result = new DC_RESOLUTION[nItems];

            bool shouldRelease = false;
            buffer.Handle.DangerousAddRef(ref shouldRelease);
            try 
            {
                IntPtr baseAddr = buffer.Handle.DangerousGetHandle();
                for (int i = 0, offset = 0; i < nItems; i++, offset += itemByteSize)
                {
                    int x = Marshal.ReadInt32(baseAddr, offset);
                    int y = Marshal.ReadInt32(baseAddr, offset + (itemByteSize / 2));
                    result[i] = new DC_RESOLUTION(x, y);
                }

                return result;
            }
            finally
            {
                if(shouldRelease)
                {
                    buffer.Handle.DangerousRelease();
                }
            }
        }

        private static DC_PAPER_SIZE[] ReadDC_PAPER_SIZEArray(HGlobalBuffer buffer, int itemByteSize)
        {
            int nItems = buffer.Length / itemByteSize;
            DC_PAPER_SIZE[] result = new DC_PAPER_SIZE[nItems];

            bool shouldRelease = false;
            buffer.Handle.DangerousAddRef(ref shouldRelease);
            try 
            {
                IntPtr baseAddr = buffer.Handle.DangerousGetHandle();
                for (int i = 0, offset = 0; i < nItems; i++, offset += itemByteSize)
                {
                    int w = Marshal.ReadInt32(baseAddr, offset);
                    int h = Marshal.ReadInt32(baseAddr, offset + (itemByteSize / 2));
                    result[i] = new DC_PAPER_SIZE(w, h);
                }

                return result;
            }
            finally
            {
                if(shouldRelease)
                {
                    buffer.Handle.DangerousRelease();
                }
            }
        }

        private static string [] ReadUnicodeStringArray(HGlobalBuffer buffer, int itemByteSize)
        {
            int nItems = buffer.Length / itemByteSize;
            string[] result = new string[nItems];

            bool shouldRelease = false;
            buffer.Handle.DangerousAddRef(ref shouldRelease);
            try 
            {
                IntPtr baseAddr = buffer.Handle.DangerousGetHandle();
                for (int i = 0, offset = 0; i < nItems; i++, offset += itemByteSize)
                {
                    IntPtr strAddr = new IntPtr(baseAddr.ToInt64() + offset);
                    string str = Marshal.PtrToStringUni(strAddr, itemByteSize / 2);

                    //Marshal.PtrToStringUni(IntPtr) Copies until '/0'
                    //Marshal.PtrToStringUni(IntPtr, int len) Copies all characters in the string including any '\0'
                    // What we want is to copy until '/0' but no more than len chars

                    int index = str.IndexOf('\0');
                    if (index >= 0)
                    {
                        str = str.Substring(0, index);
                    }
                    result[i] = str;
                }

                return result;
            }
            finally
            {
                if(shouldRelease)
                {
                    buffer.Handle.DangerousRelease();
                }
            }
        }

        #region Private Fields

        private bool _dmFieldsIsCached;

        private string _deviceName;

        private string _driverName;

        private string _portName;

        private DevModeFields _dmFields;

        SafeMemoryHandle _devMode;

        #endregion
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct DC_PAPER_SIZE
    {
        public DC_PAPER_SIZE(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct DC_RESOLUTION
    {
        public DC_RESOLUTION(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int x;
        public int y;
    }
}
