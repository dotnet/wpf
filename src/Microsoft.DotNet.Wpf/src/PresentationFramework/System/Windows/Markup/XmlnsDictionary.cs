// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   A dictionary to control XML namespace mappings
// 

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using MS.Utility;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Threading;


namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// A dictionary to control XML prefix-Namespaceuri mappings
    /// </summary>
#if PBTCOMPILER
    internal class XmlnsDictionary : IDictionary
#else
    public class XmlnsDictionary : IDictionary, System.Xaml.IXamlNamespaceResolver
#endif
    {
#region Public Methods
        /// <summary>
        /// NamespaceDeclaration class which is similar to NamespaceDeclaration class in
        /// XmlNamespaceManager code in BCL. ScopeCount gets incremented and decremented
        /// at PushScope/PopScope, and acts like a marker between Scoped Declarations.
        /// </summary>
        struct NamespaceDeclaration
        {
            /// <summary>
            /// namespace prefix
            /// </summary>
            public string Prefix;
            
            /// <summary>
            /// xml namespace uri.
            /// </summary>
            public string Uri;
            
            /// <summary>
            /// ScopeCount.  Incremented for nested scopes.   
            /// </summary>
            public int    ScopeCount;
        }

        /// <summary>
        /// Namespace Scope 
        /// to retrieve all the declarations at current level or from the root node 
        /// </summary>
        enum NamespaceScope 
        {
            /// <summary>
            /// All Namespaces from root to this Node
            /// </summary>
            All,

            /// <summary>
            /// Only Namespaces at this Node
            /// </summary>
            Local
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public XmlnsDictionary()
        {
            // Initializes the XmlnsDictionary NamespaceDeclarations array with default 8 items
            // and initializes the last Declaration marker to zero for first insertion.
            Initialize();
        }

#if !PBTCOMPILER
        /// <summary>
        /// Construct an XmlnsDictionary based on an already existing one. The Sealed property is not
        /// propagated between dictionaries.
        /// </summary>
        /// <param name="xmlnsDictionary">The dictionary on which to base the new one</param>
        public XmlnsDictionary(XmlnsDictionary xmlnsDictionary)
        {
            if(null == xmlnsDictionary)
            {
                throw new ArgumentNullException( "xmlnsDictionary" );
            }
            
            // Copy the Declarations if they exists 
            if (xmlnsDictionary != null && xmlnsDictionary.Count > 0)
            {
                _lastDecl = xmlnsDictionary._lastDecl;
                if (_nsDeclarations == null)
                {
                    _nsDeclarations = new NamespaceDeclaration[_lastDecl+1];
                }

                // Initialize the count to Zero and start counting.
                _countDecl = 0;
                
                for (int i = 0; i <= _lastDecl; i++)
                {
                    // We copy the entire Dictionary, but update the count only for non-null uri/
                    // The reason is, Count, doesn't make sense when we are implementing the 
                    // storage in our own way. We don't remove the namespace declarations when 
                    // asked to remove, instead we set their uri values to null.
                    if (xmlnsDictionary._nsDeclarations[i].Uri != null)
                    {    _countDecl++;  }

                    _nsDeclarations[i].Prefix     = xmlnsDictionary._nsDeclarations[i].Prefix;
                    _nsDeclarations[i].Uri        = xmlnsDictionary._nsDeclarations[i].Uri;
                    _nsDeclarations[i].ScopeCount = xmlnsDictionary._nsDeclarations[i].ScopeCount;
                }
            }
            else // create an empty array of Declarations. Default Size is 8. 
            {
                Initialize();
            }
        }
#endif

        /// <summary>
        /// Add a namespace to the dictionary (accepts objects for the purpose of implementing IDictionary)
        /// </summary>
        /// <param name="prefix">The XML prefix of this namespace (should be a string)</param>
        /// <param name="xmlNamespace">The namespace the prefix maps to (should be a string)</param>
        public void Add(object prefix, object xmlNamespace)
        {
            // Check for the parameters to be strings as this is IDictionary implementation with object type params.
            if (!(prefix is string) || !(xmlNamespace is string))
            {
                throw new ArgumentException(SR.Get(SRID.ParserKeysAreStrings));
            }
            // Add calls gets delegated to AddNamespace which adds a NamespaceDeclaration to the list of Declarations
            AddNamespace((string)prefix, (string)xmlNamespace);
         }

#if !PBTCOMPILER
        /// <summary>
        /// Add a namespace to the dictionary
        /// </summary>
        /// <param name="prefix">The XML prefix of this namespace</param>
        /// <param name="xmlNamespace">The namespace the prefix maps to</param>
        public void Add(string prefix,string xmlNamespace)
        {
            // Add calls gets delegated to AddNamespace which adds a NamespaceDeclaration to the list of Declarations
            AddNamespace(prefix, xmlNamespace);
        }
#endif

        /// <summary>
        /// Remove all entries from the dictionary
        /// </summary>
        public void Clear()
        {
            CheckSealed();
            _lastDecl = 0;
            _countDecl = 0;
        }

        /// <summary>
        /// Whether the dictionary contains the specified keys
        /// </summary>
        /// <param name="key">prefix to search for</param>
        /// <returns>true if the key exists in the dictionary, false otherwise</returns>
        public bool Contains(object key)
        {
            return (HasNamespace((string)key));
        }

        /// <summary>
        /// Removes an XML prefix from the dictionary
        /// </summary>
        /// <param name="prefix">The prefix to be removed</param>
        public void Remove(string prefix)
        {
            string xmlNamespace = LookupNamespace(prefix);
            RemoveNamespace(prefix, xmlNamespace);
        }

        /// <summary>
        /// Removes an XML prefix from the dictionary
        /// </summary>
        /// <param name="prefix">The prefix to be removed</param>
        public void Remove(object prefix)
        {
            this.Remove((string)prefix);           
        }

#if !PBTCOMPILER
        /// <summary>
        /// Copy the dictionary to an array
        /// </summary>
        /// <param name="array">The array into which to copy the table data</param>
        /// <param name="index">The zero-based index in array at which copying begins</param>
        public void CopyTo(DictionaryEntry[] array, int index)
        {
            CopyTo((Array)array, index);
        }
#endif

#region IDictionaryMethods

        /// <summary>
        /// IDictionary interface that returns an enumerator on the dictionary contents
        /// </summary>
        /// <returns>Returns an enumerator on the dictionary contents</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            HybridDictionary namespaceTable = new HybridDictionary(_lastDecl);

            for (int thisDecl = 0; thisDecl < _lastDecl;  thisDecl++)
            {
                if (_nsDeclarations[thisDecl].Uri != null)
                {
                    namespaceTable[_nsDeclarations[thisDecl].Prefix] = _nsDeclarations[thisDecl].Uri;
                }
            }

            return namespaceTable.GetEnumerator();
        }

#endregion IDictionaryMethods

#region IEnumerableMethods

        /// <summary>
        /// IEnumerator interface that returns an enumerator on the dictionary contents
        /// </summary>
        /// <returns>Returns an enumerator on the dictionary contents</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

#endregion IEnumerableMethods

#region ICollectionMethods

        /// <summary>
        /// ICollection method to copy the dictionary to an array
        /// </summary>
        /// <param name="array">The array into which to copy the table data</param>
        /// <param name="index">The zero-based index in array at which copying begins</param>
        public void CopyTo(Array array, int index)
        {
            IDictionary dict = GetNamespacesInScope(NamespaceScope.All) as IDictionary;
            if (dict != null)
                dict.CopyTo(array,index);
        }

#endregion ICollectionMethods

#region IXamlNamespaceResolver Members

#if !PBTCOMPILER
        public string GetNamespace(string prefix)
        {
            return LookupNamespace(prefix);
        }

        public System.Collections.Generic.IEnumerable<System.Xaml.NamespaceDeclaration> GetNamespacePrefixes()
        {
            if (_lastDecl > 0)
            {
                for (int i = _lastDecl - 1; i >= 0; i--)
                {
                    yield return new System.Xaml.NamespaceDeclaration(_nsDeclarations[i].Uri, _nsDeclarations[i].Prefix);
                }
            }
        }
#endif

#endregion


#if !PBTCOMPILER
        /// <summary>
        /// IDictionary interface that returns an enumerator on the dictionary contents
        /// </summary>
        /// <returns>Returns an enumerator on the dictionary contents</returns>
        protected IDictionaryEnumerator GetDictionaryEnumerator()
        {
            HybridDictionary namespaceTable = new HybridDictionary(_lastDecl);

            for (int thisDecl = 0; thisDecl < _lastDecl; thisDecl++)
            {
                if (_nsDeclarations[thisDecl].Uri != null)
                {
                    namespaceTable[_nsDeclarations[thisDecl].Prefix] = _nsDeclarations[thisDecl].Uri;
                }
            }

            return namespaceTable.GetEnumerator();
        }
#endif

        /// <summary>
        /// IEnumerator interface that returns an enumerator on the dictionary contents
        /// </summary>
        /// <returns>Returns an enumerator on the dictionary contents</returns>
        protected IEnumerator GetEnumerator()
        {
            return Keys.GetEnumerator();
        }

#if !PBTCOMPILER
        /// <summary>
        /// Seal the dictionary so it can't be changed (this must be done before the setting the dictionary as a
        /// dynamic property). Any attempt to modify the dictionary after this function is called will result in
        /// a InvalidOperationException being thrown.
        /// </summary>
        public void Seal()
        {
            _sealed = true;
        }
#endif

        /// <summary>
        /// Looks up the namespace corresponding to an XML namespace prefix
        /// </summary>
        /// <param name="prefix">The XML namespace prefix to look up</param>
        /// <returns>The namespace corresponding to the given prefix if it exists, null otherwise</returns>
        public string LookupNamespace(string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException( "prefix" ); 
            }
            
            if (_lastDecl >0)
            {
                for (int thisDecl = _lastDecl-1; thisDecl >= 0; thisDecl--)
                {
                    if ((_nsDeclarations[thisDecl].Prefix == prefix) && 
                        !string.IsNullOrEmpty(_nsDeclarations[thisDecl].Uri))
                    {
                        return _nsDeclarations[thisDecl].Uri;
                    }
                }
            }
            return null;
        }

#if !PBTCOMPILER
        /// <summary>
        /// Looks up the XML prefix corresponding to a namespaceuri
        /// </summary>
        /// <param name="xmlNamespace">The namespaceuri to look up</param>
        /// <returns>
        /// string.Empty if the given namespace corresponds to the default namespace; 
        /// otherwise, the XML prefix corresponding to the given namespace, or null 
        /// if none exists.
        /// </returns>
        public string LookupPrefix(string xmlNamespace)
        {
            if (xmlNamespace == null)
            {
                throw new ArgumentNullException( "xmlNamespace" ); 
            }

            if (_lastDecl > 0)
            {
                for (int thisDecl = _lastDecl-1; thisDecl >= 0; thisDecl--)
                {
                    if (_nsDeclarations[thisDecl].Uri == xmlNamespace)
                        return _nsDeclarations[thisDecl].Prefix;         
                }
            }
            return null;
       }

        /// <summary>
        /// DefaultNamespace for easy Access.
        /// </summary>
        public string DefaultNamespace()
        {
             string defaultNs = LookupNamespace(string.Empty);
             return (defaultNs == null) ? string.Empty : defaultNs;
         }
#endif

        /// <summary>
        /// Pushes the scope of the Xmlns dictionary.
        ///    when the ParserContext is entering into next level, 
        ///    PushScope will get called from ParserContext.PushScope
        /// </summary>
        public void PushScope()
        {
            CheckSealed();
            _nsDeclarations[_lastDecl].ScopeCount++;
        }

        /// <summary>
        /// Pops the scope of the Xmlns dictionary
        ///    when the ParserContext is leaving the current level, 
        ///    PopScope will get called from ParserContext.PopScope
        /// </summary>
        public void PopScope()
        {
            CheckSealed();

            int lastScopeCount = _nsDeclarations[_lastDecl].ScopeCount;
            int decl = _lastDecl;

            while (decl > 0 && _nsDeclarations[decl-1].ScopeCount == lastScopeCount) 
            {
                decl--;
            }

            // If we are not the first entry, we reduce the ScopeCount of 
            // entry which we incremented in pushscope
            if (_nsDeclarations[decl].ScopeCount > 0)
            {
                _nsDeclarations[decl].ScopeCount--;
                _nsDeclarations[decl].Prefix = String.Empty;
                _nsDeclarations[decl].Uri = null;
            }
            
            _lastDecl = decl;
        }
        
#endregion Public Methods

#region Properties

        /// <summary>
        /// IDictionary property specifying whether the dictionary is fixed size (always false)
        /// </summary>
        public bool IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// IDictionary property specifying whether the dictionary is read-only
        /// </summary>
        public bool IsReadOnly
        {
            get { return _sealed; }
        }

        /// <summary>
        /// A property indexing into the dictionary by XML prefix
        /// </summary>
        public string this[string prefix]
        {
            get {   return LookupNamespace(prefix); }
            set {   AddNamespace(prefix, value as string);}
        }

        /// <summary>
        /// A property indexing into the dictionary by XML prefix (supports objects to satisfy IDictionary spec)
        /// </summary>
        public object this[object prefix]
        {
            get 
            {
                if (!(prefix is string))
                {
                    throw new ArgumentException(SR.Get(SRID.ParserKeysAreStrings));
                }
                return LookupNamespace((string)prefix); 
            }
            set 
            {   
                if (!(prefix is string) || !(value is string))
                {
                    throw new ArgumentException(SR.Get(SRID.ParserKeysAreStrings));
                }
                AddNamespace((string)prefix, (string)value); 
            }
        }

        /// <summary>
        /// An ICollection of all keys in the dictionary
        /// </summary>
        public ICollection Keys
        {
            get 
            {
                // Dynamically create the keys table and return it.
                // should be used mainly in foreach cases thru IEnumerator.
                // Indexer is provided, so this is seldom useful.
                ArrayList prefixes = new ArrayList(_lastDecl+1);
                for (int thisDecl =0; thisDecl < _lastDecl; thisDecl++)
                {
                    // add all the Namespace Declarations whose Namespaces are not null
                    if (_nsDeclarations[thisDecl].Uri != null)
                    {
                        if (!prefixes.Contains(_nsDeclarations[thisDecl].Prefix))
                            prefixes.Add(_nsDeclarations[thisDecl].Prefix);
                    }
                }
                return prefixes;
            }
        }

        /// <summary>
        /// An ICollection of all values in the dictionary (order of ICollection corresponds 
        /// to order of Keys)
        /// </summary>
        public ICollection Values
        {
            get 
            {                
                HybridDictionary namespaceTable = new HybridDictionary(_lastDecl+1);
                for (int thisDecl = 0; thisDecl < _lastDecl; thisDecl++)
                {
                    if (_nsDeclarations[thisDecl].Uri != null)
                    {
                        namespaceTable[_nsDeclarations[thisDecl].Prefix] = _nsDeclarations[thisDecl].Uri;
                    }
                }
                return namespaceTable.Values;
            }
        }

        /// <summary>
        /// The number of elements in the dictionaries
        /// </summary>
        public int Count
        {
            get 
            {
                return _countDecl;
            }
        }

        /// <summary>
        /// Whether or not access to this dictionary is thread-safe
        /// </summary>
        public bool IsSynchronized
        {
            get  {return _nsDeclarations.IsSynchronized; }
        }

        /// <summary>
        /// An object that can be used to synchronize access to the dictionary
        /// </summary>
        public object SyncRoot
        {
            get {return _nsDeclarations.SyncRoot; }
        }

#if !PBTCOMPILER
        /// <summary>
        /// Whether or not the dictionary is sealed
        /// </summary>
        public bool Sealed
        {
            get { return _sealed; }
        }
#endif

#endregion Properties

#if !PBTCOMPILER
#region Internal
        // Unseal the dictionary internally
        internal void Unseal()
        {
            _sealed = false;
        }
 #endregion Internal
 #endif

#region Private       
        private void Initialize()
        {
            // We set the initial array to 8 and double from there when we run of the space. 
            // For the start case, we set the DefaultNamespaceuri to null. 
            _nsDeclarations = new NamespaceDeclaration[8];
            _nsDeclarations[0].Prefix = string.Empty;
            _nsDeclarations[0].Uri = null;
            _nsDeclarations[0].ScopeCount = 0;
            _lastDecl = 0;
            _countDecl = 0;
       }

       private void CheckSealed()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.ParserDictionarySealed));
            }
        }

        /// <summary>
        /// Helper to Add Namespace - Checks for prefix from rear.
        ///  If exists : override it locally, if not Adds an entry.
        /// </summary>
        /// <param name="prefix">prefix to add</param>
        /// <param name="xmlNamespace">namespace uri string to add</param>
        private void AddNamespace(string prefix, string xmlNamespace)
        {
            CheckSealed();
            
            if (xmlNamespace == null)
                throw new ArgumentNullException("xmlNamespace");

            if (prefix == null)
                throw new ArgumentNullException("prefix");

            int lastScopeCount = _nsDeclarations[_lastDecl].ScopeCount;

            if (_lastDecl > 0)
            {
                // Check the local scope for the given prefix
                for (int thisDecl = _lastDecl-1; 
                     thisDecl >= 0 && _nsDeclarations[thisDecl].ScopeCount == lastScopeCount; 
                     thisDecl--)
                {
                    if (String.Equals(_nsDeclarations[thisDecl].Prefix, prefix))
                    {
                        // Redefine an existing namespace
                        _nsDeclarations[thisDecl].Uri = xmlNamespace;
                        return; 
                    }
                }

                // Running out of Array Capacity, allocate more and copy the contents.
                if (_lastDecl == _nsDeclarations.Length - 1)
                {
                    NamespaceDeclaration[] new_nsDeclarations = new NamespaceDeclaration[_nsDeclarations.Length * 2];

                    Array.Copy(_nsDeclarations, 0, new_nsDeclarations, 0, _nsDeclarations.Length);
                    _nsDeclarations = new_nsDeclarations;
                }
            }

             _countDecl++;
            _nsDeclarations[_lastDecl].Prefix = prefix;
            _nsDeclarations[_lastDecl].Uri = xmlNamespace;
            _lastDecl++;
            _nsDeclarations[_lastDecl].ScopeCount = lastScopeCount;
        }

        // Removing the namespace given prefix and xmlNamespace.
        // We need prefix and xmlNamespace, inorder to remove the correct entry.
        // Walking from the LastDecl to first entry in the list to zero
        // and removing the right entry which matches both.
        private void RemoveNamespace(string prefix, string xmlNamespace)
        {
            CheckSealed();
            if (_lastDecl > 0)
            {
                if (xmlNamespace == null)
                {
                    throw new ArgumentNullException("xmlNamespace");
                }

                if (prefix == null)
                {
                    throw new ArgumentNullException("prefix");
                }

               int lastScopeCount = _nsDeclarations[_lastDecl-1].ScopeCount;
               for (int thisDecl = _lastDecl-1; 
                     thisDecl >= 0 && _nsDeclarations[thisDecl].ScopeCount == lastScopeCount; 
                     thisDecl--)
                {
                    if ((_nsDeclarations[thisDecl].Prefix == prefix) && (_nsDeclarations[thisDecl].Uri == xmlNamespace))
                    {
                        _nsDeclarations[thisDecl].Uri = null;
                        _countDecl--;
                    }
                }
            }
        }

        // Get all namespaces in the local or scope from the last parent
        private  IDictionary GetNamespacesInScope(NamespaceScope scope)
        {
            int i = 0;

            switch (scope)
            {
                case NamespaceScope.All:
                    i = 0;
                    break;

                case NamespaceScope.Local:
                    i = _lastDecl;
                    int lastScopeCount = _nsDeclarations[i].ScopeCount;
                    while (_nsDeclarations[i].ScopeCount == lastScopeCount)  
                        i--;
                    i++;
                    break;
            }

            HybridDictionary dict = new HybridDictionary(_lastDecl -i + 1);

            for (; i < _lastDecl; i++)
            {
                string prefix = _nsDeclarations[i].Prefix;
                string xmlNamespace    = _nsDeclarations[i].Uri;

                Debug.Assert(prefix != null);
                if (xmlNamespace.Length > 0 || prefix.Length > 0)
                {
                    dict[prefix] = xmlNamespace;
                }
                else
                {
                    // default namespace redeclared to "" -> remove from list
                    dict.Remove(prefix);
                }
            }

            return dict;
        }

        // Utility method to find whether a prefix has namespace associated.
        private bool HasNamespace(string prefix)
        {
            if (_lastDecl > 0)
            {
                for (int thisDecl = _lastDecl-1; 
                     thisDecl >= 0;
                     thisDecl--)
                {
                    if ( (_nsDeclarations[thisDecl].Prefix == prefix) && _nsDeclarations[thisDecl].Uri != null)
                    {
                        if (prefix.Length > 0 || _nsDeclarations[thisDecl].Uri.Length > 0)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }

            return false;
        }

#endregion Private

#region Private Data
        /// <summary>
        /// Namespace Declarations Array. Used to Index Namespaces within Context.
        /// </summary>
        private NamespaceDeclaration[] _nsDeclarations;

        /// <summary>
        /// Namespace Declarations Rear Index, where we do additions and deletions, also
        /// this is the index from which we start lookup for namespaces.
        /// </summary>
        private int   _lastDecl = 0;

        /// <summary>
        /// Namespace Declarations Rear Index, where we do additions and deletions, also
        /// this is the index from which we start lookup for namespaces.
        /// </summary>
        private int _countDecl = 0;

        /// <summary>
        /// Seals the Current Dictionary from further updating it.
        /// </summary>
        private bool _sealed = false;       // True if dictionary is immutable.
#endregion Private Data

    } //XmlNamespaceManager
}
