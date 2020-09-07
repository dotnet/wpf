// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define OLD_ISF

using MS.Utility;
using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Ink;
using MS.Internal.IO.Packaging;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    internal class StrokeCollectionSerializer
    {
        #region Constants (Static Fields)
        internal static readonly double AvalonToHimetricMultiplier = 2540.0d / 96.0d;
        internal static readonly double HimetricToAvalonMultiplier = 96.0d / 2540.0d;
        internal static readonly TransformDescriptor IdentityTransformDescriptor;

        static StrokeCollectionSerializer()
        {
            TransformDescriptor transformDescriptor = new TransformDescriptor();
            transformDescriptor.Transform[0] = 1.0f;
            transformDescriptor.Tag = KnownTagCache.KnownTagIndex.TransformIsotropicScale;
            transformDescriptor.Size = 1;
            StrokeCollectionSerializer.IdentityTransformDescriptor = transformDescriptor;
        }
        #endregion

        #region Constructors

        // disable default constructor
        private StrokeCollectionSerializer() { }

        /// <summary>
        /// Initialize the Ink serializer
        /// </summary>
        /// <param name="coreStrokes">Pointer to the core stroke collection - avoids recreation of collections</param>
        internal StrokeCollectionSerializer(StrokeCollection coreStrokes)
        {
            _coreStrokes = coreStrokes;
        }

        #endregion

        #region Public Fields

        internal PersistenceFormat CurrentPersistenceFormat = PersistenceFormat.InkSerializedFormat;
        internal CompressionMode CurrentCompressionMode = CompressionMode.Compressed;
        internal System.Collections.Generic.List<int> StrokeIds = null;
        #endregion

        #region Decoding

        #region Public Methods


        /// <summary>
        /// Loads a Ink object from a spcified byte array in the form of Ink Serialzied Format
        /// This method checks for the 'base64:' prefix in the byte[] because that is how V1
        /// saved ISF
        /// </summary>
        /// <param name="inkData"></param>
        internal void DecodeISF(Stream inkData)
        {
            try
            {
                // First examine the input data header
                bool isBase64;
                bool isGif;
                uint cbData;

                ExamineStreamHeader(inkData, out isBase64, out isGif, out cbData);
                if (isBase64)
                {
                    //
                    // this is a funky tablet v1 based byte[] that is base64 encoded...
                    // each 4 bytes in this array corresponds to 3 bytes of ISF data.
                    // EXCEPT the first 7 bytes which are saved with the value
                    // 'base64:' and must not be base64 decoded.
                    // and the last null terminator (if present)
                    //
                    //  The following code does two things:
                    //  1) Convert each byte to a char so it can be base64 decoded
                    //  2) Strips out the first 7 resulting characters
                    //
                    int isfBase64PrefixLength = Base64HeaderBytes.Length;
                    // the previous call to ExamineStreamHeader guarantees that inkData.Length > isfBase64PrefixLength
                    System.Diagnostics.Debug.Assert(inkData.Length > isfBase64PrefixLength);

                    inkData.Position = (long)isfBase64PrefixLength;
                    List<char> charData = new List<char>((int)inkData.Length);
                    int intByte = inkData.ReadByte();
                    while (intByte != -1)
                    {
                        byte b = (byte)intByte;
                        charData.Add((char)b);
                        intByte = inkData.ReadByte();
                    }

                    if (0 == (byte)(charData[charData.Count - 1]))
                    {
                        //strip the null terminator
                        charData.RemoveAt(charData.Count - 1);
                    }

                    char[] chars = charData.ToArray();
                    byte[] isfData = Convert.FromBase64CharArray(chars, 0, chars.Length);
                    MemoryStream ms = new MemoryStream(isfData);
                    if (IsGIFData(ms))
                    {
                        DecodeRawISF(SystemDrawingHelper.GetCommentFromGifStream(ms));
                    }
                    else
                    {
                        DecodeRawISF(ms);
                    }
                }
                else if (true == isGif)
                {
                    DecodeRawISF(SystemDrawingHelper.GetCommentFromGifStream(inkData));
                }
                else
                {
                    DecodeRawISF(inkData);
                }
            }
#if DEBUG
            catch (ArgumentException ex)
            {
                //only include an inner exception in debug builds
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), ex);
            }
            catch (InvalidOperationException ex)
            {
                //only include an inner exception in debug builds
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), ex);
            }
            catch (IndexOutOfRangeException ex)
            {
                //only include an inner exception in debug builds
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), ex);
            }
            catch (NullReferenceException ex)
            {
                //only include an inner exception in debug builds
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), ex);
            }
            catch (EndOfStreamException ex)
            {
                //only include an inner exception in debug builds
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), ex);
            }
            catch (OverflowException ex)
            {
                //only include an inner exception in debug builds
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), ex);
            }
#else
            catch (ArgumentException)
            {
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), "stream");//stream comes from StrokeCollection.ctor()
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), "stream");//stream comes from StrokeCollection.ctor()
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), "stream");//stream comes from StrokeCollection.ctor()
            }
            catch (NullReferenceException)
            {
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), "stream");//stream comes from StrokeCollection.ctor()
            }
            catch (EndOfStreamException)
            {
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), "stream");//stream comes from StrokeCollection.ctor()
            }
            catch (OverflowException)
            {
                throw new ArgumentException(SR.Get(SRID.IsfOperationFailed), "stream");//stream comes from StrokeCollection.ctor()
            }
#endif
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the strokeIds from the stream, we need to do this to decrement the count of bytes
        /// </summary>
        internal uint LoadStrokeIds(Stream isfStream, uint cbSize)
        {
            if (0 == cbSize)
                return 0;

            uint cb;
            uint cbTotal = cbSize;

            // First decode the no of ids
            uint count;

            cb = SerializationHelper.Decode(isfStream, out count);
            if (cb > cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"), "isfStream");

            cbTotal -= cb;
            if (0 == count)
                return (cbSize - cbTotal);

            cb = cbTotal;

            byte[] inputdata = new byte[cb];

            // read the stream
            uint bytesRead = StrokeCollectionSerializer.ReliableRead(isfStream, inputdata, cb);
            if (cb != bytesRead)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Read different size from stream then expected"), "isfStream");
            }
            cbTotal -= cb;

            if (0 != cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"), "isfStream");

            return cbSize;
        }


        private bool IsGIFData(Stream inkdata)
        {
            Debug.Assert(inkdata != null);
            long currentPosition = inkdata.Position;
            try
            {
                return ((byte)inkdata.ReadByte() == 'G' &&
                        (byte)inkdata.ReadByte() == 'I' &&
                        (byte)inkdata.ReadByte() == 'F');
            }
            finally
            {
                //reset position
                inkdata.Position = currentPosition;
            }
        }

        private void ExamineStreamHeader(Stream inkdata, out bool fBase64, out bool fGif, out uint cbData)
        {
            fGif = false;
            cbData = 0;
            fBase64 = false;

            if (inkdata.Length >= 7)
            {
                fBase64 = IsBase64Data(inkdata);
            }

            // Check for RAW gif
            if (!fBase64 && inkdata.Length >= 3)
            {
                fGif = IsGIFData(inkdata);
            }

            return;
        }

        private static readonly byte[] Base64HeaderBytes
                                            = new byte[]{(byte)'b',
                                                        (byte)'a',
                                                        (byte)'s',
                                                        (byte)'e',
                                                        (byte)'6',
                                                        (byte)'4',
                                                        (byte)':'};

#if OLD_ISF
        /// <summary>
        /// Takes an ISF byte[] and populates the StrokeCollection
        ///  attached to this StrokeCollectionSerializer.
        /// </summary>
        /// <param name="inkdata">a byte[] of the raw isf to decode</param>
#else
        /// <summary>
        /// Takes an ISF Stream and populates the StrokeCollection
        ///  attached to this StrokeCollectionSerializer.
        /// </summary>
        /// <param name="inputStream">a Stream the raw isf to decode</param>
#endif
        private void DecodeRawISF(Stream inputStream)
        {
            Debug.Assert(inputStream != null);

            KnownTagCache.KnownTagIndex isfTag;
            uint remainingBytesInStream;
            uint bytesDecodedInCurrentTag = 0;
            bool strokeDescriptorBlockDecoded = false;
            bool drawingAttributesBlockDecoded = false;
            bool metricBlockDecoded = false;
            bool transformDecoded = false;
            uint strokeDescriptorTableIndex = 0;
            uint oldStrokeDescriptorTableIndex = 0xFFFFFFFF;
            uint drawingAttributesTableIndex = 0;
            uint oldDrawingAttributesTableIndex = 0xFFFFFFFF;
            uint metricDescriptorTableIndex = 0;
            uint oldMetricDescriptorTableIndex = 0xFFFFFFFF;
            uint transformTableIndex = 0;
            uint oldTransformTableIndex = 0xFFFFFFFF;
            GuidList guidList = new GuidList();
            int strokeIndex = 0;

            StylusPointDescription currentStylusPointDescription = null;
            Matrix currentTabletToInkTransform = Matrix.Identity;

            _strokeDescriptorTable = new System.Collections.Generic.List<StrokeDescriptor>();
            _drawingAttributesTable = new System.Collections.Generic.List<DrawingAttributes>();
            _transformTable = new System.Collections.Generic.List<TransformDescriptor>();
            _metricTable = new System.Collections.Generic.List<MetricBlock>();

            // First make sure this ink is empty
            if (0 != _coreStrokes.Count || _coreStrokes.ExtendedProperties.Count != 0)
            {
                throw new InvalidOperationException(ISFDebugMessage("ISF decoder cannot operate on non-empty ink container"));
            }
#if OLD_ISF
            //
            // store a compressor reference at this scope, if it is needed (if there is a compresson header) and
            // therefore instanced during this routine, we will dispose of it
            // in the finally block
            //
            Compressor compressor = null;

            try
            {
#endif

            // First read the isfTag
            uint uiTag;
            uint localBytesDecoded = SerializationHelper.Decode(inputStream, out uiTag);
            if (0x00 != uiTag)
                throw new ArgumentException(SR.Get(SRID.InvalidStream));

            // Now read the size of the stream
            localBytesDecoded = SerializationHelper.Decode(inputStream, out remainingBytesInStream);
            ISFDebugTrace("Decoded Stream Size in Bytes: " + remainingBytesInStream.ToString());
            if (0 == remainingBytesInStream)
                return;

            while (0 < remainingBytesInStream)
            {
                bytesDecodedInCurrentTag = 0;

                // First read the isfTag
                localBytesDecoded = SerializationHelper.Decode(inputStream, out uiTag);
                isfTag = (KnownTagCache.KnownTagIndex)uiTag;
                if (remainingBytesInStream >= localBytesDecoded)
                    remainingBytesInStream -= localBytesDecoded;
                else
                {
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));
                }

                ISFDebugTrace("Decoding Tag: " + ((KnownTagCache.KnownTagIndex)isfTag).ToString());
                switch (isfTag)
                {
                    case KnownTagCache.KnownTagIndex.GuidTable:
                    case KnownTagCache.KnownTagIndex.DrawingAttributesTable:
                    case KnownTagCache.KnownTagIndex.DrawingAttributesBlock:
                    case KnownTagCache.KnownTagIndex.StrokeDescriptorTable:
                    case KnownTagCache.KnownTagIndex.StrokeDescriptorBlock:
                    case KnownTagCache.KnownTagIndex.MetricTable:
                    case KnownTagCache.KnownTagIndex.MetricBlock:
                    case KnownTagCache.KnownTagIndex.TransformTable:
                    case KnownTagCache.KnownTagIndex.ExtendedTransformTable:
                    case KnownTagCache.KnownTagIndex.Stroke:
                    case KnownTagCache.KnownTagIndex.CompressionHeader:
                    case KnownTagCache.KnownTagIndex.PersistenceFormat:
                    case KnownTagCache.KnownTagIndex.HimetricSize:
                    case KnownTagCache.KnownTagIndex.StrokeIds:
                        {
                            localBytesDecoded = SerializationHelper.Decode(inputStream, out bytesDecodedInCurrentTag);
                            if (remainingBytesInStream < (localBytesDecoded + bytesDecodedInCurrentTag))
                            {
                                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"), "inputStream");
                            }

                            remainingBytesInStream -= localBytesDecoded;

                            // Based on the isfTag figure out what information we're loading
                            switch (isfTag)
                            {
                                case KnownTagCache.KnownTagIndex.GuidTable:
                                    {
                                        // Load guid Table
                                        localBytesDecoded = guidList.Load(inputStream, bytesDecodedInCurrentTag);
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.DrawingAttributesTable:
                                    {
                                        // Load drawing attributes table
                                        localBytesDecoded = LoadDrawAttrsTable(inputStream, guidList, bytesDecodedInCurrentTag);
                                        drawingAttributesBlockDecoded = true;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.DrawingAttributesBlock:
                                    {
                                        //initialize to V1 defaults, we do it this way as opposed
                                        //to dr.DrawingFlags = 0 because this was a perf hot spot
                                        //and instancing the epc first mitigates it
                                        ExtendedPropertyCollection epc = new ExtendedPropertyCollection();
                                        epc.Add(KnownIds.DrawingFlags, DrawingFlags.Polyline);
                                        DrawingAttributes dr = new DrawingAttributes(epc);
                                        localBytesDecoded = DrawingAttributeSerializer.DecodeAsISF(inputStream, guidList, bytesDecodedInCurrentTag, dr);

                                        _drawingAttributesTable.Add(dr);
                                        drawingAttributesBlockDecoded = true;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.StrokeDescriptorTable:
                                    {
                                        // Load stroke descriptor table
                                        localBytesDecoded = DecodeStrokeDescriptorTable(inputStream, bytesDecodedInCurrentTag);
                                        strokeDescriptorBlockDecoded = true;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.StrokeDescriptorBlock:
                                    {
                                        // Load a single stroke descriptor
                                        localBytesDecoded = DecodeStrokeDescriptorBlock(inputStream, bytesDecodedInCurrentTag);
                                        strokeDescriptorBlockDecoded = true;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.MetricTable:
                                    {
                                        // Load Metric Table
                                        localBytesDecoded = DecodeMetricTable(inputStream, bytesDecodedInCurrentTag);
                                        metricBlockDecoded = true;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.MetricBlock:
                                    {
                                        // Load a single Metric Block
                                        MetricBlock blk;

                                        localBytesDecoded = DecodeMetricBlock(inputStream, bytesDecodedInCurrentTag, out blk);
                                        _metricTable.Clear();
                                        _metricTable.Add(blk);
                                        metricBlockDecoded = true;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.TransformTable:
                                    {
                                        // Load Transform Table
                                        localBytesDecoded = DecodeTransformTable(inputStream, bytesDecodedInCurrentTag, false);
                                        transformDecoded = true;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.ExtendedTransformTable:
                                    {
                                        // non-double transform table should have already been loaded
                                        if (!transformDecoded)
                                        {
                                            throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));
                                        }

                                        // Load double-sized Transform Table
                                        localBytesDecoded = DecodeTransformTable(inputStream, bytesDecodedInCurrentTag, true);
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.PersistenceFormat:
                                    {
                                        uint fmt;

                                        localBytesDecoded = SerializationHelper.Decode(inputStream, out fmt);
                                        // Set the appropriate persistence information
                                        if (0 == fmt)
                                        {
                                            CurrentPersistenceFormat = PersistenceFormat.InkSerializedFormat;
                                        }
                                        else if (0x00000001 == fmt)
                                        {
                                            CurrentPersistenceFormat = PersistenceFormat.Gif;
                                        }


                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.HimetricSize:
                                    {
                                        // Loads the Hi Metric Size for Fortified GIFs
                                        int sz;

                                        localBytesDecoded = SerializationHelper.SignDecode(inputStream, out sz);
                                        if (localBytesDecoded > remainingBytesInStream)
                                            throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));

                                        _himetricSize.X = (double)sz;
                                        localBytesDecoded += SerializationHelper.SignDecode(inputStream, out sz);

                                        _himetricSize.Y = (double)sz;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.CompressionHeader:
                                    {
#if OLD_ISF
                                        byte[] data = new byte[bytesDecodedInCurrentTag];

                                        // read the header from the stream
                                        uint bytesRead = StrokeCollectionSerializer.ReliableRead(inputStream, data, bytesDecodedInCurrentTag);
                                        if (bytesDecodedInCurrentTag != bytesRead)
                                        {
                                            throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Read different size from stream then expected"), "isfStream");
                                        }

                                        uint size = bytesDecodedInCurrentTag;
                                        compressor = new Compressor(data, ref size);
                                        // in case the actual number of bytes read by the compressor
                                        //      is less than the encoder had expected (e.g. compression
                                        //      header was encoded as 10 bytes, but only 7 bytes were read)
                                        //      then we don't want to adjust the stream position because
                                        //      there are likely other following tags that are encoded
                                        //      after the compression tag. This should never happen,
                                        //      so just fail if the compressor is broken or the ISF is
                                        //      corrupted.
                                        if (size != bytesDecodedInCurrentTag)
                                        {
                                            throw new InvalidOperationException(ISFDebugMessage("Compressor intialization reported inconsistent size"));
                                        }
#else
                                        //just advance the inputstream position, we don't need
                                        //no compression header in the new isf decoding
                                        inputStream.Seek(bytesDecodedInCurrentTag, SeekOrigin.Current);
#endif
                                        localBytesDecoded = bytesDecodedInCurrentTag;
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.StrokeIds:
                                    {
                                        localBytesDecoded = LoadStrokeIds(inputStream, bytesDecodedInCurrentTag);
                                        break;
                                    }

                                case KnownTagCache.KnownTagIndex.Stroke:
                                    {
                                        ISFDebugTrace("   Decoding Stroke Id#(" + (strokeIndex + 1).ToString() + ")");

                                        StrokeDescriptor strokeDescriptor = null;

                                        // Load the stroke descriptor based on the index from the list of unique
                                        // stroke descriptors
                                        if (strokeDescriptorBlockDecoded)
                                        {
                                            if (oldStrokeDescriptorTableIndex != strokeDescriptorTableIndex)
                                            {
                                                if (_strokeDescriptorTable.Count <= strokeDescriptorTableIndex)
                                                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));
                                            }

                                            strokeDescriptor = _strokeDescriptorTable[(int)strokeDescriptorTableIndex];
                                        }

                                        // use new transform if the last transform is uninit'd or has changed
                                        if (oldTransformTableIndex != transformTableIndex)
                                        {
                                            // if transform was specified in the ISF stream
                                            if (transformDecoded)
                                            {
                                                if (_transformTable.Count <= transformTableIndex)
                                                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));

                                                // Load the transform descriptor based on the index from the list of unique
                                                // transforn descriptors
                                                currentTabletToInkTransform = LoadTransform(_transformTable[(int)transformTableIndex]);
                                            }

                                            oldTransformTableIndex = transformTableIndex; // cache the transform by remembering the index

                                            // since ISF is stored in HIMETRIC, and we want to expose packet data
                                            //      as Avalon units, we'll update the convert the transform before loading the stroke
                                            currentTabletToInkTransform.Scale(StrokeCollectionSerializer.HimetricToAvalonMultiplier, StrokeCollectionSerializer.HimetricToAvalonMultiplier);
                                        }

                                        MetricBlock metricBlock = null;

                                        // Load the metric block based on the index from the list of unique metric blocks
                                        if (metricBlockDecoded)
                                        {
                                            if (oldMetricDescriptorTableIndex != metricDescriptorTableIndex)
                                            {
                                                if (_metricTable.Count <= metricDescriptorTableIndex)
                                                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));
                                            }

                                            metricBlock = _metricTable[(int)metricDescriptorTableIndex];
                                        }

                                        DrawingAttributes activeDrawingAttributes = null;

                                        // Load the drawing attributes based on the index from the list of unique drawing attributes
                                        if (drawingAttributesBlockDecoded)
                                        {
                                            if (oldDrawingAttributesTableIndex != drawingAttributesTableIndex)
                                            {
                                                if (_drawingAttributesTable.Count <= drawingAttributesTableIndex)
                                                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));

                                                oldDrawingAttributesTableIndex = drawingAttributesTableIndex;
                                            }
                                            DrawingAttributes currDA = (DrawingAttributes)_drawingAttributesTable[(int)drawingAttributesTableIndex];
                                            //we always clone so we don't get strokes that share DAs, which can lead
                                            //to all sorts of unpredictable behavior
                                            activeDrawingAttributes = currDA.Clone();
                                        }

                                        // if we didn't find an existing da to use, instance a new one
                                        if (activeDrawingAttributes == null)
                                        {
                                            activeDrawingAttributes = new DrawingAttributes();
                                        }

                                        // Now create the StylusPacketDescription from the stroke descriptor and metric block
                                        if (oldMetricDescriptorTableIndex != metricDescriptorTableIndex || oldStrokeDescriptorTableIndex != strokeDescriptorTableIndex)
                                        {
                                            currentStylusPointDescription = BuildStylusPointDescription(strokeDescriptor, metricBlock, guidList);
                                            oldStrokeDescriptorTableIndex = strokeDescriptorTableIndex;
                                            oldMetricDescriptorTableIndex = metricDescriptorTableIndex;
                                        }

                                        // Load the stroke
                                        Stroke localStroke;
#if OLD_ISF
                                        localBytesDecoded = StrokeSerializer.DecodeStroke(inputStream, bytesDecodedInCurrentTag, guidList, strokeDescriptor, currentStylusPointDescription, activeDrawingAttributes, currentTabletToInkTransform, compressor, out localStroke);
#else
                                        localBytesDecoded = StrokeSerializer.DecodeStroke(inputStream, bytesDecodedInCurrentTag, guidList, strokeDescriptor, currentStylusPointDescription, activeDrawingAttributes, currentTabletToInkTransform, out localStroke);
#endif

                                        if (localStroke != null)
                                        {
                                            _coreStrokes.AddWithoutEvent(localStroke);
                                            strokeIndex++;
                                        }
                                        break;
                                    }

                                default:
                                    {
                                        throw new InvalidOperationException(ISFDebugMessage("Invalid ISF tag logic"));
                                    }
                            }

                            // if this isfTag's decoded size != expected size, then error out
                            if (localBytesDecoded != bytesDecodedInCurrentTag)
                            {
                                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));
                            }

                            break;
                        }

                    case KnownTagCache.KnownTagIndex.Transform:
                    case KnownTagCache.KnownTagIndex.TransformIsotropicScale:
                    case KnownTagCache.KnownTagIndex.TransformAnisotropicScale:
                    case KnownTagCache.KnownTagIndex.TransformRotate:
                    case KnownTagCache.KnownTagIndex.TransformTranslate:
                    case KnownTagCache.KnownTagIndex.TransformScaleAndTranslate:
                        {
                            // Load a single Transform Block
                            TransformDescriptor xform;

                            bytesDecodedInCurrentTag = DecodeTransformBlock(inputStream, isfTag, remainingBytesInStream, false, out xform);
                            transformDecoded = true;
                            _transformTable.Clear();
                            _transformTable.Add(xform);
                            break;
                        }

                    case KnownTagCache.KnownTagIndex.TransformTableIndex:
                        {
                            // Load the Index into the Transform Table which will be used by the stroke following this till
                            // a next different Index is found
                            bytesDecodedInCurrentTag = SerializationHelper.Decode(inputStream, out transformTableIndex);
                            break;
                        }

                    case KnownTagCache.KnownTagIndex.MetricTableIndex:
                        {
                            // Load the Index into the Metric Table which will be used by the stroke following this till
                            // a next different Index is found
                            bytesDecodedInCurrentTag = SerializationHelper.Decode(inputStream, out metricDescriptorTableIndex);
                            break;
                        }

                    case KnownTagCache.KnownTagIndex.DrawingAttributesTableIndex:
                        {
                            // Load the Index into the Drawing Attributes Table which will be used by the stroke following this till
                            // a next different Index is found
                            bytesDecodedInCurrentTag = SerializationHelper.Decode(inputStream, out drawingAttributesTableIndex);
                            break;
                        }

                    case KnownTagCache.KnownTagIndex.InkSpaceRectangle:
                        {
                            // Loads the Ink Space Rectangle information
                            bytesDecodedInCurrentTag = DecodeInkSpaceRectangle(inputStream, remainingBytesInStream);
                            break;
                        }

                    case KnownTagCache.KnownTagIndex.StrokeDescriptorTableIndex:
                        {
                            // Load the Index into the Stroke Descriptor Table which will be used by the stroke following this till
                            // a next different Index is found
                            bytesDecodedInCurrentTag = SerializationHelper.Decode(inputStream, out strokeDescriptorTableIndex);
                            break;
                        }

                    default:
                        {
                            if ((uint)isfTag >= KnownIdCache.CustomGuidBaseIndex || ((uint)isfTag >= KnownTagCache.KnownTagCount && ((uint)isfTag < (KnownTagCache.KnownTagCount + KnownIdCache.OriginalISFIdTable.Length))))
                            {
                                ISFDebugTrace("  CUSTOM_GUID=" + guidList.FindGuid(isfTag).ToString());

                                // Loads any custom property data
                                bytesDecodedInCurrentTag = remainingBytesInStream;

                                Guid guid = guidList.FindGuid(isfTag);
                                if (guid == Guid.Empty)
                                {
                                    throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Global Custom Attribute tag embedded in ISF stream does not match guid table"), "inkdata");
                                }


                                object data;

                                // load the custom property data from the stream (and decode the type)
                                localBytesDecoded = ExtendedPropertySerializer.DecodeAsISF(inputStream, bytesDecodedInCurrentTag, guidList, isfTag, ref guid, out data);
                                if (localBytesDecoded > bytesDecodedInCurrentTag)
                                {
                                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"), "inkdata");
                                }


                                // add the guid/data pair into the property collection (don't redecode the type)
                                _coreStrokes.ExtendedProperties[guid] = data;
                            }
                            else
                            {
                                // Skip objects that this library doesn't know about
                                // First read the size associated with this unknown isfTag
                                localBytesDecoded = SerializationHelper.Decode(inputStream, out bytesDecodedInCurrentTag);
                                if (remainingBytesInStream < (localBytesDecoded + bytesDecodedInCurrentTag))
                                {
                                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));
                                }
                                else
                                {
                                    inputStream.Seek(bytesDecodedInCurrentTag + localBytesDecoded, SeekOrigin.Current);
                                }
                            }

                            bytesDecodedInCurrentTag = localBytesDecoded;
                            break;
                        }
                }
                ISFDebugTrace("    Size = " + bytesDecodedInCurrentTag.ToString());
                if (bytesDecodedInCurrentTag > remainingBytesInStream)
                {
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"));
                }

                // update remaining ISF buffer length with decoded so far
                remainingBytesInStream -= bytesDecodedInCurrentTag;
            }
#if OLD_ISF
        }
        finally
        {
            if (null != compressor)
            {
                compressor.Dispose();
                compressor = null;
            }
        }
#endif
        if (0 != remainingBytesInStream)
            throw new ArgumentException(ISFDebugMessage("Invalid ISF data"), "inkdata");
    }
#if OLD_ISF
    /// <summary>
    /// Loads a DrawingAttributes Table from the stream and adds individual drawing attributes to the drawattr
    /// list passed
    /// </summary>
    /// <returns></returns>
#else
    /// <summary>
    /// Loads a DrawingAttributes Table from the stream and adds individual drawing attributes to the drawattr
    /// list passed
    /// </summary>
#endif
    private uint LoadDrawAttrsTable(Stream strm, GuidList guidList, uint cbSize)
    {
        _drawingAttributesTable.Clear();

        // First, allocate a temporary buffer and read the stream into it.
        // These will be compressed DRAW_ATTR structures.
        uint cbTotal = cbSize;

        // OK, now we count the number of DRAW_ATTRS compressed into this block
        uint cbDA = 0;

        while (cbTotal > 0)
        {
            // First read the size of the first drawing attributes block
            uint cb = SerializationHelper.Decode(strm, out cbDA);

            if (cbSize < cb)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


            cbTotal -= cb;
            if (cbTotal < cbDA)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");



            // Create a new drawing attribute
            DrawingAttributes attributes = new DrawingAttributes();
            // pull off our defaults onthe drawing attribute as we need to
            //  respect what the ISF has.
            attributes.DrawingFlags = 0;
            cb = DrawingAttributeSerializer.DecodeAsISF(strm, guidList, cbDA, attributes);

            // Load the stream into this attribute
            if (cbSize < cbDA)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


            cbTotal -= cbDA;

            // Add this attribute to the global list
            _drawingAttributesTable.Add(attributes);
        }

        if (0 != cbTotal)
            throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


        return cbSize;
    }

    /// <summary>
    /// Reads and Decodes a stroke descriptor information from the stream. For details on how it is stored
    /// please refer the spec
    /// </summary>
    /// <param name="strm"></param>
    /// <param name="cbSize"></param>
    /// <param name="descr"></param>
    /// <returns></returns>
    private uint DecodeStrokeDescriptor(Stream strm, uint cbSize, out StrokeDescriptor descr)
    {
        descr = new StrokeDescriptor();
        if (0 == cbSize)
            return 0;

        uint cb;
        uint cbBlock = cbSize;

        while (cbBlock > 0)
        {
            // first read the tag
            KnownTagCache.KnownTagIndex tag;
            uint uiTag;

            cb = SerializationHelper.Decode(strm, out uiTag);
            tag = (KnownTagCache.KnownTagIndex)uiTag;
            if (cb > cbBlock)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            cbBlock -= cb;
            descr.Template.Add(tag);

            // If this is TAG_BUTTONS
            if (KnownTagCache.KnownTagIndex.Buttons == tag && cbBlock > 0)
            {
                uint cbButton;

                // Read the no. of buttons first
                cb = SerializationHelper.Decode(strm, out cbButton);
                if (cb > cbBlock)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

                cbBlock -= cb;
                descr.Template.Add((KnownTagCache.KnownTagIndex)cbButton);
                while (cbBlock > 0 && cbButton > 0)
                {
                    uint dw;

                    cb = SerializationHelper.Decode(strm, out dw);
                    if (cb > cbBlock)
                        throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

                    cbBlock -= cb;
                    cbButton--;
                    descr.Template.Add((KnownTagCache.KnownTagIndex)dw);
                }
            }
            else if (KnownTagCache.KnownTagIndex.StrokePropertyList == tag && cbBlock > 0)
            {
                // Usually stroke property comes last in the template. Hence everything below this is
                // are Tags for strokes extended properties
                while (cbBlock > 0)
                {
                    uint dw;

                    cb = SerializationHelper.Decode(strm, out dw);
                    if (cb > cbBlock)
                        throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

                    cbBlock -= cb;
                    descr.Template.Add((KnownTagCache.KnownTagIndex)dw);
                }
            }
            }

            if (0 != cbBlock)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            return cbSize;
        }


        /// <summary>
        /// Reads and Decodes a stroke descriptor information from the stream. For details on how it is stored
        /// please refer the spec
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="cbSize"></param>
        /// <returns></returns>
        private uint DecodeStrokeDescriptorBlock(Stream strm, uint cbSize)
        {
            _strokeDescriptorTable.Clear();
            if (0 == cbSize)
                return 0;

            StrokeDescriptor descr;
            uint cbRead = DecodeStrokeDescriptor(strm, cbSize, out descr);

            if (cbRead != cbSize)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            _strokeDescriptorTable.Add(descr);
            return cbRead;
        }


        /// <summary>
        /// Reads and Decodes a number of stroke descriptor information from the stream. For details on how they are stored
        /// please refer the spec
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="cbSize"></param>
        /// <returns></returns>
        private uint DecodeStrokeDescriptorTable(Stream strm, uint cbSize)
        {
            _strokeDescriptorTable.Clear();
            if (0 == cbSize)
                return 0;

            uint cb;                // Tracks the total no of bytes read from the stream
            uint cbTotal = cbSize;  // Tracks how many more bytes can be read from the stream for the table. Limited by cbSize

            while (cbTotal > 0)
            {
                // First decode the size of the next block
                uint cbBlock;

                cb = SerializationHelper.Decode(strm, out cbBlock);
                if (cb > cbTotal)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                cbTotal -= cb;
                if (cbBlock > cbTotal)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");



                StrokeDescriptor descr;

                cb = DecodeStrokeDescriptor(strm, cbBlock, out descr);
                if (cb != cbBlock)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                cbTotal -= cb;

                // Add this stroke descriptor to the list of global stroke descriptors
                _strokeDescriptorTable.Add(descr);
            }

            if (0 != cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


            return cbSize;
        }


        /// <summary>
        /// Decodes metric table from the stream. For information on how they are stored in the stream, please refer to the spec.
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="cbSize"></param>
        /// <returns></returns>
        private uint DecodeMetricTable(Stream strm, uint cbSize)
        {
            _metricTable.Clear();
            if (cbSize == 0)
                return 0;

            uint cb;
            uint cbTotal = cbSize;

            // This data is a list of Metric block. Each block starts with size of the block. After that it contains an
            // array of Metric Entries. Each metric enty comprises of size of the entry, tag for the property and the metric
            // properties.
            while (cbTotal > 0)
            {
                // First read the size of the metric block
                uint dw;

                cb = SerializationHelper.Decode(strm, out dw);
                if (cb + dw > cbTotal)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                cbTotal -= cb;

                MetricBlock newblock;

                cb = DecodeMetricBlock(strm, dw, out newblock);
                if (cb != dw)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                cbTotal -= cb;
                _metricTable.Add(newblock);
            }

            if (0 != cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


            return cbSize;
        }


        /// <summary>
        /// Decodes a Metric Block from the stream. For information on how they are stored in the stream, please refer to the spec.
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="cbSize"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private uint DecodeMetricBlock(Stream strm, uint cbSize, out MetricBlock block)
        {
            // allocate the block
            block = new MetricBlock();
            if (cbSize == 0)
                return 0;

            uint cb;
            uint cbTotal = cbSize;
            uint size;

            while (cbTotal > 0)
            {
                // First decode the tag for this entry
                uint dw;

                cb = SerializationHelper.Decode(strm, out dw);
                if (cb > cbTotal)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                cbTotal -= cb;

                // Next read the size of the metric data
                cb = SerializationHelper.Decode(strm, out size);
                if (cb + size > cbTotal)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                cbTotal -= cb;

                // now create new metric entry
                MetricEntry entry = new MetricEntry();

                entry.Tag = (KnownTagCache.KnownTagIndex)dw;

                byte[] data = new byte[size];

                uint bytesRead = StrokeCollectionSerializer.ReliableRead(strm, data, size);
                cbTotal -= bytesRead;

                if ( bytesRead != size )
                {
                    // Make sure the bytes read are expected. If not, we should bail out.
                    // An exception will be thrown.
                    break;
                }

                entry.Data = data;
                block.AddMetricEntry(entry);
}

            if (0 != cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


            return cbSize;
        }


        /// <summary>
        /// Reads and Decodes a Table of Transform Descriptors from the stream. For information on how they are stored
        /// in the stream, please refer to the spec.
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="cbSize"></param>
        /// <param name="useDoubles"></param>
        /// <returns></returns>
        private uint DecodeTransformTable(Stream strm, uint cbSize, bool useDoubles)
        {
            // only clear the transform table if not using doubles
            //      (e.g. first pass through transform table)
            if (!useDoubles)
            {
                _transformTable.Clear();
            }

            if (0 == cbSize)
                return 0;

            uint cb;
            uint cbTotal = cbSize;
            int tableIndex = 0;

            while (cbTotal > 0)
            {
                KnownTagCache.KnownTagIndex tag;
                uint uiTag;
                cb = SerializationHelper.Decode(strm, out uiTag);
                tag = (KnownTagCache.KnownTagIndex)uiTag;
                if (cb > cbTotal)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

                cbTotal -= cb;

                TransformDescriptor xform;

                cb = DecodeTransformBlock(strm, tag, cbTotal, useDoubles, out xform);
                cbTotal -= cb;
                if (useDoubles)
                {
                    _transformTable[tableIndex] = xform;
                }
                else
                {
                    _transformTable.Add(xform);
                }

                tableIndex++;
            }

            if (0 != cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            return cbSize;
        }

        /// <summary>
        /// ReliableRead
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="requestedCount"></param>
        /// <returns></returns>
        internal static uint ReliableRead(Stream stream, byte[] buffer, uint requestedCount)
        {
            if (stream == null ||
                buffer == null ||
                requestedCount > buffer.Length)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Invalid argument passed to ReliableRead"));
            }

            // let's read the whole block into our buffer
            uint totalBytesRead = 0;
            while (totalBytesRead < requestedCount)
            {
                int bytesRead = stream.Read(buffer,
                                (int)totalBytesRead,
                                (int)(requestedCount - totalBytesRead));
                if (bytesRead == 0)
                {
                    break;
                }
                totalBytesRead += (uint)bytesRead;
            }
            return totalBytesRead;
        }


        /// <summary>
        /// Reads and Decodes a Transfrom Descriptor Block from the stream. For information on how it is stored in the stream,
        /// please refer to the spec.
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="tag"></param>
        /// <param name="cbSize"></param>
        /// <param name="useDoubles"></param>
        /// <param name="xform"></param>
        /// <returns></returns>
        private uint DecodeTransformBlock(Stream strm, KnownTagCache.KnownTagIndex tag, uint cbSize, bool useDoubles, out TransformDescriptor xform)
        {
            xform = new TransformDescriptor();
            xform.Tag = tag;

            uint cbRead = 0;
            uint cbTotal = cbSize;

            if (0 == cbSize)
                return 0;

            // samgeo - Presharp issue
            // Presharp gives a warning when local IDisposable variables are not closed
            // in this case, we can't call Dispose since it will also close the underlying stream
            // which still needs to be read from
#pragma warning disable 1634, 1691
#pragma warning disable 6518
            BinaryReader bw = new BinaryReader(strm);

            if (KnownTagCache.KnownTagIndex.TransformRotate == tag)
            {
                uint angle;

                cbRead = SerializationHelper.Decode(strm, out angle);
                if (cbRead > cbSize)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                xform.Transform[0] = (double)angle;
                xform.Size = 1;
            }
            else
            {
                if (tag == KnownTagCache.KnownTagIndex.TransformIsotropicScale)
                {
                    xform.Size = 1;
                }
                else if (tag == KnownTagCache.KnownTagIndex.TransformAnisotropicScale || tag == KnownTagCache.KnownTagIndex.TransformTranslate)
                {
                    xform.Size = 2;
                }
                else if (tag == KnownTagCache.KnownTagIndex.TransformScaleAndTranslate)
                {
                    xform.Size = 4;
                }
                else
                {
                    xform.Size = 6;
                }

                if (useDoubles)
                {
                    cbRead = xform.Size * Native.SizeOfDouble;
                }
                else
                {
                    cbRead = xform.Size * Native.SizeOfFloat;
                }

                if (cbRead > cbSize)
                    throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");


                for (int i = 0; i < xform.Size; i++)
                {
                    if (useDoubles)
                    {
                        xform.Transform[i] = bw.ReadDouble();
                    }
                    else
                    {
                        xform.Transform[i] = (double)bw.ReadSingle();
                    }
                }
            }

            return cbRead;
#pragma warning restore 6518
#pragma warning restore 1634, 1691
        }

        /// <summary>
        /// Decodes Ink Space Rectangle information from the stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="cbSize"></param>
        /// <returns></returns>
        private uint DecodeInkSpaceRectangle(Stream strm, uint cbSize)
        {
            uint cb, cbRead = 0;
            uint cbTotal = cbSize;
            int data;

            //Left
            cb = SerializationHelper.SignDecode(strm, out data);
            if (cb > cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            cbTotal -= cb;
            cbRead += cb;
            _inkSpaceRectangle.X = data;
            if (cbRead > cbSize)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            //Top
            cb = SerializationHelper.SignDecode(strm, out data);
            if (cb > cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            cbTotal -= cb;
            cbRead += cb;
            _inkSpaceRectangle.Y = data;
            if (cbRead > cbSize)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            //Right
            cb = SerializationHelper.SignDecode(strm, out data);
            if (cb > cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            cbTotal -= cb;
            cbRead += cb;
            _inkSpaceRectangle.Width = data - _inkSpaceRectangle.Left;
            if (cbRead > cbSize)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            //Bottom
            cb = SerializationHelper.SignDecode(strm, out data);
            if (cb > cbTotal)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            cbTotal -= cb;
            cbRead += cb;
            _inkSpaceRectangle.Height = data - _inkSpaceRectangle.Top;
            if (cbRead > cbSize)
                throw new ArgumentException(ISFDebugMessage("Invalid ISF data"),"strm");

            return cbRead;
        }


        /// <summary>
        /// Creates a Matrix Information structure based on the transform descriptor
        /// </summary>
        /// <param name="tdrd"></param>
        /// <returns></returns>
        private Matrix LoadTransform(TransformDescriptor tdrd)
        {
            double M00 = 0.0f, M01 = 0.0f, M10 = 0.0f, M11 = 0.0f, M20 = 0.0f, M21 = 0.0f;

            if (KnownTagCache.KnownTagIndex.TransformIsotropicScale == tdrd.Tag)
            {
                M00 = M11 = tdrd.Transform[0];
            }
            else if (KnownTagCache.KnownTagIndex.TransformRotate == tdrd.Tag)
            {
                double dAngle = (tdrd.Transform[0] / 100) * (Math.PI / 180);

                M00 = M11 = Math.Cos(dAngle);
                M01 = Math.Sin(dAngle);
                if (M01 == 0.0f && M11 == 1.0f)
                {
                    //special case for 0 degree rotate transforms
                    //this is identity
                    M10 = 0.0f;
                }
                else
                {
                    M10 = -M11;
                }
            }
            else if (KnownTagCache.KnownTagIndex.TransformAnisotropicScale == tdrd.Tag)
            {
                M00 = tdrd.Transform[0];
                M11 = tdrd.Transform[1];
            }
            else if (KnownTagCache.KnownTagIndex.TransformTranslate == tdrd.Tag)
            {
                M20 = tdrd.Transform[0];
                M21 = tdrd.Transform[1];
            }
            else if (KnownTagCache.KnownTagIndex.TransformScaleAndTranslate == tdrd.Tag)
            {
                M00 = tdrd.Transform[0];
                M11 = tdrd.Transform[1];
                M20 = tdrd.Transform[2];
                M21 = tdrd.Transform[3];
            }
            else    // TAG_TRANSFORM
            {
                M00 = tdrd.Transform[0];
                M01 = tdrd.Transform[1];
                M10 = tdrd.Transform[2];
                M11 = tdrd.Transform[3];
                M20 = tdrd.Transform[4];
                M21 = tdrd.Transform[5];
            }

            return new Matrix(M00, M01, M10, M11, M20, M21);
        }


        /// <summary>
        /// Sets the Property Metrics for a property based on Tag and metric descriptor block
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tag"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private StylusPointPropertyInfo GetStylusPointPropertyInfo(Guid guid, KnownTagCache.KnownTagIndex tag, MetricBlock block)
        {
            int dw = 0;
            bool fSetDefault = false;
            uint cbEntry;
            // StylusPointPropertyInfo values that we need to read in.
            int minimum = 0;
            int maximum = 0;
            StylusPointPropertyUnit unit = StylusPointPropertyUnit.None;
            float resolution = 1.0f;

            // To begin with initialize the property metrics with respective default valuses
            // first check if this property belongs to optional list
            for (dw = 0; dw < 11; dw++)
            {
                if (MetricEntry.MetricEntry_Optional[dw].Tag == tag)
                {
                    minimum = MetricEntry.MetricEntry_Optional[dw].PropertyMetrics.Minimum;
                    maximum = MetricEntry.MetricEntry_Optional[dw].PropertyMetrics.Maximum;
                    resolution = MetricEntry.MetricEntry_Optional[dw].PropertyMetrics.Resolution;
                    unit = MetricEntry.MetricEntry_Optional[dw].PropertyMetrics.Unit;
                    fSetDefault = true;
                    break;
                }
            }

            if (false == fSetDefault)
            {
                // We will come here if the property is not found in the Optional List
                // All other cases, we will have only default values
                minimum = Int32.MinValue;
                maximum = Int32.MaxValue;
                unit = StylusPointPropertyUnit.None;
                resolution = 1.0f;
                fSetDefault = true;
            }

            // Now see if there is a valid MetricBlock. If there is one, update the PROPERTY_METRICS with
            // values from this Block
            if (null != block)
            {
                MetricEntry entry = block.GetMetricEntryList();

                while (null != entry)
                {
                    if (entry.Tag == tag)
                    {
                        cbEntry = 0;

                        int range;

                        using (MemoryStream strm = new MemoryStream(entry.Data))
                        {
                            // Decoded the Logical Min
                            cbEntry += SerializationHelper.SignDecode(strm, out range);
                            if (cbEntry >= entry.Size)
                            {
                                break; // return false;
                            }

                            minimum = range;

                            // Logical Max
                            cbEntry += SerializationHelper.SignDecode(strm, out range);
                            if (cbEntry >= entry.Size)
                            {
                                break; // return false;
                            }

                            maximum = range;

                            uint cb;

                            // Units
                            cbEntry += SerializationHelper.Decode(strm, out cb);
                            unit = (StylusPointPropertyUnit)cb;
                            if (cbEntry >= entry.Size)
                            {
                                break; // return false;
                            }

                            using (BinaryReader br = new BinaryReader(strm))
                            {
                                resolution = br.ReadSingle();
                                cbEntry += Native.SizeOfFloat;
                            }
                        }

                        break;
                    }

                    entry = entry.Next;
                }
            }

            // return a new StylusPointPropertyInfo
            return new StylusPointPropertyInfo( new StylusPointProperty(guid, StylusPointPropertyIds.IsKnownButton(guid)),
                                                minimum,
                                                maximum,
                                                unit,
                                                resolution);
        }


        /// <summary>
        /// Builds StylusPointDescription based on StrokeDescriptor and Metric Descriptor Block. Sometime Metric Descriptor block may contain
        /// metric information for properties which are not part of the stroke descriptor. They are simply ignored.
        /// </summary>
        /// <param name="strd"></param>
        /// <param name="block"></param>
        /// <param name="guidList"></param>
        /// <returns></returns>
        private StylusPointDescription BuildStylusPointDescription(StrokeDescriptor strd, MetricBlock block, GuidList guidList)
        {
            int cTags = 0;
            int packetPropertyCount = 0;
            uint buttonCount = 0;
            Guid[] buttonguids = null;
            System.Collections.Generic.List<KnownTagCache.KnownTagIndex> tags = null;

            // if strd is null, it means there is only default descriptor with X & Y
            if (null != strd)
            {
                tags = new System.Collections.Generic.List<KnownTagCache.KnownTagIndex>();
                while (cTags < strd.Template.Count)
                {
                    KnownTagCache.KnownTagIndex tag = (KnownTagCache.KnownTagIndex)strd.Template[cTags];

                    if (KnownTagCache.KnownTagIndex.Buttons == tag)
                    {
                        cTags++;

                        // The next item in the array is no of buttongs.
                        buttonCount = (uint)strd.Template[cTags];
                        cTags++;

                        // Currently we skip the the no of buttons as buttons is not implimented yet
                        buttonguids = new Guid[buttonCount];
                        for (uint u = 0; u < buttonCount; u++)
                        {
                            Guid guid = guidList.FindGuid(strd.Template[cTags]);
                            if (guid == Guid.Empty)
                            {
                                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Button guid tag embedded in ISF stream does not match guid table"),"strd");
                            }

                            buttonguids[(int)u] = guid;
                            cTags++;
                        }
                    }
                    else if (KnownTagCache.KnownTagIndex.StrokePropertyList == tag)
                    {
                        break; // since no more Packet properties can be stored
                    }
                    else
                    {
                        if (KnownTagCache.KnownTagIndex.NoX == tag ||
                            KnownTagCache.KnownTagIndex.NoY == tag)
                        {
                            throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Invalid ISF with NoX or NoY specified"), "strd");
                        }

                        tags.Add(strd.Template[cTags]);
                        packetPropertyCount++;
                        cTags++;
                    }
                }
            }


            List<StylusPointPropertyInfo> stylusPointPropertyInfos = new List<StylusPointPropertyInfo>();
            stylusPointPropertyInfos.Add(GetStylusPointPropertyInfo(KnownIds.X, (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.X), block));
            stylusPointPropertyInfos.Add(GetStylusPointPropertyInfo(KnownIds.Y, (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.Y), block));
            stylusPointPropertyInfos.Add(GetStylusPointPropertyInfo(KnownIds.NormalPressure, (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.NormalPressure), block));

            int pressureIndex = -1;
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    Guid guid = guidList.FindGuid(tags[i]);
                    if (guid == Guid.Empty)
                    {
                        throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Packet Description Property tag embedded in ISF stream does not match guid table"), "strd");
                    }
                    if (pressureIndex == -1 && guid == StylusPointPropertyIds.NormalPressure)
                    {
                        pressureIndex = i + 2; //x,y have already been accounted for
                        continue; //we've already added pressure (above)
                    }

                    stylusPointPropertyInfos.Add(GetStylusPointPropertyInfo(guid, tags[i], block));
                }

                if (null != buttonguids)
                {
                    //
                    // add the buttons to the end of the description if they exist
                    //
                    for (int i = 0; i < buttonguids.Length; i++)
                    {
                        StylusPointProperty buttonProperty = new StylusPointProperty(buttonguids[i], true);
                        StylusPointPropertyInfo buttonInfo = new StylusPointPropertyInfo(buttonProperty);
                        stylusPointPropertyInfos.Add(buttonInfo);
                    }
                }
            }

            return new StylusPointDescription(stylusPointPropertyInfos, pressureIndex);
        }
        #endregion

        #endregion // Decoding

        #region Encoding

        #region Public Methods
#if OLD_ISF
        /// <summary>
        /// This functions Saves the Ink as Ink Serialized Format based on the Compression code
        /// </summary>
        /// <returns>A byte[] with the encoded ISF</returns>
#else
        /// <summary>
        /// This functions Saves the Ink as Ink Serialized Format based on the Compression code
        /// </summary>
        /// <returns>A byte[] with the encoded ISF</returns>
#endif
        internal void EncodeISF(Stream outputStream)
        {
            _strokeLookupTable =
                new System.Collections.Generic.Dictionary<Stroke, StrokeLookupEntry>(_coreStrokes.Count);

            // Next go through all the strokes
            for (int i = 0; i < _coreStrokes.Count; i++)
            {
                _strokeLookupTable.Add(_coreStrokes[i], new StrokeLookupEntry());
            }

            // Initialize all Arraylists
            _strokeDescriptorTable = new List<StrokeDescriptor>(_coreStrokes.Count);
            _drawingAttributesTable = new List<DrawingAttributes>();
            _metricTable = new List<MetricBlock>();
            _transformTable = new List<TransformDescriptor>();

            using (MemoryStream localStream = new MemoryStream(_coreStrokes.Count * 125)) //reasonable default
            {
                GuidList guidList = BuildGuidList();
                uint cumulativeEncodedSize = 0;
                uint localEncodedSize = 0;

                byte xpData = (CurrentCompressionMode == CompressionMode.NoCompression) ? AlgoModule.NoCompression : AlgoModule.DefaultCompression;
                foreach (Stroke s in _coreStrokes)
                {
                    _strokeLookupTable[s].CompressionData = xpData;

                    //
                    // we need to get this data up front so that we can
                    // know if pressure was used (and thus if we need to add Pressure
                    // to the ISF packet description
                    //
                    int[][] isfReadyData;
                    bool shouldStorePressure;
                    s.StylusPoints.ToISFReadyArrays(out isfReadyData, out shouldStorePressure);
                    _strokeLookupTable[s].ISFReadyStrokeData = isfReadyData;
                    //
                    // this is our flag that ToISFReadyArrays sets if pressure was all default
                    //
                    _strokeLookupTable[s].StorePressure = shouldStorePressure;
                }


                // Store Ink space rectangle information if necessary and anything other than default
                if (_inkSpaceRectangle != new Rect())
                {
                    localEncodedSize = cumulativeEncodedSize;

                    Rect inkSpaceRectangle = _inkSpaceRectangle;
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)KnownTagCache.KnownTagIndex.InkSpaceRectangle);

                    int i = (int)inkSpaceRectangle.Left;

                    cumulativeEncodedSize += SerializationHelper.SignEncode(localStream, i);
                    i = (int)inkSpaceRectangle.Top;
                    cumulativeEncodedSize += SerializationHelper.SignEncode(localStream, i);
                    i = (int)inkSpaceRectangle.Right;
                    cumulativeEncodedSize += SerializationHelper.SignEncode(localStream, i);
                    i = (int)inkSpaceRectangle.Bottom;
                    cumulativeEncodedSize += SerializationHelper.SignEncode(localStream, i);

                    // validate that the expected inkspace rectangle block in ISF was the actual size encoded
                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("Encoded InkSpaceRectangle: size=" + localEncodedSize);

                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }

                // First prepare the compressor. Currently Compression is not supported.
                // Next write the persistence format information if anything other than ISF
                // Currently only ISF is implemented
                if (PersistenceFormat.InkSerializedFormat != CurrentPersistenceFormat)
                {
                    localEncodedSize = cumulativeEncodedSize;

                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)KnownTagCache.KnownTagIndex.PersistenceFormat);
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)SerializationHelper.VarSize((uint)CurrentPersistenceFormat));
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)CurrentPersistenceFormat);

                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("Encoded PersistenceFormat: size=" + localEncodedSize);

                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }

                // Future enhancement: store any size information if necessary such as GIF image size

                // Now store the Custom Guids
                localEncodedSize = cumulativeEncodedSize;
                cumulativeEncodedSize += guidList.Save(localStream);
                localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                if (localEncodedSize != 0)
                    ISFDebugTrace("Encoded Custom Guid Table: size=" + localEncodedSize);

                if (cumulativeEncodedSize != localStream.Length)
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));

                // Now build the tables
                BuildTables(guidList);

                // first write the drawing attributes
                localEncodedSize = cumulativeEncodedSize;
                cumulativeEncodedSize += SerializeDrawingAttrsTable(localStream, guidList);
                localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                if (localEncodedSize != 0)
                    ISFDebugTrace("Encoded DrawingAttributesTable: size=" + localEncodedSize);
                if (cumulativeEncodedSize != localStream.Length)
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));

                // Next write the stroke descriptor table
                localEncodedSize = cumulativeEncodedSize;
                cumulativeEncodedSize += SerializePacketDescrTable(localStream);
                localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                if (localEncodedSize != 0)
                    ISFDebugTrace("Encoded Packet Description: size=" + localEncodedSize);
                if (cumulativeEncodedSize != localStream.Length)
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));

                // Write the metric table
                localEncodedSize = cumulativeEncodedSize;
                cumulativeEncodedSize += SerializeMetricTable(localStream);
                localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                if (localEncodedSize != 0)
                    ISFDebugTrace("Encoded Metric Table: size=" + localEncodedSize);
                if (cumulativeEncodedSize != localStream.Length)
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));

                // Write the transform table
                localEncodedSize = cumulativeEncodedSize;
                cumulativeEncodedSize += SerializeTransformTable(localStream);
                localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                if (localEncodedSize != 0)
                    ISFDebugTrace("Encoded Transform Table: size=" + localEncodedSize);
                if (cumulativeEncodedSize != localStream.Length)
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));

                // Save global ink properties
                if (_coreStrokes.ExtendedProperties.Count > 0)
                {
                    localEncodedSize = cumulativeEncodedSize;
                    cumulativeEncodedSize += ExtendedPropertySerializer.EncodeAsISF(_coreStrokes.ExtendedProperties, localStream, guidList, GetCompressionAlgorithm(), true);
                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("Encoded Global Ink Attributes Table: size=" + localEncodedSize);
                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }

                // Save stroke ids
                localEncodedSize = cumulativeEncodedSize;
                cumulativeEncodedSize += SaveStrokeIds(_coreStrokes, localStream, false);
                localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                if (localEncodedSize != 0)
                    ISFDebugTrace("Encoded Stroke Id List: size=" + localEncodedSize);
                if (cumulativeEncodedSize != localStream.Length)
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));

                StoreStrokeData(localStream, guidList, ref cumulativeEncodedSize, ref localEncodedSize);

                ISFDebugTrace("Embedded ISF Stream size=" + cumulativeEncodedSize);

                // Now that all data has been written we need to prepend the stream
                long preEncodingPosition = outputStream.Position;
                uint cbFinal = SerializationHelper.Encode(outputStream, (uint)0x00);

                cbFinal += SerializationHelper.Encode(outputStream, cumulativeEncodedSize);

                //we have to use localStream to encode ISF because we have to place a variable byte 'size of isf' at the
                //beginning of the stream
                outputStream.Write(localStream.GetBuffer(), 0, (int)cumulativeEncodedSize);
                cbFinal += cumulativeEncodedSize;

                ISFDebugTrace("Final ISF Stream size=" + cbFinal);

                if (cbFinal != outputStream.Position - preEncodingPosition)
                {
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }
            }
        }

#if OLD_ISF
        /// <Summary>
        /// Encodes all of the strokes in a strokecollection to ISF
        /// </Summary>
#else
        /// <Summary>
        /// Encodes all of the strokes in a strokecollection to ISF
        /// </Summary>
#endif
        private void StoreStrokeData(Stream localStream, GuidList guidList, ref uint cumulativeEncodedSize, ref uint localEncodedSize)
        {
            // Now we will save the stroke data
            uint currentDrawingAttributesTableIndex = 0;
            uint currentStrokeDescriptorTableIndex = 0;
            uint uCurrMetricDescriptorTableIndex = 0;
            uint currentTransformTableIndex = 0;

            int[] strokeIds = StrokeIdGenerator.GetStrokeIds(_coreStrokes);
            for (int i = 0; i < _coreStrokes.Count; i++)
            {
                Stroke s = _coreStrokes[i];
                uint cbStroke = 0;

                ISFDebugTrace("Encoding Stroke Id#" + strokeIds[i]);

                // if the drawing attribute index is different from the current one, write it
                if (currentDrawingAttributesTableIndex != _strokeLookupTable[s].DrawingAttributesTableIndex)
                {
                    localEncodedSize = cumulativeEncodedSize;
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)KnownTagCache.KnownTagIndex.DrawingAttributesTableIndex);
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, _strokeLookupTable[s].DrawingAttributesTableIndex);
                    currentDrawingAttributesTableIndex = _strokeLookupTable[s].DrawingAttributesTableIndex;
                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("    Encoded DrawingAttribute Table Index: size=" + localEncodedSize);
                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }

                // if the stroke descriptor index is different from the current one, write it
                if (currentStrokeDescriptorTableIndex != _strokeLookupTable[s].StrokeDescriptorTableIndex)
                {
                    localEncodedSize = cumulativeEncodedSize;
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)KnownTagCache.KnownTagIndex.StrokeDescriptorTableIndex);
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, _strokeLookupTable[s].StrokeDescriptorTableIndex);
                    currentStrokeDescriptorTableIndex = _strokeLookupTable[s].StrokeDescriptorTableIndex;
                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("    Encoded Stroke Descriptor Index: size=" + localEncodedSize);
                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }

                // if the metric table index is different from the current one, write it
                if (uCurrMetricDescriptorTableIndex != _strokeLookupTable[s].MetricDescriptorTableIndex)
                {
                    localEncodedSize = cumulativeEncodedSize;
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)KnownTagCache.KnownTagIndex.MetricTableIndex);
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, _strokeLookupTable[s].MetricDescriptorTableIndex);
                    uCurrMetricDescriptorTableIndex = _strokeLookupTable[s].MetricDescriptorTableIndex;
                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("    Encoded Metric Index: size=" + localEncodedSize);
                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }

                // if the Transform index is different from the current one, write it
                if (currentTransformTableIndex != _strokeLookupTable[s].TransformTableIndex)
                {
                    localEncodedSize = cumulativeEncodedSize;
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)KnownTagCache.KnownTagIndex.TransformTableIndex);
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, _strokeLookupTable[s].TransformTableIndex);
                    currentTransformTableIndex = _strokeLookupTable[s].TransformTableIndex;
                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("    Encoded Transform Index: size=" + localEncodedSize);
                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }

                // now create a separate Memory Stream object which will be used for storing the saved stroke data temporarily
                using (MemoryStream tempstrm = new MemoryStream(s.StylusPoints.Count * 5)) //good approximation based on profiling isf files
                {
                    localEncodedSize = cumulativeEncodedSize;
#if OLD_ISF
                    // Now save the stroke in the temp stream
                    cbStroke = StrokeSerializer.EncodeStroke(s, tempstrm, null/*we never use CompressionMode.Max)*/, GetCompressionAlgorithm(), guidList, _strokeLookupTable[s]);
#else
                    cbStroke = StrokeSerializer.EncodeStroke(s, tempstrm, GetCompressionAlgorithm(), guidList, _strokeLookupTable[s]);
#endif

                    if (cbStroke != tempstrm.Length)
                    {
                        throw new InvalidOperationException(ISFDebugMessage("Encoded stroke size != reported size"));
                    }

                    // Now write the tag KnownTagCache.KnownTagIndex.Stroke
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, (uint)KnownTagCache.KnownTagIndex.Stroke);
                    ISFDebugTrace("Stroke size=" + tempstrm.Length);

                    // Now write the size of the stroke
                    cumulativeEncodedSize += SerializationHelper.Encode(localStream, cbStroke);

                    // Finally write the stroke data
                    localStream.Write(tempstrm.GetBuffer(), 0, (int)cbStroke);
                    cumulativeEncodedSize += cbStroke;

                    localEncodedSize = cumulativeEncodedSize - localEncodedSize;
                    if (localEncodedSize != 0)
                        ISFDebugTrace("Encoding Stroke Id#" + strokeIds[i] + " size=" + localEncodedSize);
                    if (cumulativeEncodedSize != localStream.Length)
                        throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
                }
                if (cumulativeEncodedSize != localStream.Length)
                    throw new InvalidOperationException(ISFDebugMessage("Calculated ISF stream size != actual stream size"));
            }
        }
#if OLD_ISF
        /// <summary>
        /// Saves the stroke Ids in the stream.
        /// </summary>
        /// <param name="strokes"></param>
        /// <param name="strm"></param>
        /// <param name="forceSave">save ids even if they are contiguous</param>
        /// <returns></returns>
#else
        /// <summary>
        /// Saves the stroke Ids in the stream.
        /// </summary>
        /// <param name="strokes"></param>
        /// <param name="strm"></param>
        /// <param name="forceSave">save ids even if they are contiguous</param>
#endif
        internal static uint SaveStrokeIds(StrokeCollection strokes, Stream strm, bool forceSave)
        {
            if (0 == strokes.Count)
                return 0;

            // Define an ArrayList to store the stroke ids
            int[] strkIds = StrokeIdGenerator.GetStrokeIds(strokes);

            // First enumerate all strokes to collect the ids and also check if the follow the default sequence.
            // If they do we don't save the stroke ids
            bool fDefIds = true;

            if (!forceSave)
            {
                // since the stroke allocation algorithm is i++, we check if any
                //  values are not equal to the sequential and consecutive list
                for (int i = 0; i < strkIds.Length; i++)
                {
                    if (strkIds[i] != (i + 1))
                    {
                            // if non-sequential or non-consecutive, then persist the ids
                        fDefIds = false;
                        break;
                    }
                }
                // no need to store them if all of them follow the default sequence
                if (fDefIds) return 0;
            }

            // The format is as follows
            // <Tag.StrokeIds> <Encoded Size of Stroke Id data> <StrokeId Count> <Huff compressed array of longs>
            // Encode size of stroke count
            // First write the KnownTagCache.KnownTagIndex.StrokeIds
            uint cbWrote = SerializationHelper.Encode(strm, (uint)KnownTagCache.KnownTagIndex.StrokeIds);

            ISFDebugTrace("Saved KnownTagCache.KnownTagIndex.StrokeIds size=" + cbWrote.ToString());

            // First findout the no of bytes required to huffman compress these ids
            byte algorithm = AlgoModule.DefaultCompression;
#if OLD_ISF
            byte[] data = Compressor.CompressPacketData(null, strkIds, ref algorithm);
#else
            byte[] data = Compressor.CompressPacketData(strkIds, ref algorithm);
#endif


            if (data != null)
            {
                // First write the encoded size of the buffer
                cbWrote += SerializationHelper.Encode(strm, (uint)(data.Length + SerializationHelper.VarSize((uint)strokes.Count)));

                // Write the count of ids
                cbWrote += SerializationHelper.Encode(strm, (uint)strokes.Count);
                strm.Write(data, 0, (int)data.Length);
                cbWrote += (uint)data.Length;
            }
            // If compression fails for some reason, write the uncompressed data
            else
            {
                byte bCompAlgo = AlgoModule.NoCompression;

                // Find out the size of the data + size of the id count
                uint cbStrokeId = (uint)(strokes.Count * Native.SizeOfInt + 1 + SerializationHelper.VarSize((uint)strokes.Count)); // 1 is for the compression header

                cbWrote += SerializationHelper.Encode(strm, cbStrokeId);
                cbWrote += SerializationHelper.Encode(strm, (uint)strokes.Count);
                strm.WriteByte(bCompAlgo);
                cbWrote++;

                // Now write all the ids in the stream
                // samgeo - Presharp issue
                // Presharp gives a warning when local IDisposable variables are not closed
                // in this case, we can't call Dispose since it will also close the underlying stream
                // which still needs to be written to
#pragma warning disable 1634, 1691
#pragma warning disable 6518
                BinaryWriter bw = new BinaryWriter(strm);

                for (int i = 0; i < strkIds.Length; i++)
                {
                    bw.Write(strkIds[i]);
                    cbWrote += Native.SizeOfInt;
                }
#pragma warning restore 6518
#pragma warning restore 1634, 1691
            }

            return cbWrote;
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// Simple helper method to examine the first 7 members (if they exist)
        /// of the byte[] and see if they have the ascii characters 'base64:' in them.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool IsBase64Data(Stream data)
        {
            Debug.Assert(data != null);
            long currentPosition = data.Position;
            try
            {
                byte[] isfBase64PrefixBytes = Base64HeaderBytes;
                if (data.Length < isfBase64PrefixBytes.Length)
                {
                    return false;
                }

                for (int x = 0; x < isfBase64PrefixBytes.Length; x++)
                {
                    if ((byte)data.ReadByte() != isfBase64PrefixBytes[x])
                    {
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                //reset position
                data.Position = currentPosition;
            }
        }

        /// <summary>
        /// Builds the GuidList based on ExtendedPropeties and StrokeCollection
        /// </summary>
        /// <returns></returns>
        private GuidList BuildGuidList()
        {
            GuidList guidList = new GuidList();
            int i = 0;

            // First go through the list of ink properties
            ExtendedPropertyCollection attributes = _coreStrokes.ExtendedProperties;
            for (i = 0; i < attributes.Count; i++)
            {
                guidList.Add(attributes[i].Id);
            }

            // Next go through all the strokes
            for (int j = 0; j < _coreStrokes.Count; j++)
            {
                BuildStrokeGuidList(_coreStrokes[j], guidList);
            }

            return guidList;
        }
        /// <summary>
        /// Builds the list of Custom Guids that were used by this particular stroke, either in the packet layout
        /// or in the drawing attributes, or in the buttons or in Extended properties or in the point properties
        /// and updates the guidlist with that information
        /// </summary>
        /// <param name="stroke"></param>
        /// <param name="guidList"></param>
        private void BuildStrokeGuidList(Stroke stroke, GuidList guidList)
        {
            int i = 0;

            // First drawing attributes
            //      Ignore the default Guids/attributes in the DrawingAttributes
            int count;
            Guid[] guids = ExtendedPropertySerializer.GetUnknownGuids(stroke.DrawingAttributes.ExtendedProperties, out count);

            for (i = 0; i < count; i++)
            {
                guidList.Add(guids[i]);
            }

            Guid[] descriptionGuids = stroke.StylusPoints.Description.GetStylusPointPropertyIds();
            for (i = 0; i < descriptionGuids.Length; i++)
            {
                guidList.Add(descriptionGuids[i]);
            }

            if (stroke.ExtendedProperties.Count > 0)
            {
                // Add the ExtendedProperty guids in the list
                for (i = 0; i < stroke.ExtendedProperties.Count; i++)
                {
                    guidList.Add(stroke.ExtendedProperties[i].Id);
                }
            }
        }


        private byte GetCompressionAlgorithm()
        {
            if (CompressionMode.Compressed == CurrentCompressionMode)
            {
                return AlgoModule.DefaultCompression;
            }
            return AlgoModule.NoCompression;
        }


        /// <summary>
        /// This function serializes Stroke Descriptor Table in the stream. For information on how they are serialized, please refer to the spec.
        ///
        /// </summary>
        /// <param name="strm"></param>
        /// <returns></returns>
        private uint SerializePacketDescrTable(Stream strm)
        {
            if (_strokeDescriptorTable.Count == 0)
                return 0;

            int count = 0;
            uint cbData = 0;

            // First add the appropriate header information
            if (_strokeDescriptorTable.Count == 1)
            {
                StrokeDescriptor tmp = _strokeDescriptorTable[0];

                // If there is no tag, that means default template and only one entry in the list. Return from here
                if (tmp.Template.Count == 0)
                    return 0;
                else
                {
                    // Write it out directly
                    // First the tag
                    cbData += SerializationHelper.Encode(strm, (uint)KnownTagCache.KnownTagIndex.StrokeDescriptorBlock);

                    // Now encode the descriptor itself
                    cbData += EncodeStrokeDescriptor(strm, tmp);
                }
            }
            else
            {
                uint cbTotal = 0;

                // First calculate the total encoded size of the all the Templates
                for (count = 0; count < _strokeDescriptorTable.Count; count++)
                {
                    cbTotal += SerializationHelper.VarSize((_strokeDescriptorTable[count]).Size) + (_strokeDescriptorTable[count]).Size;
                }

                // Now write the Tag
                cbData += SerializationHelper.Encode(strm, (uint)KnownTagCache.KnownTagIndex.StrokeDescriptorTable);
                cbData += SerializationHelper.Encode(strm, cbTotal);

                // Now write the encoded templates
                for (count = 0; count < _strokeDescriptorTable.Count; count++)
                {
                    cbData += EncodeStrokeDescriptor(strm, _strokeDescriptorTable[count]);
                }
            }

            return cbData;
        }


        /// <summary>
        /// This function serializes Metric Descriptor Table in the stream. For information on how they are serialized, please refer to the spec.
        /// </summary>
        /// <param name="strm"></param>
        /// <returns></returns>
        private uint SerializeMetricTable(Stream strm)
        {
            uint cSize = 0;
            MetricBlock block;

            if (0 == _metricTable.Count)
                return 0;

            for (int i = 0; i < _metricTable.Count; i++)
                cSize += _metricTable[i].Size;

            uint cbData = 0;

            // if total size of the blocks is 1, then there is nothing to write
            //  the reason that the size of the blocks is 1 instead of 0 is because
            //  MetricBlock.Size returns the size of the block plus the byte encoded
            //  size value itself. If the MetricBlock size value is 0, then byte
            //  encoded size value is 0, which has a byte size of 1.
            if (1 == cSize)
            {
                return 0;
            }
            else if (1 == _metricTable.Count)
            {
                cbData += SerializationHelper.Encode(strm, (uint)KnownTagCache.KnownTagIndex.MetricBlock);
            }
            else
            {
                cbData += SerializationHelper.Encode(strm, (uint)KnownTagCache.KnownTagIndex.MetricTable);
                cbData += SerializationHelper.Encode(strm, cSize);
            }

            for (int i = 0; i < _metricTable.Count; i++)
            {
                block = _metricTable[i];
                cbData += block.Pack(strm);
            }

            return cbData;
        }


        /// <summary>
        /// Multibyte Encodes a Stroke Descroptor
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="strd"></param>
        /// <returns></returns>
        private uint EncodeStrokeDescriptor(Stream strm, StrokeDescriptor strd)
        {
            uint cbData = 0;

            // First encode the size of the descriptor
            cbData += SerializationHelper.Encode(strm, strd.Size);
            for (int count = 0; count < strd.Template.Count; count++)
            {
                // Now encode all members of the descriptor
                cbData += SerializationHelper.Encode(strm, (uint)strd.Template[count]);
            }

            return cbData;
        }


        /// <summary>
        /// This function serializes Transform Descriptor Table in the stream. For information on how they are serialized, please refer to the spec.
        /// </summary>
        /// <param name="strm"></param>
        /// <returns></returns>
        private uint SerializeTransformTable(Stream strm)
        {
            // If there is only one entry in the TransformDescriptor table
            //      and it is the default descriptor, skip serialization of transforms
            if (_transformTable.Count == 1 && _transformTable[0].Size == 0)
            {
                return 0;
            }

            uint floatTotal = 0;
            uint doubleTotal = 0;

            // First count the size of all transforms (handling both float && double versions)
            for (int i = 0; i < _transformTable.Count; i++)
            {
                TransformDescriptor xform = _transformTable[i];
                uint cbLocal = SerializationHelper.VarSize((uint)xform.Tag);

                floatTotal += cbLocal;
                doubleTotal += cbLocal;
                if (KnownTagCache.KnownTagIndex.TransformRotate == xform.Tag)
                {
                    cbLocal = SerializationHelper.VarSize((uint)(xform.Transform[0] + 0.5f));
                    floatTotal += cbLocal;
                    doubleTotal += cbLocal;
                }
                else
                {
                    cbLocal = xform.Size * Native.SizeOfFloat;
                    floatTotal += cbLocal;
                    doubleTotal += cbLocal * 2;
                }
            }

            uint cbTotal = 0;

            // If there is only one entry in the TransformDescriptor table
            if (_transformTable.Count == 1)
            {
                TransformDescriptor xform = _transformTable[0];

                cbTotal = EncodeTransformDescriptor(strm, xform, false);
            }
            else
            {
                // Now first write the block descriptor and then write all transforms
                cbTotal += SerializationHelper.Encode(strm, (uint)KnownTagCache.KnownTagIndex.TransformTable);
                cbTotal += SerializationHelper.Encode(strm, floatTotal);
                for (int i = 0; i < _transformTable.Count; i++)
                {
                    cbTotal += EncodeTransformDescriptor(strm, _transformTable[i], false);
                }
            }
            // now write the Extended Transform table (using doubles instead of floats)
            { // note that we do not distinguish between 1 and > 1 transforms for compression
                // Now first write the block descriptor and then write all transforms
                cbTotal += SerializationHelper.Encode(strm, (uint)KnownTagCache.KnownTagIndex.ExtendedTransformTable);
                cbTotal += SerializationHelper.Encode(strm, doubleTotal);
                for (int i = 0; i < _transformTable.Count; i++)
                {
                    cbTotal += EncodeTransformDescriptor(strm, _transformTable[i], true);
                }
            }
            return cbTotal;
        }


        /// <summary>
        /// Multibyte Encode if necessary a Transform Descriptor into the stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="xform"></param>
        /// <param name="useDoubles"></param>
        /// <returns></returns>
        private uint EncodeTransformDescriptor(Stream strm, TransformDescriptor xform, bool useDoubles)
        {
            uint cbData = 0;

            // First encode the tag
            cbData = SerializationHelper.Encode(strm, (uint)xform.Tag);

            // Encode specially if transform denotes rotation
            if (KnownTagCache.KnownTagIndex.TransformRotate == xform.Tag)
            {
                uint angle = (uint)(xform.Transform[0] + 0.5f);

                cbData += SerializationHelper.Encode(strm, angle);
            }
            else
            {
                // samgeo - Presharp issue
                // Presharp gives a warning when local IDisposable variables are not closed
                // in this case, we can't call Dispose since it will also close the underlying stream
                // which still needs to be written to
#pragma warning disable 1634, 1691
#pragma warning disable 6518
                BinaryWriter bw = new BinaryWriter(strm);

                for (int i = 0; i < xform.Size; i++)
                {
                    // note that the binary writer changes serialization
                    //      lengths depending on the Write parameter cast
                    if (useDoubles)
                    {
                        bw.Write(xform.Transform[i]);
                        cbData += Native.SizeOfDouble;
                    }
                    else
                    {
                        bw.Write((float)xform.Transform[i]);
                        cbData += Native.SizeOfFloat;
                    }
                }
#pragma warning restore 6518
#pragma warning restore 1634, 1691
            }

            return cbData;
        }

#if OLD_ISF
        /// <summary>
        /// This function serializes Drawing Attributes Table in the stream. For information on how they are serialized, please refer to the spec.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="guidList"></param>
        /// <returns></returns>
#else
        /// <summary>
        /// This function serializes Drawing Attributes Table in the stream. For information on how they are serialized, please refer to the spec.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="guidList"></param>
#endif
        private uint SerializeDrawingAttrsTable(Stream stream, GuidList guidList)
        {
            uint totalSizeOfSerializedBytes = 0;
            uint sizeOfHeaderInBytes = 0;

            if (1 == _drawingAttributesTable.Count)
            {
                //we always serialize a single DA, even if it has default values so we will write width back to the stream
                DrawingAttributes drawingAttributes = _drawingAttributesTable[0];

                // There is single drawing attribute. Save it along with the size
                totalSizeOfSerializedBytes += SerializationHelper.Encode(stream, (uint)KnownTagCache.KnownTagIndex.DrawingAttributesBlock);

                // Get the size of the saved bytes
                using (MemoryStream drawingAttributeStream = new MemoryStream(16)) //reasonable default based onn profiling
                {
                    sizeOfHeaderInBytes = DrawingAttributeSerializer.EncodeAsISF(drawingAttributes, drawingAttributeStream, guidList, 0, true);

                    // Write the size first
                    totalSizeOfSerializedBytes += SerializationHelper.Encode(stream, sizeOfHeaderInBytes);

                    // write the data
                    uint bytesWritten = Convert.ToUInt32(drawingAttributeStream.Position);
                    totalSizeOfSerializedBytes += bytesWritten;
                    Debug.Assert(sizeOfHeaderInBytes == bytesWritten);

                    stream.Write(   drawingAttributeStream.GetBuffer(), //returns a direct ref, no copied
                                    0,
                                    Convert.ToInt32(bytesWritten));

                    drawingAttributeStream.Dispose();
                }
            }
            else
            {
                // Temporarily declare an array to hold the size of the saved drawing attributes
                uint[] sizes = new uint[_drawingAttributesTable.Count];
                MemoryStream[] drawingAttributeStreams = new MemoryStream[_drawingAttributesTable.Count];

                // First calculate the size of each attribute
                for (int i = 0; i < _drawingAttributesTable.Count; i++)
                {
                    DrawingAttributes drawingAttributes = _drawingAttributesTable[i];
                    drawingAttributeStreams[i] = new MemoryStream(16); //reasonable default based on profiling

                    sizes[i] = DrawingAttributeSerializer.EncodeAsISF(drawingAttributes, drawingAttributeStreams[i], guidList, 0, true);
                    sizeOfHeaderInBytes += SerializationHelper.VarSize(sizes[i]) + sizes[i];
                }

                // Now write the KnownTagCache.KnownTagIndex.DrawingAttributesTable first, then sizeOfHeaderInBytes and then individual Drawing Attributes
                totalSizeOfSerializedBytes = SerializationHelper.Encode(stream, (uint)KnownTagCache.KnownTagIndex.DrawingAttributesTable);

                totalSizeOfSerializedBytes += SerializationHelper.Encode(stream, sizeOfHeaderInBytes);
                for (int i = 0; i < _drawingAttributesTable.Count; i++)
                {
                    DrawingAttributes drawingAttributes = _drawingAttributesTable[i];

                    // write the size of the block
                    totalSizeOfSerializedBytes += SerializationHelper.Encode(stream, sizes[i]);

                    // write the saved data
                    uint bytesWritten = Convert.ToUInt32(drawingAttributeStreams[i].Position);
                    totalSizeOfSerializedBytes += bytesWritten;
                    Debug.Assert(sizes[i] == bytesWritten);

                    stream.Write(   drawingAttributeStreams[i].GetBuffer(), //returns a direct ref, no copies
                                    0,
                                    Convert.ToInt32(bytesWritten));

                    drawingAttributeStreams[i].Dispose();
                }
            }

            return totalSizeOfSerializedBytes;
        }

        /// <summary>
        /// This function builds list of all unique Tables, ie Stroke Descriptor Table, Metric Descriptor Table, Transform Descriptor Table
        /// and Drawing Attributes Table based on all the strokes. Each entry in the Table is unique with respect to the table.
        /// </summary>
        /// <param name="guidList"></param>
        private void BuildTables(GuidList guidList)
        {
            _transformTable.Clear();
            _strokeDescriptorTable.Clear();
            _metricTable.Clear();
            _drawingAttributesTable.Clear();

            int count = 0;

            for (count = 0; count < _coreStrokes.Count; count++)
            {
                Stroke stroke = _coreStrokes[count];

                // First get the updated descriptor from the stroke
                StrokeDescriptor strokeDescriptor;
                MetricBlock metricBlock;
                StrokeSerializer.BuildStrokeDescriptor(stroke, guidList, _strokeLookupTable[stroke], out strokeDescriptor, out metricBlock);
                bool fMatch = false;

                // Compare this with all the global stroke descriptor for a match
                for (int descriptorIndex = 0; descriptorIndex < _strokeDescriptorTable.Count; descriptorIndex++)
                {
                    if (strokeDescriptor.IsEqual(_strokeDescriptorTable[descriptorIndex]))
                    {
                        fMatch = true;
                        _strokeLookupTable[stroke].StrokeDescriptorTableIndex = (uint)descriptorIndex;
                        break;
                    }
                }
                if (false == fMatch)
                {
                    _strokeDescriptorTable.Add(strokeDescriptor);
                    _strokeLookupTable[stroke].StrokeDescriptorTableIndex = (uint)_strokeDescriptorTable.Count - 1;
                }

                // If there is at least one entry in the metric block, check if the current Block is equvalent to
                // any of the existing one.
                fMatch = false;
                for (int tmp = 0; tmp < _metricTable.Count; tmp++)
                {
                    MetricBlock block = _metricTable[tmp];
                    SetType type = SetType.SubSet;

                    if (block.CompareMetricBlock(metricBlock, ref type))
                    {
                        // This entry exists in the list. If it is a subset of the element, do nothing.
                        // Otherwise, replace the entry with this one
                        if (type == SetType.SuperSet)
                        {
                            _metricTable[tmp] = metricBlock;
                        }

                        fMatch = true;
                        _strokeLookupTable[stroke].MetricDescriptorTableIndex = (uint)tmp;
                        break;
                    }
                }

                if (false == fMatch)
                {
                    _metricTable.Add(metricBlock);
                    _strokeLookupTable[stroke].MetricDescriptorTableIndex = (uint)(_metricTable.Count - 1);
                }

                // Now build the Transform Table
                fMatch = false;

                //
                // always identity
                //
                TransformDescriptor xform = StrokeCollectionSerializer.IdentityTransformDescriptor;

                // First check to see if this matches with any existing Transform Blocks
                for (int i = 0; i < _transformTable.Count; i++)
                {
                    if (true == xform.Compare(_transformTable[i]))
                    {
                        fMatch = true;
                        _strokeLookupTable[stroke].TransformTableIndex = (uint)i;
                        break;
                    }
                }

                if (false == fMatch)
                {
                    _transformTable.Add(xform);
                    _strokeLookupTable[stroke].TransformTableIndex = (uint)(_transformTable.Count - 1);
                }

                // Now build the drawing attributes table
                fMatch = false;

                DrawingAttributes drattrs = _coreStrokes[count].DrawingAttributes;

                // First check to see if this matches with any existing transform blocks
                for (int i = 0; i < _drawingAttributesTable.Count; i++)
                {
                    if (true == drattrs.Equals(_drawingAttributesTable[i]))
                    {
                        fMatch = true;
                        _strokeLookupTable[stroke].DrawingAttributesTableIndex = (uint)i;
                        break;
                    }
                }

                if (false == fMatch)
                {
                    _drawingAttributesTable.Add(drattrs);
                    _strokeLookupTable[stroke].DrawingAttributesTableIndex = (uint)_drawingAttributesTable.Count - 1;
                }
            }
        }

        #endregion // Private Methods

        internal class StrokeLookupEntry
        {
            internal uint MetricDescriptorTableIndex = 0;
            internal uint StrokeDescriptorTableIndex = 0;
            internal uint TransformTableIndex = 0;
            internal uint DrawingAttributesTableIndex = 0;

            // Compression algorithm data
            internal byte CompressionData = 0;

            internal int[][] ISFReadyStrokeData = null;
            internal bool StorePressure = false;
        }

        #endregion // Encoding

        #region Debugging Methods

        [System.Diagnostics.Conditional("DEBUG_ISF")]
        static void ISFDebugTrace(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
        #endregion

        // [System.Diagnostics.Conditional("DEBUG_ISF")]
        internal static string ISFDebugMessage(string debugMessage)
        {
#if DEBUG
            return debugMessage;
#else
            return SR.Get(SRID.IsfOperationFailed);
#endif
        }

        #region Private Fields

        StrokeCollection _coreStrokes;
        private System.Collections.Generic.List<StrokeDescriptor> _strokeDescriptorTable = null;
        private System.Collections.Generic.List<TransformDescriptor> _transformTable = null;
        private System.Collections.Generic.List<DrawingAttributes> _drawingAttributesTable = null;
        private System.Collections.Generic.List<MetricBlock> _metricTable = null;
        private Vector _himetricSize = new Vector(0.0f, 0.0f);


            // The ink space rectangle (e.g. bounding box for GIF) is stored
            //      with the serialization info so that load/save roundtrip the
            //      rectangle
        private Rect _inkSpaceRectangle = new Rect();

        System.Collections.Generic.Dictionary<Stroke, StrokeLookupEntry> _strokeLookupTable = null;

        #endregion
    }

    /// <summary>
    /// Simple static method for generating StrokeIds
    /// </summary>
    internal static class StrokeIdGenerator
    {
        /// <summary>
        /// Generates backwards compatible StrokeID's for the strokes
        /// </summary>
        /// <param name="strokes">strokes</param>
        /// <returns></returns>
        internal static int[] GetStrokeIds(StrokeCollection strokes)
        {
            System.Diagnostics.Debug.Assert(strokes != null);

            int[] strokeIds = new int[strokes.Count];
            for (int x = 0; x < strokeIds.Length; x++)
            {
                //stroke ID's are 1 based (1,2,3...)
                strokeIds[x] = x + 1;
            }
            return strokeIds;
        }
    }
}

