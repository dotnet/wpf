// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xaml;
using System.Xaml.Schema;
using System.Collections.Generic;

namespace System.Windows.Baml2006
{
    partial class WpfSharedBamlSchemaContext : XamlSchemaContext
    {
        const int KnownTypeCount = 759;


        private WpfKnownType CreateKnownBamlType(short bamlNumber, bool isBamlType, bool useV3Rules)
        {
            switch (bamlNumber)
            {
                case 1: return Create_BamlType_AccessText(isBamlType, useV3Rules);
                case 2: return Create_BamlType_AdornedElementPlaceholder(isBamlType, useV3Rules);
                case 3: return Create_BamlType_Adorner(isBamlType, useV3Rules);
                case 4: return Create_BamlType_AdornerDecorator(isBamlType, useV3Rules);
                case 5: return Create_BamlType_AdornerLayer(isBamlType, useV3Rules);
                case 6: return Create_BamlType_AffineTransform3D(isBamlType, useV3Rules);
                case 7: return Create_BamlType_AmbientLight(isBamlType, useV3Rules);
                case 8: return Create_BamlType_AnchoredBlock(isBamlType, useV3Rules);
                case 9: return Create_BamlType_Animatable(isBamlType, useV3Rules);
                case 10: return Create_BamlType_AnimationClock(isBamlType, useV3Rules);
                case 11: return Create_BamlType_AnimationTimeline(isBamlType, useV3Rules);
                case 12: return Create_BamlType_Application(isBamlType, useV3Rules);
                case 13: return Create_BamlType_ArcSegment(isBamlType, useV3Rules);
                case 14: return Create_BamlType_ArrayExtension(isBamlType, useV3Rules);
                case 15: return Create_BamlType_AxisAngleRotation3D(isBamlType, useV3Rules);
                case 16: return Create_BamlType_BaseIListConverter(isBamlType, useV3Rules); // type converter
                case 17: return Create_BamlType_BeginStoryboard(isBamlType, useV3Rules);
                case 18: return Create_BamlType_BevelBitmapEffect(isBamlType, useV3Rules);
                case 19: return Create_BamlType_BezierSegment(isBamlType, useV3Rules);
                case 20: return Create_BamlType_Binding(isBamlType, useV3Rules);
                case 21: return Create_BamlType_BindingBase(isBamlType, useV3Rules);
                case 22: return Create_BamlType_BindingExpression(isBamlType, useV3Rules);
                case 23: return Create_BamlType_BindingExpressionBase(isBamlType, useV3Rules);
                case 24: return Create_BamlType_BindingListCollectionView(isBamlType, useV3Rules);
                case 25: return Create_BamlType_BitmapDecoder(isBamlType, useV3Rules);
                case 26: return Create_BamlType_BitmapEffect(isBamlType, useV3Rules);
                case 27: return Create_BamlType_BitmapEffectCollection(isBamlType, useV3Rules);
                case 28: return Create_BamlType_BitmapEffectGroup(isBamlType, useV3Rules);
                case 29: return Create_BamlType_BitmapEffectInput(isBamlType, useV3Rules);
                case 30: return Create_BamlType_BitmapEncoder(isBamlType, useV3Rules);
                case 31: return Create_BamlType_BitmapFrame(isBamlType, useV3Rules);
                case 32: return Create_BamlType_BitmapImage(isBamlType, useV3Rules);
                case 33: return Create_BamlType_BitmapMetadata(isBamlType, useV3Rules);
                case 34: return Create_BamlType_BitmapPalette(isBamlType, useV3Rules);
                case 35: return Create_BamlType_BitmapSource(isBamlType, useV3Rules);
                case 36: return Create_BamlType_Block(isBamlType, useV3Rules);
                case 37: return Create_BamlType_BlockUIContainer(isBamlType, useV3Rules);
                case 38: return Create_BamlType_BlurBitmapEffect(isBamlType, useV3Rules);
                case 39: return Create_BamlType_BmpBitmapDecoder(isBamlType, useV3Rules);
                case 40: return Create_BamlType_BmpBitmapEncoder(isBamlType, useV3Rules);
                case 41: return Create_BamlType_Bold(isBamlType, useV3Rules);
                case 42: return Create_BamlType_BoolIListConverter(isBamlType, useV3Rules); // type converter
                case 43: return Create_BamlType_Boolean(isBamlType, useV3Rules);
                case 44: return Create_BamlType_BooleanAnimationBase(isBamlType, useV3Rules);
                case 45: return Create_BamlType_BooleanAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 46: return Create_BamlType_BooleanConverter(isBamlType, useV3Rules); // type converter
                case 47: return Create_BamlType_BooleanKeyFrame(isBamlType, useV3Rules);
                case 48: return Create_BamlType_BooleanKeyFrameCollection(isBamlType, useV3Rules);
                case 49: return Create_BamlType_BooleanToVisibilityConverter(isBamlType, useV3Rules);
                case 50: return Create_BamlType_Border(isBamlType, useV3Rules);
                case 51: return Create_BamlType_BorderGapMaskConverter(isBamlType, useV3Rules);
                case 52: return Create_BamlType_Brush(isBamlType, useV3Rules);
                case 53: return Create_BamlType_BrushConverter(isBamlType, useV3Rules); // type converter
                case 54: return Create_BamlType_BulletDecorator(isBamlType, useV3Rules);
                case 55: return Create_BamlType_Button(isBamlType, useV3Rules);
                case 56: return Create_BamlType_ButtonBase(isBamlType, useV3Rules);
                case 57: return Create_BamlType_Byte(isBamlType, useV3Rules);
                case 58: return Create_BamlType_ByteAnimation(isBamlType, useV3Rules);
                case 59: return Create_BamlType_ByteAnimationBase(isBamlType, useV3Rules);
                case 60: return Create_BamlType_ByteAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 61: return Create_BamlType_ByteConverter(isBamlType, useV3Rules); // type converter
                case 62: return Create_BamlType_ByteKeyFrame(isBamlType, useV3Rules);
                case 63: return Create_BamlType_ByteKeyFrameCollection(isBamlType, useV3Rules);
                case 64: return Create_BamlType_CachedBitmap(isBamlType, useV3Rules);
                case 65: return Create_BamlType_Camera(isBamlType, useV3Rules);
                case 66: return Create_BamlType_Canvas(isBamlType, useV3Rules);
                case 67: return Create_BamlType_Char(isBamlType, useV3Rules);
                case 68: return Create_BamlType_CharAnimationBase(isBamlType, useV3Rules);
                case 69: return Create_BamlType_CharAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 70: return Create_BamlType_CharConverter(isBamlType, useV3Rules); // type converter
                case 71: return Create_BamlType_CharIListConverter(isBamlType, useV3Rules); // type converter
                case 72: return Create_BamlType_CharKeyFrame(isBamlType, useV3Rules);
                case 73: return Create_BamlType_CharKeyFrameCollection(isBamlType, useV3Rules);
                case 74: return Create_BamlType_CheckBox(isBamlType, useV3Rules);
                case 75: return Create_BamlType_Clock(isBamlType, useV3Rules);
                case 76: return Create_BamlType_ClockController(isBamlType, useV3Rules);
                case 77: return Create_BamlType_ClockGroup(isBamlType, useV3Rules);
                case 78: return Create_BamlType_CollectionContainer(isBamlType, useV3Rules);
                case 79: return Create_BamlType_CollectionView(isBamlType, useV3Rules);
                case 80: return Create_BamlType_CollectionViewSource(isBamlType, useV3Rules);
                case 81: return Create_BamlType_Color(isBamlType, useV3Rules);
                case 82: return Create_BamlType_ColorAnimation(isBamlType, useV3Rules);
                case 83: return Create_BamlType_ColorAnimationBase(isBamlType, useV3Rules);
                case 84: return Create_BamlType_ColorAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 85: return Create_BamlType_ColorConvertedBitmap(isBamlType, useV3Rules);
                case 86: return Create_BamlType_ColorConvertedBitmapExtension(isBamlType, useV3Rules);
                case 87: return Create_BamlType_ColorConverter(isBamlType, useV3Rules); // type converter
                case 88: return Create_BamlType_ColorKeyFrame(isBamlType, useV3Rules);
                case 89: return Create_BamlType_ColorKeyFrameCollection(isBamlType, useV3Rules);
                case 90: return Create_BamlType_ColumnDefinition(isBamlType, useV3Rules);
                case 91: return Create_BamlType_CombinedGeometry(isBamlType, useV3Rules);
                case 92: return Create_BamlType_ComboBox(isBamlType, useV3Rules);
                case 93: return Create_BamlType_ComboBoxItem(isBamlType, useV3Rules);
                case 94: return Create_BamlType_CommandConverter(isBamlType, useV3Rules); // type converter
                case 95: return Create_BamlType_ComponentResourceKey(isBamlType, useV3Rules);
                case 96: return Create_BamlType_ComponentResourceKeyConverter(isBamlType, useV3Rules); // type converter
                case 97: return Create_BamlType_CompositionTarget(isBamlType, useV3Rules);
                case 98: return Create_BamlType_Condition(isBamlType, useV3Rules);
                case 99: return Create_BamlType_ContainerVisual(isBamlType, useV3Rules);
                case 100: return Create_BamlType_ContentControl(isBamlType, useV3Rules);
                case 101: return Create_BamlType_ContentElement(isBamlType, useV3Rules);
                case 102: return Create_BamlType_ContentPresenter(isBamlType, useV3Rules);
                case 103: return Create_BamlType_ContentPropertyAttribute(isBamlType, useV3Rules);
                case 104: return Create_BamlType_ContentWrapperAttribute(isBamlType, useV3Rules);
                case 105: return Create_BamlType_ContextMenu(isBamlType, useV3Rules);
                case 106: return Create_BamlType_ContextMenuService(isBamlType, useV3Rules);
                case 107: return Create_BamlType_Control(isBamlType, useV3Rules);
                case 108: return Create_BamlType_ControlTemplate(isBamlType, useV3Rules);
                case 109: return Create_BamlType_ControllableStoryboardAction(isBamlType, useV3Rules);
                case 110: return Create_BamlType_CornerRadius(isBamlType, useV3Rules);
                case 111: return Create_BamlType_CornerRadiusConverter(isBamlType, useV3Rules); // type converter
                case 112: return Create_BamlType_CroppedBitmap(isBamlType, useV3Rules);
                case 113: return Create_BamlType_CultureInfo(isBamlType, useV3Rules);
                case 114: return Create_BamlType_CultureInfoConverter(isBamlType, useV3Rules); // type converter
                case 115: return Create_BamlType_CultureInfoIetfLanguageTagConverter(isBamlType, useV3Rules); // type converter
                case 116: return Create_BamlType_Cursor(isBamlType, useV3Rules);
                case 117: return Create_BamlType_CursorConverter(isBamlType, useV3Rules); // type converter
                case 118: return Create_BamlType_DashStyle(isBamlType, useV3Rules);
                case 119: return Create_BamlType_DataChangedEventManager(isBamlType, useV3Rules);
                case 120: return Create_BamlType_DataTemplate(isBamlType, useV3Rules);
                case 121: return Create_BamlType_DataTemplateKey(isBamlType, useV3Rules);
                case 122: return Create_BamlType_DataTrigger(isBamlType, useV3Rules);
                case 123: return Create_BamlType_DateTime(isBamlType, useV3Rules);
                case 124: return Create_BamlType_DateTimeConverter(isBamlType, useV3Rules); // type converter
                case 125: return Create_BamlType_DateTimeConverter2(isBamlType, useV3Rules); // type converter
                case 126: return Create_BamlType_Decimal(isBamlType, useV3Rules);
                case 127: return Create_BamlType_DecimalAnimation(isBamlType, useV3Rules);
                case 128: return Create_BamlType_DecimalAnimationBase(isBamlType, useV3Rules);
                case 129: return Create_BamlType_DecimalAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 130: return Create_BamlType_DecimalConverter(isBamlType, useV3Rules); // type converter
                case 131: return Create_BamlType_DecimalKeyFrame(isBamlType, useV3Rules);
                case 132: return Create_BamlType_DecimalKeyFrameCollection(isBamlType, useV3Rules);
                case 133: return Create_BamlType_Decorator(isBamlType, useV3Rules);
                case 134: return Create_BamlType_DefinitionBase(isBamlType, useV3Rules);
                case 135: return Create_BamlType_DependencyObject(isBamlType, useV3Rules);
                case 136: return Create_BamlType_DependencyProperty(isBamlType, useV3Rules);
                case 137: return Create_BamlType_DependencyPropertyConverter(isBamlType, useV3Rules); // type converter
                case 138: return Create_BamlType_DialogResultConverter(isBamlType, useV3Rules); // type converter
                case 139: return Create_BamlType_DiffuseMaterial(isBamlType, useV3Rules);
                case 140: return Create_BamlType_DirectionalLight(isBamlType, useV3Rules);
                case 141: return Create_BamlType_DiscreteBooleanKeyFrame(isBamlType, useV3Rules);
                case 142: return Create_BamlType_DiscreteByteKeyFrame(isBamlType, useV3Rules);
                case 143: return Create_BamlType_DiscreteCharKeyFrame(isBamlType, useV3Rules);
                case 144: return Create_BamlType_DiscreteColorKeyFrame(isBamlType, useV3Rules);
                case 145: return Create_BamlType_DiscreteDecimalKeyFrame(isBamlType, useV3Rules);
                case 146: return Create_BamlType_DiscreteDoubleKeyFrame(isBamlType, useV3Rules);
                case 147: return Create_BamlType_DiscreteInt16KeyFrame(isBamlType, useV3Rules);
                case 148: return Create_BamlType_DiscreteInt32KeyFrame(isBamlType, useV3Rules);
                case 149: return Create_BamlType_DiscreteInt64KeyFrame(isBamlType, useV3Rules);
                case 150: return Create_BamlType_DiscreteMatrixKeyFrame(isBamlType, useV3Rules);
                case 151: return Create_BamlType_DiscreteObjectKeyFrame(isBamlType, useV3Rules);
                case 152: return Create_BamlType_DiscretePoint3DKeyFrame(isBamlType, useV3Rules);
                case 153: return Create_BamlType_DiscretePointKeyFrame(isBamlType, useV3Rules);
                case 154: return Create_BamlType_DiscreteQuaternionKeyFrame(isBamlType, useV3Rules);
                case 155: return Create_BamlType_DiscreteRectKeyFrame(isBamlType, useV3Rules);
                case 156: return Create_BamlType_DiscreteRotation3DKeyFrame(isBamlType, useV3Rules);
                case 157: return Create_BamlType_DiscreteSingleKeyFrame(isBamlType, useV3Rules);
                case 158: return Create_BamlType_DiscreteSizeKeyFrame(isBamlType, useV3Rules);
                case 159: return Create_BamlType_DiscreteStringKeyFrame(isBamlType, useV3Rules);
                case 160: return Create_BamlType_DiscreteThicknessKeyFrame(isBamlType, useV3Rules);
                case 161: return Create_BamlType_DiscreteVector3DKeyFrame(isBamlType, useV3Rules);
                case 162: return Create_BamlType_DiscreteVectorKeyFrame(isBamlType, useV3Rules);
                case 163: return Create_BamlType_DockPanel(isBamlType, useV3Rules);
                case 164: return Create_BamlType_DocumentPageView(isBamlType, useV3Rules);
                case 165: return Create_BamlType_DocumentReference(isBamlType, useV3Rules);
                case 166: return Create_BamlType_DocumentViewer(isBamlType, useV3Rules);
                case 167: return Create_BamlType_DocumentViewerBase(isBamlType, useV3Rules);
                case 168: return Create_BamlType_Double(isBamlType, useV3Rules);
                case 169: return Create_BamlType_DoubleAnimation(isBamlType, useV3Rules);
                case 170: return Create_BamlType_DoubleAnimationBase(isBamlType, useV3Rules);
                case 171: return Create_BamlType_DoubleAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 172: return Create_BamlType_DoubleAnimationUsingPath(isBamlType, useV3Rules);
                case 173: return Create_BamlType_DoubleCollection(isBamlType, useV3Rules);
                case 174: return Create_BamlType_DoubleCollectionConverter(isBamlType, useV3Rules); // type converter
                case 175: return Create_BamlType_DoubleConverter(isBamlType, useV3Rules); // type converter
                case 176: return Create_BamlType_DoubleIListConverter(isBamlType, useV3Rules); // type converter
                case 177: return Create_BamlType_DoubleKeyFrame(isBamlType, useV3Rules);
                case 178: return Create_BamlType_DoubleKeyFrameCollection(isBamlType, useV3Rules);
                case 179: return Create_BamlType_Drawing(isBamlType, useV3Rules);
                case 180: return Create_BamlType_DrawingBrush(isBamlType, useV3Rules);
                case 181: return Create_BamlType_DrawingCollection(isBamlType, useV3Rules);
                case 182: return Create_BamlType_DrawingContext(isBamlType, useV3Rules);
                case 183: return Create_BamlType_DrawingGroup(isBamlType, useV3Rules);
                case 184: return Create_BamlType_DrawingImage(isBamlType, useV3Rules);
                case 185: return Create_BamlType_DrawingVisual(isBamlType, useV3Rules);
                case 186: return Create_BamlType_DropShadowBitmapEffect(isBamlType, useV3Rules);
                case 187: return Create_BamlType_Duration(isBamlType, useV3Rules);
                case 188: return Create_BamlType_DurationConverter(isBamlType, useV3Rules); // type converter
                case 189: return Create_BamlType_DynamicResourceExtension(isBamlType, useV3Rules);
                case 190: return Create_BamlType_DynamicResourceExtensionConverter(isBamlType, useV3Rules); // type converter
                case 191: return Create_BamlType_Ellipse(isBamlType, useV3Rules);
                case 192: return Create_BamlType_EllipseGeometry(isBamlType, useV3Rules);
                case 193: return Create_BamlType_EmbossBitmapEffect(isBamlType, useV3Rules);
                case 194: return Create_BamlType_EmissiveMaterial(isBamlType, useV3Rules);
                case 195: return Create_BamlType_EnumConverter(isBamlType, useV3Rules); // type converter
                case 196: return Create_BamlType_EventManager(isBamlType, useV3Rules);
                case 197: return Create_BamlType_EventSetter(isBamlType, useV3Rules);
                case 198: return Create_BamlType_EventTrigger(isBamlType, useV3Rules);
                case 199: return Create_BamlType_Expander(isBamlType, useV3Rules);
                case 200: return Create_BamlType_Expression(isBamlType, useV3Rules);
                case 201: return Create_BamlType_ExpressionConverter(isBamlType, useV3Rules); // type converter
                case 202: return Create_BamlType_Figure(isBamlType, useV3Rules);
                case 203: return Create_BamlType_FigureLength(isBamlType, useV3Rules);
                case 204: return Create_BamlType_FigureLengthConverter(isBamlType, useV3Rules); // type converter
                case 205: return Create_BamlType_FixedDocument(isBamlType, useV3Rules);
                case 206: return Create_BamlType_FixedDocumentSequence(isBamlType, useV3Rules);
                case 207: return Create_BamlType_FixedPage(isBamlType, useV3Rules);
                case 208: return Create_BamlType_Floater(isBamlType, useV3Rules);
                case 209: return Create_BamlType_FlowDocument(isBamlType, useV3Rules);
                case 210: return Create_BamlType_FlowDocumentPageViewer(isBamlType, useV3Rules);
                case 211: return Create_BamlType_FlowDocumentReader(isBamlType, useV3Rules);
                case 212: return Create_BamlType_FlowDocumentScrollViewer(isBamlType, useV3Rules);
                case 213: return Create_BamlType_FocusManager(isBamlType, useV3Rules);
                case 214: return Create_BamlType_FontFamily(isBamlType, useV3Rules);
                case 215: return Create_BamlType_FontFamilyConverter(isBamlType, useV3Rules); // type converter
                case 216: return Create_BamlType_FontSizeConverter(isBamlType, useV3Rules); // type converter
                case 217: return Create_BamlType_FontStretch(isBamlType, useV3Rules);
                case 218: return Create_BamlType_FontStretchConverter(isBamlType, useV3Rules); // type converter
                case 219: return Create_BamlType_FontStyle(isBamlType, useV3Rules);
                case 220: return Create_BamlType_FontStyleConverter(isBamlType, useV3Rules); // type converter
                case 221: return Create_BamlType_FontWeight(isBamlType, useV3Rules);
                case 222: return Create_BamlType_FontWeightConverter(isBamlType, useV3Rules); // type converter
                case 223: return Create_BamlType_FormatConvertedBitmap(isBamlType, useV3Rules);
                case 224: return Create_BamlType_Frame(isBamlType, useV3Rules);
                case 225: return Create_BamlType_FrameworkContentElement(isBamlType, useV3Rules);
                case 226: return Create_BamlType_FrameworkElement(isBamlType, useV3Rules);
                case 227: return Create_BamlType_FrameworkElementFactory(isBamlType, useV3Rules);
                case 228: return Create_BamlType_FrameworkPropertyMetadata(isBamlType, useV3Rules);
                case 229: return Create_BamlType_FrameworkPropertyMetadataOptions(isBamlType, useV3Rules);
                case 230: return Create_BamlType_FrameworkRichTextComposition(isBamlType, useV3Rules);
                case 231: return Create_BamlType_FrameworkTemplate(isBamlType, useV3Rules);
                case 232: return Create_BamlType_FrameworkTextComposition(isBamlType, useV3Rules);
                case 233: return Create_BamlType_Freezable(isBamlType, useV3Rules);
                case 234: return Create_BamlType_GeneralTransform(isBamlType, useV3Rules);
                case 235: return Create_BamlType_GeneralTransformCollection(isBamlType, useV3Rules);
                case 236: return Create_BamlType_GeneralTransformGroup(isBamlType, useV3Rules);
                case 237: return Create_BamlType_Geometry(isBamlType, useV3Rules);
                case 238: return Create_BamlType_Geometry3D(isBamlType, useV3Rules);
                case 239: return Create_BamlType_GeometryCollection(isBamlType, useV3Rules);
                case 240: return Create_BamlType_GeometryConverter(isBamlType, useV3Rules); // type converter
                case 241: return Create_BamlType_GeometryDrawing(isBamlType, useV3Rules);
                case 242: return Create_BamlType_GeometryGroup(isBamlType, useV3Rules);
                case 243: return Create_BamlType_GeometryModel3D(isBamlType, useV3Rules);
                case 244: return Create_BamlType_GestureRecognizer(isBamlType, useV3Rules);
                case 245: return Create_BamlType_GifBitmapDecoder(isBamlType, useV3Rules);
                case 246: return Create_BamlType_GifBitmapEncoder(isBamlType, useV3Rules);
                case 247: return Create_BamlType_GlyphRun(isBamlType, useV3Rules);
                case 248: return Create_BamlType_GlyphRunDrawing(isBamlType, useV3Rules);
                case 249: return Create_BamlType_GlyphTypeface(isBamlType, useV3Rules);
                case 250: return Create_BamlType_Glyphs(isBamlType, useV3Rules);
                case 251: return Create_BamlType_GradientBrush(isBamlType, useV3Rules);
                case 252: return Create_BamlType_GradientStop(isBamlType, useV3Rules);
                case 253: return Create_BamlType_GradientStopCollection(isBamlType, useV3Rules);
                case 254: return Create_BamlType_Grid(isBamlType, useV3Rules);
                case 255: return Create_BamlType_GridLength(isBamlType, useV3Rules);
                case 256: return Create_BamlType_GridLengthConverter(isBamlType, useV3Rules); // type converter
                case 257: return Create_BamlType_GridSplitter(isBamlType, useV3Rules);
                case 258: return Create_BamlType_GridView(isBamlType, useV3Rules);
                case 259: return Create_BamlType_GridViewColumn(isBamlType, useV3Rules);
                case 260: return Create_BamlType_GridViewColumnHeader(isBamlType, useV3Rules);
                case 261: return Create_BamlType_GridViewHeaderRowPresenter(isBamlType, useV3Rules);
                case 262: return Create_BamlType_GridViewRowPresenter(isBamlType, useV3Rules);
                case 263: return Create_BamlType_GridViewRowPresenterBase(isBamlType, useV3Rules);
                case 264: return Create_BamlType_GroupBox(isBamlType, useV3Rules);
                case 265: return Create_BamlType_GroupItem(isBamlType, useV3Rules);
                case 266: return Create_BamlType_Guid(isBamlType, useV3Rules);
                case 267: return Create_BamlType_GuidConverter(isBamlType, useV3Rules); // type converter
                case 268: return Create_BamlType_GuidelineSet(isBamlType, useV3Rules);
                case 269: return Create_BamlType_HeaderedContentControl(isBamlType, useV3Rules);
                case 270: return Create_BamlType_HeaderedItemsControl(isBamlType, useV3Rules);
                case 271: return Create_BamlType_HierarchicalDataTemplate(isBamlType, useV3Rules);
                case 272: return Create_BamlType_HostVisual(isBamlType, useV3Rules);
                case 273: return Create_BamlType_Hyperlink(isBamlType, useV3Rules);
                case 274: return Create_BamlType_IAddChild(isBamlType, useV3Rules);
                case 275: return Create_BamlType_IAddChildInternal(isBamlType, useV3Rules);
                case 276: return Create_BamlType_ICommand(isBamlType, useV3Rules);
                case 277: return Create_BamlType_IComponentConnector(isBamlType, useV3Rules);
                case 278: return Create_BamlType_INameScope(isBamlType, useV3Rules);
                case 279: return Create_BamlType_IStyleConnector(isBamlType, useV3Rules);
                case 280: return Create_BamlType_IconBitmapDecoder(isBamlType, useV3Rules);
                case 281: return Create_BamlType_Image(isBamlType, useV3Rules);
                case 282: return Create_BamlType_ImageBrush(isBamlType, useV3Rules);
                case 283: return Create_BamlType_ImageDrawing(isBamlType, useV3Rules);
                case 284: return Create_BamlType_ImageMetadata(isBamlType, useV3Rules);
                case 285: return Create_BamlType_ImageSource(isBamlType, useV3Rules);
                case 286: return Create_BamlType_ImageSourceConverter(isBamlType, useV3Rules); // type converter
                case 287: return Create_BamlType_InPlaceBitmapMetadataWriter(isBamlType, useV3Rules);
                case 288: return Create_BamlType_InkCanvas(isBamlType, useV3Rules);
                case 289: return Create_BamlType_InkPresenter(isBamlType, useV3Rules);
                case 290: return Create_BamlType_Inline(isBamlType, useV3Rules);
                case 291: return Create_BamlType_InlineCollection(isBamlType, useV3Rules);
                case 292: return Create_BamlType_InlineUIContainer(isBamlType, useV3Rules);
                case 293: return Create_BamlType_InputBinding(isBamlType, useV3Rules);
                case 294: return Create_BamlType_InputDevice(isBamlType, useV3Rules);
                case 295: return Create_BamlType_InputLanguageManager(isBamlType, useV3Rules);
                case 296: return Create_BamlType_InputManager(isBamlType, useV3Rules);
                case 297: return Create_BamlType_InputMethod(isBamlType, useV3Rules);
                case 298: return Create_BamlType_InputScope(isBamlType, useV3Rules);
                case 299: return Create_BamlType_InputScopeConverter(isBamlType, useV3Rules); // type converter
                case 300: return Create_BamlType_InputScopeName(isBamlType, useV3Rules);
                case 301: return Create_BamlType_InputScopeNameConverter(isBamlType, useV3Rules); // type converter
                case 302: return Create_BamlType_Int16(isBamlType, useV3Rules);
                case 303: return Create_BamlType_Int16Animation(isBamlType, useV3Rules);
                case 304: return Create_BamlType_Int16AnimationBase(isBamlType, useV3Rules);
                case 305: return Create_BamlType_Int16AnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 306: return Create_BamlType_Int16Converter(isBamlType, useV3Rules); // type converter
                case 307: return Create_BamlType_Int16KeyFrame(isBamlType, useV3Rules);
                case 308: return Create_BamlType_Int16KeyFrameCollection(isBamlType, useV3Rules);
                case 309: return Create_BamlType_Int32(isBamlType, useV3Rules);
                case 310: return Create_BamlType_Int32Animation(isBamlType, useV3Rules);
                case 311: return Create_BamlType_Int32AnimationBase(isBamlType, useV3Rules);
                case 312: return Create_BamlType_Int32AnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 313: return Create_BamlType_Int32Collection(isBamlType, useV3Rules);
                case 314: return Create_BamlType_Int32CollectionConverter(isBamlType, useV3Rules); // type converter
                case 315: return Create_BamlType_Int32Converter(isBamlType, useV3Rules); // type converter
                case 316: return Create_BamlType_Int32KeyFrame(isBamlType, useV3Rules);
                case 317: return Create_BamlType_Int32KeyFrameCollection(isBamlType, useV3Rules);
                case 318: return Create_BamlType_Int32Rect(isBamlType, useV3Rules);
                case 319: return Create_BamlType_Int32RectConverter(isBamlType, useV3Rules); // type converter
                case 320: return Create_BamlType_Int64(isBamlType, useV3Rules);
                case 321: return Create_BamlType_Int64Animation(isBamlType, useV3Rules);
                case 322: return Create_BamlType_Int64AnimationBase(isBamlType, useV3Rules);
                case 323: return Create_BamlType_Int64AnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 324: return Create_BamlType_Int64Converter(isBamlType, useV3Rules); // type converter
                case 325: return Create_BamlType_Int64KeyFrame(isBamlType, useV3Rules);
                case 326: return Create_BamlType_Int64KeyFrameCollection(isBamlType, useV3Rules);
                case 327: return Create_BamlType_Italic(isBamlType, useV3Rules);
                case 328: return Create_BamlType_ItemCollection(isBamlType, useV3Rules);
                case 329: return Create_BamlType_ItemsControl(isBamlType, useV3Rules);
                case 330: return Create_BamlType_ItemsPanelTemplate(isBamlType, useV3Rules);
                case 331: return Create_BamlType_ItemsPresenter(isBamlType, useV3Rules);
                case 332: return Create_BamlType_JournalEntry(isBamlType, useV3Rules);
                case 333: return Create_BamlType_JournalEntryListConverter(isBamlType, useV3Rules);
                case 334: return Create_BamlType_JournalEntryUnifiedViewConverter(isBamlType, useV3Rules);
                case 335: return Create_BamlType_JpegBitmapDecoder(isBamlType, useV3Rules);
                case 336: return Create_BamlType_JpegBitmapEncoder(isBamlType, useV3Rules);
                case 337: return Create_BamlType_KeyBinding(isBamlType, useV3Rules);
                case 338: return Create_BamlType_KeyConverter(isBamlType, useV3Rules); // type converter
                case 339: return Create_BamlType_KeyGesture(isBamlType, useV3Rules);
                case 340: return Create_BamlType_KeyGestureConverter(isBamlType, useV3Rules); // type converter
                case 341: return Create_BamlType_KeySpline(isBamlType, useV3Rules);
                case 342: return Create_BamlType_KeySplineConverter(isBamlType, useV3Rules); // type converter
                case 343: return Create_BamlType_KeyTime(isBamlType, useV3Rules);
                case 344: return Create_BamlType_KeyTimeConverter(isBamlType, useV3Rules); // type converter
                case 345: return Create_BamlType_KeyboardDevice(isBamlType, useV3Rules);
                case 346: return Create_BamlType_Label(isBamlType, useV3Rules);
                case 347: return Create_BamlType_LateBoundBitmapDecoder(isBamlType, useV3Rules);
                case 348: return Create_BamlType_LengthConverter(isBamlType, useV3Rules); // type converter
                case 349: return Create_BamlType_Light(isBamlType, useV3Rules);
                case 350: return Create_BamlType_Line(isBamlType, useV3Rules);
                case 351: return Create_BamlType_LineBreak(isBamlType, useV3Rules);
                case 352: return Create_BamlType_LineGeometry(isBamlType, useV3Rules);
                case 353: return Create_BamlType_LineSegment(isBamlType, useV3Rules);
                case 354: return Create_BamlType_LinearByteKeyFrame(isBamlType, useV3Rules);
                case 355: return Create_BamlType_LinearColorKeyFrame(isBamlType, useV3Rules);
                case 356: return Create_BamlType_LinearDecimalKeyFrame(isBamlType, useV3Rules);
                case 357: return Create_BamlType_LinearDoubleKeyFrame(isBamlType, useV3Rules);
                case 358: return Create_BamlType_LinearGradientBrush(isBamlType, useV3Rules);
                case 359: return Create_BamlType_LinearInt16KeyFrame(isBamlType, useV3Rules);
                case 360: return Create_BamlType_LinearInt32KeyFrame(isBamlType, useV3Rules);
                case 361: return Create_BamlType_LinearInt64KeyFrame(isBamlType, useV3Rules);
                case 362: return Create_BamlType_LinearPoint3DKeyFrame(isBamlType, useV3Rules);
                case 363: return Create_BamlType_LinearPointKeyFrame(isBamlType, useV3Rules);
                case 364: return Create_BamlType_LinearQuaternionKeyFrame(isBamlType, useV3Rules);
                case 365: return Create_BamlType_LinearRectKeyFrame(isBamlType, useV3Rules);
                case 366: return Create_BamlType_LinearRotation3DKeyFrame(isBamlType, useV3Rules);
                case 367: return Create_BamlType_LinearSingleKeyFrame(isBamlType, useV3Rules);
                case 368: return Create_BamlType_LinearSizeKeyFrame(isBamlType, useV3Rules);
                case 369: return Create_BamlType_LinearThicknessKeyFrame(isBamlType, useV3Rules);
                case 370: return Create_BamlType_LinearVector3DKeyFrame(isBamlType, useV3Rules);
                case 371: return Create_BamlType_LinearVectorKeyFrame(isBamlType, useV3Rules);
                case 372: return Create_BamlType_List(isBamlType, useV3Rules);
                case 373: return Create_BamlType_ListBox(isBamlType, useV3Rules);
                case 374: return Create_BamlType_ListBoxItem(isBamlType, useV3Rules);
                case 375: return Create_BamlType_ListCollectionView(isBamlType, useV3Rules);
                case 376: return Create_BamlType_ListItem(isBamlType, useV3Rules);
                case 377: return Create_BamlType_ListView(isBamlType, useV3Rules);
                case 378: return Create_BamlType_ListViewItem(isBamlType, useV3Rules);
                case 379: return Create_BamlType_Localization(isBamlType, useV3Rules);
                case 380: return Create_BamlType_LostFocusEventManager(isBamlType, useV3Rules);
                case 381: return Create_BamlType_MarkupExtension(isBamlType, useV3Rules);
                case 382: return Create_BamlType_Material(isBamlType, useV3Rules);
                case 383: return Create_BamlType_MaterialCollection(isBamlType, useV3Rules);
                case 384: return Create_BamlType_MaterialGroup(isBamlType, useV3Rules);
                case 385: return Create_BamlType_Matrix(isBamlType, useV3Rules);
                case 386: return Create_BamlType_Matrix3D(isBamlType, useV3Rules);
                case 387: return Create_BamlType_Matrix3DConverter(isBamlType, useV3Rules); // type converter
                case 388: return Create_BamlType_MatrixAnimationBase(isBamlType, useV3Rules);
                case 389: return Create_BamlType_MatrixAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 390: return Create_BamlType_MatrixAnimationUsingPath(isBamlType, useV3Rules);
                case 391: return Create_BamlType_MatrixCamera(isBamlType, useV3Rules);
                case 392: return Create_BamlType_MatrixConverter(isBamlType, useV3Rules); // type converter
                case 393: return Create_BamlType_MatrixKeyFrame(isBamlType, useV3Rules);
                case 394: return Create_BamlType_MatrixKeyFrameCollection(isBamlType, useV3Rules);
                case 395: return Create_BamlType_MatrixTransform(isBamlType, useV3Rules);
                case 396: return Create_BamlType_MatrixTransform3D(isBamlType, useV3Rules);
                case 397: return Create_BamlType_MediaClock(isBamlType, useV3Rules);
                case 398: return Create_BamlType_MediaElement(isBamlType, useV3Rules);
                case 399: return Create_BamlType_MediaPlayer(isBamlType, useV3Rules);
                case 400: return Create_BamlType_MediaTimeline(isBamlType, useV3Rules);
                case 401: return Create_BamlType_Menu(isBamlType, useV3Rules);
                case 402: return Create_BamlType_MenuBase(isBamlType, useV3Rules);
                case 403: return Create_BamlType_MenuItem(isBamlType, useV3Rules);
                case 404: return Create_BamlType_MenuScrollingVisibilityConverter(isBamlType, useV3Rules);
                case 405: return Create_BamlType_MeshGeometry3D(isBamlType, useV3Rules);
                case 406: return Create_BamlType_Model3D(isBamlType, useV3Rules);
                case 407: return Create_BamlType_Model3DCollection(isBamlType, useV3Rules);
                case 408: return Create_BamlType_Model3DGroup(isBamlType, useV3Rules);
                case 409: return Create_BamlType_ModelVisual3D(isBamlType, useV3Rules);
                case 410: return Create_BamlType_ModifierKeysConverter(isBamlType, useV3Rules); // type converter
                case 411: return Create_BamlType_MouseActionConverter(isBamlType, useV3Rules); // type converter
                case 412: return Create_BamlType_MouseBinding(isBamlType, useV3Rules);
                case 413: return Create_BamlType_MouseDevice(isBamlType, useV3Rules);
                case 414: return Create_BamlType_MouseGesture(isBamlType, useV3Rules);
                case 415: return Create_BamlType_MouseGestureConverter(isBamlType, useV3Rules); // type converter
                case 416: return Create_BamlType_MultiBinding(isBamlType, useV3Rules);
                case 417: return Create_BamlType_MultiBindingExpression(isBamlType, useV3Rules);
                case 418: return Create_BamlType_MultiDataTrigger(isBamlType, useV3Rules);
                case 419: return Create_BamlType_MultiTrigger(isBamlType, useV3Rules);
                case 420: return Create_BamlType_NameScope(isBamlType, useV3Rules);
                case 421: return Create_BamlType_NavigationWindow(isBamlType, useV3Rules);
                case 422: return Create_BamlType_NullExtension(isBamlType, useV3Rules);
                case 423: return Create_BamlType_NullableBoolConverter(isBamlType, useV3Rules); // type converter
                case 424: return Create_BamlType_NullableConverter(isBamlType, useV3Rules); // type converter
                case 425: return Create_BamlType_NumberSubstitution(isBamlType, useV3Rules);
                case 426: return Create_BamlType_Object(isBamlType, useV3Rules);
                case 427: return Create_BamlType_ObjectAnimationBase(isBamlType, useV3Rules);
                case 428: return Create_BamlType_ObjectAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 429: return Create_BamlType_ObjectDataProvider(isBamlType, useV3Rules);
                case 430: return Create_BamlType_ObjectKeyFrame(isBamlType, useV3Rules);
                case 431: return Create_BamlType_ObjectKeyFrameCollection(isBamlType, useV3Rules);
                case 432: return Create_BamlType_OrthographicCamera(isBamlType, useV3Rules);
                case 433: return Create_BamlType_OuterGlowBitmapEffect(isBamlType, useV3Rules);
                case 434: return Create_BamlType_Page(isBamlType, useV3Rules);
                case 435: return Create_BamlType_PageContent(isBamlType, useV3Rules);
                case 436: return Create_BamlType_PageFunctionBase(isBamlType, useV3Rules);
                case 437: return Create_BamlType_Panel(isBamlType, useV3Rules);
                case 438: return Create_BamlType_Paragraph(isBamlType, useV3Rules);
                case 439: return Create_BamlType_ParallelTimeline(isBamlType, useV3Rules);
                case 440: return Create_BamlType_ParserContext(isBamlType, useV3Rules);
                case 441: return Create_BamlType_PasswordBox(isBamlType, useV3Rules);
                case 442: return Create_BamlType_Path(isBamlType, useV3Rules);
                case 443: return Create_BamlType_PathFigure(isBamlType, useV3Rules);
                case 444: return Create_BamlType_PathFigureCollection(isBamlType, useV3Rules);
                case 445: return Create_BamlType_PathFigureCollectionConverter(isBamlType, useV3Rules); // type converter
                case 446: return Create_BamlType_PathGeometry(isBamlType, useV3Rules);
                case 447: return Create_BamlType_PathSegment(isBamlType, useV3Rules);
                case 448: return Create_BamlType_PathSegmentCollection(isBamlType, useV3Rules);
                case 449: return Create_BamlType_PauseStoryboard(isBamlType, useV3Rules);
                case 450: return Create_BamlType_Pen(isBamlType, useV3Rules);
                case 451: return Create_BamlType_PerspectiveCamera(isBamlType, useV3Rules);
                case 452: return Create_BamlType_PixelFormat(isBamlType, useV3Rules);
                case 453: return Create_BamlType_PixelFormatConverter(isBamlType, useV3Rules); // type converter
                case 454: return Create_BamlType_PngBitmapDecoder(isBamlType, useV3Rules);
                case 455: return Create_BamlType_PngBitmapEncoder(isBamlType, useV3Rules);
                case 456: return Create_BamlType_Point(isBamlType, useV3Rules);
                case 457: return Create_BamlType_Point3D(isBamlType, useV3Rules);
                case 458: return Create_BamlType_Point3DAnimation(isBamlType, useV3Rules);
                case 459: return Create_BamlType_Point3DAnimationBase(isBamlType, useV3Rules);
                case 460: return Create_BamlType_Point3DAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 461: return Create_BamlType_Point3DCollection(isBamlType, useV3Rules);
                case 462: return Create_BamlType_Point3DCollectionConverter(isBamlType, useV3Rules); // type converter
                case 463: return Create_BamlType_Point3DConverter(isBamlType, useV3Rules); // type converter
                case 464: return Create_BamlType_Point3DKeyFrame(isBamlType, useV3Rules);
                case 465: return Create_BamlType_Point3DKeyFrameCollection(isBamlType, useV3Rules);
                case 466: return Create_BamlType_Point4D(isBamlType, useV3Rules);
                case 467: return Create_BamlType_Point4DConverter(isBamlType, useV3Rules); // type converter
                case 468: return Create_BamlType_PointAnimation(isBamlType, useV3Rules);
                case 469: return Create_BamlType_PointAnimationBase(isBamlType, useV3Rules);
                case 470: return Create_BamlType_PointAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 471: return Create_BamlType_PointAnimationUsingPath(isBamlType, useV3Rules);
                case 472: return Create_BamlType_PointCollection(isBamlType, useV3Rules);
                case 473: return Create_BamlType_PointCollectionConverter(isBamlType, useV3Rules); // type converter
                case 474: return Create_BamlType_PointConverter(isBamlType, useV3Rules); // type converter
                case 475: return Create_BamlType_PointIListConverter(isBamlType, useV3Rules); // type converter
                case 476: return Create_BamlType_PointKeyFrame(isBamlType, useV3Rules);
                case 477: return Create_BamlType_PointKeyFrameCollection(isBamlType, useV3Rules);
                case 478: return Create_BamlType_PointLight(isBamlType, useV3Rules);
                case 479: return Create_BamlType_PointLightBase(isBamlType, useV3Rules);
                case 480: return Create_BamlType_PolyBezierSegment(isBamlType, useV3Rules);
                case 481: return Create_BamlType_PolyLineSegment(isBamlType, useV3Rules);
                case 482: return Create_BamlType_PolyQuadraticBezierSegment(isBamlType, useV3Rules);
                case 483: return Create_BamlType_Polygon(isBamlType, useV3Rules);
                case 484: return Create_BamlType_Polyline(isBamlType, useV3Rules);
                case 485: return Create_BamlType_Popup(isBamlType, useV3Rules);
                case 486: return Create_BamlType_PresentationSource(isBamlType, useV3Rules);
                case 487: return Create_BamlType_PriorityBinding(isBamlType, useV3Rules);
                case 488: return Create_BamlType_PriorityBindingExpression(isBamlType, useV3Rules);
                case 489: return Create_BamlType_ProgressBar(isBamlType, useV3Rules);
                case 490: return Create_BamlType_ProjectionCamera(isBamlType, useV3Rules);
                case 491: return Create_BamlType_PropertyPath(isBamlType, useV3Rules);
                case 492: return Create_BamlType_PropertyPathConverter(isBamlType, useV3Rules); // type converter
                case 493: return Create_BamlType_QuadraticBezierSegment(isBamlType, useV3Rules);
                case 494: return Create_BamlType_Quaternion(isBamlType, useV3Rules);
                case 495: return Create_BamlType_QuaternionAnimation(isBamlType, useV3Rules);
                case 496: return Create_BamlType_QuaternionAnimationBase(isBamlType, useV3Rules);
                case 497: return Create_BamlType_QuaternionAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 498: return Create_BamlType_QuaternionConverter(isBamlType, useV3Rules); // type converter
                case 499: return Create_BamlType_QuaternionKeyFrame(isBamlType, useV3Rules);
                case 500: return Create_BamlType_QuaternionKeyFrameCollection(isBamlType, useV3Rules);
                case 501: return Create_BamlType_QuaternionRotation3D(isBamlType, useV3Rules);
                case 502: return Create_BamlType_RadialGradientBrush(isBamlType, useV3Rules);
                case 503: return Create_BamlType_RadioButton(isBamlType, useV3Rules);
                case 504: return Create_BamlType_RangeBase(isBamlType, useV3Rules);
                case 505: return Create_BamlType_Rect(isBamlType, useV3Rules);
                case 506: return Create_BamlType_Rect3D(isBamlType, useV3Rules);
                case 507: return Create_BamlType_Rect3DConverter(isBamlType, useV3Rules); // type converter
                case 508: return Create_BamlType_RectAnimation(isBamlType, useV3Rules);
                case 509: return Create_BamlType_RectAnimationBase(isBamlType, useV3Rules);
                case 510: return Create_BamlType_RectAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 511: return Create_BamlType_RectConverter(isBamlType, useV3Rules); // type converter
                case 512: return Create_BamlType_RectKeyFrame(isBamlType, useV3Rules);
                case 513: return Create_BamlType_RectKeyFrameCollection(isBamlType, useV3Rules);
                case 514: return Create_BamlType_Rectangle(isBamlType, useV3Rules);
                case 515: return Create_BamlType_RectangleGeometry(isBamlType, useV3Rules);
                case 516: return Create_BamlType_RelativeSource(isBamlType, useV3Rules);
                case 517: return Create_BamlType_RemoveStoryboard(isBamlType, useV3Rules);
                case 518: return Create_BamlType_RenderOptions(isBamlType, useV3Rules);
                case 519: return Create_BamlType_RenderTargetBitmap(isBamlType, useV3Rules);
                case 520: return Create_BamlType_RepeatBehavior(isBamlType, useV3Rules);
                case 521: return Create_BamlType_RepeatBehaviorConverter(isBamlType, useV3Rules); // type converter
                case 522: return Create_BamlType_RepeatButton(isBamlType, useV3Rules);
                case 523: return Create_BamlType_ResizeGrip(isBamlType, useV3Rules);
                case 524: return Create_BamlType_ResourceDictionary(isBamlType, useV3Rules);
                case 525: return Create_BamlType_ResourceKey(isBamlType, useV3Rules);
                case 526: return Create_BamlType_ResumeStoryboard(isBamlType, useV3Rules);
                case 527: return Create_BamlType_RichTextBox(isBamlType, useV3Rules);
                case 528: return Create_BamlType_RotateTransform(isBamlType, useV3Rules);
                case 529: return Create_BamlType_RotateTransform3D(isBamlType, useV3Rules);
                case 530: return Create_BamlType_Rotation3D(isBamlType, useV3Rules);
                case 531: return Create_BamlType_Rotation3DAnimation(isBamlType, useV3Rules);
                case 532: return Create_BamlType_Rotation3DAnimationBase(isBamlType, useV3Rules);
                case 533: return Create_BamlType_Rotation3DAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 534: return Create_BamlType_Rotation3DKeyFrame(isBamlType, useV3Rules);
                case 535: return Create_BamlType_Rotation3DKeyFrameCollection(isBamlType, useV3Rules);
                case 536: return Create_BamlType_RoutedCommand(isBamlType, useV3Rules);
                case 537: return Create_BamlType_RoutedEvent(isBamlType, useV3Rules);
                case 538: return Create_BamlType_RoutedEventConverter(isBamlType, useV3Rules); // type converter
                case 539: return Create_BamlType_RoutedUICommand(isBamlType, useV3Rules);
                case 540: return Create_BamlType_RoutingStrategy(isBamlType, useV3Rules);
                case 541: return Create_BamlType_RowDefinition(isBamlType, useV3Rules);
                case 542: return Create_BamlType_Run(isBamlType, useV3Rules);
                case 543: return Create_BamlType_RuntimeNamePropertyAttribute(isBamlType, useV3Rules);
                case 544: return Create_BamlType_SByte(isBamlType, useV3Rules);
                case 545: return Create_BamlType_SByteConverter(isBamlType, useV3Rules); // type converter
                case 546: return Create_BamlType_ScaleTransform(isBamlType, useV3Rules);
                case 547: return Create_BamlType_ScaleTransform3D(isBamlType, useV3Rules);
                case 548: return Create_BamlType_ScrollBar(isBamlType, useV3Rules);
                case 549: return Create_BamlType_ScrollContentPresenter(isBamlType, useV3Rules);
                case 550: return Create_BamlType_ScrollViewer(isBamlType, useV3Rules);
                case 551: return Create_BamlType_Section(isBamlType, useV3Rules);
                case 552: return Create_BamlType_SeekStoryboard(isBamlType, useV3Rules);
                case 553: return Create_BamlType_Selector(isBamlType, useV3Rules);
                case 554: return Create_BamlType_Separator(isBamlType, useV3Rules);
                case 555: return Create_BamlType_SetStoryboardSpeedRatio(isBamlType, useV3Rules);
                case 556: return Create_BamlType_Setter(isBamlType, useV3Rules);
                case 557: return Create_BamlType_SetterBase(isBamlType, useV3Rules);
                case 558: return Create_BamlType_Shape(isBamlType, useV3Rules);
                case 559: return Create_BamlType_Single(isBamlType, useV3Rules);
                case 560: return Create_BamlType_SingleAnimation(isBamlType, useV3Rules);
                case 561: return Create_BamlType_SingleAnimationBase(isBamlType, useV3Rules);
                case 562: return Create_BamlType_SingleAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 563: return Create_BamlType_SingleConverter(isBamlType, useV3Rules); // type converter
                case 564: return Create_BamlType_SingleKeyFrame(isBamlType, useV3Rules);
                case 565: return Create_BamlType_SingleKeyFrameCollection(isBamlType, useV3Rules);
                case 566: return Create_BamlType_Size(isBamlType, useV3Rules);
                case 567: return Create_BamlType_Size3D(isBamlType, useV3Rules);
                case 568: return Create_BamlType_Size3DConverter(isBamlType, useV3Rules); // type converter
                case 569: return Create_BamlType_SizeAnimation(isBamlType, useV3Rules);
                case 570: return Create_BamlType_SizeAnimationBase(isBamlType, useV3Rules);
                case 571: return Create_BamlType_SizeAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 572: return Create_BamlType_SizeConverter(isBamlType, useV3Rules); // type converter
                case 573: return Create_BamlType_SizeKeyFrame(isBamlType, useV3Rules);
                case 574: return Create_BamlType_SizeKeyFrameCollection(isBamlType, useV3Rules);
                case 575: return Create_BamlType_SkewTransform(isBamlType, useV3Rules);
                case 576: return Create_BamlType_SkipStoryboardToFill(isBamlType, useV3Rules);
                case 577: return Create_BamlType_Slider(isBamlType, useV3Rules);
                case 578: return Create_BamlType_SolidColorBrush(isBamlType, useV3Rules);
                case 579: return Create_BamlType_SoundPlayerAction(isBamlType, useV3Rules);
                case 580: return Create_BamlType_Span(isBamlType, useV3Rules);
                case 581: return Create_BamlType_SpecularMaterial(isBamlType, useV3Rules);
                case 582: return Create_BamlType_SpellCheck(isBamlType, useV3Rules);
                case 583: return Create_BamlType_SplineByteKeyFrame(isBamlType, useV3Rules);
                case 584: return Create_BamlType_SplineColorKeyFrame(isBamlType, useV3Rules);
                case 585: return Create_BamlType_SplineDecimalKeyFrame(isBamlType, useV3Rules);
                case 586: return Create_BamlType_SplineDoubleKeyFrame(isBamlType, useV3Rules);
                case 587: return Create_BamlType_SplineInt16KeyFrame(isBamlType, useV3Rules);
                case 588: return Create_BamlType_SplineInt32KeyFrame(isBamlType, useV3Rules);
                case 589: return Create_BamlType_SplineInt64KeyFrame(isBamlType, useV3Rules);
                case 590: return Create_BamlType_SplinePoint3DKeyFrame(isBamlType, useV3Rules);
                case 591: return Create_BamlType_SplinePointKeyFrame(isBamlType, useV3Rules);
                case 592: return Create_BamlType_SplineQuaternionKeyFrame(isBamlType, useV3Rules);
                case 593: return Create_BamlType_SplineRectKeyFrame(isBamlType, useV3Rules);
                case 594: return Create_BamlType_SplineRotation3DKeyFrame(isBamlType, useV3Rules);
                case 595: return Create_BamlType_SplineSingleKeyFrame(isBamlType, useV3Rules);
                case 596: return Create_BamlType_SplineSizeKeyFrame(isBamlType, useV3Rules);
                case 597: return Create_BamlType_SplineThicknessKeyFrame(isBamlType, useV3Rules);
                case 598: return Create_BamlType_SplineVector3DKeyFrame(isBamlType, useV3Rules);
                case 599: return Create_BamlType_SplineVectorKeyFrame(isBamlType, useV3Rules);
                case 600: return Create_BamlType_SpotLight(isBamlType, useV3Rules);
                case 601: return Create_BamlType_StackPanel(isBamlType, useV3Rules);
                case 602: return Create_BamlType_StaticExtension(isBamlType, useV3Rules);
                case 603: return Create_BamlType_StaticResourceExtension(isBamlType, useV3Rules);
                case 604: return Create_BamlType_StatusBar(isBamlType, useV3Rules);
                case 605: return Create_BamlType_StatusBarItem(isBamlType, useV3Rules);
                case 606: return Create_BamlType_StickyNoteControl(isBamlType, useV3Rules);
                case 607: return Create_BamlType_StopStoryboard(isBamlType, useV3Rules);
                case 608: return Create_BamlType_Storyboard(isBamlType, useV3Rules);
                case 609: return Create_BamlType_StreamGeometry(isBamlType, useV3Rules);
                case 610: return Create_BamlType_StreamGeometryContext(isBamlType, useV3Rules);
                case 611: return Create_BamlType_StreamResourceInfo(isBamlType, useV3Rules);
                case 612: return Create_BamlType_String(isBamlType, useV3Rules);
                case 613: return Create_BamlType_StringAnimationBase(isBamlType, useV3Rules);
                case 614: return Create_BamlType_StringAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 615: return Create_BamlType_StringConverter(isBamlType, useV3Rules); // type converter
                case 616: return Create_BamlType_StringKeyFrame(isBamlType, useV3Rules);
                case 617: return Create_BamlType_StringKeyFrameCollection(isBamlType, useV3Rules);
                case 618: return Create_BamlType_StrokeCollection(isBamlType, useV3Rules);
                case 619: return Create_BamlType_StrokeCollectionConverter(isBamlType, useV3Rules); // type converter
                case 620: return Create_BamlType_Style(isBamlType, useV3Rules);
                case 621: return Create_BamlType_Stylus(isBamlType, useV3Rules);
                case 622: return Create_BamlType_StylusDevice(isBamlType, useV3Rules);
                case 623: return Create_BamlType_TabControl(isBamlType, useV3Rules);
                case 624: return Create_BamlType_TabItem(isBamlType, useV3Rules);
                case 625: return Create_BamlType_TabPanel(isBamlType, useV3Rules);
                case 626: return Create_BamlType_Table(isBamlType, useV3Rules);
                case 627: return Create_BamlType_TableCell(isBamlType, useV3Rules);
                case 628: return Create_BamlType_TableColumn(isBamlType, useV3Rules);
                case 629: return Create_BamlType_TableRow(isBamlType, useV3Rules);
                case 630: return Create_BamlType_TableRowGroup(isBamlType, useV3Rules);
                case 631: return Create_BamlType_TabletDevice(isBamlType, useV3Rules);
                case 632: return Create_BamlType_TemplateBindingExpression(isBamlType, useV3Rules);
                case 633: return Create_BamlType_TemplateBindingExpressionConverter(isBamlType, useV3Rules); // type converter
                case 634: return Create_BamlType_TemplateBindingExtension(isBamlType, useV3Rules);
                case 635: return Create_BamlType_TemplateBindingExtensionConverter(isBamlType, useV3Rules); // type converter
                case 636: return Create_BamlType_TemplateKey(isBamlType, useV3Rules);
                case 637: return Create_BamlType_TemplateKeyConverter(isBamlType, useV3Rules); // type converter
                case 638: return Create_BamlType_TextBlock(isBamlType, useV3Rules);
                case 639: return Create_BamlType_TextBox(isBamlType, useV3Rules);
                case 640: return Create_BamlType_TextBoxBase(isBamlType, useV3Rules);
                case 641: return Create_BamlType_TextComposition(isBamlType, useV3Rules);
                case 642: return Create_BamlType_TextCompositionManager(isBamlType, useV3Rules);
                case 643: return Create_BamlType_TextDecoration(isBamlType, useV3Rules);
                case 644: return Create_BamlType_TextDecorationCollection(isBamlType, useV3Rules);
                case 645: return Create_BamlType_TextDecorationCollectionConverter(isBamlType, useV3Rules); // type converter
                case 646: return Create_BamlType_TextEffect(isBamlType, useV3Rules);
                case 647: return Create_BamlType_TextEffectCollection(isBamlType, useV3Rules);
                case 648: return Create_BamlType_TextElement(isBamlType, useV3Rules);
                case 649: return Create_BamlType_TextSearch(isBamlType, useV3Rules);
                case 650: return Create_BamlType_ThemeDictionaryExtension(isBamlType, useV3Rules);
                case 651: return Create_BamlType_Thickness(isBamlType, useV3Rules);
                case 652: return Create_BamlType_ThicknessAnimation(isBamlType, useV3Rules);
                case 653: return Create_BamlType_ThicknessAnimationBase(isBamlType, useV3Rules);
                case 654: return Create_BamlType_ThicknessAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 655: return Create_BamlType_ThicknessConverter(isBamlType, useV3Rules); // type converter
                case 656: return Create_BamlType_ThicknessKeyFrame(isBamlType, useV3Rules);
                case 657: return Create_BamlType_ThicknessKeyFrameCollection(isBamlType, useV3Rules);
                case 658: return Create_BamlType_Thumb(isBamlType, useV3Rules);
                case 659: return Create_BamlType_TickBar(isBamlType, useV3Rules);
                case 660: return Create_BamlType_TiffBitmapDecoder(isBamlType, useV3Rules);
                case 661: return Create_BamlType_TiffBitmapEncoder(isBamlType, useV3Rules);
                case 662: return Create_BamlType_TileBrush(isBamlType, useV3Rules);
                case 663: return Create_BamlType_TimeSpan(isBamlType, useV3Rules);
                case 664: return Create_BamlType_TimeSpanConverter(isBamlType, useV3Rules); // type converter
                case 665: return Create_BamlType_Timeline(isBamlType, useV3Rules);
                case 666: return Create_BamlType_TimelineCollection(isBamlType, useV3Rules);
                case 667: return Create_BamlType_TimelineGroup(isBamlType, useV3Rules);
                case 668: return Create_BamlType_ToggleButton(isBamlType, useV3Rules);
                case 669: return Create_BamlType_ToolBar(isBamlType, useV3Rules);
                case 670: return Create_BamlType_ToolBarOverflowPanel(isBamlType, useV3Rules);
                case 671: return Create_BamlType_ToolBarPanel(isBamlType, useV3Rules);
                case 672: return Create_BamlType_ToolBarTray(isBamlType, useV3Rules);
                case 673: return Create_BamlType_ToolTip(isBamlType, useV3Rules);
                case 674: return Create_BamlType_ToolTipService(isBamlType, useV3Rules);
                case 675: return Create_BamlType_Track(isBamlType, useV3Rules);
                case 676: return Create_BamlType_Transform(isBamlType, useV3Rules);
                case 677: return Create_BamlType_Transform3D(isBamlType, useV3Rules);
                case 678: return Create_BamlType_Transform3DCollection(isBamlType, useV3Rules);
                case 679: return Create_BamlType_Transform3DGroup(isBamlType, useV3Rules);
                case 680: return Create_BamlType_TransformCollection(isBamlType, useV3Rules);
                case 681: return Create_BamlType_TransformConverter(isBamlType, useV3Rules); // type converter
                case 682: return Create_BamlType_TransformGroup(isBamlType, useV3Rules);
                case 683: return Create_BamlType_TransformedBitmap(isBamlType, useV3Rules);
                case 684: return Create_BamlType_TranslateTransform(isBamlType, useV3Rules);
                case 685: return Create_BamlType_TranslateTransform3D(isBamlType, useV3Rules);
                case 686: return Create_BamlType_TreeView(isBamlType, useV3Rules);
                case 687: return Create_BamlType_TreeViewItem(isBamlType, useV3Rules);
                case 688: return Create_BamlType_Trigger(isBamlType, useV3Rules);
                case 689: return Create_BamlType_TriggerAction(isBamlType, useV3Rules);
                case 690: return Create_BamlType_TriggerBase(isBamlType, useV3Rules);
                case 691: return Create_BamlType_TypeExtension(isBamlType, useV3Rules);
                case 692: return Create_BamlType_TypeTypeConverter(isBamlType, useV3Rules); // type converter
                case 693: return Create_BamlType_Typography(isBamlType, useV3Rules);
                case 694: return Create_BamlType_UIElement(isBamlType, useV3Rules);
                case 695: return Create_BamlType_UInt16(isBamlType, useV3Rules);
                case 696: return Create_BamlType_UInt16Converter(isBamlType, useV3Rules); // type converter
                case 697: return Create_BamlType_UInt32(isBamlType, useV3Rules);
                case 698: return Create_BamlType_UInt32Converter(isBamlType, useV3Rules); // type converter
                case 699: return Create_BamlType_UInt64(isBamlType, useV3Rules);
                case 700: return Create_BamlType_UInt64Converter(isBamlType, useV3Rules); // type converter
                case 701: return Create_BamlType_UShortIListConverter(isBamlType, useV3Rules); // type converter
                case 702: return Create_BamlType_Underline(isBamlType, useV3Rules);
                case 703: return Create_BamlType_UniformGrid(isBamlType, useV3Rules);
                case 704: return Create_BamlType_Uri(isBamlType, useV3Rules);
                case 705: return Create_BamlType_UriTypeConverter(isBamlType, useV3Rules); // type converter
                case 706: return Create_BamlType_UserControl(isBamlType, useV3Rules);
                case 707: return Create_BamlType_Validation(isBamlType, useV3Rules);
                case 708: return Create_BamlType_Vector(isBamlType, useV3Rules);
                case 709: return Create_BamlType_Vector3D(isBamlType, useV3Rules);
                case 710: return Create_BamlType_Vector3DAnimation(isBamlType, useV3Rules);
                case 711: return Create_BamlType_Vector3DAnimationBase(isBamlType, useV3Rules);
                case 712: return Create_BamlType_Vector3DAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 713: return Create_BamlType_Vector3DCollection(isBamlType, useV3Rules);
                case 714: return Create_BamlType_Vector3DCollectionConverter(isBamlType, useV3Rules); // type converter
                case 715: return Create_BamlType_Vector3DConverter(isBamlType, useV3Rules); // type converter
                case 716: return Create_BamlType_Vector3DKeyFrame(isBamlType, useV3Rules);
                case 717: return Create_BamlType_Vector3DKeyFrameCollection(isBamlType, useV3Rules);
                case 718: return Create_BamlType_VectorAnimation(isBamlType, useV3Rules);
                case 719: return Create_BamlType_VectorAnimationBase(isBamlType, useV3Rules);
                case 720: return Create_BamlType_VectorAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 721: return Create_BamlType_VectorCollection(isBamlType, useV3Rules);
                case 722: return Create_BamlType_VectorCollectionConverter(isBamlType, useV3Rules); // type converter
                case 723: return Create_BamlType_VectorConverter(isBamlType, useV3Rules); // type converter
                case 724: return Create_BamlType_VectorKeyFrame(isBamlType, useV3Rules);
                case 725: return Create_BamlType_VectorKeyFrameCollection(isBamlType, useV3Rules);
                case 726: return Create_BamlType_VideoDrawing(isBamlType, useV3Rules);
                case 727: return Create_BamlType_ViewBase(isBamlType, useV3Rules);
                case 728: return Create_BamlType_Viewbox(isBamlType, useV3Rules);
                case 729: return Create_BamlType_Viewport3D(isBamlType, useV3Rules);
                case 730: return Create_BamlType_Viewport3DVisual(isBamlType, useV3Rules);
                case 731: return Create_BamlType_VirtualizingPanel(isBamlType, useV3Rules);
                case 732: return Create_BamlType_VirtualizingStackPanel(isBamlType, useV3Rules);
                case 733: return Create_BamlType_Visual(isBamlType, useV3Rules);
                case 734: return Create_BamlType_Visual3D(isBamlType, useV3Rules);
                case 735: return Create_BamlType_VisualBrush(isBamlType, useV3Rules);
                case 736: return Create_BamlType_VisualTarget(isBamlType, useV3Rules);
                case 737: return Create_BamlType_WeakEventManager(isBamlType, useV3Rules);
                case 738: return Create_BamlType_WhitespaceSignificantCollectionAttribute(isBamlType, useV3Rules);
                case 739: return Create_BamlType_Window(isBamlType, useV3Rules);
                case 740: return Create_BamlType_WmpBitmapDecoder(isBamlType, useV3Rules);
                case 741: return Create_BamlType_WmpBitmapEncoder(isBamlType, useV3Rules);
                case 742: return Create_BamlType_WrapPanel(isBamlType, useV3Rules);
                case 743: return Create_BamlType_WriteableBitmap(isBamlType, useV3Rules);
                case 744: return Create_BamlType_XamlBrushSerializer(isBamlType, useV3Rules);
                case 745: return Create_BamlType_XamlInt32CollectionSerializer(isBamlType, useV3Rules);
                case 746: return Create_BamlType_XamlPathDataSerializer(isBamlType, useV3Rules);
                case 747: return Create_BamlType_XamlPoint3DCollectionSerializer(isBamlType, useV3Rules);
                case 748: return Create_BamlType_XamlPointCollectionSerializer(isBamlType, useV3Rules);
                case 749: return Create_BamlType_XamlReader(isBamlType, useV3Rules);
                case 750: return Create_BamlType_XamlStyleSerializer(isBamlType, useV3Rules);
                case 751: return Create_BamlType_XamlTemplateSerializer(isBamlType, useV3Rules);
                case 752: return Create_BamlType_XamlVector3DCollectionSerializer(isBamlType, useV3Rules);
                case 753: return Create_BamlType_XamlWriter(isBamlType, useV3Rules);
                case 754: return Create_BamlType_XmlDataProvider(isBamlType, useV3Rules);
                case 755: return Create_BamlType_XmlLangPropertyAttribute(isBamlType, useV3Rules);
                case 756: return Create_BamlType_XmlLanguage(isBamlType, useV3Rules);
                case 757: return Create_BamlType_XmlLanguageConverter(isBamlType, useV3Rules); // type converter
                case 758: return Create_BamlType_XmlNamespaceMapping(isBamlType, useV3Rules);
                case 759: return Create_BamlType_ZoomPercentageConverter(isBamlType, useV3Rules);
                default:
                    throw new InvalidOperationException("Invalid BAML number");
            }
        }

        private uint GetTypeNameHash(string typeName)
        {
            uint result = 0;
            for (int i = 0; i < 26 && i < typeName.Length; i++)
            {
                result = 101 * result + (uint)typeName[i];
            }
            return result;
        }

        // The Caller must still check if they received the correct type.
        protected WpfKnownType CreateKnownBamlType(string typeName, bool isBamlType, bool useV3Rules)
        {
            uint hash = GetTypeNameHash(typeName);
            switch (hash)
            {
                case 826391 : return Create_BamlType_Pen(isBamlType, useV3Rules);
                case 848409 : return Create_BamlType_Run(isBamlType, useV3Rules);
                case 878704 : return Create_BamlType_Uri(isBamlType, useV3Rules);
                case 7210206 : return Create_BamlType_Vector3DKeyFrameCollection(isBamlType, useV3Rules);
                case 8626695 : return Create_BamlType_Typography(isBamlType, useV3Rules);
                case 10713943 : return Create_BamlType_AxisAngleRotation3D(isBamlType, useV3Rules);
                case 17341202 : return Create_BamlType_RectKeyFrameCollection(isBamlType, useV3Rules);
                case 19590438 : return Create_BamlType_ItemsPanelTemplate(isBamlType, useV3Rules);
                case 21757238 : return Create_BamlType_Quaternion(isBamlType, useV3Rules);
                case 27438720 : return Create_BamlType_FigureLength(isBamlType, useV3Rules);
                case 35895921 : return Create_BamlType_ComponentResourceKeyConverter(isBamlType, useV3Rules); // type converter
                case 44267921 : return Create_BamlType_GridViewRowPresenter(isBamlType, useV3Rules);
                case 50494706 : return Create_BamlType_CommandBindingCollection(isBamlType, useV3Rules);
                case 56425604 : return Create_BamlType_SplinePoint3DKeyFrame(isBamlType, useV3Rules);
                case 69143185 : return Create_BamlType_Bold(isBamlType, useV3Rules);
                case 69246004 : return Create_BamlType_Byte(isBamlType, useV3Rules);
                case 70100982 : return Create_BamlType_Char(isBamlType, useV3Rules);
                case 72192662 : return Create_BamlType_MatrixCamera(isBamlType, useV3Rules);
                case 72224805 : return Create_BamlType_Enum(isBamlType, useV3Rules);
                case 74282775 : return Create_BamlType_RotateTransform(isBamlType, useV3Rules);
                case 74324990 : return Create_BamlType_Grid(isBamlType, useV3Rules);
                case 74355593 : return Create_BamlType_Guid(isBamlType, useV3Rules);
                case 79385192 : return Create_BamlType_Line(isBamlType, useV3Rules);
                case 79385712 : return Create_BamlType_List(isBamlType, useV3Rules);
                case 80374705 : return Create_BamlType_Menu(isBamlType, useV3Rules);
                case 83424081 : return Create_BamlType_Page(isBamlType, useV3Rules);
                case 83425397 : return Create_BamlType_Path(isBamlType, useV3Rules);
                case 85525098 : return Create_BamlType_Rect(isBamlType, useV3Rules);
                case 86598511 : return Create_BamlType_Size(isBamlType, useV3Rules);
                case 86639180 : return Create_BamlType_Visual(isBamlType, useV3Rules);
                case 86667402 : return Create_BamlType_Span(isBamlType, useV3Rules);
                case 92454412 : return Create_BamlType_ColorAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 95311897 : return Create_BamlType_KeyboardDevice(isBamlType, useV3Rules);
                case 98196275 : return Create_BamlType_DoubleConverter(isBamlType, useV3Rules); // type converter
                case 114848175 : return Create_BamlType_XamlPoint3DCollectionSerializer(isBamlType, useV3Rules);
                case 116324695 : return Create_BamlType_SByte(isBamlType, useV3Rules);
                case 117546261 : return Create_BamlType_SplineVector3DKeyFrame(isBamlType, useV3Rules);
                case 129393695 : return Create_BamlType_VectorAnimation(isBamlType, useV3Rules);
                case 133371900 : return Create_BamlType_DoubleIListConverter(isBamlType, useV3Rules); // type converter
                case 133966438 : return Create_BamlType_ScrollContentPresenter(isBamlType, useV3Rules);
                case 138822808 : return Create_BamlType_UIElementCollection(isBamlType, useV3Rules);
                case 141025390 : return Create_BamlType_CharKeyFrame(isBamlType, useV3Rules);
                case 149784707 : return Create_BamlType_TextDecorationCollectionConverter(isBamlType, useV3Rules); // type converter
                case 150436622 : return Create_BamlType_SplineRotation3DKeyFrame(isBamlType, useV3Rules);
                case 151882568 : return Create_BamlType_ModelVisual3D(isBamlType, useV3Rules);
                case 153543503 : return Create_BamlType_CollectionView(isBamlType, useV3Rules);
                case 155230905 : return Create_BamlType_Shape(isBamlType, useV3Rules);
                case 157696880 : return Create_BamlType_BrushConverter(isBamlType, useV3Rules); // type converter
                case 158646293 : return Create_BamlType_TranslateTransform3D(isBamlType, useV3Rules);
                case 158796542 : return Create_BamlType_TileBrush(isBamlType, useV3Rules);
                case 159112278 : return Create_BamlType_DecimalAnimationBase(isBamlType, useV3Rules);
                case 160906176 : return Create_BamlType_GroupItem(isBamlType, useV3Rules);
                case 162191870 : return Create_BamlType_ThicknessKeyFrameCollection(isBamlType, useV3Rules);
                case 163112773 : return Create_BamlType_WmpBitmapEncoder(isBamlType, useV3Rules);
                case 167522129 : return Create_BamlType_EventManager(isBamlType, useV3Rules);
                case 167785563 : return Create_BamlType_XamlInt32CollectionSerializer(isBamlType, useV3Rules);
                case 167838937 : return Create_BamlType_Style(isBamlType, useV3Rules);
                case 172295577 : return Create_BamlType_SeekStoryboard(isBamlType, useV3Rules);
                case 176201414 : return Create_BamlType_BindingListCollectionView(isBamlType, useV3Rules);
                case 180014290 : return Create_BamlType_ProgressBar(isBamlType, useV3Rules);
                case 185134902 : return Create_BamlType_Int16Converter(isBamlType, useV3Rules); // type converter
                case 185603331 : return Create_BamlType_WhitespaceSignificantCollectionAttribute(isBamlType, useV3Rules);
                case 188925504 : return Create_BamlType_DiscreteInt64KeyFrame(isBamlType, useV3Rules);
                case 193712015 : return Create_BamlType_ModifierKeysConverter(isBamlType, useV3Rules); // type converter
                case 208056328 : return Create_BamlType_Int64AnimationBase(isBamlType, useV3Rules);
                case 220163992 : return Create_BamlType_GeometryCollection(isBamlType, useV3Rules);
                case 230922235 : return Create_BamlType_ThicknessAnimationBase(isBamlType, useV3Rules);
                case 236543168 : return Create_BamlType_CultureInfo(isBamlType, useV3Rules);
                case 240474481 : return Create_BamlType_MultiDataTrigger(isBamlType, useV3Rules);
                case 246620386 : return Create_BamlType_HeaderedContentControl(isBamlType, useV3Rules);
                case 252088996 : return Create_BamlType_Table(isBamlType, useV3Rules);
                case 253854091 : return Create_BamlType_DoubleAnimation(isBamlType, useV3Rules);
                case 254218629 : return Create_BamlType_DiscreteVector3DKeyFrame(isBamlType, useV3Rules);
                case 259495020 : return Create_BamlType_Thumb(isBamlType, useV3Rules);
                case 260974524 : return Create_BamlType_KeyGestureConverter(isBamlType, useV3Rules); // type converter
                case 262392462 : return Create_BamlType_TextBox(isBamlType, useV3Rules);
                case 265347790 : return Create_BamlType_OuterGlowBitmapEffect(isBamlType, useV3Rules);
                case 269593009 : return Create_BamlType_Track(isBamlType, useV3Rules);
                case 278513255 : return Create_BamlType_Vector3DAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 283659891 : return Create_BamlType_PenLineJoin(isBamlType, useV3Rules);
                case 285954745 : return Create_BamlType_TemplateKeyConverter(isBamlType, useV3Rules); // type converter
                case 291478073 : return Create_BamlType_GifBitmapDecoder(isBamlType, useV3Rules);
                case 297191555 : return Create_BamlType_LineSegment(isBamlType, useV3Rules);
                case 300220768 : return Create_BamlType_CharAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 314824934 : return Create_BamlType_Int32RectConverter(isBamlType, useV3Rules); // type converter
                case 324370636 : return Create_BamlType_Thickness(isBamlType, useV3Rules);
                case 326446886 : return Create_BamlType_DecimalAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 333511440 : return Create_BamlType_PngBitmapDecoder(isBamlType, useV3Rules);
                case 337659401 : return Create_BamlType_Point3DKeyFrame(isBamlType, useV3Rules);
                case 339474011 : return Create_BamlType_Decimal(isBamlType, useV3Rules);
                case 339935827 : return Create_BamlType_DiscreteByteKeyFrame(isBamlType, useV3Rules);
                case 340792718 : return Create_BamlType_Int16Animation(isBamlType, useV3Rules);
                case 357673449 : return Create_BamlType_RuntimeNamePropertyAttribute(isBamlType, useV3Rules);
                case 363476966 : return Create_BamlType_UInt64Converter(isBamlType, useV3Rules); // type converter
                case 373217479 : return Create_BamlType_TemplateBindingExpression(isBamlType, useV3Rules);
                case 374151590 : return Create_BamlType_BindingBase(isBamlType, useV3Rules);
                case 374415758 : return Create_BamlType_ToggleButton(isBamlType, useV3Rules);
                case 384741759 : return Create_BamlType_RadialGradientBrush(isBamlType, useV3Rules);
                case 386930200 : return Create_BamlType_EmissiveMaterial(isBamlType, useV3Rules);
                case 387234139 : return Create_BamlType_Decorator(isBamlType, useV3Rules);
                case 390343400 : return Create_BamlType_RichTextBox(isBamlType, useV3Rules);
                case 409173380 : return Create_BamlType_Polyline(isBamlType, useV3Rules);
                case 409221055 : return Create_BamlType_LinearThicknessKeyFrame(isBamlType, useV3Rules);
                case 411745576 : return Create_BamlType_StatusBarItem(isBamlType, useV3Rules);
                case 412334313 : return Create_BamlType_DocumentViewer(isBamlType, useV3Rules);
                case 414460394 : return Create_BamlType_MultiBinding(isBamlType, useV3Rules);
                case 425410901 : return Create_BamlType_PresentationSource(isBamlType, useV3Rules);
                case 431709905 : return Create_BamlType_RowDefinitionCollection(isBamlType, useV3Rules);
                case 433371184 : return Create_BamlType_MeshGeometry3D(isBamlType, useV3Rules);
                case 435869667 : return Create_BamlType_ContextMenuService(isBamlType, useV3Rules);
                case 461968488 : return Create_BamlType_RenderTargetBitmap(isBamlType, useV3Rules);
                case 465416194 : return Create_BamlType_AdornedElementPlaceholder(isBamlType, useV3Rules);
                case 473143590 : return Create_BamlType_BitmapEffect(isBamlType, useV3Rules);
                case 481300314 : return Create_BamlType_Int64AnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 490900943 : return Create_BamlType_IAddChildInternal(isBamlType, useV3Rules);
                case 492584280 : return Create_BamlType_MouseGestureConverter(isBamlType, useV3Rules); // type converter
                case 501987435 : return Create_BamlType_Rotation3DAnimation(isBamlType, useV3Rules);
                case 504184511 : return Create_BamlType_ToolBarPanel(isBamlType, useV3Rules);
                case 507138120 : return Create_BamlType_BooleanConverter(isBamlType, useV3Rules); // type converter
                case 509621479 : return Create_BamlType_Double(isBamlType, useV3Rules);
                case 511076833 : return Create_BamlType_Localization(isBamlType, useV3Rules);
                case 511132298 : return Create_BamlType_DynamicResourceExtension(isBamlType, useV3Rules);
                case 522405838 : return Create_BamlType_UShortIListConverter(isBamlType, useV3Rules); // type converter
                case 525600274 : return Create_BamlType_TemplateBindingExtensionConverter(isBamlType, useV3Rules); // type converter
                case 532150459 : return Create_BamlType_DateTimeConverter2(isBamlType, useV3Rules); // type converter
                case 554920085 : return Create_BamlType_FontFamily(isBamlType, useV3Rules);
                case 563168829 : return Create_BamlType_Rect3D(isBamlType, useV3Rules);
                case 566074239 : return Create_BamlType_Expander(isBamlType, useV3Rules);
                case 568845828 : return Create_BamlType_ScrollBarVisibility(isBamlType, useV3Rules);
                case 571143672 : return Create_BamlType_GridViewRowPresenterBase(isBamlType, useV3Rules);
                case 577530966 : return Create_BamlType_DataTrigger(isBamlType, useV3Rules);
                case 582823334 : return Create_BamlType_UniformGrid(isBamlType, useV3Rules);
                case 585590105 : return Create_BamlType_CombinedGeometry(isBamlType, useV3Rules);
                case 602421868 : return Create_BamlType_MouseBinding(isBamlType, useV3Rules);
                case 603960058 : return Create_BamlType_ColorAnimationBase(isBamlType, useV3Rules);
                case 614788594 : return Create_BamlType_ContextMenu(isBamlType, useV3Rules);
                case 615309592 : return Create_BamlType_UIElement(isBamlType, useV3Rules);
                case 615357807 : return Create_BamlType_VectorAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 615898683 : return Create_BamlType_TypeExtension(isBamlType, useV3Rules);
                case 620560167 : return Create_BamlType_GeneralTransformGroup(isBamlType, useV3Rules);
                case 620850810 : return Create_BamlType_SizeAnimationBase(isBamlType, useV3Rules);
                case 623567164 : return Create_BamlType_PageContent(isBamlType, useV3Rules);
                case 627070138 : return Create_BamlType_SplineColorKeyFrame(isBamlType, useV3Rules);
                case 640587303 : return Create_BamlType_RoutingStrategy(isBamlType, useV3Rules);
                case 646994170 : return Create_BamlType_LinearVectorKeyFrame(isBamlType, useV3Rules);
                case 649244994 : return Create_BamlType_CommandBinding(isBamlType, useV3Rules);
                case 655979150 : return Create_BamlType_SpecularMaterial(isBamlType, useV3Rules);
                case 664895538 : return Create_BamlType_TriggerAction(isBamlType, useV3Rules);
                case 665996286 : return Create_BamlType_QuaternionConverter(isBamlType, useV3Rules); // type converter
                case 672969529 : return Create_BamlType_CornerRadiusConverter(isBamlType, useV3Rules); // type converter
                case 685428999 : return Create_BamlType_PixelFormat(isBamlType, useV3Rules);
                case 686620977 : return Create_BamlType_XamlStyleSerializer(isBamlType, useV3Rules);
                case 686841832 : return Create_BamlType_GeometryConverter(isBamlType, useV3Rules); // type converter
                case 687971593 : return Create_BamlType_JpegBitmapDecoder(isBamlType, useV3Rules);
                case 698201008 : return Create_BamlType_GridLength(isBamlType, useV3Rules);
                case 712702706 : return Create_BamlType_DocumentReference(isBamlType, useV3Rules);
                case 713325256 : return Create_BamlType_FrameworkElementFactory(isBamlType, useV3Rules);
                case 725957013 : return Create_BamlType_Int32AnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 727501438 : return Create_BamlType_JournalOwnership(isBamlType, useV3Rules);
                case 734249444 : return Create_BamlType_BevelBitmapEffect(isBamlType, useV3Rules);
                case 741421013 : return Create_BamlType_DiscreteCharKeyFrame(isBamlType, useV3Rules);
                case 748275923 : return Create_BamlType_UInt16Converter(isBamlType, useV3Rules); // type converter
                case 749660283 : return Create_BamlType_InlineCollection(isBamlType, useV3Rules);
                case 758538788 : return Create_BamlType_ICommand(isBamlType, useV3Rules);
                case 779609571 : return Create_BamlType_ScaleTransform3D(isBamlType, useV3Rules);
                case 782411712 : return Create_BamlType_FrameworkPropertyMetadata(isBamlType, useV3Rules);
                case 784038997 : return Create_BamlType_TextDecoration(isBamlType, useV3Rules);
                case 784826098 : return Create_BamlType_Underline(isBamlType, useV3Rules);
                case 787776053 : return Create_BamlType_IStyleConnector(isBamlType, useV3Rules);
                case 807830300 : return Create_BamlType_DefinitionBase(isBamlType, useV3Rules);
                case 821654102 : return Create_BamlType_QuaternionAnimation(isBamlType, useV3Rules);
                case 832085183 : return Create_BamlType_NullableBoolConverter(isBamlType, useV3Rules); // type converter
                case 840953286 : return Create_BamlType_PointKeyFrameCollection(isBamlType, useV3Rules);
                case 861523813 : return Create_BamlType_PriorityBindingExpression(isBamlType, useV3Rules);
                case 863295067 : return Create_BamlType_ColorConverter(isBamlType, useV3Rules); // type converter
                case 864192108 : return Create_BamlType_ThicknessConverter(isBamlType, useV3Rules); // type converter
                case 874593556 : return Create_BamlType_ClockController(isBamlType, useV3Rules);
                case 874609234 : return Create_BamlType_DoubleAnimationBase(isBamlType, useV3Rules);
                case 880110784 : return Create_BamlType_ExpressionConverter(isBamlType, useV3Rules); // type converter
                case 896504879 : return Create_BamlType_DoubleCollection(isBamlType, useV3Rules);
                case 897586265 : return Create_BamlType_SplineRectKeyFrame(isBamlType, useV3Rules);
                case 897706848 : return Create_BamlType_TextBlock(isBamlType, useV3Rules);
                case 905080928 : return Create_BamlType_FixedDocumentSequence(isBamlType, useV3Rules);
                case 906240700 : return Create_BamlType_UserControl(isBamlType, useV3Rules);
                case 912040738 : return Create_BamlType_TextEffectCollection(isBamlType, useV3Rules);
                case 916823320 : return Create_BamlType_InputDevice(isBamlType, useV3Rules);
                case 921174220 : return Create_BamlType_TriggerCollection(isBamlType, useV3Rules);
                case 922642898 : return Create_BamlType_PointLight(isBamlType, useV3Rules);
                case 926965831 : return Create_BamlType_InputScopeName(isBamlType, useV3Rules);
                case 936485592 : return Create_BamlType_FrameworkRichTextComposition(isBamlType, useV3Rules);
                case 937814480 : return Create_BamlType_StrokeCollectionConverter(isBamlType, useV3Rules); // type converter
                case 937862401 : return Create_BamlType_GlyphTypeface(isBamlType, useV3Rules);
                case 948576441 : return Create_BamlType_ArcSegment(isBamlType, useV3Rules);
                case 949941650 : return Create_BamlType_PropertyPath(isBamlType, useV3Rules);
                case 959679175 : return Create_BamlType_XamlPathDataSerializer(isBamlType, useV3Rules);
                case 961185762 : return Create_BamlType_Border(isBamlType, useV3Rules);
                case 967604372 : return Create_BamlType_FormatConvertedBitmap(isBamlType, useV3Rules);
                case 977040319 : return Create_BamlType_Validation(isBamlType, useV3Rules);
                case 991727131 : return Create_BamlType_MouseActionConverter(isBamlType, useV3Rules); // type converter
                case 996200203 : return Create_BamlType_AnimationTimeline(isBamlType, useV3Rules);
                case 997254168 : return Create_BamlType_Geometry(isBamlType, useV3Rules);
                case 997998281 : return Create_BamlType_ComboBox(isBamlType, useV3Rules);
                case 1016377725 : return Create_BamlType_InputMethod(isBamlType, useV3Rules);
                case 1018952883 : return Create_BamlType_ColorAnimation(isBamlType, useV3Rules);
                case 1019262156 : return Create_BamlType_PathSegmentCollection(isBamlType, useV3Rules);
                case 1019849924 : return Create_BamlType_ThicknessAnimation(isBamlType, useV3Rules);
                case 1020537735 : return Create_BamlType_Material(isBamlType, useV3Rules);
                case 1021162590 : return Create_BamlType_Vector3DConverter(isBamlType, useV3Rules); // type converter
                case 1029614653 : return Create_BamlType_Point3DCollectionConverter(isBamlType, useV3Rules); // type converter
                case 1042012617 : return Create_BamlType_Rectangle(isBamlType, useV3Rules);
                case 1043347506 : return Create_BamlType_BorderGapMaskConverter(isBamlType, useV3Rules);
                case 1049460504 : return Create_BamlType_XmlNamespaceMappingCollection(isBamlType, useV3Rules);
                case 1054011130 : return Create_BamlType_ThemeDictionaryExtension(isBamlType, useV3Rules);
                case 1056330559 : return Create_BamlType_GifBitmapEncoder(isBamlType, useV3Rules);
                case 1060097603 : return Create_BamlType_ColumnDefinitionCollection(isBamlType, useV3Rules);
                case 1067429912 : return Create_BamlType_ObjectDataProvider(isBamlType, useV3Rules);
                case 1069777608 : return Create_BamlType_MouseGesture(isBamlType, useV3Rules);
                case 1082938778 : return Create_BamlType_TableColumn(isBamlType, useV3Rules);
                case 1083837605 : return Create_BamlType_KeyboardNavigation(isBamlType, useV3Rules);
                case 1083922042 : return Create_BamlType_PageFunctionBase(isBamlType, useV3Rules);
                case 1085414201 : return Create_BamlType_LateBoundBitmapDecoder(isBamlType, useV3Rules);
                case 1094052145 : return Create_BamlType_RectAnimationBase(isBamlType, useV3Rules);
                case 1098363926 : return Create_BamlType_PngBitmapEncoder(isBamlType, useV3Rules);
                case 1104645377 : return Create_BamlType_ContentElement(isBamlType, useV3Rules);
                case 1107478903 : return Create_BamlType_DecimalConverter(isBamlType, useV3Rules); // type converter
                case 1117366565 : return Create_BamlType_PointAnimationUsingPath(isBamlType, useV3Rules);
                case 1130648825 : return Create_BamlType_SplineInt16KeyFrame(isBamlType, useV3Rules);
                case 1150413556 : return Create_BamlType_WriteableBitmap(isBamlType, useV3Rules);
                case 1158811630 : return Create_BamlType_ListViewItem(isBamlType, useV3Rules);
                case 1159768689 : return Create_BamlType_LinearRectKeyFrame(isBamlType, useV3Rules);
                case 1176820406 : return Create_BamlType_Vector3DAnimation(isBamlType, useV3Rules);
                case 1178626248 : return Create_BamlType_InlineUIContainer(isBamlType, useV3Rules);
                case 1183725611 : return Create_BamlType_ContainerVisual(isBamlType, useV3Rules);
                case 1184273902 : return Create_BamlType_MediaElement(isBamlType, useV3Rules);
                case 1186185889 : return Create_BamlType_MarkupExtension(isBamlType, useV3Rules);
                case 1209646082 : return Create_BamlType_TranslateTransform(isBamlType, useV3Rules);
                case 1210722572 : return Create_BamlType_BaseIListConverter(isBamlType, useV3Rules); // type converter
                case 1210906771 : return Create_BamlType_VectorCollection(isBamlType, useV3Rules);
                case 1221854500 : return Create_BamlType_FontStyleConverter(isBamlType, useV3Rules); // type converter
                case 1227117227 : return Create_BamlType_FontWeightConverter(isBamlType, useV3Rules); // type converter
                case 1239296217 : return Create_BamlType_TextComposition(isBamlType, useV3Rules);
                case 1253725583 : return Create_BamlType_BulletDecorator(isBamlType, useV3Rules);
                case 1263136719 : return Create_BamlType_DecimalAnimation(isBamlType, useV3Rules);
                case 1263268373 : return Create_BamlType_Model3DGroup(isBamlType, useV3Rules);
                case 1283950600 : return Create_BamlType_ResizeGrip(isBamlType, useV3Rules);
                case 1285079965 : return Create_BamlType_DashStyle(isBamlType, useV3Rules);
                case 1285743637 : return Create_BamlType_StreamGeometryContext(isBamlType, useV3Rules);
                case 1291553535 : return Create_BamlType_SplineInt32KeyFrame(isBamlType, useV3Rules);
                case 1305993458 : return Create_BamlType_TextEffect(isBamlType, useV3Rules);
                case 1318104087 : return Create_BamlType_BooleanAnimationBase(isBamlType, useV3Rules);
                case 1318159567 : return Create_BamlType_ImageDrawing(isBamlType, useV3Rules);
                case 1337691186 : return Create_BamlType_LinearColorKeyFrame(isBamlType, useV3Rules);
                case 1338939476 : return Create_BamlType_TemplateBindingExtension(isBamlType, useV3Rules);
                case 1339394931 : return Create_BamlType_ToolBar(isBamlType, useV3Rules);
                case 1339579355 : return Create_BamlType_ToolTip(isBamlType, useV3Rules);
                case 1347486791 : return Create_BamlType_ColorKeyFrame(isBamlType, useV3Rules);
                case 1359074139 : return Create_BamlType_Viewport3DVisual(isBamlType, useV3Rules);
                case 1366171760 : return Create_BamlType_ImageMetadata(isBamlType, useV3Rules);
                case 1369509399 : return Create_BamlType_DialogResultConverter(isBamlType, useV3Rules); // type converter
                case 1370978769 : return Create_BamlType_ClockGroup(isBamlType, useV3Rules);
                case 1373930089 : return Create_BamlType_XamlReader(isBamlType, useV3Rules);
                case 1392278866 : return Create_BamlType_Size3DConverter(isBamlType, useV3Rules); // type converter
                case 1395061043 : return Create_BamlType_TreeView(isBamlType, useV3Rules);
                case 1399972982 : return Create_BamlType_SingleKeyFrameCollection(isBamlType, useV3Rules);
                case 1407254931 : return Create_BamlType_Inline(isBamlType, useV3Rules);
                case 1411264711 : return Create_BamlType_PointAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 1412280093 : return Create_BamlType_GridSplitter(isBamlType, useV3Rules);
                case 1412505639 : return Create_BamlType_CollectionContainer(isBamlType, useV3Rules);
                case 1412591399 : return Create_BamlType_ToolBarTray(isBamlType, useV3Rules);
                case 1419366049 : return Create_BamlType_Camera(isBamlType, useV3Rules);
                case 1420568068 : return Create_BamlType_Canvas(isBamlType, useV3Rules);
                case 1423253394 : return Create_BamlType_ResourceDictionary(isBamlType, useV3Rules);
                case 1423763428 : return Create_BamlType_Point3DAnimationBase(isBamlType, useV3Rules);
                case 1433323584 : return Create_BamlType_TextAlignment(isBamlType, useV3Rules);
                case 1441084717 : return Create_BamlType_GridView(isBamlType, useV3Rules);
                case 1451810926 : return Create_BamlType_ParserContext(isBamlType, useV3Rules);
                case 1451899428 : return Create_BamlType_QuaternionAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 1452824079 : return Create_BamlType_JpegBitmapEncoder(isBamlType, useV3Rules);
                case 1453546252 : return Create_BamlType_TickBar(isBamlType, useV3Rules);
                case 1463715626 : return Create_BamlType_DependencyPropertyConverter(isBamlType, useV3Rules); // type converter
                case 1483217979 : return Create_BamlType_XamlVector3DCollectionSerializer(isBamlType, useV3Rules);
                case 1497057972 : return Create_BamlType_BlockUIContainer(isBamlType, useV3Rules);
                case 1503494182 : return Create_BamlType_Paragraph(isBamlType, useV3Rules);
                case 1503988241 : return Create_BamlType_Storyboard(isBamlType, useV3Rules);
                case 1505495632 : return Create_BamlType_Freezable(isBamlType, useV3Rules);
                case 1505896427 : return Create_BamlType_FlowDocument(isBamlType, useV3Rules);
                case 1514216138 : return Create_BamlType_PropertyPathConverter(isBamlType, useV3Rules); // type converter
                case 1518131472 : return Create_BamlType_GeometryDrawing(isBamlType, useV3Rules);
                case 1525454651 : return Create_BamlType_ZoomPercentageConverter(isBamlType, useV3Rules);
                case 1528777786 : return Create_BamlType_LengthConverter(isBamlType, useV3Rules); // type converter
                case 1534031197 : return Create_BamlType_MatrixTransform(isBamlType, useV3Rules);
                case 1551028176 : return Create_BamlType_DocumentViewerBase(isBamlType, useV3Rules);
                case 1553353434 : return Create_BamlType_GuidelineSet(isBamlType, useV3Rules);
                case 1563195901 : return Create_BamlType_HierarchicalDataTemplate(isBamlType, useV3Rules);
                case 1566189877 : return Create_BamlType_CornerRadius(isBamlType, useV3Rules);
                case 1566963134 : return Create_BamlType_SplineSizeKeyFrame(isBamlType, useV3Rules);
                case 1587772992 : return Create_BamlType_Button(isBamlType, useV3Rules);
                case 1587863541 : return Create_BamlType_JournalEntryListConverter(isBamlType, useV3Rules);
                case 1588658228 : return Create_BamlType_DiscretePoint3DKeyFrame(isBamlType, useV3Rules);
                case 1596615863 : return Create_BamlType_TextElement(isBamlType, useV3Rules);
                case 1599263472 : return Create_BamlType_KeyTimeConverter(isBamlType, useV3Rules); // type converter
                case 1610838933 : return Create_BamlType_MediaPlayer(isBamlType, useV3Rules);
                case 1630772625 : return Create_BamlType_FixedPage(isBamlType, useV3Rules);
                case 1636299558 : return Create_BamlType_BeginStoryboard(isBamlType, useV3Rules);
                case 1636350275 : return Create_BamlType_VectorKeyFrame(isBamlType, useV3Rules);
                case 1638466145 : return Create_BamlType_JournalEntry(isBamlType, useV3Rules);
                case 1641446656 : return Create_BamlType_AffineTransform3D(isBamlType, useV3Rules);
                case 1648168330 : return Create_BamlType_SpotLight(isBamlType, useV3Rules);
                case 1648736402 : return Create_BamlType_DiscreteVectorKeyFrame(isBamlType, useV3Rules);
                case 1649262223 : return Create_BamlType_Condition(isBamlType, useV3Rules);
                case 1661775612 : return Create_BamlType_TransformConverter(isBamlType, useV3Rules); // type converter
                case 1665515158 : return Create_BamlType_Animatable(isBamlType, useV3Rules);
                case 1667234335 : return Create_BamlType_Glyphs(isBamlType, useV3Rules);
                case 1669447028 : return Create_BamlType_ByteConverter(isBamlType, useV3Rules); // type converter
                case 1673388557 : return Create_BamlType_DiscreteQuaternionKeyFrame(isBamlType, useV3Rules);
                case 1676692392 : return Create_BamlType_GradientStopCollection(isBamlType, useV3Rules);
                case 1682538720 : return Create_BamlType_MediaClock(isBamlType, useV3Rules);
                case 1683116109 : return Create_BamlType_QuaternionRotation3D(isBamlType, useV3Rules);
                case 1684223221 : return Create_BamlType_Rotation3DAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 1689931813 : return Create_BamlType_Int16AnimationBase(isBamlType, useV3Rules);
                case 1698047614 : return Create_BamlType_KeyboardNavigationMode(isBamlType, useV3Rules);
                case 1700491611 : return Create_BamlType_CompositionTarget(isBamlType, useV3Rules);
                case 1709260677 : return Create_BamlType_Section(isBamlType, useV3Rules);
                case 1714171663 : return Create_BamlType_FrameworkPropertyMetadataOptions(isBamlType, useV3Rules);
                case 1720156579 : return Create_BamlType_TriggerBase(isBamlType, useV3Rules);
                case 1726725401 : return Create_BamlType_Separator(isBamlType, useV3Rules);
                case 1727243753 : return Create_BamlType_XmlLanguage(isBamlType, useV3Rules);
                case 1730845471 : return Create_BamlType_NameScope(isBamlType, useV3Rules);
                case 1737370437 : return Create_BamlType_MouseDevice(isBamlType, useV3Rules);
                case 1741197127 : return Create_BamlType_NullableConverter(isBamlType, useV3Rules); // type converter
                case 1749703332 : return Create_BamlType_Point3DAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 1754018176 : return Create_BamlType_LineGeometry(isBamlType, useV3Rules);
                case 1774798759 : return Create_BamlType_Transform3DCollection(isBamlType, useV3Rules);
                case 1784618733 : return Create_BamlType_PathGeometry(isBamlType, useV3Rules);
                case 1792077897 : return Create_BamlType_StaticResourceExtension(isBamlType, useV3Rules);
                case 1798811252 : return Create_BamlType_Int32Collection(isBamlType, useV3Rules);
                case 1799179879 : return Create_BamlType_FrameworkContentElement(isBamlType, useV3Rules);
                case 1810393776 : return Create_BamlType_XmlLangPropertyAttribute(isBamlType, useV3Rules);
                case 1811071644 : return Create_BamlType_PageContentCollection(isBamlType, useV3Rules);
                case 1811729200 : return Create_BamlType_BooleanKeyFrame(isBamlType, useV3Rules);
                case 1813359201 : return Create_BamlType_Rect3DConverter(isBamlType, useV3Rules); // type converter
                case 1815264388 : return Create_BamlType_ThicknessKeyFrame(isBamlType, useV3Rules);
                case 1817616839 : return Create_BamlType_RadioButton(isBamlType, useV3Rules);
                case 1825104844 : return Create_BamlType_ByteAnimation(isBamlType, useV3Rules);
                case 1829145558 : return Create_BamlType_LinearSizeKeyFrame(isBamlType, useV3Rules);
                case 1838328148 : return Create_BamlType_TextCompositionManager(isBamlType, useV3Rules);
                case 1838910454 : return Create_BamlType_LinearDoubleKeyFrame(isBamlType, useV3Rules);
                case 1841269873 : return Create_BamlType_LinearInt16KeyFrame(isBamlType, useV3Rules);
                case 1844348898 : return Create_BamlType_RotateTransform3D(isBamlType, useV3Rules);
                case 1847171633 : return Create_BamlType_RoutedEvent(isBamlType, useV3Rules);
                case 1847800773 : return Create_BamlType_RepeatBehaviorConverter(isBamlType, useV3Rules); // type converter
                case 1851065478 : return Create_BamlType_Int16KeyFrame(isBamlType, useV3Rules);
                case 1857902570 : return Create_BamlType_DiscreteColorKeyFrame(isBamlType, useV3Rules);
                case 1867638374 : return Create_BamlType_LinearDecimalKeyFrame(isBamlType, useV3Rules);
                case 1872596098 : return Create_BamlType_GroupBox(isBamlType, useV3Rules);
                case 1886012771 : return Create_BamlType_SByteConverter(isBamlType, useV3Rules); // type converter
                case 1888712354 : return Create_BamlType_SplineVectorKeyFrame(isBamlType, useV3Rules);
                case 1894131576 : return Create_BamlType_ToolTipService(isBamlType, useV3Rules);
                case 1899232249 : return Create_BamlType_DockPanel(isBamlType, useV3Rules);
                case 1899479598 : return Create_BamlType_GeneralTransformCollection(isBamlType, useV3Rules);
                case 1908047602 : return Create_BamlType_InputScope(isBamlType, useV3Rules);
                case 1908918452 : return Create_BamlType_Int32CollectionConverter(isBamlType, useV3Rules); // type converter
                case 1912422369 : return Create_BamlType_LineBreak(isBamlType, useV3Rules);
                case 1921765486 : return Create_BamlType_HostVisual(isBamlType, useV3Rules);
                case 1930264739 : return Create_BamlType_ObjectAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 1941591540 : return Create_BamlType_ListBoxItem(isBamlType, useV3Rules);
                case 1949943781 : return Create_BamlType_Point3DConverter(isBamlType, useV3Rules); // type converter
                case 1950874384 : return Create_BamlType_Expression(isBamlType, useV3Rules);
                case 1952565839 : return Create_BamlType_BitmapDecoder(isBamlType, useV3Rules);
                case 1961606018 : return Create_BamlType_SingleAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 1972001235 : return Create_BamlType_PathFigureCollection(isBamlType, useV3Rules);
                case 1974320711 : return Create_BamlType_Rotation3D(isBamlType, useV3Rules);
                case 1977826323 : return Create_BamlType_InputScopeNameConverter(isBamlType, useV3Rules); // type converter
                case 1978946399 : return Create_BamlType_PauseStoryboard(isBamlType, useV3Rules);
                case 1981708784 : return Create_BamlType_MatrixAnimationBase(isBamlType, useV3Rules);
                case 1982598063 : return Create_BamlType_Adorner(isBamlType, useV3Rules);
                case 1983189101 : return Create_BamlType_QuaternionAnimationBase(isBamlType, useV3Rules);
                case 1987454919 : return Create_BamlType_SplineThicknessKeyFrame(isBamlType, useV3Rules);
                case 1992540733 : return Create_BamlType_Stretch(isBamlType, useV3Rules);
                case 2001481592 : return Create_BamlType_Window(isBamlType, useV3Rules);
                case 2002174583 : return Create_BamlType_LinearInt32KeyFrame(isBamlType, useV3Rules);
                case 2009851621 : return Create_BamlType_MatrixKeyFrame(isBamlType, useV3Rules);
                case 2011970188 : return Create_BamlType_Int32KeyFrame(isBamlType, useV3Rules);
                case 2012322180 : return Create_BamlType_PasswordBox(isBamlType, useV3Rules);
                case 2020314122 : return Create_BamlType_Italic(isBamlType, useV3Rules);
                case 2020350540 : return Create_BamlType_GeometryModel3D(isBamlType, useV3Rules);
                case 2021668905 : return Create_BamlType_PointLightBase(isBamlType, useV3Rules);
                case 2022237748 : return Create_BamlType_DiscreteMatrixKeyFrame(isBamlType, useV3Rules);
                case 2026683522 : return Create_BamlType_TransformedBitmap(isBamlType, useV3Rules);
                case 2042108315 : return Create_BamlType_ColumnDefinition(isBamlType, useV3Rules);
                case 2043908275 : return Create_BamlType_PenLineCap(isBamlType, useV3Rules);
                case 2045195350 : return Create_BamlType_StickyNoteControl(isBamlType, useV3Rules);
                case 2057591265 : return Create_BamlType_ColorConvertedBitmapExtension(isBamlType, useV3Rules);
                case 2082963390 : return Create_BamlType_CharKeyFrameCollection(isBamlType, useV3Rules);
                case 2086386488 : return Create_BamlType_MatrixTransform3D(isBamlType, useV3Rules);
                case 2090698417 : return Create_BamlType_ResourceKey(isBamlType, useV3Rules);
                case 2090772835 : return Create_BamlType_CharIListConverter(isBamlType, useV3Rules); // type converter
                case 2095403106 : return Create_BamlType_RectKeyFrame(isBamlType, useV3Rules);
                case 2105601597 : return Create_BamlType_Point3DAnimation(isBamlType, useV3Rules);
                case 2116926181 : return Create_BamlType_ListBox(isBamlType, useV3Rules);
                case 2118568062 : return Create_BamlType_NumberSubstitution(isBamlType, useV3Rules);
                case 2134177976 : return Create_BamlType_DrawingBrush(isBamlType, useV3Rules);
                case 2145171279 : return Create_BamlType_InputLanguageManager(isBamlType, useV3Rules);
                case 2147133521 : return Create_BamlType_RepeatBehavior(isBamlType, useV3Rules);
                case 2175789292 : return Create_BamlType_Trigger(isBamlType, useV3Rules);
                case 2181752774 : return Create_BamlType_Hyperlink(isBamlType, useV3Rules);
                case 2187607252 : return Create_BamlType_ContentControl(isBamlType, useV3Rules);
                case 2194377413 : return Create_BamlType_TimeSpan(isBamlType, useV3Rules);
                case 2203399086 : return Create_BamlType_SetterBase(isBamlType, useV3Rules);
                case 2213541446 : return Create_BamlType_FrameworkTemplate(isBamlType, useV3Rules);
                case 2220064835 : return Create_BamlType_Timeline(isBamlType, useV3Rules);
                case 2224116138 : return Create_BamlType_InputScopeConverter(isBamlType, useV3Rules); // type converter
                case 2233723591 : return Create_BamlType_DoubleKeyFrameCollection(isBamlType, useV3Rules);
                case 2239188145 : return Create_BamlType_ControlTemplate(isBamlType, useV3Rules);
                case 2242193787 : return Create_BamlType_StringKeyFrameCollection(isBamlType, useV3Rules);
                case 2242877643 : return Create_BamlType_GestureRecognizer(isBamlType, useV3Rules);
                case 2245298560 : return Create_BamlType_KeyBinding(isBamlType, useV3Rules);
                case 2247557147 : return Create_BamlType_ContentPresenter(isBamlType, useV3Rules);
                case 2270334750 : return Create_BamlType_XmlDataProvider(isBamlType, useV3Rules);
                case 2275985883 : return Create_BamlType_SizeConverter(isBamlType, useV3Rules); // type converter
                case 2283191896 : return Create_BamlType_TableRow(isBamlType, useV3Rules);
                case 2286541048 : return Create_BamlType_Boolean(isBamlType, useV3Rules);
                case 2291279447 : return Create_BamlType_ItemsControl(isBamlType, useV3Rules);
                case 2293688015 : return Create_BamlType_PolyLineSegment(isBamlType, useV3Rules);
                case 2303212540 : return Create_BamlType_Transform3DGroup(isBamlType, useV3Rules);
                case 2305986747 : return Create_BamlType_IComponentConnector(isBamlType, useV3Rules);
                case 2309433103 : return Create_BamlType_TextSearch(isBamlType, useV3Rules);
                case 2316123992 : return Create_BamlType_DrawingCollection(isBamlType, useV3Rules);
                case 2325564233 : return Create_BamlType_DrawingContext(isBamlType, useV3Rules);
                case 2338158216 : return Create_BamlType_JournalEntryUnifiedViewConverter(isBamlType, useV3Rules);
                case 2341092446 : return Create_BamlType_BitmapMetadata(isBamlType, useV3Rules);
                case 2357823000 : return Create_BamlType_MenuBase(isBamlType, useV3Rules);
                case 2357963071 : return Create_BamlType_ListCollectionView(isBamlType, useV3Rules);
                case 2359651825 : return Create_BamlType_Point3D(isBamlType, useV3Rules);
                case 2359651926 : return Create_BamlType_Point4D(isBamlType, useV3Rules);
                case 2361481257 : return Create_BamlType_DiscreteInt16KeyFrame(isBamlType, useV3Rules);
                case 2361798307 : return Create_BamlType_Int32AnimationBase(isBamlType, useV3Rules);
                case 2364198568 : return Create_BamlType_Matrix3D(isBamlType, useV3Rules);
                case 2365227520 : return Create_BamlType_MenuItem(isBamlType, useV3Rules);
                case 2366255821 : return Create_BamlType_Vector3DAnimationBase(isBamlType, useV3Rules);
                case 2368443675 : return Create_BamlType_DecimalKeyFrameCollection(isBamlType, useV3Rules);
                case 2371777274 : return Create_BamlType_InkPresenter(isBamlType, useV3Rules);
                case 2372223669 : return Create_BamlType_Int64KeyFrameCollection(isBamlType, useV3Rules);
                case 2375952462 : return Create_BamlType_CursorConverter(isBamlType, useV3Rules); // type converter
                case 2382404033 : return Create_BamlType_RectangleGeometry(isBamlType, useV3Rules);
                case 2384031129 : return Create_BamlType_RowDefinition(isBamlType, useV3Rules);
                case 2387462803 : return Create_BamlType_StringKeyFrame(isBamlType, useV3Rules);
                case 2395439922 : return Create_BamlType_Rotation3DAnimationBase(isBamlType, useV3Rules);
                case 2397363533 : return Create_BamlType_DiffuseMaterial(isBamlType, useV3Rules);
                case 2399848930 : return Create_BamlType_DiscreteStringKeyFrame(isBamlType, useV3Rules);
                case 2401541526 : return Create_BamlType_ViewBase(isBamlType, useV3Rules);
                case 2405666083 : return Create_BamlType_AnchoredBlock(isBamlType, useV3Rules);
                case 2414694242 : return Create_BamlType_ProjectionCamera(isBamlType, useV3Rules);
                case 2419799105 : return Create_BamlType_EventSetter(isBamlType, useV3Rules);
                case 2431400980 : return Create_BamlType_ContentWrapperAttribute(isBamlType, useV3Rules);
                case 2431643699 : return Create_BamlType_SizeAnimation(isBamlType, useV3Rules);
                case 2439005345 : return Create_BamlType_SizeAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 2464017094 : return Create_BamlType_FontSizeConverter(isBamlType, useV3Rules); // type converter
                case 2480012208 : return Create_BamlType_BooleanToVisibilityConverter(isBamlType, useV3Rules);
                case 2495537998 : return Create_BamlType_Ellipse(isBamlType, useV3Rules);
                case 2496400610 : return Create_BamlType_DataTemplate(isBamlType, useV3Rules);
                case 2500030087 : return Create_BamlType_OrthographicCamera(isBamlType, useV3Rules);
                case 2500854951 : return Create_BamlType_Setter(isBamlType, useV3Rules);
                case 2507216059 : return Create_BamlType_Geometry3D(isBamlType, useV3Rules);
                case 2510108609 : return Create_BamlType_Point3DKeyFrameCollection(isBamlType, useV3Rules);
                case 2510136870 : return Create_BamlType_DoubleAnimationUsingPath(isBamlType, useV3Rules);
                case 2518729620 : return Create_BamlType_KeySpline(isBamlType, useV3Rules);
                case 2522385967 : return Create_BamlType_DiscreteInt32KeyFrame(isBamlType, useV3Rules);
                case 2523498610 : return Create_BamlType_UriTypeConverter(isBamlType, useV3Rules); // type converter
                case 2526122409 : return Create_BamlType_KeyConverter(isBamlType, useV3Rules); // type converter
                case 2537793262 : return Create_BamlType_BitmapSource(isBamlType, useV3Rules);
                case 2540000939 : return Create_BamlType_VectorKeyFrameCollection(isBamlType, useV3Rules);
                case 2546467590 : return Create_BamlType_AdornerDecorator(isBamlType, useV3Rules);
                case 2549694123 : return Create_BamlType_GridViewColumn(isBamlType, useV3Rules);
                case 2563020253 : return Create_BamlType_GuidConverter(isBamlType, useV3Rules); // type converter
                case 2563899293 : return Create_BamlType_StaticExtension(isBamlType, useV3Rules);
                case 2564710999 : return Create_BamlType_StopStoryboard(isBamlType, useV3Rules);
                case 2568847263 : return Create_BamlType_CheckBox(isBamlType, useV3Rules);
                case 2573940685 : return Create_BamlType_CachedBitmap(isBamlType, useV3Rules);
                case 2579083438 : return Create_BamlType_EventTrigger(isBamlType, useV3Rules);
                case 2586667908 : return Create_BamlType_MaterialGroup(isBamlType, useV3Rules);
                case 2588718206 : return Create_BamlType_BindingExpressionBase(isBamlType, useV3Rules);
                case 2590675289 : return Create_BamlType_StatusBar(isBamlType, useV3Rules);
                case 2594318825 : return Create_BamlType_EnumConverter(isBamlType, useV3Rules); // type converter
                case 2599258965 : return Create_BamlType_DateTimeConverter(isBamlType, useV3Rules); // type converter
                case 2603137612 : return Create_BamlType_ComponentResourceKey(isBamlType, useV3Rules);
                case 2604679664 : return Create_BamlType_FigureLengthConverter(isBamlType, useV3Rules); // type converter
                case 2616916250 : return Create_BamlType_CroppedBitmap(isBamlType, useV3Rules);
                case 2622762262 : return Create_BamlType_Int16KeyFrameCollection(isBamlType, useV3Rules);
                case 2625820903 : return Create_BamlType_ItemCollection(isBamlType, useV3Rules);
                case 2630693784 : return Create_BamlType_ComboBoxItem(isBamlType, useV3Rules);
                case 2632968446 : return Create_BamlType_SetterBaseCollection(isBamlType, useV3Rules);
                case 2644858326 : return Create_BamlType_SolidColorBrush(isBamlType, useV3Rules);
                case 2654418985 : return Create_BamlType_DrawingGroup(isBamlType, useV3Rules);
                case 2667804183 : return Create_BamlType_FrameworkTextComposition(isBamlType, useV3Rules);
                case 2677748290 : return Create_BamlType_XmlNamespaceMapping(isBamlType, useV3Rules);
                case 2683039828 : return Create_BamlType_Polygon(isBamlType, useV3Rules);
                case 2685434095 : return Create_BamlType_Block(isBamlType, useV3Rules);
                case 2687716458 : return Create_BamlType_PolyQuadraticBezierSegment(isBamlType, useV3Rules);
                case 2691678720 : return Create_BamlType_Brush(isBamlType, useV3Rules);
                case 2695798729 : return Create_BamlType_DiscreteRectKeyFrame(isBamlType, useV3Rules);
                case 2697498068 : return Create_BamlType_StreamGeometry(isBamlType, useV3Rules);
                case 2697933609 : return Create_BamlType_SplinePointKeyFrame(isBamlType, useV3Rules);
                case 2704854826 : return Create_BamlType_MultiBindingExpression(isBamlType, useV3Rules);
                case 2707718720 : return Create_BamlType_AdornerLayer(isBamlType, useV3Rules);
                case 2712654300 : return Create_BamlType_KeyGesture(isBamlType, useV3Rules);
                case 2714912374 : return Create_BamlType_ColorConvertedBitmap(isBamlType, useV3Rules);
                case 2717418325 : return Create_BamlType_BitmapEncoder(isBamlType, useV3Rules);
                case 2723992168 : return Create_BamlType_ScrollBar(isBamlType, useV3Rules);
                case 2727123374 : return Create_BamlType_SplineDecimalKeyFrame(isBamlType, useV3Rules);
                case 2737207145 : return Create_BamlType_GeometryGroup(isBamlType, useV3Rules);
                case 2741821828 : return Create_BamlType_DependencyProperty(isBamlType, useV3Rules);
                case 2745869284 : return Create_BamlType_TabletDevice(isBamlType, useV3Rules);
                case 2750913568 : return Create_BamlType_TabControl(isBamlType, useV3Rules);
                case 2752355982 : return Create_BamlType_Vector3DKeyFrame(isBamlType, useV3Rules);
                case 2764779975 : return Create_BamlType_SizeKeyFrame(isBamlType, useV3Rules);
                case 2770480768 : return Create_BamlType_FontStretchConverter(isBamlType, useV3Rules); // type converter
                case 2775858686 : return Create_BamlType_DiscreteRotation3DKeyFrame(isBamlType, useV3Rules);
                case 2779938702 : return Create_BamlType_ByteAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 2789494496 : return Create_BamlType_Clock(isBamlType, useV3Rules);
                case 2792556015 : return Create_BamlType_Color(isBamlType, useV3Rules);
                case 2821657175 : return Create_BamlType_StringConverter(isBamlType, useV3Rules); // type converter
                case 2823948821 : return Create_BamlType_PointAnimationBase(isBamlType, useV3Rules);
                case 2825488812 : return Create_BamlType_CollectionViewSource(isBamlType, useV3Rules);
                case 2828266559 : return Create_BamlType_DoubleKeyFrame(isBamlType, useV3Rules);
                case 2829278862 : return Create_BamlType_BmpBitmapDecoder(isBamlType, useV3Rules);
                case 2830133971 : return Create_BamlType_InputBindingCollection(isBamlType, useV3Rules);
                case 2833582507 : return Create_BamlType_PathFigure(isBamlType, useV3Rules);
                case 2836690659 : return Create_BamlType_SplineByteKeyFrame(isBamlType, useV3Rules);
                case 2840652686 : return Create_BamlType_DiscreteDoubleKeyFrame(isBamlType, useV3Rules);
                case 2853214736 : return Create_BamlType_NavigationWindow(isBamlType, useV3Rules);
                case 2854085569 : return Create_BamlType_Control(isBamlType, useV3Rules);
                case 2855103477 : return Create_BamlType_LinearQuaternionKeyFrame(isBamlType, useV3Rules);
                case 2856553785 : return Create_BamlType_GlyphRunDrawing(isBamlType, useV3Rules);
                case 2857244043 : return Create_BamlType_DrawingImage(isBamlType, useV3Rules);
                case 2865322288 : return Create_BamlType_CultureInfoConverter(isBamlType, useV3Rules); // type converter
                case 2867809997 : return Create_BamlType_QuaternionKeyFrameCollection(isBamlType, useV3Rules);
                case 2872636339 : return Create_BamlType_RemoveStoryboard(isBamlType, useV3Rules);
                case 2880449407 : return Create_BamlType_DataTemplateKey(isBamlType, useV3Rules);
                case 2884063696 : return Create_BamlType_FontStretch(isBamlType, useV3Rules);
                case 2884746986 : return Create_BamlType_WrapPanel(isBamlType, useV3Rules);
                case 2892711692 : return Create_BamlType_TiffBitmapDecoder(isBamlType, useV3Rules);
                case 2906462199 : return Create_BamlType_DiscreteThicknessKeyFrame(isBamlType, useV3Rules);
                case 2910782830 : return Create_BamlType_Single(isBamlType, useV3Rules);
                case 2923120250 : return Create_BamlType_Size3D(isBamlType, useV3Rules);
                case 2953557280 : return Create_BamlType_TableCell(isBamlType, useV3Rules);
                case 2954759040 : return Create_BamlType_KeyTime(isBamlType, useV3Rules);
                case 2958853687 : return Create_BamlType_ObjectKeyFrame(isBamlType, useV3Rules);
                case 2971239814 : return Create_BamlType_DiscreteObjectKeyFrame(isBamlType, useV3Rules);
                case 2979874461 : return Create_BamlType_LinearSingleKeyFrame(isBamlType, useV3Rules);
                case 2990868428 : return Create_BamlType_IconBitmapDecoder(isBamlType, useV3Rules);
                case 2992435596 : return Create_BamlType_Orientation(isBamlType, useV3Rules);
                case 2997528560 : return Create_BamlType_PathFigureCollectionConverter(isBamlType, useV3Rules); // type converter
                case 3000815496 : return Create_BamlType_Viewbox(isBamlType, useV3Rules);
                case 3005129221 : return Create_BamlType_SingleAnimationBase(isBamlType, useV3Rules);
                case 3005265189 : return Create_BamlType_TextBoxBase(isBamlType, useV3Rules);
                case 3008357171 : return Create_BamlType_DecimalKeyFrame(isBamlType, useV3Rules);
                case 3016322347 : return Create_BamlType_DoubleAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 3017582335 : return Create_BamlType_ObjectKeyFrameCollection(isBamlType, useV3Rules);
                case 3024874406 : return Create_BamlType_VectorAnimationBase(isBamlType, useV3Rules);
                case 3031820371 : return Create_BamlType_VideoDrawing(isBamlType, useV3Rules);
                case 3035410420 : return Create_BamlType_TypeTypeConverter(isBamlType, useV3Rules); // type converter
                case 3040108565 : return Create_BamlType_HeaderedItemsControl(isBamlType, useV3Rules);
                case 3040714245 : return Create_BamlType_BoolIListConverter(isBamlType, useV3Rules); // type converter
                case 3042527602 : return Create_BamlType_ResumeStoryboard(isBamlType, useV3Rules);
                case 3055664686 : return Create_BamlType_SkewTransform(isBamlType, useV3Rules);
                case 3056264338 : return Create_BamlType_GridViewHeaderRowPresenter(isBamlType, useV3Rules);
                case 3061725932 : return Create_BamlType_FrameworkElement(isBamlType, useV3Rules);
                case 3062728027 : return Create_BamlType_DiscreteBooleanKeyFrame(isBamlType, useV3Rules);
                case 3077777987 : return Create_BamlType_MediaTimeline(isBamlType, useV3Rules);
                case 3078955044 : return Create_BamlType_FontStyle(isBamlType, useV3Rules);
                case 3080628638 : return Create_BamlType_SplineDoubleKeyFrame(isBamlType, useV3Rules);
                case 3087488479 : return Create_BamlType_Object(isBamlType, useV3Rules);
                case 3091177486 : return Create_BamlType_PointCollection(isBamlType, useV3Rules);
                case 3098873083 : return Create_BamlType_LinearByteKeyFrame(isBamlType, useV3Rules);
                case 3100133790 : return Create_BamlType_Rotation3DKeyFrameCollection(isBamlType, useV3Rules);
                case 3107715695 : return Create_BamlType_Frame(isBamlType, useV3Rules);
                case 3109717207 : return Create_BamlType_Int16AnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 3114475758 : return Create_BamlType_FlowDocumentPageViewer(isBamlType, useV3Rules);
                case 3114500727 : return Create_BamlType_BindingExpression(isBamlType, useV3Rules);
                case 3119883437 : return Create_BamlType_XamlPointCollectionSerializer(isBamlType, useV3Rules);
                case 3120891186 : return Create_BamlType_DurationConverter(isBamlType, useV3Rules); // type converter
                case 3124512808 : return Create_BamlType_StreamResourceInfo(isBamlType, useV3Rules);
                case 3131559342 : return Create_BamlType_QuaternionKeyFrame(isBamlType, useV3Rules);
                case 3131853152 : return Create_BamlType_XamlBrushSerializer(isBamlType, useV3Rules);
                case 3133507978 : return Create_BamlType_TabItem(isBamlType, useV3Rules);
                case 3134642154 : return Create_BamlType_SkipStoryboardToFill(isBamlType, useV3Rules);
                case 3150804609 : return Create_BamlType_InPlaceBitmapMetadataWriter(isBamlType, useV3Rules);
                case 3156616619 : return Create_BamlType_TimelineCollection(isBamlType, useV3Rules);
                case 3157049388 : return Create_BamlType_BitmapPalette(isBamlType, useV3Rules);
                case 3160936067 : return Create_BamlType_ByteAnimationBase(isBamlType, useV3Rules);
                case 3168456847 : return Create_BamlType_ColorKeyFrameCollection(isBamlType, useV3Rules);
                case 3179498907 : return Create_BamlType_DoubleCollectionConverter(isBamlType, useV3Rules); // type converter
                case 3184112475 : return Create_BamlType_ThicknessAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 3187081615 : return Create_BamlType_BitmapEffectGroup(isBamlType, useV3Rules);
                case 3191294337 : return Create_BamlType_SetStoryboardSpeedRatio(isBamlType, useV3Rules);
                case 3213500826 : return Create_BamlType_MenuScrollingVisibilityConverter(isBamlType, useV3Rules);
                case 3217781231 : return Create_BamlType_Slider(isBamlType, useV3Rules);
                case 3223906597 : return Create_BamlType_ScrollViewer(isBamlType, useV3Rules);
                case 3225341700 : return Create_BamlType_BitmapFrame(isBamlType, useV3Rules);
                case 3232444943 : return Create_BamlType_SizeKeyFrameCollection(isBamlType, useV3Rules);
                case 3239409257 : return Create_BamlType_Point3DCollection(isBamlType, useV3Rules);
                case 3245663526 : return Create_BamlType_ItemsPresenter(isBamlType, useV3Rules);
                case 3249372079 : return Create_BamlType_ItemContainerTemplateKey(isBamlType, useV3Rules);
                case 3251319004 : return Create_BamlType_StylusDevice(isBamlType, useV3Rules);
                case 3253060368 : return Create_BamlType_SplineInt64KeyFrame(isBamlType, useV3Rules);
                case 3254666903 : return Create_BamlType_BlurBitmapEffect(isBamlType, useV3Rules);
                case 3269592841 : return Create_BamlType_TimeSpanConverter(isBamlType, useV3Rules); // type converter
                case 3273980620 : return Create_BamlType_ByteKeyFrameCollection(isBamlType, useV3Rules);
                case 3277780192 : return Create_BamlType_StrokeCollection(isBamlType, useV3Rules);
                case 3310116788 : return Create_BamlType_Model3DCollection(isBamlType, useV3Rules);
                case 3319196847 : return Create_BamlType_ControllableStoryboardAction(isBamlType, useV3Rules);
                case 3326778732 : return Create_BamlType_Int32KeyFrameCollection(isBamlType, useV3Rules);
                case 3332504754 : return Create_BamlType_DirectionalLight(isBamlType, useV3Rules);
                case 3335227078 : return Create_BamlType_TemplateBindingExpressionConverter(isBamlType, useV3Rules); // type converter
                case 3337633457 : return Create_BamlType_MatrixConverter(isBamlType, useV3Rules); // type converter
                case 3337984719 : return Create_BamlType_Visual3D(isBamlType, useV3Rules);
                case 3342933789 : return Create_BamlType_SplineQuaternionKeyFrame(isBamlType, useV3Rules);
                case 3347030199 : return Create_BamlType_MultiTrigger(isBamlType, useV3Rules);
                case 3361683901 : return Create_BamlType_XmlLanguageConverter(isBamlType, useV3Rules); // type converter
                case 3363408079 : return Create_BamlType_ListItem(isBamlType, useV3Rules);
                case 3365175598 : return Create_BamlType_DiscreteSizeKeyFrame(isBamlType, useV3Rules);
                case 3376689791 : return Create_BamlType_ListView(isBamlType, useV3Rules);
                case 3388398850 : return Create_BamlType_RectConverter(isBamlType, useV3Rules); // type converter
                case 3391091418 : return Create_BamlType_BitmapEffectInput(isBamlType, useV3Rules);
                case 3402641106 : return Create_BamlType_ItemContainerTemplate(isBamlType, useV3Rules);
                case 3402758583 : return Create_BamlType_ButtonBase(isBamlType, useV3Rules);
                case 3402930733 : return Create_BamlType_CharAnimationBase(isBamlType, useV3Rules);
                case 3404714779 : return Create_BamlType_CommandConverter(isBamlType, useV3Rules); // type converter
                case 3408554657 : return Create_BamlType_LinearPointKeyFrame(isBamlType, useV3Rules);
                case 3414744787 : return Create_BamlType_Image(isBamlType, useV3Rules);
                case 3415963406 : return Create_BamlType_Int16(isBamlType, useV3Rules);
                case 3415963604 : return Create_BamlType_Int32(isBamlType, useV3Rules);
                case 3415963909 : return Create_BamlType_Int64(isBamlType, useV3Rules);
                case 3418350262 : return Create_BamlType_PointKeyFrame(isBamlType, useV3Rules);
                case 3421308423 : return Create_BamlType_UInt16(isBamlType, useV3Rules);
                case 3421308621 : return Create_BamlType_UInt32(isBamlType, useV3Rules);
                case 3421308926 : return Create_BamlType_UInt64(isBamlType, useV3Rules);
                case 3431074367 : return Create_BamlType_ToolBarOverflowPanel(isBamlType, useV3Rules);
                case 3440459974 : return Create_BamlType_InkCanvas(isBamlType, useV3Rules);
                case 3446789391 : return Create_BamlType_FocusManager(isBamlType, useV3Rules);
                case 3448499789 : return Create_BamlType_Matrix(isBamlType, useV3Rules);
                case 3462744685 : return Create_BamlType_Floater(isBamlType, useV3Rules);
                case 3491907900 : return Create_BamlType_LinearPoint3DKeyFrame(isBamlType, useV3Rules);
                case 3493262121 : return Create_BamlType_DropShadowBitmapEffect(isBamlType, useV3Rules);
                case 3503874477 : return Create_BamlType_LinearVector3DKeyFrame(isBamlType, useV3Rules);
                case 3505868102 : return Create_BamlType_Cursor(isBamlType, useV3Rules);
                case 3515909783 : return Create_BamlType_Viewport3D(isBamlType, useV3Rules);
                case 3517750932 : return Create_BamlType_ParallelTimeline(isBamlType, useV3Rules);
                case 3521445823 : return Create_BamlType_StringAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 3523046863 : return Create_BamlType_MaterialCollection(isBamlType, useV3Rules);
                case 3527208580 : return Create_BamlType_RenderOptions(isBamlType, useV3Rules);
                case 3532370792 : return Create_BamlType_BitmapImage(isBamlType, useV3Rules);
                case 3538186783 : return Create_BamlType_Binding(isBamlType, useV3Rules);
                case 3544056666 : return Create_BamlType_RectAnimation(isBamlType, useV3Rules);
                case 3545069055 : return Create_BamlType_FontWeight(isBamlType, useV3Rules);
                case 3545729620 : return Create_BamlType_KeySplineConverter(isBamlType, useV3Rules); // type converter
                case 3551608724 : return Create_BamlType_Int32Converter(isBamlType, useV3Rules); // type converter
                case 3557950823 : return Create_BamlType_VisualTarget(isBamlType, useV3Rules);
                case 3564716316 : return Create_BamlType_RangeBase(isBamlType, useV3Rules);
                case 3564745150 : return Create_BamlType_CharConverter(isBamlType, useV3Rules); // type converter
                case 3567044273 : return Create_BamlType_MatrixAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 3574070525 : return Create_BamlType_RepeatButton(isBamlType, useV3Rules);
                case 3578060752 : return Create_BamlType_RoutedUICommand(isBamlType, useV3Rules);
                case 3580418462 : return Create_BamlType_Point4DConverter(isBamlType, useV3Rules); // type converter
                case 3581886304 : return Create_BamlType_PolyBezierSegment(isBamlType, useV3Rules);
                case 3594131348 : return Create_BamlType_BmpBitmapEncoder(isBamlType, useV3Rules);
                case 3597588278 : return Create_BamlType_StringAnimationBase(isBamlType, useV3Rules);
                case 3603120821 : return Create_BamlType_TemplateKey(isBamlType, useV3Rules);
                case 3610917888 : return Create_BamlType_FlowDocumentScrollViewer(isBamlType, useV3Rules);
                case 3613077086 : return Create_BamlType_GeneralTransform(isBamlType, useV3Rules);
                case 3626508971 : return Create_BamlType_InputBinding(isBamlType, useV3Rules);
                case 3626720937 : return Create_BamlType_TableRowGroup(isBamlType, useV3Rules);
                case 3627100744 : return Create_BamlType_AnimationClock(isBamlType, useV3Rules);
                case 3633058264 : return Create_BamlType_Drawing(isBamlType, useV3Rules);
                case 3638153921 : return Create_BamlType_RelativeSource(isBamlType, useV3Rules);
                case 3639115055 : return Create_BamlType_BooleanAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 3646628024 : return Create_BamlType_Matrix3DConverter(isBamlType, useV3Rules); // type converter
                case 3656924396 : return Create_BamlType_PathSegment(isBamlType, useV3Rules);
                case 3657564178 : return Create_BamlType_TiffBitmapEncoder(isBamlType, useV3Rules);
                case 3660631367 : return Create_BamlType_TabPanel(isBamlType, useV3Rules);
                case 3661012133 : return Create_BamlType_LinearGradientBrush(isBamlType, useV3Rules);
                case 3666191229 : return Create_BamlType_Selector(isBamlType, useV3Rules);
                case 3666411286 : return Create_BamlType_SingleConverter(isBamlType, useV3Rules); // type converter
                case 3670807738 : return Create_BamlType_GradientBrush(isBamlType, useV3Rules);
                case 3681601765 : return Create_BamlType_GlyphRun(isBamlType, useV3Rules);
                case 3692579028 : return Create_BamlType_Application(isBamlType, useV3Rules);
                case 3693227583 : return Create_BamlType_WmpBitmapDecoder(isBamlType, useV3Rules);
                case 3705322145 : return Create_BamlType_DateTime(isBamlType, useV3Rules);
                case 3707266540 : return Create_BamlType_Int32Animation(isBamlType, useV3Rules);
                case 3711361102 : return Create_BamlType_Figure(isBamlType, useV3Rules);
                case 3714572384 : return Create_BamlType_Label(isBamlType, useV3Rules);
                case 3722866108 : return Create_BamlType_Light(isBamlType, useV3Rules);
                case 3725053631 : return Create_BamlType_EmbossBitmapEffect(isBamlType, useV3Rules);
                case 3726867806 : return Create_BamlType_FlowDocumentReader(isBamlType, useV3Rules);
                case 3734175699 : return Create_BamlType_PixelFormatConverter(isBamlType, useV3Rules); // type converter
                case 3741909743 : return Create_BamlType_FixedDocument(isBamlType, useV3Rules);
                case 3743141867 : return Create_BamlType_PointIListConverter(isBamlType, useV3Rules); // type converter
                case 3746015879 : return Create_BamlType_XamlWriter(isBamlType, useV3Rules);
                case 3746078580 : return Create_BamlType_RectAnimationUsingKeyFrames(isBamlType, useV3Rules);
                case 3761318360 : return Create_BamlType_ContentPropertyAttribute(isBamlType, useV3Rules);
                case 3780531133 : return Create_BamlType_TransformGroup(isBamlType, useV3Rules);
                case 3797319853 : return Create_BamlType_TextDecorationCollection(isBamlType, useV3Rules);
                case 3800911244 : return Create_BamlType_Vector3DCollectionConverter(isBamlType, useV3Rules); // type converter
                case 3822069102 : return Create_BamlType_SingleAnimation(isBamlType, useV3Rules);
                case 3831772414 : return Create_BamlType_Int32Rect(isBamlType, useV3Rules);
                case 3850431872 : return Create_BamlType_ScaleTransform(isBamlType, useV3Rules);
                case 3862075430 : return Create_BamlType_PointConverter(isBamlType, useV3Rules); // type converter
                case 3870219055 : return Create_BamlType_LostFocusEventManager(isBamlType, useV3Rules);
                case 3870757565 : return Create_BamlType_MatrixKeyFrameCollection(isBamlType, useV3Rules);
                case 3894194634 : return Create_BamlType_IAddChild(isBamlType, useV3Rules);
                case 3894580134 : return Create_BamlType_EllipseGeometry(isBamlType, useV3Rules);
                case 3895289908 : return Create_BamlType_AmbientLight(isBamlType, useV3Rules);
                case 3903386330 : return Create_BamlType_RelativeSourceMode(isBamlType, useV3Rules);
                case 3908473888 : return Create_BamlType_SoundPlayerAction(isBamlType, useV3Rules);
                case 3908606180 : return Create_BamlType_DrawingVisual(isBamlType, useV3Rules);
                case 3921789478 : return Create_BamlType_Vector3DCollection(isBamlType, useV3Rules);
                case 3924959263 : return Create_BamlType_Transform3D(isBamlType, useV3Rules);
                case 3925684252 : return Create_BamlType_Transform(isBamlType, useV3Rules);
                case 3927457614 : return Create_BamlType_QuadraticBezierSegment(isBamlType, useV3Rules);
                case 3928766041 : return Create_BamlType_DiscretePointKeyFrame(isBamlType, useV3Rules);
                case 3938642252 : return Create_BamlType_GridViewColumnHeader(isBamlType, useV3Rules);
                case 3944256480 : return Create_BamlType_NullExtension(isBamlType, useV3Rules);
                case 3948871275 : return Create_BamlType_Vector(isBamlType, useV3Rules);
                case 3950543104 : return Create_BamlType_ImageSourceConverter(isBamlType, useV3Rules); // type converter
                case 3957573606 : return Create_BamlType_LinearRotation3DKeyFrame(isBamlType, useV3Rules);
                case 3963681416 : return Create_BamlType_LinearInt64KeyFrame(isBamlType, useV3Rules);
                case 3965893796 : return Create_BamlType_DocumentReferenceCollection(isBamlType, useV3Rules);
                case 3969230566 : return Create_BamlType_SingleKeyFrame(isBamlType, useV3Rules);
                case 3973477021 : return Create_BamlType_Int64KeyFrame(isBamlType, useV3Rules);
                case 3977525494 : return Create_BamlType_NavigationUIVisibility(isBamlType, useV3Rules);
                case 3981616693 : return Create_BamlType_DiscreteSingleKeyFrame(isBamlType, useV3Rules);
                case 3991721253 : return Create_BamlType_RoutedEventConverter(isBamlType, useV3Rules); // type converter
                case 4017733246 : return Create_BamlType_PointAnimation(isBamlType, useV3Rules);
                case 4029087036 : return Create_BamlType_VirtualizingPanel(isBamlType, useV3Rules);
                case 4029842000 : return Create_BamlType_ImageSource(isBamlType, useV3Rules);
                case 4034507500 : return Create_BamlType_ByteKeyFrame(isBamlType, useV3Rules);
                case 4035632042 : return Create_BamlType_ObjectAnimationBase(isBamlType, useV3Rules);
                case 4039072664 : return Create_BamlType_XamlTemplateSerializer(isBamlType, useV3Rules);
                case 4043614448 : return Create_BamlType_CultureInfoIetfLanguageTagConverter(isBamlType, useV3Rules); // type converter
                case 4048739983 : return Create_BamlType_VectorCollectionConverter(isBamlType, useV3Rules); // type converter
                case 4056415476 : return Create_BamlType_BezierSegment(isBamlType, useV3Rules);
                case 4056944842 : return Create_BamlType_SpellCheck(isBamlType, useV3Rules);
                case 4059589051 : return Create_BamlType_String(isBamlType, useV3Rules);
                case 4059695088 : return Create_BamlType_BooleanKeyFrameCollection(isBamlType, useV3Rules);
                case 4061092706 : return Create_BamlType_TreeViewItem(isBamlType, useV3Rules);
                case 4064409625 : return Create_BamlType_FontFamilyConverter(isBamlType, useV3Rules); // type converter
                case 4066832480 : return Create_BamlType_Stylus(isBamlType, useV3Rules);
                case 4070164174 : return Create_BamlType_DependencyObject(isBamlType, useV3Rules);
                case 4080336864 : return Create_BamlType_GridLengthConverter(isBamlType, useV3Rules); // type converter
                case 4083954870 : return Create_BamlType_BitmapEffectCollection(isBamlType, useV3Rules);
                case 4093700264 : return Create_BamlType_GradientStop(isBamlType, useV3Rules);
                case 4095303241 : return Create_BamlType_Int64Converter(isBamlType, useV3Rules); // type converter
                case 4099991372 : return Create_BamlType_INameScope(isBamlType, useV3Rules);
                case 4114749745 : return Create_BamlType_UInt32Converter(isBamlType, useV3Rules); // type converter
                case 4130936400 : return Create_BamlType_Panel(isBamlType, useV3Rules);
                case 4135277332 : return Create_BamlType_Model3D(isBamlType, useV3Rules);
                case 4138410030 : return Create_BamlType_VirtualizingStackPanel(isBamlType, useV3Rules);
                case 4145310526 : return Create_BamlType_Point(isBamlType, useV3Rules);
                case 4145382636 : return Create_BamlType_Popup(isBamlType, useV3Rules);
                case 4147170844 : return Create_BamlType_MatrixAnimationUsingPath(isBamlType, useV3Rules);
                case 4147517467 : return Create_BamlType_InputManager(isBamlType, useV3Rules);
                case 4156353754 : return Create_BamlType_Duration(isBamlType, useV3Rules);
                case 4156625073 : return Create_BamlType_DataChangedEventManager(isBamlType, useV3Rules);
                case 4199195260 : return Create_BamlType_TransformCollection(isBamlType, useV3Rules);
                case 4200653599 : return Create_BamlType_DocumentPageView(isBamlType, useV3Rules);
                case 4202675952 : return Create_BamlType_TimelineGroup(isBamlType, useV3Rules);
                case 4205340967 : return Create_BamlType_PerspectiveCamera(isBamlType, useV3Rules);
                case 4212267451 : return Create_BamlType_AccessText(isBamlType, useV3Rules);
                case 4219821822 : return Create_BamlType_RoutedCommand(isBamlType, useV3Rules);
                case 4221592645 : return Create_BamlType_SplineSingleKeyFrame(isBamlType, useV3Rules);
                case 4227383631 : return Create_BamlType_ImageBrush(isBamlType, useV3Rules);
                case 4232579606 : return Create_BamlType_Vector3D(isBamlType, useV3Rules);
                case 4233318098 : return Create_BamlType_StackPanel(isBamlType, useV3Rules);
                case 4234029471 : return Create_BamlType_Rotation3DKeyFrame(isBamlType, useV3Rules);
                case 4239529341 : return Create_BamlType_PriorityBinding(isBamlType, useV3Rules);
                case 4243618870 : return Create_BamlType_PointCollectionConverter(isBamlType, useV3Rules); // type converter
                case 4250838544 : return Create_BamlType_ArrayExtension(isBamlType, useV3Rules);
                case 4250961057 : return Create_BamlType_Int64Animation(isBamlType, useV3Rules);
                case 4259355998 : return Create_BamlType_DiscreteDecimalKeyFrame(isBamlType, useV3Rules);
                case 4260680252 : return Create_BamlType_VisualBrush(isBamlType, useV3Rules);
                case 4265248728 : return Create_BamlType_DynamicResourceExtensionConverter(isBamlType, useV3Rules); // type converter
                case 4268703175 : return Create_BamlType_VectorConverter(isBamlType, useV3Rules); // type converter
                case 4291638393 : return Create_BamlType_WeakEventManager(isBamlType, useV3Rules);
                default : return null;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AccessText(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              1, "AccessText",
                                              typeof(System.Windows.Controls.AccessText),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.AccessText(); };
            bamlType.ContentPropertyName = "Text";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AdornedElementPlaceholder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              2, "AdornedElementPlaceholder",
                                              typeof(System.Windows.Controls.AdornedElementPlaceholder),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.AdornedElementPlaceholder(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Adorner(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              3, "Adorner",
                                              typeof(System.Windows.Documents.Adorner),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AdornerDecorator(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              4, "AdornerDecorator",
                                              typeof(System.Windows.Documents.AdornerDecorator),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.AdornerDecorator(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AdornerLayer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              5, "AdornerLayer",
                                              typeof(System.Windows.Documents.AdornerLayer),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AffineTransform3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              6, "AffineTransform3D",
                                              typeof(System.Windows.Media.Media3D.AffineTransform3D),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AmbientLight(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              7, "AmbientLight",
                                              typeof(System.Windows.Media.Media3D.AmbientLight),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.AmbientLight(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AnchoredBlock(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              8, "AnchoredBlock",
                                              typeof(System.Windows.Documents.AnchoredBlock),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Blocks";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Animatable(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              9, "Animatable",
                                              typeof(System.Windows.Media.Animation.Animatable),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AnimationClock(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              10, "AnimationClock",
                                              typeof(System.Windows.Media.Animation.AnimationClock),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AnimationTimeline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              11, "AnimationTimeline",
                                              typeof(System.Windows.Media.Animation.AnimationTimeline),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Application(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              12, "Application",
                                              typeof(System.Windows.Application),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Application(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ArcSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              13, "ArcSegment",
                                              typeof(System.Windows.Media.ArcSegment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.ArcSegment(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ArrayExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              14, "ArrayExtension",
                                              typeof(System.Windows.Markup.ArrayExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.ArrayExtension(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Type) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.Markup.ArrayExtension(
                                     (System.Type)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_AxisAngleRotation3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              15, "AxisAngleRotation3D",
                                              typeof(System.Windows.Media.Media3D.AxisAngleRotation3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.AxisAngleRotation3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BaseIListConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              16, "BaseIListConverter",
                                              typeof(System.Windows.Media.Converters.BaseIListConverter),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BeginStoryboard(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              17, "BeginStoryboard",
                                              typeof(System.Windows.Media.Animation.BeginStoryboard),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.BeginStoryboard(); };
            bamlType.ContentPropertyName = "Storyboard";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BevelBitmapEffect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              18, "BevelBitmapEffect",
                                              typeof(System.Windows.Media.Effects.BevelBitmapEffect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.BevelBitmapEffect(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BezierSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              19, "BezierSegment",
                                              typeof(System.Windows.Media.BezierSegment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.BezierSegment(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Binding(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              20, "Binding",
                                              typeof(System.Windows.Data.Binding),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.Binding(); };
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.String) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.Data.Binding(
                                     (System.String)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BindingBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              21, "BindingBase",
                                              typeof(System.Windows.Data.BindingBase),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BindingExpression(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              22, "BindingExpression",
                                              typeof(System.Windows.Data.BindingExpression),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.ExpressionConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BindingExpressionBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              23, "BindingExpressionBase",
                                              typeof(System.Windows.Data.BindingExpressionBase),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.ExpressionConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BindingListCollectionView(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              24, "BindingListCollectionView",
                                              typeof(System.Windows.Data.BindingListCollectionView),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              25, "BitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.BitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapEffect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              26, "BitmapEffect",
                                              typeof(System.Windows.Media.Effects.BitmapEffect),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapEffectCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              27, "BitmapEffectCollection",
                                              typeof(System.Windows.Media.Effects.BitmapEffectCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.BitmapEffectCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapEffectGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              28, "BitmapEffectGroup",
                                              typeof(System.Windows.Media.Effects.BitmapEffectGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.BitmapEffectGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapEffectInput(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              29, "BitmapEffectInput",
                                              typeof(System.Windows.Media.Effects.BitmapEffectInput),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.BitmapEffectInput(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapEncoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              30, "BitmapEncoder",
                                              typeof(System.Windows.Media.Imaging.BitmapEncoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              31, "BitmapFrame",
                                              typeof(System.Windows.Media.Imaging.BitmapFrame),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapImage(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              32, "BitmapImage",
                                              typeof(System.Windows.Media.Imaging.BitmapImage),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.BitmapImage(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapMetadata(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              33, "BitmapMetadata",
                                              typeof(System.Windows.Media.Imaging.BitmapMetadata),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapPalette(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              34, "BitmapPalette",
                                              typeof(System.Windows.Media.Imaging.BitmapPalette),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BitmapSource(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              35, "BitmapSource",
                                              typeof(System.Windows.Media.Imaging.BitmapSource),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Block(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              36, "Block",
                                              typeof(System.Windows.Documents.Block),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BlockUIContainer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              37, "BlockUIContainer",
                                              typeof(System.Windows.Documents.BlockUIContainer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.BlockUIContainer(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BlurBitmapEffect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              38, "BlurBitmapEffect",
                                              typeof(System.Windows.Media.Effects.BlurBitmapEffect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.BlurBitmapEffect(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BmpBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              39, "BmpBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.BmpBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BmpBitmapEncoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              40, "BmpBitmapEncoder",
                                              typeof(System.Windows.Media.Imaging.BmpBitmapEncoder),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.BmpBitmapEncoder(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Bold(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              41, "Bold",
                                              typeof(System.Windows.Documents.Bold),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Bold(); };
            bamlType.ContentPropertyName = "Inlines";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BoolIListConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              42, "BoolIListConverter",
                                              typeof(System.Windows.Media.Converters.BoolIListConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Converters.BoolIListConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Boolean(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              43, "Boolean",
                                              typeof(System.Boolean),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Boolean(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.BooleanConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BooleanAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              44, "BooleanAnimationBase",
                                              typeof(System.Windows.Media.Animation.BooleanAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BooleanAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              45, "BooleanAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BooleanConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              46, "BooleanConverter",
                                              typeof(System.ComponentModel.BooleanConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.BooleanConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BooleanKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              47, "BooleanKeyFrame",
                                              typeof(System.Windows.Media.Animation.BooleanKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BooleanKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              48, "BooleanKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.BooleanKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.BooleanKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BooleanToVisibilityConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              49, "BooleanToVisibilityConverter",
                                              typeof(System.Windows.Controls.BooleanToVisibilityConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.BooleanToVisibilityConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Border(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              50, "Border",
                                              typeof(System.Windows.Controls.Border),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Border(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BorderGapMaskConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              51, "BorderGapMaskConverter",
                                              typeof(System.Windows.Controls.BorderGapMaskConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.BorderGapMaskConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Brush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              52, "Brush",
                                              typeof(System.Windows.Media.Brush),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BrushConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              53, "BrushConverter",
                                              typeof(System.Windows.Media.BrushConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.BrushConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_BulletDecorator(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              54, "BulletDecorator",
                                              typeof(System.Windows.Controls.Primitives.BulletDecorator),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.BulletDecorator(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Button(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              55, "Button",
                                              typeof(System.Windows.Controls.Button),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Button(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ButtonBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              56, "ButtonBase",
                                              typeof(System.Windows.Controls.Primitives.ButtonBase),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Byte(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              57, "Byte",
                                              typeof(System.Byte),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Byte(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.ByteConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ByteAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              58, "ByteAnimation",
                                              typeof(System.Windows.Media.Animation.ByteAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ByteAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ByteAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              59, "ByteAnimationBase",
                                              typeof(System.Windows.Media.Animation.ByteAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ByteAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              60, "ByteAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.ByteAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ByteAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ByteConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              61, "ByteConverter",
                                              typeof(System.ComponentModel.ByteConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.ByteConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ByteKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              62, "ByteKeyFrame",
                                              typeof(System.Windows.Media.Animation.ByteKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ByteKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              63, "ByteKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.ByteKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ByteKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CachedBitmap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              64, "CachedBitmap",
                                              typeof(System.Windows.Media.Imaging.CachedBitmap),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Camera(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              65, "Camera",
                                              typeof(System.Windows.Media.Media3D.Camera),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Canvas(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              66, "Canvas",
                                              typeof(System.Windows.Controls.Canvas),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Canvas(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Char(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              67, "Char",
                                              typeof(System.Char),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Char(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.CharConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CharAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              68, "CharAnimationBase",
                                              typeof(System.Windows.Media.Animation.CharAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CharAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              69, "CharAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.CharAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.CharAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CharConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              70, "CharConverter",
                                              typeof(System.ComponentModel.CharConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.CharConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CharIListConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              71, "CharIListConverter",
                                              typeof(System.Windows.Media.Converters.CharIListConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Converters.CharIListConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CharKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              72, "CharKeyFrame",
                                              typeof(System.Windows.Media.Animation.CharKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CharKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              73, "CharKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.CharKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.CharKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CheckBox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              74, "CheckBox",
                                              typeof(System.Windows.Controls.CheckBox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.CheckBox(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Clock(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              75, "Clock",
                                              typeof(System.Windows.Media.Animation.Clock),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ClockController(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              76, "ClockController",
                                              typeof(System.Windows.Media.Animation.ClockController),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ClockGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              77, "ClockGroup",
                                              typeof(System.Windows.Media.Animation.ClockGroup),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CollectionContainer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              78, "CollectionContainer",
                                              typeof(System.Windows.Data.CollectionContainer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.CollectionContainer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CollectionView(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              79, "CollectionView",
                                              typeof(System.Windows.Data.CollectionView),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CollectionViewSource(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              80, "CollectionViewSource",
                                              typeof(System.Windows.Data.CollectionViewSource),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.CollectionViewSource(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Color(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              81, "Color",
                                              typeof(System.Windows.Media.Color),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Color(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.ColorConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              82, "ColorAnimation",
                                              typeof(System.Windows.Media.Animation.ColorAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ColorAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              83, "ColorAnimationBase",
                                              typeof(System.Windows.Media.Animation.ColorAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              84, "ColorAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.ColorAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ColorAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorConvertedBitmap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              85, "ColorConvertedBitmap",
                                              typeof(System.Windows.Media.Imaging.ColorConvertedBitmap),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.ColorConvertedBitmap(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorConvertedBitmapExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              86, "ColorConvertedBitmapExtension",
                                              typeof(System.Windows.ColorConvertedBitmapExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.ColorConvertedBitmapExtension(); };
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Object) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.ColorConvertedBitmapExtension(
                                     (System.Object)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              87, "ColorConverter",
                                              typeof(System.Windows.Media.ColorConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.ColorConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              88, "ColorKeyFrame",
                                              typeof(System.Windows.Media.Animation.ColorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColorKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              89, "ColorKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.ColorKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ColorKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColumnDefinition(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              90, "ColumnDefinition",
                                              typeof(System.Windows.Controls.ColumnDefinition),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ColumnDefinition(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CombinedGeometry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              91, "CombinedGeometry",
                                              typeof(System.Windows.Media.CombinedGeometry),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.CombinedGeometry(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ComboBox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              92, "ComboBox",
                                              typeof(System.Windows.Controls.ComboBox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ComboBox(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ComboBoxItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              93, "ComboBoxItem",
                                              typeof(System.Windows.Controls.ComboBoxItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ComboBoxItem(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CommandConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              94, "CommandConverter",
                                              typeof(System.Windows.Input.CommandConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.CommandConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ComponentResourceKey(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              95, "ComponentResourceKey",
                                              typeof(System.Windows.ComponentResourceKey),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.ComponentResourceKey(); };
            bamlType.TypeConverterType = typeof(System.Windows.Markup.ComponentResourceKeyConverter);
            bamlType.Constructors.Add(2, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Type), typeof(System.Object) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.ComponentResourceKey(
                                     (System.Type)arguments[0],
                                     (System.Object)arguments[1]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ComponentResourceKeyConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              96, "ComponentResourceKeyConverter",
                                              typeof(System.Windows.Markup.ComponentResourceKeyConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.ComponentResourceKeyConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CompositionTarget(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              97, "CompositionTarget",
                                              typeof(System.Windows.Media.CompositionTarget),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Condition(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              98, "Condition",
                                              typeof(System.Windows.Condition),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Condition(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContainerVisual(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              99, "ContainerVisual",
                                              typeof(System.Windows.Media.ContainerVisual),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.ContainerVisual(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContentControl(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              100, "ContentControl",
                                              typeof(System.Windows.Controls.ContentControl),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ContentControl(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContentElement(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              101, "ContentElement",
                                              typeof(System.Windows.ContentElement),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.ContentElement(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContentPresenter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              102, "ContentPresenter",
                                              typeof(System.Windows.Controls.ContentPresenter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ContentPresenter(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContentPropertyAttribute(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              103, "ContentPropertyAttribute",
                                              typeof(System.Windows.Markup.ContentPropertyAttribute),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.ContentPropertyAttribute(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContentWrapperAttribute(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              104, "ContentWrapperAttribute",
                                              typeof(System.Windows.Markup.ContentWrapperAttribute),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContextMenu(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              105, "ContextMenu",
                                              typeof(System.Windows.Controls.ContextMenu),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ContextMenu(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ContextMenuService(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              106, "ContextMenuService",
                                              typeof(System.Windows.Controls.ContextMenuService),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Control(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              107, "Control",
                                              typeof(System.Windows.Controls.Control),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Control(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ControlTemplate(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              108, "ControlTemplate",
                                              typeof(System.Windows.Controls.ControlTemplate),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ControlTemplate(); };
            bamlType.ContentPropertyName = "Template";
            bamlType.DictionaryKeyPropertyName = "TargetType";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ControllableStoryboardAction(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              109, "ControllableStoryboardAction",
                                              typeof(System.Windows.Media.Animation.ControllableStoryboardAction),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CornerRadius(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              110, "CornerRadius",
                                              typeof(System.Windows.CornerRadius),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.CornerRadius(); };
            bamlType.TypeConverterType = typeof(System.Windows.CornerRadiusConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CornerRadiusConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              111, "CornerRadiusConverter",
                                              typeof(System.Windows.CornerRadiusConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.CornerRadiusConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CroppedBitmap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              112, "CroppedBitmap",
                                              typeof(System.Windows.Media.Imaging.CroppedBitmap),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.CroppedBitmap(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CultureInfo(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              113, "CultureInfo",
                                              typeof(System.Globalization.CultureInfo),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.ComponentModel.CultureInfoConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CultureInfoConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              114, "CultureInfoConverter",
                                              typeof(System.ComponentModel.CultureInfoConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.CultureInfoConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CultureInfoIetfLanguageTagConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              115, "CultureInfoIetfLanguageTagConverter",
                                              typeof(System.Windows.CultureInfoIetfLanguageTagConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.CultureInfoIetfLanguageTagConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Cursor(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              116, "Cursor",
                                              typeof(System.Windows.Input.Cursor),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Input.CursorConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CursorConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              117, "CursorConverter",
                                              typeof(System.Windows.Input.CursorConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.CursorConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DashStyle(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              118, "DashStyle",
                                              typeof(System.Windows.Media.DashStyle),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DashStyle(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DataChangedEventManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              119, "DataChangedEventManager",
                                              typeof(System.Windows.Data.DataChangedEventManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DataTemplate(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              120, "DataTemplate",
                                              typeof(System.Windows.DataTemplate),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DataTemplate(); };
            bamlType.ContentPropertyName = "Template";
            bamlType.DictionaryKeyPropertyName = "DataTemplateKey";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DataTemplateKey(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              121, "DataTemplateKey",
                                              typeof(System.Windows.DataTemplateKey),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DataTemplateKey(); };
            bamlType.TypeConverterType = typeof(System.Windows.Markup.TemplateKeyConverter);
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Object) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.DataTemplateKey(
                                     (System.Object)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DataTrigger(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              122, "DataTrigger",
                                              typeof(System.Windows.DataTrigger),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DataTrigger(); };
            bamlType.ContentPropertyName = "Setters";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DateTime(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              123, "DateTime",
                                              typeof(System.DateTime),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.DateTime(); };
            bamlType.HasSpecialValueConverter = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DateTimeConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              124, "DateTimeConverter",
                                              typeof(System.ComponentModel.DateTimeConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.DateTimeConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DateTimeConverter2(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              125, "DateTimeConverter2",
                                              typeof(System.Windows.Markup.DateTimeConverter2),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.DateTimeConverter2(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Decimal(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              126, "Decimal",
                                              typeof(System.Decimal),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Decimal(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.DecimalConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DecimalAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              127, "DecimalAnimation",
                                              typeof(System.Windows.Media.Animation.DecimalAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DecimalAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DecimalAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              128, "DecimalAnimationBase",
                                              typeof(System.Windows.Media.Animation.DecimalAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DecimalAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              129, "DecimalAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DecimalConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              130, "DecimalConverter",
                                              typeof(System.ComponentModel.DecimalConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.DecimalConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DecimalKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              131, "DecimalKeyFrame",
                                              typeof(System.Windows.Media.Animation.DecimalKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DecimalKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              132, "DecimalKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.DecimalKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DecimalKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Decorator(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              133, "Decorator",
                                              typeof(System.Windows.Controls.Decorator),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Decorator(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DefinitionBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              134, "DefinitionBase",
                                              typeof(System.Windows.Controls.DefinitionBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DependencyObject(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              135, "DependencyObject",
                                              typeof(System.Windows.DependencyObject),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DependencyObject(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DependencyProperty(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              136, "DependencyProperty",
                                              typeof(System.Windows.DependencyProperty),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Markup.DependencyPropertyConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DependencyPropertyConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              137, "DependencyPropertyConverter",
                                              typeof(System.Windows.Markup.DependencyPropertyConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.DependencyPropertyConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DialogResultConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              138, "DialogResultConverter",
                                              typeof(System.Windows.DialogResultConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DialogResultConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiffuseMaterial(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              139, "DiffuseMaterial",
                                              typeof(System.Windows.Media.Media3D.DiffuseMaterial),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.DiffuseMaterial(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DirectionalLight(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              140, "DirectionalLight",
                                              typeof(System.Windows.Media.Media3D.DirectionalLight),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.DirectionalLight(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteBooleanKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              141, "DiscreteBooleanKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteBooleanKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteBooleanKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteByteKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              142, "DiscreteByteKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteByteKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteByteKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteCharKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              143, "DiscreteCharKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteCharKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteCharKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteColorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              144, "DiscreteColorKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteColorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteColorKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteDecimalKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              145, "DiscreteDecimalKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteDecimalKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteDecimalKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteDoubleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              146, "DiscreteDoubleKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteDoubleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteInt16KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              147, "DiscreteInt16KeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteInt16KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteInt16KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteInt32KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              148, "DiscreteInt32KeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteInt32KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteInt32KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteInt64KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              149, "DiscreteInt64KeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteInt64KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteInt64KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteMatrixKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              150, "DiscreteMatrixKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteMatrixKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteMatrixKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteObjectKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              151, "DiscreteObjectKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteObjectKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteObjectKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscretePoint3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              152, "DiscretePoint3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscretePoint3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscretePoint3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscretePointKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              153, "DiscretePointKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscretePointKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscretePointKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteQuaternionKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              154, "DiscreteQuaternionKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteQuaternionKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteQuaternionKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteRectKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              155, "DiscreteRectKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteRectKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteRectKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteRotation3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              156, "DiscreteRotation3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteRotation3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteRotation3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteSingleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              157, "DiscreteSingleKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteSingleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteSingleKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteSizeKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              158, "DiscreteSizeKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteSizeKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteSizeKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteStringKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              159, "DiscreteStringKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteStringKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteStringKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteThicknessKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              160, "DiscreteThicknessKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteThicknessKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteThicknessKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteVector3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              161, "DiscreteVector3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteVector3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteVector3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DiscreteVectorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              162, "DiscreteVectorKeyFrame",
                                              typeof(System.Windows.Media.Animation.DiscreteVectorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DiscreteVectorKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DockPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              163, "DockPanel",
                                              typeof(System.Windows.Controls.DockPanel),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.DockPanel(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DocumentPageView(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              164, "DocumentPageView",
                                              typeof(System.Windows.Controls.Primitives.DocumentPageView),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.DocumentPageView(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DocumentReference(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              165, "DocumentReference",
                                              typeof(System.Windows.Documents.DocumentReference),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.DocumentReference(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DocumentViewer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              166, "DocumentViewer",
                                              typeof(System.Windows.Controls.DocumentViewer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.DocumentViewer(); };
            bamlType.ContentPropertyName = "Document";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DocumentViewerBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              167, "DocumentViewerBase",
                                              typeof(System.Windows.Controls.Primitives.DocumentViewerBase),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Document";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Double(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              168, "Double",
                                              typeof(System.Double),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Double(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.DoubleConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              169, "DoubleAnimation",
                                              typeof(System.Windows.Media.Animation.DoubleAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DoubleAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              170, "DoubleAnimationBase",
                                              typeof(System.Windows.Media.Animation.DoubleAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              171, "DoubleAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleAnimationUsingPath(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              172, "DoubleAnimationUsingPath",
                                              typeof(System.Windows.Media.Animation.DoubleAnimationUsingPath),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DoubleAnimationUsingPath(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              173, "DoubleCollection",
                                              typeof(System.Windows.Media.DoubleCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DoubleCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.DoubleCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              174, "DoubleCollectionConverter",
                                              typeof(System.Windows.Media.DoubleCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DoubleCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              175, "DoubleConverter",
                                              typeof(System.ComponentModel.DoubleConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.DoubleConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleIListConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              176, "DoubleIListConverter",
                                              typeof(System.Windows.Media.Converters.DoubleIListConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Converters.DoubleIListConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              177, "DoubleKeyFrame",
                                              typeof(System.Windows.Media.Animation.DoubleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DoubleKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              178, "DoubleKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.DoubleKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.DoubleKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Drawing(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              179, "Drawing",
                                              typeof(System.Windows.Media.Drawing),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DrawingBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              180, "DrawingBrush",
                                              typeof(System.Windows.Media.DrawingBrush),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DrawingBrush(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DrawingCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              181, "DrawingCollection",
                                              typeof(System.Windows.Media.DrawingCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DrawingCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DrawingContext(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              182, "DrawingContext",
                                              typeof(System.Windows.Media.DrawingContext),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DrawingGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              183, "DrawingGroup",
                                              typeof(System.Windows.Media.DrawingGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DrawingGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DrawingImage(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              184, "DrawingImage",
                                              typeof(System.Windows.Media.DrawingImage),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DrawingImage(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DrawingVisual(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              185, "DrawingVisual",
                                              typeof(System.Windows.Media.DrawingVisual),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.DrawingVisual(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DropShadowBitmapEffect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              186, "DropShadowBitmapEffect",
                                              typeof(System.Windows.Media.Effects.DropShadowBitmapEffect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.DropShadowBitmapEffect(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Duration(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              187, "Duration",
                                              typeof(System.Windows.Duration),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Duration(); };
            bamlType.TypeConverterType = typeof(System.Windows.DurationConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DurationConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              188, "DurationConverter",
                                              typeof(System.Windows.DurationConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DurationConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DynamicResourceExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              189, "DynamicResourceExtension",
                                              typeof(System.Windows.DynamicResourceExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DynamicResourceExtension(); };
            bamlType.TypeConverterType = typeof(System.Windows.DynamicResourceExtensionConverter);
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Object) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.DynamicResourceExtension(
                                     (System.Object)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DynamicResourceExtensionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              190, "DynamicResourceExtensionConverter",
                                              typeof(System.Windows.DynamicResourceExtensionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.DynamicResourceExtensionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Ellipse(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              191, "Ellipse",
                                              typeof(System.Windows.Shapes.Ellipse),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Shapes.Ellipse(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_EllipseGeometry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              192, "EllipseGeometry",
                                              typeof(System.Windows.Media.EllipseGeometry),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.EllipseGeometry(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_EmbossBitmapEffect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              193, "EmbossBitmapEffect",
                                              typeof(System.Windows.Media.Effects.EmbossBitmapEffect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.EmbossBitmapEffect(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_EmissiveMaterial(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              194, "EmissiveMaterial",
                                              typeof(System.Windows.Media.Media3D.EmissiveMaterial),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.EmissiveMaterial(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_EnumConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              195, "EnumConverter",
                                              typeof(System.ComponentModel.EnumConverter),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_EventManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              196, "EventManager",
                                              typeof(System.Windows.EventManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_EventSetter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              197, "EventSetter",
                                              typeof(System.Windows.EventSetter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.EventSetter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_EventTrigger(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              198, "EventTrigger",
                                              typeof(System.Windows.EventTrigger),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.EventTrigger(); };
            bamlType.ContentPropertyName = "Actions";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Expander(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              199, "Expander",
                                              typeof(System.Windows.Controls.Expander),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Expander(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Expression(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              200, "Expression",
                                              typeof(System.Windows.Expression),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.ExpressionConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ExpressionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              201, "ExpressionConverter",
                                              typeof(System.Windows.ExpressionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.ExpressionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Figure(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              202, "Figure",
                                              typeof(System.Windows.Documents.Figure),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Figure(); };
            bamlType.ContentPropertyName = "Blocks";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FigureLength(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              203, "FigureLength",
                                              typeof(System.Windows.FigureLength),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FigureLength(); };
            bamlType.TypeConverterType = typeof(System.Windows.FigureLengthConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FigureLengthConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              204, "FigureLengthConverter",
                                              typeof(System.Windows.FigureLengthConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FigureLengthConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FixedDocument(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              205, "FixedDocument",
                                              typeof(System.Windows.Documents.FixedDocument),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.FixedDocument(); };
            bamlType.ContentPropertyName = "Pages";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FixedDocumentSequence(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              206, "FixedDocumentSequence",
                                              typeof(System.Windows.Documents.FixedDocumentSequence),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.FixedDocumentSequence(); };
            bamlType.ContentPropertyName = "References";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FixedPage(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              207, "FixedPage",
                                              typeof(System.Windows.Documents.FixedPage),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.FixedPage(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Floater(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              208, "Floater",
                                              typeof(System.Windows.Documents.Floater),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Floater(); };
            bamlType.ContentPropertyName = "Blocks";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FlowDocument(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              209, "FlowDocument",
                                              typeof(System.Windows.Documents.FlowDocument),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.FlowDocument(); };
            bamlType.ContentPropertyName = "Blocks";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FlowDocumentPageViewer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              210, "FlowDocumentPageViewer",
                                              typeof(System.Windows.Controls.FlowDocumentPageViewer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.FlowDocumentPageViewer(); };
            bamlType.ContentPropertyName = "Document";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FlowDocumentReader(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              211, "FlowDocumentReader",
                                              typeof(System.Windows.Controls.FlowDocumentReader),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.FlowDocumentReader(); };
            bamlType.ContentPropertyName = "Document";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FlowDocumentScrollViewer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              212, "FlowDocumentScrollViewer",
                                              typeof(System.Windows.Controls.FlowDocumentScrollViewer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.FlowDocumentScrollViewer(); };
            bamlType.ContentPropertyName = "Document";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FocusManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              213, "FocusManager",
                                              typeof(System.Windows.Input.FocusManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontFamily(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              214, "FontFamily",
                                              typeof(System.Windows.Media.FontFamily),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.FontFamily(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.FontFamilyConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontFamilyConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              215, "FontFamilyConverter",
                                              typeof(System.Windows.Media.FontFamilyConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.FontFamilyConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontSizeConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              216, "FontSizeConverter",
                                              typeof(System.Windows.FontSizeConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FontSizeConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontStretch(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              217, "FontStretch",
                                              typeof(System.Windows.FontStretch),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FontStretch(); };
            bamlType.TypeConverterType = typeof(System.Windows.FontStretchConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontStretchConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              218, "FontStretchConverter",
                                              typeof(System.Windows.FontStretchConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FontStretchConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontStyle(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              219, "FontStyle",
                                              typeof(System.Windows.FontStyle),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FontStyle(); };
            bamlType.TypeConverterType = typeof(System.Windows.FontStyleConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontStyleConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              220, "FontStyleConverter",
                                              typeof(System.Windows.FontStyleConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FontStyleConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontWeight(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              221, "FontWeight",
                                              typeof(System.Windows.FontWeight),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FontWeight(); };
            bamlType.TypeConverterType = typeof(System.Windows.FontWeightConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FontWeightConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              222, "FontWeightConverter",
                                              typeof(System.Windows.FontWeightConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FontWeightConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FormatConvertedBitmap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              223, "FormatConvertedBitmap",
                                              typeof(System.Windows.Media.Imaging.FormatConvertedBitmap),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.FormatConvertedBitmap(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Frame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              224, "Frame",
                                              typeof(System.Windows.Controls.Frame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Frame(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkContentElement(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              225, "FrameworkContentElement",
                                              typeof(System.Windows.FrameworkContentElement),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FrameworkContentElement(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkElement(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              226, "FrameworkElement",
                                              typeof(System.Windows.FrameworkElement),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FrameworkElement(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkElementFactory(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              227, "FrameworkElementFactory",
                                              typeof(System.Windows.FrameworkElementFactory),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FrameworkElementFactory(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkPropertyMetadata(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              228, "FrameworkPropertyMetadata",
                                              typeof(System.Windows.FrameworkPropertyMetadata),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FrameworkPropertyMetadata(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkPropertyMetadataOptions(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              229, "FrameworkPropertyMetadataOptions",
                                              typeof(System.Windows.FrameworkPropertyMetadataOptions),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.FrameworkPropertyMetadataOptions(); };
            bamlType.TypeConverterType = typeof(System.Windows.FrameworkPropertyMetadataOptions);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkRichTextComposition(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              230, "FrameworkRichTextComposition",
                                              typeof(System.Windows.Documents.FrameworkRichTextComposition),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkTemplate(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              231, "FrameworkTemplate",
                                              typeof(System.Windows.FrameworkTemplate),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Template";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_FrameworkTextComposition(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              232, "FrameworkTextComposition",
                                              typeof(System.Windows.Documents.FrameworkTextComposition),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Freezable(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              233, "Freezable",
                                              typeof(System.Windows.Freezable),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeneralTransform(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              234, "GeneralTransform",
                                              typeof(System.Windows.Media.GeneralTransform),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeneralTransformCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              235, "GeneralTransformCollection",
                                              typeof(System.Windows.Media.GeneralTransformCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GeneralTransformCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeneralTransformGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              236, "GeneralTransformGroup",
                                              typeof(System.Windows.Media.GeneralTransformGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GeneralTransformGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Geometry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              237, "Geometry",
                                              typeof(System.Windows.Media.Geometry),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Geometry3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              238, "Geometry3D",
                                              typeof(System.Windows.Media.Media3D.Geometry3D),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeometryCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              239, "GeometryCollection",
                                              typeof(System.Windows.Media.GeometryCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GeometryCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeometryConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              240, "GeometryConverter",
                                              typeof(System.Windows.Media.GeometryConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GeometryConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeometryDrawing(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              241, "GeometryDrawing",
                                              typeof(System.Windows.Media.GeometryDrawing),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GeometryDrawing(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeometryGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              242, "GeometryGroup",
                                              typeof(System.Windows.Media.GeometryGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GeometryGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GeometryModel3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              243, "GeometryModel3D",
                                              typeof(System.Windows.Media.Media3D.GeometryModel3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.GeometryModel3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GestureRecognizer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              244, "GestureRecognizer",
                                              typeof(System.Windows.Ink.GestureRecognizer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Ink.GestureRecognizer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GifBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              245, "GifBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.GifBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GifBitmapEncoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              246, "GifBitmapEncoder",
                                              typeof(System.Windows.Media.Imaging.GifBitmapEncoder),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.GifBitmapEncoder(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GlyphRun(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              247, "GlyphRun",
                                              typeof(System.Windows.Media.GlyphRun),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GlyphRun(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GlyphRunDrawing(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              248, "GlyphRunDrawing",
                                              typeof(System.Windows.Media.GlyphRunDrawing),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GlyphRunDrawing(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GlyphTypeface(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              249, "GlyphTypeface",
                                              typeof(System.Windows.Media.GlyphTypeface),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GlyphTypeface(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Glyphs(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              250, "Glyphs",
                                              typeof(System.Windows.Documents.Glyphs),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Glyphs(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GradientBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              251, "GradientBrush",
                                              typeof(System.Windows.Media.GradientBrush),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "GradientStops";
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GradientStop(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              252, "GradientStop",
                                              typeof(System.Windows.Media.GradientStop),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GradientStop(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GradientStopCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              253, "GradientStopCollection",
                                              typeof(System.Windows.Media.GradientStopCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GradientStopCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Grid(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              254, "Grid",
                                              typeof(System.Windows.Controls.Grid),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Grid(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridLength(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              255, "GridLength",
                                              typeof(System.Windows.GridLength),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.GridLength(); };
            bamlType.TypeConverterType = typeof(System.Windows.GridLengthConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridLengthConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              256, "GridLengthConverter",
                                              typeof(System.Windows.GridLengthConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.GridLengthConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridSplitter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              257, "GridSplitter",
                                              typeof(System.Windows.Controls.GridSplitter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GridSplitter(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridView(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              258, "GridView",
                                              typeof(System.Windows.Controls.GridView),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GridView(); };
            bamlType.ContentPropertyName = "Columns";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridViewColumn(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              259, "GridViewColumn",
                                              typeof(System.Windows.Controls.GridViewColumn),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GridViewColumn(); };
            bamlType.ContentPropertyName = "Header";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridViewColumnHeader(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              260, "GridViewColumnHeader",
                                              typeof(System.Windows.Controls.GridViewColumnHeader),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GridViewColumnHeader(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridViewHeaderRowPresenter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              261, "GridViewHeaderRowPresenter",
                                              typeof(System.Windows.Controls.GridViewHeaderRowPresenter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GridViewHeaderRowPresenter(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridViewRowPresenter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              262, "GridViewRowPresenter",
                                              typeof(System.Windows.Controls.GridViewRowPresenter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GridViewRowPresenter(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GridViewRowPresenterBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              263, "GridViewRowPresenterBase",
                                              typeof(System.Windows.Controls.Primitives.GridViewRowPresenterBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GroupBox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              264, "GroupBox",
                                              typeof(System.Windows.Controls.GroupBox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GroupBox(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GroupItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              265, "GroupItem",
                                              typeof(System.Windows.Controls.GroupItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.GroupItem(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Guid(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              266, "Guid",
                                              typeof(System.Guid),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Guid(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.GuidConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GuidConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              267, "GuidConverter",
                                              typeof(System.ComponentModel.GuidConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.GuidConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_GuidelineSet(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              268, "GuidelineSet",
                                              typeof(System.Windows.Media.GuidelineSet),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.GuidelineSet(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_HeaderedContentControl(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              269, "HeaderedContentControl",
                                              typeof(System.Windows.Controls.HeaderedContentControl),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.HeaderedContentControl(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_HeaderedItemsControl(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              270, "HeaderedItemsControl",
                                              typeof(System.Windows.Controls.HeaderedItemsControl),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.HeaderedItemsControl(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_HierarchicalDataTemplate(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              271, "HierarchicalDataTemplate",
                                              typeof(System.Windows.HierarchicalDataTemplate),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.HierarchicalDataTemplate(); };
            bamlType.ContentPropertyName = "Template";
            bamlType.DictionaryKeyPropertyName = "DataTemplateKey";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_HostVisual(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              272, "HostVisual",
                                              typeof(System.Windows.Media.HostVisual),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.HostVisual(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Hyperlink(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              273, "Hyperlink",
                                              typeof(System.Windows.Documents.Hyperlink),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Hyperlink(); };
            bamlType.ContentPropertyName = "Inlines";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_IAddChild(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              274, "IAddChild",
                                              typeof(System.Windows.Markup.IAddChild),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_IAddChildInternal(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              275, "IAddChildInternal",
                                              typeof(System.Windows.Markup.IAddChildInternal),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ICommand(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              276, "ICommand",
                                              typeof(System.Windows.Input.ICommand),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Input.CommandConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_IComponentConnector(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              277, "IComponentConnector",
                                              typeof(System.Windows.Markup.IComponentConnector),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_INameScope(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              278, "INameScope",
                                              typeof(System.Windows.Markup.INameScope),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_IStyleConnector(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              279, "IStyleConnector",
                                              typeof(System.Windows.Markup.IStyleConnector),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_IconBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              280, "IconBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.IconBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Image(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              281, "Image",
                                              typeof(System.Windows.Controls.Image),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Image(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ImageBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              282, "ImageBrush",
                                              typeof(System.Windows.Media.ImageBrush),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.ImageBrush(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ImageDrawing(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              283, "ImageDrawing",
                                              typeof(System.Windows.Media.ImageDrawing),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.ImageDrawing(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ImageMetadata(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              284, "ImageMetadata",
                                              typeof(System.Windows.Media.ImageMetadata),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ImageSource(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              285, "ImageSource",
                                              typeof(System.Windows.Media.ImageSource),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ImageSourceConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              286, "ImageSourceConverter",
                                              typeof(System.Windows.Media.ImageSourceConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.ImageSourceConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InPlaceBitmapMetadataWriter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              287, "InPlaceBitmapMetadataWriter",
                                              typeof(System.Windows.Media.Imaging.InPlaceBitmapMetadataWriter),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InkCanvas(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              288, "InkCanvas",
                                              typeof(System.Windows.Controls.InkCanvas),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.InkCanvas(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InkPresenter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              289, "InkPresenter",
                                              typeof(System.Windows.Controls.InkPresenter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.InkPresenter(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Inline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              290, "Inline",
                                              typeof(System.Windows.Documents.Inline),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InlineCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              291, "InlineCollection",
                                              typeof(System.Windows.Documents.InlineCollection),
                                              isBamlType, useV3Rules);
            bamlType.WhitespaceSignificantCollection = true;
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InlineUIContainer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              292, "InlineUIContainer",
                                              typeof(System.Windows.Documents.InlineUIContainer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.InlineUIContainer(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputBinding(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              293, "InputBinding",
                                              typeof(System.Windows.Input.InputBinding),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputDevice(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              294, "InputDevice",
                                              typeof(System.Windows.Input.InputDevice),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputLanguageManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              295, "InputLanguageManager",
                                              typeof(System.Windows.Input.InputLanguageManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              296, "InputManager",
                                              typeof(System.Windows.Input.InputManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputMethod(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              297, "InputMethod",
                                              typeof(System.Windows.Input.InputMethod),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputScope(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              298, "InputScope",
                                              typeof(System.Windows.Input.InputScope),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.InputScope(); };
            bamlType.TypeConverterType = typeof(System.Windows.Input.InputScopeConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputScopeConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              299, "InputScopeConverter",
                                              typeof(System.Windows.Input.InputScopeConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.InputScopeConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputScopeName(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              300, "InputScopeName",
                                              typeof(System.Windows.Input.InputScopeName),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.InputScopeName(); };
            bamlType.ContentPropertyName = "NameValue";
            bamlType.TypeConverterType = typeof(System.Windows.Input.InputScopeNameConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputScopeNameConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              301, "InputScopeNameConverter",
                                              typeof(System.Windows.Input.InputScopeNameConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.InputScopeNameConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int16(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              302, "Int16",
                                              typeof(System.Int16),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Int16(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.Int16Converter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int16Animation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              303, "Int16Animation",
                                              typeof(System.Windows.Media.Animation.Int16Animation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int16Animation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int16AnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              304, "Int16AnimationBase",
                                              typeof(System.Windows.Media.Animation.Int16AnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int16AnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              305, "Int16AnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.Int16AnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int16AnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int16Converter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              306, "Int16Converter",
                                              typeof(System.ComponentModel.Int16Converter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.Int16Converter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int16KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              307, "Int16KeyFrame",
                                              typeof(System.Windows.Media.Animation.Int16KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int16KeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              308, "Int16KeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.Int16KeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int16KeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              309, "Int32",
                                              typeof(System.Int32),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Int32(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.Int32Converter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32Animation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              310, "Int32Animation",
                                              typeof(System.Windows.Media.Animation.Int32Animation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int32Animation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32AnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              311, "Int32AnimationBase",
                                              typeof(System.Windows.Media.Animation.Int32AnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32AnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              312, "Int32AnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.Int32AnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int32AnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32Collection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              313, "Int32Collection",
                                              typeof(System.Windows.Media.Int32Collection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Int32Collection(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Int32CollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32CollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              314, "Int32CollectionConverter",
                                              typeof(System.Windows.Media.Int32CollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Int32CollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32Converter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              315, "Int32Converter",
                                              typeof(System.ComponentModel.Int32Converter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.Int32Converter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              316, "Int32KeyFrame",
                                              typeof(System.Windows.Media.Animation.Int32KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32KeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              317, "Int32KeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.Int32KeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int32KeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32Rect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              318, "Int32Rect",
                                              typeof(System.Windows.Int32Rect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Int32Rect(); };
            bamlType.TypeConverterType = typeof(System.Windows.Int32RectConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int32RectConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              319, "Int32RectConverter",
                                              typeof(System.Windows.Int32RectConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Int32RectConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int64(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              320, "Int64",
                                              typeof(System.Int64),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Int64(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.Int64Converter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int64Animation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              321, "Int64Animation",
                                              typeof(System.Windows.Media.Animation.Int64Animation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int64Animation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int64AnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              322, "Int64AnimationBase",
                                              typeof(System.Windows.Media.Animation.Int64AnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int64AnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              323, "Int64AnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.Int64AnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int64AnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int64Converter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              324, "Int64Converter",
                                              typeof(System.ComponentModel.Int64Converter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.Int64Converter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int64KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              325, "Int64KeyFrame",
                                              typeof(System.Windows.Media.Animation.Int64KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Int64KeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              326, "Int64KeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.Int64KeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Int64KeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Italic(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              327, "Italic",
                                              typeof(System.Windows.Documents.Italic),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Italic(); };
            bamlType.ContentPropertyName = "Inlines";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ItemCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              328, "ItemCollection",
                                              typeof(System.Windows.Controls.ItemCollection),
                                              isBamlType, useV3Rules);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ItemsControl(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              329, "ItemsControl",
                                              typeof(System.Windows.Controls.ItemsControl),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ItemsControl(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ItemsPanelTemplate(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              330, "ItemsPanelTemplate",
                                              typeof(System.Windows.Controls.ItemsPanelTemplate),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ItemsPanelTemplate(); };
            bamlType.ContentPropertyName = "Template";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ItemsPresenter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              331, "ItemsPresenter",
                                              typeof(System.Windows.Controls.ItemsPresenter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ItemsPresenter(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_JournalEntry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              332, "JournalEntry",
                                              typeof(System.Windows.Navigation.JournalEntry),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_JournalEntryListConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              333, "JournalEntryListConverter",
                                              typeof(System.Windows.Navigation.JournalEntryListConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Navigation.JournalEntryListConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_JournalEntryUnifiedViewConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              334, "JournalEntryUnifiedViewConverter",
                                              typeof(System.Windows.Navigation.JournalEntryUnifiedViewConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Navigation.JournalEntryUnifiedViewConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_JpegBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              335, "JpegBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.JpegBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_JpegBitmapEncoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              336, "JpegBitmapEncoder",
                                              typeof(System.Windows.Media.Imaging.JpegBitmapEncoder),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.JpegBitmapEncoder(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyBinding(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              337, "KeyBinding",
                                              typeof(System.Windows.Input.KeyBinding),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.KeyBinding(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              338, "KeyConverter",
                                              typeof(System.Windows.Input.KeyConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.KeyConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyGesture(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              339, "KeyGesture",
                                              typeof(System.Windows.Input.KeyGesture),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Input.KeyGestureConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyGestureConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              340, "KeyGestureConverter",
                                              typeof(System.Windows.Input.KeyGestureConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.KeyGestureConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeySpline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              341, "KeySpline",
                                              typeof(System.Windows.Media.Animation.KeySpline),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.KeySpline(); };
            bamlType.TypeConverterType = typeof(System.Windows.KeySplineConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeySplineConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              342, "KeySplineConverter",
                                              typeof(System.Windows.KeySplineConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.KeySplineConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyTime(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              343, "KeyTime",
                                              typeof(System.Windows.Media.Animation.KeyTime),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.KeyTime(); };
            bamlType.TypeConverterType = typeof(System.Windows.KeyTimeConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyTimeConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              344, "KeyTimeConverter",
                                              typeof(System.Windows.KeyTimeConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.KeyTimeConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyboardDevice(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              345, "KeyboardDevice",
                                              typeof(System.Windows.Input.KeyboardDevice),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Label(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              346, "Label",
                                              typeof(System.Windows.Controls.Label),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Label(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LateBoundBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              347, "LateBoundBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.LateBoundBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LengthConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              348, "LengthConverter",
                                              typeof(System.Windows.LengthConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.LengthConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Light(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              349, "Light",
                                              typeof(System.Windows.Media.Media3D.Light),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Line(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              350, "Line",
                                              typeof(System.Windows.Shapes.Line),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Shapes.Line(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LineBreak(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              351, "LineBreak",
                                              typeof(System.Windows.Documents.LineBreak),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.LineBreak(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LineGeometry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              352, "LineGeometry",
                                              typeof(System.Windows.Media.LineGeometry),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.LineGeometry(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LineSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              353, "LineSegment",
                                              typeof(System.Windows.Media.LineSegment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.LineSegment(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearByteKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              354, "LinearByteKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearByteKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearByteKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearColorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              355, "LinearColorKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearColorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearColorKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearDecimalKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              356, "LinearDecimalKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearDecimalKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearDecimalKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearDoubleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              357, "LinearDoubleKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearDoubleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearDoubleKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearGradientBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              358, "LinearGradientBrush",
                                              typeof(System.Windows.Media.LinearGradientBrush),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.LinearGradientBrush(); };
            bamlType.ContentPropertyName = "GradientStops";
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearInt16KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              359, "LinearInt16KeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearInt16KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearInt16KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearInt32KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              360, "LinearInt32KeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearInt32KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearInt32KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearInt64KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              361, "LinearInt64KeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearInt64KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearInt64KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearPoint3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              362, "LinearPoint3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearPoint3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearPoint3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearPointKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              363, "LinearPointKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearPointKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearPointKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearQuaternionKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              364, "LinearQuaternionKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearQuaternionKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearQuaternionKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearRectKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              365, "LinearRectKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearRectKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearRectKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearRotation3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              366, "LinearRotation3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearRotation3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearRotation3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearSingleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              367, "LinearSingleKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearSingleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearSingleKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearSizeKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              368, "LinearSizeKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearSizeKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearSizeKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearThicknessKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              369, "LinearThicknessKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearThicknessKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearThicknessKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearVector3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              370, "LinearVector3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearVector3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearVector3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LinearVectorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              371, "LinearVectorKeyFrame",
                                              typeof(System.Windows.Media.Animation.LinearVectorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.LinearVectorKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_List(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              372, "List",
                                              typeof(System.Windows.Documents.List),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.List(); };
            bamlType.ContentPropertyName = "ListItems";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ListBox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              373, "ListBox",
                                              typeof(System.Windows.Controls.ListBox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ListBox(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ListBoxItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              374, "ListBoxItem",
                                              typeof(System.Windows.Controls.ListBoxItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ListBoxItem(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ListCollectionView(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              375, "ListCollectionView",
                                              typeof(System.Windows.Data.ListCollectionView),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ListItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              376, "ListItem",
                                              typeof(System.Windows.Documents.ListItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.ListItem(); };
            bamlType.ContentPropertyName = "Blocks";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ListView(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              377, "ListView",
                                              typeof(System.Windows.Controls.ListView),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ListView(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ListViewItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              378, "ListViewItem",
                                              typeof(System.Windows.Controls.ListViewItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ListViewItem(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Localization(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              379, "Localization",
                                              typeof(System.Windows.Localization),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_LostFocusEventManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              380, "LostFocusEventManager",
                                              typeof(System.Windows.LostFocusEventManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MarkupExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              381, "MarkupExtension",
                                              typeof(System.Windows.Markup.MarkupExtension),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Material(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              382, "Material",
                                              typeof(System.Windows.Media.Media3D.Material),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MaterialCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              383, "MaterialCollection",
                                              typeof(System.Windows.Media.Media3D.MaterialCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.MaterialCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MaterialGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              384, "MaterialGroup",
                                              typeof(System.Windows.Media.Media3D.MaterialGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.MaterialGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Matrix(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              385, "Matrix",
                                              typeof(System.Windows.Media.Matrix),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Matrix(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.MatrixConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Matrix3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              386, "Matrix3D",
                                              typeof(System.Windows.Media.Media3D.Matrix3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Matrix3D(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Matrix3DConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Matrix3DConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              387, "Matrix3DConverter",
                                              typeof(System.Windows.Media.Media3D.Matrix3DConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Matrix3DConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              388, "MatrixAnimationBase",
                                              typeof(System.Windows.Media.Animation.MatrixAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              389, "MatrixAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixAnimationUsingPath(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              390, "MatrixAnimationUsingPath",
                                              typeof(System.Windows.Media.Animation.MatrixAnimationUsingPath),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.MatrixAnimationUsingPath(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixCamera(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              391, "MatrixCamera",
                                              typeof(System.Windows.Media.Media3D.MatrixCamera),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.MatrixCamera(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              392, "MatrixConverter",
                                              typeof(System.Windows.Media.MatrixConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.MatrixConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              393, "MatrixKeyFrame",
                                              typeof(System.Windows.Media.Animation.MatrixKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              394, "MatrixKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.MatrixKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.MatrixKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixTransform(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              395, "MatrixTransform",
                                              typeof(System.Windows.Media.MatrixTransform),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.MatrixTransform(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MatrixTransform3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              396, "MatrixTransform3D",
                                              typeof(System.Windows.Media.Media3D.MatrixTransform3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.MatrixTransform3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MediaClock(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              397, "MediaClock",
                                              typeof(System.Windows.Media.MediaClock),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MediaElement(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              398, "MediaElement",
                                              typeof(System.Windows.Controls.MediaElement),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.MediaElement(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MediaPlayer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              399, "MediaPlayer",
                                              typeof(System.Windows.Media.MediaPlayer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.MediaPlayer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MediaTimeline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              400, "MediaTimeline",
                                              typeof(System.Windows.Media.MediaTimeline),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.MediaTimeline(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Menu(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              401, "Menu",
                                              typeof(System.Windows.Controls.Menu),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Menu(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MenuBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              402, "MenuBase",
                                              typeof(System.Windows.Controls.Primitives.MenuBase),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MenuItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              403, "MenuItem",
                                              typeof(System.Windows.Controls.MenuItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.MenuItem(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MenuScrollingVisibilityConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              404, "MenuScrollingVisibilityConverter",
                                              typeof(System.Windows.Controls.MenuScrollingVisibilityConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.MenuScrollingVisibilityConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MeshGeometry3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              405, "MeshGeometry3D",
                                              typeof(System.Windows.Media.Media3D.MeshGeometry3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.MeshGeometry3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Model3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              406, "Model3D",
                                              typeof(System.Windows.Media.Media3D.Model3D),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Model3DCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              407, "Model3DCollection",
                                              typeof(System.Windows.Media.Media3D.Model3DCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Model3DCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Model3DGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              408, "Model3DGroup",
                                              typeof(System.Windows.Media.Media3D.Model3DGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Model3DGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ModelVisual3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              409, "ModelVisual3D",
                                              typeof(System.Windows.Media.Media3D.ModelVisual3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.ModelVisual3D(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ModifierKeysConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              410, "ModifierKeysConverter",
                                              typeof(System.Windows.Input.ModifierKeysConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.ModifierKeysConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MouseActionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              411, "MouseActionConverter",
                                              typeof(System.Windows.Input.MouseActionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.MouseActionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MouseBinding(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              412, "MouseBinding",
                                              typeof(System.Windows.Input.MouseBinding),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.MouseBinding(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MouseDevice(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              413, "MouseDevice",
                                              typeof(System.Windows.Input.MouseDevice),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MouseGesture(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              414, "MouseGesture",
                                              typeof(System.Windows.Input.MouseGesture),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.MouseGesture(); };
            bamlType.TypeConverterType = typeof(System.Windows.Input.MouseGestureConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MouseGestureConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              415, "MouseGestureConverter",
                                              typeof(System.Windows.Input.MouseGestureConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.MouseGestureConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MultiBinding(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              416, "MultiBinding",
                                              typeof(System.Windows.Data.MultiBinding),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.MultiBinding(); };
            bamlType.ContentPropertyName = "Bindings";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MultiBindingExpression(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              417, "MultiBindingExpression",
                                              typeof(System.Windows.Data.MultiBindingExpression),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.ExpressionConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MultiDataTrigger(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              418, "MultiDataTrigger",
                                              typeof(System.Windows.MultiDataTrigger),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.MultiDataTrigger(); };
            bamlType.ContentPropertyName = "Setters";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_MultiTrigger(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              419, "MultiTrigger",
                                              typeof(System.Windows.MultiTrigger),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.MultiTrigger(); };
            bamlType.ContentPropertyName = "Setters";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_NameScope(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              420, "NameScope",
                                              typeof(System.Windows.NameScope),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.NameScope(); };
            bamlType.CollectionKind = XamlCollectionKind.Dictionary;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_NavigationWindow(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              421, "NavigationWindow",
                                              typeof(System.Windows.Navigation.NavigationWindow),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Navigation.NavigationWindow(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_NullExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              422, "NullExtension",
                                              typeof(System.Windows.Markup.NullExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.NullExtension(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_NullableBoolConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              423, "NullableBoolConverter",
                                              typeof(System.Windows.NullableBoolConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.NullableBoolConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_NullableConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              424, "NullableConverter",
                                              typeof(System.ComponentModel.NullableConverter),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_NumberSubstitution(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              425, "NumberSubstitution",
                                              typeof(System.Windows.Media.NumberSubstitution),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.NumberSubstitution(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Object(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              426, "Object",
                                              typeof(System.Object),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Object(); };
            bamlType.HasSpecialValueConverter = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ObjectAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              427, "ObjectAnimationBase",
                                              typeof(System.Windows.Media.Animation.ObjectAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ObjectAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              428, "ObjectAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ObjectDataProvider(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              429, "ObjectDataProvider",
                                              typeof(System.Windows.Data.ObjectDataProvider),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.ObjectDataProvider(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ObjectKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              430, "ObjectKeyFrame",
                                              typeof(System.Windows.Media.Animation.ObjectKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ObjectKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              431, "ObjectKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.ObjectKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ObjectKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_OrthographicCamera(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              432, "OrthographicCamera",
                                              typeof(System.Windows.Media.Media3D.OrthographicCamera),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.OrthographicCamera(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_OuterGlowBitmapEffect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              433, "OuterGlowBitmapEffect",
                                              typeof(System.Windows.Media.Effects.OuterGlowBitmapEffect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Effects.OuterGlowBitmapEffect(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Page(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              434, "Page",
                                              typeof(System.Windows.Controls.Page),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Page(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PageContent(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              435, "PageContent",
                                              typeof(System.Windows.Documents.PageContent),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.PageContent(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PageFunctionBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              436, "PageFunctionBase",
                                              typeof(System.Windows.Navigation.PageFunctionBase),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Panel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              437, "Panel",
                                              typeof(System.Windows.Controls.Panel),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Paragraph(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              438, "Paragraph",
                                              typeof(System.Windows.Documents.Paragraph),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Paragraph(); };
            bamlType.ContentPropertyName = "Inlines";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ParallelTimeline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              439, "ParallelTimeline",
                                              typeof(System.Windows.Media.Animation.ParallelTimeline),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ParallelTimeline(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ParserContext(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              440, "ParserContext",
                                              typeof(System.Windows.Markup.ParserContext),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.ParserContext(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PasswordBox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              441, "PasswordBox",
                                              typeof(System.Windows.Controls.PasswordBox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.PasswordBox(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Path(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              442, "Path",
                                              typeof(System.Windows.Shapes.Path),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Shapes.Path(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PathFigure(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              443, "PathFigure",
                                              typeof(System.Windows.Media.PathFigure),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PathFigure(); };
            bamlType.ContentPropertyName = "Segments";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PathFigureCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              444, "PathFigureCollection",
                                              typeof(System.Windows.Media.PathFigureCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PathFigureCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.PathFigureCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PathFigureCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              445, "PathFigureCollectionConverter",
                                              typeof(System.Windows.Media.PathFigureCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PathFigureCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PathGeometry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              446, "PathGeometry",
                                              typeof(System.Windows.Media.PathGeometry),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PathGeometry(); };
            bamlType.ContentPropertyName = "Figures";
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PathSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              447, "PathSegment",
                                              typeof(System.Windows.Media.PathSegment),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PathSegmentCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              448, "PathSegmentCollection",
                                              typeof(System.Windows.Media.PathSegmentCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PathSegmentCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PauseStoryboard(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              449, "PauseStoryboard",
                                              typeof(System.Windows.Media.Animation.PauseStoryboard),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.PauseStoryboard(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Pen(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              450, "Pen",
                                              typeof(System.Windows.Media.Pen),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Pen(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PerspectiveCamera(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              451, "PerspectiveCamera",
                                              typeof(System.Windows.Media.Media3D.PerspectiveCamera),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.PerspectiveCamera(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PixelFormat(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              452, "PixelFormat",
                                              typeof(System.Windows.Media.PixelFormat),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PixelFormat(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.PixelFormatConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PixelFormatConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              453, "PixelFormatConverter",
                                              typeof(System.Windows.Media.PixelFormatConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PixelFormatConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PngBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              454, "PngBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.PngBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PngBitmapEncoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              455, "PngBitmapEncoder",
                                              typeof(System.Windows.Media.Imaging.PngBitmapEncoder),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.PngBitmapEncoder(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              456, "Point",
                                              typeof(System.Windows.Point),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Point(); };
            bamlType.TypeConverterType = typeof(System.Windows.PointConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              457, "Point3D",
                                              typeof(System.Windows.Media.Media3D.Point3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Point3D(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Point3DConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              458, "Point3DAnimation",
                                              typeof(System.Windows.Media.Animation.Point3DAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Point3DAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              459, "Point3DAnimationBase",
                                              typeof(System.Windows.Media.Animation.Point3DAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              460, "Point3DAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              461, "Point3DCollection",
                                              typeof(System.Windows.Media.Media3D.Point3DCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Point3DCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Point3DCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              462, "Point3DCollectionConverter",
                                              typeof(System.Windows.Media.Media3D.Point3DCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Point3DCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              463, "Point3DConverter",
                                              typeof(System.Windows.Media.Media3D.Point3DConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Point3DConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              464, "Point3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.Point3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point3DKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              465, "Point3DKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.Point3DKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Point3DKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point4D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              466, "Point4D",
                                              typeof(System.Windows.Media.Media3D.Point4D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Point4D(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Point4DConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Point4DConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              467, "Point4DConverter",
                                              typeof(System.Windows.Media.Media3D.Point4DConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Point4DConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              468, "PointAnimation",
                                              typeof(System.Windows.Media.Animation.PointAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.PointAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              469, "PointAnimationBase",
                                              typeof(System.Windows.Media.Animation.PointAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              470, "PointAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.PointAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.PointAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointAnimationUsingPath(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              471, "PointAnimationUsingPath",
                                              typeof(System.Windows.Media.Animation.PointAnimationUsingPath),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.PointAnimationUsingPath(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              472, "PointCollection",
                                              typeof(System.Windows.Media.PointCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PointCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.PointCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              473, "PointCollectionConverter",
                                              typeof(System.Windows.Media.PointCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PointCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              474, "PointConverter",
                                              typeof(System.Windows.PointConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.PointConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointIListConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              475, "PointIListConverter",
                                              typeof(System.Windows.Media.Converters.PointIListConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Converters.PointIListConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              476, "PointKeyFrame",
                                              typeof(System.Windows.Media.Animation.PointKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              477, "PointKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.PointKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.PointKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointLight(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              478, "PointLight",
                                              typeof(System.Windows.Media.Media3D.PointLight),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.PointLight(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PointLightBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              479, "PointLightBase",
                                              typeof(System.Windows.Media.Media3D.PointLightBase),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PolyBezierSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              480, "PolyBezierSegment",
                                              typeof(System.Windows.Media.PolyBezierSegment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PolyBezierSegment(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PolyLineSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              481, "PolyLineSegment",
                                              typeof(System.Windows.Media.PolyLineSegment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PolyLineSegment(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PolyQuadraticBezierSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              482, "PolyQuadraticBezierSegment",
                                              typeof(System.Windows.Media.PolyQuadraticBezierSegment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PolyQuadraticBezierSegment(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Polygon(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              483, "Polygon",
                                              typeof(System.Windows.Shapes.Polygon),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Shapes.Polygon(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Polyline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              484, "Polyline",
                                              typeof(System.Windows.Shapes.Polyline),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Shapes.Polyline(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Popup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              485, "Popup",
                                              typeof(System.Windows.Controls.Primitives.Popup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.Popup(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PresentationSource(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              486, "PresentationSource",
                                              typeof(System.Windows.PresentationSource),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PriorityBinding(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              487, "PriorityBinding",
                                              typeof(System.Windows.Data.PriorityBinding),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.PriorityBinding(); };
            bamlType.ContentPropertyName = "Bindings";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PriorityBindingExpression(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              488, "PriorityBindingExpression",
                                              typeof(System.Windows.Data.PriorityBindingExpression),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.ExpressionConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ProgressBar(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              489, "ProgressBar",
                                              typeof(System.Windows.Controls.ProgressBar),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ProgressBar(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ProjectionCamera(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              490, "ProjectionCamera",
                                              typeof(System.Windows.Media.Media3D.ProjectionCamera),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PropertyPath(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              491, "PropertyPath",
                                              typeof(System.Windows.PropertyPath),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.PropertyPathConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PropertyPathConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              492, "PropertyPathConverter",
                                              typeof(System.Windows.PropertyPathConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.PropertyPathConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuadraticBezierSegment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              493, "QuadraticBezierSegment",
                                              typeof(System.Windows.Media.QuadraticBezierSegment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.QuadraticBezierSegment(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Quaternion(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              494, "Quaternion",
                                              typeof(System.Windows.Media.Media3D.Quaternion),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Quaternion(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.QuaternionConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuaternionAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              495, "QuaternionAnimation",
                                              typeof(System.Windows.Media.Animation.QuaternionAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.QuaternionAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuaternionAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              496, "QuaternionAnimationBase",
                                              typeof(System.Windows.Media.Animation.QuaternionAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuaternionAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              497, "QuaternionAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuaternionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              498, "QuaternionConverter",
                                              typeof(System.Windows.Media.Media3D.QuaternionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.QuaternionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuaternionKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              499, "QuaternionKeyFrame",
                                              typeof(System.Windows.Media.Animation.QuaternionKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuaternionKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              500, "QuaternionKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.QuaternionKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.QuaternionKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_QuaternionRotation3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              501, "QuaternionRotation3D",
                                              typeof(System.Windows.Media.Media3D.QuaternionRotation3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.QuaternionRotation3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RadialGradientBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              502, "RadialGradientBrush",
                                              typeof(System.Windows.Media.RadialGradientBrush),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.RadialGradientBrush(); };
            bamlType.ContentPropertyName = "GradientStops";
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RadioButton(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              503, "RadioButton",
                                              typeof(System.Windows.Controls.RadioButton),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.RadioButton(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RangeBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              504, "RangeBase",
                                              typeof(System.Windows.Controls.Primitives.RangeBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              505, "Rect",
                                              typeof(System.Windows.Rect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Rect(); };
            bamlType.TypeConverterType = typeof(System.Windows.RectConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rect3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              506, "Rect3D",
                                              typeof(System.Windows.Media.Media3D.Rect3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Rect3D(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Rect3DConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rect3DConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              507, "Rect3DConverter",
                                              typeof(System.Windows.Media.Media3D.Rect3DConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Rect3DConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RectAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              508, "RectAnimation",
                                              typeof(System.Windows.Media.Animation.RectAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.RectAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RectAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              509, "RectAnimationBase",
                                              typeof(System.Windows.Media.Animation.RectAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RectAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              510, "RectAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.RectAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.RectAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RectConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              511, "RectConverter",
                                              typeof(System.Windows.RectConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.RectConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RectKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              512, "RectKeyFrame",
                                              typeof(System.Windows.Media.Animation.RectKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RectKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              513, "RectKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.RectKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.RectKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rectangle(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              514, "Rectangle",
                                              typeof(System.Windows.Shapes.Rectangle),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Shapes.Rectangle(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RectangleGeometry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              515, "RectangleGeometry",
                                              typeof(System.Windows.Media.RectangleGeometry),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.RectangleGeometry(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RelativeSource(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              516, "RelativeSource",
                                              typeof(System.Windows.Data.RelativeSource),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.RelativeSource(); };
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Windows.Data.RelativeSourceMode) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.Data.RelativeSource(
                                     (System.Windows.Data.RelativeSourceMode)arguments[0]);
                            }));
            bamlType.Constructors.Add(3, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Windows.Data.RelativeSourceMode), typeof(System.Type), typeof(System.Int32) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.Data.RelativeSource(
                                     (System.Windows.Data.RelativeSourceMode)arguments[0],
                                     (System.Type)arguments[1],
                                     (System.Int32)arguments[2]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RemoveStoryboard(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              517, "RemoveStoryboard",
                                              typeof(System.Windows.Media.Animation.RemoveStoryboard),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.RemoveStoryboard(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RenderOptions(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              518, "RenderOptions",
                                              typeof(System.Windows.Media.RenderOptions),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RenderTargetBitmap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              519, "RenderTargetBitmap",
                                              typeof(System.Windows.Media.Imaging.RenderTargetBitmap),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RepeatBehavior(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              520, "RepeatBehavior",
                                              typeof(System.Windows.Media.Animation.RepeatBehavior),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.RepeatBehavior(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Animation.RepeatBehaviorConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RepeatBehaviorConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              521, "RepeatBehaviorConverter",
                                              typeof(System.Windows.Media.Animation.RepeatBehaviorConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.RepeatBehaviorConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RepeatButton(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              522, "RepeatButton",
                                              typeof(System.Windows.Controls.Primitives.RepeatButton),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.RepeatButton(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ResizeGrip(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              523, "ResizeGrip",
                                              typeof(System.Windows.Controls.Primitives.ResizeGrip),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.ResizeGrip(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ResourceDictionary(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              524, "ResourceDictionary",
                                              typeof(System.Windows.ResourceDictionary),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.ResourceDictionary(); };
            bamlType.IsUsableDuringInit = true;
            bamlType.CollectionKind = XamlCollectionKind.Dictionary;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ResourceKey(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              525, "ResourceKey",
                                              typeof(System.Windows.ResourceKey),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ResumeStoryboard(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              526, "ResumeStoryboard",
                                              typeof(System.Windows.Media.Animation.ResumeStoryboard),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ResumeStoryboard(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RichTextBox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              527, "RichTextBox",
                                              typeof(System.Windows.Controls.RichTextBox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.RichTextBox(); };
            bamlType.ContentPropertyName = "Document";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RotateTransform(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              528, "RotateTransform",
                                              typeof(System.Windows.Media.RotateTransform),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.RotateTransform(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RotateTransform3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              529, "RotateTransform3D",
                                              typeof(System.Windows.Media.Media3D.RotateTransform3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.RotateTransform3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rotation3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              530, "Rotation3D",
                                              typeof(System.Windows.Media.Media3D.Rotation3D),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rotation3DAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              531, "Rotation3DAnimation",
                                              typeof(System.Windows.Media.Animation.Rotation3DAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Rotation3DAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rotation3DAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              532, "Rotation3DAnimationBase",
                                              typeof(System.Windows.Media.Animation.Rotation3DAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rotation3DAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              533, "Rotation3DAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rotation3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              534, "Rotation3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.Rotation3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Rotation3DKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              535, "Rotation3DKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.Rotation3DKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Rotation3DKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RoutedCommand(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              536, "RoutedCommand",
                                              typeof(System.Windows.Input.RoutedCommand),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.RoutedCommand(); };
            bamlType.TypeConverterType = typeof(System.Windows.Input.CommandConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RoutedEvent(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              537, "RoutedEvent",
                                              typeof(System.Windows.RoutedEvent),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Markup.RoutedEventConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RoutedEventConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              538, "RoutedEventConverter",
                                              typeof(System.Windows.Markup.RoutedEventConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.RoutedEventConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RoutedUICommand(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              539, "RoutedUICommand",
                                              typeof(System.Windows.Input.RoutedUICommand),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.RoutedUICommand(); };
            bamlType.TypeConverterType = typeof(System.Windows.Input.CommandConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RoutingStrategy(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              540, "RoutingStrategy",
                                              typeof(System.Windows.RoutingStrategy),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.RoutingStrategy(); };
            bamlType.TypeConverterType = typeof(System.Windows.RoutingStrategy);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RowDefinition(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              541, "RowDefinition",
                                              typeof(System.Windows.Controls.RowDefinition),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.RowDefinition(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Run(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              542, "Run",
                                              typeof(System.Windows.Documents.Run),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Run(); };
            bamlType.ContentPropertyName = "Text";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RuntimeNamePropertyAttribute(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              543, "RuntimeNamePropertyAttribute",
                                              typeof(System.Windows.Markup.RuntimeNamePropertyAttribute),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SByte(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              544, "SByte",
                                              typeof(System.SByte),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.SByte(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.SByteConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SByteConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              545, "SByteConverter",
                                              typeof(System.ComponentModel.SByteConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.SByteConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ScaleTransform(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              546, "ScaleTransform",
                                              typeof(System.Windows.Media.ScaleTransform),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.ScaleTransform(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ScaleTransform3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              547, "ScaleTransform3D",
                                              typeof(System.Windows.Media.Media3D.ScaleTransform3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.ScaleTransform3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ScrollBar(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              548, "ScrollBar",
                                              typeof(System.Windows.Controls.Primitives.ScrollBar),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.ScrollBar(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ScrollContentPresenter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              549, "ScrollContentPresenter",
                                              typeof(System.Windows.Controls.ScrollContentPresenter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ScrollContentPresenter(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ScrollViewer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              550, "ScrollViewer",
                                              typeof(System.Windows.Controls.ScrollViewer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ScrollViewer(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Section(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              551, "Section",
                                              typeof(System.Windows.Documents.Section),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Section(); };
            bamlType.ContentPropertyName = "Blocks";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SeekStoryboard(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              552, "SeekStoryboard",
                                              typeof(System.Windows.Media.Animation.SeekStoryboard),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SeekStoryboard(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Selector(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              553, "Selector",
                                              typeof(System.Windows.Controls.Primitives.Selector),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Separator(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              554, "Separator",
                                              typeof(System.Windows.Controls.Separator),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Separator(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SetStoryboardSpeedRatio(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              555, "SetStoryboardSpeedRatio",
                                              typeof(System.Windows.Media.Animation.SetStoryboardSpeedRatio),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SetStoryboardSpeedRatio(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Setter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              556, "Setter",
                                              typeof(System.Windows.Setter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Setter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SetterBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              557, "SetterBase",
                                              typeof(System.Windows.SetterBase),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Shape(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              558, "Shape",
                                              typeof(System.Windows.Shapes.Shape),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Single(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              559, "Single",
                                              typeof(System.Single),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Single(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.SingleConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SingleAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              560, "SingleAnimation",
                                              typeof(System.Windows.Media.Animation.SingleAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SingleAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SingleAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              561, "SingleAnimationBase",
                                              typeof(System.Windows.Media.Animation.SingleAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SingleAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              562, "SingleAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.SingleAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SingleAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SingleConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              563, "SingleConverter",
                                              typeof(System.ComponentModel.SingleConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.SingleConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SingleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              564, "SingleKeyFrame",
                                              typeof(System.Windows.Media.Animation.SingleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SingleKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              565, "SingleKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.SingleKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SingleKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Size(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              566, "Size",
                                              typeof(System.Windows.Size),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Size(); };
            bamlType.TypeConverterType = typeof(System.Windows.SizeConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Size3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              567, "Size3D",
                                              typeof(System.Windows.Media.Media3D.Size3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Size3D(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Size3DConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Size3DConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              568, "Size3DConverter",
                                              typeof(System.Windows.Media.Media3D.Size3DConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Size3DConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SizeAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              569, "SizeAnimation",
                                              typeof(System.Windows.Media.Animation.SizeAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SizeAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SizeAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              570, "SizeAnimationBase",
                                              typeof(System.Windows.Media.Animation.SizeAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SizeAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              571, "SizeAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.SizeAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SizeAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SizeConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              572, "SizeConverter",
                                              typeof(System.Windows.SizeConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.SizeConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SizeKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              573, "SizeKeyFrame",
                                              typeof(System.Windows.Media.Animation.SizeKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SizeKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              574, "SizeKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.SizeKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SizeKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SkewTransform(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              575, "SkewTransform",
                                              typeof(System.Windows.Media.SkewTransform),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.SkewTransform(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SkipStoryboardToFill(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              576, "SkipStoryboardToFill",
                                              typeof(System.Windows.Media.Animation.SkipStoryboardToFill),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SkipStoryboardToFill(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Slider(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              577, "Slider",
                                              typeof(System.Windows.Controls.Slider),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Slider(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SolidColorBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              578, "SolidColorBrush",
                                              typeof(System.Windows.Media.SolidColorBrush),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.SolidColorBrush(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SoundPlayerAction(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              579, "SoundPlayerAction",
                                              typeof(System.Windows.Controls.SoundPlayerAction),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.SoundPlayerAction(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Span(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              580, "Span",
                                              typeof(System.Windows.Documents.Span),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Span(); };
            bamlType.ContentPropertyName = "Inlines";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SpecularMaterial(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              581, "SpecularMaterial",
                                              typeof(System.Windows.Media.Media3D.SpecularMaterial),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.SpecularMaterial(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SpellCheck(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              582, "SpellCheck",
                                              typeof(System.Windows.Controls.SpellCheck),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineByteKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              583, "SplineByteKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineByteKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineByteKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineColorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              584, "SplineColorKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineColorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineColorKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineDecimalKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              585, "SplineDecimalKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineDecimalKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineDecimalKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineDoubleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              586, "SplineDoubleKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineDoubleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineDoubleKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineInt16KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              587, "SplineInt16KeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineInt16KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineInt16KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineInt32KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              588, "SplineInt32KeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineInt32KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineInt32KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineInt64KeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              589, "SplineInt64KeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineInt64KeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineInt64KeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplinePoint3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              590, "SplinePoint3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplinePoint3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplinePoint3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplinePointKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              591, "SplinePointKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplinePointKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplinePointKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineQuaternionKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              592, "SplineQuaternionKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineQuaternionKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineQuaternionKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineRectKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              593, "SplineRectKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineRectKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineRectKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineRotation3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              594, "SplineRotation3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineRotation3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineRotation3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineSingleKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              595, "SplineSingleKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineSingleKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineSingleKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineSizeKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              596, "SplineSizeKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineSizeKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineSizeKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineThicknessKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              597, "SplineThicknessKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineThicknessKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineThicknessKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineVector3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              598, "SplineVector3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineVector3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineVector3DKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SplineVectorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              599, "SplineVectorKeyFrame",
                                              typeof(System.Windows.Media.Animation.SplineVectorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.SplineVectorKeyFrame(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SpotLight(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              600, "SpotLight",
                                              typeof(System.Windows.Media.Media3D.SpotLight),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.SpotLight(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StackPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              601, "StackPanel",
                                              typeof(System.Windows.Controls.StackPanel),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.StackPanel(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StaticExtension(bool isBamlType, bool useV3Rules)
        {
            // We're using an internal StaticExtension for the actual instance so we can optimize the provide value
            // call for WPF.  At the same time, we want XamlType to always pretend to be a regular StaticExtension
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              602, "StaticExtension",
                                              typeof(System.Windows.Markup.StaticExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new MS.Internal.Markup.StaticExtension(); };
            bamlType.HasSpecialValueConverter = true;
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.String) },
                            delegate(object[] arguments)
                            {
                                return new MS.Internal.Markup.StaticExtension(
                                     (System.String)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StaticResourceExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              603, "StaticResourceExtension",
                                              typeof(System.Windows.StaticResourceExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.StaticResourceExtension(); };
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Object) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.StaticResourceExtension(
                                     (System.Object)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StatusBar(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              604, "StatusBar",
                                              typeof(System.Windows.Controls.Primitives.StatusBar),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.StatusBar(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StatusBarItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              605, "StatusBarItem",
                                              typeof(System.Windows.Controls.Primitives.StatusBarItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.StatusBarItem(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StickyNoteControl(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              606, "StickyNoteControl",
                                              typeof(System.Windows.Controls.StickyNoteControl),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StopStoryboard(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              607, "StopStoryboard",
                                              typeof(System.Windows.Media.Animation.StopStoryboard),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.StopStoryboard(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Storyboard(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              608, "Storyboard",
                                              typeof(System.Windows.Media.Animation.Storyboard),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Storyboard(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StreamGeometry(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              609, "StreamGeometry",
                                              typeof(System.Windows.Media.StreamGeometry),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.StreamGeometry(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.GeometryConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StreamGeometryContext(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              610, "StreamGeometryContext",
                                              typeof(System.Windows.Media.StreamGeometryContext),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StreamResourceInfo(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              611, "StreamResourceInfo",
                                              typeof(System.Windows.Resources.StreamResourceInfo),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Resources.StreamResourceInfo(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_String(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              612, "String",
                                              typeof(System.String),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.ComponentModel.StringConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StringAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              613, "StringAnimationBase",
                                              typeof(System.Windows.Media.Animation.StringAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StringAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              614, "StringAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.StringAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.StringAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StringConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              615, "StringConverter",
                                              typeof(System.ComponentModel.StringConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.StringConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StringKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              616, "StringKeyFrame",
                                              typeof(System.Windows.Media.Animation.StringKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StringKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              617, "StringKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.StringKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.StringKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StrokeCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              618, "StrokeCollection",
                                              typeof(System.Windows.Ink.StrokeCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Ink.StrokeCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.StrokeCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StrokeCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              619, "StrokeCollectionConverter",
                                              typeof(System.Windows.StrokeCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.StrokeCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Style(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              620, "Style",
                                              typeof(System.Windows.Style),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Style(); };
            bamlType.ContentPropertyName = "Setters";
            bamlType.DictionaryKeyPropertyName = "TargetType";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Stylus(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              621, "Stylus",
                                              typeof(System.Windows.Input.Stylus),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_StylusDevice(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              622, "StylusDevice",
                                              typeof(System.Windows.Input.StylusDevice),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TabControl(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              623, "TabControl",
                                              typeof(System.Windows.Controls.TabControl),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.TabControl(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TabItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              624, "TabItem",
                                              typeof(System.Windows.Controls.TabItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.TabItem(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TabPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              625, "TabPanel",
                                              typeof(System.Windows.Controls.Primitives.TabPanel),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.TabPanel(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Table(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              626, "Table",
                                              typeof(System.Windows.Documents.Table),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Table(); };
            bamlType.ContentPropertyName = "RowGroups";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TableCell(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              627, "TableCell",
                                              typeof(System.Windows.Documents.TableCell),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.TableCell(); };
            bamlType.ContentPropertyName = "Blocks";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TableColumn(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              628, "TableColumn",
                                              typeof(System.Windows.Documents.TableColumn),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.TableColumn(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TableRow(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              629, "TableRow",
                                              typeof(System.Windows.Documents.TableRow),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.TableRow(); };
            bamlType.ContentPropertyName = "Cells";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TableRowGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              630, "TableRowGroup",
                                              typeof(System.Windows.Documents.TableRowGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.TableRowGroup(); };
            bamlType.ContentPropertyName = "Rows";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TabletDevice(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              631, "TabletDevice",
                                              typeof(System.Windows.Input.TabletDevice),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TemplateBindingExpression(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              632, "TemplateBindingExpression",
                                              typeof(System.Windows.TemplateBindingExpression),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.TemplateBindingExpressionConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TemplateBindingExpressionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              633, "TemplateBindingExpressionConverter",
                                              typeof(System.Windows.TemplateBindingExpressionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.TemplateBindingExpressionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TemplateBindingExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              634, "TemplateBindingExtension",
                                              typeof(System.Windows.TemplateBindingExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.TemplateBindingExtension(); };
            bamlType.TypeConverterType = typeof(System.Windows.TemplateBindingExtensionConverter);
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Windows.DependencyProperty) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.TemplateBindingExtension(
                                     (System.Windows.DependencyProperty)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TemplateBindingExtensionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              635, "TemplateBindingExtensionConverter",
                                              typeof(System.Windows.TemplateBindingExtensionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.TemplateBindingExtensionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TemplateKey(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              636, "TemplateKey",
                                              typeof(System.Windows.TemplateKey),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Markup.TemplateKeyConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TemplateKeyConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              637, "TemplateKeyConverter",
                                              typeof(System.Windows.Markup.TemplateKeyConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.TemplateKeyConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextBlock(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              638, "TextBlock",
                                              typeof(System.Windows.Controls.TextBlock),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.TextBlock(); };
            bamlType.ContentPropertyName = "Inlines";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextBox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              639, "TextBox",
                                              typeof(System.Windows.Controls.TextBox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.TextBox(); };
            bamlType.ContentPropertyName = "Text";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextBoxBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              640, "TextBoxBase",
                                              typeof(System.Windows.Controls.Primitives.TextBoxBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextComposition(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              641, "TextComposition",
                                              typeof(System.Windows.Input.TextComposition),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextCompositionManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              642, "TextCompositionManager",
                                              typeof(System.Windows.Input.TextCompositionManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextDecoration(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              643, "TextDecoration",
                                              typeof(System.Windows.TextDecoration),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.TextDecoration(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextDecorationCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              644, "TextDecorationCollection",
                                              typeof(System.Windows.TextDecorationCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.TextDecorationCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.TextDecorationCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextDecorationCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              645, "TextDecorationCollectionConverter",
                                              typeof(System.Windows.TextDecorationCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.TextDecorationCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextEffect(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              646, "TextEffect",
                                              typeof(System.Windows.Media.TextEffect),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.TextEffect(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextEffectCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              647, "TextEffectCollection",
                                              typeof(System.Windows.Media.TextEffectCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.TextEffectCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextElement(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              648, "TextElement",
                                              typeof(System.Windows.Documents.TextElement),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextSearch(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              649, "TextSearch",
                                              typeof(System.Windows.Controls.TextSearch),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ThemeDictionaryExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              650, "ThemeDictionaryExtension",
                                              typeof(System.Windows.ThemeDictionaryExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.ThemeDictionaryExtension(); };
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.String) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.ThemeDictionaryExtension(
                                     (System.String)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Thickness(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              651, "Thickness",
                                              typeof(System.Windows.Thickness),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Thickness(); };
            bamlType.TypeConverterType = typeof(System.Windows.ThicknessConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ThicknessAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              652, "ThicknessAnimation",
                                              typeof(System.Windows.Media.Animation.ThicknessAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ThicknessAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ThicknessAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              653, "ThicknessAnimationBase",
                                              typeof(System.Windows.Media.Animation.ThicknessAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ThicknessAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              654, "ThicknessAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ThicknessConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              655, "ThicknessConverter",
                                              typeof(System.Windows.ThicknessConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.ThicknessConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ThicknessKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              656, "ThicknessKeyFrame",
                                              typeof(System.Windows.Media.Animation.ThicknessKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ThicknessKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              657, "ThicknessKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.ThicknessKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.ThicknessKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Thumb(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              658, "Thumb",
                                              typeof(System.Windows.Controls.Primitives.Thumb),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.Thumb(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TickBar(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              659, "TickBar",
                                              typeof(System.Windows.Controls.Primitives.TickBar),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.TickBar(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TiffBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              660, "TiffBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.TiffBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TiffBitmapEncoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              661, "TiffBitmapEncoder",
                                              typeof(System.Windows.Media.Imaging.TiffBitmapEncoder),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.TiffBitmapEncoder(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TileBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              662, "TileBrush",
                                              typeof(System.Windows.Media.TileBrush),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TimeSpan(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              663, "TimeSpan",
                                              typeof(System.TimeSpan),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.TimeSpan(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.TimeSpanConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TimeSpanConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              664, "TimeSpanConverter",
                                              typeof(System.ComponentModel.TimeSpanConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.TimeSpanConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Timeline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              665, "Timeline",
                                              typeof(System.Windows.Media.Animation.Timeline),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TimelineCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              666, "TimelineCollection",
                                              typeof(System.Windows.Media.Animation.TimelineCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.TimelineCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TimelineGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              667, "TimelineGroup",
                                              typeof(System.Windows.Media.Animation.TimelineGroup),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ToggleButton(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              668, "ToggleButton",
                                              typeof(System.Windows.Controls.Primitives.ToggleButton),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.ToggleButton(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ToolBar(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              669, "ToolBar",
                                              typeof(System.Windows.Controls.ToolBar),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ToolBar(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ToolBarOverflowPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              670, "ToolBarOverflowPanel",
                                              typeof(System.Windows.Controls.Primitives.ToolBarOverflowPanel),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.ToolBarOverflowPanel(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ToolBarPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              671, "ToolBarPanel",
                                              typeof(System.Windows.Controls.Primitives.ToolBarPanel),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.ToolBarPanel(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ToolBarTray(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              672, "ToolBarTray",
                                              typeof(System.Windows.Controls.ToolBarTray),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ToolBarTray(); };
            bamlType.ContentPropertyName = "ToolBars";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ToolTip(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              673, "ToolTip",
                                              typeof(System.Windows.Controls.ToolTip),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ToolTip(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ToolTipService(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              674, "ToolTipService",
                                              typeof(System.Windows.Controls.ToolTipService),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Track(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              675, "Track",
                                              typeof(System.Windows.Controls.Primitives.Track),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.Track(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Transform(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              676, "Transform",
                                              typeof(System.Windows.Media.Transform),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Transform3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              677, "Transform3D",
                                              typeof(System.Windows.Media.Media3D.Transform3D),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Transform3DCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              678, "Transform3DCollection",
                                              typeof(System.Windows.Media.Media3D.Transform3DCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Transform3DCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Transform3DGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              679, "Transform3DGroup",
                                              typeof(System.Windows.Media.Media3D.Transform3DGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Transform3DGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TransformCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              680, "TransformCollection",
                                              typeof(System.Windows.Media.TransformCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.TransformCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TransformConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              681, "TransformConverter",
                                              typeof(System.Windows.Media.TransformConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.TransformConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TransformGroup(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              682, "TransformGroup",
                                              typeof(System.Windows.Media.TransformGroup),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.TransformGroup(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TransformedBitmap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              683, "TransformedBitmap",
                                              typeof(System.Windows.Media.Imaging.TransformedBitmap),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.TransformedBitmap(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TranslateTransform(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              684, "TranslateTransform",
                                              typeof(System.Windows.Media.TranslateTransform),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.TranslateTransform(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.TransformConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TranslateTransform3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              685, "TranslateTransform3D",
                                              typeof(System.Windows.Media.Media3D.TranslateTransform3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.TranslateTransform3D(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TreeView(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              686, "TreeView",
                                              typeof(System.Windows.Controls.TreeView),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.TreeView(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TreeViewItem(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              687, "TreeViewItem",
                                              typeof(System.Windows.Controls.TreeViewItem),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.TreeViewItem(); };
            bamlType.ContentPropertyName = "Items";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Trigger(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              688, "Trigger",
                                              typeof(System.Windows.Trigger),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Trigger(); };
            bamlType.ContentPropertyName = "Setters";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TriggerAction(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              689, "TriggerAction",
                                              typeof(System.Windows.TriggerAction),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TriggerBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              690, "TriggerBase",
                                              typeof(System.Windows.TriggerBase),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TypeExtension(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              691, "TypeExtension",
                                              typeof(System.Windows.Markup.TypeExtension),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.TypeExtension(); };
            bamlType.HasSpecialValueConverter = true;
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Type) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.Markup.TypeExtension(
                                     (System.Type)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TypeTypeConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              692, "TypeTypeConverter",
                                              typeof(System.Windows.Markup.TypeTypeConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.TypeTypeConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Typography(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              693, "Typography",
                                              typeof(System.Windows.Documents.Typography),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UIElement(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              694, "UIElement",
                                              typeof(System.Windows.UIElement),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.UIElement(); };
            bamlType.UidPropertyName = "Uid";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UInt16(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              695, "UInt16",
                                              typeof(System.UInt16),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.UInt16(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.UInt16Converter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UInt16Converter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              696, "UInt16Converter",
                                              typeof(System.ComponentModel.UInt16Converter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.UInt16Converter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UInt32(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              697, "UInt32",
                                              typeof(System.UInt32),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.UInt32(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.UInt32Converter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UInt32Converter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              698, "UInt32Converter",
                                              typeof(System.ComponentModel.UInt32Converter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.UInt32Converter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UInt64(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              699, "UInt64",
                                              typeof(System.UInt64),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.UInt64(); };
            bamlType.TypeConverterType = typeof(System.ComponentModel.UInt64Converter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UInt64Converter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              700, "UInt64Converter",
                                              typeof(System.ComponentModel.UInt64Converter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.ComponentModel.UInt64Converter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UShortIListConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              701, "UShortIListConverter",
                                              typeof(System.Windows.Media.Converters.UShortIListConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Converters.UShortIListConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Underline(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              702, "Underline",
                                              typeof(System.Windows.Documents.Underline),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.Underline(); };
            bamlType.ContentPropertyName = "Inlines";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UniformGrid(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              703, "UniformGrid",
                                              typeof(System.Windows.Controls.Primitives.UniformGrid),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Primitives.UniformGrid(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Uri(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              704, "Uri",
                                              typeof(System.Uri),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.UriTypeConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UriTypeConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              705, "UriTypeConverter",
                                              typeof(System.UriTypeConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.UriTypeConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UserControl(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              706, "UserControl",
                                              typeof(System.Windows.Controls.UserControl),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.UserControl(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Validation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              707, "Validation",
                                              typeof(System.Windows.Controls.Validation),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              708, "Vector",
                                              typeof(System.Windows.Vector),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Vector(); };
            bamlType.TypeConverterType = typeof(System.Windows.VectorConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              709, "Vector3D",
                                              typeof(System.Windows.Media.Media3D.Vector3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Vector3D(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Vector3DConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              710, "Vector3DAnimation",
                                              typeof(System.Windows.Media.Animation.Vector3DAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Vector3DAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              711, "Vector3DAnimationBase",
                                              typeof(System.Windows.Media.Animation.Vector3DAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              712, "Vector3DAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              713, "Vector3DCollection",
                                              typeof(System.Windows.Media.Media3D.Vector3DCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Vector3DCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Media3D.Vector3DCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              714, "Vector3DCollectionConverter",
                                              typeof(System.Windows.Media.Media3D.Vector3DCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Vector3DCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              715, "Vector3DConverter",
                                              typeof(System.Windows.Media.Media3D.Vector3DConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Vector3DConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              716, "Vector3DKeyFrame",
                                              typeof(System.Windows.Media.Animation.Vector3DKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Vector3DKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              717, "Vector3DKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.Vector3DKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.Vector3DKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorAnimation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              718, "VectorAnimation",
                                              typeof(System.Windows.Media.Animation.VectorAnimation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.VectorAnimation(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorAnimationBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              719, "VectorAnimationBase",
                                              typeof(System.Windows.Media.Animation.VectorAnimationBase),
                                              isBamlType, useV3Rules);
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorAnimationUsingKeyFrames(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              720, "VectorAnimationUsingKeyFrames",
                                              typeof(System.Windows.Media.Animation.VectorAnimationUsingKeyFrames),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.VectorAnimationUsingKeyFrames(); };
            bamlType.ContentPropertyName = "KeyFrames";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              721, "VectorCollection",
                                              typeof(System.Windows.Media.VectorCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.VectorCollection(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.VectorCollectionConverter);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorCollectionConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              722, "VectorCollectionConverter",
                                              typeof(System.Windows.Media.VectorCollectionConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.VectorCollectionConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              723, "VectorConverter",
                                              typeof(System.Windows.VectorConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.VectorConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorKeyFrame(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              724, "VectorKeyFrame",
                                              typeof(System.Windows.Media.Animation.VectorKeyFrame),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VectorKeyFrameCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              725, "VectorKeyFrameCollection",
                                              typeof(System.Windows.Media.Animation.VectorKeyFrameCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Animation.VectorKeyFrameCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VideoDrawing(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              726, "VideoDrawing",
                                              typeof(System.Windows.Media.VideoDrawing),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.VideoDrawing(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ViewBase(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              727, "ViewBase",
                                              typeof(System.Windows.Controls.ViewBase),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Viewbox(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              728, "Viewbox",
                                              typeof(System.Windows.Controls.Viewbox),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Viewbox(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Viewport3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              729, "Viewport3D",
                                              typeof(System.Windows.Controls.Viewport3D),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Viewport3D(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Viewport3DVisual(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              730, "Viewport3DVisual",
                                              typeof(System.Windows.Media.Media3D.Viewport3DVisual),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Media3D.Viewport3DVisual(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VirtualizingPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              731, "VirtualizingPanel",
                                              typeof(System.Windows.Controls.VirtualizingPanel),
                                              isBamlType, useV3Rules);
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VirtualizingStackPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              732, "VirtualizingStackPanel",
                                              typeof(System.Windows.Controls.VirtualizingStackPanel),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.VirtualizingStackPanel(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Visual(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              733, "Visual",
                                              typeof(System.Windows.Media.Visual),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Visual3D(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              734, "Visual3D",
                                              typeof(System.Windows.Media.Media3D.Visual3D),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VisualBrush(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              735, "VisualBrush",
                                              typeof(System.Windows.Media.VisualBrush),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.VisualBrush(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.BrushConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_VisualTarget(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              736, "VisualTarget",
                                              typeof(System.Windows.Media.VisualTarget),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_WeakEventManager(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              737, "WeakEventManager",
                                              typeof(System.Windows.WeakEventManager),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_WhitespaceSignificantCollectionAttribute(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              738, "WhitespaceSignificantCollectionAttribute",
                                              typeof(System.Windows.Markup.WhitespaceSignificantCollectionAttribute),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.WhitespaceSignificantCollectionAttribute(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Window(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              739, "Window",
                                              typeof(System.Windows.Window),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Window(); };
            bamlType.ContentPropertyName = "Content";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_WmpBitmapDecoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              740, "WmpBitmapDecoder",
                                              typeof(System.Windows.Media.Imaging.WmpBitmapDecoder),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_WmpBitmapEncoder(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              741, "WmpBitmapEncoder",
                                              typeof(System.Windows.Media.Imaging.WmpBitmapEncoder),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Imaging.WmpBitmapEncoder(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_WrapPanel(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              742, "WrapPanel",
                                              typeof(System.Windows.Controls.WrapPanel),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.WrapPanel(); };
            bamlType.ContentPropertyName = "Children";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_WriteableBitmap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              743, "WriteableBitmap",
                                              typeof(System.Windows.Media.Imaging.WriteableBitmap),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Media.ImageSourceConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlBrushSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              744, "XamlBrushSerializer",
                                              typeof(System.Windows.Markup.XamlBrushSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlBrushSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlInt32CollectionSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              745, "XamlInt32CollectionSerializer",
                                              typeof(System.Windows.Markup.XamlInt32CollectionSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlInt32CollectionSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlPathDataSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              746, "XamlPathDataSerializer",
                                              typeof(System.Windows.Markup.XamlPathDataSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlPathDataSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlPoint3DCollectionSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              747, "XamlPoint3DCollectionSerializer",
                                              typeof(System.Windows.Markup.XamlPoint3DCollectionSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlPoint3DCollectionSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlPointCollectionSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              748, "XamlPointCollectionSerializer",
                                              typeof(System.Windows.Markup.XamlPointCollectionSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlPointCollectionSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlReader(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              749, "XamlReader",
                                              typeof(System.Windows.Markup.XamlReader),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlReader(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlStyleSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              750, "XamlStyleSerializer",
                                              typeof(System.Windows.Markup.XamlStyleSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlStyleSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlTemplateSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              751, "XamlTemplateSerializer",
                                              typeof(System.Windows.Markup.XamlTemplateSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlTemplateSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlVector3DCollectionSerializer(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              752, "XamlVector3DCollectionSerializer",
                                              typeof(System.Windows.Markup.XamlVector3DCollectionSerializer),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XamlVector3DCollectionSerializer(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XamlWriter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              753, "XamlWriter",
                                              typeof(System.Windows.Markup.XamlWriter),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XmlDataProvider(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              754, "XmlDataProvider",
                                              typeof(System.Windows.Data.XmlDataProvider),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.XmlDataProvider(); };
            bamlType.ContentPropertyName = "XmlSerializer";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XmlLangPropertyAttribute(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              755, "XmlLangPropertyAttribute",
                                              typeof(System.Windows.Markup.XmlLangPropertyAttribute),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XmlLanguage(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              756, "XmlLanguage",
                                              typeof(System.Windows.Markup.XmlLanguage),
                                              isBamlType, useV3Rules);
            bamlType.TypeConverterType = typeof(System.Windows.Markup.XmlLanguageConverter);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XmlLanguageConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              757, "XmlLanguageConverter",
                                              typeof(System.Windows.Markup.XmlLanguageConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Markup.XmlLanguageConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XmlNamespaceMapping(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              758, "XmlNamespaceMapping",
                                              typeof(System.Windows.Data.XmlNamespaceMapping),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.XmlNamespaceMapping(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ZoomPercentageConverter(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              759, "ZoomPercentageConverter",
                                              typeof(System.Windows.Documents.ZoomPercentageConverter),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Documents.ZoomPercentageConverter(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CommandBinding(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "CommandBinding",
                                              typeof(System.Windows.Input.CommandBinding),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.CommandBinding(); };
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_XmlNamespaceMappingCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "XmlNamespaceMappingCollection",
                                              typeof(System.Windows.Data.XmlNamespaceMappingCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.XmlNamespaceMappingCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PageContentCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "PageContentCollection",
                                              typeof(System.Windows.Documents.PageContentCollection),
                                              isBamlType, useV3Rules);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_DocumentReferenceCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "DocumentReferenceCollection",
                                              typeof(System.Windows.Documents.DocumentReferenceCollection),
                                              isBamlType, useV3Rules);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyboardNavigationMode(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "KeyboardNavigationMode",
                                              typeof(System.Windows.Input.KeyboardNavigationMode),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.KeyboardNavigationMode(); };
            bamlType.TypeConverterType = typeof(System.Windows.Input.KeyboardNavigationMode);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Enum(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "Enum",
                                              typeof(System.Enum),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RelativeSourceMode(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "RelativeSourceMode",
                                              typeof(System.Windows.Data.RelativeSourceMode),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Data.RelativeSourceMode(); };
            bamlType.TypeConverterType = typeof(System.Windows.Data.RelativeSourceMode);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PenLineJoin(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "PenLineJoin",
                                              typeof(System.Windows.Media.PenLineJoin),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PenLineJoin(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.PenLineJoin);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_PenLineCap(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "PenLineCap",
                                              typeof(System.Windows.Media.PenLineCap),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.PenLineCap(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.PenLineCap);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_InputBindingCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "InputBindingCollection",
                                              typeof(System.Windows.Input.InputBindingCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.InputBindingCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_CommandBindingCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "CommandBindingCollection",
                                              typeof(System.Windows.Input.CommandBindingCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Input.CommandBindingCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Stretch(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "Stretch",
                                              typeof(System.Windows.Media.Stretch),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Media.Stretch(); };
            bamlType.TypeConverterType = typeof(System.Windows.Media.Stretch);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_Orientation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "Orientation",
                                              typeof(System.Windows.Controls.Orientation),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.Orientation(); };
            bamlType.TypeConverterType = typeof(System.Windows.Controls.Orientation);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TextAlignment(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "TextAlignment",
                                              typeof(System.Windows.TextAlignment),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.TextAlignment(); };
            bamlType.TypeConverterType = typeof(System.Windows.TextAlignment);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_NavigationUIVisibility(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "NavigationUIVisibility",
                                              typeof(System.Windows.Navigation.NavigationUIVisibility),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Navigation.NavigationUIVisibility(); };
            bamlType.TypeConverterType = typeof(System.Windows.Navigation.NavigationUIVisibility);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_JournalOwnership(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "JournalOwnership",
                                              typeof(System.Windows.Navigation.JournalOwnership),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Navigation.JournalOwnership(); };
            bamlType.TypeConverterType = typeof(System.Windows.Navigation.JournalOwnership);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ScrollBarVisibility(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "ScrollBarVisibility",
                                              typeof(System.Windows.Controls.ScrollBarVisibility),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ScrollBarVisibility(); };
            bamlType.TypeConverterType = typeof(System.Windows.Controls.ScrollBarVisibility);
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_TriggerCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "TriggerCollection",
                                              typeof(System.Windows.TriggerCollection),
                                              isBamlType, useV3Rules);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_UIElementCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "UIElementCollection",
                                              typeof(System.Windows.Controls.UIElementCollection),
                                              isBamlType, useV3Rules);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_SetterBaseCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "SetterBaseCollection",
                                              typeof(System.Windows.SetterBaseCollection),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.SetterBaseCollection(); };
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ColumnDefinitionCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "ColumnDefinitionCollection",
                                              typeof(System.Windows.Controls.ColumnDefinitionCollection),
                                              isBamlType, useV3Rules);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_RowDefinitionCollection(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "RowDefinitionCollection",
                                              typeof(System.Windows.Controls.RowDefinitionCollection),
                                              isBamlType, useV3Rules);
            bamlType.CollectionKind = XamlCollectionKind.Collection;
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ItemContainerTemplate(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "ItemContainerTemplate",
                                              typeof(System.Windows.Controls.ItemContainerTemplate),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ItemContainerTemplate(); };
            bamlType.ContentPropertyName = "Template";
            bamlType.DictionaryKeyPropertyName = "ItemContainerTemplateKey";
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_ItemContainerTemplateKey(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "ItemContainerTemplateKey",
                                              typeof(System.Windows.Controls.ItemContainerTemplateKey),
                                              isBamlType, useV3Rules);
            bamlType.DefaultConstructor = delegate() { return new System.Windows.Controls.ItemContainerTemplateKey(); };
            bamlType.TypeConverterType = typeof(System.Windows.Markup.TemplateKeyConverter);
            bamlType.Constructors.Add(1, new Baml6ConstructorInfo(
                            new List<Type>() { typeof(System.Object) },
                            delegate(object[] arguments)
                            {
                                return new System.Windows.Controls.ItemContainerTemplateKey(
                                     (System.Object)arguments[0]);
                            }));
            bamlType.Freeze();
            return bamlType;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WpfKnownType Create_BamlType_KeyboardNavigation(bool isBamlType, bool useV3Rules)
        {
            var bamlType = new WpfKnownType(this, // SchemaContext
                                              0, "KeyboardNavigation",
                                              typeof(System.Windows.Input.KeyboardNavigation),
                                              isBamlType, useV3Rules);
            bamlType.Freeze();
            return bamlType;
        }
    }
}
