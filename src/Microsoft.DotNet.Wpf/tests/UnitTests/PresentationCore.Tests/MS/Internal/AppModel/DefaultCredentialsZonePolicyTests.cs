// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Threading;
using MS.Internal;
using MS.Internal.AppModel;
using MS.Win32;

namespace PresentationCore.Tests.MS.Internal.AppModel;

/// <summary>
/// Tests for <see cref="DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(Uri)"/> and the
/// <c>WpfWebRequestHelper.CreateRequest</c> integration that consumes it. These tests mutate
/// process-wide static state (the policy's <c>_securityManager</c> singleton and per-host
/// cache) and so are forced onto a single xUnit collection that disables parallelism.
/// </summary>
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

    private static void InstallSecurityManager(int zone) =>
        s_securityManagerField.SetValue(null, new FakeSecurityManager(zone));

    private static void InstallThrowingSecurityManager() =>
        s_securityManagerField.SetValue(null, new FakeSecurityManager(throwIfCalled: true));

    [Fact]
    public void ShouldSendDefaultCredentials_NullUri_ReturnsFalseWithoutCallingZoneCheck()
    {
        InstallThrowingSecurityManager();
        Assert.False(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(null!));
    }

    [Fact]
    public void ShouldSendDefaultCredentials_RelativeUri_ReturnsFalseWithoutCallingZoneCheck()
    {
        InstallThrowingSecurityManager();
        Assert.False(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri("/foo", UriKind.Relative)));
    }

    [Theory]
    [InlineData(NativeMethods.URLZONE_LOCAL_MACHINE, true)]
    [InlineData(NativeMethods.URLZONE_INTRANET, true)]
    [InlineData(NativeMethods.URLZONE_TRUSTED, true)]
    [InlineData(NativeMethods.URLZONE_INTERNET, false)]
    [InlineData(NativeMethods.URLZONE_UNTRUSTED, false)]
    public void ShouldSendDefaultCredentials_ZoneMatrix(int zone, bool expected)
    {
        // Use a non-IP host so the IP fast path doesn't short-circuit; pre-poison the cache
        // to mark it as non-local so the MapUrlToZone fallback is the sole decision source.
        const string Host = "policy-tests.example.test";
        GetCache()[Host] = false;
        InstallSecurityManager(zone);

        bool actual = DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri($"http://{Host}/x"));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ShouldSendDefaultCredentials_LoopbackLiteral_ReturnsTrueViaIpFastPath()
    {
        // No security manager installed - if the IP fast-path is bypassed, MapUrlToZone
        // would NRE. This proves loopback literals never reach the zone check.
        s_securityManagerField.SetValue(null, null);
        Assert.True(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri("http://127.0.0.1/")));
    }

    [Fact]
    public void MapUrlToZone_UnknownZone_TreatedAsBlocked()
    {
        const string Host = "policy-tests-unknown.example.test";
        GetCache()[Host] = false;
        InstallSecurityManager(zone: 999);

        Assert.False(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri($"http://{Host}/x")));
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
        GetCache()[Host] = false;

        // Simulate a failed COM activation: clear the singleton and set the
        // "previously failed" flag so EnsureSecurityManager will not retry.
        s_securityManagerField.SetValue(null, null);
        s_securityManagerInitFailedField.SetValue(null, true);

        Assert.False(DefaultCredentialsZonePolicy.ShouldSendDefaultCredentials(new Uri($"http://{Host}/x")));
    }

    /// <summary>
    /// End-to-end check that <c>WpfWebRequestHelper.CreateRequest</c> only enables
    /// <see cref="HttpWebRequest.UseDefaultCredentials"/> when the policy says so. The
    /// .NET handshake stack only emits NTLM/Negotiate when that flag (or an explicit
    /// Credentials object) is set; production code never sets Credentials, so the flag
    /// is the contract.
    /// </summary>
    [Theory]
    [InlineData(NativeMethods.URLZONE_INTRANET, true)]
    [InlineData(NativeMethods.URLZONE_INTERNET, false)]
    public void CreateRequest_HonorsPolicyForUseDefaultCredentials(int zone, bool expected)
    {
        const string Host = "policy-tests-integration.example.test";
        GetCache()[Host] = false;
        InstallSecurityManager(zone);

#pragma warning disable SYSLIB0014 // WebRequest is obsolete; production code still uses it.
        var request = (HttpWebRequest)WpfWebRequestHelper.CreateRequest(new Uri($"http://{Host}/api"));
#pragma warning restore SYSLIB0014

        Assert.Equal(expected, request.UseDefaultCredentials);
    }

    /// <summary>
    /// Minimal IInternetSecurityManager double - only MapUrlToZone is exercised by
    /// DefaultCredentialsZonePolicy. Other members throw so accidental calls fail loudly.
    /// </summary>
    private sealed class FakeSecurityManager : UnsafeNativeMethods.IInternetSecurityManager
    {
        private readonly int _zone;
        private readonly bool _throwIfCalled;

        public FakeSecurityManager(int zone) => _zone = zone;

        public FakeSecurityManager(bool throwIfCalled) => _throwIfCalled = throwIfCalled;

        public void MapUrlToZone(string pwszUrl, out int pdwZone, int dwFlags)
        {
            if (_throwIfCalled)
            {
                throw new InvalidOperationException("MapUrlToZone should not have been called.");
            }

            pdwZone = _zone;
        }

        public void SetSecuritySite(NativeMethods.IInternetSecurityMgrSite pSite) => throw new NotImplementedException();
        public unsafe void GetSecuritySite(void** ppSite) => throw new NotImplementedException();
        public unsafe void GetSecurityId(string pwszUrl, byte* pbSecurityId, int* pcbSecurityId, int dwReserved) => throw new NotImplementedException();
        public unsafe void ProcessUrlAction(string pwszUrl, int dwAction, byte* pPolicy, int cbPolicy, byte* pContext, int cbContext, int dwFlags, int dwReserved) => throw new NotImplementedException();
        public unsafe void QueryCustomPolicy(string pwszUrl, void* guidKey, byte** ppPolicy, int* pcbPolicy, byte* pContext, int cbContext, int dwReserved) => throw new NotImplementedException();
        public unsafe void SetZoneMapping(int dwZone, string lpszPattern, int dwFlags) => throw new NotImplementedException();
        public unsafe void GetZoneMappings(int dwZone, void** ppenumString, int dwFlags) => throw new NotImplementedException();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CredentialPolicyCollection
{
    public const string Name = "DefaultCredentialsZonePolicy serial";
    public static readonly object s_gate = new();
}
