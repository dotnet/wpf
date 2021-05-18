// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !DONOTREFPRINTINGASMMETA
// 
//
// Description: Plug-in document serializers implement this class
//
//              See spec at <Need to post existing spec>
// 
namespace System.Windows.Documents.Serialization
{
    using System;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Windows;
    using System.Security;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Win32;
    using MS.Internal.PresentationFramework;

    /// <summary>
    /// SerializerDescriptor describes an individual plug-in serializer
    /// </summary>
    public sealed class SerializerDescriptor
    {
        #region Constructors

        private SerializerDescriptor()
        {
        }

        #endregion

        #region Private Methods

        private static string GetNonEmptyRegistryString(RegistryKey key, string value)
        {
            string result = key.GetValue(value) as string;
            if ( result == null )
            {
                throw new KeyNotFoundException();
            }

            return result;
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a SerializerDescriptor. The interface must be defined in the calling assembly
        /// The WinFX version, and assembly name, and version are initialized by reflecting on the calling assembly
        /// </summary>
        /// <remarks>
        ///     Create a SerializerDescriptor from a ISerializerFactory instance
        ///
        ///     This method currently requires full trust to run.
        /// </remarks>
        public static SerializerDescriptor CreateFromFactoryInstance(
            ISerializerFactory  factoryInstance
            )
        {

            if (factoryInstance == null)
            {
                throw new ArgumentNullException("factoryInstance");
            }
            if (factoryInstance.DisplayName == null)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderDisplayNameNull));
            }
            if (factoryInstance.ManufacturerName == null)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderManufacturerNameNull));
            }
            if (factoryInstance.ManufacturerWebsite == null)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderManufacturerWebsiteNull));
            }
            if (factoryInstance.DefaultFileExtension == null)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderDefaultFileExtensionNull));
            }

            SerializerDescriptor sd = new SerializerDescriptor();

            sd._displayName = factoryInstance.DisplayName;
            sd._manufacturerName = factoryInstance.ManufacturerName;
            sd._manufacturerWebsite = factoryInstance.ManufacturerWebsite;
            sd._defaultFileExtension = factoryInstance.DefaultFileExtension;

            // When this is called with an instantiated factory object, it must be loadable
            sd._isLoadable = true;

            Type factoryType = factoryInstance.GetType();
            sd._assemblyName = factoryType.Assembly.FullName;
            sd._assemblyPath = factoryType.Assembly.Location;
            sd._assemblyVersion = factoryType.Assembly.GetName().Version;
            sd._factoryInterfaceName = factoryType.FullName;
            sd._winFXVersion = typeof(System.Windows.Controls.Button).Assembly.GetName().Version;

            return sd;
        }

        /// <summary>
        /// From a SerializerDescriptor (which required full trust to create)
        /// creates an ISerializerFactory by loading the assembly and reflecting on the type
        /// </summary>
        /// <remarks>
        ///     Create an ISerializerFactory from a SerializerDescriptor
        ///
        ///     This method currently requires full trust to run.
        /// </remarks>
        [SuppressMessage("Microsoft.Security", "CA2001:AvoidCallingProblematicMethods")]
        internal ISerializerFactory CreateSerializerFactory()
        {

            string assemblyPath = AssemblyPath;

            // This is the only way to load the plug-in assembly. The only permitted permission
            // if file read access to the actual plug-in assembly.
            Assembly plugIn = Assembly.LoadFrom(assemblyPath);
            ISerializerFactory factory = plugIn.CreateInstance(FactoryInterfaceName) as ISerializerFactory;

            return factory;
        }

        #endregion

        #region Internal Methods

        internal void WriteToRegistryKey(RegistryKey key)
        {
            key.SetValue("uiLanguage",              CultureInfo.CurrentUICulture.Name);
            key.SetValue("displayName",             this.DisplayName);
            key.SetValue("manufacturerName",        this.ManufacturerName);
            key.SetValue("manufacturerWebsite",     this.ManufacturerWebsite);
            key.SetValue("defaultFileExtension",    this.DefaultFileExtension);
            key.SetValue("assemblyName",            this.AssemblyName);
            key.SetValue("assemblyPath",            this.AssemblyPath);
            key.SetValue("factoryInterfaceName",    this.FactoryInterfaceName);
            key.SetValue("assemblyVersion",         this.AssemblyVersion.ToString());
            key.SetValue("winFXVersion",            this.WinFXVersion.ToString());
        }

        /// <summary>
        /// Load a SerializerDescriptor from the registry
        /// </summary>
        /// <remarks>
        ///     Create a SerializerDescriptor from the registry
        ///
        ///     This method currently requires full trust to run.
        /// </remarks>
        internal static SerializerDescriptor CreateFromRegistry(RegistryKey plugIns, string keyName)
        {

            SerializerDescriptor sd = new SerializerDescriptor();

            try
            {
                RegistryKey key = plugIns.OpenSubKey(keyName);

                sd._displayName = GetNonEmptyRegistryString(key, "displayName");
                sd._manufacturerName =          GetNonEmptyRegistryString(key, "manufacturerName");
                sd._manufacturerWebsite =       new Uri(GetNonEmptyRegistryString(key, "manufacturerWebsite"));
                sd._defaultFileExtension =      GetNonEmptyRegistryString(key, "defaultFileExtension");

                sd._assemblyName =              GetNonEmptyRegistryString(key, "assemblyName");
                sd._assemblyPath =              GetNonEmptyRegistryString(key, "assemblyPath");
                sd._factoryInterfaceName =      GetNonEmptyRegistryString(key, "factoryInterfaceName");
                sd._assemblyVersion =           new Version(GetNonEmptyRegistryString(key, "assemblyVersion"));
                sd._winFXVersion =              new Version(GetNonEmptyRegistryString(key, "winFXVersion"));

                string uiLanguage =             GetNonEmptyRegistryString(key, "uiLanguage");

                key.Close();

                // update language strings. 
                if (!uiLanguage.Equals(CultureInfo.CurrentUICulture.Name))
                {
                    ISerializerFactory factory = sd.CreateSerializerFactory();

                    sd._displayName = factory.DisplayName;
                    sd._manufacturerName = factory.ManufacturerName;
                    sd._manufacturerWebsite = factory.ManufacturerWebsite;
                    sd._defaultFileExtension = factory.DefaultFileExtension;

                    key = plugIns.CreateSubKey(keyName);
                    sd.WriteToRegistryKey(key);
                    key.Close();
                }
            }
            catch (KeyNotFoundException)
            {
                sd = null;
            }

            if (sd != null)
            {
                // This will be noted in the release notes as an unsupported API until 4479 is fixed.
                // https://github.com/dotnet/wpf/issues/4479 
                #pragma warning disable SYSLIB0018 // 'Assembly.ReflectionOnlyLoadFrom(string)' is obsolete: 'ReflectionOnly loading is not su pported and throws PlatformNotSupportedException.'
                Assembly plugIn = Assembly.ReflectionOnlyLoadFrom(sd._assemblyPath);
                #pragma warning restore SYSLIB0018 // 'Assembly.ReflectionOnlyLoadFrom(string)' is obsolete: 'ReflectionOnly loading is not supported and throws PlatformNotSupportedException.'
                if (typeof(System.Windows.Controls.Button).Assembly.GetName().Version == sd._winFXVersion &&
                        plugIn != null &&
                        plugIn.GetName().Version == sd._assemblyVersion)
                {
                    sd._isLoadable = true;
                }
            }

            return sd;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// DisplayName of the Serializer
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        /// <summary>
        /// ManufacturerName of the Serializer
        /// </summary>
        public string ManufacturerName
        {
            get
            {
                return _manufacturerName;
            }
        }

        /// <summary>
        /// ManufacturerWebsite of the Serializer
        /// </summary>
        public Uri ManufacturerWebsite
        {
            get
            {
                return _manufacturerWebsite;
            }
        }

        /// <summary>
        /// Default File Extension of files produced the Serializer
        /// </summary>
        public string DefaultFileExtension
        {
            get
            {
                return _defaultFileExtension;
            }
        }

        /// <summary>
        /// AssemblyName of the Serializer
        /// </summary>
        public string AssemblyName
        {
            get
            {
                return _assemblyName;
            }
        }

        /// <summary>
        /// AssemblyPath of the Serializer
        /// </summary>
        public string AssemblyPath
        {
            get
            {
                return _assemblyPath;
            }
        }

        /// <summary>
        /// FactoryInterfaceName of the Serializer
        /// </summary>
        public string FactoryInterfaceName
        {
            get
            {
                return _factoryInterfaceName;
            }
        }

        /// <summary>
        /// AssemblyVersion of the Serializer
        /// </summary>
        public Version AssemblyVersion
        {
            get
            {
                return _assemblyVersion;
            }
        }

        /// <summary>
        /// DisplayName of the Serializer
        /// </summary>
        public Version WinFXVersion
        {
            get
            {
                return _winFXVersion;
            }
        }

        /// <summary>
        /// returns false if serializer plug-in cannot be loaded on current WinFX version
        /// </summary>
        public bool IsLoadable
        {
            get
            {
                return _isLoadable;
            }
        }


        /// <summary>
        /// Compares two SerializerDescriptor for equality
        /// </summary>
        public override bool Equals(object obj)
        {
            SerializerDescriptor sd = obj as SerializerDescriptor;
            if (sd != null)
            {
                return sd._displayName == _displayName
                && sd._assemblyName == _assemblyName
                && sd._assemblyPath == _assemblyPath
                && sd._factoryInterfaceName == _factoryInterfaceName
                && sd._defaultFileExtension == _defaultFileExtension
                && sd._assemblyVersion == _assemblyVersion
                && sd._winFXVersion == _winFXVersion;
            }
            return false;
        }

        /// <summary>
        /// Returns a hashcode for this serializer
        /// </summary>
        public override int GetHashCode()
        {
            string id = _displayName + "/" + _assemblyName + "/" + _assemblyPath + "/" + _factoryInterfaceName + "/" + _assemblyVersion + "/" + _winFXVersion;
            return id.GetHashCode();
        }

        #endregion

        #region Data

        private string      _displayName;
        private string      _manufacturerName;
        private Uri         _manufacturerWebsite;
        private string      _defaultFileExtension;
        private string      _assemblyName;
        private string      _assemblyPath;
        private string      _factoryInterfaceName;
        private Version     _assemblyVersion;
        private Version     _winFXVersion;
        private bool        _isLoadable;

        #endregion
    }
}
#endif
