// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MS.Internal.Controls;
using MS.Internal.Data;
using MS.Utility;

namespace MS.Internal.Controls
{
    internal abstract class ModelTreeEnumerator : IEnumerator
    {
        internal ModelTreeEnumerator(object content)
        {
            _content = content;
        }

        #region IEnumerator

        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        bool IEnumerator.MoveNext()
        {
            return this.MoveNext();
        }

        void IEnumerator.Reset()
        {
            this.Reset();
        }

        #endregion

        #region Protected

        protected object Content
        {
            get
            {
                return _content;
            }
        }

        protected int Index
        {
            get
            {
                return _index;
            }

            set
            {
                _index = value;
            }
        }

        protected virtual object Current
        {
            get
            {
                // Don't VerifyUnchanged(); According to MSDN:
                //     If the collection is modified between MoveNext and Current,
                //     Current will return the element that it is set to, even if
                //     the enumerator is already invalidated.

                if (_index == 0)
                {
                    return _content;
                }

#pragma warning disable 1634 // about to use PreSharp message numbers - unknown to C#
                // Fall through -- can't enumerate (before beginning or after end)
#pragma warning suppress 6503   
                throw new InvalidOperationException(SR.Get(SRID.EnumeratorInvalidOperation));
                // above exception is part of the IEnumerator.Current contract when moving beyond begin/end
#pragma warning restore 1634
            }
        }

        protected virtual bool MoveNext()
        {
            if (_index < 1)
            {
                // Singular content, can move next to 0 and that's it.
                _index++;

                if (_index == 0)
                {
                    // don't call VerifyUnchanged if we're returning false anyway.
                    // This permits users to change the Content after enumerating
                    // the content (e.g. in the invalidation callback of an inherited
                    // property).  See bug 955389.

                    VerifyUnchanged();
                    return true;
                }
            }

            return false;
        }

        protected virtual void Reset()
        {
            VerifyUnchanged();
            _index = -1;
        }

        protected abstract bool IsUnchanged
        {
            get;
        }

        protected void VerifyUnchanged()
        {
            // If the content has changed, then throw an exception
            if (!IsUnchanged)
            {
                throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));
            }
        }

        #endregion

        #region Data

        private int _index = -1;
        private object _content;

        #endregion
    }

    internal class ContentModelTreeEnumerator : ModelTreeEnumerator
    {
        internal ContentModelTreeEnumerator(ContentControl contentControl, object content) : base(content)
        {
            Debug.Assert(contentControl != null, "contentControl should be non-null.");

            _owner = contentControl;
        }

        protected override bool IsUnchanged
        {
            get
            {
                return Object.ReferenceEquals(Content, _owner.Content);
            }
        }

        private ContentControl _owner;
    }

    internal class HeaderedContentModelTreeEnumerator : ModelTreeEnumerator
    {
        internal HeaderedContentModelTreeEnumerator(HeaderedContentControl headeredContentControl, object content, object header) : base(header)
        {
            Debug.Assert(headeredContentControl != null, "headeredContentControl should be non-null.");
            Debug.Assert(header != null, "Header should be non-null. If Header was null, the base ContentControl enumerator should have been used.");

            _owner = headeredContentControl;
            _content = content;
        }

        protected override object Current
        {
            get
            {
                if ((Index == 1) && (_content != null))
                {
                    return _content;
                }

                return base.Current;
            }
        }

        protected override bool MoveNext()
        {
            if (_content != null)
            {
                if (Index == 0)
                {
                    // Moving from the header to content
                    Index++;
                    VerifyUnchanged();
                    return true;
                }
                else if (Index == 1)
                {
                    // Going from content to the end
                    Index++;
                    return false;
                }
            }

            return base.MoveNext();
        }

        protected override bool IsUnchanged
        {
            get
            {
                object header = Content;    // Header was passed to the base so that it would appear in index 0
                return Object.ReferenceEquals(header, _owner.Header) &&
                       Object.ReferenceEquals(_content, _owner.Content);
            }
        }

        private HeaderedContentControl _owner;
        private object _content;
    }

    internal class HeaderedItemsModelTreeEnumerator : ModelTreeEnumerator
    {
        internal HeaderedItemsModelTreeEnumerator(HeaderedItemsControl headeredItemsControl, IEnumerator items, object header) : base(header)
        {
            Debug.Assert(headeredItemsControl != null, "headeredItemsControl should be non-null.");
            Debug.Assert(items != null, "items should be non-null.");
            Debug.Assert(header != null, "header should be non-null. If Header was null, the base ItemsControl enumerator should have been used.");

            _owner = headeredItemsControl;
            _items = items;
        }

        protected override object Current
        {
            get
            {
                if (Index > 0)
                {
                    return _items.Current;
                }

                return base.Current;
            }
        }

        protected override bool MoveNext()
        {
            if (Index >= 0)
            {
                Index++;
                return _items.MoveNext();
            }

            return base.MoveNext();
        }

        protected override void Reset()
        {
            base.Reset();
            _items.Reset();
        }

        protected override bool IsUnchanged
        {
            get
            {
                object header = Content;    // Header was passed to the base so that it would appear in index 0
                return Object.ReferenceEquals(header, _owner.Header);
            }
        }

        private HeaderedItemsControl _owner;
        private IEnumerator _items;
    }
}
