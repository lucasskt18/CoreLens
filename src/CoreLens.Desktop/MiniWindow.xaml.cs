using System.Windows;

namespace CoreLens.Desktop;

public partial class MiniWindow : Window
{
    public MiniWindow()
    {
        InitializeComponent();
        Left = SystemParameters.WorkArea.Right - Width - 24;
        Top = SystemParameters.WorkArea.Top + 24;
        Loaded += async (_, _) =>
        {
            await WebView.EnsureCoreWebView2Async();
            WebView.Source = new Uri(Paths.PopupUrl);
        };
    }
}
