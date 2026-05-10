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
public sealed record ViewUserViewModel(bool Succeeded, string Message, string? Handle, string? DisplayName);
