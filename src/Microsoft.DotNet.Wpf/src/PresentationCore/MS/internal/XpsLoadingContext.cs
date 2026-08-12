// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unified XPS package-boundary enforcement and ambient loading context.
//
// Provides three entry points for XPS same-package containment checks:
//
//   1. IsUriAllowedInCurrentContext(uri)
//      Ambient check using AsyncLocal — used by PresentationCore sinks
//      (BitmapDecoder, PixelShader) that have no element context.
//
//   2. IsUriAllowedAgainstPackage(origin, uri)
//      Origin-tagged check — used for deferred/lazy resource loading
//      (FontSource, BitmapDownload, ColorContext) where the ambient context
//      is no longer active but the originating package URI was captured.
//
//   3. IsAllowedPackageRelativeUri(parentUri, resolvedUri)
//      Explicit check — used by PresentationFramework sinks
//      (DocumentReference, PageContent, Glyphs, ResourceDictionary) that
//      have access to the parent element's BaseUri.
//
// Set by PresentationFramework's XpsValidatingLoader during XPS content parsing.
//
// Kill switch (default off):
//   Switch.System.Windows.DisableXpsPackageBoundaryRestriction
//
// Security note:
//   Do NOT relax these checks without security review.
//   Fail-closed semantics are intentional.

using System;
using System.IO;
using System.IO.Packaging;
using System.Threading;

namespace MS.Internal
{
    internal static class XpsLoadingContext
    {
        private static readonly AsyncLocal<Uri> s_activePackageUri = new();

        /// <summary>
        /// Gets or sets the package URI of the XPS document currently being loaded
        /// on this async flow. Null when not inside an XPS load.
        /// </summary>
        internal static Uri ActivePackageUri
        {
            get => s_activePackageUri.Value;
            set => s_activePackageUri.Value = value;
        }

        /// <summary>
        /// Returns true when the current async flow is inside an XPS document load.
        /// </summary>
        internal static bool IsActive => s_activePackageUri.Value != null;

        // ----------------------------------------------------------------
        //  Ambient check — PresentationCore sinks
        // ----------------------------------------------------------------

        /// <summary>
        /// Checks whether the given URI is allowed in the current XPS loading context.
        /// When no XPS load is active, all URIs are allowed (non-XPS app scenario).
        /// When an XPS load is active, only pack:// URIs from the same package are allowed.
        /// </summary>
        internal static bool IsUriAllowedInCurrentContext(Uri uri)
        {
            return IsUriAllowedAgainstPackage(s_activePackageUri.Value, uri);
        }

        // ----------------------------------------------------------------
        //  Origin-tagged check — deferred/lazy sinks
        // ----------------------------------------------------------------

        /// <summary>
        /// Checks whether the given URI is allowed relative to a specific XPS package.
        /// Use this overload for deferred/lazy resource loading where the ambient
        /// context may no longer be active but the originating package URI was
        /// captured at object creation time.
        /// When xpsPackageOrigin is null, all URIs are allowed (non-XPS scenario).
        /// </summary>
        internal static bool IsUriAllowedAgainstPackage(Uri xpsPackageOrigin, Uri uri)
        {
            if (CoreAppContextSwitches.DisableXpsPackageBoundaryRestriction)
            {
                return true;
            }

            if (xpsPackageOrigin == null)
            {
                return true; // Not in XPS context — allow everything
            }

            return IsSamePackageUri(xpsPackageOrigin, uri);
        }

        // ----------------------------------------------------------------
        //  Explicit check — PresentationFramework sinks
        // ----------------------------------------------------------------

        /// <summary>
        /// Returns true when the resolved URI is allowed to be fetched as part
        /// of loading a child of an XPS package. The rules are:
        ///
        ///   * If the opt-out AppContext switch is set, return true (legacy).
        ///   * The resolved URI MUST be non-null.
        ///   * If the parent URI is null or NOT an XPS package context, the
        ///     call site is not inside an XPS package and the resolved
        ///     URI is allowed (legacy behavior outside XPS). This preserves
        ///     compatibility for plain ResourceDictionary loads from http://,
        ///     file://, application URIs, etc.
        ///   * If we are inside an active XPS load but parentUri is null,
        ///     fall through to same-package enforcement using the ambient
        ///     package URI.
        ///   * If the parent URI IS an absolute pack:// URI, the resolved URI
        ///     MUST also be absolute pack:// AND its package authority MUST
        ///     equal that of the parent.
        ///
        /// Any failure path returns false (fail-closed).
        /// </summary>
        internal static bool IsAllowedPackageRelativeUri(Uri parentUri, Uri resolvedUri)
        {
            if (CoreAppContextSwitches.DisableXpsPackageBoundaryRestriction)
            {
                return true;
            }

            if (resolvedUri == null)
            {
                return false;
            }

            if (!IsXpsPackageContext(parentUri))
            {
                if (parentUri == null && IsActive)
                {
                    parentUri = s_activePackageUri.Value;
                    if (parentUri == null)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }

            return IsSamePackageUri(parentUri, resolvedUri);
        }

        /// <summary>
        /// Throws <see cref="FileFormatException"/> when
        /// <see cref="IsAllowedPackageRelativeUri"/> returns false.
        /// </summary>
        internal static void EnforcePackageRelativeUri(Uri parentUri, Uri resolvedUri)
        {
            if (!IsAllowedPackageRelativeUri(parentUri, resolvedUri))
            {
                throw new FileFormatException(SR.Resource_XpsPackageBoundaryViolation);
            }
        }

        /// <summary>
        /// Returns true when <paramref name="uri"/> is an absolute pack:// URI
        /// whose authority encodes a real XPS package (i.e. an escaped package
        /// file URI). Returns false for null / non-pack URIs and for the two
        /// WPF-internal pack authorities "application:" and "siteoforigin:",
        /// which are not XPS packages.
        /// </summary>
        internal static bool IsXpsPackageContext(Uri uri)
        {
            if (uri == null || !uri.IsAbsoluteUri)
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string authority = uri.Authority ?? string.Empty;
            if (authority.StartsWith("application:", StringComparison.OrdinalIgnoreCase)
                || authority.StartsWith("siteoforigin:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        // ----------------------------------------------------------------
        //  Shared validation logic
        // ----------------------------------------------------------------

        /// <summary>
        /// Core same-package containment check shared by all entry points.
        /// Returns true if <paramref name="uri"/> is a pack:// URI belonging
        /// to the same package as <paramref name="packageOrigin"/>, or if
        /// <paramref name="uri"/> is null/relative.
        /// </summary>
        private static bool IsSamePackageUri(Uri packageOrigin, Uri uri)
        {
            if (uri == null || !uri.IsAbsoluteUri)
            {
                return true; // Relative URIs are resolved later; allow them
            }

            // In XPS context, only pack:// URIs from the same package are allowed
            if (!string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                return false; // http, https, file, UNC — all blocked in XPS context
            }

            // Reject WPF internal pack authorities (application:, siteoforigin:)
            // which point outside the current XPS package
            string authority = uri.Authority ?? string.Empty;
            if (authority.StartsWith("application:", StringComparison.OrdinalIgnoreCase)
                || authority.StartsWith("siteoforigin:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                Uri resolvedPackage = PackUriHelper.GetPackageUri(uri);
                Uri originPackage;

                try
                {
                    originPackage = PackUriHelper.GetPackageUri(packageOrigin);
                }
                catch (ArgumentException)
                {
                    originPackage = packageOrigin;
                }

                return resolvedPackage != null
                    && originPackage != null
                    && originPackage.Equals(resolvedPackage);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
    }
}
