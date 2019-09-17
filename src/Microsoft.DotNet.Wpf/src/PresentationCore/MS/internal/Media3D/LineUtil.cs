// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// GLOSSARY
//     Kernel of matrix M : Space of vectors that vanish (go to 0) when multiplied by M
// Null-space of matrix M : Same thing
//          Line matrix : Representation of a line using a matrix whose kernel is points on the line
//                      L : Matrix representing a line
//                     LT : Transpose of L
//
// This file offers a small line transform utility function.  Given a line (lin) defined by Point3D
// origin & Vector3D direction and a model matrix M it returns (in-place) a line (lout) in "model
// space" so that any point on the line when transformed by M is on the original line.
//
// In other words   x in lout ===>  x M in lin
//
// This works even if M is rank-deficient, but if M is rank 2 or less then lout is not uniquely
// determined.
//
// The basic technique is as follows, we represent lin as a matrix where points on the line are in
// the null-space (kernel) of lin (this is straightforward.)  Letting the symbols lin & lout refer
// to both the line & the corresponding matrix this means:
//
//        x in lin ===> x lin = [0,0,0,0]
//
// Then we can transform a line matrix into model space by left multiplication with M
//
//        lout = M lin
//
// That way,
// 
//        x in lout ===> x lout = [0,0,0,0] ===> x M lin = [0,0,0,0] ===> x M in lin
//
// Which is what we want.  The hard part is going back from the matrix lout to a point and a vector.
// The smallest two eigenvalues of the l matrix are zero.  The corresponding eigenvectors are two
// different points on the line.
//
// I find the eigenvectors and eigenvalues of a line matrix L by first computing the normal matrix
// N = L LT which has symmetric eigenvectors equal to the left eigenvectors of L.  I find the
// eigenvectors of N using a Jacobi method for symmetric eigenvalue problems.
//
// The method is described in Chapter 8.4 of Golub & Van Loan (Matrix Computation), but here's a
// brief summary.
//
// We apply a series of 2D rotations (A1...An) to the matrix N that each make the matrix more
// diagonal.  So if the whole sequence is A = A1 ... An then the final matrix E = AT N A is
// diagonal.
//
// Because A is orthonormal its columns are the eigenvectors of N and the diagonal elements of E are
// the eigenvalues.
//
// NOTES
//
// Forming the normal matrix N involves fourth powers of the input values.  I mitigate this by
// scaling the matrix so that its largest value is 1 before squaring it.  There may be a better
// method (perhaps even a modified Jacobi method) that would work directly on L and perhaps be more
// stable.
//
// None of this code understands rays.  This is all in terms of lines, lines, lines, lines!

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Media3D;

using MS.Utility;

namespace MS.Internal.Media3D
{
    [Flags]
    internal enum FaceType
    {
        None     = 0,
        Front    = 1 << 0,
        Back     = 1 << 1,      
    };

    internal static class LineUtil
    {
        // Coordinates of elements above the diagonal.
        readonly static int[,] s_pairs = new int[,]{ {0,1}, {0,2}, {0,3}, {1,2}, {1,3}, {2,3} };
        const int s_pairsCount = 6;

        public static void Transform(Matrix3D modelMatrix,
                                     ref Point3D origin, ref Vector3D direction, out bool isRay)
        {
            if (modelMatrix.InvertCore())
            {
                Point4D o = new Point4D(origin.X,origin.Y,origin.Z,1);
                Point4D d = new Point4D(direction.X,direction.Y,direction.Z,0);
                
                modelMatrix.MultiplyPoint(ref o);
                modelMatrix.MultiplyPoint(ref d);

                if (o.W == 1 && d.W == 0)
                {
                    // Affine transformation
                    
                    origin = new Point3D(o.X, o.Y, o.Z);
                    direction = new Vector3D(d.X, d.Y, d.Z);

                    isRay = true;
                }
                else
                {
                    // Non-affine transformation (likely projection)
                    
                    // Form 4x2 matrix with two points on line in two columns.
                    double[,] linepoints = new double[,]{{o.X,d.X},{o.Y,d.Y},{o.Z,d.Z},{o.W,d.W}};
                    
                    ColumnsToAffinePointVector(linepoints,0,1,out origin, out direction);

                    isRay = false;
                }
            }
            else
            {
                TransformSingular(ref modelMatrix, ref origin, ref direction);

                isRay = false;
            }
        }

        // modelMatrix is passed by reference for efficiency only.  It is not modified.
        private static void TransformSingular(ref Matrix3D modelMatrix,
                                              ref Point3D origin, ref Vector3D direction)
        {
            double [,] matrix = TransformedLineMatrix(ref modelMatrix, ref origin, ref direction);
            matrix = Square(matrix);
                
            double[,] eigen = new double[,]{ {1,0,0,0}, {0,1,0,0}, {0,0,1,0}, {0,0,0,1} };

            // We'll just do 5 iterations with each pair because according to my results & Golub &
            // Van Loan this process converges quickly.
            int iterations = 5 * s_pairsCount;
            for (int iter = 0; iter < iterations; ++iter)
            {
                int pair = iter % s_pairsCount;
                JacobiRotation jrot = new JacobiRotation(s_pairs[pair,0],s_pairs[pair,1],matrix);
                matrix = jrot.LeftRightMultiply(matrix);
                eigen = jrot.RightMultiply(eigen);
            }

            // That was it as far as finding eigenvectors

            int evec1,evec2;
            FindSmallestTwoDiagonal(matrix, out evec1, out evec2);

            // The eigenvectors corresponding to the two smallest eigenvalues are columns evec1 &
            // evec2.  These, in homogeneous space, are two different points on our line.  We are
            // going to convert them to an affine point & vector.
            ColumnsToAffinePointVector(eigen, evec1, evec2, out origin, out direction);
        }

        private static void ColumnsToAffinePointVector(double[,] matrix, int col1, int col2, out Point3D origin, out Vector3D direction)
        {
            // The col1 & col2 columsn of matrix are two different homogeneous points on a line.  We
            // are going to convert them to an affine point & vector using the following procedure.
            //   1. Pick the eigenvector with the largest W, call it big
            //   2. Scale it by 1/W to form an affine point, still call it big
            //   3. Add a weighted multiple of big to small to make small.W zero.
            //
            // If both w are zero then we have a projective line that intersects no affine points
            // (i.e. it's a line at infinity.)  This will cause overflow but that's fine because the
            // line at infinity doesn't intersect anything & the caller of this function needs be
            // able to handle results that come back with inf or nan by "doing nothing."

            // Step 1.
            if (matrix[3,col1]*matrix[3,col1] < matrix[3,col2]*matrix[3,col2])
            {
                int temp = col1;
                col1 = col2;
                col2 = temp;
            }

            // Step 2.
            double s = 1/matrix[3,col1];
            origin = new Point3D(s*matrix[0,col1],
                                 s*matrix[1,col1],
                                 s*matrix[2,col1]);
                
            // Step 3.
            s = -matrix[3,col2];
            direction = new Vector3D(matrix[0,col2]+s*origin.X,
                                     matrix[1,col2]+s*origin.Y,
                                     matrix[2,col2]+s*origin.Z);
        }
        

        // Returns the indices of the smallest two diagonal elements of matrix
        private static void FindSmallestTwoDiagonal(double[,] matrix, out int evec1, out int evec2)
        {
            evec1 = 0;
            evec2 = 1;
            // And corresponding squared eigenvalues.
            double eval1 = matrix[0,0]*matrix[0,0];
            double eval2 = matrix[1,1]*matrix[1,1];
            
            for (int i = 2; i < 4; ++i)
            {
                // Replace second smallest if necessary.
                double val = matrix[i,i]*matrix[i,i];
                if (val < eval1)
                {
                    if (eval1 < eval2)
                    {
                        eval2 = val;
                        evec2 = i;
                    }
                    else
                    {
                        eval1 = val;
                        evec1 = i;
                    }
                }
                else if (val < eval2)
                {
                    eval2 = val;
                    evec2 = i;
                }
            }
        }
        
        // Returns the "line matrix" corresponding to the line (origin,direction) transformed by the
        // inverse *transform* of modelMatrix.  (To transform a line by the inverse transform
        // requires multiplying by the non-inverted matrix.)
        private static double[,] TransformedLineMatrix(ref Matrix3D modelMatrix,
                                                       ref Point3D origin, ref Vector3D direction)
        {
                double x1 = origin.X;
                double y1 = origin.Y;
                double z1 = origin.Z;
                // w1 = 1
                double x2 = direction.X;
                double y2 = direction.Y;
                double z2 = direction.Z;
                // w2 = 0

                // To prove to yourself that this matrix is correct just multiply by the two
                // (homogeneous) points on the line.  Any other homogeneous point on the line is a
                // linear combination of them.

                double a = y2*z1-y1*z2;
                double b = x1*z2-x2*z1;
                double c = x2*y1-x1*y2;

                Matrix3D m = modelMatrix *
                             new Matrix3D(a,  y2,  z2,   0,
                                          b, -x2,   0,  z2,
                                          c,   0, -x2, -y2,
                                          0,   c,  -b,   a);
                double[,] matrix = new double[4,4];
                matrix[0,0] = m.M11;
                matrix[0,1] = m.M12;
                matrix[0,2] = m.M13;
                matrix[0,3] = m.M14;
                matrix[1,0] = m.M21;
                matrix[1,1] = m.M22;
                matrix[1,2] = m.M23;
                matrix[1,3] = m.M24;
                matrix[2,0] = m.M31;
                matrix[2,1] = m.M32;
                matrix[2,2] = m.M33;
                matrix[2,3] = m.M34;
                matrix[3,0] = m.OffsetX;
                matrix[3,1] = m.OffsetY;
                matrix[3,2] = m.OffsetZ;
                matrix[3,3] = m.M44;
                return matrix;
        }

        // Scales M so that its largest element is 1 and then returns M MT
        // (MT=transpose(M))
        private static double [,] Square(double[,] m)
        {
            double[,] o = new double[4,4];

            // Scale the matrix so that its largest element is 1.
            double maxvalue = 0;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    maxvalue = Math.Max(maxvalue,m[i,j]*m[i,j]);
                }
            }
            maxvalue = Math.Sqrt(maxvalue);
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    m[i,j] /= maxvalue;
                }
            }

            // Compute its square.
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    double d = 0;
                    for (int k = 0; k < 4; ++k)
                    {
                        d += m[i,k]*m[j,k];
                    }
                    o[i,j] = d;
                }
            }
            return o;
        }

        // See section 8.4 of Golub & Van Loan "Matrix Computation"
        // Remember, J is this Jacobi rotation.  JT is the transpose.
        // Also section 5.1.8 shows the formula for multiplying by the Jacobi rotation
        // Though as they helpfully point out a Jacobi rotation is the same as a Givens rotation
        private struct JacobiRotation
        {
            public JacobiRotation(int p, int q, double[,] a)
            {
                // Constructs a 2D rotation matrix M as follows
                // Start with identity.
                // Zero the p & q rows & columns
                // Then set the intersections of these rows & columns as follows
                //    [ M_pp M_pq ]  =  [ c s]
                //    [ M_qp M_qq ]     [-s c]
                //
                // Where c & s are a cosine-sine pair calculated so that multiplying a by this will
                // descrease the the off-diagonal elements.

                _p = p;
                _q = q;
                
                double tau = (a[q,q] - a[p,p])/(2*a[p,q]);
                if (tau < Double.MaxValue && tau > -Double.MaxValue)
                {
                    double root = Math.Sqrt(1+tau*tau);
                    // Choose the smaller of -tau +/- root
                    double t = -tau < 0 ? -tau + root : -tau - root;
                    _c = 1/Math.Sqrt(1+t*t);
                    _s = t * _c;
                }
                else
                {
                    _c = 1;
                    _s = 0;
                }
            }

            // These functions overwrite & return their argument.
            
            // returns JT a J
            public double[,] LeftRightMultiply(double [,] a)
            {
                return RightMultiply(LeftMultiplyTranspose(a));
            }

            // returns a J
            public double[,] RightMultiply(double [,] a)
            {
                for (int j = 0; j < 4; ++j)
                {
                    double tau1 = a[j,_p];
                    double tau2 = a[j,_q];
                    
                    a[j,_p] = _c * tau1 - _s * tau2;
                    a[j,_q] = _s * tau1 + _c * tau2;
                }

                return a;
            }


            // returns JT a
            public double[,] LeftMultiplyTranspose(double [,] a)
            {
                for (int j = 0; j < 4; ++j)
                {
                    double tau1 = a[_p,j];
                    double tau2 = a[_q,j];
                    
                    a[_p,j] = _c * tau1 - _s * tau2;
                    a[_q,j] = _s * tau1 + _c * tau2;
                }

                return a;
            }

            private int _p, _q;
            private double _c, _s;
        }

        // This method determines if the line/ray intersects the triangle.
        // If "origin" and "direction" truely represent a line, "type" should be front and back because
        // we don't have any true direction.
        //
        //     origin/direction define the line/ray
        //     v0/v1/v2 define the triangle
        //
        // origin, direction, v0, v1, v2 are passed by ref for perf.  They are NOT MODIFIED
        //
        // If this method returns false, ignore the values of hitCoord and dist.
        //
        // Ported from dxg\d3dx9\mesh\intersect.cpp (12/04/03)
        // which is an implementation of "Fast, Minimum Storage Ray-Triangle Intersection" by Moller + Trumbore
        internal static bool ComputeLineTriangleIntersection(
            FaceType type,
            ref Point3D origin,
            ref Vector3D direction,
            ref Point3D v0,
            ref Point3D v1,
            ref Point3D v2,
            out Point hitCoord,
            out double dist)
        {
            Vector3D e1;
            Point3D.Subtract(ref v1, ref v0, out e1);
            Vector3D e2;
            Point3D.Subtract(ref v2, ref v0, out e2);

            Vector3D r;
            Vector3D.CrossProduct(ref direction, ref e2, out r);
            
            double a = Vector3D.DotProduct(ref e1, ref r);
        
            Vector3D s; 
            if (a > 0 && (type & FaceType.Front) != 0)
            {
                Point3D.Subtract(ref origin, ref v0, out s);
            }
            else if (a < 0 && (type & FaceType.Back) != 0)
            {
                Point3D.Subtract(ref v0, ref origin, out s);
                a = -a;
            }
            else
            {
                hitCoord = new Point();
                dist = 0;
                return false;
            }
        
            double u = Vector3D.DotProduct(ref s, ref r);
            if ((u < 0) || (a < u)) 
            {
                hitCoord = new Point();
                dist = 0;
                return false;
            }

            Vector3D q;
            Vector3D.CrossProduct(ref s, ref e1, out q);
        
            double v = Vector3D.DotProduct(ref direction, ref q);
            if ((v < 0) || (a < (u + v))) 
            {
                hitCoord = new Point();
                dist = 0;
                return false;
            }
        
            double t = Vector3D.DotProduct(ref e2, ref q);
            double f = 1 / a;
            
            t = t * f;
            u = u * f;
            v = v * f;
  
            hitCoord = new Point(u, v);
            dist = t;

            return true;
        }

        // This function returns true if the probe line intersects the bbox volume (not
        // just the surface of the box).  Does LINE and RAY intersection tests.
        //
        // Based on Woo's method presented in Gems I, p. 395.  See also "Real-Time
        // Rendering", Haines, sec 10.4.2.
        //
        //     origin/direction define the non-oriented line or ray
        //     box is the volume to intersect
        //
        // origin, direction, and box are passed by ref for perf.  They are NOT MODIFIED
        //
        // Ported from dxg\d3dx9\mesh\intersect.cpp (12/04/03)
        internal static bool ComputeLineBoxIntersection(ref Point3D origin, ref Vector3D direction, ref Rect3D box, bool isRay)
        {
            // Reject empty bounding boxes.
            if (box.IsEmpty)
            {
                return false;
            }
        
            bool inside = true;
            bool[] middle = new bool[3];        // True if ray origin in middle for coord i.
            double[] plane = new double[3];     // Candidate BBox Planes
            int i;                              // General Loop Counter
        
            // Find all candidate planes; select the plane nearest to the ray origin
            // for each coordinate.
        
            double[] rgfMin = new double[] { box.X, box.Y, box.Z };
            double[] rgfMax = new double[] { box.X + box.SizeX, box.Y + box.SizeY, box.Z + box.SizeZ };
            double[] rgfRayPos = new double[] { origin.X, origin.Y, origin.Z };
            double[] rgfRayDir = new double[] { direction.X, direction.Y, direction.Z };
        
            for (i = 0; i < 3; ++i)
            {
                if (rgfRayPos[i] < rgfMin[i])
                {
                    middle[i] = false;
                    plane[i] = rgfMin[i];
                    inside = false;
                }
                else if (rgfRayPos[i] > rgfMax[i])
                {
                    middle[i] = false;
                    plane[i] = rgfMax[i];
                    inside = false;
                }
                else
                {
                    middle[i] = true;
                }
            }
        
            // If the ray origin is inside the box, then it must intersect the volume
            // of the bounding box.
            if (inside)
            {
                return true;
            }

            double rayt;
            if (isRay)
            {
                // If we never end up finding the furthest plane, the box will be
                // rejected since rayt is negative
                rayt = -1;
            }
            else
            {
                // Can't use -1 in the line case because rayt^2 is 1 and we
                // would miss valid ts in the furthest plane search
                rayt = 0;
            }
            
            int maxPlane = 0;
            for (i = 0; i < 3; ++i)
            {
                if (!middle[i] && (rgfRayDir[i] != 0))
                {
                    double t = (plane[i] - rgfRayPos[i]) / rgfRayDir[i];

                    if (isRay)
                    {
                        if (t > rayt)
                        {
                            rayt = t;
                            maxPlane = i; 
                        }
                    }
                    else
                    {
                        // In the original ray algorithm this test to find the furthest plane from the
                        // origin was t > rayt which only considered planes in the positive direction.
                        // I changed it to compare squared values so that we look for the farthest
                        // plane in either direction.

                        // Note that if the line intersects the box then all of the planes considered
                        // in this loop must be on the same side of the origin (because we are finding
                        // the intersection of the line with the space formed by the intersection of
                        // the half-spaces formed by the planes -- which incidentally point away from
                        // the origin.)
                        if (t * t > rayt * rayt)
                        {   
                            rayt = t;
                            maxPlane = i;
                        }
                    }
                }
            }

            // If the box is behind the ray, or if the box is beyond the extent of the
            // ray, then return no-intersect.

            if (isRay && rayt < 0)
            {
                return false;
            }
        
            // The intersection candidate point is within acceptible range; test each
            // coordinate here to ensure that it actually hits the box.
        
            for (i = 0; i < 3; ++i)
            {
                if (i != maxPlane)
                {
                    double c = rgfRayPos[i] + (rayt * rgfRayDir[i]);
                    if ((c < rgfMin[i]) || (rgfMax[i] < c))
                        return false;
                }
            }

            return true;
        }
    }
}
