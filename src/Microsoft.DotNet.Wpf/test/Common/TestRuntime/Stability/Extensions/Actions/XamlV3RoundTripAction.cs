// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Reflection;
using System.Resources;
using Microsoft.Test.Globalization;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class XamlV3RoundTripAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public DependencyObject Target { get; set; }

        public override void Perform()
        {
            string xaml;

            if(Target is Window)
            {
               FrameworkElement content = ((Window)Target).Content as FrameworkElement;
               if(content == null){ return; }

               xaml = XamlWriter.Save(content);
            }
            else
            {
               xaml = XamlWriter.Save(Target);
            }

            Trace.WriteLine("Xaml serialized:");
            Trace.WriteLine(xaml);

            //In-memory Comparison
            // Convert the string into a unicode byte stream
            //  (2 bytes per unicode char) + 2 bytes for endian marker
            UnicodeEncoding encoder = new UnicodeEncoding();
            byte[] bytes = new byte[(xaml.Length * 2) + 2];

            // Put the endian marker at the beginning of the array
            //  so that it is recognized as Unicode by the parser
            encoder.GetPreamble().CopyTo(bytes, 0);
            encoder.GetBytes(xaml).CopyTo(bytes, 2);
            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    XamlReader.Load(stream);
                }
            }
            catch (XamlParseException xpe)
            {
                bool isIgnorableMessage;

#if TESTBUILD_CLR20
                isIgnorableMessage = Exceptions.CompareMessage(xpe.Message, "ParserNoElementCreate2", WpfBinaries.PresentationFramework);
#endif
#if TESTBUILD_CLR40
                isIgnorableMessage = Exceptions.CompareMessage(xpe.Message, "NoConstructor", WpfBinaries.SystemXaml);
#endif

                // Ignoring XamlParseException with specific message, as Xaml cannot load types with no default constructor
                if (!isIgnorableMessage)
                {
                    // Context is lost for the current exception because of the catch
                    // Calling the method again so that debugger breaks at the throwing location
                    using (MemoryStream stream = new MemoryStream(bytes))
                    {
                        XamlReader.Load(stream);
                    }
                }

            }
        }
    }
}
