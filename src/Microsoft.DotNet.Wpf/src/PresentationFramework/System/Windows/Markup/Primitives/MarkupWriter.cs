// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Contents:  XAML writer
//

using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Markup;
using MS.Internal;

namespace System.Windows.Markup.Primitives
{
    /// <summary>
    /// Routines to write a tree of objects to XAML format
    /// </summary>
    public sealed class MarkupWriter : IDisposable
    {
        /// <summary>
        /// Create an instance of a MarkupObject for the given object.
        /// </summary>
        /// <param name="instance">
        /// The instance of an object that will be treated as the root of a serialization tree
        /// </param>
        /// <returns>
        /// A MarkupObject that allows iterating a tree of objects as a serializer
        /// </returns>
        public static MarkupObject GetMarkupObjectFor(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            XamlDesignerSerializationManager manager = new XamlDesignerSerializationManager(null);
            manager.XamlWriterMode = XamlWriterMode.Expression;
            return new ElementMarkupObject(instance, manager);
        }

        /// <summary>
        /// Create an instance of a MarkupObject for the given object.
        /// </summary>
        /// <param name="instance">
        /// The instance of an object that will be treated as the root of a serialization tree
        /// </param>
        /// <param name="manager">
        /// The serialization manager to use.
        /// </param>
        /// <returns>
        /// A MarkupObject that allows iterating a tree of objects as a serializer
        /// </returns>
        public static MarkupObject GetMarkupObjectFor(object instance, XamlDesignerSerializationManager manager)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            if (manager == null)
                throw new ArgumentNullException("manager");
            return new ElementMarkupObject(instance, manager);
        }

        /// <summary>
        /// Serialize the given instance to a XML writer.
        /// </summary>
        /// <param name="writer">
        /// The writer to use to write the serialized image of the instance
        /// </param>
        /// <param name="instance">
        /// The object instance to serialize
        /// </param>
        internal static void SaveAsXml(XmlWriter writer, object instance)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            SaveAsXml(writer, GetMarkupObjectFor(instance));
        }

        /// <summary>
        /// Serialize the given instance to a XML writer.
        /// </summary>
        /// <param name="writer">
        /// The writer to use to write the serialized image of the instance
        /// </param>
        /// <param name="manager">
        /// The serialization manager to use.
        /// </param>
        /// <param name="instance">
        /// The object instance to serialize
        /// </param>
        internal static void SaveAsXml(XmlWriter writer, object instance, XamlDesignerSerializationManager manager)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (manager == null)
                throw new ArgumentNullException("manager");

            manager.ClearXmlWriter();

            SaveAsXml(writer, GetMarkupObjectFor(instance, manager));
        }

        /// <summary>
        /// Serialize the given item to an XML writer.
        /// </summary>
        /// <param name="writer">
        /// The writer to use to writer the serialized image of the item
        /// </param>
        /// <param name="item">
        /// The item to serialize. For example, the return value of GetMarkupObjectFor
        /// </param>
        internal static void SaveAsXml(XmlWriter writer, MarkupObject item)
        {
            // Consider turning Debug.Assert's in the WriteItem into exceptions
            // if this method is public.

            if (writer == null)
                throw new ArgumentNullException("writer");
            if (item == null)
                throw new ArgumentNullException("item");

            try
            {
                using (MarkupWriter markupWriter = new MarkupWriter(writer))
                {
                    markupWriter.WriteItem(item);
                }
            }
            finally
            {
                writer.Flush();
            }
        }

        /// <summary>
        /// Throws an exception if the given type cannot be serialized because it is
        /// 1) not public
        /// 2) a nested-public
        /// 3) a generic type
        /// </summary>
        /// <param name="type">
        /// The type to be checked
        /// </param>
        internal static void VerifyTypeIsSerializable(Type type)
        {
            // Check the type to make sure that it is not a nested type, that it is public, and that it is not generic
            if (type.IsNestedPublic)
            {
                throw new InvalidOperationException( SR.Get( SRID.MarkupWriter_CannotSerializeNestedPublictype, type.ToString() ));
            }
            if (!type.IsPublic )
            {
                throw new InvalidOperationException( SR.Get( SRID.MarkupWriter_CannotSerializeNonPublictype, type.ToString() ));
            }
            if (type.IsGenericType)
            {
                throw new InvalidOperationException( SR.Get( SRID.MarkupWriter_CannotSerializeGenerictype, type.ToString() ));
            }
        }

        #region Internal Implementation
        internal MarkupWriter(XmlWriter writer)
        {
            _writer = writer;
            _xmlTextWriter = writer as XmlTextWriter;
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private bool RecordNamespaces(Scope scope, MarkupObject item, IValueSerializerContext context, bool
            lastWasString)
        {
            // Ensure that there's an xmlns declaration generated for strings that are emitted as content.
            // For example, <Button>Hello</Button> should not bring in the System namespace from mscorlib via xmlns but,
            // <Button><s:String>Hello</s:String><Button> should.

            bool result = true;
            if (lastWasString || item.ObjectType != typeof(string) || HasNonValueProperties(item))
            {
                scope.MakeAddressable(item.ObjectType);
                result = false;
            }

            item.AssignRootContext(context);
            lastWasString = false;
            foreach (MarkupProperty property in item.Properties)
            {
                if (property.IsComposite)
                {
                    bool isCollection = IsCollectionType(property.PropertyType);
                    foreach (MarkupObject subItem in property.Items)
                        lastWasString = RecordNamespaces(scope, subItem, context, lastWasString || isCollection);
                }
                else
                    scope.MakeAddressable(property.TypeReferences);
                if (property.DependencyProperty != null)
                    scope.MakeAddressable(property.DependencyProperty.OwnerType);
                if (property.IsKey)
                    scope.MakeAddressable(NamespaceCache.XamlNamespace);
            }

            return result;
        }

        const string clrUriPrefix = "clr-namespace:";

        /// <summary>
        /// Partially ordered lists. Elements are stored in order
        /// that obeys the SetOrder calls. Elements need only be
        /// partially ordered instead of fully ordered. A full ordering
        /// is assigned that obeys the partial ordering declared.
        ///
        /// Limitations:
        ///
        /// Cyclic references
        /// -----------------
        /// This does not detect cyclic SetOrder values,
        /// for example, using string keys,
        ///
        ///   SetOrder("a", "b");
        ///   SetOrder("b", "c");
        ///   SetOrder("c", "a");
        ///
        /// No full ordering can obey this partial ordering. This is
        /// not detected and the first partial ordering (the oldest)
        /// is ignored.
        ///
        /// Null values
        /// -----------
        /// This list cannot contain null (it is used a sentinel value)
        /// but this can be fixed if needed by creating a not-present value
        /// using new Object() but since I didn't need it I didn't implement
        /// it.
        /// </summary>
        /// <typeparam name="TKey">The type of the key to use for the partial ordering</typeparam>
        /// <typeparam name="TValue">The type of the elements in the list</typeparam>
        private class PartiallyOrderedList<TKey, TValue> : IEnumerable<TValue>
            where TValue : class
        {
            /// <summary>
            /// Private class to hold the key and the value.
            /// </summary>
            private class Entry
            {
                public readonly TKey Key;
                public readonly TValue Value;
                public List<int> Predecessors;
                public int Link;    // -1 - unseen;  <-1 - in DFS;  >=0 - next index in top. order
                public const int UNSEEN = -1;
                public const int INDFS = -2;

                public Entry(TKey key, TValue value)
                {
                    Debug.Assert( (object)key != null);
                    Key = key;
                    Value = value;
                    Predecessors = null;
                    Link = 0;
                }

                public override bool Equals(object obj)
                {
                    Entry other = obj as Entry;
                    return other != null && other.Key.Equals(Key);
                }

                public override int GetHashCode()
                {
                    return Key.GetHashCode();
                }
            }

            /// <summary>
            /// Add a value to the list. The order in the list
            /// is controled by the key and calls to SetOrder.
            /// </summary>
            /// <param name="key">The ordering key</param>
            /// <param name="value">The value</param>
            public void Add(TKey key, TValue value)
            {
                Entry entry = new Entry(key, value);
                int existingIndex = _entries.IndexOf(entry);
                // If an entry already exists use it. This happens
                // when two values of the same key are added or when
                // SetOrder refering to a key that hasn't had a value
                // added for it yet. A null value is used as a place
                // holder.
                if (existingIndex >= 0)
                {
                    entry.Predecessors = _entries[existingIndex].Predecessors;
                    _entries[existingIndex] = entry;
                }
                else
                {
                    _entries.Add(entry);
                }
            }

            private int GetEntryIndex(TKey key)
            {
                Entry entry = new Entry(key, null);
                int result = _entries.IndexOf(entry);
                if (result < 0)
                {
                    result = _entries.Count;
                    _entries.Add(entry);
                }
                return result;
            }

            public void SetOrder(TKey predecessor, TKey key)
            {
                // Find where these keys are in the list
                // If they don't exist, a null value place holder is
                // added to the end of the list.
                int predIndex = GetEntryIndex(predecessor);
                Entry predEntry = _entries[predIndex];
                int keyIndex = GetEntryIndex(key);
                Entry keyEntry = _entries[keyIndex];

                // add the constraint
                if (keyEntry.Predecessors == null)
                {
                    keyEntry.Predecessors = new List<int>();
                }
                keyEntry.Predecessors.Add(predIndex);

                // mark the list to force a sort before the next
                // enumeration
                _firstIndex = Entry.UNSEEN;
            }

            // compute a linear order consistent with the constraints.
            // This is the classic Topological Sort problem, which is
            // solved in linear time by doing a depth-first search of the
            // "reverse" directed graph (edges go from a node to its
            // predecessors), and enumerating the nodes in postorder.
            // If there are no cycles, each node is enumerated after any
            // nodes that directly or indirectly precede it.
            private void TopologicalSort()
            {
                // initialize
                _firstIndex = Entry.UNSEEN;
                _lastIndex = Entry.UNSEEN;
                for (int i=0; i<_entries.Count; ++i)
                {
                    _entries[i].Link = Entry.UNSEEN;
                }

                // start a DFS at each entry
                for (int i=0; i<_entries.Count; ++i)
                {
                    DepthFirstSearch(i);
                }
            }

            // depth-first-search of predecessors of entry at given index
            private void DepthFirstSearch(int index)
            {
                // do a search, unless we've already seen this entry
                if (_entries[index].Link == Entry.UNSEEN)
                {
                    // mark entry as 'in progress'
                    _entries[index].Link = Entry.INDFS;

                    // search the predecessors
                    if (_entries[index].Predecessors != null)
                    {
                        foreach (int predIndex in _entries[index].Predecessors)
                        {
                            DepthFirstSearch(predIndex);
                        }
                    }

                    // Add the current entry to the postorder list.  We do this
                    // by linking the previous (in postorder) entry to this one
                    if (_lastIndex == -1)
                    {
                        _firstIndex = index;    // special case for head of list
                    }
                    else
                    {
                        _entries[_lastIndex].Link = index;
                    }

                    _lastIndex = index;
                }
                /* Note: if it is desired to detect cycles, this is the
                   place to do it.
                else if (_entries[index].Link == Entry.INDFS)
                {
                    // DFS has returned to an entry that is currently being
                    // searched.  This happens if and only if there is a cycle.
                    // Report the cycle.
                }
                */
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                // if new constraints have arrived, sort the list
                if (_firstIndex < 0)
                {
                    TopologicalSort();
                }

                // Enumerate the values according to the topological order.
                // We skip null values that
                // are just place holders for keys for which the order
                // was set but the value wasn't provided.
                int index = _firstIndex;
                while (index >= 0)
                {
                    Entry entry = _entries[index];
                    if (entry.Value != null)
                        yield return entry.Value;

                    index = entry.Link;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                // Non-generic version of IEnumerator
                foreach (TValue value in this)
                    yield return value;
            }

            private List<Entry> _entries = new List<Entry>();
            private int _firstIndex = Entry.UNSEEN; // head of linear order
            private int _lastIndex;                 // index of most recently assigned entry
        }

        private const int EXTENSIONLENGTH = 9; // the number of characters in the string "Extension"

        internal void WriteItem(MarkupObject item)
        {
            Debug.Assert(item != null && _writer != null);

            // We must check the type here, even though we check again in WriteScope(item, scope), because we
            // will wrap the type in an ExtensionSimpliefMarkupObject, which is the type that will be checked
            // from WriteScope(item, scope).
            VerifyTypeIsSerializable(item.ObjectType);

            Scope scope = new Scope(null);
            scope.RecordMapping("", NamespaceCache.GetNamespaceUriFor(item.ObjectType));

            // Do a pass to ensure all namespaces are visible at the top of the file.
            RecordNamespaces(scope, item, new MarkupWriterContext(scope), false);

            // Write out the simplified form of markup extensions
            item = new ExtensionSimplifierMarkupObject(item, null);

            WriteItem(item, scope);

            _writer = null;
        }

        private void WriteItem(MarkupObject item, Scope scope)
        {
            VerifyTypeIsSerializable(item.ObjectType);

            // Start a new scope for this item
            MarkupWriterContext context = new MarkupWriterContext(scope);
            item.AssignRootContext(context);

            // Write the start of the element
            string uri = scope.MakeAddressable(item.ObjectType);
            string prefix = scope.GetPrefixOf(uri);
            string name = item.ObjectType.Name;
            if (typeof(MarkupExtension).IsAssignableFrom(item.ObjectType) &&
                name.EndsWith("Extension", StringComparison.Ordinal))
            {
                // Prefer MarkupExtensions without the "Extension" postfix.
                name = name.Substring(0, name.Length - EXTENSIONLENGTH);
            }
            _writer.WriteStartElement(prefix, name, uri);

            // Write attributes
            ContentPropertyAttribute cpa = item.Attributes[typeof(ContentPropertyAttribute)] as ContentPropertyAttribute;
            XmlLangPropertyAttribute xlpa = item.Attributes[typeof(XmlLangPropertyAttribute)] as XmlLangPropertyAttribute;
            UidPropertyAttribute upa = item.Attributes[typeof(UidPropertyAttribute)] as UidPropertyAttribute;
            MarkupProperty contentProperty = null;

            bool first = true;
            int argumentCount = 0;
            List<int> argumentCompositeIndexes = null;
            bool noOtherPropertiesAllowed = false;
            List<MarkupProperty> composites = null;
            Dictionary<string, string> writtenAttributes = new Dictionary<string, string>();
            PartiallyOrderedList<string, MarkupProperty> deferredProperties = null;
            Formatting previousFormatting = (_xmlTextWriter != null) ? _xmlTextWriter.Formatting : Formatting.None;

            foreach (MarkupProperty property in item.GetProperties(false /*mapToConstructorArgs*/))
            {
                if (property.IsConstructorArgument)
                {
                    // When the reader supports <x:Argument1>...</x:Argument1> format, do the following:
                    //   _writer.WriteStartElement(string.Format(CultureInfo.InvariantCulture, "Argument{0}", argumentCompositeIndexes[argumentCompositeIndex++]), NamespaceCache.XamlNamespace);
                    // The writer generates an exception for now:
                    throw new InvalidOperationException(SR.Get(SRID.UnserializableKeyValue));
                }
                Debug.Assert(!noOtherPropertiesAllowed || property.IsKey,
                    "Problem with MarkupObject implemenation: Items returning a ValueAsString can have no other properties");

                if (IsContentProperty(property, cpa, ref contentProperty))
                {
                    first = false;
                    continue;
                }
                if (IsDeferredProperty(property, writtenAttributes, ref deferredProperties))
                {
                    continue;
                }

                if (!property.IsComposite)
                {
                    if (property.IsAttached || property.PropertyDescriptor == null)
                    {
                        if (property.IsValueAsString)
                        {
                            // "Property" represents the content passed to a type converter
                            Debug.Assert(first, "Problem with MarkupObject implemenation: Items returning a ValueAsString can have no other properties");
                            contentProperty = property;
                            noOtherPropertiesAllowed = true;
                            first = false;
                            continue;
                        }
                        if (property.IsKey)
                        {
                            // "Property" is the key to the this dictionary entry
                            scope.MakeAddressable(NamespaceCache.XamlNamespace);
                            _writer.WriteAttributeString(scope.GetPrefixOf(NamespaceCache.XamlNamespace), "Key", NamespaceCache.XamlNamespace, property.StringValue);
                            continue;
                        }

                        // Property is attached
                        DependencyProperty dependencyProperty = property.DependencyProperty;
                        Debug.Assert(dependencyProperty != null, "Problem with MarkupObject implementation: If PropertyDescriptor is null one of the following needs to be true; IsKey, IsValueAsString, IsConstructorArgument, or DependencyProperty is not null");

                        string typeUri = scope.MakeAddressable(dependencyProperty.OwnerType);
                        scope.MakeAddressable(property.TypeReferences);

                        if (property.Attributes[typeof(DesignerSerializationOptionsAttribute)] != null)
                        {
                            DesignerSerializationOptionsAttribute option = property.Attributes[typeof(DesignerSerializationOptionsAttribute)] as DesignerSerializationOptionsAttribute;
                            if (option.DesignerSerializationOptions == DesignerSerializationOptions.SerializeAsAttribute)
                            {
                                if (dependencyProperty == UIElement.UidProperty)
                                {
                                    // Force UIElement.Uid to be serialized as x:Uid
                                    string xamlUri = scope.MakeAddressable(typeof(TypeExtension));
                                    _writer.WriteAttributeString(scope.GetPrefixOf(xamlUri), dependencyProperty.Name, xamlUri, property.StringValue);
                                }

                                continue;
                            }
                        }

                        property.VerifyOnlySerializableTypes();

                        string propertyPrefix = scope.GetPrefixOf(typeUri);
                        string localName = dependencyProperty.OwnerType.Name + "." + dependencyProperty.Name;
                        if (string.IsNullOrEmpty(propertyPrefix))
                        {
                            _writer.WriteAttributeString(localName, property.StringValue);
                        }
                        else
                        {
                            _writer.WriteAttributeString(propertyPrefix, localName, typeUri, property.StringValue);
                        }
                    }
                    else
                    {
                        property.VerifyOnlySerializableTypes();

                        if (xlpa != null && xlpa.Name == property.PropertyDescriptor.Name)
                        {
                            // This is an xml:lang attribute
                            _writer.WriteAttributeString("xml", "lang", NamespaceCache.XmlNamespace, property.StringValue);
                        }
                        else if (upa != null && upa.Name == property.PropertyDescriptor.Name)
                        {
                            string xamlUri = scope.MakeAddressable(NamespaceCache.XamlNamespace);
                            _writer.WriteAttributeString(scope.GetPrefixOf(xamlUri), property.PropertyDescriptor.Name, xamlUri, property.StringValue);
                        }
                        else
                        {
                            _writer.WriteAttributeString(property.PropertyDescriptor.Name, property.StringValue);
                        }
                        writtenAttributes[property.Name] = property.Name;
                    }
                }
                else
                {
                    if (property.DependencyProperty != null)
                    {
                        // ensure xmlns are not needed on attached composite properties.
                        scope.MakeAddressable(property.DependencyProperty.OwnerType);
                    }
                    if (property.IsKey)
                    {
                        // "Property" is the key to the this dictionary entry
                        scope.MakeAddressable(NamespaceCache.XamlNamespace);
                    }
                    else if (property.IsConstructorArgument)
                    {
                        scope.MakeAddressable(NamespaceCache.XamlNamespace);
                        if (argumentCompositeIndexes == null)
                        {
                            argumentCompositeIndexes = new List<int>();
                        }
                        argumentCompositeIndexes.Add(++argumentCount);
                    }
                    if (composites == null)
                    {
                        composites = new List<MarkupProperty>();
                    }
                    composites.Add(property);
                }
            }

            foreach (Mapping mapping in scope.EnumerateLocalMappings)
            {
                // New mappings
                _writer.WriteAttributeString("xmlns", mapping.Prefix, NamespaceCache.XmlnsNamespace, mapping.Uri);
            }

            // Before we are finished, check to see if we need to emit an xml:space="preserve" attribute
            // by detecting if we are writing any raw strings out and if they will be changed due to
            // normalization. This is done by checking if the normalized version of the string would differ
            // from the non-normalized version.
            if (!scope.XmlnsSpacePreserve && contentProperty != null &&
                !HasOnlyNormalizationNeutralStrings(contentProperty, false, false))
            {
                _writer.WriteAttributeString("xml", "space", NamespaceCache.XmlNamespace, "preserve");
                scope.XmlnsSpacePreserve = true;

                // Per the documentation for XmlWriterSettings.Indent, elements are indented as
                // long as the element does not contain mixed content. Once WriteString or 
                // WriteWhiteSpace method is called to write out a mixed element content, 
                // the XmlWriter stops indenting. The indenting resumes once the mixed content
                // element is closed. 
                // 
                // It is desirable to ensure that indentation is suspended within 
                // an element with xml:space="preserve". Here, we make a dummy call to WriteString
                // to indicate to the XmlWriter that we are about to write a "mixed" element 
                // content. When we call WriteEndElement later in this method, indentation 
                // behavior will be rolled back to that of the parent element (typically, 
                // indentaiton will simply be resumed).
                // 
                // If the underlying XmlWriterSettings did not specify indentation, this would 
                // have no net effect.  
                _writer.WriteString(string.Empty);

                if( scope.IsTopOfSpacePreservationScope && _xmlTextWriter != null )
                {
                    // If we are entering a xml:space="preserve" scope and using
                    //  a XmlTextWriter, we need to turn off its formatting options.
                    // Otherwise, XmlTextWriter will continue injecting extraneous
                    //  whitespace characters.
                    _xmlTextWriter.Formatting = Formatting.None;
                }
            }


            // See the comment in property.IsConstructorArgument
            // int argumentCompositeIndex = 0;

            // Write composite properties
            if (composites != null)
            {
                foreach (MarkupProperty property in composites)
                {
                    bool propertyTagWritten = false;
                    bool explicitTagWritten = false;

                    foreach (MarkupObject subItem in property.Items)
                    {
                        if (!propertyTagWritten)
                        {
                            propertyTagWritten = true;
                            // uri is made addressable above so it is not necessary here
                            if (property.IsAttached || property.PropertyDescriptor == null)
                            {
                                Debug.Assert(!property.IsValueAsString, "Problem with MarkupObject implementation: String values cannnot be composite");

                                if (property.IsKey)
                                {
                                    // When the reader supports <x:Key> ... </x:Key> format do the following:
                                    //   _writer.WriteStartElement("Key", NamespaceCache.XamlNamespace);
                                    // The writer generates an exception for now:
                                    throw new InvalidOperationException(SR.Get(SRID.UnserializableKeyValue, property.Value.GetType().FullName));
                                }
                                else
                                {
                                    string dpUri = scope.MakeAddressable(property.DependencyProperty.OwnerType);
                                    WritePropertyStart(scope.GetPrefixOf(dpUri), property.DependencyProperty.OwnerType.Name + "." + property.DependencyProperty.Name, dpUri);
                                }
                            }
                            else
                            {
                                WritePropertyStart(prefix, item.ObjectType.Name + "." + property.PropertyDescriptor.Name, uri);

                                writtenAttributes[property.Name] = property.Name;
                            }

                            explicitTagWritten = NeedToWriteExplicitTag(property, subItem);

                            if (explicitTagWritten)
                            {
                                WriteExplicitTagStart(property, scope);
                            }
                        }
                        WriteItem(subItem, new Scope(scope));
                    }

                    if (propertyTagWritten)
                    {
                        if (explicitTagWritten)
                        {
                            // Only write an end if we started the element
                            WriteExplicitTagEnd();
                        }
                        // Only write an end if we started the element
                        WritePropertyEnd();
                    }
                }
            }

            if (contentProperty != null)
            {
                // If we have a content property, write it here
                if (contentProperty.IsComposite)
                {
                    IXmlSerializable serializable = contentProperty.Value as IXmlSerializable;
                    if (serializable != null)
                    {
                        WriteXmlIsland(serializable, scope);
                    }
                    else
                    {
                        bool lastWasString = false;
                        List<Type> wrapperTypes = GetWrapperTypes(contentProperty.PropertyType);
                        if (wrapperTypes == null)
                        {
                            foreach (MarkupObject subItem in contentProperty.Items)
                            {
                                if (!lastWasString && subItem.ObjectType == typeof(string) &&
                                    !IsCollectionType(contentProperty.PropertyType) &&
                                    !HasNonValueProperties(subItem))
                                {
                                    _writer.WriteString(TextValue(subItem));
                                    lastWasString = true;
                                }
                                else
                                {
                                    WriteItem(subItem, new Scope(scope));
                                    lastWasString = false;
                                }
                            }
                        }
                        else
                        {
                            foreach (MarkupObject subItem in contentProperty.Items)
                            {
                                MarkupProperty wrappedProperty = GetWrappedProperty(wrapperTypes, subItem);
                                if (wrappedProperty == null)
                                {
                                    WriteItem(subItem, new Scope(scope));
                                    lastWasString = false;
                                }
                                else if (wrappedProperty.IsComposite)
                                {
                                    foreach (MarkupObject wrappedItem in wrappedProperty.Items)
                                    {
                                        if (!lastWasString && subItem.ObjectType == typeof(string) && !HasNonValueProperties(subItem))
                                        {
                                            _writer.WriteString(TextValue(wrappedItem));
                                            lastWasString = true;
                                        }
                                        else
                                        {
                                            WriteItem(wrappedItem, new Scope(scope));
                                            lastWasString = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (!lastWasString)
                                    {
                                        _writer.WriteString(wrappedProperty.StringValue);
                                        lastWasString = true;
                                    }
                                    else
                                    {
                                        WriteItem(subItem, new Scope(scope));
                                        lastWasString = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    string stringContent = contentProperty.Value as string;
                    if (stringContent == null)
                    {
                        stringContent = contentProperty.StringValue;
                    }
                    _writer.WriteString(stringContent);
                }
                writtenAttributes[contentProperty.Name] = contentProperty.Name;
            }

            // Write any defered content
            if (deferredProperties != null)
            {
                // A property had a DependsOn attribute and we needed to defer it until
                // we are sure the property was written.
                foreach (MarkupProperty property in deferredProperties)
                {
                    if (!writtenAttributes.ContainsKey(property.Name))
                    {
                        Debug.Assert(property.PropertyDescriptor != null);
                        writtenAttributes[property.Name] = property.Name;
                        _writer.WriteStartElement(prefix, item.ObjectType.Name + "." + property.PropertyDescriptor.Name, uri);

                        if (property.IsComposite || property.StringValue.IndexOf('{') == 0)
                        {
                            foreach (MarkupObject subItem in property.Items)
                            {
                                WriteItem(subItem, new Scope(scope));
                            }
                        }
                        else
                        {
                            _writer.WriteString(property.StringValue);
                        }
                        _writer.WriteEndElement();
                    }
                }
            }

            // Write the end of the element
            _writer.WriteEndElement();

            if( scope.IsTopOfSpacePreservationScope &&
                _xmlTextWriter != null &&
                _xmlTextWriter.Formatting != previousFormatting )
            {
                // Exiting a xml:space="preserve" scope.  Restore formatting options
                //  if we are using a XmlTextWriter.
                _xmlTextWriter.Formatting = previousFormatting;
            }
        }

        private bool IsContentProperty(MarkupProperty property, ContentPropertyAttribute cpa, ref MarkupProperty contentProperty)
        {
            // if the property already knows it is the content property, we're done
            bool isContentProperty = property.IsContent;

            // the property may still be the content property by comparing it to the ContentPropertyAttribute
            if (!isContentProperty)
            {
                PropertyDescriptor descriptor = property.PropertyDescriptor;

                // FrameworkTemplate.VisualTree should be treated as Content
                if (descriptor != null &&
                    typeof(FrameworkTemplate).IsAssignableFrom(descriptor.ComponentType) &&
                    property.Name == "Template" || property.Name == "VisualTree")
                {
                    isContentProperty = true;
                }

                if (cpa != null &&
                    contentProperty == null &&
                    descriptor != null &&
                    descriptor.Name == cpa.Name)
                {
                    if (property.IsComposite)
                    {
                        // We shouldn't serialize read-write IList properties as
                        // content because some IAddChild implementation do not
                        // recognize the collection itself as content, just the
                        // collection's elements.
                        if (descriptor == null ||
                            descriptor.IsReadOnly ||
                            !typeof(IList).IsAssignableFrom(descriptor.PropertyType))
                        {
                            isContentProperty = true;
                        }
                    }
                    else
                    {
                        // MarkupExtensions (including null) shouldn't be written
                        // out as content.  In general, we don't want non-composite
                        // types to go out as content, because then they lose the
                        // property's type converter context.  But for
                        // string/object type properties, we allow the value to go as
                        // content, so that it is possible to write out a
                        // non-normalized string.  E.g. otherwise it would not be
                        // possible to write a Button or TextBox with tabs.

                        if (property.Value != null &&
                            !(property.Value is MarkupExtension) &&
                            property.PropertyType.IsAssignableFrom(typeof(string)) )
                        {
                            isContentProperty = true;
                        }
                    }
                }
            }

            // if for either reason the property is the content property, then assign the ref argument
            if (isContentProperty)
            {
                contentProperty = property;
            }

            return isContentProperty;
        }

        private bool IsDeferredProperty (MarkupProperty property, Dictionary<string, string> writtenAttributes,
                                               ref PartiallyOrderedList<string, MarkupProperty> deferredProperties)
        {
            bool defer = false;

            if (property.PropertyDescriptor != null)
            {
                // Since there can be multiple DependsOn attributes, we need to iterate
                foreach (Attribute attribute in property.Attributes)
                {
                    DependsOnAttribute dependsOn = attribute as DependsOnAttribute;
                    if (dependsOn != null)
                    {
                        if (!writtenAttributes.ContainsKey(dependsOn.Name))
                        {
                            // This property depends on a property that hasn't been written yet.
                            if (deferredProperties == null)
                            {
                                deferredProperties = new PartiallyOrderedList<string, MarkupProperty>();
                            }

                            // This uses a PartiallyOrderedList to ensure this property will appear
                            // after dependsOn.Name even if it also depends on property. If the
                            // DependsOn attributes are circular (e.g. A depends on B and B
                            // depends on A) the partially ordered list doesn't honor the oldest
                            // DependsOn attribute encountered resulting in a non-deterministic
                            // ordering. This error is not reported.
                            deferredProperties.SetOrder(dependsOn.Name, property.Name);
                            defer = true;
                        }
                    }
                }
                // If any of the properties this property depends on haven't been written yet
                // we need to defer the attribute.
                if (defer)
                {
                    deferredProperties.Add(property.Name, property);
                }
            }

            return defer;
        }

        /// <summary>
        /// whether the property needs to write out an explicit tag because it is a collection value with a null default value
        /// </summary>
        private bool NeedToWriteExplicitTag(MarkupProperty property, MarkupObject firstItem)
        {
            // need to write an explicit tag if ALL of the following conditions are met:
            // 1) property is a collection type
            // 2) property has a DefaultValueAttribute
            // 3) property's DefaultValueAttribute has a value of null
            // 4) the first item under the property is not already an explicit tag

            bool result = false;

            // using this verbose syntax for returning, because we may need to add additional logic later
            if (property.IsCollectionProperty)
            {
                if (_nullDefaultValueAttribute == null)
                {
                    // if this hasn't been instantiated yet, instantiate it.
                    _nullDefaultValueAttribute = new DefaultValueAttribute(null);
                }

                if (property.Attributes.Contains(_nullDefaultValueAttribute))
                {
                    result = true;

                    Object instance = firstItem.Instance;

                    if (instance is MarkupExtension)
                    {
                        if (instance is NullExtension)
                        {
                            // we allow null to be assigned as an explicit collection
                            result = false;
                        }
                        else if (property.PropertyType.IsArray)
                        {
                            // array extensions have to match up correctly, in case
                            // of an array of arrays
                            ArrayExtension arrayExt = instance as ArrayExtension;

                            if (property.PropertyType.IsAssignableFrom(arrayExt.Type.MakeArrayType()))
                            {
                                result = false;
                            }
                        }
                    }
                    else if (property.PropertyType.IsAssignableFrom(firstItem.ObjectType))
                    {
                        result = false;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// writes out the xml start tag for the given property's explicit collection
        /// </summary>
        private void WriteExplicitTagStart(MarkupProperty property, Scope scope)
        {
            Debug.Assert(property.Value != null);
            Type tagType = property.Value.GetType();

            string uri = scope.MakeAddressable(tagType);
            string prefix = scope.GetPrefixOf(uri);
            string name = tagType.Name;

            if (typeof(MarkupExtension).IsAssignableFrom(tagType) &&
                name.EndsWith("Extension", StringComparison.Ordinal))
            {
                // Prefer MarkupExtensions without the "Extension" postfix.
                name = name.Substring(0, name.Length - EXTENSIONLENGTH);
            }

            _writer.WriteStartElement(prefix, name, uri);
        }

        /// <summary>
        /// writes out the xml end tag for the given property's explicit collection
        /// </summary>
        private void WriteExplicitTagEnd()
        {
            _writer.WriteEndElement();
        }

        /// <summary>
        /// writes out the xml start tag for a property
        /// </summary>
        private void WritePropertyStart(string prefix, string propertyName, string uri)
        {
            _writer.WriteStartElement(prefix, propertyName, uri);
        }

        /// <summary>
        /// writes out the xml end tag for a property
        /// </summary>
        private void WritePropertyEnd()
        {
            _writer.WriteEndElement();
        }

        private void WriteXmlIsland(IXmlSerializable xmlSerializable, Scope scope)
        {
            scope.MakeAddressable(NamespaceCache.XamlNamespace);
            _writer.WriteStartElement(scope.GetPrefixOf(NamespaceCache.XamlNamespace), XamlReaderHelper.DefinitionXDataTag, NamespaceCache.XamlNamespace);
            xmlSerializable.WriteXml(_writer);
            _writer.WriteEndElement();
        }

        private List<Type> GetWrapperTypes(Type type)
        {
            AttributeCollection attributes = TypeDescriptor.GetAttributes(type);
            if (attributes[typeof(ContentWrapperAttribute)] == null)
                return null;
            else
            {
                List<Type> wrapperTypes = new List<Type>();
                foreach (Attribute attribute in attributes)
                {
                    ContentWrapperAttribute contentAttribute = attribute as ContentWrapperAttribute;
                    if (contentAttribute != null)
                        wrapperTypes.Add(contentAttribute.ContentWrapper);
                }
                return wrapperTypes;
            }
        }

        private MarkupProperty GetWrappedProperty(List<Type> wrapperTypes, MarkupObject item)
        {
            if (!IsInTypes(item.ObjectType, wrapperTypes))
                return null;
            ContentPropertyAttribute cpa = item.Attributes[typeof(ContentPropertyAttribute)] as ContentPropertyAttribute;
            MarkupProperty contentProperty = null;
            foreach (MarkupProperty property in item.Properties)
            {
                if (property.IsContent || (cpa != null && property.PropertyDescriptor != null && property.PropertyDescriptor.Name == cpa.Name))
                    contentProperty = property;
                else
                {
                    // If we get any other property than the content property, need to serialize the wrapper type explicitly.
                    contentProperty = null;
                    break;
                }
            }
            return contentProperty;
        }

        private bool IsInTypes(Type type, List<Type> types)
        {
            foreach (Type t in types)
                if (t == type) return true;
            return false;
        }

        private string TextValue(MarkupObject item)
        {
            foreach (MarkupProperty property in item.Properties)
            {
                if (property.IsValueAsString)
                    return property.StringValue;
                break;
            }
            return null;
        }

        private bool HasNonValueProperties(MarkupObject item)
        {
            foreach (MarkupProperty property in item.Properties)
            {
                if (!property.IsValueAsString)
                    return true;
            }
            return false;
        }

        private bool IsCollectionType(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) ||
                typeof(Array).IsAssignableFrom(type);
        }

        private bool HasOnlyNormalizationNeutralStrings(MarkupProperty contentProperty,
            bool keepLeadingSpace, bool keepTrailingSpace)
        {
            if (!contentProperty.IsComposite)
                return IsNormalizationNeutralString(contentProperty.StringValue, keepLeadingSpace,
                    keepTrailingSpace);
            else
            {
                // A leading space is only kept if it the string is just after an inline.
                // A trailing space is only kept if it is just before an inline.
                // To determine this we will examine the list until we find an item just after
                // a string. We then have all the information necessary to determine if the
                // string is normalization neutral. If any of the strings in the content are
                // not normalization neutral then we we need a xml:space="preserve" so we terminate
                // early.

                bool result = true;
                bool currentTrimSurroundingWhitespace = !keepLeadingSpace;
                bool previousTrimSurroundingWhitespace = !keepLeadingSpace;
                string text = null;
                MarkupProperty nestedContentProperty = null;
                List<Type> wrapperTypes = GetWrapperTypes(contentProperty.PropertyType);

                foreach (MarkupObject subItem in contentProperty.Items)
                {
                    previousTrimSurroundingWhitespace = currentTrimSurroundingWhitespace;
                    currentTrimSurroundingWhitespace = ShouldTrimSurroundingWhitespace(subItem);

                    if (text != null)
                    {
                        result = IsNormalizationNeutralString(text, !previousTrimSurroundingWhitespace,
                            !currentTrimSurroundingWhitespace);
                        text = null;
                        if (!result) return false;
                    }
                    if (nestedContentProperty != null)
                    {
                        result = HasOnlyNormalizationNeutralStrings(nestedContentProperty,
                            !previousTrimSurroundingWhitespace, !currentTrimSurroundingWhitespace);
                        nestedContentProperty = null;
                        if (!result) return false;
                    }

                    if (subItem.ObjectType == typeof(string))
                    {
                        text = TextValue(subItem);
                        if (text != null)
                            continue;
                    }
                    if (wrapperTypes != null)
                    {
                        MarkupProperty wrappedProperty = GetWrappedProperty(wrapperTypes, subItem);
                        if (wrappedProperty != null)
                        {
                            nestedContentProperty = wrappedProperty;
                            continue;
                        }
                    }
                }

                if (text != null)
                {
                    // The last element was a string. Determine if it is normalization neutural.
                    result = IsNormalizationNeutralString(text, !previousTrimSurroundingWhitespace,
                        keepTrailingSpace);
                }
                else if (nestedContentProperty != null)
                {
                    // The last element was a wrapped element. Determine if it is normalization neutural.
                    result = HasOnlyNormalizationNeutralStrings(nestedContentProperty,
                        !previousTrimSurroundingWhitespace, keepTrailingSpace);
                }
                return result;
            }
        }

        private bool ShouldTrimSurroundingWhitespace(MarkupObject item)
        {
            // An item declares how surrounding whitespace should be treated with the TrimSurroundingWhitespaceAttribute.
            TrimSurroundingWhitespaceAttribute attribute = item.Attributes[typeof(TrimSurroundingWhitespaceAttribute)] as TrimSurroundingWhitespaceAttribute;
            return (attribute != null);
        }

        private bool IsNormalizationNeutralString(string value, bool keepLeadingSpace, bool keepTrailingSpace)
        {
            bool lastCharSpace = !keepLeadingSpace;

            for (int textIndex = 0; textIndex < value.Length; textIndex++)
            {
                char currentChar = value[textIndex];
                switch (currentChar)
                {
                    case ' ':
                        if (lastCharSpace)
                            // two spaces or more spaces in a row (or a leading space) will be normalized
                            return false;
                        lastCharSpace = true;
                        break;
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                        // These will be turned into spaces.
                        return false;
                    default:
                        // Anything else is left alone.
                        lastCharSpace = false;
                        break;

                    // Chinese/Japanese and Thai rules relate to LF handling and any LF character
                    // will be treated as non-neutral so they don't need special handling.
                }
            }

            return !lastCharSpace || keepTrailingSpace;
        }

        /// <summary>
        /// Private class used by Scope to record a prefix to URI mapping.
        /// </summary>
        private class Mapping
        {
            public readonly string Uri;
            public readonly string Prefix;

            public Mapping(string uri, string prefix)
            {
                Uri = uri;
                Prefix = prefix;
            }

            public override bool Equals(object obj)
            {
                Mapping other = obj as Mapping;
                return other != null && Uri.Equals(other.Uri) && Prefix.Equals(other.Prefix);
            }

            public override int GetHashCode()
            {
                return Uri.GetHashCode() + Prefix.GetHashCode();
            }
        }

        /// <summary>
        /// Private class used by MarkupWriter to record xmlns defintions emitted
        /// by the writer.
        /// </summary>
        private class Scope
        {
            private Scope _containingScope;
            private bool? _xmlnsSpacePreserve;

            private Dictionary<string, string> _uriToPrefix;
            private Dictionary<string, string> _prefixToUri;

            public Scope(Scope containingScope)
            {
                _containingScope = containingScope;
            }

            public bool XmlnsSpacePreserve
            {
                get
                {
                    if (_xmlnsSpacePreserve != null)
                        return (bool)_xmlnsSpacePreserve;
                    else if (_containingScope != null)
                        return _containingScope.XmlnsSpacePreserve;
                    else
                        return false;
                }
                set
                {
                    _xmlnsSpacePreserve = value;
                }
            }

            // Returns true if this scope is the top of a space preservation scope.
            // True if:
            //   1) This is the outermost scope.  (No containing scope)
            //   2) The containing scope has a different space preservation setting
            //      than the current scope.
            // This information is derived from _xmlnsSpacePreserve and _containingScope
            //   to avoid memory cost of tracking additional object state.
            public bool IsTopOfSpacePreservationScope
            {
                get
                {
                    if( _containingScope == null )
                    {
                        // Topmost element is top of a scope by definition.
                        return true;
                    }
                    else if( _xmlnsSpacePreserve == null )
                    {
                        // Most common case - this scope inherits the parent's
                        //  scope, so it's not a top level preservation scope.
                        return false;
                    }
                    else if( ((bool)_xmlnsSpacePreserve) != _containingScope.XmlnsSpacePreserve )
                    {
                        // Our specified preservation setting does not match containing scope's
                        //  preservation setting - we're top of our specific preservations cope.
                        return true;
                    }

                    // We have a specified preservation setting that matches the
                    //  containing scope's preservation setting.
                    Debug.Assert( ((bool)_xmlnsSpacePreserve) == _containingScope.XmlnsSpacePreserve ,
                        "At this point the space preservation settings should be equal - did somebody break the logic above us?");
                    return false;
                }
            }

            public string GetPrefixOf(string uri)
            {
                string result;
                if (_uriToPrefix != null && _uriToPrefix.TryGetValue(uri, out result))
                    return result;
                if (_containingScope != null)
                    return _containingScope.GetPrefixOf(uri);
                return null;
            }

            public string GetUriOf(string prefix)
            {
                string result;
                if (_prefixToUri != null && _prefixToUri.TryGetValue(prefix, out result))
                    return result;
                if (_containingScope != null)
                    return _containingScope.GetUriOf(prefix);
                return null;
            }

            public void RecordMapping(string prefix, string uri)
            {
                if (_uriToPrefix == null)
                    _uriToPrefix = new Dictionary<string, string>();
                if (_prefixToUri == null)
                    _prefixToUri = new Dictionary<string, string>();
                _uriToPrefix[uri] = prefix;
                _prefixToUri[prefix] = uri;
            }

            public void MakeAddressable(IEnumerable<Type> types)
            {
                if (types != null)
                    foreach (Type type in types)
                        MakeAddressable(type);
            }

            public string MakeAddressable(Type type)
            {
                return MakeAddressable(NamespaceCache.GetNamespaceUriFor(type));
            }

            public string MakeAddressable(string uri)
            {
                if (GetPrefixOf(uri) == null)
                {
                    string basePrefix = NamespaceCache.GetDefaultPrefixFor(uri);
                    string prefix = basePrefix;
                    int i = 0;
                    while (GetUriOf(prefix) != null)
                        prefix = string.Concat(basePrefix, i++);
                    RecordMapping(prefix, uri);
                }
                return uri;
            }

            public IEnumerable<Mapping> EnumerateLocalMappings
            {
                get
                {
                    if (_uriToPrefix != null)
                        foreach (KeyValuePair<string, string> mapping in _uriToPrefix)
                            yield return new Mapping(mapping.Key, mapping.Value);
                }
            }

            public IEnumerable<Mapping> EnumerateAllMappings
            {
                get
                {
                    if (_containingScope != null)
                        foreach (Mapping mapping in _containingScope.EnumerateAllMappings)
                            yield return mapping;
                    foreach (Mapping mapping in EnumerateLocalMappings)
                        yield return mapping;
                }
            }
        }

        /// <summary>
        /// A IValueSerializerContext that provides a serializer for Type's.
        /// </summary>
        private class MarkupWriterContext : IValueSerializerContext
        {
            Scope _scope;

            internal MarkupWriterContext(Scope scope)
            {
                _scope = scope;
            }

            public ValueSerializer GetValueSerializerFor(PropertyDescriptor descriptor)
            {
                if (descriptor.PropertyType == typeof(Type))
                    return new TypeValueSerializer(_scope);
                else
                    return ValueSerializer.GetSerializerFor(descriptor);
            }

            public ValueSerializer GetValueSerializerFor(Type type)
            {
                if (type == typeof(Type))
                    return new TypeValueSerializer(_scope);
                else
                    return ValueSerializer.GetSerializerFor(type);
            }

            public IContainer Container
            {
                get { return null;  }
            }

            public object Instance
            {
                get { return null; }
            }

            public void OnComponentChanged()
            {
            }

            public bool OnComponentChanging()
            {
                return true;
            }

            public PropertyDescriptor PropertyDescriptor
            {
                get { return null;  }
            }

            public object GetService(Type serviceType)
            {
                return null;
            }
        }

        /// <summary>
        /// A TypeValueSerializer that converts a Type instance to a string using
        /// the Scope.
        /// </summary>
        private class TypeValueSerializer : ValueSerializer
        {
            Scope _scope;

            public TypeValueSerializer(Scope scope)
            {
                _scope = scope;
            }

            public override bool CanConvertToString(object value, IValueSerializerContext context)
            {
                return true;
            }

            public override string ConvertToString(object value, IValueSerializerContext context)
            {
                Type type = value as Type;
                if (type == null)
                    throw new InvalidOperationException();
                string uri = _scope.MakeAddressable(type);
                string prefix = _scope.GetPrefixOf(uri);
                if (prefix == null || prefix == "")
                    return type.Name;
                else
                    return prefix + ":" + type.Name;
            }

            public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context)
            {
                Type type = value as Type;
                if (type != null)
                    return new Type[] { type };
                else
                    return base.TypeReferences(value, context);
            }
        }

        /// <summary>
        /// A private cache of namespace information derived from assemblies and their XmlnsDefintionAttribute's.
        /// </summary>
        private static class NamespaceCache
        {
            private static Dictionary<Assembly, Dictionary<string, string>> XmlnsDefinitions = new Dictionary<Assembly, Dictionary<string, string>>();
            private static Dictionary<string, string> DefaultPrefixes = new Dictionary<string, string>();
            private static object SyncObject = new object();

            static Dictionary<string, string> GetMappingsFor(Assembly assembly)
            {
                Dictionary<string, string> namespaceToUri;
                lock (SyncObject)
                {
                    if (!XmlnsDefinitions.TryGetValue(assembly, out namespaceToUri))
                    {
                        foreach (XmlnsPrefixAttribute prefix in assembly.GetCustomAttributes(typeof(XmlnsPrefixAttribute), true))
                            DefaultPrefixes[prefix.XmlNamespace] = prefix.Prefix;

                        namespaceToUri = new Dictionary<string, string>();
                        XmlnsDefinitions[assembly] = namespaceToUri;

                        Object[] customAttrs = assembly.GetCustomAttributes(typeof(XmlnsDefinitionAttribute), true);
                        foreach (XmlnsDefinitionAttribute definition in customAttrs)
                        {
                            if (definition.AssemblyName == null)
                            {
                                string previousBestNamespace = null;
                                string previousBestPrefix = null;
                                string newPrefix = null;

                                // if multiple namespaces for the type exits then give shorter prefix
                                // definitions precedence.
                                if(namespaceToUri.TryGetValue(definition.ClrNamespace, out previousBestNamespace))
                                {
                                    if(DefaultPrefixes.TryGetValue(previousBestNamespace, out previousBestPrefix))
                                    {
                                        DefaultPrefixes.TryGetValue(definition.XmlNamespace, out newPrefix);
                                    }
                                }
                                if ( (null == previousBestNamespace) || (null == previousBestPrefix) ||
                                    (null != newPrefix && previousBestPrefix.Length > newPrefix.Length) )
                                {
                                    namespaceToUri[definition.ClrNamespace] = definition.XmlNamespace;
                                }
                            }
                            else
                            {
                                Assembly referencedAssembly = Assembly.Load(new AssemblyName(definition.AssemblyName));
                                if (referencedAssembly != null)
                                {
                                    Dictionary<string, string> assembliesNamespacetoUri = GetMappingsFor(referencedAssembly);
                                    assembliesNamespacetoUri[definition.ClrNamespace] = definition.XmlNamespace;
                                }
                            }
                        }
                    }
                }
                return namespaceToUri;
            }

            public static string GetNamespaceUriFor(Type type)
            {
                string result;
                lock (SyncObject)
                {
                    if (type.Namespace == null)
                    {
                        result = string.Format(CultureInfo.InvariantCulture, clrUriPrefix + ";assembly={0}",
                            type.Assembly.GetName().Name);
                    }
                    else
                    {
                        Dictionary<string, string> namespaceToUri = GetMappingsFor(type.Assembly);
                        if (!namespaceToUri.TryGetValue(type.Namespace, out result))
                        {
                            result = string.Format(CultureInfo.InvariantCulture, clrUriPrefix + "{0};assembly={1}", type.Namespace,
                                type.Assembly.GetName().Name);
                        }
                    }
                }
                return result;
            }

            public static string GetDefaultPrefixFor(string uri)
            {
                string result;
                lock (SyncObject)
                {
                    DefaultPrefixes.TryGetValue(uri, out result);
                    if (result == null)
                    {
                        result = "assembly";
                        if (uri.StartsWith(clrUriPrefix, StringComparison.Ordinal))
                        {
                            string ns = uri.Substring(clrUriPrefix.Length, uri.IndexOf(';') - clrUriPrefix.Length);
                            StringBuilder r = new StringBuilder();
                            for (int i = 0; i < ns.Length; i++)
                            {
                                char c = ns[i];
                                if (c >= 'A' && c <= 'Z')
                                    r.Append(c.ToString().ToLower(CultureInfo.InvariantCulture));
                            }
                            if (r.Length > 0)
                                result = r.ToString();
                        }
                    }
                }
                return result;
            }

            public static string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
            public static string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
            public static string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        }
        #endregion

        private XmlWriter _writer;

        // Same reference as _writer, but cast to a XmlTextWriter.  This is to work
        //  around a XmlTextWriter-specific issue where it would inject extra whitespaces
        //  to make XML human-readable even when whitespaces are significant.
        //  (xml:space="preserve" is set to true.)
        private XmlTextWriter _xmlTextWriter;

        private static DefaultValueAttribute _nullDefaultValueAttribute;
    }
}
