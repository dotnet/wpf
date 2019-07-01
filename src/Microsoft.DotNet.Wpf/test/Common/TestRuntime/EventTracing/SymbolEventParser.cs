// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Test.EventTracing
{
    [CLSCompliant(false)]
    public sealed class SymbolTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "KernelTraceControl";
        public static Guid ProviderGuid = new Guid(0x28ad2447, 0x105b, 0x4fe2, 0x95, 0x99, 0xe5, 0x9b, 0x2a, 0xa9, 0xa6, 0x34);

        public SymbolTraceEventParser(TraceEventSource source)
            : base(source)
        {
        }

        public event Action<ImageIDTraceData> ImageIDTraceData
        {
            add
            {
                source.RegisterEventTemplate(new ImageIDTraceData(value, 0x0, DBGID_LOG_TYPE_IMAGEID, "ImageID", ImageIDTaskGuid, DBGID_LOG_TYPE_IMAGEID, "ImageID", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new NotImplementedException();
            }

        }
        public event Action<DbgIDRSDSData> DbgIDRSDSTraceData
        {
            add
            {
                source.RegisterEventTemplate(new DbgIDRSDSData(value, 0x0, DBGID_LOG_TYPE_RSDS, "DbgID/RSDS", ImageIDTaskGuid, DBGID_LOG_TYPE_RSDS, "DbgID/RSDS", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new NotImplementedException();
            }
        }
#if false 
        public event Action<DbgIDNB10TraceData> DbgIDNB10TraceData
        {
            add
            {
                source.RegisterEventTemplate(new DbgIDNB10TraceData(value, 0x0, DBGID_LOG_TYPE_NB10, "DbgID/NB10", ImageIDTaskGuid, DBGID_LOG_TYPE_NB10, "DbgID/NB10", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new NotImplementedException();
            }

        }
#endif
        #region Event ID Definitions
        public const int DBGID_LOG_TYPE_IMAGEID = 0x00;
        // public const int DBGID_LOG_TYPE_NONE = 0x20;
        // public const int DBGID_LOG_TYPE_BIN = 0x21;
        // public const int DBGID_LOG_TYPE_DBG = 0x22;
        // public const int DBGID_LOG_TYPE_NB10 = 0x23;
        public const int DBGID_LOG_TYPE_RSDS = 0x24;
        #endregion 
        #region Private

        internal static Guid ImageIDTaskGuid = new Guid(unchecked((int) 0xB3E675D7), 0x2554, 0x4f18, 0x83, 0x0B, 0x27, 0x62, 0x73, 0x25, 0x60, 0xDE);
        #endregion 
    }
    [CLSCompliant(false)]
    public sealed class DbgIDRSDSData : TraceEvent
    {
        public Address ImageBase { get { return GetHostPointer(0); } }
        // Seems to always be 0
        // public int ProcessID { get { return GetInt32At(HostOffset(4, 1)); } }
        public Guid GuidSig { get { return GetGuidAt(HostOffset(8, 1)); } }
        public int Age { get { return GetInt32At(HostOffset(24, 1)); } }
        public string PdbFileName { get { return GetAsciiStringAt(HostOffset(28, 1)); } }

        #region Private
        internal DbgIDRSDSData(Action<DbgIDRSDSData> action, int eventID, int task, string taskName, Guid taskGuid, int opCode, string opCodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opCode, opCodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected internal override void Dispatch()
        {
            action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(EventDataLength == SkipAsciiString(HostOffset(32, 1)));
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                {
                    payloadNames = new string[] { "ImageBase", "ProcessID", "GuidSig", "Age", "PDBFileName" };
                }
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ImageBase;
                case 1:
                    return 0;
                case 2:
                    return GuidSig;
                case 3:
                    return Age;
                case 4:
                    return PdbFileName;
                default:
                    Debug.Assert(false, "invalid index");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("ImageBase", ImageBase);
            sb.XmlAttrib("GuidSig", GuidSig);
            sb.XmlAttrib("Age", Age);
            sb.XmlAttrib("PdbFileName", PdbFileName);
            sb.Append("/>");
            return sb;
        }
        private Action<DbgIDRSDSData> action;
        #endregion
    }

    // TODO I could not see uses of this.   When is it used?
    [CLSCompliant(false)]
    public sealed class ImageIDTraceData : TraceEvent
    {
        public Address ImageBase { get { return GetHostPointer(0); } }
        public long ImageSize { get { return GetIntPtrAt(HostOffset(4, 1)); } }
        // Seems to always be 0
        // public int ProcessID { get { return GetInt32At(HostOffset(8, 2)); } }
        public int TimeDateStamp { get { return GetInt32At(HostOffset(12, 2)); } }
        public string OriginalFileName { get { return GetUnicodeStringAt(HostOffset(16, 2)); } }

        #region Private
        internal ImageIDTraceData(Action<ImageIDTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opCode, string opCodeName, Guid providerGuid, string providerName):
            base(eventID, task, taskName, taskGuid, opCode, opCodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(EventDataLength == SkipUnicodeString(HostOffset(16, 2)));
        }

        public override string[] PayloadNames
        {
            get {
                if (payloadNames == null)
                {
                    payloadNames = new string[] { "ImageBase", "ImageSize", "ProcessID", "TimeDateStamp", "OriginalFileName" };
                }
                return payloadNames;

            }
        }
        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ImageBase;                    
                case 1:
                    return ImageSize;                    
                case 2:
                    return 0;
                case 3:
                    return TimeDateStamp;
                case 4:
                    return OriginalFileName;
                default:
                    Debug.Assert(false, "bad index value");
                    return null;
            }
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("ImageBase", ImageBase);
            sb.XmlAttribHex("ImageSize", ImageSize);
            sb.XmlAttribHex("TimeDateStamp", TimeDateStamp);
            sb.XmlAttrib("OriginalFileName", OriginalFileName);
            sb.Append("/>");
            return sb;
        }

        private event Action<ImageIDTraceData> Action;        
        #endregion
    }
}
