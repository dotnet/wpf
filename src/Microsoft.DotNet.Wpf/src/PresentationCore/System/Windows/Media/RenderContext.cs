// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Accumulates state during a render pass of the scene.
//

namespace System.Windows.Media
{
    using System;
    using System.Windows.Threading;
    
    using System.Collections;
    using System.Diagnostics;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Composition;
    using System.Runtime.InteropServices;
    using MS.Internal;

    /// <summary>
    /// This class accumulates state during a render pass of the scene.
    /// </summary>
    internal sealed class RenderContext
    {
        // --------------------------------------------------------------------
        // 
        //   Internal Constructors
        // 
        // --------------------------------------------------------------------

        #region Internal Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        internal RenderContext()
        {
            // Do nothing.
        }

        #endregion Internal Constructors


        // --------------------------------------------------------------------
        // 
        //   Internal Properties
        // 
        // --------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Returns the current channel.
        /// </summary>
        internal DUCE.Channel Channel
        {
            get { return _channel; }
        }

        /// <summary>
        /// Returns a handle to the root node, which is attached 
        /// directly to a composition target
        /// </summary>
        internal DUCE.ResourceHandle Root
        {
            get { return _root; }
        }

        #endregion Internal Properties


        // --------------------------------------------------------------------
        // 
        //   Internal Methods
        // 
        // --------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Initialize must be called before a frame is rendered.
        /// </summary>
        internal void Initialize(
            DUCE.Channel channel, 
            DUCE.ResourceHandle root)
        {
            Debug.Assert(channel != null);

            _channel = channel;
            _root = root;
        }

        #endregion Internal Methods



        // --------------------------------------------------------------------
        // 
        //   Private Fields
        // 
        // --------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The current channel.
        /// </summary>
        private DUCE.Channel _channel;

        /// <summary>
        /// The root node, attached directly to a composition target.
        /// </summary>
        private DUCE.ResourceHandle _root;

        #endregion Private Fields
    }
}

