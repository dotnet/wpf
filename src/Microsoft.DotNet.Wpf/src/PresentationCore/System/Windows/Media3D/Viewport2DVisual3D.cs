// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using MS.Internal;
using MS.Internal.Media;
using MS.Internal.Media3D;
using MS.Internal.PresentationCore;
using MS.Internal.KnownBoxes;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Documents;
using System.Collections;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// The Viewport2DVisual3D class represents the link from 3D back to 2D, just as
    /// Viewport3DVisual represents the link from 2D in to 3D.
    /// </summary>
    [ContentProperty("Visual")]
    public sealed class Viewport2DVisual3D : Visual3D
    {
        /// <summary>
        /// Constructs a new Viewport2DVisual3D
        /// </summary>
        public Viewport2DVisual3D()
        {
            _visualBrush = CreateVisualBrush();
            _bitmapCacheBrush = CreateBitmapCacheBrush();


            // create holders for the content
            // We don't want this model to set itself as the IC for Geometry and Material 
            // so we set to to not be able to be an inheritance context.
            GeometryModel3D model = new GeometryModel3D();
            model.CanBeInheritanceContext = false;
            
            Visual3DModel = model; 
        } 
        
        internal static bool Get3DPointFor2DCoordinate(Point point, 
                                                       out Point3D point3D,
                                                       Point3DCollection positions,
                                                       PointCollection textureCoords,
                                                       Int32Collection triIndices)
        {
            point3D = new Point3D();
            
            // walk through the triangles - and look for the triangles we care about
            Point3D[] p = new Point3D[3];
            Point[] uv = new Point[3];

            if (positions != null && textureCoords != null)
            {
                if (triIndices == null || triIndices.Count == 0)
                {
                    int texCoordCount = textureCoords.Count;
                
                    // in this case we have a non-indexed mesh
                    int count = positions.Count;
                    count = count - (count % 3);

                    for (int i = 0; i < count; i+=3)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            p[j] = positions[i + j];

                            if (i + j < texCoordCount)
                            {
                                uv[j] = textureCoords[i + j];
                            }
                            else
                            {
                                // In the case you have less texture coordinates than positions, MIL will set
                                // missing ones to be 0,0.  We do the same to stay consistent. 
                                // See CMILMesh3D::CopyTextureCoordinatesFromDoubles
                                uv[j] = new Point(0, 0);
                            }
                        }

                        if (M3DUtil.IsPointInTriangle(point, uv, p, out point3D))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    // in this case we have an indexed mesh
                    int posLimit = positions.Count;
                    int texCoordLimit = textureCoords.Count;
                    
                    for (int i = 2, count=triIndices.Count; i < count; i += 3)
                    {
                        bool validTextureCoordinates = true;
                        for (int j = 0; j < 3; j++)
                        {
                            // subtract 2 to take in to account we start i
                            // at the high range of indices                        
                            int index = triIndices[(i-2) + j];

                            // if a point or texture coordinate is out of range, end early since this is an error
                            if (index < 0 || index >= posLimit)
                            {
                                // no need to look anymore - see MeshGeometry3D RayHitTestIndexedList for 
                                // reasoning why we stop
                                return false; 

                            }
                            if (index < 0 || index >= texCoordLimit)
                            {
                                validTextureCoordinates = false;
                                break;
                            }
                            
                            p[j] = positions[index];
                            uv[j] = textureCoords[index];
                        }

                        if (validTextureCoordinates)
                        {
                            if (M3DUtil.IsPointInTriangle(point, uv, p, out point3D))
                            {
                                return true;
                            }
                        }
                    }                    
                }
            }

            return false;
        }
        
        /// <summary>
        /// Converts a point given in texture coordinates to the corresponding
        /// 2D point on the UIElement passed in.
        /// </summary>
        /// <param name="uv">The texture coordinate to convert</param>
        /// <param name="visual">The UIElement whose coordinate system is to be used</param>
        /// <returns>
        /// The 2D point on the passed in UIElement cooresponding to the
        /// passed in texture coordinate. 
        /// </returns>
        internal static Point TextureCoordsToVisualCoords(Point uv, Visual visual)
        {
            return TextureCoordsToVisualCoords(uv, visual.CalculateSubgraphRenderBoundsOuterSpace());           
        }

        // same as the above except we now take the rectangle giving the bounds of the visual
        // rather than the visual itself
        internal static Point TextureCoordsToVisualCoords(Point uv, Rect descBounds)
        {
            return new Point(uv.X * descBounds.Width + descBounds.Left,
                             uv.Y * descBounds.Height + descBounds.Top);                             
        }

        /// <summary>
        /// Returns true and the intersection point for the given rayHitResult if there is an intersection,
        /// and false otherwise.
        /// </summary>
        /// <param name="rayHitResult"></param>
        /// <param name="outputPoint">The output point if there was an intersection</param>
        /// <returns>
        /// Returns the point of intersection in outputPoint if there is one, and returns true
        /// to indicate this.
        /// </returns>
        internal static bool GetIntersectionInfo(RayHitTestResult rayHitResult, out Point outputPoint)
        {
            bool success = false;
            outputPoint = new Point();

            // try to cast to a RaymeshGeometry3DHitTestResult
            RayMeshGeometry3DHitTestResult rayMeshResult = rayHitResult as RayMeshGeometry3DHitTestResult;
            if (rayMeshResult != null)
            {
                // we can now extract the mesh and visual for the object we hit
                MeshGeometry3D geom = rayMeshResult.MeshHit;

                // pull the barycentric coordinates of the intersection point
                double vertexWeight1 = rayMeshResult.VertexWeight1;
                double vertexWeight2 = rayMeshResult.VertexWeight2;
                double vertexWeight3 = rayMeshResult.VertexWeight3;

                // the indices in to where the actual intersection occurred
                int index1 = rayMeshResult.VertexIndex1;
                int index2 = rayMeshResult.VertexIndex2;
                int index3 = rayMeshResult.VertexIndex3;

                PointCollection textureCoordinates = geom.TextureCoordinates;

                // texture coordinates of the three vertices hit
                // in the case that no texture coordinates are supplied we will simply
                // treat it as if no intersection occurred
                if (textureCoordinates != null &&
                    index1 < textureCoordinates.Count &&
                    index2 < textureCoordinates.Count &&
                    index3 < textureCoordinates.Count)
                {
                    Point texCoord1 = textureCoordinates[index1];
                    Point texCoord2 = textureCoordinates[index2];
                    Point texCoord3 = textureCoordinates[index3];

                    // get the final uv values based on the barycentric coordinates
                    outputPoint = new Point(texCoord1.X * vertexWeight1 +
                                            texCoord2.X * vertexWeight2 +
                                            texCoord3.X * vertexWeight3,
                                            texCoord1.Y * vertexWeight1 +
                                            texCoord2.Y * vertexWeight2 +
                                            texCoord3.Y * vertexWeight3);
                    success = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Converts a point on the passed in UIElement to the corresponding
        /// texture coordinate for that point.  The function assumes (0, 0)
        /// is the upper-left texture coordinate and (1,1) is the lower-right.
        /// </summary>
        /// <param name="pt">The 2D point on the passed in UIElement to convert</param>
        /// <param name="visual">The UIElement whose coordinate system is being used</param>
        /// <returns>
        /// The texture coordinate corresponding to the 2D point on the passed in UIElement
        /// </returns>
        internal static Point VisualCoordsToTextureCoords(Point pt, Visual visual)
        {
            return VisualCoordsToTextureCoords(pt, visual.CalculateSubgraphRenderBoundsOuterSpace()); 
        }        

        // same as the above except we now take the rectangle giving the bounds of the visual
        // rather than the visual itself
        internal static Point VisualCoordsToTextureCoords(Point pt, Rect descBounds)
        {            
            return new Point((pt.X - descBounds.Left) / (descBounds.Right - descBounds.Left),
                             (pt.Y - descBounds.Top) / (descBounds.Bottom - descBounds.Top));
        }

        /// <summary>
        /// GenerateMaterial creates the material for the InteractiveModelVisual3D.  The
        /// material is composed of the Visual, which is displayed on a VisualBrush on a 
        /// DiffuseMaterial, as well as any post materials which are also applied.
        /// </summary>
        private void GenerateMaterial()
        {
            Material material = Material;

            // We clone the material so that we can modify parts of it without affecting the
            // original material that it came from.
            if (material != null)
            {
                material = material.CloneCurrentValue();
            }

            ((GeometryModel3D)Visual3DModel).Material = material;
            
            if (material != null)
            {
                SwapInCyclicBrush(material);
            }       
        }
        
        /// <summary>
        /// The visual applied to the VisualBrush, which is then used on the 3D object.
        /// 
        /// We AddOwner this property to get the same special treatment as VisualBrush's VisualProperty
        /// gets in InheritanceContext linkups and because both properties are used to describe the 
        /// Visual content of the owner.
        ///
        /// </summary>
        public static readonly DependencyProperty VisualProperty =
            VisualBrush.VisualProperty.AddOwner(
                            typeof(Viewport2DVisual3D),
                            new PropertyMetadata(null, new PropertyChangedCallback(OnVisualChanged)));

        /// <summary>
        /// </summary>        
        public Visual Visual
        {
            get { return (Visual)GetValue(VisualProperty); }
            set { SetValue(VisualProperty, value); }
        }

        /// <summary>
        /// The visual brush that the internal visual is contained on.
        /// </summary>
        private VisualBrush InternalVisualBrush
        {
            get { return _visualBrush; }
            set { _visualBrush = value; }
        }

        private BitmapCacheBrush InternalBitmapCacheBrush
        {
            get { return _bitmapCacheBrush; }
            set { _bitmapCacheBrush = value; }
        }
        
        internal static void OnVisualChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            Viewport2DVisual3D viewport2DVisual3D = ((Viewport2DVisual3D)sender);

            // remove the old parent, add on a new one
            Visual oldValue = (Visual)e.OldValue;
            Visual newValue = (Visual)e.NewValue;

            if (oldValue != newValue)
            {   
                //
                // The following code deals with properly setting up the new child to have its inheritance context
                // only point towards this Viewport2DVisual3D.
                //
                // When we add the new visual as a child, if that visual is an FE (which most will be) we expect it to
                // clear the inheritance context (IC) since it has a visual parent.  In the case of a non-FE they don't
                // deal with ICs anyway, so they should have a null IC.  The Assert that follows then guards against 
                // the child not having a null inheritance context.
                //
                // We then set the target Visual on the internal brush to be this new visual.  Since when we created
                // the brush we set it to not be an inheritance context, the InheritanceContext should still be null.
                //
                // We become the IC after returning from this function, since the function that calls this change handler
                // will set us as the IC.
                //

                if (viewport2DVisual3D.CacheMode as BitmapCache != null)
                {
                    viewport2DVisual3D.InternalBitmapCacheBrush.Target = newValue;
                    Debug.Assert((newValue == null || newValue.InheritanceContext == null), 
                                 "Expected BitmapCacheBrush to remove the InheritanceContext on newValue");
                }
                else
                {
                    // Add ourselves as the parent of the object
                    viewport2DVisual3D.RemoveVisualChild(oldValue);
                    viewport2DVisual3D.AddVisualChild(newValue);

                    Debug.Assert((newValue == null || newValue.InheritanceContext == null), 
                                 "Expected AddVisualChild to remove the InheritanceContext on newValue");

                    // Change the brush's target
                    viewport2DVisual3D.InternalVisualBrush.Visual = newValue;

                    // setting the visual brush to use this new child should not invalidate our previous condition
                    Debug.Assert((newValue == null || newValue.InheritanceContext == null), 
                                 "Expected the InternalVisualBrush not to set the InheritanceContext");
                }
            }
        }

        /// <summary>
        /// AttachChild
        ///
        ///    This method is called to add a 2D Visual child to the Viewport2DVisual3D
        ///
        /// </summary>
        private void AddVisualChild(Visual child)
        {
            if (child == null)
            {
                return;
            }

            if (child._parent != null)
            {
                throw new ArgumentException(SR.Get(SRID.Visual_HasParent));
            }

            // Set the parent pointer.

            child._parent = this;

            // NOTE: Since the 2D object is on a VisualBrush, it will allow it to handle
            // the dirtyness of the 2D object, realization information, as well as layout.  See
            // Visual(3D).AddVisualChild for the things they propagate on adding a new child
            
            // Fire notifications
            this.OnVisualChildrenChanged(child, null /* no removed child */);
            child.FireOnVisualParentChanged(null);
        }

        /// <summary>
        /// DisconnectChild
        ///
        ///    This method is called to remove the 2D visual child of the Viewport2DVisual3D
        ///
        /// </summary>
        private void RemoveVisualChild(Visual child)
        {
            if (child == null || child._parent == null)
            {
                return;
            }

            if (child._parent != this)
            {
                throw new ArgumentException(SR.Get(SRID.Visual_NotChild));
            }

            // NOTE: We'll let the VisualBrush handle final cleanup from the channel
            //
           
            child._parent = null;

            // NOTE: We also let the VisualBrush handle any flag propagation issues (so Visual(3D).RemoveVisualChild for
            //       the things they propagate) as well as layout.
            
            // Fire notifications
            child.FireOnVisualParentChanged(this);
            OnVisualChildrenChanged(null /* no child added */, child);
        }

        /// <summary>
        /// Creates the VisualBrush that will be used to hold the interactive
        /// 2D content.
        /// </summary>
        /// <returns>The VisualBrush to hold the interactive 2D content</returns>
        private VisualBrush CreateVisualBrush()
        {
            VisualBrush vb = new VisualBrush();

            // We don't want the VisualBrush being the InheritanceContext for the Visual it contains.  Rather we want
            // that to be the Viewport2DVisual3D itself.
            vb.CanBeInheritanceContext = false;
            
            vb.ViewportUnits = BrushMappingMode.Absolute;
            vb.TileMode = TileMode.None;            

            // set any rendering options in the visual brush - we do this to still give access to these caching hints
            // without exposing the visual brush
            RenderOptions.SetCachingHint(vb, (CachingHint)GetValue(CachingHintProperty));
            RenderOptions.SetCacheInvalidationThresholdMinimum(vb, (double)GetValue(CacheInvalidationThresholdMinimumProperty));
            RenderOptions.SetCacheInvalidationThresholdMaximum(vb, (double)GetValue(CacheInvalidationThresholdMaximumProperty));
            
            return vb;
        }
        
        /// <summary>
        /// Creates the BitmapCacheBrush that will be used to hold the interactive
        /// 2D content.
        /// </summary>
        /// <returns>The BitmapCacheBrush to hold the interactive 2D content</returns>
        private BitmapCacheBrush CreateBitmapCacheBrush()
        {
            BitmapCacheBrush bcb = new BitmapCacheBrush();

            // We don't want the cache brush being the InheritanceContext for the Visual it contains.  Rather we want
            // that to be the Viewport2DVisual3D itself.
            bcb.CanBeInheritanceContext = false;

            // Ensure that the brush supports rendering all properties on the Visual to match VisualBrush behavior.
            bcb.AutoWrapTarget = true;
            
            bcb.BitmapCache = CacheMode as BitmapCache;
            return bcb;
        }
        
        /// <summary>
        /// Replaces any instances of the sentinal brush with the internal brush
        /// </summary>
        /// <param name="material">The material to look through</param>
        private void SwapInCyclicBrush(Material material)
        {
            int numMaterialsSwapped = 0;
            Stack<Material> materialStack = new Stack<Material>();
            materialStack.Push(material);

            Brush internalBrush = (CacheMode as BitmapCache != null) ? (Brush)InternalBitmapCacheBrush : (Brush)InternalVisualBrush;
            
            while (materialStack.Count > 0)
            {
                Material currMaterial = materialStack.Pop();

                if (currMaterial is DiffuseMaterial)
                {
                    DiffuseMaterial diffMaterial = (DiffuseMaterial)currMaterial;
                    if ((Boolean)diffMaterial.GetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty))
                    {
                        diffMaterial.Brush = internalBrush;
                        numMaterialsSwapped++;
                    }
                }
                else if (currMaterial is EmissiveMaterial)
                {
                    EmissiveMaterial emmMaterial = (EmissiveMaterial)currMaterial;
                    if ((Boolean)emmMaterial.GetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty))
                    {
                        emmMaterial.Brush = internalBrush;
                        numMaterialsSwapped++;
                    }
                }
                else if (currMaterial is SpecularMaterial)
                {
                    SpecularMaterial specMaterial = (SpecularMaterial)currMaterial;
                    if ((Boolean)specMaterial.GetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty))
                    {
                        specMaterial.Brush = internalBrush;
                        numMaterialsSwapped++;
                    }
                }
                else if (currMaterial is MaterialGroup)
                {
                    MaterialGroup matGroup = (MaterialGroup)currMaterial;

                    // the IsVisualHostMaterialProperty should not be set on a MaterialGroup - verify that
                    if ((Boolean)matGroup.GetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty))
                    {
                        throw new ArgumentException(SR.Get(SRID.Viewport2DVisual3D_MaterialGroupIsInteractiveMaterial), "material");
                    }

                    // iterate over the children and put them on the stack of materials to modify
                    MaterialCollection children = matGroup.Children;
                    
                    if (children != null)
                    {
                        for (int i=0, count = children.Count; i < count; i++)
                        {
                            Material m = children[i];
                            materialStack.Push(m);
                        }
                    }
                }
                else
                {
                    Invariant.Assert(true, "Unexpected Material type encountered.  V2DV3D handles DiffuseMaterial, EmissiveMaterial, SpecularMaterial, and MaterialGroup.");
                }
            }

            // throw if there is more than 1 interactive material
            if (numMaterialsSwapped > 1)
            {
                throw new ArgumentException(SR.Get(SRID.Viewport2DVisual3D_MultipleInteractiveMaterials), "material");
            }
        }
       
        /// <summary>
        /// The 3D geometry that the InteractiveModelVisual3D represents
        /// </summary>
        public static readonly DependencyProperty GeometryProperty =
            DependencyProperty.Register(
                "Geometry",
                typeof(Geometry3D),
                typeof(Viewport2DVisual3D),
                new PropertyMetadata(null, new PropertyChangedCallback(OnGeometryChanged)));

        /// <summary>
        /// </summary>        
        public Geometry3D Geometry
        {
            get { return (Geometry3D)GetValue(GeometryProperty); }
            set { SetValue(GeometryProperty, value); }
        }

        internal static void OnGeometryChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            Viewport2DVisual3D viewport2DVisual3D = ((Viewport2DVisual3D)sender);

            viewport2DVisual3D.InvalidateAllCachedValues();

            if (!e.IsASubPropertyChange)
            {
                ((GeometryModel3D)viewport2DVisual3D.Visual3DModel).Geometry = viewport2DVisual3D.Geometry;
            }
        }

        private void InvalidateAllCachedValues()
        {
            // invalidate all of them
            InternalPositionsCache = null;
            InternalTextureCoordinatesCache = null;
            InternalTriangleIndicesCache = null;
        }

        // the cache of frozen positions for use with the various transforms
        internal Point3DCollection InternalPositionsCache
        {
            get
            {
                if (_positionsCache == null)
                {
                    Debug.Assert(Geometry == null || Geometry is MeshGeometry3D);
                    
                    MeshGeometry3D geometry = Geometry as MeshGeometry3D;
                    if (geometry != null)
                    {
                        _positionsCache = geometry.Positions;
                        if (_positionsCache != null)
                        {
                            _positionsCache = (Point3DCollection)_positionsCache.GetCurrentValueAsFrozen();
                        }
                    }
                }

                return _positionsCache;
            }
            set
            {
                _positionsCache = value;
            }
        }

        // the cache of frozen internal texture coordinates
        internal PointCollection InternalTextureCoordinatesCache
        {
            get
            {
                if (_textureCoordinatesCache == null)
                {
                    Debug.Assert(Geometry == null || Geometry is MeshGeometry3D);
                    
                    MeshGeometry3D geometry = Geometry as MeshGeometry3D;
                    if (geometry != null)
                    {
                        _textureCoordinatesCache= geometry.TextureCoordinates;
                        if (_textureCoordinatesCache != null)
                        {
                            _textureCoordinatesCache = (PointCollection)_textureCoordinatesCache.GetCurrentValueAsFrozen();
                        }
                    }
                }

                return _textureCoordinatesCache;
            }
            set
            {
                _textureCoordinatesCache = value;
            }
        }

        // the cache of frozen internal triangle indices
        internal Int32Collection InternalTriangleIndicesCache
        {
            get
            {
                if (_triangleIndicesCache== null)
                {
                    Debug.Assert(Geometry == null || Geometry is MeshGeometry3D);
                    
                    MeshGeometry3D geometry = Geometry as MeshGeometry3D;
                    if (geometry != null)
                    {
                        _triangleIndicesCache = geometry.TriangleIndices;
                        if (_triangleIndicesCache != null)
                        {
                            _triangleIndicesCache = (Int32Collection)_triangleIndicesCache.GetCurrentValueAsFrozen();
                        }
                    }
                }

                return _triangleIndicesCache;
            }
            set
            {
                _triangleIndicesCache = value;
            }
        }

                
        /// <summary>
        /// The material used to visually represent the Viewport2DVisual3D
        /// </summary>
        public static readonly DependencyProperty MaterialProperty = 
                                            DependencyProperty.Register("Material",
                                                           typeof(Material),
                                                           typeof(Viewport2DVisual3D),
                                                           new PropertyMetadata(null, 
                                                                                new PropertyChangedCallback(OnMaterialPropertyChanged)));

        /// <summary>
        ///     Material for this Viewport2DVisual3D.
        /// </summary>
        public Material Material
        {
            get { return (Material)GetValue(MaterialProperty); }
            set { SetValue(MaterialProperty, value); }
        }

        internal static void OnMaterialPropertyChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            Viewport2DVisual3D viewport2DVisual3D = ((Viewport2DVisual3D)sender);

            viewport2DVisual3D.GenerateMaterial();            
        }

        /// <summary>
        /// The attached dependency property used to indicate whether a material should be made
        /// interactive.
        /// </summary>
        public static readonly DependencyProperty IsVisualHostMaterialProperty =
            DependencyProperty.RegisterAttached(
                "IsVisualHostMaterial",
                typeof(Boolean),
                typeof(Viewport2DVisual3D),
                new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Sets the attached property IsVisualHostMaterial for the given element.
        /// </summary>
        /// <param name="element">The element to which to write the IsVisualHostMaterial attached property.</param>
        /// <param name="value">The value to set</param>
        public static void SetIsVisualHostMaterial(Material element, Boolean value)
        {
            // We should throw ArgumentNullException if element is null.
            element.SetValue(IsVisualHostMaterialProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        /// Reads the attached property IsVisualHostMaterial from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the IsVisualHostMaterial attached property.</param>
        /// <returns>The property's value.</returns>
        public static Boolean GetIsVisualHostMaterial(Material element)
        {

            // We should throw ArgumentNullException if element is null.
            return (bool)element.GetValue(IsVisualHostMaterialProperty);
        }


        public static readonly DependencyProperty CacheModeProperty =
           DependencyProperty.Register(
                "CacheMode",
                typeof(CacheMode),
                typeof(Viewport2DVisual3D),
                new PropertyMetadata(null, new PropertyChangedCallback(OnCacheModeChanged)));


        public CacheMode CacheMode
        {
            get { return (CacheMode)GetValue(CacheModeProperty); }
            set { SetValue(CacheModeProperty, value); }
        }

        internal static void OnCacheModeChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            Viewport2DVisual3D viewport2DVisual3D = ((Viewport2DVisual3D)sender);

            BitmapCache oldValue = (CacheMode)e.OldValue as BitmapCache;
            BitmapCache newValue = (CacheMode)e.NewValue as BitmapCache;

            if (oldValue != newValue)
            {
                viewport2DVisual3D.InternalBitmapCacheBrush.BitmapCache = newValue;

                //
                // The BitmapCacheBrush doesn't point directly at the Visual like the VisualBrush does,
                // since BitmapCacheBrush ignores most properties on a Visual by design.  In order for
                // those properties to be respected to match the internal VisualBrush's behavior, we insert
                // a dummy Visual node between the V2DV3D and its 2D Visual.  We then target the dummy
                // node with the brush instead.
                //
                
                if (oldValue == null)
                {
                    //
                    // If we are swapping from using the VisualBrush to using the BitmapCacheBrush...
                    //

                    // Remove the visual child from the V2DV3D and add the dummy child.
                    viewport2DVisual3D.RemoveVisualChild(viewport2DVisual3D.Visual);
                    viewport2DVisual3D.AddVisualChild(viewport2DVisual3D.InternalBitmapCacheBrush.InternalTarget);

                    Debug.Assert(  (viewport2DVisual3D.InternalBitmapCacheBrush.InternalTarget == null 
                                 || viewport2DVisual3D.InternalBitmapCacheBrush.InternalTarget.InheritanceContext == null), 
                                "Expected AddVisualChild to remove the InheritanceContext on InternalTarget");

                    // Swap the brush pointing to the visual.  The cache brush will re-parent the visual to the dummy.
                    viewport2DVisual3D.InternalVisualBrush.Visual = null;
                    viewport2DVisual3D.InternalBitmapCacheBrush.Target = viewport2DVisual3D.Visual;

                    // setting the cache brush to use this new child should not invalidate our previous condition
                    Debug.Assert(  (viewport2DVisual3D.InternalBitmapCacheBrush.InternalTarget == null 
                                 || viewport2DVisual3D.InternalBitmapCacheBrush.InternalTarget.InheritanceContext == null), 
                                 "Expected the InternalBitmapCacheBrush not to set the InheritanceContext");
                }

                if (newValue == null)
                {
                    //
                    // If we are swapping from using the BitmapCacheBrush to using the VisualBrush...
                    //

                    // Swap the brush pointing to the visual.  The cache brush will remove the dummy as the parent
                    // of the visual.
                    viewport2DVisual3D.InternalBitmapCacheBrush.Target = null;
                    viewport2DVisual3D.InternalVisualBrush.Visual = viewport2DVisual3D.Visual;
                    
                    // Remove the dummy child and re-add the visual as the V2DV3D's child.
                    viewport2DVisual3D.RemoveVisualChild(viewport2DVisual3D.InternalBitmapCacheBrush.InternalTarget);
                    viewport2DVisual3D.AddVisualChild(viewport2DVisual3D.Visual);
                    
                    Debug.Assert((viewport2DVisual3D.Visual == null || viewport2DVisual3D.Visual.InheritanceContext == null), 
                                 "Expected AddVisualChild to remove the InheritanceContext on Visual");
                }

                // If we changed from using one brush to the other we need to regenerate the Material.
                if (oldValue == null || newValue == null)
                {
                    viewport2DVisual3D.GenerateMaterial();
                }
            }
        }
        
        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected override int Visual3DChildrenCount
        {
            get { return 0; }
        }

        /// <summary>
        ///    Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual3D does not have any children.
        ///
        ///  Remark:
        ///       Need to lock down Visual tree during the callbacks.
        ///       During this virtual call it is not valid to modify the Visual tree.
        ///
        ///       It is okay to type this protected API to the 2D Visual.  The only 2D Visual with
        ///       3D childern is the Viewport3DVisual which is sealed.
        /// </summary>
        protected override Visual3D GetVisual3DChild(int index)
        {
           throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
        }

        /// <summary>
        /// Returns the number of children of this object (in most cases this will be
        /// the number of Visuals, but it some cases, Viewport3DVisual for instance,
        /// this is the number of Visual3Ds).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal override int InternalVisual2DOr3DChildrenCount
        {
            get
            {
                // Call the right virtual method.
                return (Visual != null ? 1 : 0);
            }
        }

        /// <summary>
        /// Returns the child at index "index" (in most cases this will be
        /// a Visual, but it some cases, Viewport3DVisual for instance,
        /// this is a Visual3D).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal override DependencyObject InternalGet2DOr3DVisualChild(int index)
        {
            Visual visualChild = Visual;

            if (index != 0 || visualChild == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            
            return visualChild;
        }

        /// <summary>
        /// CachingHintProperty - Hints the rendering engine that rendered content should be cached
        /// when possible.
        /// </summary>
        private static readonly DependencyProperty CachingHintProperty =
            RenderOptions.CachingHintProperty.AddOwner(
                                        typeof(Viewport2DVisual3D), 
                                        new UIPropertyMetadata(
                                            new PropertyChangedCallback(OnCachingHintChanged)));

        private static void OnCachingHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport2DVisual3D viewport2D = (Viewport2DVisual3D) d;

            RenderOptions.SetCachingHint(viewport2D._visualBrush, (CachingHint)e.NewValue);            
        }

        /// <summary>
        /// CacheInvalidationThresholdMinimum - 
        /// </summary>
        private static readonly DependencyProperty CacheInvalidationThresholdMinimumProperty =
            RenderOptions.CacheInvalidationThresholdMinimumProperty.AddOwner(
                                        typeof(Viewport2DVisual3D), 
                                        new UIPropertyMetadata(
                                            new PropertyChangedCallback(OnCacheInvalidationThresholdMinimumChanged)));


        private static void OnCacheInvalidationThresholdMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport2DVisual3D viewport2D = (Viewport2DVisual3D) d;

            RenderOptions.SetCacheInvalidationThresholdMinimum(viewport2D._visualBrush, (double)e.NewValue);            
        }

        /// <summary>
        /// CacheInvalidationThresholdMaximum - 
        /// </summary>
        private static readonly DependencyProperty CacheInvalidationThresholdMaximumProperty =
            RenderOptions.CacheInvalidationThresholdMaximumProperty.AddOwner(
                                        typeof(Viewport2DVisual3D), 
                                        new UIPropertyMetadata(
                                            new PropertyChangedCallback(OnCacheInvalidationThresholdMaximumChanged)));

        private static void OnCacheInvalidationThresholdMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport2DVisual3D viewport2D = (Viewport2DVisual3D) d;

            RenderOptions.SetCacheInvalidationThresholdMaximum(viewport2D._visualBrush, (double)e.NewValue);            
        }
             
        //------------------------------------------------------------------------
        //
        // PRIVATE DATA
        //
        //------------------------------------------------------------------------
        
        // the actual visual that is created        
        private VisualBrush _visualBrush;
        private BitmapCacheBrush _bitmapCacheBrush;

        private Point3DCollection _positionsCache = null;
        private PointCollection _textureCoordinatesCache = null;
        private Int32Collection _triangleIndicesCache = null;
    }
}
