// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using MS.Win32;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods = MS.Win32.PresentationCore.UnsafeNativeMethods;
using System.Windows.Markup;
using System.Net.Cache;

namespace System.Windows.Media.Imaging
{
    #region BitmapFrame

    /// <summary>
    /// BitmapFrame abstract class
    /// </summary>
    public abstract class BitmapFrame : BitmapSource, IUriContext
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        protected BitmapFrame()
        {
        }

        /// <summary>
        /// Internal Constructor
        /// </summary>
        internal BitmapFrame(bool useVirtuals) : base(useVirtuals)
        {
        }

        /// <summary>
        /// Create BitmapFrame from the uri or stream
        /// </summary>
        internal static BitmapFrame CreateFromUriOrStream(
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy
            )
        {
            // Create a decoder and return the first frame
            if (uri != null)
            {
                Debug.Assert((stream == null), "Both stream and uri are non-null");

                BitmapDecoder decoder = BitmapDecoder.CreateFromUriOrStream(
                    baseUri,
                    uri,
                    null,
                    createOptions,
                    cacheOption,
                    uriCachePolicy,
                    true
                    );

                if (decoder.Frames.Count == 0)
                {
                    throw new System.ArgumentException(SR.Get(SRID.Image_NoDecodeFrames), "uri");
                }

                return decoder.Frames[0];
            }
            else
            {
                Debug.Assert((stream != null), "Both stream and uri are null");

                BitmapDecoder decoder = BitmapDecoder.Create(
                    stream,
                    createOptions,
                    cacheOption
                    );

                if (decoder.Frames.Count == 0)
                {
                    throw new System.ArgumentException(SR.Get(SRID.Image_NoDecodeFrames), "stream");
                }

                return decoder.Frames[0];
            }
        }

        /// <summary>
        /// Create a BitmapFrame from a Uri using BitmapCreateOptions.None and
        /// BitmapCacheOption.Default
        /// </summary>
        /// <param name="bitmapUri">Uri of the Bitmap</param>
        public static BitmapFrame Create(
            Uri bitmapUri
            )
        {
            return Create(bitmapUri, null);
        }
        
        /// <summary>
        /// Create a BitmapFrame from a Uri using BitmapCreateOptions.None and
        /// BitmapCacheOption.Default
        /// </summary>
        /// <param name="bitmapUri">Uri of the Bitmap</param>
        /// <param name="uriCachePolicy">Optional web request cache policy</param>
        public static BitmapFrame Create(
            Uri bitmapUri,
            RequestCachePolicy uriCachePolicy
            )
        {
            if (bitmapUri == null)
            {
                throw new ArgumentNullException("bitmapUri");
            }

            return CreateFromUriOrStream(
                null,
                bitmapUri,
                null,
                BitmapCreateOptions.None,
                BitmapCacheOption.Default,
                uriCachePolicy
                );
        }

        /// <summary>
        /// Create a BitmapFrame from a Uri with the specified BitmapCreateOptions and
        /// BitmapCacheOption
        /// </summary>
        /// <param name="bitmapUri">Uri of the Bitmap</param>
        /// <param name="createOptions">Creation options</param>
        /// <param name="cacheOption">Caching option</param>
        public static BitmapFrame Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption
            )
        {
            return Create(bitmapUri, createOptions, cacheOption, null);
        }

        /// <summary>
        /// Create a BitmapFrame from a Uri with the specified BitmapCreateOptions and
        /// BitmapCacheOption
        /// </summary>
        /// <param name="bitmapUri">Uri of the Bitmap</param>
        /// <param name="createOptions">Creation options</param>
        /// <param name="cacheOption">Caching option</param>
        /// <param name="uriCachePolicy">Optional web request cache policy</param>
        public static BitmapFrame Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy
            )
        {
            if (bitmapUri == null)
            {
                throw new ArgumentNullException("bitmapUri");
            }

            return CreateFromUriOrStream(
                null,
                bitmapUri,
                null,
                createOptions,
                cacheOption,
                uriCachePolicy
                );
        }

        /// <summary>
        /// Create a BitmapFrame from a Stream using BitmapCreateOptions.None and
        /// BitmapCacheOption.Default
        /// </summary>
        /// <param name="bitmapStream">Stream of the Bitmap</param>
        public static BitmapFrame Create(
            Stream bitmapStream
            )
        {
            if (bitmapStream == null)
            {
                throw new ArgumentNullException("bitmapStream");
            }

            return CreateFromUriOrStream(
                null,
                null,
                bitmapStream,
                BitmapCreateOptions.None,
                BitmapCacheOption.Default,
                null
                );
        }

        /// <summary>
        /// Create a BitmapFrame from a Stream with the specified BitmapCreateOptions and
        /// BitmapCacheOption
        /// </summary>
        /// <param name="bitmapStream">Stream of the Bitmap</param>
        /// <param name="createOptions">Creation options</param>
        /// <param name="cacheOption">Caching option</param>
        public static BitmapFrame Create(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption
            )
        {
            if (bitmapStream == null)
            {
                throw new ArgumentNullException("bitmapStream");
            }

            return CreateFromUriOrStream(
                null,
                null,
                bitmapStream,
                createOptions,
                cacheOption,
                null
                );
        }

        /// <summary>
        /// Create a BitmapFrame from a BitmapSource
        /// </summary>
        /// <param name="source">Source input of the Bitmap</param>
        public static BitmapFrame Create(
            BitmapSource source
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            BitmapMetadata metadata = null;

            try
            {
                metadata = source.Metadata as BitmapMetadata;
            }
            catch (System.NotSupportedException)
            {
                //do not throw not support exception
                //just pass null
            }

            if (metadata != null)
            {
                metadata = metadata.Clone();
            }

            return new BitmapFrameEncode(source, null, metadata, null);
        }

        /// <summary>
        /// Create a BitmapFrame from a BitmapSource with the specified Thumbnail
        /// </summary>
        /// <param name="source">Source input of the Bitmap</param>
        /// <param name="thumbnail">Thumbnail of the resulting Bitmap</param>
        public static BitmapFrame Create(
            BitmapSource source,
            BitmapSource thumbnail
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            BitmapMetadata metadata = null;

            try
            {
                metadata = source.Metadata as BitmapMetadata;
            }
            catch (System.NotSupportedException)
            {
                //do not throw not support exception
                //just pass null
            }


            if (metadata != null)
            {
                metadata = metadata.Clone();
            }

            return BitmapFrame.Create(source, thumbnail, metadata, null);
        }


        /// <summary>
        /// Create a BitmapFrame from a BitmapSource with the specified Thumbnail and metadata
        /// </summary>
        /// <param name="source">Source input of the Bitmap</param>
        /// <param name="thumbnail">Thumbnail of the resulting Bitmap</param>
        /// <param name="metadata">BitmapMetadata of the resulting Bitmap</param>
        /// <param name="colorContexts">The ColorContexts for the resulting Bitmap</param>
        public static BitmapFrame Create(
            BitmapSource source,
            BitmapSource thumbnail,
            BitmapMetadata metadata,
            ReadOnlyCollection<ColorContext> colorContexts
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return new BitmapFrameEncode(source, thumbnail, metadata, colorContexts);
        }

        #endregion

        #region IUriContext

        /// <summary>
        /// Provides the base uri of the current context.
        /// </summary>
        public abstract Uri BaseUri { get; set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Accesses the Thumbnail property for this BitmapFrame
        /// </summary>
        public abstract BitmapSource Thumbnail { get; }

        /// <summary>
        /// Accesses the Decoder property for this BitmapFrame
        /// </summary>
        public abstract BitmapDecoder Decoder { get; }

        /// <summary>
        /// Accesses the Decoder property for this BitmapFrame
        /// </summary>
        public abstract ReadOnlyCollection<ColorContext> ColorContexts { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Create an in-place bitmap metadata writer.
        /// </summary>
        public abstract InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter();

        #endregion

        #region Internal Properties

        /// <summary>
        /// Returns cached metadata and creates BitmapMetadata if it does not exist.
        /// This code will demand site of origin permissions.
        /// </summary>
        internal virtual BitmapMetadata InternalMetadata
        {
            get { return null; }
            set { throw new NotImplementedException(); }
        }

        #endregion

        #region Data Members

        /// Thumbnail
        internal BitmapSource _thumbnail = null;

        /// <summary>
        /// Metadata
        /// </summary>
        internal BitmapMetadata _metadata;

        /// ColorContexts collection
        internal ReadOnlyCollection<ColorContext> _readOnlycolorContexts;

        #endregion
    }

    #endregion // BitmapFrame
}

