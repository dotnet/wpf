// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace WinRT
{
    internal static class GuidGenerator
    {
        public static Guid GetGUID(Type type)
        {
            return type.GetGuidType().GUID;
        }

        public static Guid GetIID(Type type)
        {
            type = type.GetGuidType();
            if (!type.IsGenericType)
            {
                return type.GUID;
            }
            return (Guid)type.GetField("PIID").GetValue(null);
        }

        public static string GetSignature(Type type)
        {
            var helperType = type.FindHelperType();
            if (helperType != null)
            {
                var sigMethod = helperType.GetMethod("GetGuidSignature", BindingFlags.Static | BindingFlags.Public);
                if (sigMethod != null)
                {
                    return (string)sigMethod.Invoke(null, new Type[] { });
                }
            }

            if (type == typeof(object))
            {
                return "cinterface(IInspectable)";
            }

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments().Select(t => GetSignature(t));
                return "pinterface({" + GetGUID(type) + "};" + String.Join(";", args) + ")";
            }

            if (type.IsValueType)
            {
                switch (type.Name)
                {
                    case "SByte": return "i1";
                    case "Byte": return "u1";
                    case "Int16": return "i2";
                    case "UInt16": return "u2";
                    case "Int32": return "i4";
                    case "UInt32": return "u4";
                    case "Int64": return "i8";
                    case "UInt64": return "u8";
                    case "Single": return "f4";
                    case "Double": return "f8";
                    case "Boolean": return "b1";
                    case "Char": return "c2";
                    case "Guid": return "g16";
                    default:
                        {
                            if (type.IsEnum)
                            {
                                var isFlags = type.CustomAttributes.Any(cad => cad.AttributeType == typeof(FlagsAttribute));
                                return "enum(" + TypeExtensions.RemoveNamespacePrefix(type.FullName) + ";" + (isFlags ? "u4" : "i4") + ")";
                            }
                            if (!type.IsPrimitive)
                            {
                                var args = type.GetFields(BindingFlags.Instance | BindingFlags.Public).Select(fi => GetSignature(fi.FieldType));
                                return "struct(" + TypeExtensions.RemoveNamespacePrefix(type.FullName) + ";" + String.Join(";", args) + ")";
                            }
                            throw new InvalidOperationException("unsupported value type");
                        }
                }
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (Projections.TryGetDefaultInterfaceTypeForRuntimeClassType(type, out Type iface))
            {
                return "rc(" + TypeExtensions.RemoveNamespacePrefix(type.FullName) + ";" + GetSignature(iface) + ")";
            }

            if (type.IsDelegate())
            {
                return "delegate({" + GetGUID(type) + "})";
            }

            return "{" + type.GUID.ToString() + "}";
        }

        private static Guid encode_guid(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
            {
                // swap bytes of int a
                byte t = data[0];
                data[0] = data[3];
                data[3] = t;
                t = data[1];
                data[1] = data[2];
                data[2] = t;
                // swap bytes of short b
                t = data[4];
                data[4] = data[5];
                data[5] = t;
                // swap bytes of short c and encode rfc time/version field
                t = data[6];
                data[6] = data[7];
                data[7] = (byte)((t & 0x0f) | (5 << 4));
                // encode rfc clock/reserved field
                data[8] = (byte)((data[8] & 0x3f) | 0x80);
            }
            return new Guid(data.Take(16).ToArray());
        }

        private static Guid wrt_pinterface_namespace = new Guid("d57af411-737b-c042-abae-878b1e16adee");

        public static Guid CreateIID(Type type)
        {
            var sig = GetSignature(type);
            if (!type.IsGenericType)
            {
                return new Guid(sig);
            }
            var data = wrt_pinterface_namespace.ToByteArray().Concat(UTF8Encoding.UTF8.GetBytes(sig)).ToArray();
            return encode_guid(SHA1.HashData(data));
        }
    }
}
