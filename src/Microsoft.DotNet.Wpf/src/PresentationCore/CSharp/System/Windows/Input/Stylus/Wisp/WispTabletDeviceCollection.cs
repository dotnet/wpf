// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Microsoft.Win32;
using MS.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Input.StylusWisp;
using System.Windows.Input.Tracing;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusWisp
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///		Collection of the tablet devices that are available on the machine.
    /// </summary>
    public class WispTabletDeviceCollection : TabletDeviceCollection
    {
        const int VistaMajorVersion = 6;

        /////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////
        internal WispTabletDeviceCollection()
        {
            WispLogic stylusLogic = StylusLogic.GetCurrentStylusLogicAs<WispLogic>();
            bool enabled = stylusLogic.Enabled;

            if (!enabled)
            {
                enabled = ShouldEnableTablets();
            }

            // If enabled or we are a tabletpc (vista sets dynamically if digitizers present) then enable the pen!
            if (enabled)
            {
                UpdateTablets(); // Create the tablet device collection!

                // Enable stylus input on all hwnds if we have not yet done so.
                if (!stylusLogic.Enabled)
                {
                    stylusLogic.EnableCore();
                }
            }
        }

        /// <summary>
        /// Checks if Tablets should be enabled.
        /// </summary>
        /// <returns></returns>
        internal static bool ShouldEnableTablets()
        {
            bool enabled = false;

            // We only want to enable by default if Wisptis is registered and tablets are detected.
            // We do the same detection on all OS versions.
            //
            // NOTE: This code does not support the new Vista wisptis feature to be able to exclude
            // tablet devices using a special registy key.  The only side effect of not supporting
            // this feature is that we would go ahead and load wisptis when we really don't need to
            // (since wisptis would not return us any tablet devices).  This should be a fairly rare
            // case though.
            //
            
            // Modify this with the new AppContext switch to disable the stylus stack.  This prevents
            // any tablet loads due to collection instantiation.
            if (StylusLogic.IsStylusAndTouchSupportEnabled
                && IsWisptisRegistered() 
                && HasTabletDevices())
            {
                enabled = true; // start up wisptis
            }

            return enabled;
        }

        private static bool IsWisptisRegistered()
        {
            bool fRegistered = false;
            RegistryKey key = null; // This object has finalizer to close the key.
            Object valDefault = null;

            bool runningOnVista = (Environment.OSVersion.Version.Major >= VistaMajorVersion);

            string keyToAssertPermissionFor =
                runningOnVista ?
                    "HKEY_CLASSES_ROOT\\Interface\\{C247F616-BBEB-406A-AED3-F75E656599AE}" :
                    "HKEY_CLASSES_ROOT\\CLSID\\{A5B020FD-E04B-4e67-B65A-E7DEED25B2CF}\\LocalServer32";

            string subkeyToOpen =
                runningOnVista ?
                    "Interface\\{C247F616-BBEB-406A-AED3-F75E656599AE}" :
                    "CLSID\\{A5B020FD-E04B-4e67-B65A-E7DEED25B2CF}\\LocalServer32";

            string valueToSearchFor =
                runningOnVista ?
                    "ITablet2" :
                    "wisptis.exe";

            key = Registry.ClassesRoot.OpenSubKey(subkeyToOpen);

            if (key != null)
            {
                valDefault = key.GetValue("");
            }

            if (key != null)
            {
                string sValDefault = valDefault as string;
                if (sValDefault != null && sValDefault.LastIndexOf(valueToSearchFor, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    fRegistered = true;
                }
                key.Close();
            }

            return fRegistered;
        }

        private static bool HasTabletDevices()
        {
            uint deviceCount = 0;
            // Determine the number of devices first (result will be -1 if fails and cDevices will have count)
            int result = (int)MS.Win32.UnsafeNativeMethods.GetRawInputDeviceList(null, ref deviceCount, (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUTDEVICELIST)));

            if (result >= 0 && deviceCount != 0)
            {
                NativeMethods.RAWINPUTDEVICELIST[] ridl = new NativeMethods.RAWINPUTDEVICELIST[deviceCount];
                int count = (int)MS.Win32.UnsafeNativeMethods.GetRawInputDeviceList(ridl, ref deviceCount, (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUTDEVICELIST)));

                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (ridl[i].dwType == NativeMethods.RIM_TYPEHID)
                        {
                            NativeMethods.RID_DEVICE_INFO deviceInfo = new NativeMethods.RID_DEVICE_INFO();
                            deviceInfo.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.RID_DEVICE_INFO));
                            uint cbSize = (uint)deviceInfo.cbSize;
                            int cBytes = (int)MS.Win32.UnsafeNativeMethods.GetRawInputDeviceInfo(ridl[i].hDevice, NativeMethods.RIDI_DEVICEINFO, ref deviceInfo, ref cbSize);

                            if (cBytes > 0)
                            {
                                if (deviceInfo.hid.usUsagePage == NativeMethods.HID_USAGE_PAGE_DIGITIZER)
                                {
                                    switch (deviceInfo.hid.usUsage)
                                    {
                                        case NativeMethods.HID_USAGE_DIGITIZER_DIGITIZER:
                                        case NativeMethods.HID_USAGE_DIGITIZER_PEN:
                                        case NativeMethods.HID_USAGE_DIGITIZER_TOUCHSCREEN:
                                        case NativeMethods.HID_USAGE_DIGITIZER_LIGHTPEN:
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("TabletDeviceCollection: GetRawInputDeviceInfo failed!");
                            }
                        }
                    }
                }
                else if (count < 0)
                {
                    System.Diagnostics.Debug.WriteLine("TabletDeviceCollection: GetRawInputDeviceList failed!");
                }
            }

            return false;
        }


        /////////////////////////////////////////////////////////////////////
        internal void UpdateTablets()
        {
            if (_tablets == null)
                throw new ObjectDisposedException("TabletDeviceCollection");

            // This method can be re-entered in a way that can cause deadlock
            // This can happen if multiple WM_DEVICECHANGE
            // messages are pending and we have not yet built the tablet collection.
            // Here's how:
            // 1. First WM_DEVICECHANGE message enters here, starts to launch pen thread
            // 2.   Penimc.UnsafeNativeMethods used for the first time, start its
            //              static constructor.
            // 3.     The static cctor starts to create a PimcManager via COM CLSID.
            // 4.       COM pumps message, a second WM_DEVICECHANGE re-enters here
            //              and starts to launch another pen thread.
            // 5.         The static cctor is skipped (although step 3 hasn't finished),
            //                  and the pen thread is started
            // 6.           The PenThreadWorker (on the UI thread) waits for the
            //                  PenThread to respond.
            // 7. Meanwhile the pen thread's code refers to Pimc.UnsafeNativeMethods.
            //      It's on a separate thread, so it waits for the static cctor to finish.
            // Deadlock:  UI thread is waiting for PenThread, but holding the CLR's
            //  lock on the static cctor.  The Pen thread is waiting for the static cctor.
            //
            // Multiple WM_DEVICECHANGE messages also cause re-entrancy even if the
            // static cctor has already finished.  There's no deadlock in that case,
            // but we end up with multiple pen threads (steps 1 and 4).  Besides being
            // redundant, that can cause problems when the threads collide with each
            // other or do work twice.
            //
            // In any case, the re-entrancy is harmful.  So we avoid it in the usual
            // way - setting a flag on entry and early-exit if the flag is set.
            //
            // Usually the outermost call will leave the tablet collection in the
            // right state, but there's a small chance that the OS or pen-input service
            // will change something while the outermost call is in progress, so that
            // the inner (re-entrant) call really would have picked up new information.
            // To handle that case, we'll simply re-run the outermost call if any
            // re-entrancy is detected.  Usually that will have no real effect, as
            // the code here detects when no change is made to the tablet collection.

            if (_inUpdateTablets)
            {
                // Log re-entrant calls
                StylusTraceLogger.LogReentrancy();

                // this is a re-entrant call.  Note that it happened, but do no work.
                _hasUpdateTabletsBeenCalledReentrantly = true;
                return;
            }

            try
            {
                _inUpdateTablets = true;
                do
                {
                    _hasUpdateTabletsBeenCalledReentrantly = false;

                    // do the real work
                    UpdateTabletsImpl();

                    // if re-entrancy happened, start over
                    // This could loop forever, but only if we get an unbounded
                    // number of re-entrant events;  that would have looped
                    // forever even without this re-entrancy logic.
                } while (_hasUpdateTabletsBeenCalledReentrantly);
            }
            finally
            {
                // when we're done (either normally or via exception)
                // reset the re-entrancy state
                _inUpdateTablets = false;
                _hasUpdateTabletsBeenCalledReentrantly = false;
            }
        }

        /////////////////////////////////////////////////////////////////////
        void UpdateTabletsImpl()
        {
            // REENTRANCY NOTE: Let a PenThread do this work to avoid reentrancy!
            //                  On return you get entire list of tablet and info needed to
            //                  create all the tablet devices (and stylus device info gets
            //                  cached too in penimc which avoids calls to wisptis.exe).

            // Use existing penthread if we have one otherwise grab an available one.
            PenThread penThread = _tablets.Length > 0 ? _tablets[0].As<WispTabletDevice>().PenThread :
                                                        PenThreadPool.GetPenThreadForPenContext(null);

            // There was an error acquiring a PenThread, do no work here.
            if (penThread == null)
            {
                Debug.Assert(false, "Error acquiring PenThread in UpdateTabletsImpl()");
                return;
            }

            TabletDeviceInfo [] tabletdevices = penThread.WorkerGetTabletsInfo();

            // First find out the index of the mouse device (usually the first at index 0)
            uint indexMouseTablet = UInt32.MaxValue;
            for (uint i = 0; i < tabletdevices.Length; i++)
            {
                // See if this is a bogus entry first.
                if (tabletdevices[i].PimcTablet == null) continue;

                // If it is the mouse tablet device we want to ignore it.
                if (tabletdevices[i].DeviceType == (TabletDeviceType)(-1))
                {
                    indexMouseTablet = i;
                    tabletdevices[i].PimcTablet = null; // ignore this one!
                }
            }

            // Now figure out count of valid tablet devices left
            uint count = 0;
            for (uint k = 0; k < tabletdevices.Length; k++)
            {
                if (tabletdevices[k].PimcTablet != null) count++;
            }

            TabletDevice[] tablets = new TabletDevice[count];

            uint tabletsIndex = 0;
            uint unchangedTabletCount = 0;
            for (uint iTablet = 0; iTablet < tabletdevices.Length; iTablet++)
            {
                if (tabletdevices[iTablet].PimcTablet == null)
                {
                    continue; // Skip looking at this index (mouse and bogus tablets are ignored).
                }

                int id = tabletdevices[iTablet].Id;

                // First see if same index has not changed (typical case)
                if (tabletsIndex < _tablets.Length && _tablets[tabletsIndex] != null && _tablets[tabletsIndex].Id == id)
                {
                    tablets[tabletsIndex] = _tablets[tabletsIndex];
                    _tablets[tabletsIndex] = null; // clear to ignore on cleanup pass.
                    unchangedTabletCount++;
                }
                else
                {
                    // Look up and see if we have this tabletdevice created already...
                    TabletDevice tablet = null;
                    for (uint i = 0; i < _tablets.Length; i++)
                    {
                        if (_tablets[i] != null && _tablets[i].Id == id)
                        {
                            tablet = _tablets[i];
                            _tablets[i] = null; // clear it so we don't dispose it.
                            break;
                        }
                    }
                    // Not found so create it.
                    if (tablet == null)
                    {
                        try
                        {
                            tablet = new TabletDevice(new WispTabletDevice(tabletdevices[iTablet], penThread));
                        }
                        catch (InvalidOperationException ex)
                        {
                            // This is caused by the Stylus ID not being unique when trying
                            // to register it in the StylusLogic.__stylusDeviceMap.  If we
                            // come across a dup then just ignore registering this tablet device.
                            // There seems to be an issue in wisptis where different tablet IDs get
                            // duplicate Stylus Ids when installing the VHID test devices.
                            if (ex.Data.Contains("System.Windows.Input.StylusLogic"))
                            {
                                continue; // Just go to next without adding this one.
                            }
                            else
                            {
                                throw; // not an expected exception, rethrow it.
                            }
                        }
                    }
                    tablets[tabletsIndex] = tablet;
                }

                tabletsIndex++;
}

            if (unchangedTabletCount == _tablets.Length &&
                unchangedTabletCount == tabletsIndex &&
                tabletsIndex == count)
            {
                // Keep the _tablet reference when nothing changes.
                // The reason is that if this method gets called from within
                // CreateContexts while looping on _tablets, it could result in
                // a null ref exception since the original _tablets array has
                // been purged to nulls.
                // NOTE: There is still the case of changing the ref (else case below)
                // when tablet devices actually change. But such a case is rare
                // (if not improbable) from within CreateContexts.
                Array.Copy(tablets, 0, _tablets, 0, count);
                _indexMouseTablet = indexMouseTablet;
            }
            else
            {
                // See if we need to re alloc the array due to invalid tabletdevice being seen.
                if (tabletsIndex != count)
                {
                    TabletDevice[] updatedTablets = new TabletDevice[tabletsIndex];
                    Array.Copy(tablets, 0, updatedTablets, 0, tabletsIndex);
                    tablets = updatedTablets;
                }

                DisposeTablets(); // Clean up any non null TabletDevice entries on old array.
                _tablets = tablets; // set updated tabletdevice array
                TabletDevices = new List<TabletDevice>(_tablets);
                _indexMouseTablet = indexMouseTablet;
            }

            // DevDiv:1078091
            // Any deferred tablet should be properly disposed of when applicable and
            // removed from the list of deferred tablets.
            DisposeDeferredTablets();
        }


        /////////////////////////////////////////////////////////////////////
        internal bool HandleTabletAdded(uint wisptisIndex, ref uint tabletIndexChanged)
        {
            if (_tablets == null)
                throw new ObjectDisposedException("TabletDeviceCollection");

            tabletIndexChanged = UInt32.MaxValue;

            // REENTRANCY NOTE: Let a PenThread do this work to avoid reentrancy!
            //                  On return you get the tablet info needed to
            //                  create a tablet devices (and stylus device info gets
            //                  cached in penimc too which avoids calls to wisptis.exe).

            // Use existing penthread if we have one otherwise grab an available one.
            PenThread penThread = _tablets.Length > 0 ? _tablets[0].As<WispTabletDevice>().PenThread :
                                                         PenThreadPool.GetPenThreadForPenContext(null);

            // There was an error acquiring a PenThread, return true to force a complete tablet refresh
            if (penThread == null)
            {
                Debug.Assert(false, "Error acquiring PenThread in HandleTabletAdded()");
                return true;
            }

            TabletDeviceInfo tabletInfo = penThread.WorkerGetTabletInfo(wisptisIndex);

            // If we failed due to a COM exception on the pen thread then return
            if (tabletInfo.PimcTablet == null)
            {
                return true; // make sure we rebuild our tablet collection. (return true + MaxValue).
            }

            // if mouse tabletdevice then ignore it.
            if (tabletInfo.DeviceType == (TabletDeviceType)(-1))
            {
                _indexMouseTablet = wisptisIndex; // update index.
                return false; // TabletDevices did not change.
            }

            // Now see if this is a duplicate add call we want to filter out (ie - already added to tablet collection).
            uint indexFound = UInt32.MaxValue;
            for (uint i = 0; i < _tablets.Length; i++)
            {
                // If it is the mouse tablet device we want to ignore it.
                if (_tablets[i].Id == tabletInfo.Id)
                {
                    indexFound = i;
                    break;
                }
            }

            // We only want to add this if it is not currently in the collection.  Wisptis will send
            // us duplicate adds at times so this is a work around for that issue.
            uint tabletIndex = UInt32.MaxValue;
            if (indexFound == UInt32.MaxValue)
            {
                tabletIndex = wisptisIndex;
                if (tabletIndex > _indexMouseTablet)
                {
                    tabletIndex--;
                }
                else
                {
                    _indexMouseTablet++;
                }

                // if index is out of range then ignore it.  Return of MaxValue causes a rebuild of the devices.
                if (tabletIndex <= _tablets.Length)
                {
                    try
                    {
                        // Add new tablet at end of collection
                        AddTablet(tabletIndex, new TabletDevice(new WispTabletDevice(tabletInfo, penThread)));
                    }
                    catch (InvalidOperationException ex)
                    {
                        // This is caused by the Stylus ID not being unique when trying
                        // to register it in the StylusLogic.__stylusDeviceMap.  If we
                        // come across a dup then we should rebuild the tablet device collection.
                        // There seems to be an issue in wisptis where different tablet IDs get
                        // duplicate Stylus Ids when installing the VHID test devices.
                        if (ex.Data.Contains("System.Windows.Input.StylusLogic"))
                        {
                            return true; // trigger the tabletdevices to be rebuilt.
                        }
                        else
                        {
                            throw; // not an expected exception, rethrow it.
                        }
                    }
                    tabletIndexChanged = tabletIndex;
                    return true;
                }
                else
                {
                    return true; // bogus index. Return true so that the caller can rebuild the collection.
                }
            }
            else
            {
                return false; // We found this tablet device already.  Don't do anything.
            }
        }

        /////////////////////////////////////////////////////////////////////
        internal uint HandleTabletRemoved(uint wisptisIndex)
        {
            if (_tablets == null)
                throw new ObjectDisposedException("TabletDeviceCollection");

            // if mouse tabletdevice then ignore it.
            if (wisptisIndex == _indexMouseTablet)
            {
                _indexMouseTablet = UInt32.MaxValue;
                return UInt32.MaxValue; // Don't process this notification any further.
            }

            uint tabletIndex = wisptisIndex;
            if (wisptisIndex > _indexMouseTablet)
            {
                tabletIndex--;
            }
            else
            {
                // Must be less than _indexMouseTablet since equality is done above.
                _indexMouseTablet--;
            }

            // if index is out of range then ignore it.
            if (tabletIndex >= _tablets.Length)
            {
                return UInt32.MaxValue; // Don't process this notification any further.
            }

            // Remove tablet from collection
            RemoveTablet(tabletIndex);

            return tabletIndex;
        }

        /////////////////////////////////////////////////////////////////////
        //  NOTE: This routine takes indexes that are in the TabletCollection range
        //        and not in the wisptis tablet index range.
        void AddTablet(uint index, TabletDevice tabletDevice)
        {
            Debug.Assert(index <= Count);
            Debug.Assert(tabletDevice.Type != (TabletDeviceType)(-1)); // make sure not the mouse tablet device!

            TabletDevice[] newTablets = new TabletDevice[Count + 1];

            uint preCopyCount = index;
            uint postCopyCount = (uint)_tablets.Length - index;

            Array.Copy(_tablets, 0, newTablets, 0, preCopyCount);
            newTablets[index] = tabletDevice;
            Array.Copy(_tablets, index, newTablets, index+1, postCopyCount);
            _tablets = newTablets;
            TabletDevices = new List<TabletDevice>(_tablets);
        }

        /////////////////////////////////////////////////////////////////////
        //  NOTE: This routine takes indexes that are in the TabletCollection range
        //        and not in the wisptis tablet index range.
        void RemoveTablet(uint index)
        {
            System.Diagnostics.Debug.Assert(index < Count && Count > 0);

            WispTabletDevice removeTablet = _tablets[index].As<WispTabletDevice>();

            TabletDevice[] tablets = new TabletDevice[_tablets.Length - 1];

            uint preCopyCount = index;
            uint postCopyCount = (uint)_tablets.Length - index - 1;

            Array.Copy(_tablets, 0, tablets, 0, preCopyCount);
            Array.Copy(_tablets, index+1, tablets, index, postCopyCount);

            _tablets = tablets;
            TabletDevices = new List<TabletDevice>(_tablets);

            // DevDiv:1078091
            // Dispose the tablet unless there is input waiting
            removeTablet.DisposeOrDeferDisposal();

            // This is now a deferred disposal, move it to the deferred list
            if (removeTablet.IsDisposalPending)
            {
                _deferredTablets.Add(removeTablet.TabletDevice);
            }
        }


        /////////////////////////////////////////////////////////////////////
        internal WispStylusDevice UpdateStylusDevices(int tabletId, int stylusId)
        {
            if (_tablets == null)
                throw new ObjectDisposedException("TabletDeviceCollection");

            for (int iTablet = 0, cTablets = _tablets.Length; iTablet < cTablets; iTablet++)
            {
                WispTabletDevice tablet = _tablets[iTablet].As<WispTabletDevice>();
                if (tablet.Id == tabletId)
                {
                    return tablet.UpdateStylusDevices(stylusId);
                }
            }
            return null;
        }

        /////////////////////////////////////////////////////////////////////
        internal void DisposeTablets()
        {
            if (_tablets != null)
            {
                for (int iTablet = 0, cTablets = _tablets.Length; iTablet < cTablets; iTablet++)
                {
                    if (_tablets[iTablet] != null)
                    {
                        WispTabletDevice removedTablet = _tablets[iTablet].TabletDeviceImpl.As<WispTabletDevice>();

                        // DevDiv:1078091
                        // Dispose the tablet unless there is input waiting
                        removedTablet.DisposeOrDeferDisposal();

                        // This is now a deferred disposal, move it to the deferred list
                        if (removedTablet.IsDisposalPending)
                        {
                            _deferredTablets.Add(removedTablet.TabletDevice);
                        }
                    }
                }
                _tablets = null;
                TabletDevices = null;
            }
        }

        /// <summary>
        /// DevDiv:1078091
        /// Dispose of and remove any tablets that had previously been deferred and
        /// can now be disposed.
        /// </summary>
        internal void DisposeDeferredTablets()
        {
            List<TabletDevice> tabletTemp = new List<TabletDevice>();

            foreach (TabletDevice tablet in _deferredTablets)
            {
                WispTabletDevice tabletDevice = tablet.TabletDeviceImpl.As<WispTabletDevice>();

                // Attempt disposal again
                tabletDevice.DisposeOrDeferDisposal();

                // If still deferred it was not disposed
                if (tabletDevice.IsDisposalPending)
                {
                    tabletTemp.Add(tablet);
                }
            }

            _deferredTablets = tabletTemp;
        }

        /////////////////////////////////////////////////////////////////////
        internal PenContext[] CreateContexts(IntPtr hwnd, PenContexts contexts)
        {
            int c = Count + _deferredTablets.Count;

            PenContext[] ctxs = new PenContext[c];

            int i = 0;

            foreach (TabletDevice tablet in _tablets)
            {
                ctxs[i++] = tablet.As<WispTabletDevice>().CreateContext(hwnd, contexts);
            }

            // DevDiv:1078091
            // We need to re-enable contexts for anything that is marked
            // as a pending disposal.  This is so we continue getting any
            // Wisp messages that might be waiting to come over the shared
            // memory channel.
            foreach (TabletDevice tablet in _deferredTablets)
            {
                ctxs[i++] = tablet.As<WispTabletDevice>().CreateContext(hwnd, contexts);
            }

            return ctxs;
        }

        /// <summary>
        /// A list of tablets that have pending disposals
        /// </summary>
        internal List<TabletDevice> DeferredTablets
        {
            get
            {
                return _deferredTablets;
            }
        }

        /////////////////////////////////////////////////////////////////////

        TabletDevice[]          _tablets = new TabletDevice[0];
        uint                    _indexMouseTablet = UInt32.MaxValue;
        bool                    _inUpdateTablets;       // detect re-entrancy
        bool                    _hasUpdateTabletsBeenCalledReentrantly;
        List<TabletDevice>      _deferredTablets = new List<TabletDevice>();
    }
}
