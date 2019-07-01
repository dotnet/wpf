// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml;

//TODO: Tidy this class up... It can be simplified a fair bit to good effect.

namespace Microsoft.Test.Stability.Extensions
{
    /// <summary>
    /// Reads lists of object types and returns an 
    /// </summary>
    public class TypeListReader
    {
        #region Public Members

        public static List<Type> ParseTypeList(string fileName, Type superClassType)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName + " does not exist.");
            }
            List<Type> foundTypes = new List<Type>();

            // Read full file...
            StringReader xmlReader = new StringReader(File.ReadAllText(fileName));
            XmlTextReader readerForTypeList = new XmlTextReader(xmlReader);
            // Holds default object to base other objects from
            TestComponentInfo defaultTestComponentInfo = new TestComponentInfo();

            while (readerForTypeList.Read())
            {
                if (readerForTypeList.NodeType == XmlNodeType.Element)
                {
                    switch (readerForTypeList.Name)
                    {
                        // Loading the default...
                        case "DefaultTestComponentInfo":
                            {
                                ParseTestComponentInfo(readerForTypeList, defaultTestComponentInfo, superClassType);
                                break;
                            }
                        // Loading a regular type
                        case "TestComponentInfo":
                            {
                                TestComponentInfo testComponentInfo = (TestComponentInfo)defaultTestComponentInfo.Clone();
                                Type newlyFoundType = ParseTestComponentInfo(readerForTypeList, testComponentInfo, superClassType);
                                if (newlyFoundType == null)
                                {
                                    throw new InvalidOperationException("The testInfo for type: " + testComponentInfo.Type + " could not be successfully loaded.");
                                }
                                AddToList(foundTypes, newlyFoundType, superClassType);
                                break;
                            }
                        // Loading a list of types from a file
                        case "LoadFile":
                            {
                                string fileToLoad = DetermineFilePath(readerForTypeList, fileName);
                                List<Type> typesFoundInFile = ParseTypeList(fileToLoad, superClassType);
                                AddToList(foundTypes, typesFoundInFile, superClassType);
                                break;
                            }
                        // Loading all matching types from an assembly file
                        case "LoadAllTypesFromAssembly":
                            {
                                string fileToLoad = readerForTypeList.GetAttribute("FileName");
                                List<Type> typesFoundInFile = GatherTypesFromAssembly(fileToLoad, superClassType);
                                AddToList(foundTypes, typesFoundInFile, superClassType);
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
            xmlReader.Close();
            return foundTypes;
        }

        #endregion

        #region Private Implementation

        private static string DetermineFilePath(XmlTextReader readerForTypeList, string existingPath)
        {
            string fileToLoad = readerForTypeList.GetAttribute("FileName");
            if (String.IsNullOrEmpty(fileToLoad))
            {
                throw new InvalidOperationException("Cannot specify LoadFile without specifying FileName!");
            }
            // Only works for current directory at the moment.
            // Either qualify your path, have the file in the directory, or change this code.
            if (!File.Exists(fileToLoad))
            {
                // Second chance... try to get it from the same path as the other file
                if (File.Exists(Path.Combine(Path.GetDirectoryName(existingPath), fileToLoad)))
                {
                    fileToLoad = Path.Combine(Path.GetDirectoryName(existingPath), fileToLoad);
                }
                else
                {
                    throw new FileNotFoundException("Cannot find " + fileToLoad);
                }
            }
            return fileToLoad;
        }

        private static List<Type> GatherTypesFromAssembly(string assemblyToLoad, Type superClassType)
        {
            List<Type> results = new List<Type>();
            Assembly searchAssembly = Load(assemblyToLoad);
            Type[] typeList = searchAssembly.GetTypes();
            foreach (Type t in typeList)
            {
                if (t.IsSubclassOf(superClassType) && !t.IsAbstract)
                {
                    results.Add(t);
                }
            }
            return results;
        }

        private static Assembly Load(string assemblyToLoad)
        {
            Assembly assembly;
            //HACK: Directly Referencing Testruntime to get around Assembly.LoadFromPartialName method insanity
            if (0 == assemblyToLoad.ToLower().CompareTo("testruntime"))
            {
                assembly = typeof(TypeListReader).Assembly;
            }
            else
            {
                string fileToLoad = Directory.GetCurrentDirectory().ToString() + "\\" + assemblyToLoad;
                assembly = Assembly.LoadFile(fileToLoad);
            }
            return assembly;
        }
        #endregion

        private static void AddToList(List<Type> previouslyDiscoveredTypes, List<Type> newlyDiscoveredTypes, Type parentType)
        {
            foreach (Type loadedType in newlyDiscoveredTypes)
            {
                AddToList(previouslyDiscoveredTypes, loadedType, parentType);
            }
        }

        private static void AddToList(List<Type> previouslyDiscoveredTypes, Type loadedType, Type parentType)
        {
            // Can only have one instance per type...
            if (previouslyDiscoveredTypes.Contains(loadedType))
            {
                throw new InvalidOperationException("Type " + loadedType.Name + " was already loaded!  All types used in stress Type discovery must be unique");
            }
            // Can only load types that are subclasses of the referenced type
            if (!loadedType.IsSubclassOf(parentType))
            {
                throw new InvalidOperationException("Type " + loadedType.Name + " must be derived from " + parentType.Name);
            }
            if (loadedType.IsAbstract)
            {
                throw new InvalidOperationException("Type " + loadedType.Name + " must be concrete, not abstract.");
            }
            previouslyDiscoveredTypes.Add(loadedType);
        }

        private static Type ParseTestComponentInfo(XmlTextReader componentXmlTextReader, TestComponentInfo testInfo, Type mustDeriveFromType)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(TestComponentInfo));

            // Parse all the attributes as properties of TestComponentInfo
            // Largely "borrowed" from XTCAdapter :)
            if (componentXmlTextReader.MoveToFirstAttribute())
            {
                do
                {
                    PropertyDescriptor prop = properties[componentXmlTextReader.Name];
                    if (prop == null)
                        throw new InvalidOperationException("The property " + componentXmlTextReader.Name + " does not exist on TestComponentInfo.");
                    componentXmlTextReader.ReadAttributeValue();
                    TypeConverter converter = prop.Converter;
                    object value = converter.ConvertFromInvariantString(componentXmlTextReader.Value);
                    prop.SetValue(testInfo, value);
                }
                while (componentXmlTextReader.MoveToNextAttribute());
            }

            if (!String.IsNullOrEmpty(testInfo.Type))
            {
                Assembly moduleToCheckForType=Load(testInfo.Assembly);

                return Type.GetType(testInfo.Type + "," + moduleToCheckForType.FullName);
            }
            return null;
        }

        private class TestComponentInfo : ICloneable
        {
            #region Private Members
            private string type = "";
            private string assembly = "";
            #endregion

            #region Public Members
            #region ICloneable Members

            public object Clone()
            {
                TestComponentInfo copy = new TestComponentInfo();
                copy.assembly = this.assembly;
                copy.type = this.type;
                return copy;
            }
            #endregion

            /// <summary>
            /// Stress object type to be instantiated.  Must be unique
            /// </summary>
            public string Type
            {
                get { return type; }
                set { type = value; }
            }

            /// <summary>
            /// Assembly that this type should be loaded from
            /// </summary>
            public string Assembly
            {
                get { return assembly; }
                set { assembly = value; }
            }
            #endregion
        }
    }
}
