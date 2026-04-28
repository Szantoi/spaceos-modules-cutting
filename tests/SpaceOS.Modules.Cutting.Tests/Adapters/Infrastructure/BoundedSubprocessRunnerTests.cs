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
        var req = new BoundedSubprocessRequest(
            Executable: "/bin/echo",
            Arguments: new[] { "hello world" },
            Timeout: TimeSpan.FromSeconds(5),
            MaxMemoryMb: 64,
            WorkingDirectory: "/tmp");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("hello world");
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_NonZeroExitCode_CapturedCorrectly()
    {
        var req = new BoundedSubprocessRequest(
            Executable: "/bin/bash",
            Arguments: new[] { "-c", "exit 42" },
            Timeout: TimeSpan.FromSeconds(5),
            MaxMemoryMb: 64,
            WorkingDirectory: "/tmp");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.ExitCode.Should().Be(42);
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_StderrCaptured()
    {
        var req = new BoundedSubprocessRequest(
            Executable: "/bin/bash",
            Arguments: new[] { "-c", "echo err >&2" },
            Timeout: TimeSpan.FromSeconds(5),
            MaxMemoryMb: 64,
            WorkingDirectory: "/tmp");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.Stderr.Should().Contain("err");
    }

    [Fact]
    public async Task RunAsync_Timeout_SetsTimedOut()
    {
        var req = new BoundedSubprocessRequest(
            Executable: "/bin/sleep",
            Arguments: new[] { "60" },
            Timeout: TimeSpan.FromMilliseconds(100),
            MaxMemoryMb: 64,
            WorkingDirectory: "/tmp");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.TimedOut.Should().BeTrue();
        result.ExitCode.Should().Be(-1);
    }

    [Fact]
    public async Task RunAsync_DurationIsRecorded()
    {
        var req = new BoundedSubprocessRequest(
            Executable: "/bin/echo",
            Arguments: new[] { "ok" },
            Timeout: TimeSpan.FromSeconds(5),
            MaxMemoryMb: 64,
            WorkingDirectory: "/tmp");

        var result = await _runner.RunAsync(req, CancellationToken.None);

        result.Duration.Should().BePositive();
    }

    [Fact]
    public void RunAsync_EmptyExecutable_ThrowsArgumentException()
    {
        var req = new BoundedSubprocessRequest(
            Executable: "",
            Arguments: Array.Empty<string>(),
            Timeout: TimeSpan.FromSeconds(5),
            MaxMemoryMb: 64,
            WorkingDirectory: "/tmp");

        var act = async () => await _runner.RunAsync(req, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void RunAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = async () => await _runner.RunAsync(null!, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentNullException>();
    }
}
