// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of Pen.
//              Pen is is the class which describes how to stroke a geometric
//              area.
//
//

using MS.Internal;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using System.Security;

namespace System.Windows.Media
{
    /// <summary>
    /// Pen - The pen class is used to describe how a shape is stroked.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public sealed partial class Pen : Animatable, DUCE.IResource
    {
        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public Pen()
        {
        }

        /// <summary>
        /// Pen - Initializes the pen from the given Brush and thickness.
        /// All other values as set to default.
        /// </summary>
        /// <param name="brush"> The Brush for this Pen. </param>
        /// <param name="thickness"> The thickness of the Pen. </param>
        public Pen(Brush brush,
                   double thickness)
        {
            Brush = brush;
            Thickness = thickness;
        }

        /// <summary>
        /// Pen - Initializes the brush from the parameters.
        /// </summary>
        /// <param name="brush"> The Pen's Brush. </param>
        /// <param name="thickness"> The Pen's thickness. </param>
        /// <param name="startLineCap"> The PenLineCap which applies to the start of the stroke. </param>
        /// <param name="endLineCap"> The PenLineCap which applies to the end of the stroke. </param>
        /// <param name="dashCap"> The PenDashCap which applies to the ends of each dash. </param>
        /// <param name="lineJoin"> The PenLineJoin. </param>
        /// <param name="miterLimit"> The miter limit. </param>
        /// <param name="dashStyle"> The dash style. </param>
        internal Pen(
            Brush brush,
            double thickness,
            PenLineCap startLineCap,
            PenLineCap endLineCap,
            PenLineCap dashCap,
            PenLineJoin lineJoin,
            double miterLimit,
            DashStyle dashStyle)
        {
            Thickness = thickness;
            StartLineCap = startLineCap;
            EndLineCap = endLineCap;
            DashCap = dashCap;
            LineJoin = lineJoin;
            MiterLimit = miterLimit;

            Brush = brush;
            DashStyle = dashStyle;
        }

        #endregion Constructors

        private MIL_PEN_CAP GetInternalCapType(PenLineCap cap)
        {
            Debug.Assert((MIL_PEN_CAP)PenLineCap.Flat == MIL_PEN_CAP.MilPenCapFlat);
            Debug.Assert((MIL_PEN_CAP)PenLineCap.Square == MIL_PEN_CAP.MilPenCapSquare);
            Debug.Assert((MIL_PEN_CAP)PenLineCap.Round == MIL_PEN_CAP.MilPenCapRound);
            Debug.Assert((MIL_PEN_CAP)PenLineCap.Triangle == MIL_PEN_CAP.MilPenCapTriangle);

            return (MIL_PEN_CAP)cap;
        }

        private MIL_PEN_JOIN GetInternalJoinType(PenLineJoin join)
        {
            Debug.Assert((MIL_PEN_JOIN)PenLineJoin.Miter == MIL_PEN_JOIN.MilPenJoinMiter);
            Debug.Assert((MIL_PEN_JOIN)PenLineJoin.Bevel == MIL_PEN_JOIN.MilPenJoinBevel);
            Debug.Assert((MIL_PEN_JOIN)PenLineJoin.Round == MIL_PEN_JOIN.MilPenJoinRound);

            return (MIL_PEN_JOIN)join;
        }

        /// <summary>
        /// Returns a packed structure of non-animate pen values.  If a property is animated, it
        /// uses the instantaneous value of the property.
        /// </summary>
        internal unsafe void GetBasicPenData(MIL_PEN_DATA* pData, out double[] dashArray)
        {
            dashArray = null;
            Invariant.Assert(pData!=null);
            unsafe
            {
                pData->Thickness = Thickness;
                pData->StartLineCap = GetInternalCapType(StartLineCap);
                pData->EndLineCap = GetInternalCapType(EndLineCap);
                pData->DashCap = GetInternalCapType(DashCap);
                pData->LineJoin = GetInternalJoinType(LineJoin);
                pData->MiterLimit = MiterLimit;
            }

            if (DashStyle != null)
            {
                DashStyle.GetDashData(pData, out dashArray);
            }
        }
 
        internal bool DoesNotContainGaps
        {
            get
            {
                DashStyle style = DashStyle;

                if (style != null)
                {
                    DoubleCollection dashes = style.Dashes;

                    if ((dashes != null) && (dashes.Count > 0))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal static bool ContributesToBounds(
            Pen pen)
        {
            return (pen != null) && (pen.Brush != null);
        }
}
}
