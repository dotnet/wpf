// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xaml;
using MS.Internal.Xaml.Context;

namespace System.Windows.Baml2006
{
    internal class Baml2006ReaderContext
    {
        public Baml2006ReaderContext(Baml2006SchemaContext schemaContext)
        {
            if (schemaContext == null)
            {
                throw new ArgumentNullException("schemaContext");
            }

            _schemaContext = schemaContext;
        }

        public Baml2006SchemaContext SchemaContext
        {
            get { return _schemaContext; }
        }

        public void PushScope()
        {
            _stack.PushScope();
            CurrentFrame.FreezeFreezables = PreviousFrame.FreezeFreezables;
        }

        public void PopScope()
        {
            _stack.PopScope();
        }

        public Baml2006ReaderFrame CurrentFrame
        {
            get { return _stack.CurrentFrame; }
        }

        public Baml2006ReaderFrame PreviousFrame
        {
            get { return _stack.PreviousFrame; }
        }

        public List<KeyRecord> KeyList { get; set; }

        public int CurrentKey { get; set; }

        public KeyRecord LastKey
        {
            get
            {
                if (KeyList != null && KeyList.Count > 0)
                {
                    return KeyList[KeyList.Count - 1];
                }
                return null;
            }
        }

        public bool InsideKeyRecord { get; set; }

        public bool InsideStaticResource { get; set; }

        public int TemplateStartDepth { get; set; }

        public int LineNumber { get; set; }
        public int LineOffset { get; set; }

        private Baml2006SchemaContext _schemaContext;
        private XamlContextStack<Baml2006ReaderFrame> _stack = new XamlContextStack<Baml2006ReaderFrame>(() => new Baml2006ReaderFrame());
    }
}