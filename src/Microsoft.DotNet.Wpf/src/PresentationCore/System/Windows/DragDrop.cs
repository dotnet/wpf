// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// 
// Description: The DragDrop system is for drag-and-drop operation.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/DragDrop%20design%20on%20WPP.mht
// 
//

using MS.Win32;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace System.Windows
{
    #region DragDrop

    /// <summary>
    /// Provides drag-and-drop operation methods.
    /// </summary>
    public static class DragDrop
    {
        //------------------------------------------------------
        //
        //  DragDrop Event
        //
        //------------------------------------------------------

        #region DragDrop Event

        /// <summary>
        /// PreviewQueryContinueDrag
        /// </summary>
        public static readonly RoutedEvent PreviewQueryContinueDragEvent = EventManager.RegisterRoutedEvent("PreviewQueryContinueDrag", RoutingStrategy.Tunnel, typeof(QueryContinueDragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the PreviewQueryContinueDrag attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewQueryContinueDragEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the PreviewQueryContinueDrag attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewQueryContinueDragEvent, handler);
        }

        /// <summary>
        /// QueryContinueDrag
        /// </summary>
        public static readonly RoutedEvent QueryContinueDragEvent = EventManager.RegisterRoutedEvent("QueryContinueDrag", RoutingStrategy.Bubble, typeof(QueryContinueDragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the QueryContinueDrag attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.AddHandler(element, QueryContinueDragEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the QueryContinueDrag attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.RemoveHandler(element, QueryContinueDragEvent, handler);
        }

        /// <summary>
        /// PreviewGiveFeedback
        /// </summary>
        public static readonly RoutedEvent PreviewGiveFeedbackEvent = EventManager.RegisterRoutedEvent("PreviewGiveFeedback", RoutingStrategy.Tunnel, typeof(GiveFeedbackEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the PreviewGiveFeedback attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewGiveFeedbackEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the PreviewGiveFeedback attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewGiveFeedbackEvent, handler);
        }

        /// <summary>
        /// GiveFeedback
        /// </summary>
        public static readonly RoutedEvent GiveFeedbackEvent = EventManager.RegisterRoutedEvent("GiveFeedback", RoutingStrategy.Bubble, typeof(GiveFeedbackEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the GiveFeedback attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.AddHandler(element, GiveFeedbackEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the GiveFeedback attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.RemoveHandler(element, GiveFeedbackEvent, handler);
        }

        /// <summary>
        /// PreviewDragEnter
        /// </summary>
        public static readonly RoutedEvent PreviewDragEnterEvent = EventManager.RegisterRoutedEvent("PreviewDragEnter", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the PreviewDragEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDragEnterEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the PreviewDragEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDragEnterEvent, handler);
        }

        /// <summary>
        /// DragEnter
        /// </summary>
        public static readonly RoutedEvent DragEnterEvent = EventManager.RegisterRoutedEvent("DragEnter", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the DragEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DragEnterEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the DragEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DragEnterEvent, handler);
        }

        /// <summary>
        /// PreviewDragOver
        /// </summary>
        public static readonly RoutedEvent PreviewDragOverEvent = EventManager.RegisterRoutedEvent("PreviewDragOver", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the PreviewDragOver attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDragOverEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the PreviewDragOver attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDragOverEvent, handler);
        }

        /// <summary>
        /// DragOver
        /// </summary>
        public static readonly RoutedEvent DragOverEvent = EventManager.RegisterRoutedEvent("DragOver", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the DragOver attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DragOverEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the DragOver attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DragOverEvent, handler);
        }

        /// <summary>
        /// PreviewDragLeave
        /// </summary>
        public static readonly RoutedEvent PreviewDragLeaveEvent = EventManager.RegisterRoutedEvent("PreviewDragLeave", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the PreviewDragLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDragLeaveEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the PreviewDragLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDragLeaveEvent, handler);
        }

        /// <summary>
        /// DragLeave
        /// </summary>
        public static readonly RoutedEvent DragLeaveEvent = EventManager.RegisterRoutedEvent("DragLeave", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the DragLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DragLeaveEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the DragLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DragLeaveEvent, handler);
        }

        /// <summary>
        /// PreviewDrop
        /// </summary>
        public static readonly RoutedEvent PreviewDropEvent = EventManager.RegisterRoutedEvent("PreviewDrop", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the PreviewDrop attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDropEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the PreviewDrop attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDropEvent, handler);
        }

        /// <summary>
        /// Drop
        /// </summary>
        public static readonly RoutedEvent DropEvent = EventManager.RegisterRoutedEvent("Drop", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>
        /// Adds a handler for the Drop attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DropEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the Drop attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DropEvent, handler);
        }

        internal static readonly RoutedEvent DragDropStartedEvent = EventManager.RegisterRoutedEvent("DragDropStarted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DragDrop));
        internal static readonly RoutedEvent DragDropCompletedEvent = EventManager.RegisterRoutedEvent("DragDropCompleted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DragDrop));

        #endregion DragDrop Event

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Begins a drag-and-drop operation.
        /// </summary>
        /// <param name="dragSource">
        /// The drag source object.
        /// </param>
        /// <param name="data">
        /// The data to drag.
        /// </param>
        /// <param name="allowedEffects">
        /// The allowed effects that is one of the DragDropEffects values.
        /// </param>
        /// <remarks>
        /// Requires UnmanagedCode permission.  
        /// If caller does not have this permission, the dragdrop will not occur.
        /// </remarks>
        public static DragDropEffects DoDragDrop(DependencyObject dragSource, object data, DragDropEffects allowedEffects)
        {
            DataObject dataObject;

            if (dragSource == null)
            {
                throw new ArgumentNullException("dragSource");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            RoutedEventArgs args = new RoutedEventArgs(DragDropStartedEvent, dragSource);
            
            // Raise the DragDropStarted internal event(Bubble).
            if (dragSource is UIElement)
            {
                ((UIElement)dragSource).RaiseEvent(args);
            }
            else if (dragSource is ContentElement)
            {
                ((ContentElement)dragSource).RaiseEvent(args);
            }
            else if (dragSource is UIElement3D)
            {
                ((UIElement3D)dragSource).RaiseEvent(args);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "dragSource");
            }

            dataObject = data as DataObject;

            if (dataObject == null)
            {
                // Create DataObject for DragDrop from the data.
                dataObject = new DataObject(data);
            }

            // Call OleDoDragDrop with DataObject.
            DragDropEffects ret = OleDoDragDrop(dragSource, dataObject, allowedEffects);

            args = new RoutedEventArgs(DragDropCompletedEvent, dragSource);
            
            // Raise the DragDropCompleted internal event(Bubble).
            if (dragSource is UIElement)
            {
                ((UIElement)dragSource).RaiseEvent(args);
            }
            else if (dragSource is ContentElement)
            {
                ((ContentElement)dragSource).RaiseEvent(args);
            }
            else if (dragSource is UIElement3D)
            {
                ((UIElement3D)dragSource).RaiseEvent(args);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "dragSource");
            }

            return ret;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Register the drop target which want to a droppable window.
        /// </summary>
        /// <param name="windowHandle">
        /// The window handle to be drop target .
        /// </param>
        internal static void RegisterDropTarget(IntPtr windowHandle)
        {
            if (windowHandle != IntPtr.Zero)
            {
                // Create OleDragSource and call Ole DoDragDrop for starting DragDrop.
                OleDropTarget oleDropTarget = new OleDropTarget(windowHandle);

                // Call OLE RegisterDragDrop and it will get the drop target events during drag-and-drop
                // operation on the drop target window.
                OleServicesContext.CurrentOleServicesContext.OleRegisterDragDrop(
                    new HandleRef(null, windowHandle),
                    (UnsafeNativeMethods.IOleDropTarget)oleDropTarget);
            }
        }

        /// <summary>
        /// Revoke the drop target which was a droppable window.
        /// </summary>
        /// <param name="windowHandle">
        /// The window handle that can accept drop.
        /// </param>        
        internal static void RevokeDropTarget(IntPtr windowHandle)
        {
            if (windowHandle != IntPtr.Zero)
            {
                // Call OLE RevokeDragDrop to revoke the droppable target window.
                OleServicesContext.CurrentOleServicesContext.OleRevokeDragDrop(
                    new HandleRef(null, windowHandle));
            }
        }

        /// <summary>
        /// Validate the dragdrop effects of DragDrop.
        /// </summary>
        internal static bool IsValidDragDropEffects(DragDropEffects dragDropEffects)
        {
            int dragDropEffectsAll;

            dragDropEffectsAll = (int)(DragDropEffects.None |
                                       DragDropEffects.Copy |
                                       DragDropEffects.Move |
                                       DragDropEffects.Link |
                                       DragDropEffects.Scroll |
                                       DragDropEffects.All);

            if (((int)dragDropEffects & ~dragDropEffectsAll) != 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate the drag action of DragDrop.
        /// </summary>
        internal static bool IsValidDragAction(DragAction dragAction)
        {
            if (dragAction == DragAction.Continue ||
                dragAction == DragAction.Drop ||
                dragAction == DragAction.Cancel)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Validate the key states of DragDrop.
        /// </summary>
        internal static bool IsValidDragDropKeyStates(DragDropKeyStates dragDropKeyStates)
        {
            int keyStatesAll;

            keyStatesAll = (int)(DragDropKeyStates.LeftMouseButton |
                                 DragDropKeyStates.RightMouseButton |
                                 DragDropKeyStates.ShiftKey |
                                 DragDropKeyStates.ControlKey |
                                 DragDropKeyStates.MiddleMouseButton |
                                 DragDropKeyStates.AltKey);

            if (((int)dragDropKeyStates & ~keyStatesAll) != 0)
            {
                return false;
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

        /// <summary>
        /// Begins a drag-and-drop operation through OLE DoDragDrop.
        /// </summary>
        /// <param name="dragSource">
        /// The drag source object.
        /// </param>
        /// <param name="dataObject">
        /// The data object to drag.
        /// </param>
        /// <param name="allowedEffects">
        /// The allowed effects that is one of the DragDropEffects values.
        /// </param>
        private static DragDropEffects OleDoDragDrop(DependencyObject dragSource, DataObject dataObject, DragDropEffects allowedEffects)
        {
            int[] dwEffect;
            OleDragSource oleDragSource;

            Debug.Assert(dragSource != null, "Invalid dragSource");
            Debug.Assert(dataObject != null, "Invalid dataObject");

            // Create the int array for passing parameter of OLE DoDragDrop
            dwEffect = new int[1];

            // Create OleDragSource and call Ole DoDragDrop for starting DragDrop.
            oleDragSource = new OleDragSource(dragSource);

            // Call OLE DoDragDrop and it will hanlde all mouse and keyboard input until drop the object.
            // We don't need to check the error return since PreserveSig attribute is defined as "false"
            // which will pops up the exception automatically.
            OleServicesContext.CurrentOleServicesContext.OleDoDragDrop(
                                                            (IComDataObject)dataObject,
                                                            (UnsafeNativeMethods.IOleDropSource)oleDragSource,
                                                            (int)allowedEffects,
                                                            dwEffect);

            // return the drop effect of DragDrop.
            return (DragDropEffects)dwEffect[0];
        }

        #endregion Private Methods
    }

    #endregion DragDrop    


    #region OleDragSource

    /// <summary>
    /// OleDragSource that handle ole QueryContinueDrag and GiveFeedback.
    /// </summary>
    internal class OleDragSource : UnsafeNativeMethods.IOleDropSource
    {
        //------------------------------------------------------
        //
        //  Constructor
        //
        //------------------------------------------------------

        #region Constructor

        /// <summary>
        /// OleDragSource constructor.
        /// </summary>
        public OleDragSource(DependencyObject dragSource)
        {
            _dragSource = dragSource;
        }

        #endregion Constructor

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region IOleDropSource

        /// <summary>
        /// Query the source to know the drag continue or not.
        /// </summary>
        int UnsafeNativeMethods.IOleDropSource.OleQueryContinueDrag(int escapeKey, int grfkeyState)
        {
            bool escapePressed;
            QueryContinueDragEventArgs args;

            escapePressed = false;

            if (escapeKey != 0)
            {
                escapePressed = true;
            }

            // Create QueryContinueDrag event arguments.
            args = new QueryContinueDragEventArgs(escapePressed, (DragDropKeyStates)grfkeyState);

            // Raise the query continue drag event for both Tunnel(Preview) and Bubble.
            RaiseQueryContinueDragEvent(args);

            // Check the drag continue result.
            if (args.Action == DragAction.Continue)
            {
                return NativeMethods.S_OK;
            }
            else if (args.Action == DragAction.Drop)
            {
                return NativeMethods.DRAGDROP_S_DROP;
            }
            else if (args.Action == DragAction.Cancel)
            {
                return NativeMethods.DRAGDROP_S_CANCEL;
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Give feedback from the source whether use the default cursor or not.
        /// </summary>
        int UnsafeNativeMethods.IOleDropSource.OleGiveFeedback(int effect)
        {
            GiveFeedbackEventArgs args;

            // Create GiveFeedback event arguments.
            args = new GiveFeedbackEventArgs((DragDropEffects)effect, /*UseDefaultCursors*/ false);

            // Raise the give feedback event for both Tunnel(Preview) and Bubble.
            RaiseGiveFeedbackEvent(args);

            // Check the give feedback result whether use default cursors or not.
            if (args.UseDefaultCursors)
            {
                return NativeMethods.DRAGDROP_S_USEDEFAULTCURSORS;
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Raise QueryContinueDrag event for Tunnel and Bubble.
        /// </summary>
        private void RaiseQueryContinueDragEvent(QueryContinueDragEventArgs args)
        {
            // Set PreviewQueryContinueDrag(Tunnel) first.
            args.RoutedEvent=DragDrop.PreviewQueryContinueDragEvent;

            // Raise the preview QueryContinueDrag event(Tunnel).
            if (_dragSource is UIElement)
            {
                ((UIElement)_dragSource).RaiseEvent(args);
            }
            else if (_dragSource is ContentElement)
            {
                ((ContentElement)_dragSource).RaiseEvent(args);
            }
            else if (_dragSource is UIElement3D)
            {
                ((UIElement3D)_dragSource).RaiseEvent(args);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "scope");
            }

            // Set QueryContinueDrag(Bubble).
            args.RoutedEvent = DragDrop.QueryContinueDragEvent;

            // Raise QueryContinueDrag event(Bubble).
            if (!args.Handled)
            {
                if (_dragSource is UIElement)
                {
                    ((UIElement)_dragSource).RaiseEvent(args);
                }
                else if (_dragSource is ContentElement)
                {
                    ((ContentElement)_dragSource).RaiseEvent(args);
                }
                else if (_dragSource is UIElement3D)
                {
                    ((UIElement3D)_dragSource).RaiseEvent(args);
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "scope");
                }
            }

            // Call the default event handling method internally if no one handle the drag source events.
            if (!args.Handled)
            {
                OnDefaultQueryContinueDrag(args);
            }
        }

        /// <summary>
        /// Raise GiveFeedback event for Tunnel and Bubble.
        /// </summary>
        private void RaiseGiveFeedbackEvent(GiveFeedbackEventArgs args)
        {
            // Set PreviewGiveFeedback(Tunnel) first.
            args.RoutedEvent=DragDrop.PreviewGiveFeedbackEvent;

            // Raise the preview GiveFeedback(Tunnel).
            if (_dragSource is UIElement)
            {
                ((UIElement)_dragSource).RaiseEvent(args);
            }
            else if (_dragSource is ContentElement)
            {
                ((ContentElement)_dragSource).RaiseEvent(args);
            }
            else if (_dragSource is UIElement3D)
            {
                ((UIElement3D)_dragSource).RaiseEvent(args);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "scope");
            }

            // Set GiveFeedback event ID(Bubble).
            args.RoutedEvent = DragDrop.GiveFeedbackEvent;

            if (!args.Handled)
            {
                // Raise GiveFeedback event(Bubble).
                if (_dragSource is UIElement)
                {
                    ((UIElement)_dragSource).RaiseEvent(args);
                }
                else if (_dragSource is ContentElement)
                {
                    ((ContentElement)_dragSource).RaiseEvent(args);
                }
                else if (_dragSource is UIElement3D)
                {
                    ((UIElement3D)_dragSource).RaiseEvent(args);
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "scope");
                }
            }

            // Call the default event handling method internally if no one handle the drag source events.
            if (!args.Handled)
            {
                OnDefaultGiveFeedback(args);
            }
        }

        /// <summary>
        /// Default query continue drag during drag-and-drop operation.
        /// </summary>
        /// <remarks>
        ///     At this stage we do not know application's intended mouse
        ///     button combination for dragging. For default purposes we assume
        ///     that the DragDrop happens due to single mouse button press.
        ///     Hence if any two mouse buttons are pressed at any point of time,
        ///     then we cancel the drapdrop. Also if no mouse buttons are pressed at
        ///     any point of time, then we complete the drop. If an application intends
        ///     to provide multi-button press dragging (like dragging by pressing 
        ///     both left and right buttons of mouse) applications will have
        ///     to handle (Preview)QueryContinueDragEvent to the allow 
        ///     such valid combinations explicitly.
        /// </remarks>
        private void OnDefaultQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            int mouseButtonDownCount = 0;
            
            if ((e.KeyStates & DragDropKeyStates.LeftMouseButton) == DragDropKeyStates.LeftMouseButton)
            {
                mouseButtonDownCount++;
            }
            if ((e.KeyStates & DragDropKeyStates.MiddleMouseButton) == DragDropKeyStates.MiddleMouseButton)
            {
                mouseButtonDownCount++;
            }
            if ((e.KeyStates & DragDropKeyStates.RightMouseButton) == DragDropKeyStates.RightMouseButton)
            {
                mouseButtonDownCount++;
            }

            e.Action = DragAction.Continue;

            if (e.EscapePressed ||
                mouseButtonDownCount >= 2)
            {
                e.Action = DragAction.Cancel;
            }
            else if (mouseButtonDownCount == 0)
            {
                e.Action = DragAction.Drop;
            }
        }

        /// <summary>
        /// Default give feedback during drag-and-drop operation.
        /// </summary>
        private void OnDefaultGiveFeedback(GiveFeedbackEventArgs e)
        {
            // Show the default DragDrop cursor.
            e.UseDefaultCursors = true;
        }

        #endregion IOleDropSource

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private DependencyObject _dragSource;

        #endregion Private Fields
    }

    #endregion OleDragSource

    #region OleDropTarget

    /// <summary>
    /// OleDropTarget that handle ole DragEnter DragOver DragLeave and DragDrop.
    /// </summary>
    internal class OleDropTarget : DispatcherObject, UnsafeNativeMethods.IOleDropTarget
    {
        //------------------------------------------------------
        //
        //  Constructor
        //
        //------------------------------------------------------
        
        #region Constructor

        /// <summary>
        /// OleDropTarget Constructor.
        /// </summary>
        public OleDropTarget(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentNullException("handle");
            }

            _windowHandle = handle;
        }

        #endregion Constructor

        //------------------------------------------------------
        //
        //  IOleDropTarget Interface
        //
        //------------------------------------------------------

        #region IOleDropTarget

        /// <summary>
        /// OleDragEnter - check the data object and notify DragEnter to the target element.
        /// </summary>
        int UnsafeNativeMethods.IOleDropTarget.OleDragEnter(object data, int dragDropKeyStates, long point, ref int effects)
        {
            DependencyObject target;
            Point targetPoint;

            // Get the data object and immediately return if there isn't the data object or no available data.
            _dataObject = GetDataObject(data);
            if (_dataObject == null || !IsDataAvailable(_dataObject))
            {
                // Set the none effect.
                effects = (int)DragDropEffects.None;

                return NativeMethods.S_FALSE;
            }

            // Get the current target from the mouse drag point that is based on screen.
            target = GetCurrentTarget(point, out targetPoint);

            // Set the last target element with the current target.
            _lastTarget = target;

            if (target != null)
            {
                // Create DragEvent argument and then raise DragEnter event for Tunnel or Bubble event.
                RaiseDragEvent(
                    DragDrop.DragEnterEvent,
                    dragDropKeyStates,
                    ref effects,
                    target,
                    targetPoint);
            }
            else
            {
                // Set the none effect.
                effects = (int)DragDropEffects.None;
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// OleDragOver - get the drop effect from the target element.
        /// </summary>
        int UnsafeNativeMethods.IOleDropTarget.OleDragOver(int dragDropKeyStates, long point, ref int effects)
        {
            DependencyObject target;
            Point targetPoint;

            Invariant.Assert(_dataObject != null);

            // Get the current target from the mouse drag point that is based on screen.
            target = GetCurrentTarget(point, out targetPoint);

            // Raise DragOver event to the target to get DragDrop effect status from the target.
            if (target != null)
            {
                // Avalon apps can have only one window handle, so we need to generate DragLeave and 
                // DragEnter event to target when target is changed by the mouse dragging.
                // If the current target is the same as the last target, just raise DragOver event to the target.
                if (target != _lastTarget)
                {
                    try
                    {
                        if (_lastTarget != null)
                        {
                            // Raise DragLeave event to the last target.
                            RaiseDragEvent(
                                DragDrop.DragLeaveEvent,
                                dragDropKeyStates,
                                ref effects,
                                _lastTarget,
                                targetPoint);
                        }

                        // Raise DragEnter event to the new target.
                        RaiseDragEvent(
                            DragDrop.DragEnterEvent,
                            dragDropKeyStates,
                            ref effects,
                            target,
                            targetPoint);
                    }
                    finally
                    {
                        // Reset the last target element to check it with the next current element.
                        _lastTarget = target;
                    }
                }
                else
                {
                    // Raise DragOver event to the target.
                    RaiseDragEvent(
                        DragDrop.DragOverEvent,
                        dragDropKeyStates,
                        ref effects,
                        target,
                        targetPoint);
                }
            }
            else
            {
                try
                {
                    if (_lastTarget != null)
                    {
                        // Raise DragLeave event to the last target.
                        RaiseDragEvent(
                            DragDrop.DragLeaveEvent,
                            dragDropKeyStates,
                            ref effects,
                            _lastTarget,
                            targetPoint);
                    }
                }
                finally
                {
                    // Update the last target element as the current target element.
                    _lastTarget = target;
                    effects = (int)DragDropEffects.None;
                }
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// OleDragLeave.
        /// </summary>
        int UnsafeNativeMethods.IOleDropTarget.OleDragLeave()
        {
            if (_lastTarget != null)
            {
                int effects;

                // Set DragDrop effects as DragDropEffects.None
                effects = 0;

                try
                {
                    // Raise DragLeave event for the last target element.
                    RaiseDragEvent(
                        DragDrop.DragLeaveEvent,
                        /* DragDropKeyStates.None */ 0,
                        ref effects,
                        _lastTarget,
                        new Point(0, 0));
                }
                finally
                {
                    // Reset the last target and data object.
                    _lastTarget = null;
                    _dataObject = null;
                }
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// OleDrop - drop the object to the target element.
        /// </summary>
        int UnsafeNativeMethods.IOleDropTarget.OleDrop(object data, int dragDropKeyStates, long point, ref int effects)
        {
            IDataObject dataObject;
            DependencyObject target;
            Point targetPoint;

            // Get the data object and then immediately return fail if there isn't the proper data.
            dataObject = GetDataObject(data);
            if (dataObject == null || !IsDataAvailable(dataObject))
            {
                effects = (int)DragDropEffects.None;

                return NativeMethods.S_FALSE;
            }

            // Reset last element and target point.
            _lastTarget = null;

            // Get the current target from the screen mouse point.
            target = GetCurrentTarget(point, out targetPoint);

            // Raise Drop event to the target element.
            if (target != null)
            {
                // Raise Drop event to the drop target.
                RaiseDragEvent(
                    DragDrop.DropEvent,
                    dragDropKeyStates,
                    ref effects,
                    target,
                    targetPoint);
            }
            else
            {
                effects = (int)DragDropEffects.None;
            }

            return NativeMethods.S_OK;
        }

        #endregion IOleDropTarget

        #region Private Methods

        /// <summary>
        /// Raise Drag(Enter/Over/Leave/Drop) events to the target.
        /// </summary>
        private void RaiseDragEvent(RoutedEvent dragEvent, int dragDropKeyStates, ref int effects, DependencyObject target, Point targetPoint)
        {
            DragEventArgs dragEventArgs;

            Invariant.Assert(_dataObject != null);
            Invariant.Assert(target != null);

            // Create DragEvent argument to raise DragEnter events to the target.
            dragEventArgs = new DragEventArgs(
                _dataObject,
                (DragDropKeyStates)dragDropKeyStates,
                (DragDropEffects)effects,
                target,
                targetPoint);

            // Set the preview(Tunnel) drop target events(Tunnel) first.
            if (dragEvent == DragDrop.DragEnterEvent)
            {
                dragEventArgs.RoutedEvent = DragDrop.PreviewDragEnterEvent;
            }
            else if (dragEvent == DragDrop.DragOverEvent)
            {
                dragEventArgs.RoutedEvent = DragDrop.PreviewDragOverEvent;
            }
            else if (dragEvent == DragDrop.DragLeaveEvent)
            {
                dragEventArgs.RoutedEvent = DragDrop.PreviewDragLeaveEvent;
            }
            else if (dragEvent == DragDrop.DropEvent)
            {
                dragEventArgs.RoutedEvent = DragDrop.PreviewDropEvent;
            }

            // Raise the preview drop target events(Tunnel).
            if (target is UIElement)
            {
                ((UIElement)target).RaiseEvent(dragEventArgs);
            }
            else if (target is ContentElement)
            {
                ((ContentElement)target).RaiseEvent(dragEventArgs);
            }
            else if (target is UIElement3D)
            {
                ((UIElement3D)target).RaiseEvent(dragEventArgs);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "scope");
            }

            // Raise the bubble DragEvent event if the preview DragEvent isn't handled.
            if (!dragEventArgs.Handled)
            {
                // Set the drop target events(Bubble).
                dragEventArgs.RoutedEvent = dragEvent;

                // Raise the drop target events(Bubble).
                if (target is UIElement)
                {
                    ((UIElement)target).RaiseEvent(dragEventArgs);
                }
                else if (target is ContentElement)
                {
                    ((ContentElement)target).RaiseEvent(dragEventArgs);
                }
                else if (target is UIElement3D)
                {
                    ((UIElement3D)target).RaiseEvent(dragEventArgs);
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.ScopeMustBeUIElementOrContent), "scope");
                }
            }

            // Call the default drop target event handling method internally if no one handle the drop target events.
            if (!dragEventArgs.Handled)
            {
                if (dragEvent == DragDrop.DragEnterEvent)
                {
                    OnDefaultDragEnter(dragEventArgs);
                }
                else if (dragEvent == DragDrop.DragOverEvent)
                {
                    OnDefaultDragOver(dragEventArgs);
                }
            }

            // Update DragDrop effects after raise DragEvent.
            effects = (int)dragEventArgs.Effects;
        }

        /// <summary>
        /// Default drag enter during drag-and-drop operation.
        /// </summary>
        private void OnDefaultDragEnter(DragEventArgs e)
        {
            bool ctrlKeyDown;

            // If there's no supported data available, don't allow the drag-and-drop.
            if (e.Data == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // Ok, there's data to move or copy here.
            if ((e.AllowedEffects & DragDropEffects.Move) != 0)
            {
                e.Effects = DragDropEffects.Move;
            }

            ctrlKeyDown = ((int)(e.KeyStates & DragDropKeyStates.ControlKey) != 0);

            if (ctrlKeyDown)
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// Default drag over during drag-and-drop operation.
        /// </summary>
        private void OnDefaultDragOver(DragEventArgs e)
        {
            bool ctrlKeyDown;

            // If there's no supported data available, don't allow the drag-and-drop.
            if (e.Data == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // Ok, there's data to move or copy here.
            if ((e.AllowedEffects & DragDropEffects.Move) != 0)
            {
                e.Effects = DragDropEffects.Move;
            }

            ctrlKeyDown = ((int)(e.KeyStates & DragDropKeyStates.ControlKey) != 0);

            if (ctrlKeyDown)
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// Get the client point from the screen point.
        /// </summary>
        private Point GetClientPointFromScreenPoint(long dragPoint, PresentationSource source)
        {
            Point screenPoint;
            Point clientPoint;

            // Convert the screen point to the client window point
            screenPoint = new Point((int)(dragPoint & 0xffffffff), (int)((dragPoint >> 32) & 0xffffffff));
            clientPoint = PointUtil.ScreenToClient(screenPoint, source);

            return clientPoint;
        }

        /// <summary>
        /// Get the current target object and target point from the mouse dragging point
        /// that is the screen point.
        /// </summary>
        private DependencyObject GetCurrentTarget(long dragPoint, out Point targetPoint)
        {
            HwndSource source;
            DependencyObject target;

            // Initialize the target as null.
            target = null;

            // Get the source from the source to hit-test and translate point.
            source = HwndSource.FromHwnd(_windowHandle);

            // Get the client point from the screen point.
            targetPoint = GetClientPointFromScreenPoint(dragPoint, source);

            if (source != null)
            {
                UIElement targetUIElement;

                // Hit-Testing to get the target object from the current mouse dragging point.
                // LocalHitTest() will get the hit-tested object from the mouse dragging point after 
                // conversion the pixel to the measure unit.
                target = MouseDevice.LocalHitTest(targetPoint, source) as DependencyObject;

                targetUIElement = target as UIElement;
                if (targetUIElement != null)
                {
                    if (targetUIElement.AllowDrop)
                    {
                        // Assign the target as the UIElement.
                        target = targetUIElement;
                    }
                    else
                    {
                        target = null;
                    }
                }
                else
                {
                    ContentElement targetContentElement;

                    targetContentElement = target as ContentElement;
                    if (targetContentElement != null)
                    {
                        if (targetContentElement.AllowDrop)
                        {
                            // Assign the target as the ContentElement.
                            target = targetContentElement;
                        }
                        else
                        {
                            target = null;
                        }
                    }
                    else
                    {
                        UIElement3D targetUIElement3D;

                        targetUIElement3D = target as UIElement3D;
                        if (targetUIElement3D != null)
                        {
                            if (targetUIElement3D.AllowDrop)
                            {
                                target = targetUIElement3D;                        
                            }
                            else
                            {
                                target = null;
                            }
                        }
                    }
                }

                if (target != null)
                {
                    // Translate the client point to the root point and then translate it to target point.
                    targetPoint = PointUtil.ClientToRoot(targetPoint, source);
                    targetPoint = InputElement.TranslatePoint(targetPoint, source.RootVisual, target);
                }
            }

            return target;
        }

        /// <summary>
        /// Get the data object.
        /// </summary>
        private IDataObject GetDataObject(object data)
        {
            IDataObject dataObject;

            dataObject = null;

            // We see if data is available on the data object.
            if (data != null)
            {
                if (data is DataObject)
                {
                    dataObject = (DataObject)data;
                }
                else
                {
                    dataObject = new DataObject((IComDataObject)data);
                }
            }

            return dataObject;
        }

        /// <summary>
        /// Check the available data.
        /// </summary>
        private bool IsDataAvailable(IDataObject dataObject)
        {
            bool dataAvailable;

            dataAvailable = false;

            if (dataObject != null)
            {
                string[] formats;

                formats = dataObject.GetFormats();

                for (int i = 0; i < formats.Length; i++)
                {
                    if (dataObject.GetDataPresent(formats[i]))
                    {
                        dataAvailable = true;
                        break;
                    }
                }
            }

            return dataAvailable;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IntPtr _windowHandle;

        private IDataObject _dataObject;

        private DependencyObject _lastTarget;

        #endregion Private Fields
    }  

    #endregion OleDropTarget
}

