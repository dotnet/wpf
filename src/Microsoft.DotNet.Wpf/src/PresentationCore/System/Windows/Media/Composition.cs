// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using MS.Internal.PresentationCore;
using MS.Win32;
using System.Diagnostics;
using System.Windows.Media.Composition;
using System.Security;

using DllImport=MS.Internal.PresentationCore.DllImport;

namespace System.Windows.Media.Composition
{
    internal static class CompositionResourceManager
    {
        public const int InvalidResourceHandle = 0;

        internal static MilColorF ColorToMilColorF(Color c)
        {
            MilColorF color;
            color.r = c.ScR;
            color.g = c.ScG;
            color.b = c.ScB;
            color.a = c.ScA;
            return color;
        }

        internal static D3DMATRIX Matrix3DToD3DMATRIX(Matrix3D m)
        {
            D3DMATRIX matrix;
            matrix._11 = (float) m.M11;
            matrix._12 = (float) m.M12;
            matrix._13 = (float) m.M13;
            matrix._14 = (float) m.M14;
            matrix._21 = (float) m.M21;
            matrix._22 = (float) m.M22;
            matrix._23 = (float) m.M23;
            matrix._24 = (float) m.M24;
            matrix._31 = (float) m.M31;
            matrix._32 = (float) m.M32;
            matrix._33 = (float) m.M33;
            matrix._34 = (float) m.M34;
            matrix._41 = (float) m.OffsetX;
            matrix._42 = (float) m.OffsetY;
            matrix._43 = (float) m.OffsetZ;
            matrix._44 = (float) m.M44;
            return matrix;
        }

        internal static MilPoint3F Point3DToMilPoint3F(Point3D p)
        {
            MilPoint3F point;
            point.X = (float) p.X;
            point.Y = (float) p.Y;
            point.Z = (float) p.Z;
            return point;
        }

        internal static MilPoint3F Vector3DToMilPoint3F(Vector3D v)
        {
            MilPoint3F point;
            point.X = (float) v.X;
            point.Y = (float) v.Y;
            point.Z = (float) v.Z;
            return point;
        }

        internal static MilQuaternionF QuaternionToMilQuaternionF(Quaternion q)
        {
            MilQuaternionF quat;
            quat.X = (float) q.X;
            quat.Y = (float) q.Y;
            quat.Z = (float) q.Z;
            quat.W = (float) q.W;
            return quat;
        }

        internal static MilMatrix4x4D MatrixToMilMatrix4x4D(Matrix m)
        {
            MilMatrix4x4D matrix;

            if (m.IsIdentity)
            {
                matrix.M_11 = 1.0;
                matrix.M_12 = 0.0;
                matrix.M_13 = 0.0;
                matrix.M_14 = 0.0;
                matrix.M_21 = 0.0;
                matrix.M_22 = 1.0;
                matrix.M_23 = 0.0;
                matrix.M_24 = 0.0;
                matrix.M_31 = 0.0;
                matrix.M_32 = 0.0;
                matrix.M_33 = 1.0;
                matrix.M_34 = 0.0;
                matrix.M_41 = 0.0;
                matrix.M_42 = 0.0;
                matrix.M_43 = 0.0;
                matrix.M_44 = 1.0;
            }
            else
            {
                matrix.M_11 = m.M11;
                matrix.M_12 = m.M12;
                matrix.M_13 = 0.0;
                matrix.M_14 = 0.0;
                matrix.M_21 = m.M21;
                matrix.M_22 = m.M22;
                matrix.M_23 = 0.0;
                matrix.M_24 = 0.0;
                matrix.M_31 = 0.0;
                matrix.M_32 = 0.0;
                matrix.M_33 = 1.0;
                matrix.M_34 = 0.0;
                matrix.M_41 = m.OffsetX;
                matrix.M_42 = m.OffsetY;
                matrix.M_43 = 0.0;
                matrix.M_44 = 1.0;
            }
            return matrix;
        }

        internal static MilMatrix3x2D TransformToMilMatrix3x2D(Transform t)
        {
            MilMatrix3x2D matrix;

            if (t == null || t.IsIdentity)
            {
                matrix.S_11 = 1.0;
                matrix.S_12 = 0.0;
                matrix.S_21 = 0.0;
                matrix.S_22 = 1.0;
                matrix.DX = 0.0;
                matrix.DY = 0.0;
            }
            else
            {
                Matrix m = t.Value;

                matrix.S_11 = m.M11;
                matrix.S_12 = m.M12;
                matrix.S_21 = m.M21;
                matrix.S_22 = m.M22;
                matrix.DX = m.OffsetX;
                matrix.DY = m.OffsetY;
            }

            return matrix;
        }

        internal static MilMatrix3x2D MatrixToMilMatrix3x2D(Matrix m)
        {
            return MatrixToMilMatrix3x2D(ref m);
        }

        internal static MilMatrix3x2D MatrixToMilMatrix3x2D(ref Matrix m)
        {
            MilMatrix3x2D matrix;

            if (m.IsIdentity)
            {
                matrix.S_11 = 1.0;
                matrix.S_12 = 0.0;
                matrix.S_21 = 0.0;
                matrix.S_22 = 1.0;
                matrix.DX = 0.0;
                matrix.DY = 0.0;
            }
            else
            {
                matrix.S_11 = m.M11;
                matrix.S_12 = m.M12;
                matrix.S_21 = m.M21;
                matrix.S_22 = m.M22;
                matrix.DX = m.OffsetX;
                matrix.DY = m.OffsetY;
            }

            return matrix;
        }

        internal static Matrix MilMatrix3x2DToMatrix(ref MilMatrix3x2D m)
        {
            return new Matrix(m.S_11, m.S_12, m.S_21, m.S_22, m.DX, m.DY);
        }

        internal static UInt32 BooleanToUInt32(Boolean v)
        {
            return (UInt32)(v ? -1 : 0);
        }
    }

    // This is a copy of what is in milcore.h
    // That file needs to be kept in sync with this one!

    internal static partial class MilCoreApi
    {
        [DllImport(DllImport.MilCore)]
        internal static extern int MilComposition_SyncFlush(
            IntPtr pChannel
            );

        [DllImport(DllImport.MilCore)]
        internal unsafe static extern int MilUtility_GetPointAtLengthFraction(
            MilMatrix3x2D *pMatrix,
            FillRule fillRule,
            byte *pPathData,
            UInt32 nSize,
            double rFraction,
            out Point pt,
            out Point vecTangent);

        [DllImport(DllImport.MilCore)]
        internal unsafe static extern int MilUtility_PolygonBounds(
            MilMatrix3x2D *pWorldMatrix,
            MIL_PEN_DATA *pPenData,
            double *pDashArray,
            Point *pPoints,
            byte *pTypes,
            UInt32 pointCount,
            UInt32 segmentCount,
            MilMatrix3x2D *pGeometryMatrix,
            double rTolerance,
            bool fRelative,
            bool fSkipHollows,
            Rect *pBounds);

        [DllImport(DllImport.MilCore)]
        internal unsafe static extern int MilUtility_PolygonHitTest(
            MilMatrix3x2D *pGeometryMatrix,
            MIL_PEN_DATA *pPenData,
            double *pDashArray,
            Point* pPoints,
            byte *pTypes,
            UInt32 cPoints,
            UInt32 cSegments,
            double rTolerance,
            bool fRelative,
            Point* pHitPoint,
            out bool pDoesContain);

        [DllImport(DllImport.MilCore)]
        internal unsafe static extern int MilUtility_PathGeometryHitTest(
            MilMatrix3x2D *pMatrix,
            MIL_PEN_DATA* pPenData,
            double* pDashArray,
            FillRule fillRule,
            byte *pPathData,
            UInt32 nSize,
            double rTolerance,
            bool fRelative,
            Point* pHitPoint,
            out bool pDoesContain);

        [DllImport(DllImport.MilCore)]
        internal unsafe static extern int MilUtility_PathGeometryHitTestPathGeometry(
            MilMatrix3x2D *pMatrix1,
            FillRule fillRule1,
            byte *pPathData1,
            UInt32 nSize1,
            MilMatrix3x2D *pMatrix2,
            FillRule fillRule2,
            byte *pPathData2,
            UInt32 nSize2,
            double rTolerance,
            bool fRelative,
            IntersectionDetail* pDetail);

        [DllImport(DllImport.MilCore)]
        internal unsafe static extern int MilUtility_GeometryGetArea(
            FillRule fillRule,
            byte *pPathData,
            UInt32 nSize,
            MilMatrix3x2D *pMatrix,
            double rTolerance,
            bool fRelative,
            double* pArea);

        [DllImport(DllImport.MilCore)]
        internal unsafe static extern void MilUtility_ArcToBezier(
            Point ptStart,              // The arc's start point
            Size rRadii,                // The ellipse's X and Y radii
            double rRotation,           // Rotation angle of the ellipse's x axis
            bool fLargeArc,             // Choose the larger of the 2 arcs if TRUE
            SweepDirection fSweepUp,    // Sweep the arc increasing the angle if TRUE
            Point ptEnd,                // The arc's end point
            MilMatrix3x2D* pMatrix,    // Transformation matrix
            Point* pPt,                 // An array receiving the Bezier points
            out int cPieces);           // The number of output Bezier curves
    }
}
