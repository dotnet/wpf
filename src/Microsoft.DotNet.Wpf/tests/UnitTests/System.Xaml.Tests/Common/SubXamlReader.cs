// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml.Tests.Common;

public class SubXamlReader : XamlReader
{
    public SubXamlReader(params XamlNodeType[] nodeTypes)
    {
        NodeTypes = nodeTypes;
    }

    public XamlNodeType[] NodeTypes { get; }
    public int CurrentIndex { get; set; }
    public int ReadCount { get; set; }

    public override bool Read()
    {
        ReadCount++;
        CurrentIndex++;
        return CurrentIndex < NodeTypes.Length;
    }

    public override XamlNodeType NodeType => NodeTypes[CurrentIndex];

    public Optional<bool> IsEofResult { get; set; }
    public override bool IsEof => IsEofResult.Or(() => CurrentIndex >= NodeTypes.Length);

    public NamespaceDeclaration? NamespaceResult { get; set; }
    public override NamespaceDeclaration Namespace => NamespaceResult!;

    public XamlType? TypeResult { get; set; }
    public override XamlType Type => TypeResult!;

    public object? ValueResult { get; set; }
    public override object Value => ValueResult!;

    public XamlMember? MemberResult { get; set; }
    public override XamlMember Member => MemberResult!;

    public XamlSchemaContext? SchemaContextResult { get; set; }
    public override XamlSchemaContext SchemaContext => SchemaContextResult!;

    public bool IsDisposedEntry => IsDisposed;
}

public class SubXamlReaderWithLineInfo : SubXamlReader, IXamlLineInfo
{
    public SubXamlReaderWithLineInfo(params XamlNodeType[] nodeTypes) : base(nodeTypes)
    {
    }

    public bool HasLineInfoResult { get; set; }
    public virtual bool HasLineInfo => HasLineInfoResult;

    public int LineNumberResult { get; set; }
    public int LineNumber => LineNumberResult;

    public int LinePositionResult { get; set; }
    public int LinePosition => LinePositionResult;
}
