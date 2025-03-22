// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace System.Xaml
{
    internal class XamlSubreader : XamlReader, IXamlLineInfo
    {
        private XamlReader _reader;
        private IXamlLineInfo _lineInfoReader;
        private bool _done;
        private bool _firstRead;
        private bool _rootIsStartMember;
        private int _depth;

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
            ObjectDisposedException.ThrowIf(IsDisposed, typeof(XamlReader)); // Can't say XamlSubreader because its internal.
            if (!_firstRead)
            {
                return LimitedRead();
            }

            _firstRead = false;
            return true;
        }

        private bool IsEmpty { get { return _done || _firstRead; } }

        public override XamlNodeType NodeType
        {
            get { return IsEmpty ? XamlNodeType.None : _reader.NodeType; }
        }

        public override bool IsEof
        {
            get { return IsEmpty ? true : _reader.IsEof; }
        }

        public override NamespaceDeclaration Namespace
        {
            get { return IsEmpty ? null : _reader.Namespace; }
        }

        public override XamlType Type
        {
            get { return IsEmpty ? null : _reader.Type; }
        }

        public override object Value
        {
            get { return IsEmpty ? null : _reader.Value; }
        }

        public override XamlMember Member
        {
            get { return IsEmpty ? null : _reader.Member; }
        }

        public override XamlSchemaContext SchemaContext
        {
            get { return _reader.SchemaContext; }
        }

        #region IXamlLineInfo Members

        public bool HasLineInfo
        {
            get
            {
                if (_lineInfoReader is null)
                {
                    return false;
                }

                return _lineInfoReader.HasLineInfo;
            }
        }

        public int LineNumber
        {
            get
            {
                if (_lineInfoReader is null)
                {
                    return 0;
                }

                return _lineInfoReader.LineNumber;
            }
        }

        public int LinePosition
        {
            get
            {
                if (_lineInfoReader is null)
                {
                    return 0;
                }

                return _lineInfoReader.LinePosition;
            }
        }

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
