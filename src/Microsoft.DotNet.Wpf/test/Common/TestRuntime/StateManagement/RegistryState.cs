// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.Security.Principal;
using System.Xml;

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// Registry State
    /// </summary>
    public class RegistryState : State<RegistryStateValue, object>
    {
        #region Private Data

        private string keyName;
        private string valueName;

        #endregion

        #region Constructors

        /// <summary/>
        public RegistryState()
            : base()
        {
            //Needed for serialization
        }

        /// <summary>
        /// Initializes a registry state given a key and value name
        /// </summary>
        /// <param name="userSid">Security Identifier of the user making the registry key change. This value can be null.</param>
        /// <param name="keyName">Key to track, with format "HKEY_CURRENT_USER\SOFTWARE"</param>
        /// <param name="valueName">Name of value to track. For the default value, set to string.empty</param>
        public RegistryState(string userSid, string keyName, string valueName)
            : base()
        {
            if (string.IsNullOrEmpty(keyName))
                throw new ArgumentNullException(@"Key Name must be specified using format HKEY_CURRENT_USER\SOFTWARE\...");
            if (valueName == null)
                throw new ArgumentNullException("Value Name must not be null. For default, use string.empty");

            if (!String.IsNullOrEmpty(userSid) && !String.IsNullOrEmpty(keyName))
            {
                SecurityIdentifier sid = new SecurityIdentifier(userSid);
                if (!sid.IsAccountSid())
                    throw new ArgumentException("The sid must be from a valid user account");

                if (keyName.Contains("HKCU"))
                    keyName = keyName.Replace("HKCU", @"HKEY_USERS\" + userSid);
                else if (keyName.Contains("HKEY_CURRENT_USER"))
                    keyName = keyName.Replace("HKEY_CURRENT_USER", @"HKEY_USERS\" + userSid);
            }

            this.keyName = keyName;
            this.valueName = valueName;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Name of the key
        /// </summary>
        public String KeyName
        {
            get { return keyName; }
            set { keyName = value; }
        }

        /// <summary>
        /// Name of the value
        /// </summary>
        public String ValueName
        {
            get { return valueName; }
            set { valueName = value; }
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Returns the value given the key and value name
        /// </summary>
        /// <returns></returns>
        public override RegistryStateValue GetValue()
        {
            return new RegistryStateValue(Registry.GetValue(keyName, valueName, null), GetRegistryValueKind(keyName, valueName));
        }

        /// <summary>
        /// Sets a value for the given key and value name
        /// </summary>
        /// <param name="value"></param>
        /// <param name="action">Action to perform when setting the value</param>
        public override bool SetValue(RegistryStateValue value, object action)
        {
            if (!object.Equals(value.Value, null))
                Registry.SetValue(keyName, valueName, value.Value, value.ValueKind);
            else
                DeleteRegistryValue(keyName, valueName);

            return true;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            RegistryState otherRegistryState = obj as RegistryState;
            if (otherRegistryState == null)
                return false;

            return (String.Equals(keyName, otherRegistryState.keyName, StringComparison.InvariantCultureIgnoreCase) &&
                    String.Equals(valueName, otherRegistryState.valueName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Private Members

        //Detects and returns the value kind for a registry key/value. Returns Unknown if key or value don't exist
        private static RegistryValueKind GetRegistryValueKind(string keyName, string valueName)
        {
            RegistryValueKind registryValueKind = RegistryValueKind.Unknown;

            using (RegistryKey key = GetRegistryKey(keyName))
            {
                if (key != null && key.GetValue(valueName, null) != null)
                    registryValueKind = key.GetValueKind(valueName);
            }

            return registryValueKind;
        }

        internal static void DeleteRegistryValue(string keyName, string valueName)
        {
            string subKey = null;

            using (RegistryKey baseKey = GetBaseRegistryKey(keyName, out subKey))
            {
                using (RegistryKey key = GetRegistryKey(keyName))
                {
                    // If this key already doesn't exist, we shouldn't try to delete it.
                    if (key != null)
                        key.DeleteValue(valueName, false);                    
                }
            }
        }

        //Opens a registry key or returns null of the key doesn't exist
        private static RegistryKey GetRegistryKey(string keyName)
        {
            string subKey = null;
            RegistryKey key = null;

            using (RegistryKey baseKey = GetBaseRegistryKey(keyName, out subKey))
            {
                //No point in trying to open a null base key or subkey name
                if (baseKey != null && subKey != null)
                    key = baseKey.OpenSubKey(subKey, true);
            }

            return key;
        }

        private static RegistryKey GetBaseRegistryKey(string keyName, out string subKey)
        {
            subKey = null;
            object[] args = new object[] { keyName, null };

            RegistryKey baseKey = (RegistryKey)typeof(Registry).InvokeMember("GetBaseKeyFromKeyName", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, args);

            if (baseKey == null)
                subKey = null;
            else
                subKey = args[1] as string;

            return baseKey;
        }

        #endregion
    }

    /// <summary>
    /// Value info for registry state
    /// </summary>
    public class RegistryStateValue
    {
        private object value;
        private RegistryValueKind valueKind;

        #region Constructor

        /// <summary/>
        public RegistryStateValue()
            : this(null, RegistryValueKind.Unknown)
        {
            //Needed for serialization
        }

        /// <summary>
        /// Creates a registry value based on the value type
        /// </summary>
        /// <param name="value"></param>
        public RegistryStateValue(object value)
            : this(value, RegistryValueKind.Unknown)
        {
        }

        /// <summary>
        /// Creates a registry value based on the specified value kind
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueKind"></param>
        public RegistryStateValue(object value, RegistryValueKind valueKind)
        {
            this.value = value;
            this.valueKind = valueKind;
        }
        #endregion

        #region Public Members

        /// <summary>
        /// Registry value 
        /// </summary>
        public object Value
        {
            get { return this.value;  }
            set { this.value = value; }
        }
        /// <summary>
        /// Type for this value.  Needed to be able to set expandstring.
        /// </summary>
        public RegistryValueKind ValueKind
        {
            get  { return this.valueKind; }
            set  { this.valueKind = value; }
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            RegistryStateValue other = obj as RegistryStateValue;
            if (object.Equals(other, null))
                return false;
            if (!object.Equals(other.value, value))
                return false;

            //Special case unknown
            if (other.valueKind == RegistryValueKind.Unknown)
                return true;
            if (valueKind == RegistryValueKind.Unknown)
                return true;

            return other.valueKind == valueKind;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            //Omitting kind because we special case it in equal
            if (value == null)
                return 0;

            return value.GetHashCode();
        }

        /// <summary/>
        public static bool operator ==(RegistryStateValue x, RegistryStateValue y)
        {
            if (object.Equals(x, null))
                return object.Equals(y, null);
            else
                return x.Equals(y);
        }

        /// <summary/>
        public static bool operator !=(RegistryStateValue x, RegistryStateValue y)
        {
            if (object.Equals(x, null))
                return !object.Equals(y, null);
            else
                return !x.Equals(y);
        }

        #endregion
    }


}
