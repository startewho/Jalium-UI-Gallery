using Jalium.UI.Controls;
using Jalium.UI.Controls.Editor;

namespace Jalium.UI.Gallery.Modules.Main.Views.Pages;

/// <summary>
/// Code-behind for RangeSliderPage.jalxaml — wires the interactive demo's
/// RangeStartChanged / RangeEndChanged events to the value labels and loads
/// syntax-highlighted code samples.
/// </summary>
public partial class RangeSliderPage : Page
{
    private const string XamlExample = @"<!-- Basic RangeSlider -->
<RangeSlider Minimum=""0""
             Maximum=""100""
             RangeStart=""20""
             RangeEnd=""80""
             Height=""24""/>

<!-- Enforce a minimum gap between thumbs -->
<RangeSlider Minimum=""0""
             Maximum=""100""
             RangeStart=""35""
             RangeEnd=""65""
             MinimumRange=""10""
             Height=""24""/>

<!-- Snap each thumb to tick marks -->
<RangeSlider Minimum=""0""
             Maximum=""100""
             RangeStart=""25""
             RangeEnd=""75""
             TickFrequency=""25""
             IsSnapToTickEnabled=""True""
             Height=""24""/>

<!-- Price filter with live labels -->
<StackPanel Orientation=""Vertical"">
    <RangeSlider x:Name=""PriceRange""
                 Minimum=""0""
                 Maximum=""1000""
                 RangeStart=""100""
                 RangeEnd=""600""
                 Height=""24""
                 RangeStartChanged=""OnPriceStartChanged""
                 RangeEndChanged=""OnPriceEndChanged""/>
    <TextBlock x:Name=""PriceRangeLabel""
               Text=""$100 — $600""/>
</StackPanel>";

    private const string CSharpExample = @"using Jalium.UI.Controls;

public partial class FilterPage : Page
{
    public FilterPage()
    {
        InitializeComponent();

        PriceRange.RangeStartChanged += OnPriceStartChanged;
        PriceRange.RangeEndChanged += OnPriceEndChanged;
        UpdatePriceLabel();
    }

    private void OnPriceStartChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        UpdatePriceLabel();
    }

    private void OnPriceEndChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        UpdatePriceLabel();
    }

    private void UpdatePriceLabel()
    {
        var lo = (int)PriceRange.RangeStart;
        var hi = (int)PriceRange.RangeEnd;
        PriceRangeLabel.Text = $""${lo} — ${hi}"";
    }

    // Programmatic creation
    private RangeSlider CreateRange(
        double min, double max,
        double start, double end,
        double minGap = 0)
    {
        return new RangeSlider
        {
            Minimum = min,
            Maximum = max,
            RangeStart = start,
            RangeEnd = end,
            MinimumRange = minGap,
            Height = 24
        };
    }
}";

    public RangeSliderPage()
    {
        InitializeComponent();

        if (DemoRangeSlider != null)
        {
            DemoRangeSlider.RangeStartChanged += OnRangeStartChanged;
            DemoRangeSlider.RangeEndChanged += OnRangeEndChanged;
        }

        LoadCodeExamples();
    }

    private void LoadCodeExamples()
    {
        if (XamlCodeEditor != null)
        {
            XamlCodeEditor.SyntaxHighlighter = JalxamlSyntaxHighlighter.Create();
            XamlCodeEditor.LoadText(XamlExample);
        }
        if (CSharpCodeEditor != null)
        {
            CSharpCodeEditor.SyntaxHighlighter = RegexSyntaxHighlighter.CreateCSharpHighlighter();
            CSharpCodeEditor.LoadText(CSharpExample);
        }
    }

    private void OnRangeStartChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RangeStartLabel != null)
        {
            RangeStartLabel.Text = $"{(int)e.NewValue}";
        }
    }

    private void OnRangeEndChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RangeEndLabel != null)
        {
            RangeEndLabel.Text = $"{(int)e.NewValue}";
        }
    }
}
