// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Test.Execution.StateManagement
{
    //Note: Alas, the Object Serializer technology does not support generics/auto-detection of 
    //      derived types. If this suckitude were to be resolved, we could use specialized types directly
    //      for each kind of module.
    /// <summary>
    /// A generic container for State Modules. Implements the Strategy pattern to select the appropriate implementation.
    /// </summary>
    public class StateModule
    {
        /// <summary/>
        [SuppressMessage("Microsoft.Naming","CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public string Type { get; set; }
        
        /// <summary>
        /// The Path to operate upon - semantics here are defined by module Implementers
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Provides String representation for debugging purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Type + " " + Path;
        }

        private IStateImplementation stateImplementation;
        internal IStateImplementation StateImplementation
        {
            get 
            {
                if (stateImplementation == null)
                {
                    StateModuleType type = (StateModuleType)Enum.Parse(typeof(StateModuleType), Type);
                    switch (type)
                    {
                        case StateModuleType.ComRegistration:
                            stateImplementation = new ComStateImplementation();
                            break;
                        case StateModuleType.GlobalAssemblyCache:
                            stateImplementation = new GacStateImplementation();
                            break;
                        case StateModuleType.ColorProfile:
                            stateImplementation = new ColorProfileStateImplementation();
                            break;
                        case StateModuleType.DefaultWebBrowser:
                            stateImplementation = new ChangeDefaultBrowserImplementation();
                            break;
                        case StateModuleType.KeyboardLayout:
                            stateImplementation = new KeyboardLayoutStateImplementation();
                            break;
                        case StateModuleType.Theme:
                            stateImplementation = new ThemeStateImplementation();
                            break;
                        case StateModuleType.Mosh:
                            stateImplementation = new ModernShellStateImplementation();
                            break;
                        default:
                            //For any non-functional states we are still working on - 
                            // this allows infra to run without blowing up, without having to change all deployments
                            stateImplementation = new EmptyStateImplementation();                         
                            break;
                    }
                }
                return stateImplementation;
            }
        }

        internal DirectoryInfo TestBinariesDirectory { get; set; }

    }
}