// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Tests;

public class BaseCompatibilityPreferencesTests
{
    [Fact]
    public void GetFlowDirectionFromLayoutRounding_Get_ReturnsExpected()
    {
        bool value = BaseCompatibilityPreferences.FlowDispatcherSynchronizationContextPriority;
        Assert.Equal(value, BaseCompatibilityPreferences.FlowDispatcherSynchronizationContextPriority);
    }
    
    [Fact]
    public void HandleDispatcherRequestProcessingFailure_Get_ReturnsExpected()
    {
        BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions value = BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure;
        Assert.True(Enum.IsDefined(value));
        Assert.Equal(value, BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure);
    }

    [Fact]
    public void InlineDispatcherSynchronizationContextSend_Get_ReturnsExpected()
    {
        bool value = BaseCompatibilityPreferences.InlineDispatcherSynchronizationContextSend;
        Assert.Equal(value, BaseCompatibilityPreferences.InlineDispatcherSynchronizationContextSend);
    }

    [Fact]
    public void ReuseDispatcherSynchronizationContextInstance_Get_ReturnsExpected()
    {
        bool value = BaseCompatibilityPreferences.ReuseDispatcherSynchronizationContextInstance;
        Assert.Equal(value, BaseCompatibilityPreferences.ReuseDispatcherSynchronizationContextInstance);
    }
}
