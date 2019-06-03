// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

// Primary root namespace for TabletPC/Ink/Handwriting/Recognition in .NET

namespace MS.Internal.Ink.InkSerializedFormat
{
    internal class StrokeDescriptor
    {
        private System.Collections.Generic.List<KnownTagCache.KnownTagIndex> _strokeDescriptor = new System.Collections.Generic.List<KnownTagCache.KnownTagIndex>();
        private uint _Size = 0;
        public uint Size
        {
            get
            {
                return _Size;
            }
            set
            {
                _Size = value;
            }
        }
        public System.Collections.Generic.List<KnownTagCache.KnownTagIndex> Template
        {
            get
            {
                return _strokeDescriptor;
            }
        }
        public StrokeDescriptor()
        {
        }

        public bool IsEqual(StrokeDescriptor strd)
        {
            // If the no of templates in them are different return false
            if( _strokeDescriptor.Count != strd.Template.Count )
                return false;

            // Compare each tag in the template. If any one of them is different, return false;
            for( int i = 0; i < _strokeDescriptor.Count; i++ )
                if( _strokeDescriptor[i] != strd.Template[i] )
                    return false;
            return true;
        }
    }
}
