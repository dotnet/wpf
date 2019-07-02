// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.ResourceFetcher
{
    #region usings
        using System;
        using System.IO;
        using System.Runtime.Serialization;
    #endregion usings;

    #region Temporary classes and interfaces
        /// <summary>
        /// Resource class
        /// </summary>
//        [SerializableAttribute]
        
        internal sealed class Resource : IDisposable //, ISerializable
        {
            #region Properties
                #region Private Properties
                    private string _description = string.Empty;
                    private string _name = string.Empty;
                    private string _extension = string.Empty;
                    private Stream _stream = null;
                #endregion Private Properties
                #region Public Properties
                    /// <summary>
                    /// Return the name associated with the resource
                    /// </summary>
                    /// <value></value>        
                    public string Name
                    {
                        get { return _name; }
                    }
                    /// <summary>
                    /// Return the extension associated with the resource
                    /// </summary>
                    /// <value></value>
                    public string Extension
                    {
                        get { return _extension; }
                    }
                    /// <summary>
                    /// Return the description associated with the resource
                    /// </summary>
                    /// <value></value>
                    public string Description
                    {
                        get { return _description; }
                    }
                #endregion Public Properties
            #endregion Properties

            #region Constructors
                /// <summary>
                /// Instanciate a new Resource object
                /// </summary>
                /// <param name="description"></param>
                /// <param name="fileName"></param>
                public Resource(string description, string fileName)
                {
                    _description = description;
                    _name = Path.GetFileNameWithoutExtension(fileName);
                    _extension = Path.GetExtension(fileName);
                }
                /// <summary>
                /// Instanciate a new Resource object
                /// </summary>
                /// <param name="description"></param>
                /// <param name="name"></param>
                /// <param name="extension"></param>
                /// <param name="data"></param>
                public Resource(string description, string name, string extension, byte[] data)
                {
                    _description = description;
                    _name = name;
                    _extension = extension;
                    _stream = new MemoryStream(data);
                }
                /// <summary>
                /// Instanciate a new Resource object
                /// </summary>
                /// <param name="description"></param>
                /// <param name="name"></param>
                /// <param name="extension"></param>
                /// <param name="stream"></param>
                public Resource(string description, string name, string extension, Stream stream) 
                {
                    _description = description;
                    _name = name;
                    _extension = extension;
                    _stream = stream;
                }
                /// <summary>
                /// Finalizer will get called in case user forgot to call Dispose
                /// </summary>
                ~Resource()
                {
                    if (_stream != null)
                    {
                        _stream.Close();
                        _stream = null;
                    }
                }
            #endregion Constructors

            #region Methods
                /// <summary>
                /// Return the Stream associate with the specified resource
                /// </summary>
                /// <returns></returns>
                public Stream GetStream()
                {
                    return _stream;
                }
                /// <summary>
                /// Return the Data associate with the specified resource
                /// </summary>
                /// <returns></returns>
                public byte[] GetData()
                {
                    if (_stream == null) { return null; }

                    byte[] retVal = null;
                    long position =_stream.Position;
                    _stream.Position = 0;
                    retVal = new byte[_stream.Length];
                    _stream.Read(retVal, 0, retVal.Length);
                    _stream.Position = position;

                    return retVal;
                }
                /// <summary>
                /// Save the specified resource into the specified file
                /// </summary>
                /// <param name="filename"></param>
                public void Save(string filename)
                {
                    FileStream fileStream = null;
                    try
                    {
                        fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                        byte[] buffer = GetData();
                        fileStream.Write(buffer, 0, buffer.Length);
                        fileStream.Flush();
                    }
                    finally
                    {
                        if (fileStream != null) { fileStream.Close(); fileStream = null; }
                    }
                }
            #endregion Methods

            #region IDisposable Members
                public void Dispose()
                {
                    if (_stream != null) 
                    { 
                        _stream.Close(); 
                        _stream = null;
                        GC.SuppressFinalize(this);
                    }
                }
            #endregion
        }
        /// <summary>
        /// ResourceKey abstract class
        /// </summary>
        [SerializableAttribute]
        
        public abstract class ResourceKey : ISerializable
        {
            #region Constructors
                /// <summary>
                /// Instantiate a new object deriving from ResourceKey
                /// </summary>
                protected ResourceKey() { }
                /// <summary>
                /// Serialization constructor
                /// </summary>
                /// <param name="info"></param>
                /// <param name="context"></param>
                protected ResourceKey(SerializationInfo info, StreamingContext context)
                { 
                }
            #endregion Constructors
            #region Methods
                /// <summary>
                /// Return the UniversalResourceId for this Key
                /// </summary>
                /// <returns></returns>
//@ review : Why is this a method instead of an abstract "get" property ?
                public abstract UniversalResourceId GetUniversalResourceId();
                /// <summary>
                /// Serialization  Method
                /// </summary>
                /// <param name="info"></param>
                /// <param name="context"></param>
                public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                    info.AddValue("UniversalResourceId", GetUniversalResourceId());
                }
            #endregion Methods
        }
        /// <summary>
        /// UniversalResourceId struct
        /// </summary>
        [SerializableAttribute]
        
        public struct UniversalResourceId : ISerializable
        {
            #region Properties
                #region Private Properties
                    private string _namespace;
                    private string _name;
                #endregion Private Properties
                #region Public Properties
                    /// <summary>
                    /// Get the namespace associated with this UniversalResourceId
                    /// </summary>
                    /// <value></value>
// @review : FxCop complains (reserved keyword) use another name instead.
            public string Namespace 
                    { 
                        get { return _namespace; } 
                    }
                    /// <summary>
                    /// Get the name associated with this UniversalResourceId
                    /// </summary>
                    /// <value></value>
                    public string Name 
                    { 
                        get { return _name; } 
                    }
                #endregion Public Properties
            #endregion Properties

            #region Constructors
                /// <summary>
                /// Instanciate a new UniversalResourceId object
                /// </summary>
                /// <param name="Namespace"></param>
                /// <param name="name"></param>
                public UniversalResourceId(string Namespace, string name) 
                {
                    _namespace = Namespace;
                    _name = name;
                }
                /// <summary>
                /// Serialization constructor
                /// </summary>
                /// <param name="info"></param>
                /// <param name="context"></param>
                private UniversalResourceId(SerializationInfo info, StreamingContext context)
                {
                    _namespace = (string)info.GetValue("Namespace", typeof(string));
                    _name = (string)info.GetValue("Name", typeof(string));
                }
            #endregion Constructors

            #region ISerializable Members
                /// <summary>
                /// Serialization Method
                /// </summary>
                /// <param name="info"></param>
                /// <param name="context"></param>
                public void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                    info.AddValue("Namespace", Namespace);
                    info.AddValue("Name", Name);
                }
            #endregion
        }
        /// <summary>
        /// IResource Provider interface definition
        /// </summary>
        
        internal interface IResourceProvider
        {
            #region Methods
                /// <summary>
                /// Retrieve the Resource associated with this ResourceKey
                /// </summary>
                /// <param name="key"></param>
                /// <returns></returns>
                Resource GetResource(ResourceKey key);
            #endregion Methods
        }
    #endregion Temporary classes and interfaces
}
