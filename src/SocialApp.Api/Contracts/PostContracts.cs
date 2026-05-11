namespace SocialApp.Api.Contracts;

public sealed record CreatePostHttpRequest(string Content);
public sealed record RepostPostHttpRequest(string? Content);
