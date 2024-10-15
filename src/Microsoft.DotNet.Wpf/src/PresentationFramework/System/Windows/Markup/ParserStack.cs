// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  ParserStack...once was in XamlReaderHelper.cs
*
\***************************************************************************/

using System.Collections.Generic;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// Parser Context stack
    /// The built-in Stack class doesn't let you do things like look at nodes beyond the top of the stack...
    /// so we'll use this instead. Note that this is not a complete stack implementation -- if you try to
    /// convert to array or some such, you'll be missing a couple elements. But this is plenty for our purposes.
    /// Main goal is to track current/parent context with a minimum of overhead.
    /// This class is internal so it can be shared with the BamlRecordReader
    /// </summary>
    internal sealed class ParserStack<T> : List<T> where T : class
    {
        /// <summary>
        ///     Creates a default ParserStack
        /// </summary>
        internal ParserStack() : base() { }

        public void Push(T item)
        {
            Add(item);
        }

        public T Pop()
        {
            T item = this[Count - 1];
            RemoveAt(Count - 1);

            return item;
        }

#if !PBTCOMPILER
        public T Peek()
        {
            // Die if peeking on empty stack
            return this[Count - 1];
        }
#endif

        public List<T> Clone()
        {
            return new List<T>(this);
        }

        /// <summary>
        /// Returns the Current Context on the stack
        /// </summary>
        internal T CurrentContext
        {
            get => Count > 0 ? this[Count - 1] : null;
        }

        /// <summary>
        /// Returns the Parent of the Current Context
        /// </summary>
        internal T ParentContext
        {
            get => Count > 1 ? this[Count - 2] : null;
        }

        /// <summary>
        /// Returns the GrandParent of the Current Context
        /// </summary>
        internal T GrandParentContext
        {
            get => Count > 2 ? this[Count - 3] : null;
        }

#if !PBTCOMPILER
        /// <summary>
        /// Returns the GreatGrandParent of the Current Context
        /// </summary>
        internal T GreatGrandParentContext
        {
            get => Count > 3 ? this[Count - 4] : null;
        }
#endif
    }
}
