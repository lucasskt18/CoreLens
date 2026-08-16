using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace CoreLens.Desktop;

internal sealed class StackOrchestrator : IAsyncDisposable
{
    private Process? _api;
    private Process? _agent;

    public async Task StartAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        var root = Paths.FindRepoRoot();
        await EnsureUiAsync(root, progress, cancellationToken);
        await StartDockerAsync(root, progress, cancellationToken);
        await WaitForDatabaseAsync(progress, cancellationToken);
        StartApi(root, progress);
        await WaitForHealthAsync(progress, cancellationToken);
        StartAgent(root, progress);
        progress.Report("Pronto.");
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            Stop(_agent);
            Stop(_api);
        });
    }

    private static async Task EnsureUiAsync(string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var index = Path.Combine(root, "src", "CoreLens.Api", "wwwroot", "index.html");
        if (File.Exists(index))
        {
            return;
        }

        progress.Report("Compilando o dashboard...");
        var script = Path.Combine(root, "scripts", "publish-ui.ps1");
        if (!File.Exists(script))
        {
            throw new InvalidOperationException("scripts/publish-ui.ps1 nao encontrado.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
            WorkingDirectory = root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Falha ao publicar a UI.");
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0 || !File.Exists(index))
        {
            throw new InvalidOperationException("Falha ao gerar o wwwroot do dashboard.");
        }
    }

    private static async Task StartDockerAsync(string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report("Verificando Docker Desktop...");
        if (!await DockerReadyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Abra o Docker Desktop e espere ficar Running. Depois abra o CoreLens de novo.");
        }

        progress.Report("Subindo TimescaleDB...");
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose up -d",
            WorkingDirectory = root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Nao consegui rodar docker compose.");
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? "docker compose up falhou."
                : error);
        }
    }

    private static async Task<bool> DockerReadyAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 15; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process is null)
                {
                    return false;
                }

                await process.WaitForExitAsync(cancellationToken);
                if (process.ExitCode == 0)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            await Task.Delay(1000, cancellationToken);
        }

        return false;
    }

    private static async Task WaitForDatabaseAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report("Esperando o banco...");
        for (var i = 0; i < 40; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "exec corelens-timescaledb pg_isready -U corelens -d corelens",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process is not null)
                {
                    await process.WaitForExitAsync(cancellationToken);
                    if (process.ExitCode == 0)
                    {
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // keep waiting
            }

            await Task.Delay(2000, cancellationToken);
        }

        throw new InvalidOperationException("TimescaleDB nao ficou pronto.");
    }

    private void StartApi(string root, IProgress<string> progress)
    {
        progress.Report("Iniciando a API...");
        var project = Path.Combine(root, "src", "CoreLens.Api", "CoreLens.Api.csproj");
        _api = StartDotnetProject(project, new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ASPNETCORE_URLS"] = Paths.AppUrl,
            ["CORELENS_DESKTOP"] = "1"
        });
    }

    private void StartAgent(string root, IProgress<string> progress)
    {
        progress.Report("Iniciando o Agent...");
        var project = Path.Combine(root, "src", "CoreLens.Agent", "CoreLens.Agent.csproj");
        _agent = StartDotnetProject(project, new Dictionary<string, string>
        {
            ["DOTNET_ENVIRONMENT"] = "Production",
            ["Agent__ApiBaseUrl"] = Paths.AppUrl
        });
    }

    private static Process StartDotnetProject(string project, IDictionary<string, string> environment)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Paths.DotnetPath(),
            Arguments = $"run --project \"{project}\" --no-launch-profile",
            WorkingDirectory = Path.GetDirectoryName(project)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var pair in environment)
        {
            psi.Environment[pair.Key] = pair.Value;
        }

        psi.Environment["DOTNET_ROOT"] = @"C:\Program Files\dotnet";

        return Process.Start(psi) ?? throw new InvalidOperationException($"Falha ao iniciar {Path.GetFileName(project)}.");
    }

    private static async Task WaitForHealthAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report("Esperando a API...");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (var i = 0; i < 60; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var response = await http.GetAsync(Paths.HealthUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception)
            {
                // API still booting
            }

            await Task.Delay(1000, cancellationToken);
        }

        throw new InvalidOperationException("A API nao respondeu em http://127.0.0.1:5080/api/health.");
    }

    private static void Stop(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (Exception)
        {
            // best-effort shutdown
        }
        finally
        {
            process.Dispose();
        }
    }
}
