// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Imports from unmanaged UiaCore DLL

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Automation.Provider;
using System.Diagnostics;
using MS.Win32;
using Microsoft.Internal;

namespace MS.Internal.Automation
{
    // this is a client-only DLL. After we split into client vs provider,
    // the provider assembly will need that attribute on its own APIs.
    internal static class UiaCoreApi
    {
        //------------------------------------------------------
        //
        //  Conditions enums and structs
        //
        //------------------------------------------------------

        #region Conditions

        internal enum ConditionType
        {
            True = 0,
            False = 1,
            Property = 2,
            And = 3,
            Or = 4,
            Not = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UiaCondition
        {
            ConditionType _conditionType;

            internal UiaCondition(ConditionType conditionType)
            {
                _conditionType = conditionType;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UiaPropertyCondition
        {
            ConditionType _conditionType;
            int _propertyId;
            [MarshalAs(UnmanagedType.Struct)] // UnmanagedType.Struct == use VARIANT
            object _value;
            PropertyConditionFlags _flags;

            internal UiaPropertyCondition(int propertyId, object value, PropertyConditionFlags flags)
            {
                _conditionType = ConditionType.Property;
                _propertyId = propertyId;
                _value = value;
                _flags = flags;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UiaAndOrCondition
        {
            ConditionType _conditionType;
            public IntPtr _conditions; // ptr to array-of-ptrs to conditions
            public int _conditionCount;

            internal UiaAndOrCondition(ConditionType conditionType, IntPtr conditions, int conditionCount)
            {
                _conditionType = conditionType;
                _conditions = conditions;
                _conditionCount = conditionCount;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UiaNotCondition
        {
            ConditionType _conditionType;
            public IntPtr _condition;

            internal UiaNotCondition(IntPtr condition)
            {
                _conditionType = ConditionType.Not;
                _condition = condition;
            }
        }
        #endregion Conditions

        //------------------------------------------------------
        //
        //  CacheRequest/CacheResponse
        //
        //------------------------------------------------------

        #region CacheRequest/Response

        [StructLayout(LayoutKind.Sequential)]
        internal class UiaCacheRequest
        {
            internal Condition _condition;
            internal TreeScope _scope;
            internal IntPtr _pProperties;
            internal int _propertyCount;
            internal IntPtr _pPatterns;
            internal int _patternCount;
            internal AutomationElementMode _automationElementMode;

            // The following fields are not used by unmanaged code...
            private AutomationProperty[] _properties;
            private AutomationPattern[] _patterns;

            public TreeScope TreeScope { get { return _scope; } }
            public AutomationElementMode AutomationElementMode { get { return _automationElementMode; } }
            public AutomationProperty [] Properties { get { return _properties; } }
            public AutomationPattern [] Patterns { get { return _patterns; } }

            internal UiaCacheRequest(Condition condition,
                                     TreeScope scope,
                                     AutomationProperty[] properties,
                                     AutomationPattern[] patterns,
                                     AutomationElementMode automationElementMode)
            {
                _condition = condition;
                _scope = scope;
                _automationElementMode = automationElementMode;

                _properties = properties;
                _patterns = patterns;

                IntPtr dataStart = Marshal.AllocCoTaskMem((properties.Length + patterns.Length) * sizeof(int));
                unsafe
                {
                    int* pdata = (int*)dataStart;
                    _pProperties = (IntPtr)pdata;
                    _propertyCount = properties.Length;
                    for (int i = 0; i < properties.Length; i++)
                    {
                        *pdata++ = properties[i].Id;
                    }

                    _pPatterns = (IntPtr)pdata;
                    _patternCount = patterns.Length;
                    for (int i = 0; i < patterns.Length; i++)
                    {
                        *pdata++ = patterns[i].Id;
                    }
                }
            }

            ~UiaCacheRequest()
            {
                if (_pProperties != IntPtr.Zero)
                {
                    // Since _pProperties points to the start of one allocation block
                    // that is used for both properties and patterns, this covers both...
                    Marshal.FreeCoTaskMem(_pProperties);
                }
            }
        }

        // This is not used in any of the DLL entry points - they have
        // separate our params for the data and strings - but is used
        // to hand back the data+strings in one unit to the ClientAPI caller.
        internal struct UiaCacheResponse
        {
            private object[,] _requestedData;
            private string _treeStructure;

            // Note - this takes ownership of the requestedData array, and modifies it in-place
            // (replacing VT_I4/VT_I8 node/pattern references with SafeHandles)
            internal UiaCacheResponse(object[,] requestedData, string treeStructure, UiaCacheRequest request)
            {
                _requestedData = requestedData;
                _treeStructure = treeStructure;
                ConvertFromComTypesToClrTypes(_requestedData, request);
            }

            internal object[,] RequestedData { get { return _requestedData; } }
            internal string TreeStructure { get { return _treeStructure; } }

            private static void ConvertFromComTypesToClrTypes(object[,] data, UiaCacheRequest request)
            {
                // Handle empty case...
                if (data == null)
                {
                    return;
                }

                // Go through the data, and convert any references to an appropriate SafeHandle type -
                // this ensures that they get released properly during cleanup.
                //
                // Note that there is a time window between getting the array back and having all the handles
                // converted where a 'rude unload' (eg. by a host such as Yukon) could cause a handle leak,
                // would need to change the underlying API to avoid this. May consider that in Beta2,
                // but for Beta1, this does the job.
                for(int objIndex = 0 ; objIndex < data.GetLength(0) ; objIndex++ )
                {
                    // Handle position 0, which can be a hnode...
                    if (request._automationElementMode == AutomationElementMode.Full)
                    {
                        object val = data[objIndex, 0];
                        if (val != null)
                        {
                            SafeNodeHandle safeHandle = UiaHUiaNodeFromVariant(val);
                            data[objIndex, 0] = safeHandle;
                        }
                    }

                    // Handle properties - these can be other nodes, or in some cases, arrays of nodes
                    // Use the corresponding object converter from Schema to do the work here (these convert
                    // from int to enum, int[] to Rect, etc.)
                    for (int propertyIndex = 0; propertyIndex < request.Properties.Length; propertyIndex++)
                    {
                        // Convert property from the VARIANT in the array to a more CLR-friendly
                        // value - eg. enums are in the array as ints, so need to cast before
                        // returning.
                        object val = data[objIndex, 1 + propertyIndex];
                        if (val == null || val == AutomationElement.NotSupported || UiaCoreApi.IsErrorMarker(val, false/*throwException*/))
                            continue;

                        AutomationPropertyInfo pi;
                        if (Schema.GetPropertyInfo(request.Properties[propertyIndex], out pi))
                        {
                            if (pi.ObjectConverter != null)
                            {
                                data[objIndex, 1 + propertyIndex] = pi.ObjectConverter(val);
                            }
                        }
                        else
                        {
                            Debug.Assert(false, "unsupported property should not have made it this far");
                        }
                    }

                    // Handle patterns
                    int patternBaseIndex = 1 + request.Properties.Length;
                    for (int patternIndex = 0; patternIndex < request.Patterns.Length; patternIndex++)
                    {
                        object val = data[objIndex, patternBaseIndex + patternIndex];
                        if (val != null)
                        {
                            // Just wrap patterns to a SafeHandle, not a full pattern object, since patten
                            // object reqire a AutomationElement reference.
                            SafePatternHandle hpatternobj = UiaHPatternObjectFromVariant(val);
                            data[objIndex, patternBaseIndex + patternIndex] = hpatternobj;
                        }
                    }
                }
            }
        }

        // Subset of UiaCacheRequest for marshalling...
        [StructLayout(LayoutKind.Sequential)]
        private class UiaMiniCacheRequest
        {
            private IntPtr _pCondition;
            private TreeScope _scope;
            private IntPtr _pProperties;
            private int _propertyCount;
            private IntPtr _pPatterns;
            private int _patternCount;
            private AutomationElementMode _automationElementMode;


            internal UiaMiniCacheRequest(UiaCacheRequest cr, IntPtr conditionPtr)
            {
                _pCondition = conditionPtr;
                _scope = cr._scope;
                _pProperties = cr._pProperties;
                _propertyCount = cr._propertyCount;
                _pPatterns = cr._pPatterns;
                _patternCount = cr._patternCount;
                _automationElementMode = cr._automationElementMode;
            }
        }


        #endregion CacheRequest/Response

        //------------------------------------------------------
        //
        //  Other API types
        //
        //------------------------------------------------------

        #region Other
        [StructLayout(LayoutKind.Sequential)]
        internal struct UiaFindParams
        {
            internal int MaxDepth;
            internal bool FindFirst;
            internal bool ExcludeRoot;
            internal IntPtr pFindCondition;
        };

        internal enum AutomationIdType
        {
            Property,
            Pattern,
            Event,
            ControlType,
            TextAttribute
        }

        internal enum NormalizeState
        {
            None,
            View,
            Custom
        }

        internal const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
        internal const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);
        internal const int UIA_E_NOCLICKABLEPOINT = unchecked((int)0x80040202);
        internal const int UIA_E_PROXYASSEMBLYNOTLOADED = unchecked((int)0x80040203);

        internal delegate void UiaEventCallback(IntPtr args,
                               [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] object[,] pRequestedData,
                               [MarshalAs(UnmanagedType.BStr)] string pTreeStructure);

        internal const int UiaHwndRuntimeIdBase = 42;

        #endregion Other

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        //
        // Client-side methods...
        //
        #region Client methods

        internal static UiaCacheResponse UiaNodeFromPoint(double x, double y, UiaCacheRequest request)
        {
            string treeStructure;
            object[,] requestedData;

            // Note on marshalling of CacheRequest: The UiaCacheRequest class contains extra fields that we use in managed code
            // (eg, managed versions of the arrays) that we can't marshal, so we use another class, UiaMiniCacheRequest, that contains
            // just the interop stuff. This needs to contain a pointer to the condition; but since we can't embed SafeHandles in structs,
            // it needs to be an IntPtr - hence the use of DangerousGetHandle here. We then use KeepAlive to ensure the condition
            // (and its unmanaged alloced memory) stays around till we're done. This is necessary because we're passing it as an IntPtr
            // within a struct, which is invisible to GC.
            // Note that Condition isn't vulnerable to IntPtr/handle recycling issues, since Condition is immutable and is tied to the
            // lifetime of the underlying object (you'd need to have a Close/Dispose equivalent in order to be vulnerable to recycling;
            // without one, your handle can't become stale.)
            UiaMiniCacheRequest miniCR = new UiaMiniCacheRequest(request, request._condition._safeHandle.DangerousGetHandle());
            CheckError(RawUiaNodeFromPoint(x, y, miniCR, out requestedData, out treeStructure));
            GC.KeepAlive(request._condition); // keep condition (and associated unmanaged memory) alive during call
            
            return new UiaCacheResponse(requestedData, treeStructure, request);
        }

        internal static UiaCacheResponse UiaNodeFromFocus(UiaCacheRequest request)
        {
            string treeStructure;
            object[,] requestedData;

            UiaMiniCacheRequest miniCR = new UiaMiniCacheRequest(request, request._condition._safeHandle.DangerousGetHandle());
            CheckError(RawUiaNodeFromFocus(miniCR, out requestedData, out treeStructure));
            GC.KeepAlive(request._condition); // keep condition (and associated unmanaged memory) alive during call

            return new UiaCacheResponse(requestedData, treeStructure, request);
        }

        internal static UiaCacheResponse UiaGetUpdatedCache(SafeNodeHandle hnode, UiaCacheRequest request, NormalizeState normalize, Condition customCondition)
        {
            string treeStructure;
            object[,] requestedData;

            UiaMiniCacheRequest miniCR = new UiaMiniCacheRequest(request, request._condition._safeHandle.DangerousGetHandle());
            CheckError(RawUiaGetUpdatedCache(hnode, miniCR, normalize, customCondition == null ? SafeConditionMemoryHandle.NullHandle : customCondition._safeHandle, out requestedData, out treeStructure));
            GC.KeepAlive(request._condition); // keep condition (and associated unmanaged memory) alive during call

            return new UiaCacheResponse(requestedData, treeStructure, request);
        }

        internal static void UiaGetPropertyValue(SafeNodeHandle hnode, int propertyId, out object value)
        {
            CheckError(RawUiaGetPropertyValue(hnode, propertyId, out value));
        }

        internal static SafePatternHandle UiaGetPatternProvider(SafeNodeHandle hnode, int patternId)
        {
            SafePatternHandle hobj;
            CheckError(RawUiaGetPatternProvider(hnode, patternId, out hobj));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (hobj == null)
            {
                hobj = new SafePatternHandle();
            }
            return hobj;
        }

        internal static int[] UiaGetRuntimeId(SafeNodeHandle hnode)
        {
            int[] runtimeId;
            CheckError(RawUiaGetRuntimeId(hnode, out runtimeId));
            return runtimeId;
        }

        internal static void UiaSetFocus(SafeNodeHandle hnode)
        {
            CheckError(RawUiaSetFocus(hnode));
        }

        internal static UiaCacheResponse UiaNavigate(SafeNodeHandle hnode, NavigateDirection direction, Condition condition, UiaCacheRequest request)
        {
            string treeStructure;
            object[,] requestedData;

            UiaMiniCacheRequest miniCR = new UiaMiniCacheRequest(request, request._condition._safeHandle.DangerousGetHandle());
            CheckError(RawUiaNavigate(hnode, direction, condition._safeHandle, miniCR, out requestedData, out treeStructure));
            GC.KeepAlive(request._condition); // keep condition (and associated unmanaged memory) alive during call

            return new UiaCacheResponse(requestedData, treeStructure, request);
        }

        internal static UiaCacheResponse[] UiaFind(SafeNodeHandle hnode, UiaFindParams findParams, Condition findCondition, UiaCacheRequest request)
        {
            // The native API for this returns separate arrays of data and tree structure strings,
            // one element each for each found element. After it returns, we need to merge corresponding
            // entries to give a single array of UiaCacheResponse objects...
            object[,] requestedData;
            int[] offsets;
            string[] treeStructures;

            UiaMiniCacheRequest miniCR = new UiaMiniCacheRequest(request, request._condition._safeHandle.DangerousGetHandle());
            findParams.pFindCondition = findCondition._safeHandle.DangerousGetHandle();
            CheckError(RawUiaFind(hnode, ref findParams, miniCR, out requestedData, out offsets, out treeStructures));
            GC.KeepAlive(request._condition); // keep condition (and associated unmanaged memory) alive during call
            GC.KeepAlive(findCondition);

            if (requestedData == null)
            {
                Debug.Assert(offsets == null && treeStructures == null, "if nothin found, all out params shoud be null");
                return new UiaCacheResponse[] {}; // Return empty cacheresponse, not null.
            }

            Debug.Assert(offsets.Length == treeStructures.Length);

            // Now do the actual merge...
            UiaCacheResponse[] responses = new UiaCacheResponse[treeStructures.Length];

            int properties = requestedData.GetLength(1);
            for (int i = 0; i < treeStructures.Length; i++)
            {
                int startRow = offsets[i];
                int endRow = i < treeStructures.Length - 1 ? offsets[i + 1] : requestedData.GetLength(0);
                int elements = endRow - startRow;

                object[,] elementData = new object[elements, properties];
                for (int e = 0; e < elements; e++)
                    for (int p = 0; p < properties; p++)
                        elementData[e, p] = requestedData[e + startRow, p];

                responses[i] = new UiaCacheResponse(elementData, treeStructures[i], request);
            }

            return responses;
        }

        internal static SafeNodeHandle UiaNodeFromHandle(IntPtr hwnd)
        {
            SafeNodeHandle hnode;
            CheckError(RawUiaNodeFromHandle(hwnd, out hnode));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (hnode == null)
            {
                hnode = new SafeNodeHandle();
            }
            return hnode;
        }

        internal static SafeNodeHandle UiaGetRootNode()
        {
            SafeNodeHandle hnode;
            CheckError(RawUiaGetRootNode(out hnode));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (hnode == null)
            {
                hnode = new SafeNodeHandle();
            }
            return hnode;
        }

        internal static SafeNodeHandle UiaNodeFromProvider(IRawElementProviderSimple provider)
        {
            SafeNodeHandle hnode;
            CheckError(RawUiaNodeFromProvider(provider, out hnode));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (hnode == null)
            {
                hnode = new SafeNodeHandle();
            }
            return hnode;
        }

        internal static SafeNodeHandle UiaHUiaNodeFromVariant(object var)
        {
            SafeNodeHandle hnode;
            CheckError(RawUiaHUiaNodeFromVariant(ref var, out hnode));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (hnode == null)
            {
                hnode = new SafeNodeHandle();
            }
            return hnode;
        }

        internal static SafePatternHandle UiaHPatternObjectFromVariant(object var)
        {
            SafePatternHandle hobj;
            CheckError(RawUiaHPatternObjectFromVariant(ref var, out hobj));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (hobj == null)
            {
                hobj = new SafePatternHandle();
            }
            return hobj;
        }

        internal static SafeTextRangeHandle UiaHTextRangeFromVariant(object var)
        {
            SafeTextRangeHandle hobj;
            CheckError(RawUiaHTextRangeFromVariant(ref var, out hobj));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (hobj == null)
            {
                hobj = new SafeTextRangeHandle();
            }
            return hobj;
        }

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaHasServerSideProvider", CharSet = CharSet.Unicode)]
        internal static extern bool UiaHasServerSideProvider(IntPtr hwnd);

        internal static bool UiaNodeRelease(IntPtr hnode)
        {
            return RawUiaNodeRelease( hnode );
        }

        internal static bool UiaPatternRelease(IntPtr hobj)
        {
            return RawUiaPatternRelease( hobj );
        }

        internal static bool UiaTextRangeRelease(IntPtr hobj)
        {
            return RawUiaTextRangeRelease( hobj );
        }

        #endregion Client methods

        //
        // Event methods (client only)
        //
        #region Event methods

        internal static SafeEventHandle UiaAddEvent(SafeNodeHandle hnode, int eventId, UiaEventCallback callback, TreeScope scope, int[] properties, UiaCacheRequest request)
        {
            SafeEventHandle hevent;

            UiaMiniCacheRequest miniCR = new UiaMiniCacheRequest(request, request._condition._safeHandle.DangerousGetHandle());
            CheckError(RawUiaAddEvent(hnode, eventId, callback, scope, properties, properties == null ? 0 : properties.Length, miniCR, out hevent));
            GC.KeepAlive(request._condition); // keep condition (and associated unmanaged memory) alive during call

            return hevent;
        }

        internal static void UiaRemoveEvent(IntPtr hevent)
        {
            CheckError(RawUiaRemoveEvent(hevent));
        }

        internal static void UiaEventAddWindow(SafeEventHandle hevent, IntPtr hwnd)
        {
            CheckError(RawUiaEventAddWindow(hevent, hwnd));
        }

        internal static void UiaEventRemoveWindow(SafeEventHandle hevent, IntPtr hwnd)
        {
            CheckError(RawUiaEventRemoveWindow(hevent, hwnd));
        }
        #endregion Event methods


        //
        // EventArgs translation
        //
        #region EventArgs translation


        // Build an int[] from an array of ints in memory at the specified address
        // Used to pull runtimeIDs from C/interop structs
        private static int[] ArrayFromIntPtr(IntPtr pInts, int cInts)
        {
            if (pInts == IntPtr.Zero)
                return null;
            int[] array = new int[cInts];
            unsafe
            {
                for (int i = 0; i < cInts; i++)
                {
                    array[i] = *(((int*)pInts) + i);
                }
            }
            return array;
        }


        // UiaCore calls the delegate directly, with an IntPtr for the eventArgs,
        // so the managed callback has to use this to parse those unmanaged args
        // into something more managable.
        internal static AutomationEventArgs GetUiaEventArgs(IntPtr argsAddr)
        {
            UiaEventArgs args = (UiaEventArgs)Marshal.PtrToStructure(argsAddr, typeof(UiaEventArgs));

            AutomationEvent eventId = AutomationEvent.LookupById(args._eventId);
            if (eventId == null)
            {
                Debug.Assert(false, "Got unknown eventId from core: " + args._eventId);
                return null;
            }

            switch (args._type)
            {
                case EventArgsType.Simple:
                    {
                        return new AutomationEventArgs(eventId);
                    }

                case EventArgsType.PropertyChanged:
                    {
                        UiaPropertyChangedEventArgs pcargs = (UiaPropertyChangedEventArgs)Marshal.PtrToStructure(argsAddr, typeof(UiaPropertyChangedEventArgs));

                        AutomationProperty propertyId = AutomationProperty.LookupById(pcargs._propertyId);
                        if (propertyId == null)
                        {
                            Debug.Assert(false, "Got unknown propertyId from core: " + pcargs._propertyId);
                            return null;
                        }

                        return new AutomationPropertyChangedEventArgs(propertyId, pcargs._oldValue, pcargs._newValue);
                    }

                case EventArgsType.StructureChanged:
                    {
                        UiaStructureChangedEventArgs scargs = (UiaStructureChangedEventArgs)Marshal.PtrToStructure(argsAddr, typeof(UiaStructureChangedEventArgs));
                        int[] runtimeId = ArrayFromIntPtr(scargs._pRuntimeId, scargs._cRuntimeIdLen);
                        return new StructureChangedEventArgs(scargs._structureChangeType, runtimeId);
                    }

                case EventArgsType.AsyncContentLoaded:
                    {
                        UiaAsyncContentLoadedEventArgs aclargs = (UiaAsyncContentLoadedEventArgs)Marshal.PtrToStructure(argsAddr, typeof(UiaAsyncContentLoadedEventArgs));
                        return new AsyncContentLoadedEventArgs(aclargs._asyncContentLoadedState, aclargs._percentComplete);
                    }

                case EventArgsType.WindowClosed:
                    {
                        UiaWindowClosedEventArgs wcargs = (UiaWindowClosedEventArgs)Marshal.PtrToStructure(argsAddr, typeof(UiaWindowClosedEventArgs));
                        int[] runtimeId = ArrayFromIntPtr(wcargs._pRuntimeId, wcargs._cRuntimeIdLen);
                        return new WindowClosedEventArgs(runtimeId);
                    }
            }

            Debug.Assert(false, "Unknown event type from core:" + args._type);
            return null;
        }
        #endregion EventArgs translation

        //
        // Pattern methods...
        //
        #region Pattern methods

        internal static void DockPattern_SetDockPosition(SafePatternHandle hobj, DockPosition dockPosition)
        {
            CheckError(RawDockPattern_SetDockPosition(hobj, dockPosition));
        }

        internal static void ExpandCollapsePattern_Collapse(SafePatternHandle hobj)
        {
            CheckError(RawExpandCollapsePattern_Collapse(hobj));
        }

        internal static void ExpandCollapsePattern_Expand(SafePatternHandle hobj)
        {
            CheckError(RawExpandCollapsePattern_Expand(hobj));
        }

        internal static SafeNodeHandle GridPattern_GetItem(SafePatternHandle hobj, int row, int column)
        {
            SafeNodeHandle result;
            CheckError(RawGridPattern_GetItem(hobj, row, column, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeNodeHandle();
            }
            return result;
        }

        internal static void InvokePattern_Invoke(SafePatternHandle hobj)
        {
            CheckError(RawInvokePattern_Invoke(hobj));
        }

        internal static string MultipleViewPattern_GetViewName(SafePatternHandle hobj, int viewId)
        {
            string result;
            CheckError(RawMultipleViewPattern_GetViewName(hobj, viewId, out result));
            return result;
        }

        internal static void MultipleViewPattern_SetCurrentView(SafePatternHandle hobj, int viewId)
        {
            CheckError(RawMultipleViewPattern_SetCurrentView(hobj, viewId));
        }

        internal static void RangeValuePattern_SetValue(SafePatternHandle hobj, double val)
        {
            CheckError(RawRangeValuePattern_SetValue(hobj, val));
        }

        internal static void ScrollItemPattern_ScrollIntoView(SafePatternHandle hobj)
        {
            CheckError(RawScrollItemPattern_ScrollIntoView(hobj));
        }

        internal static void ScrollPattern_Scroll(SafePatternHandle hobj, ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            CheckError(RawScrollPattern_Scroll(hobj, horizontalAmount, verticalAmount));
        }

        internal static void ScrollPattern_SetScrollPercent(SafePatternHandle hobj, double horizontalPercent, double verticalPercent)
        {
            CheckError(RawScrollPattern_SetScrollPercent(hobj, horizontalPercent, verticalPercent));
        }

        internal static void SelectionItemPattern_AddToSelection(SafePatternHandle hobj)
        {
            CheckError(RawSelectionItemPattern_AddToSelection(hobj));
        }

        internal static void SelectionItemPattern_RemoveFromSelection(SafePatternHandle hobj)
        {
            CheckError(RawSelectionItemPattern_RemoveFromSelection(hobj));
        }

        internal static void SelectionItemPattern_Select(SafePatternHandle hobj)
        {
            CheckError(RawSelectionItemPattern_Select(hobj));
        }

        internal static void TogglePattern_Toggle(SafePatternHandle hobj)
        {
            CheckError(RawTogglePattern_Toggle(hobj));
        }

        internal static void TransformPattern_Move(SafePatternHandle hobj, double x, double y)
        {
            CheckError(RawTransformPattern_Move(hobj, x, y));
        }

        internal static void TransformPattern_Resize(SafePatternHandle hobj, double width, double height)
        {
            CheckError(RawTransformPattern_Resize(hobj, width, height));
        }

        internal static void TransformPattern_Rotate(SafePatternHandle hobj, double degrees)
        {
            CheckError(RawTransformPattern_Rotate(hobj, degrees));
        }

        internal static void ValuePattern_SetValue(SafePatternHandle hobj, string pVal)
        {
            CheckError(RawValuePattern_SetValue(hobj, pVal));
        }

        internal static void WindowPattern_Close(SafePatternHandle hobj)
        {
            CheckError(RawWindowPattern_Close(hobj));
        }

        internal static void WindowPattern_SetWindowVisualState(SafePatternHandle hobj, WindowVisualState state)
        {
            CheckError(RawWindowPattern_SetWindowVisualState(hobj, state));
        }
        
        internal static bool WindowPattern_WaitForInputIdle(SafePatternHandle hobj, int milliseconds)
        {
            bool result;
            CheckError(RawWindowPattern_WaitForInputIdle(hobj, milliseconds, out result));
            return result;
        }

        
        internal static void SynchronizedInputPattern_StartListening(SafePatternHandle hobj, SynchronizedInputType inputType)
        {
            CheckError(RawSynchronizedInputPattern_StartListening(hobj, inputType));
           
        }
        
        internal static void SynchronizedInputPattern_Cancel(SafePatternHandle hobj)
        {
            CheckError(RawSynchronizedInputPattern_Cancel(hobj));
            
        }

        internal static void VirtualizedItemPattern_Realize(SafePatternHandle hobj)
        {
            CheckError(RawVirtualizedItemPattern_Realize(hobj));
        }

        internal static SafeNodeHandle ItemContainerPattern_FindItemByProperty(SafePatternHandle hobj, SafeNodeHandle hNode, int propertyId, object value)
        {
            SafeNodeHandle result;
            CheckError(RawItemContainerPattern_FindItemByProperty(hobj, hNode, propertyId, value, out result));
            return result;
        }
        #endregion Pattern methods

        //
        // Text methods...
        //
        #region Text methods

        internal static SafeTextRangeHandle [] TextPattern_GetSelection(SafePatternHandle hobj)
        {
            object[] arr;
            CheckError(RawTextPattern_GetSelection(hobj, out arr));
            if (arr == null)
            {
                return new SafeTextRangeHandle[] { };
            }
            SafeTextRangeHandle[] result = new SafeTextRangeHandle[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = UiaHTextRangeFromVariant(arr[i]);
            }
            return result;
        }

        internal static SafeTextRangeHandle[] TextPattern_GetVisibleRanges(SafePatternHandle hobj)
        {
            object[] arr;
            CheckError(RawTextPattern_GetVisibleRanges(hobj, out arr));
            if (arr == null)
            {
                return new SafeTextRangeHandle[] { };
            }
            SafeTextRangeHandle[] result = new SafeTextRangeHandle[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = UiaHTextRangeFromVariant(arr[i]);
            }
            return result;
        }

        internal static SafeTextRangeHandle TextPattern_RangeFromChild(SafePatternHandle hobj, SafeNodeHandle childElement)
        {
            SafeTextRangeHandle result;
            CheckError(RawTextPattern_RangeFromChild(hobj, childElement, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeTextRangeHandle();
            }
            return result;
        }

        internal static SafeTextRangeHandle TextPattern_RangeFromPoint(SafePatternHandle hobj, Point point)
        {
            SafeTextRangeHandle result;
            CheckError(RawTextPattern_RangeFromPoint(hobj, point, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeTextRangeHandle();
            }
            return result;
        }

        internal static SafeTextRangeHandle TextPattern_get_DocumentRange(SafePatternHandle hobj)
        {
            SafeTextRangeHandle result;
            CheckError(RawTextPattern_get_DocumentRange(hobj, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeTextRangeHandle();
            }
            return result;
        }

        internal static SupportedTextSelection TextPattern_get_SupportedTextSelection(SafePatternHandle hobj)
        {
            SupportedTextSelection result;
            CheckError(RawTextPattern_get_SupportedTextSelection(hobj, out result));
            return result;
        }

        internal static SafeTextRangeHandle TextRange_Clone(SafeTextRangeHandle hobj)
        {
            SafeTextRangeHandle result;
            CheckError(RawTextRange_Clone(hobj, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeTextRangeHandle();
            }
            return result;
        }

        internal static bool TextRange_Compare(SafeTextRangeHandle hobj, SafeTextRangeHandle range)
        {
            bool result;
            CheckError(RawTextRange_Compare(hobj, range, out result));
            return result;
        }

        internal static int TextRange_CompareEndpoints(SafeTextRangeHandle hobj, TextPatternRangeEndpoint endpoint, SafeTextRangeHandle targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            int result;
            CheckError(RawTextRange_CompareEndpoints(hobj, endpoint, targetRange, targetEndpoint, out result));
            return result;
        }

        internal static void TextRange_ExpandToEnclosingUnit(SafeTextRangeHandle hobj, TextUnit unit)
        {
            CheckError(RawTextRange_ExpandToEnclosingUnit(hobj, unit));
        }

        internal static SafeTextRangeHandle TextRange_FindAttribute(SafeTextRangeHandle hobj, int attributeId, object val, bool backward)
        {
            SafeTextRangeHandle result;
            CheckError(RawTextRange_FindAttribute(hobj, attributeId, val, backward, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeTextRangeHandle();
            }
            return result;
        }

        internal static SafeTextRangeHandle TextRange_FindText(SafeTextRangeHandle hobj, string text, bool backward, bool ignoreCase)
        {
            SafeTextRangeHandle result;
            CheckError(RawTextRange_FindText(hobj, text, backward, ignoreCase, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeTextRangeHandle();
            }
            return result;
        }

        internal static object TextRange_GetAttributeValue(SafeTextRangeHandle hobj, int attributeId)
        {
            object result;
            CheckError(RawTextRange_GetAttributeValue(hobj, attributeId, out result));
            return result;
        }

        internal static Rect[] TextRange_GetBoundingRectangles(SafeTextRangeHandle hobj)
        {
            double[] doubles;
            CheckError(RawTextRange_GetBoundingRectangles(hobj, out doubles));
            if (doubles == null)
            {
                return null;
            }
            int count = doubles.Length / 4;
            int leftover = doubles.Length % 4;
            if (leftover != 0)
                return null;

            Rect[] rects = new Rect[count];
            int scan = 0;
            for (int i = 0; i < count; i++)
            {
                double x = doubles[scan++];
                double y = doubles[scan++];
                double width = doubles[scan++];
                double height = doubles[scan++];
                if(width <= 0 || height <= 0)
                    rects[i] = Rect.Empty;
                else
                    rects[i] = new Rect(x, y, width, height);
            }

            return rects;
        }

        internal static SafeNodeHandle TextRange_GetEnclosingElement(SafeTextRangeHandle hobj)
        {
            SafeNodeHandle result;
            CheckError(RawTextRange_GetEnclosingElement(hobj, out result));
            // Whidbey RTM SafeHandle/PInvoke bug workaround - SafeHandles on 64 can come back
            // as null, should be non-null but Invalid. This fixes up to non-null.
            if (result == null)
            {
                result = new SafeNodeHandle();
            }
            return result;
        }

        internal static string TextRange_GetText(SafeTextRangeHandle hobj, int maxLength)
        {
            string result;
            CheckError(RawTextRange_GetText(hobj, maxLength, out result));
            return result;
        }

        internal static int TextRange_Move(SafeTextRangeHandle hobj, TextUnit unit, int count)
        {
            int result;
            CheckError(RawTextRange_Move(hobj, unit, count, out result));
            return result;
        }

        internal static int TextRange_MoveEndpointByUnit(SafeTextRangeHandle hobj, TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            int result;
            CheckError(RawTextRange_MoveEndpointByUnit(hobj, endpoint, unit, count, out result));
            return result;
        }

        internal static void TextRange_MoveEndpointByRange(SafeTextRangeHandle hobj, TextPatternRangeEndpoint endpoint, SafeTextRangeHandle targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            CheckError(RawTextRange_MoveEndpointByRange(hobj, endpoint, targetRange, targetEndpoint));
        }

        internal static void TextRange_Select(SafeTextRangeHandle hobj)
        {
            CheckError(RawTextRange_Select(hobj));
        }

        internal static void TextRange_AddToSelection(SafeTextRangeHandle hobj)
        {
            CheckError(RawTextRange_AddToSelection(hobj));
        }

        internal static void TextRange_RemoveFromSelection(SafeTextRangeHandle hobj)
        {
            CheckError(RawTextRange_RemoveFromSelection(hobj));
        }

        internal static void TextRange_ScrollIntoView(SafeTextRangeHandle hobj, bool alignToTop)
        {
            CheckError(RawTextRange_ScrollIntoView(hobj, alignToTop));
        }

        internal static object[] TextRange_GetChildren(SafeTextRangeHandle hobj)
        {
            object[] result;
            CheckError(RawTextRange_GetChildren(hobj, out result));
            return result;
        }

        #endregion Text methods

        //
        // Other methods...
        //
        internal static bool IsErrorMarker(object val, bool throwException)
        {
            // Errors/exceptions in cacherequest array are represented as
            // an array of two objects, the first is VT_ERROR, which maps
            // to int; and the second is an VT_UNKNOWN/IErrorInfo, which
            // maps to object.
            object[] arr = val as object[];
            if (arr == null || arr.Length != 2)
                return false;
            if (!(arr[0] is int))
                return false;
            // Exception slot can be null, or an IErrorInfo (if from local unmanaged or
            // remote provider), or a CLR Exception object (if from local managed code - Exception
            // implements IErrorInfo, and effectively "passes through" uiacore).
            // IsComObject returns true only for non-CLR (eg. unmanaged) objects, so we have to check
            // for CLR Exception objects (with "is Exception") separately.
            if(arr[1] != null
                && !Marshal.IsComObject(arr[1])
                && !(arr[1] is Exception) )
                return false;

            if(throwException)
            {
                int hr = (int)arr[0];
                object errorInfo = arr[1];
                if (errorInfo != null)
                {
                    // Marshal.ThrowExceptionForHR(hr, IntPtr) seems to ignore the errorInfo, but if we
                    // explicitly set it as a the errorInfo for this thread using SetErrorInfo, it works(!)...
                    // Note that the errorInfo can be null; in which case we just don't use it.
                    IntPtr errorInfoAsIntPtr = Marshal.GetIUnknownForObject(errorInfo);
#pragma warning suppress 6031, 6532, 56031
                    SetErrorInfo(0, errorInfoAsIntPtr); // ignore return value
                    Marshal.Release(errorInfoAsIntPtr);
                }

                Marshal.ThrowExceptionForHR(hr);
            }
            return true;
        }
        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        [DllImport(DllImport.UIAutomationCore, CharSet = CharSet.Unicode)]
        private static extern bool UiaGetErrorDescription([MarshalAs(UnmanagedType.BStr)] out string pDescription);

        [DllImport("oleaut32.dll")]
        private static extern int SetErrorInfo(int dwReserved, IntPtr errorInfo);

        // Check hresult for error, and handle mapping to UIA-specific
        // exceptions...
        private static void CheckError(int hr)
        {
            if (hr >= 0)
            {
                return;
            }

            // Handle these UIA exceptions specially - COM Interop
            // maps others (eg. InvalidArgument), but not these:
            if (hr == UIA_E_ELEMENTNOTENABLED
            || hr == UIA_E_ELEMENTNOTAVAILABLE
            || hr == UIA_E_NOCLICKABLEPOINT
            || hr == UIA_E_PROXYASSEMBLYNOTLOADED)
            {
                string description;
                if (!UiaGetErrorDescription(out description))
                    description = SR.Get(SRID.UnknownCoreAPIError);

                switch (hr)
                {
                    case UIA_E_ELEMENTNOTENABLED:
                        throw new ElementNotEnabledException(description);

                    case UIA_E_ELEMENTNOTAVAILABLE:
                        throw new ElementNotAvailableException(description);

                    case UIA_E_NOCLICKABLEPOINT:
                        throw new NoClickablePointException(description);

                    case UIA_E_PROXYASSEMBLYNOTLOADED:
                        throw new ProxyAssemblyNotLoadedException(description);
                }
            }

            // Not a UIA-specific exception - let COM Interop handle the rest.
            // (Marshal.ThrowExceptionForHR automatically calls GetErrorInfo to fill in the description
            // field - UIA sets the error info itself, so it will get propogated here.)
            Marshal.ThrowExceptionForHR(hr);
        }

        #endregion Private Methods

        #region Raw API methods

        //
        // Client-side methods...
        //

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaGetPropertyValue", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetPropertyValue(SafeNodeHandle hnode, int propertyId, out object value);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaGetPatternProvider", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetPatternProvider(SafeNodeHandle hnode, int patternId, out SafePatternHandle phobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaGetRuntimeId", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetRuntimeId(SafeNodeHandle hnode, [MarshalAs(UnmanagedType.SafeArray)] out int[] runtimeId);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaSetFocus", CharSet = CharSet.Unicode)]
        private static extern int RawUiaSetFocus(SafeNodeHandle hnode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaNavigate", CharSet = CharSet.Unicode)]
        private static extern int RawUiaNavigate(SafeNodeHandle hnode, NavigateDirection direction, SafeConditionMemoryHandle condition, UiaMiniCacheRequest pRequest, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] out object[,] requestedData, [MarshalAs(UnmanagedType.BStr)] out string treeStructure);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaFind", CharSet = CharSet.Unicode)]
        private static extern int RawUiaFind(SafeNodeHandle hnode, ref UiaFindParams pParams, UiaMiniCacheRequest pRequest, [MarshalAs(UnmanagedType.SafeArray)] out object[,] requestedData, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out int[] offsets, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] treeStructures);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaNodeFromHandle", CharSet = CharSet.Unicode)]
        private static extern int RawUiaNodeFromHandle(IntPtr hwnd, out SafeNodeHandle hnode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaNodeFromProvider", CharSet = CharSet.Unicode)]
        private static extern int RawUiaNodeFromProvider(IRawElementProviderSimple provider, out SafeNodeHandle hode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaGetRootNode", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetRootNode(out SafeNodeHandle hnode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaNodeFromPoint", CharSet = CharSet.Unicode)]
        private static extern int RawUiaNodeFromPoint(double x, double y, UiaMiniCacheRequest request, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] out object[,] requestedData, [MarshalAs(UnmanagedType.BStr)] out string treeStructure);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaNodeFromFocus", CharSet = CharSet.Unicode)]
        private static extern int RawUiaNodeFromFocus(UiaMiniCacheRequest pRequest, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] out object[,] requestedData, [MarshalAs(UnmanagedType.BStr)] out string treeStructure);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaGetUpdatedCache", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetUpdatedCache(SafeNodeHandle hnode, UiaMiniCacheRequest pRequest, NormalizeState normalizeState, SafeConditionMemoryHandle pNormalizeCondition, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] out object[,] requestedData, [MarshalAs(UnmanagedType.BStr)] out string treeStructure);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaHUiaNodeFromVariant", CharSet = CharSet.Unicode)]
        private static extern int RawUiaHUiaNodeFromVariant([MarshalAs(UnmanagedType.Struct)] ref object var, out SafeNodeHandle hnode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaHPatternObjectFromVariant", CharSet = CharSet.Unicode)]
        private static extern int RawUiaHPatternObjectFromVariant([MarshalAs(UnmanagedType.Struct)] ref object var, out SafePatternHandle hnode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaHTextRangeFromVariant", CharSet = CharSet.Unicode)]
        private static extern int RawUiaHTextRangeFromVariant([MarshalAs(UnmanagedType.Struct)] ref object var, out SafeTextRangeHandle hnode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaNodeRelease", CharSet = CharSet.Unicode)]
        private static extern bool RawUiaNodeRelease(IntPtr hnode);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaPatternRelease", CharSet = CharSet.Unicode)]
        private static extern bool RawUiaPatternRelease(IntPtr hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaTextRangeRelease", CharSet = CharSet.Unicode)]
        private static extern bool RawUiaTextRangeRelease(IntPtr hobj);
        // Event APIs...

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaAddEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaAddEvent(SafeNodeHandle hnode, int eventId, UiaEventCallback callback, TreeScope scope, [MarshalAs(UnmanagedType.LPArray)] int[] pProperties, int cProperties, UiaMiniCacheRequest pRequest, out SafeEventHandle hevent);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRemoveEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRemoveEvent(IntPtr hevent);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaEventAddWindow", CharSet = CharSet.Unicode)]
        private static extern int RawUiaEventAddWindow(SafeEventHandle hevent, IntPtr hwnd);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaEventRemoveWindow", CharSet = CharSet.Unicode)]
        private static extern int RawUiaEventRemoveWindow(SafeEventHandle hevent, IntPtr hwnd);

        #endregion Raw API methods

        #region Raw Pattern methods

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "DockPattern_SetDockPosition", CharSet = CharSet.Unicode)]
        private static extern int RawDockPattern_SetDockPosition(SafePatternHandle hobj, DockPosition dockPosition);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "ExpandCollapsePattern_Collapse", CharSet = CharSet.Unicode)]
        private static extern int RawExpandCollapsePattern_Collapse(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "ExpandCollapsePattern_Expand", CharSet = CharSet.Unicode)]
        private static extern int RawExpandCollapsePattern_Expand(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "GridPattern_GetItem", CharSet = CharSet.Unicode)]
        private static extern int RawGridPattern_GetItem(SafePatternHandle hobj, int row, int column, out SafeNodeHandle pResult);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "InvokePattern_Invoke", CharSet = CharSet.Unicode)]
        private static extern int RawInvokePattern_Invoke(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "MultipleViewPattern_GetViewName", CharSet = CharSet.Unicode)]
        private static extern int RawMultipleViewPattern_GetViewName(SafePatternHandle hobj, int viewId, [MarshalAs(UnmanagedType.BStr)] out string ppStr);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "MultipleViewPattern_SetCurrentView", CharSet = CharSet.Unicode)]
        private static extern int RawMultipleViewPattern_SetCurrentView(SafePatternHandle hobj, int viewId);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "RangeValuePattern_SetValue", CharSet = CharSet.Unicode)]
        private static extern int RawRangeValuePattern_SetValue(SafePatternHandle hobj, double val);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "ScrollItemPattern_ScrollIntoView", CharSet = CharSet.Unicode)]
        private static extern int RawScrollItemPattern_ScrollIntoView(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "ScrollPattern_Scroll", CharSet = CharSet.Unicode)]
        private static extern int RawScrollPattern_Scroll(SafePatternHandle hobj, ScrollAmount horizontalAmount, ScrollAmount verticalAmount);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "ScrollPattern_SetScrollPercent", CharSet = CharSet.Unicode)]
        private static extern int RawScrollPattern_SetScrollPercent(SafePatternHandle hobj, double horizontalPercent, double verticalPercent);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "SelectionItemPattern_AddToSelection", CharSet = CharSet.Unicode)]
        private static extern int RawSelectionItemPattern_AddToSelection(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "SelectionItemPattern_RemoveFromSelection", CharSet = CharSet.Unicode)]
        private static extern int RawSelectionItemPattern_RemoveFromSelection(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "SelectionItemPattern_Select", CharSet = CharSet.Unicode)]
        private static extern int RawSelectionItemPattern_Select(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TogglePattern_Toggle", CharSet = CharSet.Unicode)]
        private static extern int RawTogglePattern_Toggle(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TransformPattern_Move", CharSet = CharSet.Unicode)]
        private static extern int RawTransformPattern_Move(SafePatternHandle hobj, double x, double y);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TransformPattern_Resize", CharSet = CharSet.Unicode)]
        private static extern int RawTransformPattern_Resize(SafePatternHandle hobj, double width, double height);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TransformPattern_Rotate", CharSet = CharSet.Unicode)]
        private static extern int RawTransformPattern_Rotate(SafePatternHandle hobj, double degrees);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "ValuePattern_SetValue", CharSet = CharSet.Unicode)]
        private static extern int RawValuePattern_SetValue(SafePatternHandle hobj, [MarshalAs(UnmanagedType.LPWStr)] string pVal);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "WindowPattern_Close", CharSet = CharSet.Unicode)]
        private static extern int RawWindowPattern_Close(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "WindowPattern_SetWindowVisualState", CharSet = CharSet.Unicode)]
        private static extern int RawWindowPattern_SetWindowVisualState(SafePatternHandle hobj, WindowVisualState state);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "WindowPattern_WaitForInputIdle", CharSet = CharSet.Unicode)]
        private static extern int RawWindowPattern_WaitForInputIdle(SafePatternHandle hobj, int milliseconds, out bool pResult);
        
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "SynchronizedInputPattern_StartListening", CharSet = CharSet.Unicode)]
        private static extern int RawSynchronizedInputPattern_StartListening(SafePatternHandle hobj, SynchronizedInputType inputType);
        
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "SynchronizedInputPattern_Cancel", CharSet = CharSet.Unicode)]
        private static extern int RawSynchronizedInputPattern_Cancel(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "VirtualizedItemPattern_Realize", CharSet = CharSet.Unicode)]
        private static extern int RawVirtualizedItemPattern_Realize(SafePatternHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "ItemContainerPattern_FindItemByProperty", CharSet = CharSet.Unicode)]
        private static extern int RawItemContainerPattern_FindItemByProperty(SafePatternHandle hobj, SafeNodeHandle startAfter, int propertyId, object value, out SafeNodeHandle result);

        #endregion Raw Pattern methods

        #region Text methods

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextPattern_GetSelection", CharSet = CharSet.Unicode)]
        private static extern int RawTextPattern_GetSelection(SafePatternHandle hobj, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]out object[] result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextPattern_GetVisibleRanges", CharSet = CharSet.Unicode)]
        private static extern int RawTextPattern_GetVisibleRanges(SafePatternHandle hobj, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]out object[] result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextPattern_RangeFromChild", CharSet = CharSet.Unicode)]
        private static extern int RawTextPattern_RangeFromChild(SafePatternHandle hobj, SafeNodeHandle childElement, out SafeTextRangeHandle result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextPattern_RangeFromPoint", CharSet = CharSet.Unicode)]
        private static extern int RawTextPattern_RangeFromPoint(SafePatternHandle hobj, Point point, out SafeTextRangeHandle result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextPattern_get_DocumentRange", CharSet = CharSet.Unicode)]
        private static extern int RawTextPattern_get_DocumentRange(SafePatternHandle hobj, out SafeTextRangeHandle result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextPattern_get_SupportedTextSelection", CharSet = CharSet.Unicode)]
        private static extern int RawTextPattern_get_SupportedTextSelection(SafePatternHandle hobj, out SupportedTextSelection result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_Clone", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_Clone(SafeTextRangeHandle hobj, out SafeTextRangeHandle result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_Compare", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_Compare(SafeTextRangeHandle hobj, SafeTextRangeHandle range, out bool result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_CompareEndpoints", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_CompareEndpoints(SafeTextRangeHandle hobj, TextPatternRangeEndpoint endpoint, SafeTextRangeHandle targetRange, TextPatternRangeEndpoint targetEndpoint, out int result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_ExpandToEnclosingUnit", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_ExpandToEnclosingUnit(SafeTextRangeHandle hobj, TextUnit unit);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_FindAttribute", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_FindAttribute(SafeTextRangeHandle hobj, int attributeId, object val, bool backward, out SafeTextRangeHandle result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_FindText", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_FindText(SafeTextRangeHandle hobj, [MarshalAs(UnmanagedType.BStr)] string text, bool backward, bool ignoreCase, out SafeTextRangeHandle result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_GetAttributeValue", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_GetAttributeValue(SafeTextRangeHandle hobj, int attributeId, out object result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_GetBoundingRectangles", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_GetBoundingRectangles(SafeTextRangeHandle hobj, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_R8)] out double[] result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_GetEnclosingElement", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_GetEnclosingElement(SafeTextRangeHandle hobj, out SafeNodeHandle result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_GetText", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_GetText(SafeTextRangeHandle hobj, int maxLength, [MarshalAs(UnmanagedType.BStr)] out string result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_Move", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_Move(SafeTextRangeHandle hobj, TextUnit unit, int count, out int result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_MoveEndpointByUnit", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_MoveEndpointByUnit(SafeTextRangeHandle hobj, TextPatternRangeEndpoint endpoint, TextUnit unit, int count, out int result);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_MoveEndpointByRange", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_MoveEndpointByRange(SafeTextRangeHandle hobj, TextPatternRangeEndpoint endpoint, SafeTextRangeHandle targetRange, TextPatternRangeEndpoint targetEndpoint);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_Select", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_Select(SafeTextRangeHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_AddToSelection", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_AddToSelection(SafeTextRangeHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_RemoveFromSelection", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_RemoveFromSelection(SafeTextRangeHandle hobj);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_ScrollIntoView", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_ScrollIntoView(SafeTextRangeHandle hobj, bool alignToTop);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "TextRange_GetChildren", CharSet = CharSet.Unicode)]
        private static extern int RawTextRange_GetChildren(SafeTextRangeHandle hobj, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] out object[] result);

        #endregion Text methods

        //------------------------------------------------------
        //
        //  Private Types (Event support)
        //
        //------------------------------------------------------

        #region Private types

        // Unmanaged equivalents of the various EventArgs,
        // used by GetUiaEventArgs.

        private enum EventArgsType
        {
            Simple,
            PropertyChanged,
            StructureChanged,
            AsyncContentLoaded,
            WindowClosed
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UiaEventArgs
        {
            internal EventArgsType _type;
            internal int _eventId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UiaPropertyChangedEventArgs
        {
            internal EventArgsType _type;
            internal int _eventId;
            internal int _propertyId;
            [MarshalAs(UnmanagedType.Struct)] // UnmanagedType.Struct == use VARIANT
            internal object _oldValue;
            [MarshalAs(UnmanagedType.Struct)] // UnmanagedType.Struct == use VARIANT
            internal object _newValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UiaStructureChangedEventArgs
        {
            internal EventArgsType _type;
            internal int _eventId;
            internal StructureChangeType _structureChangeType;
            internal IntPtr _pRuntimeId;
            internal int _cRuntimeIdLen;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UiaAsyncContentLoadedEventArgs
        {
            internal EventArgsType _type;
            internal int _eventId;
            internal AsyncContentLoadedState _asyncContentLoadedState;
            internal double _percentComplete;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UiaWindowClosedEventArgs
        {
            internal EventArgsType _type;
            internal int _eventId;
            internal IntPtr _pRuntimeId;
            internal int _cRuntimeIdLen;
        }

        #endregion Private types

        //------------------------------------------------------
        //
        //  Static ctor & Proxy callback
        //
        //------------------------------------------------------

        #region static ctor and proxy callback

        private enum ProviderType
        {
            BaseHwnd,
            Proxy,
            NonClientArea,
        };

        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
        private delegate IRawElementProviderSimple[] UiaProviderCallback(IntPtr hwnd, ProviderType providerType);

        [DllImport(DllImport.UIAutomationCore, CharSet = CharSet.Unicode)]
        private static extern void UiaRegisterProviderCallback(UiaProviderCallback pCallback);

        static GCHandle _gchandle;

        static UiaCoreApi()
        {
            UiaProviderCallback onGetProviderDelegate = new UiaProviderCallback(OnGetProvider);
            _gchandle = GCHandle.Alloc(onGetProviderDelegate);
            UiaRegisterProviderCallback(onGetProviderDelegate);
        }

        static private
        IRawElementProviderSimple [] OnGetProvider(IntPtr hwnd, ProviderType providerType)
        {
            IRawElementProviderSimple provider;
            try
            {
                switch (providerType)
                {
                    case ProviderType.BaseHwnd:
                        provider = new HwndProxyElementProvider(NativeMethods.HWND.Cast(hwnd));
                        break;

                    case ProviderType.Proxy:
                        provider = ProxyManager.ProxyProviderFromHwnd(NativeMethods.HWND.Cast(hwnd), 0, UnsafeNativeMethods.OBJID_CLIENT);
                        break;

                    case ProviderType.NonClientArea:
                        provider = ProxyManager.GetNonClientProvider(hwnd);
                        break;

                    default:
                        provider = null;
                        break;
                }

                if (provider == null)
                    return null;
                return new IRawElementProviderSimple[] { provider };
            }
#pragma warning suppress 56500
            catch (Exception)
            {
                // Must catch *all* exceptions here, even critical ones,
                // since this is a callback called by unmanaged code.
                // (COM interop effectively translates all exceptions into
                // HR codes for COM interfaces, but here we have to do it
                // manually.)
                return null;
            }
        }

        #endregion static ctor and proxy callback
    }
}
