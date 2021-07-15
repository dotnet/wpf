// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description:
// Accessibility event map classes are used to determine if, and how many
// listeners there are for events and property changes.
//
//

using System;
using System.Collections;
using System.Windows;
using System.Diagnostics;

namespace MS.Internal.Automation
{
    // Manages the property map that is used to determine if there are
    // Automation clients interested in properties.
	internal class EventPropertyMap
	{
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // static class, private ctor to prevent creation
        private EventPropertyMap()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Called during queueing of property change events to check if there are any
        // listeners that are interested in this property.  Returns true if there are
        // else false.  This function could be called frequently; it must be fast.
        internal static bool IsInterestingDP(DependencyProperty dp)
        {
            using (_propertyLock.ReadLock)
            {
                if (_propertyTable != null && _propertyTable.ContainsKey(dp))
                {
                    return true;
                }
            }
            return false;
        }

        // Updates the list of DynamicProperties that are currently being listened to.
        // Called by AccEvent class when certain events are added.
        //
        // Returns: true if the event property map was created during this call and
        //          false if the property map was not created during this call.
        internal static bool AddPropertyNotify(DependencyProperty [] properties)
        {
            if (properties == null)
                return false;

            bool createdMap = false;
            using (_propertyLock.WriteLock)
            {
                // If it doesn't exist, create the property map (key=dp value=listener count)
                if (_propertyTable == null)
                {
                    // Up to 20 properties before resize and
                    // small load factor optimizes for speed
                    _propertyTable = new Hashtable(20, .1f);
                    createdMap = true;
                }

                int cDPStart = _propertyTable.Count;

                // properties is an array of the properties one listener is interested in
                foreach (DependencyProperty dp in properties)
                {
                    if (dp == null)
                        continue;

                    int cDP = 0;

                    // If the property is in the table, increment it's count
                    if (_propertyTable.ContainsKey(dp))
                    {
                        cDP = (int)_propertyTable[dp];
                    }

                    cDP++;
                    _propertyTable[dp] = cDP;
                }
            }

            return createdMap;
        }


        // Updates the list of DynamicProperties that are currently being listened to.
        // Called by AccEvent class when removing certain events.
        // Returns: true if the property table is empty after this operation.
        internal static bool RemovePropertyNotify(DependencyProperty [] properties)
        {
            Debug.Assert(properties != null);

            bool isEmpty = false;

            using (_propertyLock.WriteLock)
            {
                if (_propertyTable != null)
                {
                    int cDPStart = _propertyTable.Count;

                    // properties is an array of the properties one listener is no longer interested in
                    foreach (DependencyProperty dp in properties)
                    {
                        if (_propertyTable.ContainsKey(dp))
                        {
                            int cDP = (int)_propertyTable[dp];

                            // Update or remove the entry based on remaining listeners
                            cDP--;
                            if (cDP > 0)
                            {
                                _propertyTable[dp] = cDP;
                            }
                            else
                            {
                                _propertyTable.Remove(dp);
                            }
                        }
                    }

                    // If there are no more properties in the map then delete it; the idea is if there
                    // are no listeners, don't make the property system do extra work.
                    if (_propertyTable.Count == 0)
                    {
                        _propertyTable = null;
                    }
                }
                isEmpty = (_propertyTable == null);
            }

            return isEmpty;
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // static class, no instance data...

        private static ReaderWriterLockWrapper _propertyLock = new ReaderWriterLockWrapper();
        private static Hashtable _propertyTable;    // key=DP, data=listener count

        #endregion Private Fields
    }
}
