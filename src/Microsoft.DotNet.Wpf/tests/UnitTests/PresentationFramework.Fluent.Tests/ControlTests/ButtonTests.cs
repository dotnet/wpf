// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PresentationFramework.Fluent.Tests.TestUtilities;
using FluentAssertions.Execution;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace PresentationFramework.Fluent.Tests.ControlTests;

public class ButtonTests : BaseControlTests
{
    public ButtonTests()
    {
        TestButton = SetupTestButton();
    }

    [WpfTheory]
    [MemberData(nameof(ColorModes_TestData))]
    public void Button_Initialization_Test(ColorMode colorMode)
    {
        SetColorMode(TestWindow, colorMode);
        TestWindow.Show();

        ResourceDictionary rd = GetTestDataDictionary(colorMode, "");
        VerifyControlProperties(TestButton, rd);
    }

    [WpfTheory]
    [MemberData(nameof(ColorModes_TestData))]
    public void Button_IsEnabled_False_Test(ColorMode colorMode)
    {
        SetColorMode(TestWindow, colorMode);
        TestButton.IsEnabled = false;
        TestWindow.Show();

        ResourceDictionary rd = GetTestDataDictionary(colorMode, "Disabled");
        VerifyControlProperties(TestButton, rd);
    }

    #region Override Methods

    #pragma warning disable CS8604
    public override List<FrameworkElement> GetStyleParts(Control element)
    {
        List<FrameworkElement> templateParts = new List<FrameworkElement>();
        templateParts.Add(element);

        Border? border = element.Template.FindName("ContentBorder", element) as Border;
        border.Should().NotBeNull();

        templateParts.Add(border);

        ContentPresenter? contentPresenter = element.Template.FindName("ContentPresenter", element) as ContentPresenter;
        contentPresenter.Should().NotBeNull();

        templateParts.Add(contentPresenter);

        return templateParts;
    }

    #pragma warning restore CS8604

    #pragma warning disable CS8602

    public override void VerifyControlProperties(FrameworkElement element, ResourceDictionary expectedProperties)
    {
        if(element is not Button button)
            return;

        List<FrameworkElement> parts = GetStyleParts(button);

        Button? part_Button = parts[0] as Button;
        Border? part_ContentBorder = parts[1] as Border;
        ContentPresenter? part_ContentPresenter = parts[2] as ContentPresenter;
    
        using(new AssertionScope())
        {
            part_Button.Should().NotBeNull();
            BrushComparer.Equal(part_Button.Background, (Brush)expectedProperties["Button_Background"]).Should().BeTrue();
            if(!BrushComparer.Equal(part_Button.Background, (Brush)expectedProperties["Button_Background"]))
            {
                Console.WriteLine("part_Button.Background does not match expected value");
                BrushComparer.LogBrushDifference(part_Button.Background, (Brush)expectedProperties["Button_Background"]);
            }
            BrushComparer.Equal(part_Button.Foreground, (Brush)expectedProperties["Button_Foreground"]).Should().BeTrue();
            if(!BrushComparer.Equal(part_Button.Foreground, (Brush)expectedProperties["Button_Foreground"]))
            {
                Console.WriteLine("part_Button.Foreground does not match expected value");
                BrushComparer.LogBrushDifference(part_Button.Foreground, (Brush)expectedProperties["Button_Foreground"]);
            }
            part_Button.BorderThickness.Should().Be((Thickness)expectedProperties["Button_BorderThickness"]);
            part_Button.IsTabStop.Should().Be((bool)expectedProperties["Button_IsTabStop"]);
            part_Button.Padding.Should().Be(expectedProperties["Button_Padding"]);
            part_Button.Margin.Should().Be(expectedProperties["Button_Margin"]);
            part_Button.HorizontalAlignment.Should().Be((HorizontalAlignment)expectedProperties["Button_HorizontalAlignment"]);
            part_Button.VerticalAlignment.Should().Be((VerticalAlignment)expectedProperties["Button_VerticalAlignment"]);
            part_Button.HorizontalContentAlignment.Should().Be((HorizontalAlignment)expectedProperties["Button_HorizontalContentAlignment"]);
            part_Button.VerticalContentAlignment.Should().Be((VerticalAlignment)expectedProperties["Button_VerticalContentAlignment"]);
            part_Button.Focusable.Should().Be((bool)expectedProperties["Button_Focusable"]);
            part_Button.Cursor.Should().Be((Cursor)expectedProperties["Button_Cursor"]);
        
            part_ContentBorder.Should().NotBeNull();
            BrushComparer.Equal(part_ContentBorder.Background, (Brush)expectedProperties["ContentBorder_Background"]).Should().BeTrue();
            if(!BrushComparer.Equal(part_ContentBorder.Background, (Brush)expectedProperties["ContentBorder_Background"]))
            {
                Console.WriteLine("part_ContentBorder.Background does not match expected value");
                BrushComparer.LogBrushDifference(part_ContentBorder.Background, (Brush)expectedProperties["ContentBorder_Background"]);
            }
            part_ContentBorder.BorderThickness.Should().Be((Thickness)expectedProperties["ContentBorder_BorderThickness"]);
            part_ContentBorder.CornerRadius.Should().Be((CornerRadius)expectedProperties["ContentBorder_CornerRadius"]);
            part_ContentBorder.Margin.Should().Be((Thickness)expectedProperties["ContentBorder_Margin"]);
            part_ContentBorder.Padding.Should().Be((Thickness)expectedProperties["ContentBorder_Padding"]);
            part_ContentBorder.Focusable.Should().Be((bool)expectedProperties["ContentBorder_Focusable"]);
            part_ContentBorder.HorizontalAlignment.Should().Be((HorizontalAlignment)expectedProperties["ContentBorder_HorizontalAlignment"]);
            part_ContentBorder.VerticalAlignment.Should().Be((VerticalAlignment)expectedProperties["ContentBorder_VerticalAlignment"]);
        
            part_ContentPresenter.Should().NotBeNull();
            BrushComparer.Equal(TextElement.GetForeground(part_ContentPresenter), (Brush)expectedProperties["ContentPresenter_Foreground"]).Should().BeTrue();
            if(!BrushComparer.Equal(TextElement.GetForeground(part_ContentPresenter), (Brush)expectedProperties["ContentPresenter_Foreground"]))
            {
                Console.WriteLine("part_ContentPresenter.Foreground does not match expected value");
                BrushComparer.LogBrushDifference(TextElement.GetForeground(part_ContentPresenter), (Brush)expectedProperties["ContentPresenter_Foreground"]);
            }
            part_ContentPresenter.RecognizesAccessKey.Should().Be((bool)expectedProperties["ContentPresenter_RecognizesAccessKey"]);
            part_ContentPresenter.HorizontalAlignment.Should().Be((HorizontalAlignment)expectedProperties["ContentPresenter_HorizontalAlignment"]);
            part_ContentPresenter.VerticalAlignment.Should().Be((VerticalAlignment)expectedProperties["ContentPresenter_VerticalAlignment"]);
            part_ContentPresenter.Margin.Should().Be((Thickness)expectedProperties["ContentPresenter_Margin"]);
            part_ContentPresenter.Focusable.Should().Be((bool)expectedProperties["ContentPresenter_Focusable"]);
        }
    }

    #pragma warning restore CS8602

    #endregion

    #region Private Methods

    private Button SetupTestButton()
    {
        Button button = new Button() { Content = "Hello" };
        AddControlToView(TestWindow, button);
        return button;
    }

    #endregion

    #region Private Properties

    private Button TestButton { get; set; }
    protected override string TestDataDictionaryPath => @"/PresentationFramework.Fluent.Tests;component/ControlTests/Data/ButtonTests.xaml";

    #endregion

}

