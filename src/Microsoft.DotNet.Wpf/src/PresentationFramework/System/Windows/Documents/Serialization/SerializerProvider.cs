// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !DONOTREFPRINTINGASMMETA
// 
//
// Description: Manages plug-in document serializers
//
//              See spec at <Need to post existing spec>
// 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Security;
using System.Windows.Xps.Serialization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using MS.Internal.PresentationFramework;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Documents.Serialization
{
    /// <summary>
    /// SerializerProvider enumerates plug-in serializers
    /// </summary>
    public sealed class SerializerProvider
    {
        #region Constructors

        /// <summary>
        /// creates a SerializerProvider
        /// The constructor accesses the registry to find all installed plug-ins
        /// </summary>
        /// <remarks>
        ///     Create a SerializerProvider listing all installed serializers (from the registry)
        ///
        ///     This method currently requires full trust to run.
        /// </remarks>
        public SerializerProvider()
        {

            SerializerDescriptor sd = null;

            List<SerializerDescriptor>  installedSerializers = new List<SerializerDescriptor>();

            sd = CreateSystemSerializerDescriptor();

            if (sd != null)
            {
                installedSerializers.Add(sd);
            }

            RegistryKey plugIns = _rootKey.CreateSubKey(_registryPath);
            
            if ( plugIns != null )
            {
                foreach ( string keyName in plugIns.GetSubKeyNames())
                {
                    sd = SerializerDescriptor.CreateFromRegistry(plugIns, keyName);
                    if (sd != null)
                    {
                        installedSerializers.Add(sd);
                    }
                }

                plugIns.Close();
            }

            _installedSerializers = installedSerializers.AsReadOnly();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers the serializer plug-in identified by serializerDescriptor in the registry
        /// </summary>
        public static void RegisterSerializer(SerializerDescriptor serializerDescriptor, bool overwrite)
        {

            if (serializerDescriptor == null)
            {
                throw new ArgumentNullException("serializerDescriptor");
            }

            RegistryKey plugIns = _rootKey.CreateSubKey(_registryPath);
            string serializerKey = serializerDescriptor.DisplayName + "/" + serializerDescriptor.AssemblyName + "/" + serializerDescriptor.AssemblyVersion + "/" + serializerDescriptor.WinFXVersion;

            if (!overwrite && plugIns.OpenSubKey(serializerKey) != null)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderAlreadyRegistered), serializerKey);
            }

            RegistryKey newPlugIn = plugIns.CreateSubKey(serializerKey);
            serializerDescriptor.WriteToRegistryKey(newPlugIn);
            newPlugIn.Close();
        }

        /// <summary>
        /// Un-Registers the serializer plug-in identified by serializerDescriptor in the registry
        /// </summary>
        /// <remarks>
        ///     Removes a previously installed plug-n serialiazer from the registry
        ///
        ///     This method currently requires full trust to run.
        /// </remarks>
        public static void UnregisterSerializer(SerializerDescriptor serializerDescriptor)
        {

            if (serializerDescriptor == null)
            {
                throw new ArgumentNullException("serializerDescriptor");
            }

            RegistryKey plugIns = _rootKey.CreateSubKey(_registryPath);
            string serializerKey = serializerDescriptor.DisplayName + "/" + serializerDescriptor.AssemblyName + "/" + serializerDescriptor.AssemblyVersion + "/" + serializerDescriptor.WinFXVersion;

            if (plugIns.OpenSubKey(serializerKey) == null)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderNotRegistered), serializerKey);
            }

            plugIns.DeleteSubKeyTree(serializerKey);
        }

        /// <summary>
        /// Create a SerializerWriter identified by the passed in SerializerDescriptor on the passed in stream
        /// </summary>
        /// <remarks>
        ///     With a SerializerProvider (which requires full trust to ctor) and a SerializerDescriptor (which requires
        ///     full trust to obtain) create a SerializerWriter 
        ///
        ///     This method currently requires full trust to run.
        /// </remarks>
        public SerializerWriter CreateSerializerWriter(SerializerDescriptor serializerDescriptor, Stream stream)
        {

            SerializerWriter serializerWriter = null;

            if (serializerDescriptor == null)
            {
                throw new ArgumentNullException("serializerDescriptor");
            }

            string serializerKey = serializerDescriptor.DisplayName + "/" + serializerDescriptor.AssemblyName + "/" + serializerDescriptor.AssemblyVersion + "/" + serializerDescriptor.WinFXVersion;

            if (!serializerDescriptor.IsLoadable)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderWrongVersion), serializerKey);
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            bool found = false;
            foreach (SerializerDescriptor sd in InstalledSerializers)
            {
                if (sd.Equals(serializerDescriptor))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderUnknownSerializer), serializerKey);
            }

            try
            {
                ISerializerFactory factory = serializerDescriptor.CreateSerializerFactory();

                serializerWriter = factory.CreateSerializerWriter(stream);
            }
            catch (FileNotFoundException)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderCannotLoad), serializerDescriptor.DisplayName);
            }
            catch (FileLoadException)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderCannotLoad), serializerDescriptor.DisplayName);
            }
            catch (BadImageFormatException)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderCannotLoad), serializerDescriptor.DisplayName);
            }
            catch (MissingMethodException)
            {
                throw new ArgumentException(SR.Get(SRID.SerializerProviderCannotLoad), serializerDescriptor.DisplayName);
            }

            return serializerWriter;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Uses reflection to create the XpsSerializer
        /// </summary>
        /// <remarks>
        ///     Creates the Xps default serializer
        /// </remarks>
        /// <returns>SerializerDescriptor for new serializer</returns>
        private SerializerDescriptor CreateSystemSerializerDescriptor()
        {

            SerializerDescriptor serializerDescriptor = null;

            // The XpsSerializer (our default document serializer) is defined in ReachFramework.dll
            // But callers can only get here if the above demand succeeds, so they are already fully trusted
            serializerDescriptor = SerializerDescriptor.CreateFromFactoryInstance(
                                        new XpsSerializerFactory()
                                        );

            return serializerDescriptor;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns collection of installed serializers
        /// </summary>
        public ReadOnlyCollection<SerializerDescriptor> InstalledSerializers
        {
            get
            {
                return _installedSerializers;
            }
        }

        #endregion

        #region Data

        private const string _registryPath =            @"SOFTWARE\Microsoft\WinFX Serializers";
        private static readonly RegistryKey _rootKey =  Registry.LocalMachine;

        private ReadOnlyCollection<SerializerDescriptor> _installedSerializers;

        #endregion
    }
}
#endif
