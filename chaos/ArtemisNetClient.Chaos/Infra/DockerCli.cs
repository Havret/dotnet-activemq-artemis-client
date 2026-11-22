using System.Diagnostics;

namespace ActiveMQ.Artemis.Client.Chaos;

public static class DockerCli
{
    public static async Task<string> RunAsync(CancellationToken ct, params string[] args)
    {
        var startInfo = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker {string.Join(' ', args)} failed with {process.ExitCode}: {(await stderr).Trim()}");
        }

        return (await stdout).Trim();
    }

    public static async Task<bool> IsRunningAsync(string container, CancellationToken ct)
    {
        return await RunAsync(ct, "inspect", "-f", "{{.State.Running}}", container) == "true";
    }
}
