namespace SocialApp.User.ResponseModels;

public sealed record CreateAccountResponse(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record RegisterAccountResponse(bool Succeeded, string Message);
public sealed record VerifyRegistrationResponse(bool Succeeded, string Message);
public sealed record LoginResponse(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record LoginWithDeviceResponse(bool Succeeded, string Message, string? Handle, string? SessionToken, bool OtpRequired);
public sealed record VerifyDeviceOtpResponse(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record ForgotPasswordResponse(bool Succeeded, string Message, string? ResetToken);
public sealed record ChangePasswordResponse(bool Succeeded, string Message);
public sealed record RequestPasswordResetResponse(bool Succeeded, string Message);
public sealed record ResetPasswordResponse(bool Succeeded, string Message);
public sealed record UserSummaryResponse(string Handle, string DisplayName);
public sealed record SearchUserResponse(IReadOnlyList<UserSummaryResponse> Users);
public sealed record ViewUserResponse(bool Succeeded, string Message, string? Handle, string? DisplayName);
