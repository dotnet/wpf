// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Interop;
using System.Threading;
using System.Windows.Markup;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Test.Win32;
using System.Runtime.Serialization;
using System.Reflection;


namespace Microsoft.Test.Markup
{

    /// <summary>
    /// </summary>
    static public class WPFTreeGenerator
    {
        private static void Initialize()
        {  
            if (_initialized)
            {
                return;
            }
            
            lock(_globalLock)
            {
                if (!_initialized)
                {
                    
                    _baseUri = new Uri(WPFXamlGenerator.XamlDefinition.SupportFilesPath + Path.DirectorySeparatorChar);

                    _initialized = true;
                }
            }
        }
            
        /// <summary>
        /// </summary>
        public static object GenerateFreezable()
        {
            Initialize();
            WPFXamlGenerator.ValidateDefaultFiles();
            
            object obj = null;

            while (obj == null)
            {
                obj = Parse(WPFXamlGenerator.GenerateFreezable());

                if (obj is BitmapEffect && !WPFXamlGenerator.ShouldTestBitmapEffects)
                {
                    obj = null;
                    continue;
                }

                if (obj is Transform && !WPFXamlGenerator.ShouldTestTransforms)
                {
                    obj = null;
                    continue;
                }
            }

            return obj;
        }

        /// <summary>
        /// </summary>
        public static object GenerateWindow()
        {
            Initialize();
                
            WPFXamlGenerator.ValidateDefaultFiles();            
            return Parse(WPFXamlGenerator.GenerateWindow());
        }

        /// <summary>
        /// </summary>
        public static ResourceDictionary GenerateResourceDictionary()
        {
            Initialize();
            
            WPFXamlGenerator.ValidateDefaultFiles();
            return (ResourceDictionary)Parse(WPFXamlGenerator.GenerateResourceDictionary());
        }
        

        /// <summary>
        /// </summary>
        public static object Generate()
        {
            Initialize();
            
            WPFXamlGenerator.ValidateDefaultFiles();            
            return Parse(WPFXamlGenerator.Generate());
        }

        static object Parse(string xaml)
        {
             using (MemoryStream mStream = new MemoryStream())
             {
                using (StreamWriter writer = new StreamWriter(mStream, Encoding.Unicode))
                {
                    writer.Write(xaml);
                    writer.Flush();
                    mStream.Seek(0, SeekOrigin.Begin);

                    ParserContext pc = new ParserContext();
                    pc.BaseUri = _baseUri;
                    object obj = System.Windows.Markup.XamlReader.Load(mStream, pc);
                    return obj;
                }
             }
        }

        static bool _initialized = false;
        static object _globalLock = new object();
        static Uri _baseUri;
    }

}

