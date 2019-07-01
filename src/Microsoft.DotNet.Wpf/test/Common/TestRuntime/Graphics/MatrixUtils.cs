// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***********************************************************
 *   Description:   Helpers for Matrix math
 *
 *   Notes about Avalon's matrix math:
 *
 *      Matrices are implemented using Row Vectors instead of Column Vectors
 *  like most people are used to.  Below I will show examples of how multiplication
 *  occurs in Avalon as a reference to new users - or old dogs that can't learn new
 *  tricks :)
 *
 *      The point/vector is always to the left of the matrix during multiplication
 *  because it is a Row Vector:
 *
 *                    [ m11 m12 m13 m14 ]   [ x * m11 + y * m21 + z * m31 + w * m41
 *      [ x y z w ] * [ m21 m22 m23 m24 ] =   x * m12 + y * m22 + z * m32 + w * m42
 *                    [ m31 m32 m33 m34 ]     x * m13 + y * m23 + z * m33 + w * m43
 *                    [ m41 m42 m43 m44 ]     x * m14 + y * m24 + z * m34 + w * m44 ]
 *
 *      Transformations are always applied from left to right.  Append a transform
 *  to an existing matrix and it will be applied to the point/vector after the
 *  existing matrix is applied (conceptually, of course. In reality, all transforms
 *  are applied at once, thus the advantage of using matrices).  Prepend a transform
 *  and it will be applied to the point/vector before the existing matrix.
 *
 *      [ x y z w ] * m1 * m2 * m3   // m1 goes first, then m2, then m3
 *
 *      Avalon likes to call m41, m42, and m43 "OffsetX," "OffsetY," and "OffsetZ"
 *  because when working with 3D points (w == 1), that's exactly what these elements
 *  do after the 3x3 matrix math has been performed.  It doesn't really have the same
 *  meaning when dealing with 4D points, but I don't think there is a plan to make a
 *  Matrix4D anytime soon (ever)... :)
 *
 *
 ************************************************************/

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics
{
    /// <summary/>
    public class MatrixUtils
    {
        /// <summary/>
        public const double epsilon = 0.0000001;

        /// <summary/>
        public static int GetHashCode(Matrix m1)
        {
            return (m1.M11.GetHashCode() ^ m1.M12.GetHashCode() ^ m1.M21.GetHashCode() ^ m1.M22.GetHashCode() ^ m1.OffsetX.GetHashCode() ^ m1.OffsetY.GetHashCode());
        }

        /// <summary/>
        public static bool IsIdentity(Matrix m1)
        {
            return IsOne(m1.M11) && IsZero(m1.M12) && IsZero(m1.M21) && IsOne(m1.M22) && IsZero(m1.OffsetX) && IsZero(m1.OffsetY);
        }

        /// <summary/>
        public static bool IsIdentity(Matrix3D m)
        {
            return
                 IsOne(m.M11) && IsZero(m.M12) && IsZero(m.M13) && IsZero(m.M14) &&
                 IsZero(m.M21) && IsOne(m.M22) && IsZero(m.M23) && IsZero(m.M24) &&
                 IsZero(m.M31) && IsZero(m.M32) && IsOne(m.M33) && IsZero(m.M34) &&
                 IsZero(m.OffsetX) && IsZero(m.OffsetY) && IsZero(m.OffsetZ) && IsOne(m.M44);
        }

        /// <summary/>
        public static bool IsAffine(Matrix3D m)
        {
            return (MathEx.AreCloseEnough(m.M14, 0) && MathEx.AreCloseEnough(m.M24, 0) &&
                     MathEx.AreCloseEnough(m.M34, 0) && MathEx.AreCloseEnough(m.M44, 1));
        }

        /// <summary/>
        public static Point3D Transform(Point3D p, Transform3D tx)
        {
            return Transform(p, Value(tx));
        }

        /// <summary/>
        public static Point3D Transform(Point3D p, Matrix3D m)
        {
            double x = p.X * m.M11 + p.Y * m.M21 + p.Z * m.M31 + m.OffsetX;
            double y = p.X * m.M12 + p.Y * m.M22 + p.Z * m.M32 + m.OffsetY;
            double z = p.X * m.M13 + p.Y * m.M23 + p.Z * m.M33 + m.OffsetZ;
            double w = p.X * m.M14 + p.Y * m.M24 + p.Z * m.M34 + m.M44;

            return new Point3D(x / w, y / w, z / w);
        }

        /// <summary/>
        public static Vector3D Transform(Vector3D v, Transform3D tx)
        {
            return Transform(v, Value(tx));
        }

        /// <summary/>
        public static Vector3D Transform(Vector3D v, Matrix3D m)
        {
            return new Vector3D(
                        v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31,
                        v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32,
                        v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33
                        );
        }

        /// <summary/>
        public static Point4D Transform(Point4D p, Transform3D tx)
        {
            return Transform(p, Value(tx));
        }

        /// <summary/>
        public static Point4D Transform(Point4D p, Matrix3D m)
        {
            return new Point4D(
                        p.X * m.M11 + p.Y * m.M21 + p.Z * m.M31 + p.W * m.OffsetX,
                        p.X * m.M12 + p.Y * m.M22 + p.Z * m.M32 + p.W * m.OffsetY,
                        p.X * m.M13 + p.Y * m.M23 + p.Z * m.M33 + p.W * m.OffsetZ,
                        p.X * m.M14 + p.Y * m.M24 + p.Z * m.M34 + p.W * m.M44
                        );
        }

        /// <summary/>
        public static Point Transform(Point p1, Matrix m1)
        {
            return new Point(p1.X * m1.M11 + p1.Y * m1.M21 + m1.OffsetX, p1.X * m1.M12 + p1.Y * m1.M22 + m1.OffsetY);
        }

        /// <summary/>
        public static Point[] Transform(Point[] p1, Matrix m1)
        {
            if (p1 == null)
            {
                return null;
            }

            Point[] result = new Point[p1.Length];

            for (int i = 0; i < p1.Length; i++)
            {
                result[i] = Transform(p1[i], m1);
            }

            return result;
        }

        /// <summary/>
        public static Vector Transform(Vector v1, Matrix m1)
        {
            return new Vector(v1.X * m1.M11 + v1.Y * m1.M21, v1.X * m1.M12 + v1.Y * m1.M22);
        }

        /// <summary/>
        public static Vector[] Transform(Vector[] v1, Matrix m1)
        {
            if (v1 == null)
            {
                return null;
            }

            Vector[] result = new Vector[v1.Length];

            for (int i = 0; i < v1.Length; i++)
            {
                result[i] = Transform(v1[i], m1);
            }

            return result;
        }

        /// <summary/>
        public static Matrix3D Transpose(Matrix3D m)
        {
            return new Matrix3D(m.M11, m.M21, m.M31, m.OffsetX,
                                 m.M12, m.M22, m.M32, m.OffsetY,
                                 m.M13, m.M23, m.M33, m.OffsetZ,
                                 m.M14, m.M24, m.M34, m.M44);
        }

        /// <summary/>
        public static double Determinant(Matrix m1)
        {
            return MathEx.Determinant(m1.M11, m1.M12, 0,
                                       m1.M21, m1.M22, 0,
                                       m1.OffsetX, m1.OffsetY, 1);
        }

        /// <summary/>
        public static double Determinant(Matrix3D m)
        {
            return MathEx.Determinant(m.M11, m.M12, m.M13, m.M14,
                                       m.M21, m.M22, m.M23, m.M24,
                                       m.M31, m.M32, m.M33, m.M34,
                                       m.OffsetX, m.OffsetY, m.OffsetZ, m.M44);
        }

        /// <summary/>
        public static double GetAverageScaleFactor(Matrix3D m)
        {
            double det = MathEx.Determinant(m.M11, m.M12, m.M13,
                                             m.M21, m.M22, m.M23,
                                             m.M31, m.M32, m.M33);
            return Math.Pow(Math.Abs(det), 1.0 / 3.0);
        }

        /// <summary/>
        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            return new Matrix(
                        m1.M11 * m2.M11 + m1.M12 * m2.M21,
                        m1.M11 * m2.M12 + m1.M12 * m2.M22,
                        m1.M21 * m2.M11 + m1.M22 * m2.M21,
                        m1.M21 * m2.M12 + m1.M22 * m2.M22,
                        m1.OffsetX * m2.M11 + m1.OffsetY * m2.M21 + m2.OffsetX,
                        m1.OffsetX * m2.M12 + m1.OffsetY * m2.M22 + m2.OffsetY);
        }

        /// <summary/>
        public static Matrix3D Multiply(Transform3DGroup txg)
        {
            Matrix3D[] matrices = new Matrix3D[txg.Children.Count];

            for (int i = 0; i < matrices.Length; i++)
            {
                matrices[i] = txg.Children[i].Value;
            }
            return Multiply(matrices);
        }

        /// <summary/>
        public static Matrix3D Multiply(params Matrix3D[] matrices)
        {
            if (matrices.Length == 1)
            {
                return matrices[0];
            }

            Matrix3D result = Transform3D.Identity.Value;

            // Multiply from back to front because Avalon goes front to back
            // We can possibly exploit precision errors this way.
            for (int i = matrices.Length - 1; i >= 0; i--)
            {
                result = Multiply(matrices[i], result);
            }
            return result;
        }

        /// <summary/>
        public static Matrix3D Multiply(Matrix3D m1, Matrix3D m2)
        {
            return new Matrix3D(
                        m1.M11 * m2.M11 + m1.M12 * m2.M21 + m1.M13 * m2.M31 + m1.M14 * m2.OffsetX,
                        m1.M11 * m2.M12 + m1.M12 * m2.M22 + m1.M13 * m2.M32 + m1.M14 * m2.OffsetY,
                        m1.M11 * m2.M13 + m1.M12 * m2.M23 + m1.M13 * m2.M33 + m1.M14 * m2.OffsetZ,
                        m1.M11 * m2.M14 + m1.M12 * m2.M24 + m1.M13 * m2.M34 + m1.M14 * m2.M44,
                        m1.M21 * m2.M11 + m1.M22 * m2.M21 + m1.M23 * m2.M31 + m1.M24 * m2.OffsetX,
                        m1.M21 * m2.M12 + m1.M22 * m2.M22 + m1.M23 * m2.M32 + m1.M24 * m2.OffsetY,
                        m1.M21 * m2.M13 + m1.M22 * m2.M23 + m1.M23 * m2.M33 + m1.M24 * m2.OffsetZ,
                        m1.M21 * m2.M14 + m1.M22 * m2.M24 + m1.M23 * m2.M34 + m1.M24 * m2.M44,
                        m1.M31 * m2.M11 + m1.M32 * m2.M21 + m1.M33 * m2.M31 + m1.M34 * m2.OffsetX,
                        m1.M31 * m2.M12 + m1.M32 * m2.M22 + m1.M33 * m2.M32 + m1.M34 * m2.OffsetY,
                        m1.M31 * m2.M13 + m1.M32 * m2.M23 + m1.M33 * m2.M33 + m1.M34 * m2.OffsetZ,
                        m1.M31 * m2.M14 + m1.M32 * m2.M24 + m1.M33 * m2.M34 + m1.M34 * m2.M44,
                        m1.OffsetX * m2.M11 + m1.OffsetY * m2.M21 + m1.OffsetZ * m2.M31 + m1.M44 * m2.OffsetX,
                        m1.OffsetX * m2.M12 + m1.OffsetY * m2.M22 + m1.OffsetZ * m2.M32 + m1.M44 * m2.OffsetY,
                        m1.OffsetX * m2.M13 + m1.OffsetY * m2.M23 + m1.OffsetZ * m2.M33 + m1.M44 * m2.OffsetZ,
                        m1.OffsetX * m2.M14 + m1.OffsetY * m2.M24 + m1.OffsetZ * m2.M34 + m1.M44 * m2.M44
                        );
        }

        /// <summary/>
        public static Matrix Rotate(double angle)
        {
            angle = angle % 360;
            angle = angle * Math.PI / 180.0;

            return new Matrix(Math.Cos(angle), Math.Sin(angle),
                               -Math.Sin(angle), Math.Cos(angle),
                                      0, 0);
        }

        /// <summary/>
        public static Matrix Rotate(double angle, Point center)
        {
            Matrix result = Translate(new Point(-center.X, -center.Y));
            result = Multiply(result, Rotate(angle));
            result = Multiply(result, Translate(center));

            return result;
        }

        /// <summary/>
        public static Matrix3D Rotate(AxisAngleRotation3D r)
        {
            return Rotate(r.Axis, r.Angle);
        }

        /// <summary/>
        public static Matrix3D Rotate(AxisAngleRotation3D r, Point3D center)
        {
            return Rotate(r.Axis, r.Angle, center);
        }

        /// <summary/>
        public static Matrix3D Rotate(Quaternion q)
        {
            double x = q.X;
            double y = q.Y;
            double z = q.Z;
            double w = q.W;

            double xx = x * x;
            double xy = x * y;
            double xz = x * z;
            double xw = x * w;

            double yy = y * y;
            double yz = y * z;
            double yw = y * w;

            double zz = z * z;
            double zw = z * w;

            return new Matrix3D(
                            1 - 2 * (yy + zz), 2 * (xy + zw), 2 * (xz - yw), 0,
                            2 * (xy - zw), 1 - 2 * (xx + zz), 2 * (yz + xw), 0,
                            2 * (xz + yw), 2 * (yz - xw), 1 - 2 * (xx + yy), 0,
                            0, 0, 0, 1
                            );
        }

        /// <summary/>
        public static Matrix3D Rotate(Quaternion q, Point3D center)
        {
            Matrix3D m1 = Translate(Const.p0 - center);
            Matrix3D m2 = Rotate(q);
            Matrix3D m3 = Translate(center - Const.p0);

            return MatrixUtils.Multiply(MatrixUtils.Multiply(m1, m2), m3);
        }

        /// <summary/>
        public static Matrix3D Rotate(Vector3D axis, double angle)
        {
            return Rotate(new Quaternion(axis, angle));
        }

        /// <summary/>
        public static Matrix3D Rotate(Vector3D axis, double angle, Point3D center)
        {
            Matrix3D m1 = Translate(Const.p0 - center);
            Matrix3D m2 = Rotate(axis, angle);
            Matrix3D m3 = Translate(center - Const.p0);

            return MatrixUtils.Multiply(MatrixUtils.Multiply(m1, m2), m3);
        }

        /// <summary/>
        public static Matrix Scale(double scaleX, double scaleY)
        {
            return new Matrix(scaleX, 0, 0, scaleY, 0, 0);
        }

        /// <summary/>
        public static Matrix Scale(double scaleX, double scaleY, Point center)
        {
            Matrix result = Translate(new Point(-center.X, -center.Y));
            result = Multiply(result, Scale(scaleX, scaleY));
            result = Multiply(result, Translate(center));

            return result;
        }

        /// <summary/>
        public static Matrix3D Scale(Vector3D scale)
        {
            Matrix3D m = Matrix3D.Identity;
            m.M11 = scale.X;
            m.M22 = scale.Y;
            m.M33 = scale.Z;

            return m;
        }

        /// <summary/>
        public static Matrix3D Scale(Vector3D scale, Point3D center)
        {
            Matrix3D m1 = Translate(Const.p0 - center);
            Matrix3D m2 = Scale(scale);
            Matrix3D m3 = Translate(center - Const.p0);

            return MatrixUtils.Multiply(MatrixUtils.Multiply(m1, m2), m3);
        }

        /// <summary/>
        public static Matrix Skew(double skewX, double skewY)
        {
            skewX = skewX % 360;
            skewY = skewY % 360;

            skewX = skewX * Math.PI / 180.0;
            skewY = skewY * Math.PI / 180.0;

            return new Matrix(1, Math.Tan(skewY),
                               Math.Tan(skewX), 1,
                                      0, 0);
        }

        /// <summary/>
        public static Matrix Skew(double skewX, double skewY, Point center)
        {
            Matrix result = Translate(-center.X, -center.Y);
            result = Multiply(result, Skew(skewX, skewY));
            result = Multiply(result, Translate(center));

            return result;
        }

        /// <summary/>
        public static Matrix Translate(Point offset)
        {
            return new Matrix(1, 0, 0, 1, offset.X, offset.Y);
        }

        /// <summary/>
        public static Matrix Translate(double offsetX, double offsetY)
        {
            return new Matrix(1, 0, 0, 1, offsetX, offsetY);
        }

        /// <summary/>
        public static Matrix3D Translate(Vector3D translation)
        {
            Matrix3D m = Matrix3D.Identity;
            m.OffsetX = translation.X;
            m.OffsetY = translation.Y;
            m.OffsetZ = translation.Z;

            return m;
        }

        /// <summary>
        /// null-safe conversion from Transform3D to Matrix3D
        /// </summary>
        public static Matrix3D Value(Transform3D tx)
        {
            return (tx == null) ? Matrix3D.Identity : tx.Value;
        }

        /// <summary>
        /// null-safe conversion from Transform to Matrix
        /// </summary>
        public static Matrix Value(Transform tx)
        {
            return (tx == null) ? Matrix.Identity : tx.Value;
        }

        /// <summary/>
        public static string ToStr(Matrix m1)
        {
            return string.Format(
                    "[ {0,0}, {1,22}, {2,22} ]\r\n" +
                    "[ {3,0}, {4,22}, {5,22} ]\r\n" +
                    "[ {6,0}, {7,22}, {8,22} ]\r\n",
                    m1.M11, m1.M12, 0,
                    m1.M21, m1.M22, 0,
                    m1.OffsetX, m1.OffsetY, 1
                    );
        }

        /// <summary/>
        public static string ToStr(Matrix3D m)
        {
            return string.Format(
                    "[ {0,22}, {1,22}, {2,22}, {3,22} ]\r\n" +
                    "[ {4,22}, {5,22}, {6,22}, {7,22} ]\r\n" +
                    "[ {8,22}, {9,22}, {10,22}, {11,22} ]\r\n" +
                    "[ {12,22}, {13,22}, {14,22}, {15,22} ]\r\n",
                    m.M11, m.M12, m.M13, m.M14,
                    m.M21, m.M22, m.M23, m.M24,
                    m.M31, m.M32, m.M33, m.M34,
                    m.OffsetX, m.OffsetY, m.OffsetZ, m.M44
                    );
        }

        /// <summary>
        /// ViewMatrix encapsulates the camera transform plus the camera's view matrix.
        /// Therefore, the ViewMatrix is the matrix representing the camera's viewpoint from its current position.
        /// </summary>
        public static Matrix3D ViewMatrix(Camera camera)
        {
            Matrix3D inverseCamera = InverseCameraMatrix(camera);
            if (camera is MatrixCamera)
            {
                MatrixCamera c = camera as MatrixCamera;
                return Multiply(inverseCamera, c.ViewMatrix);
            }
            else if (camera is ProjectionCamera)
            {
                ProjectionCamera c = camera as ProjectionCamera;
                return Multiply(inverseCamera,
                                 MakeViewMatrix(c.Position, c.LookDirection, c.UpDirection));
            }
            throw new ArgumentException("Invalid camera specified " + camera.GetType());
        }

        /// <summary/>
        public static Matrix3D ProjectionMatrix(Camera camera)
        {
            if (camera is MatrixCamera)
            {
                MatrixCamera c = camera as MatrixCamera;
                return c.ProjectionMatrix;
            }
            else if (camera is PerspectiveCamera)
            {
                PerspectiveCamera c = camera as PerspectiveCamera;
                return MakePerspectiveProjection(c.NearPlaneDistance, c.FarPlaneDistance, c.FieldOfView, c.FieldOfView);
            }
            else if (camera is OrthographicCamera)
            {
                OrthographicCamera c = camera as OrthographicCamera;
                return MakeOrthographicProjection(c.NearPlaneDistance, c.FarPlaneDistance, c.Width, c.Width);
            }
            throw new ArgumentException("Invalid camera specified " + camera.GetType());
        }

        /// <summary/>
        public static Matrix3D HomogenousToScreenMatrix(Rect viewport, bool usingProjectionCamera)
        {
            double scaleX = viewport.Width / 2.0;
            double scaleY = viewport.Height / 2.0;

            // ProjectionCamera uses uniform scaling based on width (scaleX)
            double m11 = scaleX;

            // MatrixCamera scales width and height independently.
            double m22 = (usingProjectionCamera) ? scaleX : scaleY;

            // We center the origin based on the Width and Height of the viewport,
            //  then offset again based on the viewport's top left corner.
            double offsetX = scaleX + viewport.X;
            double offsetY = scaleY + viewport.Y;

            return new Matrix3D(m11, 0, 0, 0,
                                      0, -m22, 0, 0,     // Flip the y-axis
                                      0, 0, 1, 0,
                                offsetX, offsetY, 0, 1);
        }

        /// <summary/>
        public static Matrix3D InverseCameraMatrix(Camera camera)
        {
            Matrix3D inverse = Value(camera.Transform);
            if (inverse.HasInverse)
            {
                inverse.Invert();
            }
            else
            {
                // Return a matrix that projects everything to 0 (doesn't render anything)
                inverse = noRenderMatrix;
            }
            return inverse;
        }

        /// <summary/>
        public static Matrix3D WorldToScreenMatrix(Camera camera, Rect viewport)
        {
            Matrix3D worldToEye = ViewMatrix(camera);
            Matrix3D eyeToHomogenous = ProjectionMatrix(camera);
            Matrix3D homogenousToScreen = HomogenousToScreenMatrix(viewport, camera is ProjectionCamera);
            return Multiply(worldToEye, eyeToHomogenous, homogenousToScreen);
        }

        /// <summary/>
        public static Matrix3D MakeNormalTransform(Matrix3D modelToEye)
        {
            // Begin with the model-to-eye transform for everything else
            Matrix3D normalTransform = modelToEye;

            // To properly transform normals, we need the transpose of the inverse.
            // See Real Time Rendering, second edition p.35, section 3.1.7
            try
            {
                normalTransform.Invert();
            }
            catch (InvalidOperationException)
            {
                // Since the model-to-eye transform is not invertable, nothing will render.
                // Therefore, it is safe to project the normals to 0.
                return noRenderMatrix;
            }
            normalTransform = MatrixUtils.Transpose(normalTransform);

            // Forcing affine matrix - we really only care about the inner 3x3
            normalTransform.M44 = 1.0;
            normalTransform.M14 = 0.0;
            normalTransform.M24 = 0.0;
            normalTransform.M34 = 0.0;

            return normalTransform;
        }

        /// <summary/>
        public static Matrix3D MakeViewMatrix(Point3D position, Vector3D look, Vector3D up)
        {
            // Create a RH view matrix using this formula from the
            //     DirectX documentation (D3DXMatrixLookAtRH):
            //
            //  zaxis = normal(-Look)
            //  xaxis = normal(cross(Up, zaxis))
            //  yaxis = cross(zaxis, xaxis)
            //
            //  xaxis.x           yaxis.x           zaxis.x           0
            //  xaxis.y           yaxis.y           zaxis.y           0
            //  xaxis.z           yaxis.z           zaxis.z           0
            //  -dot(xaxis, eye)  -dot(yaxis, eye)  -dot(zaxis, eye)  1

            Vector3D zAxis = MathEx.Normalize(-look);
            Vector3D xAxis = MathEx.Normalize(MathEx.CrossProduct(up, zAxis));
            Vector3D yAxis = MathEx.Normalize(MathEx.CrossProduct(zAxis, xAxis));
            Vector3D Eye = new Vector3D(position.X, position.Y, position.Z);

            return new Matrix3D(
                    xAxis.X, yAxis.X, zAxis.X, 0,
                    xAxis.Y, yAxis.Y, zAxis.Y, 0,
                    xAxis.Z, yAxis.Z, zAxis.Z, 0,
                    -MathEx.DotProduct(xAxis, Eye),
                    -MathEx.DotProduct(yAxis, Eye),
                    -MathEx.DotProduct(zAxis, Eye),
                    1);
        }

        /// <summary/>
        public static Matrix3D MakeOrthographicProjection(double nearPlaneDistance, double farPlaneDistance, double width, double height)
        {
            double n = nearPlaneDistance;
            double f = farPlaneDistance;
            double t = height / 2.0;
            double b = -t;
            double r = width / 2.0;
            double l = -r;

            // Formula from Realtime Rendering, pg. 60
            // This matrix transforms the world into normalized device coordinates:
            //            -1 < x < 1, -1 < y < 1, -1 < z < 1
            Matrix3D m = new Matrix3D(2 / (r - l), 0, 0, 0,
                                              0, 2 / (t - b), 0, 0,
                                              0, 0, -2 / (f - n), 0,
                                   -(r + l) / (r - l), -(t + b) / (t - b), -(f + n) / (f - n), 1);

            // Scale z by 0.5, translate by 0.5, to put z in [0,1] range, since that's
            // what the Avalon3D spec assumes from a projection matrix
            m *= new Matrix3D(1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 0.5, 0,
                                0, 0, 0.5, 1);

            return m;
        }

        /// <summary/>
        public static Matrix3D MakePerspectiveProjection(double nearPlaneDistance, double farPlaneDistance, double fieldOfViewX, double fieldOfViewY)
        {
            double halfAngleX = MathEx.ToRadians(fieldOfViewX / 2.0);
            double halfAngleY = MathEx.ToRadians(fieldOfViewY / 2.0);

            double n = nearPlaneDistance;
            double f = farPlaneDistance;
            double t = n * Math.Tan(halfAngleY);
            double b = -t;
            double r = n * Math.Tan(halfAngleX);
            double l = -r;

            // Formula from Realtime Rendering, pg. 65 (Same as OpenGL transform)
            // This matrix transforms the world into normalized device coordinates:
            //            -1 < x < 1, -1 < y < 1, -1 < z < 1
            Matrix3D m = new Matrix3D((2 * n) / (r - l), 0, 0, 0,
                                                  0, (2 * n) / (t - b), 0, 0,
                                        (r + l) / (r - l), (t + b) / (t - b), -(f + n) / (f - n), -1,
                                                  0, 0, -(2 * f * n) / (f - n), 0);

            // Scale z by 0.5, translate by 0.5, to put z in [0,1] range, since that's
            // what the Avalon3D spec assumes from a projection matrix
            m *= new Matrix3D(1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 0.5, 0,
                                0, 0, 0.5, 1);

            return m;
        }

        /// <summary/>
        public static Matrix3D GenerateMatrix()
        {
            // We don't care too much about non-affine matrices
            // Save them for a later date
            return new Matrix3D(
                    GetDouble(), GetDouble(), GetDouble(), 0,
                    GetDouble(), GetDouble(), GetDouble(), 0,
                    GetDouble(), GetDouble(), GetDouble(), 0,
                    GetDouble(), GetDouble(), GetDouble(), 1
                    );
        }

        private static bool IsOne(double d)
        {
            return (1.0 - epsilon < d && d < 1.0 + epsilon);
        }

        private static bool IsZero(double d)
        {
            return (-epsilon < d && d < epsilon);
        }

        private static double GetDouble()
        {
            if (random == null)
            {
                random = new Random((int)DateTime.Now.Ticks);
            }
            switch (random.Next(0, 100))
            {
                case 0: return double.MaxValue;
                case 1: return double.MinValue;
                case 2: return double.Epsilon;
                case 3: return -double.Epsilon;
            }
            return random.NextDouble() * random.Next(0, 100000000) * Math.Pow(-1, random.Next(1, 2));
        }

        private static Random random;

        /// <summary>
        /// A matrix that projects to 0 in all dimensions.
        /// </summary>
        private static readonly Matrix3D noRenderMatrix = new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
    }
}
