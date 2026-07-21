using FluentAssertions;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Resilience;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Infrastructure;

public class BoundedSubprocessRunnerTests
{
    private readonly BoundedSubprocessRunner _runner = new();

    [Fact]
    public async Task RunAsync_EchoCommand_CapturesStdout()
    {
        var req = CreateShellRequest(
            windowsCommand: "echo hello world",
            unixCommand: "printf 'hello world\\n'");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("hello world");
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_NonZeroExitCode_CapturedCorrectly()
    {
        var req = CreateShellRequest(
            windowsCommand: "exit /b 42",
            unixCommand: "exit 42");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.ExitCode.Should().Be(42);
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_StderrCaptured()
    {
        var req = CreateShellRequest(
            windowsCommand: "echo err 1>&2",
            unixCommand: "printf 'err\\n' >&2");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.Stderr.Should().Contain("err");
    }

    [Fact]
    public async Task RunAsync_Timeout_SetsTimedOut()
    {
        var req = CreateShellRequest(
            windowsCommand: "ping 127.0.0.1 -n 60 >NUL",
            unixCommand: "sleep 60",
            timeout: TimeSpan.FromMilliseconds(100));

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.TimedOut.Should().BeTrue();
        result.ExitCode.Should().Be(-1);
    }

    [Fact]
    public async Task RunAsync_DurationIsRecorded()
    {
        var req = CreateShellRequest(
            windowsCommand: "echo ok",
            unixCommand: "printf 'ok\\n'");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.Duration.Should().BePositive();
    }

    [Fact]
    public async Task RunAsync_EmptyExecutable_ThrowsArgumentException()
    {
        var req = new BoundedSubprocessRequest(
            Executable: "",
            Arguments: Array.Empty<string>(),
            Timeout: TimeSpan.FromSeconds(5),
            MaxMemoryMb: 64,
            WorkingDirectory: Path.GetTempPath());

        var act = () => _runner.RunAsync(req, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _runner.RunAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static BoundedSubprocessRequest CreateShellRequest(
        string windowsCommand,
        string unixCommand,
        TimeSpan? timeout = null)
    {
        string executable;
        IReadOnlyList<string> arguments;

        if (OperatingSystem.IsWindows())
        {
            executable = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = new[] { "/d", "/c", windowsCommand };
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            executable = "/bin/sh";
            arguments = new[] { "-c", unixCommand };
        }
        else
        {
            throw new PlatformNotSupportedException(
                "The subprocess test fixture supports Windows, Linux, and macOS.");
        }

        return new BoundedSubprocessRequest(
            Executable: executable,
            Arguments: arguments,
            Timeout: timeout ?? TimeSpan.FromSeconds(5),
            MaxMemoryMb: 64,
            WorkingDirectory: Path.GetTempPath());
    }
}
