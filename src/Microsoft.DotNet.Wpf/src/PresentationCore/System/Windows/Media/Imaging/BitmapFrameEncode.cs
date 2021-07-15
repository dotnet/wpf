// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Collections.ObjectModel;
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

namespace System.Windows.Media.Imaging
{
    #region BitmapFrameEncode

    /// <summary>
    /// BitmapFrameEncode abstract class
    /// </summary>
    internal sealed class BitmapFrameEncode : BitmapFrame
    {
        #region Constructors

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal BitmapFrameEncode(
            BitmapSource source,
            BitmapSource thumbnail,
            BitmapMetadata metadata,
            ReadOnlyCollection<ColorContext> colorContexts
            )
            : base(true)
        {
            _bitmapInit.BeginInit();

            Debug.Assert(source != null);
            _source = source;
            WicSourceHandle = _source.WicSourceHandle;
            IsSourceCached = _source.IsSourceCached;
            _isColorCorrected = _source._isColorCorrected;
            _thumbnail = thumbnail;
            _readOnlycolorContexts = colorContexts;
            InternalMetadata = metadata;
            _syncObject = source.SyncObject;
            _bitmapInit.EndInit();

            FinalizeCreation();
        }

        /// <summary>
        /// Do not allow construction
        /// This will be called for cloning
        /// </summary>
        private BitmapFrameEncode() : base(true)
        {
        }

        #endregion

        #region IUriContext

        /// <summary>
        /// Provides the base uri of the current context.
        /// </summary>
        public override Uri BaseUri
        {
            get
            {
                ReadPreamble();
                return null;
            }
            set
            {
                WritePreamble();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Accesses the Thumbnail property for this BitmapFrameEncode
        /// </summary>
        public override BitmapSource Thumbnail
        {
            get
            {
                ReadPreamble();
                return _thumbnail;
            }
        }

        /// <summary>
        /// Accesses the Metadata property for this BitmapFrameEncode
        /// </summary>
        public override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();
                return InternalMetadata;
            }
        }

        /// <summary>
        /// Accesses the Decoder property for this BitmapFrameEncode
        /// </summary>
        public override BitmapDecoder Decoder
        {
            get
            {
                ReadPreamble();
                return null;
            }
        }

        /// <summary>
        /// Accesses the ColorContext property for this BitmapFrameEncode
        /// </summary>
        public override ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                ReadPreamble();
                return _readOnlycolorContexts;
            }
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// Create an in-place bitmap metadata writer.
        /// </summary>
        public override InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter()
        {
            ReadPreamble();
            return null;
        }

        #endregion

        #region Freezable
        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new BitmapFrameEncode();
        }

        /// <summary>
        /// Copy the fields not covered by DPs.  This is used by 
        /// CloneCore(), CloneCurrentValueCore(), GetAsFrozenCore() and
        /// GetCurrentValueAsFrozenCore().
        /// </summary>
        private void CopyCommon(BitmapFrameEncode sourceBitmapFrameEncode)
        {
            _bitmapInit.BeginInit();

            Debug.Assert(sourceBitmapFrameEncode._source != null);
            _source = sourceBitmapFrameEncode._source;
            _thumbnail = sourceBitmapFrameEncode._thumbnail;
            _readOnlycolorContexts = sourceBitmapFrameEncode.ColorContexts;

            if (sourceBitmapFrameEncode.InternalMetadata != null)
            {
                InternalMetadata = sourceBitmapFrameEncode.InternalMetadata.Clone();
            }

            _bitmapInit.EndInit();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceBitmapFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.CloneCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameEncode);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceBitmapFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameEncode);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceBitmapFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameEncode);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceBitmapFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameEncode);
        }


        #endregion

        #region Internal Properties / Methods

        /// <summary>
        /// Create the unmanaged resources
        /// </summary>
        internal override void FinalizeCreation()
        {
            CreationCompleted = true;
            UpdateCachedSettings();
        }

        /// <summary>
        /// Internally stores the bitmap metadata
        /// </summary>
        internal override BitmapMetadata InternalMetadata
        {
            get
            {
                // Demand Site Of Origin on the URI before usage of metadata.
                CheckIfSiteOfOrigin();

                return _metadata;
            }
            set
            {
                // Demand Site Of Origin on the URI before usage of metadata.
                CheckIfSiteOfOrigin();

                _metadata = value;
            }
        }

        #endregion

        #region Data Members

        /// Source for this Frame
        private BitmapSource _source;

        #endregion
    }

    #endregion // BitmapFrameEncode
}

