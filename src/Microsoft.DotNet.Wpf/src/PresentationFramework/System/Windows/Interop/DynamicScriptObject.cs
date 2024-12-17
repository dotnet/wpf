// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:  
//      Enables scripting support against the HTML DOM for XBAPs using the DLR
//      dynamic feature, as available through the dynamic keyword in C# 4.0 and
//      also supported in Visual Basic. Deprecated as XBAP is not supported.
//

using System.Dynamic;

namespace System.Windows.Interop
{
    /// <summary>
    /// Enables scripting support against the HTML DOM for XBAPs using the DLR.
    /// </summary>
    public sealed class DynamicScriptObject : DynamicObject
    {
        /// <summary>
        /// Internal constructor, kept for ApiCompat reasons.
        /// </summary>
        internal DynamicScriptObject() { }

        /// <summary>
        /// Calls a script method. Corresponds to methods calls in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;

            return false;
        }

        /// <summary>
        /// Gets a member from script. Corresponds to property getter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;

            return false;
        }

        /// <summary>
        /// Sets a member in script. Corresponds to property setter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value) => false;

        /// <summary>
        /// Gets an indexed value from script. Corresponds to indexer getter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;

            return false;
        }

        /// <summary>
        /// Sets a member in script, through an indexer. Corresponds to indexer setter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) => false;

        /// <summary>
        /// Calls the default script method. Corresponds to delegate calling syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;

            return false;
        }

        /// <summary>
        /// Provides a string representation of the wrapped script object.
        /// </summary>
        /// <returns>String representation of the wrapped script object, using the toString method or default member on the script object.</returns>
        public override string ToString() => null;

    }
}
