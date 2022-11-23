// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      A helper class which deals with the operations related to the clipboard.
//
// Features:
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Markup;
using System.Xml;
using MS.Internal.PresentationFramework; //security helper

namespace MS.Internal.Ink
{
    // The bits which internally represents the formats supported by InkCanvas
    [System.Flags]
    internal enum InkCanvasClipboardDataFormats
    {
        None            = 0x00,     // None
        XAML            = 0x01,     // XAML
        ISF             = 0x02,     // ISF
    }


    /// <summary>
    /// ClipboardProcessor acts as a brige between InkCanvas and various clipboard data formats
    /// It provides the functionalies -
    ///     1. Check the supported data formats in an IDataObject
    ///     2. Copy the selections in an InkCavans to an IDataObject
    ///     3. Create the stroks or frameworkelement array if there is any supported data in an IDataObject
    /// </summary>
    internal class ClipboardProcessor
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------
        
        #region Constructors

        internal ClipboardProcessor(InkCanvas inkCanvas)
        {
            if ( inkCanvas == null )
            {
                throw new ArgumentNullException("inkCanvas");
            }

            _inkCanvas = inkCanvas;

            // Create our default preferred list - Only InkCanvasClipboardFormat.Isf is supported.
            _preferredClipboardData = new Dictionary<InkCanvasClipboardFormat, ClipboardData>();
            _preferredClipboardData.Add(InkCanvasClipboardFormat.InkSerializedFormat, new ISFClipboardData());
}

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// This method returns the bits flag if there are the supported data in the IDataObject.
        /// </summary>
        /// <param name="dataObject">The IDataObject instance</param>
        /// <returns>The matched clipboard format. Return -1 if there is no recognized format in the data object</returns>
        internal bool CheckDataFormats(IDataObject dataObject)
        {
            Debug.Assert(dataObject != null && _preferredClipboardData!= null);

            foreach ( KeyValuePair<InkCanvasClipboardFormat, ClipboardData> pair in _preferredClipboardData )
            {
                if ( pair.Value.CanPaste(dataObject) )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The method copies the current selection to the IDataObject if there is any
        /// Called by :
        ///             InkCanvas.CopyToDataObject
        /// </summary>
        /// <param name="dataObject">The IDataObject instance</param>
        /// <returns>true if there is data being copied. Otherwise return false</returns>
        internal InkCanvasClipboardDataFormats CopySelectedData(IDataObject dataObject)
        {
            InkCanvasClipboardDataFormats copiedDataFormat = InkCanvasClipboardDataFormats.None;
            InkCanvasSelection inkCanvasSelection = InkCanvas.InkCanvasSelection;

            StrokeCollection strokes = inkCanvasSelection.SelectedStrokes;
            if (strokes.Count > 1)
            {
                // 
                // order the strokes so they are in the correct z-order
                // they appear in on the InkCanvas, or else they will be inconsistent
                // if copied / pasted
                StrokeCollection orderedStrokes = new StrokeCollection();
                StrokeCollection inkCanvasStrokes = InkCanvas.Strokes; //cache to avoid multiple property gets
                for (int i = 0; i < inkCanvasStrokes.Count && strokes.Count != orderedStrokes.Count; i++)
                {
                    for (int j = 0; j < strokes.Count; j++)
                    {
                        if (inkCanvasStrokes[i] == strokes[j])
                        {
                            orderedStrokes.Add(strokes[j]);
                            break;
                        }
                    }
                }

                Debug.Assert(inkCanvasSelection.SelectedStrokes.Count == orderedStrokes.Count);
                //Make a copy collection since we will alter the transform before copying the data.
                strokes = orderedStrokes.Clone();
            }
            else
            {
                //we only have zero or one stroke so we don't need to order, but we 
                //do need to clone.
                strokes = strokes.Clone();
            }

            List<UIElement> elements = new List<UIElement>(inkCanvasSelection.SelectedElements);
            Rect bounds = inkCanvasSelection.SelectionBounds;

            // Now copy the selection in the below orders.
            if ( strokes.Count != 0 || elements.Count != 0 )
            {
                // 
                // The selection should be translated to the origin (0, 0) related to its bounds.
                // Get the translate transform as a relative bounds.
                Matrix transform = Matrix.Identity;
                transform.OffsetX = -bounds.Left;
                transform.OffsetY = -bounds.Top;

                // Add ISF data first.
                if ( strokes.Count != 0 )
                {
                    // Transform the strokes first.
                    inkCanvasSelection.TransformStrokes(strokes, transform);

                    ClipboardData data = new ISFClipboardData(strokes);
                    data.CopyToDataObject(dataObject);
                    copiedDataFormat |= InkCanvasClipboardDataFormats.ISF;
                }

                // Then add XAML data.
                if ( CopySelectionInXAML(dataObject, strokes, elements, transform, bounds.Size) )
                {
                    // We have to create an InkCanvas as a container and add all the selection to it.
                    copiedDataFormat |= InkCanvasClipboardDataFormats.XAML;
                }
            }
            else
            {
                Debug.Assert(false , "CopySelectData: InkCanvas should have a selection!");
            }

            return copiedDataFormat;
        }

        /// <summary>
        /// The method returns the Strokes or UIElement array if there is the supported data in the IDataObject
        /// </summary>
        /// <param name="dataObject">The IDataObject instance</param>
        /// <param name="newStrokes">The strokes which are converted from the data in the IDataObject</param>
        /// <param name="newElements">The elements array which are converted from the data in the IDataObject</param>
        internal bool PasteData(IDataObject dataObject, ref StrokeCollection newStrokes, ref List<UIElement> newElements)
        {
            Debug.Assert(dataObject != null && _preferredClipboardData!= null);

            // We honor the order in our preferred list.
            foreach ( KeyValuePair<InkCanvasClipboardFormat, ClipboardData> pair in _preferredClipboardData )
            {
                InkCanvasClipboardFormat format = pair.Key;
                ClipboardData data = pair.Value;
                
                if ( data.CanPaste(dataObject) )
                {
                    switch ( format )
                    {
                        case InkCanvasClipboardFormat.Xaml:
                        {
                            XamlClipboardData xamlData = (XamlClipboardData)data;
                            xamlData.PasteFromDataObject(dataObject);
                            
                            List<UIElement> elements = xamlData.Elements;

                            if (elements != null && elements.Count != 0)
                            {
                                // If the Xaml data has been set in an InkCanvas, the top element will be a container InkCanvas.
                                // In this case, the new elements will be the children of the container.
                                // Otherwise, the new elements will be whatever data from the data object.
                                if (elements.Count == 1 && ClipboardProcessor.InkCanvasDType.IsInstanceOfType(elements[0]))
                                {
                                    TearDownInkCanvasContainer((InkCanvas)( elements[0] ), ref newStrokes, ref newElements);
                                }
                                else
                                {
                                    // The new elements are the data in the data object.
                                    newElements = elements;
                                }
}
                            break;
                        }
                        case InkCanvasClipboardFormat.InkSerializedFormat:
                        {
                            // Retrieve the stroke data.
                            ISFClipboardData isfData = (ISFClipboardData)data;
                            isfData.PasteFromDataObject(dataObject);

                            newStrokes = isfData.Strokes;
                            break;
                        }
                        case InkCanvasClipboardFormat.Text:
                        {
                            // Convert the text data in the data object to a text box element.
                            TextClipboardData textData = (TextClipboardData)data;
                            textData.PasteFromDataObject(dataObject);
                            newElements = textData.Elements;
                            break;
                        }
                    }

                    // Once we've done pasting, just return now.
                    return true;
                }
            }

            // Nothing gets pasted.
            return false;
        }

        #endregion Internal Methods

        internal IEnumerable<InkCanvasClipboardFormat> PreferredFormats
        {
            get
            {
                Debug.Assert(_preferredClipboardData != null);

                foreach ( KeyValuePair<InkCanvasClipboardFormat, ClipboardData> pair in _preferredClipboardData )
                {
                    yield return pair.Key;
                }
            }
            set
            {
                Debug.Assert(value != null);

                Dictionary<InkCanvasClipboardFormat, ClipboardData> preferredData = new Dictionary<InkCanvasClipboardFormat, ClipboardData>();
                
                foreach ( InkCanvasClipboardFormat format in value )
                {
                    // If we find the duplicate format in our preferred list, we should just skip it.
                    if ( !preferredData.ContainsKey(format) )
                    {
                        ClipboardData clipboardData = null;
                        switch ( format )
                        {
                            case InkCanvasClipboardFormat.InkSerializedFormat:
                                clipboardData = new ISFClipboardData();
                                break;
                            case InkCanvasClipboardFormat.Xaml:
                                clipboardData = new XamlClipboardData();
                                break;
                            case InkCanvasClipboardFormat.Text:
                                clipboardData = new TextClipboardData();
                                break;
                            default:
                                throw new ArgumentException(SR.Get(SRID.InvalidClipboardFormat), "value");
                        }

                        preferredData.Add(format, clipboardData);
                    }
                }
            
                _preferredClipboardData = preferredData;
            }
        }        


        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Copy the current Selection in XAML format.
        /// Called by :
        ///             CopySelectedData
        /// </summary>
        /// <param name="dataObject"></param>
        /// <param name="strokes"></param>
        /// <param name="elements"></param>
        /// <param name="transform"></param>
        /// <param name="size"></param>
        /// <returns>True if the copy is succeeded</returns>
        private bool CopySelectionInXAML(IDataObject dataObject, StrokeCollection strokes, List<UIElement> elements, Matrix transform, Size size)
        {
                InkCanvas inkCanvas = new InkCanvas();

                // We already transform the Strokes in CopySelectedData.
                if (strokes.Count != 0)
                {
                    inkCanvas.Strokes = strokes;
                }

                int elementCount = elements.Count;
                if (elementCount != 0)
                {
                    InkCanvasSelection inkCanvasSelection = InkCanvas.InkCanvasSelection;

                    for (int i = 0; i < elementCount; i++)
                    {
                        // NOTICE-2005/05/05-WAYNEZEN,
                        // An element can't be added to two visual trees.
                        // So, we cannot add the elements to the new container since they have been added to the current InkCanvas.
                        // Here we have to do is according to the suggestion from Avalon team -
                        //      1. Presist the elements to Xaml 
                        //      2. Load the xaml to create the new instances of the elements.
                        //      3. Add the new instances to the new container.
                        string xml = XamlWriter.Save(elements[i]);

                        UIElement newElement = XamlReader.Load(new XmlTextReader(new StringReader(xml))) as UIElement;
                        ((IAddChild)inkCanvas).AddChild(newElement);

                        // Now we tranform the element.
                        inkCanvasSelection.UpdateElementBounds(elements[i], newElement, transform);
                    }
                }

                if (inkCanvas != null)
                {
                    inkCanvas.Width = size.Width;
                    inkCanvas.Height = size.Height;

                    ClipboardData data = new XamlClipboardData(new UIElement[] { inkCanvas });

                    try
                    {
                        data.CopyToDataObject(dataObject);
                    }
                    catch (SecurityException)
                    {
                        // If we hit a SecurityException under the PartialTrust, we should just fail the copy
                        // operation.
                        inkCanvas = null;
                    }
                }

                return inkCanvas != null;
        }

        private void TearDownInkCanvasContainer(InkCanvas rootInkCanvas, ref StrokeCollection newStrokes, ref List<UIElement> newElements)
        {
            newStrokes = rootInkCanvas.Strokes;

            if ( rootInkCanvas.Children.Count != 0 )
            {
                List<UIElement> children = new List<UIElement>(rootInkCanvas.Children.Count);
                foreach (UIElement uiElement in rootInkCanvas.Children)
                {
                    children.Add(uiElement);
                }

                // Remove the children for the container
                foreach ( UIElement child in children )
                {
                    rootInkCanvas.Children.Remove(child);
                }

                // The new elements will be the children.
                newElements = children;
            }
}

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Properties
        //
        //-------------------------------------------------------------------------------

        #region Private Properties

        private InkCanvas InkCanvas
        {
            get
            {
                return _inkCanvas;
            }
        }

        /// <summary>
        /// A static DependencyObjectType of the GrabHandleAdorner which can be used for quick type checking.
        /// </summary>
        /// <value>An DependencyObjectType object</value>
        private static DependencyObjectType InkCanvasDType
        {
            get
            {
                if ( s_InkCanvasDType == null )
                {
                    s_InkCanvasDType = DependencyObjectType.FromSystemTypeInternal(typeof(InkCanvas));
                }

                return s_InkCanvasDType;
            }
        }


        #endregion Private Properties

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private InkCanvas                       _inkCanvas;
        private static DependencyObjectType     s_InkCanvasDType;

        private Dictionary<InkCanvasClipboardFormat, ClipboardData> _preferredClipboardData;

        #endregion Private Fields
    }
}

