// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Xaml;
using System.Xaml.Schema;

namespace System.Windows.Baml2006
{
    class WpfKnownTypeInvoker : XamlTypeInvoker
    {
        WpfKnownType _type;

        public WpfKnownTypeInvoker(WpfKnownType type)
            : base(type)
        {
            _type = type;
        }

        public override object CreateInstance(object[] arguments)
        {
            if ((arguments == null || arguments.Length == 0) && _type.DefaultConstructor != null)
            {
                return _type.DefaultConstructor.Invoke();
            }
            else if (_type.IsMarkupExtension)
            {
                Baml6ConstructorInfo ctorInfo;
                if(!_type.Constructors.TryGetValue(arguments.Length, out ctorInfo))
                {
                    throw new InvalidOperationException(SR.Get(SRID.PositionalArgumentsWrongLength));
                }
                return ctorInfo.Constructor(arguments);
            }
            else
            {
                return base.CreateInstance(arguments);
            }
        }
    }
}
