// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Interop;
using System.Threading;
using System.Windows.Markup;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Test.Win32;
using System.Runtime.Serialization;
using System.Reflection;


namespace Microsoft.Test.Markup
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class MissedSupportFilesException : Exception
    {

        /// <summary>
        /// Passes message parameter to base constructor.
        /// </summary>
        public MissedSupportFilesException(string fileName)
            : base(fileName)
        {
            _fileName = fileName;
        }

        /// <summary>
        /// Passes parameters to base constructor.
        /// </summary>
        public MissedSupportFilesException(string fileName, Exception innerException)
            : base(fileName, innerException)
        {
            _fileName = fileName;
        }

        /// <summary>
        /// This constructor is required for deserializing this type across AppDomains.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected MissedSupportFilesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// </summary>
        public string FileName
        {
            get
            {
                return _fileName;
            }
        }

        private string _fileName = "";
    }   
    
    /// <summary>
    /// </summary>
    public class DefaultRandomGenerator : IRandomGenerator
    {

        /// <summary>
        /// </summary>        
        public DefaultRandomGenerator()
        {
            _random = new Random();
        }
        
        /// <summary>
        /// </summary>        
        public DefaultRandomGenerator(int seed)
        {
            _random = new Random(seed);
        }

        /// <summary>
        /// </summary>        
        public int Next()
        {
            return _random.Next();
        }

        /// <summary>
        /// </summary>        
        public int Next(int top)
        {
            return _random.Next(top);
        }

        /// <summary>
        /// </summary>
        public int Next(int bottom, int top)
        {
            return _random.Next(bottom, top);
        }

        /// <summary>
        /// </summary>
        public static void SetGlobalRandomGenerator(IRandomGenerator randomGenerator)
        {
            _randomGenerator = randomGenerator;
        }

        /// <summary>
        /// </summary>
        public static IRandomGenerator GetGlobalRandomGenerator()
        {
            return _randomGenerator;
        }
        
        
        private static IRandomGenerator _randomGenerator = new DefaultRandomGenerator() as IRandomGenerator;
        private Random _random =null;
    }

    /// <summary>
    /// </summary>
    public interface IRandomGenerator
    {

        /// <summary>
        /// </summary>
        int Next();
            
        /// <summary>
        /// </summary>
        int Next(int top);

        /// <summary>
        /// </summary>
        int Next(int bottom, int top);
    }

    /// <summary>
    /// </summary>
    public class WPFXamlDefinition
    {

        /// <summary>
        /// </summary>
        public WPFXamlDefinition() : this (DefaultRandomGenerator.GetGlobalRandomGenerator())
        {

        }
        
        /// <summary>
        /// </summary>
        public WPFXamlDefinition(IRandomGenerator randomGenerator)
        {
            Initialize(randomGenerator, -1,-1,-1,-1);
        }

        /// <summary>
        /// </summary>
        public WPFXamlDefinition(IRandomGenerator randomGenerator,
                                 int maxTotalElements,
                                 int maxAttributes,
                                 int maxChildren,
                                 int maxDepth)
        {
            Initialize(randomGenerator,maxTotalElements,maxAttributes,maxChildren,maxDepth);
        }


        /// <summary>
        /// </summary>
        public WPFXamlDefinition(int maxTotalElements,
                                 int maxAttributes,
                                 int maxChildren,
                                 int maxDepth)
        {
            Initialize(DefaultRandomGenerator.GetGlobalRandomGenerator(),maxTotalElements,maxAttributes,maxChildren,maxDepth);
        }


        private void Initialize(
                                 IRandomGenerator randomGenerator,
                                 int maxTotalElements,
                                 int maxAttributes,
                                 int maxChildren,
                                 int maxDepth)
        {

            
            RandomGenerator = randomGenerator;
            MaxTotalElements = maxTotalElements <= 0 ? RandomGenerator.Next(1000) : maxTotalElements;
            MaxAttributes = maxAttributes < 0 ? RandomGenerator.Next(4) : maxAttributes;
            MaxChildren = maxChildren < 0 ? RandomGenerator.Next(1, 12) : maxChildren;

            if (maxDepth < 0)
            {
                MaxDepth = MaxChildren == 1 ? MaxTotalElements : (int)Math.Log(MaxTotalElements, MaxChildren);
                if (MaxDepth < 0)
                {
                    MaxDepth = 0;
                }

                
                MaxDepth = RandomGenerator.Next(1, MaxDepth + 1);
            }
            else
            {
                MaxDepth = maxDepth;
            }
        }

        /// <summary>
        /// </summary>        
        public int MaxTotalElements;

        /// <summary>
        /// </summary>
        public int MaxAttributes;

        /// <summary>
        /// </summary>
        public int MaxChildren;

        /// <summary>
        /// </summary>
        public int MaxDepth;

        /// <summary>
        /// </summary>        
        public IRandomGenerator RandomGenerator;

        /// <summary>
        /// Path were all the files XSD, Xaml etc can be found.
        /// </summary>        
        public string SupportFilesPath
        {
            get
            {
                return _supportFilesPath;
            }
            set
            {
                if (!Path.IsPathRooted(value))
                {
                    throw new ArgumentException("Not a full path");
                }
                
                string temp = value;
                
                if (temp[temp.Length - 1] == Path.DirectorySeparatorChar)
                {
                    throw new ArgumentException("The path should not end in a Path.DirectorySeparatorChar");
                }

                _supportFilesPath = temp;
            }
        }

        private string _supportFilesPath = Environment.CurrentDirectory;

    }




    /// <summary>
    /// </summary>
    public class SetupXamlInformation
    {
        /// <summary>
        /// </summary>
        public SetupXamlInformation() : this (false)
        {
        }


        /// <summary>
        /// </summary>
        public SetupXamlInformation(bool enableXamlCompilationFeatures)
        {
            EnableXamlCompilationFeatures = enableXamlCompilationFeatures;
        }
        
        /// <summary>
        /// </summary>
        public bool EnableXamlCompilationFeatures;

        /// <summary>
        /// </summary>
        public string ClassName;     

        /// <summary>
        /// </summary>
        public List<string> RDKeysGeneratedList = new List<string>();


        /// <summary>
        /// </summary>
        public bool EnableAsyncParsing = false;
        
    }


    /// <summary>
    /// </summary>
    public static class WPFXamlGenerator
    {
        static WPFXamlGenerator()
        {

        }
    
        /// <summary>
        /// 
        /// </summary>
        public static readonly string XamlURI = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static XamlGenerator _generator = null;
        private static XamlGenerator _windowGenerator = null;
        private static XamlGenerator _freezableGenerator = null;
        private static XamlGenerator _resourceGenerator = null;
        private static object _lockObj = new object();
        private static string _generatedXamlFileNamePrefix = "." + Path.DirectorySeparatorChar + "__tempXamlFile";
        private static IRandomGenerator _rnd = null;

        private static bool _shouldTestBitmapEffects = false;
        private static bool _shouldTestTransforms = false;
        
        private static WPFXamlDefinition _xamlDefinition;
        private static bool _initialized = false;

        [ThreadStatic]
        private static SetupXamlInformation _setupXamlInfo = null;


        /// <summary>
        /// </summary>
        public static EventHandler XamlGenerated;

        private static void OnXamlGenerated(string s)
        {
            if (XamlGenerated != null)
            {
                XamlGenerated(s, EventArgs.Empty);
            }
        }

        /// <summary>
        /// </summary>
        public static bool ShouldTestTransforms
        {
            get
            {
                return _shouldTestTransforms;
            }
        }
        
        /// <summary>
        /// </summary>
        public static bool ShouldTestBitmapEffects
        {
            get
            {
                return _shouldTestBitmapEffects;
            }
        }

        private static void ValidateInitialization()
        {
            if (_initialized)
            {
                throw new InvalidOperationException("You cannot initialized twice.");
            }
        }


        private static void _EnsureInitialized()
        {
            
            
            if (_initialized)
            {
                return;
            }

            lock (_lockObj)
            {
                if (_initialized)
                {
                    return;
                }

                Initialize(new WPFXamlDefinition());
                WPFXamlGenerator.ValidateDefaultFiles();
            }
        }

        /// <summary>
        /// Resets the generators' random number seeds.
        /// </summary>
        public static void Reset()
        {
            _EnsureInitialized();

            _generator.Reset(_rnd.Next());
            _windowGenerator.Reset(_rnd.Next());
            _freezableGenerator.Reset(_rnd.Next());
            _resourceGenerator.Reset(_rnd.Next());
        }


        /// <summary>
        /// Resets the generators' random number seeds.
        /// </summary>
        public static WPFXamlDefinition XamlDefinition
        {
            get
            {
                _EnsureInitialized();          
                
                return _xamlDefinition;
            }
        }


        private static string BuildPath(string fileName)
        {
            if (!String.IsNullOrEmpty(_xamlDefinition.SupportFilesPath))
            {
                return Path.Combine(_xamlDefinition.SupportFilesPath, fileName);
            }
            return fileName;
        }


        /// <summary>
        /// </summary>
        public static void Initialize(WPFXamlDefinition xamlDefinition)
        {
            ValidateInitialization();
            
            lock(_lockObj)
            {
                ValidateInitialization();
                
                _xamlDefinition = xamlDefinition;
                _rnd = xamlDefinition.RandomGenerator;


                string[] xamlFiles = 
                    {       
                        BuildPath("pagesource1.xaml"),
                        BuildPath("pagesource2.xaml")
                    };

                string[] resourcesFiles = 
                    {
                        BuildPath("arial.ttf"), 
                        BuildPath("bee.wmv"),
                        BuildPath("Gone Fishing.bmp"),
                        BuildPath("small font.ttf")
                    };

                string[] dllFiles = 
                    {
                        BuildPath("TestRuntimeUntrusted.dll")
                    };


                DefaultResourcesFiles = new List<string>(4);
                DefaultXamlFiles = new List<string>(2);
                DefaultDllFiles = new List<string>(3);


                DefaultResourcesFiles.AddRange(resourcesFiles);
                DefaultXamlFiles.AddRange(xamlFiles);
                DefaultDllFiles.AddRange(dllFiles);


                DefaultRandomGenerator.SetGlobalRandomGenerator(xamlDefinition.RandomGenerator);
                
                //
                // Create generators.
                //
                List<XmlGenerator> generators = new List<XmlGenerator>();

                switch (_rnd.Next(5))
                {
                    case 0:
                        // Includes everything except custom types, BitmapEffect, and LayoutTransform.
                        _generator = new XamlGenerator(BuildPath("RootTypes.xsd"));
                        break;
                    case 1:
                        // Includes everything except custom types, MediaElement, and BitmapEffect.
                        _generator = new XamlGenerator(BuildPath("RootTypes2.xsd"));
                        _shouldTestTransforms = true;
                        break;
                    case 2:
                        // Includes everything except custom types, flow types, MediaElement, and BitmapEffect.
                        _generator = new XamlGenerator(BuildPath("RootTypes3.xsd"));
                        _shouldTestTransforms = true;
                        break;
                    case 3:
                        // Includes everything except flow types, MediaElement, BitmapEffect, and LayoutTransform.
                        _generator = new XamlGenerator(BuildPath("RootTypes4.xsd"));
                        break;
                    case 4:
                        // Includes everything except custom types, flow types, MediaElement, and LayoutTransform.
                        _generator = new XamlGenerator(BuildPath("RootTypes5.xsd"));
                        _shouldTestBitmapEffects = true;
                        break;
                }


                _windowGenerator = new XamlGenerator(BuildPath("WindowTypes.xsd"));
                _freezableGenerator = new XamlGenerator(BuildPath("RootFreezableTypes.xsd"));
                _resourceGenerator = new XamlGenerator(BuildPath("ResourcesType.xsd"));
                generators.Add(_generator);
                generators.Add(_windowGenerator);
                generators.Add(_freezableGenerator);
                generators.Add(_resourceGenerator);            

                TextContentHelper textContentHelper = new TextContentHelper(_GenerateText);
                ElementHelper elementHelper = new ElementHelper(_GenerateElement);
                AttributeHelper attributeHelper1 = new AttributeHelper(_GenerateXmlLangValue);
                AttributeHelper attributeHelper2 = new AttributeHelper(_GenerateNameValue);
                AttributeHelper attributeHelper3 = new AttributeHelper(_GenerateAllowsTransparencyValue);

                foreach (XmlGenerator generator in generators)
                {
                    // Initialize XamlGenerator's random generator.
                    generator.Reset(_rnd.Next());

                    // Register helper for generating text content.
                    generator.RegisterTextHelper(textContentHelper);

                    // Register helper for generating elements so we can filter 
                    // out stuff inside Templates.
                    generator.RegisterElementHelper(elementHelper);

                    // Register helpers for generating xml:lang, Name, and AllowsTransparency values.
                    generator.RegisterAttributeHelper(attributeHelper1, "lang");
                    generator.RegisterAttributeHelper(attributeHelper2, "Name");
                    generator.RegisterAttributeHelper(attributeHelper3, "AllowsTransparency");
                }




                _initialized = true;
            }
        }


        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string GenerateFreezable()
        {

            SetupXamlInformation compileFeatures = new SetupXamlInformation();

            _EnsureInitialized();

            return _GenerateTree(_freezableGenerator,compileFeatures);
        }

        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string GenerateResourceDictionary()
        {
            SetupXamlInformation compileFeatures = new SetupXamlInformation();            

            _EnsureInitialized();          

            return _GenerateTree(_resourceGenerator,compileFeatures);
        }


        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string GenerateWindow()
        {
            SetupXamlInformation compileFeatures = new SetupXamlInformation();

            _EnsureInitialized();

            return _GenerateTree(_windowGenerator,compileFeatures);
        }

        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string Generate()
        {
            SetupXamlInformation compileFeatures = new SetupXamlInformation();            

            _EnsureInitialized();

            return _GenerateTree(_generator,compileFeatures);
        }


        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string GenerateFreezable(SetupXamlInformation compileFeatures)
        {
            _EnsureInitialized();
            return _GenerateTree(_freezableGenerator,compileFeatures);
        }

        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string GenerateResourceDictionary(SetupXamlInformation compileFeatures)
        {
            _EnsureInitialized();
            return _GenerateTree(_resourceGenerator,compileFeatures);
        }


        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string GenerateWindow(SetupXamlInformation compileFeatures)
        {
            _EnsureInitialized();
            return _GenerateTree(_windowGenerator,compileFeatures);
        }

        /// <summary>
        /// Holds routines that are useful in multiple actions.
        /// </summary>        
        public static string Generate(SetupXamlInformation compileFeatures)
        {
            _EnsureInitialized();
            return _GenerateTree(_generator,compileFeatures);
        }


        static private bool IsCompiledEnabled()
        {
            return _setupXamlInfo.EnableXamlCompilationFeatures;
        }
        
        private static string _GenerateTree(XamlGenerator generator,SetupXamlInformation setupXamlInfo)
        {
            if (setupXamlInfo == null)
            {
                throw new ArgumentNullException("setupXamlInfo");
            }
            
            _setupXamlInfo = setupXamlInfo;
            
            // 
            // Generate xaml using XamlGenerator.
            // Note: generator code was referenced from ClientTestLibrary directory.
            //

            int maxTotalElements = _xamlDefinition.MaxTotalElements;
            int maxAttributes = _xamlDefinition.MaxAttributes;
            int maxChildren = _xamlDefinition.MaxChildren;
            int maxDepth = _xamlDefinition.MaxDepth;

            DateTime start = DateTime.Now;
            Stream stream = generator.CreateStream(maxDepth, maxAttributes, maxChildren); // maxDepth, maxAttributes, maxChildren

            string xaml = "";

            // Store xaml in string.
            using(StreamReader sr = new StreamReader(stream))
            {
                xaml = sr.ReadToEnd();
            }


            xaml = ModifyXaml.PostProcess(xaml,setupXamlInfo);
           
            OnXamlGenerated(xaml); 

            //
            // Return root of generated tree or the string depending of the parseXaml argument.
            //
            return xaml;
        }
        private static void DeleteTempXamlFile(object tempFileName)
        {
            File.Delete((string)tempFileName);
        }
        // Writes text content for mixed content nodes generated by Xml/XamlGenerator.
        private static HandledLevel _GenerateText(XmlNode parentNode)
        {
            // Only write text if we're not under a Style or directly under a FlowDocument tag.
            if (!IsInStyle(parentNode) && parentNode.Name != "FlowDocument")
            {
                XmlText textNode = parentNode.OwnerDocument.CreateTextNode(_GetArbitraryString());

                parentNode.AppendChild(textNode);

                return HandledLevel.Complete;
            }
            else
            {
                return HandledLevel.None;
            }
        }
        // Adjusts xaml for compilation, async, templates, and various special xaml rules.
        private static HandledLevel _GenerateElement(XmlNode parentNode, XmlNode newNode, bool isStart)
        {
            // Add x:SynchronousMode and x:AsyncRecords to root node if the generated xaml
            // may be parsed asynchronously.
            if (!IsCompiledEnabled() && parentNode is XmlDocument && _setupXamlInfo.EnableAsyncParsing)
            {
                XmlAttribute attribute = ((XmlDocument)parentNode).CreateAttribute("SynchronousMode", XamlURI);
                attribute.Value = "Async";
                newNode.Attributes.Append(attribute);

                int asyncRecords = _rnd.Next(0, 300);

                attribute = ((XmlDocument)parentNode).CreateAttribute("AsyncRecords", XamlURI);
                attribute.Value = asyncRecords.ToString();
                newNode.Attributes.Append(attribute);

            }

            // Add x:Class attribute to root node if the generated xaml
            // will be compiled.
            if (IsCompiledEnabled() && parentNode is XmlDocument)
            {
                XmlDocument doc = parentNode as XmlDocument;
                XmlAttribute xmlAttribute = doc.CreateAttribute("x", "Class", XamlGenerator.AvalonXmlnsX);
               
                string value = "Microsoft.Test.CN_" + GetUniqueID();

                xmlAttribute.Value = value;
                _setupXamlInfo.ClassName = value;

                newNode.Attributes.Append(xmlAttribute);
            }

            // Exclude Foo.Resources property element tags when inside template xaml.
            if (IsInTag(parentNode, "Template") &&
                (newNode.LocalName.Contains(".Resources")))
            {
                return HandledLevel.Complete;
            }
            
            // Remove any preceding text nodes if this node is a 
            // property element (e.g. Foo.Background).
            // This is necessary because the xaml spec says
            // property elements cannot be interwoven with text.
            // They can exist at the beginning and/or end, but not
            // within text.
            if (newNode.LocalName.Contains("."))
            {
                XmlNode current = newNode;
                XmlNode prev = current.PreviousSibling;
                while (prev != null && prev is XmlText)
                {
                    current = prev;
                    prev = prev.PreviousSibling;
                    parentNode.RemoveChild(current);
                }
            }

            if (IsParentResourceDictionaryNode(newNode))
            {
                ProcessResourceDictionaryChild(newNode);
            }
            

            // Set a unique key for every entry in a ResourceDictionary. 
            if (!isStart && IsResourceDictionaryNode(newNode))
            {
                _SetResourceDictionaryKeys(newNode);
            }
            
            return HandledLevel.None;
        }

        static readonly string[] _posibleValues =  { "true", "false" };

        /// <summary>
        /// </summary>
        public static bool IsResourceDictionaryNode(XmlNode node)
        {            
            if (node.LocalName.Contains(".Resources") || node.LocalName.Contains("ResourceDictionary"))
            {
                return true;
            }

            return false;
        }

        private static bool IsParentResourceDictionaryNode(XmlNode node)
        {
            XmlNode parentNode = node.ParentNode;
            
            if (parentNode != null && IsResourceDictionaryNode(parentNode))
            {
                return true;
            }

            return false;
        }


        private static bool IsNestedResourceDictionaryItem(XmlNode node)
        {
            bool isRDParentVisited = false;

            while( node != null && !(node is XmlDocument))
            {
                if (IsParentResourceDictionaryNode(node))
                {
                    if(!isRDParentVisited) // this is first RD found, we'll keep looking past it
                    {
                        isRDParentVisited = true;
                    }
                    else // this is second RD found, so we're nested
                    {
                        return true;
                    }
                }
                
                node = node.ParentNode;
            }


            return false;
        }

        private static void ProcessResourceDictionaryChild(XmlNode node)
        {
            if (IsCompiledEnabled())
            {
                if (((XmlElement)node).GetAttributeNode("Shared",XamlGenerator.AvalonXmlnsX) == null &&
                    !IsNestedResourceDictionaryItem(node))
                {                   
                    int index = _rnd.Next(0, 3);

                    if (index < 2)
                    {
                        SetXSharedValue((XmlElement)node,_posibleValues[index]);
                    }
                }
            }
        }

        ///<summary>
        ///</summary>
        public static void SetXSharedValue(XmlElement node, string value)
        {
            XmlAttribute xmlAttribute = node.GetAttributeNode("Shared",XamlGenerator.AvalonXmlnsX);            

            if (xmlAttribute != null)
            {
                xmlAttribute.Value = value;
            }
            else
            {
                xmlAttribute = node.OwnerDocument.CreateAttribute("x", "Shared", XamlGenerator.AvalonXmlnsX);             
                xmlAttribute.Value = value;
                node.Attributes.Append(xmlAttribute);                
            }
           
        }

        private static string GetUniqueID()
        {
            return Guid.NewGuid().ToString("N");
        }
        
        
        private static void _SetResourceDictionaryKeys(XmlNode resourcesNode)
        {
            List<string> _implicitKeys = new List<string>();

            foreach (XmlNode node in resourcesNode.ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }

                bool isImplicitKey = false;

                // Check if we can omit a key for Style, i.e. the key will be implicit from
                // the TargetType attribute.  The key may be omitted only if an implicit 
                // key hasn't been specified yet for the same TargetType.
                string targetType = null;
                if (node.Name == "Style")
                {
                    targetType = ((XmlElement)node).GetAttribute("TargetType");
                    int index = targetType.IndexOf(' ') + 1;
                    targetType = targetType.Substring(index, targetType.Length - index - 1);

                    isImplicitKey = !_implicitKeys.Contains(targetType) && ShouldThrottle(1, 2);
                }

                if (isImplicitKey)
                {
                    _implicitKeys.Add(targetType);
                }
                else
                {
                    string keyValue = "key_" + GetUniqueID();

                    _setupXamlInfo.RDKeysGeneratedList.Add(keyValue);

                    ((XmlElement)node).SetAttribute("Key", XamlURI, keyValue);
                }
            }
        }
        // Writes xml:lang value for nodes generated by Xml/XamlGenerator.
        private static HandledLevel _GenerateXmlLangValue(XmlNode parentNode, XmlAttribute attribute)
        {
            if (attribute.NamespaceURI == "http://www.w3.org/XML/1998/namespace" && attribute.LocalName == "lang" && !parentNode.LocalName.Contains("."))
            {
                attribute.Value = "en-US";
                ((XmlElement)parentNode).SetAttributeNode(attribute);
            }

            return HandledLevel.Complete;
        }
        // Writes a Name value for nodes generated by Xml/XamlGenerator.
        private static HandledLevel _GenerateNameValue(XmlNode parentNode, XmlAttribute attribute)
        {
            if (attribute.LocalName == "Name" && (!IsInTag(parentNode, "Resources") && !IsInTag(parentNode, "ResourceDictionary")))
            {

                attribute.Value = "name_" + GetUniqueID();

                ((XmlElement)parentNode).SetAttributeNode(attribute);
            }

            return HandledLevel.Complete;
        }
        // Ensures WindowStyle is set to valid value when AllowsTransparency is true.
        // Also, sets Opacity so layered windows are more apparent.
        private static HandledLevel _GenerateAllowsTransparencyValue(XmlNode parentNode, XmlAttribute attribute)
        {
            if (ShouldThrottle(1, 2))
            {
                ((XmlElement)parentNode).SetAttribute("AllowsTransparency", "True");
                ((XmlElement)parentNode).SetAttribute("WindowStyle", "None");
                ((XmlElement)parentNode).SetAttribute("Opacity", "0.6");
            }
            else
            {
                attribute.Value = "false";
            }

            return HandledLevel.Complete;
        }
        private static string _GetArbitraryString()
        {
            char c = 'a';
            int length = _rnd.Next(100);
            StringBuilder builder = new StringBuilder(length);

            while(length > 0)
            {
                int i = _rnd.Next(Char.MinValue, (char)0xFFFD);
                c = (char)i;

                //// Skip if value is not valid xml character.
                if ( (i < 0x20 && i != 0x9 && i != 0xA && i != 0xD) )
                    continue;

                if (Char.IsLowSurrogate(c))
                    _AddSurrogate(builder, false);

                builder.Append(c);

                if (Char.IsHighSurrogate(c))
                    _AddSurrogate(builder, true);

                length--;
            }

            return builder.ToString();
        }
        private static void _AddSurrogate(StringBuilder builder, bool low)
        {
            char c;

            if (low)
            {
                c = (char)_rnd.Next(0xDC00, 0xDFFF);
                builder.Append(c);
            }
            else
            {
                c = (char)_rnd.Next(0xD800, 0xDBFF);
                builder.Append(c);
            }
        }
        /// <summary>
        /// Checks if the given node is under a Style node or is one itself.
        /// </summary>
        private static bool IsInStyle(XmlNode parentNode)
        {
            while (parentNode != null)
            {
                if (parentNode.Name == "Style")
                    return true;

                parentNode = parentNode.ParentNode;
            }

            return false;
        }
        /// <summary>
        /// Checks if the given node is under a specific node or is one itself.
        /// </summary>
        private static bool IsInTag(XmlNode node, string tagName)
        {
            while (node != null)
            {
                if (node.Name.Contains(tagName))
                    return true;

                node = node.ParentNode;
            }

            return false;
        }
        /// <summary>
        /// Displays xaml from a stream in notepad.exe.
        /// </summary>
        internal static void ShowXaml(Stream stream)
        {
            // Create a temporary file name.
            string createdXamlFile = Path.GetTempFileName();

            // Save xaml to file.
            SaveXamlFromStream(stream, createdXamlFile);

            // Run notepad.exe.
            RunProcess("notepad.exe", createdXamlFile, Environment.CurrentDirectory);

            // Reposition at beginning of stream.
            stream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Read from a stream and save the result into a file.
        /// </summary>
        internal static void SaveXamlFromString(String xaml, string filename)
        {
            StreamWriter sw = new StreamWriter(filename);

            try
            {
                sw.Write(xaml);
                sw.Flush();
            }
            finally
            {
                sw.Close();
            }
        }
        /// <summary>
        /// Read from a stream and save the result into a file.
        /// </summary>
        internal static void SaveXamlFromStream(Stream stream, string filename)
        {
            //read out
            string strXaml = null;
            StreamReader sr = new StreamReader(stream);

            strXaml = sr.ReadToEnd();

            SaveXamlFromString(strXaml, filename);
        }
        /// <summary>
        /// Callback type used for TryCallback.
        /// </summary>
        internal delegate void AttemptCallback(object obj);
        /// <summary>
        /// Attempts to execute a callback multiple times if a specific exception occurs. 
        /// This is useful for deleting a file that might be accessed by antivirus apps.
        /// </summary>
        internal static void TryCallback(AttemptCallback callback, object obj, Type filterExceptionType)
        {
            // Initialization for the multi-attempt logic
            bool success = false;
            int maxAttempts = 5;
            int timeBetweenAttempts = 1000; // sleep time, in milliseconds

            int attempts = 0;
            while (!success && attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    callback(obj);
                    success = true;
                }
                catch (Exception ex)
                {
                    if (attempts == maxAttempts || ex.GetType() != filterExceptionType)
                    {
                        throw ex;
                    }
                    else
                    {
                        Thread.Sleep(timeBetweenAttempts);
                    }
                }
            }
        }
        internal static DependencyObject GetRoot(DependencyObject fe)
        {
            if (fe == null)
            {
                return null;
            }

            DependencyObject parent = fe;
            DependencyObject newparent = LogicalTreeHelper.GetParent(parent);

            while (newparent != null)
            {
                parent = newparent;
                newparent = LogicalTreeHelper.GetParent(parent);
            }

            return parent;
        }

        /// <summary>
        /// Starts an executable with command line parameters.
        /// </summary>
        internal static int RunProcess(string processRelPath, string cmdLine, string workingDirectory)
        {
            Process winProcess = null;
            int retCode = -1;

            try
            {
                winProcess = new Process();

                ProcessStartInfo winProcessStartInfo = new ProcessStartInfo();

                winProcessStartInfo.FileName = processRelPath;
                winProcessStartInfo.Arguments = cmdLine;

                winProcessStartInfo.UseShellExecute = false;
                winProcessStartInfo.RedirectStandardOutput = true;

                winProcessStartInfo.WorkingDirectory = workingDirectory;

                winProcess.StartInfo = winProcessStartInfo;

                System.Security.Permissions.SecurityPermission permission = new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode);

                permission.Assert();

                try
                {
                    winProcess.Start();

                    string standardOutput = winProcess.StandardOutput.ReadToEnd();

                    Console.WriteLine(standardOutput);

                    winProcess.WaitForExit();

                    retCode = winProcess.ExitCode;
                }
                finally
                {
                    System.Security.CodeAccessPermission.RevertAssert();
                }
            }
            finally
            {
                if (winProcess != null)
                    winProcess.Dispose();
            }

            return retCode;
        }

        /// <summary>
        /// Retrieves the Win32 handle of an arbitrary Avalon Window.
        /// </summary>
        public static HandleRef GetHandle(ICollection objects)
        {
            
            HandleRef handle = NativeMethods.NullHandleRef;

            //
            // Look for visible object with size.
            //
            foreach(Window win in objects)
            {
                HwndSource source = (HwndSource)PresentationSource.FromVisual(win);
                
                HandleRef tempHandle = NativeMethods.NullHandleRef;
                if (source != null)
                {
                    tempHandle = new HandleRef(source, source.Handle);
                }

                if (!NativeMethods.IsWindowVisible(tempHandle))
                {
                    continue;
                }

                handle = tempHandle;
                break;
            }

            return handle;
        }



        /// <summary>
        /// Returns whether or not an action should throttle its behavior.
        /// </summary>
        /// <param name="denominator">
        /// Controls the probability of throttling. 
        /// The lower the number, the more likely True will be returned.  
        /// A value of 1 will always return True.
        /// </param>
        /// <remarks>
        /// Actions often need to tune their behavior in order to prevent
        /// breaking the constraints of the overall stress app.  For example,
        /// an action that grows the size of a tree might need to prune the tree
        /// periodically in order to avoid using excessive memory for a long time. 
        /// 
        /// ShouldThrottle provides a simple mechanism for actions to make choices
        /// based on simple probabilites.  For example, the action that grows a tree
        /// could choose to prune the tree on average 1 out of every 3 times it runs.
        /// To do that, it can call ShouldThrottle(3) and prune if it returns true.
        /// </remarks>
        /// <returns>True if the caller should modify its behavior to reduce resources.</returns>
        public static bool ShouldThrottle(int denominator)
        {
            return ShouldThrottle(1, denominator);
        }
        /// <summary>
        /// Returns whether or not an action should throttle its behavior.
        /// </summary>
        /// <param name="numerator">
        /// Controls the probability of throttling as a fraction of the denominator.
        /// </param>
        /// <param name="denominator">
        /// Controls the probability of throttling. 
        /// </param>
        /// <remarks>
        /// Actions often need to tune their behavior in order to prevent
        /// breaking the constraints of the overall stress app.  For example,
        /// an action that grows the size of a tree might need to prune the tree
        /// periodically in order to avoid using excessive memory for a long time. 
        /// 
        /// ShouldThrottle provides a simple mechanism for actions to make choices
        /// based on simple probabilites.  For example, the action that grows a tree
        /// could choose to prune the tree on average 1 out of every 3 times it runs.
        /// To do that, it can call ShouldThrottle(1, 3) and prune if it returns true.
        /// </remarks>
        /// <returns>True if the caller should modify its behavior to reduce resources.</returns>
        public static bool ShouldThrottle(int numerator, int denominator)
        {
            return numerator > DefaultRandomGenerator.GetGlobalRandomGenerator().Next(denominator);
        }


        /// <summary>
        /// </summary>
        public static List<string> DefaultResourcesFiles = null;

        /// <summary>
        /// </summary>
        public static List<string> DefaultXamlFiles = null;

        /// <summary>
        /// </summary>
        public static List<string> DefaultDllFiles = null;


        /// <summary>
        /// </summary>
        public static bool CheckDefaultFiles(out string message)
        {
            _EnsureInitialized();
            return CheckDefaultFiles(_xamlDefinition.SupportFilesPath, out message);
        }

        /// <summary>
        /// </summary>
        public static bool CheckDefaultFiles(string rootDirectory, out string message)
        {
            _EnsureInitialized();            
            List<string> fileList = new List<string>(15);
            fileList.AddRange(DefaultResourcesFiles);
            fileList.AddRange(DefaultXamlFiles);
            fileList.AddRange(DefaultXamlFiles);
            
            foreach(string s in fileList)
            {
                string file = Path.Combine(rootDirectory,s);

                if (!File.Exists(file))
                {
                    message = file;
                    return false;
                }
            }
            
            message = "";
            return true;
        }


        /// <summary>
        /// </summary>
        public static void ValidateDefaultFiles()
        {
            _EnsureInitialized();            
            ValidateDefaultFiles(_xamlDefinition.SupportFilesPath);
        }


        /// <summary>
        /// </summary>
        public static void ValidateDefaultFiles(string rootDirectory)
        {
            _EnsureInitialized();            
            string msg = "";
            
            if (!CheckDefaultFiles(rootDirectory, out msg))
            {
                throw new MissedSupportFilesException(msg);
            }
        }

        /// <summary>
        /// </summary>
        public static void CopyDefaultFiles(string originDirectory, string targetDirectory)
        {
            _EnsureInitialized();            
            ValidateDefaultFiles(originDirectory);
            
            List<string> fileList = new List<string>();
            fileList.AddRange(DefaultResourcesFiles);
            fileList.AddRange(DefaultXamlFiles);      
            
            foreach(string s in fileList)
            {
                string fileName = s;

                if (Path.IsPathRooted(s))
                {
                    fileName = Path.GetFileName(s);
                }

                File.Copy(
                    Path.Combine(originDirectory, fileName),
                    Path.Combine(targetDirectory, fileName), 
                    false);                
            }
        }
    }
}


