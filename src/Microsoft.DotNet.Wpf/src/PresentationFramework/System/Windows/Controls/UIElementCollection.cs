// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the UIElementCollection base class.
//

using MS.Internal;
using System;
using System.Collections;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Markup;

namespace System.Windows.Controls
{
    /// <summary>
    /// A UIElementCollection is a ordered collection of UIElements.
    /// </summary>
    /// <remarks>
    /// A UIElementCollection has implied context affinity. It is a violation to access
    /// the collection from a different context than that of the owning Panel.
    /// </remarks>
    /// <seealso cref="System.Windows.Media.VisualCollection" />
    public class UIElementCollection : IList
    {
        /// <summary>
        ///     The colleciton is the children collection of the visualParent. The logicalParent 
        ///     is used to do logical parenting. The flags is used to invalidate 
        ///     the resource properties in the child tree, if an Application object exists. 
        /// </summary>
        /// <param name="visualParent">The element of whom this is a children collection</param>
        /// <param name="logicalParent">The logicalParent of the elements of this collection. 
        /// if overriding Panel.CreateUIElementCollection, pass the logicalParent parameter of that method here.
        /// </param>
        public UIElementCollection(UIElement visualParent, FrameworkElement logicalParent)
        {
            if (visualParent == null)
            {
                throw new ArgumentNullException(SR.Get(SRID.Panel_NoNullVisualParent, "visualParent", this.GetType()));
            }

            _visualChildren = new VisualCollection(visualParent);
            _visualParent = visualParent;
            _logicalParent = logicalParent;
        }


        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public virtual int Count
        {
            get { return _visualChildren.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the ICollection is synchronized (thread-safe).
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual bool IsSynchronized
        {
            get { return _visualChildren.IsSynchronized; }
        }


        /// <summary>
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual object SyncRoot
        {
            get { return _visualChildren.SyncRoot; }
        }

        /// <summary>
        /// Copies the collection into the Array.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual void CopyTo(Array array, int index)
        {
            _visualChildren.CopyTo(array, index);
        }

        /// <summary>
        /// Strongly typed version of CopyTo
        /// Copies the collection into the Array.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual void CopyTo(UIElement[] array, int index)
        {
            _visualChildren.CopyTo(array, index);
        }

        /// <summary>
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual int Capacity
        {
            get { return _visualChildren.Capacity; }
            set
            {
                VerifyWriteAccess();

                _visualChildren.Capacity = value;
            }
        }

        /// <summary>
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual UIElement this[int index]
        {
            get { return _visualChildren[index] as UIElement; }
            set
            {
                VerifyWriteAccess();
                ValidateElement(value);
                
                VisualCollection vc = _visualChildren;
                
                //if setting new element into slot or assigning null, 
                //remove previously hooked element from the logical tree
                if (vc[index] != value)
                {
                    UIElement e = vc[index] as UIElement;
                    if (e != null)
                        ClearLogicalParent(e);
                
                    vc[index] = value; 
  
                    SetLogicalParent(value);
                
                    _visualParent.InvalidateMeasure();
                }
            }
        }

        // Warning: this method is very dangerous because it does not prevent adding children 
        // into collection populated by generator. This may cause crashes if used incorrectly.
        // Don't call this unless you are deriving a panel that is populating the collection 
        // in cooperation with the generator
        internal void SetInternal(int index, UIElement item)
        {
            ValidateElement(item);
            
            VisualCollection vc = _visualChildren;
            
            if(vc[index] != item)
            {
                vc[index] = null; // explicitly disconnect the existing visual;
                vc[index] = item; 
            
                _visualParent.InvalidateMeasure();
            }        
        }

        
        /// <summary>
        /// Adds the element to the UIElementCollection
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual int Add(UIElement element)
        {
            VerifyWriteAccess();

            return AddInternal(element);
        }

        // Warning: this method is very dangerous because it does not prevent adding children 
        // into collection populated by generator. This may cause crashes if used incorrectly.
        // Don't call this unless you are deriving a panel that is populating the collection 
        // in cooperation with the generator
        internal int AddInternal(UIElement element)
        {
            ValidateElement(element);

            SetLogicalParent(element);
            int retVal = _visualChildren.Add(element);

            // invalidate measure on visual parent
            _visualParent.InvalidateMeasure();

            return retVal;
        }

        /// <summary>
        /// Returns the index of the element in the UIElementCollection
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual int IndexOf(UIElement element)
        {
            return _visualChildren.IndexOf(element);
        }

        /// <summary>
        /// Removes the specified element from the UIElementCollection.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual void Remove(UIElement element)
        {
            VerifyWriteAccess();

            RemoveInternal(element);
        }

        internal void RemoveInternal(UIElement element)
        {
            _visualChildren.Remove(element);
            ClearLogicalParent(element);
            _visualParent.InvalidateMeasure();
        }

        /// <summary>
        /// Removes the specified element from the UIElementCollection.
        /// Used only by ItemsControl and by VirtualizingStackPanel
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        internal virtual void RemoveNoVerify(UIElement element)
        {
            _visualChildren.Remove(element);
        }

        /// <summary>
        /// Determines whether a element is in the UIElementCollection.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual bool Contains(UIElement element)
        {
            return _visualChildren.Contains(element);
        }

        /// <summary>
        /// Removes all elements from the UIElementCollection.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual void Clear()
        {
            VerifyWriteAccess();

            ClearInternal();
        }


        // Warning: this method is very dangerous because it does not prevent adding children 
        // into collection populated by generator. This may cause crashes if used incorrectly.
        // Don't call this unless you are deriving a panel that is populating the collection 
        // in cooperation with the generator
        internal void ClearInternal()
        {
            VisualCollection vc = _visualChildren;
            int cnt = vc.Count;

            if (cnt > 0)
            {
                // copy children in VisualCollection so that we can clear the visual link first, 
                // followed by the logical link
                Visual[] visuals = new Visual[cnt];
                for (int i = 0; i < cnt; i++)
                {
                    visuals[i] = vc[i];
                }

                vc.Clear();

                //disconnect from logical tree
                for (int i = 0; i < cnt; i++)
                {
                    UIElement e = visuals[i] as UIElement;
                    if (e != null)
                    {
                        ClearLogicalParent(e);
                    }
                }

                _visualParent.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Inserts an element into the UIElementCollection at the specified index.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual void Insert(int index, UIElement element)
        {
            VerifyWriteAccess();

            InsertInternal(index, element);
        }

        // Warning: this method is very dangerous because it does not prevent adding children 
        // into collection populated by generator. This may cause crashes if used incorrectly.
        // Don't call this unless you are deriving a panel that is populating the collection 
        // in cooperation with the generator
        internal void InsertInternal(int index, UIElement element)
        {
            ValidateElement(element);

            SetLogicalParent(element);
            _visualChildren.Insert(index, element);
            _visualParent.InvalidateMeasure();
        }

        /// <summary>
        /// Removes the UIElement at the specified index.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual void RemoveAt(int index)
        {
            VerifyWriteAccess();

            VisualCollection vc = _visualChildren;

            //disconnect from logical tree
            UIElement e = vc[index] as UIElement;

            vc.RemoveAt(index);

            if (e != null)
                ClearLogicalParent(e);

            _visualParent.InvalidateMeasure();
        }


        /// <summary>
        /// Removes a range of Visuals from the VisualCollection.
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        public virtual void RemoveRange(int index, int count)
        {
            VerifyWriteAccess();

            RemoveRangeInternal(index, count);
        }

        // Warning: this method is very dangerous because it does not prevent adding children 
        // into collection populated by generator. This may cause crashes if used incorrectly.
        // Don't call this unless you are deriving a panel that is populating the collection 
        // in cooperation with the generator
        internal void RemoveRangeInternal(int index, int count)
        {
            VisualCollection vc = _visualChildren;
            int cnt = vc.Count;
            if (count > (cnt - index))
            {
                count = cnt - index;
            }

            if (count > 0)
            {
                // copy children in VisualCollection so that we can clear the visual link first, 
                // followed by the logical link
                Visual[] visuals = new Visual[count];
                int i = index;
                for (int loop = 0; loop < count; i++, loop++)
                {
                    visuals[loop] = vc[i];
                }

                vc.RemoveRange(index, count);

                //disconnect from logical tree
                for (i = 0; i < count; i++)
                {
                    UIElement e = visuals[i] as UIElement;
                    if (e != null)
                    {
                        ClearLogicalParent(e);
                    }
                }

                _visualParent.InvalidateMeasure();
            }
        }


        /// <summary>
        /// Method that forwards to VisualCollection.Move
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="destination"></param>
        internal void MoveVisualChild(Visual visual, Visual destination)
        {
            _visualChildren.Move(visual, destination);
        }
		
        private UIElement Cast(object value)
        {
            if (value == null)
                throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull, "UIElementCollection"));

            UIElement element = value as UIElement;

            if (element == null)
                throw new System.ArgumentException(SR.Get(SRID.Collection_BadType, "UIElementCollection", value.GetType().Name, "UIElement"));

            return element;
        }
		
        #region IList Members

        /// <summary>
        /// Adds an element to the UIElementCollection
        /// </summary>
        int IList.Add(object value)
        {
            return Add(Cast(value));
        }

        /// <summary>
        /// Determines whether an element is in the UIElementCollection.
        /// </summary>
        bool IList.Contains(object value)
        {
            return Contains(value as UIElement);
        }

        /// <summary>
        /// Returns the index of the element in the UIElementCollection
        /// </summary>
        int IList.IndexOf(object value)
        {
            return IndexOf(value as UIElement);
        }

        /// <summary>
        /// Inserts an element into the UIElementCollection
        /// </summary>
        void IList.Insert(int index, object value)
        {
            Insert(index, Cast(value));
        }

        /// <summary>
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// </summary>
        bool IList.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes an element from the UIElementCollection
        /// </summary>
        void IList.Remove(object value)
        {
            Remove(value as UIElement);
        }

        /// <summary>
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = Cast(value);
            }
        }

        #endregion


        // ----------------------------------------------------------------
        // IEnumerable Interface 
        // ----------------------------------------------------------------


        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>Enumerator that enumerates the collection in order.</returns>
        public virtual IEnumerator GetEnumerator()
        {
            return _visualChildren.GetEnumerator();
        }

        /// <summary>
        ///     This method does logical parenting of the given element.
        /// </summary>
        /// <param name="element"></param>
        protected void SetLogicalParent(UIElement element)
        {
            if (_logicalParent != null)
            {
                _logicalParent.AddLogicalChild(element);
            }
        }

        /// <summary>
        ///     This method removes logical parenting when element goes away from the collection.
        /// </summary>
        /// <param name="element"></param>
        protected void ClearLogicalParent(UIElement element)
        {
            if (_logicalParent != null)
            {
                _logicalParent.RemoveLogicalChild(element);
            }
        }

        /// <summary>
        /// Provides access to visual parent.
        /// </summary>
        internal UIElement VisualParent
        {
            get { return (_visualParent); }
        }

        // Helper function to validate element; will throw exceptions if problems are detected.
        private void ValidateElement(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(SR.Get(SRID.Panel_NoNullChildren, this.GetType()));
            }
        }

        private void VerifyWriteAccess()
        {
            Panel p = _visualParent as Panel;
            if (p != null && p.IsDataBound)
            {
                throw new InvalidOperationException(SR.Get(SRID.Panel_BoundPanel_NoChildren));
            }
        }

        internal FrameworkElement LogicalParent
        {
            get { return _logicalParent; }
        }

        private readonly VisualCollection _visualChildren;
        private readonly UIElement _visualParent;
        private readonly FrameworkElement _logicalParent;
    }
}
