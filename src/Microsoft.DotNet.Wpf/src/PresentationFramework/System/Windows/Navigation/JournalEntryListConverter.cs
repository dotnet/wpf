// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implements the Converter to limit journal views to 9 items.
//
//
//

// Disable unknown #pragma warning for pragmas we use to exclude certain PreSharp violations
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Data;

using MS.Internal;

namespace System.Windows.Navigation
{
    /// <summary>
    /// This class returns an IEnumerable that is limited in length - it is used by the NavigationWindow
    /// style to limit how many entries are shown in the back and forward drop down buttons. Not
    /// intended to be used in any other situations.
    /// </summary>
    public sealed class JournalEntryListConverter : IValueConverter
    {
        /// <summary>
        /// This method from IValueConverter returns an IEnumerable which in turn will yield the
        /// ViewLimit limited collection of journal back and forward stack entries.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
#pragma warning disable  6506
            return (value != null) ? ((JournalEntryStack)value).GetLimitedJournalEntryStackEnumerable() : null;
#pragma warning restore 6506
        }

        /// <summary>
        /// This method from IValueConverter returns an IEnumerable which was originally passed to Convert
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    ///     Describes the position of the journal entry relative to the current page.
    /// </summary>
    public enum JournalEntryPosition
    {
        /// <summary>
        ///     The entry is on the BackStack
        /// </summary>
        Back,

        /// <summary>
        ///     The current page
        /// </summary>
        Current,

        /// <summary>
        ///     The entry on the ForwardStack
        /// </summary>
        Forward,
    }

    /// <summary>
    /// Puts all of the journal entries into a single list, for an IE7-style menu.
    /// </summary>
    public sealed class JournalEntryUnifiedViewConverter : IMultiValueConverter
    {
        /// <summary>
        ///     The DependencyProperty for the JournalEntryPosition property.
        /// </summary>
        public static readonly DependencyProperty JournalEntryPositionProperty =
                DependencyProperty.RegisterAttached(
                        "JournalEntryPosition",
                        typeof(JournalEntryPosition),
                        typeof(JournalEntryUnifiedViewConverter),
                        new PropertyMetadata(JournalEntryPosition.Current));

        /// <summary>
        ///     Helper for reading the JournalEntryPosition property.
        /// </summary>
        public static JournalEntryPosition GetJournalEntryPosition(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((JournalEntryPosition)element.GetValue(JournalEntryPositionProperty));
        }

        /// <summary>
        ///     Helper for setting the JournalEntryPosition property.
        /// </summary>
        public static void SetJournalEntryPosition(DependencyObject element, JournalEntryPosition position)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(JournalEntryPositionProperty, position);
        }


        /// <summary>
        /// This method from IValueConverter returns an IEnumerable which in turn will yield the
        /// single list containing all of the menu items.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values != null && values.Length == 2)
            {
                JournalEntryStack backStack = values[0] as JournalEntryStack;
                JournalEntryStack forwardStack = values[1] as JournalEntryStack;

                if (backStack != null && forwardStack != null)
                {
                    LimitedJournalEntryStackEnumerable limitedBackStack = (LimitedJournalEntryStackEnumerable)backStack.GetLimitedJournalEntryStackEnumerable();
                    LimitedJournalEntryStackEnumerable limitedForwardStack = (LimitedJournalEntryStackEnumerable)forwardStack.GetLimitedJournalEntryStackEnumerable();

                    return new UnifiedJournalEntryStackEnumerable(limitedBackStack, limitedForwardStack);
                }
            }
            
            return null;
        }

        /// <summary>
        /// This method is unused.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[] { Binding.DoNothing };
        }
    }

    // Merges LimitedBack and Forward Stack into one list and 
    internal class UnifiedJournalEntryStackEnumerable : IEnumerable, INotifyCollectionChanged
    {
        internal UnifiedJournalEntryStackEnumerable(LimitedJournalEntryStackEnumerable backStack, LimitedJournalEntryStackEnumerable forwardStack)
        {
            _backStack = backStack;
            _backStack.CollectionChanged += new NotifyCollectionChangedEventHandler(StacksChanged);

            _forwardStack = forwardStack;
            _forwardStack.CollectionChanged += new NotifyCollectionChangedEventHandler(StacksChanged);
        }

        public IEnumerator GetEnumerator()
        {
            if (_items == null)
            {
                // Reserve space so this will not have to reallocate. The most it will ever be is
                // 9 for the forward stack, 9 for the back stack, 1 for the title bar
                _items = new ArrayList(19);

                // Add ForwardStack in reverse order
                foreach (JournalEntry o in _forwardStack)
                {
                    _items.Insert(0, o);
                    JournalEntryUnifiedViewConverter.SetJournalEntryPosition(o, JournalEntryPosition.Forward);
                }

                DependencyObject current = new DependencyObject();
                current.SetValue(JournalEntry.NameProperty, SR.Get(SRID.NavWindowMenuCurrentPage));

                // "Current Page"
                _items.Add(current);

                foreach (JournalEntry o in _backStack)
                {
                    _items.Add(o);
                    JournalEntryUnifiedViewConverter.SetJournalEntryPosition(o, JournalEntryPosition.Back);
                }
            }

            return _items.GetEnumerator();
        }

        internal void StacksChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _items = null;
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private LimitedJournalEntryStackEnumerable _backStack, _forwardStack;
        private ArrayList _items;
    }
}
