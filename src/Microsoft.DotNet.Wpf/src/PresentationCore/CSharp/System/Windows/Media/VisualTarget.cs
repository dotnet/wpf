// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//
//

using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Security;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    ///
    /// </summary>
    public class VisualTarget : CompositionTarget
    {
        //----------------------------------------------------------------------
        //
        //  Constructors
        //
        //----------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// VisualTarget
        /// </summary>
        public VisualTarget(HostVisual hostVisual)
        {
            if (hostVisual == null)
            {
                throw new ArgumentNullException("hostVisual");
            }

            _hostVisual = hostVisual;
            _connected = false;
            MediaContext.RegisterICompositionTarget(Dispatcher, this);
        }

        /// <summary>
        /// This function gets called when VisualTarget is created on the channel
        /// i.e. from CreateUCEResources.
        /// </summary>
        private void BeginHosting()
        {
            Debug.Assert(!_connected);
            try
            {
                //
                // Initiate hosting by the specified host visual.
                //
                _hostVisual.BeginHosting(this);
                _connected = true;
            }
            catch
            {
                //
                // If exception has occurred after we have registered with
                // MediaContext, we need to unregister to properly release
                // allocated resources.
                // NOTE: We need to properly unregister in a disconnected state
                //
                MediaContext.UnregisterICompositionTarget(Dispatcher, this);
                throw;
            }
        }


        internal override void CreateUCEResources(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            Debug.Assert(channel != null);
            Debug.Assert(outOfBandChannel != null);

            _outOfBandChannel = outOfBandChannel;

            // create visual target resources
            base.CreateUCEResources(channel, outOfBandChannel);

            // Update state to propagate flags as necessary
            StateChangedCallback(
                new object[]
                {
                    HostStateFlags.None
                });

            //
            // Addref content node on the channel. We need extra reference
            // on that node so that it does not get immediately released
            // when Dispose is called. Actual release of the node needs
            // to be synchronized with node disconnect by the host.
            //

            bool resourceCreated = _contentRoot.CreateOrAddRefOnChannel(this, outOfBandChannel, s_contentRootType);
            Debug.Assert(!resourceCreated);
            _contentRoot.CreateOrAddRefOnChannel(this, channel, s_contentRootType);

            BeginHosting();
        }


        #endregion Constructors

        //----------------------------------------------------------------------
        //
        //  Public Properties
        //
        //----------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from this
        /// target to the rendering destination device.
        /// </summary>
        public override Matrix TransformToDevice
        {
            get
            {
                VerifyAPIReadOnly();
                Matrix m = WorldTransform;
                m.Invert();
                return m;
            }
        }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from
        /// the rendering destination device to this target.
        /// </summary>
        public override Matrix TransformFromDevice
        {
            get
            {
                VerifyAPIReadOnly();
                return WorldTransform;
            }
        }

        #endregion Public Properties

        //----------------------------------------------------------------------
        //
        //  Public Methods
        //
        //----------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Dispose cleans up the state associated with HwndTarget.
        /// </summary>
        public override void Dispose()
        {
            try
            {
                VerifyAccess();

                if (!IsDisposed)
                {
                    if (_hostVisual != null && _connected)
                    {
                        RootVisual = null;

                        //
                        // Unregister this CompositionTarget from the MediaSystem.
                        //
                        // we need to properly unregister in a disconnected state
                        MediaContext.UnregisterICompositionTarget(Dispatcher, this);
                    }
                }
            }
            finally
            {
                base.Dispose();
            }
        }

        /// <summary>
        /// This function gets called when VisualTarget is removed on the channel
        /// i.e. from ReleaseUCEResources.
        /// </summary>
        private void EndHosting()
        {
            Debug.Assert(_connected);
            _hostVisual.EndHosting();
            _connected = false;
        }

        /// <summary>
        /// This method is used to release all uce resources either on Shutdown or session disconnect
        /// </summary>
        internal override void ReleaseUCEResources(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            EndHosting();

            _contentRoot.ReleaseOnChannel(channel);

            if (_contentRoot.IsOnChannel(outOfBandChannel))
            {
                _contentRoot.ReleaseOnChannel(outOfBandChannel);
            }

            base.ReleaseUCEResources(channel, outOfBandChannel);
        }

        #endregion Public Methods

        #region Internal Properties


        /// <summary>
        /// The out of band channel on our the MediaContext this
        /// resource was created.
        /// This is needed by HostVisual for handle duplication.
        /// </summary>
        internal DUCE.Channel OutOfBandChannel
        {
            get
            {
                return _outOfBandChannel;
            }
        }


        #endregion Internal Properties

        //----------------------------------------------------------------------
        //
        //  Private Fields
        //
        //----------------------------------------------------------------------

        #region Private Fields

        DUCE.Channel _outOfBandChannel;
        private HostVisual _hostVisual;

        // Flag indicating whether VisualTarget-HostVisual connection exists.
        private bool _connected;

        #endregion Private Fields
    }
}

