using SocialApp.User.ResponseModels;
using SocialApp.User.UseCases;
using SocialApp.User.ViewModels;

namespace SocialApp.User.Presenters;

public sealed class CreateAccountPresenter : ICreateAccountOutputBoundary
{
    public CreateAccountViewModel? ViewModel { get; private set; }
    public void Present(CreateAccountResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Handle, response.SessionToken);
}

public sealed class RegisterAccountPresenter : IRegisterAccountOutputBoundary
{
    public RegisterAccountViewModel? ViewModel { get; private set; }
    public void Present(RegisterAccountResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class VerifyRegistrationPresenter : IVerifyRegistrationOutputBoundary
{
    public VerifyRegistrationViewModel? ViewModel { get; private set; }
    public void Present(VerifyRegistrationResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class LoginPresenter : ILoginOutputBoundary
{
    public LoginViewModel? ViewModel { get; private set; }
    public void Present(LoginResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Handle, response.SessionToken);
}

public sealed class LoginWithDevicePresenter : ILoginWithDeviceOutputBoundary
{
    public LoginWithDeviceViewModel? ViewModel { get; private set; }
    public void Present(LoginWithDeviceResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Handle, response.SessionToken, response.OtpRequired);
}

public sealed class VerifyDeviceOtpPresenter : IVerifyDeviceOtpOutputBoundary
{
    public VerifyDeviceOtpViewModel? ViewModel { get; private set; }
    public void Present(VerifyDeviceOtpResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Handle, response.SessionToken);
}

public sealed class ForgotPasswordPresenter : IForgotPasswordOutputBoundary
{
    public ForgotPasswordViewModel? ViewModel { get; private set; }
    public void Present(ForgotPasswordResponse response) => ViewModel = new(response.Succeeded, response.Message, response.ResetToken);
}

public sealed class ChangePasswordPresenter : IChangePasswordOutputBoundary
{
    public ChangePasswordViewModel? ViewModel { get; private set; }
    public void Present(ChangePasswordResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class RequestPasswordResetPresenter : IRequestPasswordResetOutputBoundary
{
    public RequestPasswordResetViewModel? ViewModel { get; private set; }
    public void Present(RequestPasswordResetResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class ResetPasswordPresenter : IResetPasswordOutputBoundary
{
    public ResetPasswordViewModel? ViewModel { get; private set; }
    public void Present(ResetPasswordResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class SearchUserPresenter : ISearchUserOutputBoundary
{
    public SearchUserViewModel? ViewModel { get; private set; }
    public void Present(SearchUserResponse response) => ViewModel = new(response.Users.Select(u => new UserSummaryViewModel(u.Handle, u.DisplayName)).ToArray());
}

public sealed class ViewUserPresenter : IViewUserOutputBoundary
{
    public ViewUserViewModel? ViewModel { get; private set; }
    public void Present(ViewUserResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Handle, response.DisplayName);
}
