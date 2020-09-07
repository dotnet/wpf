// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

using System;
using System.IO;
using System.IO.Packaging;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Security;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using MS.Internal.PresentationCore;                        // SecurityHelper
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Net;
using System.Net.Cache;

namespace System.Windows.Media.Imaging
{
    #region LateBoundBitmapDecoder

    /// <summary>
    /// LateBoundBitmapDecoder is a container for bitmap frames.  Each bitmap frame is an BitmapFrame.
    /// Any BitmapFrame it returns are frozen
    /// be immutable.
    /// </summary>
    public sealed class LateBoundBitmapDecoder : BitmapDecoder
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        internal LateBoundBitmapDecoder(
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy requestCachePolicy
            )
            : base(true)
        {
            _baseUri = baseUri;
            _uri = uri;
            _stream = stream;
            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _requestCachePolicy = requestCachePolicy;

            // Check to see if we need to download content off thread
            Uri uriToDecode = (_baseUri != null) ? new Uri(_baseUri, _uri) : _uri;
            if (uriToDecode != null)
            {
                if (uriToDecode.Scheme == Uri.UriSchemeHttp ||
                    uriToDecode.Scheme == Uri.UriSchemeHttps)
                {
                    // Begin the download
                    BitmapDownload.BeginDownload(this, uriToDecode, _requestCachePolicy, _stream);
                    _isDownloading = true;
                }
            }

            if (_stream != null && !_stream.CanSeek)
            {
                // Begin the download
                BitmapDownload.BeginDownload(this, uriToDecode, _requestCachePolicy, _stream);
                _isDownloading = true;
            }
}

        #endregion

        #region Properties

        /// <summary>
        /// If there is an palette, return it.
        /// Otherwise, return null.
        /// If the LateBoundDecoder is still downloading, the returned Palette is null.
        /// </summary>
        public override BitmapPalette Palette
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return Decoder.Palette;
            }
        }

        /// <summary>
        /// If there is an embedded color profile, return it.
        /// Otherwise, return null.
        /// If the LateBoundDecoder is still downloading, the returned ColorContext is null.
        /// </summary>
        public override ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return Decoder.ColorContexts;
            }
        }

        /// <summary>
        /// If there is a global thumbnail, return it.
        /// Otherwise, return null. The returned source is frozen.
        /// If the LateBoundDecoder is still downloading, the returned Thumbnail is null.
        /// </summary>
        public override BitmapSource Thumbnail
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return Decoder.Thumbnail;
            }
        }


        /// <summary>
        /// The info that identifies this codec.
        /// If the LateBoundDecoder is still downloading, the returned CodecInfo is null.
        /// </summary>
        public override BitmapCodecInfo CodecInfo
        {
            get
            {
                VerifyAccess();


                if (_isDownloading)
                {
                    return null;
                }

                return Decoder.CodecInfo;
            }
        }

        /// <summary>
        /// Access to the individual frames.
        /// Since a LateBoundBitmapDecoder is downloaded asynchronously,
        /// its possible the underlying frame collection may change once
        /// content has been downloaded and decoded. When content is initially
        /// downloading, the collection will always return at least one item
        /// in the collection. When the download/decode is complete, the BitmapFrame
        /// will automatically change its underlying content. i.e. Only the collection
        /// object may change. The actual frame object will remain the same.
        /// </summary>
        public override ReadOnlyCollection<BitmapFrame> Frames
        {
            get
            {
                VerifyAccess();

                // If the content is still being downloaded, create a collection
                // with 1 item that will point to an empty bitmap
                if (_isDownloading)
                {
                    if (_readOnlyFrames == null)
                    {
                        _frames = new List<BitmapFrame>((int)1);
                        _frames.Add(
                            new BitmapFrameDecode(
                                0,
                                _createOptions,
                                _cacheOption,
                                this
                                )
                            );
                        _readOnlyFrames = new ReadOnlyCollection<BitmapFrame>(_frames);
                    }

                    return _readOnlyFrames;
                }
                else
                {
                    return Decoder.Frames;
                }
            }
        }

        /// <summary>
        /// If there is a global preview image, return it.
        /// Otherwise, return null. The returned source is frozen.
        /// If the LateBoundDecoder is still downloading, the returned Preview is null.
        /// </summary>
        public override BitmapSource Preview
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return Decoder.Preview;
            }
        }

        /// <summary>
        /// Returns the underlying decoder associated with this late bound decoder.
        /// If the LateBoundDecoder is still downloading, the underlying decoder is null,
        /// otherwise the underlying decoder is created on first access.
        /// </summary>
        public BitmapDecoder Decoder
        {
            get
            {
                VerifyAccess();

                if (_isDownloading || _failed)
                {
                    return null;
                }

                EnsureDecoder();
                return _realDecoder;
            }
        }

        /// <summary>
        /// Returns if the decoder is downloading content
        /// </summary>
        public override bool IsDownloading
        {
            get
            {
                VerifyAccess();
                return _isDownloading;
            }
        }

        #endregion

        #region Methods

        ///
        /// Ensure that the underlying decoder is created
        ///
        private void EnsureDecoder()
        {
            if (_realDecoder == null)
            {
                _realDecoder = BitmapDecoder.CreateFromUriOrStream(
                    _baseUri,
                    _uri,
                    _stream,
                    _createOptions & ~BitmapCreateOptions.DelayCreation,
                    _cacheOption,
                    _requestCachePolicy,
                    true
                    );

                // Check to see if someone already got the frames
                // If so, we need to ensure that the real decoder
                // references the same frame as the one we already created
                // Creating a new object would be bad.
                if (_readOnlyFrames != null)
                {
                    _realDecoder.SetupFrames(null, _readOnlyFrames);

                    //
                    // The frames have been transfered to the real decoder, so we no
                    // longer need them.
                    //
                    _readOnlyFrames = null;
                    _frames = null;
                }
            }
        }

        ///
        /// Called when download is complete
        ///
        internal object DownloadCallback(object arg)
        {
            Stream newStream = (Stream)arg;

            // Assert that we are able to seek the new stream
            Debug.Assert(newStream.CanSeek == true);

            _stream = newStream;

            // If we are not supposed to delay create, then ensure the decoder
            // otherwise it will be done on first access
            if ((_createOptions & BitmapCreateOptions.DelayCreation) == 0)
            {
                try
                {
                    EnsureDecoder();
                }
                catch(Exception e)
                {
                    #pragma warning disable 6500

                    return ExceptionCallback(e);

                    #pragma warning restore 6500
                }
            }

            _isDownloading = false;
            _downloadEvent.InvokeEvents(this, null);

            return null;
        }

        ///
        /// Called when download progresses
        ///
        internal object ProgressCallback(object arg)
        {
            int percentComplete = (int)arg;

            _progressEvent.InvokeEvents(this, new DownloadProgressEventArgs(percentComplete));

            return null;
        }

        ///
        /// Called when an exception occurs
        ///
        internal object ExceptionCallback(object arg)
        {
            _isDownloading = false;
            _failed = true;
            _failedEvent.InvokeEvents(this, new ExceptionEventArgs((Exception)arg));

            return null;
        }

        #endregion

        #region Internal Abstract

        /// Need to implement this to derive from the "sealed" object
        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Data Members

        /// Is downloading data
        private bool _isDownloading;

        /// Is downloading data
        private bool _failed;

        /// Real decoder
        private BitmapDecoder _realDecoder;

        /// <summary>
        /// the cache policy to use for web requests.
        /// </summary>
        private RequestCachePolicy _requestCachePolicy;
        #endregion

    }

    #endregion
}

