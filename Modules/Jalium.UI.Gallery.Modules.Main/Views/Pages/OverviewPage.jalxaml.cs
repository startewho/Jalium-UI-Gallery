using Jalium.UI.Controls;

namespace Jalium.UI.Gallery.Modules.Main.Views.Pages;

/// <summary>
/// Landing page that greets the user before the main Gallery shell takes over.
/// The layout lives entirely in <c>OverviewPage.jalxaml</c> using framework-
/// native controls (Button, CheckBox, RadioButton, Slider, ProgressBar,
/// TextBox, PasswordBox, ComboBox, AutoCompleteBox, NumberBox, ToggleSwitch,
/// SplitButton). This code-behind only forwards the central "Enter Gallery"
/// click to MainWindow via <see cref="EnterRequested"/>.
/// </summary>
public partial class OverviewPage : Page
{
    /// <summary>
    /// Raised when the user clicks the central "Enter Gallery" button.
    /// MainWindow handles this by fading the overlay out and fading the
    /// navigation shell in.
    /// </summary>
    public event EventHandler? EnterRequested;

    public OverviewPage()
    {
        InitializeComponent();
    }

    private void OnEnterButtonClick(object? sender, EventArgs e)
    {
        EnterRequested?.Invoke(this, EventArgs.Empty);
    }
}
