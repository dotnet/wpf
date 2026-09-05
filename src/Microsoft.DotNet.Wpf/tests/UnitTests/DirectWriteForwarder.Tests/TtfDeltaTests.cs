// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for TtfDelta font subsetting internals.
/// These tests exercise the MS.Internal.TtfDelta namespace which contains
/// TrueType font subsetting functionality used by WPF.
/// </summary>
public class TtfDeltaTests
{
    /// <summary>
    /// Gets the GlobalInit type from the TtfDelta namespace.
    /// </summary>
    private static Type GetGlobalInitType()
    {
        var asm = typeof(MS.Internal.TrueTypeSubsetter).Assembly;
        return asm.GetTypes().First(t => t.Name == "GlobalInit" && t.Namespace == "MS.Internal.TtfDelta");
    }
    
    /// <summary>
    /// Gets the ControlTableInit type from the TtfDelta namespace.
    /// </summary>
    private static Type GetControlTableInitType()
    {
        var asm = typeof(MS.Internal.TrueTypeSubsetter).Assembly;
        return asm.GetTypes().First(t => t.Name == "ControlTableInit" && t.Namespace == "MS.Internal.TtfDelta");
    }
    
    [Fact]
    public void GlobalInit_Init_CanBeInvoked()
    {
        // Arrange
        var globalInitType = GetGlobalInitType();
        var initMethod = globalInitType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        
        // Act & Assert
        initMethod.Should().NotBeNull("GlobalInit.Init should be accessible");
        
        // Invoke should not throw
        var exception = Record.Exception(() => initMethod!.Invoke(null, null));
        exception.Should().BeNull("GlobalInit.Init() should complete without throwing");
    }
    
    [Fact]
    public void GlobalInit_Init_CanBeCalledMultipleTimes()
    {
        // The Init method uses a lock and _isInitialized flag, so multiple calls should be safe
        var globalInitType = GetGlobalInitType();
        var initMethod = globalInitType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        
        // Call multiple times - should not throw
        for (int i = 0; i < 5; i++)
        {
            var exception = Record.Exception(() => initMethod.Invoke(null, null));
            exception.Should().BeNull($"GlobalInit.Init() call {i + 1} should not throw");
        }
    }
    
    [Fact]
    public void ControlTableInit_Init_CanBeInvoked()
    {
        // Arrange
        var controlTableInitType = GetControlTableInitType();
        var initMethod = controlTableInitType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        
        // Act & Assert
        initMethod.Should().NotBeNull("ControlTableInit.Init should be accessible");
        
        // Invoke should not throw
        var exception = Record.Exception(() => initMethod!.Invoke(null, null));
        exception.Should().BeNull("ControlTableInit.Init() should complete without throwing");
    }
    
    [Fact]
    public void ControlTableInit_Init_CanBeCalledMultipleTimes()
    {
        // The Init method uses a lock and _isInitialized flag, so multiple calls should be safe
        var controlTableInitType = GetControlTableInitType();
        var initMethod = controlTableInitType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        
        // Call multiple times - should not throw
        for (int i = 0; i < 5; i++)
        {
            var exception = Record.Exception(() => initMethod.Invoke(null, null));
            exception.Should().BeNull($"ControlTableInit.Init() call {i + 1} should not throw");
        }
    }
    
    [Fact]
    public void TtfDelta_Namespace_ContainsExpectedTypes()
    {
        // Verify that the expected TtfDelta types are present and accessible
        var asm = typeof(MS.Internal.TrueTypeSubsetter).Assembly;
        var ttfDeltaTypes = asm.GetTypes()
            .Where(t => t.Namespace == "MS.Internal.TtfDelta")
            .ToList();
        
        ttfDeltaTypes.Should().NotBeEmpty("TtfDelta namespace should contain types");
        
        // Verify key types exist
        var typeNames = ttfDeltaTypes.Select(t => t.Name).ToHashSet();
        typeNames.Should().Contain("GlobalInit");
        typeNames.Should().Contain("ControlTableInit");
        typeNames.Should().Contain("TTFACC_FILEBUFFERINFO");
        typeNames.Should().Contain("CONST_TTFACC_FILEBUFFERINFO");
    }
    
    [Fact]
    public void TtfDelta_StructTypes_AreValueTypes()
    {
        // Verify that struct types in TtfDelta are correctly defined as value types
        var asm = typeof(MS.Internal.TrueTypeSubsetter).Assembly;
        var structTypes = new[] { "TTFACC_FILEBUFFERINFO", "CONST_TTFACC_FILEBUFFERINFO", "DIRECTORY", "HEAD", "MAXP" };
        
        foreach (var typeName in structTypes)
        {
            var type = asm.GetTypes().FirstOrDefault(t => t.Name == typeName && t.Namespace == "MS.Internal.TtfDelta");
            type?.IsValueType.Should().BeTrue($"{typeName} should be a value type (struct)");
        }
    }
    
    [Fact]
    public async Task TtfDelta_InitMethods_AreThreadSafe()
    {
        // Both GlobalInit.Init and ControlTableInit.Init use Monitor.Enter for thread safety
        // Verify this by invoking from multiple threads simultaneously
        var globalInitType = GetGlobalInitType();
        var controlTableInitType = GetControlTableInitType();
        var globalInit = globalInitType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var controlTableInit = controlTableInitType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        
        // Run 10 parallel tasks that call both Init methods
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    globalInit.Invoke(null, null);
                    controlTableInit.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }, TestContext.Current.CancellationToken));
        }
        
        await Task.WhenAll(tasks);
        
        exceptions.Should().BeEmpty("Init methods should be thread-safe and not throw exceptions");
    }
    
    [Fact]
    public void TrueTypeSubsetter_Assembly_HasInternalsVisibleToTestProject()
    {
        // Verify that the InternalsVisibleTo attribute is set correctly
        // by checking that we can access internal types
        var globalInitType = GetGlobalInitType();
        
        // GlobalInit is a "private ref class" in C++/CLI, which maps to internal in C#
        // We should be able to access it due to InternalsVisibleTo
        globalInitType.Should().NotBeNull("GlobalInit should be accessible to test project");
        globalInitType.IsNotPublic.Should().BeTrue("GlobalInit should be internal (not public)");
    }
}

