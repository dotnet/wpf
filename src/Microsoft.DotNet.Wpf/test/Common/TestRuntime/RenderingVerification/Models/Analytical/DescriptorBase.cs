// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical.Descriptors
{
    #region usings
        using System;
        using System.IO;
        using System.Collections;
        using System.Xml.Serialization;
        using System.Runtime.Serialization;
        using Microsoft.Test.RenderingVerification;
        using Microsoft.Test.RenderingVerification.Model.Analytical;
    #endregion usings

    /// <summary>
    /// Base class for any Descriptor.
    /// </summary>
    [SerializableAttribute()]
    public abstract class DescriptorBase : IDescriptor, ISerializable
    {
        #region Properties
            private string _name = string.Empty;
            private ArrayList _relation = new ArrayList();
            private Hashtable _descriptorDependentObjects = new Hashtable();
        #endregion Properties

        #region Interfaces implementation
            /// <summary>
            /// Descriptor Name 
            /// </summary>
            /// <value></value>
            public string Name 
            { 
                get
                {
                    return _name;
                }
                set 
                {
                    _name = value;
                }
            }
            /// <summary>
            /// Map specific descriptor characteristic (key = name, value = object)
            /// </summary>
            /// <value></value>
            public Hashtable DescriptorDependentObjects
            {
                get { return _descriptorDependentObjects; }
            }
            /// <summary>
            /// Collection of Criteria applied to this descriptor.
            /// </summary>
            /// <value></value>
            public abstract ICriterion[] Criteria { get;set;}
            /// <summary>
            /// The Descriptor's bounding rectangle 
            /// </summary>
            /// <value></value>
            public abstract RenderRect BoundingBox { get;}
            /// <summary>
            /// Add all the pixel participating in the shape
            /// </summary>
            /// <param name="pixels">The collection of "Pixel" paricipating in the shape</param>
            public abstract void SetParticipatingPixels(Pixel[] pixels);
            /// <summary>
            /// Compute all Descriptors for the bitmap.
            /// </summary>
            /// <param name="silhouettePixels">The pixels participating in the silhouette</param>
            public abstract void ComputeDescriptor(Pixel[] silhouettePixels);
            /// <summary>
            /// Determine the distances between this and other descriptor passed in using the list of criteria passed in
            /// </summary>
            /// <param name="descriptorToCompare">The descriptor to compare against</param>
            /// <returns>an hashtable containing the ICriterion type as key and distance to descriptor as value.</returns>
            public abstract Hashtable DistancesToDescriptor(IDescriptor descriptorToCompare);
        #endregion Interfaces implementation

        #region Constructor
            /// <summary>
            /// Serialization Constructor 
            /// </summary>
            /// <param name="info">The SerializationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            protected DescriptorBase(SerializationInfo info, StreamingContext context) : this()
            {
                _name = (string)info.GetString("Name");
                Criteria = (ICriterion[])info.GetValue("Criteria", typeof(ICriterion[]));
            }
            /// <summary>
            /// Default contructor for this type
            /// </summary>
            protected DescriptorBase()
            {
            }
        #endregion Constructor

        #region ISerializable Members
            /// <summary>
            /// ISerializable unique method, will be called by the formatter
            /// </summary>
            /// <param name="info">The SerializationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Name", _name);
                info.AddValue("Criteria", Criteria);
            }
        #endregion
    }
}
