namespace SocialApp.User.ResponseModels;

public static class UserMessageKeys
{
    public const string AccountCreated = "ACCOUNT_CREATED";
    public const string AccountAlreadyExists = "ACCOUNT_ALREADY_EXISTS";
    public const string VerificationCodeSent = "VERIFICATION_CODE_SENT";
    public const string VerificationCodeInvalid = "VERIFICATION_CODE_INVALID";
    public const string AccountVerified = "ACCOUNT_VERIFIED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string LoggedIn = "LOGGED_IN";
    public const string OneTimeCodeSent = "ONE_TIME_CODE_SENT";
    public const string OneTimeCodeInvalid = "ONE_TIME_CODE_INVALID";
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
    public const string ResetTokenInvalid = "RESET_TOKEN_INVALID";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordResetLinkSentIfAccountExists = "PASSWORD_RESET_LINK_SENT_IF_ACCOUNT_EXISTS";
    public const string ResetLinkInvalidOrExpired = "RESET_LINK_INVALID_OR_EXPIRED";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string UserFound = "USER_FOUND";
    public const string ProfileImageUploadReserved = "PROFILE_IMAGE_UPLOAD_RESERVED";
    public const string ProfileImageUploadCompleted = "PROFILE_IMAGE_UPLOAD_COMPLETED";
    public const string ProfileImageUploadNotFound = "PROFILE_IMAGE_UPLOAD_NOT_FOUND";
    public const string ProfileImageRemoved = "PROFILE_IMAGE_REMOVED";
}

public sealed record CreateAccountResponse(bool Succeeded, string MessageKey, string? Handle, string? SessionToken);
public sealed record RegisterAccountResponse(bool Succeeded, string MessageKey);
public sealed record VerifyRegistrationResponse(bool Succeeded, string MessageKey);
public sealed record LoginResponse(bool Succeeded, string MessageKey, string? Handle, string? SessionToken);
public sealed record LoginWithDeviceResponse(bool Succeeded, string MessageKey, string? Handle, string? SessionToken, bool OtpRequired);
public sealed record VerifyDeviceOtpResponse(bool Succeeded, string MessageKey, string? Handle, string? SessionToken);
public sealed record ForgotPasswordResponse(bool Succeeded, string MessageKey, string? ResetToken);
public sealed record ChangePasswordResponse(bool Succeeded, string MessageKey);
public sealed record RequestPasswordResetResponse(bool Succeeded, string MessageKey);
public sealed record ResetPasswordResponse(bool Succeeded, string MessageKey);
public sealed record UserSummaryResponse(string Handle, string DisplayName);
public sealed record SearchUserResponse(IReadOnlyList<UserSummaryResponse> Users);
public sealed record ProfileImageResponse(Guid AssetId, string StorageKey, string ContentType, long ByteLength, int? Width, int? Height, DateTimeOffset UploadedAt);
public sealed record ProfileImageUploadResponse(Guid AssetId, string StorageKey, string UploadUrl);
public sealed record BeginProfileImageUploadResponse(bool Succeeded, string MessageKey, ProfileImageUploadResponse? Upload);
public sealed record CompleteProfileImageUploadResponse(bool Succeeded, string MessageKey, ProfileImageResponse? ProfileImage);
public sealed record RemoveProfileImageResponse(bool Succeeded, string MessageKey);
public sealed record ViewUserQuotedPostSummaryResponse(Guid Id, string AuthorHandle, string Content, DateTimeOffset CreatedAt);
public sealed record ViewUserPostSummaryResponse(
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
    ViewUserQuotedPostSummaryResponse? QuotedPost);
public sealed record ViewUserResponse(bool Succeeded, string MessageKey, string? Handle, string? DisplayName, ProfileImageResponse? ProfileImage, IReadOnlyList<ViewUserPostSummaryResponse> Posts);
