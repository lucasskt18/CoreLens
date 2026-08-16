using System.IO;

namespace CoreLens.Desktop;

internal static class Paths
{
    public const string AppUrl = "http://127.0.0.1:5080";
    public const string HealthUrl = "http://127.0.0.1:5080/api/health";
    public const string PopupUrl = "http://127.0.0.1:5080/popup";

    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")) &&
                File.Exists(Path.Combine(dir.FullName, "CoreLens.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Nao achei a pasta do CoreLens (docker-compose.yml).");
    }

    public static string DotnetPath()
    {
        var preferred = @"C:\Program Files\dotnet\dotnet.exe";
        return File.Exists(preferred) ? preferred : "dotnet";
    }
}
