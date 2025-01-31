// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.RightsManagement
{
    /// <summary>
    /// LocalizedNameDescriptionPair class represent an immutable (Name, Description) pair of strings. This is 
    /// a basic building block for structures that need to express locale specific information about 
    ///  Unsigned Publish Licenses. 
    /// </summary>
    public class LocalizedNameDescriptionPair
    {
        /// <summary>
        /// Constructor for the read only LocalizedNameDescriptionPair class. It takes values for Name and Description as parameters. 
        /// </summary>
        public LocalizedNameDescriptionPair(string name, string description)
        {

            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(description);

            _name = name;
            _description = description;
        }

        /// <summary>
        /// Read only Name property.
        /// </summary>
        public string Name 
        {
            get
            {

                return _name;
            }
        }

        /// <summary>
        /// Read only Description property.
        /// </summary>
        public string Description 
        {
            get
            {
            
                return _description;
            }
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != GetType()))
            {
                return false;
            }

            LocalizedNameDescriptionPair localizedNameDescr = obj as LocalizedNameDescriptionPair;
            
            return (string.Equals(localizedNameDescr.Name, Name, StringComparison.Ordinal))
                        &&
                    (string.Equals(localizedNameDescr.Description, Description, StringComparison.Ordinal));
        }        
            
        /// <summary>
        /// Compute hash code.
        /// </summary>
        public override int GetHashCode()
        {
        
            return Name.GetHashCode()  ^  Description.GetHashCode();
        }

        private string _name;
        private string _description; 
    }
}
