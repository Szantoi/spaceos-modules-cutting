using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SpaceOS.Modules.Cutting.Infrastructure.Services;

internal interface ISmtpMessageSender
{
    Task SendAsync(MimeMessage message, CancellationToken ct);
}

internal sealed class MailKitSmtpMessageSender : ISmtpMessageSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public MailKitSmtpMessageSender(
        string host,
        int port,
        string username,
        string password)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
    }

    public async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, ct)
            .ConfigureAwait(false);
        await client.AuthenticateAsync(_username, _password, ct).ConfigureAwait(false);
        await client.SendAsync(message, ct).ConfigureAwait(false);
        await client.DisconnectAsync(true, ct).ConfigureAwait(false);
    }
}
