// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xaml;
using System.Diagnostics;
using MS.Internal.Xaml.Context;

namespace System.Windows.Baml2006
{
    internal class Baml2006ReaderFrame : XamlFrame
    {
        protected Dictionary<string, string> _namespaces;

        public Baml2006ReaderFrame()
        {
            DelayedConnectionId = -1;
        }

        public Baml2006ReaderFrame(Baml2006ReaderFrame source)
        {
            XamlType = source.XamlType;
            Member = source.Member;
            if (source._namespaces != null)
            {
                _namespaces = new Dictionary<string, string>(source._namespaces);
            }
        }

        public override XamlFrame Clone()
        {
            return new Baml2006ReaderFrame(this);
        }

        public XamlType XamlType { get; set; }
        public XamlMember Member { get; set; }
        public KeyRecord Key { get; set; }
        public int DelayedConnectionId { get; set; }
        public XamlMember ContentProperty { get; set; }
        public bool FreezeFreezables { get; set; }

        public void AddNamespace(string prefix, string xamlNs)
        {
            if (null == _namespaces)
            {
                _namespaces = new Dictionary<string, string>();
            }
            _namespaces.Add(prefix, xamlNs);
        }

        public void SetNamespaces(Dictionary<string, string> namespaces)
        {
            _namespaces = namespaces;
        }

        public bool TryGetNamespaceByPrefix(string prefix, out string xamlNs)
        {
            if (_namespaces != null && _namespaces.TryGetValue(prefix, out xamlNs))
            {
                return true;
            }
            xamlNs = null;
            return false;
        }

        public bool TryGetPrefixByNamespace(string xamlNs, out string prefix)
        {
            if (_namespaces != null)
            {
                foreach (KeyValuePair<string, string> pair in _namespaces)
                {
                    if (pair.Value == xamlNs)
                    {
                        prefix = pair.Key;

                        return true;
                    }
                }
            }

            prefix = null;
            return false;
        }


        public override void Reset()
        {
            XamlType = null;
            Member = null;
            if (_namespaces != null)
            {
                _namespaces.Clear();
            }

            Flags = Baml2006ReaderFrameFlags.None;
            IsDeferredContent = false;
            Key = null;
            DelayedConnectionId = -1;
            ContentProperty = null;
        }

        public Baml2006ReaderFrameFlags Flags { get; set; }

        public bool IsDeferredContent { get; set; }
    }

    internal enum Baml2006ReaderFrameFlags:byte
    {
        None,
        IsImplict,
        HasImplicitProperty
    }
}
