// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Windows.Markup
{
    /// <summary>
    ///     Specifies the serialization flags per property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DesignerSerializationOptionsAttribute : Attribute
    {
        #region Construction
        
        /// <summary>
        ///     Constructor for DesignerSerializationOptionsAttribute
        /// </summary>
        public DesignerSerializationOptionsAttribute(DesignerSerializationOptions designerSerializationOptions)
        {
            if (DesignerSerializationOptions.SerializeAsAttribute == designerSerializationOptions)
            {
                _designerSerializationOptions = designerSerializationOptions;
            }
            else
            {
                throw new InvalidEnumArgumentException(SR.Format(SR.Enum_Invalid, "DesignerSerializationOptions"));
            }
        }

        #endregion Construction

        #region Properties

        /// <summary>
        ///     DesignerSerializationOptions
        /// </summary>
        public DesignerSerializationOptions DesignerSerializationOptions
        {
            get { return _designerSerializationOptions; }
        }

        #endregion Properties

        #region Data

        private DesignerSerializationOptions _designerSerializationOptions;

        #endregion Data
    }
}

