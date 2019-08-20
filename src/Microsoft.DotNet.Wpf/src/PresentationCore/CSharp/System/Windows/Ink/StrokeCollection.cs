// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using MS.Utility;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows.Input;
using System.Runtime.Serialization;
using System.IO;
using System.Reflection;
using MS.Internal.Ink.InkSerializedFormat;
using MS.Internal.Ink;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

// Primary root namespace for TabletPC/Ink/Handwriting/Recognition in .NET

namespace System.Windows.Ink
{
    /// <summary>
    /// Collection of strokes objects which can be operated on in aggregate.
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(StrokeCollectionConverter))]
    public partial class StrokeCollection : Collection<Stroke>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        /// <summary>
        /// The string used to designate the native persistence format
        ///      for ink data (e.g. used on the clipboard)
        /// </summary>
        public static readonly String InkSerializedFormat = "Ink Serialized Format";

        /// <summary>Creates an empty stroke collection</summary>
        public StrokeCollection()
        {
        }

        /// <summary>Creates a StrokeCollection based on a collection of existing strokes</summary>
        public StrokeCollection(IEnumerable<Stroke> strokes)
        {
            if ( strokes == null )
            {
                throw new ArgumentNullException("strokes");
            }

            List<Stroke> items = (List<Stroke>)this.Items;

            //unfortunately we have to check for dupes with this ctor
            foreach ( Stroke stroke in strokes )
            {
                if ( items.Contains(stroke) )
                {
                    //clear and throw
                    items.Clear();
                    throw new ArgumentException(SR.Get(SRID.StrokeIsDuplicated), "strokes");
                }
                items.Add(stroke);
            }
        }

        /// <summary>Creates a collection from ISF data in the specified stream</summary>
        /// <param name="stream">Stream of ISF data</param>
        public StrokeCollection(Stream stream)
        {
            if ( stream == null )
            {
                throw new ArgumentNullException("stream");
            }
            if ( !stream.CanRead )
            {
                throw new ArgumentException(SR.Get(SRID.Image_StreamRead), "stream");
            }

            Stream seekableStream = GetSeekableStream(stream);
            if (seekableStream == null)
            {
                throw new ArgumentException(SR.Get(SRID.Invalid_isfData_Length), "stream");
            }

            //this will init our stroke collection
            StrokeCollectionSerializer serializer = new StrokeCollectionSerializer(this);
            serializer.DecodeISF(seekableStream);
        }


        /// <summary>Save the collection of strokes, including any custom attributes to a stream</summary>
        /// <param name="stream">The stream to save Ink Serialized Format to</param>
        /// <param name="compress">Flag if set to true the data will be compressed, which can
        /// reduce the output buffer size in exchange for slower Save performance.</param>
        public virtual void Save(Stream stream, bool compress)
        {
            if ( stream == null )
            {
                throw new ArgumentNullException("stream");
            }
            if ( !stream.CanWrite )
            {
                throw new ArgumentException(SR.Get(SRID.Image_StreamWrite), "stream");
            }
            SaveIsf(stream, compress);
        }

        /// <summary>Save the collection of strokes uncompressed, including any custom attributes to a stream</summary>
        /// <param name="stream">The stream to save Ink Serialized Format to</param>
        public void Save(Stream stream)
        {
            Save(stream, true);
        }

        /// <summary>
        /// Internal method for getting just the byte[] out
        /// </summary>
        internal void SaveIsf(Stream stream, bool compress)
        {
            StrokeCollectionSerializer serializer = new StrokeCollectionSerializer(this);
            serializer.CurrentCompressionMode = compress ? CompressionMode.Compressed : CompressionMode.NoCompression;
            serializer.EncodeISF(stream);
        }

        /// <summary>
        /// Private helper to read from a stream to the end and get a byte[]
        /// </summary>
        private Stream GetSeekableStream(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead);
            if ( stream.CanSeek )
            {
                int bytesToRead = (int)( stream.Length - stream.Position );
                if ( bytesToRead < 1 )
                {
                    return null; //nothing to read
                }
                //we can write to this
                return stream;
            }
            else
            {
                //Not all Stream implementations support Length.  Streams derived from 
                //NetworkStream and CryptoStream are the primary examples, but there are others
                //theyll throw NotSupportedException
                MemoryStream ms = new MemoryStream();

                int bufferSize = 4096;
                byte[] buffer = new byte[bufferSize];
                int count = bufferSize;
                while ( count == bufferSize )
                {
                    count = stream.Read(buffer, 0, 4096);
                    ms.Write(buffer, 0, count);
                }
                ms.Position = 0;
                return ms;
            }
        }

        /// <summary>
        /// Allows addition of objects to the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        /// <param name="propertyData"></param>
        public void AddPropertyData(Guid propertyDataId, object propertyData)
        {
            DrawingAttributes.ValidateStylusTipTransform(propertyDataId, propertyData);
            object oldValue = null;
            if ( ContainsPropertyData(propertyDataId) )
            {
                oldValue = GetPropertyData(propertyDataId);
                this.ExtendedProperties[propertyDataId] = propertyData;
            }
            else
            {
                this.ExtendedProperties.Add(propertyDataId, propertyData);
            }
            // fire notification
            OnPropertyDataChanged(new PropertyDataChangedEventArgs(propertyDataId, propertyData, oldValue));
        }

        /// <summary>
        /// Allows removal of objects from the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public void RemovePropertyData(Guid propertyDataId)
        {
            object propertyData = GetPropertyData(propertyDataId);
            this.ExtendedProperties.Remove(propertyDataId);
            // fire notification
            OnPropertyDataChanged(new PropertyDataChangedEventArgs(propertyDataId, null, propertyData));
        }

        /// <summary>
        /// Allows retrieval of objects from the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public object GetPropertyData(Guid propertyDataId)
        {
            if ( propertyDataId == Guid.Empty )
            {
                throw new ArgumentException(SR.Get(SRID.InvalidGuid), "propertyDataId");
            }

            return this.ExtendedProperties[propertyDataId];
        }

        /// <summary>
        /// Allows retrieval of a Array of guids that are contained in the EPC
        /// </summary>
        public Guid[] GetPropertyDataIds()
        {
            return this.ExtendedProperties.GetGuidArray();
        }

        /// <summary>
        /// Allows the checking of objects in the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public bool ContainsPropertyData(Guid propertyDataId)
        {
            return this.ExtendedProperties.Contains(propertyDataId);
        }

        /// <summary>
        /// Applies the specified transform matrix on every stroke in the collection.
        /// This method composes this transform with the existing
        /// transform on the stroke.</summary>
        /// <param name="transformMatrix">The transform to compose against each Stroke</param>
        /// <param name="applyToStylusTip">Boolean if true the transform matrix will be applied to StylusTip</param>
        /// <remarks>The StrokeCollection does not maintain a separate transform
        /// from each Stroke object. Calling Transform on the collection will
        /// cause each individual Stroke to be modified.
        /// If the StrokesChanged event fires, the changed parameter will be a pointer to 'this'
        /// collection, so any changes made to the changed event args will affect 'this' collection.</remarks>
        public void Transform(Matrix transformMatrix, bool applyToStylusTip)
        {
            // Ensure that the transformMatrix is invertible.
            if ( false == transformMatrix.HasInverse )
                throw new ArgumentException(SR.Get(SRID.MatrixNotInvertible), "transformMatrix");

            // if transformMatrix is identity or the StrokeCollection is empty
            //      then no change will occur anyway
            if ( transformMatrix.IsIdentity || Count == 0 )
            {
                return;
            }

            // Apply the transform to each strokes
            foreach ( Stroke stroke in this )
            {
                // samgeo - Presharp issue
                // Presharp gives a warning when get methods might deref a null.  It's complaining
                // here that 'stroke'' could be null, but StrokeCollection never allows nulls to be added
                // so this is not possible
#pragma warning disable 1634, 1691
#pragma warning suppress 6506
                stroke.Transform(transformMatrix, applyToStylusTip);
#pragma warning restore 1634, 1691
            }
        }

        /// <summary>
        /// Performs a deep copy of the StrokeCollection.
        /// </summary>
        public virtual StrokeCollection Clone()
        {
            StrokeCollection clone = new StrokeCollection();
            foreach ( Stroke s in this )
            {
                // samgeo - Presharp issue
                // Presharp gives a warning when get methods might deref a null.  It's complaining
                // here that s could be null, but StrokeCollection never allows nulls to be added
                // so this is not possible
#pragma warning disable 1634, 1691
#pragma warning suppress 6506
                clone.Add(s.Clone());
#pragma warning restore 1634, 1691
            }

            //
            // clone epc if we have them
            //
            if ( _extendedProperties != null )
            {
                clone._extendedProperties = _extendedProperties.Clone();
            }
            return clone;
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when the list is being cleared;
        /// raises a CollectionChanged event to any listeners
        /// </summary>
        protected override sealed void ClearItems()
        {
            if ( this.Count > 0 )
            {
                StrokeCollection removed = new StrokeCollection();
                for ( int x = 0; x < this.Count; x++ )
                {
                    ( (List<Stroke>)removed.Items ).Add(this[x]);
                }

                base.ClearItems();

                RaiseStrokesChanged(null /*added*/, removed, -1);
            }
        }

        /// <summary>
        /// called by base class RemoveAt or Remove methods
        /// </summary>
        protected override sealed void RemoveItem(int index)
        {
            Stroke removedStroke = this[index];
            base.RemoveItem(index);

            StrokeCollection removed = new StrokeCollection();
            ( (List<Stroke>)removed.Items ).Add(removedStroke);
            RaiseStrokesChanged(null /*added*/, removed, index);
        }

        /// <summary>
        /// called by base class Insert, Add methods
        /// </summary>
        protected override sealed void InsertItem(int index, Stroke stroke)
        {
            if ( stroke == null )
            {
                throw new ArgumentNullException("stroke");
            }
            if ( this.IndexOf(stroke) != -1 )
            {
                throw new ArgumentException(SR.Get(SRID.StrokeIsDuplicated), "stroke");
            }

            base.InsertItem(index, stroke);

            StrokeCollection addedStrokes = new StrokeCollection();
            ( (List<Stroke>)addedStrokes.Items ).Add(stroke);
            RaiseStrokesChanged(addedStrokes, null /*removed*/, index);
        }

        /// <summary>
        /// called by base class set_Item method
        /// </summary>
        protected override sealed void SetItem(int index, Stroke stroke)
        {
            if ( stroke == null )
            {
                throw new ArgumentNullException("stroke");
            }
            if ( IndexOf(stroke) != -1 )
            {
                throw new ArgumentException(SR.Get(SRID.StrokeIsDuplicated), "stroke");
            }

            Stroke removedStroke = this[index];
            base.SetItem(index, stroke);

            StrokeCollection removed = new StrokeCollection();
            ( (List<Stroke>)removed.Items ).Add(removedStroke);

            StrokeCollection added = new StrokeCollection();
            ( (List<Stroke>)added.Items ).Add(stroke);
            RaiseStrokesChanged(added, removed, index);
        }

        /// <summary>
        /// Gets the index of the stroke, or -1 if it is not found
        /// </summary>
        /// <param name="stroke">stroke</param>
        /// <returns></returns>
        public new int IndexOf(Stroke stroke)
        {
            if (stroke == null)
            {
                //we never allow null strokes
                return -1;
            }
            for (int i = 0; i < Count; i++)
            {
                if (object.ReferenceEquals(this[i], stroke))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Remove a set of Stroke objects to the collection
        /// </summary>
        /// <param name="strokes">The strokes to remove from the collection</param>
        /// <remarks>Changes to the collection trigger a StrokesChanged event.</remarks>
        public void Remove(StrokeCollection strokes)
        {
            if ( strokes == null )
            {
                throw new ArgumentNullException("strokes");
            }
            if ( strokes.Count == 0 )
            {
                // NOTICE-2004/06/08-WAYNEZEN:
                // We don't throw if an empty collection is going to be removed. And there is no event either.
                // This rule is also applied to invoking Clear() with an empty StrokeCollection.
                return;
            }

            int[] indexes = this.GetStrokeIndexes(strokes);
            if ( indexes == null )
            {
                // At least one stroke doesn't exist in our collection. We throw.
                ArgumentException ae = new ArgumentException(SR.Get(SRID.InvalidRemovedStroke), "strokes");
                //
                // we add a tag here so we can check for this in EraserBehavior.OnPointEraseResultChanged
                // to determine if this method is the origin of an ArgumentException we harden against
                //
                ae.Data.Add("System.Windows.Ink.StrokeCollection", "");
                throw ae;
            }

            for ( int x = indexes.Length - 1; x >= 0; x-- )
            {
                //bypass this.RemoveAt, which calls changed events
                //and call our protected List<Stroke> directly
                //remove from the back so the indexes are correct
                ( (List<Stroke>)this.Items ).RemoveAt(indexes[x]);
            }

            RaiseStrokesChanged(null /*added*/, strokes, indexes[0]);
        }

        /// <summary>
        /// Add a set of Stroke objects to the collection
        /// </summary>
        /// <param name="strokes">The strokes to add to the collection</param>
        /// <remarks>The items are added to the collection at the end of the list.
        /// If the item already exists in the collection, then the item is not added again.</remarks>
        public void Add(StrokeCollection strokes)
        {
            if ( strokes == null )
            {
                throw new ArgumentNullException("strokes");
            }
            if ( strokes.Count == 0 )
            {
                // NOTICE-2004/06/08-WAYNEZEN:
                // We don't throw if an empty collection is going to be added. And there is no event either.
                return;
            }

            int index = this.Count;

            //validate that none of the strokes exist in the collection
            for ( int x = 0; x < strokes.Count; x++ )
            {
                Stroke stroke = strokes[x];
                if ( this.IndexOf(stroke) != -1 )
                {
                    throw new ArgumentException(SR.Get(SRID.StrokeIsDuplicated), "strokes");
                }
            }

            //add the strokes
            //bypass this.AddRange, which calls changed events
            //and call our protected List<Stroke> directly
            ( (List<Stroke>)this.Items ).AddRange(strokes);

            RaiseStrokesChanged(strokes, null /*removed*/, index);
        }

        /// <summary>
        /// Replace
        /// </summary>
        /// <param name="strokeToReplace"></param>
        /// <param name="strokesToReplaceWith"></param>
        public void Replace(Stroke strokeToReplace, StrokeCollection strokesToReplaceWith)
        {
            if ( strokeToReplace == null )
            {
                throw new ArgumentNullException(SR.Get(SRID.EmptyScToReplace));
            }

            StrokeCollection strokesToReplace = new StrokeCollection();
            strokesToReplace.Add(strokeToReplace);
            this.Replace(strokesToReplace, strokesToReplaceWith);
        }

        /// <summary>
        /// Replace
        /// </summary>
        /// <param name="strokesToReplace"></param>
        /// <param name="strokesToReplaceWith"></param>
        public void Replace(StrokeCollection strokesToReplace, StrokeCollection strokesToReplaceWith)
        {
            if ( strokesToReplace == null )
            {
                throw new ArgumentNullException(SR.Get(SRID.EmptyScToReplace));
            }
            if ( strokesToReplaceWith == null )
            {
                throw new ArgumentNullException(SR.Get(SRID.EmptyScToReplaceWith));
            }

            int replaceCount = strokesToReplace.Count;
            if ( replaceCount == 0 )
            {
                ArgumentException ae = new ArgumentException(SR.Get(SRID.EmptyScToReplace), "strokesToReplace");
                //
                // we add a tag here so we can check for this in EraserBehavior.OnPointEraseResultChanged
                // to determine if this method is the origin of an ArgumentException we harden against
                //
                ae.Data.Add("System.Windows.Ink.StrokeCollection", "");
                throw ae;
            }

            int[] indexes = this.GetStrokeIndexes(strokesToReplace);
            if ( indexes == null )
            {
                // At least one stroke doesn't exist in our collection. We throw.
                ArgumentException ae = new ArgumentException(SR.Get(SRID.InvalidRemovedStroke), "strokesToReplace");
                //
                // we add a tag here so we can check for this in EraserBehavior.OnPointEraseResultChanged
                // to determine if this method is the origin of an ArgumentException we harden against
                //
                ae.Data.Add("System.Windows.Ink.StrokeCollection", "");
                throw ae;
            }


            //validate that none of the relplaceWith strokes exist in the collection
            for ( int x = 0; x < strokesToReplaceWith.Count; x++ )
            {
                Stroke stroke = strokesToReplaceWith[x];
                if ( this.IndexOf(stroke) != -1 )
                {
                    throw new ArgumentException(SR.Get(SRID.StrokeIsDuplicated), "strokesToReplaceWith");
                }
            }

            //bypass this.RemoveAt / InsertRange, which calls changed events
            //and call our protected List<Stroke> directly
            for ( int x = indexes.Length - 1; x >= 0; x-- )
            {
                //bypass this.RemoveAt, which calls changed events
                //and call our protected List<Stroke> directly
                //remove from the back so the indexes are correct
                ( (List<Stroke>)this.Items ).RemoveAt(indexes[x]);
            }

            if ( strokesToReplaceWith.Count > 0 )
            {
                //insert at the 
                ( (List<Stroke>)this.Items ).InsertRange(indexes[0], strokesToReplaceWith);
            }


            RaiseStrokesChanged(strokesToReplaceWith, strokesToReplace, indexes[0]);
        }

        /// <summary>
        /// called by StrokeCollectionSerializer during Load, bypasses Change notification
        /// </summary>
        internal void AddWithoutEvent(Stroke stroke)
        {
            Debug.Assert(stroke != null && IndexOf(stroke) == -1);
            ( (List<Stroke>)this.Items ).Add(stroke);
        }


        /// <summary>Collection of extended properties on this StrokeCollection</summary>
        internal ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                //
                // internal getter is used by the serialization code
                //
                if ( _extendedProperties == null )
                {
                    _extendedProperties = new ExtendedPropertyCollection();
                }

                return _extendedProperties;
            }
            private set
            {
                //
                // private setter used by copy
                //
                if ( value != null )
                {
                    _extendedProperties = value;
                }
            }
        }

        /// <summary>
        /// Event that notifies listeners whenever a change occurs in the set
        /// of stroke objects contained in the collection.
        /// </summary>
        /// <value>StrokeCollectionChangedEventHandler</value>
        public event StrokeCollectionChangedEventHandler StrokesChanged;

        /// <summary>
        /// Event that notifies internal listeners whenever a change occurs in the set
        /// of stroke objects contained in the collection.
        /// </summary>
        /// <value>StrokeCollectionChangedEventHandler</value>
        internal event StrokeCollectionChangedEventHandler StrokesChangedInternal;

        /// <summary>
        /// Event that notifies listeners whenever a change occurs in the propertyData
        /// </summary>
        /// <value>PropertyDataChangedEventHandler</value>
        public event PropertyDataChangedEventHandler PropertyDataChanged;

        /// <summary>
        /// INotifyPropertyChanged.PropertyChanged event, explicitly implemented
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        /// <summary>
        /// INotifyCollectionChanged.CollectionChanged event, explicitly implemented
        /// </summary>
        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add { _collectionChanged += value; }
            remove { _collectionChanged -= value; }
        }


        /// <summary>Method called on derived classes whenever a drawing attributes
        /// change has occurred in the stroke references in the collection</summary>
        /// <param name="e">The change information for the stroke collection</param>
        /// <remarks>StrokesChanged will not be called when drawing attributes or
        /// custom attributes are changed. Changes that trigger StrokesChanged
        /// include packets or points changing, modified tranforms, and stroke objects
        /// being added or removed from the collection.
        /// To ensure that events fire for event listeners, derived classes
        /// should call this method.</remarks>
        protected virtual void OnStrokesChanged(StrokeCollectionChangedEventArgs e)
        {
            if ( null == e )
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            //raise our internal event first.  This is used by
            //our Renderer and IncrementalHitTester since if they can assume
            //they are the first in the delegate chain, they can be optimized
            //to not have to handle out of order events caused by 3rd party code
            //getting called first
            if ( this.StrokesChangedInternal != null)
            {
                this.StrokesChangedInternal(this, e);
            }
            if ( this.StrokesChanged != null )
            {
                this.StrokesChanged(this, e);
            }
            if ( _collectionChanged != null )
            {
                //raise CollectionChanged.  We support the following 
                //NotifyCollectionChangedActions
                NotifyCollectionChangedEventArgs args = null;
                if ( this.Count == 0 )
                {
                    //Reset
                    Debug.Assert(e.Removed.Count > 0);
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                }
                else if ( e.Added.Count == 0 )
                {
                    //Remove
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.Removed, e.Index);
                }
                else if ( e.Removed.Count == 0 )
                {
                    //Add
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.Added, e.Index);
                }
                else
                {
                    //Replace
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.Added, e.Removed, e.Index);
                }
                _collectionChanged(this, args);
            }
        }


        /// <summary>
        /// Method called on derived classes whenever a change occurs in
        /// the PropertyData.
        /// </summary>
        /// <remarks>Derived classes should call this method (their base class)
        /// to ensure that event listeners are notified</remarks>
        protected virtual void OnPropertyDataChanged(PropertyDataChangedEventArgs e)
        {
            if ( null == e )
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            if ( this.PropertyDataChanged != null )
            {
                this.PropertyDataChanged(this, e);
            }
        }

        /// <summary>
        /// Method called when a property change occurs to the StrokeCollection
        /// </summary>
        /// <param name="e">The EventArgs specifying the name of the changed property.</param>
        /// <remarks>To follow the guidelines, this method should take a PropertyChangedEventArgs
        /// instance, but every other INotifyPropertyChanged implementation follows this pattern.</remarks>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if ( _propertyChanged != null )
            {
                _propertyChanged(this, e);
            }
        }


        /// <summary>
        /// Private helper that starts searching for stroke at index, 
        /// but will loop around before reporting -1.  This is used for 
        /// Stroke.Remove(StrokeCollection).  For example, if we're removing
        /// strokes, chances are they are in contiguous order.  If so, calling
        /// IndexOf to validate each stroke is O(n2).  If the strokes are in order
        /// this produces closer to O(n), if they are not in order, it is no worse
        /// </summary>
        private int OptimisticIndexOf(int startingIndex, Stroke stroke)
        {
            Debug.Assert(startingIndex >= 0);
            for ( int x = startingIndex; x < this.Count; x++ )
            {
                if ( this[x] == stroke )
                {
                    return x;
                }
            }

            //we didn't find anything on the first pass, now search the beginning
            for ( int x = 0; x < startingIndex; x++ )
            {
                if ( this[x] == stroke )
                {
                    return x;
                }
            }
            return -1;
        }

        /// <summary>
        /// Private helper that returns an array of indexes where the specified
        /// strokes exist in this stroke collection.  Returns null if at least one is not found.
        /// 
        /// The indexes are sorted from smallest to largest
        /// </summary>
        /// <returns></returns>
        private int[] GetStrokeIndexes(StrokeCollection strokes)
        {
            //to keep from walking the StrokeCollection twice for each stroke, we will maintain an index of
            //strokes to remove as we go
            int[] indexes = new int[strokes.Count];
            for ( int x = 0; x < indexes.Length; x++ )
            {
                indexes[x] = Int32.MaxValue;
            }

            int currentIndex = 0;
            int highestIndex = -1;
            int usedIndexCount = 0;
            for ( int x = 0; x < strokes.Count; x++ )
            {
                currentIndex = this.OptimisticIndexOf(currentIndex, strokes[x]);
                if ( currentIndex == -1 )
                {
                    //stroke doe3sn't exist, bail out.
                    return null;
                }

                //
                // optimize for the most common case... replace is passes strokes
                // in contiguous order.  Only do the sort if we need to
                //
                if ( currentIndex > highestIndex )
                {
                    //write current to the next available slot
                    indexes[usedIndexCount++] = currentIndex;
                    highestIndex = currentIndex;
                    continue;
                }

                //keep in sorted order (smallest to largest) with a simple insertion sort
                for ( int y = 0; y < indexes.Length; y++ )
                {
                    if ( currentIndex < indexes[y] )
                    {
                        if ( indexes[y] != Int32.MaxValue )
                        {
                            //shift from the end
                            for ( int i = indexes.Length - 1; i > y; i-- )
                            {
                                indexes[i] = indexes[i - 1];
                            }
                        }
                        indexes[y] = currentIndex;
                        usedIndexCount++;

                        if ( currentIndex > highestIndex )
                        {
                            highestIndex = currentIndex;
                        }
                        break;
                    }
                }
            }

            return indexes;
        }

        // This function will invoke OnStrokesChanged method.
        //      addedStrokes    -   the collection which contains the added strokes during the previous op.
        //      removedStrokes  -   the collection which contains the removed strokes during the previous op.
        private void RaiseStrokesChanged(StrokeCollection addedStrokes, StrokeCollection removedStrokes, int index)
        {
            StrokeCollectionChangedEventArgs eventArgs =
                new StrokeCollectionChangedEventArgs(addedStrokes, removedStrokes, index);

            // Invoke OnPropertyChanged
            OnPropertyChanged(CountName);
            OnPropertyChanged(IndexerName);

            // Invoke OnStrokesChanged which will fire the StrokesChanged event AND the CollectionChanged event.
            OnStrokesChanged(eventArgs);
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        // Custom 'user-defined' attributes assigned to this collection
        //  In v1, these were called Ink.ExtendedProperties
        private ExtendedPropertyCollection _extendedProperties = null;

        // The private PropertyChanged event
        private PropertyChangedEventHandler _propertyChanged;

        // private CollectionChanged event raiser
        private NotifyCollectionChangedEventHandler _collectionChanged;

        /// <summary>
        /// Constants for the PropertyChanged event
        /// </summary>
        private const string IndexerName = "Item[]";
        private const string CountName = "Count";

        //
        // Nested types...
        //

        /// <summary>
        /// ReadOnlyStrokeCollection - for StrokeCollection.StrokesChanged event args...
        /// </summary>
        internal class ReadOnlyStrokeCollection : StrokeCollection, ICollection<Stroke>, IList
        {
            internal ReadOnlyStrokeCollection(StrokeCollection strokeCollection)
            {
                if ( strokeCollection != null )
                {
                    ( (List<Stroke>)this.Items ).AddRange(strokeCollection);
                }
            }

            /// <summary>
            /// Change is not allowed.  We would override SetItem, InsertItem etc but 
            /// they need to be sealed on StrokeCollection to prevent dupes from being added
            /// </summary>
            /// <param name="e"></param>
            protected override void OnStrokesChanged(StrokeCollectionChangedEventArgs e)
            {
                throw new NotSupportedException(SR.Get(SRID.StrokeCollectionIsReadOnly));
            }

            /// <summary>
            /// IsReadOnly
            /// </summary>
            bool IList.IsReadOnly
            {
                get { return true; }
            }

            /// <summary>
            /// IsReadOnly
            /// </summary>
            bool ICollection<Stroke>.IsReadOnly
            {
                get { return true; }
            }
        }
    }
}
