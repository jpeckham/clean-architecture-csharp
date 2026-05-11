namespace SocialApp.User.RequestModels;

public sealed record CreateAccountRequest(string DisplayName, string Handle, string Email, string Password);
public sealed record RegisterAccountRequest(string DisplayName, string Handle, string Email, string Password);
public sealed record VerifyRegistrationRequest(string Email, string Code);
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginWithDeviceRequest(string Email, string Password, string DeviceId);
public sealed record VerifyDeviceOtpRequest(string Handle, string DeviceId, string Code, bool RememberDevice);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ChangePasswordRequest(string ResetToken, string NewPassword);
public sealed record RequestPasswordResetRequest(string Email, string ResetBaseUrl);
public sealed record ResetPasswordRequest(string ResetToken, string NewPassword);
public sealed record SearchUserRequest(string Query);
public sealed record ViewUserRequest(string Handle);
