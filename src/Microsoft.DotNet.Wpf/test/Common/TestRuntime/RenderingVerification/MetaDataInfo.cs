// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region Using directives
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using System.Security.Permissions;
    #endregion    
    
    /// <summary>
    /// MetadataInfo class
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class MetadataInfo : IMetadataInfo, ICloneable
    {
        #region Constants and readOnly (in)variables 
            private const string SOFTWAREUSED = "ImageUtility Library";
            private readonly string HOSTCOMPUTER = "OS : " + System.Environment.OSVersion.ToString() + " / CLR : " + System.Environment.Version.ToString() + " / CPU(s) : " + System.Environment.ProcessorCount.ToString() +" / ( MachineName : " + System.Environment.MachineName + " )";
            private readonly string ARTISTNAME = System.Environment.UserDomainName + "\\" + System.Environment.UserName;
            private readonly string COPYRIGHT = "Copyright might apply (screen snapshot)";
        #endregion Constants and readOnly (in)variables

        #region Properties
            private Hashtable _map = new Hashtable();
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a new instance of this object
            /// </summary>
            public MetadataInfo()
            {
                AddCustomProperties();
            }
            /// <summary>
            /// Create a new instance of this object and populate with the collection passed in
            /// </summary>
            /// <param name="propertyItems"></param>
            public MetadataInfo(PropertyItem[] propertyItems)
            {
                this.PropertyItems = propertyItems;
                AddCustomProperties();
            }
            private MetadataInfo(MetadataInfo metadataToClone)
            {
                this.PropertyItems = metadataToClone.PropertyItems;
                AddCustomProperties();
            }
        #endregion Constructors

        #region Methods
            private void AddCustomProperties()
            {
                if(_map.Contains(PropertyTag.PixelUnit) == false)
                {
                    System.Drawing.Size screenDPI = User32.GetScreenResolution();
                    _map.Add(PropertyTag.PixelUnit, PropertyItemFactory.CreateInstance(PropertyTag.PixelUnit, (byte)0x2));   // 1 : pixel per cm -- 2 : pixel per inch
                    if ( _map.Contains(PropertyTag.XResolution) && _map.Contains(PropertyTag.YResolution))
                    {
                        int dpiX = ((PropertyItem)_map[PropertyTag.XResolution]).Value[0];
                        int dpiY = ((PropertyItem)_map[PropertyTag.YResolution]).Value[0];
                        _map.Add(PropertyTag.PixelPerUnitX, PropertyItemFactory.CreateInstance(PropertyTag.PixelPerUnitX, dpiX));
                        _map.Add(PropertyTag.PixelPerUnitY, PropertyItemFactory.CreateInstance(PropertyTag.PixelPerUnitY, dpiY));
                    }
                    else
                    {
                        _map.Add(PropertyTag.PixelPerUnitX, PropertyItemFactory.CreateInstance(PropertyTag.PixelPerUnitX, screenDPI.Width));
                        _map.Add(PropertyTag.PixelPerUnitY, PropertyItemFactory.CreateInstance(PropertyTag.PixelPerUnitY, screenDPI.Height));
                    }
                }
                if (_map.Contains(PropertyTag.SoftwareUsed) == false)
                {
                    _map.Add(PropertyTag.SoftwareUsed, PropertyItemFactory.CreateInstance(PropertyTag.SoftwareUsed, SOFTWAREUSED));
                }
                if (_map.Contains(PropertyTag.HostComputer) == false)
                {
                    _map.Add(PropertyTag.HostComputer, PropertyItemFactory.CreateInstance(PropertyTag.HostComputer, HOSTCOMPUTER));
                }
                if (_map.Contains(PropertyTag.Artist) == false)
                {
                    _map.Add(PropertyTag.Artist, PropertyItemFactory.CreateInstance(PropertyTag.Artist, ARTISTNAME));
                }
                if (_map.Contains(PropertyTag.Copyright) == false)
                {
                    _map.Add(PropertyTag.Copyright, PropertyItemFactory.CreateInstance(PropertyTag.Copyright, COPYRIGHT));
                }

                // HACK : Store original color depth info into the ImageDescription tag
                // Reason : 
                //    * GDI+ cannot store BitsPerSample and SamplesPerPixel for PNG images)
                //    * Cannot store custom PropertyImtem in PNG (as least not using GDI+)
                if (_map.Contains(PropertyTag.ImageDescription) == false)
                {
                    IntPtr screenDC = IntPtr.Zero;
                    try
                    {
                        screenDC = User32.GetDC(IntPtr.Zero);
                        if (screenDC == IntPtr.Zero) { throw new System.Runtime.InteropServices.ExternalException("Native call to 'GetDC' API failed"); }

                        int originalBpp = GDI32.GetDeviceCaps(screenDC, (int)GDI32.GetDeviceCapsIndex.BITSPIXEL);
                        if (originalBpp == 0) { throw new System.Runtime.InteropServices.ExternalException("Native call to 'GetDeviceCaps(BitsPixel)' API failed"); }

                        PixelFormat pixelFormat = PixelFormat.Undefined;
                        switch (originalBpp)
                        {
//                            case 64: pixelFormat = PixelFormat.Format64bppArgb; break;
//                            case 48: pixelFormat = PixelFormat.Format48bppRgb; break;
                            case 32: pixelFormat = PixelFormat.Format32bppArgb; break;
                            case 24: pixelFormat = PixelFormat.Format24bppRgb; break;
                            case 16: pixelFormat = PixelFormat.Format16bppRgb565; break;
                            case 15: pixelFormat = PixelFormat.Format16bppRgb555; break;
//                            case 8: pixelFormat = PixelFormat.Format8bppIndexed; break;
//                            case 4: pixelFormat = PixelFormat.Format4bppIndexed; break;
//                            case 1: pixelFormat = PixelFormat.Format1bppIndexed; break;
                            default: throw new NotSupportedException("This Color Depth ( " + originalBpp.ToString() + " bits per pixel) is not supported");
                        }
                        string description = "OriginalPixelFormat=" + ((int)pixelFormat).ToString();
                        _map.Add(PropertyTag.ImageDescription, PropertyItemFactory.CreateInstance(PropertyTag.ImageDescription, description));
                    }
                    finally
                    {
                        if (screenDC != IntPtr.Zero) { User32.ReleaseDC(IntPtr.Zero, screenDC); screenDC = IntPtr.Zero; }
                    }
                }

            }
        #endregion Methods

        #region IMetadataInfo Members
            /// <summary>
            /// Get/set the collection associated with this object
            /// </summary>
            /// <value></value>
            public PropertyItem[] PropertyItems
            {
                get
                {
                    PropertyItem[] retVal = new PropertyItem[_map.Values.Count];
                    _map.Values.CopyTo(retVal, 0);
                    return retVal;
                }
                set
                {
                    if (value == null) { throw new ArgumentNullException("PropertyItems value cannot be null"); }
                    _map.Clear();
                    foreach (PropertyItem pi in value)
                    {
                        if(_map.Contains(pi.Id)) { _map[pi.Id] = pi; }
                        else { _map.Add(pi.Id, pi); }
                    }
                }
            }
        #endregion

        #region ICloneable Members
            /// <summary>
            /// returns a deep copy of this object
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new MetadataInfo(this);
            }
        #endregion
    }


    /// <summary>
    /// User friendly helper class Retrieve property out of an object
    /// </summary>
    public /*internal*/ class MetadataInfoHelper
    {
        private MetadataInfoHelper() { }    // block instantiation
        /// <summary>
        /// Get the Image DPI settings
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        static public Point GetDpi(Image image)
        {
            if (image == null) { throw new ArgumentNullException("image", "Cannot pass a null value"); }
            return GetDpi(new MetadataInfo(image.PropertyItems));
        }
        /// <summary>
        /// Get the IImageAdapter DPI settings
        /// </summary>
        /// <param name="imageAdapter"></param>
        /// <returns></returns>
        static public Point GetDpi(IImageAdapter imageAdapter) 
        {
            if (imageAdapter == null) { throw new ArgumentNullException("imageAdapter", "Cannot pass a null value"); }
            return GetDpi(imageAdapter.Metadata);
        }
        /// <summary>
        /// Get the x and y DPI setting associate with this object
        /// </summary>
        /// <param name="metadataInfo"></param>
        /// <returns></returns>
        static internal Point GetDpi(IMetadataInfo metadataInfo)
        {
            Hashtable map = new Hashtable();
            foreach (PropertyItem item in metadataInfo.PropertyItems)
            {
                map.Add(item.Id, item);
            }
            // ArrayList list = new ArrayList(metadataInfo.PropertyItems);
            if (map.Contains((int)PropertyTag.PixelUnit) == false)
            {
                    // TODO / BUGBUG / HACK
                    // @REVIEW : What should we do here ?
                    //  * return screen DPI as shell does ?
                    //  * throw exception ?
                    //  * DPI = 0 ? -1 ?
                    throw new NotSupportedException("This image does not have any embedded DPI information");
            }
            if (map.Contains((int)PropertyTag.PixelPerUnitX) == false || map.Contains((int)PropertyTag.PixelPerUnitY) == false)
            {
                throw new InvalidOperationException("PixelPerUnitX and PixelPerUnitY must always be set when PixelUnit is set");
            }
            byte[] buffer = ((PropertyItem)map[(int)PropertyTag.PixelPerUnitX]).Value;
            float xDpi = buffer[0] + (buffer[1] << 8) + (buffer[2] << 16) + (buffer[3] << 24);
            buffer = ((PropertyItem)map[(int)PropertyTag.PixelPerUnitY]).Value;
            float yDpi = buffer[0] + (buffer[1] << 8) + (buffer[2] << 16) + (buffer[3] << 24);
            if (((PropertyItem)map[(int)PropertyTag.PixelUnit]).Value[0] == (byte)0x1) // 1 : dots/meter -- 2 : dots/inch
            {
                xDpi *= 2.54f / 100;    // 1 inch = 2.54 cm ( in dots/meter so need to divide by 100 to get dot/cm)
                yDpi *= 2.54f / 100;
            }
            xDpi = (float)Math.Round(xDpi);
            yDpi = (float)Math.Round(yDpi);

            return new Point((int)xDpi, (int)yDpi);

        }

        static internal void SetDpi(IMetadataInfo metadataInfo, Point dpi)
        {
            foreach (PropertyItem pItem in metadataInfo.PropertyItems)
            {
                if (pItem.Id == PropertyTag.PixelPerUnitX)
                {
                    pItem.Value[0] = (byte)dpi.X;
                    pItem.Value[1] = pItem.Value[2] = pItem.Value[3] = 0;
                }
                else if (pItem.Id == PropertyTag.PixelPerUnitY)
                {
                    pItem.Value[0] = (byte)dpi.Y;
                    pItem.Value[1] = pItem.Value[2] = pItem.Value[3] = 0;
                }
                else if (pItem.Id == PropertyTag.PixelUnit)
                {
                    //setting unit to dots per inch
                    pItem.Value[0] = (byte)2; //1: dots per cm; 2: dots per inch
                }
            }
        }


        /// <summary>
        /// Get the machineInfo associated with this image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        static public string GetMachineInfo(Image image)
        {
            if (image == null) { throw new ArgumentNullException("image", "Cannot pass a null argument"); }
            return GetMachineInfo(new MetadataInfo(image.PropertyItems));
        }
        /// <summary>
        /// Get the machineInfo associated with this imageAdapter
        /// </summary>
        /// <param name="imageAdapter"></param>
        /// <returns></returns>
        static public string GetMachineInfo(ImageAdapter imageAdapter)
        {
            if (imageAdapter == null) { throw new ArgumentNullException("imageAdapter", "Cannot pass a null argument"); }
            if (imageAdapter.Metadata == null) { return string.Empty; }
            return GetMachineInfo(imageAdapter.Metadata);
        }
        /// <summary>
        /// Get the machineInfo associated with this object
        /// </summary>
        /// <param name="metadataInfo"></param>
        /// <returns></returns>
        static private string GetMachineInfo(IMetadataInfo metadataInfo)
        {
            // BUGBUG : Check if image format support metadata.
            // BUGBUG : Wrong logic ! Won't work... TO BE FIXED ASAP !
            throw new NotImplementedException("GetMachineInfo API Not implemented yet");
/*
            PropertyItem propertyItem = metadataInfo.PropertyItems[(int)PropertyTag.HostComputer];
            if (propertyItem == null) { return string.Empty; }
            return System.Text.ASCIIEncoding.ASCII.GetString(propertyItem.Value);
*/
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metadataInfo"></param>
        /// <returns></returns>
        static public PixelFormat GetPixelFormat(IMetadataInfo metadataInfo)
        {

            PixelFormat retVal = PixelFormat.Undefined;
            PropertyItem description = null;
            foreach (PropertyItem propertyItem in metadataInfo.PropertyItems)
            {
                if (propertyItem.Id == PropertyTag.ImageDescription) { description = propertyItem; break; }
            }

            // if no/corrupted description => default to 32bits
            if (description == null) { retVal = PixelFormat.Format32bppArgb; }
            else
            {
                string originalDepth = System.Text.ASCIIEncoding.ASCII.GetString(description.Value);
                if (System.Text.RegularExpressions.Regex.IsMatch(originalDepth, @"^\D*\d+\D*?") == false) { retVal = PixelFormat.Format32bppArgb; }
                else
                {
                    originalDepth = System.Text.RegularExpressions.Regex.Replace(originalDepth, @"^\D*(\d+)\D*?", "$1");
                    retVal = (PixelFormat)System.Convert.ToInt32(originalDepth);
                }
            }
            return retVal;

        }
    }
}
