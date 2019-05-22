// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Placeholder PropertyInfo used in SourceValueState when the corresponding
//      property in SourceValueInfo is really a named indexed property (from VB).
//

using System;
using System.Reflection;

namespace MS.Internal.Data
{
    internal class IndexerPropertyInfo : PropertyInfo
    {
        private IndexerPropertyInfo()
        {
        }

        internal static IndexerPropertyInfo Instance
        {
            get { return _instance; }
        }

        static readonly IndexerPropertyInfo _instance = new IndexerPropertyInfo();

        #region PropertyInfo overrides

        public override PropertyAttributes Attributes
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return null;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return new ParameterInfo[0];
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return null;
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            // This is the only interesting line.  GetValue returns the base object itself.
            return obj;
        }

        public override Type PropertyType
        {
            get { return typeof(Object); }
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override Type DeclaringType
        {
            get { throw new NotImplementedException(); }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override string Name
        {
            get { return "IndexerProperty"; }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        #endregion PropertyInfo overrides
    }
}
