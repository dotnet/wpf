// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++                                                                          
    Abstract:
        Contains the class definition of some classes
        that maintain the NameSpaces specific information.
                                                                           
--*/
using System.Collections;

namespace System.Windows.Xps.Serialization
{
    internal class SerializableObjectNamespaceInfo
    {
        #region Constructor

        internal 
        SerializableObjectNamespaceInfo(
            Type type, 
            string prefix, 
            string xmlNamespace
            ) :
        this(type.Namespace, prefix, xmlNamespace)
        {
        }
    
        internal
        SerializableObjectNamespaceInfo(
            string clrNamespace, 
            string prefix, 
            string xmlNamespace
            )
        {
            this._xmlNamespace = xmlNamespace;
            this._clrNamespace = clrNamespace;
            this._prefix       = prefix;
        }

        #endregion Constructor
    
        #region Internal Properties

        internal 
        string 
        Prefix
        {
            get 
            {
                return _prefix; 
            }
        }
    
        internal 
        string 
        XmlNamespace
        {
            get 
            {
                 return _xmlNamespace; 
            }
        }
    
        internal 
        string 
        ClrNamespace
        {
            get 
            { 
                return _clrNamespace; 
            }
        }

        #endregion Internal Properties

        #region Private Data

        private readonly string _prefix;
        private readonly string _xmlNamespace;
        private readonly string _clrNamespace;

        #endregion Private Data
    };

    internal class MetroSerializationNamespaceTable
    {
        #region Constructor

        internal 
        MetroSerializationNamespaceTable(
            MetroSerializationNamespaceTable parent
            )
        {
            Initialize(parent);
        }

        #endregion Constructor
    
        #region Internal Properties

        internal 
        SerializableObjectNamespaceInfo 
        this[Type type]
        {
            get
            {
                return (SerializableObjectNamespaceInfo)_innerDictionary[type];
            }
            
            set
            {
                _innerDictionary[type] = value;
            }
        }

        #endregion Internal Properties
        
        #region Internal Methods
    
        internal
        bool 
        Contains(
            Type type
            ) 
        {
            return _innerDictionary.Contains(type);
        }
        
        internal 
        void 
        Add(
            Type type, 
            SerializableObjectNamespaceInfo namespaceInfo) 
        {
            _innerDictionary.Add(type, namespaceInfo);
        }
    
        internal 
        void 
        Initialize(
            MetroSerializationNamespaceTable parent
            )
        {

            _innerDictionary = new Hashtable(11);

        }

        #endregion Internal Methods

        #region Private Data

        private IDictionary _innerDictionary;        

        #endregion Private Data
    };
}
