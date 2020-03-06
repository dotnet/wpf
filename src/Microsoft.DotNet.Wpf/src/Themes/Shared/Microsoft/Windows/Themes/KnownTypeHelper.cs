// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.Text;
using System.Xaml;
using System.Windows.Baml2006;
using System.Windows;
using System.Xaml.Schema;

namespace Microsoft.Windows.Themes
{
    internal class KnownTypeHelper : ThemeKnownTypeHelper
    {
        static KnownTypeHelper()
        {
            KnownTypeHelper helper = new KnownTypeHelper();
            System.Windows.Markup.XamlReader.BamlSharedSchemaContext.ThemeKnownTypeHelpers.Add(helper);
        }

        public override XamlType GetKnownXamlType(String name)
        {
            switch(name)
            {

#if (THEME_AERO || THEME_AERO2)
                case "ListBoxChrome": return Create_BamlType_ListBoxChrome();
#endif
#if (THEME_AERO || THEME_AERO2 || THEME_LUNA || THEME_ROYALE)
                case "BulletChrome": return Create_BamlType_BulletChrome();
                case "ButtonChrome": return Create_BamlType_ButtonChrome();
#endif
#if (THEME_AERO || THEME_AERO2 || THEME_AEROLITE || THEME_LUNA || THEME_ROYALE)
                case "ScrollChrome": return Create_BamlType_ScrollChrome();
                case "ScrollGlyph": return Create_BamlType_ScrollGlyph();
#endif                
#if (THEME_CLASSIC)
                case "ClassicBorderDecorator": return Create_BamlType_ClassicBorderDecorator();
                case "ClassicBorderStyle": return Create_BamlType_ClassicBorderStyle();
#endif
                case "SystemDropShadowChrome": return Create_BamlType_SystemDropShadowChrome();
                default: return null;
            }
        }

#if (THEME_CLASSIC)
 
        private static WpfKnownMember Create_BamlProperty_ClassicBorderDecorator_BorderStyle()
        {
            Type type = typeof(Microsoft.Windows.Themes.ClassicBorderDecorator);
            DependencyProperty  dp = Microsoft.Windows.Themes.ClassicBorderDecorator.BorderStyleProperty;
            var bamlMember = new WpfKnownMember( System.Windows.Markup.XamlReader.BamlSharedSchemaContext,  // Schema Context
                            System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(Microsoft.Windows.Themes.ClassicBorderDecorator)), // DeclaringType
                            "BorderStyle", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            false // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(Microsoft.Windows.Themes.ClassicBorderStyle);
            bamlMember.Freeze();
            return bamlMember;
        }
       
        private static WpfKnownType Create_BamlType_ClassicBorderDecorator()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "ClassicBorderDecorator",
                                              typeof(Microsoft.Windows.Themes.ClassicBorderDecorator));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.ClassicBorderDecorator(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }

        private static WpfKnownType Create_BamlType_ClassicBorderStyle()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "ClassicBorderStyle",
                                              typeof(Microsoft.Windows.Themes.ClassicBorderStyle));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.ClassicBorderStyle(); };
            bamlType.TypeConverterType = typeof(Microsoft.Windows.Themes.ClassicBorderStyle);
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }
#endif

#if (THEME_AERO || THEME_AERO2 || THEME_LUNA || THEME_ROYALE)

        private static WpfKnownType Create_BamlType_ButtonChrome()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "ButtonChrome",
                                              typeof(Microsoft.Windows.Themes.ButtonChrome));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.ButtonChrome(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }

        private static WpfKnownType Create_BamlType_BulletChrome()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "BulletChrome",
                                              typeof(Microsoft.Windows.Themes.BulletChrome));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.BulletChrome(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }

#endif

#if (THEME_AERO || THEME_AERO2 || THEME_AEROLITE || THEME_LUNA || THEME_ROYALE)

        private static WpfKnownMember Create_BamlProperty_ScrollChrome_ScrollGlyph()
        {
            Type type = typeof(Microsoft.Windows.Themes.ScrollChrome);
            DependencyProperty dp = Microsoft.Windows.Themes.ScrollChrome.ScrollGlyphProperty;
            var bamlMember = new WpfKnownMember(System.Windows.Markup.XamlReader.BamlSharedSchemaContext,  // Schema Context
                            System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(Microsoft.Windows.Themes.ScrollChrome)), // DeclaringType
                            "ScrollGlyph", // Name
                             dp, // DependencyProperty
                            false, // IsReadOnly
                            true // IsAttachable
                                     );
            bamlMember.TypeConverterType = typeof(Microsoft.Windows.Themes.ScrollGlyph);
            bamlMember.Freeze();
            return bamlMember;
        }


        private static WpfKnownType Create_BamlType_ScrollChrome()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "ScrollChrome",
                                              typeof(Microsoft.Windows.Themes.ScrollChrome));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.ScrollChrome(); };
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }
        
        private static WpfKnownType Create_BamlType_ScrollGlyph()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "ScrollGlyph",
                                              typeof(Microsoft.Windows.Themes.ScrollGlyph));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.ScrollGlyph(); };
            bamlType.TypeConverterType = typeof(Microsoft.Windows.Themes.ScrollGlyph);
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }

#endif

#if (THEME_AERO || THEME_AERO2)

        private static WpfKnownType Create_BamlType_ListBoxChrome()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "ListBoxChrome",
                                              typeof(Microsoft.Windows.Themes.ListBoxChrome));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.ListBoxChrome(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }

#endif

        private static WpfKnownType Create_BamlType_SystemDropShadowChrome()
        {
            var bamlType = new ThemesKnownType(System.Windows.Markup.XamlReader.BamlSharedSchemaContext, // SchemaContext
                                              0, "SystemDropShadowChrome",
                                              typeof(Microsoft.Windows.Themes.SystemDropShadowChrome));
            bamlType.DefaultConstructor = delegate() { return new Microsoft.Windows.Themes.SystemDropShadowChrome(); };
            bamlType.ContentPropertyName = "Child";
            bamlType.RuntimeNamePropertyName = "Name";
            bamlType.XmlLangPropertyName = "Language";
            bamlType.UidPropertyName = "Uid";
            bamlType.IsUsableDuringInit = true;
            bamlType.CollectionKind = XamlCollectionKind.None;
            bamlType.Freeze();
            return bamlType;
        }

        class ThemesKnownType : WpfKnownType
        {
            public ThemesKnownType(XamlSchemaContext xsc, int bamlNumber, string name, Type type) :
                base(xsc, bamlNumber, name, type)
            {
            }

            protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
            {
                XamlMember member = FindKnownMember(name);
                if (member != null)
                {
                    return member;
                }
              
                return base.LookupMember(name, skipReadOnlyCheck);
            }

            protected override XamlMember LookupAttachableMember(string name)
            {
                XamlMember member = FindKnownMember(name);
                if (member != null)
                {
                    return member;
                }

                return base.LookupAttachableMember(name);
            }

            private XamlMember FindKnownMember(string name)
            {
                XamlMember member;
                if (Members.TryGetValue(name, out member))
                {
                    return member;
                }
#if (THEME_AERO || THEME_AERO2 || THEME_AEROLITE || THEME_LUNA || THEME_ROYALE)
                if (UnderlyingType == typeof(ScrollChrome) && name == "ScrollGlyph")
                {
                    member = Create_BamlProperty_ScrollChrome_ScrollGlyph();
                    if (Members.TryAdd(name, member))
                    {
                        return member;
                    }
                    else
                    {
                        return Members[name];
                    }
                }
#endif
#if (THEME_CLASSIC)
                if (UnderlyingType == typeof(ClassicBorderDecorator) && name == "BorderStyle")
                {
                    member = Create_BamlProperty_ClassicBorderDecorator_BorderStyle();
                    if (Members.TryAdd(name, member))
                    {
                        return member;
                    }
                    else
                    {
                        return Members[name];
                    }
                }
#endif
                return null;
            }
        }
    }
}
