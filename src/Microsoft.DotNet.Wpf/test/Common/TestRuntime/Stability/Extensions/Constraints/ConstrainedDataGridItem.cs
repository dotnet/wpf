// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.ComponentModel;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    /// <summary>
    /// A class with some basic properties for DataGrid item
    /// </summary>
    public class ConstrainedDataGridItem
    {     
        private Boolean _Boolean;
        private Int32 _Int32;
        private Int64 _Int64;
        private Byte _Byte;
        private Single _Single;
        private Double _Double;
        private Decimal _Decimal;
        private Byte[] _ByteArray;
        private Char _Char;
        private String _String;
        private Object _Object;
        private Enumerations _Enumerations;
        private Uri _Uri;

        public Boolean BooleanProp
        {
            get { return this._Boolean; }
            set
            {
                if ((this._Boolean != value))
                {
                    this._Boolean = value;
                    this.NotifyPropertyChanged("BooleanProp");
                }
            }
        }

        public Int32 Int32Prop
        {
            get { return this._Int32; }
            set
            {
                if ((this._Int32 != value))
                {
                    this._Int32 = value;
                    this.NotifyPropertyChanged("Int32Prop");
                }
            }
        }

        public Int64 Int64Prop
        {
            get { return this._Int64; }
            set
            {
                if ((this._Int64 != value))
                {
                    this._Int64 = value;
                    this.NotifyPropertyChanged("Int64Prop");
                }
            }
        }

        public Byte ByteProp
        {
            get { return this._Byte; }
            set
            {
                if ((this._Byte != value))
                {
                    this._Byte = value;
                    this.NotifyPropertyChanged("ByteProp");
                }
            }
        }

        public Single SingleProp
        {
            get { return this._Single; }
            set
            {
                if ((this._Single != value))
                {
                    this._Single = value;
                    this.NotifyPropertyChanged("SingleProp");
                }
            }
        }

        public Double DoubleProp
        {
            get { return this._Double; }
            set
            {
                if ((this._Double != value))
                {
                    this._Double = value;
                    this.NotifyPropertyChanged("DoubleProp");
                }
            }
        }

        public Decimal DecimalProp
        {
            get { return this._Decimal; }
            set
            {
                if ((this._Decimal != value))
                {
                    this._Decimal = value;
                    this.NotifyPropertyChanged("DecimalProp");
                }
            }
        }

        public Byte[] ByteArrayProp
        {
            get { return this._ByteArray; }
            set
            {
                if ((this._ByteArray != value))
                {
                    this._ByteArray = value;
                    this.NotifyPropertyChanged("ByteArrayProp");
                }
            }
        }

        public Char CharProp
        {
            get { return this._Char; }
            set
            {
                if ((this._Char != value))
                {
                    this._Char = value;
                    this.NotifyPropertyChanged("CharProp");
                }
            }
        }

        public String StringProp
        {
            get { return this._String; }
            set
            {
                if ((this._String != value))
                {
                    this._String = value;
                    this.NotifyPropertyChanged("StringProp");
                }
            }
        }

        public Object ObjectProp
        {
            get { return this._Object; }
            set
            {
                if ((this._Object != value))
                {
                    this._Object = value;
                    this.NotifyPropertyChanged("ObjectProp");
                }
            }
        }

        public Enumerations EnumerationsProp
        {
            get { return this._Enumerations; }
            set
            {
                if ((this._Enumerations != value))
                {
                    this._Enumerations = value;
                    this.NotifyPropertyChanged("EnumerationsProp");
                }
            }
        }

        public Uri UriProp
        {
            get { return this._Uri; }
            set
            {
                if ((this._Uri != value))
                {
                    this._Uri = value;
                    this.NotifyPropertyChanged("UriProp");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged(String propertyName)
        {
            if ((this.PropertyChanged != null))
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public enum Enumerations { Enum0, Enum1, Enum2, Enum3, Enum4 };
}
