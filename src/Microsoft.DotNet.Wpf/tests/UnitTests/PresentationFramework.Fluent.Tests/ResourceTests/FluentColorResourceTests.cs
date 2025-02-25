// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions.Execution;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace PresentationFramework.Fluent.Tests.ResourceTests;
public class FluentColorResourceTests
{

    [WpfTheory]
    [MemberData(nameof(ColorDictionary_Validate_TestData))]
    public void Fluent_ColorDictionary_ValidateTest(string testSource, string actualSource)
    {
        ResourceDictionary dictionary1 = LoadResourceDictionary(testSource);
        ResourceDictionary dictionary2 = LoadResourceDictionary(actualSource);

        using (new AssertionScope())
        {
            List<object> missingKeys = new List<object>();
            foreach (object key in dictionary1.Keys)
            {
                if (dictionary2.Contains(key))
                {
                    if (dictionary1[key] is Color)
                    {
                        dictionary2[key].Should().BeOfType<Color>();
                        dictionary1[key].Should().Be(dictionary2[key]);
                    }
                }
                else
                {
                    missingKeys.Add(key);
                }
            }

            missingKeys.Should().BeEmpty();
        }
    }

    [WpfTheory]
    [MemberData(nameof(ThemeDictionary_Validate_TestData))]
    public void ThemeDictionary_FluentColor_ValidateTest(string testSource, string actualSource)
    {
        actualSource.Should().NotBeNull();
        ResourceDictionary dictionary1 = LoadResourceDictionary(testSource);
        ResourceDictionary dictionary2 = LoadResourceDictionary(actualSource);

        using (new AssertionScope())
        {
            List<object> missingKeys = new List<object>();
            foreach (object key in dictionary1.Keys)
            {
                if (dictionary2.Contains(key))
                {
                    if (dictionary1[key] is Color)
                    {
                        dictionary2[key].Should().BeOfType<Color>();
                        dictionary1[key].Should().Be(dictionary2[key]);
                    }
                }
                else
                {
                    missingKeys.Add(key);
                }
            }

            missingKeys.Should().BeEmpty();
        }
    }


    #region Helper Methods

    private static ResourceDictionary LoadResourceDictionary(string source)
    {
        var uri = new Uri(source, UriKind.RelativeOrAbsolute);

        if (Application.LoadComponent(uri) is not ResourceDictionary resourceDictionary)
        {
            throw new ArgumentException($"The source : {source} can not be loaded.");
        }

        return resourceDictionary;
    }

    // private static ResourceDictionary LoadTestDictionary(string resourceName)
    // {
    //     var assembly = Assembly.GetExecutingAssembly();
    //     using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new ArgumentException($"The resource : {resourceName} can not be loaded.");

    //     if (XamlReader.Load(stream) is not ResourceDictionary dictionary)
    //     {
    //         throw new ArgumentException($"The resource : {resourceName} can not be loaded.");
    //     }

    //     return dictionary;
    // }

    #endregion


    #region Test Data

    public static IEnumerable<object[]> ColorDictionary_Validate_TestData()
    {
        int count = s_colorDictionarySourceList.Count;
        for (int i = 0; i < count; i++)
        {
            yield return new object[] { s_testColorDictionarySourceList[i], s_colorDictionarySourceList[i] };
        }
    }

    public static IEnumerable<object[]> ThemeDictionary_Validate_TestData()
    {
        int count = s_themeDictionarySourceList.Count;
        for (int i = 1; i < count; i++)
        {
            yield return new object[] { s_testColorDictionarySourceList[i - 1], (string)s_themeDictionarySourceList[i][0] };
        }
    }

    public static readonly IList<object[]> s_themeDictionarySourceList
        = new List<object[]>
            {
                new object[] { $"{ThemeDictionaryPath}/Fluent.xaml" },
                new object[] { $"{ThemeDictionaryPath}/Fluent.Light.xaml" },
                new object[] { $"{ThemeDictionaryPath}/Fluent.Dark.xaml" },
                new object[] { $"{ThemeDictionaryPath}/Fluent.HC.xaml" },
            };

    public static readonly IList<string> s_colorDictionarySourceList
        = new List<string>
        {
            $"{ColorDictionaryPath}/Light.xaml",
            $"{ColorDictionaryPath}/Dark.xaml",
            $"{ColorDictionaryPath}/HC.xaml"
        };

    public static readonly IList<string> s_testColorDictionarySourceList
        = new List<string>
        {
            $"{TestColorDictionaryPath}/Light.Test.xaml",
            $"{TestColorDictionaryPath}/Dark.Test.xaml",
            $"{TestColorDictionaryPath}/HC.Test.xaml"
        };

    private const string ThemeDictionaryPath = @"/PresentationFramework.Fluent;component/Themes";
    private const string ColorDictionaryPath = @"/PresentationFramework.Fluent;component/Resources/Theme";
    private const string TestColorDictionaryPath = @"/PresentationFramework.Fluent.Tests;component/ResourceTests/Data";

    #endregion

}
