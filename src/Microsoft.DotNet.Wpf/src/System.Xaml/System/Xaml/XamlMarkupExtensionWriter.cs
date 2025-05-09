﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Text;

namespace System.Xaml
{
    internal class XamlMarkupExtensionWriter : XamlWriter
    {
        private StringBuilder sb;
        private Stack<Node> nodes;
        private WriterState currentState;
        private XamlXmlWriter xamlXmlWriter;
        private XamlXmlWriterSettings settings;
        private XamlMarkupExtensionWriterSettings meSettings;
        private bool failed;

        public XamlMarkupExtensionWriter(XamlXmlWriter xamlXmlWriter)
        {
            Initialize(xamlXmlWriter);
        }

        public XamlMarkupExtensionWriter(XamlXmlWriter xamlXmlWriter, XamlMarkupExtensionWriterSettings meSettings)
        {
            this.meSettings = meSettings;
            Initialize(xamlXmlWriter);
        }

        private void Initialize(XamlXmlWriter xamlXmlWriter)
        {
            this.xamlXmlWriter = xamlXmlWriter;
            settings = xamlXmlWriter.Settings; // This will clone, only want to do this once
            meSettings ??= new XamlMarkupExtensionWriterSettings();
            currentState = Start.State;
            sb = new StringBuilder();
            nodes = new Stack<Node>();
            failed = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override XamlSchemaContext SchemaContext
        {
            get
            {
                return xamlXmlWriter.SchemaContext;
            }
        }

        public void Reset()
        {
            currentState = Start.State;
            sb = new StringBuilder();
            nodes.Clear();
            failed = false;
        }

        // MarkupExtensionString is used to obtain the curly-formatted markup extension string.
        // It should be called after calling the final WriteEndObject().
        // If MarkupExtensionString is not called before writing the next markup extension string
        // in curly syntax, the previous markup extension string is lost.
        public string MarkupExtensionString
        {
            get
            {
                if (nodes.Count == 0)
                {
                    return sb.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        // This is set to true when the Markup Extension Writer fails to write
        // the given node stream in curly form.
        public bool Failed
        {
            get
            {
                return failed;
            }
        }

        private string LookupPrefix(XamlType type)
        {
            string prefix = xamlXmlWriter.LookupPrefix(type.GetXamlNamespaces(), out _);

            if (prefix is null)
            {
                if (!meSettings.ContinueWritingWhenPrefixIsNotFound)
                {
                    // the prefix is not found and curly syntax has no way of defining a prefix
                    failed = true;
                    return string.Empty; // what we return here is not important, since Failed has set to be true
                }
            }

            return prefix;
        }

        private string LookupPrefix(XamlMember property)
        {
            string prefix = xamlXmlWriter.LookupPrefix(property.GetXamlNamespaces(), out _);

            if (prefix is null)
            {
                if (!meSettings.ContinueWritingWhenPrefixIsNotFound)
                {
                    failed = true;
                    // the prefix is not found and curly syntax has no way of defining a prefix
                    return string.Empty; // what we return here is not important, since Failed has set to be true
                }
            }

            return prefix;
        }

        private void CheckMemberForUniqueness(Node objectNode, XamlMember property)
        {
            if (!settings.AssumeValidInput)
            {
                if (objectNode.Members is null)
                {
                    objectNode.Members = new XamlPropertySet();
                }
                else if (objectNode.Members.Contains(property))
                {
                    throw new InvalidOperationException(SR.Format(SR.XamlMarkupExtensionWriterDuplicateMember, property.Name));
                }

                objectNode.Members.Add(property);
            }
        }

        public override void WriteStartObject(XamlType type)
        {
            currentState.WriteStartObject(this, type);
        }

        public override void WriteGetObject()
        {
            currentState.WriteGetObject(this);
        }

        public override void WriteEndObject()
        {
            currentState.WriteEndObject(this);
        }

        public override void WriteStartMember(XamlMember property)
        {
            currentState.WriteStartMember(this, property);
        }

        public override void WriteEndMember()
        {
            currentState.WriteEndMember(this);
        }

        public override void WriteNamespace(NamespaceDeclaration namespaceDeclaration)
        {
            currentState.WriteNamespace(this, namespaceDeclaration);
        }

        public override void WriteValue(object value)
        {
            if (value is not string s)
            {
                throw new ArgumentException(SR.XamlMarkupExtensionWriterCannotWriteNonstringValue);
            }

            currentState.WriteValue(this, s);
        }

        private class Node
        {
            public XamlMember XamlProperty
            {
                get;
                set;
            }

            public XamlPropertySet Members
            {
                get;
                set;
            }

            public XamlNodeType NodeType
            {
                get;
                set;
            }

            public XamlType XamlType
            {
                get;
                set;
            }
        }

        private abstract class WriterState
        {
            // according to the BNF, CharactersToEscape ::= ['",={}\]
            private static char[] specialChars = new char[] { '\'', '"', ',', '=', '{', '}', '\\', ' ' };

            public virtual void WriteStartObject(XamlMarkupExtensionWriter writer, XamlType type)
            {
                writer.failed = true;
            }

            public virtual void WriteGetObject(XamlMarkupExtensionWriter writer)
            {
                writer.failed = true;
            }

            public virtual void WriteEndObject(XamlMarkupExtensionWriter writer)
            {
                writer.failed = true;
            }

            public virtual void WriteStartMember(XamlMarkupExtensionWriter writer, XamlMember property)
            {
                writer.failed = true;
            }

            public virtual void WriteEndMember(XamlMarkupExtensionWriter writer)
            {
                writer.failed = true;
            }

            public virtual void WriteValue(XamlMarkupExtensionWriter writer, string value)
            {
                writer.failed = true;
            }

            public virtual void WriteNamespace(XamlMarkupExtensionWriter writer, NamespaceDeclaration namespaceDeclaration)
            {
                writer.failed = true;
            }

            protected static bool ContainCharacterToEscape(string s)
            {
                return s.IndexOfAny(specialChars) >= 0;
            }

            protected static string FormatStringInCorrectSyntax(string s)
            {
                StringBuilder sb = new StringBuilder("\"");
                for (int i = 0; i < s.Length; i++)
                {
                    // BNF: DoubleQuotedValue ::= '"' ((Char - ["\]) | '\"' | '\\')+ '"'
                    // so the only characters we need to skip are the backslash and the double quote.

                    if (s[i] == '\\' || s[i] == '"')
                    {
                        sb.Append('\\');
                    }

                    sb.Append(s[i]);
                }

                sb.Append('\"');
                return sb.ToString();
            }

            protected void WritePrefix(XamlMarkupExtensionWriter writer, string prefix)
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    writer.sb.Append(prefix);
                    writer.sb.Append(':');
                }
            }

            public void WriteString(XamlMarkupExtensionWriter writer, string value)
            {
                if (ContainCharacterToEscape(value) || value.Length == 0)
                {
                    value = FormatStringInCorrectSyntax(value);
                }

                writer.sb.Append(value);
            }
        }

        // XamlMarkupExtensionWriter returns to this state after a markup extension has been completed,
        // i.e. when the number of closing curly bracket "}" matches the number of opening curly bracket "{".
        // At this state, XamlMarkupExtensionWriter is ready to start writing a markup extension
        private class Start : WriterState
        {
            private static WriterState state = new Start();

            private Start() { }

            public static WriterState State
            {
                get { return state; }
            }

            public override void WriteStartObject(XamlMarkupExtensionWriter writer, XamlType type)
            {
                writer.Reset();

                string prefix = writer.LookupPrefix(type);

                writer.sb.Append('{');
                WritePrefix(writer, prefix);
                writer.sb.Append(XamlXmlWriter.GetTypeName(type));

                writer.nodes.Push(new Node { NodeType = XamlNodeType.StartObject, XamlType = type });
                writer.currentState = InObjectBeforeMember.State;
            }
        }

        private abstract class InObject : WriterState
        {
            protected InObject() { }

            public abstract string Delimiter
            {
                get;
            }

            public override void WriteEndObject(XamlMarkupExtensionWriter writer)
            {
                if (writer.nodes.Count == 0)
                {
                    throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                }

                Node node = writer.nodes.Pop();

                if (node.NodeType != XamlNodeType.StartObject)
                {
                    throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                }

                writer.sb.Append('}');

                if (writer.nodes.Count == 0)
                {
                    writer.currentState = Start.State;
                }
                else
                {
                    Node member = writer.nodes.Peek();
                    if (member.NodeType != XamlNodeType.StartMember)
                    {
                        throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                    }

                    if (member.XamlProperty == XamlLanguage.PositionalParameters)
                    {
                        writer.currentState = InPositionalParametersAfterValue.State;
                    }
                    else
                    {
                        writer.currentState = InMemberAfterValueOrEndObject.State;
                    }
                }
            }

            protected void UpdateStack(XamlMarkupExtensionWriter writer, XamlMember property)
            {
                if (writer.nodes.Count == 0)
                {
                    throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                }

                Node objectNode = writer.nodes.Peek();

                if (objectNode.NodeType != XamlNodeType.StartObject)
                {
                    throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                }

                writer.CheckMemberForUniqueness(objectNode, property);

                writer.nodes.Push(new Node
                {
                    NodeType = XamlNodeType.StartMember,
                    XamlType = objectNode.XamlType,
                    XamlProperty = property
                });
            }

            protected void WriteNonPositionalParameterMember(XamlMarkupExtensionWriter writer, XamlMember property)
            {
                if (XamlXmlWriter.IsImplicit(property) ||
                    (property.IsDirective && (property.Type.IsCollection || property.Type.IsDictionary)))
                {
                    writer.failed = true;
                    return;
                }

                if (property.IsDirective)
                {
                    writer.sb.Append(Delimiter);
                    WritePrefix(writer, writer.LookupPrefix(property));
                    writer.sb.Append(property.Name);
                }
                else if (property.IsAttachable)
                {
                    writer.sb.Append(Delimiter);
                    WritePrefix(writer, writer.LookupPrefix(property));
                    string local = $"{property.DeclaringType.Name}.{property.Name}";
                    writer.sb.Append(local);
                }
                else
                {
                    writer.sb.Append(Delimiter);
                    writer.sb.Append(property.Name);
                }

                writer.sb.Append('=');

                writer.currentState = InMember.State;
            }
        }

        private class InObjectBeforeMember : InObject
        {
            private static WriterState state = new InObjectBeforeMember();

            private InObjectBeforeMember() { }

            public static WriterState State
            {
                get { return state; }
            }

            public override string Delimiter
            {
                get { return " "; }
            }

            public override void WriteStartMember(XamlMarkupExtensionWriter writer, XamlMember property)
            {
                UpdateStack(writer, property);
                if (property == XamlLanguage.PositionalParameters)
                {
                    writer.currentState = InPositionalParametersBeforeValue.State;
                }
                else
                {
                    WriteNonPositionalParameterMember(writer, property);
                }
            }
        }

        private class InObjectAfterMember : InObject
        {
            private static WriterState state = new InObjectAfterMember();

            private InObjectAfterMember() { }

            public static WriterState State
            {
                get { return state; }
            }

            public override string Delimiter
            {
                get { return ", "; }
            }

            public override void WriteStartMember(XamlMarkupExtensionWriter writer, XamlMember property)
            {
                UpdateStack(writer, property);
                WriteNonPositionalParameterMember(writer, property);
            }
        }

        private abstract class InPositionalParameters : WriterState
        {
            protected InPositionalParameters()
            {
            }

            public abstract string Delimiter
            {
                get;
            }

            public override void WriteValue(XamlMarkupExtensionWriter writer, string value)
            {
                writer.sb.Append(Delimiter);
                WriteString(writer, value);
                writer.currentState = InPositionalParametersAfterValue.State;
            }

            public override void WriteStartObject(XamlMarkupExtensionWriter writer, XamlType type)
            {
                writer.sb.Append(Delimiter);
                writer.currentState = InMember.State;
                writer.currentState.WriteStartObject(writer, type);
            }
        }

        private class InPositionalParametersBeforeValue : InPositionalParameters
        {
            private static WriterState state = new InPositionalParametersBeforeValue();

            private InPositionalParametersBeforeValue() { }

            public static WriterState State
            {
                get { return state; }
            }

            public override string Delimiter
            {
                get { return " "; }
            }
        }

        private class InPositionalParametersAfterValue : InPositionalParameters
        {
            private static WriterState state = new InPositionalParametersAfterValue();

            private InPositionalParametersAfterValue() { }

            public static WriterState State
            {
                get { return state; }
            }

            public override string Delimiter
            {
                get { return ", "; }
            }

            public override void WriteEndMember(XamlMarkupExtensionWriter writer)
            {
                Node node = writer.nodes.Pop();

                if (node.NodeType != XamlNodeType.StartMember || node.XamlProperty != XamlLanguage.PositionalParameters)
                {
                    throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                }

                writer.currentState = InObjectAfterMember.State;
            }
        }

        private class InMember : WriterState
        {
            private static WriterState state = new InMember();

            private InMember() { }

            public static WriterState State
            {
                get { return state; }
            }

            public override void WriteValue(XamlMarkupExtensionWriter writer, string value)
            {
                WriteString(writer, value);
                writer.currentState = InMemberAfterValueOrEndObject.State;
            }

            public override void WriteStartObject(XamlMarkupExtensionWriter writer, XamlType type)
            {
                if (!type.IsMarkupExtension)
                {
                    // can not write a non-ME object in this state in curly form
                    writer.failed = true;
                    return;
                }

                string prefix = writer.LookupPrefix(type);

                writer.sb.Append('{');
                WritePrefix(writer, prefix);
                writer.sb.Append(XamlXmlWriter.GetTypeName(type));

                writer.nodes.Push(new Node { NodeType = XamlNodeType.StartObject, XamlType = type });
                writer.currentState = InObjectBeforeMember.State;
            }
        }

        private class InMemberAfterValueOrEndObject : WriterState
        {
            private static WriterState state = new InMemberAfterValueOrEndObject();

            private InMemberAfterValueOrEndObject() { }

            public static WriterState State
            {
                get { return state; }
            }

            public override void WriteEndMember(XamlMarkupExtensionWriter writer)
            {
                if (writer.nodes.Count == 0)
                {
                    throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                }

                Node member = writer.nodes.Pop();

                if (member.NodeType != XamlNodeType.StartMember)
                {
                    throw new InvalidOperationException(SR.XamlMarkupExtensionWriterInputInvalid);
                }

                writer.currentState = InObjectAfterMember.State;
            }
        }
    }

    internal class XamlMarkupExtensionWriterSettings
    {
        public bool ContinueWritingWhenPrefixIsNotFound
        {
            get;
            set;
        }
    }
}
