// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:  
//      Enables scripting support against the HTML DOM for XBAPs using the DLR
//      dynamic feature, as available through the dynamic keyword in C# 4.0 and
//      also supported in Visual Basic.
//

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows;
using MS.Internal.Interop;
using MS.Win32;

namespace System.Windows.Interop
{
    /// <summary>
    /// Enables scripting support against the HTML DOM for XBAPs using the DLR.
    /// </summary>
    public sealed class DynamicScriptObject : DynamicObject
    {
        //----------------------------------------------
        //
        // Constructors
        //
        //----------------------------------------------

        #region Constructor

        /// <summary>
        /// Wraps the given object in a script object for "dynamic" access.
        /// </summary>
        /// <param name="scriptObject">Object to be wrapped.</param>
        internal DynamicScriptObject(UnsafeNativeMethods.IDispatch scriptObject)
        {
            if (scriptObject == null)
            {
                throw new ArgumentNullException("scriptObject");
            }

            _scriptObject = scriptObject;

            // In the case of IE, we use IDispatchEx for enhanced security (see InvokeOnScriptObject).
            _scriptObjectEx = _scriptObject as UnsafeNativeMethods.IDispatchEx;
        }

        #endregion Constructor


        //----------------------------------------------
        //
        // Public Methods
        //
        //----------------------------------------------

        #region Public Methods

        /// <summary>
        /// Calls a script method. Corresponds to methods calls in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (binder == null)
            {
                throw new ArgumentNullException("binder");
            }

            result = InvokeAndReturn(binder.Name, NativeMethods.DISPATCH_METHOD, args);
            return true;
        }

        /// <summary>
        /// Gets a member from script. Corresponds to property getter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder == null)
            {
                throw new ArgumentNullException("binder");
            }

            result = InvokeAndReturn(binder.Name, NativeMethods.DISPATCH_PROPERTYGET, null);
            return true;
        }

        /// <summary>
        /// Sets a member in script. Corresponds to property setter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (binder == null)
            {
                throw new ArgumentNullException("binder");
            }

            int flags = GetPropertyPutMethod(value);
            object result = InvokeAndReturn(binder.Name, flags, new object[] { value });
            return true;
        }

        /// <summary>
        /// Gets an indexed value from script. Corresponds to indexer getter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (binder == null)
            {
                throw new ArgumentNullException("binder");
            }

            if (indexes == null)
            {
                throw new ArgumentNullException("indexes");
            }

            // IE supports a default member for indexers. Try that first. This accommodates for indexing
            // in collection types, using a default method called "item".
            if (BrowserInteropHelper.IsHostedInIEorWebOC)
            {
                if (TryFindMemberAndInvoke(null, NativeMethods.DISPATCH_METHOD, false /* no DISPID caching */, indexes, out result))
                {
                    return true;
                }
            }

            // We fall back to property lookup given the first argument of the indices. This accommodates
            // for arrays (e.g. d.x[0]) and square-bracket-style property lookup (e.g. d.document["title"]).
            if (indexes.Length != 1)
            {
                throw new ArgumentException("indexes", HRESULT.DISP_E_BADPARAMCOUNT.GetException());
            }

            object index = indexes[0];
            if (index == null)
            {
                throw new ArgumentOutOfRangeException("indexes");
            }

            result = InvokeAndReturn(index.ToString(), NativeMethods.DISPATCH_PROPERTYGET, false /* no DISPID caching */, null);
            return true;
        }

        /// <summary>
        /// Sets a member in script, through an indexer. Corresponds to indexer setter syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (binder == null)
            {
                throw new ArgumentNullException("binder");
            }

            if (indexes == null)
            {
                throw new ArgumentNullException("indexes");
            }

            if (indexes.Length != 1)
            {
                throw new ArgumentException("indexes", HRESULT.DISP_E_BADPARAMCOUNT.GetException());
            }

            object index = indexes[0];
            if (index == null)
            {
                throw new ArgumentOutOfRangeException("indexes");
            }

            // We don't cache resolved DISPIDs for indexers as they have the potential to be used for dynamic resolution
            // of a bunch of members, e.g. when indexing into arrays. This would flood the cache, likely for just a one-
            // time access. So we just don't cache in this case.
            object result = InvokeAndReturn(index.ToString(), NativeMethods.DISPATCH_PROPERTYPUT, false /* no DISPID caching */,
                                new object[] { value });

            return true;
        }

        /// <summary>
        /// Calls the default script method. Corresponds to delegate calling syntax in the front-end language.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true - We never defer behavior to the call site, and throw if invalid access is attempted.</returns>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            if (binder == null)
            {
                throw new ArgumentNullException("binder");
            }

            result = InvokeAndReturn(null, NativeMethods.DISPATCH_METHOD, args);
            return true;
        }

        /// <summary>
        /// Provides a string representation of the wrapped script object.
        /// </summary>
        /// <returns>String representation of the wrapped script object, using the toString method or default member on the script object.</returns>
        public override string ToString()
        {
            // Note we shouldn't throw in this method (rule CA1065), so we try with best attempt.

            HRESULT hr;

            Guid guid = Guid.Empty;
            object result = null;
            var dp = new NativeMethods.DISPPARAMS();

            // Try to find a toString method.
            int dispid;
            if (TryGetDispIdForMember("toString", true /* DISPID caching */, out dispid))
            {
                hr = InvokeOnScriptObject(dispid, NativeMethods.DISPATCH_METHOD, dp, null /* EXCEPINFO */, out result);
            }
            else
            {
                // If no toString method is found, we try the default member first as a property, then as a method.

                dispid = NativeMethods.DISPID_VALUE;

                hr = InvokeOnScriptObject(dispid, NativeMethods.DISPATCH_PROPERTYGET, dp, null /* EXCEPINFO */, out result);

                if (hr.Failed)
                {
                    hr = InvokeOnScriptObject(dispid, NativeMethods.DISPATCH_METHOD, dp, null /* EXCEPINFO */, out result);
                }
            }

            if (hr.Succeeded && result != null)
            {
                return result.ToString();
            }

            return base.ToString();
        }

        #endregion Public Methods


        //----------------------------------------------
        //
        // Internal Properties
        //
        //----------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Gets the IDispatch script object wrapped by the DynamicScriptObject.
        /// </summary>
        internal UnsafeNativeMethods.IDispatch ScriptObject
        {
            get
            {
                return _scriptObject;
            }
        }

        #endregion Internal Properties


        //----------------------------------------------
        //
        // Internal Methods
        //
        //----------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Helper method to attempt invoking a script member with the given name, flags and arguments.
        /// </summary>
        /// <param name="memberName">The name of the member to invoke.</param>
        /// <param name="flags">One of the DISPATCH_ flags for IDispatch calls.</param>
        /// <param name="cacheDispId">true to enable caching of DISPIDs; false otherwise.</param>
        /// <param name="args">Arguments passed to the call.</param>
        /// <param name="result">The raw (not wrapped in DynamicScriptObject) result of the invocation.</param>
        /// <returns>true if the member was found; false otherwise.</returns>
        internal unsafe bool TryFindMemberAndInvokeNonWrapped(string memberName, int flags, bool cacheDispId, object[] args, out object result)
        {
            result = null;

            // In true DLR style we'd return false here, deferring further attempts for resolution to the
            // call site. For better debuggability though, and as we're talking to a specialized object
            // model, we rather throw instead. This Try-method allows internal lookups without directly
            // throwing on non-fatal "member not found" situations, as we might want to have more complex
            // fallback logic (as in ToString).
            int dispid;
            if (!TryGetDispIdForMember(memberName, cacheDispId, out dispid))
            {
                return false;
            }

            NativeMethods.DISPPARAMS dp = new NativeMethods.DISPPARAMS();

            // Required additional DISPPARAMS arguments for PROPERTYPUT cases. See IDispatch::Invoke
            // documentation for more info.
            int propertyPutDispId = NativeMethods.DISPID_PROPERTYPUT;
            if (flags == NativeMethods.DISPATCH_PROPERTYPUT || flags == NativeMethods.DISPATCH_PROPERTYPUTREF)
            {
                dp.cNamedArgs = 1;

                // Stack allocated variables are fixed (unmoveable), so no need to use a fixed
                // statement. The local variable should not get repurposed by the compiler in an
                // unsafe block as we're handing out a pointer to it. For comparison, see the DLR
                // code, CorRuntimeHelpers.cs, where the ConvertInt32ByrefToPtr function relies
                // on the same behavior as they take an IntPtr to a stack-allocated variable that
                // gets used by a DISPPARAMS in a similar way. They have a separate method likely
                // because expression trees can't deal with unsafe code, and they need to fix as
                // they use a by-ref argument which is considered movable (see C# spec, 18.3):
                //
                // public static unsafe IntPtr ConvertInt32ByrefToPtr(ref Int32 value) {
                //     fixed (Int32 *x = &value) {
                //         AssertByrefPointsToStack(new IntPtr(x));
                //         return new IntPtr(x);
                //     }
                // }
                dp.rgdispidNamedArgs = new IntPtr(&propertyPutDispId);
            }

            try
            {
                if (args != null)
                {
                    // Callers of this method might want to implement fallbacks that require the original
                    // arguments in the original order, maybe to feed it in to this method again. If we
                    // wouldn't clone the arguments array into a local copy, we'd be reversing again.
                    args = (object[])args.Clone();

                    // Reverse the argument order so that parameters read naturally after IDispatch.
                    // This code was initially ported from WinForms, see WinForms bug 187662.
                    Array.Reverse(args);

                    // Unwrap arguments that were already promoted to DynamicScriptObject. This can happen
                    // if the output of a script accessing method is fed in to the input of another one.
                    for (int i = 0; i < args.Length; i++)
                    {
                        var wrappedArg = args[i] as DynamicScriptObject;
                        if (wrappedArg != null)
                        {
                            args[i] = wrappedArg._scriptObject;
                        }

                        if (args[i] != null)
                        {
                            // Arrays of COM visible objects (in our definition of the word, see further) are
                            // not considered COM visible by themselves, so we take care of this case as well.
                            // Jagged arrays are not supported somehow, causing a SafeArrayTypeMismatchException
                            // in the call to GetNativeVariantForObject on ArrayToVARIANTVector called below.
                            // Multi-dimensional arrays turn out to work fine, so we don't opt out from those.
                            Type argType = args[i].GetType();
                            if (argType.IsArray)
                            {
                                argType = argType.GetElementType();
                            }

                            // Caveat: IsTypeVisibleFromCom evaluates false for COM object wrappers provided
                            // by the CLR. Therefore we also check for the IsCOMObject property. It also seems
                            // COM interop special-cases DateTime as it's not revealed to be visible by any
                            // of the first two checks below.
                            if (!MarshalLocal.IsTypeVisibleFromCom(argType) && 
                                !argType.IsCOMObject && argType != typeof(DateTime))
                            {
                                throw new ArgumentException(SR.Get(SRID.NeedToBeComVisible));
                            }
                        }
                    }

                    dp.rgvarg = UnsafeNativeMethods.ArrayToVARIANTHelper.ArrayToVARIANTVector(args);
                    dp.cArgs = (uint)args.Length;
                }

                NativeMethods.EXCEPINFO exInfo = new NativeMethods.EXCEPINFO();
                HRESULT hr = InvokeOnScriptObject(dispid, flags, dp, exInfo, out result);

                if (hr.Failed)
                {
                    if (hr == HRESULT.DISP_E_MEMBERNOTFOUND)
                    {
                        return false;
                    }

                    // See KB article 247784, INFO: '80020101' Returned From Some ActiveX Scripting Methods.
                    // Internet Explorer returns this error when it has already reported a script error to the user
                    // through a dialog or by putting a message in the status bar (Page contains script error). We
                    // want consistency between browsers, so route this through the DISP_E_EXCEPTION case.
                    if (hr == HRESULT.SCRIPT_E_REPORTED)
                    {
                        exInfo.scode = hr.Code;
                        hr = HRESULT.DISP_E_EXCEPTION;
                    }

                    // We prefix exception messagages with "[memberName]" so that the target of the invocation can
                    // be found easily. This is useful beyond just seeing the call site in the debugger as dynamic
                    // calls lead to complicated call stacks with the DLR sliced in between the source and target.
                    // Also, for good or for bad, dynamic has the potential to encourage endless "dotting into", so
                    // it's preferrable to have our runtime resolution failure eloquating the target of the dynamic
                    // call. Unfortunately stock CLR exception types often don't offer a convenient spot to put
                    // this info in, so we resort to the Message property. Anyway, those exceptions are primarily
                    // meant to provide debugging convenience and should not be reported to the end-user in a well-
                    // tested application. Essentially all of this is to be conceived as "deferred compilation".
                    string member = "[" + (memberName ?? "(default)") + "]";
                    Exception comException = hr.GetException();

                    if (hr == HRESULT.DISP_E_EXCEPTION)
                    {
                        // We wrap script execution failures in TargetInvocationException to keep the API surface
                        // free of COMExceptions that reflect a mere implementation detail.
                        int errorCode = exInfo.scode != 0 ? exInfo.scode : exInfo.wCode;
                        hr = HRESULT.Make(true /* severity */, Facility.Dispatch, errorCode);
                        string message = member + " " + (exInfo.bstrDescription ?? string.Empty);
                        throw new TargetInvocationException(message, comException)
                        {
                            HelpLink = exInfo.bstrHelpFile,
                            Source = exInfo.bstrSource
                        };
                    }
                    else if (hr == HRESULT.DISP_E_BADPARAMCOUNT || hr == HRESULT.DISP_E_PARAMNOTOPTIONAL)
                    {
                        throw new TargetParameterCountException(member, comException);
                    }
                    else if (hr == HRESULT.DISP_E_OVERFLOW || hr == HRESULT.DISP_E_TYPEMISMATCH)
                    {
                        throw new ArgumentException(member, new InvalidCastException(comException.Message, hr.Code));
                    }
                    else
                    {
                        // Something really bad has happened; keeping the exception as-is.
                        throw comException;
                    }
                }
            }
            finally
            {
                if (dp.rgvarg != IntPtr.Zero)
                {
                    UnsafeNativeMethods.ArrayToVARIANTHelper.FreeVARIANTVector(dp.rgvarg, args.Length);
                }
            }

            return true;
        }

        #endregion Internal Methods


        //----------------------------------------------
        //
        // Private Methods
        //
        //----------------------------------------------

        #region Private Methods

        /// <summary>
        /// Helper method to invoke a script member with the given name, flags and arguments.
        /// This overload always caches resolved DISPIDs.
        /// </summary>
        /// <param name="memberName">The name of the member to invoke.</param>
        /// <param name="flags">One of the DISPATCH_ flags for IDispatch calls.</param>
        /// <param name="args">Arguments passed to the call.</param>
        /// <returns>The result of the invocation.</returns>
        private object InvokeAndReturn(string memberName, int flags, object[] args)
        {
            return InvokeAndReturn(memberName, flags, true /* DISPID caching */, args);
        }

        /// <summary>
        /// Helper method to invoke a script member with the given name, flags and arguments.
        /// This overload allows control over the resolved DISPIDs caching behavior.
        /// </summary>
        /// <param name="memberName">The name of the member to invoke.</param>
        /// <param name="flags">One of the DISPATCH_ flags for IDispatch calls.</param>
        /// <param name="cacheDispId">true to enable caching of DISPIDs; false otherwise.</param>
        /// <param name="args">Arguments passed to the call.</param>
        /// <returns>The result of the invocation.</returns>
        private object InvokeAndReturn(string memberName, int flags, bool cacheDispId, object[] args)
        {
            object result;
            if (!TryFindMemberAndInvoke(memberName, flags, cacheDispId, args, out result))
            {
                if (flags == NativeMethods.DISPATCH_METHOD)
                    throw new MissingMethodException(this.ToString(), memberName);
                else
                    throw new MissingMemberException(this.ToString(), memberName);
            }

            return result;
        }

        /// <summary>
        /// Helper method to attempt invoking a script member with the given name, flags and arguments.
        /// Wraps the result value in a DynamicScriptObject if required.
        /// </summary>
        /// <param name="memberName">The name of the member to invoke.</param>
        /// <param name="flags">One of the DISPATCH_ flags for IDispatch calls.</param>
        /// <param name="cacheDispId">true to enable caching of DISPIDs; false otherwise.</param>
        /// <param name="args">Arguments passed to the call.</param>
        /// <param name="result">The result of the invocation, wrapped in DynamicScriptObject if required.</param>
        /// <returns>true if the member was found; false otherwise.</returns>
        private bool TryFindMemberAndInvoke(string memberName, int flags, bool cacheDispId, object[] args, out object result)
        {
            if (!TryFindMemberAndInvokeNonWrapped(memberName, flags, cacheDispId, args, out result))
            {
                return false;
            }

            // Only wrap returned COM objects; if the object returns as a CLR object, we just return it.
            if (result != null && Marshal.IsComObject(result))
            {
                // Script objects implement IDispatch.
                result = new DynamicScriptObject((UnsafeNativeMethods.IDispatch)result);
            }

            return true;
        }

        /// <summary>
        /// Helper method to map a script member with the given name onto a DISPID.
        /// </summary>
        /// <param name="memberName">The name of the member to look up.</param>
        /// <param name="cacheDispId">true to enable caching of DISPIDs; false otherwise.</param>
        /// <param name="dispid">If the member was found, its DISPID; otherwise, default DISPID_VALUE.</param>
        /// <returns>true if the member was found; false if it wasn't (DISP_E_UNKNOWNNAME).</returns>
        private bool TryGetDispIdForMember(string memberName, bool cacheDispId, out int dispid)
        {
            dispid = NativeMethods.DISPID_VALUE;
            if (!string.IsNullOrEmpty(memberName))
            {
                if (   !cacheDispId /* short-circuit lookup; will never get cached */
                    || !_dispIdCache.TryGetValue(memberName, out dispid))
                {
                    Guid guid = Guid.Empty;

                    string[] names   = new string[] { memberName };
                    int[]    dispids = new int[]    { NativeMethods.DISPID_UNKNOWN };

                    // Only the "member not found" case deserves special treatment. We leave it up to the
                    // caller to decide on the right treatment.
                    HRESULT hr = _scriptObject.GetIDsOfNames(ref guid, names, dispids.Length, Thread.CurrentThread.CurrentCulture.LCID, dispids);
                    if (hr == HRESULT.DISP_E_UNKNOWNNAME)
                    {
                        return false;
                    }

                    // Fatal unexpected exception here; it's fine to leak a COMException to the surface.
                    hr.ThrowIfFailed();

                    dispid = dispids[0];

                    if (cacheDispId)
                    {
                        _dispIdCache[memberName] = dispid;
                    }
                }
            }

            return true;
        }

        private HRESULT InvokeOnScriptObject(int dispid, int flags, NativeMethods.DISPPARAMS dp, NativeMethods.EXCEPINFO exInfo, out object result)
        {
            // If we use reflection to call script code, we need to Assert for the UnmanagedCode permission. 
            // But it will be a security issue when the WPF app makes a framework object available to the 
            // hosted script via ObjectForScripting or as parameter of InvokeScript, and calls the framework
            // API that demands the UnmanagedCode permission. We do not want the demand to succeed. However, 
            // the stack walk will ignore the native frames and keeps going until it reaches the Assert.
            //
            // As an example, if a call to a script object causes an immediate callback before the initial
            // call returns, reentrancy occurs via COM's blocking message loop on outgoing calls:
            //
            //   [managed ComVisible object]
            //   [CLR COM interop]
            //   [COM runtime]
            //   ole32.dll!CCliModalLoop::BlockFn()
            //   ole32.dll!ModalLoop()
            //   [COM runtime]
            //   PresentationFramework!DynamicScriptObject::InvokeScript(...)
            //
            // That is why we switch to invoking the script via IDispatch with SUCS on the methods.

            if (_scriptObjectEx != null)
            {
                // This case takes care of IE hosting where the use of IDispatchEx is recommended by IE people
                // since the service provider object we can pass here is used by the browser to enforce cross-
                // zone scripting mitigations. 
                return _scriptObjectEx.InvokeEx(dispid, Thread.CurrentThread.CurrentCulture.LCID, flags, dp, out result, exInfo, BrowserInteropHelper.HostHtmlDocumentServiceProvider);
            }
            else
            {
                Guid guid = Guid.Empty;
                return _scriptObject.Invoke(dispid, ref guid, Thread.CurrentThread.CurrentCulture.LCID, flags, dp, out result, exInfo, null);
            }
        }

        /// <summary>
        /// Helper function to get the IDispatch::Invoke invocation method for setting a property.
        /// </summary>
        /// <param name="value">Object to be assigned to the property.</param>
        /// <returns>DISPATCH_PROPERTYPUTREF or DISPATCH_PROPERTYPUT</returns>
        private static int GetPropertyPutMethod(object value)
        {
            // On the top-level script scope, setting a variable of a reference
            // type without using the DISPATCH_PROPERTYPUTREF flag doesn't work since it causes the
            // default member to be invoked as part of the conversion of the reference to a "value".
            // It seems this didn't affect DOM property setters where the IDispatch implementation is
            // more relaxed about the use of PUTREF versus PUT. This code is pretty much analog to
            // the DLR's COM binder's; see ndp\fx\src\Dynamic\System\Dynamic\ComBinderHelpers.cs for
            // further information.

            if (value == null)
            {
                return NativeMethods.DISPATCH_PROPERTYPUTREF;
            }

            Type type = value.GetType();
            if (   type.IsValueType
                || type.IsArray
                || type == typeof(string)
                || type == typeof(CurrencyWrapper)
                || type == typeof(DBNull)
                || type == typeof(Missing))
            {
                return NativeMethods.DISPATCH_PROPERTYPUT;
            }
            else
            {
                return NativeMethods.DISPATCH_PROPERTYPUTREF;
            }
        }

        #endregion Private Methods


        //----------------------------------------------
        //
        // Private Fields
        //
        //----------------------------------------------

        #region Private Fields

        /// <summary>
        /// Script object to invoke operations on through the "dynamic" feature.
        /// </summary>
        private UnsafeNativeMethods.IDispatch _scriptObject;

        /// <summary>
        /// Script object to invoke operations on through the "dynamic" feature.
        /// Used in the case of IE, where IDispatchEx is used to tighten security (see InvokeOnScriptObject).
        /// </summary>
        private UnsafeNativeMethods.IDispatchEx _scriptObjectEx;

        /// <summary>
        /// Cache of DISPID values for members. Allows to speed up repeated calls.
        /// </summary>
        private Dictionary<string, int> _dispIdCache = new Dictionary<string, int>();

        #endregion Private Fields
    }
}
