// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: BamlLocalizableResourceKey class
//

using System;

namespace System.Windows.Markup.Localizer
{
    /// <summary>
    /// Key to BamlLocalizableResource
    /// </summary>   
    public class BamlLocalizableResourceKey
    {
        //-------------------------------
        // Constructor
        //-------------------------------
        internal BamlLocalizableResourceKey(
            string uid,
            string className,
            string propertyName,
            string assemblyName
            )
        {
            if (uid == null)
            {
                throw new ArgumentNullException("uid");
            }

            if (className == null)
            {
                throw new ArgumentNullException("className");
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            _uid          = uid;
            _className    = className;
            _propertyName = propertyName;
            _assemblyName = assemblyName;            
        }

        /// <summary>
        /// Construct a key to the BamlLocalizableResource. The key 
        /// consists of name, class name and property name, which will be used to 
        /// identify a localizable resource in Baml.
        /// </summary>
        /// <param name="uid">The unique id of the element that has the localizable resource. It is equivalent of x:Uid in XAML file.</param>
        /// <param name="className">class name of localizable resource in Baml. </param>
        /// <param name="propertyName">property name of the localizable resource in Baml </param>
        public BamlLocalizableResourceKey(
            string uid,
            string className,
            string propertyName
            ) : this (uid, className, propertyName, null)
        {            
        }

        //-------------------------------
        // Public properties
        //-------------------------------

        /// <summary>
        /// Id of the element that has the localizable resource 
        /// </summary>
        public string Uid
        {
            get { return _uid; }
        }

        /// <summary>
        /// Class name of the localizable resource
        /// </summary>
        public string ClassName
        {
            get { return _className; }
        }

        /// <summary>
        /// Property name of the localizable resource 
        /// </summary>
        public string PropertyName
        {
            get { return _propertyName; }
        }

        /// <summary>
        /// The name of the assembly that defines the type of the localizable resource. 
        /// </summary>
        /// <remarks>
        /// Assembly name is not required for uniquely identifying a resource in Baml. It is 
        /// popluated when extracting resources from Baml so that users can find the type information
        /// of the localizable resource. 
        /// </remarks>
        public string AssemblyName
        {
            get { return _assemblyName; }
        }

        /// <summary>
        /// Compare two BamlLocalizableResourceKey objects
        /// </summary>
        /// <param name="other">The other BamlLocalizableResourceKey object to be compared against</param>
        /// <returns>True if they are equal. False otherwise</returns>        
        public bool Equals(BamlLocalizableResourceKey other)
        {
            if (other == null)
            {
                return false;
            }
        
            return _uid == other._uid
                && _className == other._className
                && _propertyName == other._propertyName; 
        }

        /// <summary>
        /// Compare two BamlLocalizableResourceKey objects
        /// </summary>
        /// <param name="other">The other BamlLocalizableResourceKey object to be compared against</param>
        /// <returns>True if they are equal. False otherwise</returns>        
        public override bool Equals(object other)
        {
            return Equals(other as BamlLocalizableResourceKey);
        }

        /// <summary>
        /// Get the hashcode of this object
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return _uid.GetHashCode() 
                ^  _className.GetHashCode()
                ^  _propertyName.GetHashCode();
        }
        
        //-------------------------------
        // Private members
        //-------------------------------
        private string _uid;
        private string _className;
        private string _propertyName;
        private string _assemblyName;
    }
}
