// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Drawing;

    internal class AsyncData : IDisposable
    {
        private Bitmap _masterImage;
        private string _masterName;
        private Bitmap _capturedImage;
        private ImageComparisonResult _result;
        private System.Threading.ManualResetEvent _manualResetEvent;
        private ImageComparisonSettings _toleranceSettings;

        public Bitmap CapturedImage
        {
            get { return _capturedImage; }
            internal set { _capturedImage = value; }
        }
        public string MasterName
        {
            get { return _masterName; }
        }
        public Bitmap MasterImage
        {
            get { return _masterImage; }
            internal set { _masterImage = value; }
        }
        public ImageComparisonResult Result
        {
            get { return _result; }
        }
        public int Index 
        {
            get { return _result.Index; }
        }
        public System.Threading.ManualResetEvent SynchronizationObject
        {
            get { return (System.Threading.ManualResetEvent)((IAsyncResult)_result).AsyncWaitHandle; }
        }
        public ImageComparisonSettings ToleranceSettings
        {
            get { return _toleranceSettings; }
            internal set { _toleranceSettings = value; }
        }

        public AsyncData(Bitmap masterImage, string masterName, Bitmap capturedImage, ImageComparisonSettings toleranceSettings, int index) : this(masterImage, masterName, capturedImage, null, toleranceSettings, index)
        {
        }
        public AsyncData(Bitmap masterImage, string masterName, Bitmap capturedImage, System.Threading.ManualResetEvent manualResetEvent, ImageComparisonSettings toleranceSettings, int index)
        {
            _manualResetEvent = manualResetEvent;
            _capturedImage = capturedImage;
            _masterImage = masterImage;
            _masterName = masterName;
            _result = new ImageComparisonResult(index, manualResetEvent);
            _toleranceSettings = toleranceSettings;
        }
        ~AsyncData()
        { 
            FreeResources(); 
        }

        public void Dispose()
        {
            FreeResources();
            GC.SuppressFinalize(this);
        }

        private void FreeResources()
        {
            if (_masterImage != null) { _masterImage.Dispose(); _masterImage = null; }
            if (_capturedImage != null) { _capturedImage.Dispose(); _capturedImage = null; }
        }

    }
}
