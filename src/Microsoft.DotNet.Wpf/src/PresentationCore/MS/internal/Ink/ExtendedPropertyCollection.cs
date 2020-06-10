// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using MS.Internal.Ink.InkSerializedFormat;
using System.Windows.Media;
using System.Reflection;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    /// <summary>
    /// A collection of name/value pairs, called ExtendedProperties, can be stored
    /// in a collection to enable aggregate operations and assignment to Ink object
    /// model objects, such StrokeCollection and Stroke.
    /// </summary>
    internal sealed class ExtendedPropertyCollection //does not implement ICollection, we don't need it
    {
        /// <summary>
        /// Create a new empty ExtendedPropertyCollection
        /// </summary>
        internal ExtendedPropertyCollection()
        {
        }

        /// <summary>Overload of the Equals method which determines if two ExtendedPropertyCollection
        /// objects contain equivalent key/value pairs</summary>
        public override bool Equals(object o)
        {
            if (o == null || o.GetType() != GetType())
            {
                return false;
            }

            //
            // compare counts
            //
            ExtendedPropertyCollection that = (ExtendedPropertyCollection)o;
            if (that.Count != this.Count)
            {
                return false;
            }

            //
            // counts are equal, compare individual items
            // 
            //
            for (int x = 0; x < that.Count; x++)
            {
                bool cont = false;
                for (int i = 0; i < _extendedProperties.Count; i++)
                {
                    if (_extendedProperties[i].Equals(that[x]))
                    {
                        cont = true;
                        break;
                    }
                }
                if (!cont)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Overload of the equality operator which determines
        /// if two ExtendedPropertyCollections are equal</summary>
        public static bool operator ==(ExtendedPropertyCollection first, ExtendedPropertyCollection second)
        {
            // compare the GC ptrs for the obvious reference equality
            if (((object)first == null && (object)second == null) ||
                ((object)first == (object)second))
            {
                return true;
            }
            // otherwise, if one of the ptrs are null, but not the other then return false
            else if ((object)first == null || (object)second == null)
            {
                return false;
            }
            // finally use the full `blown value-style comparison against the collection contents
            else
            {
                return first.Equals(second);
            }
        }

        /// <summary>Overload of the not equals operator to determine if two
        /// ExtendedPropertyCollections have different key/value pairs</summary>
        public static bool operator!=(ExtendedPropertyCollection first, ExtendedPropertyCollection second)
        {
            return !(first == second);
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Check to see if the attribute is defined in the collection. 
        /// </summary>
        /// <param name="attributeId">Attribute identifier</param>
        /// <returns>True if attribute is set in the mask, false otherwise</returns>
        internal bool Contains(Guid attributeId)
        {
            for (int x = 0; x < _extendedProperties.Count; x++)
            {
                if (_extendedProperties[x].Id == attributeId)
                {
                    //
                    // a typical pattern is to first check if 
                    // ep.Contains(guid)
                    // before accessing:
                    // object o = ep[guid];
                    //
                    // I'm caching the index that contains returns so that we
                    // can look there first for the guid in the indexer
                    //
                    _optimisticIndex = x;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the ExtendedPropertyCollection
        /// </summary>
        /// <returns>Copy of the ExtendedPropertyCollection</returns>
        /// <remarks>Any reference types held in the collection will only be deep copied (e.g. Arrays).
        /// </remarks>
        internal ExtendedPropertyCollection Clone()
        {
            ExtendedPropertyCollection copied = new ExtendedPropertyCollection();
            for (int x = 0; x < _extendedProperties.Count; x++)
            {
                copied.Add(_extendedProperties[x].Clone());
            }
            return copied;
        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="id">Id</param>
        /// <param name="value">value</param>
        internal void Add(Guid id, object value)
        {
            if (this.Contains(id))
            {
                throw new ArgumentException(SR.Get(SRID.EPExists), "id");
            }

            ExtendedProperty extendedProperty = new ExtendedProperty(id, value);

            //this will raise change events
            this.Add(extendedProperty);
        }


        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="id">id</param>
        internal void Remove(Guid id)
        {
            if (!Contains(id))
            {
                throw new ArgumentException(SR.Get(SRID.EPGuidNotFound), "id");
            }

            ExtendedProperty propertyToRemove = GetExtendedPropertyById(id);
            System.Diagnostics.Debug.Assert(propertyToRemove != null);

            _extendedProperties.Remove(propertyToRemove);

            //
            // this value is bogus now
            //
            _optimisticIndex = -1;

            // fire notification event
            if (this.Changed != null)
            {
                ExtendedPropertiesChangedEventArgs eventArgs
                    = new ExtendedPropertiesChangedEventArgs(propertyToRemove, null);
                this.Changed(this, eventArgs);
            }
        }

        /// <value>
        ///     Retrieve the Guid array of ExtendedProperty Ids  in the collection.
        ///     <paramref>Guid[]</paramref> is of type <see cref="System.Int32"/>.
        ///     <seealso cref="System.Collections.ICollection.Count"/>
        /// </value>
        internal Guid[] GetGuidArray()
        {
            if (_extendedProperties.Count > 0)
            {
                Guid[] guids = new Guid[_extendedProperties.Count];
                for (int i = 0; i < _extendedProperties.Count; i++)
                {
                    guids[i] = this[i].Id;
                }
                return guids;
            }
            else
            {
                return Array.Empty<Guid>();
            }
        }

        /// <summary>
        /// Generic accessor for the ExtendedPropertyCollection. 
        /// </summary>
        /// <param name="attributeId">Attribue Id to find</param>
        /// <returns>Value for attribute specified by Id</returns>
        /// <exception cref="System.ArgumentException">Specified identifier was not found</exception>
        /// <remarks>
        /// Note that you can access extended properties via this indexer.
        /// </remarks>
        internal object this[Guid attributeId]
        {
            get
            {
                ExtendedProperty ep = GetExtendedPropertyById(attributeId);
                if (ep == null)
                {
                    throw new ArgumentException(SR.Get(SRID.EPNotFound), "attributeId");
                }
                return ep.Value;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                for (int i = 0; i < _extendedProperties.Count; i++)
                {
                    ExtendedProperty currentProperty = _extendedProperties[i];

                    if (currentProperty.Id == attributeId)
                    {
                        object oldValue = currentProperty.Value;
                        //this will raise events
                        currentProperty.Value = value;

                        //raise change if anyone is listening
                        if (this.Changed != null)
                        {
                            ExtendedPropertiesChangedEventArgs eventArgs
                                = new ExtendedPropertiesChangedEventArgs(
                                    new ExtendedProperty(currentProperty.Id, oldValue), //old prop
                                    currentProperty);                                   //new prop

                            this.Changed(this, eventArgs);
                        }
                        return;
                    }
                }

                //
                //  we didn't find the Id in the collection, we need to add it.
                //  this will raise change notifications
                //
                ExtendedProperty attributeToAdd = new ExtendedProperty(attributeId, value);
                this.Add(attributeToAdd);
            }
        }

        /// <summary>
        /// Generic accessor for the ExtendedPropertyCollection. 
        /// </summary>
        /// <param name="index">index into masking collection to retrieve</param>
        /// <returns>ExtendedProperty specified at index</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Index was not found</exception>
        /// <remarks>
        /// Note that you can access extended properties via this indexer.
        /// </remarks>
        internal ExtendedProperty this[int index]
        {
            get
            {
                return _extendedProperties[index];
            }
        }

        /// <value>
        ///     Retrieve the number of ExtendedProperty objects in the collection.
        ///     <paramref>Count</paramref> is of type <see cref="System.Int32"/>.
        ///     <seealso cref="System.Collections.ICollection.Count"/>
        /// </value>
        internal int Count
        {
            get
            {
                return _extendedProperties.Count;
            }
        }

        /// <summary>
        /// Event fired whenever a ExtendedProperty is modified in the collection
        /// </summary>
        internal event ExtendedPropertiesChangedEventHandler Changed;


        /// <summary>
        /// private Add, we need to consider making this public in order to implement the generic ICollection
        /// </summary>
        private void Add(ExtendedProperty extendedProperty)
        {
            System.Diagnostics.Debug.Assert(!this.Contains(extendedProperty.Id), "ExtendedProperty already belongs to the collection");

            _extendedProperties.Add(extendedProperty);

            // fire notification event
            if (this.Changed != null)
            {
                ExtendedPropertiesChangedEventArgs eventArgs
                    = new ExtendedPropertiesChangedEventArgs(null, extendedProperty);
                this.Changed(this, eventArgs);
            }
        }

        /// <summary>
        /// Private helper for getting an EP out of our internal collection
        /// </summary>
        /// <param name="id">id</param>
        private ExtendedProperty GetExtendedPropertyById(Guid id)
        {
            //
            // a typical pattern is to first check if 
            // ep.Contains(guid)
            // before accessing:
            // object o = ep[guid];
            //
            // The last call to .Contains sets this index
            //
            if (_optimisticIndex != -1 &&
                _optimisticIndex < _extendedProperties.Count &&
                _extendedProperties[_optimisticIndex].Id == id)
            {
                return _extendedProperties[_optimisticIndex];
            }

                //we didn't find the ep optimistically, perform linear lookup
            for (int i = 0; i < _extendedProperties.Count; i++)
            {
                if (_extendedProperties[i].Id == id)
                {
                    return _extendedProperties[i];
                }
            }

            return null;
        }
        
        // the set of ExtendedProperties stored in this collection
        private List<ExtendedProperty> _extendedProperties = new List<ExtendedProperty>();

        
        //used to optimize across Contains / Index calls
        private int _optimisticIndex = -1;
    }
}
