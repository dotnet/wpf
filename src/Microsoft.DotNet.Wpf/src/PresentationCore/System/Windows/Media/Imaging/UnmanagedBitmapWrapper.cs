// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//

namespace System.Windows.Media.Imaging
{
    internal sealed class UnmanagedBitmapWrapper : BitmapSource
    {
        public UnmanagedBitmapWrapper(BitmapSourceSafeMILHandle bitmapSource) :
            base(true)
        {            
            _bitmapInit.BeginInit();

            //
            // This constructor is used by BitmapDecoder and BitmapFrameDecode for thumbnails and
            // previews. The bitmapSource parameter comes from BitmapSource.CreateCachedBitmap
            // which already calculated memory pressure, so there's no need to do it here.
            //
            WicSourceHandle = bitmapSource;
            _bitmapInit.EndInit();
            UpdateCachedSettings();
        }

        #region Protected Methods

        internal UnmanagedBitmapWrapper(bool initialize) :
            base(true)        
        {       
            // Call BeginInit and EndInit if initialize is true.
            if (initialize)
            {
                _bitmapInit.BeginInit();
                _bitmapInit.EndInit();
            }
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore()">Freezable.CreateInstanceCore</see>.
        /// </summary>
        protected override Freezable CreateInstanceCore()
        {
            return new UnmanagedBitmapWrapper(false);
        }

        private void CopyCommon(UnmanagedBitmapWrapper sourceBitmap)
        {
            _bitmapInit.BeginInit();
            _bitmapInit.EndInit();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            UnmanagedBitmapWrapper sourceBitmap = (UnmanagedBitmapWrapper)sourceFreezable;
            base.CloneCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            UnmanagedBitmapWrapper sourceBitmap = (UnmanagedBitmapWrapper)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            UnmanagedBitmapWrapper sourceBitmap = (UnmanagedBitmapWrapper)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            UnmanagedBitmapWrapper sourceBitmap = (UnmanagedBitmapWrapper)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        #endregion
    }
}
