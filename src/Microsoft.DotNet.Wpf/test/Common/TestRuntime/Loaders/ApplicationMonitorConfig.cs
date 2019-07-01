// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Security.Permissions;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Loaders 
{

    // Sample AppMonitorConfig:
    //     
    //    <AppMonitorConfig>
    //        <Using Namespace="STEP_NAMESPACE" Assembly="STEP_ASSEMBLY"/>
    //        <Using Namespace="STEP_NAMESPACE" Assembly="STEP_ASSEMBLY"/>
    //        <Using Namespace="STEP_NAMESPACE" Assembly="STEP_ASSEMBLY"/>
    //        <Steps>
    //            <STEP_TYPE_NAME [AlwaysRun="true/false"] />
    //            <STEP_TYPE_NAME [AlwaysRun="true/false"] CUSTOM_ATTRIB="CUSTOM_VALUE">
    //              <CUSTOM_ELEMENT ... />
    //          </STEP_TYPE_NAME>
    //        </Steps>
    //
    //    </AppMonitorConfig>
    // For further documentation see <doc>, or contact MattGal/CoromT

    /// <summary>
    /// Application Monitor Loader Config file parser
    /// </summary>
    public class ApplicationMonitorConfig 
    {

        #region Private Data

        // Assembly to look for Steps / Handlers in
        List<UsingStatement> usings = new List<UsingStatement>();
        // Steps to execute:
        List<LoaderStep> steps = new List<LoaderStep>();
        MainStep mainStep = new MainStep();

        #endregion
        
        #region Constructors

        /// <summary>
        /// Creates a new instance of the ApplicationMonitorConfig
        /// </summary>
        /// <param name="filename">filename of the config file to parse</param>
        public ApplicationMonitorConfig(string filename) 
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            //default usings...
            usings.Add(new UsingStatement(typeof(ApplicationMonitor).Assembly, "Microsoft.Test.Loaders.Steps"));
            usings.Add(new UsingStatement(typeof(ApplicationMonitor).Assembly, "Microsoft.Test.Loaders.UIHandlers"));

            #pragma warning disable 618
            //Get the using statements
            // LoadWithPartialName should be OK here since the only assemblies that I can find in Usings for AMC files 
            // are test assemblies (Which will therefore match CLR version)
            foreach (XmlElement usingElement in doc.DocumentElement.SelectNodes("Using"))
                usings.Add(new UsingStatement(Assembly.LoadWithPartialName(usingElement.GetAttribute("Assembly")), usingElement.GetAttribute("Namespace")));
            #pragma warning restore 618

            //Create all the steps
            foreach (XmlElement stepElement in doc.DocumentElement.SelectNodes("Steps/*")) 
            {
                LoaderStep step = ParseObject(stepElement) as LoaderStep;
                if (step == null)
                    throw new ArgumentException("The Steps element may only contain objects of Type LoaderStep");
                mainStep.AddChildStep(step);
            }
        }

        #endregion
        
        #region Public Members

        /// <summary>
        /// Runs all the steps
        /// </summary>
        /// <returns>returns whether one of the steps returned false</returns>
        public bool RunSteps() 
        {
            return mainStep.DoStep();
        }

        #endregion

        #region Private Implementation

        private object ParseObject(XmlElement element) 
        {
            // Create the object
            Type type = GetType(element.LocalName);
            object obj = Activator.CreateInstance(type, false);

            // Set all the attributes as properties
            foreach (XmlAttribute att in element.Attributes)
                ParseProperty(obj, att);

            foreach (XmlElement child in element.SelectNodes("*")) 
            {
                if (child.LocalName.StartsWith(element.LocalName + ".")) 
                {
                    //set the verbox property syntax
                    ParseProperty(obj, child);
                }
                else if (obj is LoaderStep) 
                {
                    //Adding Child LoaderStep to another LoaderStep
                    object childStep = ParseObject(child);
                    if (!(childStep is LoaderStep))
                        throw new ArgumentException("You can not add objects of type " + childStep.GetType().FullName + " as a child of a Step. You may only add other steps");
                    ((LoaderStep)obj).AddChildStep((LoaderStep)childStep);
                }
                else 
                {
                    if (obj is IList) 
                    {
                        object childobject = ParseObject(child);
                        ((IList)obj).Add(childobject);
                    }
                    else
                        throw new ArgumentException("You cannot add a child to objects of type " + obj.GetType().FullName + ". The Type must be a LoaderStep or implement IList.");
                }
            }

            return obj;
        }

        private void ParseProperty(object obj, XmlNode node) 
        {
            //attribute
            if (node is XmlAttribute) 
            {
                XmlAttribute att = (XmlAttribute)node;
                SetMemberValue(obj, att.LocalName, att.Value);
            }
            else if (node is XmlElement) 
            {
                XmlElement element = (XmlElement)node;
                XmlNodeList childElements = element.SelectNodes("*");
                string memberName = element.LocalName.Substring(element.LocalName.IndexOf('.') + 1);
                MemberInfo member = GetPublicFieldOrProperty(obj, memberName);
                Type propType = (member is PropertyInfo) ? ((PropertyInfo)member).PropertyType : ((FieldInfo)member).FieldType;
                bool isReadOnly = (member is PropertyInfo) ? !((PropertyInfo)member).CanWrite : ((FieldInfo)member).IsInitOnly;
                Type ilistType = propType.GetInterface(typeof(IList).FullName);
                
                if (isReadOnly) 
                {
                    if (ilistType != null) 
                    {
                        //read-only property that implement IList
                        IList list = (member is PropertyInfo) ? (IList)((PropertyInfo)member).GetValue(obj, null) : (IList)((FieldInfo)member).GetValue(obj);
                        foreach (XmlElement child in childElements)
                            list.Add(ParseObject(child));
                    }
                    else
                        throw new ArgumentException("You cannot set the value of the ReadOnly property " + memberName + " on type " + obj.GetType().FullName);
                }
                else if (propType.IsArray) 
                {
                    //writable array
                    ArrayList list = new ArrayList();
                    foreach (XmlElement child in childElements)
                        list.Add(ParseObject(child));
                    SetMemberValue(obj, member, list.ToArray(propType.GetElementType()));
                }
                else if (childElements.Count > 1) 
                {
                    //multiple children for a single property
                    throw new ArgumentException("A writable field or property can not contain multiple object values.");
                }
                else if (childElements.Count == 1) 
                {
                    //single children for a single property
                    SetMemberValue(obj, member, ParseObject((XmlElement)childElements[0]));
                }
                else 
                {
                    //no children for a single property * must be text
                    SetMemberValue(obj, member, ObjectFromString(propType, element.InnerText));
                }
            }
            else
                throw new ArgumentException("The node must be a XmlAttribute or XmlElement to set a property");
        }

        private void SetMemberValue(object obj, string name, string value) 
        {
            MemberInfo member = GetPublicFieldOrProperty(obj, name);
            Type propType = (member is PropertyInfo) ? ((PropertyInfo)member).PropertyType : ((FieldInfo)member).FieldType;

            SetMemberValue(obj, member, ObjectFromString(propType, value));
        }

        private void SetMemberValue(object obj, MemberInfo member, object value) 
        {
            if (member is PropertyInfo)
                ((PropertyInfo)member).SetValue(obj, value, new object[0]);
            else if (member is FieldInfo)
                ((FieldInfo)member).SetValue(obj, value);
            else
                throw new ArgumentException("Can only set the value of a field or property");
        }

        private MemberInfo GetPublicFieldOrProperty(object obj, string name) 
        {
            Type type = obj.GetType();
            MemberInfo[] members = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance);
            if (members.Length == 0)
                throw new ArgumentException("Cannot find a public property or field on type " + type.FullName + " with the name " + name);
            if (members.Length > 1)
                throw new ArgumentException("The Type " + type.FullName + " contains more then one overload with the name " + name + ". You may only have on member with this name.");
            if (!(members[0] is PropertyInfo || members[0] is FieldInfo))
                throw new ArgumentException("The Type " + type.FullName + " contains more then one overload with the name " + name + ". You may only have on member with this name.");
            return members[0];
        }
        
        private object ObjectFromString(Type type, string value) 
        {
            if (type == typeof(string))
                return value;
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (!converter.CanConvertFrom(typeof(string)))
                throw new ArgumentException("Cannot convert type " + type.FullName + " to from string (the type converter does not support this)");
            return converter.ConvertFromInvariantString(value);
        }
        
        //Gets a type using the partial type name by searching the config includes
        private Type GetType(string name) 
        {
            foreach (UsingStatement statement in usings) 
            {
                Type type = statement.Assembly.GetType(statement.Namespace + "." + name, false, true);
                if (type != null)
                    return type;
            }
            throw new ArgumentException("Could not find a Type in with the name '" + name + "' in any of the specified using Statements");
        }
        
        private struct UsingStatement 
        {
            public UsingStatement(Assembly assembly, string Namespace) 
            {
                this.Assembly = assembly;
                this.Namespace = Namespace;
            }

            public Assembly Assembly;
            public string Namespace;
        }
        
        // Helper class to allow instantiation of the abstract LoaderStep
        // Used as a virtual parent step for all the inorder steps.
        class MainStep : LoaderStep 
        {
        }

        #endregion

    }
}