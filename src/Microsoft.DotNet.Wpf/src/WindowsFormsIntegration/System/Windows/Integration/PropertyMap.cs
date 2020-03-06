// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Data;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Interop;

using MS.Win32;

using SD = System.Drawing;
using SWF = System.Windows.Forms;

using SW = System.Windows;
using SWC = System.Windows.Controls;
using SWM = System.Windows.Media;
using SWMI = System.Windows.Media.Imaging;
using SWS = System.Windows.Markup;
using SWI = System.Windows.Input;
using System.Reflection;

namespace System.Windows.Forms.Integration
{
    /// <summary>
    /// Provides a translation function for a mapped property of the host control.
    /// </summary>
    /// <param name="host">The host control (either WindowsFormsHost or ElementHost) whose property is being mapped.</param>
    /// <param name="propertyName">The name of the property being translated.</param>
    /// <param name="value">The new value of the property</param>
    public delegate void PropertyTranslator(object host, String propertyName, object value);

    /// <summary>
    /// Defines how property changes in the host control are mapped to the hosted control or element.
    /// </summary>
    public class PropertyMap
    {
        private object _sourceObject;
        private Dictionary<string, PropertyTranslator> _defaultTranslators;
        private Dictionary<String, PropertyTranslator> _wrappedDictionary;
    
        /// <summary>
        ///     Initializes a new instance of the PropertyMap class.
        /// </summary>
        public PropertyMap()
        {
            _wrappedDictionary = new Dictionary<string, PropertyTranslator>();
        }

        /// <summary>
        ///     Initializes a new instance of a System.Windows.Forms.Integration.PropertyMap.
        /// </summary>
        public PropertyMap(object source)
            : this()
        {
            _sourceObject = source;
        }

        /// <summary>
        ///     Identifies the source of the properties in the property map. 
        /// </summary>
        protected object SourceObject
        {
            get
            {
                return _sourceObject;
            }
        }

        /// <summary>
        ///     Gets or sets the PropertyTranslator for the specified property name. If no 
        ///     PropertyTranslator exists, it will be added to the property map. If a 
        ///     PropertyTranslator already exists, it will be replaced.
        /// </summary>
        /// <param name="propertyName">The name of the host control property whose PropertyTranslator you want to get 
        /// or set.</param>
        /// <returns></returns>
        public PropertyTranslator this[String propertyName]
        {
            get
            {
                ThrowIfPropertyDoesntExistOnSource(propertyName);
                PropertyTranslator value;
                if (_wrappedDictionary.TryGetValue(propertyName, out value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                //Run through our regular invalid property checks, then
                //apply the translator
                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_NullArgument), "propertyName"));
                }
                if (value == null)
                {
                    throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_NullArgument), "translator"));
                }
                ThrowIfPropertyDoesntExistOnSource(propertyName);
                _wrappedDictionary[propertyName] = value;    //This will replace an existing mapping, unlike Add.
                Apply(propertyName);
            }
        }

        /// <summary>
        ///     Returns an ICollection of strings identifying all of the host control 
        ///     properties that have been mapped.  
        /// </summary>
        public ICollection Keys
        {
            get
            {
                return _wrappedDictionary.Keys;
            }
        }

        /// <summary>
        ///     Returns an ICollection of all PropertyTranslator delegates currently being used to 
        ///     map the host control properties.    
        /// </summary>
        public ICollection Values
        {
            get
            {
                return _wrappedDictionary.Values;
            }
        }

        /// <summary>
        ///     Adds a PropertyTranslator delegate to the property map that runs when the specified property 
        ///     of the host control changes. 
        /// </summary>
        /// <param name="propertyName">A string containing the name of the host control property to translate.</param>
        /// <param name="translator">A PropertyTranslator delegate which will be called when the specificied property changes.</param>
        public void Add(String propertyName, PropertyTranslator translator)
        {
            if (Contains(propertyName))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_PropertyMappingExists), propertyName));
            }
            this[propertyName] = translator;
        }

        private void ThrowIfPropertyDoesntExistOnSource(string propertyName)
        {
            if (SourceObject != null)
            {
                if (GetProperty(propertyName) == null)
                {
                    // Property 'Foreground' doesn't exist on type 'Window'
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_PropertyDoesntExist), propertyName, SourceObject.GetType().FullName));
                }
            }
        }

        /// <summary>
        ///     Runs the PropertyTranslator for the given property, based on the 
        ///     SourceObject's current value.
        /// </summary>
        /// <param name="propertyName"></param>
        public void Apply(string propertyName)
        {
            if (Contains(propertyName))
            {
                ThrowIfPropertyDoesntExistOnSource(propertyName);
                PropertyInfo property = GetProperty(propertyName);
                if (property != null)
                {
                    RunTranslator(this[propertyName], SourceObject, propertyName, property.GetValue(SourceObject, null));
                }
            }
        }

        /// <summary>
        ///     Runs the PropertyTranslator for all properties, based on the 
        ///     SourceObject's current values.
        /// </summary>
        public void ApplyAll()
        {
            foreach (KeyValuePair<string, PropertyTranslator> entry in DefaultTranslators)
            {
                Apply(entry.Key);
            }
        }

        private PropertyInfo GetProperty(string propertyName)
        {
            if (SourceObject == null)
            {
                return null;
            }
            return SourceObject.GetType().GetProperty(propertyName, Type.EmptyTypes);
        }

        /// <summary>
        ///     Removes all property mappings from the property map so that the properties 
        ///     are no longer translated.
        /// </summary>
        public void Clear()
        {
            _wrappedDictionary.Clear();
        }

        /// <summary>
        ///     Determines whether the specified property is being mapped.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the host control property.</param>
        /// <returns></returns>
        public bool Contains(String propertyName)
        {
            return _wrappedDictionary.ContainsKey(propertyName);
        }

        /// <summary>
        ///     Removes the specified property from the property map so that the property is 
        ///     no longer translated.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the host control property.</param>
        public void Remove(String propertyName)
        {
            _wrappedDictionary.Remove(propertyName);
        }

        /// <summary>
        ///     Gets the list of properties that are translated by default.
        /// </summary>
        protected Dictionary<string, PropertyTranslator> DefaultTranslators
        {
            get
            {
                if (_defaultTranslators == null)
                {
                    _defaultTranslators = new Dictionary<string, PropertyTranslator>();
                }
                return _defaultTranslators;
            }
        }

        internal bool PropertyMappedToEmptyTranslator(string propertyName)
        {
            return (this[propertyName] == EmptyPropertyTranslator);
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        internal void EmptyPropertyTranslator(object host, string propertyName, object value)
        {
        }



        /// <summary>
        ///     Restores the default property mapping for the specified property.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the host control property.</param>
        public void Reset(String propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_ArgumentNullOrEmpty), "propertyName"));
            }

            //Resetting means get the value from the list of default values,
            //if it's there, otherwise removing the item you've added.
            PropertyTranslator value;
            if (DefaultTranslators.TryGetValue(propertyName, out value))
            {
                this[propertyName] = value;
            }
            else
            {
                Remove(propertyName);
            }
        }

        /// <summary>
        ///     Restores the default property mapping for all of the host control's properties.
        /// </summary>
        public void ResetAll()
        {
            Clear();
            AddDefaultPropertyTranslators();
        }

        private void AddDefaultPropertyTranslators()
        {
            foreach (KeyValuePair<string, PropertyTranslator> entry in DefaultTranslators)
            {
                Add(entry.Key, entry.Value);
            }
        }

        /// <summary>
        ///     Use this to look up and call the proper property translation delegate. This finds the
        ///     delegate in this mapping, and calls it with the parameters provided.
        /// </summary>
        /// <param name="host">The host control (either WindowsFormsHost or ElementHost) whose property is being mapped.</param>
        /// <param name="propertyName">The name of the property being translated.</param>
        /// <param name="value">The new value of the property.</param>
        internal void OnPropertyChanged(object host, string propertyName, object value)
        {
            PropertyTranslator translator;

            if (!_wrappedDictionary.TryGetValue(propertyName, out translator) || translator == null)
            {
                return;
            }

            RunTranslator(translator, host, propertyName, value);
        }

//Disable the PreSharp error 56500 Avoid `swallowing errors by catching non-specific exceptions.
//In this specific case, we are catching the exception and firing an event which can be handled in user code
//The user handling the event can make the determination on whether or not the exception should be rethrown
//(Wrapped in an InvalidOperationException).
#pragma warning disable 1634, 1691
#pragma warning disable 56500
        internal void RunTranslator(PropertyTranslator translator, object host, string propertyName, object value)
        {
            try
            {
                translator(host, propertyName, value);
            }
            catch (Exception ex)
            {
                PropertyMappingExceptionEventArgs args = new PropertyMappingExceptionEventArgs(ex, propertyName, value);
                if (_propertyMappingError != null)
                {
                    _propertyMappingError(SourceObject, args);
                }
                if (args.ThrowException)
                {
                    throw new InvalidOperationException(SR.Get(SRID.WFI_PropertyMapError), ex);
                }
            }
        }
#pragma warning restore 56500 
#pragma warning restore 1634, 1691

        private event EventHandler<PropertyMappingExceptionEventArgs> _propertyMappingError;

        /// <summary>
        /// Occurs when an exception is raised by a PropertyMap delegate.
        /// </summary>
        public event EventHandler<PropertyMappingExceptionEventArgs> PropertyMappingError
        {
            add { _propertyMappingError += value; }
            remove { _propertyMappingError -= value; }
        }
    }
}
