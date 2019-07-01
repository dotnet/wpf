// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
#if TESTBUILD_CLR40

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Reflection;
using System.Resources;
using System.Xaml;
using System.Xml;
using Microsoft.Test.Globalization;
using Microsoft.Test.Serialization;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    class XamlV4RoundTripForWpfAction : SimpleDiscoverableAction
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
                    xaml = SaveToXaml();
                }
                catch (Exception e)
                {
                    bool isIgnorableMessage = Exceptions.CompareMessage(e.Message, "DeferringLoaderNoSave", Microsoft.Test.Globalization.WpfBinaries.PresentationFramework);

                    // Ignoring NotSupportedException with specific message
                    if (!(e.GetType() == typeof(NotSupportedException) && isIgnorableMessage))
                    {
                        Trace.WriteLine("[XamlV4RoundTripForWpfAction] Serialization failed with following exception\n. " + e.ToString());
                        Trace.WriteLine("[XamlV4RoundTripForWpfAction] Dumping object tree...");
                        try
                        {
                            Trace.WriteLine("[XamlV4RoundTripForWpfAction] " + ObjectSerializer.Serialize(Target));
                        }
                        catch(Exception)
                        {
                            Trace.WriteLine("[XamlV4RoundTripForWpfAction] ObjectSerializer was unable to serialize " + Target.GetType().ToString());
                        }

                        // Context is lost for the current exception because of the catch
                        // Calling the method again so that debugger breaks at the throwing location
                        SaveToXaml();
                    }

                    return;
                }

                Trace.WriteLine("[XamlV4RoundTripForWpfAction] Serialized Xaml:");
                Trace.WriteLine("[XamlV4RoundTripForWpfAction] " + xaml);

                using (StringReader stringReader = new StringReader(xaml))
                {
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        System.Windows.Markup.XamlReader.Load(xmlReader);
                    }
                }
            }
        }

        private string SaveToXaml()
        {
            XamlSchemaContext schemaContext = System.Windows.Markup.XamlReader.GetWpfSchemaContext();
            StringBuilder stringBuilder = new StringBuilder();

            using (XamlObjectReader xamlObjectReader = new XamlObjectReader(Target, schemaContext, new XamlObjectReaderSettings { RequireExplicitContentVisibility=true }))
            {
                using (XamlXmlWriter xamlXmlWriter = new XamlXmlWriter(XmlWriter.Create(stringBuilder, new XmlWriterSettings{Indent=true, OmitXmlDeclaration=true}), xamlObjectReader.SchemaContext))
                {
                    XamlServices.Transform(xamlObjectReader, xamlXmlWriter);
                    return stringBuilder.ToString();
                }
            }
         }
    }
}

#endif
