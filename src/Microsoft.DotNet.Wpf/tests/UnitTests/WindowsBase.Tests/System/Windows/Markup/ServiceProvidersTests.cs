// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Markup.Tests;

public class ServiceProvidersTests
{
    [Fact]
    public void AddService_Invoke_GetServiceReturnsExpected()
    {
        var serviceProviders = new ServiceProviders();

        var service1 = new object();
        serviceProviders.AddService(typeof(object), service1);
        Assert.Same(service1, serviceProviders.GetService(typeof(object)));

        // Add again.
        serviceProviders.AddService(typeof(object), service1);
        Assert.Same(service1, serviceProviders.GetService(typeof(object)));

        // Add another.
        var service2 = new object();
        serviceProviders.AddService(typeof(string), service2);
        Assert.Same(service1, serviceProviders.GetService(typeof(object)));
        Assert.Same(service2, serviceProviders.GetService(typeof(string)));
    }

    [Fact]
    public void AddService_NullServiceType_ThrowsArgumentNullException()
    {
        var serviceProviders = new ServiceProviders();
        Assert.Throws<ArgumentNullException>("serviceType", () => serviceProviders.AddService(null, new object()));
    }

    [Fact]
    public void AddService_NullService_ThrowsArgumentNullException()
    {
        var serviceProviders = new ServiceProviders();
        Assert.Throws<ArgumentNullException>("service", () => serviceProviders.AddService(typeof(object), null));
    }

    [Fact]
    public void AddService_ServiceTypeExists_ThrowsArgumentException()
    {
        var serviceProviders = new ServiceProviders();
        serviceProviders.AddService(typeof(object), new object());
        Assert.Throws<ArgumentException>("serviceType", () => serviceProviders.AddService(typeof(object), new object()));
    }

    [Fact]
    public void GetService_NoSuchServiceTypeEmpty_ReturnsNull()
    {
        var serviceProviders = new ServiceProviders();
        Assert.Null(serviceProviders.GetService(typeof(object)));
    }

    [Fact]
    public void GetService_NoSuchServiceTypeNotEmpty_ReturnsNull()
    {
        var serviceProviders = new ServiceProviders();
        serviceProviders.AddService(typeof(string), new object());

        Assert.Null(serviceProviders.GetService(typeof(object)));
    }

    [Fact]
    public void GetService_NullServiceTypeEmpty_ThrowsArgumentNullException()
    {
        var serviceProviders = new ServiceProviders();
        // TODO: should have correct paramName
        Assert.Throws<ArgumentNullException>("key", () => serviceProviders.GetService(null));
    }

    [Fact]
    public void GetService_NullServiceTypeNotEmpty_ThrowsArgumentNullException()
    {
        var serviceProviders = new ServiceProviders();
        serviceProviders.AddService(typeof(string), new object());

        // TODO: should have correct paramName
        Assert.Throws<ArgumentNullException>("key", () => serviceProviders.GetService(null));
    }
}
