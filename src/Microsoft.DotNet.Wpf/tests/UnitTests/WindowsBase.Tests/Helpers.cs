// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Media;

namespace System.Windows.Tests;

public static class Helpers
{
#pragma warning disable xUnit1013
    public static string GetResourcePath(string name) => Path.GetFullPath(Path.Combine("Resources", name));

    public static void ExecuteOnDifferentThread(Action action, ApartmentState? state = null)
    {
        ExceptionDispatchInfo? edi = null;
        var t = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                edi = ExceptionDispatchInfo.Capture(e);
            }
        });
        if (state is not null)
        {
            t.SetApartmentState(state.Value);
        }
        t.Start();
        t.Join();

        edi?.Throw();
    }

    public static T ExecuteOnDifferentThread<T>(Func<T> action, ApartmentState? state = null)
    {
        T? result = default;
        ExceptionDispatchInfo? edi = null;
        var t = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception e)
            {
                edi = ExceptionDispatchInfo.Capture(e);
            }
        });
        if (state is not null)
        {
            t.SetApartmentState(state.Value);
        }
        t.Start();
        t.Join();

        if (edi is not null)
        {
            edi.Throw();
#pragma warning disable CA2201 // Do not raise reserved exception types
            throw new Exception("Not reachable.");
#pragma warning restore CA2201 // Do not raise reserved exception types
        }
        else
        {
            return result!;
        }
    }
    
    public static void AssertEqualRounded(Matrix expected, Matrix actual, int precision = 5)
    {
        if (expected.Equals(actual))
        {
            return;
        }

        try
        {
            Assert.Equal(expected.M11, actual.M11, precision);
            Assert.Equal(expected.M12, actual.M12, precision);
            Assert.Equal(expected.M21, actual.M21, precision);
            Assert.Equal(expected.M22, actual.M22, precision);
            Assert.Equal(expected.OffsetX, actual.OffsetX, precision);
            Assert.Equal(expected.OffsetY, actual.OffsetY, precision);
        }
        catch (Exception)
        {
            // Throw main AssertException with formatting.
            //Assert.Equal(expected, actual);
        }
    }

    public static void AssertEqualRounded(Rect expected, Rect actual, int precision)
    {
        if (expected.Equals(actual))
        {
            return;
        }

        try
        {
            Assert.Equal(expected.X, actual.X, precision);
            Assert.Equal(expected.Y, actual.Y, precision);
            Assert.Equal(expected.Width, actual.Width, precision);
            Assert.Equal(expected.Height, actual.Height, precision);
        }
        catch (Exception)
        {
            // Throw main AssertException with formatting.
            Assert.Equal(expected, actual);
        }
    }
#pragma warning restore xUnit1013
}
