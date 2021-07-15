// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO;
using System.Security;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text;
using MS.Internal.PresentationCore;                        // SecurityHelper

namespace System.Windows.Media.Imaging
{
    #region InPlaceBitmapMetadataWriter

    /// <summary>
    /// Metadata Class for BitmapImage.
    /// </summary>
    sealed public partial class InPlaceBitmapMetadataWriter : BitmapMetadata
    {
        #region Constructors

        /// <summary>
        ///
        /// </summary>
        private InPlaceBitmapMetadataWriter()
        {
        }

        /// <summary>
        ///
        /// </summary>
        internal InPlaceBitmapMetadataWriter(
            SafeMILHandle /* IWICFastMetadataEncoder */ fmeHandle,
            SafeMILHandle /* IWICMetadataQueryWriter */ metadataHandle,
            object syncObject
        ) : base(metadataHandle, false, false, syncObject)
        {
            _fmeHandle = fmeHandle;
        }

        /// <summary>
        ///
        /// </summary>
        static internal InPlaceBitmapMetadataWriter CreateFromFrameDecode(BitmapSourceSafeMILHandle frameHandle, object syncObject)
        {
            Invariant.Assert(frameHandle != null);

            SafeMILHandle /* IWICFastMetadataEncoder */ fmeHandle = null;
            SafeMILHandle /* IWICMetadataQueryWriter */ metadataHandle = null;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                lock (syncObject)
                {
                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateFastMetadataEncoderFromFrameDecode(
                            factoryMaker.ImagingFactoryPtr,
                            frameHandle,
                            out fmeHandle));
                }
            }

            HRESULT.Check(UnsafeNativeMethods.WICFastMetadataEncoder.GetMetadataQueryWriter(
                    fmeHandle,
                    out metadataHandle));

            return new InPlaceBitmapMetadataWriter(fmeHandle, metadataHandle, syncObject);
        }

        /// <summary>
        ///
        /// </summary>
        static internal InPlaceBitmapMetadataWriter CreateFromDecoder(SafeMILHandle decoderHandle, object syncObject)
        {
            Invariant.Assert(decoderHandle != null);

            SafeMILHandle /* IWICFastMetadataEncoder */ fmeHandle = null;
            SafeMILHandle /* IWICMetadataQueryWriter */ metadataHandle = null;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                lock (syncObject)
                {
                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateFastMetadataEncoderFromDecoder(
                            factoryMaker.ImagingFactoryPtr,
                            decoderHandle,
                            out fmeHandle));
                }
            }

            HRESULT.Check(UnsafeNativeMethods.WICFastMetadataEncoder.GetMetadataQueryWriter(
                    fmeHandle,
                    out metadataHandle));

            return new InPlaceBitmapMetadataWriter(fmeHandle, metadataHandle, syncObject);
        }

        /// <summary>
        ///
        /// </summary>
        public bool TrySave()
        {
            int hr;

            Invariant.Assert(_fmeHandle != null);

            lock (SyncObject)
            {
                hr = UnsafeNativeMethods.WICFastMetadataEncoder.Commit(_fmeHandle);
            }

            return HRESULT.Succeeded(hr);
        }

        #endregion

        #region Freezable

        /// <summary>
        ///     Shadows inherited Copy() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new InPlaceBitmapMetadataWriter Clone()
        {
            return (InPlaceBitmapMetadataWriter)base.Clone();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            throw new InvalidOperationException(SR.Get(SRID.Image_InplaceMetadataNoCopy));
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Get(SRID.Image_InplaceMetadataNoCopy));
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Get(SRID.Image_InplaceMetadataNoCopy));
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Get(SRID.Image_InplaceMetadataNoCopy));
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Get(SRID.Image_InplaceMetadataNoCopy));
        }
        #endregion

        private SafeMILHandle _fmeHandle;
    }

    #endregion
}
