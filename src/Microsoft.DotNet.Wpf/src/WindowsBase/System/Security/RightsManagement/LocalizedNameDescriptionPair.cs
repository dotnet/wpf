// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Allow use of presharp warning numbers [6506] and [6518] unknown to the compiler
#pragma warning disable 1634, 1691

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
            
            //PRESHARP:Parameter to this public method must be validated:  A null-dereference can occur here. 
            //This is a false positive as the checks above can gurantee no null dereference will occur  
#pragma warning disable 6506
            return (string.Equals(localizedNameDescr.Name, Name, StringComparison.Ordinal))
                        &&
                    (string.Equals(localizedNameDescr.Description, Description, StringComparison.Ordinal));
#pragma warning restore 6506
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
