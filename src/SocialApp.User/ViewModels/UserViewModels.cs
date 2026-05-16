namespace SocialApp.User.ViewModels;

public sealed record CreateAccountViewModel(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record RegisterAccountViewModel(bool Succeeded, string Message);
public sealed record VerifyRegistrationViewModel(bool Succeeded, string Message);
public sealed record LoginViewModel(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record LoginWithDeviceViewModel(bool Succeeded, string Message, string? Handle, string? SessionToken, bool OtpRequired);
public sealed record VerifyDeviceOtpViewModel(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record ForgotPasswordViewModel(bool Succeeded, string Message, string? ResetToken);
public sealed record ChangePasswordViewModel(bool Succeeded, string Message);
public sealed record RequestPasswordResetViewModel(bool Succeeded, string Message);
public sealed record ResetPasswordViewModel(bool Succeeded, string Message);
public sealed record UserSummaryViewModel(string Handle, string DisplayName);
public sealed record SearchUserViewModel(IReadOnlyList<UserSummaryViewModel> Users);
public sealed record ProfileImageViewModel(Guid AssetId, string StorageKey, string ContentType, long ByteLength, int? Width, int? Height, DateTimeOffset UploadedAt);
public sealed record BeginProfileImageUploadViewModel(bool Succeeded, string Message, Guid? AssetId, string? UploadUrl);
public sealed record CompleteProfileImageUploadViewModel(bool Succeeded, string Message, ProfileImageViewModel? ProfileImage);
public sealed record RemoveProfileImageViewModel(bool Succeeded, string Message);
public sealed record ViewUserQuotedPostSummaryViewModel(Guid Id, string AuthorHandle, string Content, DateTimeOffset CreatedAt, IReadOnlyList<ViewUserPostMediaSummaryViewModel>? Media = null);
public sealed record ViewUserPostMediaSummaryViewModel(
    Guid AssetId,
    string Kind,
    string ContentType,
    long ByteLength,
    int? Width,
    int? Height,
    long? DurationMs,
    int SortOrder,
    string? ThumbnailKey,
    string? AltText,
    string? MediaUrl = null);
public sealed record ViewUserPostSummaryViewModel(
    Guid Id,
    string AuthorHandle,
    string Content,
    Guid? ParentPostId,
    Guid? OriginalPostId,
    DateTimeOffset CreatedAt,
    int LikeCount,
    bool LikedByCurrentReader,
    int RepostCount,
    bool RepostedByCurrentReader,
    ViewUserQuotedPostSummaryViewModel? QuotedPost,
    IReadOnlyList<ViewUserPostMediaSummaryViewModel>? Media = null);
public sealed record ViewUserViewModel(bool Succeeded, string Message, string? Handle, string? DisplayName, ProfileImageViewModel? ProfileImage, IReadOnlyList<ViewUserPostSummaryViewModel> Posts);
