using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;

namespace SocialApp.Api.Endpoints;

public sealed class UserGatewayAccountHandleAdapter(IUserGateway users) : IAccountHandleGateway
{
    public bool Exists(string handle) =>
        users.FindByHandle(SocialPost.NormalizeHandle(handle)) is not null;
}
