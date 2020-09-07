// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xaml;
using System.Windows.Markup;
using System.Diagnostics;

namespace System.Windows.Baml2006
{
    [DebuggerDisplay("{DebuggerString}")]
    internal class KeyRecord
    {
        public KeyRecord(bool shared, bool sharedSet, int valuePosition, Type keyType) :
            this(shared, sharedSet, valuePosition)
        {
            _data = keyType;
        }

        public KeyRecord(bool shared, bool sharedSet, int valuePosition, string keyString) :
            this(shared, sharedSet, valuePosition)
        {
            _data = keyString;
        }

        public KeyRecord(bool shared, bool sharedSet, int valuePosition, XamlSchemaContext context) :
            this(shared, sharedSet, valuePosition)
        {
            _data = new XamlNodeList(context, 8);
        }

        private KeyRecord(bool shared, bool sharedSet, int valuePosition)
        {
            _shared = shared;
            _sharedSet = sharedSet;
            ValuePosition = valuePosition;
        }

        public bool Shared { get { return _shared; } }
        public bool SharedSet { get { return _sharedSet; } }
        public long ValuePosition { get; set; }
        public int ValueSize { get; set; }
        public byte Flags { get; set; }

        // This can either be a StaticResource or an OptimizedStaticResource
        // Since they don't share anything in common, we've made this a list of objects.
        public List<Object> StaticResources
        {
            get
            {
                if (_resources == null)
                {
                    _resources = new List<Object>();
                }

                return _resources;
            }
        }

        public bool HasStaticResources
        {
            get { return (_resources != null && _resources.Count > 0); }
        }

        public StaticResource LastStaticResource
        {
            get
            {
                Debug.Assert(StaticResources[StaticResources.Count - 1] is StaticResource);
                return StaticResources[StaticResources.Count - 1] as StaticResource;
            }
        }

        public string KeyString
        {
            get { return _data as String; }
        }

        public Type KeyType
        {
            get { return _data as Type; }
        }

        public XamlNodeList KeyNodeList
        {
            get
            {
                return _data as XamlNodeList;
            }
        }

        private List<Object> _resources;
        private object _data;
        bool _shared;
        bool _sharedSet;
    }

    internal class StaticResource
    {
        public StaticResource(XamlType type, XamlSchemaContext schemaContext)
        {
            ResourceNodeList = new XamlNodeList(schemaContext, 8);
            ResourceNodeList.Writer.WriteStartObject(type);
        }

        public XamlNodeList ResourceNodeList { get; private set; }
    }

    internal class OptimizedStaticResource
    {
        public OptimizedStaticResource(byte flags, short keyId)
        {
            _isType = (flags & TypeExtensionValueMask) != 0;
            _isStatic = (flags & StaticExtensionValueMask) != 0;

            KeyId = keyId;
        }

        public short KeyId { get; set; }

        public object KeyValue { get; set; }

        public bool IsKeyStaticExtension
        {
            get { return _isStatic; }
        }
        public bool IsKeyTypeExtension
        {
            get { return _isType; }
        }

        private bool _isStatic;
        private bool _isType;

        private static readonly byte TypeExtensionValueMask = 0x01;
        private static readonly byte StaticExtensionValueMask = 0x02;
    }
}
