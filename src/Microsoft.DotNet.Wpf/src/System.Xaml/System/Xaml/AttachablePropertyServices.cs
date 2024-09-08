// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Xaml
{
    public static class AttachablePropertyServices
    {
        static DefaultAttachedPropertyStore attachedProperties = new DefaultAttachedPropertyStore();

        public static int GetAttachedPropertyCount(object instance)
        {
            if (instance == null)
            {
                return 0;
            }

            IAttachedPropertyStore ap = instance as IAttachedPropertyStore;
            if (ap != null)
            {
                return ap.PropertyCount;
            }

            return attachedProperties.GetPropertyCount(instance);
        }

        public static void CopyPropertiesTo(object instance, KeyValuePair<AttachableMemberIdentifier, object>[] array, int index)
        {
            if (instance == null)
            {
                return;
            }

            IAttachedPropertyStore ap = instance as IAttachedPropertyStore;
            if (ap != null)
            {
                ap.CopyPropertiesTo(array, index);
            }
            else
            {
                attachedProperties.CopyPropertiesTo(instance, array, index);
            }
        }

        public static bool RemoveProperty(object instance, AttachableMemberIdentifier name)
        {
            if (instance == null)
            {
                return false;
            }

            IAttachedPropertyStore ap = instance as IAttachedPropertyStore;
            if (ap != null)
            {
                return ap.RemoveProperty(name);
            }

            return attachedProperties.RemoveProperty(instance, name);
        }

        public static void SetProperty(object instance, AttachableMemberIdentifier name, object value)
        {
            if (instance == null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(name);

            IAttachedPropertyStore ap = instance as IAttachedPropertyStore;
            if (ap != null)
            {
                ap.SetProperty(name, value);
                return;
            }

            attachedProperties.SetProperty(instance, name, value);
        }

        [SuppressMessage("Microsoft.Design", "CA1007")]
        public static bool TryGetProperty(object instance, AttachableMemberIdentifier name, out object value)
        {
            return TryGetProperty<object>(instance, name, out value);
        }

        public static bool TryGetProperty<T>(object instance, AttachableMemberIdentifier name, out T value)
        {
            if (instance == null)
            {
                value = default(T);
                return false;
            }

            IAttachedPropertyStore ap = instance as IAttachedPropertyStore;
            if (ap != null)
            {
                object obj;
                bool result = ap.TryGetProperty(name, out obj);
                if (result)
                {
                    if (obj is T)
                    {
                        value = (T)obj;
                        return true;
                    }
                }
                value = default(T);
                return false;
            }

            return attachedProperties.TryGetProperty(instance, name, out value);
        }

        // DefaultAttachedPropertyStore is used by the global AttachedPropertyServices to implement
        // global attached properties for types which don't implement IAttachedProperties or DO/Dependency Property
        // integration for their attached properties.

        sealed class DefaultAttachedPropertyStore
        {
            Lazy<ConditionalWeakTable<object, Dictionary<AttachableMemberIdentifier, object>>> instanceStorage =
                new Lazy<ConditionalWeakTable<object, Dictionary<AttachableMemberIdentifier, object>>>();

            public void CopyPropertiesTo(object instance, KeyValuePair<AttachableMemberIdentifier, object>[] array, int index)
            {
                if (instanceStorage.IsValueCreated)
                {
                    Dictionary<AttachableMemberIdentifier, object> instanceProperties;
                    if (instanceStorage.Value.TryGetValue(instance, out instanceProperties))
                    {
                        lock (instanceProperties)
                        {
                            ((ICollection<KeyValuePair<AttachableMemberIdentifier, object>>)instanceProperties).CopyTo(array, index);
                        }
                    }
                }
            }

            public int GetPropertyCount(object instance)
            {
                if (instanceStorage.IsValueCreated)
                {
                    Dictionary<AttachableMemberIdentifier, object> instanceProperties;
                    if (instanceStorage.Value.TryGetValue(instance, out instanceProperties))
                    {
                        lock (instanceProperties)
                        {
                            return instanceProperties.Count;
                        }
                    }
                }
                return 0;
            }

            // <summary>
            // Remove the property 'name'. If the property doesn't exist it returns false.
            // </summary>
            public bool RemoveProperty(object instance, AttachableMemberIdentifier name)
            {
                if (instanceStorage.IsValueCreated)
                {
                    Dictionary<AttachableMemberIdentifier, object> instanceProperties;
                    if (instanceStorage.Value.TryGetValue(instance, out instanceProperties))
                    {
                        lock (instanceProperties)
                        {
                            return instanceProperties.Remove(name);
                        }
                    }
                }
                return false;
            }

            // <summary>
            // Set the property 'name' value to 'value', if the property doesn't currently exist this will add the property
            // </summary>
            public void SetProperty(object instance, AttachableMemberIdentifier name, object value)
            {
                Dictionary<AttachableMemberIdentifier, object> instanceProperties;
                if (!instanceStorage.Value.TryGetValue(instance, out instanceProperties))
                {
                    instanceProperties = new Dictionary<AttachableMemberIdentifier, object>();
                    //
                    // Workaround lack of TryAdd for ConditionalWeakTable
                    try
                    {
                        instanceStorage.Value.Add(instance, instanceProperties);
                    }
                    catch (ArgumentException)
                    {
                        //
                        // If Add fails we raced and the item should exist
                        if (!instanceStorage.Value.TryGetValue(instanceStorage, out instanceProperties))
                        {
                            //
                            // If for some reason it doesn't, throw.
                            throw new InvalidOperationException(SR.DefaultAttachablePropertyStoreCannotAddInstance);
                        }
                    }
                }

                lock (instanceProperties)
                {
                    instanceProperties[name] = value;
                }
            }

            // <summary>
            // Retrieve the value of the attached property 'name'. If there is not attached property then return false.
            // </summary>
            public bool TryGetProperty<T>(object instance, AttachableMemberIdentifier name, out T value)
            {
                if (instanceStorage.IsValueCreated)
                {
                    Dictionary<AttachableMemberIdentifier, object> instanceProperties;
                    if (instanceStorage.Value.TryGetValue(instance, out instanceProperties))
                    {
                        lock (instanceProperties)
                        {
                            object valueAsObj;
                            if (instanceProperties.TryGetValue(name, out valueAsObj) &&
                                valueAsObj is T)
                            {
                                value = (T)valueAsObj;
                                return true;
                            }
                        }
                    }
                }
                value = default(T);
                return false;
            }
        }
    }
}
