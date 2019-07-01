// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;

#if !STANDALONE_BUILD
using Microsoft.Test.Security.Wrappers;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Infrastructure for running under partial trust test scenarios
    /// </summary>
    public class PT
    {

#if !STANDALONE_BUILD
        /// <summary/>
        [System.CLSCompliant(false)]
        public static TypeSW Trust(Type type)
        {
            return TypeSW.Wrap(type);
        }
#else
        /// <summary/>
        [System.CLSCompliant(false)]
        public static Type Trust(Type type)
        {
            return type;
        }
#endif

#if !STANDALONE_BUILD
        /// <summary/>
        [System.CLSCompliant(false)]
        public static WindowSW Trust(Window window)
        {
            return WindowSW.Wrap(window);
        }
#else
        /// <summary/>
        [System.CLSCompliant(false)]
        public static Window Trust(Window window)
        {
            return window;
        }
#endif

#if !STANDALONE_BUILD
        /// <summary/>
        [System.CLSCompliant(false)]
        public static ApplicationSW Trust(Application application)
        {
            return ApplicationSW.Wrap(application);
        }
#else
        /// <summary/>
        [System.CLSCompliant(false)]
        public static Application Trust(Application application)
        {
            return application;
        }
#endif

#if !STANDALONE_BUILD
        /// <summary/>
        [System.CLSCompliant(false)]
        public static Type Untrust(TypeSW type)
        {
            return type.InnerObject;
        }
#else
        /// <summary/>
        [System.CLSCompliant(false)]
        public static Type Untrust(Type type)
        {
            return type;
        }
#endif

#if !STANDALONE_BUILD
        /// <summary/>
        [System.CLSCompliant(false)]
        public static FileStream Untrust(FileStreamSW stream)
        {
            return stream.InnerObject;
        }
#else
        /// <summary/>
        [System.CLSCompliant(false)]
        public static FileStream Untrust(FileStream stream)
        {
            return stream;
        }
#endif

#if !STANDALONE_BUILD
        /// <summary/>
        [System.CLSCompliant(false)]
        public static HwndSource Untrust(HwndSourceSW source)
        {
            return source.InnerObject;
        }
#else
        /// <summary/>
        [System.CLSCompliant(false)]
        public static HwndSource Untrust(HwndSource source)
        {
            return source;
        }
#endif

#if !STANDALONE_BUILD
        /// <summary/>
        [System.CLSCompliant(false)]
        public static Window Untrust(WindowSW window)
        {
            return window.InnerObject;
        }
#else
        /// <summary/>
        [System.CLSCompliant(false)]
        public static Window Untrust(Window window)
        {
            return window;
        }
#endif
    }
}
