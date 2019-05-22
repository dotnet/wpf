// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace System.Windows.Controls
{
    /// <summary>
    /// The event args class to be used with AutoGeneratingColumn event.
    /// </summary>
    public class DataGridAutoGeneratingColumnEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyType"></param>
        /// <param name="column"></param>
        public DataGridAutoGeneratingColumnEventArgs(string propertyName, Type propertyType, DataGridColumn column) :
            this(column, propertyName, propertyType, null)
        {
        }

        internal DataGridAutoGeneratingColumnEventArgs(DataGridColumn column, ItemPropertyInfo itemPropertyInfo) : 
            this(column, itemPropertyInfo.Name, itemPropertyInfo.PropertyType, itemPropertyInfo.Descriptor)
        {
        }

        internal DataGridAutoGeneratingColumnEventArgs(
            DataGridColumn column,
            string propertyName,
            Type propertyType,
            object propertyDescriptor)
        {
            _column = column;
            _propertyName = propertyName;
            _propertyType = propertyType;
            PropertyDescriptor = propertyDescriptor;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Column which is being generated
        /// </summary>
        public DataGridColumn Column
        {
            get
            {
                return _column;
            }

            set
            {
                _column = value;
            }
        }

        /// <summary>
        /// Property for which the column is getting generated
        /// </summary>
        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }

        /// <summary>
        /// Type of the property for which the column is getting generated
        /// </summary>
        public Type PropertyType
        {
            get
            {
                return _propertyType;
            }
        }

        /// <summary>
        /// Descriptor of the property for which the column is gettign generated
        /// </summary>
        public object PropertyDescriptor
        {
            get
            {
                return _propertyDescriptor;
            }

            private set
            {
                if (value == null)
                {
                    _propertyDescriptor = null;
                }
                else
                {
                    Debug.Assert(
                        typeof(PropertyDescriptor).IsAssignableFrom(value.GetType()) ||
                        typeof(PropertyInfo).IsAssignableFrom(value.GetType()),
                        "Property descriptor should be either a PropertyDescriptor or a PropertyInfo");
                    _propertyDescriptor = value;
                }
            }
        }

        /// <summary>
        /// Flag to indicated if generation of this column has to be cancelled
        /// </summary>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }

            set
            {
                _cancel = value;
            }
        }

        #endregion

        #region Data

        private DataGridColumn _column;
        private string _propertyName;
        private Type _propertyType;
        private object _propertyDescriptor;
        private bool _cancel;

        #endregion
    }
}
