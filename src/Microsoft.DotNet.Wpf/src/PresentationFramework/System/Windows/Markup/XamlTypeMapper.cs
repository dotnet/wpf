// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//  Maps namespaceURI and LocalName to appropriate element, properties, and events.
//

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using MS.Utility;

#if !PBTCOMPILER

using System.Windows;
using System.Windows.Markup;
using System.Windows.Resources;
using System.Windows.Threading;
using SecurityHelper=MS.Internal.PresentationFramework.SecurityHelper;
using MS.Internal;  // CriticalExceptions

#else

using System.Runtime.CompilerServices;

#endif

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    ///<summary>
    /// Handles mapping between XML NamepaceURI and .NET namespace types
    ///</summary>
#if PBTCOMPILER
    internal class XamlTypeMapper
#else
    public partial class XamlTypeMapper
#endif
    {
#region Public

#region Methods

#if !PBTCOMPILER
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assemblyNames">Assemblies XamlTypeMapper should use when resolving XAML</param>
        public XamlTypeMapper(string[] assemblyNames)
        {
            if(null == assemblyNames)
            {
                throw new ArgumentNullException( "assemblyNames" );
            }

            _assemblyNames = assemblyNames;
            _namespaceMaps = null;
        }
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assemblyNames">Assemblies XamlTypeMapper should use when resolving XAML</param>
        /// <param name="namespaceMaps">NamespaceMap the XamlTypeMapper should use when resolving XAML</param>
        public XamlTypeMapper(
            string[] assemblyNames,
            NamespaceMapEntry[] namespaceMaps)
        {
            if(null == assemblyNames)
            {
                throw new ArgumentNullException( "assemblyNames" );
            }

            _assemblyNames = assemblyNames;
            _namespaceMaps = namespaceMaps;
#if PBTCOMPILER
            _hasInternals = false;
            _hasLocalReference = false;
#endif
        }

        /// <summary>
        /// Helper to map an Xaml tag to a DotNet Type
        /// </summary>
        /// <remarks>
        /// Example:<para/>
        ///     If the xml contained the tags <base:Button xmlns:base="AvalonBase"/>
        ///     you would call XamlTypeMapper.GetType("AvalonBase","Button");
        ///     <para/>
        ///     Note the XmlNamespace "AvalonBase" is the actual namespace value, not
        ///     the base: prefix.
        /// </remarks>
        /// <param name="xmlNamespace">NamespaceURI of tag</param>
        /// <param name="localName">localName of the Tag</param>
        /// <returns>Type for the object. If no type was found NULL is returned</returns>
        public Type GetType(
            string xmlNamespace,
            string localName)
        {
            if(null == xmlNamespace)
            {
                throw new ArgumentNullException( "xmlNamespace" );
            }
            if(null == localName)
            {
                throw new ArgumentNullException( "localName" );
            }

            TypeAndSerializer typeAndSerializer =
                GetTypeOnly(xmlNamespace,localName);

            return typeAndSerializer != null ? typeAndSerializer.ObjectType : null;
        }

#if !PBTCOMPILER
        /// <summary>
        ///  Programmatic counterpart to the <?Mapping ... ?> XAML PI.  For example, <para/>
        ///    <?Mapping XmlNamespace="swc" ClrNamespace="System.Windows.ComponentModel" Assembly="PresentationFramework" ?>
        /// </summary>
        /// <param name="xmlNamespace">
        /// The "swc" argument in the mapping PI example.
        /// </param>
        /// <param name="clrNamespace">
        /// The "System.Windows.ComponentModel" argument in the mapping PI example.
        /// </param>
        /// <param name="assemblyName">
        /// The "PresentationFramework" argument in the mapping PI example.
        /// </param>
        public void AddMappingProcessingInstruction(
            string  xmlNamespace,
            string  clrNamespace,
            string  assemblyName )
        {
            if( null == xmlNamespace )
            {
                throw new ArgumentNullException("xmlNamespace");
            }
            if( null == clrNamespace )
            {
                throw new ArgumentNullException("clrNamespace");
            }
            if( null == assemblyName )
            {
                throw new ArgumentNullException("assemblyName");
            }

            // Parameter validation : Check for String.Empty as well?

            // Add mapping to the table keyed by xmlNamespace
            ClrNamespaceAssemblyPair pair = new ClrNamespaceAssemblyPair(clrNamespace, assemblyName);
            PITable[xmlNamespace] = pair;

            // Add mapping to the table keyed by assembly and clrnamespace
            string upperAssemblyName = assemblyName.ToUpper(
                                              TypeConverterHelper.InvariantEnglishUS);
            String fullName = clrNamespace + "#" + upperAssemblyName;

            _piReverseTable[fullName] = xmlNamespace;

            // Add mapping to the SchemaContext
            if (_schemaContext != null)
            {
                _schemaContext.SetMappingProcessingInstruction(xmlNamespace, pair);
            }
        }
#endif
        /// <summary>
        ///     This allows specifying a path to use when loading the named assembly.
        /// </summary>
        /// <param name="assemblyName">
        /// The short name of the assembly, with no extension or path specified
        /// </param>
        /// <param name="assemblyPath">
        /// The file path of the assembly
        /// </param>
        public void SetAssemblyPath(
            string assemblyName,
            string assemblyPath)
        {
            if( null == assemblyName )
            {
                throw new ArgumentNullException("assemblyName");
            }
            if( null == assemblyPath )
            {
                throw new ArgumentNullException("assemblyPath");
            }
            if (assemblyPath == string.Empty)
            {
                _lineNumber = 0;  // Public API, so we don't know the line number.
                ThrowException(SRID.ParserBadAssemblyPath);
            }
            if (assemblyName == string.Empty)
            {
                _lineNumber = 0;  // Public API, so we don't know the line number.
                ThrowException(SRID.ParserBadAssemblyName);
            }

            string asmName = assemblyName.ToUpper(CultureInfo.InvariantCulture);
            lock (_assemblyPathTable)
            {
                _assemblyPathTable[asmName] = assemblyPath;
            }

#if PBTCOMPILER
            PreLoadDefaultAssemblies(asmName, assemblyPath);
#else
            // Allow people to reset the path of previously loaded assemblies
            // so they can be loaded again.   The is the Dev build/load/build/load
            // Designer scenario.  (Don't mess with GACed assemblies)
            Assembly assem = ReflectionHelper.GetAlreadyLoadedAssembly(asmName);
            if (assem != null && !assem.GlobalAssemblyCache)
            {
                ReflectionHelper.ResetCacheForAssembly(asmName);
                // No way to reset SchemaContext at assembly granularity, so just reset the whole context
                if (_schemaContext != null)
                {
                    _schemaContext = null;
                }
            }
#endif
        }

#endregion Methods

#region Properties

        /// <summary>
        ///  Instance of XamlTypeMapper to use if none is specified in a
        ///  ParserContext.  XamlTypeMapper returned is the internal default.
        /// </summary>
        public static XamlTypeMapper DefaultMapper
        {
            get
            {
                return XmlParserDefaults.DefaultMapper;
            }
        }

#endregion Properties

#endregion Public

#region Internal

#region Initialization

#if !PBTCOMPILER
        ///<summary>
        /// Initialize the XamlTypeMapper so that it is ready for a parse operation.
        ///</summary>
        internal void Initialize()
        {
            _typeLookupFromXmlHashtable.Clear();
            _namespaceMapHashList.Clear();
            _piTable.Clear();
            _piReverseTable.Clear();
            lock (_assemblyPathTable)
            {
                _assemblyPathTable.Clear();
            }
            _referenceAssembliesLoaded = false;
        }
#endif

        // Return a new XamlTypeMapper that has the same instance variables as this instance,
        // will all complex properties deep copied.
#if !PBTCOMPILER
        internal XamlTypeMapper Clone()
        {
            XamlTypeMapper newMapper = new XamlTypeMapper(_assemblyNames.Clone() as string[]);

            newMapper._mapTable = _mapTable;
            newMapper._referenceAssembliesLoaded = _referenceAssembliesLoaded;
            newMapper._lineNumber = _lineNumber;
            newMapper._linePosition = _linePosition;

            newMapper._namespaceMaps = _namespaceMaps.Clone() as NamespaceMapEntry[];
            newMapper._typeLookupFromXmlHashtable = _typeLookupFromXmlHashtable.Clone() as Hashtable;
            newMapper._namespaceMapHashList = _namespaceMapHashList.Clone() as Hashtable;
            newMapper._typeInformationCache = CloneHybridDictionary(_typeInformationCache);
            newMapper._piTable = CloneHybridDictionary(_piTable);
            newMapper._piReverseTable = CloneStringDictionary(_piReverseTable);
            newMapper._assemblyPathTable = CloneHybridDictionary(_assemblyPathTable);

            return newMapper;
        }
#endif

#if !PBTCOMPILER
        private HybridDictionary CloneHybridDictionary(HybridDictionary dict)
        {
            HybridDictionary newDict = new HybridDictionary(dict.Count);
            foreach ( DictionaryEntry de in dict )
            {
                newDict.Add(de.Key, de.Value);
            }
            return newDict;
        }
#endif

#if !PBTCOMPILER
        private Dictionary<string, string> CloneStringDictionary(Dictionary<string, string> dict)
        {
            Dictionary<string, string> newDict = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in dict)
            {
                newDict.Add(kvp.Key, kvp.Value);
            }
            return newDict;
        }
#endif

#endregion Initialization

#region Assemblies

        ///<summary>
        /// Returns the assembly path for the passed assembly.  If none, return null.
        ///</summary>
        internal string AssemblyPathFor(string assemblyName)
        {
            string path = null;
            if (assemblyName != null)
            {
                // This method is used by SchemaContext, which needs to be thread-safe, so lock around it
                lock (_assemblyPathTable)
                {
                    path = _assemblyPathTable[assemblyName.ToUpper(
                                            CultureInfo.InvariantCulture)] as string;
                }
            }

#if PBTCOMPILER

            if (path == null)
            {
                // If the assembly name contains full assembly name, we should use the short
                // assembly name to search the assembly path cache table.

                int indexComma = assemblyName.IndexOf(",", StringComparison.Ordinal);

                if (indexComma > 0)
                {
                    string assemblyShortName = assemblyName.Substring(0, indexComma).ToUpper(CultureInfo.InvariantCulture);
                    path = _assemblyPathTable[assemblyShortName] as String;
                }
            }
#endif

            return path;
        }

        /// <summary>
        /// Load assemblies that are in the referenced assembly list passed to the XamlTypeMapper
        /// by the compiler.  Don't load known assemblies that should already be present,
        /// since the references may not be the correct versions.
        /// </summary>
        private bool LoadReferenceAssemblies()
        {
            if (!_referenceAssembliesLoaded)
            {
                _referenceAssembliesLoaded = true;
                foreach (DictionaryEntry entry in _assemblyPathTable)
                {
                    ReflectionHelper.LoadAssembly(entry.Key as String, entry.Value as String);
                }
                return true;
            }
            else
            {
                // Already loaded, so they don't need to be loaded again
                return false;
            }
        }

#endregion Assemblies

#region AssemblyLoading

#if  PBTCOMPILER

        private void PreLoadDefaultAssemblies(string asmName, string asmPath)
        {          
            if (AssemblyWB == null && string.Compare(asmName, _assemblyNames[0], StringComparison.OrdinalIgnoreCase) == 0)
            {
                AssemblyWB = ReflectionHelper.LoadAssembly(asmName, asmPath);
            }
            else if (AssemblyPC == null && string.Compare(asmName, _assemblyNames[1], StringComparison.OrdinalIgnoreCase) == 0)
            {
                AssemblyPC = ReflectionHelper.LoadAssembly(asmName, asmPath);
            }
            else if (AssemblyPF == null && string.Compare(asmName, _assemblyNames[2], StringComparison.OrdinalIgnoreCase) == 0)
            {
                AssemblyPF = ReflectionHelper.LoadAssembly(asmName, asmPath);
            }
            else if (string.Compare(asmName, "SYSTEM.XML", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // make sure System.Xml is at least loaded as ReflectionOnly
                ReflectionHelper.LoadAssembly(asmName, asmPath);
            }
            else if (string.Compare(asmName, "SYSTEM", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // make sure System is at least loaded as ReflectionOnly
                ReflectionHelper.LoadAssembly(asmName, asmPath);
            }
        }
#endif

#endregion AssemblyLoading

        #region Events

#if !PBTCOMPILER

        /// <summary>
        /// Helper to map an Attribute to a RoutedEvent
        /// </summary>
        /// <remarks>
        ///    Example: <para/>
        ///     If the xaml contained the tag <base:Button base:Click="MyClick" xmlns:base="BaseXmlNs"/>
        ///     you would call <para/>
        ///
        ///         Type owner = XamlTypeMapper.GetType("BaseXmlNs","Button"); <para/>
        ///         RoutedEvent = XamlTypeMapper.GetRoutedEvent(owner,"MyClick","BaseXmlNs");
        /// </remarks>
        /// <param name="owner">Type of the owner</param>
        /// <param name="xmlNamespace">Xml NamespaceURI of the attribute</param>
        /// <param name="localName">Local name of the attribute</param>
        /// <returns>The RoutedEvent ID or null if no match was found</returns>
        /// <ExternalAPI/>
        internal RoutedEvent GetRoutedEvent(
            Type   owner,
            string xmlNamespace,
            string localName)
        {
            Type baseType = null;
            string dynamicObjectName = null;

            if(null == localName)
            {
                throw new ArgumentNullException( "localName" );
            }
            if(null == xmlNamespace)
            {
                throw new ArgumentNullException( "xmlNamespace" );
            }
            if (owner != null && !ReflectionHelper.IsPublicType(owner))
            {
                _lineNumber = 0;  // Public API, so we don't know the line number.
                ThrowException(SRID.ParserOwnerEventMustBePublic, owner.FullName );
            }

            RoutedEvent Event = GetDependencyObject(true,owner,xmlNamespace,
                localName,ref baseType,ref dynamicObjectName)
                as RoutedEvent;

            return Event;
        }

#endif

#endregion Events

#region Properties

#if !PBTCOMPILER
        ///<summary>
        /// Converts the string representation of an Attribute Value to an appropriate
        /// Type for the Property.  This handles use of type converters and the special
        /// *prefix:Type.Field syntax for enums, static properties and fields.
        /// </summary>
        ///<param name="targetObject">Target object that the property needs to be set on</param>
        ///<param name="propType">Type of the property</param>
        ///<param name="propName">Name of the property.  This is used only for
        ///                       error reporting and some pre-validation</param>
        ///<param name="dpOrPiOrFi">DependencyProperty or PropertyInfo or FieldInfo. This is used
        ///         for evaluating the TypeConverter to be used for conversion</param>
        ///<param name="typeContext">Context for the type converter</param>
        ///<param name="parserContext">Context for enum, field and property resolution</param>
        ///<param name="value">string value of the property the Attribute</param>
        ///<param name="converterTypeId">typeId of converter to use for paring the attribute value</param>
        ///<returns>
        /// An Object for the attribute value is returned.
        /// Null is returned if no TypeConverter for the Property type.
        ///</returns>
        internal Object ParseProperty(
            object                 targetObject,
            Type                   propType,
            string                 propName,
            object                 dpOrPiOrFi,
            ITypeDescriptorContext typeContext,
            ParserContext          parserContext,
            string                 value,
            short                  converterTypeId)
        {
            _lineNumber = parserContext != null ? parserContext.LineNumber : 0;
            _linePosition = parserContext != null ? parserContext.LinePosition : 0;

            // If value is to be converted to a string, just return the string itself instead of
            // going needlessly through the TC. But check that the target prop Type can accept strings.
            if (converterTypeId < 0 && ((short)-converterTypeId == (short)KnownElements.StringConverter))
            {
                if (propType == typeof(object) || propType == typeof(string))
                {
                    return value;
                }
                else
                {
                    string message = SR.Get(SRID.ParserCannotConvertPropertyValueString, value, propName, propType.FullName);
                    XamlParseException.ThrowException(parserContext, _lineNumber, _linePosition, message, null);
                }
            }

            Object obj = null;   // Object to return
            TypeConverter typeConvert;

            if (converterTypeId != 0)
            {
                typeConvert = parserContext.MapTable.GetConverterFromId(converterTypeId, propType, parserContext);
            }
            else
            {
                // NOTE: This may still be a known converter. This is typically the case when adding
                // a text Record. This should also be potentially optimized by resolving & writing out the
                // TC at compile time.

                // Reflect for per property type converter or type converter based on the property's type
                typeConvert = GetPropertyConverter(propType, dpOrPiOrFi);

                #if DEBUG
                if( propType.Assembly.FullName == "PresentationFramework"
                    ||
                    propType.Assembly.FullName == "PresentationCore"
                    ||
                    propType.Assembly.FullName == "WindowsBase" )
                {
                    Debug.WriteLine( "Reflected for type converter on " + propType.Name + "." + propName );
                }
                #endif
            }

#if !STRESS
            try
            {
#endif
                obj =  typeConvert.ConvertFromString(typeContext, TypeConverterHelper.InvariantEnglishUS, value);

                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.TraceActivityItem( TraceMarkup.TypeConvert,
                                                 typeConvert,
                                                 value,
                                                 obj );
                }
#if !STRESS
            }
            catch (Exception e)
            {
                if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                {
                    throw;
                }

                // If the targetObject can provide a fallback value for this property then use that instead

                IProvidePropertyFallback iProvidePropertyFallback = targetObject as IProvidePropertyFallback;
                if (iProvidePropertyFallback != null && iProvidePropertyFallback.CanProvidePropertyFallback(propName))
                {
                    obj = iProvidePropertyFallback.ProvidePropertyFallback(propName, e);

                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.TraceActivityItem( TraceMarkup.TypeConvertFallback,
                                                     typeConvert,
                                                     value,
                                                     obj );
                    }
                }

                // If we got the default object TypeConverter, then we know the conversion will
                // fail, so create a more meaningful error message here.
                else if (typeConvert.GetType() == typeof(TypeConverter))
                {
                    string message;
                    if( propName != string.Empty )
                    {
                        // <SomeElement SomeProp="SomeText"/> and there's no TypeConverter
                        //  to handle converting "SomeText" into an instance of something
                        //  that can be set into SomeProp.
                        message = SR.Get(SRID.ParserDefaultConverterProperty, propType.FullName, propName, value);
                    }
                    else
                    {
                        // <SomeElement>SomeText</SomeElement> and there's no TypeConverter
                        //  associated with the type SomeElement
                        message = SR.Get(SRID.ParserDefaultConverterElement, propType.FullName, value);
                    }
                    XamlParseException.ThrowException(parserContext, _lineNumber, _linePosition, message, null);
                }
                else
                {
                    string message = TypeConverterFailure( value, propName, propType.FullName );
                    XamlParseException.ThrowException(parserContext, _lineNumber, _linePosition, message, e);
                }
            }
#endif

            // Verify that the type converter actually gave us an instance of the correct object type.
            if( obj != null )
            {
                if(!propType.IsAssignableFrom(obj.GetType()))
                {
                    string message = TypeConverterFailure( value, propName, propType.FullName );

                    XamlParseException.ThrowException(parserContext, _lineNumber, _linePosition, message, null);
                }
            }

            return obj;
        }

        private string TypeConverterFailure( string value, string propName, string propType )
        {
            string message;

            if( propName != string.Empty )
            {
                // We were called to do type conversion on a string that's been
                //  assigned in an element attribute, but failed for whatever reason.
                //
                //   <Rectangle Fill="Red"/>
                //
                // propName is 'Fill' in this case.

                message = SR.Get(SRID.ParserCannotConvertPropertyValueString, value, propName, propType);
            }
            else
            {
                // We are being called by BamlRecordReader::GetObjectFromString
                //  which is not trying to convert a property. It's actually
                //  trying to get an element out of this.
                //
                //   <SolidColorBrush>Red</SolidColorBrush>
                //
                // There is no associated propName available in this case, so we
                //  give a different error message.

                message = SR.Get(SRID.ParserCannotConvertInitializationText, value, propType );
            }
            return message;
        }
#endif

        // ValidateNames does Name validation, and ValidateEnums does enum
        // name validation.  Note that both must be called to determine if a
        // property is valid before it is set, but the order is not important.  Hence
        // ValidateNames can be called before writing out a BAML record, and
        // ValidateEnums can be called later after the BAML record has been read.


        /// <summary>
        /// Validate the Name property.
        /// This will throw an exception if the property is an
        /// Name that does not follow the rules of only letters, digits and underscores in
        /// Name names.
        /// </summary>
        internal void ValidateNames(
            string   value,
            int      lineNumber,
            int      linePosition)
        {
            // set the linenumber and position
            _lineNumber = lineNumber;
            _linePosition = linePosition;

            if (value == string.Empty)
            {
                ThrowException(SRID.ParserBadName, value);
            }

            if (MarkupExtensionParser.LooksLikeAMarkupExtension(value))
            {
                string message = SR.Get(SRID.ParserBadUidOrNameME, value);
                message += " ";
                message += SR.Get(SRID.ParserLineAndOffset,
                            lineNumber.ToString(CultureInfo.CurrentCulture),
                            linePosition.ToString(CultureInfo.CurrentCulture));

                XamlParseException parseException = new XamlParseException(message, lineNumber, linePosition);

                throw parseException;
            }

            if (!NameValidationHelper.IsValidIdentifierName(value))
            {
                ThrowException(SRID.ParserBadName, value);
            }
        }

        /// <summary>
        /// Validate that if the type converter is
        /// for enums you can't pass numbers to it.
        /// </summary>
        internal void ValidateEnums(
            string        propName,
            Type          propType,
            string        attribValue)
        {
            if (propType.IsEnum && attribValue != string.Empty)
            {
                // Handle enum strings of the form "one, two, three".  Check that
                // each of the values does NOT start with a digit.  This doesn't
                // validate that the enum is correct, just that there are no digits
                // specified.
                bool lookingForComma = false;
                for (int i = 0; i < attribValue.Length; i++)
                {
                    if (!Char.IsWhiteSpace(attribValue[i]))
                    {
                        if (lookingForComma)
                        {
                            if (attribValue[i] == ',')
                            {
                                lookingForComma = false;
                            }
                        }
                        else if (Char.IsDigit(attribValue[i]))
                        {
                            ThrowException(SRID.ParserNoDigitEnums, propName, attribValue);
                        }
                        else
                        {
                            lookingForComma = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get cached member info for the property name.  This can be
        /// a PropertyInfo for the property or a MethodInfo for the static
        /// setter.  This does not work for EventInfo, so don't call it.
        /// </summary>
        /// <param name="owner">Type of the owner of the property </param>
        /// <param name="propName">Name of the property</param>
        /// <param name="onlyPropInfo">True if the caller wants the PropertyInfo for the
        ///     case where both a MethodInfo and propertyInfo are cached for this property
        /// </param>
        /// <param name="infoRecord">The attribute info record retrieved from
        ///     the map table, if one is found. </param>
        /// <remarks>
        /// There is only one scenario under which two memberInfo need to be
        /// cached for a given attribute. This is the case when there are both a
        /// Clr wrapper and static Settor for a given DP. In that case we cache
        /// an object array of two elements. Also the MethodInfo for the given DP
        /// will be discovered first by the XamlReaderHelper while the PropertyInfo
        /// will be discovered by the BamlRecordWriter.
        /// </remarks>
        private MemberInfo GetCachedMemberInfo(
               Type                    owner,
               string                  propName,
               bool                    onlyPropInfo,
           out BamlAttributeInfoRecord infoRecord)
        {
            infoRecord = null;
            if (MapTable != null)
            {
                string fullName = owner.IsGenericType ? owner.Namespace + "." + owner.Name : owner.FullName;
                object key = MapTable.GetAttributeInfoKey(fullName, propName);
                infoRecord = MapTable.GetHashTableData(key) as BamlAttributeInfoRecord;

                if (infoRecord != null)
                {
                    return infoRecord.GetPropertyMember(onlyPropInfo) as MemberInfo;
                }
            }
            return null;
        }

#if !PBTCOMPILER
        /// <summary>
        /// Add cached member info for the property name.
        /// </summary>
        private void AddCachedAttributeInfo(
               Type                    ownerType,
               BamlAttributeInfoRecord infoRecord)
        {
            if (MapTable != null)
            {
                object key = MapTable.GetAttributeInfoKey(ownerType.FullName, infoRecord.Name);
                MapTable.AddHashTableData(key, infoRecord);
            }
        }

        /// <summary>
        /// Helper function for getting Clr PropertyInfo on a type and updating the
        /// passed attribute info record.  Also update the property cache with this
        /// attribute information if it was not already present.
        /// </summary>
        /// <remarks>
        /// Note that the ObjectHashTable may contain
        /// a BamlAttributeInfoRecord from a previous parse for the same property.  If
        /// we find one in the hash table, use its property info instead of reflecting.
        /// </remarks>
        internal void UpdateClrPropertyInfo(
            Type currentParentType,
            BamlAttributeInfoRecord attribInfo)
        {
            Debug.Assert(null != attribInfo, "null attribInfo");
            Debug.Assert(null != currentParentType, "null currentParentType");

            bool isInternal = false;
            string propName = attribInfo.Name;

            BamlAttributeInfoRecord cachedInfoRecord;
            attribInfo.PropInfo = GetCachedMemberInfo(currentParentType, propName, true, out cachedInfoRecord)
                as PropertyInfo;

            if (attribInfo.PropInfo == null)
            {
                // If no cached property info, use the slow route of reflecting to get
                // the property info.
                attribInfo.PropInfo = PropertyInfoFromName(propName, currentParentType, !ReflectionHelper.IsPublicType(currentParentType), false, out isInternal);
                attribInfo.IsInternal = isInternal;
                if (attribInfo.PropInfo != null)
                {
                    // If we successfully find a property info via reflection, cache it.
                    if (cachedInfoRecord != null)
                    {
                        cachedInfoRecord.SetPropertyMember(attribInfo.PropInfo);
                        cachedInfoRecord.IsInternal = attribInfo.IsInternal;
                    }
                    else
                    {
                        AddCachedAttributeInfo(currentParentType, attribInfo);
                    }
                }
            }
            else
            {
                attribInfo.IsInternal = cachedInfoRecord.IsInternal;
            }
        }

        private void UpdateAttachedPropertyMethdodInfo(BamlAttributeInfoRecord attributeInfo, bool isSetter)
        {
            MethodInfo attachedPropertyInfo = null;
            Type propertyOwnerType = attributeInfo.OwnerType;
            Debug.Assert(propertyOwnerType != null);
            bool tryInternal = !ReflectionHelper.IsPublicType(propertyOwnerType);
            string propName = (isSetter ? "Set" : "Get") + attributeInfo.Name;
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;

            // Check Set\GetFoo method presence

            try
            {
                if (!tryInternal)
                {
                    // first try public methods
                    attachedPropertyInfo = propertyOwnerType.GetMethod(propName, flags);
                }

                if (attachedPropertyInfo == null)
                {
                    // if public method not found, try non-public method next.
                    attachedPropertyInfo = propertyOwnerType.GetMethod(propName, flags | BindingFlags.NonPublic);
                }
            }
            catch (AmbiguousMatchException)
            {
            }

            int paramCount = isSetter ? 2 : 1;
            if (attachedPropertyInfo != null && attachedPropertyInfo.GetParameters().Length == paramCount)
            {
                // the MethodInfo has to be public or internal.
                Debug.Assert(attachedPropertyInfo.IsPublic ||
                                 attachedPropertyInfo.IsAssembly ||
                                 attachedPropertyInfo.IsFamilyOrAssembly);

                if (isSetter)
                {
                    attributeInfo.AttachedPropertySetter = attachedPropertyInfo;
                }
                else
                {
                    attributeInfo.AttachedPropertyGetter = attachedPropertyInfo;
                }
            }
        }

        internal void UpdateAttachedPropertySetter(BamlAttributeInfoRecord attributeInfo)
        {
            if (attributeInfo.AttachedPropertySetter == null)
            {
                UpdateAttachedPropertyMethdodInfo(attributeInfo, true);
            }
        }

        internal void UpdateAttachedPropertyGetter(BamlAttributeInfoRecord attributeInfo)
        {
            if (attributeInfo.AttachedPropertyGetter == null)
            {
                UpdateAttachedPropertyMethdodInfo(attributeInfo, false);
            }
        }

#endif

        /// <summary>
        /// Common helper method to resolve an xml XmlNamespace and LocalName to
        /// either a EventInfo or a PropertyInfo.
        /// </summary>
        /// <param name="isEvent">True if Event, False look for Property</param>
        /// <param name="owner">Type we should look for attribute on, can be null</param>
        /// <param name="xmlNamespace">XmlNamespace or the Attribute</param>
        /// <param name="localName">local Name of the Attribute</param>
        /// <param name="propName">Name of the resolved property or event</param>
        /// <returns>PropertyInfo or EventInfo for resolved property or event</returns>
        internal MemberInfo GetClrInfo(
                bool   isEvent,
                Type   owner,
                string xmlNamespace,
                string localName,
            ref string propName)
        {
            Debug.Assert(null != localName, "null localName");
            Debug.Assert(null != xmlNamespace, "null xmlNamespace");

            // adjust urtNamespace and localName if there are any periods in the localName.
            string globalClassName = null;
            int lastIndex = localName.LastIndexOf('.');

            if (-1 != lastIndex)
            {
                // If using .net then match against the class.
                globalClassName = localName.Substring(0, lastIndex);
                localName = localName.Substring(lastIndex+1);
            }

            return GetClrInfoForClass(isEvent, owner, xmlNamespace, localName, globalClassName, ref propName);
        }

#if PBTCOMPILER
        // Checks to see if a given event handler delegate type is accessible.
        static private bool IsAllowedEventDelegateType(Type delegateType)
        {
            if (!ReflectionHelper.IsPublicType(delegateType))
            {
                if (!ReflectionHelper.IsInternalType(delegateType) ||
                    !IsInternalAllowedOnType(delegateType))
                {
                    return false;
                }
            }

            return true;
        }

        // Checks to see if a given event's add method is accessible.
        // Checks for protected add methods as well if requested.
        private bool IsAllowedEvent(EventInfo ei, bool isProtectedAllowed)
        {
            MethodInfo mi = ei.GetAddMethod(true);
            return IsAllowedMethod(mi, isProtectedAllowed);
        }

        // Checks to see if a given property's set method is accessible.
        // Always checks for protected set methods as well.
        internal bool IsAllowedPropertySet(PropertyInfo pi)
        {
            MethodInfo mi = pi.GetSetMethod(true);
            return IsAllowedMethod(mi, true);
        }

        // Checks to see if a given property's get method is accessible.
        // Checks for protected get methods as well if requested.
        private bool IsAllowedPropertyGet(PropertyInfo pi, bool checkProtected)
        {
            MethodInfo mi = pi.GetGetMethod(true);
            return IsAllowedMethod(mi, checkProtected);
        }

        // Checks to see if a given property's get method is accessible.
        // Always checks for protected get methods as well.
        internal bool IsAllowedPropertyGet(PropertyInfo pi)
        {
            MethodInfo mi = pi.GetGetMethod(true);
            return IsAllowedMethod(mi, true);
        }

        // Checks to see if a given field member is accessible.
        private bool IsAllowedField(FieldInfo fi)
        {
            bool allowed = false;

            // No field, so not allowed
            if (fi != null)
            {
                // field is public -- always allow.
                allowed = fi.IsPublic;
                if (!allowed)
                {
                    // if not, try accessible internal fields.
                    // if the field from a base type, then it must be in the same assembly
                    // as the type from which it was reflected. If not, internals will not
                    // be allowed.
                    if (fi.ReflectedType.Assembly == fi.DeclaringType.Assembly)
                    {
                        // if reflected type is public, check to see if internals are allowed
                        // on that type (i.e local or friend). If not, the type has to be an
                        // internal allowed type due to the guaranteed central check in
                        // CreateTypeAndSerializer().
                        if (ReflectionHelper.IsPublicType(fi.ReflectedType))
                        {
                            allowed = IsInternalAllowedOnType(fi.ReflectedType);
                        }
                        else
                        {
                            allowed = true;
                        }

                        // Either ways, if reflected type is allowed only allow non-public
                        // fields that are internal.
                        allowed = allowed && (fi.IsAssembly || fi.IsFamilyOrAssembly);
                    }
                }
            }

            return allowed;
        }

        // Checks to see if a given methodInfo (for a property's get\set method
        // or an event's add method) is accessible.
        private bool IsAllowedMethod(MethodInfo mi, bool checkProtected)
        {
            bool allowed = false;

            // No method, so not allowed
            if (mi != null)
            {
                // method is public -- always allow.
                allowed = mi.IsPublic;
                if (!allowed)
                {
                    // method is not public.
                    // Next check to see if the mapper will allow looking for protected
                    // attributes. This will be the case if current methodInfo has been
                    // reflected off of the markup sub-classed root element, i.e one with
                    // an x:Class attribute, in which case IsProtectedAttributeAllowed will
                    // be true. So in this case allow protected if caller wishes for this
                    // by setting the checkProtected param to true.
                    if (checkProtected && IsProtectedAttributeAllowed)
                    {
                        // if so, allow protected or internal protected method.
                        allowed = mi.IsFamily || mi.IsFamilyOrAssembly;
                    }

                    if (!allowed)
                    {
                        // if not, try accessible internal methods.
                        // if the property or event inherits from a base type, then its
                        // accessor method must be in the same assembly as the type from
                        // which it was reflected. If not, internals will not be allowed.
                        if (mi.ReflectedType.Assembly == mi.DeclaringType.Assembly)
                        {
                            // if reflected type is public, check to see if internals are allowed
                            // on that type (i.e local or friend). If not, the type has to be an
                            // internal allowed type due to the guaranteed central check in
                            // CreateTypeAndSerializer().
                            if (ReflectionHelper.IsPublicType(mi.ReflectedType))
                            {
                                allowed = IsInternalAllowedOnType(mi.ReflectedType);
                            }
                            else
                            {
                                allowed = true;
                            }

                            // Either ways, if reflected type is allowed only allow non-public members
                            // that are internal.
                            allowed = allowed && (mi.IsAssembly || mi.IsFamilyOrAssembly);
                        }
                    }
                }
            }

            return allowed;
        }
#else
        // Checks to see if a given property's set method is public.
        // Used only in Xaml Load sceanrios.
        internal bool IsAllowedPropertySet(PropertyInfo pi)
        {
            MethodInfo mi = pi.GetSetMethod(true);
            return (mi != null && mi.IsPublic);
        }

        // Checks to see if a given property's get method is public.
        // Used only in Xaml Load sceanrios.
        internal bool IsAllowedPropertyGet(PropertyInfo pi)
        {
            MethodInfo mi = pi.GetGetMethod(true);
            return (mi != null && mi.IsPublic);
        }

        // Checks to see if a given property's set method is accessible.
        // Used only in compiled Baml Load sceanrios.
        static internal bool IsAllowedPropertySet(PropertyInfo pi, bool allowProtected, out bool isPublic)
        {
            MethodInfo mi = pi.GetSetMethod(true);
            bool isProtected = allowProtected && mi != null && mi.IsFamily;
            // return isPublic == true only if the property is public on a base declaring Type.
            // if the property is public on the reflected internal type itself, then we still
            // need to call the generated helper to set the property.
            isPublic = mi != null && mi.IsPublic && ReflectionHelper.IsPublicType(mi.DeclaringType);
            return (mi != null && (mi.IsPublic || mi.IsAssembly || mi.IsFamilyOrAssembly || isProtected));
        }

        // Checks to see if a given property's get method is accessible.
        // Used only in compiled Baml Load sceanrios.
        static private bool IsAllowedPropertyGet(PropertyInfo pi, bool allowProtected, out bool isPublic)
        {
            MethodInfo mi = pi.GetGetMethod(true);
            bool isProtected = allowProtected && mi != null && mi.IsFamily;
            // return isPublic == true only if the property is public on a base declaring Type.
            // if the property is public on the reflected internal type itself, then we still
            // need to call the generated helper to get the property.
            isPublic = mi != null && mi.IsPublic && ReflectionHelper.IsPublicType(mi.DeclaringType);
            return (mi != null && (mi.IsPublic || mi.IsAssembly || mi.IsFamilyOrAssembly || isProtected));
        }

        // Checks to see if a given event's add method is accessible.
        // Used only in compiled Baml Load sceanrios.
        static private bool IsAllowedEvent(EventInfo ei, bool allowProtected, out bool isPublic)
        {
            MethodInfo mi = ei.GetAddMethod(true);
            bool isProtected = allowProtected && mi != null && mi.IsFamily;
            // return isPublic == true only if the event is public on a base declaring Type.
            // if the event is public on the reflected internal type itself, then we still
            // need to call the generated helper to hook up the event.
            isPublic = mi != null && mi.IsPublic && ReflectionHelper.IsPublicType(mi.DeclaringType);
            return (mi != null && (mi.IsPublic || mi.IsAssembly || mi.IsFamilyOrAssembly || isProtected));
        }
#endif

        // Checks to see if a given event's add method is public.
        // Used in all (xaml load, xaml compile & compiled Baml Load sceanrios.
        static private bool IsPublicEvent(EventInfo ei)
        {
            MethodInfo mi = ei.GetAddMethod(true);
            return (mi != null && mi.IsPublic);
        }

#if !PBTCOMPILER
        /// <summary>
        /// Allows a sub-classed XamlTypeMapper called under Full Trust to participate
        /// in deciding if an internal type should be accessible.
        /// </summary>
        /// <param name="type">The internal type</param>
        /// <returns>
        /// When overriden, should return true if accessible, false if not.
        /// Returns false by default, if no one overrides.
        /// </returns>
        protected virtual bool AllowInternalType(Type type)
        {
            return false;
        }

        private bool IsInternalTypeAllowedInFullTrust(Type type)
        {
            bool isAllowed = false;
            // If the type is internal, then allow them to participate
            // in deciding if that internal type should be accessible.
            if (ReflectionHelper.IsInternalType(type))
            {
                isAllowed = AllowInternalType(type);
            }

            return isAllowed;
        }
#endif

        /// <summary>
        /// Common helper method to resolve an xml XmlNamespace and LocalName to
        /// either a EventInfo or a PropertyInfo.
        /// </summary>
        /// <param name="isEvent">True if Event, False look for Property</param>
        /// <param name="owner">Type we should look for attribute on, can be null</param>
        /// <param name="xmlNamespace">XmlNamespace or the Attribute</param>
        /// <param name="localName">local Name of the Attribute with no class</param>
        /// <param name="globalClassName">Class Name of the Attribute, or null if not present</param>
        /// <param name="propName">Name of the resolved property or event</param>
        /// <returns>PropertyInfo or EventInfo for resolved property or event</returns>
        internal MemberInfo GetClrInfoForClass (
                bool   isEvent,
                Type   owner,
                string xmlNamespace,
                string localName,
                string globalClassName,
            ref string propName)
        {
            MemberInfo mi = null;
#if PBTCOMPILER
            if (owner == null || ReflectionHelper.IsPublicType(owner))
            {
#endif
            // first, try normal lookup for public properties and events only.
                mi = GetClrInfoForClass(isEvent, owner, xmlNamespace, localName, globalClassName, false, ref propName);
#if PBTCOMPILER
            }

            if (mi == null && owner != null)
            {
                // if lookup on internal type or if public property or event lookup failed,
                // try internal ones as well, or protected if the type happens to be a
                // code-generated root.
                mi = GetClrInfoForClass(isEvent, owner, xmlNamespace, localName, globalClassName, true, ref propName);
            }
#endif

            return mi;
        }

        private MemberInfo GetClrInfoForClass(
                bool isEvent,
                Type owner,
                string xmlNamespace,
                string localName,
                string globalClassName,
                bool tryInternal,
            ref string propName)
        {
            bool isInternal = false;
            MemberInfo memberInfo = null;
            BindingFlags defaultBinding = BindingFlags.Public;

#if PBTCOMPILER
            if (tryInternal)
            {
                defaultBinding |= BindingFlags.NonPublic;
            }
#endif

            propName = null;
            ParameterInfo[] pis = null;

            // if this is a globalClass then resolve the type and then call the dpFromName
            if (null != globalClassName)
            {
                TypeAndSerializer typeAndSerializer =
                    GetTypeOnly(xmlNamespace, globalClassName);

                if (typeAndSerializer != null && typeAndSerializer.ObjectType != null)
                {
                    BamlAttributeInfoRecord infoRecord;
                    Type objectType = typeAndSerializer.ObjectType;
                    memberInfo = GetCachedMemberInfo(objectType, localName, false, out infoRecord);

                    if (memberInfo == null)
                    {
                        if (isEvent)
                        {
                            // See if attached event first
                            memberInfo = objectType.GetMethod("Add" + localName + "Handler",
                                defaultBinding |
                                BindingFlags.Static |
                                BindingFlags.FlattenHierarchy);

                            // Make sure that we found a method of the right signature.
                            // Otherwise discard what you found.
                            if (memberInfo != null)
                            {
                                MethodInfo mi = memberInfo as MethodInfo;
                                if (mi != null)
                                {
                                    pis = mi.GetParameters();
                                    Type dependencyObjectType = KnownTypes.Types[(int)KnownElements.DependencyObject];
                                    if (pis == null || pis.Length != 2 || !dependencyObjectType.IsAssignableFrom(pis[0].ParameterType))
                                    {
                                        memberInfo = null;
                                    }
#if PBTCOMPILER
                                    if (tryInternal && memberInfo != null && !IsAllowedMethod(mi, false))
                                    {
                                        ThrowException(SRID.ParserCantSetAttribute, "bubbling event", objectType.Name + "." + localName, "Add Handler method");
                                    }
#endif
                                }
                            }

                            if (memberInfo == null)
                            {
                                // Not an attached event so try Clr event
                                memberInfo = objectType.GetEvent(localName,
                                    defaultBinding |
                                    BindingFlags.Instance |
                                    BindingFlags.FlattenHierarchy);

                                if (memberInfo != null)
                                {
                                    EventInfo ei = memberInfo as EventInfo;
#if PBTCOMPILER
                                    if (!IsAllowedEventDelegateType(ei.EventHandlerType))
#else
                                    if (!ReflectionHelper.IsPublicType(ei.EventHandlerType))
#endif
                                    {
                                        ThrowException(SRID.ParserEventDelegateTypeNotAccessible, ei.EventHandlerType.FullName, objectType.Name + "." + localName);
                                    }

#if PBTCOMPILER
                                    if (tryInternal)
                                    {
                                        // Check if the event add method accessor itself is accessible.
                                        // Also if this is a non-public event on a public type, it will
                                        // check to make sure that the public type is accessible\allowed.
                                        if (!IsAllowedEvent(ei, false))
                                        {
                                            ThrowException(SRID.ParserCantSetAttribute, "event", objectType.Name + "." + localName, "add");
                                        }
                                    }
                                    else
                                    {
#endif
                                        // Check if the event add method accessor itself is public.
                                        if (!IsPublicEvent(ei))
                                        {
#if PBTCOMPILER
                                            memberInfo = null;
#else
                                            ThrowException(SRID.ParserCantSetAttribute, "event", objectType.Name + "." + localName, "add");
#endif
                                        }
#if PBTCOMPILER
                                    }
#endif
                                }
                            }
                        }
                        else
                        {
                            // See if attached property first - start from a Setter
                            memberInfo = objectType.GetMethod("Set" + localName,
                                defaultBinding |
                                BindingFlags.Static |
                                BindingFlags.FlattenHierarchy);
                            if (memberInfo != null && ((MethodInfo)memberInfo).GetParameters().Length != 2)
                            {
                                memberInfo = null;
                            }
                            // Try read-only case (Getter only)
                            if (memberInfo == null)
                            {
                                memberInfo = objectType.GetMethod("Get" + localName,
                                    defaultBinding |
                                    BindingFlags.Static |
                                    BindingFlags.FlattenHierarchy);
                                if (memberInfo != null && ((MethodInfo)memberInfo).GetParameters().Length != 1)
                                {
                                    memberInfo = null;
                                }
                            }

#if PBTCOMPILER
                            if (tryInternal && memberInfo != null && !IsAllowedMethod(memberInfo as MethodInfo, false))
                            {
                                ThrowException(SRID.ParserCantSetAttribute, "attached property", objectType.Name + "." + localName, "Set method");
                            }
#endif

                            if (memberInfo == null)
                            {
                                // Not an attached property, so try clr property
                                memberInfo = PropertyInfoFromName(localName, objectType, tryInternal, true, out isInternal);

                                // If we've found a property info, then the owner had better
                                // be the same type as or a subclass of the objectType, or
                                // they are in different inheritance hierarchies.  
                                if (memberInfo != null)
                                {
                                    if (owner != null &&
                                        !objectType.IsAssignableFrom(owner))
                                    {
                                        ThrowException(SRID.ParserAttachedPropInheritError,
                                                       String.Format(CultureInfo.CurrentCulture, "{0}.{1}", objectType.Name, localName),
                                                       owner.Name);
                                    }
                                }
                            }

                            if (null != memberInfo)
                            {
                                if (infoRecord != null)
                                {
#if !PBTCOMPILER
                                    // DP's aren't present in the PBT case
                                    if (infoRecord.DP == null)
                                    {
                                        infoRecord.DP = MapTable.GetDependencyProperty(infoRecord);
                                    }
#endif
                                    infoRecord.SetPropertyMember(memberInfo);
                                }
                            }
                        }
                    }
                }
            }
            else if (null != owner)
            {
                Type baseType = owner;

                // See if the owner knows about this class.
                // Look for a parent type until we find a match or fail.
                if (null != baseType)
                {
                    BamlAttributeInfoRecord infoRecord;
                    memberInfo = GetCachedMemberInfo(baseType, localName, false, out infoRecord);

                    if (memberInfo == null)
                    {
                        if (isEvent)
                        {
                            // See if attached event first
                            memberInfo = baseType.GetMethod("Add" + localName + "Handler",
                                defaultBinding | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                            // Make sure that we found a method of the right signature.
                            // Otherwise discard what you found.
                            if (memberInfo != null)
                            {
                                MethodInfo mi = memberInfo as MethodInfo;
                                if (mi != null)
                                {
                                    pis = mi.GetParameters();
                                    Type dependencyObjectType = KnownTypes.Types[(int)KnownElements.DependencyObject];
                                    if (pis == null || pis.Length != 2 || !dependencyObjectType.IsAssignableFrom(pis[0].ParameterType))
                                    {
                                        memberInfo = null;
                                    }
#if PBTCOMPILER
                                    if (tryInternal && memberInfo != null && !IsAllowedMethod(mi, true))
                                    {
                                        ThrowException(SRID.ParserCantSetAttribute, "bubbling event", owner.Name + "." + localName, "Add Handler method");
                                    }
#endif
                                }
                            }

                            if (memberInfo == null)
                            {
                                // Not an attached event, so try for a clr event.
                                memberInfo = baseType.GetEvent(localName,
                                    BindingFlags.Instance | BindingFlags.FlattenHierarchy | defaultBinding);

                                if (memberInfo != null)
                                {
                                    EventInfo ei = memberInfo as EventInfo;
#if PBTCOMPILER
                                    if (!IsAllowedEventDelegateType(ei.EventHandlerType))
#else
                                    if (!ReflectionHelper.IsPublicType(ei.EventHandlerType))
#endif
                                    {
                                        ThrowException(SRID.ParserEventDelegateTypeNotAccessible, ei.EventHandlerType.FullName, owner.Name + "." + localName);
                                    }

#if PBTCOMPILER
                                    if (tryInternal)
                                    {
                                        // Check if the event add method accessor itself is accessible.
                                        // Also if this is a non-public event on a public type, it will
                                        // not check to make sure that the public type is accessible\allowed
                                        // since that would have all ready been done in the caller of this fucntion.
                                        if (!IsAllowedEvent(ei, true))
                                        {
                                            ThrowException(SRID.ParserCantSetAttribute, "event", owner.Name + "." + localName, "add");
                                        }
                                    }
                                    else
                                    {
#endif
                                        // check if the event add method accessor itself is public.
                                        if (!IsPublicEvent(ei))
                                        {
#if PBTCOMPILER
                                            memberInfo = null;
#else
                                            ThrowException(SRID.ParserCantSetAttribute, "event", owner.Name + "." + localName, "add");
#endif
                                        }
#if PBTCOMPILER
                                    }
#endif
                                }
                            }
                        }
                        else
                        {
                            // See if attached property first - start from a Setter
                            memberInfo = baseType.GetMethod("Set" + localName,
                                defaultBinding |
                                BindingFlags.Static |
                                BindingFlags.FlattenHierarchy);
                            if (memberInfo != null && ((MethodInfo)memberInfo).GetParameters().Length != 2)
                            {
                                memberInfo = null;
                            }
                            // Try read-only case (Getter only)
                            if (memberInfo == null)
                            {
                                memberInfo = baseType.GetMethod("Get" + localName,
                                    defaultBinding |
                                    BindingFlags.Static |
                                    BindingFlags.FlattenHierarchy);
                                if (memberInfo != null && ((MethodInfo)memberInfo).GetParameters().Length != 1)
                                {
                                    memberInfo = null;
                                }
                            }

#if PBTCOMPILER
                            if (tryInternal && memberInfo != null && !IsAllowedMethod(memberInfo as MethodInfo, true))
                            {
                                ThrowException(SRID.ParserCantSetAttribute, "attached property", owner.Name + "." + localName, "Set method");
                            }
#endif

                            if (memberInfo == null)
                            {
                                // Not an attached property, so try for a clr property.
                                memberInfo = PropertyInfoFromName(localName, baseType, tryInternal, true, out isInternal);
                            }

                            if (null != memberInfo)
                            {
                                if (infoRecord != null)
                                {
#if !PBTCOMPILER
                                    // DP's aren't present in the PBT case
                                    if (infoRecord.DP == null)
                                    {
                                        infoRecord.DP = MapTable.GetDependencyProperty(infoRecord);
                                    }
#endif
                                    infoRecord.SetPropertyMember(memberInfo);
                                }
                            }
                        }
                    }
                }
            }

            if (null != memberInfo)
            {
                propName = localName;
            }

            return memberInfo;
        }

#if !PBTCOMPILER
        /// <summary>
        /// Helper method to get an event on an owner, walking up the class hierarchy
        /// when doing so
        /// </summary>
        /// <param name="owner">Type we should look for the event on</param>
        /// <param name="eventName">Then name of the handler of the event</param>
        /// <returns>EventInfo for resolved event</returns>
        internal EventInfo GetClrEventInfo(
            Type   owner,
            string eventName)
        {
            Debug.Assert(null != eventName, "null eventName");
            Debug.Assert(null != owner, "null owner");

            EventInfo eventInfo = null;

            // Look up the parent chain until we find a match or fail by
            // going off the top of the chain.
            while (owner != null)
            {
                eventInfo = owner.GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public);
                if (eventInfo != null)
                {
                    break;
                }

                owner = GetCachedBaseType(owner);
            }

            return eventInfo;
        }

        /// <summary>
        /// Helper method to resolve an xml namespace and localName to
        /// either a RoutedEvent or a DependencyProperty.  If they are not present,
        /// still resolve using a guess at the valid setter name to look for.
        /// </summary>
        /// <remarks>
        ///  Note that this will not resolve clr properties.  Call GetClrInfo to do that.
        /// </remarks>
        /// <param name="isEvent">True if Event, False look for Property</param>
        /// <param name="owner">Type we should look for attribute on, can be null</param>
        /// <param name="xmlNamespace">XmlNamespace or the Attribute</param>
        /// <param name="localName">local Name of the Attribute</param>
        /// <param name="baseType">Base type the object was found on</param>
        /// <param name="dynamicObjectName">registered name of the Object on the type.</param>
        /// <returns>resolved object, which can be a RoutedEvent, a DependencyProperty
        ///          or the MethodInfo for the event or property setter</returns>
        internal object GetDependencyObject(
                bool       isEvent,
                Type       owner,
                string     xmlNamespace,
                string     localName,
            ref Type       baseType,
            ref string     dynamicObjectName)
        {
            Debug.Assert(null != localName, "null localName");
            Debug.Assert(null != xmlNamespace, "null xmlNamespace");

            object memInfo = null;
            string globalClassName = null;

            dynamicObjectName = null;

            // Extract the class name if there are any periods in the localName.
            int lastIndex = localName.LastIndexOf('.');
            if (-1 != lastIndex)
            {
                // if using .net then match against the class.
                globalClassName = localName.Substring(0,lastIndex);
                localName = localName.Substring(lastIndex+1);
            }

            // If this is a globalClassName then resolve the type and then call
            // DependencyProperty.FromName.
            if (null != globalClassName)
            {
                TypeAndSerializer typeAndSerializer =
                    GetTypeOnly(xmlNamespace,globalClassName);

                if (typeAndSerializer != null && typeAndSerializer.ObjectType != null)
                {
                    baseType = typeAndSerializer.ObjectType;
                    if (isEvent)
                    {
                        memInfo = RoutedEventFromName(localName,baseType);
                    }
                    else
                    {
                        memInfo = DependencyProperty.FromName(localName, baseType);
                    }

                    if (null != memInfo)
                    {
                        Debug.Assert(null != baseType, "baseType not set");
                        dynamicObjectName = localName;
                    }
                }
            }
            else
            {
                NamespaceMapEntry[] namespaceMaps = GetNamespaceMapEntries(xmlNamespace);

                if (null == namespaceMaps)
                {
                    return null;
                }

                baseType = owner;

                // See if the owner knows about this class.
                // Look for a parent type with any namespace matching the property
                while (null != baseType)
                {
                    bool foundNamespaceMatch = false;

                    // Look at each namespace for a match with this baseType
                    for (int count = 0;
                         count < namespaceMaps.Length && !foundNamespaceMatch;
                         count ++)
                    {
                        NamespaceMapEntry namespaceMap = namespaceMaps[count];

                        // see if the urtNamespace in the namespace map is valid
                        // for the type we are trying to apply
                        if (namespaceMap.ClrNamespace == GetCachedNamespace(baseType))
                        {
                            foundNamespaceMatch = true;
                        }
                    }

                    if (foundNamespaceMatch)
                    {
                        // For 'normal' properties and events that are not prefixed by
                        // a class name, only attempt to get dependency IDs and Events.
                        // The caller should use GetClrInfo to get CLR properties for
                        // 'normal' properties and events if this attempt fails.
                        if (isEvent)
                        {
                            memInfo = RoutedEventFromName(localName,baseType);
                        }
                        else
                        {
                            memInfo = DependencyProperty.FromName(localName, baseType);
                        }
                    }

                    // Only do one loop for events, since all base classes are checked in
                    // a single operation.  For properties, loop through the base classes here.
                    if (null != memInfo || isEvent)
                    {
                        // for assembly and typeName use the original, not the base
                        // type we found it on.
                        dynamicObjectName = localName;
                        break;
                    }
                    else
                    {
                        baseType = GetCachedBaseType(baseType);
                    }
                }
            }

            return memInfo;
        }


         ///<summary>
        /// Returns a DependencyProperty given a local name and an xml namespace
        ///</summary>
        ///<param name="localName">
        /// The property name
        ///</param>
        ///<param name="xmlNamespace">
        /// Xml namespace associated with name
        ///</param>
        ///<param name="ownerType">
        /// The Type where this DP was registered.
        ///</param>
        ///<returns>
        /// Returns a DependencyProperty the attached property
        ///</returns>
        internal DependencyProperty DependencyPropertyFromName(
                string localName,
                string xmlNamespace,
            ref Type   ownerType)
         {
             Debug.Assert(null != localName, "null localName");
             Debug.Assert(null != xmlNamespace, "null xmlNamespace");

            // Adjust localName if there are any periods that indicate a global class.  Use
            // this class name as the owner type and return it.  Otherwise just use the
            // passed name and owner.
            int lastIndex = localName.LastIndexOf('.');
            if (-1 != lastIndex)
            {
                string globalClassName = localName.Substring(0,lastIndex);
                localName = localName.Substring(lastIndex+1);
                TypeAndSerializer typeAndSerializer =
                    GetTypeOnly(xmlNamespace, globalClassName);
                if (typeAndSerializer == null || typeAndSerializer.ObjectType == null)
                {
                    ThrowException(SRID.ParserNoType, globalClassName);
                }
                ownerType = typeAndSerializer.ObjectType;
            }

            if(null == ownerType)
            {
                throw new ArgumentNullException( "ownerType" );
            }

            return DependencyProperty.FromName(localName, ownerType);
        }

#endif

        /// <summary>
        /// Return the property that has an attached XmlLang attribute.  This identifies this
        /// property as being the one to receive xml:lang attribute values when parsing, or
        /// the holder of the CultureInfo related string.  The XamlTypeMapper caches this
        /// along with the TypeAndSerializer information for fast retrieval.
        /// </summary>
        internal PropertyInfo GetXmlLangProperty(
                string    xmlNamespace,     // xml namespace for the type
                string    localName)        // local name of the type without any '.'
        {
            TypeAndSerializer typeAndSerializer = GetTypeOnly(xmlNamespace, localName);

            if (typeAndSerializer == null || typeAndSerializer.ObjectType == null)
            {
                return null;
            }

            if (typeAndSerializer.XmlLangProperty == null)
            {
                BamlAssemblyInfoRecord bairPF = MapTable.GetAssemblyInfoFromId(-1);

                if (typeAndSerializer.ObjectType.Assembly == bairPF.Assembly)
                {
                    if (KnownTypes.Types[(int)KnownElements.FrameworkElement].IsAssignableFrom(typeAndSerializer.ObjectType) ||
                        KnownTypes.Types[(int)KnownElements.FrameworkContentElement].IsAssignableFrom(typeAndSerializer.ObjectType))
                    {
                        typeAndSerializer.XmlLangProperty = (KnownTypes.Types[(int)KnownElements.FrameworkElement]).GetProperty("Language",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    }
                }
                else
                {
                    string xmlLangPropertyName = null;
                    bool   xmlLangPropertyFound = false;

#if !PBTCOMPILER
                    AttributeCollection attributes = TypeDescriptor.GetAttributes(typeAndSerializer.ObjectType);

                    if (attributes != null)
                    {
                        XmlLangPropertyAttribute xlpa = attributes[typeof(XmlLangPropertyAttribute)] as XmlLangPropertyAttribute;
                        if (xlpa != null)
                        {
                            xmlLangPropertyFound = true;
                            xmlLangPropertyName = xlpa.Name;
                        }
                    }
#else
                    Type typeValue = null;
                    xmlLangPropertyName = ReflectionHelper.GetCustomAttributeData(typeAndSerializer.ObjectType,
                                                                                  KnownTypes.Types[(int)KnownElements.XmlLangPropertyAttribute],
                                                                                  false,
                                                                              ref xmlLangPropertyFound,
                                                                              out typeValue);
#endif

                    if ( xmlLangPropertyFound )
                    {
                        if( xmlLangPropertyName != null && xmlLangPropertyName.Length > 0)
                        {
                            typeAndSerializer.XmlLangProperty = typeAndSerializer.ObjectType.GetProperty(
                                xmlLangPropertyName,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                        }

                        if( typeAndSerializer.XmlLangProperty == null )
                        {
                            // Either the given name could not be found, or there
                            //  was no name specified at all.  (null or empty string.)
                            // The latter case may get a special meaning in the future,
                            //  but for now they're all errors.
                            ThrowException(SRID.ParserXmlLangPropertyValueInvalid);
                        }
                    }
                }
            }

            return typeAndSerializer.XmlLangProperty;
        }

        ///<summary>
        /// Returns a PropertyInfo from an attribute name, given the ownerType of
        /// the property.
        ///</summary>
        ///<param name="localName">
        /// The attribute name
        ///</param>
        ///<param name="ownerType">
        /// The Type that owns the attribute
        ///</param>
        ///<param name="tryInternal">
        /// Indicates if this function should search for non-public properties as well
        ///</param>
        ///<param name="tryPublicOnly">
        /// Indicates if this function should search for public properties only, regardless
        /// of tryInternal as is the case in XamlLoad sceanrios.
        ///</param>
        ///<param name="isInternal">
        /// Indicates if this fucntion actually found a non-public property
        ///</param>
        ///<returns>
        /// Returns a PropertyInfo for the property
        ///</returns>
        private PropertyInfo PropertyInfoFromName(
            string localName,
            Type   ownerType,
            bool tryInternal,
            bool tryPublicOnly,
            out bool isInternal)
        {
            PropertyInfo info = null;
            isInternal = false;
            TypeInformationCacheData typeInfo = GetCachedInformationForType(ownerType);
            Debug.Assert(typeInfo != null, "Must have cached type info at this point");
            PropertyAndType propAndType = typeInfo.GetPropertyAndType(localName);

            // peterost - This is a TEMPORARY workaround to
            //    properties that have been overridden in some classes using the
            //    "new" operator.  This should be removed when properties such as
            //    Window.Width and ColumnDefintion.Width are rationalized.
            if (propAndType == null || !propAndType.PropInfoSet)
            {
                try
                {
                    BindingFlags flags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public;
#if PBTCOMPILER
                    if (tryInternal)
                    {
                        flags |= BindingFlags.NonPublic;
                    }
                    info = ownerType.GetProperty(localName, flags);
#else
                    if (!tryInternal)
                    {
                        info = ownerType.GetProperty(localName, flags);
                    }

                    // Xaml load scenarios should not look for internals - tryPublicOnly == true
                    // If the above load failed then try again looking at internals, unless
                    // tryPublicOnly tells us not to do that.
                    if (info == null && !tryPublicOnly)
                    {
                        info = ownerType.GetProperty(localName, flags | BindingFlags.NonPublic);
                        if (info != null)
                        {
                            isInternal = true;
                        }
                    }
#endif
                }
                catch (AmbiguousMatchException)
                {
                    PropertyInfo[] infos = ownerType.GetProperties(
                                  BindingFlags.Instance | BindingFlags.Public);
                    for (int i = 0; i < infos.Length; i++)
                    {
                        if (infos[i].Name == localName)
                        {
                            info = infos[i];
                            break;
                        }
                    }
                }
#if PBTCOMPILER
                if (tryInternal || info != null)
                {
#endif
                    typeInfo.SetPropertyAndType(localName, info, ownerType, isInternal);
#if PBTCOMPILER
                }
#endif
            }
            else
            {
                info = propAndType.PropInfo;
                isInternal = propAndType.IsInternal;
            }

            return info;
        }

#if !PBTCOMPILER

        ///<summary>
        /// Returns a RoutedEvent from an attribute name
        ///</summary>
        ///<param name="localName">
        /// The attribute name
        ///</param>
        ///<param name="ownerType">
        /// The Type that owns the attribute
        ///</param>
        ///<returns>
        /// Returns a RoutedEvent for the name
        ///</returns>
        internal RoutedEvent RoutedEventFromName(
            string localName,
            Type   ownerType)
        {
            RoutedEvent Event = null;
            Type currentType = ownerType;

            // Force load the Statics by walking up the hierarchy and running class constructors
            while (null != currentType)
            {
                MS.Internal.WindowsBase.SecurityHelper.RunClassConstructor(currentType);
                currentType = GetCachedBaseType(currentType);
            }

            // EventManager takes care of going up the hierarchy, so we don't need to loop here
            // to do it.  Just do one call.
            Event =  EventManager.GetRoutedEventFromName(localName,ownerType);

            return Event;
       }
#endif

        /// <summary>
        /// Given an object that is either a PropertyInfo, MethodInfo for the static
        /// setter, or DependencyProperty, return the type of the property.  This assumes
        /// the Avalon rule that a DependencyProperty has a static setter where the
        /// second parameter gives the type of the property.
        /// </summary>
        internal static Type GetPropertyType(object propertyMember)
        {
            Type propertyType;
            bool propertyCanWrite;
            GetPropertyType(propertyMember, out propertyType, out propertyCanWrite);

            return propertyType;
        }

        /// <summary>
        /// Given an object that is either a PropertyInfo, MethodInfo for the static
        /// setter, or DependencyProperty, return the type of the property.  This assumes
        /// the Avalon rule that a DependencyProperty has a static setter where the
        /// second parameter gives the type of the property.
        /// </summary>
        internal static void GetPropertyType(
            object propertyMember,
        out Type   propertyType,
        out bool   propertyCanWrite)
        {
#if !PBTCOMPILER
            DependencyProperty dp = propertyMember as DependencyProperty;
            if (dp != null)
            {
                propertyType = dp.PropertyType;
                propertyCanWrite = !dp.ReadOnly;
            }
            else
            {
#endif
                PropertyInfo propertyInfo = propertyMember as PropertyInfo;
                if (propertyInfo != null)
                {
                    propertyType = propertyInfo.PropertyType;
                    propertyCanWrite = propertyInfo.CanWrite;
                }
                else
                {
                    MethodInfo methodInfo = propertyMember as MethodInfo;
                    if (methodInfo != null)
                    {
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        propertyType = parameters.Length == 1 ? methodInfo.ReturnType : parameters[1].ParameterType;
                        propertyCanWrite = parameters.Length == 1 ? false : true;
                    }
                    else
                    {
                        // If its not a propertyinfo, methodinfo, or dependencyproperty,
                        // all we know is that it must be an object...
                        propertyType = typeof(object);
                        propertyCanWrite = false;
                    }
                }
#if !PBTCOMPILER
            }
#endif
        }


        /// <summary>
        /// Given an object that is either a PropertyInfo, MethodInfo for the static
        /// setter, or DependencyProperty, return the name of the property.  This assumes
        /// the Avalon rule that a DependencyProperty has a static getter "Get" + name.
        /// </summary>
        internal static string GetPropertyName(object propertyMember)
        {
#if !PBTCOMPILER
            DependencyProperty dp = propertyMember as DependencyProperty;
            if (dp != null)
            {
                return dp.Name;
            }
#endif

            PropertyInfo propertyInfo = propertyMember as PropertyInfo;
            if (propertyInfo != null)
            {
                return propertyInfo.Name;
            }
            else
            {
                MethodInfo methodInfo = propertyMember as MethodInfo;
                if (methodInfo != null)
                {
                    return methodInfo.Name.Substring("Get".Length);
                }
            }

            return null;
        }

        /// <summary>
        /// Given an object that is either a PropertyInfo, MethodInfo for the static
        /// setter, or DependencyProperty, return the type of the object that declares
        /// or owns this property.
        /// </summary>
        internal static Type GetDeclaringType(object propertyMember)
        {
            Type validType = null;
            MemberInfo memInfo = propertyMember as MemberInfo;
            if (memInfo != null)
            {
                validType = memInfo.DeclaringType;
            }
            else
            {
#if !PBTCOMPILER
                Debug.Assert( propertyMember is DependencyProperty);
                validType = ((DependencyProperty)propertyMember).OwnerType;
#endif
            }

            return validType;
        }
#endregion Properties

#region Types

#if !PBTCOMPILER
        /// <summary>
        ///  Return the type that corresponds to typeName, given that the type is
        ///  located as a subelement or property on the passed element.
        /// </summary>
        /// <param name="typeName">
        ///   The full xaml name of a type, including an xml namespace prefix, if needed.
        ///   The name is of the form prefix:typename, such as MyNs:MyNewButton
        /// </param>
        /// <param name="element">
        ///   A DependencyObject that is logical parent for the type to be resolved.  This
        ///   is required because it is this element (or its ancestors) that contains
        ///   namespace mapping data that is needed to resolve the typeName.
        /// </param>
        /// <returns>
        ///  The resolved clr type.  Null if not found
        /// </returns>
        internal static Type GetTypeFromName(string typeName, DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException( "element" );
            }
            if (typeName == null)
            {
                throw new ArgumentNullException( "typeName" );
            }

            // Now map the prefix to an xml namespace uri
            int colonIndex = typeName.IndexOf(':');
            string prefix = string.Empty;
            if (colonIndex > 0)
            {
                prefix = typeName.Substring(0, colonIndex);
                typeName = typeName.Substring(colonIndex+1, typeName.Length-colonIndex-1);
            }

            // First, get the xmlns dictionary to map prefixes to xml namespace uris
            XmlnsDictionary prefixDictionary = element.GetValue(XmlAttributeProperties.XmlnsDictionaryProperty)
                                               as XmlnsDictionary;

            object xmlNamespaceObject = (prefixDictionary != null) ? prefixDictionary[prefix] : null;

            // Then get the list of NamespaceMapEntry objects that maps the xml namespace uri to one
            // or more clr namespace / assembly pairs.  This should be stored on the root element
            // of the tree.
            Hashtable namespaceMaps = element.GetValue(XmlAttributeProperties.XmlNamespaceMapsProperty)
                                               as Hashtable;

            NamespaceMapEntry[] namespaces = (namespaceMaps != null && xmlNamespaceObject != null) ? namespaceMaps[xmlNamespaceObject] as NamespaceMapEntry[] : null;

            if (namespaces == null)
            {
                //           If the namespace prefix is empty, we may not have
                //           loaded the default namespace.  Get this from
                //           the token reader.  NOTE:  This is a temporary stopgap
                //           until we figure out exact requirements for namespace
                //           resolution as it applies to styles, etc.
                if (prefix == string.Empty)
                {
                    List<ClrNamespaceAssemblyPair> namespaceAssemblyPair = GetClrNamespacePairFromCache(XamlReaderHelper.DefaultNamespaceURI);
                    foreach (ClrNamespaceAssemblyPair usd in namespaceAssemblyPair)
                    {
                        if (usd.AssemblyName != null)
                        {
                            Assembly assy = ReflectionHelper.LoadAssembly(usd.AssemblyName, null);
                            if (assy != null)
                            {
                                string fullTypeName = String.Format(TypeConverterHelper.InvariantEnglishUS, "{0}.{1}", usd.ClrNamespace, typeName);
                                Type t = assy.GetType(fullTypeName);
                                if (t != null)
                                    return t;
                            }
                        }
                    }
                }

                // Stopgap didn't work, so fail now.
                return null;
            }

            // Check all the clr namespace / assembly pairs to see if there is a type
            // that matches the passed short name.  Return it if found.
            for (int i = 0; i < namespaces.Length; i++)
            {
                Assembly assy = namespaces[i].Assembly;
                if (assy != null)
                {
                    string fullTypeName = String.Format(TypeConverterHelper.InvariantEnglishUS, "{0}.{1}", namespaces[i].ClrNamespace, typeName);
                    Type t = assy.GetType(fullTypeName);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }

            // Didn't find a match, so return null.
            return null;
       }
#endif

        // Given a qualified member Name, returns its declaring Type and its name as a string.
        internal Type GetTargetTypeAndMember(string valueParam,
                                             ParserContext context,
                                             bool isTypeExpected,
                                         out string memberName)
        {
            string typeName = valueParam;
            string prefix = String.Empty;
            int typeIndex = typeName.IndexOf(':');
            if (typeIndex >= 0)
            {
                prefix = typeName.Substring(0, typeIndex);
                typeName = typeName.Substring(typeIndex + 1);
            }

            memberName = null;
            Type targetType = null;

            typeIndex = typeName.LastIndexOf('.');
            if (typeIndex >= 0)
            {
                memberName = typeName.Substring(typeIndex + 1);
                typeName = typeName.Substring(0, typeIndex);
                string namespaceUri = context.XmlnsDictionary[prefix];
                TypeAndSerializer tas = GetTypeOnly(namespaceUri, typeName);
                if (tas != null)
                {
                    targetType = tas.ObjectType;
                }
                if (targetType == null)
                {
                    ThrowException(SRID.ParserNoType, typeName);
                }
            }
            else if (!isTypeExpected && prefix.Length == 0)
            {
                // A Type may not be expected for e.g. for TemplateBinding param
                // values. In this case there must also not be a prefix. If there
                // is one it will just point to a namespace and not a Type.
                memberName = typeName;
            }
            else
            {
                // A type was expected but we didn't find one. So throw.
                ThrowException(SRID.ParserBadMemberReference, valueParam);
            }


            return targetType;
        }

        // Given a qualified member Name, returns its declaring Type and its name as a string.
        internal Type GetDependencyPropertyOwnerAndName(string memberValue,
                                                        ParserContext context,
                                                        Type defaultTargetType,
                                                    out string memberName)
        {
            Type targetType = GetTargetTypeAndMember(memberValue, context, false, out memberName);
            if (targetType == null)
            {
                targetType = defaultTargetType;
                if (targetType == null)
                {
                    // if there was also no default target type then throw.
                    ThrowException(SRID.ParserBadMemberReference, memberValue);
                }
            }

            Debug.Assert(memberName != null);
            string fieldName = memberName + "Property";
            MemberInfo memberInfo = GetStaticMemberInfo(targetType, fieldName, true);
            Debug.Assert(memberInfo != null);

            // Need to get the actual type that declares the DP in order to
            // correctly find a possible hit in the KnownProperties table.
            if (memberInfo.DeclaringType != targetType)
            {
                targetType = memberInfo.DeclaringType;
            }

            return targetType;
        }

        // Gets the PropertyInfo or FieldInfo of a member.
        internal MemberInfo GetStaticMemberInfo(Type targetType, string memberName, bool fieldInfoOnly)
        {
            Debug.Assert(targetType != null);
            // first try public members only.
            MemberInfo mi = GetStaticMemberInfo(targetType, memberName, fieldInfoOnly, false);

#if PBTCOMPILER
            if (mi == null)
            {
                // If not found, then try non-public members next.
                mi = GetStaticMemberInfo(targetType, memberName, fieldInfoOnly, true);
                if (mi != null)
                {
                    // if found, check access levels to see if it should really be allowed.
                    bool isAllowed = false;
                    PropertyInfo pi = mi as PropertyInfo;
                    if (pi != null)
                    {
                        isAllowed = IsAllowedPropertyGet(pi, false);
                    }
                    else
                    {
                        isAllowed = IsAllowedField(mi as FieldInfo);
                    }

                    if (!isAllowed)
                    {
                        ThrowException(SRID.ParserStaticMemberNotAllowed, memberName, targetType.Name);
                    }
                }
            }
#endif
            if (mi == null)
            {
                ThrowException(SRID.ParserInvalidStaticMember, memberName, targetType.Name);
            }

            return mi;
        }

        private MemberInfo GetStaticMemberInfo(Type targetType, string memberName, bool fieldInfoOnly, bool tryInternal)
        {
            MemberInfo memberInfo = null;
            BindingFlags bf = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Static;

            if (tryInternal)
            {
                bf |= BindingFlags.NonPublic;
            }

            if (!fieldInfoOnly)
            {
                memberInfo = targetType.GetProperty(memberName, bf);
            }

            if (memberInfo == null)
            {
                memberInfo = targetType.GetField(memberName, bf);
            }

            return memberInfo;
        }

        /// <summary>
        /// Get the Type that type corresponds to a localName in
        /// the given namespace.  Note that localName can be of the form classname.typename,
        /// and can not include a namespace prefix.
        /// </summary>
        /// <remarks>
        /// Return the actual type and the serializer for that type as a
        /// TypeAndSerializer object from the cache.  Note that in this call
        /// the Serializer information may not be present if a new TypeAndSerializer
        /// instance is created.  Be sure to check the IsSerializerTypeSet flag.
        /// </remarks>
        internal TypeAndSerializer GetTypeOnly (
                string    xmlNamespace,     // xml namespace for the type
                string    localName)        // local name of the type without any '.'
        {
            Debug.Assert(null != xmlNamespace,"null value passed for xmlNamespace");
            Debug.Assert(null != localName,"null value passed for localName");

            // check if object is in the Hash.
            String hashString = xmlNamespace + ":" + localName;

            TypeAndSerializer typeAndSerializer =
                 _typeLookupFromXmlHashtable[hashString] as TypeAndSerializer;

            if (null == typeAndSerializer)
            {
                if (!_typeLookupFromXmlHashtable.Contains(hashString))
                {
                    typeAndSerializer = CreateTypeAndSerializer(xmlNamespace, localName);
                    _typeLookupFromXmlHashtable[hashString] = typeAndSerializer;
                }
            }

            return typeAndSerializer;
        }

        /// <summary>
        /// Get the Type and serializer for that type corresponding to a localName in
        /// the given namespace.  Note that localName can be of the form classname.typename,
        /// and can not include a namespace prefix.
        /// </summary>
        /// <remarks>
        /// Return the actual type and the serializer for that type as a
        /// TypeAndSerializer object from the cache.  If there is no
        /// serializer defined, as is the case for clr objects, then null is set
        /// for the SerializerType in the returned TypeAndSerializer. Also if this type is
        /// associated with a property we will check for serializer attributes on the property first.
        /// If no matching attribute is found for the property we resort to search for the attribute on the given type.
        /// </remarks>
        internal TypeAndSerializer GetTypeAndSerializer (
                string    xmlNamespace,     // xml namespace for the type
                string    localName,        // local name of the type without any '.'
                object    dpOrPiorMi)       // property associated with the type
        {
            Debug.Assert(null != xmlNamespace,"null value passed for xmlNamespace");
            Debug.Assert(null != localName,"null value passed for localName");

            // check if object is in the Hash.
            String hashString = xmlNamespace + ":" + localName;

            TypeAndSerializer typeAndSerializer =
                _typeLookupFromXmlHashtable[hashString] as TypeAndSerializer;

            if (null == typeAndSerializer)
            {
                if (!_typeLookupFromXmlHashtable.Contains(hashString))
                {
                    typeAndSerializer = CreateTypeAndSerializer(xmlNamespace, localName);
                    _typeLookupFromXmlHashtable[hashString] = typeAndSerializer;
                }
            }

            // If we've found a TypeAndSerializer, check whether we have reflected for the
            // serializer information yet.  If not, then do it now.
            if (typeAndSerializer != null && !typeAndSerializer.IsSerializerTypeSet)
            {
                // Check for SerializerAttribute on the type. The serializer for the type is evaluated
                // the very first time we create a new data strcuture for the given type.
                typeAndSerializer.SerializerType = GetXamlSerializerForType(typeAndSerializer.ObjectType);
                typeAndSerializer.IsSerializerTypeSet = true;
            }

            return typeAndSerializer;
        }

#if PBTCOMPILER
        private static bool IsFriendAssembly(Assembly assembly)
        {
            // WinFx assemblies can never be friends of compiled assemblies, so just bail out.
            if (assembly == XamlTypeMapper.AssemblyPF ||
                assembly == XamlTypeMapper.AssemblyPC ||
                assembly == XamlTypeMapper.AssemblyWB)
            {
                return false;
            }

            return ReflectionHelper.IsFriendAssembly(assembly);
        }

        private static bool IsInternalAllowedOnType(Type type)
        {
            bool isInternalAllowed = ReflectionHelper.LocalAssemblyName == type.Assembly.GetName().Name ||
                                     IsFriendAssembly(type.Assembly);
            _hasInternals = _hasInternals || isInternalAllowed;
            return isInternalAllowed;
        }

        private static bool IsInternalAllowedOnType(NamespaceMapEntry namespaceMap)
        {
            bool isInternalAllowed = namespaceMap.LocalAssembly || IsFriendAssembly(namespaceMap.Assembly);
            _hasInternals = _hasInternals || isInternalAllowed;
            return isInternalAllowed;
        }

        internal static bool HasInternals
        {
            get { return _hasInternals; }
            set { _hasInternals = value; }
        }

        internal static bool HasLocalReference
        {
            get
            {
                return _hasLocalReference;
            }
        }
#endif

        /// <summary>
        /// Create a TypeAndSerializer object for the passed data, if a valid Type
        /// is found.
        /// </summary>
        private TypeAndSerializer CreateTypeAndSerializer(
                string    xmlNamespace,     // xml namespace for the type
                string    localName)        // local name of the type without any '.'
        {
            TypeAndSerializer typeAndSerializer = null;
            NamespaceMapEntry[] namespaceMaps = GetNamespaceMapEntries(xmlNamespace);

            if (namespaceMaps != null)
            {
                // We'll do a first pass with only known types
                // and then do a second pass with full reflection
                bool knownTypesOnly = true;
                for (int count = 0; count < namespaceMaps.Length;)
                {
                    NamespaceMapEntry namespaceMap = namespaceMaps[count];
                    if (null != namespaceMap)
                    {
                        Type objectType = GetObjectType(namespaceMap, localName, knownTypesOnly);
                        if (null != objectType)
                        {
                            // A non-public type is never allowable, except for internal types
                            // in local or friend assemblies, so catch it here.
                            if (!ReflectionHelper.IsPublicType(objectType))
                            {
#if PBTCOMPILER
                                // Don't allow internal known types in case we have any.
                                if (knownTypesOnly ||
                                    !ReflectionHelper.IsInternalType(objectType) ||
                                    !IsInternalAllowedOnType(namespaceMap))
#else
                                // Give Full Trust callers a chance to resolve legitimate internal types.
                                // This can be used by tools like designers and localizers running in FT.
                                if (!IsInternalTypeAllowedInFullTrust(objectType))
#endif
                                {
                                    ThrowException(SRID.ParserPublicType, objectType.Name);
                                }
                            }
                            // Create new data structure to store information for the current type
                            typeAndSerializer = new TypeAndSerializer();
                            typeAndSerializer.ObjectType = objectType;

                            break;
                        }
                    }

                    count++;
                    if (knownTypesOnly && (count == namespaceMaps.Length))
                    {
                        // Reset for second pass
                        knownTypesOnly = false;
                        count = 0;
                    }
                }
            }
            return typeAndSerializer;
        }


        /// <summary>
        /// Return the Type that corresponds to a localName within a given clr namespace
        /// in a given assembly
        /// </summary>
        /// <param name="namespaceMap">Specifies the CLR namespace and Assembly to look in</param>
        /// <param name="localName">The name of the type</param>
        /// <param name="knownTypesOnly">Search only known types</param>
        /// <returns>Type of object that corresponds to localName</returns>
        private Type GetObjectType(
            NamespaceMapEntry namespaceMap,
            string            localName,
            bool              knownTypesOnly)
        {
            Debug.Assert(namespaceMap.ClrNamespace != null,"Null CLR Namespace");
            Debug.Assert(localName != null,"Null localName");

            Type type = null;

            // Construct the full type name by concatenating the clr namespace and the local name

            if (knownTypesOnly)
            {
                short typeID = BamlMapTable.GetKnownTypeIdFromName(namespaceMap.AssemblyName, namespaceMap.ClrNamespace, localName);
                if (typeID != 0)
                {
                    type = BamlMapTable.GetKnownTypeFromId(typeID);
                }
            }
            else
            {
               Assembly assembly ;
#if PBTCOMPILER
                try
                {
#endif
                    // This may not work if the assembly is not present at compile time, so allow for that.
                    // At runtime it had better be there, so don't ignore it.
                    assembly = namespaceMap.Assembly;
#if PBTCOMPILER
                }
                catch (FileNotFoundException)
                {
                    assembly = null;
                }
#endif

                if (null != assembly)
                {
                    // Type loads may fail if all the prerequisite assemblies haven't been loaded
                    // yet.  In this case, try one more time after loaded all assemblies that the
                    // compiler may have told the XamlTypeMapper about.
                    string fullTypeName = namespaceMap.ClrNamespace + "." + localName;
                    try
                    {
                        type = assembly.GetType(fullTypeName);
                    }
                    catch (Exception e)
                    {
                        if (CriticalExceptions.IsCriticalException(e))
                        {
                           throw;
                        }
                        else
                        {
                            if (LoadReferenceAssemblies())
                            {
                                try
                                {
                                    type = assembly.GetType(fullTypeName);
                                }
                                catch (ArgumentException)
                                {
                                    // A null type is allowable, and the caller will catch it
                                    type = null;
                                }
                            }
                        }
                    }
                }
            }

            return type;
        }

        internal int GetCustomBamlSerializerIdForType(Type objectType)
        {
            // support for xaml -> custom binary and custom binary -> object.
            if (objectType == KnownTypes.Types[(int)KnownElements.Brush])
            {
                return (int)KnownElements.XamlBrushSerializer;
            }
            else if (objectType == KnownTypes.Types[(int)KnownElements.Geometry] ||
                     objectType == KnownTypes.Types[(int)KnownElements.StreamGeometry])
            {
                //
                // The only type of geometry that can be serialized to a string is
                // a StreamGeometry, so if objectType is Geometry and we're getting here
                // then the attribute must in fact be a StreamGeometry.
                //
                return (int)KnownElements.XamlPathDataSerializer;
            }
            else if (objectType == KnownTypes.Types[(int)KnownElements.Point3DCollection])
            {
                return (int)KnownElements.XamlPoint3DCollectionSerializer;
            }
            else if (objectType == KnownTypes.Types[(int)KnownElements.Vector3DCollection])
            {
                return (int)KnownElements.XamlVector3DCollectionSerializer;
            }
            else if (objectType == KnownTypes.Types[(int)KnownElements.PointCollection])
            {
                return (int)KnownElements.XamlPointCollectionSerializer;
            }
            else if (objectType == KnownTypes.Types[(int)KnownElements.Int32Collection])
            {
                return (int)KnownElements.XamlInt32CollectionSerializer;
            }

            return 0;
        }

        /// <summary>
        /// Sees if type has a Xaml serializer attribute that gives the
        /// type name of the serializer and figure out the type.
        /// </summary>
        internal Type GetXamlSerializerForType(Type objectType)
        {
            // support for xaml -> baml and baml -> objects
            if (objectType == KnownTypes.Types[(int)KnownElements.Style])
            {
                return typeof(XamlStyleSerializer);
            }
            else if (KnownTypes.Types[(int)KnownElements.FrameworkTemplate].IsAssignableFrom(objectType))
            {
                return typeof(XamlTemplateSerializer);
            }

            return null;
        }

#if !PBTCOMPILER
        internal static Type GetInternalTypeHelperTypeFromAssembly(ParserContext pc)
        {
            Assembly a = pc.StreamCreatedAssembly;
            if (a == null)
            {
                return null;
            }

            Type ithType = a.GetType(GeneratedNamespace + "." + GeneratedInternalTypeHelperClassName);
            if (ithType == null)
            {
                // if GITH is not found, try to see if a root namespace was implicitly added to it.
                // This would be the case for assemblies built with VB that had a RootNamespace
                // property specified in the project file.
                RootNamespaceAttribute rnsa = (RootNamespaceAttribute)Attribute.GetCustomAttribute(a, typeof(RootNamespaceAttribute));
                if (rnsa != null)
                {
                    string rootNamespace = rnsa.Namespace;
                    ithType = a.GetType(rootNamespace + "." + GeneratedNamespace + "." + GeneratedInternalTypeHelperClassName);
                }
            }

            return ithType;
        }
        
        private static InternalTypeHelper GetInternalTypeHelperFromAssembly(ParserContext pc)
        {
            InternalTypeHelper ith = null;
            Type ithType = GetInternalTypeHelperTypeFromAssembly(pc);
            if (ithType != null)
            {
                ith = (InternalTypeHelper)Activator.CreateInstance(ithType);
            }
            return ith;
        }

        internal static object CreateInternalInstance(ParserContext pc, Type type)
        {
            object instance = Activator.CreateInstance(type,
                                                BindingFlags.Public |
                                                BindingFlags.NonPublic |
                                                BindingFlags.Instance |
                                                BindingFlags.CreateInstance,
                                                null,
                                                null,
                                                TypeConverterHelper.InvariantEnglishUS);

            return instance;
        }

        internal static object GetInternalPropertyValue(ParserContext pc, object rootElement, PropertyInfo pi, object target)
        {
            object propValue = null;
            bool isPublicProperty = false;
            bool allowProtected = (rootElement is IComponentConnector) && (rootElement == target);
            bool isAllowedProperty = IsAllowedPropertyGet(pi, allowProtected, out isPublicProperty);

            if (isAllowedProperty)
            {
                propValue = pi.GetValue(target, BindingFlags.Default, null, null, TypeConverterHelper.InvariantEnglishUS);
            }

            return propValue;
        }

        internal static bool SetInternalPropertyValue(ParserContext pc, object rootElement, PropertyInfo pi, object target, object value)
        {
            bool isPublicProperty = false;
            bool allowProtected = (rootElement is IComponentConnector) && (rootElement == target);
            bool isAllowedProperty = IsAllowedPropertySet(pi, allowProtected, out isPublicProperty);

            if (isAllowedProperty)
            {
                pi.SetValue(target, value, BindingFlags.Default, null, null, TypeConverterHelper.InvariantEnglishUS);
                return true;
            }

            return false;
        }

        internal static Delegate CreateDelegate(ParserContext pc, Type delegateType, object target, string handler)
        {
            Delegate d = null;
            bool isAllowedDelegateType = ReflectionHelper.IsPublicType(delegateType) || ReflectionHelper.IsInternalType(delegateType);

            if (isAllowedDelegateType)
            {
                 d = Delegate.CreateDelegate(delegateType, target, handler);
            }

            return d;
        }

        internal static bool AddInternalEventHandler(ParserContext pc, object rootElement, EventInfo eventInfo, object target, Delegate handler)
        {
            bool isPublicEvent = false;
            bool allowProtected = rootElement == target;
            bool isAllowedEvent = IsAllowedEvent(eventInfo, allowProtected, out isPublicEvent);

            if (isAllowedEvent)
            {
                eventInfo.AddEventHandler(target, handler);
                return true;
            }

            return false;
        }
#endif

        // Return true if this is a locally compiled assembly
        internal bool IsLocalAssembly(string namespaceUri)
        {
            bool localAssembly = false;
#if PBTCOMPILER
            NamespaceMapEntry[] namespaceMaps = GetNamespaceMapEntries(namespaceUri);
            localAssembly = (namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly);
#endif
            return localAssembly;
        }

        /// <summary>
        /// Given a string of the format MyNs:MyButton return the type that it refers to.  This uses
        /// the XmlnsDictionary in the passed parse context to resolve prefixes on the
        /// type names
        /// </summary>
        internal Type GetTypeFromBaseString(
            string        typeString,
            ParserContext context,
            bool          throwOnError)
        {
            string xmlns = string.Empty;
            Type keyObject = null;
            // Check if the typeString is of the form xmlns:type.  If so, get
            // the appropriate mapped NamespaceURI.  If not, use the default URI
            int colonIndex = typeString.IndexOf(':');
            if (colonIndex == -1)
            {
                xmlns = context.XmlnsDictionary[string.Empty];
                if (xmlns == null)
                {
                    ThrowException(SRID.ParserUndeclaredNS, string.Empty);
                }
            }
            else
            {
                string prefix = typeString.Substring(0, colonIndex);
                xmlns = context.XmlnsDictionary[prefix];
                if (xmlns == null)
                {
                    ThrowException(SRID.ParserUndeclaredNS, prefix);
                }
                else
                {
                    typeString = typeString.Substring(colonIndex + 1);
                }
            }

#if !PBTCOMPILER
            // Optimize for SystemMetric types that are very frequently used.
            if (string.CompareOrdinal(xmlns, XamlReaderHelper.DefaultNamespaceURI) == 0)
            {
                switch (typeString)
                {
                    case "SystemParameters":
                        keyObject = typeof(SystemParameters);
                        break;

                    case "SystemColors":
                        keyObject = typeof(SystemColors);
                        break;

                    case "SystemFonts":
                        keyObject = typeof(SystemFonts);
                        break;
                }
            }
#endif
            if (keyObject == null)
            {
                keyObject = GetType(xmlns, typeString);
            }

            if (keyObject == null && throwOnError)
            {
                // if local type, then don't throw as it may be resolved in pass2.
                // it is upto the caller who has context about compilation passes
                // to throw as appropriate in Pass2 if the type is still not found.
                if (!IsLocalAssembly(xmlns))
                {
                    _lineNumber = context != null ? context.LineNumber : 0;
                    _linePosition = context != null ? context.LinePosition : 0;

                    ThrowException(SRID.ParserResourceKeyType, typeString);
                }
            }

            return keyObject;
        }

#if PBTCOMPILER
        internal Type GetTypeArgsType(
            string typeString,
            ParserContext context,
            out string localTypeArgClassName,
            out string localTypeArgNamespace)
        {
            Type t = null;
            localTypeArgClassName = string.Empty;
            localTypeArgNamespace = string.Empty;

            // Check if the typeString is of the form xmlns:type.  If so, get
            // the appropriate mapped NamespaceURI.  If not, use the default URI
            int colonIndex = typeString.IndexOf(':');
            if (colonIndex == -1)
            {
                string xmlns = context.XmlnsDictionary[string.Empty];
                if (xmlns == null)
                {
                    ThrowException(SRID.ParserUndeclaredNS, string.Empty);
                }
                else
                {
                    t = GetType(xmlns, typeString);
                }
            }
            else
            {
                string prefix = typeString.Substring(0, colonIndex);
                string xmlns = context.XmlnsDictionary[prefix];
                if (xmlns == null)
                {
                    ThrowException(SRID.ParserUndeclaredNS, prefix);
                }
                else
                {
                    NamespaceMapEntry[] namespaceMaps = GetNamespaceMapEntries(xmlns);
                    typeString = typeString.Substring(colonIndex + 1);
                    bool isLocalArg = namespaceMaps != null &&
                                      namespaceMaps.Length == 1 &&
                                      namespaceMaps[0].LocalAssembly;

                    if (isLocalArg)
                    {
                        localTypeArgNamespace = namespaceMaps[0].ClrNamespace;
                        localTypeArgClassName = typeString;
                    }
                    else
                    {
                        t = GetType(xmlns, typeString);
                    }
                }
            }

            return t;
        }
#endif

        /// <summary>
        /// Helper method given a type returns the Cached information for the type.  If there
        /// is no existing cached information for that type, a new cache object is created
        /// and added to the cache.
        /// </summary>
        private TypeInformationCacheData GetCachedInformationForType(Type type)
        {
            TypeInformationCacheData typeInformationCacheData;
            typeInformationCacheData = _typeInformationCache[type] as TypeInformationCacheData;
            if (null == typeInformationCacheData)
            {
                typeInformationCacheData = new TypeInformationCacheData(type.BaseType);
                typeInformationCacheData.ClrNamespace = type.Namespace;

                _typeInformationCache[type] = typeInformationCacheData;
            }

            return typeInformationCacheData;
        }

#if !PBTCOMPILER

        /// <summary>
        /// Returns the type's BaseType. This is cached because the .net Type.BaseType()
        /// call is expensive.
        /// </summary>
        private Type GetCachedBaseType(Type t)
        {
            TypeInformationCacheData typeInformation = GetCachedInformationForType(t);

            return typeInformation.BaseType;
        }

        /***************************************************************************\
        *
        * XamlTypeMapper.ProcessNameString
        *
        * Given a name in markup, extract and process the prefix if any.
        *
        \***************************************************************************/

        internal static string ProcessNameString(ParserContext parserContext, ref string nameString)
        {
            // For certain parts of the styling markup, a property is specified as an
            //  attribute value.  This may include the namespace specification.  Normally
            //  when we encounter this in an attribute the XML parser processes the name-
            //  space for us.  But when it's the value we'll have to deal with it our-
            //  selves.  Look for the namespace specifier and extract it for namespace lookup.
            //  "foons:BarClass.BazProp" -> "http://www.example.com" + "BarClass.BazProp"

            // The colon is what we look for to determine if there's a namespace prefix specifier.
            int nsIndex = nameString.IndexOf(':');
            string nsPrefix = string.Empty;
            if( nsIndex != -1 )
            {
                // Found a namespace prefix separator, so create replacement propertyName.
                // String processing - split "foons" from "BarClass.BazProp"
                nsPrefix = nameString.Substring (0, nsIndex);
                nameString = nameString.Substring (nsIndex + 1);
            }

            // Find the namespace, even if its the default one
            string namespaceURI = parserContext.XmlnsDictionary[nsPrefix];
            if (namespaceURI == null)
            {
                parserContext.XamlTypeMapper.ThrowException(SRID.ParserPrefixNSProperty, nsPrefix, nameString);
            }

            return namespaceURI;
        }

        /***************************************************************************\
        *
        * XamlTypeMapper.ParsePropertyName
        *
        * Given a property name, find the associated DependencyProperty and return it.
        *
        \***************************************************************************/

        internal static DependencyProperty ParsePropertyName(
                ParserContext   parserContext,
                string          propertyName,
            ref Type            ownerType)
        {
            string namespaceURI = ProcessNameString(parserContext, ref propertyName);

            DependencyProperty dp = parserContext.XamlTypeMapper.DependencyPropertyFromName(
                                            propertyName,
                                            namespaceURI,
                                            ref ownerType);

            return dp;
        }

        /***************************************************************************\
        *
        * XamlTypeMapper.ParseEventName
        *
        * Given an event name, find the associated RoutedEvent and return it.
        *
        \***************************************************************************/

        internal static RoutedEvent ParseEventName(
            ParserContext   parserContext,
            string          eventName,
            Type            ownerType)
        {
            string namespaceURI = ProcessNameString(parserContext, ref eventName);

            RoutedEvent Event = parserContext.XamlTypeMapper.GetRoutedEvent(
                ownerType, namespaceURI, eventName);

            return Event;
        }

#endif

        /// <summary>
        /// Return an object of the passed type.
        /// </summary>
        /// <remarks>
        /// If the type is a KnownElement (which it likely will be) it is faster
        /// to create it using the KnownTypes hardcoded constructors, so check that
        /// first before using the Activator.CreateInstance fallback.
        /// </remarks>
        internal object CreateInstance(Type t)
        {
            object o = null;
#if !PBTCOMPILER
            short typeId = BamlMapTable.GetKnownTypeIdFromType(t);
            if (typeId < 0)
            {
                o = MapTable.CreateKnownTypeFromId(typeId);
            }
            else
            {
#endif
                o = Activator.CreateInstance(t,
                                             BindingFlags.Instance | BindingFlags.CreateInstance | BindingFlags.Public,
                                             null,
                                             null,
                                             TypeConverterHelper.InvariantEnglishUS);
#if !PBTCOMPILER
            }
#endif
            return o;
        }

#endregion Types

#region Namespaces

        // Return true if the passed namespace is known, meaning that it maps
        // to a set of assemblies and clr namespaces
        internal bool IsXmlNamespaceKnown(
                string xmlNamespace,
            out string newXmlNamespace)
        {
            bool result;

            // if the namespace is empty, then there's no need to do all the extra work associated with getting the
            // namespace map entries.  Just return false.
            if (String.IsNullOrEmpty(xmlNamespace))
            {
                result = false;
                newXmlNamespace = null;
            }
            else
            {
                NamespaceMapEntry[] namespaceMaps = GetNamespaceMapEntries(xmlNamespace);
                if (_xmlnsCache == null)
                {
#if PBTCOMPILER
                    Debug.Assert(false, "Should initialize cache prior to compiling");
#else
                    _xmlnsCache = new XmlnsCache();
#endif
                }
                newXmlNamespace = _xmlnsCache.GetNewXmlnamespace(xmlNamespace);

                // if the xmlNamespace has valid entries or is mapped to another namespace, then it is known.
                result = (namespaceMaps != null && namespaceMaps.Length > 0) ||
                              !String.IsNullOrEmpty(newXmlNamespace);
            }

            return result; // return variable isn't needed, just makes debugging easier.
        }


#if !PBTCOMPILER

        //
        // Pass xmlNamespace to AssemblyList mapping to xmlnsCache so that the xmlnsCache
        // can take the assembly list to get the right xmlns->clrns mapping later.
        //
        internal void SetUriToAssemblyNameMapping(string xmlNamespace, short[] assemblyIds)
        {
            //
            // If the xmlNamespace is for a mapping,  there is no need to ask for xmlnsCache
            // to get xmlns->clrns mapping.
            //
            if (xmlNamespace.StartsWith(XamlReaderHelper.MappingProtocol, StringComparison.Ordinal))
            {
                return;
            }

            if (_xmlnsCache == null)
            {
                _xmlnsCache = new XmlnsCache();
            }

            string[] asmNameList = null;

            if (assemblyIds != null && assemblyIds.Length > 0)
            {
                asmNameList = new string[assemblyIds.Length];

                for (int i = 0; i < assemblyIds.Length; i++)
                {
                    BamlAssemblyInfoRecord assemblyInfo = MapTable.GetAssemblyInfoFromId(assemblyIds[i]);
                    asmNameList[i] = assemblyInfo.AssemblyFullName;
                }
            }

            _xmlnsCache.SetUriToAssemblyNameMapping(xmlNamespace, asmNameList);
        }


#endif

        ///<summary>
        /// Returns a NamespaceMapEntry array given a xmlNamespace.  Each
        /// NamespaceMapEntry contains information the XamlTypeMapper uses for Mapping between an xml
        /// XmlNamespace and what Assembly, Namespace to look in.
        ///</summary>
        ///<param name="xmlNamespace">
        /// The xmlNamespace for the Map
        ///</param>
        ///<returns>
        /// Returns an Array of NamespaceMapEntry
        ///</returns>
        internal NamespaceMapEntry[] GetNamespaceMapEntries(string xmlNamespace)
        {
            NamespaceMapEntry[] namespaceMaps = null;

            // check out hash and if already have resolved the Uri
            // don't resolve again.
            namespaceMaps = _namespaceMapHashList[xmlNamespace] as NamespaceMapEntry[];

            if (null == namespaceMaps)
            {
                ArrayList namespaceMapArray = new ArrayList(6);

                // this is the first time the XmlNamespace has been requested
                // add items from the namespaceMap and then walk the assembly
                // list to find matching items.

                if (null != _namespaceMaps)
                {
                    // need to find out the total number of entries by asking each hash
                    // review, may be better to sort these firt time through
                    // assumption here that the namespaceMap entries are small.
                    for (int i = 0; i < _namespaceMaps.Length; i++)
                    {
                        NamespaceMapEntry namespaceMap = _namespaceMaps[i];

                        if (namespaceMap.XmlNamespace == xmlNamespace)
                        {
                            namespaceMapArray.Add(namespaceMap);
                        }
                    }
                }


                List<ClrNamespaceAssemblyPair> namespaceAssemblyPair;
                // Check for Processing instructions for mapping.  If that fails,
                // check for Xmlns definition files.
                if (PITable.Contains(xmlNamespace))
                {
                    namespaceAssemblyPair = new List<ClrNamespaceAssemblyPair>(1);
                    namespaceAssemblyPair.Add((ClrNamespaceAssemblyPair)PITable[xmlNamespace]);
                }
                else
                {
                    namespaceAssemblyPair = GetClrNamespacePairFromCache(xmlNamespace);
                }

                // now walk through any using statements we got and add them
                if (null != namespaceAssemblyPair)
                {
                    for (int j = 0; j < namespaceAssemblyPair.Count; j++)
                    {
                        ClrNamespaceAssemblyPair mapping = namespaceAssemblyPair[j];
                        string          usingAssemblyName = null;
                        string          path =  AssemblyPathFor(mapping.AssemblyName);

                        // using could either have an assembly or not, if it does
                        // have an assembly just need to add this one entry, if
                        // no assembly map in the referenced assemblies.

                        if (
#if PBTCOMPILER
                            mapping.LocalAssembly ||
#endif
                            (!(String.IsNullOrEmpty(mapping.AssemblyName)) && !(String.IsNullOrEmpty(mapping.ClrNamespace))) )
                        {
                            usingAssemblyName = mapping.AssemblyName;
                            NamespaceMapEntry nsMap = new NamespaceMapEntry(xmlNamespace,
                                usingAssemblyName,mapping.ClrNamespace,path);

#if PBTCOMPILER
                            nsMap.LocalAssembly = mapping.LocalAssembly;
                            // if xaml has local components, then it could have internals
                            // but there is no way to determine this until pass2. But we
                            // need to set this here in order to generate the InternalTypeHelper
                            // in pass1.
                            if (nsMap.LocalAssembly)
                            {
                                _hasLocalReference = true;
                            }
#endif

                            namespaceMapArray.Add(nsMap);
                        }

                        if (!String.IsNullOrEmpty(mapping.ClrNamespace))
                        {
                            // Also add in any assembly from the AssemblyNames collection
                            for (int k = 0; k < _assemblyNames.Length; k++)
                            {
                                if (usingAssemblyName == null)
                                {
                                    namespaceMapArray.Add(new NamespaceMapEntry(xmlNamespace,
                                                      _assemblyNames[k],
                                                      mapping.ClrNamespace,
                                                      path));
                                }
                                else
                                {
                                    // If we have a using Assembly, then only add assemblies from
                                    // AssemblyNames that match the using Assembly.
                                    int charIndex = _assemblyNames[k].LastIndexOf('\\');
                                    if (charIndex > 0 &&
                                        _assemblyNames[k].Substring(charIndex + 1) == usingAssemblyName)
                                    {
                                        namespaceMapArray.Add(new NamespaceMapEntry(xmlNamespace,
                                                        _assemblyNames[k],
                                                        mapping.ClrNamespace,
                                                        path));
                                    }
                                }
                            }
                        }
                    }
                }

                // convert to a namespaceMap
                namespaceMaps = (NamespaceMapEntry[]) namespaceMapArray.ToArray(typeof(NamespaceMapEntry));

                // add to hash even if not items so we don't do this work again.
                if (null != namespaceMaps)
                {
                    _namespaceMapHashList.Add(xmlNamespace,namespaceMaps);
                }
            }

            return namespaceMaps;
        }


#if PBTCOMPILER

        /// <summary>
        /// Invalide the namespace mapping cache associated with a namespace. This should be done
        /// when the PITable has been changed and the mapping cache might have been built for the
        /// namespace already.
        /// </summary>
        /// <param name="xmlNamespace">The namespace to for whose cache to invalidate</param>
        internal void InvalidateMappingCache(string xmlNamespace)
        {
            _namespaceMapHashList.Remove(xmlNamespace);
        }

        /// <summary>
        /// Set up the XmlnsCache use for resolving namespaces to use only the assembly path
        /// table that has been built up by the XamlTypeMapper.
        /// </summary>
        internal void InitializeReferenceXmlnsCache()
        {
            _xmlnsCache = new XmlnsCache(_assemblyPathTable);
        }

#else
        // Get the xml namespace from the _piReverseTable that corresponds to the
        // passed type full name and assembly name
        internal string GetXmlNamespace(
            string  clrNamespaceFullName,
            string  assemblyFullName)
        {
            string upperAssemblyName = assemblyFullName.ToUpper(
                                              TypeConverterHelper.InvariantEnglishUS);

            String fullName = clrNamespaceFullName + "#" + upperAssemblyName;

            String ret;

            if (_piReverseTable.TryGetValue(fullName, out ret) && ret != null)
            {
                return ret;
            }
            else
            {
               return string.Empty;
            }
        }

        /// <summary>
        /// Given a Type returns it's .net Namespace. This is cached because each call
        /// to .urtNamspace allocates a string.
        /// </summary>
        private string GetCachedNamespace(Type t)
        {
            TypeInformationCacheData typeInformation = GetCachedInformationForType(t);

            return typeInformation.ClrNamespace;
        }
#endif

        /// <summary>
        /// Check an Xml namespace URI and loads the associated Clr namespaces and assemblies
        /// into ClrNamespaceAssemblyPair structures.
        ///  Throws an exception if no valid namespaces are found.
        /// </summary>
        /// <param name="namespaceUri">Xml namespace that maps to one or more Clr namespace
        /// and assembly pairs</param>
        /// <returns>array of ClrNamespaceAssemblyPair structures</returns>
        internal static List<ClrNamespaceAssemblyPair> GetClrNamespacePairFromCache(
                string namespaceUri)
        {
            List<ClrNamespaceAssemblyPair> mappingArray = null;

            if (_xmlnsCache == null)
            {
#if PBTCOMPILER
                Debug.Assert(false, "Should initialize cache prior to compiling");
#else
                _xmlnsCache = new XmlnsCache();
#endif
            }

            mappingArray = _xmlnsCache.GetMappingArray(namespaceUri);

            return mappingArray;
        }

#endregion Namespaces

#region TypeConverters

        // Returns the Type of the TypeConverter for the given type.
        // Returns null if not found.
        internal Type GetTypeConverterType(Type type)
        {
            Debug.Assert(null != type, "Null passed for type to GetTypeConverterType");
            TypeInformationCacheData typeData = GetCachedInformationForType(type) as TypeInformationCacheData;
            Type converterType = null;

            if (null != typeData.TypeConverterType)
            {
                converterType = typeData.TypeConverterType;
                return converterType;
            }

            // Check for known TypeConverters first. These are always public.
            converterType = MapTable.GetKnownConverterTypeFromType(type);
            if (converterType == null)
            {
                // If not found, next try looking for the TypeConverter for the type using reflection.
                converterType = TypeConverterHelper.GetConverterType(type);
                if (converterType == null)
                {
                    converterType = TypeConverterHelper.GetCoreConverterTypeFromCustomType(type);
                }
            }

            typeData.TypeConverterType = converterType;
            return converterType;
        }

#if !PBTCOMPILER
        // Returns the TypeConverter for the given type
        // Throws a XamlParseException if no TypeConverter is found.
        internal TypeConverter GetTypeConverter(Type type)
        {
            Debug.Assert(null != type, "Null passed for type to GetTypeConverter");

            TypeInformationCacheData typeData = GetCachedInformationForType(type) as TypeInformationCacheData;
            TypeConverter typeConverter = null;

            // if the TypeConverter for this type was ever successfully
            // queried before it should have a non-null value.
            if (null != typeData.Converter)
            {
                typeConverter = typeData.Converter;
                return typeConverter;
            }

            // Check for known TypeConverters first. These are always public.
            typeConverter = MapTable.GetKnownConverterFromType(type);
            if (typeConverter == null)
            {
                // If not found, next try looking for the TypeConverter for the type using reflection.
                Type converterType = TypeConverterHelper.GetConverterType(type);
                if (converterType == null)
                {
                    typeConverter = TypeConverterHelper.GetCoreConverterFromCustomType(type);
                }
                else
                {
                    typeConverter = CreateInstance(converterType) as TypeConverter;
                }
            }

            typeData.Converter = typeConverter;

            if (null == typeConverter)
            {
                ThrowException(SRID.ParserNoTypeConv, type.Name);
            }

            return typeConverter;
        }
#endif

        // Returns the Type of the TypeConverter applied to the given property itself.
        // Returns null if not found.
        internal Type GetPropertyConverterType(Type propType, object dpOrPiOrMi)
        {
            Debug.Assert(null != propType, "Null passed for propType to GetPropertyConverterType");
            Type converterType = null;

            // If the current value being parsed is a property value then we must look
            // if there is special TypeConverter specified as attribute for this property
            if (null != dpOrPiOrMi)
            {
#if PBTCOMPILER
                TypeInformationCacheData typeData = GetCachedInformationForType(propType) as TypeInformationCacheData;
                object ret = typeData.PropertyConverters[dpOrPiOrMi];
                if (null != ret)
                {
                    converterType = (Type)ret;
                    return converterType;
                }
                else
#endif
                {
                    // Get the memberInfo from the DP, PropertyInfo(CLR) or MethodInfo(Attached) for the property
                    MemberInfo memberInfo = TypeConverterHelper.GetMemberInfoForPropertyConverter(dpOrPiOrMi);
                    if (memberInfo != null)
                    {
                        // If not found, next try looking for the TypeConverter on the property itself using reflection.
                        converterType = TypeConverterHelper.GetConverterType(memberInfo);
                    }
                }

#if PBTCOMPILER
                // Cache the per property TypeConverter type. This doesn't need to be cached at run-time
                // since GetPropertyConverter will be used for getting the actual TypeConveretr instance.
                // This saves us the cost of a HashTable.
                typeData.SetPropertyConverter(dpOrPiOrMi, converterType);
#endif
            }

            return converterType;
        }

#if !PBTCOMPILER
        // Returns the TypeConverter applied to the given property iself.
        // If not found returns the TypeConverter applied to the given property's type.
        // Throws a XamlParseException if no TypeConverter is found.
        internal TypeConverter GetPropertyConverter(Type propType, object dpOrPiOrMi)
        {
            Debug.Assert(null != propType, "Null passed for propType to GetPropertyConverter");
            TypeConverter typeConverter = null;

            TypeInformationCacheData typeData = GetCachedInformationForType(propType) as TypeInformationCacheData;

            // If the current value being parsed is a property value then we must look
            // if there is special TypeConverter specified as attribute for this property
            if (null != dpOrPiOrMi)
            {
                object ret = typeData.PropertyConverters[dpOrPiOrMi];
                if (null != ret)
                {
                    typeConverter = (TypeConverter)ret;
                    return typeConverter;
                }

                // Get the memberInfo from the DP, PropertyInfo(CLR) or MethodInfo(Attached) for the property
                MemberInfo memberInfo = TypeConverterHelper.GetMemberInfoForPropertyConverter(dpOrPiOrMi);
                if (memberInfo != null)
                {
                    // If not found, next try looking for the TypeConverter on the property itself using reflection.
                    Type converterType = TypeConverterHelper.GetConverterType(memberInfo);
                    if (converterType != null)
                    {
                        // create an instance of the TypeConverter if found
                        typeConverter = CreateInstance(converterType) as TypeConverter;
                    }
                }
            }

            // If no TypeConverter is found on the property itself, try to find it based on the property's type
            if (typeConverter == null)
            {
                typeConverter = GetTypeConverter(propType);
            }

            // Cache the per property TypeConverter
            if (dpOrPiOrMi != null)
                typeData.SetPropertyConverter(dpOrPiOrMi, typeConverter);

            return typeConverter;
        }
#endif

#endregion TypeConverters

#region Resources

        /// <summary>
        /// Given a string, return a key to be used in a dictionary relating to this string.
        /// If the string is a simple string, just return that.  If the string represents a
        /// Typeof declaration, then return the Type that maps to that string.
        /// Note that this can return null if the key is not resolved.  It is up to the caller
        /// to throw the appropriate error message in that case.
        /// </summary>
        internal object GetDictionaryKey(string keyString, ParserContext context)
        {
             if (keyString.Length > 0 &&
                 (Char.IsWhiteSpace(keyString[0]) ||
                  Char.IsWhiteSpace(keyString[keyString.Length-1])))
             {
                keyString = keyString.Trim();
             }
             return keyString;
        }

#endregion Resources

#if !PBTCOMPILER

#region ConstructorInfos

        /// <summary>
        /// Fetches the cached ConstructorInfos if there exists one or
        /// then creates a new one and caches it for later.
        /// </summary>
        internal ConstructorData GetConstructors(Type type)
        {
            // Create a cache if it does not already exist
            if (_constructorInformationCache == null)
            {
                _constructorInformationCache = new HybridDictionary(3);
            }

            // Add an entry for the current type if it does not already exist
            if (!_constructorInformationCache.Contains(type))
            {
                _constructorInformationCache[type] = new ConstructorData(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            }

            // Return the cached value
            return (ConstructorData)_constructorInformationCache[type];
        }

        /// <summary>
        /// Helper class that is used to store constructor information for a type
        /// </summary>
        internal class ConstructorData
        {
            #region Constructors

            internal ConstructorData(ConstructorInfo[] constructors)
            {
                _constructors = constructors;
            }

            #endregion Constructors

            #region Methods

            /// <summary>
            /// Fetches the cached ParameterInfos if there exists one or
            /// then creates a new one and caches it for later.
            /// </summary>
            internal ParameterInfo[] GetParameters(int constructorIndex)
            {
                // Create a parameters Cache if it does not already exist
                if (_parameters == null)
                {
                    Debug.Assert(_constructors != null, "This operation is legal only after the constructors have been fetched");
                    _parameters = new ParameterInfo[_constructors.Length][];
                }

                // Add an entry for the current constructor if it does not already exist
                if (_parameters[constructorIndex] == null)
                {
                    _parameters[constructorIndex] = _constructors[constructorIndex].GetParameters();
                }

                // Return the cached value
                return _parameters[constructorIndex];
            }

            #endregion Methods

            #region Properties

            internal ConstructorInfo[] Constructors
            {
                get { return _constructors; }
            }

            #endregion Properties

            #region Data

            private ConstructorInfo[] _constructors;
            private ParameterInfo[][] _parameters;

            #endregion Data
        }

#endregion ConstructorInfos

#endif

#region TrimSurroundingWhitespace

        /// <summary>
        /// Returns the Cached TrimSurroundingWhitespace for the associated type
        /// We cache this because the reflection lookup on the attribute
        /// is slow.
        /// </summary>
        /// <param name="t">type to return the TrimSurroundingWhitespaceAttribute for</param>
        /// <returns>The TrimSurroundingWhitespace value for t</returns>
        internal bool GetCachedTrimSurroundingWhitespace(Type t)
        {
            TypeInformationCacheData typeInformation = GetCachedInformationForType(t);

            // if first time asked for the layout Type get it.
            if (!typeInformation.TrimSurroundingWhitespaceSet)
            {
                typeInformation.TrimSurroundingWhitespace = GetTrimSurroundingWhitespace(t);
                typeInformation.TrimSurroundingWhitespaceSet = true;
            }

            return typeInformation.TrimSurroundingWhitespace;
        }

        /// <summary>
        ///  Helper function for  use to find out the TrimSurroundingWhitespace
        ///  associated with a Type.
        /// </summary>
        private bool GetTrimSurroundingWhitespace(Type type)
        {
            Debug.Assert(null != type, "null value for type passed to GetWhitespace");

            // in retail return default.
            if (null != type)
            {
#if !PBTCOMPILER
                TrimSurroundingWhitespaceAttribute[] trimAttribute =
                    type.GetCustomAttributes(typeof(TrimSurroundingWhitespaceAttribute),true )
                    as TrimSurroundingWhitespaceAttribute[];

                if (trimAttribute.Length > 0)
                {
                    Debug.Assert(1 == trimAttribute.Length,"More than one TrimWhitespace Attribute");
                    return true;
                }
#else
                // Reflecting for attributes doesn't work on asmmeta files, so
                // we have to hard code known layout types here.  This is very
                // fragile, but we'll fix it in M8.2. - It's end of M11 already though...
                if (KnownTypes.Types[(int)KnownElements.LineBreak].IsAssignableFrom(type))
                {
                    return true;
                }
#endif
            }
            return false;
        }

#endregion TrimSurroundingWhitespace

#region Exceptions

        //
        // ThrowException wrappers for 0-3 parameter SRIDs
        //

        private void ThrowException(string id)
        {
            ThrowExceptionWithLine(SR.Get(id), null);
        }

        internal void ThrowException(string id, string parameter)
        {
            ThrowExceptionWithLine(SR.Get(id, parameter), null);
        }

        private void ThrowException(string id, string parameter1, string parameter2)
        {
            ThrowExceptionWithLine(SR.Get(id, parameter1, parameter2), null);
        }

        private void ThrowException(string id, string parameter1, string parameter2, string parameter3)
        {
            ThrowExceptionWithLine(SR.Get(id, parameter1, parameter2, parameter3), null);
        }



        //
        // ThrowException wrapper that just adds the line number & position.
        //

        internal void ThrowExceptionWithLine(string message, Exception innerException)
        {
            XamlParseException.ThrowException(message, innerException, _lineNumber, _linePosition);
        }


#endregion Exceptions

#region Properties

        /// <summary>
        /// Hashtable where key is the xmlNamespace, and value is the
        /// ClrNamespaceAssemblyPair structure containing clrNamespace and assembly
        /// </summary>
        internal HybridDictionary PITable
        {
            get { return _piTable; }
        }


        /// <summary>
        /// This is the associated BamlMapTable that contains information about what is
        /// in a baml stream.  The XamlTypeMapper uses the map table (if present) as a cache
        /// to store some assembly, type and property information.  If it is not present
        /// then this information is not cached and must be retrieved for every request.
        /// </summary>
        internal BamlMapTable MapTable
        {
            get { return _mapTable; }
            set { _mapTable = value; }
        }

        /// <summary>
        /// Line number used for error reporting
        /// </summary>
        internal int LineNumber
        {
            set { _lineNumber = value; }
        }

        /// <summary>
        /// Line position used for error reporting
        /// </summary>
        internal int LinePosition
        {
            set { _linePosition = value; }
        }

#if !PBTCOMPILER
        /// <summary>
        ///  Return the hashtable that is keyed by xml namespace uri and
        ///  has values that are collection of NamespaceMapEntry objects for that
        ///  xml namespace.
        /// </summary>
        internal Hashtable NamespaceMapHashList
        {
            get { return _namespaceMapHashList; }
        }

        internal System.Xaml.XamlSchemaContext SchemaContext
        {
            get
            {
                if (_schemaContext == null)
                {
                    _schemaContext = new XamlTypeMapperSchemaContext(this);
                }
                return _schemaContext;
            }
        }
#else
        // true if the Type Mapper can allow protected attributes.
        // This will be the case for a markup sub-classed root element only.
        internal bool IsProtectedAttributeAllowed
        {
            get { return _isProtectedAttributeAllowed; }
            set { _isProtectedAttributeAllowed = value; }
        }

        internal void ResetMapper()
        {
            _piTable.Clear();
            _piReverseTable.Clear();
            _lineNumber = 0;
            _linePosition = 0;
            _isProtectedAttributeAllowed = false;

            NamespaceMapEntry[] defaultNsMaps = _namespaceMapHashList[XamlReaderHelper.DefaultNamespaceURI] as NamespaceMapEntry[];
            NamespaceMapEntry[] definitionNsMaps = _namespaceMapHashList[XamlReaderHelper.DefinitionNamespaceURI] as NamespaceMapEntry[];
            NamespaceMapEntry[] definitionMetroNsMaps = _namespaceMapHashList[XamlReaderHelper.DefinitionMetroNamespaceURI] as NamespaceMapEntry[];

            _namespaceMapHashList.Clear();
            if (null != defaultNsMaps)
            {
                _namespaceMapHashList.Add(XamlReaderHelper.DefaultNamespaceURI, defaultNsMaps);
            }
            if (null != definitionNsMaps)
            {
                _namespaceMapHashList.Add(XamlReaderHelper.DefinitionNamespaceURI, definitionNsMaps);
            }
            if (null != definitionMetroNsMaps)
            {
                _namespaceMapHashList.Add(XamlReaderHelper.DefinitionMetroNamespaceURI, definitionMetroNsMaps);
            }
        }
#endif
#endregion Properties

        #region Data

        // Class used for Type information data cache.
        internal class TypeInformationCacheData
        {
            /// <summary>
            /// Create a new instance of the type cache.  Note that the type of this cached
            /// data is not stored anywhere in this object.  The type that this object pertains
            /// to is stored as the key in the _typeInformationCache dictionary.
            /// </summary>
            internal TypeInformationCacheData(Type baseType)
            {
                _baseType = baseType;
            }

            /// <summary>
            /// Urt Namespace for this type
            /// </summary>
            internal string ClrNamespace
            {
#if !PBTCOMPILER
                get { return _clrNamespace; }
#endif
                set { _clrNamespace = value; }
            }

#if !PBTCOMPILER
            /// <summary>
            /// The parent type in the inheritance hierarchy of this type.  This is
            /// stored here since BaseType lookups take some time.
            /// </summary>
            internal Type BaseType
            {
                get { return _baseType; }
            }

            /// <summary>
            /// TypeConverter associted with this type, if there is one.  This is stored
            /// here so that GetTypeConverter does not need to be called as often.
            /// </summary>
            internal TypeConverter Converter
            {
                get { return _typeConverter; }
                set { _typeConverter = value; }
            }
#endif
            // The type of the TypeConverter
            internal Type TypeConverterType
            {
                get { return _typeConverterType; }
                set { _typeConverterType = value; }
            }

            /// <summary>
            /// TrimSurroundingWhitespace value for this type.
            /// </summary>
            internal bool TrimSurroundingWhitespace
            {
                get { return _trimSurroundingWhitespace; }
                set { _trimSurroundingWhitespace = value; }
            }

            /// <summary>
            /// Flag to indicate if we have cached the TrimSurroundingWhitespace
            /// </summary>
            internal bool TrimSurroundingWhitespaceSet
            {
                get { return _trimSurroundingWhitespaceSet; }
                set { _trimSurroundingWhitespaceSet = value; }
            }

            /// <summary>
            /// Get the DependencyProperty from the Hashtable of PropertyAndType
            /// keyed by DependencyProperty name
            /// </summary>
            internal PropertyAndType GetPropertyAndType(string dpName)
            {
                if (_dpLookupHashtable == null)
                {
                    _dpLookupHashtable = new Hashtable();
                    return null;
                }

                return _dpLookupHashtable[dpName] as PropertyAndType;
            }


            /// <summary>
            /// Set a new PropertyAndType in the DependencyProperty information Hashtable.
            /// </summary>
            internal void SetPropertyAndType(
                string          dpName,
                PropertyInfo    dpInfo,
                Type            ownerType,
                bool            isInternal)
            {
                Debug.Assert(_dpLookupHashtable != null,
                    "GetPropertyAndType must always be called before SetPropertyAndType");

                // add the type taking a lock
                PropertyAndType pAndT = _dpLookupHashtable[dpName] as PropertyAndType;
                if (pAndT == null)
                {
                    _dpLookupHashtable[dpName] = new PropertyAndType(null, dpInfo, false, true, ownerType, isInternal);
                }
                else
                {
                    pAndT.PropInfo = dpInfo;
                    pAndT.PropInfoSet = true;
                    pAndT.IsInternal = isInternal;
                }
            }

            /// <summary>
            /// TypeConverters based upon attributes on property
            /// </summary>
            internal HybridDictionary PropertyConverters
            {
                get
                {
                    if (null == _propertyConverters)
                    {
                        _propertyConverters = new HybridDictionary();
                    }

                    return _propertyConverters;
                }
            }

            /// <summary>
            /// Set a new PropertyConverter for the given property.
            /// NOTE: This method takes a lock on the table. So to set
            /// values into the table you must use this method.
            /// </summary>
            internal void SetPropertyConverter(
                object dpOrPi,
#if !PBTCOMPILER
                TypeConverter converter)
#else
                Type converter)
#endif
            {
                _propertyConverters[dpOrPi] = converter;
            }

            // Private data members
            string        _clrNamespace;
            Type          _baseType;
            bool          _trimSurroundingWhitespace;
            Hashtable     _dpLookupHashtable;  // Hashtable of PropertyAndType keyed by dp name
            HybridDictionary     _propertyConverters = new HybridDictionary(); // Dictionary of TypeConverters keyed on dpOrPi
            bool          _trimSurroundingWhitespaceSet;
#if !PBTCOMPILER
            TypeConverter _typeConverter;
#endif
            Type          _typeConverterType;
        }

        // DP setter method, PropertyInfo and Type record held in _dpLookupHashtable
        internal class PropertyAndType
        {
            public PropertyAndType (MethodInfo dpSetter,
                                    PropertyInfo dpInfo,
                                    bool setterSet,
                                    bool propInfoSet,
                                    Type ot,
                                    bool isInternal)
            {
                Setter      = dpSetter;
                PropInfo    = dpInfo;
                OwnerType   = ot;
                SetterSet   = setterSet;
                PropInfoSet = propInfoSet;
                IsInternal  = isInternal;
            }

            public PropertyInfo PropInfo;
            public MethodInfo   Setter;
            public Type         OwnerType;
            public bool         PropInfoSet;
            public bool         SetterSet;
            public bool         IsInternal;
        }

        // Constants that identify special types of string values
        internal const string MarkupExtensionTypeString = "Type ";
        internal const string MarkupExtensionStaticString = "Static ";
//        internal const string MarkupExtensionNullString = "Null";
        internal const string MarkupExtensionDynamicResourceString = "DynamicResource ";

        // If the case or name of the assembly name changes in the Framework build,
        // then the following will have to change also.
        internal const string PresentationFrameworkDllName  = "PresentationFramework";

        // Namespace & classname of the generated helper class for accessing allowed internal types in PT.
        internal const string GeneratedNamespace = "XamlGeneratedNamespace";
        internal const string GeneratedInternalTypeHelperClassName = "GeneratedInternalTypeHelper";

#if !PBTCOMPILER
        internal const string MarkupExtensionTemplateBindingString = "TemplateBinding ";
#else
        private static bool _hasInternals = false;
        private static bool _hasLocalReference = false;
        private bool _isProtectedAttributeAllowed = false;
        internal static Assembly AssemblyWB = null;
        internal static Assembly AssemblyPC = null;
        internal static Assembly AssemblyPF = null;

        internal static void Clear()
        {
            _hasInternals = false;
            _hasLocalReference = false;
            AssemblyWB = null;
            AssemblyPC = null;
            AssemblyPF = null;
        }
#endif
        // Map table associated with this XamlTypeMapper.  This contains information
        // about what is stored in BAML.  The XamlTypeMapper makes use of the caches in
        // the BamlMapTable to store some assembly, type and property information.
        BamlMapTable _mapTable;

        // Array of assembly names that can be used when resolving clr namespaces
        string[] _assemblyNames;

        // array or namespace map entries such as `http:// mappings.
        NamespaceMapEntry[] _namespaceMaps;

        // HashTable of cached type lookups and the serializers for that type.  These
        // are always TypeAndSerializer objects
        Hashtable _typeLookupFromXmlHashtable = new Hashtable();

        // Hash table of mappings between xmlNamespace and mappings
        Hashtable _namespaceMapHashList = new Hashtable();

        // Hashtable where the key is the fullTypeName + '#' + propertyName and the value
        HybridDictionary _typeInformationCache = new HybridDictionary();

#if !PBTCOMPILER
        // Hashtable where the key is the type and the value is the set of constructors for that type
        HybridDictionary _constructorInformationCache;

        // A SchemaContext that respects the namespace mappings, PIs, and assembly paths
        // passed in to this TypeMapper
        private XamlTypeMapperSchemaContext _schemaContext;
#endif

        // Hashtable where key is the xmlNamespace, and value is the
        // ClrNamespaceAssemblyPair structure containing clrNamespace and assembly
        HybridDictionary _piTable =  new HybridDictionary();

        // Hashtable where key is the clrNamespace + "#" + assemblyName and the
        // value is the corresponding xmlNamespace.  This is used for fast lookups
        // of xmlnamespace if you know the assembly and clr namespace.
        Dictionary<string, string> _piReverseTable = new Dictionary<string, string>();

        // Hashtable where key is the assembly's short name that has been uppercased,
        // and the value is a path where that assembly can be loaded from.
        // Always lock on this object when writing to it
        HybridDictionary _assemblyPathTable = new HybridDictionary();

        // true if referenced assemblies in the _assemblyPathTable have been loaded
        bool _referenceAssembliesLoaded = false;

        // Line number and position in original Xaml file corresponding to the
        // current BAML record.
        int _lineNumber = 0;
        int _linePosition = 0;

        // Cache of namespace and assemblies.
        private static XmlnsCache _xmlnsCache = null;

#endregion Data

#endregion Internal
    }

    // Type of object and type of Serializer for that type.  If this type
    // also contains an [XmlLang] property, this caches the property info also.
    // These are contained in the _typeLookupFromXmlHashtable hastable
    internal class TypeAndSerializer
    {
        public TypeAndSerializer()
        {
        }

        public Type ObjectType = null;
        public Type SerializerType = null;
        public bool IsSerializerTypeSet = false;
        public PropertyInfo XmlLangProperty;
    }

    /// <summary>
    /// Contains information the XamlTypeMapper uses for Mapping between an xml
    /// XmlNamespace and what Assembly, Namespace to look in.
    /// </summary>
    [DebuggerDisplay("'{_xmlNamespace}'={_clrNamespace}:{_assemblyName}")]
#if PBTCOMPILER
    internal class NamespaceMapEntry
#else
    public class NamespaceMapEntry
#endif
    {
        #region Constructors

        ///<summary>
        /// NamespaceMapEntry default constructor
        ///</summary>
        public NamespaceMapEntry()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xmlNamespace">The XML NamespaceURi</param>
        /// <param name="assemblyName">Assembly to use when resolving a Tag</param>
        /// <param name="clrNamespace">Namespace within the assembly</param>
        public NamespaceMapEntry(string xmlNamespace,string assemblyName,string clrNamespace)
        {
            if (xmlNamespace == null)
                throw new ArgumentNullException("xmlNamespace");

            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");

            if (clrNamespace == null)
                throw new ArgumentNullException("clrNamespace");

            _xmlNamespace = xmlNamespace;
            _assemblyName = assemblyName;
            _clrNamespace = clrNamespace;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xmlNamespace">The XML NamespaceURi</param>
        /// <param name="assemblyName">Assembly to use when resolving a Tag</param>
        /// <param name="clrNamespace">Namespace within the assembly</param>
        /// <param name="assemblyPath">Path to use when loading assembly.  This may be null.</param>
         internal NamespaceMapEntry(
                string xmlNamespace,
                string assemblyName,
                string clrNamespace,
                string assemblyPath) : this(xmlNamespace, assemblyName, clrNamespace)
        {
            _assemblyPath = assemblyPath;
        }

        #endregion Constructors


        #region Properties

        /// <summary>
        /// Xml namespace specified in the constructor
        /// </summary>
        public string XmlNamespace
        {
            get { return _xmlNamespace; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (_xmlNamespace == null)
                {
                    _xmlNamespace = value;
                }
            }
        }

        /// <summary>
        /// AssemblyName specified in the constructor
        /// </summary>
        public string AssemblyName
        {
            get { return _assemblyName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (_assemblyName == null)
                {
                    _assemblyName = value;
                }
            }
        }

        /// <summary>
        /// ClrNamespace specified within the constructor
        /// </summary>
        public string ClrNamespace
        {
            get { return _clrNamespace; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (_clrNamespace == null)
                {
                    _clrNamespace = value;
                }
            }
        }


        #endregion Properties

        /// <summary>
        /// returns instance of the assembly associate with this
        /// namespace map
        /// </summary>
        internal Assembly Assembly
        {
            get
            {
                if (null == _assembly && _assemblyName.Length > 0)
                {
#if PBTCOMPILER
                    // NOTE: At compile time a local assembly can already be loaded if being
                    // referenced by dehydrated assemblies and so we should attempt to return
                    // that.
                    if (_isLocalAssembly)
                    {
                        string assemblyNameLookup = _assemblyName.ToUpper(CultureInfo.InvariantCulture);

                        // Check if the assembly has already been loaded.
                        if (!ReflectionHelper.HasAlreadyReflectionOnlyLoaded(assemblyNameLookup))
                        {
                            return null;
                        }
                    }

#endif
                    _assembly = ReflectionHelper.LoadAssembly(_assemblyName, _assemblyPath);
                }

                return _assembly;
            }
        }

        /// <summary>
        /// Get and set the path to use when loading the assembly.
        /// </summary>
        internal string AssemblyPath
        {
            get { return _assemblyPath; }
            set { _assemblyPath = value; }
        }

#if PBTCOMPILER
        internal bool LocalAssembly
        {
            get { return _isLocalAssembly; }
            set { _isLocalAssembly = value; }
        }

        bool     _isLocalAssembly;
#endif

#region Data

        string   _xmlNamespace;
        string   _assemblyName;
        string   _assemblyPath;
        Assembly _assembly;
        string   _clrNamespace;

#endregion Data
    }

    // This is a convenience holder for all the possible IDs that Xaml understands which could
    // be on an object element.
    internal class XamlObjectIds
    {
        public string Name = null;
        public string Uid = null;
        public object Key = null;
    }


#region XmlParserDefaults Class

    // class for getting and setting mapping defaults.
    internal static class XmlParserDefaults
    {
#region Methods

        /// <summary>
        ///  Instance of XamlTypeMapper to use if none is specified in the
        ///  ParserContext. XamlTypeMapper returned is has its assembly and namespace
        ///  maps initialized to those set
        ///  via SetDefaultXmlMapping() or one built from our internal defaults.
        /// </summary>
        internal static XamlTypeMapper DefaultMapper
        {
            get
            {
                return new XamlTypeMapper(GetDefaultAssemblyNames(),GetDefaultNamespaceMaps());
            }
        }

#endregion Methods

#region Properties

        /// <summary>
        /// Returns an array of the DefaultAssemblyNames
        /// </summary>
        internal static string[] GetDefaultAssemblyNames()
        {
            return (string[])_defaultAssemblies.Clone();
        }

        /// <summary>
        /// Returns array of the DefaultNamespaceMaps
        /// </summary>
        internal static NamespaceMapEntry[] GetDefaultNamespaceMaps()
        {
            return (NamespaceMapEntry[])_defaultNamespaceMapTable.Clone();
        }

#endregion Properties

#region Data

        // array of our defaultAssemblies.
        private static readonly string[] _defaultAssemblies = {"WindowsBase", "PresentationCore", "PresentationFramework"};

        // array of namespaceMaps the map an xmlns namespaceURI
        // to the assembly and urtNamespace to search in when resolving the xml

        private static readonly NamespaceMapEntry[] _defaultNamespaceMapTable = { };

#endregion Data
    }
#endregion XmlParserDefaults Class
}




