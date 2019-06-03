// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implementation of GeometryGroup
//

using System;
using MS.Internal;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Composition;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Markup;
using System.Runtime.InteropServices;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    #region GeometryGroup
    /// <summary>
    /// GeometryGroup
    /// </summary>
    [ContentProperty("Children")]
    public sealed partial class GeometryGroup : Geometry
    {
        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public GeometryGroup()
        {
        }
        #endregion

        #region Overrides
        /// <summary>
        /// GetPathGeometryData - returns a struct which contains this Geometry represented
        /// as a path geometry's serialized format.
        /// </summary>
        internal override PathGeometryData GetPathGeometryData()
        {
            PathGeometry pathGeometry = GetAsPathGeometry();

            return pathGeometry.GetPathGeometryData();            
        }

        internal override PathGeometry GetAsPathGeometry()
        {
            PathGeometry pg = new PathGeometry();
            pg.AddGeometry(this);

            pg.FillRule = FillRule;

            Debug.Assert(pg.CanFreeze);

            return pg;
        }
        
        #endregion

        #region GetPathFigureCollection
        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            // Combine the transform argument with the internal transform
            Transform combined = new MatrixTransform(GetCombinedMatrix(transform));

            PathFigureCollection result = new PathFigureCollection();
            GeometryCollection children = Children;

            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    PathFigureCollection pathFigures = children.Internal_GetItem(i).GetTransformedFigureCollection(combined);
                    if (pathFigures != null)
                    {
                        int count = pathFigures.Count;
                        for (int j = 0; j < count; ++j)
                        {
                            result.Add(pathFigures[j]);
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        #region IsEmpty

        /// <summary>
        /// Returns true if this geometry is empty
        /// </summary>
        public override bool IsEmpty()
        {
            GeometryCollection children = Children;
            if (children == null)
            {
                return true;
            }

            for (int i=0; i<children.Count; i++)
            {
                if (!((Geometry)children[i]).IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        internal override bool IsObviouslyEmpty()
        {
            GeometryCollection children = Children;
            return (children == null) || (children.Count == 0);
        }

        #endregion IsEmpty

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        public override bool MayHaveCurves()
        {
            GeometryCollection children = Children;
            if (children == null)
            {
                return false;
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (((Geometry)children[i]).MayHaveCurves())
                {
                    return true;
                }
            }

            return false;
        }
}
    #endregion
}


