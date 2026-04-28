using System.Diagnostics;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Resilience;

/// <summary>Request parameters for running a bounded subprocess.</summary>
public sealed record BoundedSubprocessRequest(
    string Executable,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout,
    int MaxMemoryMb,
    string WorkingDirectory);

/// <summary>Result of a bounded subprocess invocation.</summary>
public sealed record BoundedSubprocessResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    TimeSpan Duration,
    bool TimedOut,
    bool OutputTruncated);

/// <summary>SEC-05, SEC-18: Runs external processes with output size limits and structured argument passing.</summary>
public interface IBoundedSubprocessRunner
{
    Task<BoundedSubprocessResult> RunAsync(BoundedSubprocessRequest req, CancellationToken ct);
}

/// <summary>
/// SEC-05: Uses ArgumentList (not a raw Arguments string) so no shell injection is possible.
/// SEC-18: stdout and stderr are each truncated at 1 MB.
/// </summary>
internal sealed class BoundedSubprocessRunner : IBoundedSubprocessRunner
{
    private const int MaxOutputBytes = 1024 * 1024; // 1 MB

    public async Task<BoundedSubprocessResult> RunAsync(BoundedSubprocessRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.Executable))
            throw new ArgumentException("Executable must not be empty.", nameof(req));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(req.Timeout);

        var psi = new ProcessStartInfo
        {
            FileName = req.Executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = req.WorkingDirectory,
            CreateNoWindow = true
        };

        // SEC-05: each argument added individually — NO string concatenation
        foreach (var arg in req.Arguments)
            psi.ArgumentList.Add(arg);

        var sw = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.Start();

        var stdoutTask = ReadWithLimitAsync(process.StandardOutput, MaxOutputBytes, cts.Token);
        var stderrTask = ReadWithLimitAsync(process.StandardError, MaxOutputBytes, cts.Token);

        bool timedOut;
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            timedOut = false;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout expired — kill the process
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            timedOut = true;
        }

        string stdout;
        bool stdoutTruncated;
        string stderr;
        bool stderrTruncated;

        try
        {
            (stdout, stdoutTruncated) = await stdoutTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Timeout or external cancel interrupted the stdout read — treat as empty
            stdout = string.Empty;
            stdoutTruncated = timedOut;
        }

        try
        {
            (stderr, stderrTruncated) = await stderrTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            stderr = string.Empty;
            stderrTruncated = timedOut;
        }
        sw.Stop();

        return new BoundedSubprocessResult(
            ExitCode: timedOut ? -1 : process.ExitCode,
            Stdout: stdout,
            Stderr: stderr,
            Duration: sw.Elapsed,
            TimedOut: timedOut,
            OutputTruncated: stdoutTruncated || stderrTruncated);
    }

    private static async Task<(string Output, bool Truncated)> ReadWithLimitAsync(
        System.IO.TextReader reader, int maxBytes, CancellationToken ct)
    {
        var buffer = new char[4096];
        var sb = new System.Text.StringBuilder();
        var totalBytes = 0;
        var truncated = false;

        int read;
        while ((read = await reader.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            var chunk = new string(buffer, 0, read);
            var chunkBytes = System.Text.Encoding.UTF8.GetByteCount(chunk);

            if (totalBytes + chunkBytes > maxBytes)
            {
                truncated = true;
                break;
            }

            sb.Append(chunk);
            totalBytes += chunkBytes;
        }

        return (sb.ToString(), truncated);
    }
}
