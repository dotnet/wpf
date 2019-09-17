// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo(Microsoft.Internal.BuildInfo.ReachFramework)]
namespace MS.Internal.PrintWin32Thunk
{
    internal partial class XpsPrintStream : System.IO.Stream
    {
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanTimeout { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public static MS.Internal.PrintWin32Thunk.XpsPrintStream CreateXpsPrintStream() { throw null; }
        protected override void Dispose(bool A_0) { }
        public override void Flush() { }
        public System.Runtime.InteropServices.ComTypes.IStream GetManagedIStream() { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }

    internal partial class SafeMemoryHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeMemoryHandle(System.IntPtr Win32Pointer) : base (default(System.IntPtr), default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
        public static MS.Internal.PrintWin32Thunk.SafeMemoryHandle Null { get { throw null; } }
        public virtual int Size { get { throw null; } }
        public void CopyFromArray(byte[] source, int startIndex, int length) { }
        public void CopyToArray(byte[] destination, int startIndex, int length) { }
        public static MS.Internal.PrintWin32Thunk.SafeMemoryHandle Create(int byteCount) { throw null; }
        protected override bool ReleaseHandle() { throw null; }
        public static bool TryCreate(int byteCount, ref MS.Internal.PrintWin32Thunk.SafeMemoryHandle result) { throw null; }
        public static MS.Internal.PrintWin32Thunk.SafeMemoryHandle Wrap(System.IntPtr Win32Pointer) { throw null; }
    }
}
namespace System.Printing
{
    internal interface ILegacyDevice
    {
        int StartDocument(string printerName, string jobName, string filename, byte[] deviceMode);
        void StartDocumentWithoutCreatingDC(string printerName, string jobName, string filename);
        void EndDocument();
        void CreateDeviceContext(string printerName, string jobName, byte[] deviceMode);
        void DeleteDeviceContext();
        string ExtEscGetName();
        bool ExtEscMXDWPassThru();
        void StartPage(byte[] deviceMode, int rasterizationDPI);
        void EndPage();
        void PopTransform();
        void PopClip();
        void PushClip(System.Windows.Media.Geometry clipGeometry);
        void PushTransform(System.Windows.Media.Matrix transform);
        void DrawGeometry(System.Windows.Media.Brush brush, System.Windows.Media.Pen pen, System.Windows.Media.Brush strokeBrush, System.Windows.Media.Geometry geometry);
        void DrawImage(System.Windows.Media.Imaging.BitmapSource source, byte[] buffer, System.Windows.Rect rect);
        void DrawGlyphRun(System.Windows.Media.Brush brush, System.Windows.Media.GlyphRun glyphRun);
        void Comment(string message);
    }
    public partial class PrintQueue
    {
        internal System.Windows.Xps.Serialization.RCW.IXpsOMPackageWriter XpsOMPackageWriter { set { } }
        internal static uint GetDpiX(System.Printing.ILegacyDevice legacyDevice) { throw null; }
        internal static uint GetDpiY(System.Printing.ILegacyDevice legacyDevice) { throw null; }
        internal System.Printing.ILegacyDevice GetLegacyDevice() { throw null; }
    }
    internal class PrintSystemDispatcherObject : System.Windows.Threading.DispatcherObject
    {
        public void VerifyThreadLocality() {}
    }
}
namespace System.Windows.Xps
{
    public partial class XpsDocumentWriter 
    {
        internal XpsDocumentWriter(System.Printing.PrintQueue printQueue) { }
        internal XpsDocumentWriter(System.Windows.Xps.Packaging.XpsDocument document) { }
    }
}
