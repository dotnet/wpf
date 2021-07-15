// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xaml;
using System.Reflection;

namespace System.Windows.Baml2006
{
    partial class WpfSharedBamlSchemaContext: XamlSchemaContext
    {
        const int KnownPropertyCount = 268;


        private WpfKnownMember CreateKnownMember(short bamlNumber)
        {
            switch(bamlNumber)
            {
                case 1: return Create_BamlProperty_AccessText_Text();
                case 2: return Create_BamlProperty_BeginStoryboard_Storyboard();
                case 3: return Create_BamlProperty_BitmapEffectGroup_Children();
                case 4: return Create_BamlProperty_Border_Background();
                case 5: return Create_BamlProperty_Border_BorderBrush();
                case 6: return Create_BamlProperty_Border_BorderThickness();
                case 7: return Create_BamlProperty_ButtonBase_Command();
                case 8: return Create_BamlProperty_ButtonBase_CommandParameter();
                case 9: return Create_BamlProperty_ButtonBase_CommandTarget();
                case 10: return Create_BamlProperty_ButtonBase_IsPressed();
                case 11: return Create_BamlProperty_ColumnDefinition_MaxWidth();
                case 12: return Create_BamlProperty_ColumnDefinition_MinWidth();
                case 13: return Create_BamlProperty_ColumnDefinition_Width();
                case 14: return Create_BamlProperty_ContentControl_Content();
                case 15: return Create_BamlProperty_ContentControl_ContentTemplate();
                case 16: return Create_BamlProperty_ContentControl_ContentTemplateSelector();
                case 17: return Create_BamlProperty_ContentControl_HasContent();
                case 18: return Create_BamlProperty_ContentElement_Focusable();
                case 19: return Create_BamlProperty_ContentPresenter_Content();
                case 20: return Create_BamlProperty_ContentPresenter_ContentSource();
                case 21: return Create_BamlProperty_ContentPresenter_ContentTemplate();
                case 22: return Create_BamlProperty_ContentPresenter_ContentTemplateSelector();
                case 23: return Create_BamlProperty_ContentPresenter_RecognizesAccessKey();
                case 24: return Create_BamlProperty_Control_Background();
                case 25: return Create_BamlProperty_Control_BorderBrush();
                case 26: return Create_BamlProperty_Control_BorderThickness();
                case 27: return Create_BamlProperty_Control_FontFamily();
                case 28: return Create_BamlProperty_Control_FontSize();
                case 29: return Create_BamlProperty_Control_FontStretch();
                case 30: return Create_BamlProperty_Control_FontStyle();
                case 31: return Create_BamlProperty_Control_FontWeight();
                case 32: return Create_BamlProperty_Control_Foreground();
                case 33: return Create_BamlProperty_Control_HorizontalContentAlignment();
                case 34: return Create_BamlProperty_Control_IsTabStop();
                case 35: return Create_BamlProperty_Control_Padding();
                case 36: return Create_BamlProperty_Control_TabIndex();
                case 37: return Create_BamlProperty_Control_Template();
                case 38: return Create_BamlProperty_Control_VerticalContentAlignment();
                case 39: return Create_BamlProperty_DockPanel_Dock();
                case 40: return Create_BamlProperty_DockPanel_LastChildFill();
                case 41: return Create_BamlProperty_DocumentViewerBase_Document();
                case 42: return Create_BamlProperty_DrawingGroup_Children();
                case 43: return Create_BamlProperty_FlowDocumentReader_Document();
                case 44: return Create_BamlProperty_FlowDocumentScrollViewer_Document();
                case 45: return Create_BamlProperty_FrameworkContentElement_Style();
                case 46: return Create_BamlProperty_FrameworkElement_FlowDirection();
                case 47: return Create_BamlProperty_FrameworkElement_Height();
                case 48: return Create_BamlProperty_FrameworkElement_HorizontalAlignment();
                case 49: return Create_BamlProperty_FrameworkElement_Margin();
                case 50: return Create_BamlProperty_FrameworkElement_MaxHeight();
                case 51: return Create_BamlProperty_FrameworkElement_MaxWidth();
                case 52: return Create_BamlProperty_FrameworkElement_MinHeight();
                case 53: return Create_BamlProperty_FrameworkElement_MinWidth();
                case 54: return Create_BamlProperty_FrameworkElement_Name();
                case 55: return Create_BamlProperty_FrameworkElement_Style();
                case 56: return Create_BamlProperty_FrameworkElement_VerticalAlignment();
                case 57: return Create_BamlProperty_FrameworkElement_Width();
                case 58: return Create_BamlProperty_GeneralTransformGroup_Children();
                case 59: return Create_BamlProperty_GeometryGroup_Children();
                case 60: return Create_BamlProperty_GradientBrush_GradientStops();
                case 61: return Create_BamlProperty_Grid_Column();
                case 62: return Create_BamlProperty_Grid_ColumnSpan();
                case 63: return Create_BamlProperty_Grid_Row();
                case 64: return Create_BamlProperty_Grid_RowSpan();
                case 65: return Create_BamlProperty_GridViewColumn_Header();
                case 66: return Create_BamlProperty_HeaderedContentControl_HasHeader();
                case 67: return Create_BamlProperty_HeaderedContentControl_Header();
                case 68: return Create_BamlProperty_HeaderedContentControl_HeaderTemplate();
                case 69: return Create_BamlProperty_HeaderedContentControl_HeaderTemplateSelector();
                case 70: return Create_BamlProperty_HeaderedItemsControl_HasHeader();
                case 71: return Create_BamlProperty_HeaderedItemsControl_Header();
                case 72: return Create_BamlProperty_HeaderedItemsControl_HeaderTemplate();
                case 73: return Create_BamlProperty_HeaderedItemsControl_HeaderTemplateSelector();
                case 74: return Create_BamlProperty_Hyperlink_NavigateUri();
                case 75: return Create_BamlProperty_Image_Source();
                case 76: return Create_BamlProperty_Image_Stretch();
                case 77: return Create_BamlProperty_ItemsControl_ItemContainerStyle();
                case 78: return Create_BamlProperty_ItemsControl_ItemContainerStyleSelector();
                case 79: return Create_BamlProperty_ItemsControl_ItemTemplate();
                case 80: return Create_BamlProperty_ItemsControl_ItemTemplateSelector();
                case 81: return Create_BamlProperty_ItemsControl_ItemsPanel();
                case 82: return Create_BamlProperty_ItemsControl_ItemsSource();
                case 83: return Create_BamlProperty_MaterialGroup_Children();
                case 84: return Create_BamlProperty_Model3DGroup_Children();
                case 85: return Create_BamlProperty_Page_Content();
                case 86: return Create_BamlProperty_Panel_Background();
                case 87: return Create_BamlProperty_Path_Data();
                case 88: return Create_BamlProperty_PathFigure_Segments();
                case 89: return Create_BamlProperty_PathGeometry_Figures();
                case 90: return Create_BamlProperty_Popup_Child();
                case 91: return Create_BamlProperty_Popup_IsOpen();
                case 92: return Create_BamlProperty_Popup_Placement();
                case 93: return Create_BamlProperty_Popup_PopupAnimation();
                case 94: return Create_BamlProperty_RowDefinition_Height();
                case 95: return Create_BamlProperty_RowDefinition_MaxHeight();
                case 96: return Create_BamlProperty_RowDefinition_MinHeight();
                case 97: return Create_BamlProperty_ScrollViewer_CanContentScroll();
                case 98: return Create_BamlProperty_ScrollViewer_HorizontalScrollBarVisibility();
                case 99: return Create_BamlProperty_ScrollViewer_VerticalScrollBarVisibility();
                case 100: return Create_BamlProperty_Shape_Fill();
                case 101: return Create_BamlProperty_Shape_Stroke();
                case 102: return Create_BamlProperty_Shape_StrokeThickness();
                case 103: return Create_BamlProperty_TextBlock_Background();
                case 104: return Create_BamlProperty_TextBlock_FontFamily();
                case 105: return Create_BamlProperty_TextBlock_FontSize();
                case 106: return Create_BamlProperty_TextBlock_FontStretch();
                case 107: return Create_BamlProperty_TextBlock_FontStyle();
                case 108: return Create_BamlProperty_TextBlock_FontWeight();
                case 109: return Create_BamlProperty_TextBlock_Foreground();
                case 110: return Create_BamlProperty_TextBlock_Text();
                case 111: return Create_BamlProperty_TextBlock_TextDecorations();
                case 112: return Create_BamlProperty_TextBlock_TextTrimming();
                case 113: return Create_BamlProperty_TextBlock_TextWrapping();
                case 114: return Create_BamlProperty_TextBox_Text();
                case 115: return Create_BamlProperty_TextElement_Background();
                case 116: return Create_BamlProperty_TextElement_FontFamily();
                case 117: return Create_BamlProperty_TextElement_FontSize();
                case 118: return Create_BamlProperty_TextElement_FontStretch();
                case 119: return Create_BamlProperty_TextElement_FontStyle();
                case 120: return Create_BamlProperty_TextElement_FontWeight();
                case 121: return Create_BamlProperty_TextElement_Foreground();
                case 122: return Create_BamlProperty_TimelineGroup_Children();
                case 123: return Create_BamlProperty_Track_IsDirectionReversed();
                case 124: return Create_BamlProperty_Track_Maximum();
                case 125: return Create_BamlProperty_Track_Minimum();
                case 126: return Create_BamlProperty_Track_Orientation();
                case 127: return Create_BamlProperty_Track_Value();
                case 128: return Create_BamlProperty_Track_ViewportSize();
                case 129: return Create_BamlProperty_Transform3DGroup_Children();
                case 130: return Create_BamlProperty_TransformGroup_Children();
                case 131: return Create_BamlProperty_UIElement_ClipToBounds();
                case 132: return Create_BamlProperty_UIElement_Focusable();
                case 133: return Create_BamlProperty_UIElement_IsEnabled();
                case 134: return Create_BamlProperty_UIElement_RenderTransform();
                case 135: return Create_BamlProperty_UIElement_Visibility();
                case 136: return Create_BamlProperty_Viewport3D_Children();
                case 138: return Create_BamlProperty_AdornedElementPlaceholder_Child();
                case 139: return Create_BamlProperty_AdornerDecorator_Child();
                case 140: return Create_BamlProperty_AnchoredBlock_Blocks();
                case 141: return Create_BamlProperty_ArrayExtension_Items();
                case 142: return Create_BamlProperty_BlockUIContainer_Child();
                case 143: return Create_BamlProperty_Bold_Inlines();
                case 144: return Create_BamlProperty_BooleanAnimationUsingKeyFrames_KeyFrames();
                case 145: return Create_BamlProperty_Border_Child();
                case 146: return Create_BamlProperty_BulletDecorator_Child();
                case 147: return Create_BamlProperty_Button_Content();
                case 148: return Create_BamlProperty_ButtonBase_Content();
                case 149: return Create_BamlProperty_ByteAnimationUsingKeyFrames_KeyFrames();
                case 150: return Create_BamlProperty_Canvas_Children();
                case 151: return Create_BamlProperty_CharAnimationUsingKeyFrames_KeyFrames();
                case 152: return Create_BamlProperty_CheckBox_Content();
                case 153: return Create_BamlProperty_ColorAnimationUsingKeyFrames_KeyFrames();
                case 154: return Create_BamlProperty_ComboBox_Items();
                case 155: return Create_BamlProperty_ComboBoxItem_Content();
                case 156: return Create_BamlProperty_ContextMenu_Items();
                case 157: return Create_BamlProperty_ControlTemplate_VisualTree();
                case 158: return Create_BamlProperty_DataTemplate_VisualTree();
                case 159: return Create_BamlProperty_DataTrigger_Setters();
                case 160: return Create_BamlProperty_DecimalAnimationUsingKeyFrames_KeyFrames();
                case 161: return Create_BamlProperty_Decorator_Child();
                case 162: return Create_BamlProperty_DockPanel_Children();
                case 163: return Create_BamlProperty_DocumentViewer_Document();
                case 164: return Create_BamlProperty_DoubleAnimationUsingKeyFrames_KeyFrames();
                case 165: return Create_BamlProperty_EventTrigger_Actions();
                case 166: return Create_BamlProperty_Expander_Content();
                case 167: return Create_BamlProperty_Figure_Blocks();
                case 168: return Create_BamlProperty_FixedDocument_Pages();
                case 169: return Create_BamlProperty_FixedDocumentSequence_References();
                case 170: return Create_BamlProperty_FixedPage_Children();
                case 171: return Create_BamlProperty_Floater_Blocks();
                case 172: return Create_BamlProperty_FlowDocument_Blocks();
                case 173: return Create_BamlProperty_FlowDocumentPageViewer_Document();
                case 174: return Create_BamlProperty_FrameworkTemplate_VisualTree();
                case 175: return Create_BamlProperty_Grid_Children();
                case 176: return Create_BamlProperty_GridView_Columns();
                case 177: return Create_BamlProperty_GridViewColumnHeader_Content();
                case 178: return Create_BamlProperty_GroupBox_Content();
                case 179: return Create_BamlProperty_GroupItem_Content();
                case 180: return Create_BamlProperty_HeaderedContentControl_Content();
                case 181: return Create_BamlProperty_HeaderedItemsControl_Items();
                case 182: return Create_BamlProperty_HierarchicalDataTemplate_VisualTree();
                case 183: return Create_BamlProperty_Hyperlink_Inlines();
                case 184: return Create_BamlProperty_InkCanvas_Children();
                case 185: return Create_BamlProperty_InkPresenter_Child();
                case 186: return Create_BamlProperty_InlineUIContainer_Child();
                case 187: return Create_BamlProperty_InputScopeName_NameValue();
                case 188: return Create_BamlProperty_Int16AnimationUsingKeyFrames_KeyFrames();
                case 189: return Create_BamlProperty_Int32AnimationUsingKeyFrames_KeyFrames();
                case 190: return Create_BamlProperty_Int64AnimationUsingKeyFrames_KeyFrames();
                case 191: return Create_BamlProperty_Italic_Inlines();
                case 192: return Create_BamlProperty_ItemsControl_Items();
                case 193: return Create_BamlProperty_ItemsPanelTemplate_VisualTree();
                case 194: return Create_BamlProperty_Label_Content();
                case 195: return Create_BamlProperty_LinearGradientBrush_GradientStops();
                case 196: return Create_BamlProperty_List_ListItems();
                case 197: return Create_BamlProperty_ListBox_Items();
                case 198: return Create_BamlProperty_ListBoxItem_Content();
                case 199: return Create_BamlProperty_ListItem_Blocks();
                case 200: return Create_BamlProperty_ListView_Items();
                case 201: return Create_BamlProperty_ListViewItem_Content();
                case 202: return Create_BamlProperty_MatrixAnimationUsingKeyFrames_KeyFrames();
                case 203: return Create_BamlProperty_Menu_Items();
                case 204: return Create_BamlProperty_MenuBase_Items();
                case 205: return Create_BamlProperty_MenuItem_Items();
                case 206: return Create_BamlProperty_ModelVisual3D_Children();
                case 207: return Create_BamlProperty_MultiBinding_Bindings();
                case 208: return Create_BamlProperty_MultiDataTrigger_Setters();
                case 209: return Create_BamlProperty_MultiTrigger_Setters();
                case 210: return Create_BamlProperty_ObjectAnimationUsingKeyFrames_KeyFrames();
                case 211: return Create_BamlProperty_PageContent_Child();
                case 212: return Create_BamlProperty_PageFunctionBase_Content();
                case 213: return Create_BamlProperty_Panel_Children();
                case 214: return Create_BamlProperty_Paragraph_Inlines();
                case 215: return Create_BamlProperty_ParallelTimeline_Children();
                case 216: return Create_BamlProperty_Point3DAnimationUsingKeyFrames_KeyFrames();
                case 217: return Create_BamlProperty_PointAnimationUsingKeyFrames_KeyFrames();
                case 218: return Create_BamlProperty_PriorityBinding_Bindings();
                case 219: return Create_BamlProperty_QuaternionAnimationUsingKeyFrames_KeyFrames();
                case 220: return Create_BamlProperty_RadialGradientBrush_GradientStops();
                case 221: return Create_BamlProperty_RadioButton_Content();
                case 222: return Create_BamlProperty_RectAnimationUsingKeyFrames_KeyFrames();
                case 223: return Create_BamlProperty_RepeatButton_Content();
                case 224: return Create_BamlProperty_RichTextBox_Document();
                case 225: return Create_BamlProperty_Rotation3DAnimationUsingKeyFrames_KeyFrames();
                case 226: return Create_BamlProperty_Run_Text();
                case 227: return Create_BamlProperty_ScrollViewer_Content();
                case 228: return Create_BamlProperty_Section_Blocks();
                case 229: return Create_BamlProperty_Selector_Items();
                case 230: return Create_BamlProperty_SingleAnimationUsingKeyFrames_KeyFrames();
                case 231: return Create_BamlProperty_SizeAnimationUsingKeyFrames_KeyFrames();
                case 232: return Create_BamlProperty_Span_Inlines();
                case 233: return Create_BamlProperty_StackPanel_Children();
                case 234: return Create_BamlProperty_StatusBar_Items();
                case 235: return Create_BamlProperty_StatusBarItem_Content();
                case 236: return Create_BamlProperty_Storyboard_Children();
                case 237: return Create_BamlProperty_StringAnimationUsingKeyFrames_KeyFrames();
                case 238: return Create_BamlProperty_Style_Setters();
                case 239: return Create_BamlProperty_TabControl_Items();
                case 240: return Create_BamlProperty_TabItem_Content();
                case 241: return Create_BamlProperty_TabPanel_Children();
                case 242: return Create_BamlProperty_Table_RowGroups();
                case 243: return Create_BamlProperty_TableCell_Blocks();
                case 244: return Create_BamlProperty_TableRow_Cells();
                case 245: return Create_BamlProperty_TableRowGroup_Rows();
                case 246: return Create_BamlProperty_TextBlock_Inlines();
                case 247: return Create_BamlProperty_ThicknessAnimationUsingKeyFrames_KeyFrames();
                case 248: return Create_BamlProperty_ToggleButton_Content();
                case 249: return Create_BamlProperty_ToolBar_Items();
                case 250: return Create_BamlProperty_ToolBarOverflowPanel_Children();
                case 251: return Create_BamlProperty_ToolBarPanel_Children();
                case 252: return Create_BamlProperty_ToolBarTray_ToolBars();
                case 253: return Create_BamlProperty_ToolTip_Content();
                case 254: return Create_BamlProperty_TreeView_Items();
                case 255: return Create_BamlProperty_TreeViewItem_Items();
                case 256: return Create_BamlProperty_Trigger_Setters();
                case 257: return Create_BamlProperty_Underline_Inlines();
                case 258: return Create_BamlProperty_UniformGrid_Children();
                case 259: return Create_BamlProperty_UserControl_Content();
                case 260: return Create_BamlProperty_Vector3DAnimationUsingKeyFrames_KeyFrames();
                case 261: return Create_BamlProperty_VectorAnimationUsingKeyFrames_KeyFrames();
                case 262: return Create_BamlProperty_Viewbox_Child();
                case 263: return Create_BamlProperty_Viewport3DVisual_Children();
                case 264: return Create_BamlProperty_VirtualizingPanel_Children();
                case 265: return Create_BamlProperty_VirtualizingStackPanel_Children();
                case 266: return Create_BamlProperty_Window_Content();
                case 267: return Create_BamlProperty_WrapPanel_Children();
                case 268: return Create_BamlProperty_XmlDataProvider_XmlSerializer();
                default:
                    throw new InvalidOperationException("Invalid BAML number");
            }
        }

        private uint GetTypeNameHashForPropeties(string typeName)
        {
            uint result = 0;
            for (int i = 1; i < 15 && i < typeName.Length; i++)
            {
                result = 101 * result + (uint)typeName[i];
            }
            return result;
        }

        internal WpfKnownMember CreateKnownMember(string type, string property)
        {
            uint hash = GetTypeNameHashForPropeties(type);
            switch (hash)
            {
                case 1632072630:
                    switch(property)
                    {
                        case "Text": return GetKnownBamlMember(-1);
                        default: return null;
                    }
                case 491630740:
                    switch(property)
                    {
                        case "Storyboard": return GetKnownBamlMember(-2);
                        case "Name": return Create_BamlProperty_BeginStoryboard_Name();
                        default: return null;
                    }
                case 381891668:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-3);
                        default: return null;
                    }
                case 3079254648:
                    switch(property)
                    {
                        case "Background": return GetKnownBamlMember(-4);
                        case "BorderBrush": return GetKnownBamlMember(-5);
                        case "BorderThickness": return GetKnownBamlMember(-6);
                        case "Child": return GetKnownBamlMember(-145);
                        default: return null;
                    }
                case 848944877:
                    switch(property)
                    {
                        case "Command": return GetKnownBamlMember(-7);
                        case "CommandParameter": return GetKnownBamlMember(-8);
                        case "CommandTarget": return GetKnownBamlMember(-9);
                        case "IsPressed": return GetKnownBamlMember(-10);
                        case "Content": return GetKnownBamlMember(-148);
                        case "ClickMode": return Create_BamlProperty_ButtonBase_ClickMode();
                        default: return null;
                    }
                case 175175278:
                    switch(property)
                    {
                        case "MaxWidth": return GetKnownBamlMember(-11);
                        case "MinWidth": return GetKnownBamlMember(-12);
                        case "Width": return GetKnownBamlMember(-13);
                        default: return null;
                    }
                case 4237892661:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-14);
                        case "ContentTemplate": return GetKnownBamlMember(-15);
                        case "ContentTemplateSelector": return GetKnownBamlMember(-16);
                        case "HasContent": return GetKnownBamlMember(-17);
                        default: return null;
                    }
                case 3154930786:
                    switch(property)
                    {
                        case "Focusable": return GetKnownBamlMember(-18);
                        default: return null;
                    }
                case 3536639290:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-19);
                        case "ContentSource": return GetKnownBamlMember(-20);
                        case "ContentTemplate": return GetKnownBamlMember(-21);
                        case "ContentTemplateSelector": return GetKnownBamlMember(-22);
                        case "RecognizesAccessKey": return GetKnownBamlMember(-23);
                        default: return null;
                    }
                case 1367449766:
                    switch(property)
                    {
                        case "Background": return GetKnownBamlMember(-24);
                        case "BorderBrush": return GetKnownBamlMember(-25);
                        case "BorderThickness": return GetKnownBamlMember(-26);
                        case "FontFamily": return GetKnownBamlMember(-27);
                        case "FontSize": return GetKnownBamlMember(-28);
                        case "FontStretch": return GetKnownBamlMember(-29);
                        case "FontStyle": return GetKnownBamlMember(-30);
                        case "FontWeight": return GetKnownBamlMember(-31);
                        case "Foreground": return GetKnownBamlMember(-32);
                        case "HorizontalContentAlignment": return GetKnownBamlMember(-33);
                        case "IsTabStop": return GetKnownBamlMember(-34);
                        case "Padding": return GetKnownBamlMember(-35);
                        case "TabIndex": return GetKnownBamlMember(-36);
                        case "Template": return GetKnownBamlMember(-37);
                        case "VerticalContentAlignment": return GetKnownBamlMember(-38);
                        default: return null;
                    }
                case 1236602933:
                    switch(property)
                    {
                        case "LastChildFill": return GetKnownBamlMember(-40);
                        case "Children": return GetKnownBamlMember(-162);
                        default: return null;
                    }
                case 1681553739:
                    switch(property)
                    {
                        case "Document": return GetKnownBamlMember(-41);
                        default: return null;
                    }
                case 1534050549:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-42);
                        default: return null;
                    }
                case 2545195941:
                    switch(property)
                    {
                        case "Document": return GetKnownBamlMember(-43);
                        default: return null;
                    }
                case 2545205957:
                    switch(property)
                    {
                        case "Document": return GetKnownBamlMember(-44);
                        default: return null;
                    }
                case 1543401471:
                    switch(property)
                    {
                        case "Style": return GetKnownBamlMember(-45);
                        case "Name": return Create_BamlProperty_FrameworkContentElement_Name();
                        case "Resources": return Create_BamlProperty_FrameworkContentElement_Resources();
                        default: return null;
                    }
                case 767240674:
                    switch(property)
                    {
                        case "FlowDirection": return GetKnownBamlMember(-46);
                        case "Height": return GetKnownBamlMember(-47);
                        case "HorizontalAlignment": return GetKnownBamlMember(-48);
                        case "Margin": return GetKnownBamlMember(-49);
                        case "MaxHeight": return GetKnownBamlMember(-50);
                        case "MaxWidth": return GetKnownBamlMember(-51);
                        case "MinHeight": return GetKnownBamlMember(-52);
                        case "MinWidth": return GetKnownBamlMember(-53);
                        case "Name": return GetKnownBamlMember(-54);
                        case "Style": return GetKnownBamlMember(-55);
                        case "VerticalAlignment": return GetKnownBamlMember(-56);
                        case "Width": return GetKnownBamlMember(-57);
                        case "Resources": return Create_BamlProperty_FrameworkElement_Resources();
                        case "Triggers": return Create_BamlProperty_FrameworkElement_Triggers();
                        default: return null;
                    }
                case 2497569086:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-58);
                        default: return null;
                    }
                case 2762527090:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-59);
                        default: return null;
                    }
                case 3696127683:
                    switch(property)
                    {
                        case "GradientStops": return GetKnownBamlMember(-60);
                        case "MappingMode": return Create_BamlProperty_GradientBrush_MappingMode();
                        default: return null;
                    }
                case 1173619:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-175);
                        case "ColumnDefinitions": return Create_BamlProperty_Grid_ColumnDefinitions();
                        case "RowDefinitions": return Create_BamlProperty_Grid_RowDefinitions();
                        default: return null;
                    }
                case 812041272:
                    switch(property)
                    {
                        case "Header": return GetKnownBamlMember(-65);
                        default: return null;
                    }
                case 4042892829:
                    switch(property)
                    {
                        case "HasHeader": return GetKnownBamlMember(-66);
                        case "Header": return GetKnownBamlMember(-67);
                        case "HeaderTemplate": return GetKnownBamlMember(-68);
                        case "HeaderTemplateSelector": return GetKnownBamlMember(-69);
                        case "Content": return GetKnownBamlMember(-180);
                        default: return null;
                    }
                case 3794574170:
                    switch(property)
                    {
                        case "HasHeader": return GetKnownBamlMember(-70);
                        case "Header": return GetKnownBamlMember(-71);
                        case "HeaderTemplate": return GetKnownBamlMember(-72);
                        case "HeaderTemplateSelector": return GetKnownBamlMember(-73);
                        case "Items": return GetKnownBamlMember(-181);
                        default: return null;
                    }
                case 1732790398:
                    switch(property)
                    {
                        case "NavigateUri": return GetKnownBamlMember(-74);
                        case "Inlines": return GetKnownBamlMember(-183);
                        default: return null;
                    }
                case 113302810:
                    switch(property)
                    {
                        case "Source": return GetKnownBamlMember(-75);
                        case "Stretch": return GetKnownBamlMember(-76);
                        default: return null;
                    }
                case 2414917938:
                    switch(property)
                    {
                        case "ItemContainerStyle": return GetKnownBamlMember(-77);
                        case "ItemContainerStyleSelector": return GetKnownBamlMember(-78);
                        case "ItemTemplate": return GetKnownBamlMember(-79);
                        case "ItemTemplateSelector": return GetKnownBamlMember(-80);
                        case "ItemsPanel": return GetKnownBamlMember(-81);
                        case "ItemsSource": return GetKnownBamlMember(-82);
                        case "Items": return GetKnownBamlMember(-192);
                        default: return null;
                    }
                case 1343785127:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-83);
                        default: return null;
                    }
                case 4100099324:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-84);
                        default: return null;
                    }
                case 1000001:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-85);
                        default: return null;
                    }
                case 101071616:
                    switch(property)
                    {
                        case "Background": return GetKnownBamlMember(-86);
                        case "Children": return GetKnownBamlMember(-213);
                        case "IsItemsHost": return Create_BamlProperty_Panel_IsItemsHost();
                        default: return null;
                    }
                case 1001317:
                    switch(property)
                    {
                        case "Data": return GetKnownBamlMember(-87);
                        default: return null;
                    }
                case 649104411:
                    switch(property)
                    {
                        case "Segments": return GetKnownBamlMember(-88);
                        case "IsClosed": return Create_BamlProperty_PathFigure_IsClosed();
                        case "IsFilled": return Create_BamlProperty_PathFigure_IsFilled();
                        default: return null;
                    }
                case 213893085:
                    switch(property)
                    {
                        case "Figures": return GetKnownBamlMember(-89);
                        default: return null;
                    }
                case 115517852:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-90);
                        case "IsOpen": return GetKnownBamlMember(-91);
                        case "Placement": return GetKnownBamlMember(-92);
                        case "PopupAnimation": return GetKnownBamlMember(-93);
                        default: return null;
                    }
                case 2231796391:
                    switch(property)
                    {
                        case "Height": return GetKnownBamlMember(-94);
                        case "MaxHeight": return GetKnownBamlMember(-95);
                        case "MinHeight": return GetKnownBamlMember(-96);
                        default: return null;
                    }
                case 1540591646:
                    switch(property)
                    {
                        case "CanContentScroll": return GetKnownBamlMember(-97);
                        case "HorizontalScrollBarVisibility": return GetKnownBamlMember(-98);
                        case "VerticalScrollBarVisibility": return GetKnownBamlMember(-99);
                        case "Content": return GetKnownBamlMember(-227);
                        default: return null;
                    }
                case 108152214:
                    switch(property)
                    {
                        case "Fill": return GetKnownBamlMember(-100);
                        case "Stroke": return GetKnownBamlMember(-101);
                        case "StrokeThickness": return GetKnownBamlMember(-102);
                        case "StrokeLineJoin": return Create_BamlProperty_Shape_StrokeLineJoin();
                        case "StrokeStartLineCap": return Create_BamlProperty_Shape_StrokeStartLineCap();
                        case "StrokeEndLineCap": return Create_BamlProperty_Shape_StrokeEndLineCap();
                        case "Stretch": return Create_BamlProperty_Shape_Stretch();
                        case "StrokeMiterLimit": return Create_BamlProperty_Shape_StrokeMiterLimit();
                        default: return null;
                    }
                case 1089745292:
                    switch(property)
                    {
                        case "Background": return GetKnownBamlMember(-103);
                        case "FontFamily": return GetKnownBamlMember(-104);
                        case "FontSize": return GetKnownBamlMember(-105);
                        case "FontStretch": return GetKnownBamlMember(-106);
                        case "FontStyle": return GetKnownBamlMember(-107);
                        case "FontWeight": return GetKnownBamlMember(-108);
                        case "Foreground": return GetKnownBamlMember(-109);
                        case "Text": return GetKnownBamlMember(-110);
                        case "TextDecorations": return GetKnownBamlMember(-111);
                        case "TextTrimming": return GetKnownBamlMember(-112);
                        case "TextWrapping": return GetKnownBamlMember(-113);
                        case "Inlines": return GetKnownBamlMember(-246);
                        case "TextAlignment": return Create_BamlProperty_TextBlock_TextAlignment();
                        default: return null;
                    }
                case 385774234:
                    switch(property)
                    {
                        case "Text": return GetKnownBamlMember(-114);
                        case "TextWrapping": return Create_BamlProperty_TextBox_TextWrapping();
                        case "TextAlignment": return Create_BamlProperty_TextBox_TextAlignment();
                        default: return null;
                    }
                case 2075696131:
                    switch(property)
                    {
                        case "Background": return GetKnownBamlMember(-115);
                        case "FontFamily": return GetKnownBamlMember(-116);
                        case "FontSize": return GetKnownBamlMember(-117);
                        case "FontStretch": return GetKnownBamlMember(-118);
                        case "FontStyle": return GetKnownBamlMember(-119);
                        case "FontWeight": return GetKnownBamlMember(-120);
                        case "Foreground": return GetKnownBamlMember(-121);
                        default: return null;
                    }
                case 3627706972:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-122);
                        default: return null;
                    }
                case 118453917:
                    switch(property)
                    {
                        case "IsDirectionReversed": return GetKnownBamlMember(-123);
                        case "Maximum": return GetKnownBamlMember(-124);
                        case "Minimum": return GetKnownBamlMember(-125);
                        case "Orientation": return GetKnownBamlMember(-126);
                        case "Value": return GetKnownBamlMember(-127);
                        case "ViewportSize": return GetKnownBamlMember(-128);
                        case "Thumb": return Create_BamlProperty_Track_Thumb();
                        case "IncreaseRepeatButton": return Create_BamlProperty_Track_IncreaseRepeatButton();
                        case "DecreaseRepeatButton": return Create_BamlProperty_Track_DecreaseRepeatButton();
                        default: return null;
                    }
                case 966650152:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-129);
                        default: return null;
                    }
                case 1543239001:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-130);
                        default: return null;
                    }
                case 4081990243:
                    switch(property)
                    {
                        case "ClipToBounds": return GetKnownBamlMember(-131);
                        case "Focusable": return GetKnownBamlMember(-132);
                        case "IsEnabled": return GetKnownBamlMember(-133);
                        case "RenderTransform": return GetKnownBamlMember(-134);
                        case "Visibility": return GetKnownBamlMember(-135);
                        case "Uid": return Create_BamlProperty_UIElement_Uid();
                        case "RenderTransformOrigin": return Create_BamlProperty_UIElement_RenderTransformOrigin();
                        case "SnapsToDevicePixels": return Create_BamlProperty_UIElement_SnapsToDevicePixels();
                        case "CommandBindings": return Create_BamlProperty_UIElement_CommandBindings();
                        case "InputBindings": return Create_BamlProperty_UIElement_InputBindings();
                        case "AllowDrop": return Create_BamlProperty_UIElement_AllowDrop();
                        default: return null;
                    }
                case 1489718377:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-136);
                        default: return null;
                    }
                case 2369223502:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-138);
                        default: return null;
                    }
                case 1535690395:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-139);
                        default: return null;
                    }
                case 3699188754:
                    switch(property)
                    {
                        case "Blocks": return GetKnownBamlMember(-140);
                        default: return null;
                    }
                case 1752642139:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-141);
                        default: return null;
                    }
                case 826277256:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-142);
                        default: return null;
                    }
                case 1143319:
                    switch(property)
                    {
                        case "Inlines": return GetKnownBamlMember(-143);
                        default: return null;
                    }
                case 1583456952:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-144);
                        default: return null;
                    }
                case 109056765:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-146);
                        case "Bullet": return Create_BamlProperty_BulletDecorator_Bullet();
                        default: return null;
                    }
                case 3705841878:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-147);
                        default: return null;
                    }
                case 2361592662:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-149);
                        default: return null;
                    }
                case 1618471045:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-150);
                        default: return null;
                    }
                case 2420033511:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-151);
                        default: return null;
                    }
                case 2742486520:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-152);
                        default: return null;
                    }
                case 755422265:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-153);
                        default: return null;
                    }
                case 1171637538:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-154);
                        default: return null;
                    }
                case 2979510881:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-155);
                        default: return null;
                    }
                case 3042134663:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-156);
                        default: return null;
                    }
                case 3159584246:
                    switch(property)
                    {
                        case "VisualTree": return GetKnownBamlMember(-157);
                        case "Triggers": return Create_BamlProperty_ControlTemplate_Triggers();
                        case "TargetType": return Create_BamlProperty_ControlTemplate_TargetType();
                        default: return null;
                    }
                case 1376032174:
                    switch(property)
                    {
                        case "VisualTree": return GetKnownBamlMember(-158);
                        case "Triggers": return Create_BamlProperty_DataTemplate_Triggers();
                        case "DataTemplateKey": return Create_BamlProperty_DataTemplate_DataTemplateKey();
                        case "DataType": return Create_BamlProperty_DataTemplate_DataType();
                        default: return null;
                    }
                case 1374402354:
                    switch(property)
                    {
                        case "Setters": return GetKnownBamlMember(-159);
                        case "Value": return Create_BamlProperty_DataTrigger_Value();
                        case "Binding": return Create_BamlProperty_DataTrigger_Binding();
                        default: return null;
                    }
                case 2615247465:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-160);
                        default: return null;
                    }
                case 4019572119:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-161);
                        default: return null;
                    }
                case 441893333:
                    switch(property)
                    {
                        case "Document": return GetKnownBamlMember(-163);
                        default: return null;
                    }
                case 3239315111:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-164);
                        default: return null;
                    }
                case 4284496765:
                    switch(property)
                    {
                        case "Actions": return GetKnownBamlMember(-165);
                        case "RoutedEvent": return Create_BamlProperty_EventTrigger_RoutedEvent();
                        case "SourceName": return Create_BamlProperty_EventTrigger_SourceName();
                        default: return null;
                    }
                case 4206512190:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-166);
                        default: return null;
                    }
                case 2443733648:
                    switch(property)
                    {
                        case "Blocks": return GetKnownBamlMember(-167);
                        default: return null;
                    }
                case 1831113161:
                    switch(property)
                    {
                        case "Pages": return GetKnownBamlMember(-168);
                        default: return null;
                    }
                case 372593541:
                    switch(property)
                    {
                        case "References": return GetKnownBamlMember(-169);
                        default: return null;
                    }
                case 3222460427:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-170);
                        default: return null;
                    }
                case 4281390711:
                    switch(property)
                    {
                        case "Blocks": return GetKnownBamlMember(-171);
                        default: return null;
                    }
                case 1742124221:
                    switch(property)
                    {
                        case "Blocks": return GetKnownBamlMember(-172);
                        default: return null;
                    }
                case 2545175141:
                    switch(property)
                    {
                        case "Document": return GetKnownBamlMember(-173);
                        default: return null;
                    }
                case 3079776431:
                    switch(property)
                    {
                        case "VisualTree": return GetKnownBamlMember(-174);
                        case "Template": return Create_BamlProperty_FrameworkTemplate_Template();
                        case "Resources": return Create_BamlProperty_FrameworkTemplate_Resources();
                        default: return null;
                    }
                case 4253354066:
                    switch(property)
                    {
                        case "Columns": return GetKnownBamlMember(-176);
                        default: return null;
                    }
                case 411789920:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-177);
                        default: return null;
                    }
                case 389898151:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-178);
                        default: return null;
                    }
                case 732268889:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-179);
                        default: return null;
                    }
                case 1663174964:
                    switch(property)
                    {
                        case "VisualTree": return GetKnownBamlMember(-182);
                        default: return null;
                    }
                case 1971172509:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-184);
                        default: return null;
                    }
                case 2495415765:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-185);
                        default: return null;
                    }
                case 2232234900:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-186);
                        default: return null;
                    }
                case 3737794794:
                    switch(property)
                    {
                        case "NameValue": return GetKnownBamlMember(-187);
                        default: return null;
                    }
                case 486209962:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-188);
                        default: return null;
                    }
                case 1197649600:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-189);
                        default: return null;
                    }
                case 276220969:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-190);
                        default: return null;
                    }
                case 3582123533:
                    switch(property)
                    {
                        case "Inlines": return GetKnownBamlMember(-191);
                        default: return null;
                    }
                case 374054883:
                    switch(property)
                    {
                        case "VisualTree": return GetKnownBamlMember(-193);
                        default: return null;
                    }
                case 100949204:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-194);
                        default: return null;
                    }
                case 2575003659:
                    switch(property)
                    {
                        case "GradientStops": return GetKnownBamlMember(-195);
                        case "StartPoint": return Create_BamlProperty_LinearGradientBrush_StartPoint();
                        case "EndPoint": return Create_BamlProperty_LinearGradientBrush_EndPoint();
                        default: return null;
                    }
                case 1082836:
                    switch(property)
                    {
                        case "ListItems": return GetKnownBamlMember(-196);
                        default: return null;
                    }
                case 3251168569:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-197);
                        default: return null;
                    }
                case 2579567368:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-198);
                        default: return null;
                    }
                case 1957772275:
                    switch(property)
                    {
                        case "Blocks": return GetKnownBamlMember(-199);
                        default: return null;
                    }
                case 1971053987:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-200);
                        default: return null;
                    }
                case 1169860818:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-201);
                        default: return null;
                    }
                case 3589500084:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-202);
                        default: return null;
                    }
                case 1041528:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-203);
                        default: return null;
                    }
                case 2685586543:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-204);
                        default: return null;
                    }
                case 2692991063:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-205);
                        case "Role": return Create_BamlProperty_MenuItem_Role();
                        case "IsChecked": return Create_BamlProperty_MenuItem_IsChecked();
                        default: return null;
                    }
                case 3203967083:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-206);
                        default: return null;
                    }
                case 3251291345:
                    switch(property)
                    {
                        case "Bindings": return GetKnownBamlMember(-207);
                        case "Converter": return Create_BamlProperty_MultiBinding_Converter();
                        case "ConverterParameter": return Create_BamlProperty_MultiBinding_ConverterParameter();
                        default: return null;
                    }
                case 141114174:
                    switch(property)
                    {
                        case "Setters": return GetKnownBamlMember(-208);
                        case "Conditions": return Create_BamlProperty_MultiDataTrigger_Conditions();
                        default: return null;
                    }
                case 1888893854:
                    switch(property)
                    {
                        case "Setters": return GetKnownBamlMember(-209);
                        case "Conditions": return Create_BamlProperty_MultiTrigger_Conditions();
                        default: return null;
                    }
                case 489623484:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-210);
                        default: return null;
                    }
                case 3329578860:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-211);
                        default: return null;
                    }
                case 1539399457:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-212);
                        default: return null;
                    }
                case 1481865686:
                    switch(property)
                    {
                        case "Inlines": return GetKnownBamlMember(-214);
                        default: return null;
                    }
                case 3221949491:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-215);
                        default: return null;
                    }
                case 3250492243:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-216);
                        default: return null;
                    }
                case 4060568379:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-217);
                        default: return null;
                    }
                case 1940998317:
                    switch(property)
                    {
                        case "Bindings": return GetKnownBamlMember(-218);
                        default: return null;
                    }
                case 1366062463:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-219);
                        default: return null;
                    }
                case 3215452047:
                    switch(property)
                    {
                        case "GradientStops": return GetKnownBamlMember(-220);
                        default: return null;
                    }
                case 1262679173:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-221);
                        default: return null;
                    }
                case 4013926948:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-222);
                        default: return null;
                    }
                case 3359941107:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-223);
                        default: return null;
                    }
                case 4130373030:
                    switch(property)
                    {
                        case "Document": return GetKnownBamlMember(-224);
                        default: return null;
                    }
                case 1536792507:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-225);
                        default: return null;
                    }
                case 11927:
                    switch(property)
                    {
                        case "Text": return GetKnownBamlMember(-226);
                        default: return null;
                    }
                case 2495870938:
                    switch(property)
                    {
                        case "Blocks": return GetKnownBamlMember(-228);
                        default: return null;
                    }
                case 1509448966:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-229);
                        default: return null;
                    }
                case 3423765539:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-230);
                        default: return null;
                    }
                case 1362944236:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-231);
                        default: return null;
                    }
                case 1152419:
                    switch(property)
                    {
                        case "Inlines": return GetKnownBamlMember(-232);
                        default: return null;
                    }
                case 2127983347:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-233);
                        case "Orientation": return Create_BamlProperty_StackPanel_Orientation();
                        default: return null;
                    }
                case 3803038822:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-234);
                        default: return null;
                    }
                case 2195627365:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-235);
                        default: return null;
                    }
                case 3693620786:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-236);
                        default: return null;
                    }
                case 2579011428:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-237);
                        default: return null;
                    }
                case 120760246:
                    switch(property)
                    {
                        case "Setters": return GetKnownBamlMember(-238);
                        case "TargetType": return Create_BamlProperty_Style_TargetType();
                        case "Triggers": return Create_BamlProperty_Style_Triggers();
                        case "BasedOn": return Create_BamlProperty_Style_BasedOn();
                        case "Resources": return Create_BamlProperty_Style_Resources();
                        default: return null;
                    }
                case 671959932:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-239);
                        default: return null;
                    }
                case 3256889750:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-240);
                        default: return null;
                    }
                case 3237288451:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-241);
                        default: return null;
                    }
                case 100949904:
                    switch(property)
                    {
                        case "RowGroups": return GetKnownBamlMember(-242);
                        default: return null;
                    }
                case 3145595724:
                    switch(property)
                    {
                        case "Blocks": return GetKnownBamlMember(-243);
                        default: return null;
                    }
                case 1859848980:
                    switch(property)
                    {
                        case "Cells": return GetKnownBamlMember(-244);
                        default: return null;
                    }
                case 3051751957:
                    switch(property)
                    {
                        case "Rows": return GetKnownBamlMember(-245);
                        default: return null;
                    }
                case 2134797854:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-247);
                        default: return null;
                    }
                case 1516882570:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-248);
                        default: return null;
                    }
                case 1462776703:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-249);
                        case "Orientation": return Create_BamlProperty_ToolBar_Orientation();
                        default: return null;
                    }
                case 4085468031:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-250);
                        default: return null;
                    }
                case 1646651323:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-251);
                        default: return null;
                    }
                case 1891671667:
                    switch(property)
                    {
                        case "ToolBars": return GetKnownBamlMember(-252);
                        default: return null;
                    }
                case 1462961127:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-253);
                        default: return null;
                    }
                case 971718127:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-254);
                        default: return null;
                    }
                case 908592222:
                    switch(property)
                    {
                        case "Items": return GetKnownBamlMember(-255);
                        default: return null;
                    }
                case 2299171064:
                    switch(property)
                    {
                        case "Setters": return GetKnownBamlMember(-256);
                        case "Value": return Create_BamlProperty_Trigger_Value();
                        case "SourceName": return Create_BamlProperty_Trigger_SourceName();
                        case "Property": return Create_BamlProperty_Trigger_Property();
                        default: return null;
                    }
                case 4251506749:
                    switch(property)
                    {
                        case "Inlines": return GetKnownBamlMember(-257);
                        default: return null;
                    }
                case 3726396217:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-258);
                        default: return null;
                    }
                case 4049813583:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-259);
                        default: return null;
                    }
                case 2006016895:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-260);
                        default: return null;
                    }
                case 1631317593:
                    switch(property)
                    {
                        case "KeyFrames": return GetKnownBamlMember(-261);
                        default: return null;
                    }
                case 1797740290:
                    switch(property)
                    {
                        case "Child": return GetKnownBamlMember(-262);
                        default: return null;
                    }
                case 282171645:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-263);
                        default: return null;
                    }
                case 1133493129:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-264);
                        default: return null;
                    }
                case 1133525638:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-265);
                        default: return null;
                    }
                case 2450772053:
                    switch(property)
                    {
                        case "Content": return GetKnownBamlMember(-266);
                        case "ResizeMode": return Create_BamlProperty_Window_ResizeMode();
                        case "WindowState": return Create_BamlProperty_Window_WindowState();
                        case "Title": return Create_BamlProperty_Window_Title();
                        case "AllowsTransparency": return Create_BamlProperty_Window_AllowsTransparency();
                        default: return null;
                    }
                case 15810163:
                    switch(property)
                    {
                        case "Children": return GetKnownBamlMember(-267);
                        default: return null;
                    }
                case 3607421190:
                    switch(property)
                    {
                        case "XmlSerializer": return GetKnownBamlMember(-268);
                        case "XPath": return Create_BamlProperty_XmlDataProvider_XPath();
                        default: return null;
                    }
                case 2040874456:
                    switch(property)
                    {
                        case "Value": return Create_BamlProperty_Setter_Value();
                        case "TargetName": return Create_BamlProperty_Setter_TargetName();
                        case "Property": return Create_BamlProperty_Setter_Property();
                        default: return null;
                    }
                case 2714779469:
                    switch(property)
                    {
                        case "Path": return Create_BamlProperty_Binding_Path();
                        case "Converter": return Create_BamlProperty_Binding_Converter();
                        case "Source": return Create_BamlProperty_Binding_Source();
                        case "RelativeSource": return Create_BamlProperty_Binding_RelativeSource();
                        case "Mode": return Create_BamlProperty_Binding_Mode();
                        case "ElementName": return Create_BamlProperty_Binding_ElementName();
                        case "UpdateSourceTrigger": return Create_BamlProperty_Binding_UpdateSourceTrigger();
                        case "XPath": return Create_BamlProperty_Binding_XPath();
                        case "ConverterParameter": return Create_BamlProperty_Binding_ConverterParameter();
                        default: return null;
                    }
                case 2189110588:
                    switch(property)
                    {
                        case "ResourceId": return Create_BamlProperty_ComponentResourceKey_ResourceId();
                        case "TypeInTargetAssembly": return Create_BamlProperty_ComponentResourceKey_TypeInTargetAssembly();
                        default: return null;
                    }
                case 1796721919:
                    switch(property)
                    {
                        case "BeginTime": return Create_BamlProperty_Timeline_BeginTime();
                        default: return null;
                    }
                case 4254925692:
                    switch(property)
                    {
                        case "DeferrableContent": return Create_BamlProperty_ResourceDictionary_DeferrableContent();
                        case "Source": return Create_BamlProperty_ResourceDictionary_Source();
                        case "MergedDictionaries": return Create_BamlProperty_ResourceDictionary_MergedDictionaries();
                        default: return null;
                    }
                case 1147347271:
                    switch(property)
                    {
                        case "AncestorType": return Create_BamlProperty_RelativeSource_AncestorType();
                        default: return null;
                    }
                case 790939867:
                    switch(property)
                    {
                        case "Resources": return Create_BamlProperty_Application_Resources();
                        default: return null;
                    }
                case 2699530403:
                    switch(property)
                    {
                        case "Command": return Create_BamlProperty_CommandBinding_Command();
                        default: return null;
                    }
                case 2006957996:
                    switch(property)
                    {
                        case "Property": return Create_BamlProperty_Condition_Property();
                        case "Value": return Create_BamlProperty_Condition_Value();
                        case "Binding": return Create_BamlProperty_Condition_Binding();
                        default: return null;
                    }
                case 137005044:
                    switch(property)
                    {
                        case "FallbackValue": return Create_BamlProperty_BindingBase_FallbackValue();
                        default: return null;
                    }
                case 350834986:
                    switch(property)
                    {
                        case "TileMode": return Create_BamlProperty_TileBrush_TileMode();
                        case "ViewboxUnits": return Create_BamlProperty_TileBrush_ViewboxUnits();
                        case "ViewportUnits": return Create_BamlProperty_TileBrush_ViewportUnits();
                        default: return null;
                    }
                case 2108852657:
                    switch(property)
                    {
                        case "Pen": return Create_BamlProperty_GeometryDrawing_Pen();
                        default: return null;
                    }
                case 3750147462:
                    switch(property)
                    {
                        case "Command": return Create_BamlProperty_InputBinding_Command();
                        default: return null;
                    }
                case 4223882185:
                    switch(property)
                    {
                        case "Gesture": return Create_BamlProperty_KeyBinding_Gesture();
                        case "Key": return Create_BamlProperty_KeyBinding_Key();
                        default: return null;
                    }
                case 3936355701:
                    switch(property)
                    {
                        case "Orientation": return Create_BamlProperty_ScrollBar_Orientation();
                        default: return null;
                    }
                case 837389320:
                    switch(property)
                    {
                        case "SharedSizeGroup": return Create_BamlProperty_DefinitionBase_SharedSizeGroup();
                        default: return null;
                    }
                case 112414925:
                    switch(property)
                    {
                        case "TextAlignment": return Create_BamlProperty_Block_TextAlignment();
                        default: return null;
                    }
                case 10311:
                    switch(property)
                    {
                        case "LineJoin": return Create_BamlProperty_Pen_LineJoin();
                        default: return null;
                    }
                case 2246554763:
                    switch(property)
                    {
                        case "Color": return Create_BamlProperty_SolidColorBrush_Color();
                        default: return null;
                    }
                case 118659550:
                    switch(property)
                    {
                        case "Opacity": return Create_BamlProperty_Brush_Opacity();
                        default: return null;
                    }
                case 3484345457:
                    switch(property)
                    {
                        case "AcceptsTab": return Create_BamlProperty_TextBoxBase_AcceptsTab();
                        case "VerticalScrollBarVisibility": return Create_BamlProperty_TextBoxBase_VerticalScrollBarVisibility();
                        case "HorizontalScrollBarVisibility": return Create_BamlProperty_TextBoxBase_HorizontalScrollBarVisibility();
                        default: return null;
                    }
                case 2067968796:
                    switch(property)
                    {
                        case "IsStroked": return Create_BamlProperty_PathSegment_IsStroked();
                        default: return null;
                    }
                case 118454921:
                    switch(property)
                    {
                        case "JournalOwnership": return Create_BamlProperty_Frame_JournalOwnership();
                        case "NavigationUIVisibility": return Create_BamlProperty_Frame_NavigationUIVisibility();
                        default: return null;
                    }
                case 4272319926:
                    switch(property)
                    {
                        case "ObjectType": return Create_BamlProperty_ObjectDataProvider_ObjectType();
                        default: return null;
                    }
                default: return null;
            }
        }

        internal WpfKnownMember CreateKnownAttachableMember(string type, string property)
        {
            uint hash = GetTypeNameHashForPropeties(type);
            switch (hash)
            {
                case 1236602933:
                    switch(property)
                    {
                        case "Dock": return GetKnownBamlMember(-39);
                        default: return null;
                    }
                case 1173619:
                    switch(property)
                    {
                        case "Column": return GetKnownBamlMember(-61);
                        case "ColumnSpan": return GetKnownBamlMember(-62);
                        case "Row": return GetKnownBamlMember(-63);
                        case "RowSpan": return GetKnownBamlMember(-64);
                        default: return null;
                    }
                case 1618471045:
                    switch(property)
                    {
                        case "Top": return Create_BamlProperty_Canvas_Top();
                        case "Left": return Create_BamlProperty_Canvas_Left();
                        case "Bottom": return Create_BamlProperty_Canvas_Bottom();
                        case "Right": return Create_BamlProperty_Canvas_Right();
                        default: return null;
                    }
                case 1509448966:
                    switch(property)
                    {
                        case "IsSelected": return Create_BamlProperty_Selector_IsSelected();
                        default: return null;
                    }
                case 3693620786:
                    switch(property)
                    {
                        case "TargetName": return Create_BamlProperty_Storyboard_TargetName();
                        case "TargetProperty": return Create_BamlProperty_Storyboard_TargetProperty();
                        default: return null;
                    }
                case 1133493129:
                    switch(property)
                    {
                        case "IsVirtualizing": return Create_BamlProperty_VirtualizingPanel_IsVirtualizing();
                        default: return null;
                    }
                case 3749867153:
                    switch(property)
                    {
                        case "NameScope": return Create_BamlProperty_NameScope_NameScope();
                        default: return null;
                    }
                case 378630271:
                    switch(property)
                    {
                        case "JournalEntryPosition": return Create_BamlProperty_JournalEntryUnifiedViewConverter_JournalEntryPosition();
                        default: return null;
                    }
                case 249275044:
                    switch(property)
                    {
                        case "DirectionalNavigation": return Create_BamlProperty_KeyboardNavigation_DirectionalNavigation();
                        case "TabNavigation": return Create_BamlProperty_KeyboardNavigation_TabNavigation();
                        default: return null;
                    }
                case 3951806740:
                    switch(property)
                    {
                        case "ToolTip": return Create_BamlProperty_ToolTipService_ToolTip();
                        default: return null;
                    }
                default: return null;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_AccessText_Text()
        {
            Type type = typeof(System.Windows.Controls.AccessText);
            DependencyProperty  dp = System.Windows.Controls.AccessText.TextProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.AccessText)), // DeclaringType
                            "Text", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BeginStoryboard_Storyboard()
        {
            Type type = typeof(System.Windows.Media.Animation.BeginStoryboard);
            DependencyProperty  dp = System.Windows.Media.Animation.BeginStoryboard.StoryboardProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.BeginStoryboard)), // DeclaringType
                            "Storyboard", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BitmapEffectGroup_Children()
        {
            Type type = typeof(System.Windows.Media.Effects.BitmapEffectGroup);
            DependencyProperty  dp = System.Windows.Media.Effects.BitmapEffectGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Effects.BitmapEffectGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Border_Background()
        {
            Type type = typeof(System.Windows.Controls.Border);
            DependencyProperty  dp = System.Windows.Controls.Border.BackgroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Border)), // DeclaringType
                            "Background", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Border_BorderBrush()
        {
            Type type = typeof(System.Windows.Controls.Border);
            DependencyProperty  dp = System.Windows.Controls.Border.BorderBrushProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Border)), // DeclaringType
                            "BorderBrush", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Border_BorderThickness()
        {
            Type type = typeof(System.Windows.Controls.Border);
            DependencyProperty  dp = System.Windows.Controls.Border.BorderThicknessProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Border)), // DeclaringType
                            "BorderThickness", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.ThicknessConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ButtonBase_Command()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ButtonBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ButtonBase.CommandProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ButtonBase)), // DeclaringType
                            "Command", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.CommandConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ButtonBase_CommandParameter()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ButtonBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ButtonBase.CommandParameterProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ButtonBase)), // DeclaringType
                            "CommandParameter", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ButtonBase_CommandTarget()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ButtonBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ButtonBase.CommandTargetProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ButtonBase)), // DeclaringType
                            "CommandTarget", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ButtonBase_IsPressed()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ButtonBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ButtonBase)), // DeclaringType
                            "IsPressed", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ColumnDefinition_MaxWidth()
        {
            Type type = typeof(System.Windows.Controls.ColumnDefinition);
            DependencyProperty  dp = System.Windows.Controls.ColumnDefinition.MaxWidthProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ColumnDefinition)), // DeclaringType
                            "MaxWidth", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ColumnDefinition_MinWidth()
        {
            Type type = typeof(System.Windows.Controls.ColumnDefinition);
            DependencyProperty  dp = System.Windows.Controls.ColumnDefinition.MinWidthProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ColumnDefinition)), // DeclaringType
                            "MinWidth", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ColumnDefinition_Width()
        {
            Type type = typeof(System.Windows.Controls.ColumnDefinition);
            DependencyProperty  dp = System.Windows.Controls.ColumnDefinition.WidthProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ColumnDefinition)), // DeclaringType
                            "Width", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.GridLengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentControl_Content()
        {
            Type type = typeof(System.Windows.Controls.ContentControl);
            DependencyProperty  dp = System.Windows.Controls.ContentControl.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentControl)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentControl_ContentTemplate()
        {
            Type type = typeof(System.Windows.Controls.ContentControl);
            DependencyProperty  dp = System.Windows.Controls.ContentControl.ContentTemplateProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentControl)), // DeclaringType
                            "ContentTemplate", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentControl_ContentTemplateSelector()
        {
            Type type = typeof(System.Windows.Controls.ContentControl);
            DependencyProperty  dp = System.Windows.Controls.ContentControl.ContentTemplateSelectorProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentControl)), // DeclaringType
                            "ContentTemplateSelector", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentControl_HasContent()
        {
            Type type = typeof(System.Windows.Controls.ContentControl);
            DependencyProperty  dp = System.Windows.Controls.ContentControl.HasContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentControl)), // DeclaringType
                            "HasContent", // Name
                             dp, // DependencyProperty
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentElement_Focusable()
        {
            Type type = typeof(System.Windows.ContentElement);
            DependencyProperty  dp = System.Windows.ContentElement.FocusableProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.ContentElement)), // DeclaringType
                            "Focusable", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentPresenter_Content()
        {
            Type type = typeof(System.Windows.Controls.ContentPresenter);
            DependencyProperty  dp = System.Windows.Controls.ContentPresenter.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentPresenter)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentPresenter_ContentSource()
        {
            Type type = typeof(System.Windows.Controls.ContentPresenter);
            DependencyProperty  dp = System.Windows.Controls.ContentPresenter.ContentSourceProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentPresenter)), // DeclaringType
                            "ContentSource", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentPresenter_ContentTemplate()
        {
            Type type = typeof(System.Windows.Controls.ContentPresenter);
            DependencyProperty  dp = System.Windows.Controls.ContentPresenter.ContentTemplateProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentPresenter)), // DeclaringType
                            "ContentTemplate", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentPresenter_ContentTemplateSelector()
        {
            Type type = typeof(System.Windows.Controls.ContentPresenter);
            DependencyProperty  dp = System.Windows.Controls.ContentPresenter.ContentTemplateSelectorProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentPresenter)), // DeclaringType
                            "ContentTemplateSelector", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContentPresenter_RecognizesAccessKey()
        {
            Type type = typeof(System.Windows.Controls.ContentPresenter);
            DependencyProperty  dp = System.Windows.Controls.ContentPresenter.RecognizesAccessKeyProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContentPresenter)), // DeclaringType
                            "RecognizesAccessKey", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_Background()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.BackgroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "Background", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_BorderBrush()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.BorderBrushProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "BorderBrush", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_BorderThickness()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.BorderThicknessProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "BorderThickness", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.ThicknessConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_FontFamily()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.FontFamilyProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "FontFamily", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.FontFamilyConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_FontSize()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.FontSizeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "FontSize", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontSizeConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_FontStretch()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.FontStretchProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "FontStretch", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontStretchConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_FontStyle()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.FontStyleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "FontStyle", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontStyleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_FontWeight()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.FontWeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "FontWeight", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontWeightConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_Foreground()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.ForegroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "Foreground", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_HorizontalContentAlignment()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.HorizontalContentAlignmentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "HorizontalContentAlignment", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.HorizontalAlignment);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_IsTabStop()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.IsTabStopProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "IsTabStop", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_Padding()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.PaddingProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "Padding", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.ThicknessConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_TabIndex()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.TabIndexProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "TabIndex", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.Int32Converter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_Template()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.TemplateProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "Template", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Control_VerticalContentAlignment()
        {
            Type type = typeof(System.Windows.Controls.Control);
            DependencyProperty  dp = System.Windows.Controls.Control.VerticalContentAlignmentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Control)), // DeclaringType
                            "VerticalContentAlignment", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.VerticalAlignment);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DockPanel_Dock()
        {
            Type type = typeof(System.Windows.Controls.DockPanel);
            DependencyProperty  dp = System.Windows.Controls.DockPanel.DockProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.DockPanel)), // DeclaringType
                            "Dock", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.Dock);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DockPanel_LastChildFill()
        {
            Type type = typeof(System.Windows.Controls.DockPanel);
            DependencyProperty  dp = System.Windows.Controls.DockPanel.LastChildFillProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.DockPanel)), // DeclaringType
                            "LastChildFill", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DocumentViewerBase_Document()
        {
            Type type = typeof(System.Windows.Controls.Primitives.DocumentViewerBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.DocumentViewerBase.DocumentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.DocumentViewerBase)), // DeclaringType
                            "Document", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DrawingGroup_Children()
        {
            Type type = typeof(System.Windows.Media.DrawingGroup);
            DependencyProperty  dp = System.Windows.Media.DrawingGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.DrawingGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FlowDocumentReader_Document()
        {
            Type type = typeof(System.Windows.Controls.FlowDocumentReader);
            DependencyProperty  dp = System.Windows.Controls.FlowDocumentReader.DocumentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.FlowDocumentReader)), // DeclaringType
                            "Document", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FlowDocumentScrollViewer_Document()
        {
            Type type = typeof(System.Windows.Controls.FlowDocumentScrollViewer);
            DependencyProperty  dp = System.Windows.Controls.FlowDocumentScrollViewer.DocumentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.FlowDocumentScrollViewer)), // DeclaringType
                            "Document", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkContentElement_Style()
        {
            Type type = typeof(System.Windows.FrameworkContentElement);
            DependencyProperty  dp = System.Windows.FrameworkContentElement.StyleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkContentElement)), // DeclaringType
                            "Style", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_FlowDirection()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.FlowDirectionProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "FlowDirection", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FlowDirection);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_Height()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.HeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "Height", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_HorizontalAlignment()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.HorizontalAlignmentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "HorizontalAlignment", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.HorizontalAlignment);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_Margin()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.MarginProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "Margin", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.ThicknessConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_MaxHeight()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.MaxHeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "MaxHeight", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_MaxWidth()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.MaxWidthProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "MaxWidth", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_MinHeight()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.MinHeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "MinHeight", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_MinWidth()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.MinWidthProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "MinWidth", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_Name()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.NameProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "Name", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_Style()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.StyleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "Style", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_VerticalAlignment()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.VerticalAlignmentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "VerticalAlignment", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.VerticalAlignment);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_Width()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            DependencyProperty  dp = System.Windows.FrameworkElement.WidthProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "Width", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GeneralTransformGroup_Children()
        {
            Type type = typeof(System.Windows.Media.GeneralTransformGroup);
            DependencyProperty  dp = System.Windows.Media.GeneralTransformGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.GeneralTransformGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GeometryGroup_Children()
        {
            Type type = typeof(System.Windows.Media.GeometryGroup);
            DependencyProperty  dp = System.Windows.Media.GeometryGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.GeometryGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GradientBrush_GradientStops()
        {
            Type type = typeof(System.Windows.Media.GradientBrush);
            DependencyProperty  dp = System.Windows.Media.GradientBrush.GradientStopsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.GradientBrush)), // DeclaringType
                            "GradientStops", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Grid_Column()
        {
            Type type = typeof(System.Windows.Controls.Grid);
            DependencyProperty  dp = System.Windows.Controls.Grid.ColumnProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Grid)), // DeclaringType
                            "Column", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.Int32Converter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Grid_ColumnSpan()
        {
            Type type = typeof(System.Windows.Controls.Grid);
            DependencyProperty  dp = System.Windows.Controls.Grid.ColumnSpanProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Grid)), // DeclaringType
                            "ColumnSpan", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.Int32Converter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Grid_Row()
        {
            Type type = typeof(System.Windows.Controls.Grid);
            DependencyProperty  dp = System.Windows.Controls.Grid.RowProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Grid)), // DeclaringType
                            "Row", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.Int32Converter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Grid_RowSpan()
        {
            Type type = typeof(System.Windows.Controls.Grid);
            DependencyProperty  dp = System.Windows.Controls.Grid.RowSpanProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Grid)), // DeclaringType
                            "RowSpan", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.Int32Converter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GridViewColumn_Header()
        {
            Type type = typeof(System.Windows.Controls.GridViewColumn);
            DependencyProperty  dp = System.Windows.Controls.GridViewColumn.HeaderProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.GridViewColumn)), // DeclaringType
                            "Header", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedContentControl_HasHeader()
        {
            Type type = typeof(System.Windows.Controls.HeaderedContentControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedContentControl.HasHeaderProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedContentControl)), // DeclaringType
                            "HasHeader", // Name
                             dp, // DependencyProperty
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedContentControl_Header()
        {
            Type type = typeof(System.Windows.Controls.HeaderedContentControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedContentControl.HeaderProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedContentControl)), // DeclaringType
                            "Header", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedContentControl_HeaderTemplate()
        {
            Type type = typeof(System.Windows.Controls.HeaderedContentControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedContentControl.HeaderTemplateProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedContentControl)), // DeclaringType
                            "HeaderTemplate", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedContentControl_HeaderTemplateSelector()
        {
            Type type = typeof(System.Windows.Controls.HeaderedContentControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedContentControl.HeaderTemplateSelectorProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedContentControl)), // DeclaringType
                            "HeaderTemplateSelector", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedItemsControl_HasHeader()
        {
            Type type = typeof(System.Windows.Controls.HeaderedItemsControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedItemsControl.HasHeaderProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedItemsControl)), // DeclaringType
                            "HasHeader", // Name
                             dp, // DependencyProperty
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedItemsControl_Header()
        {
            Type type = typeof(System.Windows.Controls.HeaderedItemsControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedItemsControl.HeaderProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedItemsControl)), // DeclaringType
                            "Header", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedItemsControl_HeaderTemplate()
        {
            Type type = typeof(System.Windows.Controls.HeaderedItemsControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedItemsControl.HeaderTemplateProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedItemsControl)), // DeclaringType
                            "HeaderTemplate", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedItemsControl_HeaderTemplateSelector()
        {
            Type type = typeof(System.Windows.Controls.HeaderedItemsControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedItemsControl.HeaderTemplateSelectorProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedItemsControl)), // DeclaringType
                            "HeaderTemplateSelector", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Hyperlink_NavigateUri()
        {
            Type type = typeof(System.Windows.Documents.Hyperlink);
            DependencyProperty  dp = System.Windows.Documents.Hyperlink.NavigateUriProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Hyperlink)), // DeclaringType
                            "NavigateUri", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.UriTypeConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Image_Source()
        {
            Type type = typeof(System.Windows.Controls.Image);
            DependencyProperty  dp = System.Windows.Controls.Image.SourceProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Image)), // DeclaringType
                            "Source", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Image_Stretch()
        {
            Type type = typeof(System.Windows.Controls.Image);
            DependencyProperty  dp = System.Windows.Controls.Image.StretchProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Image)), // DeclaringType
                            "Stretch", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.Stretch);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsControl_ItemContainerStyle()
        {
            Type type = typeof(System.Windows.Controls.ItemsControl);
            DependencyProperty  dp = System.Windows.Controls.ItemsControl.ItemContainerStyleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsControl)), // DeclaringType
                            "ItemContainerStyle", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsControl_ItemContainerStyleSelector()
        {
            Type type = typeof(System.Windows.Controls.ItemsControl);
            DependencyProperty  dp = System.Windows.Controls.ItemsControl.ItemContainerStyleSelectorProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsControl)), // DeclaringType
                            "ItemContainerStyleSelector", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsControl_ItemTemplate()
        {
            Type type = typeof(System.Windows.Controls.ItemsControl);
            DependencyProperty  dp = System.Windows.Controls.ItemsControl.ItemTemplateProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsControl)), // DeclaringType
                            "ItemTemplate", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsControl_ItemTemplateSelector()
        {
            Type type = typeof(System.Windows.Controls.ItemsControl);
            DependencyProperty  dp = System.Windows.Controls.ItemsControl.ItemTemplateSelectorProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsControl)), // DeclaringType
                            "ItemTemplateSelector", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsControl_ItemsPanel()
        {
            Type type = typeof(System.Windows.Controls.ItemsControl);
            DependencyProperty  dp = System.Windows.Controls.ItemsControl.ItemsPanelProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsControl)), // DeclaringType
                            "ItemsPanel", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsControl_ItemsSource()
        {
            Type type = typeof(System.Windows.Controls.ItemsControl);
            DependencyProperty  dp = System.Windows.Controls.ItemsControl.ItemsSourceProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsControl)), // DeclaringType
                            "ItemsSource", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MaterialGroup_Children()
        {
            Type type = typeof(System.Windows.Media.Media3D.MaterialGroup);
            DependencyProperty  dp = System.Windows.Media.Media3D.MaterialGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Media3D.MaterialGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Model3DGroup_Children()
        {
            Type type = typeof(System.Windows.Media.Media3D.Model3DGroup);
            DependencyProperty  dp = System.Windows.Media.Media3D.Model3DGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Media3D.Model3DGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Page_Content()
        {
            Type type = typeof(System.Windows.Controls.Page);
            DependencyProperty  dp = System.Windows.Controls.Page.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Page)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Panel_Background()
        {
            Type type = typeof(System.Windows.Controls.Panel);
            DependencyProperty  dp = System.Windows.Controls.Panel.BackgroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Panel)), // DeclaringType
                            "Background", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Path_Data()
        {
            Type type = typeof(System.Windows.Shapes.Path);
            DependencyProperty  dp = System.Windows.Shapes.Path.DataProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Path)), // DeclaringType
                            "Data", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PathFigure_Segments()
        {
            Type type = typeof(System.Windows.Media.PathFigure);
            DependencyProperty  dp = System.Windows.Media.PathFigure.SegmentsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.PathFigure)), // DeclaringType
                            "Segments", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PathGeometry_Figures()
        {
            Type type = typeof(System.Windows.Media.PathGeometry);
            DependencyProperty  dp = System.Windows.Media.PathGeometry.FiguresProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.PathGeometry)), // DeclaringType
                            "Figures", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.PathFigureCollectionConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Popup_Child()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Popup);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Popup.ChildProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Popup)), // DeclaringType
                            "Child", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Popup_IsOpen()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Popup);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Popup.IsOpenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Popup)), // DeclaringType
                            "IsOpen", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Popup_Placement()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Popup);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Popup.PlacementProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Popup)), // DeclaringType
                            "Placement", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.Primitives.PlacementMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Popup_PopupAnimation()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Popup);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Popup.PopupAnimationProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Popup)), // DeclaringType
                            "PopupAnimation", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.Primitives.PopupAnimation);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RowDefinition_Height()
        {
            Type type = typeof(System.Windows.Controls.RowDefinition);
            DependencyProperty  dp = System.Windows.Controls.RowDefinition.HeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.RowDefinition)), // DeclaringType
                            "Height", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.GridLengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RowDefinition_MaxHeight()
        {
            Type type = typeof(System.Windows.Controls.RowDefinition);
            DependencyProperty  dp = System.Windows.Controls.RowDefinition.MaxHeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.RowDefinition)), // DeclaringType
                            "MaxHeight", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RowDefinition_MinHeight()
        {
            Type type = typeof(System.Windows.Controls.RowDefinition);
            DependencyProperty  dp = System.Windows.Controls.RowDefinition.MinHeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.RowDefinition)), // DeclaringType
                            "MinHeight", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ScrollViewer_CanContentScroll()
        {
            Type type = typeof(System.Windows.Controls.ScrollViewer);
            DependencyProperty  dp = System.Windows.Controls.ScrollViewer.CanContentScrollProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ScrollViewer)), // DeclaringType
                            "CanContentScroll", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ScrollViewer_HorizontalScrollBarVisibility()
        {
            Type type = typeof(System.Windows.Controls.ScrollViewer);
            DependencyProperty  dp = System.Windows.Controls.ScrollViewer.HorizontalScrollBarVisibilityProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ScrollViewer)), // DeclaringType
                            "HorizontalScrollBarVisibility", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.ScrollBarVisibility);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ScrollViewer_VerticalScrollBarVisibility()
        {
            Type type = typeof(System.Windows.Controls.ScrollViewer);
            DependencyProperty  dp = System.Windows.Controls.ScrollViewer.VerticalScrollBarVisibilityProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ScrollViewer)), // DeclaringType
                            "VerticalScrollBarVisibility", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.ScrollBarVisibility);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_Fill()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.FillProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "Fill", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_Stroke()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.StrokeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "Stroke", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_StrokeThickness()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.StrokeThicknessProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "StrokeThickness", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_Background()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.BackgroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "Background", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_FontFamily()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.FontFamilyProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "FontFamily", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.FontFamilyConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_FontSize()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.FontSizeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "FontSize", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontSizeConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_FontStretch()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.FontStretchProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "FontStretch", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontStretchConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_FontStyle()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.FontStyleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "FontStyle", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontStyleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_FontWeight()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.FontWeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "FontWeight", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontWeightConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_Foreground()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.ForegroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "Foreground", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_Text()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.TextProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "Text", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_TextDecorations()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.TextDecorationsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "TextDecorations", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.TextDecorationCollectionConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_TextTrimming()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.TextTrimmingProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "TextTrimming", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.TextTrimming);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_TextWrapping()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.TextWrappingProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "TextWrapping", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.TextWrapping);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBox_Text()
        {
            Type type = typeof(System.Windows.Controls.TextBox);
            DependencyProperty  dp = System.Windows.Controls.TextBox.TextProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBox)), // DeclaringType
                            "Text", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextElement_Background()
        {
            Type type = typeof(System.Windows.Documents.TextElement);
            DependencyProperty  dp = System.Windows.Documents.TextElement.BackgroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TextElement)), // DeclaringType
                            "Background", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextElement_FontFamily()
        {
            Type type = typeof(System.Windows.Documents.TextElement);
            DependencyProperty  dp = System.Windows.Documents.TextElement.FontFamilyProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TextElement)), // DeclaringType
                            "FontFamily", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.FontFamilyConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextElement_FontSize()
        {
            Type type = typeof(System.Windows.Documents.TextElement);
            DependencyProperty  dp = System.Windows.Documents.TextElement.FontSizeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TextElement)), // DeclaringType
                            "FontSize", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontSizeConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextElement_FontStretch()
        {
            Type type = typeof(System.Windows.Documents.TextElement);
            DependencyProperty  dp = System.Windows.Documents.TextElement.FontStretchProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TextElement)), // DeclaringType
                            "FontStretch", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontStretchConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextElement_FontStyle()
        {
            Type type = typeof(System.Windows.Documents.TextElement);
            DependencyProperty  dp = System.Windows.Documents.TextElement.FontStyleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TextElement)), // DeclaringType
                            "FontStyle", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontStyleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextElement_FontWeight()
        {
            Type type = typeof(System.Windows.Documents.TextElement);
            DependencyProperty  dp = System.Windows.Documents.TextElement.FontWeightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TextElement)), // DeclaringType
                            "FontWeight", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.FontWeightConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextElement_Foreground()
        {
            Type type = typeof(System.Windows.Documents.TextElement);
            DependencyProperty  dp = System.Windows.Documents.TextElement.ForegroundProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TextElement)), // DeclaringType
                            "Foreground", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TimelineGroup_Children()
        {
            Type type = typeof(System.Windows.Media.Animation.TimelineGroup);
            DependencyProperty  dp = System.Windows.Media.Animation.TimelineGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.TimelineGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_IsDirectionReversed()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Track.IsDirectionReversedProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "IsDirectionReversed", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_Maximum()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Track.MaximumProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "Maximum", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.DoubleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_Minimum()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Track.MinimumProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "Minimum", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.DoubleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_Orientation()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Track.OrientationProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "Orientation", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.Orientation);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_Value()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Track.ValueProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "Value", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.DoubleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_ViewportSize()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Track.ViewportSizeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "ViewportSize", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.DoubleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Transform3DGroup_Children()
        {
            Type type = typeof(System.Windows.Media.Media3D.Transform3DGroup);
            DependencyProperty  dp = System.Windows.Media.Media3D.Transform3DGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Media3D.Transform3DGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TransformGroup_Children()
        {
            Type type = typeof(System.Windows.Media.TransformGroup);
            DependencyProperty  dp = System.Windows.Media.TransformGroup.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.TransformGroup)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_ClipToBounds()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.ClipToBoundsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "ClipToBounds", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_Focusable()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.FocusableProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "Focusable", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_IsEnabled()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.IsEnabledProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "IsEnabled", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_RenderTransform()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.RenderTransformProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "RenderTransform", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_Visibility()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.VisibilityProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "Visibility", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Visibility);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Viewport3D_Children()
        {
            Type type = typeof(System.Windows.Controls.Viewport3D);
            DependencyProperty  dp = System.Windows.Controls.Viewport3D.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Viewport3D)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_AdornedElementPlaceholder_Child()
        {
            Type type = typeof(System.Windows.Controls.AdornedElementPlaceholder);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.AdornedElementPlaceholder)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.AdornedElementPlaceholder)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.AdornedElementPlaceholder)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_AdornerDecorator_Child()
        {
            Type type = typeof(System.Windows.Documents.AdornerDecorator);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.AdornerDecorator)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Documents.AdornerDecorator)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.AdornerDecorator)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_AnchoredBlock_Blocks()
        {
            Type type = typeof(System.Windows.Documents.AnchoredBlock);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.AnchoredBlock)), // DeclaringType
                            "Blocks", // Name
                            typeof(System.Windows.Documents.BlockCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.AnchoredBlock)target).Blocks; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ArrayExtension_Items()
        {
            Type type = typeof(System.Windows.Markup.ArrayExtension);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Markup.ArrayExtension)), // DeclaringType
                            "Items", // Name
                            typeof(System.Collections.IList), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Markup.ArrayExtension)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BlockUIContainer_Child()
        {
            Type type = typeof(System.Windows.Documents.BlockUIContainer);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.BlockUIContainer)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Documents.BlockUIContainer)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.BlockUIContainer)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Bold_Inlines()
        {
            Type type = typeof(System.Windows.Documents.Bold);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Bold)), // DeclaringType
                            "Inlines", // Name
                            typeof(System.Windows.Documents.InlineCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Bold)target).Inlines; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BooleanAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.BooleanKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.BooleanKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Border_Child()
        {
            Type type = typeof(System.Windows.Controls.Border);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Border)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Border)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Border)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BulletDecorator_Child()
        {
            Type type = typeof(System.Windows.Controls.Primitives.BulletDecorator);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.BulletDecorator)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Primitives.BulletDecorator)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.BulletDecorator)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Button_Content()
        {
            Type type = typeof(System.Windows.Controls.Button);
            DependencyProperty  dp = System.Windows.Controls.Button.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Button)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ButtonBase_Content()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ButtonBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ButtonBase.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ButtonBase)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ByteAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.ByteAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.ByteAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.ByteKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.ByteAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.ByteKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.ByteAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Canvas_Children()
        {
            Type type = typeof(System.Windows.Controls.Canvas);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Canvas)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Canvas)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_CharAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.CharAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.CharAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.CharKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.CharAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.CharKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.CharAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_CheckBox_Content()
        {
            Type type = typeof(System.Windows.Controls.CheckBox);
            DependencyProperty  dp = System.Windows.Controls.CheckBox.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.CheckBox)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ColorAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.ColorAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.ColorAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.ColorKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.ColorAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.ColorKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.ColorAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ComboBox_Items()
        {
            Type type = typeof(System.Windows.Controls.ComboBox);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ComboBox)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ComboBox)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ComboBoxItem_Content()
        {
            Type type = typeof(System.Windows.Controls.ComboBoxItem);
            DependencyProperty  dp = System.Windows.Controls.ComboBoxItem.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ComboBoxItem)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ContextMenu_Items()
        {
            Type type = typeof(System.Windows.Controls.ContextMenu);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ContextMenu)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ContextMenu)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ControlTemplate_VisualTree()
        {
            Type type = typeof(System.Windows.Controls.ControlTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ControlTemplate)), // DeclaringType
                            "VisualTree", // Name
                            typeof(System.Windows.FrameworkElementFactory), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.ControlTemplate)target).VisualTree = (System.Windows.FrameworkElementFactory)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ControlTemplate)target).VisualTree; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DataTemplate_VisualTree()
        {
            Type type = typeof(System.Windows.DataTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.DataTemplate)), // DeclaringType
                            "VisualTree", // Name
                            typeof(System.Windows.FrameworkElementFactory), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.DataTemplate)target).VisualTree = (System.Windows.FrameworkElementFactory)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.DataTemplate)target).VisualTree; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DataTrigger_Setters()
        {
            Type type = typeof(System.Windows.DataTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.DataTrigger)), // DeclaringType
                            "Setters", // Name
                            typeof(System.Windows.SetterBaseCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.DataTrigger)target).Setters; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DecimalAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.DecimalKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.DecimalKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Decorator_Child()
        {
            Type type = typeof(System.Windows.Controls.Decorator);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Decorator)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Decorator)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Decorator)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DockPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.DockPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.DockPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.DockPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DocumentViewer_Document()
        {
            Type type = typeof(System.Windows.Controls.DocumentViewer);
            DependencyProperty  dp = System.Windows.Controls.DocumentViewer.DocumentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.DocumentViewer)), // DeclaringType
                            "Document", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DoubleAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.DoubleKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.DoubleKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_EventTrigger_Actions()
        {
            Type type = typeof(System.Windows.EventTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.EventTrigger)), // DeclaringType
                            "Actions", // Name
                            typeof(System.Windows.TriggerActionCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.EventTrigger)target).Actions; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Expander_Content()
        {
            Type type = typeof(System.Windows.Controls.Expander);
            DependencyProperty  dp = System.Windows.Controls.Expander.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Expander)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Figure_Blocks()
        {
            Type type = typeof(System.Windows.Documents.Figure);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Figure)), // DeclaringType
                            "Blocks", // Name
                            typeof(System.Windows.Documents.BlockCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Figure)target).Blocks; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FixedDocument_Pages()
        {
            Type type = typeof(System.Windows.Documents.FixedDocument);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.FixedDocument)), // DeclaringType
                            "Pages", // Name
                            typeof(System.Windows.Documents.PageContentCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.FixedDocument)target).Pages; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FixedDocumentSequence_References()
        {
            Type type = typeof(System.Windows.Documents.FixedDocumentSequence);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.FixedDocumentSequence)), // DeclaringType
                            "References", // Name
                            typeof(System.Windows.Documents.DocumentReferenceCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.FixedDocumentSequence)target).References; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FixedPage_Children()
        {
            Type type = typeof(System.Windows.Documents.FixedPage);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.FixedPage)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.FixedPage)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Floater_Blocks()
        {
            Type type = typeof(System.Windows.Documents.Floater);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Floater)), // DeclaringType
                            "Blocks", // Name
                            typeof(System.Windows.Documents.BlockCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Floater)target).Blocks; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FlowDocument_Blocks()
        {
            Type type = typeof(System.Windows.Documents.FlowDocument);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.FlowDocument)), // DeclaringType
                            "Blocks", // Name
                            typeof(System.Windows.Documents.BlockCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.FlowDocument)target).Blocks; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FlowDocumentPageViewer_Document()
        {
            Type type = typeof(System.Windows.Controls.FlowDocumentPageViewer);
            DependencyProperty  dp = System.Windows.Controls.FlowDocumentPageViewer.DocumentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.FlowDocumentPageViewer)), // DeclaringType
                            "Document", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkTemplate_VisualTree()
        {
            Type type = typeof(System.Windows.FrameworkTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkTemplate)), // DeclaringType
                            "VisualTree", // Name
                            typeof(System.Windows.FrameworkElementFactory), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.FrameworkTemplate)target).VisualTree = (System.Windows.FrameworkElementFactory)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.FrameworkTemplate)target).VisualTree; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Grid_Children()
        {
            Type type = typeof(System.Windows.Controls.Grid);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Grid)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Grid)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GridView_Columns()
        {
            Type type = typeof(System.Windows.Controls.GridView);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.GridView)), // DeclaringType
                            "Columns", // Name
                            typeof(System.Windows.Controls.GridViewColumnCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.GridView)target).Columns; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GridViewColumnHeader_Content()
        {
            Type type = typeof(System.Windows.Controls.GridViewColumnHeader);
            DependencyProperty  dp = System.Windows.Controls.GridViewColumnHeader.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.GridViewColumnHeader)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GroupBox_Content()
        {
            Type type = typeof(System.Windows.Controls.GroupBox);
            DependencyProperty  dp = System.Windows.Controls.GroupBox.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.GroupBox)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GroupItem_Content()
        {
            Type type = typeof(System.Windows.Controls.GroupItem);
            DependencyProperty  dp = System.Windows.Controls.GroupItem.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.GroupItem)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedContentControl_Content()
        {
            Type type = typeof(System.Windows.Controls.HeaderedContentControl);
            DependencyProperty  dp = System.Windows.Controls.HeaderedContentControl.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedContentControl)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HeaderedItemsControl_Items()
        {
            Type type = typeof(System.Windows.Controls.HeaderedItemsControl);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.HeaderedItemsControl)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.HeaderedItemsControl)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_HierarchicalDataTemplate_VisualTree()
        {
            Type type = typeof(System.Windows.HierarchicalDataTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.HierarchicalDataTemplate)), // DeclaringType
                            "VisualTree", // Name
                            typeof(System.Windows.FrameworkElementFactory), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.HierarchicalDataTemplate)target).VisualTree = (System.Windows.FrameworkElementFactory)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.HierarchicalDataTemplate)target).VisualTree; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Hyperlink_Inlines()
        {
            Type type = typeof(System.Windows.Documents.Hyperlink);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Hyperlink)), // DeclaringType
                            "Inlines", // Name
                            typeof(System.Windows.Documents.InlineCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Hyperlink)target).Inlines; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_InkCanvas_Children()
        {
            Type type = typeof(System.Windows.Controls.InkCanvas);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.InkCanvas)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.InkCanvas)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_InkPresenter_Child()
        {
            Type type = typeof(System.Windows.Controls.InkPresenter);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.InkPresenter)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.InkPresenter)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.InkPresenter)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_InlineUIContainer_Child()
        {
            Type type = typeof(System.Windows.Documents.InlineUIContainer);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.InlineUIContainer)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Documents.InlineUIContainer)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.InlineUIContainer)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_InputScopeName_NameValue()
        {
            Type type = typeof(System.Windows.Input.InputScopeName);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Input.InputScopeName)), // DeclaringType
                            "NameValue", // Name
                            typeof(System.Windows.Input.InputScopeNameValue), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.InputScopeNameValue);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Input.InputScopeName)target).NameValue = (System.Windows.Input.InputScopeNameValue)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Input.InputScopeName)target).NameValue; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Int16AnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.Int16AnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Int16AnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.Int16KeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.Int16AnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.Int16KeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.Int16AnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Int32AnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.Int32AnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Int32AnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.Int32KeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.Int32AnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.Int32KeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.Int32AnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Int64AnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.Int64AnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Int64AnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.Int64KeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.Int64AnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.Int64KeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.Int64AnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Italic_Inlines()
        {
            Type type = typeof(System.Windows.Documents.Italic);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Italic)), // DeclaringType
                            "Inlines", // Name
                            typeof(System.Windows.Documents.InlineCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Italic)target).Inlines; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsControl_Items()
        {
            Type type = typeof(System.Windows.Controls.ItemsControl);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsControl)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ItemsControl)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ItemsPanelTemplate_VisualTree()
        {
            Type type = typeof(System.Windows.Controls.ItemsPanelTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ItemsPanelTemplate)), // DeclaringType
                            "VisualTree", // Name
                            typeof(System.Windows.FrameworkElementFactory), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.ItemsPanelTemplate)target).VisualTree = (System.Windows.FrameworkElementFactory)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ItemsPanelTemplate)target).VisualTree; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Label_Content()
        {
            Type type = typeof(System.Windows.Controls.Label);
            DependencyProperty  dp = System.Windows.Controls.Label.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Label)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_LinearGradientBrush_GradientStops()
        {
            Type type = typeof(System.Windows.Media.LinearGradientBrush);
            DependencyProperty  dp = System.Windows.Media.LinearGradientBrush.GradientStopsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.LinearGradientBrush)), // DeclaringType
                            "GradientStops", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_List_ListItems()
        {
            Type type = typeof(System.Windows.Documents.List);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.List)), // DeclaringType
                            "ListItems", // Name
                            typeof(System.Windows.Documents.ListItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.List)target).ListItems; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ListBox_Items()
        {
            Type type = typeof(System.Windows.Controls.ListBox);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ListBox)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ListBox)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ListBoxItem_Content()
        {
            Type type = typeof(System.Windows.Controls.ListBoxItem);
            DependencyProperty  dp = System.Windows.Controls.ListBoxItem.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ListBoxItem)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ListItem_Blocks()
        {
            Type type = typeof(System.Windows.Documents.ListItem);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.ListItem)), // DeclaringType
                            "Blocks", // Name
                            typeof(System.Windows.Documents.BlockCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.ListItem)target).Blocks; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ListView_Items()
        {
            Type type = typeof(System.Windows.Controls.ListView);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ListView)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ListView)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ListViewItem_Content()
        {
            Type type = typeof(System.Windows.Controls.ListViewItem);
            DependencyProperty  dp = System.Windows.Controls.ListViewItem.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ListViewItem)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MatrixAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.MatrixKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.MatrixKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Menu_Items()
        {
            Type type = typeof(System.Windows.Controls.Menu);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Menu)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Menu)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MenuBase_Items()
        {
            Type type = typeof(System.Windows.Controls.Primitives.MenuBase);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.MenuBase)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.MenuBase)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MenuItem_Items()
        {
            Type type = typeof(System.Windows.Controls.MenuItem);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.MenuItem)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.MenuItem)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ModelVisual3D_Children()
        {
            Type type = typeof(System.Windows.Media.Media3D.ModelVisual3D);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Media3D.ModelVisual3D)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Media.Media3D.Visual3DCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Media3D.ModelVisual3D)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MultiBinding_Bindings()
        {
            Type type = typeof(System.Windows.Data.MultiBinding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.MultiBinding)), // DeclaringType
                            "Bindings", // Name
                            typeof(System.Collections.ObjectModel.Collection<System.Windows.Data.BindingBase>), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.MultiBinding)target).Bindings; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MultiDataTrigger_Setters()
        {
            Type type = typeof(System.Windows.MultiDataTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.MultiDataTrigger)), // DeclaringType
                            "Setters", // Name
                            typeof(System.Windows.SetterBaseCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.MultiDataTrigger)target).Setters; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MultiTrigger_Setters()
        {
            Type type = typeof(System.Windows.MultiTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.MultiTrigger)), // DeclaringType
                            "Setters", // Name
                            typeof(System.Windows.SetterBaseCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.MultiTrigger)target).Setters; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ObjectAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.ObjectKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.ObjectKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PageContent_Child()
        {
            Type type = typeof(System.Windows.Documents.PageContent);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.PageContent)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.Documents.FixedPage), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Documents.PageContent)target).Child = (System.Windows.Documents.FixedPage)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.PageContent)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PageFunctionBase_Content()
        {
            Type type = typeof(System.Windows.Navigation.PageFunctionBase);
            DependencyProperty  dp = System.Windows.Navigation.PageFunctionBase.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Navigation.PageFunctionBase)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Panel_Children()
        {
            Type type = typeof(System.Windows.Controls.Panel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Panel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Panel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Paragraph_Inlines()
        {
            Type type = typeof(System.Windows.Documents.Paragraph);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Paragraph)), // DeclaringType
                            "Inlines", // Name
                            typeof(System.Windows.Documents.InlineCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Paragraph)target).Inlines; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ParallelTimeline_Children()
        {
            Type type = typeof(System.Windows.Media.Animation.ParallelTimeline);
            DependencyProperty  dp = System.Windows.Media.Animation.ParallelTimeline.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.ParallelTimeline)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Point3DAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.Point3DKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.Point3DKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PointAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.PointAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.PointAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.PointKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.PointAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.PointKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.PointAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PriorityBinding_Bindings()
        {
            Type type = typeof(System.Windows.Data.PriorityBinding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.PriorityBinding)), // DeclaringType
                            "Bindings", // Name
                            typeof(System.Collections.ObjectModel.Collection<System.Windows.Data.BindingBase>), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.PriorityBinding)target).Bindings; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_QuaternionAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.QuaternionKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.QuaternionKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RadialGradientBrush_GradientStops()
        {
            Type type = typeof(System.Windows.Media.RadialGradientBrush);
            DependencyProperty  dp = System.Windows.Media.RadialGradientBrush.GradientStopsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.RadialGradientBrush)), // DeclaringType
                            "GradientStops", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RadioButton_Content()
        {
            Type type = typeof(System.Windows.Controls.RadioButton);
            DependencyProperty  dp = System.Windows.Controls.RadioButton.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.RadioButton)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RectAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.RectAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.RectAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.RectKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.RectAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.RectKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.RectAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RepeatButton_Content()
        {
            Type type = typeof(System.Windows.Controls.Primitives.RepeatButton);
            DependencyProperty  dp = System.Windows.Controls.Primitives.RepeatButton.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.RepeatButton)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RichTextBox_Document()
        {
            Type type = typeof(System.Windows.Controls.RichTextBox);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.RichTextBox)), // DeclaringType
                            "Document", // Name
                            typeof(System.Windows.Documents.FlowDocument), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.RichTextBox)target).Document = (System.Windows.Documents.FlowDocument)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.RichTextBox)target).Document; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Rotation3DAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.Rotation3DKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.Rotation3DKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Run_Text()
        {
            Type type = typeof(System.Windows.Documents.Run);
            DependencyProperty  dp = System.Windows.Documents.Run.TextProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Run)), // DeclaringType
                            "Text", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ScrollViewer_Content()
        {
            Type type = typeof(System.Windows.Controls.ScrollViewer);
            DependencyProperty  dp = System.Windows.Controls.ScrollViewer.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ScrollViewer)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Section_Blocks()
        {
            Type type = typeof(System.Windows.Documents.Section);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Section)), // DeclaringType
                            "Blocks", // Name
                            typeof(System.Windows.Documents.BlockCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Section)target).Blocks; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Selector_Items()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Selector);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Selector)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.Selector)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_SingleAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.SingleAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.SingleAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.SingleKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.SingleAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.SingleKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.SingleAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_SizeAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.SizeAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.SizeAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.SizeKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.SizeAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.SizeKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.SizeAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Span_Inlines()
        {
            Type type = typeof(System.Windows.Documents.Span);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Span)), // DeclaringType
                            "Inlines", // Name
                            typeof(System.Windows.Documents.InlineCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Span)target).Inlines; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_StackPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.StackPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.StackPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.StackPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_StatusBar_Items()
        {
            Type type = typeof(System.Windows.Controls.Primitives.StatusBar);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.StatusBar)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.StatusBar)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_StatusBarItem_Content()
        {
            Type type = typeof(System.Windows.Controls.Primitives.StatusBarItem);
            DependencyProperty  dp = System.Windows.Controls.Primitives.StatusBarItem.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.StatusBarItem)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Storyboard_Children()
        {
            Type type = typeof(System.Windows.Media.Animation.Storyboard);
            DependencyProperty  dp = System.Windows.Media.Animation.Storyboard.ChildrenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Storyboard)), // DeclaringType
                            "Children", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_StringAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.StringAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.StringAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.StringKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.StringAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.StringKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.StringAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Style_Setters()
        {
            Type type = typeof(System.Windows.Style);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Style)), // DeclaringType
                            "Setters", // Name
                            typeof(System.Windows.SetterBaseCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Style)target).Setters; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TabControl_Items()
        {
            Type type = typeof(System.Windows.Controls.TabControl);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TabControl)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.TabControl)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TabItem_Content()
        {
            Type type = typeof(System.Windows.Controls.TabItem);
            DependencyProperty  dp = System.Windows.Controls.TabItem.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TabItem)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TabPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.Primitives.TabPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.TabPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.TabPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Table_RowGroups()
        {
            Type type = typeof(System.Windows.Documents.Table);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Table)), // DeclaringType
                            "RowGroups", // Name
                            typeof(System.Windows.Documents.TableRowGroupCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Table)target).RowGroups; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TableCell_Blocks()
        {
            Type type = typeof(System.Windows.Documents.TableCell);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TableCell)), // DeclaringType
                            "Blocks", // Name
                            typeof(System.Windows.Documents.BlockCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.TableCell)target).Blocks; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TableRow_Cells()
        {
            Type type = typeof(System.Windows.Documents.TableRow);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TableRow)), // DeclaringType
                            "Cells", // Name
                            typeof(System.Windows.Documents.TableCellCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.TableRow)target).Cells; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TableRowGroup_Rows()
        {
            Type type = typeof(System.Windows.Documents.TableRowGroup);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.TableRowGroup)), // DeclaringType
                            "Rows", // Name
                            typeof(System.Windows.Documents.TableRowCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.TableRowGroup)target).Rows; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_Inlines()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "Inlines", // Name
                            typeof(System.Windows.Documents.InlineCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.TextBlock)target).Inlines; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ThicknessAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.ThicknessKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.ThicknessKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToggleButton_Content()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ToggleButton);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ToggleButton.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ToggleButton)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToolBar_Items()
        {
            Type type = typeof(System.Windows.Controls.ToolBar);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ToolBar)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ToolBar)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToolBarOverflowPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ToolBarOverflowPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ToolBarOverflowPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.ToolBarOverflowPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToolBarPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ToolBarPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ToolBarPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.ToolBarPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToolBarTray_ToolBars()
        {
            Type type = typeof(System.Windows.Controls.ToolBarTray);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ToolBarTray)), // DeclaringType
                            "ToolBars", // Name
                            typeof(System.Collections.ObjectModel.Collection<System.Windows.Controls.ToolBar>), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ToolBarTray)target).ToolBars; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToolTip_Content()
        {
            Type type = typeof(System.Windows.Controls.ToolTip);
            DependencyProperty  dp = System.Windows.Controls.ToolTip.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ToolTip)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TreeView_Items()
        {
            Type type = typeof(System.Windows.Controls.TreeView);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TreeView)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.TreeView)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TreeViewItem_Items()
        {
            Type type = typeof(System.Windows.Controls.TreeViewItem);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TreeViewItem)), // DeclaringType
                            "Items", // Name
                            typeof(System.Windows.Controls.ItemCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.TreeViewItem)target).Items; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Trigger_Setters()
        {
            Type type = typeof(System.Windows.Trigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Trigger)), // DeclaringType
                            "Setters", // Name
                            typeof(System.Windows.SetterBaseCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Trigger)target).Setters; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Underline_Inlines()
        {
            Type type = typeof(System.Windows.Documents.Underline);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Underline)), // DeclaringType
                            "Inlines", // Name
                            typeof(System.Windows.Documents.InlineCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Documents.Underline)target).Inlines; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UniformGrid_Children()
        {
            Type type = typeof(System.Windows.Controls.Primitives.UniformGrid);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.UniformGrid)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.UniformGrid)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UserControl_Content()
        {
            Type type = typeof(System.Windows.Controls.UserControl);
            DependencyProperty  dp = System.Windows.Controls.UserControl.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.UserControl)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Vector3DAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.Vector3DKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.Vector3DKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_VectorAnimationUsingKeyFrames_KeyFrames()
        {
            Type type = typeof(System.Windows.Media.Animation.VectorAnimationUsingKeyFrames);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.VectorAnimationUsingKeyFrames)), // DeclaringType
                            "KeyFrames", // Name
                            typeof(System.Windows.Media.Animation.VectorKeyFrameCollection), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.VectorAnimationUsingKeyFrames)target).KeyFrames = (System.Windows.Media.Animation.VectorKeyFrameCollection)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.VectorAnimationUsingKeyFrames)target).KeyFrames; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Viewbox_Child()
        {
            Type type = typeof(System.Windows.Controls.Viewbox);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Viewbox)), // DeclaringType
                            "Child", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Viewbox)target).Child = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Viewbox)target).Child; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Viewport3DVisual_Children()
        {
            Type type = typeof(System.Windows.Media.Media3D.Viewport3DVisual);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Media3D.Viewport3DVisual)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Media.Media3D.Visual3DCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Media3D.Viewport3DVisual)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_VirtualizingPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.VirtualizingPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.VirtualizingPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.VirtualizingPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_VirtualizingStackPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.VirtualizingStackPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.VirtualizingStackPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.VirtualizingStackPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Window_Content()
        {
            Type type = typeof(System.Windows.Window);
            DependencyProperty  dp = System.Windows.Window.ContentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Window)), // DeclaringType
                            "Content", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_WrapPanel_Children()
        {
            Type type = typeof(System.Windows.Controls.WrapPanel);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.WrapPanel)), // DeclaringType
                            "Children", // Name
                            typeof(System.Windows.Controls.UIElementCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.WrapPanel)target).Children; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_XmlDataProvider_XmlSerializer()
        {
            Type type = typeof(System.Windows.Data.XmlDataProvider);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.XmlDataProvider)), // DeclaringType
                            "XmlSerializer", // Name
                            typeof(System.Xml.Serialization.IXmlSerializable), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.XmlDataProvider)target).XmlSerializer; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ControlTemplate_Triggers()
        {
            Type type = typeof(System.Windows.Controls.ControlTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ControlTemplate)), // DeclaringType
                            "Triggers", // Name
                            typeof(System.Windows.TriggerCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ControlTemplate)target).Triggers; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DataTemplate_Triggers()
        {
            Type type = typeof(System.Windows.DataTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.DataTemplate)), // DeclaringType
                            "Triggers", // Name
                            typeof(System.Windows.TriggerCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.DataTemplate)target).Triggers; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DataTemplate_DataTemplateKey()
        {
            Type type = typeof(System.Windows.DataTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.DataTemplate)), // DeclaringType
                            "DataTemplateKey", // Name
                            typeof(System.Object), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.DataTemplate)target).DataTemplateKey; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ControlTemplate_TargetType()
        {
            Type type = typeof(System.Windows.Controls.ControlTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ControlTemplate)), // DeclaringType
                            "TargetType", // Name
                            typeof(System.Type), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Type);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.ControlTemplate)target).TargetType = (System.Type)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.ControlTemplate)target).TargetType; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_Resources()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "Resources", // Name
                            typeof(System.Windows.ResourceDictionary), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.FrameworkElement)target).Resources = (System.Windows.ResourceDictionary)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.FrameworkElement)target).Resources; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkTemplate_Template()
        {
            Type type = typeof(System.Windows.FrameworkTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkTemplate)), // DeclaringType
                            "Template", // Name
                            typeof(System.Windows.TemplateContent), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
             bamlMember.DeferringLoaderType = typeof(System.Windows.TemplateContentLoader);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.FrameworkTemplate)target).Template = (System.Windows.TemplateContent)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.FrameworkTemplate)target).Template; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Grid_ColumnDefinitions()
        {
            Type type = typeof(System.Windows.Controls.Grid);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Grid)), // DeclaringType
                            "ColumnDefinitions", // Name
                            typeof(System.Windows.Controls.ColumnDefinitionCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Grid)target).ColumnDefinitions; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Grid_RowDefinitions()
        {
            Type type = typeof(System.Windows.Controls.Grid);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Grid)), // DeclaringType
                            "RowDefinitions", // Name
                            typeof(System.Windows.Controls.RowDefinitionCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Grid)target).RowDefinitions; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MultiTrigger_Conditions()
        {
            Type type = typeof(System.Windows.MultiTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.MultiTrigger)), // DeclaringType
                            "Conditions", // Name
                            typeof(System.Windows.ConditionCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.MultiTrigger)target).Conditions; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_NameScope_NameScope()
        {
            Type type = typeof(System.Windows.NameScope);
            DependencyProperty  dp = System.Windows.NameScope.NameScopeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.NameScope)), // DeclaringType
                            "NameScope", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Style_TargetType()
        {
            Type type = typeof(System.Windows.Style);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Style)), // DeclaringType
                            "TargetType", // Name
                            typeof(System.Type), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Type);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Style)target).TargetType = (System.Type)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Style)target).TargetType; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Style_Triggers()
        {
            Type type = typeof(System.Windows.Style);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Style)), // DeclaringType
                            "Triggers", // Name
                            typeof(System.Windows.TriggerCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Style)target).Triggers; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Setter_Value()
        {
            Type type = typeof(System.Windows.Setter);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Setter)), // DeclaringType
                            "Value", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Markup.SetterTriggerConditionValueConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Setter)target).Value = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Setter)target).Value; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Setter_TargetName()
        {
            Type type = typeof(System.Windows.Setter);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Setter)), // DeclaringType
                            "TargetName", // Name
                            typeof(System.String), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Setter)target).TargetName = (System.String)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Setter)target).TargetName; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_Path()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "Path", // Name
                            typeof(System.Windows.PropertyPath), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.PropertyPathConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).Path = (System.Windows.PropertyPath)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).Path; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ComponentResourceKey_ResourceId()
        {
            Type type = typeof(System.Windows.ComponentResourceKey);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.ComponentResourceKey)), // DeclaringType
                            "ResourceId", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.ComponentResourceKey)target).ResourceId = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.ComponentResourceKey)target).ResourceId; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ComponentResourceKey_TypeInTargetAssembly()
        {
            Type type = typeof(System.Windows.ComponentResourceKey);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.ComponentResourceKey)), // DeclaringType
                            "TypeInTargetAssembly", // Name
                            typeof(System.Type), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Type);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.ComponentResourceKey)target).TypeInTargetAssembly = (System.Type)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.ComponentResourceKey)target).TypeInTargetAssembly; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_Converter()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "Converter", // Name
                            typeof(System.Windows.Data.IValueConverter), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).Converter = (System.Windows.Data.IValueConverter)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).Converter; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_Source()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "Source", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).Source = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).Source; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_RelativeSource()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "RelativeSource", // Name
                            typeof(System.Windows.Data.RelativeSource), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).RelativeSource = (System.Windows.Data.RelativeSource)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).RelativeSource; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_Mode()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "Mode", // Name
                            typeof(System.Windows.Data.BindingMode), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Data.BindingMode);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).Mode = (System.Windows.Data.BindingMode)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).Mode; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Timeline_BeginTime()
        {
            Type type = typeof(System.Windows.Media.Animation.Timeline);
            DependencyProperty  dp = System.Windows.Media.Animation.Timeline.BeginTimeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Timeline)), // DeclaringType
                            "BeginTime", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.TimeSpanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Style_BasedOn()
        {
            Type type = typeof(System.Windows.Style);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Style)), // DeclaringType
                            "BasedOn", // Name
                            typeof(System.Windows.Style), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Style)target).BasedOn = (System.Windows.Style)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Style)target).BasedOn; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_ElementName()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "ElementName", // Name
                            typeof(System.String), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).ElementName = (System.String)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).ElementName; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_UpdateSourceTrigger()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "UpdateSourceTrigger", // Name
                            typeof(System.Windows.Data.UpdateSourceTrigger), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Data.UpdateSourceTrigger);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).UpdateSourceTrigger = (System.Windows.Data.UpdateSourceTrigger)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).UpdateSourceTrigger; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ResourceDictionary_DeferrableContent()
        {
            Type type = typeof(System.Windows.ResourceDictionary);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.ResourceDictionary)), // DeclaringType
                            "DeferrableContent", // Name
                            typeof(System.Windows.DeferrableContent), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.DeferrableContentConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.ResourceDictionary)target).DeferrableContent = (System.Windows.DeferrableContent)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.ResourceDictionary)target).DeferrableContent; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Trigger_Value()
        {
            Type type = typeof(System.Windows.Trigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Trigger)), // DeclaringType
                            "Value", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Markup.SetterTriggerConditionValueConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Trigger)target).Value = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Trigger)target).Value; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Trigger_SourceName()
        {
            Type type = typeof(System.Windows.Trigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Trigger)), // DeclaringType
                            "SourceName", // Name
                            typeof(System.String), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Trigger)target).SourceName = (System.String)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Trigger)target).SourceName; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_RelativeSource_AncestorType()
        {
            Type type = typeof(System.Windows.Data.RelativeSource);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.RelativeSource)), // DeclaringType
                            "AncestorType", // Name
                            typeof(System.Type), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Type);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.RelativeSource)target).AncestorType = (System.Type)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.RelativeSource)target).AncestorType; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_Uid()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.UidProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "Uid", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkContentElement_Name()
        {
            Type type = typeof(System.Windows.FrameworkContentElement);
            DependencyProperty  dp = System.Windows.FrameworkContentElement.NameProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkContentElement)), // DeclaringType
                            "Name", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkContentElement_Resources()
        {
            Type type = typeof(System.Windows.FrameworkContentElement);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkContentElement)), // DeclaringType
                            "Resources", // Name
                            typeof(System.Windows.ResourceDictionary), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.FrameworkContentElement)target).Resources = (System.Windows.ResourceDictionary)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.FrameworkContentElement)target).Resources; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Style_Resources()
        {
            Type type = typeof(System.Windows.Style);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Style)), // DeclaringType
                            "Resources", // Name
                            typeof(System.Windows.ResourceDictionary), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Style)target).Resources = (System.Windows.ResourceDictionary)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Style)target).Resources; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkTemplate_Resources()
        {
            Type type = typeof(System.Windows.FrameworkTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkTemplate)), // DeclaringType
                            "Resources", // Name
                            typeof(System.Windows.ResourceDictionary), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.FrameworkTemplate)target).Resources = (System.Windows.ResourceDictionary)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.FrameworkTemplate)target).Resources; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Application_Resources()
        {
            Type type = typeof(System.Windows.Application);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Application)), // DeclaringType
                            "Resources", // Name
                            typeof(System.Windows.ResourceDictionary), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Application)target).Resources = (System.Windows.ResourceDictionary)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Application)target).Resources; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MultiBinding_Converter()
        {
            Type type = typeof(System.Windows.Data.MultiBinding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.MultiBinding)), // DeclaringType
                            "Converter", // Name
                            typeof(System.Windows.Data.IMultiValueConverter), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.MultiBinding)target).Converter = (System.Windows.Data.IMultiValueConverter)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.MultiBinding)target).Converter; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MultiBinding_ConverterParameter()
        {
            Type type = typeof(System.Windows.Data.MultiBinding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.MultiBinding)), // DeclaringType
                            "ConverterParameter", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.MultiBinding)target).ConverterParameter = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.MultiBinding)target).ConverterParameter; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_LinearGradientBrush_StartPoint()
        {
            Type type = typeof(System.Windows.Media.LinearGradientBrush);
            DependencyProperty  dp = System.Windows.Media.LinearGradientBrush.StartPointProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.LinearGradientBrush)), // DeclaringType
                            "StartPoint", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.PointConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_LinearGradientBrush_EndPoint()
        {
            Type type = typeof(System.Windows.Media.LinearGradientBrush);
            DependencyProperty  dp = System.Windows.Media.LinearGradientBrush.EndPointProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.LinearGradientBrush)), // DeclaringType
                            "EndPoint", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.PointConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_CommandBinding_Command()
        {
            Type type = typeof(System.Windows.Input.CommandBinding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Input.CommandBinding)), // DeclaringType
                            "Command", // Name
                            typeof(System.Windows.Input.ICommand), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.CommandConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Input.CommandBinding)target).Command = (System.Windows.Input.ICommand)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Input.CommandBinding)target).Command; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Condition_Property()
        {
            Type type = typeof(System.Windows.Condition);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Condition)), // DeclaringType
                            "Property", // Name
                            typeof(System.Windows.DependencyProperty), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Markup.DependencyPropertyConverter);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Condition)target).Property = (System.Windows.DependencyProperty)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Condition)target).Property; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Condition_Value()
        {
            Type type = typeof(System.Windows.Condition);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Condition)), // DeclaringType
                            "Value", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Markup.SetterTriggerConditionValueConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Condition)target).Value = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Condition)target).Value; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Condition_Binding()
        {
            Type type = typeof(System.Windows.Condition);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Condition)), // DeclaringType
                            "Binding", // Name
                            typeof(System.Windows.Data.BindingBase), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Condition)target).Binding = (System.Windows.Data.BindingBase)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Condition)target).Binding; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BindingBase_FallbackValue()
        {
            Type type = typeof(System.Windows.Data.BindingBase);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.BindingBase)), // DeclaringType
                            "FallbackValue", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.BindingBase)target).FallbackValue = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.BindingBase)target).FallbackValue; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Window_ResizeMode()
        {
            Type type = typeof(System.Windows.Window);
            DependencyProperty  dp = System.Windows.Window.ResizeModeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Window)), // DeclaringType
                            "ResizeMode", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.ResizeMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Window_WindowState()
        {
            Type type = typeof(System.Windows.Window);
            DependencyProperty  dp = System.Windows.Window.WindowStateProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Window)), // DeclaringType
                            "WindowState", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.WindowState);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Window_Title()
        {
            Type type = typeof(System.Windows.Window);
            DependencyProperty  dp = System.Windows.Window.TitleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Window)), // DeclaringType
                            "Title", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_StrokeLineJoin()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.StrokeLineJoinProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "StrokeLineJoin", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.PenLineJoin);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_StrokeStartLineCap()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.StrokeStartLineCapProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "StrokeStartLineCap", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.PenLineCap);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_StrokeEndLineCap()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.StrokeEndLineCapProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "StrokeEndLineCap", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.PenLineCap);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TileBrush_TileMode()
        {
            Type type = typeof(System.Windows.Media.TileBrush);
            DependencyProperty  dp = System.Windows.Media.TileBrush.TileModeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.TileBrush)), // DeclaringType
                            "TileMode", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.TileMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TileBrush_ViewboxUnits()
        {
            Type type = typeof(System.Windows.Media.TileBrush);
            DependencyProperty  dp = System.Windows.Media.TileBrush.ViewboxUnitsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.TileBrush)), // DeclaringType
                            "ViewboxUnits", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushMappingMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TileBrush_ViewportUnits()
        {
            Type type = typeof(System.Windows.Media.TileBrush);
            DependencyProperty  dp = System.Windows.Media.TileBrush.ViewportUnitsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.TileBrush)), // DeclaringType
                            "ViewportUnits", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushMappingMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GeometryDrawing_Pen()
        {
            Type type = typeof(System.Windows.Media.GeometryDrawing);
            DependencyProperty  dp = System.Windows.Media.GeometryDrawing.PenProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.GeometryDrawing)), // DeclaringType
                            "Pen", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBox_TextWrapping()
        {
            Type type = typeof(System.Windows.Controls.TextBox);
            DependencyProperty  dp = System.Windows.Controls.TextBox.TextWrappingProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBox)), // DeclaringType
                            "TextWrapping", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.TextWrapping);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_StackPanel_Orientation()
        {
            Type type = typeof(System.Windows.Controls.StackPanel);
            DependencyProperty  dp = System.Windows.Controls.StackPanel.OrientationProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.StackPanel)), // DeclaringType
                            "Orientation", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.Orientation);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_Thumb()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "Thumb", // Name
                            typeof(System.Windows.Controls.Primitives.Thumb), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Primitives.Track)target).Thumb = (System.Windows.Controls.Primitives.Thumb)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.Track)target).Thumb; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_IncreaseRepeatButton()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "IncreaseRepeatButton", // Name
                            typeof(System.Windows.Controls.Primitives.RepeatButton), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Primitives.Track)target).IncreaseRepeatButton = (System.Windows.Controls.Primitives.RepeatButton)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.Track)target).IncreaseRepeatButton; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Track_DecreaseRepeatButton()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Track);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Track)), // DeclaringType
                            "DecreaseRepeatButton", // Name
                            typeof(System.Windows.Controls.Primitives.RepeatButton), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Primitives.Track)target).DecreaseRepeatButton = (System.Windows.Controls.Primitives.RepeatButton)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.Track)target).DecreaseRepeatButton; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_EventTrigger_RoutedEvent()
        {
            Type type = typeof(System.Windows.EventTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.EventTrigger)), // DeclaringType
                            "RoutedEvent", // Name
                            typeof(System.Windows.RoutedEvent), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Markup.RoutedEventConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.EventTrigger)target).RoutedEvent = (System.Windows.RoutedEvent)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.EventTrigger)target).RoutedEvent; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_InputBinding_Command()
        {
            Type type = typeof(System.Windows.Input.InputBinding);
            DependencyProperty  dp = System.Windows.Input.InputBinding.CommandProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Input.InputBinding)), // DeclaringType
                            "Command", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.CommandConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_KeyBinding_Gesture()
        {
            Type type = typeof(System.Windows.Input.KeyBinding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Input.KeyBinding)), // DeclaringType
                            "Gesture", // Name
                            typeof(System.Windows.Input.InputGesture), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.KeyGestureConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Input.KeyBinding)target).Gesture = (System.Windows.Input.InputGesture)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Input.KeyBinding)target).Gesture; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBox_TextAlignment()
        {
            Type type = typeof(System.Windows.Controls.TextBox);
            DependencyProperty  dp = System.Windows.Controls.TextBox.TextAlignmentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBox)), // DeclaringType
                            "TextAlignment", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.TextAlignment);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBlock_TextAlignment()
        {
            Type type = typeof(System.Windows.Controls.TextBlock);
            DependencyProperty  dp = System.Windows.Controls.TextBlock.TextAlignmentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.TextBlock)), // DeclaringType
                            "TextAlignment", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.TextAlignment);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_JournalEntryUnifiedViewConverter_JournalEntryPosition()
        {
            Type type = typeof(System.Windows.Navigation.JournalEntryUnifiedViewConverter);
            DependencyProperty  dp = System.Windows.Navigation.JournalEntryUnifiedViewConverter.JournalEntryPositionProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Navigation.JournalEntryUnifiedViewConverter)), // DeclaringType
                            "JournalEntryPosition", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Navigation.JournalEntryPosition);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_GradientBrush_MappingMode()
        {
            Type type = typeof(System.Windows.Media.GradientBrush);
            DependencyProperty  dp = System.Windows.Media.GradientBrush.MappingModeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.GradientBrush)), // DeclaringType
                            "MappingMode", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.BrushMappingMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MenuItem_Role()
        {
            Type type = typeof(System.Windows.Controls.MenuItem);
            DependencyProperty  dp = System.Windows.Controls.MenuItem.RoleProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.MenuItem)), // DeclaringType
                            "Role", // Name
                             dp, // DependencyProperty
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.MenuItemRole);
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DataTrigger_Value()
        {
            Type type = typeof(System.Windows.DataTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.DataTrigger)), // DeclaringType
                            "Value", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.DataTrigger)target).Value = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.DataTrigger)target).Value; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DataTrigger_Binding()
        {
            Type type = typeof(System.Windows.DataTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.DataTrigger)), // DeclaringType
                            "Binding", // Name
                            typeof(System.Windows.Data.BindingBase), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.DataTrigger)target).Binding = (System.Windows.Data.BindingBase)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.DataTrigger)target).Binding; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Setter_Property()
        {
            Type type = typeof(System.Windows.Setter);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Setter)), // DeclaringType
                            "Property", // Name
                            typeof(System.Windows.DependencyProperty), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Markup.DependencyPropertyConverter);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Setter)target).Property = (System.Windows.DependencyProperty)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Setter)target).Property; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ResourceDictionary_Source()
        {
            Type type = typeof(System.Windows.ResourceDictionary);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.ResourceDictionary)), // DeclaringType
                            "Source", // Name
                            typeof(System.Uri), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.UriTypeConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.ResourceDictionary)target).Source = (System.Uri)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.ResourceDictionary)target).Source; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BeginStoryboard_Name()
        {
            Type type = typeof(System.Windows.Media.Animation.BeginStoryboard);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.BeginStoryboard)), // DeclaringType
                            "Name", // Name
                            typeof(System.String), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Media.Animation.BeginStoryboard)target).Name = (System.String)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Media.Animation.BeginStoryboard)target).Name; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ResourceDictionary_MergedDictionaries()
        {
            Type type = typeof(System.Windows.ResourceDictionary);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.ResourceDictionary)), // DeclaringType
                            "MergedDictionaries", // Name
                            typeof(System.Collections.ObjectModel.Collection<System.Windows.ResourceDictionary>), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.ResourceDictionary)target).MergedDictionaries; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_KeyboardNavigation_DirectionalNavigation()
        {
            Type type = typeof(System.Windows.Input.KeyboardNavigation);
            DependencyProperty  dp = System.Windows.Input.KeyboardNavigation.DirectionalNavigationProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Input.KeyboardNavigation)), // DeclaringType
                            "DirectionalNavigation", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.KeyboardNavigationMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_KeyboardNavigation_TabNavigation()
        {
            Type type = typeof(System.Windows.Input.KeyboardNavigation);
            DependencyProperty  dp = System.Windows.Input.KeyboardNavigation.TabNavigationProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Input.KeyboardNavigation)), // DeclaringType
                            "TabNavigation", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.KeyboardNavigationMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ScrollBar_Orientation()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ScrollBar);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ScrollBar.OrientationProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ScrollBar)), // DeclaringType
                            "Orientation", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.Orientation);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Trigger_Property()
        {
            Type type = typeof(System.Windows.Trigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Trigger)), // DeclaringType
                            "Property", // Name
                            typeof(System.Windows.DependencyProperty), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Markup.DependencyPropertyConverter);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Trigger)target).Property = (System.Windows.DependencyProperty)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Trigger)target).Property; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_EventTrigger_SourceName()
        {
            Type type = typeof(System.Windows.EventTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.EventTrigger)), // DeclaringType
                            "SourceName", // Name
                            typeof(System.String), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.EventTrigger)target).SourceName = (System.String)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.EventTrigger)target).SourceName; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DefinitionBase_SharedSizeGroup()
        {
            Type type = typeof(System.Windows.Controls.DefinitionBase);
            DependencyProperty  dp = System.Windows.Controls.DefinitionBase.SharedSizeGroupProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.DefinitionBase)), // DeclaringType
                            "SharedSizeGroup", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToolTipService_ToolTip()
        {
            Type type = typeof(System.Windows.Controls.ToolTipService);
            DependencyProperty  dp = System.Windows.Controls.ToolTipService.ToolTipProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ToolTipService)), // DeclaringType
                            "ToolTip", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PathFigure_IsClosed()
        {
            Type type = typeof(System.Windows.Media.PathFigure);
            DependencyProperty  dp = System.Windows.Media.PathFigure.IsClosedProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.PathFigure)), // DeclaringType
                            "IsClosed", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PathFigure_IsFilled()
        {
            Type type = typeof(System.Windows.Media.PathFigure);
            DependencyProperty  dp = System.Windows.Media.PathFigure.IsFilledProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.PathFigure)), // DeclaringType
                            "IsFilled", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ButtonBase_ClickMode()
        {
            Type type = typeof(System.Windows.Controls.Primitives.ButtonBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.ButtonBase.ClickModeProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.ButtonBase)), // DeclaringType
                            "ClickMode", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.ClickMode);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Block_TextAlignment()
        {
            Type type = typeof(System.Windows.Documents.Block);
            DependencyProperty  dp = System.Windows.Documents.Block.TextAlignmentProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Documents.Block)), // DeclaringType
                            "TextAlignment", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.TextAlignment);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_RenderTransformOrigin()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.RenderTransformOriginProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "RenderTransformOrigin", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.PointConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Pen_LineJoin()
        {
            Type type = typeof(System.Windows.Media.Pen);
            DependencyProperty  dp = System.Windows.Media.Pen.LineJoinProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Pen)), // DeclaringType
                            "LineJoin", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.PenLineJoin);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_BulletDecorator_Bullet()
        {
            Type type = typeof(System.Windows.Controls.Primitives.BulletDecorator);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.BulletDecorator)), // DeclaringType
                            "Bullet", // Name
                            typeof(System.Windows.UIElement), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Controls.Primitives.BulletDecorator)target).Bullet = (System.Windows.UIElement)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Controls.Primitives.BulletDecorator)target).Bullet; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_SnapsToDevicePixels()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.SnapsToDevicePixelsProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "SnapsToDevicePixels", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_CommandBindings()
        {
            Type type = typeof(System.Windows.UIElement);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "CommandBindings", // Name
                            typeof(System.Windows.Input.CommandBindingCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.UIElement)target).CommandBindings; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_InputBindings()
        {
            Type type = typeof(System.Windows.UIElement);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "InputBindings", // Name
                            typeof(System.Windows.Input.InputBindingCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.UIElement)target).InputBindings; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_SolidColorBrush_Color()
        {
            Type type = typeof(System.Windows.Media.SolidColorBrush);
            DependencyProperty  dp = System.Windows.Media.SolidColorBrush.ColorProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.SolidColorBrush)), // DeclaringType
                            "Color", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.ColorConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Brush_Opacity()
        {
            Type type = typeof(System.Windows.Media.Brush);
            DependencyProperty  dp = System.Windows.Media.Brush.OpacityProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Brush)), // DeclaringType
                            "Opacity", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.DoubleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBoxBase_AcceptsTab()
        {
            Type type = typeof(System.Windows.Controls.Primitives.TextBoxBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.TextBoxBase.AcceptsTabProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.TextBoxBase)), // DeclaringType
                            "AcceptsTab", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_PathSegment_IsStroked()
        {
            Type type = typeof(System.Windows.Media.PathSegment);
            DependencyProperty  dp = System.Windows.Media.PathSegment.IsStrokedProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.PathSegment)), // DeclaringType
                            "IsStroked", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_VirtualizingPanel_IsVirtualizing()
        {
            Type type = typeof(System.Windows.Controls.VirtualizingPanel);
            DependencyProperty  dp = System.Windows.Controls.VirtualizingPanel.IsVirtualizingProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.VirtualizingPanel)), // DeclaringType
                            "IsVirtualizing", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_Stretch()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.StretchProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "Stretch", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Media.Stretch);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Frame_JournalOwnership()
        {
            Type type = typeof(System.Windows.Controls.Frame);
            DependencyProperty  dp = System.Windows.Controls.Frame.JournalOwnershipProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Frame)), // DeclaringType
                            "JournalOwnership", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Navigation.JournalOwnership);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Frame_NavigationUIVisibility()
        {
            Type type = typeof(System.Windows.Controls.Frame);
            DependencyProperty  dp = System.Windows.Controls.Frame.NavigationUIVisibilityProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Frame)), // DeclaringType
                            "NavigationUIVisibility", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Navigation.NavigationUIVisibility);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Storyboard_TargetName()
        {
            Type type = typeof(System.Windows.Media.Animation.Storyboard);
            DependencyProperty  dp = System.Windows.Media.Animation.Storyboard.TargetNameProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Storyboard)), // DeclaringType
                            "TargetName", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_XmlDataProvider_XPath()
        {
            Type type = typeof(System.Windows.Data.XmlDataProvider);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.XmlDataProvider)), // DeclaringType
                            "XPath", // Name
                            typeof(System.String), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.XmlDataProvider)target).XPath = (System.String)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.XmlDataProvider)target).XPath; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Selector_IsSelected()
        {
            Type type = typeof(System.Windows.Controls.Primitives.Selector);
            DependencyProperty  dp = System.Windows.Controls.Primitives.Selector.IsSelectedProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.Selector)), // DeclaringType
                            "IsSelected", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_DataTemplate_DataType()
        {
            Type type = typeof(System.Windows.DataTemplate);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.DataTemplate)), // DeclaringType
                            "DataType", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.Ambient = true;
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.DataTemplate)target).DataType = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.DataTemplate)target).DataType; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Shape_StrokeMiterLimit()
        {
            Type type = typeof(System.Windows.Shapes.Shape);
            DependencyProperty  dp = System.Windows.Shapes.Shape.StrokeMiterLimitProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Shapes.Shape)), // DeclaringType
                            "StrokeMiterLimit", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.DoubleConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_UIElement_AllowDrop()
        {
            Type type = typeof(System.Windows.UIElement);
            DependencyProperty  dp = System.Windows.UIElement.AllowDropProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.UIElement)), // DeclaringType
                            "AllowDrop", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MenuItem_IsChecked()
        {
            Type type = typeof(System.Windows.Controls.MenuItem);
            DependencyProperty  dp = System.Windows.Controls.MenuItem.IsCheckedProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.MenuItem)), // DeclaringType
                            "IsChecked", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Panel_IsItemsHost()
        {
            Type type = typeof(System.Windows.Controls.Panel);
            DependencyProperty  dp = System.Windows.Controls.Panel.IsItemsHostProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Panel)), // DeclaringType
                            "IsItemsHost", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_XPath()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "XPath", // Name
                            typeof(System.String), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).XPath = (System.String)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).XPath; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Window_AllowsTransparency()
        {
            Type type = typeof(System.Windows.Window);
            DependencyProperty  dp = System.Windows.Window.AllowsTransparencyProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Window)), // DeclaringType
                            "AllowsTransparency", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ObjectDataProvider_ObjectType()
        {
            Type type = typeof(System.Windows.Data.ObjectDataProvider);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.ObjectDataProvider)), // DeclaringType
                            "ObjectType", // Name
                            typeof(System.Type), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Type);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.ObjectDataProvider)target).ObjectType = (System.Type)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.ObjectDataProvider)target).ObjectType; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_ToolBar_Orientation()
        {
            Type type = typeof(System.Windows.Controls.ToolBar);
            DependencyProperty  dp = System.Windows.Controls.ToolBar.OrientationProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.ToolBar)), // DeclaringType
                            "Orientation", // Name
                             dp, // DependencyProperty
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.Orientation);
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBoxBase_VerticalScrollBarVisibility()
        {
            Type type = typeof(System.Windows.Controls.Primitives.TextBoxBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.TextBoxBase.VerticalScrollBarVisibilityProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.TextBoxBase)), // DeclaringType
                            "VerticalScrollBarVisibility", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.ScrollBarVisibility);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_TextBoxBase_HorizontalScrollBarVisibility()
        {
            Type type = typeof(System.Windows.Controls.Primitives.TextBoxBase);
            DependencyProperty  dp = System.Windows.Controls.Primitives.TextBoxBase.HorizontalScrollBarVisibilityProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Primitives.TextBoxBase)), // DeclaringType
                            "HorizontalScrollBarVisibility", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Controls.ScrollBarVisibility);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_FrameworkElement_Triggers()
        {
            Type type = typeof(System.Windows.FrameworkElement);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.FrameworkElement)), // DeclaringType
                            "Triggers", // Name
                            typeof(System.Windows.TriggerCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.FrameworkElement)target).Triggers; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_MultiDataTrigger_Conditions()
        {
            Type type = typeof(System.Windows.MultiDataTrigger);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.MultiDataTrigger)), // DeclaringType
                            "Conditions", // Name
                            typeof(System.Windows.ConditionCollection), // type
                            true, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.MultiDataTrigger)target).Conditions; };
            bamlMember.IsWritePrivate = true;
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_KeyBinding_Key()
        {
            Type type = typeof(System.Windows.Input.KeyBinding);
            DependencyProperty  dp = System.Windows.Input.KeyBinding.KeyProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Input.KeyBinding)), // DeclaringType
                            "Key", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.Input.KeyConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Binding_ConverterParameter()
        {
            Type type = typeof(System.Windows.Data.Binding);
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Data.Binding)), // DeclaringType
                            "ConverterParameter", // Name
                            typeof(System.Object), // type
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.HasSpecialTypeConverter = true;
            bamlMember.TypeConverterType = typeof(System.Object);
            bamlMember.SetDelegate = delegate(object target, object value) { ((System.Windows.Data.Binding)target).ConverterParameter = (System.Object)value; };
            bamlMember.GetDelegate = delegate(object target) { return ((System.Windows.Data.Binding)target).ConverterParameter; };
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Canvas_Top()
        {
            Type type = typeof(System.Windows.Controls.Canvas);
            DependencyProperty  dp = System.Windows.Controls.Canvas.TopProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Canvas)), // DeclaringType
                            "Top", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Canvas_Left()
        {
            Type type = typeof(System.Windows.Controls.Canvas);
            DependencyProperty  dp = System.Windows.Controls.Canvas.LeftProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Canvas)), // DeclaringType
                            "Left", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Canvas_Bottom()
        {
            Type type = typeof(System.Windows.Controls.Canvas);
            DependencyProperty  dp = System.Windows.Controls.Canvas.BottomProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Canvas)), // DeclaringType
                            "Bottom", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Canvas_Right()
        {
            Type type = typeof(System.Windows.Controls.Canvas);
            DependencyProperty  dp = System.Windows.Controls.Canvas.RightProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Controls.Canvas)), // DeclaringType
                            "Right", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.LengthConverter);
            bamlMember.Freeze();
            return bamlMember;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownMember Create_BamlProperty_Storyboard_TargetProperty()
        {
            Type type = typeof(System.Windows.Media.Animation.Storyboard);
            DependencyProperty  dp = System.Windows.Media.Animation.Storyboard.TargetPropertyProperty;
            var bamlMember = new WpfKnownMember( this,  // Schema Context
                            this.GetXamlType(typeof(System.Windows.Media.Animation.Storyboard)), // DeclaringType
                            "TargetProperty", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(System.Windows.PropertyPathConverter);
            bamlMember.Freeze();
            return bamlMember;
        }
    }
}
