// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Diagnostics;
using System.Net.Cache;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(BitmapImage))]
    class BitmapImageFactory : DiscoverableFactory<BitmapImage>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 DecodePixelHeight { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 DecodePixelWidth { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Uri { get; set; }
        public Rotation Rotation { get; set; }

        public override BitmapImage Create(DeterministicRandom random)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = random.NextEnum<BitmapCacheOption>();
            bitmapImage.CreateOptions = random.NextEnum<BitmapCreateOptions>();
            bitmapImage.DecodePixelHeight = DecodePixelHeight;
            bitmapImage.DecodePixelWidth = DecodePixelWidth;
            bitmapImage.UriCachePolicy = new HttpRequestCachePolicy((HttpRequestCacheLevel)random.NextEnum<HttpRequestCacheLevel>());
            bitmapImage.UriSource = Uri;
            bitmapImage.DecodeFailed += new EventHandler<ExceptionEventArgs>(BitmapImage_DecodeFailed);
            bitmapImage.DownloadCompleted += new EventHandler(BitmapImage_DownloadCompleted);
            bitmapImage.DownloadFailed += new EventHandler<ExceptionEventArgs>(BitmapImage_DownloadFailed);
            bitmapImage.Rotation = Rotation;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private void BitmapImage_DecodeFailed(object obj, ExceptionEventArgs args)
        {
            Trace.WriteLine("Image decode failed.");
        }

        private void BitmapImage_DownloadFailed(object obj, ExceptionEventArgs args)
        {
            Trace.WriteLine("Image download failed.");
        }

        private void BitmapImage_DownloadCompleted(object obj, EventArgs args)
        {
            Trace.WriteLine("Image download competed.");
        }
    }
}
