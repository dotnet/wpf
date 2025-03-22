// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Provides a method to turn a baml stream into an object.
//

using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using MS.Internal.Resources;
using System.IO.Packaging;
using System.ComponentModel;
using System.Windows.Controls;

namespace MS.Internal.AppModel
{
    // !!!! Note: Those methods are registered as MimeObjectFactory.StreamToObjectFactoryDelegate. The caller expects the 
    // delgate to close stream. 
    internal static class AppModelKnownContentFactory
    {
        // <summary>
        // Creates an object instance from a Baml stream and it's Uri
        // </summary>
        internal static object BamlConverter(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter)
        {
            return BamlConverterCore(stream, baseUri, canUseTopLevelBrowser, sandboxExternalContent, allowAsync, isJournalNavigation, out asyncObjectConverter, false);
        }

        internal static object BamlConverterCore(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter, bool isUnsafe)
        {
            asyncObjectConverter = null;
            if (isUnsafe)
            {
                throw new InvalidOperationException(SR.Format(SR.BamlIsNotSupportedOutsideOfApplicationResources));
            }
            // If this stream comes from outside the application throw
            //
            if (!BaseUriHelper.IsPackApplicationUri(baseUri))
            {
                throw new InvalidOperationException(SR.BamlIsNotSupportedOutsideOfApplicationResources);
            }

            // If this stream comes from a content file also throw
            Uri partUri = PackUriHelper.GetPartUri(baseUri);
            string partName, assemblyName, assemblyVersion, assemblyKey;
            BaseUriHelper.GetAssemblyNameAndPart(partUri, out partName, out assemblyName, out assemblyVersion, out assemblyKey);
            if (ContentFileHelper.IsContentFile(partName))
            {
                throw new InvalidOperationException(SR.BamlIsNotSupportedOutsideOfApplicationResources);
            }

            ParserContext pc = new ParserContext
            {
                BaseUri = baseUri,
                SkipJournaledProperties = isJournalNavigation
            };

            return Application.LoadBamlStreamWithSyncInfo(stream, pc);
        }

        // <summary>
        // Creates an object instance from a Xaml stream and it's Uri
        // </summary>
        internal static object XamlConverter(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter)
        {
            return XamlConverterCore(stream, baseUri, canUseTopLevelBrowser, sandboxExternalContent, allowAsync, isJournalNavigation, out asyncObjectConverter, false);
        }

        internal static object XamlConverterCore(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter, bool isUnsafe)
        {
            asyncObjectConverter = null;

            if (sandboxExternalContent)
            {
                if (string.Equals(baseUri.Scheme, BaseUriHelper.PackAppBaseUri.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    baseUri = BaseUriHelper.ConvertPackUriToAbsoluteExternallyVisibleUri(baseUri);
                }

                stream.Close();

                WebBrowser webBrowser = new WebBrowser
                {
                    Source = baseUri
                };
                return webBrowser;
            }
            else
            {
                ParserContext pc = new ParserContext
                {
                    BaseUri = baseUri,
                    SkipJournaledProperties = isJournalNavigation
                };

                if (allowAsync)
                {
                    XamlReader xr = new XamlReader();
                    asyncObjectConverter = xr;
                    xr.LoadCompleted += new AsyncCompletedEventHandler(OnParserComplete);
                    if(isUnsafe)
                    {
                        pc.FromRestrictiveReader = true;
                    }
                    // XamlReader.Load will close the stream.
                    return xr.LoadAsync(stream, pc);
                }
                else
                {
                    // XamlReader.Load will close the stream.
                    return XamlReader.Load(stream, pc, isUnsafe);
                }
            }
        }

        private static void OnParserComplete(object sender, AsyncCompletedEventArgs args)
        {
            // We can get this event from cancellation. We do not care about the error if there is any
            // that happened as a result of cancellation.
            if ((!args.Cancelled) && (args.Error != null))
            {
                throw args.Error;
            }
        }

        internal static object HtmlXappConverter(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter)
        {
            return HtmlXappConverterCore(stream, baseUri, canUseTopLevelBrowser, sandboxExternalContent, allowAsync, isJournalNavigation, out asyncObjectConverter, false);
        }

        internal static object HtmlXappConverterCore(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter, bool isUnsafe)
        {
            asyncObjectConverter = null;
            if (isUnsafe)
            {
                throw new InvalidOperationException(SR.Format(SR.BamlIsNotSupportedOutsideOfApplicationResources));
            }
            if (canUseTopLevelBrowser)
            {
                return null;
            }

            if (string.Equals(baseUri.Scheme, BaseUriHelper.PackAppBaseUri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                baseUri = BaseUriHelper.ConvertPackUriToAbsoluteExternallyVisibleUri(baseUri);
            }

            stream.Close();

            WebBrowser webBrowser = new WebBrowser
            {
                Source = baseUri
            };

            return webBrowser;
        }
    }
}
