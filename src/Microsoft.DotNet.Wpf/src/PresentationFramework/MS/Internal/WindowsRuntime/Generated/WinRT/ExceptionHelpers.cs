// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    internal static class ExceptionHelpers
    {
        private const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);
        private const int RO_E_CLOSED = unchecked((int)0x80000013);
        internal const int E_BOUNDS = unchecked((int)0x8000000b);
        internal const int E_CHANGED_STATE = unchecked((int)0x8000000c);
        private const int E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000d);
        private const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000e);
        private const int E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);
        private const int APPMODEL_ERROR_NO_PACKAGE = unchecked((int)0x80073D54);
        internal const int E_XAMLPARSEFAILED = unchecked((int)0x802B000A);
        internal const int E_LAYOUTCYCLE = unchecked((int)0x802B0014);
        internal const int E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);
        internal const int E_ELEMENTNOTAVAILABLE = unchecked((int)0x802B001F);

        [DllImport("oleaut32.dll")]
        private static extern int SetErrorInfo(uint dwReserved, IntPtr perrinfo);

        internal delegate int GetRestrictedErrorInfo(out IntPtr ppRestrictedErrorInfo);
        private static GetRestrictedErrorInfo getRestrictedErrorInfo;

        internal delegate int SetRestrictedErrorInfo(IntPtr pRestrictedErrorInfo);
        private static SetRestrictedErrorInfo setRestrictedErrorInfo;

        internal delegate int RoOriginateLanguageException(int error, IntPtr message, IntPtr langaugeException);
        private static RoOriginateLanguageException roOriginateLanguageException;

        internal delegate int RoReportUnhandledError(IntPtr pRestrictedErrorInfo);
        private static RoReportUnhandledError roReportUnhandledError;

        static ExceptionHelpers()
        {
            IntPtr winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-1.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
            if (winRTErrorModule != IntPtr.Zero)
            {
                getRestrictedErrorInfo = Platform.GetProcAddress<GetRestrictedErrorInfo>(winRTErrorModule);
                setRestrictedErrorInfo = Platform.GetProcAddress<SetRestrictedErrorInfo>(winRTErrorModule);
                roOriginateLanguageException = Platform.GetProcAddress<RoOriginateLanguageException>(winRTErrorModule);
                roReportUnhandledError = Platform.GetProcAddress<RoReportUnhandledError>(winRTErrorModule);
            }
            else
            {
                winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-0.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
                if (winRTErrorModule != IntPtr.Zero)
                {
                    getRestrictedErrorInfo = Platform.GetProcAddress<GetRestrictedErrorInfo>(winRTErrorModule);
                    setRestrictedErrorInfo = Platform.GetProcAddress<SetRestrictedErrorInfo>(winRTErrorModule);
                }
            }
        }

        public static void ThrowExceptionForHR(int hr)
        {
            Exception ex = GetExceptionForHR(hr, useGlobalErrorState: true, out bool restoredExceptionFromGlobalState);
            if (restoredExceptionFromGlobalState)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            else if (ex is object)
            {
                throw ex;
            }
        }

        public static Exception GetExceptionForHR(int hr) => GetExceptionForHR(hr, false, out _);

        private static Exception GetExceptionForHR(int hr, bool useGlobalErrorState, out bool restoredExceptionFromGlobalState)
        {
            restoredExceptionFromGlobalState = false;
            if (hr >= 0)
            {
                return null;
            }

            ObjectReference<ABI.WinRT.Interop.IErrorInfo.Vftbl> iErrorInfo = null;
            IObjectReference restrictedErrorInfoToSave = null;
            Exception ex;
            string description = null;
            string restrictedError = null;
            string restrictedErrorReference = null;
            string restrictedCapabilitySid = null;
            bool hasOtherLanguageException = false;

            if (useGlobalErrorState && getRestrictedErrorInfo != null)
            {
                Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(out IntPtr restrictedErrorInfoPtr));

                if (restrictedErrorInfoPtr != IntPtr.Zero)
                {
                    IObjectReference restrictedErrorInfoRef = ObjectReference<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>.Attach(ref restrictedErrorInfoPtr);
                    restrictedErrorInfoToSave = restrictedErrorInfoRef.As<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>();

                    ABI.WinRT.Interop.IRestrictedErrorInfo restrictedErrorInfo = new ABI.WinRT.Interop.IRestrictedErrorInfo(restrictedErrorInfoRef);
                    restrictedErrorInfo.GetErrorDetails(out description, out int hrLocal, out restrictedError, out restrictedCapabilitySid);
                    restrictedErrorReference = restrictedErrorInfo.GetReference();
                    if (restrictedErrorInfoRef.TryAs<ABI.WinRT.Interop.ILanguageExceptionErrorInfo.Vftbl>(out var languageErrorInfoRef) >= 0)
                    {
                        ILanguageExceptionErrorInfo languageErrorInfo = new ABI.WinRT.Interop.ILanguageExceptionErrorInfo(languageErrorInfoRef);
                        using IObjectReference languageException = languageErrorInfo.GetLanguageException();
                        if (languageException is object)
                        {
                            if (languageException.IsReferenceToManagedObject)
                            {
                                ex = ComWrappersSupport.FindObject<Exception>(languageException.ThisPtr);
                                if (GetHRForException(ex) == hr)
                                {
                                    restoredExceptionFromGlobalState = true;
                                    return ex;
                                }
                            }
                            else
                            {
                                hasOtherLanguageException = true;
                            }
                        }
                    }
                    else
                    {
                        if (hr == hrLocal)
                        {
                            restrictedErrorInfoRef.TryAs<ABI.WinRT.Interop.IErrorInfo.Vftbl>(out iErrorInfo);
                        }
                    }
                }
            }

            using (iErrorInfo)
            {
                switch (hr)
                {
                    case E_ILLEGAL_STATE_CHANGE:
                    case E_ILLEGAL_METHOD_CALL:
                    case E_ILLEGAL_DELEGATE_ASSIGNMENT:
                    case APPMODEL_ERROR_NO_PACKAGE:
                        ex = new InvalidOperationException(description);
                        break;
                    default:
                        ex = Marshal.GetExceptionForHR(hr, iErrorInfo?.ThisPtr ?? (IntPtr)(-1));
                        break;
                }
            }

            ex.AddExceptionDataForRestrictedErrorInfo(
                description,
                restrictedError,
                restrictedErrorReference,
                restrictedCapabilitySid,
                restrictedErrorInfoToSave,
                hasOtherLanguageException);

            return ex;
        }

        public static unsafe void SetErrorInfo(Exception ex)
        {
            if (getRestrictedErrorInfo != null && setRestrictedErrorInfo != null && roOriginateLanguageException != null)
            {
                // If the exception has information for an IRestrictedErrorInfo, use that
                // as our error so as to propagate the error through WinRT end-to-end.
                if (ex.TryGetRestrictedLanguageErrorObject(out var restrictedErrorObject))
                {
                    using (restrictedErrorObject)
                    {
                        setRestrictedErrorInfo(restrictedErrorObject.ThisPtr);
                    }
                }
                else
                {
                    string message = ex.Message;
                    if (string.IsNullOrEmpty(message))
                    {
                        message = ex.GetType().FullName;
                    }

                    IntPtr hstring;

                    if (Platform.WindowsCreateString(message, message.Length, &hstring) != 0)
                    {
                        hstring = IntPtr.Zero;
                    }

                    using var managedExceptionWrapper = ComWrappersSupport.CreateCCWForObject(ex);
                    roOriginateLanguageException(GetHRForException(ex), hstring, managedExceptionWrapper.ThisPtr);
                }
            }
            else
            {
                using var iErrorInfo = ComWrappersSupport.CreateCCWForObject(new ManagedExceptionErrorInfo(ex));
                SetErrorInfo(0, iErrorInfo.ThisPtr);
            }
        }

        public static void ReportUnhandledError(Exception ex)
        {
            SetErrorInfo(ex);
            if (getRestrictedErrorInfo != null && roReportUnhandledError != null)
            {
                Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(out IntPtr ppRestrictedErrorInfo));
                using var restrictedErrorRef = ObjectReference<IUnknownVftbl>.Attach(ref ppRestrictedErrorInfo);
                roReportUnhandledError(restrictedErrorRef.ThisPtr);
            }
        }

        public static int GetHRForException(Exception ex)
        {
            int hr = ex.HResult;
            if (ex.TryGetRestrictedLanguageErrorObject(out var restrictedErrorObject))
            {
                restrictedErrorObject.AsType<ABI.WinRT.Interop.IRestrictedErrorInfo>().GetErrorDetails(out _, out hr, out _, out _);
            }
            if (hr == COR_E_OBJECTDISPOSED)
            {
                return RO_E_CLOSED;
            }
            return hr;
        }

        //
        // Exception requires anything to be added into Data dictionary is serializable
        // This wrapper is made serializable to satisfy this requirement but does NOT serialize
        // the object and simply ignores it during serialization, because we only need
        // the exception instance in the app to hold the error object alive.
        //
        [Serializable]
        internal class __RestrictedErrorObject
        {
            // Hold the error object instance but don't serialize/deserialize it
            [NonSerialized]
            private readonly IObjectReference _realErrorObject;

            internal __RestrictedErrorObject(IObjectReference errorObject)
            {
                _realErrorObject = errorObject;
            }

            public IObjectReference RealErrorObject
            {
                get
                {
                    return _realErrorObject;
                }
            }
        }

        internal static void AddExceptionDataForRestrictedErrorInfo(
            this Exception ex,
            string description,
            string restrictedError,
            string restrictedErrorReference,
            string restrictedCapabilitySid,
            IObjectReference restrictedErrorObject,
            bool hasRestrictedLanguageErrorObject = false)
        {
            IDictionary dict = ex.Data;
            if (dict != null)
            {
                dict.Add("Description", description);
                dict.Add("RestrictedDescription", restrictedError);
                dict.Add("RestrictedErrorReference", restrictedErrorReference);
                dict.Add("RestrictedCapabilitySid", restrictedCapabilitySid);

                // Keep the error object alive so that user could retrieve error information
                // using Data["RestrictedErrorReference"]
                dict.Add("__RestrictedErrorObjectReference", restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject));
                dict.Add("__HasRestrictedLanguageErrorObject", hasRestrictedLanguageErrorObject);
            }
        }

        internal static bool TryGetRestrictedLanguageErrorObject(
            this Exception ex,
            out IObjectReference restrictedErrorObject)
        {
            restrictedErrorObject = null;
            IDictionary dict = ex.Data;
            if (dict != null && dict.Contains("__HasRestrictedLanguageErrorObject"))
            {
                if (dict.Contains("__RestrictedErrorObjectReference"))
                {
                    if (dict["__RestrictedErrorObjectReference"] is __RestrictedErrorObject restrictedObject)
                        restrictedErrorObject = restrictedObject.RealErrorObject;
                }
                return (bool)dict["__HasRestrictedLanguageErrorObject"]!;
            }

            return false;
        }

        public static Exception AttachRestrictedErrorInfo(Exception e)
        {
            // If there is no exception, then the restricted error info doesn't apply to it
            if (e != null)
            {
                try
                {
                    // Get the restricted error info for this thread and see if it may correlate to the current
                    // exception object.  Note that in general the thread's IRestrictedErrorInfo is not meant for
                    // exceptions that are marshaled Windows.Foundation.HResults and instead are intended for
                    // HRESULT ABI return values.   However, in many cases async APIs will set the thread's restricted
                    // error info as a convention in order to provide extended debugging information for the ErrorCode
                    // property.
                    Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(out IntPtr restrictedErrorInfoPtr));

                    if (restrictedErrorInfoPtr != IntPtr.Zero)
                    {
                        IObjectReference restrictedErrorInfoRef = ObjectReference<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>.Attach(ref restrictedErrorInfoPtr);

                        ABI.WinRT.Interop.IRestrictedErrorInfo restrictedErrorInfo = new ABI.WinRT.Interop.IRestrictedErrorInfo(restrictedErrorInfoRef);

                        restrictedErrorInfo.GetErrorDetails(out string description,
                                                            out int restrictedErrorInfoHResult,
                                                            out string restrictedDescription,
                                                            out string capabilitySid);

                        // Since this is a special case where by convention there may be a correlation, there is not a
                        // guarantee that the restricted error info does belong to the async error code.  In order to
                        // reduce the risk that we associate incorrect information with the exception object, we need
                        // to apply a heuristic where we attempt to match the current exception's HRESULT with the
                        // HRESULT the IRestrictedErrorInfo belongs to.  If it is a match we will assume association
                        // for the IAsyncInfo case.
                        if (e.HResult == restrictedErrorInfoHResult)
                        {
                            e.AddExceptionDataForRestrictedErrorInfo(description,
                                                                    restrictedDescription,
                                                                     restrictedErrorInfo.GetReference(),
                                                                     capabilitySid,
                                                                     restrictedErrorInfoRef.As<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>());
                        }
                    }
                }
                catch
                {
                    // If we can't get the restricted error info, then proceed as if it isn't associated with this
                    // error.
                }
            }

            return e;
        }
    }

    internal static class ExceptionExtensions
    {
        public static void SetHResult(this Exception ex, int value)
        {
            ex.GetType().GetProperty("HResult").SetValue(ex, value);
        }

        internal static Exception GetExceptionForHR(this Exception innerException, int hresult, string messageResource)
        {
            Exception e;
            if (innerException != null)
            {
                string message = innerException.Message ?? messageResource;
                e = new Exception(message, innerException);
            }
            else
            {
                e = new Exception(messageResource);
            }
            e.SetHResult(hresult);
            return e;
        }
    }

    internal class ErrorStrings
    {
        internal static string Format(string format, params object[] args) => String.Format(format, args);

        internal static readonly string Arg_IndexOutOfRangeException = "Index was outside the bounds of the array.";
        internal static readonly string Arg_KeyNotFound = "The given key was not present in the dictionary.";
        internal static readonly string Arg_KeyNotFoundWithKey = "The given key '{0}' was not present in the dictionary.";
        internal static readonly string Arg_RankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";
        internal static readonly string Argument_AddingDuplicate = "An item with the same key has already been added.";
        internal static readonly string Argument_AddingDuplicateWithKey = "An item with the same key has already been added. Key: {0}";
        internal static readonly string Argument_IndexOutOfArrayBounds = "The specified index is out of bounds of the specified array.";
        internal static readonly string Argument_InsufficientSpaceToCopyCollection = "The specified space is not sufficient to copy the elements from this Collection.";
        internal static readonly string ArgumentOutOfRange_Index = "Index was out of range. Must be non-negative and less than the size of the collection.";
        internal static readonly string ArgumentOutOfRange_IndexLargerThanMaxValue = "This collection cannot work with indices larger than Int32.MaxValue - 1 (0x7FFFFFFF - 1).";
        internal static readonly string InvalidOperation_CannotRemoveLastFromEmptyCollection = "Cannot remove the last element from an empty collection.";
        internal static readonly string InvalidOperation_CollectionBackingDictionaryTooLarge = "The collection backing this Dictionary contains too many elements.";
        internal static readonly string InvalidOperation_CollectionBackingListTooLarge = "The collection backing this List contains too many elements.";
        internal static readonly string InvalidOperation_EnumEnded = "Enumeration already finished.";
        internal static readonly string InvalidOperation_EnumFailedVersion = "Collection was modified; enumeration operation may not execute.";
        internal static readonly string InvalidOperation_EnumNotStarted = "Enumeration has not started. Call MoveNext.";
        internal static readonly string NotSupported_KeyCollectionSet = "Mutating a key collection derived from a dictionary is not allowed.";
        internal static readonly string NotSupported_ValueCollectionSet = "Mutating a value collection derived from a dictionary is not allowed.";
    }
}
