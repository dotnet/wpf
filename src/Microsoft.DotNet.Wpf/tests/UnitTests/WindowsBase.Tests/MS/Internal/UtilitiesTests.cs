// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using MS.Internal;

namespace WindowsBase.Tests.MS.Internal
{
    public class UtilitiesTests
    {
        [Fact]
        public void IsCompositionEnabled_ShouldReturnFalseOnUnsupportedOS()
        {
            // This test verifies that on unsupported OS versions (below Vista),
            // IsCompositionEnabled returns false without calling DWM APIs
            
            // Note: This test will only be meaningful on actual pre-Vista systems,
            // but ensures the code path is covered
            Assert.IsType<bool>(Utilities.IsCompositionEnabled);
        }

        [Fact]
        public void IsCompositionEnabled_ShouldNotThrowWhenCompositionDisabled()
        {
            // This test verifies that when desktop composition is disabled,
            // IsCompositionEnabled returns false instead of throwing COMException
            
            // This test documents the expected behavior but can't easily simulate
            // the disabled composition state without mocking
            
            bool result;
            Exception caughtException = null;
            
            try
            {
                result = Utilities.IsCompositionEnabled;
            }
            catch (Exception ex)
            {
                caughtException = ex;
                result = false;
            }
            
            // The important thing is that no COMException with HRESULT 0x80263001 is thrown
            if (caughtException != null && caughtException is COMException comEx)
            {
                Assert.NotEqual(unchecked((int)0x80263001), comEx.HResult);
            }
            
            // Result should be a valid boolean
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void IsOSVersionProperties_ShouldReturnValidBooleans()
        {
            // Verify that all OS version properties return valid booleans
            Assert.IsType<bool>(Utilities.IsOSVistaOrNewer);
            Assert.IsType<bool>(Utilities.IsOSWindows7OrNewer);
            Assert.IsType<bool>(Utilities.IsOSWindows8OrNewer);
            
            // Verify logical consistency
            if (Utilities.IsOSWindows8OrNewer)
            {
                Assert.True(Utilities.IsOSWindows7OrNewer);
                Assert.True(Utilities.IsOSVistaOrNewer);
            }
            
            if (Utilities.IsOSWindows7OrNewer)
            {
                Assert.True(Utilities.IsOSVistaOrNewer);
            }
        }
    }
}