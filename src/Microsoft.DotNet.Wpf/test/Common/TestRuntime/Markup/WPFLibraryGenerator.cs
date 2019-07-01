// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class OriginalXamlFilePathAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="originalXamlPath"></param>
        public OriginalXamlFilePathAttribute(string originalXamlPath)
        {
            _originalXaml = originalXamlPath;
        }

        /// <summary>
        /// 
        /// </summary>
        public string OriginalXamlPath
        {
            get
            {
                return _originalXaml;
            }
        }

        private string _originalXaml = "";
    }

    /// <summary>
    /// </summary>
    public class SetupCompileTestException : Exception
    {

        /// <summary>
        /// Passes message parameter to base constructor.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        public SetupCompileTestException(string message)
            : base(message)
        {

        }

        
        /// <summary>
        /// This constructor is required for deserializing this type across AppDomains.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public SetupCompileTestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }

    /// <summary>
    /// </summary>
    public class CompileTestException : Exception
    {

       

        
        /// <summary>
        /// This constructor is required for deserializing this type across AppDomains.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public CompileTestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        
    }

    /// <summary>
    /// </summary>
    public struct BamlXamlPair
    {
        /// <summary>
        /// </summary>
        public BamlXamlPair(Type type, String xamlFile)
        {
            this._type = type;
            this._xamlFile = xamlFile;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bamlRootObject"></param>
        /// <param name="xamlRootObject"></param>
        public void CreatePairObjects(out object  bamlRootObject, out object xamlRootObject)
        {
            
            string path = Path.GetDirectoryName(_xamlFile);
            using (Stream ms = CleanXamlFromCompilerOnlyFeatures())
            {
                ParserContext pc = new ParserContext();
                pc.BaseUri = new Uri(path);
                xamlRootObject = System.Windows.Markup.XamlReader.Load(ms, pc);               
            }

            bamlRootObject = Activator.CreateInstance(_type);

            if (bamlRootObject is System.Windows.Markup.IComponentConnector)
            {
                ((System.Windows.Markup.IComponentConnector)bamlRootObject).InitializeComponent();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public Stream GetXamlStream()
        {
            using (FileStream fs = new FileStream(_xamlFile, FileMode.Open, FileAccess.Read))
            {
                return WPFLibraryGenerator.CopyStream(fs);                
            }
        }


        private Stream CleanXamlFromCompilerOnlyFeatures()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xamlFile);

            XmlElement element = xmlDoc.DocumentElement;
            element.RemoveAttributeNode((XmlAttribute)element.GetAttributeNode("Class", WPFXamlGenerator.XamlURI));

            MemoryStream ms = new MemoryStream();
            xmlDoc.Save(ms);
            
            xmlDoc = null;
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <summary>
        /// </summary>
        private Type _type;

        /// <summary>
        /// </summary>
        private string _xamlFile;
    }


    /// <summary>
    /// </summary>
    public class WPFLibrary
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="generator"></param>
        internal WPFLibrary(WPFLibraryGenerator generator)
        {
            if (generator == null)
                throw new ArgumentNullException("generator");
            
            _libraryGenerator = generator;
        }

        /// <summary>
        /// </summary>
        public string Path
        {
            get
            {
                if (IsLibraryCreated)
                {
                    return _libraryGenerator.LibraryFile;
                }
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsLibraryCreated
        {
            get
            {
                return _libraryGenerator.IsLibraryCreated;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetRandomBamlFilePath()
        {
            return _libraryGenerator.GetRandomBamlFile();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Stream GetRandomBamlStream()
        {
            return _libraryGenerator.GetRandomBamlStream();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public BamlXamlPair[] GetBamlXamlPairs()
        {
            return _libraryGenerator.GetBamlXamlPairs();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Type GetRandomType()
        {
            return _libraryGenerator.GetRandomObject(typeof(object));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Type GetRandomType(Type derivedFrom)
        {
            return _libraryGenerator.GetRandomObject(derivedFrom);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object GetRandomObject()
        {
            return _libraryGenerator.GetRandomObject(typeof(object));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object GetRandomObject(Type derivedFrom)
        {
            object o = Activator.CreateInstance(GetRandomType(derivedFrom));

            if (o is System.Windows.Markup.IComponentConnector)
            {
                ((System.Windows.Markup.IComponentConnector)o).InitializeComponent();
            }
            
            return o;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Dispose()
        {
            _libraryGenerator.Dispose();
        }


        
        WPFLibraryGenerator _libraryGenerator = null;
    }

    /// <summary>
    /// </summary>
    public class WPFLibraryParams
    {
        /// <summary>
        /// </summary>
        public WPFLibraryParams()
        {
            AmountXamls = 10;
            AmountResourceDictionaries = 0;
            ResourceFiles = null;
            XamlPages = null;
            References = null;
            RootBuildPath = Environment.CurrentDirectory;
            LibraryBinPlacePath = Environment.CurrentDirectory;
        }

        /// <summary>
        /// </summary>
        public int AmountXamls;

        /// <summary>
        /// </summary>
        public int AmountResourceDictionaries;        

        /// <summary>
        /// </summary>
        public string[] ResourceFiles;

        /// <summary>
        /// </summary>
        public string[] XamlPages;

        /// <summary>
        /// </summary>
        public string[] References;

        /// <summary>
        /// </summary>
        public string RootBuildPath;

        /// <summary>
        /// </summary>
        public string LibraryBinPlacePath;


    }


    /// <summary>
    /// </summary>
    public class WPFLibraryGenerator
    {        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static WPFLibrary Generate()
        {
            return Generate(new WPFLibraryParams());
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="amountXamls"></param>
        /// <param name="resourceFiles"></param>
        /// <param name="xamlPages"></param>
        /// <param name="references"></param>
        /// <returns></returns>
        public static WPFLibrary Generate(
                                            int amountXamls, 
                                            string[] resourceFiles, 
                                            string[] xamlPages,
                                            string[] references)
        {
            WPFLibraryParams wpfLibraryParams = new WPFLibraryParams();

            wpfLibraryParams.AmountXamls = amountXamls;
            wpfLibraryParams.References = references;
            wpfLibraryParams.ResourceFiles = resourceFiles;
            wpfLibraryParams.XamlPages = xamlPages;
            
            return Generate(wpfLibraryParams);
        }



        /// <summary>
        /// 
        /// </summary>
        public static WPFLibrary Generate(
                                            int amountXamls, 
                                            int amountResourceDictionaries,
                                            string[] resourceFiles, 
                                            string[] xamlPages,
                                            string[] references)
        {
            WPFLibraryParams wpfLibraryParams = new WPFLibraryParams();

            wpfLibraryParams.AmountXamls = amountXamls;
            wpfLibraryParams.AmountResourceDictionaries = amountResourceDictionaries;
            wpfLibraryParams.References = references;
            wpfLibraryParams.ResourceFiles = resourceFiles;
            wpfLibraryParams.XamlPages = xamlPages;
            
            return Generate(wpfLibraryParams);
        }


        /// <summary>
        /// 
        /// </summary>
        public static WPFLibrary Generate(WPFLibraryParams wpfLibraryParams)
        {
        
            WPFXamlGenerator.ValidateDefaultFiles();

            List<string> list = new List<string>();

            if (wpfLibraryParams.ResourceFiles != null)
            {
                list.AddRange(wpfLibraryParams.ResourceFiles);
            }
            if (wpfLibraryParams.XamlPages != null)
            {
                list.AddRange(wpfLibraryParams.XamlPages);
            }
            if (wpfLibraryParams.References != null)
            {
                list.AddRange(wpfLibraryParams.References);
            }            

            ThrowIfNoAbsolutePath(list);

            WPFLibraryGenerator g = new WPFLibraryGenerator(wpfLibraryParams);
           
           return new WPFLibrary(g);
        }


        internal void Dispose()
        {

        }



        /// <summary>
        /// </summary>
        private WPFLibraryGenerator(WPFLibraryParams wpfLibraryParams)
        {            
            _wpfLibraryParams = wpfLibraryParams;
            _amountResourceDictionaries = wpfLibraryParams.AmountResourceDictionaries;
            _amountXaml = wpfLibraryParams.AmountXamls;

            Guid directorySuffix = Guid.NewGuid();
            _directorySuffix = directorySuffix;
            _buildPath = Path.Combine(wpfLibraryParams.RootBuildPath, _buildPathPrefix + directorySuffix.ToString("N"));

            _assemblyName +=  directorySuffix.ToString("N");


            _resourceFiles = wpfLibraryParams.ResourceFiles;
            _xamlPages = wpfLibraryParams.XamlPages;
            _references = wpfLibraryParams.References;            

            _finalDLLPath = Path.Combine(wpfLibraryParams.LibraryBinPlacePath, _assemblyName + ".dll");

        }

        /// <summary>
        /// </summary>
        internal bool IsLibraryCreated
        {
            get
            {
                return _isbuiltOK;
            }
        }

        /// <summary>
        /// </summary>
        internal string GetRandomBamlFile()
        {
            if (!IsLibraryCreated)
            {
                return null;
            }
        
            if (_bamlFiles == null)
            {
                lock(_instanceLock)
                {
                    if (_bamlFiles == null)
                    {
                        _bamlFiles = Directory.GetFiles(_buildPath,"*.baml",SearchOption.AllDirectories);
                    }
                }
            }

            string bamlFile = _bamlFiles[DefaultRandomGenerator.GetGlobalRandomGenerator().Next(_bamlFiles.Length)];

            return bamlFile;            
        }




        /// <summary>
        /// </summary>
        internal Stream GetRandomBamlStream()
        {

            string bamlFile = GetRandomBamlFile();

            if (String.IsNullOrEmpty(bamlFile))
            {
                return null;
            }
                

            using (FileStream reader = new FileStream(bamlFile, FileMode.Open, FileAccess.Read))
            {
                return CopyStream(reader);
            }
            
        }


        /// <summary>
        /// </summary>
        internal BamlXamlPair[] GetBamlXamlPairs()
        {
            if (!IsLibraryCreated)
            {
                return null;
            }

            return _bamlXaml.ToArray();
        }


        /// <summary>
        /// </summary>
        internal Type GetRandomObject(Type baseType)
        {
            if (!IsLibraryCreated)
            {
                return null;
            }
            int index;
            if (baseType != typeof(object))
            {
                List<Type> types = new List<Type>(_types.Length);

                foreach(Type type in _types)
                {
                    if (baseType.IsAssignableFrom(type))
                    {
                        types.Add(type);
                    }
                }

                if (types.Count == 0)
                {
                    return null;
                }
                
                index = DefaultRandomGenerator.GetGlobalRandomGenerator().Next(types.Count);

                return types[index];            
            }

            index = DefaultRandomGenerator.GetGlobalRandomGenerator().Next(_types.Length);
            return _types[index];         
        }

      

        /// <summary>
        /// </summary>
        internal string LibraryFile
        {
            get
            {
                return _finalDLLPath;
            }
        }

       

        private void WriteXamlFile(string xamlFullName, string content)
        {        
            using (FileStream fs = new FileStream(xamlFullName ,FileMode.CreateNew, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs,Encoding.Unicode))
                {
                    writer.Write(content);
                    writer.Flush();
                }                
            }

            if (!File.Exists(xamlFullName))
            {
                throw new SetupCompileTestException("The file " + xamlFullName +" was not generated correctly" );
            }

        }

        private void WriteCSFileForXamlFile(string xamlFileName, string className, string originalXaml)
        {
            string classNameHelper = className.Substring(className.LastIndexOf(".")+1);
            
            using (FileStream fs = new FileStream(xamlFileName ,FileMode.CreateNew, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs,Encoding.Unicode))
                {
                    writer.WriteLine("using System;");
                    writer.WriteLine("namespace Microsoft.Test");
                    writer.WriteLine("{");
                    writer.WriteLine("  [Microsoft.Test.Markup.OriginalXamlFilePath(@\"" + originalXaml + "\")]");
                    writer.WriteLine("  public partial class " + classNameHelper + "{}");
                    writer.WriteLine("}");
                    writer.Flush();
                }
                
            }

            if (!File.Exists(xamlFileName))
            {
                throw new SetupCompileTestException("The file " + xamlFileName +" was not generated correctly" );
            }



        }

        private  void BuildBamlXamlPairs(Type type)
        {
            object[] oArray = type.GetCustomAttributes(typeof(OriginalXamlFilePathAttribute), false);
            if (oArray == null || oArray.Length == 0)
            {
                return;
            }

            OriginalXamlFilePathAttribute attribute = (OriginalXamlFilePathAttribute)oArray[0];
            _bamlXaml.Add(new BamlXamlPair(type,attribute.OriginalXamlPath));
        }


        private void LoadAllTypes()
        {
            _assembly = Assembly.LoadFrom(_finalDLLPath);
            Type[] types = _assembly.GetTypes();
            List<Type> typeList = new List<Type>();

            for (int i=0;i<types.Length;i++)
            {
                if (types[i].IsClass && types[i].IsPublic)
                {
                    typeList.Add(types[i]);
                    BuildBamlXamlPairs(types[i]);
                }
            }
            _types = typeList.ToArray();                       
        }


        /// <summary>
        /// </summary>
        internal static Stream CopyStream(Stream reader)
        {
            MemoryStream writer = new MemoryStream();

            if (!reader.CanRead)
            {
                throw new InvalidOperationException("");
            }

            if (!writer.CanWrite)
            {
                throw new InvalidOperationException("");
            }

            int byteInfo = 0;
            
            while( (byteInfo = reader.ReadByte()) != -1)
            {
                writer.WriteByte(Convert.ToByte(byteInfo));
            }

            writer.Seek(0,SeekOrigin.Begin);
            
            return writer;
               
        }


        private static void ThrowIfNoAbsolutePath(List<string> list)
        {
            foreach(string s in list)
            {
                if (!Path.IsPathRooted(s))
                {
                    throw new ArgumentException("The file "+ s + "doesn't contain absolute path.");
                }

                if (!File.Exists(s))
                {
                    throw new ArgumentException("The file "+ s + "cannot be found.");
                }
            }
        }

        bool _isbuiltOK = false;
        object _instanceLock = new object();
        Assembly _assembly = null;       
        Type[] _types = null;
        int _amountXaml = 0;

        string _assemblyName = "CSDll_";
        string _buildPath = "";
        string[] _bamlFiles = null;
        Guid _directorySuffix;
        string _finalDLLPath = "";
        string[] _resourceFiles;
        string[] _xamlPages;
        string[] _references;
        int _amountResourceDictionaries = 0;
        List<BamlXamlPair> _bamlXaml = new List<BamlXamlPair>();
            

        static string _buildPathPrefix = "X_";
        static object _globalLock = new object();
        WPFLibraryParams _wpfLibraryParams = null;
    }

}


