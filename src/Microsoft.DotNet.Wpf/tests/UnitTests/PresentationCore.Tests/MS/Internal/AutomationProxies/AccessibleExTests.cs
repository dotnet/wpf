// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

extern alias uiaProviders;

using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using Accessibility;
using Moq;
using Accessible = uiaProviders::MS.Internal.AutomationProxies.Accessible;
using AccessibleRole = uiaProviders::MS.Internal.AutomationProxies.AccessibleRole;
using MSAAEventDispatcher = uiaProviders::MS.Internal.AutomationProxies.MSAAEventDispatcher;
using MsaaNativeProvider = uiaProviders::MS.Internal.AutomationProxies.MsaaNativeProvider;
using NativeMethods = uiaProviders::MS.Win32.NativeMethods;
using UnsafeNativeMethods = uiaProviders::MS.Win32.UnsafeNativeMethods;

namespace PresentationCore.Tests.MS.Internal.AutomationProxies;

public class AccessibleExTests
{
    [Fact]
    public void IsPatternSupported_OutlineItem_SupportsSelectionItem()
    {
        TestMsaaNativeProvider provider = new(CreateAccessible(AccessibleRole.OutlineItem));

        Assert.True(provider.IsPatternSupported(SelectionItemPattern.Pattern));
    }

    [Fact]
    public void IsPatternSupported_Outline_SupportsSelection()
    {
        TestMsaaNativeProvider provider = new(CreateAccessible(AccessibleRole.Outline));

        Assert.True(provider.IsPatternSupported(SelectionPattern.Pattern));
    }

    [Fact]
    public void SelectionContainer_NestedOutlineItem_ReturnsOutlineAncestor()
    {
        TestMsaaNativeProvider root = new(
            CreateAccessible(AccessibleRole.Outline),
            parent: null,
            knownRoot: null,
            MsaaNativeProvider.RootStatus.Root);
        TestMsaaNativeProvider parent = new(
            CreateAccessible(AccessibleRole.OutlineItem),
            root,
            root,
            MsaaNativeProvider.RootStatus.NotRoot);
        TestMsaaNativeProvider child = new(
            CreateAccessible(AccessibleRole.OutlineItem),
            parent,
            root,
            MsaaNativeProvider.RootStatus.NotRoot);

        IRawElementProviderSimple? container =
            ((ISelectionItemProvider)child).SelectionContainer;

        Assert.Same(root, container);
    }

    [Fact]
    public void GetPatternProvider_UnknownPattern_ReturnsNull()
    {
        TestMsaaNativeProvider provider = new(CreateAccessible(AccessibleRole.OutlineItem));

        object? pattern =
            ((IRawElementProviderSimple)provider).GetPatternProvider(int.MaxValue);

        Assert.Null(pattern);
    }

    [Fact]
    public void GetAccessibleExProvider_Self_ReturnsRootProvider()
    {
        TestAccessibleEx accessibleEx = new();
        TestServiceProvider serviceProvider = new(accessibleEx);

        IRawElementProviderSimple? provider =
            Accessible.GetAccessibleExProvider(serviceProvider, NativeMethods.CHILD_SELF);

        Assert.Same(accessibleEx, provider);
        Assert.Equal(typeof(UnsafeNativeMethods.IAccessibleEx).GUID, serviceProvider.ServiceId);
        Assert.Equal(typeof(UnsafeNativeMethods.IAccessibleEx).GUID, serviceProvider.InterfaceId);
        Assert.Equal(0, accessibleEx.GetObjectForChildCallCount);
    }

    [Fact]
    public void GetAccessibleExProvider_Child_ReturnsChildProvider()
    {
        TestAccessibleEx child = new();
        TestAccessibleEx root = new() { Child = child };
        TestServiceProvider serviceProvider = new(root);

        IRawElementProviderSimple? provider =
            Accessible.GetAccessibleExProvider(serviceProvider, childId: 42);

        Assert.Same(child, provider);
        Assert.Equal(1, root.GetObjectForChildCallCount);
        Assert.Equal(42, root.LastChildId);
    }

    [Fact]
    public void GetAccessibleExProvider_ServiceDoesNotImplementIAccessibleEx_ReturnsNull()
    {
        TestServiceProvider serviceProvider = new(new object());

        IRawElementProviderSimple? provider =
            Accessible.GetAccessibleExProvider(serviceProvider, NativeMethods.CHILD_SELF);

        Assert.Null(provider);
    }

    [Theory]
    [InlineData(NativeMethods.E_FAIL)]
    [InlineData(NativeMethods.E_NOINTERFACE)]
    [InlineData(NativeMethods.E_NOTIMPL)]
    [InlineData(NativeMethods.E_INVALIDARG)]
    public void GetAccessibleExProvider_UnsupportedService_ReturnsNull(int errorCode)
    {
        TestServiceProvider serviceProvider = new(GetExceptionForHR(errorCode));

        IRawElementProviderSimple? provider =
            Accessible.GetAccessibleExProvider(serviceProvider, NativeMethods.CHILD_SELF);

        Assert.Null(provider);
    }

    [Fact]
    public void GetAccessibleExProvider_UnsupportedChild_ReturnsNull()
    {
        TestAccessibleEx root = new()
        {
            GetObjectForChildException = GetExceptionForHR(NativeMethods.E_INVALIDARG)
        };

        IRawElementProviderSimple? provider =
            Accessible.GetAccessibleExProvider(new TestServiceProvider(root), childId: 42);

        Assert.Null(provider);
    }

    [Fact]
    public void GetAccessibleExProvider_UnexpectedComFailure_Throws()
    {
        COMException expected = Assert.IsType<COMException>(
            GetExceptionForHR(NativeMethods.RPC_E_DISCONNECTED));
        TestServiceProvider serviceProvider = new(expected);

        COMException actual = Assert.Throws<COMException>(
            () => Accessible.GetAccessibleExProvider(serviceProvider, NativeMethods.CHILD_SELF));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetPatternProvider_SupportedPattern_ReturnsPatternProvider()
    {
        object expected = new();
        TestAccessibleEx accessibleEx = new() { PatternProvider = expected };

        object? actual = Accessible.GetPatternProvider(accessibleEx, patternId: 10005);

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(NativeMethods.E_FAIL)]
    [InlineData(NativeMethods.E_NOTIMPL)]
    [InlineData(NativeMethods.E_INVALIDARG)]
    public void GetPatternProvider_UnsupportedPattern_ReturnsNull(int errorCode)
    {
        TestAccessibleEx accessibleEx = new()
        {
            GetPatternProviderException = GetExceptionForHR(errorCode)
        };

        object? actual = Accessible.GetPatternProvider(accessibleEx, patternId: 10005);

        Assert.Null(actual);
    }

    [Fact]
    public void GetPatternProvider_UnexpectedComFailure_Throws()
    {
        COMException expected = Assert.IsType<COMException>(
            GetExceptionForHR(NativeMethods.RPC_E_DISCONNECTED));
        TestAccessibleEx accessibleEx = new() { GetPatternProviderException = expected };

        COMException actual = Assert.Throws<COMException>(
            () => Accessible.GetPatternProvider(accessibleEx, patternId: 10005));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetPatternProvider_ProjectedInvalidArgument_ReturnsNull()
    {
        Exception projected = GetExceptionForHR(NativeMethods.E_INVALIDARG);
        Assert.IsType<ArgumentException>(projected);
        TestAccessibleEx accessibleEx = new()
        {
            GetPatternProviderException = projected
        };

        object? actual = Accessible.GetPatternProvider(accessibleEx, patternId: 10005);

        Assert.Null(actual);
    }

    [Fact]
    public void GetPatternProvider_ProjectedNoInterface_ReturnsNull()
    {
        Exception projected = GetExceptionForHR(NativeMethods.E_NOINTERFACE);
        Assert.IsType<InvalidCastException>(projected);
        TestAccessibleEx accessibleEx = new()
        {
            GetPatternProviderException = projected
        };

        object? actual = Accessible.GetPatternProvider(accessibleEx, patternId: 10005);

        Assert.Null(actual);
    }

    [Fact]
    public void GetPropertyValue_SupportedProperty_ReturnsValue()
    {
        object expected = new();
        TestAccessibleEx accessibleEx = new() { PropertyValue = expected };

        object? actual = Accessible.GetPropertyValue(accessibleEx, propertyId: 30152);

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(NativeMethods.E_FAIL)]
    [InlineData(NativeMethods.E_NOINTERFACE)]
    [InlineData(NativeMethods.E_NOTIMPL)]
    [InlineData(NativeMethods.E_INVALIDARG)]
    public void GetPropertyValue_UnsupportedProperty_ReturnsNull(int errorCode)
    {
        TestAccessibleEx accessibleEx = new()
        {
            GetPropertyValueException = GetExceptionForHR(errorCode)
        };

        object? actual = Accessible.GetPropertyValue(accessibleEx, propertyId: 30152);

        Assert.Null(actual);
    }

    [Fact]
    public void GetPropertyValue_UnexpectedComFailure_Throws()
    {
        COMException expected = Assert.IsType<COMException>(
            GetExceptionForHR(NativeMethods.RPC_E_DISCONNECTED));
        TestAccessibleEx accessibleEx = new() { GetPropertyValueException = expected };

        COMException actual = Assert.Throws<COMException>(
            () => Accessible.GetPropertyValue(accessibleEx, propertyId: 30152));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetPatternPropertyValue_ExpandCollapse_ReturnsState()
    {
        TestAccessibleEx provider = new()
        {
            PatternProvider = new TestExpandCollapseProvider(ExpandCollapseState.Expanded)
        };

        object? actual = MsaaNativeProvider.GetPatternPropertyValue(
            provider,
            ExpandCollapsePattern.ExpandCollapseStateProperty);

        Assert.Equal(ExpandCollapseState.Expanded, actual);
    }

    [Fact]
    public void GetPatternPropertyValue_Toggle_ReturnsState()
    {
        TestAccessibleEx provider = new()
        {
            PatternProvider = new TestToggleProvider(ToggleState.Indeterminate)
        };

        object? actual = MsaaNativeProvider.GetPatternPropertyValue(
            provider,
            TogglePattern.ToggleStateProperty);

        Assert.Equal(ToggleState.Indeterminate, actual);
    }

    [Fact]
    public void GetPatternPropertyValue_UnrelatedProperty_ReturnsNull()
    {
        object? actual = MsaaNativeProvider.GetPatternPropertyValue(
            new TestAccessibleEx(),
            AutomationElement.NameProperty);

        Assert.Null(actual);
    }

    [Fact]
    public void GetPatternPropertyFromWinEvent_ExpandCollapse_ReturnsProperty()
    {
        AutomationProperty? property = MSAAEventDispatcher.GetPatternPropertyFromWinEvent(
            ExpandCollapsePattern.ExpandCollapseStateProperty.Id);

        Assert.Same(ExpandCollapsePattern.ExpandCollapseStateProperty, property);
    }

    [Fact]
    public void GetPatternPropertyFromWinEvent_Toggle_ReturnsProperty()
    {
        AutomationProperty? property = MSAAEventDispatcher.GetPatternPropertyFromWinEvent(
            TogglePattern.ToggleStateProperty.Id);

        Assert.Same(TogglePattern.ToggleStateProperty, property);
    }

    [Theory]
    [InlineData(NativeMethods.EVENT_OBJECT_STATECHANGE)]
    [InlineData(30000)]
    public void GetPatternPropertyFromWinEvent_UnsupportedProperty_ReturnsNull(int eventId)
    {
        Assert.Null(MSAAEventDispatcher.GetPatternPropertyFromWinEvent(eventId));
    }

    private static Exception GetExceptionForHR(int errorCode)
    {
        return Marshal.GetExceptionForHR(errorCode)!;
    }

    private sealed class TestServiceProvider : UnsafeNativeMethods.IServiceProvider
    {
        private readonly object _service;

        internal TestServiceProvider(object service)
        {
            _service = service;
        }

        internal Guid ServiceId { get; private set; }
        internal Guid InterfaceId { get; private set; }

        public object QueryService(ref Guid service, ref Guid riid)
        {
            ServiceId = service;
            InterfaceId = riid;

            if (_service is Exception exception)
            {
                throw exception;
            }

            return _service;
        }
    }

    private sealed class TestAccessibleEx :
        UnsafeNativeMethods.IAccessibleEx,
        IRawElementProviderSimple
    {
        internal TestAccessibleEx? Child { get; set; }
        internal Exception? GetObjectForChildException { get; set; }
        internal Exception? GetPatternProviderException { get; set; }
        internal Exception? GetPropertyValueException { get; set; }
        internal object? PatternProvider { get; set; }
        internal object? PropertyValue { get; set; }
        internal int GetObjectForChildCallCount { get; private set; }
        internal int LastChildId { get; private set; }

        public UnsafeNativeMethods.IAccessibleEx GetObjectForChild(int idChild)
        {
            GetObjectForChildCallCount++;
            LastChildId = idChild;

            if (GetObjectForChildException is Exception exception)
            {
                throw exception;
            }

            return Child!;
        }

        public void GetIAccessiblePair(out IAccessible accessible, out int childId)
        {
            throw new NotSupportedException();
        }

        public int[] GetRuntimeId()
        {
            throw new NotSupportedException();
        }

        public UnsafeNativeMethods.IAccessibleEx ConvertReturnedElement(IRawElementProviderSimple provider)
        {
            throw new NotSupportedException();
        }

        ProviderOptions IRawElementProviderSimple.ProviderOptions =>
            ProviderOptions.ServerSideProvider;

        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            if (GetPatternProviderException is Exception exception)
            {
                throw exception;
            }

            return PatternProvider!;
        }

        object IRawElementProviderSimple.GetPropertyValue(int propertyId)
        {
            if (GetPropertyValueException is Exception exception)
            {
                throw exception;
            }

            return PropertyValue!;
        }

        IRawElementProviderSimple IRawElementProviderSimple.HostRawElementProvider => null!;
    }

    private sealed class TestExpandCollapseProvider : IExpandCollapseProvider
    {
        internal TestExpandCollapseProvider(ExpandCollapseState state)
        {
            ExpandCollapseState = state;
        }

        public ExpandCollapseState ExpandCollapseState { get; }

        public void Collapse()
        {
            throw new NotSupportedException();
        }

        public void Expand()
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestToggleProvider : IToggleProvider
    {
        internal TestToggleProvider(ToggleState state)
        {
            ToggleState = state;
        }

        public ToggleState ToggleState { get; }

        public void Toggle()
        {
            throw new NotSupportedException();
        }
    }

    private static Accessible CreateAccessible(AccessibleRole role)
    {
        Mock<IAccessible> accessible = new(MockBehavior.Strict);
        accessible
            .Setup(instance => instance.get_accRole(NativeMethods.CHILD_SELF))
            .Returns((int)role);
        return Accessible.Wrap(accessible.Object);
    }

    private sealed class TestMsaaNativeProvider : MsaaNativeProvider
    {
        internal TestMsaaNativeProvider(Accessible accessible)
            : this(
                accessible,
                parent: null,
                knownRoot: null,
                RootStatus.NotRoot)
        {
        }

        internal TestMsaaNativeProvider(
            Accessible accessible,
            MsaaNativeProvider? parent,
            MsaaNativeProvider? knownRoot,
            RootStatus rootStatus)
            : base(
                accessible,
                new IntPtr(1),
                parent!,
                knownRoot!,
                rootStatus)
        {
        }
    }
}
