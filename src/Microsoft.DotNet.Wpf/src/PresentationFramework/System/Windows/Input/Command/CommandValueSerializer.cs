// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Contents:  ValueSerializer for the ICommand interface
//

namespace System.Windows.Input
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Markup;
    using System.Windows.Documents; // EditingCommands
    using System.Reflection;

    internal class CommandValueSerializer : ValueSerializer 
    {
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            if (context == null || context.GetValueSerializerFor(typeof(Type)) == null)
                return false;

            // Can only convert routed commands
            RoutedCommand command = value as RoutedCommand;

            if (command == null || command.OwnerType == null)
            {
                return false;
            }
            
            if (CommandConverter.IsKnownType(command.OwnerType))
            {
                return true;
            }
            else
            {
                string localName = command.Name + "Command";
                Type ownerType = command.OwnerType;
                string typeName = ownerType.Name;

                // Get them from Properties
                PropertyInfo propertyInfo = ownerType.GetProperty(localName, BindingFlags.Public | BindingFlags.Static);
                if (propertyInfo != null)
                    return true;

                // Get them from Fields (ScrollViewer.PageDownCommand is a static readonly field
                FieldInfo fieldInfo = ownerType.GetField(localName, BindingFlags.Static | BindingFlags.Public);
                if (fieldInfo != null)
                    return true;
            }

            return false; 
        }

        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }

        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            if (value != null)
            {
                RoutedCommand command = value as RoutedCommand;
                if (null != command && null != command.OwnerType)
                {
                    // Known Commands, so write shorter version
                    if (CommandConverter.IsKnownType(command.OwnerType))
                    {
                        return command.Name;
                    }
                    else
                    {
                        ValueSerializer typeSerializer = null;

                        if (context == null)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.ValueSerializerContextUnavailable, this.GetType().Name ));
                        }

                        // Get the ValueSerializer for the System.Type type
                        typeSerializer = context.GetValueSerializerFor(typeof(Type));
                        if (typeSerializer == null)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.TypeValueSerializerUnavailable, this.GetType().Name ));
                        }

                        return typeSerializer.ConvertToString(command.OwnerType, context) + "." + command.Name + "Command";
                    }
                }
            }
            else
                return string.Empty;
            
            throw GetConvertToException(value, typeof(string));
        }

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context)
        {
            if (value != null)
            {
                RoutedCommand command = value as RoutedCommand;
                if (command != null)
                {
                    if (command.OwnerType != null && !CommandConverter.IsKnownType(command.OwnerType))
                    {
                        return new Type[] { command.OwnerType };
                    }
                }
            }
            return base.TypeReferences(value, context);
        }

        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            if (value != null)
            {
                if (value.Length != 0)
                {
                    Type declaringType = null;
                    String commandName;

                    // Check for "ns:Class.Command" syntax.
                    
                    int dotIndex = value.IndexOf('.');
                    if (dotIndex >= 0)
                    {
                        // We have "ns:Class.Command" syntax.

                        // Find the type name in the form of "ns:Class".
                        string typeName = value.Substring(0, dotIndex);

                        if (context == null)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.ValueSerializerContextUnavailable, this.GetType().Name ));
                        }

                        // Get the ValueSerializer for the System.Type type
                        ValueSerializer typeSerializer = context.GetValueSerializerFor(typeof(Type));
                        if (typeSerializer == null)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.TypeValueSerializerUnavailable, this.GetType().Name ));
                        }


                        // Use the TypeValueSerializer to parse the "ns:Class" into a System.Type.
                        declaringType = typeSerializer.ConvertFromString(typeName, context) as Type;

                        // Strip out the "Command" part of "ns:Class.Command".
                        commandName = value.Substring(dotIndex + 1).Trim();
                    }
                    else
                    {
                        // Assume the known commands
                        commandName = value.Trim();
                    }

                    // Find the command given the declaring type & name (this is shared with CommandConverter)
                    ICommand command = CommandConverter.ConvertFromHelper( declaringType, commandName );

                    if (command != null)
                    {
                        return command;
                    }
                }
                else
                {
                    return null; // String.Empty <==> null , (for roundtrip cases where Command property values are null)
                }
            }

            return base.ConvertFromString(value, context);
        }
    }
}
