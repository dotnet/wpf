// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the LinkTargetCollection as holder for a collection
//      of LinkTarget
//

namespace System.Windows.Documents
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows.Threading;
    using System.Windows.Markup;

    //=====================================================================
    /// <summary>
    /// LinkTarget is the class that keep name that a named element exist in document
    /// </summary>
    public sealed class LinkTarget
    {
        /// <summary>
        /// The element name
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private string _name;
    }

    //=====================================================================
    /// <summary>
    /// LinkTargetCollection is an ordered collection of LinkTarget
    /// It has to implement plain IList because the parser doesn't support 
    /// generics IList.
    /// </summary>
    public sealed class LinkTargetCollection : CollectionBase
    {
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        /// <summary>
        /// <!-- see cref="System.Collections.Generic.IList&lt;&gt;.this" / -->
        /// </summary>
        public LinkTarget this[int index]
        {
            get
            {
                return (LinkTarget)((IList)this)[index];
            }
            set
            {
                ((IList)this)[index] = value;
            }
        }

        /// <summary>
        /// <!-- see cref="System.Collections.Generic.ICollection&lt;&gt;.Add" / -->
        /// </summary>
        public int Add(LinkTarget value)
        {
            return ((IList)this).Add((object)value);
        }


        /// <summary>
        /// <!-- see cref="System.Collections.Generic.ICollection&lt;&gt;.Remove" / -->
        /// </summary>
        public void Remove(LinkTarget value)
        {
            ((IList)this).Remove((object) value);
        }

        /// <summary>
        /// <!-- see cref="System.Collections.Generic.ICollection&lt;&gt;.Contains" / -->
        /// </summary>
        public bool Contains(LinkTarget value)
        {
            return ((IList)this).Contains((object)value);
        }


        /// <summary>
        /// <!-- see cref="System.Collections.Generic.ICollection&lt;&gt;.CopyTo" / -->
        /// </summary>
        public void CopyTo(LinkTarget[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        /// <summary>
        /// <!-- see cref="System.Collections.Generic.IList&lt;&gt;.IndexOf" / -->
        /// </summary>
        public int IndexOf(LinkTarget value)
        {
            return ((IList)this).IndexOf((object)value);
        }


        /// <summary>
        /// <!-- see cref="System.Collections.Generic.IList&lt;&gt;.Insert" / -->
        /// </summary>
        public void Insert(int index, LinkTarget value)
        {
            ((IList)this).Insert(index, (object)value);
        }
    }
}