// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Input
{
    /// <summary>
    /// StylusPointDescription describes the properties that a StylusPoint supports.
    /// </summary>
    public class StylusPointDescription
    {
        /// <summary>
        /// Internal statics for our magic numbers
        /// </summary>
        internal static readonly int RequiredCountOfProperties = 3;
        internal static readonly int RequiredXIndex = 0;
        internal static readonly int RequiredYIndex = 1;
        internal static readonly int RequiredPressureIndex = 2;
        internal static readonly int MaximumButtonCount = 31;

        private int                         _buttonCount = 0;
        private int                         _originalPressureIndex = RequiredPressureIndex;
        private StylusPointPropertyInfo[]   _stylusPointPropertyInfos;

        /// <summary>
        /// StylusPointDescription
        /// </summary>
        public StylusPointDescription()
        {
            //implement the default packet description
            _stylusPointPropertyInfos = 
                new StylusPointPropertyInfo[]
                {
                    StylusPointPropertyInfoDefaults.X,
                    StylusPointPropertyInfoDefaults.Y,
                    StylusPointPropertyInfoDefaults.NormalPressure
                };
        }

        /// <summary>
        /// StylusPointDescription
        /// </summary>
        public StylusPointDescription(IEnumerable<StylusPointPropertyInfo> stylusPointPropertyInfos)
        {
            if (null == stylusPointPropertyInfos)
            {
                throw new ArgumentNullException("stylusPointPropertyInfos");
            }
            List<StylusPointPropertyInfo> infos =
                new List<StylusPointPropertyInfo>(stylusPointPropertyInfos);

            if (infos.Count < RequiredCountOfProperties ||
                infos[RequiredXIndex].Id != StylusPointPropertyIds.X ||
                infos[RequiredYIndex].Id != StylusPointPropertyIds.Y ||
                infos[RequiredPressureIndex].Id != StylusPointPropertyIds.NormalPressure)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidStylusPointDescription), "stylusPointPropertyInfos");
            }

            //
            // look for duplicates, validate that buttons are last
            //
            List<Guid> seenIds = new List<Guid>();
            seenIds.Add(StylusPointPropertyIds.X);
            seenIds.Add(StylusPointPropertyIds.Y);
            seenIds.Add(StylusPointPropertyIds.NormalPressure);

            int buttonCount = 0;
            for (int x = RequiredCountOfProperties; x < infos.Count; x++)
            {
                if (seenIds.Contains(infos[x].Id))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidStylusPointDescriptionDuplicatesFound), "stylusPointPropertyInfos");
                }
                if (infos[x].IsButton)
                {
                    buttonCount++;
                }
                else
                {
                    //this is not a button, make sure we haven't seen one before
                    if (buttonCount > 0)
                    {
                        throw new ArgumentException(SR.Get(SRID.InvalidStylusPointDescriptionButtonsMustBeLast), "stylusPointPropertyInfos");
                    }
                }
                seenIds.Add(infos[x].Id);
            }
            if (buttonCount > MaximumButtonCount)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidStylusPointDescriptionTooManyButtons), "stylusPointPropertyInfos");
            }

            _buttonCount = buttonCount;
            _stylusPointPropertyInfos = infos.ToArray();
        }

        /// <summary>
        /// StylusPointDescription
        /// </summary>
        /// <param name="stylusPointPropertyInfos">stylusPointPropertyInfos</param>
        /// <param name="originalPressureIndex">originalPressureIndex - does the digitizer really support pressure?  If so, the index this was at</param>
        internal StylusPointDescription(IEnumerable<StylusPointPropertyInfo> stylusPointPropertyInfos, int originalPressureIndex)
            : this (stylusPointPropertyInfos)
        {
            _originalPressureIndex = originalPressureIndex;
        }

        /// <summary>
        /// HasProperty
        /// </summary>
        /// <param name="stylusPointProperty">stylusPointProperty</param>
        public bool HasProperty(StylusPointProperty stylusPointProperty)
        {
            if (null == stylusPointProperty)
            {
                throw new ArgumentNullException("stylusPointProperty");
            }

            int index = IndexOf(stylusPointProperty.Id);
            if (-1 == index)
            {
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// The count of properties this StylusPointDescription contains
        /// </summary>
        public int PropertyCount
        {
            get { return _stylusPointPropertyInfos.Length; }
        }

        /// <summary>
        /// GetProperty
        /// </summary>
        /// <param name="stylusPointProperty">stylusPointProperty</param>
        public StylusPointPropertyInfo GetPropertyInfo(StylusPointProperty stylusPointProperty)
        {
            if (null == stylusPointProperty)
            {
                throw new ArgumentNullException("stylusPointProperty");
            }
            return GetPropertyInfo(stylusPointProperty.Id);
        }

        /// <summary>
        /// GetPropertyInfo
        /// </summary>
        /// <param name="guid">guid</param>
        internal StylusPointPropertyInfo GetPropertyInfo(Guid guid)
        {
            int index = IndexOf(guid);
            if (-1 == index)
            {
                //we didn't find it
                throw new ArgumentException("stylusPointProperty");
            }
            return _stylusPointPropertyInfos[index];
        }

        /// <summary>
        /// Returns the index of the given StylusPointProperty by ID, or -1 if none is found
        /// </summary>
        internal int GetPropertyIndex(Guid guid)
        {
            return IndexOf(guid);
        }

        /// <summary>
        /// GetStylusPointProperties
        /// </summary>
        public ReadOnlyCollection<StylusPointPropertyInfo> GetStylusPointProperties()
        {
            return new ReadOnlyCollection<StylusPointPropertyInfo>(_stylusPointPropertyInfos);
        }

        /// <summary>
        /// GetStylusPointPropertyIdss
        /// </summary>
        internal Guid[] GetStylusPointPropertyIds()
        {
            Guid[] ret = new Guid[_stylusPointPropertyInfos.Length];
            for (int x = 0; x < ret.Length; x++)
            {
                ret[x] = _stylusPointPropertyInfos[x].Id;
            }
            return ret;
        }

        /// <summary>
        /// Internal helper for determining how many ints in a raw int array
        /// correspond to one point we get from the input system
        /// </summary>
        internal int GetInputArrayLengthPerPoint()
        {
            int buttonLength = _buttonCount > 0 ? 1 : 0;
            int propertyLength = (_stylusPointPropertyInfos.Length - _buttonCount) + buttonLength;
            if (!this.ContainsTruePressure)
            {
                propertyLength--;
            }
            return propertyLength;
        }

        /// <summary>
        /// Internal helper for determining how many members a StylusPoint's
        /// internal int[] should be for additional data
        /// </summary>
        internal int GetExpectedAdditionalDataCount()
        {
            int buttonLength = _buttonCount > 0 ? 1 : 0;
            int expectedLength = ((_stylusPointPropertyInfos.Length - _buttonCount) + buttonLength) - 3 /*x, y, p*/;
            return expectedLength;
        }

        /// <summary>
        /// Internal helper for determining how many ints in a raw int array
        /// correspond to one point when saving to himetric
        /// </summary>
        /// <returns></returns>
        internal int GetOutputArrayLengthPerPoint()
        {
            int length = GetInputArrayLengthPerPoint();
            if (!this.ContainsTruePressure)
            {
                length++;
            }
            return length;
        }

        /// <summary>
        /// Internal helper for determining how many buttons are present
        /// </summary>
        internal int ButtonCount
        {
            get
            {
                return _buttonCount;
            }
        }

        /// <summary>
        /// Internal helper for determining what bit position the button is at
        /// </summary>
        internal int GetButtonBitPosition(StylusPointProperty buttonProperty)
        {
            if (!buttonProperty.IsButton)
            {
                throw new InvalidOperationException();
            }
            int buttonIndex = 0;
            for (int x = _stylusPointPropertyInfos.Length - _buttonCount; //start of the buttons
                 x < _stylusPointPropertyInfos.Length; x++)
            {
                if (_stylusPointPropertyInfos[x].Id == buttonProperty.Id)
                {
                    return buttonIndex;
                }
                if (_stylusPointPropertyInfos[x].IsButton)
                {
                    // we're in the buttons, but this isn't the right one,
                    // bump the button index and keep looking
                    buttonIndex++;
                }
            }
            return -1;
        }

        /// <summary>
        /// ContainsTruePressure - true if this StylusPointDescription was instanced
        /// by a TabletDevice or by ISF serialization that contains NormalPressure 
        /// </summary>
        internal bool ContainsTruePressure
        {
            get { return (_originalPressureIndex != -1); }
        }

        /// <summary>
        /// Internal helper to determine the original pressure index
        /// </summary>
        internal int OriginalPressureIndex
        {
            get { return _originalPressureIndex; }
        }

        /// <summary>
        /// Returns true if the two StylusPointDescriptions have the same StylusPointProperties.  Metrics are ignored.
        /// </summary>
        /// <param name="stylusPointDescription1">stylusPointDescription1</param>
        /// <param name="stylusPointDescription2">stylusPointDescription2</param>
        public static bool AreCompatible(StylusPointDescription stylusPointDescription1, StylusPointDescription stylusPointDescription2)
        {
            if (stylusPointDescription1 == null || stylusPointDescription2 == null)
            {
                throw new ArgumentNullException("stylusPointDescription");
            }

            #pragma warning disable 6506 // if a StylusPointDescription is not null, then _stylusPointPropertyInfos is not null.
            //
            // ignore X, Y, Pressure - they are guaranteed to be the first3 members
            //
            Debug.Assert(   stylusPointDescription1._stylusPointPropertyInfos.Length >= RequiredCountOfProperties &&
                            stylusPointDescription1._stylusPointPropertyInfos[0].Id == StylusPointPropertyIds.X &&
                            stylusPointDescription1._stylusPointPropertyInfos[1].Id == StylusPointPropertyIds.Y &&
                            stylusPointDescription1._stylusPointPropertyInfos[2].Id == StylusPointPropertyIds.NormalPressure);

            Debug.Assert(   stylusPointDescription2._stylusPointPropertyInfos.Length >= RequiredCountOfProperties &&
                            stylusPointDescription2._stylusPointPropertyInfos[0].Id == StylusPointPropertyIds.X &&
                            stylusPointDescription2._stylusPointPropertyInfos[1].Id == StylusPointPropertyIds.Y &&
                            stylusPointDescription2._stylusPointPropertyInfos[2].Id == StylusPointPropertyIds.NormalPressure);

            if (stylusPointDescription1._stylusPointPropertyInfos.Length != stylusPointDescription2._stylusPointPropertyInfos.Length)
            {
                return false;
            }
            for (int x = RequiredCountOfProperties; x < stylusPointDescription1._stylusPointPropertyInfos.Length; x++)
            {
                if (!StylusPointPropertyInfo.AreCompatible(stylusPointDescription1._stylusPointPropertyInfos[x], stylusPointDescription2._stylusPointPropertyInfos[x]))
                {
                    return false;
                }
            }
            #pragma warning restore 6506

            return true;
        }
        
        /// <summary>
        /// Returns a new StylusPointDescription with the common StylusPointProperties from both
        /// </summary>
        /// <param name="stylusPointDescription">stylusPointDescription</param>
        /// <param name="stylusPointDescriptionPreserveInfo">stylusPointDescriptionPreserveInfo</param>
        /// <remarks>The StylusPointProperties from stylusPointDescriptionPreserveInfo will be returned in the new StylusPointDescription</remarks>
        public static StylusPointDescription GetCommonDescription(StylusPointDescription stylusPointDescription, StylusPointDescription stylusPointDescriptionPreserveInfo)
        {
            if (stylusPointDescription == null)
            {
                throw new ArgumentNullException("stylusPointDescription");
            }
            if (stylusPointDescriptionPreserveInfo == null)
            {
                throw new ArgumentNullException("stylusPointDescriptionPreserveInfo");
            }


            #pragma warning disable 6506 // if a StylusPointDescription is not null, then _stylusPointPropertyInfos is not null.
            //
            // ignore X, Y, Pressure - they are guaranteed to be the first3 members
            //
            Debug.Assert(stylusPointDescription._stylusPointPropertyInfos.Length >= 3 &&
                            stylusPointDescription._stylusPointPropertyInfos[0].Id == StylusPointPropertyIds.X &&
                            stylusPointDescription._stylusPointPropertyInfos[1].Id == StylusPointPropertyIds.Y &&
                            stylusPointDescription._stylusPointPropertyInfos[2].Id == StylusPointPropertyIds.NormalPressure);

            Debug.Assert(stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos.Length >= 3 &&
                            stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[0].Id == StylusPointPropertyIds.X &&
                            stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[1].Id == StylusPointPropertyIds.Y &&
                            stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[2].Id == StylusPointPropertyIds.NormalPressure);


            //add x, y, p
            List<StylusPointPropertyInfo> commonProperties = new List<StylusPointPropertyInfo>();
            commonProperties.Add(stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[0]);
            commonProperties.Add(stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[1]);
            commonProperties.Add(stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[2]);

            //add common properties
            for (int x = RequiredCountOfProperties; x < stylusPointDescription._stylusPointPropertyInfos.Length; x++)
            {
                for (int y = RequiredCountOfProperties; y < stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos.Length; y++)
                {
                    if (StylusPointPropertyInfo.AreCompatible(  stylusPointDescription._stylusPointPropertyInfos[x], 
                                                            stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[y]))
                    {
                        commonProperties.Add(stylusPointDescriptionPreserveInfo._stylusPointPropertyInfos[y]);
                    }
                }
            }
            #pragma warning restore 6506
            
            return new StylusPointDescription(commonProperties);
        }

        /// <summary>
        /// Returns true if this StylusPointDescription is a subset 
        /// of the StylusPointDescription passed in
        /// </summary>
        /// <param name="stylusPointDescriptionSuperset">stylusPointDescriptionSuperset</param>
        /// <returns></returns>
        public bool IsSubsetOf(StylusPointDescription stylusPointDescriptionSuperset)
        {
            if (null == stylusPointDescriptionSuperset)
            {
                throw new ArgumentNullException("stylusPointDescriptionSuperset");
            }
            if (stylusPointDescriptionSuperset._stylusPointPropertyInfos.Length < _stylusPointPropertyInfos.Length)
            {
                return false;
            }
            //
            // iterate through our local properties and make sure that the 
            // superset contains them
            //
            for (int x = 0; x < _stylusPointPropertyInfos.Length; x++)
            {
                Guid id = _stylusPointPropertyInfos[x].Id;
                if (-1 == stylusPointDescriptionSuperset.IndexOf(id))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the index of the given StylusPointProperty, or -1 if none is found
        /// </summary>
        /// <param name="propertyId">propertyId</param>
        private int IndexOf(Guid propertyId)
        {
            for (int x = 0; x < _stylusPointPropertyInfos.Length; x++)
            {
                if (_stylusPointPropertyInfos[x].Id == propertyId)
                {
                    return x;
                }
            }
            return -1;
        }
    }
}
