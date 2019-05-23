// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Helper methods for code that uses types from System.Data.

using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;

namespace MS.Internal
{
    //FxCop can't tell that this class is instantiated via reflection, so suppress the FxCop warning.
    [SuppressMessage("Microsoft.Performance","CA1812:AvoidUninstantiatedInternalClasses")]
    internal class SystemDataExtension : SystemDataExtensionMethods
    {
        // return true if the list is a DataView
        internal override bool IsDataView(IBindingList list)
        {
            return (list is DataView);
        }

        // return true if the item is a DataRowView
        internal override bool IsDataRowView(object item)
        {
            return (item is DataRowView);
        }

        // return true if the value is null in the SqlTypes sense
        internal override bool IsSqlNull(object value)
        {
            INullable nullable = value as INullable;
            return (nullable != null && nullable.IsNull);
        }

        // return true if the type is nullable in the SqlTypes sense
        internal override bool IsSqlNullableType(Type type)
        {
            return typeof(INullable).IsAssignableFrom(type);
        }

        // ADO DataSet exposes some properties that cause problems involving
        // identity and change notifications.  We handle these specially.
        internal override bool IsDataSetCollectionProperty(PropertyDescriptor pd)
        {
            if (s_DataTablePropertyDescriptorType == null)
            {
                // lazy load the types for the offending PD's.  They're internal, so
                // we get them indirectly.
                DataSet dataset = new DataSet();
                dataset.Locale = System.Globalization.CultureInfo.InvariantCulture;

                DataTable table1 = new DataTable("Table1");
                table1.Locale = System.Globalization.CultureInfo.InvariantCulture;
                table1.Columns.Add("ID", typeof(int));
                dataset.Tables.Add(table1);

                DataTable table2 = new DataTable("Table2");
                table2.Locale = System.Globalization.CultureInfo.InvariantCulture;
                table2.Columns.Add("ID", typeof(int));
                dataset.Tables.Add(table2);

                dataset.Relations.Add(new DataRelation("IDRelation",
                                                    table1.Columns["ID"],
                                                    table2.Columns["ID"]));

                System.Collections.IList list = ((IListSource)dataset).GetList();
                PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(list[0]);
                s_DataTablePropertyDescriptorType = pdc["Table1"].GetType();

                pdc = ((ITypedList)table1.DefaultView).GetItemProperties(null);
                s_DataRelationPropertyDescriptorType = pdc["IDRelation"].GetType();
            }

            Type pdType = pd.GetType();
            return (pdType == s_DataTablePropertyDescriptorType) ||
                (pdType == s_DataRelationPropertyDescriptorType);
        }

        // Intercept GetValue calls for certain ADO properties
        internal override object GetValue(object item, PropertyDescriptor pd, bool useFollowParent)
        {
            object value = GetRawValue(item, pd, useFollowParent);

            if (IsDataSetCollectionProperty(pd))
            {
                // ADO returns a newly-created object (of type DataView or RelatedView)
                // for each call to the getter of a "DataSet collection property".
                // These objects are not referenced by any other ADO objects,
                // so it is up to the caller to keep them alive.  But WPF tries
                // hard not to add strong references to these objects.  There
                // are only three ways:
                //  1. Creating a BindingListCollectionView over the object
                //  2. Adding a ValueChanged listener for one of the object's properties
                //  3. Caching the object in the ValueTable
                //
                // Actually (3) no longer happens at all - it was causing a memory
                // leak of the entire DataSet. 
                //
                // If the app never uses (1) or (2), there's nothing to keep the
                // object alive.  There is a case where this actually
                // happens - the app uses bindings with indexers on the object, but
                // doesn't use any collection views or PropertyDescriptor-backed
                // properties.   After a while, the object is GC'd, and the app
                // silently stops working.
                //
                // To fix this, we add a reference from a suitable ADO object to
                // the new DataView/RelatedView.  This reference can't involve
                // any WPF objects - that would bring back the memory leak.
                // Instead, we use an ephemeral object created just for this purpose.
                // The ephemeral object subscribes to an event on the "suitable
                // ADO object", intentionally *not* using the WeakEvent pattern
                // so that the ADO object refers to the ephemeral object via the handler.
                // The ephemeral object also holds a strong reference to the new
                // DataView/RelatedView.  Together, these references tie the
                // lifetime of the DataView/RelatedView to the lifetime of the
                // ADO object, as desired.

                if (pd.GetType() == s_DataTablePropertyDescriptorType)
                {
                    // the "suitable ADO object" is the corresponding DataTable
                    DataTable dataTable = (value as DataView)?.Table;
                    if (dataTable != null)
                    {
                        new DataTableToDataViewLink(dataTable, value);
                    }
                }
                else if (pd.GetType() == s_DataRelationPropertyDescriptorType)
                {
                    // the "suitable ADO object" is the parent DataRowView
                    DataRowView dataRowView = item as DataRowView;
                    if (dataRowView != null)
                    {
                        new DataRowViewToRelatedViewLink(dataRowView, value);
                    }
                }
            }

            return value;
        }

        private object GetRawValue(object item, PropertyDescriptor pd, bool useFollowParent)
        {
            if (useFollowParent && pd.GetType() == s_DataRelationPropertyDescriptorType)
            {
                // the DataRelation property returns a child view that doesn't
                // work in master/detail scenarios, when the primary key linking
                // the two tables changes. 
                // ADO added a new method that returns a better child view, specifically
                // to fix this bug, but System.Data.DataRelationPropertyDescriptor.GetValue
                // still uses the old method:
                //
                // public override object GetValue(object component) {
                //     DataRowView dataRowView = (DataRowView) component;
                //     return dataRowView.CreateChildView(relation);
                // }
                //
                // so we intercept the GetValue call and use the new method.
                // (The value of the 'relation' member isn't publicly visible,
                // but its name is.  That's enough to call the new method.)
                DataRowView dataRowView = (DataRowView)item;
                return dataRowView.CreateChildView(pd.Name, followParent:true);
            }

            // otherwise, call GetValue the normal way
            return pd.GetValue(item);
        }

        // return true if DBNull is a valid value for the given item and column.
        // The column may be specified directly by name, or indirectly by indexer: Item[arg]
        internal override bool DetermineWhetherDBNullIsValid(object item, string columnName, object arg)
        {
            DataRowView drv;
            DataRow dr = null;
            if ((drv = item as DataRowView) == null &&
                (dr = item as DataRow) == null)
            {
                return false;
            }

            // this code was provided by the ADO team
            DataTable table = (drv != null) ? drv.DataView.Table : dr.Table;

            DataColumn column = null;
            if (arg != null)
            {
                if ((columnName = arg as String) != null)
                {
                    column = table.Columns[columnName];
                }
                else if (arg is int)
                {
                    int index = (int)arg;
                    if (0 <= index && index < table.Columns.Count)
                    {
                        column = table.Columns[index];
                    }
                }
            }
            else if (columnName != null)
            {
                column = table.Columns[columnName];
            }

            return (column != null) && column.AllowDBNull;
        }

        private class DataTableToDataViewLink
        {
            public DataTableToDataViewLink(DataTable dataTable, object target)
            {
                _target = target;
                dataTable.Initialized += OnInitialized;
            }

            void OnInitialized(object sender, EventArgs e)
            {
            }

            object _target;
        }

        private class DataRowViewToRelatedViewLink
        {
            public DataRowViewToRelatedViewLink(DataRowView dataRowView, object target)
            {
                _target = target;
                dataRowView.PropertyChanged += OnPropertyChanged;
            }

            void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
            }

            object _target;
        }

        static Type s_DataTablePropertyDescriptorType;
        static Type s_DataRelationPropertyDescriptorType;
    }
}

