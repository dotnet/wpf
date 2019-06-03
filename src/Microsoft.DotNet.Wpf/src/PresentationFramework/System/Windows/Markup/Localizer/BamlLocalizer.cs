// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Contents:  BamlLocalizer class, part of Baml Localization API
//

using System;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Windows.Markup;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Windows;

using MS.Internal.Globalization;

namespace System.Windows.Markup.Localizer
{
    /// <summary>
    /// BamlLocalizer class localizes a baml stram. 
    /// </summary>
    public class BamlLocalizer
    {
        //-----------------------------------
        // constructor 
        //-----------------------------------
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">source baml stream to be localized</param>
        public BamlLocalizer(Stream source) : this (source, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">source baml stream to be localized</param>
        /// <param name="resolver">Localizability resolver implemented by client</param>
        public BamlLocalizer(
            Stream source,
            BamlLocalizabilityResolver resolver
            ) : this (source, resolver, null)
        {
        }        

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="source">source baml straem to be localized</param>
        /// <param name="resolver">Localizability resolver implemented by client</param>
        /// <param name="comments">TextReader to read localization comments XML </param>
        public BamlLocalizer(
            Stream                      source,
            BamlLocalizabilityResolver  resolver,
            TextReader                  comments
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            _tree = BamlResourceDeserializer.LoadBaml(source);

            // create a Baml Localization Enumerator
            _bamlTreeMap = new BamlTreeMap(this, _tree, resolver, comments);
        }
        
        /// <summary>
        /// Extract localizable resources from the source baml.
        /// </summary>
        /// <returns>Localizable resources returned in the form of BamlLoaclizationDictionary</returns>
        public BamlLocalizationDictionary ExtractResources()
        {
            return _bamlTreeMap.LocalizationDictionary.Copy();
        }

        /// <summary>
        /// Udpate the source baml into a target stream with all the resource udpates applied.
        /// </summary>
        /// <param name="target">target stream</param>
        /// <param name="updates">resource updates to be applied when generating the localized baml</param>
        public void UpdateBaml(
            Stream                      target, 
            BamlLocalizationDictionary  updates
            )
        {            
            if (target  == null)
            {
                throw new ArgumentNullException("target");
            }
            if (updates == null)
            {
                throw new ArgumentNullException("updates");
            }

            // we duplicate the internal baml tree here because 
            // we will need to modify the tree to do generation
            // UpdateBaml can be called multiple times
            BamlTree _duplicateTree = _tree.Copy();     
            _bamlTreeMap.EnsureMap();
            
            // Udpate the tree
            BamlTreeUpdater.UpdateTree(
                _duplicateTree, 
                _bamlTreeMap, 
                updates            
                );     
           
            // Serialize the tree into Baml
            BamlResourceSerializer.Serialize(
                this,
                _duplicateTree, 
                target
                );                
        }

        /// <summary>
        /// Raised by BamlLocalizer when it encounters any abnormal conditions. 
        /// </summary>
        /// <remarks>
        /// BamlLocalizer can normally ignore the errors and continue the localization. 
        /// As a result, the output baml will only be partially localized. The client 
        /// can choose to stop the BamlLocalizer on error by throw exceptions in their 
        /// event handlers.  
        /// </remarks>
        public event BamlLocalizerErrorNotifyEventHandler ErrorNotify; 

        /// <summary>
        /// Raise ErrorNotify event 
        /// </summary>
        protected virtual void OnErrorNotify(BamlLocalizerErrorNotifyEventArgs e)
        {
            BamlLocalizerErrorNotifyEventHandler handler = ErrorNotify;
            if (handler != null) 
            {
                handler(this, e);
            }
        }        

        //----------------------------------
        // Internal method
        //----------------------------------
        internal void RaiseErrorNotifyEvent(BamlLocalizerErrorNotifyEventArgs e)
        {
            OnErrorNotify(e);
        }

        //----------------------------------
        // Private member
        //----------------------------------                
        private BamlTreeMap _bamlTreeMap;
        private BamlTree    _tree;           
    }

    /// <summary>
    /// The delegate for the BamlLocalizer.ErrorNotify event
    /// </summary>
    public delegate void BamlLocalizerErrorNotifyEventHandler(object sender, BamlLocalizerErrorNotifyEventArgs e);
}
