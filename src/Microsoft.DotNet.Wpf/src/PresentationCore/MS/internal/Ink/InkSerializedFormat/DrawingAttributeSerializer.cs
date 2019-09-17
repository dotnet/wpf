// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define OLD_ISF

using System;
using System.IO;
using System.Security;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using MS.Internal.Ink.InkSerializedFormat;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows;

//
//  These are the V1 DrawingAttributes and their respective defaults:
//
//  DrawingAttributes.Color == System.Drawing.Color.Black
//  DrawingAttributes.Height == 1f
//  DrawingAttributes.PenTip == Microsoft.Ink.PenTip.Ball
//  DrawingAttributes.RasterOperation == Microsoft.Ink.RasterOperation.CopyPen
//  DrawingAttributes.Transparency == 0
//  DrawingAttributes.Width == 53f
//  -and-
//  DrawingAttributes.AntiAliased == true
//  DrawingAttributes.FitToCurve == false
//  DrawingAttributes.IgnorePressure == false
//      which can be reduced to DrawingFlags.AntiAliased
//      since AntiAliased, FitToCurve and IgnorePressure
//      all use DrawingFlags as their default
//
//
//  These are the V2 DrawingAttributes and their respective defaults
//
//  DrawingAttributes.Color == System.Windows.Media.Colors.Black (V1 'Color')
//  DrawingAttributes.Height == 5f (V1 'Height')
//  DrawingAttributes.Width == 5f (V1 'Width')
//  DrawingAttributes.IsHollow == false (no V1 equivalent)
//  DrawingAttributes.StylusTip == StylusTip.Ellipse (V1 'PenTip')
//  DrawingAttributes.IsHighlighter == false (V1 'Transparency' or 'RasterOperation' set)
//  DrawingAttributes.StylusTipTransform == Matrix.Identity (No V1 equivalent)

//  -and-
//  DrawingAttributes.FitToCurve == true (different than V1)
//  DrawingAttributes.IgnorePressure == false
//      which can be reduced to (DrawingFlags)0;
//      since AntiAliased, FitToCurve and IgnorePressure
//      all use DrawingFlags as their default
//
//  These V1 attributes are ignored
//  DrawingAttributes.AntiAliased == true (always true in Avalon)
//
//


namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// ExtendedProperty converter for ISF and serialization purposes
    /// </summary>
    internal static class DrawingAttributeSerializer
    {
        private static readonly double      V1PenWidthWhenWidthIsMissing = 25.0f;
        private static readonly double      V1PenHeightWhenHeightIsMissing = 25.0f;
        private static readonly int         TransparencyDefaultV1 = 0;
        internal static readonly uint        RasterOperationMaskPen = 9;
        internal static readonly uint        RasterOperationDefaultV1 = 13;

        /// <summary>The v1 ISF version of the pen tip shape. For v2, this is represented as StylusShape</summary>
        private enum PenTip
        {
            Circle = 0,
            Rectangle = 1,
            Default = Circle
        }
        private static class PenTipHelper
        {
            internal static bool IsDefined(PenTip penTip)
            {
                if (penTip < PenTip.Circle || penTip > PenTip.Rectangle)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>The v1 ISF version of the pen style. For v2, this is represented as StylusShape</summary>
        private enum PenStyle
        {
            Cosmetic = 0x00000000,     // no shape 
            Geometric = 0x00010000,     // has shape 
            Default = Geometric
        }

        #region Decoding
#if OLD_ISF
        /// <summary>
        /// Loads drawing attributes from a memory buffer.
        /// </summary>
        /// <param name="stream">Memory buffer to read from</param>
        /// <param name="guidList">Guid tags if extended properties are used</param>
        /// <param name="maximumStreamSize">Maximum size of buffer to read through</param>
        /// <param name="da">The drawing attributes collection to decode into</param>
        /// <returns>Number of bytes read</returns>
#else
        /// <summary>
        /// Loads drawing attributes from a memory buffer.
        /// </summary>
        /// <param name="stream">Memory buffer to read from</param>
        /// <param name="guidList">Guid tags if extended properties are used</param>
        /// <param name="maximumStreamSize">Maximum size of buffer to read through</param>
        /// <param name="da">The drawing attributes collection to decode into</param>
        /// <returns>Number of bytes read</returns>
#endif
        internal static uint DecodeAsISF(Stream stream, GuidList guidList, uint maximumStreamSize, DrawingAttributes da)
        {
            PenTip penTip = PenTip.Default;
            PenStyle penStyle = PenStyle.Default;
            double stylusWidth = DrawingAttributeSerializer.V1PenWidthWhenWidthIsMissing;
            double stylusHeight = DrawingAttributeSerializer.V1PenHeightWhenHeightIsMissing;
            uint rasterOperation = DrawingAttributeSerializer.RasterOperationDefaultV1;
            int transparency = DrawingAttributeSerializer.TransparencyDefaultV1;
            bool widthIsSetInISF = false; //did we find KnownIds.Width?
            bool heightIsSetInISF = false; //did we find KnownIds.Height?


            uint cbTotal = maximumStreamSize;
            while (maximumStreamSize > 0)
            {
                KnownTagCache.KnownTagIndex tag;
                uint uiTag;
                // First read the tag
                uint cb = SerializationHelper.Decode (stream, out uiTag);
                tag = (KnownTagCache.KnownTagIndex)uiTag;

                if (maximumStreamSize < cb)
                {
                    throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("ISF size is larger than maximum stream size"));
                }

                maximumStreamSize -= cb;

                // Get the guid based on the tag
                Guid guid = guidList.FindGuid (tag);
                if (guid == Guid.Empty)
                {
                    throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Drawing Attribute tag embedded in ISF stream does not match guid table"));
                }

                uint dw = 0;

                if (KnownIds.PenTip == guid)
                {
                    cb = SerializationHelper.Decode (stream, out dw);
                    penTip = (PenTip)dw;
                    if (!PenTipHelper.IsDefined(penTip))
                    {
                        throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Invalid PenTip value found in ISF stream"));
                    }
                    maximumStreamSize -= cb;
                }
                else if (KnownIds.PenStyle == guid)
                {
                    cb = SerializationHelper.Decode(stream, out dw);
                    penStyle = (PenStyle)dw;
                    maximumStreamSize -= cb;
                }
                else if (KnownIds.DrawingFlags == guid)
                {
                    // Encode the drawing flags with considerations for v2 model
                    cb = SerializationHelper.Decode (stream, out dw);
                    DrawingFlags flags = (DrawingFlags)dw;
                    da.DrawingFlags = flags;
                    maximumStreamSize -= cb;
                }
                else if (KnownIds.RasterOperation == guid)
                {
                    uint ropSize = GuidList.GetDataSizeIfKnownGuid(KnownIds.RasterOperation);
                    if (ropSize == 0)
                    {
                        throw new InvalidOperationException(StrokeCollectionSerializer. ISFDebugMessage("ROP data size was not found"));
                    }

                    byte[] data = new byte[ropSize];
                    stream.Read (data, 0, (int)ropSize);

                    if (data != null && data.Length > 0)
                    {
                        //data[0] holds the allowable values of 0-255
                        rasterOperation = Convert.ToUInt32(data[0]);
                    }

                    maximumStreamSize -= ropSize;
                }
                else if (KnownIds.CurveFittingError == guid)
                {
                    cb = SerializationHelper.Decode (stream, out dw);
                    da.FittingError = (int)dw;
                    maximumStreamSize -= cb;
                }
                else if (KnownIds.StylusHeight == guid || KnownIds.StylusWidth == guid)
                {
                    double _size;
                    cb = SerializationHelper.Decode (stream, out dw);
                    _size = (double)dw;
                    maximumStreamSize -= cb;
                    if (maximumStreamSize > 0)
                    {
                        cb = SerializationHelper.Decode (stream, out dw);
                        maximumStreamSize -= cb;
                        if (KnownTagCache.KnownTagIndex.Mantissa == (KnownTagCache.KnownTagIndex)dw)
                        {
                            uint cbInSize;
                            // First thing that is in there is maximumStreamSize of the data
                            cb = SerializationHelper.Decode (stream, out cbInSize);
                            maximumStreamSize -= cb;

                            // in maximumStreamSize is one more than the decoded no
                            cbInSize++;
                            if (cbInSize > maximumStreamSize)
                            {
                                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("ISF size if greater then maximum stream size"));
                            }
                            byte[] in_data = new byte[cbInSize];
							
                            uint bytesRead = (uint) stream.Read (in_data, 0, (int)cbInSize);
                            if (cbInSize != bytesRead)
                            {
                                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Read different size from stream then expected"));
                            }

                            byte[] out_buffer = Compressor.DecompressPropertyData (in_data);
                            using (MemoryStream localStream = new MemoryStream(out_buffer))
                            using (BinaryReader rdr = new BinaryReader(localStream))
                            {
                                short sFraction = rdr.ReadInt16();
                                _size += (double)(sFraction / DrawingAttributes.StylusPrecision);

                                maximumStreamSize -= cbInSize;
			                }
                        }
                        else
                        {
                            // Seek it back by cb
                            stream.Seek (-cb, SeekOrigin.Current);
                            maximumStreamSize += cb;
                        }
                    }
                    if (KnownIds.StylusWidth == guid)
                    {
                        widthIsSetInISF = true;
                        stylusWidth = _size;
                    }
                    else
                    {
                        heightIsSetInISF = true;
                        stylusHeight = _size;
                    }
                }
                else if (KnownIds.Transparency == guid)
                {
                    cb = SerializationHelper.Decode(stream, out dw);
                    transparency = (int)dw;
                    maximumStreamSize -= cb;
                }
                else if (KnownIds.Color == guid)
                {
                    cb = SerializationHelper.Decode(stream, out dw);

                    Color color = Color.FromRgb((byte)(dw & 0xff), (byte)((dw & 0xff00) >> Native.BitsPerByte), (byte)((dw & 0xff0000) >> (Native.BitsPerByte * 2)));
                    da.Color = color;
                    maximumStreamSize -= cb;
                }
                else if (KnownIds.StylusTipTransform == guid)
                {
                    try
                    {
                        object data;
                        cb = ExtendedPropertySerializer.DecodeAsISF(stream, maximumStreamSize, guidList, tag, ref guid, out data);

                        Matrix matrix = Matrix.Parse((string)data);
                        da.StylusTipTransform = matrix;
                    }
                    catch (InvalidOperationException) // Matrix.Parse failed.
                    {
                        System.Diagnostics.Debug.Assert(false, "Corrupt Matrix in the ExtendedPropertyCollection!");
                    }
                    finally
                    {
                        maximumStreamSize -= cb;
                    }
                }
                else
                {
                    object data;
                    cb = ExtendedPropertySerializer.DecodeAsISF(stream, maximumStreamSize, guidList, tag, ref guid, out data);
                    maximumStreamSize -= cb;
                    da.AddPropertyData(guid,data);
                }
            }

            if (0 != maximumStreamSize)
            {
                throw new ArgumentException ();
            }

            //
            // time to create our drawing attributes.
            //
            // 1) First we need to evaluate PenTip / StylusTip
            // Here is the V1 - V2 mapping
            //
            // PenTip.Circle == StylusTip.Ellipse
            // PenTip.Rectangle == StylusTip.Rectangle
            // PenTip.Rectangle == StylusTip.Diamond
            if (penTip == PenTip.Default)
            {
                //Since StylusTip is stored in the EPC at this point (if set), we can compare against it here.
                if (da.StylusTip != StylusTip.Ellipse)
                {
                    //
                    // StylusTip was set to something other than Ellipse
                    // when we last serialized (or else StylusTip would be Ellipse, the default)
                    // when StylusTip is != Ellipse and we serialize, we set PenTip to Rectangle
                    // which is not the default.  Therefore, if PenTip is back to Circle,
                    // that means someone set it in V1 and we should respect that by 
                    // changing StylusTip back to Ellipse
                    //
                    da.StylusTip = StylusTip.Ellipse;
                }
                //else da.StylusTip is already set
            }
            else
            {
                System.Diagnostics.Debug.Assert(penTip == PenTip.Rectangle);
                if (da.StylusTip == StylusTip.Ellipse)
                {
                    //
                    // PenTip is Rectangle and StylusTip was either not set
                    // before or was set to Ellipse and PenTip was changed
                    // in a V1 ink object.  Either way, we need to change StylusTip to Rectangle
                    da.StylusTip = StylusTip.Rectangle;
                }
                //else da.StylusTip is already set
            }

            //
            // 2) next we need to set hight and width
            //
            if (da.StylusTip == StylusTip.Ellipse &&
                widthIsSetInISF && 
                !heightIsSetInISF)
            {
                //
                // special case: V1 PenTip of Circle only used Width to compute the circle size
                // and so it only serializes Width of 53
                // but since our default is Ellipse, if Height is unset and we use the default
                // height of 30, then your ink that looked like 53,53 in V1 will look 
                // like 30,53 here.
                //
                //
                stylusHeight = stylusWidth;
                da.HeightChangedForCompatabity = true;
            }
            // need to convert width/height into Avalon, since they are stored in HIMETRIC in ISF
            stylusHeight *= StrokeCollectionSerializer.HimetricToAvalonMultiplier;
            stylusWidth *= StrokeCollectionSerializer.HimetricToAvalonMultiplier;

            // Map 0.0 width to DrawingAttributes.DefaultXXXXXX (V1 53 equivalent)
            double height = DoubleUtil.IsZero(stylusHeight) ? (Double)DrawingAttributes.GetDefaultDrawingAttributeValue(KnownIds.StylusHeight) : stylusHeight;
            double width = DoubleUtil.IsZero(stylusWidth) ? (Double)DrawingAttributes.GetDefaultDrawingAttributeValue(KnownIds.StylusWidth) : stylusWidth;

            da.Height = GetCappedHeightOrWidth(height);
            da.Width = GetCappedHeightOrWidth(width);
            
			//
            // 3) next we need to set IsHighlighter (by looking for RasterOperation.MaskPen)
            //

            //
            // always store raster op
            //
            da.RasterOperation = rasterOperation;
            if (rasterOperation == DrawingAttributeSerializer.RasterOperationDefaultV1)
            {
                //
                // if rasterop is default, make sure IsHighlighter isn't in the EPC
                //
                if (da.ContainsPropertyData(KnownIds.IsHighlighter))
                {
                    da.RemovePropertyData(KnownIds.IsHighlighter);
                }
            }
            else
            {
                if (rasterOperation == DrawingAttributeSerializer.RasterOperationMaskPen)
                {
                    da.IsHighlighter = true;
                }
            }
            //else, IsHighlighter will be set to false by default, no need to set it
            
            //
            // 4) see if there is a transparency we need to add to color
            //
            if (transparency > DrawingAttributeSerializer.TransparencyDefaultV1)
            {
                //note: Color.A is set to 255 by default, which means fully opaque
                //transparency is just the opposite - 0 means fully opaque so 
                //we need to flip the values
                int alpha = MathHelper.AbsNoThrow(transparency - 255);
                Color color = da.Color;
                color.A = Convert.ToByte(alpha);
                da.Color = color;
            }
            return cbTotal;
        }

        /// <summary>
        /// Internal helper to limit what we set for width or height on deserialization
        /// </summary>
        internal static double GetCappedHeightOrWidth(double heightOrWidth)
        {
            Debug.Assert(DrawingAttributes.MaxHeight == DrawingAttributes.MaxWidth &&
                         DrawingAttributes.MinHeight == DrawingAttributes.MinWidth);

            if (heightOrWidth > DrawingAttributes.MaxHeight)
            {
                return DrawingAttributes.MaxHeight;
            }
            if (heightOrWidth < DrawingAttributes.MinHeight)
            {
                return DrawingAttributes.MinHeight;
            }
            return heightOrWidth;
        }
        #endregion // Decoding

        #region Encoding

#if OLD_ISF
        /// <Summary>
        /// Encodes a DrawingAttriubtesin the ISF stream.
        /// </Summary>
#else
        /// <Summary>
        /// Encodes a DrawingAttriubtesin the ISF stream.
        /// </Summary>
#endif
        internal static uint EncodeAsISF(DrawingAttributes da, Stream stream, GuidList guidList, byte compressionAlgorithm, bool fTag)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(compressionAlgorithm == 0);
            System.Diagnostics.Debug.Assert(fTag == true);
#endif
            Debug.Assert(stream != null);
            uint cbData = 0;
            BinaryWriter bw = new BinaryWriter(stream);

            PersistDrawingFlags(da, stream, guidList, ref cbData, ref bw);

            PersistColorAndTransparency(da, stream, guidList, ref cbData, ref bw);

            PersistRasterOperation(da, stream, guidList, ref cbData, ref bw);

            PersistWidthHeight(da, stream, guidList, ref cbData, ref bw);

            PersistStylusTip(da, stream, guidList, ref cbData, ref bw);

            PersistExtendedProperties(da, stream, guidList, ref cbData, ref bw, compressionAlgorithm, fTag);

            return cbData;
        }


        private static void PersistDrawingFlags(DrawingAttributes da, Stream stream, GuidList guidList, ref uint cbData, ref BinaryWriter bw)
        {
            //
            // always serialize DrawingFlags, even when it is the default of AntiAliased.  V1 loaders 
            // expect it.
            //
            Debug.Assert(bw != null);
            cbData += SerializationHelper.Encode(stream, (uint)guidList.FindTag(KnownIds.DrawingFlags, true));
            cbData += SerializationHelper.Encode(stream, (uint)(int)da.DrawingFlags);

            if (da.ContainsPropertyData(KnownIds.CurveFittingError))
            {
                Debug.Assert(bw != null);
                cbData += SerializationHelper.Encode(stream, (uint)guidList.FindTag(KnownIds.CurveFittingError, true));
                cbData += SerializationHelper.Encode(stream, (uint)(int)da.GetPropertyData(KnownIds.CurveFittingError));
            }
        }

        private static void PersistColorAndTransparency(DrawingAttributes da, Stream stream, GuidList guidList, ref uint cbData, ref BinaryWriter bw)
        {
            // if the color is non-default (e.g. not black), then store it
            // the v1 encoder throws away the default color (Black) so it isn't valuable
            // to save.
            if (da.ContainsPropertyData(KnownIds.Color))
            {
                Color daColor = da.Color;
                System.Diagnostics.Debug.Assert(da.Color != (Color)DrawingAttributes.GetDefaultDrawingAttributeValue(KnownIds.Color), "Color was put in the EPC for the default value!");

                //Note: we don't store the alpha value of the color (we don't use it)
                uint r = (uint)daColor.R, g = (uint)daColor.G, b = (uint)(daColor.B);
                uint colorVal = r + (g << Native.BitsPerByte) + (b << (Native.BitsPerByte * 2));

                Debug.Assert(bw != null);
                cbData += SerializationHelper.Encode(stream, (uint)guidList.FindTag(KnownIds.Color, true));
                cbData += SerializationHelper.Encode(stream, colorVal);
            }

            //set transparency if Color.A is set
            byte alphaChannel = da.Color.A;
            if (alphaChannel != 255)
            {
                //note: Color.A is set to 255 by default, which means fully opaque
                //transparency is just the opposite - 0 means fully opaque so 
                //we need to flip the values
                int transparency = MathHelper.AbsNoThrow(( (int)alphaChannel ) - 255);
                Debug.Assert(bw != null);
                cbData += SerializationHelper.Encode(stream, (uint)guidList.FindTag(KnownIds.Transparency, true));
                cbData += SerializationHelper.Encode(stream, Convert.ToUInt32(transparency));
            }
        }

        private static void PersistRasterOperation(DrawingAttributes da, Stream stream, GuidList guidList, ref uint cbData, ref BinaryWriter bw)
        {
            // write any non-default RasterOp value that we might have picked up from 
            // V1 interop or by setting IsHighlighter.
            if (da.RasterOperation != DrawingAttributeSerializer.RasterOperationDefaultV1)
            {
                uint ropSize = GuidList.GetDataSizeIfKnownGuid(KnownIds.RasterOperation);
                if (ropSize == 0)
                {
                    throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage("ROP data size was not found"));
                }

                Debug.Assert(bw != null);
                cbData += SerializationHelper.Encode(stream, (uint)guidList.FindTag(KnownIds.RasterOperation, true));
                long currentPosition = stream.Position;
                bw.Write(da.RasterOperation);
                if ((uint)(stream.Position - currentPosition) != ropSize)
                {
                    throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage("ROP data was incorrectly serialized"));
                }
                cbData += ropSize;
            }
        }
#if OLD_ISF
        /// <Summary>
        /// Encodes the ExtendedProperties in the ISF stream.
        /// </Summary>
#else
        /// <Summary>
        /// Encodes the ExtendedProperties in the ISF stream.
        /// </Summary>
#endif
        private static void PersistExtendedProperties(DrawingAttributes da, Stream stream, GuidList guidList, ref uint cbData, ref BinaryWriter bw, byte compressionAlgorithm, bool fTag)
        {
            // Now save the extended properties
            ExtendedPropertyCollection epcClone = da.CopyPropertyData();

            //walk from the back removing EPs that are uses for DrawingAttributes
            for (int x = epcClone.Count - 1; x >= 0; x--)
            {
                //
                // look for StylusTipTransform while we're at it and turn it into a string
                // for serialization
                //
                if (epcClone[x].Id == KnownIds.StylusTipTransform)
                {
                    Matrix matrix = (Matrix)epcClone[x].Value;
                    string matrixString = matrix.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    epcClone[x].Value = matrixString;
                    continue;
                }

                if (DrawingAttributes.RemoveIdFromExtendedProperties(epcClone[x].Id))
                {
                    epcClone.Remove(epcClone[x].Id);
                }
            }

            cbData += ExtendedPropertySerializer.EncodeAsISF(epcClone, stream, guidList, compressionAlgorithm, fTag);
        }
#if OLD_ISF
        /// <Summary>
        /// Encodes the StylusTip in the ISF stream.
        /// </Summary>
#else
        /// <Summary>
        /// Encodes the StylusTip in the ISF stream.
        /// </Summary>
#endif
        private static void PersistStylusTip(DrawingAttributes da, Stream stream, GuidList guidList, ref uint cbData, ref BinaryWriter bw)
        {
            //
            // persist the StylusTip
            //
            if (da.ContainsPropertyData(KnownIds.StylusTip))
            {
                System.Diagnostics.Debug.Assert(da.StylusTip != StylusTip.Ellipse, "StylusTip was put in the EPC for the default value!");

                //
                // persist PenTip.Rectangle for V1 ISF
                //
                Debug.Assert(bw != null);
                cbData += SerializationHelper.Encode(stream, (uint)guidList.FindTag(KnownIds.PenTip, true));
                cbData += SerializationHelper.Encode(stream, (uint)PenTip.Rectangle);

                using (MemoryStream localStream = new MemoryStream(6)) //reasonable default
                {
                    Int32 stylusTip = Convert.ToInt32(da.StylusTip, System.Globalization.CultureInfo.InvariantCulture);
                    System.Runtime.InteropServices.VarEnum type = SerializationHelper.ConvertToVarEnum(PersistenceTypes.StylusTip, true);
                    ExtendedPropertySerializer.EncodeAttribute(KnownIds.StylusTip, stylusTip, type, localStream);

                    cbData += ExtendedPropertySerializer.EncodeAsISF(KnownIds.StylusTip, localStream.ToArray(), stream, guidList, 0, true);
                }
            }
        }

        private static void PersistWidthHeight(DrawingAttributes da, Stream stream, GuidList guidList, ref uint cbData, ref BinaryWriter bw)
        {
            //persist the height and width
            // For v1 loaders we persist height and width in StylusHeight and StylusWidth
            double stylusWidth = da.Width;
            double stylusHeight = da.Height;

            // Save the pen tip's width and height.
            for (int i = 0; i < 2; i++)
            {
                Guid guid = (i == 0) ? KnownIds.StylusWidth : KnownIds.StylusHeight;
                double size = (0 == i) ? stylusWidth : stylusHeight;

                //
                // the size is now in Avalon units, we need to convert to HIMETRIC
                //
                size *= StrokeCollectionSerializer.AvalonToHimetricMultiplier;

                double sizeWhenMissing = (0 == i) ? V1PenWidthWhenWidthIsMissing : V1PenHeightWhenHeightIsMissing;

                //
                // only persist height / width if they are equal to the height / width 
                // when missing in the isf stream OR for compatibility with V1
                //
                bool skipPersisting = DoubleUtil.AreClose(size, sizeWhenMissing);
                if ( stylusWidth == stylusHeight && 
                     da.StylusTip == StylusTip.Ellipse && 
                     guid == KnownIds.StylusHeight && 
                     da.HeightChangedForCompatabity)
                {
                    //we need to put height in the ISF stream for compat
                    skipPersisting = true;
                }

                    
                if (!skipPersisting)
                {
                    uint uIntegral = (uint)(size + 0.5f);

                    Debug.Assert(bw != null);
                    cbData += SerializationHelper.Encode(stream, (uint)guidList.FindTag(guid, true));
                    cbData += SerializationHelper.Encode(stream, uIntegral);

                    short sFraction = (size > uIntegral) ? (short)(DrawingAttributes.StylusPrecision * (size - uIntegral) + 0.5f) : (short)(DrawingAttributes.StylusPrecision * (size - uIntegral) - 0.5);

                    // If the fractional values is non zero, we store this value along with TAG_MANTISSA and size with a precisson of 1000
                    if (0 != sFraction)
                    {
                        uint cb = Native.SizeOfUShort; // For header NO_COMPRESS

                        Debug.Assert(bw != null);
                        cbData += SerializationHelper.Encode(stream, (uint)MS.Internal.Ink.InkSerializedFormat.KnownTagCache.KnownTagIndex.Mantissa);
                        cbData += SerializationHelper.Encode(stream, cb);
                        bw.Write((byte)0x00);
                        bw.Write((short)sFraction);

                        cbData += cb + 1; // include size of encoded 0 and encoded fraction value
                    }
                }
            }
        }


        #endregion // Encoding

        internal static class PersistenceTypes
        {
            public static readonly Type StylusTip = typeof(Int32);
            public static readonly Type IsHollow = typeof(bool);
            public static readonly Type StylusTipTransform = typeof(string);
        }
    }
}
