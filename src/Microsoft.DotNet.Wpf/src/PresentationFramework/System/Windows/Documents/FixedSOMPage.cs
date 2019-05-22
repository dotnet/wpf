// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                         
    Description:
        A semantic container that contains all the first-level containers on the page      
--*/

namespace System.Windows.Documents
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Markup;    // for XmlLanguage
    using System.Windows.Media;
    using System.Globalization;
    using System.Diagnostics;
    
    internal sealed class FixedSOMPage: FixedSOMContainer
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMPage()
        {
        }
        #endregion Constructors


        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region Public Methods
#if DEBUG
        public override void Render(DrawingContext dc, string label, DrawDebugVisual debugVisuals)
        {
            switch (debugVisuals)
            {
                case DrawDebugVisual.None:
                case DrawDebugVisual.Glyphs: //Handled in FixedPage
                    //Nothing to do
                    break;
                default:
                    int groupIndex = 0;
                    int boxIndex = 0;
                    for (int i=0; i<_semanticBoxes.Count; i++)
                    {
                        FixedSOMGroup group = _semanticBoxes[i] as FixedSOMGroup;
                        if (group != null)
                        {
                            if (debugVisuals == DrawDebugVisual.Groups)
                            {
                                group.Render(dc, groupIndex.ToString(),  debugVisuals);
                                groupIndex++;
                            }
                            List<FixedSOMSemanticBox> groupBoxes = group.SemanticBoxes;
                            for (int j=0; j<groupBoxes.Count; j++)
                            {
                                groupBoxes[j].Render(dc, boxIndex.ToString(), debugVisuals);
                                boxIndex++;
                            }
                        }
                        else 
                        {
                            _semanticBoxes[i].Render(dc, boxIndex.ToString(), debugVisuals);
                            boxIndex++;
                        }
                    }                    
                    break;
            }
        }
#endif        

        public void AddFixedBlock(FixedSOMFixedBlock fixedBlock)
        {
            base.Add(fixedBlock);
        }

        public void AddTable(FixedSOMTable table)
        {
            base.Add(table);
        }

        public override void SetRTFProperties(FixedElement element)
        {
            if (_cultureInfo != null)
            {
                element.SetValue(FrameworkContentElement.LanguageProperty, XmlLanguage.GetLanguage(_cultureInfo.IetfLanguageTag));
            }
        }

        #endregion Public Methods

        //--------------------------------------------------------------------
        //
        // Internal properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        internal override FixedElement.ElementType[] ElementTypes
        {
            get
            {
                return new FixedElement.ElementType[1] { FixedElement.ElementType.Section };
            }
        }

        internal List<FixedNode> MarkupOrder
        {
            get
            {
                return _markupOrder;
            }
            set
            {
                _markupOrder = value;
            }
        }

        internal CultureInfo CultureInfo
        {
            set
            {
                _cultureInfo = value;
            }
        }

        #endregion Public Properties        

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        
        private List<FixedNode> _markupOrder;
        private CultureInfo _cultureInfo;

        #endregion Private Fields
    }
}


