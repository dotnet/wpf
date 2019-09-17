// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This class is used to compress a Path to BAML.
//
//  At compile-time this api is called into to "flatten" graphics calls to a BinaryWriter
//  At run-time this api is called into to rehydrate the flattened graphics calls
//        via invoking methods on a supplied StreamGeometryContext.
//
//  Via this compression - we reduce the time spent parsing at startup, we create smaller baml,
//  and we reduce creation of temporary strings.
//

using MS.Internal;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.IO;
using MS.Utility;

#if PBTCOMPILER

using MS.Internal.Markup;

namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MS.Internal.PresentationCore;

namespace MS.Internal.Media
#endif
{
     /// <summary>
     ///     ParserStreamGeometryContext
     /// </summary>
     internal class ParserStreamGeometryContext : StreamGeometryContext
     {
        enum ParserGeometryContextOpCodes : byte
        {
            BeginFigure = 0,
            LineTo = 1,
            QuadraticBezierTo = 2,
            BezierTo = 3,
            PolyLineTo = 4,
            PolyQuadraticBezierTo = 5,
            PolyBezierTo = 6,
            ArcTo = 7,
            Closed = 8,
            FillRule = 9,
        }

        private const byte HighNibble = 0xF0;
        private const byte LowNibble = 0x0F;

        private const byte SetBool1 = 0x10; // 00010000
        private const byte SetBool2 = 0x20; // 00100000
        private const byte SetBool3 = 0x40; // 01000000
        private const byte SetBool4 = 0x80; // 10000000

        #region Constructors

        /// <summary>
        /// This constructor exists to prevent external derivation
        /// </summary>
        internal ParserStreamGeometryContext(BinaryWriter bw)
        {
            _bw = bw;
        }

        #endregion Constructors


        #region internal Methods

#if PRESENTATION_CORE
        internal void SetFillRule(FillRule fillRule)
#else
        internal void SetFillRule(bool boolFillRule)
#endif
        {
#if PRESENTATION_CORE
            bool boolFillRule = FillRuleToBool(fillRule);
#endif

            byte packedByte = PackByte(ParserGeometryContextOpCodes.FillRule, boolFillRule, false);

            _bw.Write(packedByte);
        }

        /// <summary>
        /// BeginFigure - Start a new figure.
        /// </summary>
        /// <remarks>
        /// Stored as [PointAndTwoBools] (see SerializepointAndTwoBools method).
        /// </remarks>

        public override void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
        {
            //
            // We need to update the BeginFigure block of the last figure (if
            // there was one).
            //
            FinishFigure();

            _startPoint = startPoint;
            _isFilled = isFilled;
            _isClosed = isClosed;

            _figureStreamPosition = CurrentStreamPosition;

            //
            // This will be overwritten later when we start the next figure (i.e. when we're sure isClosed isn't
            // going to change). We write it out now to ensure that we reserve exactly the right amount of space.
            // Note that the number of bytes written is dependant on the particular value of startPoint, since
            // we can compress doubles when they are in fact integral.
            //
            SerializePointAndTwoBools(ParserGeometryContextOpCodes.BeginFigure, startPoint, isFilled, isClosed);
        }

        /// <summary>
        /// LineTo - append a LineTo to the current figure.
        /// </summary>
        /// <remarks>
        /// Stored as [PointAndTwoBools] (see SerializepointAndTwoBools method).
        /// </remarks>
        public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
        {
            SerializePointAndTwoBools(ParserGeometryContextOpCodes.LineTo, point, isStroked, isSmoothJoin);
        }

        /// <summary>
        /// QuadraticBezierTo - append a QuadraticBezierTo to the current figure.
        /// </summary>
        /// <remarks>
        /// Stored as [PointAndTwoBools] [Number] [Number]
        /// </remarks>
        public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
        {
            SerializePointAndTwoBools(ParserGeometryContextOpCodes.QuadraticBezierTo, point1, isStroked, isSmoothJoin);

            XamlSerializationHelper.WriteDouble(_bw, point2.X);
            XamlSerializationHelper.WriteDouble(_bw, point2.Y);
        }

        /// <summary>
        /// BezierTo - apply a BezierTo to the current figure.
        /// </summary>
        /// <remarks>
        /// Stored as [PointAndTwoBools] [Number] [Number] [Number] [Number]
        /// </remarks>
        public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
        {
            SerializePointAndTwoBools(ParserGeometryContextOpCodes.BezierTo, point1, isStroked, isSmoothJoin);

            XamlSerializationHelper.WriteDouble(_bw, point2.X);
            XamlSerializationHelper.WriteDouble(_bw, point2.Y);

            XamlSerializationHelper.WriteDouble(_bw, point3.X);
            XamlSerializationHelper.WriteDouble(_bw, point3.Y);
        }

        /// <summary>
        /// PolyLineTo - append a PolyLineTo to the current figure.
        /// </summary>
        /// <remarks>
        /// Stored as [ListOfPointAndTwoBools] (see SerializeListOfPointsAndTwoBools method).
        /// </remarks>
        public override void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            SerializeListOfPointsAndTwoBools(ParserGeometryContextOpCodes.PolyLineTo, points, isStroked, isSmoothJoin);
        }

        /// <summary>
        /// PolyQuadraticBezierTo - append a PolyQuadraticBezierTo to the current figure.
        /// </summary>
        /// <remarks>
        /// Stored as [ListOfPointAndTwoBools] (see SerializeListOfPointsAndTwoBools method).
        /// </remarks>
        public override void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            SerializeListOfPointsAndTwoBools(ParserGeometryContextOpCodes.PolyQuadraticBezierTo, points, isStroked, isSmoothJoin);
        }

        /// <summary>
        /// PolyBezierTo - append a PolyBezierTo to the current figure.
        /// </summary>
        /// <remarks>
        /// Stored as [ListOfPointAndTwoBools] (see SerializeListOfPointsAndTwoBools method).
        /// </remarks>
        public override void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            SerializeListOfPointsAndTwoBools(ParserGeometryContextOpCodes.PolyBezierTo, points, isStroked, isSmoothJoin);
        }

        /// <summary>
        /// ArcTo - append an ArcTo to the current figure.
        /// </summary>
        /// <remarks>
        /// Stored as [PointAndTwoBools] [Packed byte for isLargeArc and sweepDirection] [Pair of Numbers for Size] [Pair of Numbers for rotation Angle]
        ///
        ///     Also note that we've special cased this method signature to avoid moving the enum for SweepDirection into PBT (will require codegen changes).
        /// </remarks>
#if PBTCOMPILER
        public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked, bool isSmoothJoin)
#else
        public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
#endif
        {
            SerializePointAndTwoBools(ParserGeometryContextOpCodes.ArcTo, point, isStroked, isSmoothJoin);

            //
            // Pack isLargeArc & sweepDirection into a single byte.
            //
            byte packMe = 0;
            if (isLargeArc)
            {
                packMe = LowNibble;
            }

#if PBTCOMPILER
            if (sweepDirection)
#else
            if (SweepToBool(sweepDirection))
#endif
            {
                packMe |= HighNibble;
            }

            _bw.Write(packMe);

            //
            // Write out Size & Rotation Angle.
            //
            XamlSerializationHelper.WriteDouble(_bw, size.Width);
            XamlSerializationHelper.WriteDouble(_bw, size.Height);
            XamlSerializationHelper.WriteDouble(_bw, rotationAngle);
        }

        internal bool FigurePending
        {
            get
            {
                return (_figureStreamPosition > -1);
            }
        }

        internal int CurrentStreamPosition
        {
            get
            {
                return checked((int)_bw.Seek(0, SeekOrigin.Current));
            }
        }

        internal void FinishFigure()
        {
            if (FigurePending)
            {
                int currentOffset = CurrentStreamPosition;

                //
                // Go back and overwrite our existing begin figure block. See comment in BeginFigure.
                //
                _bw.Seek(_figureStreamPosition, SeekOrigin.Begin);
                SerializePointAndTwoBools(ParserGeometryContextOpCodes.BeginFigure, _startPoint, _isFilled, _isClosed);

                _bw.Seek(currentOffset, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// This is the same as the Close call:
        /// Closes the Context and flushes the content.
        /// Afterwards the Context can not be used anymore.
        /// This call does not require all Push calls to have been Popped.
        /// </summary>
        internal override void DisposeCore()
        {
        }

        /// <summary>
        /// SetClosedState - Sets the current closed state of the figure.
        /// </summary>
        internal override void SetClosedState(bool closed)
        {
            _isClosed = closed;
        }

        /// <summary>
        ///     Mark that the stream is Done.
        /// </summary>
        internal void MarkEOF()
        {
            //
            // We need to update the BeginFigure block of the last figure (if
            // there was one).
            //
            FinishFigure();
            _bw.Write((byte) ParserGeometryContextOpCodes.Closed);
        }

#if PRESENTATION_CORE
        internal static void Deserialize(BinaryReader br, StreamGeometryContext sc, StreamGeometry geometry)
        {
            bool closed = false;
            Byte currentByte;

            while (!closed)
            {
                currentByte = br.ReadByte();

                ParserGeometryContextOpCodes opCode = UnPackOpCode(currentByte);

                switch(opCode)
                {
                    case ParserGeometryContextOpCodes.FillRule :
                        DeserializeFillRule(br, currentByte, geometry);
                        break;

                    case ParserGeometryContextOpCodes.BeginFigure :
                        DeserializeBeginFigure(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.LineTo :
                        DeserializeLineTo(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.QuadraticBezierTo :
                        DeserializeQuadraticBezierTo(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.BezierTo :
                        DeserializeBezierTo(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.PolyLineTo :
                        DeserializePolyLineTo(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.PolyQuadraticBezierTo :
                        DeserializePolyQuadraticBezierTo(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.PolyBezierTo :
                        DeserializePolyBezierTo(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.ArcTo :
                        DeserializeArcTo(br, currentByte, sc);
                        break;

                    case ParserGeometryContextOpCodes.Closed :
                        closed = true;
                        break;
                }
            }
        }
#endif
        #endregion internal Methods

        #region private Methods

        //
        // Deserialization Methods.
        //
        // These are only required at "runtime" - therefore only in PRESENTATION_CORE
        //

#if PRESENTATION_CORE

        private static void DeserializeFillRule(BinaryReader br, Byte firstByte, StreamGeometry geometry)
        {
            bool boolFillRule;
            bool unused;
            FillRule fillRule;

            UnPackBools(firstByte, out boolFillRule, out unused);

            fillRule = BoolToFillRule(boolFillRule);

            geometry.FillRule = fillRule;
}

        private static void DeserializeBeginFigure(BinaryReader br, Byte firstByte, StreamGeometryContext sc)
        {
            Point point;
            bool isFilled;
            bool isClosed;

            DeserializePointAndTwoBools(br, firstByte, out point, out isFilled, out isClosed);

            sc.BeginFigure(point, isFilled, isClosed);
        }

        private static void DeserializeLineTo(BinaryReader br, Byte firstByte, StreamGeometryContext sc)
        {
            Point point;
            bool isStroked;
            bool isSmoothJoin;

            DeserializePointAndTwoBools(br, firstByte, out point, out isStroked, out isSmoothJoin);

            sc.LineTo(point, isStroked, isSmoothJoin);
        }

        private static void DeserializeQuadraticBezierTo(BinaryReader br, byte firstByte, StreamGeometryContext sc)
        {
            Point point1;
            Point point2 = new Point();
            bool isStroked;
            bool isSmoothJoin;

            DeserializePointAndTwoBools(br, firstByte, out point1, out isStroked, out isSmoothJoin);

            point2.X = XamlSerializationHelper.ReadDouble(br);
            point2.Y = XamlSerializationHelper.ReadDouble(br);

            sc.QuadraticBezierTo(point1, point2, isStroked, isSmoothJoin);
        }

        private static void DeserializeBezierTo(BinaryReader br, byte firstByte, StreamGeometryContext sc)
        {
            Point point1;
            Point point2 = new Point();
            Point point3 = new Point();

            bool isStroked;
            bool isSmoothJoin;

            DeserializePointAndTwoBools(br, firstByte, out point1, out isStroked, out isSmoothJoin);

            point2.X = XamlSerializationHelper.ReadDouble(br);
            point2.Y = XamlSerializationHelper.ReadDouble(br);

            point3.X = XamlSerializationHelper.ReadDouble(br);
            point3.Y = XamlSerializationHelper.ReadDouble(br);

            sc.BezierTo(point1, point2, point3, isStroked, isSmoothJoin);
        }

        private static void DeserializePolyLineTo(BinaryReader br, Byte firstByte, StreamGeometryContext sc)
        {
            bool isStroked;
            bool isSmoothJoin;
            IList<Point> points;

            points = DeserializeListOfPointsAndTwoBools(br, firstByte, out isStroked, out isSmoothJoin);

            sc.PolyLineTo(points, isStroked, isSmoothJoin);
        }

        private static void DeserializePolyQuadraticBezierTo(BinaryReader br, Byte firstByte, StreamGeometryContext sc)
        {
            bool isStroked;
            bool isSmoothJoin;
            IList<Point> points;

            points = DeserializeListOfPointsAndTwoBools(br, firstByte, out isStroked, out isSmoothJoin);

            sc.PolyQuadraticBezierTo(points, isStroked, isSmoothJoin);
        }

        private static void DeserializePolyBezierTo(BinaryReader br, Byte firstByte, StreamGeometryContext sc)
        {
            bool isStroked;
            bool isSmoothJoin;
            IList<Point> points;

            points = DeserializeListOfPointsAndTwoBools(br, firstByte, out isStroked, out isSmoothJoin);

            sc.PolyBezierTo(points, isStroked, isSmoothJoin);
        }


        private static void DeserializeArcTo(BinaryReader br, byte firstByte, StreamGeometryContext sc)
        {
            Point point;
            Size size = new Size();
            double rotationAngle;
            bool isStroked;
            bool isSmoothJoin;
            bool isLargeArc;
            SweepDirection sweepDirection;

            DeserializePointAndTwoBools(br, firstByte, out point, out isStroked, out isSmoothJoin);

            // Read the packed byte for isLargeArd & sweepDirection.

            //
            // Pack isLargeArc & sweepDirection into a signle byte.
            //
            byte packedByte = br.ReadByte();

            isLargeArc = ((packedByte & LowNibble) != 0);

            sweepDirection = BoolToSweep(((packedByte & HighNibble) != 0));


            size.Width = XamlSerializationHelper.ReadDouble(br);
            size.Height = XamlSerializationHelper.ReadDouble(br);
            rotationAngle = XamlSerializationHelper.ReadDouble(br);

            sc.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked, isSmoothJoin);
        }

        //
        //  Private Deserialization helpers.
        //

        private static void UnPackBools(byte packedByte, out bool bool1, out bool bool2)
        {
            bool1 = (packedByte & SetBool1) != 0;
            bool2 = (packedByte & SetBool2) != 0;
        }

        private static void UnPackBools(byte packedByte, out bool bool1, out bool bool2, out bool bool3, out bool bool4)
        {
            bool1 = (packedByte & SetBool1) != 0;
            bool2 = (packedByte & SetBool2) != 0;
            bool3 = (packedByte & SetBool3) != 0;
            bool4 = (packedByte & SetBool4) != 0;
        }

        private static ParserGeometryContextOpCodes UnPackOpCode(byte packedByte)
        {
            return ((ParserGeometryContextOpCodes) (packedByte & 0x0F));
        }

        private static IList<Point> DeserializeListOfPointsAndTwoBools(BinaryReader br, Byte firstByte, out bool bool1, out bool bool2)
        {
            int count;
            IList<Point> points;
            Point point;

            // Pack the two bools into one byte
            UnPackBools(firstByte, out bool1, out bool2);

            count = br.ReadInt32();

            points = new List<Point>(count);

            for(int i = 0; i < count; i++)
            {
                point = new Point(XamlSerializationHelper.ReadDouble(br),
                                  XamlSerializationHelper.ReadDouble(br));

                points.Add(point);
            }

            return points;
        }


        private static void DeserializePointAndTwoBools(BinaryReader br, Byte firstByte, out Point point, out bool bool1, out bool bool2)
        {
            bool isScaledIntegerX = false;
            bool isScaledIntegerY = false;

            UnPackBools(firstByte, out bool1, out bool2, out isScaledIntegerX, out isScaledIntegerY);

            point = new Point(DeserializeDouble(br, isScaledIntegerX),
                              DeserializeDouble(br, isScaledIntegerY));
        }

        private static Double DeserializeDouble(BinaryReader br, bool isScaledInt)
        {
            if (isScaledInt)
            {
                return XamlSerializationHelper.ReadScaledInteger(br);
            }
            else
            {
                return XamlSerializationHelper.ReadDouble(br);
            }
        }

        //
        // Private serialization helpers
        //

        private static SweepDirection BoolToSweep(bool value)
        {
            if(!value)
                return SweepDirection.Counterclockwise;
            else
                return SweepDirection.Clockwise;
        }

        private static bool SweepToBool(SweepDirection sweep)
        {
            if (sweep == SweepDirection.Counterclockwise)
                return false;
            else
                return true;
        }

        private static FillRule BoolToFillRule(bool value)
        {
            if(!value)
                return FillRule.EvenOdd;
            else
                return FillRule.Nonzero;
        }

        private static bool FillRuleToBool(FillRule fill)
        {
            if (fill == FillRule.EvenOdd)
                return false;
            else
                return true;
        }

#endif

        //
        // SerializePointAndTwoBools
        //
        // Binary format is :
        //
        //  <Byte+OpCode> <Number1> <Number2>
        //
        //      Where :
        //          <Byte+OpCode> := OpCode + bool1 + bool2 + isScaledIntegerX + isScaledIntegerY
        //          <NumberN> := <ScaledInteger> | <SerializationFloatTypeForSpecialNumbers> | <SerializationFloatType.Double+Double>
        //          <SerializationFloatTypeForSpecialNumbers> := <SerializationFloatType.Zero> | <SerializationFloatType.One> | <SerializationFloatType.MinusOne>
        //          <SerializationFloatType.Double+Double> := <SerializationFloatType.Double> <Double>
        //
        // By packing the flags for isScaledInteger into the first byte - we save 2 extra bytes per number for the common case.
        //
        // As a result - most LineTo's (and other operations) will be stored in 9 bytes.
        //               Some LineTo's will be 6 (or even sometimes 3)
        //               Max LineTo will be 19 (two doubles).
        private void SerializePointAndTwoBools(ParserGeometryContextOpCodes opCode,
                                                       Point point,
                                                       bool bool1,
                                                       bool bool2)
        {
            int intValueX = 0;
            int intValueY = 0;
            bool isScaledIntegerX, isScaledIntegerY;

            isScaledIntegerX = XamlSerializationHelper.CanConvertToInteger(point.X, ref intValueX);
            isScaledIntegerY = XamlSerializationHelper.CanConvertToInteger(point.Y, ref intValueY);

            _bw.Write(PackByte(opCode, bool1, bool2, isScaledIntegerX, isScaledIntegerY));

            SerializeDouble(point.X, isScaledIntegerX, intValueX);
            SerializeDouble(point.Y, isScaledIntegerY, intValueY);
        }

        // SerializeListOfPointsAndTwoBools
        //
        // Binary format is :
        //
        //  <Byte+OpCode> <Count> <Number1> ... <NumberN>
        //
        //      <Byte+OpCode> := OpCode + bool1 + bool2
        //      <Count> := int32
        //      <NumberN> := <SerializationFloatType.ScaledInteger+Integer> | <SerializationFloatTypeForSpecialNumbers> | <SerializationFloatType.Double+Double>
        private void SerializeListOfPointsAndTwoBools(ParserGeometryContextOpCodes opCode, IList<Point> points, bool bool1, bool bool2)
        {
            // Pack the two bools into one byte
            Byte packedByte = PackByte(opCode, bool1, bool2);
            _bw.Write(packedByte);

            // Write the count.
            _bw.Write(points.Count);

            // Write out all the Points
            for(int i = 0; i < points.Count; i++)
            {
                XamlSerializationHelper.WriteDouble(_bw, points[i].X);
                XamlSerializationHelper.WriteDouble(_bw, points[i].Y);
            }
        }

        private void SerializeDouble(double value, bool isScaledInt, int scaledIntValue)
        {
            if (isScaledInt)
            {
                _bw.Write(scaledIntValue);
            }
            else
            {
                XamlSerializationHelper.WriteDouble(_bw, value);
            }
        }

        private static byte PackByte(ParserGeometryContextOpCodes opCode, bool bool1, bool bool2)
        {
            return PackByte(opCode, bool1, bool2, false, false);
        }


        // PackByte
        //      Packs an op-code, and up to 4 booleans into a single byte.
        //
        // Binary format is :
        //      First 4 bits map directly to the op-code.
        //      Next 4 bits map to booleans 1 - 4.
        //
        //          Like this:
        //
        //              7| 6  | 5  | 4  | 3 | 2 | 1 | 0 |
        //           <B4>|<B3>|<B2>|<B1><-  Op Code    ->
        //
        private static byte PackByte(ParserGeometryContextOpCodes opCode, bool bool1, bool bool2, bool bool3, bool bool4)
        {
            byte packedByte = (byte) opCode;

            if (packedByte >= 16)
            {
                throw new ArgumentException(SR.Get(SRID.UnknownPathOperationType));
            }

            if (bool1)
            {
                packedByte |= SetBool1;
            }

            if (bool2)
            {
                packedByte |= SetBool2;
            }

            if (bool3)
            {
                packedByte |= SetBool3;
            }

            if (bool4)
            {
                packedByte |= SetBool4;
            }

            return packedByte;
        }

        #endregion private Methods

        private BinaryWriter _bw;

        Point _startPoint;
        bool _isClosed;
        bool _isFilled;

        int _figureStreamPosition = -1;
    }
}

