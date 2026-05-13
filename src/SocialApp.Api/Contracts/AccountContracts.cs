namespace SocialApp.Api.Contracts;

public sealed record CreateAccountHttpRequest(string DisplayName, string Handle, string Email, string Password);
public sealed record RegisterAccountHttpRequest(string DisplayName, string Handle, string Email, string Password);
public sealed record VerifyRegistrationHttpRequest(string Email, string Code);
public sealed record LoginHttpRequest(string Email, string Password);
public sealed record LoginWithDeviceHttpRequest(string Email, string Password, string DeviceId);
public sealed record VerifyDeviceOtpHttpRequest(string Handle, string DeviceId, string Code, bool RememberDevice);
public sealed record RequestPasswordResetHttpRequest(string Email);
public sealed record ResetPasswordHttpRequest(string Token, string NewPassword);
public sealed record BeginProfileImageUploadHttpRequest(string ContentType, long ByteLength);
public sealed record CompleteProfileImageUploadHttpRequest(Guid AssetId, int? Width = null, int? Height = null);
