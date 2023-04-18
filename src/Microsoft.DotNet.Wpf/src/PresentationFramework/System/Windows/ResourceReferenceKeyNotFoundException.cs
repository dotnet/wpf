// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   A resource could not be found
//
// 

using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Windows
{
    ///<summary>Exception class for resource reference</summary>
    [Serializable]
    public class ResourceReferenceKeyNotFoundException: InvalidOperationException
    {
        ///<summary>
        /// Constructor
        ///</summary>
        public ResourceReferenceKeyNotFoundException()
        {
            _resourceKey = null;
        }
        
        ///<summary>
        /// Constructor
        ///</summary>
        public ResourceReferenceKeyNotFoundException(string message, object resourceKey) 
                        : base(message)
        {
            _resourceKey = resourceKey;
        }
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        ///<summary>
        /// Constructor (required for Xml web service)
        ///</summary>
        protected  ResourceReferenceKeyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _resourceKey = info.GetValue("Key", typeof(object));
        }
#pragma warning restore SYSLIB0051 // Type or member is obsolete
        ///<summary>
        /// LineNumber that the exception occured on.
        ///</summary>
        public object Key
        {
            get { return _resourceKey; }
        }


        /// <summary>
        /// Populates a SerializationInfo with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">
        /// The SerializationInfo to populate with data.
        /// </param>
        /// <param name="context">
        /// The destination for this serialization.
        /// </param>
#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Key", _resourceKey);
        }
#pragma warning restore SYSLIB0051 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member
        private object _resourceKey;
    }

    
}


