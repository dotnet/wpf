// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Proxy for XLinq's XDeferredAxis class
//

/***************************************************************************\
    When binding to XLinq, a path like "Elements[Book]" natively returns an
    object of type XDeferredAxis<XElement>, an IEnumerable whose enumerator
    lists all the children with tagname "Book".  There are two problems with this:

    1. Every call to GetValue returns a new XDeferredAxis, even if no changes
        have occurred in the Xml tree.  There is no single object that represents
        "the children named 'Book'", which leads to confusion with collection views,
        sorting, filtering, grouping, etc.

    2. XDeferredAxis does not support collection change notifications.  This leads
        to problems when adding or removing nodes from the Xml tree.

    To work around these problems, we intercept calls to GetValue and return
    an XDeferredAxisSource instead.  We cache this result in the ValueTable, which
    solves (1).  XDeferredAxisSource's indexer return an ObservableCollection,
    which solves (2).  At each request to GetValue, we update the contents of
    the ObservableCollection, raising an Add or Remove event if possible, or a
    Reset if the contents have changed more violently.

    The trick is to do this all without actually mentioning XElement, XDeferredAxis,
    or other XLinq-defined types in the code, since we cannot take a build
    dependency on XLinq.  Reflection saves the day.
\***************************************************************************/

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.ObjectModel;       // ReadOnlyObservableCollection
using System.Collections.Specialized;       // HybridDictionary
using System.ComponentModel;                // PropertyDescriptor
using System.Globalization;                 // CultureInfo
using System.Reflection;                    // MemberInfo, PropertyInfo, etc.

namespace MS.Internal.Data
{
    internal sealed class XDeferredAxisSource
    {
        internal XDeferredAxisSource(object component, PropertyDescriptor pd)
        {
            _component = new WeakReference(component);
            _propertyDescriptor = pd;
            _table = new HybridDictionary();
        }

        public IEnumerable this[string name]
        {
            get
            {
                Record record = (Record)_table[name];

                if (record == null)
                {
                    object component = _component.Target;
                    if (component == null)
                        return null;

                    // initialize a new DC with the result of enumerating the
                    // XElement's children or descendants.  We have to re-fetch
                    // the XElement's property because XLinq's implementation
                    // throws if we query the same indexer with different
                    // arguments.
                    IEnumerable xda = _propertyDescriptor.GetValue(component) as IEnumerable;

                    if (xda != null && name != FullCollectionKey)
                    {
                        // call xelement.Elements[name] via reflection.  We know that
                        // xda really has type XDeferredAxis<T>, which has only one
                        // indexer (the one we want), so we use aryMembers[0] without
                        // much checking.  If XLinq changes their code, this could break.
                        MemberInfo[] aryMembers = xda.GetType().GetDefaultMembers();
                        Debug.Assert(aryMembers.Length == 1, "XLinq changed XDeferredAxis to have more than one indexer");
                        PropertyInfo pi = (aryMembers.Length > 0) ? aryMembers[0] as PropertyInfo : null;
                        xda = (pi == null) ? null :
                                    pi.GetValue(xda,
                                                BindingFlags.GetProperty, null,
                                                new object[] { name },
                                                CultureInfo.InvariantCulture)
                                        as IEnumerable;
                    }

                    record = new Record(xda);
                    _table[name] = record;
                }
                else
                {
                    // at each query, update the ObservableCollection to agree with
                    // the current contents of the XDeferredAxis, raising appropriate
                    // collection change events.
                    record.DC.Update(record.XDA);
                }

                return record.Collection;
            }
        }

        internal IEnumerable FullCollection
        {
            get { return this[FullCollectionKey]; }
        }

        WeakReference _component;     // the XElement of interest
        PropertyDescriptor _propertyDescriptor;    // the PD to obtain its elements or descendants
        HybridDictionary _table;         // table of results:  string -> <XDA, DC, Collection>
        const string FullCollectionKey = "%%FullCollection%%";      // not a legal XML tag name

        class Record
        {
            public Record(IEnumerable xda)
            {
                _xda = xda;
                if (xda != null)
                {
                    _dc = new DifferencingCollection(xda);
                    _rooc = new ReadOnlyObservableCollection<object>(_dc);
                }
            }

            public IEnumerable XDA { get { return _xda; } }
            public DifferencingCollection DC { get { return _dc; } }
            public ReadOnlyObservableCollection<object> Collection { get { return _rooc; } }

            IEnumerable _xda;   // the XDeferredAxis
            DifferencingCollection _dc;    // the corresponding ObservableCollection
            ReadOnlyObservableCollection<object> _rooc; // wrapper around the DC
        }
    }
}
