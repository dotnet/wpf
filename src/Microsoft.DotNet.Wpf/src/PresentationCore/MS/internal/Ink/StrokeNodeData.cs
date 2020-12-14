// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;

namespace MS.Internal.Ink
{
    #region StrokeNodeData

    /// <summary>
    /// This structure represents a node on a stroke spine. 
    /// </summary>
    internal readonly struct StrokeNodeData
    {
        #region Statics

        private static readonly StrokeNodeData s_empty = new StrokeNodeData();

        #endregion

        #region API (internal)

        /// <summary> Returns static object representing an unitialized node </summary>
        internal static ref readonly StrokeNodeData Empty { get { return ref s_empty; } }

        /// <summary>
        /// Constructor for nodes of a pressure insensitive stroke
        /// </summary>
        /// <param name="position">position of the node</param>
        internal StrokeNodeData(Point position)
        {
            _position = position;
            _pressure = 1;
        }
      
        /// <summary>
        /// Constructor for nodes with pressure data
        /// </summary>
        /// <param name="position">position of the node</param>
        /// <param name="pressure">pressure scaling factor at the node</param>
        internal StrokeNodeData(Point position, float pressure)
        {
            System.Diagnostics.Debug.Assert(DoubleUtil.GreaterThan((double)pressure, 0d));

            _position = position;
            _pressure = pressure;
        }
      
        /// <summary> Tells whether the structre was properly initialized </summary>
        internal bool IsEmpty 
        { 
            get 
            {
                Debug.Assert(DoubleUtil.AreClose(0, s_empty._pressure));
                return DoubleUtil.AreClose(_pressure, s_empty._pressure); 
            } 
        }
        
        /// <summary> Position of the node </summary>
        internal Point Position 
        { 
            get { return _position; } 
        }

        /// <summary> Pressure scaling factor at the node </summary>
        internal float PressureFactor { get { return _pressure; } }

        #endregion

        #region Privates

        private readonly Point   _position;
        private readonly float _pressure;

        #endregion
    }

    #endregion
}
