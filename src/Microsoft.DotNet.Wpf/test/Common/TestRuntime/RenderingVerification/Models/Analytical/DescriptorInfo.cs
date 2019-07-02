// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical
{
    #region usings
        using System;
        using System.Collections;
        using System.Runtime.Serialization;
    #endregion usings

    /// <summary>
    /// Contains the actual IDescriptor along with extra info about it.
    /// Note : To serialize this you need to use the BinaryFormatter or SoapFormatter
    /// </summary>
    [SerializableAttribute()]
    public class DescriptorInfo : ISerializable
    {
        #region Properties
            #region Private Properties
                private int _depth = 0;
                private ArrayList _pixels = null;
                private ArrayList _aggregatedNoisePixels = null;
                private ArrayList _neighbors = null;
                private IDescriptor _iDescriptor = null;
                private int _id = -1;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The level of nesting of this descriptor
                /// </summary>
                /// <value></value>
                public int Depth
                {
                    get
                    {
                        return _depth;
                    }
                    set
                    {
                        _depth = value;
                    }
                }
                /// <summary>
                /// The pixels participating in this shape
                /// </summary>
                /// <value></value>
                public ArrayList Pixels
                {
                    get 
                    {
                        return _pixels;
                    }
                }
                /// <summary>
                /// The "noise" pixel aggregated to this descriptor
                /// </summary>
                /// <value></value>
                public ArrayList AggregatedNoisePixels 
                {
                    get 
                    {
                        return _aggregatedNoisePixels;
                    }
                }
                /// <summary>
                /// A collection of DescriptorInfo touching this descriptor
                /// </summary>
                /// <value></value>
                public ArrayList Neighbors
                {
                    get 
                    {
                        return _neighbors;
                    }
                }
                /// <summary>
                /// The IDescriptor hosted by this type
                /// </summary>
                /// <value></value>
                public IDescriptor IDescriptor 
                {
                    get 
                    {
                        return _iDescriptor;
                    }
                }
                /// <summary>
                /// Identification number of this descriptor
                /// </summary>
                /// <value></value>
                public int ID
                {
                    get { return _id; }
                    set { _id = value; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private DescriptorInfo()
            {
                _neighbors = new ArrayList();
                _pixels = new ArrayList();
                _aggregatedNoisePixels = new ArrayList();
            }
            /// <summary>
            /// Create a new instance of this type using the IDescriptor specified as hosted descriptor
            /// </summary>
            /// <param name="hostedDescriptor"></param>
            public DescriptorInfo(IDescriptor hostedDescriptor) : this()
            {
                _iDescriptor = hostedDescriptor;
            }
            /// <summary>
            /// Create a new instance of this type -- Needed for Serialization, will be called by the formatter when deserializing
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected DescriptorInfo(SerializationInfo info, StreamingContext context) : this()
            {
                _iDescriptor = (IDescriptor)info.GetValue("_iDescriptor", typeof(IDescriptor));
            }
        #endregion Constructors

        #region Methods
            internal void ReplaceIDescriptor(IDescriptor descriptor)
            {

                if (descriptor == null)
                {
                    throw new ArgumentNullException("descriptor", "The argument passed in must be set to a valid instance of an object implementing IDescriptor (null passed in)");
                }
                // BUGBUG : Need to clone this instead of using the original one ?
                _iDescriptor = descriptor;
            }
            internal string GetInfo()
            {
                string retVal = string.Empty;
                retVal += string.Format(" type='{0}'", _iDescriptor.GetType().Name);
                retVal += string.Format(" name='{0}'", _iDescriptor.Name);
                retVal += string.Format(" boundingBox='{0}'",_iDescriptor.BoundingBox);
                retVal += string.Format(" id='{0}'",_id);
                return retVal;
            }
            internal string[] GetCriteriaInfo()
            {
                string[] retVal = new string[_iDescriptor.Criteria.Length];
                ICriterion criterion = null;
                for(int index = 0; index < _iDescriptor.Criteria.Length; index++)
                {
                    criterion =  (ICriterion)_iDescriptor.Criteria[index];
                    retVal[index] += string.Format(" Name='{0}'", criterion.Name);
                    retVal[index] += string.Format(" Description='{0}'", criterion.Description);
                    retVal[index] += string.Format(" Value='{0}'", criterion.Value.ToString());
                }
                return retVal;
            }
            /// <summary>
            /// Add a relation between 2 descriptors (descriptor touch each other)
            /// </summary>
            /// <param name="descriptor">The touching descriptor</param>
            public void AddRelation (DescriptorInfo descriptor)
            {
                _neighbors.Add(descriptor);
                descriptor._neighbors.Add(this);
            }
            /// <summary>
            /// Add a pixel to the list of participating pixels
            /// </summary>
            /// <param name="pixel"></param>
            public void AddParticipatingPixel(Pixel pixel)
            {
                _pixels.Add (pixel);
            }
            internal void SetParticipatingPixels()
            {
                _iDescriptor.SetParticipatingPixels((Pixel[])_pixels.ToArray(typeof(Pixel)));
            }
            /// <summary>
            /// Format the DescriptorInfo for user friendly ouput
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return _iDescriptor.Name + "  { Top:" + _iDescriptor.BoundingBox.Top + ", Left:" + _iDescriptor.BoundingBox.Left + ", Bottom:"+ _iDescriptor.BoundingBox.Bottom + ", Right:" + _iDescriptor.BoundingBox.Right+" }";
            }
        #endregion Methods

        #region ISerializable implementation
            /// <summary>
            /// The implementation of the unique ISerialization Method, will be called by the formatter when serialing
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("_iDescriptor", _iDescriptor);
            }
        #endregion ISerializable implementation
    }
}
