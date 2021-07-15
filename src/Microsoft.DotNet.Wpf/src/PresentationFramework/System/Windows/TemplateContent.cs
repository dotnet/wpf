// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xaml;
using System.Xaml.Schema;
using System.Xaml.Permissions;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Diagnostics;
using System.Windows.Markup;
using System.Globalization;
using MS.Utility;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Security;
using MS.Internal.Xaml.Context;
using System.Windows.Baml2006;

namespace System.Windows
{
    //TemplateContent is meant to hold any data that is needed during TemplateLoad and TemplateApply.
    // _templateLoadData holds all other data.
    [XamlDeferLoad(typeof(TemplateContentLoader), typeof(FrameworkElement))]
    public class TemplateContent
    {
        internal class Frame : XamlFrame
        {
            private FrugalObjectList<System.Xaml.NamespaceDeclaration> _namespaces;
            private System.Xaml.XamlType _xamlType;

            public Frame() { }

            public XamlType Type
            {
                get { return _xamlType; }
                set
                {
                    // can't change the type (except from null)
                    Debug.Assert(_xamlType == null);

                    _xamlType = value;
                }
            }

            public XamlMember Property { get; set; }
            public string Name { get; set; }
            public bool NameSet { get; set; }
            public bool IsInNameScope { get; set; }
            public bool IsInStyleOrTemplate { get; set; }
            public object Instance { get; set; }

            //ContentPresenter
            public bool ContentSet { get; set; }
            public bool ContentSourceSet { get; set; }
            public String ContentSource { get; set; }
            public bool ContentTemplateSet { get; set; }
            public bool ContentTemplateSelectorSet { get; set; }
            public bool ContentStringFormatSet { get; set; }

            //GridViewRowPresenter
            public bool ColumnsSet { get; set; }

            public override void Reset()
            {
                _xamlType = null;
                Property = null;
                Name = null;
                NameSet = false;
                IsInNameScope = false;
                Instance = null;
                ContentSet = false;
                ContentSourceSet = false;
                ContentSource = null;
                ContentTemplateSet = false;
                ContentTemplateSelectorSet = false;
                ContentStringFormatSet = false;
                IsInNameScope = false;
                if (HasNamespaces)
                {
                    _namespaces = null;
                }
            }

            public FrugalObjectList<System.Xaml.NamespaceDeclaration> Namespaces
            {
                get
                {
                    if (_namespaces == null)
                    {
                        _namespaces = new FrugalObjectList<System.Xaml.NamespaceDeclaration>();
                    }
                    return _namespaces;
                }
            }

            public bool HasNamespaces { get { return _namespaces != null && _namespaces.Count > 0; } }

            public override string ToString()
            {
                string type = (this.Type == null) ? String.Empty : this.Type.Name;
                string prop = (this.Property == null) ? "-" : this.Property.Name;
                string inst = (Instance == null) ? "-" : "*";
                string res = String.Format(CultureInfo.InvariantCulture,
                    "{0}.{1} inst={2}", type, prop, inst);
                return res;
            }
        }

        internal class StackOfFrames : XamlContextStack<Frame>
        {
            public StackOfFrames() : base(()=>new Frame()) { }

            public void Push(System.Xaml.XamlType xamlType, string name)
            {
                bool isInNameScope = false;
                bool isInStyleOrTemplate = false;

                if (Depth > 0)
                {
                    isInNameScope = CurrentFrame.IsInNameScope || (CurrentFrame.Type != null && FrameworkTemplate.IsNameScope(CurrentFrame.Type));
                    isInStyleOrTemplate = CurrentFrame.IsInStyleOrTemplate ||
                        (CurrentFrame.Type != null &&
                            (typeof(FrameworkTemplate).IsAssignableFrom(CurrentFrame.Type.UnderlyingType) ||
                             typeof(Style).IsAssignableFrom(CurrentFrame.Type.UnderlyingType)));
                }

                if (Depth == 0 || CurrentFrame.Type != null)
                {
                    base.PushScope();
                }

                CurrentFrame.Type = xamlType;
                CurrentFrame.Name = name;
                CurrentFrame.IsInNameScope = isInNameScope;
                CurrentFrame.IsInStyleOrTemplate = isInStyleOrTemplate;
            }

            public void AddNamespace(System.Xaml.NamespaceDeclaration nsd)
            {
                bool isInNameScope = false;
                bool isInStyleOrTemplate = false;

                if (Depth > 0)
                {
                    isInNameScope = CurrentFrame.IsInNameScope || (CurrentFrame.Type != null && FrameworkTemplate.IsNameScope(CurrentFrame.Type));
                    isInStyleOrTemplate = CurrentFrame.IsInStyleOrTemplate ||
                        (CurrentFrame.Type != null &&
                            (typeof(FrameworkTemplate).IsAssignableFrom(CurrentFrame.Type.UnderlyingType) ||
                             typeof(Style).IsAssignableFrom(CurrentFrame.Type.UnderlyingType)));
                }

                if (Depth == 0 || CurrentFrame.Type != null)
                {
                    base.PushScope();
                }

                CurrentFrame.Namespaces.Add(nsd);
                CurrentFrame.IsInNameScope = isInNameScope;
                CurrentFrame.IsInStyleOrTemplate = isInStyleOrTemplate;
            }

            public FrugalObjectList<System.Xaml.NamespaceDeclaration> InScopeNamespaces
            {
                get
                {
                    FrugalObjectList<System.Xaml.NamespaceDeclaration> allNamespaces = null;
                    Frame iteratorFrame = this.CurrentFrame;

                    while (iteratorFrame != null)
                    {
                        if (iteratorFrame.HasNamespaces)
                        {
                            if (allNamespaces == null)  // late allocation cause there is often nothing.
                            {
                                allNamespaces = new FrugalObjectList<System.Xaml.NamespaceDeclaration>();
                            }
                            for (int idx = 0; idx < iteratorFrame.Namespaces.Count; idx++)
                            {
                                allNamespaces.Add(iteratorFrame.Namespaces[idx]);
                            }
                        }
                        iteratorFrame = (Frame)iteratorFrame.Previous;
                    }
                    return allNamespaces;
                }
            }
        }

        internal TemplateContent(System.Xaml.XamlReader xamlReader, IXamlObjectWriterFactory factory,
            IServiceProvider context)
        {
            TemplateLoadData = new TemplateLoadData();
            ObjectWriterFactory = factory;
            SchemaContext = xamlReader.SchemaContext;
            ObjectWriterParentSettings = factory.GetParentSettings();

            XamlAccessLevel accessLevel = ObjectWriterParentSettings.AccessLevel;
            TemplateLoadData.Reader = xamlReader;

            Initialize(context);
        }

        private void Initialize(IServiceProvider context)
        {
            XamlObjectWriterSettings settings = System.Windows.Markup.XamlReader.
                CreateObjectWriterSettings(ObjectWriterParentSettings);
            settings.AfterBeginInitHandler = delegate(object sender, System.Xaml.XamlObjectEventArgs args)
            {
                //In several situations this even will happen with Stack == null.
                //If this event was on the XOW, perhaps we could stop listening in some circumstances?
                if (Stack != null && Stack.Depth > 0)
                {
                    Stack.CurrentFrame.Instance = args.Instance;
                }
            };
            settings.SkipProvideValueOnRoot = true;
            TemplateLoadData.ObjectWriter = ObjectWriterFactory.GetXamlObjectWriter(settings);
            TemplateLoadData.ServiceProviderWrapper = new ServiceProviderWrapper(context, SchemaContext);

            IRootObjectProvider irop = context.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
            if (irop != null)
            {
                TemplateLoadData.RootObject = irop.RootObject;
            }
        }

        // This needs to take the XamlReader passed in and sort it into things that
        // can be shared and can't be shared.  Anything that can't be shared
        // needs to be stored away so that we can instantiate at template load time.
        // Shared values need to be stored into the shared value tables.
        internal void ParseXaml()
        {
            Debug.Assert(TemplateLoadData.Reader != null);
            StackOfFrames stack = new StackOfFrames();

            TemplateLoadData.ServiceProviderWrapper.Frames = stack;

            OwnerTemplate.StyleConnector = TemplateLoadData.RootObject as IStyleConnector;
            TemplateLoadData.RootObject = null;

            List<PropertyValue> sharedProperties = new List<PropertyValue>();

            int nameNumber = 1;
            ParseTree(stack, sharedProperties, ref nameNumber);

            // Items panel templates have special rules
            if (OwnerTemplate is ItemsPanelTemplate)
            {
                PropertyValue pv = new PropertyValue();
                pv.ValueType = PropertyValueType.Set;
                pv.ChildName = TemplateLoadData.RootName;
                pv.ValueInternal = true;
                pv.Property = Panel.IsItemsHostProperty;

                sharedProperties.Add(pv);
            }

            // Add all the shared properties to the special table
            for (int i = 0; i < sharedProperties.Count; i++)
            {
                PropertyValue value = sharedProperties[i];

                if (value.ValueInternal is TemplateBindingExtension)  // Use ValueInternal to avoid creating deferred resource references
                {
                    value.ValueType = PropertyValueType.TemplateBinding;
                }
                else if (value.ValueInternal is DynamicResourceExtension) // Use ValueInternal to avoid creating deferred resource references
                {
                    DynamicResourceExtension dynamicResource = value.Value as DynamicResourceExtension;

                    value.ValueType = PropertyValueType.Resource;
                    value.ValueInternal = dynamicResource.ResourceKey;
                }
                else
                {
                    StyleHelper.SealIfSealable(value.ValueInternal);
                }

                StyleHelper.UpdateTables(ref value, ref OwnerTemplate.ChildRecordFromChildIndex, ref OwnerTemplate.TriggerSourceRecordFromChildIndex,
                    ref OwnerTemplate.ResourceDependents, ref OwnerTemplate._dataTriggerRecordFromBinding, OwnerTemplate.ChildIndexFromChildName, ref OwnerTemplate._hasInstanceValues);
            }

            //We don't need to use this object writer anymore so let's clear it out.
            TemplateLoadData.ObjectWriter = null;
        }

        internal System.Xaml.XamlReader PlayXaml()
        {
            return _xamlNodeList.GetReader();
        }

        //Called by FrameworkTemplate.Seal() to let go of the data used for Template Load.
        internal void ResetTemplateLoadData()
        {
            TemplateLoadData = null;
        }

        //+------------------------------------------------------------------------------------------
        //
        //  UpdateSharedList
        //
        //  Update the last items on the shared DependencyProperty list with the
        //  name of the current element.
        //
        //+------------------------------------------------------------------------------------------
        private void UpdateSharedPropertyNames(string name, List<PropertyValue> sharedProperties, XamlType type)
        {
#if DEBUG
            int lastIndex = OwnerTemplate.LastChildIndex;
#endif

            // Generate an index for this name
            int childIndex = StyleHelper.CreateChildIndexFromChildName(name, OwnerTemplate);

            OwnerTemplate.ChildNames.Add(name);
            OwnerTemplate.ChildTypeFromChildIndex.Add(childIndex, type.UnderlyingType);

#if DEBUG
            Debug.Assert(childIndex == lastIndex);
#endif

            // The tail of the _sharedProperties list has the properties for this element,
            // all with no name.  Fill in the name now.

            for (int i = sharedProperties.Count - 1; i >= 0; i--)
            {
                PropertyValue sdp = sharedProperties[i];

                if (sdp.ChildName == null)
                {
                    sdp.ChildName = name;
                }
                else
                {
                    break;
                }

                sharedProperties[i] = sdp;
            }
        }

        private void ParseTree(
            StackOfFrames stack,
            List<PropertyValue> sharedProperties,
            ref int nameNumber)
        {
            ParseNodes(stack, sharedProperties, ref nameNumber);
        }

        private void ParseNodes(
            StackOfFrames stack,
            List<PropertyValue> sharedProperties,
            ref int nameNumber)
        {
            _xamlNodeList = new XamlNodeList(SchemaContext);
            System.Xaml.XamlWriter writer = _xamlNodeList.Writer;
            System.Xaml.XamlReader reader = TemplateLoadData.Reader;

            // Prepare to provide source info if needed
            IXamlLineInfoConsumer lineInfoConsumer = null;
            IXamlLineInfo lineInfo = null;
            if (XamlSourceInfoHelper.IsXamlSourceInfoEnabled)
            {
                lineInfo = reader as IXamlLineInfo;
                if (lineInfo != null)
                {
                    lineInfoConsumer = writer as IXamlLineInfoConsumer;
                }
            }

            while (reader.Read())
            {
                if (lineInfoConsumer != null)
                {
                    lineInfoConsumer.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
                }

                object newValue;
                bool reProcessOnApply = ParseNode(reader, stack, sharedProperties, ref nameNumber, out newValue);
                if (reProcessOnApply)
                {
                    if (newValue == DependencyProperty.UnsetValue)
                    {
                        writer.WriteNode(reader);
                    }
                    else
                    {
                        writer.WriteValue(newValue);
                    }
                }
            }
            writer.Close();
            TemplateLoadData.Reader = null;
        }

        // Returns true if the node should be re-processed on template apply. If the value is
        // shareable, the node doesn't need to be reprocessed, so we return false.
        private bool ParseNode(System.Xaml.XamlReader xamlReader,
            StackOfFrames stack,
            List<PropertyValue> sharedProperties,
            ref int nameNumber,
            out object newValue)
        {
            newValue = DependencyProperty.UnsetValue;
            switch (xamlReader.NodeType)
            {
                case System.Xaml.XamlNodeType.StartObject:
                    {
                        // Process the load-time binding of StaticResources.
                        // SR usage in RD's will be instance Values coming from the BAML reader.
                        // SR in node list form are from inline templates.  (or XML text)
                        // V3 also hard codes type EQUALITY against StaticResourceExtension
                        if (xamlReader.Type.UnderlyingType == typeof(StaticResourceExtension))
                        {
                            // Use the shared XamlObjectWriter but clear the state first
                            XamlObjectWriter writer = TemplateLoadData.ObjectWriter;
                            writer.Clear();
                            WriteNamespaces(writer, stack.InScopeNamespaces, null);

                            // newValue is an out parameter that will change the value in the processed node stream.
                            newValue = LoadTimeBindUnshareableStaticResource(xamlReader, writer);
                            return true;
                        }

                        // Check to see if the parent object needs to have the name set
                        if (stack.Depth > 0 &&
                            stack.CurrentFrame.NameSet == false &&
                            stack.CurrentFrame.Type != null &&
                            !stack.CurrentFrame.IsInNameScope &&
                            !stack.CurrentFrame.IsInStyleOrTemplate)
                        {
                            // FEs and FCEs need to be added to the name to index map
                            if (typeof(FrameworkElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType) ||
                                typeof(FrameworkContentElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                            {
                                // All FEs and FCEs must have a name.  We assign a default if there was no name provided
                                string name = nameNumber++.ToString(CultureInfo.InvariantCulture) + "_T";
                                UpdateSharedPropertyNames(name, sharedProperties, stack.CurrentFrame.Type);
                                stack.CurrentFrame.Name = name;
                            }
                            stack.CurrentFrame.NameSet = true;
                        }

                        if (RootType == null)
                        {
                            RootType = xamlReader.Type;
                        }
                        stack.Push(xamlReader.Type, null);
                    }
                    break;
                case System.Xaml.XamlNodeType.GetObject:
                    {
                        // Check to see if the parent object needs to have the name set
                        if (stack.Depth > 0 &&
                            stack.CurrentFrame.NameSet == false &&
                            stack.CurrentFrame.Type != null &&
                            !stack.CurrentFrame.IsInNameScope &&
                            !stack.CurrentFrame.IsInStyleOrTemplate)
                        {
                            // FEs and FCEs need to be added to the name to index map
                            if (typeof(FrameworkElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType) ||
                                typeof(FrameworkContentElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                            {
                                // All FEs and FCEs must have a name.  We assign a default if there was no name provided
                                string name = nameNumber++.ToString(CultureInfo.InvariantCulture) + "_T";
                                UpdateSharedPropertyNames(name, sharedProperties, stack.CurrentFrame.Type);
                                stack.CurrentFrame.Name = name;
                            }
                            stack.CurrentFrame.NameSet = true;
                        }

                        XamlType type = stack.CurrentFrame.Property.Type;
                        if (RootType == null)
                        {
                            RootType = type;
                        }
                        stack.Push(type, null);
                    }
                    break;
                case System.Xaml.XamlNodeType.EndObject:
                    if (!stack.CurrentFrame.IsInStyleOrTemplate)
                    {
                        if (stack.CurrentFrame.NameSet == false && !stack.CurrentFrame.IsInNameScope)
                        {
                            // FEs and FCEs need to be added to the name to index map
                            if (typeof(FrameworkElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType) ||
                                typeof(FrameworkContentElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                            {
                                // All FEs and FCEs must have a name.  We assign a default if there was no name provided
                                string name = nameNumber++.ToString(CultureInfo.InvariantCulture) + "_T";
                                UpdateSharedPropertyNames(name, sharedProperties, stack.CurrentFrame.Type);
                                stack.CurrentFrame.Name = name;
                            }
                            stack.CurrentFrame.NameSet = true;
                        }

                        if (TemplateLoadData.RootName == null && stack.Depth == 1)
                        {
                            TemplateLoadData.RootName = stack.CurrentFrame.Name;
                        }

                        // ContentPresenters have special rules for aliasing Content
                        if (typeof(ContentPresenter).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                        {
                            AutoAliasContentPresenter(
                                OwnerTemplate.TargetTypeInternal,
                                stack.CurrentFrame.ContentSource,
                                stack.CurrentFrame.Name,
                                ref OwnerTemplate.ChildRecordFromChildIndex,
                                ref OwnerTemplate.TriggerSourceRecordFromChildIndex,
                                ref OwnerTemplate.ResourceDependents,
                                ref OwnerTemplate._dataTriggerRecordFromBinding,
                                ref OwnerTemplate._hasInstanceValues,
                                OwnerTemplate.ChildIndexFromChildName,
                                stack.CurrentFrame.ContentSet,
                                stack.CurrentFrame.ContentSourceSet,
                                stack.CurrentFrame.ContentTemplateSet,
                                stack.CurrentFrame.ContentTemplateSelectorSet,
                                stack.CurrentFrame.ContentStringFormatSet
                                );
                        }

                        // GridViewRowPresenters have special rules for aliasing Content
                        if (typeof(GridViewRowPresenter).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                        {
                            AutoAliasGridViewRowPresenter(
                                OwnerTemplate.TargetTypeInternal,
                                stack.CurrentFrame.ContentSource,
                                stack.CurrentFrame.Name,
                                ref OwnerTemplate.ChildRecordFromChildIndex,
                                ref OwnerTemplate.TriggerSourceRecordFromChildIndex,
                                ref OwnerTemplate.ResourceDependents,
                                ref OwnerTemplate._dataTriggerRecordFromBinding,
                                ref OwnerTemplate._hasInstanceValues,
                                OwnerTemplate.ChildIndexFromChildName,
                                stack.CurrentFrame.ContentSet,
                                stack.CurrentFrame.ColumnsSet
                                );
                        }
                    }

                    stack.PopScope();
                    break;

                case System.Xaml.XamlNodeType.StartMember:
                    stack.CurrentFrame.Property = xamlReader.Member;

                    if (!stack.CurrentFrame.IsInStyleOrTemplate)
                    {

                        // Need to know if these properties are set for
                        // autoaliasing.
                        if (typeof(GridViewRowPresenter).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                        {
                            if (xamlReader.Member.Name == "Content")
                                stack.CurrentFrame.ContentSet = true;
                            else if (xamlReader.Member.Name == "Columns")
                                stack.CurrentFrame.ColumnsSet = true;
                        }
                        else if (typeof(ContentPresenter).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                        {
                            if (xamlReader.Member.Name == "Content")
                                stack.CurrentFrame.ContentSet = true;
                            else if (xamlReader.Member.Name == "ContentTemplate")
                                stack.CurrentFrame.ContentTemplateSet = true;
                            else if (xamlReader.Member.Name == "ContentTemplateSelector")
                                stack.CurrentFrame.ContentTemplateSelectorSet = true;
                            else if (xamlReader.Member.Name == "ContentStringFormat")
                                stack.CurrentFrame.ContentStringFormatSet = true;
                            else if (xamlReader.Member.Name == "ContentSource")
                                stack.CurrentFrame.ContentSourceSet = true;
                        }

                        if (!stack.CurrentFrame.IsInNameScope &&
                            xamlReader.Member.IsDirective == false)
                        {

                            // Try to see if the property is shareable
                            PropertyValue? sharedValue;
                            var iReader = xamlReader as IXamlIndexingReader;
                            Debug.Assert(iReader != null, "Template's Reader is not a Indexing Reader");

                            bool sharable = false;
                            int savedIdx = iReader.CurrentIndex;

                            try
                            {
                                sharable = TrySharingProperty(xamlReader,
                                        stack.CurrentFrame.Type,
                                        stack.CurrentFrame.Name,
                                        stack.InScopeNamespaces,
                                        out sharedValue);
                            }
                            catch
                            {
                                sharable = false;
                                sharedValue = null;
                            }

                            // Property is NOT shareable.
                            // Back-up and add the unsharable section.
                            if (!sharable)
                            {
                                iReader.CurrentIndex = savedIdx;
                                break;
                            }
                            else
                            {
                                // Value can be shared.
                                // Add it to the shared properties list

                                Debug.Assert(sharedValue != null);
                                sharedProperties.Add(sharedValue.Value);

                                if (typeof(GridViewRowPresenter).
                                    IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType) ||
                                    typeof(ContentPresenter).
                                    IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                                {
                                    if (sharedValue.Value.Property.Name == "ContentSource")
                                    {
                                        stack.CurrentFrame.ContentSource =
                                            sharedValue.Value.ValueInternal as String;

                                        // Consider throwing the exception
                                        // ContentSource can only be a String or null.  If it is anything
                                        // else, we should fall back to the default by pretending
                                        // the property was never set.
                                        if (!(sharedValue.Value.ValueInternal is String) &&
                                            sharedValue.Value.ValueInternal != null)
                                        {
                                            stack.CurrentFrame.ContentSourceSet = false;
                                        }
                                    }
                                }
                            }
                            return false;
                        }
                    }
                    break;

                case System.Xaml.XamlNodeType.EndMember:
                    stack.CurrentFrame.Property = null;
                    break;

                case System.Xaml.XamlNodeType.Value:
                    if (!stack.CurrentFrame.IsInStyleOrTemplate)
                    {
                        if (FrameworkTemplate.IsNameProperty(stack.CurrentFrame.Property, stack.CurrentFrame.Type))
                        {
                            string name = xamlReader.Value as String;

                            stack.CurrentFrame.Name = name;
                            stack.CurrentFrame.NameSet = true;

                            if (TemplateLoadData.RootName == null)
                            {
                                TemplateLoadData.RootName = name;
                            }

                            if (!stack.CurrentFrame.IsInNameScope)
                            {
                                // FEs and FCEs need to be added to the name to index map
                                if (typeof(FrameworkElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType) ||
                                    typeof(FrameworkContentElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                                {
                                    TemplateLoadData.NamedTypes.Add(name, stack.CurrentFrame.Type);

                                    // All FEs and FCEs must have a name.  We assign a default if there was no name provided
                                    UpdateSharedPropertyNames(name, sharedProperties, stack.CurrentFrame.Type);
                                }
                            }
                        }

                        if (typeof(ContentPresenter).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType)
                            && stack.CurrentFrame.Property.Name == "ContentSource")
                        {
                            stack.CurrentFrame.ContentSource = xamlReader.Value as String;
                        }
                    }
                    object value = xamlReader.Value;
                    StaticResourceExtension staticResource = value as StaticResourceExtension;
                    // Process the load-time binding of StaticResources.
                    // If this is a simple inline template then the StaticResources will be StaticResourcesExtensions
                    // [They also might be node-lists, see LoadTimeBindUnshareableStaticResource()]
                    // and we do a full search for the location of the value and hold a deferred reference to it.
                    // If the template was in a Resource Dictionary, the RD would have already replaced the
                    // StaticResource with a StaticResourceHolder and we need to do a "live stack only" walk
                    // to look for RD's that might be closer, for better values.
                    if (staticResource != null)
                    {
                        object obj = null;

                        // Inline Template case:
                        if (staticResource.GetType() == typeof(StaticResourceExtension))
                        {
                            obj = staticResource.TryProvideValueInternal(TemplateLoadData.ServiceProviderWrapper, true/*allowDeferredReference*/, true/*mustReturnDeferredResourceReference*/);
                        }

                        // Template in a Resource Dictionary entry case:
                        else if (staticResource.GetType() == typeof(StaticResourceHolder))
                        {
                            obj = staticResource.FindResourceInDeferredContent(TemplateLoadData.ServiceProviderWrapper, true/*allowDeferredReference*/, false/*mustReturnDeferredResourceReference*/);
                            if (obj == DependencyProperty.UnsetValue)
                            {
                                obj = null;  // value is only interesting if it improves the previous value.
                            }
                        }
                        if (obj != null)
                        {
                            var deferredResourceReference = obj as DeferredResourceReference;

                            // newValue is an out parameter that will change the value in the processed node stream.
                            newValue = new StaticResourceHolder(staticResource.ResourceKey, deferredResourceReference);
                        }
                    }
                    break;

                case System.Xaml.XamlNodeType.NamespaceDeclaration:

                    // *** This code assumes that NamespaceDeclarations can only come before [Start|Get]Object. ***
                    // This needs to be updated in the future to support it before properties & Values.

                    if (!stack.CurrentFrame.IsInStyleOrTemplate)
                    {
                        // Check to see if the parent object needs to have the name set
                        if (stack.Depth > 0 && stack.CurrentFrame.NameSet == false && stack.CurrentFrame.Type != null && !stack.CurrentFrame.IsInNameScope)
                        {
                            // FEs and FCEs need to be added to the name to index map
                            if (typeof(FrameworkElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType) ||
                                typeof(FrameworkContentElement).IsAssignableFrom(stack.CurrentFrame.Type.UnderlyingType))
                            {
                                // All FEs and FCEs must have a name.  We assign a default if there was no name provided
                                string name = nameNumber++.ToString(CultureInfo.InvariantCulture) + "_T";
                                UpdateSharedPropertyNames(name, sharedProperties, stack.CurrentFrame.Type);
                                stack.CurrentFrame.Name = name;
                            }
                            stack.CurrentFrame.NameSet = true;
                        }
                    }

                    stack.AddNamespace(xamlReader.Namespace);
                    break;
                case System.Xaml.XamlNodeType.None:
                    break;
            }
            return true;
        }

        private StaticResourceExtension LoadTimeBindUnshareableStaticResource(Xaml.XamlReader xamlReader, XamlObjectWriter writer)
        {
            Debug.Assert(xamlReader.NodeType == Xaml.XamlNodeType.StartObject);
            Debug.Assert(xamlReader.Type.UnderlyingType == typeof(StaticResourceExtension));

            // Loop through the nodes and create the StaticResource.  Since objects could be nested inside the SRE, we need to keep
            // track of the number of start and end objects.
            int elementDepth = 0;
            do
            {
                writer.WriteNode(xamlReader);
                switch (xamlReader.NodeType)
                {
                    case Xaml.XamlNodeType.StartObject:
                    case Xaml.XamlNodeType.GetObject:
                        elementDepth++;
                        break;
                    case Xaml.XamlNodeType.EndObject:
                        elementDepth--;
                        break;
                }
            }
            while (elementDepth > 0 && xamlReader.Read());

            StaticResourceExtension resource = writer.Result as StaticResourceExtension;
            Debug.Assert(resource != null);

            // If the StaicResource was in NodeList form then it would not have been pre-resolved.
            // So do a full walk now, not a Live-Stack only.
            // Resolve the StaticResource value including lookup to the app and the theme
            DeferredResourceReference value = (DeferredResourceReference)resource.TryProvideValueInternal(TemplateLoadData.ServiceProviderWrapper, true/*allowDeferredReference*/, true/*mustReturnDeferredResourceReference*/);

            // Return the value that will be written out the unshareable node list;
            return new StaticResourceHolder(resource.ResourceKey, value);
        }

        // Tries to see if the property can be shared.  Returns true property is shared.
        // Caller is responsible for bookmarking.
        //
        // Only property values can be shared.  We try to share anything that is a Freezable,
        // ME, Style, Template.  However, if the parent type is named, we can't share anything.
        // However, TemplateBindingExtension are ALWAYS shared
        private bool TrySharingProperty(System.Xaml.XamlReader xamlReader,
            XamlType parentType,
            string parentName,
            FrugalObjectList<System.Xaml.NamespaceDeclaration> previousNamespaces,
            out PropertyValue? sharedValue)
        {
            Debug.Assert(xamlReader.NodeType == System.Xaml.XamlNodeType.StartMember);
            // All DependencyPropertys are wrapped in WpfXamlMembers.  If it's not a WpfXamlMember, it's not a DependencyProperty
            WpfXamlMember xamlProperty = xamlReader.Member as WpfXamlMember;
            if (xamlProperty == null)
            {
                sharedValue = null;
                return false;
            }
            DependencyProperty property = xamlProperty.DependencyProperty;

            // We can only share DPs.  Return the node pipe with just the SP in it
            if (property == null)
            {
                sharedValue = null;
                return false;
            }
            // Also, we cannot share the name property
            if (xamlReader.Member == parentType.GetAliasedProperty(XamlLanguage.Name))
            {
                sharedValue = null;
                return false;
            }
            // We can only share properties on FEs and FCEs
            if (!typeof(FrameworkElement).IsAssignableFrom(parentType.UnderlyingType) &&
                 !typeof(FrameworkContentElement).IsAssignableFrom(parentType.UnderlyingType))
            {
                sharedValue = null;
                return false;
            }

            // Check if the value is shareable.
            // We are in a StartMember so we assume there is another node to read.
            xamlReader.Read();

            // Check if the value is shareable.
            if (xamlReader.NodeType == System.Xaml.XamlNodeType.Value)
            {
                // Null would be shareable but when 4.0 shipped a null Value caused
                // an exception to be thrown here.
                //    xamlReader.Value.GetType();
                // Throwning an exceptions will mark item as unshareable.  So for compability,
                // Null is not shareable.
                // But the much more common case of a NullExtension object is sharable.
                if (xamlReader.Value == null)
                {
                    sharedValue = null;
                    return false;
                }
                Type typeofValue = xamlReader.Value.GetType();
                if (!CheckSpecialCasesShareable(typeofValue, property))
                {
                    sharedValue = null;
                    return false;
                }
                if (!(xamlReader.Value is String))
                {
                    return TrySharingValue(property, xamlReader.Value,
                        parentName, xamlReader, true, out sharedValue);
                }
                else
                {
                    object value = xamlReader.Value;

                    TypeConverter converter = null;
                    if (xamlProperty.TypeConverter != null)
                    {
                        converter = xamlProperty.TypeConverter.ConverterInstance;
                    }
                    else if (xamlProperty.Type.TypeConverter != null)
                    {
                        converter = xamlProperty.Type.TypeConverter.ConverterInstance;
                    }

                    if (converter != null)
                    {
                        value = converter.ConvertFrom(TemplateLoadData.ServiceProviderWrapper, CultureInfo.InvariantCulture, value);
                    }

                    return TrySharingValue(property, value, parentName, xamlReader, true, out sharedValue);
                }
            }
            else if (xamlReader.NodeType == System.Xaml.XamlNodeType.StartObject
                || xamlReader.NodeType == System.Xaml.XamlNodeType.NamespaceDeclaration)
            {
                FrugalObjectList<System.Xaml.NamespaceDeclaration> localNamespaces = null;
                if (xamlReader.NodeType == System.Xaml.XamlNodeType.NamespaceDeclaration)
                {
                    localNamespaces = new FrugalObjectList<Xaml.NamespaceDeclaration>();
                    while (xamlReader.NodeType == System.Xaml.XamlNodeType.NamespaceDeclaration)
                    {
                        localNamespaces.Add(xamlReader.Namespace);
                        xamlReader.Read();
                    }
                }

                Debug.Assert(xamlReader.NodeType == System.Xaml.XamlNodeType.StartObject);

                Type typeofValue = xamlReader.Type.UnderlyingType;
                if (!CheckSpecialCasesShareable(typeofValue, property))
                {
                    sharedValue = null;
                    return false;
                }
                if (!IsTypeShareable(xamlReader.Type.UnderlyingType))
                {
                    sharedValue = null;
                    return false;
                }
                else
                {
                    // Keep track of the number of SOs and EOs
                    // Perf note: This stack may be sharable if this gets hit multiple times per TemplateContent.
                    StackOfFrames frames = new StackOfFrames();
                    frames.Push(xamlReader.Type, null);

                    bool insideTemplate = false;
                    bool insideStyle = false;

                    // We don't care about named elements inside of Templates or Styles.  We allow it to just work.
                    if (typeof(FrameworkTemplate).IsAssignableFrom(xamlReader.Type.UnderlyingType))
                    {
                        insideTemplate = true;
                        Stack = frames;
                    }
                    else if (typeof(Style).IsAssignableFrom(xamlReader.Type.UnderlyingType))
                    {
                        insideStyle = true;
                        Stack = frames;
                    }

                    try
                    {

                        //Setup ObjectWriter to be able to create the right values
                        XamlObjectWriter writer = TemplateLoadData.ObjectWriter;
                        writer.Clear();
                        WriteNamespaces(writer, previousNamespaces, localNamespaces);
                        writer.WriteNode(xamlReader);

                        bool done = false;
                        while (!done && xamlReader.Read())
                        {
                            SkipFreeze(xamlReader);
                            writer.WriteNode(xamlReader);
                            switch (xamlReader.NodeType)
                            {
                                case System.Xaml.XamlNodeType.StartObject:
                                    if (typeof(StaticResourceExtension).IsAssignableFrom(xamlReader.Type.UnderlyingType))
                                    {
                                        sharedValue = null;
                                        return false;
                                    }
                                    frames.Push(xamlReader.Type, null);
                                    break;

                                case System.Xaml.XamlNodeType.GetObject:
                                    XamlType type = frames.CurrentFrame.Property.Type;
                                    frames.Push(type, null);
                                    break;

                                case System.Xaml.XamlNodeType.EndObject:
                                    if (frames.Depth == 1)
                                    {
                                        return TrySharingValue(property, writer.Result, parentName,
                                            xamlReader, true, out sharedValue);
                                    }
                                    frames.PopScope();
                                    break;

                                case System.Xaml.XamlNodeType.StartMember:
                                    // Anything that is named cannot be shared
                                    if (!(insideStyle || insideTemplate) && FrameworkTemplate.IsNameProperty(xamlReader.Member, frames.CurrentFrame.Type))
                                    {
                                        done = true;
                                        break;
                                    }
                                    frames.CurrentFrame.Property = xamlReader.Member;
                                    break;

                                case System.Xaml.XamlNodeType.Value:
                                    if (xamlReader.Value != null && typeof(StaticResourceExtension).IsAssignableFrom(xamlReader.Value.GetType()))
                                    {
                                        sharedValue = null;
                                        return false;
                                    }
                                    // We want to wire EventSetters for Styles but not wire
                                    // events inside of a FramewokrTemplate
                                    if (!insideTemplate && frames.CurrentFrame.Property == XamlLanguage.ConnectionId)
                                    {
                                        if (OwnerTemplate.StyleConnector != null)
                                        {
                                            OwnerTemplate.StyleConnector.Connect((int)xamlReader.Value, frames.CurrentFrame.Instance);
                                        }
                                    }
                                    break;
                            }
                        }

                        // We've broken out of the while loop and haven't returned
                        // The property is NOT shareable.
                        sharedValue = null;
                        return false;
                    }
                    finally
                    {
                        Stack = null;
                    }
                }
            }
            else if (xamlReader.NodeType == System.Xaml.XamlNodeType.GetObject)
            {
                sharedValue = null;
                return false;
            }
            else
            {
                // Should never happen
                throw new System.Windows.Markup.XamlParseException(SR.Get(SRID.ParserUnexpectedEndEle));
            }
        }

        private static bool CheckSpecialCasesShareable(Type typeofValue, DependencyProperty property)
        {
            if (typeofValue != typeof(DynamicResourceExtension)
                && typeofValue != typeof(TemplateBindingExtension)
                && typeofValue != typeof(TypeExtension)
                && typeofValue != typeof(StaticExtension))
            {
                // Do not share in the case property type <: IList, Array, IDictionary to maintain compat with v3
                // They wrapped with BamlCollectionHolder in these 3 cases so the value isn't shared.
                if (typeof(IList).IsAssignableFrom(property.PropertyType))
                {
                    return false;
                }
                if (property.PropertyType.IsArray)
                {
                    return false;
                }
                if (typeof(IDictionary).IsAssignableFrom(property.PropertyType))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsFreezableDirective(System.Xaml.XamlReader reader)
        {
            System.Xaml.XamlNodeType nodeType = reader.NodeType;
            if (nodeType == System.Xaml.XamlNodeType.StartMember)
            {
                System.Xaml.XamlMember member = reader.Member;
                return (member.IsUnknown && member.IsDirective && member.Name == "Freeze");
            }
            return false;
        }

        private static void SkipFreeze(System.Xaml.XamlReader reader)
        {
            if (IsFreezableDirective(reader))
            {
                reader.Read();  // V
                reader.Read();  // EM
                reader.Read();  // Next
            }
        }

        private bool TrySharingValue(DependencyProperty property,
                                     object value,
                                     string parentName,
                                     System.Xaml.XamlReader xamlReader,
                                     bool allowRecursive,
                                     out PropertyValue? sharedValue)
        {
            sharedValue = null;

            // Null is sharable.
            if (value != null && !IsTypeShareable(value.GetType()))
            {
                return false;
            }

            bool isValueShareable = true;

            if (value is Freezable)
            {
                // Check if it's a freezable and we can freeze it
                Freezable freezable = value as Freezable;
                if (freezable != null)
                {
                    if (freezable.CanFreeze)
                    {
                        freezable.Freeze();
                    }
                    else
                    {
                        // Object is not freezable, which means its
                        // not shareable.
                        isValueShareable = false;
                    }
                }
            }
            else if (value is CollectionViewSource)
            {
                CollectionViewSource viewSource = value as CollectionViewSource;
                if (viewSource != null)
                {
                    isValueShareable = viewSource.IsShareableInTemplate();
                }
            }
            else if (value is MarkupExtension)
            {
                // Share the actual ME directly for these 3.
                if (value is BindingBase ||
                    value is TemplateBindingExtension ||
                    value is DynamicResourceExtension)
                {
                    isValueShareable = true;
                }
                else if ((value is StaticResourceExtension) || (value is StaticResourceHolder))
                {
                    isValueShareable = false;
                }
                else
                {
                    TemplateLoadData.ServiceProviderWrapper.SetData(_sharedDpInstance, property);
                    value = (value as MarkupExtension).ProvideValue(TemplateLoadData.ServiceProviderWrapper);
                    TemplateLoadData.ServiceProviderWrapper.Clear();

                    if (allowRecursive)
                    {
                        // Terminate recursive checking of provided value to prevent infinite loop
                        return TrySharingValue(property, value, parentName, xamlReader, false, out sharedValue);
                    }
                    else
                    {
                        isValueShareable = true;
                    }
                }
            }

            if (isValueShareable)
            {
                // If we're here, that means the property can be shared
                PropertyValue propertyValue = new PropertyValue();
                propertyValue.Property = property;
                propertyValue.ChildName = parentName;
                propertyValue.ValueInternal = value;
                propertyValue.ValueType = PropertyValueType.Set;

                sharedValue = propertyValue;

                // Read the EndProperty so it's not going to be read by the outer reader
                xamlReader.Read();
                Debug.Assert(xamlReader.NodeType == System.Xaml.XamlNodeType.EndMember);
            }

            return isValueShareable;
        }

        private bool IsTypeShareable(Type type)
        {
            if ( // We handle Freezables on an per-instance basis.
                typeof(Freezable).IsAssignableFrom(type)
                ||
                // Well-known immutable CLR types
                type == typeof(string) || type == typeof(Uri) || type == typeof(Type)
                ||
                // We assume MEs are shareable; the host object is responsible
                // for ensuring immutability.  The exception is static resource
                // references, for which we have special support in templates.
                (typeof(MarkupExtension).IsAssignableFrom(type)
                    &&
                    !typeof(StaticResourceExtension).IsAssignableFrom(type))
                ||
                // Styles & Templates are mostly shareable
                typeof(Style).IsAssignableFrom(type)
                ||
                typeof(FrameworkTemplate).IsAssignableFrom(type)
                ||
                // CVS might be shareable, wait for the instance check
                typeof(System.Windows.Data.CollectionViewSource).IsAssignableFrom(type)
                ||
                // Value types are immutable by nature
                (type != null && type.IsValueType))
                return true;

            return false;
        }

        private void WriteNamespaces(System.Xaml.XamlWriter writer,
                                     FrugalObjectList<System.Xaml.NamespaceDeclaration> previousNamespaces,
                                     FrugalObjectList<System.Xaml.NamespaceDeclaration> localNamespaces)
        {
            if (previousNamespaces != null)
            {
                for (int idx = 0; idx < previousNamespaces.Count; idx++)
                {
                    writer.WriteNamespace(previousNamespaces[idx]);
                }
            }
            if (localNamespaces != null)
            {
                for (int idx = 0; idx < localNamespaces.Count; idx++)
                {
                    writer.WriteNamespace(localNamespaces[idx]);
                }
            }
        }


        private static void AutoAliasContentPresenter(
            Type targetType,
            string contentSource,
            string templateChildName,
            ref FrugalStructList<ChildRecord> childRecordFromChildIndex,
            ref FrugalStructList<ItemStructMap<TriggerSourceRecord>> triggerSourceRecordFromChildIndex,
            ref FrugalStructList<ChildPropertyDependent> resourceDependents,
            ref HybridDictionary dataTriggerRecordFromBinding,
            ref bool hasInstanceValues,
            HybridDictionary childIndexFromChildName,
            bool isContentPropertyDefined,
            bool isContentSourceSet,
            bool isContentTemplatePropertyDefined,
            bool isContentTemplateSelectorPropertyDefined,
            bool isContentStringFormatPropertyDefined
            )
        {
            if (String.IsNullOrEmpty(contentSource) && isContentSourceSet == false)
                contentSource = "Content";

            if (!String.IsNullOrEmpty(contentSource) && !isContentPropertyDefined)
            {
                Debug.Assert(templateChildName != null);

                DependencyProperty dpContent = DependencyProperty.FromName(contentSource, targetType);
                DependencyProperty dpContentTemplate = DependencyProperty.FromName(contentSource + "Template", targetType);
                DependencyProperty dpContentTemplateSelector = DependencyProperty.FromName(contentSource + "TemplateSelector", targetType);
                DependencyProperty dpContentStringFormat = DependencyProperty.FromName(contentSource + "StringFormat", targetType);

                if (dpContent == null && isContentSourceSet)
                {
                    throw new InvalidOperationException(SR.Get(SRID.MissingContentSource, contentSource, targetType));
                }

                if (dpContent != null)
                {
                    PropertyValue pv = new PropertyValue();
                    pv.ValueType = PropertyValueType.TemplateBinding;
                    pv.ChildName = templateChildName;
                    pv.ValueInternal = new TemplateBindingExtension(dpContent);
                    pv.Property = ContentPresenter.ContentProperty;

                    StyleHelper.UpdateTables(ref pv, ref childRecordFromChildIndex,
                        ref triggerSourceRecordFromChildIndex,
                        ref resourceDependents,
                        ref dataTriggerRecordFromBinding,
                        childIndexFromChildName,
                        ref hasInstanceValues);

                }

                if (!isContentTemplatePropertyDefined &&
                    !isContentTemplateSelectorPropertyDefined &&
                    !isContentStringFormatPropertyDefined)
                {
                    if (dpContentTemplate != null)
                    {
                        PropertyValue pv = new PropertyValue();
                        pv.ValueType = PropertyValueType.TemplateBinding;
                        pv.ChildName = templateChildName;
                        pv.ValueInternal = new TemplateBindingExtension(dpContentTemplate);
                        pv.Property = ContentPresenter.ContentTemplateProperty;

                        StyleHelper.UpdateTables(ref pv, ref childRecordFromChildIndex,
                               ref triggerSourceRecordFromChildIndex,
                               ref resourceDependents,
                               ref dataTriggerRecordFromBinding,
                               childIndexFromChildName,
                               ref hasInstanceValues);
                    }

                    if (dpContentTemplateSelector != null)
                    {
                        PropertyValue pv = new PropertyValue();
                        pv.ValueType = PropertyValueType.TemplateBinding;
                        pv.ChildName = templateChildName;
                        pv.ValueInternal = new TemplateBindingExtension(dpContentTemplateSelector);
                        pv.Property = ContentPresenter.ContentTemplateSelectorProperty;

                        StyleHelper.UpdateTables(ref pv, ref childRecordFromChildIndex,
                               ref triggerSourceRecordFromChildIndex,
                               ref resourceDependents,
                               ref dataTriggerRecordFromBinding,
                               childIndexFromChildName,
                               ref hasInstanceValues);
                    }

                    if (dpContentStringFormat != null)
                    {
                        PropertyValue pv = new PropertyValue();
                        pv.ValueType = PropertyValueType.TemplateBinding;
                        pv.ChildName = templateChildName;
                        pv.ValueInternal = new TemplateBindingExtension(dpContentStringFormat);
                        pv.Property = ContentPresenter.ContentStringFormatProperty;


                        StyleHelper.UpdateTables(ref pv, ref childRecordFromChildIndex,
                             ref triggerSourceRecordFromChildIndex,
                             ref resourceDependents,
                             ref dataTriggerRecordFromBinding,
                             childIndexFromChildName,
                             ref hasInstanceValues);
                    }
                }
            }
        }


        private static void AutoAliasGridViewRowPresenter(
                Type targetType,
                string contentSource,
                string childName,
                ref FrugalStructList<ChildRecord> childRecordFromChildIndex,
                ref FrugalStructList<ItemStructMap<TriggerSourceRecord>> triggerSourceRecordFromChildIndex,
                ref FrugalStructList<ChildPropertyDependent> resourceDependents,
                ref HybridDictionary dataTriggerRecordFromBinding,
                ref bool hasInstanceValues,
                HybridDictionary childIndexFromChildID,
                bool isContentPropertyDefined,
                bool isColumnsPropertyDefined
                )
        {
            // <GridViewRowPresenter Content="{TemplateBinding Property=Content}" .../>
            if (!isContentPropertyDefined)
            {
                DependencyProperty dpContent = DependencyProperty.FromName("Content", targetType);

                if (dpContent != null)
                {
                    PropertyValue propertyValue = new PropertyValue();
                    propertyValue.ValueType = PropertyValueType.TemplateBinding;
                    propertyValue.ChildName = childName;
                    propertyValue.ValueInternal = new TemplateBindingExtension(dpContent);
                    propertyValue.Property = GridViewRowPresenter.ContentProperty;

                    StyleHelper.UpdateTables(ref propertyValue,
                                             ref childRecordFromChildIndex,
                                             ref triggerSourceRecordFromChildIndex,
                                             ref resourceDependents,
                                             ref dataTriggerRecordFromBinding,
                                             childIndexFromChildID,
                                             ref hasInstanceValues);
                }
            }

            // <GridViewRowPresenter Columns="{TemplateBinding Property=GridView.ColumnCollection}" .../>
            if (!isColumnsPropertyDefined)
            {
                PropertyValue propertyValue = new PropertyValue();
                propertyValue.ValueType = PropertyValueType.TemplateBinding;
                propertyValue.ChildName = childName;
                propertyValue.ValueInternal = new TemplateBindingExtension(GridView.ColumnCollectionProperty);
                propertyValue.Property = GridViewRowPresenter.ColumnsProperty;

                StyleHelper.UpdateTables(ref propertyValue,
                                         ref childRecordFromChildIndex,
                                         ref triggerSourceRecordFromChildIndex,
                                         ref resourceDependents,
                                         ref dataTriggerRecordFromBinding,
                                         childIndexFromChildID,
                                         ref hasInstanceValues);
            }
        }

        internal XamlType RootType { get; private set; }

        internal XamlType GetTypeForName(string name)
        {
            return TemplateLoadData.NamedTypes[name];
        }

        internal FrameworkTemplate OwnerTemplate { get; set; }
        internal IXamlObjectWriterFactory ObjectWriterFactory { get; private set; }
        internal XamlObjectWriterSettings ObjectWriterParentSettings { get; private set; }

        internal XamlSchemaContext SchemaContext { get; private set; }

        //_xamlNodeList is the postProcessed list, not the original template nodes.  TemplateContentConverter
        // and TemplateContent do that processing.
        internal XamlNodeList _xamlNodeList = null;

        private static SharedDp _sharedDpInstance = new SharedDp(null, null, null);
        private StackOfFrames Stack
        {
            get
            {
                return TemplateLoadData.Stack;
            }
            set
            {
                TemplateLoadData.Stack = value;
            }
        }

        //This will be nulled out when the FrameworkTemplate is sealed.
        internal TemplateLoadData TemplateLoadData
        {
            get;
            set;
        }

        internal class ServiceProviderWrapper : ITypeDescriptorContext, IXamlTypeResolver, IXamlNamespaceResolver, IProvideValueTarget
        {
            private IServiceProvider _services;
            internal StackOfFrames Frames { get; set; }
            private XamlSchemaContext _schemaContext;
            private Object _targetObject;
            private Object _targetProperty;

            public ServiceProviderWrapper(IServiceProvider services, XamlSchemaContext schemaContext)
            {
                _services = services;
                _schemaContext = schemaContext;
            }
            #region IServiceProvider Members

            object IServiceProvider.GetService(Type serviceType)
            {
                if (serviceType == typeof(IXamlTypeResolver))
                {
                    return this;
                }
                else if (serviceType == typeof(IProvideValueTarget))
                {
                    return this;
                }
                else
                {
                    return _services.GetService(serviceType);
                }
            }

            #endregion

            #region IXamlTypeResolver Members

            Type IXamlTypeResolver.Resolve(string qualifiedTypeName)
            {
                return _schemaContext.GetXamlType(XamlTypeName.Parse(qualifiedTypeName, this)).UnderlyingType;
            }

            #endregion

            #region IXamlNamespaceResolver Members

            string IXamlNamespaceResolver.GetNamespace(string prefix)
            {
                FrugalObjectList<NamespaceDeclaration> namespaces = Frames.InScopeNamespaces;
                if (namespaces != null)
                {
                    for (int idx = 0; idx < namespaces.Count; idx++)
                    {
                        if (namespaces[idx].Prefix == prefix)
                        {
                            return namespaces[idx].Namespace;
                        }
                    }
                }

                return ((IXamlNamespaceResolver)_services.GetService(typeof(IXamlNamespaceResolver))).GetNamespace(prefix);
            }

            IEnumerable<NamespaceDeclaration> IXamlNamespaceResolver.GetNamespacePrefixes()
            {
                throw new NotImplementedException();
            }

            #endregion

            #region ITypeDescriptorContext Members

            IContainer ITypeDescriptorContext.Container
            {
                get { return null; }
            }

            object ITypeDescriptorContext.Instance
            {
                get { return null; }
            }

            void ITypeDescriptorContext.OnComponentChanged()
            {
            }

            bool ITypeDescriptorContext.OnComponentChanging()
            {
                return false;
            }

            PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
            {
                get { return null; }
            }

            #endregion

            public void SetData(Object targetObject, Object targetProperty)
            {
                _targetObject = targetObject;
                _targetProperty = targetProperty;
            }
            public void Clear()
            {
                _targetObject = null;
                _targetProperty = null;
            }
            Object IProvideValueTarget.TargetObject { get { return _targetObject; } }
            Object IProvideValueTarget.TargetProperty { get { return _targetProperty; } }
        }
    }

    //This class is meant to hold any data that a TemplateContent needs during TemplateLoad
    // that isn't needed by a FrameworkTemplate during TemplateApply.
    internal class TemplateLoadData
    {
        internal TemplateLoadData()
        {
        }

        internal TemplateContent.StackOfFrames Stack { get; set; }

        internal Dictionary<string, XamlType> _namedTypes;
        internal Dictionary<string, XamlType> NamedTypes
        {
            get
            {
                if (_namedTypes == null)
                    _namedTypes = new Dictionary<string, XamlType>();
                return _namedTypes;
            }
        }

        internal System.Xaml.XamlReader Reader
        {
            get;
            set;
        }

        internal string RootName { get; set; }
        internal object RootObject { get; set; }
        internal TemplateContent.ServiceProviderWrapper ServiceProviderWrapper { get; set; }
        internal XamlObjectWriter ObjectWriter { get; set; }
    }
}
