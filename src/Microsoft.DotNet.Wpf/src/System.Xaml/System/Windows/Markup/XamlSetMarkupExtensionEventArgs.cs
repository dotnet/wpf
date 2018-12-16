// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xaml;

namespace System.Windows.Markup
{
    public class XamlSetMarkupExtensionEventArgs : XamlSetValueEventArgs
    {
        public XamlSetMarkupExtensionEventArgs(XamlMember member,
            MarkupExtension value, IServiceProvider serviceProvider) :
            base(member, value)
        {
            ServiceProvider = serviceProvider;
        }

        public MarkupExtension MarkupExtension { get { return Value as MarkupExtension; } }
        public IServiceProvider ServiceProvider { get; private set; }

        internal XamlSetMarkupExtensionEventArgs(XamlMember member,
            MarkupExtension value, IServiceProvider serviceProvider, object targetObject)
            : this(member, value, serviceProvider)
        {
            TargetObject = targetObject;
        }

        internal XamlType CurrentType { get; set; }
        internal object TargetObject { get; private set; }

        public override void CallBase()
        {
            if (CurrentType != null)
            {
                XamlType baseType = CurrentType.BaseType;

                if (baseType != null)
                {
                    this.CurrentType = baseType;
                    if (baseType.SetMarkupExtensionHandler != null)
                    {
                        baseType.SetMarkupExtensionHandler(TargetObject, this);
                    }
                }
            }
        }
    }
}