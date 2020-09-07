// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  ParserStack...once was in XamlReaderHelper.cs
*
\***************************************************************************/

using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using MS.Utility;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using MS.Internal;


#if !PBTCOMPILER

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

#endif

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
    internal class ParserStack : ArrayList
    {
        /// <summary>
        ///     Creates a default ParserStack
        /// </summary>
        internal ParserStack() : base()
        {
        }

        /// <summary>
        ///     Creates a clone.
        /// </summary>
        private ParserStack(ICollection collection) : base(collection)
        {
        }

        #region StackOverrides

        public void Push(object o)
        {
            Add(o);
        }

        public object Pop()
        {
            object o = this[Count - 1];
            RemoveAt(Count - 1);
            return o;
        }

#if !PBTCOMPILER
        public object Peek()
        {
            // Die if peeking on empty stack
            return this[Count - 1];
        }
#endif

        public override object Clone()
        {
            return new ParserStack(this);
        }

        #endregion // StackOverrides

        #region Properties

        /// <summary>
        /// Returns the Current Context on the stack
        /// </summary>
        internal object CurrentContext
        {
            get { return Count > 0 ? this[Count - 1] : null; }
        }

        /// <summary>
        /// Returns the Parent of the Current Context
        /// </summary>
        internal object ParentContext
        {
            get { return Count > 1 ? this[Count - 2] : null; }
        }

        /// <summary>
        /// Returns the GrandParent of the Current Context
        /// </summary>
        internal object GrandParentContext
        {
            get { return Count > 2 ? this[Count - 3] : null; }
        }

#if !PBTCOMPILER
        /// <summary>
        /// Returns the GreatGrandParent of the Current Context
        /// </summary>
        internal object GreatGrandParentContext
        {
            get { return Count > 3 ? this[Count - 4] : null; }
        }
#endif

        #endregion // Properties

    }
}
