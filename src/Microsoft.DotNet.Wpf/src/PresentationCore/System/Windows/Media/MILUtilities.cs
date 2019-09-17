// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.Security;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using MS.Internal;
using System.Runtime.InteropServices;
using MS.Internal.PresentationCore;

namespace System.Windows.Media
{
    internal static class MILUtilities
    {
        internal static readonly D3DMATRIX D3DMATRIXIdentity =
                                new D3DMATRIX(1, 0, 0, 0,
                                              0, 1, 0, 0,
                                              0, 0, 1, 0,
                                              0, 0, 0, 1);

        /// <summary>
        /// Converts a System.Windows.Media.Matrix to a D3DMATRIX.
        /// </summary>
        /// <param name="matrix"> Input Matrix to convert </param>
        /// <param name="d3dMatrix"> Output convered D3DMATRIX </param>        
        internal static unsafe void ConvertToD3DMATRIX(
            /* in */ Matrix* matrix,
            /* out */ D3DMATRIX* d3dMatrix
            )
        {
            *d3dMatrix = D3DMATRIXIdentity;

            float* pD3DMatrix = (float*)d3dMatrix;
            double* pMatrix = (double*)matrix;

            // m11 = m11
            pD3DMatrix[0] = (float)pMatrix[0];

            // m12 = m12
            pD3DMatrix[1] = (float)pMatrix[1];

            // m21 = m21
            pD3DMatrix[4] = (float)pMatrix[2];

            // m22 = m22
            pD3DMatrix[5] = (float)pMatrix[3];

            // m41 = offsetX
            pD3DMatrix[12] = (float)pMatrix[4];

            // m42 = offsetY
            pD3DMatrix[13] = (float)pMatrix[5];
        }

        /// <summary>
        /// Converts a D3DMATRIX to a System.Windows.Media.Matrix.
        /// </summary>
        /// <param name="d3dMatrix"> Input D3DMATRIX to convert </param>
        /// <param name="matrix"> Output converted Matrix </param>
        internal static unsafe void ConvertFromD3DMATRIX(
            /* in */ D3DMATRIX* d3dMatrix,
            /* out */ Matrix* matrix
            )
        {
            float* pD3DMatrix = (float*)d3dMatrix;
            double* pMatrix = (double*)matrix;

            //
            // Convert first D3DMatrix Vector
            //

            pMatrix[0] = (double) pD3DMatrix[0]; // m11 = m11
            pMatrix[1] = (double) pD3DMatrix[1]; // m12 = m12

            // Assert that non-affine fields are identity or NaN
            //
            // Multiplication with an affine 2D matrix (i.e., a matrix
            // with only _11, _12, _21, _22, _41, & _42 set to non-identity
            // values) containing NaN's, can cause the NaN's to propagate to
            // all other fields.  Thus, we allow NaN's in addition to 
            // identity values.
            Debug.Assert(pD3DMatrix[2] == 0.0f || Single.IsNaN(pD3DMatrix[2]));
            Debug.Assert(pD3DMatrix[3] == 0.0f || Single.IsNaN(pD3DMatrix[3]));

            //
            // Convert second D3DMatrix Vector
            //

            pMatrix[2] = (double) pD3DMatrix[4]; // m21 = m21
            pMatrix[3] = (double) pD3DMatrix[5]; // m22 = m22
            Debug.Assert(pD3DMatrix[6] == 0.0f || Single.IsNaN(pD3DMatrix[6]));
            Debug.Assert(pD3DMatrix[7] == 0.0f || Single.IsNaN(pD3DMatrix[7]));

            //
            // Convert third D3DMatrix Vector
            //

            Debug.Assert(pD3DMatrix[8] == 0.0f || Single.IsNaN(pD3DMatrix[8]));
            Debug.Assert(pD3DMatrix[9] == 0.0f || Single.IsNaN(pD3DMatrix[9]));
            Debug.Assert(pD3DMatrix[10] == 1.0f || Single.IsNaN(pD3DMatrix[10]));
            Debug.Assert(pD3DMatrix[11] == 0.0f || Single.IsNaN(pD3DMatrix[11]));

            //
            // Convert fourth D3DMatrix Vector
            //

            pMatrix[4] = (double) pD3DMatrix[12]; // m41 = offsetX
            pMatrix[5] = (double) pD3DMatrix[13]; // m42 = offsetY
            Debug.Assert(pD3DMatrix[14] == 0.0f || Single.IsNaN(pD3DMatrix[14]));
            Debug.Assert(pD3DMatrix[15] == 1.0f || Single.IsNaN(pD3DMatrix[15]));

            *((MatrixTypes*)(pMatrix+6)) = MatrixTypes.TRANSFORM_IS_UNKNOWN;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MILRect3D
        {
            public MILRect3D(ref Rect3D rect)
            {
                X = (float)rect.X;
                Y = (float)rect.Y;
                Z = (float)rect.Z;
                LengthX = (float)rect.SizeX;
                LengthY = (float)rect.SizeY;
                LengthZ = (float)rect.SizeZ;
            }
            
            public float X; 
            public float Y; 
            public float Z;
            public float LengthX; 
            public float LengthY; 
            public float LengthZ;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MilRectF
        {
            public float Left;
            public float Top;
            public float Right;
            public float Bottom;
        };

        [DllImport(DllImport.MilCore)]
        private extern static /*HRESULT*/ int MIL3DCalcProjected2DBounds(
            ref D3DMATRIX pFullTransform3D,
            ref MILRect3D pboxBounds,
            out MilRectF prcDestRect); 

        [DllImport(DllImport.MilCore, EntryPoint = "MilUtility_CopyPixelBuffer", PreserveSig = false)]
        internal extern static unsafe void MILCopyPixelBuffer(
            byte *  pOutputBuffer,
            uint    outputBufferSize,
            uint    outputBufferStride,
            uint    outputBufferOffsetInBits,
            byte *  pInputBuffer,
            uint    inputBufferSize,
            uint    inputBufferStride,
            uint    inputBufferOffsetInBits,
            uint    height,
            uint    copyWidthInBits
            );

        internal static Rect ProjectBounds(
            ref Matrix3D viewProjMatrix, 
            ref Rect3D originalBox)
        {
            D3DMATRIX viewProjFloatMatrix = CompositionResourceManager.Matrix3DToD3DMATRIX(viewProjMatrix);
            MILRect3D originalBoxFloat = new MILRect3D(ref originalBox);
            MilRectF outRect = new MilRectF();

            HRESULT.Check(
                MIL3DCalcProjected2DBounds(
                    ref viewProjFloatMatrix, 
                    ref originalBoxFloat, 
                    out outRect));

            if (outRect.Left == outRect.Right || 
                outRect.Top == outRect.Bottom)
            {
                return Rect.Empty;
            }
            else
            {
                return new Rect(
                    outRect.Left, 
                    outRect.Top, 
                    outRect.Right - outRect.Left, 
                    outRect.Bottom - outRect.Top
                    );
            }
        }
    }
}

