// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region usings
    using System;
    using System.IO;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.Serialization;
    #endregion usings

    /// <summary>
    /// RemoteImageUtility is Remotable Object which can exist on different machines and
    /// provides screen capture functions
    /// </summary>
    internal class ImageUtilityProxy : MarshalByRefObject
    {
        /// <summary>
        /// Empty Constructor
        /// </summary>
        internal ImageUtilityProxy(IntPtr window)
        {
            hwnd = window;
        }

        /// <summary>
        /// Returns a bitmap of the screen
        /// </summary>
        /// <param name="rectangle">Position of the Screen to take the Capture of</param>
        /// <returns>Returns the Bitmap of the screen</returns>
        public SerializableBitmap CaptureScreen(Rectangle rectangle)
        {
            Bitmap clientarea = ImageUtility.CaptureScreen(hwnd, true,false);
            Bitmap bmp = ImageUtility.ClipBitmap(clientarea, rectangle);

            return new SerializableBitmap(bmp);
        }

        internal IntPtr hwnd;
    }

    /// <summary>
    /// Bitmap Wrapper that can be Serializable
    /// </summary>
    [Serializable]
    internal class SerializableBitmap : ISerializable
    {
        private Bitmap bitmap;

        /// <summary>
        /// Constructor that takes in a Bitmap to Host
        /// </summary>
        /// <param name="bmp"></param>
        internal SerializableBitmap(Bitmap bmp)
        {
            bitmap = bmp;
        }

        /// <summary>
        /// Gets the hosted Bitmap
        /// </summary>
        internal Bitmap Bitmap
        {
            get
            {
                return bitmap;
            }
        }

        #region ISerializable Implementation

        internal SerializableBitmap(SerializationInfo info, StreamingContext context)
        {
            byte[] bitmapBuffer = (byte[])info.GetValue("BitmapBuffer",typeof(byte[]));
            MemoryStream ms = new MemoryStream(bitmapBuffer);
            bitmap = new Bitmap(ms);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            info.AddValue("BitmapBuffer",ms.GetBuffer(),typeof(byte[]));
        }

        #endregion

    }

}
