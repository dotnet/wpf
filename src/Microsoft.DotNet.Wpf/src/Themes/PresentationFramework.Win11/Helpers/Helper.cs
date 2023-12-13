using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PresentationFramework.Win11
{
    internal static class Helper
    {
        public static bool IsAnimationsEnabled => SystemParameters.ClientAreaAnimation &&
                                                  RenderCapability.Tier > 0;

        public static bool TryGetTransformToDevice(Visual visual, out Matrix value)
        {
            var presentationSource = PresentationSource.FromVisual(visual);
            if (presentationSource != null)
            {
                value = presentationSource.CompositionTarget.TransformToDevice;
                return true;
            }

            value = default;
            return false;
        }

        public static Vector GetOffset(
            UIElement element1,
            InterestPoint interestPoint1,
            UIElement element2,
            InterestPoint interestPoint2,
            Rect element2Bounds)
        {
            Point point = element1.TranslatePoint(GetPoint(element1, interestPoint1), element2);
            if (element2Bounds.IsEmpty)
            {
                return point - GetPoint(element2, interestPoint2);
            }
            else
            {
                return point - GetPoint(element2Bounds, interestPoint2);
            }
        }

        private static Point GetPoint(UIElement element, InterestPoint interestPoint)
        {
            return GetPoint(new Rect(element.RenderSize), interestPoint);
        }

        private static Point GetPoint(Rect rect, InterestPoint interestPoint)
        {
            switch (interestPoint)
            {
                case InterestPoint.TopLeft:
                    return rect.TopLeft;
                case InterestPoint.TopRight:
                    return rect.TopRight;
                case InterestPoint.BottomLeft:
                    return rect.BottomLeft;
                case InterestPoint.BottomRight:
                    return rect.BottomRight;
                case InterestPoint.Center:
                    return new Point(rect.Left + rect.Width / 2,
                                     rect.Top + rect.Height / 2);
                default:
                    throw new ArgumentOutOfRangeException(nameof(interestPoint));
            }
        }

        public static bool HasDefaultValue(this DependencyObject d, DependencyProperty dp)
        {
            return DependencyPropertyHelper.GetValueSource(d, dp).BaseValueSource == BaseValueSource.Default;
        }

        // return true if there is a local or style-supplied value for the dp
        public static bool HasNonDefaultValue(this DependencyObject d, DependencyProperty dp)
        {
            return !HasDefaultValue(d, dp);
        }

        public static bool HasLocalValue(this DependencyObject d, DependencyProperty dp)
        {
            return d.ReadLocalValue(dp) != DependencyProperty.UnsetValue;
        }

        public static DpiScale2 GetDpi(this Window window)
        {
            if (window is null)
            {
                throw new ArgumentNullException(nameof(window));
            }

#if NET462_OR_NEWER
            return new DpiScale2(VisualTreeHelper.GetDpi(window));
#else
            var hwnd = new WindowInteropHelper(window).Handle;
            var hwndSource = HwndSource.FromHwnd(hwnd);
            if (hwndSource != null)
            {
                Matrix transformToDevice = hwndSource.CompositionTarget.TransformToDevice;
                return new DpiScale2(transformToDevice.M11, transformToDevice.M22);
            }
            else
            {
                Debug.Fail("Should not reach here");
                return new DpiScale2(1, 1);
            }
#endif
        }
    }

    internal enum InterestPoint
    {
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3,
        Center = 4,
    }
}
