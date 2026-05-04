using Jalium.UI.Controls;
using Jalium.UI.Controls.Editor;
using Jalium.UI.Notifications;

namespace Jalium.UI.Gallery.Modules.Main.Views.Pages;

/// <summary>
/// Code-behind for SystemNotificationPage.jalxaml demonstrating OS-level system notifications.
/// </summary>
public partial class SystemNotificationPage : Page
{
    public SystemNotificationPage()
    {
        InitializeComponent();

        var mgr = SystemNotificationManager.Current;
        Exception? initFailure = TryInitializeManager(mgr);

        bool supported = mgr.IsSupported;
        UpdateSupportText(supported, initFailure);
        ConfigureActionButtons(mgr, supported);

        if (ExpirationSlider != null)
            ExpirationSlider.ValueChanged += OnExpirationChanged;

        LoadCodeExamples();
    }

    private static Exception? TryInitializeManager(SystemNotificationManager mgr)
    {
        try
        {
            mgr.Initialize("Jalium.UI.Gallery", "Jalium.UI Gallery");
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private void UpdateSupportText(bool supported, Exception? initFailure)
    {
        if (SupportedText == null) return;

        if (supported)
        {
            SupportedText.Text = "System notifications are supported on this platform.";
            SupportedText.Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(0x4C, 0xAF, 0x50));
            return;
        }

        var detail = initFailure?.Message;
        SupportedText.Text = string.IsNullOrEmpty(detail)
            ? "System notifications are NOT supported on this platform."
            : $"System notifications are NOT available on this platform. Reason: {detail}";
        SupportedText.Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(0xF4, 0x43, 0x36));
    }

    private void ConfigureActionButtons(SystemNotificationManager mgr, bool supported)
    {
        if (SendBasicButton != null)
        {
            SendBasicButton.Click += OnSendBasicClick;
            SendBasicButton.IsEnabled = supported;
        }

        if (LowPriorityButton != null)
        {
            LowPriorityButton.Click += (s, e) => SendWithPriority(NotificationPriority.Low);
            LowPriorityButton.IsEnabled = supported;
        }
        if (NormalPriorityButton != null)
        {
            NormalPriorityButton.Click += (s, e) => SendWithPriority(NotificationPriority.Normal);
            NormalPriorityButton.IsEnabled = supported;
        }
        if (HighPriorityButton != null)
        {
            HighPriorityButton.Click += (s, e) => SendWithPriority(NotificationPriority.High);
            HighPriorityButton.IsEnabled = supported;
        }

        if (SendActionButton != null)
        {
            SendActionButton.Click += OnSendActionClick;
            SendActionButton.IsEnabled = supported;
        }

        if (SendAdvancedButton != null)
        {
            SendAdvancedButton.Click += OnSendAdvancedClick;
            SendAdvancedButton.IsEnabled = supported;
        }
        if (RemoveTagButton != null)
        {
            RemoveTagButton.Click += OnRemoveTagClick;
            RemoveTagButton.IsEnabled = supported;
        }
        if (ClearAllButton != null)
        {
            ClearAllButton.Click += (s, e) => mgr.ClearAll();
            ClearAllButton.IsEnabled = supported;
        }
    }

    private void OnSendBasicClick(object? sender, EventArgs e)
    {
        var title = BasicTitleBox?.Text ?? "Hello";
        var body = BasicBodyBox?.Text ?? "Notification body";
        SystemNotificationManager.Current.Show(title, body);
    }

    private void SendWithPriority(NotificationPriority priority)
    {
        var content = new NotificationContent
        {
            Title = $"{priority} Priority",
            Body = $"This notification was sent with {priority} priority.",
            Priority = priority
        };
        SystemNotificationManager.Current.Show(content);
    }

    private void OnSendActionClick(object? sender, EventArgs e)
    {
        var content = new NotificationContent
        {
            Title = "Download Complete",
            Body = "report_2026.pdf has been downloaded successfully.",
            Actions =
            {
                new NotificationAction("open", "Open File"),
                new NotificationAction("folder", "Show in Folder")
            }
        };

        var handle = SystemNotificationManager.Current.Show(content);
        handle.Activated += (s, args) =>
        {
            var action = args.ActionId ?? "body";
            Dispatcher.Invoke(() =>
            {
                if (ActionResultText != null)
                    ActionResultText.Text = $"Activated: ActionId = \"{action}\"";
            });
        };
        handle.Dismissed += (s, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (ActionResultText != null)
                    ActionResultText.Text = $"Dismissed: Reason = {args.Reason}";
            });
        };
    }

    private void OnSendAdvancedClick(object? sender, EventArgs e)
    {
        var seconds = ExpirationSlider?.Value ?? 10;
        var content = new NotificationContent
        {
            Title = "Tagged Notification",
            Body = $"Tag: {TagBox?.Text}, Group: {GroupBox?.Text}",
            Tag = TagBox?.Text,
            Group = GroupBox?.Text,
            Silent = SilentCheckBox?.IsChecked ?? false,
            Expiration = seconds > 0 ? TimeSpan.FromSeconds(seconds) : null
        };
        SystemNotificationManager.Current.Show(content);
    }

    private void OnRemoveTagClick(object? sender, EventArgs e)
    {
        var tag = TagBox?.Text;
        var group = GroupBox?.Text;
        if (!string.IsNullOrEmpty(tag))
            SystemNotificationManager.Current.Remove(tag, string.IsNullOrEmpty(group) ? null : group);
    }

    private void OnExpirationChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ExpirationText != null)
            ExpirationText.Text = e.NewValue > 0 ? $"{e.NewValue:F0}s" : "Default";
    }

    private const string XamlExample = @"<!-- System notifications are code-only (no XAML element).
     Use SystemNotificationManager from code-behind. -->
<Button x:Name=""SendButton"" Content=""Send Notification""/>
<Button x:Name=""ClearButton"" Content=""Clear All""/>";

    private const string CSharpExample = @"// Initialize once at app startup
var mgr = SystemNotificationManager.Current;
mgr.Initialize(""MyApp"", ""My Application"");

// Simple notification
mgr.Show(""Hello"", ""This is a system notification."");

// Notification with full content
var content = new NotificationContent
{
    Title = ""Download Complete"",
    Body = ""report.pdf has been downloaded."",
    Priority = NotificationPriority.Normal,
    Silent = false,
    Tag = ""download-1"",
    Group = ""downloads"",
    Expiration = TimeSpan.FromSeconds(10),
    Actions =
    {
        new NotificationAction(""open"", ""Open File""),
        new NotificationAction(""folder"", ""Show in Folder"")
    }
};

// Show and handle activation
var handle = mgr.Show(content);
handle.Activated += (s, args) =>
{
    if (args.ActionId == ""open"")
        Process.Start(filePath);
};
handle.Dismissed += (s, args) =>
{
    Debug.WriteLine($""Dismissed: {args.Reason}"");
};

// Remove specific notification by tag
mgr.Remove(""download-1"", ""downloads"");

// Clear all notifications
mgr.ClearAll();

// Check platform support
if (mgr.IsSupported)
    mgr.Show(""Supported"", ""Notifications work!"");";

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
}
