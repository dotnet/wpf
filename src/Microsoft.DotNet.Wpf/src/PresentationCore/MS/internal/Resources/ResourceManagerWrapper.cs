// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  ResourceManagerWrapper is responsbile for getting resource 
//  stream for a given Uri from :
//
//     A.  Right SatelliteAssembly for localizable resource.
//     B.  Main assembly for non-localizable resource.
// 

using System;
using System.IO.Packaging;
using System.IO;
using System.Collections;
using System.Windows.Resources;
using System.Resources;
using System.Reflection;
using System.Globalization;
using MS.Internal.PresentationCore;                   // SafeSecurityHelper
using System.Windows;

namespace MS.Internal.Resources
{
    internal class ResourceManagerWrapper
    {
        //
        // Constructor 
        //
        // Take an Assembly instance as input.
        // For a Uri which stands for resource in  main application
        // assembly, this constructor will be called.
        //
        internal ResourceManagerWrapper(Assembly assembly)
        {
            _assembly = assembly;
        }
 
        #region Internal methods        

        internal Stream GetStream(string name)
        {
            Stream stream = null;

            //
            // Try to search the resource from localizable resourceManager.
            // ResourceManager will do all the culture probe-and-fallback work.
            //
            try
            {
                stream = this.ResourceManager.GetStream(name, CultureInfo.CurrentUICulture);
            }
            catch (SystemException e)
            {
                bool bMissingResource;

                bMissingResource = (e is MissingManifestResourceException || e is MissingSatelliteAssemblyException);

                if (!bMissingResource)
                {
                    throw;
                }

                // If it is missing Resource Manfifest exception, it is possible
                // the resource is in unlocalizable part. catch and eat the 
                // exception here so that the code has chance to search the resource 
                // from unlocalizable resource set part.
            }

            if (stream == null)
            {
                // Try to search the unlocalizable resource from Main assembly only.
                if (ResourceSet != null)
                {
                    try
                    {
                        stream = this.ResourceSet.GetObject(name) as Stream;
                    }
                    catch (SystemException e)
                    {
                        if (!(e is MissingManifestResourceException))
                        {
                            throw;
                        }

                        // Don't throw exception here.
                        // If it doesn't return a valid stream, later code GetStreamCore in 
                        // ResourcePart will throw exception and let user know what happened 
                        // with more helpful information.
                        //
                    }
                }
            }

            return stream;
        }        

        #endregion Internal methods

        #region Internal property

        internal Assembly Assembly
        {
            get
            {
                return _assembly;
            }
            set
            {
                _assembly = value;                
                _resourceManager = null;
                _resourceSet = null;      
                _resourceList = null;
            }
        }

        //
        // Readonly property
        // Return a list of resources available in this ResourceManagerWrapper.
        // It contains both localizable and unlocalizable resources.
        //
        internal IList ResourceList
        {
            get 
            {
                if (_resourceList == null)
                {
                    _resourceList = new ArrayList();

                    if (this.ResourceManager != null)
                    {
                        CultureInfo ciNeutral = GetNeutralResourcesLanguage();

                        // ResourceSet for neutral language contains all the available resources 
                        // in this ResourceManager.
                        //
                        // Set true to the second parameter, createIfNotExists, in GetResourceSet( )
                        //
                        // This is to guarantee that API GetResourceSet( ) can always return ResourceSet 
                        // instance for the neutral language.
                        //
                        // If RM.GetObject or GetStream is called first, ResourceManager can create 
                        // ResourceSet instance for each cutlure and cache them in memory, so later 
                        // GetResourceSet( ) can return right instance no matter what value in second parm.
                        // 
                        // But if GetObject/GetStream is not called first in user's code, and the second parm
                        // is set to false, the resourceSet instance for the language is not available when 
                        // this API is called. Even though this API can get the Resource streams internally, 
                        // but it would NOT create instance of ResourceSet and return the object because 
                        // createIfNotExists is false.
                        //
                        ResourceSet rsLoc = this.ResourceManager.GetResourceSet(ciNeutral, true, false);

                        if (rsLoc != null)
                        {
                            AddResourceNameToList(rsLoc, ref _resourceList);

                            rsLoc.Close();
                        }
                    }

                    if (this.ResourceSet != null)
                    {
                        // This contains non-localizable resources in Main assembly.

                        AddResourceNameToList(this.ResourceSet, ref _resourceList);
                    }
                }

                return (IList)_resourceList;
            }
        }


        #endregion Internal property


        #region private methods

        //
        // Get Neutral Resource language for the assembly of this ResourceManagerWrapper
        //
        private CultureInfo GetNeutralResourcesLanguage( )
        {
            //
            // If NeutralResourceLanguageAttribute is not set, the resource must be
            // in Main assembly, return CultureInfor.InvariantCulture.
            //
            CultureInfo ciNeutral = CultureInfo.InvariantCulture;

            NeutralResourcesLanguageAttribute neutralLangAttr = Attribute.GetCustomAttribute(_assembly, typeof(NeutralResourcesLanguageAttribute)) as NeutralResourcesLanguageAttribute;

            if (neutralLangAttr != null)
            {
                ciNeutral = new CultureInfo(neutralLangAttr.CultureName);
            }

            return ciNeutral;
        }


        // 
        // Add all the availabe resource names in ResourceSet to ArrayList
        //
        private void AddResourceNameToList(ResourceSet rs, ref ArrayList resourceList)
        {
            IDictionaryEnumerator deResources;

            deResources = rs.GetEnumerator();

            if (deResources != null)
            {
                while (deResources.MoveNext())
                {
                    string resName = deResources.Key as string;

                    resourceList.Add(resName);
                }
            }
        }

        #endregion private methods


        #region private properties

        //
        // Get ResourceSet for non-localizable resources.
        //
        private ResourceSet ResourceSet
        {
            get
            {
                if (_resourceSet == null)
                {
                    string manifestResourceName;

                    manifestResourceName = SafeSecurityHelper.GetAssemblyPartialName(_assembly) + UnLocalizableResourceNameSuffix;

                    ResourceManager manager = new ResourceManager(manifestResourceName, this._assembly);
                    _resourceSet = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
                }

                return _resourceSet;
            }
        }


        //
        // Get ResourceManger for the localizable resources.
        //
        private ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    string baseResourceName;  // Our build system always generate a resource base name "$(AssemblyShortname).g"

                    baseResourceName = SafeSecurityHelper.GetAssemblyPartialName(_assembly) + LocalizableResourceNameSuffix;

                    _resourceManager = new ResourceManager(baseResourceName, this._assembly);
                }

                return _resourceManager;
            }
        } 

        #endregion private properties


        #region private field

        private ResourceManager _resourceManager = null;  // For localizable resources.
        private ResourceSet     _resourceSet = null;      // For non-localizable resources.
        private Assembly        _assembly = null;
        private ArrayList       _resourceList = null;

        private const string LocalizableResourceNameSuffix = ".g";
        private const string UnLocalizableResourceNameSuffix = ".unlocalizable.g";

        #endregion private field
    }
}
