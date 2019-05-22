// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;
using System.IO;
using System.Windows.Media;
using MS.Internal;
using System.Windows.Media.Media3D;
using System.ComponentModel;

namespace System.Windows.Baml2006
{
    internal class DeferredBinaryDeserializerExtension : MarkupExtension
    {
        private IFreezeFreezables _freezer;
        private bool _canFreeze;
        private readonly BinaryReader _reader;
        private readonly Stream _stream;
        private readonly int _converterId;
        public DeferredBinaryDeserializerExtension(IFreezeFreezables freezer, BinaryReader reader, int converterId, int dataByteSize)
        {
            _freezer = freezer;
            // We need to evaluate this immediately since ProvideValue may be called much later.
            _canFreeze = freezer.FreezeFreezables;
            byte[] bytes = reader.ReadBytes(dataByteSize);
            _stream = new MemoryStream(bytes);
            _reader = new BinaryReader(_stream);
            _converterId = converterId;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            _stream.Position = 0;
            switch (_converterId)
            {
                case Baml2006SchemaContext.KnownTypes.XamlBrushSerializer:
                    return System.Windows.Media.SolidColorBrush.DeserializeFrom(_reader, 
                        new DeferredBinaryDeserializerExtensionContext(serviceProvider, _freezer, _canFreeze));
                case Baml2006SchemaContext.KnownTypes.XamlPathDataSerializer:
                    return Parsers.DeserializeStreamGeometry(_reader);
                case Baml2006SchemaContext.KnownTypes.XamlPoint3DCollectionSerializer:
                    return Point3DCollection.DeserializeFrom(_reader);
                case Baml2006SchemaContext.KnownTypes.XamlPointCollectionSerializer:
                    return PointCollection.DeserializeFrom(_reader);
                case Baml2006SchemaContext.KnownTypes.XamlVector3DCollectionSerializer:
                    return Vector3DCollection.DeserializeFrom(_reader);
                default:
                    throw new NotImplementedException();
            }
        }

        private class DeferredBinaryDeserializerExtensionContext : ITypeDescriptorContext, IFreezeFreezables
        {
            private IServiceProvider _serviceProvider;
            private IFreezeFreezables _freezer;
            private bool _canFreeze;
            public DeferredBinaryDeserializerExtensionContext(IServiceProvider serviceProvider, IFreezeFreezables freezer, bool canFreeze)
            {
                _freezer = freezer;
                _canFreeze = canFreeze;
                _serviceProvider = serviceProvider;
            }

            object IServiceProvider.GetService(Type serviceType)
            {
                if (serviceType == typeof(IFreezeFreezables))
                {
                    return this;
                }
                return _serviceProvider.GetService(serviceType);
            }

            #region ITypeDescriptorContext Methods
            // ITypeDescriptorContext derives from IServiceProvider.
            void ITypeDescriptorContext.OnComponentChanged()
            {
            }

            bool ITypeDescriptorContext.OnComponentChanging()
            {
                return false;
            }

            IContainer ITypeDescriptorContext.Container
            {
                get { return null; }
            }

            object ITypeDescriptorContext.Instance
            {
                get { return null; }
            }

            PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
            {
                get { return null; }
            }
            #endregion

            #region IFreezeFreezables Members

            bool IFreezeFreezables.FreezeFreezables
            {
                get { return _canFreeze;  }
            }

            bool IFreezeFreezables.TryFreeze(string value, Freezable freezable)
            {
                return _freezer.TryFreeze(value, freezable);
            }

            Freezable IFreezeFreezables.TryGetFreezable(string value)
            {
                return _freezer.TryGetFreezable(value);
            }

            #endregion
        }
    }
}
