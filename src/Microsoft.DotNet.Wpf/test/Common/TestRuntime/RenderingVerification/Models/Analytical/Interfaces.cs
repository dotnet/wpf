// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical
{
    #region usings
        using System;
        using System.IO;
        using System.Collections;
        using System.Runtime.Serialization;
        using Microsoft.Test.RenderingVerification;
    #endregion usings

    /// <summary>
    /// Definition for the IDescriptor interface
    /// </summary>
    public interface IDescriptor : ISerializable
    {
        /// <summary>
        /// Descriptor Name 
        /// </summary>
        /// <value></value>
        string Name { get;set;}
        /// <summary>
        /// Collection of Criteria applied to this descriptor.
        /// </summary>
        /// <value></value>
        ICriterion[] Criteria { get;set;}
        /// <summary>
        /// The Descriptor's bounding rectangle 
        /// </summary>
        /// <value></value>
        RenderRect BoundingBox { get;}
        /// <summary>
        /// Map specific descriptor characteristic (key = name, value = object)
        /// </summary>
        /// <value></value>
        Hashtable DescriptorDependentObjects { get; }

        /// <summary>
        /// Add all the pixels participating in the shape
        /// </summary>
        /// <param name="pixels">The collection of "Pixel" paricipating in the silhouette/shape/Image</param>
        void SetParticipatingPixels(Pixel[] pixels);
        /// <summary>
        /// Compute all Descriptors for the bitmap.
        /// </summary>
        /// <param name="silhouetteExtraPixels">The pixels participating in the silhouette</param>
        void ComputeDescriptor(Pixel[] silhouetteExtraPixels);
        /// <summary>
        /// Determine the distances between this and other descriptor passed in using the list of criteria passed in
        /// </summary>
        /// <param name="descriptorToCompare">The descriptor to compare against</param>
        /// <returns>an hashtable containing the ICriterion type as key and distance to descriptor as value.</returns>
        Hashtable DistancesToDescriptor(IDescriptor descriptorToCompare);
    }

    /// <summary>
    /// Definition for the ICriterion interface
    /// </summary>
    public interface ICriterion
    {
        /// <summary>
        /// Name of the Criterion
        /// </summary>
        /// <value></value>
        string Name { get;}
        /// <summary>
        /// Description of the criterion
        /// </summary>
        /// <value></value>
        string Description{ get;}
        /// <summary>
        /// Value of the tolerance criterion
        /// </summary>
        /// <value></value>
        object Value { get;set;}
        /// <summary>
        /// Returns the value resulting in computing the difference bewtween two descriptors.
        /// Note :  a RenderingVerificationException will be thrown if you try to call this Property before calling the "Pass" Method
        /// </summary>
        /// <value></value>
        double DistanceBetweenDescriptors { get; }
        /// <summary>
        /// Get/set the weight of this criteria; used for classifiying closest criteria (i.e. : Shape more important than position)
        /// </summary>
        /// <value></value>
        int Weight { get;set; }
        /// <summary>
        /// Test if the descriptor is within tolerance
        /// </summary>
        /// <param name="descriptorToMatch">Descriptor to match</param>
        /// <param name="descriptorTest">Descriptor that will be tested for criteria</param>
        /// <returns>true if descriptors match (within tolerance), false otherwise</returns>
        bool Pass(IDescriptor descriptorToMatch, IDescriptor descriptorTest);
    }
}
