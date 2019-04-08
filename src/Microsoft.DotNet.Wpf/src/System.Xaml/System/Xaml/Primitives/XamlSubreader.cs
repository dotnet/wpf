// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml
{
    internal class XamlSubreader : XamlReader, IXamlLineInfo
    {
        XamlReader _reader;
        IXamlLineInfo _lineInfoReader;
        bool _done;
        bool _firstRead;
        bool _rootIsStartMember;
        int _depth;

        public XamlSubreader(XamlReader reader)
        {
            _reader = reader;
            _lineInfoReader = reader as IXamlLineInfo;
            _done = false;
            _depth = 0;
            _firstRead = true;
            _rootIsStartMember = (reader.NodeType == XamlNodeType.StartMember);
        }

        public override bool Read()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("XamlReader");  // can't say "XamlSubreader" it's an internal class.
            }
            if (!_firstRead)
            {
                return LimitedRead();
            }
            _firstRead = false;
            return true;
        }

        private bool IsEmpty => _done || _firstRead;

        public override XamlNodeType NodeType => IsEmpty ? XamlNodeType.None : _reader.NodeType;

        public override bool IsEof => IsEmpty ? true : _reader.IsEof;

        public override NamespaceDeclaration Namespace => IsEmpty ? null : _reader.Namespace;

        public override XamlType Type => IsEmpty ? null : _reader.Type;

        public override object Value => IsEmpty ? null : _reader.Value;

        public override XamlMember Member => IsEmpty ? null : _reader.Member;

        public override XamlSchemaContext SchemaContext => _reader.SchemaContext;

        #region IXamlLineInfo Members

        public bool HasLineInfo => _lineInfoReader != null && _lineInfoReader.HasLineInfo;

        public int LineNumber =>_lineInfoReader == null ? 0 : _lineInfoReader.LineNumber;

        public int LinePosition => _lineInfoReader == null ? 0 : _lineInfoReader.LinePosition;

        #endregion

        // ----------  Private methods --------------

        private bool LimitedRead()
        {
            if (IsEof)
            {
                return false;
            }

            XamlNodeType nodeType = _reader.NodeType;

            if (_rootIsStartMember)
            {
                if (nodeType == XamlNodeType.StartMember)
                {
                    _depth += 1;
                }
                else if (nodeType == XamlNodeType.EndMember)
                {
                    _depth -= 1;
                }
            }
            else
            {
                if (nodeType == XamlNodeType.StartObject
                    || nodeType == XamlNodeType.GetObject)
                {
                    _depth += 1;
                }
                else if (nodeType == XamlNodeType.EndObject)
                {
                    _depth -= 1;
                }
            }

            if (_depth == 0)
            {
                _done = true;
            }
            _reader.Read();
            return !IsEof;
        }
    }
}
