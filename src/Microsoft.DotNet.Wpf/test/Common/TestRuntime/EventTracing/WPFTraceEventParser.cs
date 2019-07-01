// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using Microsoft.Test.EventTracing.FastSerialization;

namespace Microsoft.Test.EventTracing
{
    [CLSCompliant(false)] 
    public sealed class WPFTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "Windows-Presentation-Framework";
        public static Guid WPFCrimsonProviderGuid = new Guid("E13B77A8-14B6-11DE-8069-001B212B5009");
        public static Guid WPFClassicProviderGuid = new Guid("a42c77db-874f-422e-9b44-6d89fe2bd3e5");
        public WPFTraceEventParser(TraceEventSource source) : base(source) { }
#if TESTBUILD_CLR20
        public static Guid ProviderGuid = WPFClassicProviderGuid;
#endif
#if TESTBUILD_CLR40
        public static Guid ProviderGuid = WPFCrimsonProviderGuid;
#endif
        public event Action<EmptyTraceData> FirstRaster
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "FirstRaster", FirstRasterGuid, 0, "", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> ApplicationStartup
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "ApplicationStartup", ApplicationStartupGuid, 0, "", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> ControlStartup
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "ControlStartup", ControlStartupGuid, 0, "", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> MediaRenderStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "MediaRender", MediaRenderGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> MediaRenderStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "MediaRender", MediaRenderGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> ParseStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "Parse", ParseGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> ParseStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "Parse", ParseGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> RasterStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "Raster", RasterGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> RasterStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "Raster", RasterGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> PutSourceStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "PutSource", PutSourceGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> PutSourceStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "PutSource", PutSourceGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> CLRStartupStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "CLRStartup", CLRStartupGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> CLRStartupStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "CLRStartup", CLRStartupGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> CoreDrawStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "CoreDraw", CoreDrawGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> CoreDrawStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "CoreDraw", CoreDrawGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> DownloadRequestStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "DownloadRequest", DownloadRequestGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> DownloadRequestStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "DownloadRequest", DownloadRequestGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> PutRootVisualStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "PutRootVisual", PutRootVisualGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> PutRootVisualStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "PutRootVisual", PutRootVisualGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        #region private
        private static Guid ApplicationStartupGuid = new Guid(unchecked((int)0x3b99be34), unchecked((short)0xa702), unchecked((short)0x477a), 0xbf, 0xff, 0x3b, 0xef, 0xde, 0xfd, 0x5c, 0xe);
        private static Guid FirstRasterGuid = new Guid(unchecked((int)0x84d6a4e1), unchecked((short)0x252a), unchecked((short)0x4742), 0xb5, 0xa8, 0x8d, 0x45, 0xd2, 0xbd, 0x80, 0x1f);
        private static Guid ControlStartupGuid = new Guid(unchecked((int)0xb72aadc1), unchecked((short)0xa110), unchecked((short)0x4221), 0x8e, 0x3, 0x5, 0x42, 0x9a, 0x5b, 0xfa, 0xf9);

        private static Guid MediaRenderGuid = new Guid(unchecked((int)0x21c1ea55), unchecked((short)0x76a7), unchecked((short)0x4819), 0xb0, 0xfb, 0x7c, 0x6f, 0xb1, 0x62, 0x2b, 0xf4);
        private static Guid ParseGuid = new Guid(unchecked((int)0x9835e264), unchecked((short)0x4b9), unchecked((short)0x4ee1), 0x8f, 0xd, 0x66, 0x6c, 0x1a, 0x86, 0xea, 0x24);
        private static Guid RasterGuid = new Guid(unchecked((int)0xe26e6715), unchecked((short)0x54a), unchecked((short)0x4a6c), 0xaa, 0x60, 0x72, 0x6d, 0xd4, 0x12, 0x15, 0xfa);
        private static Guid PutSourceGuid = new Guid(unchecked((int)0xea919d73), unchecked((short)0x2960), unchecked((short)0x49e4), 0x86, 0x49, 0xf4, 0x41, 0xe0, 0x2f, 0x58, 0xcd);
        private static Guid CLRStartupGuid = new Guid(unchecked((int)0x84d1c136), unchecked((short)0x7de5), unchecked((short)0x46c9), 0xb5, 0xb7, 0x26, 0x43, 0x25, 0xc4, 0xa0, 0x1c);
        private static Guid CoreDrawGuid = new Guid(unchecked((int)0xb9460fc7), unchecked((short)0xa0c6), unchecked((short)0x4ca0), 0x8e, 0xd7, 0xbd, 0xdd, 0xd7, 0xef, 0x89, 0x21);
        private static Guid DownloadRequestGuid = new Guid(unchecked((int)0x47ebc335), unchecked((short)0xe0b7), unchecked((short)0x490f), 0x86, 0x45, 0x62, 0xc1, 0xa3, 0x26, 0x1f, 0x73);
        private static Guid PutRootVisualGuid = new Guid(unchecked((int)0xa72c6400), unchecked((short)0x7ef2), unchecked((short)0x4355), 0xb2, 0x31, 0xe3, 0x8f, 0xe4, 0xd6, 0x3a, 0x84);

        /* TODO ADD 
        private static Guid MEDIARENDERCALLBACK = new Guid(unchecked((int)0x1e8b30e8), unchecked((short)0xf14e), unchecked((short)0x4c9c), 0x90, 0xfc, 0x1b, 0x40, 0x6a, 0x41, 0x8d, 0x16);
        private static Guid MEDIADRAWFRAME = new Guid(unchecked((int)0x34142b63), unchecked((short)0xd35b), unchecked((short)0x4bce), 0x99, 0xf9, 0xd9, 0x6b, 0x45, 0x7e, 0xb2, 0xb1);
        private static Guid MEDIABUFFERING = new Guid(unchecked((int)0x56c0ff81), unchecked((short)0x58b7), unchecked((short)0x4917), 0xab, 0x7c, 0x14, 0x17, 0xf1, 0xfd, 0xf4, 0x63);
        private static Guid MEDIADROPPEDFRAME = new Guid(unchecked((int)0x5fe427ba), unchecked((short)0x300), unchecked((short)0x49a0), 0x92, 0x7e, 0x63, 0x3a, 0xc0, 0x97, 0x3e, 0x5b);
        private static Guid INFORMATION = new Guid(unchecked((int)0x9c67d5e7), unchecked((short)0x7a), unchecked((short)0x4f1f), 0x86, 0xaf, 0xa9, 0xb6, 0xbb, 0xc3, 0x7e, 0xa5);
        private static Guid CLRSHUTDOWN = new Guid(unchecked((int)0x8a537cd0), unchecked((short)0xcf1e), unchecked((short)0x4d92), 0x9c, 0x8b, 0xa0, 0x5, 0xae, 0x1d, 0xd4, 0x1);
        private static Guid CLRCONVERTVALUE = new Guid(unchecked((int)0xf40da7fe), unchecked((short)0x130f), unchecked((short)0x4c05), 0xac, 0x9d, 0xf8, 0x58, 0x5b, 0xec, 0x75, 0x4b);
        private static Guid CLROBJECTLIFETIME = new Guid(unchecked((int)0xba41b098), unchecked((short)0xeefe), unchecked((short)0x4979), 0xaf, 0xfa, 0x13, 0x69, 0x54, 0x2a, 0xca, 0xe5);
        private static Guid CONTROLSHUTDOWN = new Guid(unchecked((int)0x9a20056), unchecked((short)0x657a), unchecked((short)0x425c), 0x84, 0xd3, 0x5a, 0x88, 0x1d, 0x3a, 0xd, 0x3d);
        private static Guid CONTROLINPLACE = new Guid(unchecked((int)0xe43d3d12), unchecked((short)0x378b), unchecked((short)0x41ab), 0x86, 0xb7, 0x48, 0x62, 0x0, 0xb0, 0x8b, 0x4d);
        private static Guid NPCTRLLOADDLL = new Guid(unchecked((int)0xa72c6400), unchecked((short)0x7ef2), unchecked((short)0x4355), 0xb2, 0x31, 0xe3, 0x8f, 0xe4, 0xd6, 0x3a, 0x90);
        private static Guid WINRENDER = new Guid(unchecked((int)0x735fd9e9), unchecked((short)0xe746), unchecked((short)0x41ba), 0x91, 0xc9, 0x5e, 0xf2, 0xff, 0xa4, 0xd1, 0xff);
        private static Guid SETVALUEMANAGED = new Guid(unchecked((int)0xcc050497), unchecked((short)0xcf9a), unchecked((short)0x4dc7), 0x82, 0xbb, 0xad, 0xeb, 0x29, 0x78, 0xea, 0x7d);
        private static Guid SETVALUENATIVE = new Guid(unchecked((int)0xfc06a042), unchecked((short)0x7e58), unchecked((short)0x465e), 0x83, 0x4c, 0xa8, 0xb6, 0x79, 0x1a, 0x35, 0x51);
        private static Guid MEDIASAMPLEADDED = new Guid(unchecked((int)0x5e8e692d), unchecked((short)0x784d), unchecked((short)0x4931), 0x9a, 0x5c, 0xbb, 0xa7, 0xca, 0xcd, 0xb3, 0xd0);
        private static Guid SEADRAGONDECODE = new Guid(unchecked((int)0x7f8ec7f0), unchecked((short)0x3872), unchecked((short)0x4b59), 0xa9, 0x8c, 0x26, 0x74, 0xf0, 0x18, 0xbe, 0xc7);
        private static Guid SEADRAGONTILEDOWNLOAD = new Guid(unchecked((int)0x91529c11), unchecked((short)0x8e1e), unchecked((short)0x472b), 0xad, 0xb9, 0xb7, 0xf5, 0x20, 0x1f, 0xda, 0x7b);
        private static Guid SEADRAGONRASTERIZEFRAME = new Guid(unchecked((int)0xed955f6a), unchecked((short)0x234b), unchecked((short)0x4238), 0xaf, 0x5, 0x79, 0xe5, 0xf8, 0x3e, 0x82, 0x9c);
        private static Guid SEADRAGONBRUSHGENERATE = new Guid(unchecked((int)0xb11c850c), unchecked((short)0x4316), unchecked((short)0x4cf9), 0x9e, 0xa4, 0x76, 0xb5, 0x7d, 0x68, 0x16, 0x86);
        private static Guid SEADRAGONIMAGERESOLVED = new Guid(unchecked((int)0x4ca9935a), unchecked((short)0x9693), unchecked((short)0x416d), 0x83, 0x51, 0x8e, 0x53, 0x28, 0x91, 0x5, 0x2c);
        private static Guid DRMINDIV = new Guid(unchecked((int)0xdd4fb860), unchecked((short)0xc61a), unchecked((short)0x11dc), 0xb1, 0x91, 0x27, 0xbe, 0x56, 0xd8, 0x95, 0x93);
        private static Guid DRMLICENSEACQUISITION = new Guid(unchecked((int)0xdce394ba), unchecked((short)0x5dc0), unchecked((short)0x442d), 0x9e, 0x1c, 0xef, 0xa4, 0xf, 0xd4, 0xa, 0x53);
        private static Guid DATABINDING = new Guid(unchecked((int)0x82989716), unchecked((short)0x868b), unchecked((short)0x4572), 0xba, 0x3b, 0x48, 0x2e, 0xa8, 0xa4, 0x58, 0x7a);
        private static Guid DATAGRIDSCROLL = new Guid(unchecked((int)0xaeb50af0), unchecked((short)0x685e), unchecked((short)0x4e1c), 0x87, 0x80, 0xc4, 0x75, 0x60, 0xb1, 0xe9, 0xfc);
        **/
        #endregion
    }
}
