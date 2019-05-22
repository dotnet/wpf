// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: DefaultTextStoreTextComposition class is the composition 
//              object for the input in DefaultTextStore.
//              Cicero's composition injected to DefaulteTextStore is
//              represent by this DefaultTextStoreTextComposition.
//              This has custom Complete method to control
//              Cicero's composiiton.
//
//

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Security; 

using MS.Win32;

namespace System.Windows.Input
{
    /// <summary>
    ///     DefaultTextStoreTextComposition class implements Complete for 
    ///     the composition in DefaultTextStore.
    /// </summary>
    internal class DefaultTextStoreTextComposition : TextComposition
    {
        //------------------------------------------------------
        //
        //  ctor
        //
        //------------------------------------------------------

        /// <summary>
        ///     ctor
        /// </summary>
        ///<SecurityNote>
        ///     Critical - calls base ctor - which in turn stores the inputmanager that's critical. 
        ///</SecurityNote> 
        [SecurityCritical]
        internal DefaultTextStoreTextComposition(InputManager inputManager, IInputElement source, string text, TextCompositionAutoComplete autoComplete) : base(inputManager, source, text, autoComplete)
        {
        }

        //------------------------------------------------------
        //
        //  Public Interface Methods 
        //
        //------------------------------------------------------

        /// <summary>
        ///     Finalize the composition.
        ///     This does not call base.Complete() because TextComposition.Complete()
        ///     will call TextServicesManager.CompleteComposition() directly to generate TextCompositionEvent.
        ///     We finalize Cicero's composition and DefaultTextStore will automatically
        ///     generate the proper TextComposition events.
        /// </summary>
        /// <SecurityNote>
        ///   Critical: This completes the composition and in doing so calls GetTransitionaryContext which gives it ITfContext
        ///   TreatAsSafe: The context is not exposed, neither are the other members
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        public override void Complete()
        {
//             VerifyAccess();

            UnsafeNativeMethods.ITfContext context = GetTransitoryContext();
            UnsafeNativeMethods.ITfContextOwnerCompositionServices compositionService = context as UnsafeNativeMethods.ITfContextOwnerCompositionServices;
            UnsafeNativeMethods.ITfCompositionView composition = GetComposition(context);
            
            if (composition != null)
            {
                // Terminate composition if there is a composition view.
                compositionService.TerminateComposition(composition);
                Marshal.ReleaseComObject(composition);
            }

            Marshal.ReleaseComObject(context);
}

        //------------------------------------------------------
        //
        //  private Methods 
        //
        //------------------------------------------------------

        /// <summary>
        ///     Get the base ITfContext of the transitory document.
        /// </summary>
        /// <SecurityNote>
        ///   Critical: This exposes ITfContext which has unsecure methods
        /// </SecurityNote>
        [SecurityCritical]
        private UnsafeNativeMethods.ITfContext GetTransitoryContext()
        {
            DefaultTextStore defaultTextStore = DefaultTextStore.Current;
            UnsafeNativeMethods.ITfDocumentMgr doc = defaultTextStore.TransitoryDocumentManager;
            UnsafeNativeMethods.ITfContext context;

            doc.GetBase(out context);

            Marshal.ReleaseComObject(doc);
            return context;
        }

        /// <summary>
        ///     Get ITfContextView of the context.
        /// </summary>
        ///<SecurityNote>
        ///     Critical: calls Marshal.ReleaseComObject which has a LinkDemand
        ///	TreatAsSafe: can't pass in arbitrary COM object to release
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        private UnsafeNativeMethods.ITfCompositionView GetComposition(UnsafeNativeMethods.ITfContext context)
        {
            UnsafeNativeMethods.ITfContextComposition contextComposition;
            UnsafeNativeMethods.IEnumITfCompositionView enumCompositionView;
            UnsafeNativeMethods.ITfCompositionView[] compositionViews = new UnsafeNativeMethods.ITfCompositionView[1];
            int fetched;
          
            contextComposition = (UnsafeNativeMethods.ITfContextComposition)context;
            contextComposition.EnumCompositions(out enumCompositionView);

            enumCompositionView.Next(1, compositionViews, out fetched);

            Marshal.ReleaseComObject(enumCompositionView);
            return compositionViews[0];
}
    }
}
