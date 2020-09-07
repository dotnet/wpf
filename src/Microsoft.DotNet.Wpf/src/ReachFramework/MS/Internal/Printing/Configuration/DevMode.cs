// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Security;

    /// <remarks>
    /// Helper class to interpret public DEVMODEA and DEVMODEW fields packed in a byte array
    /// This class only deals with DEVMODE fields relevant to printing
    /// From http://msdn.microsoft.com/en-us/library/dd183565(VS.85).aspx
    /// Note this class is not a DEVMODE per-se, It is a utility wrapper on byte [] which is the devmode
    /// </remarks>
    internal class DevMode
    {
        public DevMode()
        {
            this._isDevModeW = true;
        }

        public DevMode(byte[] devModeBytes)
        {
            if (devModeBytes == null)
            {
                throw new ArgumentNullException("devModeBytes");
            }

            this._isDevModeW = true;
            this.ByteData = devModeBytes;
        }

        #region Public Properties

        /// <summary>
        /// DEVMODE bytes
        /// </summary>
        public byte[] ByteData
        {
            get
            {
                EnsureInitialized();
                return this._byteData;
            }

            private set
            {
                this._byteData = value;

                if (HasValidSize(this._isDevModeW))
                {
                    return;
                }

                // size check may have failed because the byte [] is a DEVMODEA not a DEVMODEW
                // try again using assuming a DEVMODEA
                if (HasValidSize(!this._isDevModeW))
                {
                    this._isDevModeW = !this._isDevModeW;
                }
            }
        }

        /// <summary>
        /// String that specifies the friendly name of the printer. 
        /// </summary>
        public string DeviceName
        {
            get { return ReadChars(dmDeviceNameByteOffset, CCHDEVICENAME); }
            set { WriteChars(dmDeviceNameByteOffset, CCHDEVICENAME, value); }
        }

        public ushort SpecVersion
        {
            get { return ReadWORD(dmSpecVersionByteOffset); }
            private set { WriteWORD(dmSpecVersionByteOffset, value); }
        }

        public ushort DriverVersion
        {
            get { return ReadWORD(dmDriverVersionByteOffset); }
        }

        /// <summary>
        /// Specifies the size, in bytes, of the DEVMODE structure, 
        /// not including any private driver-specific data 
        /// that can follow the public members of the structure.
        /// </summary>
        public ushort Size
        {
            get { return ReadWORD(dmSizeByteOffset); }
            private set { WriteWORD(dmSizeByteOffset, value); }
        }

        /// <summary>
        /// Contains the number of bytes of private driver-data that follow this structure.
        /// If a device driver does not use device-specific information, set this member to zero.
        /// </summary>
        public ushort DriverExtra
        {
            get { return ReadWORD(dmDriverExtraByteOffset); }
            set { WriteWORD(dmDriverExtraByteOffset, value); }
        }

        /// <summary>
        /// DWORD that specifies whether certain members of the DEVMODE structure have been initialized.
        /// If a member is initialized, its corresponding bit is set, otherwise the bit is clear. 
        /// </summary>
        public DevModeFields Fields
        {
            get { return (DevModeFields)ReadDWORD(dmFieldsByteOffset); }
            set { WriteDWORD(dmFieldsByteOffset, (uint)value); }
        }

        /// <summary>
        /// Short integer that specifies the orientation of the paper for printer devices. 
        /// </summary>
        public DevModeOrientation Orientation
        {
            get { return (DevModeOrientation)ReadWORD(dmOrientationByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_ORIENTATION);
                WriteWORD(dmOrientationByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Short integer that specifies the size of the paper for printer devices. 
        /// </summary>
        /// <remarks>See DevModePaperSizes for a list of standard paper sizes</remarks>
        public short PaperSize
        {
            get { return (short)ReadWORD(dmPaperSizeByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_PAPERSIZE);
                WriteWORD(dmPaperSizeByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Overrides the length of the paper specified by the dmPaperSize member.
        /// </summary>        
        /// <remarks>The unit is a tenth of a millimeter</remarks>
        public short PaperLength
        {
            get { return (short)ReadWORD(dmPaperLengthByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_PAPERLENGTH);
                WriteWORD(dmPaperLengthByteOffset, (ushort)value);
            }
        }

        /// <su mmary>
        /// Overrides the width of the paper specified by the dmPaperSize member.
        /// </summary>
        /// <remarks>The unit is a tenth of a millimeter</remarks>
        public short PaperWidth
        {
            get { return (short)ReadWORD(dmPaperWidthByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_PAPERWIDTH);
                WriteWORD(dmPaperWidthByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Specifies the factor by which the printed output is to be scaled by a factor of dmScale / 100.
        /// </summary>
        public short Scale
        {
            get { return (short)ReadWORD(dmScaleByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_SCALE);
                WriteWORD(dmScaleByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Short integer that specifies the number of copies that you want to print if the device supports multiple-page copies.
        /// </summary>
        public short Copies
        {
            get { return (short)ReadWORD(dmCopiesByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_COPIES);
                WriteWORD(dmCopiesByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Specifies the paper source. 
        /// </summary>
        /// <remarks>See DevModePaperSources for standard paper sources</remarks>
        public short DefaultSource
        {
            get { return (short)ReadWORD(dmDefaultSourceByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_DEFAULTSOURCE);
                WriteWORD(dmDefaultSourceByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Specifies the printer resolution.
        /// If a positive value is specified, it specifies the number of dots per inch (DPI) and is therefore device dependent.
        /// </summary>
        /// <remarks>See DevModeResolutions for standard resolutions</remarks>
        public short PrintQuality
        {
            get { return (short)ReadWORD(dmPrintQualityByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_PRINTQUALITY);
                WriteWORD(dmPrintQualityByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Short integer that specifies color or monochrome printing on color printers. 
        /// </summary>
        public DevModeColor Color
        {
            get { return (DevModeColor)ReadWORD(dmColorByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_COLOR);
                WriteWORD(dmColorByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Selects duplex or double-sided printing for printers capable of duplex printing
        /// </summary>
        public DevModeDuplex Duplex
        {
            get { return (DevModeDuplex)ReadWORD(dmDuplexByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_DUPLEX);
                WriteWORD(dmDuplexByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Specifies the y-resolution, in dots per inch, of the printer. 
        /// If the printer initializes this member, the dmPrintQuality member specifies the x-resolution, in dots per inch, of the printer. 
        /// </summary>
        public short YResolution
        {
            get { return (short)ReadWORD(dmYResolutionByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_YRESOLUTION);
                WriteWORD(dmYResolutionByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Specifies how TrueType fonts should be printed.
        /// </summary>
        public DevModeTrueTypeOption TTOption
        {
            get { return (DevModeTrueTypeOption)ReadWORD(dmTTOptionByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_TTOPTION);
                WriteWORD(dmTTOptionByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// Specifies whether collation should be used when printing multiple copies.
        /// </summary>
        public DevModeCollate Collate
        {
            get { return (DevModeCollate)ReadWORD(dmCollateByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_COLLATE);
                WriteWORD(dmCollateByteOffset, (ushort)value);
            }
        }

        /// <summary>
        /// The name of the form to use
        /// </summary>
        public string FormName
        {
            get { return ReadChars(dmFormNameByteOffset, CCHFORMNAME); }
            set
            {
                this.SetField(DevModeFields.DM_FORMNAME);
                WriteChars(dmFormNameByteOffset, CCHFORMNAME, value);
            }
        }

        /// <summary>
        /// Specifies where the NUP is done.
        /// </summary>
        public DevModeNUp Nup
        {
            get { return (DevModeNUp)ReadDWORD(dmNupByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_NUP);
                WriteDWORD(dmNupByteOffset, (uint)value);
            }
        }

        /// <summary>
        /// Windows 95/98/Me; Windows 2000/XP: Specifies how ICM is handled.
        /// </summary>
        public DevModeICMMethod ICMMethod
        {
            get { return (DevModeICMMethod)ReadDWORD(dmICMMethodByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_ICMMETHOD);
                WriteDWORD(dmICMMethodByteOffset, (uint)value);
            }
        }

        /// <summary>
        /// Windows 95/98/Me; Windows 2000/XP: Specifies which color matching method, or intent, should be used by default.
        /// </summary>
        /// <remarks>See DevModeICMIntents for standard values</remarks>
        public uint ICMIntent
        {
            get { return ReadDWORD(dmICMIntentByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_ICMINTENT);
                WriteDWORD(dmICMIntentByteOffset, (uint)value);
            }
        }

        /// <summary>
        /// Windows 95/98/Me; Windows 2000/XP: Specifies the type of media being printed on.
        /// </summary>
        /// <remarks>See DevModeMediaTypes for standard media types</remarks>
        public uint MediaType
        {
            get { return ReadDWORD(dmMediaTypeByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_MEDIATYPE);
                WriteDWORD(dmMediaTypeByteOffset, (uint)value);
            }
        }

        /// <summary>
        /// Windows 95/98/Me; Windows 2000/XP: Specifies how dithering is to be done.
        /// </summary>
        /// <remarks>See DevModeDitherTypes for standard media types</remarks>
        public uint DitherType
        {
            get { return ReadDWORD(dmDitherTypeByteOffset); }
            set
            {
                this.SetField(DevModeFields.DM_DITHERTYPE);
                WriteDWORD(dmDitherTypeByteOffset, (uint)value);
            }
        }

        #endregion

        #region Public Methods

        public void EnsureInitialized()
        {
            if (this._byteData == null)
            {
                this._byteData = new byte[GetVariableByteSize(true) + WINVER_0x0500_FixedByteSize];
                this._isDevModeW = true;
                this.SpecVersion = WINVER_0x0500_DM_SPECVERSION;
                this.Size = (ushort)this._byteData.Length;
            }
        }

        public DevMode Clone()
        {
            DevMode result = new DevMode();

            if (this._byteData != null)
            {
                result._byteData = (byte[])this._byteData.Clone();
            }

            result._isDevModeW = this._isDevModeW;

            return result;
        }

        public bool IsFieldSet(DevModeFields field)
        {
            return (this.Fields & field) == field;
        }

        public bool IsAnyFieldSet(DevModeFields fields)
        {
            return (this.Fields & fields) != 0x00;
        }

        /// <summary>
        /// Copies DevMode fields
        /// </summary>
        /// <param name="src">Source DevMode to copy from</param>
        /// <param name="fields">Fields to copy</param>
        public void Copy(DevMode src, DevModeFields fields)
        {
            if (src == null)
            {
                return;
            }

            Copy(DevModeFields.DM_ORIENTATION, fields, src.Orientation, (DevModeOrientation value) => this.Orientation = value);
            Copy(DevModeFields.DM_PAPERSIZE, fields, src.PaperSize, (short value) => this.PaperSize = value);
            Copy(DevModeFields.DM_PAPERLENGTH, fields, src.PaperLength, (short value) => this.PaperLength = value);
            Copy(DevModeFields.DM_PAPERWIDTH, fields, src.PaperWidth, (short value) => this.PaperWidth = value);
            Copy(DevModeFields.DM_SCALE, fields, src.Scale, (short value) => this.Scale = value);
            Copy(DevModeFields.DM_COPIES, fields, src.Copies, (short value) => this.Copies = value);
            Copy(DevModeFields.DM_DEFAULTSOURCE, fields, src.DefaultSource, (short value) => this.DefaultSource = value);
            Copy(DevModeFields.DM_PRINTQUALITY, fields, src.PrintQuality, (short value) => this.PrintQuality = value);
            Copy(DevModeFields.DM_COLOR, fields, src.Color, (DevModeColor value) => this.Color = value);
            Copy(DevModeFields.DM_DUPLEX, fields, src.Duplex, (DevModeDuplex value) => this.Duplex = value);
            Copy(DevModeFields.DM_YRESOLUTION, fields, src.YResolution, (short value) => this.YResolution = value);
            Copy(DevModeFields.DM_TTOPTION, fields, src.TTOption, (DevModeTrueTypeOption value) => this.TTOption = value);
            Copy(DevModeFields.DM_COLLATE, fields, src.Collate, (DevModeCollate value) => this.Collate = value);
            Copy(DevModeFields.DM_FORMNAME, fields, src.FormName, (string value) => this.FormName = value);
            Copy(DevModeFields.DM_NUP, fields, src.Nup, (DevModeNUp value) => this.Nup = value);
            Copy(DevModeFields.DM_ICMMETHOD, fields, src.ICMMethod, (DevModeICMMethod value) => this.ICMMethod = value);
            Copy(DevModeFields.DM_ICMINTENT, fields, src.ICMIntent, (uint value) => this.ICMIntent = value);
            Copy(DevModeFields.DM_MEDIATYPE, fields, src.MediaType, (uint value) => this.MediaType = value);
            Copy(DevModeFields.DM_DITHERTYPE, fields, src.DitherType, (uint value) => this.DitherType = value);
        }

        /// <summary>
        /// Creates a DEVMODEW with bytes copies from an IntPtr
        /// </summary>
        /// <param name="devModeWPointer">Pointer to Unicode DevMode</param>
        /// <returns>DevMode</returns>
        /// <secritynote>
        /// </secritynote>
        public static DevMode FromIntPtr(IntPtr devModeWPointer)
        {
            if (devModeWPointer == IntPtr.Zero)
            {
                return null;
            }

            short dmSize = Marshal.ReadInt16(devModeWPointer, DevMode.DEVMODEW_dmSizeByteOffset);
            short dmExtraSize = Marshal.ReadInt16(devModeWPointer, DevMode.DEVMODEW_dmDriverExtraByteOffset);
            int devModeWByteSize = dmSize + dmExtraSize;

            byte[] devModeWBytes = new byte[devModeWByteSize];
            Marshal.Copy(devModeWPointer, devModeWBytes, 0, devModeWByteSize);
            return new DevMode(devModeWBytes);
        }

        public bool CompatibleCopy(DevMode ticketDevMode)
        {
            if (DevMode.AreCompatible(this, ticketDevMode))
            {
                Array.Copy(ticketDevMode.ByteData, this.dmFieldsByteOffset, this.ByteData, this.dmFieldsByteOffset, this.ByteData.Length - this.dmFieldsByteOffset);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tests to see if two DEVMODEs were obtained from or apply to the same device
        /// </summary>
        /// <param name="a">DEVMODE to compare</param>
        /// <param name="b">DEVMODE to compare</param>
        /// <returns>True if both DEVMODE were obtained or apply to the same device</returns>
        public static bool AreCompatible(DevMode a, DevMode b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            return (a.DriverVersion == b.DriverVersion)
                && (a.SpecVersion == b.SpecVersion)
                && (a.Size == b.Size)
                && (a.DriverExtra == b.DriverExtra);
        }

        
        #endregion

        #region Private Methods

        private int dmDeviceNameByteSize
        {
            get { return CCHDEVICENAME * BytesPerCharCode(this._isDevModeW); }
        }

        private int dmFormNameByteSize
        {
            get { return CCHFORMNAME * BytesPerCharCode(this._isDevModeW); }
        }

        private void SetField(DevModeFields field)
        {
            this.Fields |= field;
        }

        private ushort ReadWORD(int byteOffset)
        {
            byte[] data = ByteData;
            if (byteOffset >= data.Length)
            {
                return 0;
            }

            return (ushort)(data[byteOffset] | (data[byteOffset + 1] << 8));
        }

        private uint ReadDWORD(int byteOffset)
        {
            byte[] data = ByteData;
            if (byteOffset >= data.Length)
            {
                return 0;
            }

            return (uint)(data[byteOffset] | (data[byteOffset + 1] << 8) | (data[byteOffset + 2] << 16) | (data[byteOffset + 3] << 24));
        }

        private void WriteWORD(int byteOffset, ushort value)
        {
            byte[] data = ByteData;
            if (byteOffset >= data.Length)
            {
                return;
            }

            data[byteOffset] = (byte)value;
            data[byteOffset + 1] = (byte)(value >> 8);
        }

        private void WriteDWORD(int byteOffset, uint value)
        {
            byte[] data = ByteData;
            if (byteOffset >= data.Length)
            {
                return;
            }

            data[byteOffset] = (byte)value;
            data[byteOffset + 1] = (byte)(value >> 8);
            data[byteOffset + 2] = (byte)(value >> 16);
            data[byteOffset + 3] = (byte)(value >> 24);
        }

        private static void Copy<T>(DevModeFields mask, DevModeFields field, T value, Action<T> setter)
        {
            if ((mask & field) == mask)
            {
                setter(value);
            }
        }

        /// <summary>
        /// Read at most charCount characters return the first null terminated string found in it.
        /// </summary>
        /// <param name="byteOffset">Offset to start reading from</param>
        /// <param name="maxCharCount">Max number of characters to read</param>
        /// <returns>First null terminated string read</returns>
        private string ReadChars(int byteOffset, int maxCharCount)
        {
            EnsureInitialized();

            StringBuilder result = new StringBuilder(maxCharCount, maxCharCount);

            if (_isDevModeW)
            {
                for (int charsRead = 0; charsRead < maxCharCount; charsRead++)
                {
                    char ch = ReadUTF16Char(byteOffset, charsRead);
                    if (ch < 1)
                    {
                        break;
                    }

                    result.Append(ch);
                }
            }
            else
            {
                for (int charsRead = 0; charsRead < maxCharCount; charsRead++)
                {
                    char ch = ReadAsciiChar(byteOffset, charsRead);
                    if (ch < 1)
                    {
                        break;
                    }

                    result.Append(ch);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Write at most charCount characters.
        /// </summary>
        /// <param name="byteOffset">Offset to start writing to</param>
        /// <param name="maxCharsToWrite">Number of characters to read</param>
        /// <param name="value">String to write</param>
        private void WriteChars(int byteOffset, int maxCharsToWrite, string value)
        {
            EnsureInitialized();

            // Do not write more than maxCharsToWrite - 1, we are reserving one char for the terminationg '/0'
            int charsToWrite = Math.Min(value.Length, maxCharsToWrite - 1);
            if (_isDevModeW)
            {
                // Write characters as UTF16
                for (int charsWritten = 0; charsWritten < charsToWrite; charsWritten++)
                {
                    WriteUTF16Char(byteOffset, charsWritten, value[charsWritten]);
                }

                // Write terminating '/0' as UTF16
                for (int charsWritten = charsToWrite; charsWritten < maxCharsToWrite; charsWritten++)
                {
                    WriteUTF16Char(byteOffset, charsWritten, '\0');
                }
            }
            else
            {
                // Write characters as ASCII
                for (int charsWritten = 0; charsWritten < charsToWrite; charsWritten++)
                {
                    WriteAsciiChar(byteOffset, charsWritten, value[charsWritten]);
                }

                // Write terminating '/0' as ASCII
                for (int charsWritten = charsToWrite; charsWritten < maxCharsToWrite; charsWritten++)
                {
                    WriteAsciiChar(byteOffset, charsWritten, '\0');
                }
            }
        }

        private void WriteUTF16Char(int ptr, int charOffset, char ch)
        {
            WriteWORD(ptr + (charOffset * 2), ch);
        }

        private char ReadUTF16Char(int ptr, int charOffset)
        {
            return (char)ReadWORD(ptr + (charOffset * 2));
        }

        private void WriteAsciiChar(int ptr, int charOffset, char ch)
        {
            Invariant.Assert(IsAscii(ch));

            _byteData[ptr + charOffset] = (byte)(ch & 0xFF);
        }

        private char ReadAsciiChar(int ptr, int charOffset)
        {
            char ch = (char)_byteData[ptr + charOffset];

            Invariant.Assert(IsAscii(ch));

            return ch;
        }

        // Bytes per character - 1 for DEVMODEA, 2 for DEVMODEW
        private static int BytesPerCharCode(bool isDevModeW)
        {
            return isDevModeW ? 2 : 1;
        }

        // Number of bytes that vary depending on whether the DEVMODE is a DEVMODEA or a DEVMODEW
        private int GetVariableByteSize(bool isDevModeW)
        {
            return (CCHDEVICENAME + CCHFORMNAME) * BytesPerCharCode(isDevModeW);
        }

        /// <summary>
        /// Test whether the DEVMODE bytes has valid dmSize and dmDriverExtra fields is interpreted as a DEVMODEA or DEVMODEW
        /// </summary>
        /// <param name="isUnicode"></param>
        /// <returns></returns>
        private bool HasValidSize(bool isDevModeW)
        {
            int byteDataLength = this.ByteData.Length;
            int dmSize = this.Size;
            int dmDriverExtra = this.DriverExtra;

            if ((dmSize + dmDriverExtra) > byteDataLength)
            {
                // The dmSize and dmDriverExtra fields do not correctly describe how many bytes are in the entire DEVMODE
                return false;
            }

            if (dmSize < (GetVariableByteSize(isDevModeW) + WINVER_0x0000_FixedByteSize))
            {
                // The dmSize field is less than the smallest know public DEVMODE
                return false;
            }

            return true;
        }

        private static bool IsAscii(char ch)
        {
            // Test that the high order bits (bit 31 to 16) are 0. This test includes extended ascii characters (bit 15 is set)
            return (ch & 0xFF00) == 0x0000;
        }

        #endregion

        #region Private Properties

        private int dmDeviceNameByteOffset { get { return 0; } }
        private int dmSpecVersionByteOffset { get { return dmDeviceNameByteOffset + dmDeviceNameByteSize; } }
        private int dmDriverVersionByteOffset { get { return dmSpecVersionByteOffset + 2; } }
        private int dmSizeByteOffset { get { return dmSpecVersionByteOffset + 4; } }
        private int dmDriverExtraByteOffset { get { return dmSpecVersionByteOffset + 6; } }
        private int dmFieldsByteOffset { get { return dmSpecVersionByteOffset + 8; } }
        private int dmOrientationByteOffset { get { return dmSpecVersionByteOffset + 12; } }
        private int dmPaperSizeByteOffset { get { return dmSpecVersionByteOffset + 14; } }
        private int dmPaperLengthByteOffset { get { return dmSpecVersionByteOffset + 16; } }
        private int dmPaperWidthByteOffset { get { return dmSpecVersionByteOffset + 18; } }
        private int dmScaleByteOffset { get { return dmSpecVersionByteOffset + 20; } }
        private int dmCopiesByteOffset { get { return dmSpecVersionByteOffset + 22; } }
        private int dmDefaultSourceByteOffset { get { return dmSpecVersionByteOffset + 24; } }
        private int dmPrintQualityByteOffset { get { return dmSpecVersionByteOffset + 26; } }
        private int dmColorByteOffset { get { return dmSpecVersionByteOffset + 28; } }
        private int dmDuplexByteOffset { get { return dmSpecVersionByteOffset + 30; } }
        private int dmYResolutionByteOffset { get { return dmSpecVersionByteOffset + 32; } }
        private int dmTTOptionByteOffset { get { return dmSpecVersionByteOffset + 34; } }
        private int dmCollateByteOffset { get { return dmSpecVersionByteOffset + 36; } }
        private int dmFormNameByteOffset { get { return dmSpecVersionByteOffset + 38; } }
        private int dmNupByteOffset { get { return dmFormNameByteOffset + dmFormNameByteSize + 14; } }

        // introduced in xp
        private int dmICMMethodByteOffset { get { return dmNupByteOffset + 4; } }
        private int dmICMIntentByteOffset { get { return dmNupByteOffset + 8; } }
        private int dmMediaTypeByteOffset { get { return dmNupByteOffset + 12; } }
        private int dmDitherTypeByteOffset { get { return dmNupByteOffset + 16; } }

        #endregion

        #region Private Fields

        private byte[] _byteData;
        private bool _isDevModeW;

        #endregion

        // Max character count (including null terminator) of dmDeviceName field
        private const int CCHDEVICENAME = 32;

        // Max character count (including null terminator) of dmFormName field
        private const int CCHFORMNAME = 32;

        // Number of fixed size bytes in the Win 95\98\Me public DEVMODE
        private const int WINVER_0x0000_FixedByteSize = 56;

        // Value of DM_SPECVERSION in the Win NT public DEVMODE
        private const ushort WINVER_0x0500_DM_SPECVERSION = 0x0401;

        // Number of fixed size bytes in the Win NT public DEVMODE
        private const int WINVER_0x0500_FixedByteSize = 88;

        // Minimum number of bytes in a DEVMODEA
        public const int MinDEVMODEA_ByteSize = 120;

        // Byte offset to a DEVMODEA dmSize field
        public const int DEVMODEA_dmSizeByteOffset = 36;

        // Byte offset to a DEVMODEA dmDriverExtra field
        public const int DEVMODEA_dmDriverExtraByteOffset = 38;

        // Minimum number of bytes in a DEVMODEW
        public const int MinDEVMODEW_ByteSize = 184;

        // Byte offset to a DEVMODEW dmSize field
        public const int DEVMODEW_dmSizeByteOffset = 68;

        // Byte offset to a DEVMODEW dmDriverExtra field
        public const int DEVMODEW_dmDriverExtraByteOffset = 70;
    }
}
