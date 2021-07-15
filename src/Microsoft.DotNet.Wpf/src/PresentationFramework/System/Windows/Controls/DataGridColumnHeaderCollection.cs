// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Collection used as the ItemsSource of The DataGridColumnHeadersPresenter.  This is a wrapper around the Column collection; each item
    ///     returns the corresponding Column.Header.
    /// </summary>
    internal class DataGridColumnHeaderCollection : IEnumerable, INotifyCollectionChanged, IDisposable
    {
        public DataGridColumnHeaderCollection(ObservableCollection<DataGridColumn> columns)
        {
            _columns = columns;

            if (_columns != null)
            {
                _columns.CollectionChanged += OnColumnsChanged;
            }
        }

        public DataGridColumn ColumnFromIndex(int index)
        {
            if (index >= 0 && index < _columns.Count)
            {
                return _columns[index];
            }

            return null;
        }

        #region Notification Propagation

        /// <summary>
        /// Called when the Header property on a Column changes.  Causes us to fire a CollectionChanged event to specify that an item has been replaced.
        /// </summary>
        /// <param name="column"></param>
        internal void NotifyHeaderPropertyChanged(DataGridColumn column, DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(e.Property == DataGridColumn.HeaderProperty, "We only want to know about the header property changing");
            Debug.Assert(_columns.Contains(column));

            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Replace,
                e.NewValue,
                e.OldValue,
                _columns.IndexOf(column));

            FireCollectionChanged(args);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (_columns != null)
            {
                _columns.CollectionChanged -= OnColumnsChanged;
            }
        }

        #endregion 

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new ColumnHeaderCollectionEnumerator(_columns);
        }

        private class ColumnHeaderCollectionEnumerator : IEnumerator, IDisposable
        {
            public ColumnHeaderCollectionEnumerator(ObservableCollection<DataGridColumn> columns)
            {
                if (columns != null)
                {
                    _columns = columns;
                    _columns.CollectionChanged += OnColumnsChanged;
                }

                _current = -1;
            }

            #region IEnumerator Members

            public object Current
            {
                get
                {
                    if (IsValid)
                    {
                        DataGridColumn column = _columns[_current];

                        if (column != null)
                        {
                            return column.Header;
                        }

                        return null;
                    }

                    throw new InvalidOperationException();
                }
            }

            public bool MoveNext()
            {
                if (HasChanged)
                {
                    throw new InvalidOperationException();
                }

                if (_columns != null && _current < _columns.Count - 1)
                {
                    _current++;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                if (HasChanged)
                {
                    throw new InvalidOperationException();
                }

                _current = -1;
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                if (_columns != null)
                {
                    _columns.CollectionChanged -= OnColumnsChanged;
                }
            }

            #endregion 

            #region Helpers

            private bool HasChanged
            {
                get
                {
                    return _columnsChanged;
                }
            }

            private bool IsValid
            {
                get
                {
                    return _columns != null && _current >= 0 && _current < _columns.Count && !HasChanged;
                }
            }

            private void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                _columnsChanged = true;
            }

            #endregion 

            #region Data

            private int _current;
            private bool _columnsChanged;
            private ObservableCollection<DataGridColumn> _columns;

            #endregion
        }

        #endregion

        #region INotifyCollectionChanged Members

        /// <summary>
        /// INotifyCollectionChanged CollectionChanged event
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     Helper to raise a CollectionChanged event when the columns collection has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs newArgs;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    newArgs = new NotifyCollectionChangedEventArgs(e.Action, HeadersFromColumns(e.NewItems), e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    newArgs = new NotifyCollectionChangedEventArgs(e.Action, HeadersFromColumns(e.OldItems), e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Move:
                    newArgs = new NotifyCollectionChangedEventArgs(e.Action, HeadersFromColumns(e.OldItems), e.NewStartingIndex, e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    newArgs = new NotifyCollectionChangedEventArgs(e.Action, HeadersFromColumns(e.NewItems), HeadersFromColumns(e.OldItems), e.OldStartingIndex);
                    break;

                default:
                    newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
            }

            FireCollectionChanged(newArgs);
        }

        private void FireCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, args);
            }
        }
        
        private static object[] HeadersFromColumns(IList columns)
        {
            object[] headers = new object[columns.Count];

            for (int i = 0; i < columns.Count; i++)
            {
                DataGridColumn column = columns[i] as DataGridColumn;

                if (column != null)
                {
                    headers[i] = column.Header;
                }
                else
                {
                    headers[i] = null;
                }
            }

            return headers;
        }

        #endregion

        private ObservableCollection<DataGridColumn> _columns;
    }
}