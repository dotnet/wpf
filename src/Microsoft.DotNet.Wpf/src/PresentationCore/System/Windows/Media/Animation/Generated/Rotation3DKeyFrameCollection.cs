// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This collection is used in conjunction with a KeyFrameRotation3DAnimation
    /// to animate a Rotation3D property value along a set of key frames.
    /// </summary>
    public class Rotation3DKeyFrameCollection : Freezable, IList
    {
        #region Data

        private List<Rotation3DKeyFrame> _keyFrames;
        private static Rotation3DKeyFrameCollection s_emptyCollection;

        #endregion

        #region Constructors

        /// <Summary>
        /// Creates a new Rotation3DKeyFrameCollection.
        /// </Summary>
        public Rotation3DKeyFrameCollection()
            : base()
        {
            _keyFrames = new List< Rotation3DKeyFrame>(2);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// An empty Rotation3DKeyFrameCollection.
        /// </summary>
        public static Rotation3DKeyFrameCollection Empty
        {
            get
            {
                if (s_emptyCollection == null)
                {
                    Rotation3DKeyFrameCollection emptyCollection = new Rotation3DKeyFrameCollection();

                    emptyCollection._keyFrames = new List< Rotation3DKeyFrame>(0);
                    emptyCollection.Freeze();

                    s_emptyCollection = emptyCollection;
                }

                return s_emptyCollection;
            }
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Creates a freezable copy of this Rotation3DKeyFrameCollection.
        /// </summary>
        /// <returns>The copy</returns>
        public new Rotation3DKeyFrameCollection Clone()
        {
            return (Rotation3DKeyFrameCollection)base.Clone();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new Rotation3DKeyFrameCollection();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(System.Windows.Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            Rotation3DKeyFrameCollection sourceCollection = (Rotation3DKeyFrameCollection) sourceFreezable;
            base.CloneCore(sourceFreezable);

            int count = sourceCollection._keyFrames.Count;

            _keyFrames = new List< Rotation3DKeyFrame>(count);

            for (int i = 0; i < count; i++)
            {
                Rotation3DKeyFrame keyFrame = (Rotation3DKeyFrame)sourceCollection._keyFrames[i].Clone();
                _keyFrames.Add(keyFrame);
                OnFreezablePropertyChanged(null, keyFrame);
            }
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(System.Windows.Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            Rotation3DKeyFrameCollection sourceCollection = (Rotation3DKeyFrameCollection) sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            int count = sourceCollection._keyFrames.Count;

            _keyFrames = new List< Rotation3DKeyFrame>(count);

            for (int i = 0; i < count; i++)
            {
                Rotation3DKeyFrame keyFrame = (Rotation3DKeyFrame)sourceCollection._keyFrames[i].CloneCurrentValue();
                _keyFrames.Add(keyFrame);
                OnFreezablePropertyChanged(null, keyFrame);
            }
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(System.Windows.Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            Rotation3DKeyFrameCollection sourceCollection = (Rotation3DKeyFrameCollection) sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            int count = sourceCollection._keyFrames.Count;

            _keyFrames = new List< Rotation3DKeyFrame>(count);

            for (int i = 0; i < count; i++)
            {
                Rotation3DKeyFrame keyFrame = (Rotation3DKeyFrame)sourceCollection._keyFrames[i].GetAsFrozen();
                _keyFrames.Add(keyFrame);
                OnFreezablePropertyChanged(null, keyFrame);
            }
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(System.Windows.Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            Rotation3DKeyFrameCollection sourceCollection = (Rotation3DKeyFrameCollection) sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            int count = sourceCollection._keyFrames.Count;

            _keyFrames = new List< Rotation3DKeyFrame>(count);

            for (int i = 0; i < count; i++)
            {
                Rotation3DKeyFrame keyFrame = (Rotation3DKeyFrame)sourceCollection._keyFrames[i].GetCurrentValueAsFrozen();
                _keyFrames.Add(keyFrame);
                OnFreezablePropertyChanged(null, keyFrame);
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected override bool FreezeCore(bool isChecking)
        {
            bool canFreeze = base.FreezeCore(isChecking);

            for (int i = 0; i < _keyFrames.Count && canFreeze; i++)
            {
                canFreeze &= Freezable.Freeze(_keyFrames[i], isChecking);
            }

            return canFreeze;
        }

        #endregion

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator of the Rotation3DKeyFrames in the collection.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            ReadPreamble();

            return _keyFrames.GetEnumerator();
        }

        #endregion

        #region ICollection

        /// <summary>
        /// Returns the number of Rotation3DKeyFrames in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                ReadPreamble();

                return _keyFrames.Count;
            }
        }

        /// <summary>
        /// See <see cref="System.Collections.ICollection.IsSynchronized">ICollection.IsSynchronized</see>.
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                ReadPreamble();

                return (IsFrozen || Dispatcher != null);
            }
        }

        /// <summary>
        /// See <see cref="System.Collections.ICollection.SyncRoot">ICollection.SyncRoot</see>.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                ReadPreamble();

                return ((ICollection)_keyFrames).SyncRoot;
            }
        }

        /// <summary>
        /// Copies all of the Rotation3DKeyFrames in the collection to an
        /// array.
        /// </summary>
        void ICollection.CopyTo(Array array, int index)
        {
            ReadPreamble();

            ((ICollection)_keyFrames).CopyTo(array, index);
        }

        /// <summary>
        /// Copies all of the Rotation3DKeyFrames in the collection to an
        /// array of Rotation3DKeyFrames.
        /// </summary>
        public void CopyTo(Rotation3DKeyFrame[] array, int index)
        {
            ReadPreamble();

            _keyFrames.CopyTo(array, index);
        }

        #endregion

        #region IList

        /// <summary>
        /// Adds a Rotation3DKeyFrame to the collection.
        /// </summary>
        int IList.Add(object keyFrame)
        {
            return Add((Rotation3DKeyFrame)keyFrame);
        }

        /// <summary>
        /// Adds a Rotation3DKeyFrame to the collection.
        /// </summary>
        public int Add(Rotation3DKeyFrame keyFrame)
        {
            if (keyFrame == null)
            {
                throw new ArgumentNullException("keyFrame");
            }

            WritePreamble();

            OnFreezablePropertyChanged(null, keyFrame);
            _keyFrames.Add(keyFrame);

            WritePostscript();

            return _keyFrames.Count - 1;
        }

        /// <summary>
        /// Removes all Rotation3DKeyFrames from the collection.
        /// </summary>
        public void Clear()
        {
            WritePreamble();

            if (_keyFrames.Count > 0)
            {            
                for (int i = 0; i < _keyFrames.Count; i++)
                {
                    OnFreezablePropertyChanged(_keyFrames[i], null);
                }

                _keyFrames.Clear();

                WritePostscript();
            }
        }

        /// <summary>
        /// Returns true of the collection contains the given Rotation3DKeyFrame.
        /// </summary>
        bool IList.Contains(object keyFrame)
        {
            return Contains((Rotation3DKeyFrame)keyFrame);
        }

        /// <summary>
        /// Returns true of the collection contains the given Rotation3DKeyFrame.
        /// </summary>
        public bool Contains(Rotation3DKeyFrame keyFrame)
        {
            ReadPreamble();

            return _keyFrames.Contains(keyFrame);
        }

        /// <summary>
        /// Returns the index of a given Rotation3DKeyFrame in the collection. 
        /// </summary>
        int IList.IndexOf(object keyFrame)
        {
            return IndexOf((Rotation3DKeyFrame)keyFrame);
        }

        /// <summary>
        /// Returns the index of a given Rotation3DKeyFrame in the collection. 
        /// </summary>
        public int IndexOf(Rotation3DKeyFrame keyFrame)
        {
            ReadPreamble();

            return _keyFrames.IndexOf(keyFrame);
        }

        /// <summary>
        /// Inserts a Rotation3DKeyFrame into a specific location in the collection. 
        /// </summary>
        void IList.Insert(int index, object keyFrame)
        {
            Insert(index, (Rotation3DKeyFrame)keyFrame);
        }

        /// <summary>
        /// Inserts a Rotation3DKeyFrame into a specific location in the collection. 
        /// </summary>
        public void Insert(int index, Rotation3DKeyFrame keyFrame)
        {
            if (keyFrame == null)
            {
                throw new ArgumentNullException("keyFrame");
            }

            WritePreamble();

            OnFreezablePropertyChanged(null, keyFrame);
            _keyFrames.Insert(index, keyFrame);

            WritePostscript();
        }

        /// <summary>
        /// Returns true if the collection is frozen.
        /// </summary>
        public bool IsFixedSize
        {
            get
            {
                ReadPreamble();

                return IsFrozen;
            }
        }

        /// <summary>
        /// Returns true if the collection is frozen.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                ReadPreamble();

                return IsFrozen;
            }
        }

        /// <summary>
        /// Removes a Rotation3DKeyFrame from the collection.
        /// </summary>
        void IList.Remove(object keyFrame)
        {
            Remove((Rotation3DKeyFrame)keyFrame);
        }

        /// <summary>
        /// Removes a Rotation3DKeyFrame from the collection.
        /// </summary>
        public void Remove(Rotation3DKeyFrame keyFrame)
        {
            WritePreamble();

            if (_keyFrames.Contains(keyFrame))
            {
                OnFreezablePropertyChanged(keyFrame, null);
                _keyFrames.Remove(keyFrame);

                WritePostscript();
            }
        }

        /// <summary>
        /// Removes the Rotation3DKeyFrame at the specified index from the collection.
        /// </summary>
        public void RemoveAt(int index)
        {
            WritePreamble();

            OnFreezablePropertyChanged(_keyFrames[index], null);
            _keyFrames.RemoveAt(index);

            WritePostscript();
        }

        /// <summary>
        /// Gets or sets the Rotation3DKeyFrame at a given index.
        /// </summary>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (Rotation3DKeyFrame)value;
            }
        }

        /// <summary>
        /// Gets or sets the Rotation3DKeyFrame at a given index.
        /// </summary>
        public Rotation3DKeyFrame this[int index]
        {
            get
            {
                ReadPreamble();

                return _keyFrames[index];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(String.Format(CultureInfo.InvariantCulture, "Rotation3DKeyFrameCollection[{0}]", index));
                }

                WritePreamble();

                if (value != _keyFrames[index])
                {
                    OnFreezablePropertyChanged(_keyFrames[index], value);
                    _keyFrames[index] = value;

                    Debug.Assert(_keyFrames[index] != null);

                    WritePostscript();
                }
            }
        }

        #endregion
    }
}
