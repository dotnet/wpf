// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Xaml
{
    public interface IAmbientProvider
    {
        AmbientPropertyValue GetFirstAmbientValue(IEnumerable<XamlType> ceilingTypes,
                                                    params XamlMember[] properties);
        object GetFirstAmbientValue(params XamlType[] types);

        IEnumerable<AmbientPropertyValue> GetAllAmbientValues(IEnumerable<XamlType> ceilingTypes,
                                                    params XamlMember[] properties);

        IEnumerable<object> GetAllAmbientValues(params XamlType[] types);

        IEnumerable<AmbientPropertyValue> GetAllAmbientValues(IEnumerable<XamlType> ceilingTypes,
                                                              bool searchLiveStackOnly,
                                                              IEnumerable<XamlType> types,
                                                              params XamlMember[] properties);
    }

    public class AmbientPropertyValue
    {
        public AmbientPropertyValue(XamlMember property, object value)
        {
            RetrievedProperty = property;
            Value = value;
        }

        public object Value { get; }
        public XamlMember RetrievedProperty { get; }
    }
}
