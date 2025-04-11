// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using FluentAssertions.Execution;
using Application = System.Windows.Application;

namespace PresentationFramework.Fluent.Tests.ResourceTests;
public class FluentResourceDictionaryTests
{
    [WpfTheory]
    [MemberData(nameof(s_themeDictionarySourceList))]
    public void Fluent_ResourceDictionary_LoadTests(string source)
    {
        LoadFluentResourceDictionary(source);
    }

    [WpfTheory]
    [MemberData(nameof(GetColorDictionary_MatchKeys_TestData))]
    public void Fluent_ColorDictionary_MatchKeysTest(string firstSource, string secondSource)
    {
        ResourceDictionary dictionary1 = LoadFluentResourceDictionary(firstSource);
        ResourceDictionary dictionary2 = LoadFluentResourceDictionary(secondSource);

        GetResourceKeysFromResourceDictionary(dictionary1,
            out List<string> dictionary1StringKeys, out List<object> dictionary1ObjectKeys);

        GetResourceKeysFromResourceDictionary(dictionary2,
            out List<string> dictionary2StringKeys, out List<object> dictionary2ObjectKeys);

        List<string> dictionary1ExtraStringKeys = dictionary1StringKeys.Except(dictionary2StringKeys).ToList();
        List<string> dictionary2ExtraStringKeys = dictionary2StringKeys.Except(dictionary1StringKeys).ToList();

        List<object> dictionary1ExtraObjectKeys = dictionary1ObjectKeys.Except(dictionary2ExtraStringKeys).ToList();
        List<object> dictionary2ExtraObjectKeys = dictionary2ObjectKeys.Except(dictionary1ObjectKeys).ToList();

        Log_ExtraKeys(dictionary1ExtraStringKeys, $"Dictionary 1 : {firstSource} extra keys");
        Log_ExtraKeys(dictionary2ExtraStringKeys, $"Dictionary 2 : {secondSource} extra keys");

        using (new AssertionScope())
        {
            dictionary1ExtraStringKeys.Should().BeEmpty();
            dictionary2ExtraStringKeys.Should().BeEmpty();
            dictionary1ExtraObjectKeys.Should().BeEmpty();
            dictionary2ExtraObjectKeys.Should().BeEmpty();
        }
    }

    #region Helper Methods

    private static void Log_ExtraKeys(List<string> dictionary1ExtraStringKeys, string v)
    {
        Console.WriteLine(v);
        if (dictionary1ExtraStringKeys.Count == 0)
        {
            Console.WriteLine("None\n");
            return;
        }

        foreach (string key in dictionary1ExtraStringKeys)
        {
            Console.WriteLine(key);
        }
        Console.WriteLine();
    }

    private static ResourceDictionary LoadFluentResourceDictionary(string source)
    {
        var uri = new Uri(source, UriKind.RelativeOrAbsolute);

        if(Application.LoadComponent(uri) is not ResourceDictionary resourceDictionary)
        {
            throw new ArgumentException($"The source : {source} can not be loaded.");
        }

        return resourceDictionary;
    }

    private static int GetResourceKeysFromResourceDictionary(ResourceDictionary resourceDictionary,
        out List<string> stringResourceKeys,
        out List<object> objectResourceKeys)
    {
        ArgumentNullException.ThrowIfNull(resourceDictionary, nameof(resourceDictionary));
        stringResourceKeys = new List<string>();
        objectResourceKeys = new List<object>();

        int resourceDictionaryKeysCount = resourceDictionary.Count;

        foreach (object key in resourceDictionary.Keys)
        {
            if (key is string skey)
            {
                stringResourceKeys.Add(skey);
            }
            else
            {
                objectResourceKeys.Add(key);
            }
        }

        return resourceDictionaryKeysCount;
    }

    #endregion


    #region Test Data

    public static IEnumerable<object[]> GetColorDictionary_MatchKeys_TestData()
    {
        int count = s_colorDictionarySourceList.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                yield return new object[] { s_colorDictionarySourceList[i], s_colorDictionarySourceList[j] };
            }
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

    private const string ThemeDictionaryPath = @"/PresentationFramework.Fluent;component/Themes";
    private const string ColorDictionaryPath = @"/PresentationFramework.Fluent;component/Resources/Theme";

    #endregion

}