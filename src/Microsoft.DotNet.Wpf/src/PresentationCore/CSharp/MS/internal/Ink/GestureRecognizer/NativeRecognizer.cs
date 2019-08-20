// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      A wrapper class which interoperates with the unmanaged recognition APIS
//      in mshwgst.dll
//
// Features:
//
//  01/14/2005 waynezen:       Created
//
//

using Microsoft.Win32;
using MS.Win32;
using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows.Input;
using MS.Internal.PresentationCore;

using MS.Utility;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace MS.Internal.Ink.GestureRecognition
{
    /// <summary>
    /// NativeRecognizer class
    /// </summary>
    internal sealed class NativeRecognizer : IDisposable
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static constructor
        /// </summary>
        static NativeRecognizer()
        {
            s_isSupported = LoadRecognizerDll();
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private NativeRecognizer()
        {
            Debug.Assert(NativeRecognizer.RecognizerHandleSingleton != null);

            int hr = MS.Win32.Recognizer.UnsafeNativeMethods.CreateContext(NativeRecognizer.RecognizerHandleSingleton,
                                                                        out _hContext);
            if (HRESULT.Failed(hr))
            {
                //don't throw a com exception here, we don't need to pass out any details
                throw new InvalidOperationException(SR.Get(SRID.UnspecifiedGestureConstructionException));
            }

            // We add a reference of the recognizer to the context handle.
            // The context will dereference the recognizer reference when it gets disposed.
            // This trick will prevent the GC from disposing the recognizer before all contexts.
            _hContext.AddReferenceOnRecognizer(NativeRecognizer.RecognizerHandleSingleton);
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Create an Instance of the NativeRecognizer.
        /// </summary>
        /// <returns>null if it fails</returns>
        internal static NativeRecognizer CreateInstance()
        {
            if (NativeRecognizer.RecognizerHandleSingleton != null)
            {
                return new NativeRecognizer();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Set the enabled gestures
        /// </summary>
        /// <param name="applicationGestures"></param>
        internal ApplicationGesture[] SetEnabledGestures(IEnumerable<ApplicationGesture> applicationGestures)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("NativeRecognizer");
            }

            //validate and get an array out
            ApplicationGesture[] enabledGestures =
                GetApplicationGestureArrayAndVerify(applicationGestures);

            // Set enabled Gestures.
            int hr = SetEnabledGestures(_hContext, enabledGestures);
            if (HRESULT.Failed(hr))
            {
                //don't throw a com exception here, we don't need to pass out any details
                throw new InvalidOperationException(SR.Get(SRID.UnspecifiedSetEnabledGesturesException));
            }

            return enabledGestures;
        }

        /// <summary>
        /// Recognize the strokes.
        /// </summary>
        /// <param name="strokes"></param>
        /// <returns></returns>
        internal GestureRecognitionResult[] Recognize(StrokeCollection strokes)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("NativeRecognizer");
            }

            //
            // note that we validate this argument from GestureRecognizer 
            // but since this is marked TAS, we want to do it here as well
            //
            if (strokes == null)
            {
                throw new ArgumentNullException("strokes"); // Null is not allowed as the argument value
            }
            if (strokes.Count > 2)
            {
                throw new ArgumentException(SR.Get(SRID.StrokeCollectionCountTooBig), "strokes");
            }

            // Create an empty result.
            GestureRecognitionResult[] recResults = new GestureRecognitionResult[]{};

            if ( strokes.Count == 0 )
            {
                return recResults;
            }

            int hr = 0;

            try
            {
                // Reset the context
                hr = MS.Win32.Recognizer.UnsafeNativeMethods.ResetContext(_hContext);
                if (HRESULT.Failed(hr))
                {
                    //finally block will clean up and throw
                    return recResults;
                }

                // Add strokes
                hr = AddStrokes(_hContext, strokes);
                if (HRESULT.Failed(hr))
                {
                    //AddStrokes's finally block will clean up this finally block will throw
                    return recResults;
                }

                // recognize the ink
                bool bIncremental;
                hr = MS.Win32.Recognizer.UnsafeNativeMethods.Process(_hContext, out bIncremental);

                if (HRESULT.Succeeded(hr))
                {
                    if ( s_GetAlternateListExists )
                    {
                        recResults = InvokeGetAlternateList();
                    }
                    else
                    {
                        recResults = InvokeGetLatticePtr();
                    }
                }
            }
            finally
            {
                // Check if we should report any error.
                if ( HRESULT.Failed(hr) )
                {
                    //don't throw a com exception here, we don't need to pass out any details
                    throw new InvalidOperationException(SR.Get(SRID.UnspecifiedGestureException));
                }
            }

            return recResults;
        }


        internal static ApplicationGesture[] GetApplicationGestureArrayAndVerify(IEnumerable<ApplicationGesture> applicationGestures)
        {
            if (applicationGestures == null)
            {
                // Null is not allowed as the argument value
                throw new ArgumentNullException("applicationGestures");
            }

            uint count = 0;
            //we need to make a disconnected copy
            ICollection<ApplicationGesture> collection = applicationGestures as ICollection<ApplicationGesture>;
            if (collection != null)
            {
                count = (uint)collection.Count;
            }
            else
            {
                foreach (ApplicationGesture gesture in applicationGestures)
                {
                    count++;
                }
            }

            // Cannot be empty
            if (count == 0)
            {
                // An empty array is not allowed.
                throw new ArgumentException(SR.Get(SRID.ApplicationGestureArrayLengthIsZero), "applicationGestures");
            }

            bool foundAllGestures = false;
            List<ApplicationGesture> gestures = new List<ApplicationGesture>();
            foreach (ApplicationGesture gesture in applicationGestures)
            {
                if (!ApplicationGestureHelper.IsDefined(gesture))
                {
                    throw new ArgumentException(SR.Get(SRID.ApplicationGestureIsInvalid), "applicationGestures");
                }

                //check for allgestures
                if (gesture == ApplicationGesture.AllGestures)
                {
                    foundAllGestures = true;
                }

                //check for dupes
                if (gestures.Contains(gesture))
                {
                    throw new ArgumentException(SR.Get(SRID.DuplicateApplicationGestureFound), "applicationGestures");
                }

                gestures.Add(gesture);
            }

            // AllGesture cannot be specified with other gestures
            if (foundAllGestures && gestures.Count != 1)
            {
                // no dupes allowed
                throw new ArgumentException(SR.Get(SRID.AllGesturesMustExistAlone), "applicationGestures");
            }

            return gestures.ToArray();
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // IDisposable
        //
        //-------------------------------------------------------------------------------

        #region IDisposable

        /// <summary>
        /// A simple pattern of the dispose implementation.
        /// There is no finalizer since the SafeHandle will take care of releasing the context.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _hContext.Dispose();
            _disposed = true;
        }

        #endregion IDisposable

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///  Load the mshwgst.dll from the path in the registry. Make sure this loading action prior to invoking
        ///  any native functions marked with DllImport in mshwgst.dll
        ///  This method is called from the NativeRecognizer's static construtor.
        /// </summary>
        private static bool LoadRecognizerDll()
        {
            // ISSUE-2005/01/14-WAYNEZEN,
            // We may hit the problem when an application already load mshwgst.dll from somewhere rather than the
            // directory we are looking for. The options to resolve this -
            //  1. We fail the recognition functionality.
            //  2. We unload the previous mshwgst.dll
            //  3. We switch the DllImport usage to the new dynamic PInvoke mechanism in Whidbey. Please refer to the blog
            //     http://blogs.msdn.com/junfeng/archive/2004/07/14/181932.aspx. Then we don't have to unload the existing
            //     mshwgst.dll.
            String path = null;
            RegistryKey regkey = Registry.LocalMachine;
            RegistryKey recognizerKey = regkey.OpenSubKey(GestureRecognizerPath);
            if (recognizerKey != null)
            {
                try
                {
                    // Try to read the recognizer path subkey
                    path = recognizerKey.GetValue(GestureRecognizerValueName) as string;
                    if (path == null)
                    {
                        return false;
                    } 
                }
                finally
                {
                    recognizerKey.Close();
                }
            }
            else
            {
                // we couldn't find the path in the registry
                // no key to close
                return false;
            }
 
            if (path != null)
            {
                IntPtr hModule = MS.Win32.UnsafeNativeMethods.LoadLibrary(path);

                // Check whether GetAlternateList exists in the loaded Dll.
                s_GetAlternateListExists = false;
                if ( hModule != IntPtr.Zero )
                {
                    s_GetAlternateListExists = MS.Win32.UnsafeNativeMethods.GetProcAddressNoThrow(
                        new HandleRef(null, hModule), "GetAlternateList") != IntPtr.Zero ?
                        true : false;
                }

                return hModule != IntPtr.Zero ? true : false; 
            }
            return false; //path was null 
        }

        /// <summary>
        /// Set the enabled gestures.
        /// This method is called from the internal SetEnabledGestures method.
        /// </summary>
        private int SetEnabledGestures(MS.Win32.Recognizer.ContextSafeHandle recContext, ApplicationGesture[] enabledGestures)
        {
            Debug.Assert(recContext != null && !recContext.IsInvalid);

            // NOTICE-2005/01/11-WAYNEZEN,
            // The following usage was copied from drivers\tablet\recognition\ink\core\twister\src\wispapis.c
            // SetEnabledUnicodeRanges
            //      Set ranges of gestures enabled in this recognition context
            //      The behavior of this function is the following:
            //          (a) (A part of) One of the requested ranges lies outside
            //              gesture interval---currently  [GESTURE_NULL, GESTURE_NULL + 255)
            //              return E_UNEXPECTED and keep the previously set ranges
            //          (b) All requested ranges are within the gesture interval but
            //              some of them are not supported:
            //              return S_TRUNCATED and set those requested gestures that are
            //              supported (possibly an empty set)
            //          (c) All requested gestures are supported
            //              return S_OK and set all requested gestures.
            //      Note:  An easy way to set all supported gestures as enabled is to use
            //              SetEnabledUnicodeRanges() with one range=(GESTURE_NULL,255).

            // Enabel gestures
            uint cRanges = (uint)( enabledGestures.Length );

            MS.Win32.Recognizer.CHARACTER_RANGE[] charRanges = new MS.Win32.Recognizer.CHARACTER_RANGE[cRanges];

            if ( cRanges == 1 && enabledGestures[0] == ApplicationGesture.AllGestures )
            {
                charRanges[0].cChars = MAX_GESTURE_COUNT;
                charRanges[0].wcLow = GESTURE_NULL;
            }
            else
            {
                for ( int i = 0; i < cRanges; i++ )
                {
                    charRanges[i].cChars = 1;
                    charRanges[i].wcLow = (ushort)( enabledGestures[i] );
                }
            }
            int hr = MS.Win32.Recognizer.UnsafeNativeMethods.SetEnabledUnicodeRanges(recContext, cRanges, charRanges);
            return hr;
        }

        /// <summary>
        /// Add the strokes to the recoContext.
        /// The method is called from the internal Recognize method.
        /// </summary>
        private int AddStrokes(MS.Win32.Recognizer.ContextSafeHandle recContext, StrokeCollection strokes)
        {
            Debug.Assert(recContext != null && !recContext.IsInvalid);

            int hr;

            foreach ( Stroke stroke in strokes )
            {
                MS.Win32.Recognizer.PACKET_DESCRIPTION packetDescription = 
                    new MS.Win32.Recognizer.PACKET_DESCRIPTION();
                IntPtr packets = IntPtr.Zero;
                try
                {
                    int countOfBytes;
                    NativeMethods.XFORM xForm;
                    GetPacketData(stroke, out packetDescription, out countOfBytes, out packets, out xForm);
                    if (packets == IntPtr.Zero)
                    {
                        return -2147483640; //E_FAIL - 0x80000008.  We never raise this in an exception
                    }

                    hr = MS.Win32.Recognizer.UnsafeNativeMethods.AddStroke(recContext, ref packetDescription, (uint)countOfBytes, packets, xForm);
                    if ( HRESULT.Failed(hr) )
                    {
                        // Return from here. The finally block will free the memory and report the error properly.
                        return hr;
                    }
                }
                finally
                {
                    // Release the resources in the finally block
                    ReleaseResourcesinPacketDescription(packetDescription, packets);
                }
            }

            return MS.Win32.Recognizer.UnsafeNativeMethods.EndInkInput(recContext);
}

        /// <summary>
        /// Retrieve the packet description, packets data and XFORM which is the information the native recognizer needs.
        /// The method is called from AddStrokes.
        /// </summary>
        private void GetPacketData
        (
            Stroke stroke,
            out MS.Win32.Recognizer.PACKET_DESCRIPTION packetDescription, 
            out int countOfBytes, 
            out IntPtr packets, 
            out NativeMethods.XFORM xForm
        )
        {
            int i;
            countOfBytes = 0;
            packets = IntPtr.Zero;
            packetDescription = new MS.Win32.Recognizer.PACKET_DESCRIPTION();
            Matrix matrix = Matrix.Identity;
            xForm = new NativeMethods.XFORM((float)(matrix.M11), (float)(matrix.M12), (float)(matrix.M21),
                                            (float)(matrix.M22), (float)(matrix.OffsetX), (float)(matrix.OffsetY));

            StylusPointCollection stylusPoints = stroke.StylusPoints;
            if (stylusPoints.Count == 0)
            {
                return; //we'll fail when the calling routine sees that packets is IntPtr.Zer
            }

            if (stylusPoints.Description.PropertyCount > StylusPointDescription.RequiredCountOfProperties)
            {
                //
                // reformat to X, Y, P
                //
                StylusPointDescription reformatDescription
                    = new StylusPointDescription(
                            new StylusPointPropertyInfo[]{
                                new StylusPointPropertyInfo(StylusPointProperties.X),
                                new StylusPointPropertyInfo(StylusPointProperties.Y),
                                stylusPoints.Description.GetPropertyInfo(StylusPointProperties.NormalPressure)});
                stylusPoints = stylusPoints.Reformat(reformatDescription);
            }

            //
            // now make sure we only take a finite amount of data for the stroke
            //
            if (stylusPoints.Count > MaxStylusPoints)
            {
                stylusPoints = stylusPoints.Clone(MaxStylusPoints);
            }

            Guid[] propertyGuids = new Guid[]{  StylusPointPropertyIds.X, //required index for SPD
                                                StylusPointPropertyIds.Y, //required index for SPD
                                                StylusPointPropertyIds.NormalPressure}; //required index for SPD

            Debug.Assert(stylusPoints != null);
            Debug.Assert(propertyGuids.Length == StylusPointDescription.RequiredCountOfProperties);

            // Get the packet description
            packetDescription.cbPacketSize = (uint)(propertyGuids.Length * Marshal.SizeOf(typeof(Int32)));
            packetDescription.cPacketProperties = (uint)propertyGuids.Length;

            //
            // use X, Y defaults for metrics, sometimes mouse metrics can be bogus
            // always use NormalPressure metrics, though.
            //
            StylusPointPropertyInfo[] infosToUse = new StylusPointPropertyInfo[StylusPointDescription.RequiredCountOfProperties];
            infosToUse[StylusPointDescription.RequiredXIndex] = StylusPointPropertyInfoDefaults.X;
            infosToUse[StylusPointDescription.RequiredYIndex] = StylusPointPropertyInfoDefaults.Y;
            infosToUse[StylusPointDescription.RequiredPressureIndex] =
                stylusPoints.Description.GetPropertyInfo(StylusPointProperties.NormalPressure);

            MS.Win32.Recognizer.PACKET_PROPERTY[] packetProperties = 
                new MS.Win32.Recognizer.PACKET_PROPERTY[packetDescription.cPacketProperties];
            
            StylusPointPropertyInfo propertyInfo;
            for ( i = 0; i < packetDescription.cPacketProperties; i++ )
            {
                packetProperties[i].guid = propertyGuids[i];
                propertyInfo = infosToUse[i];

                MS.Win32.Recognizer.PROPERTY_METRICS propertyMetrics = new MS.Win32.Recognizer.PROPERTY_METRICS( );
                propertyMetrics.nLogicalMin = propertyInfo.Minimum;
                propertyMetrics.nLogicalMax = propertyInfo.Maximum;
                propertyMetrics.Units = (int)(propertyInfo.Unit);
                propertyMetrics.fResolution = propertyInfo.Resolution;
                packetProperties[i].PropertyMetrics = propertyMetrics;
            }

            unsafe
            {
                int allocationSize = (int)(Marshal.SizeOf(typeof(MS.Win32.Recognizer.PACKET_PROPERTY)) * packetDescription.cPacketProperties);
                packetDescription.pPacketProperties = Marshal.AllocCoTaskMem(allocationSize);
                MS.Win32.Recognizer.PACKET_PROPERTY* pPacketProperty = 
                    (MS.Win32.Recognizer.PACKET_PROPERTY*)(packetDescription.pPacketProperties.ToPointer());
                MS.Win32.Recognizer.PACKET_PROPERTY* pElement = pPacketProperty;
                for ( i = 0 ; i < packetDescription.cPacketProperties ; i ++ )
                {
                    Marshal.StructureToPtr(packetProperties[i], new IntPtr(pElement), false);
                    pElement++;
                }
            }

            // Get packet data
            int[] rawPackets = stylusPoints.ToHiMetricArray();
            int packetCount = rawPackets.Length;
            if (packetCount != 0)
            {
                countOfBytes = packetCount * Marshal.SizeOf(typeof(Int32));
                packets = Marshal.AllocCoTaskMem(countOfBytes);
                Marshal.Copy(rawPackets, 0, packets, packetCount);
            }
        }

        /// <summary>
        /// Release the memory blocks which has been created for mashalling purpose.
        /// The method is called from AddStrokes.
        /// </summary>
        private void ReleaseResourcesinPacketDescription(MS.Win32.Recognizer.PACKET_DESCRIPTION pd, IntPtr packets)
        {
            if ( pd.pPacketProperties != IntPtr.Zero )
            {
                unsafe
                {
                    MS.Win32.Recognizer.PACKET_PROPERTY* pPacketProperty = 
                        (MS.Win32.Recognizer.PACKET_PROPERTY*)( pd.pPacketProperties.ToPointer( ) );
                    MS.Win32.Recognizer.PACKET_PROPERTY* pElement = pPacketProperty;

                    for ( int i = 0; i < pd.cPacketProperties; i++ )
                    {
                        Marshal.DestroyStructure(new IntPtr(pElement), typeof(MS.Win32.Recognizer.PACKET_PROPERTY));
                        pElement++;
                    }
                }
                Marshal.FreeCoTaskMem(pd.pPacketProperties);
                pd.pPacketProperties = IntPtr.Zero; 
            }

            if ( pd.pguidButtons != IntPtr.Zero )
            {
                Marshal.FreeCoTaskMem(pd.pguidButtons);
                pd.pguidButtons = IntPtr.Zero;
            }

            if ( packets != IntPtr.Zero )
            {
                Marshal.FreeCoTaskMem(packets);
                packets = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Invokes GetAlternateList in the native dll
        /// </summary>
        /// <returns></returns>
        private GestureRecognitionResult[] InvokeGetAlternateList()
        {
            GestureRecognitionResult[] recResults = new GestureRecognitionResult[] { };
            int hr = 0;

            MS.Win32.Recognizer.RECO_RANGE recoRange;
            recoRange.iwcBegin = 0;
            recoRange.cCount = 1;
            uint countOfAlternates = IRAS_DefaultCount;
            IntPtr[] pRecoAlternates = new IntPtr[IRAS_DefaultCount];

            try
            {
                hr = MS.Win32.Recognizer.UnsafeNativeMethods.GetAlternateList(_hContext, ref recoRange, ref countOfAlternates, pRecoAlternates, MS.Win32.Recognizer.ALT_BREAKS.ALT_BREAKS_SAME);

                if ( HRESULT.Succeeded(hr) && countOfAlternates != 0 )
                {
                    List<GestureRecognitionResult> resultList = new List<GestureRecognitionResult>();

                    for ( int i = 0; i < countOfAlternates; i++ )
                    {
                        uint size = 1; // length of string == 1 since gesture id is a single WCHAR
                        StringBuilder recoString = new StringBuilder(1);
                        RecognitionConfidence confidenceLevel;

                        if ( HRESULT.Failed(MS.Win32.Recognizer.UnsafeNativeMethods.GetString(pRecoAlternates[i], out recoRange, ref size, recoString))
                            || HRESULT.Failed(MS.Win32.Recognizer.UnsafeNativeMethods.GetConfidenceLevel(pRecoAlternates[i], out recoRange, out confidenceLevel)) )
                        {
                            // Fail to retrieve the reco result, skip this one
                            continue;
                        }

                        ApplicationGesture gesture = (ApplicationGesture)recoString[0];
                        Debug.Assert(ApplicationGestureHelper.IsDefined(gesture));
                        if (ApplicationGestureHelper.IsDefined(gesture))
                        {
                            resultList.Add(new GestureRecognitionResult(confidenceLevel, gesture));
                        }
                    }

                    recResults = resultList.ToArray();
                }
            }
            finally
            {
                // Destroy the alternates
                for ( int i = 0; i < countOfAlternates; i++ )
                {
                    if (pRecoAlternates[i] != IntPtr.Zero)
                    {
                        #pragma warning suppress 6031, 56031 // Return value ignored on purpose.
                        MS.Win32.Recognizer.UnsafeNativeMethods.DestroyAlternate(pRecoAlternates[i]);
                        pRecoAlternates[i] = IntPtr.Zero;
                    }
                }
            }

            return recResults;
        }

        /// <summary>
        /// Invokes GetLatticePtr in the native dll
        /// </summary>
        /// <returns></returns>
        private GestureRecognitionResult[] InvokeGetLatticePtr()
        {
            GestureRecognitionResult[] recResults = new GestureRecognitionResult[] { };

//            int hr = 0;
            IntPtr ptr = IntPtr.Zero;

            // NOTICE-2005/07/11-WAYNEZEN,
            // There is no need to free the returned the structure. 
            // The memory will be released when ResetContext, which is invoked in the callee - Recognize, is called.
            if ( HRESULT.Succeeded(
                MS.Win32.Recognizer.UnsafeNativeMethods.GetLatticePtr(
                _hContext, ref ptr)) )
            {
                unsafe
                {
                    MS.Win32.Recognizer.RECO_LATTICE* pRecoLattice = (MS.Win32.Recognizer.RECO_LATTICE*)ptr;

                    uint bestResultColumnCount = pRecoLattice->ulBestResultColumnCount;
                    Debug.Assert(!(bestResultColumnCount != 0 && pRecoLattice->pLatticeColumns == IntPtr.Zero), "Invalid results!");
                    if ( bestResultColumnCount > 0 && pRecoLattice->pLatticeColumns != IntPtr.Zero )
                    {
                        List<GestureRecognitionResult> resultList = new List<GestureRecognitionResult>();

                        MS.Win32.Recognizer.RECO_LATTICE_COLUMN* pLatticeColumns =
                            (MS.Win32.Recognizer.RECO_LATTICE_COLUMN*)(pRecoLattice->pLatticeColumns);
                        ulong* pulBestResultColumns = (ulong*)(pRecoLattice->pulBestResultColumns);

                        for ( uint i = 0; i < bestResultColumnCount; i++ )
                        {
                            ulong column = pulBestResultColumns[i];
                            MS.Win32.Recognizer.RECO_LATTICE_COLUMN recoColumn = pLatticeColumns[column];

                            Debug.Assert(0 < recoColumn.cLatticeElements, "Invalid results!");

                            for ( int j = 0; j < recoColumn.cLatticeElements; j++ )
                            {
                                MS.Win32.Recognizer.RECO_LATTICE_ELEMENT recoElement =
                                    ((MS.Win32.Recognizer.RECO_LATTICE_ELEMENT*)(recoColumn.pLatticeElements))[j];

                                Debug.Assert((RECO_TYPE)(recoElement.type) == RECO_TYPE.RECO_TYPE_WCHAR, "The Application gesture has to be WCHAR type" );

                                if ( (RECO_TYPE)(recoElement.type) == RECO_TYPE.RECO_TYPE_WCHAR )
                                {
                                    // Retrieve the confidence lever
                                    RecognitionConfidence confidenceLevel = RecognitionConfidence.Poor;

                                    MS.Win32.Recognizer.RECO_LATTICE_PROPERTIES recoProperties = recoElement.epProp;

                                    uint propertyCount = recoProperties.cProperties;
                                    MS.Win32.Recognizer.RECO_LATTICE_PROPERTY** apProps = 
                                        (MS.Win32.Recognizer.RECO_LATTICE_PROPERTY**)recoProperties.apProps;
                                    for ( int k = 0; k < propertyCount; k++ )
                                    {
                                        MS.Win32.Recognizer.RECO_LATTICE_PROPERTY* pProps = apProps[k];
                                        if ( pProps->guidProperty == GUID_CONFIDENCELEVEL )
                                        {
                                            Debug.Assert(pProps->cbPropertyValue == sizeof(uint) / sizeof(byte));
                                            RecognitionConfidence level = (RecognitionConfidence)(((uint*)pProps->pPropertyValue))[0];
                                            if ( level >= RecognitionConfidence.Strong && level <= RecognitionConfidence.Poor )
                                            {
                                                confidenceLevel = level;
                                            }

                                            break;
                                        }
                                    }

                                    ApplicationGesture gesture = (ApplicationGesture)((char)(recoElement.pData));
                                    Debug.Assert(ApplicationGestureHelper.IsDefined(gesture));
                                    if (ApplicationGestureHelper.IsDefined(gesture))
                                    {
                                        // Get the gesture result
                                        resultList.Add(new GestureRecognitionResult(confidenceLevel,gesture));
                                    }
                                }
                            }
}

                        recResults = (GestureRecognitionResult[])(resultList.ToArray());
                    }
                }
            }

            return recResults;
        }

        #endregion Private Methods


        /// <summary>
        /// RecognizerHandle is a static property. But it's a SafeHandle.
        /// So, we don't have to worry about releasing the handle since RecognizerSafeHandle when there is no reference on it.
        /// </summary>
        private static MS.Win32.Recognizer.RecognizerSafeHandle RecognizerHandleSingleton
        {
            get
            {
                if (s_isSupported && s_hRec == null)
                {
                    lock (_syncRoot)
                    {
                        if (s_isSupported && s_hRec == null)
                        {
                            if (HRESULT.Failed(MS.Win32.Recognizer.UnsafeNativeMethods.CreateRecognizer(ref s_Gesture, out s_hRec)))
                            {
                                s_hRec = null;
                            }
                        }
                    }
                }

                return s_hRec;
            }
        }

        enum RECO_TYPE : ushort
        {
            RECO_TYPE_WSTRING = 0,
            RECO_TYPE_WCHAR = 1
        }

        private const string GestureRecognizerPath = @"SOFTWARE\MICROSOFT\TPG\SYSTEM RECOGNIZERS\{BED9A940-7D48-48E3-9A68-F4887A5A1B2E}";
        private const string GestureRecognizerFullPath = "HKEY_LOCAL_MACHINE" + @"\" + GestureRecognizerPath;
        private const string GestureRecognizerValueName = "RECOGNIZER DLL";
        private const string GestureRecognizerGuid = "{BED9A940-7D48-48E3-9A68-F4887A5A1B2E}";
        
        
        // This constant is an identical value as the one in drivers\tablet\recognition\ink\core\common\inc\gesture\gesturedefs.h
        private const ushort MAX_GESTURE_COUNT = 256;
        // This constant is an identical value as the one in drivers\tablet\include\sdk\recdefs.h
        private const ushort GESTURE_NULL = 0xf000;
        // This constant is an identical value as the one in public\internal\drivers\inc\msinkaut.h
        private const ushort IRAS_DefaultCount = 10;
        private const ushort MaxStylusPoints = 10000;

        // The GUID has been copied from public\internal\drivers\inc\tpcguid.h
        //// {7DFE11A7-FB5D-4958-8765-154ADF0D833F}
        //DEFINE_GUID(GUID_CONFIDENCELEVEL, 0x7dfe11a7, 0xfb5d, 0x4958, 0x87, 0x65, 0x15, 0x4a, 0xdf, 0xd, 0x83, 0x3f);
        private static readonly Guid GUID_CONFIDENCELEVEL = new Guid("{7DFE11A7-FB5D-4958-8765-154ADF0D833F}");


        /// <summary>
        /// IDisposable support
        /// </summary>
        private bool                                    _disposed = false;

        /// <summary>
        /// Each NativeRecognizer instance has it's own recognizer context
        /// </summary>
        private MS.Win32.Recognizer.ContextSafeHandle _hContext;

        /// <summary>
        /// Used to lock for instancing the native recognizer handle
        /// </summary>
        private static object                           _syncRoot = new object();

        /// <summary>
        /// All NativeRecognizer share a single handle to the recognizer
        /// </summary>
        private static MS.Win32.Recognizer.RecognizerSafeHandle s_hRec;

        /// <summary>
        /// The Guid of the GestureRecognizer used for registry lookup
        /// </summary>
        private static Guid                             s_Gesture = new Guid(GestureRecognizerGuid);

        /// <summary>
        /// can we load the recognizer?
        /// </summary>
        private static readonly bool s_isSupported;

        /// <summary>
        /// A flag indicates whether we can find the entry point of 
        /// GetAlternateList function in mshwgst.dll
        /// </summary>
        private static bool s_GetAlternateListExists;
}
}
