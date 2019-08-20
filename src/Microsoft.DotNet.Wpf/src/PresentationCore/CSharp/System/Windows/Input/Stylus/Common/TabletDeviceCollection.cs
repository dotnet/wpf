// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections;
using System.Collections.Generic;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    ///		Collection of the tablet devices that are available on the machine.
    /// </summary>
    public class TabletDeviceCollection : ICollection, IEnumerable
    {
        #region Casting

        internal T As<T>()
            where T : TabletDeviceCollection
        {
            return this as T;
        }

        #endregion

        #region ICollection

        /// <summary>
        /// Retrieve the number of TabletDevice objects in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                if (TabletDevices == null)
                    throw new ObjectDisposedException(nameof(TabletDeviceCollection));

                return TabletDevices.Count;
            }
        }

        /// <summary>
        ///
        /// This is a vestigial function that isn't used anymore but needs to remain in
        /// order to implement ICollection.  Before this class implemented the CopyTo
        /// specific to an array of TabletDevice and used this solely, but the switch to
        /// List<TabletDevice> for the underlying collection no longer needs this.  The
        /// public interface must remain unchanged so we keep this anyway.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        void ICollection.CopyTo(Array array, int index)
        {
            Array.Copy(TabletDevices.ToArray(), 0, array, index, Count);
        }

        /// <summary>
        /// Copy the TabletDevice objects in the collection to another array
        /// of TabletDevices.
        /// </summary>
        /// <param name="array">destination array</param>
        /// <param name="index">position in destination array to begin copying</param>
        public void CopyTo(TabletDevice[] array, int index)
        {
            TabletDevices.CopyTo(array, index);
        }

        /// <summary>
        /// Retrieve the specified TabletDevice object from the collection.
        /// </summary>
        /// <param name="index">index of TabletDevice in collection to retrieve</param>
        public TabletDevice this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                    throw new ArgumentException(SR.Get(SRID.Stylus_IndexOutOfRange, index.ToString(System.Globalization.CultureInfo.InvariantCulture)), "index");

                return TabletDevices[index];
            }
        }

        /// <summary>
        /// Returns an object which can be used to lock during synchronization by collection users.
        /// <seealso cref="System.Collections.ICollection.SyncRoot"/>
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Determine if the collection has thread-safe operations.
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region IEnumerable

        /// <summary>
        /// Standard implementation of IEnumerable which enables callers to use the
        /// foreach construct to enumerate through each TabletDevice in the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return TabletDevices.GetEnumerator();
        }

        #endregion

        #region Properties

        internal List<TabletDevice> TabletDevices { get; set; } = new List<TabletDevice>();

        #endregion

        #region Static Properties

        private static TabletDeviceCollection _emptyTabletDeviceCollection;

        /// <summary>
        /// An empty tablet collection for use when there is no appropriate stylus logic instance.
        /// </summary>
        internal static TabletDeviceCollection EmptyTabletDeviceCollection
        {
            get
            {
                if(_emptyTabletDeviceCollection == null)
                {
                    _emptyTabletDeviceCollection = new TabletDeviceCollection();
                }

                return _emptyTabletDeviceCollection;
            }
        }

        #endregion
    }
}
