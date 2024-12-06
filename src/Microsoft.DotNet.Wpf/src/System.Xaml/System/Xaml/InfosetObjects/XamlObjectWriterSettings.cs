// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows.Markup;
using System.Xaml.Permissions;

namespace System.Xaml
{
    public class XamlObjectWriterSettings : XamlWriterSettings
    {
        public XamlObjectWriterSettings()
        {
        }

        public XamlObjectWriterSettings(XamlObjectWriterSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            AfterBeginInitHandler = settings.AfterBeginInitHandler;
            BeforePropertiesHandler = settings.BeforePropertiesHandler;
            AfterPropertiesHandler = settings.AfterPropertiesHandler;
            AfterEndInitHandler = settings.AfterEndInitHandler;

            XamlSetValueHandler = settings.XamlSetValueHandler;

            RootObjectInstance = settings.RootObjectInstance;
            IgnoreCanConvert = settings.IgnoreCanConvert;
            ExternalNameScope = settings.ExternalNameScope;
            SkipDuplicatePropertyCheck = settings.SkipDuplicatePropertyCheck;
            RegisterNamesOnExternalNamescope = settings.RegisterNamesOnExternalNamescope;
            AccessLevel = settings.AccessLevel;
            SkipProvideValueOnRoot = settings.SkipProvideValueOnRoot;
            PreferUnconvertedDictionaryKeys = settings.PreferUnconvertedDictionaryKeys;
            SourceBamlUri = settings.SourceBamlUri;
        }

        public EventHandler<XamlObjectEventArgs> AfterBeginInitHandler { get; set; }
        public EventHandler<XamlObjectEventArgs> BeforePropertiesHandler { get; set; }
        public EventHandler<XamlObjectEventArgs> AfterPropertiesHandler { get; set; }
        public EventHandler<XamlObjectEventArgs> AfterEndInitHandler { get; set; }
        public EventHandler<XamlSetValueEventArgs> XamlSetValueHandler { get; set; }

        public object RootObjectInstance { get; set; }
        public bool IgnoreCanConvert { get; set; }
        public INameScope ExternalNameScope { get; set; }
        public bool SkipDuplicatePropertyCheck { get; set; }
        public bool RegisterNamesOnExternalNamescope { get; set; }
        public bool SkipProvideValueOnRoot { get; set; }
        public bool PreferUnconvertedDictionaryKeys { get; set; }

        /// <summary>
        /// SourceBamlUri will be used by XamlObjectWriter in BeginInitHandler's SourceBamlUri property, in place of the actual BaseUri.
        /// This is only useful to give the correct info in that handler, while keeping runtime behavior fully compatible.
        /// </summary>
        public Uri SourceBamlUri { get; set; }

        // Not critical: This property indicates the expected access level for non-public members
        //               in partial trust. We don't actually make any security decisions based on
        //               this value without demanding XamlLoadPermission.
        public XamlAccessLevel AccessLevel { get; set; }

        internal XamlObjectWriterSettings StripDelegates()
        {
            XamlObjectWriterSettings result = new XamlObjectWriterSettings(this)
            {
                // We need better protection against leaking out these delegates
                AfterBeginInitHandler = null,
                AfterEndInitHandler = null,
                AfterPropertiesHandler = null,
                BeforePropertiesHandler = null,
                XamlSetValueHandler = null
            };

            return result;
        }
    }
}
