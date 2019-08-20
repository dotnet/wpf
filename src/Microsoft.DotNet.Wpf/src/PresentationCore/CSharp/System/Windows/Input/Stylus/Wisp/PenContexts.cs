// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

using System.Windows.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusWisp;
using System.Windows.Media;
using System.Security;
using MS.Internal;

using MS.Win32;
using MS.Utility;
using System.Runtime.InteropServices;

using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

using MS.Internal.PresentationCore;                        // SecurityHelper

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////

    internal sealed class PenContexts
    {
        /////////////////////////////////////////////////////////////////////////

        internal PenContexts(WispLogic stylusLogic, PresentationSource inputSource)
        {
            HwndSource hwndSource = inputSource as HwndSource;
            if(hwndSource == null || IntPtr.Zero == (hwndSource).CriticalHandle)
            {
                throw new InvalidOperationException(SR.Get(SRID.Stylus_PenContextFailure));
            }

            _stylusLogic  = stylusLogic;
            _inputSource   = new SecurityCriticalData<HwndSource>(hwndSource);
        }

        /////////////////////////////////////////////////////////////////////////

        internal void Enable()
        {
            if (_contexts == null)
            {
                // create contexts
                _contexts = _stylusLogic.WispTabletDevices.CreateContexts(_inputSource.Value.CriticalHandle, this);

                foreach(PenContext context in _contexts)
                {
                    context.Enable();
                }
            }
        }

        internal void Disable(bool shutdownWorkerThread)
        {
            if (_contexts != null)
            {
                foreach(PenContext context in _contexts)
                {
                    context.Disable(shutdownWorkerThread);
                }

                _contexts = null; // release refs on PenContext objects
            }
        }

        internal bool IsWindowDisabled
        {
            get { return _isWindowDisabled; }
            set { _isWindowDisabled = value; }
        }

        internal Point DestroyedLocation
        {
            get { return _destroyedLocation; }
            set { _destroyedLocation = value; }
        }

        /////////////////////////////////////////////////////////////////////

        internal void OnPenDown(PenContext penContext, int tabletDeviceId, int stylusPointerId, int[] data, int timestamp)
        {
            ProcessInput(RawStylusActions.Down, penContext, tabletDeviceId, stylusPointerId, data, timestamp);
        }

        /////////////////////////////////////////////////////////////////////

        internal void OnPenUp(PenContext penContext, int tabletDeviceId, int stylusPointerId, int[] data, int timestamp)
        {
            ProcessInput(RawStylusActions.Up, penContext, tabletDeviceId, stylusPointerId, data, timestamp);
        }

        /////////////////////////////////////////////////////////////////////

        internal void OnPackets(PenContext penContext, int tabletDeviceId, int stylusPointerId, int[] data, int timestamp)
        {
            ProcessInput(RawStylusActions.Move, penContext, tabletDeviceId, stylusPointerId, data, timestamp);
        }

        /////////////////////////////////////////////////////////////////////

        internal void OnInAirPackets(PenContext penContext, int tabletDeviceId, int stylusPointerId, int[] data, int timestamp)
        {
            ProcessInput(RawStylusActions.InAirMove, penContext, tabletDeviceId, stylusPointerId, data, timestamp);
        }

        /////////////////////////////////////////////////////////////////////

        internal void OnPenInRange(PenContext penContext, int tabletDeviceId, int stylusPointerId, int[] data, int timestamp)
        {
            ProcessInput(RawStylusActions.InRange, penContext, tabletDeviceId, stylusPointerId, data, timestamp);
        }

        /////////////////////////////////////////////////////////////////////

        internal void OnPenOutOfRange(PenContext penContext, int tabletDeviceId, int stylusPointerId, int timestamp)
        {
            ProcessInput(RawStylusActions.OutOfRange, penContext, tabletDeviceId, stylusPointerId, new int[]{}, timestamp);
        }

        /////////////////////////////////////////////////////////////////////


        internal void OnSystemEvent(PenContext penContext, 
                                           int tabletDeviceId, 
                                           int stylusPointerId, 
                                           int timestamp, 
                                           SystemGesture id, 
                                           int gestureX,
                                           int gestureY,
                                           int buttonState)
        {
            _stylusLogic.ProcessSystemEvent(penContext, 
                                             tabletDeviceId,
                                             stylusPointerId, 
                                             timestamp,
                                             id, 
                                             gestureX, 
                                             gestureY,
                                             buttonState, 
                                             _inputSource.Value);
        }

        /////////////////////////////////////////////////////////////////////

        void ProcessInput(
            RawStylusActions actions,
            PenContext penContext,
            int tabletDeviceId, 
            int stylusPointerId,
            int[] data, int timestamp)
        {
            // (all events but SystemEvent go thru here)
            _stylusLogic.ProcessInput(
                                        actions,
                                        penContext,
                                        tabletDeviceId,
                                        stylusPointerId,
                                        data, 
                                        timestamp,
                                        _inputSource.Value);
        }

        /////////////////////////////////////////////////////////////////////////

        internal PenContext GetTabletDeviceIDPenContext(int tabletDeviceId)
        {
            if (_contexts != null)
            {
                for (int i = 0; i < _contexts.Length; i++)
                {
                    PenContext context = _contexts[i];
                    if (context.TabletDeviceId == tabletDeviceId) 
                        return context;
                }
            }
            return null;
        }

        /////////////////////////////////////////////////////////////////////////

        internal bool ConsiderInRange(int timestamp)
        {
            if (_contexts != null)
            {
                for (int i = 0; i < _contexts.Length; i++)
                {
                    PenContext context = _contexts[i];
                    
                    // We consider it InRange if we have a queued up context event or 
                    // the timestamp - LastInRangeTime <= 500 (seen one in the last 500ms)
                    //  Here's some info on how this works...
                    //      int.MaxValue - int.MinValue = -1 (subtracting any negative # from MaxValue keeps this negative)
                    //      int.MinValue - int.MaxValue = 1 (subtracting any positive # from MinValue keeps this positive)
                    //  So subtracting wrapping values will return proper sign depending on which was earlier.
                    //  We do have the assumption that these values will be relative close in time.  If the
                    //  time wraps we'll say yet but the only harm is that we may defer a mouse move event temporarily
                    //  which won't cause any harm.
                    if (context.QueuedInRangeCount > 0 || (Math.Abs(unchecked(timestamp - context.LastInRangeTime)) <= 500))
                        return true;
                }
            }
            return false;
        }


        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified pen context index in response
        /// to the WM_TABLET_ADDED notification
        /// </summary>
        internal void AddContext(uint index)
        {
            // We only tear down the old context when PenContexts are enabled without being
            // dispose and we have a valid index. Otherwise, no-op here.
            if (_contexts != null && index <= _contexts.Length && _inputSource.Value.CriticalHandle != IntPtr.Zero)
            {
                PenContext[] ctxs = new PenContext[_contexts.Length + 1];
                uint preCopyCount = index;
                uint postCopyCount = (uint)_contexts.Length - index;

                Array.Copy(_contexts, 0, ctxs, 0, preCopyCount);
                PenContext newContext = _stylusLogic.TabletDevices[(int)index].As<WispTabletDevice>().CreateContext(_inputSource.Value.CriticalHandle, this);
                ctxs[index] = newContext;
                Array.Copy(_contexts, index, ctxs, index+1, postCopyCount);
                _contexts = ctxs;
                newContext.Enable();
            }
        }

        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified pen context index in response
        /// to the WM_TABLET_REMOVED notification
        /// </summary>
        internal void RemoveContext(uint index)
        {
            // We only tear down the old context when PenContexts are enabled without being
            // dispose and we have a valid index. Otherwise, no-op here.
            if (_contexts != null && index < _contexts.Length)
            {
                PenContext removeCtx = _contexts[index];

                PenContext[] ctxs = new PenContext[_contexts.Length - 1];
                uint preCopyCount = index;
                uint postCopyCount = (uint)_contexts.Length - index - 1;

                Array.Copy(_contexts, 0, ctxs, 0, preCopyCount);
                Array.Copy(_contexts, index+1, ctxs, index, postCopyCount);

                removeCtx.Disable(false); // shut down this context.

                _contexts = ctxs;
            }
        }

        /////////////////////////////////////////////////////////////////////

        internal object SyncRoot 
        {
            get
            {
                return __rtiLock;
            }
        } 


        internal void AddStylusPlugInCollection(StylusPlugInCollection pic)
        {
            // must be called from inside of lock(__rtiLock)
            
            // insert in ZOrder
            _plugInCollectionList.Insert(FindZOrderIndex(pic), pic); 
        }
        
        internal void RemoveStylusPlugInCollection(StylusPlugInCollection pic)
        {
            // must be called from inside of lock(__rtiLock)
            _plugInCollectionList.Remove(pic);
        }

        internal int FindZOrderIndex(StylusPlugInCollection spicAdding)
        {
            //should be called inside of lock(__rtiLock)
            DependencyObject spicAddingVisual = spicAdding.Element as Visual;
            int i;
            for (i=0; i < _plugInCollectionList.Count; i++)
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

        internal StylusPlugInCollection InvokeStylusPluginCollectionForMouse(RawStylusInputReport inputReport, IInputElement directlyOver, StylusPlugInCollection currentPlugInCollection)
        {
            StylusPlugInCollection newPlugInCollection = null;

            // lock to make sure only one event is processed at a time and no changes to state can
            // be made until we finish routing this event.
            lock(__rtiLock)
            {
                //Debug.Assert(inputReport.Actions == RawStylusActions.Down || 
                //             inputReport.Actions == RawStylusActions.Up ||
                //             inputReport.Actions == RawStylusActions.Move);
                
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
                    _stylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
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
                    
                    // We are on the pen thread, just call directly.
                    newPlugInCollection.FireRawStylusInput(rawStylusInput);

                    // Indicate we've used a stylus plugin
                    _stylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;

                    // Fire custom data events (always confirmed for mouse)
                    foreach (RawStylusInputCustomData customData in rawStylusInput.CustomDataList)
                    {
                        customData.Owner.FireCustomData(customData.Data, inputReport.Actions, true);
                    }
                }
            }
            return newPlugInCollection;
        }
        
        internal void InvokeStylusPluginCollection(RawStylusInputReport inputReport)
        {
            // Find PenContexts object for this inputReport.
            StylusPlugInCollection pic = null;

            // lock to make sure only one event is processed at a time and no changes to state can
            // be made until we finish routing this event.
            lock(__rtiLock)
            {
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

                WispStylusDevice stylusDevice = inputReport.StylusDevice.As<WispStylusDevice>();

                StylusPlugInCollection currentPic = stylusDevice.CurrentNonVerifiedTarget;
                
                // Fire Leave event if we need to.
                if (currentPic != null && currentPic != pic)
                {
                    // Create new RawStylusInput to send
                    GeneralTransformGroup transformTabletToView = new GeneralTransformGroup();
                    transformTabletToView.Children.Add(new MatrixTransform(_stylusLogic.GetTabletToViewTransform(stylusDevice.TabletDevice))); // this gives matrix in measured units (not device)
                    transformTabletToView.Children.Add(currentPic.ViewToElement); // Make it relative to the element.
                    transformTabletToView.Freeze(); // Must be frozen for multi-threaded access.
                    
                    RawStylusInput tempRawStylusInput = new RawStylusInput(inputReport, transformTabletToView, currentPic);
                    
                    currentPic.FireEnterLeave(false, tempRawStylusInput, false);

                    // Indicate we've used a stylus plugin
                    _stylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
                    stylusDevice.CurrentNonVerifiedTarget = null;
                }

                if (pic != null)
                {
                    // NOTE: PenContext info will not change (it gets rebuilt instead so keeping ref is fine)
                    //    The transformTabletToView matrix and plugincollection rects though can change based 
                    //    off of layout events which is why we need to lock this.
                    GeneralTransformGroup transformTabletToView = new GeneralTransformGroup();
                    transformTabletToView.Children.Add(new MatrixTransform(_stylusLogic.GetTabletToViewTransform(stylusDevice.TabletDevice))); // this gives matrix in measured units (not device)
                    transformTabletToView.Children.Add(pic.ViewToElement); // Make it relative to the element.
                    transformTabletToView.Freeze();  // Must be frozen for multi-threaded access.
                    
                    RawStylusInput rawStylusInput = new RawStylusInput(inputReport, transformTabletToView, pic);
                    inputReport.RawStylusInput = rawStylusInput;

                    if (pic != currentPic)
                    {
                        stylusDevice.CurrentNonVerifiedTarget = pic;
                        pic.FireEnterLeave(true, rawStylusInput, false);
                    }
                    
                    // We are on the pen thread, just call directly.
                    pic.FireRawStylusInput(rawStylusInput);

                    // Indicate we've used a stylus plugin
                    _stylusLogic.Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.StylusPluginsUsed;
                }
            } // lock(__rtiLock)
        }
        
        internal StylusPlugInCollection TargetPlugInCollection(RawStylusInputReport inputReport)
        {
            // Caller must make call to this routine inside of lock(__rtiLock)!
            StylusPlugInCollection pic = null;

            // We should only be called when not on the application thread!
            System.Diagnostics.Debug.Assert(!inputReport.StylusDevice.CheckAccess());

            WispStylusDevice stylusDevice = inputReport.StylusDevice.As<WispStylusDevice>();

            // We're on the pen thread so can't touch visual tree.  Use capturedPlugIn (if capture on) or cached rects.
            bool elementHasCapture = false;
            pic = stylusDevice.GetCapturedPlugInCollection(ref elementHasCapture);
            int pointLength = inputReport.PenContext.StylusPointDescription.GetInputArrayLengthPerPoint();
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
                ptTablet = _stylusLogic.MeasureUnitsFromDeviceUnits(ptTablet); // change to measured units now.

                pic = HittestPlugInCollection(ptTablet); // Use cached rectangles for UIElements.
            }
            return pic;
        }

        /////////////////////////////////////////////////////////////////////
        // NOTE: this should only be called inside of app Dispatcher
        internal StylusPlugInCollection FindPlugInCollection(UIElement element)
        {
            // Since we are only called on app Dispatcher this cannot change out from under us.
            // System.Diagnostics.Debug.Assert(_stylusLogic.Dispatcher.CheckAccess());

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

        /////////////////////////////////////////////////////////////////////
        // NOTE: this is called on pen thread (outside of apps Dispatcher)
        StylusPlugInCollection HittestPlugInCollection(Point pt)
        {
            // Caller must make call to this routine inside of lock(__rtiLock)!
            
            foreach (StylusPlugInCollection plugInCollection in _plugInCollectionList)
            {
                if (plugInCollection.IsHit(pt))
                {
                    return plugInCollection;
                }
            }

            return null;
        }

        /////////////////////////////////////////////////////////////////////

        internal SecurityCriticalData<HwndSource> _inputSource;

        WispLogic                      _stylusLogic;

        object                       __rtiLock = new object();
        List<StylusPlugInCollection> _plugInCollectionList = new List<StylusPlugInCollection>();

        PenContext[]        _contexts;
        
        bool                _isWindowDisabled;
        Point               _destroyedLocation = new Point(0,0);
    }
}

