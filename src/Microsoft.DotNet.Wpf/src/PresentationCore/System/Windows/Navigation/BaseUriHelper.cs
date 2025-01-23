// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PackUriHelper = System.IO.Packaging.PackUriHelper;

using System.Globalization;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;
using System.Reflection;

using MS.Internal;
using MS.Internal.AppModel;
using MS.Internal.IO.Packaging;
using MS.Internal.PresentationCore;

namespace System.Windows.Navigation
{
    /// <summary>
    /// BaseUriHelper class provides BaseUri related property, methods.
    /// </summary>
    public static class BaseUriHelper
    {
        private static readonly Uri s_siteOfOriginBaseUri = PackUriHelper.Create(new Uri("SiteOfOrigin://"));
        private static readonly Uri s_packAppBaseUri = PackUriHelper.Create(new Uri("application://"));

        private const string COMPONENT = ";component";
        private const string VERSION = "v";
        private const char COMPONENT_DELIMITER = ';';

        private static Assembly s_resourceAssembly;

        // Cached result of calling the following:
        // PackUriHelper.GetPackageUri(BaseUriHelper.PackAppBaseUri).GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
        private const string PackageApplicationBaseUriEscaped = "application:///";
        private const string PackageSiteOfOriginBaseUriEscaped = "siteoforigin:///";

        static BaseUriHelper()
        {
            // Add an instance of the ResourceContainer to PreloadedPackages so that PackWebRequestFactory can find it
            // and mark it as thread-safe so PackWebResponse won't protect returned streams with a synchronizing wrapper
            PreloadedPackages.AddPackage(PackUriHelper.GetPackageUri(SiteOfOriginBaseUri), new SiteOfOriginContainer(), true);
        }

        /// <summary>
        ///     The DependencyProperty for BaseUri of current Element.
        ///
        ///     Flags: None
        ///     Default Value: null.
        /// </summary>
        public static readonly DependencyProperty BaseUriProperty = DependencyProperty.RegisterAttached("BaseUri", typeof(Uri), typeof(BaseUriHelper), new PropertyMetadata(null));

        /// <summary>
        /// Get BaseUri for a dependency object inside a tree.
        /// </summary>
        /// <param name="element">Dependency Object</param>
        /// <returns>BaseUri for the <paramref name="element"/>.</returns>
        public static Uri GetBaseUri(DependencyObject element)
        {
            Uri baseUri = GetBaseUriCore(element);

            //
            // Manipulate BaseUri after tree searching is done.
            //

            if (baseUri is null)
            {
                // If no BaseUri information is found from the current tree,
                // just take the Application's BaseUri.
                // Application's BaseUri must be an absolute Uri.

                baseUri = PackAppBaseUri;
            }
            else
            {
                if (!baseUri.IsAbsoluteUri)
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

                    baseUri = new Uri(PackAppBaseUri, baseUri);
                }
            }

            return baseUri;
        }

        internal static Uri SiteOfOriginBaseUri
        {
            get => s_siteOfOriginBaseUri;
        }

        internal static Uri PackAppBaseUri
        {
            get => s_packAppBaseUri;
        }

        internal static Assembly ResourceAssembly
        {
            get
            {
                s_resourceAssembly ??= Assembly.GetEntryAssembly();

                return s_resourceAssembly;
            }
            // This should only be called from Framework through Application.ResourceAssembly setter.
            set => s_resourceAssembly = value;
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
                string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase) &&

                // Does the "inner" URI have the application: scheme
                string.Equals(PackUriHelper.GetPackageUri(uri).GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped),
                              PackageApplicationBaseUriEscaped,
                              StringComparison.OrdinalIgnoreCase);
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
        internal static void GetAssemblyAndPartNameFromPackAppUri(Uri uri, out Assembly assembly, out ReadOnlySpan<char> partName)
        {
            // The input Uri is assumed to be a valid absolute pack application Uri.
            // The caller should guarantee that.
            // Perform a sanity check to make sure the assumption stays.
            Debug.Assert(uri is not null && uri.IsAbsoluteUri && string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase) && IsPackApplicationUri(uri));

            // Generate a relative Uri which gets rid of the pack://application:,,, authority part.
            Uri partUri = new(uri.AbsolutePath, UriKind.Relative);

            GetAssemblyNameAndPart(partUri, out AssemblyPackageInfo assemblyInfo);
            partName = assemblyInfo.PackagePartName;

            if (assemblyInfo.AssemblyName.IsEmpty)
            {
                // The uri doesn't contain ";component". it should map to the entry application assembly.
                assembly = ResourceAssembly;

                // The partName returned from GetAssemblyNameAndPart should be escaped.
                Debug.Assert(assemblyInfo.PackagePartName.Equals(uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped), StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                assembly = GetLoadedAssembly(assemblyInfo.AssemblyName, assemblyInfo.AssemblyVersion, assemblyInfo.AssemblyToken);
            }
        }

        /// <summary>
        /// Retrieves <see cref="Assembly"/> using AssemblyName, AssemblyVersion and AssemblyToken from the loaded assemblies pool, or loads it on demand.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="assemblyVersion"></param>
        /// <param name="assemblyToken"></param>
        /// <returns></returns>
        internal static Assembly GetLoadedAssembly(ReadOnlySpan<char> assemblyName, ReadOnlySpan<char> assemblyVersion, ReadOnlySpan<char> assemblyToken)
        {
            // We always use the primary assembly (culture neutral) for resource manager.
            // If the required resource lives in satellite assembly, ResourceManager can find the right satellite assembly later.
            AssemblyName asmName = new(assemblyName.ToString()) { CultureInfo = CultureInfo.InvariantCulture };

            // Parse Version
            if (!assemblyVersion.IsEmpty)
                asmName.Version = Version.Parse(assemblyVersion);

            // Parse Key
            if (!assemblyToken.IsEmpty)
                asmName.SetPublicKeyToken(Convert.FromHexString(assemblyToken));

            // If assembly is not yet loaded to the AppDomain, try to load it with information specified in resource Uri,
            // otherwise let Assembly.Load throw an exception in case the method couldn't find the assembly
            return SafeSecurityHelper.GetLoadedAssembly(asmName) ?? Assembly.Load(asmName);
        }

        /// <summary>
        /// Returns AssemblyName, AssemblyVersion, PublicKeyToken and package Part from a relative <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">A relative <see cref="Uri"/> in /AssemblyShortName{;Version]{;PublicKey];component/PackagePartName format.</param>
        /// <param name="assemblyInfo">The sliced out information from the current <see cref="Uri"/>.</param>
        /// <exception cref="UriFormatException"></exception>
        /// <remarks>See more info on <seealso href="https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf"/>.</remarks>
        internal static void GetAssemblyNameAndPart(Uri uri, out AssemblyPackageInfo assemblyInfo)
        {
            Invariant.Assert(uri is not null && !uri.IsAbsoluteUri, "This method accepts relative uri only.");

            ReadOnlySpan<char> original = uri.ToString(); // only relative Uri here (enforced by Package)

            // Start and end points for the first segment in the Uri.
            int segmentStart = original[0] == '/' ? 1 : 0;
            int segmentEnd;

            assemblyInfo = new AssemblyPackageInfo { PackagePartName = original.Slice(segmentStart) };
            segmentEnd = assemblyInfo.PackagePartName.IndexOf('/');

            if (segmentEnd == -1)
                return;

            // Get the first uri section
            ReadOnlySpan<char> referencedAssemblySegment = original.Slice(segmentStart, segmentEnd);

            // If false, the resource does not come from the current assembly but likely from the entry assembly.
            if (!referencedAssemblySegment.EndsWith(COMPONENT, StringComparison.OrdinalIgnoreCase))
                return;

            assemblyInfo.PackagePartName = original.Slice(segmentStart + segmentEnd + 1);

            Span<Range> segments = stackalloc Range[5];
            int count = referencedAssemblySegment.Split(segments, COMPONENT_DELIMITER);

            // Valid values are 2; 3; 4 as we optionally can also include Version and PublicKeyToken
            if (count is < 2 or > 4)
                throw new UriFormatException(SR.WrongFirstSegment);

            assemblyInfo.AssemblyName = referencedAssemblySegment[segments[0]];

            // If the uri contains escaping character, convert it back to normal unicode string
            // so that the string as assembly name can be recognized by Assembly.Load(...) later.
            if (IsUriUnescapeRequired(assemblyInfo.AssemblyName))
                assemblyInfo.AssemblyName = Uri.UnescapeDataString(assemblyInfo.AssemblyName);

            for (int i = 1; i < count - 1; i++)
            {
                if (referencedAssemblySegment[segments[i]].StartsWith(VERSION, StringComparison.OrdinalIgnoreCase))
                {
                    if (assemblyInfo.AssemblyVersion.IsEmpty)
                        assemblyInfo.AssemblyVersion = referencedAssemblySegment[segments[i]].Slice(1);  // Get rid of the leading "v"
                    else
                        throw new UriFormatException(SR.WrongFirstSegment);
                }
                else
                {
                    if (assemblyInfo.AssemblyToken.IsEmpty)
                        assemblyInfo.AssemblyToken = referencedAssemblySegment[segments[i]];
                    else
                        throw new UriFormatException(SR.WrongFirstSegment);
                }
            }
        }

        internal static bool IsComponentEntryAssembly(ReadOnlySpan<char> component)
        {
            if (!component.EndsWith(COMPONENT, StringComparison.OrdinalIgnoreCase))
                return false;

            Span<Range> segments = stackalloc Range[5];
            int count = component.Split(segments, COMPONENT_DELIMITER) - 2;

            // Check whether the assembly name is the same as the EntryAssembly.
            if (count <= 2)
            {
                ReadOnlySpan<char> assemblyName = component[segments[0]];
                Assembly assembly = ResourceAssembly;

                if (IsUriUnescapeRequired(assemblyName))
                    assemblyName = Uri.UnescapeDataString(component[segments[0]]);

                if (assembly is not null)
                {
                    return ReflectionUtils.GetAssemblyPartialName(assembly).Equals(assemblyName, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        internal static Uri GetResolvedUri(Uri baseUri, Uri orgUri)
        {
            return new Uri(baseUri, orgUri);
        }

        internal static Uri MakeRelativeToSiteOfOriginIfPossible(Uri sUri)
        {
            if (Uri.Compare(sUri, SiteOfOriginBaseUri, UriComponents.Scheme, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                Uri packageUri = PackUriHelper.GetPackageUri(sUri);
                if (string.Equals(packageUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped), PackageSiteOfOriginBaseUriEscaped, StringComparison.OrdinalIgnoreCase))
                {
                    return new Uri(sUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)).MakeRelativeUri(sUri);
                }
            }

            return sUri;
        }

        internal static Uri ConvertPackUriToAbsoluteExternallyVisibleUri(Uri packUri)
        {
            Invariant.Assert(packUri.IsAbsoluteUri && string.Equals(packUri.Scheme, PackAppBaseUri.Scheme, StringComparison.OrdinalIgnoreCase));

            Uri relative = MakeRelativeToSiteOfOriginIfPossible(packUri);

            if (!relative.IsAbsoluteUri)
                return new Uri(SiteOfOriginContainer.SiteOfOrigin, relative);

            throw new InvalidOperationException(SR.Format(SR.CannotNavigateToApplicationResourcesInWebBrowser, packUri));
        }

        // If a Uri is constructed with a legacy path such as c:\foo\bar then the Uri
        // object will not correctly resolve relative Uris in some cases.  This method
        // detects and fixes this by constructing a new Uri with an original string
        // that contains the scheme file://.
        internal static Uri FixFileUri(Uri uri)
        {
            if (uri is not null && uri.IsAbsoluteUri &&
                string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase) &&
                !uri.OriginalString.StartsWith(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(uri.AbsoluteUri);
            }

            return uri;
        }

        // If the Uri provided is a pack Uri calling out an assembly and the assembly name matches that from assemblyInfo
        // this method will append the version taken from assemblyInfo, provided the Uri does not already have a version,
        // if the Uri provided the public Key token we must also verify that it matches the one in assemblyInfo.
        // We only add the version if the Uri is missing both the version and the key, otherwise returns null.
        // If the Uri is not a pack Uri, or we can't extract the information we need this method returns null.
        internal static Uri AppendAssemblyVersion(Uri uri, Assembly assembly)
        {
            Uri source = null;
            Uri baseUri = null;

            // assemblyInfo.GetName does not work in PartialTrust, so do this instead.
            AssemblyName currAssemblyName = new AssemblyName(assembly.FullName);
            string version = currAssemblyName.Version?.ToString();

            if (uri is not null && !string.IsNullOrEmpty(version))
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

                if (source is not null)
                {
                    GetAssemblyNameAndPart(source, out AssemblyPackageInfo assemblyInfo);

                    bool assemblyKeyProvided = !assemblyInfo.AssemblyToken.IsEmpty;

                    // Make sure we:
                    //      1) Successfully extracted the assemblyName from the Uri.
                    //      2) No assembly version was already provided in the Uri.
                    //      3) The assembly short name matches the name in assembly.
                    //      4) If a public key token was provided in the Uri, verify
                    //          that it matches the token in assemblyInfo.
                    if (!assemblyInfo.AssemblyName.IsEmpty && assemblyInfo.AssemblyVersion.IsEmpty &&
                        assemblyInfo.AssemblyName.Equals(currAssemblyName.Name, StringComparison.Ordinal) &&
                        (!assemblyKeyProvided || ComparePublicKeyTokens(currAssemblyName, assemblyInfo.AssemblyToken)))
                    {
                        StringBuilder uriStringBuilder = new(assemblyInfo.AssemblyName.Length + assemblyInfo.PackagePartName.Length + 32);

                        uriStringBuilder.Append('/');
                        uriStringBuilder.Append(assemblyInfo.AssemblyName);
                        uriStringBuilder.Append(COMPONENT_DELIMITER);
                        uriStringBuilder.Append(VERSION);
                        uriStringBuilder.Append(version);
                        if (assemblyKeyProvided)
                        {
                            uriStringBuilder.Append(COMPONENT_DELIMITER);
                            uriStringBuilder.Append(assemblyInfo.AssemblyToken);
                        }
                        uriStringBuilder.Append(COMPONENT);
                        uriStringBuilder.Append('/');
                        uriStringBuilder.Append(assemblyInfo.PackagePartName);

                        string appendedUri = uriStringBuilder.ToString();

                        return baseUri is not null ? new Uri(baseUri, appendedUri) : new Uri(appendedUri, UriKind.Relative);
                    }
                }

            }

            return null;
        }

        /// <summary>
        /// Get <see cref="BaseUriProperty"/> for a <see cref="DependencyObject"/> inside a tree.
        /// </summary>
        /// <param name="element">A <see cref="DependencyObject"/> to perform search in.</param>
        /// <returns>BaseUri for the element.</returns>
        internal static Uri GetBaseUriCore(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            // Search the tree to find the closest parent which implements
            // IUriContext or have set value for BaseUri property.
            Uri baseUri = null;
            DependencyObject doCurrent = element;

            while (doCurrent is not null)
            {
                // Try to get BaseUri property value from current node.
                baseUri = doCurrent.GetValue(BaseUriProperty) as Uri;

                if (baseUri is not null)
                {
                    // Got the right node which is the closest to original element.
                    // Stop searching here.
                    break;
                }

                if (doCurrent is IUriContext uriContext)
                {
                    // If the element implements IUriContext, and if the BaseUri
                    // is not null, just take the BaseUri from there and stop the search loop.
                    baseUri = uriContext.BaseUri;

                    if (baseUri is not null)
                        break;
                }

                // The current node doesn't contain BaseUri value, try its parent node in the tree.

                if (doCurrent is UIElement uie)
                {
                    // Do the tree walk up
                    doCurrent = uie.GetUIParent(true);
                    continue;
                }

                if (doCurrent is ContentElement ce)
                {
                    doCurrent = ce.Parent;
                    continue;
                }

                // Try the Visual tree search
                if (doCurrent is Visual vis)
                {
                    doCurrent = VisualTreeHelper.GetParent(vis);
                    continue;
                }

                // Not even a Visual, break here to avoid an infinite loop
                break;
            }

            return baseUri;
        }

        private static bool ComparePublicKeyTokens(AssemblyName asmName, ReadOnlySpan<char> assemblyKey)
        {
            Span<byte> parsedKeyToken = assemblyKey.IsEmpty ? null : stackalloc byte[8];
            Convert.FromHexString(assemblyKey, parsedKeyToken, out _, out _);

            return ReflectionUtils.IsSamePublicKeyToken(asmName.GetPublicKeyToken(), parsedKeyToken);
        }

        /// <summary>
        /// Generally we do need to unescape pack uris, hence checking twice won't hurt so we don't end up allocating a <see cref="string"/>.
        /// </summary>
        /// <param name="uri">The uri to verify.</param>
        /// <returns><see langword="true"/> if <paramref name="uri"/> needs unescaping, <see langword="false"/> otherwise.</returns>
        private static bool IsUriUnescapeRequired(ReadOnlySpan<char> uri)
        {
            return uri.Contains('%');
        }
    }
}
