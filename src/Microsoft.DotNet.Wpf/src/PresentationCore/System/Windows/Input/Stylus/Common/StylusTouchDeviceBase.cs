// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal;
using System.Security;
using System.Windows.Media;

namespace System.Windows.Input
{
    /// <summary>
    ///
    /// Base class for splitting common functionality between the WM_POINTER and WISP based
    /// WPF touch stacks.
    /// </summary>
    internal abstract class StylusTouchDeviceBase : TouchDevice
    {
        #region TouchDevice Implementation

        internal StylusTouchDeviceBase(StylusDeviceBase stylusDevice)
            : base(stylusDevice.Id)
        {
            StylusDevice = stylusDevice;

            // DevDiv:652804
            // This used to be statically defined, but this gave rise to issues with 
            // calling GetIntermediateTouchPoints as the properties were likely to not
            // match the hardware.  Now, we set this from the actual tablet queried
            // description so we can be assured they match save for when there is an
            // actual erroneous situation.
            _stylusPointDescription = StylusDevice?.TabletDevice?.TabletDeviceImpl?.StylusPointDescription ?? _stylusPointDescription;

            PromotingToOther = true;
        }

        /// <summary>
        ///     Provides the current position.
        /// </summary>
        /// <param name="relativeTo">Defines the coordinate space.</param>
        /// <returns>The current position in the coordinate space of relativeTo.</returns>
        public override TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            Point position = StylusDevice.GetPosition(relativeTo);

            Rect rectBounds = GetBounds(StylusDevice.RawStylusPoint, position, relativeTo);

            return new TouchPoint(this, position, rectBounds, _lastAction);
        }

        private Rect GetBounds(StylusPoint stylusPoint, Point position, IInputElement relativeTo)
        {
            GeneralTransform elementToRoot;
            GeneralTransform rootToElement;
            GetRootTransforms(relativeTo, out elementToRoot, out rootToElement);
            return GetBounds(stylusPoint, position, relativeTo, elementToRoot, rootToElement);
        }

        private Rect GetBounds(StylusPoint stylusPoint,
            Point position,
            IInputElement relativeTo,
            GeneralTransform elementToRoot,
            GeneralTransform rootToElement)
        {
            // Get width and heith in pixel value
            double width = GetStylusPointWidthOrHeight(stylusPoint, /*isWidth*/ true);
            double height = GetStylusPointWidthOrHeight(stylusPoint, /*isWidth*/ false);

            // Get the position with respect to root
            Point rootPoint;
            if (elementToRoot == null ||
                !elementToRoot.TryTransform(position, out rootPoint))
            {
                rootPoint = position;
            }

            // Create a Rect with respect to root and transform it to element coordinate space
            Rect rectBounds = new Rect(rootPoint.X - width * 0.5, rootPoint.Y - height * 0.5, width, height);
            if (rootToElement != null)
            {
                rectBounds = rootToElement.TransformBounds(rectBounds);
            }
            return rectBounds;
        }

        protected abstract double GetStylusPointWidthOrHeight(StylusPoint stylusPoint, bool isWidth);

        /// <summary>
        ///     Provides all of the known points the device hit since the last reported position update.
        /// </summary>
        /// <param name="relativeTo">Defines the coordinate space.</param>
        /// <returns>A list of points in the coordinate space of relativeTo.</returns>
        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            // Retrieve the stylus points
            StylusPointCollection stylusPoints = StylusDevice.GetStylusPoints(relativeTo, _stylusPointDescription);
            int count = stylusPoints.Count;
            TouchPointCollection touchPoints = new TouchPointCollection();

            GeneralTransform elementToRoot;
            GeneralTransform rootToElement;
            GetRootTransforms(relativeTo, out elementToRoot, out rootToElement);

            // Convert the stylus points into touch points
            for (int i = 0; i < count; i++)
            {
                StylusPoint stylusPoint = stylusPoints[i];
                Point position = new Point(stylusPoint.X, stylusPoint.Y);
                Rect rectBounds = GetBounds(stylusPoint, position, relativeTo, elementToRoot, rootToElement);

                TouchPoint touchPoint = new TouchPoint(this, position, rectBounds, _lastAction);
                touchPoints.Add(touchPoint);
            }

            return touchPoints;
        }

        private void GetRootTransforms(IInputElement relativeTo, out GeneralTransform elementToRoot, out GeneralTransform rootToElement)
        {
            elementToRoot = rootToElement = null;

            DependencyObject containingVisual = InputElement.GetContainingVisual(relativeTo as DependencyObject);
            if (containingVisual != null)
            {
                PresentationSource relativePresentationSource = PresentationSource.CriticalFromVisual(containingVisual);
                Visual rootVisual = (relativePresentationSource != null) ? relativePresentationSource.RootVisual : null;
                Visual containingVisual2D = VisualTreeHelper.GetContainingVisual2D(containingVisual);
                if ((rootVisual != null) && (containingVisual2D != null))
                {
                    elementToRoot = containingVisual2D.TransformToAncestor(rootVisual);
                    rootToElement = rootVisual.TransformToDescendant(containingVisual2D);
                }
            }
        }

        #endregion

        #region StylusLogic Related

        internal void ChangeActiveSource(PresentationSource activeSource)
        {
            SetActiveSource(activeSource);
        }

        internal void OnActivate()
        {
            Activate();

            _activeDeviceCount++;

            if (_activeDeviceCount == 1)
            {
                IsPrimary = true;
                OnActivateImpl();
            }

            PromotingToOther = true;
        }

        /// <summary>
        /// Override to provide stack specific behavior for activation
        /// </summary>
        protected abstract void OnActivateImpl();

        internal void OnDeactivate()
        {
            Deactivate();

            PromotingToOther = true;
            DownHandled = false;

            _activeDeviceCount--;

            OnDeactivateImpl();

            IsPrimary = false;
        }

        /// <summary>
        /// Override to provide stack specific behavior for deactivation
        /// </summary>
        protected abstract void OnDeactivateImpl();

        internal bool OnDown()
        {
            _lastAction = TouchAction.Down;
            DownHandled = ReportDown();
            return DownHandled;
        }

        internal bool OnMove()
        {
            _lastAction = TouchAction.Move;
            return ReportMove();
        }

        internal bool OnUp()
        {
            _lastAction = TouchAction.Up;
            return ReportUp();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Whether input associated with this device is being promoted to some other device.
        /// </summary>
        public bool PromotingToOther
        {
            get;
            protected set;
        }

        /// <summary>
        ///     Bool indicates if down event was handled
        /// </summary>
        internal bool DownHandled
        {
            get;
            private set;
        }

        /// <summary>
        ///     The associated StylusDevice
        /// </summary>
        internal StylusDeviceBase StylusDevice { get; private set; }

        internal bool IsPrimary { get; private set; }

        internal static int ActiveDeviceCount { get { return _activeDeviceCount; } }

        #endregion

        #region Member Variables

        [ThreadStatic]
        private static int _activeDeviceCount = 0;

        private TouchAction _lastAction = TouchAction.Move;

        /// <summary>
        /// DevDiv:652804
        /// This used to be static readonly, which made a design time assumption on the stylus device.
        /// This led to issues so we preserve some of the original behavior by using this as a default
        /// initialization that can change if there is a valid Stylus/Tablet Device at construction.
        /// </summary>
        private StylusPointDescription _stylusPointDescription =
            new StylusPointDescription(
                new StylusPointPropertyInfo[]
                {
                    StylusPointPropertyInfoDefaults.X,
                    StylusPointPropertyInfoDefaults.Y,
                    StylusPointPropertyInfoDefaults.NormalPressure,
                    StylusPointPropertyInfoDefaults.Width,
                    StylusPointPropertyInfoDefaults.Height
                });

        #endregion

        #region Constants

        internal const double CentimetersPerInch = 2.54d;

        #endregion
    }
}
