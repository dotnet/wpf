// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary/>
    public enum InterpolationMode
    {
        /// <summary/>
        Phong,
        /// <summary/>
        Gouraud
    }

    /// <summary>
    /// A face is determined by triangle winding order.
    ///  The "front" face is the CounterClockwise winding of a triangle
    /// The "back" face is the Clockwise winding of a triangle
    /// </summary>
    public enum VisibleFaces
    {
        /// <summary/>
        None = 0x0,
        /// <summary/>
        Front = 0x1,
        /// <summary/>
        Back = 0x2,
        /// <summary/>
        Both = 0x3
    }
    
    /// <summary>
    /// Custom software renderer for test verification purposes
    /// </summary>
    internal class ModelRenderer
    {


        public ModelRenderer(Bounds bounds, Camera camera, RenderBuffer buffer, params Light[] lights)
        {
            this.bounds = bounds;
            this.camera = camera;
            if (lights == null || lights.Length == 0)
            {
                lights = new Light[] { new AmbientLight(Colors.Black) };
            }
            this.lights = lights;

            // Render buffer is created externally
            this.buffer = buffer;

            depthTest = DepthTestFunction.LessThanOrEqualTo;
            interpolation = InterpolationMode.Gouraud;
        }

        public void Render(Model3D model)
        {
            Render(model, InterpolationMode.Gouraud);
        }

        public void Render(Model3D model, InterpolationMode interpolation)
        {
            this.interpolation = interpolation;

            if (model is GeometryModel3D)
            {
                Render((GeometryModel3D)model);
            }
#if SSL
            else
            {
                Render( (ScreenSpaceLines3D)model );
            }
#endif
        }


#if SSL
        private void Render( ScreenSpaceLines3D lines )
        {
            ProjectedGeometry geometry = MagicLinesToProjectedGeometry( lines, lines.Transform );
            Shader shader = new ScreenSpaceLinesShader( geometry.FrontFaceTriangles, buffer );
            shader.Rasterize( bounds.RenderBounds );

            // SSL are always front facing ( CCW winding ) triangles
            RenderSilhouetteTolerance( geometry, buffer, VisibleFaces.Front );
        }
#endif
        private void Render(GeometryModel3D model)
        {
            if (model.Geometry is MeshGeometry3D)
            {
                // Project geometry into a list of triangles
                ProjectedGeometry pg = MeshToProjectedGeometry((MeshGeometry3D)model.Geometry, model.Transform);

                // Remember the view matrix for view space lighting
                Matrix3D view = MatrixUtils.ViewMatrix(camera);

                // Render back faces
                RenderGeometry(pg, view, model.BackMaterial, VisibleFaces.Back);

                // Render front faces
                RenderGeometry(pg, view, model.Material, VisibleFaces.Front);
            }
            else if (model.Geometry == null)
            {
                // do nothing
            }
            else
            {
                throw new NotSupportedException("I cannot render Geometry3D of type: " + model.Geometry.GetType());
            }
        }

        private void RenderGeometry(ProjectedGeometry pg, Matrix3D view, Material material, VisibleFaces faces)
        {
            if (material == null)
            {
                return;
            }

            Triangle[] triangles = null;

            // Choose list based on triangle winding/facing
            switch (faces)
            {
                case VisibleFaces.Front:
                    triangles = pg.FrontFaceTriangles;
                    break;

                case VisibleFaces.Back:
                    triangles = pg.BackFaceTriangles;
                    break;

                default:
                    throw new NotSupportedException("Cannot render these type of faces: " + faces.ToString());
            }

            // We should only look into materials if we have any geometry to render.
            // Doing otherwise will throw exceptions when trying to use screen-space
            // bounds or UV coordinates.
            if (triangles.Length > 0)
            {
                // Create a flat, ordered list of textures to apply to this model
                List<Material> materials = ExtractMaterials(material);

                // Create a list of textures from the materials
                TextureFilter[] textures = TextureFilter.CreateTextures(
                        materials, pg.OriginalMinUV, pg.OriginalMaxUV, Rect.Intersect(bounds.RenderBounds, pg.ScreenSpaceBounds));

                // Use a precomputed light shader
                Shader shader = null;
                if (interpolation == InterpolationMode.Phong)
                {
                    shader = new PrecomputedPhongShader(triangles, buffer, lights, textures, view);
                }
                else
                {
                    shader = new PrecomputedGouraudShader(triangles, buffer, lights, textures, view);
                }
                shader.Rasterize(bounds.RenderBounds);
                RenderSilhouetteTolerance(pg, buffer, faces);
            }
        }

        private void RenderSilhouetteTolerance(ProjectedGeometry geometry, RenderBuffer rasterization, VisibleFaces faces)
        {
            List<Edge> silhouetteEdges = null;

            // Choose list based on triangle winding/facing
            switch (faces)
            {
                case VisibleFaces.Front:
                    silhouetteEdges = geometry.FrontFaceSilhouetteEdges;
                    break;

                case VisibleFaces.Back:
                    silhouetteEdges = geometry.BackFaceSilhouetteEdges;
                    break;

                default:
                    throw new NotSupportedException("Cannot render these type of faces: " + faces);
            }

            // Outline edge detection
            if (RenderTolerance.SilhouetteEdgeTolerance > 0)
            {
                // Create a set of triangles that will draw lines on the tolerance buffer
                SilhouetteEdgeTriangle[] edgeTriangles = new SilhouetteEdgeTriangle[2 * silhouetteEdges.Count];
                int count = 0;
                
                //double the silhouette pixel tolerance in nonstandard DPI scenario.
                //This compensates for differences in anti-aliasing sampling patterns in high and low DPI
                double tolerance = RenderTolerance.SilhouetteEdgeTolerance;
                if (!RenderTolerance.IsSquare96Dpi)
                {
                    tolerance *=4 ;
                }

                foreach (Edge edge in silhouetteEdges)
                {
                    // FWD triangle
                    edgeTriangles[count++] = new SilhouetteEdgeTriangle(edge.Start, edge.End, tolerance);

                    // BWD triangle
                    edgeTriangles[count++] = new SilhouetteEdgeTriangle(edge.End, edge.Start, tolerance);
                }

                // Render our ignore geometry onto the tolerance buffer.
                RenderToToleranceShader edgeIgnore = new RenderToToleranceShader(edgeTriangles, rasterization);
                edgeIgnore.Rasterize(bounds.RenderBounds);
            }
        }

        private List<Material> ExtractMaterials(Material material)
        {
            List<Material> materials = new List<Material>();
            if (material is MaterialGroup)
            {
                ExtractMaterialsRecursive((MaterialGroup)material, materials);
            }
            else if (material != null)
            {
                // NOTE: we need to use CloneCurrentValue() per this bug:
                //  (Bug 1200256 - Windows OS Bugs) CloneCurrentValue should return current value of sub-objects.
                materials.Add(material.CloneCurrentValue());
            }
            return materials;
        }

        private void ExtractMaterialsRecursive(MaterialGroup group, List<Material> materials)
        {
            if (group.Children == null)
            {
                return;
            }
            foreach (Material m in group.Children)
            {
                if (m is MaterialGroup)
                {
                    ExtractMaterialsRecursive((MaterialGroup)m, materials);
                }
                else
                {
                    // NOTE: we need to use CloneCurrentValue() per this bug:
                    //  (Bug 1200256 - Windows OS Bugs) CloneCurrentValue should return current value of sub-objects.
                    materials.Add(m.CloneCurrentValue());
                }
            }
        }
#if SSL
        private ProjectedGeometry MagicLinesToProjectedGeometry( ScreenSpaceLines3D lines, Transform3D tx )
        {
            // These matrices are the same as the ones in MeshToProjectedGeometry
            Matrix3D modelToWorld = tx.Value;
            Matrix3D worldToEye = MatrixUtils.ViewMatrix( camera );
            Matrix3D modelToEyeMatrix = modelToWorld * worldToEye;
            Matrix3D projectionMatrix = MatrixUtils.ProjectionMatrix( camera );
            Matrix3D toScreenSpace = MatrixUtils.HomogenousToScreenMatrix( bounds.ViewportBounds, camera is ProjectionCamera );
            Matrix3D projectToViewport = projectionMatrix * toScreenSpace;

            // For n points, there are n-1 lines.
            int numLines = ( lines.Points == null ) ? 0 : lines.Points.Count - 1;

            // ScreenSpaceLine triangles are created post-projection, therefore reverse winding caused by
            //  having a negative determinant in the modelToEyeMatrix will not affect our rendering.
            SSLProjectedGeometry geometry = new SSLProjectedGeometry( projectToViewport );

            for ( int n = 0; n < numLines; n++ )
            {
                Point3D begin = lines.Points[ n ];
                Point3D end = lines.Points[ n + 1 ];

                begin *= modelToEyeMatrix;
                end *= modelToEyeMatrix;

                geometry.AddScreenSpaceLine( begin, end, lines.Thickness, lines.Color );
            }

            return geometry;
        }
#endif

        private ProjectedGeometry MeshToProjectedGeometry(MeshGeometry3D mesh, Transform3D tx)
        {
            MeshOperations.RemoveNullFields(mesh);

            if (mesh.TriangleIndices.Count == 0)
            {
                // Having triangle indices in a mesh isn't required
                // Generate them if they don't exist
                MeshOperations.GenerateTriangleIndices(mesh);
            }
            else
            {
                // If we didn't generate them, there could be bad indices in there.
                // Remove the triangles that would cause our renderer problems in the future.
                MeshOperations.RemoveBogusTriangles(mesh);
            }

            // Having texture coordinates in a mesh isn't required
            // Generate them if they don't exist
            if (mesh.TextureCoordinates.Count == 0)
            {
                MeshOperations.GenerateTextureCoordinates(mesh);
            }

            // We need explicit normal information, so calculate them when they're missing
            if (mesh.Normals.Count != mesh.Positions.Count)
            {
                // We default to counter-clockwise winding order ...
                MeshOperations.GenerateNormals(mesh, false);
            }

            Point3DCollection positions = mesh.Positions;
            Vector3DCollection normals = mesh.Normals;
            PointCollection textureCoordinates = mesh.TextureCoordinates;
            Int32Collection triangleIndices = mesh.TriangleIndices;

            // We think of the following coordinate systems:
            // Model space: Coordinates which are model-local
            // World space: Coordinates which relate all the models
            // Eye space: Coordinates where the eye point is (0,0,0) and is looking down -Z
            // Homogeneous space: Projected to a canonical rectangular solid view volume. (-1, -1, 0) -> (1, 1, 1)
            // Screen space: Scaled homogeneous space so that X,Y correspond to X,Y pixel values on the screen.
            Matrix3D modelToWorld = tx.Value;
            Matrix3D worldToEye = MatrixUtils.ViewMatrix(camera);
            Matrix3D modelToEyeMatrix = modelToWorld * worldToEye;
            Matrix3D normalTransform = MatrixUtils.MakeNormalTransform(modelToEyeMatrix);
            Matrix3D projectionMatrix = MatrixUtils.ProjectionMatrix(camera);
            Matrix3D toScreenSpace = MatrixUtils.HomogenousToScreenMatrix(bounds.ViewportBounds, camera is ProjectionCamera);
            Matrix3D projectToViewport = projectionMatrix * toScreenSpace;

            bool reverseWinding = MatrixUtils.Determinant(modelToEyeMatrix) < 0;
            MeshProjectedGeometry pg = new MeshProjectedGeometry(projectToViewport);
            int numTriangles = triangleIndices.Count / 3;

            Vertex v1, v2, v3;
            int index;
            for (int n = 0; n < numTriangles; n++)
            {
                v1 = new Vertex();
                v2 = new Vertex();
                v3 = new Vertex();

                // We default material colors to pure White (0xff,0xff,0xff) since
                // we will iluminate against that and then modulate with
                // the actual textures on a per-pixel level.
                v1.Color = Colors.White;
                v2.Color = Colors.White;
                v3.Color = Colors.White;

                index = triangleIndices[n * 3];
                v1.ModelSpacePosition = positions[index];
                v1.ModelSpaceNormal = normals[index];
                v1.Position = MatrixUtils.Transform((Point4D)positions[index], modelToEyeMatrix);
                v1.Normal = MatrixUtils.Transform(normals[index], normalTransform);
                v1.TextureCoordinates = textureCoordinates[index];

                index = triangleIndices[n * 3 + 1];
                v2.ModelSpacePosition = positions[index];
                v2.ModelSpaceNormal = normals[index];
                v2.Position = MatrixUtils.Transform((Point4D)positions[index], modelToEyeMatrix);
                v2.Normal = MatrixUtils.Transform(normals[index], normalTransform);
                v2.TextureCoordinates = textureCoordinates[index];

                index = triangleIndices[n * 3 + 2];
                v3.ModelSpacePosition = positions[index];
                v3.ModelSpaceNormal = normals[index];
                v3.Position = MatrixUtils.Transform((Point4D)positions[index], modelToEyeMatrix);
                v3.Normal = MatrixUtils.Transform(normals[index], normalTransform);
                v3.TextureCoordinates = textureCoordinates[index];

                if (reverseWinding)
                {
                    // Change winding-order so the mesh renders
                    pg.AddTriangle(v1, v3, v2);
                }
                else
                {
                    pg.AddTriangle(v1, v2, v3);
                }
            }

            pg.NormalizeTextureCoordinates();
            return pg;
        }

        internal MeshGeometry3D ExtractTrianglesAtPoints(
                    MeshGeometry3D mesh,
                    Transform3D tx,
                    Point[] selectionPoints)
        {
            ProjectedGeometry pg = MeshToProjectedGeometry(mesh, tx);
            MeshGeometry3D outputMesh = new MeshGeometry3D();

            foreach (Triangle t in pg.FrontFaceTriangles)
            {
                foreach (Point p in selectionPoints)
                {
                    if (t.Bounds.Contains(p.X + pixelCenterX, p.Y + pixelCenterY)
                            && t.Contains(p.X + pixelCenterX, p.Y + pixelCenterY))
                    {
                        t.SaveToMesh(outputMesh);
                        // saving the triangle once is enough
                        break;
                    }
                }
            }
            foreach (Triangle t in pg.BackFaceTriangles)
            {
                foreach (Point p in selectionPoints)
                {
                    if (t.Bounds.Contains(p.X + pixelCenterX, p.Y + pixelCenterY)
                            && t.Contains(p.X + pixelCenterX, p.Y + pixelCenterY))
                    {
                        // Back facing ... flip normals
                        t.vertex1.Normal *= -1;
                        t.vertex2.Normal *= -1;
                        t.vertex3.Normal *= -1;
                        t.SaveToMesh(outputMesh);
                        // saving the triangle once is enough
                        break;
                    }
                }
            }
            return outputMesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public DepthTestFunction DepthTest { set { depthTest = value; } }

        private Bounds bounds;
        private Camera camera;
        private Light[] lights;
        private DepthTestFunction depthTest;
        private InterpolationMode interpolation;

        private RenderBuffer buffer;

        private const double pixelCenterX = 0.5;
        private const double pixelCenterY = 0.5;
    }
}
