// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: ThousandthOfEmRealDoubles class
//
//

using System;
using System.Diagnostics;
using System.Collections.Generic;

using System.Windows;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// This is a fixed-size implementation of IList&lt;double&gt;. It is aimed to reduce the double values storage 
    /// while providing enough precision for glyph run operations. Current usage pattern suggests that there is no
    /// need to support resizing functionality (i.e. Add(), Insert(), Remove(), RemoveAt()).
    ///  
    /// For each double being stored, it will try to scale the value to 16-bit integer expressed in 1/1000th of 
    /// the given Em size. The scale will only be done if the precision remains no less than 1/2000th of an inch.
    /// 
    /// There are two scenarios where the given double value can not be scaled to 16-bit integer:
    /// o The given Em size is so big such that 1/1000th of it is not precise enough. 
    /// o The given double value is so big such that the scaled value cannot be fit into a short. 
    /// 
    /// If either of these cases happens (expected to happen rarely), this array implementation will fall back to store all 
    /// values as double. 
    /// </summary>
    internal sealed class ThousandthOfEmRealDoubles : IList<double>
    {
        //----------------------------------
        // Constructor
        //----------------------------------
        internal ThousandthOfEmRealDoubles(
            double emSize,
            int    capacity
            )
        {
            Debug.Assert(capacity >= 0);
            _emSize = emSize;
            InitArrays(capacity);
        }
        
        internal ThousandthOfEmRealDoubles(
            double        emSize,
            IList<double> realValues
            )
        {
            Debug.Assert(realValues != null);
            _emSize = emSize;            
            InitArrays(realValues.Count);            

            // do the setting
            for (int i = 0; i < Count; i++)
            {
                this[i] = realValues[i];
            }
}

        //-------------------------------------
        // Internal properties
        //-------------------------------------
        public int Count
        {
            get
            {
                if (_shortList != null)
                {
                    return _shortList.Length;
                }
                else
                {
                    return _doubleList.Length;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }        

        public double this[int index]
        {
            get
            {
                // Let underlying array do boundary check
                if (_shortList != null)
                {                    
                    return ThousandthOfEmToReal(_shortList[index]);
                }
                else
                {
                    return _doubleList[index];
                }
            }

            set
            {
                // Let underlying array do boundary check
                if (_shortList != null)
                {
                    short sValue;
                    if (RealToThousandthOfEm(value, out sValue))
                    {
                        _shortList[index] = sValue;
                    }
                    else
                    {
                        // The input double can't be scaled. We will 
                        // fall back to use double[] now                        
                        _doubleList = new double[_shortList.Length];
                        for (int i = 0; i < _shortList.Length; i++)
                        {
                            _doubleList[i] = ThousandthOfEmToReal(_shortList[i]);
                        }

                        _doubleList[index] = value; // set the current value
                        _shortList = null;          // deprecate the short array from now on
                    }
                }
                else
                {
                    _doubleList[index] = value; // we are using double array 
                }
            }
        }

        //------------------------------------
        // internal methods
        //------------------------------------
        public int IndexOf(double item)
        {
            // linear search 
            for (int i = 0; i < Count; i++)
            {
                if (this[i] == item)
                {
                    return i;
                }
            }            
            
            return -1;
        }

        public void Clear()
        {
            // zero the stored values
            if (_shortList != null)
            {
                for (int i = 0; i < _shortList.Length; i++)
                {
                    _shortList[i] = 0;
                }
            }
            else
            {
                for (int i = 0; i < _doubleList.Length; i++)
                {
                    _doubleList[i] = 0;
                }
            }
        }

        public bool Contains(double item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(double[] array, int arrayIndex)
        {            
            // parameter validations
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(
                    SR.Get(SRID.Collection_CopyTo_ArrayCannotBeMultidimensional), 
                    "array");                
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            if (arrayIndex >= array.Length)
            {
                throw new ArgumentException(
                    SR.Get(
                        SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, 
                        "arrayIndex", 
                        "array"),
                    "arrayIndex");
            }

            if ((array.Length - Count - arrayIndex) < 0)
            {
                throw new ArgumentException(
                    SR.Get(
                        SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength,
                        "arrayIndex",
                        "array"));
            }           
            

            // do the copying here
            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }        

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<double>)this).GetEnumerator();
        }


        public void Add(double value)
        {
            // not supported, same as double[] 
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        public void Insert(int index, double item)
        {
            // not supported, same as double[] 
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        public bool Remove(double item)
        {
            // not supported, same as double[]             
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        public void RemoveAt(int index)
        {
            // not supported, same as double[]             
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        //---------------------------------------------
        // Private methods
        //---------------------------------------------       
        private void InitArrays(int capacity)
        {
            if (_emSize > CutOffEmSize)
            {
                // use double storage when emsize is too big
                _doubleList = new double[capacity];
            }
            else
            {
                // store value as scaled short.
                _shortList = new short[capacity];
            }            
        }

        private bool RealToThousandthOfEm(double value, out short thousandthOfEm)
        {
            double scaled = (value / _emSize) * ToThousandthOfEm;
            
            if (scaled > short.MaxValue || scaled < short.MinValue)
            {
                // value too big to fit into a short
                thousandthOfEm = 0;
                return false;
            }
            else
            {
                // round to nearest short
                thousandthOfEm = (short) Math.Round(scaled);
                return true;
            }
        }

        private double ThousandthOfEmToReal(short thousandthOfEm)
        {
            return ((double)thousandthOfEm) * ToReal * _emSize;
        }        

        //----------------------------------------
        // Private members
        //----------------------------------------
        private short[]  _shortList;  // scaled short values
        private double[] _doubleList; // fall-back double list, is null for most cases
        private double   _emSize;     // em size to scaled with

        // Default scaling is 1/1000 emsize.         
        private const double ToThousandthOfEm = 1000.0;
        private const double ToReal           = 1.0 / ToThousandthOfEm;

        // To achieve precsion of no less than 1/2000 of an inch, font Em size must be no greater than 48. 
        // i.e. 48px is 1/2 inch. 1000th of Em size at 48px is 1/2000 inch.
        private const double CutOffEmSize = 48;         
}    
}
