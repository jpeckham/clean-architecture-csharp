using SocialApp.Api.Endpoints;
using SocialApp.Infrastructure.AcsEmail;
using SocialApp.Infrastructure.CosmosMongo;
using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

if (!string.IsNullOrWhiteSpace(builder.Configuration["CosmosMongo:ConnectionString"]))
{
    builder.Services.AddCosmosMongoInfrastructure(builder.Configuration);
}
else
{
    builder.Services.AddSingleton<IUserGateway, InMemoryUserGateway>();
    builder.Services.AddSingleton<ISessionGateway, InMemorySessionGateway>();
    builder.Services.AddSingleton<IPostGateway, InMemoryPostGateway>();
    builder.Services.AddSingleton<IClock, SystemClock>();
    builder.Services.AddSingleton<IPendingRegistrationGateway, InMemoryPendingRegistrationGateway>();
    builder.Services.AddSingleton<IVerificationCodeGateway, InMemoryVerificationCodeGateway>();
    builder.Services.AddSingleton<IRememberedDeviceGateway, InMemoryRememberedDeviceGateway>();
    builder.Services.AddSingleton<IPasswordResetTokenGateway, InMemoryPasswordResetTokenGateway>();
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["AcsEmail:ConnectionString"]))
{
    builder.Services.AddAcsEmailInfrastructure(builder.Configuration);
}
else
{
    builder.Services.AddSingleton<IEmailGateway, InMemoryEmailGateway>();
}

var app = builder.Build();

app.UseCors();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapSocialAppSlice();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/emails", (IEmailGateway emailGateway) =>
        emailGateway is InMemoryEmailGateway inMemory
            ? Results.Ok(new { emails = inMemory.Sent })
            : Results.NotFound(new { message = "The development email outbox is only available when using the in-memory email gateway." }));
}

app.Run();

public partial class Program;
