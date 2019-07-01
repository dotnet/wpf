// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    ////////////////////////////////////////////////////////////////////////////////////////////
    internal abstract class EnumerableW: IEnumerable {
        public EnumerableW(IEnumerable enumerable) {
            if(enumerable == null) {
                throw new ArgumentNullException("enumerable");
            }
            _innerEnumerable = enumerable;
        }
        
        IEnumerable _innerEnumerable;
        public IEnumerable InnerEnumerable { get { return _innerEnumerable; } }
        
        protected abstract IEnumerator GetEnumeratorImpl();
            
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumeratorImpl(); }        
    }
        
    internal abstract class EnumeratorW: IEnumerator {
        protected EnumeratorW(EnumerableW enumerable) {
            if(enumerable == null) {
                throw new ArgumentNullException("enumerable");
            }
            _innerEnumerator = enumerable.InnerEnumerable.GetEnumerator();
        }

        IEnumerator _innerEnumerator;
        public IEnumerator InnerEnumerator { get { return _innerEnumerator; } }
    
        public void Reset() { InnerEnumerator.Reset(); }
        
        public bool MoveNext() { return InnerEnumerator.MoveNext(); }
        
        protected abstract object CurrentImpl { get; }
        
        object IEnumerator.Current {
            get { return CurrentImpl; }
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////////////
    
    ////////////////////////////////////////////////////////////////////////////////////////////
    internal class ColumnResultListW: EnumerableW {
        public ColumnResultListW(IEnumerable colList):
            base(colList)
        {
        }
        
        protected override IEnumerator GetEnumeratorImpl() { return GetEnumerator(); }
        public ColumnResultEnumeratorW GetEnumerator() { return new ColumnResultEnumeratorW(this); }                
    }
    
    internal class ColumnResultEnumeratorW: EnumeratorW {
        internal ColumnResultEnumeratorW(ColumnResultListW enumerable):
            base(enumerable)
        {
        }

        protected override object CurrentImpl { get { return Current; } }
        public ColumnResultW Current { 
            get { return new ColumnResultW(InnerEnumerator.Current); }
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////////////
    
    ////////////////////////////////////////////////////////////////////////////////////////////
    internal class ParagraphResultListW: EnumerableW {
        public ParagraphResultListW(IEnumerable paragraphList):
            base(paragraphList)
        {
        }
        
        protected override IEnumerator GetEnumeratorImpl() { return GetEnumerator(); }
        public ParagraphResultEnumeratorW GetEnumerator() { return new ParagraphResultEnumeratorW(this); }
    }
    internal class ParagraphResultEnumeratorW: EnumeratorW {
        internal ParagraphResultEnumeratorW(ParagraphResultListW enumerable):
            base(enumerable)
        {
        }

        protected override object CurrentImpl { get { return Current; } }
        public ParagraphResultW Current { 
            get { return ParagraphResultW.FromObject(InnerEnumerator.Current); }
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////
    internal class LineResultListW: EnumerableW {
        public LineResultListW(IEnumerable lineList):
            base(lineList)
        {
        }
        
        protected override IEnumerator GetEnumeratorImpl() { return GetEnumerator(); }
        public LineResultEnumeratorW GetEnumerator() { return new LineResultEnumeratorW(this); }
    }
    internal class LineResultEnumeratorW: EnumeratorW {
        internal LineResultEnumeratorW(LineResultListW enumerable):
            base(enumerable)
        {
        }

        protected override object CurrentImpl { get { return Current; } }
        public LineResultW Current { 
            get { return new LineResultW(InnerEnumerator.Current); }
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////////////
}
