// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Primary root namespace for TabletPC/Ink/Handwriting/Recognition in .NET

namespace MS.Internal.Ink.InkSerializedFormat
{
    internal class TransformDescriptor
    {
        private double[]   _transform = new double[6];
        private uint      _size = 0;
        private KnownTagCache.KnownTagIndex _tag = KnownTagCache.KnownTagIndex.Unknown;

        public KnownTagCache.KnownTagIndex Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                _tag = value;
            }
        }
        public uint Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }
        public double[] Transform
        {
            get
            {
                return _transform;
            }
        }

        public bool Compare(TransformDescriptor that)
        {
            if( that.Tag == Tag )
            {
                if( that.Size == _size )
                {
                    for( int i = 0; i < _size; i++ )
                    {
                        if( !DoubleUtil.AreClose(that.Transform[i], _transform[i] ))
                            return false;
                    }
                    return true;
                }
                else
                    return false;
            }
            return false;
        }
    }
}

