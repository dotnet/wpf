// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//             This class provides a convenient way to persist/depersist the events on a PageFunction
//
//              By calling the _Detach() method on a pagefunction, 
//               this class will build a list of the class & methods on that Pagefunction,
//               as well as removing the current listener on the class when it's done. 
//
//               By passing in a pagefunction on the _Attach method, the class will reattach the 
//               saved list to the calling pagefunction
// 

using System;
using System.Windows.Navigation;
using System.Windows;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Security;

namespace MS.Internal.AppModel
{
    [Serializable]
    internal struct ReturnEventSaverInfo
    {
        internal ReturnEventSaverInfo(string delegateTypeName, string targetTypeName, string delegateMethodName, bool fSamePf)
        {
            _delegateTypeName = delegateTypeName;
            _targetTypeName = targetTypeName;
            _delegateMethodName = delegateMethodName;
            _delegateInSamePF = fSamePf;
        }

        internal String _delegateTypeName;
        internal String _targetTypeName;
        internal String _delegateMethodName;
        internal bool _delegateInSamePF;   // Return Event handler comes from the same pagefunction, this is for non-generic workaround.
    }

    [Serializable]
    internal class ReturnEventSaver
    {
        internal ReturnEventSaver()
        {
        }


        internal void _Detach(PageFunctionBase pf)
        {
            if (pf._Return != null && pf._Saver == null)
            {
                ReturnEventSaverInfo[] list = null;

                Delegate[] delegates = null;

                delegates = (pf._Return).GetInvocationList();
                list = _returnList = new ReturnEventSaverInfo[delegates.Length];

                for (int i = 0; i < delegates.Length; i++)
                {
                    Delegate returnDelegate = delegates[i];
                    bool bSamePf = false;

                    if (returnDelegate.Target == pf)
                    {
                        // This is the Event Handler implemented by the same PF, use for NonGeneric handling.
                        bSamePf = true;
                    }

                    MethodInfo m = returnDelegate.Method;
                    ReturnEventSaverInfo info = new ReturnEventSaverInfo(
                        returnDelegate.GetType().AssemblyQualifiedName,
                        returnDelegate.Target.GetType().AssemblyQualifiedName,
                        m.Name, bSamePf);

                    list[i] = info;
                }

                //
                // only save if there were delegates already attached. 
                // note that there will be cases where the Saver has already been pre-populated from a Load
                // but no delegates have been created yet ( as the PF hasn`t called finish as yet) 
                //
                // By only storing the saver once there are delegates - we avoid the problem of 
                // wiping out any newly restored saver 
                pf._Saver = this;
            }

            pf._DetachEvents();
        }


        //
        // Attach the stored events to the supplied pagefunction. 
        // 
        // caller  - the Calling Page's root element. We will reattach events *from* this page root element *to* the child
        //
        // child   - the child PageFunction. Caller was originally attached to child, we're now reattaching *to* the child
        //
        internal void _Attach(Object caller, PageFunctionBase child)
        {
            ReturnEventSaverInfo[] list = null;

            list = _returnList;

            if (list != null)
            {
                Debug.Assert(caller != null, "Caller should not be null");
                for (int i = 0; i < list.Length; i++)
                {
                    //
                    // Future notes: how do we handle listeners that were not on the calling pagefunction ? 
                    // E.g. - if we had a listener to OnFinish from a Button on the calling page.  
                    //  "Return event never fired from PageFunction hosted in its own window"
                    // 
                    if (string.Compare(_returnList[i]._targetTypeName, caller.GetType().AssemblyQualifiedName, StringComparison.Ordinal) != 0)
                    {
                        throw new NotSupportedException(SR.Get(SRID.ReturnEventHandlerMustBeOnParentPage));
                    }

                    Delegate d;
                    try
                    {
                        d = Delegate.CreateDelegate(
                                                                Type.GetType(_returnList[i]._delegateTypeName),
                                                                caller,
                                                                _returnList[i]._delegateMethodName);
                    }
                    catch (Exception ex)
                    {
                        throw new NotSupportedException(SR.Get(SRID.ReturnEventHandlerMustBeOnParentPage), ex);
                    }

                    child._AddEventHandler(d);
                }
            }
        }

        private ReturnEventSaverInfo[] _returnList;     // The list of delegates we want to persist and return later 
    }
}
