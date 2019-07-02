// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.ResourceFetcher
{
    #region usings
        using System;
        using System.IO;
        using System.Drawing;
        using System.Runtime.Serialization;
    #endregion usings;

    /// <summary>
    /// The ResourceKey for the EmbeddedReourceProvider
    /// </summary>
    [SerializableAttribute]
    public class EmbeddedResourceKey : ResourceKey, ISerializable
    {      
        #region Properties
            #region Private Properties
                private string _fileName = string.Empty;
                private ResourceType _resourceType = ResourceType.Icon;
                private object _resourceName = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the name of the file containing the resource to extract
                /// </summary>
                /// <value></value>
                public string FileName
                {
                    get { return _fileName; }
                }
                /// <summary>
                /// Get the type of resource to extract
                /// </summary>
                /// <value></value>
                public ResourceType ResourceType 
                { 
                    get { return _resourceType; } 
                }
                /// <summary>
                /// Get the name (or number) of the resource to extract
                /// </summary>
                /// <value></value>
                public object ResourceName
                {
                    get { return _resourceName; }
                }
            #endregion Public Properties
        #endregion Private Properties

        #region Constructors
            private EmbeddedResourceKey() { }
            /// <summary>
            /// Create a new EmbeddedResourceKey object
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="resourceType"></param>
            /// <param name="resourceName"></param>
            public EmbeddedResourceKey(string fileName, ResourceType resourceType, object resourceName) : this()
            {
                _fileName = fileName;
                _resourceType = resourceType;
                _resourceName = resourceName;
            }
            /// <summary>
            /// Serialization constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected EmbeddedResourceKey(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                _fileName = (string)info.GetValue("FileName", typeof(string));
                _resourceType = (ResourceType)info.GetValue("ResourceType", typeof(ResourceType));
                _resourceName = (object)info.GetValue("ResourceName", typeof(object));
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Get the UniversalResourceId for this object
            /// </summary>
            /// <returns></returns>
            public override UniversalResourceId GetUniversalResourceId()
            {
                return new UniversalResourceId("Embedded", _fileName + "#" + _resourceType + "#" + (string)_resourceName);
            }
        #endregion Methods

        #region ISerializable Members
            /// <summary>
            /// Serialization Method
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("FileName", FileName);
                info.AddValue("ResourceType", ResourceType);
                info.AddValue("ResourceName", ResourceName);
            }
        #endregion
    }
}
