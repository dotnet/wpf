// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Markup;


namespace Microsoft.Test.Serialization
{
    /// <summary>
    /// Wrap an internal BamlWriter by giving it a public api that mirrors the internal one.
    /// Use reflection to invoke the internal properties and methods.
    /// </summary>
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
    public class BamlWriterWrapper
    {
        /// <summary>
        /// 
        /// </summary>
        public BamlWriterWrapper(Stream stream)
        {
            _writerType = WrapperUtil.AssemblyPF.GetType("System.Windows.Markup.BamlWriter");
            _writer = Activator.CreateInstance(_writerType, new object[1] { stream });
            _bamlAttributeUsage = WrapperUtil.AssemblyPF.GetType("System.Windows.Markup.BamlAttributeUsage");
        }

        /// <summary>
        /// Write a StartDocument Baml Node
        /// </summary>
        public void WriteStartDocument()
        {
            _writerType.InvokeMember("WriteStartDocument",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }

        /// <summary>
        /// Write a EndDocument Baml Node
        /// </summary>
        public void WriteEndDocument()
        {
            _writerType.InvokeMember("WriteEndDocument",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }

        /// <summary>
        /// Write a ConnectionId Baml Node
        /// </summary>
        public void WriteConnectionId(Int32 connectionId)
        {
            _writerType.InvokeMember("WriteConnectionId",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { connectionId });
        }

        /// <summary>
        /// Write a RootElement Baml Node
        /// </summary>
        public void WriteRootElement(string assemblyName, string typeFullName)
        {
            _writerType.InvokeMember("WriteRootElement",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, typeFullName });
        }

        /// <summary>
        /// Write a StartElement Baml Node
        /// </summary>
        public void WriteStartElement(
            string assemblyName,
            string typeFullName,
            bool isInjected,
            bool useTypeConverter)
        {
            _writerType.InvokeMember("WriteStartElement",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, typeFullName, isInjected, useTypeConverter });
        }

        /// <summary>
        /// Write a EndElement Baml Node
        /// </summary>
        public void WriteEndElement()
        {
            _writerType.InvokeMember("WriteEndElement",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }

        /// <summary>
        /// Write a StartConstructor Baml Node
        /// </summary>
        public void WriteStartConstructor()
        {
            _writerType.InvokeMember("WriteStartConstructor",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }

        /// <summary>
        /// Write a EndConstructor Baml Node
        /// </summary>
        public void WriteEndConstructor()
        {
            _writerType.InvokeMember("WriteEndConstructor",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }

        /// <summary>
        /// Write Property Baml Node
        /// attributeUsage is of the BamlAttributeUsage enum but that is an Internal Type
        /// </summary>
        public void WriteProperty(
            string assemblyName,
            string ownerTypeFullName,
            string propName,
            string value,
            object attributeUsage)
        {
            _writerType.InvokeMember("WriteProperty",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, ownerTypeFullName, propName, value, attributeUsage });
        }

        /// <summary>
        /// Write Property Baml Node
        /// attributeUsage is of the BamlAttributeUsage enum but that is an Internal Type
        /// </summary>
        public void WriteProperty(
            string assemblyName,
            string ownerTypeFullName,
            string propName,
            string value,
            string attributeUsage)
        {
            _writerType.InvokeMember("WriteProperty",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, ownerTypeFullName, propName, value,
                                      Enum.Parse(_bamlAttributeUsage, attributeUsage) });
        }

        /// <summary>
        /// Write a XmlnsProperty Baml Node
        /// </summary>
        public void WriteXmlnsProperty(
            string localName,
            string xmlNamespace)
        {
            _writerType.InvokeMember("WriteXmlnsProperty",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { localName, xmlNamespace });
        }

        /// <summary>
        /// Write DefAttribute Baml Node
        /// </summary>
        public void WriteDefAttribute(
            string name,
            string value)
        {
            _writerType.InvokeMember("WriteDefAttribute",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { name, value });
        }

        /// <summary>
        /// Write WritePresentationOptionsAttribute Baml Node
        /// </summary>
        public void WritePresentationOptionsAttribute(
                string name,
                string value)
        {
            _writerType.InvokeMember("WritePresentationOptionsAttribute",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { name, value });
        }

        /// <summary>
        /// Write WriteContentProperty Baml Node
        /// </summary>
        public void WriteContentProperty(
            string assemblyName,
            string ownerTypeFullName,
            string propName)
        {
            _writerType.InvokeMember("WriteContentProperty",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, ownerTypeFullName, propName });
        }



        /// <summary>
        /// Write StartComplexProperty Baml Node
        /// </summary>
        public void WriteStartComplexProperty(
            string assemblyName,
            string ownerTypeFullName,
            string propName)
        {
            _writerType.InvokeMember("WriteStartComplexProperty",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, ownerTypeFullName, propName });
        }

        /// <summary>
        /// Write EndComplexProperty Baml Node
        /// </summary>
        public void WriteEndComplexProperty()
        {
            _writerType.InvokeMember("WriteEndComplexProperty",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }

        /// <summary>
        /// Write LiteralContent Baml Node
        /// </summary>
        public void WriteLiteralContent(
            string contents)
        {
            _writerType.InvokeMember("WriteLiteralContent",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { contents });
        }

        /// <summary>
        /// Write a PIMapping Baml Node
        /// </summary>
        public void WritePIMapping(
            string xmlNamespace,
            string clrNamespace,
            string assemblyName)
        {
            _writerType.InvokeMember("WritePIMapping",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { xmlNamespace, clrNamespace, assemblyName });
        }

        /// <summary>
        /// Write a Text Baml Node
        /// </summary>
        public void WriteText(
            string textContent,
            string typeConverterAssemblyName,
            string typeConverterTypeName)
        {
            _writerType.InvokeMember("WriteText",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { textContent, typeConverterAssemblyName, typeConverterTypeName });
        }

        /// <summary>
        /// Write a RoutedEvent Baml Node
        /// </summary>
        public void WriteRoutedEvent(
            string assemblyName,
            string ownerTypeFullName,
            string eventIdName,
            string handlerName)
        {
            _writerType.InvokeMember("WriteRoutedEvent",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, ownerTypeFullName, eventIdName, handlerName });
        }

        /// <summary>
        /// Write Event Baml Node
        /// </summary>
        public void WriteEvent(
            string eventName,
            string handlerName)
        {
            _writerType.InvokeMember("WriteEvent",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { eventName, handlerName });
        }

        /// <summary>
        /// Write a StartArray Baml Node
        /// </summary>
        public void WriteStartArray(
            string assemblyName,
            string typeFullName)
        {
            _writerType.InvokeMember("WriteStartArray",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { assemblyName, typeFullName });
        }

        /// <summary>
        /// Write a EndArray Baml Node
        /// </summary>
        public void WriteEndArray()
        {
            _writerType.InvokeMember("WriteEndArray",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }

        /// <summary>
        /// Close Underlying Baml Writer
        /// </summary>
        public void Close()
        {
            _writerType.InvokeMember("Close",
                                      WrapperUtil.MethodBindFlags, null, _writer,
                                      new object[] { });
        }


        Type _writerType;
        object _writer;
        Type _bamlAttributeUsage;
    }

}

