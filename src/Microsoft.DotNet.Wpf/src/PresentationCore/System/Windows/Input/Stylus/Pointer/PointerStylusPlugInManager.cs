// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Utility;
using MS.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// Implements a manager to organize and track StylusPlugins for a given PresentationSource.
    /// 
    /// This class also provides the appropriate functions for calling through to StylusPluginCollections
    /// and hit testing against them.  It encompasses most of the functionality previously held by PenContexts
    /// in the WISP stack, but with less attachment to the underlying stack implementation.
    /// </summary>
    internal class PointerStylusPlugInManager
    {
        #region Construction/Initialization

        internal PointerStylusPlugInManager(PresentationSource source)
        {
            _inputSource = new SecurityCriticalData<PresentationSource>(source);
        }

        #endregion

        #region Plugin Collection Manipulation

        /// <summary>
        /// Adds a plugin collection in the Z order that its owning element appears graphically
        /// </summary>
        /// <param name="pic"></param>
        internal void AddStylusPlugInCollection(StylusPlugInCollection pic)
        {
            // insert in ZOrder
            _plugInCollectionList.Insert(FindZOrderIndex(pic), pic);
        }

        internal void RemoveStylusPlugInCollection(StylusPlugInCollection pic)
        {
            _plugInCollectionList.Remove(pic);
        }

        #endregion

        #region Plugin Hit/Order Utilities

        /// <summary>
        /// Get a transformation from the tablet to the screen
        /// </summary>
        /// <param name="tablet">The tablet device to use</param>
        /// <returns>A matrix from the tablet to the screen</returns>
        private Matrix GetTabletToViewTransform(TabletDevice tablet)
        {
            Matrix matrix = (_inputSource.Value as HwndSource)?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;

            matrix.Invert();
            matrix *= tablet.TabletDeviceImpl.TabletToScreen;

            return matrix;
        }

        /// <summary>
        /// Find the Z-Order of the collection's owning element.
        /// </summary>
        /// <param name="spicAdding">The collection we are adding</param>
        /// <returns>The integer z-order of the collection</returns>
        internal int FindZOrderIndex(StylusPlugInCollection spicAdding)
        {
            // As we do not have real time input, we no longer need a lock here.
            // When real-time input is added back, make sure to add the lock back.
            //should be called inside of lock(__rtiLock)
            DependencyObject spicAddingVisual = spicAdding.Element as Visual;
            int i;
            for (i = 0; i < _plugInCollectionList.Count; i++)
            {
                // first see if parent of node, if it is then we can just scan till we find the 
                // first non parent and we're done
                DependencyObject curV = _plugInCollectionList[i].Element as Visual;
                if (VisualTreeHelper.IsAncestorOf(spicAddingVisual, curV))
                {
                    i++;
                    while (i < _plugInCollectionList.Count)
                    {
                        curV = _plugInCollectionList[i].Element as Visual;
                        if (!VisualTreeHelper.IsAncestorOf(spicAddingVisual, curV))
                            break; // done
                        i++;
                    }
                    return i;
                }
                else
                {
                    // Look to see if spicAddingVisual is higher in ZOrder than i, if so then we're done
                    DependencyObject commonParent = VisualTreeHelper.FindCommonAncestor(spicAddingVisual, curV);
                    // If no common parent found then we must have multiple plugincollection elements 
                    // that have been removed from the visual tree and we haven't been notified yet of
                    // that change.  In this case just ignore this plugincollection element and go to 
                    // the next.
                    if (commonParent == null)
                        continue;
                    // If curV is the commonParent we find then we're done.  This new plugin should be
                    // above this one.
                    if (curV == commonParent)
                        return i;
                    // now find first child for each under that common visual that these fall under (not they must be different or common parent is sort of busted.
                    while (VisualTreeHelper.GetParentInternal(spicAddingVisual) != commonParent)
                        spicAddingVisual = VisualTreeHelper.GetParentInternal(spicAddingVisual);
                    while (VisualTreeHelper.GetParentInternal(curV) != commonParent)
                        curV = VisualTreeHelper.GetParentInternal(curV);
                    // now see which is higher in zorder
                    int count = VisualTreeHelper.GetChildrenCount(commonParent);
                    for (int j = 0; j < count; j++)
                    {
                        DependencyObject child = VisualTreeHelper.GetChild(commonParent, j);
                        if (child == spicAddingVisual)
                            return i;
                        else if (child == curV)
                            break; // look at next index in _piList.                        
                    }
                }
            }

            return i; // this wasn't higher so return last index.
        }

        /// <summary>
        /// Target a plugin collection that either has capture or passes a hit test.
        /// raw input.
        /// </summary>
        /// <param name="inputReport">The raw input to process</param>
        /// <returns>The appropriate collection target</returns>
        internal StylusPlugInCollection TargetPlugInCollection(RawStylusInputReport inputReport)
        {
            StylusPlugInCollection pic = null;

            PointerStylusDevice stylusDevice = inputReport.StylusDevice.As<PointerStylusDevice>();

            bool elementHasCapture = false;

            pic = stylusDevice.GetCapturedPlugInCollection(ref elementHasCapture);

            int pointLength = inputReport.StylusPointDescription.GetInputArrayLengthPerPoint();

            // Make sure that the captured Plugin Collection is still in the list.  CaptureChanges are
            // deferred so there is a window where the stylus device is not updated yet.  This protects us
            // from using a bogus plugin collecton for an element that is in an invalid state.
            if (elementHasCapture && !_plugInCollectionList.Contains(pic))
            {
                elementHasCapture = false;  // force true hittesting to be done!
            }

            if (!elementHasCapture && inputReport.Data != null && inputReport.Data.Length >= pointLength)
            {
                int[] data = inputReport.Data;
                System.Diagnostics.Debug.Assert(data.Length % pointLength == 0);
                Point ptTablet = new Point(data[data.Length - pointLength], data[data.Length - pointLength + 1]);
                // Note: the StylusLogic data inside DeviceUnitsFromMeasurUnits is protected by __rtiLock.
                ptTablet = ptTablet * stylusDevice.TabletDevice.TabletDeviceImpl.TabletToScreen;
                ptTablet.X = (int)Math.Round(ptTablet.X); // Make sure we snap to whole window pixels.
                ptTablet.Y = (int)Math.Round(ptTablet.Y);
                ptTablet *= inputReport.InputSource.CompositionTarget.TransformFromDevice; // change to measured units now.

                pic = HittestPlugInCollection(ptTablet); // Use cached rectangles for UIElements.
            }

            return pic;
        }

        /// <summary>
        /// Finds a plugin collection by the UIElement it is associated with (directly or ancestrally).
        /// </summary>
        /// <param name="element">The element to find</param>
        /// <returns>The plugin collection associated with the element or null if none are.</returns>
        internal StylusPlugInCollection FindPlugInCollection(UIElement element)
        {
            foreach (StylusPlugInCollection plugInCollection in _plugInCollectionList)
            {
                // If same element or element is child of the plugincollection element than say we hit it.
                if (plugInCollection.Element == element ||
                     (plugInCollection.Element as Visual).IsAncestorOf(element as Visual))
                {
                    return plugInCollection;
                }
            }

            return null;
        }

        /// <summary>
        /// Hit test a point against a plugin collection
        /// </summary>
        /// <param name="pt">The point to test</param>
        /// <returns>The plugin collection that passes the test or null if none do.</returns>
        StylusPlugInCollection HittestPlugInCollection(Point pt)
        {
            foreach (StylusPlugInCollection plugInCollection in _plugInCollectionList)
            {
                if (plugInCollection.IsHit(pt))
                {
                    return plugInCollection;
                }
            }

            return null;
        }


        #endregion

        #region Plugin Invocation


        /// <summary>
        /// Verifies that the call to the stylus plugin on the RTI thread was valid against the current visual tree.
        /// If this verification fails, we will rebuild and resend the raw input and custom data in order to inform
        /// the target collections of the error and fix it.
        /// </summary>
        /// <remarks>
        /// This is a remnant of verifying hits to the collections based on the UI thread calling this
        /// after the real time input thread has previously called a collection.  This is due to the RTI thread
        /// not being 100% synchronous with the visual tree in terms of hitting the collections.  This is sort of
        /// redundant right now, but needs to be here when we re-implement the RTI.
        /// </remarks>
        internal void VerifyStylusPlugInCollectionTarget(RawStylusInputReport rawStylusInputReport)
        {
            switch (rawStylusInputReport.Actions)
            {
                case RawStylusActions.Down:
                case RawStylusActions.Move:
                case RawStylusActions.Up:
                    break;
                default:
                    return; // do nothing if not Down, Move or Up.
            }

            PointerLogic pointerLogic = StylusLogic.GetCurrentStylusLogicAs<PointerLogic>();

            RawStylusInput originalRSI = rawStylusInputReport.RawStylusInput;
            // See if we have a plugin for the target of this input.
            StylusPlugInCollection targetPIC = null;
            StylusPlugInCollection targetRtiPIC = (originalRSI != null) ? originalRSI.Target : null;
            bool updateEventPoints = false;

            // Make sure we use UIElement for target if non NULL and hit ContentElement.
            UIElement newTarget = InputElement.GetContainingUIElement(rawStylusInputReport.StylusDevice.DirectlyOver as DependencyObject) as UIElement;

            if (newTarget != null)
            {
                targetPIC = FindPlugInCollection(newTarget);
            }

            // Make sure any lock() calls do not reenter on us.
            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                // See if we hit the wrong PlugInCollection on the pen thread and clean things up if we did.
                if (targetRtiPIC != null && targetRtiPIC != targetPIC && originalRSI != null)
                {
                    // Fire custom data not confirmed events for both pre and post since bad target...
                    foreach (RawStylusInputCustomData customData in originalRSI.CustomDataList)
                    {
                        customData.Owner.FireCustomData(customData.Data, rawStylusInputReport.Actions, false);
                    }

                    updateEventPoints = originalRSI.StylusPointsModified;

                    // Clear RawStylusInput data.
                    rawStylusInputReport.RawStylusInput = null;
                }

                // See if we need to build up an RSI to send to the plugincollection (due to a mistarget).
                bool sendRawStylusInput = false;
                if (targetPIC != null && rawStylusInputReport.RawStylusInput == null)
                {
                    // NOTE: (This applies when RTI is back in) Info will not change (it gets rebuilt instead so keeping ref is fine)
                    //    The transformTabletToView matrix and plugincollection rects though can change based
                    //    off of layout events which is why we need to lock this.
                    GeneralTransformGroup transformTabletToView = new GeneralTransformGroup();
                    transformTabletToView.Children.Add(rawStylusInputReport.StylusDevice.As<PointerStylusDevice>().GetTabletToElementTransform(null)); // this gives matrix in measured units (not device)
                    transformTabletToView.Children.Add(targetPIC.ViewToElement); // Make it relative to the element.
                    transformTabletToView.Freeze();  // Must be frozen for multi-threaded access.

                    RawStylusInput rawStylusInput = new RawStylusInput(rawStylusInputReport, transformTabletToView, targetPIC);
                    rawStylusInputReport.RawStylusInput = rawStylusInput;
                    sendRawStylusInput = true;
                }

                PointerStylusDevice stylusDevice = rawStylusInputReport.StylusDevice.As<PointerStylusDevice>();

                // Now fire the confirmed enter/leave events as necessary.
                StylusPlugInCollection currentTarget = stylusDevice.CurrentVerifiedTarget;
                if (targetPIC != currentTarget)
                {
                    if (currentTarget != null)
                    {
                        // Fire leave event.  If we never had a plugin for this event then create a temp one.
                        if (originalRSI == null)
                        {
                            GeneralTransformGroup transformTabletToView = new GeneralTransformGroup();
                            transformTabletToView.Children.Add(stylusDevice.GetTabletToElementTransform(null)); // this gives matrix in measured units (not device)
                            transformTabletToView.Children.Add(currentTarget.ViewToElement); // Make it relative to the element.
                            transformTabletToView.Freeze();  // Must be frozen for multi-threaded access.
                            originalRSI = new RawStylusInput(rawStylusInputReport, transformTabletToView, currentTarget);
                        }
                        currentTarget.FireEnterLeave(false, originalRSI, true);
                    }

                    if (targetPIC != null)
                    {
                        // Fire Enter event
                        targetPIC.FireEnterLeave(true, rawStylusInputReport.RawStylusInput, true);

                        // Indicate we've used a stylus plugin
                        pointerLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
                    }

                    // Update the verified target.
                    stylusDevice.CurrentVerifiedTarget = targetPIC;
                }

                // Now fire RawStylusInput if needed to the right plugincollection.
                if (sendRawStylusInput)
                {
                    // We are on the pen thread, just call directly.
                    targetPIC.FireRawStylusInput(rawStylusInputReport.RawStylusInput);
                    updateEventPoints = (updateEventPoints || rawStylusInputReport.RawStylusInput.StylusPointsModified);

                    // Indicate we've used a stylus plugin
                    pointerLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
                }

                // Now fire PrePreviewCustomData events.
                if (targetPIC != null)
                {
                    // Send custom data pre event
                    foreach (RawStylusInputCustomData customData in rawStylusInputReport.RawStylusInput.CustomDataList)
                    {
                        customData.Owner.FireCustomData(customData.Data, rawStylusInputReport.Actions, true);
                    }
                }

                // VerifyRawTarget might resend to correct plugins or may have hit the wrong plugincollection.  The StylusPackets
                // may be overriden in those plugins so we need to call UpdateEventStylusPoints to update things.
                if (updateEventPoints)
                {
                    rawStylusInputReport.StylusDevice.As<PointerStylusDevice>().UpdateEventStylusPoints(rawStylusInputReport, true);
                }
            }
        }

        /// <summary>
        /// </summary>
        internal StylusPlugInCollection InvokeStylusPluginCollectionForMouse(RawStylusInputReport inputReport, IInputElement directlyOver, StylusPlugInCollection currentPlugInCollection)
        {
            StylusPlugInCollection newPlugInCollection = null;

            // Find new target plugin collection
            if (directlyOver != null)
            {
                UIElement uiElement = InputElement.GetContainingUIElement(directlyOver as DependencyObject) as UIElement;
                if (uiElement != null)
                {
                    newPlugInCollection = FindPlugInCollection(uiElement);
                }
            }

            // Fire Leave event to old pluginCollection if we need to.
            if (currentPlugInCollection != null && currentPlugInCollection != newPlugInCollection)
            {
                // NOTE: input report points for mouse are in avalon measured units and not device!
                RawStylusInput tempRawStylusInput = new RawStylusInput(inputReport, currentPlugInCollection.ViewToElement, currentPlugInCollection);

                currentPlugInCollection.FireEnterLeave(false, tempRawStylusInput, true);

                // Indicate we've used a stylus plugin
                StylusLogic.CurrentStylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
            }
            if (newPlugInCollection != null)
            {
                // NOTE: input report points for mouse are in avalon measured units and not device!
                RawStylusInput rawStylusInput = new RawStylusInput(inputReport, newPlugInCollection.ViewToElement, newPlugInCollection);
                inputReport.RawStylusInput = rawStylusInput;

                if (newPlugInCollection != currentPlugInCollection)
                {
                    newPlugInCollection.FireEnterLeave(true, rawStylusInput, true);
                }

                // (Comment not applicable until RTI back in) We are on the RTI thread, just call directly.
                newPlugInCollection.FireRawStylusInput(rawStylusInput);

                // Indicate we've used a stylus plugin
                StylusLogic.CurrentStylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;

                // Fire custom data events (always confirmed for mouse)
                foreach (RawStylusInputCustomData customData in rawStylusInput.CustomDataList)
                {
                    customData.Owner.FireCustomData(customData.Data, inputReport.Actions, true);
                }
            }

            return newPlugInCollection;
        }

        /// <summary>
        /// Calls the appropriate plugin collection for the raw input.
        /// </summary>
        /// <remarks>
        /// This would normally be called on the RTI thread.  As we do not have one right now, we just call this in the
        /// Hwnd input provider in order to keep similar semantics to how the WISP stack accomplishes this.  When the RTI is 
        /// re-implemented, we need to ensure this gets called from there exclusively.
        /// </remarks>
        internal void InvokeStylusPluginCollection(RawStylusInputReport inputReport)
        {
            // Find PenContexts object for this inputReport.
            StylusPlugInCollection pic = null;

            switch (inputReport.Actions)
            {
                case RawStylusActions.Down:
                case RawStylusActions.Move:
                case RawStylusActions.Up:
                    // Figure out current target plugincollection.
                    pic = TargetPlugInCollection(inputReport);
                    break;

                default:
                    return; // Nothing to do unless one of the above events
            }

            PointerStylusDevice stylusDevice = inputReport.StylusDevice.As<PointerStylusDevice>();

            StylusPlugInCollection currentPic = stylusDevice.CurrentVerifiedTarget;

            // Fire Leave event if we need to.
            if (currentPic != null && currentPic != pic)
            {
                // Create new RawStylusInput to send
                GeneralTransformGroup transformTabletToView = new GeneralTransformGroup();
                transformTabletToView.Children.Add(new MatrixTransform(GetTabletToViewTransform(stylusDevice.TabletDevice))); // this gives matrix in measured units (not device)
                transformTabletToView.Children.Add(currentPic.ViewToElement); // Make it relative to the element.
                transformTabletToView.Freeze(); // Must be frozen for multi-threaded access.

                RawStylusInput tempRawStylusInput = new RawStylusInput(inputReport, transformTabletToView, currentPic);

                currentPic.FireEnterLeave(false, tempRawStylusInput, false);

                // Indicate we've used a stylus plugin
                StylusLogic.CurrentStylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
                stylusDevice.CurrentVerifiedTarget = null;
            }

            if (pic != null)
            {
                // NOTE: Comment not applicable without RTI.  Info will not change (it gets rebuilt instead so keeping ref is fine)
                //    The transformTabletToView matrix and plugincollection rects though can change based 
                //    off of layout events which is why we need to lock this.
                GeneralTransformGroup transformTabletToView = new GeneralTransformGroup();
                transformTabletToView.Children.Add(new MatrixTransform(GetTabletToViewTransform(stylusDevice.TabletDevice))); // this gives matrix in measured units (not device)
                transformTabletToView.Children.Add(pic.ViewToElement); // Make it relative to the element.
                transformTabletToView.Freeze();  // Must be frozen for multi-threaded access.

                RawStylusInput rawStylusInput = new RawStylusInput(inputReport, transformTabletToView, pic);
                inputReport.RawStylusInput = rawStylusInput;

                if (pic != currentPic)
                {
                    stylusDevice.CurrentVerifiedTarget = pic;
                    pic.FireEnterLeave(true, rawStylusInput, false);
                }

                // Send the input
                pic.FireRawStylusInput(rawStylusInput);

                // Indicate we've used a stylus plugin
                StylusLogic.CurrentStylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
            }
        }

        /// <summary>
        /// Used in order to allow StylusPlugins to affect mouse inputs as well as touch.
        /// </summary>
        internal static void InvokePlugInsForMouse(ProcessInputEventArgs e)
        {
            if (!e.StagingItem.Input.Handled)
            {
                // if we see a preview mouse event that is not generated by a stylus
                // then send on to plugin
                if ((e.StagingItem.Input.RoutedEvent != Mouse.PreviewMouseDownEvent) &&
                    (e.StagingItem.Input.RoutedEvent != Mouse.PreviewMouseUpEvent) &&
                    (e.StagingItem.Input.RoutedEvent != Mouse.PreviewMouseMoveEvent) &&
                    (e.StagingItem.Input.RoutedEvent != InputManager.InputReportEvent))
                    return;

                // record the mouse capture for later reference..
                MouseDevice mouseDevice;
                PresentationSource source;
                bool leftButtonDown;
                bool rightButtonDown;
                RawStylusActions stylusActions = RawStylusActions.None;
                int timestamp;

                // See if we need to deal sending a leave due to this PresentationSource being Deactivated
                // If not then we just return and do nothing.
                if (e.StagingItem.Input.RoutedEvent == InputManager.InputReportEvent)
                {
                    if (_activeMousePlugInCollection?.Element == null)
                        return;

                    InputReportEventArgs input = e.StagingItem.Input as InputReportEventArgs;

                    if (input.Report.Type != InputType.Mouse)
                        return;

                    RawMouseInputReport mouseInputReport = (RawMouseInputReport)input.Report;

                    if ((mouseInputReport.Actions & RawMouseActions.Deactivate) != RawMouseActions.Deactivate)
                        return;

                    mouseDevice = InputManager.UnsecureCurrent.PrimaryMouseDevice;

                    // Mouse set directly over to null when truly deactivating.
                    if (mouseDevice == null || mouseDevice.DirectlyOver != null)
                        return;

                    leftButtonDown = mouseDevice.LeftButton == MouseButtonState.Pressed;
                    rightButtonDown = mouseDevice.RightButton == MouseButtonState.Pressed;
                    timestamp = mouseInputReport.Timestamp;

                    // Get presentationsource from element.
                    source = PresentationSource.CriticalFromVisual(_activeMousePlugInCollection.Element as Visual);
                }
                else
                {
                    MouseEventArgs mouseEventArgs = e.StagingItem.Input as MouseEventArgs;
                    mouseDevice = mouseEventArgs.MouseDevice;
                    leftButtonDown = mouseDevice.LeftButton == MouseButtonState.Pressed;
                    rightButtonDown = mouseDevice.RightButton == MouseButtonState.Pressed;

                    // Only look at mouse input reports that truly come from a mouse (and is not an up or deactivate) and it
                    // must be pressed state if a move (we don't fire stylus inair moves currently)
                    if (mouseEventArgs.StylusDevice != null &&
                        e.StagingItem.Input.RoutedEvent != Mouse.PreviewMouseUpEvent)
                        return;

                    if (e.StagingItem.Input.RoutedEvent == Mouse.PreviewMouseMoveEvent)
                    {
                        if (!leftButtonDown)
                            return;
                        stylusActions = RawStylusActions.Move;
                    }
                    if (e.StagingItem.Input.RoutedEvent == Mouse.PreviewMouseDownEvent)
                    {
                        MouseButtonEventArgs mouseButtonEventArgs = mouseEventArgs as MouseButtonEventArgs;
                        if (mouseButtonEventArgs.ChangedButton != MouseButton.Left)
                            return;
                        stylusActions = RawStylusActions.Down;
                    }
                    if (e.StagingItem.Input.RoutedEvent == Mouse.PreviewMouseUpEvent)
                    {
                        MouseButtonEventArgs mouseButtonEventArgs = mouseEventArgs as MouseButtonEventArgs;
                        if (mouseButtonEventArgs.ChangedButton != MouseButton.Left)
                            return;
                        stylusActions = RawStylusActions.Up;
                    }
                    timestamp = mouseEventArgs.Timestamp;

                    Visual directlyOverVisual = mouseDevice.DirectlyOver as Visual;
                    if (directlyOverVisual == null)
                    {
                        return;
                    }

                    // 
                    // Take the presentation source which is associated to the directly over element.
                    source = PresentationSource.CriticalFromVisual(directlyOverVisual);
                }

                if ((source != null) &&
                    (source.CompositionTarget != null) &&
                    !source.CompositionTarget.IsDisposed)
                {
                    IInputElement directlyOver = mouseDevice.DirectlyOver;
                    int packetStatus = (leftButtonDown ? 1 : 0) | (rightButtonDown ? 9 : 0); // pen tip down == 1, barrel = 8
                    Point ptClient = mouseDevice.GetPosition(source.RootVisual as IInputElement);
                    ptClient = source.CompositionTarget.TransformToDevice.Transform(ptClient);

                    int buttons = (leftButtonDown ? 1 : 0) | (rightButtonDown ? 3 : 0);
                    int[] data = { (int)ptClient.X, (int)ptClient.Y, packetStatus, buttons };
                    RawStylusInputReport inputReport = new RawStylusInputReport(
                                                                InputMode.Foreground,
                                                                timestamp,
                                                                source,
                                                                stylusActions,
                                                                () => { return MousePointDescription; },
                                                                0, 0,
                                                                data);

                    PointerStylusPlugInManager manager =
                        StylusLogic.GetCurrentStylusLogicAs<PointerLogic>()?.GetManagerForSource(source);

                    // Avoid re-entrancy due to lock() being used.
                    using (Dispatcher.CurrentDispatcher.DisableProcessing())
                    {
                        // Call plugins (does enter/leave and FireCustomData as well)
                        _activeMousePlugInCollection = manager?.InvokeStylusPluginCollectionForMouse(inputReport, directlyOver, _activeMousePlugInCollection);
                    }
                }
            }
        }

        #endregion

        #region Private Members

        private static StylusPointDescription _mousePointDescription;

        /// <summary>
        /// Used to provide a valid StylusPointDescription for mouse data.
        /// </summary>
        private static StylusPointDescription MousePointDescription
        {
            get
            {
                if (_mousePointDescription == null)
                {
                    _mousePointDescription = new StylusPointDescription(
                                                    new StylusPointPropertyInfo[] {
                                                        StylusPointPropertyInfoDefaults.X,
                                                        StylusPointPropertyInfoDefaults.Y,
                                                        StylusPointPropertyInfoDefaults.NormalPressure,
                                                        StylusPointPropertyInfoDefaults.PacketStatus,
                                                        StylusPointPropertyInfoDefaults.TipButton,
                                                        StylusPointPropertyInfoDefaults.BarrelButton
                                                    },
                                                    -1); // No real pressure in data
                }

                return _mousePointDescription;
            }
        }

        internal SecurityCriticalData<PresentationSource> _inputSource;

        List<StylusPlugInCollection> _plugInCollectionList = new List<StylusPlugInCollection>();

        [ThreadStatic]
        private static StylusPlugInCollection _activeMousePlugInCollection;

        #endregion
    }
}

