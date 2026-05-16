namespace SocialApp.User.Gateways;

public interface ICredentialsGateway
{
    string Create(string password);
    bool Matches(string password, string credentials);
    string Normalize(string credentials);
}
