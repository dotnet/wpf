// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;
using System.Windows;
using System;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    //TODO: Factor into Separate Classes.
    class MeshGeometry3DFactory : DiscoverableFactory<MeshGeometry3D>
    {
        public override MeshGeometry3D Create(DeterministicRandom random)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            switch (random.Next(5))
            {
                case 0:
                    mesh = CreatePlane(random);
                    break;
                case 1:
                    mesh = CreatePyramid(random);
                    break;
                case 2:
                    mesh = CreateCube(random);
                    break;
                case 3:
                    mesh = CreateSphere(random);
                    break;
                case 4:
                    mesh = CreateCylinder(random);
                    break;
                default:
                    goto case 0;
            }
            return mesh;
        }

        private MeshGeometry3D CreatePlane(DeterministicRandom random)
        {
            Point3D point0 = new Point3D(-5, -5, 0);
            Point3D point1 = new Point3D(5, -5, 0);
            Point3D point2 = new Point3D(5, 5, 0);
            Point3D point3 = new Point3D(-5, 5, 0);

            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions.Add(point0);
            mesh.Positions.Add(point1);
            mesh.Positions.Add(point2);
            mesh.Positions.Add(point3);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            Vector3D normal0 = CalculateNormal(point0, point1, point2);
            mesh.Normals.Add(normal0);
            mesh.Normals.Add(normal0);
            mesh.Normals.Add(normal0);

            Vector3D normal1 = CalculateNormal(point0, point2, point3);
            mesh.Normals.Add(normal1);
            mesh.Normals.Add(normal1);
            mesh.Normals.Add(normal1);

            for (int i = 0; i < 6; i++)
            {
                mesh.TextureCoordinates.Add(new Point(random.NextDouble(), random.NextDouble()));
            }

            return mesh;
        }

        private MeshGeometry3D CreatePyramid(DeterministicRandom random)
        {
            Point3D point0 = new Point3D(0, 0, 5);
            Point3D point1 = new Point3D(5, 0, 0);
            Point3D point2 = new Point3D(0, 5, 0);
            Point3D point3 = new Point3D(0, 0, 0);

            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions.Add(point0);
            mesh.Positions.Add(point1);
            mesh.Positions.Add(point2);
            mesh.Positions.Add(point3);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(3);

            Vector3D normal0 = CalculateNormal(point0, point1, point2);
            mesh.Normals.Add(normal0);
            mesh.Normals.Add(normal0);
            mesh.Normals.Add(normal0);

            Vector3D normal1 = CalculateNormal(point0, point2, point3);
            mesh.Normals.Add(normal1);
            mesh.Normals.Add(normal1);
            mesh.Normals.Add(normal1);

            Vector3D normal2 = CalculateNormal(point1, point3, point2);
            mesh.Normals.Add(normal2);
            mesh.Normals.Add(normal2);
            mesh.Normals.Add(normal2);

            Vector3D normal3 = CalculateNormal(point1, point0, point3);
            mesh.Normals.Add(normal3);
            mesh.Normals.Add(normal3);
            mesh.Normals.Add(normal3);

            for (int i = 0; i < 12; i++)
            {
                mesh.TextureCoordinates.Add(new Point(random.NextDouble(), random.NextDouble()));
            }

            return mesh;
        }

        private MeshGeometry3D CreateCube(DeterministicRandom random)
        {
            Point3D point0 = new Point3D(0, 0, 0);
            Point3D point1 = new Point3D(5, 0, 0);
            Point3D point2 = new Point3D(5, 0, 5);
            Point3D point3 = new Point3D(0, 0, 5);
            Point3D point4 = new Point3D(0, 5, 0);
            Point3D point5 = new Point3D(5, 5, 0);
            Point3D point6 = new Point3D(5, 5, 5);
            Point3D point7 = new Point3D(0, 5, 5);

            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions.Add(point0);
            mesh.Positions.Add(point1);
            mesh.Positions.Add(point2);
            mesh.Positions.Add(point3);
            mesh.Positions.Add(point4);
            mesh.Positions.Add(point5);
            mesh.Positions.Add(point6);
            mesh.Positions.Add(point7);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(4);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(1);

            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(7);

            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(6);

            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(7);

            Vector3D normal0 = CalculateNormal(point0, point1, point2);
            mesh.Normals.Add(normal0);
            mesh.Normals.Add(normal0);
            mesh.Normals.Add(normal0);

            Vector3D normal1 = CalculateNormal(point0, point2, point3);
            mesh.Normals.Add(normal1);
            mesh.Normals.Add(normal1);
            mesh.Normals.Add(normal1);

            Vector3D normal2 = CalculateNormal(point0, point3, point7);
            mesh.Normals.Add(normal2);
            mesh.Normals.Add(normal2);
            mesh.Normals.Add(normal2);

            Vector3D normal3 = CalculateNormal(point0, point7, point4);
            mesh.Normals.Add(normal3);
            mesh.Normals.Add(normal3);
            mesh.Normals.Add(normal3);

            Vector3D normal4 = CalculateNormal(point0, point4, point5);
            mesh.Normals.Add(normal4);
            mesh.Normals.Add(normal4);
            mesh.Normals.Add(normal4);

            Vector3D normal5 = CalculateNormal(point0, point5, point1);
            mesh.Normals.Add(normal5);
            mesh.Normals.Add(normal5);
            mesh.Normals.Add(normal5);

            Vector3D normal6 = CalculateNormal(point3, point2, point6);
            mesh.Normals.Add(normal6);
            mesh.Normals.Add(normal6);
            mesh.Normals.Add(normal6);

            Vector3D normal7 = CalculateNormal(point3, point6, point7);
            mesh.Normals.Add(normal7);
            mesh.Normals.Add(normal7);
            mesh.Normals.Add(normal7);

            Vector3D normal8 = CalculateNormal(point2, point1, point5);
            mesh.Normals.Add(normal8);
            mesh.Normals.Add(normal8);
            mesh.Normals.Add(normal8);

            Vector3D normal9 = CalculateNormal(point2, point5, point6);
            mesh.Normals.Add(normal9);
            mesh.Normals.Add(normal9);
            mesh.Normals.Add(normal9);

            Vector3D normal10 = CalculateNormal(point6, point5, point4);
            mesh.Normals.Add(normal10);
            mesh.Normals.Add(normal10);
            mesh.Normals.Add(normal10);

            Vector3D normal11 = CalculateNormal(point6, point4, point7);
            mesh.Normals.Add(normal11);
            mesh.Normals.Add(normal11);
            mesh.Normals.Add(normal11);

            for (int i = 0; i < 36; i++)
            {
                mesh.TextureCoordinates.Add(new Point(random.NextDouble(), random.NextDouble()));
            }

            return mesh;
        }

        private MeshGeometry3D CreateSphere(DeterministicRandom random)
        {
            int thetaDiv = random.Next(80) + 20;
            int phiDiv = random.Next(40) + 10;
            double radius = random.NextDouble() * 20 + 1.0;

            double divTheta = DegreeToRadian(360.0) / thetaDiv;
            double divPhi = DegreeToRadian(180.0) / phiDiv;

            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int pi = 0; pi <= phiDiv; pi++)
            {
                double phi = pi * divPhi;

                for (int ti = 0; ti <= thetaDiv; ti++)
                {
                    double theta = ti * divTheta;

                    mesh.Positions.Add(GetSphereVertexPosition(theta, phi, radius));
                    mesh.Normals.Add((Vector3D)GetSphereVertexPosition(theta, phi, 1.0));
                    mesh.TextureCoordinates.Add(new Point(random.NextDouble(), random.NextDouble()));
                }
            }

            for (int pi = 0; pi < phiDiv; pi++)
            {
                for (int ti = 0; ti < thetaDiv; ti++)
                {
                    int x0 = ti;
                    int x1 = (ti + 1);
                    int y0 = pi * (thetaDiv + 1);
                    int y1 = (pi + 1) * (thetaDiv + 1);

                    mesh.TriangleIndices.Add(x0 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y0);

                    mesh.TriangleIndices.Add(x1 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y1);
                }
            }

            return mesh;
        }

        private MeshGeometry3D CreateCylinder(DeterministicRandom random)
        {
            int thetaDiv = random.Next(40) + 10;
            int yDiv = random.Next(40) + 10;
            double radius = random.NextDouble() * 20 + 1.0;

            double maxTheta = DegreeToRadian(360.0);
            double maxY = random.Next(10) + 1;
            double minY = -1 * maxY;

            double dt = maxTheta / thetaDiv;
            double dy = (maxY - minY) / yDiv;

            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int yi = 0; yi <= yDiv; yi++)
            {
                double y = minY + yi * dy;

                for (int ti = 0; ti <= thetaDiv; ti++)
                {
                    double theta = ti * dt;

                    mesh.Positions.Add(GetCylinderVertexPosition(theta, y, radius));
                    mesh.Normals.Add((Vector3D)GetCylinderVertexPosition(theta, 0, 1.0));
                    mesh.TextureCoordinates.Add(new Point(random.NextDouble(), random.NextDouble()));
                }
            }

            for (int yi = 0; yi < yDiv; yi++)
            {
                for (int ti = 0; ti < thetaDiv; ti++)
                {
                    int x0 = ti;
                    int x1 = (ti + 1);
                    int y0 = yi * (thetaDiv + 1);
                    int y1 = (yi + 1) * (thetaDiv + 1);

                    mesh.TriangleIndices.Add(x0 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y0);

                    mesh.TriangleIndices.Add(x1 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y1);
                }
            }

            return mesh;
        }

        private Vector3D CalculateNormal(Point3D point0, Point3D point1, Point3D point2)
        {
            Vector3D vector0 = new Vector3D(point1.X - point0.X, point1.Y - point0.Y, point1.Z - point0.Z);
            Vector3D vector1 = new Vector3D(point2.X - point1.X, point2.Y - point1.Y, point2.Z - point1.Z);

            return Vector3D.CrossProduct(vector0, vector1);
        }

        private Point3D GetSphereVertexPosition(double theta, double phi, double radius)
        {
            double x = radius * Math.Sin(theta) * Math.Sin(phi);
            double y = radius * Math.Cos(phi);
            double z = radius * Math.Cos(theta) * Math.Sin(phi);

            return new Point3D(x, y, z);
        }

        private Point3D GetCylinderVertexPosition(double theta, double y, double radius)
        {
            double x = radius * Math.Cos(theta);
            double z = radius * Math.Sin(theta);

            return new Point3D(x, y, z);
        }

        private double DegreeToRadian(double degrees)
        {
            return (degrees / 180.0) * Math.PI;
        }
    }
}
