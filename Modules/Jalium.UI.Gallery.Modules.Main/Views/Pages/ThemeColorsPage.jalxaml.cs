using System.Globalization;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Media;

namespace Jalium.UI.Gallery.Modules.Main.Views.Pages;

/// <summary>
/// Theme color browser. Walks the active <see cref="Application.Resources"/> tree
/// (including merged + theme dictionaries), surfaces every Color/Brush key, and
/// lets the user copy either the raw key, a ready-to-paste <c>{ThemeResource Key}</c>
/// /<c>{StaticResource Key}</c> expression, or the literal value via a per-row
/// <see cref="SplitButton"/> (primary click = copy key, flyout = alternates).
/// </summary>
public partial class ThemeColorsPage : Page
{
    private const string ThemeOptionDark = "Dark";
    private const string ThemeOptionLight = "Light";
    private const string ThemeOptionHighContrast = "HighContrast";

    /// <summary>
    /// One row of the resource browser.
    /// </summary>
    private sealed class ResourceEntry
    {
        public required string Key { get; init; }
        public required object Value { get; init; }
        public required ResourceKind Kind { get; init; }
        public required string Display { get; init; }
        public required Brush Preview { get; init; }
    }

    private enum ResourceKind
    {
        Color,
        SolidBrush,
        GradientBrush
    }

    private readonly List<ResourceEntry> _allEntries = new();

    /// <summary>
    /// The theme dictionary key this page is *previewing*.
    /// Initialized from the live application theme but driven independently
    /// after the user picks something from the Theme combo box, so toggling
    /// the combo never calls ThemeManager.ApplyTheme — it just walks a
    /// different ThemeDictionaries slice.
    /// </summary>
    private string _previewThemeKey = ThemeManager.CurrentTheme.ToString();

    public ThemeColorsPage()
    {
        InitializeComponent();
        SetupThemeSelector();
        WireFilterEvents();
        ReloadAll();
    }

    private void SetupThemeSelector()
    {
        if (ThemeSelector == null) return;

        ThemeSelector.Items.Add(ThemeOptionDark);
        ThemeSelector.Items.Add(ThemeOptionLight);
        ThemeSelector.Items.Add(ThemeOptionHighContrast);
        ThemeSelector.SelectedItem = ThemeManager.CurrentTheme.ToString();
        ThemeSelector.SelectionChanged += OnThemeSelectorChanged;
    }

    private void WireFilterEvents()
    {
        if (FilterBox != null)
            FilterBox.TextChanged += (_, _) => RenderRows();

        if (ShowColorsCheck != null)
        {
            ShowColorsCheck.Checked += (_, _) => RenderRows();
            ShowColorsCheck.Unchecked += (_, _) => RenderRows();
        }
        if (ShowBrushesCheck != null)
        {
            ShowBrushesCheck.Checked += (_, _) => RenderRows();
            ShowBrushesCheck.Unchecked += (_, _) => RenderRows();
        }
        if (ShowGradientsCheck != null)
        {
            ShowGradientsCheck.Checked += (_, _) => RenderRows();
            ShowGradientsCheck.Unchecked += (_, _) => RenderRows();
        }
    }

    private void OnThemeSelectorChanged(object? sender, EventArgs e)
    {
        if (ThemeSelector?.SelectedItem is not string variantName)
            return;

        // Preview-only switch: just remember which theme dictionary to walk
        // next time, and rebuild rows. Do NOT call ThemeManager.ApplyTheme.
        //
        // Why: ApplyTheme reparses Generic.jalxaml + every control style
        // dictionary AND sweeps every DynamicResource subscription. With
        // 500+ live SplitButton/MenuFlyout rows on this page, those two
        // things racing each other corrupted the parser/refresh state in
        // a way that left the ComboBox unresponsive after the first switch.
        // Reading values straight out of ThemeDictionaries[<key>] gives the
        // user the exact same data without any global side-effect.
        _previewThemeKey = variantName;
        ReloadAll();
    }

    /// <summary>
    /// Re-walks the resource tree for the currently active theme and rebuilds the rows.
    /// </summary>
    private void ReloadAll()
    {
        _allEntries.Clear();

        var app = Application.Current;
        if (app?.Resources == null)
        {
            RenderRows();
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        CollectFromDictionary(app.Resources, seen);

        _allEntries.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Key, b.Key));
        RenderRows();
    }

    /// <summary>
    /// Walks a ResourceDictionary, its MergedDictionaries, and the slice of
    /// ThemeDictionaries that matches the *previewed* theme key. Theme-dictionary
    /// values win over merged-dictionary values to mirror the lookup order
    /// the framework uses at runtime. The preview key is independent of the
    /// global <see cref="ResourceDictionary.CurrentThemeKey"/> so users can
    /// inspect any theme without triggering a real theme switch.
    /// </summary>
    private void CollectFromDictionary(ResourceDictionary dict, HashSet<string> seen)
    {
        // Theme dictionary slice for the previewed theme has priority.
        // Try exact match first, then case-insensitive fallback so combo-box
        // strings like "Dark" still resolve when the dictionary uses different casing.
        if (TryGetThemedSlice(dict, _previewThemeKey, out var themed) && themed != null)
        {
            CollectFromDictionary(themed, seen);
        }

        foreach (var key in dict.Keys)
        {
            if (key is not string keyName)
                continue;

            if (!seen.Add(keyName))
                continue;

            var raw = dict[key];
            if (raw == null)
                continue;

            if (TryBuildEntry(keyName, raw, out var entry))
            {
                _allEntries.Add(entry);
            }
        }

        // Recurse into merged dictionaries (in reverse so earlier ones win,
        // matching ResourceDictionary lookup order).
        for (var i = dict.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            CollectFromDictionary(dict.MergedDictionaries[i], seen);
        }
    }

    /// <summary>
    /// Look up a ThemeDictionaries entry by name, falling back to a
    /// case-insensitive match so the combo box string "Dark" resolves
    /// even if the dictionary's key was registered as "DARK", etc.
    /// </summary>
    private static bool TryGetThemedSlice(ResourceDictionary dict, string themeKey, out ResourceDictionary? themed)
    {
        if (dict.ThemeDictionaries.TryGetValue(themeKey, out themed))
            return themed != null;

        foreach (var kvp in dict.ThemeDictionaries)
        {
            if (kvp.Key is string s && string.Equals(s, themeKey, StringComparison.OrdinalIgnoreCase))
            {
                themed = kvp.Value;
                return themed != null;
            }
        }

        themed = null;
        return false;
    }

    private static bool TryBuildEntry(string key, object value, out ResourceEntry entry)
    {
        switch (value)
        {
            case Color color:
                entry = new ResourceEntry
                {
                    Key = key,
                    Value = color,
                    Kind = ResourceKind.Color,
                    Display = FormatColor(color),
                    Preview = new SolidColorBrush(color)
                };
                return true;

            case SolidColorBrush solid:
                entry = new ResourceEntry
                {
                    Key = key,
                    Value = solid,
                    Kind = ResourceKind.SolidBrush,
                    Display = FormatColor(solid.Color) + (solid.Opacity < 1.0 ? $"  α={solid.Opacity:0.##}" : string.Empty),
                    Preview = solid
                };
                return true;

            case LinearGradientBrush gradient:
                entry = new ResourceEntry
                {
                    Key = key,
                    Value = gradient,
                    Kind = ResourceKind.GradientBrush,
                    Display = FormatGradient(gradient),
                    Preview = gradient
                };
                return true;

            case GradientBrush other:
                entry = new ResourceEntry
                {
                    Key = key,
                    Value = other,
                    Kind = ResourceKind.GradientBrush,
                    Display = FormatGradient(other),
                    Preview = other
                };
                return true;

            default:
                entry = null!;
                return false;
        }
    }

    private static string FormatColor(Color c)
    {
        return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);
    }

    private static string FormatGradient(GradientBrush gradient)
    {
        if (gradient.GradientStops.Count == 0)
            return "(empty gradient)";

        var first = gradient.GradientStops[0].Color;
        var last = gradient.GradientStops[^1].Color;
        return gradient.GradientStops.Count == 2
            ? $"{FormatColor(first)} → {FormatColor(last)}"
            : $"{FormatColor(first)} → {FormatColor(last)}  ({gradient.GradientStops.Count} stops)";
    }

    /// <summary>
    /// Rebuilds the visible row list based on the search/filter state.
    /// </summary>
    private void RenderRows()
    {
        if (ResourceList == null)
            return;

        ResourceList.Children.Clear();

        var filter = (FilterBox?.Text ?? string.Empty).Trim();
        var showColors = ShowColorsCheck?.IsChecked ?? true;
        var showBrushes = ShowBrushesCheck?.IsChecked ?? true;
        var showGradients = ShowGradientsCheck?.IsChecked ?? true;

        var visible = 0;
        foreach (var entry in _allEntries)
        {
            if (!PassesKindFilter(entry, showColors, showBrushes, showGradients))
                continue;
            if (filter.Length > 0 && entry.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            ResourceList.Children.Add(BuildRow(entry, visible));
            visible++;
        }

        if (visible == 0)
        {
            ResourceList.Children.Add(new TextBlock
            {
                Text = _allEntries.Count == 0
                    ? "No theme color resources found."
                    : "No matching keys.",
                Margin = new Thickness(20, 24, 20, 24),
                Opacity = 0.6,
                FontSize = 13
            });
        }

        if (CountText != null)
        {
            CountText.Text = visible == _allEntries.Count
                ? $"{_allEntries.Count} keys"
                : $"{visible} / {_allEntries.Count} keys";
        }
    }

    private static bool PassesKindFilter(ResourceEntry entry, bool showColors, bool showBrushes, bool showGradients)
    {
        // No checkbox selected → no kind filter applied, show every kind.
        if (!showColors && !showBrushes && !showGradients)
            return true;

        return entry.Kind switch
        {
            ResourceKind.Color => showColors,
            ResourceKind.SolidBrush => showBrushes,
            ResourceKind.GradientBrush => showGradients,
            _ => true
        };
    }

    /// <summary>
    /// Builds one row: [swatch] [key + value] [Copy SplitButton].
    /// SplitButton primary action copies the key; the flyout exposes
    /// "{ThemeResource …}" expression and the literal hex/value string.
    /// Alternating row backgrounds use SubtleFillColorTertiaryBrush for striping
    /// without hardcoding a non-themed color.
    /// </summary>
    private FrameworkElement BuildRow(ResourceEntry entry, int rowIndex)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Swatch
        var swatch = new Border
        {
            Background = entry.Preview,
            Width = 40,
            Height = 32,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(12, 8, 12, 8),
            BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("ControlStrokeColorDefaultBrush") as Brush
                          ?? new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80))
        };
        Grid.SetColumn(swatch, 0);
        grid.Children.Add(swatch);

        // Key + value text stack
        var textStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 12, 8)
        };
        textStack.Children.Add(new TextBlock
        {
            Text = entry.Key,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = $"{KindLabel(entry.Kind)}  ·  {entry.Display}",
            FontSize = 11,
            Opacity = 0.65,
            FontFamily = "Cascadia Code,Consolas,Menlo,monospace",
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        // Copy SplitButton: primary action copies the key, flyout exposes alternates.
        var copySplit = BuildCopySplitButton(entry);
        Grid.SetColumn(copySplit, 2);
        grid.Children.Add(copySplit);

        // Whole-row click also copies the key for convenience.
        var rowBackground = (rowIndex & 1) == 1
            ? TryFindResource("SubtleFillColorTertiaryBrush") as Brush
            : null;

        var rowBorder = new Border
        {
            Background = rowBackground ?? new SolidColorBrush(Color.FromArgb(0x00, 0, 0, 0)),
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            Child = grid
        };
        rowBorder.MouseLeftButtonUp += (_, _) => CopyToClipboard(entry.Key, $"Copied key: {entry.Key}");
        return rowBorder;
    }

    /// <summary>
    /// Builds the per-row SplitButton.
    /// Primary action = copy raw key.
    /// Flyout items = {ThemeResource} expression, hex/value string, and
    /// when applicable a StaticResource expression for completeness.
    /// </summary>
    private SplitButton BuildCopySplitButton(ResourceEntry entry)
    {
        var themeResourceExpr = "{ThemeResource " + entry.Key + "}";
        var staticResourceExpr = "{StaticResource " + entry.Key + "}";

        var flyout = new MenuFlyout();

        var copyExprItem = new MenuFlyoutItem { Text = "Copy {ThemeResource " + entry.Key + "}" };
        copyExprItem.Click += (_, _) => CopyToClipboard(themeResourceExpr, $"Copied: {themeResourceExpr}");
        flyout.Items.Add(copyExprItem);

        var copyStaticItem = new MenuFlyoutItem { Text = "Copy {StaticResource " + entry.Key + "}" };
        copyStaticItem.Click += (_, _) => CopyToClipboard(staticResourceExpr, $"Copied: {staticResourceExpr}");
        flyout.Items.Add(copyStaticItem);

        flyout.Items.Add(new Separator());

        var copyValueItem = new MenuFlyoutItem { Text = $"Copy value ({entry.Display})" };
        copyValueItem.Click += (_, _) => CopyToClipboard(entry.Display, $"Copied value: {entry.Display}");
        flyout.Items.Add(copyValueItem);

        var copyKeyItem = new MenuFlyoutItem { Text = "Copy key only" };
        copyKeyItem.Click += (_, _) => CopyToClipboard(entry.Key, $"Copied key: {entry.Key}");
        flyout.Items.Add(copyKeyItem);

        var splitButton = new SplitButton
        {
            Content = "Copy",
            MinWidth = 96,
            Height = 30,
            Margin = new Thickness(0, 8, 12, 8),
            FontSize = 12,
            Background = TryFindResource("AccentBrush") as Brush,
            BorderBrush = TryFindResource("AccentBrush") as Brush,
            Foreground = TryFindResource("TextOnAccentFillColorSelectedTextBrush") as Brush,
            Flyout = flyout
        };

        // Primary action: copy the raw key (matches the row click for muscle memory).
        splitButton.Click += (_, _) => CopyToClipboard(entry.Key, $"Copied key: {entry.Key}");

        return splitButton;
    }

    private static string KindLabel(ResourceKind kind)
    {
        return kind switch
        {
            ResourceKind.Color => "Color",
            ResourceKind.SolidBrush => "SolidColorBrush",
            ResourceKind.GradientBrush => "GradientBrush",
            _ => kind.ToString()
        };
    }

    private void CopyToClipboard(string text, string banner)
    {
        try
        {
            Clipboard.SetText(text);
            ShowCopyBanner(banner);
        }
        catch (Exception ex)
        {
            ShowCopyBanner($"Copy failed: {ex.Message}");
        }
    }

    private void ShowCopyBanner(string message)
    {
        if (CopyBanner == null || CopyBannerText == null)
            return;

        CopyBannerText.Text = message;
        CopyBanner.Visibility = Visibility.Visible;
    }
}
