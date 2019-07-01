// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using System.Xml;
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
    /// Wrap an internal BamlReader by giving it a public api that mirrors the internal one.
    /// Use reflection to invoke the internal properties and methods.
    /// </summary>
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
    public class BamlReaderWrapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fs"></param>
        public BamlReaderWrapper(Stream fs)
        {
            _readerType = WrapperUtil.AssemblyPF.GetType("System.Windows.Markup.BamlReader");
            _reader = Activator.CreateInstance(_readerType, new object[1] { fs });
        }

        /// <summary>
        /// Return the number of properties.  Note that this 
        /// does not include complex properties or children elements
        /// </summary>
        public int PropertyCount
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("PropertyCount", WrapperUtil.PropertyBindFlags);
                return (int)pi.GetValue(_reader, null);
            }
        }

        /// <summary>
        /// Return true if the current node has any simple properties
        /// </summary>
        public bool HasProperties
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("HasProperties", WrapperUtil.PropertyBindFlags);
                return (bool)pi.GetValue(_reader, null);
            }
        }

        /// <summary>
        /// Return the connection Id of current element for hooking up
        /// IDs and events.
        /// </summary>
        public Int32 ConnectionId
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("ConnectionId", WrapperUtil.PropertyBindFlags);
                return (Int32)pi.GetValue(_reader, null);
            }
        }

        /// <summary>
        /// Defines what this attribute is used for such as being an alias for
        /// xml:lang, xml:space or x:ID
        /// This returns a value of BamlAttributeUsage enum but that is an Internal Type
        /// </summary>
        public object AttributeUsage
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("AttributeUsage", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null);

            }
        }

        /// <summary>
        /// Gets the type of the current node (eg  Element, StartComplexProperty,  
        /// Text, etc)
        /// This returns a value of BamlNodeType enum but that is an Internal Type
        /// </summary>
        public string NodeType
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("NodeType", WrapperUtil.PropertyBindFlags);
                object o = pi.GetValue(_reader, null);
                return o.ToString();
            }
        }

        /// <summary>
        /// Gets the fully qualified name of the current node.
        /// </summary>
        public string Name
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("Name", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as string;
            }
        }

        /// <summary>
        /// Gets the local name only, with prefix and owning class removed
        /// </summary>
        public string LocalName
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("LocalName", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as String;
            }
        }

        /// <summary>
        /// Gets the owner type name only, with prefix removed
        /// </summary>
        /// <remarks>
        /// Applies to properties and events only
        /// </remarks>
        public string OwnerTypeName
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("OwnerTypeName", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as string;
            }
        }

        /// <summary>
        /// Gets the prefix associated with the current node, if there is one
        /// </summary>
        public string Prefix
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("Prefix", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as string;
            }
        }

        /// <summary>
        /// Gets the assembly name associated with the type of the current node, if there is one
        /// </summary>
        public string AssemblyName
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("AssemblyName", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as string;
            }
        }

        /// <summary>
        /// Gets the XML namespace URI of the node on which the reader is positioned
        /// </summary>
        public string XmlNamespace
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("XmlNamespace", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as string;
            }
        }

        /// <summary>
        /// Gets the CLR namespace of the node on which the reader is positioned
        /// </summary>
        public string ClrNamespace
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("ClrNamespace", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as string;
            }
        }

        /// <summary>
        /// Gets the text value of the current node (eg  property value or text content)
        /// </summary>
        public string Value
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("Value", WrapperUtil.PropertyBindFlags);
                return pi.GetValue(_reader, null) as string;
            }
        }


        /// <summary>    
        /// Gets the state of the reader.
        /// </summary>
        public ReadState ReadState
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("ReadState", WrapperUtil.PropertyBindFlags);
                return (ReadState)pi.GetValue(_reader, null);
            }
        }

        /// <summary>    
        /// Whether the element was injected and should be ignored
        /// </summary>
        public bool IsInjected
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("IsInjected", WrapperUtil.PropertyBindFlags);
                return (bool)pi.GetValue(_reader, null);
            }
        }

        /// <summary>    
        /// Whether this object instance is expected to be created via TypeConverter
        /// </summary>
        public bool CreateUsingTypeConverter
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("CreateUsingTypeConverter", WrapperUtil.PropertyBindFlags);
                return (bool)pi.GetValue(_reader, null);
            }
        }

        /// <summary>
        /// If a text recored is typeconverter input and the typeconverter is known
        /// </summary>
        public string TypeConverterName
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("TypeConverterName", WrapperUtil.PropertyBindFlags);
                return (string)pi.GetValue(_reader, null);
            }
        }

        /// <summary>
        /// If a text recored is typeconverter input and the typeconverter is known
        /// </summary>
        public string TypeConverterAssemblyName
        {
            get
            {
                PropertyInfo pi = _readerType.GetProperty("TypeConverterAssemblyName", WrapperUtil.PropertyBindFlags);
                return (string)pi.GetValue(_reader, null);
            }
        }

        /// <summary>
        /// Reads the next node from the stream.
        /// </summary>
        public bool Read()
        {
            return (bool)_readerType.InvokeMember("Read",
                                      WrapperUtil.MethodBindFlags, null, _reader,
                                      new object[] { });
        }

        /// <summary>
        /// Close the underlying BAML stream.  
        /// </summary>
        /// <remarks>
        /// Once the BamlReader is closed, it cannot be used
        /// for any further operations.  Calling any public interfaces will fail.
        /// </remarks>
        public void Close()
        {
            _readerType.InvokeMember("Close",
                                      WrapperUtil.MethodBindFlags, null, _reader,
                                      new object[] { });
        }

        /// <summary>
        /// Moves to the first property for this element or object.  
        /// Return true if property exists, false otherwise.
        /// </summary>
        public bool MoveToFirstProperty()
        {
            return (bool)_readerType.InvokeMember("MoveToFirstProperty",
                                      WrapperUtil.MethodBindFlags, null, _reader,
                                      new object[] { });
        }


        /// <summary>
        /// Move to the next property for this element or object.  
        /// Return true if there is a next property; false if there are no more properties.
        /// </summary>
        public bool MoveToNextProperty()
        {
            return (bool)_readerType.InvokeMember("MoveToNextProperty",
                                      WrapperUtil.MethodBindFlags, null, _reader,
                                      new object[] { });
        }

        Type _readerType;
        object _reader;
    }

}
