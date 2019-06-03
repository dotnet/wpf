// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Xaml;
using System.Xaml.Schema;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Concurrent;

namespace System.Windows.Baml2006
{
    // XamlType for New types defined in the BAML stream.
    //
    internal class WpfXamlType : XamlType
    {
        [Flags]
        private enum BoolTypeBits
        {
            BamlScenerio = 0x0001,
            V3Rules     = 0x0002
        }

        const int ConcurrencyLevel = 1;
        // ConcurrentDictionary's capacity should not be divisible by a small prime.
        // ConcurrentDictionary grows by doing 2 * capacity + 1 and finding the first that isn't 
        //   divisible by 2,3,5,7.  Anything less than 11 would be inefficient in growing
        const int Capacity = 11;

        // In the "reading from BAML" senario we don't sperate Attachable from non-attachable.
        // BAML is pre-approved by the compiler so we don't have to worry about incorrect usage
        // BAML has higher performance with only one cache.  One less dictionary lookup.
        // But text can ask for <Brush StackPanel.Background="Red">  and property looks like
        // an attachable property and the look-up needs to fail. [in text].
        private ConcurrentDictionary<string, XamlMember> _attachableMembers;
        private ConcurrentDictionary<string, XamlMember> _members;

        // We share this with WpfKnownType.  Any changes to the bitfield needs to be propogated to WpfKnownType
        protected byte _bitField;
        private bool IsBamlScenario
        {
            get { return GetFlag(ref _bitField, (byte)BoolTypeBits.BamlScenerio); }
            set { SetFlag(ref _bitField, (byte)BoolTypeBits.BamlScenerio, value); }
        }
        private bool UseV3Rules
        {
            get { return GetFlag(ref _bitField, (byte)BoolTypeBits.V3Rules); }
            set { SetFlag(ref _bitField, (byte)BoolTypeBits.V3Rules, value); }
        }

        protected ConcurrentDictionary<string, XamlMember> Members
        {
            get
            {
                if (_members == null)
                {
                    _members = new ConcurrentDictionary<string, XamlMember>(ConcurrencyLevel, Capacity);
                }
                return _members;
            }
        }

        protected ConcurrentDictionary<string, XamlMember> AttachableMembers
        {
            get
            {
                if (_attachableMembers == null)
                {
                    _attachableMembers = new ConcurrentDictionary<string, XamlMember>(ConcurrencyLevel, Capacity);
                }
                return _attachableMembers;
            }
        }

        public WpfXamlType(Type type, XamlSchemaContext schema, bool isBamlScenario, bool useV3Rules)
            : base(type, schema)
        {
            IsBamlScenario = isBamlScenario;
            UseV3Rules = useV3Rules;
        }

        protected override XamlMember LookupContentProperty()
        {
            XamlMember result = base.LookupContentProperty();
            WpfXamlMember wpfMember = result as WpfXamlMember;
            if (wpfMember != null)
            {
                result = wpfMember.AsContentProperty;
            }
            return result;
        }

        protected override bool LookupIsNameScope()
        {
            if (UnderlyingType == typeof(ResourceDictionary))
            {
                return false;
            }
            else if (typeof(ResourceDictionary).IsAssignableFrom(UnderlyingType))
            {
                InterfaceMapping map = UnderlyingType.GetInterfaceMap(typeof(System.Windows.Markup.INameScope));
                foreach (MethodInfo method in map.TargetMethods)
                {
                    if (method.Name.Contains("RegisterName"))
                    {
                        return method.DeclaringType != typeof(ResourceDictionary);
                    }
                }
                return false;
            }
            else
            {
                return base.LookupIsNameScope();
            }
        }

        private XamlMember FindMember(string name, bool isAttached, bool skipReadOnlyCheck)
        {
            // Try looking for a known member
            XamlMember member = FindKnownMember(name, isAttached);

            // Return the known member if we have one
            if (member != null)
            {
                return member;
            }

            // Look for members backed by DPs
            member = FindDependencyPropertyBackedProperty(name, isAttached, skipReadOnlyCheck);
            if (member != null)
            {
                return member;
            }

            // Look for members backed by RoutedEvents
            member = FindRoutedEventBackedProperty(name, isAttached, skipReadOnlyCheck);
            if (member != null)
            {
                return member;
            }

            // Ask the base class (XamlType) to find the member
            // We make this call in case a user overrides or news a member
            if (isAttached)
            {
                member = base.LookupAttachableMember(name);
            }
            else
            {
                member = base.LookupMember(name, skipReadOnlyCheck);
            }

            // If the base class finds one and it's declared type is a known type,
            // try looking for a known property.
            WpfKnownType wpfKnownType;
            if (member != null && (wpfKnownType = member.DeclaringType as WpfKnownType) != null)
            {
                XamlMember knownMember = FindKnownMember(wpfKnownType, name, isAttached);
                if (knownMember != null)
                {
                    return knownMember;
                }
            }
            return member;
        }

        protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
        {
            return FindMember(name, false /* isAttached */, skipReadOnlyCheck);
        }

        protected override XamlMember LookupAttachableMember(string name)
        {
            return FindMember(name, true /* isAttached */, false /* skipReadOnlyCheck doens't matter for Attached */);
        }

        protected override IEnumerable<XamlMember> LookupAllMembers()
        {
            List<XamlMember> members = new List<XamlMember>();
            var reflectedMembers = base.LookupAllMembers();

            foreach (var reflectedMember in reflectedMembers)
            {
                var member = reflectedMember;
                if (!(member is WpfXamlMember))
                {
                    member = GetMember(member.Name);
                }
                members.Add(member);
            }

            return members;
        }

        // This will first check the cache on the type
        // Then will look for a knownMember if the type is a WpfKnownType
        // Then we look a DependencyProperty for the member
        //      If we do find one, double check to see if there is a known member that matches the DP
        //          This only happens on non-known types that derive from known types
        // Otherwise, return null
        private XamlMember FindKnownMember(string name, bool isAttachable)
        {
            XamlType type = this;
            // Only support looking up KnownMembers on KnownTypes (otherwise we could miss a new/override member)
            if (this is WpfKnownType)
            {
                do
                {
                    WpfXamlType wpfXamlType = type as WpfXamlType;
                    XamlMember xamlMember = FindKnownMember(wpfXamlType, name, isAttachable);
                    if (xamlMember != null)
                    {
                        return xamlMember;
                    }

                    type = type.BaseType;
                }
                while (type != null);
            }
            return null;
        }

        private XamlMember FindRoutedEventBackedProperty(string name, bool isAttachable, bool skipReadOnlyCheck)
        {
            RoutedEvent re = EventManager.GetRoutedEventFromName(
                name, UnderlyingType);
            XamlMember xamlMember = null;
            if (re != null)
            {
                // Try looking for a known member first instead of creating a new WpfXamlMember
                WpfXamlType wpfXamlType = null;
                if (IsBamlScenario)
                {
                    wpfXamlType = System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(re.OwnerType) as WpfXamlType;
                }
                else
                {
                    wpfXamlType = System.Windows.Markup.XamlReader.GetWpfSchemaContext().GetXamlType(re.OwnerType) as WpfXamlType;
                }

                if (wpfXamlType != null)
                {
                    xamlMember = FindKnownMember(wpfXamlType, name, isAttachable);
                }

                if (IsBamlScenario)
                {
                    xamlMember = new WpfXamlMember(re, isAttachable);
                }
                else
                {
                    if (isAttachable)
                    {
                        xamlMember = GetAttachedRoutedEvent(name, re);
                        if (xamlMember == null)
                        {
                            xamlMember = GetRoutedEvent(name, re, skipReadOnlyCheck);
                        }

                        if (xamlMember == null)
                        {
                            xamlMember = new WpfXamlMember(re, true);
                        }
                    }
                    else
                    {
                        xamlMember = GetRoutedEvent(name, re, skipReadOnlyCheck);
                        if (xamlMember == null)
                        {
                            xamlMember = GetAttachedRoutedEvent(name, re);
                        }

                        if (xamlMember == null)
                        {
                            xamlMember = new WpfXamlMember(re, false);
                        }
                    }
                }
                if (Members.TryAdd(name, xamlMember))
                {
                    return xamlMember;
                }
                else
                {
                    return Members[name];
                }
            }
            return xamlMember;
        }

        private XamlMember FindDependencyPropertyBackedProperty(string name, bool isAttachable, bool skipReadOnlyCheck)
        {
            XamlMember xamlMember = null;

            // If it's a dependency property, return a wrapping XamlMember
            DependencyProperty property;
            if ((property = DependencyProperty.FromName(name, this.UnderlyingType)) != null)
            {
                // Try looking for a known member first instead of creating a new WpfXamlMember
                WpfXamlType wpfXamlType = null;
                if (IsBamlScenario)
                {
                    wpfXamlType = System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(property.OwnerType) as WpfXamlType;
                }
                else
                {
                    wpfXamlType = System.Windows.Markup.XamlReader.GetWpfSchemaContext().GetXamlType(property.OwnerType) as WpfXamlType;
                }

                if (wpfXamlType != null)
                {
                    xamlMember = FindKnownMember(wpfXamlType, name, isAttachable);
                }

                if (xamlMember == null)
                {
                    if (IsBamlScenario)
                    {
                        // In Baml Scenarios, we don't want to lookup the MemberInfo since we always know the 
                        // type converter and we don't allows DeferringLoader (since the MarkupCompiler doesn't support it)
                        xamlMember = new WpfXamlMember(property, isAttachable);
                    }
                    else
                    {
                        // Try to find the MemberInfo so we can use that directly.  There's no direct way to do this
                        // with XamlType so we'll just get the XamlMember and get the underlying member
                        if (isAttachable)
                        {
                            xamlMember = GetAttachedDependencyProperty(name, property);
                            if (xamlMember == null)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            xamlMember = GetRegularDependencyProperty(name, property, skipReadOnlyCheck);
                            if (xamlMember == null)
                            {
                                xamlMember = GetAttachedDependencyProperty(name, property);
                            }

                            if (xamlMember == null)
                            {
                                xamlMember = new WpfXamlMember(property, false);
                            }
                        }
                    }
                    return CacheAndReturnXamlMember(xamlMember);
                }
            }
            return xamlMember;
        }

        private XamlMember CacheAndReturnXamlMember(XamlMember xamlMember)
        {
            // If you are read from BAML then store everything in Members.
            if (!xamlMember.IsAttachable || IsBamlScenario)
            {
                if (Members.TryAdd(xamlMember.Name, xamlMember))
                {
                    return xamlMember;
                }
                else
                {
                    return Members[xamlMember.Name];
                }
            }
            else
            {
                if (AttachableMembers.TryAdd(xamlMember.Name, xamlMember))
                {
                    return xamlMember;
                }
                else
                {
                    return AttachableMembers[xamlMember.Name];
                }
            }
        }

        private XamlMember GetAttachedRoutedEvent(string name, RoutedEvent re)
        {
            XamlMember memberFromBase = base.LookupAttachableMember(name);
            if (memberFromBase != null)
            {
                return new WpfXamlMember(re, (MethodInfo)memberFromBase.UnderlyingMember, SchemaContext, UseV3Rules);
            }
            return null;
        }

        private XamlMember GetRoutedEvent(string name, RoutedEvent re, bool skipReadOnlyCheck)
        {
            XamlMember memberFromBase = base.LookupMember(name, skipReadOnlyCheck);
            if (memberFromBase != null)
            {
                return new WpfXamlMember(re, (EventInfo)memberFromBase.UnderlyingMember, SchemaContext, UseV3Rules);
            }
            return null;
        }

        private XamlMember GetAttachedDependencyProperty(string name, DependencyProperty property)
        {
            XamlMember memberFromBase = base.LookupAttachableMember(name);
            if (memberFromBase != null)
            {
                return new WpfXamlMember(property,
                    memberFromBase.Invoker.UnderlyingGetter,
                    memberFromBase.Invoker.UnderlyingSetter,
                    SchemaContext, UseV3Rules);
            }
            return null;
        }

        private XamlMember GetRegularDependencyProperty(string name, DependencyProperty property, bool skipReadOnlyCheck)
        {
            XamlMember memberFromBase = base.LookupMember(name, skipReadOnlyCheck);
            if (memberFromBase != null)
            {
                PropertyInfo propertyInfo = memberFromBase.UnderlyingMember as PropertyInfo;
                if (propertyInfo != null)
                {
                    return new WpfXamlMember(property, propertyInfo, SchemaContext, UseV3Rules);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return null;
        }

        // First try looking at the cache
        // Then look to see if we have a known property on the type
        private static XamlMember FindKnownMember(WpfXamlType wpfXamlType, string name, bool isAttachable)
        {
            XamlMember xamlMember = null;

            // Look in the cache first
            if (!isAttachable || wpfXamlType.IsBamlScenario)
            {
                if (wpfXamlType._members != null && wpfXamlType.Members.TryGetValue(name, out xamlMember))
                {
                    return xamlMember;
                }
            }
            else
            {
                if (wpfXamlType._attachableMembers != null && wpfXamlType.AttachableMembers.TryGetValue(name, out xamlMember))
                {
                    return xamlMember;
                }
            }

            WpfKnownType knownType = wpfXamlType as WpfKnownType;
            // Only look for known properties on a known type
            if (knownType != null)
            {
                // if it is a Baml Senario BAML doesn't really care if it was attachable or not
                // so look for the property in AttachableMembers also if it wasn't found in Members.
                if (!isAttachable || wpfXamlType.IsBamlScenario)
                {
                    xamlMember = System.Windows.Markup.XamlReader.BamlSharedSchemaContext.CreateKnownMember(wpfXamlType.Name, name);
                }
                if (isAttachable || (xamlMember == null && wpfXamlType.IsBamlScenario))
                {
                    xamlMember = System.Windows.Markup.XamlReader.BamlSharedSchemaContext.CreateKnownAttachableMember(wpfXamlType.Name, name);
                }

                if (xamlMember != null)
                {
                    return knownType.CacheAndReturnXamlMember(xamlMember);
                }
            }
            return null;
        }

        protected override XamlCollectionKind LookupCollectionKind()
        {
            if (UseV3Rules)
            {
                if (UnderlyingType.IsArray)
                {
                    return XamlCollectionKind.Array;
                }
                if (typeof(IDictionary).IsAssignableFrom(UnderlyingType))
                {
                    return XamlCollectionKind.Dictionary;
                }
                if (typeof(IList).IsAssignableFrom(UnderlyingType))
                {
                    return XamlCollectionKind.Collection;
                }
                // Several types in V3 implemented IAddChildInternal which allowed them to be collections
                if (typeof(System.Windows.Documents.DocumentReferenceCollection).IsAssignableFrom(UnderlyingType) || 
                    typeof(System.Windows.Documents.PageContentCollection).IsAssignableFrom(UnderlyingType))
                {
                    return XamlCollectionKind.Collection;
                } 
                // Doing a type comparison against XmlNamespaceMappingCollection will load System.Xml. We get around
                // this by only doing the comparison if it's an ICollection<XmlNamespaceMapping>
                if (typeof(ICollection<System.Windows.Data.XmlNamespaceMapping>).IsAssignableFrom(UnderlyingType) 
                    && IsXmlNamespaceMappingCollection)
                {
                    return XamlCollectionKind.Collection;
                }
                return XamlCollectionKind.None;
            }
            else
            {
                return base.LookupCollectionKind();
            }
        }

        // Having this directly in LookupCollectionKind forces System.Xml to load.
        private bool IsXmlNamespaceMappingCollection
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get
            {
                return typeof(System.Windows.Data.XmlNamespaceMappingCollection).IsAssignableFrom(UnderlyingType);
            }
        }
        internal XamlMember FindBaseXamlMember(string name, bool isAttachable)
        {
            if (isAttachable)
            {
                return base.LookupAttachableMember(name);
            }
            else
            {
                return base.LookupMember(name, true);
            }            
        }

        internal static bool GetFlag(ref byte bitField, byte typeBit)
        {
            return (bitField & typeBit) != 0;
        }

        internal static void SetFlag(ref byte bitField, byte typeBit, bool value)
        {
            if (value)
            {
                bitField = (byte)(bitField | typeBit);
            }
            else
            {
                bitField = (byte)(bitField & ~typeBit);
            }
        }
    }
}
