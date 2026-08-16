using System.Threading;
using System.Windows;

namespace CoreLens.Desktop;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private CancellationTokenSource? _showLoop;
    private StackOrchestrator? _stack;
    private TrayService? _tray;
    private MainWindow? _main;

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, @"Local\CoreLens.Desktop", out var created);
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\CoreLens.Desktop.Show");

        if (!created)
        {
            _showEvent.Set();
            Shutdown();
            return;
        }

        _main = new MainWindow();
        _main.Show();
        _tray = new TrayService(
            () => _ = _main.ShowDashboardAsync(),
            () => _main.ShowMini(),
            ExitApp);

        _showLoop = new CancellationTokenSource();
        _ = Task.Run(() => ListenForSecondInstance(_showLoop.Token));

        _stack = new StackOrchestrator();
        var progress = new Progress<string>(status => _main.SetStatus(status));

        try
        {
            await _stack.StartAsync(progress, CancellationToken.None);
            await _main.ShowDashboardAsync();
        }
        catch (Exception ex)
        {
            _main.SetStatus(ex.Message);
        }
    }

    private void ListenForSecondInstance(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_showEvent?.WaitOne(500) == true)
            {
                Dispatcher.Invoke(() => _ = _main?.ShowDashboardAsync());
            }
        }
    }

    private async void ExitApp()
    {
        _showLoop?.Cancel();
        if (_stack is not null)
        {
            await _stack.DisposeAsync();
        }

        _tray?.Dispose();
        _main?.ForceClose();
        Shutdown();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _showLoop?.Cancel();
        _tray?.Dispose();
        _mutex?.Dispose();
        _showEvent?.Dispose();
        base.OnExit(e);
    }
}
