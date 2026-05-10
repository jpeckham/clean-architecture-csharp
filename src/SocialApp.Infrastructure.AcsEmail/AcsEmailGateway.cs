using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.AcsEmail;

public sealed class AcsEmailGateway(IOptions<AcsEmailOptions> options) : IEmailGateway
{
    public void Send(string to, string subject, string body)
    {
        var value = options.Value;
        var client = new EmailClient(value.ConnectionString);
        client.Send(
            WaitUntil.Started,
            value.SenderAddress,
            to,
            subject,
            body);
    }
}
