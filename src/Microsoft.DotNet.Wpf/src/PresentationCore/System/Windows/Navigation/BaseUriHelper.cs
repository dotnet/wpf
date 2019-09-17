// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//

using System;
using System.Diagnostics;
using System.IO.Packaging;
using System.Globalization;
using System.Net;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Reflection;
using System.IO;

using MS.Internal;
using MS.Internal.AppModel;
using MS.Internal.IO.Packaging;
using MS.Internal.PresentationCore;

using PackUriHelper = System.IO.Packaging.PackUriHelper;
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling your C# source code with the actual C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows.Navigation
{
    /// <summary>
    /// BaseUriHelper class provides BaseUri related property, methods.
    /// </summary>
    public static class BaseUriHelper
    {
        private const string SOOBASE = "SiteOfOrigin://";
        private static readonly Uri _siteOfOriginBaseUri = PackUriHelper.Create(new Uri(SOOBASE));
        private const string APPBASE = "application://";
        private static readonly Uri _packAppBaseUri = PackUriHelper.Create(new Uri(APPBASE));

        private static SecurityCriticalDataForSet<Uri> _baseUri;

        // Cached result of calling
        // PackUriHelper.GetPackageUri(BaseUriHelper.PackAppBaseUri).GetComponents(
        // UriComponents.AbsoluteUri,
        // UriFormat.UriEscaped);
        private const string _packageApplicationBaseUriEscaped = "application:///";
        private const string _packageSiteOfOriginBaseUriEscaped = "siteoforigin:///";

        static BaseUriHelper()
        {
            _baseUri = new SecurityCriticalDataForSet<Uri>(_packAppBaseUri);
            // Add an instance of the ResourceContainer to PreloadedPackages so that PackWebRequestFactory can find it
            // and mark it as thread-safe so PackWebResponse won't protect returned streams with a synchronizing wrapper
            PreloadedPackages.AddPackage(PackUriHelper.GetPackageUri(SiteOfOriginBaseUri), new SiteOfOriginContainer(), true);
        }

        #region public property and method

        /// <summary>
        ///     The DependencyProperty for BaseUri of current Element.
        ///
        ///     Flags: None
        ///     Default Value: null.
        /// </summary>
        public static readonly DependencyProperty BaseUriProperty =
                    DependencyProperty.RegisterAttached(
                                "BaseUri",
                                typeof(Uri),
                                typeof(BaseUriHelper),
                                new PropertyMetadata((object)null));


        /// <summary>
        /// Get BaseUri for a dependency object inside a tree.
        ///
        /// </summary>
        /// <param name="element">Dependency Object</param>
        /// <returns>BaseUri for the element</returns>
        /// <remarks>
        ///     Callers must have FileIOPermission(FileIOPermissionAccess.PathDiscovery) for the given Uri to call this API.
        /// </remarks>
        public static Uri GetBaseUri(DependencyObject element)
        {
            Uri baseUri = GetBaseUriCore(element);

            //
            // Manipulate BaseUri after tree searching is done.
            //

            if (baseUri == null)
            {
                // If no BaseUri information is found from the current tree,
                // just take the Application's BaseUri.
                // Application's BaseUri must be an absolute Uri.

                baseUri = BaseUriHelper.BaseUri;
            }
            else
            {
                if (baseUri.IsAbsoluteUri == false)
                {
                    // Most likely the BaseUriDP in element or IUriContext.BaseUri
                    // is set to a relative Uri programmatically in user's code.
                    // For this case, we should resolve this relative Uri to PackAppBase
                    // to generate an absolute Uri.

                    //
                    // BamlRecordReader now always sets absolute Uri for UriContext
                    // element when the tree is generated from baml/xaml stream, this
                    // code path would not run for parser-loaded tree.
                    //

                    baseUri = new Uri(BaseUriHelper.BaseUri, baseUri);
                }
            }

            return baseUri;
        }


       #endregion public property and method

        #region internal properties and methods

        static internal Uri SiteOfOriginBaseUri
        {
            [FriendAccessAllowed]
            get
            {
                return _siteOfOriginBaseUri;
            }
        }

        static internal Uri PackAppBaseUri
        {
            [FriendAccessAllowed]
            get
            {
                return _packAppBaseUri;
            }
        }

        /// <summary>
        /// Checks whether the input uri is in the "pack://application:,,," form
        /// </summary>
        internal static bool IsPackApplicationUri(Uri uri)
        {        
            return 
                // Is the "outer" URI absolute?
                uri.IsAbsoluteUri && 

                // Does the "outer" URI have the pack: scheme?
                SecurityHelper.AreStringTypesEqual(uri.Scheme, System.IO.Packaging.PackUriHelper.UriSchemePack) &&

                // Does the "inner" URI have the application: scheme
                SecurityHelper.AreStringTypesEqual(
                    PackUriHelper.GetPackageUri(uri).GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped),
                    _packageApplicationBaseUriEscaped);
        }

        // The method accepts a relative or absolute Uri and returns the appropriate assembly.
        //
        // For absolute Uri, it accepts only "pack://application:,,,/...", throw exception for
        // any other absolute Uri.
        //
        // If the first segment of that Uri contains ";component", returns the assembly whose
        // assembly name matches the text string in the first segment. otherwise, this method
        // would return EntryAssembly in the AppDomain.
        //
        [FriendAccessAllowed]
        internal static void GetAssemblyAndPartNameFromPackAppUri(Uri uri, out Assembly assembly, out string partName)
        {
            // The input Uri is assumed to be a valid absolute pack application Uri.
            // The caller should guarantee that.
            // Perform a sanity check to make sure the assumption stays.
            Debug.Assert(uri != null && uri.IsAbsoluteUri && SecurityHelper.AreStringTypesEqual(uri.Scheme, System.IO.Packaging.PackUriHelper.UriSchemePack) && IsPackApplicationUri(uri));

            // Generate a relative Uri which gets rid of the pack://application:,,, authority part.
            Uri partUri = new Uri(uri.AbsolutePath, UriKind.Relative);

            string assemblyName;
            string assemblyVersion;
            string assemblyKey;

            GetAssemblyNameAndPart(partUri, out partName, out assemblyName, out assemblyVersion, out assemblyKey);

            if (String.IsNullOrEmpty(assemblyName))
            {
                // The uri doesn't contain ";component". it should map to the enty application assembly.
                assembly = ResourceAssembly;

                // The partName returned from GetAssemblyNameAndPart should be escaped.
                Debug.Assert(String.Compare(partName, uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped), StringComparison.OrdinalIgnoreCase) == 0);
            }
            else
            {
                assembly = GetLoadedAssembly(assemblyName, assemblyVersion, assemblyKey);
            }
        }


        //
        //
        [FriendAccessAllowed]
        internal static Assembly GetLoadedAssembly(string assemblyName, string assemblyVersion, string assemblyKey)
        {
            Assembly assembly;
            AssemblyName asmName = new AssemblyName(assemblyName);

            // We always use the primary assembly (culture neutral) for resource manager.
            // if the required resource lives in satellite assembly, ResourceManager can find
            // the right satellite assembly later.
            asmName.CultureInfo = new CultureInfo(String.Empty);

            if (!String.IsNullOrEmpty(assemblyVersion))
            {
                asmName.Version = new Version(assemblyVersion);
            }

            byte[] keyToken = ParseAssemblyKey(assemblyKey);

            if (keyToken != null)
            {
                asmName.SetPublicKeyToken(keyToken);
            }

            assembly = SafeSecurityHelper.GetLoadedAssembly(asmName);

            if (assembly == null)
            {
                // The assembly is not yet loaded to the AppDomain, try to load it with information specified in resource Uri.
                assembly = Assembly.Load(asmName);
            }

            return assembly;
        }

        //
        // Return assembly Name, Version, Key and package Part from a relative Uri.
        //
        [FriendAccessAllowed]
        internal static void GetAssemblyNameAndPart(Uri uri, out string partName, out string assemblyName, out string assemblyVersion, out string assemblyKey)
        {
            Invariant.Assert(uri != null && uri.IsAbsoluteUri == false, "This method accepts relative uri only.");

            string original = uri.ToString(); // only relative Uri here (enforced by Package)

            // Start and end points for the first segment in the Uri.
            int start = 0;
            int end;

            if (original[0] == '/')
            {
                start = 1;
            }

            partName = original.Substring(start);

            assemblyName = string.Empty;
            assemblyVersion = string.Empty;
            assemblyKey = string.Empty;

            end = original.IndexOf('/', start);

            string firstSegment = String.Empty;
            bool fHasComponent = false;

            if (end > 0)
            {
                // get the first section
                firstSegment = original.Substring(start, end - start);

                // The resource comes from dll
                if (firstSegment.EndsWith(COMPONENT, StringComparison.OrdinalIgnoreCase))
                {
                    partName = original.Substring(end + 1);
                    fHasComponent = true;
                }
            }

            if (fHasComponent)
            {
                string[] assemblyInfo = firstSegment.Split(new char[] { COMPONENT_DELIMITER });

                int count = assemblyInfo.Length;

                if ((count > 4) || (count < 2))
                {
                    throw new UriFormatException(SR.Get(SRID.WrongFirstSegment));
                }

                //
                // if the uri contains escaping character,
                // Convert it back to normal unicode string
                // so that the string as assembly name can be
                // recognized by Assembly.Load later.
                //
                assemblyName = Uri.UnescapeDataString(assemblyInfo[0]);

                for (int i = 1; i < count - 1; i++)
                {
                    if (assemblyInfo[i].StartsWith(VERSION, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(assemblyVersion))
                        {
                            assemblyVersion = assemblyInfo[i].Substring(1);  // Get rid of the leading "v"
                        }
                        else
                        {
                            throw new UriFormatException(SR.Get(SRID.WrongFirstSegment));
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(assemblyKey))
                        {
                            assemblyKey = assemblyInfo[i];
                        }
                        else
                        {
                            throw new UriFormatException(SR.Get(SRID.WrongFirstSegment));
                        }
                    }
                } // end of for loop

            } // end of if fHasComponent
        }

        [FriendAccessAllowed]
        static internal bool IsComponentEntryAssembly(string component)
        {
            if (component.EndsWith(COMPONENT, StringComparison.OrdinalIgnoreCase))
            {
                string[] assemblyInfo = component.Split(new Char[] { COMPONENT_DELIMITER });
                // Check whether the assembly name is the same as the EntryAssembly.
                int count = assemblyInfo.Length;
                if ((count >= 2) && (count <= 4))
                {
                    string assemblyName = Uri.UnescapeDataString(assemblyInfo[0]);

                    Assembly assembly = ResourceAssembly;

                    if (assembly != null)
                    {
                        return (String.Compare(SafeSecurityHelper.GetAssemblyPartialName(assembly), assemblyName, StringComparison.OrdinalIgnoreCase) == 0);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }
                
        [FriendAccessAllowed]
        static internal Uri GetResolvedUri(Uri baseUri, Uri orgUri)
        {
            return new Uri(baseUri, orgUri);
        }

        [FriendAccessAllowed]
        static internal Uri MakeRelativeToSiteOfOriginIfPossible(Uri sUri)
        {
            if (Uri.Compare(sUri, SiteOfOriginBaseUri, UriComponents.Scheme, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0)
            {                
                Uri packageUri = PackUriHelper.GetPackageUri(sUri);
                if (String.Compare(packageUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped), _packageSiteOfOriginBaseUriEscaped, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return (new Uri(sUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped))).MakeRelativeUri(sUri);
                }
            }

            return sUri;
        }

        [FriendAccessAllowed]
        static internal Uri ConvertPackUriToAbsoluteExternallyVisibleUri(Uri packUri)
        {
            Invariant.Assert(packUri.IsAbsoluteUri && SecurityHelper.AreStringTypesEqual(packUri.Scheme, PackAppBaseUri.Scheme));

            Uri relative = MakeRelativeToSiteOfOriginIfPossible(packUri);

            if (! relative.IsAbsoluteUri)
            {
                return new Uri(SiteOfOriginContainer.SiteOfOrigin, relative);
            }
            else
            {
               throw new InvalidOperationException(SR.Get(SRID.CannotNavigateToApplicationResourcesInWebBrowser, packUri));
            }
        }

        // If a Uri is constructed with a legacy path such as c:\foo\bar then the Uri
        // object will not correctly resolve relative Uris in some cases.  This method
        // detects and fixes this by constructing a new Uri with an original string
        // that contains the scheme file://.
        [FriendAccessAllowed]
        static internal Uri FixFileUri(Uri uri)
        {
            if (uri != null && uri.IsAbsoluteUri && SecurityHelper.AreStringTypesEqual(uri.Scheme, Uri.UriSchemeFile) &&
                string.Compare(uri.OriginalString, 0, Uri.UriSchemeFile, 0, Uri.UriSchemeFile.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return new Uri(uri.AbsoluteUri);
            }

            return uri;
        }

        static internal Uri BaseUri
        {
            [FriendAccessAllowed]
            get
            {
                return _baseUri.Value;
            }
            [FriendAccessAllowed]
            set
            {
                // This setter should only be called from Framework through
                // BindUriHelper.set_BaseUri.
                _baseUri.Value = value;
            }
        }

        static internal Assembly ResourceAssembly
        {
            get
            {
                if (_resourceAssembly == null)
                {
                    _resourceAssembly = Assembly.GetEntryAssembly();
                }
                return _resourceAssembly;
            }
            [FriendAccessAllowed]
            set
            {
                // This should only be called from Framework through Application.ResourceAssembly setter.
                _resourceAssembly = value;
            }
        }
        
        // If the Uri provided is a pack Uri calling out an assembly and the assembly name matches that from assemblyInfo
        // this method will append the version taken from assemblyInfo, provided the Uri does not already have a version,
        // if the Uri provided the public Key token we must also verify that it matches the one in assemblyInfo.
        // We only add the version if the Uri is missing both the version and the key, otherwise returns null.
        // If the Uri is not a pack Uri, or we can't extract the information we need this method returns null.
        static internal Uri AppendAssemblyVersion(Uri uri, Assembly assemblyInfo)
        {
            Uri source = null;
            Uri baseUri = null;

            // assemblyInfo.GetName does not work in PartialTrust, so do this instead.
            AssemblyName currAssemblyName = new AssemblyName(assemblyInfo.FullName);
            string version = currAssemblyName.Version?.ToString();
            
            if (uri != null && !string.IsNullOrEmpty(version))
            {
                if (uri.IsAbsoluteUri)
                {
                    if (IsPackApplicationUri(uri))
                    {
                        // Extract the relative Uri and keep the base Uri so we can
                        // put them back together after we append the version.
                        source = new Uri(uri.AbsolutePath, UriKind.Relative);
                        baseUri = new Uri(uri.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
                    }
                }
                else
                {
                    source = uri;
                }


                if (source != null)
                {
                    string appendedUri;
                    string assemblyName;
                    string assemblyVersion;
                    string assemblyKey;
                    string partName;

                    BaseUriHelper.GetAssemblyNameAndPart(source, out partName, out assemblyName, out assemblyVersion, out assemblyKey);

                    bool assemblyKeyProvided = !string.IsNullOrEmpty(assemblyKey);

                    // Make sure we:
                    //      1) Successfully extracted the assemblyName from the Uri.
                    //      2) No assembly version was already provided in the Uri.
                    //      3) The assembly short name matches the name in assemblyInfo.
                    //      4) If a public key token was provided in the Uri, verify
                    //          that it matches the token in asssemblyInfo.
                    if (!string.IsNullOrEmpty(assemblyName) && string.IsNullOrEmpty(assemblyVersion) &&
                        assemblyName.Equals(currAssemblyName.Name, StringComparison.Ordinal) &&
                        (!assemblyKeyProvided || AssemblyMatchesKeyString(currAssemblyName, assemblyKey)))
                    {
                        StringBuilder uriStringBuilder = new StringBuilder();

                        uriStringBuilder.Append('/');
                        uriStringBuilder.Append(assemblyName);
                        uriStringBuilder.Append(COMPONENT_DELIMITER);
                        uriStringBuilder.Append(VERSION);
                        uriStringBuilder.Append(version);
                        if (assemblyKeyProvided)
                        {
                            uriStringBuilder.Append(COMPONENT_DELIMITER);
                            uriStringBuilder.Append(assemblyKey);
                        }
                        uriStringBuilder.Append(COMPONENT);
                        uriStringBuilder.Append('/');
                        uriStringBuilder.Append(partName);

                        appendedUri = uriStringBuilder.ToString();

                        if (baseUri != null)
                        {
                            return new Uri(baseUri, appendedUri);
                        }

                        return new Uri(appendedUri, UriKind.Relative);
                    }
                }

            }
            
            return null;
        }

        #endregion internal properties and methods

        #region private methods

        /// <summary>
        /// Get BaseUri for a dependency object inside a tree.
        ///
        /// </summary>
        /// <param name="element">Dependency Object</param>
        /// <returns>BaseUri for the element</returns>
        /// <remarks>
        ///     Callers must have FileIOPermission(FileIOPermissionAccess.PathDiscovery) for the given Uri to call this API.
        /// </remarks>
        internal static Uri GetBaseUriCore(DependencyObject element)
        {
            Uri baseUri = null;
            DependencyObject doCurrent;

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

                //
                // Search the tree to find the closest parent which implements
                // IUriContext or have set value for BaseUri property.
                //
                doCurrent = element;

                while (doCurrent != null)
                {
                    // Try to get BaseUri property value from current node.
                    baseUri = doCurrent.GetValue(BaseUriProperty) as Uri;

                    if (baseUri != null)
                    {
                        // Got the right node which is the closest to original element.
                        // Stop searching here.
                        break;
                    }

                    IUriContext uriContext = doCurrent as IUriContext;

                    if (uriContext != null)
                    {
                        // If the element implements IUriContext, and if the BaseUri
                        // is not null, just take the BaseUri from there.
                        // and stop the search loop.
                        baseUri = uriContext.BaseUri;

                        if (baseUri != null)
                            break;
                    }

                    //
                    // The current node doesn't contain BaseUri value,
                    // try its parent node in the tree.

                    UIElement uie = doCurrent as UIElement;

                    if (uie != null)
                    {
                        // Do the tree walk up
                        doCurrent = uie.GetUIParent(true);
                    }
                    else
                    {
                        ContentElement ce = doCurrent as ContentElement;

                        if (ce != null)
                        {
                            doCurrent = ce.Parent;
                        }
                        else
                        {
                            Visual vis = doCurrent as Visual;

                            if (vis != null)
                            {
                                // Try the Visual tree search
                                doCurrent = VisualTreeHelper.GetParent(vis);
                            }
                            else
                            {
                                // Not a Visual.
                                // Stop here for the tree searching to aviod an infinite loop.
                                break;
                            }
                        }
                    }
                }

            return baseUri;
        }
        
        private static bool AssemblyMatchesKeyString(AssemblyName asmName, string assemblyKey)
        {
            byte[] parsedKeyToken = ParseAssemblyKey(assemblyKey);
            byte[] assemblyKeyToken = asmName.GetPublicKeyToken();

            return SafeSecurityHelper.IsSameKeyToken(assemblyKeyToken, parsedKeyToken);
        }

        private static byte[] ParseAssemblyKey(string assemblyKey)
        {
            if (!string.IsNullOrEmpty(assemblyKey))
            {
                int byteCount = assemblyKey.Length / 2;
                byte[] keyToken = new byte[byteCount];
                for (int i = 0; i < byteCount; i++)
                {
                    string byteString = assemblyKey.Substring(i * 2, 2);
                    keyToken[i] = byte.Parse(byteString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }

                return keyToken;
            }

            return null;
        }

        #endregion

        private const string COMPONENT = ";component";
        private const string VERSION = "v";
        private const char COMPONENT_DELIMITER = ';';
       
        private static Assembly _resourceAssembly;
    }
}


