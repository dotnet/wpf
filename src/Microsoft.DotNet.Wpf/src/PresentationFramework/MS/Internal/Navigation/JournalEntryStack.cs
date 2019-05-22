// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

using MS.Internal;
using MS.Internal.AppModel;
using MS.Utility;
using System.ComponentModel;

namespace System.Windows.Navigation
{
    /// <summary>
    /// This is the base class for the JournalEntryBackStack and JournalEntryForwardStack
    /// classes.
    /// </summary>
    internal abstract class JournalEntryStack : IEnumerable, INotifyCollectionChanged
    {
        internal JournalEntryStack(Journal journal)
        {
            _journal = journal;
        }

        internal void OnCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        internal JournalEntryFilter Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        internal IEnumerable GetLimitedJournalEntryStackEnumerable()
        {
            if (_ljese == null)
            {
                _ljese = new LimitedJournalEntryStackEnumerable(this);
            }
            return _ljese;
        }

        LimitedJournalEntryStackEnumerable _ljese;
        protected JournalEntryFilter _filter;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public abstract IEnumerator GetEnumerator();
        protected Journal _journal;
    }

    /// <summary>
    /// This class exists to provide an IEnumerator over the BackStack.
    /// </summary>
    internal class JournalEntryBackStack : JournalEntryStack
    {
        public JournalEntryBackStack(Journal journal)
            : base(journal)
        {
        }

        public override IEnumerator GetEnumerator()
        {
            //Debug.WriteLine("Getting BackStack");
            return new JournalEntryStackEnumerator(_journal, _journal.CurrentIndex - 1, -1, this.Filter);
        }
    }

    /// <summary>
    /// This class exists to provide an IEnumerator over the ForwardStack.
    /// </summary>
    internal class JournalEntryForwardStack : JournalEntryStack
    {
        public JournalEntryForwardStack(Journal journal)
            : base(journal)
        {
        }

        public override IEnumerator GetEnumerator()
        {
            //Debug.WriteLine("Getting ForwardStack");
            return new JournalEntryStackEnumerator(_journal, _journal.CurrentIndex + 1, 1, this.Filter);
        }
    }

    /// <summary>
    /// This will enumerate over the navigable JournalEntries in the journal, starting at start,
    /// going in the direction of delta, and returning no more than _viewLimit values.
    /// This is used for display purposes.
    /// </summary>
    internal class JournalEntryStackEnumerator : IEnumerator
    {
        public JournalEntryStackEnumerator(Journal journal, int start, int delta, JournalEntryFilter filter)
        {
            _journal = journal;
            _version = journal.Version;
            _start = start;
            _delta = delta;
            _filter = filter;
            this.Reset();
        }

        public void Reset()
        {
            _next = _start;
            _current = null;
        }

        public bool MoveNext()
        {
            VerifyUnchanged();

            while ((_next >= 0) && (_next < _journal.TotalCount))
            {
                _current = _journal[_next];
                _next += _delta;
                if ((_filter == null) || _filter(_current))
                {
                    Debug.Assert(_current != null, "If we are returning true, our current cannot be null");
                    return true;
                }
            }

            _current = null;
            return false;
        }

        public object Current
        {
            get { return _current; }
        }

        /// <summary>
        /// Verifies that the journal has not been changed since this enumerator was created
        /// </summary>
        protected void VerifyUnchanged()
        {
            if (_version != _journal.Version)
            {
                throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));
            }
        }

        Journal _journal;
        int _start;
        int _delta;
        int _next;
        JournalEntry _current;
        JournalEntryFilter _filter;
        int _version;
    }

    internal class LimitedJournalEntryStackEnumerable : IEnumerable, INotifyCollectionChanged
    {
        internal LimitedJournalEntryStackEnumerable(IEnumerable ieble)
        {
            _ieble = ieble;
            INotifyCollectionChanged ichildnotify = ieble as INotifyCollectionChanged;
            if (ichildnotify != null)
            {
                ichildnotify.CollectionChanged += new NotifyCollectionChangedEventHandler(PropogateCollectionChanged);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new LimitedJournalEntryStackEnumerator(_ieble, DefaultMaxMenuEntries);
        }

        internal void PropogateCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        private const uint  DefaultMaxMenuEntries = 9; // the maximum number of items in the dropdown menus
        
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private IEnumerable _ieble;
    }

    internal class LimitedJournalEntryStackEnumerator : IEnumerator
    {
        internal LimitedJournalEntryStackEnumerator(IEnumerable ieble, uint viewLimit)
        {
            _ienum = ieble.GetEnumerator();
            _viewLimit = viewLimit;
        }

        public void Reset()
        {
            _itemsReturned = 0;
            _ienum.Reset();
        }

        public bool MoveNext()
        {
            bool success;
            if (_itemsReturned == _viewLimit)
            {
                success = false;
            }
            else
            {
                success = _ienum.MoveNext();
                if (success) 
                {
                    _itemsReturned++;
                }
            }
            return success;
        }

        public object Current
        {
            get { return _ienum.Current; }
        }
        
        private uint _itemsReturned;
        private uint _viewLimit;
        private IEnumerator _ienum;
    }
}
