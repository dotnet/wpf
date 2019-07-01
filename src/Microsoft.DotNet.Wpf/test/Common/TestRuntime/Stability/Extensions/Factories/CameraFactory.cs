// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    abstract class ProjectionCameraFactory<T> : DiscoverableFactory<T> where T : Camera
    {
        public Point3D Position { get; set; }
        public Vector3D LookDirection { get; set; }
        public Vector3D UpDirection { get; set; }

        protected void ApplyProjectionCameraProperties(ProjectionCamera camera, DeterministicRandom random)
        {
            camera.Position = Position;
            camera.LookDirection = LookDirection;
            camera.UpDirection = UpDirection;
            camera.NearPlaneDistance = random.NextDouble() * 10;
            camera.FarPlaneDistance = camera.NearPlaneDistance + 500;
        }
    }

    class OrthographicCameraFactory : ProjectionCameraFactory<OrthographicCamera>
    {
        public override OrthographicCamera Create(DeterministicRandom random)
        {
            OrthographicCamera camera = new OrthographicCamera();
            ApplyProjectionCameraProperties(camera, random);
            camera.Width = random.NextDouble() * 5;
            return camera;
        }
    }

    class PerspectiveCameraFactory : ProjectionCameraFactory<PerspectiveCamera>
    {
        public override PerspectiveCamera Create(DeterministicRandom random)
        {
            PerspectiveCamera camera = new PerspectiveCamera();
            ApplyProjectionCameraProperties(camera, random);
            camera.FieldOfView = random.NextDouble() * 60;
            return camera;
        }
    }

    class MatrixCameraFactory : DiscoverableFactory<MatrixCamera>
    {
        public Matrix3D Matrix3D { get; set; }

        public override MatrixCamera Create(DeterministicRandom random)
        {
            MatrixCamera matrixCamera = new MatrixCamera();
            matrixCamera.ViewMatrix = Matrix3D;
            matrixCamera.ProjectionMatrix = Matrix3D;
            return matrixCamera;
        }
    }
}
