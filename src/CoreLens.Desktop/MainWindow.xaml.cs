using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace CoreLens.Desktop;

public partial class MainWindow : Window
{
    private bool _forceClose;
    private MiniWindow? _mini;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;
    }

    public async Task ShowDashboardAsync()
    {
        Show();
        Activate();
        WindowState = WindowState.Normal;

        if (WebView.CoreWebView2 is null)
        {
            await WebView.EnsureCoreWebView2Async();
            var core = WebView.CoreWebView2 ?? throw new InvalidOperationException("WebView2 nao iniciou.");
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.WebMessageReceived += OnWebMessage;
        }

        WebView.Source = new Uri(Paths.AppUrl);
        StatusText.Visibility = Visibility.Collapsed;
        WebView.Visibility = Visibility.Visible;
    }

    public void ShowMini()
    {
        if (_mini is null || !_mini.IsLoaded)
        {
            _mini = new MiniWindow();
            _mini.Closed += (_, _) => _mini = null;
        }

        _mini.Show();
        _mini.Activate();
    }

    public void ForceClose()
    {
        _forceClose = true;
        _mini?.Close();
        Close();
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        if (message.Contains("open-mini", StringComparison.OrdinalIgnoreCase))
        {
            Dispatcher.Invoke(ShowMini);
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_forceClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
