// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml
{
    // This is the base class for the simplest implementation of a
    // Node based XamlReader.
    // It serves up the values of the current node.
    // Advancing to the next node with Read() is left to be defined
    // in the deriving class.
    //
    abstract internal class ReaderBaseDelegate: XamlReader, IXamlLineInfo
    {
        protected XamlSchemaContext _schemaContext;
        protected XamlNode _currentNode;
        protected LineInfo _currentLineInfo;

        protected ReaderBaseDelegate(XamlSchemaContext schemaContext)
        {
            if (schemaContext == null)
            {
                throw new ArgumentNullException(nameof(schemaContext));
            }
            _schemaContext = schemaContext;            
        }

        public override XamlNodeType NodeType => _currentNode.NodeType;

        public override bool IsEof => _currentNode.IsEof;

        public override NamespaceDeclaration  Namespace => _currentNode.NamespaceDeclaration;

        public override XamlType Type => _currentNode.XamlType;

        public override object Value => _currentNode.Value;

        public override XamlMember Member => _currentNode.Member;

        public override XamlSchemaContext SchemaContext => _schemaContext;

        public bool HasLineInfo { get; set; }

        public int LineNumber =>_currentLineInfo != null ? _currentLineInfo.LineNumber : 0;

        public int LinePosition => _currentLineInfo != null ? _currentLineInfo.LinePosition : 0;
    }
}
