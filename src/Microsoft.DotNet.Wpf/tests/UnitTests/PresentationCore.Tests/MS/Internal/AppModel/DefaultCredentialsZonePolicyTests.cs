// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using MS.Internal.AppModel;

namespace PresentationCore.Tests.MS.Internal.AppModel;

/// <summary>
/// Tests for <see cref="DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(Uri)"/>.
/// These tests mutate process-wide static state (the policy's <c>_securityManager</c>
/// singleton and per-host cache) and so are forced onto a single xUnit collection that
/// disables parallelism.
/// </summary>
/// <remarks>
/// The tests deliberately exercise only the deterministic paths that do not depend on the
/// native <c>IInternetSecurityManager</c> COM zone lookup: the null/relative early-outs, the
/// local/private IP fast path, and the fail-closed behavior when the security manager could
/// not be created. Zone-classification for routable hosts is delegated to the OS zone manager
/// and is not unit-testable without a live COM object, so it is intentionally not covered here.
/// </remarks>
[Collection(CredentialPolicyCollection.Name)]
public sealed class DefaultCredentialsZonePolicyTests : IDisposable
{
    private static readonly Type s_policyType = typeof(DefaultCredentialsZonePolicy);

    private static readonly FieldInfo s_securityManagerField =
        s_policyType.GetField("_securityManager", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly FieldInfo s_securityManagerInitFailedField =
        s_policyType.GetField("_securityManagerInitFailed", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly FieldInfo s_localHostCacheField =
        s_policyType.GetField("s_localHostCache", BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly object? _originalSecurityManager;
    private readonly bool _originalSecurityManagerInitFailed;

    public DefaultCredentialsZonePolicyTests()
    {
        Monitor.Enter(CredentialPolicyCollection.s_gate);
        _originalSecurityManager = s_securityManagerField.GetValue(null);
        _originalSecurityManagerInitFailed = (bool)s_securityManagerInitFailedField.GetValue(null)!;
        GetCache().Clear();
    }

    public void Dispose()
    {
        s_securityManagerField.SetValue(null, _originalSecurityManager);
        s_securityManagerInitFailedField.SetValue(null, _originalSecurityManagerInitFailed);
        GetCache().Clear();
        Monitor.Exit(CredentialPolicyCollection.s_gate);
    }

    private static ConcurrentDictionary<string, bool> GetCache() =>
        (ConcurrentDictionary<string, bool>)s_localHostCacheField.GetValue(null)!;

    [Fact]
    public void ShouldSendDefaultCredentials_NullUri_ReturnsFalse()
    {
        Assert.False(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(null!));
    }

    [Fact]
    public void ShouldSendDefaultCredentials_RelativeUri_ReturnsFalse()
    {
        Assert.False(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri("/foo", UriKind.Relative)));
    }

    [Theory]
    [InlineData("http://127.0.0.1/")]        // IPv4 loopback
    [InlineData("http://10.0.0.1/")]         // RFC1918 10/8
    [InlineData("http://172.16.0.1/")]       // RFC1918 172.16/12
    [InlineData("http://192.168.1.1/")]      // RFC1918 192.168/16
    [InlineData("http://169.254.1.1/")]      // IPv4 link-local
    [InlineData("http://[::1]/")]            // IPv6 loopback
    public void ShouldSendDefaultCredentials_LocalOrPrivateLiteral_ReturnsTrueViaIpFastPath(string url)
    {
        // Literal non-routable addresses resolve locally without DNS and are permitted
        // by the pre-check before the native zone lookup is ever consulted.
        s_securityManagerField.SetValue(null, null);
        Assert.True(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri(url)));
    }

    /// <summary>
    /// On exotic Windows SKUs the COM activation for IInternetSecurityManager can
    /// fail. The policy must fail closed in that case (return false) rather than
    /// throwing or silently allowing credentials.
    /// </summary>
    [Fact]
    public void ShouldSendDefaultCredentials_SecurityManagerInitFailed_FailsClosed()
    {
        const string Host = "policy-tests-no-sm.example.test";

        // Mark the host as non-local so the decision falls through to the (unavailable)
        // zone manager rather than the local/private fast path.
        GetCache()[Host] = false;

        // Simulate a failed COM activation: clear the singleton and set the
        // "previously failed" flag so EnsureSecurityManager will not retry.
        s_securityManagerField.SetValue(null, null);
        s_securityManagerInitFailedField.SetValue(null, true);

        Assert.False(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri($"http://{Host}/x")));
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CredentialPolicyCollection
{
    public const string Name = "DefaultCredentialsZonePolicy serial";
    public static readonly object s_gate = new();
}
