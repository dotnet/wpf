using System.Windows;

namespace PresentationFramework.Win11
{
    internal readonly record struct DpiScale2
    {
        public DpiScale2(double dpiScaleX, double dpiScaleY)
        {
            DpiScaleX = dpiScaleX;
            DpiScaleY = dpiScaleY;
        }

#if NET462_OR_NEWER
        public DpiScale2(DpiScale dpiScale)
            : this(dpiScale.DpiScaleX, dpiScale.DpiScaleY)
        {
        }
#endif

        public double DpiScaleX { get; }
        public double DpiScaleY { get; }
    }
}
