// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
#if TESTBUILD_CLR40

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Reflection;
using System.Xaml;
using System.Xml;
using Microsoft.Test.Serialization;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    class XamlV4RoundTripAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public Object Target { get; set; }

        public override void Perform()
        {
            if (Target != null)
            {
                string xaml = null;

                try
                {
                    xaml = XamlServices.Save(Target);
                }
                catch (Exception e)
                {
                    Trace.WriteLine("[XamlV4RoundTripAction] Serialization failed with following exception\n. " + e.ToString());
                    Trace.WriteLine("[XamlV4RoundTripAction] Dumping object tree...");
                    try
                    {
                        Trace.WriteLine("[XamlV4RoundTripAction] " + ObjectSerializer.Serialize(Target));
                    }
                    catch(Exception)
                    {
                        Trace.WriteLine("[XamlV4RoundTripAction] ObjectSerializer was unable to serialize " + Target.GetType().ToString());
                    }

                    // Context is lost for the current exception because of the catch
                    // Calling the method again so that debugger breaks at the throwing location
                    XamlServices.Save(Target);
                }

                Trace.WriteLine("[XamlV4RoundTripAction] Serialized Xaml:");
                Trace.WriteLine("[XamlV4RoundTripAction] " + xaml);

                using (StringReader stringReader = new StringReader(xaml))
                {
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        XamlServices.Load(xmlReader);
                    }
                }
            }
        }
    }
}

#endif
