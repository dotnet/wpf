// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Threading;
using System.IO;
#if TESTBUILD_CLR20
// These includes are for the different Assembly.LoadFrom() overload used to make CLR 2.0 CAS policy work
using System.Security;
using System.Security.Policy;
#endif
using Microsoft.Test.FaultInjection.Constants;

namespace Microsoft.Test.FaultInjection
{
    internal static class Serializer
    {
        #region Private Data

        private static AssemblyResolver resolver = new AssemblyResolver();

        #endregion

        #region Public Members

        public static void SerializeRules(string fileName, FaultRule[] rules, Mutex mutex)
        {
            if (rules == null)
            {
                throw new FaultInjectionException(ApiErrorMessages.FaultRulesNullInSerialization);
            }

            // Recording locations for all assemblies loaded in AppDomain running test code
            // We need these information to find types while deserialization
            Dictionary<string, string> locations = new Dictionary<string, string>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    locations[assembly.FullName] = assembly.Location;
                }
                catch (NotSupportedException)
                {
                    // Swallow NotSupportedException for dynamic assemblies
                }
            }

            // Serialize fault rules into a temporary file
            BinaryFormatter formatter = new BinaryFormatter();
            string tempFile = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileName(Path.GetTempFileName()));
            using (FileStream stream = File.Open(tempFile, FileMode.Create))
            {
                formatter.Serialize(stream, locations);
                formatter.Serialize(stream, rules);
            }

            // Swap it to serialization file
            using (ScopedLock scope = new ScopedLock(mutex))
            {
                File.Delete(fileName);
                File.Move(tempFile, fileName);
            }
        }

        public static FaultRule[] DeserializeRules(string fileName, Mutex mutex)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (ScopedLock scope = new ScopedLock(mutex))
            {
                using (FileStream stream = File.Open(fileName, FileMode.Open))
                {
                    // Use assembly locations recorded to handle assembly resolvation
                    Dictionary<string, string> locations = (Dictionary<string, string>)formatter.Deserialize(stream);
                    resolver.AddAssemblyLocations(locations);

                    return (FaultRule[])formatter.Deserialize(stream);
                }
            }
        }

        public static byte[] SerializeRuleToBuffer(FaultRule rule)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, rule);
                return stream.GetBuffer();
            }
        }

        #endregion  

        #region Private Members

        private sealed class ScopedLock : IDisposable
        {
            private readonly Mutex mutex;
            public ScopedLock(Mutex mutex)
            {
                this.mutex = mutex;
                this.mutex.WaitOne();
            }
            public void Dispose()
            {
                this.mutex.ReleaseMutex();
            }
        }
        // We use this class to resolve assemblies which are visible to API but not AUT
        // It do this by find assembly locations recorded while serialize rules
        private sealed class AssemblyResolver
        {
            public AssemblyResolver()
            {
                // Kicks in when AUT can't find assembly by itself
                AppDomain.CurrentDomain.AssemblyResolve += this.AssemblyResolveEventHandler;
            }

            public void AddAssemblyLocations(Dictionary<string, string> newLocations)
            {
                foreach (KeyValuePair<string, string> pair in newLocations)
                {
                    this.locations[pair.Key] = pair.Value;
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001")] //Assembly.LoadFrom
            public Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
            {
                string location;
                if (locations.TryGetValue(args.Name, out location))
                {
#if TESTBUILD_CLR20
                    Evidence evidence = new Evidence();
                    evidence.AddHost(new Zone(SecurityZone.MyComputer));
                    return Assembly.LoadFrom(location, evidence);
#endif
#if TESTBUILD_CLR40
                    return Assembly.LoadFrom(location);
#endif
      
                }
                return null;    // Should I do log here?
            }

            private Dictionary<string, string> locations = new Dictionary<string, string>();
        }       

        #endregion  
    }
}
