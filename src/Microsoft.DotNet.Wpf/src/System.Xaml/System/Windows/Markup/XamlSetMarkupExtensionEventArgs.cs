// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

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

        public MarkupExtension MarkupExtension => Value as MarkupExtension;
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
            if (CurrentType is not null)
            {
                XamlType baseType = CurrentType.BaseType;
                if (baseType is not null)
                {
                    CurrentType = baseType;
                    if (baseType.SetMarkupExtensionHandler is not null)
                    {
                        baseType.SetMarkupExtensionHandler(TargetObject, this);
                    }
                }
            }
        }
    }
}
