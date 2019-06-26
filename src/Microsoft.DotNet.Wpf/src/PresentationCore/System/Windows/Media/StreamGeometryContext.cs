// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This class is used by the StreamGeometry class to generate an inlined,
// flattened geometry stream.
//

using MS.Internal;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;


#if !PBTCOMPILER 
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

using MS.Internal.PresentationCore;

namespace System.Windows.Media

#elif PBTCOMPILER 

using MS.Internal.Markup; 


namespace MS.Internal.Markup
#endif 
{
    /// <summary>
    ///     StreamGeometryContext
    /// </summary>    
#if ! PBTCOMPILER 
    public abstract class StreamGeometryContext : DispatcherObject, IDisposable
#else
    internal abstract class StreamGeometryContext : IDisposable
#endif 
    {
        #region Constructors

        /// <summary>
        /// This constructor exists to prevent external derivation
        /// </summary>
        internal StreamGeometryContext()
        {
        }

        #endregion Constructors

        #region IDisposable

        void IDisposable.Dispose()
        {
#if ! PBTCOMPILER 
            VerifyAccess();
#endif 
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region Public Methods
        
        /// <summary>
        /// Closes the StreamContext and flushes the content.
        /// Afterwards the StreamContext can not be used anymore.
        /// This call does not require all Push calls to have been Popped.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        public virtual void Close()
        {
            DisposeCore();
        }


        /// <summary>
        /// BeginFigure - Start a new figure.
        /// </summary>
        public abstract void BeginFigure(Point startPoint, bool isFilled, bool isClosed);
        
        /// <summary>
        /// LineTo - append a LineTo to the current figure.
        /// </summary>
        public abstract void LineTo(Point point, bool isStroked, bool isSmoothJoin);

        /// <summary>
        /// QuadraticBezierTo - append a QuadraticBezierTo to the current figure.
        /// </summary>
        public abstract void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin);
        
        /// <summary>
        /// BezierTo - apply a BezierTo to the current figure.
        /// </summary>
        public abstract void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin);
        
        /// <summary>
        /// PolyLineTo - append a PolyLineTo to the current figure.
        /// </summary>
        public abstract void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin);

        /// <summary>
        /// PolyQuadraticBezierTo - append a PolyQuadraticBezierTo to the current figure.
        /// </summary>
        public abstract void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin);

        /// <summary>
        /// PolyBezierTo - append a PolyBezierTo to the current figure.
        /// </summary>
        public abstract void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin);

        /// <summary>
        /// ArcTo - append an ArcTo to the current figure.
        /// </summary>

        // Special case this one. Bringing in sweep direction requires code-gen changes. 
        // 
#if !PBTCOMPILER        
        public abstract void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin);
#else
        public abstract void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked, bool isSmoothJoin);
#endif

        #endregion Public Methods

        /// <summary>
        /// This is the same as the Close call:
        /// Closes the Context and flushes the content.
        /// Afterwards the Context can not be used anymore.
        /// This call does not require all Push calls to have been Popped.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        internal virtual void DisposeCore() {}

        /// <summary>
        /// SetClosedState - Sets the current closed state of the figure. 
        /// </summary>
        internal abstract void SetClosedState(bool closed);
    }
}
