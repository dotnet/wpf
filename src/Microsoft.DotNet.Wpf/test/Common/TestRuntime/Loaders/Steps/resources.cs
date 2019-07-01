// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// UnmanagedResourceHelper
    /// </summary>
    internal class UnmanagedResourceHelper
    {
        /// <summary>
        /// ResourceType
        /// </summary>
        internal enum ResourceType
        {
            Cursor = 1,
            Bitmap = 2,
            Icon = 3,
            Menu = 4,
            Dialog = 5,
            String = 6,
            FontDir = 7,
            Font = 8,
            Accelerator = 9,
            Data = 10,
            MessageTable = 11,
            HTML = 23
        }

        /// <summary>
        /// UnmanagedResourceHelper
        /// </summary>
        /// <param name="containerPath"></param>
        internal UnmanagedResourceHelper(string containerPath)
        {
            libraryPath = containerPath;
            // initialize the library
            library = Win32Helper.LoadLibraryEx(containerPath, IntPtr.Zero, Win32Helper.Constants.LOAD_LIBRARY_AS_DATAFILE);
        }

        /// <summary>
        /// GetResource
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal string GetResource(string name, ResourceType type)
        {
            // handle special requests
            if (type == ResourceType.String)
            {
                return (GetString(Int32.Parse(name)));
            }

            // find the resource
            int resourceType = (int)type;
            IntPtr hresinfo = Win32Helper.FindResource(library, name, resourceType);
            if (hresinfo == IntPtr.Zero)
            {
                throw (new ArgumentException("name/resource " + name + " Type " + type + " was not found in library " + libraryPath));
            }

            // get resource size
            int size = Win32Helper.SizeofResource(library, hresinfo);

            // load it
            IntPtr h = Win32Helper.LoadResource(library, hresinfo);

            // get a pointer to its content in memory
            IntPtr pstr = Win32Helper.LockResource(h);

            // create a string from that pointer - Assumes resources stored as Unicode
            String s = null;
            unsafe
            {
                // string is Unicode
                s = new string((char*)pstr, 0, size / sizeof(char));
            }
            return (s);
        }

        /// <summary>
        /// GetString
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal string GetString(int id)
        {
            // strings
            StringBuilder result = new StringBuilder(1000);
            Win32Helper.LoadString(library, id, result, 1000);        
            //HACK: Need to remove hotkey labels from certain resource strings.    
            return (result.ToString().Replace("(&S)", "").Replace("(&O)", "").Replace("&", ""));
        }

        /// <summary>
        /// Handle to hold the library from which resources will be extracted
        /// </summary>
        private IntPtr library = IntPtr.Zero;

        private string libraryPath = "";
    }

    /// <summary>
    /// ManagedResourcesHelper
    /// </summary>
    internal class ManagedResourceHelper
    {
        ResourceManager _rm = null;

        /// <summary>
        /// ManagedResourceHelper
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceName"></param>
        internal ManagedResourceHelper(string assembly, string resourceName)
        {
            Assembly a = null;
            // try loading the assembly from a full path
            try
            {
                a = Assembly.LoadFrom(assembly);
            }
            catch (FileNotFoundException)
            {
            }

            // if assembly was not loaded, it may be in the GAC
            try
            {
                a = Assembly.Load(assembly);
            }
            catch (FileLoadException)
            {
            }

            // if no assembly was found, throw
            if (a == null)
            {
                throw (new ArgumentException("Could not load assembly '" + assembly + "'"));
            }

            // get the resource manager from the assembly
            _rm = new ResourceManager(resourceName, a);
        }

        /// <summary>
        /// GetString
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal string GetString(string name)
        {
            return (_rm.GetString(name));
        }
    }
}