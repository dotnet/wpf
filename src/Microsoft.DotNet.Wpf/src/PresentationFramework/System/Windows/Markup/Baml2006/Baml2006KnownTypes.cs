// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Converters;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows;
using System.Xaml;
using System;

namespace System.Windows.Baml2006
{
    internal partial class Baml2006SchemaContext : XamlSchemaContext
    {
        private delegate Type LazyTypeOf();

        internal static class KnownTypes
        {
            public const Int16 BooleanConverter = 46;
            public const Int16 DependencyPropertyConverter = 137;
            public const Int16 EnumConverter = 195;
            public const Int16 StringConverter = 615;

            public const Int16 XamlBrushSerializer = 744;
            public const Int16 XamlInt32CollectionSerializer = 745;
            public const Int16 XamlPathDataSerializer = 746;
            public const Int16 XamlPoint3DCollectionSerializer = 747;
            public const Int16 XamlPointCollectionSerializer = 748;
            public const Int16 XamlVector3DCollectionSerializer = 752;

            public const Int16 MaxKnownType = 759;
            public const Int16 MaxKnownProperty = 268;
            public const Int16 MinKnownProperty = -268;

            public const Int16 VisualTreeKnownPropertyId = -174;

            public static Type GetAttachableTargetType(Int16 propertyId)
            {
                switch (propertyId)
                {
                    case -39: //DockPanel.Dock
                        return typeof(System.Windows.UIElement);
                    case -61: //Grid.Column
                        return typeof(System.Windows.UIElement);
                    case -62: //Grid.ColumnSpan
                        return typeof(System.Windows.UIElement);
                    case -63: //Grid.Row
                        return typeof(System.Windows.UIElement);
                    case -64: //Grid.RowSpan
                        return typeof(System.Windows.UIElement);
                    default:
                        return typeof(System.Windows.DependencyObject);
                }
            }

            public static Assembly GetKnownAssembly(Int16 assemblyId)
            {
                Assembly assembly;

                switch (-assemblyId)
                {
                    case 0: assembly = typeof(double).Assembly; break;
                    case 1: assembly = typeof(System.Uri).Assembly; break;
                    case 2: assembly = typeof(System.Windows.DependencyObject).Assembly; break;
                    case 3: assembly = typeof(System.Windows.UIElement).Assembly; break;
                    case 4: assembly = typeof(System.Windows.FrameworkElement).Assembly; break;
                    default: assembly = null; break;
                }

                return assembly;
            }

            public static Type GetKnownType(Int16 typeId)
            {
                typeId = (Int16)(-typeId);

                LazyTypeOf t;
                switch (typeId)
                {
                    case 1: t = () => typeof(AccessText); break;
                    case 2: t = () => typeof(AdornedElementPlaceholder); break;
                    case 3: t = () => typeof(Adorner); break;
                    case 4: t = () => typeof(AdornerDecorator); break;
                    case 5: t = () => typeof(AdornerLayer); break;
                    case 6: t = () => typeof(AffineTransform3D); break;
                    case 7: t = () => typeof(AmbientLight); break;
                    case 8: t = () => typeof(AnchoredBlock); break;
                    case 9: t = () => typeof(Animatable); break;
                    case 10: t = () => typeof(AnimationClock); break;
                    case 11: t = () => typeof(AnimationTimeline); break;
                    case 12: t = () => typeof(Application); break;
                    case 13: t = () => typeof(ArcSegment); break;
                    case 14: t = () => typeof(ArrayExtension); break;
                    case 15: t = () => typeof(AxisAngleRotation3D); break;
                    case 16: t = () => typeof(BaseIListConverter); break;
                    case 17: t = () => typeof(BeginStoryboard); break;
                    case 18: t = () => typeof(BevelBitmapEffect); break;
                    case 19: t = () => typeof(BezierSegment); break;
                    case 20: t = () => typeof(Binding); break;
                    case 21: t = () => typeof(BindingBase); break;
                    case 22: t = () => typeof(BindingExpression); break;
                    case 23: t = () => typeof(BindingExpressionBase); break;
                    case 24: t = () => typeof(BindingListCollectionView); break;
                    case 25: t = () => typeof(BitmapDecoder); break;
                    case 26: t = () => typeof(BitmapEffect); break;
                    case 27: t = () => typeof(BitmapEffectCollection); break;
                    case 28: t = () => typeof(BitmapEffectGroup); break;
                    case 29: t = () => typeof(BitmapEffectInput); break;
                    case 30: t = () => typeof(BitmapEncoder); break;
                    case 31: t = () => typeof(BitmapFrame); break;
                    case 32: t = () => typeof(BitmapImage); break;
                    case 33: t = () => typeof(BitmapMetadata); break;
                    case 34: t = () => typeof(BitmapPalette); break;
                    case 35: t = () => typeof(BitmapSource); break;
                    case 36: t = () => typeof(Block); break;
                    case 37: t = () => typeof(BlockUIContainer); break;
                    case 38: t = () => typeof(BlurBitmapEffect); break;
                    case 39: t = () => typeof(BmpBitmapDecoder); break;
                    case 40: t = () => typeof(BmpBitmapEncoder); break;
                    case 41: t = () => typeof(Bold); break;
                    case 42: t = () => typeof(BoolIListConverter); break;
                    case 43: t = () => typeof(Boolean); break;
                    case 44: t = () => typeof(BooleanAnimationBase); break;
                    case 45: t = () => typeof(BooleanAnimationUsingKeyFrames); break;
                    case 46: t = () => typeof(BooleanConverter); break;
                    case 47: t = () => typeof(BooleanKeyFrame); break;
                    case 48: t = () => typeof(BooleanKeyFrameCollection); break;
                    case 49: t = () => typeof(BooleanToVisibilityConverter); break;
                    case 50: t = () => typeof(Border); break;
                    case 51: t = () => typeof(BorderGapMaskConverter); break;
                    case 52: t = () => typeof(Brush); break;
                    case 53: t = () => typeof(BrushConverter); break;
                    case 54: t = () => typeof(BulletDecorator); break;
                    case 55: t = () => typeof(Button); break;
                    case 56: t = () => typeof(ButtonBase); break;
                    case 57: t = () => typeof(Byte); break;
                    case 58: t = () => typeof(ByteAnimation); break;
                    case 59: t = () => typeof(ByteAnimationBase); break;
                    case 60: t = () => typeof(ByteAnimationUsingKeyFrames); break;
                    case 61: t = () => typeof(ByteConverter); break;
                    case 62: t = () => typeof(ByteKeyFrame); break;
                    case 63: t = () => typeof(ByteKeyFrameCollection); break;
                    case 64: t = () => typeof(CachedBitmap); break;
                    case 65: t = () => typeof(Camera); break;
                    case 66: t = () => typeof(Canvas); break;
                    case 67: t = () => typeof(Char); break;
                    case 68: t = () => typeof(CharAnimationBase); break;
                    case 69: t = () => typeof(CharAnimationUsingKeyFrames); break;
                    case 70: t = () => typeof(CharConverter); break;
                    case 71: t = () => typeof(CharIListConverter); break;
                    case 72: t = () => typeof(CharKeyFrame); break;
                    case 73: t = () => typeof(CharKeyFrameCollection); break;
                    case 74: t = () => typeof(CheckBox); break;
                    case 75: t = () => typeof(Clock); break;
                    case 76: t = () => typeof(ClockController); break;
                    case 77: t = () => typeof(ClockGroup); break;
                    case 78: t = () => typeof(CollectionContainer); break;
                    case 79: t = () => typeof(CollectionView); break;
                    case 80: t = () => typeof(CollectionViewSource); break;
                    case 81: t = () => typeof(Color); break;
                    case 82: t = () => typeof(ColorAnimation); break;
                    case 83: t = () => typeof(ColorAnimationBase); break;
                    case 84: t = () => typeof(ColorAnimationUsingKeyFrames); break;
                    case 85: t = () => typeof(ColorConvertedBitmap); break;
                    case 86: t = () => typeof(ColorConvertedBitmapExtension); break;
                    case 87: t = () => typeof(ColorConverter); break;
                    case 88: t = () => typeof(ColorKeyFrame); break;
                    case 89: t = () => typeof(ColorKeyFrameCollection); break;
                    case 90: t = () => typeof(ColumnDefinition); break;
                    case 91: t = () => typeof(CombinedGeometry); break;
                    case 92: t = () => typeof(ComboBox); break;
                    case 93: t = () => typeof(ComboBoxItem); break;
                    case 94: t = () => typeof(CommandConverter); break;
                    case 95: t = () => typeof(ComponentResourceKey); break;
                    case 96: t = () => typeof(ComponentResourceKeyConverter); break;
                    case 97: t = () => typeof(CompositionTarget); break;
                    case 98: t = () => typeof(Condition); break;
                    case 99: t = () => typeof(ContainerVisual); break;
                    case 100: t = () => typeof(ContentControl); break;
                    case 101: t = () => typeof(ContentElement); break;
                    case 102: t = () => typeof(ContentPresenter); break;
                    case 103: t = () => typeof(ContentPropertyAttribute); break;
                    case 104: t = () => typeof(ContentWrapperAttribute); break;
                    case 105: t = () => typeof(ContextMenu); break;
                    case 106: t = () => typeof(ContextMenuService); break;
                    case 107: t = () => typeof(Control); break;
                    case 108: t = () => typeof(ControlTemplate); break;
                    case 109: t = () => typeof(ControllableStoryboardAction); break;
                    case 110: t = () => typeof(CornerRadius); break;
                    case 111: t = () => typeof(CornerRadiusConverter); break;
                    case 112: t = () => typeof(CroppedBitmap); break;
                    case 113: t = () => typeof(CultureInfo); break;
                    case 114: t = () => typeof(CultureInfoConverter); break;
                    case 115: t = () => typeof(CultureInfoIetfLanguageTagConverter); break;
                    case 116: t = () => typeof(Cursor); break;
                    case 117: t = () => typeof(CursorConverter); break;
                    case 118: t = () => typeof(DashStyle); break;
                    case 119: t = () => typeof(DataChangedEventManager); break;
                    case 120: t = () => typeof(DataTemplate); break;
                    case 121: t = () => typeof(DataTemplateKey); break;
                    case 122: t = () => typeof(DataTrigger); break;
                    case 123: t = () => typeof(DateTime); break;
                    case 124: t = () => typeof(DateTimeConverter); break;
                    case 125: t = () => typeof(DateTimeConverter2); break;
                    case 126: t = () => typeof(Decimal); break;
                    case 127: t = () => typeof(DecimalAnimation); break;
                    case 128: t = () => typeof(DecimalAnimationBase); break;
                    case 129: t = () => typeof(DecimalAnimationUsingKeyFrames); break;
                    case 130: t = () => typeof(DecimalConverter); break;
                    case 131: t = () => typeof(DecimalKeyFrame); break;
                    case 132: t = () => typeof(DecimalKeyFrameCollection); break;
                    case 133: t = () => typeof(Decorator); break;
                    case 134: t = () => typeof(DefinitionBase); break;
                    case 135: t = () => typeof(DependencyObject); break;
                    case 136: t = () => typeof(DependencyProperty); break;
                    case 137: t = () => typeof(DependencyPropertyConverter); break;
                    case 138: t = () => typeof(DialogResultConverter); break;
                    case 139: t = () => typeof(DiffuseMaterial); break;
                    case 140: t = () => typeof(DirectionalLight); break;
                    case 141: t = () => typeof(DiscreteBooleanKeyFrame); break;
                    case 142: t = () => typeof(DiscreteByteKeyFrame); break;
                    case 143: t = () => typeof(DiscreteCharKeyFrame); break;
                    case 144: t = () => typeof(DiscreteColorKeyFrame); break;
                    case 145: t = () => typeof(DiscreteDecimalKeyFrame); break;
                    case 146: t = () => typeof(DiscreteDoubleKeyFrame); break;
                    case 147: t = () => typeof(DiscreteInt16KeyFrame); break;
                    case 148: t = () => typeof(DiscreteInt32KeyFrame); break;
                    case 149: t = () => typeof(DiscreteInt64KeyFrame); break;
                    case 150: t = () => typeof(DiscreteMatrixKeyFrame); break;
                    case 151: t = () => typeof(DiscreteObjectKeyFrame); break;
                    case 152: t = () => typeof(DiscretePoint3DKeyFrame); break;
                    case 153: t = () => typeof(DiscretePointKeyFrame); break;
                    case 154: t = () => typeof(DiscreteQuaternionKeyFrame); break;
                    case 155: t = () => typeof(DiscreteRectKeyFrame); break;
                    case 156: t = () => typeof(DiscreteRotation3DKeyFrame); break;
                    case 157: t = () => typeof(DiscreteSingleKeyFrame); break;
                    case 158: t = () => typeof(DiscreteSizeKeyFrame); break;
                    case 159: t = () => typeof(DiscreteStringKeyFrame); break;
                    case 160: t = () => typeof(DiscreteThicknessKeyFrame); break;
                    case 161: t = () => typeof(DiscreteVector3DKeyFrame); break;
                    case 162: t = () => typeof(DiscreteVectorKeyFrame); break;
                    case 163: t = () => typeof(DockPanel); break;
                    case 164: t = () => typeof(DocumentPageView); break;
                    case 165: t = () => typeof(DocumentReference); break;
                    case 166: t = () => typeof(DocumentViewer); break;
                    case 167: t = () => typeof(DocumentViewerBase); break;
                    case 168: t = () => typeof(Double); break;
                    case 169: t = () => typeof(DoubleAnimation); break;
                    case 170: t = () => typeof(DoubleAnimationBase); break;
                    case 171: t = () => typeof(DoubleAnimationUsingKeyFrames); break;
                    case 172: t = () => typeof(DoubleAnimationUsingPath); break;
                    case 173: t = () => typeof(DoubleCollection); break;
                    case 174: t = () => typeof(DoubleCollectionConverter); break;
                    case 175: t = () => typeof(DoubleConverter); break;
                    case 176: t = () => typeof(DoubleIListConverter); break;
                    case 177: t = () => typeof(DoubleKeyFrame); break;
                    case 178: t = () => typeof(DoubleKeyFrameCollection); break;
                    case 179: t = () => typeof(System.Windows.Media.Drawing); break; // ambiguous
                    case 180: t = () => typeof(DrawingBrush); break;
                    case 181: t = () => typeof(DrawingCollection); break;
                    case 182: t = () => typeof(DrawingContext); break;
                    case 183: t = () => typeof(DrawingGroup); break;
                    case 184: t = () => typeof(DrawingImage); break;
                    case 185: t = () => typeof(DrawingVisual); break;
                    case 186: t = () => typeof(DropShadowBitmapEffect); break;
                    case 187: t = () => typeof(Duration); break;
                    case 188: t = () => typeof(DurationConverter); break;
                    case 189: t = () => typeof(DynamicResourceExtension); break;
                    case 190: t = () => typeof(DynamicResourceExtensionConverter); break;
                    case 191: t = () => typeof(Ellipse); break;
                    case 192: t = () => typeof(EllipseGeometry); break;
                    case 193: t = () => typeof(EmbossBitmapEffect); break;
                    case 194: t = () => typeof(EmissiveMaterial); break;
                    case 195: t = () => typeof(EnumConverter); break;
                    case 196: t = () => typeof(EventManager); break;
                    case 197: t = () => typeof(EventSetter); break;
                    case 198: t = () => typeof(EventTrigger); break;
                    case 199: t = () => typeof(Expander); break;
                    case 200: t = () => typeof(Expression); break;
                    case 201: t = () => typeof(ExpressionConverter); break;
                    case 202: t = () => typeof(Figure); break;
                    case 203: t = () => typeof(FigureLength); break;
                    case 204: t = () => typeof(FigureLengthConverter); break;
                    case 205: t = () => typeof(FixedDocument); break;
                    case 206: t = () => typeof(FixedDocumentSequence); break;
                    case 207: t = () => typeof(FixedPage); break;
                    case 208: t = () => typeof(Floater); break;
                    case 209: t = () => typeof(FlowDocument); break;
                    case 210: t = () => typeof(FlowDocumentPageViewer); break;
                    case 211: t = () => typeof(FlowDocumentReader); break;
                    case 212: t = () => typeof(FlowDocumentScrollViewer); break;
                    case 213: t = () => typeof(FocusManager); break;
                    case 214: t = () => typeof(FontFamily); break;
                    case 215: t = () => typeof(FontFamilyConverter); break;
                    case 216: t = () => typeof(FontSizeConverter); break;
                    case 217: t = () => typeof(FontStretch); break;
                    case 218: t = () => typeof(FontStretchConverter); break;
                    case 219: t = () => typeof(FontStyle); break;
                    case 220: t = () => typeof(FontStyleConverter); break;
                    case 221: t = () => typeof(FontWeight); break;
                    case 222: t = () => typeof(FontWeightConverter); break;
                    case 223: t = () => typeof(FormatConvertedBitmap); break;
                    case 224: t = () => typeof(Frame); break;
                    case 225: t = () => typeof(FrameworkContentElement); break;
                    case 226: t = () => typeof(FrameworkElement); break;
                    case 227: t = () => typeof(FrameworkElementFactory); break;
                    case 228: t = () => typeof(FrameworkPropertyMetadata); break;
                    case 229: t = () => typeof(FrameworkPropertyMetadataOptions); break;
                    case 230: t = () => typeof(FrameworkRichTextComposition); break;
                    case 231: t = () => typeof(FrameworkTemplate); break;
                    case 232: t = () => typeof(FrameworkTextComposition); break;
                    case 233: t = () => typeof(Freezable); break;
                    case 234: t = () => typeof(GeneralTransform); break;
                    case 235: t = () => typeof(GeneralTransformCollection); break;
                    case 236: t = () => typeof(GeneralTransformGroup); break;
                    case 237: t = () => typeof(Geometry); break;
                    case 238: t = () => typeof(Geometry3D); break;
                    case 239: t = () => typeof(GeometryCollection); break;
                    case 240: t = () => typeof(GeometryConverter); break;
                    case 241: t = () => typeof(GeometryDrawing); break;
                    case 242: t = () => typeof(GeometryGroup); break;
                    case 243: t = () => typeof(GeometryModel3D); break;
                    case 244: t = () => typeof(GestureRecognizer); break;
                    case 245: t = () => typeof(GifBitmapDecoder); break;
                    case 246: t = () => typeof(GifBitmapEncoder); break;
                    case 247: t = () => typeof(GlyphRun); break;
                    case 248: t = () => typeof(GlyphRunDrawing); break;
                    case 249: t = () => typeof(GlyphTypeface); break;
                    case 250: t = () => typeof(Glyphs); break;
                    case 251: t = () => typeof(GradientBrush); break;
                    case 252: t = () => typeof(GradientStop); break;
                    case 253: t = () => typeof(GradientStopCollection); break;
                    case 254: t = () => typeof(Grid); break;
                    case 255: t = () => typeof(GridLength); break;
                    case 256: t = () => typeof(GridLengthConverter); break;
                    case 257: t = () => typeof(GridSplitter); break;
                    case 258: t = () => typeof(GridView); break;
                    case 259: t = () => typeof(GridViewColumn); break;
                    case 260: t = () => typeof(GridViewColumnHeader); break;
                    case 261: t = () => typeof(GridViewHeaderRowPresenter); break;
                    case 262: t = () => typeof(GridViewRowPresenter); break;
                    case 263: t = () => typeof(GridViewRowPresenterBase); break;
                    case 264: t = () => typeof(GroupBox); break;
                    case 265: t = () => typeof(GroupItem); break;
                    case 266: t = () => typeof(Guid); break;
                    case 267: t = () => typeof(GuidConverter); break;
                    case 268: t = () => typeof(GuidelineSet); break;
                    case 269: t = () => typeof(HeaderedContentControl); break;
                    case 270: t = () => typeof(HeaderedItemsControl); break;
                    case 271: t = () => typeof(HierarchicalDataTemplate); break;
                    case 272: t = () => typeof(HostVisual); break;
                    case 273: t = () => typeof(Hyperlink); break;
                    case 274: t = () => typeof(IAddChild); break;
                    case 275: t = () => typeof(IAddChildInternal); break;
                    case 276: t = () => typeof(ICommand); break;
                    case 277: t = () => typeof(IComponentConnector); break;
                    case 278: t = () => typeof(INameScope); break;
                    case 279: t = () => typeof(IStyleConnector); break;
                    case 280: t = () => typeof(IconBitmapDecoder); break;
                    case 281: t = () => typeof(Image); break;
                    case 282: t = () => typeof(ImageBrush); break;
                    case 283: t = () => typeof(ImageDrawing); break;
                    case 284: t = () => typeof(ImageMetadata); break;
                    case 285: t = () => typeof(ImageSource); break;
                    case 286: t = () => typeof(ImageSourceConverter); break;
                    case 287: t = () => typeof(InPlaceBitmapMetadataWriter); break;
                    case 288: t = () => typeof(InkCanvas); break;
                    case 289: t = () => typeof(InkPresenter); break;
                    case 290: t = () => typeof(Inline); break;
                    case 291: t = () => typeof(InlineCollection); break;
                    case 292: t = () => typeof(InlineUIContainer); break;
                    case 293: t = () => typeof(InputBinding); break;
                    case 294: t = () => typeof(InputDevice); break;
                    case 295: t = () => typeof(InputLanguageManager); break;
                    case 296: t = () => typeof(InputManager); break;
                    case 297: t = () => typeof(InputMethod); break;
                    case 298: t = () => typeof(InputScope); break;
                    case 299: t = () => typeof(InputScopeConverter); break;
                    case 300: t = () => typeof(InputScopeName); break;
                    case 301: t = () => typeof(InputScopeNameConverter); break;
                    case 302: t = () => typeof(Int16); break;
                    case 303: t = () => typeof(Int16Animation); break;
                    case 304: t = () => typeof(Int16AnimationBase); break;
                    case 305: t = () => typeof(Int16AnimationUsingKeyFrames); break;
                    case 306: t = () => typeof(Int16Converter); break;
                    case 307: t = () => typeof(Int16KeyFrame); break;
                    case 308: t = () => typeof(Int16KeyFrameCollection); break;
                    case 309: t = () => typeof(Int32); break;
                    case 310: t = () => typeof(Int32Animation); break;
                    case 311: t = () => typeof(Int32AnimationBase); break;
                    case 312: t = () => typeof(Int32AnimationUsingKeyFrames); break;
                    case 313: t = () => typeof(Int32Collection); break;
                    case 314: t = () => typeof(Int32CollectionConverter); break;
                    case 315: t = () => typeof(Int32Converter); break;
                    case 316: t = () => typeof(Int32KeyFrame); break;
                    case 317: t = () => typeof(Int32KeyFrameCollection); break;
                    case 318: t = () => typeof(Int32Rect); break;
                    case 319: t = () => typeof(Int32RectConverter); break;
                    case 320: t = () => typeof(Int64); break;
                    case 321: t = () => typeof(Int64Animation); break;
                    case 322: t = () => typeof(Int64AnimationBase); break;
                    case 323: t = () => typeof(Int64AnimationUsingKeyFrames); break;
                    case 324: t = () => typeof(Int64Converter); break;
                    case 325: t = () => typeof(Int64KeyFrame); break;
                    case 326: t = () => typeof(Int64KeyFrameCollection); break;
                    case 327: t = () => typeof(Italic); break;
                    case 328: t = () => typeof(ItemCollection); break;
                    case 329: t = () => typeof(ItemsControl); break;
                    case 330: t = () => typeof(ItemsPanelTemplate); break;
                    case 331: t = () => typeof(ItemsPresenter); break;
                    case 332: t = () => typeof(JournalEntry); break;
                    case 333: t = () => typeof(JournalEntryListConverter); break;
                    case 334: t = () => typeof(JournalEntryUnifiedViewConverter); break;
                    case 335: t = () => typeof(JpegBitmapDecoder); break;
                    case 336: t = () => typeof(JpegBitmapEncoder); break;
                    case 337: t = () => typeof(KeyBinding); break;
                    case 338: t = () => typeof(KeyConverter); break;
                    case 339: t = () => typeof(KeyGesture); break;
                    case 340: t = () => typeof(KeyGestureConverter); break;
                    case 341: t = () => typeof(KeySpline); break;
                    case 342: t = () => typeof(KeySplineConverter); break;
                    case 343: t = () => typeof(KeyTime); break;
                    case 344: t = () => typeof(KeyTimeConverter); break;
                    case 345: t = () => typeof(KeyboardDevice); break;
                    case 346: t = () => typeof(Label); break;
                    case 347: t = () => typeof(LateBoundBitmapDecoder); break;
                    case 348: t = () => typeof(LengthConverter); break;
                    case 349: t = () => typeof(Light); break;
                    case 350: t = () => typeof(Line); break;
                    case 351: t = () => typeof(LineBreak); break;
                    case 352: t = () => typeof(LineGeometry); break;
                    case 353: t = () => typeof(LineSegment); break;
                    case 354: t = () => typeof(LinearByteKeyFrame); break;
                    case 355: t = () => typeof(LinearColorKeyFrame); break;
                    case 356: t = () => typeof(LinearDecimalKeyFrame); break;
                    case 357: t = () => typeof(LinearDoubleKeyFrame); break;
                    case 358: t = () => typeof(LinearGradientBrush); break;
                    case 359: t = () => typeof(LinearInt16KeyFrame); break;
                    case 360: t = () => typeof(LinearInt32KeyFrame); break;
                    case 361: t = () => typeof(LinearInt64KeyFrame); break;
                    case 362: t = () => typeof(LinearPoint3DKeyFrame); break;
                    case 363: t = () => typeof(LinearPointKeyFrame); break;
                    case 364: t = () => typeof(LinearQuaternionKeyFrame); break;
                    case 365: t = () => typeof(LinearRectKeyFrame); break;
                    case 366: t = () => typeof(LinearRotation3DKeyFrame); break;
                    case 367: t = () => typeof(LinearSingleKeyFrame); break;
                    case 368: t = () => typeof(LinearSizeKeyFrame); break;
                    case 369: t = () => typeof(LinearThicknessKeyFrame); break;
                    case 370: t = () => typeof(LinearVector3DKeyFrame); break;
                    case 371: t = () => typeof(LinearVectorKeyFrame); break;
                    case 372: t = () => typeof(List); break;
                    case 373: t = () => typeof(ListBox); break;
                    case 374: t = () => typeof(ListBoxItem); break;
                    case 375: t = () => typeof(ListCollectionView); break;
                    case 376: t = () => typeof(ListItem); break;
                    case 377: t = () => typeof(ListView); break;
                    case 378: t = () => typeof(ListViewItem); break;
                    case 379: t = () => typeof(Localization); break;
                    case 380: t = () => typeof(LostFocusEventManager); break;
                    case 381: t = () => typeof(MarkupExtension); break;
                    case 382: t = () => typeof(Material); break;
                    case 383: t = () => typeof(MaterialCollection); break;
                    case 384: t = () => typeof(MaterialGroup); break;
                    case 385: t = () => typeof(Matrix); break;
                    case 386: t = () => typeof(Matrix3D); break;
                    case 387: t = () => typeof(Matrix3DConverter); break;
                    case 388: t = () => typeof(MatrixAnimationBase); break;
                    case 389: t = () => typeof(MatrixAnimationUsingKeyFrames); break;
                    case 390: t = () => typeof(MatrixAnimationUsingPath); break;
                    case 391: t = () => typeof(MatrixCamera); break;
                    case 392: t = () => typeof(MatrixConverter); break;
                    case 393: t = () => typeof(MatrixKeyFrame); break;
                    case 394: t = () => typeof(MatrixKeyFrameCollection); break;
                    case 395: t = () => typeof(MatrixTransform); break;
                    case 396: t = () => typeof(MatrixTransform3D); break;
                    case 397: t = () => typeof(MediaClock); break;
                    case 398: t = () => typeof(MediaElement); break;
                    case 399: t = () => typeof(MediaPlayer); break;
                    case 400: t = () => typeof(MediaTimeline); break;
                    case 401: t = () => typeof(Menu); break;
                    case 402: t = () => typeof(MenuBase); break;
                    case 403: t = () => typeof(MenuItem); break;
                    case 404: t = () => typeof(MenuScrollingVisibilityConverter); break;
                    case 405: t = () => typeof(MeshGeometry3D); break;
                    case 406: t = () => typeof(Model3D); break;
                    case 407: t = () => typeof(Model3DCollection); break;
                    case 408: t = () => typeof(Model3DGroup); break;
                    case 409: t = () => typeof(ModelVisual3D); break;
                    case 410: t = () => typeof(ModifierKeysConverter); break;
                    case 411: t = () => typeof(MouseActionConverter); break;
                    case 412: t = () => typeof(MouseBinding); break;
                    case 413: t = () => typeof(MouseDevice); break;
                    case 414: t = () => typeof(MouseGesture); break;
                    case 415: t = () => typeof(MouseGestureConverter); break;
                    case 416: t = () => typeof(MultiBinding); break;
                    case 417: t = () => typeof(MultiBindingExpression); break;
                    case 418: t = () => typeof(MultiDataTrigger); break;
                    case 419: t = () => typeof(MultiTrigger); break;
                    case 420: t = () => typeof(NameScope); break;
                    case 421: t = () => typeof(NavigationWindow); break;
                    case 422: t = () => typeof(NullExtension); break;
                    case 423: t = () => typeof(NullableBoolConverter); break;
                    case 424: t = () => typeof(NullableConverter); break;
                    case 425: t = () => typeof(NumberSubstitution); break;
                    case 426: t = () => typeof(Object); break;
                    case 427: t = () => typeof(ObjectAnimationBase); break;
                    case 428: t = () => typeof(ObjectAnimationUsingKeyFrames); break;
                    case 429: t = () => typeof(ObjectDataProvider); break;
                    case 430: t = () => typeof(ObjectKeyFrame); break;
                    case 431: t = () => typeof(ObjectKeyFrameCollection); break;
                    case 432: t = () => typeof(OrthographicCamera); break;
                    case 433: t = () => typeof(OuterGlowBitmapEffect); break;
                    case 434: t = () => typeof(Page); break;
                    case 435: t = () => typeof(PageContent); break;
                    case 436: t = () => typeof(PageFunctionBase); break;
                    case 437: t = () => typeof(Panel); break;
                    case 438: t = () => typeof(Paragraph); break;
                    case 439: t = () => typeof(ParallelTimeline); break;
                    case 440: t = () => typeof(ParserContext); break;
                    case 441: t = () => typeof(PasswordBox); break;
                    case 442: t = () => typeof(Path); break;
                    case 443: t = () => typeof(PathFigure); break;
                    case 444: t = () => typeof(PathFigureCollection); break;
                    case 445: t = () => typeof(PathFigureCollectionConverter); break;
                    case 446: t = () => typeof(PathGeometry); break;
                    case 447: t = () => typeof(PathSegment); break;
                    case 448: t = () => typeof(PathSegmentCollection); break;
                    case 449: t = () => typeof(PauseStoryboard); break;
                    case 450: t = () => typeof(Pen); break;
                    case 451: t = () => typeof(PerspectiveCamera); break;
                    case 452: t = () => typeof(PixelFormat); break;
                    case 453: t = () => typeof(PixelFormatConverter); break;
                    case 454: t = () => typeof(PngBitmapDecoder); break;
                    case 455: t = () => typeof(PngBitmapEncoder); break;
                    case 456: t = () => typeof(Point); break;
                    case 457: t = () => typeof(Point3D); break;
                    case 458: t = () => typeof(Point3DAnimation); break;
                    case 459: t = () => typeof(Point3DAnimationBase); break;
                    case 460: t = () => typeof(Point3DAnimationUsingKeyFrames); break;
                    case 461: t = () => typeof(Point3DCollection); break;
                    case 462: t = () => typeof(Point3DCollectionConverter); break;
                    case 463: t = () => typeof(Point3DConverter); break;
                    case 464: t = () => typeof(Point3DKeyFrame); break;
                    case 465: t = () => typeof(Point3DKeyFrameCollection); break;
                    case 466: t = () => typeof(Point4D); break;
                    case 467: t = () => typeof(Point4DConverter); break;
                    case 468: t = () => typeof(PointAnimation); break;
                    case 469: t = () => typeof(PointAnimationBase); break;
                    case 470: t = () => typeof(PointAnimationUsingKeyFrames); break;
                    case 471: t = () => typeof(PointAnimationUsingPath); break;
                    case 472: t = () => typeof(PointCollection); break;
                    case 473: t = () => typeof(PointCollectionConverter); break;
                    case 474: t = () => typeof(PointConverter); break;
                    case 475: t = () => typeof(PointIListConverter); break;
                    case 476: t = () => typeof(PointKeyFrame); break;
                    case 477: t = () => typeof(PointKeyFrameCollection); break;
                    case 478: t = () => typeof(PointLight); break;
                    case 479: t = () => typeof(PointLightBase); break;
                    case 480: t = () => typeof(PolyBezierSegment); break;
                    case 481: t = () => typeof(PolyLineSegment); break;
                    case 482: t = () => typeof(PolyQuadraticBezierSegment); break;
                    case 483: t = () => typeof(Polygon); break;
                    case 484: t = () => typeof(Polyline); break;
                    case 485: t = () => typeof(Popup); break;
                    case 486: t = () => typeof(PresentationSource); break;
                    case 487: t = () => typeof(PriorityBinding); break;
                    case 488: t = () => typeof(PriorityBindingExpression); break;
                    case 489: t = () => typeof(ProgressBar); break;
                    case 490: t = () => typeof(ProjectionCamera); break;
                    case 491: t = () => typeof(PropertyPath); break;
                    case 492: t = () => typeof(PropertyPathConverter); break;
                    case 493: t = () => typeof(QuadraticBezierSegment); break;
                    case 494: t = () => typeof(Quaternion); break;
                    case 495: t = () => typeof(QuaternionAnimation); break;
                    case 496: t = () => typeof(QuaternionAnimationBase); break;
                    case 497: t = () => typeof(QuaternionAnimationUsingKeyFrames); break;
                    case 498: t = () => typeof(QuaternionConverter); break;
                    case 499: t = () => typeof(QuaternionKeyFrame); break;
                    case 500: t = () => typeof(QuaternionKeyFrameCollection); break;
                    case 501: t = () => typeof(QuaternionRotation3D); break;
                    case 502: t = () => typeof(RadialGradientBrush); break;
                    case 503: t = () => typeof(RadioButton); break;
                    case 504: t = () => typeof(RangeBase); break;
                    case 505: t = () => typeof(Rect); break;
                    case 506: t = () => typeof(Rect3D); break;
                    case 507: t = () => typeof(Rect3DConverter); break;
                    case 508: t = () => typeof(RectAnimation); break;
                    case 509: t = () => typeof(RectAnimationBase); break;
                    case 510: t = () => typeof(RectAnimationUsingKeyFrames); break;
                    case 511: t = () => typeof(RectConverter); break;
                    case 512: t = () => typeof(RectKeyFrame); break;
                    case 513: t = () => typeof(RectKeyFrameCollection); break;
                    case 514: t = () => typeof(Rectangle); break;
                    case 515: t = () => typeof(RectangleGeometry); break;
                    case 516: t = () => typeof(RelativeSource); break;
                    case 517: t = () => typeof(RemoveStoryboard); break;
                    case 518: t = () => typeof(RenderOptions); break;
                    case 519: t = () => typeof(RenderTargetBitmap); break;
                    case 520: t = () => typeof(RepeatBehavior); break;
                    case 521: t = () => typeof(RepeatBehaviorConverter); break;
                    case 522: t = () => typeof(RepeatButton); break;
                    case 523: t = () => typeof(ResizeGrip); break;
                    case 524: t = () => typeof(ResourceDictionary); break;
                    case 525: t = () => typeof(ResourceKey); break;
                    case 526: t = () => typeof(ResumeStoryboard); break;
                    case 527: t = () => typeof(RichTextBox); break;
                    case 528: t = () => typeof(RotateTransform); break;
                    case 529: t = () => typeof(RotateTransform3D); break;
                    case 530: t = () => typeof(Rotation3D); break;
                    case 531: t = () => typeof(Rotation3DAnimation); break;
                    case 532: t = () => typeof(Rotation3DAnimationBase); break;
                    case 533: t = () => typeof(Rotation3DAnimationUsingKeyFrames); break;
                    case 534: t = () => typeof(Rotation3DKeyFrame); break;
                    case 535: t = () => typeof(Rotation3DKeyFrameCollection); break;
                    case 536: t = () => typeof(RoutedCommand); break;
                    case 537: t = () => typeof(RoutedEvent); break;
                    case 538: t = () => typeof(RoutedEventConverter); break;
                    case 539: t = () => typeof(RoutedUICommand); break;
                    case 540: t = () => typeof(RoutingStrategy); break;
                    case 541: t = () => typeof(RowDefinition); break;
                    case 542: t = () => typeof(Run); break;
                    case 543: t = () => typeof(RuntimeNamePropertyAttribute); break;
                    case 544: t = () => typeof(SByte); break;
                    case 545: t = () => typeof(SByteConverter); break;
                    case 546: t = () => typeof(ScaleTransform); break;
                    case 547: t = () => typeof(ScaleTransform3D); break;
                    case 548: t = () => typeof(ScrollBar); break;
                    case 549: t = () => typeof(ScrollContentPresenter); break;
                    case 550: t = () => typeof(ScrollViewer); break;
                    case 551: t = () => typeof(Section); break;
                    case 552: t = () => typeof(SeekStoryboard); break;
                    case 553: t = () => typeof(Selector); break;
                    case 554: t = () => typeof(Separator); break;
                    case 555: t = () => typeof(SetStoryboardSpeedRatio); break;
                    case 556: t = () => typeof(Setter); break;
                    case 557: t = () => typeof(SetterBase); break;
                    case 558: t = () => typeof(Shape); break;
                    case 559: t = () => typeof(Single); break;
                    case 560: t = () => typeof(SingleAnimation); break;
                    case 561: t = () => typeof(SingleAnimationBase); break;
                    case 562: t = () => typeof(SingleAnimationUsingKeyFrames); break;
                    case 563: t = () => typeof(SingleConverter); break;
                    case 564: t = () => typeof(SingleKeyFrame); break;
                    case 565: t = () => typeof(SingleKeyFrameCollection); break;
                    case 566: t = () => typeof(Size); break;
                    case 567: t = () => typeof(Size3D); break;
                    case 568: t = () => typeof(Size3DConverter); break;
                    case 569: t = () => typeof(SizeAnimation); break;
                    case 570: t = () => typeof(SizeAnimationBase); break;
                    case 571: t = () => typeof(SizeAnimationUsingKeyFrames); break;
                    case 572: t = () => typeof(SizeConverter); break;
                    case 573: t = () => typeof(SizeKeyFrame); break;
                    case 574: t = () => typeof(SizeKeyFrameCollection); break;
                    case 575: t = () => typeof(SkewTransform); break;
                    case 576: t = () => typeof(SkipStoryboardToFill); break;
                    case 577: t = () => typeof(Slider); break;
                    case 578: t = () => typeof(SolidColorBrush); break;
                    case 579: t = () => typeof(SoundPlayerAction); break;
                    case 580: t = () => typeof(Span); break;
                    case 581: t = () => typeof(SpecularMaterial); break;
                    case 582: t = () => typeof(SpellCheck); break;
                    case 583: t = () => typeof(SplineByteKeyFrame); break;
                    case 584: t = () => typeof(SplineColorKeyFrame); break;
                    case 585: t = () => typeof(SplineDecimalKeyFrame); break;
                    case 586: t = () => typeof(SplineDoubleKeyFrame); break;
                    case 587: t = () => typeof(SplineInt16KeyFrame); break;
                    case 588: t = () => typeof(SplineInt32KeyFrame); break;
                    case 589: t = () => typeof(SplineInt64KeyFrame); break;
                    case 590: t = () => typeof(SplinePoint3DKeyFrame); break;
                    case 591: t = () => typeof(SplinePointKeyFrame); break;
                    case 592: t = () => typeof(SplineQuaternionKeyFrame); break;
                    case 593: t = () => typeof(SplineRectKeyFrame); break;
                    case 594: t = () => typeof(SplineRotation3DKeyFrame); break;
                    case 595: t = () => typeof(SplineSingleKeyFrame); break;
                    case 596: t = () => typeof(SplineSizeKeyFrame); break;
                    case 597: t = () => typeof(SplineThicknessKeyFrame); break;
                    case 598: t = () => typeof(SplineVector3DKeyFrame); break;
                    case 599: t = () => typeof(SplineVectorKeyFrame); break;
                    case 600: t = () => typeof(SpotLight); break;
                    case 601: t = () => typeof(StackPanel); break;
                    case 602: t = () => typeof(StaticExtension); break;
                    case 603: t = () => typeof(StaticResourceExtension); break;
                    case 604: t = () => typeof(StatusBar); break;
                    case 605: t = () => typeof(StatusBarItem); break;
                    case 606: t = () => typeof(StickyNoteControl); break;
                    case 607: t = () => typeof(StopStoryboard); break;
                    case 608: t = () => typeof(Storyboard); break;
                    case 609: t = () => typeof(StreamGeometry); break;
                    case 610: t = () => typeof(StreamGeometryContext); break;
                    case 611: t = () => typeof(StreamResourceInfo); break;
                    case 612: t = () => typeof(String); break;
                    case 613: t = () => typeof(StringAnimationBase); break;
                    case 614: t = () => typeof(StringAnimationUsingKeyFrames); break;
                    case 615: t = () => typeof(StringConverter); break;
                    case 616: t = () => typeof(StringKeyFrame); break;
                    case 617: t = () => typeof(StringKeyFrameCollection); break;
                    case 618: t = () => typeof(StrokeCollection); break;
                    case 619: t = () => typeof(StrokeCollectionConverter); break;
                    case 620: t = () => typeof(Style); break;
                    case 621: t = () => typeof(Stylus); break;
                    case 622: t = () => typeof(StylusDevice); break;
                    case 623: t = () => typeof(TabControl); break;
                    case 624: t = () => typeof(TabItem); break;
                    case 625: t = () => typeof(TabPanel); break;
                    case 626: t = () => typeof(Table); break;
                    case 627: t = () => typeof(TableCell); break;
                    case 628: t = () => typeof(TableColumn); break;
                    case 629: t = () => typeof(TableRow); break;
                    case 630: t = () => typeof(TableRowGroup); break;
                    case 631: t = () => typeof(TabletDevice); break;
                    case 632: t = () => typeof(TemplateBindingExpression); break;
                    case 633: t = () => typeof(TemplateBindingExpressionConverter); break;
                    case 634: t = () => typeof(TemplateBindingExtension); break;
                    case 635: t = () => typeof(TemplateBindingExtensionConverter); break;
                    case 636: t = () => typeof(TemplateKey); break;
                    case 637: t = () => typeof(TemplateKeyConverter); break;
                    case 638: t = () => typeof(TextBlock); break;
                    case 639: t = () => typeof(TextBox); break;
                    case 640: t = () => typeof(TextBoxBase); break;
                    case 641: t = () => typeof(TextComposition); break;
                    case 642: t = () => typeof(TextCompositionManager); break;
                    case 643: t = () => typeof(TextDecoration); break;
                    case 644: t = () => typeof(TextDecorationCollection); break;
                    case 645: t = () => typeof(TextDecorationCollectionConverter); break;
                    case 646: t = () => typeof(TextEffect); break;
                    case 647: t = () => typeof(TextEffectCollection); break;
                    case 648: t = () => typeof(TextElement); break;
                    case 649: t = () => typeof(TextSearch); break;
                    case 650: t = () => typeof(ThemeDictionaryExtension); break;
                    case 651: t = () => typeof(Thickness); break;
                    case 652: t = () => typeof(ThicknessAnimation); break;
                    case 653: t = () => typeof(ThicknessAnimationBase); break;
                    case 654: t = () => typeof(ThicknessAnimationUsingKeyFrames); break;
                    case 655: t = () => typeof(ThicknessConverter); break;
                    case 656: t = () => typeof(ThicknessKeyFrame); break;
                    case 657: t = () => typeof(ThicknessKeyFrameCollection); break;
                    case 658: t = () => typeof(Thumb); break;
                    case 659: t = () => typeof(TickBar); break;
                    case 660: t = () => typeof(TiffBitmapDecoder); break;
                    case 661: t = () => typeof(TiffBitmapEncoder); break;
                    case 662: t = () => typeof(TileBrush); break;
                    case 663: t = () => typeof(TimeSpan); break;
                    case 664: t = () => typeof(TimeSpanConverter); break;
                    case 665: t = () => typeof(Timeline); break;
                    case 666: t = () => typeof(TimelineCollection); break;
                    case 667: t = () => typeof(TimelineGroup); break;
                    case 668: t = () => typeof(ToggleButton); break;
                    case 669: t = () => typeof(ToolBar); break;
                    case 670: t = () => typeof(ToolBarOverflowPanel); break;
                    case 671: t = () => typeof(ToolBarPanel); break;
                    case 672: t = () => typeof(ToolBarTray); break;
                    case 673: t = () => typeof(ToolTip); break;
                    case 674: t = () => typeof(ToolTipService); break;
                    case 675: t = () => typeof(Track); break;
                    case 676: t = () => typeof(Transform); break;
                    case 677: t = () => typeof(Transform3D); break;
                    case 678: t = () => typeof(Transform3DCollection); break;
                    case 679: t = () => typeof(Transform3DGroup); break;
                    case 680: t = () => typeof(TransformCollection); break;
                    case 681: t = () => typeof(TransformConverter); break;
                    case 682: t = () => typeof(TransformGroup); break;
                    case 683: t = () => typeof(TransformedBitmap); break;
                    case 684: t = () => typeof(TranslateTransform); break;
                    case 685: t = () => typeof(TranslateTransform3D); break;
                    case 686: t = () => typeof(TreeView); break;
                    case 687: t = () => typeof(TreeViewItem); break;
                    case 688: t = () => typeof(Trigger); break;
                    case 689: t = () => typeof(TriggerAction); break;
                    case 690: t = () => typeof(TriggerBase); break;
                    case 691: t = () => typeof(TypeExtension); break;
                    case 692: t = () => typeof(TypeTypeConverter); break;
                    case 693: t = () => typeof(Typography); break;
                    case 694: t = () => typeof(UIElement); break;
                    case 695: t = () => typeof(UInt16); break;
                    case 696: t = () => typeof(UInt16Converter); break;
                    case 697: t = () => typeof(UInt32); break;
                    case 698: t = () => typeof(UInt32Converter); break;
                    case 699: t = () => typeof(UInt64); break;
                    case 700: t = () => typeof(UInt64Converter); break;
                    case 701: t = () => typeof(UShortIListConverter); break;
                    case 702: t = () => typeof(Underline); break;
                    case 703: t = () => typeof(UniformGrid); break;
                    case 704: t = () => typeof(Uri); break;
                    case 705: t = () => typeof(UriTypeConverter); break;
                    case 706: t = () => typeof(UserControl); break;
                    case 707: t = () => typeof(Validation); break;
                    case 708: t = () => typeof(Vector); break;
                    case 709: t = () => typeof(Vector3D); break;
                    case 710: t = () => typeof(Vector3DAnimation); break;
                    case 711: t = () => typeof(Vector3DAnimationBase); break;
                    case 712: t = () => typeof(Vector3DAnimationUsingKeyFrames); break;
                    case 713: t = () => typeof(Vector3DCollection); break;
                    case 714: t = () => typeof(Vector3DCollectionConverter); break;
                    case 715: t = () => typeof(Vector3DConverter); break;
                    case 716: t = () => typeof(Vector3DKeyFrame); break;
                    case 717: t = () => typeof(Vector3DKeyFrameCollection); break;
                    case 718: t = () => typeof(VectorAnimation); break;
                    case 719: t = () => typeof(VectorAnimationBase); break;
                    case 720: t = () => typeof(VectorAnimationUsingKeyFrames); break;
                    case 721: t = () => typeof(VectorCollection); break;
                    case 722: t = () => typeof(VectorCollectionConverter); break;
                    case 723: t = () => typeof(VectorConverter); break;
                    case 724: t = () => typeof(VectorKeyFrame); break;
                    case 725: t = () => typeof(VectorKeyFrameCollection); break;
                    case 726: t = () => typeof(VideoDrawing); break;
                    case 727: t = () => typeof(ViewBase); break;
                    case 728: t = () => typeof(Viewbox); break;
                    case 729: t = () => typeof(Viewport3D); break;
                    case 730: t = () => typeof(Viewport3DVisual); break;
                    case 731: t = () => typeof(VirtualizingPanel); break;
                    case 732: t = () => typeof(VirtualizingStackPanel); break;
                    case 733: t = () => typeof(Visual); break;
                    case 734: t = () => typeof(Visual3D); break;
                    case 735: t = () => typeof(VisualBrush); break;
                    case 736: t = () => typeof(VisualTarget); break;
                    case 737: t = () => typeof(WeakEventManager); break;
                    case 738: t = () => typeof(WhitespaceSignificantCollectionAttribute); break;
                    case 739: t = () => typeof(Window); break;
                    case 740: t = () => typeof(WmpBitmapDecoder); break;
                    case 741: t = () => typeof(WmpBitmapEncoder); break;
                    case 742: t = () => typeof(WrapPanel); break;
                    case 743: t = () => typeof(WriteableBitmap); break;
                    case 744: t = () => typeof(XamlBrushSerializer); break;
                    case 745: t = () => typeof(XamlInt32CollectionSerializer); break;
                    case 746: t = () => typeof(XamlPathDataSerializer); break;
                    case 747: t = () => typeof(XamlPoint3DCollectionSerializer); break;
                    case 748: t = () => typeof(XamlPointCollectionSerializer); break;
                    case 749: t = () => typeof(System.Windows.Markup.XamlReader); break; // ambiguous
                    case 750: t = () => typeof(XamlStyleSerializer); break;
                    case 751: t = () => typeof(XamlTemplateSerializer); break;
                    case 752: t = () => typeof(XamlVector3DCollectionSerializer); break;
                    case 753: t = () => typeof(System.Windows.Markup.XamlWriter); break; // ambiguous
                    case 754: t = () => typeof(XmlDataProvider); break;
                    case 755: t = () => typeof(XmlLangPropertyAttribute); break;
                    case 756: t = () => typeof(XmlLanguage); break;
                    case 757: t = () => typeof(XmlLanguageConverter); break;
                    case 758: t = () => typeof(XmlNamespaceMapping); break;
                    case 759: t = () => typeof(ZoomPercentageConverter); break;
                    default: t = () => null; break;
                }

                return t();
            }

            // Initialize known object types
            internal static TypeConverter CreateKnownTypeConverter(Int16 converterId)
            {
                TypeConverter o = null;
                switch (converterId)
                {
                    case -42: o = new System.Windows.Media.Converters.BoolIListConverter(); break;
                    case -46: o = new System.ComponentModel.BooleanConverter(); break;
                    case -53: o = new System.Windows.Media.BrushConverter(); break;
                    case -61: o = new System.ComponentModel.ByteConverter(); break;
                    case -70: o = new System.ComponentModel.CharConverter(); break;
                    case -71: o = new System.Windows.Media.Converters.CharIListConverter(); break;
                    case -87: o = new System.Windows.Media.ColorConverter(); break;
                    case -94: o = new System.Windows.Input.CommandConverter(); break;
                    case -96: o = new System.Windows.Markup.ComponentResourceKeyConverter(); break;
                    case -111: o = new System.Windows.CornerRadiusConverter(); break;
                    case -114: o = new System.ComponentModel.CultureInfoConverter(); break;
                    case -115: o = new System.Windows.CultureInfoIetfLanguageTagConverter(); break;
                    case -117: o = new System.Windows.Input.CursorConverter(); break;
                    case -124: o = new System.ComponentModel.DateTimeConverter(); break;
                    case -125: o = new System.Windows.Markup.DateTimeConverter2(); break;
                    case -130: o = new System.ComponentModel.DecimalConverter(); break;
                    case -137: o = new System.Windows.Markup.DependencyPropertyConverter(); break;
                    case -138: o = new System.Windows.DialogResultConverter(); break;
                    case -174: o = new System.Windows.Media.DoubleCollectionConverter(); break;
                    case -175: o = new System.ComponentModel.DoubleConverter(); break;
                    case -176: o = new System.Windows.Media.Converters.DoubleIListConverter(); break;
                    case -188: o = new System.Windows.DurationConverter(); break;
                    case -190: o = new System.Windows.DynamicResourceExtensionConverter(); break;
                    case -201: o = new System.Windows.ExpressionConverter(); break;
                    case -204: o = new System.Windows.FigureLengthConverter(); break;
                    case -215: o = new System.Windows.Media.FontFamilyConverter(); break;
                    case -216: o = new System.Windows.FontSizeConverter(); break;
                    case -218: o = new System.Windows.FontStretchConverter(); break;
                    case -220: o = new System.Windows.FontStyleConverter(); break;
                    case -222: o = new System.Windows.FontWeightConverter(); break;
                    case -240: o = new System.Windows.Media.GeometryConverter(); break;
                    case -256: o = new System.Windows.GridLengthConverter(); break;
                    case -267: o = new System.ComponentModel.GuidConverter(); break;
                    case -286: o = new System.Windows.Media.ImageSourceConverter(); break;
                    case -299: o = new System.Windows.Input.InputScopeConverter(); break;
                    case -301: o = new System.Windows.Input.InputScopeNameConverter(); break;
                    case -306: o = new System.ComponentModel.Int16Converter(); break;
                    case -314: o = new System.Windows.Media.Int32CollectionConverter(); break;
                    case -315: o = new System.ComponentModel.Int32Converter(); break;
                    case -319: o = new System.Windows.Int32RectConverter(); break;
                    case -324: o = new System.ComponentModel.Int64Converter(); break;
                    case -338: o = new System.Windows.Input.KeyConverter(); break;
                    case -340: o = new System.Windows.Input.KeyGestureConverter(); break;
                    case -342: o = new System.Windows.KeySplineConverter(); break;
                    case -344: o = new System.Windows.KeyTimeConverter(); break;
                    case -348: o = new System.Windows.LengthConverter(); break;
                    case -387: o = new System.Windows.Media.Media3D.Matrix3DConverter(); break;
                    case -392: o = new System.Windows.Media.MatrixConverter(); break;
                    case -410: o = new System.Windows.Input.ModifierKeysConverter(); break;
                    case -411: o = new System.Windows.Input.MouseActionConverter(); break;
                    case -415: o = new System.Windows.Input.MouseGestureConverter(); break;
                    case -423: o = new System.Windows.NullableBoolConverter(); break;
                    case -445: o = new System.Windows.Media.PathFigureCollectionConverter(); break;
                    case -453: o = new System.Windows.Media.PixelFormatConverter(); break;
                    case -462: o = new System.Windows.Media.Media3D.Point3DCollectionConverter(); break;
                    case -463: o = new System.Windows.Media.Media3D.Point3DConverter(); break;
                    case -467: o = new System.Windows.Media.Media3D.Point4DConverter(); break;
                    case -473: o = new System.Windows.Media.PointCollectionConverter(); break;
                    case -474: o = new System.Windows.PointConverter(); break;
                    case -475: o = new System.Windows.Media.Converters.PointIListConverter(); break;
                    case -492: o = new System.Windows.PropertyPathConverter(); break;
                    case -498: o = new System.Windows.Media.Media3D.QuaternionConverter(); break;
                    case -507: o = new System.Windows.Media.Media3D.Rect3DConverter(); break;
                    case -511: o = new System.Windows.RectConverter(); break;
                    case -521: o = new System.Windows.Media.Animation.RepeatBehaviorConverter(); break;
                    case -538: o = new System.Windows.Markup.RoutedEventConverter(); break;
                    case -545: o = new System.ComponentModel.SByteConverter(); break;
                    case -563: o = new System.ComponentModel.SingleConverter(); break;
                    case -568: o = new System.Windows.Media.Media3D.Size3DConverter(); break;
                    case -572: o = new System.Windows.SizeConverter(); break;
                    case -615: o = new System.ComponentModel.StringConverter(); break;
                    case -619: o = new System.Windows.StrokeCollectionConverter(); break;
                    case -633: o = new System.Windows.TemplateBindingExpressionConverter(); break;
                    case -635: o = new System.Windows.TemplateBindingExtensionConverter(); break;
                    case -637: o = new System.Windows.Markup.TemplateKeyConverter(); break;
                    case -645: o = new System.Windows.TextDecorationCollectionConverter(); break;
                    case -655: o = new System.Windows.ThicknessConverter(); break;
                    case -664: o = new System.ComponentModel.TimeSpanConverter(); break;
                    case -681: o = new System.Windows.Media.TransformConverter(); break;
                    case -692: o = new System.Windows.Markup.TypeTypeConverter(); break;
                    case -696: o = new System.ComponentModel.UInt16Converter(); break;
                    case -698: o = new System.ComponentModel.UInt32Converter(); break;
                    case -700: o = new System.ComponentModel.UInt64Converter(); break;
                    case -701: o = new System.Windows.Media.Converters.UShortIListConverter(); break;
                    case -705: o = new System.UriTypeConverter(); break;
                    case -714: o = new System.Windows.Media.Media3D.Vector3DCollectionConverter(); break;
                    case -715: o = new System.Windows.Media.Media3D.Vector3DConverter(); break;
                    case -722: o = new System.Windows.Media.VectorCollectionConverter(); break;
                    case -723: o = new System.Windows.VectorConverter(); break;
                    case -757: o = new System.Windows.Markup.XmlLanguageConverter(); break;
                }
                return o;
            }
            

            public static bool GetKnownProperty(Int16 propertyId, out Int16 typeId, out string propertyName)
            {
                switch (propertyId)
                {
                    case -1: //AccessText.Text
                        typeId = -1;
                        propertyName = "Text";
                        break;
                    case -2: //BeginStoryboard.Storyboard
                        typeId = -17;
                        propertyName = "Storyboard";
                        break;
                    case -3: //BitmapEffectGroup.Children
                        typeId = -28;
                        propertyName = "Children";
                        break;
                    case -4: //Border.Background
                        typeId = -50;
                        propertyName = "Background";
                        break;
                    case -5: //Border.BorderBrush
                        typeId = -50;
                        propertyName = "BorderBrush";
                        break;
                    case -6: //Border.BorderThickness
                        typeId = -50;
                        propertyName = "BorderThickness";
                        break;
                    case -7: //ButtonBase.Command
                        typeId = -56;
                        propertyName = "Command";
                        break;
                    case -8: //ButtonBase.CommandParameter
                        typeId = -56;
                        propertyName = "CommandParameter";
                        break;
                    case -9: //ButtonBase.CommandTarget
                        typeId = -56;
                        propertyName = "CommandTarget";
                        break;
                    case -10: //ButtonBase.IsPressed
                        typeId = -56;
                        propertyName = "IsPressed";
                        break;
                    case -11: //ColumnDefinition.MaxWidth
                        typeId = -90;
                        propertyName = "MaxWidth";
                        break;
                    case -12: //ColumnDefinition.MinWidth
                        typeId = -90;
                        propertyName = "MinWidth";
                        break;
                    case -13: //ColumnDefinition.Width
                        typeId = -90;
                        propertyName = "Width";
                        break;
                    case -14: //ContentControl.Content
                        typeId = -100;
                        propertyName = "Content";
                        break;
                    case -15: //ContentControl.ContentTemplate
                        typeId = -100;
                        propertyName = "ContentTemplate";
                        break;
                    case -16: //ContentControl.ContentTemplateSelector
                        typeId = -100;
                        propertyName = "ContentTemplateSelector";
                        break;
                    case -17: //ContentControl.HasContent
                        typeId = -100;
                        propertyName = "HasContent";
                        break;
                    case -18: //ContentElement.Focusable
                        typeId = -101;
                        propertyName = "Focusable";
                        break;
                    case -19: //ContentPresenter.Content
                        typeId = -102;
                        propertyName = "Content";
                        break;
                    case -20: //ContentPresenter.ContentSource
                        typeId = -102;
                        propertyName = "ContentSource";
                        break;
                    case -21: //ContentPresenter.ContentTemplate
                        typeId = -102;
                        propertyName = "ContentTemplate";
                        break;
                    case -22: //ContentPresenter.ContentTemplateSelector
                        typeId = -102;
                        propertyName = "ContentTemplateSelector";
                        break;
                    case -23: //ContentPresenter.RecognizesAccessKey
                        typeId = -102;
                        propertyName = "RecognizesAccessKey";
                        break;
                    case -24: //Control.Background
                        typeId = -107;
                        propertyName = "Background";
                        break;
                    case -25: //Control.BorderBrush
                        typeId = -107;
                        propertyName = "BorderBrush";
                        break;
                    case -26: //Control.BorderThickness
                        typeId = -107;
                        propertyName = "BorderThickness";
                        break;
                    case -27: //Control.FontFamily
                        typeId = -107;
                        propertyName = "FontFamily";
                        break;
                    case -28: //Control.FontSize
                        typeId = -107;
                        propertyName = "FontSize";
                        break;
                    case -29: //Control.FontStretch
                        typeId = -107;
                        propertyName = "FontStretch";
                        break;
                    case -30: //Control.FontStyle
                        typeId = -107;
                        propertyName = "FontStyle";
                        break;
                    case -31: //Control.FontWeight
                        typeId = -107;
                        propertyName = "FontWeight";
                        break;
                    case -32: //Control.Foreground
                        typeId = -107;
                        propertyName = "Foreground";
                        break;
                    case -33: //Control.HorizontalContentAlignment
                        typeId = -107;
                        propertyName = "HorizontalContentAlignment";
                        break;
                    case -34: //Control.IsTabStop
                        typeId = -107;
                        propertyName = "IsTabStop";
                        break;
                    case -35: //Control.Padding
                        typeId = -107;
                        propertyName = "Padding";
                        break;
                    case -36: //Control.TabIndex
                        typeId = -107;
                        propertyName = "TabIndex";
                        break;
                    case -37: //Control.Template
                        typeId = -107;
                        propertyName = "Template";
                        break;
                    case -38: //Control.VerticalContentAlignment
                        typeId = -107;
                        propertyName = "VerticalContentAlignment";
                        break;
                    case -39: //DockPanel.Dock
                        typeId = -163;
                        propertyName = "Dock";
                        break;
                    case -40: //DockPanel.LastChildFill
                        typeId = -163;
                        propertyName = "LastChildFill";
                        break;
                    case -41: //DocumentViewerBase.Document
                        typeId = -167;
                        propertyName = "Document";
                        break;
                    case -42: //DrawingGroup.Children
                        typeId = -183;
                        propertyName = "Children";
                        break;
                    case -43: //FlowDocumentReader.Document
                        typeId = -211;
                        propertyName = "Document";
                        break;
                    case -44: //FlowDocumentScrollViewer.Document
                        typeId = -212;
                        propertyName = "Document";
                        break;
                    case -45: //FrameworkContentElement.Style
                        typeId = -225;
                        propertyName = "Style";
                        break;
                    case -46: //FrameworkElement.FlowDirection
                        typeId = -226;
                        propertyName = "FlowDirection";
                        break;
                    case -47: //FrameworkElement.Height
                        typeId = -226;
                        propertyName = "Height";
                        break;
                    case -48: //FrameworkElement.HorizontalAlignment
                        typeId = -226;
                        propertyName = "HorizontalAlignment";
                        break;
                    case -49: //FrameworkElement.Margin
                        typeId = -226;
                        propertyName = "Margin";
                        break;
                    case -50: //FrameworkElement.MaxHeight
                        typeId = -226;
                        propertyName = "MaxHeight";
                        break;
                    case -51: //FrameworkElement.MaxWidth
                        typeId = -226;
                        propertyName = "MaxWidth";
                        break;
                    case -52: //FrameworkElement.MinHeight
                        typeId = -226;
                        propertyName = "MinHeight";
                        break;
                    case -53: //FrameworkElement.MinWidth
                        typeId = -226;
                        propertyName = "MinWidth";
                        break;
                    case -54: //FrameworkElement.Name
                        typeId = -226;
                        propertyName = "Name";
                        break;
                    case -55: //FrameworkElement.Style
                        typeId = -226;
                        propertyName = "Style";
                        break;
                    case -56: //FrameworkElement.VerticalAlignment
                        typeId = -226;
                        propertyName = "VerticalAlignment";
                        break;
                    case -57: //FrameworkElement.Width
                        typeId = -226;
                        propertyName = "Width";
                        break;
                    case -58: //GeneralTransformGroup.Children
                        typeId = -236;
                        propertyName = "Children";
                        break;
                    case -59: //GeometryGroup.Children
                        typeId = -242;
                        propertyName = "Children";
                        break;
                    case -60: //GradientBrush.GradientStops
                        typeId = -251;
                        propertyName = "GradientStops";
                        break;
                    case -61: //Grid.Column
                        typeId = -254;
                        propertyName = "Column";
                        break;
                    case -62: //Grid.ColumnSpan
                        typeId = -254;
                        propertyName = "ColumnSpan";
                        break;
                    case -63: //Grid.Row
                        typeId = -254;
                        propertyName = "Row";
                        break;
                    case -64: //Grid.RowSpan
                        typeId = -254;
                        propertyName = "RowSpan";
                        break;
                    case -65: //GridViewColumn.Header
                        typeId = -259;
                        propertyName = "Header";
                        break;
                    case -66: //HeaderedContentControl.HasHeader
                        typeId = -269;
                        propertyName = "HasHeader";
                        break;
                    case -67: //HeaderedContentControl.Header
                        typeId = -269;
                        propertyName = "Header";
                        break;
                    case -68: //HeaderedContentControl.HeaderTemplate
                        typeId = -269;
                        propertyName = "HeaderTemplate";
                        break;
                    case -69: //HeaderedContentControl.HeaderTemplateSelector
                        typeId = -269;
                        propertyName = "HeaderTemplateSelector";
                        break;
                    case -70: //HeaderedItemsControl.HasHeader
                        typeId = -270;
                        propertyName = "HasHeader";
                        break;
                    case -71: //HeaderedItemsControl.Header
                        typeId = -270;
                        propertyName = "Header";
                        break;
                    case -72: //HeaderedItemsControl.HeaderTemplate
                        typeId = -270;
                        propertyName = "HeaderTemplate";
                        break;
                    case -73: //HeaderedItemsControl.HeaderTemplateSelector
                        typeId = -270;
                        propertyName = "HeaderTemplateSelector";
                        break;
                    case -74: //Hyperlink.NavigateUri
                        typeId = -273;
                        propertyName = "NavigateUri";
                        break;
                    case -75: //Image.Source
                        typeId = -281;
                        propertyName = "Source";
                        break;
                    case -76: //Image.Stretch
                        typeId = -281;
                        propertyName = "Stretch";
                        break;
                    case -77: //ItemsControl.ItemContainerStyle
                        typeId = -329;
                        propertyName = "ItemContainerStyle";
                        break;
                    case -78: //ItemsControl.ItemContainerStyleSelector
                        typeId = -329;
                        propertyName = "ItemContainerStyleSelector";
                        break;
                    case -79: //ItemsControl.ItemTemplate
                        typeId = -329;
                        propertyName = "ItemTemplate";
                        break;
                    case -80: //ItemsControl.ItemTemplateSelector
                        typeId = -329;
                        propertyName = "ItemTemplateSelector";
                        break;
                    case -81: //ItemsControl.ItemsPanel
                        typeId = -329;
                        propertyName = "ItemsPanel";
                        break;
                    case -82: //ItemsControl.ItemsSource
                        typeId = -329;
                        propertyName = "ItemsSource";
                        break;
                    case -83: //MaterialGroup.Children
                        typeId = -384;
                        propertyName = "Children";
                        break;
                    case -84: //Model3DGroup.Children
                        typeId = -408;
                        propertyName = "Children";
                        break;
                    case -85: //Page.Content
                        typeId = -434;
                        propertyName = "Content";
                        break;
                    case -86: //Panel.Background
                        typeId = -437;
                        propertyName = "Background";
                        break;
                    case -87: //Path.Data
                        typeId = -442;
                        propertyName = "Data";
                        break;
                    case -88: //PathFigure.Segments
                        typeId = -443;
                        propertyName = "Segments";
                        break;
                    case -89: //PathGeometry.Figures
                        typeId = -446;
                        propertyName = "Figures";
                        break;
                    case -90: //Popup.Child
                        typeId = -485;
                        propertyName = "Child";
                        break;
                    case -91: //Popup.IsOpen
                        typeId = -485;
                        propertyName = "IsOpen";
                        break;
                    case -92: //Popup.Placement
                        typeId = -485;
                        propertyName = "Placement";
                        break;
                    case -93: //Popup.PopupAnimation
                        typeId = -485;
                        propertyName = "PopupAnimation";
                        break;
                    case -94: //RowDefinition.Height
                        typeId = -541;
                        propertyName = "Height";
                        break;
                    case -95: //RowDefinition.MaxHeight
                        typeId = -541;
                        propertyName = "MaxHeight";
                        break;
                    case -96: //RowDefinition.MinHeight
                        typeId = -541;
                        propertyName = "MinHeight";
                        break;
                    case -97: //ScrollViewer.CanContentScroll
                        typeId = -550;
                        propertyName = "CanContentScroll";
                        break;
                    case -98: //ScrollViewer.HorizontalScrollBarVisibility
                        typeId = -550;
                        propertyName = "HorizontalScrollBarVisibility";
                        break;
                    case -99: //ScrollViewer.VerticalScrollBarVisibility
                        typeId = -550;
                        propertyName = "VerticalScrollBarVisibility";
                        break;
                    case -100: //Shape.Fill
                        typeId = -558;
                        propertyName = "Fill";
                        break;
                    case -101: //Shape.Stroke
                        typeId = -558;
                        propertyName = "Stroke";
                        break;
                    case -102: //Shape.StrokeThickness
                        typeId = -558;
                        propertyName = "StrokeThickness";
                        break;
                    case -103: //TextBlock.Background
                        typeId = -638;
                        propertyName = "Background";
                        break;
                    case -104: //TextBlock.FontFamily
                        typeId = -638;
                        propertyName = "FontFamily";
                        break;
                    case -105: //TextBlock.FontSize
                        typeId = -638;
                        propertyName = "FontSize";
                        break;
                    case -106: //TextBlock.FontStretch
                        typeId = -638;
                        propertyName = "FontStretch";
                        break;
                    case -107: //TextBlock.FontStyle
                        typeId = -638;
                        propertyName = "FontStyle";
                        break;
                    case -108: //TextBlock.FontWeight
                        typeId = -638;
                        propertyName = "FontWeight";
                        break;
                    case -109: //TextBlock.Foreground
                        typeId = -638;
                        propertyName = "Foreground";
                        break;
                    case -110: //TextBlock.Text
                        typeId = -638;
                        propertyName = "Text";
                        break;
                    case -111: //TextBlock.TextDecorations
                        typeId = -638;
                        propertyName = "TextDecorations";
                        break;
                    case -112: //TextBlock.TextTrimming
                        typeId = -638;
                        propertyName = "TextTrimming";
                        break;
                    case -113: //TextBlock.TextWrapping
                        typeId = -638;
                        propertyName = "TextWrapping";
                        break;
                    case -114: //TextBox.Text
                        typeId = -639;
                        propertyName = "Text";
                        break;
                    case -115: //TextElement.Background
                        typeId = -648;
                        propertyName = "Background";
                        break;
                    case -116: //TextElement.FontFamily
                        typeId = -648;
                        propertyName = "FontFamily";
                        break;
                    case -117: //TextElement.FontSize
                        typeId = -648;
                        propertyName = "FontSize";
                        break;
                    case -118: //TextElement.FontStretch
                        typeId = -648;
                        propertyName = "FontStretch";
                        break;
                    case -119: //TextElement.FontStyle
                        typeId = -648;
                        propertyName = "FontStyle";
                        break;
                    case -120: //TextElement.FontWeight
                        typeId = -648;
                        propertyName = "FontWeight";
                        break;
                    case -121: //TextElement.Foreground
                        typeId = -648;
                        propertyName = "Foreground";
                        break;
                    case -122: //TimelineGroup.Children
                        typeId = -667;
                        propertyName = "Children";
                        break;
                    case -123: //Track.IsDirectionReversed
                        typeId = -675;
                        propertyName = "IsDirectionReversed";
                        break;
                    case -124: //Track.Maximum
                        typeId = -675;
                        propertyName = "Maximum";
                        break;
                    case -125: //Track.Minimum
                        typeId = -675;
                        propertyName = "Minimum";
                        break;
                    case -126: //Track.Orientation
                        typeId = -675;
                        propertyName = "Orientation";
                        break;
                    case -127: //Track.Value
                        typeId = -675;
                        propertyName = "Value";
                        break;
                    case -128: //Track.ViewportSize
                        typeId = -675;
                        propertyName = "ViewportSize";
                        break;
                    case -129: //Transform3DGroup.Children
                        typeId = -679;
                        propertyName = "Children";
                        break;
                    case -130: //TransformGroup.Children
                        typeId = -682;
                        propertyName = "Children";
                        break;
                    case -131: //UIElement.ClipToBounds
                        typeId = -694;
                        propertyName = "ClipToBounds";
                        break;
                    case -132: //UIElement.Focusable
                        typeId = -694;
                        propertyName = "Focusable";
                        break;
                    case -133: //UIElement.IsEnabled
                        typeId = -694;
                        propertyName = "IsEnabled";
                        break;
                    case -134: //UIElement.RenderTransform
                        typeId = -694;
                        propertyName = "RenderTransform";
                        break;
                    case -135: //UIElement.Visibility
                        typeId = -694;
                        propertyName = "Visibility";
                        break;
                    case -136: //Viewport3D.Children
                        typeId = -729;
                        propertyName = "Children";
                        break;
                    case -138: //AdornedElementPlaceholder.Child
                        typeId = -2;
                        propertyName = "Child";
                        break;
                    case -139: //AdornerDecorator.Child
                        typeId = -4;
                        propertyName = "Child";
                        break;
                    case -140: //AnchoredBlock.Blocks
                        typeId = -8;
                        propertyName = "Blocks";
                        break;
                    case -141: //ArrayExtension.Items
                        typeId = -14;
                        propertyName = "Items";
                        break;
                    case -142: //BlockUIContainer.Child
                        typeId = -37;
                        propertyName = "Child";
                        break;
                    case -143: //Bold.Inlines
                        typeId = -41;
                        propertyName = "Inlines";
                        break;
                    case -144: //BooleanAnimationUsingKeyFrames.KeyFrames
                        typeId = -45;
                        propertyName = "KeyFrames";
                        break;
                    case -145: //Border.Child
                        typeId = -50;
                        propertyName = "Child";
                        break;
                    case -146: //BulletDecorator.Child
                        typeId = -54;
                        propertyName = "Child";
                        break;
                    case -147: //Button.Content
                        typeId = -55;
                        propertyName = "Content";
                        break;
                    case -148: //ButtonBase.Content
                        typeId = -56;
                        propertyName = "Content";
                        break;
                    case -149: //ByteAnimationUsingKeyFrames.KeyFrames
                        typeId = -60;
                        propertyName = "KeyFrames";
                        break;
                    case -150: //Canvas.Children
                        typeId = -66;
                        propertyName = "Children";
                        break;
                    case -151: //CharAnimationUsingKeyFrames.KeyFrames
                        typeId = -69;
                        propertyName = "KeyFrames";
                        break;
                    case -152: //CheckBox.Content
                        typeId = -74;
                        propertyName = "Content";
                        break;
                    case -153: //ColorAnimationUsingKeyFrames.KeyFrames
                        typeId = -84;
                        propertyName = "KeyFrames";
                        break;
                    case -154: //ComboBox.Items
                        typeId = -92;
                        propertyName = "Items";
                        break;
                    case -155: //ComboBoxItem.Content
                        typeId = -93;
                        propertyName = "Content";
                        break;
                    case -156: //ContextMenu.Items
                        typeId = -105;
                        propertyName = "Items";
                        break;
                    case -157: //ControlTemplate.VisualTree
                        typeId = -108;
                        propertyName = "VisualTree";
                        break;
                    case -158: //DataTemplate.VisualTree
                        typeId = -120;
                        propertyName = "VisualTree";
                        break;
                    case -159: //DataTrigger.Setters
                        typeId = -122;
                        propertyName = "Setters";
                        break;
                    case -160: //DecimalAnimationUsingKeyFrames.KeyFrames
                        typeId = -129;
                        propertyName = "KeyFrames";
                        break;
                    case -161: //Decorator.Child
                        typeId = -133;
                        propertyName = "Child";
                        break;
                    case -162: //DockPanel.Children
                        typeId = -163;
                        propertyName = "Children";
                        break;
                    case -163: //DocumentViewer.Document
                        typeId = -166;
                        propertyName = "Document";
                        break;
                    case -164: //DoubleAnimationUsingKeyFrames.KeyFrames
                        typeId = -171;
                        propertyName = "KeyFrames";
                        break;
                    case -165: //EventTrigger.Actions
                        typeId = -198;
                        propertyName = "Actions";
                        break;
                    case -166: //Expander.Content
                        typeId = -199;
                        propertyName = "Content";
                        break;
                    case -167: //Figure.Blocks
                        typeId = -202;
                        propertyName = "Blocks";
                        break;
                    case -168: //FixedDocument.Pages
                        typeId = -205;
                        propertyName = "Pages";
                        break;
                    case -169: //FixedDocumentSequence.References
                        typeId = -206;
                        propertyName = "References";
                        break;
                    case -170: //FixedPage.Children
                        typeId = -207;
                        propertyName = "Children";
                        break;
                    case -171: //Floater.Blocks
                        typeId = -208;
                        propertyName = "Blocks";
                        break;
                    case -172: //FlowDocument.Blocks
                        typeId = -209;
                        propertyName = "Blocks";
                        break;
                    case -173: //FlowDocumentPageViewer.Document
                        typeId = -210;
                        propertyName = "Document";
                        break;
                    case -174: //FrameworkTemplate.VisualTree
                        typeId = -231;
                        propertyName = "VisualTree";
                        break;
                    case -175: //Grid.Children
                        typeId = -254;
                        propertyName = "Children";
                        break;
                    case -176: //GridView.Columns
                        typeId = -258;
                        propertyName = "Columns";
                        break;
                    case -177: //GridViewColumnHeader.Content
                        typeId = -260;
                        propertyName = "Content";
                        break;
                    case -178: //GroupBox.Content
                        typeId = -264;
                        propertyName = "Content";
                        break;
                    case -179: //GroupItem.Content
                        typeId = -265;
                        propertyName = "Content";
                        break;
                    case -180: //HeaderedContentControl.Content
                        typeId = -269;
                        propertyName = "Content";
                        break;
                    case -181: //HeaderedItemsControl.Items
                        typeId = -270;
                        propertyName = "Items";
                        break;
                    case -182: //HierarchicalDataTemplate.VisualTree
                        typeId = -271;
                        propertyName = "VisualTree";
                        break;
                    case -183: //Hyperlink.Inlines
                        typeId = -273;
                        propertyName = "Inlines";
                        break;
                    case -184: //InkCanvas.Children
                        typeId = -288;
                        propertyName = "Children";
                        break;
                    case -185: //InkPresenter.Child
                        typeId = -289;
                        propertyName = "Child";
                        break;
                    case -186: //InlineUIContainer.Child
                        typeId = -292;
                        propertyName = "Child";
                        break;
                    case -187: //InputScopeName.NameValue
                        typeId = -300;
                        propertyName = "NameValue";
                        break;
                    case -188: //Int16AnimationUsingKeyFrames.KeyFrames
                        typeId = -305;
                        propertyName = "KeyFrames";
                        break;
                    case -189: //Int32AnimationUsingKeyFrames.KeyFrames
                        typeId = -312;
                        propertyName = "KeyFrames";
                        break;
                    case -190: //Int64AnimationUsingKeyFrames.KeyFrames
                        typeId = -323;
                        propertyName = "KeyFrames";
                        break;
                    case -191: //Italic.Inlines
                        typeId = -327;
                        propertyName = "Inlines";
                        break;
                    case -192: //ItemsControl.Items
                        typeId = -329;
                        propertyName = "Items";
                        break;
                    case -193: //ItemsPanelTemplate.VisualTree
                        typeId = -330;
                        propertyName = "VisualTree";
                        break;
                    case -194: //Label.Content
                        typeId = -346;
                        propertyName = "Content";
                        break;
                    case -195: //LinearGradientBrush.GradientStops
                        typeId = -358;
                        propertyName = "GradientStops";
                        break;
                    case -196: //List.ListItems
                        typeId = -372;
                        propertyName = "ListItems";
                        break;
                    case -197: //ListBox.Items
                        typeId = -373;
                        propertyName = "Items";
                        break;
                    case -198: //ListBoxItem.Content
                        typeId = -374;
                        propertyName = "Content";
                        break;
                    case -199: //ListItem.Blocks
                        typeId = -376;
                        propertyName = "Blocks";
                        break;
                    case -200: //ListView.Items
                        typeId = -377;
                        propertyName = "Items";
                        break;
                    case -201: //ListViewItem.Content
                        typeId = -378;
                        propertyName = "Content";
                        break;
                    case -202: //MatrixAnimationUsingKeyFrames.KeyFrames
                        typeId = -389;
                        propertyName = "KeyFrames";
                        break;
                    case -203: //Menu.Items
                        typeId = -401;
                        propertyName = "Items";
                        break;
                    case -204: //MenuBase.Items
                        typeId = -402;
                        propertyName = "Items";
                        break;
                    case -205: //MenuItem.Items
                        typeId = -403;
                        propertyName = "Items";
                        break;
                    case -206: //ModelVisual3D.Children
                        typeId = -409;
                        propertyName = "Children";
                        break;
                    case -207: //MultiBinding.Bindings
                        typeId = -416;
                        propertyName = "Bindings";
                        break;
                    case -208: //MultiDataTrigger.Setters
                        typeId = -418;
                        propertyName = "Setters";
                        break;
                    case -209: //MultiTrigger.Setters
                        typeId = -419;
                        propertyName = "Setters";
                        break;
                    case -210: //ObjectAnimationUsingKeyFrames.KeyFrames
                        typeId = -428;
                        propertyName = "KeyFrames";
                        break;
                    case -211: //PageContent.Child
                        typeId = -435;
                        propertyName = "Child";
                        break;
                    case -212: //PageFunctionBase.Content
                        typeId = -436;
                        propertyName = "Content";
                        break;
                    case -213: //Panel.Children
                        typeId = -437;
                        propertyName = "Children";
                        break;
                    case -214: //Paragraph.Inlines
                        typeId = -438;
                        propertyName = "Inlines";
                        break;
                    case -215: //ParallelTimeline.Children
                        typeId = -439;
                        propertyName = "Children";
                        break;
                    case -216: //Point3DAnimationUsingKeyFrames.KeyFrames
                        typeId = -460;
                        propertyName = "KeyFrames";
                        break;
                    case -217: //PointAnimationUsingKeyFrames.KeyFrames
                        typeId = -470;
                        propertyName = "KeyFrames";
                        break;
                    case -218: //PriorityBinding.Bindings
                        typeId = -487;
                        propertyName = "Bindings";
                        break;
                    case -219: //QuaternionAnimationUsingKeyFrames.KeyFrames
                        typeId = -497;
                        propertyName = "KeyFrames";
                        break;
                    case -220: //RadialGradientBrush.GradientStops
                        typeId = -502;
                        propertyName = "GradientStops";
                        break;
                    case -221: //RadioButton.Content
                        typeId = -503;
                        propertyName = "Content";
                        break;
                    case -222: //RectAnimationUsingKeyFrames.KeyFrames
                        typeId = -510;
                        propertyName = "KeyFrames";
                        break;
                    case -223: //RepeatButton.Content
                        typeId = -522;
                        propertyName = "Content";
                        break;
                    case -224: //RichTextBox.Document
                        typeId = -527;
                        propertyName = "Document";
                        break;
                    case -225: //Rotation3DAnimationUsingKeyFrames.KeyFrames
                        typeId = -533;
                        propertyName = "KeyFrames";
                        break;
                    case -226: //Run.Text
                        typeId = -542;
                        propertyName = "Text";
                        break;
                    case -227: //ScrollViewer.Content
                        typeId = -550;
                        propertyName = "Content";
                        break;
                    case -228: //Section.Blocks
                        typeId = -551;
                        propertyName = "Blocks";
                        break;
                    case -229: //Selector.Items
                        typeId = -553;
                        propertyName = "Items";
                        break;
                    case -230: //SingleAnimationUsingKeyFrames.KeyFrames
                        typeId = -562;
                        propertyName = "KeyFrames";
                        break;
                    case -231: //SizeAnimationUsingKeyFrames.KeyFrames
                        typeId = -571;
                        propertyName = "KeyFrames";
                        break;
                    case -232: //Span.Inlines
                        typeId = -580;
                        propertyName = "Inlines";
                        break;
                    case -233: //StackPanel.Children
                        typeId = -601;
                        propertyName = "Children";
                        break;
                    case -234: //StatusBar.Items
                        typeId = -604;
                        propertyName = "Items";
                        break;
                    case -235: //StatusBarItem.Content
                        typeId = -605;
                        propertyName = "Content";
                        break;
                    case -236: //Storyboard.Children
                        typeId = -608;
                        propertyName = "Children";
                        break;
                    case -237: //StringAnimationUsingKeyFrames.KeyFrames
                        typeId = -614;
                        propertyName = "KeyFrames";
                        break;
                    case -238: //Style.Setters
                        typeId = -620;
                        propertyName = "Setters";
                        break;
                    case -239: //TabControl.Items
                        typeId = -623;
                        propertyName = "Items";
                        break;
                    case -240: //TabItem.Content
                        typeId = -624;
                        propertyName = "Content";
                        break;
                    case -241: //TabPanel.Children
                        typeId = -625;
                        propertyName = "Children";
                        break;
                    case -242: //Table.RowGroups
                        typeId = -626;
                        propertyName = "RowGroups";
                        break;
                    case -243: //TableCell.Blocks
                        typeId = -627;
                        propertyName = "Blocks";
                        break;
                    case -244: //TableRow.Cells
                        typeId = -629;
                        propertyName = "Cells";
                        break;
                    case -245: //TableRowGroup.Rows
                        typeId = -630;
                        propertyName = "Rows";
                        break;
                    case -246: //TextBlock.Inlines
                        typeId = -638;
                        propertyName = "Inlines";
                        break;
                    case -247: //ThicknessAnimationUsingKeyFrames.KeyFrames
                        typeId = -654;
                        propertyName = "KeyFrames";
                        break;
                    case -248: //ToggleButton.Content
                        typeId = -668;
                        propertyName = "Content";
                        break;
                    case -249: //ToolBar.Items
                        typeId = -669;
                        propertyName = "Items";
                        break;
                    case -250: //ToolBarOverflowPanel.Children
                        typeId = -670;
                        propertyName = "Children";
                        break;
                    case -251: //ToolBarPanel.Children
                        typeId = -671;
                        propertyName = "Children";
                        break;
                    case -252: //ToolBarTray.ToolBars
                        typeId = -672;
                        propertyName = "ToolBars";
                        break;
                    case -253: //ToolTip.Content
                        typeId = -673;
                        propertyName = "Content";
                        break;
                    case -254: //TreeView.Items
                        typeId = -686;
                        propertyName = "Items";
                        break;
                    case -255: //TreeViewItem.Items
                        typeId = -687;
                        propertyName = "Items";
                        break;
                    case -256: //Trigger.Setters
                        typeId = -688;
                        propertyName = "Setters";
                        break;
                    case -257: //Underline.Inlines
                        typeId = -702;
                        propertyName = "Inlines";
                        break;
                    case -258: //UniformGrid.Children
                        typeId = -703;
                        propertyName = "Children";
                        break;
                    case -259: //UserControl.Content
                        typeId = -706;
                        propertyName = "Content";
                        break;
                    case -260: //Vector3DAnimationUsingKeyFrames.KeyFrames
                        typeId = -712;
                        propertyName = "KeyFrames";
                        break;
                    case -261: //VectorAnimationUsingKeyFrames.KeyFrames
                        typeId = -720;
                        propertyName = "KeyFrames";
                        break;
                    case -262: //Viewbox.Child
                        typeId = -728;
                        propertyName = "Child";
                        break;
                    case -263: //Viewport3DVisual.Children
                        typeId = -730;
                        propertyName = "Children";
                        break;
                    case -264: //VirtualizingPanel.Children
                        typeId = -731;
                        propertyName = "Children";
                        break;
                    case -265: //VirtualizingStackPanel.Children
                        typeId = -732;
                        propertyName = "Children";
                        break;
                    case -266: //Window.Content
                        typeId = -739;
                        propertyName = "Content";
                        break;
                    case -267: //WrapPanel.Children
                        typeId = -742;
                        propertyName = "Children";
                        break;
                    case -268: //XmlDataProvider.XmlSerializer
                        typeId = -754;
                        propertyName = "XmlSerializer";
                        break;
                    default:
                        typeId = Int16.MinValue;
                        propertyName = null;
                        break;
                }
                return propertyName != null;
            }

            public static string GetKnownString(Int16 stringId)
            {
                string result;

                switch (stringId)
                {
                    case -1:
                        result = "Name";
                        break;
                    case -2:
                        result = "Uid";
                        break;
                    default:
                        result = null;
                        break;
                }

                return result;
            }

            public static Type GetTypeConverterForKnownProperty(Int16 propertyId)
            {
                Int16 declaringTypeId;
                string propertyName;
                if (!GetKnownProperty(propertyId, out declaringTypeId, out propertyName))
                {
                    return null;
                }
                KnownElements typeConverterType = System.Windows.Markup.KnownTypes.GetKnownTypeConverterIdForProperty(
                    (KnownElements)(-1 * declaringTypeId), propertyName);
                if (typeConverterType == KnownElements.UnknownElement)
                {
                    return null;
                }
                return GetKnownType((Int16)(-1 * (Int16)typeConverterType));
            }

            // returns null for some known properties which can be either attachable or not
            public static bool? IsKnownPropertyAttachable(Int16 propertyId)
            {
                if (propertyId >= 0 || propertyId < MinKnownProperty)
                {
                    return null;
                }
                switch (propertyId)
                {
                    case -39: //DockPanel.Dock
                        return true;
                    case -46: //FrameworkElement.FlowDirection
                        return null;
                    case -61: //Grid.Column
                        return true;
                    case -62: //Grid.ColumnSpan
                        return true;
                    case -63: //Grid.Row
                        return true;
                    case -64: //Grid.RowSpan
                        return true;
                    case -97: //ScrollViewer.CanContentScroll
                        return null;
                    case -98: //ScrollViewer.HorizontalScrollBarVisibility
                        return null;
                    case -99: //ScrollViewer.VerticalScrollBarVisibility
                        return null;
                    case -104: //TextBlock.FontFamily
                        return null;
                    case -105: //TextBlock.FontSize
                        return null;
                    case -106: //TextBlock.FontStretch
                        return null;
                    case -107: //TextBlock.FontStyle
                        return null;
                    case -108: //TextBlock.FontWeight
                        return null;
                    case -109: //TextBlock.Foreground
                        return null;
                    case -116: //TextElement.FontFamily
                        return null;
                    case -117: //TextElement.FontSize
                        return null;
                    case -118: //TextElement.FontStretch
                        return null;
                    case -119: //TextElement.FontStyle
                        return null;
                    case -120: //TextElement.FontWeight
                        return null;
                    case -121: //TextElement.Foreground
                        return null;
                    default: // All other known properties are instance members
                        return false;
                }
            }
        }
    }
}

