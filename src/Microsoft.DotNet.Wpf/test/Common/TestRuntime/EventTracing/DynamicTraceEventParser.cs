// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Security;
using System.Xml;
using Microsoft.Test.EventTracing.FastSerialization;

namespace Microsoft.Test.EventTracing
{
    [SecuritySafeCritical, SecurityCritical]
    [CLSCompliant(false)]
    public sealed class DynamicTraceEventParser : TraceEventParser
    {
        public DynamicTraceEventParser(TraceEventSource source)
            : base(source)
        {
            if (source == null)         // Happens during deserialization.  
                return;

            // Try to retieve persisted state 
            state = (DynamicTraceEventParserState)StateObject;
            if (state == null)
            {
                StateObject = state = new DynamicTraceEventParserState();
                dynamicManifests = new Dictionary<Guid, DynamicManifestInfo>();

                this.source.RegisterUnhandledEvent(delegate(TraceEvent data)
                {
                    if (data.Opcode != (TraceEventOpcode)0xFE)
                        return;
                    if (data.ID != 0 && (byte)data.ID != 0xFE)    // Zero is for classic ETW.  
                        return;

                    // Look up our information. 
                    DynamicManifestInfo dynamicManifest;
                    if (!dynamicManifests.TryGetValue(data.ProviderGuid, out dynamicManifest))
                    {
                        dynamicManifest = new DynamicManifestInfo();
                        dynamicManifests.Add(data.ProviderGuid, dynamicManifest);
                    }

                    ProviderManifest provider = dynamicManifest.AddChunk(data);
                    // We have a completed manifest, add it to our list.  
                    if (provider != null)
                        AddProvider(provider);
                });
            }
            else if (allCallbackCalled)
            {
                foreach (ProviderManifest provider in state.providers.Values)
                    provider.AddProviderEvents(source, allCallback);
            }
        }

        public override event Action<TraceEvent> All
        {
            add
            {
                if (state != null)
                {
                    foreach (ProviderManifest provider in state.providers.Values)
                        provider.AddProviderEvents(source, value);
                }
                if (value != null)
                    allCallback += value;
                allCallbackCalled = true;
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        #region private
        private class DynamicManifestInfo
        {
            internal DynamicManifestInfo() { }

            byte[][] Chunks;
            int ChunksLeft;
            ProviderManifest provider;
            byte majorVersion;
            byte minorVersion;
            ManifestEnvelope.ManifestFormats format;

            internal unsafe ProviderManifest AddChunk(TraceEvent data)
            {
                if (provider != null)
                    return null;

                if (data.EventDataLength <= sizeof(ManifestEnvelope) || data.GetByteAt(3) != 0x5B)  // magic number 
                    return null;

                ushort totalChunks = (ushort)data.GetInt16At(4);
                ushort chunkNum = (ushort)data.GetInt16At(6);
                if (chunkNum >= totalChunks || totalChunks == 0)
                    return null;

                if (Chunks == null)
                {
                    format = (ManifestEnvelope.ManifestFormats)data.GetByteAt(0);
                    majorVersion = (byte)data.GetByteAt(1);
                    minorVersion = (byte)data.GetByteAt(2);
                    ChunksLeft = totalChunks;
                    Chunks = new byte[ChunksLeft][];
                }
                else
                {
                    // Chunks have to agree with the format and version information. 
                    if (format != (ManifestEnvelope.ManifestFormats)data.GetByteAt(0) ||
                        majorVersion != data.GetByteAt(1) || minorVersion == data.GetByteAt(2))
                        return null;
                }

                if (Chunks[chunkNum] != null)
                    return null;

                byte[] chunk = new byte[data.EventDataLength - 8];
                Chunks[chunkNum] = data.EventData(chunk, 0, 8, chunk.Length);
                --ChunksLeft;
                if (ChunksLeft > 0)
                    return null;

                // OK we have a complete set of chunks
                byte[] serializedData = Chunks[0];
                if (Chunks.Length > 1)
                {
                    int totalLength = 0;
                    for (int i = 0; i < Chunks.Length; i++)
                        totalLength += Chunks[i].Length;

                    // Concatinate all the arrays. 
                    serializedData = new byte[totalLength];
                    int pos = 0;
                    for (int i = 0; i < Chunks.Length; i++)
                    {
                        Array.Copy(Chunks[i], 0, serializedData, pos, Chunks[i].Length);
                        pos += Chunks[i].Length;
                    }
                }
                Chunks = null;
                // string str = Encoding.UTF8.GetString(serializedData);
                provider = new ProviderManifest(serializedData, format, majorVersion, minorVersion);
                return provider;
            }
        }

        private void AddProvider(ProviderManifest provider)
        {
            // If someone as asked for callbacks on every event, then include these too. 
            if (allCallbackCalled && state.providers.ContainsKey(provider.Guid))
                provider.AddProviderEvents(source, allCallback);

            // Remember this serialized information.
            state.providers[provider.Guid] = provider;
        }

        DynamicTraceEventParserState state;
        private Dictionary<Guid, DynamicManifestInfo> dynamicManifests;
        Action<TraceEvent> allCallback;
        bool allCallbackCalled;
        #endregion
    }

    class DynamicTraceEventData : TraceEvent
    {
        internal DynamicTraceEventData(Action<TraceEvent> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            Action = action;
        }

        internal event Action<TraceEvent> Action;
        protected internal override void Dispatch()
        {
            if (Action != null)
            {
                Action(this);
            }
        }
        public override string[] PayloadNames
        {
            get { Debug.Assert(payloadNames != null); return payloadNames; }
        }
        public override object PayloadValue(int index)
        {
            int offset = payloadFetches[index].offset;
            if (offset < 0)
                offset = SkipToField(index);
            Type type = payloadFetches[index].type;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return GetUnicodeStringAt(offset);
                case TypeCode.Byte:
                    return (byte)GetByteAt(offset);
                case TypeCode.SByte:
                    return (SByte)GetByteAt(offset);
                case TypeCode.Int16:
                    return GetInt16At(offset);
                case TypeCode.UInt16:
                    return (UInt16)GetInt16At(offset);
                case TypeCode.Int32:
                    return GetInt32At(offset);
                case TypeCode.UInt32:
                    return (UInt32)GetInt32At(offset);
                case TypeCode.Int64:
                    return GetInt64At(offset);
                case TypeCode.UInt64:
                    return (UInt64)GetInt64At(offset);
                default:
                    if (type == typeof(Guid))
                        return GetGuidAt(offset);
                    throw new Exception("Unsupported type " + payloadFetches[index].type);
            }
        }

        private int SkipToField(int index)
        {
            // Find the first field that has a fixed offset. 
            int offset = 0;
            int cur = index;
            while (0 < cur)
            {
                --cur;
                offset = payloadFetches[cur].offset;
                if (offset >= 0)
                    break;
            }

            // TODO is probably does pay to remember the offsets in a particular instance, since otherwise the
            // algorithm is N*N
            while (cur < index)
            {
                int size = SizeOfType(payloadFetches[cur].type);
                if (size < 0)
                {
                    if (payloadFetches[cur].type == typeof(string))
                        offset = SkipUnicodeString(offset);
                    else
                        throw new Exception("Unexpected type " + payloadFetches[cur].type.Name + " encountered.");
                }
                else
                    offset += size;
                cur++;
            }
            return offset;
        }

        internal static int SizeOfType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return int.MinValue;
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return 1;
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    return 2;
                case TypeCode.UInt32:
                case TypeCode.Int32:
                    return 4;
                case TypeCode.UInt64:
                case TypeCode.Int64:
                    return 8;
                default:
                    if (type == typeof(Guid))
                        return 16;
                    throw new Exception("Unsupported type " + type.Name); // TODO FIX NOW;
            }
        }

        internal struct PayloadFetch
        {
            public PayloadFetch(int offset, Type type)
            {
                this.offset = offset;
                this.type = type;
            }
            public int offset;
            public Type type;
        };
        internal PayloadFetch[] payloadFetches;
    }

    class DynamicTraceEventParserState : IFastSerializable
    {
        public DynamicTraceEventParserState() { providers = new Dictionary<Guid, ProviderManifest>(); }

        internal Dictionary<Guid, ProviderManifest> providers;

        #region IFastSerializable Members

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(providers.Count);
            foreach (ProviderManifest provider in providers.Values)
                serializer.Write(provider);
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            int count;
            deserializer.Read(out count);
            for (int i = 0; i < count; i++)
            {
                ProviderManifest provider;
                deserializer.Read(out provider);
                providers.Add(provider.Guid, provider);
            }
        }

        #endregion
    }

    class ProviderManifest : IFastSerializable
    {
        public ProviderManifest(byte[] serializedManifest, ManifestEnvelope.ManifestFormats format, byte majorVersion, byte minorVersion)
        {
            this.serializedManifest = serializedManifest;
        }

        public string Name { get { if (!inited) Init(); return name; } }
        public Guid Guid { get { if (!inited) Init(); return guid; } }
        public void AddProviderEvents(ITraceParserServices source, Action<TraceEvent> callback)
        {
            if (error != null)
                return;
            if (!inited)
                Init();
            try
            {
                Dictionary<string, int> opcodes = new Dictionary<string, int>();
                opcodes.Add("win:Info", 0);
                opcodes.Add("win:Start", 1);
                opcodes.Add("win:Stop", 2);
                opcodes.Add("win:DC_Start", 3);
                opcodes.Add("win:DC_End", 4);
                opcodes.Add("win:Extension", 5);
                opcodes.Add("win:Reply", 6);
                opcodes.Add("win:Resume", 7);
                opcodes.Add("win:Suspend", 8);
                opcodes.Add("win:Send", 9);
                opcodes.Add("win:Receive", 240);
                Dictionary<string, TaskInfo> tasks = new Dictionary<string, TaskInfo>();
                Dictionary<string, DynamicTraceEventData> templates = new Dictionary<string, DynamicTraceEventData>();
                while (reader.Read())
                {
                    // TODO I currently require opcodes,and tasks BEFORE events BEFORE templates.  
                    // Can be fixed by going multi-pass. 
                    switch (reader.Name)
                    {
                        case "event":
                            {
                                int taskNum = 0;
                                Guid taskGuid = Guid;
                                string taskName = reader.GetAttribute("task");
                                if (taskName != null)
                                {
                                    TaskInfo taskInfo;
                                    if (tasks.TryGetValue(taskName, out taskInfo))
                                    {
                                        taskNum = taskInfo.id;
                                        taskGuid = taskInfo.guid;
                                    }
                                }
                                else
                                    taskName = "";

                                int opcode = 0;
                                string opcodeName = reader.GetAttribute("opcode");
                                if (opcodeName != null)
                                {
                                    opcode = opcodes[opcodeName];
                                    // Strip off any namespace prefix (TODO is this a good idea?
                                    int colon = opcodeName.IndexOf(':');
                                    if (colon >= 0)
                                        opcodeName = opcodeName.Substring(colon + 1);
                                }

                                int eventID = int.Parse(reader.GetAttribute("value"));

                                DynamicTraceEventData eventTemplate = new DynamicTraceEventData(
                                callback, eventID, taskNum, taskName, taskGuid, opcode, opcodeName, Guid, Name);

                                string templateName = reader.GetAttribute("template");
                                if (templateName != null)
                                    templates[templateName] = eventTemplate;
                                else
                                {
                                    eventTemplate.payloadNames = new string[0];
                                    eventTemplate.payloadFetches = new DynamicTraceEventData.PayloadFetch[0];
                                    source.RegisterEventTemplate(eventTemplate);
                                }
                            } break;
                        case "template":
                            {
                                string templateName = reader.GetAttribute("tid");
                                Debug.Assert(templateName != null);
                                DynamicTraceEventData eventTemplate = templates[templateName];
                                ComputeFieldInfo(eventTemplate, reader.ReadSubtree());
                                source.RegisterEventTemplate(eventTemplate);
                                templates.Remove(templateName);
                            } break;
                        case "opcode":
                            opcodes.Add(reader.GetAttribute("name"), int.Parse(reader.GetAttribute("value")));
                            break;
                        case "task":
                            {
                                TaskInfo info = new TaskInfo();
                                info.id = int.Parse(reader.GetAttribute("value"));
                                string guidString = reader.GetAttribute("eventGUID");
                                if (guidString != null)
                                    info.guid = new Guid(guidString);
                                tasks.Add(reader.GetAttribute("name"), info);
                            } break;
                    }
                }

                // TODO Register any events with undefined templates as having empty payloads (can rip out after 1/2009)
                foreach (DynamicTraceEventData eventTemplate in templates.Values)
                {
                    eventTemplate.payloadNames = new string[0];
                    eventTemplate.payloadFetches = new DynamicTraceEventData.PayloadFetch[0];
                    source.RegisterEventTemplate(eventTemplate);
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Exception during manifest parsing");
                Console.WriteLine("Error: Exception during processing, symbolic information not available");
                error = e;
            }
            inited = false;     // If we call it again, start over from the begining.  
        }


        #region private
        private class TaskInfo
        {
            public int id;
            public Guid guid;
        };

        private static void ComputeFieldInfo(DynamicTraceEventData template, XmlReader reader)
        {
            List<string> payloadNames = new List<string>();
            List<DynamicTraceEventData.PayloadFetch> payloadFetches = new List<DynamicTraceEventData.PayloadFetch>();
            int offset = 0;
            while (reader.Read())
            {
                if (reader.Name == "data")
                {
                    Type type = GetTypeForManifestTypeName(reader.GetAttribute("inType"));
                    payloadNames.Add(reader.GetAttribute("name"));
                    payloadFetches.Add(new DynamicTraceEventData.PayloadFetch(offset, type));
                    if (offset >= 0)
                    {
                        int size = DynamicTraceEventData.SizeOfType(type);
                        Debug.Assert(size != 0);
                        if (size >= 0)
                            offset += size;
                        else
                            offset = int.MinValue;
                    }
                }
            }
            template.payloadNames = payloadNames.ToArray();
            template.payloadFetches = payloadFetches.ToArray();
        }

        private static Type GetTypeForManifestTypeName(string manifestTypeName)
        {
            switch (manifestTypeName)
            {
                // TODO do we want to support unsigned?
                case "win:Pointer":
                case "trace:SizeT":
                    return typeof(IntPtr);
                case "win:Boolean":
                    return typeof(bool);
                case "win:UInt8":
                case "win:Int8":
                    return typeof(byte);
                case "win:UInt16":
                case "win:Int16":
                case "trace:Port":
                    return typeof(short);
                case "win:UInt32":
                case "win:Int32":
                case "trace:IPAddr":
                case "trace:IPAddrV4":
                    return typeof(int);
                case "trace:WmiTime":
                case "win:UInt64":
                case "win:Int64":
                    return typeof(long);
                case "win:UnicodeString":
                    return typeof(string);
                case "win:GUID":
                    return typeof(Guid);
                default:
                    throw new Exception("Unsupported type " + manifestTypeName);
            }
        }

        #region IFastSerializable Members

        void IFastSerializable.ToStream(Serializer serializer)
        {
            int count = 0;
            if (serializedManifest != null)
                count = serializedManifest.Length;
            serializer.Write(count);
            for (int i = 0; i < count; i++)
                serializer.Write(serializedManifest[i]);
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            int count = deserializer.ReadInt();
            serializedManifest = new byte[count];
            for (int i = 0; i < count; i++)
                serializedManifest[i] = deserializer.ReadByte();
            Init();
        }

        private void Init()
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreComments = true;
                settings.IgnoreWhitespace = true;
                System.IO.MemoryStream stream = new System.IO.MemoryStream(serializedManifest);
                reader = XmlReader.Create(stream, settings);
                if (reader.Read() && reader.Name == "provider")
                {
                    guid = new Guid(reader.GetAttribute("guid"));
                    name = reader.GetAttribute("name");
                    fileName = reader.GetAttribute("resourceFileName");
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Exception during manifest parsing");
                error = e;
            }
            inited = true;
        }

        #endregion
        private XmlReader reader;
        private byte[] serializedManifest;
        private Guid guid;
        private string name;
        private string fileName;
        private bool inited;
        private Exception error;

        #endregion
    }
}
