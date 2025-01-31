// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Xaml;

namespace MS.Internal.Xaml.Context
{
    internal abstract class XamlCommonFrame: XamlFrame
    {
        internal Dictionary<string, string> _namespaces;

        public Dictionary<string, string> Namespaces
        {
            get
            {
                if (_namespaces is null)
                    _namespaces = new Dictionary<string, string>();
                return _namespaces;
            }
        }

        public XamlCommonFrame() : base()
        {
        }

        public XamlCommonFrame(XamlCommonFrame source) : base(source)
        {
            XamlType = source.XamlType;
            Member = source.Member;

            if (source._namespaces is not null)
            {
                SetNamespaces(source._namespaces);
            }
        }

        public override void Reset()
        {
            XamlType = null;
            Member = null;
            _namespaces?.Clear();
        }

        public XamlType XamlType { get; set; }
        public XamlMember Member { get; set; }

        public void AddNamespace(string prefix, string xamlNs)
        {
            Namespaces.Add(prefix, xamlNs);
        }

        public void SetNamespaces(Dictionary<string, string> namespaces)
        {
            _namespaces?.Clear();
            if (namespaces is not null)
            {
                foreach (KeyValuePair<string, string> ns in namespaces)
                {
                    Namespaces.Add(ns.Key, ns.Value);
                }
            }
        }

        public bool TryGetNamespaceByPrefix(string prefix, out string xamlNs)
        {
            if (_namespaces is not null && _namespaces.TryGetValue(prefix, out xamlNs))
            {
                return true;
            }

            xamlNs = null;
            return false;
        }

        public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes()
        {
            List<NamespaceDeclaration> _namespaceDeclarations = new List<NamespaceDeclaration>();
            foreach (KeyValuePair<string, string> kvp in _namespaces)
            {
                _namespaceDeclarations.Add(new NamespaceDeclaration(kvp.Value, kvp.Key));
            }

            return _namespaceDeclarations;
        }
    }
}
