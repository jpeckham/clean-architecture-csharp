namespace SocialApp.Api.Contracts;

public sealed record CreatePostHttpRequest(string Content, IReadOnlyList<Guid>? MediaAssetIds = null);
public sealed record ReplyPostHttpRequest(string? Content);
public sealed record RepostPostHttpRequest(string? Content);
public sealed record BeginPostMediaUploadHttpRequest(
    string Kind,
    string ContentType,
    long ByteLength,
    int? Width = null,
    int? Height = null,
    long? DurationMs = null,
    string? AltText = null);
