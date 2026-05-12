using SocialApp.User.RequestModels;
using SocialApp.User.UseCases;

namespace SocialApp.User.Controllers;

public sealed class CreateAccountController(ICreateAccountInputBoundary input)
{
    public void Create(string displayName, string handle, string email, string password) => input.Handle(new(displayName, handle, email, password));
}

public sealed class RegisterAccountController(IRegisterAccountInputBoundary input)
{
    public void Register(string displayName, string handle, string email, string password) => input.Handle(new(displayName, handle, email, password));
}

public sealed class VerifyRegistrationController(IVerifyRegistrationInputBoundary input)
{
    public void Verify(string email, string code) => input.Handle(new(email, code));
}

public sealed class LoginController(ILoginInputBoundary input)
{
    public void Login(string email, string password) => input.Handle(new(email, password));
}

public sealed class LoginWithDeviceController(ILoginWithDeviceInputBoundary input)
{
    public void Login(string email, string password, string deviceId) => input.Handle(new(email, password, deviceId));
}

public sealed class VerifyDeviceOtpController(IVerifyDeviceOtpInputBoundary input)
{
    public void Verify(string handle, string deviceId, string code, bool rememberDevice) => input.Handle(new(handle, deviceId, code, rememberDevice));
}

public sealed class ForgotPasswordController(IForgotPasswordInputBoundary input)
{
    public void RequestReset(string email) => input.Handle(new ForgotPasswordRequest(email));
}

public sealed class ChangePasswordController(IChangePasswordInputBoundary input)
{
    public void ChangePassword(string resetToken, string newPassword) => input.Handle(new(resetToken, newPassword));
}

public sealed class RequestPasswordResetController(IRequestPasswordResetInputBoundary input)
{
    public void RequestReset(string email, string resetBaseUrl) => input.Handle(new(email, resetBaseUrl));
}

public sealed class ResetPasswordController(IResetPasswordInputBoundary input)
{
    public void Reset(string resetToken, string newPassword) => input.Handle(new(resetToken, newPassword));
}

public sealed class SearchUserController(ISearchUserInputBoundary input)
{
    public void Search(string query) => input.Handle(new(query));
}

public sealed class ViewUserController(IViewUserInputBoundary input)
{
    public void View(string handle, string readerHandle, int recentPostLimit = 20) => input.Handle(new(handle, readerHandle, recentPostLimit));
}

public sealed class BeginProfileImageUploadController(IBeginProfileImageUploadInputBoundary input)
{
    public void Begin(string ownerHandle, string contentType, long byteLength) => input.Handle(new(ownerHandle, contentType, byteLength));
}

public sealed class CompleteProfileImageUploadController(ICompleteProfileImageUploadInputBoundary input)
{
    public void Complete(string currentUserHandle, string profileHandle, Guid assetId, int? width, int? height) =>
        input.Handle(new(currentUserHandle, profileHandle, assetId, width, height));
}

public sealed class RemoveProfileImageController(IRemoveProfileImageInputBoundary input)
{
    public void Remove(string currentUserHandle, string profileHandle) => input.Handle(new(currentUserHandle, profileHandle));
}
