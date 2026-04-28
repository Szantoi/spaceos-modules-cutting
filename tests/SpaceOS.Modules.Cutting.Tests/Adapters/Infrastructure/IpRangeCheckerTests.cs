using System.Net;
using FluentAssertions;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Infrastructure;

public class IpRangeCheckerTests
{
    private readonly IpRangeChecker _checker = new();

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.2")]
    [InlineData("127.255.255.255")]
    public void IsBlocked_Loopback_ReturnsTrue(string ip)
    {
        _checker.IsBlocked(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("10.1.2.3")]
    public void IsBlocked_PrivateClassA_ReturnsTrue(string ip)
    {
        _checker.IsBlocked(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Theory]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("172.20.10.5")]
    public void IsBlocked_PrivateClassB_ReturnsTrue(string ip)
    {
        _checker.IsBlocked(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Theory]
    [InlineData("192.168.0.1")]
    [InlineData("192.168.1.100")]
    [InlineData("192.168.255.255")]
    public void IsBlocked_PrivateClassC_ReturnsTrue(string ip)
    {
        _checker.IsBlocked(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Theory]
    [InlineData("169.254.0.1")]
    [InlineData("169.254.169.254")]  // AWS metadata
    [InlineData("169.254.1.1")]
    public void IsBlocked_LinkLocal_ReturnsTrue(string ip)
    {
        _checker.IsBlocked(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("93.184.216.34")]
    [InlineData("172.32.0.1")]  // just outside 172.16/12
    [InlineData("172.15.255.255")]  // just below 172.16
    public void IsBlocked_PublicIp_ReturnsFalse(string ip)
    {
        _checker.IsBlocked(IPAddress.Parse(ip)).Should().BeFalse();
    }

    [Fact]
    public void IsBlocked_IPv6LinkLocal_ReturnsTrue()
    {
        // fe80::/10
        var ip = IPAddress.Parse("fe80::1");
        _checker.IsBlocked(ip).Should().BeTrue();
    }

    [Fact]
    public void IsBlocked_IPv6Public_ReturnsFalse()
    {
        var ip = IPAddress.Parse("2001:4860:4860::8888");
        _checker.IsBlocked(ip).Should().BeFalse();
    }

    [Fact]
    public void IsBlocked_NullArgument_Throws()
    {
        var act = () => _checker.IsBlocked(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
