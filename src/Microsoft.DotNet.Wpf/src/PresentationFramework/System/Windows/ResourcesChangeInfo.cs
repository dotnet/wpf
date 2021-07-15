// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   This data-structure is used
//   1. As the data that is passed around by the DescendentsWalker
//      during a resources change tree-walk.
//
//

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows
{
    /// <summary>
    ///     This is the data that is passed through the DescendentsWalker
    ///     during a resources change tree-walk.
    /// </summary>
    internal struct ResourcesChangeInfo
    {
        #region Constructors

        /// <summary>
        ///     This constructor is used for notifying changes to individual
        ///     entries in a ResourceDictionary
        /// </summary>
        internal ResourcesChangeInfo(object key)
        {
            _oldDictionaries = null;
            _newDictionaries = null;
            _key = key;
            _container = null;
            _flags = 0;
        }

        /// <summary>
        ///     This constructor is used for notifying changes in Application.Resources,
        ///     [FE/FCE].Resources, ResourceDictionary.EndInit
        /// </summary>
        internal ResourcesChangeInfo(ResourceDictionary oldDictionary, ResourceDictionary newDictionary)
        {
            _oldDictionaries = null;
            if (oldDictionary != null)
            {
                _oldDictionaries = new List<ResourceDictionary>(1);
                _oldDictionaries.Add(oldDictionary);
            }

            _newDictionaries = null;
            if (newDictionary != null)
            {
                _newDictionaries = new List<ResourceDictionary>(1);
                _newDictionaries.Add(newDictionary);
            }

            _key = null;
            _container = null;
            _flags = 0;
        }

        /// <summary>
        ///     This constructor is used for notifying changes in Style.Resources,
        ///     Template.Resources, ThemeStyle.Resources
        /// </summary>
        internal ResourcesChangeInfo(
            List<ResourceDictionary> oldDictionaries,
            List<ResourceDictionary> newDictionaries,
            bool                     isStyleResourcesChange,
            bool                     isTemplateResourcesChange,
            DependencyObject         container)
        {
            _oldDictionaries = oldDictionaries;
            _newDictionaries = newDictionaries;
            _key = null;
            _container = container;
            _flags = 0;
            IsStyleResourcesChange = isStyleResourcesChange;
            IsTemplateResourcesChange = isTemplateResourcesChange;
        }

        #endregion Constructors

        #region Operations

        /// <summary>
        ///     This is a static accessor for a ResourcesChangeInfo that is used
        ///     for theme change notifications
        /// </summary>
        internal static ResourcesChangeInfo ThemeChangeInfo
        {
            get
            {
                ResourcesChangeInfo info = new ResourcesChangeInfo();
                info.IsThemeChange = true;
                return info;
            }
        }

        /// <summary>
        ///     This is a static accessor for a ResourcesChangeInfo that is used
        ///     for tree change notifications
        /// </summary>
        internal static ResourcesChangeInfo TreeChangeInfo
        {
            get
            {
                ResourcesChangeInfo info = new ResourcesChangeInfo();
                info.IsTreeChange = true;
                return info;
            }
        }

        /// <summary>
        ///     This is a static accessor for a ResourcesChangeInfo that is used
        ///     for system colors or settings change notifications
        /// </summary>
        internal static ResourcesChangeInfo SysColorsOrSettingsChangeInfo
        {
            get
            {
                ResourcesChangeInfo info = new ResourcesChangeInfo();
                info.IsSysColorsOrSettingsChange = true;
                return info;
            }
        }

        /// <summary>
        ///     This is a static accessor for a ResourcesChangeInfo that is used
        ///     for any ResourceDictionary operations that we aren't able to provide
        ///     the precise 'key that changed' information
        /// </summary>
        internal static ResourcesChangeInfo CatastrophicDictionaryChangeInfo
        {
            get
            {
                ResourcesChangeInfo info = new ResourcesChangeInfo();
                info.IsCatastrophicDictionaryChange = true;
                return info;
            }
        }

        // This flag is used to indicate that a theme change has occured
        internal bool  IsThemeChange
        {
            get { return ReadPrivateFlag(PrivateFlags.IsThemeChange); }
            set { WritePrivateFlag(PrivateFlags.IsThemeChange, value); }
        }

        // This flag is used to indicate that a tree change has occured
        internal bool  IsTreeChange
        {
            get { return ReadPrivateFlag(PrivateFlags.IsTreeChange); }
            set { WritePrivateFlag(PrivateFlags.IsTreeChange, value); }
        }

        // This flag is used to indicate that a style has changed
        internal bool  IsStyleResourcesChange
        {
            get { return ReadPrivateFlag(PrivateFlags.IsStyleResourceChange); }
            set { WritePrivateFlag(PrivateFlags.IsStyleResourceChange, value); }
        }

        // This flag is used to indicate that this resource change was triggered from a Template change
        internal bool IsTemplateResourcesChange
        {
            get {return ReadPrivateFlag(PrivateFlags.IsTemplateResourceChange); }
            set { WritePrivateFlag(PrivateFlags.IsTemplateResourceChange, value); }
        }

        // This flag is used to indicate that a system color or settings change has occured
        internal bool IsSysColorsOrSettingsChange
        {
            get {return ReadPrivateFlag(PrivateFlags.IsSysColorsOrSettingsChange); }
            set { WritePrivateFlag(PrivateFlags.IsSysColorsOrSettingsChange, value); }
        }

        // This flag is used to indicate that a catastrophic dictionary change has occured
        internal bool IsCatastrophicDictionaryChange
        {
            get {return ReadPrivateFlag(PrivateFlags.IsCatastrophicDictionaryChange); }
            set { WritePrivateFlag(PrivateFlags.IsCatastrophicDictionaryChange, value); }
        }

        // This flag is used to indicate that an implicit data template change has occured
        internal bool IsImplicitDataTemplateChange
        {
            get {return ReadPrivateFlag(PrivateFlags.IsImplicitDataTemplateChange); }
            set { WritePrivateFlag(PrivateFlags.IsImplicitDataTemplateChange, value); }
        }

        // This flag is used to indicate if the current operation is an effective add operation
        internal bool IsResourceAddOperation
        {
            get { return _key != null || (_newDictionaries != null && _newDictionaries.Count > 0); }
        }

        // This member is used to identify the container when a style change happens
        internal DependencyObject Container
        {
            get { return _container; }
        }

        // Says if either the old or the new dictionaries contain the given key
        internal bool Contains(object key, bool isImplicitStyleKey)
        {
            if (IsTreeChange || IsCatastrophicDictionaryChange)
            {
                return true;
            }
            else if (IsThemeChange || IsSysColorsOrSettingsChange)
            {
                // Implicit Styles are not fetched from the Themes.
                // So we do not need to respond to theme changes.
                // This is a performance optimization.

                return !isImplicitStyleKey;
            }

            Debug.Assert(_oldDictionaries != null || _newDictionaries != null || _key != null, "Must have a dictionary or a key that has changed");

            if (_key != null)
            {
                if (Object.Equals(_key, key))
                {
                    return true;
                }
            }

            if (_oldDictionaries != null)
            {
                for (int i=0; i<_oldDictionaries.Count; i++)
                {
                    if (_oldDictionaries[i].Contains(key))
                    {
                        return true;
                    }
                }
            }

            if (_newDictionaries != null)
            {
                for (int i=0; i<_newDictionaries.Count; i++)
                {
                    if (_newDictionaries[i].Contains(key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // determine whether this change affects implicit data templates
        internal void SetIsImplicitDataTemplateChange()
        {
            bool isImplicitDataTemplateChange = (IsCatastrophicDictionaryChange ||
                                                (_key is DataTemplateKey));

            if (!isImplicitDataTemplateChange && _oldDictionaries != null)
            {
                foreach (ResourceDictionary rd in _oldDictionaries)
                {
                    if (rd.HasImplicitDataTemplates)
                    {
                        isImplicitDataTemplateChange = true;
                        break;
                    }
                }
            }

            if (!isImplicitDataTemplateChange && _newDictionaries != null)
            {
                foreach (ResourceDictionary rd in _newDictionaries)
                {
                    if (rd.HasImplicitDataTemplates)
                    {
                        isImplicitDataTemplateChange = true;
                        break;
                    }
                }
            }

            IsImplicitDataTemplateChange = isImplicitDataTemplateChange;
        }

        #endregion Operations

        #region PrivateMethods

        private void WritePrivateFlag(PrivateFlags bit, bool value)
        {
            if (value)
            {
                _flags |= bit;
            }
            else
            {
                _flags &= ~bit;
            }
        }

        private bool ReadPrivateFlag(PrivateFlags bit)
        {
            return (_flags & bit) != 0;
        }

        #endregion PrivateMethods

        #region PrivateDataStructures

        private enum PrivateFlags : byte
        {
            IsThemeChange                   = 0x01,
            IsTreeChange                    = 0x02,
            IsStyleResourceChange           = 0x04,
            IsTemplateResourceChange        = 0x08,
            IsSysColorsOrSettingsChange     = 0x10,
            IsCatastrophicDictionaryChange  = 0x20,
            IsImplicitDataTemplateChange    = 0x40,
        }

        #endregion PrivateDataStructures

        #region Data

        private List<ResourceDictionary> _oldDictionaries;
        private List<ResourceDictionary> _newDictionaries;
        private object                   _key;
        private DependencyObject         _container;
        private PrivateFlags             _flags;

        #endregion Data
    }
}

