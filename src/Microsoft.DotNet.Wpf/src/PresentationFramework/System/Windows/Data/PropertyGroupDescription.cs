// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Description of grouping based on a property value.
//
// See spec at Grouping.mht
//

using System;                   // StringComparison
using System.Collections;       // IComparer
using System.ComponentModel;    // [DefaultValue]
using System.Globalization;     // CultureInfo
using System.Reflection;        // PropertyInfo
using System.Windows;           // SR
using MS.Internal;              // XmlHelper

namespace System.Windows.Data
{
    /// <summary>
    /// Description of grouping based on a property value.
    /// </summary>
    public class PropertyGroupDescription : GroupDescription
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of PropertyGroupDescription.
        /// </summary>
        public PropertyGroupDescription()
        {
        }

        /// <summary>
        /// Initializes a new instance of PropertyGroupDescription.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property whose value is used to determine which group(s)
        /// an item belongs to.
        /// If PropertyName is null, the item itself is used.
        /// </param>
        public PropertyGroupDescription(string propertyName)
        {
            UpdatePropertyName(propertyName);
        }

        /// <summary>
        /// Initializes a new instance of PropertyGroupDescription.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property whose value is used to determine which group(s)
        /// an item belongs to.
        /// If PropertyName is null, the item itself is used.
        /// </param>
        /// <param name="converter">
        /// This converter is applied to the property value (or the item) to
        /// produce the final value used to determine which group(s) an item
        /// belongs to.
        /// If the delegate returns an ICollection, the item is added to
        /// multiple groups - one for each member of the collection.
        /// </param>
        public PropertyGroupDescription(string propertyName,
                                        IValueConverter converter)
        {
            UpdatePropertyName(propertyName);
            _converter = converter;
        }

        /// <summary>
        /// Initializes a new instance of PropertyGroupDescription.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property whose value is used to determine which group(s)
        /// an item belongs to.
        /// If PropertyName is null, the item itself is used.
        /// </param>
        /// <param name="converter">
        /// This converter is applied to the property value (or the item) to
        /// produce the final value used to determine which group(s) an item
        /// belongs to.
        /// If the delegate returns an ICollection, the item is added to
        /// multiple groups - one for each member of the collection.
        /// </param>
        /// <param name="stringComparison">
        /// This governs the comparison between an item's value (as determined
        /// by PropertyName and Converter) and a group's name.
        /// It is ignored unless both comparands are strings.
        /// The default value is StringComparison.Ordinal.
        /// </param>
        public PropertyGroupDescription(string propertyName,
                                        IValueConverter converter,
                                        StringComparison stringComparison)
        {
            UpdatePropertyName(propertyName);
            _converter = converter;
            _stringComparison = stringComparison;
        }

        #endregion Constructors

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// The name of the property whose value is used to determine which group(s)
        /// an item belongs to.
        /// If PropertyName is null, the item itself is used.
        /// </summary>
        [DefaultValue(null)]
        public string PropertyName
        {
            get { return _propertyName; }
            set
            {
                UpdatePropertyName(value);
                OnPropertyChanged("PropertyName");
            }
        }

        /// <summary>
        /// This converter is applied to the property value (or the item) to
        /// produce the final value used to determine which group(s) an item
        /// belongs to.
        /// If the delegate returns an ICollection, the item is added to
        /// multiple groups - one for each member of the collection.
        /// </summary>
        [DefaultValue(null)]
        public IValueConverter Converter
        {
            get { return _converter; }
            set { _converter = value; OnPropertyChanged("Converter"); }
        }

        /// <summary>
        /// This governs the comparison between an item's value (as determined
        /// by PropertyName and Converter) and a group's name.
        /// It is ignored unless both comparands are strings.
        /// The default value is StringComparison.Ordinal.
        /// </summary>
        [DefaultValue(StringComparison.Ordinal)]
        public StringComparison StringComparison
        {
            get { return _stringComparison; }
            set { _stringComparison = value; OnPropertyChanged("StringComparison"); }
        }

        /// <summary>
        /// A comparer that orders groups in ascending order of Name.
        /// This can be used as a value for GroupDescription.CustomSort
        /// </summary>
        public static IComparer CompareNameAscending
        {
            get { return _compareNameAscending; }
        }

        /// <summary>
        /// A comparer that orders groups in descending order of Name.
        /// This can be used as a value for GroupDescription.CustomSort
        /// </summary>
        public static IComparer CompareNameDescending
        {
            get { return _compareNameDescending; }
        }

        #endregion Public Properties

        #region Public Methods

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Return the group name(s) for the given item
        /// </summary>
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            object value;
            object xmlValue;

            // get the property value
            if (String.IsNullOrEmpty(PropertyName))
            {
                value = item;
            }
            else if (SystemXmlHelper.TryGetValueFromXmlNode(item, PropertyName, out xmlValue))
            {
                value = xmlValue;
            }
            else if (item != null)
            {
                using (_propertyPath.SetContext(item))
                {
                    value = _propertyPath.GetValue();
                }
            }
            else
            {
                value = null;
            }

            // apply the converter to the value
            if (Converter != null)
            {
                value = Converter.Convert(value, typeof(object), level, culture);
            }

            return value;
        }

        /// <summary>
        /// Return true if the names match (i.e the item should belong to the group).
        /// </summary>
        public override bool NamesMatch(object groupName, object itemName)
        {
            string s1 = groupName as string;
            string s2 = itemName as string;

            if (s1 != null && s2 != null)
            {
                return String.Equals(s1, s2, StringComparison);
            }
            else
            {
                return Object.Equals(groupName, itemName);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void UpdatePropertyName(string propertyName)
        {
            _propertyName = propertyName;
            _propertyPath = !String.IsNullOrEmpty(propertyName) ? new PropertyPath(propertyName) : null;
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion Private Methods

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        string              _propertyName;
        PropertyPath        _propertyPath;
        IValueConverter     _converter;
        StringComparison    _stringComparison = StringComparison.Ordinal;
        static readonly IComparer _compareNameAscending = new NameComparer(ListSortDirection.Ascending);
        static readonly IComparer _compareNameDescending = new NameComparer(ListSortDirection.Descending);

        #endregion Private Fields

        #region Private Types

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        class NameComparer : IComparer
        {
            public NameComparer(ListSortDirection direction)
            {
                _direction = direction;
            }

            int IComparer.Compare(object x, object y)
            {
                CollectionViewGroup group;
                object xName, yName;

                group = x as CollectionViewGroup;
                xName = group?.Name ?? x;

                group = y as CollectionViewGroup;
                yName = group?.Name ?? y;

                int value = Comparer.DefaultInvariant.Compare(xName, yName);
                return (_direction == ListSortDirection.Ascending) ? value : -value;
            }

            ListSortDirection _direction;
        }

        #endregion Private Types
    }
}

