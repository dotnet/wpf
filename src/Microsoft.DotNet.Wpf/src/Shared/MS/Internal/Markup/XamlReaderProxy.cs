// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: This helper class reflects into internal overloads of XamlReader.Load to use the RestrictiveXamlXmlReader
// to avoid loading unsafe loose xaml.
//

using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Windows.Markup;

#if REACHFRAMEWORK
namespace MS.Internal.ReachFramework
#elif PRESENTATIONFRAMEWORK
namespace MS.Internal.PresentationFramework
#else
namespace MS.Internal
#endif
{
    namespace Markup
    {
        /// <summary>
        /// Provides a helper class to create delegates to reflect into XamlReader.Load
        /// </summary>
        internal class XamlReaderProxy
        {
            /// <summary>
            /// The static constructor creates and stores delegates for overloads of <see cref="XamlReader.Load"/> that need to be reflected into
            /// so that we can safeguard entry-points for external xaml loading.
            /// </summary>
            /// <remark> Doing this in the static constructor guarantees thread safety.</remark>

            static XamlReaderProxy()
            {
                MethodInfo method = _xamlReaderType.GetMethod(XamlLoadMethodName, BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(Stream), typeof(ParserContext), typeof(bool) }, null);

                if (method == null)
                {
                    throw new MissingMethodException(XamlLoadMethodName);
                }

                _xamlLoad3 = (XamlLoadDelegate3)method.CreateDelegate(typeof(XamlLoadDelegate3));

                method = _xamlReaderType.GetMethod(XamlLoadMethodName, BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(XmlReader), typeof(bool) }, null);

                if (method == null)
                {
                    throw new MissingMethodException(XamlLoadMethodName);
                }

                _xamlLoad2 = (XamlLoadDelegate2)method.CreateDelegate(typeof(XamlLoadDelegate2));
            }
    
            public static object Load(Stream stream, ParserContext parserContext, bool useRestrictiveXamlReader)
            {
                return _xamlLoad3.Invoke(stream, parserContext, useRestrictiveXamlReader);
            }

            public static object Load(XmlReader reader, bool useRestrictiveXamlReader)
            {
                return _xamlLoad2.Invoke(reader, useRestrictiveXamlReader);
            }

            private delegate object XamlLoadDelegate3(Stream stream, ParserContext parserContext, bool useRestrictiveXamlReader);
            private static XamlLoadDelegate3 _xamlLoad3;

            private delegate object XamlLoadDelegate2(XmlReader reader, bool useRestrictiveXamlReader);
            private static XamlLoadDelegate2 _xamlLoad2;

            private static readonly Type _xamlReaderType = typeof(XamlReader);
            private const string XamlLoadMethodName = nameof(Load);

        }
    }
}
