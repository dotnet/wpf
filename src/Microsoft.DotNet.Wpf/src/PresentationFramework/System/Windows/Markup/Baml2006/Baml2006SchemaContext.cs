// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Xaml;
using System.Xaml.Schema;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Runtime.Serialization;
using KnownTypesV3 = System.Windows.Markup.KnownTypes;
using System.Threading;
using MS.Internal.WindowsBase;

namespace System.Windows.Baml2006
{
    internal partial class Baml2006SchemaContext : XamlSchemaContext
    {
        public Baml2006SchemaContext(Assembly localAssembly):
            this(localAssembly, System.Windows.Markup.XamlReader.BamlSharedSchemaContext)
        {
        }

        internal Baml2006SchemaContext(Assembly localAssembly, XamlSchemaContext parentSchemaContext)
            : base(Array.Empty<Assembly>())
        {
            _localAssembly = localAssembly;
            _parentSchemaContext = parentSchemaContext;
        }

        #region XamlSchemaContext Overrides

        public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
        {
            return _parentSchemaContext.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace);
        }

        public override XamlDirective GetXamlDirective(string xamlNamespace, string name)
        {
            return _parentSchemaContext.GetXamlDirective(xamlNamespace, name);
        }

        public override IEnumerable<string> GetAllXamlNamespaces()
        {
            return _parentSchemaContext.GetAllXamlNamespaces();
        }

        public override ICollection<XamlType> GetAllXamlTypes(string xamlNamespace)
        {
            return _parentSchemaContext.GetAllXamlTypes(xamlNamespace);
        }

        public override string GetPreferredPrefix(string xmlns)
        {
            return _parentSchemaContext.GetPreferredPrefix(xmlns);
        }

        public override XamlType GetXamlType(Type type)
        {
            return _parentSchemaContext.GetXamlType(type);
        }

        protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            EnsureXmlnsAssembliesLoaded(xamlNamespace);
            XamlTypeName fullTypeName = new XamlTypeName { Namespace = xamlNamespace, Name = name };
            if (typeArguments != null)
            {
                foreach (XamlType typeArg in typeArguments)
                {
                    fullTypeName.TypeArguments.Add(new XamlTypeName(typeArg));
                }
            }
            return _parentSchemaContext.GetXamlType(fullTypeName);
        }

        #endregion

        #region Internal Properties

        internal XamlMember StaticExtensionMemberTypeProperty { get { return _xStaticMemberProperty.Value; } }

        internal XamlMember TypeExtensionTypeProperty { get { return _xTypeTypeProperty.Value; } }

        internal XamlMember ResourceDictionaryDeferredContentProperty { get { return _resourceDictionaryDefContentProperty.Value; } }

        internal XamlType ResourceDictionaryType { get { return _resourceDictionaryType.Value; } }

        internal XamlType EventSetterType { get { return _eventSetterType.Value; } }

        internal XamlMember EventSetterEventProperty { get { return _eventSetterEventProperty.Value; } }

        internal XamlMember EventSetterHandlerProperty { get { return _eventSetterHandlerProperty.Value; } }

        internal XamlMember FrameworkTemplateTemplateProperty { get { return _frameworkTemplateTemplateProperty.Value; } }

        internal XamlType StaticResourceExtensionType { get { return _staticResourceExtensionType.Value; } }

        internal Assembly LocalAssembly { get { return _localAssembly; } }

        internal Baml2006ReaderSettings Settings { get; set; }
        internal const Int16 StaticExtensionTypeId = 602;
        internal const Int16 StaticResourceTypeId = 603;
        internal const Int16 DynamicResourceTypeId = 189;
        internal const Int16 TemplateBindingTypeId = 634;
        internal const Int16 TypeExtensionTypeId = 691;
        internal const string WpfNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

#endregion

        #region Internal Methods
        internal void Reset()
        {
            lock (_syncObject)
            {
                _bamlAssembly.Clear();
                _bamlType.Clear();
                _bamlProperty.Clear();
                _bamlString.Clear();
                _bamlXmlnsMappings.Clear();
            }
        }


        internal Assembly GetAssembly(Int16 assemblyId)
        {
            BamlAssembly bamlAssembly;

            if (TryGetBamlAssembly(assemblyId, out bamlAssembly))
            {
                return ResolveAssembly(bamlAssembly);
            }

            throw new KeyNotFoundException();
        }

        internal String GetAssemblyName(Int16 assemblyId)
        {
            BamlAssembly bamlAssembly;

            if (TryGetBamlAssembly(assemblyId, out bamlAssembly))
            {
                return bamlAssembly.Name;
            }

            throw new KeyNotFoundException(SR.Get(SRID.BamlAssemblyIdNotFound, assemblyId.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS)));
        }

        internal Type GetClrType(Int16 typeId)
        {
            BamlType bamlType;
            XamlType xamlType;

            if (TryGetBamlType(typeId, out bamlType, out xamlType))
            {
                if (xamlType != null)
                {
                    return xamlType.UnderlyingType;
                }
                return ResolveBamlTypeToType(bamlType);
            }

            throw new KeyNotFoundException(SR.Get(SRID.BamlTypeIdNotFound, typeId.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS)));
        }

        internal XamlType GetXamlType(Int16 typeId)
        {
            BamlType bamlType;
            XamlType xamlType;

            if (TryGetBamlType(typeId, out bamlType, out xamlType))
            {
                if (xamlType != null)
                {
                    return xamlType;
                }
                return ResolveBamlType(bamlType, typeId);
            }

            throw new KeyNotFoundException(SR.Get(SRID.BamlTypeIdNotFound, typeId.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS)));
        }

        internal DependencyProperty GetDependencyProperty(Int16 propertyId)
        {
            XamlMember member = GetProperty(propertyId, false);
            WpfXamlMember wpfMember = member as WpfXamlMember;
            if (wpfMember != null)
            {
                Debug.Assert(wpfMember.DependencyProperty != null);
                return wpfMember.DependencyProperty;
            }

            throw new KeyNotFoundException();
        }

        internal XamlMember GetProperty(Int16 propertyId, XamlType parentType)
        {
            BamlProperty bamlProperty;
            XamlMember xamlMember;

            if (TryGetBamlProperty(propertyId, out bamlProperty, out xamlMember))
            {
                if (xamlMember != null)
                {
                    return xamlMember;
                }
                XamlType declaringType = GetXamlType(bamlProperty.DeclaringTypeId);

                // If the parent type & declaring type are assignable, then it could
                // be either a regular property or attachable.
                if (parentType.CanAssignTo(declaringType))
                {
                    xamlMember = declaringType.GetMember(bamlProperty.Name);
                    if (xamlMember == null)
                    {
                        xamlMember = declaringType.GetAttachableMember(bamlProperty.Name);
                    }
                }
                else
                {
                    //If not assignable, it has to be an attachable member
                    xamlMember = declaringType.GetAttachableMember(bamlProperty.Name);
                }
                lock (_syncObject)
                {
                    _bamlProperty[propertyId] = xamlMember;
                }
                return xamlMember;
            }

            throw new KeyNotFoundException();
        }


        internal XamlMember GetProperty(Int16 propertyId, bool isAttached)
        {
            BamlProperty bamlProperty;
            XamlMember xamlMember;

            if (TryGetBamlProperty(propertyId, out bamlProperty, out xamlMember))
            {
                XamlType declaringType;
                if (xamlMember != null)
                {
                    // If we ask for the Attachable member and we cached the non-Attachable
                    // member, look for the correct member
                    if (xamlMember.IsAttachable != isAttached)
                    {
                        declaringType = xamlMember.DeclaringType;
                        if (isAttached)
                        {
                            xamlMember = declaringType.GetAttachableMember(xamlMember.Name);
                        }
                        else
                        {
                            xamlMember = declaringType.GetMember(xamlMember.Name);
                        }
                    }
                    return xamlMember;
                }
                declaringType = GetXamlType(bamlProperty.DeclaringTypeId);
                if (isAttached)
                {
                    xamlMember = declaringType.GetAttachableMember(bamlProperty.Name);
                }
                else
                {
                    xamlMember = declaringType.GetMember(bamlProperty.Name);
                }
                lock (_syncObject)
                {
                    _bamlProperty[propertyId] = xamlMember;
                }
                return xamlMember;
            }

            throw new KeyNotFoundException();
        }

        internal XamlType GetPropertyDeclaringType(Int16 propertyId)
        {
            BamlProperty bamlProperty;
            XamlMember xamlMember;

            // IsAttachable doesn't matter since we're only looking for the DeclaringType
            if (TryGetBamlProperty(propertyId, out bamlProperty, out xamlMember))
            {
                if (xamlMember != null)
                {
                    return xamlMember.DeclaringType;
                }
                return GetXamlType(bamlProperty.DeclaringTypeId);
            }

            throw new KeyNotFoundException();
        }

        internal String GetPropertyName(Int16 propertyId, bool fullName)
        {
            BamlProperty bamlProperty = null;
            XamlMember xamlMember;

            // IsAttachable doesn't matter since we're only looking for the name
            if (TryGetBamlProperty(propertyId, out bamlProperty, out xamlMember))
            {
                if (xamlMember != null)
                {
                    return xamlMember.Name;
                }
                return bamlProperty.Name;
            }

            throw new KeyNotFoundException();
        }

        internal string GetString(Int16 stringId)
        {
            string result;
            lock (_syncObject)
            {
                if (stringId >= 0 && stringId < _bamlString.Count)
                {
                    result = _bamlString[stringId];
                }
                else
                {
                    result = KnownTypes.GetKnownString(stringId);
                }
            }

            if (result == null)
            {
                throw new KeyNotFoundException();
            }

            return result;
        }

        internal void AddAssembly(Int16 assemblyId, string assemblyName)
        {
            if (assemblyId < 0)
            {
                throw new ArgumentOutOfRangeException("assemblyId");
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }

            lock (_bamlAssembly)
            {
                if (assemblyId == _bamlAssembly.Count)
                {
                    BamlAssembly assembly = new BamlAssembly(assemblyName);
                    _bamlAssembly.Add(assembly);
                }
                else if (assemblyId > _bamlAssembly.Count)
                {
                    throw new ArgumentOutOfRangeException("assemblyId", SR.Get(SRID.AssemblyIdOutOfSequence, assemblyId));
                }
            }
            // Duplicate IDs (assemblyId < _bamlAssembly.Count) are ignored
        }

        internal void AddXamlType(Int16 typeId, Int16 assemblyId, string typeName, TypeInfoFlags flags)
        {
            if (typeId < 0)
            {
                throw new ArgumentOutOfRangeException("typeId");
            }

            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }

            lock (_syncObject)
            {
                if (typeId == _bamlType.Count)
                {
                    BamlType type = new BamlType(assemblyId, typeName);
                    type.Flags = flags;
                    _bamlType.Add(type);
                }
                else if (typeId > _bamlType.Count)
                {
                    throw new ArgumentOutOfRangeException("typeId", SR.Get(SRID.TypeIdOutOfSequence, typeId));
                }
            }
            // Duplicate IDs (typeID < _bamlType.Count) are ignored
        }

        internal void AddProperty(Int16 propertyId, Int16 declaringTypeId, string propertyName)
        {
            if (propertyId < 0)
            {
                throw new ArgumentOutOfRangeException("propertyId");
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            lock (_syncObject)
            {
                if (propertyId == _bamlProperty.Count)
                {
                    BamlProperty property = new BamlProperty(declaringTypeId, propertyName);
                    _bamlProperty.Add(property);
                }
                else if (propertyId > _bamlProperty.Count)
                {
                    throw new ArgumentOutOfRangeException("propertyId", SR.Get(SRID.PropertyIdOutOfSequence, propertyId));
                }
            }
            // Duplicate IDs (propertyId < _bamlProperty.Count) are ignored
        }

        internal void AddString(Int16 stringId, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            lock (_syncObject)
            {
                if (stringId == _bamlString.Count)
                {
                    _bamlString.Add(value);
                }
                else if (stringId > _bamlString.Count)
                {
                    throw new ArgumentOutOfRangeException("stringId", SR.Get(SRID.StringIdOutOfSequence, stringId));
                }
            }
            // Duplicate IDs (stringId < _bamlString.Count) are ignored
        }

        internal void AddXmlnsMapping(string xmlns, short[] assemblies)
        {
            lock (_syncObject)
            {
                // If there are duplicate records for the same namespace, we overwrite
                _bamlXmlnsMappings[xmlns] = assemblies;
            }
        }

        #endregion

        #region Private Methods

        private void EnsureXmlnsAssembliesLoaded(string xamlNamespace)
        {
            short[] assemblies;
            lock (_syncObject)
            {
                _bamlXmlnsMappings.TryGetValue(xamlNamespace, out assemblies);
            }
            if (assemblies != null)
            {
                foreach (short assembly in assemblies)
                {
                    // Ensure that the assembly is loaded
                    GetAssembly(assembly);
                }
            }
        }

        private Assembly ResolveAssembly(BamlAssembly bamlAssembly)
        {
            if (bamlAssembly.Assembly != null)
            {
                return bamlAssembly.Assembly;
            }

            AssemblyName assemblyName = new AssemblyName(bamlAssembly.Name);
            bamlAssembly.Assembly = MS.Internal.WindowsBase.SafeSecurityHelper.GetLoadedAssembly(assemblyName);
            if (bamlAssembly.Assembly == null)
            {
                byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
                if (assemblyName.Version != null || assemblyName.CultureInfo != null || publicKeyToken != null)
                {
                    try
                    {
                        bamlAssembly.Assembly = Assembly.Load(assemblyName.FullName);
                    }
                    catch
                    {
                        // Fall back to short name match.
                        if (bamlAssembly.Assembly == null)
                        {
                            // First try to match the local assembly (which may be in the LoadFrom/LoadFile context)
                            if (MatchesLocalAssembly(assemblyName.Name, publicKeyToken))
                            {
                                bamlAssembly.Assembly = _localAssembly;
                            }
                            // Otherwise try Assembly.Load
                            else
                            {
                                AssemblyName shortName = new AssemblyName(assemblyName.Name);
                                if (publicKeyToken != null)
                                {
                                    shortName.SetPublicKeyToken(publicKeyToken);
                                }
                                bamlAssembly.Assembly = Assembly.Load(shortName);
                            }
                        }
                    }
                }
                else
                {
                    // Only a short name was provided.
                    // Don't need to check for local assembly match, because if it matched the local
                    // assembly, we would have caught it in GetLoadedAssembly up above.
                    bamlAssembly.Assembly = Assembly.LoadWithPartialName(assemblyName.Name);
                }
            }
            return bamlAssembly.Assembly;
        }

        private bool MatchesLocalAssembly(string shortName, byte[] publicKeyToken)
        {
            if (_localAssembly == null)
            {
                return false;
            }
            AssemblyName localAssemblyName = new AssemblyName(_localAssembly.FullName);
            if (shortName != localAssemblyName.Name)
            {
                return false;
            }
            if (publicKeyToken == null)
            {
                return true;
            }
            return MS.Internal.PresentationFramework.SafeSecurityHelper.IsSameKeyToken(
                publicKeyToken, localAssemblyName.GetPublicKeyToken());
        }

        private Type ResolveBamlTypeToType(BamlType bamlType)
        {
                BamlAssembly bamlAssembly;
                if (TryGetBamlAssembly(bamlType.AssemblyId, out bamlAssembly))
                {
                    Assembly assembly = ResolveAssembly(bamlAssembly);
                    if (assembly != null)
                    {
                        return assembly.GetType(bamlType.Name, false);
                    }
                }

            return null;
        }

        private XamlType ResolveBamlType(BamlType bamlType, Int16 typeId)
        {
            Type type = ResolveBamlTypeToType(bamlType);
            if (type != null)
            {
                bamlType.ClrNamespace = type.Namespace;
                XamlType xType = _parentSchemaContext.GetXamlType(type);
                lock (_syncObject)
                {
                    _bamlType[typeId] = xType;
                }
                return xType;
            }

            // NOTE: If XamlSchemaContext to can provide an UnknownType of name bamlType.Name
            // return a new UnknownType instead of throwing NotImplemented. 
            // return bamlType.XamlType = new UnknownType(new XamlTypeName(bamlType.Name), this, null);
            throw new NotImplementedException();
        }

        private bool TryGetBamlAssembly(Int16 assemblyId, out BamlAssembly bamlAssembly)
        {
            lock (_syncObject)
            {
                if (assemblyId >= 0 && assemblyId < _bamlAssembly.Count)
                {
                    bamlAssembly = _bamlAssembly[assemblyId];
                    return true;
                }
            }

            Assembly assembly = KnownTypes.GetKnownAssembly(assemblyId);
            if (assembly != null)
            {
                bamlAssembly = new BamlAssembly(assembly);
                return true;
            }

            bamlAssembly = null;
            return false;
        }

        private bool TryGetBamlType(Int16 typeId, out BamlType bamlType, out XamlType xamlType)
        {
            bamlType = null;
            xamlType = null;
            lock (_syncObject)
            {
                if (typeId >= 0 && typeId < _bamlType.Count)
                {
                    object type = _bamlType[typeId];
                    bamlType = type as BamlType;
                    xamlType = type as XamlType;
                    return (type != null);
                }
            }
            if (typeId < 0)
            {
                if (_parentSchemaContext == System.Windows.Markup.XamlReader.BamlSharedSchemaContext)
                {
                    xamlType = System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetKnownBamlType(typeId);
                }
                else
                {
                    xamlType = _parentSchemaContext.GetXamlType(KnownTypes.GetKnownType(typeId));
                }

                return true;
            }

            return (bamlType != null);
        }

        private bool TryGetBamlProperty(Int16 propertyId, out BamlProperty bamlProperty, out XamlMember xamlMember)
        {
            lock (_syncObject)
            {
                if (propertyId >= 0 && propertyId < _bamlProperty.Count)
                {
                    Object property = _bamlProperty[propertyId];
                    xamlMember = property as XamlMember;
                    bamlProperty = property as BamlProperty;
                    return true;
                }
            }

            if (propertyId < 0)
            {
                if (_parentSchemaContext == System.Windows.Markup.XamlReader.BamlSharedSchemaContext)
                {
                    xamlMember = System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetKnownBamlMember(propertyId);
                }
                else
                {
                    Int16 typeId;
                    string propertyName;
                    KnownTypes.GetKnownProperty(propertyId, out typeId, out propertyName);
                    xamlMember = GetXamlType(typeId).GetMember(propertyName);
                }
                bamlProperty = null;
                return true;
            }

            xamlMember = null;
            bamlProperty = null;
            return false;
        }

        #endregion

        #region Private Data

        // Assignments to these collections are idempotent, thus threadsafe
        private readonly List<BamlAssembly> _bamlAssembly = new List<BamlAssembly>();
        private readonly List<object> _bamlType = new List<object>();
        private readonly List<Object> _bamlProperty = new List<Object>();
        private readonly List<string> _bamlString = new List<string>();
        private readonly Dictionary<string, short[]> _bamlXmlnsMappings = new Dictionary<string, short[]>();

        private static readonly Lazy<XamlMember> _xStaticMemberProperty
            = new Lazy<XamlMember>(() => XamlLanguage.Static.GetMember("MemberType"));

        private static readonly Lazy<XamlMember> _xTypeTypeProperty
            = new Lazy<XamlMember>(() => XamlLanguage.Static.GetMember("Type"));

        private static readonly Lazy<XamlMember> _resourceDictionaryDefContentProperty
            = new Lazy<XamlMember>(() => _resourceDictionaryType.Value.GetMember("DeferrableContent"));

        private static readonly Lazy<XamlType> _resourceDictionaryType
            = new Lazy<XamlType>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(ResourceDictionary)));

        private static readonly Lazy<XamlType> _eventSetterType
            = new Lazy<XamlType>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(EventSetter)));

        private static readonly Lazy<XamlMember> _eventSetterEventProperty
            = new Lazy<XamlMember>(() => _eventSetterType.Value.GetMember("Event"));

        private static readonly Lazy<XamlMember> _eventSetterHandlerProperty
            = new Lazy<XamlMember>(() => _eventSetterType.Value.GetMember("Handler"));

        private static readonly Lazy<XamlMember> _frameworkTemplateTemplateProperty
            = new Lazy<XamlMember>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(FrameworkTemplate)).GetMember("Template"));

        private static readonly Lazy<XamlType> _staticResourceExtensionType
            = new Lazy<XamlType>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(StaticResourceExtension)));

        private object _syncObject = new object();

        private Assembly _localAssembly;

        private XamlSchemaContext _parentSchemaContext;

        #endregion

        #region Private Types and Enums

        sealed class BamlAssembly
        {
            /// <summary>
            /// </summary>
            /// <param name="name">A fully qualified assembly name</param>
            public BamlAssembly(string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                Name = name;
                Assembly = null;
            }

            public BamlAssembly(Assembly assembly)
            {
                if (assembly == null)
                {
                    throw new ArgumentNullException("assembly");
                }

                Name = null;
                Assembly = assembly;
            }

            // Information needed to resolve a BamlAssembly to a CLR Assembly
            public readonly string Name;

            // Resolved BamlAssembly information
            internal Assembly Assembly;
        }

        sealed class BamlType
        {
            public BamlType(Int16 assemblyId, string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                AssemblyId = assemblyId;
                Name = name;
            }

            // Information needed to resolve a BamlType to a XamlType
            public Int16 AssemblyId;
            public string Name;
            public TypeInfoFlags Flags;

            // Resolved BamlType information
            public string ClrNamespace;
        }

        sealed class BamlProperty
        {
            public BamlProperty(Int16 declaringTypeId, string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                DeclaringTypeId = declaringTypeId;
                Name = name;
            }

            // Information needed to resolve a BamlProperty to a XamlMember
            public readonly Int16 DeclaringTypeId;
            public readonly string Name;
        }

        #endregion

        [Flags]
        internal enum TypeInfoFlags : byte
        {
            Internal             = 0x1,
            UnusedTwo            = 0x2,
            UnusedThree          = 0x4,
        }
    }
}
