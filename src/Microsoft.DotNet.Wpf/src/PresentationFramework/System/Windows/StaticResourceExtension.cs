// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Class for Xaml markup extension for static resource references.
*
*
\***************************************************************************/
using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Diagnostics;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Reflection;
using MS.Internal;
using System.Xaml;
using System.Collections.Generic;

namespace System.Windows
{
    /// <summary>
    ///  Class for Xaml markup extension for static resource references.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    [Localizability(LocalizationCategory.NeverLocalize)] // cannot be localized
    public class StaticResourceExtension : MarkupExtension
    {
        /// <summary>
        ///  Constructor that takes no parameters
        /// </summary>
        public StaticResourceExtension()
        {
        }

        /// <summary>
        ///  Constructor that takes the resource key that this is a static reference to.
        /// </summary>
        public StaticResourceExtension(
            object resourceKey)
        {
            if (resourceKey == null)
            {
                throw new ArgumentNullException("resourceKey");
            }
            _resourceKey = resourceKey;
        }

        /// <summary>
        ///  Return an object that should be set on the targetObject's targetProperty
        ///  for this markup extension.  For StaticResourceExtension, this is the object found in
        ///  a resource dictionary in the current parent chain that is keyed by ResourceKey
        /// </summary>
        /// <param name="serviceProvider">Object that can provide services for the markup extension.</param>
        /// <returns>
        ///  The object to set on this property.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (ResourceKey is SystemResourceKey)
            {
                return (ResourceKey as SystemResourceKey).Resource;
            }

            return ProvideValueInternal(serviceProvider, false);
        }

        /// <summary>
        ///  The key in a Resource Dictionary used to find the object refered to by this
        ///  Markup Extension.
        /// </summary>
        [ConstructorArgument("resourceKey")]
        public object ResourceKey
        {
            get { return _resourceKey; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _resourceKey = value;
            }
        }

        /// <summary>
        /// This value is used to resolved StaticResourceIds
        /// </summary>
        internal virtual DeferredResourceReference PrefetchedValue
        {
            get { return null; }
        }

        internal object ProvideValueInternal(IServiceProvider serviceProvider, bool allowDeferredReference)
        {
            object value = TryProvideValueInternal(serviceProvider, allowDeferredReference, false /* mustReturnDeferredResourceReference */);

            if (value == DependencyProperty.UnsetValue)
            {
                throw new Exception(SR.Get(SRID.ParserNoResource, ResourceKey.ToString()));
            }
            return value;
        }

        internal object TryProvideValueInternal(IServiceProvider serviceProvider, bool allowDeferredReference, bool mustReturnDeferredResourceReference)
        {
            if (!ResourceDictionaryDiagnostics.HasStaticResourceResolvedListeners)
            {
                return TryProvideValueImpl(serviceProvider, allowDeferredReference, mustReturnDeferredResourceReference);
            }
            else
            {
                return TryProvideValueWithDiagnosticEvent(serviceProvider, allowDeferredReference, mustReturnDeferredResourceReference);
            }
        }

        private object TryProvideValueWithDiagnosticEvent(IServiceProvider serviceProvider, bool allowDeferredReference, bool mustReturnDeferredResourceReference)
        {
            IProvideValueTarget provideValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (provideValueTarget == null || provideValueTarget.TargetObject == null || provideValueTarget.TargetProperty == null
                || ResourceDictionaryDiagnostics.ShouldIgnoreProperty(provideValueTarget.TargetProperty))
            {
                // if there's no target, or target is ignorable, get the value the quick way.
                return TryProvideValueImpl(serviceProvider, allowDeferredReference, mustReturnDeferredResourceReference);
            }

            ResourceDictionaryDiagnostics.LookupResult result;
            object value;
            bool success = false;

            try
            {
                // request a result of the lookup
                result = ResourceDictionaryDiagnostics.RequestLookupResult(this);

                // do the lookup - the ResourceDictionary that resolves the reference will fill in the result
                value = TryProvideValueImpl(serviceProvider, allowDeferredReference, mustReturnDeferredResourceReference);

                // for the purposes of diagnostics, a deferred reference is a success -
                // we only need the dictionary that holds the value, not the value itself
                DeferredResourceReference deferredReference = value as DeferredResourceReference;
                if (deferredReference != null)
                {
                    success = true;
                    ResourceDictionary dict = deferredReference.Dictionary;
                    if (dict != null)
                    {
                        // the result may not have been recorded yet
                        ResourceDictionaryDiagnostics.RecordLookupResult(ResourceKey, dict);
                    }
                }
                else
                {
                    success = (value != DependencyProperty.UnsetValue);
                }
            }
            finally
            {
                // revert the request
                ResourceDictionaryDiagnostics.RevertRequest(this, success);
            }

            // raise the diagnostic event
            if (success)
            {
                ResourceDictionaryDiagnostics.OnStaticResourceResolved(provideValueTarget.TargetObject, provideValueTarget.TargetProperty, result);
            }

            return value;
        }

        private object TryProvideValueImpl(IServiceProvider serviceProvider, bool allowDeferredReference, bool mustReturnDeferredResourceReference)
        {
            // Get prefetchedValue
            DeferredResourceReference prefetchedValue = PrefetchedValue;

            object value;
            if (prefetchedValue == null)
            {
                // Do a normal look up.
                value = FindResourceInEnviroment(serviceProvider, allowDeferredReference, mustReturnDeferredResourceReference);
            }
            else
            {
                // If we have a Deferred Value, first check the current parse stack for a better (nearer)
                // value.  This happens when this is a parse of deferred content and there is another
                // Resource Dictionary availible above this, yet still part of this deferred content.
                // This searches up to the outer most enclosing resource dictionary.
                value = FindResourceInDeferredContent(serviceProvider, allowDeferredReference, mustReturnDeferredResourceReference);

                // If we didn't find a new value in this part of deferred content
                // then use the existing prefetchedValue (DeferredResourceReference)
                if (value == DependencyProperty.UnsetValue)
                {
                    value = allowDeferredReference
                             ? prefetchedValue
                             : prefetchedValue.GetValue(BaseValueSourceInternal.Unknown);
                }

            }
            return value;
        }

        private ResourceDictionary FindTheResourceDictionary(IServiceProvider serviceProvider, bool isDeferredContentSearch)
        {
            var schemaContextProvider = serviceProvider.GetService(typeof(IXamlSchemaContextProvider)) as IXamlSchemaContextProvider;
            if (schemaContextProvider == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.MarkupExtensionNoContext, GetType().Name, "IXamlSchemaContextProvider"));
            }

            var ambientProvider = serviceProvider.GetService(typeof(IAmbientProvider)) as IAmbientProvider;
            if (ambientProvider == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.MarkupExtensionNoContext, GetType().Name, "IAmbientProvider"));
            }

            XamlSchemaContext schemaContext = schemaContextProvider.SchemaContext;

            // This seems like a lot of work to do on every Provide Value
            // but that types and properties are cached in the schema.
            //
            XamlType feXType = schemaContext.GetXamlType(typeof(FrameworkElement));
            XamlType styleXType = schemaContext.GetXamlType(typeof(Style));
            XamlType templateXType = schemaContext.GetXamlType(typeof(FrameworkTemplate));
            XamlType appXType = schemaContext.GetXamlType(typeof(Application));
            XamlType fceXType = schemaContext.GetXamlType(typeof(FrameworkContentElement));

            XamlMember fceResourcesProperty = fceXType.GetMember("Resources");
            XamlMember feResourcesProperty = feXType.GetMember("Resources");
            XamlMember styleResourcesProperty = styleXType.GetMember("Resources");
            XamlMember styleBasedOnProperty = styleXType.GetMember("BasedOn");
            XamlMember templateResourcesProperty = templateXType.GetMember("Resources");
            XamlMember appResourcesProperty = appXType.GetMember("Resources");

            XamlType[] types = new XamlType[1] { schemaContext.GetXamlType(typeof(ResourceDictionary)) };

            IEnumerable<AmbientPropertyValue> ambientValues = null;

            ambientValues = ambientProvider.GetAllAmbientValues(null,    // ceilingTypes
                                                                isDeferredContentSearch,
                                                                types,
                                                                fceResourcesProperty,
                                                                feResourcesProperty,
                                                                styleResourcesProperty,
                                                                styleBasedOnProperty,
                                                                templateResourcesProperty,
                                                                appResourcesProperty);

            List<AmbientPropertyValue> ambientList;
            ambientList = ambientValues as List<AmbientPropertyValue>;
            Debug.Assert(ambientList != null, "IAmbientProvider.GetAllAmbientValues no longer returns List<>, please copy the list");

            for(int i=0; i<ambientList.Count; i++)
            {
                AmbientPropertyValue ambientValue = ambientList[i];

                if (ambientValue.Value is ResourceDictionary)
                {
                    var resourceDictionary = (ResourceDictionary)ambientValue.Value;
                    if (resourceDictionary.Contains(ResourceKey))
                    {
                        return resourceDictionary;
                    }
                }
                if (ambientValue.Value is Style)
                {
                    var style = (Style)ambientValue.Value;
                    var resourceDictionary = style.FindResourceDictionary(ResourceKey);
                    if (resourceDictionary != null)
                    {
                        return resourceDictionary;
                    }
                }
            }
            return null;
        }

        internal object FindResourceInDeferredContent(IServiceProvider serviceProvider, bool allowDeferredReference, bool mustReturnDeferredResourceReference)
        {
            ResourceDictionary dictionaryWithKey = FindTheResourceDictionary(serviceProvider, /* isDeferredContentSearch */ true);
            object value = DependencyProperty.UnsetValue;

            if (dictionaryWithKey != null)
            {
                value = dictionaryWithKey.Lookup(ResourceKey, allowDeferredReference, mustReturnDeferredResourceReference, canCacheAsThemeResource:false);
            }
            if (mustReturnDeferredResourceReference && value == DependencyProperty.UnsetValue)
            {
                value = new DeferredResourceReferenceHolder(ResourceKey, value);
            }
            return value;
        }

        /// <summary>
        /// Search just the App and Theme Resources.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="allowDeferredReference"></param>
        /// <param name="mustReturnDeferredResourceReference"></param>
        /// <returns></returns>
        private object FindResourceInAppOrSystem(IServiceProvider serviceProvider,
                                                bool allowDeferredReference,
                                                bool mustReturnDeferredResourceReference)
        {
            object val;
            if (!SystemResources.IsSystemResourcesParsing)
            {
                object source;
                val = FrameworkElement.FindResourceFromAppOrSystem(ResourceKey,
                                                                   out source,
                                                                   false,  // disableThrowOnResourceFailure
                                                                   allowDeferredReference,
                                                                   mustReturnDeferredResourceReference);
            }
            else
            {
                val = SystemResources.FindResourceInternal(ResourceKey,
                                                           allowDeferredReference,
                                                           mustReturnDeferredResourceReference);
            }
            return val;
        }


        private object FindResourceInEnviroment(IServiceProvider serviceProvider,
                                                bool allowDeferredReference,
                                                bool mustReturnDeferredResourceReference)
        {
            ResourceDictionary dictionaryWithKey = FindTheResourceDictionary(serviceProvider, /* isDeferredContentSearch */ false);

            if (dictionaryWithKey != null)
            {
                object value = dictionaryWithKey.Lookup(ResourceKey, allowDeferredReference, mustReturnDeferredResourceReference, canCacheAsThemeResource:false);
                return value;
            }

            // Look at app or themes
            object val = FindResourceInAppOrSystem(serviceProvider, allowDeferredReference, mustReturnDeferredResourceReference);
            if (val == null)
            {
                val = DependencyProperty.UnsetValue;
            }

            if (mustReturnDeferredResourceReference)
            {
                if (!(val is DeferredResourceReference))
                {
                    val = new DeferredResourceReferenceHolder(ResourceKey, val);
                }
            }
            return val;
        }

        private object _resourceKey;
    }
}

