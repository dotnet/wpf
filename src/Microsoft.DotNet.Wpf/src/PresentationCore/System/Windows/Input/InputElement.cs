﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;
using MS.Internal;
using System.Windows.Interop;
using System.Windows.Media.Media3D;

namespace System.Windows.Input
{
    // Helper class for working with IInputElement objects.
    internal static class InputElement
    {
        // Return whether the InputElement is one of our types.
        internal static bool IsValid(IInputElement e)
        {
            DependencyObject o = e as DependencyObject;
            return IsValid(o);
        }

        internal static bool IsValid(DependencyObject o)
        {
            return o is UIElement or ContentElement or UIElement3D; 
}

        // Returns the containing input element of the given DynamicObject.
        // If onlyTraverse2D is set to true, then we stop once we see a 3D object and return null
        internal static DependencyObject GetContainingUIElement(DependencyObject o, bool onlyTraverse2D)
        {
            DependencyObject container = null;

            if(o != null)
            {
                if(o is UIElement)
                {
                    container = o;
                }
                else if (o is UIElement3D && !onlyTraverse2D)
                {
                    container = o;
                } 
                else if(o is ContentElement contentElement)
                {
                    DependencyObject parent = ContentOperations.GetParent(contentElement);
                    if(parent != null)
                    {
                        container = GetContainingUIElement(parent, onlyTraverse2D);
                    }
                    else
                    {
                        parent = contentElement.GetUIParentCore();
                        if(parent != null)
                        {
                            container = GetContainingUIElement(parent, onlyTraverse2D);
                        }
                    }
                }
                else if (o is Visual v)
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(v);
                    if(parent != null)
                    {
                        container = GetContainingUIElement(parent, onlyTraverse2D);
                    }
                }
                else if (!onlyTraverse2D && o is Visual3D v3D)
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(v3D);
                    if (parent != null)
                    {
                        container = GetContainingUIElement(parent, onlyTraverse2D);
                    }
                }
            }

            return container;
        }
        
        // Returns the containing input element of the given DynamicObject.
        internal static DependencyObject GetContainingUIElement(DependencyObject o)
        {
            return GetContainingUIElement(o, false);
        }
               

        // Returns the containing input element of the given DynamicObject.
        // If onlyTraverse2D is set to true, then we stop once we see a 3D object and return null
        internal static IInputElement GetContainingInputElement(DependencyObject o, bool onlyTraverse2D)
        {
            IInputElement container = null;

            if(o != null)
            {
                if(o is UIElement uiElement)
                {
                    container = uiElement;
                }
                else if(o is ContentElement contentElement)
                {
                    container = contentElement;
                }
                else if (o is UIElement3D uIElement3D && !onlyTraverse2D)
                {
                    container = uIElement3D;
                }
                else if(o is Visual v)
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(v);
                    if(parent != null)
                    {
                        container = GetContainingInputElement(parent, onlyTraverse2D);
                    }
                }
                else if (!onlyTraverse2D && o is Visual3D v3D)
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(v3D);
                    if (parent != null)
                    {
                        container = GetContainingInputElement(parent, onlyTraverse2D);
                    }
                }
            }

            return container;
        }

        // Returns the containing input element of the given DynamicObject.
        //
        internal static IInputElement GetContainingInputElement(DependencyObject o)
        {
            return GetContainingInputElement(o, false);
        }
        
        // Returns the containing visual of the given DynamicObject.
        internal static DependencyObject GetContainingVisual(DependencyObject o)
        {
            DependencyObject v = null;

            if(o != null)
            {
                if(o is UIElement uiElement)
                {
                    v = uiElement;
                }
                else if (o is Visual3D visual3D)
                {
                    v = visual3D;
                }
                else if(o is ContentElement contentElement)
                {
                    DependencyObject parent = ContentOperations.GetParent(contentElement);
                    if(parent != null)
                    {
                        v = GetContainingVisual(parent);
                    }
                    else
                    {
                        parent = contentElement.GetUIParentCore();
                        if(parent != null)
                        {
                            v = GetContainingVisual(parent);
                        }
                    }
                }
                else
                {
                    v = o as Visual;

                    if (v == null)
                    {
                        v = o as Visual3D;
                    }
                }
            }

            return v;
        }

        // Returns the root visual of the containing element.
        internal static DependencyObject GetRootVisual(DependencyObject o)
        {
            return GetRootVisual(o, enable2DTo3DTransition: true);
        }
        
        internal static DependencyObject GetRootVisual(DependencyObject o, bool enable2DTo3DTransition)
        {
            DependencyObject rootVisual = GetContainingVisual(o);
            DependencyObject parentVisual;
                       
            while(rootVisual != null && ((parentVisual = VisualTreeHelper.GetParent(rootVisual)) != null))
            {
                // if we are not supposed to transition from 2D to 3D and the root 
                // is a Visual and the parent is a Visual3D break
                if (!enable2DTo3DTransition && 
                     rootVisual is Visual && parentVisual is Visual3D)

                {
                    break;
                }
                
                rootVisual = parentVisual;
            }
            

            return rootVisual;
        }

        internal static Point TranslatePoint(Point pt, DependencyObject from, DependencyObject to)
        {
            bool unused = false;
            return TranslatePoint(pt, from, to, out unused);
        }

        internal static Point TranslatePoint(Point pt, DependencyObject from, DependencyObject to, out bool translated)
        {
            translated = false;

            Point ptTranslated = pt;

            // Get the containing and root visuals we are coming from.
            DependencyObject vFromAsDO = InputElement.GetContainingVisual(from);
            Visual rootFrom = InputElement.GetRootVisual(from) as Visual;
            
            Visual vFrom = vFromAsDO as Visual;

            if (vFromAsDO != null && vFrom == null)
            {
                // must be a Visual3D - get it's 2D visual parent
                vFrom = VisualTreeHelper.GetContainingVisual2D(vFromAsDO);
            }

            if(vFrom != null && rootFrom != null)
            {
                GeneralTransform gUp;
                Matrix mUp;

                bool isUpSimple = false;
                isUpSimple = vFrom.TrySimpleTransformToAncestor(rootFrom,
                                                                inverse: false,
                                                                out gUp,
                                                                out mUp);               
                if (isUpSimple)
                {
                    ptTranslated = mUp.Transform(ptTranslated);
                }
                else if (!gUp.TryTransform(ptTranslated, out ptTranslated))
                {
                    // Error.  Out parameter has been set false.
                    return new Point();
                }

                // If no element was specified to translate to, we leave the coordinates
                // translated to the root.
                if(to != null)
                {
                    // Get the containing and root visuals we are going to.
                    DependencyObject vTo = InputElement.GetContainingVisual(to);
                    Visual rootTo = InputElement.GetRootVisual(to) as Visual;

                    if(vTo != null && rootTo != null)
                    {
                        // If both are under the same root visual, we can easily translate the point
                        // between them by translating up to the root, and then back down.
                        //
                        // However, if both are under different roots, we can only translate
                        // between them if we know how to relate the two root visuals.  Currently
                        // we only know how to do that if both roots are sourced in HwndSources.
                        if(rootFrom != rootTo)
                        {
                            HwndSource sourceFrom = PresentationSource.CriticalFromVisual(rootFrom) as HwndSource;
                            HwndSource sourceTo = PresentationSource.CriticalFromVisual(rootTo) as HwndSource;


                            if(sourceFrom != null && sourceFrom.Handle != IntPtr.Zero && sourceFrom.CompositionTarget != null &&
                               sourceTo != null && sourceTo.Handle != IntPtr.Zero && sourceTo.CompositionTarget != null)
                            {
                                // Translate the point into client coordinates.
                                ptTranslated = PointUtil.RootToClient(ptTranslated, sourceFrom);

                                // Translate the point into screen coordinates.
                                Point ptScreen = PointUtil.ClientToScreen(ptTranslated, sourceFrom);

                                // Translate the point back the the client coordinates of the To window.
                                ptTranslated = PointUtil.ScreenToClient(ptScreen, sourceTo);

                                // Translate the point back to the root element.
                                ptTranslated = PointUtil.ClientToRoot(ptTranslated, sourceTo);
}
                            else
                            {
                                // Error.  Out parameter has been set false.
                                return new Point();
                            }
                        }

                        // Translate the point from the root to the visual.
                        GeneralTransform gDown;
                        Matrix mDown;

                        Visual vToAsVisual = vTo as Visual;
                        if (vToAsVisual == null)
                        {
                            // must be a Visual3D
                            vToAsVisual = VisualTreeHelper.GetContainingVisual2D(vTo);
                        }

                        bool isDownSimple = vToAsVisual.TrySimpleTransformToAncestor(rootTo,
                                                                                     inverse: true,
                                                                                     out gDown,
                                                                                     out mDown);

                        if (isDownSimple)
                        {
                            ptTranslated = mDown.Transform(ptTranslated);
                        }
                        else if (gDown != null)
                        {
                            if (!gDown.TryTransform(ptTranslated, out ptTranslated))
                            {
                                // Error.  Out parameter has been set false.
                                return new Point();
                            }
                        }
                        else
                        {
                            // Error.  Out parameter has been set false.
                            return new Point();
                        }                                           
                    }
                    else
                    {
                        // Error.  Out parameter has been set false.
                        return new Point();
                    }
                }
            }
            else
            {
                // Error.  Out parameter has been set false.
                return new Point();
            }

            translated = true;
            return ptTranslated;
        }

        // Caches the ContentElement's DependencyObjectType
        private static DependencyObjectType ContentElementType = DependencyObjectType.FromSystemTypeInternal(typeof(ContentElement));

        // Caches the UIElement's DependencyObjectType
        private static DependencyObjectType UIElementType = DependencyObjectType.FromSystemTypeInternal(typeof(UIElement));

        // Caches the UIElement3D's DependencyObjectType
        private static DependencyObjectType UIElement3DType = DependencyObjectType.FromSystemTypeInternal(typeof(UIElement3D));    
    }
}

